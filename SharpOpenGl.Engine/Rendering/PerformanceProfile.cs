namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Describes a quality tier for adaptive performance scaling.
/// Used to reduce rendering load on mobile or low-end devices.
/// </summary>
public enum PerformanceTier
{
    /// <summary>Full quality — all effects, maximum draw distance.</summary>
    High,

    /// <summary>Reduced particles and LOD distances — suitable for mid-range mobile.</summary>
    Medium,

    /// <summary>Minimal effects, aggressive LOD — sustains 30 fps on low-end devices.</summary>
    Low,
}

/// <summary>
/// Quality settings derived from a <see cref="PerformanceTier"/>.
/// Renderers and systems read these values to scale their workload.
/// </summary>
public sealed class PerformanceProfile
{
    // ── Preset factory ────────────────────────────────────────────────────────

    /// <summary>Create a profile for the given <paramref name="tier"/>.</summary>
    public static PerformanceProfile FromTier(PerformanceTier tier) => tier switch
    {
        PerformanceTier.Low    => Low,
        PerformanceTier.Medium => Medium,
        _                      => High,
    };

    /// <summary>High-quality preset (desktop / powerful GPU).</summary>
    public static readonly PerformanceProfile High = new()
    {
        Tier                    = PerformanceTier.High,
        MaxParticlesPerEmitter  = 256,
        ParticleEmitRateScale   = 1.0f,
        LodSimpleDistanceScale  = 1.0f,
        LodIconDistanceScale    = 1.0f,
        MaxDrawDistance         = 2000f,
        EnableExplosionEffects  = true,
        EnableShieldEffects     = true,
        EnableWeaponTrails      = true,
        ShaderComplexity        = ShaderComplexityLevel.Full,
        MaxBatchSize            = 512,
    };

    /// <summary>Medium-quality preset (mid-range mobile / tablet).</summary>
    public static readonly PerformanceProfile Medium = new()
    {
        Tier                    = PerformanceTier.Medium,
        MaxParticlesPerEmitter  = 128,
        ParticleEmitRateScale   = 0.6f,
        LodSimpleDistanceScale  = 0.7f,
        LodIconDistanceScale    = 0.7f,
        MaxDrawDistance         = 1200f,
        EnableExplosionEffects  = true,
        EnableShieldEffects     = false,
        EnableWeaponTrails      = true,
        ShaderComplexity        = ShaderComplexityLevel.Reduced,
        MaxBatchSize            = 256,
    };

    /// <summary>Low-quality preset (low-end mobile device).</summary>
    public static readonly PerformanceProfile Low = new()
    {
        Tier                    = PerformanceTier.Low,
        MaxParticlesPerEmitter  = 48,
        ParticleEmitRateScale   = 0.3f,
        LodSimpleDistanceScale  = 0.5f,
        LodIconDistanceScale    = 0.5f,
        MaxDrawDistance         = 800f,
        EnableExplosionEffects  = false,
        EnableShieldEffects     = false,
        EnableWeaponTrails      = false,
        ShaderComplexity        = ShaderComplexityLevel.Minimal,
        MaxBatchSize            = 128,
    };

    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>The quality tier this profile represents.</summary>
    public PerformanceTier Tier { get; init; }

    /// <summary>Maximum number of simultaneous particles per emitter.</summary>
    public int MaxParticlesPerEmitter { get; init; }

    /// <summary>Multiplier applied to every emitter's <c>EmitRate</c>.</summary>
    public float ParticleEmitRateScale { get; init; }

    /// <summary>
    /// Multiplier on <see cref="MeshLod.SimpleDistance"/>.
    /// Values &lt; 1 cause earlier LOD transitions (fewer vertices at a given distance).
    /// </summary>
    public float LodSimpleDistanceScale { get; init; }

    /// <summary>Multiplier on <see cref="MeshLod.IconDistance"/>.</summary>
    public float LodIconDistanceScale { get; init; }

    /// <summary>Maximum camera draw distance in world units.</summary>
    public float MaxDrawDistance { get; init; }

    /// <summary>Whether explosion particle effects are rendered.</summary>
    public bool EnableExplosionEffects { get; init; }

    /// <summary>Whether shield-bubble translucent effects are rendered.</summary>
    public bool EnableShieldEffects { get; init; }

    /// <summary>Whether weapon-fire trail effects are rendered.</summary>
    public bool EnableWeaponTrails { get; init; }

    /// <summary>Shader complexity level to request from the shader manager.</summary>
    public ShaderComplexityLevel ShaderComplexity { get; init; }

    /// <summary>Maximum number of draw calls batched per frame.</summary>
    public int MaxBatchSize { get; init; }
}

/// <summary>
/// Shader complexity hint passed to renderers for WebGL2 / mobile optimisations.
/// </summary>
public enum ShaderComplexityLevel
{
    /// <summary>All lighting, specular, and post-processing effects active.</summary>
    Full,

    /// <summary>Diffuse + emissive only; no specular or per-pixel lighting.</summary>
    Reduced,

    /// <summary>Flat colour / vertex colour only; minimal GPU cost.</summary>
    Minimal,
}
