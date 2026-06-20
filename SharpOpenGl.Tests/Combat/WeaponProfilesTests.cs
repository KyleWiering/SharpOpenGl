using OpenTK.Mathematics;
using SharpOpenGl.Engine.Combat;
using SharpOpenGl.Engine.ECS;
using Xunit;

namespace SharpOpenGl.Tests.Combat;

public class WeaponProfilesTests
{
    [Theory]
    [InlineData("laser", WeaponVisualKind.LaserBolt)]
    [InlineData("beam", WeaponVisualKind.Beam)]
    [InlineData("torpedo", WeaponVisualKind.Torpedo)]
    [InlineData("missile", WeaponVisualKind.Rocket)]
    [InlineData("bomb", WeaponVisualKind.Bomb)]
    [InlineData("cannon", WeaponVisualKind.EnergyPulse)]
    [InlineData("wave", WeaponVisualKind.Wave)]
    public void Resolve_maps_weapon_types_to_visuals(string weaponType, WeaponVisualKind expected)
    {
        var weapon = new WeaponComponent { Type = weaponType, ProjectileType = "default" };
        var profile = WeaponProfiles.Resolve(weapon);
        Assert.Equal(expected, profile.Visual);
    }

    [Fact]
    public void DefaultProjectileTypeKey_uses_linear_for_laser()
    {
        Assert.Equal("linear", WeaponProfiles.DefaultProjectileTypeKey("laser"));
    }

    [Fact]
    public void Resolve_homing_override_changes_motion_only()
    {
        var weapon = new WeaponComponent { Type = "laser", ProjectileType = "homing" };
        var profile = WeaponProfiles.Resolve(weapon);
        Assert.Equal(ProjectileType.Homing, profile.Motion);
        Assert.Equal(WeaponVisualKind.LaserBolt, profile.Visual);
    }

    [Fact]
    public void MeshKey_returns_stable_projectile_keys()
    {
        Assert.StartsWith("projectile/", WeaponProfiles.MeshKey(WeaponVisualKind.Rocket));
    }
}