using System.Text.Json;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.Build;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Build;

public class StationArticulationTests
{
    private const float PivotScale = 7f;
    private const float Tolerance = 1e-3f;

    private static EntityDefinition MakeDefenseTurretDefinition() => new()
    {
        Id = "defense_turret",
        Category = "base",
        Components = new ComponentsDefinition
        {
            Health = new HealthDefinition { MaxHP = 600, Armor = 40 },
            Building = new BuildingDefinition
            {
                BuildingType = "defense_turret",
                Footprint = [1, 1],
                Rotates = true,
            },
            Weapons =
            [
                new WeaponDefinition { Slot = 0, Type = "laser", Damage = 35, Range = 320, FireRate = 3.0f },
            ],
        },
    };

    private static EntityDefinition MakeSensorArrayDefinition() => new()
    {
        Id = "sensor_array",
        Category = "base",
        Components = new ComponentsDefinition
        {
            Health = new HealthDefinition { MaxHP = 450, Armor = 25 },
            Building = new BuildingDefinition
            {
                BuildingType = "sensor_array",
                Footprint = [2, 2],
                Rotates = true,
            },
        },
    };

    private static EntityDefinition MakeShipyardMediumDefinition() => new()
    {
        Id = "shipyard_medium",
        Category = "base",
        BuildTime = 100f,
        Components = new ComponentsDefinition
        {
            Health = new HealthDefinition { MaxHP = 1500, Armor = 75 },
            Building = new BuildingDefinition
            {
                BuildingType = "shipyard_medium",
                Footprint = [3, 3],
                Rotates = false,
            },
        },
    };

    [Fact]
    public void BaseFactory_defense_turret_spawns_yaw_and_pitch_children()
    {
        var world = new World();
        Entity owner = new BaseFactory().Create(world, MakeDefenseTurretDefinition());

        var yawParts = FindPartsOwnedBy(world, owner, ArticulatedPartType.TurretYaw);
        var pitchParts = FindPitchPartsForTurret(world, owner);

        Assert.Single(yawParts);
        Assert.Single(pitchParts);

        var yaw = world.GetComponent<ArticulatedPartComponent>(yawParts[0])!;
        var pitch = world.GetComponent<ArticulatedPartComponent>(pitchParts[0])!;

        Assert.Equal(owner, yaw.Owner);
        Assert.Equal(yawParts[0], pitch.Owner);
        Assert.Equal(new Vector3(0f, PivotScale * 0.42f, 0f), yaw.LocalPivotOffset);
        Assert.True(yaw.IdleSweepEnabled);

        var pitchRender = world.GetComponent<RenderComponent>(pitchParts[0]);
        Assert.NotNull(pitchRender);
        Assert.Equal(ArticulatedStationPartMeshes.TurretBarrelKeyPrefix, pitchRender.MeshKey);
    }

    [Fact]
    public void ArticulationSystem_defense_turret_aims_toward_mock_target_in_range()
    {
        var world = new World();
        var system = new ArticulationSystem();

        Entity owner = new BaseFactory().Create(world, MakeDefenseTurretDefinition());
        world.AddComponent(owner, new TransformComponent
        {
            Position = Vector3.Zero,
            Scale = Vector3.One * 1.4f,
        });

        Entity target = world.CreateEntity();
        world.AddComponent(target, new TransformComponent { Position = new Vector3(40f, 60f, -120f) });
        world.AddComponent(target, new HealthComponent { MaxHP = 100f, CurrentHP = 100f });

        world.GetComponent<CombatTargetComponent>(owner)!.CurrentTarget = target;

        Entity pitchPart = FindPitchPartsForTurret(world, owner).Single();
        var pitch = world.GetComponent<ArticulatedPartComponent>(pitchPart)!;

        system.Update(world, 2f);

        Assert.True(pitch.HasAimTarget);
        Assert.True(pitch.TargetPitch > 0f);
        Assert.True(pitch.CurrentPitch > 0f);
    }

