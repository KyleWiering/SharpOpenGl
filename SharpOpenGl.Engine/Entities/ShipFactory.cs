using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.ECS;

namespace SharpOpenGl.Engine.Entities;

/// <summary>
/// Creates ship entities (hero and fighter classes) from <see cref="EntityDefinition"/> data.
/// </summary>
/// <remarks>
/// Typical usage:
/// <code>
/// var factory = new ShipFactory(assetManager);
/// Entity hero = factory.Create(world, def);
/// </code>
/// </remarks>
public sealed class ShipFactory
{
    private readonly AssetManager? _assets;

    /// <param name="assets">
    /// Optional asset manager used to validate mesh references.
    /// Pass <c>null</c> in unit tests where disk access is unavailable.
    /// </param>
    public ShipFactory(AssetManager? assets = null)
    {
        _assets = assets;
    }

    /// <summary>
    /// Spawn a ship entity described by <paramref name="def"/> into <paramref name="world"/>.
    /// Attaches: <see cref="TransformComponent"/>, <see cref="RenderComponent"/>,
    /// <see cref="HealthComponent"/>, <see cref="MovementComponent"/>,
    /// <see cref="WeaponListComponent"/>, and (if applicable)
    /// <see cref="HeroComponent"/> or <see cref="SquadMemberComponent"/>.
    /// </summary>
    /// <returns>The newly created entity handle.</returns>
    public Entity Create(World world, EntityDefinition def)
    {
        Entity entity = world.CreateEntity();

        // Transform (spawns at origin; caller can set position after)
        world.AddComponent(entity, new TransformComponent());

        // Render — resolve mesh with fallback
        string meshKey = FactoryHelpers.ResolveMesh(
            _assets, def.Mesh, def.FallbackMesh, "meshes/default_ship.obj");
        world.AddComponent(entity, new RenderComponent { MeshKey = meshKey, MeshId = -1 });

        // Core gameplay components
        FactoryHelpers.ApplyHealth(world, entity, def.Components?.Health);
        FactoryHelpers.ApplyMovement(world, entity, def.Components?.Movement);
        FactoryHelpers.ApplyWeapons(world, entity, def.Components?.Weapons);
        FactoryHelpers.ApplyShipTurretArticulation(world, entity, def);
        SpecialHullArticulationSpawner.AttachSpecialHullParts(world, entity, def, raceId: string.Empty);

        // Category-specific components
        if (string.Equals(def.Category, "hero", StringComparison.OrdinalIgnoreCase))
        {
            FactoryHelpers.ApplyHero(world, entity,
                def.Components?.Hero,
                def.Components?.Abilities);
        }
        else if (def.Components?.SquadMember != null)
        {
            FactoryHelpers.ApplySquadMember(world, entity, def.Components.SquadMember);
        }

        // Resource collector (miners)
        FactoryHelpers.ApplyResourceCollector(world, entity, def.Components?.ResourceCollector);
        FactoryHelpers.ApplyShipRepair(world, entity, def.Components?.ShipRepair);
        FactoryHelpers.ApplyStructureBuilder(world, entity, def.Components?.StructureBuilder);
        FactoryHelpers.ApplySightRadius(world, entity, def.Components);

        return entity;
    }
}
