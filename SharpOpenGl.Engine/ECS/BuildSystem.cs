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

    /// <summary>
    /// Optional hook invoked after a unit spawns so the game layer can assign meshes,
    /// selection, and player ownership.
    /// </summary>
    public Action<World, Entity, Entity, BuildingComponent, EntityDefinition>? OnUnitSpawned { get; set; }

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

            building.BuildProgress += deltaTime * building.ProductionRate;

            string currentId = building.BuildQueue.Peek();
            var def = _definitionLoader(currentId);
            if (def == null)
            {
                building.BuildQueue.Dequeue();
                building.BuildProgress = 0f;
                continue;
            }

            if (building.BuildProgress >= def.BuildTime)
            {
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

        var buildingTransform = world.GetComponent<TransformComponent>(buildingEntity);
        var spawnTransform = world.GetComponent<TransformComponent>(spawned);
        if (buildingTransform != null && spawnTransform != null)
        {
            Vector3 exitOffset = building.RallyPoint.HasValue
                ? (building.RallyPoint.Value - buildingTransform.Position).Normalized() * 35f
                : new Vector3(35f, 0f, 0f);
            if (exitOffset.LengthSquared < 1f)
                exitOffset = new Vector3(35f, 0f, 0f);
            spawnTransform.Position = buildingTransform.Position + exitOffset;
        }

        if (building.RallyPoint.HasValue)
        {
            var movement = world.GetComponent<MovementComponent>(spawned);
            if (movement != null)
                movement.PathTarget = building.RallyPoint.Value;
        }

        var collector = world.GetComponent<ResourceCollectorComponent>(spawned);
        if (collector != null)
        {
            collector.PlayerId = building.PlayerId;
            collector.DepositTarget = buildingEntity;
        }

        OnUnitSpawned?.Invoke(world, spawned, buildingEntity, building, def);
    }
}