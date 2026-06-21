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
                if (movement.Velocity.LengthSquared > 0.01f)
                    movement.Velocity *= MathF.Max(0f, 1f - deltaTime * DecelerationFactor);
                else
                    movement.Velocity = Vector3.Zero;
                continue;
            }

            var transform = world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            Vector3 target = movement.PathTarget.Value;
            float distance = PathRouteHelper.HorizontalDistance(transform.Position, target);

            bool routeManaged = world.HasComponent<DestinationComponent>(entity)
                || world.HasComponent<PathComponent>(entity);
            bool finalLeg = !routeManaged || IsOnFinalPathLeg(world, entity);

            if (distance < ArrivalThreshold && !routeManaged)
            {
                movement.PathTarget = null;
                movement.Velocity = Vector3.Zero;
                continue;
            }

            Vector3 direction = PathRouteHelper.HorizontalDirection(transform.Position, target);
            if (direction.LengthSquared < 0.0001f)
                continue;

            float maxSpeed = movement.Speed * MovementBalance.SpeedMultiplier;
            float maxAccel = movement.Acceleration * MovementBalance.AccelerationMultiplier;

            float targetYaw = MathHelper.RadiansToDegrees(MathF.Atan2(direction.X, direction.Z));
            float currentYaw = transform.EulerAngles.Y;
            float yawDiff = WrapAngle(targetYaw - currentYaw);
            float maxTurn = movement.TurnRate * deltaTime;
            float yawStep = MathHelper.Clamp(yawDiff, -maxTurn, maxTurn);
            transform.EulerAngles = new Vector3(
                transform.EulerAngles.X,
                currentYaw + yawStep,
                transform.EulerAngles.Z);

            float desiredSpeed = maxSpeed;
            if (finalLeg)
            {
                float slowRadius = maxSpeed * 0.35f;
                if (distance < slowRadius)
                {
                    desiredSpeed = maxSpeed * (distance / slowRadius);
                    desiredSpeed = MathF.Max(desiredSpeed, maxSpeed * 0.2f);
                }
            }

            Vector3 desiredVelocity = direction * desiredSpeed;
            Vector3 accel = desiredVelocity - movement.Velocity;
            if (accel.LengthSquared > 0.001f)
            {
                float accelMag = MathF.Min(accel.Length, maxAccel * deltaTime);
                accel = accel.Normalized() * accelMag;
            }
            movement.Velocity += accel;

            if (movement.Velocity.Length > maxSpeed)
                movement.Velocity = movement.Velocity.Normalized() * maxSpeed;

            transform.Position += movement.Velocity * deltaTime;
        }
    }

    private static bool IsOnFinalPathLeg(World world, Entity entity)
    {
        var path = world.GetComponent<PathComponent>(entity);
        if (path == null || path.Waypoints.Count == 0)
            return true;

        return path.CurrentWaypointIndex >= path.Waypoints.Count - 1;
    }

    private static float WrapAngle(float degrees)
    {
        while (degrees > 180f) degrees -= 360f;
        while (degrees < -180f) degrees += 360f;
        return degrees;
    }
}