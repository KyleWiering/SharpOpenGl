using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Missions;

using SharpOpenGl.Engine.Persistence;
using Xunit;

namespace SharpOpenGl.Tests.Persistence;

public class WorldSaveLoadTests
{
    [Fact]
    public void Save_and_load_roundtrip_preserves_entity_count_and_hero_hp()
    {
        var sourceWorld = BuildSampleWorld(out Entity hero, out int expectedCount);
        var grid = new GridSystem(32, 32, 10f);
        var fog = new FogOfWar(grid);
        fog.Reveal(0, 8, 8, 4);

        var resources = new ResourceManager();
        var player = resources.AddPlayer(1);
        player.SetStartingAmount(ResourceType.Energy, 420f);
        player.SetStartingAmount(ResourceType.Minerals, 210f);
        player.SetStartingAmount(ResourceType.Data, 55f);
        player.SetStartingAmount(ResourceType.Crew, 12f);

        var mission = new MissionState(MakeMissionDefinition())
        {
            ElapsedTime = 95f,
        };
        mission.PrimaryObjectives[0].IsCompleted = true;
        mission.Triggers[0].HasFired = true;
        mission.RegisterEntityTag("hero", hero);

        SaveData save = WorldSaveService.Capture(new WorldSaveContext
        {
            World = sourceWorld,
            ResourceManager = resources,
            MissionState = mission,
            GridSystem = grid,
            FogOfWar = fog,
            FogPlayerId = 0,
            CameraX = 120f,
            CameraY = 340f,
            CameraZoom = 1.25f,
            SlotName = SaveSlotNames.ManualSlots[0],
        });

        var targetWorld = new World();
        var targetGrid = new GridSystem(32, 32, 10f);
        var targetFog = new FogOfWar(targetGrid);
        var targetResources = new ResourceManager();
        targetResources.AddPlayer(1);
        var factory = new UnitFactory();
        var targetMission = new MissionState(MakeMissionDefinition());

        WorldLoadResult result = WorldLoadService.Restore(new WorldLoadContext
        {
            World = targetWorld,
            ResourceManager = targetResources,
            MissionState = targetMission,
            GridSystem = targetGrid,
            FogOfWar = targetFog,
            FogPlayerId = 0,
            UnitFactory = factory,
            ResolveDefinition = id => SampleDefinitions()[id],
            FinalizeUnit = (entity, def, playerId, isEnemy) =>
            {
                if (def == null) return;
                if (playerId > 1)
                    targetWorld.AddComponent(entity, new AIControlledComponent { PlayerId = playerId });
            },
        }, save);

        Assert.Equal(expectedCount, result.EntityCount);
        Assert.Equal(expectedCount, save.Entities.Count);
        Assert.NotEqual(Entity.Null, result.HeroEntity);

        HealthComponent? heroHealth = targetWorld.GetComponent<HealthComponent>(result.HeroEntity);
        Assert.NotNull(heroHealth);
        Assert.Equal(850f, heroHealth!.CurrentHP, 0.001f);

        Assert.Equal(420f, targetResources.GetPlayer(1)!.GetAmount(ResourceType.Energy), 0.001f);
        Assert.Equal(95f, targetMission.ElapsedTime, 0.001f);
        Assert.True(targetMission.PrimaryObjectives[0].IsCompleted);
        Assert.True(targetMission.Triggers[0].HasFired);
        Assert.True(targetMission.EntityTags.ContainsKey("hero"));
        Assert.True(targetGrid.GetCell(8, 8)!.GetFog(0) != FogState.Unexplored);
    }

