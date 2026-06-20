using SharpOpenGl.Engine.Audio;
using SharpOpenGl.Engine.Combat;
using Xunit;

namespace SharpOpenGl.Tests.Combat;

public class WeaponAudioProfilesTests
{
    [Theory]
    [InlineData("laser", AudioEventType.WeaponFire)]
    [InlineData("cannon", AudioEventType.WeaponFire)]
    [InlineData("missile", AudioEventType.WeaponLaunch)]
    [InlineData("torpedo", AudioEventType.WeaponLaunch)]
    [InlineData("bomb", AudioEventType.WeaponLaunch)]
    [InlineData("wave", AudioEventType.ShieldHit)]
    public void FireSoundFor_maps_weapon_types(string weaponType, AudioEventType expected)
    {
        Assert.Equal(expected, WeaponAudioProfiles.FireSoundFor(weaponType));
    }
}