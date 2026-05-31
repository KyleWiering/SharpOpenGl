namespace SharpOpenGl.Engine.Multiplayer;

/// <summary>
/// Plays back a <see cref="ReplayData"/> recording by feeding commands into a
/// <see cref="CommandQueue"/> at exactly the ticks they were originally issued.
/// The caller is responsible for advancing the <see cref="DeterministicClock"/>
/// and calling <see cref="FeedTick"/> each simulation tick.
/// </summary>
public sealed class ReplayPlayer
{
    private readonly ReplayData _data;
    private int _cursor;

    /// <summary>The replay data being played back.</summary>
    public ReplayData Data => _data;

    /// <summary><c>true</c> when all commands have been fed into the queue.</summary>
    public bool IsFinished => _cursor >= _data.Entries.Length;

    /// <param name="data">The recorded session to play back.</param>
    public ReplayPlayer(ReplayData data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
    }

    /// <summary>
    /// Enqueue all commands whose tick equals <paramref name="tick"/> into
    /// <paramref name="queue"/>. Call once per simulation tick during replay.
    /// </summary>
    public void FeedTick(long tick, CommandQueue queue)
    {
        while (_cursor < _data.Entries.Length && _data.Entries[_cursor].Tick == tick)
        {
            IGameCommand cmd = CommandSerializer.Deserialize(_data.Entries[_cursor].CommandJson);
            queue.Enqueue(cmd);
            _cursor++;
        }
    }

    /// <summary>Reset playback to the beginning of the recording.</summary>
    public void Reset() => _cursor = 0;
}
