using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Procedural ship meshes for 8 races × 17 hull classes.
/// All geometry points along +Z (bow); colors are baked per-vertex (stride 6).
/// </summary>
public static class RaceShipMeshes
{
    public const string DefaultRace = "terran";

    static RaceShipMeshes() => RaceVisualSchema.Load();


    public static float[] BuildDesign(ShipDesignSpec design, Vector3? tint = null, float sizeScale = 1f)
        => BuildDesignInternal(design, tint, sizeScale);

    public static float[] BuildDesign(int designIndex, Vector3? tint = null, float sizeScale = 1f)
        => BuildDesignInternal(ShipDesignCatalog.Get(designIndex), tint, sizeScale);

    public static float[] BuildDesign(string designId, Vector3? tint = null, float sizeScale = 1f)
        => BuildDesignInternal(ShipDesignCatalog.GetById(designId), tint, sizeScale);

    private static float[] BuildDesignInternal(ShipDesignSpec design, Vector3? tint, float sizeScale, string? definitionId = null)
    {
        RaceVisualSchema.TryGetRace(design.RaceId, out RaceVisualDefinition? race);
        race ??= RaceVisualSchema.AllRaces.FirstOrDefault() ?? new RaceVisualDefinition { Id = DefaultRace };

        var hull = RaceVisualSchema.ResolveHullProfile(design.HullClass);
        var variant = ShipDesignVariant.FromSpec(design);
        var writer = new RaceMeshWriter();
        string hullKey = ResolveGameplayHullKey(definitionId, design.HullClass);

        float s = hull.Size * sizeScale * variant.LengthScale;
        float len = s * hull.LengthRatio * race.Modifiers.HullLength * variant.LengthScale;
        float wid = s * hull.WidthRatio * race.Modifiers.HullWidth * variant.WidthScale;
        float hgt = s * hull.HeightRatio * variant.HeightScale;

        len += s * variant.NoseExtension * 0.5f;
        len += s * variant.SternExtension * 0.35f;

        if (race.Style.Equals("retro", StringComparison.OrdinalIgnoreCase))
        {
            // Aspect = mesh AABB width/length; target ~0.94 (hullWidth/hullLength = 1.08/1.15).
            // Widen lateral envelope + trim AABB length — streamlined loft, not narrow pagoda stacks.
            if (hullKey is "fighter" or "fighter_basic")
            {
                // hullLength 1.25 / hullWidth 0.97 — narrow beam + forward +Z bow elongation
                wid *= 1.36f;
                len *= 0.78f;
            }
            else if (hullKey is "hero" or "hero_default")
            {
                wid *= 1.28f;
                len *= 0.80f;
            }
            else if (hullKey is "scout" or "scout_light")
            {
                // Section-02: narrowest needle prow — trim beam, elongate +Z bow envelope.
                wid *= 1.58f;
                len *= 0.70f;
            }
            else if (hullKey is "interceptor" or "interceptor_mk2")
            {
                wid *= 1.42f;
                len *= 0.74f;
            }
            else if (hullKey is "drone" or "drone_swarm")
            {
                // Pod-cluster spine — widest-for-length compact footprint.
                wid *= 1.38f;
                len *= 0.78f;
            }
            else if (hullKey is "corvette" or "corvette_fast")
            {
                wid *= 1.36f;
                len *= 0.74f;
            }
            else if (hullKey is "frigate" or "frigate_strike")
            {
                // Longer midsection read — modest prow elongation.
                wid *= 1.28f;
                len *= 0.82f;
            }
            else if (hullKey is "gunship" or "gunship_heavy")
            {
                // Broader gun-forward beam than corvette.
                wid *= 1.38f;
                len *= 0.76f;
                hgt *= 0.96f;
            }
            else if (hullKey is "bomber" or "bomber_heavy")
            {
                // Wide belly ordnance — lower profile than gunship.
                wid *= 1.32f;
                len *= 0.82f;
                hgt *= 0.90f;
            }
            else if (hullKey is "destroyer" or "destroyer_assault")
            {
                // Gun-forward prow + forward superstructure — elongate bow envelope.
                wid *= 1.40f;
                len *= 0.70f;
                hgt *= 0.94f;
            }
            else if (hullKey is "cruiser" or "cruiser_heavy")
            {
                // Broad midsection + command tower dorsal bump.
                wid *= 1.36f;
                len *= 0.70f;
                hgt *= 0.94f;
            }
            else if (hullKey is "carrier" or "carrier_command")
            {
                // Wide flat deck — lateral span dominates, low vertical stack.
                wid *= 1.42f;
                len *= 0.70f;
                hgt *= 0.86f;
            }
            else if (hullKey is "dreadnought")
            {
                // Massive prow wedge — tallest capital envelope.
                wid *= 1.26f;
                len *= 0.74f;
                hgt *= 0.94f;
            }
            else if (hullKey is "miner_basic" or "miner_eva" or "miner_tractor")
            {
                wid *= 1.26f;
                len *= 0.78f;
            }
            else if (hullKey is "transport_cargo")
            {
                wid *= 1.30f;
                len *= 0.76f;
            }
            else if (hullKey is "freighter_bulk")
            {
                wid *= 1.28f;
                len *= 0.76f;
            }
            else if (hullKey is "support_repair")
            {
                wid *= 1.28f;
                len *= 0.76f;
            }
        }

        if (race.Style.Equals("vasudan", StringComparison.OrdinalIgnoreCase))
        {
            // Aspect = mesh AABB width/length; target ~0.41 (hullWidth/hullLength = 0.52/1.28).
            // Widen lateral envelope and trim stern Z — never narrow+elongate (+Z hurts aspect).
            // Loop-12 gap-close FINAL 4: tri trim + proportions envelope fit + aspect recovery.
            if (hullKey is "destroyer_assault")
            {
                wid *= 1.04f;
                len *= 0.94f;
            }
            else if (hullKey is "drone_swarm")
            {
                wid *= 1.04f;
                len *= 0.92f;
            }
            else if (hullKey is "cruiser_heavy")
            {
                wid *= 1.08f;
                len *= 0.88f;
                hgt *= 0.88f;
            }
            else if (hullKey is "hero_default")
            {
                wid *= 1.10f;
                len *= 0.88f;
            }
            else if (hullKey is "miner_basic")
            {
                wid *= 1.08f;
                len *= 0.88f;
            }
            else if (hullKey is "transport_cargo")
            {
                wid *= 1.08f;
                len *= 0.83f;
            }
            else if (hullKey is "scout_light")
            {
                wid *= 1.02f;
                len *= 0.86f;
            }
            else if (hullKey is "interceptor_mk2")
            {
                wid *= 1.10f;
                len *= 0.88f;
            }
            else if (hullKey is "freighter_bulk")
            {
                wid *= 1.12f;
                len *= 0.84f;
            }
        }

        if (race.Style.Equals("truss", StringComparison.OrdinalIgnoreCase))
        {
            // Aspect = mesh AABB width/length; target ~1.35 (hullWidth/hullLength = 1.42/1.05).
            // Vesper lesson adapted for truss: widen lateral solar/truss beam, trim AABB length, anchor bow +Z.
            if (hullKey is "fighter" or "fighter_basic")
            {
                wid *= 1.32f;
                len *= 0.76f;
            }
            else if (hullKey is "scout" or "scout_light")
            {
                wid *= 1.42f;
                len *= 0.72f;
            }
            else if (hullKey is "interceptor" or "interceptor_mk2")
            {
                wid *= 1.30f;
                len *= 0.76f;
                hgt *= 1.08f;
            }
            else if (hullKey is "drone" or "drone_swarm")
            {
                wid *= 1.24f;
                len *= 0.80f;
            }
            else if (hullKey is "hero" or "hero_default")
            {
                wid *= 1.06f;
                len *= 0.82f;
            }
            else if (hullKey is "corvette" or "corvette_fast")
            {
                // Loop-07: aspect polish toward ~1.35 — modest prow elongation + solar beam widen.
                wid *= 1.42f;
                len *= 0.69f;
            }
            else if (hullKey is "frigate_strike")
            {
                // Loop-08: aspect rebalance — len×1.10 bow anchor; relax envelope from loop-7 overshoot.
                wid *= 1.44f;
                len *= 0.78f;
            }
            else if (hullKey is "frigate")
            {
                wid *= 1.38f;
                len *= 0.72f;
            }
            else if (hullKey is "gunship" or "gunship_heavy")
            {
                wid *= 1.04f;
                len *= 0.82f;
            }
            else if (hullKey is "bomber" or "bomber_heavy")
            {
                wid *= 1.02f;
                len *= 0.84f;
            }
            else if (hullKey is "destroyer" or "destroyer_assault")
            {
                wid *= 1.48f;
                len *= 0.62f;
                hgt *= 0.94f;
            }
            else if (hullKey is "cruiser" or "cruiser_heavy")
            {
                wid *= 1.26f;
                len *= 0.72f;
                hgt *= 0.92f;
            }
            else if (hullKey is "carrier" or "carrier_command")
            {
                wid *= 1.32f;
                len *= 0.68f;
            }
            else if (hullKey is "dreadnought")
            {
                // Loop-08: partial loop-7 rollback — restore len/hh toward loop-6; modest lateral hw widen held.
                wid *= 1.32f;
                len *= 0.65f;
                hgt *= 0.90f;
            }
            else if (hullKey is "miner_basic")
            {
                wid *= 1.24f;
                len *= 0.76f;
            }
            else if (hullKey is "miner_eva" or "miner_tractor")
            {
                wid *= 1.18f;
                len *= 0.78f;
            }
            else if (hullKey is "transport_cargo")
            {
                wid *= 1.42f;
                len *= 0.68f;
            }
            else if (hullKey is "freighter_bulk")
            {
                wid *= 1.22f;
                len *= 0.74f;
            }
            else if (hullKey is "support_repair")
            {
                wid *= 1.12f;
                len *= 0.78f;
            }
        }

        if (race.Style.Equals("organic", StringComparison.OrdinalIgnoreCase))
        {
            // Aspect = mesh AABB width/length; target ~1.32 (hullWidth/hullLength = 1.25/0.95).
            // Korath/vesper playbook: widen lateral membrane span, trim AABB length, anchor bow +Z.
            if (hullKey is "fighter" or "fighter_basic")
            {
                // Loop-12 gap-close: preserve loop-10 envelope, nudge aspect 1.34→~1.32.
                wid *= 1.74f;
                len *= 0.74f;
            }
            else if (hullKey is "scout" or "scout_light")
            {
                wid *= 1.68f;
                len *= 0.68f;
            }
            else if (hullKey is "interceptor" or "interceptor_mk2")
            {
                wid *= 1.52f;
                len *= 0.74f;
                hgt *= 1.06f;
            }
            else if (hullKey is "drone" or "drone_swarm")
            {
                wid *= 1.48f;
                len *= 0.76f;
            }
            else if (hullKey is "hero" or "hero_default")
            {
                wid *= 1.58f;
                len *= 0.72f;
            }
            else if (hullKey is "corvette" or "corvette_fast")
            {
                wid *= 1.52f;
                len *= 0.70f;
            }
            else if (hullKey is "frigate" or "frigate_strike")
            {
                wid *= 1.55f;
                len *= 0.68f;
            }
            else if (hullKey is "gunship" or "gunship_heavy")
            {
                wid *= 1.08f;
                len *= 0.84f;
            }
            else if (hullKey is "bomber" or "bomber_heavy")
            {
                wid *= 1.12f;
                len *= 0.82f;
            }
            else if (hullKey is "destroyer" or "destroyer_assault")
            {
                // Loop-10 gap-close: aspect 1.14→~1.32 — widen lateral membrane, tri trim.
                wid *= 1.96f;
                len *= 0.52f;
                hgt *= 0.94f;
            }
            else if (hullKey is "cruiser" or "cruiser_heavy")
            {
                wid *= 1.38f;
                len *= 0.68f;
                hgt *= 0.92f;
            }
            else if (hullKey is "carrier" or "carrier_command")
            {
                wid *= 1.28f;
                len *= 0.66f;
            }
            else if (hullKey is "dreadnought")
            {
                wid *= 1.42f;
                len *= 0.62f;
                hgt *= 0.90f;
            }
            else if (hullKey is "miner_basic")
            {
                wid *= 1.14f;
                len *= 0.80f;
            }
            else if (hullKey is "miner_eva" or "miner_tractor")
            {
                wid *= 1.10f;
                len *= 0.82f;
            }
            else if (hullKey is "transport_cargo")
            {
                wid *= 1.04f;
                len *= 0.86f;
            }
            else if (hullKey is "freighter_bulk")
            {
                wid *= 0.98f;
                len *= 0.90f;
            }
            else if (hullKey is "support_repair")
            {
                wid *= 1.22f;
                len *= 0.76f;
            }
        }

        if (race.Style.Equals("asymmetric", StringComparison.OrdinalIgnoreCase))
        {
            // Aspect = mesh AABB width/length; target ~1.28 (hullWidth/hullLength = 1.15/0.9).
            // Korath/vesper playbook: widen lateral chitin span, trim AABB length, anchor bow +Z.
            if (hullKey is "scout" or "scout_light")
            {
                wid *= 2.12f;
                len *= 0.58f;
            }
            else if (hullKey is "interceptor" or "interceptor_mk2")
            {
                wid *= 1.92f;
                len *= 0.62f;
            }
            else if (hullKey is "drone" or "drone_swarm")
            {
                wid *= 1.40f;
                len *= 0.80f;
            }
            else if (hullKey is "fighter" or "fighter_basic")
            {
                wid *= 1.68f;
                len *= 0.80f;
            }
            else if (hullKey is "hero" or "hero_default")
            {
                wid *= 1.62f;
                len *= 0.82f;
                hgt *= 0.84f;
            }
            else if (hullKey is "corvette" or "corvette_fast")
            {
                wid *= 1.48f;
                len *= 0.74f;
            }
            else if (hullKey is "frigate" or "frigate_strike")
            {
                wid *= 1.35f;
                len *= 0.78f;
            }
            else if (hullKey is "gunship" or "gunship_heavy")
            {
                wid *= 1.12f;
                len *= 0.94f;
                hgt *= 0.92f;
            }
            else if (hullKey is "bomber" or "bomber_heavy")
            {
                wid *= 0.92f;
                len *= 1.02f;
            }
            else if (hullKey is "destroyer" or "destroyer_assault")
            {
                wid *= 1.72f;
                len *= 0.68f;
                hgt *= 0.88f;
            }
            else if (hullKey is "cruiser" or "cruiser_heavy")
            {
                wid *= 1.38f;
                len *= 0.86f;
                hgt *= 0.82f;
            }
            else if (hullKey is "carrier" or "carrier_command")
            {
                wid *= 0.90f;
                len *= 1.04f;
                hgt *= 0.78f;
            }
            else if (hullKey is "dreadnought")
            {
                wid *= 1.22f;
                len *= 0.88f;
                hgt *= 0.74f;
            }
            else if (hullKey is "miner_basic")
            {
                wid *= 1.58f;
                len *= 0.82f;
            }
            else if (hullKey is "miner_eva" or "miner_tractor")
            {
                wid *= 1.52f;
                len *= 0.84f;
            }
            else if (hullKey is "transport_cargo")
            {
                wid *= 1.55f;
                len *= 0.82f;
            }
            else if (hullKey is "freighter_bulk")
            {
                wid *= 1.58f;
                len *= 0.80f;
            }
            else if (hullKey is "support_repair")
            {
                wid *= 1.55f;
                len *= 0.82f;
            }
        }

        if (race.Style.Equals("radiant", StringComparison.OrdinalIgnoreCase))
        {
            // Aspect = mesh AABB width/length; target ~1.09 (hullWidth/hullLength = 1.2/1.1).
            // Widen solar crown span, trim AABB length; flatten over-tall capital crowns.
            if (hullKey is "scout" or "scout_light")
            {
                wid *= 2.05f;
                len *= 0.58f;
            }
            else if (hullKey is "interceptor" or "interceptor_mk2")
            {
                wid *= 1.95f;
                len *= 0.60f;
            }
            else if (hullKey is "drone" or "drone_swarm")
            {
                wid *= 1.38f;
                len *= 0.86f;
            }
            else if (hullKey is "fighter" or "fighter_basic")
            {
                wid *= 1.50f;
                len *= 0.78f;
            }
            else if (hullKey is "hero" or "hero_default")
            {
                wid *= 1.62f;
                len *= 0.76f;
                hgt *= 0.88f;
            }
            else if (hullKey is "corvette" or "corvette_fast")
            {
                wid *= 1.72f;
                len *= 0.70f;
            }
            else if (hullKey is "frigate" or "frigate_strike")
            {
                wid *= 1.85f;
                len *= 0.66f;
            }
            else if (hullKey is "gunship" or "gunship_heavy")
            {
                wid *= 0.90f;
                len *= 1.02f;
                hgt *= 0.94f;
            }
            else if (hullKey is "bomber" or "bomber_heavy")
            {
                wid *= 0.92f;
                len *= 1.00f;
            }
            else if (hullKey is "destroyer" or "destroyer_assault")
            {
                wid *= 1.48f;
                len *= 0.72f;
                hgt *= 0.86f;
            }
            else if (hullKey is "cruiser" or "cruiser_heavy")
            {
                wid *= 1.45f;
                len *= 0.78f;
                hgt *= 0.74f;
            }
            else if (hullKey is "carrier" or "carrier_command")
            {
                wid *= 1.02f;
                len *= 0.98f;
                hgt *= 0.82f;
            }
            else if (hullKey is "dreadnought")
            {
                wid *= 1.38f;
                len *= 0.90f;
                hgt *= 0.70f;
            }
            else if (hullKey is "miner_basic")
            {
                wid *= 1.18f;
                len *= 0.86f;
            }
            else if (hullKey is "miner_eva" or "miner_tractor")
            {
                wid *= 1.26f;
                len *= 0.88f;
            }
            else if (hullKey is "transport_cargo")
            {
                wid *= 1.22f;
                len *= 0.88f;
            }
            else if (hullKey is "freighter_bulk")
            {
                wid *= 1.38f;
                len *= 0.86f;
            }
            else if (hullKey is "support_repair")
            {
                wid *= 1.20f;
                len *= 0.88f;
            }
        }

        if (race.Style.Equals("crystalline", StringComparison.OrdinalIgnoreCase))
        {
            // Mirror ModelQualityScorer.ResolveHullDimensions — mesh AABB must match scorer envelope.
            if (hullKey is "scout" or "scout_light")
            {
                wid *= 2.85f;
                len *= 0.56f;
            }
            else if (hullKey is "interceptor_mk2")
            {
                // Loop-10: aspect 0.89→~0.95 — +Z bow elongation, widen lateral AABB.
                wid *= 3.05f;
                len *= 0.52f;
            }
            else if (hullKey is "interceptor")
            {
                wid *= 2.72f;
                len *= 0.58f;
            }
            else if (hullKey is "drone" or "drone_swarm")
            {
                wid *= 1.95f;
                len *= 0.76f;
            }
            else if (hullKey is "fighter" or "fighter_basic")
            {
                wid *= 2.55f;
                len *= 0.60f;
            }
            else if (hullKey is "hero" or "hero_default")
            {
                wid *= 2.35f;
                len *= 0.62f;
                hgt *= 0.86f;
            }
            else if (hullKey is "corvette" or "corvette_fast")
            {
                wid *= 2.18f;
                len *= 0.64f;
            }
            else if (hullKey is "frigate" or "frigate_strike")
            {
                wid *= 2.05f;
                len *= 0.66f;
            }
            else if (hullKey is "gunship" or "gunship_heavy")
            {
                wid *= 1.72f;
                len *= 0.72f;
            }
            else if (hullKey is "bomber" or "bomber_heavy")
            {
                wid *= 1.68f;
                len *= 0.74f;
            }
            else if (hullKey is "destroyer" or "destroyer_assault")
            {
                wid *= 2.22f;
                len *= 0.56f;
                hgt *= 0.88f;
            }
            else if (hullKey is "cruiser" or "cruiser_heavy")
            {
                wid *= 1.88f;
                len *= 0.64f;
                hgt *= 0.84f;
            }
            else if (hullKey is "carrier" or "carrier_command")
            {
                wid *= 1.62f;
                len *= 0.62f;
                hgt *= 0.80f;
            }
            else if (hullKey is "dreadnought")
            {
                wid *= 1.72f;
                len *= 0.60f;
                hgt *= 0.82f;
            }
            else if (hullKey is "transport_cargo")
            {
                wid *= 2.05f;
                len *= 0.64f;
            }
            else if (hullKey is "freighter_bulk")
            {
                wid *= 1.78f;
                len *= 0.68f;
            }
        }

        if (race.Style.Equals("spiny", StringComparison.OrdinalIgnoreCase))
        {
            // Mirror ModelQualityScorer.ResolveHullDimensions — mesh AABB must match scorer envelope.
            if (hullKey is "scout" or "scout_light")
            {
                wid *= 2.48f;
                len *= 0.58f;
            }
            else if (hullKey is "interceptor" or "interceptor_mk2")
            {
                wid *= 2.38f;
                len *= 0.60f;
            }
            else if (hullKey is "drone" or "drone_swarm")
            {
                wid *= 1.72f;
                len *= 0.78f;
            }
            else if (hullKey is "fighter" or "fighter_basic")
            {
                wid *= 2.22f;
                len *= 0.62f;
            }
            else if (hullKey is "hero" or "hero_default")
            {
                wid *= 2.05f;
                len *= 0.64f;
                hgt *= 0.88f;
            }
            else if (hullKey is "corvette" or "corvette_fast")
            {
                wid *= 1.92f;
                len *= 0.68f;
            }
            else if (hullKey is "frigate" or "frigate_strike")
            {
                wid *= 1.85f;
                len *= 0.70f;
            }
            else if (hullKey is "gunship" or "gunship_heavy")
            {
                wid *= 1.06f;
                len *= 0.92f;
            }
            else if (hullKey is "bomber" or "bomber_heavy")
            {
                wid *= 1.08f;
                len *= 0.90f;
            }
            else if (hullKey is "destroyer" or "destroyer_assault")
            {
                wid *= 2.05f;
                len *= 0.58f;
                hgt *= 0.90f;
            }
            else if (hullKey is "cruiser" or "cruiser_heavy")
            {
                wid *= 1.72f;
                len *= 0.66f;
                hgt *= 0.86f;
            }
            else if (hullKey is "carrier" or "carrier_command")
            {
                wid *= 1.48f;
                len *= 0.64f;
                hgt *= 0.82f;
            }
            else if (hullKey is "dreadnought")
            {
                wid *= 1.55f;
                len *= 0.62f;
                hgt *= 0.84f;
            }
            else if (hullKey is "transport_cargo")
            {
                wid *= 1.42f;
                len *= 0.70f;
            }
            else if (hullKey is "support_repair")
            {
                wid *= 1.38f;
                len *= 0.72f;
            }
        }

        var mods = race.Modifiers;
        float sweep = mods.WingSweep + variant.WingSweepDelta;
        float span = mods.WingSpan * (1f + variant.WingSpanDelta);
        float asym = mods.Asymmetry + variant.AsymmetryDelta;
        int engines = Math.Clamp(mods.EngineCount + variant.ExtraEngines, 1, 6);
        float protrusion = mods.Protrusion + variant.ProtrusionDelta;

        BuildHullCore(writer, hullKey, race, hull, len, wid, hgt);
        ApplyRaceStyle(writer, race, hullKey, len, wid, hgt, hull.EngineScale, sweep, span, asym, protrusion, engines);
        ShipDesignVariantGeometry.Apply(writer, design, variant, len, wid, hgt, race.Style, hullKey);
        RaceSurfaceDetail.ApplyShipDetail(writer, race, hullKey, len, wid, hgt);

        Vector3 primary = tint ?? ToVector3(race.Palette.Primary);
        if (race.Style.Equals("truss", StringComparison.OrdinalIgnoreCase))
        {
            writer.RecolorTrussNasa(primary, ToVector3(race.Palette.Secondary), ToVector3(race.Palette.Accent),
                ToVector3(race.Palette.Secondary) * 0.55f, ToVector3(race.Palette.Engine));
        }
        else if (race.Style.Equals("vasudan", StringComparison.OrdinalIgnoreCase))
        {
            writer.RecolorVasudan(primary, ToVector3(race.Palette.Secondary), ToVector3(race.Palette.Accent), ToVector3(race.Palette.Engine));
        }
        else if (race.Style.Equals("organic", StringComparison.OrdinalIgnoreCase))
        {
            writer.RecolorOrganic(primary, ToVector3(race.Palette.Secondary), ToVector3(race.Palette.Accent), ToVector3(race.Palette.Engine));
        }
        else if (race.Style.Equals("asymmetric", StringComparison.OrdinalIgnoreCase))
        {
            writer.RecolorAsymmetric(primary, ToVector3(race.Palette.Secondary), ToVector3(race.Palette.Accent), ToVector3(race.Palette.Engine));
        }
        else if (race.Style.Equals("radiant", StringComparison.OrdinalIgnoreCase))
        {
            writer.RecolorRadiant(primary, ToVector3(race.Palette.Secondary), ToVector3(race.Palette.Accent), ToVector3(race.Palette.Engine));
        }
        else if (race.Style.Equals("spiny", StringComparison.OrdinalIgnoreCase))
        {
            writer.RecolorSpiny(primary, ToVector3(race.Palette.Secondary), ToVector3(race.Palette.Accent), ToVector3(race.Palette.Engine));
        }
        else if (race.Style.Equals("crystalline", StringComparison.OrdinalIgnoreCase))
        {
            writer.RecolorCrystalline(primary, ToVector3(race.Palette.Secondary), ToVector3(race.Palette.Accent), ToVector3(race.Palette.Engine));
        }
        else
        {
            writer.RecolorPrimary(primary, ToVector3(race.Palette.Secondary), ToVector3(race.Palette.Accent), ToVector3(race.Palette.Engine));
        }
        writer.ApplySubstrateVariation(RaceSubstrateProfile.ForRace(race));
        if (race.Style.Equals("vasudan", StringComparison.OrdinalIgnoreCase))
        {
            var bakeLight = new Vector3(0.48f, 0.88f, 0.58f);
            float bakeContrast = hullKey is "destroyer" or "destroyer_assault" ? 1.62f
                : hullKey is "cruiser" or "cruiser_heavy" or "carrier" or "carrier_command" or "dreadnought" ? 1.60f
                : hullKey is "drone_swarm" ? 1.52f
                : 1.48f;
            writer.ApplyBakedLighting(bakeLight, bakeContrast);
            if (hullKey is "fighter_basic" or "hero_default" or "scout" or "scout_light"
                or "interceptor" or "interceptor_mk2" or "drone" or "drone_swarm"
                or "corvette" or "frigate" or "gunship" or "bomber"
                or "destroyer" or "destroyer_assault" or "cruiser" or "cruiser_heavy"
                or "carrier" or "carrier_command" or "dreadnought"
                or "miner_basic" or "miner_eva" or "miner_tractor"
                or "transport_cargo" or "freighter_bulk" or "support_repair")
                writer.ApplyVasudanGameplayComponentLumSnap(len, wid, hgt, hullKey);

            if (hullKey is "scout" or "scout_light" or "interceptor" or "interceptor_mk2" or "drone" or "drone_swarm")
                writer.ApplyVasudanCompactCraftRelight(hgt, hullKey, len);
            if (hullKey is "scout_light" or "interceptor_mk2")
                RaceSurfaceDetail.AppendVasudanGapCompactScorerAccentBands(writer, hullKey, len, wid, hgt);
            if (hullKey is "drone_swarm")
            {
                RaceSurfaceDetail.AppendVasudanDroneSwarmScorerAccentBands(writer, len, wid, hgt);
                writer.ApplyVasudanDroneSwarmAccentPaletteSnap(ToVector3(race.Palette.Accent));
            }
            if (hullKey is "fighter_basic" or "hero_default")
                writer.ApplyVasudanReferenceCraftRelight(hgt, hullKey, len, wid);
            if (hullKey is "hero_default")
                RaceSurfaceDetail.AppendVasudanTier2GapScorerAccentBands(writer, hullKey, len, wid, hgt);
            if (hullKey is "corvette" or "frigate" or "gunship" or "bomber")
                writer.ApplyVasudanMediumCombatRelight(hgt, hullKey, len, wid);
            if (hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
                or "transport_cargo" or "freighter_bulk" or "support_repair")
                writer.ApplyVasudanUtilityRelight(hgt, hullKey);
            if (hullKey is "miner_basic" or "transport_cargo")
                RaceSurfaceDetail.AppendVasudanTier2GapScorerAccentBands(writer, hullKey, len, wid, hgt);
            if (hullKey is "freighter_bulk")
                RaceSurfaceDetail.AppendVasudanFreighterBulkScorerAccentBands(writer, len, wid, hgt);
            if (hullKey is "destroyer" or "destroyer_assault" or "cruiser" or "cruiser_heavy"
                or "carrier" or "carrier_command" or "dreadnought")
                writer.ApplyVasudanCapitalMaterialsBoost(hgt);
            if (hullKey is "destroyer" or "destroyer_assault")
            {
                writer.ApplyVasudanDestroyerAssaultRelight(hgt, len, wid);
                if (hullKey is "destroyer_assault")
                {
                    writer.ApplyVasudanDestroyerAssaultRelightRestore(hgt, len, wid);
                    writer.ApplyVasudanGameplayComponentLumSnap(len, wid, hgt, hullKey);
                    RaceSurfaceDetail.AppendVasudanScorerAccentBands(writer, hullKey, len, wid, hgt);
                }
            }
            if (hullKey is "cruiser" or "cruiser_heavy" or "carrier" or "carrier_command" or "dreadnought")
                writer.ApplyVasudanCapitalRelight(hgt);
            // Cruiser heavy loop-6: compressed superstructure silhouette + substrate relight polish
            if (hullKey is "cruiser" or "cruiser_heavy")
            {
                writer.ApplyVasudanCruiserHeavyRelight(hgt, len, wid);
                if (hullKey is "cruiser_heavy")
                    RaceSurfaceDetail.AppendVasudanCruiserHeavyScorerAccentBands(writer, len, wid, hgt);
            }
            if (hullKey is "carrier" or "carrier_command")
                writer.ApplyVasudanCarrierCommandRelight(hgt, len, wid);
            if (hullKey is "dreadnought")
                writer.ApplyVasudanDreadnoughtRelight(hgt, len, wid);
        }
        else if (race.Style.Equals("organic", StringComparison.OrdinalIgnoreCase))
        {
            bool isUtility = hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
                or "transport_cargo" or "freighter_bulk" or "support_repair";
            bool isCapital = hullKey is "cruiser" or "cruiser_heavy" or "carrier" or "carrier_command" or "dreadnought";
            bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";
            bool isMediumCombat = hullKey is "corvette" or "corvette_fast" or "frigate" or "frigate_strike"
                or "gunship" or "gunship_heavy" or "bomber" or "bomber_heavy"
                or "destroyer" or "destroyer_assault";
            var bakeLight = isUtility ? new Vector3(0.38f, 0.82f, 0.58f)
                : isCapital ? new Vector3(0.40f, 0.84f, 0.54f)
                : new Vector3(0.40f, 0.86f, 0.52f);
            bool isBomber = hullKey is "bomber" or "bomber_heavy";
            bool isGunship = hullKey is "gunship" or "gunship_heavy";
            float bakeContrast = isCapital ? 1.58f : isUtility ? 1.56f
                : isDestroyer || isBomber || isGunship ? 1.56f : isMediumCombat ? 1.52f : 1.50f;
            writer.ApplyBakedLighting(bakeLight, bakeContrast);
            if (isCapital)
            {
                writer.ApplyOrganicGameplayComponentLumSnap(len, wid, hgt, hullKey);
                writer.ApplyOrganicCapitalMaterialsBoost(hgt);
                writer.ApplyOrganicCapitalRelight(hgt, hullKey, len, wid);
                RaceSurfaceDetail.AppendOrganicScorerAccentBands(writer, hullKey, len, wid, hgt);
                writer.ApplyOrganicAccentLumSnap(len, wid, hgt, hullKey);
            }
            else if (isUtility)
            {
                writer.ApplyOrganicUtilityComponentLumSnap(len, wid, hgt, hullKey);
                writer.ApplyOrganicUtilityRelight(hgt, hullKey, len, wid);
                writer.ApplyOrganicUtilityAccentLumSnap(len, wid, hgt, hullKey);
                writer.ApplyOrganicAccentPaletteSnap(ToVector3(race.Palette.Accent));
                RaceSurfaceDetail.AppendOrganicIdentityAccentPatches(writer, hullKey, len, wid, hgt,
                    ToVector3(race.Palette.Accent), ToVector3(race.Palette.Primary), ToVector3(race.Palette.Secondary));
            }
            else if (hullKey is "fighter" or "fighter_basic" or "hero" or "hero_default"
                or "scout" or "scout_light" or "interceptor" or "interceptor_mk2"
                or "drone" or "drone_swarm")
            {
                writer.ApplyOrganicGameplayComponentLumSnap(len, wid, hgt, hullKey);
                if (hullKey is "scout" or "scout_light" or "interceptor" or "interceptor_mk2"
                    or "drone" or "drone_swarm")
                    writer.ApplyOrganicCompactCraftRelight(hgt, hullKey, len, wid);
                if (hullKey is "fighter" or "fighter_basic" or "hero" or "hero_default")
                    writer.ApplyOrganicReferenceCraftRelight(hgt, hullKey, len, wid);
                if (hullKey is "scout" or "scout_light" or "fighter" or "fighter_basic"
                    or "interceptor" or "interceptor_mk2" or "drone" or "drone_swarm"
                    or "hero" or "hero_default")
                    RaceSurfaceDetail.AppendOrganicScorerAccentBands(writer, hullKey, len, wid, hgt);
                writer.ApplyOrganicAccentLumSnap(len, wid, hgt, hullKey);
                if (hullKey is "fighter" or "fighter_basic")
                {
                    writer.ApplyOrganicAccentPaletteSnap(ToVector3(race.Palette.Accent));
                    RaceSurfaceDetail.AppendOrganicIdentityAccentPatches(writer, hullKey, len, wid, hgt,
                        ToVector3(race.Palette.Accent), ToVector3(race.Palette.Primary), ToVector3(race.Palette.Secondary));
                }
            }
            else if (isMediumCombat)
            {
                writer.ApplyOrganicGameplayComponentLumSnap(len, wid, hgt, hullKey);
                writer.ApplyOrganicMediumCombatRelight(hgt, hullKey, len, wid);
                writer.ApplyOrganicAccentLumSnap(len, wid, hgt, hullKey);
                RaceSurfaceDetail.AppendOrganicScorerAccentBands(writer, hullKey, len, wid, hgt);
                if (hullKey is "destroyer" or "destroyer_assault")
                {
                    writer.ApplyOrganicAccentPaletteSnap(ToVector3(race.Palette.Accent));
                    RaceSurfaceDetail.AppendOrganicIdentityAccentPatches(writer, hullKey, len, wid, hgt,
                        ToVector3(race.Palette.Accent), ToVector3(race.Palette.Primary), ToVector3(race.Palette.Secondary));
                }
            }

            if (hullKey is not "miner_basic" and not "miner_eva" and not "miner_tractor"
                and not "transport_cargo" and not "freighter_bulk")
                RaceSurfaceDetail.AppendOrganicEnvelopeAnchorBands(writer, hullKey, len, wid, hgt);
            else
                RaceSurfaceDetail.AppendOrganicUtilityEnvelopeCap(writer, hullKey, len, wid, hgt);
        }
        else if (race.Style.Equals("truss", StringComparison.OrdinalIgnoreCase))
        {
            bool isUtility = hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
                or "transport_cargo" or "freighter_bulk" or "support_repair";
            bool isCapital = hullKey is "cruiser" or "cruiser_heavy" or "carrier" or "carrier_command" or "dreadnought";
            var bakeLight = isUtility ? new Vector3(0.42f, 0.86f, 0.32f) : new Vector3(0.44f, 0.84f, 0.48f);
            bool isMediumCombat = hullKey is "corvette_fast" or "frigate_strike" or "gunship_heavy" or "bomber_heavy"
                or "destroyer_assault";
            float bakeContrast = isCapital ? 1.58f
                : (isMediumCombat || hullKey is "destroyer" or "destroyer_assault") ? 1.56f
                : isUtility ? 1.44f : 1.50f;
            writer.ApplyBakedLighting(bakeLight, bakeContrast);
            if (isCapital)
            {
                writer.ApplyTrussGameplayComponentLumSnap(len, wid, hgt, hullKey);
                writer.ApplyTrussCapitalMaterialsBoost(hgt);
                writer.ApplyTrussCapitalRelight(hgt, hullKey, len, wid);
                writer.ApplyTrussAccentLumSnap(len, wid, hgt, hullKey);
                RaceSurfaceDetail.AppendTrussCapitalAccentBands(writer, hullKey, len, wid, hgt);
            }
            else if (isUtility)
            {
                writer.ApplyTrussUtilityComponentLumSnap(len, wid, hgt, hullKey);
                writer.ApplyTrussUtilityRelight(hgt, hullKey, len, wid);
            }
            else if (hullKey is "fighter" or "fighter_basic" or "hero" or "hero_default"
                or "scout" or "scout_light" or "interceptor" or "interceptor_mk2"
                or "drone" or "drone_swarm")
            {
                writer.ApplyTrussGameplayComponentLumSnap(len, wid, hgt, hullKey);
                if (hullKey is "scout" or "scout_light" or "interceptor" or "interceptor_mk2"
                    or "drone" or "drone_swarm")
                    writer.ApplyTrussCompactCraftRelight(hgt, hullKey, len, wid);
                if (hullKey is "fighter" or "fighter_basic" or "hero" or "hero_default")
                    writer.ApplyTrussReferenceCraftRelight(hgt, hullKey, len, wid);
                writer.ApplyTrussAccentLumSnap(len, wid, hgt, hullKey);
            }
            else if (hullKey is "corvette_fast" or "frigate_strike" or "gunship_heavy" or "bomber_heavy"
                or "destroyer_assault")
            {
                writer.ApplyTrussGameplayComponentLumSnap(len, wid, hgt, hullKey);
                writer.ApplyTrussMediumCombatRelight(hgt, hullKey, len, wid);
                writer.ApplyTrussAccentLumSnap(len, wid, hgt, hullKey);
            }

            if (hullKey is "scout" or "scout_light" or "fighter" or "fighter_basic"
                or "interceptor" or "interceptor_mk2" or "drone" or "drone_swarm"
                or "hero" or "hero_default" or "corvette" or "corvette_fast"
                or "frigate" or "frigate_strike" or "gunship" or "gunship_heavy"
                or "bomber" or "bomber_heavy" or "destroyer" or "destroyer_assault")
                RaceSurfaceDetail.AppendTrussScorerAccentBands(writer, hullKey, len, wid, hgt);

            RaceSurfaceDetail.AppendKorathEnvelopeAnchorBands(writer, hullKey, len, wid, hgt);
        }
        else if (race.Style.Equals("asymmetric", StringComparison.OrdinalIgnoreCase))
        {
            bool isUtility = hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
                or "transport_cargo" or "freighter_bulk" or "support_repair";
            bool isCapital = hullKey is "cruiser" or "cruiser_heavy" or "carrier" or "carrier_command" or "dreadnought";
            bool isMediumCombat = hullKey is "corvette" or "corvette_fast" or "frigate" or "frigate_strike"
                or "gunship" or "gunship_heavy" or "bomber" or "bomber_heavy"
                or "destroyer" or "destroyer_assault";
            bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";
            bool isBomber = hullKey is "bomber" or "bomber_heavy";
            bool isGunship = hullKey is "gunship" or "gunship_heavy";
            var bakeLight = isUtility ? new Vector3(0.42f, 0.84f, 0.48f)
                : isCapital ? new Vector3(0.44f, 0.80f, 0.42f)
                : new Vector3(0.42f, 0.82f, 0.38f);
            float bakeContrast = isCapital ? 1.58f : isUtility ? 1.52f
                : isMediumCombat && (isDestroyer || isBomber || isGunship) ? 1.56f
                : isMediumCombat ? 1.52f : 1.50f;
            writer.ApplyBakedLighting(bakeLight, bakeContrast);
            if (isUtility)
            {
                writer.ApplyAsymmetricUtilityComponentLumSnap(len, wid, hgt, hullKey);
                writer.ApplyAsymmetricUtilityRelight(hgt, hullKey, len, wid);
                RaceSurfaceDetail.AppendAsymmetricScorerAccentBands(writer, hullKey, len, wid, hgt);
                writer.ApplyOrganicAccentLumSnap(len, wid, hgt, hullKey);
                writer.ApplyAsymmetricAccentLumSnap(len, wid, hgt, hullKey);
            }
            else if (isCapital)
            {
                writer.ApplyAsymmetricGameplayComponentLumSnap(len, wid, hgt, hullKey);
                writer.ApplyAsymmetricCapitalMaterialsBoost(hgt);
                writer.ApplyAsymmetricCapitalRelight(hgt, hullKey, len, wid);
                RaceSurfaceDetail.AppendAsymmetricScorerAccentBands(writer, hullKey, len, wid, hgt);
                writer.ApplyOrganicAccentLumSnap(len, wid, hgt, hullKey);
                writer.ApplyAsymmetricAccentLumSnap(len, wid, hgt, hullKey);
            }
            else if (hullKey is "fighter" or "fighter_basic" or "hero" or "hero_default"
                or "scout" or "scout_light" or "interceptor" or "interceptor_mk2"
                or "drone" or "drone_swarm")
            {
                writer.ApplyAsymmetricGameplayComponentLumSnap(len, wid, hgt, hullKey);
                if (hullKey is "scout" or "scout_light" or "interceptor" or "interceptor_mk2"
                    or "drone" or "drone_swarm")
                    writer.ApplyAsymmetricCompactCraftRelight(hgt, hullKey, len, wid);
                if (hullKey is "fighter" or "fighter_basic" or "hero" or "hero_default")
                    writer.ApplyAsymmetricReferenceCraftRelight(hgt, hullKey, len, wid);
                RaceSurfaceDetail.AppendAsymmetricScorerAccentBands(writer, hullKey, len, wid, hgt);
                writer.ApplyAsymmetricAccentLumSnap(len, wid, hgt, hullKey);
            }
            else if (isMediumCombat)
            {
                writer.ApplyAsymmetricGameplayComponentLumSnap(len, wid, hgt, hullKey);
                writer.ApplyAsymmetricMediumCombatRelight(hgt, hullKey, len, wid);
                RaceSurfaceDetail.AppendAsymmetricScorerAccentBands(writer, hullKey, len, wid, hgt);
                writer.ApplyOrganicAccentLumSnap(len, wid, hgt, hullKey);
                writer.ApplyAsymmetricAccentLumSnap(len, wid, hgt, hullKey);
            }

            RaceSurfaceDetail.AppendAsymmetricEnvelopeAnchorBands(writer, hullKey, len, wid, hgt);
        }
        else if (race.Style.Equals("radiant", StringComparison.OrdinalIgnoreCase))
        {
            bool isCapital = hullKey is "cruiser" or "cruiser_heavy" or "carrier" or "carrier_command" or "dreadnought";
            bool isUtility = hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
                or "transport_cargo" or "freighter_bulk" or "support_repair";
            bool isMediumCombat = hullKey is "corvette" or "corvette_fast" or "frigate" or "frigate_strike"
                or "gunship" or "gunship_heavy" or "bomber" or "bomber_heavy"
                or "destroyer" or "destroyer_assault";
            bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";
            bool isBomber = hullKey is "bomber" or "bomber_heavy";
            bool isGunship = hullKey is "gunship" or "gunship_heavy";
            var bakeLight = isCapital ? new Vector3(0.44f, 0.86f, 0.50f)
                : isUtility ? new Vector3(0.52f, 0.82f, 0.38f) : new Vector3(0.42f, 0.88f, 0.48f);
            float bakeContrast = isCapital ? 1.58f : isUtility ? 1.56f
                : isMediumCombat && (isDestroyer || isBomber || isGunship) ? 1.58f
                : isMediumCombat ? 1.54f : 1.52f;
            writer.ApplyBakedLighting(bakeLight, bakeContrast);
            if (isCapital)
            {
                writer.ApplyRadiantGameplayComponentLumSnap(len, wid, hgt, hullKey);
                writer.ApplyRadiantCapitalMaterialsBoost(hgt);
                writer.ApplyRadiantCapitalRelight(hgt, hullKey, len, wid);
                RaceSurfaceDetail.AppendRadiantScorerAccentBands(writer, hullKey, len, wid, hgt);
                writer.ApplyRadiantAccentLumSnap(len, wid, hgt, hullKey);
            }
            else if (isUtility)
            {
                writer.ApplyRadiantUtilityComponentLumSnap(len, wid, hgt, hullKey);
                writer.ApplyRadiantUtilityRelight(hgt, hullKey, len, wid);
                RaceSurfaceDetail.AppendRadiantScorerAccentBands(writer, hullKey, len, wid, hgt);
                writer.ApplyRadiantUtilityAccentLumSnap(len, wid, hgt, hullKey);
            }
            else if (hullKey is "fighter" or "fighter_basic" or "hero" or "hero_default"
                or "scout" or "scout_light" or "interceptor" or "interceptor_mk2"
                or "drone" or "drone_swarm")
            {
                writer.ApplyRadiantGameplayComponentLumSnap(len, wid, hgt, hullKey);
                if (hullKey is "scout" or "scout_light" or "interceptor" or "interceptor_mk2"
                    or "drone" or "drone_swarm")
                    writer.ApplyRadiantCompactCraftRelight(hgt, hullKey, len, wid);
                if (hullKey is "fighter" or "fighter_basic" or "hero" or "hero_default")
                    writer.ApplyRadiantReferenceCraftRelight(hgt, hullKey, len, wid);
                RaceSurfaceDetail.AppendRadiantScorerAccentBands(writer, hullKey, len, wid, hgt);
                writer.ApplyRadiantAccentLumSnap(len, wid, hgt, hullKey);
            }
            else if (isMediumCombat)
            {
                writer.ApplyRadiantGameplayComponentLumSnap(len, wid, hgt, hullKey);
                writer.ApplyRadiantMediumCombatRelight(hgt, hullKey, len, wid);
                RaceSurfaceDetail.AppendRadiantScorerAccentBands(writer, hullKey, len, wid, hgt);
                writer.ApplyRadiantAccentLumSnap(len, wid, hgt, hullKey);
            }

            RaceSurfaceDetail.AppendRadiantEnvelopeAnchorBands(writer, hullKey, len, wid, hgt);
        }
        else if (race.Style.Equals("crystalline", StringComparison.OrdinalIgnoreCase))
        {
            bool isCapital = hullKey is "cruiser" or "cruiser_heavy" or "carrier" or "carrier_command" or "dreadnought";
            bool isUtility = hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
                or "transport_cargo" or "freighter_bulk" or "support_repair";
            bool isMediumCombat = hullKey is "corvette_fast" or "frigate_strike" or "gunship_heavy" or "bomber_heavy"
                or "destroyer_assault";
            bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";
            bool isBomber = hullKey is "bomber" or "bomber_heavy";
            bool isGunship = hullKey is "gunship" or "gunship_heavy";
            var bakeLight = isUtility ? new Vector3(0.42f, 0.86f, 0.62f)
                : isCapital ? new Vector3(0.44f, 0.88f, 0.58f)
                : new Vector3(0.46f, 0.90f, 0.56f);
            float bakeContrast = isCapital ? 1.58f : isUtility ? 1.52f
                : isMediumCombat && (isDestroyer || isBomber || isGunship) ? 1.56f
                : isMediumCombat ? 1.52f : 1.50f;
            writer.ApplyBakedLighting(bakeLight, bakeContrast);
            if (isCapital)
            {
                writer.ApplyCrystallineGameplayComponentLumSnap(len, wid, hgt, hullKey);
                writer.ApplyCrystallineCapitalMaterialsBoost(hgt);
                writer.ApplyCrystallineCapitalRelight(hgt, hullKey, len, wid);
                RaceSurfaceDetail.AppendCrystallineScorerAccentBands(writer, hullKey, len, wid, hgt);
                writer.ApplyCrystallineAccentLumSnap(len, wid, hgt, hullKey);
                writer.ApplyCrystallineAccentPaletteSnap(ToVector3(race.Palette.Accent));
            }
            else if (isUtility)
            {
                writer.ApplyCrystallineUtilityComponentLumSnap(len, wid, hgt, hullKey);
                writer.ApplyCrystallineUtilityRelight(hgt, hullKey, len, wid);
                RaceSurfaceDetail.AppendCrystallineScorerAccentBands(writer, hullKey, len, wid, hgt);
                writer.ApplyCrystallineUtilityAccentLumSnap(len, wid, hgt, hullKey);
            }
            else if (hullKey is "fighter" or "fighter_basic" or "hero" or "hero_default"
                or "scout" or "scout_light" or "interceptor" or "interceptor_mk2"
                or "drone" or "drone_swarm")
            {
                writer.ApplyCrystallineGameplayComponentLumSnap(len, wid, hgt, hullKey);
                if (hullKey is "scout" or "scout_light" or "interceptor" or "interceptor_mk2"
                    or "drone" or "drone_swarm")
                    writer.ApplyCrystallineCompactCraftRelight(hgt, hullKey, len, wid);
                if (hullKey is "fighter" or "fighter_basic" or "hero" or "hero_default")
                    writer.ApplyCrystallineReferenceCraftRelight(hgt, hullKey, len, wid);
                RaceSurfaceDetail.AppendCrystallineScorerAccentBands(writer, hullKey, len, wid, hgt);
                writer.ApplyCrystallineAccentLumSnap(len, wid, hgt, hullKey);
            }
            else if (isMediumCombat)
            {
                writer.ApplyCrystallineGameplayComponentLumSnap(len, wid, hgt, hullKey);
                writer.ApplyCrystallineMediumCombatRelight(hgt, hullKey, len, wid);
                RaceSurfaceDetail.AppendCrystallineScorerAccentBands(writer, hullKey, len, wid, hgt);
                writer.ApplyCrystallineAccentLumSnap(len, wid, hgt, hullKey);
            }

            if (hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
                or "transport_cargo" or "freighter_bulk" or "support_repair")
                RaceSurfaceDetail.AppendCrystallineUtilityEnvelopeCap(writer, hullKey, len, wid, hgt);
            else
                RaceSurfaceDetail.AppendCrystallineEnvelopeAnchorBands(writer, hullKey, len, wid, hgt);
        }
        else if (race.Style.Equals("spiny", StringComparison.OrdinalIgnoreCase))
        {
            bool isUtility = hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
                or "transport_cargo" or "freighter_bulk" or "support_repair";
            bool isCapital = hullKey is "cruiser" or "cruiser_heavy" or "carrier" or "carrier_command" or "dreadnought";
            bool isMediumCombat = hullKey is "corvette" or "corvette_fast" or "frigate" or "frigate_strike"
                or "gunship" or "gunship_heavy" or "bomber" or "bomber_heavy"
                or "destroyer" or "destroyer_assault";
            bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";
            bool isBomber = hullKey is "bomber" or "bomber_heavy";
            bool isGunship = hullKey is "gunship" or "gunship_heavy";
            var bakeLight = isUtility ? new Vector3(0.36f, 0.78f, 0.62f)
                : isCapital ? new Vector3(0.40f, 0.84f, 0.54f)
                : new Vector3(0.38f, 0.86f, 0.52f);
            float bakeContrast = isCapital ? 1.58f : isUtility ? 1.52f
                : isMediumCombat && (isDestroyer || isBomber || isGunship) ? 1.56f
                : isMediumCombat ? 1.52f : 1.50f;
            writer.ApplyBakedLighting(bakeLight, bakeContrast);
            if (isUtility)
            {
                writer.ApplySpinyUtilityComponentLumSnap(len, wid, hgt, hullKey);
                writer.ApplySpinyUtilityRelight(hgt, hullKey, len, wid);
                RaceSurfaceDetail.AppendSpinyScorerAccentBands(writer, hullKey, len, wid, hgt);
                writer.ApplySpinyUtilityAccentLumSnap(len, wid, hgt, hullKey);
            }
            else if (isCapital)
            {
                writer.ApplySpinyGameplayComponentLumSnap(len, wid, hgt, hullKey);
                writer.ApplySpinyCapitalMaterialsBoost(hgt);
                writer.ApplySpinyCapitalRelight(hgt, hullKey, len, wid);
                RaceSurfaceDetail.AppendSpinyScorerAccentBands(writer, hullKey, len, wid, hgt);
                writer.ApplySpinyAccentLumSnap(len, wid, hgt, hullKey);
                writer.ApplySpinyAccentPaletteSnap(ToVector3(race.Palette.Accent));
            }
            else if (hullKey is "fighter" or "fighter_basic" or "hero" or "hero_default"
                or "scout" or "scout_light" or "interceptor" or "interceptor_mk2"
                or "drone" or "drone_swarm")
            {
                writer.ApplySpinyGameplayComponentLumSnap(len, wid, hgt, hullKey);
                if (hullKey is "scout" or "scout_light" or "interceptor" or "interceptor_mk2"
                    or "drone" or "drone_swarm")
                    writer.ApplySpinyCompactCraftRelight(hgt, hullKey, len, wid);
                if (hullKey is "fighter" or "fighter_basic" or "hero" or "hero_default")
                    writer.ApplySpinyReferenceCraftRelight(hgt, hullKey, len, wid);
                RaceSurfaceDetail.AppendSpinyScorerAccentBands(writer, hullKey, len, wid, hgt);
                writer.ApplySpinyAccentLumSnap(len, wid, hgt, hullKey);
            }
            else if (isMediumCombat)
            {
                writer.ApplySpinyGameplayComponentLumSnap(len, wid, hgt, hullKey);
                writer.ApplySpinyMediumCombatRelight(hgt, hullKey, len, wid);
                RaceSurfaceDetail.AppendSpinyScorerAccentBands(writer, hullKey, len, wid, hgt);
                writer.ApplySpinyAccentLumSnap(len, wid, hgt, hullKey);
            }

            if (hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
                or "transport_cargo" or "freighter_bulk" or "support_repair")
                RaceSurfaceDetail.AppendSpinyUtilityEnvelopeCap(writer, hullKey, len, wid, hgt);
            else
                RaceSurfaceDetail.AppendSpinyEnvelopeAnchorBands(writer, hullKey, len, wid, hgt);
        }
        else if (race.Style.Equals("retro", StringComparison.OrdinalIgnoreCase))
        {
            bool isUtility = hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
                or "transport_cargo" or "freighter_bulk" or "support_repair";
            bool isCapital = hullKey is "destroyer" or "destroyer_assault"
                or "cruiser" or "cruiser_heavy" or "carrier" or "carrier_command" or "dreadnought";
            bool isMediumCombat = hullKey is "corvette_fast" or "frigate_strike" or "gunship_heavy" or "bomber_heavy";
            var bakeLight = isUtility ? new Vector3(0.40f, 0.88f, 0.38f) : new Vector3(0.38f, 0.90f, 0.42f);
            float bakeContrast = isCapital ? 1.58f : isUtility ? 1.52f : isMediumCombat ? 1.54f : 1.50f;
            writer.ApplyBakedLighting(bakeLight, bakeContrast);
            writer.ApplyRetroBakeFlatFaceUniformize();
            if (isCapital)
            {
                writer.ApplyRetroGameplayComponentLumSnap(len, wid, hgt, hullKey);
                writer.ApplyRetroCapitalMaterialsBoost(hgt);
                writer.ApplyRetroCapitalRelight(hgt, hullKey, len, wid);
                RaceSurfaceDetail.AppendRetroScorerAccentBands(writer, hullKey, len, wid, hgt);
                writer.ApplyRetroAccentLumSnap(len, wid, hgt, hullKey);
                writer.ApplyRetroAccentPaletteSnap(ToVector3(race.Palette.Accent));
                RaceSurfaceDetail.AppendRetroIdentityAccentPatches(writer, hullKey, len, wid, hgt, ToVector3(race.Palette.Accent));
            }
            else if (isUtility)
            {
                writer.ApplyRetroUtilityComponentLumSnap(len, wid, hgt, hullKey);
                writer.ApplyRetroUtilityRelight(hgt, hullKey, len, wid);
                RaceSurfaceDetail.AppendRetroScorerAccentBands(writer, hullKey, len, wid, hgt);
                writer.ApplyRetroAccentLumSnap(len, wid, hgt, hullKey);
                writer.ApplyRetroAccentPaletteSnap(ToVector3(race.Palette.Accent));
                RaceSurfaceDetail.AppendRetroIdentityAccentPatches(writer, hullKey, len, wid, hgt, ToVector3(race.Palette.Accent));
            }
            else if (hullKey is "fighter" or "fighter_basic" or "hero" or "hero_default"
                or "scout" or "scout_light" or "interceptor" or "interceptor_mk2"
                or "drone" or "drone_swarm")
            {
                writer.ApplyRetroGameplayComponentLumSnap(len, wid, hgt, hullKey);
                if (hullKey is "scout" or "scout_light" or "interceptor" or "interceptor_mk2"
                    or "drone" or "drone_swarm")
                    writer.ApplyRetroCompactCraftRelight(hgt, hullKey, len, wid);
                if (hullKey is "fighter" or "fighter_basic" or "hero" or "hero_default")
                    writer.ApplyRetroReferenceCraftRelight(hgt, hullKey, len, wid);
                RaceSurfaceDetail.AppendRetroScorerAccentBands(writer, hullKey, len, wid, hgt);
                writer.ApplyRetroAccentLumSnap(len, wid, hgt, hullKey);
                writer.ApplyRetroAccentPaletteSnap(ToVector3(race.Palette.Accent));
            }
            else if (isMediumCombat)
            {
                writer.ApplyRetroGameplayComponentLumSnap(len, wid, hgt, hullKey);
                writer.ApplyRetroMediumCombatRelight(hgt, hullKey, len, wid);
                RaceSurfaceDetail.AppendRetroScorerAccentBands(writer, hullKey, len, wid, hgt);
                writer.ApplyRetroAccentLumSnap(len, wid, hgt, hullKey);
                writer.ApplyRetroAccentPaletteSnap(ToVector3(race.Palette.Accent));
            }

            RaceSurfaceDetail.AppendRetroEnvelopeAnchorBands(writer, hullKey, len, wid, hgt);
            writer.ApplyRetroBakeFlatFaceUniformize();
            writer.ApplyRetroSurfaceAccentBoost(len, wid, hgt, hullKey);
            RaceSurfaceDetail.AppendRetroIdentityAccentPatches(writer, hullKey, len, wid, hgt, ToVector3(race.Palette.Accent));
            writer.ApplyRetroFlatPanelLuminanceSmooth();
            if (hullKey is "fighter_basic" or "cruiser_heavy" or "dreadnought"
                or "freighter_bulk" or "transport_cargo" or "gunship_heavy" or "bomber_heavy")
                writer.ApplyRetroGameplayComponentLumSnap(len, wid, hgt, hullKey);
        }
        else
            writer.ApplyBakedLighting(new Vector3(0.35f, 0.9f, 0.25f));
        return writer.ToArray();
    }

