using OpenTK.Mathematics;
using SharpOpenGl.Engine.Config;
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
        return profile with { Scale = CombatBalance.ScaleProjectile(profile.Scale) };
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

    /// <summary>Distinct trail tint per visual kind for particle streaks.</summary>
    public static Vector4 TrailColor(WeaponVisualKind visual) =>
        TrailColor(visual, ProjectileType.Linear);

    /// <summary>
    /// Trail tint with homing-specific seeker heat — torpedoes run cold ion blue,
    /// rockets run hot exhaust orange so homing shots read at RTS zoom.
    /// </summary>
    public static Vector4 TrailColor(WeaponVisualKind visual, ProjectileType motion)
    {
        if (motion is ProjectileType.Homing or ProjectileType.AoE)
        {
            return visual switch
            {
                WeaponVisualKind.Torpedo => new Vector4(0.42f, 0.88f, 1f, 1f),
                WeaponVisualKind.Rocket => new Vector4(1f, 0.34f, 0.04f, 1f),
                WeaponVisualKind.Bomb => new Vector4(1f, 0.48f, 0.08f, 1f),
                _ => TrailColor(visual),
            };
        }

        return visual switch
        {
            WeaponVisualKind.LaserBolt => new Vector4(0.25f, 0.95f, 1f, 1f),
            WeaponVisualKind.Beam => new Vector4(0.4f, 0.82f, 1f, 1f),
            WeaponVisualKind.Torpedo => new Vector4(0.78f, 0.86f, 0.98f, 1f),
            WeaponVisualKind.Rocket => new Vector4(1f, 0.52f, 0.08f, 1f),
            WeaponVisualKind.Bomb => new Vector4(1f, 0.42f, 0.05f, 1f),
            WeaponVisualKind.EnergyPulse => new Vector4(0.82f, 0.32f, 1f, 1f),
            WeaponVisualKind.Wave => new Vector4(0.2f, 1f, 0.72f, 1f),
            _ => new Vector4(1f, 0.55f, 0.25f, 1f),
        };
    }

    /// <summary>
    /// Warms homing trail tint as lifetime elapses — simulates seeker lock heat without mesh uploads.
    /// </summary>
    public static Vector4 HomingTrailSteerTint(Vector4 baseTint, float lifetimeRatio, WeaponVisualKind visual)
    {
        float heat = Math.Clamp(1f - lifetimeRatio, 0f, 1f);
        float steerBoost = visual switch
        {
            WeaponVisualKind.Torpedo => 0.18f * heat,
            WeaponVisualKind.Rocket => 0.28f * heat,
            _ => 0.12f * heat,
        };

        return new Vector4(
            MathF.Min(1f, baseTint.X + steerBoost),
            MathF.Max(0f, baseTint.Y - steerBoost * 0.35f),
            MathF.Max(0f, baseTint.Z - steerBoost * 0.55f),
            baseTint.W);
    }

    private static WeaponProfile ForWeaponType(string type) => type.ToLowerInvariant() switch
    {
        "laser" or "turret" or "station_laser" =>
            new(ProjectileType.Linear, WeaponVisualKind.LaserBolt, 900f, 2.2f, 0f,
                new Vector4(0.2f, 0.95f, 1f, 1f), 1f),

        "flak" or "point_defense" =>
            new(ProjectileType.Linear, WeaponVisualKind.LaserBolt, 820f, 2f, 0f,
                new Vector4(1f, 0.32f, 0.12f, 1f), 0.95f),

        "beam" or "laser_beam" or "railgun" =>
            new(ProjectileType.Linear, WeaponVisualKind.Beam, 1400f, 1.4f, 0f,
                new Vector4(0.35f, 0.78f, 1f, 1f), 1.15f),

        "torpedo" =>
            new(ProjectileType.Homing, WeaponVisualKind.Torpedo, 160f, 9f, 0f,
                new Vector4(0.75f, 0.82f, 0.95f, 1f), 1.4f),

        "missile" or "rocket" =>
            new(ProjectileType.Homing, WeaponVisualKind.Rocket, 300f, 7f, 0f,
                new Vector4(1f, 0.55f, 0.08f, 1f), 1.2f),

        "bomb" or "mega_bomb" =>
            new(ProjectileType.AoE, WeaponVisualKind.Bomb, 110f, 12f, 28f,
                new Vector4(1f, 0.42f, 0.05f, 1f), 2f),

        "cannon" or "plasma" or "pulse" =>
            new(ProjectileType.Linear, WeaponVisualKind.EnergyPulse, 520f, 3.5f, 0f,
                new Vector4(0.82f, 0.32f, 1f, 1f), 1.25f),

        "wave" or "emp" or "disruptor" =>
            new(ProjectileType.AoE, WeaponVisualKind.Wave, 220f, 5f, 32f,
                new Vector4(0.2f, 1f, 0.72f, 1f), 1.75f),

        _ =>
            new(ProjectileType.Linear, WeaponVisualKind.LaserBolt, 700f, 2.5f, 0f,
                new Vector4(1f, 0.58f, 0.28f, 1f), 0.95f),
    };
}