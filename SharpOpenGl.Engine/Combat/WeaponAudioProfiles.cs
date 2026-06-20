using SharpOpenGl.Engine.Audio;

namespace SharpOpenGl.Engine.Combat;

/// <summary>Maps weapon type strings to logical audio events for combat SFX.</summary>
public static class WeaponAudioProfiles
{
    public static AudioEventType FireSoundFor(string weaponType) => weaponType.ToLowerInvariant() switch
    {
        "missile" or "torpedo" or "rocket" or "bomb" => AudioEventType.WeaponLaunch,
        "wave" => AudioEventType.ShieldHit,
        _ => AudioEventType.WeaponFire,
    };

    public static AudioEventType ImpactSoundFor(string weaponType) => weaponType.ToLowerInvariant() switch
    {
        "missile" or "torpedo" or "rocket" or "bomb" or "wave" => AudioEventType.Explosion,
        _ => AudioEventType.WeaponFire,
    };
}