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

    private static float[] BuildDesignInternal(ShipDesignSpec design, Vector3? tint, float sizeScale)
    {
        RaceVisualSchema.TryGetRace(design.RaceId, out RaceVisualDefinition? race);
        race ??= RaceVisualSchema.AllRaces.FirstOrDefault() ?? new RaceVisualDefinition { Id = DefaultRace };

        var hull = RaceVisualSchema.ResolveHullProfile(design.HullClass);
        var variant = ShipDesignVariant.FromSpec(design);
        var writer = new RaceMeshWriter();

        float s = hull.Size * sizeScale * variant.LengthScale;
        float len = s * hull.LengthRatio * race.Modifiers.HullLength * variant.LengthScale;
        float wid = s * hull.WidthRatio * race.Modifiers.HullWidth * variant.WidthScale;
        float hgt = s * hull.HeightRatio * variant.HeightScale;

        len += s * variant.NoseExtension * 0.5f;
        len += s * variant.SternExtension * 0.35f;

        var mods = race.Modifiers;
        float sweep = mods.WingSweep + variant.WingSweepDelta;
        float span = mods.WingSpan * (1f + variant.WingSpanDelta);
        float asym = mods.Asymmetry + variant.AsymmetryDelta;
        int engines = Math.Clamp(mods.EngineCount + variant.ExtraEngines, 1, 6);
        float protrusion = mods.Protrusion + variant.ProtrusionDelta;

        BuildHullCore(writer, design.HullClass, race, hull, len, wid, hgt);
        ApplyRaceStyle(writer, race, design.HullClass, len, wid, hgt, hull.EngineScale, sweep, span, asym, protrusion, engines);
        ShipDesignVariantGeometry.Apply(writer, design, variant, len, wid, hgt, race.Style);
        RaceSurfaceDetail.ApplyShipDetail(writer, race, design.HullClass, len, wid, hgt);

        Vector3 primary = tint ?? ToVector3(race.Palette.Primary);
        writer.RecolorPrimary(primary, ToVector3(race.Palette.Secondary), ToVector3(race.Palette.Accent), ToVector3(race.Palette.Engine));
        writer.ApplySubstrateVariation(RaceSubstrateProfile.ForRace(race));
        writer.ApplyBakedLighting(new Vector3(0.35f, 0.9f, 0.25f));
        return writer.ToArray();
    }

    public static float[] Build(string raceId, string hullOrDefinitionId, Vector3? tint = null, float sizeScale = 1f)
        => BuildDesign(ShipDesignCatalog.Resolve(hullOrDefinitionId, raceId), tint, sizeScale);

    public static float[] BuildForDefinition(string definitionId, string? raceId = null, Vector3? tint = null)
    {
        string race = raceId ?? DefaultRace;
        return BuildDesign(ShipDesignCatalog.Resolve(definitionId, race), tint);
    }

    public static float[] BuildEnemyVariant(string definitionId, Vector3? tint = null)
        => BuildDesign(ShipDesignCatalog.ResolveForEnemy(definitionId), tint);

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
        AddEngines(w, len, wid, hgt, engineCount, engineScale, race.Style);
    }

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

        foreach (float x in offsets)
        {
            float y = style is "blocky" or "truss" ? hgt * 0.15f : hgt * 0.08f;
            w.Tri(x, y, z, x - nozzle, 0, z - nozzle * 2.2f, x + nozzle, 0, z - nozzle * 2.2f);
            if (style is "radiant" or "organic" or "crystalline")
            {
                w.Tri(x, y + hgt * 0.25f, z + nozzle * 0.5f, x - nozzle * 0.6f, y, z, x + nozzle * 0.6f, y, z);
            }
        }
    }

    private static Vector3 ToVector3(float[] rgb)
        => new(rgb.Length > 0 ? rgb[0] : 0.5f, rgb.Length > 1 ? rgb[1] : 0.5f, rgb.Length > 2 ? rgb[2] : 0.5f);
}
