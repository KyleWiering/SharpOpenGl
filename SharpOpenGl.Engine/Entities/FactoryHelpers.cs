using OpenTK.Mathematics;
using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.Combat;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Engine.Entities;

/// <summary>
/// Internal helpers shared by all entity factories.
/// Translates <see cref="EntityDefinition"/> component blocks into ECS components.
/// </summary>
internal static class FactoryHelpers
{
    /// <summary>
    /// Resolve the mesh key to use: prefer <paramref name="preferred"/>,
    /// fall back to <paramref name="fallback"/> or <paramref name="defaultMeshKey"/> when missing.
    /// </summary>
    internal static string ResolveMesh(
        AssetManager? assets, string preferred, string fallback, string defaultMeshKey)
    {
        if (assets != null && !string.IsNullOrEmpty(preferred) && assets.MeshExists(preferred))
            return preferred;

        string resolved = ResolveFallbackMesh(assets, fallback, defaultMeshKey);
        if (assets != null
            && !string.IsNullOrEmpty(preferred)
            && !string.Equals(preferred, resolved, StringComparison.OrdinalIgnoreCase)
            && !assets.MeshExists(resolved))
        {
            Console.WriteLine(
                $"[Factory] Mesh '{preferred}' not found, no fallback available.");
        }

        return resolved;
    }

    private static string ResolveFallbackMesh(
        AssetManager? assets, string fallback, string defaultMeshKey)
    {
        if (assets == null)
            return PickFallbackKey(fallback, defaultMeshKey);

        if (!string.IsNullOrEmpty(fallback)
            && !string.Equals(fallback, "default", StringComparison.OrdinalIgnoreCase)
            && assets.MeshExists(fallback))
            return fallback;

        if (assets.MeshExists(defaultMeshKey))
            return defaultMeshKey;

        return PickFallbackKey(fallback, defaultMeshKey);
    }

    private static string PickFallbackKey(string fallback, string defaultMeshKey) =>
        string.IsNullOrEmpty(fallback) || string.Equals(fallback, "default", StringComparison.OrdinalIgnoreCase)
            ? defaultMeshKey
            : fallback;

    /// <summary>Apply <see cref="HealthComponent"/> from definition.</summary>
    internal static void ApplyHealth(World world, Entity entity, HealthDefinition? def)
    {
        if (def == null) return;
        world.AddComponent(entity, new HealthComponent
        {
            MaxHP         = def.MaxHP,
            CurrentHP     = def.MaxHP,
            MaxShields    = def.Shields,
            CurrentShields = def.Shields,
            Armor         = def.Armor,
        });
    }

    /// <summary>Apply <see cref="MovementComponent"/> from definition.</summary>
    internal static void ApplyMovement(World world, Entity entity, MovementDefinition? def)
    {
        if (def == null) return;
        world.AddComponent(entity, new MovementComponent
        {
            Speed        = def.Speed,
            Acceleration = def.Acceleration,
            TurnRate     = def.TurnRate,
        });
    }

    /// <summary>
    /// Apply one <see cref="WeaponComponent"/> per weapon entry.
    /// Each weapon lives in a keyed component where the slot is encoded.
    /// Because the ECS uses one pool per type, multiple weapons on the same
    /// entity are stored as a <see cref="WeaponListComponent"/> wrapper.
    /// </summary>
    internal static void ApplyWeapons(World world, Entity entity, WeaponDefinition[]? defs)
    {
        if (defs == null || defs.Length == 0) return;

        var list = new WeaponListComponent();
        foreach (var wd in defs)
        {
            string projectileType = string.IsNullOrWhiteSpace(wd.ProjectileType)
                || string.Equals(wd.ProjectileType, "default", StringComparison.OrdinalIgnoreCase)
                ? WeaponProfiles.DefaultProjectileTypeKey(wd.Type)
                : wd.ProjectileType;

            list.Weapons.Add(new WeaponComponent
            {
                Slot           = wd.Slot,
                Type           = wd.Type,
                Damage         = wd.Damage,
                Range          = CombatBalance.ScaleRange(wd.Range),
                FireRate        = wd.FireRate,
                ProjectileType = projectileType,
            });
        }
        world.AddComponent(entity, list);
    }

