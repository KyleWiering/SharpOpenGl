using OpenTK.Mathematics;
using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.Combat;
using SharpOpenGl.Engine.ECS;

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
                Range          = wd.Range,
                FireRate        = wd.FireRate,
                ProjectileType = projectileType,
            });
        }
        world.AddComponent(entity, list);
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
        }

        world.AddComponent(entity, comp);
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

        world.AddComponent(entity, new ResourceCollectorComponent
        {
            CarryCapacity = def.CarryCapacity,
        });
    }
}