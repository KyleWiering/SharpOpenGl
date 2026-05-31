using SharpOpenGl.Engine.Multiplayer;
using System.Text.Json;
using Xunit;

namespace SharpOpenGl.Tests.Multiplayer;

public class NetworkMessageTests
{
    [Fact]
    public void NetworkMessage_serializes_and_deserializes()
    {
        var msg = new NetworkMessage
        {
            MessageType = NetworkMessageType.Chat,
            SenderId = 1,
            SequenceNumber = 42,
            Payload = "Hello!",
        };

        string json = msg.Serialize();
        NetworkMessage restored = NetworkMessage.Deserialize(json);

        Assert.Equal(msg.MessageType, restored.MessageType);
        Assert.Equal(msg.SenderId, restored.SenderId);
        Assert.Equal(msg.SequenceNumber, restored.SequenceNumber);
        Assert.Equal(msg.Payload, restored.Payload);
    }

    [Fact]
    public void NetworkMessage_ForCommand_wraps_game_command()
    {
        var cmd = new AttackCommand { PlayerId = 0, Tick = 5, TargetEntityId = 9u };
        var msg = NetworkMessage.ForCommand(cmd, senderId: 0, seq: 1);

        Assert.Equal(NetworkMessageType.GameCommand, msg.MessageType);
        Assert.Equal(0, msg.SenderId);
        Assert.Equal(1, msg.SequenceNumber);

        // The payload should deserialise back to the original command
        var restored = (AttackCommand)CommandSerializer.Deserialize(msg.Payload);
        Assert.Equal(cmd.TargetEntityId, restored.TargetEntityId);
    }

    [Fact]
    public void NetworkMessage_ForHeartbeat_contains_tick_and_checksum()
    {
        var msg = NetworkMessage.ForHeartbeat(senderId: 0, seq: 3, tick: 100, checksum: 0xDEADBEEF);
        Assert.Equal(NetworkMessageType.SyncHeartbeat, msg.MessageType);

        var payload = JsonDocument.Parse(msg.Payload).RootElement;
        Assert.Equal(100, payload.GetProperty("tick").GetInt64());
        Assert.Equal(0xDEADBEEF, payload.GetProperty("checksum").GetUInt32());
    }

    [Fact]
    public void NetworkMessage_ForGameStart_contains_seed()
    {
        var msg = NetworkMessage.ForGameStart(senderId: 0, seq: 1, seed: 42);
        Assert.Equal(NetworkMessageType.GameStart, msg.MessageType);

        var payload = JsonDocument.Parse(msg.Payload).RootElement;
        Assert.Equal(42, payload.GetProperty("seed").GetInt32());
    }
}
