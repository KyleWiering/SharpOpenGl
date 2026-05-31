namespace SharpOpenGl.Engine.Diagnostics;

/// <summary>
/// Tracks per-frame timing and computes rolling-average FPS and frame times.
/// </summary>
/// <remarks>
/// Call <see cref="BeginFrame"/> at the start of each frame and
/// <see cref="EndFrame"/> at the end.  Query <see cref="AverageFps"/>,
/// <see cref="AverageFrameMs"/>, and <see cref="PeakFrameMs"/> for results.
/// </remarks>
public sealed class PerformanceProfiler
{
    private readonly float[] _frameTimes;
    private int _head;
    private int _count;
    private long _frameStartTick;

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>
    /// Create a profiler with a rolling window of <paramref name="sampleCount"/> frames.
    /// </summary>
    /// <param name="sampleCount">Number of frames to average. Default is 60.</param>
    public PerformanceProfiler(int sampleCount = 60)
    {
        if (sampleCount < 1) throw new ArgumentOutOfRangeException(nameof(sampleCount));
        _frameTimes = new float[sampleCount];
    }

    // ── Per-frame ─────────────────────────────────────────────────────────────

    /// <summary>Record the start of a frame (high-resolution timestamp).</summary>
    public void BeginFrame()
    {
        _frameStartTick = System.Diagnostics.Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Record the end of a frame and add the elapsed milliseconds to the rolling
    /// window.  Call once per frame, after all update and draw work is complete.
    /// </summary>
    public void EndFrame()
    {
        long endTick = System.Diagnostics.Stopwatch.GetTimestamp();
        float ms = (endTick - _frameStartTick)
                   / (float)System.Diagnostics.Stopwatch.Frequency * 1000f;

        _frameTimes[_head] = ms;
        _head = (_head + 1) % _frameTimes.Length;
        if (_count < _frameTimes.Length) _count++;
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>Rolling-average frames-per-second. Returns 0 before the first sample.</summary>
    public float AverageFps
    {
        get
        {
            float avg = AverageFrameMs;
            return avg > 0f ? 1000f / avg : 0f;
        }
    }

    /// <summary>Rolling-average frame time in milliseconds.</summary>
    public float AverageFrameMs
    {
        get
        {
            if (_count == 0) return 0f;
            float sum = 0f;
            for (int i = 0; i < _count; i++)
                sum += _frameTimes[i];
            return sum / _count;
        }
    }

    /// <summary>Maximum frame time recorded in the current rolling window (ms).</summary>
    public float PeakFrameMs
    {
        get
        {
            if (_count == 0) return 0f;
            float peak = 0f;
            for (int i = 0; i < _count; i++)
                if (_frameTimes[i] > peak) peak = _frameTimes[i];
            return peak;
        }
    }

    /// <summary>
    /// Number of frames currently stored in the rolling window.
    /// Starts at 0 and grows until it equals the configured sample count.
    /// </summary>
    public int SampleCount => _count;

    /// <summary>Clear all accumulated samples.</summary>
    public void Reset()
    {
        Array.Clear(_frameTimes, 0, _frameTimes.Length);
        _head  = 0;
        _count = 0;
    }
}
