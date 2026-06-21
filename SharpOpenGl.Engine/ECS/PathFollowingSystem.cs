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
    private const float WaypointArrivalThreshold = 2.5f;

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

            if (path == null)
            {
                path = ComputePath(transform.Position, dest);
                if (path == null)
                {
                    world.RemoveComponent<DestinationComponent>(entity);
                    movement.PathTarget = null;
                    continue;
                }
                world.AddComponent(entity, path);
            }

            if (path.IsComplete)
            {
                world.RemoveComponent<PathComponent>(entity);
                world.RemoveComponent<DestinationComponent>(entity);
                movement.PathTarget = null;
                continue;
            }

            AdvancePassedWaypoints(path, transform.Position);

            if (path.IsComplete)
            {
                world.RemoveComponent<PathComponent>(entity);
                world.RemoveComponent<DestinationComponent>(entity);
                movement.PathTarget = null;
                continue;
            }

            int targetIndex = SelectLookAheadIndex(path, transform.Position);
            movement.PathTarget = path.Waypoints[targetIndex];
        }
    }

    private void AdvancePassedWaypoints(PathComponent path, Vector3 position)
    {
        while (path.CurrentWaypointIndex < path.Waypoints.Count)
        {
            Vector3 waypoint = path.Waypoints[path.CurrentWaypointIndex];
            float dist = PathRouteHelper.HorizontalDistance(position, waypoint);

            if (dist < WaypointArrivalThreshold)
            {
                path.CurrentWaypointIndex++;
                continue;
            }

            if (path.CurrentWaypointIndex < path.Waypoints.Count - 1)
            {
                Vector3 next = path.Waypoints[path.CurrentWaypointIndex + 1];
                Vector3 leg = new(next.X - waypoint.X, 0f, next.Z - waypoint.Z);
                Vector3 toShip = new(position.X - waypoint.X, 0f, position.Z - waypoint.Z);
                if (Vector3.Dot(leg, toShip) > 0f && leg.LengthSquared > 0.01f)
                {
                    path.CurrentWaypointIndex++;
                    continue;
                }
            }

            break;
        }
    }

    private int SelectLookAheadIndex(PathComponent path, Vector3 position)
    {
        int index = path.CurrentWaypointIndex;
        for (int i = path.CurrentWaypointIndex + 1; i < path.Waypoints.Count; i++)
        {
            if (PathRouteHelper.HasClearLine(position, path.Waypoints[i], _grid))
                index = i;
            else
                break;
        }

        return index;
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

        var path = new PathComponent();

        if (PathRouteHelper.HasClearLine(currentPos, dest.Target, _grid))
        {
            path.Waypoints.Add(dest.Target);
            return path;
        }

        List<GridCell> cells = Pathfinding.FindPath(
            _grid, startCell, goalCell, diagonal: true);
        if (cells.Count == 0 && startCell != goalCell) return null;

        var raw = new List<Vector3>();
        foreach (GridCell cell in cells)
            raw.Add(_grid.GridToWorld(cell.X, cell.Y));

        raw.Add(dest.Target);
        path.Waypoints = PathRouteHelper.StringPull(raw, _grid);
        return path;
    }
}