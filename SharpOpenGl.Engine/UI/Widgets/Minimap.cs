using OpenTK.Mathematics;
using SharpOpenGl.Engine.Grid;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>
/// Represents a unit dot rendered on the minimap.
/// </summary>
public readonly record struct MinimapUnit(
    Vector2 NormalizedPosition,   // 0..1 in both axes
    Vector4 Color,
    bool IsFriendly
);

/// <summary>
/// Mini-map widget that renders the fog-of-war grid and unit positions.
/// </summary>
/// <remarks>
/// Set <see cref="FogOfWar"/>, <see cref="GridSize"/>, and <see cref="Units"/>
/// each frame before <c>Draw</c> is called.
/// </remarks>
public sealed class Minimap : Widget
{
    // ── Data bindings ─────────────────────────────────────────────────────────

    /// <summary>Fog-of-war state.  May be <c>null</c> (draw all cells visible).</summary>
    public FogOfWar? FogOfWar { get; set; }

    /// <summary>Player ID used when sampling fog-of-war state.</summary>
    public int PlayerId { get; set; }

    /// <summary>Total grid dimensions (columns, rows).</summary>
    public Vector2i GridSize { get; set; } = new(32, 32);

    /// <summary>Unit dots to overlay on the map.</summary>
    public IReadOnlyList<MinimapUnit> Units { get; set; } = Array.Empty<MinimapUnit>();

    /// <summary>Camera's visible region as a normalised rect (0..1).</summary>
    public (Vector2 Min, Vector2 Max) CameraViewport { get; set; } =
        (new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.75f));

    // ── Visual config ─────────────────────────────────────────────────────────

    /// <summary>Background colour (represents unexplored space).</summary>
    public Vector4 BackgroundColor { get; set; } = new Vector4(0.02f, 0.02f, 0.05f, 1f);

    /// <summary>Colour of explored-but-not-visible cells.</summary>
    public Vector4 ExploredColor { get; set; } = new Vector4(0.15f, 0.15f, 0.25f, 1f);

    /// <summary>Colour of fully visible cells.</summary>
    public Vector4 VisibleColor { get; set; } = new Vector4(0.25f, 0.25f, 0.4f, 1f);

    /// <summary>Border colour of the minimap frame.</summary>
    public Vector4 BorderColor { get; set; } = new Vector4(0.5f, 0.5f, 0.7f, 1f);

    /// <summary>Camera viewport indicator colour.</summary>
    public Vector4 CameraBoxColor { get; set; } = new Vector4(1f, 1f, 1f, 0.6f);

    // ── Drawing ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        renderer.DrawRect(position, size, BackgroundColor);

        if (FogOfWar != null)
        {
            // Draw fog cells.  Batch into two passes for simplicity.
            float cellW = size.X / GridSize.X;
            float cellH = size.Y / GridSize.Y;

            for (int row = 0; row < GridSize.Y; row++)
            {
                for (int col = 0; col < GridSize.X; col++)
                {
                    FogState state = FogOfWar.GetState(PlayerId, col, row);
                    if (state == FogState.Unexplored) continue;

                    Vector4 cellColor = state == FogState.Visible
                        ? VisibleColor : ExploredColor;

                    renderer.DrawRect(
                        new Vector2(position.X + col * cellW, position.Y + row * cellH),
                        new Vector2(cellW, cellH),
                        cellColor);
                }
            }
        }
        else
        {
            // No fog — fill entire map as visible.
            renderer.DrawRect(position, size, VisibleColor);
        }

        // Draw unit dots.
        float dotSize = Math.Max(3f, Math.Min(size.X, size.Y) * 0.02f);
        foreach (MinimapUnit unit in Units)
        {
            Vector2 dotPos = new(
                position.X + unit.NormalizedPosition.X * size.X - dotSize / 2f,
                position.Y + unit.NormalizedPosition.Y * size.Y - dotSize / 2f);
            renderer.DrawRect(dotPos, new Vector2(dotSize, dotSize), unit.Color);
        }

        // Camera viewport box.
        var (camMin, camMax) = CameraViewport;
        Vector2 boxPos = position + camMin * size;
        Vector2 boxSize = (camMax - camMin) * size;
        renderer.DrawRectOutline(boxPos, boxSize, CameraBoxColor);

        // Border frame.
        renderer.DrawRectOutline(position, size, BorderColor);
    }
}