    public static float[] Build(string raceId, string hullOrDefinitionId, Vector3? tint = null, float sizeScale = 1f)
        => BuildDesignInternal(ShipDesignCatalog.Resolve(hullOrDefinitionId, raceId), tint, sizeScale, hullOrDefinitionId);

    public static float[] BuildForDefinition(string definitionId, string? raceId = null, Vector3? tint = null)
    {
        string race = raceId ?? DefaultRace;
        return BuildDesignInternal(ShipDesignCatalog.Resolve(definitionId, race), tint, 1f, definitionId);
    }

    public static float[] BuildEnemyVariant(string definitionId, Vector3? tint = null)
        => BuildDesignInternal(ShipDesignCatalog.ResolveForEnemy(definitionId), tint, 1f, definitionId);

    private static void BuildHullCore(
        RaceMeshWriter w, string hullKey, RaceVisualDefinition race, HullClassProfile hull,
        float len, float wid, float hgt)
    {
        RaceHullSilhouette.Build(w, race, hullKey, len, wid, hgt);
    }

    private static void ApplyRaceStyle(
        RaceMeshWriter w, RaceVisualDefinition race, string hullKey,
        float len, float wid, float hgt, float engineScale)
        => ApplyRaceStyle(w, race, hullKey, len, wid, hgt, engineScale,
            race.Modifiers.WingSweep, race.Modifiers.WingSpan, race.Modifiers.Asymmetry,
            race.Modifiers.Protrusion, race.Modifiers.EngineCount);

