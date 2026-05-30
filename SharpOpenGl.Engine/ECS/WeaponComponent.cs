namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Describes a single weapon mounted on an entity.
/// Entities may have multiple weapons (attach separate instances per slot).
/// </summary>
public sealed class WeaponComponent
{
    /// <summary>Weapon slot index (0-based). Identifies the mount point on the ship.</summary>
    public int Slot { get; set; }

    /// <summary>Weapon type identifier (e.g. "laser", "missile", "cannon").</summary>
    public string Type { get; set; } = "laser";

    /// <summary>Damage dealt per hit.</summary>
    public float Damage { get; set; }

    /// <summary>Maximum attack range in world units.</summary>
    public float Range { get; set; }

    /// <summary>Shots (or projectiles) fired per second.</summary>
    public float FireRate { get; set; }

    /// <summary>Projectile type key (resolves to a projectile definition).</summary>
    public string ProjectileType { get; set; } = "default";

    /// <summary>Seconds remaining until this weapon can fire again.</summary>
    public float Cooldown { get; set; }

    /// <summary>Returns <c>true</c> when the weapon is ready to fire.</summary>
    public bool IsReady => Cooldown <= 0f;
}
