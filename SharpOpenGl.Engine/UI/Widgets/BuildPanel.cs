using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>
/// Data for a single buildable item displayed in the <see cref="BuildPanel"/>.
/// </summary>
public sealed class BuildableItem
{
    /// <summary>Entity definition ID.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Display name shown to the player.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Energy cost.</summary>
    public int EnergyCost { get; init; }

    /// <summary>Minerals cost.</summary>
    public int MineralsCost { get; init; }

    /// <summary>Data cost.</summary>
    public int DataCost { get; init; }

    /// <summary>Crew cost.</summary>
    public int CrewCost { get; init; }

    /// <summary>Build time in seconds.</summary>
    public float BuildTime { get; init; }

    /// <summary>Whether the player can afford this item.</summary>
    public bool CanAfford { get; set; } = true;
}

/// <summary>
/// Data for items currently in the build queue.
/// </summary>
public sealed class QueuedItem
{
    /// <summary>Display name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Build progress fraction 0–1.</summary>
    public float Progress { get; init; }
}

/// <summary>
/// Shows buildable items for a selected building and the current production queue.
/// Raises <see cref="BuildRequested"/> when the player clicks a build button.
/// </summary>
public sealed class BuildPanel : Widget
{
    /// <summary>Buildable items available at the selected building.</summary>
    public IReadOnlyList<BuildableItem> AvailableItems { get; set; } = Array.Empty<BuildableItem>();

    /// <summary>Current production queue.</summary>
    public IReadOnlyList<QueuedItem> Queue { get; set; } = Array.Empty<QueuedItem>();

    /// <summary>Building type name shown at the top of the panel.</summary>
    public string BuildingName { get; set; } = string.Empty;

    /// <summary>Supply info: "Used / Cap".</summary>
    public string SupplyText { get; set; } = string.Empty;

    /// <summary>Fired when the player clicks to build an item. Argument is the definition ID.</summary>
    public event Action<string>? BuildRequested;

    /// <summary>Background colour.</summary>
    public Vector4 BackgroundColor { get; set; } = new(0.05f, 0.05f, 0.12f, 0.92f);

    /// <summary>Button colour for affordable items.</summary>
    public Vector4 ButtonColor { get; set; } = new(0.15f, 0.35f, 0.15f, 1f);

    /// <summary>Button colour for unaffordable items.</summary>
    public Vector4 DisabledColor { get; set; } = new(0.3f, 0.15f, 0.15f, 1f);

    private const float HorizontalPadding = 8f;
    private const float MaxTextInnerWidth = 260f;
    private const float ButtonHeight = 28f;
    private const float ButtonGap = 4f;
    private float FontSize { get; } = 12f;
    private float _scrollOffsetY;
    private int _hoveredItemIndex = -1;

    private static string FitLabel(string text, float maxWidth, float preferredSize, out float drawSize)
    {
        float minSize = MathF.Max(8f, preferredSize - 4f);
        drawSize = UIFontMetrics.FitFontSize(text, preferredSize, maxWidth, minSize);
        return UITextDrawing.TruncateWithEllipsis(text, maxWidth, drawSize);
    }

    /// <inheritdoc/>
    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        renderer.DrawRect(position, size, BackgroundColor);
        renderer.DrawRectOutline(position, size, new Vector4(0.5f, 0.4f, 0.2f, 1f));

        float textMaxW = MathF.Min(size.X - HorizontalPadding * 2f, MaxTextInnerWidth);
        float y = position.Y + 6f - _scrollOffsetY;
        float x = position.X + HorizontalPadding;
        float bottom = position.Y + size.Y;

        string title = FitLabel(BuildingName, textMaxW, FontSize + 2f, out float titleSize);
        if (y + titleSize + 2f > position.Y && y < bottom)
            renderer.DrawText(title, new Vector2(x, y), titleSize,
                new Vector4(0.9f, 0.8f, 0.4f, 1f));
        y += titleSize + 10f;

        if (!string.IsNullOrEmpty(SupplyText))
        {
            string supply = FitLabel($"Supply: {SupplyText}", textMaxW, FontSize, out float supplySize);
            if (y + supplySize > position.Y && y < bottom)
                renderer.DrawText(supply, new Vector2(x, y), supplySize,
                    new Vector4(0.7f, 0.7f, 0.9f, 1f));
            y += supplySize + 6f;
        }

        if (Queue.Count > 0)
        {
            if (y + FontSize > position.Y && y < bottom)
                renderer.DrawText("Building:", new Vector2(x, y), FontSize,
                    new Vector4(0.8f, 0.8f, 0.8f, 1f));
            y += FontSize + 2f;
            foreach (var item in Queue)
            {
                float barW = textMaxW;
                if (y + 14f > position.Y && y < bottom)
                {
                    renderer.DrawRect(new Vector2(x, y), new Vector2(barW, 10f),
                        new Vector4(0.15f, 0.15f, 0.15f, 1f));
                    float fill = barW * Math.Clamp(item.Progress, 0f, 1f);
                    if (fill > 0f)
                        renderer.DrawRect(new Vector2(x, y), new Vector2(fill, 10f),
                            new Vector4(0.3f, 0.8f, 0.3f, 1f));
                    string queueLabel = FitLabel(
                        $"{item.Name} ({item.Progress * 100:0}%)", barW - 4f, FontSize - 2f, out float queueSize);
                    renderer.DrawText(queueLabel, new Vector2(x + 2f, y - 1f), queueSize,
                        new Vector4(1f, 1f, 1f, 1f));
                }
                y += 14f;
            }
            y += 4f;
        }

