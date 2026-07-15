using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Links a per-nozzle <see cref="ParticleEmitterComponent"/> child entity to its owner ship.
/// </summary>
public sealed class ShipEngineNozzleComponent
{
    /// <summary>Owning ship entity.</summary>
    public Entity Owner { get; set; }

    /// <summary>Nozzle position in the owner ship's local space (pre-display-scale).</summary>
    public Vector3 LocalOffset { get; set; }

    /// <summary>When true, emit a reduced idle trail even when the ship is stationary (gallery showcase).</summary>
    public bool IdleGlowWhenStationary { get; set; }

    /// <summary>Full-movement emit rate configured at spawn (before idle scaling).</summary>
    public float BaseEmitRate { get; set; } = 60f;

    /// <summary>
    /// Current gimbal yaw in degrees. Applied after owner yaw, before gimbal pitch, when
    /// transforming <see cref="LocalOffset"/> and exhaust direction to world space.
    /// </summary>
    public float GimbalYaw { get; set; }

    /// <summary>
    /// Current gimbal pitch in degrees. Applied after owner yaw and <see cref="GimbalYaw"/>.
    /// </summary>
    public float GimbalPitch { get; set; }

    /// <summary>Minimum gimbal yaw in degrees (default −3°).</summary>
    public float GimbalYawMin { get; set; } = -3f;

    /// <summary>Maximum gimbal yaw in degrees (default +3°).</summary>
    public float GimbalYawMax { get; set; } = 3f;

    /// <summary>Minimum gimbal pitch in degrees (default −2°).</summary>
    public float GimbalPitchMin { get; set; } = -2f;

    /// <summary>Maximum gimbal pitch in degrees (default +2°).</summary>
    public float GimbalPitchMax { get; set; } = 2f;

    /// <summary>Per-nozzle seed for stable thrust-vectoring oscillation.</summary>
    public float GimbalNoiseSeed { get; set; }
}