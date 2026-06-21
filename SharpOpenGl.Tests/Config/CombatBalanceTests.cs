using SharpOpenGl.Engine.Combat;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;
using Xunit;

namespace SharpOpenGl.Tests.Config;

public class CombatBalanceTests
{
    [Fact]
    public void Apply_reads_multipliers_from_config()
    {
        CombatBalance.ResetForTests();
        CombatBalance.Apply(new CombatTuningConfig
        {
            GlobalWeaponRangeMultiplier = 0.5f,
            GlobalProjectileScaleMultiplier = 2f,
        });

        Assert.Equal(0.5f, CombatBalance.WeaponRangeMultiplier);
        Assert.Equal(2f, CombatBalance.ProjectileScaleMultiplier);
    }

    [Fact]
    public void ScaleRange_and_ScaleProjectile_apply_multipliers()
    {
        CombatBalance.ResetForTests();
        Assert.Equal(120f, CombatBalance.ScaleRange(200f), 3);
        Assert.Equal(1.275f, CombatBalance.ScaleProjectile(0.85f), 3);
    }

    [Fact]
    public void WeaponProfiles_Resolve_applies_projectile_scale_multiplier()
    {
        CombatBalance.ResetForTests();
        CombatBalance.Apply(new CombatTuningConfig { GlobalProjectileScaleMultiplier = 2f });

        var profile = WeaponProfiles.Resolve(new WeaponComponent { Type = "laser", ProjectileType = "default" });
        Assert.True(profile.Scale > 1.5f);
    }
}