    [Fact]
    public void Save_and_load_roundtrip_preserves_under_construction_state()
    {
        var sourceWorld = new World();
        Entity building = sourceWorld.CreateEntity();
        sourceWorld.AddComponent(building, new TransformComponent { Position = new Vector3(18f, 0f, 20f) });
        sourceWorld.AddComponent(building, new BuildingComponent
        {
            BuildingType = "power_reactor",
            ProductionRate = 0f,
            PlayerId = 1,
        });
        sourceWorld.AddComponent(building, new HealthComponent
        {
            MaxHP = 1000f,
            CurrentHP = 250f,
        });
        sourceWorld.AddComponent(building, new EntityNameComponent { DefinitionId = "power_reactor" });
        sourceWorld.AddComponent(building, new UnderConstructionComponent
        {
            DefinitionId = "power_reactor",
            BuildProgress = 12f,
            TotalBuildTime = 30f,
            PlayerId = 1,
        });

        var grid = new GridSystem(32, 32, 10f);
        var fog = new FogOfWar(grid);
        var resources = new ResourceManager();
        resources.AddPlayer(1);

        SaveData save = WorldSaveService.Capture(new WorldSaveContext
        {
            World = sourceWorld,
            ResourceManager = resources,
            GridSystem = grid,
            FogOfWar = fog,
            FogPlayerId = 0,
            CameraX = 0f,
            CameraY = 0f,
            CameraZoom = 1f,
            SlotName = SaveSlotNames.ManualSlots[0],
        });

        var targetWorld = new World();
        var targetGrid = new GridSystem(32, 32, 10f);
        var targetFog = new FogOfWar(targetGrid);
        var targetResources = new ResourceManager();
        targetResources.AddPlayer(1);
        var factory = new UnitFactory();

        WorldLoadService.Restore(new WorldLoadContext
        {
            World = targetWorld,
            ResourceManager = targetResources,
            GridSystem = targetGrid,
            FogOfWar = targetFog,
            FogPlayerId = 0,
            UnitFactory = factory,
            ResolveDefinition = id => SampleDefinitions()[id],
        }, save);

        Entity loaded = targetWorld.Query<BuildingComponent>().First().Entity;
        var underConstruction = targetWorld.GetComponent<UnderConstructionComponent>(loaded);
        Assert.NotNull(underConstruction);
        Assert.Equal(12f, underConstruction!.BuildProgress, 0.001f);
        Assert.Equal(30f, underConstruction.TotalBuildTime, 0.001f);
        Assert.Equal(0f, targetWorld.GetComponent<BuildingComponent>(loaded)!.ProductionRate, 0.001f);
        Assert.Null(targetWorld.GetComponent<WeaponListComponent>(loaded));
    }

    [Fact]
    public void Save_and_load_roundtrip_preserves_resource_node_amount_and_position()
    {
        var sourceWorld = new World();
        Entity node = sourceWorld.CreateEntity();
        sourceWorld.AddComponent(node, new TransformComponent { Position = new Vector3(50f, 1f, 50f) });
        sourceWorld.AddComponent(node, new ResourceNodeComponent
        {
            ResourceType = ResourceType.Minerals,
            Amount = 1234f,
            MaxAmount = 5000f,
            HarvestRate = 10f,
        });

        SaveData save = CaptureWorld(sourceWorld);
        var (targetWorld, result) = RestoreWorld(save);

        Assert.Equal(1, result.EntityCount);
        Assert.Single(save.Entities);
        Assert.Equal("resource_node_minerals", save.Entities[0].TemplateId);
        Assert.Equal(1234f, save.Entities[0].Health, 0.001f);
        Assert.Equal(5000f, save.Entities[0].Shields, 0.001f);
        Assert.Equal(50f, save.Entities[0].X, 0.001f);
        Assert.Equal(50f, save.Entities[0].Y, 0.001f);

        var (loadedEntity, loadedNode) = targetWorld.Query<ResourceNodeComponent>().Single();
        TransformComponent? transform = targetWorld.GetComponent<TransformComponent>(loadedEntity);
        Assert.NotNull(transform);
        Assert.Equal(50f, transform!.Position.X, 0.001f);
        Assert.Equal(50f, transform.Position.Z, 0.001f);
        Assert.Equal(ResourceType.Minerals, loadedNode.ResourceType);
        Assert.Equal(1234f, loadedNode.Amount, 0.001f);
        Assert.Equal(5000f, loadedNode.MaxAmount, 0.001f);
        Assert.Equal(10f, loadedNode.HarvestRate, 0.001f);
    }

    [Fact]
    public void Save_and_load_roundtrip_preserves_depleted_resource_node()
    {
        var sourceWorld = new World();
        Entity node = sourceWorld.CreateEntity();
        sourceWorld.AddComponent(node, new TransformComponent { Position = new Vector3(12f, 1f, 18f) });
        sourceWorld.AddComponent(node, new ResourceNodeComponent
        {
            ResourceType = ResourceType.Energy,
            Amount = 0f,
            MaxAmount = 5000f,
            HarvestRate = 10f,
        });

        SaveData save = CaptureWorld(sourceWorld);
        var (targetWorld, result) = RestoreWorld(save);

        Assert.Equal(1, result.EntityCount);
        Assert.Equal(0f, save.Entities[0].Health, 0.001f);
        Assert.Equal(5000f, save.Entities[0].Shields, 0.001f);

        var (_, loadedNode) = targetWorld.Query<ResourceNodeComponent>().Single();
        Assert.Equal(0f, loadedNode.Amount, 0.001f);
        Assert.Equal(5000f, loadedNode.MaxAmount, 0.001f);
        Assert.True(loadedNode.IsDepleted);
    }

