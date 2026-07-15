using SharpOpenGl.Engine.Build;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Entities;
using Xunit;

namespace SharpOpenGl.Tests.Build;

public class BuildingProductionQueueTests
{
    private static ResourceManager MakeManager(int playerId, int energy = 1000, int minerals = 1000, int crew = 100)
    {
        var rm = new ResourceManager();
        var pr = rm.AddPlayer(playerId);
        pr.SetStartingAmount(ResourceType.Energy, energy);
        pr.SetStartingAmount(ResourceType.Minerals, minerals);
        pr.SetStartingAmount(ResourceType.Crew, crew);
        return rm;
    }

    private static EntityDefinition FighterDef() => new()
    {
        Id = "fighter_basic",
        DisplayName = "Interceptor Mk.I",
        BuildTime = 12f,
        Cost = new CostDefinition { Energy = 50, Minerals = 80, Crew = 1 },
    };

    [Fact]
    public void TryCancelAtIndex_refunds_front_item_and_resets_progress()
    {
        var rm = MakeManager(1, energy: 500, minerals: 500, crew: 10);
        var supply = new SupplySystem(rm);
        supply.ConsumeSupply(1, 1);

        var building = new BuildingComponent
        {
            BuildProgress = 4f,
            BuildQueue = new Queue<string>(["fighter_basic", "fighter_basic"]),
        };

        bool ok = BuildingProductionQueue.TryCancelAtIndex(
            building, 1, _ => FighterDef(), rm, supply, playerId: 1);

        Assert.True(ok);
        Assert.Single(building.BuildQueue);
        Assert.Equal(0f, building.BuildProgress);
        Assert.Equal(550f, rm.GetPlayer(1)!.GetAmount(ResourceType.Energy));
        Assert.Equal(580f, rm.GetPlayer(1)!.GetAmount(ResourceType.Minerals));
        Assert.Equal(0, supply.GetUsed(1));
    }

    [Fact]
    public void TryCancelAtIndex_refunds_tail_without_resetting_progress()
    {
        var rm = MakeManager(1, energy: 500, minerals: 500, crew: 10);
        var supply = new SupplySystem(rm);
        supply.ConsumeSupply(1, 2);

        var building = new BuildingComponent
        {
            BuildProgress = 4f,
            BuildQueue = new Queue<string>(["fighter_basic", "fighter_basic"]),
        };

        bool ok = BuildingProductionQueue.TryCancelAtIndex(
            building, 2, _ => FighterDef(), rm, supply, playerId: 1);

        Assert.True(ok);
        Assert.Single(building.BuildQueue);
        Assert.Equal(4f, building.BuildProgress);
        Assert.Equal(550f, rm.GetPlayer(1)!.GetAmount(ResourceType.Energy));
        Assert.Equal(1, supply.GetUsed(1));
    }

    [Fact]
    public void TryCancelAtIndex_returns_false_for_invalid_index()
    {
        var rm = MakeManager(1);
        var building = new BuildingComponent
        {
            BuildQueue = new Queue<string>(["fighter_basic"]),
        };

        Assert.False(BuildingProductionQueue.TryCancelAtIndex(
            building, 0, _ => FighterDef(), rm, supply: null, playerId: 1));
        Assert.False(BuildingProductionQueue.TryCancelAtIndex(
            building, 2, _ => FighterDef(), rm, supply: null, playerId: 1));
    }
}