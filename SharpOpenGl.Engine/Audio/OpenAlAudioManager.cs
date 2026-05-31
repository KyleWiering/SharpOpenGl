using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Audio;

/// <summary>
/// Desktop audio manager backed by OpenAL (via OpenTK).
/// Maintains a pool of <see cref="SourcePoolSize"/> AL sources for sound
/// effects and one dedicated streaming source for background music.
/// Falls back gracefully to <see cref="NullAudioManager"/> behaviour if no
/// audio device is available.
/// </summary>
public sealed class OpenAlAudioManager : IAudioManager
{
    private const int SourcePoolSize = 16;

    private readonly ALDevice  _device;
    private readonly ALContext _context;
    private readonly bool      _initialized;

    // SFX source pool
    private readonly int[] _sfxSources;
    private int _nextSource;

    // Per-event AL buffer (lazy-generated from PlaceholderSoundGenerator)
    private readonly Dictionary<AudioEventType, int> _buffers = new();

    // Music
    private int   _musicSource;
    private int   _musicBuffer;
    private bool  _musicPlaying;
    private float _musicFade;      // current gain factor for crossfade
    private float _musicFadeSpeed; // units per second (negative = fade-out)

    /// <inheritdoc/>
    public AudioSettings Settings { get; } = new AudioSettings();

