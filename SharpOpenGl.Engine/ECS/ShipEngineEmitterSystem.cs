using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Drives Terran ship engine-trail particle emitters each frame: world origin, exhaust direction, emission gating.
/// </summary>
public sealed class ShipEngineEmitterSystem : GameSystem
{
    private const float MovingVelocityThresholdSq = 0.25f;
    private const float IdleEmitRateFactor = 0.25f;
    private const float GimbalSlewRateDegreesPerSecond = 120f;

    private float _gimbalTime;

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        _gimbalTime += deltaTime;

        foreach (var (nozzleEntity, nozzle) in world.Query<ShipEngineNozzleComponent>())
        {
            if (!world.IsAlive(nozzle.Owner))
            {
                world.DestroyEntity(nozzleEntity);
                continue;
            }

            var ownerTf = world.GetComponent<TransformComponent>(nozzle.Owner);
            var emitterComp = world.GetComponent<ParticleEmitterComponent>(nozzleEntity);
            if (ownerTf == null || emitterComp == null) continue;

            var movement = world.GetComponent<MovementComponent>(nozzle.Owner);
            bool moving = movement != null && movement.Velocity.LengthSquared > MovingVelocityThresholdSq;

            UpdateGimbal(nozzle, movement, moving, _gimbalTime, deltaTime);

            Matrix4 rot = BuildNozzleRotation(ownerTf, nozzle);

            Vector3 scaledOffset = new Vector3(
                nozzle.LocalOffset.X * ownerTf.Scale.X,
                nozzle.LocalOffset.Y * ownerTf.Scale.Y,
                nozzle.LocalOffset.Z * ownerTf.Scale.Z);
            Vector3 worldOffset = Vector3.TransformNormal(scaledOffset, rot);
            Vector3 origin = ownerTf.Position + worldOffset;

            Vector3 exhaustDir = Vector3.TransformNormal(-Vector3.UnitZ, rot);
            if (exhaustDir.LengthSquared > 1e-6f)
                exhaustDir = Vector3.Normalize(exhaustDir);

            var emitter = emitterComp.Emitter;
            emitter.Origin = origin;
            emitter.BaseVelocity = exhaustDir * 3f;

            if (moving)
            {
                emitter.IsEmitting = true;
                emitter.EmitRate = nozzle.BaseEmitRate;
            }
            else if (nozzle.IdleGlowWhenStationary)
            {
                emitter.IsEmitting = true;
                emitter.EmitRate = TerranEngineNozzleLayout.CapEmitRate(
                    emitter, nozzle.BaseEmitRate * IdleEmitRateFactor);
            }
            else
            {
                emitter.IsEmitting = false;
            }
        }
    }

    /// <summary>
    /// Composes owner yaw, then gimbal yaw (Y), then gimbal pitch (X) for nozzle offset and exhaust.
    /// </summary>
    public static Matrix4 BuildNozzleRotation(TransformComponent ownerTf, ShipEngineNozzleComponent nozzle)
    {
        float ownerYawRad = MathHelper.DegreesToRadians(ownerTf.EulerAngles.Y);
        Matrix4 ownerYaw = Matrix4.CreateRotationY(ownerYawRad);

        float gimbalYawRad = MathHelper.DegreesToRadians(nozzle.GimbalYaw);
        Matrix4 gimbalYaw = Matrix4.CreateRotationY(gimbalYawRad);

        float gimbalPitchRad = MathHelper.DegreesToRadians(nozzle.GimbalPitch);
        Matrix4 gimbalPitch = Matrix4.CreateRotationX(gimbalPitchRad);

        return ownerYaw * gimbalYaw * gimbalPitch;
    }

    private static void UpdateGimbal(
        ShipEngineNozzleComponent nozzle,
        MovementComponent? movement,
        bool moving,
        float gimbalTime,
        float deltaTime)
    {
        float targetYaw;
        float targetPitch;

        if (moving)
        {
            float seed = nozzle.GimbalNoiseSeed;
            float yawAmp = (nozzle.GimbalYawMax - nozzle.GimbalYawMin) * 0.5f;
            float pitchAmp = (nozzle.GimbalPitchMax - nozzle.GimbalPitchMin) * 0.5f;

            targetYaw = MathF.Sin(gimbalTime * 2.7f + seed) * yawAmp;
            targetPitch = MathF.Cos(gimbalTime * 3.1f + seed * 1.7f) * pitchAmp;

            if (movement != null && movement.Velocity.LengthSquared > 1e-6f)
            {
                Vector3 vel = movement.Velocity;
                float horizLen = MathF.Sqrt(vel.X * vel.X + vel.Z * vel.Z);
                if (horizLen > 1e-4f)
                {
                    float pitchBias = Math.Clamp(vel.Y / horizLen * 1.5f, -0.8f, 0.8f);
                    targetPitch += pitchBias;
                }
            }

            targetYaw = Math.Clamp(targetYaw, nozzle.GimbalYawMin, nozzle.GimbalYawMax);
            targetPitch = Math.Clamp(targetPitch, nozzle.GimbalPitchMin, nozzle.GimbalPitchMax);
        }
        else
        {
            targetYaw = 0f;
            targetPitch = 0f;
        }

        float slew = GimbalSlewRateDegreesPerSecond * deltaTime;
        nozzle.GimbalYaw = SlewToward(nozzle.GimbalYaw, targetYaw, slew);
        nozzle.GimbalPitch = SlewToward(nozzle.GimbalPitch, targetPitch, slew);
    }

    private static float SlewToward(float current, float target, float maxDelta)
    {
        float diff = target - current;
        if (MathF.Abs(diff) <= maxDelta)
            return target;

        return current + MathF.Sign(diff) * maxDelta;
    }
}