    [Fact]
    public void SkirmishSpawn_defense_turret_has_articulated_children()
    {
        var world = new World();
        Entity building = world.CreateEntity();
        world.AddComponent(building, new TransformComponent { Position = Vector3.Zero });
        world.AddComponent(building, new BuildingComponent
        {
            BuildingType = "defense_turret",
            Footprint = [1, 1],
            Rotates = true,
            PlayerId = 0,
        });
        var weapons = new WeaponListComponent();
        weapons.Weapons.Add(new WeaponComponent
        {
            Slot = 0,
            Type = "laser",
            Damage = 35f,
            Range = 320f,
            FireRate = 3f,
        });
        world.AddComponent(building, weapons);
        world.AddComponent(building, new CombatTargetComponent
        {
            Faction = 0,
            TargetingMode = TargetPriority.Closest,
        });

        BaseFactory.TrySpawnArticulation(world, building, "defense_turret");

        var yawParts = FindPartsOwnedBy(world, building, ArticulatedPartType.TurretYaw);
        var pitchParts = FindPitchPartsForTurret(world, building);

        Assert.Single(yawParts);
        Assert.Single(pitchParts);
    }

    [Fact]
    public void BaseFactory_sensor_array_spawns_dish_child()
    {
        var world = new World();
        Entity owner = new BaseFactory().Create(world, MakeSensorArrayDefinition());

        var dishParts = FindPartsOwnedBy(world, owner, ArticulatedPartType.SensorDish);
        Assert.Single(dishParts);

        var dish = world.GetComponent<ArticulatedPartComponent>(dishParts[0])!;
        Assert.Equal(new Vector3(0f, PivotScale * 0.58f, PivotScale * 0.08f), dish.LocalPivotOffset);
        Assert.Equal(-60f, dish.YawMin);
        Assert.Equal(60f, dish.YawMax);
        Assert.True(dish.IdleSweepEnabled);
        Assert.Equal(25f, dish.IdleSweepSpeed);
        Assert.False(dish.HasAimTarget);

        var render = world.GetComponent<RenderComponent>(dishParts[0]);
        Assert.NotNull(render);
        Assert.Equal(ArticulatedStationPartMeshes.SensorDishKeyPrefix, render.MeshKey);
    }

    [Fact]
    public void ArticulationSystem_sensor_dish_idle_sweeps_within_yaw_limits()
    {
        var world = new World();
        var system = new ArticulationSystem();

        Entity owner = new BaseFactory().Create(world, MakeSensorArrayDefinition());
        world.AddComponent(owner, new TransformComponent { Position = Vector3.Zero });

        Entity dishPart = FindPartsOwnedBy(world, owner, ArticulatedPartType.SensorDish).Single();
        var dish = world.GetComponent<ArticulatedPartComponent>(dishPart)!;

        for (int i = 0; i < 30; i++)
            system.Update(world, 0.25f);

        Assert.InRange(dish.CurrentYaw, dish.YawMin, dish.YawMax);
        Assert.NotEqual(0f, dish.CurrentYaw);
    }

    [Fact]
    public void ComposePartModelMatrix_sensor_dish_follows_station_rotation()
    {
        var world = new World();
        Entity owner = new BaseFactory().Create(world, MakeSensorArrayDefinition());
        world.AddComponent(owner, new TransformComponent
        {
            Position = new Vector3(5f, 0f, -3f),
            EulerAngles = new Vector3(0f, 0f, 0f),
            Scale = Vector3.One * 1.8f,
        });
        world.AddComponent(owner, new BuildingComponent { Rotates = true });

        Entity dishPart = FindPartsOwnedBy(world, owner, ArticulatedPartType.SensorDish).Single();
        var dish = world.GetComponent<ArticulatedPartComponent>(dishPart)!;
        dish.CurrentYaw = 20f;

        var ownerTransform = world.GetComponent<TransformComponent>(owner)!;
        Matrix4 baseModel = ArticulationMath.ComposePartModelMatrix(
            ownerTransform.GetModelMatrix(),
            dish.LocalPivotOffset,
            dish.CurrentYaw,
            dish.CurrentPitch,
            dish.MeshLocalOffset,
            ownerTransform.Scale);
        Vector3 baseForward = ExtractWorldForward(baseModel);

        StationRotationSystem.UpdateStationRotations(world, 2f);
        ownerTransform = world.GetComponent<TransformComponent>(owner)!;
        Matrix4 rotatedModel = ArticulationMath.ComposePartModelMatrix(
            ownerTransform.GetModelMatrix(),
            dish.LocalPivotOffset,
            dish.CurrentYaw,
            dish.CurrentPitch,
            dish.MeshLocalOffset,
            ownerTransform.Scale);
        Vector3 rotatedForward = ExtractWorldForward(rotatedModel);

        float delta = Vector3.Dot(Vector3.Normalize(baseForward), Vector3.Normalize(rotatedForward));
        Assert.True(delta < 0.99f, "Dish world orientation should change when parent station yaws.");
        Assert.True(rotatedForward.LengthSquared > Tolerance);
    }

