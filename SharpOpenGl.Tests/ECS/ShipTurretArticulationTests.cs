using System.Text.Json;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.ECS;

public class ShipTurretArticulationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static IEnumerable<object[]> ArmedHullIds =>
    [
        ["fighter_basic"],
        ["interceptor_mk2"],
        ["scout_light"],
        ["drone_swarm"],
        ["corvette_fast"],
        ["frigate_strike"],
        ["gunship_heavy"],
        ["bomber_heavy"],
        ["destroyer_assault"],
        ["cruiser_heavy"],
        ["hero_default"],
        ["carrier_command"],
        ["dreadnought"],
        ["dreadnought_malthus"],
    ];

    public static readonly Dictionary<string, int> ExpectedArticulatedPartCounts = new(StringComparer.Ordinal)
    {
        ["fighter_basic"] = 3,
        ["interceptor_mk2"] = 3,
        ["scout_light"] = 3,
        ["drone_swarm"] = 3,
        ["corvette_fast"] = 2,
        ["frigate_strike"] = 2,
        ["gunship_heavy"] = 2,
        ["bomber_heavy"] = 4,
        ["destroyer_assault"] = 2,
        ["cruiser_heavy"] = 6,
        ["hero_default"] = 4,
        ["carrier_command"] = 5,
        ["dreadnought"] = 8,
        ["dreadnought_malthus"] = 12,
    };

    private static EntityDefinition MakeArmedHullDefinition(string hullId) => hullId switch
    {
        "gunship_heavy" => new EntityDefinition
        {
            Id = "gunship_heavy",
            Category = "gunship",
            Components = new ComponentsDefinition
            {
                Health = new HealthDefinition { MaxHP = 320, Shields = 60, Armor = 30 },
                Movement = new MovementDefinition { Speed = 60, Acceleration = 70, TurnRate = 120 },
                Weapons =
                [
                    new WeaponDefinition { Slot = 0, Type = "laser", Damage = 32, Range = 280, FireRate = 2.0f },
                    new WeaponDefinition { Slot = 1, Type = "missile", Damage = 60, Range = 360, FireRate = 0.5f },
                ],
            },
        },
        "destroyer_assault" => new EntityDefinition
        {
            Id = "destroyer_assault",
            Category = "destroyer",
            Components = new ComponentsDefinition
            {
                Health = new HealthDefinition { MaxHP = 500, Shields = 80, Armor = 40 },
                Movement = new MovementDefinition { Speed = 55, Acceleration = 50, TurnRate = 90 },
                Weapons =
                [
                    new WeaponDefinition { Slot = 0, Type = "laser", Damage = 40, Range = 340, FireRate = 1.8f },
                ],
            },
        },
        _ => MakeCorvetteDefinition(),
    };

    private static EntityDefinition MakeCorvetteDefinition() => new()
    {
        Id = "corvette_fast",
        Category = "corvette",
        Components = new ComponentsDefinition
        {
            Health = new HealthDefinition { MaxHP = 180, Shields = 40, Armor = 20 },
            Movement = new MovementDefinition { Speed = 75, Acceleration = 90, TurnRate = 150 },
            Weapons =
            [
                new WeaponDefinition { Slot = 0, Type = "laser", Damage = 28, Range = 260, FireRate = 2.2f },
                new WeaponDefinition { Slot = 1, Type = "missile", Damage = 55, Range = 320, FireRate = 0.4f },
            ],
        },
    };

    [Theory]
    [MemberData(nameof(ArmedHullIds))]
    public void ShipFactory_spawns_correct_child_count_per_hull(string hullId)
    {
        EntityDefinition def = LoadShip(hullId);
        var world = new World();
        Entity ship = new ShipFactory().Create(world, def);

        int count = CountArticulatedPartsForHull(world, ship);
        Assert.Equal(ExpectedArticulatedPartCounts[hullId], count);
    }

    [Fact]
    public void Fighter_basic_from_json_spawns_pitch_only_turret_child()
    {
        EntityDefinition def = LoadShip("fighter_basic");
        var world = new World();
        Entity ship = new ShipFactory().Create(world, def);

        var turretParts = FindTurretPartsForHull(world, ship);
        Assert.Single(turretParts);

        var pitch = world.GetComponent<ArticulatedPartComponent>(turretParts[0])!;
        Assert.Equal(ArticulatedPartType.TurretPitch, pitch.PartType);
        Assert.Equal(new Vector3(0f, 0.05f, 0.45f), pitch.LocalPivotOffset);
    }

    [Fact]
    public void Frigate_strike_from_json_spawns_yaw_and_pitch_with_hull_mesh_keys()
    {
        EntityDefinition def = LoadShip("frigate_strike");
        var world = new World();
        Entity ship = new ShipFactory().Create(world, def);

        var yawParts = FindPartsOwnedBy(world, ship, ArticulatedPartType.TurretYaw);
        var pitchParts = FindPitchPartsForTurret(world, ship);

        Assert.Single(yawParts);
        Assert.Single(pitchParts);

        Assert.Equal(
            ArticulatedShipPartMeshes.BuildPartKey("turret_yaw", "frigate_strike"),
            world.GetComponent<RenderComponent>(yawParts[0])!.MeshKey);
        Assert.Equal(
            ArticulatedShipPartMeshes.BuildPartKey("turret_pitch", "frigate_strike"),
            world.GetComponent<RenderComponent>(pitchParts[0])!.MeshKey);
    }

    [Fact]
    public void Corvette_fast_from_json_matches_prior_static_pivot_values()
    {
        EntityDefinition def = LoadShip("corvette_fast");
        var world = new World();
        Entity ship = new ShipFactory().Create(world, def);

        Entity yawPart = FindPartsOwnedBy(world, ship, ArticulatedPartType.TurretYaw).Single();
        Entity pitchPart = FindPitchPartsForTurret(world, ship).Single();

        var yaw = world.GetComponent<ArticulatedPartComponent>(yawPart)!;
        var pitch = world.GetComponent<ArticulatedPartComponent>(pitchPart)!;

        Assert.Equal(new Vector3(0f, 0.4f, 0.6f), yaw.LocalPivotOffset);
        Assert.Equal(new Vector3(0f, 0.15f, 0.2f), pitch.LocalPivotOffset);
        Assert.Equal(-120f, yaw.YawMin);
        Assert.Equal(120f, yaw.YawMax);
        Assert.Equal(-10f, pitch.PitchMin);
        Assert.Equal(45f, pitch.PitchMax);
        Assert.True(yaw.IdleSweepEnabled);
        Assert.Equal(8f, yaw.IdleSweepSpeed);
    }

    [Fact]
    public void Cruiser_heavy_spawns_three_independent_yaw_roots()
    {
        EntityDefinition def = LoadShip("cruiser_heavy");
        var world = new World();
        Entity ship = new ShipFactory().Create(world, def);

        var yawRoots = FindPartsOwnedBy(world, ship, ArticulatedPartType.TurretYaw);
        Assert.Equal(3, yawRoots.Count);
    }

    [Fact]
    public void Dreadnought_malthus_spawns_six_yaw_roots()
    {
        EntityDefinition def = LoadShip("dreadnought_malthus");
        var world = new World();
        Entity ship = new ShipFactory().Create(world, def);

        var yawRoots = FindPartsOwnedBy(world, ship, ArticulatedPartType.TurretYaw);
        Assert.Equal(6, yawRoots.Count);
    }

    [Fact]
    public void Carrier_command_spawns_two_defensive_turrets()
    {
        EntityDefinition def = LoadShip("carrier_command");
        var world = new World();
        Entity ship = new ShipFactory().Create(world, def);

        var yawParts = FindPartsOwnedBy(world, ship, ArticulatedPartType.TurretYaw);
        var pitchParts = FindAllTurretPitchParts(world, ship);

        Assert.Equal(2, yawParts.Count);
        Assert.Equal(2, pitchParts.Count);
    }

    [Fact]
    public void JSON_pivot_offsets_match_definition_for_cruiser_bow_and_dreadnought_port()
    {
        EntityDefinition cruiserDef = LoadShip("cruiser_heavy");
        var world = new World();
        Entity cruiser = new ShipFactory().Create(world, cruiserDef);

        Entity bowYaw = FindPartByExpectedPivot(
            world,
            cruiser,
            ArticulatedPartType.TurretYaw,
            new Vector3(0f, 0.55f, 1.0f));
        Assert.NotEqual(Entity.Null, bowYaw);

        EntityDefinition dreadDef = LoadShip("dreadnought");
        Entity dreadnought = new ShipFactory().Create(world, dreadDef);

        Entity portYaw = FindPartByExpectedPivot(
            world,
            dreadnought,
            ArticulatedPartType.TurretYaw,
            new Vector3(-0.75f, 0.5f, 0.0f));
        Assert.NotEqual(Entity.Null, portYaw);
    }

    [Theory]
    [InlineData("corvette_fast")]
    [InlineData("gunship_heavy")]
    [InlineData("destroyer_assault")]
    public void ShipFactory_spawns_yaw_and_pitch_children_with_mesh_keys(string hullId)
    {
        var world = new World();
        Entity ship = new ShipFactory().Create(world, MakeArmedHullDefinition(hullId));

        var yawParts = FindPartsOwnedBy(world, ship, ArticulatedPartType.TurretYaw);
        var pitchParts = FindPitchPartsForTurret(world, ship);

        Assert.Single(yawParts);
        Assert.Single(pitchParts);

        var yaw = world.GetComponent<ArticulatedPartComponent>(yawParts[0])!;
        var pitch = world.GetComponent<ArticulatedPartComponent>(pitchParts[0])!;
        Assert.Equal(ArticulatedPartType.TurretYaw, yaw.PartType);
        Assert.Equal(ArticulatedPartType.TurretPitch, pitch.PartType);

        Assert.Equal(
            ArticulatedShipPartMeshes.BuildPartKey("turret_yaw", hullId),
            world.GetComponent<RenderComponent>(yawParts[0])!.MeshKey);
        Assert.Equal(
            ArticulatedShipPartMeshes.BuildPartKey("turret_pitch", hullId),
            world.GetComponent<RenderComponent>(pitchParts[0])!.MeshKey);
    }

    public static IEnumerable<object[]> YawTurretHullIds =>
    [
        ["scout_light"],
        ["corvette_fast"],
        ["frigate_strike"],
        ["gunship_heavy"],
        ["bomber_heavy"],
        ["destroyer_assault"],
        ["cruiser_heavy"],
        ["hero_default"],
        ["carrier_command"],
        ["dreadnought"],
        ["dreadnought_malthus"],
    ];

    [Theory]
    [MemberData(nameof(YawTurretHullIds))]
    public void CombatAim_sets_target_yaw_toward_mock_target_for_armed_hull(string hullId)
    {
        CombatBalance.ResetForTests();
        EntityDefinition def = LoadShip(hullId);
        var (world, system, ship, yawPart, _) = SpawnCombatShip(def, Vector3.Zero);

        Entity target = world.CreateEntity();
        world.AddComponent(target, new TransformComponent { Position = new Vector3(50f, 0.4f, 0.6f) });
        world.AddComponent(target, new HealthComponent { MaxHP = 100f, CurrentHP = 100f });
        world.AddComponent(target, new CombatTargetComponent { Faction = 2 });

        world.GetComponent<CombatTargetComponent>(ship)!.CurrentTarget = target;

        system.Update(world, 0.016f);

        var yaw = world.GetComponent<ArticulatedPartComponent>(yawPart)!;
        Assert.True(yaw.HasAimTarget);
        Assert.InRange(yaw.TargetYaw, 85f, 95f);
    }

    [Fact]
    public void CombatAim_sets_target_yaw_toward_mock_target_in_range()
    {
        CombatBalance.ResetForTests();
        var (world, system, ship, yawPart, _) = SpawnCombatShip(LoadShip("corvette_fast"), Vector3.Zero);

        Entity target = world.CreateEntity();
        world.AddComponent(target, new TransformComponent { Position = new Vector3(50f, 0.4f, 0.6f) });
        world.AddComponent(target, new HealthComponent { MaxHP = 100f, CurrentHP = 100f });
        world.AddComponent(target, new CombatTargetComponent { Faction = 2 });

        world.GetComponent<CombatTargetComponent>(ship)!.CurrentTarget = target;

        system.Update(world, 0.016f);

        var yaw = world.GetComponent<ArticulatedPartComponent>(yawPart)!;
        Assert.True(yaw.HasAimTarget);
        Assert.Equal(90f, yaw.TargetYaw, precision: 2);
    }

    [Fact]
    public void CombatAim_tracks_target_in_range_for_frigate_strike()
    {
        CombatBalance.ResetForTests();
        var (world, system, ship, yawPart, _) = SpawnCombatShip(LoadShip("frigate_strike"), Vector3.Zero);

        Entity target = world.CreateEntity();
        world.AddComponent(target, new TransformComponent { Position = new Vector3(50f, 0.4f, 0.6f) });
        world.AddComponent(target, new HealthComponent { MaxHP = 100f, CurrentHP = 100f });
        world.AddComponent(target, new CombatTargetComponent { Faction = 2 });

        world.GetComponent<CombatTargetComponent>(ship)!.CurrentTarget = target;

        system.Update(world, 0.016f);

        var yaw = world.GetComponent<ArticulatedPartComponent>(yawPart)!;
        Assert.True(yaw.HasAimTarget);
        Assert.InRange(yaw.TargetYaw, 85f, 95f);
    }

    [Fact]
    public void CombatAim_cruiser_heavy_all_yaw_parts_slew_toward_target()
    {
        CombatBalance.ResetForTests();
        EntityDefinition def = LoadShip("cruiser_heavy");
        var world = new World();
        var system = new ArticulationSystem();

        Entity ship = new ShipFactory().Create(world, def);
        world.AddComponent(ship, new TransformComponent { Position = Vector3.Zero, Scale = Vector3.One });
        world.AddComponent(ship, new CombatTargetComponent { Faction = 1, TargetingMode = TargetPriority.Closest });

        Entity target = world.CreateEntity();
        world.AddComponent(target, new TransformComponent { Position = new Vector3(50f, 0.4f, 0.6f) });
        world.AddComponent(target, new HealthComponent { MaxHP = 100f, CurrentHP = 100f });
        world.AddComponent(target, new CombatTargetComponent { Faction = 2 });
        world.GetComponent<CombatTargetComponent>(ship)!.CurrentTarget = target;

        system.Update(world, 0.5f);

        var yawRoots = FindPartsOwnedBy(world, ship, ArticulatedPartType.TurretYaw);
        Assert.Equal(3, yawRoots.Count);

        foreach (Entity yawPart in yawRoots)
        {
            var yaw = world.GetComponent<ArticulatedPartComponent>(yawPart)!;
            Assert.True(yaw.HasAimTarget);
            Assert.InRange(yaw.TargetYaw, 85f, 95f);
            Assert.True(yaw.CurrentYaw > 0f);
            Assert.True(yaw.CurrentYaw < yaw.TargetYaw);
        }
    }

    [Fact]
    public void CombatAim_sets_target_pitch_for_elevated_target()
    {
        CombatBalance.ResetForTests();
        var (world, system, ship, _, pitchPart) = SpawnCombatShip(LoadShip("corvette_fast"), Vector3.Zero);

        Entity target = world.CreateEntity();
        world.AddComponent(target, new TransformComponent { Position = new Vector3(0f, 60f, -80f) });
        world.AddComponent(target, new HealthComponent { MaxHP = 100f, CurrentHP = 100f });
        world.AddComponent(target, new CombatTargetComponent { Faction = 2 });

        world.GetComponent<CombatTargetComponent>(ship)!.CurrentTarget = target;

        system.Update(world, 0.016f);

        var pitch = world.GetComponent<ArticulatedPartComponent>(pitchPart)!;
        Assert.True(pitch.HasAimTarget);
        Assert.True(pitch.TargetPitch > 0f);
    }

    [Fact]
    public void CombatAim_clears_aim_when_target_out_of_range()
    {
        CombatBalance.ResetForTests();
        var (world, system, ship, yawPart, pitchPart) = SpawnCombatShip(LoadShip("corvette_fast"), Vector3.Zero);

        float maxRange = world.GetComponent<WeaponListComponent>(ship)!.Weapons.Max(w => w.Range);

        Entity target = world.CreateEntity();
        world.AddComponent(target, new TransformComponent { Position = new Vector3(maxRange + 50f, 0f, 0f) });
        world.AddComponent(target, new HealthComponent { MaxHP = 100f, CurrentHP = 100f });
        world.AddComponent(target, new CombatTargetComponent { Faction = 2 });

        world.GetComponent<CombatTargetComponent>(ship)!.CurrentTarget = target;

        system.Update(world, 0.016f);

        var yaw = world.GetComponent<ArticulatedPartComponent>(yawPart)!;
        var pitch = world.GetComponent<ArticulatedPartComponent>(pitchPart)!;
        Assert.False(yaw.HasAimTarget);
        Assert.False(pitch.HasAimTarget);
    }

    [Fact]
    public void CombatAim_clears_aim_when_target_dead()
    {
        CombatBalance.ResetForTests();
        var (world, system, ship, yawPart, pitchPart) = SpawnCombatShip(LoadShip("corvette_fast"), Vector3.Zero);

        Entity target = world.CreateEntity();
        world.AddComponent(target, new TransformComponent { Position = new Vector3(40f, 0f, -40f) });
        world.AddComponent(target, new HealthComponent { MaxHP = 100f, CurrentHP = 0f });
        world.AddComponent(target, new CombatTargetComponent { Faction = 2 });

        world.GetComponent<CombatTargetComponent>(ship)!.CurrentTarget = target;

        system.Update(world, 0.016f);

        var yaw = world.GetComponent<ArticulatedPartComponent>(yawPart)!;
        var pitch = world.GetComponent<ArticulatedPartComponent>(pitchPart)!;
        Assert.False(yaw.HasAimTarget);
        Assert.False(pitch.HasAimTarget);
    }

    [Fact]
    public void CombatAim_skips_fog_hidden_target_for_player_unit()
    {
        CombatBalance.ResetForTests();
        var grid = new GridSystem(32, 32, cellSize: 10f);
        var fog = new FogOfWar(grid, playerCount: 1);
        var gate = new CombatFogGate { Grid = grid, Fog = fog };

        var (world, system, ship, yawPart, pitchPart) = SpawnCombatShip(
            LoadShip("corvette_fast"),
            grid.GridToWorld(0, 0),
            gate);

        Entity target = world.CreateEntity();
        world.AddComponent(target, new TransformComponent { Position = grid.GridToWorld(20, 20) });
        world.AddComponent(target, new HealthComponent { MaxHP = 100f, CurrentHP = 100f });
        world.AddComponent(target, new CombatTargetComponent { Faction = 2 });

        world.GetComponent<CombatTargetComponent>(ship)!.CurrentTarget = target;

        system.Update(world, 0.016f);

        var yaw = world.GetComponent<ArticulatedPartComponent>(yawPart)!;
        var pitch = world.GetComponent<ArticulatedPartComponent>(pitchPart)!;
        Assert.False(yaw.HasAimTarget);
        Assert.False(pitch.HasAimTarget);
    }

    [Fact]
    public void Update_slews_turret_toward_target_over_frames()
    {
        CombatBalance.ResetForTests();
        var (world, system, ship, yawPart, _) = SpawnCombatShip(LoadShip("corvette_fast"), Vector3.Zero);

        Entity target = world.CreateEntity();
        world.AddComponent(target, new TransformComponent { Position = new Vector3(50f, 0.4f, 0.6f) });
        world.AddComponent(target, new HealthComponent { MaxHP = 100f, CurrentHP = 100f });
        world.AddComponent(target, new CombatTargetComponent { Faction = 2 });

        world.GetComponent<CombatTargetComponent>(ship)!.CurrentTarget = target;

        system.Update(world, 0.5f);

        var yaw = world.GetComponent<ArticulatedPartComponent>(yawPart)!;
        Assert.True(yaw.HasAimTarget);
        Assert.Equal(90f, yaw.TargetYaw, precision: 2);
        Assert.True(yaw.CurrentYaw > 0f);
        Assert.True(yaw.CurrentYaw < yaw.TargetYaw);
    }

    private static (World world, ArticulationSystem system, Entity ship, Entity yawPart, Entity pitchPart)
        SpawnCombatShip(EntityDefinition def, Vector3 shipPosition, CombatFogGate? fogGate = null)
    {
        var world = new World();
        var system = new ArticulationSystem(fogGate);

        Entity ship = new ShipFactory().Create(world, def);
        world.AddComponent(ship, new TransformComponent
        {
            Position = shipPosition,
            Scale = Vector3.One,
        });
        world.AddComponent(ship, new CombatTargetComponent
        {
            Faction = 1,
            TargetingMode = TargetPriority.Closest,
        });

        List<Entity> yawParts = FindPartsOwnedBy(world, ship, ArticulatedPartType.TurretYaw);
        Entity yawPart = yawParts.Count > 0
            ? yawParts[0]
            : FindAllTurretPitchParts(world, ship).First();
        Entity pitchPart = FindAllTurretPitchParts(world, ship).First();

        return (world, system, ship, yawPart, pitchPart);
    }

    private static EntityDefinition LoadShip(string id)
    {
        string path = Path.Combine(GetGameDataPath(), "Ships", $"{id}.json");
        string json = File.ReadAllText(path);
        var def = JsonSerializer.Deserialize<EntityDefinition>(json, JsonOptions);
        Assert.NotNull(def);
        return def!;
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

    private static int CountArticulatedPartsForHull(World world, Entity hull)
    {
        int count = 0;
        foreach (var (_, part) in world.Query<ArticulatedPartComponent>())
        {
            if (ArticulationDrawHelper.ResolveRootOwner(world, part) == hull)
                count++;
        }

        return count;
    }

    private static List<Entity> FindTurretPartsForHull(World world, Entity hull)
    {
        var matches = new List<Entity>();
        foreach (var (entity, part) in world.Query<ArticulatedPartComponent>())
        {
            if (ArticulationDrawHelper.ResolveRootOwner(world, part) != hull)
                continue;

            if (part.PartType is ArticulatedPartType.TurretYaw or ArticulatedPartType.TurretPitch)
                matches.Add(entity);
        }

        return matches;
    }

    private static List<Entity> FindAllTurretPitchParts(World world, Entity hull)
    {
        var matches = new List<Entity>();
        foreach (var (entity, part) in world.Query<ArticulatedPartComponent>())
        {
            if (ArticulationDrawHelper.ResolveRootOwner(world, part) != hull)
                continue;

            if (part.PartType == ArticulatedPartType.TurretPitch)
                matches.Add(entity);
        }

        return matches;
    }

    private static Entity FindPartByExpectedPivot(
        World world,
        Entity hull,
        ArticulatedPartType partType,
        Vector3 expectedPivot)
    {
        foreach (var (entity, part) in world.Query<ArticulatedPartComponent>())
        {
            if (ArticulationDrawHelper.ResolveRootOwner(world, part) != hull)
                continue;

            if (part.PartType == partType && part.LocalPivotOffset == expectedPivot)
                return entity;
        }

        return Entity.Null;
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

    private static List<Entity> FindPitchPartsForTurret(World world, Entity hull)
    {
        Entity yaw = FindPartsOwnedBy(world, hull, ArticulatedPartType.TurretYaw).Single();
        var matches = new List<Entity>();
        foreach (var (entity, part) in world.Query<ArticulatedPartComponent>())
        {
            if (part.Owner == yaw && part.PartType == ArticulatedPartType.TurretPitch)
                matches.Add(entity);
        }

        return matches;
    }
}