    [Fact]
    public void Save_and_load_roundtrip_preserves_sandbox_metadata()
    {
        var sourceWorld = new World();
        var grid = new GridSystem(32, 32, 10f);
        var fog = new FogOfWar(grid);
        var resources = new ResourceManager();
        resources.AddPlayer(1);

        SaveData save = WorldSaveService.Capture(new WorldSaveContext
        {
            World = sourceWorld,
            ResourceManager = resources,
            MissionState = null,
            GridSystem = grid,
            FogOfWar = fog,
            FogPlayerId = 0,
            CameraX = 0f,
            CameraY = 0f,
            CameraZoom = 1f,
            SlotName = SaveSlotNames.ManualSlots[0],
            IsSandboxSession = true,
            ProceduralMapSeed = 42_424_242,
            SandboxSeedText = "my-sandbox-seed",
        });

        Assert.True(save.IsSandboxSession);
        Assert.Equal(42_424_242, save.ProceduralMapSeed);
        Assert.Equal("my-sandbox-seed", save.SandboxSeedText);
        Assert.Equal(string.Empty, save.MissionId);

        var (targetWorld, result) = RestoreWorld(save, missionState: null);

        Assert.True(result.IsSandboxSession);
        Assert.Equal(42_424_242, result.ProceduralMapSeed);
        Assert.Equal("my-sandbox-seed", result.SandboxSeedText);
        Assert.Empty(targetWorld.Query<ResourceNodeComponent>());
    }

    [Fact]
    public void Save_load_preserves_explored_and_visible_states_exactly()
    {
        var grid = new GridSystem(32, 32, 10f);
        var fog = new FogOfWar(grid, playerCount: 1);
        grid.GetCell(7, 7)!.SetFog(0, FogState.Explored);
        grid.GetCell(8, 8)!.SetFog(0, FogState.Visible);
        grid.GetCell(9, 9)!.SetFog(0, FogState.Explored);
        grid.GetCell(10, 10)!.SetFog(0, FogState.Visible);

        SaveData save = CaptureFogWorld(grid, fog);
        var (_, targetGrid) = RestoreFogWorld(save, playerCount: 1);

        Assert.Equal(FogState.Explored, targetGrid.GetCell(7, 7)!.GetFog(0));
        Assert.Equal(FogState.Visible, targetGrid.GetCell(8, 8)!.GetFog(0));
        Assert.Equal(FogState.Explored, targetGrid.GetCell(9, 9)!.GetFog(0));
        Assert.Equal(FogState.Visible, targetGrid.GetCell(10, 10)!.GetFog(0));
        Assert.Equal(4, save.FogStates.Count);
    }

    [Fact]
    public void Save_load_preserves_multi_player_fog()
    {
        var grid = new GridSystem(32, 32, 10f);
        var fog = new FogOfWar(grid, playerCount: 2);
        grid.GetCell(4, 4)!.SetFog(0, FogState.Visible);
        grid.GetCell(5, 5)!.SetFog(0, FogState.Explored);
        grid.GetCell(20, 20)!.SetFog(1, FogState.Visible);
        grid.GetCell(21, 21)!.SetFog(1, FogState.Explored);

        SaveData save = CaptureFogWorld(grid, fog);
        var (_, targetGrid) = RestoreFogWorld(save, playerCount: 2);

        Assert.Equal(FogState.Visible, targetGrid.GetCell(4, 4)!.GetFog(0));
        Assert.Equal(FogState.Explored, targetGrid.GetCell(5, 5)!.GetFog(0));
        Assert.Equal(FogState.Visible, targetGrid.GetCell(20, 20)!.GetFog(1));
        Assert.Equal(FogState.Explored, targetGrid.GetCell(21, 21)!.GetFog(1));
        Assert.Equal(4, save.FogStates.Count);
        Assert.Contains("0:4:4", save.FogStates.Keys);
        Assert.Contains("1:21:21", save.FogStates.Keys);
    }

    [Fact]
    public void Loaded_explored_cells_survive_fog_system_tick_outside_sight()
    {
        var grid = new GridSystem(32, 32, 10f);
        var fog = new FogOfWar(grid, playerCount: 1);
        grid.GetCell(15, 15)!.SetFog(0, FogState.Explored);

        SaveData save = CaptureFogWorld(grid, fog);
        var (targetWorld, targetGrid) = RestoreFogWorld(save, playerCount: 1);

        var targetFog = new FogOfWar(targetGrid, playerCount: 1);
        var system = new FogOfWarSystem(targetGrid, targetFog, playerId: 0);

        Entity ship = targetWorld.CreateEntity();
        targetWorld.AddComponent(ship, new TransformComponent
        {
            Position = targetGrid.GridToWorld(0, 0),
        });
        targetWorld.AddComponent(ship, new SightRadiusComponent { Radius = 2 });

        system.Update(targetWorld, 0.016f);

        Assert.Equal(FogState.Explored, targetGrid.GetCell(15, 15)!.GetFog(0));
        targetWorld.Dispose();
    }

