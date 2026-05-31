using SharpOpenGl.Engine.Audio;
using SharpOpenGl.Engine.Events;
using OpenTK.Mathematics;
using Xunit;

namespace SharpOpenGl.Tests.Audio;

public class AudioSettingsTests
{
    [Fact]
    public void Default_volumes_are_in_range()
    {
        var s = new AudioSettings();
        Assert.InRange(s.MasterVolume, 0f, 1f);
        Assert.InRange(s.SfxVolume,    0f, 1f);
        Assert.InRange(s.MusicVolume,  0f, 1f);
    }

    [Theory]
    [InlineData(-1f, 0f)]
    [InlineData(2f,  1f)]
    [InlineData(0.5f, 0.5f)]
    public void MasterVolume_clamps_to_0_1(float input, float expected)
    {
        var s = new AudioSettings { MasterVolume = input };
        Assert.Equal(expected, s.MasterVolume);
    }

    [Theory]
    [InlineData(-0.1f, 0f)]
    [InlineData(1.1f,  1f)]
    [InlineData(0.8f, 0.8f)]
    public void SfxVolume_clamps_to_0_1(float input, float expected)
    {
        var s = new AudioSettings { SfxVolume = input };
        Assert.Equal(expected, s.SfxVolume);
    }

    [Theory]
    [InlineData(-0.5f, 0f)]
    [InlineData(1.5f,  1f)]
    [InlineData(0.3f, 0.3f)]
    public void MusicVolume_clamps_to_0_1(float input, float expected)
    {
        var s = new AudioSettings { MusicVolume = input };
        Assert.Equal(expected, s.MusicVolume);
    }

    [Fact]
    public void EffectiveSfxGain_is_product_of_master_and_sfx()
    {
        var s = new AudioSettings { MasterVolume = 0.5f, SfxVolume = 0.8f };
        Assert.Equal(0.5f * 0.8f, s.EffectiveSfxGain, precision: 5);
    }

    [Fact]
    public void EffectiveMusicGain_is_product_of_master_and_music()
    {
        var s = new AudioSettings { MasterVolume = 0.6f, MusicVolume = 0.7f };
        Assert.Equal(0.6f * 0.7f, s.EffectiveMusicGain, precision: 5);
    }
}

public class NullAudioManagerTests
{
    private readonly NullAudioManager _audio = new();

    [Fact]
    public void PlaySound_does_not_throw()
    {
        _audio.PlaySound(AudioEventType.UIClick);
        _audio.PlaySound(AudioEventType.Explosion, new Vector3(10, 0, 5));
    }

    [Fact]
    public void PlayMusic_does_not_throw()
    {
        _audio.PlayMusic("menu_theme");
    }

    [Fact]
    public void StopMusic_does_not_throw()
    {
        _audio.StopMusic();
        _audio.StopMusic(0f);
    }

    [Fact]
    public void Update_does_not_throw()
    {
        _audio.Update(0.016f);
    }

    [Fact]
    public void SetListenerTransform_does_not_throw()
    {
        _audio.SetListenerTransform(Vector3.Zero, -Vector3.UnitZ, Vector3.UnitY);
    }

    [Fact]
    public void SetMasterVolume_updates_settings()
    {
        _audio.SetMasterVolume(0.3f);
        Assert.Equal(0.3f, _audio.Settings.MasterVolume);
    }

    [Fact]
    public void SetSfxVolume_updates_settings()
    {
        _audio.SetSfxVolume(0.6f);
        Assert.Equal(0.6f, _audio.Settings.SfxVolume);
    }

    [Fact]
    public void SetMusicVolume_updates_settings()
    {
        _audio.SetMusicVolume(0.9f);
        Assert.Equal(0.9f, _audio.Settings.MusicVolume);
    }

    [Fact]
    public void Dispose_does_not_throw()
    {
        using var audio = new NullAudioManager();
        audio.PlaySound(AudioEventType.WeaponFire);
    }

    [Fact]
    public void Settings_are_not_null()
    {
        Assert.NotNull(_audio.Settings);
    }
}

public class PlaceholderSoundGeneratorTests
{
    [Fact]
    public void GenerateTone_returns_correct_length()
    {
        float duration = 0.1f;
        short[] samples = PlaceholderSoundGenerator.GenerateTone(440f, duration);
        int expected = (int)(PlaceholderSoundGenerator.SampleRate * duration);
        Assert.Equal(expected, samples.Length);
    }

    [Fact]
    public void GenerateNoise_returns_correct_length()
    {
        float duration = 0.2f;
        short[] samples = PlaceholderSoundGenerator.GenerateNoise(duration);
        int expected = (int)(PlaceholderSoundGenerator.SampleRate * duration);
        Assert.Equal(expected, samples.Length);
    }

    [Fact]
    public void GenerateSweep_returns_correct_length()
    {
        float duration = 0.15f;
        short[] samples = PlaceholderSoundGenerator.GenerateSweep(800f, 200f, duration);
        int expected = (int)(PlaceholderSoundGenerator.SampleRate * duration);
        Assert.Equal(expected, samples.Length);
    }

    [Theory]
    [InlineData(AudioEventType.WeaponFire)]
    [InlineData(AudioEventType.Explosion)]
    [InlineData(AudioEventType.UIClick)]
    [InlineData(AudioEventType.MissionComplete)]
    [InlineData(AudioEventType.EngineIdle)]
    public void GetPlaceholder_returns_non_empty_buffer(AudioEventType type)
    {
        short[] samples = PlaceholderSoundGenerator.GetPlaceholder(type);
        Assert.NotEmpty(samples);
    }

    [Fact]
    public void GetPlaceholder_all_types_return_data()
    {
        foreach (AudioEventType type in Enum.GetValues<AudioEventType>())
        {
            short[] samples = PlaceholderSoundGenerator.GetPlaceholder(type);
            Assert.NotEmpty(samples);
        }
    }

    [Fact]
    public void GenerateTone_samples_within_short_range()
    {
        short[] samples = PlaceholderSoundGenerator.GenerateTone(440f, 0.05f);
        foreach (short s in samples)
            Assert.InRange((int)s, short.MinValue, short.MaxValue);
    }
}

public class AudioEventTests
{
    [Fact]
    public void SoundRequestedEvent_stores_fields()
    {
        var pos = new Vector3(1f, 2f, 3f);
        var evt = new SoundRequestedEvent(AudioEventType.WeaponFire, pos);
        Assert.Equal(AudioEventType.WeaponFire, evt.EventType);
        Assert.Equal(pos, evt.WorldPosition);
    }

    [Fact]
    public void MusicRequestedEvent_defaults_loop_true()
    {
        var evt = new MusicRequestedEvent("battle_theme");
        Assert.True(evt.Loop);
        Assert.Equal(1.0f, evt.CrossfadeSeconds);
    }

    [Fact]
    public void VolumeChangedEvent_stores_fields()
    {
        var evt = new VolumeChangedEvent("Master", 0.75f);
        Assert.Equal("Master", evt.Channel);
        Assert.Equal(0.75f, evt.NewValue);
    }
}
