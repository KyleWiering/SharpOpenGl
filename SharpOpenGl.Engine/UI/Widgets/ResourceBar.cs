using OpenTK.Mathematics;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.UI;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>
/// Displays the current amount, maximum, and income rate of all four resources.
/// Bind <see cref="Resources"/> each frame from <c>ResourceManager.GetDisplay()</c>.
/// </summary>
public sealed class ResourceBar : Widget
{
    private static readonly Dictionary<ResourceType, Vector4> ResourceColors = new()
    {
        [ResourceType.Energy]   = new Vector4(0.9f, 0.8f, 0.2f, 1f),
        [ResourceType.Minerals] = new Vector4(0.4f, 0.8f, 1.0f, 1f),
        [ResourceType.Data]     = new Vector4(0.6f, 1.0f, 0.6f, 1f),
        [ResourceType.Crew]     = new Vector4(1.0f, 0.6f, 0.4f, 1f),
    };

    /// <summary>
    /// Current resource snapshot.  Update this each frame from the economy system.
    /// </summary>
    public IReadOnlyList<ResourceDisplay> Resources { get; set; } =
        Array.Empty<ResourceDisplay>();

    /// <summary>Background colour of the bar panel.</summary>
    public Vector4 BackgroundColor { get; set; } = new Vector4(0.05f, 0.05f, 0.1f, 0.9f);

    /// <summary>Height of the fill bar in pixels.</summary>
    public float BarHeight { get; set; } = 5f;

    /// <summary>Font size for resource labels.</summary>
    public float FontSize { get; set; } = 16f;

    private const float Padding = 4f;
    private const float BadgeSize = 12f;
    private const float BadgeGap = 3f;
    private const float LabelBarGap = 2f;
    private int _hoveredSlotIndex = -1;

    /// <inheritdoc/>
    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        renderer.DrawRect(position, size, BackgroundColor);

        if (Resources.Count == 0) return;

        float slotWidth = size.X / Resources.Count;

        for (int i = 0; i < Resources.Count; i++)
        {
            ResourceDisplay res = Resources[i];
            float slotX = position.X + i * slotWidth + Padding;
            float y = position.Y + Padding;
            float slotW = slotWidth - Padding * 2f;

            Vector4 color = ResourceColors.TryGetValue(res.Type, out Vector4 c)
                ? c : new Vector4(1f, 1f, 1f, 1f);

            var badgePos = new Vector2(slotX, y);
            var badgeFill = color * 0.35f;
            badgeFill.W = 1f;
            renderer.DrawRect(badgePos, new Vector2(BadgeSize, BadgeSize), badgeFill);
            renderer.DrawRectOutline(badgePos, new Vector2(BadgeSize, BadgeSize), color);

            string abbrev = ResourceAbbreviation(res.Type);
            float abbrevSize = UIFontMetrics.FitFontSize(abbrev, FontSize - 4f, BadgeSize, 8f);
            float abbrevW = UIFontMetrics.MeasureTextWidth(abbrev, abbrevSize);
            renderer.DrawText(
                abbrev,
                badgePos + new Vector2((BadgeSize - abbrevW) * 0.5f, (BadgeSize - abbrevSize) * 0.5f),
                abbrevSize,
                color);

            float textX = slotX + BadgeSize + BadgeGap;
            float textW = slotW - BadgeSize - BadgeGap;

            float barY = y + FontSize + LabelBarGap;
            renderer.DrawRect(
                new Vector2(textX, barY),
                new Vector2(textW, BarHeight),
                new Vector4(0.2f, 0.2f, 0.2f, 1f));

            float fillW = textW * Math.Clamp(res.Fraction, 0f, 1f);
            if (fillW > 0f)
            {
                renderer.DrawRect(
                    new Vector2(textX, barY),
                    new Vector2(fillW, BarHeight),
                    color);
            }

            string income = FormatIncome(res.IncomePerSecond);
            string label = $"{res.Current:0}/{res.Max:0} {income}";
            float fittedSize = UIFontMetrics.FitFontSize(label, FontSize, textW, 10f);
            label = UITextDrawing.TruncateWithEllipsis(label, textW, fittedSize);
            renderer.DrawText(label, new Vector2(textX, y), fittedSize, color);
        }
    }

    /// <inheritdoc/>
    public override TooltipContent? GetTooltipContent()
    {
        if (!Visible || _hoveredSlotIndex < 0 || _hoveredSlotIndex >= Resources.Count)
            return null;

        ResourceDisplay res = Resources[_hoveredSlotIndex];
        string abbrev = ResourceAbbreviation(res.Type);
        string fullName = ResourceFullName(res.Type);
        string flavor = ResourceFlavorName(res.Type);

        return new TooltipContent(
            Title: $"{fullName} ({abbrev})",
            CostLine: $"{res.Current:0} / {res.Max:0}",
            RoleLine: flavor,
            BuildTime: $"Income: {FormatIncome(res.IncomePerSecond)}");
    }

    /// <inheritdoc/>
    public override void UpdatePointerState(
        Vector2 pointerPosition, bool isPointerDown,
        Vector2 containerPosition, Vector2 containerSize)
    {
        _hoveredSlotIndex = -1;
        if (!Visible) return;

        var (pos, size) = Resolve(containerPosition, containerSize);
        if (!Contains(pointerPosition, containerPosition, containerSize))
            return;

        if (Resources.Count == 0) return;

        float localX = pointerPosition.X - pos.X;
        float slotWidth = size.X / Resources.Count;
        int slot = (int)(localX / slotWidth);
        if (slot >= 0 && slot < Resources.Count)
            _hoveredSlotIndex = slot;
    }

    internal static string ResourceAbbreviation(ResourceType type) => type switch
    {
        ResourceType.Energy => "E",
        ResourceType.Minerals => "M",
        ResourceType.Data => "D",
        ResourceType.Crew => "C",
        _ => type.ToString()[..1],
    };

    internal static string ResourceFullName(ResourceType type) => type switch
    {
        ResourceType.Energy => "Energy",
        ResourceType.Minerals => "Minerals",
        ResourceType.Data => "Data",
        ResourceType.Crew => "Crew",
        _ => type.ToString(),
    };

    internal static string ResourceFlavorName(ResourceType type) => type switch
    {
        ResourceType.Energy => "Plasma Cores",
        ResourceType.Minerals => "Astrium Ore",
        ResourceType.Data => "Quantum Fragments",
        ResourceType.Crew => "Personnel",
        _ => string.Empty,
    };

    private static string FormatIncome(float incomePerSecond) =>
        incomePerSecond >= 0
            ? $"+{incomePerSecond:0.#}/s"
            : $"{incomePerSecond:0.#}/s";
}