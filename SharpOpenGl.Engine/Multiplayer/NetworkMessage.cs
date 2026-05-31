using System.Text.Json;

namespace SharpOpenGl.Engine.Multiplayer;

/// <summary>
/// Envelope used to transmit data between peers in a multiplayer session.
/// A message carries a <see cref="MessageType"/> discriminator so receivers can
/// dispatch the embedded <see cref="Payload"/> to the correct handler.
/// </summary>
/// <remarks>
/// Wire format: a single JSON object with fields <c>messageType</c>, <c>senderId</c>,
/// <c>sequenceNumber</c>, and <c>payload</c> (a nested JSON string or object).
/// </remarks>
public sealed class NetworkMessage
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Identifies what the <see cref="Payload"/> contains.</summary>
    public NetworkMessageType MessageType { get; init; }

    /// <summary>Zero-based player index of the sender.</summary>
    public int SenderId { get; init; }

    /// <summary>
    /// Monotonically increasing counter per sender, used for ordering and
    /// de-duplication of messages on the receiving end.
    /// </summary>
    public long SequenceNumber { get; init; }

    /// <summary>
    /// The message body as a raw JSON string.
    /// Receivers decode this according to <see cref="MessageType"/>.
    /// </summary>
    public string Payload { get; init; } = string.Empty;

    // ── Serialisation helpers ───────────────────────────────────────────────

    /// <summary>Serialise this message to a JSON string suitable for transmission.</summary>
    public string Serialize() => JsonSerializer.Serialize(this, _options);

    /// <summary>
    /// Deserialise a <see cref="NetworkMessage"/> from its wire-format JSON string.
    /// </summary>
    public static NetworkMessage Deserialize(string json) =>
        JsonSerializer.Deserialize<NetworkMessage>(json, _options)
        ?? throw new JsonException("Deserialised network message was null.");

    // ── Factory helpers ─────────────────────────────────────────────────────

    /// <summary>Create a <see cref="NetworkMessageType.GameCommand"/> message wrapping a serialised command.</summary>
    public static NetworkMessage ForCommand(IGameCommand command, int senderId, long seq) =>
        new()
        {
            MessageType = NetworkMessageType.GameCommand,
            SenderId = senderId,
            SequenceNumber = seq,
            Payload = CommandSerializer.Serialize(command),
        };

    /// <summary>Create a <see cref="NetworkMessageType.SyncHeartbeat"/> message.</summary>
    public static NetworkMessage ForHeartbeat(int senderId, long seq, long tick, uint checksum) =>
        new()
        {
            MessageType = NetworkMessageType.SyncHeartbeat,
            SenderId = senderId,
            SequenceNumber = seq,
            Payload = JsonSerializer.Serialize(new { tick, checksum }),
        };

    /// <summary>Create a <see cref="NetworkMessageType.GameStart"/> message containing the session seed.</summary>
    public static NetworkMessage ForGameStart(int senderId, long seq, int seed) =>
        new()
        {
            MessageType = NetworkMessageType.GameStart,
            SenderId = senderId,
            SequenceNumber = seq,
            Payload = JsonSerializer.Serialize(new { seed }),
        };
}
