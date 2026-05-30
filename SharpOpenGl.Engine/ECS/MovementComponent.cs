using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Defines how an entity moves through the world.
/// Movement systems read these values each frame to update <see cref="TransformComponent"/>.
/// </summary>
public sealed class MovementComponent
{
    /// <summary>Maximum movement speed in world units per second.</summary>
    public float Speed { get; set; }

    /// <summary>Rate of velocity change in world units per second².</summary>
    public float Acceleration { get; set; }

    /// <summary>Maximum rotation speed in degrees per second.</summary>
    public float TurnRate { get; set; }

    /// <summary>
    /// World-space position the entity is currently moving toward.
    /// <c>null</c> means the entity is stationary or has no pending move order.
    /// </summary>
    public Vector3? PathTarget { get; set; }

    /// <summary>Current velocity vector (world units per second).</summary>
    public Vector3 Velocity { get; set; } = Vector3.Zero;
}
