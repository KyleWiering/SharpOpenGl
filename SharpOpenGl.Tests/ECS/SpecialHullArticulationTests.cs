using OpenTK.Mathematics;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.ECS;

public class SpecialHullArticulationTests
{
    private static EntityDefinition BomberDef() => new()
    {
        Id = "bomber_heavy",
        Category = "bomber",
        Components = new ComponentsDefinition
        {
            Weapons =
            [
                new WeaponDefinition { Slot = 0, Type = "torpedo", Damage = 200, Range = 400, FireRate = 0.3f },
            ],
        },
        Articulation = new ArticulationDefinition
        {
            Parts =
            [
                new ArticulationPartDefinition
                {
                    Id = "bay_port",
                    PartType = "BayDoor",
                    LocalPivot = [-0.35f, 0.15f, 0.25f],
                    PitchMin = 0,
                    PitchMax = 90,
                    MeshKey = "articulated/bay_door/bomber_heavy/port",
                },
                new ArticulationPartDefinition
                {
                    Id = "bay_starboard",
                    PartType = "BayDoor",
                    LocalPivot = [0.35f, 0.15f, 0.25f],
                    PitchMin = 0,
                    PitchMax = 90,
                    MeshKey = "articulated/bay_door/bomber_heavy/starboard",
                },
            ],
        },
    };

    private static EntityDefinition ScoutDef() => new()
    {
        Id = "scout_light",
        Category = "scout",
        Components = new ComponentsDefinition
        {
            Weapons =
            [
                new WeaponDefinition { Slot = 0, Type = "laser", Damage = 8, Range = 150, FireRate = 3f },
            ],
        },
        Articulation = new ArticulationDefinition
        {
            Parts =
            [
                new ArticulationPartDefinition
                {
                    Id = "sensor_dish",
                    PartType = "SensorDish",
                    LocalPivot = [0f, 0.35f, 0.45f],
                    YawMin = -120,
                    YawMax = 120,
                    PitchMin = -15,
                    PitchMax = 45,
                    IdleSweep = true,
                    MeshKey = "articulated/sensor_dish/scout_light",
                },
            ],
        },
    };

    private static EntityDefinition DroneDef() => new()
    {
        Id = "drone_swarm",
        Category = "drone",
        Components = new ComponentsDefinition
        {
            Weapons =
            [
                new WeaponDefinition { Slot = 0, Type = "laser", Damage = 6, Range = 120, FireRate = 4f },
            ],
        },
        Articulation = new ArticulationDefinition
        {
            Parts =
            [
                new ArticulationPartDefinition
                {
                    Id = "launcher_pod",
                    PartType = "TurretYaw",
                    LocalPivot = [0f, 0.1f, -0.35f],
                    YawMin = -90,
                    YawMax = 90,
                    MeshKey = "articulated/launcher_pod/drone_swarm",
                },
            ],
        },
    };

    private static EntityDefinition CarrierDef() => new()
    {
        Id = "carrier_command",
        Category = "carrier",
        Components = new ComponentsDefinition
        {
            Weapons =
            [
                new WeaponDefinition { Slot = 0, Type = "flak", Damage = 20, Range = 250, FireRate = 8f },
            ],
        },
        Articulation = new ArticulationDefinition
        {
            Parts =
            [
                new ArticulationPartDefinition
                {
                    Id = "deck_elevator",
                    PartType = "BayDoor",
                    LocalPivot = [0f, 0.2f, -0.8f],
                    PitchMin = 0,
                    PitchMax = 60,
                    MeshKey = "articulated/deck_segment/carrier_command",
                },
            ],
        },
    };

    private static EntityDefinition FighterDef() => new()
    {
        Id = "fighter_basic",
        Category = "fighter",
        Components = new ComponentsDefinition
        {
            Movement = new MovementDefinition(),
            Weapons =
            [
                new WeaponDefinition { Slot = 0, Type = "laser", Damage = 15, Range = 200, FireRate = 2f },
            ],
        },
        Articulation = new ArticulationDefinition
        {
            Parts =
            [
                new ArticulationPartDefinition
                {
                    Id = "wing_flap_left",
                    PartType = "EngineGimbal",
                    LocalPivot = [-0.42f, 0.05f, -0.15f],
                    PitchMin = -5,
                    PitchMax = 25,
                    MeshKey = "articulated/wing_flap/fighter_basic/left",
                },
            ],
        },
    };

