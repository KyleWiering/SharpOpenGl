using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Runtime state for a live projectile entity.
/// Spawned by <c>CombatSystem</c> and consumed by <c>ProjectileSystem</c>.
/// </summary>
public sealed class ProjectileComponent
{
    /// <summary>Entity that fired this projectile.</summary>
    public Entity Owner { get; set; } = Entity.Null;

    /// <summary>Primary target entity (used by <see cref="ProjectileType.Homing"/>).</summary>
    public Entity Target { get; set; } = Entity.Null;

    /// <summary>How the projectile travels and deals damage.</summary>
    public ProjectileType Type { get; set; } = ProjectileType.Linear;

    /// <summary>Base damage before armor/shield reduction.</summary>
    public float Damage { get; set; }

    /// <summary>Travel speed in world units per second (ignored for Instant).</summary>
    public float Speed { get; set; } = 400f;

    /// <summary>Blast radius in world units (AoE only).</summary>
    public float BlastRadius { get; set; }

    /// <summary>Remaining lifetime in seconds. Projectile is destroyed when this reaches 0.</summary>
    public float Lifetime { get; set; } = 3f;

    /// <summary>Current direction of travel (unit vector).</summary>
    public Vector3 Direction { get; set; } = Vector3.UnitZ;

    /// <summary>Player / faction ID of the owner; prevents friendly-fire.</summary>
    public int OwnerFaction { get; set; }
}
