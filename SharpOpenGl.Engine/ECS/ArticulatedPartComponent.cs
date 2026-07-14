using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Marks a child entity as an articulated sub-part of a parent hull or building.
/// The part is owned via <see cref="Owner"/> (child-entity pattern); angle state is updated
/// by <c>ArticulationSystem</c> and composed at draw time via <see cref="Rendering.ArticulationMath"/>.
/// </summary>
public sealed class ArticulatedPartComponent
{
    /// <summary>Parent hull or building entity that owns this articulated part.</summary>
    public Entity Owner { get; set; }

    /// <summary>Behavioral/visual role of this part.</summary>
    public ArticulatedPartType PartType { get; set; }

    /// <summary>
    /// Pivot position in the owner's local space (pre-display-scale), matching
    /// <see cref="ShipEngineNozzleComponent.LocalOffset"/> semantics.
    /// </summary>
    public Vector3 LocalPivotOffset { get; set; }

    /// <summary>Mesh origin offset from the pivot in part-local space (applied after yaw/pitch).</summary>
    public Vector3 MeshLocalOffset { get; set; }

    /// <summary>Minimum yaw angle in degrees.</summary>
    public float YawMin { get; set; }

    /// <summary>Maximum yaw angle in degrees.</summary>
    public float YawMax { get; set; }

    /// <summary>Minimum pitch angle in degrees.</summary>
    public float PitchMin { get; set; }

    /// <summary>Maximum pitch angle in degrees.</summary>
    public float PitchMax { get; set; }

    /// <summary>Current smoothed yaw angle in degrees.</summary>
    public float CurrentYaw { get; set; }

    /// <summary>Current smoothed pitch angle in degrees.</summary>
    public float CurrentPitch { get; set; }

    /// <summary>Desired yaw angle in degrees when <see cref="HasAimTarget"/> is true.</summary>
    public float TargetYaw { get; set; }

    /// <summary>Desired pitch angle in degrees when <see cref="HasAimTarget"/> is true.</summary>
    public float TargetPitch { get; set; }

    /// <summary>
    /// When false, no external aim target is active and idle sweep may run.
    /// </summary>
    public bool HasAimTarget { get; set; }

    /// <summary>When true and <see cref="HasAimTarget"/> is false, the part may animate an idle sweep.</summary>
    public bool IdleSweepEnabled { get; set; }

    /// <summary>Degrees per second for idle sweep motion.</summary>
    public float IdleSweepSpeed { get; set; }

    /// <summary>Maximum angle change rate toward the target, in degrees per second.</summary>
    public float SlewRateDegreesPerSecond { get; set; } = 90f;
}