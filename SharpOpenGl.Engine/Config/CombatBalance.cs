namespace SharpOpenGl.Engine.Config;

/// <summary>Runtime combat tuning loaded from <c>GameData/Config/balance.json</c>.</summary>
public static class CombatBalance
{
    public static float WeaponRangeMultiplier { get; private set; } = 0.6f;
    public static float ProjectileScaleMultiplier { get; private set; } = 1.5f;

    public static void Apply(CombatTuningConfig? config)
    {
        if (config == null) return;
        if (config.GlobalWeaponRangeMultiplier > 0f)
            WeaponRangeMultiplier = config.GlobalWeaponRangeMultiplier;
        if (config.GlobalProjectileScaleMultiplier > 0f)
            ProjectileScaleMultiplier = config.GlobalProjectileScaleMultiplier;
    }

    public static float ScaleRange(float range) => range * WeaponRangeMultiplier;

    public static float ScaleProjectile(float scale) => scale * ProjectileScaleMultiplier;

    public static void ResetForTests()
    {
        WeaponRangeMultiplier = 0.6f;
        ProjectileScaleMultiplier = 1.5f;
    }
}

public sealed class CombatTuningConfig
{
    public float GlobalWeaponRangeMultiplier { get; set; } = 0.6f;
    public float GlobalProjectileScaleMultiplier { get; set; } = 1.5f;
}