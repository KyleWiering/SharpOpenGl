using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;

namespace SharpOpenGl.Engine.Grid;

/// <summary>
/// Manages a 2-D square grid of <see cref="GridCell"/> objects for each <see cref="GridLayer"/>.
/// Provides coordinate conversion between world-space and grid-space.
/// </summary>
public sealed class GridSystem
{
    // ── Configuration ─────────────────────────────────────────────────────────

    /// <summary>Number of columns (X axis).</summary>
    public int Width { get; }

    /// <summary>Number of rows (Y axis).</summary>
    public int Height { get; }

    /// <summary>World-space size of each cell edge.</summary>
    public float CellSize { get; }

    /// <summary>World-space origin of cell (0, 0) — its bottom-left corner.</summary>
    public Vector2 Origin { get; }

    // ── Storage ───────────────────────────────────────────────────────────────

    // _cells[layer, x, y]
    private readonly GridCell[,,] _cells;

    private static readonly GridLayer[] AllLayers =
        (GridLayer[])Enum.GetValues(typeof(GridLayer));

    /// <summary>
    /// Create an empty grid of the given dimensions.
    /// </summary>
    /// <param name="width">Number of columns.</param>
    /// <param name="height">Number of rows.</param>
    /// <param name="cellSize">World-space length of one cell edge.</param>
    /// <param name="origin">World-space position of the bottom-left corner of cell (0,0).</param>
    /// <param name="maxPlayers">Size of per-cell fog array.</param>
    public GridSystem(int width, int height, float cellSize = 1.0f,
                      Vector2 origin = default, int maxPlayers = 8)
    {
        Width = width;
        Height = height;
        CellSize = cellSize;
        Origin = origin;

        int layerCount = AllLayers.Length;
        _cells = new GridCell[layerCount, width, height];

        foreach (GridLayer layer in AllLayers)
        {
            int li = (int)layer;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    _cells[li, x, y] = new GridCell(x, y, layer, maxPlayers);
        }
    }

    // ── Cell access ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the cell at <paramref name="x"/>, <paramref name="y"/> on
    /// <paramref name="layer"/>, or <c>null</c> if out of bounds.
    /// </summary>
    public GridCell? GetCell(int x, int y, GridLayer layer = GridLayer.Surface)
    {
        if (!InBounds(x, y)) return null;
        return _cells[(int)layer, x, y];
    }

    /// <summary>Returns <c>true</c> when the coordinate is within the grid.</summary>
    public bool InBounds(int x, int y) =>
        (uint)x < (uint)Width && (uint)y < (uint)Height;

    // ── Neighbours ────────────────────────────────────────────────────────────

    private static readonly (int dx, int dy)[] CardinalDirs =
    {
        (1, 0), (-1, 0), (0, 1), (0, -1),
    };

    private static readonly (int dx, int dy)[] AllDirs =
    {
        (1, 0), (-1, 0), (0, 1), (0, -1),
        (1, 1), (1, -1), (-1, 1), (-1, -1),
    };

    /// <summary>
    /// Returns passable, unoccupied neighbours of <paramref name="cell"/> on <paramref name="layer"/>.
    /// Occupied building cells block unit movement the same as impassable terrain.
    /// </summary>
    /// <param name="cell">Source cell.</param>
    /// <param name="layer">Layer to search on.</param>
    /// <param name="diagonal">
    /// Include diagonal neighbours when <c>true</c>. Diagonal steps do not apply
    /// corner-cutting checks; cardinal-only pathfinding (the default) avoids slipping
    /// between adjacent occupied building cells.
    /// </param>
    public IEnumerable<GridCell> GetNeighbours(GridCell cell, GridLayer layer = GridLayer.Surface,
                                               bool diagonal = false)
    {
        var dirs = diagonal ? AllDirs : CardinalDirs;
        foreach ((int dx, int dy) in dirs)
        {
            GridCell? n = GetCell(cell.X + dx, cell.Y + dy, layer);
            if (n != null && n.IsPassable && n.Occupant == Entity.Null)
                yield return n;
        }
    }

    // ── Coordinate conversion ─────────────────────────────────────────────────

    /// <summary>
    /// Convert a grid coordinate to world-space centre of the cell.
    /// Returns the Y component as a flat-plane position (Z is the layer offset).
    /// </summary>
    public Vector3 GridToWorld(int x, int y, GridLayer layer = GridLayer.Surface)
    {
        float wx = Origin.X + (x + 0.5f) * CellSize;
        float wz = Origin.Y + (y + 0.5f) * CellSize;
        float wy = (int)layer * CellSize;          // height offset per layer
        return new Vector3(wx, wy, wz);
    }

    /// <summary>
    /// Convert a world-space position to the nearest grid column/row on the given layer.
    /// Returns <c>false</c> when the resulting coordinate is out of bounds.
    /// </summary>
    public bool WorldToGrid(Vector3 worldPos, out int x, out int y,
                            GridLayer layer = GridLayer.Surface)
    {
        x = (int)MathF.Floor((worldPos.X - Origin.X) / CellSize);
        y = (int)MathF.Floor((worldPos.Z - Origin.Y) / CellSize);
        return InBounds(x, y);
    }

    // ── Enumeration ───────────────────────────────────────────────────────────

    /// <summary>Enumerate every cell on a given layer in row-major order.</summary>
    public IEnumerable<GridCell> AllCells(GridLayer layer = GridLayer.Surface)
    {
        int li = (int)layer;
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                yield return _cells[li, x, y];
    }
}
