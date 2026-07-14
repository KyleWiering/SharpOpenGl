using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Computes Terran integrated engine-cluster nozzle positions mirroring
/// <see cref="RaceHullSilhouette"/> stern geometry.
/// </summary>
public static class TerranEngineNozzleLayout
{
    private readonly record struct ClusterParams(
        float LengthScale,
        float WidthScale,
        float HeightMultiplier,
        float SternZFrac,
        float BellSpread);

    /// <summary>
    /// Resolves gameplay hull envelope dimensions for Terran ships (matches mesh build proportions).
    /// </summary>
    public static (float Len, float Wid, float Hgt) ResolveHullDimensions(string hullOrDefinitionId)
    {
        string hullKey = RaceVisualSchema.ResolveHullKey(hullOrDefinitionId);
        var design = ShipDesignCatalog.Resolve(hullOrDefinitionId, "terran");
        var hull = RaceVisualSchema.ResolveHullProfile(design.HullClass);
        var variant = ShipDesignVariant.FromSpec(design);
        RaceVisualSchema.TryGetRace("terran", out RaceVisualDefinition? race);
        race ??= new RaceVisualDefinition { Id = "terran", Style = "retro" };

        float s = hull.Size * variant.LengthScale;
        float len = s * hull.LengthRatio * race.Modifiers.HullLength * variant.LengthScale;
        float wid = s * hull.WidthRatio * race.Modifiers.HullWidth * variant.WidthScale;
        float hgt = s * hull.HeightRatio * variant.HeightScale;
        len += s * variant.NoseExtension * 0.5f;
        len += s * variant.SternExtension * 0.35f;

        ApplyRetroEnvelopeScaling(hullKey, ref len, ref wid, ref hgt);
        return (len, wid, hgt);
    }

    /// <summary>
    /// Local-space nozzle positions for twin (or triple) integrated engine bells.
    /// </summary>
    public static IReadOnlyList<Vector3> ComputeLocalOffsets(
        string hullKey, float len, float wid, float hgt, int engineCount = 2)
    {
        hullKey = RaceVisualSchema.ResolveHullKey(hullKey);
        var cluster = ResolveClusterParams(hullKey);

        float l = len * cluster.LengthScale;
        float bw = wid * cluster.WidthScale;
        float bh = hgt * 0.90f * cluster.HeightMultiplier;

        float sternZ = l * cluster.SternZFrac;
        float depth = l * 0.09f;
        float halfBell = bw * cluster.BellSpread;
        float y = bh * 0.12f;
        float z = sternZ + depth;

        var offsets = new List<Vector3>(Math.Max(engineCount, 2));
        offsets.Add(new Vector3(-halfBell, y, z));
        offsets.Add(new Vector3(halfBell, y, z));

        if (engineCount >= 3)
            offsets.Add(new Vector3(0f, y, z));

        return offsets;
    }

    /// <summary>Emit-rate multiplier bucket for hull class (fighter &lt; capital).</summary>
    public static float ResolveEmitRateScale(string hullKey)
    {
        hullKey = RaceVisualSchema.ResolveHullKey(hullKey);
        return hullKey switch
        {
            "fighter" or "fighter_basic" or "scout" or "scout_light"
                or "interceptor" or "interceptor_mk2" or "drone" or "drone_swarm" => 0.6f,
            "cruiser" or "cruiser_heavy" or "carrier" or "carrier_command" or "dreadnought" => 1.4f,
            "miner_basic" or "miner_eva" or "miner_tractor" or "transport_cargo"
                or "freighter_bulk" or "support_repair" => 0.8f,
            _ => 1.0f,
        };
    }

    /// <summary>Caps per-nozzle emit rate so steady-state live particles stay within emitter capacity.</summary>
    public static float CapEmitRate(ParticleEmitter emitter, float scaledRate)
    {
        float cap = emitter.ParticleLifetime > 1e-4f
            ? emitter.Capacity / emitter.ParticleLifetime
            : scaledRate;
        return Math.Min(scaledRate, cap);
    }

