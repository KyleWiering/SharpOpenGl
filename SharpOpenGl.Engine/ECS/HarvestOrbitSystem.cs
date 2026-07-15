namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Drives harvest collectors toward an orbit ring around assigned resource nodes.
/// Runs before <see cref="MovementSystem"/> so path targets are applied each frame.
/// </summary>
public sealed class HarvestOrbitSystem : GameSystem
{
    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        foreach (var (entity, collector) in world.Query<ResourceCollectorComponent>())
        {
            if (collector.State != CollectorState.MovingToNode
                && collector.State != CollectorState.Collecting)
                continue;

            HarvestOrbitHelper.UpdateHarvestOrbit(world, entity, collector, deltaTime);
        }
    }
}