        float btnH = ButtonHeight;
        float btnW = textMaxW;
        for (int i = 0; i < AvailableItems.Count; i++)
        {
            var item = AvailableItems[i];
            var btnPos = new Vector2(x, y);
            var btnSize = new Vector2(btnW, btnH);
            var color = item.CanAfford ? ButtonColor : DisabledColor;

            if (y + btnH > position.Y && y < bottom)
            {
                renderer.DrawRect(btnPos, btnSize, color);
                renderer.DrawRectOutline(btnPos, btnSize, new Vector4(0.6f, 0.6f, 0.6f, 0.5f));

                string label = FitLabel(
                    $"{item.Name}  E:{item.EnergyCost} M:{item.MineralsCost} " +
                    $"D:{item.DataCost} C:{item.CrewCost}  ({item.BuildTime:0}s)",
                    btnW - 8f, FontSize, out float itemSize);
                renderer.DrawText(label, btnPos + new Vector2(4f, 6f), itemSize,
                    item.CanAfford ? new Vector4(1f, 1f, 1f, 1f) : new Vector4(0.6f, 0.4f, 0.4f, 1f));
            }

            y += btnH + ButtonGap;
        }
    }

    /// <inheritdoc/>
    public override TooltipContent? GetTooltipContent()
    {
        if (!Visible || _hoveredItemIndex < 0 || _hoveredItemIndex >= AvailableItems.Count)
            return null;

        var item = AvailableItems[_hoveredItemIndex];
        string buildTime = item.BuildTime <= 0f
            ? "Build: instant"
            : $"Build: {item.BuildTime:0}s";

        return new TooltipContent(
            Title: item.Name,
            CostLine: $"E:{item.EnergyCost} M:{item.MineralsCost} D:{item.DataCost} C:{item.CrewCost}",
            BuildTime: buildTime,
            AffordReason: item.CanAfford ? null : "Insufficient resources");
    }

    /// <inheritdoc/>
    public override void UpdatePointerState(
        Vector2 pointerPosition, bool isPointerDown,
        Vector2 containerPosition, Vector2 containerSize)
    {
        _hoveredItemIndex = -1;
        if (!Visible) return;

        var (pos, size) = Resolve(containerPosition, containerSize);
        if (!Contains(pointerPosition, containerPosition, containerSize))
            return;

        Vector2 local = pointerPosition - pos;
        float y = ContentTopOffset() - _scrollOffsetY;
        for (int i = 0; i < AvailableItems.Count; i++)
        {
            if (local.Y >= y && local.Y < y + ButtonHeight)
            {
                _hoveredItemIndex = i;
                return;
            }

            y += ButtonHeight + ButtonGap;
        }
    }

    /// <inheritdoc/>
    public override bool HandleScroll(
        Vector2 screenPoint, float deltaY,
        Vector2 containerPosition, Vector2 containerSize)
    {
        if (!Visible) return false;

        var (pos, size) = Resolve(containerPosition, containerSize);
        if (screenPoint.X < pos.X || screenPoint.X >= pos.X + size.X ||
            screenPoint.Y < pos.Y || screenPoint.Y >= pos.Y + size.Y)
            return false;

        float contentHeight = MeasureContentHeight(size.X);
        float maxScroll = Math.Max(0f, contentHeight - size.Y);
        if (maxScroll <= 0f) return false;

        _scrollOffsetY = Math.Clamp(_scrollOffsetY + (deltaY > 0f ? 32f : -32f), 0f, maxScroll);
        return true;
    }

    private float ContentTopOffset()
    {
        float y = 6f + FontSize + 10f;
        if (!string.IsNullOrEmpty(SupplyText)) y += FontSize + 6f;
        if (Queue.Count > 0) y += FontSize + 2f + Queue.Count * 14f + 4f;
        return y;
    }

    private float MeasureContentHeight(float width)
    {
        _ = width;
        float y = ContentTopOffset();
        y += AvailableItems.Count * (ButtonHeight + ButtonGap);
        return y + 8f;
    }

    /// <inheritdoc/>
    public override bool HandlePointerTapped(
        Vector2 screenPoint, int button,
        Vector2 containerPosition, Vector2 containerSize)
    {
        if (!Visible || button != 0) return false;

        var (pos, size) = Resolve(containerPosition, containerSize);

        // Check if click is within this panel
        if (screenPoint.X < pos.X || screenPoint.X >= pos.X + size.X ||
            screenPoint.Y < pos.Y || screenPoint.Y >= pos.Y + size.Y)
            return false;

        Vector2 localPos = screenPoint - pos;

        float y = ContentTopOffset() - _scrollOffsetY;

        for (int i = 0; i < AvailableItems.Count; i++)
        {
            if (localPos.Y >= y && localPos.Y < y + ButtonHeight)
            {
                var item = AvailableItems[i];
                if (item.CanAfford)
                    BuildRequested?.Invoke(item.Id);
                return true;
            }
            y += ButtonHeight + ButtonGap;
        }

        return false;
    }
}
