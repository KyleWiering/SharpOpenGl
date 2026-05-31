using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Attaches a <see cref="ParticleEmitter"/> to an entity.
/// The <see cref="ParticleSystem"/> advances it each frame.
/// </summary>
public sealed class ParticleEmitterComponent
{
    /// <summary>The emitter instance owned by this component.</summary>
    public ParticleEmitter Emitter { get; set; } = new ParticleEmitter();
}
