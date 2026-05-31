namespace SharpOpenGl.Engine.Audio;

/// <summary>
/// Holds user-configurable audio volume settings.
/// All volume values are in the range [0, 1] where 1 is full volume.
/// </summary>
public sealed class AudioSettings
{
    private float _masterVolume = 1.0f;
    private float _sfxVolume    = 1.0f;
    private float _musicVolume  = 0.7f;

    /// <summary>Master output multiplier applied to all channels.</summary>
    public float MasterVolume
    {
        get => _masterVolume;
        set => _masterVolume = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>Volume multiplier for sound effects.</summary>
    public float SfxVolume
    {
        get => _sfxVolume;
        set => _sfxVolume = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>Volume multiplier for background music.</summary>
    public float MusicVolume
    {
        get => _musicVolume;
        set => _musicVolume = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>Mute all audio when the application window loses focus.</summary>
    public bool MuteOnFocusLoss { get; set; } = true;

    /// <summary>Effective SFX gain after applying master volume.</summary>
    public float EffectiveSfxGain   => MasterVolume * SfxVolume;

    /// <summary>Effective music gain after applying master volume.</summary>
    public float EffectiveMusicGain => MasterVolume * MusicVolume;
}
