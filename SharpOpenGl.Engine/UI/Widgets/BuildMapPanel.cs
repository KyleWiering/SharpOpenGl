using OpenTK.Mathematics;
using SharpOpenGl.Engine.Build;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>
/// Categorized build-map panel for placing base structures.
/// Opens with B key or HUD button; selecting a building enters placement mode.
/// </summary>
public sealed class BuildMapPanel : Widget
{
    /// <summary>Categories and entries to display.</summary>
    public IReadOnlyList<BuildMapCategoryView> Categories { get; set; } = [];

    /// <summary>Fired when the player selects a placeable building.</summary>
    public event Action<string>? BuildingSelected;

    /// <summary>Fired when the player closes the panel.</summary>
    public event Action? CloseRequested;

    public Vector4 BackgroundColor { get; set; } = new(0.04f, 0.05f, 0.12f, 0.94f);
    public Vector4 HeaderColor { get; set; } = new(0.85f, 0.75f, 0.35f, 1f);
    public Vector4 CategoryColor { get; set; } = new(0.55f, 0.7f, 0.95f, 1f);
    public Vector4 EnabledColor { get; set; } = new(0.14f, 0.32f, 0.18f, 1f);
    public Vector4 LockedColor { get; set; } = new(0.22f, 0.18f, 0.18f, 1f);
    public Vector4 UnaffordableColor { get; set; } = new(0.32f, 0.16f, 0.16f, 1f);

    private const float FontSize = 12f;
    private const float TitleHeight = 28f;
    private const float CategoryHeaderHeight = 20f;
    private const float TileSize = 56f;
    private const float IconSize = 48f;
    private const float IconPadding = (TileSize - IconSize) * 0.5f;
    private const float ButtonGap = 6f;
    private const float ColumnGap = 8f;
    private const float Padding = 10f;
    private static readonly Vector4 LockOverlayColor = new(0f, 0f, 0f, 0.55f);
    private float _scrollOffsetY;
    private BuildMapEntryView? _hoveredEntry;

    /// <inheritdoc/>
    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        renderer.DrawRect(position, size, BackgroundColor);
        renderer.DrawRectOutline(position, size, new Vector4(0.45f, 0.55f, 0.75f, 1f));

        float headerBottom = position.Y + Padding + TitleHeight + FontSize + 8f;
        float contentTop = headerBottom;
        float contentBottom = position.Y + size.Y - Padding;
        float x = position.X + Padding;
        float innerW = size.X - Padding * 2f;
        var tileSize = new Vector2(TileSize, TileSize);
        var iconTile = new Vector2(IconSize, IconSize);

        DrawFittedLine(renderer, "Build Structures", new Vector2(x, position.Y + Padding),
            FontSize + 4f, innerW, HeaderColor);
        DrawFittedLine(renderer, "Press B or click X to close",
            new Vector2(x, position.Y + Padding + TitleHeight),
            FontSize - 1f, innerW, new Vector4(0.55f, 0.6f, 0.7f, 1f));

        float y = contentTop - _scrollOffsetY;

        foreach (var category in Categories)
        {
            if (y + CategoryHeaderHeight > contentTop && y < contentBottom)
            {
                DrawFittedLine(renderer, category.DisplayName, new Vector2(x, y),
                    FontSize + 1f, innerW, CategoryColor);
            }
            y += CategoryHeaderHeight;

            int column = 0;
            float rowY = y;
            float maxRowY = y;

            foreach (var entry in category.Buildings)
            {
                float btnX = x + column * (TileSize + ColumnGap);
                var btnPos = new Vector2(btnX, rowY);

                if (rowY + TileSize > contentTop && rowY < contentBottom)
                {
                    Vector4 color = !entry.IsUnlocked
                        ? LockedColor
                        : entry.CanAfford ? EnabledColor : UnaffordableColor;

                    renderer.DrawRect(btnPos, tileSize, color);
                    renderer.DrawRectOutline(btnPos, tileSize, new Vector4(0.5f, 0.55f, 0.65f, 0.6f));

                    var iconPos = btnPos + new Vector2(IconPadding, IconPadding);
                    var iconDesc = ResolveIcon(entry);
                    BuildIconDrawing.Draw(renderer, iconDesc, iconPos, IconSize);

                    if (!entry.IsUnlocked)
                        DrawLockOverlay(renderer, iconPos, iconTile);
                }

                maxRowY = MathF.Max(maxRowY, rowY + TileSize);
                column++;
                if (column >= 2)
                {
                    column = 0;
                    rowY += TileSize + ButtonGap;
                }
            }

            y = (column == 0 ? maxRowY : rowY + TileSize) + ButtonGap + 6f;
        }