    [Fact]
    public void ComposePartModelMatrix_part_local_yaw_independent_of_parent_spin()
    {
        var world = new World();
        Entity owner = new BaseFactory().Create(world, MakeSensorArrayDefinition());
        world.AddComponent(owner, new TransformComponent
        {
            Position = Vector3.Zero,
            EulerAngles = new Vector3(0f, 45f, 0f),
            Scale = Vector3.One * 1.8f,
        });

        Entity dishPart = FindPartsOwnedBy(world, owner, ArticulatedPartType.SensorDish).Single();
        var dish = world.GetComponent<ArticulatedPartComponent>(dishPart)!;
        dish.CurrentYaw = 30f;

        var ownerTransform = world.GetComponent<TransformComponent>(owner)!;
        Matrix4 withYaw = ArticulationMath.ComposePartModelMatrix(
            ownerTransform.GetModelMatrix(),
            dish.LocalPivotOffset,
            dish.CurrentYaw,
            0f,
            dish.MeshLocalOffset,
            ownerTransform.Scale);

        dish.CurrentYaw = -30f;
        Matrix4 oppositeYaw = ArticulationMath.ComposePartModelMatrix(
            ownerTransform.GetModelMatrix(),
            dish.LocalPivotOffset,
            dish.CurrentYaw,
            0f,
            dish.MeshLocalOffset,
            ownerTransform.Scale);

        Vector3 withPoint = ArticulationMath.TransformPoint(new Vector3(0f, 0f, 1f), withYaw);
        Vector3 oppositePoint = ArticulationMath.TransformPoint(new Vector3(0f, 0f, 1f), oppositeYaw);
        Assert.NotEqual(withPoint, oppositePoint);
    }

    [Fact]
    public void BaseFactory_shipyard_medium_spawns_crane_and_door_children()
    {
        var world = new World();
        Entity owner = new BaseFactory().Create(world, MakeShipyardMediumDefinition());

        var craneParts = FindPartsOwnedBy(world, owner, ArticulatedPartType.Crane);
        var doorParts = FindPartsOwnedBy(world, owner, ArticulatedPartType.BayDoor);

        Assert.Single(craneParts);
        Assert.Single(doorParts);

        var crane = world.GetComponent<ArticulatedPartComponent>(craneParts[0])!;
        var door = world.GetComponent<ArticulatedPartComponent>(doorParts[0])!;

        Assert.Equal(new Vector3(0f, PivotScale * 0.78f, PivotScale * 0.06f), crane.LocalPivotOffset);
        Assert.Equal(-45f, crane.YawMin);
        Assert.Equal(45f, crane.YawMax);
        Assert.True(crane.IdleSweepEnabled);

        Assert.Equal(new Vector3(0f, PivotScale * 0.28f, -PivotScale * 0.18f), door.LocalPivotOffset);
        Assert.Equal(90f, door.PitchMax);

        Assert.Equal(ArticulatedStationPartMeshes.ShipyardCraneKeyPrefix,
            world.GetComponent<RenderComponent>(craneParts[0])!.MeshKey);
        Assert.Equal(ArticulatedStationPartMeshes.ShipyardBayDoorKeyPrefix,
            world.GetComponent<RenderComponent>(doorParts[0])!.MeshKey);
    }

    [Fact]
    public void ArticulationSystem_crane_yaw_tracks_build_progress()
    {
        var world = new World();
        var system = new ArticulationSystem
        {
            DefinitionLoader = _ => new EntityDefinition { BuildTime = 100f },
        };

        Entity owner = new BaseFactory().Create(world, MakeShipyardMediumDefinition());
        world.AddComponent(owner, new TransformComponent { Position = Vector3.Zero });

        var building = world.GetComponent<BuildingComponent>(owner)!;
        building.BuildQueue.Enqueue("fighter_basic");
        building.BuildProgress = 50f;

        Entity cranePart = FindPartsOwnedBy(world, owner, ArticulatedPartType.Crane).Single();
        var crane = world.GetComponent<ArticulatedPartComponent>(cranePart)!;

        system.Update(world, 2f);

        Assert.True(crane.HasAimTarget);
        Assert.Equal(45f, crane.TargetYaw, precision: 1);
        Assert.True(crane.CurrentYaw > 0f);
    }