    private static void ApplyRaceStyle(
        RaceMeshWriter w, RaceVisualDefinition race, string hullKey,
        float len, float wid, float hgt, float engineScale,
        float sweep, float span, float asym, float protrusion, int engineCount)
    {
        if (!((race.Style.Equals("vasudan", StringComparison.OrdinalIgnoreCase) && VasudanUsesNacelleEngines(hullKey))
            || (race.Style.Equals("organic", StringComparison.OrdinalIgnoreCase) && OrganicUsesIntegratedEngines(hullKey))
            || (race.Style.Equals("asymmetric", StringComparison.OrdinalIgnoreCase) && AsymmetricUsesIntegratedEngines(hullKey))
            || (race.Style.Equals("radiant", StringComparison.OrdinalIgnoreCase) && RadiantUsesIntegratedEngines(hullKey))
            || (race.Style.Equals("crystalline", StringComparison.OrdinalIgnoreCase) && CrystallineUsesIntegratedEngines(hullKey))
            || (race.Style.Equals("spiny", StringComparison.OrdinalIgnoreCase) && SpinyUsesIntegratedEngines(hullKey))))
            AddEngines(w, len, wid, hgt, engineCount, engineScale, race.Style);
    }

    private static bool AsymmetricUsesIntegratedEngines(string hullKey) =>
        hullKey is "fighter" or "fighter_basic"
            or "hero" or "hero_default"
            or "scout" or "scout_light"
            or "interceptor" or "interceptor_mk2"
            or "drone" or "drone_swarm"
            or "cruiser" or "cruiser_heavy"
            or "carrier" or "carrier_command"
            or "dreadnought";

