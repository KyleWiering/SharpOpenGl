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

            // If entity already has a destination, let PathFollowingSystem handle it
            if (world.HasComponent<DestinationComponent>(entity)) continue;

            // Advance to next waypoint
            if (queue.CurrentIndex >= queue.Waypoints.Count)
            {
                if (queue.Patrol)
                {
                    queue.CurrentIndex = 0;
                }
                else
                {
                    // All waypoints completed — remove queue and fire event
                    _bus.Publish(new ShipArrivedEvent(entity.Index));
                    world.RemoveComponent<WaypointQueueComponent>(entity);
                    continue;
                }
            }

            // Assign destination from current waypoint
            Vector3 target = queue.Waypoints[queue.CurrentIndex];
            world.AddComponent(entity, new DestinationComponent
            {
                Target = target,
                GridX = (int)target.X,
                GridY = (int)target.Z
            });

            queue.CurrentIndex++;

            // Fire arrival event for intermediate waypoints
            if (queue.CurrentIndex > 1)
            {
                _bus.Publish(new ShipArrivedEvent(entity.Index));
            }
        }
    }
}

/// <summary>
/// Published when a ship arrives at a waypoint or final destination.
/// </summary>
public readonly record struct ShipArrivedEvent(uint EntityIndex);
