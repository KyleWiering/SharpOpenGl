using SharpOpenGl.Engine.Multiplayer;
using Xunit;

namespace SharpOpenGl.Tests.Multiplayer;

public class ReplayTests
{
    [Fact]
    public void ReplayRecorder_records_and_exports_commands()
    {
        var recorder = new ReplayRecorder(seed: 7);
        recorder.Start(0);
        recorder.Record(new MoveCommand { PlayerId = 0, Tick = 1, TargetX = 5f, TargetZ = 5f });
        recorder.Record(new AttackCommand { PlayerId = 0, Tick = 2, TargetEntityId = 3u });
        recorder.Stop(10);

        ReplayData data = recorder.Export();
        Assert.Equal(7, data.Seed);
        Assert.Equal(0, data.StartTick);
        Assert.Equal(10, data.EndTick);
        Assert.Equal(2, data.Entries.Length);
    }

    [Fact]
    public void ReplayRecorder_ignores_commands_when_not_recording()
    {
        var recorder = new ReplayRecorder();
        // Record without calling Start — should be silently ignored
        recorder.Record(new MoveCommand { PlayerId = 0, Tick = 1 });

        ReplayData data = recorder.Export();
        Assert.Empty(data.Entries);
    }

    [Fact]
    public void ReplayPlayer_feeds_commands_at_correct_ticks()
    {
        var recorder = new ReplayRecorder(seed: 42);
        recorder.Start(0);
        recorder.Record(new MoveCommand { PlayerId = 0, Tick = 1, TargetX = 10f });
        recorder.Record(new StopCommand { PlayerId = 0, Tick = 3 });
        recorder.Stop(5);

        var player = new ReplayPlayer(recorder.Export());
        var queue = new CommandQueue();

        // Tick 1 → should receive MoveCommand
        player.FeedTick(1, queue);
        var tick1 = queue.DrainUpTo(1);
        Assert.Single(tick1);
        Assert.IsType<MoveCommand>(tick1[0]);

        // Tick 2 → nothing
        player.FeedTick(2, queue);
        Assert.Empty(queue.DrainUpTo(2));

        // Tick 3 → StopCommand
        player.FeedTick(3, queue);
        var tick3 = queue.DrainUpTo(3);
        Assert.Single(tick3);
        Assert.IsType<StopCommand>(tick3[0]);

        Assert.True(player.IsFinished);
    }

    [Fact]
    public void ReplayPlayer_Reset_allows_playback_from_start()
    {
        var recorder = new ReplayRecorder();
        recorder.Start();
        recorder.Record(new MoveCommand { PlayerId = 0, Tick = 1 });
        recorder.Stop(2);

        var player = new ReplayPlayer(recorder.Export());
        var queue = new CommandQueue();

        player.FeedTick(1, queue);
        queue.DrainUpTo(1);
        Assert.True(player.IsFinished);

        player.Reset();
        Assert.False(player.IsFinished);

        player.FeedTick(1, queue);
        var replayed = queue.DrainUpTo(1);
        Assert.Single(replayed);
    }

    [Fact]
    public void Replay_matches_original_command_values()
    {
        var original = new MoveCommand
        {
            PlayerId = 0,
            Tick = 5,
            EntityIds = new uint[] { 1, 2 },
            TargetX = 99.9f,
            TargetZ = -42.0f,
            AttackMove = true,
        };

        var recorder = new ReplayRecorder();
        recorder.Start();
        recorder.Record(original);
        recorder.Stop(10);

        var player = new ReplayPlayer(recorder.Export());
        var queue = new CommandQueue();
        player.FeedTick(5, queue);
        var cmd = (MoveCommand)queue.DrainUpTo(5)[0];

        Assert.Equal(original.TargetX, cmd.TargetX);
        Assert.Equal(original.TargetZ, cmd.TargetZ);
        Assert.Equal(original.AttackMove, cmd.AttackMove);
        Assert.Equal(original.EntityIds, cmd.EntityIds);
    }
}
