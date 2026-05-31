using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Manages a fixed-size pool of <see cref="Particle"/> instances.
/// Spawns, updates, and provides vertex data for rendering each frame.
/// </summary>
public sealed class ParticleEmitter
{
    private readonly Particle[] _pool;
    private float _emitAccumulator;

    // ── Configuration ─────────────────────────────────────────────────────────

    /// <summary>World-space origin of new particles.</summary>
    public Vector3 Origin { get; set; } = Vector3.Zero;

    /// <summary>Particles emitted per second.</summary>
    public float EmitRate { get; set; } = 20f;

    /// <summary>Base velocity applied to newly spawned particles.</summary>
    public Vector3 BaseVelocity { get; set; } = Vector3.UnitY;

    /// <summary>Random velocity spread added per axis (±spread).</summary>
    public float VelocitySpread { get; set; } = 0.1f;

    /// <summary>Lifetime in seconds for newly spawned particles.</summary>
    public float ParticleLifetime { get; set; } = 1f;

    /// <summary>Start colour for new particles.</summary>
    public Vector4 StartColor { get; set; } = Vector4.One;

    /// <summary>End colour particles fade to over their lifetime.</summary>
    public Vector4 EndColor { get; set; } = Vector4.Zero;

    /// <summary>When false, no new particles are spawned (existing ones still update).</summary>
    public bool IsEmitting { get; set; } = true;

    /// <summary>Maximum particles in the pool.</summary>
    public int Capacity => _pool.Length;

    // ── Construction ─────────────────────────────────────────────────────────

    /// <param name="capacity">Maximum simultaneous particles.</param>
    public ParticleEmitter(int capacity = 128)
    {
        _pool = new Particle[capacity];
        for (int i = 0; i < capacity; i++)
            _pool[i] = new Particle();
    }

    // ── Simulation ────────────────────────────────────────────────────────────

    /// <summary>Advance simulation by <paramref name="dt"/> seconds.</summary>
    public void Update(float dt)
    {
        // Update existing particles
        for (int i = 0; i < _pool.Length; i++)
            _pool[i].Update(dt);

        if (!IsEmitting) return;

        // Emit new particles
        _emitAccumulator += EmitRate * dt;
        while (_emitAccumulator >= 1f)
        {
            SpawnOne();
            _emitAccumulator -= 1f;
        }
    }

    /// <summary>Number of currently live particles.</summary>
    public int LiveCount
    {
        get
        {
            int count = 0;
            foreach (var p in _pool)
                if (p.IsAlive) count++;
            return count;
        }
    }

    /// <summary>
    /// Build a float array of live particle positions (x, y, z per point)
    /// for upload to a GL_POINTS draw call.  Returns the number of live particles.
    /// </summary>
    public int BuildPointBuffer(float[] buffer)
    {
        int idx = 0;
        foreach (var p in _pool)
        {
            if (!p.IsAlive) continue;
            if (idx + 3 > buffer.Length) break;
            buffer[idx++] = p.Position.X;
            buffer[idx++] = p.Position.Y;
            buffer[idx++] = p.Position.Z;
        }
        return idx / 3;
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private static readonly Random _rng = new(42);

    private void SpawnOne()
    {
        for (int i = 0; i < _pool.Length; i++)
        {
            if (_pool[i].IsAlive) continue;

            float rx = (float)(_rng.NextDouble() * 2 - 1) * VelocitySpread;
            float ry = (float)(_rng.NextDouble() * 2 - 1) * VelocitySpread;
            float rz = (float)(_rng.NextDouble() * 2 - 1) * VelocitySpread;

            _pool[i].Position = Origin;
            _pool[i].Velocity = BaseVelocity + new Vector3(rx, ry, rz);
            _pool[i].Color    = StartColor;
            _pool[i].EndColor = EndColor;
            _pool[i].Age      = 0f;
            _pool[i].MaxAge   = ParticleLifetime;
            _pool[i].IsAlive  = true;
            return;
        }
        // Pool exhausted — no slot available
    }
}
