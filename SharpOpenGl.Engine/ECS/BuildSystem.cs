using OpenTK.Mathematics;
using SharpOpenGl.Engine.Entities;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Advances production queues on all <see cref="BuildingComponent"/> entities each frame.
/// When an item completes, spawns the entity and sends it to the rally point.
/// </summary>
public sealed class BuildSystem : GameSystem
{
    private readonly UnitFactory _factory;
    private readonly Func<string, EntityDefinition?> _definitionLoader;

    /// <param name="factory">Factory for creating spawned entities.</param>
    /// <param name="definitionLoader">
    /// Function that resolves a definition ID to its <see cref="EntityDefinition"/>.
    /// Returns null if the definition is not found.
    /// </param>
    public BuildSystem(UnitFactory factory, Func<string, EntityDefinition?> definitionLoader)
    {
        _factory = factory;
        _definitionLoader = definitionLoader;
    }

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        foreach (var (entity, building) in world.Query<BuildingComponent>())
        {
            if (building.BuildQueue.Count == 0) continue;

            // Advance build progress
            building.BuildProgress += deltaTime * building.ProductionRate;

            // Check if current item is complete
            string currentId = building.BuildQueue.Peek();
            var def = _definitionLoader(currentId);
            if (def == null)
            {
                // Unknown definition — skip this item
                building.BuildQueue.Dequeue();
                building.BuildProgress = 0f;
                continue;
            }

            if (building.BuildProgress >= def.BuildTime)
            {
                // Item complete — spawn entity
                building.BuildQueue.Dequeue();
                building.BuildProgress = 0f;

                SpawnCompletedUnit(world, entity, building, def);
            }
        }
    }

    private void SpawnCompletedUnit(
        World world, Entity buildingEntity, BuildingComponent building, EntityDefinition def)
    {
        Entity spawned = _factory.Create(world, def);

        // Position near the building
        var buildingTransform = world.GetComponent<TransformComponent>(buildingEntity);
        if (buildingTransform != null)
        {
            var spawnTransform = world.GetComponent<TransformComponent>(spawned);
            if (spawnTransform != null)
            {
                // Offset spawn position slightly from building
                spawnTransform.Position = buildingTransform.Position + new Vector3(20f, 0f, 0f);
            }
        }

        // Send to rally point if set
        if (building.RallyPoint.HasValue)
        {
            var movement = world.GetComponent<MovementComponent>(spawned);
            if (movement != null)
                movement.PathTarget = building.RallyPoint.Value;
        }

        // If it's a miner and building has a deposit target setup, assign it
        var collector = world.GetComponent<ResourceCollectorComponent>(spawned);
        if (collector != null)
        {
            collector.PlayerId = building.PlayerId;
            collector.DepositTarget = buildingEntity;
        }
    }
}
