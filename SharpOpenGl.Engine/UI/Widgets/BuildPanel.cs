using OpenTK.Mathematics;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.UI;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>
/// Production state for a queued build item.
/// </summary>
public enum QueuedState
{
    Building,
    Queued,
    Complete,
}

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

    /// <summary>Whether this item is already in the production queue.</summary>
    public bool IsQueued { get; set; }

    /// <summary>Operational role shown as the left-column hull glyph on the build button.</summary>
    public ShipRole? Role { get; init; }
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

    /// <summary>Whether this item is currently being built.</summary>
    public bool IsCurrent { get; init; }

    /// <summary>1-based position in the queue for display.</summary>
    public int QueueIndex { get; init; } = 1;

    /// <summary>Production state.</summary>
    public QueuedState State { get; init; } = QueuedState.Queued;

    /// <summary>Seconds remaining on the active build (0 for waiting items).</summary>
    public float RemainingSeconds { get; init; }

    /// <summary>Total build time in seconds for progress context.</summary>
    public float TotalBuildTime { get; init; }
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

    /// <summary>Fired when the player cancels a queued item. Argument is the 1-based queue index.</summary>
    public event Action<int>? QueueCancelRequested;

    /// <summary>Background colour.</summary>
    public Vector4 BackgroundColor { get; set; } = MenuTheme.PanelBackground;

    /// <summary>Button colour for affordable items.</summary>
    public Vector4 ButtonColor { get; set; } = new(0.15f, 0.35f, 0.15f, 1f);

    /// <summary>Button colour for unaffordable items.</summary>
    public Vector4 DisabledColor { get; set; } = new(0.3f, 0.15f, 0.15f, 1f);

    /// <summary>Button colour for items already in the production queue.</summary>
    public Vector4 QueuedColor { get; set; } = new(0.12f, 0.28f, 0.38f, 1f);

    /// <summary>Horizontal inset from panel edges for text and buttons.</summary>
    public const float HorizontalPadding = 8f;
    private const float IconColumnWidth = 28f;
    private const float IconGlyphSize = 24f;
    private const float ButtonHeight = 38f;
    private const float ButtonGap = 4f;
    private const float MinimumRowHitHeight = 44f;
    private const float RowHitPaddingVertical =
        (MinimumRowHitHeight - ButtonHeight) * 0.5f;
    private const float LabelLeftPad = 4f;
    private const float LabelRightPad = 4f;
    private const float PrimaryLabelTopPad = 5f;
    private const float SecondaryLabelBottomPad = 4f;
    private const int ProductionNameMaxLines = 2;
    private const int CompactQueueThreshold = 3;
    private const float QueueRowHeight = 14f;
    private const float CurrentProgressBarHeight = 10f;
    private const float CurrentStatusRowHeight = 14f;
    private const float CurrentQueueBlockHeight = CurrentProgressBarHeight + 2f + CurrentStatusRowHeight;
    private const float WaitingRowHeightNormal = 16f;
    private const float WaitingRowHeightCompact = 14f;
    private const float QueueBadgeWidth = 22f;
    private const float CancelButtonWidth = 18f;
    private const float CancelButtonHeight = 14f;
    private float FontSize { get; } = 12f;
    private float _scrollOffsetY;
    private int _hoveredItemIndex = -1;
    private int _pressedItemIndex = -1;
    private int _hoveredCancelQueueIndex;

    private bool UseCompactQueue => Queue.Count > CompactQueueThreshold;

    private float EffectiveWaitingRowHeight =>
        UseCompactQueue ? WaitingRowHeightCompact : WaitingRowHeightNormal;

    /// <summary>Inner content width derived from the resolved panel width.</summary>
    public static float ComputeContentWidth(float panelWidth) =>
        MathF.Max(0f, panelWidth - HorizontalPadding * 2f);

    private static (string Text, float LogicalDrawSize) FitLabel(
        IUIRenderer renderer, string text, float maxLogicalWidth,
        float preferredLogicalSize, float minLogicalSize = 8f)
    {
        float physicalScale = MathF.Max(renderer.ScaleToPhysical(1f), 0.001f);
        float preferredPhysical = renderer.ResolveFontSize(preferredLogicalSize);
        // Reserve physical pixels so MinPhysicalFontSize ellipsis rows never bleed past the panel edge.
        float physicalGutter = MathF.Max(2f, preferredPhysical * 0.12f);
        float maxPhysicalWidth = MathF.Max(0f, renderer.ScaleToPhysical(maxLogicalWidth) - physicalGutter);
        float minPhysical = MathF.Max(
            ScaledUIRenderer.MinPhysicalFontSize,
            renderer.ResolveFontSize(minLogicalSize));
        float fittedPhysical = UIFontMetrics.FitFontSize(
            text, preferredPhysical, maxPhysicalWidth, minPhysical);
        string display = UITextDrawing.TruncateWithEllipsis(text, maxPhysicalWidth, fittedPhysical);
        float logicalDrawSize = fittedPhysical / physicalScale;
        float drawPhysicalSize = renderer.ResolveFontSize(logicalDrawSize);
        if (!string.IsNullOrEmpty(display)
            && UIFontMetrics.MeasureTextWidth(display, drawPhysicalSize) > maxPhysicalWidth + UITextDrawing.WidthTolerance)
            display = string.Empty;
        return (display, logicalDrawSize);
    }

    private static void DrawFittedText(
        IUIRenderer renderer, string text, Vector2 position, float maxLogicalWidth,
        float preferredLogicalSize, Vector4 color, float minLogicalSize = 8f)
    {
        var (display, logicalSize) = FitLabel(
            renderer, text, maxLogicalWidth, preferredLogicalSize, minLogicalSize);
        renderer.DrawText(display, position, logicalSize, color);
    }

    private static (IReadOnlyList<string> Lines, float LogicalDrawSize) FitWrappedLabel(
        IUIRenderer renderer, string text, float maxLogicalWidth,
        float preferredLogicalSize, int maxLines, float minLogicalSize = 8f)
    {
        float physicalScale = MathF.Max(renderer.ScaleToPhysical(1f), 0.001f);
        float preferredPhysical = renderer.ResolveFontSize(preferredLogicalSize);
        float physicalGutter = MathF.Max(2f, preferredPhysical * 0.12f);
        float maxPhysicalWidth = MathF.Max(0f, renderer.ScaleToPhysical(maxLogicalWidth) - physicalGutter);
        float minPhysical = MathF.Max(
            ScaledUIRenderer.MinPhysicalFontSize,
            renderer.ResolveFontSize(minLogicalSize));

        float fittedPhysical = preferredPhysical;
        var lines = UITextDrawing.WrapTextLimited(text, maxPhysicalWidth, fittedPhysical, maxLines);
        float widestLine = lines.Count == 0
            ? 0f
            : lines.Max(line => UIFontMetrics.MeasureTextWidth(line, fittedPhysical));

        while (fittedPhysical > minPhysical && widestLine > maxPhysicalWidth + UITextDrawing.WidthTolerance)
        {
            fittedPhysical -= 1f;
            lines = UITextDrawing.WrapTextLimited(text, maxPhysicalWidth, fittedPhysical, maxLines);
            widestLine = lines.Max(line => UIFontMetrics.MeasureTextWidth(line, fittedPhysical));
        }

        float logicalDrawSize = fittedPhysical / physicalScale;
        return (lines, logicalDrawSize);
    }

    private static int MaxProductionNameLines(float areaHeight, float logicalFontSize, int cap = ProductionNameMaxLines)
    {
        if (areaHeight <= 0f || logicalFontSize <= 0f)
            return 1;

        float lineStep = logicalFontSize * UIFontMetrics.GlyphHeightFactor;
        int fit = Math.Max(1, (int)MathF.Floor(areaHeight / lineStep + 0.001f));
        return Math.Min(cap, fit);
    }

    private static void DrawWrappedFittedText(
        IUIRenderer renderer, string text, Vector2 position, float maxLogicalWidth,
        float preferredLogicalSize, Vector4 color, int maxLines,
        float maxLogicalHeight = 0f, float minLogicalSize = 8f)
    {
        var (_, logicalDrawSize) = FitLabel(
            renderer, text, maxLogicalWidth, preferredLogicalSize, minLogicalSize);

        int effectiveMaxLines = maxLines;
        if (maxLogicalHeight > 0f)
        {
            effectiveMaxLines = Math.Min(
                maxLines,
                MaxProductionNameLines(maxLogicalHeight, logicalDrawSize, maxLines));
        }

        var (lines, drawSize) = FitWrappedLabel(
            renderer, text, maxLogicalWidth, preferredLogicalSize, effectiveMaxLines, minLogicalSize);

        float lineStep = drawSize * UIFontMetrics.GlyphHeightFactor;
        for (int i = 0; i < lines.Count; i++)
            renderer.DrawText(lines[i], position + new Vector2(0f, i * lineStep), drawSize, color);
    }

    private static float QueueWaitingLabelMaxWidth(float textMaxW) =>
        textMaxW - QueueBadgeWidth - CancelButtonWidth - 6f;

    private static Vector4 Brighten(Vector4 color, float amount = 0.12f) =>
        new(
            MathF.Min(color.X + amount, 1f),
            MathF.Min(color.Y + amount, 1f),
            MathF.Min(color.Z + amount, 1f),
            color.W);

    /// <inheritdoc/>
    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        renderer.DrawRect(position, size, BackgroundColor);
        renderer.DrawRectOutline(position, size, MenuTheme.PanelBorder);

        float textMaxW = ComputeContentWidth(size.X);
        float y = position.Y + 6f - _scrollOffsetY;
        float x = position.X + HorizontalPadding;
        float bottom = position.Y + size.Y;

        var (_, titleSize) = FitLabel(renderer, BuildingName, textMaxW, FontSize + 2f);
        if (y + titleSize + 2f > position.Y && y < bottom)
            DrawFittedText(renderer, BuildingName, new Vector2(x, y), textMaxW, FontSize + 2f,
                new Vector4(0.9f, 0.8f, 0.4f, 1f));
        y += titleSize + 10f;

        if (!string.IsNullOrEmpty(SupplyText))
        {
            var (_, supplySize) = FitLabel(renderer, $"Supply: {SupplyText}", textMaxW, FontSize);
            if (y + supplySize > position.Y && y < bottom)
                DrawFittedText(renderer, $"Supply: {SupplyText}", new Vector2(x, y), textMaxW, FontSize,
                    new Vector4(0.7f, 0.7f, 0.9f, 1f));
            y += supplySize + 6f;
        }

        var current = Queue.FirstOrDefault(item => item.IsCurrent);
        int currentIndex = current?.QueueIndex ?? 1;
        string headerText = Queue.Count switch
        {
            0 => "Production Queue: Idle",
            1 => "Production Queue (1/1)",
            _ => $"Production Queue ({currentIndex}/{Queue.Count})",
        };
        var (_, headerSize) = FitLabel(renderer, headerText, textMaxW, FontSize);
        if (y + headerSize > position.Y && y < bottom)
        {
            DrawFittedText(renderer, headerText, new Vector2(x, y), textMaxW, FontSize,
                Queue.Count > 0
                    ? new Vector4(0.8f, 0.8f, 0.8f, 1f)
                    : new Vector4(0.62f, 0.66f, 0.72f, 1f));
        }
        y += headerSize + 2f;

        if (Queue.Count == 0)
        {
            var (_, idleSize) = FitLabel(
                renderer, "Click a unit below to queue production", textMaxW, FontSize - 2f, 8f);
            if (y + idleSize > position.Y && y < bottom)
            {
                DrawFittedText(renderer, "Click a unit below to queue production", new Vector2(x, y),
                    textMaxW, FontSize - 2f, new Vector4(0.55f, 0.6f, 0.68f, 1f), 8f);
            }
            y += QueueRowHeight;
            y += 4f;
        }
        else
        {
            if (current != null)
            {
                float contentW = textMaxW - CancelButtonWidth - 2f;
                if (y + CurrentQueueBlockHeight > position.Y && y < bottom)
                {
                    renderer.DrawRect(new Vector2(x, y), new Vector2(contentW, CurrentProgressBarHeight),
                        new Vector4(0.15f, 0.15f, 0.15f, 1f));
                    float fill = contentW * Math.Clamp(current.Progress, 0f, 1f);
                    if (fill > 0f)
                        renderer.DrawRect(new Vector2(x, y), new Vector2(fill, CurrentProgressBarHeight),
                            new Vector4(0.3f, 0.8f, 0.3f, 1f));

                    string status = FormatCurrentQueueStatus(current);
                    DrawFittedText(
                        renderer, status, new Vector2(x + 2f, y + CurrentProgressBarHeight + 2f),
                        contentW - 4f, FontSize - 2f, new Vector4(0.92f, 0.95f, 1f, 1f), 8f);

                    DrawQueueCancelButton(
                        renderer, new Vector2(x + textMaxW - CancelButtonWidth, y + 4f),
                        _hoveredCancelQueueIndex == current.QueueIndex);
                }

                y += CurrentQueueBlockHeight;
            }

            var waitingItems = Queue.Where(q => !q.IsCurrent).OrderBy(q => q.QueueIndex).ToList();
            if (UseCompactQueue)
            {
                if (waitingItems.Count > 0)
                    DrawWaitingRow(renderer, x, ref y, waitingItems[0], textMaxW, position.Y, bottom);

                int collapsedCount = waitingItems.Count - 1;
                if (collapsedCount > 0)
                    DrawCollapsedQueueSummary(renderer, x, ref y, collapsedCount, textMaxW, position.Y, bottom);
            }
            else
            {
                foreach (var item in waitingItems)
                    DrawWaitingRow(renderer, x, ref y, item, textMaxW, position.Y, bottom);
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
            Vector4 color = item.IsQueued
                ? QueuedColor
                : item.CanAfford ? ButtonColor : DisabledColor;

            bool isHovered = _hoveredItemIndex == i;
            bool isPressed = _pressedItemIndex == i;
            if (isHovered)
                color = Brighten(color);

            if (y + btnH > position.Y && y < bottom)
            {
                renderer.DrawRect(btnPos, btnSize, color);
                Vector4 borderColor = isPressed
                    ? new Vector4(1f, 0.95f, 0.55f, 1f)
                    : isHovered
                        ? MenuTheme.ButtonBorderHover
                        : item.CanAfford
                            ? MenuTheme.ButtonBorder with { W = MenuTheme.ButtonBorder.W * 0.65f }
                            : MenuTheme.ButtonBorder with { W = MenuTheme.ButtonBorder.W * 0.45f };
                renderer.DrawRectOutline(btnPos, btnSize, borderColor);

                float iconX = btnPos.X + (IconColumnWidth - IconGlyphSize) * 0.5f;
                float iconY = btnPos.Y + (btnH - IconGlyphSize) * 0.5f;
                DrawProductionIcon(
                    renderer, item, new Vector2(iconX, iconY),
                    isHovered, !item.CanAfford, item.IsQueued);

                float labelX = btnPos.X + IconColumnWidth + LabelLeftPad;
                float labelMaxW = btnW - IconColumnWidth - LabelLeftPad - LabelRightPad;
                var (_, costSize) = FitLabel(
                    renderer, FormatAbbreviatedCosts(item), labelMaxW, FontSize - 2f, 8f);

                Vector4 nameColor = item.IsQueued
                    ? new Vector4(0.75f, 0.9f, 1f, 1f)
                    : item.CanAfford
                        ? MenuTheme.ButtonText
                        : MenuTheme.ButtonTextDisabled;
                Vector4 costColor = item.IsQueued
                    ? new Vector4(0.58f, 0.72f, 0.82f, 0.95f)
                    : item.CanAfford
                        ? MenuTheme.MutedTextColor
                        : Darken(MenuTheme.MutedTextColor, 0.75f);

                float nameAreaHeight = btnH - PrimaryLabelTopPad - SecondaryLabelBottomPad - costSize;
                DrawWrappedFittedText(
                    renderer, item.Name, new Vector2(labelX, btnPos.Y + PrimaryLabelTopPad),
                    labelMaxW, FontSize, nameColor, ProductionNameMaxLines, nameAreaHeight);
                DrawFittedText(
                    renderer, FormatAbbreviatedCosts(item),
                    new Vector2(labelX, btnPos.Y + btnH - costSize - SecondaryLabelBottomPad),
                    labelMaxW, FontSize - 2f, costColor, 8f);
            }

            y += btnH + ButtonGap;
        }
    }

    /// <inheritdoc/>
    public override TooltipContent? GetTooltipContent()
    {
        if (!Visible)
            return null;

        if (_hoveredCancelQueueIndex > 0)
        {
            var queued = Queue.FirstOrDefault(item => item.QueueIndex == _hoveredCancelQueueIndex);
            string title = queued?.IsCurrent == true
                ? "Cancel current build"
                : "Cancel queued unit";
            return new TooltipContent(
                Title: title,
                CostLine: "Full resource refund",
                BuildTime: queued?.IsCurrent == true ? "Progress is lost" : null,
                RoleLine: null,
                AffordReason: "Click X to remove from queue");
        }

        if (_hoveredItemIndex < 0 || _hoveredItemIndex >= AvailableItems.Count)
            return null;

        var item = AvailableItems[_hoveredItemIndex];
        string buildTime = item.BuildTime <= 0f
            ? "Build: instant"
            : $"Build: {item.BuildTime:0}s";

        string? affordReason = null;
        if (!item.CanAfford)
            affordReason = "Insufficient resources";
        else if (item.IsQueued)
            affordReason = "In production";

        string? roleLine = item.Role is ShipRole role
            ? $"Role: {ShipRoleResolver.DisplayName(role)}"
            : null;

        return new TooltipContent(
            Title: item.IsQueued ? $"{item.Name} (Queued)" : item.Name,
            CostLine: $"E:{item.EnergyCost} M:{item.MineralsCost} D:{item.DataCost} C:{item.CrewCost}",
            BuildTime: buildTime,
            RoleLine: roleLine,
            AffordReason: affordReason);
    }

    /// <inheritdoc/>
    public override void UpdatePointerState(
        Vector2 pointerPosition, bool isPointerDown,
        Vector2 containerPosition, Vector2 containerSize)
    {
        _hoveredItemIndex = -1;
        _hoveredCancelQueueIndex = 0;
        if (!isPointerDown)
            _pressedItemIndex = -1;
        if (!Visible) return;

        var (pos, size) = Resolve(containerPosition, containerSize);
        if (!Contains(pointerPosition, containerPosition, containerSize))
            return;

        Vector2 local = pointerPosition - pos;
        if (TryHitQueueCancel(local, size.X, out int cancelIndex))
        {
            _hoveredCancelQueueIndex = cancelIndex;
            return;
        }

        float y = ContentTopOffset() - _scrollOffsetY;
        for (int i = 0; i < AvailableItems.Count; i++)
        {
            if (PointInRowHitArea(local.Y, y))
            {
                _hoveredItemIndex = i;
                if (isPointerDown)
                    _pressedItemIndex = i;
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

        float scrollStep = ButtonHeight + ButtonGap;
        _scrollOffsetY = Math.Clamp(_scrollOffsetY + (deltaY > 0f ? scrollStep : -scrollStep), 0f, maxScroll);
        return true;
    }

    private void DrawWaitingRow(
        IUIRenderer renderer, float x, ref float y, QueuedItem item,
        float textMaxW, float clipTop, float clipBottom)
    {
        float rowHeight = EffectiveWaitingRowHeight;
        if (y + rowHeight > clipTop && y < clipBottom)
        {
            string badge = $"#{item.QueueIndex}";
            renderer.DrawRect(new Vector2(x, y), new Vector2(QueueBadgeWidth, rowHeight - 2f),
                new Vector4(0.18f, 0.22f, 0.32f, 1f));
            float badgeMaxW = QueueBadgeWidth - 6f;
            DrawFittedText(renderer, badge, new Vector2(x + 3f, y + 1f), badgeMaxW,
                FontSize - 3f, new Vector4(0.75f, 0.85f, 1f, 1f), 8f);

            float labelMaxW = QueueWaitingLabelMaxWidth(textMaxW);
            DrawFittedText(renderer, item.Name, new Vector2(x + QueueBadgeWidth + 4f, y + 1f),
                labelMaxW, FontSize - 2f, new Vector4(0.75f, 0.78f, 0.82f, 1f), 8f);

            DrawQueueCancelButton(
                renderer,
                new Vector2(x + textMaxW - CancelButtonWidth, y + (rowHeight - CancelButtonHeight) * 0.5f),
                _hoveredCancelQueueIndex == item.QueueIndex);
        }

        y += rowHeight;
    }

    private void DrawCollapsedQueueSummary(
        IUIRenderer renderer, float x, ref float y, int collapsedCount,
        float textMaxW, float clipTop, float clipBottom)
    {
        float rowHeight = EffectiveWaitingRowHeight;
        if (y + rowHeight > clipTop && y < clipBottom)
        {
            DrawFittedText(renderer, $"+{collapsedCount} more in queue", new Vector2(x + 2f, y + 1f),
                textMaxW - 4f, FontSize - 2f, new Vector4(0.62f, 0.68f, 0.74f, 1f), 8f);
        }

        y += rowHeight;
    }

    private float ContentTopOffset()
    {
        float y = 6f + FontSize + 10f;
        if (!string.IsNullOrEmpty(SupplyText)) y += FontSize + 6f;

        y += FontSize + 2f;
        if (Queue.Count == 0)
        {
            y += QueueRowHeight + 4f;
        }
        else
        {
            if (Queue.Any(item => item.IsCurrent))
                y += CurrentQueueBlockHeight;

            int waitingCount = Queue.Count(item => !item.IsCurrent);
            if (UseCompactQueue)
            {
                if (waitingCount > 0)
                    y += EffectiveWaitingRowHeight;
                if (waitingCount > 1)
                    y += EffectiveWaitingRowHeight;
            }
            else
            {
                y += waitingCount * WaitingRowHeightNormal;
            }

            y += 4f;
        }

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

        if (screenPoint.X < pos.X || screenPoint.X >= pos.X + size.X ||
            screenPoint.Y < pos.Y || screenPoint.Y >= pos.Y + size.Y)
            return false;

        Vector2 localPos = screenPoint - pos;

        if (TryHitQueueCancel(localPos, size.X, out int cancelIndex))
        {
            QueueCancelRequested?.Invoke(cancelIndex);
            return true;
        }

        float y = ContentTopOffset() - _scrollOffsetY;

        for (int i = 0; i < AvailableItems.Count; i++)
        {
            if (PointInRowHitArea(localPos.Y, y))
            {
                var item = AvailableItems[i];
                _pressedItemIndex = i;
                if (item.CanAfford)
                    BuildRequested?.Invoke(item.Id);
                return true;
            }
            y += ButtonHeight + ButtonGap;
        }

        return false;
    }

    private static bool PointInRowHitArea(float localY, float rowTop) =>
        localY >= rowTop - RowHitPaddingVertical
        && localY < rowTop + ButtonHeight + RowHitPaddingVertical;

    private static string FormatAbbreviatedCosts(BuildableItem item)
    {
        string time = item.BuildTime <= 0f ? "instant" : $"{item.BuildTime:0}s";
        return $"E{item.EnergyCost} M{item.MineralsCost} D{item.DataCost} C{item.CrewCost} · {time}";
    }

    private static void DrawProductionIcon(
        IUIRenderer renderer, BuildableItem item, Vector2 position,
        bool isHovered, bool isDisabled, bool isQueued)
    {
        float brighten = isHovered ? 1.12f : 1f;
        float dim = isDisabled ? 0.55f : isQueued ? 0.88f : 1f;
        float alpha = isDisabled ? 0.65f : isQueued ? 0.9f : 1f;

        if (item.Role is ShipRole role)
        {
            var (primary, accent) = HullRoleIconTints(role);
            MenuIconDrawing.DrawShipRole(
                renderer, role, position, IconGlyphSize,
                TintIconColor(primary, brighten, dim, alpha),
                TintIconColor(accent, brighten, dim, alpha));
            return;
        }

        var fallbackPrimary = new Vector4(0.62f, 0.14f, 0.14f, 0.95f);
        var fallbackAccent = new Vector4(0.98f, 0.88f, 0.88f, 1f);
        MenuIconDrawing.Draw(
            renderer, MenuIconKind.HullMilitary, position, IconGlyphSize,
            TintIconColor(fallbackPrimary, brighten, dim, alpha),
            TintIconColor(fallbackAccent, brighten, dim, alpha));
    }

    private static (Vector4 Primary, Vector4 Accent) HullRoleIconTints(ShipRole role) => role switch
    {
        ShipRole.Military => (new Vector4(0.62f, 0.14f, 0.14f, 0.95f), new Vector4(0.98f, 0.88f, 0.88f, 1f)),
        ShipRole.Engineering => (new Vector4(0.62f, 0.42f, 0.08f, 0.95f), new Vector4(0.98f, 0.92f, 0.72f, 1f)),
        ShipRole.Political => (new Vector4(0.62f, 0.50f, 0.10f, 0.95f), new Vector4(1f, 0.94f, 0.55f, 1f)),
        _ => (new Vector4(0.3f, 0.3f, 0.3f, 0.95f), new Vector4(1f, 1f, 1f, 1f)),
    };

    private static Vector4 TintIconColor(Vector4 color, float brighten, float dim, float alpha) =>
        new(
            MathF.Min(color.X * brighten * dim, 1f),
            MathF.Min(color.Y * brighten * dim, 1f),
            MathF.Min(color.Z * brighten * dim, 1f),
            color.W * alpha);

    private static Vector4 Darken(Vector4 color, float factor) =>
        new(color.X * factor, color.Y * factor, color.Z * factor, color.W);

    private static string FormatCurrentQueueStatus(QueuedItem current)
    {
        int pct = (int)MathF.Round(Math.Clamp(current.Progress, 0f, 1f) * 100f);
        if (current.RemainingSeconds > 0.5f)
            return $"Building · {pct}% · {current.RemainingSeconds:0}s left";

        if (current.TotalBuildTime > 0f)
            return $"Building · {pct}%";

        return $"Building · {current.Name}";
    }

    private static void DrawQueueCancelButton(IUIRenderer renderer, Vector2 position, bool isHovered)
    {
        var fill = isHovered
            ? new Vector4(0.55f, 0.2f, 0.2f, 1f)
            : new Vector4(0.35f, 0.15f, 0.15f, 1f);
        renderer.DrawRect(position, new Vector2(CancelButtonWidth, CancelButtonHeight), fill);
        DrawFittedText(renderer, "X", position + new Vector2(4f, 1f),
            CancelButtonWidth - 6f, 10f, new Vector4(1f, 0.85f, 0.85f, 1f), 8f);
    }

    private bool TryHitQueueCancel(Vector2 localPos, float panelWidth, out int queueIndex)
    {
        queueIndex = 0;
        if (Queue.Count == 0)
            return false;

        float textMaxW = ComputeContentWidth(panelWidth);
        float x = HorizontalPadding;
        float y = QueueSectionTopOffset() - _scrollOffsetY;

        var current = Queue.FirstOrDefault(item => item.IsCurrent);
        if (current != null)
        {
            var cancelPos = new Vector2(x + textMaxW - CancelButtonWidth, y + 4f);
            if (PointInCancelButton(localPos, cancelPos))
            {
                queueIndex = current.QueueIndex;
                return true;
            }

            y += CurrentQueueBlockHeight;
        }

        var waitingItems = Queue.Where(q => !q.IsCurrent).OrderBy(q => q.QueueIndex).ToList();
        if (UseCompactQueue)
        {
            if (waitingItems.Count > 0)
            {
                float rowHeight = EffectiveWaitingRowHeight;
                var cancelPos = new Vector2(
                    x + textMaxW - CancelButtonWidth,
                    y + (rowHeight - CancelButtonHeight) * 0.5f);
                if (PointInCancelButton(localPos, cancelPos))
                {
                    queueIndex = waitingItems[0].QueueIndex;
                    return true;
                }
            }
        }
        else
        {
            foreach (var item in waitingItems)
            {
                float rowHeight = EffectiveWaitingRowHeight;
                var cancelPos = new Vector2(
                    x + textMaxW - CancelButtonWidth,
                    y + (rowHeight - CancelButtonHeight) * 0.5f);
                if (PointInCancelButton(localPos, cancelPos))
                {
                    queueIndex = item.QueueIndex;
                    return true;
                }

                y += rowHeight;
            }
        }

        return false;
    }

    private float QueueSectionTopOffset()
    {
        float y = 6f + FontSize + 10f;
        if (!string.IsNullOrEmpty(SupplyText))
            y += FontSize + 6f;
        return y + FontSize + 2f;
    }

    private static bool PointInCancelButton(Vector2 localPos, Vector2 buttonTopLeft) =>
        localPos.X >= buttonTopLeft.X
        && localPos.X < buttonTopLeft.X + CancelButtonWidth
        && localPos.Y >= buttonTopLeft.Y
        && localPos.Y < buttonTopLeft.Y + CancelButtonHeight;
}