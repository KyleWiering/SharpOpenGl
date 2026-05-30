using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.ECS;

namespace SharpOpenGl.Engine.Entities;

/// <summary>
/// Generic factory that creates any unit-type entity from an <see cref="EntityDefinition"/>.
/// Delegates to <see cref="ShipFactory"/> or <see cref="BaseFactory"/> based on category,
/// and handles unknown categories with a safe default.
/// </summary>
public sealed class UnitFactory
{
    private readonly ShipFactory _shipFactory;
    private readonly BaseFactory _baseFactory;

    /// <param name="assets">
    /// Optional asset manager forwarded to inner factories for mesh resolution.
    /// </param>
    public UnitFactory(AssetManager? assets = null)
    {
        _shipFactory = new ShipFactory(assets);
        _baseFactory = new BaseFactory(assets);
    }

    /// <summary>
    /// Spawn the appropriate entity type for <paramref name="def"/> into <paramref name="world"/>.
    /// <list type="bullet">
    ///   <item>Categories <c>"base"</c> / <c>"building"</c> → <see cref="BaseFactory"/>.</item>
    ///   <item>All other categories → <see cref="ShipFactory"/>.</item>
    /// </list>
    /// </summary>
    /// <returns>The newly created entity handle.</returns>
    public Entity Create(World world, EntityDefinition def)
    {
        bool isBuilding = def.Category.Equals("base", StringComparison.OrdinalIgnoreCase)
                       || def.Category.Equals("building", StringComparison.OrdinalIgnoreCase);

        return isBuilding
            ? _baseFactory.Create(world, def)
            : _shipFactory.Create(world, def);
    }
}
