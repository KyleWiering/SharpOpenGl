using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Audio;

/// <summary>
/// No-op audio manager used in headless and test environments.
/// All calls succeed silently without touching any audio hardware.
/// </summary>
public sealed class NullAudioManager : IAudioManager
{
    /// <inheritdoc/>
    public AudioSettings Settings { get; } = new AudioSettings();

    /// <inheritdoc/>
    public void PlaySound(AudioEventType eventType, Vector3 worldPosition = default) { }

    /// <inheritdoc/>
    public void PlayMusic(string trackId, bool loop = true, float crossfadeSeconds = 1.0f) { }

    /// <inheritdoc/>
    public void StopMusic(float fadeOutSeconds = 1.0f) { }

    /// <inheritdoc/>
    public void Update(float deltaTime) { }

    /// <inheritdoc/>
    public void SetListenerTransform(Vector3 position, Vector3 forward, Vector3 up) { }

    /// <inheritdoc/>
    public void SetMasterVolume(float volume) => Settings.MasterVolume = volume;

    /// <inheritdoc/>
    public void SetSfxVolume(float volume) => Settings.SfxVolume = volume;

    /// <inheritdoc/>
    public void SetMusicVolume(float volume) => Settings.MusicVolume = volume;

    /// <inheritdoc/>
    public void Dispose() { }
}
