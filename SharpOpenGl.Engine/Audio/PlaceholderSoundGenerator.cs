namespace SharpOpenGl.Engine.Audio;

/// <summary>
/// Generates simple procedural PCM audio buffers for use as placeholder sound
/// effects when no asset files are present.  All output is 16-bit mono at the
/// specified sample rate.
/// </summary>
public static class PlaceholderSoundGenerator
{
    /// <summary>Default sample rate used for generated sounds (Hz).</summary>
    public const int SampleRate = 22050;

    /// <summary>
    /// Generate a sine-wave tone at <paramref name="frequency"/> Hz for
    /// <paramref name="durationSeconds"/> seconds.
    /// The amplitude envelope is an ADSR with hard-coded times suitable for
    /// one-shot SFX.
    /// </summary>
    /// <returns>16-bit PCM samples (little-endian, mono).</returns>
    public static short[] GenerateTone(float frequency, float durationSeconds,
                                       float attackSec = 0.01f, float releaseSec = 0.05f)
    {
        int totalSamples = (int)(SampleRate * durationSeconds);
        int attackSamples  = (int)(SampleRate * attackSec);
        int releaseSamples = (int)(SampleRate * releaseSec);

        short[] samples = new short[totalSamples];
        for (int i = 0; i < totalSamples; i++)
        {
            float t        = (float)i / SampleRate;
            float rawSine  = MathF.Sin(2f * MathF.PI * frequency * t);

            // Amplitude envelope
            float env;
            if (i < attackSamples)
                env = (float)i / attackSamples;
            else if (i >= totalSamples - releaseSamples)
                env = (float)(totalSamples - i) / releaseSamples;
            else
                env = 1f;

            samples[i] = (short)(rawSine * env * short.MaxValue * 0.7f);
        }
        return samples;
    }

    /// <summary>
    /// Generate white noise for the specified duration (used for explosions, static).
    /// </summary>
    public static short[] GenerateNoise(float durationSeconds, float releaseSec = 0.15f)
    {
        int totalSamples   = (int)(SampleRate * durationSeconds);
        int releaseSamples = (int)(SampleRate * releaseSec);
        var rng            = new Random(42); // deterministic

        short[] samples = new short[totalSamples];
        for (int i = 0; i < totalSamples; i++)
        {
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            float env   = i >= totalSamples - releaseSamples
                ? (float)(totalSamples - i) / releaseSamples
                : 1f;
            samples[i] = (short)(noise * env * short.MaxValue * 0.5f);
        }
        return samples;
    }

    /// <summary>
    /// Generate a frequency-swept tone (laser / power-up effect).
    /// The frequency glides from <paramref name="startHz"/> to
    /// <paramref name="endHz"/> linearly over the duration.
    /// </summary>
    public static short[] GenerateSweep(float startHz, float endHz,
                                        float durationSeconds, float releaseSec = 0.03f)
    {
        int totalSamples   = (int)(SampleRate * durationSeconds);
        int releaseSamples = (int)(SampleRate * releaseSec);

        short[] samples = new short[totalSamples];
        double phase = 0.0;
        for (int i = 0; i < totalSamples; i++)
        {
            float t    = (float)i / totalSamples;
            float freq = startHz + (endHz - startHz) * t;
            phase += 2.0 * Math.PI * freq / SampleRate;

            float env = i >= totalSamples - releaseSamples
                ? (float)(totalSamples - i) / releaseSamples
                : 1f;

            samples[i] = (short)(Math.Sin(phase) * env * short.MaxValue * 0.6f);
        }
        return samples;
    }

    /// <summary>
    /// Returns pre-made PCM data for the given <see cref="AudioEventType"/>.
    /// All samples are 16-bit mono at <see cref="SampleRate"/> Hz.
    /// </summary>
    public static short[] GetPlaceholder(AudioEventType type) => type switch
    {
        AudioEventType.WeaponFire     => GenerateSweep(800f, 200f, 0.12f),
        AudioEventType.WeaponLaunch   => GenerateSweep(400f, 150f, 0.18f),
        AudioEventType.Explosion      => GenerateNoise(0.45f, 0.20f),
        AudioEventType.ShieldHit      => GenerateSweep(1200f, 600f, 0.08f),
        AudioEventType.UnitMoveAck    => GenerateTone(660f, 0.05f),
        AudioEventType.UnitAttackAck  => GenerateTone(440f, 0.05f),
        AudioEventType.ResourceCollected => GenerateSweep(400f, 800f, 0.10f),
        AudioEventType.UIClick        => GenerateTone(1000f, 0.04f, 0.005f, 0.02f),
        AudioEventType.UIHover        => GenerateTone(800f, 0.03f, 0.005f, 0.015f),
        AudioEventType.MissionComplete => GenerateTone(523f, 0.6f, 0.02f, 0.3f),
        AudioEventType.MissionFail     => GenerateSweep(400f, 100f, 0.8f, 0.4f),
        AudioEventType.BuildingPlaced  => GenerateTone(330f, 0.08f),
        AudioEventType.EngineIdle      => GenerateNoise(0.5f, 0.05f),
        _                              => GenerateTone(440f, 0.05f),
    };
}
