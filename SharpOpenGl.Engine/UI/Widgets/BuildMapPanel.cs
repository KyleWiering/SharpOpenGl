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
    private const float ButtonHeight = 64f;
    private const float ButtonGap = 6f;
    private const float ColumnGap = 8f;
    private const float Padding = 10f;

    /// <inheritdoc/>
    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        renderer.DrawRect(position, size, BackgroundColor);
        renderer.DrawRectOutline(position, size, new Vector4(0.45f, 0.55f, 0.75f, 1f));

        float y = position.Y + Padding;
        float x = position.X + Padding;
        float innerW = size.X - Padding * 2f;

        renderer.DrawText("Build Structures", new Vector2(x, y), FontSize + 4, HeaderColor);
        y += TitleHeight;

        renderer.DrawText("Press B or click X to close", new Vector2(x, y), FontSize - 1,
            new Vector4(0.55f, 0.6f, 0.7f, 1f));
        y += FontSize + 8f;

        float btnW = (innerW - ColumnGap) * 0.5f;

        foreach (var category in Categories)
        {
            renderer.DrawText(category.DisplayName, new Vector2(x, y), FontSize + 1, CategoryColor);
            y += CategoryHeaderHeight;

            int column = 0;
            float rowY = y;
            float maxRowY = y;

            foreach (var entry in category.Buildings)
            {
                float btnX = x + column * (btnW + ColumnGap);
                var btnPos = new Vector2(btnX, rowY);
                var btnSize = new Vector2(btnW, ButtonHeight);

                Vector4 color = !entry.IsUnlocked
                    ? LockedColor
                    : entry.CanAfford ? EnabledColor : UnaffordableColor;

                renderer.DrawRect(btnPos, btnSize, color);
                renderer.DrawRectOutline(btnPos, btnSize, new Vector4(0.5f, 0.55f, 0.65f, 0.6f));

                string state = !entry.IsUnlocked ? "LOCKED"
                    : entry.CanAfford ? "READY" : "COST";
                renderer.DrawText(entry.Name, btnPos + new Vector2(4f, 4f), FontSize,
                    new Vector4(0.95f, 0.95f, 0.95f, 1f));
                renderer.DrawText($"{entry.FootprintCols}x{entry.FootprintRows}  {state}",
                    btnPos + new Vector2(4f, 20f), FontSize - 1,
                    new Vector4(0.75f, 0.8f, 0.9f, 1f));
                renderer.DrawText(
                    $"E:{entry.EnergyCost} M:{entry.MineralsCost} D:{entry.DataCost} C:{entry.CrewCost}",
                    btnPos + new Vector2(4f, 36f), FontSize - 2,
                    new Vector4(0.65f, 0.7f, 0.8f, 1f));

                maxRowY = MathF.Max(maxRowY, rowY + ButtonHeight);
                column++;
                if (column >= 2)
                {
                    column = 0;
                    rowY += ButtonHeight + ButtonGap;
                }
            }

            y = (column == 0 ? maxRowY : rowY + ButtonHeight) + ButtonGap + 6f;
        }

        var closePos = new Vector2(position.X + size.X - Padding - 48f, position.Y + Padding);
        renderer.DrawRect(closePos, new Vector2(48f, 22f), new Vector4(0.35f, 0.15f, 0.15f, 1f));
        renderer.DrawText("X", closePos + new Vector2(18f, 3f), FontSize + 2,
            new Vector4(1f, 0.85f, 0.85f, 1f));
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

        float y = Padding + TitleHeight + FontSize + 8f;
        float x = Padding;
        float innerW = size.X - Padding * 2f;
        float btnW = (innerW - ColumnGap) * 0.5f;

        foreach (var category in Categories)
        {
            y += CategoryHeaderHeight;
            int column = 0;
            float rowY = y;

            foreach (var entry in category.Buildings)
            {
                float btnX = x + column * (btnW + ColumnGap);
                if (local.X >= btnX && local.X < btnX + btnW &&
                    local.Y >= rowY && local.Y < rowY + ButtonHeight)
                {
                    if (entry.IsSelectable)
                        BuildingSelected?.Invoke(entry.Id);
                    return true;
                }

                column++;
                if (column >= 2)
                {
                    column = 0;
                    rowY += ButtonHeight + ButtonGap;
                }
            }

            y = (column == 0 ? rowY : rowY + ButtonHeight) + ButtonGap + 6f;
        }

        return true;
    }
}