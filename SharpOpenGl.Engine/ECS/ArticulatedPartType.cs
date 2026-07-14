namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Identifies the behavioral/visual role of an articulated sub-part on a hull or building.
/// </summary>
public enum ArticulatedPartType
{
    /// <summary>Horizontal turret traverse (yaw only or primary yaw axis).</summary>
    TurretYaw,

    /// <summary>Turret elevation (pitch).</summary>
    TurretPitch,

    /// <summary>Rotating sensor dish on stations and capital ships.</summary>
    SensorDish,

    /// <summary>Construction / cargo crane arm.</summary>
    Crane,

    /// <summary>Shipyard bay arch door segment (pitch-open).</summary>
    BayDoor,

    /// <summary>Mining collector arm on harvester units.</summary>
    MiningArm,

    /// <summary>Repair tool arm on support repair tenders.</summary>
    RepairArm,

    /// <summary>Engine nozzle gimbal for exhaust vectoring visuals.</summary>
    EngineGimbal,

    /// <summary>Drone spine launcher pod — yaw toward combat target, no pitch barrel child.</summary>
    LauncherPod,

    /// <summary>Fighter/interceptor subtle wing flap pitch under thrust.</summary>
    WingFlap,
}