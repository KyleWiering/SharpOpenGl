using OpenTK.Mathematics;
using SharpOpenGl.Engine.Config;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Moves entities toward their <see cref="MovementComponent.PathTarget"/>.
/// Applies acceleration, speed limiting, and stops when close enough to destination.
/// </summary>
public sealed class MovementSystem : GameSystem
{
    private const float ArrivalThreshold = 1.0f;
    private const float DecelerationFactor = 3f;

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        foreach (var (entity, movement) in world.Query<MovementComponent>())
        {
            var disabled = world.GetComponent<DisabledComponent>(entity);
            if (disabled is { IsActive: true })
            {
                movement.PathTarget = null;
                movement.Velocity = Vector3.Zero;
                continue;
            }

            if (movement.PathTarget == null)
            {
                // Decelerate to stop
                if (movement.Velocity.LengthSquared > 0.01f)
                {
                    movement.Velocity *= MathF.Max(0f, 1f - deltaTime * DecelerationFactor);
                }
                else
                {
                    movement.Velocity = Vector3.Zero;
                }
                continue;
            }

            var transform = world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            Vector3 target = movement.PathTarget.Value;
            Vector3 toTarget = target - transform.Position;
            float distance = toTarget.Length;

            // PathFollowingSystem owns arrival while a route is active.
            bool routeManaged = world.HasComponent<DestinationComponent>(entity)
                || world.HasComponent<PathComponent>(entity);

            if (distance < ArrivalThreshold && !routeManaged)
            {
                movement.PathTarget = null;
                movement.Velocity = Vector3.Zero;
                continue;
            }

            Vector3 direction = toTarget.Normalized();
            float maxSpeed = movement.Speed * MovementBalance.SpeedMultiplier;
            float maxAccel = movement.Acceleration * MovementBalance.AccelerationMultiplier;

            // Rotate toward target (yaw only for top-down RTS)
            float targetYaw = MathHelper.RadiansToDegrees(
                MathF.Atan2(direction.X, direction.Z));
            float currentYaw = transform.EulerAngles.Y;
            float yawDiff = WrapAngle(targetYaw - currentYaw);
            float maxTurn = movement.TurnRate * deltaTime;
            float yawStep = MathHelper.Clamp(yawDiff, -maxTurn, maxTurn);
            transform.EulerAngles = new Vector3(
                transform.EulerAngles.X,
                currentYaw + yawStep,
                transform.EulerAngles.Z);

            // Compute desired speed (slow near target)
            float desiredSpeed = maxSpeed;
            float slowRadius = maxSpeed * 0.3f;
            if (distance < slowRadius)
            {
                desiredSpeed = maxSpeed * (distance / slowRadius);
                desiredSpeed = MathF.Max(desiredSpeed, maxSpeed * 0.15f);
            }

            // Accelerate toward target
            Vector3 desiredVelocity = direction * desiredSpeed;
            Vector3 accel = desiredVelocity - movement.Velocity;
            if (accel.LengthSquared > 0.001f)
            {
                float accelMag = MathF.Min(accel.Length, maxAccel * deltaTime);
                accel = accel.Normalized() * accelMag;
            }
            movement.Velocity += accel;

            // Clamp speed
            if (movement.Velocity.Length > maxSpeed)
            {
                movement.Velocity = movement.Velocity.Normalized() * maxSpeed;
            }

            transform.Position += movement.Velocity * deltaTime;
        }
    }

    private static float WrapAngle(float degrees)
    {
        while (degrees > 180f) degrees -= 360f;
        while (degrees < -180f) degrees += 360f;
        return degrees;
    }
}