    /// <summary>
    /// Spawns nested yaw + pitch articulated child entities for armed corvette/gunship/destroyer hulls.
    /// </summary>
    internal static void ApplyShipTurretArticulation(World world, Entity hull, EntityDefinition def)
    {
        if (def.Components?.Weapons == null || def.Components.Weapons.Length == 0)
            return;

        string hullKey = ShipTurretArticulationDefs.ResolveHullKey(def);

        if (def.Articulation != null)
        {
            if (TryFilterTurretArticulation(def.Articulation, out ArticulationDefinition? turretArticulation))
                ArticulationSpawner.SpawnFromDefinition(world, hull, turretArticulation, hullKey);
            return;
        }

        if (!ShipTurretArticulationDefs.TryGet(hullKey, out ShipTurretDef turretDef))
            return;

        Entity yawEntity = world.CreateEntity();
        world.AddComponent(yawEntity, new ArticulatedPartComponent
        {
            Owner = hull,
            PartType = ArticulatedPartType.TurretYaw,
            LocalPivotOffset = turretDef.HullPivotOffset,
            MeshLocalOffset = turretDef.YawMeshOffset,
            YawMin = turretDef.YawMin,
            YawMax = turretDef.YawMax,
            PitchMin = turretDef.PitchMin,
            PitchMax = turretDef.PitchMax,
            IdleSweepEnabled = true,
            IdleSweepSpeed = 8f,
            SlewRateDegreesPerSecond = 90f,
        });
        world.AddComponent(yawEntity, new RenderComponent
        {
            MeshKey = ArticulatedShipPartMeshes.BuildPartKey("turret_yaw", hullKey),
            MeshId = -1,
            Visible = true,
            PrimitiveType = 4,
        });

        Entity pitchEntity = world.CreateEntity();
        world.AddComponent(pitchEntity, new ArticulatedPartComponent
        {
            Owner = yawEntity,
            PartType = ArticulatedPartType.TurretPitch,
            LocalPivotOffset = turretDef.PitchPivotOnYaw,
            MeshLocalOffset = turretDef.PitchMeshOffset,
            YawMin = turretDef.YawMin,
            YawMax = turretDef.YawMax,
            PitchMin = turretDef.PitchMin,
            PitchMax = turretDef.PitchMax,
            IdleSweepEnabled = false,
            SlewRateDegreesPerSecond = 90f,
        });
        world.AddComponent(pitchEntity, new RenderComponent
        {
            MeshKey = ArticulatedShipPartMeshes.BuildPartKey("turret_pitch", hullKey),
            MeshId = -1,
            Visible = true,
            PrimitiveType = 4,
        });
    }

    private static bool TryFilterTurretArticulation(
        ArticulationDefinition? articulation,
        out ArticulationDefinition? turretArticulation)
    {
        turretArticulation = null;
        if (articulation?.Parts is not { Length: > 0 } parts)
            return false;

        var turretParts = new List<ArticulationPartDefinition>();
        foreach (ArticulationPartDefinition part in parts)
        {
            if (!ArticulationDefinitionParser.TryParsePartType(part.PartType, out ArticulatedPartType partType))
                continue;

            if (partType is not (ArticulatedPartType.TurretYaw or ArticulatedPartType.TurretPitch))
                continue;

            // launcher_pod is a special-hull part typed as TurretYaw in JSON for schema compat.
            if (string.Equals(part.Id, "launcher_pod", StringComparison.OrdinalIgnoreCase))
                continue;

            turretParts.Add(part);
        }

        if (turretParts.Count == 0)
            return false;

        turretArticulation = new ArticulationDefinition { Parts = turretParts.ToArray() };
        return ArticulationDefinitionParser.IsValid(turretArticulation);
    }

    /// <summary>Apply <see cref="HeroComponent"/> from definition.</summary>
    internal static void ApplyHero(World world, Entity entity,
        HeroDefinition? hero, AbilityDefinition[]? abilities)
    {
        if (hero == null) return;

        var comp = new HeroComponent
        {
            Level          = hero.Level,
            XP             = hero.XP,
            UpgradeTreeKey = hero.UpgradeTree,
        };

        if (abilities != null)
        {
            foreach (var ab in abilities)
            {
                comp.AbilitySlots[ab.Slot]     = ab.Id;
                comp.AbilityCooldowns[ab.Slot] = 0f;
            }

            ApplyAbilityList(world, entity, abilities);
        }

        world.AddComponent(entity, comp);
    }

