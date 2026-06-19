using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.Audio;

namespace SharpOpenGl.Audio;

/// <summary>
/// Desktop audio manager backed by OpenAL (via OpenTK).
/// </summary>
public sealed class OpenAlAudioManager : IAudioManager
{
    private const int SourcePoolSize = 16;

    private readonly ALDevice  _device;
    private readonly ALContext _context;
    private readonly bool      _initialized;

    private readonly int[] _sfxSources;
    private int _nextSource;

    private readonly Dictionary<AudioEventType, int> _buffers = new();

    private int   _musicSource;
    private int   _musicBuffer;
    private bool  _musicPlaying;
    private float _musicFade;
    private float _musicFadeSpeed;
    private bool? _pendingMusicLoop;

    public AudioSettings Settings { get; } = new AudioSettings();

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

    public void PlayMusic(string trackId, bool loop = true, float crossfadeSeconds = 1.0f)
    {
        if (!_initialized) return;

        if (_musicPlaying)
        {
            _musicFadeSpeed  = crossfadeSeconds > 0f ? -1f / crossfadeSeconds : float.NegativeInfinity;
            _pendingMusicLoop = loop;
            return;
        }

        StartMusicTrack(loop);
    }

    public void StopMusic(float fadeOutSeconds = 1.0f)
    {
        if (!_initialized || !_musicPlaying) return;

        _musicFadeSpeed = fadeOutSeconds > 0f ? -1f / fadeOutSeconds : float.NegativeInfinity;
    }

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

                if (_pendingMusicLoop.HasValue)
                {
                    StartMusicTrack(_pendingMusicLoop.Value);
                    _pendingMusicLoop = null;
                }
            }
        }
    }

    public void SetListenerTransform(Vector3 position, Vector3 forward, Vector3 up)
    {
        if (!_initialized) return;

        AL.Listener(ALListener3f.Position, position.X, position.Y, position.Z);
        float[] orientation = [forward.X, forward.Y, forward.Z, up.X, up.Y, up.Z];
        AL.Listener(ALListenerfv.Orientation, orientation);
    }

    public void SetMasterVolume(float volume)
    {
        Settings.MasterVolume = volume;
        if (_initialized)
            AL.Listener(ALListenerf.Gain, Settings.MasterVolume);
    }

    public void SetSfxVolume(float volume) => Settings.SfxVolume = volume;

    public void SetMusicVolume(float volume)
    {
        Settings.MusicVolume = volume;
        if (_initialized && _musicPlaying)
            AL.Source(_musicSource, ALSourcef.Gain, _musicFade * Settings.EffectiveMusicGain);
    }

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

    private int NextSource()
    {
        int src = _sfxSources[_nextSource % SourcePoolSize];
        _nextSource = (_nextSource + 1) % SourcePoolSize;
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

        byte[] bytes = new byte[pcm.Length * 2];
        Buffer.BlockCopy(pcm, 0, bytes, 0, bytes.Length);

        AL.BufferData(buf, ALFormat.Mono16, bytes, PlaceholderSoundGenerator.SampleRate);
        _buffers[type] = buf;
        return buf;
    }

    private void StartMusicTrack(bool loop)
    {
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