    /// <summary>
    /// Create and initialise the OpenAL audio manager.
    /// If no audio device is available the manager operates silently.
    /// </summary>
    public OpenAlAudioManager()
    {
        try
        {
            _device  = ALC.OpenDevice(null);
            _context = ALC.CreateContext(_device, (int[]?)null);
            ALC.MakeContextCurrent(_context);

            _sfxSources = new int[SourcePoolSize];
            AL.GenSources(SourcePoolSize, _sfxSources);

            _musicSource = AL.GenSource();
            _musicBuffer = 0;

            _initialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Audio] OpenAL init failed ({ex.Message}); using silent mode.");
            _sfxSources  = Array.Empty<int>();
            _initialized = false;
        }
    }

    // ── IAudioManager ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void PlaySound(AudioEventType eventType, Vector3 worldPosition = default)
    {
        if (!_initialized) return;

        int buffer = GetOrCreateBuffer(eventType);
        int src    = NextSource();

        AL.Source(src, ALSourcei.Buffer, buffer);
        AL.Source(src, ALSourceb.Looping, false);
        AL.Source(src, ALSource3f.Position, worldPosition.X, worldPosition.Y, worldPosition.Z);
        AL.Source(src, ALSourcef.Gain, Settings.EffectiveSfxGain);
        AL.SourcePlay(src);
    }

    /// <inheritdoc/>
    public void PlayMusic(string trackId, bool loop = true, float crossfadeSeconds = 1.0f)
    {
        if (!_initialized) return;

        // Stop any current music with a quick fade-out then start new track.
        // For placeholder, we generate a low-frequency tone as the "music" track.
        if (_musicPlaying)
        {
            _musicFadeSpeed = crossfadeSeconds > 0f ? -1f / crossfadeSeconds : float.NegativeInfinity;
            return;
        }

        StartMusicTrack(loop);
    }

    /// <inheritdoc/>
    public void StopMusic(float fadeOutSeconds = 1.0f)
    {
        if (!_initialized || !_musicPlaying) return;

        _musicFadeSpeed = fadeOutSeconds > 0f ? -1f / fadeOutSeconds : float.NegativeInfinity;
    }

    /// <inheritdoc/>
    public void Update(float deltaTime)
    {
        if (!_initialized) return;

        if (_musicPlaying)
        {
            _musicFade = Math.Clamp(_musicFade + _musicFadeSpeed * deltaTime, 0f, 1f);
            AL.Source(_musicSource, ALSourcef.Gain, _musicFade * Settings.EffectiveMusicGain);

            if (_musicFade <= 0f)
            {
                AL.SourceStop(_musicSource);
                _musicPlaying = false;
            }
        }
    }

    /// <inheritdoc/>
    public void SetListenerTransform(Vector3 position, Vector3 forward, Vector3 up)
    {
        if (!_initialized) return;

        AL.Listener(ALListener3f.Position, position.X, position.Y, position.Z);
        float[] orientation = [forward.X, forward.Y, forward.Z, up.X, up.Y, up.Z];
        AL.Listener(ALListenerfv.Orientation, orientation);
    }

    /// <inheritdoc/>
    public void SetMasterVolume(float volume)
    {
        Settings.MasterVolume = volume;
        if (_initialized)
            AL.Listener(ALListenerf.Gain, Settings.MasterVolume);
    }

    /// <inheritdoc/>
    public void SetSfxVolume(float volume) => Settings.SfxVolume = volume;

    /// <inheritdoc/>
    public void SetMusicVolume(float volume)
    {
        Settings.MusicVolume = volume;
        if (_initialized && _musicPlaying)
            AL.Source(_musicSource, ALSourcef.Gain, _musicFade * Settings.EffectiveMusicGain);
    }

    // ── IDisposable ──────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_initialized) return;

        AL.SourceStop(_musicSource);
        AL.DeleteSource(_musicSource);

        if (_sfxSources.Length > 0)
        {
            AL.SourceStop(_sfxSources);
            AL.DeleteSources(_sfxSources);
        }

        foreach (int buf in _buffers.Values)
            AL.DeleteBuffer(buf);

        if (_musicBuffer != 0)
            AL.DeleteBuffer(_musicBuffer);

        ALC.MakeContextCurrent(ALContext.Null);
        ALC.DestroyContext(_context);
        ALC.CloseDevice(_device);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private int NextSource()
    {
        int src = _sfxSources[_nextSource % SourcePoolSize];
        _nextSource = (_nextSource + 1) % SourcePoolSize;
        // If still playing, stop it to reclaim the slot
        AL.GetSource(src, ALGetSourcei.SourceState, out int state);
        if ((ALSourceState)state == ALSourceState.Playing)
            AL.SourceStop(src);
        return src;
    }

    private int GetOrCreateBuffer(AudioEventType type)
    {
        if (_buffers.TryGetValue(type, out int existing))
            return existing;

        short[] pcm = PlaceholderSoundGenerator.GetPlaceholder(type);
        int buf = AL.GenBuffer();

        // Convert short[] to byte[] for OpenAL
        byte[] bytes = new byte[pcm.Length * 2];
        Buffer.BlockCopy(pcm, 0, bytes, 0, bytes.Length);

        AL.BufferData(buf, ALFormat.Mono16, bytes, PlaceholderSoundGenerator.SampleRate);
        _buffers[type] = buf;
        return buf;
    }

    private void StartMusicTrack(bool loop)
    {
        // Use a long low-frequency sweep as placeholder music
        short[] pcm  = PlaceholderSoundGenerator.GenerateTone(110f, 4.0f, 0.5f, 0.5f);
        byte[]  bytes = new byte[pcm.Length * 2];
        Buffer.BlockCopy(pcm, 0, bytes, 0, bytes.Length);

        if (_musicBuffer != 0) AL.DeleteBuffer(_musicBuffer);
        _musicBuffer = AL.GenBuffer();
        AL.BufferData(_musicBuffer, ALFormat.Mono16, bytes, PlaceholderSoundGenerator.SampleRate);

        AL.Source(_musicSource, ALSourcei.Buffer, _musicBuffer);
        AL.Source(_musicSource, ALSourceb.Looping, loop);
        AL.Source(_musicSource, ALSourcef.Gain, Settings.EffectiveMusicGain);
        AL.SourcePlay(_musicSource);

        _musicPlaying   = true;
        _musicFade      = 1f;
        _musicFadeSpeed = 0f;
    }
}
