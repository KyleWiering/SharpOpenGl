using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Grid;
using Xunit;

namespace SharpOpenGl.Tests.ECS;

public class AIBuildQueueSystemTests
{
    [Fact]
    public void PickProduction_prefers_miner_before_fighter()
    {
        var building = new BuildingComponent
        {
            PlayerId = 2,
            BuildingType = "shipyard_small",
            Producible = ["scout_light", "fighter_basic", "miner_basic"],
        };

        string? picked = AIBuildQueueSystem.PickProduction(building);

        Assert.Equal("miner_basic", picked);
    }

    [Fact]
    public void PickProduction_queues_fighter_when_miner_already_queued()
    {
        var building = new BuildingComponent
        {
            PlayerId = 2,
            BuildingType = "shipyard_small",
            Producible = ["fighter_basic", "miner_basic"],
            BuildQueue = new Queue<string>(["miner_basic"]),
        };

        string? picked = AIBuildQueueSystem.PickProduction(building);

        Assert.Equal("fighter_basic", picked);
    }

    [Fact]
    public void PickProduction_advances_to_corvette_after_fighter_queued()
    {
        var building = new BuildingComponent
        {
            PlayerId = 2,
            BuildingType = "shipyard_medium",
            Producible = ["miner_basic", "fighter_basic", "corvette_fast", "destroyer_assault"],
            BuildQueue = new Queue<string>(["miner_basic", "fighter_basic"]),
        };

        string? picked = AIBuildQueueSystem.PickProduction(building);

        Assert.Equal("corvette_fast", picked);
    }

    [Fact]
    public void PickProduction_advances_to_destroyer_when_lighter_hulls_queued()
    {
        var building = new BuildingComponent
        {
            PlayerId = 2,
            BuildingType = "shipyard_large",
            Producible = ["miner_basic", "fighter_basic", "corvette_fast", "destroyer_assault", "bomber_heavy"],
            BuildQueue = new Queue<string>(["miner_basic", "fighter_basic", "corvette_fast"]),
        };

        string? picked = AIBuildQueueSystem.PickProduction(building);

        Assert.Equal("destroyer_assault", picked);
    }

    [Theory]
    [InlineData(SkirmishDifficultyTier.Easy, 0.35f, 2)]
    [InlineData(SkirmishDifficultyTier.Normal, 0.6f, 4)]
    [InlineData(SkirmishDifficultyTier.Hard, 0.85f, 6)]
    public void Difficulty_tuning_scales_aggressiveness_and_spawns(
        SkirmishDifficultyTier tier, float expectedAggro, int expectedScouts)
    {
        Assert.Equal(expectedAggro, SkirmishDifficultyTuning.AiAggressiveness(tier), 2);
        Assert.Equal(expectedScouts, SkirmishDifficultyTuning.AiScoutSpawnCount(tier));
    }

    [Fact]
    public void Difficulty_cycle_wraps_easy_to_hard()
    {
        Assert.Equal(SkirmishDifficultyTier.Normal, SkirmishDifficultyTuning.Cycle(SkirmishDifficultyTier.Easy));
        Assert.Equal(SkirmishDifficultyTier.Hard, SkirmishDifficultyTuning.Cycle(SkirmishDifficultyTier.Normal));
        Assert.Equal(SkirmishDifficultyTier.Easy, SkirmishDifficultyTuning.Cycle(SkirmishDifficultyTier.Hard));
        Assert.Equal(SkirmishDifficultyTier.Hard, SkirmishDifficultyTuning.Cycle(SkirmishDifficultyTier.Easy, -1));
    }

    [Theory]
    [InlineData(SkirmishDifficultyTier.Easy, 300f, 180f, 60f, 30f)]
    [InlineData(SkirmishDifficultyTier.Normal, 500f, 300f, 100f, 50f)]
    [InlineData(SkirmishDifficultyTier.Hard, 800f, 500f, 160f, 80f)]
    public void AiStartingResources_scales_by_difficulty_tier(
        SkirmishDifficultyTier tier,
        float expectedEnergy,
        float expectedMinerals,
        float expectedData,
        float expectedCrew)
    {
        var amounts = SkirmishDifficultyTuning.AiStartingResources(tier);

        Assert.Equal(expectedEnergy, amounts.Energy);
        Assert.Equal(expectedMinerals, amounts.Minerals);
        Assert.Equal(expectedData, amounts.Data);
        Assert.Equal(expectedCrew, amounts.Crew);
    }
}