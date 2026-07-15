using System.Text.Json;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.ECS;

public class ArticulationSchemaTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private const string RoundTripJson = """
        {
          "id": "test_ship",
          "displayName": "Test Ship",
          "category": "fighter",
          "articulation": {
            "parts": [
              {
                "id": "turret_yaw",
                "partType": "TurretYaw",
                "localPivot": [0, 0.4, 0.6],
                "yawMin": -120,
                "yawMax": 120,
                "idleSweep": true,
                "idleSweepSpeed": 8,
                "slewRate": 90,
                "meshKey": "articulated/turret_yaw/corvette_fast"
              },
              {
                "id": "turret_pitch",
                "partType": "TurretPitch",
                "ownerPartId": "turret_yaw",
                "localPivot": [0, 0.15, 0.2],
                "pitchMin": -10,
                "pitchMax": 45,
                "slewRate": 90,
                "meshKey": "articulated/turret_pitch/corvette_fast"
              }
            ]
          }
        }
        """;

    [Fact]
    public void Deserialize_articulation_parts_roundtrip()
    {
        var def = JsonSerializer.Deserialize<EntityDefinition>(RoundTripJson, Options);
        Assert.NotNull(def);
        Assert.NotNull(def!.Articulation);
        Assert.Equal(2, def.Articulation!.Parts.Length);

        ArticulationPartDefinition yaw = def.Articulation.Parts[0];
        Assert.Equal("turret_yaw", yaw.Id);
        Assert.Equal("TurretYaw", yaw.PartType);
        Assert.Equal(3, yaw.LocalPivot!.Length);
        Assert.Equal(0f, yaw.LocalPivot[0]);
        Assert.Equal(0.4f, yaw.LocalPivot[1]);
        Assert.Equal(0.6f, yaw.LocalPivot[2]);
        Assert.Equal(-120f, yaw.YawMin);
        Assert.Equal(120f, yaw.YawMax);
        Assert.True(yaw.IdleSweep);
        Assert.Equal(8f, yaw.IdleSweepSpeed);
        Assert.Equal(90f, yaw.SlewRate);
        Assert.Equal("articulated/turret_yaw/corvette_fast", yaw.MeshKey);

        ArticulationPartDefinition pitch = def.Articulation.Parts[1];
        Assert.Equal("turret_pitch", pitch.Id);
        Assert.Equal("TurretPitch", pitch.PartType);
        Assert.Equal("turret_yaw", pitch.OwnerPartId);
        Assert.Equal(3, pitch.LocalPivot!.Length);
        Assert.Equal(0f, pitch.LocalPivot[0]);
        Assert.Equal(0.15f, pitch.LocalPivot[1]);
        Assert.Equal(0.2f, pitch.LocalPivot[2]);
        Assert.Equal(-10f, pitch.PitchMin);
        Assert.Equal(45f, pitch.PitchMax);
        Assert.Equal(90f, pitch.SlewRate);
        Assert.Equal("articulated/turret_pitch/corvette_fast", pitch.MeshKey);
    }

    [Fact]
    public void Deserialize_articulation_optional_fields_omitted()
    {
        const string json = """
            {
              "id": "minimal",
              "articulation": {
                "parts": [
                  { "id": "arm", "partType": "MiningArm", "localPivot": [0, 1, 0] }
                ]
              }
            }
            """;

        var def = JsonSerializer.Deserialize<EntityDefinition>(json, Options);
        Assert.NotNull(def);

        ArticulationPartDefinition part = def!.Articulation!.Parts.Single();
        Assert.Equal("arm", part.Id);
        Assert.Equal("MiningArm", part.PartType);
        Assert.Equal(3, part.LocalPivot!.Length);
        Assert.Equal(0f, part.LocalPivot[0]);
        Assert.Equal(1f, part.LocalPivot[1]);
        Assert.Equal(0f, part.LocalPivot[2]);
        Assert.Null(part.MeshOffset);
        Assert.Null(part.MeshKey);
        Assert.Null(part.OwnerPartId);
        Assert.Null(part.IdleSweep);
        Assert.Null(part.IdleSweepSpeed);
        Assert.Null(part.SlewRate);
    }

    [Fact]
    public void Deserialize_entity_without_articulation_is_null()
    {
        string path = Path.Combine(GetGameDataPath(), "Ships", "transport_cargo.json");
        string json = File.ReadAllText(path);

        var def = JsonSerializer.Deserialize<EntityDefinition>(json, Options);
        Assert.NotNull(def);
        Assert.Null(def!.Articulation);
    }

    [Fact]
    public void SpawnFromDefinition_creates_root_yaw_and_nested_pitch()
    {
        var world = new World();
        Entity hull = world.CreateEntity();

        var definition = new ArticulationDefinition
        {
            Parts =
            [
                new ArticulationPartDefinition
                {
                    Id = "turret_yaw",
                    PartType = "TurretYaw",
                    LocalPivot = [0f, 0.4f, 0.6f],
                    MeshKey = "articulated/turret_yaw/corvette_fast",
                },
                new ArticulationPartDefinition
                {
                    Id = "turret_pitch",
                    PartType = "TurretPitch",
                    OwnerPartId = "turret_yaw",
                    LocalPivot = [0f, 0.15f, 0.2f],
                    MeshKey = "articulated/turret_pitch/corvette_fast",
                },
            ],
        };

        int created = ArticulationSpawner.SpawnFromDefinition(world, hull, definition, "corvette_fast");
        Assert.Equal(2, created);

        Entity yawPart = FindPartsOwnedBy(world, hull, ArticulatedPartType.TurretYaw).Single();
        Entity pitchPart = FindPitchPartsForTurret(world, hull).Single();

        var pitch = world.GetComponent<ArticulatedPartComponent>(pitchPart)!;
        Assert.Equal(yawPart, pitch.Owner);
        Assert.NotEqual(hull, pitch.Owner);
    }

    [Fact]
    public void SpawnFromDefinition_adds_render_only_when_mesh_key_present()
    {
        var world = new World();
        Entity owner = world.CreateEntity();

        var definition = new ArticulationDefinition
        {
            Parts =
            [
                new ArticulationPartDefinition
                {
                    Id = "turret_yaw",
                    PartType = "TurretYaw",
                    LocalPivot = [0f, 2.94f, 0f],
                },
                new ArticulationPartDefinition
                {
                    Id = "turret_pitch",
                    PartType = "TurretPitch",
                    OwnerPartId = "turret_yaw",
                    LocalPivot = [0f, 0.7f, 2.94f],
                    MeshKey = ArticulatedStationPartMeshes.TurretBarrelKeyPrefix,
                },
            ],
        };

        ArticulationSpawner.SpawnFromDefinition(world, owner, definition);

        Entity yawPart = FindPartsOwnedBy(world, owner, ArticulatedPartType.TurretYaw).Single();
        Entity pitchPart = FindPitchPartsForTurret(world, owner).Single();

        Assert.Null(world.GetComponent<RenderComponent>(yawPart));
        Assert.NotNull(world.GetComponent<RenderComponent>(pitchPart));
    }

    [Fact]
    public void SpawnFromDefinition_second_call_is_idempotent()
    {
        var world = new World();
        Entity hull = world.CreateEntity();
        var definition = ShipTurretArticulationDefs.TryToArticulationDefinition("corvette_fast")!;

        int first = ArticulationSpawner.SpawnFromDefinition(world, hull, definition, "corvette_fast");
        int second = ArticulationSpawner.SpawnFromDefinition(world, hull, definition, "corvette_fast");

        Assert.Equal(2, first);
        Assert.Equal(0, second);
        Assert.Equal(2, CountPartsForRoot(world, hull));
    }

    [Fact]
    public void SpawnFromDefinition_skips_unknown_part_type()
    {
        var world = new World();
        Entity owner = world.CreateEntity();

        var definition = new ArticulationDefinition
        {
            Parts =
            [
                new ArticulationPartDefinition
                {
                    Id = "bad",
                    PartType = "InvalidPart",
                    LocalPivot = [0f, 1f, 0f],
                },
                new ArticulationPartDefinition
                {
                    Id = "good",
                    PartType = "SensorDish",
                    LocalPivot = [0f, 4.06f, 0.56f],
                    MeshKey = ArticulatedStationPartMeshes.SensorDishKeyPrefix,
                },
            ],
        };

        int created = ArticulationSpawner.SpawnFromDefinition(world, owner, definition);
        Assert.Equal(1, created);
        Assert.Single(FindPartsOwnedBy(world, owner, ArticulatedPartType.SensorDish));
    }

    [Fact]
    public void SpawnFromDefinition_invalid_owner_part_id_destroys_or_skips_orphan()
    {
        var world = new World();
        Entity owner = world.CreateEntity();

        var definition = new ArticulationDefinition
        {
            Parts =
            [
                new ArticulationPartDefinition
                {
                    Id = "orphan_pitch",
                    PartType = "TurretPitch",
                    OwnerPartId = "missing_yaw",
                    LocalPivot = [0f, 0.15f, 0.2f],
                    MeshKey = "articulated/turret_pitch/corvette_fast",
                },
            ],
        };

        int created = ArticulationSpawner.SpawnFromDefinition(world, owner, definition, "corvette_fast");
        Assert.Equal(0, created);
        Assert.Equal(0, CountPartsForRoot(world, owner));
    }

    [Fact]
    public void SpawnFromDefinition_maps_slew_and_idle_fields()
    {
        var world = new World();
        Entity hull = world.CreateEntity();

        var definition = new ArticulationDefinition
        {
            Parts =
            [
                new ArticulationPartDefinition
                {
                    Id = "turret_yaw",
                    PartType = "TurretYaw",
                    LocalPivot = [0f, 0.4f, 0.6f],
                    IdleSweep = true,
                    IdleSweepSpeed = 12f,
                    SlewRate = 75f,
                    MeshKey = "articulated/turret_yaw/corvette_fast",
                },
            ],
        };

        ArticulationSpawner.SpawnFromDefinition(world, hull, definition, "corvette_fast");

        Entity yawPart = FindPartsOwnedBy(world, hull, ArticulatedPartType.TurretYaw).Single();
        var yaw = world.GetComponent<ArticulatedPartComponent>(yawPart)!;

        Assert.True(yaw.IdleSweepEnabled);
        Assert.Equal(12f, yaw.IdleSweepSpeed);
        Assert.Equal(75f, yaw.SlewRateDegreesPerSecond);
    }

    [Fact]
    public void ApplyShipTurretArticulation_uses_json_when_articulation_present()
    {
        var world = new World();
        Entity hull = world.CreateEntity();
        var def = new EntityDefinition
        {
            Id = "corvette_fast",
            Category = "corvette",
            Components = new ComponentsDefinition
            {
                Weapons = [new WeaponDefinition { Slot = 0, Type = "laser", Damage = 10, Range = 100, FireRate = 1f }],
            },
            Articulation = ShipTurretArticulationDefs.TryToArticulationDefinition("corvette_fast"),
        };

        FactoryHelpers.ApplyShipTurretArticulation(world, hull, def);

        Assert.Equal(2, CountPartsForRoot(world, hull));
        Assert.Single(FindPartsOwnedBy(world, hull, ArticulatedPartType.TurretYaw));
        Assert.Single(FindPitchPartsForTurret(world, hull));
    }

    [Fact]
    public void ApplyShipTurretArticulation_falls_back_when_articulation_absent()
    {
        var world = new World();
        Entity hull = world.CreateEntity();
        var def = new EntityDefinition
        {
            Id = "corvette_fast",
            Category = "corvette",
            Components = new ComponentsDefinition
            {
                Weapons = [new WeaponDefinition { Slot = 0, Type = "laser", Damage = 10, Range = 100, FireRate = 1f }],
            },
        };

        FactoryHelpers.ApplyShipTurretArticulation(world, hull, def);

        Assert.Equal(2, CountPartsForRoot(world, hull));
        Entity yawPart = FindPartsOwnedBy(world, hull, ArticulatedPartType.TurretYaw).Single();
        var yaw = world.GetComponent<ArticulatedPartComponent>(yawPart)!;
        Assert.Equal(new Vector3(0f, 0.4f, 0.6f), yaw.LocalPivotOffset);
    }

    [Fact]
    public void BaseFactory_defense_turret_json_matches_static_part_count()
    {
        string path = Path.Combine(GetGameDataPath(), "Bases", "defense_turret.json");
        var def = JsonSerializer.Deserialize<EntityDefinition>(File.ReadAllText(path), Options)!;

        var world = new World();
        Entity owner = new BaseFactory().Create(world, def);

        Assert.Equal(2, CountPartsForRoot(world, owner));
        Assert.Single(FindPartsOwnedBy(world, owner, ArticulatedPartType.TurretYaw));
        Assert.Single(FindPitchPartsForTurret(world, owner));
    }

    [Fact]
    public void BaseFactory_sensor_array_json_dish_has_mesh_offset()
    {
        string path = Path.Combine(GetGameDataPath(), "Bases", "sensor_array.json");
        var def = JsonSerializer.Deserialize<EntityDefinition>(File.ReadAllText(path), Options)!;

        var world = new World();
        Entity owner = new BaseFactory().Create(world, def);

        Entity dishPart = FindPartsOwnedBy(world, owner, ArticulatedPartType.SensorDish).Single();
        var dish = world.GetComponent<ArticulatedPartComponent>(dishPart)!;

        Assert.Equal(new Vector3(0f, -4.06f, -0.56f), dish.MeshLocalOffset);
    }

    [Fact]
    public void Migrated_corvette_json_equivalent_to_static_table()
    {
        string path = Path.Combine(GetGameDataPath(), "Ships", "corvette_fast.json");
        var def = JsonSerializer.Deserialize<EntityDefinition>(File.ReadAllText(path), Options)!;
        Assert.True(ShipTurretArticulationDefs.TryGet("corvette_fast", out ShipTurretDef staticDef));

        var world = new World();
        Entity hull = new ShipFactory().Create(world, def);

        Entity yawPart = FindPartsOwnedBy(world, hull, ArticulatedPartType.TurretYaw).Single();
        Entity pitchPart = FindPitchPartsForTurret(world, hull).Single();
        var yaw = world.GetComponent<ArticulatedPartComponent>(yawPart)!;
        var pitch = world.GetComponent<ArticulatedPartComponent>(pitchPart)!;

        Assert.Equal(staticDef.HullPivotOffset, yaw.LocalPivotOffset);
        Assert.Equal(staticDef.PitchPivotOnYaw, pitch.LocalPivotOffset);
        Assert.Equal(staticDef.YawMin, yaw.YawMin);
        Assert.Equal(staticDef.YawMax, yaw.YawMax);
        Assert.Equal(staticDef.PitchMin, pitch.PitchMin);
        Assert.Equal(staticDef.PitchMax, pitch.PitchMax);
    }

    [Fact]
    public void Invalid_articulation_graceful_skip()
    {
        var world = new World();
        Entity hull = world.CreateEntity();
        var def = new EntityDefinition
        {
            Id = "corvette_fast",
            Category = "corvette",
            Components = new ComponentsDefinition
            {
                Weapons = [new WeaponDefinition { Slot = 0, Type = "laser", Damage = 10, Range = 100, FireRate = 1f }],
            },
            Articulation = new ArticulationDefinition { Parts = [] },
        };

        FactoryHelpers.ApplyShipTurretArticulation(world, hull, def);

        Assert.Equal(0, CountPartsForRoot(world, hull));

        def.Articulation = new ArticulationDefinition
        {
            Parts =
            [
                new ArticulationPartDefinition { Id = "dup", PartType = "TurretYaw", LocalPivot = [0f, 1f, 0f] },
                new ArticulationPartDefinition { Id = "dup", PartType = "TurretPitch", LocalPivot = [0f, 1f, 0f] },
            ],
        };

        FactoryHelpers.ApplyShipTurretArticulation(world, hull, def);
        Assert.Equal(0, CountPartsForRoot(world, hull));
    }

    [Fact]
    public void Skirmish_wrapper_passes_def_when_available()
    {
        string path = Path.Combine(GetGameDataPath(), "Bases", "defense_turret.json");
        var def = JsonSerializer.Deserialize<EntityDefinition>(File.ReadAllText(path), Options)!;

        var world = new World();
        Entity building = world.CreateEntity();
        world.AddComponent(building, new BuildingComponent
        {
            BuildingType = "defense_turret",
            Footprint = [1, 1],
        });

        BaseFactory.TrySpawnArticulation(world, building, "defense_turret", def);

        Assert.Equal(2, CountPartsForRoot(world, building));
    }

    [Theory]
    [InlineData("turretyaw")]
    [InlineData("TurretYaw")]
    public void TryParsePartType_accepts_case_insensitive_enum(string raw)
    {
        Assert.True(ArticulationDefinitionParser.TryParsePartType(raw, out var type));
        Assert.Equal(ArticulatedPartType.TurretYaw, type);
    }

    [Fact]
    public void TryParsePartType_unknown_returns_false()
    {
        Assert.False(ArticulationDefinitionParser.TryParsePartType("InvalidPart", out _));
    }

    [Fact]
    public void IsValid_rejects_duplicate_part_ids()
    {
        var def = new ArticulationDefinition
        {
            Parts =
            [
                new ArticulationPartDefinition { Id = "dup", PartType = "TurretYaw" },
                new ArticulationPartDefinition { Id = "dup", PartType = "TurretPitch" },
            ],
        };

        Assert.False(ArticulationDefinitionParser.IsValid(def));
    }

    [Theory]
    [InlineData(new float[] { 1f, 2f }, null)]
    [InlineData(new float[] { 1f, 2f, 3f }, "1,2,3")]
    public void TryParseVec3_requires_three_elements(float[] values, string? expected)
    {
        Vector3? parsed = ArticulationDefinitionParser.TryParseVec3(values);

        if (expected is null)
        {
            Assert.Null(parsed);
            return;
        }

        Assert.NotNull(parsed);
        Assert.Equal(new Vector3(1f, 2f, 3f), parsed!.Value);
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

    private static int CountPartsForRoot(World world, Entity root)
    {
        int count = 0;
        foreach (var (_, part) in world.Query<ArticulatedPartComponent>())
        {
            if (ArticulationDrawHelper.ResolveRootOwner(world, part) == root)
                count++;
        }

        return count;
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