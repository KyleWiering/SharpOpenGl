using SharpOpenGl.Engine.Multiplayer;
using Xunit;

namespace SharpOpenGl.Tests.Multiplayer;

public class LobbyTests
{
    [Fact]
    public void LobbyRoom_Join_assigns_slot_zero_to_first_player()
    {
        var room = new LobbyRoom("Test Room", maxPlayers: 2);
        LobbyPlayer? player = room.Join("Alice");

        Assert.NotNull(player);
        Assert.Equal(0, player!.SlotIndex);
        Assert.True(player.IsHost);
    }

    [Fact]
    public void LobbyRoom_second_player_is_not_host()
    {
        var room = new LobbyRoom("Test Room", maxPlayers: 2);
        room.Join("Alice");
        LobbyPlayer? bob = room.Join("Bob");

        Assert.NotNull(bob);
        Assert.False(bob!.IsHost);
        Assert.Equal(1, bob.SlotIndex);
    }

    [Fact]
    public void LobbyRoom_Join_returns_null_when_full()
    {
        var room = new LobbyRoom("Test Room", maxPlayers: 1);
        room.Join("Alice");
        LobbyPlayer? overflow = room.Join("Bob");
        Assert.Null(overflow);
    }

    [Fact]
    public void LobbyRoom_ConnectedCount_excludes_disconnected_players()
    {
        var room = new LobbyRoom("Test Room", maxPlayers: 2);
        room.Join("Alice");
        var bob = room.Join("Bob")!;
        Assert.Equal(2, room.ConnectedCount);

        room.Leave(bob.SlotIndex);
        Assert.Equal(1, room.ConnectedCount);
    }

    [Fact]
    public void LobbyRoom_TryStart_succeeds_when_all_players_ready()
    {
        var room = new LobbyRoom("Test Room", maxPlayers: 2);
        var alice = room.Join("Alice")!;
        var bob = room.Join("Bob")!;
        room.SetReady(alice.SlotIndex, true);
        room.SetReady(bob.SlotIndex, true);

        bool started = room.TryStart(seed: 99);
        Assert.True(started);
        Assert.Equal(LobbyPhase.Starting, room.Phase);
        Assert.Equal(99, room.SessionSeed);
    }

    [Fact]
    public void LobbyRoom_TryStart_fails_when_player_not_ready()
    {
        var room = new LobbyRoom("Test Room", maxPlayers: 2);
        var alice = room.Join("Alice")!;
        room.Join("Bob");
        room.SetReady(alice.SlotIndex, true);
        // Bob is not ready

        bool started = room.TryStart(seed: 1);
        Assert.False(started);
        Assert.Equal(LobbyPhase.Waiting, room.Phase);
    }

    [Fact]
    public void LobbyRoom_TryStart_fails_when_no_players()
    {
        var room = new LobbyRoom("Empty Room");
        Assert.False(room.TryStart(seed: 0));
    }

    [Fact]
    public void LobbyRoom_phase_transitions_correctly()
    {
        var room = new LobbyRoom("Test Room", maxPlayers: 1);
        var p = room.Join("Solo")!;
        room.SetReady(p.SlotIndex, true);
        room.TryStart(seed: 1);

        room.ConfirmGameStarted();
        Assert.Equal(LobbyPhase.InGame, room.Phase);

        room.ConfirmGameEnded();
        Assert.Equal(LobbyPhase.PostGame, room.Phase);
    }

    [Fact]
    public void LobbyRoom_constructor_throws_for_invalid_max_players()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LobbyRoom("Bad", maxPlayers: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LobbyRoom("Bad", maxPlayers: 9));
    }

    [Fact]
    public void LobbyRoom_Join_returns_null_after_game_starts()
    {
        var room = new LobbyRoom("Test Room", maxPlayers: 2);
        var p = room.Join("Alice")!;
        room.SetReady(p.SlotIndex, true);
        room.TryStart(seed: 1);

        LobbyPlayer? late = room.Join("LatePlayer");
        Assert.Null(late);
    }
}