    [Fact]
    public void ArticulationSystem_crane_idle_sweeps_when_queue_empty()
    {
        var world = new World();
        var system = new ArticulationSystem();

        Entity owner = new BaseFactory().Create(world, MakeShipyardMediumDefinition());
        world.AddComponent(owner, new TransformComponent { Position = Vector3.Zero });

        var building = world.GetComponent<BuildingComponent>(owner)!;
        building.BuildProgress = 0f;

        Entity cranePart = FindPartsOwnedBy(world, owner, ArticulatedPartType.Crane).Single();
        var crane = world.GetComponent<ArticulatedPartComponent>(cranePart)!;

        system.Update(world, 1.5f);

        Assert.False(crane.HasAimTarget);
        Assert.InRange(crane.CurrentYaw, -45f, 45f);
        Assert.NotEqual(0f, crane.CurrentYaw);
    }

    [Fact]
    public void ArticulationSystem_bay_door_opens_proportionally_to_build_progress()
    {
        var world = new World();
        var system = new ArticulationSystem
        {
            DefinitionLoader = _ => new EntityDefinition { BuildTime = 100f },
        };

        Entity owner = new BaseFactory().Create(world, MakeShipyardMediumDefinition());
        world.AddComponent(owner, new TransformComponent { Position = Vector3.Zero });

        var building = world.GetComponent<BuildingComponent>(owner)!;
        building.BuildQueue.Enqueue("fighter_basic");
        building.BuildProgress = 40f;

        Entity doorPart = FindPartsOwnedBy(world, owner, ArticulatedPartType.BayDoor).Single();
        var door = world.GetComponent<ArticulatedPartComponent>(doorPart)!;

        system.Update(world, 2f);

        Assert.True(door.HasAimTarget);
        Assert.Equal(30f, door.TargetPitch, precision: 1);
        Assert.True(door.CurrentPitch > 0f);
    }

    [Fact]
    public void ComposePartModelMatrix_shipyard_crane_stable_under_station_yaw()
    {
        var world = new World();
        Entity owner = new BaseFactory().Create(world, MakeShipyardMediumDefinition());
        world.AddComponent(owner, new TransformComponent
        {
            Position = new Vector3(2f, 0f, 4f),
            EulerAngles = new Vector3(0f, 30f, 0f),
            Scale = Vector3.One * 2.2f,
        });

        Entity cranePart = FindPartsOwnedBy(world, owner, ArticulatedPartType.Crane).Single();
        var crane = world.GetComponent<ArticulatedPartComponent>(cranePart)!;
        crane.CurrentYaw = 15f;

        var ownerTransform = world.GetComponent<TransformComponent>(owner)!;
        Matrix4 model = ArticulationMath.ComposePartModelMatrix(
            ownerTransform.GetModelMatrix(),
            crane.LocalPivotOffset,
            crane.CurrentYaw,
            crane.CurrentPitch,
            crane.MeshLocalOffset,
            ownerTransform.Scale);

        Vector3 pivotWorld = ArticulationMath.ComputePivotWorld(
            ownerTransform.GetModelMatrix(),
            crane.LocalPivotOffset,
            ownerTransform.Scale);
        Vector3 transformedOrigin = ArticulationMath.TransformPoint(Vector3.Zero, model);

        Assert.True((transformedOrigin - pivotWorld).Length < PivotScale);
        Assert.True(model.Row0.LengthSquared > Tolerance);
    }

    private static List<Entity> FindPitchPartsForTurret(World world, Entity owner)
    {
        Entity yaw = FindPartsOwnedBy(world, owner, ArticulatedPartType.TurretYaw).Single();
        var matches = new List<Entity>();
        foreach (var (entity, part) in world.Query<ArticulatedPartComponent>())
        {
            if (part.Owner == yaw && part.PartType == ArticulatedPartType.TurretPitch)
                matches.Add(entity);
        }

        return matches;
    }

    private static List<Entity> FindPartsOwnedBy(World world, Entity owner, ArticulatedPartType partType)
    {
        var matches = new List<Entity>();
        foreach (var (entity, part) in world.Query<ArticulatedPartComponent>())
        {
            if (part.Owner == owner && part.PartType == partType)
                matches.Add(entity);
        }

        return matches;
    }