    private static EntityDefinition CorvetteDef() => new()
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
            ],
        },
    };

    private static Entity SpawnShip(World world, EntityDefinition def)
    {
        Entity ship = world.CreateEntity();
        world.AddComponent(ship, new TransformComponent { Position = Vector3.Zero, Scale = Vector3.One });
        world.AddComponent(ship, new EntityNameComponent { DefinitionId = def.Id });
        FactoryHelpers.ApplyWeapons(world, ship, def.Components?.Weapons);
        return ship;
    }

    private static List<Entity> FindBayDoors(World world, Entity owner) =>
        FindPartsOwnedBy(world, owner, ArticulatedPartType.BayDoor);

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

    [Fact]
    public void AttachSpecialHullParts_bomber_creates_two_bay_doors()
    {
        var world = new World();
        Entity ship = world.CreateEntity();
        var def = BomberDef();

        int created = SpecialHullArticulationSpawner.AttachSpecialHullParts(world, ship, def, "terran");

        Assert.Equal(2, created);
        Assert.Equal(2, FindBayDoors(world, ship).Count);
    }

    [Fact]
    public void AttachSpecialHullParts_scout_creates_sensor_dish()
    {
        var world = new World();
        Entity ship = world.CreateEntity();
        var def = ScoutDef();

        int created = SpecialHullArticulationSpawner.AttachSpecialHullParts(world, ship, def, "terran");

        Assert.Equal(1, created);
        var dishes = FindPartsOwnedBy(world, ship, ArticulatedPartType.SensorDish);
        Assert.Single(dishes);

        var part = world.GetComponent<ArticulatedPartComponent>(dishes[0])!;
        Assert.True(part.IdleSweepEnabled);
    }

    [Fact]
    public void AttachSpecialHullParts_idempotent()
    {
        var world = new World();
        Entity ship = world.CreateEntity();
        var def = BomberDef();

        int first = SpecialHullArticulationSpawner.AttachSpecialHullParts(world, ship, def, "terran");
        int second = SpecialHullArticulationSpawner.AttachSpecialHullParts(world, ship, def, "terran");

        Assert.Equal(2, first);
        Assert.Equal(0, second);
        Assert.Equal(2, FindBayDoors(world, ship).Count);
    }

    [Fact]
    public void AttachSpecialHullParts_drone_creates_launcher_pod()
    {
        var world = new World();
        Entity ship = world.CreateEntity();
        var def = DroneDef();

        int created = SpecialHullArticulationSpawner.AttachSpecialHullParts(world, ship, def, "terran");

        Assert.Equal(1, created);
        var pods = FindPartsOwnedBy(world, ship, ArticulatedPartType.LauncherPod);
        Assert.Single(pods);

        var render = world.GetComponent<RenderComponent>(pods[0]);
        Assert.NotNull(render);
        Assert.Equal("articulated/launcher_pod/drone_swarm", render!.MeshKey);
    }

    [Fact]
    public void BomberBay_doors_open_on_aggressive_stance()
    {
        var world = new World();
        var aimSystem = new SpecialHullArticulationAimSystem();
        var articulationSystem = new ArticulationSystem();

        Entity ship = SpawnShip(world, BomberDef());
        world.AddComponent(ship, new StanceComponent { CurrentStance = Stance.Aggressive });
        SpecialHullArticulationSpawner.AttachSpecialHullParts(world, ship, BomberDef(), "terran");

        world.AddSystem(aimSystem);
        world.AddSystem(articulationSystem);
        world.Update(0.016f);

        foreach (Entity door in FindBayDoors(world, ship))
        {
            var part = world.GetComponent<ArticulatedPartComponent>(door)!;
            Assert.InRange(part.TargetPitch, 89f, 91f);
        }
    }

    [Fact]
    public void BomberBay_doors_close_when_idle()
    {
        var world = new World();
        var aimSystem = new SpecialHullArticulationAimSystem();
        var articulationSystem = new ArticulationSystem();

        Entity ship = SpawnShip(world, BomberDef());
        world.AddComponent(ship, new StanceComponent { CurrentStance = Stance.Defensive });
        SpecialHullArticulationSpawner.AttachSpecialHullParts(world, ship, BomberDef(), "terran");

        world.AddSystem(aimSystem);
        world.AddSystem(articulationSystem);
        world.Update(0.016f);

        foreach (Entity door in FindBayDoors(world, ship))
        {
            var part = world.GetComponent<ArticulatedPartComponent>(door)!;
            Assert.InRange(part.TargetPitch, -1f, 1f);
        }
    }

    [Fact]
    public void CarrierDeck_idle_cycle_oscillates_pitch()
    {
        var world = new World();
        var aimSystem = new SpecialHullArticulationAimSystem();
        var articulationSystem = new ArticulationSystem();

        Entity ship = SpawnShip(world, CarrierDef());
        SpecialHullArticulationSpawner.AttachSpecialHullParts(world, ship, CarrierDef(), "terran");
        Entity deck = FindBayDoors(world, ship).Single();

        world.AddSystem(aimSystem);
        world.AddSystem(articulationSystem);

        world.Update(0f);
        float pitchA = world.GetComponent<ArticulatedPartComponent>(deck)!.TargetPitch;

        world.Update(3f);
        float pitchB = world.GetComponent<ArticulatedPartComponent>(deck)!.TargetPitch;

        Assert.NotEqual(pitchA, pitchB);
        Assert.InRange(pitchB, 0f, 60f);
    }

    [Fact]
    public void DroneLauncher_pod_yaws_toward_target()
    {
        CombatBalance.ResetForTests();
        var world = new World();
        var aimSystem = new SpecialHullArticulationAimSystem();
        var articulationSystem = new ArticulationSystem();

        Entity ship = SpawnShip(world, DroneDef());
        world.AddComponent(ship, new CombatTargetComponent { Faction = 1 });
        SpecialHullArticulationSpawner.AttachSpecialHullParts(world, ship, DroneDef(), "terran");
        Entity pod = FindPartsOwnedBy(world, ship, ArticulatedPartType.LauncherPod).Single();

        Entity target = world.CreateEntity();
        world.AddComponent(target, new TransformComponent { Position = new Vector3(40f, 0f, -10f) });
        world.AddComponent(target, new HealthComponent { MaxHP = 100f, CurrentHP = 100f });
        world.AddComponent(target, new CombatTargetComponent { Faction = 2 });
        world.GetComponent<CombatTargetComponent>(ship)!.CurrentTarget = target;

        world.AddSystem(aimSystem);
        world.AddSystem(articulationSystem);
        world.Update(0.016f);

        var part = world.GetComponent<ArticulatedPartComponent>(pod)!;
        Assert.NotEqual(0f, part.TargetYaw);
    }

    [Fact]
    public void ScoutSensor_tracks_target_in_range()
    {
        CombatBalance.ResetForTests();
        var world = new World();
        var aimSystem = new SpecialHullArticulationAimSystem();
        var articulationSystem = new ArticulationSystem();

        Entity ship = SpawnShip(world, ScoutDef());
        world.AddComponent(ship, new CombatTargetComponent { Faction = 1 });
        SpecialHullArticulationSpawner.AttachSpecialHullParts(world, ship, ScoutDef(), "terran");
        Entity dish = FindPartsOwnedBy(world, ship, ArticulatedPartType.SensorDish).Single();

        Entity target = world.CreateEntity();
        world.AddComponent(target, new TransformComponent { Position = new Vector3(30f, 5f, -20f) });
        world.AddComponent(target, new HealthComponent { MaxHP = 100f, CurrentHP = 100f });
        world.AddComponent(target, new CombatTargetComponent { Faction = 2 });
        world.GetComponent<CombatTargetComponent>(ship)!.CurrentTarget = target;

        world.AddSystem(aimSystem);
        world.AddSystem(articulationSystem);
        world.Update(0.016f);

        var part = world.GetComponent<ArticulatedPartComponent>(dish)!;
        Assert.True(part.HasAimTarget);
        Assert.NotEqual(0f, part.TargetYaw);
    }

    [Fact]
    public void ScoutSensor_idle_sweeps_without_target()
    {
        var world = new World();
        var aimSystem = new SpecialHullArticulationAimSystem();
        var articulationSystem = new ArticulationSystem();

        Entity ship = SpawnShip(world, ScoutDef());
        SpecialHullArticulationSpawner.AttachSpecialHullParts(world, ship, ScoutDef(), "terran");
        Entity dish = FindPartsOwnedBy(world, ship, ArticulatedPartType.SensorDish).Single();

        world.AddSystem(aimSystem);
        world.AddSystem(articulationSystem);
        world.Update(1f);

        var part = world.GetComponent<ArticulatedPartComponent>(dish)!;
        Assert.False(part.HasAimTarget);
        Assert.NotEqual(0f, part.CurrentYaw);
    }

    [Fact]
    public void FighterWingFlap_deflects_under_thrust()
    {
        var world = new World();
        var aimSystem = new SpecialHullArticulationAimSystem();
        var articulationSystem = new ArticulationSystem();

        Entity ship = SpawnShip(world, FighterDef());
        world.AddComponent(ship, new MovementComponent { Velocity = new Vector3(2f, 0f, 1f) });
        SpecialHullArticulationSpawner.AttachSpecialHullParts(world, ship, FighterDef(), "terran");
        Entity flap = FindPartsOwnedBy(world, ship, ArticulatedPartType.EngineGimbal).Single();

        world.AddSystem(aimSystem);
        world.AddSystem(articulationSystem);
        world.Update(0.016f);

        var thrustPart = world.GetComponent<ArticulatedPartComponent>(flap)!;
        Assert.NotEqual(0f, thrustPart.TargetPitch);

        world.GetComponent<MovementComponent>(ship)!.Velocity = Vector3.Zero;
        world.Update(0.016f);

        thrustPart = world.GetComponent<ArticulatedPartComponent>(flap)!;
        Assert.False(thrustPart.HasAimTarget);
        Assert.Equal(0f, thrustPart.TargetPitch);
    }

    [Fact]
    public void SpecialHull_drivers_do_not_break_turret_aim()
    {
        CombatBalance.ResetForTests();
        var world = new World();
        var aimSystem = new SpecialHullArticulationAimSystem();
        var articulationSystem = new ArticulationSystem();

        EntityDefinition def = CorvetteDef();
        Entity ship = new ShipFactory().Create(world, def);
        world.AddComponent(ship, new TransformComponent { Position = Vector3.Zero, Scale = Vector3.One });
        world.AddComponent(ship, new EntityNameComponent { DefinitionId = def.Id });
        world.AddComponent(ship, new CombatTargetComponent { Faction = 1, TargetingMode = TargetPriority.Closest });
        world.AddComponent(ship, new StanceComponent { CurrentStance = Stance.Aggressive });

        Entity bayDoor = world.CreateEntity();
        world.AddComponent(bayDoor, new ArticulatedPartComponent
        {
            Owner = ship,
            PartType = ArticulatedPartType.BayDoor,
            LocalPivotOffset = new Vector3(-0.35f, 0.15f, 0.25f),
            PitchMin = 0f,
            PitchMax = 90f,
        });

        Entity yawPart = FindPartsOwnedBy(world, ship, ArticulatedPartType.TurretYaw).Single();

        Entity target = world.CreateEntity();
        world.AddComponent(target, new TransformComponent { Position = new Vector3(50f, 0.4f, 0.6f) });
        world.AddComponent(target, new HealthComponent { MaxHP = 100f, CurrentHP = 100f });
        world.AddComponent(target, new CombatTargetComponent { Faction = 2 });
        world.GetComponent<CombatTargetComponent>(ship)!.CurrentTarget = target;

        world.AddSystem(aimSystem);
        world.AddSystem(articulationSystem);
        world.Update(0.016f);

        var yaw = world.GetComponent<ArticulatedPartComponent>(yawPart)!;
        Assert.True(yaw.HasAimTarget);
        Assert.InRange(yaw.TargetYaw, 85f, 95f);
    }

    [Fact]
    public void AttachSpecialHullParts_fallback_without_json_articulation()
    {
        var world = new World();
        Entity ship = world.CreateEntity();
        var def = new EntityDefinition { Id = "scout_light", Category = "scout" };

        int created = SpecialHullArticulationSpawner.AttachSpecialHullParts(world, ship, def, "terran");

        Assert.Equal(1, created);
        Assert.Single(FindPartsOwnedBy(world, ship, ArticulatedPartType.SensorDish));
    }

}