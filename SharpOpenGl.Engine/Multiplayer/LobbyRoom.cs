namespace SharpOpenGl.Engine.Multiplayer;

/// <summary>Current phase of a <see cref="LobbyRoom"/>.</summary>
public enum LobbyPhase
{
    /// <summary>Accepting players and waiting for all to be ready.</summary>
    Waiting,

    /// <summary>Countdown before the game starts.</summary>
    Starting,

    /// <summary>The game session is in progress.</summary>
    InGame,

    /// <summary>The session ended; the lobby is in a post-game state.</summary>
    PostGame,
}

/// <summary>
/// Manages player membership, readiness, and game-start coordination for one
/// multiplayer session. The host calls <see cref="TryStart"/> once all players
/// signal readiness.
/// </summary>
public sealed class LobbyRoom
{
    private readonly List<LobbyPlayer> _players = new();
    private readonly object _lock = new();

    /// <summary>Maximum number of concurrent players allowed in this room.</summary>
    public int MaxPlayers { get; }

    /// <summary>Human-readable room name.</summary>
    public string RoomName { get; }

    /// <summary>Current lifecycle phase of the lobby.</summary>
    public LobbyPhase Phase { get; private set; } = LobbyPhase.Waiting;

    /// <summary>Pseudo-random seed negotiated for the session. Set when the game starts.</summary>
    public int SessionSeed { get; private set; }

    /// <param name="roomName">Human-readable identifier for the room.</param>
    /// <param name="maxPlayers">Maximum number of player slots (1–8).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxPlayers"/> is outside [1,8].</exception>
    public LobbyRoom(string roomName, int maxPlayers = 4)
    {
        if (maxPlayers < 1 || maxPlayers > 8)
            throw new ArgumentOutOfRangeException(nameof(maxPlayers), "Must be between 1 and 8.");
        RoomName = roomName;
        MaxPlayers = maxPlayers;
    }

    /// <summary>Current number of connected (non-disconnected) players.</summary>
    public int ConnectedCount
    {
        get
        {
            lock (_lock)
                return _players.Count(p => p.Status != PlayerStatus.Disconnected);
        }
    }

    /// <summary>A snapshot of the current player list.</summary>
    public LobbyPlayer[] Players
    {
        get { lock (_lock) return _players.ToArray(); }
    }

    /// <summary>
    /// Add a player to the lobby. Returns the assigned <see cref="LobbyPlayer"/> on success,
    /// or <c>null</c> if the room is full or the game has already started.
    /// </summary>
    public LobbyPlayer? Join(string displayName)
    {
        lock (_lock)
        {
            if (Phase != LobbyPhase.Waiting) return null;
            if (ConnectedCount >= MaxPlayers) return null;

            int slot = NextFreeSlot();
            bool isHost = _players.Count == 0;
            var player = new LobbyPlayer
            {
                SlotIndex = slot,
                DisplayName = displayName,
                Status = PlayerStatus.Connected,
                IsHost = isHost,
            };
            _players.Add(player);
            return player;
        }
    }

    /// <summary>Mark a player's slot as disconnected.</summary>
    public void Leave(int slotIndex)
    {
        lock (_lock)
        {
            var player = _players.FirstOrDefault(p => p.SlotIndex == slotIndex);
            if (player is not null)
                player.Status = PlayerStatus.Disconnected;
        }
    }

    /// <summary>Toggle the ready state of a connected player.</summary>
    public void SetReady(int slotIndex, bool ready)
    {
        lock (_lock)
        {
            var player = _players.FirstOrDefault(p => p.SlotIndex == slotIndex);
            if (player is null || player.Status == PlayerStatus.Disconnected) return;
            player.Status = ready ? PlayerStatus.Ready : PlayerStatus.Connected;
        }
    }

    /// <summary>
    /// Attempt to transition the lobby to <see cref="LobbyPhase.Starting"/>.
    /// Succeeds only when all connected players are ready and there is at least one player.
    /// </summary>
    /// <returns><c>true</c> when the lobby transitions to the starting phase.</returns>
    public bool TryStart(int seed)
    {
        lock (_lock)
        {
            if (Phase != LobbyPhase.Waiting) return false;
            var connected = _players.Where(p => p.Status != PlayerStatus.Disconnected).ToList();
            if (connected.Count == 0) return false;
            if (connected.Any(p => p.Status != PlayerStatus.Ready)) return false;

            SessionSeed = seed;
            Phase = LobbyPhase.Starting;
            return true;
        }
    }

    /// <summary>Advance the phase from <see cref="LobbyPhase.Starting"/> to <see cref="LobbyPhase.InGame"/>.</summary>
    public void ConfirmGameStarted()
    {
        lock (_lock)
        {
            if (Phase == LobbyPhase.Starting)
                Phase = LobbyPhase.InGame;
        }
    }

    /// <summary>Advance the phase to <see cref="LobbyPhase.PostGame"/>.</summary>
    public void ConfirmGameEnded()
    {
        lock (_lock)
        {
            if (Phase == LobbyPhase.InGame)
                Phase = LobbyPhase.PostGame;
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private int NextFreeSlot()
    {
        var used = new HashSet<int>(_players.Select(p => p.SlotIndex));
        for (int i = 0; i < MaxPlayers; i++)
            if (!used.Contains(i)) return i;
        return -1;
    }
}
