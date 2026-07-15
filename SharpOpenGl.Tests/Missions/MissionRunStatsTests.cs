using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Missions;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Missions;

public class MissionRunStatsTests
{
    [Fact]
    public void RecordDeath_increments_enemies_destroyed_for_player_kills()
    {
        var world = new World();
        var stats = new MissionRunStats();

        Entity killer = SpawnShip(world, faction: 1);
        Entity victim = SpawnShip(world, faction: 2);

        stats.RecordDeath(world, victim, killer);

        Assert.Equal(1, stats.EnemiesDestroyed);
        Assert.Equal(0, stats.UnitsLost);
    }

    [Fact]
    public void RecordDeath_increments_units_lost_for_player_deaths()
    {
        var world = new World();
        var stats = new MissionRunStats();

        Entity killer = SpawnShip(world, faction: 2);
        Entity victim = SpawnShip(world, faction: 1);

        stats.RecordDeath(world, victim, killer);

        Assert.Equal(0, stats.EnemiesDestroyed);
        Assert.Equal(1, stats.UnitsLost);
    }

    [Fact]
    public void FormatSummaryLines_includes_combat_counters()
    {
        var stats = new MissionRunStats();
        var world = new World();
        stats.RecordDeath(world, SpawnShip(world, 2), SpawnShip(world, 1));
        stats.StructuresBuilt = 2;

        IReadOnlyList<string> lines = stats.FormatSummaryLines(isVictory: true);

        Assert.Contains("Enemies destroyed: 1", lines);
        Assert.Contains("Units lost: 0", lines);
        Assert.Contains("Structures built: 2", lines);
    }

    private static Entity SpawnShip(World world, int faction)
    {
        Entity entity = world.CreateEntity();
        world.AddComponent(entity, new CombatTargetComponent { Faction = faction });
        world.AddComponent(entity, new RenderComponent { RaceTextureIndex = 0 });
        return entity;
    }
}