        var closePos = new Vector2(position.X + size.X - Padding - 48f, position.Y + Padding);
        renderer.DrawRect(closePos, new Vector2(48f, 22f), new Vector4(0.35f, 0.15f, 0.15f, 1f));
        renderer.DrawText("X", closePos + new Vector2(18f, 3f), FontSize + 2,
            new Vector4(1f, 0.85f, 0.85f, 1f));
    }

    /// <inheritdoc/>
    public override TooltipContent? GetTooltipContent()
    {
        if (!Visible || _hoveredEntry == null)
            return null;

        return TooltipContent.FromBuildEntry(_hoveredEntry);
    }

    /// <inheritdoc/>
    public override void UpdatePointerState(
        Vector2 pointerPosition, bool isPointerDown,
        Vector2 containerPosition, Vector2 containerSize)
    {
        if (!Visible)
        {
            _hoveredEntry = null;
            return;
        }

        var (pos, size) = Resolve(containerPosition, containerSize);
        if (!Contains(pointerPosition, containerPosition, containerSize))
        {
            _hoveredEntry = null;
            return;
        }

        Vector2 local = pointerPosition - pos;
        _hoveredEntry = HitTestEntry(local, size);
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
        float viewportHeight = size.Y - (Padding + TitleHeight + FontSize + 8f + Padding);
        float maxScroll = Math.Max(0f, contentHeight - viewportHeight);
        if (maxScroll <= 0f) return false;

        _scrollOffsetY = Math.Clamp(_scrollOffsetY + (deltaY > 0f ? 48f : -48f), 0f, maxScroll);
        return true;
    }

    private float MeasureContentHeight(float panelWidth)
    {
        _ = panelWidth;
        float y = 0f;
        foreach (var category in Categories)
        {
            y += CategoryHeaderHeight;
            int count = category.Buildings.Count;
            int rows = Math.Max(1, (count + 1) / 2);
            y += rows * (TileSize + ButtonGap) + 6f;
        }

        return y;
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

        Vector2 local = screenPoint - pos;

        var closePos = new Vector2(size.X - Padding - 48f, Padding);
        if (local.X >= closePos.X && local.X < closePos.X + 48f &&
            local.Y >= closePos.Y && local.Y < closePos.Y + 22f)
        {
            CloseRequested?.Invoke();
            return true;
        }

        BuildMapEntryView? hit = HitTestEntry(local, size);
        if (hit != null)
        {
            if (hit.IsSelectable)
                BuildingSelected?.Invoke(hit.Id);
            return true;
        }

        return true;
    }

    private BuildMapEntryView? HitTestEntry(Vector2 local, Vector2 size)
    {
        float y = Padding + TitleHeight + FontSize + 8f - _scrollOffsetY;
        float x = Padding;

        foreach (var category in Categories)
        {
            y += CategoryHeaderHeight;
            int column = 0;
            float rowY = y;

            foreach (var entry in category.Buildings)
            {
                float btnX = x + column * (TileSize + ColumnGap);
                if (local.X >= btnX && local.X < btnX + TileSize &&
                    local.Y >= rowY && local.Y < rowY + TileSize)
                    return entry;

                column++;
                if (column >= 2)
                {
                    column = 0;
                    rowY += TileSize + ButtonGap;
                }
            }

            y = (column == 0 ? rowY : rowY + TileSize) + ButtonGap + 6f;
        }

        return null;
    }

    private static BuildIconDescriptor ResolveIcon(BuildMapEntryView entry) =>
        string.IsNullOrEmpty(entry.Icon.BuildingType)
            ? BuildIconCatalog.Get(entry.Id)
            : entry.Icon;

    private static void DrawLockOverlay(IUIRenderer renderer, Vector2 iconPos, Vector2 iconTile)
    {
        renderer.DrawRect(iconPos, iconTile, LockOverlayColor);

        const float lockFontSize = 9f;
        const string lockLabel = "LOCK";
        float labelW = UIFontMetrics.MeasureTextWidth(lockLabel, lockFontSize);
        var labelPos = iconPos + new Vector2(
            (iconTile.X - labelW) * 0.5f,
            (iconTile.Y - lockFontSize) * 0.5f);
        renderer.DrawText(lockLabel, labelPos, lockFontSize, new Vector4(0.92f, 0.88f, 0.82f, 1f));
    }

    private static void DrawFittedLine(
        IUIRenderer renderer, string text, Vector2 position,
        float preferredSize, float maxWidth, Vector4 color, float minSize = 8f)
    {
        if (string.IsNullOrEmpty(text)) return;

        float size = UIFontMetrics.FitFontSize(text, preferredSize, maxWidth, minSize);
        string fitted = UITextDrawing.TruncateWithEllipsis(text, maxWidth, size);
        renderer.DrawText(fitted, position, size, color);
    }
}