    private static Vector3 ExtractWorldForward(Matrix4 model)
    {
        Vector3 forward = ArticulationMath.TransformPoint(new Vector3(0f, 0f, -1f), model)
            - ArticulationMath.TransformPoint(Vector3.Zero, model);
        return forward.LengthSquared > Tolerance ? Vector3.Normalize(forward) : Vector3.UnitZ;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static readonly string[] AllSixteenBaseTypes =
    [
        "command_center",
        "power_reactor",
        "supply_depot",
        "resource_refinery",
        "repair_bay",
        "fabrication_hub",
        "comms_relay",
        "orbital_uplink",
        "shield_emitter",
        "fortress_core",
        "defense_turret",
        "missile_battery",
        "sensor_array",
        "shipyard_small",
        "shipyard_medium",
        "shipyard_large",
    ];

    private static readonly Dictionary<string, int> ExpectedPartCounts = new(StringComparer.Ordinal)
    {
        ["command_center"] = 1,
        ["power_reactor"] = 1,
        ["supply_depot"] = 1,
        ["resource_refinery"] = 1,
        ["repair_bay"] = 2,
        ["fabrication_hub"] = 1,
        ["comms_relay"] = 1,
        ["orbital_uplink"] = 2,
        ["shield_emitter"] = 1,
        ["fortress_core"] = 2,
        ["defense_turret"] = 2,
        ["missile_battery"] = 1,
        ["sensor_array"] = 1,
        ["shipyard_small"] = 2,
        ["shipyard_medium"] = 2,
        ["shipyard_large"] = 2,
    };

    private static EntityDefinition LoadBaseDefinition(string baseId)
    {
        string path = Path.Combine(GetGameDataPath(), "Bases", $"{baseId}.json");
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<EntityDefinition>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to load base definition '{baseId}'.");
    }

    [Theory]
    [MemberData(nameof(AllBaseTypeIds))]
    public void BaseFactory_spawns_articulated_children(string baseType)
    {
        var world = new World();
        EntityDefinition def = LoadBaseDefinition(baseType);
        Entity owner = new BaseFactory().Create(world, def);

        int partCount = CountArticulatedPartsForOwner(world, owner);
        Assert.True(partCount >= 1, $"Expected at least one articulated child for '{baseType}'.");
        Assert.Equal(ExpectedPartCounts[baseType], partCount);
    }

    [Fact]
    public void BaseFactory_all_sixteen_types_have_distinct_part_counts()
    {
        var observed = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string baseType in AllSixteenBaseTypes)
        {
            var world = new World();
            Entity owner = new BaseFactory().Create(world, LoadBaseDefinition(baseType));
            observed[baseType] = CountArticulatedPartsForOwner(world, owner);
        }

        Assert.Equal(ExpectedPartCounts.Count, observed.Count);
        foreach ((string baseType, int expected) in ExpectedPartCounts)
            Assert.Equal(expected, observed[baseType]);
    }

    [Fact]
    public void ArticulationSpawner_json_matches_static_fallback_defense_turret()
    {
        var jsonWorld = new World();
        Entity jsonOwner = new BaseFactory().Create(jsonWorld, LoadBaseDefinition("defense_turret"));

        var staticWorld = new World();
        Entity staticOwner = new BaseFactory().Create(staticWorld, MakeDefenseTurretDefinition());

        var jsonYaw = jsonWorld.GetComponent<ArticulatedPartComponent>(
            FindPartsOwnedBy(jsonWorld, jsonOwner, ArticulatedPartType.TurretYaw).Single())!;
        var staticYaw = staticWorld.GetComponent<ArticulatedPartComponent>(
            FindPartsOwnedBy(staticWorld, staticOwner, ArticulatedPartType.TurretYaw).Single())!;

        Assert.True(Vector3.Distance(staticYaw.LocalPivotOffset, jsonYaw.LocalPivotOffset) < Tolerance);
        Assert.Equal(staticYaw.YawMin, jsonYaw.YawMin);
        Assert.Equal(staticYaw.YawMax, jsonYaw.YawMax);
        Assert.Equal(staticYaw.IdleSweepEnabled, jsonYaw.IdleSweepEnabled);
        Assert.Equal(staticYaw.IdleSweepSpeed, jsonYaw.IdleSweepSpeed, precision: 1);

        Entity jsonYawEntity = FindPartsOwnedBy(jsonWorld, jsonOwner, ArticulatedPartType.TurretYaw).Single();
        Entity staticYawEntity = FindPartsOwnedBy(staticWorld, staticOwner, ArticulatedPartType.TurretYaw).Single();

        var jsonPitch = jsonWorld.GetComponent<ArticulatedPartComponent>(
            FindPitchPartsForTurret(jsonWorld, jsonOwner).Single())!;
        var staticPitch = staticWorld.GetComponent<ArticulatedPartComponent>(
            FindPitchPartsForTurret(staticWorld, staticOwner).Single())!;

        Assert.Equal(staticYawEntity, staticPitch.Owner);
        Assert.Equal(jsonYawEntity, jsonPitch.Owner);
        Assert.True(Vector3.Distance(staticPitch.LocalPivotOffset, jsonPitch.LocalPivotOffset) < Tolerance);
        Assert.Equal(staticPitch.PitchMin, jsonPitch.PitchMin);
        Assert.Equal(staticPitch.PitchMax, jsonPitch.PitchMax);
    }

