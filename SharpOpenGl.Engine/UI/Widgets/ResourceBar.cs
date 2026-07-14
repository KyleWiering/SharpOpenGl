using OpenTK.Mathematics;
using SharpOpenGl.Engine.Economy;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>
/// Displays the current amount, maximum, and income rate of all four resources.
/// Bind <see cref="Resources"/> each frame from <c>ResourceManager.GetDisplay()</c>.
/// </summary>
public sealed class ResourceBar : Widget
{
    // ── Colour palette ────────────────────────────────────────────────────────

    private static readonly Dictionary<ResourceType, Vector4> ResourceColors = new()
    {
        [ResourceType.Energy]   = new Vector4(0.9f, 0.8f, 0.2f, 1f),  // gold
        [ResourceType.Minerals] = new Vector4(0.4f, 0.8f, 1.0f, 1f),  // blue
        [ResourceType.Data]     = new Vector4(0.6f, 1.0f, 0.6f, 1f),  // green
        [ResourceType.Crew]     = new Vector4(1.0f, 0.6f, 0.4f, 1f),  // orange
    };

    // ── Data binding ──────────────────────────────────────────────────────────

    /// <summary>
    /// Current resource snapshot.  Update this each frame from the economy system.
    /// </summary>
    public IReadOnlyList<ResourceDisplay> Resources { get; set; } =
        Array.Empty<ResourceDisplay>();

    // ── Visual config ─────────────────────────────────────────────────────────

    /// <summary>Background colour of the bar panel.</summary>
    public Vector4 BackgroundColor { get; set; } = new Vector4(0.05f, 0.05f, 0.1f, 0.9f);

    /// <summary>Height of the fill bar in pixels.</summary>
    public float BarHeight { get; set; } = 6f;

    /// <summary>Font size for resource labels.</summary>
    public float FontSize { get; set; } = 16f;

    // ── Drawing ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        renderer.DrawRect(position, size, BackgroundColor);

        if (Resources.Count == 0) return;

        float slotWidth = size.X / Resources.Count;
        float padding = 6f;

        for (int i = 0; i < Resources.Count; i++)
        {
            ResourceDisplay res = Resources[i];
            float x = position.X + i * slotWidth + padding;
            float y = position.Y + padding;
            float slotW = slotWidth - padding * 2f;

            Vector4 color = ResourceColors.TryGetValue(res.Type, out Vector4 c)
                ? c : new Vector4(1f, 1f, 1f, 1f);

            // Fill bar background
            renderer.DrawRect(new Vector2(x, y + FontSize + 4f),
                new Vector2(slotW, BarHeight),
                new Vector4(0.2f, 0.2f, 0.2f, 1f));

            // Fill bar foreground
            float fillW = slotW * Math.Clamp(res.Fraction, 0f, 1f);
            if (fillW > 0f)
                renderer.DrawRect(new Vector2(x, y + FontSize + 4f),
                    new Vector2(fillW, BarHeight), color);

            string income = res.IncomePerSecond >= 0
                ? $"+{res.IncomePerSecond:0.#}/s"
                : $"{res.IncomePerSecond:0.#}/s";
            string label = $"{ShortResourceName(res.Type)} {res.Current:0}/{res.Max:0} {income}";
            float fittedSize = UIFontMetrics.FitFontSize(label, FontSize, slotW, 10f);
            label = UITextDrawing.TruncateWithEllipsis(label, slotW, fittedSize);
            renderer.DrawText(label, new Vector2(x, y), fittedSize, color);
        }
    }

    private static string ShortResourceName(ResourceType type) => type switch
    {
        ResourceType.Energy => "E",
        ResourceType.Minerals => "M",
        ResourceType.Data => "D",
        ResourceType.Crew => "C",
        _ => type.ToString()[..1],
    };
}
