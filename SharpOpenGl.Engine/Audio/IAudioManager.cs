using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Audio;

/// <summary>
/// Platform-agnostic audio manager.  Implementations provide sound-effect
/// playback (positional, pooled) and background music (looping, crossfade).
/// A null-safe <see cref="NullAudioManager"/> is available for headless or
/// test environments where no audio device is present.
/// </summary>
public interface IAudioManager : IDisposable
{
    /// <summary>Current volume settings. Modify to change volumes at runtime.</summary>
    AudioSettings Settings { get; }

    /// <summary>
    /// Play a one-shot sound effect at the given world-space position.
    /// Positional audio is relative to the listener (usually the camera).
    /// Pass <see cref="Vector3.Zero"/> for non-positional (UI) sounds.
    /// </summary>
    void PlaySound(AudioEventType eventType, Vector3 worldPosition = default);

    /// <summary>
    /// Start playing a music track identified by <paramref name="trackId"/>.
    /// If a track is already playing it will fade out over
    /// <paramref name="crossfadeSeconds"/> before the new one begins.
    /// </summary>
    void PlayMusic(string trackId, bool loop = true, float crossfadeSeconds = 1.0f);

    /// <summary>
    /// Stop the currently playing music.
    /// </summary>
    /// <param name="fadeOutSeconds">Time over which the music fades to silence.</param>
    void StopMusic(float fadeOutSeconds = 1.0f);

    /// <summary>
    /// Update internal state (fade timers, voice management).
    /// Call once per game frame.
    /// </summary>
    void Update(float deltaTime);

    /// <summary>
    /// Set the listener position and orientation for 3-D positional audio.
    /// Call every frame with the camera world position.
    /// </summary>
    void SetListenerTransform(Vector3 position, Vector3 forward, Vector3 up);

    /// <summary>
    /// Apply master volume change immediately (0–1).
    /// Shorthand for <c>Settings.MasterVolume = value; ApplySettings();</c>
    /// </summary>
    void SetMasterVolume(float volume);

    /// <summary>Apply SFX volume change immediately (0–1).</summary>
    void SetSfxVolume(float volume);

    /// <summary>Apply music volume change immediately (0–1).</summary>
    void SetMusicVolume(float volume);
}