    [Fact]
    public void ComposePartModelMatrix_rotating_station_no_double_yaw()
    {
        var world = new World();
        Entity owner = new BaseFactory().Create(world, LoadBaseDefinition("sensor_array"));
        world.AddComponent(owner, new TransformComponent
        {
            Position = new Vector3(5f, 0f, -3f),
            EulerAngles = Vector3.Zero,
            Scale = Vector3.One * 1.8f,
        });
        world.AddComponent(owner, new BuildingComponent { Rotates = true });

        Entity dishPart = FindPartsOwnedBy(world, owner, ArticulatedPartType.SensorDish).Single();
        var dish = world.GetComponent<ArticulatedPartComponent>(dishPart)!;
        dish.CurrentYaw = 20f;

        var ownerTransform = world.GetComponent<TransformComponent>(owner)!;
        Matrix4 baseModel = ArticulationMath.ComposePartModelMatrix(
            ownerTransform.GetModelMatrix(),
            dish.LocalPivotOffset,
            dish.CurrentYaw,
            dish.CurrentPitch,
            dish.MeshLocalOffset,
            ownerTransform.Scale);
        Vector3 baseForward = ExtractWorldForward(baseModel);

        StationRotationSystem.UpdateStationRotations(world, 2f);
        ownerTransform = world.GetComponent<TransformComponent>(owner)!;
        Matrix4 rotatedModel = ArticulationMath.ComposePartModelMatrix(
            ownerTransform.GetModelMatrix(),
            dish.LocalPivotOffset,
            dish.CurrentYaw,
            dish.CurrentPitch,
            dish.MeshLocalOffset,
            ownerTransform.Scale);
        Vector3 rotatedForward = ExtractWorldForward(rotatedModel);

        float delta = Vector3.Dot(Vector3.Normalize(baseForward), Vector3.Normalize(rotatedForward));
        Assert.True(delta < 0.99f, "Dish world orientation should change when parent station yaws.");
    }

    [Fact]
    public void Skirmish_spawn_path_attaches_articulation_for_command_center()
    {
        var world = new World();
        Entity building = world.CreateEntity();
        world.AddComponent(building, new TransformComponent { Position = Vector3.Zero });
        world.AddComponent(building, new BuildingComponent
        {
            BuildingType = "command_center",
            Footprint = [2, 2],
            Rotates = false,
            PlayerId = 0,
        });

        BaseFactory.TrySpawnArticulation(world, building, "command_center", LoadBaseDefinition("command_center"));

        Assert.Equal(1, CountArticulatedPartsForOwner(world, building));
    }

    public static IEnumerable<object[]> AllBaseTypeIds() =>
        AllSixteenBaseTypes.Select(id => new object[] { id });

    private static int CountArticulatedPartsForOwner(World world, Entity owner)
    {
        int count = 0;
        foreach (var (_, part) in world.Query<ArticulatedPartComponent>())
        {
            if (ArticulationDrawHelper.ResolveRootOwner(world, part) == owner)
                count++;
        }

        return count;
    }

    private static string GetGameDataPath()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "SharpOpenGl.sln")))
            dir = Directory.GetParent(dir)?.FullName;
        if (dir == null)
            throw new InvalidOperationException("Could not locate SharpOpenGl.sln.");
        return Path.Combine(dir, "GameData");
    }
}