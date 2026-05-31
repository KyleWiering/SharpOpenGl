namespace SharpOpenGl.Engine.Audio;

/// <summary>
/// Identifies a logical audio event so the audio manager can map it to
/// the correct sound effect without callers knowing about file paths or buffers.
/// </summary>
public enum AudioEventType
{
    /// <summary>Ranged weapon discharge (laser, cannon).</summary>
    WeaponFire,

    /// <summary>Missile or torpedo launch.</summary>
    WeaponLaunch,

    /// <summary>Projectile impact / unit explosion.</summary>
    Explosion,

    /// <summary>Shield absorbs a hit (bubble pop / energy crackle).</summary>
    ShieldHit,

    /// <summary>Unit acknowledged a move command.</summary>
    UnitMoveAck,

    /// <summary>Unit acknowledged a target/attack command.</summary>
    UnitAttackAck,

    /// <summary>Resource deposited or collected.</summary>
    ResourceCollected,

    /// <summary>Generic UI button click.</summary>
    UIClick,

    /// <summary>Generic UI hover sound.</summary>
    UIHover,

    /// <summary>Mission success fanfare.</summary>
    MissionComplete,

    /// <summary>Mission failure sting.</summary>
    MissionFail,

    /// <summary>Building placement confirmed.</summary>
    BuildingPlaced,

    /// <summary>Engine idle / ambient thruster loop.</summary>
    EngineIdle,
}
