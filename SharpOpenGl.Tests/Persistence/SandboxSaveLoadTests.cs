using System.Text.Json;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Persistence;
using Xunit;

namespace SharpOpenGl.Tests.Persistence;

public class SandboxSaveLoadTests
{
    [Fact]
    public void Sandbox_save_snapshot_includes_seed_metadata()
    {
        const string seedText = "demo-42";
        int parsedSeed = ProceduralSeedHelper.ParseSeed(seedText);

        var world = new World();
        var grid = new GridSystem(32, 32, 10f);
        var fog = new FogOfWar(grid);
        var resources = new ResourceManager();
        resources.AddPlayer(1);

        SaveData save = WorldSaveService.Capture(new WorldSaveContext
        {
            World = world,
            ResourceManager = resources,
            MissionState = null,
            GridSystem = grid,
            FogOfWar = fog,
            FogPlayerId = 0,
            CameraX = 64f,
            CameraY = 128f,
            CameraZoom = 1.1f,
            SlotName = SaveSlotNames.ManualSlots[0],
            IsSandboxSession = true,
            ProceduralMapSeed = parsedSeed,
            SandboxSeedText = seedText,
        });

        Assert.True(save.IsSandboxSession);
        Assert.Equal(parsedSeed, save.ProceduralMapSeed);
        Assert.Equal(seedText, save.SandboxSeedText);
        Assert.Equal(string.Empty, save.MissionId);
        Assert.Equal(0f, save.ElapsedMissionTime, 0.001f);
    }

    [Fact]
    public void Sandbox_resource_nodes_survive_save_load_cycle()
    {
        var sourceWorld = new World();

        Entity mineralsNode = SpawnResourceNode(
            sourceWorld,
            ResourceType.Minerals,
            new Vector3(40f, 1f, 60f),
            amount: 3200f,
            maxAmount: 5000f);
        Entity energyNode = SpawnResourceNode(
            sourceWorld,
            ResourceType.Energy,
            new Vector3(80f, 1f, 20f),
            amount: 0f,
            maxAmount: 5000f);
        Entity dataNode = SpawnResourceNode(
            sourceWorld,
            ResourceType.Data,
            new Vector3(12f, 1f, 90f),
            amount: 900f,
            maxAmount: 2500f);

        SaveData save = CaptureWorld(sourceWorld, sandboxSession: true, seedText: "node-roundtrip");
        var (targetWorld, result) = RestoreWorld(save);

        Assert.Equal(3, save.Entities.Count);
        Assert.Equal(3, result.EntityCount);

        var loadedNodes = targetWorld.Query<ResourceNodeComponent>().ToList();
        Assert.Equal(3, loadedNodes.Count);

        ResourceNodeComponent minerals = FindNode(loadedNodes, ResourceType.Minerals);
        ResourceNodeComponent energy = FindNode(loadedNodes, ResourceType.Energy);
        ResourceNodeComponent data = FindNode(loadedNodes, ResourceType.Data);

        Assert.Equal(3200f, minerals.Amount, 0.001f);
        Assert.Equal(5000f, minerals.MaxAmount, 0.001f);
        Assert.Equal(0f, energy.Amount, 0.001f);
        Assert.Equal(5000f, energy.MaxAmount, 0.001f);
        Assert.True(energy.IsDepleted);
        Assert.Equal(900f, data.Amount, 0.001f);
        Assert.Equal(2500f, data.MaxAmount, 0.001f);

        Assert.Equal((int)mineralsNode.Index, save.Entities.Single(e => e.TemplateId == "resource_node_minerals").EntityId);
        Assert.Equal(0f, save.Entities.Single(e => e.TemplateId == "resource_node_energy").Health, 0.001f);
    }

    [Fact]
    public void Legacy_save_without_sandbox_fields_loads_with_defaults()
    {
        const string legacyJson = """
            {
              "Version": 1,
              "SlotName": "Slot 1",
              "MissionId": "tutorial_01",
              "ElapsedMissionTime": 45.5,
              "Entities": []
            }
            """;

        SaveData? loaded = JsonSerializer.Deserialize<SaveData>(legacyJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        Assert.NotNull(loaded);
        Assert.False(loaded!.IsSandboxSession);
        Assert.Equal(0, loaded.ProceduralMapSeed);
        Assert.Equal(string.Empty, loaded.SandboxSeedText);
        Assert.Equal("tutorial_01", loaded.MissionId);
    }

    private static Entity SpawnResourceNode(
        World world,
        ResourceType type,
        Vector3 position,
        float amount,
        float maxAmount)
    {
        Entity node = world.CreateEntity();
        world.AddComponent(node, new TransformComponent { Position = position });
        world.AddComponent(node, new ResourceNodeComponent
        {
            ResourceType = type,
            Amount = amount,
            MaxAmount = maxAmount,
            HarvestRate = 10f,
        });
        return node;
    }

    private static ResourceNodeComponent FindNode(
        List<(Entity Entity, ResourceNodeComponent Node)> nodes,
        ResourceType type) =>
        nodes.Single(n => n.Node.ResourceType == type).Node;

    private static SaveData CaptureWorld(World world, bool sandboxSession = false, string? seedText = null)
    {
        var grid = new GridSystem(32, 32, 10f);
        var fog = new FogOfWar(grid);
        var resources = new ResourceManager();
        resources.AddPlayer(1);

        return WorldSaveService.Capture(new WorldSaveContext
        {
            World = world,
            ResourceManager = resources,
            GridSystem = grid,
            FogOfWar = fog,
            FogPlayerId = 0,
            CameraX = 0f,
            CameraY = 0f,
            CameraZoom = 1f,
            SlotName = SaveSlotNames.ManualSlots[0],
            IsSandboxSession = sandboxSession,
            ProceduralMapSeed = sandboxSession ? ProceduralSeedHelper.ParseSeed(seedText) : 0,
            SandboxSeedText = seedText,
        });
    }

    private static (World World, WorldLoadResult Result) RestoreWorld(SaveData save)
    {
        var targetWorld = new World();
        var targetGrid = new GridSystem(32, 32, 10f);
        var targetFog = new FogOfWar(targetGrid);
        var targetResources = new ResourceManager();
        targetResources.AddPlayer(1);

        WorldLoadResult result = WorldLoadService.Restore(new WorldLoadContext
        {
            World = targetWorld,
            ResourceManager = targetResources,
            GridSystem = targetGrid,
            FogOfWar = targetFog,
            FogPlayerId = 0,
            UnitFactory = new UnitFactory(),
            ResolveDefinition = _ => null,
        }, save);

        return (targetWorld, result);
    }
}