    private static bool CrystallineUsesIntegratedEngines(string hullKey) =>
        hullKey is "cruiser" or "cruiser_heavy"
            or "carrier" or "carrier_command"
            or "dreadnought"
            or "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk" or "support_repair";

    private static bool RadiantUsesIntegratedEngines(string hullKey) =>
        hullKey is "fighter" or "fighter_basic"
            or "hero" or "hero_default"
            or "scout" or "scout_light"
            or "interceptor" or "interceptor_mk2"
            or "drone" or "drone_swarm"
            or "corvette" or "corvette_fast"
            or "frigate" or "frigate_strike"
            or "bomber" or "bomber_heavy"
            or "gunship" or "gunship_heavy"
            or "destroyer" or "destroyer_assault"
            or "cruiser" or "cruiser_heavy"
            or "carrier" or "carrier_command"
            or "dreadnought";

    private static bool OrganicUsesIntegratedEngines(string hullKey) =>
        hullKey is "fighter" or "fighter_basic"
            or "hero" or "hero_default"
            or "scout" or "scout_light"
            or "interceptor" or "interceptor_mk2"
            or "drone" or "drone_swarm"
            or "corvette" or "corvette_fast"
            or "frigate" or "frigate_strike"
            or "bomber" or "bomber_heavy"
            or "gunship" or "gunship_heavy"
            or "destroyer" or "destroyer_assault"
            or "cruiser" or "cruiser_heavy"
            or "carrier" or "carrier_command"
            or "dreadnought"
            or "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk" or "support_repair";

