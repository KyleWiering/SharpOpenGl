using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using Xunit;

namespace SharpOpenGl.Tests.ECS;

public class SquadSystemTests
{
    private static (World world, SquadSystem squads, List<Entity> ships) SetupShips(int count)
    {
        var world = new World();
        var squads = new SquadSystem();
        var ships = new List<Entity>();

        for (int i = 0; i < count; i++)
        {
            Entity ship = world.CreateEntity();
            world.AddComponent(ship, new TransformComponent
            {
                Position = new Vector3(i * 5f, 0f, 0f),
            });
            world.AddComponent(ship, new MovementComponent { Speed = 10f });
            world.AddComponent(ship, new SquadMemberComponent());
            ships.Add(ship);
        }

        return (world, squads, ships);
    }

    [Theory]
    [InlineData(FormationType.Line)]
    [InlineData(FormationType.Wedge)]
    [InlineData(FormationType.Box)]
    [InlineData(FormationType.Column)]
    public void FormationLayout_produces_leader_at_origin_for_all_types(FormationType formation)
    {
        Vector3[] offsets = FormationLayout.ComputeOffsets(formation, 5);

        Assert.Equal(5, offsets.Length);
        Assert.Equal(Vector3.Zero, offsets[0]);
        Assert.All(offsets.Skip(1), offset => Assert.True(offset.Z <= 0f));
    }

    [Fact]
    public void FormationLayout_line_is_symmetric_for_even_follower_count()
    {
        Vector3[] offsets = FormationLayout.ComputeOffsets(FormationType.Line, 5);

        Assert.Equal(-18f, offsets[1].X);
        Assert.Equal(18f, offsets[4].X);
        Assert.Equal(-6f, offsets[2].X);
        Assert.Equal(6f, offsets[3].X);
        Assert.Equal(-12f, offsets[1].Z);
        Assert.Equal(-12f, offsets[4].Z);
    }

    [Fact]
    public void FormationLayout_column_stacks_followers_behind_leader()
    {
        Vector3[] offsets = FormationLayout.ComputeOffsets(FormationType.Column, 4);

        Assert.Equal(new Vector3(0f, 0f, -12f), offsets[1]);
        Assert.Equal(new Vector3(0f, 0f, -24f), offsets[2]);
        Assert.Equal(new Vector3(0f, 0f, -36f), offsets[3]);
    }

    [Fact]
    public void FormationLayout_rotate_offset_matches_yaw()
    {
        Vector3 local = new(0f, 0f, -12f);
        Vector3 rotated = FormationLayout.RotateOffset(local, 90f);

        Assert.Equal(-12f, rotated.X, 2);
        Assert.Equal(0f, rotated.Z, 2);
    }

    [Fact]
    public void FormSquad_assigns_id_slot_and_offsets()
    {
        var (world, squads, ships) = SetupShips(3);

        int squadId = squads.FormSquad(world, ships, FormationType.Wedge);
        Assert.True(squadId > 0);

        for (int i = 0; i < ships.Count; i++)
        {
            var member = world.GetComponent<SquadMemberComponent>(ships[i])!;
            Assert.Equal(squadId, member.SquadId);
            Assert.Equal(i, member.FormationSlot);
            if (i > 0)
                Assert.NotEqual(Vector3.Zero, member.FormationOffset);
        }

        Assert.Equal(Vector3.Zero, world.GetComponent<SquadMemberComponent>(ships[0])!.FormationOffset);

        world.Dispose();
    }

    [Fact]
    public void DisbandSquad_clears_membership()
    {
        var (world, squads, ships) = SetupShips(3);
        int squadId = squads.FormSquad(world, ships, FormationType.Box);

        squads.DisbandSquad(world, squadId);

        foreach (var ship in ships)
        {
            var member = world.GetComponent<SquadMemberComponent>(ship)!;
            Assert.Equal(-1, member.SquadId);
            Assert.Equal(-1, member.FormationSlot);
            Assert.Equal(Vector3.Zero, member.FormationOffset);
        }

        world.Dispose();
    }

