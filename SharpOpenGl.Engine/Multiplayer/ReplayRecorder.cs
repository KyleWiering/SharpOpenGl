namespace SharpOpenGl.Engine.Multiplayer;

/// <summary>
/// Records a stream of <see cref="IGameCommand"/> instances during a live game session
/// so that the session can be replayed identically later via <see cref="ReplayPlayer"/>.
/// </summary>
/// <remarks>
/// Thread-safe — commands may be recorded from any thread (e.g. network receive thread).
/// The recording is tagged with a <see cref="Seed"/> so the replay can restore the same
/// pseudo-random state as the original session.
/// </remarks>
public sealed class ReplayRecorder
{
    private readonly List<ReplayEntry> _entries = new();
    private readonly object _lock = new();
    private bool _recording;

    /// <summary>Seed used to initialise any pseudo-random systems at session start.</summary>
    public int Seed { get; }

    /// <summary>Tick at which recording started (usually 0 for a new game).</summary>
    public long StartTick { get; private set; }

    /// <summary>Tick at which <see cref="Stop"/> was called, or -1 if still recording.</summary>
    public long EndTick { get; private set; } = -1;

    /// <summary>Whether the recorder is actively accepting commands.</summary>
    public bool IsRecording => _recording;

    /// <param name="seed">Pseudo-random seed for the session.</param>
    public ReplayRecorder(int seed = 0)
    {
        Seed = seed;
    }

    /// <summary>Begin recording. Must be called before <see cref="Record"/>.</summary>
    public void Start(long startTick = 0)
    {
        lock (_lock)
        {
            _entries.Clear();
            StartTick = startTick;
            EndTick = -1;
            _recording = true;
        }
    }

    /// <summary>Stop recording and seal the replay data.</summary>
    public void Stop(long endTick)
    {
        lock (_lock)
        {
            _recording = false;
            EndTick = endTick;
        }
    }

    /// <summary>
    /// Record a command. Silently ignored when not recording.
    /// </summary>
    public void Record(IGameCommand command)
    {
        lock (_lock)
        {
            if (!_recording) return;
            _entries.Add(new ReplayEntry(command.Tick, CommandSerializer.Serialize(command)));
        }
    }

    /// <summary>
    /// Export the recorded entries as an ordered, immutable snapshot.
    /// </summary>
    public ReplayData Export()
    {
        lock (_lock)
        {
            return new ReplayData(Seed, StartTick, EndTick, _entries.ToArray());
        }
    }
}

/// <summary>A single timestamped command entry within a replay.</summary>
/// <param name="Tick">Simulation tick at which the command was issued.</param>
/// <param name="CommandJson">JSON-serialised form of the command.</param>
public sealed record ReplayEntry(long Tick, string CommandJson);

/// <summary>Immutable snapshot of a full recorded session, ready for playback.</summary>
/// <param name="Seed">Pseudo-random seed used when the session was recorded.</param>
/// <param name="StartTick">First tick of the recording.</param>
/// <param name="EndTick">Last tick of the recording (-1 if recording was not stopped).</param>
/// <param name="Entries">Ordered array of all recorded commands.</param>
public sealed record ReplayData(int Seed, long StartTick, long EndTick, ReplayEntry[] Entries);