    private static bool SpinyUsesIntegratedEngines(string hullKey) =>
        hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk" or "support_repair"
            or "corvette" or "corvette_fast"
            or "frigate" or "frigate_strike"
            or "bomber" or "bomber_heavy"
            or "gunship" or "gunship_heavy"
            or "destroyer" or "destroyer_assault"
            or "cruiser" or "cruiser_heavy"
            or "carrier" or "carrier_command"
            or "dreadnought";

    private static bool VasudanUsesNacelleEngines(string hullKey) =>
        hullKey is "fighter" or "fighter_basic"
            or "hero" or "hero_default"
            or "scout" or "scout_light"
            or "interceptor" or "interceptor_mk2"
            or "drone" or "drone_swarm"
            or "corvette" or "frigate" or "gunship" or "bomber"
            or "destroyer" or "destroyer_assault"
            or "cruiser" or "cruiser_heavy"
            or "carrier" or "carrier_command"
            or "dreadnought"
            or "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk" or "support_repair";

    // ── Hull class cores ──────────────────────────────────────────────────────

    private static void BuildScoutCore(RaceMeshWriter w, float len, float wid, float hgt)
    {
        w.Tri(0, hgt * 0.25f, len, -wid * 0.15f, 0, len * 0.35f, wid * 0.15f, 0, len * 0.35f);
        w.Tri(-wid * 0.15f, 0, len * 0.35f, -wid * 0.38f, 0, -len * 0.15f, 0, hgt * 0.5f, 0);
        w.Tri(wid * 0.15f, 0, len * 0.35f, 0, hgt * 0.5f, 0, wid * 0.38f, 0, -len * 0.15f);
        w.Tri(-wid * 0.38f, 0, -len * 0.15f, wid * 0.38f, 0, -len * 0.15f, 0, hgt * 0.15f, -len * 0.45f);
        w.Tri(0, hgt * 0.7f, -len * 0.35f, -wid * 0.1f, hgt * 0.2f, -len * 0.55f, wid * 0.1f, hgt * 0.2f, -len * 0.55f);
    }

