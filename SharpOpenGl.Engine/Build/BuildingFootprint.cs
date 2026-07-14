using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Grid;

namespace SharpOpenGl.Engine.Build;

/// <summary>Grid footprint helpers for building placement and occupancy.</summary>
public static class BuildingFootprint
{
    /// <summary>Resolve footprint dimensions from a building component or defaults.</summary>
    public static (int Cols, int Rows) GetSize(int[]? footprint)
    {
        if (footprint is { Length: >= 2 })
            return (Math.Max(1, footprint[0]), Math.Max(1, footprint[1]));
        return (1, 1);
    }

    /// <summary>
    /// Compute bottom-left grid cell for a footprint centered on <paramref name="worldPos"/>.
    /// </summary>
    public static bool TryGetOrigin(GridSystem grid, Vector3 worldPos, int cols, int rows,
        out int originX, out int originY)
    {
        originX = 0;
        originY = 0;
        if (!grid.WorldToGrid(worldPos, out int centerX, out int centerY))
            return false;

        originX = centerX - cols / 2;
        originY = centerY - rows / 2;
        return true;
    }

    /// <summary>
    /// Snap a world cursor position to the centre of the nearest grid cell.
    /// Uses <see cref="GridSystem.WorldToGrid"/> and <see cref="GridSystem.GridToWorld"/>.
    /// </summary>
    public static Vector3 SnapToCellCenter(GridSystem grid, Vector3 worldPos)
    {
        grid.WorldToGrid(worldPos, out int x, out int y);
        return grid.GridToWorld(x, y);
    }

    /// <summary>Enumerate all grid cells covered by a footprint.</summary>
    public static IEnumerable<(int X, int Y)> EnumerateCells(
        GridSystem grid, Vector3 worldPos, int cols, int rows)
    {
        if (!TryGetOrigin(grid, worldPos, cols, rows, out int originX, out int originY))
            yield break;

        for (int dx = 0; dx < cols; dx++)
        for (int dy = 0; dy < rows; dy++)
            yield return (originX + dx, originY + dy);
    }

    /// <summary>Mark all footprint cells as occupied by <paramref name="buildingEntity"/>.</summary>
    public static void Occupy(GridSystem grid, Entity buildingEntity, Vector3 worldPos, int[] footprint)
    {
        var (cols, rows) = GetSize(footprint);
        foreach (var (x, y) in EnumerateCells(grid, worldPos, cols, rows))
        {
            GridCell? cell = grid.GetCell(x, y);
            if (cell != null)
                cell.Occupant = buildingEntity;
        }
    }

    /// <summary>
    /// Collect distinct <em>completed</em> building types owned by a player.
    /// Structures still under construction are excluded — prereq checks require finished buildings.
    /// </summary>
    public static HashSet<string> GetBuiltTypes(World world, int playerId)
    {
        var built = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (entity, building) in world.Query<BuildingComponent>())
        {
            if (world.HasComponent<UnderConstructionComponent>(entity)) continue;
            if (building.PlayerId == playerId && !string.IsNullOrWhiteSpace(building.BuildingType))
                built.Add(building.BuildingType);
        }
        return built;
    }
}