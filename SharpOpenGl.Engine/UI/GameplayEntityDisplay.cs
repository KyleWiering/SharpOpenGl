using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;

namespace SharpOpenGl.Engine.UI;

/// <summary>Visual classification for HUD labels and selection rings.</summary>
public enum EntityDisplayKind
{
    Friendly,
    Hostile,
    Neutral,
    Harvestable,
    Scenery,
}

/// <summary>Shared gameplay entity colors for HUD and world markers.</summary>
public static class GameplayEntityDisplay
{
    public static readonly Vector4 FriendlyColor = new(0.35f, 0.9f, 0.45f, 1f);
    public static readonly Vector4 HostileColor = new(0.95f, 0.25f, 0.25f, 1f);
    public static readonly Vector4 NeutralColor = new(0.95f, 0.85f, 0.2f, 1f);
    public static readonly Vector4 HarvestableColor = new(0.55f, 0.85f, 1f, 1f);
    public static readonly Vector4 SceneryColor = new(1f, 1f, 1f, 1f);
    public static readonly Vector4 SelectionFriendly = new(0f, 1f, 0f, 1f);
    public static readonly Vector4 SelectionHostile = HostileColor;
    public static readonly Vector4 SelectionNeutral = NeutralColor;
    public static readonly Vector4 SelectionHarvestable = HarvestableColor;
    public static readonly Vector4 SelectionScenery = SceneryColor;
    public static readonly Vector4 AttackHoverRingColor = new(1f, 0.4f, 0.15f, 1f);

    public static EntityDisplayKind Classify(World world, Entity entity)
    {
        if (world.HasComponent<ResourceNodeComponent>(entity))
            return EntityDisplayKind.Harvestable;

        var mapFeature = world.GetComponent<MapFeatureComponent>(entity);
        if (mapFeature != null)
        {
            return mapFeature.Kind switch
            {
                MapFeatureKind.NeutralPlanet => EntityDisplayKind.Neutral,
                _ => EntityDisplayKind.Scenery,
            };
        }

        if (world.HasComponent<AIControlledComponent>(entity))
            return EntityDisplayKind.Hostile;

        if (world.HasComponent<BuildingComponent>(entity))
            return EntityDisplayKind.Scenery;

        var combat = world.GetComponent<CombatTargetComponent>(entity);
        if (combat != null && combat.Faction > 1)
            return EntityDisplayKind.Hostile;
        if (combat != null && combat.Faction == 0)
            return EntityDisplayKind.Neutral;

        return EntityDisplayKind.Friendly;
    }

    public static bool IsHostileToPlayer(World world, Entity entity, int playerFaction = 1)
    {
        if (world.HasComponent<AIControlledComponent>(entity))
            return true;

        var health = world.GetComponent<HealthComponent>(entity);
        if (health == null || health.IsDead)
            return false;

        var combat = world.GetComponent<CombatTargetComponent>(entity);
        return combat != null && combat.Faction > playerFaction;
    }

    public static Vector4 LabelColor(EntityDisplayKind kind) => kind switch
    {
        EntityDisplayKind.Hostile => HostileColor,
        EntityDisplayKind.Neutral => NeutralColor,
        EntityDisplayKind.Harvestable => HarvestableColor,
        EntityDisplayKind.Scenery => SceneryColor,
        _ => new Vector4(0.55f, 0.85f, 1f, 1f),
    };

    public static Vector4 SelectionRingColor(EntityDisplayKind kind) => kind switch
    {
        EntityDisplayKind.Hostile => SelectionHostile,
        EntityDisplayKind.Neutral => SelectionNeutral,
        EntityDisplayKind.Harvestable => SelectionHarvestable,
        EntityDisplayKind.Scenery => SelectionScenery,
        _ => SelectionFriendly,
    };

    /// <summary>Dimmed selection ring while a structure is still under construction.</summary>
    public static Vector4 SelectionRingColor(World world, Entity entity, EntityDisplayKind kind)
    {
        Vector4 baseColor = SelectionRingColor(kind);
        if (!TryGetConstructionFraction(world, entity, out _))
            return baseColor;

        return baseColor with { W = baseColor.W * 0.55f };
    }

    /// <summary>Returns true when entity is building and outputs normalized 0–1 progress.</summary>
    public static bool TryGetConstructionFraction(World world, Entity entity, out float fraction01)
    {
        var underConstruction = world.GetComponent<UnderConstructionComponent>(entity);
        if (underConstruction == null || underConstruction.TotalBuildTime <= 0f)
        {
            fraction01 = 0f;
            return false;
        }

        fraction01 = Math.Clamp(underConstruction.BuildProgress / underConstruction.TotalBuildTime, 0f, 1f);
        return true;
    }

    /// <summary>Whole-percent construction progress for HUD labels.</summary>
    public static int ConstructionPercent(World world, Entity entity) =>
        TryGetConstructionFraction(world, entity, out float fraction)
            ? (int)MathF.Round(fraction * 100f)
            : 0;

    /// <summary>Appends <c>(42%)</c> suffix when the entity is under construction.</summary>
    public static string AppendConstructionSuffix(World world, Entity entity, string displayName)
    {
        if (!TryGetConstructionFraction(world, entity, out float fraction))
            return displayName;

        return $"{displayName} ({(int)MathF.Round(fraction * 100f)}%)";
    }

    public static Vector4 WorldTintColor(EntityDisplayKind kind) => kind switch
    {
        EntityDisplayKind.Hostile => HostileColor,
        EntityDisplayKind.Harvestable => HarvestableColor,
        EntityDisplayKind.Neutral => NeutralColor,
        _ => new Vector4(0, 0, 0, 0),
    };
}