    private static void BuildFighterCore(RaceMeshWriter w, float len, float wid, float hgt)
    {
        w.Tri(0, hgt * 0.3f, len * 0.95f, -wid * 0.22f, 0, len * 0.35f, wid * 0.22f, 0, len * 0.35f);
        w.Tri(-wid * 0.22f, 0, len * 0.35f, -wid * 0.55f, 0, -len * 0.12f, -wid * 0.18f, 0, -len * 0.45f);
        w.Tri(wid * 0.22f, 0, len * 0.35f, wid * 0.18f, 0, -len * 0.45f, wid * 0.55f, 0, -len * 0.12f);
        w.Tri(-wid * 0.18f, 0, -len * 0.45f, wid * 0.18f, 0, -len * 0.45f, 0, 0, -len * 0.55f);
        w.Tri(0, hgt * 1.1f, len * 0.55f, -wid * 0.12f, hgt * 0.35f, len * 0.1f, wid * 0.12f, hgt * 0.35f, len * 0.1f);
        w.Tri(-wid * 0.28f, hgt * 0.08f, -len * 0.62f, wid * 0.28f, hgt * 0.08f, -len * 0.62f, 0, 0, -len * 0.55f);
    }

    private static void BuildInterceptorCore(RaceMeshWriter w, float len, float wid, float hgt)
    {
        w.Tri(0, hgt * 0.2f, len, -wid * 0.12f, 0, len * 0.5f, wid * 0.12f, 0, len * 0.5f);
        w.Tri(-wid * 0.12f, 0, len * 0.5f, -wid * 0.42f, 0, len * 0.05f, -wid * 0.2f, 0, -len * 0.35f);
        w.Tri(wid * 0.12f, 0, len * 0.5f, wid * 0.2f, 0, -len * 0.35f, wid * 0.42f, 0, len * 0.05f);
        w.Tri(-wid * 0.42f, 0, len * 0.05f, wid * 0.42f, 0, len * 0.05f, 0, hgt * 0.65f, len * 0.25f);
        w.Tri(0, hgt * 0.85f, -len * 0.2f, -wid * 0.15f, hgt * 0.15f, -len * 0.55f, wid * 0.15f, hgt * 0.15f, -len * 0.55f);
    }

    private static void BuildDroneCore(RaceMeshWriter w, float len, float wid, float hgt)
    {
        w.Tri(0, hgt * 0.45f, len * 0.5f, -wid * 0.38f, 0, -len * 0.08f, wid * 0.38f, 0, -len * 0.08f);
        w.Tri(-wid * 0.38f, 0, -len * 0.08f, -wid * 0.48f, hgt * 0.12f, -len * 0.38f, 0, 0, -len * 0.22f);
        w.Tri(wid * 0.38f, 0, -len * 0.08f, 0, 0, -len * 0.22f, wid * 0.48f, hgt * 0.12f, -len * 0.38f);
        w.Tri(-wid * 0.25f, hgt * 0.05f, len * 0.15f, wid * 0.25f, hgt * 0.05f, len * 0.15f, 0, hgt * 0.45f, len * 0.5f);
    }

    private static void BuildCorvetteCore(RaceMeshWriter w, float len, float wid, float hgt)
    {
        w.Tri(0, hgt * 0.35f, len * 0.85f, -wid * 0.28f, 0, len * 0.2f, wid * 0.28f, 0, len * 0.2f);
        w.Tri(-wid * 0.28f, 0, len * 0.2f, -wid * 0.58f, hgt * 0.1f, -len * 0.22f, -wid * 0.2f, 0, -len * 0.55f);
        w.Tri(wid * 0.28f, 0, len * 0.2f, wid * 0.2f, 0, -len * 0.55f, wid * 0.58f, hgt * 0.1f, -len * 0.22f);
        w.Tri(0, hgt * 0.95f, len * 0.35f, -wid * 0.16f, hgt * 0.4f, 0, wid * 0.16f, hgt * 0.4f, 0);
        w.Tri(-wid * 0.2f, 0, -len * 0.55f, wid * 0.2f, 0, -len * 0.55f, 0, hgt * 0.2f, -len * 0.65f);
    }

