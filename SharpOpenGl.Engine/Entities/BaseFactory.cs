using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.ECS;

namespace SharpOpenGl.Engine.Entities;

/// <summary>
/// Creates base / building entities from <see cref="EntityDefinition"/> data.
/// </summary>
public sealed class BaseFactory
{
    private readonly AssetManager? _assets;

    /// <param name="assets">
    /// Optional asset manager used to validate mesh references.
    /// Pass <c>null</c> in unit tests where disk access is unavailable.
    /// </param>
    public BaseFactory(AssetManager? assets = null)
    {
        _assets = assets;
    }

    /// <summary>
    /// Spawn a building entity described by <paramref name="def"/> into <paramref name="world"/>.
    /// Attaches: <see cref="TransformComponent"/>, <see cref="RenderComponent"/>,
    /// <see cref="HealthComponent"/>, and <see cref="BuildingComponent"/>.
    /// Buildings have no <see cref="MovementComponent"/> (they are stationary).
    /// </summary>
    /// <returns>The newly created entity handle.</returns>
    public Entity Create(World world, EntityDefinition def, int faction = 1)
    {
        Entity entity = world.CreateEntity();

        world.AddComponent(entity, new TransformComponent());

        string meshKey = FactoryHelpers.ResolveMesh(
            _assets, def.Mesh, def.FallbackMesh, "meshes/default_base.obj");
        world.AddComponent(entity, new RenderComponent { MeshKey = meshKey, MeshId = -1 });

        FactoryHelpers.ApplyHealth(world, entity, def.Components?.Health);
        FactoryHelpers.ApplyBuilding(world, entity, def.Components?.Building);
        FactoryHelpers.ApplyWeapons(world, entity, def.Components?.Weapons);
        FactoryHelpers.ApplySightRadius(world, entity, def.Components);

        if (def.Components?.Weapons is { Length: > 0 })
        {
            world.AddComponent(entity, new CombatTargetComponent
            {
                Faction = faction,
                TargetingMode = TargetPriority.Closest,
                Priority = 50,
            });
        }

        string buildingType = def.Components?.Building?.BuildingType ?? def.Id;
        TrySpawnArticulation(world, entity, buildingType, def);

        return entity;
    }

    /// <summary>
    /// Spawns articulated child entities for a building (turrets, sensor dish, shipyard crane/door).
    /// Used by <see cref="Create"/> and skirmish spawn paths that assemble buildings manually.
    /// </summary>
    public static void TrySpawnArticulation(
        World world, Entity owner, string buildingType, EntityDefinition? def = null) =>
        BaseArticulationFactory.TrySpawnArticulation(world, owner, buildingType, def);
}