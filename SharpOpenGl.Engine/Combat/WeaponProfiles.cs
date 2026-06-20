using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;

namespace SharpOpenGl.Engine.Combat;

/// <summary>Distinct projectile appearance per weapon family.</summary>
public enum WeaponVisualKind
{
    LaserBolt,
    Beam,
    Torpedo,
    Rocket,
    Bomb,
    EnergyPulse,
    Wave,
}

/// <summary>Resolved travel, visuals, and tuning for a weapon type string.</summary>
public sealed record WeaponProfile(
    ProjectileType Motion,
    WeaponVisualKind Visual,
    float Speed,
    float Lifetime,
    float BlastRadius,
    Vector4 Color,
    float Scale);

/// <summary>Maps weapon type strings from GameData to motion and visuals.</summary>
public static class WeaponProfiles
{
    public static WeaponProfile Resolve(WeaponComponent weapon)
    {
        var profile = ForWeaponType(weapon.Type);
        if (IsExplicitProjectileOverride(weapon.ProjectileType))
            profile = profile with { Motion = ParseMotion(weapon.ProjectileType) };
        return profile;
    }

    public static string DefaultProjectileTypeKey(string weaponType) =>
        MotionToKey(ForWeaponType(weaponType).Motion);

    public static string MeshKey(WeaponVisualKind visual) => visual switch
    {
        WeaponVisualKind.LaserBolt => "projectile/laser_bolt",
        WeaponVisualKind.Beam => "projectile/beam",
        WeaponVisualKind.Torpedo => "projectile/torpedo",
        WeaponVisualKind.Rocket => "projectile/rocket",
        WeaponVisualKind.Bomb => "projectile/bomb",
        WeaponVisualKind.EnergyPulse => "projectile/energy_pulse",
        WeaponVisualKind.Wave => "projectile/wave",
        _ => "projectile/laser_bolt",
    };

    private static bool IsExplicitProjectileOverride(string projectileType) =>
        !string.IsNullOrWhiteSpace(projectileType)
        && !string.Equals(projectileType, "default", StringComparison.OrdinalIgnoreCase);

    private static ProjectileType ParseMotion(string projectileType) => projectileType.ToLowerInvariant() switch
    {
        "homing" or "missile" or "torpedo" => ProjectileType.Homing,
        "aoe" or "bomb" or "wave" => ProjectileType.AoE,
        "instant" or "beam" => ProjectileType.Instant,
        _ => ProjectileType.Linear,
    };

    private static string MotionToKey(ProjectileType motion) => motion switch
    {
        ProjectileType.Homing => "homing",
        ProjectileType.AoE => "aoe",
        ProjectileType.Instant => "instant",
        _ => "linear",
    };

    private static WeaponProfile ForWeaponType(string type) => type.ToLowerInvariant() switch
    {
        "laser" or "flak" or "point_defense" or "turret" or "station_laser" =>
            new(ProjectileType.Linear, WeaponVisualKind.LaserBolt, 900f, 2.2f, 0f,
                new Vector4(1f, 0.35f, 0.25f, 1f), 0.55f),

        "beam" or "laser_beam" or "railgun" =>
            new(ProjectileType.Linear, WeaponVisualKind.Beam, 1400f, 1.4f, 0f,
                new Vector4(0.45f, 0.85f, 1f, 1f), 0.7f),

        "torpedo" =>
            new(ProjectileType.Homing, WeaponVisualKind.Torpedo, 160f, 9f, 0f,
                new Vector4(0.75f, 0.8f, 0.85f, 1f), 1.1f),

        "missile" or "rocket" =>
            new(ProjectileType.Homing, WeaponVisualKind.Rocket, 300f, 7f, 0f,
                new Vector4(1f, 0.65f, 0.15f, 1f), 0.85f),

        "bomb" or "mega_bomb" =>
            new(ProjectileType.AoE, WeaponVisualKind.Bomb, 110f, 12f, 28f,
                new Vector4(0.95f, 0.45f, 0.1f, 1f), 1.6f),

        "cannon" or "plasma" or "pulse" =>
            new(ProjectileType.Linear, WeaponVisualKind.EnergyPulse, 520f, 3.5f, 0f,
                new Vector4(0.55f, 0.35f, 1f, 1f), 0.95f),

        "wave" or "emp" or "disruptor" =>
            new(ProjectileType.AoE, WeaponVisualKind.Wave, 220f, 5f, 32f,
                new Vector4(0.35f, 1f, 0.85f, 1f), 1.4f),

        _ =>
            new(ProjectileType.Linear, WeaponVisualKind.LaserBolt, 700f, 2.5f, 0f,
                new Vector4(1f, 0.5f, 0.3f, 1f), 0.6f),
    };
}