using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>Race-styled station geometry — one mesh per building type × race substrate.</summary>
public static class RaceStationMeshes
{
    public static float[] Build(string buildingType, string raceId, bool splitArticulation = false, float? styleScaleOverride = null)
    {
        RaceVisualSchema.TryGetRace(raceId, out RaceVisualDefinition? race);
        race ??= RaceVisualSchema.AllRaces.FirstOrDefault() ?? new RaceVisualDefinition { Id = RaceShipMeshes.DefaultRace };

        Vector3 primary = ToVector3(race.Palette.Primary);
        Vector3 secondary = ToVector3(race.Palette.Secondary);
        Vector3 accent = ToVector3(race.Palette.Accent);
        var writer = new RaceMeshWriter();
        float styleScale = styleScaleOverride ?? (0.85f + race.Modifiers.Superstructure * 0.3f);
        string type = buildingType.ToLowerInvariant();
        float stationScale = 7f * styleScale;

        switch (type)
        {
            case "command_center":
                BuildCommandCenter(writer, race, primary, secondary, accent, stationScale, omitMast: true);
                break;
            case "shipyard_small":
                BuildShipyard(writer, race, primary, secondary, accent, stationScale, 2, 2, omitCrane: true, omitBayDoor: true);
                break;
            case "shipyard_medium":
            case "shipyard":
                BuildShipyard(writer, race, primary, secondary, accent, stationScale, 3, 3, omitCrane: true, omitBayDoor: true);
                break;
            case "shipyard_large":
                BuildShipyard(writer, race, primary, secondary, accent, stationScale, 3, 5, omitCrane: true, omitBayDoor: true);
                break;
            case "defense_turret":
                BuildDefenseTurret(writer, race, primary, secondary, accent, stationScale, omitBarrel: true);
                break;
            case "missile_battery":
                BuildMissileBattery(writer, race, primary, secondary, accent, stationScale, omitPod: true);
                break;
            case "sensor_array":
                BuildSensorArray(writer, race, primary, secondary, accent, stationScale, omitDish: true);
                break;
            case "resource_refinery":
                BuildRefinery(writer, race, primary, secondary, accent, stationScale, omitArm: true);
                break;
            case "repair_bay":
                BuildRepairBay(writer, race, primary, secondary, accent, stationScale, omitCrane: true, omitBayDoor: true);
                break;
            case "power_reactor":
                BuildReactor(writer, race, primary, secondary, accent, stationScale, omitRing: true);
                break;
            case "supply_depot":
                BuildSupplyDepot(writer, race, primary, secondary, accent, stationScale, omitCrane: true);
                break;
            case "fabrication_hub":
                BuildFabricationHub(writer, race, primary, secondary, accent, stationScale);
                break;
            case "comms_relay":
                BuildCommsRelay(writer, race, primary, secondary, accent, stationScale);
                break;
            case "orbital_uplink":
                BuildOrbitalUplink(writer, race, primary, secondary, accent, stationScale);
                break;
            case "shield_emitter":
                BuildShieldEmitter(writer, race, primary, secondary, accent, stationScale);
                break;
            case "fortress_core":
                BuildFortressCore(writer, race, primary, secondary, accent, stationScale);
                break;
            default:
                BuildCommandCenter(writer, race, primary, secondary, accent, 6f * styleScale);
                break;
        }

        float detailScale = styleScale;
        float detailS = stationScale;
        RaceSurfaceDetail.ApplyStationDetail(writer, race, type, detailS);
        ApplyStationRaceRecolor(writer, race);
        writer.ApplySubstrateVariation(RaceSubstrateProfile.ForRace(race));
        ApplyStationRacePipeline(writer, race, type, detailS);
        return writer.ToArray();
    }

    /// <summary>Per-style palette remap — mirrors ship path before substrate variation.</summary>
    private static void ApplyStationRaceRecolor(RaceMeshWriter writer, RaceVisualDefinition race)
    {
        Vector3 primary = ToVector3(race.Palette.Primary);
        Vector3 secondary = ToVector3(race.Palette.Secondary);
        Vector3 accent = ToVector3(race.Palette.Accent);
        Vector3 engine = race.Palette.Engine?.Length >= 3
            ? ToVector3(race.Palette.Engine)
            : accent;

        string style = race.Style.ToLowerInvariant();
        if (style == "truss")
        {
            writer.RecolorTrussNasa(primary, secondary, accent, secondary * 0.55f, engine);
        }
        else if (style == "vasudan")
        {
            writer.RecolorVasudan(primary, secondary, accent, engine);
        }
        else if (style == "organic")
        {
            writer.RecolorOrganic(primary, secondary, accent, engine);
        }
        else if (style == "asymmetric")
        {
            writer.RecolorAsymmetric(primary, secondary, accent, engine);
        }
        else if (style == "radiant")
        {
            writer.RecolorRadiant(primary, secondary, accent, engine);
        }
        else if (style == "spiny")
        {
            writer.RecolorSpiny(primary, secondary, accent, engine);
        }
        else if (style == "crystalline")
        {
            writer.RecolorCrystalline(primary, secondary, accent, engine);
        }
        else
        {
            writer.RecolorPrimary(primary, secondary, accent, engine);
        }
    }

    /// <summary>command_center ≈ cruiser hgt; defense_turret/sensor_array ≈ fighter hgt.</summary>
    private static bool StationIsCompact(string buildingType) =>
        buildingType is "defense_turret" or "sensor_array";

    private const float StationBaseScale = 7f;
    private const string CompactReferenceHull = "fighter_basic";

    /// <summary>Relight envelope — compact stations mirror ship <c>fighter_basic</c> hgt/len/wid per style.</summary>
    private static (float hgt, float len, float wid) StationRelightEnvelope(
        RaceVisualDefinition race, float stationS, string buildingType)
    {
        if (!StationIsCompact(buildingType))
            return (stationS * 0.28f, stationS * 1.1f, stationS * 1.6f);

        return StationCompactRelightEnvelope(race, stationS);
    }

    /// <summary>Scale ship <c>fighter_basic</c> relight bands into station coordinate space (base scale 7).</summary>
    private static (float hgt, float len, float wid) StationCompactRelightEnvelope(
        RaceVisualDefinition race, float stationS)
    {
        var design = ShipDesignCatalog.Resolve(CompactReferenceHull, race.Id);
        var hull = RaceVisualSchema.ResolveHullProfile(design.HullClass);
        var variant = ShipDesignVariant.FromSpec(design);

        float sShip = hull.Size * variant.LengthScale;
        float len = sShip * hull.LengthRatio * race.Modifiers.HullLength * variant.LengthScale;
        float wid = sShip * hull.WidthRatio * race.Modifiers.HullWidth * variant.WidthScale;
        float hgt = sShip * hull.HeightRatio * variant.HeightScale;
        len += sShip * variant.NoseExtension * 0.5f;
        len += sShip * variant.SternExtension * 0.35f;

        ApplyFighterBasicStyleEnvelopeScaling(race.Style, ref len, ref wid, ref hgt);

        float hgtStation = hgt * stationS / StationBaseScale;
        float footprintScale = stationS / sShip;
        return (hgtStation, len * footprintScale, wid * footprintScale);
    }

    /// <summary>Per-style fighter_basic AABB trims — mirrors <see cref="RaceShipMeshes"/> build path.</summary>
    private static void ApplyFighterBasicStyleEnvelopeScaling(
        string style, ref float len, ref float wid, ref float hgt)
    {
        if (style.Equals("retro", StringComparison.OrdinalIgnoreCase))
        {
            wid *= 1.36f;
            len *= 0.78f;
        }
        else if (style.Equals("truss", StringComparison.OrdinalIgnoreCase))
        {
            wid *= 1.32f;
            len *= 0.76f;
        }
        else if (style.Equals("organic", StringComparison.OrdinalIgnoreCase))
        {
            wid *= 1.74f;
            len *= 0.74f;
        }
        else if (style.Equals("asymmetric", StringComparison.OrdinalIgnoreCase))
        {
            wid *= 1.68f;
            len *= 0.80f;
        }
        else if (style.Equals("radiant", StringComparison.OrdinalIgnoreCase))
        {
            wid *= 1.50f;
            len *= 0.78f;
        }
        else if (style.Equals("spiny", StringComparison.OrdinalIgnoreCase))
        {
            wid *= 2.22f;
            len *= 0.62f;
        }
        else if (style.Equals("crystalline", StringComparison.OrdinalIgnoreCase))
        {
            wid *= 2.55f;
            len *= 0.60f;
        }
    }

    /// <summary>Maps station building types to ship hull keys so capital/compact accent lum snaps gate correctly.</summary>
    private static string StationHullKeyAnalog(string buildingType, bool compact)
    {
        if (compact)
            return CompactReferenceHull;

        return buildingType switch
        {
            "shipyard_large" => "carrier_command",
            "shipyard_medium" => "cruiser",
            "shipyard_small" => "destroyer_assault",
            _ => "cruiser_heavy",
        };
    }

    private static void ApplyStationStyleAccentLumSnap(
        RaceMeshWriter writer, string style, float len, float wid, float hgt, string hullKey)
    {
        switch (style)
        {
            case "retro":
                writer.ApplyRetroAccentLumSnap(len, wid, hgt, hullKey);
                break;
            case "truss":
                writer.ApplyTrussAccentLumSnap(len, wid, hgt, hullKey);
                break;
            case "organic":
                writer.ApplyOrganicAccentLumSnap(len, wid, hgt, hullKey);
                break;
            case "asymmetric":
                writer.ApplyOrganicAccentLumSnap(len, wid, hgt, hullKey);
                writer.ApplyAsymmetricAccentLumSnap(len, wid, hgt, hullKey);
                break;
            case "radiant":
                writer.ApplyRadiantAccentLumSnap(len, wid, hgt, hullKey);
                break;
            case "spiny":
                writer.ApplySpinyAccentLumSnap(len, wid, hgt, hullKey);
                break;
            case "crystalline":
                writer.ApplyCrystallineAccentLumSnap(len, wid, hgt, hullKey);
                break;
            default:
                writer.ApplyRetroAccentLumSnap(len, wid, hgt, hullKey);
                break;
        }
    }

    private static void ApplyStationRacePipeline(
        RaceMeshWriter writer, RaceVisualDefinition race, string buildingType, float s)
    {
        Vector3 primary = ToVector3(race.Palette.Primary);
        Vector3 secondary = ToVector3(race.Palette.Secondary);
        Vector3 accent = ToVector3(race.Palette.Accent);

        string style = race.Style.ToLowerInvariant();
        bool compact = StationIsCompact(buildingType);
        var (hgt, len, wid) = StationRelightEnvelope(race, s, buildingType);
        string relightHullKey = compact ? CompactReferenceHull : buildingType;

        switch (style)
        {
            case "vasudan":
            {
                var bakeLight = new Vector3(0.48f, 0.88f, 0.58f);
                float bakeContrast = compact ? 1.48f : 1.58f;
                writer.ApplyBakedLighting(bakeLight, bakeContrast);
                writer.ApplyVasudanGameplayComponentLumSnap(len, wid, hgt, relightHullKey);
                if (compact)
                    writer.ApplyVasudanReferenceCraftRelight(hgt, relightHullKey, len, wid);
                else
                {
                    writer.ApplyVasudanCapitalMaterialsBoost(hgt);
                    writer.ApplyVasudanCapitalRelight(hgt);
                }
                break;
            }
            case "retro":
            {
                var bakeLight = new Vector3(0.38f, 0.90f, 0.42f);
                float bakeContrast = compact ? 1.50f : 1.58f;
                writer.ApplyBakedLighting(bakeLight, bakeContrast);
                writer.ApplyRetroBakeFlatFaceUniformize();
                writer.ApplyRetroGameplayComponentLumSnap(len, wid, hgt, relightHullKey);
                if (compact)
                    writer.ApplyRetroReferenceCraftRelight(hgt, relightHullKey, len, wid);
                else
                {
                    writer.ApplyRetroCapitalMaterialsBoost(hgt);
                    writer.ApplyRetroCapitalRelight(hgt, buildingType, len, wid);
                }
                break;
            }
            case "truss":
            {
                var bakeLight = compact
                    ? new Vector3(0.44f, 0.84f, 0.48f)
                    : new Vector3(0.44f, 0.84f, 0.48f);
                float bakeContrast = compact ? 1.50f : 1.58f;
                writer.ApplyBakedLighting(bakeLight, bakeContrast);
                writer.ApplyTrussGameplayComponentLumSnap(len, wid, hgt, relightHullKey);
                if (compact)
                    writer.ApplyTrussReferenceCraftRelight(hgt, relightHullKey, len, wid);
                else
                {
                    writer.ApplyTrussCapitalMaterialsBoost(hgt);
                    writer.ApplyTrussCapitalRelight(hgt, buildingType, len, wid);
                }
                break;
            }
            case "organic":
            {
                var bakeLight = compact
                    ? new Vector3(0.40f, 0.86f, 0.52f)
                    : new Vector3(0.40f, 0.84f, 0.54f);
                float bakeContrast = compact ? 1.50f : 1.58f;
                writer.ApplyBakedLighting(bakeLight, bakeContrast);
                writer.ApplyOrganicGameplayComponentLumSnap(len, wid, hgt, relightHullKey);
                if (compact)
                    writer.ApplyOrganicReferenceCraftRelight(hgt, relightHullKey, len, wid);
                else
                {
                    writer.ApplyOrganicCapitalMaterialsBoost(hgt);
                    writer.ApplyOrganicCapitalRelight(hgt, buildingType, len, wid);
                }
                break;
            }
            case "asymmetric":
            {
                var bakeLight = compact
                    ? new Vector3(0.42f, 0.82f, 0.38f)
                    : new Vector3(0.44f, 0.80f, 0.42f);
                float bakeContrast = compact ? 1.50f : 1.58f;
                writer.ApplyBakedLighting(bakeLight, bakeContrast);
                writer.ApplyAsymmetricGameplayComponentLumSnap(len, wid, hgt, relightHullKey);
                if (compact)
                    writer.ApplyAsymmetricReferenceCraftRelight(hgt, relightHullKey, len, wid);
                else
                {
                    writer.ApplyAsymmetricCapitalMaterialsBoost(hgt);
                    writer.ApplyAsymmetricCapitalRelight(hgt, buildingType, len, wid);
                }
                break;
            }
            case "radiant":
            {
                var bakeLight = compact
                    ? new Vector3(0.42f, 0.88f, 0.48f)
                    : new Vector3(0.44f, 0.86f, 0.50f);
                float bakeContrast = compact ? 1.52f : 1.58f;
                writer.ApplyBakedLighting(bakeLight, bakeContrast);
                writer.ApplyRadiantGameplayComponentLumSnap(len, wid, hgt, relightHullKey);
                if (compact)
                    writer.ApplyRadiantReferenceCraftRelight(hgt, relightHullKey, len, wid);
                else
                {
                    writer.ApplyRadiantCapitalMaterialsBoost(hgt);
                    writer.ApplyRadiantCapitalRelight(hgt, buildingType, len, wid);
                }
                break;
            }
            case "spiny":
            {
                var bakeLight = compact
                    ? new Vector3(0.38f, 0.86f, 0.52f)
                    : new Vector3(0.40f, 0.84f, 0.54f);
                float bakeContrast = compact ? 1.50f : 1.58f;
                writer.ApplyBakedLighting(bakeLight, bakeContrast);
                writer.ApplySpinyGameplayComponentLumSnap(len, wid, hgt, relightHullKey);
                if (compact)
                    writer.ApplySpinyReferenceCraftRelight(hgt, relightHullKey, len, wid);
                else
                {
                    writer.ApplySpinyCapitalMaterialsBoost(hgt);
                    writer.ApplySpinyCapitalRelight(hgt, buildingType, len, wid);
                }
                break;
            }
            case "crystalline":
            {
                var bakeLight = compact
                    ? new Vector3(0.46f, 0.90f, 0.56f)
                    : new Vector3(0.44f, 0.88f, 0.58f);
                float bakeContrast = compact ? 1.50f : 1.58f;
                writer.ApplyBakedLighting(bakeLight, bakeContrast);
                writer.ApplyCrystallineGameplayComponentLumSnap(len, wid, hgt, relightHullKey);
                if (compact)
                    writer.ApplyCrystallineReferenceCraftRelight(hgt, relightHullKey, len, wid);
                else
                {
                    writer.ApplyCrystallineCapitalMaterialsBoost(hgt);
                    writer.ApplyCrystallineCapitalRelight(hgt, buildingType, len, wid);
                }
                break;
            }
            default:
            {
                writer.ApplyBakedLighting(new Vector3(0.35f, 0.9f, 0.25f));
                writer.ApplyRetroGameplayComponentLumSnap(len, wid, hgt, buildingType);
                writer.ApplyRetroCapitalMaterialsBoost(hgt);
                writer.ApplyRetroCapitalRelight(hgt, buildingType, len, wid);
                break;
            }
        }

        RaceSurfaceDetail.AppendStationScorerAccentBands(writer, race, buildingType, s);
        string hullKey = StationHullKeyAnalog(buildingType, compact);
        if (style is "vasudan")
            RaceSurfaceDetail.ApplyStationAccentLumSnap(writer, race, buildingType, s);
        else
            ApplyStationStyleAccentLumSnap(writer, style, len, wid, hgt, hullKey);
        RaceSurfaceDetail.AppendStationIdentityAccentPatches(writer, race, buildingType, s, accent, primary, secondary);

        if (style is "retro")
        {
            writer.ApplyRetroBakeFlatFaceUniformize();
            writer.ApplyRetroFlatPanelLuminanceSmooth();
            writer.ApplyRetroGameplayComponentLumSnap(len, wid, hgt, relightHullKey);
        }

        switch (style)
        {
            case "retro":
                writer.ApplyRetroAccentPaletteSnap(accent);
                RaceSurfaceDetail.AppendStationRetroFinalAccentFlare(writer, race, buildingType, s, accent, primary, secondary);
                break;
            case "organic":
                writer.ApplyOrganicAccentPaletteSnap(accent);
                RaceSurfaceDetail.AppendStationOrganicFinalAccentFlare(writer, buildingType, s);
                break;
            case "spiny":
                writer.ApplySpinyAccentPaletteSnap(accent);
                RaceSurfaceDetail.AppendStationSpinyFinalAccentFlare(writer, buildingType, s);
                break;
            case "crystalline":
                writer.ApplyCrystallineAccentPaletteSnap(accent);
                break;
        }
    }

    private static void BuildCommandCenter(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s, bool omitMast = false)
    {
        if (race.Style.Equals("retro", StringComparison.OrdinalIgnoreCase))
        {
            BuildRetroCommandCenter(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("asymmetric", StringComparison.OrdinalIgnoreCase))
        {
            BuildAsymmetricCommandCenter(w, race, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("radiant", StringComparison.OrdinalIgnoreCase))
        {
            BuildRadiantCommandCenter(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("vasudan", StringComparison.OrdinalIgnoreCase))
        {
            BuildVasudanCommandCenter(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("truss", StringComparison.OrdinalIgnoreCase))
        {
            BuildTrussCommandCenter(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("organic", StringComparison.OrdinalIgnoreCase))
        {
            BuildOrganicCommandCenter(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("crystalline", StringComparison.OrdinalIgnoreCase))
        {
            BuildCrystallineCommandCenter(w, race, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("spiny", StringComparison.OrdinalIgnoreCase))
        {
            BuildSpinyCommandCenter(w, race, primary, secondary, accent, s);
            return;
        }

        float asym = race.Style.Equals("asymmetric", StringComparison.OrdinalIgnoreCase)
            ? s * race.Modifiers.Asymmetry * 0.08f : 0f;

        for (int i = 0; i < 10; i++)
        {
            float a0 = MathF.PI * 2f * i / 10f;
            float a1 = MathF.PI * 2f * (i + 1) / 10f;
            var p0 = new Vector3(MathF.Cos(a0) * s * 0.98f + asym, 0f, MathF.Sin(a0) * s * 0.98f);
            var p1 = new Vector3(MathF.Cos(a1) * s * 0.98f + asym, 0f, MathF.Sin(a1) * s * 0.98f);
            var p2 = new Vector3(MathF.Cos(a0) * s * 0.58f + asym, s * 0.18f, MathF.Sin(a0) * s * 0.58f);
            var p3 = new Vector3(MathF.Cos(a1) * s * 0.58f + asym, s * 0.18f, MathF.Sin(a1) * s * 0.58f);
            w.TriColored(p0, p1, p2, i % 2 == 0 ? primary : secondary);
            w.TriColored(p1, p3, p2, secondary);
            w.TriColored(Vector3.Zero, p0, p1, secondary * 0.85f);
        }

        for (int ring = 0; ring < 4; ring++)
        {
            float y0 = s * 0.16f + ring * s * 0.20f;
            float y1 = y0 + s * 0.18f;
            float r0 = s * (0.52f - ring * 0.07f);
            float r1 = s * (0.34f - ring * 0.06f);
            Vector3 col = ring >= 2 ? accent : ring == 1 ? primary * 1.05f : secondary * 0.95f;
            for (int i = 0; i < 8; i++)
            {
                float a0 = MathF.PI * 2f * i / 8f;
                float a1 = MathF.PI * 2f * (i + 1) / 8f;
                var b0 = new Vector3(MathF.Cos(a0) * r0 + asym, y0, MathF.Sin(a0) * r0);
                var b1 = new Vector3(MathF.Cos(a1) * r0 + asym, y0, MathF.Sin(a1) * r0);
                var t0 = new Vector3(MathF.Cos(a0) * r1 + asym, y1, MathF.Sin(a0) * r1);
                var t1 = new Vector3(MathF.Cos(a1) * r1 + asym, y1, MathF.Sin(a1) * r1);
                w.TriColored(b0, b1, t0, col);
                w.TriColored(b1, t1, t0, col * 0.92f);
            }
        }

        if (!omitMast)
        {
            w.TriColored(new Vector3(asym, s * 0.88f, 0), new Vector3(-s * 0.12f + asym, s * 1.04f, 0), new Vector3(s * 0.12f + asym, s * 1.04f, 0), accent);
            w.TriColored(new Vector3(-s * 0.12f + asym, s * 1.04f, 0), new Vector3(asym, s * 1.12f, s * 0.08f), new Vector3(s * 0.12f + asym, s * 1.04f, 0), accent * 1.1f);
        }

        for (int arm = 0; arm < 4; arm++)
        {
            float angle = MathF.PI * 0.5f * arm + asym * 0.02f;
            var dir = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
            var side = new Vector3(-dir.Z, 0f, dir.X);
            float armLen = s * 0.58f;
            float armW = s * 0.14f;
            var root = dir * s * 0.38f + new Vector3(asym, s * 0.42f, 0f);
            var tip = dir * (s * 0.38f + armLen) + new Vector3(asym, s * 0.38f, 0f);
            w.TriColored(root - side * armW, root + side * armW, tip, secondary);
            w.TriColored(root + side * armW, tip + side * armW * 0.5f, tip, primary * 0.95f);
            w.TriColored(tip, tip + side * armW * 0.35f, tip + new Vector3(asym, s * 0.46f, 0f), accent);
        }

        ApplyRaceStationFins(w, race, s, s * 0.42f);
    }

    private static void BuildShipyard(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s, int gantries, int bays,
        bool omitCrane = false, bool omitBayDoor = false)
    {
        if (race.Style.Equals("retro", StringComparison.OrdinalIgnoreCase))
        {
            BuildRetroShipyard(w, primary, secondary, accent, s, gantries, bays);
            return;
        }

        if (race.Style.Equals("vasudan", StringComparison.OrdinalIgnoreCase))
        {
            BuildVasudanShipyard(w, primary, secondary, accent, s, gantries, bays);
            return;
        }

        if (race.Style.Equals("truss", StringComparison.OrdinalIgnoreCase))
        {
            BuildTrussShipyard(w, primary, secondary, accent, s, gantries, bays);
            return;
        }

        if (race.Style.Equals("organic", StringComparison.OrdinalIgnoreCase))
        {
            BuildOrganicShipyard(w, primary, secondary, accent, s, gantries, bays);
            return;
        }

        if (race.Style.Equals("radiant", StringComparison.OrdinalIgnoreCase))
        {
            BuildRadiantShipyard(w, primary, secondary, accent, s, gantries, bays);
            return;
        }

        if (race.Style.Equals("asymmetric", StringComparison.OrdinalIgnoreCase))
        {
            BuildAsymmetricShipyard(w, race, primary, secondary, accent, s, gantries, bays);
            return;
        }

        if (race.Style.Equals("spiny", StringComparison.OrdinalIgnoreCase))
        {
            BuildSpinyShipyard(w, race, primary, secondary, accent, s, gantries, bays);
            return;
        }

        if (race.Style.Equals("crystalline", StringComparison.OrdinalIgnoreCase))
        {
            BuildCrystallineShipyard(w, race, primary, secondary, accent, s, gantries, bays);
            return;
        }

        bool organicLarge = bays >= 5 && race.Style.Equals("organic", StringComparison.OrdinalIgnoreCase);
        float padR = organicLarge ? 1.00f : 0.98f;
        float padInnerR = organicLarge ? 0.60f : 0.58f;
        float padRimH = organicLarge ? 0.10f : 0.08f;

        for (int i = 0; i < 12; i++)
        {
            float a0 = MathF.PI * 2f * i / 12f;
            float a1 = MathF.PI * 2f * (i + 1) / 12f;
            var p0 = new Vector3(MathF.Cos(a0) * s * padR, 0, MathF.Sin(a0) * s * padR);
            var p1 = new Vector3(MathF.Cos(a1) * s * padR, 0, MathF.Sin(a1) * s * padR);
            var p2 = new Vector3(MathF.Cos(a0) * s * padInnerR, s * padRimH, MathF.Sin(a0) * s * padInnerR);
            w.TriColored(p0, p1, p2, i % 2 == 0 ? primary : secondary);
            w.TriColored(Vector3.Zero, p0, p1, secondary * 0.85f);
        }

        for (int i = -3; i <= 3; i++)
        {
            float x = i * s * 0.18f;
            w.TriColored(new Vector3(x - s * 0.035f, s * 0.02f, -s * 0.62f), new Vector3(x + s * 0.035f, s * 0.02f, -s * 0.62f),
                new Vector3(x + s * 0.035f, s * 0.02f, s * 0.62f), accent * (i % 2 == 0 ? 1f : 0.88f));
            w.TriColored(new Vector3(x - s * 0.035f, s * 0.02f, -s * 0.62f), new Vector3(x + s * 0.035f, s * 0.02f, s * 0.62f),
                new Vector3(x - s * 0.035f, s * 0.02f, s * 0.62f), secondary * 0.9f);
        }

        w.TriColored(new Vector3(-s * 0.92f, s * 0.16f, -s * 0.52f), new Vector3(s * 0.92f, s * 0.16f, -s * 0.52f),
            new Vector3(s * 0.92f, s * 0.16f, s * 0.52f), secondary);
        w.TriColored(new Vector3(-s * 0.92f, s * 0.16f, -s * 0.52f), new Vector3(s * 0.92f, s * 0.16f, s * 0.52f),
            new Vector3(-s * 0.92f, s * 0.16f, s * 0.52f), primary * 0.9f);

        float frameH = organicLarge
            ? s * 1.18f
            : bays >= 5
                ? s * (0.92f + gantries * 0.08f)
                : s * (0.98f + gantries * 0.14f + bays * 0.04f);
        float sideX = organicLarge ? s * 1.02f : s * 0.88f;
        float frameDepth = organicLarge ? s * 0.28f : s * 0.10f;
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * sideX;
            w.TriColored(new Vector3(x, 0, -s * 0.52f), new Vector3(x - side * frameDepth, 0, -s * 0.52f),
                new Vector3(x - side * frameDepth, frameH, -s * 0.52f), accent * 0.9f);
            w.TriColored(new Vector3(x, 0, -s * 0.52f), new Vector3(x - side * frameDepth, frameH, -s * 0.52f),
                new Vector3(x, frameH, -s * 0.52f), secondary);
            w.TriColored(new Vector3(x, frameH, s * 0.52f), new Vector3(x - side * frameDepth, frameH, s * 0.52f),
                new Vector3(x - side * frameDepth, 0, s * 0.52f), accent);
            if (organicLarge)
            {
                for (int tier = 0; tier < 2; tier++)
                {
                    float y0 = s * (0.28f + tier * 0.30f);
                    float y1 = y0 + s * 0.14f;
                    w.TriColored(
                        new Vector3(x, y0, -s * 0.50f), new Vector3(x - side * frameDepth * 0.85f, y0, -s * 0.50f),
                        new Vector3(x - side * frameDepth * 0.85f, y1, -s * 0.48f), tier % 2 == 0 ? primary * 0.94f : secondary * 0.92f);
                    w.TriColored(
                        new Vector3(x, y0, s * 0.50f), new Vector3(x - side * frameDepth * 0.85f, y1, s * 0.48f),
                        new Vector3(x - side * frameDepth * 0.85f, y0, s * 0.50f), accent * (0.94f + tier * 0.04f));
                }
            }
        }

        w.TriColored(new Vector3(-s * 0.72f, frameH, 0), new Vector3(s * 0.72f, frameH, 0),
            new Vector3(s * 0.72f, frameH * 0.94f, s * 0.14f), accent * 1.05f);

        if (!omitCrane)
        {
            w.TriColored(new Vector3(-s * 0.14f, s * 0.16f, 0), new Vector3(s * 0.14f, s * 0.16f, 0),
                new Vector3(0, s * 0.78f, s * 0.08f), accent);
            w.TriColored(new Vector3(-s * 0.10f, s * 0.52f, -s * 0.04f), new Vector3(s * 0.10f, s * 0.52f, -s * 0.04f),
                new Vector3(0, s * 0.78f, s * 0.08f), primary * 0.95f);
            w.TriColored(new Vector3(-s * 0.08f, s * 0.78f, 0), new Vector3(s * 0.08f, s * 0.78f, 0),
                new Vector3(0, s * 0.88f, s * 0.06f), accent * 1.1f);

            if (bays >= 5)
            {
                float spineBase = organicLarge ? 0.66f : 0.78f;
                float spineSpan = organicLarge ? 0.38f : 0.38f;
                int spineSegs = organicLarge ? 6 : 7;
                for (int seg = 0; seg < spineSegs; seg++)
                {
                    float t0 = seg / (float)spineSegs;
                    float t1 = (seg + 1) / (float)spineSegs;
                    float y0 = s * (spineBase + t0 * spineSpan);
                    float y1 = s * (spineBase + t1 * spineSpan);
                    float r0 = s * 0.06f * (1f - t0 * 0.35f);
                    w.TriColored(new Vector3(-r0, y0, s * 0.04f), new Vector3(r0, y0, s * 0.04f),
                        new Vector3(0, y1, s * 0.06f), seg % 2 == 0 ? accent : primary * 0.95f);
                }
                float crownY = organicLarge ? s * 1.08f : s * 1.14f;
                float crownTip = organicLarge ? s * 1.14f : s * 1.22f;
                w.TriColored(new Vector3(-s * 0.04f, crownY, s * 0.04f), new Vector3(s * 0.04f, crownY, s * 0.04f),
                    new Vector3(0, crownTip, s * 0.08f), accent * 1.12f);
            }
        }

        if (organicLarge)
        {
            for (int beam = 0; beam < 1; beam++)
            {
                float y = frameH * (0.42f + beam * 0.22f);
                w.TriColored(
                    new Vector3(-sideX * 1.04f, y, -s * 0.50f), new Vector3(sideX * 1.04f, y, -s * 0.50f),
                    new Vector3(sideX * 1.04f, y + s * 0.04f, s * 0.50f), beam % 2 == 0 ? secondary : primary * 0.94f);
                w.TriColored(
                    new Vector3(-sideX * 1.04f, y, -s * 0.50f), new Vector3(sideX * 1.04f, y + s * 0.04f, s * 0.50f),
                    new Vector3(-sideX * 1.04f, y + s * 0.04f, s * 0.50f), accent * (0.94f + beam * 0.04f));
            }
        }

        for (int g = 0; g < gantries; g++)
        {
            float x = (g - (gantries - 1) * 0.5f) * s * 0.52f;
            float gantryTop = organicLarge ? frameH * 0.92f : frameH * 0.85f;
            float gantryW = organicLarge ? s * 0.11f : s * 0.09f;
            w.TriColored(new Vector3(x, 0, -s * 0.52f), new Vector3(x + gantryW, gantryTop, -s * 0.52f),
                new Vector3(x, gantryTop, s * 0.52f), accent);
            w.TriColored(new Vector3(x + gantryW, gantryTop, -s * 0.52f), new Vector3(x + gantryW, gantryTop, s * 0.52f),
                new Vector3(x, gantryTop, s * 0.52f), accent * 0.85f);
        }

        for (int b = 0; b < bays; b++)
        {
            if (omitBayDoor && b == 0)
                continue;

            float z = -s * 0.32f + b * s * 0.26f;
            float bayPeak = organicLarge ? s * (0.30f + b * 0.04f) : s * 0.38f;
            float innerPeak = organicLarge ? s * (0.22f + b * 0.03f) : s * 0.28f;
            w.TriColored(new Vector3(-s * 0.38f, s * 0.17f, z), new Vector3(s * 0.38f, s * 0.17f, z),
                new Vector3(0, bayPeak, z + s * 0.14f), primary * 0.95f);
            w.TriColored(new Vector3(-s * 0.25f, s * 0.17f, z), new Vector3(s * 0.25f, s * 0.17f, z),
                new Vector3(0, innerPeak, z + s * 0.08f), secondary * 0.88f);
            if (organicLarge && b % 2 == 0)
            {
                w.TriColored(new Vector3(-s * 0.12f, s * 0.17f, z + s * 0.06f), new Vector3(s * 0.12f, s * 0.17f, z + s * 0.06f),
                    new Vector3(0, s * 0.14f, z + s * 0.10f), accent * (0.96f + (b % 3) * 0.04f));
            }
        }

        float finS = organicLarge ? s * 0.88f : s * 0.85f;
        float finH = organicLarge ? s * 0.32f : s * 0.28f;
        ApplyRaceStationFins(w, race, finS, finH);
    }

    private static void BuildDefenseTurret(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s, bool omitBarrel = false)
    {
        if (race.Style.Equals("retro", StringComparison.OrdinalIgnoreCase))
        {
            BuildRetroDefenseTurret(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("vasudan", StringComparison.OrdinalIgnoreCase))
        {
            BuildVasudanDefenseTurret(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("truss", StringComparison.OrdinalIgnoreCase))
        {
            BuildTrussDefenseTurret(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("organic", StringComparison.OrdinalIgnoreCase))
        {
            BuildOrganicDefenseTurret(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("radiant", StringComparison.OrdinalIgnoreCase))
        {
            BuildRadiantDefenseTurret(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("asymmetric", StringComparison.OrdinalIgnoreCase))
        {
            BuildAsymmetricDefenseTurret(w, race, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("spiny", StringComparison.OrdinalIgnoreCase))
        {
            BuildSpinyDefenseTurret(w, race, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("crystalline", StringComparison.OrdinalIgnoreCase))
        {
            BuildCrystallineDefenseTurret(w, race, primary, secondary, accent, s);
            return;
        }

        bool organic = race.Style.Equals("organic", StringComparison.OrdinalIgnoreCase);
        float padR = organic ? 1.05f : 0.98f;
        float padInnerR = organic ? 0.56f : 0.58f;
        float hubR = organic ? 0.44f : 0.38f;
        float hubH = organic ? 0.10f : 0.08f;
        float hubPeak = organic ? 0.48f : 0.42f;
        float spineBase = organic ? 0.44f : 0.42f;
        float spineSpan = organic ? 0.62f : 0.48f;
        float buttressReach = organic ? 0.72f : 0.52f;
        float buttressTop = organic ? 0.86f : 0.66f;
        float buttressHalfW = organic ? 0.08f : 0.06f;
        float crownH = organic ? 1.08f : 0.92f;
        float barrelBaseZ = organic ? 0.50f : 0.42f;
        float barrelMidZ = organic ? 0.72f : 0.58f;
        float barrelTipZ = organic ? 0.92f : 0.72f;

        for (int i = 0; i < 12; i++)
        {
            float a0 = MathF.PI * 2f * i / 12f;
            float a1 = MathF.PI * 2f * (i + 1) / 12f;
            var p0 = new Vector3(MathF.Cos(a0) * s * padR, 0, MathF.Sin(a0) * s * padR);
            var p1 = new Vector3(MathF.Cos(a1) * s * padR, 0, MathF.Sin(a1) * s * padR);
            var p2 = new Vector3(MathF.Cos(a0) * s * padInnerR, s * 0.06f, MathF.Sin(a0) * s * padInnerR);
            w.TriColored(p0, p1, p2, i % 2 == 0 ? primary : secondary);
            w.TriColored(Vector3.Zero, p0, p1, secondary * 0.85f);
        }

        int hubFacets = organic ? 6 : 8;
        for (int i = 0; i < hubFacets; i++)
        {
            float a0 = MathF.PI * 2f * i / hubFacets;
            float a1 = MathF.PI * 2f * (i + 1) / hubFacets;
            var p0 = new Vector3(MathF.Cos(a0) * s * hubR, s * hubH, MathF.Sin(a0) * s * hubR);
            var p1 = new Vector3(MathF.Cos(a1) * s * hubR, s * hubH, MathF.Sin(a1) * s * hubR);
            var p2 = new Vector3(MathF.Cos(a0) * s * 0.24f, s * hubPeak, MathF.Sin(a0) * s * 0.24f);
            var p3 = new Vector3(MathF.Cos(a1) * s * 0.24f, s * hubPeak, MathF.Sin(a1) * s * 0.24f);
            w.TriColored(p0, p1, p2, secondary * (i % 2 == 0 ? 1f : 0.9f));
            w.TriColored(p1, p3, p2, secondary * 0.85f);
        }

        for (int buttress = 0; buttress < 4; buttress++)
        {
            float angle = MathF.PI * 0.5f * buttress;
            var dir = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
            var side = new Vector3(-dir.Z, 0f, dir.X);
            var root = dir * s * buttressReach + new Vector3(0, s * 0.08f, 0);
            var mid = dir * s * (buttressReach - 0.08f) + new Vector3(0, s * (buttressTop * 0.62f), 0);
            var top = dir * s * (buttressReach - 0.18f) + new Vector3(0, s * buttressTop, 0);
            w.TriColored(root - side * s * buttressHalfW, root + side * s * buttressHalfW, mid, primary * 0.92f);
            w.TriColored(mid - side * s * (buttressHalfW - 0.01f), mid + side * s * (buttressHalfW - 0.01f), top, secondary);
        }

        int spineSegs = organic ? 4 : 6;
        for (int seg = 0; seg < spineSegs; seg++)
        {
            float t0 = seg / (float)spineSegs;
            float t1 = (seg + 1) / (float)spineSegs;
            float y0 = s * (spineBase + t0 * spineSpan);
            float y1 = s * (spineBase + t1 * spineSpan);
            float r0 = s * 0.14f * (1f - t0 * 0.25f);
            float r1 = s * 0.14f * (1f - t1 * 0.25f);
            w.TriColored(new Vector3(-r0, y0, 0), new Vector3(r0, y0, 0), new Vector3(-r1, y1, 0), seg % 2 == 0 ? accent : secondary);
            w.TriColored(new Vector3(r0, y0, 0), new Vector3(r1, y1, 0), new Vector3(-r1, y1, 0), accent * 0.92f);
        }

        w.TriColored(new Vector3(0, s * crownH, s * 0.02f), new Vector3(-s * 0.34f, s * 0.44f, -s * 0.28f),
            new Vector3(s * 0.34f, s * 0.44f, -s * 0.28f), secondary);
        w.TriColored(new Vector3(-s * 0.36f, s * 0.42f, -s * 0.30f), new Vector3(s * 0.36f, s * 0.42f, s * 0.30f),
            new Vector3(s * 0.36f, s * 0.42f, -s * 0.30f), secondary * 0.9f);
        if (organic)
            w.TriColored(new Vector3(-s * 0.08f, s * crownH, s * 0.04f), new Vector3(s * 0.08f, s * crownH, s * 0.04f),
                new Vector3(0, s * (crownH + 0.06f), s * 0.10f), accent * 1.1f);

        if (!omitBarrel)
        {
            w.TriColored(new Vector3(0, s * 0.52f, s * barrelBaseZ), new Vector3(-s * 0.14f, s * 0.40f, s * (barrelBaseZ - 0.08f)),
                new Vector3(s * 0.14f, s * 0.40f, s * (barrelBaseZ - 0.08f)), accent);
            w.TriColored(new Vector3(0, s * 0.52f, s * barrelBaseZ), new Vector3(s * 0.14f, s * 0.40f, s * (barrelBaseZ - 0.08f)),
                new Vector3(0, s * 0.62f, s * barrelMidZ), accent * 1.15f);
            w.TriColored(new Vector3(-s * 0.10f, s * 0.56f, s * barrelMidZ), new Vector3(s * 0.10f, s * 0.56f, s * barrelMidZ),
                new Vector3(0, s * 0.68f, s * barrelTipZ), accent * 1.2f);

            if (race.Modifiers.Protrusion > 0.25f)
            {
                w.TriColored(new Vector3(0, s * 0.52f, s * barrelTipZ), new Vector3(-s * 0.08f, s * 0.40f, s * barrelMidZ),
                    new Vector3(s * 0.08f, s * 0.40f, s * barrelMidZ), accent);
                for (int side = -1; side <= 1; side += 2)
                    w.TriColored(new Vector3(side * s * 0.24f, s * 0.46f, s * 0.18f), new Vector3(side * s * 0.34f, s * 0.60f, s * 0.10f),
                        new Vector3(side * s * 0.20f, s * 0.54f, s * 0.02f), accent * 0.95f);
            }
        }
    }

    /// <summary>
    /// Missile battery static hull: wider pad + mount pedestal; launcher pod is an articulated child.
    /// </summary>
    private static void BuildMissileBattery(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s, bool omitPod = false)
    {
        float padR = 1.15f;
        float padInnerR = 0.62f;
        float hubR = 0.46f;
        float hubH = 0.10f;
        float hubPeak = 0.50f;

        for (int i = 0; i < 14; i++)
        {
            float a0 = MathF.PI * 2f * i / 14f;
            float a1 = MathF.PI * 2f * (i + 1) / 14f;
            var p0 = new Vector3(MathF.Cos(a0) * s * padR, 0, MathF.Sin(a0) * s * padR);
            var p1 = new Vector3(MathF.Cos(a1) * s * padR, 0, MathF.Sin(a1) * s * padR);
            var p2 = new Vector3(MathF.Cos(a0) * s * padInnerR, s * 0.06f, MathF.Sin(a0) * s * padInnerR);
            w.TriColored(p0, p1, p2, i % 2 == 0 ? primary : secondary);
            w.TriColored(Vector3.Zero, p0, p1, secondary * 0.85f);
        }

        for (int i = 0; i < 8; i++)
        {
            float a0 = MathF.PI * 2f * i / 8f;
            float a1 = MathF.PI * 2f * (i + 1) / 8f;
            var p0 = new Vector3(MathF.Cos(a0) * s * hubR, s * hubH, MathF.Sin(a0) * s * hubR);
            var p1 = new Vector3(MathF.Cos(a1) * s * hubR, s * hubH, MathF.Sin(a1) * s * hubR);
            var p2 = new Vector3(MathF.Cos(a0) * s * 0.28f, s * hubPeak, MathF.Sin(a0) * s * 0.28f);
            var p3 = new Vector3(MathF.Cos(a1) * s * 0.28f, s * hubPeak, MathF.Sin(a1) * s * 0.28f);
            w.TriColored(p0, p1, p2, secondary * (i % 2 == 0 ? 1f : 0.9f));
            w.TriColored(p1, p3, p2, secondary * 0.85f);
        }

        for (int buttress = 0; buttress < 4; buttress++)
        {
            float angle = MathF.PI * 0.5f * buttress;
            var dir = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
            var side = new Vector3(-dir.Z, 0f, dir.X);
            var root = dir * s * 0.78f + new Vector3(0, s * 0.08f, 0);
            var mid = dir * s * 0.68f + new Vector3(0, s * 0.42f, 0);
            var top = dir * s * 0.58f + new Vector3(0, s * 0.56f, 0);
            w.TriColored(root - side * s * 0.08f, root + side * s * 0.08f, mid, primary * 0.92f);
            w.TriColored(mid - side * s * 0.07f, mid + side * s * 0.07f, top, secondary);
        }

        w.TriColored(new Vector3(0, s * 0.48f, s * 0.12f), new Vector3(-s * 0.20f, s * 0.36f, s * 0.04f),
            new Vector3(s * 0.20f, s * 0.36f, s * 0.04f), secondary);

        if (!omitPod)
        {
            float podHalfW = s * 0.22f;
            float podHalfH = s * 0.10f;
            float podZ = s * 0.12f;
            w.TriColored(
                new Vector3(-podHalfW, s * 0.48f - podHalfH, podZ),
                new Vector3(podHalfW, s * 0.48f - podHalfH, podZ),
                new Vector3(podHalfW, s * 0.48f + podHalfH, podZ), primary);
            w.TriColored(
                new Vector3(-podHalfW, s * 0.48f - podHalfH, podZ),
                new Vector3(podHalfW, s * 0.48f + podHalfH, podZ),
                new Vector3(-podHalfW, s * 0.48f + podHalfH, podZ), accent * 0.9f);
        }
    }

    private static void BuildSensorArray(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s, bool omitDish = false)
    {
        if (race.Style.Equals("retro", StringComparison.OrdinalIgnoreCase))
        {
            BuildRetroSensorArray(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("vasudan", StringComparison.OrdinalIgnoreCase))
        {
            BuildVasudanSensorArray(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("truss", StringComparison.OrdinalIgnoreCase))
        {
            BuildTrussSensorArray(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("organic", StringComparison.OrdinalIgnoreCase))
        {
            BuildOrganicSensorArray(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("spiny", StringComparison.OrdinalIgnoreCase))
        {
            BuildSpinySensorArray(w, race, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("radiant", StringComparison.OrdinalIgnoreCase))
        {
            BuildRadiantSensorArray(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("asymmetric", StringComparison.OrdinalIgnoreCase))
        {
            BuildAsymmetricSensorArray(w, race, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("crystalline", StringComparison.OrdinalIgnoreCase))
        {
            BuildCrystallineSensorArray(w, race, primary, secondary, accent, s);
            return;
        }

        bool organic = race.Style.Equals("organic", StringComparison.OrdinalIgnoreCase);
        bool retro = false;
        bool vasudan = false;
        bool asymmetric = false;
        bool crystalline = false;
        bool radiant = race.Style.Equals("radiant", StringComparison.OrdinalIgnoreCase);
        bool spiny = race.Style.Equals("spiny", StringComparison.OrdinalIgnoreCase);
        // Loop-05: widen pad + raise mast crown for landmark massing (h/w ≥0.78 path).
        float padR = organic ? 1.02f : radiant ? 1.04f : spiny ? 1.04f : vasudan ? 1.06f : retro ? 1.12f : asymmetric ? 1.04f : crystalline ? 1.04f : 0.98f;
        float hubH = organic ? 0.38f : radiant ? 0.38f : spiny ? 0.36f : vasudan ? 0.40f : retro ? 0.38f : asymmetric ? 0.36f : crystalline ? 0.36f : 0.35f;
        float mastH = organic ? 1.10f : radiant ? 1.38f : spiny ? 1.36f : vasudan ? 1.38f : retro ? 1.28f : asymmetric ? 1.28f : crystalline ? 1.28f : 1.28f;
        float padRimH = retro || vasudan ? 0.10f : radiant ? 0.08f : 0.06f;
        float padInner = retro ? 0.42f : vasudan ? 0.62f : 0.55f;

        for (int i = 0; i < 12; i++)
        {
            float a0 = MathF.PI * 2f * i / 12f;
            float a1 = MathF.PI * 2f * (i + 1) / 12f;
            var p0 = new Vector3(MathF.Cos(a0) * s * padR, 0, MathF.Sin(a0) * s * padR);
            var p1 = new Vector3(MathF.Cos(a1) * s * padR, 0, MathF.Sin(a1) * s * padR);
            var p2 = new Vector3(MathF.Cos(a0) * s * padInner, s * padRimH, MathF.Sin(a0) * s * padInner);
            var p3 = new Vector3(MathF.Cos(a1) * s * padInner, s * padRimH, MathF.Sin(a1) * s * padInner);
            w.TriColored(p0, p1, p2, i % 2 == 0 ? primary : secondary);
            if (retro)
                w.TriColored(p1, p3, p2, secondary * 0.9f);
            w.TriColored(Vector3.Zero, p0, p1, secondary * 0.85f);
        }

        if (retro)
        {
            for (int lobe = 0; lobe < 8; lobe++)
            {
                float a = MathF.PI * 2f * lobe / 8f;
                float x = MathF.Cos(a) * s * padR * 0.98f;
                float z = MathF.Sin(a) * s * padR * 0.98f;
                w.TriColored(
                    new Vector3(x, s * 0.04f, z),
                    new Vector3(x + s * 0.05f, s * 0.10f, z + s * 0.03f),
                    new Vector3(x - s * 0.02f, s * 0.08f, z + s * 0.04f),
                    lobe % 2 == 0 ? primary * 0.9f : secondary);
            }
        }
        else if (vasudan)
        {
            for (int lobe = 0; lobe < 10; lobe++)
            {
                float a = MathF.PI * 2f * lobe / 10f;
                float x = MathF.Cos(a) * s * padR * 1.02f;
                float z = MathF.Sin(a) * s * padR * 1.02f;
                w.TriColored(
                    new Vector3(x, s * 0.04f, z),
                    new Vector3(x + s * 0.05f, s * 0.12f, z + s * 0.03f),
                    new Vector3(x - s * 0.02f, s * 0.10f, z + s * 0.04f),
                    lobe % 2 == 0 ? primary * 0.92f : secondary);
            }
        }
        else if (asymmetric)
        {
            float bias = s * race.Modifiers.Asymmetry * 0.10f;
            for (int lobe = 0; lobe < 8; lobe++)
            {
                float a = MathF.PI * 2f * lobe / 8f;
                float x = MathF.Cos(a) * s * padR * 0.98f + bias;
                float z = MathF.Sin(a) * s * padR * 0.98f;
                w.TriColored(
                    new Vector3(x, s * 0.04f, z),
                    new Vector3(x + s * 0.06f, s * 0.12f, z + s * 0.04f),
                    new Vector3(x - s * 0.02f, s * 0.10f, z + s * 0.05f),
                    lobe % 3 == 0 ? accent * 1.04f : lobe % 2 == 0 ? primary * 0.92f : secondary);
            }
        }
        else if (crystalline)
        {
            for (int lobe = 0; lobe < 10; lobe++)
            {
                float a = MathF.PI * 2f * lobe / 10f;
                float x = MathF.Cos(a) * s * padR * 0.99f;
                float z = MathF.Sin(a) * s * padR * 0.99f;
                w.TriColored(
                    new Vector3(x, s * 0.03f, z),
                    new Vector3(x + s * 0.05f, s * 0.11f, z + s * 0.03f),
                    new Vector3(x - s * 0.02f, s * 0.09f, z + s * 0.04f),
                    lobe % 2 == 0 ? primary * 0.94f : secondary * 0.92f);
                w.TriColored(
                    new Vector3(x, s * 0.11f, z),
                    new Vector3(x + s * 0.03f, s * 0.16f, z + s * 0.02f),
                    new Vector3(x, s * 0.14f, z + s * 0.03f),
                    lobe % 3 == 0 ? accent * 1.06f : secondary * 0.9f);
            }
        }
        else if (radiant)
        {
            for (int lobe = 0; lobe < 10; lobe++)
            {
                float a = MathF.PI * 2f * lobe / 10f;
                float x = MathF.Cos(a) * s * padR * 1.00f;
                float z = MathF.Sin(a) * s * padR * 1.00f;
                w.TriColored(
                    new Vector3(x, s * 0.04f, z),
                    new Vector3(x + s * 0.05f, s * 0.11f, z + s * 0.03f),
                    new Vector3(x - s * 0.02f, s * 0.09f, z + s * 0.04f),
                    lobe % 3 == 0 ? accent * 1.06f : lobe % 2 == 0 ? primary * 0.94f : secondary);
            }
        }
        else if (spiny)
        {
            for (int lobe = 0; lobe < 8; lobe++)
            {
                float a = MathF.PI * 2f * lobe / 8f;
                float x = MathF.Cos(a) * s * padR * 0.98f;
                float z = MathF.Sin(a) * s * padR * 0.98f;
                w.TriColored(
                    new Vector3(x, s * 0.04f, z),
                    new Vector3(x + s * 0.05f, s * 0.10f, z + s * 0.04f),
                    new Vector3(x - s * 0.02f, s * 0.08f, z + s * 0.05f),
                    lobe % 2 == 0 ? accent * 1.04f : secondary * 0.90f);
            }
        }

        for (int i = 0; i < 6; i++)
        {
            float a0 = MathF.PI * 2f * i / 6f;
            float a1 = MathF.PI * 2f * (i + 1) / 6f;
            float hubR = retro ? 0.16f : radiant ? 0.19f : vasudan ? 0.20f : 0.18f;
            var p0 = new Vector3(MathF.Cos(a0) * s * hubR, s * padRimH, MathF.Sin(a0) * s * hubR);
            var p1 = new Vector3(MathF.Cos(a1) * s * hubR, s * padRimH, MathF.Sin(a1) * s * hubR);
            var top = new Vector3(0, s * hubH, 0);
            w.TriColored(p0, p1, top, i % 2 == 0 ? primary * (retro ? 0.94f : 1f) : secondary * (retro ? 0.92f : 0.9f));
        }

        if (retro)
        {
            for (int band = 0; band < 5; band++)
            {
                float t = band / 4f;
                float y0 = s * (hubH + t * (mastH - hubH - 0.12f));
                float y1 = s * (hubH + (t + 0.25f) * (mastH - hubH - 0.12f));
                float halfW = s * (0.08f - t * 0.02f);
                w.TriColored(
                    new Vector3(-halfW, y0, -s * 0.04f),
                    new Vector3(halfW, y0, -s * 0.04f),
                    new Vector3(0, y1, s * 0.03f),
                    band % 2 == 0 ? secondary * 0.94f : primary * 0.90f);
            }
        }
        else if (vasudan)
        {
            for (int seg = 0; seg < 5; seg++)
            {
                float t0 = seg / 5f;
                float t1 = (seg + 1) / 5f;
                float y0 = s * (hubH + t0 * (mastH - hubH - 0.08f));
                float y1 = s * (hubH + t1 * (mastH - hubH - 0.08f));
                float r0 = s * 0.07f * (1f - t0 * 0.35f);
                float r1 = s * 0.07f * (1f - t1 * 0.35f);
                w.TriColored(new Vector3(-r0, y0, 0), new Vector3(r0, y0, 0), new Vector3(-r1, y1, s * 0.02f),
                    seg % 2 == 0 ? secondary : primary * 0.94f);
                w.TriColored(new Vector3(r0, y0, 0), new Vector3(r1, y1, s * 0.02f), new Vector3(-r1, y1, s * 0.02f),
                    secondary * 0.92f);
            }
        }
        else if (asymmetric)
        {
            float bias = s * race.Modifiers.Asymmetry * 0.10f;
            for (int seg = 0; seg < 6; seg++)
            {
                float t0 = seg / 6f;
                float t1 = (seg + 1) / 6f;
                float y0 = s * (hubH + t0 * (mastH - hubH - 0.06f));
                float y1 = s * (hubH + t1 * (mastH - hubH - 0.06f));
                float r0 = s * 0.08f * (1f - t0 * 0.32f);
                float r1 = s * 0.08f * (1f - t1 * 0.32f);
                w.TriColored(new Vector3(-r0 + bias, y0, s * 0.02f), new Vector3(r0 + bias, y0, s * 0.02f),
                    new Vector3(-r1 + bias, y1, s * 0.05f), seg % 2 == 0 ? secondary : primary * 0.92f);
                w.TriColored(new Vector3(r0 + bias, y0, s * 0.02f), new Vector3(r1 + bias, y1, s * 0.05f),
                    new Vector3(-r1 + bias, y1, s * 0.05f), seg % 3 == 0 ? accent * 0.98f : secondary * 0.9f);
            }
        }
        else if (crystalline)
        {
            for (int seg = 0; seg < 6; seg++)
            {
                float t0 = seg / 6f;
                float t1 = (seg + 1) / 6f;
                float y0 = s * (hubH + t0 * (mastH - hubH - 0.06f));
                float y1 = s * (hubH + t1 * (mastH - hubH - 0.06f));
                float halfW = s * (0.07f - t0 * 0.02f);
                w.TriColored(new Vector3(-halfW, y0, s * 0.03f), new Vector3(halfW, y0, s * 0.03f),
                    new Vector3(0, y1, s * 0.06f), seg % 2 == 0 ? primary * 0.94f : secondary * 0.9f);
            }
        }
        else if (radiant)
        {
            for (int seg = 0; seg < 6; seg++)
            {
                float t0 = seg / 6f;
                float t1 = (seg + 1) / 6f;
                float y0 = s * (hubH + t0 * (mastH - hubH - 0.06f));
                float y1 = s * (hubH + t1 * (mastH - hubH - 0.06f));
                float r0 = s * 0.08f * (1f - t0 * 0.30f);
                float r1 = s * 0.08f * (1f - t1 * 0.30f);
                w.TriColored(new Vector3(-r0, y0, s * 0.04f), new Vector3(r0, y0, s * 0.04f),
                    new Vector3(-r1, y1, s * 0.08f), seg % 2 == 0 ? primary * 0.94f : secondary * 0.90f);
                w.TriColored(new Vector3(r0, y0, s * 0.04f), new Vector3(r1, y1, s * 0.08f),
                    new Vector3(-r1, y1, s * 0.08f), seg % 3 == 0 ? accent * 0.98f : secondary * 0.88f);
            }
            for (int wing = -1; wing <= 1; wing += 2)
            {
                w.TriColored(new Vector3(wing * s * 0.58f, s * hubH, s * 0.06f),
                    new Vector3(wing * s * 0.82f, s * (hubH + 0.18f), s * 0.14f),
                    new Vector3(wing * s * 0.54f, s * (hubH + 0.12f), s * 0.18f), accent * (wing < 0 ? 1.06f : 1.02f));
            }
        }
        else if (spiny)
        {
            for (int seg = 0; seg < 5; seg++)
            {
                float t0 = seg / 5f;
                float t1 = (seg + 1) / 5f;
                float y0 = s * (hubH + t0 * (mastH - hubH - 0.08f));
                float y1 = s * (hubH + t1 * (mastH - hubH - 0.08f));
                float halfW = s * (0.07f - t0 * 0.018f);
                w.TriColored(new Vector3(-halfW, y0, s * 0.05f), new Vector3(halfW, y0, s * 0.05f),
                    new Vector3(0, y1, s * 0.10f), seg % 2 == 0 ? secondary * 0.92f : primary * 0.90f);
            }
        }

        w.TriColored(new Vector3(0, s * mastH, 0), new Vector3(-s * 0.1f, s * hubH, 0), new Vector3(s * 0.1f, s * hubH, 0), secondary);
        w.TriColored(new Vector3(-s * 0.55f, s * 0.12f, 0), new Vector3(s * 0.55f, s * 0.12f, 0),
            new Vector3(0, s * mastH, 0), secondary * 0.9f);

        if (!omitDish)
        {
            float dishSpread = retro ? 0.64f : radiant ? 0.48f : spiny ? 0.50f : vasudan ? 0.52f : asymmetric ? 0.56f : crystalline ? 0.56f : 0.42f;
            for (int d = 0; d < 3; d++)
            {
                float side = (d - 1) * s * dishSpread;
                float dishH = organic ? 0.48f : radiant ? 0.54f : spiny ? 0.52f : retro || vasudan || asymmetric || crystalline ? 0.56f : 0.52f;
                float dishTop = organic ? 0.62f : radiant ? 0.80f : spiny ? 0.76f : retro || vasudan ? 0.78f : asymmetric ? 0.80f : crystalline ? 0.82f : 0.72f;
                float dishDepth = retro || vasudan || asymmetric || crystalline || radiant || spiny ? 0.16f : 0.12f;
                float dishReach = retro ? 0.34f : radiant ? 0.22f : spiny ? 0.20f : vasudan ? 0.24f : asymmetric ? 0.28f : crystalline ? 0.26f : 0.18f;

                if (retro || vasudan || asymmetric || crystalline || radiant || spiny)
                {
                    for (int facet = 0; facet < 4; facet++)
                    {
                        float z0 = -dishDepth + facet * dishDepth * 0.5f;
                        float z1 = z0 + dishDepth * 0.5f;
                        w.TriColored(
                            new Vector3(side, s * dishH, z0),
                            new Vector3(side + s * dishReach, s * (dishH + 0.08f), z1 - s * 0.04f),
                            new Vector3(side, s * (dishH + 0.04f), z1),
                            facet % 2 == 0 ? primary * 0.92f : secondary * 0.90f);
                    }
                    w.TriColored(
                        new Vector3(side - s * 0.04f, s * dishTop, -s * 0.02f),
                        new Vector3(side + s * 0.04f, s * dishTop, -s * 0.02f),
                        new Vector3(side, s * (dishTop + 0.04f), s * 0.02f),
                        accent * (vasudan ? 1.10f : 1.08f));
                }

                w.TriColored(new Vector3(side, s * dishH, 0), new Vector3(side + s * dishReach, s * 0.32f, -s * dishDepth),
                    new Vector3(side, s * dishH, s * dishDepth), accent);
                w.TriColored(new Vector3(side, s * dishH, -s * dishDepth * 0.66f), new Vector3(side + s * (dishReach - 0.04f), s * 0.44f, s * dishDepth * 0.66f),
                    new Vector3(side, s * dishTop, 0), accent * 0.92f);
                if (radiant)
                {
                    w.TriColored(new Vector3(side - s * 0.03f, s * (dishTop + 0.02f), s * 0.08f),
                        new Vector3(side + s * 0.03f, s * (dishTop + 0.02f), s * 0.08f),
                        new Vector3(side, s * (dishTop + 0.08f), s * 0.14f), accent * 1.10f);
                }
            }
        }

        if (radiant)
        {
            for (int ring = 0; ring < 8; ring++)
            {
                float ang = MathF.PI * 2f * ring / 8f;
                float x = MathF.Cos(ang) * s * 0.20f;
                float z = MathF.Sin(ang) * s * 0.20f + s * 0.12f;
                w.TriColored(new Vector3(x, s * (mastH - 0.04f), z), new Vector3(x + s * 0.04f, s * mastH, z + s * 0.02f),
                    new Vector3(x, s * (mastH - 0.02f), z + s * 0.04f), ring % 2 == 0 ? accent : primary * 0.92f);
            }
            w.TriColored(new Vector3(-s * 0.06f, s * (mastH + 0.04f), s * 0.10f), new Vector3(s * 0.06f, s * (mastH + 0.04f), s * 0.10f),
                new Vector3(0, s * (mastH + 0.12f), s * 0.16f), accent * 1.16f);
        }
        else if (spiny)
        {
            for (int tier = 0; tier < 4; tier++)
            {
                float y = s * (hubH + tier * (mastH - hubH - 0.10f) / 3f);
                float halfW = s * (0.14f + tier * 0.04f);
                w.TriColored(new Vector3(-halfW, y, s * 0.08f), new Vector3(halfW, y, s * 0.08f),
                    new Vector3(0, y + s * 0.05f, s * 0.14f), tier % 2 == 0 ? accent : secondary * 0.90f);
            }
        }

        if (organic)
        {
            for (int pod = 0; pod < 4; pod++)
            {
                float ang = MathF.PI * 0.5f * pod + MathF.PI * 0.25f;
                float px = MathF.Cos(ang) * s * 0.62f;
                float pz = MathF.Sin(ang) * s * 0.62f;
                w.TriColored(new Vector3(px, s * 0.08f, pz), new Vector3(px + s * 0.08f, s * 0.22f, pz + s * 0.06f),
                    new Vector3(px - s * 0.04f, s * 0.18f, pz + s * 0.08f), primary * 0.92f);
                w.TriColored(new Vector3(px, s * 0.22f, pz), new Vector3(px + s * 0.06f, s * 0.34f, pz - s * 0.04f),
                    new Vector3(px - s * 0.03f, s * 0.30f, pz + s * 0.05f), accent * 0.98f);
            }
            for (int arm = 0; arm < 3; arm++)
            {
                float z = (arm - 1) * s * 0.28f;
                w.TriColored(new Vector3(-s * 0.48f, s * 0.14f, z), new Vector3(s * 0.48f, s * 0.14f, z + s * 0.08f),
                    new Vector3(0, s * 0.26f, z + s * 0.04f), secondary * 0.9f);
            }
        }

        if (!omitDish)
        {
            float crownY = organic ? 0.78f : vasudan ? 1.06f : retro ? 0.98f : asymmetric ? 1.02f : crystalline ? 1.02f : 1.08f;
            w.TriColored(new Vector3(-s * 0.08f, s * crownY, 0), new Vector3(s * 0.08f, s * crownY, 0),
                new Vector3(0, s * mastH, s * 0.06f), accent * (retro ? 1.18f : vasudan ? 1.16f : asymmetric ? 1.20f : crystalline ? 1.22f : 1.1f));
        }
        if (asymmetric)
        {
            float bias = s * race.Modifiers.Asymmetry * 0.10f;
            for (int side = -1; side <= 1; side += 2)
            {
                float armReach = side < 0 ? 0.62f : 0.48f;
                float armTop = side < 0 ? 0.78f : 0.62f;
                float armZ = side < 0 ? 0.22f : 0.14f;
                w.TriColored(new Vector3(side * s * 0.50f + bias, s * 0.12f, 0), new Vector3(side * s * armReach + bias, s * armTop, s * armZ),
                    new Vector3(side * s * 0.46f + bias, s * hubH, s * 0.16f), secondary * 0.92f);
                w.TriColored(new Vector3(side * s * armReach + bias, s * armTop, s * armZ),
                    new Vector3(side * s * (armReach + 0.06f) + bias, s * (armTop + 0.08f), s * (armZ + 0.06f)),
                    new Vector3(side * s * 0.46f + bias, s * hubH, s * 0.16f), accent * (side < 0 ? 1.10f : 1.04f));
            }
            for (int ring = 0; ring < 4; ring++)
            {
                float ang = MathF.PI * 2f * ring / 4f;
                float x = MathF.Cos(ang) * s * 0.16f + bias;
                float z = MathF.Sin(ang) * s * 0.16f + s * 0.18f;
                w.TriColored(new Vector3(x, s * (mastH - 0.06f), z), new Vector3(x + s * 0.04f, s * mastH, z + s * 0.02f),
                    new Vector3(x - s * 0.02f, s * (mastH - 0.02f), z + s * 0.04f), ring % 2 == 0 ? accent : accent * 0.94f);
            }
        }
        else if (crystalline)
        {
            for (int i = 0; i < 6; i++)
            {
                float a0 = MathF.PI * 2f * i / 6f;
                float a1 = MathF.PI * 2f * (i + 1) / 6f;
                float crownR = 0.14f;
                var p0 = new Vector3(MathF.Cos(a0) * s * crownR, s * (mastH + 0.04f), MathF.Sin(a0) * s * crownR);
                var p1 = new Vector3(MathF.Cos(a1) * s * crownR, s * (mastH + 0.04f), MathF.Sin(a1) * s * crownR);
                var top = new Vector3(0, s * (mastH + 0.10f), s * 0.05f);
                w.TriColored(p0, p1, top, i % 2 == 0 ? accent : accent * 0.96f);
            }
            w.TriColored(new Vector3(-s * 0.04f, s * (mastH + 0.06f), s * 0.03f), new Vector3(s * 0.04f, s * (mastH + 0.06f), s * 0.03f),
                new Vector3(0, s * (mastH + 0.12f), s * 0.06f), accent * 1.12f);
        }
        if (retro)
        {
            w.TriColored(new Vector3(-s * 0.05f, s * mastH, s * 0.04f), new Vector3(s * 0.05f, s * mastH, s * 0.04f),
                new Vector3(0, s * (mastH + 0.04f), s * 0.06f), accent * 1.24f);
            w.TriColored(new Vector3(-s * 0.06f, s * (mastH - 0.08f), 0), new Vector3(s * 0.06f, s * (mastH - 0.08f), 0),
                new Vector3(0, s * mastH, s * 0.02f), secondary * 0.88f);
        }
        else if (vasudan)
        {
            for (int i = 0; i < 8; i++)
            {
                float a0 = MathF.PI * 2f * i / 8f;
                float a1 = MathF.PI * 2f * (i + 1) / 8f;
                float crownR = 0.14f;
                var p0 = new Vector3(MathF.Cos(a0) * s * crownR, s * (mastH + 0.06f), MathF.Sin(a0) * s * crownR);
                var p1 = new Vector3(MathF.Cos(a1) * s * crownR, s * (mastH + 0.06f), MathF.Sin(a1) * s * crownR);
                var top = new Vector3(0, s * (mastH + 0.14f), s * 0.04f);
                w.TriColored(p0, p1, top, i % 2 == 0 ? accent : accent * 0.96f);
            }
            w.TriColored(new Vector3(-s * 0.06f, s * (mastH + 0.10f), s * 0.02f), new Vector3(s * 0.06f, s * (mastH + 0.10f), s * 0.02f),
                new Vector3(0, s * (mastH + 0.18f), s * 0.06f), accent * 1.12f);

            for (int side = -1; side <= 1; side += 2)
            {
                float armReach = side < 0 ? 0.58f : 0.44f;
                float armTop = side < 0 ? 0.74f : 0.58f;
                float armZ = side < 0 ? 0.16f : 0.08f;
                w.TriColored(new Vector3(side * s * 0.52f, s * 0.10f, 0), new Vector3(side * s * armReach, s * armTop, s * armZ),
                    new Vector3(side * s * 0.48f, s * hubH, s * 0.14f), secondary * 0.92f);
                w.TriColored(new Vector3(side * s * armReach, s * armTop, s * armZ),
                    new Vector3(side * s * (armReach + 0.04f), s * (armTop + 0.06f), s * (armZ - 0.04f)),
                    new Vector3(side * s * 0.48f, s * hubH, s * 0.14f), primary * 0.9f);
                w.TriColored(new Vector3(side * s * armReach, s * armTop, s * armZ),
                    new Vector3(side * s * (armReach + 0.02f), s * (armTop + 0.04f), s * (armZ + 0.04f)),
                    new Vector3(side * s * (armReach - 0.02f), s * (armTop - 0.02f), s * (armZ + 0.06f)), accent * (side < 0 ? 1.08f : 1.02f));
            }
        }
        w.TriColored(new Vector3(-s * 0.12f, s * 0.42f, -s * 0.08f), new Vector3(s * 0.12f, s * 0.42f, -s * 0.08f),
            new Vector3(0, s * 0.52f, s * 0.10f), accent * 0.95f);
    }

    private static void BuildRefinery(RaceMeshWriter w, RaceVisualDefinition race, Vector3 primary, Vector3 secondary, Vector3 accent, float s, bool omitArm = false)
    {
        if (race.Style.Equals("retro", StringComparison.OrdinalIgnoreCase))
        {
            BuildRetroRefinery(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("vasudan", StringComparison.OrdinalIgnoreCase))
        {
            BuildVasudanRefinery(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("truss", StringComparison.OrdinalIgnoreCase))
        {
            BuildTrussRefinery(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("organic", StringComparison.OrdinalIgnoreCase))
        {
            BuildOrganicRefinery(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("radiant", StringComparison.OrdinalIgnoreCase))
        {
            BuildRadiantRefinery(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("asymmetric", StringComparison.OrdinalIgnoreCase))
        {
            BuildAsymmetricRefinery(w, race, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("spiny", StringComparison.OrdinalIgnoreCase))
        {
            BuildSpinyRefinery(w, race, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("crystalline", StringComparison.OrdinalIgnoreCase))
        {
            BuildCrystallineRefinery(w, race, primary, secondary, accent, s);
            return;
        }

        for (int i = 0; i < 12; i++)
        {
            float a0 = MathF.PI * 2f * i / 12f;
            float a1 = MathF.PI * 2f * (i + 1) / 12f;
            var p0 = new Vector3(MathF.Cos(a0) * s * 0.98f, 0, MathF.Sin(a0) * s * 0.98f);
            var p1 = new Vector3(MathF.Cos(a1) * s * 0.98f, 0, MathF.Sin(a1) * s * 0.98f);
            var p2 = new Vector3(MathF.Cos(a0) * s * 0.58f, s * 0.10f, MathF.Sin(a0) * s * 0.58f);
            w.TriColored(p0, p1, p2, i % 2 == 0 ? primary : secondary);
            w.TriColored(Vector3.Zero, p0, p1, secondary * 0.85f);
        }

        int stacks = Math.Clamp(2 + race.Modifiers.EngineCount / 2, 3, 6);
        for (int i = 0; i < stacks; i++)
        {
            float x = (i - (stacks - 1) * 0.5f) * s * 0.32f;
            float h = s * (0.72f + (i % 2) * 0.14f);
            w.TriColored(new Vector3(x, 0, -s * 0.18f), new Vector3(x + s * 0.14f, h, -s * 0.18f),
                new Vector3(x, h, s * 0.18f), secondary * 0.95f);
            w.TriColored(new Vector3(x + s * 0.14f, h, -s * 0.18f), new Vector3(x + s * 0.14f, h, s * 0.18f),
                new Vector3(x, h, s * 0.18f), primary * 0.9f);
            w.TriColored(new Vector3(x + s * 0.07f, h, 0), new Vector3(x + s * 0.07f, h + s * 0.12f, -s * 0.06f),
                new Vector3(x + s * 0.07f, h + s * 0.12f, s * 0.06f), accent);
        }

        w.TriColored(new Vector3(-s * 0.12f, s * 0.10f, 0), new Vector3(s * 0.12f, s * 0.10f, 0),
            new Vector3(0, s * 0.82f, s * 0.08f), accent * 1.05f);

        for (int p = 0; p < 4; p++)
        {
            float z = -s * 0.3f + p * s * 0.18f;
            w.TriColored(new Vector3(-s * 0.5f, s * 0.10f, z), new Vector3(s * 0.5f, s * 0.10f, z),
                new Vector3(0, s * 0.28f, z + s * 0.06f), accent * 0.9f);
        }

        if (!omitArm)
        {
            for (int seg = 0; seg < 8; seg++)
            {
                float t0 = seg / 8f;
                float t1 = (seg + 1) / 8f;
                float y0 = s * (0.10f + t0 * 0.58f);
                float y1 = s * (0.10f + t1 * 0.58f);
                float r0 = s * 0.11f * (1f - t0 * 0.28f);
                float r1 = s * 0.11f * (1f - t1 * 0.28f);
                w.TriColored(new Vector3(-r0, y0, 0), new Vector3(r0, y0, 0), new Vector3(-r1, y1, s * 0.03f), seg % 2 == 0 ? accent : secondary);
                w.TriColored(new Vector3(r0, y0, 0), new Vector3(r1, y1, s * 0.03f), new Vector3(-r1, y1, s * 0.03f), accent * 0.95f);
            }

            w.TriColored(new Vector3(0, s * 0.70f, 0), new Vector3(-s * 0.06f, s * 0.62f, 0), new Vector3(s * 0.06f, s * 0.62f, 0), accent * 1.12f);
        }
    }

    private static void BuildRepairBay(RaceMeshWriter w, RaceVisualDefinition race, Vector3 primary, Vector3 secondary, Vector3 accent, float s,
        bool omitCrane = false, bool omitBayDoor = false)
    {
        if (race.Style.Equals("retro", StringComparison.OrdinalIgnoreCase))
        {
            BuildRetroRepairBay(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("organic", StringComparison.OrdinalIgnoreCase))
        {
            BuildOrganicRepairBay(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("vasudan", StringComparison.OrdinalIgnoreCase))
        {
            BuildVasudanRepairBay(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("truss", StringComparison.OrdinalIgnoreCase))
        {
            BuildTrussRepairBay(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("radiant", StringComparison.OrdinalIgnoreCase))
        {
            BuildRadiantRepairBay(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("asymmetric", StringComparison.OrdinalIgnoreCase))
        {
            BuildAsymmetricRepairBay(w, race, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("spiny", StringComparison.OrdinalIgnoreCase))
        {
            BuildSpinyRepairBay(w, race, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("crystalline", StringComparison.OrdinalIgnoreCase))
        {
            BuildCrystallineRepairBay(w, race, primary, secondary, accent, s);
            return;
        }

        bool organic = race.Style.Equals("organic", StringComparison.OrdinalIgnoreCase);
        bool retro = false;
        bool vasudan = false;
        bool asymmetric = false;
        bool crystalline = false;
        bool radiant = race.Style.Equals("radiant", StringComparison.OrdinalIgnoreCase);
        bool spiny = race.Style.Equals("spiny", StringComparison.OrdinalIgnoreCase);
        // Loop-05/06: widen pad + raise service spine crown for landmark massing (h/w ≥0.78 path).
        float padR = organic ? 1.02f : radiant ? 1.04f : spiny ? 1.06f : retro ? 1.12f : vasudan ? 1.04f : asymmetric ? 1.04f : crystalline ? 1.04f : 0.98f;
        float rimH = organic ? 0.48f : radiant ? 0.58f : spiny ? 0.54f : retro ? 0.46f : vasudan ? 0.64f : asymmetric ? 0.52f : crystalline ? 0.48f : 0.58f;
        float peakH = organic ? 1.12f : radiant ? 1.38f : spiny ? 1.26f : retro ? 1.05f : vasudan ? 1.42f : asymmetric ? 1.12f : crystalline ? 1.08f : 1.32f;
        float spineLean = vasudan || asymmetric ? s * race.Modifiers.Asymmetry * 0.22f : crystalline ? s * 0.04f : spiny ? s * race.Modifiers.Asymmetry * 0.08f : 0f;

        for (int i = 0; i < 12; i++)
        {
            float a0 = MathF.PI * 2f * i / 12f;
            float a1 = MathF.PI * 2f * (i + 1) / 12f;
            var p0 = new Vector3(MathF.Cos(a0) * s * padR, 0, MathF.Sin(a0) * s * padR);
            var p1 = new Vector3(MathF.Cos(a1) * s * padR, 0, MathF.Sin(a1) * s * padR);
            var p2 = new Vector3(MathF.Cos(a0) * s * 0.62f, s * rimH, MathF.Sin(a0) * s * 0.62f);
            var p3 = new Vector3(MathF.Cos(a1) * s * 0.62f, s * rimH, MathF.Sin(a1) * s * 0.62f);
            w.TriColored(p0, p1, p2, i % 2 == 0 ? primary : secondary);
            w.TriColored(p1, p3, p2, secondary * 0.9f);
            w.TriColored(Vector3.Zero, p0, p1, secondary * 0.85f);
        }

        if (retro)
        {
            for (int lobe = 0; lobe < 10; lobe++)
            {
                float a = MathF.PI * 2f * lobe / 10f;
                float x = MathF.Cos(a) * s * padR * 1.02f;
                float z = MathF.Sin(a) * s * padR * 1.02f;
                w.TriColored(
                    new Vector3(x, s * 0.04f, z),
                    new Vector3(x + s * 0.05f, s * 0.10f, z + s * 0.03f),
                    new Vector3(x - s * 0.02f, s * 0.08f, z + s * 0.04f),
                    lobe % 2 == 0 ? primary * 0.92f : secondary);
            }
        }

        if (vasudan)
        {
            for (int lobe = 0; lobe < 12; lobe++)
            {
                float a = MathF.PI * 2f * lobe / 12f;
                float x = MathF.Cos(a) * s * padR * 1.04f;
                float z = MathF.Sin(a) * s * padR * 1.04f;
                w.TriColored(
                    new Vector3(x, s * 0.03f, z),
                    new Vector3(x + s * 0.06f, s * 0.10f, z + s * 0.04f),
                    new Vector3(x - s * 0.03f, s * 0.08f, z + s * 0.05f),
                    lobe % 3 == 0 ? accent * 1.06f : lobe % 2 == 0 ? primary * 0.92f : secondary);
                w.TriColored(
                    new Vector3(x, s * 0.10f, z),
                    new Vector3(x + s * 0.04f, s * 0.16f, z + s * 0.02f),
                    new Vector3(x - s * 0.02f, s * 0.14f, z + s * 0.03f),
                    lobe % 3 == 0 ? accent * 1.12f : secondary * 0.9f);
            }
        }
        else if (asymmetric)
        {
            for (int lobe = 0; lobe < 10; lobe++)
            {
                float a = MathF.PI * 2f * lobe / 10f;
                float x = MathF.Cos(a) * s * padR * 1.02f + spineLean;
                float z = MathF.Sin(a) * s * padR * 1.02f;
                w.TriColored(
                    new Vector3(x, s * 0.04f, z),
                    new Vector3(x + s * 0.06f, s * 0.11f, z + s * 0.04f),
                    new Vector3(x - s * 0.02f, s * 0.09f, z + s * 0.05f),
                    lobe % 3 == 0 ? accent * 1.08f : lobe % 2 == 0 ? primary * 0.92f : secondary);
            }
        }
        else if (crystalline)
        {
            for (int lobe = 0; lobe < 10; lobe++)
            {
                float a = MathF.PI * 2f * lobe / 10f;
                float x = MathF.Cos(a) * s * padR * 1.02f;
                float z = MathF.Sin(a) * s * padR * 1.02f;
                w.TriColored(
                    new Vector3(x, s * 0.04f, z),
                    new Vector3(x + s * 0.05f, s * 0.12f, z + s * 0.03f),
                    new Vector3(x - s * 0.02f, s * 0.10f, z + s * 0.04f),
                    lobe % 2 == 0 ? primary * 0.94f : secondary * 0.9f);
                w.TriColored(
                    new Vector3(x, s * 0.12f, z),
                    new Vector3(x + s * 0.03f, s * 0.18f, z + s * 0.02f),
                    new Vector3(x, s * 0.16f, z + s * 0.03f),
                    lobe % 3 == 0 ? accent * 1.10f : secondary * 0.88f);
            }
        }
        else if (radiant)
        {
            for (int lobe = 0; lobe < 10; lobe++)
            {
                float a = MathF.PI * 2f * lobe / 10f;
                float x = MathF.Cos(a) * s * padR * 1.01f;
                float z = MathF.Sin(a) * s * padR * 1.01f;
                w.TriColored(
                    new Vector3(x, s * 0.04f, z),
                    new Vector3(x + s * 0.05f, s * 0.11f, z + s * 0.03f),
                    new Vector3(x - s * 0.02f, s * 0.09f, z + s * 0.04f),
                    lobe % 3 == 0 ? accent * 1.08f : lobe % 2 == 0 ? primary * 0.94f : secondary);
            }
        }
        else if (spiny)
        {
            for (int lobe = 0; lobe < 10; lobe++)
            {
                float a = MathF.PI * 2f * lobe / 10f;
                float x = MathF.Cos(a) * s * padR * 1.02f + spineLean;
                float z = MathF.Sin(a) * s * padR * 1.02f;
                w.TriColored(
                    new Vector3(x, s * 0.04f, z),
                    new Vector3(x + s * 0.05f, s * 0.10f, z + s * 0.04f),
                    new Vector3(x - s * 0.02f, s * 0.08f, z + s * 0.05f),
                    lobe % 2 == 0 ? accent * 1.06f : secondary * 0.90f);
            }
        }

        w.TriColored(new Vector3(-s * 0.42f + spineLean, s * rimH, -s * 0.32f), new Vector3(s * 0.42f + spineLean, s * rimH, -s * 0.32f),
            new Vector3(spineLean, s * (peakH - 0.14f), s * 0.14f), primary * 0.94f);
        w.TriColored(new Vector3(-s * 0.28f + spineLean, s * rimH, -s * 0.06f), new Vector3(s * 0.28f + spineLean, s * rimH, -s * 0.06f),
            new Vector3(spineLean, s * (peakH - 0.06f), s * 0.08f), secondary * 0.92f);
        w.TriColored(new Vector3(-s * 0.10f + spineLean, s * (peakH - 0.14f), s * 0.02f), new Vector3(s * 0.10f + spineLean, s * (peakH - 0.14f), s * 0.02f),
            new Vector3(spineLean, s * peakH, s * 0.10f), accent * 1.15f);
        w.TriColored(new Vector3(-s * 0.06f + spineLean, s * (peakH - 0.04f), s * 0.06f), new Vector3(s * 0.06f + spineLean, s * (peakH - 0.04f), s * 0.06f),
            new Vector3(spineLean, s * peakH, s * 0.04f), accent * 1.22f);

        if (!organic && !omitBayDoor)
        {
            float recessZ = asymmetric || crystalline ? -0.56f : -0.52f;
            w.TriColored(new Vector3(-s * 0.38f + spineLean, s * 0.06f, s * recessZ), new Vector3(s * 0.38f + spineLean, s * 0.06f, s * recessZ),
                new Vector3(spineLean, s * 0.18f, s * (recessZ + 0.08f)), secondary * 0.78f);
            w.TriColored(new Vector3(-s * 0.22f + spineLean, s * 0.18f, s * (recessZ + 0.04f)), new Vector3(s * 0.22f + spineLean, s * 0.18f, s * (recessZ + 0.04f)),
                new Vector3(spineLean, s * 0.26f, s * (recessZ + 0.12f)), primary * 0.88f);
            w.TriColored(new Vector3(-s * 0.14f + spineLean, s * 0.26f, s * (recessZ + 0.06f)), new Vector3(s * 0.14f + spineLean, s * 0.26f, s * (recessZ + 0.06f)),
                new Vector3(spineLean, s * rimH * 0.88f, s * (recessZ + 0.14f)), accent * 0.96f);
        }

        if (!omitCrane)
        for (int side = -1; side <= 1; side += 2)
        {
            float armReach = organic ? 0.78f
                : radiant ? (side < 0 ? 0.96f : 0.80f)
                : spiny ? (side < 0 ? 0.94f : 0.78f)
                : vasudan ? (side < 0 ? 0.94f : 0.72f)
                : retro ? (side < 0 ? 0.94f : 0.84f)
                : asymmetric ? (side < 0 ? 0.98f : 0.82f)
                : crystalline ? (side < 0 ? 0.92f : 0.78f)
                : side < 0 ? 0.86f : 0.74f;
            float armH = organic ? 0.38f : radiant ? 0.52f : spiny ? 0.50f : retro ? 0.48f : vasudan ? 0.56f : asymmetric ? 0.54f : crystalline ? 0.52f : 0.48f;
            float armTop = organic ? 0.48f
                : radiant ? (side < 0 ? 0.92f : 0.70f)
                : spiny ? (side < 0 ? 0.88f : 0.66f)
                : vasudan ? (side < 0 ? 0.90f : 0.64f)
                : retro ? (side < 0 ? 0.72f : 0.60f)
                : asymmetric ? (side < 0 ? 0.94f : 0.72f)
                : crystalline ? (side < 0 ? 0.86f : 0.68f)
                : side < 0 ? 0.82f : 0.66f;
            float armZ = radiant ? (side < 0 ? 0.26f : 0.18f)
                : spiny ? (side < 0 ? 0.24f : 0.16f)
                : asymmetric ? (side < 0 ? 0.24f : 0.16f)
                : crystalline ? (side < 0 ? 0.20f : 0.12f)
                : side < 0 ? 0.18f : 0.10f;
            w.TriColored(new Vector3(side * s * 0.62f, s * 0.08f, 0), new Vector3(side * s * armReach, s * armH, s * armZ),
                new Vector3(side * s * 0.62f, s * rimH, s * 0.20f), secondary * 0.92f);
            w.TriColored(new Vector3(side * s * armReach, s * armH, s * armZ), new Vector3(side * s * armReach, s * armTop, s * (armZ - 0.06f)),
                new Vector3(side * s * 0.62f, s * rimH, s * 0.20f), primary * 0.9f);
            w.TriColored(new Vector3(side * s * armReach, s * armTop, s * (armZ - 0.06f)),
                new Vector3(side * s * (armReach + 0.04f), s * (armTop + 0.06f), s * (armZ - 0.10f)),
                new Vector3(side * s * armReach, s * (armTop - 0.04f), s * (armZ - 0.02f)), accent * (side < 0 ? 1.12f : 1.06f));
        }

        if (retro)
        {
            for (int band = 0; band < 5; band++)
            {
                float t = band / 4f;
                float y = s * (rimH + t * (peakH - rimH - 0.10f));
                float halfW = s * (0.18f + t * 0.10f);
                float z = s * (0.04f + t * 0.10f);
                w.TriColored(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + s * 0.04f, z + s * 0.03f),
                    band % 2 == 0 ? primary * 0.96f : secondary * 0.90f);
            }
        }

        if (vasudan)
        {
            for (int band = 0; band < 6; band++)
            {
                float t = band / 5f;
                float y = s * (rimH + t * (peakH - rimH - 0.08f));
                float halfW = s * (0.16f + t * 0.12f);
                float z = s * (0.06f + t * 0.12f);
                w.TriColored(
                    new Vector3(-halfW + spineLean, y, z),
                    new Vector3(halfW + spineLean, y, z),
                    new Vector3(spineLean, y + s * 0.05f, z + s * 0.04f),
                    band % 2 == 0 ? primary * 0.94f : secondary * 0.88f);
            }

            w.TriColored(
                new Vector3(-s * 0.05f + spineLean, s * (peakH + 0.02f), s * 0.08f),
                new Vector3(s * 0.05f + spineLean, s * (peakH + 0.02f), s * 0.08f),
                new Vector3(spineLean, s * (peakH + 0.10f), s * 0.12f), accent * 1.28f);
            w.TriColored(
                new Vector3(-s * 0.04f + spineLean, s * (peakH + 0.08f), s * 0.10f),
                new Vector3(s * 0.04f + spineLean, s * (peakH + 0.08f), s * 0.10f),
                new Vector3(spineLean, s * (peakH + 0.14f), s * 0.06f), accent * 1.32f);

            w.TriColored(
                new Vector3(-s * 0.30f + spineLean, s * 0.08f, -s * 0.52f),
                new Vector3(s * 0.22f + spineLean, s * 0.08f, -s * 0.52f),
                new Vector3(spineLean, s * 0.26f, -s * 0.44f), secondary * 0.82f);
            w.TriColored(
                new Vector3(-s * 0.16f + spineLean, s * 0.26f, -s * 0.48f),
                new Vector3(s * 0.10f + spineLean, s * 0.26f, -s * 0.48f),
                new Vector3(spineLean, s * 0.38f, -s * 0.40f), primary * 0.86f);
        }
        else if (asymmetric)
        {
            for (int band = 0; band < 5; band++)
            {
                float t = band / 4f;
                float y = s * (rimH + t * (peakH - rimH - 0.08f));
                float halfW = s * (0.12f + t * 0.10f);
                float z = s * (0.06f + t * 0.12f);
                w.TriColored(
                    new Vector3(-halfW + spineLean, y, z),
                    new Vector3(halfW + spineLean, y, z),
                    new Vector3(spineLean, y + s * 0.04f, z + s * 0.03f),
                    band % 2 == 0 ? primary * 0.92f : secondary * 0.86f);
            }
            w.TriColored(
                new Vector3(-s * 0.05f + spineLean, s * (peakH - 0.02f), s * 0.08f),
                new Vector3(s * 0.05f + spineLean, s * (peakH - 0.02f), s * 0.08f),
                new Vector3(spineLean, s * (peakH + 0.04f), s * 0.10f), accent * 1.20f);
        }
        else if (crystalline)
        {
            for (int band = 0; band < 4; band++)
            {
                float t = band / 3f;
                float y = s * (rimH + t * (peakH - rimH - 0.10f));
                float halfW = s * (0.10f + t * 0.08f);
                float z = s * (0.05f + t * 0.10f);
                w.TriColored(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + s * 0.04f, z + s * 0.03f),
                    band % 2 == 0 ? primary * 0.94f : secondary * 0.88f);
            }
            w.TriColored(
                new Vector3(-s * 0.04f, s * (peakH - 0.02f), s * 0.06f),
                new Vector3(s * 0.04f, s * (peakH - 0.02f), s * 0.06f),
                new Vector3(0, s * (peakH + 0.04f), s * 0.08f), accent * 1.18f);
        }
        else if (radiant)
        {
            for (int band = 0; band < 5; band++)
            {
                float t = band / 4f;
                float y = s * (rimH + t * (peakH - rimH - 0.08f));
                float halfW = s * (0.14f + t * 0.12f);
                float z = s * (0.08f + t * 0.14f);
                w.TriColored(
                    new Vector3(-halfW + spineLean, y, z),
                    new Vector3(halfW + spineLean, y, z),
                    new Vector3(spineLean, y + s * 0.05f, z + s * 0.04f),
                    band % 2 == 0 ? primary * 0.94f : secondary * 0.88f);
            }
            for (int wing = -1; wing <= 1; wing += 2)
            {
                w.TriColored(new Vector3(wing * s * 0.56f + spineLean, s * (peakH - 0.06f), s * 0.12f),
                    new Vector3(wing * s * 0.78f + spineLean, s * (peakH + 0.04f), s * 0.20f),
                    new Vector3(wing * s * 0.52f + spineLean, s * peakH, s * 0.16f), accent * (wing < 0 ? 1.12f : 1.06f));
            }
            w.TriColored(
                new Vector3(-s * 0.05f + spineLean, s * (peakH + 0.02f), s * 0.10f),
                new Vector3(s * 0.05f + spineLean, s * (peakH + 0.02f), s * 0.10f),
                new Vector3(spineLean, s * (peakH + 0.10f), s * 0.14f), accent * 1.24f);
        }
        else if (spiny)
        {
            for (int band = 0; band < 4; band++)
            {
                float t = band / 3f;
                float y = s * (rimH + t * (peakH - rimH - 0.06f));
                float halfW = s * (0.12f + t * 0.10f);
                float z = s * (0.10f + t * 0.16f);
                w.TriColored(
                    new Vector3(-halfW + spineLean, y, z),
                    new Vector3(halfW + spineLean, y, z),
                    new Vector3(spineLean, y + s * 0.04f, z + s * 0.05f),
                    band % 2 == 0 ? accent : secondary * 0.88f);
            }
            w.TriColored(
                new Vector3(-s * 0.04f + spineLean, s * (peakH + 0.02f), s * 0.12f),
                new Vector3(s * 0.04f + spineLean, s * (peakH + 0.02f), s * 0.12f),
                new Vector3(spineLean, s * (peakH + 0.08f), s * 0.18f), accent * 1.16f);
        }

        if (organic)
        {
            for (int bay = 0; bay < 2; bay++)
            {
                float z = bay == 0 ? -s * 0.34f : s * 0.30f;
                w.TriColored(new Vector3(-s * 0.52f, s * 0.10f, z), new Vector3(s * 0.52f, s * 0.10f, z + s * 0.10f),
                    new Vector3(0, s * 0.28f, z + s * 0.05f), primary * 0.94f);
                w.TriColored(new Vector3(-s * 0.28f, s * 0.28f, z), new Vector3(s * 0.28f, s * 0.28f, z + s * 0.06f),
                    new Vector3(0, s * 0.36f, z + s * 0.03f), accent * 0.96f);
            }
            for (int rib = 0; rib < 6; rib++)
            {
                float a = MathF.PI * 2f * rib / 6f;
                float x = MathF.Cos(a) * s * 0.44f;
                float z = MathF.Sin(a) * s * 0.44f;
                w.TriColored(new Vector3(x, s * 0.12f, z), new Vector3(x + s * 0.04f, s * 0.24f, z + s * 0.03f),
                    new Vector3(x, s * 0.20f, z + s * 0.05f), secondary * 0.88f);
            }
        }
    }

    private static void BuildReactor(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s, bool omitRing = false)
    {
        if (race.Style.Equals("retro", StringComparison.OrdinalIgnoreCase))
        {
            BuildRetroReactor(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("radiant", StringComparison.OrdinalIgnoreCase))
        {
            BuildRadiantReactor(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("vasudan", StringComparison.OrdinalIgnoreCase))
        {
            BuildVasudanReactor(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("truss", StringComparison.OrdinalIgnoreCase))
        {
            BuildTrussReactor(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("organic", StringComparison.OrdinalIgnoreCase))
        {
            BuildOrganicReactor(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("asymmetric", StringComparison.OrdinalIgnoreCase))
        {
            BuildAsymmetricReactor(w, race, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("spiny", StringComparison.OrdinalIgnoreCase))
        {
            BuildSpinyReactor(w, race, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("crystalline", StringComparison.OrdinalIgnoreCase))
        {
            BuildCrystallineReactor(w, race, primary, secondary, accent, s);
            return;
        }

        bool asymmetric = false;
        bool crystalline = false;
        float padR = asymmetric || crystalline ? 1.04f : 0.98f;
        float coreH = asymmetric ? 0.82f : crystalline ? 0.74f : 0.72f;
        float crownH = asymmetric ? 1.06f : crystalline ? 0.98f : 0.98f;
        float bias = asymmetric ? s * race.Modifiers.Asymmetry * 0.10f : 0f;

        for (int i = 0; i < 12; i++)
        {
            float a0 = MathF.PI * 2f * i / 12f;
            float a1 = MathF.PI * 2f * (i + 1) / 12f;
            var p0 = new Vector3(MathF.Cos(a0) * s * padR + bias, 0, MathF.Sin(a0) * s * padR);
            var p1 = new Vector3(MathF.Cos(a1) * s * padR + bias, 0, MathF.Sin(a1) * s * padR);
            var p2 = new Vector3(MathF.Cos(a0) * s * 0.55f + bias, s * 0.06f, MathF.Sin(a0) * s * 0.55f);
            w.TriColored(p0, p1, p2, i % 2 == 0 ? primary : secondary);
            w.TriColored(Vector3.Zero, p0, p1, secondary * 0.85f);
        }

        if (asymmetric)
        {
            for (int lobe = 0; lobe < 8; lobe++)
            {
                float a = MathF.PI * 2f * lobe / 8f;
                float x = MathF.Cos(a) * s * padR * 0.98f + bias;
                float z = MathF.Sin(a) * s * padR * 0.98f;
                w.TriColored(
                    new Vector3(x, s * 0.04f, z),
                    new Vector3(x + s * 0.05f, s * 0.10f, z + s * 0.03f),
                    new Vector3(x - s * 0.02f, s * 0.08f, z + s * 0.04f),
                    lobe % 3 == 0 ? accent * 1.06f : lobe % 2 == 0 ? primary * 0.92f : secondary);
            }
        }

        int segments = crystalline ? 12 : asymmetric ? 10 : 10;
        for (int i = 0; i < segments; i++)
        {
            float a0 = MathF.PI * 2f * i / segments;
            float a1 = MathF.PI * 2f * (i + 1) / segments;
            var p0 = new Vector3(MathF.Cos(a0) * s * 0.45f + bias, 0, MathF.Sin(a0) * s * 0.45f);
            var p1 = new Vector3(MathF.Cos(a1) * s * 0.45f + bias, 0, MathF.Sin(a1) * s * 0.45f);
            var top = new Vector3(bias, s * coreH, 0);
            w.TriColored(p0, p1, top, i % 2 == 0 ? accent : accent * 0.85f);
            w.TriColored(p0, p1, Vector3.Zero, secondary);
        }

        if (!omitRing)
        {
            for (int ring = 0; ring < 3; ring++)
            {
                float y = s * (coreH + ring * 0.06f);
                float r = s * (0.5f - ring * 0.06f);
                for (int i = 0; i < segments; i++)
                {
                    float a0 = MathF.PI * 2f * i / segments;
                    float a1 = MathF.PI * 2f * (i + 1) / segments;
                    var p0 = new Vector3(MathF.Cos(a0) * r + bias, y, MathF.Sin(a0) * r);
                    var p1 = new Vector3(MathF.Cos(a1) * r + bias, y, MathF.Sin(a1) * r);
                    var top = new Vector3(bias, y + s * 0.05f, 0);
                    w.TriColored(p0, p1, top, accent * (1f + ring * 0.05f));
                }
            }

            w.TriColored(new Vector3(bias, s * coreH, 0), new Vector3(-s * 0.1f + bias, s * (coreH + 0.10f), 0), new Vector3(s * 0.1f + bias, s * (coreH + 0.10f), 0), accent * 1.2f);
            w.TriColored(new Vector3(bias, s * (coreH + 0.10f), 0), new Vector3(bias, s * crownH, s * 0.08f), new Vector3(-s * 0.08f + bias, s * (coreH + 0.10f), 0), accent * 1.25f);
            w.TriColored(new Vector3(-s * 0.06f + bias, s * crownH, 0), new Vector3(s * 0.06f + bias, s * crownH, 0), new Vector3(bias, s * (crownH + 0.07f), s * 0.04f), accent * 1.3f);
        }

        if (asymmetric)
        {
            for (int fin = 0; fin < 3; fin++)
            {
                float angle = MathF.PI * 2f * fin / 3f + MathF.PI * 0.20f;
                var dir = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
                var side = new Vector3(-dir.Z, 0f, dir.X);
                var root = dir * s * 0.84f + new Vector3(bias, s * 0.08f, 0);
                var top = dir * s * 0.74f + new Vector3(bias, s * 0.62f, s * 0.04f);
                w.TriColored(root - side * s * 0.06f, root + side * s * 0.06f, top, fin % 2 == 0 ? primary * 0.9f : secondary);
            }
            for (int vent = 0; vent < 2; vent++)
            {
                float z = s * (0.16f + vent * 0.12f);
                w.TriColored(new Vector3(bias - s * 0.06f, s * 0.44f, z), new Vector3(bias + s * 0.06f, s * 0.44f, z),
                    new Vector3(bias, s * 0.56f, z + s * 0.06f), vent % 2 == 0 ? accent : accent * 0.94f);
            }
        }

    }

    private static void BuildSupplyDepot(RaceMeshWriter w, RaceVisualDefinition race, Vector3 primary, Vector3 secondary, Vector3 accent, float s, bool omitCrane = false)
    {
        if (race.Style.Equals("retro", StringComparison.OrdinalIgnoreCase))
        {
            BuildRetroSupplyDepot(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("vasudan", StringComparison.OrdinalIgnoreCase))
        {
            BuildVasudanSupplyDepot(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("truss", StringComparison.OrdinalIgnoreCase))
        {
            BuildTrussSupplyDepot(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("organic", StringComparison.OrdinalIgnoreCase))
        {
            BuildOrganicSupplyDepot(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("radiant", StringComparison.OrdinalIgnoreCase))
        {
            BuildRadiantSupplyDepot(w, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("asymmetric", StringComparison.OrdinalIgnoreCase))
        {
            BuildAsymmetricSupplyDepot(w, race, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("spiny", StringComparison.OrdinalIgnoreCase))
        {
            BuildSpinySupplyDepot(w, race, primary, secondary, accent, s);
            return;
        }

        if (race.Style.Equals("crystalline", StringComparison.OrdinalIgnoreCase))
        {
            BuildCrystallineSupplyDepot(w, race, primary, secondary, accent, s);
            return;
        }

        bool organic = race.Style.Equals("organic", StringComparison.OrdinalIgnoreCase);
        float padR = organic ? 1.02f : 0.98f;
        float siloH = organic ? 0.58f : 0.68f;
        float hubH = organic ? 0.44f : 0.48f;
        float peakH = organic ? 1.08f : 1.02f;

        for (int i = 0; i < 12; i++)
        {
            float a0 = MathF.PI * 2f * i / 12f;
            float a1 = MathF.PI * 2f * (i + 1) / 12f;
            var p0 = new Vector3(MathF.Cos(a0) * s * padR, 0, MathF.Sin(a0) * s * padR);
            var p1 = new Vector3(MathF.Cos(a1) * s * padR, 0, MathF.Sin(a1) * s * padR);
            var p2 = new Vector3(MathF.Cos(a0) * s * 0.55f, s * 0.06f, MathF.Sin(a0) * s * 0.55f);
            w.TriColored(p0, p1, p2, i % 2 == 0 ? primary : secondary);
            w.TriColored(Vector3.Zero, p0, p1, secondary * 0.85f);
        }

        for (int c = 0; c < 4; c++)
        {
            float ox = (c % 2 == 0 ? -1 : 1) * s * 0.38f;
            float oz = (c < 2 ? -1 : 1) * s * 0.3f;
            w.TriColored(new Vector3(ox, 0, oz), new Vector3(ox + s * 0.24f, 0, oz),
                new Vector3(ox, s * siloH, oz + s * 0.2f), c % 2 == 0 ? primary : secondary);
            w.TriColored(new Vector3(ox + s * 0.24f, 0, oz), new Vector3(ox + s * 0.24f, s * siloH, oz + s * 0.2f),
                new Vector3(ox, s * siloH, oz + s * 0.2f), secondary * 0.9f);
            w.TriColored(new Vector3(ox + s * 0.12f, s * siloH, oz + s * 0.1f), new Vector3(ox + s * 0.12f, s * (siloH + 0.12f), oz),
                new Vector3(ox + s * 0.12f, s * (siloH + 0.06f), oz + s * 0.18f), accent);
        }

        w.TriColored(new Vector3(0, 0, 0), new Vector3(-s * 0.16f, s * hubH, -s * 0.08f), new Vector3(s * 0.16f, s * hubH, -s * 0.08f), primary);
        w.TriColored(new Vector3(-s * 0.16f, s * hubH, -s * 0.08f), new Vector3(s * 0.16f, s * hubH, s * 0.08f), new Vector3(s * 0.16f, s * hubH, -s * 0.08f), primary * 0.94f);
        w.TriColored(new Vector3(-s * 0.16f, s * hubH, -s * 0.08f), new Vector3(-s * 0.16f, s * hubH, s * 0.08f), new Vector3(s * 0.16f, s * hubH, s * 0.08f), primary * 0.9f);
        if (!omitCrane)
        {
            for (int seg = 0; seg < 5; seg++)
            {
                float t0 = seg / 5f;
                float t1 = (seg + 1) / 5f;
                float y0 = s * (hubH + t0 * (peakH - hubH - 0.08f));
                float y1 = s * (hubH + t1 * (peakH - hubH - 0.08f));
                float r0 = s * 0.12f * (1f - t0 * 0.2f);
                float r1 = s * 0.12f * (1f - t1 * 0.2f);
                w.TriColored(new Vector3(-r0, y0, 0), new Vector3(r0, y0, 0), new Vector3(0, y1, s * 0.04f), seg % 2 == 0 ? accent : primary);
            }

            w.TriColored(new Vector3(0, s * (peakH - 0.08f), 0), new Vector3(-s * 0.08f, s * (peakH - 0.18f), 0), new Vector3(s * 0.08f, s * (peakH - 0.18f), 0), accent * 1.15f);
            w.TriColored(new Vector3(-s * 0.06f, s * (peakH - 0.08f), 0), new Vector3(s * 0.06f, s * (peakH - 0.08f), 0), new Vector3(0, s * peakH, s * 0.04f), accent * 1.2f);
        }

        if (organic)
        {
            for (int link = 0; link < 4; link++)
            {
                float z = (link - 1.5f) * s * 0.22f;
                w.TriColored(new Vector3(-s * 0.42f, s * 0.08f, z), new Vector3(s * 0.42f, s * 0.08f, z + s * 0.06f),
                    new Vector3(0, s * 0.18f, z + s * 0.03f), secondary * 0.9f);
            }
            for (int mound = 0; mound < 5; mound++)
            {
                float a = MathF.PI * 2f * mound / 5f;
                float x = MathF.Cos(a) * s * 0.56f;
                float z = MathF.Sin(a) * s * 0.56f;
                w.TriColored(new Vector3(x, s * 0.06f, z), new Vector3(x + s * 0.05f, s * 0.16f, z + s * 0.04f),
                    new Vector3(x - s * 0.02f, s * 0.12f, z + s * 0.06f), accent * 0.94f);
            }
        }
    }

    // ── Organic / asymmetric / radiant pilot station geometry ───────────────

    /// <summary>Wide bio-dome pad — soft 8-facet lobe rim + teal vein accent band.</summary>
    private static void OrganicAddBioDomePad(RaceMeshWriter w, float s, float padR, float innerR, float rimH,
        Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        RetroAddBoxUniform(w, -innerR, innerR, 0, rimH, -innerR, innerR, primary);

        int lobes = 8;
        float lobeW = padR * 0.22f;
        float lobeD = (padR - innerR) * 0.52f;
        for (int i = 0; i < lobes; i++)
        {
            float a = MathF.PI * 2f * i / lobes;
            float midR = (padR + innerR) * 0.5f;
            float cx = MathF.Cos(a) * midR;
            float cz = MathF.Sin(a) * midR;
            float tangX = -MathF.Sin(a) * lobeW;
            float tangZ = MathF.Cos(a) * lobeW;
            float radialX = MathF.Cos(a) * lobeD;
            float radialZ = MathF.Sin(a) * lobeD;
            RetroAddBoxUniform(w,
                cx - tangX - radialX, cx + tangX + radialX,
                0, rimH * 0.92f,
                cz - tangZ - radialZ, cz + tangZ + radialZ,
                secondary * 0.96f);
        }

        float veinH = s * 0.018f;
        for (int i = 0; i < lobes; i++)
        {
            float a = MathF.PI * 2f * i / lobes;
            float cx = MathF.Cos(a) * padR * 0.97f;
            float cz = MathF.Sin(a) * padR * 0.97f;
            float tangW = padR * 0.11f;
            RetroAddBoxUniform(w, cx - tangW, cx + tangW, rimH * 0.78f, rimH * 0.78f + veinH,
                cz - tangW * 0.5f, cz + tangW * 0.5f, accent);
        }
    }

    /// <summary>Low bio-dome repair pod on pad deck — height ≤0.12s.</summary>
    private static void OrganicAddPodCluster(RaceMeshWriter w, float cx, float cy, float cz, float radius,
        float s, Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        float podH = s * 0.10f;
        RetroAddFacetedColumn(w, cx, cz, cy, cy + podH * 0.55f, radius, radius * 0.72f, 7, secondary, secondary);
        RetroAddFacetedColumn(w, cx, cz, cy + podH * 0.55f, cy + podH, radius * 0.72f, radius * 0.12f, 6, primary, primary);
        float crownH = s * 0.014f;
        RetroAddBoxUniform(w, cx - radius * 0.45f, cx + radius * 0.45f, cy + podH, cy + podH + crownH,
            cz - radius * 0.45f, cz + radius * 0.45f, accent);
    }

    /// <summary>Curved vine connector arm reaching +Z from pad edge.</summary>
    private static void OrganicAddVineConnectorArm(RaceMeshWriter w, float startX, float startY, float startZ,
        float reachZ, float s, Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        int segs = 4;
        float armW = s * 0.05f;
        float prevX = startX;
        float prevY = startY;
        float prevZ = startZ;
        for (int seg = 1; seg <= segs; seg++)
        {
            float t = seg / (float)segs;
            float curveX = startX + MathF.Sin(t * MathF.PI * 0.42f) * s * 0.06f;
            float curveY = startY + t * s * 0.04f;
            float curveZ = startZ + (reachZ - startZ) * t;
            float midX = (prevX + curveX) * 0.5f;
            float midY = (prevY + curveY) * 0.5f;
            float midZ = (prevZ + curveZ) * 0.5f;
            RetroAddBoxUniform(w,
                midX - armW, midX + armW,
                midY, midY + s * 0.035f,
                midZ - armW * 0.8f, midZ + armW * 0.8f,
                seg % 2 == 0 ? primary * 0.94f : secondary * 0.92f);
            prevX = curveX;
            prevY = curveY;
            prevZ = curveZ;
        }

        float tipX = prevX;
        float tipZ = prevZ;
        RetroAddBoxUniform(w, tipX - armW * 0.7f, tipX + armW * 0.7f, prevY, prevY + s * 0.05f,
            tipZ - armW * 0.5f, tipZ + armW * 1.2f, accent * 0.98f);
        RetroAddFacetedColumn(w, tipX, tipZ + armW * 0.8f, prevY + s * 0.04f, prevY + s * 0.09f,
            armW * 0.9f, armW * 0.25f, 5, accent, accent);
    }

    /// <summary>Central 6-facet bio-dome hub — low crown, plan-dominant.</summary>
    private static void OrganicAddDomeHub(RaceMeshWriter w, float cx, float cy, float cz, float radius, float height,
        float s, Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        float baseH = height * 0.55f;
        RetroAddFacetedColumn(w, cx, cz, cy, cy + baseH, radius, radius * 0.78f, 6, secondary, secondary);
        RetroAddFacetedColumn(w, cx, cz, cy + baseH, cy + height, radius * 0.78f, radius * 0.14f, 6, primary, primary);
        float crownH = s * 0.012f;
        RetroAddBoxUniform(w, cx - radius * 0.35f, cx + radius * 0.35f, cy + height, cy + height + crownH,
            cz - radius * 0.35f, cz + radius * 0.35f, accent);
    }

    /// <summary>Low dish pod on deck ring — flush comms footprint.</summary>
    private static void OrganicAddDishPod(RaceMeshWriter w, float cx, float cy, float cz, float reach, float s,
        Vector3 primary, Vector3 secondary, Vector3 accent)
        => RetroAddCommsDishLow(w, cx, cy, cz, reach, s, primary, secondary, accent);

    /// <summary>Curved vine gantry arm reaching ±X from pad edge.</summary>
    private static void OrganicAddVineGantry(RaceMeshWriter w, float startX, float startY, float startZ,
        float reachX, float s, Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        int segs = 4;
        float armW = s * 0.05f;
        float prevX = startX;
        float prevY = startY;
        float prevZ = startZ;
        for (int seg = 1; seg <= segs; seg++)
        {
            float t = seg / (float)segs;
            float curveX = startX + (reachX - startX) * t;
            float curveY = startY + t * s * 0.03f;
            float curveZ = startZ + MathF.Sin(t * MathF.PI * 0.5f) * s * 0.05f;
            float midX = (prevX + curveX) * 0.5f;
            float midY = (prevY + curveY) * 0.5f;
            float midZ = (prevZ + curveZ) * 0.5f;
            RetroAddBoxUniform(w,
                midX - armW, midX + armW,
                midY, midY + s * 0.032f,
                midZ - armW * 0.8f, midZ + armW * 0.8f,
                seg % 2 == 0 ? primary * 0.94f : secondary * 0.92f);
            prevX = curveX;
            prevY = curveY;
            prevZ = curveZ;
        }

        float tipY = prevY;
        RetroAddBoxUniform(w, prevX - armW * 0.7f, prevX + armW * 1.4f, tipY, tipY + s * 0.05f,
            prevZ - armW * 0.5f, prevZ + armW * 0.5f, accent * 0.98f);
        OrganicAddPodCluster(w, prevX + armW * 0.6f, tipY, prevZ, armW * 1.1f, s, primary, secondary, accent);
    }

    /// <summary>Organic vine intake stack — low wobble column ≤0.14s.</summary>
    private static void OrganicAddVineStack(RaceMeshWriter w, float cx, float cy, float cz, float height, float s,
        Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        int segs = 3;
        float stackW = s * 0.055f;
        float prevY = cy;
        for (int seg = 1; seg <= segs; seg++)
        {
            float t = seg / (float)segs;
            float segY = cy + height * t;
            float wobbleX = MathF.Sin(t * MathF.PI * 0.85f) * s * 0.025f;
            RetroAddBoxUniform(w, cx + wobbleX - stackW, cx + wobbleX + stackW, prevY, segY,
                cz - stackW * 0.7f, cz + stackW * 0.7f, seg % 2 == 0 ? primary * 0.94f : secondary * 0.92f);
            prevY = segY;
        }

        RetroAddFacetedColumn(w, cx, cz, prevY, prevY + s * 0.04f, stackW * 1.1f, stackW * 0.28f, 5, accent, accent);
    }

    /// <summary>Coolant vine ring segment on deck around reactor dome.</summary>
    private static void OrganicAddVineRing(RaceMeshWriter w, float cx, float cy, float cz, float radius, float s,
        Vector3 primary, Vector3 secondary, Vector3 accent, int segments = 8)
    {
        for (int i = 0; i < segments; i++)
        {
            float a0 = MathF.PI * 2f * i / segments;
            float a1 = MathF.PI * 2f * (i + 1) / segments;
            float midA = (a0 + a1) * 0.5f;
            float midX = cx + MathF.Cos(midA) * radius;
            float midZ = cz + MathF.Sin(midA) * radius;
            float armW = s * 0.034f;
            RetroAddBoxUniform(w, midX - armW, midX + armW, cy, cy + s * 0.024f, midZ - armW, midZ + armW,
                i % 2 == 0 ? primary * 0.93f : secondary * 0.91f);
        }
    }

    /// <summary>Violet engine-well glow band — matches aetherian ship engine (0.55, 0.25, 1).</summary>
    private static void OrganicAddEngineGlowWell(RaceMeshWriter w, float cx, float cy, float cz, float s)
    {
        Vector3 engine = new(0.55f, 0.25f, 1f);
        RetroAddBoxUniform(w, cx - s * 0.07f, cx + s * 0.07f, cy, cy + s * 0.03f, cz - s * 0.07f, cz + s * 0.07f, engine * 0.88f);
        RetroAddBoxUniform(w, cx - s * 0.045f, cx + s * 0.045f, cy + s * 0.02f, cy + s * 0.05f, cz - s * 0.045f, cz + s * 0.045f, engine * 1.06f);
    }

    /// <summary>Low barrel pod on raised dome turret pad.</summary>
    private static void OrganicAddBarrelPod(RaceMeshWriter w, float cx, float cy, float cz, float length, float s,
        Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        RetroAddBoxUniform(w, cx - s * 0.035f, cx + s * 0.035f, cy, cy + s * 0.035f, cz, cz + length * 0.28f, secondary);
        RetroAddFacetedColumn(w, cx, cz + length * 0.32f, cy + s * 0.02f, cy + s * 0.05f, s * 0.032f, s * 0.018f, 5, accent, accent);
    }

    /// <summary>Low wide dark spiny pad — uniform deck tiers, no facet wedges.</summary>
    private static void SpinyAddWidePad(RaceMeshWriter w, float s, float padR, float innerR, float rimH,
        Vector3 primary, Vector3 secondary)
    {
        RetroAddBoxUniform(w, -innerR, innerR, 0, rimH, -innerR, innerR, secondary * 0.94f);

        int segs = 10;
        float tangW = padR * 0.22f;
        float radialW = (padR - innerR) * 0.45f;
        for (int i = 0; i < segs; i++)
        {
            float a = MathF.PI * 2f * i / segs;
            float cx = MathF.Cos(a) * (padR + innerR) * 0.5f;
            float cz = MathF.Sin(a) * (padR + innerR) * 0.5f;
            RetroAddBoxUniform(w, cx - tangW, cx + tangW, 0, rimH * 0.92f, cz - radialW, cz + radialW, primary);
        }

        for (int cardinal = 0; cardinal < 4; cardinal++)
        {
            float a = MathF.PI * 0.5f * cardinal;
            float cx = MathF.Cos(a) * padR * 0.96f;
            float cz = MathF.Sin(a) * padR * 0.96f;
            float tangX = MathF.Cos(a) * padR * 0.09f;
            float tangZ = MathF.Sin(a) * padR * 0.09f;
            RetroAddBoxUniform(w, cx - tangX, cx + tangX, 0, rimH * 0.88f, cz - tangZ, cz + tangZ, secondary);
        }
    }

    /// <summary>Void-purple accent band on pad rim edge for RaceIdentity lift.</summary>
    private static void SpinyAddVoidAccentBand(RaceMeshWriter w, float s, float padR, float y, float bandH,
        int segments, Vector3 accent)
        => RetroAddPadAccentRing(w, s, padR, y, bandH, segments, accent);

    /// <summary>Low radial spike-dish node — reach scales with protrusion modifier.</summary>
    private static void SpinyAddSpikeCluster(RaceMeshWriter w, float cx, float cy, float cz, float angle, float reach,
        float s, Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        var dir = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
        float perpX = -dir.Z * s * 0.04f;
        float perpZ = dir.X * s * 0.04f;
        float dishH = s * 0.14f;

        RetroAddBoxUniform(w, cx - perpX, cx + perpX, cy, cy + s * 0.04f, cz - perpZ, cz + perpZ, secondary);
        RetroAddBoxUniform(w,
            cx - reach * 0.30f + dir.X * reach * 0.08f, cx + reach * 0.30f + dir.X * reach * 0.08f,
            cy + s * 0.04f, cy + s * 0.08f, cz - reach * 0.22f, cz + reach * 0.10f, primary * 0.92f);
        float tipX = cx + dir.X * reach * 0.32f;
        float tipZ = cz + dir.Z * reach * 0.32f;
        RetroAddBoxUniform(w, tipX - s * 0.02f, tipX + s * 0.02f, cy + s * 0.08f, cy + dishH, tipZ - s * 0.02f, tipZ + s * 0.02f, accent);
        RetroAddBoxUniform(w, cx - s * 0.015f, cx + s * 0.015f, cy + s * 0.06f, cy + dishH * 0.82f,
            cz - dir.Z * reach * 0.06f, cz - dir.Z * reach * 0.02f, accent * 0.96f);
    }

    /// <summary>Clustered deck thorn spikes — plan-dominant massing, not a lone vertical tower.</summary>
    private static void SpinyAddThornSuperstructure(RaceMeshWriter w, float cx, float cy, float cz, float scale, float s,
        Vector3 primary, Vector3 secondary, Vector3 accent, float protrusion)
    {
        float clusterSpread = scale * (0.55f + protrusion * 0.15f);
        ReadOnlySpan<float> offX = stackalloc float[] { 0.0f, 0.18f, -0.14f, 0.10f, -0.08f };
        ReadOnlySpan<float> offZ = stackalloc float[] { 0.0f, -0.12f, 0.16f, 0.14f, -0.18f };
        float maxH = s * 0.12f;

        RetroAddBoxUniform(w, cx - clusterSpread * 0.6f, cx + clusterSpread * 0.6f, cy, cy + s * 0.03f,
            cz - clusterSpread * 0.6f, cz + clusterSpread * 0.6f, primary * 0.90f);

        for (int t = 0; t < 5; t++)
        {
            float px = cx + offX[t] * clusterSpread;
            float pz = cz + offZ[t] * clusterSpread;
            float thornH = maxH * (0.75f + t * 0.05f);
            float thornW = s * (0.025f + protrusion * 0.008f);
            RetroAddBoxUniform(w, px - thornW, px + thornW, cy + s * 0.03f, cy + s * 0.03f + thornH * 0.65f,
                pz - thornW, pz + thornW, secondary * 0.95f);
            RetroAddBoxUniform(w, px - thornW * 0.7f, px + thornW * 0.7f, cy + s * 0.03f + thornH * 0.65f,
                cy + s * 0.03f + thornH, pz - thornW * 0.7f, pz + thornW * 0.7f, accent * (t % 2 == 0 ? 1.04f : 0.98f));
        }
    }

    /// <summary>Spike connector arm from pad edge toward functional dock/repair/cargo reach.</summary>
    private static void SpinyAddSpikeConnectorArm(RaceMeshWriter w, float startX, float startY, float startZ,
        float endX, float endZ, float s, float protrusion, Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        int segs = 3;
        float armW = s * (0.032f + protrusion * 0.008f);
        float prevX = startX;
        float prevY = startY;
        float prevZ = startZ;
        for (int seg = 1; seg <= segs; seg++)
        {
            float t = seg / (float)segs;
            float curveX = startX + (endX - startX) * t;
            float curveY = startY + t * s * 0.03f;
            float curveZ = startZ + (endZ - startZ) * t;
            float midX = (prevX + curveX) * 0.5f;
            float midY = (prevY + curveY) * 0.5f;
            float midZ = (prevZ + curveZ) * 0.5f;
            float halfLenX = MathF.Abs(curveX - prevX) * 0.5f + armW;
            float halfLenZ = MathF.Abs(curveZ - prevZ) * 0.5f + armW * 0.6f;
            RetroAddBoxUniform(w, midX - halfLenX, midX + halfLenX, prevY, curveY + s * 0.02f,
                midZ - halfLenZ, midZ + halfLenZ, seg % 2 == 0 ? secondary * 0.94f : primary * 0.92f);
            prevX = curveX;
            prevY = curveY;
            prevZ = curveZ;
        }

        RetroAddBoxUniform(w, endX - armW * 0.8f, endX + armW * 0.8f, startY + s * 0.02f, startY + s * 0.06f,
            endZ - armW, endZ + armW * 0.4f, accent);
        RetroAddBoxUniform(w, endX - armW * 0.5f, endX + armW * 0.5f, startY + s * 0.06f, startY + s * 0.10f,
            endZ + armW * 0.2f, endZ + armW * 0.7f, accent * 1.06f);
    }

    /// <summary>Thorn gantry arm reaching ±X from pad edge — shipyard dock spike arms.</summary>
    private static void SpinyAddSpikeGantry(RaceMeshWriter w, float startX, float startY, float startZ,
        float reachX, float s, float protrusion, Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        int segs = 4;
        float armW = s * (0.038f + protrusion * 0.010f);
        float prevX = startX;
        float prevY = startY;
        float prevZ = startZ;
        for (int seg = 1; seg <= segs; seg++)
        {
            float t = seg / (float)segs;
            float curveX = startX + (reachX - startX) * t;
            float curveY = startY + t * s * 0.025f;
            float curveZ = startZ + MathF.Sin(t * MathF.PI * 0.5f) * s * 0.04f;
            float midX = (prevX + curveX) * 0.5f;
            float midY = (prevY + curveY) * 0.5f;
            float midZ = (prevZ + curveZ) * 0.5f;
            float halfLenX = MathF.Abs(curveX - prevX) * 0.5f + armW;
            float halfLenZ = MathF.Abs(curveZ - prevZ) * 0.5f + armW * 0.55f;
            RetroAddBoxUniform(w, midX - halfLenX, midX + halfLenX, prevY, curveY + s * 0.018f,
                midZ - halfLenZ, midZ + halfLenZ, seg % 2 == 0 ? primary * 0.93f : secondary * 0.91f);
            prevX = curveX;
            prevY = curveY;
            prevZ = curveZ;
        }

        float tipH = s * (0.08f + protrusion * 0.03f);
        RetroAddBoxUniform(w, reachX - armW, reachX + armW * 0.6f, startY + s * 0.02f, startY + tipH * 0.65f,
            startZ - armW * 0.7f, startZ + armW * 0.7f, secondary);
        RetroAddBoxUniform(w, reachX - armW * 0.7f, reachX + armW * 0.4f, startY + tipH * 0.65f, startY + tipH,
            startZ - armW * 0.5f, startZ + armW * 0.5f, accent * 1.04f);
    }

    /// <summary>Low deck bay cutout slot — repair_bay open bay read at RTS zoom.</summary>
    private static void SpinyAddBayCutout(RaceMeshWriter w, float cx, float cy, float cz, float halfW, float halfD, float s,
        Vector3 secondary)
    {
        RetroAddBoxUniform(w, cx - halfW, cx + halfW, cy, cy + s * 0.018f, cz - halfD, cz + halfD, secondary * 0.76f);
        RetroAddBoxUniform(w, cx - halfW * 0.85f, cx + halfW * 0.85f, cy + s * 0.018f, cy + s * 0.028f,
            cz - halfD * 0.9f, cz + halfD * 0.9f, secondary * 0.82f);
    }

    /// <summary>Void intake thorn stack — refinery processor pods.</summary>
    private static void SpinyAddThornStack(RaceMeshWriter w, float cx, float cy, float cz, float height, float s,
        float protrusion, Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        float stackW = s * (0.08f + protrusion * 0.02f);
        RetroAddBoxUniform(w, cx - stackW, cx + stackW, cy, cy + height * 0.55f, cz - stackW * 0.8f, cz + stackW * 0.8f, primary);
        RetroAddBoxUniform(w, cx - stackW * 0.7f, cx + stackW * 0.7f, cy + height * 0.55f, cy + height,
            cz - stackW * 0.6f, cz + stackW * 0.6f, secondary * 0.95f);
        RetroAddBoxUniform(w, cx - s * 0.025f, cx + s * 0.025f, cy + height, cy + height + s * 0.04f,
            cz - s * 0.02f, cz + s * 0.02f, accent);
    }

    /// <summary>Void engine glow well — matches voidborn ship engine palette.</summary>
    private static void SpinyAddVoidEngineGlowWell(RaceMeshWriter w, float cx, float cy, float cz, float s, Vector3 engine)
    {
        RetroAddBoxUniform(w, cx - s * 0.08f, cx + s * 0.08f, cy, cy + s * 0.025f, cz - s * 0.08f, cz + s * 0.08f,
            new Vector3(0.12f, 0.06f, 0.22f));
        RetroAddBoxUniform(w, cx - s * 0.055f, cx + s * 0.055f, cy + s * 0.02f, cy + s * 0.045f, cz - s * 0.055f, cz + s * 0.055f,
            engine * 0.92f);
        RetroAddBoxUniform(w, cx - s * 0.035f, cx + s * 0.035f, cy + s * 0.04f, cy + s * 0.055f, cz - s * 0.035f, cz + s * 0.035f,
            engine * 1.08f);
    }

    /// <summary>Radial spike ring around center well — reactor crown tier.</summary>
    private static void SpinyAddSpikeRing(RaceMeshWriter w, float cx, float cy, float cz, float radius, float s,
        float protrusion, Vector3 primary, Vector3 secondary, Vector3 accent, float yawOffset = 0f)
    {
        int count = 8;
        float spikeReach = s * (0.08f + protrusion * 0.05f);
        for (int i = 0; i < count; i++)
        {
            float ang = MathF.PI * 2f * i / count + yawOffset;
            float dx = cx + MathF.Cos(ang) * radius;
            float dz = cz + MathF.Sin(ang) * radius;
            SpinyAddSpikeCluster(w, dx, cy, dz, ang, spikeReach, s, primary, secondary, accent);
        }
    }

    /// <summary>Thorn turret ring + low barrel spikes — defense emplacement.</summary>
    private static void SpinyAddTurretRing(RaceMeshWriter w, float cx, float cy, float cz, float radius, float s,
        float protrusion, Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        int ringSegs = 6;
        for (int i = 0; i < ringSegs; i++)
        {
            float ang = MathF.PI * 2f * i / ringSegs;
            float rx = cx + MathF.Cos(ang) * radius;
            float rz = cz + MathF.Sin(ang) * radius;
            float thornH = s * (0.08f + protrusion * 0.03f);
            float thornW = s * 0.022f;
            RetroAddBoxUniform(w, rx - thornW, rx + thornW, cy, cy + thornH * 0.7f, rz - thornW, rz + thornW, secondary);
            RetroAddBoxUniform(w, rx - thornW * 0.7f, rx + thornW * 0.7f, cy + thornH * 0.7f, cy + thornH,
                rz - thornW * 0.7f, rz + thornW * 0.7f, accent * (i % 2 == 0 ? 1.04f : 0.98f));
        }

        RetroAddBoxUniform(w, cx - s * 0.04f, cx + s * 0.04f, cy + s * 0.04f, cy + s * 0.07f, cz + s * 0.06f, cz + s * 0.18f, primary);
        RetroAddBoxUniform(w, cx - s * 0.025f, cx + s * 0.025f, cy + s * 0.07f, cy + s * 0.10f, cz + s * 0.16f, cz + s * 0.24f, accent);
        RetroAddBoxUniform(w, cx - s * 0.035f, cx - s * 0.01f, cy + s * 0.05f, cy + s * 0.08f, cz + s * 0.10f, cz + s * 0.16f, secondary * 0.94f);
        RetroAddBoxUniform(w, cx + s * 0.01f, cx + s * 0.035f, cy + s * 0.05f, cy + s * 0.08f, cz + s * 0.10f, cz + s * 0.16f, secondary * 0.94f);
    }

    /// <summary>Small spike pod cluster on deck — shipyard bay clusters.</summary>
    private static void SpinyAddSpikePodCluster(RaceMeshWriter w, float cx, float cy, float cz, float scale, float s,
        float protrusion, Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        SpinyAddThornSuperstructure(w, cx, cy, cz, scale, s, primary, secondary, accent, protrusion);
        float reach = s * (0.06f + protrusion * 0.04f);
        SpinyAddSpikeCluster(w, cx, cy, cz + scale * 0.35f, MathF.PI * 0.5f, reach, s, primary, secondary, accent);
    }

    /// <summary>Faceted crystal pad — 6–8 facet rim blocks on pad perimeter.</summary>
    private static void CrystallineAddFacetedPad(RaceMeshWriter w, float s, float padR, float innerR, float rimH,
        int facetBlocks, Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        RetroAddBoxUniform(w, -innerR, innerR, 0, rimH, -innerR, innerR, primary);

        int blocks = Math.Clamp(facetBlocks, 6, 8);
        for (int i = 0; i < blocks; i++)
        {
            float a = MathF.PI * 2f * i / blocks;
            float cx = MathF.Cos(a) * padR * 0.94f;
            float cz = MathF.Sin(a) * padR * 0.94f;
            float tangW = padR * 0.11f;
            CrystallineAddCrystalFacet(w, cx, cz, rimH * 0.95f, tangW,
                i % 3 == 0 ? secondary * 0.96f : primary * 0.94f, accent * 0.98f);
        }

        for (int inner = 0; inner < blocks; inner++)
        {
            float a = MathF.PI * 2f * inner / blocks + MathF.PI / blocks;
            float ix = MathF.Cos(a) * innerR * 0.72f;
            float iz = MathF.Sin(a) * innerR * 0.72f;
            RetroAddBoxUniform(w, ix - s * 0.035f, ix + s * 0.035f, rimH, rimH + s * 0.012f, iz - s * 0.035f, iz + s * 0.035f,
                secondary * 0.92f);
        }
    }

    /// <summary>Single faceted rim block on pad perimeter.</summary>
    private static void CrystallineAddCrystalFacet(RaceMeshWriter w, float cx, float cz, float height, float halfW,
        Vector3 body, Vector3 cap)
    {
        RetroAddBoxUniform(w, cx - halfW, cx + halfW, 0, height, cz - halfW * 0.55f, cz + halfW * 0.55f, body);
        RetroAddBoxUniform(w, cx - halfW * 0.75f, cx + halfW * 0.75f, height, height + height * 0.12f,
            cz - halfW * 0.45f, cz + halfW * 0.45f, cap);
    }

    /// <summary>Horizontal ice-blue frost tier band on hub faces + pad deck.</summary>
    private static void CrystallineAddFrostPanelBand(RaceMeshWriter w, float cx, float cy, float cz, float radius,
        float bandH, int segments, Vector3 primary, Vector3 accent)
    {
        float tangW = radius * 0.18f;
        Vector3 frost = Vector3.Lerp(primary, accent, 0.35f);
        for (int i = 0; i < segments; i++)
        {
            float a = MathF.PI * 2f * i / segments;
            float px = cx + MathF.Cos(a) * radius;
            float pz = cz + MathF.Sin(a) * radius;
            RetroAddBoxUniform(w, px - tangW, px + tangW, cy, cy + bandH, pz - tangW * 0.5f, pz + tangW * 0.5f, frost);
        }
    }

    /// <summary>Prismatic core glow well — cryo engine luminance at hub center.</summary>
    private static void CrystallineAddPrismaticCore(RaceMeshWriter w, float cx, float cy, float cz, float s)
    {
        Vector3 engine = new(0.4f, 0.9f, 1.0f);
        Vector3 prismatic = new(0.85f, 0.98f, 1.0f);
        RetroAddBoxUniform(w, cx - s * 0.08f, cx + s * 0.08f, cy, cy + s * 0.025f, cz - s * 0.08f, cz + s * 0.08f,
            new Vector3(0.32f, 0.58f, 0.78f));
        RetroAddBoxUniform(w, cx - s * 0.055f, cx + s * 0.055f, cy + s * 0.02f, cy + s * 0.045f, cz - s * 0.055f, cz + s * 0.055f,
            engine * 0.92f);
        RetroAddBoxUniform(w, cx - s * 0.035f, cx + s * 0.035f, cy + s * 0.04f, cy + s * 0.055f, cz - s * 0.035f, cz + s * 0.035f,
            prismatic * 1.08f);
    }

    /// <summary>Ice-blue frost accent band on pad rim perimeter.</summary>
    private static void CrystallineAddPadFrostBand(RaceMeshWriter w, float padR, float rimH, float bandH, int segments,
        Vector3 primary, Vector3 accent)
    {
        float tangW = padR * 0.10f;
        Vector3 frost = Vector3.Lerp(primary, accent, 0.42f);
        for (int i = 0; i < segments; i++)
        {
            float a = MathF.PI * 2f * i / segments;
            float px = MathF.Cos(a) * padR * 0.90f;
            float pz = MathF.Sin(a) * padR * 0.90f;
            RetroAddBoxUniform(w, px - tangW, px + tangW, rimH * 0.88f, rimH * 0.88f + bandH, pz - tangW * 0.45f, pz + tangW * 0.45f, frost);
        }
    }

    /// <summary>Low facet dish node on deck — radial sensor footprint, not tall mast.</summary>
    private static void CrystallineAddFacetDishLow(RaceMeshWriter w, float cx, float cy, float cz, float reach, float s,
        Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        RetroAddBoxUniform(w, cx - s * 0.05f, cx + s * 0.05f, cy, cy + s * 0.04f, cz - s * 0.05f, cz + s * 0.05f, secondary);
        RetroAddBoxUniform(w, cx - reach * 0.45f, cx + reach * 0.35f, cy + s * 0.04f, cy + s * 0.07f,
            cz - reach * 0.28f, cz + reach * 0.10f, primary * 0.94f);
        RetroAddBoxUniform(w, cx - s * 0.025f, cx + s * 0.025f, cy + s * 0.07f, cy + s * 0.10f,
            cz + reach * 0.06f, cz + reach * 0.10f, accent * 0.98f);
    }

    /// <summary>Side crystal gantry arm extending from pad rim.</summary>
    private static void CrystallineAddCrystalGantry(RaceMeshWriter w, float cx, float cy, float cz, float angle, float reach, float s,
        int facets, Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        float dirX = MathF.Cos(angle);
        float dirZ = MathF.Sin(angle);
        float armW = s * 0.05f;
        float tipX = cx + dirX * reach;
        float tipZ = cz + dirZ * reach;
        RetroAddBoxUniform(w, cx - armW, cx + armW, cy, cy + s * 0.14f, cz - armW * 0.7f, cz + armW * 0.7f, secondary * 0.94f);
        RetroAddBoxUniform(w, tipX - armW * 0.8f, tipX + armW * 0.6f, cy + s * 0.02f, cy + s * 0.10f,
            tipZ - armW, tipZ + armW * 0.5f, primary * 0.92f);
        RetroAddBoxUniform(w, tipX - armW * 0.5f, tipX + armW * 0.4f, cy + s * 0.10f, cy + s * 0.13f,
            tipZ + armW * 0.2f, tipZ + armW * 0.7f, accent * 0.98f);
        CrystallineAddFrostPanelBand(w, cx + dirX * reach * 0.45f, cy + s * 0.08f, cz + dirZ * reach * 0.45f,
            reach * 0.22f, s * 0.012f, facets, primary, accent);
    }

    /// <summary>Prismatic reactor core well with cryo engine glow tiers.</summary>
    private static void CrystallineAddReactorCoreWell(RaceMeshWriter w, float cx, float cy, float cz, float s,
        int facetCount, Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        Vector3 engine = new(0.4f, 0.9f, 1.0f);
        float wellR = s * 0.14f;
        RetroAddFacetedColumn(w, cx, cz, cy, cy + s * 0.06f, wellR, wellR * 0.78f, facetCount, secondary, primary);
        CrystallineAddFrostPanelBand(w, cx, cy + s * 0.04f, cz, wellR * 0.88f, s * 0.014f, facetCount, primary, accent);
        CrystallineAddPrismaticCore(w, cx, cy + s * 0.01f, cz, s);
        RetroAddBoxUniform(w, cx - s * 0.04f, cx + s * 0.04f, cy + s * 0.05f, cy + s * 0.08f, cz - s * 0.04f, cz + s * 0.04f, engine);
        RetroAddBoxUniform(w, cx - s * 0.03f, cx + s * 0.03f, cy + s * 0.07f, cy + s * 0.10f, cz - s * 0.03f, cz + s * 0.03f, engine * 1.12f);
        RetroAddBoxUniform(w, cx - wellR * 0.55f, cx + wellR * 0.55f, cy + s * 0.10f, cy + s * 0.12f, cz - wellR * 0.55f, cz + wellR * 0.55f, accent);
    }

    /// <summary>Offset hub mass block cluster — hub shifted +X by asymmetry offset.</summary>
    private static void AsymmetricAddOffsetHub(RaceMeshWriter w, float offsetX, float rimH, float scale,
        Vector3 primary, Vector3 secondary, Vector3 accent, int tiers = 3)
    {
        RetroAddBox(w, offsetX - scale * 0.26f, offsetX + scale * 0.20f, rimH, rimH + scale * 0.10f,
            -scale * 0.22f, scale * 0.18f, secondary, primary);
        RetroAddBox(w, offsetX - scale * 0.20f, offsetX + scale * 0.16f, rimH + scale * 0.10f, rimH + scale * 0.18f,
            -scale * 0.16f, scale * 0.14f, primary, secondary);
        if (tiers >= 3)
        {
            RetroAddBox(w, offsetX - scale * 0.14f, offsetX + scale * 0.12f, rimH + scale * 0.18f, rimH + scale * 0.24f,
                -scale * 0.12f, scale * 0.10f, secondary, primary);
            RetroAddBoxUniform(w, offsetX - scale * 0.16f, offsetX + scale * 0.14f, rimH + scale * 0.24f, rimH + scale * 0.26f,
                -scale * 0.14f, scale * 0.12f, accent);
        }

        RetroAddBoxUniform(w, offsetX + scale * 0.08f, offsetX + scale * 0.22f, rimH, rimH + scale * 0.14f,
            scale * 0.04f, scale * 0.20f, primary * 0.92f);
        RetroAddBoxUniform(w, offsetX - scale * 0.06f, offsetX + scale * 0.10f, rimH + scale * 0.06f, rimH + scale * 0.16f,
            -scale * 0.20f, -scale * 0.06f, secondary * 0.94f);
    }

    /// <summary>Wide asymmetric pad — staggered ±Z extensions, not symmetric ring.</summary>
    private static void AsymmetricAddStaggeredPad(RaceMeshWriter w, float s, float padR, float rimH, float offsetX,
        Vector3 primary, Vector3 secondary)
    {
        float innerR = padR * 0.58f;
        RetroAddBoxUniform(w, offsetX - innerR, offsetX + innerR * 0.88f, 0, rimH, -innerR * 0.82f, innerR * 0.78f, primary);

        ReadOnlySpan<float> extX = stackalloc float[] { 0.72f, 0.58f, 0.84f };
        ReadOnlySpan<float> extZ = stackalloc float[] { 0.38f, -0.48f, -0.22f };
        ReadOnlySpan<float> extLen = stackalloc float[] { 0.28f, 0.32f, 0.24f };
        for (int e = 0; e < 3; e++)
        {
            float cx = offsetX + extX[e] * padR;
            float cz = extZ[e] * padR;
            float halfLen = extLen[e] * padR;
            float halfW = padR * 0.14f;
            RetroAddBoxUniform(w, cx - halfLen, cx + halfLen, 0, rimH * 0.88f, cz - halfW, cz + halfW,
                e % 2 == 0 ? secondary * 0.96f : primary * 0.94f);
        }

        RetroAddBoxUniform(w, offsetX + padR * 0.52f, offsetX + padR * 0.92f, 0, rimH * 0.82f,
            padR * 0.08f, padR * 0.28f, secondary * 0.92f);
        RetroAddBoxUniform(w, offsetX - padR * 0.18f, offsetX + padR * 0.42f, 0, rimH * 0.80f,
            -padR * 0.62f, -padR * 0.34f, primary * 0.93f);
    }

    /// <summary>Amber diagonal accent stripes on hub faces + pad extension tops.</summary>
    private static void AsymmetricAddDiagonalAccentStripe(RaceMeshWriter w, float offsetX, float rimH, float scale,
        Vector3 accent)
    {
        float stripeH = scale * 0.018f;
        RetroAddBoxUniform(w, offsetX - scale * 0.22f, offsetX - scale * 0.08f, rimH + scale * 0.08f, rimH + scale * 0.08f + stripeH,
            -scale * 0.14f, scale * 0.06f, accent);
        RetroAddBoxUniform(w, offsetX + scale * 0.04f, offsetX + scale * 0.18f, rimH + scale * 0.14f, rimH + scale * 0.14f + stripeH,
            -scale * 0.06f, scale * 0.16f, accent * 0.96f);
        RetroAddBoxUniform(w, offsetX + scale * 0.48f, offsetX + scale * 0.72f, rimH * 0.72f, rimH * 0.72f + stripeH,
            scale * 0.10f, scale * 0.24f, accent * 1.04f);
        RetroAddBoxUniform(w, offsetX - scale * 0.04f, offsetX + scale * 0.22f, rimH * 0.68f, rimH * 0.68f + stripeH,
            -scale * 0.52f, -scale * 0.28f, accent * 0.98f);
    }

    /// <summary>Amber bioluminescent accent wells on spine crown + pad rim.</summary>
    private static void AsymmetricAddAmberAccentWell(RaceMeshWriter w, float offsetX, float rimH, float scale, Vector3 accent)
    {
        RetroAddBoxUniform(w, offsetX - scale * 0.10f, offsetX + scale * 0.10f, rimH + scale * 0.20f, rimH + scale * 0.23f,
            -scale * 0.08f, scale * 0.08f, accent * 1.10f);
        RetroAddBoxUniform(w, offsetX + scale * 0.44f, offsetX + scale * 0.72f, rimH * 0.84f, rimH * 0.88f,
            -scale * 0.50f, -scale * 0.26f, accent * 1.06f);
        RetroAddBoxUniform(w, offsetX - scale * 0.04f, offsetX + scale * 0.16f, rimH + scale * 0.04f, rimH + scale * 0.07f,
            scale * 0.06f, scale * 0.22f, accent * 1.02f);
    }

    /// <summary>Concentric glow ring tier on pad deck — plan-dominant at RTS zoom.</summary>
    private static void RadiantAddGlowRing(RaceMeshWriter w, float cx, float cy, float cz, float radius, float tube,
        int segments, Vector3 color)
    {
        RetroAddCoolingRingFlush(w, cx, cy, cz, radius, tube, segments, color);
    }

    /// <summary>Solar collector petal arm radiating from center well.</summary>
    private static void RadiantAddSolarPetal(RaceMeshWriter w, float cx, float cy, float cz, float angle, float reach,
        float s, Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        var dir = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
        float perpX = -dir.Z * s * 0.06f;
        float perpZ = dir.X * s * 0.06f;
        float rootX = cx + dir.X * s * 0.10f;
        float rootZ = cz + dir.Z * s * 0.10f;
        float tipX = cx + dir.X * reach;
        float tipZ = cz + dir.Z * reach;
        float midX = (rootX + tipX) * 0.5f;
        float midZ = (rootZ + tipZ) * 0.5f;
        float halfLen = reach * 0.5f;
        RetroAddBoxUniform(w,
            midX - dir.X * halfLen - perpX, midX + dir.X * halfLen + perpX,
            cy, cy + s * 0.028f,
            midZ - dir.Z * halfLen - perpZ, midZ + dir.Z * halfLen + perpZ,
            primary * 0.95f);
        RetroAddBoxUniform(w,
            tipX - perpX * 0.75f, tipX + perpX * 0.75f,
            cy + s * 0.02f, cy + s * 0.05f,
            tipZ - perpZ * 0.75f, tipZ + perpZ * 0.75f,
            secondary * 0.92f);
        RetroAddBoxUniform(w,
            tipX - perpX * 0.45f, tipX + perpX * 0.45f,
            cy + s * 0.05f, cy + s * 0.058f,
            tipZ - perpZ * 0.45f, tipZ + perpZ * 0.45f,
            accent);
    }

    /// <summary>Gold/amber accent band on pad rim + ring tier caps.</summary>
    private static void RadiantAddConcentricBand(RaceMeshWriter w, float cx, float cy, float cz, float padR,
        float bandH, int segments, Vector3 accent)
    {
        RetroAddPadAccentRing(w, 1f, padR, cy, bandH, segments, accent);
    }

    /// <summary>Wide solar collector pad — 8-lobe rim + gold accent band.</summary>
    private static void RadiantAddSolarPad(RaceMeshWriter w, float padR, float innerR, float rimH, float s,
        Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        RetroAddBoxUniform(w, -innerR, innerR, 0, rimH, -innerR, innerR, primary);
        for (int lobe = 0; lobe < 8; lobe++)
        {
            float a = MathF.PI * 2f * lobe / 8f;
            float cx = MathF.Cos(a) * (padR + innerR) * 0.5f;
            float cz = MathF.Sin(a) * (padR + innerR) * 0.5f;
            float tangW = padR * 0.12f;
            RetroAddBoxUniform(w, cx - tangW, cx + tangW, 0, rimH * 0.90f, cz - tangW * 0.55f, cz + tangW * 0.55f,
                secondary * 0.95f);
        }
        RadiantAddConcentricBand(w, 0, rimH * 0.82f, 0, padR * 0.98f, s * 0.024f, 16, accent);
    }

    /// <summary>Stacked concentric glow ring tiers on pad deck.</summary>
    private static void RadiantAddGlowRingTiers(RaceMeshWriter w, float rimH, float s, ReadOnlySpan<float> ringR,
        Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        for (int ring = 0; ring < ringR.Length; ring++)
        {
            float radius = s * ringR[ring];
            float ringY = rimH + s * 0.012f + ring * s * 0.007f;
            RadiantAddGlowRing(w, 0, ringY, 0, radius, s * 0.028f, 14,
                ring % 2 == 0 ? accent : primary * 0.96f);
        }
    }

    private static void BuildOrganicRepairBay(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.12f;
        float innerR = s * 0.66f;
        float rimH = s * 0.05f;

        OrganicAddBioDomePad(w, s, padR, innerR, rimH, primary, secondary, accent);

        OrganicAddPodCluster(w, -s * 0.30f, rimH, -s * 0.20f, s * 0.14f, s, primary, secondary, accent);
        OrganicAddPodCluster(w, s * 0.24f, rimH, s * 0.14f, s * 0.13f, s, primary, secondary, accent);
        OrganicAddPodCluster(w, -s * 0.06f, rimH, s * 0.30f, s * 0.12f, s, primary, secondary, accent);

        OrganicAddVineConnectorArm(w, -s * 0.44f, rimH, padR * 0.82f, s * 0.58f, s, primary, secondary, accent);
        OrganicAddVineConnectorArm(w, s * 0.04f, rimH, padR * 0.90f, s * 0.66f, s, primary, secondary, accent);
        OrganicAddVineConnectorArm(w, s * 0.40f, rimH, padR * 0.86f, s * 0.62f, s, primary, secondary, accent);

        RetroAddBoxUniform(w, -s * 0.08f, s * 0.08f, rimH + s * 0.02f, rimH + s * 0.06f, -s * 0.08f, s * 0.08f,
            accent * 0.88f);
        RetroAddBoxUniform(w, -s * 0.05f, s * 0.05f, rimH + s * 0.04f, rimH + s * 0.07f, -s * 0.05f, s * 0.05f,
            accent * 1.06f);
    }

    private static void BuildOrganicCommandCenter(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.14f;
        float innerR = s * 0.68f;
        float rimH = s * 0.05f;

        OrganicAddBioDomePad(w, s, padR, innerR, rimH, primary, secondary, accent);
        OrganicAddDomeHub(w, 0, rimH, 0, s * 0.22f, s * 0.12f, s, primary, secondary, accent);

        ReadOnlySpan<float> dishA = stackalloc float[] { 0.25f, 1.10f, 2.05f, 3.00f };
        for (int d = 0; d < 4; d++)
        {
            float a = MathF.PI * 0.5f * dishA[d];
            float dx = MathF.Cos(a) * padR * 0.78f;
            float dz = MathF.Sin(a) * padR * 0.78f;
            OrganicAddDishPod(w, dx, rimH + s * 0.01f, dz, s * 0.11f, s, primary, secondary, accent);
        }
    }

    private static void BuildOrganicShipyard(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s,
        int gantries, int bays)
    {
        bool large = bays >= 5;
        bool medium = bays >= 3 && !large;
        float padR = large ? s * 1.20f : medium ? s * 1.12f : s * 1.08f;
        float innerR = padR * 0.60f;
        float rimH = s * 0.05f;
        int vineCount = large ? 5 : medium ? Math.Max(gantries, 3) : 2;
        int podCount = large ? 5 : medium ? 3 : 2;

        OrganicAddBioDomePad(w, s, padR, innerR, rimH, primary, secondary, accent);

        for (int v = 0; v < vineCount; v++)
        {
            float side = v % 2 == 0 ? -1f : 1f;
            float zSlot = -padR * 0.42f + v * padR * 0.18f;
            float startX = side * padR * 0.72f;
            float reachX = side * padR * (large ? 1.02f : medium ? 0.94f : 0.86f);
            OrganicAddVineGantry(w, startX, rimH, zSlot, reachX, s, primary, secondary, accent);
        }

        ReadOnlySpan<float> podX = large
            ? stackalloc float[] { -0.28f, 0.10f, 0.32f, -0.08f, 0.22f }
            : medium
                ? stackalloc float[] { -0.22f, 0.14f, -0.04f }
                : stackalloc float[] { -0.18f, 0.16f };
        ReadOnlySpan<float> podZ = large
            ? stackalloc float[] { -0.14f, 0.18f, -0.22f, 0.26f, 0.08f }
            : medium
                ? stackalloc float[] { 0.12f, -0.16f, 0.24f }
                : stackalloc float[] { 0.10f, -0.12f };

        for (int p = 0; p < podCount; p++)
        {
            if (large)
            {
                float px = podX[p] * s;
                float pz = podZ[p] * s;
                RetroAddBoxUniform(w, px - s * 0.08f, px + s * 0.08f, rimH + s * 0.08f, rimH + s * 0.11f,
                    pz - s * 0.06f, pz + s * 0.06f, p % 2 == 0 ? secondary * 0.94f : primary * 0.92f);
            }
            else
            {
                OrganicAddPodCluster(w, podX[p] * s, rimH, podZ[p] * s, s * (medium ? 0.12f : 0.11f), s, primary, secondary, accent);
            }
        }

        if (medium || large)
            OrganicAddDomeHub(w, 0, rimH, -s * 0.06f, s * (large ? 0.16f : 0.14f), s * 0.10f, s, primary, secondary, accent);
    }

    private static void BuildOrganicDefenseTurret(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 0.96f;
        float innerR = s * 0.56f;
        float rimH = s * 0.05f;

        OrganicAddBioDomePad(w, s, padR, innerR, rimH, primary, secondary, accent);
        OrganicAddDomeHub(w, 0, rimH + s * 0.02f, 0, s * 0.18f, s * 0.10f, s, primary, secondary, accent);

        for (int ring = 0; ring < 6; ring++)
        {
            float a = MathF.PI * 2f * ring / 6f;
            float rx = MathF.Cos(a) * s * 0.30f;
            float rz = MathF.Sin(a) * s * 0.30f;
            float crownH = s * 0.014f;
            RetroAddBoxUniform(w, rx - s * 0.03f, rx + s * 0.03f, rimH + s * 0.10f, rimH + s * 0.10f + crownH,
                rz - s * 0.03f, rz + s * 0.03f, ring % 2 == 0 ? accent : accent * 0.94f);
        }

        OrganicAddBarrelPod(w, 0, rimH + s * 0.08f, s * 0.14f, s * 0.22f, s, primary, secondary, accent);
        OrganicAddBarrelPod(w, -s * 0.10f, rimH + s * 0.07f, s * 0.10f, s * 0.16f, s, primary, secondary, accent);
        OrganicAddBarrelPod(w, s * 0.10f, rimH + s * 0.07f, s * 0.10f, s * 0.16f, s, primary, secondary, accent);
    }

    private static void BuildOrganicSensorArray(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.10f;
        float innerR = s * 0.64f;
        float rimH = s * 0.05f;

        OrganicAddBioDomePad(w, s, padR, innerR, rimH, primary, secondary, accent);

        ReadOnlySpan<float> dishA = stackalloc float[] { 0.35f, 1.20f, 2.40f, 3.55f };
        for (int d = 0; d < 4; d++)
        {
            float a = MathF.PI * 0.5f * dishA[d];
            float dx = MathF.Cos(a) * padR * 0.72f;
            float dz = MathF.Sin(a) * padR * 0.72f;
            OrganicAddDishPod(w, dx, rimH + s * 0.01f, dz, s * 0.12f, s, primary, secondary, accent);
        }

        OrganicAddPodCluster(w, 0, rimH, 0, s * 0.10f, s, primary, secondary, accent);
    }

    private static void BuildSpinySensorArray(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.12f;
        float innerR = s * 0.68f;
        float rimH = s * 0.05f;
        float protrusion = race.Modifiers.Protrusion;
        float yawOffset = MathF.PI * 0.10f;

        SpinyAddWidePad(w, s, padR, innerR, rimH, primary, secondary);
        SpinyAddVoidAccentBand(w, s, padR * 0.97f, rimH * 0.82f, s * 0.022f, 14, accent);

        int clusterCount = Math.Clamp(5 + (int)(protrusion * 2), 5, 7);
        float ringR = padR * 0.72f;
        float spikeReach = s * (0.10f + protrusion * 0.06f);
        for (int c = 0; c < clusterCount; c++)
        {
            float ang = MathF.PI * 2f * c / clusterCount + yawOffset;
            float dx = MathF.Cos(ang) * ringR;
            float dz = MathF.Sin(ang) * ringR;
            SpinyAddSpikeCluster(w, dx, rimH + s * 0.01f, dz, ang, spikeReach, s, primary, secondary, accent);
        }

        SpinyAddThornSuperstructure(w, 0, rimH, 0, s * 0.28f, s, primary, secondary, accent, protrusion);

        for (int well = 0; well < 8; well++)
        {
            float ang = MathF.PI * 2f * well / 8f + yawOffset;
            float wx = MathF.Cos(ang) * padR * 0.88f;
            float wz = MathF.Sin(ang) * padR * 0.88f;
            RetroAddBoxUniform(w, wx - s * 0.03f, wx + s * 0.03f, rimH, rimH + s * 0.015f, wz - s * 0.03f, wz + s * 0.03f,
                accent * 0.92f);
        }
    }

    private static void BuildSpinyCommandCenter(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.10f;
        float innerR = s * 0.66f;
        float rimH = s * 0.05f;
        float protrusion = race.Modifiers.Protrusion;
        float yawOffset = MathF.PI * 0.10f;

        SpinyAddWidePad(w, s, padR, innerR, rimH, primary, secondary);
        SpinyAddVoidAccentBand(w, s, padR * 0.97f, rimH * 0.82f, s * 0.022f, 14, accent);
        SpinyAddThornSuperstructure(w, 0, rimH, 0, s * 0.30f, s, primary, secondary, accent, protrusion);

        ReadOnlySpan<float> dishA = stackalloc float[] { 0.30f, 1.05f, 2.20f, 3.40f };
        float spikeReach = s * (0.09f + protrusion * 0.05f);
        for (int d = 0; d < 4; d++)
        {
            float ang = MathF.PI * 0.5f * dishA[d] + yawOffset;
            float dx = MathF.Cos(ang) * padR * 0.74f;
            float dz = MathF.Sin(ang) * padR * 0.74f;
            SpinyAddSpikeCluster(w, dx, rimH + s * 0.01f, dz, ang, spikeReach, s, primary, secondary, accent);
        }

        for (int well = 0; well < 6; well++)
        {
            float ang = MathF.PI * 2f * well / 6f + yawOffset;
            float wx = MathF.Cos(ang) * padR * 0.90f;
            float wz = MathF.Sin(ang) * padR * 0.90f;
            RetroAddBoxUniform(w, wx - s * 0.03f, wx + s * 0.03f, rimH, rimH + s * 0.015f, wz - s * 0.03f, wz + s * 0.03f,
                accent * 0.92f);
        }
    }

    private static void BuildSpinyShipyard(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s, int gantries, int bays)
    {
        bool large = bays >= 5;
        bool medium = bays >= 3 && !large;
        float padR = large ? s * 1.18f : medium ? s * 1.12f : s * 1.08f;
        float innerR = padR * 0.60f;
        float rimH = s * 0.05f;
        float protrusion = race.Modifiers.Protrusion;
        int gantryCount = large ? 5 : medium ? Math.Max(gantries, 3) : 2;
        int podCount = large ? 5 : medium ? 3 : 2;

        SpinyAddWidePad(w, s, padR, innerR, rimH, primary, secondary);
        SpinyAddVoidAccentBand(w, s, padR * 0.97f, rimH * 0.82f, s * 0.022f, 14, accent);

        for (int v = 0; v < gantryCount; v++)
        {
            float side = v % 2 == 0 ? -1f : 1f;
            float zSlot = -padR * 0.38f + v * padR * (large ? 0.16f : medium ? 0.18f : 0.22f);
            float startX = side * padR * 0.68f;
            float reachX = side * padR * (large ? 1.04f : medium ? 0.96f : 0.88f);
            SpinyAddSpikeGantry(w, startX, rimH, zSlot, reachX, s, protrusion, primary, secondary, accent);
        }

        ReadOnlySpan<float> podX = large
            ? stackalloc float[] { -0.28f, 0.10f, 0.32f, -0.08f, 0.22f }
            : medium
                ? stackalloc float[] { -0.22f, 0.14f, -0.04f }
                : stackalloc float[] { -0.18f, 0.16f };
        ReadOnlySpan<float> podZ = large
            ? stackalloc float[] { -0.14f, 0.18f, -0.22f, 0.26f, 0.08f }
            : medium
                ? stackalloc float[] { 0.12f, -0.16f, 0.24f }
                : stackalloc float[] { 0.10f, -0.12f };

        for (int p = 0; p < podCount; p++)
        {
            float px = podX[p] * s;
            float pz = podZ[p] * s;
            float podScale = s * (large ? 0.14f : medium ? 0.12f : 0.11f);
            SpinyAddSpikePodCluster(w, px, rimH, pz, podScale, s, protrusion, primary, secondary, accent);
        }

        if (medium || large)
        {
            SpinyAddSpikeConnectorArm(w, -padR * 0.42f, rimH, padR * 0.52f, -padR * 0.18f, s * 0.68f, s, protrusion, primary, secondary, accent);
            SpinyAddSpikeConnectorArm(w, padR * 0.08f, rimH, padR * 0.58f, padR * 0.32f, s * 0.74f, s, protrusion, primary, secondary, accent);
        }

        if (large)
        {
            SpinyAddSpikeConnectorArm(w, -s * 0.06f, rimH, padR * 0.64f, s * 0.14f, s * 0.82f, s, protrusion, primary, secondary, accent);
            SpinyAddThornSuperstructure(w, 0, rimH, -s * 0.08f, s * 0.18f, s, primary, secondary, accent, protrusion);
        }
    }

    private static void BuildSpinyDefenseTurret(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 0.96f;
        float innerR = s * 0.56f;
        float rimH = s * 0.05f;
        float protrusion = race.Modifiers.Protrusion;

        SpinyAddWidePad(w, s, padR, innerR, rimH, primary, secondary);
        SpinyAddVoidAccentBand(w, s, padR * 0.97f, rimH * 0.82f, s * 0.020f, 12, accent);
        SpinyAddThornSuperstructure(w, 0, rimH, 0, s * 0.22f, s, primary, secondary, accent, protrusion);
        SpinyAddTurretRing(w, 0, rimH + s * 0.02f, 0, s * 0.28f, s, protrusion, primary, secondary, accent);

        for (int well = 0; well < 6; well++)
        {
            float ang = MathF.PI * 2f * well / 6f + MathF.PI * 0.10f;
            float wx = MathF.Cos(ang) * padR * 0.86f;
            float wz = MathF.Sin(ang) * padR * 0.86f;
            RetroAddBoxUniform(w, wx - s * 0.025f, wx + s * 0.025f, rimH, rimH + s * 0.012f, wz - s * 0.025f, wz + s * 0.025f,
                accent * 0.90f);
        }
    }

    private static void BuildSpinyRefinery(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.10f;
        float innerR = s * 0.64f;
        float rimH = s * 0.05f;
        float protrusion = race.Modifiers.Protrusion;
        float yawOffset = MathF.PI * 0.10f;

        SpinyAddWidePad(w, s, padR, innerR, rimH, primary, secondary);
        SpinyAddVoidAccentBand(w, s, padR * 0.97f, rimH * 0.82f, s * 0.022f, 14, accent);

        SpinyAddSpikePodCluster(w, -s * 0.26f, rimH, s * 0.14f, s * 0.12f, s, protrusion, primary, secondary, accent);
        SpinyAddSpikePodCluster(w, s * 0.20f, rimH, -s * 0.12f, s * 0.11f, s, protrusion, primary, secondary, accent);
        SpinyAddSpikePodCluster(w, -s * 0.04f, rimH, -s * 0.24f, s * 0.10f, s, protrusion, primary, secondary, accent);

        SpinyAddThornStack(w, s * 0.22f, rimH, s * 0.30f, s * 0.38f, s, protrusion, primary, secondary, accent);
        SpinyAddThornStack(w, -s * 0.26f, rimH, s * 0.06f, s * 0.34f, s, protrusion, primary, secondary, accent);

        SpinyAddSpikeConnectorArm(w, -padR * 0.38f, rimH, padR * 0.76f, -padR * 0.12f, s * 0.54f, s, protrusion, primary, secondary, accent);
        SpinyAddSpikeConnectorArm(w, padR * 0.32f, rimH, padR * 0.72f, padR * 0.58f, s * 0.50f, s, protrusion, primary, secondary, accent);

        for (int well = 0; well < 8; well++)
        {
            float ang = MathF.PI * 2f * well / 8f + yawOffset;
            float wx = MathF.Cos(ang) * padR * 0.88f;
            float wz = MathF.Sin(ang) * padR * 0.88f;
            RetroAddBoxUniform(w, wx - s * 0.03f, wx + s * 0.03f, rimH, rimH + s * 0.015f, wz - s * 0.03f, wz + s * 0.03f,
                accent * 0.92f);
        }
    }

    private static void BuildSpinyRepairBay(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.10f;
        float innerR = s * 0.66f;
        float rimH = s * 0.05f;
        float protrusion = race.Modifiers.Protrusion;
        float yawOffset = MathF.PI * 0.10f;

        SpinyAddWidePad(w, s, padR, innerR, rimH, primary, secondary);
        SpinyAddVoidAccentBand(w, s, padR * 0.97f, rimH * 0.82f, s * 0.022f, 14, accent);

        SpinyAddBayCutout(w, -s * 0.24f, rimH, -s * 0.06f, s * 0.28f, s * 0.18f, s, secondary);
        SpinyAddBayCutout(w, s * 0.20f, rimH, s * 0.14f, s * 0.24f, s * 0.16f, s, secondary);

        SpinyAddThornSuperstructure(w, -s * 0.08f, rimH, -s * 0.04f, s * 0.16f, s, primary, secondary, accent, protrusion);
        SpinyAddThornSuperstructure(w, s * 0.18f, rimH, s * 0.10f, s * 0.14f, s, primary, secondary, accent, protrusion);

        ReadOnlySpan<float> armX = stackalloc float[] { -0.18f, 0.08f, -0.04f };
        ReadOnlySpan<float> armZ = stackalloc float[] { 0.58f, 0.72f, 0.64f };
        for (int a = 0; a < 3; a++)
        {
            float startZ = padR * 0.68f;
            SpinyAddSpikeConnectorArm(w, armX[a] * s, rimH, startZ, armX[a] * s * 0.35f, armZ[a] * s, s, protrusion, primary, secondary, accent);
        }

        SpinyAddSpikeConnectorArm(w, -padR * 0.44f, rimH, padR * 0.22f, -padR * 0.72f, s * 0.18f, s, protrusion, primary, secondary, accent);
        SpinyAddSpikeConnectorArm(w, padR * 0.36f, rimH, padR * 0.18f, padR * 0.70f, s * 0.22f, s, protrusion, primary, secondary, accent);

        for (int well = 0; well < 8; well++)
        {
            float ang = MathF.PI * 2f * well / 8f + yawOffset;
            float wx = MathF.Cos(ang) * padR * 0.90f;
            float wz = MathF.Sin(ang) * padR * 0.90f;
            RetroAddBoxUniform(w, wx - s * 0.03f, wx + s * 0.03f, rimH, rimH + s * 0.015f, wz - s * 0.03f, wz + s * 0.03f,
                accent * 0.94f);
        }
    }

    private static void BuildSpinyReactor(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        Vector3 engine = race.Palette.Engine?.Length >= 3
            ? ToVector3(race.Palette.Engine)
            : new Vector3(0.45f, 0.05f, 0.75f);
        float padR = s * 1.10f;
        float innerR = s * 0.64f;
        float rimH = s * 0.05f;
        float protrusion = race.Modifiers.Protrusion;
        float yawOffset = MathF.PI * 0.10f;

        SpinyAddWidePad(w, s, padR, innerR, rimH, primary, secondary);
        SpinyAddVoidAccentBand(w, s, padR * 0.97f, rimH * 0.82f, s * 0.022f, 14, accent);

        RetroAddBoxUniform(w, -s * 0.14f, s * 0.14f, rimH, rimH + s * 0.03f, -s * 0.14f, s * 0.14f, secondary * 0.88f);
        SpinyAddVoidEngineGlowWell(w, 0, rimH + s * 0.02f, 0, s, engine);
        SpinyAddSpikeRing(w, 0, rimH + s * 0.01f, 0, s * 0.36f, s, protrusion, primary, secondary, accent, yawOffset);
        SpinyAddThornSuperstructure(w, 0, rimH, 0, s * 0.20f, s, primary, secondary, accent, protrusion);

        for (int well = 0; well < 8; well++)
        {
            float ang = MathF.PI * 2f * well / 8f + yawOffset;
            float wx = MathF.Cos(ang) * padR * 0.88f;
            float wz = MathF.Sin(ang) * padR * 0.88f;
            RetroAddBoxUniform(w, wx - s * 0.03f, wx + s * 0.03f, rimH, rimH + s * 0.015f, wz - s * 0.03f, wz + s * 0.03f,
                accent * 0.96f);
        }
    }

    private static void BuildSpinySupplyDepot(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.06f;
        float innerR = s * 0.62f;
        float rimH = s * 0.05f;
        float protrusion = race.Modifiers.Protrusion;
        float yawOffset = MathF.PI * 0.10f;

        SpinyAddWidePad(w, s, padR, innerR, rimH, primary, secondary);
        SpinyAddVoidAccentBand(w, s, padR * 0.97f, rimH * 0.82f, s * 0.022f, 14, accent);

        SpinyAddThornSuperstructure(w, 0, rimH, 0, s * 0.26f, s, primary, secondary, accent, protrusion);

        RetroAddBoxUniform(w, -s * 0.20f, s * 0.16f, rimH, rimH + s * 0.10f, -s * 0.14f, s * 0.10f, secondary * 0.92f);
        RetroAddBoxUniform(w, -s * 0.16f, s * 0.12f, rimH + s * 0.10f, rimH + s * 0.13f, -s * 0.10f, s * 0.06f, accent * 1.02f);

        SpinyAddSpikeConnectorArm(w, -padR * 0.66f, rimH, -s * 0.08f, -s * 0.88f, s * 0.42f, s, protrusion, primary, secondary, accent);
        SpinyAddSpikeConnectorArm(w, padR * 0.58f, rimH, s * 0.10f, s * 0.82f, s * 0.46f, s, protrusion, primary, secondary, accent);

        ReadOnlySpan<float> crateX = stackalloc float[] { -0.20f, 0.18f, -0.08f, 0.10f };
        ReadOnlySpan<float> crateZ = stackalloc float[] { -0.14f, 0.12f, 0.18f, -0.20f };
        for (int c = 0; c < 4; c++)
        {
            float cx = crateX[c] * s;
            float cz = crateZ[c] * s;
            RetroAddBoxUniform(w, cx - s * 0.05f, cx + s * 0.05f, rimH + s * 0.02f, rimH + s * 0.08f, cz - s * 0.04f, cz + s * 0.04f,
                c % 2 == 0 ? primary * 0.93f : secondary * 0.91f);
        }

        for (int well = 0; well < 6; well++)
        {
            float ang = MathF.PI * 2f * well / 6f + yawOffset;
            float wx = MathF.Cos(ang) * padR * 0.88f;
            float wz = MathF.Sin(ang) * padR * 0.88f;
            RetroAddBoxUniform(w, wx - s * 0.03f, wx + s * 0.03f, rimH, rimH + s * 0.015f, wz - s * 0.03f, wz + s * 0.03f,
                accent * 0.92f);
        }
    }

    private static void BuildCrystallineCommandCenter(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.14f;
        float innerR = s * 0.70f;
        float rimH = s * 0.05f;
        float sharpness = race.Modifiers.FacetSharpness;
        int facetCount = Math.Clamp(6 + (int)(sharpness * 2), 6, 8);
        int hubFacets = Math.Clamp(6 + (int)(sharpness * 1.5f), 6, 8);

        CrystallineAddFacetedPad(w, s, padR, innerR, rimH, facetCount, primary, secondary, accent);
        CrystallineAddPrismaticCore(w, 0, rimH + s * 0.01f, 0, s);

        float hubR = s * 0.24f;
        float hubY = rimH;
        for (int tier = 0; tier < 3; tier++)
        {
            float shrink = 1f - tier * 0.18f;
            float tierH = s * (0.06f + tier * 0.02f);
            float tierTop = hubY + tierH;
            Vector3 tierCol = tier == 0 ? secondary : tier == 1 ? primary * 0.96f : primary;
            RetroAddFacetedColumn(w, 0, 0, hubY, tierTop, hubR * shrink, hubR * shrink * 0.82f, hubFacets, tierCol, tierCol);
            CrystallineAddFrostPanelBand(w, 0, hubY + tierH * 0.72f, 0, hubR * shrink * 0.92f, s * 0.016f, hubFacets, primary, accent);
            hubY = tierTop;
        }

        RetroAddBoxUniform(w, -hubR * 0.35f, hubR * 0.35f, hubY, hubY + s * 0.025f, -hubR * 0.35f, hubR * 0.35f, accent);

        ReadOnlySpan<float> dishA = stackalloc float[] { 0.38f, 1.22f, 2.48f, 3.72f };
        ReadOnlySpan<float> dishR = stackalloc float[] { 0.76f, 0.68f, 0.74f, 0.70f };
        int dishCount = sharpness > 0.5f ? 4 : 3;
        for (int d = 0; d < dishCount; d++)
        {
            float a = MathF.PI * 0.5f * dishA[d];
            float dx = MathF.Cos(a) * padR * dishR[d];
            float dz = MathF.Sin(a) * padR * dishR[d];
            float reach = s * 0.10f;
            RetroAddBoxUniform(w, dx - s * 0.04f, dx + s * 0.04f, rimH + s * 0.01f, rimH + s * 0.05f, dz - s * 0.04f, dz + s * 0.04f, secondary);
            RetroAddBoxUniform(w, dx - reach * 0.5f, dx + reach * 0.35f, rimH + s * 0.05f, rimH + s * 0.08f,
                dz - reach * 0.3f, dz + reach * 0.12f, primary * 0.94f);
            RetroAddBoxUniform(w, dx - s * 0.02f, dx + s * 0.02f, rimH + s * 0.08f, rimH + s * 0.11f,
                dz + reach * 0.08f, dz + reach * 0.12f, accent * 0.96f);
        }
    }

    private static void BuildCrystallineShipyard(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s, int gantries, int bays)
    {
        bool isLarge = bays >= 5;
        bool isSmall = bays <= 2 && gantries <= 2;
        float padR = s * (isLarge ? 1.22f : bays >= 3 ? 1.14f : 1.10f);
        float innerR = padR * 0.62f;
        float rimH = s * 0.05f;
        float sharpness = race.Modifiers.FacetSharpness;
        int facetCount = Math.Clamp(6 + (int)(sharpness * 2), 6, 8);

        CrystallineAddFacetedPad(w, s, padR, innerR, rimH, facetCount, primary, secondary, accent);
        CrystallineAddPadFrostBand(w, padR, rimH, s * 0.018f, facetCount + 2, primary, accent);
        CrystallineAddPrismaticCore(w, 0, rimH + s * 0.01f, 0, s);

        int gantryCount = isLarge ? 5 : isSmall ? 2 : Math.Clamp(gantries, 3, 4);
        ReadOnlySpan<float> gantryA = isLarge
            ? stackalloc float[] { 0.22f, 0.62f, 1.02f, 1.42f, 1.82f }
            : isSmall
                ? stackalloc float[] { 0.50f, 1.50f }
                : stackalloc float[] { 0.30f, 0.80f, 1.30f, 1.80f };
        for (int g = 0; g < gantryCount; g++)
        {
            float ang = MathF.PI * gantryA[g];
            float cx = MathF.Cos(ang) * padR * 0.82f;
            float cz = MathF.Sin(ang) * padR * 0.82f;
            float reach = s * (isLarge ? 0.22f : isSmall ? 0.16f : 0.18f);
            CrystallineAddCrystalGantry(w, cx, rimH + s * 0.01f, cz, ang, reach, s, facetCount, primary, secondary, accent);
        }

        int bayCount = Math.Min(bays, isLarge ? 5 : isSmall ? 2 : 4);
        for (int b = 0; b < bayCount; b++)
        {
            float bz = padR * (-0.20f + b * (isLarge ? 0.20f : 0.24f));
            float bx = padR * (0.08f + (b % 2) * 0.14f);
            float bW = padR * (isLarge ? 0.20f : 0.16f);
            RetroAddBoxUniform(w, bx - bW, bx + bW * 0.8f, rimH, rimH + s * 0.10f,
                bz - padR * 0.10f, bz + padR * 0.10f, b % 2 == 0 ? primary * 0.93f : secondary * 0.91f);
            RetroAddBoxUniform(w, bx - bW * 0.5f, bx + bW * 0.4f, rimH + s * 0.10f, rimH + s * 0.13f,
                bz - padR * 0.08f, bz + padR * 0.08f, accent * 0.98f);
            CrystallineAddCrystalFacet(w, bx, bz, rimH + s * 0.08f, bW * 0.35f,
                b % 2 == 0 ? secondary * 0.94f : primary * 0.92f, accent);
        }

        if (isLarge)
        {
            for (int cluster = 0; cluster < 3; cluster++)
            {
                float cx = (cluster - 1) * s * 0.18f;
                float cz = s * 0.06f + cluster * s * 0.05f;
                RetroAddBoxUniform(w, cx - s * 0.08f, cx + s * 0.08f, rimH + s * 0.02f, rimH + s * 0.08f,
                    cz - s * 0.06f, cz + s * 0.06f, cluster % 2 == 0 ? primary : secondary);
                RetroAddBoxUniform(w, cx - s * 0.06f, cx + s * 0.06f, rimH + s * 0.08f, rimH + s * 0.10f,
                    cz - s * 0.05f, cz + s * 0.05f, accent);
            }
        }
    }

    private static void BuildCrystallineDefenseTurret(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 0.98f;
        float innerR = s * 0.54f;
        float rimH = s * 0.05f;
        float raisedH = s * 0.04f;
        float sharpness = race.Modifiers.FacetSharpness;
        int facetCount = Math.Clamp(6 + (int)(sharpness * 2), 6, 8);

        CrystallineAddFacetedPad(w, s, padR, innerR, rimH, facetCount, primary, secondary, accent);
        CrystallineAddPadFrostBand(w, padR, rimH, s * 0.016f, facetCount, primary, accent);

        RetroAddBoxUniform(w, -innerR * 0.85f, innerR * 0.85f, rimH, rimH + raisedH, -innerR * 0.85f, innerR * 0.85f, secondary);
        CrystallineAddFrostPanelBand(w, 0, rimH + raisedH * 0.70f, 0, innerR * 0.82f, s * 0.014f, facetCount, primary, accent);

        float ringY = rimH + raisedH + s * 0.02f;
        float ringR = s * 0.18f;
        for (int i = 0; i < facetCount; i++)
        {
            float a = MathF.PI * 2f * i / facetCount;
            float cx = MathF.Cos(a) * ringR;
            float cz = MathF.Sin(a) * ringR;
            CrystallineAddCrystalFacet(w, cx, cz, ringY, s * 0.035f,
                i % 2 == 0 ? secondary * 0.94f : primary * 0.92f, accent * 0.98f);
        }

        RetroAddFacetedColumn(w, 0, 0, ringY + s * 0.04f, ringY + s * 0.10f, s * 0.10f, s * 0.07f, facetCount, secondary, primary);
        CrystallineAddFrostPanelBand(w, 0, ringY + s * 0.07f, 0, s * 0.09f, s * 0.012f, facetCount, primary, accent);

        float barrelBase = ringY + s * 0.10f;
        RetroAddBoxUniform(w, -s * 0.05f, s * 0.05f, barrelBase, barrelBase + s * 0.04f, s * 0.02f, s * 0.14f, primary);
        RetroAddBoxUniform(w, -s * 0.035f, s * 0.035f, barrelBase + s * 0.02f, barrelBase + s * 0.06f, s * 0.12f, s * 0.26f, accent);
        RetroAddBoxUniform(w, -s * 0.025f, s * 0.025f, barrelBase + s * 0.04f, barrelBase + s * 0.08f, s * 0.24f, s * 0.30f, accent * 1.10f);
    }

    private static void BuildCrystallineSensorArray(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.12f;
        float innerR = s * 0.66f;
        float rimH = s * 0.05f;
        float sharpness = race.Modifiers.FacetSharpness;
        int facetCount = Math.Clamp(6 + (int)(sharpness * 2), 6, 8);
        int dishCount = sharpness > 0.5f ? 4 : 3;

        CrystallineAddFacetedPad(w, s, padR, innerR, rimH, facetCount, primary, secondary, accent);
        CrystallineAddPadFrostBand(w, padR, rimH, s * 0.018f, facetCount + 2, primary, accent);
        CrystallineAddPrismaticCore(w, 0, rimH + s * 0.01f, 0, s);

        ReadOnlySpan<float> dishA = stackalloc float[] { 0.42f, 1.18f, 2.08f, 2.92f };
        ReadOnlySpan<float> dishR = stackalloc float[] { 0.72f, 0.66f, 0.74f, 0.68f };
        for (int d = 0; d < dishCount; d++)
        {
            float a = MathF.PI * 0.5f * dishA[d];
            float dx = MathF.Cos(a) * padR * dishR[d];
            float dz = MathF.Sin(a) * padR * dishR[d];
            CrystallineAddFacetDishLow(w, dx, rimH + s * 0.01f, dz, s * 0.10f, s, primary, secondary, accent);
        }

        for (int ring = 0; ring < 3; ring++)
        {
            float radius = s * (0.28f + ring * 0.10f);
            CrystallineAddFrostPanelBand(w, 0, rimH + s * 0.02f + ring * s * 0.004f, 0, radius, s * 0.012f,
                facetCount, primary, accent);
        }

        RetroAddBoxUniform(w, -s * 0.06f, s * 0.06f, rimH + s * 0.04f, rimH + s * 0.07f, -s * 0.06f, s * 0.06f, accent * 0.96f);
    }

    private static void BuildCrystallineRefinery(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.14f;
        float innerR = s * 0.66f;
        float rimH = s * 0.05f;
        float sharpness = race.Modifiers.FacetSharpness;
        int facetCount = Math.Clamp(6 + (int)(sharpness * 2), 6, 8);

        CrystallineAddFacetedPad(w, s, padR, innerR, rimH, facetCount, primary, secondary, accent);
        CrystallineAddPadFrostBand(w, padR, rimH, s * 0.018f, facetCount + 2, primary, accent);

        ReadOnlySpan<float> procX = stackalloc float[] { -0.22f, 0.16f, 0.02f };
        ReadOnlySpan<float> procZ = stackalloc float[] { 0.14f, -0.10f, 0.26f };
        for (int p = 0; p < 3; p++)
        {
            float px = procX[p] * s;
            float pz = procZ[p] * s;
            float halfW = s * 0.10f;
            RetroAddBoxUniform(w, px - halfW, px + halfW, rimH, rimH + s * 0.12f, pz - halfW * 0.75f, pz + halfW * 0.75f,
                p % 2 == 0 ? primary * 0.92f : secondary * 0.90f);
            CrystallineAddCrystalFacet(w, px, pz, rimH + s * 0.10f, halfW * 0.55f,
                secondary * 0.94f, accent * (0.98f + p * 0.02f));
        }

        for (int stack = 0; stack < 2; stack++)
        {
            float side = stack == 0 ? -1f : 1f;
            float cx = side * s * 0.32f;
            float peak = s * (stack == 0 ? 0.20f : 0.18f);
            RetroAddFacetedColumn(w, cx, s * 0.04f, rimH + s * 0.02f, rimH + peak, s * 0.09f, s * 0.05f, facetCount, secondary, accent);
            CrystallineAddFrostPanelBand(w, cx, rimH + peak * 0.65f, s * 0.04f, s * 0.07f, s * 0.012f, facetCount, primary, accent);
            RetroAddBoxUniform(w, cx - s * 0.04f, cx + s * 0.04f, rimH + peak, rimH + peak + s * 0.025f,
                s * 0.02f, s * 0.10f, accent * (1.06f + stack * 0.04f));
        }

        RetroAddBoxUniform(w, -s * 0.28f, s * 0.28f, rimH + s * 0.02f, rimH + s * 0.05f, s * 0.06f, s * 0.20f, secondary * 0.90f);
    }

    private static void BuildCrystallineRepairBay(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.12f;
        float innerR = s * 0.66f;
        float rimH = s * 0.05f;
        float sharpness = race.Modifiers.FacetSharpness;
        int facetCount = Math.Clamp(6 + (int)(sharpness * 2), 6, 8);

        CrystallineAddFacetedPad(w, s, padR, innerR, rimH, facetCount, primary, secondary, accent);
        CrystallineAddPadFrostBand(w, padR, rimH, s * 0.018f, facetCount + 2, primary, accent);

        for (int bay = 0; bay < 2; bay++)
        {
            float bz = padR * (bay == 0 ? -0.32f : 0.26f);
            float bx = padR * (bay == 0 ? 0.06f : 0.20f);
            float slotW = padR * 0.32f;
            RetroAddBoxUniform(w, bx - slotW, bx + slotW * 0.7f, rimH, rimH + s * 0.03f,
                bz - padR * 0.12f, bz + padR * 0.12f, secondary * 0.82f);
            RetroAddBoxUniform(w, bx - slotW * 0.75f, bx + slotW * 0.55f, rimH + s * 0.03f, rimH + s * 0.08f,
                bz - padR * 0.10f, bz + padR * 0.10f, primary * 0.90f);
            CrystallineAddFrostPanelBand(w, bx, rimH + s * 0.05f, bz, slotW * 0.55f, s * 0.010f, facetCount, primary, accent);
        }

        ReadOnlySpan<float> armReach = stackalloc float[] { 0.72f, 0.58f };
        ReadOnlySpan<float> armZ = stackalloc float[] { 0.28f, 0.16f };
        for (int side = -1; side <= 1; side += 2)
        {
            int idx = side < 0 ? 0 : 1;
            float ax = side * padR * 0.44f;
            float reach = armReach[idx] * s;
            float az = armZ[idx] * s;
            RetroAddBoxUniform(w, ax, reach, rimH, rimH + s * 0.16f, az - s * 0.05f, az + s * 0.12f,
                side < 0 ? secondary * 0.92f : primary * 0.90f);
            RetroAddBoxUniform(w, reach - s * 0.04f, reach + s * 0.06f, rimH + s * 0.16f, rimH + s * 0.22f,
                az + s * 0.06f, az + s * 0.18f, accent * (side < 0 ? 1.08f : 1.04f));
            CrystallineAddCrystalFacet(w, reach, az + s * 0.12f, rimH + s * 0.18f, s * 0.04f,
                primary * 0.92f, accent);
        }

        RetroAddBoxUniform(w, -s * 0.08f, s * 0.08f, rimH + s * 0.02f, rimH + s * 0.06f, padR * 0.12f, padR * 0.28f, accent * 1.04f);
    }

    private static void BuildCrystallineReactor(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.14f;
        float innerR = s * 0.66f;
        float rimH = s * 0.05f;
        float sharpness = race.Modifiers.FacetSharpness;
        int facetCount = Math.Clamp(6 + (int)(sharpness * 2), 6, 8);

        CrystallineAddFacetedPad(w, s, padR, innerR, rimH, facetCount, primary, secondary, accent);
        CrystallineAddPadFrostBand(w, padR, rimH, s * 0.020f, facetCount + 2, primary, accent);

        ReadOnlySpan<float> ringR = stackalloc float[] { 0.40f, 0.30f, 0.20f };
        for (int ring = 0; ring < 3; ring++)
        {
            float radius = s * ringR[ring];
            CrystallineAddFrostPanelBand(w, 0, rimH + s * 0.012f + ring * s * 0.005f, 0, radius, s * 0.014f,
                facetCount + ring, primary, accent);
        }

        CrystallineAddReactorCoreWell(w, 0, rimH + s * 0.01f, 0, s, facetCount, primary, secondary, accent);

        for (int c = 0; c < 4; c++)
        {
            float angle = MathF.PI * 0.25f + MathF.PI * 0.5f * c;
            float cx = MathF.Cos(angle) * s * 0.44f;
            float cz = MathF.Sin(angle) * s * 0.44f;
            CrystallineAddCrystalFacet(w, cx, cz, rimH + s * 0.01f, s * 0.045f,
                c % 2 == 0 ? primary * 0.92f : secondary * 0.90f, accent);
        }
    }

    private static void BuildCrystallineSupplyDepot(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.08f;
        float innerR = s * 0.60f;
        float rimH = s * 0.05f;
        float sharpness = race.Modifiers.FacetSharpness;
        int facetCount = Math.Clamp(6 + (int)(sharpness * 2), 6, 8);

        CrystallineAddFacetedPad(w, s, padR, innerR, rimH, facetCount, primary, secondary, accent);
        CrystallineAddPadFrostBand(w, padR, rimH, s * 0.016f, facetCount, primary, accent);

        RetroAddBoxUniform(w, -s * 0.14f, s * 0.14f, rimH, rimH + s * 0.12f, -s * 0.10f, s * 0.10f, secondary);
        RetroAddBoxUniform(w, -s * 0.12f, s * 0.12f, rimH + s * 0.12f, rimH + s * 0.14f, -s * 0.08f, s * 0.08f, accent);
        CrystallineAddFrostPanelBand(w, 0, rimH + s * 0.06f, 0, s * 0.10f, s * 0.012f, facetCount, primary, accent);
        CrystallineAddPrismaticCore(w, 0, rimH + s * 0.02f, 0, s * 0.85f);

        for (int ext = 0; ext < 2; ext++)
        {
            float side = ext == 0 ? -1f : 1f;
            float cx = side * padR * 0.72f;
            float halfLen = padR * 0.22f;
            RetroAddBoxUniform(w, cx - halfLen, cx + halfLen, rimH, rimH + s * 0.04f, -s * 0.09f, s * 0.09f, primary);
            for (int cargo = 0; cargo < 2; cargo++)
            {
                float z = (cargo == 0 ? -1f : 1f) * s * 0.05f;
                float h = s * (cargo == 0 ? 0.10f : 0.12f);
                float halfW = s * 0.07f;
                RetroAddBoxUniform(w, cx - halfW, cx + halfW, rimH + s * 0.04f, rimH + s * 0.04f + h,
                    z - halfW * 0.75f, z + halfW * 0.75f, secondary);
                CrystallineAddCrystalFacet(w, cx, z, rimH + s * 0.04f + h, halfW * 0.55f,
                    primary * 0.92f, accent);
            }
        }
    }

    private static void BuildOrganicRefinery(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.12f;
        float innerR = s * 0.66f;
        float rimH = s * 0.05f;

        OrganicAddBioDomePad(w, s, padR, innerR, rimH, primary, secondary, accent);

        OrganicAddPodCluster(w, -s * 0.28f, rimH, s * 0.16f, s * 0.13f, s, primary, secondary, accent);
        OrganicAddPodCluster(w, s * 0.22f, rimH, -s * 0.14f, s * 0.12f, s, primary, secondary, accent);
        OrganicAddPodCluster(w, -s * 0.04f, rimH, -s * 0.26f, s * 0.11f, s, primary, secondary, accent);

        OrganicAddVineStack(w, s * 0.18f, rimH, s * 0.28f, s * 0.12f, s, primary, secondary, accent);
        OrganicAddVineStack(w, -s * 0.24f, rimH, s * 0.08f, s * 0.11f, s, primary, secondary, accent);

        OrganicAddVineConnectorArm(w, -s * 0.40f, rimH, padR * 0.78f, s * 0.52f, s, primary, secondary, accent);
        OrganicAddVineConnectorArm(w, s * 0.36f, rimH, padR * 0.74f, s * 0.48f, s, primary, secondary, accent);
    }

    private static void BuildOrganicReactor(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.10f;
        float innerR = s * 0.64f;
        float rimH = s * 0.05f;

        OrganicAddBioDomePad(w, s, padR, innerR, rimH, primary, secondary, accent);
        OrganicAddDomeHub(w, 0, rimH, 0, s * 0.20f, s * 0.11f, s, primary, secondary, accent);
        OrganicAddVineRing(w, 0, rimH + s * 0.01f, 0, s * 0.38f, s, primary, secondary, accent, segments: 8);
        OrganicAddEngineGlowWell(w, 0, rimH + s * 0.02f, 0, s);
    }

    private static void BuildOrganicSupplyDepot(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.06f;
        float innerR = s * 0.62f;
        float rimH = s * 0.05f;

        OrganicAddBioDomePad(w, s, padR, innerR, rimH, primary, secondary, accent);
        OrganicAddDomeHub(w, 0, rimH, 0, s * 0.24f, s * 0.13f, s, primary, secondary, accent);

        OrganicAddVineConnectorArm(w, -padR * 0.68f, rimH, -s * 0.08f, s * 0.42f, s, primary, secondary, accent);
        OrganicAddVineConnectorArm(w, padR * 0.62f, rimH, s * 0.10f, s * 0.46f, s, primary, secondary, accent);
    }

    private static void BuildAsymmetricCommandCenter(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float hubOffset = s * race.Modifiers.Asymmetry * 0.22f;
        float padR = s * 1.14f;
        float rimH = s * 0.05f;
        float hubScale = s * 0.36f;

        AsymmetricAddStaggeredPad(w, s, padR, rimH, hubOffset, primary, secondary);
        AsymmetricAddOffsetHub(w, hubOffset, rimH, hubScale, primary, secondary, accent, tiers: 3);
        AsymmetricAddDiagonalAccentStripe(w, hubOffset, rimH, hubScale, accent);

        ReadOnlySpan<float> dishX = stackalloc float[] { 0.10f, 0.28f, 0.06f, 0.22f };
        ReadOnlySpan<float> dishZ = stackalloc float[] { -0.18f, 0.12f, 0.28f, -0.32f };
        for (int d = 0; d < 4; d++)
        {
            float dx = hubOffset + dishX[d] * s;
            float dz = dishZ[d] * s;
            RetroAddCommsDishLow(w, dx, rimH + s * 0.02f, dz, s * 0.13f, s, primary, secondary, accent);
        }

        RetroAddBoxUniform(w, hubOffset + s * 0.52f, hubOffset + s * 0.78f, rimH, rimH + s * 0.10f,
            -s * 0.06f, s * 0.08f, secondary * 0.94f);
        RetroAddBoxUniform(w, hubOffset - s * 0.12f, hubOffset + s * 0.18f, rimH + s * 0.06f, rimH + s * 0.09f,
            -s * 0.48f, -s * 0.28f, accent * 1.02f);
        AsymmetricAddAmberAccentWell(w, hubOffset, rimH, hubScale, accent);
    }

    private static void BuildAsymmetricShipyard(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s, int gantries, int bays)
    {
        float hubOffset = s * race.Modifiers.Asymmetry * 0.22f;
        bool isLarge = bays >= 5;
        bool isMedium = bays >= 3 && bays < 5;
        float padR = isLarge ? s * 1.22f : isMedium ? s * 1.14f : s * 1.10f;
        float rimH = s * 0.05f;
        float hubScale = isLarge ? s * 0.40f : isMedium ? s * 0.36f : s * 0.32f;

        AsymmetricAddStaggeredPad(w, s, padR, rimH, hubOffset, primary, secondary);
        AsymmetricAddOffsetHub(w, hubOffset, rimH, hubScale, primary, secondary, accent, tiers: isLarge ? 3 : 2);
        AsymmetricAddDiagonalAccentStripe(w, hubOffset, rimH, hubScale, accent);
        AsymmetricAddAmberAccentWell(w, hubOffset, rimH, hubScale, accent);

        int gantryBlocks = isLarge ? 3 : 2;
        ReadOnlySpan<float> gantryZ = isLarge
            ? stackalloc float[] { 0.42f, -0.38f, 0.12f }
            : stackalloc float[] { 0.36f, -0.44f };
        for (int g = 0; g < gantryBlocks; g++)
        {
            float gx = hubOffset + padR * (0.38f + g * 0.08f);
            float gz = gantryZ[g] * padR;
            float gW = padR * (isLarge ? 0.16f : 0.14f);
            float gLen = padR * (isLarge ? 0.32f : 0.26f);
            RetroAddBoxUniform(w, gx - gLen * 0.5f, gx + gLen * 0.5f, rimH, rimH + s * 0.22f,
                gz - gW, gz + gW, g % 2 == 0 ? secondary * 0.94f : primary * 0.92f);
            RetroAddBoxUniform(w, gx - gLen * 0.3f, gx + gLen * 0.3f, rimH + s * 0.22f, rimH + s * 0.28f,
                gz - gW * 0.7f, gz + gW * 0.7f, accent * 0.98f);
        }

        int bayCount = Math.Min(bays, isLarge ? 5 : isMedium ? 4 : 2);
        for (int b = 0; b < bayCount; b++)
        {
            float bz = padR * (-0.18f + b * (isLarge ? 0.22f : 0.28f));
            float bx = hubOffset + padR * (0.12f + (b % 2) * 0.18f);
            float bW = padR * 0.22f;
            RetroAddBoxUniform(w, bx - bW, bx + bW * 0.8f, rimH, rimH + s * 0.12f,
                bz - padR * 0.12f, bz + padR * 0.12f, b % 2 == 0 ? primary * 0.93f : secondary * 0.91f);
            RetroAddBoxUniform(w, bx - bW * 0.6f, bx + bW * 0.5f, rimH, rimH + s * 0.04f,
                bz - padR * 0.14f, bz + padR * 0.14f, accent * 0.96f);
        }

        for (int g = 0; g < gantries; g++)
        {
            float dockX = hubOffset + (g - (gantries - 1) * 0.5f) * padR * 0.28f;
            float dockZ = padR * (0.52f + (g % 2) * 0.14f);
            RetroAddBoxUniform(w, dockX - padR * 0.10f, dockX + padR * 0.10f, rimH, rimH + s * 0.18f,
                dockZ - padR * 0.08f, dockZ + padR * 0.22f, g % 2 == 0 ? accent * 0.94f : secondary * 0.90f);
        }
    }

    private static void BuildAsymmetricDefenseTurret(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float hubOffset = s * race.Modifiers.Asymmetry * 0.22f;
        float padR = s * 0.94f;
        float rimH = s * 0.05f;
        float hubScale = s * 0.30f;

        AsymmetricAddStaggeredPad(w, s, padR, rimH, hubOffset, primary, secondary);
        AsymmetricAddOffsetHub(w, hubOffset, rimH, hubScale, primary, secondary, accent, tiers: 2);
        AsymmetricAddDiagonalAccentStripe(w, hubOffset, rimH, hubScale, accent);
        AsymmetricAddAmberAccentWell(w, hubOffset, rimH, hubScale, accent);

        RetroAddBoxUniform(w, hubOffset - hubScale * 0.12f, hubOffset + hubScale * 0.18f,
            rimH, rimH + s * 0.06f, -hubScale * 0.14f, hubScale * 0.12f, secondary * 0.92f);

        float barrelBase = rimH + hubScale * 0.18f;
        RetroAddBoxUniform(w, hubOffset - s * 0.06f, hubOffset + s * 0.06f, barrelBase, barrelBase + s * 0.08f,
            hubScale * 0.08f, hubScale * 0.28f, primary * 0.94f);
        RetroAddBoxUniform(w, hubOffset - s * 0.04f, hubOffset + s * 0.04f, barrelBase + s * 0.08f, barrelBase + s * 0.14f,
            hubScale * 0.24f, hubScale * 0.48f, accent);
        RetroAddBoxUniform(w, hubOffset - s * 0.03f, hubOffset + s * 0.03f, barrelBase + s * 0.14f, barrelBase + s * 0.18f,
            hubScale * 0.44f, hubScale * 0.58f, accent * 1.12f);

        RetroAddBoxUniform(w, hubOffset - padR * 0.42f, hubOffset - padR * 0.22f, rimH, rimH + s * 0.16f,
            padR * 0.28f, padR * 0.48f, secondary * 0.90f);
        RetroAddBoxUniform(w, hubOffset + padR * 0.18f, hubOffset + padR * 0.38f, rimH, rimH + s * 0.12f,
            -padR * 0.52f, -padR * 0.32f, primary * 0.91f);
    }

    private static void BuildAsymmetricSensorArray(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float hubOffset = s * race.Modifiers.Asymmetry * 0.22f;
        float padR = s * 1.10f;
        float rimH = s * 0.05f;
        float hubScale = s * 0.32f;

        AsymmetricAddStaggeredPad(w, s, padR, rimH, hubOffset, primary, secondary);
        AsymmetricAddOffsetHub(w, hubOffset, rimH, hubScale, primary, secondary, accent, tiers: 2);
        AsymmetricAddDiagonalAccentStripe(w, hubOffset, rimH, hubScale, accent);
        AsymmetricAddAmberAccentWell(w, hubOffset, rimH, hubScale, accent);

        ReadOnlySpan<float> dishX = stackalloc float[] { 0.08f, 0.32f, 0.04f, 0.24f };
        ReadOnlySpan<float> dishZ = stackalloc float[] { -0.22f, 0.14f, 0.32f, -0.36f };
        for (int d = 0; d < 4; d++)
        {
            float dx = hubOffset + dishX[d] * s;
            float dz = dishZ[d] * s;
            RetroAddCommsDishLow(w, dx, rimH + s * 0.02f, dz, s * 0.12f, s, primary, secondary, accent);
        }

        RetroAddFacetedColumn(w, hubOffset, hubOffset * 0.02f, rimH + hubScale * 0.20f, rimH + hubScale * 0.26f,
            hubScale * 0.08f, hubScale * 0.04f, 6, secondary, accent);
        RetroAddBoxUniform(w, hubOffset - s * 0.04f, hubOffset + s * 0.04f, rimH + hubScale * 0.24f, rimH + hubScale * 0.26f,
            -s * 0.02f, s * 0.06f, accent * 1.08f);
    }

    private static void BuildAsymmetricRefinery(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float hubOffset = s * race.Modifiers.Asymmetry * 0.22f;
        float padR = s * 1.14f;
        float rimH = s * 0.05f;
        float hubScale = s * 0.34f;

        AsymmetricAddStaggeredPad(w, s, padR, rimH, hubOffset, primary, secondary);
        AsymmetricAddOffsetHub(w, hubOffset, rimH, hubScale, primary, secondary, accent, tiers: 2);
        AsymmetricAddDiagonalAccentStripe(w, hubOffset, rimH, hubScale, accent);
        AsymmetricAddAmberAccentWell(w, hubOffset, rimH, hubScale, accent);

        ReadOnlySpan<float> procX = stackalloc float[] { -0.28f, 0.18f, -0.04f };
        ReadOnlySpan<float> procZ = stackalloc float[] { 0.20f, -0.14f, 0.34f };
        ReadOnlySpan<float> procH = stackalloc float[] { 0.38f, 0.44f, 0.32f };
        for (int p = 0; p < 3; p++)
        {
            float px = hubOffset + procX[p] * s;
            float pz = procZ[p] * s;
            float ph = procH[p] * s;
            float halfW = s * (0.14f + p * 0.02f);
            RetroAddBoxUniform(w, px - halfW, px + halfW, rimH, rimH + ph,
                pz - halfW * 0.8f, pz + halfW * 0.8f, p % 2 == 0 ? primary * 0.92f : secondary * 0.90f);
            RetroAddBoxUniform(w, px - halfW * 0.6f, px + halfW * 0.6f, rimH + ph, rimH + ph + s * 0.04f,
                pz - halfW * 0.5f, pz + halfW * 0.5f, accent * (0.98f + p * 0.04f));
        }

        ReadOnlySpan<float> stackX = stackalloc float[] { 0.22f, -0.18f };
        ReadOnlySpan<float> stackH = stackalloc float[] { 0.52f, 0.42f };
        for (int c = 0; c < 2; c++)
        {
            float cx = hubOffset + stackX[c] * s;
            float peak = stackH[c] * s;
            RetroAddFacetedColumn(w, cx, s * 0.04f, rimH + hubScale * 0.12f, rimH + peak,
                s * (0.10f - c * 0.02f), s * (0.05f - c * 0.01f), 6, secondary, accent);
            RetroAddBoxUniform(w, cx - s * 0.04f, cx + s * 0.04f, rimH + peak, rimH + peak + s * 0.03f,
                s * 0.02f, s * 0.10f, accent * (1.08f + c * 0.04f));
        }

        RetroAddBoxUniform(w, hubOffset - s * 0.22f, hubOffset + s * 0.14f, rimH, rimH + s * 0.08f,
            -padR * 0.48f, -padR * 0.28f, secondary * 0.88f);
    }

    private static void BuildAsymmetricRepairBay(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float hubOffset = s * race.Modifiers.Asymmetry * 0.22f;
        float padR = s * 1.12f;
        float rimH = s * 0.05f;
        float hubScale = s * 0.34f;

        AsymmetricAddStaggeredPad(w, s, padR, rimH, hubOffset, primary, secondary);
        AsymmetricAddOffsetHub(w, hubOffset, rimH, hubScale, primary, secondary, accent, tiers: 2);
        AsymmetricAddDiagonalAccentStripe(w, hubOffset, rimH, hubScale, accent);
        AsymmetricAddAmberAccentWell(w, hubOffset, rimH, hubScale, accent);

        for (int bay = 0; bay < 2; bay++)
        {
            float bz = padR * (bay == 0 ? -0.34f : 0.28f);
            float bx = hubOffset + padR * (bay == 0 ? 0.08f : 0.24f);
            float slotW = padR * 0.36f;
            RetroAddBoxUniform(w, bx - slotW, bx + slotW * 0.7f, rimH, rimH + s * 0.04f,
                bz - padR * 0.14f, bz + padR * 0.14f, secondary * 0.82f);
            RetroAddBoxUniform(w, bx - slotW * 0.8f, bx + slotW * 0.6f, rimH + s * 0.04f, rimH + s * 0.10f,
                bz - padR * 0.10f, bz + padR * 0.10f, primary * 0.90f);
        }

        ReadOnlySpan<float> armReach = stackalloc float[] { 0.78f, 0.62f };
        ReadOnlySpan<float> armZ = stackalloc float[] { 0.30f, 0.18f };
        for (int side = -1; side <= 1; side += 2)
        {
            int idx = side < 0 ? 0 : 1;
            float ax = hubOffset + side * padR * 0.48f;
            float reach = armReach[idx] * s;
            float az = armZ[idx] * s;
            RetroAddBoxUniform(w, ax, reach, rimH, rimH + s * 0.22f,
                az - s * 0.06f, az + s * 0.14f, side < 0 ? secondary * 0.92f : primary * 0.90f);
            RetroAddBoxUniform(w, reach - s * 0.04f, reach + s * 0.08f, rimH + s * 0.22f, rimH + s * 0.30f,
                az + s * 0.08f, az + s * 0.22f, accent * (side < 0 ? 1.10f : 1.04f));
        }

        RetroAddBoxUniform(w, hubOffset - s * 0.10f, hubOffset + s * 0.10f, rimH + hubScale * 0.18f, rimH + hubScale * 0.22f,
            padR * 0.14f, padR * 0.32f, accent * 1.06f);
    }

    private static void BuildAsymmetricReactor(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        Vector3 engine = race.Palette.Engine?.Length >= 3
            ? ToVector3(race.Palette.Engine)
            : accent;
        float hubOffset = s * race.Modifiers.Asymmetry * 0.22f;
        float padR = s * 1.10f;
        float rimH = s * 0.05f;
        float hubScale = s * 0.34f;

        AsymmetricAddStaggeredPad(w, s, padR, rimH, hubOffset, primary, secondary);
        AsymmetricAddOffsetHub(w, hubOffset, rimH, hubScale, primary, secondary, accent, tiers: 2);
        AsymmetricAddDiagonalAccentStripe(w, hubOffset, rimH, hubScale, accent);
        AsymmetricAddAmberAccentWell(w, hubOffset, rimH, hubScale, accent);

        RetroAddBoxUniform(w, hubOffset - hubScale * 0.18f, hubOffset + hubScale * 0.14f,
            rimH, rimH + s * 0.04f, -hubScale * 0.16f, hubScale * 0.14f, secondary * 0.86f);

        float domeBase = rimH + s * 0.04f;
        float domeH = s * 0.10f;
        RetroAddFacetedColumn(w, hubOffset, hubOffset * 0.04f, domeBase, domeBase + domeH * 0.70f,
            hubScale * 0.12f, hubScale * 0.08f, 8, secondary, primary);
        RetroAddFacetedColumn(w, hubOffset, hubOffset * 0.04f, domeBase + domeH * 0.70f, domeBase + domeH,
            hubScale * 0.08f, hubScale * 0.02f, 6, primary, accent);
        RetroAddBoxUniform(w, hubOffset - s * 0.05f, hubOffset + s * 0.05f, domeBase + s * 0.02f, domeBase + s * 0.05f,
            -s * 0.05f, s * 0.05f, engine);
        RetroAddBoxUniform(w, hubOffset - s * 0.04f, hubOffset + s * 0.04f, domeBase + s * 0.04f, domeBase + s * 0.07f,
            -s * 0.04f, s * 0.04f, engine * 1.14f);

        ReadOnlySpan<float> ringR = stackalloc float[] { 0.40f, 0.30f, 0.20f };
        for (int ring = 0; ring < 3; ring++)
        {
            float radius = s * ringR[ring];
            float ringY = rimH + s * 0.012f + ring * s * 0.006f;
            RetroAddCoolingRingFlush(w, hubOffset, ringY, padR * 0.08f, radius, s * 0.024f, 12,
                ring % 2 == 0 ? accent : primary * 0.96f);
        }

        RetroAddBoxUniform(w, hubOffset - s * 0.12f, hubOffset + s * 0.12f, domeBase + domeH, domeBase + domeH + s * 0.016f,
            -s * 0.10f, s * 0.10f, accent * 1.12f);
    }

    private static void BuildAsymmetricSupplyDepot(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float hubOffset = s * race.Modifiers.Asymmetry * 0.22f;
        float padR = s * 1.08f;
        float rimH = s * 0.05f;
        float hubScale = s * 0.32f;

        AsymmetricAddStaggeredPad(w, s, padR, rimH, hubOffset, primary, secondary);
        AsymmetricAddOffsetHub(w, hubOffset, rimH, hubScale, primary, secondary, accent, tiers: 2);
        AsymmetricAddDiagonalAccentStripe(w, hubOffset, rimH, hubScale, accent);
        AsymmetricAddAmberAccentWell(w, hubOffset, rimH, hubScale, accent);

        RetroAddBoxUniform(w, hubOffset - hubScale * 0.28f, hubOffset + hubScale * 0.12f,
            rimH, rimH + hubScale * 0.28f, -hubScale * 0.20f, hubScale * 0.08f, secondary * 0.90f);
        RetroAddBoxUniform(w, hubOffset - hubScale * 0.22f, hubOffset + hubScale * 0.08f,
            rimH + hubScale * 0.28f, rimH + hubScale * 0.32f, -hubScale * 0.16f, hubScale * 0.04f, accent * 1.04f);

        ReadOnlySpan<float> cargoX = stackalloc float[] { 0.34f, 0.12f, 0.48f };
        ReadOnlySpan<float> cargoZ = stackalloc float[] { -0.24f, 0.28f, -0.08f };
        ReadOnlySpan<float> cargoH = stackalloc float[] { 0.32f, 0.28f, 0.36f };
        for (int c = 0; c < 3; c++)
        {
            float cx = hubOffset + cargoX[c] * s;
            float cz = cargoZ[c] * s;
            float ch = cargoH[c] * s;
            float halfW = s * (0.12f + c * 0.015f);
            RetroAddBoxUniform(w, cx - halfW, cx + halfW, rimH, rimH + ch,
                cz - halfW * 0.7f, cz + halfW * 0.7f, c % 2 == 0 ? primary * 0.93f : secondary * 0.91f);
            RetroAddBoxUniform(w, cx - halfW * 0.8f, cx + halfW * 0.8f, rimH + ch, rimH + ch + s * 0.03f,
                cz - halfW * 0.5f, cz + halfW * 0.5f, accent * (0.96f + c * 0.04f));
        }

        RetroAddBoxUniform(w, hubOffset + padR * 0.38f, hubOffset + padR * 0.72f, rimH, rimH + s * 0.08f,
            padR * 0.06f, padR * 0.26f, secondary * 0.88f);
        RetroAddBoxUniform(w, hubOffset - padR * 0.08f, hubOffset + padR * 0.28f, rimH, rimH + s * 0.06f,
            -padR * 0.58f, -padR * 0.36f, primary * 0.90f);
    }

    private static void BuildRadiantReactor(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        Vector3 engine = new(1f, 0.82f, 0.25f);
        float padR = s * 1.16f;
        float innerR = s * 0.62f;
        float rimH = s * 0.05f;

        RetroAddBoxUniform(w, -innerR, innerR, 0, rimH, -innerR, innerR, primary);
        for (int lobe = 0; lobe < 8; lobe++)
        {
            float a = MathF.PI * 2f * lobe / 8f;
            float cx = MathF.Cos(a) * (padR + innerR) * 0.5f;
            float cz = MathF.Sin(a) * (padR + innerR) * 0.5f;
            float tangW = padR * 0.12f;
            RetroAddBoxUniform(w, cx - tangW, cx + tangW, 0, rimH * 0.90f, cz - tangW * 0.55f, cz + tangW * 0.55f,
                secondary * 0.95f);
        }
        RadiantAddConcentricBand(w, 0, rimH * 0.82f, 0, padR * 0.98f, s * 0.024f, 16, accent);

        ReadOnlySpan<float> ringR = stackalloc float[] { 0.44f, 0.34f, 0.24f, 0.14f };
        for (int ring = 0; ring < 4; ring++)
        {
            float radius = s * ringR[ring];
            float ringY = rimH + s * 0.012f + ring * s * 0.007f;
            RadiantAddGlowRing(w, 0, ringY, 0, radius, s * 0.028f, 14,
                ring % 2 == 0 ? accent : primary * 0.96f);
        }

        int petals = 7;
        for (int p = 0; p < petals; p++)
        {
            float angle = MathF.PI * 2f * p / petals + MathF.PI / petals;
            RadiantAddSolarPetal(w, 0, rimH + s * 0.03f, 0, angle, s * 0.34f, s, primary, secondary, accent);
        }

        float domeBase = rimH + s * 0.02f;
        float domeH = s * 0.08f;
        RetroAddFacetedColumn(w, 0, 0, domeBase, domeBase + domeH * 0.65f, s * 0.10f, s * 0.07f, 8, secondary, secondary);
        RetroAddFacetedColumn(w, 0, 0, domeBase + domeH * 0.65f, domeBase + domeH, s * 0.07f, s * 0.02f, 6, primary, primary);
        RetroAddBoxUniform(w, -s * 0.06f, s * 0.06f, domeBase + s * 0.02f, domeBase + s * 0.05f, -s * 0.06f, s * 0.06f, engine);
        RetroAddBoxUniform(w, -s * 0.04f, s * 0.04f, domeBase + s * 0.04f, domeBase + s * 0.07f, -s * 0.04f, s * 0.04f, engine * 1.14f);
        RetroAddBoxUniform(w, -s * 0.12f, s * 0.12f, domeBase + domeH, domeBase + domeH + s * 0.018f, -s * 0.12f, s * 0.12f, accent);
    }

    private static void BuildRadiantCommandCenter(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        Vector3 engine = new(1f, 0.82f, 0.25f);
        float padR = s * 1.16f;
        float innerR = s * 0.62f;
        float rimH = s * 0.05f;

        RadiantAddSolarPad(w, padR, innerR, rimH, s, primary, secondary, accent);
        ReadOnlySpan<float> ringR = stackalloc float[] { 0.50f, 0.40f, 0.30f, 0.18f };
        RadiantAddGlowRingTiers(w, rimH, s, ringR, primary, secondary, accent);

        float hubBase = rimH + s * 0.02f;
        float hubH = s * 0.06f;
        RetroAddFacetedColumn(w, 0, 0, hubBase, hubBase + hubH * 0.65f, s * 0.12f, s * 0.09f, 8, secondary, secondary);
        RetroAddFacetedColumn(w, 0, 0, hubBase + hubH * 0.65f, hubBase + hubH, s * 0.09f, s * 0.03f, 6, primary, primary);
        RetroAddBoxUniform(w, -s * 0.05f, s * 0.05f, hubBase + s * 0.02f, hubBase + s * 0.05f, -s * 0.05f, s * 0.05f, engine);
        RetroAddBoxUniform(w, -s * 0.14f, s * 0.14f, hubBase + hubH, hubBase + hubH + s * 0.018f, -s * 0.14f, s * 0.14f, accent);

        for (int d = 0; d < 4; d++)
        {
            float a = MathF.PI * 0.5f * d + MathF.PI * 0.25f;
            float dx = MathF.Cos(a) * s * 0.52f;
            float dz = MathF.Sin(a) * s * 0.52f;
            RetroAddCommsDishLow(w, dx, rimH + s * 0.02f, dz, s * 0.12f, s, primary, secondary, accent);
        }
    }

    private static void BuildRadiantShipyard(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s,
        int gantries, int bays)
    {
        bool isLarge = bays >= 5;
        bool isSmall = bays <= 2 && gantries <= 2;
        float padR = s * (isLarge ? 1.24f : bays >= 3 ? 1.14f : 1.10f);
        float innerR = padR * 0.54f;
        float rimH = s * 0.05f;
        int ringCount = isLarge ? 5 : bays >= 3 ? 4 : 2;

        RadiantAddSolarPad(w, padR, innerR, rimH, s, primary, secondary, accent);

        for (int ring = 0; ring < ringCount; ring++)
        {
            float t = ringCount <= 1 ? 0f : ring / (float)(ringCount - 1);
            float radius = padR * (0.36f + t * 0.10f);
            float ringY = rimH + s * 0.012f + ring * s * 0.006f;
            RadiantAddGlowRing(w, 0, ringY, 0, radius, s * 0.026f, 12 + ring * 2,
                ring % 2 == 0 ? accent : primary * 0.96f);
        }

        for (int b = 0; b < bays; b++)
        {
            float z = -s * 0.28f + b * s * (isLarge ? 0.22f : 0.24f);
            float bayR = s * (isSmall ? 0.22f : isLarge ? 0.30f : 0.26f);
            RadiantAddGlowRing(w, 0, rimH + s * 0.018f, z, bayR, s * 0.022f, 10,
                b % 2 == 0 ? secondary : accent * 0.98f);
        }

        int petalArms = isSmall ? 2 : isLarge ? gantries : Math.Min(gantries, 4);
        for (int g = 0; g < petalArms; g++)
        {
            float angle = isSmall
                ? (g == 0 ? MathF.PI * 0.5f : -MathF.PI * 0.5f)
                : MathF.PI * 2f * g / petalArms;
            RadiantAddSolarPetal(w, 0, rimH + s * 0.03f, 0, angle, padR * (isLarge ? 0.82f : 0.72f), s, primary, secondary, accent);
        }

        if (isLarge)
        {
            for (int outer = 0; outer < 3; outer++)
            {
                float radius = padR * (0.88f + outer * 0.04f);
                RadiantAddGlowRing(w, 0, rimH + s * 0.024f + outer * s * 0.004f, 0, radius, s * 0.020f, 16, accent * (0.96f + outer * 0.02f));
            }
        }

        RetroAddBoxUniform(w, -s * 0.08f, s * 0.08f, rimH + s * 0.02f, rimH + s * 0.07f, -s * 0.08f, s * 0.08f, accent * 0.98f);
    }

    private static void BuildRadiantDefenseTurret(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        Vector3 engine = new(1f, 0.82f, 0.25f);
        float padR = s * 0.98f;
        float innerR = s * 0.56f;
        float rimH = s * 0.05f;
        float raisedH = s * 0.04f;

        RadiantAddSolarPad(w, padR, innerR, rimH, s, primary, secondary, accent);
        RetroAddBoxUniform(w, -innerR * 0.85f, innerR * 0.85f, rimH, rimH + raisedH, -innerR * 0.85f, innerR * 0.85f, secondary);
        RadiantAddConcentricBand(w, 0, rimH + raisedH * 0.75f, 0, innerR * 0.90f, s * 0.020f, 12, accent);

        float ringY = rimH + raisedH + s * 0.02f;
        RadiantAddGlowRing(w, 0, ringY, 0, s * 0.20f, s * 0.030f, 12, accent);
        RetroAddBoxUniform(w, -s * 0.14f, s * 0.14f, ringY + s * 0.03f, ringY + s * 0.045f, -s * 0.14f, s * 0.14f, accent * 1.04f);

        RetroAddFacetedColumn(w, 0, 0, ringY + s * 0.04f, ringY + s * 0.10f, s * 0.10f, s * 0.07f, 8, secondary, primary);
        RetroAddBoxUniform(w, -s * 0.04f, s * 0.04f, ringY + s * 0.06f, ringY + s * 0.09f, -s * 0.04f, s * 0.04f, engine * 0.95f);

        float barrelBase = ringY + s * 0.10f;
        RetroAddBoxUniform(w, -s * 0.06f, s * 0.06f, barrelBase, barrelBase + s * 0.04f, s * 0.02f, s * 0.14f, primary);
        RetroAddBoxUniform(w, -s * 0.04f, s * 0.04f, barrelBase + s * 0.02f, barrelBase + s * 0.04f, s * 0.14f, s * 0.28f, accent);
    }

    private static void BuildRadiantSensorArray(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.12f;
        float innerR = s * 0.60f;
        float rimH = s * 0.05f;

        RadiantAddSolarPad(w, padR, innerR, rimH, s, primary, secondary, accent);
        ReadOnlySpan<float> ringR = stackalloc float[] { 0.46f, 0.36f, 0.26f, 0.16f };
        RadiantAddGlowRingTiers(w, rimH, s, ringR, primary, secondary, accent);

        RetroAddBoxUniform(w, -s * 0.10f, s * 0.10f, rimH + s * 0.02f, rimH + s * 0.08f, -s * 0.10f, s * 0.10f, secondary);
        RetroAddBoxUniform(w, -s * 0.06f, s * 0.06f, rimH + s * 0.06f, rimH + s * 0.09f, -s * 0.06f, s * 0.06f, accent);

        for (int ring = 0; ring < 3; ring++)
        {
            float radius = s * (0.32f + ring * 0.12f);
            for (int d = 0; d < 6; d++)
            {
                float a = MathF.PI * 2f * d / 6f + ring * 0.15f;
                float dx = MathF.Cos(a) * radius;
                float dz = MathF.Sin(a) * radius;
                RetroAddCommsDishLow(w, dx, rimH + s * 0.02f, dz, s * 0.10f, s, primary, secondary, accent);
            }
        }
    }

    private static void BuildRadiantRefinery(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.14f;
        float innerR = s * 0.60f;
        float rimH = s * 0.05f;

        RadiantAddSolarPad(w, padR, innerR, rimH, s, primary, secondary, accent);
        ReadOnlySpan<float> ringR = stackalloc float[] { 0.42f, 0.32f, 0.22f };
        RadiantAddGlowRingTiers(w, rimH, s, ringR, primary, secondary, accent);

        for (int p = 0; p < 6; p++)
        {
            float a = MathF.PI * 2f * p / 6f;
            float px = MathF.Cos(a) * s * 0.34f;
            float pz = MathF.Sin(a) * s * 0.34f;
            RetroAddBoxUniform(w, px - s * 0.07f, px + s * 0.07f, rimH + s * 0.02f, rimH + s * 0.10f, pz - s * 0.06f, pz + s * 0.06f, primary);
            RetroAddBoxUniform(w, px - s * 0.06f, px + s * 0.06f, rimH + s * 0.10f, rimH + s * 0.12f, pz - s * 0.05f, pz + s * 0.05f, accent);
        }

        for (int intake = 0; intake < 2; intake++)
        {
            float angle = intake == 0 ? MathF.PI * 0.25f : MathF.PI * 1.25f;
            RadiantAddSolarPetal(w, 0, rimH + s * 0.03f, 0, angle, s * 0.38f, s, primary, secondary, accent);
            var dir = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
            float tipX = dir.X * s * 0.38f;
            float tipZ = dir.Z * s * 0.38f;
            RetroAddBoxUniform(w, tipX - s * 0.05f, tipX + s * 0.05f, rimH + s * 0.04f, rimH + s * 0.14f, tipZ - s * 0.05f, tipZ + s * 0.05f, secondary);
            RetroAddBoxUniform(w, tipX - s * 0.04f, tipX + s * 0.04f, rimH + s * 0.14f, rimH + s * 0.16f, tipZ - s * 0.04f, tipZ + s * 0.04f, accent);
        }
    }

    private static void BuildRadiantRepairBay(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.12f;
        float innerR = s * 0.60f;
        float rimH = s * 0.05f;

        RadiantAddSolarPad(w, padR, innerR, rimH, s, primary, secondary, accent);
        ReadOnlySpan<float> ringR = stackalloc float[] { 0.44f, 0.32f, 0.20f };
        RadiantAddGlowRingTiers(w, rimH, s, ringR, primary, secondary, accent);

        for (int slot = 0; slot < 3; slot++)
        {
            float x = -s * 0.18f + slot * s * 0.18f;
            RetroAddBoxUniform(w, x - s * 0.10f, x + s * 0.10f, rimH, rimH + s * 0.025f, -padR * 0.72f, -padR * 0.52f, secondary * 0.88f);
            RetroAddBoxUniform(w, x - s * 0.08f, x + s * 0.08f, rimH + s * 0.025f, rimH + s * 0.08f, -padR * 0.70f, -padR * 0.58f, primary * 0.92f);
            RadiantAddGlowRing(w, x, rimH + s * 0.03f, -padR * 0.62f, s * 0.12f, s * 0.020f, 8, slot % 2 == 0 ? accent : secondary);
        }

        for (int arm = 0; arm < 3; arm++)
        {
            float angle = MathF.PI * 0.15f + arm * MathF.PI * 0.35f;
            RadiantAddSolarPetal(w, 0, rimH + s * 0.03f, padR * 0.35f, angle, s * 0.42f, s, primary, secondary, accent);
        }

        RetroAddBoxUniform(w, -s * 0.08f, s * 0.08f, rimH + s * 0.02f, rimH + s * 0.07f, -s * 0.08f, s * 0.08f, accent * 0.96f);
    }

    private static void BuildRadiantSupplyDepot(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.08f;
        float innerR = s * 0.58f;
        float rimH = s * 0.05f;

        RadiantAddSolarPad(w, padR, innerR, rimH, s, primary, secondary, accent);
        ReadOnlySpan<float> ringR = stackalloc float[] { 0.40f, 0.30f, 0.20f };
        RadiantAddGlowRingTiers(w, rimH, s, ringR, primary, secondary, accent);

        RetroAddBoxUniform(w, -s * 0.14f, s * 0.14f, rimH + s * 0.02f, rimH + s * 0.10f, -s * 0.12f, s * 0.12f, secondary);
        RetroAddBoxUniform(w, -s * 0.12f, s * 0.12f, rimH + s * 0.10f, rimH + s * 0.12f, -s * 0.10f, s * 0.10f, accent);
        RadiantAddGlowRing(w, 0, rimH + s * 0.04f, 0, s * 0.18f, s * 0.024f, 10, primary * 0.96f);

        for (int ext = 0; ext < 2; ext++)
        {
            float angle = ext == 0 ? 0f : MathF.PI;
            RadiantAddSolarPetal(w, 0, rimH + s * 0.03f, 0, angle, padR * 0.78f, s, primary, secondary, accent);
            var dir = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
            float midX = dir.X * padR * 0.55f;
            float midZ = dir.Z * padR * 0.55f;
            RetroAddBoxUniform(w, midX - s * 0.08f, midX + s * 0.08f, rimH + s * 0.04f, rimH + s * 0.12f, midZ - s * 0.07f, midZ + s * 0.07f, primary);
            RetroAddBoxUniform(w, midX - s * 0.07f, midX + s * 0.07f, rimH + s * 0.12f, rimH + s * 0.14f, midZ - s * 0.06f, midZ + s * 0.06f, accent);
        }
    }

    // ── Vesper (vasudan) station geometry ───────────────────────────────────

    private static void VasudanAddBox(RaceMeshWriter w, float x0, float x1, float y0, float y1, float z0, float z1,
        Vector3 color)
        => RetroAddBox(w, x0, x1, y0, y1, z0, z1, color, color);

    private static void VasudanAddBoxUniform(RaceMeshWriter w, float x0, float x1, float y0, float y1, float z0, float z1,
        Vector3 color)
        => VasudanAddBox(w, x0, x1, y0, y1, z0, z1, color);

    private static void VasudanAddAccentBand(RaceMeshWriter w, float s, float padR, float y, float bandH, Vector3 accent)
    {
        float tangW = padR * 0.12f;
        for (int i = 0; i < 16; i++)
        {
            float a = MathF.PI * 2f * i / 16f;
            float cx = MathF.Cos(a) * padR;
            float cz = MathF.Sin(a) * padR;
            VasudanAddBoxUniform(w, cx - tangW, cx + tangW, y, y + bandH, cz - tangW * 0.55f, cz + tangW * 0.55f, accent);
        }
    }

    /// <summary>Angular sandstone terrace pad — stepped rim tiers + cyan accent lip bands (box-only).</summary>
    private static void VasudanAddTerracePad(RaceMeshWriter w, float s, float padR, float innerR, float rimH,
        Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        VasudanAddBoxUniform(w, -innerR, innerR, 0, rimH, -innerR, innerR, primary);

        for (int tier = 0; tier < 4; tier++)
        {
            float t = tier / 3f;
            float tierInner = innerR + (padR - innerR) * t * 0.82f;
            float tierOuter = innerR + (padR - innerR) * (t * 0.82f + 0.30f);
            float tierH = rimH * (0.78f + t * 0.22f);
            int segs = 8;
            float tangW = (tierOuter - tierInner) * 0.42f;
            float radialW = (tierOuter - tierInner) * 0.48f;
            for (int i = 0; i < segs; i++)
            {
                float a = MathF.PI * 2f * i / segs + (tier % 2) * MathF.PI / segs;
                float midR = (tierOuter + tierInner) * 0.5f;
                float cx = MathF.Cos(a) * midR;
                float cz = MathF.Sin(a) * midR;
                VasudanAddBoxUniform(w, cx - tangW, cx + tangW, 0, tierH, cz - radialW, cz + radialW,
                    tier % 2 == 0 ? primary : secondary);
            }

            VasudanAddAccentBand(w, s, tierOuter * 0.97f, tierH * 0.86f, s * 0.018f, accent);
        }

        for (int corner = 0; corner < 4; corner++)
        {
            float a = MathF.PI * 0.5f * corner + MathF.PI * 0.25f;
            float cx = MathF.Cos(a) * padR * 0.93f;
            float cz = MathF.Sin(a) * padR * 0.93f;
            float tangX = MathF.Cos(a) * s * 0.09f;
            float tangZ = MathF.Sin(a) * s * 0.09f;
            VasudanAddBoxUniform(w, cx - tangX, cx + tangX, 0, rimH * 0.94f, cz - tangZ, cz + tangZ, secondary);
        }
    }

    /// <summary>Flush ascending terrace tier box toward hub center.</summary>
    private static void VasudanAddTerraceTier(RaceMeshWriter w, float cx, float cy, float cz, float scale, Vector3 color)
    {
        VasudanAddBoxUniform(w, cx - scale * 0.24f, cx + scale * 0.24f, cy, cy + scale * 0.10f,
            cz - scale * 0.24f, cz + scale * 0.24f, color);
    }

    /// <summary>Flush radial terrace service arm — angular offsets, box-only.</summary>
    private static void VasudanAddFlushRadialArm(RaceMeshWriter w, float s, float angle, float armStart, float armEnd,
        float rimH, float armH, Vector3 body, Vector3 tipAccent)
    {
        var dir = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
        float halfLen = (armEnd - armStart) * 0.5f;
        float cx = dir.X * (armStart + armEnd) * 0.5f;
        float cz = dir.Z * (armStart + armEnd) * 0.5f;
        float perpX = -dir.Z * s * 0.075f;
        float perpZ = dir.X * s * 0.075f;
        VasudanAddBoxUniform(w,
            cx - dir.X * halfLen - perpX, cx + dir.X * halfLen + perpX,
            rimH, rimH + armH,
            cz - dir.Z * halfLen - perpZ, cz + dir.Z * halfLen + perpZ,
            body);
        float tx = dir.X * armEnd;
        float tz = dir.Z * armEnd;
        VasudanAddBoxUniform(w, tx - s * 0.05f, tx + s * 0.05f, rimH + armH * 0.60f, rimH + armH + s * 0.02f,
            tz - s * 0.05f, tz + s * 0.05f, tipAccent);
    }

    private static void BuildVasudanCommandCenter(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.16f;
        float innerR = s * 0.76f;
        float rimH = s * 0.06f;
        float hubScale = s * 0.70f;

        VasudanAddTerracePad(w, s, padR, innerR, rimH, primary, secondary, accent);

        float cy = rimH;
        for (int tier = 0; tier < 4; tier++)
        {
            float shrink = 1f - tier * 0.17f;
            float tierScale = hubScale * shrink;
            VasudanAddTerraceTier(w, 0, cy, 0, tierScale, tier % 2 == 0 ? secondary : primary);
            if (tier < 3)
                VasudanAddAccentBand(w, s, tierScale * 0.96f, cy + s * (0.08f + tier * 0.03f), s * 0.020f, accent);
            cy += s * (0.07f + tier * 0.015f);
        }

        VasudanAddBoxUniform(w, -hubScale * 0.14f, hubScale * 0.14f, cy, cy + s * 0.03f,
            -hubScale * 0.14f, hubScale * 0.14f, accent);

        for (int d = 0; d < 4; d++)
        {
            float a = MathF.PI * 0.5f * d + MathF.PI * 0.125f;
            float dx = MathF.Cos(a) * s * 0.50f;
            float dz = MathF.Sin(a) * s * 0.50f;
            VasudanAddBoxUniform(w, dx - s * 0.06f, dx + s * 0.06f, rimH + s * 0.02f, rimH + s * 0.10f,
                dz - s * 0.05f, dz + s * 0.05f, secondary);
            VasudanAddBoxUniform(w, dx - s * 0.04f, dx + s * 0.04f, rimH + s * 0.10f, rimH + s * 0.12f,
                dz - s * 0.03f, dz + s * 0.03f, accent);
        }

        for (int arm = 0; arm < 4; arm++)
        {
            float angle = MathF.PI * 0.5f * arm + MathF.PI * 0.18f;
            VasudanAddFlushRadialArm(w, s, angle, s * 0.32f, padR * 0.94f, rimH, s * 0.06f, secondary, accent);
        }

        float spineBase = cy;
        VasudanAddBoxUniform(w, -s * 0.03f, s * 0.03f, spineBase, spineBase + s * 0.30f, -s * 0.03f, s * 0.03f, secondary);
        VasudanAddBoxUniform(w, -s * 0.04f, s * 0.04f, spineBase + s * 0.30f, spineBase + s * 0.48f, -s * 0.04f, s * 0.04f, accent);

        for (int pod = 0; pod < 4; pod++)
        {
            float angle = MathF.PI * 0.25f + MathF.PI * 0.5f * pod;
            float px = MathF.Cos(angle) * s * 0.66f;
            float pz = MathF.Sin(angle) * s * 0.66f;
            VasudanAddBoxUniform(w, px - s * 0.06f, px + s * 0.06f, rimH, rimH + s * 0.07f, pz - s * 0.06f, pz + s * 0.06f, primary);
        }
    }

    private static void VasudanAddCommsDishLow(RaceMeshWriter w, float cx, float cy, float cz, float reach, float s,
        Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        VasudanAddBoxUniform(w, cx - s * 0.04f, cx + s * 0.04f, cy, cy + s * 0.05f, cz - s * 0.04f, cz + s * 0.04f, secondary);
        VasudanAddBoxUniform(w, cx - reach * 0.55f, cx + reach * 0.55f, cy + s * 0.05f, cy + s * 0.08f,
            cz - reach * 0.28f, cz + reach * 0.10f, primary);
        VasudanAddBoxUniform(w, cx - s * 0.025f, cx + s * 0.025f, cy + s * 0.08f, cy + s * 0.10f,
            cz + reach * 0.08f, cz + reach * 0.14f, accent);
    }

    private static void VasudanAddCoolingRingFlush(RaceMeshWriter w, float cx, float cy, float cz, float radius, float tube,
        int segments, Vector3 color)
    {
        for (int i = 0; i < segments; i++)
        {
            float a = MathF.PI * 2f * i / segments;
            float px = cx + MathF.Cos(a) * radius;
            float pz = cz + MathF.Sin(a) * radius;
            float tangX = -MathF.Sin(a) * tube * 1.15f;
            float tangZ = MathF.Cos(a) * tube * 1.15f;
            VasudanAddBoxUniform(w, px - tangX, px + tangX, cy, cy + tube, pz - tangZ, pz + tangZ, color);
        }
    }

    private static void BuildVasudanShipyard(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent,
        float s, int gantries, int bays)
    {
        if (bays >= 5)
            BuildVasudanShipyardLarge(w, primary, secondary, accent, s, gantries, bays);
        else if (bays <= 2)
            BuildVasudanShipyardSmall(w, primary, secondary, accent, s, gantries, bays);
        else
            BuildVasudanShipyardMedium(w, primary, secondary, accent, s, gantries, bays);
    }

    private static void BuildVasudanShipyardSmall(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent,
        float s, int gantries, int bays)
    {
        float padR = s * 1.06f;
        float innerR = padR * 0.62f;
        float rimH = s * 0.06f;
        float frameH = s * 0.48f;
        float spanX = s * 0.68f;

        VasudanAddTerracePad(w, s, padR, innerR, rimH, primary, secondary, accent);
        VasudanAddTerraceTier(w, 0, rimH, 0, s * 0.56f, secondary);
        VasudanAddTerraceTier(w, 0, rimH + s * 0.08f, 0, s * 0.44f, primary);
        VasudanAddAccentBand(w, s, s * 0.50f, rimH + s * 0.16f, s * 0.018f, accent);

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * spanX;
            VasudanAddBoxUniform(w, x - side * s * 0.05f, x, rimH, frameH, -padR * 0.58f, padR * 0.58f, secondary);
            VasudanAddFlushRadialArm(w, s, side < 0 ? MathF.PI * 0.72f : MathF.PI * 0.28f, s * 0.28f, padR * 0.90f,
                rimH + s * 0.02f, s * 0.05f, primary, accent);
        }

        for (int b = 0; b < bays; b++)
        {
            float z = -s * 0.14f + b * s * 0.22f;
            VasudanAddBoxUniform(w, -s * 0.14f, s * 0.14f, rimH + s * 0.02f, rimH + s * 0.12f, z - s * 0.06f, z + s * 0.06f,
                b % 2 == 0 ? primary : secondary);
            VasudanAddBoxUniform(w, -s * 0.12f, s * 0.12f, rimH + s * 0.12f, rimH + s * 0.15f, z - s * 0.05f, z + s * 0.05f, accent);
        }

        for (int crane = -1; crane <= 1; crane += 2)
        {
            float x = crane * s * 0.30f;
            VasudanAddBoxUniform(w, x - s * 0.04f, x + s * 0.04f, rimH, rimH + s * 0.12f, s * 0.16f, s * 0.32f, secondary);
            VasudanAddBoxUniform(w, x - s * 0.03f, x + s * 0.16f, rimH + s * 0.10f, rimH + s * 0.13f, s * 0.24f, s * 0.30f, accent);
        }
    }

    private static void BuildVasudanShipyardMedium(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent,
        float s, int gantries, int bays)
    {
        float padR = s * 1.10f;
        float innerR = padR * 0.60f;
        float rimH = s * 0.06f;
        float frameH = s * 0.52f;
        float spanX = s * 0.78f;

        VasudanAddTerracePad(w, s, padR, innerR, rimH, primary, secondary, accent);

        float cy = rimH;
        for (int tier = 0; tier < 3; tier++)
        {
            float tierScale = s * (0.62f - tier * 0.10f);
            VasudanAddTerraceTier(w, 0, cy, 0, tierScale, tier % 2 == 0 ? secondary : primary);
            VasudanAddAccentBand(w, s, tierScale * 0.94f, cy + s * 0.08f, s * 0.018f, accent);
            cy += s * 0.08f;
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * spanX;
            VasudanAddBoxUniform(w, x - side * s * 0.05f, x, rimH, frameH, -padR * 0.66f, padR * 0.66f, secondary);
            for (int tier = 0; tier <= gantries; tier++)
            {
                float t = tier / MathF.Max(1f, gantries);
                float y = rimH + s * 0.04f + tier * (frameH - rimH - s * 0.08f) / MathF.Max(1, gantries);
                VasudanAddFlushRadialArm(w, s, side < 0 ? MathF.PI * 0.78f : MathF.PI * 0.22f,
                    s * (0.30f + t * 0.08f), padR * (0.86f + t * 0.06f), y, s * 0.04f, primary, accent);
            }
        }

        for (int b = 0; b < bays; b++)
        {
            float z = -s * 0.22f + b * s * 0.20f;
            float bayW = s * 0.16f;
            VasudanAddBoxUniform(w, -bayW, bayW, rimH + s * 0.02f, rimH + s * 0.12f, z - s * 0.07f, z + s * 0.07f,
                b % 2 == 0 ? primary : secondary);
            float roofW = bayW * 0.72f;
            VasudanAddBoxUniform(w, -roofW, roofW, rimH + s * 0.12f, rimH + s * 0.16f, z - s * 0.05f, z + s * 0.05f,
                b % 2 == 0 ? secondary : primary);
            if (b % 2 == 0)
                VasudanAddBoxUniform(w, -roofW * 0.9f, roofW * 0.9f, rimH + s * 0.16f, rimH + s * 0.18f,
                    z - s * 0.04f, z + s * 0.04f, accent);
        }
    }

    private static void BuildVasudanShipyardLarge(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent,
        float s, int gantries, int bays)
    {
        float padR = s * 1.16f;
        float innerR = padR * 0.58f;
        float rimH = s * 0.06f;
        float frameH = s * 0.54f;
        float spanX = s * 0.86f;

        VasudanAddTerracePad(w, s, padR, innerR, rimH, primary, secondary, accent);

        float cy = rimH;
        for (int tier = 0; tier < 4; tier++)
        {
            float tierScale = s * (0.74f - tier * 0.12f);
            VasudanAddTerraceTier(w, 0, cy, 0, tierScale, tier % 2 == 0 ? secondary : primary);
            if (tier < 3)
                VasudanAddAccentBand(w, s, tierScale * 0.95f, cy + s * 0.07f, s * 0.020f, accent);
            cy += s * 0.07f;
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * spanX;
            VasudanAddBoxUniform(w, x - side * s * 0.06f, x, rimH, frameH, -padR * 0.74f, padR * 0.74f, secondary);
            for (int tier = 0; tier <= gantries; tier++)
            {
                float t = tier / MathF.Max(1f, gantries);
                float y = rimH + s * 0.03f + tier * (frameH - rimH - s * 0.10f) / MathF.Max(1, gantries);
                VasudanAddFlushRadialArm(w, s, side < 0 ? MathF.PI * 0.82f : MathF.PI * 0.18f,
                    s * (0.34f + t * 0.10f), padR * (0.88f + t * 0.08f), y, s * 0.05f, primary, accent);
            }
        }

        VasudanAddBoxUniform(w, -spanX, spanX, frameH - s * 0.04f, frameH, -padR * 0.76f, padR * 0.76f, accent);

        for (int g = 0; g < gantries; g++)
        {
            float x = (g - (gantries - 1) * 0.5f) * s * 0.36f;
            VasudanAddBoxUniform(w, x - s * 0.04f, x + s * 0.04f, rimH, frameH * 0.82f, -padR * 0.68f, -padR * 0.58f, secondary);
            VasudanAddBoxUniform(w, x, x + s * 0.20f, frameH * 0.74f, frameH * 0.78f, -padR * 0.66f, -padR * 0.52f, accent);
        }

        for (int b = 0; b < bays; b++)
        {
            float z = -s * 0.26f + b * s * 0.17f;
            float bayW = s * 0.20f;
            VasudanAddBoxUniform(w, -bayW, bayW, rimH + s * 0.02f, rimH + s * 0.14f, z - s * 0.07f, z + s * 0.07f,
                b % 2 == 0 ? primary : secondary);
            if (b % 2 == 0)
                VasudanAddBoxUniform(w, -bayW * 0.92f, bayW * 0.92f, rimH + s * 0.14f, rimH + s * 0.17f,
                    z - s * 0.05f, z + s * 0.05f, accent);
        }
    }

    private static void BuildVasudanDefenseTurret(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 0.94f;
        float innerR = padR * 0.54f;
        float rimH = s * 0.06f;
        float raisedH = s * 0.08f;

        VasudanAddTerracePad(w, s, padR, innerR, rimH, primary, secondary, accent);
        VasudanAddBoxUniform(w, -innerR * 0.86f, innerR * 0.86f, rimH, rimH + raisedH, -innerR * 0.86f, innerR * 0.86f, secondary);
        VasudanAddAccentBand(w, s, innerR * 0.90f, rimH + raisedH * 0.70f, s * 0.016f, accent);

        float ringY = rimH + raisedH + s * 0.02f;
        float ringR = s * 0.16f;
        for (int i = 0; i < 8; i++)
        {
            float a = MathF.PI * 2f * i / 8f;
            float cx = MathF.Cos(a) * ringR;
            float cz = MathF.Sin(a) * ringR;
            VasudanAddBoxUniform(w, cx - s * 0.03f, cx + s * 0.03f, ringY, ringY + s * 0.04f, cz - s * 0.03f, cz + s * 0.03f, secondary);
        }
        VasudanAddBoxUniform(w, -ringR * 0.50f, ringR * 0.50f, ringY + s * 0.04f, ringY + s * 0.05f,
            -ringR * 0.50f, ringR * 0.50f, accent);

        for (int barrel = 0; barrel < 4; barrel++)
        {
            float angle = MathF.PI * 0.5f * barrel + MathF.PI * 0.25f;
            var dir = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
            float bx = dir.X * s * 0.26f;
            float bz = dir.Z * s * 0.26f;
            float perpX = -dir.Z * s * 0.022f;
            float perpZ = dir.X * s * 0.022f;
            VasudanAddBoxUniform(w, bx - perpX, bx + perpX, ringY + s * 0.04f, ringY + s * 0.07f,
                bz - s * 0.07f - perpZ, bz + s * 0.03f + perpZ, primary);
            VasudanAddBoxUniform(w, bx - perpX * 0.8f, bx + perpX * 0.8f, ringY + s * 0.07f, ringY + s * 0.09f,
                bz + s * 0.02f, bz + s * 0.10f, accent);
        }

        for (int pod = 0; pod < 4; pod++)
        {
            float angle = MathF.PI * 0.5f * pod;
            float px = MathF.Cos(angle) * padR * 0.70f;
            float pz = MathF.Sin(angle) * padR * 0.70f;
            VasudanAddBoxUniform(w, px - s * 0.05f, px + s * 0.05f, rimH, rimH + s * 0.06f, pz - s * 0.05f, pz + s * 0.05f, primary);
        }
    }

    private static void BuildVasudanSensorArray(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.08f;
        float innerR = padR * 0.64f;
        float rimH = s * 0.06f;

        VasudanAddTerracePad(w, s, padR, innerR, rimH, primary, secondary, accent);
        VasudanAddTerraceTier(w, 0, rimH, 0, s * 0.40f, secondary);
        VasudanAddTerraceTier(w, 0, rimH + s * 0.08f, 0, s * 0.30f, primary);
        VasudanAddAccentBand(w, s, s * 0.36f, rimH + s * 0.14f, s * 0.018f, accent);

        float deckY = rimH + s * 0.02f;
        for (int d = 0; d < 4; d++)
        {
            float a = MathF.PI * 0.5f * d + MathF.PI * 0.25f;
            float dx = MathF.Cos(a) * s * 0.46f;
            float dz = MathF.Sin(a) * s * 0.46f;
            VasudanAddCommsDishLow(w, dx, deckY, dz, s * 0.12f, s, primary, secondary, accent);
        }

        VasudanAddCoolingRingFlush(w, 0, rimH + s * 0.05f, 0, s * 0.38f, s * 0.022f, 12, secondary);
        VasudanAddBoxUniform(w, -s * 0.05f, s * 0.05f, rimH + s * 0.07f, rimH + s * 0.10f, -s * 0.05f, s * 0.05f, accent);
    }

    private static void BuildVasudanRefinery(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.10f;
        float innerR = padR * 0.60f;
        float rimH = s * 0.06f;

        VasudanAddTerracePad(w, s, padR, innerR, rimH, primary, secondary, accent);
        VasudanAddTerraceTier(w, 0, rimH, 0, s * 0.36f, secondary);
        VasudanAddAccentBand(w, s, s * 0.32f, rimH + s * 0.08f, s * 0.018f, accent);

        ReadOnlySpan<float> procX = stackalloc float[] { -0.20f, 0.16f, 0.02f };
        ReadOnlySpan<float> procZ = stackalloc float[] { 0.10f, -0.08f, 0.24f };
        ReadOnlySpan<float> procH = stackalloc float[] { 0.14f, 0.16f, 0.12f };
        for (int p = 0; p < 3; p++)
        {
            float x = procX[p] * s;
            float z = procZ[p] * s;
            float h = procH[p] * s;
            float halfW = s * 0.10f;
            VasudanAddBoxUniform(w, x - halfW, x + halfW, rimH, rimH + h, z - halfW * 0.70f, z + halfW * 0.70f, primary);
            VasudanAddBoxUniform(w, x - halfW * 0.75f, x + halfW * 0.75f, rimH + h, rimH + h + s * 0.02f,
                z - halfW * 0.55f, z + halfW * 0.55f, accent);
        }

        for (int intake = 0; intake < 2; intake++)
        {
            float side = intake == 0 ? -1f : 1f;
            float x = side * s * 0.32f;
            float h = s * 0.20f;
            float halfW = s * 0.08f;
            VasudanAddBoxUniform(w, x - halfW, x + halfW, rimH, rimH + h, -halfW * 0.8f, halfW * 0.8f, secondary);
            VasudanAddBoxUniform(w, x - halfW * 0.70f, x + halfW * 0.70f, rimH + h, rimH + h + s * 0.025f,
                -halfW * 0.65f, halfW * 0.65f, accent);
        }

        VasudanAddBoxUniform(w, -s * 0.28f, s * 0.28f, rimH + s * 0.03f, rimH + s * 0.06f, s * 0.06f, s * 0.20f, secondary);
        VasudanAddBoxUniform(w, -s * 0.26f, s * 0.26f, rimH + s * 0.03f, rimH + s * 0.06f, -s * 0.20f, -s * 0.06f, secondary);
    }

    private static void BuildVasudanRepairBay(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.08f;
        float innerR = padR * 0.62f;
        float rimH = s * 0.06f;
        float shellH = s * 0.16f;

        VasudanAddTerracePad(w, s, padR, innerR, rimH, primary, secondary, accent);
        VasudanAddBoxUniform(w, -s * 0.32f, s * 0.32f, rimH, rimH + shellH, -s * 0.20f, s * 0.12f, primary);
        VasudanAddAccentBand(w, s, s * 0.30f, rimH + shellH * 0.88f, s * 0.018f, accent);

        for (int slot = 0; slot < 3; slot++)
        {
            float x = -s * 0.18f + slot * s * 0.18f;
            float recessD = s * 0.05f;
            VasudanAddBoxUniform(w, x - s * 0.07f, x + s * 0.07f, rimH, rimH + recessD, -s * 0.22f, -s * 0.12f, secondary);
            VasudanAddBoxUniform(w, x - s * 0.06f, x + s * 0.06f, rimH + recessD, rimH + shellH * 0.80f, -s * 0.26f, -s * 0.24f, primary);
            if (slot % 2 == 0)
                VasudanAddBoxUniform(w, x - s * 0.05f, x + s * 0.05f, rimH + shellH * 0.76f, rimH + shellH * 0.80f,
                    -s * 0.25f, -s * 0.23f, accent);
        }

        float craneY = rimH + shellH * 0.68f;
        VasudanAddBoxUniform(w, -s * 0.30f, s * 0.30f, craneY, craneY + s * 0.04f, -s * 0.05f, s * 0.05f, secondary);
        VasudanAddBoxUniform(w, -s * 0.28f, s * 0.28f, craneY + s * 0.04f, craneY + s * 0.06f, -s * 0.03f, s * 0.03f, accent);

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * s * 0.36f;
            VasudanAddBoxUniform(w, x - s * 0.03f, x + s * 0.03f, rimH, rimH + shellH * 0.60f, s * 0.04f, s * 0.10f, secondary);
            VasudanAddBoxUniform(w, x - s * 0.02f, x + s * 0.12f, rimH + shellH * 0.54f, rimH + shellH * 0.58f, s * 0.02f, s * 0.08f, accent);
        }
    }

    private static void BuildVasudanReactor(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        Vector3 engine = new(0.12f, 0.78f, 0.68f);
        float padR = s * 1.06f;
        float innerR = padR * 0.64f;
        float rimH = s * 0.06f;

        VasudanAddTerracePad(w, s, padR, innerR, rimH, primary, secondary, accent);
        VasudanAddTerraceTier(w, 0, rimH, 0, s * 0.44f, secondary);
        VasudanAddTerraceTier(w, 0, rimH + s * 0.08f, 0, s * 0.32f, primary);
        VasudanAddAccentBand(w, s, s * 0.38f, rimH + s * 0.14f, s * 0.018f, accent);

        float domeBase = rimH + s * 0.10f;
        VasudanAddBoxUniform(w, -s * 0.14f, s * 0.14f, domeBase, domeBase + s * 0.08f, -s * 0.14f, s * 0.14f, secondary);
        VasudanAddBoxUniform(w, -s * 0.10f, s * 0.10f, domeBase + s * 0.08f, domeBase + s * 0.11f, -s * 0.10f, s * 0.10f, primary);
        VasudanAddBoxUniform(w, -s * 0.07f, s * 0.07f, domeBase + s * 0.03f, domeBase + s * 0.07f, -s * 0.07f, s * 0.07f, engine);
        VasudanAddBoxUniform(w, -s * 0.05f, s * 0.05f, domeBase + s * 0.06f, domeBase + s * 0.09f, -s * 0.05f, s * 0.05f, engine * 1.08f);
        VasudanAddBoxUniform(w, -s * 0.12f, s * 0.12f, domeBase + s * 0.11f, domeBase + s * 0.13f, -s * 0.12f, s * 0.12f, accent);

        VasudanAddCoolingRingFlush(w, 0, rimH + s * 0.06f, 0, s * 0.40f, s * 0.025f, 12, secondary);
        VasudanAddCoolingRingFlush(w, 0, rimH + s * 0.08f, 0, s * 0.28f, s * 0.020f, 10, primary);

        for (int c = 0; c < 4; c++)
        {
            float angle = MathF.PI * 0.25f + MathF.PI * 0.5f * c;
            float cx = MathF.Cos(angle) * s * 0.44f;
            float cz = MathF.Sin(angle) * s * 0.44f;
            VasudanAddBoxUniform(w, cx - s * 0.05f, cx + s * 0.05f, rimH, rimH + s * 0.08f, cz - s * 0.05f, cz + s * 0.05f, primary);
            VasudanAddBoxUniform(w, cx - s * 0.04f, cx + s * 0.04f, rimH + s * 0.08f, rimH + s * 0.10f, cz - s * 0.04f, cz + s * 0.04f, accent);
        }
    }

    private static void BuildVasudanSupplyDepot(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.02f;
        float innerR = padR * 0.58f;
        float rimH = s * 0.06f;

        VasudanAddTerracePad(w, s, padR, innerR, rimH, primary, secondary, accent);
        VasudanAddBoxUniform(w, -s * 0.14f, s * 0.14f, rimH, rimH + s * 0.12f, -s * 0.10f, s * 0.10f, secondary);
        VasudanAddBoxUniform(w, -s * 0.12f, s * 0.12f, rimH + s * 0.12f, rimH + s * 0.14f, -s * 0.08f, s * 0.08f, accent);
        VasudanAddBoxUniform(w, -s * 0.09f, s * 0.09f, rimH + s * 0.05f, rimH + s * 0.09f, -s * 0.05f, s * 0.05f, primary);

        for (int ext = 0; ext < 2; ext++)
        {
            float side = ext == 0 ? -1f : 1f;
            float padStart = padR * 0.50f;
            float padEnd = padR * 0.92f;
            float cx = side * (padStart + padEnd) * 0.5f;
            float halfLen = (padEnd - padStart) * 0.5f;
            VasudanAddBoxUniform(w, cx - halfLen, cx + halfLen, rimH, rimH + s * 0.04f, -s * 0.10f, s * 0.10f, primary);
            VasudanAddAccentBand(w, s, MathF.Abs(cx) + halfLen * 0.5f, rimH + s * 0.03f, s * 0.016f, accent);

            for (int cargo = 0; cargo < 2; cargo++)
            {
                float z = (cargo == 0 ? -1f : 1f) * s * 0.05f;
                float h = s * (cargo == 0 ? 0.12f : 0.14f);
                float halfW = s * 0.07f;
                VasudanAddBoxUniform(w, cx - halfW, cx + halfW, rimH + s * 0.04f, rimH + s * 0.04f + h,
                    z - halfW * 0.75f, z + halfW * 0.75f, secondary);
                VasudanAddBoxUniform(w, cx - halfW * 0.85f, cx + halfW * 0.85f, rimH + s * 0.04f + h, rimH + s * 0.04f + h + s * 0.02f,
                    z - halfW * 0.60f, z + halfW * 0.60f, accent);
            }
        }

        for (int corner = 0; corner < 4; corner++)
        {
            float a = MathF.PI * 0.5f * corner + MathF.PI * 0.25f;
            float px = MathF.Cos(a) * padR * 0.76f;
            float pz = MathF.Sin(a) * padR * 0.76f;
            VasudanAddBoxUniform(w, px - s * 0.04f, px + s * 0.04f, rimH, rimH + s * 0.05f, pz - s * 0.04f, pz + s * 0.04f, primary);
        }
    }

    // ── Korath (truss) station geometry ─────────────────────────────────────

    private static void TrussAddBox(RaceMeshWriter w, float x0, float x1, float y0, float y1, float z0, float z1,
        Vector3 color)
        => RetroAddBox(w, x0, x1, y0, y1, z0, z1, color, color);

    private static void TrussAddFrameBox(RaceMeshWriter w, float x0, float x1, float y0, float y1, float z0, float z1,
        Vector3 color)
        => TrussAddBox(w, x0, x1, y0, y1, z0, z1, color);

    /// <summary>Dual-tone NASA flush panel cluster on gantry deck surfaces.</summary>
    private static void TrussAddNasaPanel(RaceMeshWriter w, float x0, float x1, float y0, float y1, float z0, float z1,
        Vector3 white, Vector3 grey)
    {
        TrussAddFrameBox(w, x0, x1, y0, y1, z0, z1, white);
        float inset = (x1 - x0) * 0.08f;
        TrussAddFrameBox(w, x0 + inset, x1 - inset, y0, y1, z0 + inset * 0.5f, z1 - inset * 0.5f, grey);
    }

    /// <summary>Widest skeletal pad — open truss corner posts dominate plan silhouette.</summary>
    private static void TrussAddSkeletalPad(RaceMeshWriter w, float s, float padR, float innerR, float rimH,
        Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        TrussAddNasaPanel(w, -innerR, innerR, 0, rimH, -innerR, innerR, primary, secondary);

        int segs = 10;
        float tangW = padR * 0.22f;
        float radialW = (padR - innerR) * 0.44f;
        for (int i = 0; i < segs; i++)
        {
            float a = MathF.PI * 2f * i / segs;
            float midR = (padR + innerR) * 0.5f;
            float cx = MathF.Cos(a) * midR;
            float cz = MathF.Sin(a) * midR;
            TrussAddFrameBox(w, cx - tangW, cx + tangW, 0, rimH * 0.88f, cz - radialW, cz + radialW,
                i % 2 == 0 ? secondary : primary * 0.96f);
        }

        for (int corner = 0; corner < 4; corner++)
        {
            float a = MathF.PI * 0.5f * corner;
            float cx = MathF.Cos(a) * padR * 0.96f;
            float cz = MathF.Sin(a) * padR * 0.96f;
            TrussAddFrameBox(w, cx - s * 0.05f, cx + s * 0.05f, 0, rimH + s * 0.14f, cz - s * 0.05f, cz + s * 0.05f, secondary);
            TrussAddFrameBox(w, cx - s * 0.04f, cx + s * 0.04f, rimH + s * 0.12f, rimH + s * 0.16f, cz - s * 0.04f, cz + s * 0.04f, accent);
        }
    }

    /// <summary>Escalating exposed truss gantry boom arm.</summary>
    private static void TrussAddGantryBoom(RaceMeshWriter w, float x, int side, float y, float reach, float depth,
        float s, float tierT, Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        TrussAddFrameBox(w, x - side * s * (0.10f + tierT * 0.04f), x - side * s * 0.02f, y, y + s * 0.04f,
            -reach, reach, primary);
        TrussAddFrameBox(w, x - side * s * (0.12f + tierT * 0.02f), x - side * s * 0.02f, y + s * 0.04f, y + s * 0.06f,
            -depth, depth, tierT > 0.85f ? accent : secondary);
        TrussAddNasaPanel(w, x - side * s * 0.08f, x - side * s * 0.03f, y - s * 0.02f, y + s * 0.02f,
            -reach * 0.72f, reach * 0.72f, primary, secondary);
    }

    private static void BuildTrussShipyard(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent,
        float s, int gantries, int bays)
    {
        if (bays >= 5)
            BuildTrussShipyardLarge(w, primary, secondary, accent, s, gantries, bays);
        else
            BuildTrussShipyardStub(w, primary, secondary, accent, s, gantries, bays);
    }

    private static void BuildTrussShipyardStub(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent,
        float s, int gantries, int bays)
    {
        bool isSmall = bays <= 2 && gantries <= 2;
        float padR = s * (isSmall ? 1.08f : 1.14f);
        float innerR = padR * 0.60f;
        float rimH = s * 0.06f;
        float frameH = s * (isSmall ? 0.50f : 0.54f);
        float spanX = s * (isSmall ? 0.74f : 0.82f);

        TrussAddSkeletalPad(w, s, padR, innerR, rimH, primary, secondary, accent);
        TrussAddNasaPanel(w, -spanX * 0.36f, spanX * 0.36f, rimH, rimH + s * 0.04f, -padR * 0.40f, padR * 0.40f, primary, secondary);

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * spanX;
            TrussAddFrameBox(w, x - side * s * 0.05f, x, rimH, frameH, -padR * 0.68f, padR * 0.68f, secondary);
            for (int tier = 0; tier <= gantries; tier++)
            {
                float t = tier / MathF.Max(1f, gantries);
                float y = rimH + s * 0.05f + tier * (frameH - rimH - s * 0.08f) / MathF.Max(1, gantries);
                float reach = spanX * (0.80f + t * 0.12f);
                TrussAddGantryBoom(w, x, side, y, reach, padR * 0.66f, s, t, primary, secondary, accent);
            }
        }

        for (int b = 0; b < bays; b++)
        {
            float z = -s * 0.20f + b * s * 0.22f;
            float bayW = s * 0.16f;
            TrussAddFrameBox(w, -bayW, bayW, rimH + s * 0.02f, rimH + s * 0.12f, z - s * 0.06f, z + s * 0.06f,
                b % 2 == 0 ? primary : secondary);
        }
    }

    private static void BuildTrussShipyardLarge(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent,
        float s, int gantries, int bays)
    {
        float padR = s * 1.30f;
        float innerR = padR * 0.58f;
        float rimH = s * 0.06f;
        float frameH = s * 0.58f;
        float spanX = s * 0.94f;

        TrussAddSkeletalPad(w, s, padR, innerR, rimH, primary, secondary, accent);
        TrussAddNasaPanel(w, -spanX * 0.44f, spanX * 0.44f, rimH, rimH + s * 0.04f, -padR * 0.48f, padR * 0.48f, primary, secondary);

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * spanX;
            TrussAddFrameBox(w, x - side * s * 0.06f, x, rimH, frameH, -padR * 0.76f, padR * 0.76f, secondary);
            for (int tier = 0; tier <= gantries; tier++)
            {
                float t = tier / MathF.Max(1f, gantries);
                float y = rimH + s * 0.04f + tier * (frameH - rimH - s * 0.10f) / MathF.Max(1, gantries);
                float reach = spanX * (0.74f + t * 0.20f);
                float depth = padR * (0.58f + t * 0.16f);
                TrussAddGantryBoom(w, x, side, y, reach, depth, s, t, primary, secondary, accent);
            }
        }

        TrussAddFrameBox(w, -spanX, spanX, frameH - s * 0.04f, frameH, -padR * 0.78f, padR * 0.78f, accent);
        TrussAddNasaPanel(w, -spanX * 0.94f, spanX * 0.94f, frameH - s * 0.06f, frameH - s * 0.02f,
            -padR * 0.72f, padR * 0.72f, primary, secondary);

        for (int g = 0; g < gantries; g++)
        {
            float x = (g - (gantries - 1) * 0.5f) * s * 0.38f;
            TrussAddFrameBox(w, x - s * 0.04f, x + s * 0.04f, rimH, frameH * 0.84f, -padR * 0.72f, -padR * 0.62f, secondary);
            TrussAddFrameBox(w, x, x + s * 0.22f, frameH * 0.76f, frameH * 0.82f, -padR * 0.70f, -padR * 0.56f, accent);
            TrussAddFrameBox(w, x - s * 0.03f, x + s * 0.03f, frameH * 0.60f, frameH * 0.64f,
                -padR * 0.64f, -padR * 0.58f, accent);
        }

        for (int b = 0; b < bays; b++)
        {
            float z = -s * 0.28f + b * s * 0.18f;
            float bayW = s * 0.22f;
            float bayH = s * 0.16f;
            TrussAddFrameBox(w, -bayW, -bayW + s * 0.04f, rimH + s * 0.02f, rimH + bayH, z - s * 0.07f, z + s * 0.07f,
                b % 2 == 0 ? secondary : primary);
            TrussAddFrameBox(w, bayW - s * 0.04f, bayW, rimH + s * 0.02f, rimH + bayH, z - s * 0.07f, z + s * 0.07f,
                b % 2 == 0 ? secondary : primary);
            TrussAddFrameBox(w, -bayW, bayW, rimH + s * 0.02f, rimH + s * 0.05f, z - s * 0.07f, z - s * 0.03f,
                b % 2 == 0 ? primary : secondary);
            TrussAddFrameBox(w, -bayW, bayW, rimH + s * 0.02f, rimH + s * 0.05f, z + s * 0.03f, z + s * 0.07f,
                b % 2 == 0 ? primary : secondary);
            if (b % 2 == 0)
                TrussAddFrameBox(w, -bayW * 0.92f, bayW * 0.92f, rimH + bayH, rimH + bayH + s * 0.03f,
                    z - s * 0.05f, z + s * 0.05f, accent);
            TrussAddNasaPanel(w, -bayW * 0.85f, bayW * 0.85f, rimH + s * 0.05f, rimH + bayH - s * 0.02f,
                z - s * 0.05f, z + s * 0.05f, primary, secondary);
        }

        for (int crane = -1; crane <= 1; crane += 2)
        {
            float x = crane * s * 0.42f;
            TrussAddFrameBox(w, x - s * 0.04f, x + s * 0.04f, rimH, rimH + s * 0.20f, -s * 0.06f, s * 0.12f, secondary);
            TrussAddFrameBox(w, x - s * 0.02f, x + s * 0.18f, rimH + s * 0.16f, rimH + s * 0.20f, -s * 0.04f, s * 0.10f, accent);
        }

        for (int walk = 0; walk < 5; walk++)
        {
            float z = -s * 0.36f + walk * s * 0.18f;
            TrussAddFrameBox(w, -spanX, spanX, frameH * 0.66f, frameH * 0.70f, z, z + s * 0.04f, secondary);
            TrussAddFrameBox(w, -spanX * 0.44f, -spanX * 0.38f, frameH * 0.66f, frameH * 0.72f, z, z + s * 0.03f, accent);
            TrussAddFrameBox(w, spanX * 0.38f, spanX * 0.44f, frameH * 0.66f, frameH * 0.72f, z, z + s * 0.03f, accent);
        }

        for (int tower = 0; tower < 3; tower++)
        {
            float x = (tower - 1) * s * 0.34f;
            TrussAddFrameBox(w, x - s * 0.05f, x + s * 0.05f, rimH, frameH * 0.68f, -padR * 0.68f, -padR * 0.58f, secondary);
            TrussAddFrameBox(w, x - s * 0.04f, x + s * 0.20f, frameH * 0.62f, frameH * 0.66f, -padR * 0.66f, -padR * 0.52f, accent);
        }
    }

    private static void TrussAddFlushRadialArm(RaceMeshWriter w, float s, float angle, float armStart, float armEnd,
        float rimH, float armH, Vector3 body, Vector3 tipAccent)
    {
        var dir = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
        float halfLen = (armEnd - armStart) * 0.5f;
        float cx = dir.X * (armStart + armEnd) * 0.5f;
        float cz = dir.Z * (armStart + armEnd) * 0.5f;
        float perpX = -dir.Z * s * 0.07f;
        float perpZ = dir.X * s * 0.07f;
        TrussAddFrameBox(w,
            cx - dir.X * halfLen - perpX, cx + dir.X * halfLen + perpX,
            rimH, rimH + armH,
            cz - dir.Z * halfLen - perpZ, cz + dir.Z * halfLen + perpZ,
            body);
        float tx = dir.X * armEnd;
        float tz = dir.Z * armEnd;
        TrussAddFrameBox(w, tx - s * 0.05f, tx + s * 0.05f, rimH + armH * 0.60f, rimH + armH + s * 0.02f,
            tz - s * 0.05f, tz + s * 0.05f, tipAccent);
    }

    private static void TrussAddPadAccentRing(RaceMeshWriter w, float s, float padR, float y, float bandH,
        int segments, Vector3 accent)
    {
        float tangW = padR * 0.14f;
        for (int i = 0; i < segments; i++)
        {
            float a = MathF.PI * 2f * i / segments;
            float cx = MathF.Cos(a) * padR;
            float cz = MathF.Sin(a) * padR;
            TrussAddFrameBox(w, cx - tangW, cx + tangW, y, y + bandH, cz - tangW * 0.55f, cz + tangW * 0.55f, accent);
        }
    }

    private static void TrussAddCoolingRingFlush(RaceMeshWriter w, float cx, float cy, float cz, float radius, float tube,
        int segments, Vector3 color)
    {
        for (int i = 0; i < segments; i++)
        {
            float a = MathF.PI * 2f * i / segments;
            float px = cx + MathF.Cos(a) * radius;
            float pz = cz + MathF.Sin(a) * radius;
            float tangX = -MathF.Sin(a) * tube * 1.15f;
            float tangZ = MathF.Cos(a) * tube * 1.15f;
            TrussAddFrameBox(w, px - tangX, px + tangX, cy, cy + tube, pz - tangZ, pz + tangZ, color);
        }
    }

    private static void TrussAddCommsGantryArm(RaceMeshWriter w, float cx, float cy, float cz, float reach, float s,
        Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        TrussAddFrameBox(w, cx - s * 0.04f, cx + s * 0.04f, cy, cy + s * 0.14f, cz - s * 0.04f, cz + s * 0.04f, secondary);
        TrussAddFrameBox(w, cx - reach * 0.50f, cx + reach * 0.50f, cy + s * 0.10f, cy + s * 0.14f,
            cz - reach * 0.22f, cz + reach * 0.08f, primary);
        TrussAddNasaPanel(w, cx - reach * 0.42f, cx + reach * 0.42f, cy + s * 0.06f, cy + s * 0.10f,
            cz - reach * 0.18f, cz + reach * 0.06f, primary, secondary);
        TrussAddFrameBox(w, cx - s * 0.025f, cx + s * 0.025f, cy + s * 0.12f, cy + s * 0.16f,
            cz + reach * 0.06f, cz + reach * 0.10f, accent);
    }

    private static void BuildTrussCommandCenter(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.18f;
        float innerR = padR * 0.58f;
        float rimH = s * 0.06f;
        float hubScale = s * 0.72f;

        TrussAddSkeletalPad(w, s, padR, innerR, rimH, primary, secondary, accent);
        TrussAddPadAccentRing(w, s, padR * 0.97f, rimH * 0.82f, s * 0.022f, 16, accent);

        TrussAddNasaPanel(w, -hubScale * 0.24f, hubScale * 0.24f, rimH, rimH + s * 0.10f,
            -hubScale * 0.24f, hubScale * 0.24f, primary, secondary);
        TrussAddFrameBox(w, -hubScale * 0.18f, hubScale * 0.18f, rimH + s * 0.10f, rimH + s * 0.18f,
            -hubScale * 0.18f, hubScale * 0.18f, secondary);
        TrussAddFrameBox(w, -hubScale * 0.12f, hubScale * 0.12f, rimH + s * 0.18f, rimH + s * 0.22f,
            -hubScale * 0.12f, hubScale * 0.12f, accent);

        for (int ring = 0; ring < 4; ring++)
        {
            float a = MathF.PI * 0.5f * ring + MathF.PI * 0.125f;
            float dx = MathF.Cos(a) * s * 0.48f;
            float dz = MathF.Sin(a) * s * 0.48f;
            TrussAddFrameBox(w, dx - s * 0.05f, dx + s * 0.05f, rimH + s * 0.02f, rimH + s * 0.12f,
                dz - s * 0.04f, dz + s * 0.04f, secondary);
            TrussAddFrameBox(w, dx - s * 0.04f, dx + s * 0.04f, rimH + s * 0.12f, rimH + s * 0.14f,
                dz - s * 0.03f, dz + s * 0.03f, accent);
        }

        for (int arm = 0; arm < 4; arm++)
        {
            float angle = MathF.PI * 0.5f * arm + MathF.PI * 0.18f;
            TrussAddFlushRadialArm(w, s, angle, s * 0.34f, padR * 0.94f, rimH, s * 0.06f, secondary, accent);
            float ax = MathF.Cos(angle) * padR * 0.82f;
            float az = MathF.Sin(angle) * padR * 0.82f;
            TrussAddCommsGantryArm(w, ax, rimH + s * 0.04f, az, s * 0.12f, s, primary, secondary, accent);
        }

        TrussAddFrameBox(w, -s * 0.03f, s * 0.03f, rimH + s * 0.22f, rimH + s * 0.38f, -s * 0.03f, s * 0.03f, secondary);
        TrussAddFrameBox(w, -s * 0.04f, s * 0.04f, rimH + s * 0.38f, rimH + s * 0.44f, -s * 0.04f, s * 0.04f, accent);

        for (int pod = 0; pod < 4; pod++)
        {
            float angle = MathF.PI * 0.25f + MathF.PI * 0.5f * pod;
            float px = MathF.Cos(angle) * s * 0.64f;
            float pz = MathF.Sin(angle) * s * 0.64f;
            TrussAddNasaPanel(w, px - s * 0.06f, px + s * 0.06f, rimH, rimH + s * 0.07f, pz - s * 0.06f, pz + s * 0.06f, primary, secondary);
        }
    }

    private static void BuildTrussDefenseTurret(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 0.90f;
        float innerR = padR * 0.52f;
        float rimH = s * 0.06f;
        float raisedH = s * 0.08f;

        TrussAddSkeletalPad(w, s, padR, innerR, rimH, primary, secondary, accent);
        TrussAddFrameBox(w, -innerR * 0.84f, innerR * 0.84f, rimH, rimH + raisedH, -innerR * 0.84f, innerR * 0.84f, secondary);
        TrussAddPadAccentRing(w, s, innerR * 0.92f, rimH + raisedH * 0.65f, s * 0.018f, 10, accent);

        float ringY = rimH + raisedH + s * 0.02f;
        float ringR = s * 0.15f;
        for (int i = 0; i < 8; i++)
        {
            float a = MathF.PI * 2f * i / 8f;
            float cx = MathF.Cos(a) * ringR;
            float cz = MathF.Sin(a) * ringR;
            TrussAddFrameBox(w, cx - s * 0.025f, cx + s * 0.025f, ringY, ringY + s * 0.05f, cz - s * 0.025f, cz + s * 0.025f, secondary);
            if (i % 2 == 0)
                TrussAddFrameBox(w, cx - s * 0.015f, cx + s * 0.015f, ringY + s * 0.05f, ringY + s * 0.07f,
                    cz - s * 0.015f, cz + s * 0.015f, accent);
        }

        for (int barrel = 0; barrel < 4; barrel++)
        {
            float angle = MathF.PI * 0.5f * barrel + MathF.PI * 0.25f;
            var dir = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
            float bx = dir.X * s * 0.24f;
            float bz = dir.Z * s * 0.24f;
            float perpX = -dir.Z * s * 0.020f;
            float perpZ = dir.X * s * 0.020f;
            TrussAddFrameBox(w, bx - perpX, bx + perpX, ringY + s * 0.05f, ringY + s * 0.08f,
                bz - s * 0.06f - perpZ, bz + s * 0.03f + perpZ, primary);
            TrussAddFrameBox(w, bx - perpX, bx + perpX * 2f, ringY + s * 0.07f, ringY + s * 0.09f,
                bz + s * 0.02f, bz + s * 0.10f, accent);
        }

        for (int post = 0; post < 4; post++)
        {
            float angle = MathF.PI * 0.5f * post;
            float px = MathF.Cos(angle) * padR * 0.68f;
            float pz = MathF.Sin(angle) * padR * 0.68f;
            TrussAddFrameBox(w, px - s * 0.04f, px + s * 0.04f, rimH, rimH + s * 0.10f, pz - s * 0.04f, pz + s * 0.04f, secondary);
        }
    }

    private static void BuildTrussSensorArray(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.10f;
        float innerR = padR * 0.60f;
        float rimH = s * 0.06f;

        TrussAddSkeletalPad(w, s, padR, innerR, rimH, primary, secondary, accent);
        TrussAddNasaPanel(w, -s * 0.20f, s * 0.20f, rimH, rimH + s * 0.08f, -s * 0.20f, s * 0.20f, primary, secondary);
        TrussAddFrameBox(w, -s * 0.14f, s * 0.14f, rimH + s * 0.08f, rimH + s * 0.12f, -s * 0.14f, s * 0.14f, accent);

        for (int seg = 0; seg < 8; seg++)
        {
            float a = MathF.PI * 2f * seg / 8f;
            float cx = MathF.Cos(a) * s * 0.34f;
            float cz = MathF.Sin(a) * s * 0.34f;
            TrussAddFrameBox(w, cx - s * 0.04f, cx + s * 0.04f, rimH + s * 0.02f, rimH + s * 0.06f, cz - s * 0.04f, cz + s * 0.04f, secondary);
        }

        for (int wing = -1; wing <= 1; wing += 2)
        {
            float x = wing * s * 0.52f;
            TrussAddFrameBox(w, x - s * 0.03f, x + s * 0.03f, rimH + s * 0.04f, rimH + s * 0.10f, -s * 0.06f, s * 0.06f, secondary);
            TrussAddNasaPanel(w, x - s * 0.22f, x + s * 0.22f, rimH + s * 0.06f, rimH + s * 0.10f, -s * 0.04f, s * 0.04f, primary, secondary);
            TrussAddFrameBox(w, x - s * 0.20f, x + s * 0.20f, rimH + s * 0.10f, rimH + s * 0.12f, -s * 0.03f, s * 0.03f, accent);
        }

        TrussAddCoolingRingFlush(w, 0, rimH + s * 0.05f, 0, s * 0.36f, s * 0.022f, 12, primary);
    }

    private static void BuildTrussRefinery(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.12f;
        float innerR = padR * 0.58f;
        float rimH = s * 0.06f;

        TrussAddSkeletalPad(w, s, padR, innerR, rimH, primary, secondary, accent);
        TrussAddNasaPanel(w, -s * 0.18f, s * 0.18f, rimH, rimH + s * 0.06f, -s * 0.18f, s * 0.18f, primary, secondary);

        ReadOnlySpan<float> tankX = stackalloc float[] { -0.22f, 0.18f, 0.0f };
        ReadOnlySpan<float> tankZ = stackalloc float[] { 0.12f, -0.10f, 0.26f };
        ReadOnlySpan<float> tankH = stackalloc float[] { 0.16f, 0.18f, 0.14f };
        for (int t = 0; t < 3; t++)
        {
            float x = tankX[t] * s;
            float z = tankZ[t] * s;
            float h = tankH[t] * s;
            float halfW = s * 0.10f;
            TrussAddFrameBox(w, x - halfW, x + halfW, rimH, rimH + h, z - halfW * 0.70f, z + halfW * 0.70f, secondary);
            TrussAddFrameBox(w, x - halfW + s * 0.02f, x + halfW - s * 0.02f, rimH + s * 0.02f, rimH + h - s * 0.02f,
                z - halfW * 0.55f, z + halfW * 0.55f, primary);
            TrussAddFrameBox(w, x - halfW * 0.75f, x + halfW * 0.75f, rimH + h, rimH + h + s * 0.02f,
                z - halfW * 0.55f, z + halfW * 0.55f, accent);
        }

        for (int intake = 0; intake < 2; intake++)
        {
            float side = intake == 0 ? -1f : 1f;
            float x = side * s * 0.34f;
            float h = s * 0.18f;
            TrussAddFrameBox(w, x - s * 0.03f, x + s * 0.03f, rimH, rimH + h, -s * 0.06f, s * 0.06f, secondary);
            TrussAddGantryBoom(w, x, side > 0 ? 1 : -1, rimH + h * 0.60f, s * 0.14f, s * 0.06f, s, 0.5f, primary, secondary, accent);
        }
    }

    private static void BuildTrussRepairBay(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.10f;
        float innerR = padR * 0.60f;
        float rimH = s * 0.06f;
        float shellH = s * 0.16f;

        TrussAddSkeletalPad(w, s, padR, innerR, rimH, primary, secondary, accent);
        TrussAddFrameBox(w, -s * 0.30f, s * 0.30f, rimH, rimH + shellH, -s * 0.18f, s * 0.10f, secondary);
        TrussAddNasaPanel(w, -s * 0.26f, s * 0.26f, rimH + shellH, rimH + shellH + s * 0.02f, -s * 0.14f, s * 0.06f, primary, secondary);

        for (int slot = 0; slot < 3; slot++)
        {
            float x = -s * 0.18f + slot * s * 0.18f;
            TrussAddFrameBox(w, x - s * 0.07f, x + s * 0.07f, rimH, rimH + s * 0.05f, -s * 0.22f, -s * 0.12f, primary);
            TrussAddFrameBox(w, x - s * 0.06f, x + s * 0.06f, rimH + s * 0.05f, rimH + shellH * 0.78f, -s * 0.26f, -s * 0.24f, secondary);
            TrussAddFrameBox(w, x - s * 0.05f, x + s * 0.05f, rimH + shellH * 0.74f, rimH + shellH * 0.78f, -s * 0.25f, -s * 0.23f, accent);
        }

        float craneY = rimH + shellH * 0.66f;
        TrussAddFrameBox(w, -s * 0.28f, s * 0.28f, craneY, craneY + s * 0.04f, -s * 0.05f, s * 0.05f, secondary);
        TrussAddFrameBox(w, -s * 0.26f, s * 0.26f, craneY + s * 0.04f, craneY + s * 0.06f, -s * 0.03f, s * 0.03f, accent);
        TrussAddGantryBoom(w, 0, 1, craneY, s * 0.22f, s * 0.08f, s, 0.85f, primary, secondary, accent);

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * s * 0.36f;
            TrussAddFrameBox(w, x - s * 0.03f, x + s * 0.03f, rimH, rimH + shellH * 0.58f, s * 0.04f, s * 0.10f, secondary);
            TrussAddFrameBox(w, x - s * 0.02f, x + s * 0.12f, rimH + shellH * 0.52f, rimH + shellH * 0.56f, s * 0.02f, s * 0.08f, accent);
        }
    }

    private static void BuildTrussReactor(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        Vector3 engine = accent;
        float padR = s * 1.08f;
        float innerR = padR * 0.62f;
        float rimH = s * 0.06f;

        TrussAddSkeletalPad(w, s, padR, innerR, rimH, primary, secondary, accent);
        TrussAddNasaPanel(w, -s * 0.22f, s * 0.22f, rimH, rimH + s * 0.06f, -s * 0.22f, s * 0.22f, primary, secondary);

        float cageR = s * 0.14f;
        for (int i = 0; i < 8; i++)
        {
            float a = MathF.PI * 2f * i / 8f;
            float cx = MathF.Cos(a) * cageR;
            float cz = MathF.Sin(a) * cageR;
            TrussAddFrameBox(w, cx - s * 0.025f, cx + s * 0.025f, rimH + s * 0.02f, rimH + s * 0.14f, cz - s * 0.025f, cz + s * 0.025f, secondary);
        }

        TrussAddFrameBox(w, -cageR * 0.55f, cageR * 0.55f, rimH + s * 0.06f, rimH + s * 0.10f, -cageR * 0.55f, cageR * 0.55f, engine);
        TrussAddFrameBox(w, -cageR * 0.40f, cageR * 0.40f, rimH + s * 0.08f, rimH + s * 0.12f, -cageR * 0.40f, cageR * 0.40f, engine * 1.08f);
        TrussAddFrameBox(w, -cageR * 0.65f, cageR * 0.65f, rimH + s * 0.12f, rimH + s * 0.14f, -cageR * 0.65f, cageR * 0.65f, accent);

        TrussAddCoolingRingFlush(w, 0, rimH + s * 0.06f, 0, s * 0.40f, s * 0.025f, 12, secondary);
        TrussAddCoolingRingFlush(w, 0, rimH + s * 0.08f, 0, s * 0.28f, s * 0.020f, 10, primary);

        for (int c = 0; c < 4; c++)
        {
            float angle = MathF.PI * 0.25f + MathF.PI * 0.5f * c;
            float cx = MathF.Cos(angle) * s * 0.44f;
            float cz = MathF.Sin(angle) * s * 0.44f;
            TrussAddFrameBox(w, cx - s * 0.04f, cx + s * 0.04f, rimH, rimH + s * 0.08f, cz - s * 0.04f, cz + s * 0.04f, secondary);
            TrussAddFrameBox(w, cx - s * 0.03f, cx + s * 0.03f, rimH + s * 0.08f, rimH + s * 0.10f, cz - s * 0.03f, cz + s * 0.03f, accent);
        }
    }

    private static void BuildTrussSupplyDepot(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.04f;
        float innerR = padR * 0.56f;
        float rimH = s * 0.06f;

        TrussAddSkeletalPad(w, s, padR, innerR, rimH, primary, secondary, accent);
        TrussAddFrameBox(w, -s * 0.14f, s * 0.14f, rimH, rimH + s * 0.12f, -s * 0.10f, s * 0.10f, secondary);
        TrussAddNasaPanel(w, -s * 0.12f, s * 0.12f, rimH + s * 0.12f, rimH + s * 0.14f, -s * 0.08f, s * 0.08f, primary, secondary);
        TrussAddFrameBox(w, -s * 0.08f, s * 0.08f, rimH + s * 0.05f, rimH + s * 0.09f, -s * 0.05f, s * 0.05f, primary);

        for (int ext = 0; ext < 2; ext++)
        {
            float side = ext == 0 ? -1f : 1f;
            float padStart = padR * 0.50f;
            float padEnd = padR * 0.92f;
            float cx = side * (padStart + padEnd) * 0.5f;
            float halfLen = (padEnd - padStart) * 0.5f;
            TrussAddNasaPanel(w, cx - halfLen, cx + halfLen, rimH, rimH + s * 0.04f, -s * 0.10f, s * 0.10f, primary, secondary);
            TrussAddPadAccentRing(w, s, MathF.Abs(cx) + halfLen * 0.4f, rimH + s * 0.03f, s * 0.016f, 6, accent);

            for (int cargo = 0; cargo < 2; cargo++)
            {
                float z = (cargo == 0 ? -1f : 1f) * s * 0.05f;
                float h = s * (cargo == 0 ? 0.12f : 0.14f);
                float halfW = s * 0.07f;
                TrussAddFrameBox(w, cx - halfW, cx + halfW, rimH + s * 0.04f, rimH + s * 0.04f + h,
                    z - halfW * 0.75f, z + halfW * 0.75f, secondary);
                TrussAddNasaPanel(w, cx - halfW * 0.80f, cx + halfW * 0.80f, rimH + s * 0.04f + h, rimH + s * 0.04f + h + s * 0.02f,
                    z - halfW * 0.55f, z + halfW * 0.55f, primary, secondary);
            }
        }

        for (int corner = 0; corner < 4; corner++)
        {
            float a = MathF.PI * 0.5f * corner + MathF.PI * 0.25f;
            float px = MathF.Cos(a) * padR * 0.76f;
            float pz = MathF.Sin(a) * padR * 0.76f;
            TrussAddFrameBox(w, px - s * 0.04f, px + s * 0.04f, rimH, rimH + s * 0.06f, pz - s * 0.04f, pz + s * 0.04f, secondary);
            TrussAddFrameBox(w, px - s * 0.03f, px + s * 0.03f, rimH + s * 0.04f, rimH + s * 0.06f, pz - s * 0.03f, pz + s * 0.03f, accent);
        }
    }

    // ── Terran (retro) industrial station geometry ──────────────────────────

    private static void RetroAddBox(RaceMeshWriter w, float x0, float x1, float y0, float y1, float z0, float z1,
        Vector3 side, Vector3 top)
    {
        var bl0 = new Vector3(x0, y0, z0); var br0 = new Vector3(x1, y0, z0);
        var tr0 = new Vector3(x1, y1, z0); var tl0 = new Vector3(x0, y1, z0);
        var bl1 = new Vector3(x0, y0, z1); var br1 = new Vector3(x1, y0, z1);
        var tr1 = new Vector3(x1, y1, z1); var tl1 = new Vector3(x0, y1, z1);
        w.TriColored(bl0, br0, tr0, side); w.TriColored(bl0, tr0, tl0, side);
        w.TriColored(bl1, tr1, br1, side); w.TriColored(bl1, tl1, tr1, side);
        w.TriColored(bl0, tl0, tl1, top); w.TriColored(bl0, tl1, bl1, top);
        w.TriColored(br0, br1, tr1, top); w.TriColored(br0, tr1, tr0, top);
        w.TriColored(bl0, bl1, br1, side); w.TriColored(bl0, br1, br0, side);
        w.TriColored(tr0, tr1, tl1, side); w.TriColored(tr0, tl1, tl0, side);
    }

    /// <summary>Flush box with uniform luminance — avoids facet-seam gradients on coplanar pad/deck zones.</summary>
    private static void RetroAddBoxUniform(RaceMeshWriter w, float x0, float x1, float y0, float y1, float z0, float z1,
        Vector3 color)
    {
        RetroAddBox(w, x0, x1, y0, y1, z0, z1, color, color);
    }

    /// <summary>Flush radial service arm on pad deck — box-only, no pyramid facets.</summary>
    private static void RetroAddFlushRadialArm(RaceMeshWriter w, float s, float angle, float armStart, float armEnd,
        float rimH, float armH, Vector3 body, Vector3 tipAccent)
    {
        var dir = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
        float halfLen = (armEnd - armStart) * 0.5f;
        float cx = dir.X * (armStart + armEnd) * 0.5f;
        float cz = dir.Z * (armStart + armEnd) * 0.5f;
        float perpX = -dir.Z * s * 0.07f;
        float perpZ = dir.X * s * 0.07f;
        RetroAddBoxUniform(w,
            cx - dir.X * halfLen - perpX, cx + dir.X * halfLen + perpX,
            rimH, rimH + armH,
            cz - dir.Z * halfLen - perpZ, cz + dir.Z * halfLen + perpZ,
            body);
        float tx = dir.X * armEnd;
        float tz = dir.Z * armEnd;
        RetroAddBoxUniform(w, tx - s * 0.05f, tx + s * 0.05f, rimH + armH * 0.65f, rimH + armH + s * 0.02f,
            tz - s * 0.05f, tz + s * 0.05f, tipAccent);
    }

    /// <summary>Wide industrial pad — central deck plate + radial rim boxes (no facet wedges).</summary>
    private static void RetroAddIndustrialPad(RaceMeshWriter w, float s, float padR, float innerR, float rimH,
        Vector3 primary, Vector3 secondary)
    {
        RetroAddBoxUniform(w, -innerR, innerR, 0, rimH, -innerR, innerR, primary);

        int segs = 8;
        float tangW = padR * 0.26f;
        float radialW = (padR - innerR) * 0.48f;
        for (int i = 0; i < segs; i++)
        {
            float a = MathF.PI * 2f * i / segs;
            float midR = (padR + innerR) * 0.5f;
            float cx = MathF.Cos(a) * midR;
            float cz = MathF.Sin(a) * midR;
            RetroAddBoxUniform(w, cx - tangW, cx + tangW, 0, rimH * 0.90f, cz - radialW, cz + radialW, primary);
        }

        for (int cardinal = 0; cardinal < 4; cardinal++)
        {
            float a = MathF.PI * 0.5f * cardinal;
            float cx = MathF.Cos(a) * padR * 0.94f;
            float cz = MathF.Sin(a) * padR * 0.94f;
            float tangX = MathF.Cos(a) * padR * 0.10f;
            float tangZ = MathF.Sin(a) * padR * 0.10f;
            RetroAddBoxUniform(w, cx - tangX, cx + tangX, 0, rimH * 0.85f, cz - tangZ, cz + tangZ, secondary);
        }
    }

    /// <summary>Orange accent band on pad rim edge for RaceIdentity recovery.</summary>
    private static void RetroAddPadAccentRing(RaceMeshWriter w, float s, float padR, float y, float bandH,
        int segments, Vector3 accent)
    {
        float tangW = padR * 0.14f;
        for (int i = 0; i < segments; i++)
        {
            float a = MathF.PI * 2f * i / segments;
            float cx = MathF.Cos(a) * padR;
            float cz = MathF.Sin(a) * padR;
            RetroAddBox(w, cx - tangW, cx + tangW, y, y + bandH, cz - tangW * 0.55f, cz + tangW * 0.55f, accent, accent);
        }
    }

    /// <summary>Low-profile clustered box superstructure — hub tiers + corner bay nubs.</summary>
    private static void RetroAddDeckCluster(RaceMeshWriter w, float cx, float cy, float cz, float scale,
        Vector3 primary, Vector3 secondary, Vector3 accent, int tiers = 3)
    {
        RetroAddBox(w, cx - scale * 0.24f, cx + scale * 0.24f, cy, cy + scale * 0.10f, cz - scale * 0.24f, cz + scale * 0.24f,
            secondary, primary);
        RetroAddBox(w, cx - scale * 0.18f, cx + scale * 0.18f, cy + scale * 0.10f, cy + scale * 0.20f, cz - scale * 0.18f, cz + scale * 0.18f,
            primary, secondary);
        if (tiers >= 3)
        {
            RetroAddBox(w, cx - scale * 0.12f, cx + scale * 0.12f, cy + scale * 0.20f, cy + scale * 0.26f, cz - scale * 0.12f, cz + scale * 0.12f,
                secondary, primary);
            RetroAddBox(w, cx - scale * 0.14f, cx + scale * 0.14f, cy + scale * 0.26f, cy + scale * 0.28f, cz - scale * 0.14f, cz + scale * 0.14f,
                accent, accent);
        }

        for (int bay = 0; bay < 4; bay++)
        {
            float a = MathF.PI * 0.5f * bay + MathF.PI * 0.25f;
            float bx = cx + MathF.Cos(a) * scale * 0.28f;
            float bz = cz + MathF.Sin(a) * scale * 0.28f;
            RetroAddBox(w, bx - scale * 0.08f, bx + scale * 0.08f, cy, cy + scale * 0.08f, bz - scale * 0.08f, bz + scale * 0.08f,
                primary, secondary);
        }
    }

    /// <summary>Flush low-profile comms dish on deck — readable from plan view, not vertical landmark.</summary>
    private static void RetroAddCommsDishLow(RaceMeshWriter w, float cx, float cy, float cz, float reach, float s,
        Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        RetroAddBox(w, cx - s * 0.04f, cx + s * 0.04f, cy, cy + s * 0.06f, cz - s * 0.04f, cz + s * 0.04f, secondary, primary);
        RetroAddBox(w, cx - reach * 0.55f, cx + reach * 0.55f, cy + s * 0.06f, cy + s * 0.09f, cz - reach * 0.30f, cz + reach * 0.12f,
            primary, primary);
        RetroAddBox(w, cx - s * 0.025f, cx + s * 0.025f, cy + s * 0.09f, cy + s * 0.11f, cz + reach * 0.10f, cz + reach * 0.16f,
            accent, accent);
    }

    private static void RetroAddFacetedColumn(RaceMeshWriter w, float cx, float cz, float y0, float y1,
        float r0, float r1, int facets, Vector3 colA, Vector3 colB)
    {
        for (int i = 0; i < facets; i++)
        {
            float a0 = MathF.PI * 2f * i / facets;
            float a1 = MathF.PI * 2f * (i + 1) / facets;
            var b0 = new Vector3(cx + MathF.Cos(a0) * r0, y0, cz + MathF.Sin(a0) * r0);
            var b1 = new Vector3(cx + MathF.Cos(a1) * r0, y0, cz + MathF.Sin(a1) * r0);
            var t0 = new Vector3(cx + MathF.Cos(a0) * r1, y1, cz + MathF.Sin(a0) * r1);
            var t1 = new Vector3(cx + MathF.Cos(a1) * r1, y1, cz + MathF.Sin(a1) * r1);
            w.TriColored(b0, b1, t0, i % 2 == 0 ? colA : colB);
            w.TriColored(b1, t1, t0, i % 2 == 0 ? colB * 0.94f : colA * 0.92f);
        }
    }

    private static void RetroAddDish(RaceMeshWriter w, float cx, float cy, float cz, float reach, float depth,
        Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        for (int f = 0; f < 5; f++)
        {
            float t0 = f / 5f;
            float t1 = (f + 1) / 5f;
            float z0 = cz - depth * 0.5f + depth * t0;
            float z1 = cz - depth * 0.5f + depth * t1;
            float r0 = reach * (0.35f + t0 * 0.65f);
            float r1 = reach * (0.35f + t1 * 0.65f);
            w.TriColored(new Vector3(cx - r0, cy, z0), new Vector3(cx + r0, cy, z0), new Vector3(cx, cy + reach * 0.12f, z1),
                f % 2 == 0 ? primary * 0.92f : secondary * 0.90f);
            w.TriColored(new Vector3(cx + r0, cy, z0), new Vector3(cx + r1 * 0.6f, cy + reach * 0.08f, z1),
                new Vector3(cx, cy + reach * 0.12f, z1), secondary * 0.88f);
        }
        w.TriColored(new Vector3(cx - reach * 0.08f, cy + reach * 0.14f, cz), new Vector3(cx + reach * 0.08f, cy + reach * 0.14f, cz),
            new Vector3(cx, cy + reach * 0.20f, cz + depth * 0.3f), accent);
    }

    private static void RetroAddCoolingRing(RaceMeshWriter w, float cx, float cy, float cz, float radius, float tube,
        int segments, Vector3 outer, Vector3 inner)
    {
        for (int i = 0; i < segments; i++)
        {
            float a0 = MathF.PI * 2f * i / segments;
            float a1 = MathF.PI * 2f * (i + 1) / segments;
            var o0 = new Vector3(cx + MathF.Cos(a0) * radius, cy, cz + MathF.Sin(a0) * radius);
            var o1 = new Vector3(cx + MathF.Cos(a1) * radius, cy, cz + MathF.Sin(a1) * radius);
            var i0 = new Vector3(cx + MathF.Cos(a0) * (radius - tube), cy + tube * 0.4f, cz + MathF.Sin(a0) * (radius - tube));
            var i1 = new Vector3(cx + MathF.Cos(a1) * (radius - tube), cy + tube * 0.4f, cz + MathF.Sin(a1) * (radius - tube));
            w.TriColored(o0, o1, i0, i % 2 == 0 ? outer : inner);
            w.TriColored(o1, i1, i0, i % 2 == 0 ? inner * 0.94f : outer * 0.92f);
        }
    }

    /// <summary>Flush box coolant ring on deck — uniform luminance, no tri-facet tube strips.</summary>
    private static void RetroAddCoolingRingFlush(RaceMeshWriter w, float cx, float cy, float cz, float radius, float tube,
        int segments, Vector3 color)
    {
        for (int i = 0; i < segments; i++)
        {
            float a = MathF.PI * 2f * i / segments;
            float px = cx + MathF.Cos(a) * radius;
            float pz = cz + MathF.Sin(a) * radius;
            float tangX = -MathF.Sin(a) * tube * 1.15f;
            float tangZ = MathF.Cos(a) * tube * 1.15f;
            RetroAddBoxUniform(w, px - tangX, px + tangX, cy, cy + tube, pz - tangZ, pz + tangZ, color);
        }
    }

    private static void BuildRetroCommandCenter(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.20f;
        float innerR = s * 0.80f;
        float rimH = s * 0.06f;
        float hubScale = s * 0.82f;

        RetroAddIndustrialPad(w, s, padR, innerR, rimH, primary, secondary);
        RetroAddPadAccentRing(w, s, padR * 0.98f, rimH * 0.82f, s * 0.025f, 16, accent);

        RetroAddDeckCluster(w, 0, rimH, 0, hubScale, primary, secondary, accent, tiers: 3);
        RetroAddBoxUniform(w, -hubScale * 0.22f, hubScale * 0.22f, rimH + hubScale * 0.24f, rimH + hubScale * 0.27f,
            -hubScale * 0.22f, hubScale * 0.22f, accent);

        for (int d = 0; d < 4; d++)
        {
            float a = MathF.PI * 0.5f * d;
            float dx = MathF.Cos(a) * s * 0.54f;
            float dz = MathF.Sin(a) * s * 0.54f;
            RetroAddCommsDishLow(w, dx, rimH + s * 0.02f, dz, s * 0.14f, s, primary, secondary, accent);
        }

        float mastBase = rimH + hubScale * 0.26f;
        RetroAddBoxUniform(w, -s * 0.035f, s * 0.035f, mastBase, mastBase + s * 0.16f, -s * 0.035f, s * 0.035f, secondary);
        RetroAddBoxUniform(w, -s * 0.05f, s * 0.05f, mastBase + s * 0.16f, mastBase + s * 0.22f, -s * 0.05f, s * 0.05f, accent);

        for (int arm = 0; arm < 4; arm++)
        {
            float angle = MathF.PI * 0.5f * arm;
            RetroAddFlushRadialArm(w, s, angle, s * 0.36f, padR * 0.96f, rimH, s * 0.05f, secondary, accent);
        }

        for (int pod = 0; pod < 4; pod++)
        {
            float angle = MathF.PI * 0.25f + MathF.PI * 0.5f * pod;
            float px = MathF.Cos(angle) * s * 0.68f;
            float pz = MathF.Sin(angle) * s * 0.68f;
            RetroAddBoxUniform(w, px - s * 0.07f, px + s * 0.07f, rimH, rimH + s * 0.08f, pz - s * 0.07f, pz + s * 0.07f, primary);
        }
    }

    private static void BuildRetroShipyard(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s, int gantries, int bays)
    {
        bool isLarge = bays >= 5;
        bool isSmall = bays <= 2 && gantries <= 2;
        float padR = s * (isLarge ? 1.28f : bays >= 3 ? 1.14f : 1.08f);
        float innerR = padR * 0.62f;
        float rimH = s * 0.06f;
        float frameH = s * (isLarge ? 0.62f : isSmall ? 0.52f : 0.58f);
        float spanX = s * (isLarge ? 0.92f : isSmall ? 0.72f : 0.82f);

        RetroAddIndustrialPad(w, s, padR, innerR, rimH, primary, secondary);
        RetroAddPadAccentRing(w, s, padR * 0.97f, rimH * 0.80f, s * 0.03f, isLarge ? 20 : 14, accent);

        RetroAddDeckCluster(w, 0, rimH, 0, s * (isLarge ? 0.82f : isSmall ? 0.58f : 0.68f), primary, secondary, accent, isLarge ? 4 : isSmall ? 2 : 3);
        RetroAddBox(w, -s * 0.14f, s * 0.14f, rimH, rimH + s * 0.04f, -s * 0.46f, s * 0.46f, secondary, primary);

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * spanX;
            RetroAddBox(w, x - side * s * 0.06f, x, rimH, frameH, -padR * 0.74f, padR * 0.74f, accent, secondary);
            for (int tier = 0; tier < gantries + 1; tier++)
            {
                float t = tier / MathF.Max(1f, gantries);
                float y = rimH + s * 0.06f + tier * (frameH - rimH - s * 0.10f) / MathF.Max(1, gantries);
                float reach = spanX * (isLarge ? 0.78f + t * 0.18f : 0.88f);
                float depth = padR * (isLarge ? 0.62f + t * 0.14f : 0.74f);
                RetroAddBox(w, x - side * s * (0.10f + t * 0.04f), x - side * s * 0.02f, y, y + s * 0.04f,
                    -reach, reach, primary, secondary);
                if (isLarge && tier == gantries)
                {
                    RetroAddBoxUniform(w, x - side * s * 0.12f, x - side * s * 0.02f, y + s * 0.04f, y + s * 0.06f,
                        -depth, depth, accent);
                }
            }
        }

        RetroAddBoxUniform(w, -spanX, spanX, frameH - s * 0.04f, frameH, -padR * 0.76f, padR * 0.76f, accent);
        RetroAddBox(w, -spanX * 0.94f, spanX * 0.94f, frameH - s * 0.08f, frameH - s * 0.04f, -padR * 0.70f, padR * 0.70f,
            secondary, primary);

        for (int g = 0; g < gantries; g++)
        {
            float x = (g - (gantries - 1) * 0.5f) * s * (isLarge ? 0.38f : 0.46f);
            RetroAddBox(w, x - s * 0.04f, x + s * 0.04f, rimH, frameH * 0.86f, -padR * 0.70f, -padR * 0.64f, secondary, accent);
            RetroAddBoxUniform(w, x, x + s * 0.22f, frameH * 0.78f, frameH * 0.84f, -padR * 0.68f, -padR * 0.54f, accent);
            if (isLarge)
            {
                RetroAddBoxUniform(w, x - s * 0.03f, x + s * 0.03f, frameH * 0.62f, frameH * 0.66f,
                    -padR * 0.62f, -padR * 0.58f, accent);
            }
        }

        for (int b = 0; b < bays; b++)
        {
            float z = -s * 0.26f + b * s * (isLarge ? 0.18f : 0.22f);
            float bayW = s * (isLarge ? 0.24f : 0.18f);
            RetroAddBoxUniform(w, -bayW, bayW, rimH + s * 0.02f, rimH + s * 0.14f, z - s * 0.08f, z + s * 0.08f,
                b % 2 == 0 ? primary : secondary);
            if (b % 2 == 0)
            {
                RetroAddBoxUniform(w, -bayW * 0.9f, bayW * 0.9f, rimH + s * 0.14f, rimH + s * 0.17f, z - s * 0.06f, z + s * 0.06f,
                    accent);
            }
            if (isLarge || !isSmall)
            {
                float roofW = bayW * (isLarge ? 0.75f : 0.68f);
                float roofH = isLarge ? s * 0.20f : s * 0.16f;
                RetroAddBoxUniform(w, -roofW, roofW, rimH + s * 0.14f, rimH + roofH, z - s * 0.05f, z + s * 0.05f,
                    b % 2 == 0 ? secondary : primary);
                if (!isLarge)
                {
                    RetroAddBoxUniform(w, -roofW * 0.85f, roofW * 0.85f, rimH + roofH - s * 0.02f, rimH + roofH,
                        z - s * 0.04f, z + s * 0.04f, accent);
                }
            }
        }

        if (isSmall)
        {
            for (int arm = -1; arm <= 1; arm += 2)
            {
                float x = arm * s * 0.34f;
                RetroAddBoxUniform(w, x - s * 0.04f, x + s * 0.04f, rimH, rimH + s * 0.14f, s * 0.18f, s * 0.36f, secondary);
                RetroAddBoxUniform(w, x - s * 0.03f, x + s * 0.03f, rimH + s * 0.12f, rimH + s * 0.16f, s * 0.30f, s * 0.34f, accent);
                RetroAddBoxUniform(w, x - s * 0.05f, x + s * 0.22f, rimH + s * 0.10f, rimH + s * 0.13f, s * 0.24f, s * 0.30f, primary);
            }
            RetroAddBoxUniform(w, -s * 0.10f, s * 0.10f, rimH + s * 0.08f, rimH + s * 0.11f, -s * 0.04f, s * 0.04f, accent);
        }
        else if (!isLarge)
        {
            for (int crane = -1; crane <= 1; crane += 2)
            {
                float x = crane * s * 0.40f;
                RetroAddBoxUniform(w, x - s * 0.04f, x + s * 0.04f, rimH, rimH + s * 0.22f, -s * 0.08f, s * 0.14f, secondary);
                RetroAddBoxUniform(w, x - s * 0.02f, x + s * 0.18f, rimH + s * 0.18f, rimH + s * 0.22f, -s * 0.06f, s * 0.10f, accent);
            }
            RetroAddBoxUniform(w, -spanX * 0.88f, spanX * 0.88f, frameH - s * 0.06f, frameH - s * 0.02f, -s * 0.08f, s * 0.08f, accent);
        }

        if (isLarge)
        {
            for (int walk = 0; walk < 5; walk++)
            {
                float z = -s * 0.34f + walk * s * 0.17f;
                RetroAddBoxUniform(w, -spanX, spanX, frameH * 0.68f, frameH * 0.72f, z, z + s * 0.04f, secondary);
                RetroAddBoxUniform(w, -spanX * 0.42f, -spanX * 0.38f, frameH * 0.68f, frameH * 0.74f, z, z + s * 0.03f, accent);
                RetroAddBoxUniform(w, spanX * 0.38f, spanX * 0.42f, frameH * 0.68f, frameH * 0.74f, z, z + s * 0.03f, accent);
            }

            for (int tower = 0; tower < 3; tower++)
            {
                float x = (tower - 1) * s * 0.34f;
                RetroAddBoxUniform(w, x - s * 0.05f, x + s * 0.05f, rimH, frameH * 0.70f, -padR * 0.66f, -padR * 0.58f, secondary);
                RetroAddBoxUniform(w, x - s * 0.04f, x + s * 0.20f, frameH * 0.64f, frameH * 0.68f, -padR * 0.64f, -padR * 0.52f, accent);
            }

            for (int cluster = 0; cluster < 3; cluster++)
            {
                float cx = (cluster - 1) * s * 0.22f;
                float cz = s * 0.08f + cluster * s * 0.06f;
                RetroAddBoxUniform(w, cx - s * 0.10f, cx + s * 0.10f, rimH + s * 0.04f, rimH + s * 0.12f,
                    cz - s * 0.08f, cz + s * 0.08f, cluster % 2 == 0 ? primary : secondary);
                if (cluster % 2 == 0)
                    RetroAddBoxUniform(w, cx - s * 0.08f, cx + s * 0.08f, rimH + s * 0.12f, rimH + s * 0.14f,
                        cz - s * 0.06f, cz + s * 0.06f, accent);
            }
        }
    }

    private static void BuildRetroDefenseTurret(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 0.92f;
        float innerR = s * 0.50f;
        float rimH = s * 0.06f;
        float raisedH = s * 0.08f;

        RetroAddIndustrialPad(w, s, padR, innerR, rimH, primary, secondary);
        RetroAddPadAccentRing(w, s, padR * 0.98f, rimH * 0.82f, s * 0.022f, 12, accent);

        RetroAddBoxUniform(w, -innerR * 0.88f, innerR * 0.88f, rimH, rimH + raisedH, -innerR * 0.88f, innerR * 0.88f, secondary);
        RetroAddBoxUniform(w, -innerR * 0.72f, innerR * 0.72f, rimH + raisedH, rimH + raisedH + s * 0.02f,
            -innerR * 0.72f, innerR * 0.72f, primary);

        float ringY = rimH + raisedH + s * 0.02f;
        float ringR = s * 0.18f;
        float ringH = s * 0.05f;
        for (int i = 0; i < 10; i++)
        {
            float a = MathF.PI * 2f * i / 10f;
            float cx = MathF.Cos(a) * ringR;
            float cz = MathF.Sin(a) * ringR;
            RetroAddBoxUniform(w, cx - s * 0.035f, cx + s * 0.035f, ringY, ringY + ringH, cz - s * 0.035f, cz + s * 0.035f, secondary);
        }

        RetroAddBoxUniform(w, -ringR * 0.55f, ringR * 0.55f, ringY + ringH, ringY + ringH + s * 0.015f,
            -ringR * 0.55f, ringR * 0.55f, accent);

        for (int barrel = 0; barrel < 4; barrel++)
        {
            float angle = MathF.PI * 0.5f * barrel + MathF.PI * 0.25f;
            var dir = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
            float bx = dir.X * s * 0.28f;
            float bz = dir.Z * s * 0.28f;
            float perpX = -dir.Z * s * 0.025f;
            float perpZ = dir.X * s * 0.025f;
            RetroAddBoxUniform(w, bx - perpX, bx + perpX, ringY + ringH, ringY + s * 0.06f,
                bz - s * 0.08f - perpZ, bz + s * 0.04f + perpZ, primary);
            RetroAddBoxUniform(w, bx - perpX * 0.8f, bx + perpX * 0.8f, ringY + s * 0.06f, ringY + s * 0.08f,
                bz + s * 0.02f, bz + s * 0.12f, accent);
        }

        for (int pod = 0; pod < 4; pod++)
        {
            float angle = MathF.PI * 0.5f * pod;
            float px = MathF.Cos(angle) * padR * 0.72f;
            float pz = MathF.Sin(angle) * padR * 0.72f;
            RetroAddBoxUniform(w, px - s * 0.05f, px + s * 0.05f, rimH, rimH + s * 0.06f, pz - s * 0.05f, pz + s * 0.05f, primary);
        }
    }

    private static void BuildRetroSensorArray(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.12f;
        float innerR = padR * 0.60f;
        float rimH = s * 0.06f;
        float hubHalf = s * 0.12f;

        RetroAddIndustrialPad(w, s, padR, innerR, rimH, primary, secondary);
        RetroAddPadAccentRing(w, s, padR * 0.97f, rimH * 0.80f, s * 0.025f, 16, accent);

        RetroAddBoxUniform(w, -hubHalf, hubHalf, rimH, rimH + s * 0.06f, -hubHalf, hubHalf, primary);
        RetroAddBoxUniform(w, -hubHalf * 0.70f, hubHalf * 0.70f, rimH + s * 0.06f, rimH + s * 0.09f, -hubHalf * 0.70f, hubHalf * 0.70f, secondary);
        RetroAddBoxUniform(w, -s * 0.03f, s * 0.03f, rimH + s * 0.09f, rimH + s * 0.11f, -s * 0.03f, s * 0.03f, accent);

        for (int d = 0; d < 8; d++)
        {
            float a = MathF.PI * 2f * d / 8f + MathF.PI * 0.25f;
            float rad = d % 2 == 0 ? s * 0.58f : s * 0.44f;
            float dx = MathF.Cos(a) * rad;
            float dz = MathF.Sin(a) * rad;
            float reach = d % 2 == 0 ? s * 0.14f : s * 0.11f;
            RetroAddSensorDishRingFlush(w, dx, rimH + s * 0.02f, dz, reach, s, primary, secondary, accent);
        }

        float deckY = rimH + s * 0.02f;
        for (int seg = 0; seg < 8; seg++)
        {
            float a = MathF.PI * 2f * seg / 8f;
            float cx = MathF.Cos(a) * s * 0.38f;
            float cz = MathF.Sin(a) * s * 0.38f;
            RetroAddBoxUniform(w, cx - s * 0.05f, cx + s * 0.05f, deckY, deckY + s * 0.03f, cz - s * 0.05f, cz + s * 0.05f, secondary);
        }

        for (int c = 0; c < 4; c++)
        {
            float a = MathF.PI * 0.5f * c + MathF.PI * 0.25f;
            float px = MathF.Cos(a) * padR * 0.90f;
            float pz = MathF.Sin(a) * padR * 0.90f;
            RetroAddBoxUniform(w, px - s * 0.05f, px + s * 0.05f, 0, rimH + s * 0.03f, pz - s * 0.05f, pz + s * 0.05f, primary);
            RetroAddBoxUniform(w, px - s * 0.03f, px + s * 0.03f, rimH + s * 0.03f, rimH + s * 0.05f, pz - s * 0.03f, pz + s * 0.03f, accent);
        }
    }

    /// <summary>Flush uniform-luminance dish node on deck ring — no side/top facet gradients.</summary>
    private static void RetroAddSensorDishRingFlush(RaceMeshWriter w, float cx, float cy, float cz, float reach, float s,
        Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        RetroAddBoxUniform(w, cx - s * 0.05f, cx + s * 0.05f, cy, cy + s * 0.05f, cz - s * 0.05f, cz + s * 0.05f, secondary);
        RetroAddBoxUniform(w, cx - reach * 0.52f, cx + reach * 0.52f, cy + s * 0.05f, cy + s * 0.08f, cz - reach * 0.28f, cz + reach * 0.10f, primary);
        RetroAddBoxUniform(w, cx - s * 0.02f, cx + s * 0.02f, cy + s * 0.08f, cy + s * 0.10f, cz + reach * 0.08f, cz + reach * 0.12f, accent);
    }

    private static void BuildRetroRefinery(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.12f;
        float innerR = padR * 0.60f;
        float rimH = s * 0.06f;

        RetroAddIndustrialPad(w, s, padR, innerR, rimH, primary, secondary);
        RetroAddPadAccentRing(w, s, padR * 0.98f, rimH * 0.82f, s * 0.025f, 14, accent);

        RetroAddDeckCluster(w, 0, rimH, 0, s * 0.38f, primary, secondary, accent, tiers: 2);

        ReadOnlySpan<float> procX = stackalloc float[] { -0.22f, 0.18f, 0.02f };
        ReadOnlySpan<float> procZ = stackalloc float[] { 0.12f, -0.10f, 0.28f };
        ReadOnlySpan<float> procH = stackalloc float[] { 0.16f, 0.18f, 0.14f };
        for (int p = 0; p < 3; p++)
        {
            float x = procX[p] * s;
            float z = procZ[p] * s;
            float h = procH[p] * s;
            float halfW = s * 0.11f;
            RetroAddBoxUniform(w, x - halfW, x + halfW, rimH, rimH + h, z - halfW * 0.70f, z + halfW * 0.70f, primary);
            RetroAddBoxUniform(w, x - halfW * 0.75f, x + halfW * 0.75f, rimH + h, rimH + h + s * 0.025f,
                z - halfW * 0.55f, z + halfW * 0.55f, accent);
        }

        for (int intake = 0; intake < 2; intake++)
        {
            float side = intake == 0 ? -1f : 1f;
            float x = side * s * 0.34f;
            float h = s * 0.22f;
            float halfW = s * 0.09f;
            RetroAddBoxUniform(w, x - halfW, x + halfW, rimH, rimH + h, -halfW * 0.8f, halfW * 0.8f, secondary);
            RetroAddBoxUniform(w, x - halfW * 0.70f, x + halfW * 0.70f, rimH + h, rimH + h + s * 0.03f,
                -halfW * 0.65f, halfW * 0.65f, accent);
            RetroAddBoxUniform(w, x - halfW * 0.35f, x + halfW * 0.35f, rimH + h * 0.45f, rimH + h * 0.65f,
                side * halfW * 0.55f, side * halfW * 0.85f, accent * 0.92f);
        }

        RetroAddBoxUniform(w, -s * 0.30f, s * 0.30f, rimH + s * 0.04f, rimH + s * 0.07f, s * 0.08f, s * 0.22f, secondary);
        RetroAddBoxUniform(w, -s * 0.28f, s * 0.28f, rimH + s * 0.04f, rimH + s * 0.07f, -s * 0.22f, -s * 0.08f, secondary);
    }

    private static void BuildRetroRepairBay(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.18f;
        float innerR = padR * 0.64f;
        float rimH = s * 0.06f;
        float shellH = s * 0.14f;

        RetroAddIndustrialPad(w, s, padR, innerR, rimH, primary, secondary);
        RetroAddPadAccentRing(w, s, padR * 0.97f, rimH * 0.80f, s * 0.025f, 8, accent);

        RetroAddBoxUniform(w, -s * 0.36f, s * 0.36f, rimH, rimH + shellH, -s * 0.24f, s * 0.16f, primary);
        RetroAddBoxUniform(w, -s * 0.32f, s * 0.32f, rimH + shellH, rimH + shellH + s * 0.02f, -s * 0.20f, s * 0.12f, accent);

        for (int slot = 0; slot < 3; slot++)
        {
            float x = -s * 0.22f + slot * s * 0.22f;
            RetroAddBoxUniform(w, x - s * 0.09f, x + s * 0.09f, rimH, rimH + s * 0.04f, -s * 0.28f, -s * 0.12f, secondary);
            RetroAddBoxUniform(w, x - s * 0.08f, x + s * 0.08f, rimH + s * 0.04f, rimH + shellH * 0.90f, -s * 0.30f, -s * 0.24f, primary);
            if (slot % 2 == 0)
            {
                RetroAddBoxUniform(w, x - s * 0.06f, x + s * 0.06f, rimH + shellH * 0.82f, rimH + shellH * 0.86f,
                    -s * 0.28f, -s * 0.26f, accent);
            }
        }

        float craneY = rimH + shellH * 0.78f;
        RetroAddBoxUniform(w, -s * 0.34f, s * 0.34f, craneY, craneY + s * 0.035f, -s * 0.08f, s * 0.08f, secondary);
        RetroAddBoxUniform(w, -s * 0.32f, s * 0.32f, craneY + s * 0.035f, craneY + s * 0.05f, -s * 0.05f, s * 0.05f, accent);

        for (int side = -1; side <= 1; side += 2)
        {
            float reach = side < 0 ? 0.44f : 0.40f;
            float armLen = side < 0 ? 0.18f : 0.14f;
            float x = side * s * reach;
            RetroAddBoxUniform(w, x - side * s * 0.03f, x + side * armLen, rimH, rimH + shellH * 0.55f, s * 0.06f, s * 0.14f, secondary);
            RetroAddBoxUniform(w, x + side * (armLen - 0.02f), x + side * (armLen + 0.02f), rimH + shellH * 0.48f, rimH + shellH * 0.52f,
                s * 0.04f, s * 0.10f, accent);
            RetroAddBoxUniform(w, x + side * armLen, x + side * (armLen + 0.04f), rimH + shellH * 0.42f, rimH + shellH * 0.46f,
                -s * 0.02f, s * 0.08f, accent);
        }

        RetroAddBoxUniform(w, -s * 0.08f, s * 0.08f, rimH + shellH * 0.50f, rimH + shellH * 0.62f, s * 0.10f, s * 0.20f, secondary);
    }

    private static void BuildRetroReactor(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        Vector3 engine = new(0.55f, 0.72f, 0.92f);
        float padR = s * 1.08f;
        float innerR = s * 0.66f;
        float rimH = s * 0.06f;

        RetroAddIndustrialPad(w, s, padR, innerR, rimH, primary, secondary);
        RetroAddPadAccentRing(w, s, padR * 0.97f, rimH * 0.80f, s * 0.025f, 14, accent);

        RetroAddDeckCluster(w, 0, rimH, 0, s * 0.48f, primary, secondary, accent, tiers: 2);

        float domeBase = rimH + s * 0.12f;
        RetroAddBoxUniform(w, -s * 0.16f, s * 0.16f, domeBase, domeBase + s * 0.10f, -s * 0.16f, s * 0.16f, secondary);
        RetroAddBoxUniform(w, -s * 0.11f, s * 0.11f, domeBase + s * 0.10f, domeBase + s * 0.14f, -s * 0.11f, s * 0.11f, primary);
        RetroAddBoxUniform(w, -s * 0.08f, s * 0.08f, domeBase + s * 0.04f, domeBase + s * 0.08f, -s * 0.08f, s * 0.08f, engine);
        RetroAddBoxUniform(w, -s * 0.05f, s * 0.05f, domeBase + s * 0.08f, domeBase + s * 0.11f, -s * 0.05f, s * 0.05f, engine * 1.08f);
        RetroAddBoxUniform(w, -s * 0.14f, s * 0.14f, domeBase + s * 0.14f, domeBase + s * 0.16f, -s * 0.14f, s * 0.14f, accent);

        RetroAddCoolingRingFlush(w, 0, rimH + s * 0.08f, 0, s * 0.42f, s * 0.03f, 12, secondary);
        RetroAddCoolingRingFlush(w, 0, rimH + s * 0.10f, 0, s * 0.30f, s * 0.025f, 10, primary);

        for (int c = 0; c < 4; c++)
        {
            float angle = MathF.PI * 0.25f + MathF.PI * 0.5f * c;
            float cx = MathF.Cos(angle) * s * 0.46f;
            float cz = MathF.Sin(angle) * s * 0.46f;
            RetroAddBoxUniform(w, cx - s * 0.05f, cx + s * 0.05f, rimH, rimH + s * 0.10f, cz - s * 0.05f, cz + s * 0.05f, primary);
            RetroAddBoxUniform(w, cx - s * 0.04f, cx + s * 0.04f, rimH + s * 0.10f, rimH + s * 0.12f, cz - s * 0.04f, cz + s * 0.04f, accent);
        }
    }

    private static void BuildRetroSupplyDepot(RaceMeshWriter w, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float padR = s * 1.04f;
        float innerR = padR * 0.58f;
        float rimH = s * 0.06f;

        RetroAddIndustrialPad(w, s, padR, innerR, rimH, primary, secondary);
        RetroAddPadAccentRing(w, s, padR * 0.98f, rimH * 0.82f, s * 0.025f, 12, accent);

        RetroAddBoxUniform(w, -s * 0.16f, s * 0.16f, rimH, rimH + s * 0.14f, -s * 0.12f, s * 0.12f, secondary);
        RetroAddBoxUniform(w, -s * 0.14f, s * 0.14f, rimH + s * 0.14f, rimH + s * 0.17f, -s * 0.10f, s * 0.10f, accent);
        RetroAddBoxUniform(w, -s * 0.10f, s * 0.10f, rimH + s * 0.06f, rimH + s * 0.10f, -s * 0.06f, s * 0.06f, primary);

        for (int ext = 0; ext < 2; ext++)
        {
            float side = ext == 0 ? -1f : 1f;
            float padStart = padR * 0.52f;
            float padEnd = padR * 0.94f;
            float cx = side * (padStart + padEnd) * 0.5f;
            float halfLen = (padEnd - padStart) * 0.5f;
            RetroAddBoxUniform(w, cx - halfLen, cx + halfLen, rimH, rimH + s * 0.04f, -s * 0.10f, s * 0.10f, primary);

            for (int cargo = 0; cargo < 2; cargo++)
            {
                float z = (cargo == 0 ? -1f : 1f) * s * 0.06f;
                float h = s * (cargo == 0 ? 0.14f : 0.16f);
                float halfW = s * 0.08f;
                RetroAddBoxUniform(w, cx - halfW, cx + halfW, rimH + s * 0.04f, rimH + s * 0.04f + h,
                    z - halfW * 0.75f, z + halfW * 0.75f, secondary);
                RetroAddBoxUniform(w, cx - halfW * 0.85f, cx + halfW * 0.85f, rimH + s * 0.04f + h, rimH + s * 0.04f + h + s * 0.02f,
                    z - halfW * 0.60f, z + halfW * 0.60f, accent);
            }
        }

        for (int corner = 0; corner < 4; corner++)
        {
            float a = MathF.PI * 0.5f * corner + MathF.PI * 0.25f;
            float px = MathF.Cos(a) * padR * 0.78f;
            float pz = MathF.Sin(a) * padR * 0.78f;
            RetroAddBoxUniform(w, px - s * 0.04f, px + s * 0.04f, rimH, rimH + s * 0.05f, pz - s * 0.04f, pz + s * 0.04f, primary);
        }
    }

    private static void ApplyRaceStationFins(RaceMeshWriter w, RaceVisualDefinition race, float s, float h)
    {
        switch (race.Style.ToLowerInvariant())
        {
            case "sleek":
                w.TriColored(new Vector3(0, h, s * 0.5f), new Vector3(-s * 0.08f, h * 0.6f, s * 0.7f), new Vector3(s * 0.08f, h * 0.6f, s * 0.7f), ToVector3(race.Palette.Accent));
                break;
            case "blocky":
                w.TriColored(new Vector3(-s * 0.55f, h * 0.3f, s * 0.45f), new Vector3(-s * 0.55f, h * 0.3f, s * 0.65f), new Vector3(-s * 0.7f, h * 0.5f, s * 0.55f), ToVector3(race.Palette.Secondary));
                w.TriColored(new Vector3(s * 0.55f, h * 0.3f, s * 0.45f), new Vector3(s * 0.7f, h * 0.5f, s * 0.55f), new Vector3(s * 0.55f, h * 0.3f, s * 0.65f), ToVector3(race.Palette.Secondary));
                break;
            case "truss":
                w.TriColored(new Vector3(-s * 0.72f, h * 0.22f, s * 0.48f), new Vector3(-s * 0.72f, h * 0.38f, s * 0.62f), new Vector3(-s * 0.95f, h * 0.3f, s * 0.55f), ToVector3(race.Palette.Accent));
                w.TriColored(new Vector3(s * 0.72f, h * 0.22f, s * 0.48f), new Vector3(s * 0.95f, h * 0.3f, s * 0.55f), new Vector3(s * 0.72f, h * 0.38f, s * 0.62f), ToVector3(race.Palette.Accent));
                w.TriColored(new Vector3(-s * 0.18f, h * 0.55f, s * 0.58f), new Vector3(s * 0.18f, h * 0.55f, s * 0.58f), new Vector3(0, h * 0.82f, s * 0.72f), ToVector3(race.Palette.Secondary));
                break;
            case "organic":
                w.TriColored(new Vector3(-s * 0.35f, h * 0.2f, s * 0.55f), new Vector3(0, h * 0.55f, s * 0.75f), new Vector3(s * 0.35f, h * 0.2f, s * 0.55f), ToVector3(race.Palette.Accent));
                break;
            case "crystalline":
                for (int f = 0; f < 3; f++)
                {
                    float ang = MathF.PI * 2f * f / 3f + 0.4f;
                    var tip = new Vector3(MathF.Cos(ang) * s * 0.15f, h * 1.15f, MathF.Sin(ang) * s * 0.15f + s * 0.55f);
                    w.TriColored(new Vector3(0, h, s * 0.5f), new Vector3(MathF.Cos(ang) * s * 0.22f, h * 0.45f, s * 0.62f), tip, ToVector3(race.Palette.Accent) * 1.1f);
                }
                break;
            case "spiny":
                for (int sp = 0; sp < 4; sp++)
                {
                    float ang = MathF.PI * 0.5f * sp;
                    var basePt = new Vector3(MathF.Cos(ang) * s * 0.5f, h * 0.35f, MathF.Sin(ang) * s * 0.5f + s * 0.45f);
                    var tip = new Vector3(MathF.Cos(ang) * s * 0.62f, h * 0.95f, MathF.Sin(ang) * s * 0.62f + s * 0.55f);
                    w.TriColored(basePt, tip, new Vector3(MathF.Cos(ang + 0.15f) * s * 0.48f, h * 0.4f, MathF.Sin(ang + 0.15f) * s * 0.48f + s * 0.48f), ToVector3(race.Palette.Accent));
                }
                break;
            case "asymmetric":
                w.TriColored(new Vector3(s * 0.25f, h * 0.55f, s * 0.55f), new Vector3(s * 0.52f, h * 0.28f, s * 0.72f), new Vector3(s * 0.08f, h * 0.22f, s * 0.68f), ToVector3(race.Palette.Accent));
                w.TriColored(new Vector3(s * 0.35f, h * 0.75f, s * 0.48f), new Vector3(s * 0.55f, h * 0.45f, s * 0.62f), new Vector3(s * 0.18f, h * 0.42f, s * 0.58f), ToVector3(race.Palette.Accent) * 0.95f);
                break;
            case "radiant":
                w.TriColored(new Vector3(-s * 0.55f, h * 0.15f, s * 0.5f), new Vector3(s * 0.55f, h * 0.15f, s * 0.5f), new Vector3(0, h * 0.52f, s * 0.75f), ToVector3(race.Palette.Accent) * 1.05f);
                for (int wing = -1; wing <= 1; wing += 2)
                    w.TriColored(new Vector3(wing * s * 0.62f, h * 0.22f, s * 0.55f), new Vector3(wing * s * 0.82f, h * 0.38f, s * 0.48f),
                        new Vector3(wing * s * 0.58f, h * 0.42f, s * 0.68f), ToVector3(race.Palette.Accent) * 0.95f);
                break;
            default:
                w.TriColored(new Vector3(-s * 0.12f, h * 0.4f, s * 0.55f), new Vector3(0, h * 0.75f, s * 0.65f), new Vector3(s * 0.12f, h * 0.4f, s * 0.55f), ToVector3(race.Palette.Accent));
                break;
        }
    }

    private static void BuildFabricationHub(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s) =>
        BuildRefinery(w, race, primary, secondary, accent, s, omitArm: true);

    private static void BuildCommsRelay(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s) =>
        BuildSensorArray(w, race, primary, secondary, accent, s, omitDish: true);

    private static void BuildOrbitalUplink(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        BuildSensorArray(w, race, primary, secondary, accent, s, omitDish: true);
        w.TriColored(new Vector3(-s * 0.10f, s * 0.72f, 0), new Vector3(s * 0.10f, s * 0.72f, 0),
            new Vector3(0, s * 0.92f, s * 0.06f), accent * 1.15f);
    }

    private static void BuildShieldEmitter(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s) =>
        BuildReactor(w, race, primary, secondary, accent, s, omitRing: true);

    private static void BuildFortressCore(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        float styleScale = 0.85f + race.Modifiers.Superstructure * 0.3f;
        BuildCommandCenter(w, race, primary, secondary, accent, 8f * styleScale, omitMast: true);
        for (int pod = -1; pod <= 1; pod += 2)
        {
            float x = pod * s * 0.35f;
            w.TriColored(new Vector3(x - s * 0.08f, s * 0.48f, s * 0.16f), new Vector3(x + s * 0.08f, s * 0.48f, s * 0.16f),
                new Vector3(x, s * 0.58f, s * 0.24f), accent);
        }
    }

    private static Vector3 ToVector3(float[] rgb) =>
        rgb.Length >= 3 ? new Vector3(rgb[0], rgb[1], rgb[2]) : Vector3.One;
}