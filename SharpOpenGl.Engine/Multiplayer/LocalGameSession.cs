namespace SharpOpenGl.Engine.Multiplayer;

/// <summary>
/// Hosts two independent <see cref="CommandQueue"/> instances sharing a single
/// <see cref="DeterministicClock"/>. Used for local split-testing: both "players"
/// issue commands that are serialised, transmitted through the shared clock, and
/// executed by their respective simulations — verifying that the command + replay
/// pipeline is deterministic without requiring a real network connection.
/// </summary>
/// <remarks>
/// Typical usage:
/// <code>
/// var session = new LocalGameSession(ticksPerSecond: 20, seed: 42);
/// session.Start();
///
/// // Issue commands as player 0 and player 1:
/// session.IssueCommand(new MoveCommand { PlayerId = 0, Tick = session.CurrentTick + 1, ... });
/// session.IssueCommand(new MoveCommand { PlayerId = 1, Tick = session.CurrentTick + 1, ... });
///
/// // Each render frame:
/// session.Advance(deltaSeconds);
/// while (session.HasPendingTick)
/// {
///     long tick = session.ConsumeTick();
///     IGameCommand[] p0Cmds = session.DrainCommandsForPlayer(0, tick);
///     IGameCommand[] p1Cmds = session.DrainCommandsForPlayer(1, tick);
///     SimPlayer0(p0Cmds, tick);
///     SimPlayer1(p1Cmds, tick);
/// }
/// </code>
/// </remarks>
public sealed class LocalGameSession
{
    private readonly DeterministicClock _clock;
    private readonly CommandQueue[] _queues;
    private readonly ReplayRecorder[] _recorders;

    /// <summary>Number of local players (1 or 2).</summary>
    public int PlayerCount { get; }

    /// <summary>Pseudo-random seed for this session.</summary>
    public int Seed { get; }

    /// <summary>Whether the session has been started.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>Forwards to <see cref="DeterministicClock.CurrentTick"/>.</summary>
    public long CurrentTick => _clock.CurrentTick;

    /// <summary>Forwards to <see cref="DeterministicClock.HasPendingTick"/>.</summary>
    public bool HasPendingTick => _clock.HasPendingTick;

    /// <param name="ticksPerSecond">Simulation tick rate.</param>
    /// <param name="seed">Pseudo-random seed. Identical seeds produce identical sessions.</param>
    /// <param name="playerCount">1 or 2 local players.</param>
    public LocalGameSession(int ticksPerSecond = 20, int seed = 0, int playerCount = 2)
    {
        if (playerCount < 1 || playerCount > 2)
            throw new ArgumentOutOfRangeException(nameof(playerCount), "Must be 1 or 2.");

        Seed = seed;
        PlayerCount = playerCount;
        _clock = new DeterministicClock(ticksPerSecond);
        _queues = Enumerable.Range(0, playerCount).Select(_ => new CommandQueue()).ToArray();
        _recorders = Enumerable.Range(0, playerCount).Select(_ => new ReplayRecorder(seed)).ToArray();
    }

    /// <summary>Start the session and begin recording.</summary>
    public void Start()
    {
        _clock.Reset();
        foreach (var q in _queues) q.Clear();
        foreach (var r in _recorders) r.Start(0);
        IsRunning = true;
    }

    /// <summary>Stop the session and seal the replay recordings.</summary>
    public void Stop()
    {
        IsRunning = false;
        foreach (var r in _recorders) r.Stop(_clock.CurrentTick);
    }

    /// <summary>
    /// Advance the simulation clock by <paramref name="realDeltaSeconds"/>.
    /// Call once per render frame.
    /// </summary>
    public void Advance(double realDeltaSeconds) => _clock.Advance(realDeltaSeconds);

    /// <summary>
    /// Consume one pending simulation tick from the clock.
    /// </summary>
    public long ConsumeTick() => _clock.ConsumeTick();

    /// <summary>
    /// Issue a command on behalf of a player. The command is enqueued into that
    /// player's <see cref="CommandQueue"/> and also recorded for replay.
    /// </summary>
    public void IssueCommand(IGameCommand command)
    {
        int pid = command.PlayerId;
        if (pid < 0 || pid >= PlayerCount)
            throw new ArgumentOutOfRangeException(nameof(command), $"PlayerId {pid} out of range.");

        _queues[pid].Enqueue(command);
        _recorders[pid].Record(command);
    }

    /// <summary>
    /// Drain all commands for <paramref name="playerId"/> at or before <paramref name="tick"/>.
    /// </summary>
    public IGameCommand[] DrainCommandsForPlayer(int playerId, long tick)
    {
        if (playerId < 0 || playerId >= PlayerCount)
            throw new ArgumentOutOfRangeException(nameof(playerId));
        return _queues[playerId].DrainUpTo(tick);
    }

    /// <summary>Export the completed replay for a specific player.</summary>
    public ReplayData ExportReplay(int playerId)
    {
        if (playerId < 0 || playerId >= PlayerCount)
            throw new ArgumentOutOfRangeException(nameof(playerId));
        return _recorders[playerId].Export();
    }
}
