using OpenTK.Mathematics;
using SharpOpenGl.Engine.Events;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Processes entities with <see cref="WaypointQueueComponent"/> to automatically
/// assign destinations from a waypoint queue. Supports sequential and patrol modes.
/// </summary>
public sealed class AutoMoveSystem : GameSystem
{
    private readonly EventBus _bus;

    public AutoMoveSystem(EventBus bus)
    {
        _bus = bus;
    }

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        foreach (var (entity, queue) in world.Query<WaypointQueueComponent>())
        {
            if (queue.Waypoints.Count == 0) continue;

            if (world.HasComponent<DestinationComponent>(entity))
            {
                queue.LegInProgress = true;
                continue;
            }

            if (queue.LegInProgress)
            {
                queue.LegInProgress = false;
                queue.CurrentIndex++;

                if (queue.CurrentIndex < queue.Waypoints.Count)
                    _bus.Publish(new ShipArrivedEvent(entity.Index));
            }

            if (queue.CurrentIndex >= queue.Waypoints.Count)
            {
                if (queue.Patrol)
                {
                    queue.CurrentIndex = 0;
                }
                else
                {
                    _bus.Publish(new ShipArrivedEvent(entity.Index));
                    world.RemoveComponent<WaypointQueueComponent>(entity);
                    continue;
                }
            }

            Vector3 target = queue.Waypoints[queue.CurrentIndex];
            world.AddComponent(entity, new DestinationComponent
            {
                Target = target,
                GridX = (int)target.X,
                GridY = (int)target.Z,
            });
            queue.LegInProgress = true;
        }
    }
}

/// <summary>
/// Published when a ship arrives at a waypoint or final destination.
/// </summary>
public readonly record struct ShipArrivedEvent(uint EntityIndex);