    [Fact]
    public void AssignMoveRoutes_leader_gets_destination_followers_get_formation_slots()
    {
        var (world, squads, ships) = SetupShips(3);
        squads.FormSquad(world, ships, FormationType.Line);

        var destination = new Vector3(100f, 0f, 50f);
        squads.AssignMoveRoutes(world, ships, destination);

        var leaderQueue = world.GetComponent<WaypointQueueComponent>(ships[0])!;
        Assert.Single(leaderQueue.Waypoints);
        Assert.Equal(destination, leaderQueue.Waypoints[0]);

        float facing = FormationLayout.FacingYaw(
            world.GetComponent<TransformComponent>(ships[0])!.Position,
            destination);

        for (int i = 1; i < ships.Count; i++)
        {
            var member = world.GetComponent<SquadMemberComponent>(ships[i])!;
            Vector3 expected = destination + FormationLayout.RotateOffset(member.FormationOffset, facing);
            var queue = world.GetComponent<WaypointQueueComponent>(ships[i])!;
            Assert.Single(queue.Waypoints);
            Assert.Equal(expected, queue.Waypoints[0]);
        }

        world.Dispose();
    }

    [Fact]
    public void CycleFormation_changes_type_and_recomputes_offsets()
    {
        var (world, squads, ships) = SetupShips(4);
        squads.FormSquad(world, ships, FormationType.Line);
        Vector3[] lineOffsets = ships
            .Select(ship => world.GetComponent<SquadMemberComponent>(ship)!.FormationOffset)
            .ToArray();

        FormationType? next = squads.CycleFormation(world, ships);
        Assert.Equal(FormationType.Wedge, next);

        Vector3[] wedgeOffsets = ships
            .Select(ship => world.GetComponent<SquadMemberComponent>(ship)!.FormationOffset)
            .ToArray();

        Assert.NotEqual(lineOffsets[2], wedgeOffsets[2]);

        world.Dispose();
    }

    [Fact]
    public void FormSquad_single_unit_returns_negative_id()
    {
        var (world, squads, ships) = SetupShips(1);

        Assert.Equal(-1, squads.FormSquad(world, ships));

        world.Dispose();
    }

    [Fact]
    public void CycleFormation_cycles_line_wedge_box_column_and_back()
    {
        var (world, squads, ships) = SetupShips(4);
        squads.FormSquad(world, ships, FormationType.Line);

        Assert.Equal(FormationType.Wedge, squads.CycleFormation(world, ships));
        Assert.Equal(FormationType.Box, squads.CycleFormation(world, ships));
        Assert.Equal(FormationType.Column, squads.CycleFormation(world, ships));
        Assert.Equal(FormationType.Line, squads.CycleFormation(world, ships));

        Assert.Equal(FormationType.Line, squads.GetFormationForSelection(ships, world));

        world.Dispose();
    }

    [Fact]
    public void GetFormationForSelection_returns_null_for_mixed_squads()
    {
        var (world, squads, ships) = SetupShips(4);
        squads.FormSquad(world, ships.Take(2).ToList(), FormationType.Line);
        squads.FormSquad(world, ships.Skip(2).ToList(), FormationType.Wedge);

        Assert.Null(squads.GetFormationForSelection(ships, world));

        world.Dispose();
    }

    [Fact]
    public void FormationLayout_next_formation_matches_documented_cycle()
    {
        Assert.Equal(FormationType.Wedge, FormationLayout.NextFormation(FormationType.Line));
        Assert.Equal(FormationType.Box, FormationLayout.NextFormation(FormationType.Wedge));
        Assert.Equal(FormationType.Column, FormationLayout.NextFormation(FormationType.Box));
        Assert.Equal(FormationType.Line, FormationLayout.NextFormation(FormationType.Column));
    }
}