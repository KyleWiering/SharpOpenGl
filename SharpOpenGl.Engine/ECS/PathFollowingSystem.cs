using OpenTK.Mathematics;
using SharpOpenGl.Engine.Grid;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Computes paths for entities with a <see cref="DestinationComponent"/> using A* pathfinding,
/// and follows the path by setting <see cref="MovementComponent.PathTarget"/> to each waypoint.
/// </summary>
public sealed class PathFollowingSystem : GameSystem
{
    private readonly GridSystem _grid;
    private const float WaypointArrivalThreshold = 0.5f;

    public PathFollowingSystem(GridSystem grid)
    {
        _grid = grid;
    }

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        foreach (var (entity, dest) in world.Query<DestinationComponent>())
        {
            var movement = world.GetComponent<MovementComponent>(entity);
            var transform = world.GetComponent<TransformComponent>(entity);
            if (movement == null || transform == null) continue;

            var path = world.GetComponent<PathComponent>(entity);

            // If no path computed yet, compute one
            if (path == null)
            {
                path = ComputePath(transform.Position, dest);
                if (path == null)
                {
                    // Unreachable — remove destination, move as close as possible
                    world.RemoveComponent<DestinationComponent>(entity);
                    movement.PathTarget = null;
                    continue;
                }
                world.AddComponent(entity, path);
            }

            // Follow path waypoints
            if (path.IsComplete)
            {
                // Arrived at destination
                world.RemoveComponent<PathComponent>(entity);
                world.RemoveComponent<DestinationComponent>(entity);
                movement.PathTarget = null;
                continue;
            }

            Vector3 nextWaypoint = path.Waypoints[path.CurrentWaypointIndex];
            float dist = (transform.Position - nextWaypoint).Length;

            if (dist < WaypointArrivalThreshold)
            {
                path.CurrentWaypointIndex++;
                if (path.IsComplete)
                {
                    world.RemoveComponent<PathComponent>(entity);
                    world.RemoveComponent<DestinationComponent>(entity);
                    movement.PathTarget = null;
                }
                else
                {
                    movement.PathTarget = path.Waypoints[path.CurrentWaypointIndex];
                }
            }
            else
            {
                movement.PathTarget = nextWaypoint;
            }
        }
    }

    private PathComponent? ComputePath(Vector3 currentPos, DestinationComponent dest)
    {
        if (!_grid.WorldToGrid(currentPos, out int startX, out int startY))
            return null;

        if (!_grid.WorldToGrid(dest.Target, out int goalX, out int goalY))
            return null;

        GridCell? startCell = _grid.GetCell(startX, startY);
        GridCell? goalCell = _grid.GetCell(goalX, goalY);

        if (startCell == null || goalCell == null) return null;
        if (!goalCell.IsPassable) return null;

        List<GridCell> cells = Pathfinding.FindPath(_grid, startCell, goalCell);
        if (cells.Count == 0 && startCell != goalCell) return null;

        var path = new PathComponent();
        foreach (GridCell cell in cells)
        {
            path.Waypoints.Add(_grid.GridToWorld(cell.X, cell.Y));
        }
        // Final waypoint is the exact destination
        path.Waypoints.Add(dest.Target);

        return path;
    }
}
