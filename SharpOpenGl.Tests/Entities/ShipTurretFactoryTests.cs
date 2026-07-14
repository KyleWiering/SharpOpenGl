using System.Text.Json;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Entities;

public class ShipTurretFactoryTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
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
            ],
        },
    };

    private static EntityDefinition MakeMinerDefinition() => new()
    {
        Id = "miner_basic",
        Category = "miner",
        Components = new ComponentsDefinition
        {
            Health = new HealthDefinition { MaxHP = 150, Armor = 20 },
            Movement = new MovementDefinition { Speed = 45, Acceleration = 60, TurnRate = 120 },
            Weapons = [],
            ResourceCollector = new ResourceCollectorDefinition
            {
                HarvestMode = "drones",
                HarvestRange = 28,
                HarvestRate = 15,
            },
        },
    };

    private static EntityDefinition MakeDestroyerDefinition() => new()
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
    };

    [Fact]
    public void Create_corvette_fast_spawns_yaw_and_pitch_children_with_weapon_list()
    {
        var world = new World();
        Entity hull = new ShipFactory().Create(world, MakeCorvetteDefinition());

        Assert.NotNull(world.GetComponent<WeaponListComponent>(hull));

        var yawParts = FindPartsOwnedBy(world, hull, ArticulatedPartType.TurretYaw);
        var pitchParts = FindPitchPartsForTurret(world, hull);

        Assert.Single(yawParts);
        Assert.Single(pitchParts);
    }

    [Fact]
    public void Create_unarmed_miner_spawns_no_turret_children()
    {
        var world = new World();
        Entity hull = new ShipFactory().Create(world, MakeMinerDefinition());

        Assert.Null(world.GetComponent<WeaponListComponent>(hull));
        Assert.Empty(FindAllArticulatedParts(world, hull));
    }

    [Fact]
    public void Create_miner_hull_skips_turret_children()
    {
        var world = new World();
        Entity hull = new ShipFactory().Create(world, MakeMinerDefinition());

        Assert.Null(world.GetComponent<WeaponListComponent>(hull));
        Assert.Empty(FindAllArticulatedParts(world, hull));
    }

    [Fact]
    public void Create_cruiser_heavy_spawns_multi_turret_children_with_weapon_list()
    {
        EntityDefinition def = LoadShip("cruiser_heavy");
        var world = new World();
        Entity hull = new ShipFactory().Create(world, def);

        Assert.NotNull(world.GetComponent<WeaponListComponent>(hull));

        var yawParts = FindPartsOwnedBy(world, hull, ArticulatedPartType.TurretYaw);
        var pitchParts = FindAllTurretPitchParts(world, hull);

        Assert.Equal(3, yawParts.Count);
        Assert.Equal(3, pitchParts.Count);
    }

    [Fact]
    public void Create_fighter_basic_spawns_limited_articulation()
    {
        EntityDefinition def = LoadShip("fighter_basic");
        var world = new World();
        Entity hull = new ShipFactory().Create(world, def);

        var turretParts = FindTurretPartsForHull(world, hull);
        Assert.Single(turretParts);

        var part = world.GetComponent<ArticulatedPartComponent>(turretParts[0])!;
        Assert.Equal(ArticulatedPartType.TurretPitch, part.PartType);
    }

    [Fact]
    public void Turret_yaw_pitch_limits_match_hull_table()
    {
        var world = new World();
        Entity hull = new ShipFactory().Create(world, MakeDestroyerDefinition());

        Entity yawPart = FindPartsOwnedBy(world, hull, ArticulatedPartType.TurretYaw).Single();
        Entity pitchPart = FindPitchPartsForTurret(world, hull).Single();

        var yaw = world.GetComponent<ArticulatedPartComponent>(yawPart)!;
        var pitch = world.GetComponent<ArticulatedPartComponent>(pitchPart)!;

        Assert.Equal(new Vector3(0f, 0.5f, 1.1f), yaw.LocalPivotOffset);
        Assert.Equal(new Vector3(0f, 0.25f, 0.3f), pitch.LocalPivotOffset);
        Assert.Equal(-120f, yaw.YawMin);
        Assert.Equal(120f, yaw.YawMax);
        Assert.Equal(-10f, yaw.PitchMin);
        Assert.Equal(45f, yaw.PitchMax);
        Assert.Equal(-10f, pitch.PitchMin);
        Assert.Equal(45f, pitch.PitchMax);
        Assert.True(yaw.IdleSweepEnabled);
        Assert.Equal(8f, yaw.IdleSweepSpeed);
    }

    [Fact]
    public void Pitch_part_mesh_key_resolves_to_articulated_path()
    {
        var world = new World();
        Entity hull = new ShipFactory().Create(world, MakeCorvetteDefinition());

        Entity pitchPart = FindPitchPartsForTurret(world, hull).Single();
        var render = world.GetComponent<RenderComponent>(pitchPart)!;

        Assert.Equal(
            ArticulatedShipPartMeshes.BuildPartKey("turret_pitch", "corvette_fast"),
            render.MeshKey);
        Assert.True(ArticulatedShipPartMeshes.TryBuild(render.MeshKey, Vector3.One, out float[] vertices));
        Assert.True(vertices.Length > 0);
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

    private static List<Entity> FindAllArticulatedParts(World world, Entity hull)
    {
        var matches = new List<Entity>();
        foreach (var (entity, part) in world.Query<ArticulatedPartComponent>())
        {
            if (ArticulationDrawHelper.ResolveRootOwner(world, part) == hull)
                matches.Add(entity);
        }

        return matches;
    }
}