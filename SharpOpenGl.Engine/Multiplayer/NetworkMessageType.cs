namespace SharpOpenGl.Engine.Multiplayer;

/// <summary>Discriminates the payload type carried by a <see cref="NetworkMessage"/>.</summary>
public enum NetworkMessageType
{
    /// <summary>Client requests to join a lobby room.</summary>
    JoinRequest,

    /// <summary>Server acknowledges a successful join, returning the assigned player slot.</summary>
    JoinAck,

    /// <summary>Server notifies all clients of an updated lobby player list.</summary>
    LobbyState,

    /// <summary>Host signals that the game is starting; payload contains the random seed.</summary>
    GameStart,

    /// <summary>A player-issued game command that must be applied on all peers.</summary>
    GameCommand,

    /// <summary>Synchronisation heartbeat — contains current tick and a state checksum.</summary>
    SyncHeartbeat,

    /// <summary>A peer has disconnected. Payload identifies the player slot.</summary>
    PlayerLeft,

    /// <summary>Host has paused the game (e.g. a player is lagging).</summary>
    GamePause,

    /// <summary>Host has resumed the game after a pause.</summary>
    GameResume,

    /// <summary>Chat message from one player to the lobby or game session.</summary>
    Chat,
}
