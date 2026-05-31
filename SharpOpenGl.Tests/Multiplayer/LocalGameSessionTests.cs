using SharpOpenGl.Engine.Multiplayer;
using Xunit;

namespace SharpOpenGl.Tests.Multiplayer;

public class LocalGameSessionTests
{
    [Fact]
    public void Session_starts_at_tick_zero()
    {
        var session = new LocalGameSession(ticksPerSecond: 20, seed: 0);
        session.Start();
        Assert.Equal(0, session.CurrentTick);
    }

    [Fact]
    public void Session_advances_ticks_correctly()
    {
        var session = new LocalGameSession(ticksPerSecond: 20, seed: 0);
        session.Start();
        session.Advance(0.05); // one tick at 20 Hz

        Assert.True(session.HasPendingTick);
        long tick = session.ConsumeTick();
        Assert.Equal(1, tick);
    }

    [Fact]
    public void Session_dispatches_command_to_correct_player_queue()
    {
        var session = new LocalGameSession(ticksPerSecond: 20, seed: 0, playerCount: 2);
        session.Start();

        session.IssueCommand(new MoveCommand { PlayerId = 0, Tick = 1 });
        session.IssueCommand(new MoveCommand { PlayerId = 1, Tick = 1 });

        session.Advance(0.05);
        long tick = session.ConsumeTick();

        var p0Cmds = session.DrainCommandsForPlayer(0, tick);
        var p1Cmds = session.DrainCommandsForPlayer(1, tick);

        Assert.Single(p0Cmds);
        Assert.Single(p1Cmds);
    }

    [Fact]
    public void Session_records_and_exports_replay()
    {
        var session = new LocalGameSession(ticksPerSecond: 20, seed: 5, playerCount: 1);
        session.Start();
        session.IssueCommand(new BuildCommand { PlayerId = 0, Tick = 1, ItemId = "fighter_basic" });
        session.Advance(0.05);
        session.ConsumeTick();
        session.Stop();

        ReplayData replay = session.ExportReplay(0);
        Assert.Equal(5, replay.Seed);
        Assert.Single(replay.Entries);
    }

    [Fact]
    public void Session_throws_for_out_of_range_player_command()
    {
        var session = new LocalGameSession(playerCount: 1);
        session.Start();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            session.IssueCommand(new MoveCommand { PlayerId = 1, Tick = 1 }));
    }

    [Fact]
    public void Session_constructor_throws_for_invalid_player_count()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new LocalGameSession(playerCount: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new LocalGameSession(playerCount: 3));
    }
}