    private static ClusterParams ResolveClusterParams(string hullKey) =>
        hullKey switch
        {
            "fighter" or "fighter_basic" => new(0.96f, 0.92f, 1.28f, -0.30f, 0.09f),
            "hero" or "hero_default" => new(0.92f, 0.76f, 1.00f, -0.32f, 0.11f),
            "scout" or "scout_light" => new(0.92f, 0.72f, 1.22f, -0.28f, 0.06f),
            "interceptor" or "interceptor_mk2" => new(0.90f, 0.80f, 1.10f, -0.30f, 0.08f),
            "drone" or "drone_swarm" => new(0.82f, 0.88f, 1.00f, -0.22f, 0.05f),
            "corvette" or "corvette_fast" => new(0.88f, 0.78f, 1.00f, -0.32f, 0.09f),
            "frigate" or "frigate_strike" => new(0.90f, 0.80f, 1.00f, -0.34f, 0.10f),
            "gunship" or "gunship_heavy" => new(0.86f, 0.88f, 1.00f, -0.30f, 0.11f),
            "bomber" or "bomber_heavy" => new(0.88f, 0.86f, 0.88f, -0.28f, 0.10f),
            "destroyer" or "destroyer_assault" => new(0.90f, 0.82f, 1.00f, -0.34f, 0.11f),
            "cruiser" or "cruiser_heavy" => new(0.86f, 1.06f, 1.04f, -0.32f, 0.12f),
            "carrier" or "carrier_command" => new(0.92f, 1.12f, 1.00f, -0.36f, 0.13f),
            "dreadnought" => new(0.94f, 0.80f, 1.06f, -0.36f, 0.12f),
            "freighter_bulk" => new(0.94f, 0.88f, 1.00f, -0.32f, 0.10f),
            "miner_basic" or "miner_eva" or "miner_tractor" or "transport_cargo" or "support_repair"
                => new(0.94f, 0.88f, 1.00f, -0.32f, 0.08f),
            _ => new(0.94f, 0.88f, 1.00f, -0.28f, 0.10f),
        };

    private static void ApplyRetroEnvelopeScaling(string hullKey, ref float len, ref float wid, ref float hgt)
    {
        switch (hullKey)
        {
            case "fighter" or "fighter_basic":
                wid *= 1.36f; len *= 0.78f; break;
            case "hero" or "hero_default":
                wid *= 1.28f; len *= 0.80f; break;
            case "scout" or "scout_light":
                wid *= 1.58f; len *= 0.70f; break;
            case "interceptor" or "interceptor_mk2":
                wid *= 1.42f; len *= 0.74f; break;
            case "drone" or "drone_swarm":
                wid *= 1.38f; len *= 0.78f; break;
            case "corvette" or "corvette_fast":
                wid *= 1.36f; len *= 0.74f; break;
            case "frigate" or "frigate_strike":
                wid *= 1.28f; len *= 0.82f; break;
            case "gunship" or "gunship_heavy":
                wid *= 1.38f; len *= 0.76f; hgt *= 0.96f; break;
            case "bomber" or "bomber_heavy":
                wid *= 1.32f; len *= 0.82f; hgt *= 0.90f; break;
            case "destroyer" or "destroyer_assault":
                wid *= 1.40f; len *= 0.70f; hgt *= 0.94f; break;
            case "cruiser" or "cruiser_heavy":
                wid *= 1.36f; len *= 0.70f; hgt *= 0.94f; break;
            case "carrier" or "carrier_command":
                wid *= 1.42f; len *= 0.70f; hgt *= 0.86f; break;
            case "dreadnought":
                wid *= 1.26f; len *= 0.74f; hgt *= 0.94f; break;
            case "miner_basic" or "miner_eva" or "miner_tractor":
                wid *= 1.26f; len *= 0.78f; break;
            case "transport_cargo":
                wid *= 1.30f; len *= 0.76f; break;
            case "freighter_bulk":
                wid *= 1.28f; len *= 0.76f; break;
            case "support_repair":
                wid *= 1.28f; len *= 0.76f; break;
        }
    }
}