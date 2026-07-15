using OpenTK.Mathematics;
using SharpOpenGl.Engine.Grid;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Builds ground tint quads for pathing-relevant terrain inside the camera view.
/// Skips unexplored fog cells so the overlay reinforces exploration memory.
/// </summary>
public sealed class TerrainReadabilityOverlay
{
    private readonly List<TerrainTintCell> _cells = new();

    public int CellCount => _cells.Count;

    /// <summary>Visible tint cells from the last <see cref="Sync"/> call.</summary>
    public IReadOnlyList<TerrainTintCell> Cells => _cells;

    /// <summary>
    /// Rebuild visible terrain tint cells for the current camera bounds.
    /// </summary>
    /// <param name="cameraBoundsXZ">minX, maxX, minZ, maxZ in world space.</param>
    public void Sync(GridSystem grid, FogOfWar? fog, int playerId, Vector4 cameraBoundsXZ)
    {
        _cells.Clear();

        if (!grid.WorldToGrid(new Vector3(cameraBoundsXZ.X, 0f, cameraBoundsXZ.Z), out int minGx, out int minGy))
            minGx = minGy = 0;
        if (!grid.WorldToGrid(new Vector3(cameraBoundsXZ.Y, 0f, cameraBoundsXZ.W), out int maxGx, out int maxGy))
        {
            maxGx = grid.Width - 1;
            maxGy = grid.Height - 1;
        }

        minGx = Math.Clamp(minGx, 0, grid.Width - 1);
        minGy = Math.Clamp(minGy, 0, grid.Height - 1);
        maxGx = Math.Clamp(maxGx, 0, grid.Width - 1);
        maxGy = Math.Clamp(maxGy, 0, grid.Height - 1);

        if (minGx > maxGx) (minGx, maxGx) = (maxGx, minGx);
        if (minGy > maxGy) (minGy, maxGy) = (maxGy, minGy);

        for (int x = minGx; x <= maxGx; x++)
        for (int y = minGy; y <= maxGy; y++)
        {
            GridCell? cell = grid.GetCell(x, y);
            if (cell == null || !TerrainVisualPalette.AffectsPathing(cell.Terrain)) continue;

            if (fog != null)
            {
                FogState state = fog.GetState(playerId, x, y);
                if (state == FogState.Unexplored) continue;
            }

            Vector4? tint = TerrainVisualPalette.ResolveTint(cell.Terrain);
            if (tint is null) continue;

            float alpha = tint.Value.W;
            if (fog != null && fog.GetState(playerId, x, y) == FogState.Explored)
                alpha *= 0.55f;

            _cells.Add(new TerrainTintCell(
                grid.GridToWorld(x, y),
                tint.Value with { W = alpha },
                grid.CellSize));
        }
    }

    public readonly record struct TerrainTintCell(Vector3 Center, Vector4 Color, float CellSize);
}