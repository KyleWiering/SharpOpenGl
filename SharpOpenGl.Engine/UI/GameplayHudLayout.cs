using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Engine.UI;

/// <summary>
/// Resolves gameplay HUD panel bounds and applies density-safe layout at the 1024×768 default window.
/// All coordinates are logical (1920×1080 reference space).
/// </summary>
public static class GameplayHudLayout
{
    public const float ResourceBarHeight = 40f;
    public const float ResourceBarFontSize = 16f;

    public const float BuildMapPanelWidth = 420f;
    public const float BuildMapPanelHeight = 520f;
    public const float BuildMapPanelPositionX = 12f;

    public const float UnitInfoStandardWidth = 560f;
    public const float UnitInfoStandardHeight = 160f;
    public const float UnitInfoCompactWidth = 480f;
    public const float UnitInfoCompactHeight = 140f;
    public const float UnitInfoOffsetX = -280f;
    public const float UnitInfoOffsetY = -164f;

    public const float BuildPanelWidth = 258f;
    public const float BuildPanelHeight = 500f;
    public const float BuildPanelOffsetX = -288f;
    public const float BuildPanelOffsetY = 56f;

    public const float PanelGap = 8f;

    /// <summary>Axis-aligned HUD panel bounds in logical coordinates.</summary>
    public readonly record struct PanelRect(float Left, float Top, float Right, float Bottom)
    {
        public float Width => Right - Left;
        public float Height => Bottom - Top;

        public bool Overlaps(PanelRect other) =>
            Left < other.Right - 0.5f
            && Right > other.Left + 0.5f
            && Top < other.Bottom - 0.5f
            && Bottom > other.Top + 0.5f;
    }

    /// <summary>
    /// Tighten ResourceBar, clamp BuildMapPanel below the minimap band, and compact UnitInfoPanel when the build map is open.
    /// </summary>
    public static void ApplyDensityLayout(GameplayHUD hud)
    {
        Vector2 viewport = UIScaler.ReferenceSize;

        hud.ResourceBar.Size = new Vector2(viewport.X, ResourceBarHeight);
        hud.ResourceBar.FontSize = ResourceBarFontSize;

        float buildMapHeight = ComputeBuildMapHeight(ResolveTopY(hud.Minimap, viewport));
        hud.BuildMapPanel.Size = new Vector2(BuildMapPanelWidth, buildMapHeight);
        hud.BuildMapPanel.Position = new Vector2(
            BuildMapPanelPositionX,
            ComputeBuildMapOffsetY(buildMapHeight));

        bool buildMapOpen = hud.BuildMapPanel.Visible;
        if (buildMapOpen)
        {
            hud.UnitInfoPanel.Size = new Vector2(UnitInfoCompactWidth, UnitInfoCompactHeight);
            hud.UnitInfoPanel.FontSize = 16f;
        }
        else
        {
            hud.UnitInfoPanel.Size = new Vector2(UnitInfoStandardWidth, UnitInfoStandardHeight);
            hud.UnitInfoPanel.FontSize = 17f;
        }

        hud.UnitInfoPanel.Position = new Vector2(UnitInfoOffsetX, UnitInfoOffsetY);

        hud.BuildPanel.Size = new Vector2(BuildPanelWidth, BuildPanelHeight);
        hud.BuildPanel.Position = new Vector2(BuildPanelOffsetX, BuildPanelOffsetY);

        var (buildPanelPos, buildPanelSize) = hud.BuildPanel.Resolve(Vector2.Zero, viewport);
        float buildPanelRight = buildPanelPos.X + buildPanelSize.X;
        float maxRight = viewport.X - PanelGap;
        if (buildPanelRight > maxRight)
        {
            float shift = buildPanelRight - maxRight;
            hud.BuildPanel.Position = new Vector2(
                hud.BuildPanel.Position.X - shift,
                hud.BuildPanel.Position.Y);
        }
    }

    /// <summary>Resolve widget bounds for overlap audits.</summary>
    public static PanelRect GetBounds(Widget widget, Vector2 viewport) =>
        ToRect(widget.Resolve(Vector2.Zero, viewport));

    /// <summary>Return true when any pair of visible panels overlap.</summary>
    public static bool AnyOverlap(IReadOnlyList<PanelRect> panels)
    {
        for (int i = 0; i < panels.Count; i++)
        {
            for (int j = i + 1; j < panels.Count; j++)
            {
                if (panels[i].Overlaps(panels[j]))
                    return true;
            }
        }

        return false;
    }

    internal static float ComputeBuildMapHeight(float minimapTopY)
    {
        float contentTop = ResourceBarHeight + PanelGap;
        float maxBottom = minimapTopY - PanelGap;
        float maxHeight = MathF.Max(360f, maxBottom - contentTop);
        return MathF.Min(BuildMapPanelHeight, maxHeight);
    }

    internal static float ComputeBuildMapOffsetY(float panelHeight)
    {
        float contentTop = ResourceBarHeight + PanelGap;
        float originY = (UIScaler.ReferenceSize.Y - panelHeight) * 0.5f;
        return contentTop - originY;
    }

    private static float ResolveTopY(Widget widget, Vector2 viewport)
    {
        var (pos, _) = widget.Resolve(Vector2.Zero, viewport);
        return pos.Y;
    }

    private static PanelRect ToRect((Vector2 Position, Vector2 Size) resolved) =>
        new(
            resolved.Position.X,
            resolved.Position.Y,
            resolved.Position.X + resolved.Size.X,
            resolved.Position.Y + resolved.Size.Y);
}