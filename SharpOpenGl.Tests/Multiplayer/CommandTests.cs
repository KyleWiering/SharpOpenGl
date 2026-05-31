using SharpOpenGl.Engine.Multiplayer;
using Xunit;

namespace SharpOpenGl.Tests.Multiplayer;

public class CommandTests
{
    // ── Command construction ─────────────────────────────────────────────────

    [Fact]
    public void MoveCommand_has_correct_type()
    {
        var cmd = new MoveCommand { PlayerId = 0, Tick = 1, TargetX = 10f, TargetZ = 20f };
        Assert.Equal(CommandType.Move, cmd.Type);
    }

    [Fact]
    public void AttackCommand_has_correct_type()
    {
        var cmd = new AttackCommand { PlayerId = 0, Tick = 1, TargetEntityId = 5u };
        Assert.Equal(CommandType.Attack, cmd.Type);
    }

    [Fact]
    public void BuildCommand_has_correct_type()
    {
        var cmd = new BuildCommand { PlayerId = 1, Tick = 2, BuilderEntityId = 3u, ItemId = "fighter_basic" };
        Assert.Equal(CommandType.Build, cmd.Type);
    }

    [Fact]
    public void StopCommand_has_correct_type()
    {
        var cmd = new StopCommand { PlayerId = 0, Tick = 5, EntityIds = new uint[] { 1, 2, 3 } };
        Assert.Equal(CommandType.Stop, cmd.Type);
    }

    [Fact]
    public void UseAbilityCommand_has_correct_type()
    {
        var cmd = new UseAbilityCommand { PlayerId = 0, Tick = 3, SourceEntityId = 1u, AbilityId = "shield_burst" };
        Assert.Equal(CommandType.UseAbility, cmd.Type);
    }

    // ── Serialisation round-trips ─────────────────────────────────────────────

    [Fact]
    public void MoveCommand_roundtrips_through_json()
    {
        var original = new MoveCommand
        {
            PlayerId = 1,
            Tick = 10,
            EntityIds = new uint[] { 2, 5 },
            TargetX = 3.5f,
            TargetZ = -7.0f,
            AttackMove = true,
        };
        string json = CommandSerializer.Serialize(original);
        var restored = (MoveCommand)CommandSerializer.Deserialize(json);

        Assert.Equal(original.PlayerId, restored.PlayerId);
        Assert.Equal(original.Tick, restored.Tick);
        Assert.Equal(original.TargetX, restored.TargetX);
        Assert.Equal(original.TargetZ, restored.TargetZ);
        Assert.Equal(original.AttackMove, restored.AttackMove);
        Assert.Equal(original.EntityIds, restored.EntityIds);
    }

    [Fact]
    public void AttackCommand_roundtrips_through_json()
    {
        var original = new AttackCommand
        {
            PlayerId = 0,
            Tick = 5,
            AttackerIds = new uint[] { 1, 2 },
            TargetEntityId = 9u,
        };
        string json = CommandSerializer.Serialize(original);
        var restored = (AttackCommand)CommandSerializer.Deserialize(json);

        Assert.Equal(original.PlayerId, restored.PlayerId);
        Assert.Equal(original.Tick, restored.Tick);
        Assert.Equal(original.TargetEntityId, restored.TargetEntityId);
        Assert.Equal(original.AttackerIds, restored.AttackerIds);
    }

    [Fact]
    public void BuildCommand_roundtrips_through_json()
    {
        var original = new BuildCommand
        {
            PlayerId = 0,
            Tick = 3,
            BuilderEntityId = 7u,
            ItemId = "command_center",
        };
        string json = CommandSerializer.Serialize(original);
        var restored = (BuildCommand)CommandSerializer.Deserialize(json);

        Assert.Equal(original.ItemId, restored.ItemId);
        Assert.Equal(original.BuilderEntityId, restored.BuilderEntityId);
    }

    [Fact]
    public void StopCommand_roundtrips_through_json()
    {
        var original = new StopCommand
        {
            PlayerId = 1,
            Tick = 8,
            EntityIds = new uint[] { 4, 5, 6 },
        };
        string json = CommandSerializer.Serialize(original);
        var restored = (StopCommand)CommandSerializer.Deserialize(json);

        Assert.Equal(original.EntityIds, restored.EntityIds);
    }

    [Fact]
    public void UseAbilityCommand_roundtrips_through_json()
    {
        var original = new UseAbilityCommand
        {
            PlayerId = 0,
            Tick = 2,
            SourceEntityId = 1u,
            AbilityId = "warp_jump",
            TargetX = 50f,
            TargetZ = 50f,
        };
        string json = CommandSerializer.Serialize(original);
        var restored = (UseAbilityCommand)CommandSerializer.Deserialize(json);

        Assert.Equal(original.AbilityId, restored.AbilityId);
        Assert.Equal(original.TargetX, restored.TargetX);
    }

    // ── CommandQueue ─────────────────────────────────────────────────────────

    [Fact]
    public void CommandQueue_Count_reflects_enqueued_commands()
    {
        var queue = new CommandQueue();
        queue.Enqueue(new MoveCommand { PlayerId = 0, Tick = 1 });
        queue.Enqueue(new MoveCommand { PlayerId = 0, Tick = 2 });
        Assert.Equal(2, queue.Count);
    }

    [Fact]
    public void CommandQueue_DrainUpTo_returns_commands_at_or_before_tick()
    {
        var queue = new CommandQueue();
        queue.Enqueue(new MoveCommand { PlayerId = 0, Tick = 1 });
        queue.Enqueue(new MoveCommand { PlayerId = 0, Tick = 2 });
        queue.Enqueue(new MoveCommand { PlayerId = 0, Tick = 5 });

        var drained = queue.DrainUpTo(2);
        Assert.Equal(2, drained.Length);
        Assert.Equal(1, queue.Count); // tick 5 remains
    }

    [Fact]
    public void CommandQueue_DrainUpTo_returns_empty_when_no_commands_ready()
    {
        var queue = new CommandQueue();
        queue.Enqueue(new MoveCommand { PlayerId = 0, Tick = 10 });

        var drained = queue.DrainUpTo(5);
        Assert.Empty(drained);
    }

    [Fact]
    public void CommandQueue_Clear_empties_all_commands()
    {
        var queue = new CommandQueue();
        queue.Enqueue(new MoveCommand { PlayerId = 0, Tick = 1 });
        queue.Clear();
        Assert.Equal(0, queue.Count);
    }
}
