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