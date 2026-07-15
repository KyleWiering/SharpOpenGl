using OpenTK.Mathematics;
using SharpOpenGl.Engine.Build;
using SharpOpenGl.Engine.UI;

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
    public Vector4 EnabledColor { get; set; } = new(0.16f, 0.38f, 0.2f, 1f);
    public Vector4 LockedColor { get; set; } = new(0.2f, 0.17f, 0.17f, 1f);
    public Vector4 UnaffordableColor { get; set; } = new(0.38f, 0.14f, 0.14f, 1f);
    public Vector4 HoverOutlineColor { get; set; } = new(0.55f, 0.85f, 1f, 1f);

    private const float FontSize = 12f;
    private const float TitleHeight = 28f;
    private const float CategoryHeaderHeight = 20f;
    private const float CategoryLockedHintHeight = 14f;
    private const float CategoryLockedHintGap = 2f;
    private const float TileSize = 64f;
    private const float LockedMicroLabelBandHeight = 14f;
    private const float LockedMicroLabelFontSize = 9f;
    private const float LockedMicroLabelMinFontSize = 8f;
    private const int LockedMicroLabelMaxNameChars = 10;
    private const float TierBadgeWidth = 22f;
    private const float TierBadgeHeight = 12f;
    private const float TierBadgeFontSize = 8f;
    private const float IconSize = 52f;
    private const float IconPadding = (TileSize - IconSize) * 0.5f;
    private const float ButtonGap = 6f;
    private const float ColumnGap = 8f;
    private const float Padding = 10f;
    private const float TileHitPadding = 2f;
    private const float CloseButtonWidth = 48f;
    private const float CloseButtonVisualHeight = 22f;
    private const float CloseButtonHitHeight = 44f;
    private static readonly Vector4 LockOverlayColor = new(0f, 0f, 0f, 0.55f);
    private static readonly Vector4 LockedMicroLabelColor = new(0.82f, 0.78f, 0.72f, 1f);
    private static readonly Vector4 TierBadgeBackgroundColor = new(0.08f, 0.1f, 0.16f, 0.92f);
    private static readonly Vector4 TierBadgeTextColor = new(0.72f, 0.82f, 0.95f, 1f);
    private static readonly Vector4 CapstoneBadgeTextColor = new(0.95f, 0.82f, 0.45f, 1f);
    private static readonly Vector4 CategoryLockedHintColor = new(0.45f, 0.5f, 0.58f, 1f);
    private const string CategoryLockedHintText = "Build prerequisites in earlier tiers";
    private const string PanelSubtitleText = "Pick a structure icon to place · hover for name and cost";
    private const string PanelShortcutHintText = "Press B or HUD Build · click X to close";
    private const float CompactPanelHeightThreshold = 560f;
    private float _scrollOffsetY;
    private BuildMapEntryView? _hoveredEntry;

    /// <inheritdoc/>
    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        renderer.DrawRect(position, size, BackgroundColor);
        renderer.DrawRectOutline(position, size, new Vector4(0.45f, 0.55f, 0.75f, 1f));

        bool compactHeader = IsCompactHeader(size);
        float headerBottom = position.Y + GetHeaderBlockHeight(compactHeader);
        float contentTop = headerBottom;
        float contentBottom = position.Y + size.Y - Padding;
        float x = position.X + Padding;
        float innerW = size.X - Padding * 2f;
        var tileSize = new Vector2(TileSize, TileSize);
        var iconTile = new Vector2(IconSize, IconSize);

        DrawFittedLine(renderer, "Build Structures", new Vector2(x, position.Y + Padding),
            FontSize + 4f, innerW, HeaderColor);
        DrawFittedLine(renderer, PanelSubtitleText,
            new Vector2(x, position.Y + Padding + TitleHeight),
            FontSize - 1f, innerW, new Vector4(0.55f, 0.6f, 0.7f, 1f));
        if (!compactHeader)
        {
            DrawFittedLine(renderer, PanelShortcutHintText,
                new Vector2(x, position.Y + Padding + TitleHeight + FontSize + 2f),
                FontSize - 2f, innerW, new Vector4(0.48f, 0.52f, 0.62f, 1f), minSize: 8f);
        }

        float y = contentTop - _scrollOffsetY;

        foreach (var category in Categories)
        {
            int tierIndex = ResolveTierIndex(category);
            int unlockedCount = category.Buildings.Count(static entry => entry.IsUnlocked);
            int totalCount = category.Buildings.Count;
            string headerText = FormatCategoryHeader(category.DisplayName, tierIndex, unlockedCount, totalCount);
            Vector4 headerColor = ResolveCategoryHeaderColor(tierIndex, unlockedCount);
            bool showLockedHint = unlockedCount == 0 && tierIndex > 1;
            float headerBlockHeight = GetCategoryHeaderBlockHeight(showLockedHint);

            if (y + headerBlockHeight > contentTop && y < contentBottom)
            {
                DrawFittedLine(renderer, headerText, new Vector2(x, y),
                    FontSize + 1f, innerW, headerColor);

                if (showLockedHint)
                {
                    float hintY = y + CategoryHeaderHeight + CategoryLockedHintGap;
                    DrawFittedLine(renderer, CategoryLockedHintText, new Vector2(x, hintY),
                        FontSize - 2f, innerW, CategoryLockedHintColor, minSize: 7f);
                }

                float dividerY = y + headerBlockHeight - 2f;
                renderer.DrawRect(
                    new Vector2(x, dividerY),
                    new Vector2(innerW, 1f),
                    new Vector4(0.35f, 0.42f, 0.55f, 0.55f));
            }
            y += headerBlockHeight;

            int column = 0;
            float rowY = y;
            float maxRowY = y;

            foreach (var entry in category.Buildings)
            {
                float btnX = x + column * (TileSize + ColumnGap);
                var btnPos = new Vector2(btnX, rowY);
                int entryTierIndex = ResolveEntryTierIndex(category, entry);

                if (rowY + TileSize > contentTop && rowY < contentBottom)
                {
                    Vector4 color = !entry.IsUnlocked
                        ? LockedColor
                        : entry.CanAfford ? EnabledColor : UnaffordableColor;

                    renderer.DrawRect(btnPos, tileSize, color);
                    DrawTierBadge(renderer, btnPos, entryTierIndex, entry.CategoryId);
                    bool isHovered = ReferenceEquals(_hoveredEntry, entry);
                    Vector4 outlineColor = isHovered
                        ? HoverOutlineColor
                        : new Vector4(0.5f, 0.55f, 0.65f, 0.6f);
                    renderer.DrawRectOutline(btnPos, tileSize, outlineColor);
                    if (isHovered)
                    {
                        var ringPos = btnPos - new Vector2(2f, 2f);
                        var ringSize = tileSize + new Vector2(4f, 4f);
                        renderer.DrawRectOutline(ringPos, ringSize, HoverOutlineColor);
                    }

                    var iconPos = btnPos + new Vector2(IconPadding, IconPadding);
                    var iconDesc = ResolveIcon(entry);
                    BuildIconDrawing.Draw(renderer, iconDesc, iconPos, IconSize);

                    if (!entry.IsUnlocked)
                    {
                        DrawLockOverlay(renderer, iconPos, iconTile);
                        DrawPrerequisiteMicroLabel(renderer, btnPos, entry);
                    }
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

        var closePos = new Vector2(position.X + size.X - Padding - CloseButtonWidth, position.Y + Padding);
        renderer.DrawRect(closePos, new Vector2(CloseButtonWidth, CloseButtonVisualHeight), new Vector4(0.35f, 0.15f, 0.15f, 1f));
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
        float viewportHeight = size.Y - (GetHeaderBlockHeight(IsCompactHeader(size)) + Padding);
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
            int tierIndex = ResolveTierIndex(category);
            int unlockedCount = category.Buildings.Count(static entry => entry.IsUnlocked);
            bool showLockedHint = unlockedCount == 0 && tierIndex > 1;
            y += GetCategoryHeaderBlockHeight(showLockedHint);
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

        if (TryHitCloseButton(local, size))
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
        float y = GetHeaderBlockHeight(IsCompactHeader(size)) - _scrollOffsetY;
        float x = Padding;

        foreach (var category in Categories)
        {
            int tierIndex = ResolveTierIndex(category);
            int unlockedCount = category.Buildings.Count(static entry => entry.IsUnlocked);
            bool showLockedHint = unlockedCount == 0 && tierIndex > 1;
            y += GetCategoryHeaderBlockHeight(showLockedHint);
            int column = 0;
            float rowY = y;

            foreach (var entry in category.Buildings)
            {
                float btnX = x + column * (TileSize + ColumnGap);
                if (PointInExpandedTile(btnX, rowY, local))
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

    private static bool PointInExpandedTile(float tileX, float tileY, Vector2 local)
    {
        float hitX = tileX - TileHitPadding;
        float hitY = tileY - TileHitPadding;
        float hitSize = TileSize + TileHitPadding * 2f;
        return local.X >= hitX && local.X < hitX + hitSize
            && local.Y >= hitY && local.Y < hitY + hitSize;
    }

    private static bool TryHitCloseButton(Vector2 local, Vector2 panelSize)
    {
        var closePos = GetCloseButtonVisualOrigin(panelSize);
        float hitY = closePos.Y - (CloseButtonHitHeight - CloseButtonVisualHeight) * 0.5f;
        return local.X >= closePos.X && local.X < closePos.X + CloseButtonWidth
            && local.Y >= hitY && local.Y < hitY + CloseButtonHitHeight;
    }

    private static Vector2 GetCloseButtonVisualOrigin(Vector2 panelSize) =>
        new(panelSize.X - Padding - CloseButtonWidth, Padding);

    private static BuildIconDescriptor ResolveIcon(BuildMapEntryView entry) =>
        string.IsNullOrEmpty(entry.Icon.BuildingType)
            ? BuildIconCatalog.Get(entry.Id)
            : entry.Icon;

    private static void DrawLockOverlay(IUIRenderer renderer, Vector2 iconPos, Vector2 iconTile)
    {
        float overlayHeight = iconTile.Y - LockedMicroLabelBandHeight;
        var overlaySize = new Vector2(iconTile.X, overlayHeight);
        renderer.DrawRect(iconPos, overlaySize, LockOverlayColor);

        const float lockFontSize = 9f;
        const string lockLabel = "LOCK";
        float labelW = UIFontMetrics.MeasureTextWidth(lockLabel, lockFontSize);
        var labelPos = iconPos + new Vector2(
            (iconTile.X - labelW) * 0.5f,
            (overlayHeight - lockFontSize) * 0.5f);
        renderer.DrawText(lockLabel, labelPos, lockFontSize, new Vector4(0.92f, 0.88f, 0.82f, 1f));
    }

    private static void DrawTierBadge(
        IUIRenderer renderer, Vector2 tilePos, int tierIndex, string categoryId)
    {
        string badge = FormatTierBadge(tierIndex, categoryId);
        var badgePos = tilePos + new Vector2(2f, 2f);
        var badgeSize = new Vector2(TierBadgeWidth, TierBadgeHeight);
        renderer.DrawRect(badgePos, badgeSize, TierBadgeBackgroundColor);
        renderer.DrawRectOutline(badgePos, badgeSize, new Vector4(0.45f, 0.55f, 0.7f, 0.75f));

        float labelW = UIFontMetrics.MeasureTextWidth(badge, TierBadgeFontSize);
        var textPos = badgePos + new Vector2(
            MathF.Max(1f, (badgeSize.X - labelW) * 0.5f),
            (badgeSize.Y - TierBadgeFontSize) * 0.5f);
        Vector4 textColor = string.Equals(categoryId, "capstone", StringComparison.OrdinalIgnoreCase)
            ? CapstoneBadgeTextColor
            : TierBadgeTextColor;
        renderer.DrawText(badge, textPos, TierBadgeFontSize, textColor);
    }

    private static void DrawPrerequisiteMicroLabel(
        IUIRenderer renderer, Vector2 tilePos, BuildMapEntryView entry)
    {
        string? label = FormatPrerequisiteMicroLabel(entry);
        if (label == null)
            return;

        var bandPos = tilePos + new Vector2(0f, TileSize - LockedMicroLabelBandHeight);
        var bandSize = new Vector2(TileSize, LockedMicroLabelBandHeight);
        renderer.DrawRect(bandPos, bandSize, new Vector4(0.04f, 0.05f, 0.08f, 0.92f));

        float maxLabelWidth = TileSize - 4f;
        float fittedSize = UIFontMetrics.FitFontSize(
            label, LockedMicroLabelFontSize, maxLabelWidth, LockedMicroLabelMinFontSize);
        string fitted = UITextDrawing.TruncateWithEllipsis(label, maxLabelWidth, fittedSize);
        float labelW = UIFontMetrics.MeasureTextWidth(fitted, fittedSize);
        var textPos = bandPos + new Vector2(
            MathF.Max(2f, (bandSize.X - labelW) * 0.5f),
            (bandSize.Y - fittedSize) * 0.5f);
        renderer.DrawText(fitted, textPos, fittedSize, LockedMicroLabelColor);
    }

    internal static string FormatCategoryHeader(
        string displayName, int tierIndex, int unlockedCount, int totalCount) =>
        $"{displayName} — Tier {tierIndex} ({unlockedCount}/{totalCount})";

    internal static string FormatTierBadge(int tierIndex, string categoryId)
    {
        if (string.Equals(categoryId, "capstone", StringComparison.OrdinalIgnoreCase))
            return "Cap";

        return tierIndex switch
        {
            <= 1 => "T1",
            2 => "T2",
            3 => "T3",
            _ => "T3",
        };
    }

    private int ResolveEntryTierIndex(BuildMapCategoryView category, BuildMapEntryView entry)
    {
        if (category.TierIndex > 0)
            return category.TierIndex;

        return ResolveTierIndex(category);
    }

    private int ResolveTierIndex(BuildMapCategoryView category)
    {
        if (category.TierIndex > 0)
            return category.TierIndex;

        int index = 0;
        foreach (var listed in Categories)
        {
            index++;
            if (ReferenceEquals(listed, category))
                return index;
        }

        return 1;
    }

    private static float GetCategoryHeaderBlockHeight(bool showLockedHint) =>
        CategoryHeaderHeight
        + (showLockedHint ? CategoryLockedHintGap + CategoryLockedHintHeight : 0f);

    private Vector4 ResolveCategoryHeaderColor(int tierIndex, int unlockedCount)
    {
        if (unlockedCount == 0 && tierIndex > 1)
            return MuteColor(CategoryColor, 0.62f);
        return CategoryColor;
    }

    private static Vector4 MuteColor(Vector4 color, float factor) =>
        new(color.X * factor, color.Y * factor, color.Z * factor, color.W);

    internal static string? FormatPrerequisiteMicroLabel(BuildMapEntryView entry)
    {
        if (entry.IsUnlocked)
            return null;

        bool isCapstone = string.Equals(entry.CategoryId, "capstone", StringComparison.OrdinalIgnoreCase);
        bool showProgress = isCapstone
            || entry.PrerequisiteTotalCount > 1
            || entry.PrerequisiteMetCount > 0;

        string? progress = null;
        if (showProgress && entry.PrerequisiteTotalCount > 0)
            progress = $"{entry.PrerequisiteMetCount}/{entry.PrerequisiteTotalCount}";

        string? missing = ExtractFirstMissingPrerequisite(entry);
        if (!string.IsNullOrWhiteSpace(missing) && missing.Length > LockedMicroLabelMaxNameChars)
            missing = missing[..LockedMicroLabelMaxNameChars] + "…";

        if (!string.IsNullOrWhiteSpace(progress) && !string.IsNullOrWhiteSpace(missing))
            return $"{progress} · Needs {missing}";

        if (!string.IsNullOrWhiteSpace(progress))
            return progress;

        if (!string.IsNullOrWhiteSpace(missing))
            return $"Needs: {missing}";

        return null;
    }

    private static string? ExtractFirstMissingPrerequisite(BuildMapEntryView entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.LockReason)
            && entry.LockReason.StartsWith("Requires: ", StringComparison.Ordinal))
        {
            string remainder = entry.LockReason["Requires: ".Length..].Trim();
            int comma = remainder.IndexOf(',');
            return comma >= 0 ? remainder[..comma].Trim() : remainder;
        }

        return entry.Prerequisites.Count > 0 ? entry.Prerequisites[0] : null;
    }

    internal static bool IsCompactHeader(Vector2 panelSize) =>
        panelSize.Y < CompactPanelHeightThreshold;

    internal static float GetHeaderBlockHeight(bool compactHeader) =>
        Padding
        + TitleHeight
        + FontSize
        + (compactHeader ? 6f : FontSize + 12f);

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