using SharpOpenGl.Engine.ECS;

namespace SharpOpenGl.Engine.Grid;

/// <summary>
/// A* pathfinding on a <see cref="GridSystem"/>.
/// Returns a list of cells from start (exclusive) to goal (inclusive),
/// or an empty list when no path exists.
/// </summary>
public static class Pathfinding
{
    /// <summary>
    /// Find a path from <paramref name="start"/> to <paramref name="goal"/> using A*.
    /// Terrain movement costs from <see cref="GridCell.MovementCost"/> are respected.
    /// </summary>
    /// <param name="grid">The grid to search.</param>
    /// <param name="start">Starting cell.</param>
    /// <param name="goal">Target cell.</param>
    /// <param name="layer">Layer to search on.</param>
    /// <param name="diagonal">Allow diagonal movement when <c>true</c>.</param>
    /// <returns>
    /// Ordered list of cells from the cell after <paramref name="start"/> to
    /// <paramref name="goal"/> (inclusive). Empty if no path exists.
    /// </returns>
    public static List<GridCell> FindPath(GridSystem grid, GridCell start, GridCell goal,
                                          GridLayer layer = GridLayer.Surface,
                                          bool diagonal = false)
    {
        if (start == goal) return new List<GridCell>();
        if (!goal.IsPassable || goal.Occupant != Entity.Null) return new List<GridCell>();

        // Priority queue ordered by f = g + h
        var open = new PriorityQueue<GridCell, float>();
        var gScore = new Dictionary<GridCell, float>();
        var cameFrom = new Dictionary<GridCell, GridCell>();
        var closed = new HashSet<GridCell>();

        gScore[start] = 0f;
        open.Enqueue(start, Heuristic(start, goal));

        while (open.Count > 0)
        {
            GridCell current = open.Dequeue();

            if (current == goal)
                return ReconstructPath(cameFrom, goal, start);

            if (!closed.Add(current)) continue;

            foreach (GridCell neighbour in grid.GetNeighbours(current, layer, diagonal))
            {
                if (closed.Contains(neighbour)) continue;

                float tentativeG = gScore[current] + neighbour.MovementCost;

                if (!gScore.TryGetValue(neighbour, out float knownG) || tentativeG < knownG)
                {
                    gScore[neighbour] = tentativeG;
                    cameFrom[neighbour] = current;
                    float f = tentativeG + Heuristic(neighbour, goal);
                    open.Enqueue(neighbour, f);
                }
            }
        }

        return new List<GridCell>(); // no path found
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Chebyshev / octile distance heuristic (admissible for grid movement).</summary>
    private static float Heuristic(GridCell a, GridCell b)
    {
        int dx = Math.Abs(a.X - b.X);
        int dy = Math.Abs(a.Y - b.Y);
        return dx + dy;   // Manhattan — consistent for cardinal-only movement
    }

    private static List<GridCell> ReconstructPath(
        Dictionary<GridCell, GridCell> cameFrom, GridCell goal, GridCell start)
    {
        var path = new List<GridCell>();
        GridCell current = goal;
        while (current != start)
        {
            path.Add(current);
            current = cameFrom[current];
        }
        path.Reverse();
        return path;
    }
}
