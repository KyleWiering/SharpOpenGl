using System.Text.Json;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.ECS;

public class FleetGalleryArticulationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    [Fact]
    public void GallerySpawn_all_sixteen_bases_have_articulated_children()
    {
        foreach (string baseId in FleetGalleryLayout.AllBaseIds)
        {
            var world = new World();
            EntityDefinition def = LoadBaseDefinition(baseId);
            Entity owner = new BaseFactory().Create(world, def);
            BaseFactory.TrySpawnArticulation(world, owner, baseId, def);

            int partCount = CountArticulatedPartsForOwner(world, owner);
            Assert.True(partCount >= 1, $"Expected articulated children for gallery base '{baseId}'.");
        }
    }

    [Theory]
    [MemberData(nameof(ArmedHullIds))]
    public void GallerySpawn_armed_hulls_spawn_expected_part_counts(string hullId)
    {
        EntityDefinition def = LoadShipDefinition(hullId);
        var world = new World();
        Entity ship = new ShipFactory().Create(world, def);

        int count = CountArticulatedPartsForOwner(world, ship);
        Assert.Equal(ShipTurretArticulationTests.ExpectedArticulatedPartCounts[hullId], count);
    }

    [Theory]
    [InlineData("bomber_heavy")]
    [InlineData("carrier_command")]
    [InlineData("scout_light")]
    [InlineData("drone_swarm")]
    public void GallerySpawn_special_hulls_attach_bay_dish_launcher_parts(string hullId)
    {
        EntityDefinition def = LoadShipDefinition(hullId);
        var world = new World();
        Entity ship = new ShipFactory().Create(world, def);
        SpecialHullArticulationSpawner.AttachSpecialHullParts(world, ship, def, "terran");

        int specialPartCount = 0;
        foreach (var (_, part) in world.Query<ArticulatedPartComponent>())
        {
            if (ArticulationDrawHelper.ResolveRootOwner(world, part) != ship)
                continue;

            if (part.PartType is ArticulatedPartType.BayDoor
                or ArticulatedPartType.SensorDish
                or ArticulatedPartType.LauncherPod
                or ArticulatedPartType.WingFlap)
                specialPartCount++;
        }

        Assert.True(specialPartCount >= 1, $"Expected special articulation part on '{hullId}'.");
    }

    [Fact]
    public void GallerySpawn_tick_articulation_system_angle_delta()
    {
        CombatBalance.ResetForTests();
        var world = new World();
        var articulationSystem = new ArticulationSystem();

        Entity ship = new ShipFactory().Create(world, LoadShipDefinition("corvette_fast"));
        world.AddComponent(ship, new TransformComponent { Position = new Vector3(-20f, 0f, 0f) });
        world.AddComponent(ship, new CombatTargetComponent
        {
            Faction = 1,
            TargetingMode = TargetPriority.Closest,
        });

        Entity sensor = new BaseFactory().Create(world, LoadBaseDefinition("sensor_array"));
        world.AddComponent(sensor, new TransformComponent { Position = new Vector3(0f, 0f, 0f) });

        Entity turret = new BaseFactory().Create(world, LoadBaseDefinition("defense_turret"));
        world.AddComponent(turret, new TransformComponent { Position = new Vector3(20f, 0f, 0f) });

        Entity target = world.CreateEntity();
        world.AddComponent(target, new TransformComponent { Position = new Vector3(30f, 5f, -10f) });
        world.AddComponent(target, new HealthComponent { MaxHP = 100f, CurrentHP = 100f });
        world.AddComponent(target, new CombatTargetComponent { Faction = 2 });

        world.GetComponent<CombatTargetComponent>(ship)!.CurrentTarget = target;
        world.GetComponent<CombatTargetComponent>(turret)!.CurrentTarget = target;

        var initialAngles = SnapshotArticulatedAngles(world);
        for (int i = 0; i < 32; i++)
            articulationSystem.Update(world, 0.016f);
        var finalAngles = SnapshotArticulatedAngles(world);

        Assert.True(initialAngles.Count > 0);
        Assert.True(AnyAngleChanged(initialAngles, finalAngles));
    }

    [Fact]
    public void GallerySpawn_mock_target_enables_combat_aim()
    {
        CombatBalance.ResetForTests();
        EntityDefinition def = LoadShipDefinition("gunship_heavy");
        var world = new World();
        var articulationSystem = new ArticulationSystem();

        Entity ship = new ShipFactory().Create(world, def);
        world.AddComponent(ship, new TransformComponent { Position = Vector3.Zero });
        world.AddComponent(ship, new CombatTargetComponent
        {
            Faction = 1,
            TargetingMode = TargetPriority.Closest,
        });

        Entity mockTarget = world.CreateEntity();
        world.AddComponent(mockTarget, new TransformComponent { Position = new Vector3(50f, 0.4f, 0.6f) });
        world.AddComponent(mockTarget, new HealthComponent { MaxHP = 100f, CurrentHP = 100f });
        world.AddComponent(mockTarget, new CombatTargetComponent { Faction = 99 });
        world.AddComponent(mockTarget, new EntityNameComponent { DefinitionId = "gallery_mock_target" });

        world.GetComponent<CombatTargetComponent>(ship)!.CurrentTarget = mockTarget;

        articulationSystem.Update(world, 0.016f);

        bool anyAim = false;
        foreach (var (_, part) in world.Query<ArticulatedPartComponent>())
        {
            if (ArticulationDrawHelper.ResolveRootOwner(world, part) != ship)
                continue;

            if (part.PartType is ArticulatedPartType.TurretYaw or ArticulatedPartType.TurretPitch
                && (part.HasAimTarget || MathF.Abs(part.TargetYaw) > 1f))
            {
                anyAim = true;
                break;
            }
        }

        Assert.True(anyAim);
    }

    public static IEnumerable<object[]> ArmedHullIds =>
        ShipTurretArticulationTests.ArmedHullIds;

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

    private static Dictionary<Entity, (float yaw, float pitch)> SnapshotArticulatedAngles(World world)
    {
        var snapshot = new Dictionary<Entity, (float yaw, float pitch)>();
        foreach (var (entity, part) in world.Query<ArticulatedPartComponent>())
            snapshot[entity] = (part.CurrentYaw, part.CurrentPitch);
        return snapshot;
    }

    private static bool AnyAngleChanged(
        Dictionary<Entity, (float yaw, float pitch)> before,
        Dictionary<Entity, (float yaw, float pitch)> after)
    {
        foreach (var (entity, initial) in before)
        {
            if (!after.TryGetValue(entity, out var final))
                continue;

            if (MathF.Abs(final.yaw - initial.yaw) > 1e-3f
                || MathF.Abs(final.pitch - initial.pitch) > 1e-3f)
                return true;
        }

        return false;
    }

    private static EntityDefinition LoadBaseDefinition(string baseId)
    {
        string path = Path.Combine(GetGameDataPath(), "Bases", $"{baseId}.json");
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<EntityDefinition>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to load base '{baseId}'.");
    }

    private static EntityDefinition LoadShipDefinition(string hullId)
    {
        string path = Path.Combine(GetGameDataPath(), "Ships", $"{hullId}.json");
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<EntityDefinition>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to load ship '{hullId}'.");
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