    [Fact]
    public void FogStates_survives_save_manager_json_roundtrip()
    {
        var grid = new GridSystem(32, 32, 10f);
        var fog = new FogOfWar(grid, playerCount: 2);
        grid.GetCell(4, 4)!.SetFog(0, FogState.Visible);
        grid.GetCell(5, 5)!.SetFog(0, FogState.Explored);
        grid.GetCell(20, 20)!.SetFog(1, FogState.Visible);
        grid.GetCell(21, 21)!.SetFog(1, FogState.Explored);

        SaveData capture = CaptureFogWorld(grid, fog);
        string dir = Path.Combine(Path.GetTempPath(), $"fog_json_{Guid.NewGuid():N}");
        var mgr = new SaveManager(dir);
        capture.SlotName = SaveSlotNames.ManualSlots[0];

        try
        {
            Assert.True(mgr.Save(capture));
            SaveData? loaded = mgr.Load(capture.SlotName);
            Assert.NotNull(loaded);
            Assert.Equal(capture.FogStates.Count, loaded!.FogStates.Count);

            foreach (var (key, value) in capture.FogStates)
                Assert.Equal(value, loaded.FogStates[key]);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void LoadLatest_restore_path_uses_full_snapshot()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"saveload_{Guid.NewGuid():N}");
        var mgr = new SaveManager(dir);

        var save = new SaveData
        {
            SlotName = SaveSlotNames.Autosave,
            MissionId = "tutorial_01",
            ElapsedMissionTime = 30f,
            Entities =
            [
                new EntitySaveRecord
                {
                    EntityId = 7,
                    TemplateId = "hero_default",
                    X = 12f,
                    Y = 18f,
                    Health = 777f,
                    PlayerId = 1,
                },
            ],
        };
        mgr.Save(save);

        SaveData? latest = mgr.LoadLatest();
        Assert.NotNull(latest);
        Assert.Equal(SaveSlotNames.Autosave, latest!.SlotName);
        Assert.Single(latest.Entities);
        Assert.Equal(777f, latest.Entities[0].Health, 0.001f);

        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
    }

    private static SaveData CaptureFogWorld(GridSystem grid, FogOfWar fog)
    {
        var resources = new ResourceManager();
        resources.AddPlayer(1);

        return WorldSaveService.Capture(new WorldSaveContext
        {
            World = new World(),
            ResourceManager = resources,
            GridSystem = grid,
            FogOfWar = fog,
            FogPlayerId = 0,
            CameraX = 0f,
            CameraY = 0f,
            CameraZoom = 1f,
            SlotName = SaveSlotNames.ManualSlots[0],
        });
    }

    private static (World World, GridSystem Grid) RestoreFogWorld(SaveData save, int playerCount)
    {
        var targetWorld = new World();
        var targetGrid = new GridSystem(32, 32, 10f);
        var targetFog = new FogOfWar(targetGrid, playerCount: playerCount);
        var targetResources = new ResourceManager();
        targetResources.AddPlayer(1);

        WorldLoadService.Restore(new WorldLoadContext
        {
            World = targetWorld,
            ResourceManager = targetResources,
            GridSystem = targetGrid,
            FogOfWar = targetFog,
            FogPlayerId = 0,
            UnitFactory = new UnitFactory(),
            ResolveDefinition = _ => null,
        }, save);

        return (targetWorld, targetGrid);
    }

