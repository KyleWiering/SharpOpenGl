using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;

namespace SharpOpenGl.Engine.Build;

/// <summary>
/// Applies slow continuous Y-axis rotation to buildings flagged with <see cref="BuildingComponent.Rotates"/>.
/// </summary>
public static class StationRotationSystem
{
    /// <summary>Degrees per second for rotating station visuals.</summary>
    public const float RotationSpeedDegreesPerSecond = 20f;

    /// <summary>
    /// Updates yaw on all rotating buildings. Non-rotating buildings are left unchanged.
    /// </summary>
    public static void UpdateStationRotations(World world, float deltaTime)
    {
        if (deltaTime <= 0f)
            return;

        float yawDelta = RotationSpeedDegreesPerSecond * deltaTime;

        foreach (var (entity, building) in world.Query<BuildingComponent>())
        {
            if (!building.Rotates)
                continue;

            TransformComponent? transform = world.GetComponent<TransformComponent>(entity);
            if (transform == null)
                continue;

            Vector3 angles = transform.EulerAngles;
            angles.Y += yawDelta;
            transform.EulerAngles = angles;
        }
    }
}