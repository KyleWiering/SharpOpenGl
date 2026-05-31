namespace SharpOpenGl.Engine.Multiplayer;

/// <summary>Connection state of a player within a <see cref="LobbyRoom"/>.</summary>
public enum PlayerStatus
{
    /// <summary>The player is connected and visible in the lobby.</summary>
    Connected,

    /// <summary>The player has indicated they are ready to start.</summary>
    Ready,

    /// <summary>The player has disconnected or timed out.</summary>
    Disconnected,
}

/// <summary>
/// Represents a single player's presence inside a <see cref="LobbyRoom"/>.
/// </summary>
public sealed class LobbyPlayer
{
    /// <summary>Zero-based slot index assigned to this player (stable across a session).</summary>
    public int SlotIndex { get; init; }

    /// <summary>Display name chosen by the player.</summary>
    public string DisplayName { get; init; } = "Player";

    /// <summary>Current connection / readiness state.</summary>
    public PlayerStatus Status { get; set; } = PlayerStatus.Connected;

    /// <summary>Ping to the host in milliseconds. -1 when unknown.</summary>
    public int PingMs { get; set; } = -1;

    /// <summary>Whether this slot is the host/owner of the room.</summary>
    public bool IsHost { get; init; }

    /// <inheritdoc/>
    public override string ToString() =>
        $"[{SlotIndex}] {DisplayName} ({Status}){(IsHost ? " [host]" : "")}";
}
