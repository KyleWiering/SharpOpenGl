using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Marks an entity as a member of a squadron.
/// The formation system uses these values to position units relative to their squad leader.
/// </summary>
public sealed class SquadMemberComponent
{
    /// <summary>
    /// Identifier of the squad this unit belongs to.
    /// A value of <c>-1</c> means the unit is not currently assigned to a squad.
    /// </summary>
    public int SquadId { get; set; } = -1;

    /// <summary>
    /// Zero-based index of this unit's slot within its formation.
    /// A value of <c>-1</c> means unassigned.
    /// </summary>
    public int FormationSlot { get; set; } = -1;

    /// <summary>
    /// Local offset from the squad leader's position when in formation.
    /// Expressed in world-space units.
    /// </summary>
    public Vector3 FormationOffset { get; set; } = Vector3.Zero;
}
