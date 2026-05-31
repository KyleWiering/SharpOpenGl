using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// A single particle in a particle emitter.
/// Treated as a struct-like value object stored in an array pool.
/// </summary>
public sealed class Particle
{
    /// <summary>Current world-space position.</summary>
    public Vector3 Position { get; set; }

    /// <summary>Velocity (units per second).</summary>
    public Vector3 Velocity { get; set; }

    /// <summary>Current RGBA colour (channels 0–1).</summary>
    public Vector4 Color { get; set; } = Vector4.One;

    /// <summary>Target colour at end of life (linearly interpolated).</summary>
    public Vector4 EndColor { get; set; } = Vector4.Zero;

    /// <summary>Age in seconds (0 = just spawned).</summary>
    public float Age { get; set; }

    /// <summary>Maximum lifetime in seconds.</summary>
    public float MaxAge { get; set; } = 1f;

    /// <summary>Whether this slot is currently in use.</summary>
    public bool IsAlive { get; set; }

    /// <summary>Normalised age in [0,1].</summary>
    public float LifeRatio => MaxAge > 0f ? Math.Clamp(Age / MaxAge, 0f, 1f) : 1f;

    /// <summary>Advance the particle by one simulation step.</summary>
    public void Update(float dt)
    {
        if (!IsAlive) return;
        Age      += dt;
        Position += Velocity * dt;
        Color     = Vector4.Lerp(Color, EndColor, LifeRatio);
        if (Age >= MaxAge)
            IsAlive = false;
    }
}