    private static SaveData CaptureWorld(World world)
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
        });
    }

    private static (World World, WorldLoadResult Result) RestoreWorld(
        SaveData save,
        MissionState? missionState = null)
    {
        var targetWorld = new World();
        var targetGrid = new GridSystem(32, 32, 10f);
        var targetFog = new FogOfWar(targetGrid);
        var targetResources = new ResourceManager();
        targetResources.AddPlayer(1);
        var factory = new UnitFactory();

        WorldLoadResult result = WorldLoadService.Restore(new WorldLoadContext
        {
            World = targetWorld,
            ResourceManager = targetResources,
            MissionState = missionState,
            GridSystem = targetGrid,
            FogOfWar = targetFog,
            FogPlayerId = 0,
            UnitFactory = factory,
            ResolveDefinition = id => SampleDefinitions().GetValueOrDefault(id),
        }, save);

        return (targetWorld, result);
    }

    private static World BuildSampleWorld(out Entity hero, out int expectedCount)
    {
        var world = new World();
        var defs = SampleDefinitions();

        hero = SpawnShip(world, defs["hero_default"], new Vector3(10f, 0f, 20f), 850f, playerId: 1);
        world.AddComponent(hero, new HeroComponent());

        SpawnShip(world, defs["fighter_basic"], new Vector3(30f, 0f, 25f), 120f, playerId: 1);
        SpawnShip(world, defs["scout_light"], new Vector3(80f, 0f, 90f), 75f, playerId: 2);

        Entity building = world.CreateEntity();
        world.AddComponent(building, new TransformComponent { Position = new Vector3(-20f, 0f, -15f) });
        world.AddComponent(building, new BuildingComponent { BuildingType = "command_center", PlayerId = 1 });
        world.AddComponent(building, new HealthComponent { MaxHP = 2000f, CurrentHP = 1800f });
        world.AddComponent(building, new EntityNameComponent { DefinitionId = "command_center" });

        Entity node = world.CreateEntity();
        world.AddComponent(node, new TransformComponent { Position = new Vector3(50f, 1f, 50f) });
        world.AddComponent(node, new ResourceNodeComponent
        {
            ResourceType = ResourceType.Minerals,
            Amount = 4000f,
            MaxAmount = 5000f,
        });

        expectedCount = 5;
        return world;
    }

    private static Entity SpawnShip(
        World world,
        EntityDefinition def,
        Vector3 position,
        float hp,
        int playerId)
    {
        var factory = new UnitFactory();
        Entity entity = factory.Create(world, def);
        TransformComponent? transform = world.GetComponent<TransformComponent>(entity);
        if (transform != null)
            transform.Position = position;

        HealthComponent? health = world.GetComponent<HealthComponent>(entity);
        if (health != null)
            health.CurrentHP = hp;

        world.AddComponent(entity, new EntityNameComponent { DefinitionId = def.Id });
        if (playerId > 1)
            world.AddComponent(entity, new AIControlledComponent { PlayerId = playerId });

        return entity;
    }

    private static Dictionary<string, EntityDefinition> SampleDefinitions() => new()
    {
        ["hero_default"] = new EntityDefinition
        {
            Id = "hero_default",
            Category = "hero",
            Components = new ComponentsDefinition
            {
                Health = new HealthDefinition { MaxHP = 1000f },
                Movement = new MovementDefinition { Speed = 80f },
                Hero = new HeroDefinition { Level = 1 },
            },
        },
        ["fighter_basic"] = new EntityDefinition
        {
            Id = "fighter_basic",
            Category = "fighter",
            Components = new ComponentsDefinition
            {
                Health = new HealthDefinition { MaxHP = 150f },
                Movement = new MovementDefinition { Speed = 70f },
            },
        },
        ["scout_light"] = new EntityDefinition
        {
            Id = "scout_light",
            Category = "fighter",
            Components = new ComponentsDefinition
            {
                Health = new HealthDefinition { MaxHP = 90f },
                Movement = new MovementDefinition { Speed = 90f },
            },
        },
        ["command_center"] = new EntityDefinition
        {
            Id = "command_center",
            Category = "building",
            Components = new ComponentsDefinition
            {
                Health = new HealthDefinition { MaxHP = 2000f },
                Building = new BuildingDefinition { BuildingType = "command_center", Footprint = [2, 2] },
            },
        },
        ["power_reactor"] = new EntityDefinition
        {
            Id = "power_reactor",
            Category = "building",
            BuildTime = 30f,
            Components = new ComponentsDefinition
            {
                Health = new HealthDefinition { MaxHP = 1000f },
                Building = new BuildingDefinition
                {
                    BuildingType = "power_reactor",
                    ProductionRate = 1.5f,
                    Footprint = [2, 2],
                },
                Weapons =
                [
                    new WeaponDefinition { Slot = 0, Type = "laser", Damage = 10f, Range = 100f, FireRate = 1f },
                ],
            },
        },
    };

    private static MissionDefinition MakeMissionDefinition() => new()
    {
        Id = "tutorial_01",
        DisplayName = "Tutorial",
        Objectives = new ObjectivesDefinition
        {
            Primary =
            [
                new ObjectiveDefinition { Id = "destroy_scout", Description = "Destroy scout" },
            ],
        },
        Triggers =
        [
            new TriggerDefinition { Id = "spawn_wave_1", Actions = [] },
        ],
    };
}