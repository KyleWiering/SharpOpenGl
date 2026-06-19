using OpenTK.Mathematics;
using SharpOpenGl.Engine.Grid;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>
/// Represents a unit dot rendered on the minimap.
/// </summary>
public readonly record struct MinimapUnit(
    Vector2 NormalizedPosition,
    Vector4 Color,
    bool IsFriendly
);

/// <summary>
/// Mission waypoint marker on the minimap.
/// </summary>
public readonly record struct MinimapMarker(
    Vector2 NormalizedPosition,
    Vector4 Color
);

/// <summary>
/// Mini-map widget that renders fog-of-war, units, and mission markers.
/// </summary>
public sealed class Minimap : Widget
{
    public FogOfWar? FogOfWar { get; set; }
    public int PlayerId { get; set; }
    public Vector2i GridSize { get; set; } = new(32, 32);

    /// <summary>
    /// Display resolution for fog tiles. Defaults to <see cref="GridSize"/> when zero.
    /// </summary>
    public Vector2i FogDisplayCells { get; set; }

    public IReadOnlyList<MinimapUnit> Units { get; set; } = Array.Empty<MinimapUnit>();
    public IReadOnlyList<MinimapMarker> ObjectiveMarkers { get; set; } = Array.Empty<MinimapMarker>();
    public (Vector2 Min, Vector2 Max) CameraViewport { get; set; } =
        (new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.75f));

    public Vector4 BackgroundColor { get; set; } = new Vector4(0.02f, 0.02f, 0.05f, 1f);
    public Vector4 UnexploredColor { get; set; } = new Vector4(0.04f, 0.04f, 0.08f, 1f);
    public Vector4 ExploredColor { get; set; } = new Vector4(0.15f, 0.15f, 0.25f, 1f);
    public Vector4 VisibleColor { get; set; } = new Vector4(0.25f, 0.25f, 0.4f, 1f);
    public Vector4 BorderColor { get; set; } = new Vector4(0.5f, 0.5f, 0.7f, 1f);
    public Vector4 CameraBoxColor { get; set; } = new Vector4(1f, 1f, 1f, 0.6f);

    /// <summary>Raised when the player clicks the minimap. Argument is normalised 0..1.</summary>
    public event Action<Vector2>? Clicked;

    /// <summary>
    /// Returns normalised click position when <paramref name="screenPoint"/> hits the minimap.
    /// </summary>
    public bool TryGetClick(Vector2 screenPoint, Vector2 containerPosition, Vector2 containerSize,
        out Vector2 normalizedPosition)
    {
        normalizedPosition = Vector2.Zero;
        if (!Visible) return false;

        var (pos, size) = Resolve(containerPosition, containerSize);
        if (screenPoint.X < pos.X || screenPoint.X >= pos.X + size.X
            || screenPoint.Y < pos.Y || screenPoint.Y >= pos.Y + size.Y)
            return false;

        normalizedPosition = Vector2.Clamp((screenPoint - pos) / size, Vector2.Zero, Vector2.One);
        return true;
    }

    /// <inheritdoc/>
    public override bool HandlePointerTapped(
        Vector2 screenPoint, int button,
        Vector2 containerPosition, Vector2 containerSize)
    {
        if (!TryGetClick(screenPoint, containerPosition, containerSize, out Vector2 norm))
            return false;

        Clicked?.Invoke(norm);
        return true;
    }

    /// <inheritdoc/>
    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        renderer.DrawRect(position, size, BackgroundColor);

        if (FogOfWar != null)
        {
            DrawFog(renderer, position, size);
        }
        else
        {
            renderer.DrawRect(position, size, VisibleColor);
        }

        float markerSize = Math.Max(6f, Math.Min(size.X, size.Y) * 0.045f);
        foreach (MinimapMarker marker in ObjectiveMarkers)
        {
            Vector2 markerPos = new(
                position.X + marker.NormalizedPosition.X * size.X - markerSize / 2f,
                position.Y + marker.NormalizedPosition.Y * size.Y - markerSize / 2f);
            renderer.DrawRect(markerPos, new Vector2(markerSize, markerSize), marker.Color);
            renderer.DrawRectOutline(markerPos, new Vector2(markerSize, markerSize),
                new Vector4(1f, 0.9f, 0.3f, 1f));
        }

        float dotSize = Math.Max(4f, Math.Min(size.X, size.Y) * 0.025f);
        foreach (MinimapUnit unit in Units)
        {
            Vector2 dotPos = new(
                position.X + unit.NormalizedPosition.X * size.X - dotSize / 2f,
                position.Y + unit.NormalizedPosition.Y * size.Y - dotSize / 2f);
            renderer.DrawRect(dotPos, new Vector2(dotSize, dotSize), unit.Color);
        }

        var (camMin, camMax) = CameraViewport;
        Vector2 boxPos = position + camMin * size;
        Vector2 boxSize = (camMax - camMin) * size;
        renderer.DrawRectOutline(boxPos, boxSize, CameraBoxColor);
        renderer.DrawRectOutline(position, size, BorderColor);
    }

    private void DrawFog(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        Vector2i displayCells = FogDisplayCells;
        if (displayCells.X <= 0 || displayCells.Y <= 0)
            displayCells = GridSize;

        float cellW = size.X / displayCells.X;
        float cellH = size.Y / displayCells.Y;
        int sampleW = Math.Max(1, GridSize.X / displayCells.X);
        int sampleH = Math.Max(1, GridSize.Y / displayCells.Y);

        for (int row = 0; row < displayCells.Y; row++)
        {
            for (int col = 0; col < displayCells.X; col++)
            {
                int startX = col * sampleW;
                int startY = row * sampleH;
                FogState state = SampleFogState(startX, startY, sampleW, sampleH);

                Vector4 cellColor = state switch
                {
                    FogState.Visible => VisibleColor,
                    FogState.Explored => ExploredColor,
                    _ => UnexploredColor
                };

                renderer.DrawRect(
                    new Vector2(position.X + col * cellW, position.Y + row * cellH),
                    new Vector2(cellW + 0.5f, cellH + 0.5f),
                    cellColor);
            }
        }
    }

    private FogState SampleFogState(int startX, int startY, int sampleW, int sampleH)
    {
        bool anyVisible = false;
        bool anyExplored = false;

        int endX = Math.Min(GridSize.X, startX + sampleW);
        int endY = Math.Min(GridSize.Y, startY + sampleH);

        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                FogState state = FogOfWar!.GetState(PlayerId, x, y);
                if (state == FogState.Visible) anyVisible = true;
                else if (state == FogState.Explored) anyExplored = true;
            }
        }

        if (anyVisible) return FogState.Visible;
        if (anyExplored) return FogState.Explored;
        return FogState.Unexplored;
    }
}