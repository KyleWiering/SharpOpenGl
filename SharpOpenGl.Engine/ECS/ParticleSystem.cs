namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Advances all <see cref="ParticleEmitterComponent"/> instances in the world each frame.
/// Rendering of the particles is handled by the platform renderer, not this system.
/// </summary>
public sealed class ParticleSystem : GameSystem
{
    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        foreach (var (_, emitterComp) in world.Query<ParticleEmitterComponent>())
        {
            emitterComp.Emitter.Update(deltaTime);
        }
    }
}