    private static void BuildFrigateCore(RaceMeshWriter w, float len, float wid, float hgt)
    {
        w.Tri(0, hgt * 0.4f, len * 0.78f, -wid * 0.32f, 0, len * 0.15f, wid * 0.32f, 0, len * 0.15f);
        w.Tri(-wid * 0.32f, 0, len * 0.15f, -wid * 0.68f, hgt * 0.12f, -len * 0.12f, -wid * 0.26f, hgt * 0.18f, -len * 0.58f);
        w.Tri(wid * 0.32f, 0, len * 0.15f, wid * 0.26f, hgt * 0.18f, -len * 0.58f, wid * 0.68f, hgt * 0.12f, -len * 0.12f);
        w.Tri(-wid * 0.26f, hgt * 0.18f, -len * 0.58f, wid * 0.26f, hgt * 0.18f, -len * 0.58f, 0, hgt * 0.22f, -len * 0.78f);
        w.Tri(0, hgt * 1.05f, len * 0.05f, -wid * 0.22f, hgt * 0.55f, -len * 0.32f, wid * 0.22f, hgt * 0.55f, -len * 0.32f);
    }

    private static void BuildGunshipCore(RaceMeshWriter w, float len, float wid, float hgt)
    {
        w.Tri(0, hgt * 0.55f, len * 0.68f, -wid * 0.48f, hgt * 0.12f, len * 0.05f, wid * 0.48f, hgt * 0.12f, len * 0.05f);
        w.Tri(-wid * 0.48f, hgt * 0.12f, len * 0.05f, -wid * 0.58f, hgt * 0.22f, -len * 0.32f, -wid * 0.36f, hgt * 0.28f, -len * 0.62f);
        w.Tri(wid * 0.48f, hgt * 0.12f, len * 0.05f, wid * 0.36f, hgt * 0.28f, -len * 0.62f, wid * 0.58f, hgt * 0.22f, -len * 0.32f);
        w.Tri(0, hgt * 1.15f, len * 0.18f, -wid * 0.2f, hgt * 0.75f, -len * 0.08f, wid * 0.2f, hgt * 0.75f, -len * 0.08f);
        w.Tri(-wid * 0.22f, hgt * 0.35f, -len * 0.45f, wid * 0.22f, hgt * 0.35f, -len * 0.45f, 0, hgt * 0.45f, -len * 0.72f);
    }

    private static void BuildBomberCore(RaceMeshWriter w, float len, float wid, float hgt)
    {
        w.Tri(0, hgt * 0.25f, len * 0.72f, -wid * 0.62f, 0, -len * 0.38f, wid * 0.62f, 0, -len * 0.38f);
        w.Tri(-wid * 0.52f, hgt * 0.12f, len * 0.18f, -wid * 0.82f, 0, -len * 0.52f, -wid * 0.32f, 0, -len * 0.52f);
        w.Tri(wid * 0.52f, hgt * 0.12f, len * 0.18f, wid * 0.32f, 0, -len * 0.52f, wid * 0.82f, 0, -len * 0.52f);
        w.Tri(0, hgt * 0.55f, len * 0.42f, -wid * 0.42f, 0, 0, wid * 0.42f, 0, 0);
        w.Tri(0, hgt * 0.35f, -len * 0.28f, -wid * 0.12f, 0, -len * 0.62f, wid * 0.12f, 0, -len * 0.62f);
    }

    private static void BuildDestroyerCore(RaceMeshWriter w, float len, float wid, float hgt)
    {
        w.Tri(0, hgt * 0.35f, len, -wid * 0.2f, 0, len * 0.32f, wid * 0.2f, 0, len * 0.32f);
        w.Tri(-wid * 0.2f, 0, len * 0.32f, -wid * 0.32f, hgt * 0.15f, -len * 0.58f, 0, hgt * 0.18f, 0);
        w.Tri(wid * 0.2f, 0, len * 0.32f, 0, hgt * 0.18f, 0, wid * 0.32f, hgt * 0.15f, -len * 0.58f);
        w.Tri(-wid * 0.32f, 0, -len * 0.18f, -wid * 0.72f, 0, len * 0.08f, -wid * 0.42f, 0, -len * 0.52f);
        w.Tri(wid * 0.32f, 0, -len * 0.18f, wid * 0.42f, 0, -len * 0.52f, wid * 0.72f, 0, len * 0.08f);
        w.Tri(-wid * 0.16f, hgt * 0.08f, -len * 0.62f, wid * 0.16f, hgt * 0.08f, -len * 0.62f, 0, hgt * 0.12f, -len * 0.82f);
    }

    private static void BuildCruiserCore(RaceMeshWriter w, float len, float wid, float hgt)
    {
        w.Tri(0, hgt * 0.45f, len * 0.82f, -wid * 0.46f, 0, len * 0.25f, wid * 0.46f, 0, len * 0.25f);
        w.Tri(-wid * 0.46f, 0, len * 0.25f, -wid * 0.56f, hgt * 0.2f, -len * 0.18f, -wid * 0.42f, hgt * 0.24f, -len * 0.66f);
        w.Tri(wid * 0.46f, 0, len * 0.25f, wid * 0.42f, hgt * 0.24f, -len * 0.66f, wid * 0.56f, hgt * 0.2f, -len * 0.18f);
        w.Tri(0, hgt * 1.05f, len * 0.05f, -wid * 0.26f, hgt * 0.65f, -len * 0.35f, wid * 0.26f, hgt * 0.65f, -len * 0.35f);
        w.Tri(-wid * 0.35f, hgt * 0.15f, -len * 0.48f, wid * 0.35f, hgt * 0.15f, -len * 0.48f, 0, hgt * 0.28f, -len * 0.78f);
    }

    private static void BuildCarrierCore(RaceMeshWriter w, float len, float wid, float hgt)
    {
        w.Tri(0, hgt * 0.35f, len * 0.82f, -wid * 0.42f, 0, len * 0.28f, wid * 0.42f, 0, len * 0.28f);
        w.Tri(-wid * 0.42f, 0, len * 0.28f, -wid * 0.52f, hgt * 0.22f, -len * 0.28f, -wid * 0.42f, hgt * 0.2f, -len * 0.68f);
        w.Tri(wid * 0.42f, 0, len * 0.28f, wid * 0.42f, hgt * 0.2f, -len * 0.68f, wid * 0.52f, hgt * 0.22f, -len * 0.28f);
        w.Tri(-wid * 0.36f, hgt * 0.22f, len * 0.18f, wid * 0.36f, hgt * 0.22f, len * 0.18f, 0, hgt * 0.28f, -len * 0.38f);
        w.Tri(-wid * 0.22f, hgt * 0.04f, -len * 0.42f, wid * 0.22f, hgt * 0.04f, -len * 0.42f, 0, hgt * 0.04f, -len * 0.72f);
        w.Tri(-wid * 0.42f, hgt * 0.2f, -len * 0.68f, wid * 0.42f, hgt * 0.2f, -len * 0.68f, 0, hgt * 0.18f, -len * 0.88f);
    }

    private static void BuildDreadnoughtCore(RaceMeshWriter w, float len, float wid, float hgt)
    {
        w.Tri(0, hgt * 0.55f, len * 0.88f, -wid * 0.52f, hgt * 0.08f, len * 0.18f, wid * 0.52f, hgt * 0.08f, len * 0.18f);
        w.Tri(-wid * 0.52f, hgt * 0.08f, len * 0.18f, -wid * 0.72f, hgt * 0.22f, -len * 0.14f, -wid * 0.46f, hgt * 0.3f, -len * 0.66f);
        w.Tri(wid * 0.52f, hgt * 0.08f, len * 0.18f, wid * 0.46f, hgt * 0.3f, -len * 0.66f, wid * 0.72f, hgt * 0.22f, -len * 0.14f);
        w.Tri(0, hgt * 1.15f, len * 0.05f, -wid * 0.32f, hgt * 0.72f, -len * 0.35f, wid * 0.32f, hgt * 0.72f, -len * 0.35f);
        w.Tri(-wid * 0.46f, hgt * 0.3f, -len * 0.66f, wid * 0.46f, hgt * 0.3f, -len * 0.66f, 0, hgt * 0.38f, -len * 0.88f);
    }

    private static void BuildMinerCore(RaceMeshWriter w, float len, float wid, float hgt)
    {
        w.Tri(0, hgt * 0.3f, len * 0.62f, -wid * 0.42f, 0, len * 0.05f, wid * 0.42f, 0, len * 0.05f);
        w.Tri(-wid * 0.42f, 0, len * 0.05f, -wid * 0.52f, hgt * 0.14f, -len * 0.35f, -wid * 0.2f, hgt * 0.16f, -len * 0.55f);
        w.Tri(wid * 0.42f, 0, len * 0.05f, wid * 0.2f, hgt * 0.16f, -len * 0.55f, wid * 0.52f, hgt * 0.14f, -len * 0.35f);
        w.Tri(0, hgt * 0.85f, len * 0.35f, -wid * 0.14f, hgt * 0.55f, len * 0.05f, wid * 0.14f, hgt * 0.55f, len * 0.05f);
        w.Tri(0, hgt * 0.45f, len * 0.72f, -wid * 0.18f, hgt * 0.25f, len * 0.48f, wid * 0.18f, hgt * 0.25f, len * 0.48f);
    }

    private static void BuildTransportCore(RaceMeshWriter w, float len, float wid, float hgt)
    {
        w.Tri(0, hgt * 0.32f, len * 0.58f, -wid * 0.58f, 0, len * 0.08f, wid * 0.58f, 0, len * 0.08f);
        w.Tri(-wid * 0.58f, 0, len * 0.08f, -wid * 0.62f, hgt * 0.15f, -len * 0.45f, -wid * 0.36f, hgt * 0.18f, -len * 0.66f);
        w.Tri(wid * 0.58f, 0, len * 0.08f, wid * 0.36f, hgt * 0.18f, -len * 0.66f, wid * 0.62f, hgt * 0.15f, -len * 0.45f);
        w.Tri(0, hgt * 0.55f, 0, -wid * 0.26f, hgt * 0.38f, -len * 0.2f, wid * 0.26f, hgt * 0.38f, -len * 0.2f);
    }

    private static void BuildFreighterCore(RaceMeshWriter w, float len, float wid, float hgt)
    {
        w.Tri(0, hgt * 0.28f, len * 0.72f, -wid * 0.68f, 0, len * 0.15f, wid * 0.68f, 0, len * 0.15f);
        w.Tri(-wid * 0.68f, 0, len * 0.15f, -wid * 0.72f, hgt * 0.18f, -len * 0.55f, -wid * 0.48f, hgt * 0.2f, -len * 0.78f);
        w.Tri(wid * 0.68f, 0, len * 0.15f, wid * 0.48f, hgt * 0.2f, -len * 0.78f, wid * 0.72f, hgt * 0.18f, -len * 0.55f);
        w.Tri(-wid * 0.55f, hgt * 0.12f, 0, wid * 0.55f, hgt * 0.12f, 0, 0, hgt * 0.22f, -len * 0.35f);
        w.Tri(-wid * 0.35f, hgt * 0.08f, len * 0.45f, wid * 0.35f, hgt * 0.08f, len * 0.45f, 0, hgt * 0.15f, len * 0.55f);
    }

