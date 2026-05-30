using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Grid;

/// <summary>
/// Precomputes a flow field over a <see cref="GridSystem"/> that steers any number of units
/// toward a single goal cell. Each cell stores a normalised direction vector
/// pointing toward the cheapest path to the goal.
/// </summary>
/// <remarks>
/// Flow-field pathfinding is computed once per goal and efficiently handles
/// large groups without per-unit path searches.
/// </remarks>
public sealed class FlowField
{
    private readonly GridSystem _grid;
    private readonly GridLayer _layer;

    /// <summary>The goal cell this flow field is calculated for.</summary>
    public GridCell Goal { get; }

    // Flat array indexed by [x + y * Width]. Null ⟹ unreachable.
    private readonly Vector2?[] _directions;

    private FlowField(GridSystem grid, GridLayer layer, GridCell goal, Vector2?[] directions)
    {
        _grid = grid;
        _layer = layer;
        Goal = goal;
        _directions = directions;
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Build a flow field on <paramref name="grid"/> for <paramref name="goal"/>
    /// using a Dijkstra flood-fill from the goal outward.
    /// </summary>
    public static FlowField Build(GridSystem grid, GridCell goal,
                                  GridLayer layer = GridLayer.Surface)
    {
        int total = grid.Width * grid.Height;
        float[] cost = new float[total];
        Array.Fill(cost, float.MaxValue);

        var queue = new Queue<GridCell>();
        cost[Index(grid, goal)] = 0f;
        queue.Enqueue(goal);

        // Dijkstra-based cost propagation
        while (queue.Count > 0)
        {
            GridCell current = queue.Dequeue();
            float currentCost = cost[Index(grid, current)];

            foreach (GridCell n in grid.GetNeighbours(current, layer, diagonal: false))
            {
                int ni = Index(grid, n);
                float newCost = currentCost + n.MovementCost;
                if (newCost < cost[ni])
                {
                    cost[ni] = newCost;
                    queue.Enqueue(n);
                }
            }
        }

        // Build direction vectors — point toward the cheapest neighbour
        var directions = new Vector2?[total];
        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                GridCell? cell = grid.GetCell(x, y, layer);
                if (cell == null || !cell.IsPassable) continue;

                int ci = Index(grid, cell);
                if (cost[ci] == float.MaxValue) continue; // unreachable

                GridCell? bestNeighbour = null;
                float bestCost = float.MaxValue;

                foreach (GridCell n in grid.GetNeighbours(cell, layer, diagonal: false))
                {
                    float nc = cost[Index(grid, n)];
                    if (nc < bestCost) { bestCost = nc; bestNeighbour = n; }
                }

                if (cell == goal)
                {
                    directions[ci] = Vector2.Zero;
                }
                else if (bestNeighbour != null)
                {
                    int dx = bestNeighbour.X - x;
                    int dy = bestNeighbour.Y - y;
                    directions[ci] = Vector2.Normalize(new Vector2(dx, dy));
                }
            }
        }

        return new FlowField(grid, layer, goal, directions);
    }

    // ── Query ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the flow direction for a grid cell, or <c>null</c> if unreachable.
    /// </summary>
    public Vector2? GetDirection(int x, int y)
    {
        if (!_grid.InBounds(x, y)) return null;
        return _directions[x + y * _grid.Width];
    }

    /// <summary>
    /// Returns the flow direction for <paramref name="cell"/>, or <c>null</c> if unreachable.
    /// </summary>
    public Vector2? GetDirection(GridCell cell) => GetDirection(cell.X, cell.Y);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int Index(GridSystem grid, GridCell cell) =>
        cell.X + cell.Y * grid.Width;
}
