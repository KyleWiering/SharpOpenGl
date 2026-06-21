using OpenTK.Mathematics;
using SharpOpenGl.Engine.Events;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Manages short-lived explosion particle emitters driven by <see cref="ExplosionVfxEvent"/>.
/// Platform renderers call <see cref="BuildVertexData"/> each frame to draw GL_POINTS bursts.
/// </summary>
public sealed class ExplosionVfxController
{
    private const int MaxEmitters = 48;
    private const int MaxParticles = 4096;
    private const float BurstSimSeconds = 0.1f;

    private readonly List<ParticleEmitter> _emitters = new();
    private readonly float[] _vertexBuffer = new float[MaxParticles * 6];

    /// <summary>When false, incoming events are ignored (e.g. low performance tier).</summary>
    public bool Enabled { get; set; } = true;

    public int ActiveEmitterCount => _emitters.Count;

    public void Bind(EventBus bus) =>
        bus.Subscribe<ExplosionVfxEvent>(evt => Spawn(evt.Position, evt.Kind, evt.Scale));

    public void Spawn(Vector3 position, ExplosionVfxKind kind, float scale = 1f)
    {
        if (!Enabled) return;

        var emitter = kind switch
        {
            ExplosionVfxKind.Impact       => ParticleEffects.CreateImpactBurst(position, scale),
            ExplosionVfxKind.ShipDeath    => ParticleEffects.CreateShipDeathExplosion(position, scale),
            ExplosionVfxKind.StationDeath => ParticleEffects.CreateStationDeathExplosion(position, scale),
            _                             => ParticleEffects.CreateImpactBurst(position, scale),
        };

        emitter.Update(BurstSimSeconds);
        emitter.IsEmitting = false;

        if (_emitters.Count >= MaxEmitters)
            _emitters.RemoveAt(0);

        _emitters.Add(emitter);
    }

    public void Update(float deltaTime)
    {
        for (int i = _emitters.Count - 1; i >= 0; i--)
        {
            _emitters[i].Update(deltaTime);
            if (_emitters[i].LiveCount == 0)
                _emitters.RemoveAt(i);
        }
    }

    /// <summary>Returns live particle count and interleaved position/RGB vertex data.</summary>
    public (int PointCount, float[] Vertices) BuildVertexData()
    {
        int totalPoints = 0;
        int floatIndex = 0;

        foreach (var emitter in _emitters)
        {
            int written = emitter.WriteColoredPoints(_vertexBuffer, floatIndex);
            if (written == 0) continue;

            totalPoints += written;
            floatIndex += written * 6;
            if (floatIndex >= _vertexBuffer.Length)
                break;
        }

        if (floatIndex == _vertexBuffer.Length)
            return (totalPoints, _vertexBuffer);

        var trimmed = new float[floatIndex];
        Array.Copy(_vertexBuffer, trimmed, floatIndex);
        return (totalPoints, trimmed);
    }
}