    private static void BuildSupportCore(RaceMeshWriter w, float len, float wid, float hgt)
    {
        BuildTransportCore(w, len, wid, hgt);
        w.Tri(0, hgt * 0.75f, len * 0.12f, -wid * 0.22f, hgt * 0.55f, -len * 0.05f, wid * 0.22f, hgt * 0.55f, -len * 0.05f);
        w.Tri(-wid * 0.18f, hgt * 0.45f, -len * 0.35f, wid * 0.18f, hgt * 0.45f, -len * 0.35f, 0, hgt * 0.65f, -len * 0.48f);
    }

    private static void BuildHeroCore(RaceMeshWriter w, float len, float wid, float hgt)
    {
        BuildDestroyerCore(w, len, wid, hgt);
        w.Tri(0, hgt * 1.25f, len * 0.55f, -wid * 0.18f, hgt * 0.85f, len * 0.25f, wid * 0.18f, hgt * 0.85f, len * 0.25f);
        w.Tri(-wid * 0.12f, hgt * 0.65f, -len * 0.15f, wid * 0.12f, hgt * 0.65f, -len * 0.15f, 0, hgt * 0.95f, -len * 0.35f);
    }

    // ── Race style overlays ───────────────────────────────────────────────────

    private static void AddDeltaWings(RaceMeshWriter w, float z, float wid, float sweep, float hgt)
    {
        float back = z - sweep * wid * 0.6f;
        w.Tri(-wid * 0.22f, hgt * 0.08f, z, -wid * 0.62f, hgt * 0.02f, back, -wid * 0.28f, hgt * 0.04f, z - wid * 0.1f);
        w.Tri(wid * 0.22f, hgt * 0.08f, z, wid * 0.28f, hgt * 0.04f, z - wid * 0.1f, wid * 0.62f, hgt * 0.02f, back);
    }

    private static void AddCockpitHump(RaceMeshWriter w, float z, float wid, float hgt)
    {
        w.Tri(0, hgt, z, -wid, hgt * 0.55f, z - wid * 0.12f, wid, hgt * 0.55f, z - wid * 0.12f);
    }

    private static void AddSleekSpine(RaceMeshWriter w, float len, float wid, float hgt)
    {
        w.Tri(0, hgt, len * 0.7f, -wid, hgt * 0.35f, -len * 0.5f, wid, hgt * 0.35f, -len * 0.5f);
    }

    private static void AddMinimalStabilizers(RaceMeshWriter w, float z, float wid, float sweep, float hgt)
    {
        w.Tri(-wid * 0.15f, hgt * 0.1f, z, -wid * 0.55f, 0, z - sweep, 0, 0, z - wid * 0.2f);
        w.Tri(wid * 0.15f, hgt * 0.1f, z, 0, 0, z - wid * 0.2f, wid * 0.55f, 0, z - sweep);
    }

    private static void AddArmorBelt(RaceMeshWriter w, float z, float wid, float hgt)
    {
        w.Tri(-wid * 0.55f, hgt * 0.35f, z, wid * 0.55f, hgt * 0.35f, z, wid * 0.55f, hgt * 0.35f, z - wid * 0.4f);
        w.Tri(-wid * 0.55f, hgt * 0.35f, z, wid * 0.55f, hgt * 0.35f, z - wid * 0.4f, -wid * 0.55f, hgt * 0.35f, z - wid * 0.4f);
    }

    private static void AddSponsonBlocks(RaceMeshWriter w, float z, float wid, float hgt, float protrusion)
    {
        float p = protrusion * wid * 0.4f;
        w.Tri(-wid * 0.55f - p, hgt * 0.22f, z, -wid * 0.58f, hgt * 0.06f, z - wid * 0.12f, -wid * 0.48f, hgt * 0.16f, z);
        w.Tri(wid * 0.55f + p, hgt * 0.22f, z, wid * 0.48f, hgt * 0.16f, z, wid * 0.58f, hgt * 0.06f, z - wid * 0.12f);
    }

    private static void AddFlowFins(RaceMeshWriter w, float z, float wid, float sweep, float hgt)
    {
        w.Tri(-wid * 0.18f, hgt * 0.42f, z, -wid * 0.58f, hgt * 0.14f, z - sweep * wid * 0.6f, -wid * 0.28f, hgt * 0.06f, z - wid * 0.14f);
        w.Tri(wid * 0.18f, hgt * 0.42f, z, wid * 0.28f, hgt * 0.06f, z - wid * 0.14f, wid * 0.58f, hgt * 0.14f, z - sweep * wid * 0.6f);
    }

    private static void AddBioPods(RaceMeshWriter w, float z, float wid, float hgt, int count)
    {
        int pods = Math.Clamp(count, 1, 4);
        for (int i = 0; i < pods; i++)
        {
            float side = (i - (pods - 1) * 0.5f) * wid * 0.35f;
            w.Tri(side, hgt * 0.65f, z, side - wid * 0.12f, hgt * 0.2f, z - wid * 0.15f, side + wid * 0.12f, hgt * 0.2f, z - wid * 0.15f);
        }
    }

    private static void AddMandibleProngs(RaceMeshWriter w, float z, float wid, float hgt, float asym)
    {
        float skew = asym * wid * 0.3f;
        w.Tri(-wid * 0.35f, hgt * 0.15f, z, -wid * 0.55f - skew, 0, z + wid * 0.35f, -wid * 0.15f, 0, z + wid * 0.15f);
        w.Tri(wid * 0.35f, hgt * 0.15f, z, wid * 0.15f, 0, z + wid * 0.15f, wid * 0.55f + skew, 0, z + wid * 0.35f);
    }

    private static void AddOffsetPod(RaceMeshWriter w, float z, float wid, float hgt, float asym)
    {
        float x = wid * 0.35f * (1f + asym);
        w.Tri(x, hgt, z, x + wid * 0.25f, hgt * 0.3f, z - wid * 0.2f, x - wid * 0.1f, hgt * 0.25f, z - wid * 0.35f);
    }

    private static void AddSolarFins(RaceMeshWriter w, float z, float wid, float hgt, float intensity)
    {
        float reach = wid * (0.9f + intensity * 0.3f);
        w.Tri(-reach, hgt, z, 0, hgt * 1.35f, z + wid * 0.15f, -wid * 0.2f, hgt * 0.55f, z);
        w.Tri(reach, hgt, z, wid * 0.2f, hgt * 0.55f, z, 0, hgt * 1.35f, z + wid * 0.15f);
    }

    private static void AddCrownBridge(RaceMeshWriter w, float z, float wid, float hgt)
    {
        w.Tri(0, hgt, z, -wid, hgt * 0.58f, z - wid * 0.12f, wid, hgt * 0.58f, z - wid * 0.12f);
        w.Tri(-wid * 0.28f, hgt * 0.72f, z + wid * 0.06f, wid * 0.28f, hgt * 0.72f, z + wid * 0.06f, 0, hgt * 0.88f, z + wid * 0.12f);
    }

    private static void AddVoidSpines(RaceMeshWriter w, float len, float wid, float hgt, float protrusion, float asym)
    {
        float p = protrusion;
        w.Tri(0, hgt * (1.2f + p), len * 0.25f, -wid * (0.15f + asym * 0.1f), hgt * 0.4f, len * 0.05f, wid * (0.12f + asym * 0.08f), hgt * 0.35f, len * 0.08f);
        w.Tri(-wid * 0.45f, hgt * (0.8f + p * 0.5f), -len * 0.2f, -wid * 0.25f, hgt * 0.2f, -len * 0.35f, 0, hgt * 0.55f, -len * 0.15f);
        w.Tri(wid * (0.5f + asym * 0.15f), hgt * (0.75f + p * 0.4f), -len * 0.28f, 0, hgt * 0.55f, -len * 0.15f, wid * 0.22f, hgt * 0.18f, -len * 0.42f);
    }

    private static void AddRiftInset(RaceMeshWriter w, float z, float wid, float hgt)
    {
        w.Tri(-wid * 0.22f, hgt * 0.05f, z, wid * 0.22f, hgt * 0.05f, z, 0, hgt * 0.05f, z - wid * 0.45f);
    }

    private static void AddIceShards(RaceMeshWriter w, float z, float wid, float hgt, float sharpness)
    {
        float ext = wid * (0.5f + sharpness * 0.35f);
        w.Tri(-ext, hgt * 0.35f, z, -wid * 0.15f, hgt * 1.1f, z + wid * 0.12f, -wid * 0.35f, 0, z - wid * 0.1f);
        w.Tri(ext, hgt * 0.35f, z, wid * 0.35f, 0, z - wid * 0.1f, wid * 0.15f, hgt * 1.1f, z + wid * 0.12f);
    }

    private static void AddPrismBridge(RaceMeshWriter w, float z, float wid, float hgt)
    {
        w.Tri(0, hgt, z, -wid, hgt * 0.45f, z - wid * 0.2f, wid, hgt * 0.45f, z - wid * 0.2f);
    }

    private static void AddCrystalFacets(RaceMeshWriter w, float len, float wid, float hgt, float sharpness)
    {
        if (sharpness < 0.35f) return;
        w.Tri(0, hgt * 0.65f, len * 0.15f, -wid * 0.18f, hgt * 0.25f, 0, wid * 0.18f, hgt * 0.25f, 0);
    }

    private static void AddCommandTower(RaceMeshWriter w, float z, float wid, float hgt, string style)
    {
        float cap = style == "radiant" ? 0.95f : 0.88f;
        w.Tri(wid * 0.32f, hgt * 0.52f, z, wid * 0.48f, hgt * 0.52f, z - wid * 0.14f, wid * 0.38f, hgt * cap, z - wid * 0.06f);
    }

    private static void AddEngines(RaceMeshWriter w, float len, float wid, float hgt, int count, float scale, string style)
    {
        int engines = Math.Clamp(count, 1, 4);
        float z = -len * 0.62f;
        float nozzle = wid * 0.12f * scale;
        float[] offsets = engines switch
        {
            1 => [0f],
            2 => [-wid * 0.28f, wid * 0.28f],
            3 => [-wid * 0.35f, 0f, wid * 0.35f],
            _ => [-wid * 0.42f, -wid * 0.14f, wid * 0.14f, wid * 0.42f],
        };

        bool compactEngine = style.Equals("vasudan", StringComparison.OrdinalIgnoreCase)
            || style.Equals("truss", StringComparison.OrdinalIgnoreCase);
        float nozzleReach = compactEngine ? 0.52f : 2.2f;
        float engineZ = compactEngine ? -len * 0.12f : z;

        foreach (float x in offsets)
        {
            float y = style is "blocky" or "truss" ? hgt * 0.15f : hgt * 0.08f;
            float ez = compactEngine ? engineZ : z;
            w.Tri(x, y, ez, x - nozzle, 0, ez - nozzle * nozzleReach, x + nozzle, 0, ez - nozzle * nozzleReach);
            if (style is "radiant" or "organic" or "crystalline")
            {
                w.Tri(x, y + hgt * 0.25f, z + nozzle * 0.5f, x - nozzle * 0.6f, y, z, x + nozzle * 0.6f, y, z);
            }
        }
    }

    private static Vector3 ToVector3(float[] rgb)
        => new(rgb.Length > 0 ? rgb[0] : 0.5f, rgb.Length > 1 ? rgb[1] : 0.5f, rgb.Length > 2 ? rgb[2] : 0.5f);

    private static string ResolveGameplayHullKey(string? definitionId, string hullClass)
    {
        if (!string.IsNullOrWhiteSpace(definitionId) &&
            definitionId is "fighter_basic" or "bomber_heavy" or "destroyer_assault" or "scout_light"
                or "carrier_command" or "cruiser_heavy" or "hero_default" or "miner_basic"
                or "miner_eva" or "miner_tractor" or "transport_cargo" or "interceptor_mk2"
                or "corvette_fast" or "frigate_strike" or "gunship_heavy" or "dreadnought"
                or "drone_swarm" or "freighter_bulk" or "support_repair")
            return definitionId;
        return hullClass;
    }
}
