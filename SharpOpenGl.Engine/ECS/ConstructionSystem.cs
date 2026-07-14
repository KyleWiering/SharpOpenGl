using SharpOpenGl.Engine.Entities;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Advances autonomous structure construction each frame. When build time elapses,
/// removes <see cref="UnderConstructionComponent"/> and activates deferred gameplay
/// components (production, weapons, sight).
/// </summary>
public sealed class ConstructionSystem : GameSystem
{
    private readonly Func<string, EntityDefinition?> _definitionLoader;

    /// <summary>
    /// Optional hook for the host layer to swap scaffold mesh to the final building mesh.
    /// Registered before <see cref="BuildSystem"/> so production can start the same frame.
    /// </summary>
    public Action<World, Entity, EntityDefinition>? OnConstructionComplete { get; set; }

    public ConstructionSystem(Func<string, EntityDefinition?> definitionLoader)
    {
        _definitionLoader = definitionLoader;
    }

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        var completing = new List<(Entity Entity, UnderConstructionComponent UnderConstruction)>();

        foreach (var (entity, underConstruction) in world.Query<UnderConstructionComponent>())
        {
            underConstruction.BuildProgress += deltaTime;
            if (underConstruction.BuildProgress >= underConstruction.TotalBuildTime)
                completing.Add((entity, underConstruction));
        }

        foreach (var (entity, _) in completing)
            CompleteConstruction(world, entity);
    }

    private void CompleteConstruction(World entityWorld, Entity entity)
    {
        var underConstruction = entityWorld.GetComponent<UnderConstructionComponent>(entity);
        if (underConstruction == null) return;

        var def = _definitionLoader(underConstruction.DefinitionId);
        entityWorld.RemoveComponent<UnderConstructionComponent>(entity);

        if (def == null) return;

        var building = entityWorld.GetComponent<BuildingComponent>(entity);
        if (building != null)
        {
            var buildingDef = def.Components?.Building;
            building.ProductionRate = buildingDef?.ProductionRate ?? 1f;
        }

        if (def.Components?.Weapons is { Length: > 0 } &&
            entityWorld.GetComponent<WeaponListComponent>(entity) == null)
        {
            FactoryHelpers.ApplyWeapons(entityWorld, entity, def.Components.Weapons);
            entityWorld.AddComponent(entity, new CombatTargetComponent
            {
                Faction = underConstruction.PlayerId,
                TargetingMode = TargetPriority.Closest,
                Priority = 50,
            });
        }

        if (entityWorld.GetComponent<SightRadiusComponent>(entity) == null)
        {
            int sight = def.Components?.SightRadius > 0 ? def.Components.SightRadius : 10;
            entityWorld.AddComponent(entity, new SightRadiusComponent { Radius = sight });
        }

        var health = entityWorld.GetComponent<HealthComponent>(entity);
        if (health != null)
        {
            var healthDef = def.Components?.Health;
            float maxHp = healthDef?.MaxHP ?? health.MaxHP;
            health.MaxHP = maxHp;
            health.CurrentHP = maxHp;
            health.Armor = healthDef?.Armor ?? health.Armor;
        }

        OnConstructionComplete?.Invoke(entityWorld, entity, def);
    }
}