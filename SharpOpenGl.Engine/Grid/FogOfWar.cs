namespace SharpOpenGl.Engine.Grid;

/// <summary>
/// Tracks per-player fog-of-war visibility across a <see cref="GridSystem"/>.
/// Each player sees only cells within sight range of their own units.
/// </summary>
public sealed class FogOfWar
{
    private readonly GridSystem _grid;
    private readonly int _playerCount;

    /// <param name="grid">The grid this fog-of-war tracks.</param>
    /// <param name="playerCount">Number of players (must be ≤ 8, matching GridCell allocation).</param>
    public FogOfWar(GridSystem grid, int playerCount = 2)
    {
        _grid = grid;
        _playerCount = Math.Clamp(playerCount, 1, 8);
    }

    /// <summary>Number of 0-based fog player layers tracked by this instance.</summary>
    public int PlayerCount => _playerCount;

    // ── Per-frame update ──────────────────────────────────────────────────────

    /// <summary>
    /// Recompute visibility for <paramref name="playerId"/> from scratch.
    /// All currently-<see cref="FogState.Visible"/> cells are downgraded to
    /// <see cref="FogState.Explored"/> before the new reveal pass.
    /// </summary>
    /// <param name="playerId">0-based player index.</param>
    /// <param name="unitPositions">
    /// Collection of (gridX, gridY, sightRange) tuples describing each unit
    /// contributing to visibility.
    /// </param>
    /// <param name="layer">Layer to update.</param>
    public void Update(int playerId,
                       IEnumerable<(int X, int Y, int SightRange)> unitPositions,
                       GridLayer layer = GridLayer.Surface)
    {
        if ((uint)playerId >= (uint)_playerCount) return;

        // Downgrade all Visible → Explored
        foreach (GridCell cell in _grid.AllCells(layer))
        {
            if (cell.GetFog(playerId) == FogState.Visible)
                cell.SetFog(playerId, FogState.Explored);
        }

        // Reveal cells within each unit's sight range
        foreach ((int ux, int uy, int range) in unitPositions)
            Reveal(playerId, ux, uy, range, layer);
    }

    /// <summary>
    /// Unconditionally reveal a circular area around <paramref name="cx"/>,
    /// <paramref name="cy"/> for <paramref name="playerId"/>.
    /// </summary>
    public void Reveal(int playerId, int cx, int cy, int radius,
                       GridLayer layer = GridLayer.Surface)
    {
        if ((uint)playerId >= (uint)_playerCount) return;

        int minX = Math.Max(0, cx - radius);
        int maxX = Math.Min(_grid.Width  - 1, cx + radius);
        int minY = Math.Max(0, cy - radius);
        int maxY = Math.Min(_grid.Height - 1, cy + radius);

        int r2 = radius * radius;

        for (int x = minX; x <= maxX; x++)
        for (int y = minY; y <= maxY; y++)
        {
            int dx = x - cx, dy = y - cy;
            if (dx * dx + dy * dy <= r2)
            {
                GridCell? cell = _grid.GetCell(x, y, layer);
                cell?.SetFog(playerId, FogState.Visible);
            }
        }
    }

    // ── Query helpers ─────────────────────────────────────────────────────────

    /// <summary>Returns the fog state for a cell at grid coordinate.</summary>
    public FogState GetState(int playerId, int x, int y,
                             GridLayer layer = GridLayer.Surface)
    {
        GridCell? cell = _grid.GetCell(x, y, layer);
        return cell?.GetFog(playerId) ?? FogState.Unexplored;
    }

    /// <summary>Returns <c>true</c> if the cell is currently visible to the player.</summary>
    public bool IsVisible(int playerId, int x, int y,
                          GridLayer layer = GridLayer.Surface) =>
        GetState(playerId, x, y, layer) == FogState.Visible;
}
