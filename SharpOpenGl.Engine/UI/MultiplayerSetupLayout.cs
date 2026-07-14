using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.UI;

/// <summary>
/// Logical layout bands for the multiplayer setup screen at <see cref="UIScaler.ReferenceSize"/>.
/// Keeps the map picker, slot columns, and validation message separated at 1024×768 scale.
/// </summary>
public static class MultiplayerSetupLayout
{
    public const float RegionGap = 8f;

    public const float MapRowTop = 132f;
    public const float MapRowHeight = 52f;

    public const float ColumnWidth = 860f;
    public const float ColumnGap = 20f;
    public const float LeftColumnX = -ColumnWidth - ColumnGap;
    public const float RightColumnX = ColumnGap;

    public const float SlotRowStartY = 188f;
    public const float SlotRowHeight = 88f;
    public const int SlotRowsPerColumn = 4;

    public const float ValidationTop = 556f;
    public const float ValidationHeight = 48f;

    public const float StartButtonTop = 652f;
    public const float StartButtonHeight = 58f;

    /// <summary>Axis-aligned layout band in logical coordinates.</summary>
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

    public enum RegionKind
    {
        MapPicker,
        SlotGrid,
        Validation,
        StartActions,
    }

    /// <summary>Authoritative region bounds used by overlap audits.</summary>
    public static PanelRect GetRegionBounds(RegionKind region, Vector2 viewport)
    {
        float cx = viewport.X * 0.5f;

        return region switch
        {
            RegionKind.MapPicker => new PanelRect(
                cx - 420f,
                MapRowTop,
                cx + 420f,
                MapRowTop + MapRowHeight),
            RegionKind.SlotGrid => new PanelRect(
                cx + LeftColumnX,
                SlotRowStartY,
                cx + RightColumnX + ColumnWidth,
                SlotRowStartY + SlotRowsPerColumn * SlotRowHeight),
            RegionKind.Validation => new PanelRect(
                cx - 460f,
                ValidationTop,
                cx + 460f,
                ValidationTop + ValidationHeight),
            RegionKind.StartActions => new PanelRect(
                cx - 200f,
                StartButtonTop,
                cx + 200f,
                StartButtonTop + StartButtonHeight),
            _ => default,
        };
    }

    /// <summary>Return true when any pair of regions overlap.</summary>
    public static bool AnyOverlap(IReadOnlyList<PanelRect> regions)
    {
        for (int i = 0; i < regions.Count; i++)
        {
            for (int j = i + 1; j < regions.Count; j++)
            {
                if (regions[i].Overlaps(regions[j]))
                    return true;
            }
        }

        return false;
    }

    /// <summary>Resolve a widget to a tight axis-aligned rect.</summary>
    public static PanelRect ToRect((Vector2 Position, Vector2 Size) resolved) =>
        new(
            resolved.Position.X,
            resolved.Position.Y,
            resolved.Position.X + resolved.Size.X,
            resolved.Position.Y + resolved.Size.Y);
}