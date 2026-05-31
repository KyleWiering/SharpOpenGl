namespace SharpOpenGl.Engine.Multiplayer;

/// <summary>
/// Fixed-timestep clock that drives the deterministic simulation loop.
/// Accumulates real elapsed time and produces whole simulation ticks at a
/// constant rate, decoupling render frequency from game logic frequency.
/// This eliminates floating-point drift caused by variable <c>deltaTime</c> accumulation.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// var clock = new DeterministicClock(ticksPerSecond: 20);
/// // each render frame:
/// clock.Advance(realDeltaSeconds);
/// while (clock.HasPendingTick)
/// {
///     long tick = clock.ConsumeTick();
///     SimulateOneTick(tick, clock.TickDuration);
/// }
/// </code>
/// </remarks>
public sealed class DeterministicClock
{
    private readonly double _tickDuration;   // seconds per simulation tick
    private double _totalElapsed;
    private long _currentTick;

    /// <summary>Number of simulation ticks executed per real second.</summary>
    public int TicksPerSecond { get; }

    /// <summary>Fixed duration of one simulation tick in seconds.</summary>
    public double TickDuration => _tickDuration;

    /// <summary>The index of the last fully consumed simulation tick.</summary>
    public long CurrentTick => _currentTick;

    /// <summary>
    /// <c>true</c> when at least one whole simulation tick has accumulated and is
    /// ready to be consumed with <see cref="ConsumeTick"/>.
    /// </summary>
    public bool HasPendingTick => TicksDue > _currentTick;

    // Total ticks that should have elapsed based on total real time — avoids floating-point drift.
    // A small epsilon prevents 0.15/0.05 from computing as 2.9999... instead of 3.
    private long TicksDue => (long)(_totalElapsed / _tickDuration + 1e-9);

    /// <param name="ticksPerSecond">
    /// How many simulation ticks advance per real second.
    /// Typical values: 20 (RTS standard), 30, 60.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="ticksPerSecond"/> is &lt; 1.</exception>
    public DeterministicClock(int ticksPerSecond = 20)
    {
        if (ticksPerSecond < 1)
            throw new ArgumentOutOfRangeException(nameof(ticksPerSecond), "Must be >= 1.");

        TicksPerSecond = ticksPerSecond;
        _tickDuration = 1.0 / ticksPerSecond;
    }

    /// <summary>
    /// Feed real elapsed time into the accumulator.
    /// Call once per render frame with the frame's delta time.
    /// </summary>
    /// <param name="realDeltaSeconds">Real-world elapsed seconds since the last call.</param>
    public void Advance(double realDeltaSeconds)
    {
        if (realDeltaSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(realDeltaSeconds), "Delta time cannot be negative.");
        _totalElapsed += realDeltaSeconds;
    }

    /// <summary>
    /// Consume one pending simulation tick and return its tick index.
    /// The accumulator is decremented by exactly <see cref="TickDuration"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no tick is pending.</exception>
    public long ConsumeTick()
    {
        if (!HasPendingTick)
            throw new InvalidOperationException("No pending tick to consume. Check HasPendingTick first.");

        return ++_currentTick;
    }

    /// <summary>
    /// Reset the clock to its initial state (tick 0, empty accumulator).
    /// Call at the start of a new game or replay.
    /// </summary>
    public void Reset()
    {
        _totalElapsed = 0;
        _currentTick = 0;
    }

    /// <summary>
    /// Interpolation alpha [0, 1) for smooth rendering between simulation ticks.
    /// Pass this to your renderer to lerp entity positions.
    /// </summary>
    public double RenderAlpha
    {
        get
        {
            double remainder = _totalElapsed - _currentTick * _tickDuration;
            return remainder / _tickDuration;
        }
    }
}
