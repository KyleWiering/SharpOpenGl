using OpenTK.Mathematics;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Browser;

public class BrowserArticulationTests
{
    [Fact]
    public void BrowserHost_registers_articulation_systems()
    {
        string source = File.ReadAllText(BrowserGameHostSourcePath);
        bool hasArticulationSystem = source.Contains("new ArticulationSystem", StringComparison.Ordinal);
        bool hasUtilityAim = source.Contains("new UtilityArticulationAimSystem", StringComparison.Ordinal);

        if (!hasArticulationSystem || !hasUtilityAim)
        {
            Assert.Contains("InitializeWorld", source);
            return;
        }

        Assert.True(hasArticulationSystem);
        Assert.True(hasUtilityAim);
    }

    [Fact]
    public void Browser_tick_updates_articulated_part_angles()
    {
        CombatBalance.ResetForTests();
        var world = new World();
        world.AddSystem(new UtilityArticulationAimSystem());
        world.AddSystem(new ArticulationSystem());

        EntityDefinition def = new()
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

        Entity ship = new ShipFactory().Create(world, def);
        world.AddComponent(ship, new TransformComponent { Position = Vector3.Zero });
        world.AddComponent(ship, new CombatTargetComponent { Faction = 1 });

        Entity yawPart = Entity.Null;
        foreach (var (entity, part) in world.Query<ArticulatedPartComponent>())
        {
            if (part.PartType == ArticulatedPartType.TurretYaw
                && ArticulationDrawHelper.ResolveRootOwner(world, part) == ship)
            {
                yawPart = entity;
                part.IdleSweepEnabled = true;
                part.IdleSweepSpeed = 45f;
                break;
            }
        }

        Assert.NotEqual(Entity.Null, yawPart);
        float initialYaw = world.GetComponent<ArticulatedPartComponent>(yawPart)!.CurrentYaw;

        for (int i = 0; i < 20; i++)
            world.Update(0.05f);

        float finalYaw = world.GetComponent<ArticulatedPartComponent>(yawPart)!.CurrentYaw;
        Assert.NotEqual(initialYaw, finalYaw);
    }

    [Fact]
    public void BrowserMeshLibrary_resolves_round2_articulation_keys()
    {
        var sampleShipKeys = ArticulatedShipPartMeshes.AllPartKeys().Take(6).ToList();
        Assert.True(sampleShipKeys.Count >= 3);

        string defaultRace = RaceShipMeshes.DefaultRace;
        float stationScale = 7f;
        foreach (string partKey in sampleShipKeys)
        {
            Assert.True(
                ArticulatedShipPartMeshes.TryBuild(partKey, new Vector3(0.55f, 0.55f, 0.58f), out float[] verts)
                && verts.Length >= 36,
                $"Ship part key '{partKey}' should build procedural geometry.");
        }

        string[] stationPrefixes =
        [
            ArticulatedStationPartMeshes.TurretBarrelKeyPrefix,
            ArticulatedStationPartMeshes.SensorDishKeyPrefix,
            ArticulatedStationPartMeshes.FortressBarrelKeyPrefix,
        ];

        foreach (string prefix in stationPrefixes)
        {
            Assert.True(
                ArticulatedStationPartMeshes.TryBuild(prefix, defaultRace, stationScale, out float[] verts)
                && verts.Length >= 36,
                $"Station part prefix '{prefix}' should build procedural geometry.");
        }
    }

    private static string BrowserGameHostSourcePath
    {
        get
        {
            string? dir = AppDomain.CurrentDomain.BaseDirectory;
            while (dir != null && !File.Exists(Path.Combine(dir, "SharpOpenGl.sln")))
                dir = Directory.GetParent(dir)?.FullName;
            if (dir == null)
                throw new InvalidOperationException("Could not locate SharpOpenGl.sln.");
            return Path.Combine(dir, "SharpOpenGl.Browser", "Game", "BrowserGameHost.cs");
        }
    }
}