using OpenTK.Mathematics;
using SharpOpenGl.Engine.Grid;

namespace SharpOpenGl.Engine.ECS;

/// <summary>Smoothing and line-of-sight helpers for grid path following.</summary>
public static class PathRouteHelper
{
    /// <summary>Removes intermediate waypoints when a straight segment is passable.</summary>
    public static List<Vector3> StringPull(IReadOnlyList<Vector3> waypoints, GridSystem grid)
    {
        if (waypoints.Count <= 2)
            return waypoints.ToList();

        var result = new List<Vector3> { waypoints[0] };
        int anchor = 0;

        for (int i = 2; i < waypoints.Count; i++)
        {
            if (!HasClearLine(waypoints[anchor], waypoints[i], grid))
            {
                result.Add(waypoints[i - 1]);
                anchor = i - 1;
            }
        }

        result.Add(waypoints[^1]);
        return result;
    }

    /// <summary>Returns true when every grid cell along the XZ segment is passable.</summary>
    public static bool HasClearLine(Vector3 from, Vector3 to, GridSystem grid)
    {
        if (!grid.WorldToGrid(from, out int x0, out int y0))
            return false;
        if (!grid.WorldToGrid(to, out int x1, out int y1))
            return false;

        foreach ((int x, int y) in GridLine(x0, y0, x1, y1))
        {
            GridCell? cell = grid.GetCell(x, y);
            if (cell == null || !cell.IsPassable)
                return false;
        }

        return true;
    }

    private static IEnumerable<(int x, int y)> GridLine(int x0, int y0, int x1, int y1)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            yield return (x0, y0);
            if (x0 == x1 && y0 == y1)
                break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    /// <summary>XZ-plane distance between two world positions.</summary>
    public static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = a.X - b.X;
        float dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    /// <summary>Unit direction on the XZ plane from <paramref name="from"/> to <paramref name="to"/>.</summary>
    public static Vector3 HorizontalDirection(Vector3 from, Vector3 to)
    {
        float dx = to.X - from.X;
        float dz = to.Z - from.Z;
        float len = MathF.Sqrt(dx * dx + dz * dz);
        if (len < 0.001f) return Vector3.Zero;
        return new Vector3(dx / len, 0f, dz / len);
    }
}