    /// <summary>Attach <see cref="AbilityListComponent"/> from JSON ability entries.</summary>
    internal static void ApplyAbilityList(World world, Entity entity, AbilityDefinition[] abilities)
    {
        if (abilities.Length == 0) return;

        var list = new AbilityListComponent();
        foreach (var ab in abilities)
        {
            list.Abilities.Add(new AbilityComponent
            {
                Slot = ab.Slot,
                Id = ab.Id,
                MaxCooldown = ab.Cooldown,
                CurrentCooldown = 0f,
            });
        }

        world.AddComponent(entity, list);
    }

    /// <summary>Apply <see cref="SquadMemberComponent"/> from definition.</summary>
    internal static void ApplySquadMember(World world, Entity entity, SquadMemberDefinition? def)
    {
        if (def == null) return;

        var offset = Vector3.Zero;
        if (def.FormationOffset is { Length: >= 3 } fo)
            offset = new Vector3(fo[0], fo[1], fo[2]);

        world.AddComponent(entity, new SquadMemberComponent
        {
            FormationSlot   = def.FormationSlot,
            FormationOffset = offset,
        });
    }

    /// <summary>Apply <see cref="BuildingComponent"/> from definition.</summary>
    internal static void ApplyBuilding(World world, Entity entity, BuildingDefinition? def)
    {
        if (def == null) return;

        var comp = new BuildingComponent
        {
            BuildingType   = def.BuildingType,
            ProductionRate = def.ProductionRate,
            Footprint      = def.Footprint is { Length: >= 2 } ? def.Footprint : [1, 1],
            Rotates        = def.Rotates,
        };

        foreach (string item in def.BuildQueue)
            comp.BuildQueue.Enqueue(item);

        world.AddComponent(entity, comp);
    }

    /// <summary>Apply <see cref="SightRadiusComponent"/> from definition.</summary>
    internal static void ApplySightRadius(World world, Entity entity, ComponentsDefinition? components)
    {
        int radius = components?.SightRadius > 0 ? components.SightRadius : 5;
        world.AddComponent(entity, new SightRadiusComponent { Radius = radius });
    }

    /// <summary>Apply <see cref="ResourceCollectorComponent"/> from definition.</summary>
    internal static void ApplyResourceCollector(World world, Entity entity, ResourceCollectorDefinition? def)
    {
        if (def == null) return;

        var mode = HarvestModeDefaults.Parse(def.HarvestMode);
        float range = def.HarvestRange > 0f
            ? def.HarvestRange
            : HarvestModeDefaults.DefaultRange(mode);
        float rateMult = def.HarvestRateMultiplier > 0f
            ? def.HarvestRateMultiplier
            : HarvestModeDefaults.DefaultRateMultiplier(mode);
        float capacity = def.CarryCapacity > 0f
            ? HarvestModeDefaults.DefaultCarryCapacity(mode, def.CarryCapacity)
            : HarvestModeDefaults.DefaultCarryCapacity(mode, 50f);

        world.AddComponent(entity, new ResourceCollectorComponent
        {
            HarvestMode = mode,
            HarvestRange = range,
            HarvestRate = def.HarvestRate * rateMult,
            CarryCapacity = capacity,
        });
    }

    /// <summary>Apply <see cref="ShipRepairComponent"/> from definition.</summary>
    internal static void ApplyShipRepair(World world, Entity entity, ShipRepairDefinition? def)
    {
        if (def == null) return;

        float range = def.RepairRange > 0f
            ? def.RepairRange
            : 60f;

        world.AddComponent(entity, new ShipRepairComponent
        {
            RepairRange = range,
            RepairRate = def.RepairRate > 0f ? def.RepairRate : 12f,
            RepairableCategories = def.RepairableCategories?.ToArray() ?? [],
        });
    }

    /// <summary>Apply <see cref="StructureBuilderComponent"/> from definition.</summary>
    internal static void ApplyStructureBuilder(World world, Entity entity, StructureBuilderDefinition? def)
    {
        if (def == null) return;

        float range = def.PlacementRange > 0f
            ? def.PlacementRange
            : StructureBuilderComponent.DefaultPlacementRange;

        world.AddComponent(entity, new StructureBuilderComponent
        {
            PlacementRange = range,
            BuildableIds = def.BuildableIds?.ToList() ?? [],
        });
    }
}