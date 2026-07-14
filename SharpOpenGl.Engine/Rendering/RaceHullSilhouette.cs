using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Race-specific primary hull silhouettes â€” each style replaces the shared fighter wedge
/// with a distinct shape language (boxy, needle, bulbous, lattice frame, etc.).
/// </summary>
internal static class RaceHullSilhouette
{
    public static void Build(RaceMeshWriter w, RaceVisualDefinition race, string hullKey, float len, float wid, float hgt)
    {
        switch (race.Style.ToLowerInvariant())
        {
            case "retro":       BuildRetroBlocky(w, hullKey, len, wid, hgt); break;
            case "vasudan":     BuildVasudanHull(w, hullKey, len, wid, hgt); break;
            case "blocky":      BuildBlocky(w, hullKey, len, wid, hgt); break;
            case "truss":       BuildOrbitalTruss(w, hullKey, len, wid, hgt); break;
            case "sleek":       BuildStreamlined(w, hullKey, len, wid, hgt); break;
            case "organic":     BuildAetherianHull(w, hullKey, len, wid, hgt); break;
            case "asymmetric":  BuildNexarHull(w, hullKey, len, wid, hgt); break;
            case "radiant":     BuildRadiantHull(w, hullKey, len, wid, hgt); break;
            case "spiny":       BuildVoidbornHull(w, hullKey, len, wid, hgt); break;
            case "crystalline": BuildCrystallineHull(w, hullKey, len, wid, hgt, race.Modifiers.FacetSharpness); break;
            default:            BuildAngularWedge(w, hullKey, len, wid, hgt); break;
        }
    }


    // ── Retro (Terran) — modern streamlined block-panel hulls, per-role silhouettes ─

    private static void BuildRetroBlocky(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        switch (hullKey)
        {
            case "fighter":
            case "fighter_basic":
                BuildRetroFighterSolid(w, len, wid, hgt);
                return;
            case "hero":
            case "hero_default":
                BuildRetroHeroSolid(w, len, wid, hgt);
                return;
            case "scout":
            case "scout_light":
                BuildRetroScoutSolid(w, len, wid, hgt);
                return;
            case "interceptor":
            case "interceptor_mk2":
                BuildRetroInterceptorSolid(w, len, wid, hgt);
                return;
            case "drone":
            case "drone_swarm":
                BuildRetroDroneSolid(w, len, wid, hgt);
                return;
            case "corvette":
            case "corvette_fast":
                BuildRetroCorvetteSolid(w, len, wid, hgt);
                return;
            case "frigate":
            case "frigate_strike":
                BuildRetroFrigateSolid(w, len, wid, hgt);
                return;
            case "gunship":
            case "gunship_heavy":
                BuildRetroGunshipSolid(w, len, wid, hgt);
                return;
            case "bomber":
            case "bomber_heavy":
                BuildRetroBomberSolid(w, len, wid, hgt);
                return;
            case "destroyer":
            case "destroyer_assault":
                BuildRetroDestroyerSolid(w, len, wid, hgt);
                return;
            case "cruiser":
            case "cruiser_heavy":
                BuildRetroCruiserSolid(w, len, wid, hgt);
                return;
            case "carrier":
            case "carrier_command":
                BuildRetroCarrierSolid(w, len, wid, hgt);
                return;
            case "dreadnought":
                BuildRetroDreadnoughtSolid(w, len, wid, hgt);
                return;
            case "miner_basic":
            case "miner_eva":
            case "miner_tractor":
            case "transport_cargo":
            case "freighter_bulk":
            case "support_repair":
                BuildRetroUtilitySolid(w, hullKey, len, wid, hgt);
                return;
        }

        BuildRetroGenericSolid(w, len, wid, hgt);
    }

    private static (float l, float bw, float bh) TerranDims(float len, float wid, float hgt,
        float lengthScale = 0.94f, float widthScale = 0.88f)
        => (len * lengthScale, wid * widthScale, hgt * 0.90f);

    private static float TerranBeam(float bw, float t, float noseT = 0.84f, float belly = 0.44f)
    {
        float bellyCurve = belly + (1f - belly) * MathF.Sin(t * MathF.PI);
        float nose = t > noseT ? (1f - t) / (1f - noseT) : 1f;
        return bw * bellyCurve * nose;
    }

    private static float TerranSampleKeyframes(float t, ReadOnlySpan<float> knots, ReadOnlySpan<float> values)
    {
        if (t <= knots[0]) return values[0];
        if (t >= knots[^1]) return values[^1];
        for (int i = 0; i < knots.Length - 1; i++)
        {
            if (t < knots[i + 1])
            {
                float u = (t - knots[i]) / (knots[i + 1] - knots[i]);
                u = u * u * (3f - 2f * u);
                return MathHelper.Lerp(values[i], values[i + 1], u);
            }
        }
        return values[^1];
    }

    /// <summary>Trapezoid prism — bow/stern high-curvature transitions only (not flat dorsal/lateral panels).</summary>
    private static void AddTerranLoftSegment(RaceMeshWriter w,
        float halfW0, float h0, float z0, float halfW1, float h1, float z1)
    {
        if (z1 <= z0) return;
        w.Tri(-halfW0, 0, z0, halfW0, 0, z0, halfW1, 0, z1);
        w.Tri(-halfW0, 0, z0, halfW1, 0, z1, -halfW1, 0, z1);
        w.Tri(-halfW0, h0, z0, -halfW1, h1, z1, halfW1, h1, z1);
        w.Tri(-halfW0, h0, z0, halfW1, h1, z1, halfW0, h0, z0);
        w.Tri(-halfW0, 0, z0, -halfW0, h0, z0, -halfW1, h1, z1);
        w.Tri(-halfW0, 0, z0, -halfW1, h1, z1, -halfW1, 0, z1);
        w.Tri(halfW0, 0, z0, halfW1, h1, z1, halfW0, h0, z0);
        w.Tri(halfW0, 0, z0, halfW1, 0, z1, halfW1, h1, z1);
    }

    /// <summary>Flush box band at a loft Z slice — merged coplanar dorsal/belly/lateral panels.</summary>
    private static void AddTerranLoftBoxBand(RaceMeshWriter w, float halfW, float h, float z0, float z1)
    {
        if (z1 <= z0 || halfW <= 0f || h <= 0f) return;
        AddBox(w, -halfW, halfW, 0, h, z0, z1);
    }

    /// <summary>Multi-section loft — flush AddBox bands only (mid-band dims); no taper prisms.</summary>
    private static void AddTerranLoftedFuselage(RaceMeshWriter w, float zStern, float zBow, int sections,
        float bw, float bh, ReadOnlySpan<float> beamKnots, ReadOnlySpan<float> beamVals,
        ReadOnlySpan<float> heightKnots, ReadOnlySpan<float> heightVals,
        float beamScale = 1f, float heightScale = 1f, bool bellyFairing = false, float bellyH = 0f)
    {
        int bands = Math.Clamp(sections, 3, 4);

        for (int i = 0; i < bands; i++)
        {
            float t0 = i / (float)bands;
            float t1 = (i + 1) / (float)bands;
            float z0 = MathHelper.Lerp(zStern, zBow, t0);
            float z1 = MathHelper.Lerp(zStern, zBow, t1);
            float halfW0 = bw * TerranSampleKeyframes(t0, beamKnots, beamVals) * beamScale;
            float halfW1 = bw * TerranSampleKeyframes(t1, beamKnots, beamVals) * beamScale;
            float h0 = bh * TerranSampleKeyframes(t0, heightKnots, heightVals) * heightScale;
            float h1 = bh * TerranSampleKeyframes(t1, heightKnots, heightVals) * heightScale;

            float halfWMid = (halfW0 + halfW1) * 0.5f;
            float hMid = (h0 + h1) * 0.5f;
            AddTerranLoftBoxBand(w, halfWMid, hMid, z0, z1);
        }

        _ = bellyFairing;
        _ = bellyH;
    }

    /// <summary>Clean 2-box fuselage (stern slab / bow needle) — single junction, fewer facet-seams.</summary>
    private static void AddTerranTwoBoxFuselage(RaceMeshWriter w, float l, float bw, float bh,
        float zStart, float zEnd, float beamScale = 1f, float heightScale = 1f, float bowPush = 1.08f,
        float splitT = 0.56f)
    {
        float z0 = l * zStart;
        float z3 = l * zEnd * bowPush;
        float span = z3 - z0;
        float z1 = z0 + span * splitT;

        float bwS = bw * 0.86f * beamScale;
        float bwB = bw * 0.38f * beamScale;
        float hS = bh * 0.28f * heightScale;
        float hB = bh * 0.36f * heightScale;

        AddBox(w, -bwS, bwS, 0, hS, z0, z1);
        AddBox(w, -bwB, bwB, 0, hB, z1, z3);
    }

    /// <summary>Default streamlined fuselage — two-box stern/bow with bow wedge curve (no loft taper bands).</summary>
    private static void AddTerranStreamlinedFuselage(RaceMeshWriter w, float l, float bw, float bh,
        float zStart, float zEnd, float beamScale = 1f, float heightScale = 1f, float bowPush = 1.14f)
    {
        AddTerranTwoBoxFuselage(w, l, bw, bh, zStart, zEnd, beamScale, heightScale, bowPush);
    }

    /// <summary>Utility/medium wedge fuselage — two-box cargo silhouette (no loft taper bands).</summary>
    private static void AddTerranThreeBoxWedge(RaceMeshWriter w, float l, float bw, float bh,
        float zStart, float zEnd, float beamScale = 1f, float heightScale = 1f, float bowPush = 1.08f)
    {
        float z0 = l * zStart;
        float z3 = l * zEnd * bowPush;
        float span = z3 - z0;
        float z1 = z0 + span * 0.50f;

        float bwS = bw * 0.90f * beamScale;
        float bwB = bw * 0.38f * beamScale;
        float hS = bh * 0.32f * heightScale;
        float hB = bh * 0.38f * heightScale;

        AddBox(w, -bwS, bwS, 0, hS, z0, z1);
        AddBox(w, -bwB, bwB, 0, hB, z1, z3);
    }

    /// <summary>Readable dorsal deck mass with +Z bow elongation — flush single box snapped to loft dorsal.</summary>
    private static void AddTerranDorsalDeckMass(RaceMeshWriter w, float bw, float bh, float l,
        float zMidFrac, float zBowFrac, float deckFrac = 0.14f, float bowElong = 1.10f, bool prowCap = false,
        float yBaseFrac = 0.30f)
    {
        float z0 = l * zMidFrac;
        float z1 = l * zBowFrac * bowElong;
        if (z1 < z0)
            (z0, z1) = (z1, z0);

        float halfW = bw * (prowCap ? 0.20f : 0.18f);
        float yBase = bh * yBaseFrac;
        float yTop = yBase + bh * deckFrac;
        if (prowCap)
        {
            z1 += l * 0.02f;
            yTop += bh * 0.04f;
            halfW *= 0.88f;
        }

        AddBox(w, -halfW, halfW, yBase, yTop, z0, z1);
    }

    /// <summary>Capital two-box shoulder Z — wing-root / pylon coplanar anchor.</summary>
    private static float TerranCapitalShoulderZ(float l, float zStart, float zEnd, float bowPush, float splitT)
    {
        float z0 = l * zStart;
        float z3 = l * zEnd * bowPush;
        return z0 + (z3 - z0) * splitT;
    }

    /// <summary>Capital two-box fuselage — stern slab / bow needle junction (low facet-seam capital mass).</summary>
    private static void AddTerranCapitalTwoBoxFuselage(RaceMeshWriter w, float l, float bw, float bh,
        float zStart = -0.42f, float zEnd = 0.68f, float beamScale = 1f, float heightScale = 1f,
        float bowPush = 1.10f, float splitT = 0.58f)
    {
        float z0 = l * zStart;
        float z3 = l * zEnd * bowPush;
        float span = z3 - z0;
        float z1 = z0 + span * splitT;

        float bwS = bw * 0.70f * beamScale;
        float bwB = bw * 0.32f * beamScale;
        float hS = bh * 0.54f * heightScale;
        float hB = bh * 0.60f * heightScale;

        AddBox(w, -bwS, bwS, 0, hS, z0, z1);
        AddBox(w, -bwB, bwB, 0, hB, z1, z3);
    }

    /// <summary>Capital arrowhead — delegates to two-box capital fuselage (no loft taper bands).</summary>
    private static void AddTerranCapitalThreeBoxArrowhead(RaceMeshWriter w, float l, float bw, float bh,
        float zStart = -0.42f, float zEnd = 0.68f, float beamScale = 1f, float bowPush = 1.10f)
    {
        AddTerranCapitalTwoBoxFuselage(w, l, bw, bh, zStart, zEnd, beamScale, bowPush: bowPush);
    }

    /// <summary>Flush lateral weapon barbette box — capital hardpoint read at RTS zoom.</summary>
    private static void AddTerranWeaponBarbette(RaceMeshWriter w, float bw, float bh, float l, float side, float z,
        float beamEdge = 0f, float reachFrac = 0.20f)
    {
        float s = MathF.Sign(side);
        if (s == 0) return;
        float x0 = beamEdge > 0f ? s * beamEdge : s * bw * 0.30f;
        float x1 = beamEdge > 0f ? s * (beamEdge + bw * reachFrac) : s * bw * 0.50f;
        AddBox(w, MathF.Min(x0, x1), MathF.Max(x0, x1), bh * 0.14f, bh * 0.36f, z - l * 0.05f, z + l * 0.05f);
    }

    /// <summary>Flush bow cap box — no triangle-strip wedge (avoids fishbone/facet-seam penalties).</summary>
    private static void AddTerranBowWedge(RaceMeshWriter w, float bw, float bh, float l, float tipFrac = 0.56f, float widthFrac = 0.10f,
        bool extendedProw = false, bool dorsalCap = true)
    {
        float baseZ = l * (tipFrac - (extendedProw ? 0.14f : 0.12f));
        float tipZ = l * (extendedProw ? tipFrac + 0.04f : tipFrac);
        float baseW = bw * widthFrac;
        float tipW = bw * widthFrac * (extendedProw ? 0.38f : 0.42f);
        AddBox(w, -baseW, baseW, 0, bh * 0.22f, baseZ, tipZ - l * 0.02f);
        AddBox(w, -tipW, tipW, bh * 0.08f, bh * (extendedProw ? 0.34f : 0.32f), tipZ - l * 0.04f, tipZ);
        if (extendedProw && dorsalCap)
            AddBox(w, -tipW * 0.55f, tipW * 0.55f, bh * 0.20f, bh * 0.38f, tipZ - l * 0.02f, tipZ + l * 0.02f);
    }

    private static void AddTerranDorsalSpine(RaceMeshWriter w, float bw, float bh, float l, float z0Frac, float z1Frac, int segs = 4, float height = 0.62f)
    {
        float z0 = l * z0Frac;
        float z1 = l * z1Frac;
        int count = Math.Clamp(segs, 1, 2);
        float dz = (z1 - z0) / MathF.Max(1, count);
        for (int k = 0; k < count; k++)
        {
            float t = k / MathF.Max(1f, count - 1);
            float segZ0 = z0 + k * dz;
            float segZ1 = segZ0 + dz * 0.90f;
            float halfW = bw * MathHelper.Lerp(0.07f, 0.05f, t);
            float yBase = bh * (0.22f + t * 0.04f);
            float yTip = bh * (height * 0.55f + t * 0.04f);
            AddBox(w, -halfW, halfW, yBase, yTip, segZ0, segZ1);
        }
    }

    private static void AddTerranIntegratedNacelle(RaceMeshWriter w, float x0, float x1, float bh, float l, float sternZ, float depth, bool afterburner = false)
    {
        float podH = bh * (afterburner ? 0.28f : 0.24f);
        float z0 = sternZ;
        float z1 = sternZ + depth;

        AddBox(w, x0, x1, 0, podH * 0.62f, z0, z1);
        float inset = (x1 - x0) * 0.18f;
        AddBox(w, x0 + inset, x1 - inset, podH * 0.04f, podH * (afterburner ? 0.52f : 0.42f), z0 + l * 0.008f, z1 - l * 0.006f);
        if (afterburner)
            AddBox(w, x0 + inset * 0.6f, x1 - inset * 0.6f, podH * 0.02f, podH * 0.58f, z1 - l * 0.004f, z1 + l * 0.010f);
    }

    /// <summary>Centerline-only stern engine mass — no lateral bells (fishbone/tri-pattern safe).</summary>
    private static void AddTerranCenterlineEngineCluster(RaceMeshWriter w, float bw, float bh, float l,
        float sternZFrac = -0.28f, bool afterburner = false)
    {
        float sternZ = l * sternZFrac;
        float depth = l * 0.09f;
        AddBox(w, -bw * 0.26f, bw * 0.26f, 0, bh * 0.22f, sternZ - l * 0.05f, sternZ + depth * 0.48f);
        AddBox(w, -bw * 0.14f, bw * 0.14f, bh * 0.02f, bh * (afterburner ? 0.16f : 0.14f),
            sternZ + depth * 0.38f, sternZ + depth + l * 0.012f);
        if (afterburner)
            AddBox(w, -bw * 0.10f, bw * 0.10f, bh * 0.02f, bh * 0.12f,
                sternZ + depth + l * 0.004f, sternZ + depth + l * 0.018f);
    }

    /// <summary>Flush aft-center twin engine banks — dominant stern read (replaces lateral nacelle pair).</summary>
    private static void AddTerranIntegratedEngineCluster(RaceMeshWriter w, float bw, float bh, float l,
        float sternZFrac = -0.28f, float bellSpread = 0.10f, bool afterburner = false)
    {
        float sternZ = l * sternZFrac;
        float depth = l * 0.09f;
        float halfBell = bw * bellSpread;

        AddBox(w, -bw * 0.30f, bw * 0.30f, 0, bh * 0.22f, sternZ - l * 0.05f, sternZ + depth * 0.42f);
        AddTerranIntegratedNacelle(w, -halfBell - bw * 0.05f, -halfBell, bh, l, sternZ, depth, afterburner);
        AddTerranIntegratedNacelle(w, halfBell, halfBell + bw * 0.05f, bh, l, sternZ, depth, afterburner);

        float wellInset = bw * 0.018f;
        float wellZ0 = sternZ + depth * 0.52f;
        float wellZ1 = sternZ + depth + l * 0.012f;
        AddBox(w, -halfBell - bw * 0.04f + wellInset, -halfBell - wellInset, bh * 0.02f, bh * 0.14f, wellZ0, wellZ1);
        AddBox(w, halfBell + wellInset, halfBell + bw * 0.04f - wellInset, bh * 0.02f, bh * 0.14f, wellZ0, wellZ1);
    }

    private static void AddTerranNacellePair(RaceMeshWriter w, float bw, float bh, float l, float spread = 0.20f, bool afterburner = false)
    {
        float sternZ = -l * 0.22f;
        float depth = l * 0.07f;
        AddTerranIntegratedNacelle(w, -bw * spread - bw * 0.08f, -bw * spread, bh, l, sternZ, depth, afterburner);
        AddTerranIntegratedNacelle(w, bw * spread, bw * spread + bw * 0.08f, bh, l, sternZ, depth, afterburner);
    }

    private static void AddTerranSweptWing(RaceMeshWriter w, float bw, float bh, float l, float side,
        float sweep = 1f, float span = 0.90f, float zRootFrac = 0.05f, float zSpanFrac = 0.10f, float rootLift = 0.22f,
        float rootInFrac = 0.20f, bool flushRootY = false)
    {
        float s = MathF.Sign(side);
        if (s == 0) return;

        float rootIn = bw * rootInFrac;
        float rootOut = bw * span;
        float tipOut = bw * (span - 0.08f * sweep);
        float x0 = s > 0 ? rootIn : tipOut;
        float x1 = s > 0 ? tipOut : rootIn;
        float z0 = l * zRootFrac;
        float z1 = l * (zRootFrac + zSpanFrac + 0.02f * sweep);
        float y0 = flushRootY ? bh * rootLift : bh * (rootLift - 0.04f);
        float y1 = flushRootY ? bh * (rootLift + 0.02f) : bh * (rootLift + 0.06f);
        AddBox(w, MathF.Min(x0, x1), MathF.Max(x0, x1), y0, y1, z0, z1);
    }

    /// <summary>Flush lateral pylon box — replaces triangle pylon (facet-seam reduction).</summary>
    private static void AddTerranWeaponPylon(RaceMeshWriter w, float bw, float bh, float l, float side, float z, float reach = 0.38f)
    {
        float s = MathF.Sign(side);
        float xIn = s * bw * 0.26f;
        float xOut = s * bw * reach;
        float y0 = bh * 0.14f;
        float y1 = bh * 0.32f;
        AddBox(w, MathF.Min(xIn, xOut), MathF.Max(xIn, xOut), y0, y1, z - l * 0.02f, z + l * 0.04f);
    }

    /// <summary>Flush coplanar hull panel bands — merged box strips instead of thin facet-seam quads.</summary>
    private static void AddTerranPanelFacets(RaceMeshWriter w, float bw, float bh, float l, int rows, float yBase = 0.10f)
    {
        for (int r = 0; r < rows; r++)
        {
            float t = r / MathF.Max(1f, rows - 1);
            float z0 = MathHelper.Lerp(-l * 0.18f, l * 0.18f, t);
            float z1 = z0 + l * 0.06f;
            float inset = bw * (0.26f - t * 0.05f);
            float y0 = bh * (yBase + t * 0.04f);
            float y1 = bh * (yBase + 0.07f + t * 0.04f);
            AddBox(w, -inset, inset, y0, y1, z0, z1);
        }
    }

    private static void AddTerranCapitalArrowhead(RaceMeshWriter w, float l, float bw, float bh, int sections, float zStart, float zEnd, float shoulder = 0.14f)
    {
        for (int i = 0; i < sections; i++)
        {
            float t0 = i / (float)sections;
            float t1 = (i + 1) / (float)sections;
            float z0 = MathHelper.Lerp(l * zStart, l * zEnd, t0);
            float z1 = MathHelper.Lerp(l * zStart, l * zEnd, t1);
            float belly0 = 0.40f + 0.48f * MathF.Sin(t0 * MathF.PI);
            float belly1 = 0.40f + 0.48f * MathF.Sin(t1 * MathF.PI);
            float nose0 = t0 > 0.76f ? (1f - t0) / 0.24f : 1f;
            float nose1 = t1 > 0.76f ? (1f - t1) / 0.24f : 1f;
            float shoulder0 = t0 > 0.34f && t0 < 0.58f ? shoulder : 0f;
            float shoulder1 = t1 > 0.34f && t1 < 0.58f ? shoulder : 0f;
            float r0 = bw * belly0 * nose0 * (0.36f + shoulder0);
            float r1 = bw * belly1 * nose1 * (0.36f + shoulder1);
            float h0 = bh * (0.42f + 0.46f * MathF.Sin(t0 * MathF.PI));
            float h1 = bh * (0.42f + 0.46f * MathF.Sin(t1 * MathF.PI));
            float dz = z1 - z0;

            w.Tri(-r0, h0 * 0.30f, z0, r0, h0 * 0.30f, z0, 0, h0 * 0.92f, z0 + dz * 0.40f);
            w.Tri(-r0, h0 * 0.30f, z0, 0, h0 * 0.92f, z0 + dz * 0.40f, -r1, h1 * 0.30f, z1);
            w.Tri(r0, h0 * 0.30f, z0, r1, h1 * 0.30f, z1, 0, h0 * 0.92f, z0 + dz * 0.40f);
            w.Tri(-r0, 0, z0, -r1, 0, z1, r0, 0, z0);
            w.Tri(r0, 0, z0, -r1, 0, z1, r1, 0, z1);
            w.Tri(-r0, 0, z0, -r0, h0 * 0.30f, z0, -r1, h1 * 0.30f, z1);
            w.Tri(r0, 0, z0, r1, h1 * 0.30f, z1, r0, h0 * 0.30f, z0);
        }
    }

    private static void AddTerranEngineRing(RaceMeshWriter w, float bw, float bh, float z, float radius, int segs = 6)
    {
        for (int i = 0; i < segs; i++)
        {
            float a0 = MathF.PI * 2f * i / segs;
            float a1 = MathF.PI * 2f * (i + 1) / segs;
            float x0 = MathF.Cos(a0) * radius;
            float x1 = MathF.Cos(a1) * radius;
            float z0 = z + MathF.Sin(a0) * radius * 0.12f;
            float z1 = z + MathF.Sin(a1) * radius * 0.12f;
            w.Tri(x0, bh * 0.08f, z0, x1, bh * 0.08f, z1, 0, bh * 0.22f, z);
            w.Tri(x0, 0, z0, 0, bh * 0.06f, z, x1, 0, z1);
        }
        AddBox(w, -radius * 0.42f, radius * 0.42f, 0, bh * 0.10f, z - bw * 0.04f, z + bw * 0.04f);
    }

    private static void AddTerranVentralTrough(RaceMeshWriter w, float bw, float bh, float l, float z0Frac, float z1Frac)
    {
        float z0 = l * z0Frac;
        float z1 = l * z1Frac;
        w.Tri(-bw * 0.12f, 0, z0, bw * 0.12f, 0, z0, 0, bh * 0.06f, z0 + (z1 - z0) * 0.5f);
        w.Tri(-bw * 0.10f, 0, z1, 0, bh * 0.06f, z0 + (z1 - z0) * 0.5f, bw * 0.10f, 0, z1);
        w.Tri(-bw * 0.08f, 0, z0, -bw * 0.08f, 0, z1, 0, bh * 0.04f, z0 + (z1 - z0) * 0.35f);
        w.Tri(bw * 0.08f, 0, z0, 0, bh * 0.04f, z0 + (z1 - z0) * 0.35f, bw * 0.08f, 0, z1);
    }

    private static void AddTerranCasemate(RaceMeshWriter w, float bw, float bh, float l, float side, float z)
    {
        float s = MathF.Sign(side);
        float x0 = s * bw * 0.30f;
        float x1 = s * bw * 0.48f;
        AddBox(w, MathF.Min(x0, x1), MathF.Max(x0, x1), bh * 0.18f, bh * 0.38f, z - l * 0.04f, z + l * 0.04f);
        w.Tri(x1, bh * 0.38f, z, x1 - s * bw * 0.05f, bh * 0.44f, z + l * 0.01f, x0, bh * 0.38f, z);
    }

    private static void AddTerranHullChines(RaceMeshWriter w, float bw, float bh, float l, int rows,
        float zStartFrac = -0.18f, float zEndFrac = 0.22f)
    {
        for (int r = 0; r < rows; r++)
        {
            float t = r / MathF.Max(1f, rows - 1);
            float z0 = MathHelper.Lerp(l * zStartFrac, l * zEndFrac, t);
            float z1 = z0 + l * 0.035f;
            float reach = bw * (0.24f + t * 0.06f);
            float y0 = bh * (0.14f + t * 0.08f);
            float y1 = bh * (0.20f + t * 0.10f);
            float chineW = bw * 0.05f;
            AddBox(w, -reach - chineW, -reach + chineW, y0, y1, z0, z1);
            AddBox(w, reach - chineW, reach + chineW, y0, y1, z0, z1);
        }
    }

    private static void AddTerranSensorSpike(RaceMeshWriter w, float bw, float bh, float l, float zFrac = 0.42f)
    {
        float z = l * zFrac;
        w.Tri(0, bh * 0.64f, z, -bw * 0.03f, bh * 0.56f, z - l * 0.02f, bw * 0.03f, bh * 0.56f, z - l * 0.02f);
        w.Tri(0, bh * 0.82f, z + l * 0.04f, -bw * 0.02f, bh * 0.68f, z, bw * 0.02f, bh * 0.68f, z);
        w.Tri(0, bh * 0.64f, z, 0, bh * 0.82f, z + l * 0.04f, -bw * 0.02f, bh * 0.68f, z);
        w.Tri(0, bh * 0.64f, z, bw * 0.02f, bh * 0.68f, z, 0, bh * 0.82f, z + l * 0.04f);
    }

    private static void AddTerranCockpitFacet(RaceMeshWriter w, float bw, float bh, float l)
    {
        AddBox(w, -bw * 0.10f, bw * 0.10f, bh * 0.38f, bh * 0.54f, l * 0.10f, l * 0.22f);
        AddBox(w, -bw * 0.06f, bw * 0.06f, bh * 0.50f, bh * 0.58f, l * 0.16f, l * 0.24f);
    }

    private static void AddTerranSecondaryDeck(RaceMeshWriter w, float l, float bw, float bh, int sections, float zStart, float zEnd)
    {
        for (int i = 0; i < sections; i++)
        {
            float t0 = i / (float)sections;
            float t1 = (i + 1) / (float)sections;
            float z0 = MathHelper.Lerp(l * zStart, l * zEnd, t0);
            float z1 = MathHelper.Lerp(l * zStart, l * zEnd, t1);
            float r0 = bw * 0.14f * (1f - t0 * 0.3f);
            float r1 = bw * 0.14f * (1f - t1 * 0.3f);
            float h0 = bh * 0.52f;
            float h1 = bh * 0.52f;
            w.Tri(-r0, h0, z0, r0, h0, z0, 0, h0 + bh * 0.08f, z0 + (z1 - z0) * 0.4f);
            w.Tri(-r0, h0, z0, 0, h0 + bh * 0.08f, z0 + (z1 - z0) * 0.4f, -r1, h1, z1);
            w.Tri(r0, h0, z0, r1, h1, z1, 0, h0 + bh * 0.08f, z0 + (z1 - z0) * 0.4f);
        }
    }

    private static void BuildRetroFighterSolid(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var (l, bw, bh) = TerranDims(len, wid, hgt, 0.96f, 0.92f);
        bh *= 1.18f;
        const float zStart = -0.24f;
        const float zEnd = 0.54f;
        const float bowPush = 1.28f;
        const float splitT = 0.56f;
        AddTerranTwoBoxFuselage(w, l, bw, bh, zStart, zEnd, bowPush: bowPush);
        AddTerranBowWedge(w, bw, bh, l, 0.74f, 0.09f, extendedProw: true, dorsalCap: false);
        AddTerranCenterlineEngineCluster(w, bw, bh, l, sternZFrac: -0.30f);
        float shoulderZ = TerranCapitalShoulderZ(l, zStart, zEnd, bowPush, splitT);
        // wo-mesh-01-01: flush dorsal shoulder panel — wider coplanar deck read, fewer fuselage slivers.
        AddBox(w, -bw * 0.12f, bw * 0.12f, bh * 0.18f, bh * 0.24f, shoulderZ - l * 0.01f, shoulderZ + l * 0.03f);
    }

    private static void BuildRetroHeroSolid(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var (l, bw, bh) = TerranDims(len, wid, hgt, 0.92f, 0.76f);
        AddTerranCapitalTwoBoxFuselage(w, l, bw, bh, -0.36f, 0.68f, beamScale: 1.04f, bowPush: 1.12f);
        AddTerranBowWedge(w, bw, bh, l, 0.84f, 0.14f);
        AddTerranDorsalDeckMass(w, bw, bh, l, 0.04f, 0.62f, 0.24f, 1.16f);
        AddTerranDorsalSpine(w, bw, bh, l, 0.08f, 0.34f, segs: 2, height: 0.68f);
        AddTerranIntegratedEngineCluster(w, bw, bh, l, sternZFrac: -0.32f, bellSpread: 0.11f);
        AddTerranSweptWing(w, bw, bh, l, -1f, 1.0f, 0.78f);
        AddTerranSweptWing(w, bw, bh, l, 1f, 1.0f, 0.78f);
        AddBox(w, -bw * 0.06f, bw * 0.06f, bh * 0.66f, bh * 0.90f, l * 0.14f, l * 0.30f);
        AddBox(w, -bw * 0.10f, bw * 0.10f, bh * 0.58f, bh * 0.72f, l * 0.20f, l * 0.26f);
    }

    private static void BuildRetroScoutSolid(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var (l, bw, bh) = TerranDims(len, wid, hgt, 0.92f, 0.72f);
        bh *= 1.22f;
        AddTerranStreamlinedFuselage(w, l, bw, bh, -0.26f, 0.68f, beamScale: 0.78f, heightScale: 0.96f, bowPush: 1.22f);
        AddTerranBowWedge(w, bw, bh, l, 0.86f, 0.09f, extendedProw: true);
        AddTerranDorsalDeckMass(w, bw, bh, l, 0.06f, 0.68f, 0.22f, 1.24f, prowCap: true);
        AddTerranIntegratedEngineCluster(w, bw, bh, l, sternZFrac: -0.28f, bellSpread: 0.06f);
        AddBox(w, -bw * 0.36f, -bw * 0.22f, bh * 0.04f, bh * 0.10f, l * 0.06f, l * 0.12f);
        AddBox(w, bw * 0.22f, bw * 0.36f, bh * 0.04f, bh * 0.10f, l * 0.06f, l * 0.12f);
    }

    private static void BuildRetroInterceptorSolid(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var (l, bw, bh) = TerranDims(len, wid, hgt, 0.90f, 0.80f);
        bh *= 1.10f;
        AddTerranStreamlinedFuselage(w, l, bw, bh, -0.30f, 0.60f, beamScale: 0.90f, heightScale: 0.94f, bowPush: 1.14f);
        AddTerranBowWedge(w, bw, bh, l, 0.80f, 0.11f);
        AddTerranDorsalDeckMass(w, bw, bh, l, 0.04f, 0.56f, 0.18f, 1.16f);
        AddTerranIntegratedEngineCluster(w, bw, bh, l, sternZFrac: -0.30f, bellSpread: 0.08f, afterburner: true);
        AddTerranSweptWing(w, bw, bh, l, -1f, 1.6f, 0.94f, zRootFrac: 0.06f, zSpanFrac: 0.14f, rootLift: 0.18f);
        AddTerranSweptWing(w, bw, bh, l, 1f, 1.6f, 0.94f, zRootFrac: 0.06f, zSpanFrac: 0.14f, rootLift: 0.18f);
        AddBox(w, -bw * 0.12f, bw * 0.12f, bh * 0.08f, bh * 0.16f, l * 0.14f, l * 0.28f);
    }

    private static void BuildRetroDroneSolid(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var (l, bw, bh) = TerranDims(len, wid, hgt, 0.82f, 0.88f);
        bh *= 0.92f;
        AddTerranStreamlinedFuselage(w, l, bw, bh, -0.20f, 0.46f, beamScale: 1.06f, heightScale: 0.82f, bowPush: 1.10f);
        AddTerranBowWedge(w, bw, bh, l, 0.62f, 0.16f);
        AddTerranDorsalDeckMass(w, bw, bh, l, 0.02f, 0.42f, 0.14f, 1.08f);
        AddBox(w, -bw * 0.14f, bw * 0.14f, bh * 0.10f, bh * 0.22f, -l * 0.06f, l * 0.10f);
        for (int q = 0; q < 4; q++)
        {
            float sx = (q % 2 == 0 ? -1f : 1f) * bw * 0.38f;
            float sz = (q < 2 ? -1f : 1f) * l * 0.16f;
            AddBox(w, sx - bw * 0.04f, sx + bw * 0.04f, 0, bh * 0.12f, sz - l * 0.025f, sz + l * 0.025f);
            AddBox(w, sx - bw * 0.025f, sx + bw * 0.025f, bh * 0.02f, bh * 0.10f, sz - l * 0.018f, sz + l * 0.018f);
        }
        AddTerranIntegratedEngineCluster(w, bw, bh, l, sternZFrac: -0.22f, bellSpread: 0.05f);
    }

    private static void BuildRetroCorvetteSolid(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var (l, bw, bh) = TerranDims(len, wid, hgt, 0.88f, 0.78f);
        AddTerranStreamlinedFuselage(w, l, bw, bh, -0.36f, 0.62f, beamScale: 0.96f, bowPush: 1.12f);
        AddTerranBowWedge(w, bw, bh, l, 0.78f, 0.12f);
        AddTerranDorsalDeckMass(w, bw, bh, l, 0.02f, 0.58f, 0.20f, 1.14f);
        AddTerranIntegratedEngineCluster(w, bw, bh, l, sternZFrac: -0.32f, bellSpread: 0.09f);
        AddTerranWeaponPylon(w, bw, bh, l, -1f, l * 0.08f, 0.42f);
        AddTerranWeaponPylon(w, bw, bh, l, 1f, l * 0.08f, 0.42f);
        AddTerranCockpitFacet(w, bw, bh, l);
        AddTerranPanelFacets(w, bw, bh, l, 2, 0.11f);
    }

    private static void BuildRetroFrigateSolid(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var (l, bw, bh) = TerranDims(len, wid, hgt, 0.90f, 0.80f);
        AddTerranStreamlinedFuselage(w, l, bw, bh, -0.40f, 0.68f, beamScale: 1.00f, bowPush: 1.12f);
        AddTerranBowWedge(w, bw, bh, l, 0.82f, 0.13f);
        AddTerranDorsalDeckMass(w, bw, bh, l, -0.02f, 0.62f, 0.22f, 1.14f);
        AddTerranIntegratedEngineCluster(w, bw, bh, l, sternZFrac: -0.34f, bellSpread: 0.10f);
        AddTerranWeaponPylon(w, bw, bh, l, -1f, l * 0.20f, 0.40f);
        AddTerranWeaponPylon(w, bw, bh, l, 1f, l * 0.06f, 0.34f);
        AddBox(w, -bw * 0.12f, bw * 0.12f, bh * 0.12f, bh * 0.24f, l * 0.24f, l * 0.38f);
    }

    private static void BuildRetroGunshipSolid(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var (l, bw, bh) = TerranDims(len, wid, hgt, 0.88f, 0.82f);
        const float zStart = -0.36f;
        const float zEnd = 0.66f;
        const float bowPush = 1.22f;
        const float splitT = 0.56f;
        AddTerranTwoBoxFuselage(w, l, bw, bh, zStart, zEnd, beamScale: 1.00f, bowPush: bowPush);
        AddTerranBowWedge(w, bw, bh, l, 0.82f, 0.10f, extendedProw: true, dorsalCap: false);
        AddBox(w, -bw * 0.20f, bw * 0.20f, bh * 0.24f, bh * 0.52f, l * 0.28f, l * 0.46f);
        AddTerranCenterlineEngineCluster(w, bw, bh, l, sternZFrac: -0.30f);
    }

    private static void BuildRetroBomberSolid(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var (l, bw, bh) = TerranDims(len, wid, hgt, 0.94f, 0.78f);
        const float bowPush = 1.22f;
        AddTerranStreamlinedFuselage(w, l, bw, bh, -0.34f, 0.64f, beamScale: 0.96f, heightScale: 0.94f, bowPush: bowPush);
        AddTerranBowWedge(w, bw, bh, l, 0.82f, 0.10f, extendedProw: true);
        float shoulderZ = TerranCapitalShoulderZ(l, -0.34f, 0.64f, bowPush, 0.56f);
        AddTerranDorsalDeckMass(w, bw, bh, l, 0.02f, 0.58f, 0.26f, 1.18f, prowCap: true, yBaseFrac: 0.32f);
        AddTerranDorsalSpine(w, bw, bh, l, 0.06f, 0.32f, segs: 2, height: 0.92f);
        AddBox(w, -bw * 0.22f, bw * 0.22f, bh * 0.38f, bh * 0.52f, l * 0.06f, l * 0.20f);
        AddBox(w, -bw * 0.12f, bw * 0.12f, bh * 0.48f, bh * 0.58f, l * 0.10f, l * 0.18f);
        AddBox(w, -bw * 0.08f, bw * 0.08f, bh * 0.22f, bh * 0.34f, l * 0.52f, l * 0.62f);
        AddTerranIntegratedEngineCluster(w, bw, bh, l, sternZFrac: -0.28f, bellSpread: 0.10f);
        float wingRootFrac = shoulderZ / l;
        AddTerranSweptWing(w, bw, bh, l, -1f, 0.84f, 0.62f, zRootFrac: wingRootFrac, zSpanFrac: 0.08f, rootLift: 0.14f);
        AddTerranSweptWing(w, bw, bh, l, 1f, 0.84f, 0.62f, zRootFrac: wingRootFrac, zSpanFrac: 0.08f, rootLift: 0.14f);
        for (int side = -1; side <= 1; side += 2)
        {
            float s = MathF.Sign(side);
            float xIn = s * bw * 0.20f;
            float xOut = s * bw * 0.32f;
            AddBox(w, MathF.Min(xIn, xOut), MathF.Max(xIn, xOut), bh * 0.12f, bh * 0.24f, shoulderZ - l * 0.03f, shoulderZ + l * 0.03f);
        }
    }

    private static void BuildRetroDestroyerSolid(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var (l, bw, bh) = TerranDims(len, wid, hgt, 0.94f, 0.88f);
        const float zStart = -0.26f;
        const float zEnd = 0.58f;
        const float bowPush = 1.10f;
        AddTerranTwoBoxFuselage(w, l, bw, bh, zStart, zEnd, beamScale: 1.12f, heightScale: 0.86f, bowPush: bowPush);
        AddTerranBowWedge(w, bw, bh, l, 0.78f, 0.10f, extendedProw: true, dorsalCap: false);
        AddBox(w, -bw * 0.18f, bw * 0.18f, bh * 0.30f, bh * 0.48f, l * 0.16f, l * 0.40f);
        AddBox(w, -bw * 0.08f, bw * 0.08f, bh * 0.36f, bh * 0.44f, l * 0.22f, l * 0.30f);
        AddTerranCenterlineEngineCluster(w, bw, bh, l, sternZFrac: -0.30f, afterburner: true);
    }

    private static void BuildRetroCruiserSolid(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var (l, bw, bh) = TerranDims(len, wid, hgt, 0.90f, 1.14f);
        bh *= 1.08f;
        const float zStart = -0.44f;
        const float zEnd = 0.68f;
        const float bowPush = 1.14f;
        const float splitT = 0.56f;
        const float beamScale = 1.10f;
        float beamEdge = bw * 0.70f * beamScale;
        AddTerranCapitalTwoBoxFuselage(w, l, bw, bh, zStart, zEnd, beamScale: beamScale, bowPush: bowPush, splitT: splitT);
        AddTerranBowWedge(w, bw, bh, l, 0.74f, 0.14f, extendedProw: true);
        AddTerranDorsalDeckMass(w, bw, bh, l, -0.04f, 0.30f, 0.28f, 1.18f, yBaseFrac: 0.52f);
        AddTerranDorsalSpine(w, bw, bh, l, 0.02f, 0.32f, segs: 3, height: 0.74f);
        AddTerranIntegratedEngineCluster(w, bw, bh, l, sternZFrac: -0.34f, bellSpread: 0.13f, afterburner: true);
        float shoulderZ = TerranCapitalShoulderZ(l, zStart, zEnd, bowPush, splitT);
        float wingRootFrac = shoulderZ / l - 0.02f;
        AddTerranSweptWing(w, bw, bh, l, -1f, 0.82f, 0.76f, zRootFrac: wingRootFrac, zSpanFrac: 0.10f, rootLift: 0.16f,
            rootInFrac: 0.728f, flushRootY: true);
        AddTerranSweptWing(w, bw, bh, l, 1f, 0.82f, 0.76f, zRootFrac: wingRootFrac, zSpanFrac: 0.10f, rootLift: 0.16f,
            rootInFrac: 0.728f, flushRootY: true);
        AddTerranWeaponBarbette(w, bw, bh, l, -1f, shoulderZ, beamEdge, 0.18f);
        AddTerranWeaponBarbette(w, bw, bh, l, 1f, shoulderZ, beamEdge, 0.18f);
        AddBox(w, -bw * 0.10f, bw * 0.10f, bh * 0.48f, bh * 0.62f, l * 0.08f, l * 0.18f);
    }

    private static void BuildRetroCarrierSolid(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var (l, bw, bh) = TerranDims(len, wid, hgt, 0.92f, 1.12f);
        bh *= 0.88f;
        AddTerranStreamlinedFuselage(w, l, bw, bh, -0.44f, 0.58f, beamScale: 1.08f, heightScale: 0.72f, bowPush: 1.06f);
        AddBox(w, -bw * 0.52f, bw * 0.52f, bh * 0.22f, bh * 0.32f, -l * 0.32f, l * 0.22f);
        AddBox(w, -bw * 0.48f, -bw * 0.08f, bh * 0.30f, bh * 0.42f, -l * 0.18f, l * 0.06f);
        AddBox(w, bw * 0.10f, bw * 0.38f, bh * 0.28f, bh * 0.38f, -l * 0.10f, l * 0.14f);
        AddBox(w, -bw * 0.08f, bw * 0.08f, bh * 0.32f, bh * 0.48f, l * 0.04f, l * 0.12f);
        AddBox(w, -bw * 0.14f, bw * 0.14f, 0, bh * 0.18f, l * 0.48f, l * 0.58f);
        AddTerranIntegratedEngineCluster(w, bw, bh, l, sternZFrac: -0.36f, bellSpread: 0.13f);
        AddTerranWeaponBarbette(w, bw, bh, l, -1f, l * 0.02f);
        AddTerranWeaponBarbette(w, bw, bh, l, 1f, l * 0.02f);
    }

    private static void BuildRetroDreadnoughtSolid(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var (l, bw, bh) = TerranDims(len, wid, hgt, 0.94f, 0.80f);
        bh *= 1.18f;
        const float zStart = -0.48f;
        const float zEnd = 0.78f;
        const float bowPush = 1.20f;
        const float splitT = 0.58f;
        const float beamScale = 1.08f;
        float beamEdge = bw * 0.70f * beamScale;
        AddTerranCapitalTwoBoxFuselage(w, l, bw, bh, zStart, zEnd, beamScale: beamScale, bowPush: bowPush, splitT: splitT);
        AddTerranBowWedge(w, bw, bh, l, 0.90f, 0.16f, extendedProw: true, dorsalCap: false);
        AddTerranDorsalDeckMass(w, bw, bh, l, 0.00f, 0.66f, 0.28f, 1.20f, yBaseFrac: 0.54f);
        AddTerranDorsalSpine(w, bw, bh, l, 0.06f, 0.30f, segs: 2, height: 0.72f);
        AddTerranIntegratedEngineCluster(w, bw, bh, l, sternZFrac: -0.36f, bellSpread: 0.12f, afterburner: true);
        AddBox(w, -beamEdge, -beamEdge * 0.84f, 0, bh * 0.18f, -l * 0.34f, -l * 0.22f);
        AddBox(w, beamEdge * 0.84f, beamEdge, 0, bh * 0.18f, -l * 0.34f, -l * 0.22f);
        float shoulderZ = TerranCapitalShoulderZ(l, zStart, zEnd, bowPush, splitT);
        AddTerranWeaponBarbette(w, bw, bh, l, -1f, shoulderZ, beamEdge, 0.16f);
        AddTerranWeaponBarbette(w, bw, bh, l, 1f, shoulderZ, beamEdge, 0.16f);
    }

    private static void BuildRetroUtilitySolid(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float widthScale = hullKey switch
        {
            "freighter_bulk" => 0.82f,
            "miner_eva" => 0.76f,
            "support_repair" => 0.80f,
            "transport_cargo" => 0.78f,
            _ => 0.78f
        };
        float lengthScale = hullKey switch
        {
            "freighter_bulk" => 0.94f,
            "transport_cargo" => 0.92f,
            _ => 0.88f
        };
        var (l, bw, bh) = TerranDims(len, wid, hgt, lengthScale, widthScale);

        bool isCargoHull = hullKey is "freighter_bulk" or "transport_cargo";
        float cargoBeam = hullKey is "freighter_bulk" ? 1.06f : hullKey is "transport_cargo" ? 1.02f : 1.02f;
        float cargoHeight = hullKey is "freighter_bulk" ? 1.12f : hullKey is "transport_cargo" ? 0.92f : 0.96f;
        float cargoBowPush = hullKey is "freighter_bulk" ? 1.22f : hullKey is "transport_cargo" ? 1.22f : isCargoHull ? 1.16f : 1.08f;
        float cargoBowFrac = hullKey is "freighter_bulk" ? 0.82f : hullKey is "transport_cargo" ? 0.80f : 0.70f;
        float cargoZEnd = hullKey is "freighter_bulk" ? 0.72f : hullKey is "miner_basic" ? 0.58f : hullKey is "transport_cargo" ? 0.72f : isCargoHull ? 0.66f : 0.62f;
        AddTerranThreeBoxWedge(w, l, bw, bh,
            -0.40f, cargoZEnd,
            beamScale: cargoBeam, heightScale: cargoHeight, bowPush: cargoBowPush);
        AddTerranBowWedge(w, bw, bh, l, cargoBowFrac, 0.10f,
            extendedProw: hullKey is "freighter_bulk", dorsalCap: hullKey is "freighter_bulk");
        if (isCargoHull)
            AddTerranDorsalDeckMass(w, bw, bh, l, -0.04f, 0.64f, hullKey is "freighter_bulk" ? 0.20f : 0.20f,
                hullKey is "freighter_bulk" ? 1.20f : 1.18f, prowCap: hullKey is "transport_cargo");
        AddBox(w, -bw * 0.18f, bw * 0.18f, bh * 0.26f, bh * 0.34f, -l * 0.08f, l * 0.18f);
        AddTerranIntegratedEngineCluster(w, bw, bh, l, sternZFrac: -0.32f,
            bellSpread: hullKey is "freighter_bulk" ? 0.10f : 0.08f);

        switch (hullKey)
        {
            case "miner_basic":
                for (int side = -1; side <= 1; side += 2)
                {
                    float xRoot = side * bw * 0.30f;
                    float xTip = side * bw * 0.62f;
                    AddBox(w, MathF.Min(xRoot, xTip), MathF.Max(xRoot, xTip), bh * 0.10f, bh * 0.22f, l * 0.14f, l * 0.22f);
                    AddBox(w, xTip - side * bw * 0.05f, xTip + side * bw * 0.03f, bh * 0.22f, bh * 0.30f, l * 0.24f, l * 0.32f);
                }
                AddBox(w, -bw * 0.08f, bw * 0.08f, bh * 0.44f, bh * 0.62f, l * 0.24f, l * 0.34f);
                AddTerranWeaponPylon(w, bw, bh, l, -1f, l * 0.20f, 0.48f);
                AddTerranWeaponPylon(w, bw, bh, l, 1f, l * 0.20f, 0.48f);
                break;
            case "miner_eva":
                AddBox(w, -bw * 0.14f, bw * 0.14f, bh * 0.48f, bh * 0.78f, l * 0.10f, l * 0.20f);
                AddBox(w, bw * 0.28f, bw * 0.40f, bh * 0.22f, bh * 0.40f, l * 0.02f, l * 0.10f);
                AddBox(w, bw * 0.36f, bw * 0.50f, bh * 0.34f, bh * 0.48f, l * 0.08f, l * 0.16f);
                break;
            case "miner_tractor":
                AddBox(w, -bw * 0.10f, bw * 0.10f, bh * 0.24f, bh * 0.34f, l * 0.08f, l * 0.22f);
                for (int side = -1; side <= 1; side += 2)
                {
                    float x0 = side * bw * 0.16f;
                    float x1 = side * bw * 0.56f;
                    AddBox(w, MathF.Min(x0, x1), MathF.Max(x0, x1), bh * 0.20f, bh * 0.32f, l * 0.04f, l * 0.16f);
                    AddBox(w, x1 + (side > 0 ? 0f : -bw * 0.08f), x1 + (side > 0 ? bw * 0.08f : 0f), bh * 0.30f, bh * 0.42f, l * 0.16f, l * 0.26f);
                }
                AddBox(w, -bw * 0.06f, bw * 0.06f, bh * 0.36f, bh * 0.50f, l * 0.20f, l * 0.30f);
                break;
            case "transport_cargo":
                AddBox(w, -bw * 0.30f, bw * 0.30f, bh * 0.20f, bh * 0.44f, -l * 0.12f, l * 0.08f);
                AddBox(w, -bw * 0.22f, bw * 0.22f, bh * 0.44f, bh * 0.56f, -l * 0.04f, l * 0.04f);
                AddBox(w, -bw * 0.14f, bw * 0.14f, bh * 0.40f, bh * 0.48f, l * 0.02f, l * 0.08f);
                for (int side = -1; side <= 1; side += 2)
                {
                    float s = MathF.Sign(side);
                    float xRoot = s * bw * 0.20f;
                    float xTip = s * bw * 0.28f;
                    AddBox(w, MathF.Min(xRoot, xTip), MathF.Max(xRoot, xTip), bh * 0.12f, bh * 0.18f, l * 0.04f, l * 0.10f);
                }
                break;
            case "freighter_bulk":
                AddBox(w, -bw * 0.36f, bw * 0.36f, bh * 0.20f, bh * 0.44f, -l * 0.26f, l * 0.08f);
                AddBox(w, -bw * 0.30f, bw * 0.30f, bh * 0.44f, bh * 0.58f, -l * 0.16f, l * 0.02f);
                AddBox(w, -bw * 0.22f, bw * 0.22f, bh * 0.58f, bh * 0.66f, -l * 0.10f, l * 0.02f);
                AddBox(w, -bw * 0.34f, bw * 0.34f, 0, bh * 0.14f, -l * 0.30f, -l * 0.18f);
                for (int side = -1; side <= 1; side += 2)
                {
                    float s = MathF.Sign(side);
                    float xRoot = s * bw * 0.24f;
                    float xTip = s * bw * 0.36f;
                    AddBox(w, MathF.Min(xRoot, xTip), MathF.Max(xRoot, xTip), bh * 0.14f, bh * 0.22f, -l * 0.04f, l * 0.08f);
                }
                break;
            case "support_repair":
                AddBox(w, -bw * 0.22f, bw * 0.22f, bh * 0.48f, bh * 0.58f, l * 0.04f, l * 0.16f);
                AddBox(w, -bw * 0.10f, bw * 0.10f, bh * 0.52f, bh * 0.78f, l * 0.06f, l * 0.18f);
                AddBox(w, -bw * 0.06f, bw * 0.06f, bh * 0.68f, bh * 0.74f, l * 0.10f, l * 0.16f);
                for (int side = -1; side <= 1; side += 2)
                {
                    float x0 = side * bw * 0.14f;
                    float x1 = side * bw * 0.52f;
                    AddBox(w, MathF.Min(x0, x1), MathF.Max(x0, x1), bh * 0.38f, bh * 0.50f, -l * 0.02f, l * 0.10f);
                    AddBox(w, x1 + (side > 0 ? 0f : -bw * 0.06f), x1 + (side > 0 ? bw * 0.06f : 0f), bh * 0.48f, bh * 0.60f, l * 0.06f, l * 0.14f);
                }
                break;
        }
    }

    private static void BuildRetroGenericSolid(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var (l, bw, bh) = TerranDims(len, wid, hgt);
        AddTerranStreamlinedFuselage(w, l, bw, bh, -0.38f, 0.62f, bowPush: 1.10f);
        AddTerranBowWedge(w, bw, bh, l);
        AddTerranDorsalDeckMass(w, bw, bh, l, 0.04f, 0.56f, 0.14f, 1.12f);
        AddTerranIntegratedEngineCluster(w, bw, bh, l);
    }

    // â”€â”€ Vasudan (Vesper) â€” 90s elongated reptilian hull, dorsal keel â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static void BuildVasudanHull(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        switch (hullKey)
        {
            case "fighter":
            case "fighter_basic":
                BuildVasudanFighterSolid(w, len, wid, hgt);
                return;
            case "hero":
            case "hero_default":
                BuildVasudanHeroSolid(w, len, wid, hgt);
                return;
            case "scout":
            case "scout_light":
                BuildVasudanScoutSolid(w, len, wid, hgt);
                return;
            case "interceptor":
            case "interceptor_mk2":
                BuildVasudanInterceptorSolid(w, len, wid, hgt);
                return;
            case "drone":
            case "drone_swarm":
                BuildVasudanDroneSolid(w, len, wid, hgt);
                return;
            case "corvette":
            case "corvette_fast":
                BuildVasudanCorvetteSolid(w, len, wid, hgt);
                return;
            case "frigate":
                BuildVasudanFrigateSolid(w, len, wid, hgt);
                return;
            case "gunship":
                BuildVasudanGunshipSolid(w, len, wid, hgt);
                return;
            case "bomber":
                BuildVasudanBomberSolid(w, len, wid, hgt);
                return;
            case "destroyer":
            case "destroyer_assault":
                BuildVasudanDestroyerSolid(w, len, wid, hgt);
                return;
            case "cruiser":
            case "cruiser_heavy":
                BuildVasudanCruiserSolid(w, len, wid, hgt);
                return;
            case "carrier":
            case "carrier_command":
                BuildVasudanCarrierSolid(w, len, wid, hgt);
                return;
            case "dreadnought":
                BuildVasudanDreadnoughtSolid(w, len, wid, hgt);
                return;
            case "miner_basic":
            case "miner_eva":
            case "miner_tractor":
            case "transport_cargo":
            case "freighter_bulk":
            case "support_repair":
                BuildVasudanUtilitySolid(w, hullKey, len, wid, hgt);
                return;
        }

        bool isCompact = hullKey is "scout" or "scout_light";
        float l = len * (isCompact ? 1.02f : 1.1f);
        float bw = wid * (isCompact ? 0.74f : 0.58f);
        float bh = hgt * (isCompact ? 0.92f : 0.74f);
        float zStart = -l * 0.44f;
        float zEnd = l * 0.82f;
        float crestY = 0.72f;
        float sideY = 0.35f;

        int sections = hullKey is "dreadnought" or "carrier" ? 8 : isCompact ? 10 : 6;

        for (int i = 0; i < sections; i++)
        {
            float t0 = i / (float)sections;
            float t1 = (i + 1) / (float)sections;
            float z0 = MathHelper.Lerp(zStart, zEnd, t0);
            float z1 = MathHelper.Lerp(zStart, zEnd, t1);
            float belly = 0.48f + 0.48f * MathF.Sin(t0 * MathF.PI);
            float belly1 = 0.48f + 0.48f * MathF.Sin(t1 * MathF.PI);
            float taper0 = t0 > 0.84f ? (1f - t0) / 0.16f : 1f;
            float taper1 = t1 > 0.84f ? (1f - t1) / 0.16f : 1f;
            float r0 = bw * belly * taper0;
            float r1 = bw * belly1 * taper1;
            float yTop0 = bh * sideY;
            float yTop1 = bh * sideY;

            w.Tri(-r0, yTop0, z0, r0, yTop0, z0, 0, bh * crestY, z0 + (z1 - z0) * 0.45f);
            w.Tri(-r0, yTop0, z0, 0, bh * crestY, z0 + (z1 - z0) * 0.45f, -r1, yTop1, z1);
            w.Tri(r0, yTop0, z0, r1, yTop1, z1, 0, bh * crestY, z0 + (z1 - z0) * 0.45f);
            w.Tri(-r0, 0, z0, -r1, 0, z1, r0, 0, z0);
            w.Tri(r0, 0, z0, -r1, 0, z1, r1, 0, z1);
            w.Tri(-r0, 0, z0, -r0, yTop0, z0, -r1, yTop1, z1);
            w.Tri(-r0, 0, z0, -r1, yTop1, z1, -r1, 0, z1);
            w.Tri(r0, 0, z0, r1, yTop1, z1, r0, yTop0, z0);
            w.Tri(r0, 0, z0, r1, 0, z1, r1, yTop1, z1);
        }

        w.Tri(-bw * 0.35f, 0, zEnd, bw * 0.35f, 0, zEnd, 0, bh * crestY * 0.85f, zEnd + l * 0.04f);
        w.Tri(-bw * 0.35f, 0, zStart, 0, bh * sideY * 0.6f, zStart - l * 0.03f, bw * 0.35f, 0, zStart);

        for (int i = 0; i < sections - 1; i++)
        {
            float t = (i + 0.5f) / sections;
            float z = MathHelper.Lerp(zStart, zEnd, t);
            w.Tri(0, bh * 0.78f, z, -bw * 0.1f, bh * 0.62f, z - bw * 0.14f, bw * 0.1f, bh * 0.62f, z - bw * 0.14f);
        }

        if (hullKey is not "drone")
        {
            w.Tri(-bw * 0.42f, bh * 0.28f, zStart + (zEnd - zStart) * 0.18f,
                bw * 0.42f, bh * 0.28f, zStart + (zEnd - zStart) * 0.18f,
                0, bh * 0.48f, zStart + (zEnd - zStart) * 0.08f);
        }
    }

    /// <summary>Closed vasudan fighter â€” 2010s hard-surface layered hull (solid, no see-through).</summary>
    private static void BuildVasudanFighterSolid(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        var canopy = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;

        float hw = wid * 0.5f;

        AddBoxMat(w, hull, -hw * 0.42f, hw * 0.42f, 0, hgt * 0.38f, -len * 0.06f, len * 0.24f);
        AddBoxMat(w, frame, -hw * 0.20f, hw * 0.20f, hgt * 0.36f, hgt * 0.52f, len * 0.04f, len * 0.18f);

        AddVasudanFighterNose(w, hull, frame, canopy, hw, hgt, len);
        AddVasudanFighterCanopyLite(w, canopy, hw, hgt, len);
        AddVasudanModernWingLite(w, hull, canopy, hw, hgt, len, -1f);
        AddVasudanModernWingLite(w, hull, canopy, hw, hgt, len, 1f);

        AddVasudanFighterTail(w, hull, frame, canopy, hw, hgt, len);
        AddVasudanEngineNacelle(w, hull, engine, canopy, -hw * 0.18f, hw * 0.06f, hgt, len, gameplayWide: true);
        AddVasudanEngineNacelle(w, hull, engine, canopy, hw * 0.06f, hw * 0.18f, hgt, len, gameplayWide: true);

        AddVasudanFighterHardpointsCompact(w, hull, RaceMeshWriter.HullMaterial.Weapon,
            RaceMeshWriter.HullMaterial.ShieldGen, hw, hgt, len);
    }

    /// <summary>Utility/logistics hulls — shared vasudan substrate with role-distinct attachments.</summary>
    private static void BuildVasudanUtilitySolid(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float hw = wid * 0.5f;
        float widthScale = hullKey is "freighter_bulk" ? 0.90f : hullKey is "miner_eva" ? 1.02f : 0.96f;
        hw *= widthScale;

        AddVasudanUtilityFuselage(w, hull, frame, accent, hw, hgt, len, hullKey);
        AddVasudanUtilityLateralSponsons(w, hull, accent, hw, hgt, len, hullKey);
        if (hullKey is "miner_basic")
            AddVasudanDorsalKeel(w, hull, accent, hw, hgt, len, hullKey);

        switch (hullKey)
        {
            case "miner_basic": AddVasudanMiningArms(w, hull, accent, weapon, hw, hgt, len); break;
            case "miner_eva":
                AddVasudanEvaPod(w, hull, accent, frame, hw, hgt, len);
                AddVasudanEvaDockArm(w, hull, accent, frame, hw, hgt, len);
                break;
            case "miner_tractor": AddVasudanTractorApparatus(w, hull, accent, weapon, hw, hgt, len); break;
            case "transport_cargo":
                AddVasudanCargoSpine(w, hull, frame, accent, hw, hgt, len);
                AddVasudanCargoBays(w, hull, frame, accent, hw, hgt, len, 1, hullKey);
                AddVasudanDorsalKeel(w, hull, accent, hw, hgt, len, hullKey);
                break;
            case "freighter_bulk":
                AddVasudanBulkCargoSpine(w, hull, frame, accent, hw, hgt, len);
                AddVasudanCargoBays(w, hull, frame, accent, hw, hgt, len, 2, hullKey);
                AddVasudanDorsalKeel(w, hull, accent, hw, hgt, len, hullKey);
                break;
            case "support_repair":
                AddVasudanRepairBooms(w, hull, accent, frame, hw, hgt, len);
                AddVasudanRepairAntenna(w, hull, accent, frame, hw, hgt, len);
                break;
        }

        bool gameplayWide = hullKey is not "miner_basic";
        AddVasudanEngineNacelle(w, hull, engine, accent, -hw * 0.20f, -hw * 0.06f, hgt, len, gameplayWide: gameplayWide);
        AddVasudanEngineNacelle(w, hull, engine, accent, hw * 0.06f, hw * 0.20f, hgt, len, gameplayWide: gameplayWide);
        AddVasudanUtilityBridge(w, hull, accent, hw, hgt, len);
        if (hullKey is not "miner_basic")
        {
            var shield = RaceMeshWriter.HullMaterial.ShieldGen;
            w.TriMat(shield, -hw * 0.05f, hgt * 0.66f, len * 0.08f, hw * 0.05f, hgt * 0.66f, len * 0.08f, 0, hgt * 0.70f, len * 0.10f);
        }
    }

    private static void AddVasudanUtilityFuselage(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial frame,
        RaceMeshWriter.HullMaterial accent, float hw, float hgt, float len, string hullKey)
    {
        float bellyW = hw * (hullKey is "freighter_bulk" ? 0.38f : hullKey is "transport_cargo" ? 0.36f : 0.38f);
        float sternZ = hullKey switch
        {
            "freighter_bulk" => -len * 0.42f,
            "transport_cargo" => -len * 0.38f,
            "miner_basic" => -len * 0.36f,
            _ => -len * 0.36f
        };
        float bowZ = hullKey switch
        {
            "freighter_bulk" => len * 0.76f,
            "transport_cargo" => len * 0.72f,
            "miner_basic" => len * 0.60f,
            _ => len * 0.54f
        };

        AddBoxMat(w, hull, -bellyW, bellyW, 0, hgt * 0.42f, sternZ, bowZ * 0.74f);
        AddBoxMat(w, frame, -bellyW * 0.55f, bellyW * 0.55f, hgt * 0.40f, hgt * 0.56f, sternZ + len * 0.02f, bowZ * 0.60f);

        float noseBase = bowZ * 0.74f;
        float noseTip = bowZ;
        w.TriMat(hull, -bellyW * 0.26f, hgt * 0.14f, noseBase, bellyW * 0.26f, hgt * 0.14f, noseBase, 0, hgt * 0.46f, noseTip * 0.94f);
        w.TriMat(accent, -hw * 0.05f, hgt * 0.50f, noseTip * 0.84f, hw * 0.05f, hgt * 0.50f, noseTip * 0.84f, 0, hgt * 0.56f, noseTip);

        int ridgeSegs = hullKey is "freighter_bulk" ? 2 : hullKey is "transport_cargo" ? 2 : hullKey is "miner_basic" ? 2 : 4;
        for (int i = 0; i < ridgeSegs; i++)
        {
            float t = i / MathF.Max(1f, ridgeSegs - 1);
            float z = MathHelper.Lerp(sternZ + len * 0.04f, bowZ * 0.58f, t);
            w.TriMat(accent, 0, hgt * 0.58f, z, -bellyW * 0.10f, hgt * 0.48f, z - len * 0.02f, bellyW * 0.10f, hgt * 0.48f, z - len * 0.02f);
        }
    }

    private static void AddVasudanUtilityLateralSponsons(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial accent,
        float hw, float hgt, float len, string hullKey)
    {
        float reach = hullKey switch
        {
            "freighter_bulk" => hw * 0.56f,
            "transport_cargo" => hw * 0.58f,
            "miner_eva" => hw * 0.72f,
            "support_repair" => hw * 0.70f,
            _ => hw * 0.64f
        };
        float z0 = -len * 0.08f;
        float z1 = len * (hullKey is "freighter_bulk" ? 0.34f : hullKey is "transport_cargo" ? 0.32f : 0.22f);
        float yTop = hgt * 0.18f;

        for (int side = -1; side <= 1; side += 2)
        {
            float xIn = side * hw * 0.36f;
            float xOut = side * reach;
            w.TriMat(accent, xOut, yTop, z0, xIn, yTop, z1, xOut, yTop * 0.55f, z1 - len * 0.02f);
            w.TriMat(hull, xIn, 0, z0, xOut, yTop * 0.55f, z1 - len * 0.02f, xIn, yTop * 0.85f, z1);
            w.TriMat(hull, xIn, 0, z0, xIn, yTop * 0.85f, z1, xOut, yTop * 0.55f, z1 - len * 0.02f);
            if (hullKey is not "transport_cargo" && (hullKey is not "freighter_bulk" || side > 0))
                w.TriMat(accent, xOut, yTop, z0, xOut - side * hw * 0.04f, yTop, z0 - len * 0.01f, xOut, yTop * 0.70f, z1);
        }
    }

    private static void AddVasudanMiningArms(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial accent,
        RaceMeshWriter.HullMaterial weapon, float hw, float hgt, float len, bool trussStyle = false)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            float xRoot = side * hw * 0.36f;
            float xElbow = side * hw * 0.54f;
            float xTip = side * hw * (trussStyle ? 0.68f : 0.82f);
            float zRoot = len * 0.12f;
            float zTip = len * (trussStyle ? 0.58f : 0.58f);
            AddBoxMat(w, weapon, MathF.Min(xRoot, xElbow), MathF.Max(xRoot, xElbow), hgt * 0.08f, hgt * 0.20f, zRoot, zRoot + len * 0.05f);
            w.TriMat(accent, xElbow, hgt * 0.22f, zRoot + len * 0.04f, xTip, hgt * 0.28f, zTip, xElbow, hgt * 0.14f, zTip - len * 0.02f);
            w.TriMat(hull, xElbow, hgt * 0.14f, zTip - len * 0.02f, xTip, hgt * 0.28f, zTip, xTip, hgt * 0.08f, zTip + len * 0.02f);
            w.TriMat(weapon, xTip, hgt * 0.10f, zTip, xTip - hw * 0.03f, hgt * 0.06f, zTip + len * 0.02f, xTip, hgt * 0.14f, zTip + len * 0.03f);
            w.TriMat(accent, xTip, hgt * 0.16f, zTip + len * 0.02f, xTip + side * hw * 0.05f, hgt * 0.22f, zTip + len * 0.04f, xTip, hgt * 0.10f, zTip + len * 0.05f);
        }
    }

    private static void AddVasudanEvaPod(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial accent,
        RaceMeshWriter.HullMaterial frame, float hw, float hgt, float len)
    {
        float podR = hw * 0.42f;
        float podY = hgt * 0.52f;
        float podZ = len * 0.08f;
        for (int i = 0; i < 6; i++)
        {
            float a0 = MathF.PI * 2f * i / 6f;
            float a1 = MathF.PI * 2f * (i + 1) / 6f;
            float x0 = MathF.Cos(a0) * podR;
            float z0 = podZ + MathF.Sin(a0) * podR * 0.55f;
            float x1 = MathF.Cos(a1) * podR;
            float z1 = podZ + MathF.Sin(a1) * podR * 0.55f;
            w.TriMat(hull, x0, podY, z0, x1, podY, z1, 0, podY + hgt * 0.18f, podZ);
            w.TriMat(frame, x0, podY * 0.82f, z0, x1, podY * 0.82f, z1, 0, podY * 0.72f, podZ);
        }
        AddBoxMat(w, accent, -hw * 0.08f, hw * 0.08f, hgt * 0.68f, hgt * 0.78f, len * 0.14f, len * 0.20f);
    }

    private static void AddVasudanTractorApparatus(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial accent,
        RaceMeshWriter.HullMaterial weapon, float hw, float hgt, float len)
    {
        float dishZ = len * 0.42f;
        float dishR = hw * 0.26f;
        for (int i = 0; i < 8; i++)
        {
            float a0 = MathF.PI * 2f * i / 8f;
            float a1 = MathF.PI * 2f * (i + 1) / 8f;
            w.TriMat(weapon, MathF.Cos(a0) * dishR, hgt * 0.64f, dishZ + MathF.Sin(a0) * dishR * 0.42f,
                MathF.Cos(a1) * dishR, hgt * 0.64f, dishZ + MathF.Sin(a1) * dishR * 0.42f,
                0, hgt * 0.78f, dishZ + len * 0.03f);
        }
        for (int side = -1; side <= 1; side += 2)
        {
            float x0 = side * hw * 0.14f;
            float x1 = side * hw * 0.70f;
            AddBoxMat(w, hull, MathF.Min(x0, x1), MathF.Max(x0, x1), hgt * 0.22f, hgt * 0.32f, len * 0.02f, len * 0.16f);
            w.TriMat(accent, x1, hgt * 0.34f, len * 0.08f, x1 + side * hw * 0.10f, hgt * 0.42f, len * 0.18f, x1, hgt * 0.28f, len * 0.20f);
            w.TriMat(weapon, x1, hgt * 0.30f, len * 0.04f, x1 + side * hw * 0.06f, hgt * 0.36f, len * 0.10f, x1, hgt * 0.24f, len * 0.12f);
        }
    }

    private static void AddVasudanCargoBays(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial frame,
        RaceMeshWriter.HullMaterial accent, float hw, float hgt, float len, int bayCount, string hullKey)
    {
        bool narrowBay = hullKey is "freighter_bulk" or "transport_cargo";
        for (int b = 0; b < bayCount; b++)
        {
            float t = bayCount == 1 ? 0.5f : b / (float)(bayCount - 1);
            float z = MathHelper.Lerp(-len * 0.02f, len * (narrowBay ? 0.18f : 0.14f), t);
            float bayW = hw * (bayCount >= 3 ? 0.32f : narrowBay ? 0.22f : 0.30f);
            float bayH = hgt * (bayCount >= 3 ? 0.34f : narrowBay ? 0.28f : 0.30f);
            for (int side = -1; side <= 1; side += 2)
            {
                float cx = side * hw * (narrowBay ? 0.40f : 0.52f);
                AddBoxMat(w, hull, cx - bayW * 0.5f, cx + bayW * 0.5f, hgt * 0.06f, hgt * 0.06f + bayH, z - len * 0.04f, z + len * 0.04f);
                if (!narrowBay)
                    w.TriMat(accent, cx - bayW * 0.25f, hgt * 0.06f + bayH, z, cx + bayW * 0.25f, hgt * 0.06f + bayH, z, cx, hgt * 0.06f + bayH + hgt * 0.04f, z + len * 0.008f);
            }
        }
    }

    private static void AddVasudanRepairBooms(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial accent,
        RaceMeshWriter.HullMaterial frame, float hw, float hgt, float len, bool trussStyle = false)
    {
        float boomZ1 = len * (trussStyle ? 0.16f : 0.12f);
        float boomReach = trussStyle ? 0.58f : 0.82f;
        AddBoxMat(w, frame, -hw * 0.12f, hw * 0.12f, hgt * 0.56f, hgt * 0.80f, len * 0.02f, boomZ1);
        for (int side = -1; side <= 1; side += 2)
        {
            float x0 = side * hw * 0.12f;
            float x1 = side * hw * boomReach;
            float z0 = -len * 0.04f;
            float z1 = boomZ1;
            AddBoxMat(w, hull, MathF.Min(x0, x1), MathF.Max(x0, x1), hgt * 0.42f, hgt * 0.54f, z0, z1);
            w.TriMat(accent, x1, hgt * 0.56f, z1, x1 + side * hw * 0.12f, hgt * 0.66f, z1 + len * 0.05f, x1, hgt * 0.50f, z1 + len * 0.07f);
            w.TriMat(RaceMeshWriter.HullMaterial.Weapon, x1, hgt * 0.64f, z1 + len * 0.04f,
                x1 + side * hw * 0.04f, hgt * 0.58f, z1 + len * 0.05f, x1, hgt * 0.52f, z1 + len * 0.06f);
        }
    }

    private static void AddVasudanRepairAntenna(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial accent,
        RaceMeshWriter.HullMaterial frame, float hw, float hgt, float len, bool trussStyle = false)
    {
        float mastZEnd = len * (trussStyle ? 0.14f : 0.10f);
        AddBoxMat(w, frame, -hw * 0.04f, hw * 0.04f, hgt * 0.78f, hgt * 0.96f, len * 0.04f, mastZEnd);
        w.TriMat(accent, -hw * 0.14f, hgt * 0.88f, len * 0.08f, hw * 0.14f, hgt * 0.88f, len * 0.08f, 0, hgt * 1.02f, mastZEnd);
        w.TriMat(hull, -hw * 0.06f, hgt * 0.82f, len * 0.06f, hw * 0.06f, hgt * 0.82f, len * 0.06f, 0, hgt * 0.90f, len * 0.08f);
        if (trussStyle)
        {
            w.TriMat(accent, -hw * 0.06f, hgt * 0.96f, mastZEnd * 0.92f, hw * 0.06f, hgt * 0.96f, mastZEnd * 0.92f, 0, hgt * 1.08f, mastZEnd + len * 0.02f);
            AddKorathDorsalKeelBand(w, hw, hgt * 0.44f, len * 0.04f, mastZEnd + len * 0.02f, 3);
        }
    }

    private static void AddVasudanEvaDockArm(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial accent,
        RaceMeshWriter.HullMaterial frame, float hw, float hgt, float len)
    {
        float xArm = hw * 0.56f;
        AddBoxMat(w, frame, hw * 0.34f, xArm + hw * 0.06f, hgt * 0.34f, hgt * 0.42f, len * 0.02f, len * 0.10f);
        w.TriMat(accent, xArm, hgt * 0.44f, len * 0.08f, xArm + hw * 0.10f, hgt * 0.50f, len * 0.12f, xArm, hgt * 0.38f, len * 0.14f);
        w.TriMat(hull, hw * 0.30f, hgt * 0.36f, len * 0.04f, xArm, hgt * 0.44f, len * 0.08f, hw * 0.30f, hgt * 0.40f, len * 0.10f);
    }

    private static void AddVasudanCargoSpine(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial frame,
        RaceMeshWriter.HullMaterial accent, float hw, float hgt, float len, bool trussStyle = false)
    {
        float zEnd = len * (trussStyle ? 0.46f : 0.42f);
        for (int s = 0; s < 2; s++)
        {
            float t = s;
            float z = MathHelper.Lerp(-len * 0.02f, zEnd, t);
            float halfW = hw * MathHelper.Lerp(0.10f, 0.05f, t);
            AddBoxMat(w, hull, -halfW, halfW, hgt * 0.38f, hgt * 0.54f, z - len * 0.04f, z + len * 0.04f);
            if (s == 1)
                w.TriMat(accent, -halfW * 0.7f, hgt * 0.56f, z, halfW * 0.7f, hgt * 0.56f, z, 0, hgt * 0.60f, z + len * 0.010f);
        }
        AddBoxMat(w, frame, -hw * 0.05f, hw * 0.05f, hgt * 0.52f, hgt * 0.58f, -len * 0.06f, zEnd);
    }

    private static void AddVasudanBulkCargoSpine(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial frame,
        RaceMeshWriter.HullMaterial accent, float hw, float hgt, float len, bool trussStyle = false)
    {
        float zEnd = len * (trussStyle ? 0.48f : 0.44f);
        float halfW = hw * 0.07f;
        AddBoxMat(w, hull, -halfW, halfW, hgt * 0.36f, hgt * 0.52f, -len * 0.04f, zEnd + len * 0.04f);
        w.TriMat(accent, -halfW * 0.65f, hgt * 0.54f, zEnd * 0.76f, halfW * 0.65f, hgt * 0.54f, zEnd * 0.76f, 0, hgt * 0.60f, zEnd);
        AddBoxMat(w, frame, -hw * 0.06f, hw * 0.06f, hgt * 0.50f, hgt * 0.56f, -len * 0.08f, zEnd);
    }

    /// <summary>Narrow dorsal keel ridge along +Z — vasudan industrial spine without widening beam.</summary>
    private static void AddVasudanDorsalKeel(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial accent,
        float hw, float hgt, float len, string hullKey, bool trussStyle = false)
    {
        float keelHalfW = hw * (hullKey is "freighter_bulk" ? 0.045f : 0.040f);
        float yBase = hgt * 0.52f;
        float yCrest = hgt * (hullKey is "freighter_bulk" ? 0.64f : 0.60f);
        float zEnd = len * (hullKey switch
        {
            "freighter_bulk" => trussStyle ? 0.48f : 0.44f,
            "transport_cargo" => trussStyle ? 0.46f : 0.42f,
            "miner_basic" => trussStyle ? 0.27f : 0.24f,
            "miner_eva" => 0.22f,
            "miner_tractor" => 0.26f,
            _ => 0.18f
        });
        int keelSegs = hullKey is "freighter_bulk" or "transport_cargo" or "miner_basic" or "miner_eva" or "miner_tractor" ? 2 : 2;
        for (int i = 0; i < keelSegs; i++)
        {
            float t = i / MathF.Max(1f, keelSegs - 1);
            float z = MathHelper.Lerp(-len * 0.02f, zEnd, t);
            w.TriMat(hull, -keelHalfW, yBase, z, keelHalfW, yBase, z, 0, yCrest, z + len * 0.014f);
            if ((hullKey is "freighter_bulk" && i == 0) || (hullKey is not "freighter_bulk" and not "transport_cargo" && i % 2 == 0))
                w.TriMat(accent, -keelHalfW * 0.6f, yCrest * 0.96f, z + len * 0.006f, keelHalfW * 0.6f, yCrest * 0.96f, z + len * 0.006f, 0, yCrest, z + len * 0.014f);
        }
    }

    private static void AddVasudanUtilityBridge(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial accent, float hw, float hgt, float len)
    {
        AddBoxMat(w, hull, -hw * 0.14f, hw * 0.14f, hgt * 0.48f, hgt * 0.68f, len * 0.04f, len * 0.14f);
        AddBoxMat(w, accent, -hw * 0.10f, hw * 0.10f, hgt * 0.66f, hgt * 0.72f, len * 0.06f, len * 0.12f);
    }

    /// <summary>Flagship hero — elongated command superstructure, refined dorsal bridge.</summary>
    private static void BuildVasudanHeroSolid(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        var canopy = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float hw = wid * 0.5f;

        AddBoxMat(w, hull, -hw * 0.44f, hw * 0.44f, 0, hgt * 0.40f, -len * 0.08f, len * 0.40f);
        AddBoxMat(w, frame, -hw * 0.08f, hw * 0.08f, hgt * 0.36f, hgt * 0.42f, -len * 0.04f, len * 0.30f);
        AddBoxMat(w, frame, -hw * 0.22f, hw * 0.22f, hgt * 0.38f, hgt * 0.56f, len * 0.02f, len * 0.24f);
        AddVasudanFighterNose(w, hull, frame, canopy, hw, hgt, len);
        AddVasudanHeroFlagshipProw(w, hull, frame, canopy, hw, hgt, len);
        AddBoxMat(w, canopy, -hw * 0.18f, hw * 0.18f, hgt * 0.50f, hgt * 0.84f, len * 0.08f, len * 0.26f);
        AddBoxMat(w, frame, -hw * 0.05f, hw * 0.05f, hgt * 0.88f, hgt * 0.98f, len * 0.08f, len * 0.16f);
        w.TriMat(canopy, -hw * 0.04f, hgt * 0.96f, len * 0.10f, hw * 0.04f, hgt * 0.96f, len * 0.10f,
            0, hgt * 1.04f, len * 0.14f);
        AddVasudanWingLiteParam(w, hull, canopy, hw, hgt, len, -1f, 1.10f, 0.14f);
        AddVasudanWingLiteParam(w, hull, canopy, hw, hgt, len, 1f, 1.10f, 0.14f);
        AddVasudanFighterTail(w, hull, frame, canopy, hw, hgt, len);
        AddVasudanEngineNacelle(w, hull, engine, canopy, -hw * 0.20f, hw * 0.04f, hgt, len, gameplayWide: true);
        AddVasudanEngineNacelle(w, hull, engine, canopy, hw * 0.04f, hw * 0.20f, hgt, len, gameplayWide: true);
        AddBoxMat(w, shield, -hw * 0.11f, hw * 0.11f, hgt * 0.74f, hgt * 0.86f, len * 0.04f, len * 0.13f);
    }

    /// <summary>Lean scout — needle nose, narrow wings, minimal mass.</summary>
    private static void BuildVasudanScoutSolid(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        var canopy = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float hw = wid * 0.5f;

        AddBoxMat(w, hull, -hw * 0.30f, hw * 0.30f, 0, hgt * 0.34f, -len * 0.02f, len * 0.20f);
        AddBoxMat(w, frame, -hw * 0.12f, hw * 0.12f, hgt * 0.30f, hgt * 0.46f, len * 0.04f, len * 0.14f);
        float baseZ = len * 0.24f;
        float tipZ = len * 0.78f;
        w.TriMat(hull, -hw * 0.12f, hgt * 0.10f, baseZ, hw * 0.12f, hgt * 0.10f, baseZ, 0, hgt * 0.52f, tipZ);
        AddBoxMat(w, canopy, -hw * 0.10f, hw * 0.10f, hgt * 0.38f, hgt * 0.62f, len * 0.08f, len * 0.20f);
        AddVasudanWingLiteParam(w, hull, canopy, hw, hgt, len, -1f, 1.85f, 0.14f);
        AddVasudanWingLiteParam(w, hull, canopy, hw, hgt, len, 1f, 1.85f, 0.14f);
        AddBoxMat(w, canopy, -hw * 0.04f, hw * 0.04f, hgt * 0.72f, hgt * 0.96f, len * 0.04f, len * 0.14f);
        AddVasudanFighterTail(w, hull, frame, canopy, hw, hgt, len);
        AddVasudanEngineNacelle(w, hull, engine, canopy, -hw * 0.10f, hw * 0.10f, hgt, len);
        AddBoxMat(w, shield, -hw * 0.08f, hw * 0.08f, hgt * 0.62f, hgt * 0.74f, len * 0.03f, len * 0.12f);
        AddBoxMat(w, weapon, -hw * 0.56f, -hw * 0.46f, hgt * 0.08f, hgt * 0.16f, len * 0.03f, len * 0.12f);
        AddBoxMat(w, weapon, hw * 0.46f, hw * 0.56f, hgt * 0.08f, hgt * 0.16f, len * 0.03f, len * 0.12f);
    }

    /// <summary>Aggressive interceptor — forward sweep, weapon-forward pods.</summary>
    private static void BuildVasudanInterceptorSolid(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        var canopy = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float hw = wid * 0.5f;

        AddBoxMat(w, hull, -hw * 0.34f, hw * 0.34f, 0, hgt * 0.36f, -len * 0.04f, len * 0.22f);
        float baseZ = len * 0.28f;
        float tipZ = len * 0.72f;
        w.TriMat(hull, -hw * 0.14f, hgt * 0.10f, baseZ, hw * 0.14f, hgt * 0.10f, baseZ, 0, hgt * 0.50f, tipZ);
        AddBoxMat(w, canopy, -hw * 0.12f, hw * 0.12f, hgt * 0.42f, hgt * 0.72f, len * 0.10f, len * 0.20f);
        AddVasudanWingLiteParam(w, hull, canopy, hw, hgt, len, -1f, 1.12f, 0.20f);
        AddVasudanWingLiteParam(w, hull, canopy, hw, hgt, len, 1f, 1.12f, 0.20f);
        AddBoxMat(w, weapon, -hw * 0.10f, hw * 0.10f, hgt * 0.10f, hgt * 0.20f, len * 0.26f, len * 0.38f);
        w.TriMat(weapon, -hw * 0.06f, hgt * 0.20f, len * 0.34f, hw * 0.06f, hgt * 0.20f, len * 0.34f, 0, hgt * 0.26f, len * 0.38f);
        AddBoxMat(w, weapon, -hw * 0.60f, -hw * 0.50f, hgt * 0.08f, hgt * 0.16f, len * 0.20f, len * 0.32f);
        AddBoxMat(w, weapon, hw * 0.50f, hw * 0.60f, hgt * 0.08f, hgt * 0.16f, len * 0.20f, len * 0.32f);
        AddVasudanFighterTail(w, hull, frame, canopy, hw, hgt, len);
        AddVasudanEngineNacelle(w, hull, engine, canopy, -hw * 0.16f, hw * 0.04f, hgt, len);
        AddVasudanEngineNacelle(w, hull, engine, canopy, hw * 0.04f, hw * 0.16f, hgt, len);
    }

    /// <summary>Swarm drone — elongated spine with fore/aft micro-pods along +Z.</summary>
    private static void BuildVasudanDroneSolid(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        var canopy = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        float hw = wid * 0.5f;

        AddBoxMat(w, hull, -hw * 0.16f, hw * 0.16f, 0, hgt * 0.32f, -len * 0.38f, len * 0.34f);
        // Loop-10 drone_swarm: stern-trimmed prow spine (no +Z stretch — aspect via lateral pods).
        float prowTip = len * 0.88f;
        w.TriMat(hull, -hw * 0.06f, hgt * 0.12f, len * 0.38f, hw * 0.06f, hgt * 0.12f, len * 0.38f, 0, hgt * 0.36f, prowTip);
        w.TriMat(frame, -hw * 0.03f, hgt * 0.32f, len * 0.78f, hw * 0.03f, hgt * 0.32f, len * 0.78f, 0, hgt * 0.42f, prowTip);
        w.TriMat(canopy, -hw * 0.04f, hgt * 0.36f, len * 0.80f, hw * 0.04f, hgt * 0.36f, len * 0.80f, 0, hgt * 0.44f, prowTip);
        AddBoxMat(w, canopy, -hw * 0.05f, hw * 0.05f, hgt * 0.34f, hgt * 0.52f, len * 0.10f, len * 0.26f);
        AddBoxMat(w, canopy, -hw * 0.03f, hw * 0.03f, hgt * 0.52f, hgt * 0.68f, len * 0.06f, len * 0.18f);
        AddBoxMat(w, frame, -hw * 0.03f, hw * 0.03f, hgt * 0.48f, hgt * 0.66f, len * 0.04f, len * 0.22f);
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * hw * 0.26f;
            float xOut = side * hw * 0.30f;
            AddBoxMat(w, hull, MathF.Min(0, x), MathF.Max(0, x), 0, hgt * 0.14f, len * 0.04f, len * 0.12f);
            w.TriMat(canopy, xOut, hgt * 0.22f, len * 0.14f, xOut - side * hw * 0.03f, hgt * 0.18f, len * 0.18f, xOut, hgt * 0.14f, len * 0.20f);
            AddBoxMat(w, engine, MathF.Min(0, x), MathF.Max(0, x), 0, hgt * 0.14f, -len * 0.30f, -len * 0.18f);
        }
        AddBoxMat(w, frame, -hw * 0.08f, hw * 0.08f, hgt * 0.18f, hgt * 0.24f, -len * 0.08f, len * 0.06f);
        AddBoxMat(w, engine, -hw * 0.12f, hw * 0.12f, 0, hgt * 0.16f, -len * 0.36f, -len * 0.28f);
        AddBoxMat(w, canopy, -hw * 0.04f, hw * 0.04f, hgt * 0.20f, hgt * 0.28f, len * 0.08f, len * 0.16f);
    }

    /// <summary>Agile corvette — swept wings, moderate weapon bays, twin nacelles.</summary>
    private static void BuildVasudanCorvetteSolid(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        var canopy = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float hw = wid * 0.5f;

        AddBoxMat(w, hull, -hw * 0.38f, hw * 0.38f, 0, hgt * 0.34f, -len * 0.10f, len * 0.30f);
        AddBoxMat(w, frame, -hw * 0.18f, hw * 0.18f, hgt * 0.32f, hgt * 0.48f, len * 0.02f, len * 0.16f);
        AddVasudanMediumNose(w, hull, canopy, hw, hgt, len, 0.56f);
        AddVasudanWingLiteParam(w, hull, canopy, hw, hgt, len, -1f, 1.14f, 0.14f);
        AddVasudanWingLiteParam(w, hull, canopy, hw, hgt, len, 1f, 1.14f, 0.14f);
        AddVasudanMediumDorsalKeel(w, frame, canopy, hw, hgt, len, 2, 0.18f);
        for (int side = -1; side <= 1; side += 2)
        {
            float xLead = side * hw * 1.02f;
            float zBow = len * 0.08f;
            w.TriMat(canopy, xLead, hgt * 0.22f, zBow,
                xLead - side * hw * 0.04f, hgt * 0.18f, zBow + len * 0.01f,
                xLead, hgt * 0.14f, zBow + len * 0.012f);
            w.TriMat(canopy, xLead, hgt * 0.20f, zBow - len * 0.02f,
                xLead - side * hw * 0.03f, hgt * 0.16f, zBow,
                xLead, hgt * 0.12f, zBow + len * 0.005f);
        }
        AddVasudanFighterTail(w, hull, frame, canopy, hw, hgt, len);
        AddVasudanEngineNacelle(w, hull, engine, canopy, -hw * 0.22f, -hw * 0.06f, hgt, len, gameplayWide: true);
        AddVasudanEngineNacelle(w, hull, engine, canopy, hw * 0.06f, hw * 0.22f, hgt, len, gameplayWide: true);
        AddBoxMat(w, shield, -hw * 0.10f, hw * 0.10f, hgt * 0.70f, hgt * 0.82f, len * 0.04f, len * 0.11f);
        AddBoxMat(w, weapon, -hw * 0.54f, -hw * 0.44f, hgt * 0.04f, hgt * 0.14f, len * 0.02f, len * 0.11f);
        AddBoxMat(w, weapon, hw * 0.44f, hw * 0.54f, hgt * 0.04f, hgt * 0.14f, len * 0.02f, len * 0.11f);
    }

    /// <summary>Balanced strike frigate — layered panels, triple weapon bays, command bridge.</summary>
    private static void BuildVasudanFrigateSolid(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var canopy = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float hw = wid * 0.5f;

        AddBoxMat(w, hull, -hw * 0.42f, hw * 0.42f, 0, hgt * 0.36f, -len * 0.14f, len * 0.38f);
        AddBoxMat(w, frame, -hw * 0.22f, hw * 0.22f, hgt * 0.34f, hgt * 0.52f, len * 0.04f, len * 0.20f);
        AddVasudanMediumNose(w, hull, canopy, hw, hgt, len, 0.62f);
        AddVasudanMediumDorsalKeel(w, frame, canopy, hw, hgt, len, 3, 0.24f);
        AddVasudanWingLiteParam(w, hull, canopy, hw, hgt, len, -1f, 1.22f, 0.12f);
        AddVasudanWingLiteParam(w, hull, canopy, hw, hgt, len, 1f, 1.22f, 0.12f);
        AddVasudanFrigatePanelLite(w, panel, frame, canopy, hw, hgt, len);
        AddVasudanFighterTail(w, hull, frame, canopy, hw, hgt, len);
        AddVasudanEngineNacelle(w, hull, engine, canopy, -hw * 0.24f, -hw * 0.04f, hgt, len, gameplayWide: true);
        AddVasudanEngineNacelle(w, hull, engine, canopy, hw * 0.04f, hw * 0.24f, hgt, len, gameplayWide: true);
        AddBoxMat(w, shield, -hw * 0.10f, hw * 0.10f, hgt * 0.70f, hgt * 0.82f, len * 0.04f, len * 0.11f);
        AddBoxMat(w, weapon, -hw * 0.56f, -hw * 0.44f, hgt * 0.04f, hgt * 0.14f, len * 0.02f, len * 0.11f);
        AddBoxMat(w, weapon, hw * 0.44f, hw * 0.56f, hgt * 0.04f, hgt * 0.14f, len * 0.02f, len * 0.11f);
        AddBoxMat(w, weapon, -hw * 0.10f, hw * 0.10f, hgt * 0.06f, hgt * 0.18f, len * 0.18f, len * 0.32f);
        w.TriMat(canopy, -hw * 0.08f, hgt * 0.20f, len * 0.26f, hw * 0.08f, hgt * 0.20f, len * 0.26f,
            0, hgt * 0.26f, len * 0.30f);
    }

    /// <summary>Heavy gunship — broad dorsal superstructure, quad weapon hardpoints (no wing extension).</summary>
    private static void BuildVasudanGunshipSolid(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        var canopy = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float hw = wid * 0.5f;

        AddBoxMat(w, hull, -hw * 0.42f, hw * 0.42f, 0, hgt * 0.36f, -len * 0.18f, len * 0.32f);
        AddBoxMat(w, frame, -hw * 0.30f, hw * 0.30f, hgt * 0.34f, hgt * 0.56f, -len * 0.06f, len * 0.18f);
        w.TriMat(canopy, -hw * 0.20f, hgt * 0.56f, len * 0.04f, hw * 0.20f, hgt * 0.56f, len * 0.04f,
            0, hgt * 0.68f, len * 0.12f);
        AddVasudanMediumNose(w, hull, canopy, hw, hgt, len, 0.58f);
        AddVasudanMediumDorsalKeel(w, frame, canopy, hw, hgt, len, 2, 0.20f);
        w.TriMat(weapon, -hw * 0.12f, 0, len * 0.14f, hw * 0.12f, 0, len * 0.14f, 0, hgt * 0.08f, len * 0.22f);
        w.TriMat(weapon, -hw * 0.10f, 0, len * 0.20f, hw * 0.10f, 0, len * 0.20f, 0, hgt * 0.08f, len * 0.28f);
        AddVasudanGunshipSponsons(w, hull, weapon, canopy, hw, hgt, len);
        AddVasudanFighterTail(w, hull, frame, canopy, hw, hgt, len);
        AddVasudanEngineNacelle(w, hull, engine, canopy, -hw * 0.22f, -hw * 0.06f, hgt, len, gameplayWide: true);
        AddVasudanEngineNacelle(w, hull, engine, canopy, hw * 0.06f, hw * 0.22f, hgt, len, gameplayWide: true);
        AddBoxMat(w, shield, -hw * 0.10f, hw * 0.10f, hgt * 0.66f, hgt * 0.80f, len * 0.04f, len * 0.11f);
        AddBoxMat(w, weapon, -hw * 0.10f, hw * 0.10f, hgt * 0.06f, hgt * 0.18f, len * 0.14f, len * 0.26f);
    }

    /// <summary>Wide bomber — payload belly bays, dorsal spine accents (fuselage-wide, no wings).</summary>
    private static void BuildVasudanBomberSolid(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        var canopy = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float hw = wid * 0.5f;

        AddBoxMat(w, hull, -hw * 0.44f, hw * 0.44f, 0, hgt * 0.30f, -len * 0.14f, len * 0.32f);
        w.TriMat(weapon, -hw * 0.30f, 0, -len * 0.02f, hw * 0.30f, 0, -len * 0.02f, 0, hgt * 0.10f, len * 0.08f);
        AddBoxMat(w, frame, -hw * 0.18f, hw * 0.18f, hgt * 0.28f, hgt * 0.44f, len * 0.02f, len * 0.16f);
        AddVasudanMediumNose(w, hull, canopy, hw, hgt, len, 0.58f);
        AddVasudanMediumDorsalKeel(w, frame, canopy, hw, hgt, len, 3, 0.22f);
        AddVasudanBomberPayloadBays(w, weapon, frame, canopy, hw, hgt, len);
        AddVasudanFighterTail(w, hull, frame, canopy, hw, hgt, len);
        AddVasudanEngineNacelle(w, hull, engine, canopy, -hw * 0.22f, -hw * 0.06f, hgt, len);
        AddVasudanEngineNacelle(w, hull, engine, canopy, hw * 0.06f, hw * 0.22f, hgt, len);
        AddBoxMat(w, shield, -hw * 0.08f, hw * 0.08f, hgt * 0.66f, hgt * 0.74f, len * 0.04f, len * 0.10f);
        AddBoxMat(w, weapon, -hw * 0.46f, -hw * 0.40f, hgt * 0.04f, hgt * 0.10f, len * 0.02f, len * 0.08f);
        AddBoxMat(w, weapon, hw * 0.40f, hw * 0.46f, hgt * 0.04f, hgt * 0.10f, len * 0.02f, len * 0.08f);
    }

    private static void AddVasudanMediumNose(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial canopy,
        float hw, float hgt, float len, float tipReach)
    {
        float baseZ = len * 0.26f;
        float tipZ = len * tipReach;
        float baseW = hw * 0.26f;
        float midZ = len * (tipReach * 0.72f);
        float midW = hw * 0.14f;

        w.TriMat(hull, -baseW, hgt * 0.14f, baseZ, baseW, hgt * 0.14f, baseZ, 0, hgt * 0.46f, midZ);
        w.TriMat(hull, -midW, hgt * 0.30f, midZ, 0, hgt * 0.56f, tipZ, midW, hgt * 0.30f, midZ);
        w.TriMat(hull, -baseW, 0, baseZ, 0, hgt * 0.14f, tipZ * 0.98f, baseW, 0, baseZ);
        w.TriMat(canopy, -hw * 0.05f, hgt * 0.50f, tipZ * 0.90f, hw * 0.05f, hgt * 0.50f, tipZ * 0.90f,
            0, hgt * 0.54f, tipZ * 0.96f);
    }

    private static void AddVasudanMediumDorsalKeel(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial frame, RaceMeshWriter.HullMaterial accent,
        float hw, float hgt, float len, int segments, float zReach)
    {
        for (int s = 0; s < segments; s++)
        {
            float t = segments > 1 ? s / (segments - 1f) : 0f;
            float z = MathHelper.Lerp(len * 0.06f, len * zReach, t);
            float yBase = hgt * (0.48f + t * 0.16f);
            w.TriMat(frame,
                -hw * 0.05f, yBase, z, hw * 0.05f, yBase, z, 0, yBase + hgt * 0.06f, z + len * 0.005f);
            if (s % 2 == 0)
            {
                w.TriMat(accent,
                    -hw * 0.03f, yBase + hgt * 0.04f, z, hw * 0.03f, yBase + hgt * 0.04f, z,
                    0, yBase + hgt * 0.10f, z + len * 0.004f);
            }
        }
    }

    private static void AddVasudanFrigatePanelLite(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial panel, RaceMeshWriter.HullMaterial frame,
        RaceMeshWriter.HullMaterial accent, float hw, float hgt, float len)
    {
        for (int i = 0; i < 2; i++)
        {
            float t = i;
            float z = MathHelper.Lerp(-len * 0.02f, len * 0.12f, t);
            float inset = hw * (0.30f - t * 0.04f);
            w.TriMat(panel,
                -inset, hgt * 0.22f, z, -inset + hw * 0.04f, hgt * 0.20f, z + len * 0.008f,
                -inset, hgt * 0.18f, z + len * 0.010f);
            w.TriMat(panel,
                inset, hgt * 0.22f, z, inset - hw * 0.04f, hgt * 0.20f, z + len * 0.008f,
                inset, hgt * 0.18f, z + len * 0.010f);
        }
    }

    private static void AddVasudanGunshipSponsons(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial weapon,
        RaceMeshWriter.HullMaterial accent, float hw, float hgt, float len)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            float xIn = side * hw * 0.38f;
            float xOut = side * hw * 0.46f;
            float z0 = len * 0.04f;
            float z1 = -len * 0.06f;
            w.TriMat(accent, xOut, hgt * 0.20f, z0, xOut, hgt * 0.14f, z1, xIn, hgt * 0.18f, z0);
            w.TriMat(hull, xIn, hgt * 0.10f, z1, xOut, hgt * 0.14f, z1, xIn, hgt * 0.18f, z0);
            AddBoxMat(w, weapon, MathF.Min(xIn, xOut), MathF.Max(xIn, xOut), hgt * 0.04f, hgt * 0.14f,
                len * 0.06f, len * 0.12f);
        }
    }

    private static void AddVasudanBomberPayloadBays(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial weapon, RaceMeshWriter.HullMaterial frame,
        RaceMeshWriter.HullMaterial accent, float hw, float hgt, float len)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            float xOut = side * hw * 0.30f;
            float xIn = side * hw * 0.22f;
            w.TriMat(weapon, xOut, 0, -len * 0.02f, xIn, 0, len * 0.02f, xOut, hgt * 0.08f, len * 0.01f);
        }

        for (int p = 0; p < 3; p++)
        {
            float t = p / 2f;
            float z = MathHelper.Lerp(-len * 0.04f, len * 0.20f, t);
            float xSpan = hw * MathHelper.Lerp(0.30f, 0.10f, t);
            w.TriMat(accent,
                -xSpan, hgt * (0.36f + t * 0.04f), z, xSpan, hgt * (0.36f + t * 0.04f), z,
                0, hgt * (0.40f + t * 0.05f), z + len * 0.006f);
        }

        w.TriMat(weapon, -hw * 0.28f, hgt * 0.02f, len * 0.12f, hw * 0.28f, hgt * 0.02f, len * 0.12f,
            0, hgt * 0.08f, len * 0.16f);
    }

    private static void AddVasudanMediumHardpoints(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial weapon,
        RaceMeshWriter.HullMaterial shieldGen, float hw, float hgt, float len,
        int weaponPods, bool dorsalWeapon)
    {
        AddBoxMat(w, shieldGen, -hw * 0.08f, hw * 0.08f, hgt * 0.72f, hgt * 0.80f, len * 0.04f, len * 0.10f);

        for (int side = -1; side <= 1; side += 2)
        {
            float xRoot = side * hw * 0.54f;
            float xPod = side * hw * (0.58f + weaponPods * 0.02f);
            AddBoxMat(w, weapon, MathF.Min(xRoot, xPod), MathF.Max(xRoot, xPod), hgt * 0.04f, hgt * 0.14f,
                len * 0.02f, len * 0.10f);
            if (weaponPods >= 3)
            {
                AddBoxMat(w, weapon, MathF.Min(xRoot - side * hw * 0.02f, xPod), MathF.Max(xRoot - side * hw * 0.02f, xPod),
                    hgt * 0.02f, hgt * 0.08f, -len * 0.02f, len * 0.04f);
            }
            if (weaponPods >= 4)
            {
                AddBoxMat(w, weapon, MathF.Min(xRoot, xPod + side * hw * 0.04f), MathF.Max(xRoot, xPod + side * hw * 0.04f),
                    hgt * 0.10f, hgt * 0.18f, len * 0.08f, len * 0.14f);
            }
        }

        if (dorsalWeapon)
            AddBoxMat(w, weapon, -hw * 0.08f, hw * 0.08f, hgt * 0.08f, hgt * 0.16f, len * 0.18f, len * 0.26f);
    }

    /// <summary>Assault destroyer — dense paneling, forward weapon clusters, lateral sponsons.</summary>
    private static void BuildVasudanDestroyerSolid(RaceMeshWriter w, float len, float wid, float hgt)
        => BuildVasudanCapitalSolidCore(w, len, wid, hgt, CapitalHullProfile.Destroyer);

    /// <summary>Heavy cruiser — tiered dorsal superstructure, reinforced mid-hull.</summary>
    private static void BuildVasudanCruiserSolid(RaceMeshWriter w, float len, float wid, float hgt)
        => BuildVasudanCapitalSolidCore(w, len, wid, hgt, CapitalHullProfile.Cruiser);

    /// <summary>Command carrier — flat flight-deck cues, offset command tower.</summary>
    private static void BuildVasudanCarrierSolid(RaceMeshWriter w, float len, float wid, float hgt)
        => BuildVasudanCapitalSolidCore(w, len, wid, hgt, CapitalHullProfile.Carrier);

    /// <summary>Dreadnought — largest silhouette, imposing prow, heavy spine accents.</summary>
    private static void BuildVasudanDreadnoughtSolid(RaceMeshWriter w, float len, float wid, float hgt)
        => BuildVasudanCapitalSolidCore(w, len, wid, hgt, CapitalHullProfile.Dreadnought);

    private enum CapitalHullProfile { Destroyer, Cruiser, Carrier, Dreadnought }

    private static void BuildVasudanCapitalSolidCore(
        RaceMeshWriter w, float len, float wid, float hgt, CapitalHullProfile profile)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float hw = wid * 0.5f;

        float spanMul = profile switch
        {
            CapitalHullProfile.Destroyer => 1.92f,
            CapitalHullProfile.Cruiser => 1.36f,
            CapitalHullProfile.Carrier => 1.12f,
            _ => 1.58f,
        };
        float prowTip = profile switch
        {
            CapitalHullProfile.Destroyer => 0.99f,
            CapitalHullProfile.Cruiser => 0.98f,
            CapitalHullProfile.Carrier => 0.90f,
            _ => 0.98f,
        };
        float hullBeam = profile is CapitalHullProfile.Cruiser ? 0.34f : 0.38f;
        float hullFwd = profile switch
        {
            CapitalHullProfile.Destroyer => 0.46f,
            CapitalHullProfile.Cruiser => 0.46f,
            _ => 0.44f,
        };
        float prowBaseW = profile is CapitalHullProfile.Cruiser ? 0.22f : 0.26f;
        int spineSegs = profile is CapitalHullProfile.Dreadnought ? 6
            : profile is CapitalHullProfile.Cruiser ? 3
            : profile is CapitalHullProfile.Destroyer ? 4
            : profile is CapitalHullProfile.Carrier ? 3
            : 4;
        int engines = profile is CapitalHullProfile.Destroyer ? 2 : 3;

        AddBoxMat(w, hull, -hw * hullBeam, hw * hullBeam, 0, hgt * 0.32f, -len * 0.32f, len * hullFwd);
        if (profile is CapitalHullProfile.Cruiser)
            AddBoxMat(w, frame, -hw * 0.16f, hw * 0.16f, hgt * 0.32f, hgt * 0.42f, len * 0.02f, len * 0.18f);
        else
            AddBoxMat(w, frame, -hw * 0.18f, hw * 0.18f, hgt * 0.44f, hgt * 0.58f, len * 0.02f, len * 0.18f);
        if (profile is CapitalHullProfile.Destroyer)
            AddBoxMat(w, frame, -hw * 0.12f, hw * 0.12f, hgt * 0.58f, hgt * 0.74f, len * 0.06f, len * 0.14f);
        if (profile is CapitalHullProfile.Cruiser)
            AddBoxMat(w, frame, -hw * 0.28f, hw * 0.28f, hgt * 0.04f, hgt * 0.16f, -len * 0.06f, len * 0.26f);

        if (profile is CapitalHullProfile.Cruiser or CapitalHullProfile.Dreadnought)
            AddVasudanCapitalSuperstructureLite(w, frame, accent, hw, hgt, len,
                profile is CapitalHullProfile.Dreadnought ? 3 : 2,
                profile is CapitalHullProfile.Cruiser ? 0.60f : 1f);

        if (profile is CapitalHullProfile.Carrier)
        {
            AddBoxMat(w, panel, -hw * 0.42f, hw * 0.42f, hgt * 0.28f, hgt * 0.34f, -len * 0.20f, len * 0.22f);
            AddBoxMat(w, hull, -hw * 0.26f, hw * 0.26f, hgt * 0.06f, hgt * 0.20f, -len * 0.10f, len * 0.04f);
            AddBoxMat(w, frame, hw * 0.14f, hw * 0.26f, hgt * 0.50f, hgt * 0.72f, len * 0.02f, len * 0.12f);
            AddVasudanCarrierFlightDeckBands(w, panel, frame, hw, hgt, len);
        }

        float prowCrest = profile is CapitalHullProfile.Cruiser ? 0.88f : 1f;
        AddVasudanCapitalProw(w, hull, accent, hw, hgt, len, prowTip, prowBaseW, prowCrest);
        AddVasudanWingLiteParam(w, hull, accent, hw, hgt, len, -1f, spanMul, profile is CapitalHullProfile.Carrier ? 0.30f : 0.36f);
        AddVasudanWingLiteParam(w, hull, accent, hw, hgt, len, 1f, spanMul, profile is CapitalHullProfile.Carrier ? 0.30f : 0.36f);

        if (profile is CapitalHullProfile.Destroyer)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                float tipOut = hw * (spanMul - 0.22f);
                w.TriMat(accent, s * tipOut, hgt * 0.24f, -len * 0.03f,
                    s * (tipOut - hw * 0.02f), hgt * 0.27f, -len * 0.05f,
                    s * tipOut, hgt * 0.20f, -len * 0.04f);
            }
        }

        if (profile is CapitalHullProfile.Destroyer)
        {
            AddBoxMat(w, weapon, -hw * 0.12f, hw * 0.12f, hgt * 0.08f, hgt * 0.18f, len * 0.72f, len * 0.88f);
            AddBoxMat(w, accent, -hw * 0.04f, hw * 0.04f, hgt * 0.58f, hgt * 0.66f, len * 0.48f, len * 0.56f);
            for (int s = 0; s < 3; s++)
            {
                float t = s / 2f;
                float z = MathHelper.Lerp(len * 0.18f, len * 0.58f, t);
                w.TriMat(accent, -hw * 0.05f, hgt * (0.54f + t * 0.08f), z, hw * 0.05f, hgt * (0.54f + t * 0.08f), z,
                    0, hgt * (0.60f + t * 0.08f), z + len * 0.008f);
            }
        }
        else if (profile is CapitalHullProfile.Dreadnought)
        {
            AddBoxMat(w, weapon, -hw * 0.14f, hw * 0.14f, hgt * 0.06f, hgt * 0.20f, len * 0.70f, len * 0.90f);
            AddBoxMat(w, weapon, -hw * 0.58f, -hw * 0.44f, hgt * 0.04f, hgt * 0.18f, len * 0.60f, len * 0.78f);
            AddBoxMat(w, weapon, hw * 0.44f, hw * 0.58f, hgt * 0.04f, hgt * 0.18f, len * 0.60f, len * 0.78f);
            w.TriMat(weapon, -hw * 0.10f, hgt * 0.20f, len * 0.82f, hw * 0.10f, hgt * 0.20f, len * 0.82f,
                0, hgt * 0.26f, len * 0.88f);
        }
        else
        {
            AddBoxMat(w, weapon, -hw * 0.10f, hw * 0.10f, hgt * 0.08f, hgt * 0.16f, len * 0.58f, len * 0.70f);
        }

        if (profile is CapitalHullProfile.Dreadnought)
        {
            AddBoxMat(w, accent, -hw * 0.10f, hw * 0.10f, hgt * 0.48f, hgt * 0.66f, len * 0.78f, len * 0.94f);
            w.TriMat(accent, -hw * 0.06f, hgt * 0.62f, len * 0.90f, hw * 0.06f, hgt * 0.62f, len * 0.90f,
                0, hgt * 0.70f, len * 0.96f);
            w.TriMat(accent, -hw * 0.20f, hgt * 0.28f, len * 0.88f, -hw * 0.08f, hgt * 0.36f, len * 0.94f,
                -hw * 0.16f, hgt * 0.18f, len * 0.90f);
            w.TriMat(accent, hw * 0.20f, hgt * 0.28f, len * 0.88f, hw * 0.08f, hgt * 0.36f, len * 0.94f,
                hw * 0.16f, hgt * 0.18f, len * 0.90f);
        }

        float spineHeight = profile is CapitalHullProfile.Cruiser ? 0.66f : 1f;
        AddVasudanCapitalSpine(w, accent, hw, hgt, len, spineSegs, spineHeight);
        bool gameplayEngines = profile is not CapitalHullProfile.Destroyer;
        AddVasudanCapitalEnginesLite(w, hull, engine, accent, hw, hgt, len, engines, gameplayWide: gameplayEngines);
        if (profile is CapitalHullProfile.Cruiser)
            AddBoxMat(w, shield, -hw * 0.08f, hw * 0.08f, hgt * 0.50f, hgt * 0.60f, len * 0.06f, len * 0.12f);
        else
            AddBoxMat(w, shield, -hw * 0.08f, hw * 0.08f, hgt * 0.62f, hgt * 0.74f, len * 0.06f, len * 0.12f);
        if (profile is CapitalHullProfile.Dreadnought or CapitalHullProfile.Carrier)
        {
            w.TriMat(shield, -hw * 0.08f, hgt * 0.76f, len * 0.08f, hw * 0.08f, hgt * 0.76f, len * 0.08f,
                0, hgt * 0.82f, len * 0.12f);
        }
    }

    private static void AddVasudanCarrierFlightDeckBands(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial panel, RaceMeshWriter.HullMaterial frame,
        float hw, float hgt, float len)
    {
        for (int b = 0; b < 3; b++)
        {
            float t = b / 2f;
            float z = MathHelper.Lerp(-len * 0.14f, len * 0.12f, t);
            float halfW = hw * MathHelper.Lerp(0.36f, 0.26f, t);
            w.TriMat(panel, -halfW, hgt * 0.29f, z, halfW, hgt * 0.29f, z, 0, hgt * 0.31f, z + len * 0.012f);
        }
        w.TriMat(frame, -hw * 0.40f, hgt * 0.27f, -len * 0.18f, hw * 0.40f, hgt * 0.27f, -len * 0.18f,
            0, hgt * 0.29f, -len * 0.16f);
    }

    private static void AddVasudanCapitalSuperstructureLite(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial frame, RaceMeshWriter.HullMaterial accent,
        float hw, float hgt, float len, int tiers, float heightScale = 1f)
    {
        for (int t = 0; t < tiers; t++)
        {
            float shrink = 1f - t * 0.12f;
            float y0 = hgt * (0.48f * heightScale + t * 0.12f * heightScale);
            float y1 = hgt * (0.56f * heightScale + t * 0.12f * heightScale);
            float z0 = len * (0.02f - t * 0.03f);
            float z1 = len * (0.14f - t * 0.02f);
            AddBoxMat(w, frame, -hw * 0.20f * shrink, hw * 0.20f * shrink, y0, y1, z0, z1);
            if (t == tiers - 1)
                w.TriMat(accent, -hw * 0.06f, y1, z1, hw * 0.06f, y1, z1, 0, y1 + hgt * 0.05f, z1 + len * 0.01f);
        }
    }

    private static void AddVasudanCapitalEnginesLite(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial engine,
        RaceMeshWriter.HullMaterial accent, float hw, float hgt, float len, int count, bool gameplayWide = false)
    {
        count = Math.Clamp(count, 2, 3);
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : i / (float)(count - 1);
            float cx = MathHelper.Lerp(-hw * 0.22f, hw * 0.22f, t);
            float halfW = hw * (gameplayWide ? 0.10f : 0.08f);
            float podH = hgt * (gameplayWide ? 0.20f : 0.18f);
            AddBoxMat(w, hull, cx - halfW, cx + halfW, 0, podH, -len * 0.30f, -len * 0.16f);
            float glowInset = gameplayWide ? 0.42f : 0.55f;
            float glowTop = gameplayWide ? 0.16f : 0.12f;
            AddBoxMat(w, engine, cx - halfW * glowInset, cx + halfW * glowInset, hgt * 0.02f, hgt * glowTop,
                -len * 0.32f, -len * 0.20f);
            w.TriMat(accent, cx, hgt * 0.14f, -len * 0.20f, cx - halfW * 0.45f, hgt * 0.05f, -len * 0.26f, cx + halfW * 0.45f, hgt * 0.05f, -len * 0.26f);

        }
    }

    private static void AddVasudanCapitalProw(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial accent,
        float hw, float hgt, float len, float tipMul, float baseWMul = 0.26f, float crestScale = 1f)
    {
        float baseZ = len * (tipMul - 0.14f);
        float tipZ = len * tipMul;
        w.TriMat(hull, -hw * baseWMul, hgt * 0.14f, baseZ, hw * baseWMul, hgt * 0.14f, baseZ, 0, hgt * 0.46f, tipZ * 0.96f);
        w.TriMat(hull, -hw * (baseWMul - 0.08f), 0, baseZ, 0, hgt * 0.14f, tipZ * 0.92f, hw * (baseWMul - 0.08f), 0, baseZ);
        w.TriMat(accent, -hw * 0.06f, hgt * (0.40f * crestScale), tipZ * 0.92f, hw * 0.06f, hgt * (0.40f * crestScale), tipZ * 0.92f,
            0, hgt * (0.52f * crestScale), tipZ);
    }

    private static void AddVasudanCapitalSpine(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial accent, float hw, float hgt, float len, int segments,
        float heightScale = 1f)
    {
        for (int i = 0; i < segments; i++)
        {
            float t = (i + 0.5f) / segments;
            float z = MathHelper.Lerp(-len * 0.28f, len * 0.72f, t);
            w.TriMat(accent,
                0, hgt * (0.56f + t * 0.08f) * heightScale, z,
                -hw * 0.08f, hgt * (0.42f + t * 0.04f) * heightScale, z - len * 0.035f,
                hw * 0.08f, hgt * (0.42f + t * 0.04f) * heightScale, z - len * 0.035f);
        }
    }

    private static void AddVasudanWingLiteParam(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial accent,
        float hw, float hgt, float len, float side, float spanMul, float rootZMul)
    {
        float s = MathF.Sign(side);
        if (s == 0) return;

        float rootIn = hw * 0.28f;
        float rootOut = hw * spanMul;
        float tipOut = hw * (spanMul - 0.22f);
        float rootZ = len * rootZMul;
        float midZ = len * 0.02f;
        float tipZ = -len * 0.03f;
        float yBot = 0;
        float yTop = hgt * 0.16f;
        float yApex = hgt * 0.24f;

        w.TriMat(accent, s * rootOut, yTop, rootZ, s * tipOut, yApex, tipZ, s * rootIn, yTop, midZ);
        w.TriMat(accent, s * rootIn, yTop, midZ, s * tipOut, yApex, tipZ, s * rootIn, yBot, midZ);
        w.TriMat(hull, s * rootIn, yBot, midZ, s * tipOut, yApex, tipZ, s * tipOut, yBot, tipZ);
        w.TriMat(hull, s * rootIn, yBot, midZ, s * tipOut, yBot, tipZ, s * rootIn, yBot, rootZ);
        w.TriMat(accent, s * rootOut, yTop, rootZ, s * (rootOut - hw * 0.02f), yTop, rootZ - len * 0.01f, s * tipOut, yApex, tipZ);
    }

    /// <summary>Gameplay-readable component zones — lateral weapon pods, dorsal shield, minimal tri cost.</summary>
    private static void AddVasudanFighterHardpointsCompact(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial weapon,
        RaceMeshWriter.HullMaterial shieldGen, float hw, float hgt, float len, bool flagship = false)
    {
        float shieldHalfW = flagship ? 0.11f : 0.09f;
        float shieldY0 = hgt * (flagship ? 0.74f : 0.70f);
        float shieldY1 = hgt * (flagship ? 0.86f : 0.82f);
        float shieldZ0 = len * (flagship ? 0.04f : 0.05f);
        float shieldZ1 = len * (flagship ? 0.13f : 0.11f);

        AddBoxMat(w, shieldGen, -hw * shieldHalfW, hw * shieldHalfW, shieldY0, shieldY1, shieldZ0, shieldZ1);
        w.TriMat(shieldGen, -hw * shieldHalfW, shieldY1, shieldZ0 + len * 0.02f,
            -hw * (shieldHalfW - 0.03f), shieldY1 + hgt * 0.02f, shieldZ1,
            -hw * (shieldHalfW - 0.02f), shieldY0 + hgt * 0.02f, shieldZ1 - len * 0.01f);
        w.TriMat(shieldGen, hw * shieldHalfW, shieldY1, shieldZ0 + len * 0.02f,
            hw * (shieldHalfW - 0.03f), shieldY1 + hgt * 0.02f, shieldZ1,
            hw * (shieldHalfW - 0.02f), shieldY0 + hgt * 0.02f, shieldZ1 - len * 0.01f);

        float weaponOut = flagship ? 0.56f : 0.54f;
        float weaponIn = flagship ? 0.42f : 0.44f;
        AddBoxMat(w, weapon, -hw * weaponOut, -hw * weaponIn, hgt * 0.04f, hgt * 0.14f, len * 0.04f, len * 0.11f);
        AddBoxMat(w, weapon, hw * weaponIn, hw * weaponOut, hgt * 0.04f, hgt * 0.14f, len * 0.04f, len * 0.11f);
        w.TriMat(weapon, -hw * weaponOut, hgt * 0.10f, len * 0.08f,
            -hw * (weaponOut - 0.04f), hgt * 0.06f, len * 0.10f,
            -hw * weaponIn, hgt * 0.08f, len * 0.11f);
        w.TriMat(weapon, hw * weaponOut, hgt * 0.10f, len * 0.08f,
            hw * (weaponOut - 0.04f), hgt * 0.06f, len * 0.10f,
            hw * weaponIn, hgt * 0.08f, len * 0.11f);
    }

    private static void AddVasudanFighterHardpoints(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial frame,
        RaceMeshWriter.HullMaterial engine, RaceMeshWriter.HullMaterial weapon,
        RaceMeshWriter.HullMaterial shieldGen, float hw, float hgt, float len)
    {
        AddBoxMat(w, shieldGen, -hw * 0.08f, hw * 0.08f, hgt * 0.76f, hgt * 0.86f, len * 0.04f, len * 0.12f);
        w.TriMat(shieldGen, -hw * 0.06f, hgt * 0.86f, len * 0.08f, hw * 0.06f, hgt * 0.86f, len * 0.08f,
            0, hgt * 0.92f, len * 0.10f);
        AddBoxMat(w, shieldGen, -hw * 0.05f, hw * 0.05f, hgt * 0.88f, hgt * 0.94f, len * 0.06f, len * 0.11f);
        for (int side = -1; side <= 1; side += 2)
        {
            float xTip = side * hw * 0.72f;
            AddBoxMat(w, shieldGen, MathF.Min(xTip, xTip + side * hw * 0.04f), MathF.Max(xTip, xTip + side * hw * 0.04f),
                hgt * 0.18f, hgt * 0.26f, -len * 0.04f, len * 0.02f);
            w.TriMat(shieldGen, xTip, hgt * 0.26f, -len * 0.02f, xTip - side * hw * 0.02f, hgt * 0.22f, len * 0.01f,
                xTip, hgt * 0.20f, len * 0.02f);
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float xRoot = side * hw * 0.52f;
            float xPod = side * hw * 0.58f;
            AddBoxMat(w, weapon, MathF.Min(xRoot, xPod), MathF.Max(xRoot, xPod), hgt * 0.04f, hgt * 0.14f,
                len * 0.02f, len * 0.10f);
            AddBoxMat(w, weapon, MathF.Min(xRoot - side * hw * 0.02f, xPod), MathF.Max(xRoot - side * hw * 0.02f, xPod),
                hgt * 0.02f, hgt * 0.08f, len * 0.08f, len * 0.14f);
            w.TriMat(weapon, xPod, hgt * 0.10f, len * 0.06f, xPod - side * hw * 0.04f, hgt * 0.06f, len * 0.10f,
                xPod, hgt * 0.04f, len * 0.12f);
        }

        AddBoxMat(w, weapon, -hw * 0.06f, hw * 0.06f, hgt * 0.06f, hgt * 0.14f, len * 0.22f, len * 0.30f);
        w.TriMat(weapon, -hw * 0.04f, hgt * 0.14f, len * 0.26f, hw * 0.04f, hgt * 0.14f, len * 0.26f,
            0, hgt * 0.18f, len * 0.28f);
        for (int side = -1; side <= 1; side += 2)
        {
            float xBay = side * hw * 0.44f;
            AddBoxMat(w, weapon, MathF.Min(xBay, xBay + side * hw * 0.06f), MathF.Max(xBay, xBay + side * hw * 0.06f),
                hgt * 0.02f, hgt * 0.10f, -len * 0.02f, len * 0.06f);
            for (int m = 0; m < 3; m++)
            {
                float mz = MathHelper.Lerp(-len * 0.01f, len * 0.05f, m / 2f);
                AddBoxMat(w, frame, MathF.Min(xBay - side * hw * 0.01f, xBay + side * hw * 0.05f),
                    MathF.Max(xBay - side * hw * 0.01f, xBay + side * hw * 0.05f),
                    hgt * 0.08f, hgt * 0.11f, mz, mz + len * 0.008f);
            }
        }

    }

    private static void AddVasudanHeroFlagshipProw(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial frame,
        RaceMeshWriter.HullMaterial accent, float hw, float hgt, float len)
    {
        float baseZ = len * 0.48f;
        float tipZ = len * 0.62f;
        w.TriMat(hull, -hw * 0.10f, hgt * 0.18f, baseZ, hw * 0.10f, hgt * 0.18f, baseZ, 0, hgt * 0.42f, tipZ);
        w.TriMat(hull, -hw * 0.06f, 0, baseZ, 0, hgt * 0.12f, tipZ * 0.96f, hw * 0.06f, 0, baseZ);
        w.TriMat(accent, -hw * 0.04f, hgt * 0.36f, tipZ * 0.94f, hw * 0.04f, hgt * 0.36f, tipZ * 0.94f,
            0, hgt * 0.46f, tipZ);
    }

    private static void AddVasudanFighterNose(RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial frame, RaceMeshWriter.HullMaterial canopy, float hw, float hgt, float len)
    {
        float baseZ = len * 0.29f;
        float tipZ = len * 0.545f;
        float baseW = hw * 0.22f;
        float midZ = len * 0.405f;
        float midW = hw * 0.12f;

        w.TriMat(hull, -baseW, hgt * 0.12f, baseZ, baseW, hgt * 0.12f, baseZ, 0, hgt * 0.44f, midZ);
        w.TriMat(hull, -midW, hgt * 0.28f, midZ, 0, hgt * 0.54f, tipZ, midW, hgt * 0.28f, midZ);
        w.TriMat(hull, -baseW, 0, baseZ, 0, hgt * 0.12f, tipZ * 0.98f, baseW, 0, baseZ);
        w.TriMat(canopy, -hw * 0.04f, hgt * 0.48f, tipZ * 0.88f, hw * 0.04f, hgt * 0.48f, tipZ * 0.88f, 0, hgt * 0.52f, tipZ * 0.95f);
    }

    private static void AddVasudanFighterCanopy(RaceMeshWriter w, RaceMeshWriter.HullMaterial canopy, RaceMeshWriter.HullMaterial frame, float hw, float hgt, float len)
    {
        AddBoxMat(w, canopy, -hw * 0.15f, hw * 0.15f, hgt * 0.46f, hgt * 0.82f, len * 0.10f, len * 0.22f);
        AddBoxMat(w, frame, -hw * 0.17f, hw * 0.17f, hgt * 0.44f, hgt * 0.48f, len * 0.08f, len * 0.24f);
        AddBoxMat(w, frame, -hw * 0.12f, hw * 0.12f, hgt * 0.72f, hgt * 0.76f, len * 0.12f, len * 0.20f);
    }

    private static void AddVasudanFighterCanopyLite(RaceMeshWriter w, RaceMeshWriter.HullMaterial canopy, float hw, float hgt, float len)
        => AddBoxMat(w, canopy, -hw * 0.15f, hw * 0.15f, hgt * 0.46f, hgt * 0.80f, len * 0.10f, len * 0.22f);

    private static void AddVasudanModernWingLite(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial accent,
        float hw, float hgt, float len, float side)
    {
        float s = MathF.Sign(side);
        if (s == 0) return;

        float rootIn = hw * 0.28f;
        float rootOut = hw * 1.16f;
        float tipOut = hw * 0.84f;
        float rootZ = len * 0.14f;
        float midZ = len * 0.02f;
        float tipZ = -len * 0.03f;
        float yBot = 0;
        float yTop = hgt * 0.16f;
        float yApex = hgt * 0.24f;

        w.TriMat(accent, s * rootOut, yTop, rootZ, s * tipOut, yApex, tipZ, s * rootIn, yTop, midZ);
        w.TriMat(accent, s * rootIn, yTop, midZ, s * tipOut, yApex, tipZ, s * rootIn, yBot, midZ);
        w.TriMat(hull, s * rootIn, yBot, midZ, s * tipOut, yApex, tipZ, s * tipOut, yBot, tipZ);
        w.TriMat(hull, s * rootIn, yBot, midZ, s * tipOut, yBot, tipZ, s * rootIn, yBot, rootZ);
        w.TriMat(accent, s * rootOut, yTop, rootZ, s * (rootOut - hw * 0.02f), yTop, rootZ - len * 0.01f, s * tipOut, yApex, tipZ);
    }

    private static void AddVasudanModernWing(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial frame,
        RaceMeshWriter.HullMaterial accent, float hw, float hgt, float len, float side)
    {
        float s = MathF.Sign(side);
        if (s == 0) return;

        float rootIn = hw * 0.28f;
        float rootOut = hw * 1.16f;
        float midOut = hw * 0.98f;
        float tipOut = hw * 0.84f;
        float rootZ = len * 0.14f;
        float midZ = len * 0.02f;
        float tipZ = -len * 0.03f;
        float yBot = 0;
        float yTop = hgt * 0.16f;
        float yApex = hgt * 0.24f;

        float xRootIn = s * rootIn;
        float xRootOut = s * rootOut;
        float xMidLo = s * midOut * 0.85f;
        float xMidHi = s * midOut;
        AddBoxMat(w, hull, MathF.Min(xRootIn, xRootOut), MathF.Max(xRootIn, xRootOut), yBot, yTop, len * 0.02f, rootZ);
        AddBoxMat(w, hull, MathF.Min(xMidLo, xMidHi), MathF.Max(xMidLo, xMidHi), yBot, yApex, midZ, rootZ - len * 0.04f);

        w.TriMat(accent, s * rootOut, yTop, rootZ, s * tipOut, yApex, tipZ, s * rootIn, yTop, midZ);
        w.TriMat(accent, s * rootIn, yTop, midZ, s * tipOut, yApex, tipZ, s * rootIn, yBot, midZ);
        w.TriMat(hull, s * rootIn, yBot, midZ, s * tipOut, yApex, tipZ, s * tipOut, yBot, tipZ);
        w.TriMat(hull, s * rootIn, yBot, midZ, s * tipOut, yBot, tipZ, s * rootIn, yBot, rootZ);
        float xFrameLo = s * rootIn;
        float xFrameHi = s * (rootIn + hw * 0.04f);
        AddBoxMat(w, frame, MathF.Min(xFrameLo, xFrameHi), MathF.Max(xFrameLo, xFrameHi), yTop * 0.9f, yTop * 1.1f, rootZ - len * 0.02f, rootZ + len * 0.02f);
        float xTipLo = s * tipOut;
        float xTipHi = s * (tipOut + hw * 0.06f);
        AddBoxMat(w, accent, MathF.Min(xTipLo, xTipHi), MathF.Max(xTipLo, xTipHi), yApex * 0.92f, yApex * 1.08f, tipZ - len * 0.014f, tipZ + len * 0.014f);
        AddBoxMat(w, accent, MathF.Min(s * (tipOut - hw * 0.02f), s * tipOut), MathF.Max(s * (tipOut - hw * 0.02f), s * tipOut), yTop, yApex, tipZ, tipZ + len * 0.02f);
        w.TriMat(accent, s * rootOut, yTop, rootZ, s * midOut, yApex, midZ, s * (rootOut - hw * 0.02f), yTop, rootZ - len * 0.01f);
    }

    private static void AddVasudanEngineNacelle(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial engine,
        RaceMeshWriter.HullMaterial ring, float x0, float x1, float hgt, float len, bool gameplayWide = false)
    {
        float podH = hgt * (gameplayWide ? 0.24f : 0.22f);
        float podW = x1 - x0;
        float sternZ = gameplayWide ? -len * 0.11f : -len * 0.10f;
        float sternZ1 = gameplayWide ? -len * 0.02f : -len * 0.03f;
        AddBoxMat(w, hull, x0, x1, 0, podH * 0.65f, sternZ, sternZ1);
        float inset = gameplayWide ? 0.16f : 0.24f;
        AddBoxMat(w, engine, x0 + podW * inset, x1 - podW * inset, podH * 0.02f, podH * (gameplayWide ? 0.50f : 0.40f),
            sternZ + len * 0.01f, sternZ1 - len * 0.01f);
        if (gameplayWide)
        {
            float cx = (x0 + x1) * 0.5f;
            w.TriMat(engine, x0 + podW * 0.12f, podH * 0.08f, sternZ + len * 0.005f,
                x1 - podW * 0.12f, podH * 0.08f, sternZ + len * 0.005f,
                cx, podH * 0.44f, sternZ1 - len * 0.008f);
        }
    }

    private static void AddVasudanFuselagePanels(RaceMeshWriter w, RaceMeshWriter.HullMaterial frame, RaceMeshWriter.HullMaterial panel, float hw, float hgt, float len)
    {
        for (int i = 0; i < 5; i++)
        {
            float t = i / 4f;
            float z = MathHelper.Lerp(-len * 0.06f, len * 0.22f, t);
            float inset = hw * (0.37f - t * 0.02f);
            float panelH0 = hgt * (0.17f + t * 0.02f);
            float panelH1 = hgt * (0.33f + t * 0.01f);
            float depth = len * (0.014f + t * 0.002f);
            AddBoxMat(w, panel, -inset, -inset + hw * 0.03f, panelH0, panelH1, z, z + depth);
            AddBoxMat(w, panel, inset - hw * 0.03f, inset, panelH0, panelH1, z, z + depth);
            float seamY = hgt * (0.39f + t * 0.015f);
            AddBoxMat(w, frame, -hw * 0.045f, hw * 0.045f, seamY, seamY + hgt * 0.025f, z - len * 0.003f, z + depth + len * 0.004f);
            float bevelY = panelH1 + hgt * 0.012f;
            AddBoxMat(w, frame, -inset + hw * 0.01f, -inset + hw * 0.04f, bevelY, bevelY + hgt * 0.018f, z + depth * 0.4f, z + depth + len * 0.002f);
            AddBoxMat(w, frame, inset - hw * 0.04f, inset - hw * 0.01f, bevelY, bevelY + hgt * 0.018f, z + depth * 0.4f, z + depth + len * 0.002f);
        }
    }


    private static void AddVasudanFighterIntakes(RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial panel, float hw, float hgt, float len)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * hw * 0.34f;
            float xOut = side * hw * 0.40f;
            float z0 = len * 0.04f;
            float z1 = len * 0.12f;
            w.TriMat(panel, x, hgt * 0.20f, z0, xOut, hgt * 0.18f, z1, x, hgt * 0.14f, z1);
            w.TriMat(hull, x, hgt * 0.14f, z1, xOut, hgt * 0.18f, z1, x, hgt * 0.20f, z0);
        }
    }

    private static void AddVasudanFighterTail(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial frame,
        RaceMeshWriter.HullMaterial accent, float hw, float hgt, float len)
    {
        float finZ = -len * 0.04f;
        AddBoxMat(w, hull, -hw * 0.06f, hw * 0.06f, hgt * 0.48f, hgt * 0.64f, finZ, -len * 0.008f);
        AddBoxMat(w, accent, -hw * 0.20f, hw * 0.20f, hgt * 0.38f, hgt * 0.44f, finZ - len * 0.01f, finZ + len * 0.01f);
    }

    // ── Korath medium combat truss hulls — role-distinct silhouettes on shared NASA spine language ─

    /// <summary>Fast corvette — elongated truss wedge, narrow solar booms, flank weapon rigging.</summary>
    private static void BuildKorathCorvetteTruss(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var truss = RaceMeshWriter.HullMaterial.Truss;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float l = len * 1.12f;
        float hw = wid * 0.46f;
        float hh = hgt * 0.42f;
        float bowZ = l * 0.60f;
        float sternZ = -l * 0.16f;
        int cylSegs = 8;

        AddNasaTrussSpine(w, hw * 0.22f, hh * 0.32f, sternZ, bowZ * 0.96f, 6);
        AddNasaCylinder(w, 0, hh * 0.10f, l * 0.18f, hw * 0.34f, hh * 0.88f, cylSegs);
        AddNasaCylinder(w, 0, hh * 0.12f, -l * 0.06f, hw * 0.28f, hh * 0.72f, cylSegs);
        w.TriMat(hull, -hw * 0.14f, hh * 0.22f, bowZ * 0.72f, hw * 0.14f, hh * 0.22f, bowZ * 0.72f, 0, hh * 0.52f, bowZ);
        w.TriMat(hull, -hw * 0.08f, 0, bowZ * 0.66f, 0, hh * 0.18f, bowZ * 0.92f, hw * 0.08f, 0, bowZ * 0.66f);
        w.TriMat(hull, -hw * 0.06f, hh * 0.28f, bowZ * 0.88f, hw * 0.06f, hh * 0.28f, bowZ * 0.88f, 0, hh * 0.34f, bowZ * 0.98f);

        float panelZ = l * 0.04f;
        float panelH = hh * 0.54f;
        AddNasaSolarArray(w, -wid * 0.48f, -hw * 0.04f, panelH, hgt * 0.94f, panelZ - l * 0.06f, panelZ + l * 0.06f, 2, 3);
        AddNasaSolarArray(w, hw * 0.04f, wid * 0.48f, panelH, hgt * 0.94f, panelZ - l * 0.06f, panelZ + l * 0.06f, 2, 3);
        AddMatBridge(w, -hw, -wid * 0.30f, panelH + hh * 0.02f, panelH + hh * 0.08f, panelZ);
        AddMatBridge(w, hw, wid * 0.30f, panelH + hh * 0.02f, panelH + hh * 0.08f, panelZ);

        AddRadiatorPanel(w, -hw * 0.88f, -hw * 0.62f, hh * 0.48f, hh * 0.92f, sternZ * 0.50f, sternZ * 0.20f);
        AddRadiatorPanel(w, hw * 0.62f, hw * 0.88f, hh * 0.48f, hh * 0.92f, sternZ * 0.50f, sternZ * 0.20f);
        AddBoxMat(w, weapon, -hw * 0.58f, -hw * 0.46f, hh * 0.04f, hh * 0.16f, l * 0.02f, l * 0.12f);
        AddBoxMat(w, weapon, hw * 0.46f, hw * 0.58f, hh * 0.04f, hh * 0.16f, l * 0.02f, l * 0.12f);
        AddBoxMat(w, truss, -hw * 0.10f, hw * 0.10f, hh * 0.78f, hh * 0.92f, l * 0.08f, l * 0.16f);
        AddKorathDorsalKeelBand(w, hw, hh, l * 0.02f, bowZ * 0.84f, 3);
        AddKorathBowTip(w, hw, hh, bowZ);
    }

    /// <summary>Strike frigate — balanced gun-deck frame, modest +Z prow tip, dorsal keel truss strip.</summary>
    private static void BuildKorathFrigateStrikeTruss(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float l = len * 1.10f;
        float hw = wid * 0.48f;
        float hh = hgt * 0.44f;
        float bowZ = l * 0.62f;
        float sternZ = -l * 0.20f;
        int cylSegs = 6;

        AddNasaTrussSpine(w, hw * 0.26f, hh * 0.34f, sternZ, bowZ, 6);
        for (int m = 0; m < 2; m++)
        {
            float t = m;
            float cz = MathHelper.Lerp(-l * 0.22f, l * 0.18f, t);
            float modR = hw * (0.44f + 0.04f * m);
            AddNasaCylinder(w, 0, hh * 0.10f, cz, modR, hh * 0.72f, cylSegs);
        }

        AddBoxMat(w, hull, -hw * 0.56f, -hw * 0.38f, hh * 0.12f, hh * 0.42f, l * 0.08f, l * 0.28f);
        AddBoxMat(w, hull, hw * 0.38f, hw * 0.56f, hh * 0.12f, hh * 0.42f, l * 0.08f, l * 0.28f);
        AddBoxMat(w, weapon, -hw * 0.12f, hw * 0.12f, hh * 0.06f, hh * 0.20f, l * 0.26f, l * 0.42f);
        w.TriMat(hull, -hw * 0.10f, hh * 0.18f, bowZ * 0.78f, hw * 0.10f, hh * 0.18f, bowZ * 0.78f, 0, hh * 0.36f, bowZ);

        float panelZ = l * 0.04f;
        float panelH = hh * 0.54f;
        float wingReach = wid * 0.46f;
        AddNasaSolarArray(w, -wingReach, -hw * 0.05f, panelH, hgt * 0.90f, panelZ - l * 0.06f, panelZ + l * 0.06f, 2, 3);
        AddNasaSolarArray(w, hw * 0.05f, wingReach, panelH, hgt * 0.90f, panelZ - l * 0.06f, panelZ + l * 0.06f, 2, 3);
        AddMatBridge(w, -hw, -wingReach * 0.62f, panelH + hh * 0.02f, panelH + hh * 0.08f, panelZ);
        AddMatBridge(w, hw, wingReach * 0.62f, panelH + hh * 0.02f, panelH + hh * 0.08f, panelZ);

        AddKorathDorsalKeelBand(w, hw, hh, l * 0.02f, bowZ * 0.94f, 4);
        AddKorathBowTip(w, hw, hh, bowZ);
    }

    /// <summary>Standard frigate — balanced gun-deck frame, modest +Z prow tip, compact command ridge.</summary>
    private static void BuildKorathFrigateTruss(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var truss = RaceMeshWriter.HullMaterial.Truss;
        float hw = wid * 0.48f;
        float hh = hgt * 0.44f;
        float bowZ = len * 0.58f;
        int cylSegs = 6;

        AddNasaTrussSpine(w, hw * 0.26f, hh * 0.34f, -len * 0.52f, bowZ * 0.96f, 6);
        for (int m = 0; m < 2; m++)
        {
            float t = m;
            float cz = MathHelper.Lerp(-len * 0.20f, len * 0.16f, t);
            float modR = hw * (0.44f + 0.04f * m);
            AddNasaCylinder(w, 0, hh * 0.10f, cz, modR, hh * 0.72f, cylSegs);
        }

        AddBoxMat(w, hull, -hw * 0.56f, -hw * 0.38f, hh * 0.12f, hh * 0.42f, len * 0.08f, len * 0.26f);
        AddBoxMat(w, hull, hw * 0.38f, hw * 0.56f, hh * 0.12f, hh * 0.42f, len * 0.08f, len * 0.26f);
        AddBoxMat(w, weapon, -hw * 0.12f, hw * 0.12f, hh * 0.06f, hh * 0.20f, len * 0.24f, len * 0.38f);
        w.TriMat(hull, -hw * 0.10f, hh * 0.18f, len * 0.48f, hw * 0.10f, hh * 0.18f, len * 0.48f, 0, hh * 0.36f, bowZ);

        float panelZ = len * 0.04f;
        float panelH = hh * 0.54f;
        AddNasaSolarArray(w, -wid * 0.50f, -hw * 0.05f, panelH, hgt * 0.92f, panelZ - len * 0.06f, panelZ + len * 0.06f, 2, 3);
        AddNasaSolarArray(w, hw * 0.05f, wid * 0.50f, panelH, hgt * 0.92f, panelZ - len * 0.06f, panelZ + len * 0.06f, 2, 3);
        AddMatBridge(w, -hw, -wid * 0.30f, panelH + hh * 0.02f, panelH + hh * 0.08f, panelZ);
        AddMatBridge(w, hw, wid * 0.30f, panelH + hh * 0.02f, panelH + hh * 0.08f, panelZ);

        AddBoxMat(w, truss, -hw * 0.10f, hw * 0.10f, hh * 0.78f, hh * 0.86f, len * 0.14f, len * 0.20f);
        AddKorathBowTip(w, hw, hh, len * 0.96f);
    }

    /// <summary>Heavy bomber — narrow-beam payload spine, +Z elongation, dorsal keel, bow anchor.</summary>
    private static void BuildKorathBomberTruss(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var truss = RaceMeshWriter.HullMaterial.Truss;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float l = len * 1.10f;
        float hw = wid * 0.30f;
        float hh = hgt * 0.36f;
        float bowZ = l * 0.66f;
        int cylSegs = 6;

        AddNasaTrussSpine(w, hw * 0.22f, hh * 0.28f, -l * 0.54f, bowZ * 0.98f, 5);
        AddNasaCylinder(w, 0, hh * 0.08f, l * 0.14f, hw * 0.30f, hh * 0.52f, cylSegs);
        AddNasaCylinder(w, 0, hh * 0.06f, -l * 0.14f, hw * 0.24f, hh * 0.40f, cylSegs);

        w.TriMat(hull, -hw * 0.10f, hh * 0.14f, bowZ * 0.76f, hw * 0.10f, hh * 0.14f, bowZ * 0.76f, 0, hh * 0.32f, bowZ);
        AddKorathDorsalKeelBand(w, hw, hh, l * 0.04f, bowZ * 0.90f, 3);
        AddKorathBowTip(w, hw, hh, bowZ);

        AddBoxMat(w, weapon, -hw * 0.26f, hw * 0.26f, 0, hh * 0.10f, -len * 0.02f, len * 0.10f);
        AddBoxMat(w, truss, -hw * 0.28f, hw * 0.28f, -hh * 0.02f, hh * 0.48f, l * 0.04f, l * 0.16f);

        float panelZ = len * 0.04f;
        float panelH = hh * 0.40f;
        AddNasaSolarArray(w, -wid * 0.50f, -hw * 0.04f, panelH, hgt * 0.88f, panelZ - len * 0.05f, panelZ + len * 0.05f, 2, 1);
        AddNasaSolarArray(w, hw * 0.04f, wid * 0.50f, panelH, hgt * 0.88f, panelZ - len * 0.05f, panelZ + len * 0.05f, 2, 1);
        AddMatBridge(w, -hw, -wid * 0.28f, panelH + hh * 0.02f, panelH + hh * 0.06f, panelZ);
        AddMatBridge(w, hw, wid * 0.28f, panelH + hh * 0.02f, panelH + hh * 0.06f, panelZ);
        AddKorathBowTip(w, hw, hh, len * 0.96f);
    }

    /// <summary>Heavy gunship — flattened chin weapon truss, low gun-deck, compact assault frame.</summary>
    private static void BuildKorathGunshipTruss(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var truss = RaceMeshWriter.HullMaterial.Truss;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float l = len * 1.00f;
        float hw = wid * 0.48f;
        float hh = hgt * 0.30f;
        float bowZ = l * 0.54f;
        int cylSegs = 6;

        AddNasaTrussSpine(w, hw * 0.26f, hh * 0.28f, -l * 0.52f, bowZ * 0.96f, 5);
        AddNasaCylinder(w, 0, hh * 0.10f, l * 0.08f, hw * 0.40f, hh * 0.56f, cylSegs);
        AddNasaCylinder(w, 0, hh * 0.08f, -l * 0.18f, hw * 0.32f, hh * 0.44f, cylSegs);

        AddBoxMat(w, truss, -hw * 0.28f, hw * 0.28f, hh * 0.24f, hh * 0.38f, -len * 0.02f, len * 0.14f);
        w.TriMat(weapon, -hw * 0.12f, 0, bowZ * 0.26f, hw * 0.12f, 0, bowZ * 0.26f, 0, hh * 0.08f, bowZ * 0.48f);
        AddBoxMat(w, weapon, -hw * 0.46f, -hw * 0.34f, hh * 0.04f, hh * 0.12f, len * 0.06f, len * 0.14f);
        AddBoxMat(w, weapon, hw * 0.34f, hw * 0.46f, hh * 0.04f, hh * 0.12f, len * 0.06f, len * 0.14f);

        w.TriMat(hull, -hw * 0.10f, hh * 0.12f, bowZ * 0.74f, hw * 0.10f, hh * 0.12f, bowZ * 0.74f, 0, hh * 0.28f, bowZ);
        AddKorathBowTip(w, hw, hh, bowZ);

        float panelZ = len * 0.04f;
        float panelH = hh * 0.42f;
        AddNasaSolarArray(w, -wid * 0.50f, -hw * 0.04f, panelH, hgt * 0.90f, panelZ - len * 0.05f, panelZ + len * 0.05f, 2, 1);
        AddNasaSolarArray(w, hw * 0.04f, wid * 0.50f, panelH, hgt * 0.90f, panelZ - len * 0.05f, panelZ + len * 0.05f, 2, 1);
        AddMatBridge(w, -hw, -wid * 0.28f, panelH + hh * 0.02f, panelH + hh * 0.06f, panelZ);
        AddMatBridge(w, hw, wid * 0.28f, panelH + hh * 0.02f, panelH + hh * 0.06f, panelZ);

        AddBoxMat(w, hull, -hw * 0.08f, hw * 0.08f, hh * 0.50f, hh * 0.56f, len * 0.04f, len * 0.10f);
        AddKorathBowTip(w, hw, hh, len * 0.96f);
    }

    /// <summary>Assault destroyer — trimmed spine/modules; prow re-anchored to +Z envelope.</summary>
    private static void BuildKorathDestroyerTruss(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var truss = RaceMeshWriter.HullMaterial.Truss;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float hw = wid * 0.50f;
        float hh = hgt * 0.44f;
        // wo-06-09 loop 8: aspect 1.09→~1.35 — re-anchor prow to +Z bound
        float bowZ = len * 0.64f;
        int cylSegs = 6;

        AddNasaTrussSpine(w, hw * 0.30f, hh * 0.34f, -len * 0.52f, bowZ, 5);
        for (int m = 0; m < 3; m++)
        {
            float t = m / 2f;
            float cz = MathHelper.Lerp(-len * 0.28f, len * 0.20f, t);
            float modR = hw * (0.42f + 0.06f * m);
            float modH = hh * (0.72f + (m % 2) * 0.10f);
            AddNasaCylinder(w, 0, hh * 0.10f, cz, modR, modH, cylSegs);
            if (m == 1)
                AddDockingRing(w, 0, hh * 0.10f + modH, cz + modH * 0.26f, modR * 0.76f, modR * 1.04f, cylSegs);
        }

        AddBoxMat(w, hull, -hw * 0.52f, -hw * 0.34f, hh * 0.14f, hh * 0.42f, len * 0.42f, len * 0.56f);
        AddBoxMat(w, hull, hw * 0.34f, hw * 0.52f, hh * 0.14f, hh * 0.42f, len * 0.42f, len * 0.56f);
        AddBoxMat(w, weapon, -hw * 0.10f, hw * 0.10f, hh * 0.08f, hh * 0.22f, len * 0.56f, len * 0.68f);
        AddBoxMat(w, hull, -hw * 0.08f, hw * 0.08f, hh * 0.16f, hh * 0.34f, len * 0.52f, bowZ);

        for (int r = 0; r < 2; r++)
        {
            float z = MathHelper.Lerp(-len * 0.28f, -len * 0.08f, r);
            float side = r == 0 ? -1f : 1f;
            AddRadiatorPanel(w, side * hw * 0.82f, side * wid * 0.42f, hh * 0.50f, hh * 0.94f, z - len * 0.04f, z + len * 0.04f);
        }

        float panelZ = len * 0.04f;
        float panelH = hh * 0.54f;
        AddNasaSolarArray(w, -wid * 0.50f, -hw * 0.05f, panelH, hgt * 0.92f, panelZ - len * 0.06f, panelZ + len * 0.06f, 2, 3);
        AddNasaSolarArray(w, hw * 0.05f, wid * 0.50f, panelH, hgt * 0.92f, panelZ - len * 0.06f, panelZ + len * 0.06f, 2, 3);
        AddMatBridge(w, -hw, -wid * 0.30f, panelH + hh * 0.02f, panelH + hh * 0.08f, panelZ);
        AddMatBridge(w, hw, wid * 0.30f, panelH + hh * 0.02f, panelH + hh * 0.08f, panelZ);
        AddKorathBowTip(w, hw, hh, len * 0.96f);

        AddCommunicationsMast(w, 0, hh * 1.02f, -len * 0.08f, hh * 0.32f, hw * 0.06f);
        AddBoxMat(w, truss, -hw * 0.12f, hw * 0.12f, hh * 0.74f, hh * 0.88f, len * 0.10f, len * 0.16f);
    }

    /// <summary>Industrial/logistics truss hulls — wide cargo rigging with per-role attachments.</summary>
    private static void BuildTrussUtilitySolid(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var truss = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;

        float widthScale = hullKey is "freighter_bulk" ? 0.92f
            : hullKey is "support_repair" ? 0.88f
            : hullKey is "miner_eva" ? 0.94f
            : hullKey is "miner_tractor" ? 0.94f
            : 1.00f;
        float hw = wid * 0.46f * widthScale;
        float hh = hgt * 0.46f;
        int cylSegs = 8;

        float sternZ = hullKey switch
        {
            "freighter_bulk" => -len * 0.18f,
            "transport_cargo" => -len * 0.16f,
            "miner_basic" => -len * 0.16f,
            "support_repair" => -len * 0.14f,
            _ => -len * 0.16f
        };
        float bowZ = hullKey switch
        {
            "freighter_bulk" => len * 0.58f,
            "transport_cargo" => len * 0.56f,
            "miner_basic" => len * 0.52f,
            "miner_eva" => len * 0.48f,
            "miner_tractor" => len * 0.46f,
            "support_repair" => len * 0.50f,
            _ => len * 0.48f
        };

        bool isCargo = hullKey is "freighter_bulk" or "transport_cargo";
        AddNasaTrussSpine(w, hw * 0.30f, hh * 0.34f, sternZ, bowZ * 0.76f, isCargo ? 5 : 6);

        float bellyW = hw * (hullKey is "freighter_bulk" ? 0.40f : hullKey is "transport_cargo" ? 0.38f : 0.40f);
        AddBoxMat(w, hull, -bellyW, bellyW, 0, hh * 0.90f, sternZ + len * 0.02f, bowZ * 0.68f);
        AddBoxMat(w, truss, -bellyW * 0.58f, bellyW * 0.58f, hh * 0.84f, hh * 1.10f, sternZ + len * 0.04f, bowZ * 0.56f);
        w.TriMat(hull, -bellyW * 0.22f, hh * 0.14f, bowZ * 0.68f, bellyW * 0.22f, hh * 0.14f, bowZ * 0.68f, 0, hh * 0.50f, bowZ);
        w.TriMat(accent, -hw * 0.04f, hh * 0.54f, bowZ * 0.86f, hw * 0.04f, hh * 0.54f, bowZ * 0.86f, 0, hh * 0.62f, bowZ);

        int modules = hullKey is "freighter_bulk" ? 3 : 2;
        for (int m = 0; m < modules; m++)
        {
            float t = m / MathF.Max(1f, modules - 1);
            float cz = MathHelper.Lerp(sternZ + len * 0.06f, bowZ * 0.42f, t);
            float modR = hw * (hullKey is "freighter_bulk" ? 0.42f : 0.38f);
            int modSegs = isCargo ? 6 : cylSegs;
            AddNasaCylinder(w, 0, hh * 0.08f, cz, modR, hh * 0.72f, modSegs);
            if (m < modules - 1)
                AddDockingRing(w, 0, hh * 0.08f + hh * 0.72f, cz + hh * 0.22f, modR * 0.76f, modR * 1.02f, cylSegs);
        }

        switch (hullKey)
        {
            case "miner_basic":
                AddVasudanMiningArms(w, hull, accent, weapon, hw, hgt, len, trussStyle: true);
                AddVasudanDorsalKeel(w, hull, accent, hw, hgt, len, hullKey, trussStyle: true);
                for (int side = -1; side <= 1; side += 2)
                {
                    float xTip = side * hw * 0.64f;
                    w.TriMat(truss, xTip, hgt * 0.18f, len * 0.50f, xTip, hgt * 0.24f, len * 0.56f, 0, hgt * 0.22f, len * 0.54f);
                    w.TriMat(accent, xTip, hgt * 0.26f, len * 0.56f, xTip + side * hw * 0.03f, hgt * 0.30f, len * 0.58f, xTip, hgt * 0.20f, len * 0.57f);
                }
                break;
            case "miner_eva":
                AddVasudanEvaPod(w, hull, accent, truss, hw, hgt, len);
                AddVasudanEvaDockArm(w, hull, accent, truss, hw, hgt, len);
                AddVasudanDorsalKeel(w, hull, accent, hw, hgt, len, hullKey, trussStyle: true);
                w.TriMat(accent, hw * 0.52f, hgt * 0.48f, len * 0.14f, hw * 0.60f, hgt * 0.54f, len * 0.18f, hw * 0.54f, hgt * 0.42f, len * 0.16f);
                break;
            case "miner_tractor":
                AddVasudanTractorApparatus(w, hull, accent, weapon, hw, hgt, len);
                AddVasudanDorsalKeel(w, hull, accent, hw, hgt, len, hullKey, trussStyle: true);
                for (int side = -1; side <= 1; side += 2)
                {
                    float xTip = side * hw * 0.70f;
                    w.TriMat(truss, xTip, hgt * 0.30f, len * 0.12f, xTip + side * hw * 0.06f, hgt * 0.36f, len * 0.18f, xTip, hgt * 0.26f, len * 0.16f);
                    w.TriMat(accent, xTip + side * hw * 0.08f, hgt * 0.40f, len * 0.16f, xTip + side * hw * 0.12f, hgt * 0.44f, len * 0.20f, xTip + side * hw * 0.06f, hgt * 0.36f, len * 0.18f);
                }
                break;
            case "transport_cargo":
                AddVasudanCargoSpine(w, hull, truss, accent, hw, hgt, len, trussStyle: true);
                AddVasudanCargoBays(w, hull, truss, accent, hw, hgt, len, 2, hullKey);
                AddVasudanDorsalKeel(w, hull, accent, hw, hgt, len, hullKey, trussStyle: true);
                break;
            case "freighter_bulk":
                AddVasudanBulkCargoSpine(w, hull, truss, accent, hw, hgt, len, trussStyle: true);
                AddVasudanCargoBays(w, hull, truss, accent, hw, hgt, len, 2, hullKey);
                AddVasudanDorsalKeel(w, hull, accent, hw, hgt, len, hullKey, trussStyle: true);
                break;
            case "support_repair":
                AddVasudanRepairBooms(w, hull, accent, truss, hw, hgt, len, trussStyle: true);
                AddVasudanRepairAntenna(w, hull, accent, truss, hw, hgt, len, trussStyle: true);
                AddKorathBowTip(w, hw, hh, len * 0.96f);
                break;
        }

        if (isCargo)
        {
            float panelZ = len * 0.04f;
            float panelH = hh * 0.52f;
            float solarReach = hullKey is "freighter_bulk" ? 0.42f : 0.46f;
            AddNasaSolarArray(w, -wid * solarReach, -hw * 0.04f, panelH, hgt * 0.92f, panelZ - len * 0.06f, panelZ + len * 0.06f, 2, 2);
            AddNasaSolarArray(w, hw * 0.04f, wid * solarReach, panelH, hgt * 0.92f, panelZ - len * 0.06f, panelZ + len * 0.06f, 2, 2);
            AddMatBridge(w, -hw, -wid * 0.32f, panelH + hh * 0.02f, panelH + hh * 0.08f, panelZ);
            AddMatBridge(w, hw, wid * 0.32f, panelH + hh * 0.02f, panelH + hh * 0.08f, panelZ);
        }
        else if (hullKey != "miner_basic" && hullKey != "support_repair")
        {
            for (int r = 0; r < 2; r++)
            {
                float z = MathHelper.Lerp(sternZ + len * 0.04f, bowZ * 0.20f, r);
                float side = r % 2 == 0 ? -1f : 1f;
                float radOut = hullKey is "miner_eva" or "miner_tractor" ? hw + wid * 0.06f : hw + wid * 0.08f;
                AddRadiatorPanel(w, side * hw * 0.78f, side * radOut, hh * 0.46f, hh * 0.88f, z - len * 0.04f, z + len * 0.04f);
            }
        }

        AddBoxMat(w, engine, -hw * 0.18f, -hw * 0.04f, 0, hh * 0.24f, sternZ - len * 0.04f, sternZ + len * 0.02f);
        AddBoxMat(w, engine, hw * 0.04f, hw * 0.18f, 0, hh * 0.24f, sternZ - len * 0.04f, sternZ + len * 0.02f);
        AddVasudanUtilityBridge(w, hull, accent, hw, hgt, len);

        if (hullKey is not "miner_basic")
        {
            w.TriMat(shield, -hw * 0.05f, hgt * 0.66f, len * 0.08f, hw * 0.05f, hgt * 0.66f, len * 0.08f, 0, hgt * 0.70f, len * 0.10f);
            float cupolaY = hullKey is "support_repair" ? hh * 0.92f : hullKey is "freighter_bulk" ? hh * 0.96f : hh * 1.02f;
            AddNasaCupola(w, 0, cupolaY, bowZ * 0.22f, hw * 0.18f, hh * 0.14f, cylSegs / 2);
        }
    }

    // ── Korath small/light combat truss hulls — role-distinct compact silhouettes ─

    /// <summary>Narrow dorsal keel truss band along +Z — industrial spine read without beam widen.</summary>
    private static void AddKorathDorsalKeelBand(RaceMeshWriter w, float hw, float hh, float z0, float z1, int segs = 3)
    {
        var truss = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        for (int k = 0; k < segs; k++)
        {
            float t = k / MathF.Max(1f, segs - 1);
            float z = MathHelper.Lerp(z0, z1, t);
            float halfK = hw * MathHelper.Lerp(0.04f, 0.02f, t);
            var mat = k == segs - 1 ? accent : truss;
            w.TriMat(mat,
                -halfK, hh * (0.72f + t * 0.08f), z, halfK, hh * (0.72f + t * 0.08f), z,
                0, hh * (0.80f + t * 0.06f), z + (z1 - z0) * 0.02f);
        }
    }

    /// <summary>Modest forward bow tip — re-anchors nose toward hull +Z envelope bound.</summary>
    private static void AddKorathBowTip(RaceMeshWriter w, float hw, float hh, float bowZ)
    {
        var truss = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float baseZ = bowZ * 0.84f;
        w.TriMat(truss, -hw * 0.08f, hh * 0.14f, baseZ, hw * 0.08f, hh * 0.14f, baseZ, 0, hh * 0.40f, bowZ);
        w.TriMat(accent, -hw * 0.03f, hh * 0.36f, bowZ * 0.96f, hw * 0.03f, hh * 0.36f, bowZ * 0.96f, 0, hh * 0.46f, bowZ);
    }

    private static void BuildKorathScoutTruss(RaceMeshWriter w, float len, float wid, float hgt)
    {
        float hw = wid * 0.30f;
        float hh = hgt * 0.38f;
        int cylSegs = 4;
        float bowZ = len * 0.56f;
        float sternZ = -len * 0.14f;

        AddNasaTrussSpine(w, hw * 0.22f, hh * 0.32f, sternZ, bowZ * 0.90f, 4);
        AddNasaCylinder(w, 0, hh * 0.14f, len * 0.20f, hw * 0.62f, hh * 0.95f, cylSegs);
        AddNasaCylinder(w, 0, hh * 0.10f, len * 0.02f, hw * 0.48f, hh * 0.72f, cylSegs);
        AddDockingRing(w, 0, hh * 0.86f, len * 0.32f, hw * 0.46f, hw * 0.62f, cylSegs);
        AddMatBridge(w, -wid * 0.42f, -wid * 0.46f, hh * 0.38f, hh * 0.48f, len * 0.06f);
        AddMatBridge(w, wid * 0.42f, wid * 0.46f, hh * 0.38f, hh * 0.48f, len * 0.06f);
        AddKorathDorsalKeelBand(w, hw, hh, len * 0.04f, bowZ * 0.84f, 3);
        AddKorathBowTip(w, hw, hh, len * 0.96f);
    }

    private static void BuildKorathFighterTruss(RaceMeshWriter w, float len, float wid, float hgt)
    {
        float bowZ = len * 0.58f;
        float sternZ = -len * 0.16f;
        float hw = wid * 0.34f;
        float hh = hgt * 0.40f;
        int cylSegs = 8;

        AddNasaTrussSpine(w, hw * 0.28f, hh * 0.34f, sternZ, bowZ * 0.92f, 6);
        for (int m = 0; m < 3; m++)
        {
            float cz = MathHelper.Lerp(sternZ * 0.40f, len * 0.18f, m / 2f);
            float modR = hw * (0.52f + m * 0.04f);
            float modH = hh * (0.82f + (m == 1 ? 0.08f : 0f));
            AddNasaCylinder(w, 0, hh * 0.12f, cz, modR, modH, cylSegs);
            if (m < 2)
                AddDockingRing(w, 0, hh * 0.10f + modH, cz + modH * 0.24f, modR * 0.76f, modR * 1.02f, cylSegs / 2);
        }

        float panelH = hh * 0.52f;
        float panelZ = len * 0.04f;
        AddNasaSolarArray(w, -wid * 0.50f, -hw * 0.04f, panelH, hgt * 0.94f, panelZ - len * 0.05f, panelZ + len * 0.05f, 3, 4);
        AddNasaSolarArray(w, hw * 0.04f, wid * 0.50f, panelH, hgt * 0.94f, panelZ - len * 0.05f, panelZ + len * 0.05f, 3, 4);
        AddMatBridge(w, -hw, -wid * 0.30f, panelH + hh * 0.02f, panelH + hh * 0.07f, panelZ);
        AddMatBridge(w, hw, wid * 0.30f, panelH + hh * 0.02f, panelH + hh * 0.07f, panelZ);
        AddRadiatorPanel(w, -hw * 0.88f, -hw * 0.62f, hh * 0.44f, hh * 0.88f, sternZ * 0.50f, sternZ * 0.20f);
        AddRadiatorPanel(w, hw * 0.62f, hw * 0.88f, hh * 0.44f, hh * 0.88f, sternZ * 0.50f, sternZ * 0.20f);
        AddKorathDorsalKeelBand(w, hw, hh, len * 0.04f, bowZ * 0.82f, 4);
        AddKorathBowTip(w, hw, hh, len * 0.96f);
    }

    private static void BuildKorathInterceptorTruss(RaceMeshWriter w, float len, float wid, float hgt)
    {
        float bowZ = len * 0.56f;
        float sternZ = -len * 0.14f;
        float hw = wid * 0.34f;
        float hh = hgt * 0.46f;
        int cylSegs = 5;

        AddNasaTrussSpine(w, hw * 0.28f, hh * 0.34f, sternZ, bowZ * 0.90f, 5);
        for (int m = 0; m < 2; m++)
        {
            float cz = MathHelper.Lerp(len * 0.02f, len * 0.16f, m);
            AddNasaCylinder(w, 0, hh * 0.12f, cz, hw * 0.58f, hh * (0.92f - m * 0.06f), cylSegs);
            if (m == 0)
                AddDockingRing(w, 0, hh * 0.90f, cz + hh * 0.26f, hw * 0.42f, hw * 0.58f, cylSegs);
        }

        float panelH = hh * 0.52f;
        float panelZ = len * 0.04f;
        AddNasaSolarArray(w, -wid * 0.50f, -hw * 0.04f, panelH, hgt * 0.94f, panelZ - len * 0.05f, panelZ + len * 0.05f, 2, 3);
        AddNasaSolarArray(w, hw * 0.04f, wid * 0.50f, panelH, hgt * 0.94f, panelZ - len * 0.05f, panelZ + len * 0.05f, 2, 3);
        AddMatBridge(w, -hw, -wid * 0.30f, panelH + hh * 0.02f, panelH + hh * 0.07f, panelZ);
        AddMatBridge(w, hw, wid * 0.30f, panelH + hh * 0.02f, panelH + hh * 0.07f, panelZ);
        AddKorathDorsalKeelBand(w, hw, hh, len * 0.04f, bowZ * 0.82f, 3);
        AddKorathBowTip(w, hw, hh, len * 0.96f);
    }

    private static void BuildKorathDroneTruss(RaceMeshWriter w, float len, float wid, float hgt)
    {
        float l = len * 0.903f;
        float hw = wid * 0.28f;
        float hh = hgt * 0.34f;
        int cylSegs = 4;
        float bowZ = l * 0.48f;
        float sternZ = -l * 0.40f;

        AddNasaTrussSpine(w, hw * 0.20f, hh * 0.28f, sternZ, bowZ * 0.86f, 4);
        AddNasaCylinder(w, 0, hh * 0.10f, l * 0.12f, hw * 0.42f, hh * 0.62f, cylSegs);
        AddNasaCylinder(w, -hw * 0.38f, hh * 0.08f, -l * 0.10f, hw * 0.36f, hh * 0.54f, cylSegs);
        AddNasaCylinder(w, hw * 0.36f, hh * 0.08f, -l * 0.14f, hw * 0.34f, hh * 0.52f, cylSegs);
        AddMatBridge(w, -hw * 0.38f, hw * 0.36f, hh * 0.42f, hh * 0.48f, -l * 0.10f);
        AddKorathDorsalKeelBand(w, hw, hh, -l * 0.02f, bowZ * 0.80f, 3);
        AddKorathBowTip(w, hw, hh, bowZ);
    }

    private static void BuildKorathHeroTruss(RaceMeshWriter w, float len, float wid, float hgt)
    {
        float l = len * 0.96f;
        float hw = wid * 0.36f;
        float hh = hgt * 0.42f;
        int cylSegs = 6;

        AddNasaTrussSpine(w, hw * 0.30f, hh * 0.36f, -l * 0.56f, l * 0.54f, 6);
        for (int m = 0; m < 3; m++)
        {
            float cz = MathHelper.Lerp(-l * 0.28f, l * 0.28f, m / 2f);
            float modR = hw * (0.54f + (m == 1 ? 0.08f : 0f));
            float modH = hh * (0.88f + (m == 1 ? 0.12f : 0f));
            AddNasaCylinder(w, 0, hh * 0.12f, cz, modR, modH, cylSegs);
            if (m < 2)
                AddDockingRing(w, 0, hh * 0.92f + (m == 1 ? hh * 0.12f : 0f), cz + hh * 0.30f, modR * 0.76f, modR * 1.04f, cylSegs);
        }

        AddNasaCupola(w, 0, hh * 1.18f, l * 0.20f, hw * 0.28f, hh * 0.22f, cylSegs / 2);
        AddCommunicationsMast(w, 0, hh * 1.38f, l * 0.08f, hh * 0.48f, hw * 0.06f);
        w.TriMat(RaceMeshWriter.HullMaterial.Solar, -hw * 0.04f, hh * 1.22f, l * 0.24f, hw * 0.04f, hh * 1.22f, l * 0.24f, 0, hh * 1.30f, l * 0.28f);

        AddNasaSolarArray(w, -wid * 0.50f, -hw * 0.04f, hh * 0.56f, hgt * 0.94f, len * 0.02f, len * 0.10f, 3, 4);
        AddNasaSolarArray(w, hw * 0.04f, wid * 0.50f, hh * 0.56f, hgt * 0.94f, len * 0.02f, len * 0.10f, 3, 4);
        AddMatBridge(w, -hw, -wid * 0.28f, hh * 0.60f, hh * 0.66f, len * 0.04f);
        AddMatBridge(w, hw, wid * 0.28f, hh * 0.60f, hh * 0.66f, len * 0.04f);
        AddKorathBowTip(w, hw, hh, len * 0.96f);
    }

    /// <summary>Heavy cruiser — balanced truss mass, mid-hull armor belt frame, wide envelope booms.</summary>
    private static void BuildKorathCruiserHeavyTruss(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var truss = RaceMeshWriter.HullMaterial.Truss;
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float hw = wid * 0.44f;
        float hh = hgt * 0.44f;
        int cylSegs = 8;

        AddNasaTrussSpine(w, hw * 0.32f, hh * 0.34f, -len * 0.18f, len * 0.52f, 8);
        for (int m = 0; m < 5; m++)
        {
            float t = m / 4f;
            float cz = MathHelper.Lerp(-len * 0.12f, len * 0.24f, t);
            float modR = hw * (0.48f + 0.08f * MathF.Sin(m * 1.3f));
            float modH = hh * (0.74f + (m % 2) * 0.12f);
            AddNasaCylinder(w, 0, hh * 0.10f, cz, modR, modH, cylSegs);
            if (m < 4)
                AddDockingRing(w, 0, hh * 0.10f + modH, cz + modH * 0.26f, modR * 0.76f, modR * 1.04f, cylSegs / 2);
        }

        AddBoxMat(w, truss, -hw * 0.72f, hw * 0.72f, hh * 0.22f, hh * 0.46f, len * 0.04f, len * 0.18f);
        AddBoxMat(w, hull, -hw * 0.54f, -hw * 0.36f, hh * 0.14f, hh * 0.40f, len * 0.20f, len * 0.30f);
        AddBoxMat(w, hull, hw * 0.36f, hw * 0.54f, hh * 0.14f, hh * 0.40f, len * 0.20f, len * 0.30f);
        AddBoxMat(w, weapon, -hw * 0.10f, hw * 0.10f, hh * 0.08f, hh * 0.20f, len * 0.28f, len * 0.34f);
        AddBoxMat(w, hull, -hw * 0.12f, hw * 0.12f, hh * 0.10f, hh * 0.22f, len * 0.32f, len * 0.38f);

        for (int r = 0; r < 3; r++)
        {
            float z = MathHelper.Lerp(-len * 0.10f, len * 0.06f, r / 2f);
            float side = r % 2 == 0 ? -1f : 1f;
            AddRadiatorPanel(w, side * wid * 0.40f, side * wid * 0.44f, hh * 0.48f, hh * 0.92f, z - len * 0.04f, z + len * 0.04f);
        }

        float panelZ = len * 0.04f;
        float panelH = hh * 0.52f;
        AddNasaSolarArray(w, -wid * 0.46f, -hw * 0.06f, panelH, hgt * 0.92f, panelZ - len * 0.06f, panelZ + len * 0.06f, 3, 5);
        AddNasaSolarArray(w, hw * 0.06f, wid * 0.46f, panelH, hgt * 0.92f, panelZ - len * 0.06f, panelZ + len * 0.06f, 3, 5);
        AddMatBridge(w, -hw, -wid * 0.30f, panelH + hh * 0.02f, panelH + hh * 0.08f, panelZ);
        AddMatBridge(w, hw, wid * 0.30f, panelH + hh * 0.02f, panelH + hh * 0.08f, panelZ);
        AddKorathBowTip(w, hw, hh, len * 0.96f);

        AddNasaCupola(w, 0, hgt * 0.92f, len * 0.16f, hw * 0.18f, hh * 0.14f, cylSegs / 2);
        AddCommunicationsMast(w, 0, hgt * 0.94f, -len * 0.08f, hh * 0.30f, hw * 0.06f);
        AddBoxMat(w, truss, -hw * 0.08f, hw * 0.08f, hh * 0.50f, hh * 0.60f, -len * 0.06f, len * 0.28f);
    }

    /// <summary>Command carrier — flat flight-deck frame, hangar bay rigging, broad solar envelope.</summary>
    private static void BuildKorathCarrierCommandTruss(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var truss = RaceMeshWriter.HullMaterial.Truss;
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float hw = wid * 0.44f;
        float hh = hgt * 0.30f;
        int cylSegs = 6;

        AddNasaTrussSpine(w, hw * 0.28f, hh * 0.30f, -len * 0.18f, len * 0.50f, 4);
        AddBoxMat(w, truss, -hw * 0.22f, hw * 0.22f, hh * 0.18f, hh * 0.36f, len * 0.30f, len * 0.36f);
        AddBoxMat(w, hull, -hw * 0.14f, hw * 0.14f, hh * 0.08f, hh * 0.22f, len * 0.34f, len * 0.38f);
        AddBoxMat(w, truss, -hw * 0.34f, hw * 0.34f, hh * 0.12f, hh * 0.28f, -len * 0.14f, -len * 0.08f);

        for (int m = 0; m < 2; m++)
        {
            float t = m;
            float cz = MathHelper.Lerp(-len * 0.08f, len * 0.14f, t);
            float modR = hw * (0.42f + 0.06f * MathF.Sin(m * 1.0f));
            float modH = hh * (0.62f + (m % 2) * 0.08f);
            AddNasaCylinder(w, 0, hh * 0.08f, cz, modR, modH, cylSegs);
            if (m < 1)
                AddDockingRing(w, 0, hh * 0.08f + modH, cz + modH * 0.22f, modR * 0.74f, modR * 1.02f, cylSegs / 2);
        }

        AddBoxMat(w, hull, -hw * 0.72f, hw * 0.72f, hh * 0.52f, hh * 0.66f, -len * 0.22f, len * 0.16f);
        AddBoxMat(w, truss, -hw * 0.82f, hw * 0.82f, hh * 0.42f, hh * 0.56f, -len * 0.12f, len * 0.04f);
        AddBoxMat(w, weapon, -hw * 0.28f, hw * 0.28f, hh * 0.20f, hh * 0.28f, -len * 0.04f, len * 0.04f);
        AddBoxMat(w, truss, -hw * 0.06f, hw * 0.06f, hh * 0.56f, hh * 0.68f, -len * 0.08f, len * 0.24f);

        float zRad = len * 0.02f;
        AddRadiatorPanel(w, -wid * 0.42f, -wid * 0.48f, hh * 0.40f, hh * 0.72f, zRad - len * 0.04f, zRad + len * 0.04f);
        AddRadiatorPanel(w, wid * 0.42f, wid * 0.48f, hh * 0.40f, hh * 0.72f, zRad - len * 0.04f, zRad + len * 0.04f);

        float panelZ = len * 0.04f;
        float panelH = hh * 0.42f;
        AddNasaSolarArray(w, -wid * 0.50f, -hw * 0.06f, panelH, hgt * 0.90f, panelZ - len * 0.06f, panelZ + len * 0.06f, 2, 3);
        AddNasaSolarArray(w, hw * 0.06f, wid * 0.50f, panelH, hgt * 0.90f, panelZ - len * 0.06f, panelZ + len * 0.06f, 2, 3);
        AddMatBridge(w, -hw, -wid * 0.30f, panelH + hh * 0.02f, panelH + hh * 0.06f, panelZ);
        AddMatBridge(w, hw, wid * 0.30f, panelH + hh * 0.02f, panelH + hh * 0.06f, panelZ);
        AddKorathBowTip(w, hw, hh, len * 0.96f);

        AddNasaCupola(w, 0, hh * 0.82f, len * 0.14f, hw * 0.18f, hh * 0.12f, 4);
        AddCommunicationsMast(w, 0, hgt * 0.92f, -len * 0.06f, hh * 0.28f, hw * 0.06f);
    }

    /// <summary>Dreadnought — imposing prow rigging, extended spine, quad radiator banks.</summary>
    private static void BuildKorathDreadnoughtTruss(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var truss = RaceMeshWriter.HullMaterial.Truss;
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float l = len * 1.22f;
        float hw = wid * 0.50f;
        float hh = hgt * 0.32f;
        int cylSegs = 7;
        float bowZ = l * 0.57f;
        float sternZ = -l * 0.19f;

        AddNasaTrussSpine(w, hw * 0.28f, hh * 0.30f, sternZ, bowZ * 0.94f, 7);
        float prowZ = bowZ * 0.82f;
        for (int side = -1; side <= 1; side += 2)
        {
            float xOut = side * hw * 0.72f;
            w.TriMat(truss, 0, hh * 0.32f, prowZ, xOut, hh * 0.16f, prowZ - l * 0.04f, xOut * 0.40f, hh * 0.38f, prowZ + l * 0.02f);
            w.TriMat(truss, 0, hh * 0.32f, prowZ, xOut * 0.40f, hh * 0.38f, prowZ + l * 0.02f, 0, hh * 0.44f, prowZ + l * 0.05f);
            w.TriMat(truss, xOut * 0.52f, hh * 0.20f, prowZ - l * 0.02f, xOut, hh * 0.14f, prowZ, 0, hh * 0.26f, prowZ + l * 0.01f);
        }
        AddBoxMat(w, truss, -hw * 0.16f, hw * 0.16f, hh * 0.38f, hh * 0.52f, l * 0.30f, l * 0.38f);
        AddBoxMat(w, hull, -hw * 0.12f, hw * 0.12f, hh * 0.08f, hh * 0.18f, l * 0.36f, l * 0.42f);

        for (int m = 0; m < 4; m++)
        {
            float t = m / 3f;
            float cz = MathHelper.Lerp(-l * 0.12f, l * 0.24f, t);
            float modR = hw * (0.44f + 0.08f * MathF.Sin(m * 1.2f));
            float modH = hh * (0.66f + (m % 2) * 0.08f);
            AddNasaCylinder(w, 0, hh * 0.08f, cz, modR, modH, cylSegs);
            if (m < 3)
                AddDockingRing(w, 0, hh * 0.08f + modH, cz + modH * 0.24f, modR * 0.76f, modR * 1.04f, cylSegs / 2);
        }
        AddNasaCylinder(w, 0, hh * 0.08f, sternZ * 0.55f, hw * 0.36f, hh * 0.52f, 6);
        for (int side = -1; side <= 1; side += 2)
        {
            float xOut = side * hw * 0.64f;
            w.TriMat(truss, xOut, hh * 0.20f, bowZ * 0.72f, xOut - side * hw * 0.04f, hh * 0.26f, bowZ * 0.76f, xOut, hh * 0.16f, bowZ * 0.78f);
            w.TriMat(truss, xOut - side * hw * 0.02f, hh * 0.30f, bowZ * 0.80f, xOut, hh * 0.24f, bowZ * 0.76f, xOut, hh * 0.16f, bowZ * 0.78f);
        }

        AddBoxMat(w, hull, -hw * 0.52f, -hw * 0.34f, hh * 0.10f, hh * 0.32f, l * 0.26f, l * 0.34f);
        AddBoxMat(w, hull, hw * 0.34f, hw * 0.52f, hh * 0.10f, hh * 0.32f, l * 0.26f, l * 0.34f);
        AddBoxMat(w, weapon, -hw * 0.10f, hw * 0.10f, hh * 0.05f, hh * 0.16f, l * 0.32f, l * 0.40f);
        AddBoxMat(w, truss, -hw * 0.08f, hw * 0.08f, hh * 0.44f, hh * 0.52f, sternZ * 0.40f, l * 0.22f);

        for (int r = 0; r < 3; r++)
        {
            float z = MathHelper.Lerp(sternZ * 0.40f, l * 0.06f, r / 2f);
            float side = r % 2 == 0 ? -1f : 1f;
            AddRadiatorPanel(w, side * wid * 0.42f, side * wid * 0.48f, hh * 0.38f, hh * 0.76f, z - l * 0.04f, z + l * 0.04f);
        }

        float panelZ = l * 0.04f;
        float panelH = hh * 0.40f;
        AddNasaSolarArray(w, -wid * 0.50f, -hw * 0.06f, panelH, hgt * 0.86f, panelZ - l * 0.06f, panelZ + l * 0.06f, 3, 4);
        AddNasaSolarArray(w, hw * 0.06f, wid * 0.50f, panelH, hgt * 0.86f, panelZ - l * 0.06f, panelZ + l * 0.06f, 3, 4);
        AddMatBridge(w, -hw, -wid * 0.30f, panelH + hh * 0.02f, panelH + hh * 0.06f, panelZ);
        AddMatBridge(w, hw, wid * 0.30f, panelH + hh * 0.02f, panelH + hh * 0.06f, panelZ);
        AddKorathDorsalKeelBand(w, hw, hh, l * 0.06f, bowZ * 0.86f, 5);
        w.TriMat(truss, -hw * 0.10f, hh * 0.34f, l * 0.28f, hw * 0.10f, hh * 0.34f, l * 0.28f, 0, hh * 0.42f, l * 0.32f);
        w.TriMat(hull, -hw * 0.08f, hh * 0.12f, sternZ * 0.30f, hw * 0.08f, hh * 0.12f, sternZ * 0.30f, 0, hh * 0.20f, sternZ * 0.36f);
        w.TriMat(truss, -hw * 0.06f, hh * 0.46f, l * 0.14f, hw * 0.06f, hh * 0.46f, l * 0.14f, 0, hh * 0.50f, l * 0.18f);
        AddKorathBowTip(w, hw, hh, bowZ);

        AddNasaCupola(w, 0, hh * 0.72f, l * 0.18f, hw * 0.18f, hh * 0.10f, 4);
        AddCommunicationsMast(w, 0, hh * 1.62f, sternZ * 0.30f, hh * 0.22f, hw * 0.06f);
    }

    // ── Orbital truss (Korath) — late-90s NASA stacked modules on filled truss spine ─

    private static void BuildOrbitalTruss(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        switch (hullKey)
        {
            case "scout":
            case "scout_light":
                BuildKorathScoutTruss(w, len, wid, hgt);
                return;
            case "fighter":
            case "fighter_basic":
                BuildKorathFighterTruss(w, len, wid, hgt);
                return;
            case "interceptor":
            case "interceptor_mk2":
                BuildKorathInterceptorTruss(w, len, wid, hgt);
                return;
            case "drone":
            case "drone_swarm":
                BuildKorathDroneTruss(w, len, wid, hgt);
                return;
            case "hero":
            case "hero_default":
                BuildKorathHeroTruss(w, len, wid, hgt);
                return;
            case "corvette":
            case "corvette_fast":
                BuildKorathCorvetteTruss(w, len, wid, hgt);
                return;
            case "frigate":
                BuildKorathFrigateTruss(w, len, wid, hgt);
                return;
            case "frigate_strike":
                BuildKorathFrigateStrikeTruss(w, len, wid, hgt);
                return;
            case "bomber":
            case "bomber_heavy":
                BuildKorathBomberTruss(w, len, wid, hgt);
                return;
            case "gunship":
            case "gunship_heavy":
                BuildKorathGunshipTruss(w, len, wid, hgt);
                return;
            case "destroyer":
            case "destroyer_assault":
                BuildKorathDestroyerTruss(w, len, wid, hgt);
                return;
            case "cruiser":
            case "cruiser_heavy":
                BuildKorathCruiserHeavyTruss(w, len, wid, hgt);
                return;
            case "carrier":
            case "carrier_command":
                BuildKorathCarrierCommandTruss(w, len, wid, hgt);
                return;
            case "dreadnought":
                BuildKorathDreadnoughtTruss(w, len, wid, hgt);
                return;
            case "miner_basic":
            case "miner_eva":
            case "miner_tractor":
            case "transport_cargo":
            case "freighter_bulk":
            case "support_repair":
                BuildTrussUtilitySolid(w, hullKey, len, wid, hgt);
                return;
        }

        float mass = MassScale(hullKey);
        float l = len * (0.88f + mass * 0.08f);
        float hw = wid * 0.36f;
        float hh = hgt * 0.4f;
        bool compact = hullKey is "drone" or "drone_swarm" or "scout" or "scout_light";
        bool capital = hullKey is "dreadnought" or "carrier" or "carrier_command" or "cruiser" or "cruiser_heavy";
        int cylSegs = HullSections(hullKey, 10, 6, 12);

        AddNasaTrussSpine(w, hw * 0.3f, hh * 0.35f, -l * 0.58f, l * 0.62f, HullSections(hullKey, 8, 4, 12));

        int modules = compact ? 2 : capital ? 5 : 3;
        for (int m = 0; m < modules; m++)
        {
            float t = m / (float)Math.Max(1, modules - 1);
            float cz = MathHelper.Lerp(-l * 0.42f, l * 0.38f, t);
            float modR = hw * (compact ? 0.7f : 0.52f + 0.1f * MathF.Sin(m * 1.4f));
            float modH = hh * (compact ? 1.05f : 0.82f + (m % 2) * 0.16f);
            float cy = hh * 0.12f;
            AddNasaCylinder(w, 0, cy, cz, modR, modH, cylSegs);
            if (m < modules - 1)
                AddDockingRing(w, 0, cy + modH, cz + modH * 0.32f, modR * 0.8f, modR * 1.1f, cylSegs);
        }

        if (!compact)
        {
            int rads = capital ? 3 : 2;
            for (int r = 0; r < rads; r++)
            {
                float z = MathHelper.Lerp(-l * 0.46f, -l * 0.1f, r / (float)Math.Max(1, rads - 1));
                float side = r % 2 == 0 ? -1f : 1f;
                AddRadiatorPanel(w, side * hw * 0.92f, side * (hw + wid * 0.2f), hh * 0.52f, hh * 1.02f, z - l * 0.06f, z + l * 0.06f);
            }
        }

        if (!compact)
        {
            float panelSpan = wid * (capital ? 1.1f : 0.84f);
            float panelZ = l * 0.02f;
            float panelH = hh * 0.6f;
            int gridRows = capital ? 4 : 3;
            int gridCols = capital ? 6 : 4;
            AddNasaSolarArray(w, -hw - panelSpan, -hw * 0.06f, panelH, panelH + hh * 0.22f, panelZ - l * 0.14f, panelZ + l * 0.14f, gridRows, gridCols);
            AddNasaSolarArray(w, hw * 0.06f, hw + panelSpan, panelH, panelH + hh * 0.22f, panelZ - l * 0.14f, panelZ + l * 0.14f, gridRows, gridCols);
            AddMatBridge(w, -hw, -hw - panelSpan * 0.4f, panelH + hh * 0.02f, panelH + hh * 0.08f, panelZ);
            AddMatBridge(w, hw, hw + panelSpan * 0.4f, panelH + hh * 0.02f, panelH + hh * 0.08f, panelZ);
        }

        if (!compact)
            AddNasaCupola(w, 0, hh * 1.15f, l * 0.26f, hw * 0.26f, hh * 0.2f, cylSegs / 2);

        if (capital)
            AddCommunicationsMast(w, 0, hh * 1.32f, -l * 0.2f, hh * 0.52f, hw * 0.07f);
    }

    // â”€â”€ Blocky â€” stacked rectangular decks, flat armor faces â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static void BuildBlocky(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float mass = MassScale(hullKey);
        float l = len * (0.78f + mass * 0.08f);
        float bw = wid * (1.05f + mass * 0.12f);
        float bh = hgt * (0.82f + mass * 0.18f);

        AddBox(w, -bw * 0.5f, bw * 0.5f, 0, bh, -l * 0.55f, l * 0.55f);
        AddBox(w, -bw * 0.32f, bw * 0.32f, bh, bh + bh * 0.45f, l * 0.05f, l * 0.42f);

        int decks = hullKey is "dreadnought" or "carrier" or "cruiser" ? 3 : 2;
        for (int d = 0; d < decks; d++)
        {
            float z = -l * 0.35f + d * l * 0.28f;
            AddBox(w, -bw * 0.62f, -bw * 0.38f, bh * 0.15f, bh * 0.55f, z, z + l * 0.14f);
            AddBox(w, bw * 0.38f, bw * 0.62f, bh * 0.15f, bh * 0.55f, z, z + l * 0.14f);
        }

        AddBox(w, -bw * 0.18f, bw * 0.18f, bh * 0.45f, bh * 0.95f, l * 0.42f, l * 0.62f);
        if (hullKey is "gunship" or "bomber" or "destroyer")
            AddBox(w, -bw * 0.7f, bw * 0.7f, 0, bh * 0.35f, -l * 0.15f, l * 0.2f);
    }

    // â”€â”€ Streamlined (Vesper) â€” narrow needle fuselage, low profile â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static void BuildStreamlined(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float l = len * 1.22f;
        float nw = wid * 0.14f;
        float nh = hgt * 0.32f;
        int sections = hullKey is "dreadnought" or "carrier" ? 9 : 7;

        for (int i = 0; i < sections; i++)
        {
            float t0 = i / (float)sections;
            float t1 = (i + 1) / (float)sections;
            float z0 = MathHelper.Lerp(-l * 0.62f, l * 0.92f, t0);
            float z1 = MathHelper.Lerp(-l * 0.62f, l * 0.92f, t1);
            float r0 = SectionRadius(t0) * nw;
            float r1 = SectionRadius(t1) * nw;
            float h0 = nh * (0.55f + 0.45f * SectionRadius(t0));
            float h1 = nh * (0.55f + 0.45f * SectionRadius(t1));

            w.Tri(-r0, h0, z0, r0, h0, z0, 0, h0 * 1.15f, z0 + (z1 - z0) * 0.5f);
            w.Tri(-r0, h0, z0, 0, h0 * 1.15f, z0 + (z1 - z0) * 0.5f, -r1, h1, z1);
            w.Tri(r0, h0, z0, r1, h1, z1, 0, h1 * 1.15f, z0 + (z1 - z0) * 0.5f);
            w.Tri(-r0, 0, z0, -r1, 0, z1, r0, 0, z0);
            w.Tri(r0, 0, z0, -r1, 0, z1, r1, 0, z1);
        }

        if (hullKey is not "drone" and not "scout")
        {
            float finZ = -l * 0.35f;
            w.Tri(0, nh * 0.5f, finZ, -nw * 1.8f, nh * 0.08f, finZ - nw * 0.6f, nw * 1.8f, nh * 0.08f, finZ - nw * 0.6f);
        }
    }

    private static float SectionRadius(float t)
    {
        float belly = MathF.Sin(t * MathF.PI);
        float nose = t > 0.78f ? (1f - t) / 0.22f : 1f;
        return 0.35f + 0.65f * belly * nose;
    }

    // ── Aetherian (organic) — per-hull bio-loft silhouettes with role readability ─

    private static void BuildAetherianHull(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        switch (hullKey)
        {
            case "fighter":
            case "fighter_basic":
                BuildAetherianFighter(w, len, wid, hgt);
                return;
            case "hero":
            case "hero_default":
                BuildAetherianHero(w, len, wid, hgt);
                return;
            case "scout":
            case "scout_light":
                BuildAetherianScout(w, len, wid, hgt);
                return;
            case "interceptor":
            case "interceptor_mk2":
                BuildAetherianInterceptor(w, len, wid, hgt);
                return;
            case "drone":
            case "drone_swarm":
                BuildAetherianDrone(w, len, wid, hgt);
                return;
            case "corvette":
            case "corvette_fast":
                BuildAetherianCorvetteFast(w, len, wid, hgt);
                return;
            case "frigate":
            case "frigate_strike":
                BuildAetherianFrigateStrike(w, len, wid, hgt);
                return;
            case "bomber":
            case "bomber_heavy":
                BuildAetherianBomberHeavy(w, len, wid, hgt);
                return;
            case "gunship":
            case "gunship_heavy":
                BuildAetherianGunshipHeavy(w, len, wid, hgt);
                return;
            case "destroyer":
            case "destroyer_assault":
                BuildAetherianDestroyerAssault(w, len, wid, hgt);
                return;
            case "cruiser":
            case "cruiser_heavy":
                BuildAetherianCruiserHeavyOrganic(w, len, wid, hgt);
                return;
            case "carrier":
            case "carrier_command":
                BuildAetherianCarrierCommandOrganic(w, len, wid, hgt);
                return;
            case "dreadnought":
                BuildAetherianDreadnoughtOrganic(w, len, wid, hgt);
                return;
            case "miner_basic":
            case "miner_eva":
            case "miner_tractor":
            case "transport_cargo":
            case "freighter_bulk":
            case "support_repair":
                BuildOrganicUtilitySolid(w, hullKey, len, wid, hgt);
                return;
        }

        BuildBulbous(w, hullKey, len, wid, hgt);
    }

    /// <summary>Fast corvette — swept bio-wedge wedge, narrow lateral pods, modest +Z bow.</summary>
    private static void BuildAetherianCorvetteFast(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var fold = RaceMeshWriter.HullMaterial.Truss;
        var vein = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        float l = len * 1.03f;
        float bw = wid * 0.88f;
        float bh = hgt * 1.22f;

        AddLoftedHull(w, l, bw, bh, 12,
            t =>
            {
                float belly = 0.44f + 0.56f * MathF.Sin(t * MathF.PI);
                float nose = t > 0.80f ? (1f - t) / 0.20f : 1f;
                float sweep = t > 0.62f ? 1f + (t - 0.62f) * 0.14f : 1f;
                return bw * belly * nose * sweep * 0.40f;
            },
            t => bh * (0.40f + 0.60f * MathF.Sin(t * MathF.PI)),
            -0.50f, 0.78f);

        AddLoftedHull(w, l * 0.40f, bw * 0.20f, bh * 0.52f, 4,
            t => bw * 0.06f * (1f - t * 0.4f),
            t => bh * (0.55f + t * 0.32f),
            0.12f, 0.60f);

        for (int p = 0; p < 2; p++)
        {
            float side = (p == 0 ? -1f : 1f) * bw * 0.34f;
            float z = l * (0.06f + p * 0.08f);
            AddIntegratedBulb(w, side, bh * 0.34f, z, bw * 0.13f, bh * 0.17f, 7);
        }

        AddBoxMat(w, weapon, -bw * 0.50f, -bw * 0.36f, bh * 0.04f, bh * 0.15f, l * 0.02f, l * 0.12f);
        AddBoxMat(w, weapon, bw * 0.36f, bw * 0.50f, bh * 0.04f, bh * 0.15f, l * 0.02f, l * 0.12f);
        w.TriMat(hull, -bw * 0.08f, bh * 0.26f, l * 0.72f, bw * 0.08f, bh * 0.26f, l * 0.72f, 0, bh * 0.46f, l * 0.78f);
        w.TriMat(fold, -bw * 0.06f, bh * 0.44f, l * 0.58f, bw * 0.06f, bh * 0.44f, l * 0.58f, 0, bh * 0.50f, l * 0.64f);
        AddBoxMat(w, engine, -bw * 0.12f, bw * 0.12f, 0, bh * 0.14f, -l * 0.46f, -l * 0.36f);
        w.TriMat(vein, -bw * 0.04f, bh * 0.50f, l * 0.18f, bw * 0.04f, bh * 0.50f, l * 0.18f, 0, bh * 0.56f, l * 0.24f);
    }

    /// <summary>Strike frigate — balanced gun-pod blooms, integrated lateral weapon sacs.</summary>
    private static void BuildAetherianFrigateStrike(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var fold = RaceMeshWriter.HullMaterial.Truss;
        var vein = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        float l = len * 1.10f;
        float bw = wid * 1.02f;
        float bh = hgt * 1.28f;

        AddLoftedHull(w, l, bw, bh, 10,
            t =>
            {
                float belly = 0.46f + 0.54f * MathF.Sin(t * MathF.PI);
                float nose = t > 0.78f ? (1f - t) / 0.22f : 1f;
                float bulge = MathF.Max(0f, 1f - MathF.Abs(t - 0.42f) * 4.5f) * 0.12f;
                return bw * belly * nose * (0.38f + bulge);
            },
            t => bh * (0.42f + 0.58f * MathF.Sin(t * MathF.PI)),
            -0.52f, 0.80f);

        for (int p = 0; p < 3; p++)
        {
            float side = (p - 1) * bw * 0.30f;
            float z = MathHelper.Lerp(-l * 0.04f, l * 0.22f, p / 2f);
            AddIntegratedBulb(w, side, bh * 0.38f, z, bw * 0.15f, bh * 0.20f, 5);
            if (p != 1)
                AddBoxMat(w, weapon, side - bw * 0.08f, side + bw * 0.08f, bh * 0.06f, bh * 0.18f, z - l * 0.04f, z + l * 0.04f);
        }

        AddBoxMat(w, fold, -bw * 0.10f, bw * 0.10f, bh * 0.48f, bh * 0.58f, l * 0.12f, l * 0.22f);
        w.TriMat(hull, -bw * 0.10f, bh * 0.28f, l * 0.66f, bw * 0.10f, bh * 0.28f, l * 0.66f, 0, bh * 0.44f, l * 0.72f);
        AddBoxMat(w, weapon, -bw * 0.10f, bw * 0.10f, bh * 0.06f, bh * 0.18f, l * 0.20f, l * 0.32f);
        AddBoxMat(w, engine, -bw * 0.14f, bw * 0.14f, 0, bh * 0.16f, -l * 0.48f, -l * 0.38f);
        for (int v = 0; v < 3; v++)
        {
            float z = l * (0.04f + v * 0.08f);
            w.TriMat(vein, -bw * 0.05f, bh * 0.52f, z, bw * 0.05f, bh * 0.52f, z, 0, bh * 0.58f, z + l * 0.006f);
        }
    }

    /// <summary>Heavy bomber — wide payload sac, dorsal membrane spine, modest elongation.</summary>
    private static void BuildAetherianBomberHeavy(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var fold = RaceMeshWriter.HullMaterial.Truss;
        var vein = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        float l = len * 1.02f;
        float bw = wid * 1.08f;
        float bh = hgt * 1.18f;

        AddLoftedHull(w, l, bw, bh, 13,
            t =>
            {
                float belly = 0.52f + 0.48f * MathF.Sin(t * MathF.PI);
                float sac = MathF.Max(0f, 1f - MathF.Abs(t - 0.38f) * 3.2f) * 0.18f;
                float nose = t > 0.78f ? (1f - t) / 0.22f : 1f;
                return bw * belly * nose * (0.42f + sac);
            },
            t => bh * (0.38f + 0.52f * MathF.Sin(t * MathF.PI)),
            -0.54f, 0.82f);

        AddLoftedHull(w, l * 0.55f, bw * 0.28f, bh * 0.70f, 5,
            t => bw * 0.08f * (1f - t * 0.35f),
            t => bh * (0.50f + t * 0.38f),
            0.08f, 0.58f);

        w.TriMat(weapon, -bw * 0.28f, 0, -l * 0.02f, bw * 0.28f, 0, -l * 0.02f, 0, bh * 0.10f, l * 0.08f);
        AddBoxMat(w, fold, -bw * 0.22f, bw * 0.22f, bh * 0.26f, bh * 0.42f, l * 0.02f, l * 0.14f);

        for (int p = 0; p < 2; p++)
        {
            float side = (p == 0 ? -1f : 1f) * bw * 0.38f;
            AddIntegratedBulb(w, side, bh * 0.30f, l * 0.06f, bw * 0.12f, bh * 0.16f, 7);
        }

        w.TriMat(hull, -bw * 0.10f, bh * 0.24f, l * 0.66f, bw * 0.10f, bh * 0.24f, l * 0.66f, 0, bh * 0.38f, l * 0.72f);
        AddBoxMat(w, engine, -bw * 0.16f, bw * 0.16f, 0, bh * 0.14f, -l * 0.50f, -l * 0.40f);
        for (int v = 0; v < 4; v++)
        {
            float z = l * (0.02f + v * 0.07f);
            float xSpan = bw * MathHelper.Lerp(0.18f, 0.06f, v / 3f);
            w.TriMat(vein, -xSpan, bh * 0.44f, z, xSpan, bh * 0.44f, z, 0, bh * 0.50f, z + l * 0.006f);
        }
    }

    /// <summary>Heavy gunship — flattened chin weapon pod, low assault membrane frame.</summary>
    private static void BuildAetherianGunshipHeavy(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var fold = RaceMeshWriter.HullMaterial.Truss;
        var vein = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        float l = len * 1.06f;
        float bw = wid * 1.04f;
        float bh = hgt * 1.10f;

        AddLoftedHull(w, l, bw, bh, 12,
            t =>
            {
                float belly = 0.50f + 0.50f * MathF.Sin(t * MathF.PI);
                float chin = t > 0.22f && t < 0.52f ? 0.10f : 0f;
                float nose = t > 0.76f ? (1f - t) / 0.24f : 1f;
                return bw * belly * nose * (0.40f + chin);
            },
            t => bh * (0.36f + 0.48f * MathF.Sin(t * MathF.PI)),
            -0.50f, 0.74f);

        w.TriMat(weapon, -bw * 0.14f, 0, l * 0.22f, bw * 0.14f, 0, l * 0.22f, 0, bh * 0.08f, l * 0.42f);
        w.TriMat(weapon, -bw * 0.10f, 0, l * 0.30f, bw * 0.10f, 0, l * 0.30f, 0, bh * 0.08f, l * 0.48f);
        AddBoxMat(w, weapon, -bw * 0.44f, -bw * 0.30f, bh * 0.04f, bh * 0.12f, l * 0.06f, l * 0.14f);
        AddBoxMat(w, weapon, bw * 0.30f, bw * 0.44f, bh * 0.04f, bh * 0.12f, l * 0.06f, l * 0.14f);
        AddBoxMat(w, fold, -bw * 0.24f, bw * 0.24f, bh * 0.28f, bh * 0.40f, -l * 0.02f, l * 0.12f);

        for (int p = 0; p < 2; p++)
        {
            float side = (p == 0 ? -1f : 1f) * bw * 0.32f;
            AddIntegratedBulb(w, side, bh * 0.32f, l * 0.04f, bw * 0.14f, bh * 0.16f, 7);
        }

        w.TriMat(hull, -bw * 0.10f, bh * 0.22f, l * 0.58f, bw * 0.10f, bh * 0.22f, l * 0.58f, 0, bh * 0.34f, l * 0.64f);
        AddBoxMat(w, engine, -bw * 0.14f, bw * 0.14f, 0, bh * 0.14f, -l * 0.46f, -l * 0.36f);
        w.TriMat(vein, -bw * 0.06f, bh * 0.46f, l * 0.08f, bw * 0.06f, bh * 0.46f, l * 0.08f, 0, bh * 0.52f, l * 0.12f);
    }

    /// <summary>Assault destroyer — dorsal spine ridge, multi-pod assault envelope.</summary>
    private static void BuildAetherianDestroyerAssault(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var fold = RaceMeshWriter.HullMaterial.Truss;
        var vein = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        // wo-06-09 loop 15: aspect recovery — trim +Z over-elongation, widen assault beam
        float l = len * 1.04f;
        float bw = wid * 1.02f;
        float bh = hgt * 1.26f;

        AddLoftedHull(w, l, bw, bh, 8,
            t =>
            {
                float belly = 0.48f + 0.52f * MathF.Sin(t * MathF.PI);
                float ridge = MathF.Max(0f, 1f - MathF.Abs(t - 0.46f) * 3.6f) * 0.12f;
                float nose = t > 0.80f ? (1f - t) / 0.20f : 1f;
                return bw * belly * nose * (0.40f + ridge);
            },
            t => bh * (0.44f + 0.56f * MathF.Sin(t * MathF.PI)),
            -0.48f, 0.84f);

        AddLoftedHull(w, l * 0.54f, bw * 0.18f, bh * 0.78f, 3,
            t => bw * 0.06f * (1f - t * 0.28f),
            t => bh * (0.60f + t * 0.38f),
            0.20f, 0.72f);

        for (int p = 0; p < 2; p++)
        {
            float side = (p == 0 ? -1f : 1f) * bw * 0.26f;
            float z = MathHelper.Lerp(-l * 0.04f, l * 0.22f, p);
            AddIntegratedBulb(w, side, bh * 0.40f, z, bw * 0.10f, bh * 0.16f, 4);
        }

        AddBoxMat(w, weapon, -bw * 0.10f, bw * 0.10f, bh * 0.08f, bh * 0.20f, l * 0.48f, l * 0.58f);
        AddBoxMat(w, fold, -bw * 0.10f, bw * 0.10f, bh * 0.64f, bh * 0.78f, l * 0.10f, l * 0.18f);
        AddBoxMat(w, hull, -bw * 0.08f, bw * 0.08f, bh * 0.44f, bh * 0.54f, l * 0.66f, l * 0.76f);

        AddHullRing(w, bw * 0.18f, bh * 0.50f, -l * 0.16f, 5);

        AddBoxMat(w, engine, -bw * 0.16f, bw * 0.16f, 0, bh * 0.16f, -l * 0.50f, -l * 0.40f);
        AddBoxMat(w, vein, -bw * 0.06f, bw * 0.06f, bh * 0.56f, bh * 0.64f, l * 0.04f, l * 0.12f);
    }

    /// <summary>Heavy cruiser — balanced bio-mass, dorsal bloom ridge, lateral sac bulges.</summary>
    private static void BuildAetherianCruiserHeavyOrganic(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var fold = RaceMeshWriter.HullMaterial.Truss;
        var vein = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float l = len * 1.04f;
        float bw = wid * 1.10f;
        float bh = hgt * 1.30f;

        AddLoftedHull(w, l, bw, bh, 14,
            t =>
            {
                float belly = 0.48f + 0.52f * MathF.Sin(t * MathF.PI);
                float ridge = MathF.Max(0f, 1f - MathF.Abs(t - 0.44f) * 3.6f) * 0.12f;
                float nose = t > 0.82f ? (1f - t) / 0.18f : 1f;
                return bw * belly * nose * (0.40f + ridge);
            },
            t => bh * (0.44f + 0.56f * MathF.Sin(t * MathF.PI)),
            -0.54f, 0.78f);

        AddLoftedHull(w, l * 0.58f, bw * 0.22f, bh * 0.88f, 2,
            t => bw * 0.07f * (1f - t * 0.35f),
            t => bh * (0.60f + t * 0.38f),
            0.12f, 0.66f);

        for (int p = 0; p < 2; p++)
        {
            float side = (p == 0 ? -1f : 1f) * bw * 0.34f;
            float z = MathHelper.Lerp(-l * 0.06f, l * 0.20f, p);
            AddIntegratedBulb(w, side, bh * 0.38f, z, bw * 0.15f, bh * 0.21f, 4);
        }

        AddBoxMat(w, fold, -bw * 0.14f, bw * 0.14f, bh * 0.34f, bh * 0.48f, -l * 0.04f, l * 0.20f);
        AddBoxMat(w, weapon, -bw * 0.10f, bw * 0.10f, bh * 0.10f, bh * 0.18f, l * 0.54f, l * 0.64f);
        w.TriMat(hull, -bw * 0.08f, bh * 0.28f, l * 0.62f, bw * 0.08f, bh * 0.28f, l * 0.62f, 0, bh * 0.44f, l * 0.68f);

        AddOrganicCapitalEngineWells(w, len, wid, hgt, 3);
        AddOrganicCapitalBiolumRings(w, l, bw, bh, 4);
        for (int v = 0; v < 3; v++)
        {
            float z = l * (-0.04f + v * 0.07f);
            w.TriMat(vein, -bw * 0.06f, bh * 0.54f, z, bw * 0.06f, bh * 0.54f, z, 0, bh * 0.62f, z + l * 0.006f);
        }
    }

    /// <summary>Command carrier — flat deck/hangar membrane, broad organic envelope.</summary>
    private static void BuildAetherianCarrierCommandOrganic(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var membrane = RaceMeshWriter.HullMaterial.Radiator;
        var vein = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float l = len * 1.06f;
        float bw = wid * 1.18f;
        float bh = hgt * 1.14f;

        AddLoftedHull(w, l, bw, bh, 14,
            t =>
            {
                float belly = 0.52f + 0.38f * MathF.Sin(t * MathF.PI);
                float nose = t > 0.80f ? (1f - t) / 0.20f : 1f;
                return bw * belly * nose * 0.42f;
            },
            t => bh * (0.38f + 0.48f * MathF.Sin(t * MathF.PI)),
            -0.52f, 0.76f);

        for (int p = 0; p < 3; p++)
        {
            float side = (p - 1) * bw * 0.36f;
            float z = MathHelper.Lerp(-l * 0.08f, l * 0.20f, p / 2f);
            AddIntegratedBulb(w, side, bh * 0.34f, z, bw * 0.16f, bh * 0.18f, 6);
        }

        float deckY = bh * 0.60f;
        float deckZ0 = -l * 0.32f;
        float deckZ1 = l * 0.28f;
        w.TriMat(membrane, -bw * 0.42f, deckY, deckZ1, bw * 0.42f, deckY, deckZ1, 0, deckY * 1.06f, deckZ0 + l * 0.06f);
        w.TriMat(membrane, -bw * 0.42f, deckY, deckZ1, 0, deckY * 1.06f, deckZ0 + l * 0.06f, -bw * 0.42f, deckY, deckZ0);
        w.TriMat(membrane, bw * 0.42f, deckY, deckZ1, bw * 0.42f, deckY, deckZ0, 0, deckY * 1.06f, deckZ0 + l * 0.06f);
        for (int h = 0; h < 2; h++)
        {
            float z = MathHelper.Lerp(-l * 0.16f, l * 0.10f, h);
            w.TriMat(membrane, -bw * 0.34f, deckY * 0.94f, z, bw * 0.34f, deckY * 0.94f, z, 0, deckY, z + l * 0.010f);
        }

        float keelY = bh * 0.66f;
        for (int k = 0; k < 3; k++)
        {
            float z = MathHelper.Lerp(l * 0.04f, l * 0.22f, k / 2f);
            w.TriMat(membrane, -bw * 0.08f, keelY, z, bw * 0.08f, keelY, z, 0, keelY + bh * 0.06f, z + l * 0.008f);
        }

        AddBoxMat(w, weapon, -bw * 0.26f, bw * 0.26f, bh * 0.12f, bh * 0.20f, -l * 0.04f, l * 0.04f);
        w.TriMat(hull, -bw * 0.06f, bh * 0.24f, l * 0.10f, bw * 0.06f, bh * 0.24f, l * 0.10f, 0, bh * 0.30f, l * 0.14f);
        AddOrganicCapitalEngineWells(w, len, wid, hgt, 3);
        AddOrganicCapitalBiolumRings(w, l, bw, bh, 5);
        for (int v = 0; v < 3; v++)
        {
            float z = l * (0.04f + v * 0.06f);
            w.TriMat(vein, -bw * 0.05f, bh * 0.48f, z, bw * 0.05f, bh * 0.48f, z, 0, bh * 0.54f, z + l * 0.006f);
        }
    }

    /// <summary>Dreadnought — imposing prow pod cluster on extended bio-loft mass.</summary>
    private static void BuildAetherianDreadnoughtOrganic(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var fold = RaceMeshWriter.HullMaterial.Truss;
        var vein = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float l = len * 1.09f;
        float bw = wid * 1.14f;
        float bh = hgt * 1.18f;

        AddLoftedHull(w, l, bw, bh, 14,
            t =>
            {
                float belly = 0.46f + 0.54f * MathF.Sin(t * MathF.PI);
                float prow = t > 0.70f ? 1f + (t - 0.70f) * 0.18f : 1f;
                float nose = t > 0.84f ? (1f - t) / 0.16f : 1f;
                return bw * belly * nose * prow * 0.42f;
            },
            t => bh * (0.42f + 0.50f * MathF.Sin(t * MathF.PI)),
            -0.56f, 0.82f);

        float prowZ = l * 0.66f;
        for (int p = 0; p < 3; p++)
        {
            float side = (p - 1) * bw * 0.22f;
            AddIntegratedBulb(w, side, bh * 0.40f, prowZ, bw * 0.14f, bh * 0.16f, 6);
            if (p == 1)
                w.TriMat(weapon, side, bh * 0.44f, prowZ + l * 0.04f, side - bw * 0.06f, bh * 0.38f, prowZ, side + bw * 0.06f, bh * 0.38f, prowZ);
        }

        for (int p = 0; p < 3; p++)
        {
            float side = (p - 1) * bw * 0.36f;
            float z = MathHelper.Lerp(-l * 0.10f, l * 0.24f, p / 2f);
            AddIntegratedBulb(w, side, bh * 0.36f, z, bw * 0.15f, bh * 0.18f, 6);
        }

        AddBoxMat(w, fold, -bw * 0.12f, bw * 0.12f, bh * 0.28f, bh * 0.40f, l * 0.48f, l * 0.62f);
        AddBoxMat(w, weapon, -bw * 0.12f, bw * 0.12f, bh * 0.08f, bh * 0.16f, l * 0.62f, l * 0.72f);
        w.TriMat(hull, -bw * 0.08f, bh * 0.24f, l * 0.70f, bw * 0.08f, bh * 0.24f, l * 0.70f, 0, bh * 0.38f, l * 0.76f);

        AddOrganicCapitalEngineWells(w, len, wid, hgt, 3);
        AddOrganicCapitalBiolumRings(w, l, bw, bh, 6);
        for (int v = 0; v < 3; v++)
        {
            float z = l * (0.10f + v * 0.08f);
            w.TriMat(vein, -bw * 0.06f, bh * 0.48f, z, bw * 0.06f, bh * 0.48f, z, 0, bh * 0.54f, z + l * 0.006f);
        }
    }

    private static void AddOrganicCapitalEngineWells(RaceMeshWriter w, float len, float wid, float hgt, int count)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        float hw = wid * 0.5f;
        count = Math.Clamp(count, 2, 3);
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : i / (float)(count - 1);
            float cx = MathHelper.Lerp(-hw * 0.28f, hw * 0.28f, t);
            float halfW = hw * 0.10f;
            AddBoxMat(w, hull, cx - halfW, cx + halfW, 0, hgt * 0.18f, -len * 0.28f, -len * 0.14f);
            AddBoxMat(w, engine, cx - halfW * 0.50f, cx + halfW * 0.50f, hgt * 0.02f, hgt * 0.14f, -len * 0.30f, -len * 0.18f);
        }
    }

    private static void AddOrganicCapitalBiolumRings(RaceMeshWriter w, float l, float bw, float bh, int ringCount)
    {
        var vein = RaceMeshWriter.HullMaterial.Solar;
        for (int i = 0; i < ringCount; i++)
        {
            float t = (i + 0.5f) / ringCount;
            float z = MathHelper.Lerp(-l * 0.36f, l * 0.40f, t);
            float r = bw * (0.20f + 0.07f * MathF.Sin(t * MathF.PI * 2.2f));
            for (int s = 0; s < 8; s++)
            {
                float a0 = MathF.PI * 2f * s / 8f;
                float a1 = MathF.PI * 2f * (s + 1) / 8f;
                float x0 = MathF.Cos(a0) * r;
                float x1 = MathF.Cos(a1) * r;
                float zz0 = z + MathF.Sin(a0) * r * 0.12f;
                float zz1 = z + MathF.Sin(a1) * r * 0.12f;
                w.TriMat(vein, x0, bh * 0.50f, zz0, x1, bh * 0.50f, zz1, 0, bh * 0.56f, z);
            }
        }
    }

    /// <summary>Slim forward pod spine — readable scout role.</summary>
    private static void BuildAetherianScout(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var membrane = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float l = len * 0.90f;
        float hw = wid * 0.5f;
        float bh = hgt * 1.20f;

        AddOrganicBioLoft(w, hull, l, hw * 0.78f, bh, 9, 0.34f, -0.42f, 0.70f);
        AddOrganicBowTip(w, hull, accent, hw * 0.10f, bh * 0.48f, l * 0.58f, l * 0.09f);
        AddOrganicDorsalKeel(w, hull, membrane, accent, hw, bh, l, "scout_light");
        for (int side = -1; side <= 1; side += 2)
            AddOrganicWingSweep(w, hull, accent, side, hw, bh, l, 0.48f, 0.44f);
        AddIntegratedBulb(w, 0, bh * 0.52f, l * 0.10f, hw * 0.12f, bh * 0.18f, 5);
        for (int side = -1; side <= 1; side += 2)
            AddIntegratedBulb(w, side * hw * 0.22f, bh * 0.28f, -l * 0.04f, hw * 0.10f, bh * 0.14f, 5);
        AddBoxMat(w, shield, -hw * 0.06f, hw * 0.06f, bh * 0.62f, bh * 0.74f, l * 0.04f, l * 0.12f);
        for (int side = -1; side <= 1; side += 2)
            AddBoxMat(w, weapon, side * hw * 0.40f - hw * 0.06f, side * hw * 0.40f + hw * 0.06f,
                bh * 0.08f, bh * 0.16f, l * 0.02f, l * 0.10f);
        for (int e = 0; e < 3; e++)
        {
            float x = (e - 1) * hw * 0.14f;
            AddBoxMat(w, engine, x - hw * 0.05f, x + hw * 0.05f, 0, bh * 0.14f, -l * 0.34f, -l * 0.24f);
        }
        w.TriMat(membrane, -hw * 0.04f, bh * 0.40f, l * 0.02f, hw * 0.04f, bh * 0.40f, l * 0.02f, 0, bh * 0.50f, l * 0.10f);
    }

    /// <summary>Balanced wing-sweep bio-loft fighter profile.</summary>
    private static void BuildAetherianFighter(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var membrane = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float l = len * 0.92f;
        float hw = wid * 0.5f;
        float bh = hgt * 1.28f;
        float sweep = 0.55f;

        AddOrganicBioLoft(w, hull, l, hw * 0.88f, bh, 10, 0.42f, -0.48f, 0.68f);
        AddOrganicBowTip(w, hull, accent, hw * 0.14f, bh * 0.46f, l * 0.52f, l * 0.07f);
        AddOrganicDorsalKeel(w, hull, membrane, accent, hw, bh, l, "fighter_basic");
        for (int side = -1; side <= 1; side += 2)
            AddOrganicWingSweep(w, hull, accent, side, hw, bh, l, sweep, 0.46f);
        for (int p = 0; p < 2; p++)
        {
            float z = MathHelper.Lerp(-l * 0.04f, l * 0.18f, p);
            AddIntegratedBulb(w, 0, bh * 0.36f, z, hw * 0.14f, bh * 0.20f, 4);
        }
        for (int e = 0; e < 2; e++)
        {
            float x = (e - 0.5f) * hw * 0.36f;
            AddBoxMat(w, engine, x - hw * 0.06f, x + hw * 0.06f, 0, bh * 0.16f, -l * 0.38f, -l * 0.26f);
        }
        for (int side = -1; side <= 1; side += 2)
            AddBoxMat(w, weapon, side * hw * 0.44f - hw * 0.06f, side * hw * 0.44f + hw * 0.06f,
                bh * 0.08f, bh * 0.16f, l * 0.02f, l * 0.10f);
        AddBoxMat(w, shield, -hw * 0.08f, hw * 0.08f, bh * 0.66f, bh * 0.78f, l * 0.06f, l * 0.14f);
        w.TriMat(membrane, -hw * 0.06f, bh * 0.44f, l * 0.04f, hw * 0.06f, bh * 0.44f, l * 0.04f, 0, bh * 0.54f, l * 0.12f);
    }

    /// <summary>Aggressive swept-fin interceptor profile.</summary>
    private static void BuildAetherianInterceptor(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var membrane = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float l = len * 0.90f;
        float hw = wid * 0.5f;
        float bh = hgt * 1.22f;

        AddOrganicBioLoft(w, hull, l, hw * 0.78f, bh, 11, 0.36f, -0.44f, 0.72f);
        AddOrganicBowTip(w, hull, accent, hw * 0.12f, bh * 0.44f, l * 0.56f, l * 0.10f);
        AddOrganicDorsalKeel(w, hull, membrane, accent, hw, bh, l, "interceptor_mk2");
        for (int side = -1; side <= 1; side += 2)
            AddOrganicWingSweep(w, hull, accent, side, hw, bh, l, 0.72f, 0.48f);
        AddBoxMat(w, weapon, -hw * 0.08f, hw * 0.08f, bh * 0.10f, bh * 0.20f, l * 0.28f, l * 0.38f);
        for (int side = -1; side <= 1; side += 2)
            AddBoxMat(w, weapon, side * hw * 0.42f - hw * 0.06f, side * hw * 0.42f + hw * 0.06f,
                bh * 0.08f, bh * 0.16f, l * 0.02f, l * 0.10f);
        for (int e = 0; e < 3; e++)
        {
            float x = (e - 1) * hw * 0.16f;
            AddBoxMat(w, engine, x - hw * 0.05f, x + hw * 0.05f, 0, bh * 0.14f, -l * 0.34f, -l * 0.24f);
        }
        AddBoxMat(w, shield, -hw * 0.06f, hw * 0.06f, bh * 0.64f, bh * 0.76f, l * 0.04f, l * 0.12f);
        w.TriMat(membrane, -hw * 0.05f, bh * 0.42f, l * 0.08f, hw * 0.05f, bh * 0.42f, l * 0.08f, 0, bh * 0.52f, l * 0.16f);
    }

    /// <summary>Compact cluster-pod drone profile.</summary>
    private static void BuildAetherianDrone(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var membrane = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float l = len * 0.88f;
        float hw = wid * 0.5f;
        float bh = hgt * 1.10f;

        AddOrganicBioLoft(w, hull, l, hw * 0.58f, bh, 8, 0.30f, -0.36f, 0.60f);
        AddOrganicBowTip(w, hull, accent, hw * 0.06f, bh * 0.34f, l * 0.46f, l * 0.09f);
        AddOrganicDorsalKeel(w, hull, membrane, accent, hw, bh, l, "drone_swarm");
        float[] podZ = [-l * 0.12f, l * 0.06f, l * 0.20f];
        for (int p = 0; p < podZ.Length; p++)
            AddIntegratedBulb(w, 0, bh * (0.30f + p * 0.06f), podZ[p], hw * (0.12f - p * 0.02f), bh * 0.16f, 5);
        for (int side = -1; side <= 1; side += 2)
            AddIntegratedBulb(w, side * hw * 0.18f, bh * 0.22f, l * 0.04f, hw * 0.08f, bh * 0.12f, 4);
        AddBoxMat(w, engine, -hw * 0.10f, hw * 0.10f, 0, bh * 0.14f, -l * 0.30f, -l * 0.20f);
        for (int side = -1; side <= 1; side += 2)
            AddBoxMat(w, weapon, side * hw * 0.24f - hw * 0.05f, side * hw * 0.24f + hw * 0.05f,
                bh * 0.08f, bh * 0.14f, l * 0.12f, l * 0.20f);
        AddBoxMat(w, shield, -hw * 0.05f, hw * 0.05f, bh * 0.58f, bh * 0.68f, l * 0.04f, l * 0.12f);
        w.TriMat(membrane, -hw * 0.03f, bh * 0.36f, -l * 0.02f, hw * 0.03f, bh * 0.36f, -l * 0.02f, 0, bh * 0.44f, l * 0.06f);
    }

    /// <summary>Command crown bloom hero profile.</summary>
    private static void BuildAetherianHero(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var membrane = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float l = len * 0.94f;
        float hw = wid * 0.5f;
        float bh = hgt * 1.32f;
        float sweep = 0.55f;

        AddOrganicBioLoft(w, hull, l, hw * 0.96f, bh, 11, 0.44f, -0.50f, 0.74f);
        AddOrganicBowTip(w, hull, accent, hw * 0.12f, bh * 0.48f, l * 0.52f, l * 0.09f);
        AddOrganicDorsalKeel(w, hull, membrane, accent, hw, bh, l, "hero_default");
        for (int side = -1; side <= 1; side += 2)
            AddOrganicWingSweep(w, hull, accent, side, hw, bh, l, sweep, 0.50f);
        for (int p = 0; p < 3; p++)
        {
            float z = MathHelper.Lerp(-l * 0.20f, l * 0.22f, p / 2f);
            AddIntegratedBulb(w, 0, bh * 0.38f, z, hw * (0.16f - p * 0.02f), bh * 0.22f, 5);
        }
        AddIntegratedBulb(w, 0, bh * 0.76f, l * 0.18f, hw * 0.16f, bh * 0.20f, 5);
        w.TriMat(accent, -hw * 0.08f, bh * 0.72f, l * 0.20f, hw * 0.08f, bh * 0.72f, l * 0.20f, 0, bh * 0.84f, l * 0.26f);
        for (int e = 0; e < 3; e++)
        {
            float x = (e - 1) * hw * 0.18f;
            AddBoxMat(w, engine, x - hw * 0.06f, x + hw * 0.06f, 0, bh * 0.16f, -l * 0.40f, -l * 0.28f);
        }
        for (int side = -1; side <= 1; side += 2)
            AddBoxMat(w, weapon, side * hw * 0.48f - hw * 0.06f, side * hw * 0.48f + hw * 0.06f,
                bh * 0.08f, bh * 0.16f, l * 0.04f, l * 0.12f);
        AddBoxMat(w, shield, -hw * 0.08f, hw * 0.08f, bh * 0.70f, bh * 0.82f, l * 0.08f, l * 0.16f);
        w.TriMat(membrane, -hw * 0.06f, bh * 0.46f, l * 0.06f, hw * 0.06f, bh * 0.46f, l * 0.06f, 0, bh * 0.56f, l * 0.14f);
    }

    private static void AddOrganicBioLoft(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial mat, float l, float bw, float bh,
        int sections, float bulgeAmp, float zStart, float zEnd)
    {
        AddLoftedHullMat(w, mat, l, bw, bh, sections,
            t =>
            {
                float belly = 0.46f + 0.54f * MathF.Sin(t * MathF.PI);
                float nose = t > 0.78f ? (1f - t) / 0.22f : 1f;
                float bulge = bulgeAmp * MathF.Max(0f, 1f - MathF.Abs(t - 0.42f) * 4f);
                return bw * belly * nose * (0.40f + bulge);
            },
            t => bh * (0.44f + 0.56f * MathF.Sin(t * MathF.PI)),
            zStart, zEnd);
    }

    private static void AddOrganicBowTip(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial accent,
        float halfW, float peakH, float baseZ, float tipExtension)
    {
        float tipZ = baseZ + tipExtension;
        w.TriMat(hull, -halfW, peakH * 0.22f, baseZ, halfW, peakH * 0.22f, baseZ, 0, peakH, tipZ);
        w.TriMat(accent, -halfW * 0.5f, peakH * 0.72f, baseZ + tipExtension * 0.35f,
            halfW * 0.5f, peakH * 0.72f, baseZ + tipExtension * 0.35f, 0, peakH * 0.86f, tipZ);
    }

    /// <summary>Dorsal membrane keel band — modest +Z elongation read without lateral spread.</summary>
    private static void AddOrganicDorsalKeel(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial membrane,
        RaceMeshWriter.HullMaterial accent, float hw, float bh, float l, string hullKey)
    {
        float keelHalfW = hw * (hullKey switch
        {
            "scout_light" => 0.035f,
            "interceptor_mk2" => 0.038f,
            "fighter_basic" => 0.040f,
            "drone_swarm" => 0.028f,
            "hero_default" => 0.045f,
            _ => 0.038f
        });
        float yBase = bh * 0.46f;
        float yCrest = bh * (hullKey is "hero_default" ? 0.58f : 0.54f);
        float zEnd = l * (hullKey switch
        {
            "scout_light" => 0.28f,
            "interceptor_mk2" => 0.26f,
            "fighter_basic" => 0.18f,
            "drone_swarm" => 0.18f,
            "hero_default" => 0.26f,
            _ => 0.16f
        });
        int keelSegs = hullKey is "drone_swarm" ? 3 : hullKey is "hero_default" ? 4 : hullKey is "scout_light" ? 4 : 3;
        for (int i = 0; i < keelSegs; i++)
        {
            float t = i / MathF.Max(1f, keelSegs - 1);
            float z = MathHelper.Lerp(-l * 0.02f, zEnd, t);
            w.TriMat(hull, -keelHalfW, yBase, z, keelHalfW, yBase, z, 0, yCrest, z + l * 0.012f);
            if (i % 2 == 0 || hullKey is "hero_default" or "interceptor_mk2" or "scout_light")
                w.TriMat(accent, -keelHalfW * 0.55f, yCrest * 0.94f, z + l * 0.005f,
                    keelHalfW * 0.55f, yCrest * 0.94f, z + l * 0.005f, 0, yCrest, z + l * 0.012f);
            if (hullKey is "interceptor_mk2")
                w.TriMat(membrane, -keelHalfW * 0.70f, yBase + bh * 0.03f, z,
                    keelHalfW * 0.70f, yBase + bh * 0.03f, z, 0, yCrest * 0.90f, z + l * 0.010f);
            if (hullKey is "hero_default" && i % 2 == 0)
                w.TriMat(membrane, -keelHalfW * 0.75f, yBase + bh * 0.04f, z,
                    keelHalfW * 0.75f, yBase + bh * 0.04f, z, 0, yCrest * 0.88f, z + l * 0.008f);
        }
    }

    private static void AddOrganicWingSweep(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial accent,
        int side, float hw, float bh, float l, float sweep, float span)
    {
        float xRoot = side * hw * 0.22f;
        float xTip = side * hw * span;
        float zRoot = l * 0.06f;
        float zTip = -l * sweep * 0.28f;
        w.TriMat(hull, xRoot, bh * 0.30f, zRoot, xTip, bh * 0.14f, zTip, xRoot, bh * 0.10f, zRoot - l * 0.04f);
        w.TriMat(accent, xTip - side * hw * 0.04f, bh * 0.18f, zTip + l * 0.02f,
            xTip, bh * 0.12f, zTip, xTip - side * hw * 0.02f, bh * 0.08f, zTip + l * 0.04f);
    }

    private static void AddLoftedHullMat(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial mat, float len, float wid, float hgt, int sections,
        Func<float, float> radiusFn, Func<float, float> heightFn, float zStart, float zEnd)
    {
        for (int i = 0; i < sections; i++)
        {
            float t0 = i / (float)sections;
            float t1 = (i + 1) / (float)sections;
            float z0 = MathHelper.Lerp(len * zStart, len * zEnd, t0);
            float z1 = MathHelper.Lerp(len * zStart, len * zEnd, t1);
            float r0 = radiusFn(t0);
            float r1 = radiusFn(t1);
            float h0 = heightFn(t0);
            float h1 = heightFn(t1);
            float dz = z1 - z0;

            w.TriMat(mat, -r0, h0 * 0.32f, z0, r0, h0 * 0.32f, z0, 0, h0, z0 + dz * 0.45f);
            w.TriMat(mat, -r0, h0 * 0.32f, z0, 0, h0, z0 + dz * 0.45f, -r1, h1 * 0.32f, z1);
            w.TriMat(mat, r0, h0 * 0.32f, z0, r1, h1 * 0.32f, z1, 0, h0, z0 + dz * 0.45f);
            w.TriMat(mat, -r0, 0, z0, -r1, 0, z1, r0, 0, z0);
            w.TriMat(mat, r0, 0, z0, -r1, 0, z1, r1, 0, z1);
        }
    }

    /// <summary>Industrial/logistics organic hulls — flowing membrane base with per-role pod blooms.</summary>
    private static void BuildOrganicUtilitySolid(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float widthScale = hullKey is "freighter_bulk" ? 0.92f : hullKey is "miner_eva" ? 0.94f : 0.98f;
        float l = len * (hullKey switch
        {
            "freighter_bulk" => 1.02f,
            "transport_cargo" => 1.02f,
            "miner_basic" => 0.97f,
            "miner_eva" => 0.93f,
            "miner_tractor" => 0.96f,
            "support_repair" => 0.97f,
            _ => 0.92f
        });
        float bw = wid * widthScale * (hullKey is "transport_cargo" ? 1.04f : 1.08f);
        float bh = hgt * (hullKey is "freighter_bulk" ? 1.42f : 1.32f);
        // Loop-12 tri trim: cargo utility hulls ≤180 sweet max (geometry penalty recovery).
        int sections = hullKey is "freighter_bulk" or "transport_cargo" ? 7 : HullSections(hullKey, 10, 7, 12);
        int pods = hullKey is "freighter_bulk" or "transport_cargo" ? 2 : 3;
        float bowZ = hullKey switch
        {
            "freighter_bulk" => 0.88f,
            "transport_cargo" => 0.88f,
            "miner_basic" => 0.83f,
            "miner_eva" => 0.78f,
            "miner_tractor" => 0.80f,
            "support_repair" => 0.81f,
            _ => 0.80f
        };

        AddLoftedHull(w, l, bw, bh, sections,
            t =>
            {
                float belly = 0.46f + 0.54f * MathF.Sin(t * MathF.PI);
                float nose = t > 0.82f ? (1f - t) / 0.18f : 1f;
                float bulge = hullKey is "freighter_bulk" ? 0.10f * MathF.Sin(t * MathF.PI * 1.6f) : 0f;
                for (int p = 0; p < pods; p++)
                {
                    float pt = 0.20f + p * 0.16f;
                    bulge += MathF.Max(0f, 1f - MathF.Abs(t - pt) * 4.8f) * (hullKey is "freighter_bulk" ? 0.12f : 0.14f);
                }
                return bw * belly * nose * (0.36f + bulge);
            },
            t => bh * (0.40f + 0.60f * MathF.Sin(t * MathF.PI)),
            -0.50f, bowZ);

        int lateralPods = hullKey switch
        {
            "transport_cargo" => 1,
            "freighter_bulk" => 1,
            "support_repair" => 2,
            _ => 3
        };
        float podReach = hullKey is "transport_cargo" ? 0.24f : 0.28f;
        float podRadius = hullKey is "transport_cargo" ? 0.11f : 0.14f;
        int bulbSegs = hullKey is "freighter_bulk" or "transport_cargo" ? 3 : 5;
        for (int p = 0; p < lateralPods; p++)
        {
            float side = (p - (lateralPods - 1) * 0.5f) * bw * podReach;
            float z = MathHelper.Lerp(-l * 0.06f, l * (hullKey is "transport_cargo" ? 0.28f : 0.26f), p / (float)Math.Max(1, lateralPods - 1));
            AddIntegratedBulb(w, side, bh * 0.36f, z, bw * podRadius, bh * 0.20f, bulbSegs);
        }

        AddLoftedHull(w, l * 0.32f, bw * 0.38f, bh * 0.88f, hullKey is "freighter_bulk" or "transport_cargo" ? 2 : 4,
            t => bw * 0.07f * (1f - t * 0.45f),
            t => bh * (0.52f + t * 0.38f),
            0.46f, bowZ * 0.92f);

        int rings = hullKey switch
        {
            "freighter_bulk" or "transport_cargo" => 2,
            "support_repair" => 3,
            _ => 4
        };
        int ringSegs = hullKey is "freighter_bulk" or "transport_cargo" ? 4 : 5;
        for (int i = 0; i < rings; i++)
        {
            float t = (i + 0.5f) / rings;
            float z = MathHelper.Lerp(-l * 0.32f, l * (bowZ * 0.50f), t);
            float r = bw * (0.20f + 0.07f * MathF.Sin(t * MathF.PI * 2f));
            AddHullRing(w, r, bh * 0.52f, z, ringSegs);
        }

        float hw = wid * 0.5f * widthScale;
        switch (hullKey)
        {
            case "miner_basic": AddOrganicMiningToolPods(w, hw, hgt, len); break;
            case "miner_eva": AddOrganicEvaPodBloom(w, hw, hgt, len); break;
            case "miner_tractor": AddOrganicTractorBloom(w, hw, hgt, len); break;
            case "transport_cargo": AddOrganicCargoSpineSac(w, hw, hgt, len); break;
            case "freighter_bulk": AddOrganicBulkMembrane(w, hw, hgt, len); break;
            case "support_repair": AddOrganicRepairAntennaBlooms(w, hw, hgt, len); break;
        }

        AddOrganicUtilityDorsalKeel(w, hullKey, hw, hgt, len);
    }

    /// <summary>Narrow dorsal cargo/tool spine keel — modest +Z elongation without beam widen.</summary>
    private static void AddOrganicUtilityDorsalKeel(RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float keelHalfW = hw * 0.034f;
        float yBase = hgt * 0.46f;
        float yCrest = hgt * (hullKey is "freighter_bulk" or "transport_cargo" ? 0.60f : 0.56f);
        float zEnd = len * (hullKey switch
        {
            "freighter_bulk" => 0.48f,
            "transport_cargo" => 0.48f,
            "miner_basic" => 0.33f,
            "miner_eva" => 0.24f,
            "miner_tractor" => 0.28f,
            "support_repair" => 0.28f,
            _ => 0.20f
        });
        int keelSegs = hullKey is "freighter_bulk" or "transport_cargo" ? 1
            : hullKey is "support_repair" ? 2 : 3;
        for (int i = 0; i < keelSegs; i++)
        {
            float t = i / MathF.Max(1f, keelSegs - 1);
            float z = MathHelper.Lerp(-len * 0.02f, zEnd, t);
            w.TriMat(hull, -keelHalfW, yBase, z, keelHalfW, yBase, z, 0, yCrest, z + len * 0.012f);
            if (keelSegs > 1 || hullKey is not "freighter_bulk" and not "transport_cargo")
                w.TriMat(accent, -keelHalfW * 0.55f, yCrest * 0.96f, z + len * 0.006f,
                    keelHalfW * 0.55f, yCrest * 0.96f, z + len * 0.006f, 0, yCrest, z + len * 0.012f);
        }
    }

    private static void AddOrganicMiningToolPods(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        w.TriMat(hull, -hw * 0.06f, hgt * 0.28f, len * 0.52f, hw * 0.06f, hgt * 0.28f, len * 0.52f, 0, hgt * 0.38f, len * 0.58f);
        w.TriMat(accent, -hw * 0.04f, hgt * 0.34f, len * 0.56f, hw * 0.04f, hgt * 0.34f, len * 0.56f, 0, hgt * 0.40f, len * 0.60f);
        for (int side = -1; side <= 1; side += 2)
        {
            float xRoot = side * hw * 0.34f;
            float xTip = side * hw * 0.72f;
            float zRoot = len * 0.10f;
            float zTip = len * 0.57f;
            w.Tri(xRoot, hgt * 0.16f, zRoot, xTip, hgt * 0.22f, zTip, xRoot, hgt * 0.10f, zTip - len * 0.02f);
            w.Tri(xTip, hgt * 0.22f, zTip, xTip + side * hw * 0.05f, hgt * 0.28f, zTip + len * 0.02f, xTip, hgt * 0.12f, zTip + len * 0.03f);
            AddIntegratedBulb(w, xTip, hgt * 0.24f, zTip, hw * 0.12f, hgt * 0.16f, 5);
            AddIntegratedBulb(w, xTip + side * hw * 0.04f, hgt * 0.18f, zTip + len * 0.04f, hw * 0.07f, hgt * 0.09f, 4);
        }
    }

    private static void AddOrganicEvaPodBloom(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        float podR = hw * 0.32f;
        float podY = hgt * 0.50f;
        float podZ = len * 0.08f;
        AddIntegratedBulb(w, 0, podY + hgt * 0.10f, podZ, podR, hgt * 0.20f, 8);
        AddIntegratedBulb(w, hw * 0.46f, hgt * 0.38f, len * 0.12f, hw * 0.10f, hgt * 0.12f, 6);
        w.TriMat(hull, -hw * 0.05f, hgt * 0.30f, len * 0.46f, hw * 0.05f, hgt * 0.30f, len * 0.46f, 0, hgt * 0.40f, len * 0.52f);
    }

    private static void AddOrganicTractorBloom(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        float dishZ = len * 0.40f;
        AddIntegratedBulb(w, 0, hgt * 0.68f, dishZ, hw * 0.24f, hgt * 0.14f, 8);
        w.TriMat(hull, -hw * 0.05f, hgt * 0.32f, len * 0.44f, hw * 0.05f, hgt * 0.32f, len * 0.44f, 0, hgt * 0.42f, len * 0.50f);
        for (int side = -1; side <= 1; side += 2)
        {
            float xTip = side * hw * 0.66f;
            w.Tri(side * hw * 0.14f, hgt * 0.24f, len * 0.04f, xTip, hgt * 0.30f, len * 0.16f, xTip, hgt * 0.20f, len * 0.20f);
            AddIntegratedBulb(w, xTip, hgt * 0.32f, len * 0.14f, hw * 0.09f, hgt * 0.10f, 6);
        }
    }

    private static void AddOrganicCargoSpineSac(RaceMeshWriter w, float hw, float hgt, float len)
    {
        for (int s = 0; s < 2; s++)
        {
            float t = s;
            float z = MathHelper.Lerp(-len * 0.02f, len * 0.47f, t);
            AddIntegratedBulb(w, 0, hgt * 0.50f + t * hgt * 0.08f, z, hw * MathHelper.Lerp(0.14f, 0.06f, t), hgt * 0.13f, 4);
        }
        AddLoftedHull(w, len * 0.27f, hw * 0.10f, hgt * 0.55f, 2,
            _ => hw * 0.05f,
            t => hgt * (0.48f + t * 0.14f),
            0.02f, 0.44f);
    }

    private static void AddOrganicBulkMembrane(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        AddIntegratedBulb(w, 0, hgt * 0.16f, len * 0.08f, hw * 0.26f, hgt * 0.11f, 4);
        w.TriMat(hull, -hw * 0.06f, hgt * 0.38f, len * 0.36f, hw * 0.06f, hgt * 0.38f, len * 0.36f, 0, hgt * 0.46f, len * 0.46f);
        for (int side = -1; side <= 1; side += 2)
        {
            float cx = side * hw * 0.36f;
            AddIntegratedBulb(w, cx, hgt * 0.28f, len * 0.34f, hw * 0.12f, hgt * 0.12f, 4);
        }
    }

    private static void AddOrganicRepairAntennaBlooms(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        for (int a = 0; a < 2; a++)
        {
            float z = len * (0.06f + a * 0.060f);
            AddIntegratedBulb(w, 0, hgt * (0.74f + a * 0.06f), z, hw * 0.06f, hgt * 0.10f, 5);
        }
        w.TriMat(hull, -hw * 0.02f, hgt * 0.88f, len * 0.14f, hw * 0.02f, hgt * 0.88f, len * 0.14f, 0, hgt * 0.98f, len * 0.18f);
        w.TriMat(accent, -hw * 0.015f, hgt * 0.94f, len * 0.16f, hw * 0.015f, hgt * 0.94f, len * 0.16f, 0, hgt * 1.02f, len * 0.20f);
        for (int side = -1; side <= 1; side += 2)
        {
            float xBoom = side * hw * 0.66f;
            w.Tri(side * hw * 0.10f, hgt * 0.44f, len * 0.04f, xBoom, hgt * 0.54f, len * 0.14f, xBoom, hgt * 0.40f, len * 0.18f);
        }
    }

    // â”€â”€ Bulbous / organic fallback â€” smooth bio-loft with integrated pod bulges â”€

    private static void BuildBulbous(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float l = len * 0.92f;
        float bw = wid * 1.05f;
        float bh = hgt * 1.35f;
        int sections = HullSections(hullKey, 14, 8, 18);
        int pods = hullKey is "miner" or "support" or "transport" ? 4 : 3;

        AddLoftedHull(w, l, bw, bh, sections,
            t =>
            {
                float belly = 0.48f + 0.52f * MathF.Sin(t * MathF.PI);
                float nose = t > 0.8f ? (1f - t) / 0.2f : 1f;
                float bulge = 0f;
                for (int p = 0; p < pods; p++)
                {
                    float pt = 0.22f + p * 0.18f;
                    bulge += MathF.Max(0f, 1f - MathF.Abs(t - pt) * 5f) * 0.14f;
                }
                return bw * belly * nose * (0.38f + bulge);
            },
            t => bh * (0.42f + 0.58f * MathF.Sin(t * MathF.PI)),
            -0.52f, 0.82f);

        for (int p = 0; p < pods; p++)
        {
            float side = (p - (pods - 1) * 0.5f) * bw * 0.32f;
            float z = MathHelper.Lerp(-l * 0.08f, l * 0.28f, p / (float)Math.Max(1, pods - 1));
            AddIntegratedBulb(w, side, bh * 0.38f, z, bw * 0.16f, bh * 0.22f, 8);
        }

        AddLoftedHull(w, l * 0.35f, bw * 0.45f, bh * 0.9f, 6,
            t => bw * 0.08f * (1f - t * 0.5f),
            t => bh * (0.55f + t * 0.35f),
            0.48f, 0.78f);

        if (hullKey is not "drone")
        {
            int rings = HullSections(hullKey, 8, 5, 10);
            for (int i = 0; i < rings; i++)
            {
                float t = (i + 0.5f) / rings;
                float z = MathHelper.Lerp(-l * 0.35f, l * 0.45f, t);
                float r = bw * (0.22f + 0.08f * MathF.Sin(t * MathF.PI * 2f));
                AddHullRing(w, r, bh * 0.55f, z, 8);
            }
        }
    }

    // â”€â”€ Asymmetric hive (Nexar) â€” lofted chitin hulls with embossed ring bands â”€â”€â”€â”€

    private static void BuildNexarHull(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        switch (hullKey)
        {
            case "scout":
            case "scout_light":
                BuildNexarScout(w, len, wid, hgt);
                return;
            case "fighter":
            case "fighter_basic":
                BuildNexarFighter(w, len, wid, hgt);
                return;
            case "interceptor":
            case "interceptor_mk2":
                BuildNexarInterceptor(w, len, wid, hgt);
                return;
            case "drone":
            case "drone_swarm":
                BuildNexarDrone(w, len, wid, hgt);
                return;
            case "hero":
            case "hero_default":
                BuildNexarHero(w, len, wid, hgt);
                return;
            case "miner_basic":
            case "miner_eva":
            case "miner_tractor":
            case "transport_cargo":
            case "freighter_bulk":
            case "support_repair":
                BuildAsymmetricUtilitySolid(w, hullKey, len, wid, hgt);
                return;
            case "cruiser":
            case "cruiser_heavy":
                BuildNexarCruiserHeavyAsymmetric(w, len, wid, hgt);
                return;
            case "carrier":
            case "carrier_command":
                BuildNexarCarrierCommandAsymmetric(w, len, wid, hgt);
                return;
            case "dreadnought":
                BuildNexarDreadnoughtAsymmetric(w, len, wid, hgt);
                return;
            case "corvette":
            case "corvette_fast":
                BuildNexarCorvetteFast(w, len, wid, hgt);
                return;
            case "frigate":
            case "frigate_strike":
                BuildNexarFrigateStrike(w, len, wid, hgt);
                return;
            case "bomber":
            case "bomber_heavy":
                BuildNexarBomberHeavy(w, len, wid, hgt);
                return;
            case "gunship":
            case "gunship_heavy":
                BuildNexarGunshipHeavy(w, len, wid, hgt);
                return;
            case "destroyer":
            case "destroyer_assault":
                BuildNexarDestroyerAssault(w, len, wid, hgt);
                return;
            default:
                BuildLatticeFrame(w, hullKey, len, wid, hgt);
                break;
        }
    }

    /// <summary>Heavy cruiser — balanced chitin bio-mass, offset dorsal bloom ridge, lateral hive bulges.</summary>
    private static void BuildNexarCruiserHeavyAsymmetric(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var fold = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float l = len * 1.04f;
        float bw = wid * 1.10f;
        float bh = hgt * 1.30f;
        float asym = NexarAsym * 0.38f;

        // Loop-07 tri trim: capital rings/bulbs under 180 sweet max.
        AddLoftedHull(w, l, bw, bh, 6,
            t =>
            {
                float belly = 0.48f + 0.52f * MathF.Sin(t * MathF.PI);
                float ridge = MathF.Max(0f, 1f - MathF.Abs(t - 0.44f) * 3.6f) * 0.12f;
                float nose = t > 0.82f ? (1f - t) / 0.18f : 1f;
                return bw * belly * nose * (0.40f + ridge);
            },
            t => bh * (0.44f + 0.56f * MathF.Sin(t * MathF.PI)),
            -0.54f, 0.78f,
            t => bw * asym * MathF.Sin(t * MathF.PI * 1.3f));

        AddLoftedHull(w, l * 0.58f, bw * 0.22f, bh * 0.88f, 2,
            t => bw * 0.07f * (1f - t * 0.35f),
            t => bh * (0.60f + t * 0.38f),
            0.12f, 0.66f,
            t => bw * asym * 0.42f);

        for (int p = 0; p < 2; p++)
        {
            float side = (p == 0 ? -1f : 1f) * bw * 0.36f;
            float z = MathHelper.Lerp(-l * 0.04f + asym * 0.08f, l * 0.22f, p);
            AddIntegratedBulb(w, side, bh * 0.38f, z, bw * 0.15f, bh * 0.21f, 3);
        }

        AddBoxMat(w, fold, -bw * 0.14f, bw * 0.14f, bh * 0.34f, bh * 0.48f, -l * 0.04f, l * 0.20f);
        AddBoxMat(w, weapon, -bw * 0.10f, bw * 0.10f, bh * 0.10f, bh * 0.18f, l * 0.54f, l * 0.64f);
        w.TriMat(hull, -bw * 0.08f, bh * 0.28f, l * 0.62f, bw * 0.08f, bh * 0.28f, l * 0.62f, 0, bh * 0.44f, l * 0.68f);

        AddNexarCapitalEngineWells(w, len, wid, hgt, 3);
        AddNexarCapitalAmberRings(w, l, bw, bh, 3, asym);
        for (int v = 0; v < 3; v++)
        {
            float z = l * (-0.04f + v * 0.07f);
            w.TriMat(accent, -bw * 0.06f, bh * 0.54f, z, bw * 0.06f, bh * 0.54f, z, 0, bh * 0.62f, z + l * 0.006f);
        }
    }

    /// <summary>Command carrier — flat deck/hangar chitin, lateral mass bias, hive hangar cues.</summary>
    private static void BuildNexarCarrierCommandAsymmetric(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var membrane = RaceMeshWriter.HullMaterial.Radiator;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float l = len * 1.06f;
        float bw = wid * 1.18f;
        float bh = hgt * 1.14f;
        float asym = NexarAsym * 0.44f;

        AddLoftedHull(w, l, bw, bh, 14,
            t =>
            {
                float belly = 0.52f + 0.38f * MathF.Sin(t * MathF.PI);
                float nose = t > 0.80f ? (1f - t) / 0.20f : 1f;
                return bw * belly * nose * 0.42f;
            },
            t => bh * (0.38f + 0.48f * MathF.Sin(t * MathF.PI)),
            -0.52f, 0.76f,
            t => bw * asym * MathF.Sin(t * MathF.PI * 1.1f));

        for (int p = 0; p < 3; p++)
        {
            float side = (p - 1) * bw * 0.38f + asym * 0.12f;
            float z = MathHelper.Lerp(-l * 0.08f, l * 0.20f, p / 2f);
            AddIntegratedBulb(w, side, bh * 0.34f, z, bw * 0.16f, bh * 0.18f, 6);
        }

        float deckY = bh * 0.60f;
        float deckZ0 = -l * 0.32f;
        float deckZ1 = l * 0.28f;
        float deckSkew = asym * 0.18f;
        w.TriMat(membrane, -bw * 0.42f + deckSkew, deckY, deckZ1, bw * 0.42f + deckSkew, deckY, deckZ1, deckSkew, deckY * 1.06f, deckZ0 + l * 0.06f);
        w.TriMat(membrane, -bw * 0.42f + deckSkew, deckY, deckZ1, deckSkew, deckY * 1.06f, deckZ0 + l * 0.06f, -bw * 0.42f + deckSkew, deckY, deckZ0);
        w.TriMat(membrane, bw * 0.42f + deckSkew, deckY, deckZ1, bw * 0.42f + deckSkew, deckY, deckZ0, deckSkew, deckY * 1.06f, deckZ0 + l * 0.06f);
        for (int h = 0; h < 2; h++)
        {
            float z = MathHelper.Lerp(-l * 0.16f, l * 0.10f, h);
            w.TriMat(membrane, -bw * 0.34f + deckSkew, deckY * 0.94f, z, bw * 0.34f + deckSkew, deckY * 0.94f, z, deckSkew, deckY, z + l * 0.010f);
        }

        float keelY = bh * 0.66f;
        for (int k = 0; k < 3; k++)
        {
            float z = MathHelper.Lerp(l * 0.04f, l * 0.22f, k / 2f);
            w.TriMat(membrane, -bw * 0.08f + asym * 0.06f, keelY, z, bw * 0.08f + asym * 0.06f, keelY, z, asym * 0.06f, keelY + bh * 0.06f, z + l * 0.008f);
        }

        AddBoxMat(w, weapon, -bw * 0.26f, bw * 0.26f, bh * 0.12f, bh * 0.20f, -l * 0.04f, l * 0.04f);
        w.TriMat(hull, -bw * 0.06f, bh * 0.24f, l * 0.10f, bw * 0.06f, bh * 0.24f, l * 0.10f, asym * 0.08f, bh * 0.30f, l * 0.14f);
        AddNexarCapitalEngineWells(w, len, wid, hgt, 3);
        AddNexarCapitalAmberRings(w, l, bw, bh, 5, asym);
        for (int v = 0; v < 3; v++)
        {
            float z = l * (0.04f + v * 0.06f);
            w.TriMat(accent, -bw * 0.05f, bh * 0.48f, z, bw * 0.05f, bh * 0.48f, z, asym * 0.06f, bh * 0.54f, z + l * 0.006f);
        }
    }

    /// <summary>Dreadnought — imposing prow pod cluster, embossed ring stacks on extended chitin mass.</summary>
    private static void BuildNexarDreadnoughtAsymmetric(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var fold = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float l = len * 1.09f;
        float bw = wid * 1.14f;
        float bh = hgt * 1.18f;
        float asym = NexarAsym * 0.40f;

        AddLoftedHull(w, l, bw, bh, 14,
            t =>
            {
                float belly = 0.46f + 0.54f * MathF.Sin(t * MathF.PI);
                float prow = t > 0.70f ? 1f + (t - 0.70f) * 0.18f : 1f;
                float nose = t > 0.84f ? (1f - t) / 0.16f : 1f;
                return bw * belly * nose * prow * 0.42f;
            },
            t => bh * (0.42f + 0.50f * MathF.Sin(t * MathF.PI)),
            -0.56f, 0.82f,
            t => bw * asym * MathF.Sin(t * MathF.PI * 1.2f));

        float prowZ = l * 0.66f;
        for (int p = 0; p < 3; p++)
        {
            float side = (p - 1) * bw * 0.22f + asym * 0.10f;
            AddIntegratedBulb(w, side, bh * 0.40f, prowZ, bw * 0.14f, bh * 0.16f, 6);
            if (p == 1)
                w.TriMat(weapon, side, bh * 0.44f, prowZ + l * 0.04f, side - bw * 0.06f, bh * 0.38f, prowZ, side + bw * 0.06f, bh * 0.38f, prowZ);
        }

        for (int p = 0; p < 3; p++)
        {
            float side = (p - 1) * bw * 0.38f + asym * 0.14f;
            float z = MathHelper.Lerp(-l * 0.10f, l * 0.24f, p / 2f);
            AddIntegratedBulb(w, side, bh * 0.36f, z, bw * 0.15f, bh * 0.18f, 6);
        }

        AddBoxMat(w, fold, -bw * 0.12f, bw * 0.12f, bh * 0.28f, bh * 0.40f, l * 0.48f, l * 0.62f);
        AddBoxMat(w, weapon, -bw * 0.12f, bw * 0.12f, bh * 0.08f, bh * 0.16f, l * 0.62f, l * 0.72f);
        w.TriMat(hull, -bw * 0.08f, bh * 0.24f, l * 0.70f, bw * 0.08f, bh * 0.24f, l * 0.70f, asym * 0.08f, bh * 0.38f, l * 0.76f);

        AddNexarCapitalEngineWells(w, len, wid, hgt, 3);
        AddNexarCapitalAmberRings(w, l, bw, bh, 6, asym);
        for (int v = 0; v < 3; v++)
        {
            float z = l * (0.10f + v * 0.08f);
            w.TriMat(accent, -bw * 0.06f, bh * 0.48f, z, bw * 0.06f, bh * 0.48f, z, asym * 0.06f, bh * 0.54f, z + l * 0.006f);
        }
    }

    private static void AddNexarCapitalEngineWells(RaceMeshWriter w, float len, float wid, float hgt, int count)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        float hw = wid * 0.5f;
        count = Math.Clamp(count, 2, 3);
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : i / (float)(count - 1);
            float cx = MathHelper.Lerp(-hw * 0.28f, hw * 0.28f, t);
            float halfW = hw * 0.10f;
            AddBoxMat(w, hull, cx - halfW, cx + halfW, 0, hgt * 0.18f, -len * 0.28f, -len * 0.14f);
            AddBoxMat(w, engine, cx - halfW * 0.50f, cx + halfW * 0.50f, hgt * 0.02f, hgt * 0.14f, -len * 0.30f, -len * 0.18f);
        }
    }

    private static void AddNexarCapitalAmberRings(RaceMeshWriter w, float l, float bw, float bh, int ringCount, float asym)
    {
        var accent = RaceMeshWriter.HullMaterial.Solar;
        for (int i = 0; i < ringCount; i++)
        {
            float t = (i + 0.5f) / ringCount;
            float z = MathHelper.Lerp(-l * 0.36f, l * 0.40f, t);
            float skew = bw * asym * 0.35f * MathF.Sin(t * MathF.PI * 1.3f);
            float r = bw * (0.20f + 0.07f * MathF.Sin(t * MathF.PI * 2.2f));
            for (int s = 0; s < 6; s++)
            {
                float a0 = MathF.PI * 2f * s / 6f;
                float a1 = MathF.PI * 2f * (s + 1) / 6f;
                float x0 = MathF.Cos(a0) * r + skew;
                float x1 = MathF.Cos(a1) * r + skew;
                float zz0 = z + MathF.Sin(a0) * r * 0.12f;
                float zz1 = z + MathF.Sin(a1) * r * 0.12f;
                w.TriMat(accent, x0, bh * 0.50f, zz0, x1, bh * 0.50f, zz1, skew, bh * 0.56f, z);
            }
        }
    }

    private const float NexarAsym = 0.65f;
    private const float NexarWingSweep = 0.42f;

    private static void AddNexarChitinCore(
        RaceMeshWriter w, float l, float hw, float hh, int sections, float widthScale, float skewScale)
    {
        float asym = NexarAsym * skewScale;
        AddLoftedHull(w, l, hw * 2f * widthScale, hh, sections,
            t => hw * widthScale * (0.38f + 0.28f * MathF.Sin(t * MathF.PI) + (t > 0.75f ? (1f - t) / 0.25f * 0.1f : 0f)),
            t => hh * (0.45f + 0.55f * MathF.Sin(t * MathF.PI)),
            -0.55f, 0.75f,
            t => hw * asym * MathF.Sin(t * MathF.PI * 1.3f));
    }

    private static void AddNexarEmbossedRings(RaceMeshWriter w, float l, float hw, float hh, int rings, float skewScale)
    {
        float asym = NexarAsym * skewScale;
        for (int r = 0; r < rings; r++)
        {
            float t = r / (float)Math.Max(1, rings - 1);
            float z = MathHelper.Lerp(-l * 0.5f, l * 0.55f, t);
            float skew = hw * asym * MathF.Sin(t * MathF.PI * 1.3f);
            AddEmbossedRing(w, hw * 0.88f, hh * 0.82f, z, skew, hh * 0.06f, 6);
        }
    }

    private static void AddNexarBowTip(RaceMeshWriter w, RaceMeshWriter.HullMaterial mat, float hw, float hh, float bowZ, float extension)
    {
        var accent = RaceMeshWriter.HullMaterial.Solar;
        w.TriMat(mat, -hw * 0.08f, hh * 0.42f, bowZ, hw * 0.08f, hh * 0.42f, bowZ, 0, hh * 0.54f, bowZ + extension);
        w.TriMat(accent, -hw * 0.04f, hh * 0.46f, bowZ + extension * 0.4f, hw * 0.04f, hh * 0.46f, bowZ + extension * 0.4f,
            0, hh * 0.52f, bowZ + extension);
    }

    private static void AddNexarChitinWing(
        RaceMeshWriter w, int side, float hw, float hh, float l, float span, float sweep, float zAnchor)
    {
        var chitin = RaceMeshWriter.HullMaterial.Truss;
        float xRoot = side * hw * 0.22f;
        float xTip = side * hw * span;
        float zTip = zAnchor - sweep * hw * 0.55f;
        w.TriMat(chitin, xRoot, hh * 0.28f, zAnchor, xTip, hh * 0.14f, zTip, xRoot, hh * 0.08f, zAnchor - l * 0.04f);
        w.TriMat(chitin, xRoot, hh * 0.08f, zAnchor - l * 0.04f, xTip, hh * 0.14f, zTip, xTip * 0.82f, hh * 0.04f, zTip - l * 0.02f);
    }

    /// <summary>Slim offset pod spine scout profile.</summary>
    private static void BuildNexarScout(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float l = len * 0.90f;
        float hw = wid * 0.38f;
        float hh = hgt * 0.72f;
        float bowZ = l * 0.52f;
        float bowExt = l * 0.05f;

        AddNexarChitinCore(w, l, hw, hh, 5, 0.42f, 0.85f);
        AddNexarEmbossedRings(w, l, hw, hh, 2, 0.85f);
        AddLoftedHull(w, l * 0.22f, hw * 0.55f, hh * 0.75f, 3,
            t => hw * 0.14f * (1f - t * 0.25f),
            t => hh * (0.48f + t * 0.38f),
            0.08f, 0.22f,
            t => hw * NexarAsym * 0.55f);
        AddQuadPanel(w, hw * 0.28f, hh * 0.62f, l * 0.18f, hw * 0.42f, hh * 0.38f, -l * 0.08f);
        AddNexarBowTip(w, hull, hw, hh, bowZ, bowExt);
        AddIntegratedBulb(w, hw * 0.32f, hh * 0.44f, l * 0.08f, hw * 0.14f, hh * 0.18f, 5);
        for (int e = 0; e < 3; e++)
        {
            float x = hw * 0.18f + (e - 1) * hw * 0.12f;
            AddBoxMat(w, engine, x - hw * 0.05f, x + hw * 0.05f, 0, hh * 0.14f, -l * 0.34f, -l * 0.24f);
        }
        AddBoxMat(w, weapon, hw * 0.36f - hw * 0.05f, hw * 0.36f + hw * 0.05f, hh * 0.08f, hh * 0.16f, l * 0.02f, l * 0.10f);
        AddBoxMat(w, shield, -hw * 0.06f, hw * 0.06f, hh * 0.62f, hh * 0.74f, l * 0.04f, l * 0.12f);
        w.TriMat(accent, hw * 0.20f, hh * 0.50f, l * 0.06f, hw * 0.34f, hh * 0.42f, l * 0.02f, hw * 0.26f, hh * 0.58f, l * 0.10f);
    }

    /// <summary>Balanced asymmetric wing-chitin fighter profile.</summary>
    private static void BuildNexarFighter(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var chitin = RaceMeshWriter.HullMaterial.Truss;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float l = len * 0.92f;
        float hw = wid * 0.50f;
        float hh = hgt * 0.78f;
        float bowZ = l * 0.50f;
        float bowExt = l * 0.06f;

        AddNexarChitinCore(w, l, hw, hh, 5, 0.58f, 1.0f);
        AddNexarEmbossedRings(w, l, hw, hh, 2, 1.0f);
        AddNexarChitinWing(w, -1, hw, hh, l, 0.62f, NexarWingSweep, l * 0.06f);
        AddNexarChitinWing(w, 1, hw, hh, l, 0.48f, NexarWingSweep * 0.85f, l * 0.04f);
        AddQuadPanel(w, hw * 0.30f, hh * 0.68f, l * 0.22f, -hw * 0.12f, hh * 0.36f, -l * 0.18f);
        AddQuadPanel(w, -hw * 0.38f, hh * 0.52f, l * 0.28f, hw * 0.18f, -hh * 0.02f, -l * 0.24f);
        AddNexarBowTip(w, hull, hw, hh, bowZ, bowExt);
        for (int e = 0; e < 3; e++)
        {
            float x = (e - 1) * hw * 0.16f + hw * 0.08f;
            AddBoxMat(w, engine, x - hw * 0.06f, x + hw * 0.06f, 0, hh * 0.16f, -l * 0.36f, -l * 0.26f);
        }
        for (int side = -1; side <= 1; side += 2)
            AddBoxMat(w, weapon, side * hw * 0.40f - hw * 0.06f, side * hw * 0.40f + hw * 0.06f,
                hh * 0.08f, hh * 0.16f, l * 0.02f, l * 0.10f);
        AddBoxMat(w, shield, -hw * 0.08f, hw * 0.08f, hh * 0.66f, hh * 0.78f, l * 0.06f, l * 0.14f);
        w.TriMat(chitin, -hw * 0.06f, hh * 0.44f, l * 0.04f, hw * 0.06f, hh * 0.44f, l * 0.04f, 0, hh * 0.54f, l * 0.12f);
    }

    /// <summary>Aggressive swept offset-fin interceptor profile.</summary>
    private static void BuildNexarInterceptor(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float l = len * 0.90f;
        float hw = wid * 0.46f;
        float hh = hgt * 0.76f;
        float bowZ = l * 0.54f;
        float bowExt = l * 0.06f;

        AddNexarChitinCore(w, l, hw, hh, 5, 0.52f, 0.95f);
        AddNexarEmbossedRings(w, l, hw, hh, 2, 0.95f);
        AddNexarChitinWing(w, 1, hw, hh, l, 0.78f, NexarWingSweep * 1.15f, l * 0.12f);
        AddNexarChitinWing(w, -1, hw, hh, l, 0.34f, NexarWingSweep * 0.55f, l * 0.02f);
        AddQuadPanel(w, hw * 0.42f, hh * 0.72f, l * 0.30f, hw * 0.22f, hh * 0.28f, -l * 0.14f);
        AddNexarBowTip(w, hull, hw, hh, bowZ, bowExt);
        AddBoxMat(w, weapon, -hw * 0.08f, hw * 0.08f, hh * 0.10f, hh * 0.20f, l * 0.28f, l * 0.38f);
        for (int side = -1; side <= 1; side += 2)
            AddBoxMat(w, weapon, side * hw * 0.38f - hw * 0.05f, side * hw * 0.38f + hw * 0.05f,
                hh * 0.08f, hh * 0.16f, l * 0.02f, l * 0.10f);
        for (int e = 0; e < 3; e++)
        {
            float x = (e - 1) * hw * 0.14f;
            AddBoxMat(w, engine, x - hw * 0.05f, x + hw * 0.05f, 0, hh * 0.14f, -l * 0.34f, -l * 0.24f);
        }
        AddBoxMat(w, shield, -hw * 0.06f, hw * 0.06f, hh * 0.64f, hh * 0.76f, l * 0.04f, l * 0.12f);
        w.TriMat(accent, hw * 0.30f, hh * 0.48f, l * 0.10f, hw * 0.48f, hh * 0.32f, l * 0.04f, hw * 0.38f, hh * 0.56f, l * 0.14f);
    }

    /// <summary>Compact cluster-pod drone profile.</summary>
    private static void BuildNexarDrone(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var chitin = RaceMeshWriter.HullMaterial.Truss;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float l = len * 0.88f;
        float hw = wid * 0.34f;
        float hh = hgt * 0.66f;
        float bowZ = l * 0.46f;
        float bowExt = l * 0.05f;

        AddNexarChitinCore(w, l, hw, hh, 5, 0.36f, 0.75f);
        AddNexarEmbossedRings(w, l, hw, hh, 2, 0.75f);
        float[] podX = [hw * 0.22f, -hw * 0.14f, hw * 0.08f];
        float[] podZ = [-l * 0.06f, l * 0.08f, l * 0.18f];
        for (int p = 0; p < podX.Length; p++)
            AddIntegratedBulb(w, podX[p], hh * (0.30f + p * 0.06f), podZ[p], hw * (0.12f - p * 0.02f), hh * 0.14f, 3);
        AddNexarBowTip(w, hull, hw, hh, bowZ, bowExt);
        AddBoxMat(w, engine, -hw * 0.10f, hw * 0.10f, 0, hh * 0.14f, -l * 0.30f, -l * 0.20f);
        for (int side = -1; side <= 1; side += 2)
            AddBoxMat(w, weapon, side * hw * 0.22f - hw * 0.05f, side * hw * 0.22f + hw * 0.05f,
                hh * 0.08f, hh * 0.14f, l * 0.12f, l * 0.20f);
        AddBoxMat(w, shield, -hw * 0.05f, hw * 0.05f, hh * 0.58f, hh * 0.68f, l * 0.04f, l * 0.12f);
        w.TriMat(chitin, -hw * 0.03f, hh * 0.36f, -l * 0.02f, hw * 0.03f, hh * 0.36f, -l * 0.02f, 0, hh * 0.44f, l * 0.06f);
    }

    /// <summary>Command crown bloom hero with lateral mass offset.</summary>
    private static void BuildNexarHero(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float l = len * 0.96f;
        float hw = wid * 0.54f;
        float hh = hgt * 0.82f;
        float bowZ = l * 0.50f;
        float bowExt = l * 0.06f;

        // Loop-07 tri trim: hero reference craft under 180 sweet max.
        AddNexarChitinCore(w, l, hw, hh, 4, 0.62f, 1.05f);
        AddNexarEmbossedRings(w, l, hw, hh, 2, 1.05f);
        AddNexarChitinWing(w, -1, hw, hh, l, 0.58f, NexarWingSweep, l * 0.08f);
        AddNexarChitinWing(w, 1, hw, hh, l, 0.44f, NexarWingSweep * 0.80f, l * 0.02f);
        AddLoftedHull(w, l * 0.24f, hw * 0.48f, hh * 0.90f, 3,
            t => hw * 0.16f * (1f - t * 0.2f),
            t => hh * (0.62f + t * 0.48f),
            0.20f, 0.38f,
            t => hw * NexarAsym * 0.42f);
        for (int p = 0; p < 2; p++)
        {
            float z = MathHelper.Lerp(-l * 0.16f, l * 0.20f, p);
            AddIntegratedBulb(w, hw * 0.24f, hh * (0.52f + p * 0.08f), z, hw * (0.16f - p * 0.02f), hh * 0.20f, 3);
        }
        AddNexarBowTip(w, hull, hw, hh, bowZ, bowExt);
        for (int e = 0; e < 3; e++)
        {
            float x = (e - 1) * hw * 0.18f + hw * 0.10f;
            AddBoxMat(w, engine, x - hw * 0.06f, x + hw * 0.06f, 0, hh * 0.16f, -l * 0.38f, -l * 0.26f);
        }
        for (int side = -1; side <= 1; side += 2)
            AddBoxMat(w, weapon, side * hw * 0.42f - hw * 0.06f, side * hw * 0.42f + hw * 0.06f,
                hh * 0.08f, hh * 0.16f, l * 0.04f, l * 0.12f);
        AddBoxMat(w, shield, -hw * 0.08f, hw * 0.08f, hh * 0.72f, hh * 0.86f, l * 0.10f, l * 0.20f);
        w.TriMat(accent, -hw * 0.20f, hh * 0.78f, l * 0.16f, hw * 0.08f, hh * 0.78f, l * 0.16f, hw * 0.04f, hh * 0.92f, l * 0.22f);
    }

    /// <summary>Fast corvette — swept offset wedge, modest +Z bow, lateral chitin pod offset.</summary>
    private static void BuildNexarCorvetteFast(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        float l = len * 1.02f;
        float hw = wid * 0.62f;
        float hh = hgt * 0.78f;

        AddNexarChitinCore(w, l, hw, hh, 5, 0.52f, 0.90f);
        AddNexarEmbossedRings(w, l, hw, hh, 2, 0.90f);
        AddQuadPanel(w, hw * 0.30f, hh * 0.68f, l * 0.28f, -hw * 0.12f, hh * 0.32f, -l * 0.18f);
        w.TriMat(hull, -hw * 0.08f, hh * 0.30f, l * 0.62f, hw * 0.08f, hh * 0.30f, l * 0.62f, 0, hh * 0.48f, l * 0.68f);
        AddBoxMat(w, weapon, -hw * 0.46f, -hw * 0.32f, hh * 0.04f, hh * 0.14f, l * 0.02f, l * 0.10f);
        AddBoxMat(w, engine, -hw * 0.10f, hw * 0.10f, 0, hh * 0.12f, -l * 0.44f, -l * 0.34f);
        w.TriMat(accent, -hw * 0.04f, hh * 0.50f, l * 0.14f, hw * 0.04f, hh * 0.50f, l * 0.14f, 0, hh * 0.56f, l * 0.18f);
    }

    /// <summary>Strike frigate — balanced gun-pod chitin blooms with offset quad panels.</summary>
    private static void BuildNexarFrigateStrike(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        // Loop-07 aspect: widen lateral reach for target ~1.28.
        float l = len * 1.00f;
        float hw = wid * 0.74f;
        float hh = hgt * 0.82f;
        float bowZ = l * 0.68f;
        float bowExt = l * 0.05f;

        AddNexarChitinCore(w, l, hw, hh, 5, 0.58f, 0.95f);
        AddNexarEmbossedRings(w, l, hw, hh, 2, 0.95f);
        for (int p = 0; p < 2; p++)
        {
            float side = (p == 0 ? -1f : 1f) * hw * 0.36f;
            float z = l * (0.04f + p * 0.10f);
            AddIntegratedBulb(w, side, hh * 0.36f, z, hw * 0.12f, hh * 0.16f, 3);
            AddBoxMat(w, weapon, side - hw * 0.07f, side + hw * 0.07f, hh * 0.06f, hh * 0.16f, z - l * 0.04f, z + l * 0.04f);
        }
        AddNexarBowTip(w, hull, hw, hh, bowZ, bowExt);
        w.TriMat(hull, -hw * 0.10f, hh * 0.28f, l * 0.64f, hw * 0.10f, hh * 0.28f, l * 0.64f, 0, hh * 0.42f, l * 0.70f);
        AddBoxMat(w, engine, -hw * 0.12f, hw * 0.12f, 0, hh * 0.14f, -l * 0.46f, -l * 0.36f);
        w.TriMat(accent, -hw * 0.05f, hh * 0.52f, l * 0.10f, hw * 0.05f, hh * 0.52f, l * 0.10f, 0, hh * 0.58f, l * 0.14f);
    }

    /// <summary>Heavy bomber — wide payload sac, lateral offset chitin blooms.</summary>
    private static void BuildNexarBomberHeavy(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        float l = len * 1.04f;
        float hw = wid * 0.72f;
        float hh = hgt * 0.76f;

        AddNexarChitinCore(w, l, hw, hh, 5, 0.64f, 1.0f);
        AddNexarEmbossedRings(w, l, hw, hh, 2, 1.0f);
        AddLoftedHull(w, l * 0.50f, hw * 0.38f, hh * 0.68f, 2,
            t => hw * 0.10f * (1f - t * 0.30f),
            t => hh * (0.48f + t * 0.34f),
            0.06f, 0.54f,
            t => hw * NexarAsym * 0.40f * MathF.Sin(t * MathF.PI));
        w.TriMat(weapon, -hw * 0.26f, 0, -l * 0.02f, hw * 0.26f, 0, -l * 0.02f, 0, hh * 0.10f, l * 0.06f);
        AddIntegratedBulb(w, hw * 0.40f, hh * 0.28f, l * 0.04f, hw * 0.11f, hh * 0.14f, 4);
        w.TriMat(hull, -hw * 0.10f, hh * 0.24f, l * 0.60f, hw * 0.10f, hh * 0.24f, l * 0.60f, 0, hh * 0.36f, l * 0.66f);
        AddBoxMat(w, engine, -hw * 0.14f, hw * 0.14f, 0, hh * 0.12f, -l * 0.48f, -l * 0.38f);
        w.TriMat(accent, -hw * 0.06f, hh * 0.44f, l * 0.08f, hw * 0.06f, hh * 0.44f, l * 0.08f, 0, hh * 0.50f, l * 0.12f);
    }

    /// <summary>Heavy gunship — flattened chin weapon pod mass, offset chitin frame.</summary>
    private static void BuildNexarGunshipHeavy(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        float l = len * 1.05f;
        float hw = wid * 0.68f;
        float hh = hgt * 0.74f;

        AddNexarChitinCore(w, l, hw, hh, 5, 0.56f, 0.92f);
        AddNexarEmbossedRings(w, l, hw, hh, 2, 0.92f);
        w.TriMat(weapon, -hw * 0.14f, 0, l * 0.20f, hw * 0.14f, 0, l * 0.20f, 0, hh * 0.08f, l * 0.38f);
        w.TriMat(weapon, -hw * 0.10f, 0, l * 0.28f, hw * 0.10f, 0, l * 0.28f, 0, hh * 0.08f, l * 0.44f);
        AddBoxMat(w, weapon, -hw * 0.42f, -hw * 0.28f, hh * 0.02f, hh * 0.10f, l * 0.04f, l * 0.12f);
        AddBoxMat(w, weapon, hw * 0.28f, hw * 0.42f, hh * 0.02f, hh * 0.10f, l * 0.04f, l * 0.12f);
        w.TriMat(hull, -hw * 0.10f, hh * 0.22f, l * 0.56f, hw * 0.10f, hh * 0.22f, l * 0.56f, 0, hh * 0.32f, l * 0.62f);
        AddBoxMat(w, engine, -hw * 0.12f, hw * 0.12f, 0, hh * 0.12f, -l * 0.44f, -l * 0.34f);
        w.TriMat(accent, -hw * 0.05f, hh * 0.44f, l * 0.06f, hw * 0.05f, hh * 0.44f, l * 0.06f, 0, hh * 0.50f, l * 0.10f);
    }

    /// <summary>Assault destroyer — dorsal spine ridge with embossed ring bands.</summary>
    private static void BuildNexarDestroyerAssault(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var chitin = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        float l = len * 1.08f;
        float hw = wid * 0.64f;
        float hh = hgt * 0.84f;

        AddNexarChitinCore(w, l, hw, hh, 5, 0.54f, 0.88f);
        AddNexarEmbossedRings(w, l, hw, hh, 2, 0.88f);
        AddLoftedHull(w, l * 0.52f, hw * 0.22f, hh * 0.78f, 2,
            t => hw * 0.06f * (1f - t * 0.26f),
            t => hh * (0.58f + t * 0.36f),
            0.16f, 0.72f,
            t => hw * NexarAsym * 0.32f);
        for (int p = 0; p < 2; p++)
        {
            float side = (p == 0 ? -1f : 1f) * hw * 0.26f;
            float z = MathHelper.Lerp(-l * 0.06f, l * 0.26f, p);
            AddIntegratedBulb(w, side, hh * 0.38f, z, hw * 0.10f, hh * 0.15f, 4);
        }
        AddBoxMat(w, weapon, -hw * 0.10f, hw * 0.10f, hh * 0.08f, hh * 0.18f, l * 0.46f, l * 0.58f);
        AddBoxMat(w, chitin, -hw * 0.10f, hw * 0.10f, hh * 0.62f, hh * 0.76f, l * 0.08f, l * 0.18f);
        w.TriMat(hull, -hw * 0.08f, hh * 0.30f, l * 0.68f, hw * 0.08f, hh * 0.30f, l * 0.68f, 0, hh * 0.48f, l * 0.74f);
        AddBoxMat(w, engine, -hw * 0.14f, hw * 0.14f, 0, hh * 0.14f, -l * 0.50f, -l * 0.38f);
        for (int v = 0; v < 4; v++)
        {
            float z = l * (-0.02f + v * 0.07f);
            w.TriMat(accent, -hw * 0.05f, hh * 0.54f, z, hw * 0.05f, hh * 0.54f, z, 0, hh * 0.62f, z + l * 0.006f);
        }
    }

    /// <summary>Industrial/logistics nexar hulls — lofted chitin base with per-role offset mass attachments.</summary>
    private static void BuildAsymmetricUtilitySolid(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        // Loop-07 tri trim + aspect: utility hulls ≤180 sweet max, target aspect ~1.28.
        float widthScale = hullKey is "freighter_bulk" ? 0.88f
            : hullKey is "transport_cargo" ? 0.90f
            : hullKey is "miner_eva" ? 0.90f
            : hullKey is "miner_basic" or "miner_tractor" ? 0.92f
            : 0.94f;
        float l = len * (hullKey switch
        {
            "freighter_bulk" => 1.06f,
            "transport_cargo" => 1.10f,
            "miner_basic" => 1.04f,
            "miner_eva" => 1.02f,
            "miner_tractor" => 1.00f,
            "support_repair" => 1.02f,
            _ => 0.96f
        });
        float hw = wid * 0.56f * widthScale;
        float hh = hgt * 0.70f;
        int sections = 5;
        float bowZ = hullKey switch
        {
            "freighter_bulk" => 0.68f,
            "transport_cargo" => 0.66f,
            "miner_basic" => 0.58f,
            "miner_eva" => 0.52f,
            "miner_tractor" => 0.54f,
            "support_repair" => 0.56f,
            _ => 0.54f
        };

        AddNexarChitinCore(w, l, hw, hh, sections, widthScale, 0.42f);
        int rings = hullKey is "freighter_bulk" or "transport_cargo" ? 2 : 3;
        AddNexarEmbossedRings(w, l, hw, hh, rings, 0.42f);

        AddLoftedHull(w, l * 0.26f, hw * 0.76f, hh * 0.82f, 2,
            t => hw * 0.16f * (1f - t * 0.35f),
            t => hh * (0.48f + t * 0.38f),
            0.06f, bowZ * 0.82f,
            t => hw * NexarAsym * 0.32f * MathF.Sin(t * MathF.PI * 1.2f));

        switch (hullKey)
        {
            case "miner_basic": AddNexarMiningToolPods(w, hw, hh, l); break;
            case "miner_eva": AddNexarEvaPodBloom(w, hw, hh, l); break;
            case "miner_tractor": AddNexarTractorBloom(w, hw, hh, l); break;
            case "transport_cargo": AddNexarCargoSpineSac(w, hw, hh, l); break;
            case "freighter_bulk": AddNexarBulkChitinHull(w, hw, hh, l); break;
            case "support_repair": AddNexarRepairAntennaBlooms(w, hw, hh, l); break;
        }

    }

    private static void AddNexarMiningToolPods(RaceMeshWriter w, float hw, float hh, float len)
    {
        var chitin = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        w.TriMat(chitin, -hw * 0.06f, hh * 0.26f, len * 0.50f, hw * 0.06f, hh * 0.26f, len * 0.50f, 0, hh * 0.36f, len * 0.56f);
        for (int side = -1; side <= 1; side += 2)
        {
            float xRoot = side * hw * 0.30f;
            float xTip = side * hw * 0.70f;
            float zTip = len * 0.54f;
            w.TriMat(weapon, xRoot, hh * 0.14f, len * 0.08f, xTip, hh * 0.20f, zTip, xRoot, hh * 0.08f, zTip - len * 0.02f);
            w.TriMat(accent, xTip, hh * 0.20f, zTip, xTip + side * hw * 0.05f, hh * 0.26f, zTip + len * 0.02f, xTip, hh * 0.12f, zTip + len * 0.03f);
            AddIntegratedBulb(w, xTip, hh * 0.22f, zTip, hw * 0.11f, hh * 0.14f, 4);
        }
    }

    private static void AddNexarEvaPodBloom(RaceMeshWriter w, float hw, float hh, float len)
    {
        var chitin = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        AddIntegratedBulb(w, hw * 0.38f, hh * 0.48f, len * 0.10f, hw * 0.28f, hh * 0.18f, 5);
        w.TriMat(chitin, -hw * 0.05f, hh * 0.28f, len * 0.44f, hw * 0.05f, hh * 0.28f, len * 0.44f, 0, hh * 0.38f, len * 0.50f);
        w.TriMat(accent, hw * 0.44f, hh * 0.42f, len * 0.12f, hw * 0.52f, hh * 0.48f, len * 0.16f, hw * 0.46f, hh * 0.36f, len * 0.14f);
    }

    private static void AddNexarTractorBloom(RaceMeshWriter w, float hw, float hh, float len)
    {
        var chitin = RaceMeshWriter.HullMaterial.Hull;
        AddIntegratedBulb(w, 0, hh * 0.64f, len * 0.36f, hw * 0.22f, hh * 0.12f, 5);
        w.TriMat(chitin, -hw * 0.05f, hh * 0.30f, len * 0.42f, hw * 0.05f, hh * 0.30f, len * 0.42f, 0, hh * 0.40f, len * 0.48f);
        for (int side = -1; side <= 1; side += 2)
        {
            float xTip = side * hw * 0.62f;
            w.Tri(side * hw * 0.12f, hh * 0.22f, len * 0.04f, xTip, hh * 0.28f, len * 0.14f, xTip, hh * 0.18f, len * 0.18f);
            AddIntegratedBulb(w, xTip, hh * 0.30f, len * 0.12f, hw * 0.08f, hh * 0.10f, 4);
        }
    }

    private static void AddNexarCargoSpineSac(RaceMeshWriter w, float hw, float hh, float len)
    {
        // wo-mesh-r2-03 — flush cargo spine boxes (eliminate tri chevron sac / lofted facet seams).
        float yBase = hh * 0.44f;
        float yTop = hh * 0.56f;
        AddBox(w, -hw * 0.06f, hw * 0.06f, yBase, yTop, -len * 0.02f, len * 0.18f);
        AddBox(w, -hw * 0.04f, hw * 0.04f, yTop, yTop + hh * 0.06f, len * 0.14f, len * 0.30f);
        AddBox(w, -hw * 0.08f, hw * 0.08f, yBase - hh * 0.04f, yBase, len * 0.22f, len * 0.36f);
    }

    private static void AddNexarBulkChitinHull(RaceMeshWriter w, float hw, float hh, float len)
    {
        var chitin = RaceMeshWriter.HullMaterial.Hull;
        for (int b = 0; b < 2; b++)
        {
            float z = len * (0.08f + b * 0.12f);
            AddIntegratedBulb(w, 0, hh * 0.14f, z, hw * 0.24f, hh * 0.10f, 5);
        }
        w.TriMat(chitin, -hw * 0.06f, hh * 0.36f, len * 0.34f, hw * 0.06f, hh * 0.36f, len * 0.34f, 0, hh * 0.44f, len * 0.42f);
        for (int side = -1; side <= 1; side += 2)
            AddIntegratedBulb(w, side * hw * 0.34f, hh * 0.26f, len * 0.32f, hw * 0.11f, hh * 0.11f, 4);
    }

    private static void AddNexarRepairAntennaBlooms(RaceMeshWriter w, float hw, float hh, float len)
    {
        var chitin = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        for (int a = 0; a < 2; a++)
            AddIntegratedBulb(w, 0, hh * (0.72f + a * 0.06f), len * (0.06f + a * 0.06f), hw * 0.06f, hh * 0.10f, 4);
        w.TriMat(chitin, -hw * 0.02f, hh * 0.86f, len * 0.14f, hw * 0.02f, hh * 0.86f, len * 0.14f, 0, hh * 0.96f, len * 0.18f);
        w.TriMat(accent, -hw * 0.015f, hh * 0.92f, len * 0.16f, hw * 0.015f, hh * 0.92f, len * 0.16f, 0, hh * 1.00f, len * 0.20f);
        for (int side = -1; side <= 1; side += 2)
        {
            float xBoom = side * hw * 0.62f;
            w.Tri(side * hw * 0.10f, hh * 0.42f, len * 0.04f, xBoom, hh * 0.52f, len * 0.14f, xBoom, hh * 0.38f, len * 0.18f);
        }
    }

    private static void BuildLatticeFrame(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float l = len;
        float hw = wid * 0.62f;
        float hh = hgt * 0.72f;
        int sections = HullSections(hullKey, 12, 7, 16);
        float asym = 0.22f;

        AddLoftedHull(w, l, wid, hgt, sections,
            t => hw * (0.38f + 0.28f * MathF.Sin(t * MathF.PI) + (t > 0.75f ? (1f - t) / 0.25f * 0.1f : 0f)),
            t => hh * (0.45f + 0.55f * MathF.Sin(t * MathF.PI)),
            -0.55f, 0.75f,
            t => hw * asym * MathF.Sin(t * MathF.PI * 1.3f));

        int rings = HullSections(hullKey, 7, 4, 10);
        for (int r = 0; r < rings; r++)
        {
            float t = r / (float)Math.Max(1, rings - 1);
            float z = MathHelper.Lerp(-l * 0.5f, l * 0.55f, t);
            float skew = hw * asym * MathF.Sin(t * MathF.PI * 1.3f);
            AddEmbossedRing(w, hw * 0.88f, hh * 0.82f, z, skew, hh * 0.06f, 10);
        }

        AddLoftedHull(w, l * 0.28f, wid * 0.42f, hgt * 0.85f, 6,
            t => hw * 0.18f * (1f - t * 0.3f),
            t => hh * (0.5f + t * 0.4f),
            -0.06f, 0.18f,
            t => hw * asym * 0.35f);

        if (hullKey is not "drone")
        {
            AddQuadPanel(w, hw * 0.35f, hh * 0.75f, l * 0.32f, -hw * 0.15f, hh * 0.35f, -l * 0.22f);
            AddQuadPanel(w, -hw * 0.45f, hh * 0.55f, l * 0.38f, hw * 0.2f, -hh * 0.02f, -l * 0.28f);
        }
    }

    // ── Radiant (Solari) — per-hull solar crown deck silhouettes with role readability ─

    private static void BuildRadiantHull(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        switch (hullKey)
        {
            case "corvette":
            case "corvette_fast":
                BuildRadiantCorvetteFast(w, len, wid, hgt);
                return;
            case "frigate":
            case "frigate_strike":
                BuildRadiantFrigateStrike(w, len, wid, hgt);
                return;
            case "bomber":
            case "bomber_heavy":
                BuildRadiantBomberHeavy(w, len, wid, hgt);
                return;
            case "gunship":
            case "gunship_heavy":
                BuildRadiantGunshipHeavy(w, len, wid, hgt);
                return;
            case "destroyer":
            case "destroyer_assault":
                BuildRadiantDestroyerAssault(w, len, wid, hgt);
                return;
            case "cruiser":
            case "cruiser_heavy":
                BuildRadiantCruiserHeavy(w, len, wid, hgt);
                return;
            case "carrier":
            case "carrier_command":
                BuildRadiantCarrierCommand(w, len, wid, hgt);
                return;
            case "dreadnought":
                BuildRadiantDreadnought(w, len, wid, hgt);
                return;
            case "miner_basic":
            case "miner_eva":
            case "miner_tractor":
            case "transport_cargo":
            case "freighter_bulk":
            case "support_repair":
                BuildRadiantUtilitySolid(w, hullKey, len, wid, hgt);
                return;
        }

        BuildRadiantDeck(w, hullKey, len, wid, hgt);
    }

    /// <summary>Heavy cruiser — balanced solar mass, dorsal crown bloom ridge.</summary>
    private static void BuildRadiantCruiserHeavy(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Truss;
        var solar = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float l = len * 1.08f;
        float span = wid * 1.34f;
        float deckH = hgt * 0.32f;

        AddLoftedHull(w, l, wid * 0.58f, hgt * 0.80f, 9,
            t => wid * (0.24f + 0.16f * MathF.Sin(t * MathF.PI) + (t > 0.76f ? (1f - t) / 0.24f * 0.08f : 0f)),
            t => deckH * (0.58f + 0.34f * MathF.Sin(t * MathF.PI)),
            -0.54f, 0.74f);

        AddRadiantSolarCrownDeck(w, l, span, deckH, 3, 4);
        AddLoftedHull(w, l * 0.30f, wid * 0.28f, hgt * 0.94f, 2,
            t => wid * 0.09f * (1f - t * 0.35f),
            t => deckH + hgt * (0.44f + t * 0.46f),
            0.14f, 0.50f);

        float crownZ = l * 0.18f;
        w.TriMat(solar, -span * 0.08f, deckH * 1.42f, crownZ, span * 0.08f, deckH * 1.42f, crownZ, 0, deckH * 1.68f, crownZ + l * 0.06f);
        float keelY = deckH * 1.34f;
        w.TriMat(panel, -span * 0.06f, keelY, l * 0.14f, span * 0.06f, keelY, l * 0.14f, 0, keelY + deckH * 0.08f, l * 0.148f);
        w.TriMat(hull, -span * 0.08f, deckH * 0.58f, l * 0.62f, span * 0.08f, deckH * 0.58f, l * 0.62f, 0, deckH * 0.74f, l * 0.68f);
        AddBoxMat(w, weapon, -span * 0.10f, span * 0.10f, deckH * 0.10f, deckH * 0.20f, l * 0.54f, l * 0.64f);
        AddRadiantCapitalEngineWells(w, len, wid, hgt, 3);
    }

    /// <summary>Command carrier — flat deck/hangar solar crown, wide 3×5 panel grid.</summary>
    private static void BuildRadiantCarrierCommand(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Truss;
        var solar = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float l = len * 1.06f;
        float span = wid * 1.44f;
        float deckH = hgt * 0.28f;

        AddLoftedHull(w, l, wid * 0.64f, hgt * 0.74f, 12,
            t => wid * (0.26f + 0.14f * MathF.Sin(t * MathF.PI) + (t > 0.80f ? (1f - t) / 0.20f * 0.07f : 0f)),
            t => deckH * (0.52f + 0.30f * MathF.Sin(t * MathF.PI)),
            -0.50f, 0.74f);

        AddRadiantSolarCrownDeck(w, l, span, deckH, 3, 5);

        float hangarZ0 = -l * 0.28f;
        float hangarZ1 = l * 0.14f;
        w.TriMat(panel, -span * 0.38f, deckH * 0.96f, hangarZ1, span * 0.38f, deckH * 0.96f, hangarZ1, 0, deckH * 1.02f, hangarZ0);
        w.TriMat(panel, -span * 0.38f, deckH * 0.96f, hangarZ1, 0, deckH * 1.02f, hangarZ0, -span * 0.38f, deckH * 0.96f, hangarZ0);
        w.TriMat(solar, span * 0.38f, deckH * 0.96f, hangarZ1, span * 0.38f, deckH * 0.96f, hangarZ0, 0, deckH * 1.02f, hangarZ0);

        for (int h = 0; h < 2; h++)
        {
            float z = MathHelper.Lerp(hangarZ0 + l * 0.06f, hangarZ1 - l * 0.06f, h);
            w.TriMat(solar, -span * 0.22f, deckH * 0.90f, z, span * 0.22f, deckH * 0.90f, z, 0, deckH * 0.94f, z + l * 0.008f);
        }

        AddLoftedHull(w, l * 0.22f, wid * 0.26f, hgt * 0.82f, 3,
            t => wid * 0.08f * (1f - t * 0.30f),
            t => deckH + hgt * (0.40f + t * 0.34f),
            0.10f, 0.44f);

        float keelY = deckH * 1.18f;
        for (int k = 0; k < 3; k++)
        {
            float z = MathHelper.Lerp(l * 0.04f, l * 0.22f, k / 2f);
            w.TriMat(panel, -span * 0.07f, keelY, z, span * 0.07f, keelY, z, 0, keelY + deckH * 0.08f, z + l * 0.008f);
        }

        AddBoxMat(w, weapon, -span * 0.22f, span * 0.22f, deckH * 0.12f, deckH * 0.22f, -l * 0.04f, l * 0.04f);
        w.TriMat(hull, -span * 0.06f, deckH * 0.48f, l * 0.12f, span * 0.06f, deckH * 0.48f, l * 0.12f, 0, deckH * 0.54f, l * 0.16f);
        AddRadiantCapitalEngineWells(w, len, wid, hgt, 3);
    }

    /// <summary>Dreadnought — imposing prow cluster, embossed panel stacks, elevated superstructure.</summary>
    private static void BuildRadiantDreadnought(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Truss;
        var solar = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float l = len * 1.06f;
        float span = wid * 1.40f;
        float deckH = hgt * 0.30f;

        AddLoftedHull(w, l, wid * 0.62f, hgt * 0.78f, 14,
            t =>
            {
                float belly = 0.24f + 0.16f * MathF.Sin(t * MathF.PI);
                float prow = t > 0.70f ? 1f + (t - 0.70f) * 0.12f : 1f;
                float nose = t > 0.84f ? (1f - t) / 0.16f : 1f;
                return wid * belly * prow * nose;
            },
            t => deckH * (0.54f + 0.32f * MathF.Sin(t * MathF.PI)),
            -0.56f, 0.80f);

        AddRadiantSolarCrownDeck(w, l, span, deckH, 3, 5);

        float prowZ = l * 0.62f;
        for (int p = 0; p < 3; p++)
        {
            float side = (p - 1) * span * 0.18f;
            AddBoxMat(w, panel, side - span * 0.06f, side + span * 0.06f, deckH * 0.50f, deckH * 0.64f, prowZ - l * 0.04f, prowZ + l * 0.06f);
            w.TriMat(solar, side, deckH * 0.68f, prowZ + l * 0.04f, side - span * 0.05f, deckH * 0.58f, prowZ, side + span * 0.05f, deckH * 0.58f, prowZ);
        }

        AddLoftedHull(w, l * 0.34f, wid * 0.30f, hgt * 0.94f, 5,
            t => wid * 0.10f * (1f - t * 0.32f),
            t => deckH + hgt * (0.46f + t * 0.42f),
            0.20f, 0.54f);

        for (int stack = 0; stack < 3; stack++)
        {
            float z = MathHelper.Lerp(l * 0.36f, l * 0.54f, stack / 2f);
            AddBoxMat(w, panel, -span * 0.10f, span * 0.10f, deckH * 0.32f + stack * deckH * 0.05f, deckH * 0.40f + stack * deckH * 0.05f, z, z + l * 0.06f);
        }

        AddBoxMat(w, weapon, -span * 0.12f, span * 0.12f, deckH * 0.08f, deckH * 0.18f, l * 0.62f, l * 0.72f);
        w.TriMat(hull, -span * 0.08f, deckH * 0.56f, l * 0.68f, span * 0.08f, deckH * 0.56f, l * 0.68f, 0, deckH * 0.70f, l * 0.74f);
        AddRadiantCapitalEngineWells(w, len, wid, hgt, 3);
    }

    private static void AddRadiantCapitalEngineWells(RaceMeshWriter w, float len, float wid, float hgt, int count)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        float hw = wid * 0.5f;
        count = Math.Clamp(count, 2, 3);
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : i / (float)(count - 1);
            float cx = MathHelper.Lerp(-hw * 0.30f, hw * 0.30f, t);
            float halfW = hw * 0.11f;
            AddBoxMat(w, hull, cx - halfW, cx + halfW, 0, hgt * 0.20f, -len * 0.30f, -len * 0.14f);
            AddBoxMat(w, engine, cx - halfW * 0.52f, cx + halfW * 0.52f, hgt * 0.02f, hgt * 0.16f, -len * 0.32f, -len * 0.18f);
        }
    }

    /// <summary>Fast corvette — swept radiant wedge, narrow fuselage pod, modest +Z bow.</summary>
    private static void BuildRadiantCorvetteFast(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Truss;
        var solar = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        float l = len * 1.08f;
        float span = wid * 1.22f;
        float deckH = hgt * 0.30f;

        AddLoftedHull(w, l, wid * 0.50f, hgt * 0.72f, 11,
            t =>
            {
                float belly = 0.40f + 0.52f * MathF.Sin(t * MathF.PI);
                float nose = t > 0.82f ? (1f - t) / 0.18f : 1f;
                float sweep = t > 0.64f ? 1f + (t - 0.64f) * 0.10f : 1f;
                return wid * belly * nose * sweep * 0.38f;
            },
            t => deckH * (0.50f + 0.42f * MathF.Sin(t * MathF.PI)),
            -0.48f, 0.78f);

        AddRadiantSolarCrownDeck(w, l, span, deckH, 2, 3);
        AddBoxMat(w, weapon, -span * 0.22f, -span * 0.10f, deckH * 0.06f, deckH * 0.16f, l * 0.04f, l * 0.14f);
        AddBoxMat(w, weapon, span * 0.10f, span * 0.22f, deckH * 0.06f, deckH * 0.16f, l * 0.04f, l * 0.14f);
        w.TriMat(hull, -span * 0.06f, deckH * 0.28f, l * 0.68f, span * 0.06f, deckH * 0.28f, l * 0.68f, 0, deckH * 0.48f, l * 0.74f);
        w.TriMat(panel, -span * 0.05f, deckH * 0.46f, l * 0.52f, span * 0.05f, deckH * 0.46f, l * 0.52f, 0, deckH * 0.52f, l * 0.58f);
        AddBoxMat(w, engine, -span * 0.10f, span * 0.10f, 0, deckH * 0.14f, -l * 0.44f, -l * 0.34f);
        w.TriMat(solar, -span * 0.04f, deckH * 0.50f, l * 0.16f, span * 0.04f, deckH * 0.50f, l * 0.16f, 0, deckH * 0.56f, l * 0.20f);
    }

    /// <summary>Strike frigate — balanced gun-pod solar deck blooms.</summary>
    private static void BuildRadiantFrigateStrike(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Truss;
        var solar = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        float l = len * 1.10f;
        float span = wid * 1.30f;
        float deckH = hgt * 0.32f;

        AddLoftedHull(w, l, wid * 0.58f, hgt * 0.78f, 12,
            t =>
            {
                float belly = 0.44f + 0.50f * MathF.Sin(t * MathF.PI);
                float bulge = MathF.Max(0f, 1f - MathF.Abs(t - 0.40f) * 4.2f) * 0.10f;
                float nose = t > 0.78f ? (1f - t) / 0.22f : 1f;
                return wid * belly * nose * (0.36f + bulge);
            },
            t => deckH * (0.52f + 0.40f * MathF.Sin(t * MathF.PI)),
            -0.50f, 0.78f);

        AddRadiantSolarCrownDeck(w, l, span, deckH, 2, 4);
        for (int p = 0; p < 2; p++)
        {
            float side = (p == 0 ? -1f : 1f) * span * 0.28f;
            AddBoxMat(w, weapon, side - span * 0.06f, side + span * 0.06f, deckH * 0.08f, deckH * 0.18f, l * 0.08f, l * 0.18f);
        }
        AddBoxMat(w, panel, -span * 0.10f, span * 0.10f, deckH * 0.50f, deckH * 0.60f, l * 0.10f, l * 0.22f);
        w.TriMat(hull, -span * 0.08f, deckH * 0.30f, l * 0.64f, span * 0.08f, deckH * 0.30f, l * 0.64f, 0, deckH * 0.46f, l * 0.70f);
        AddBoxMat(w, engine, -span * 0.12f, span * 0.12f, 0, deckH * 0.16f, -l * 0.46f, -l * 0.36f);
        for (int v = 0; v < 3; v++)
        {
            float z = l * (0.04f + v * 0.08f);
            w.TriMat(solar, -span * 0.05f, deckH * 0.54f, z, span * 0.05f, deckH * 0.54f, z, 0, deckH * 0.60f, z + l * 0.006f);
        }
    }

    /// <summary>Heavy bomber — wide payload crown array under solar deck.</summary>
    private static void BuildRadiantBomberHeavy(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Truss;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        float l = len * 1.02f;
        float span = wid * 1.42f;
        float deckH = hgt * 0.23f;

        AddLoftedHull(w, l, wid * 0.62f, hgt * 0.58f, 12,
            t =>
            {
                float belly = 0.50f + 0.46f * MathF.Sin(t * MathF.PI);
                float sac = MathF.Max(0f, 1f - MathF.Abs(t - 0.36f) * 3.0f) * 0.16f;
                float nose = t > 0.76f ? (1f - t) / 0.24f : 1f;
                return wid * belly * nose * (0.40f + sac);
            },
            t => deckH * (0.46f + 0.46f * MathF.Sin(t * MathF.PI)),
            -0.52f, 0.80f);

        AddRadiantSolarCrownDeck(w, l, span, deckH, 2, 4, crownRise: 1.14f);
        w.TriMat(weapon, -span * 0.26f, 0, l * 0.02f, span * 0.26f, 0, l * 0.02f, 0, deckH * 0.12f, l * 0.10f);
        AddBoxMat(w, panel, -span * 0.20f, span * 0.20f, deckH * 0.28f, deckH * 0.44f, l * 0.02f, l * 0.16f);
        w.TriMat(hull, -span * 0.10f, deckH * 0.26f, l * 0.62f, span * 0.10f, deckH * 0.26f, l * 0.62f, 0, deckH * 0.40f, l * 0.68f);
        AddBoxMat(w, engine, -span * 0.14f, span * 0.14f, 0, deckH * 0.14f, -l * 0.48f, -l * 0.38f);
    }

    /// <summary>Heavy gunship — chin weapon pod under wide solar crown.</summary>
    private static void BuildRadiantGunshipHeavy(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var solar = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        float l = len * 1.04f;
        float span = wid * 1.34f;
        float deckH = hgt * 0.19f;

        AddLoftedHull(w, l, wid * 0.60f, hgt * 0.52f, 11,
            t =>
            {
                float belly = 0.48f + 0.48f * MathF.Sin(t * MathF.PI);
                float chin = t > 0.24f && t < 0.50f ? 0.12f : 0f;
                float nose = t > 0.74f ? (1f - t) / 0.26f : 1f;
                return wid * belly * nose * (0.38f + chin);
            },
            t => deckH * (0.44f + 0.42f * MathF.Sin(t * MathF.PI)),
            -0.48f, 0.74f);

        AddRadiantSolarCrownDeck(w, l, span, deckH, 2, 3, crownRise: 1.06f);
        w.TriMat(weapon, -span * 0.16f, 0, l * 0.24f, span * 0.16f, 0, l * 0.24f, 0, deckH * 0.08f, l * 0.44f);
        w.TriMat(weapon, -span * 0.12f, 0, l * 0.32f, span * 0.12f, 0, l * 0.32f, 0, deckH * 0.08f, l * 0.50f);
        AddBoxMat(w, weapon, -span * 0.30f, -span * 0.18f, deckH * 0.04f, deckH * 0.12f, l * 0.06f, l * 0.14f);
        AddBoxMat(w, weapon, span * 0.18f, span * 0.30f, deckH * 0.04f, deckH * 0.12f, l * 0.06f, l * 0.14f);
        w.TriMat(hull, -span * 0.08f, deckH * 0.24f, l * 0.56f, span * 0.08f, deckH * 0.24f, l * 0.56f, 0, deckH * 0.36f, l * 0.62f);
        AddBoxMat(w, engine, -span * 0.12f, span * 0.12f, 0, deckH * 0.14f, -l * 0.44f, -l * 0.34f);
        w.TriMat(solar, -span * 0.06f, deckH * 0.48f, l * 0.08f, span * 0.06f, deckH * 0.48f, l * 0.08f, 0, deckH * 0.54f, l * 0.12f);
    }

    /// <summary>Assault destroyer — dorsal spine ridge with embossed panel stacks.</summary>
    private static void BuildRadiantDestroyerAssault(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Truss;
        var solar = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        float l = len * 1.16f;
        float span = wid * 1.22f;
        float deckH = hgt * 0.34f;

        AddLoftedHull(w, l, wid * 0.54f, hgt * 0.82f, 10,
            t =>
            {
                float belly = 0.46f + 0.50f * MathF.Sin(t * MathF.PI);
                float ridge = MathF.Max(0f, 1f - MathF.Abs(t - 0.44f) * 3.4f) * 0.14f;
                float nose = t > 0.80f ? (1f - t) / 0.20f : 1f;
                return wid * belly * nose * (0.38f + ridge);
            },
            t => deckH * (0.50f + 0.48f * MathF.Sin(t * MathF.PI)),
            -0.46f, 0.86f);

        AddRadiantSolarCrownDeck(w, l, span, deckH, 3, 4);
        AddLoftedHull(w, l * 0.52f, wid * 0.22f, hgt * 0.78f, 3,
            t => wid * 0.06f * (1f - t * 0.30f),
            t => deckH + hgt * (0.42f + t * 0.48f),
            0.16f, 0.72f);

        AddHullRing(w, span * 0.22f, deckH * 0.56f, l * 0.04f, 5);

        AddBoxMat(w, weapon, -span * 0.10f, span * 0.10f, deckH * 0.10f, deckH * 0.22f, l * 0.48f, l * 0.60f);
        AddBoxMat(w, panel, -span * 0.10f, span * 0.10f, deckH * 0.66f, deckH * 0.80f, l * 0.08f, l * 0.20f);
        w.TriMat(hull, -span * 0.08f, deckH * 0.34f, l * 0.68f, span * 0.08f, deckH * 0.34f, l * 0.68f, 0, deckH * 0.52f, l * 0.74f);
        AddBoxMat(w, engine, -span * 0.14f, span * 0.14f, 0, deckH * 0.16f, -l * 0.50f, -l * 0.40f);
        for (int v = 0; v < 2; v++)
        {
            float z = l * (0.04f + v * 0.10f);
            w.TriMat(solar, -span * 0.06f, deckH * 0.58f, z, span * 0.06f, deckH * 0.58f, z, 0, deckH * 0.66f, z + l * 0.006f);
        }
    }

    private static void AddRadiantSolarCrownDeck(
        RaceMeshWriter w, float l, float span, float deckH, int panelRows, int panelCols, float crownRise = 1.55f)
    {
        float deckZ0 = -l * 0.38f;
        float deckZ1 = l * 0.38f;
        float crownPeak = deckH * crownRise;
        w.Tri(-span * 0.5f, deckH, deckZ1, span * 0.5f, deckH, deckZ1, 0, crownPeak, deckZ0 + l * 0.08f);
        w.Tri(-span * 0.5f, deckH, deckZ1, 0, crownPeak, deckZ0 + l * 0.08f, -span * 0.5f, deckH, deckZ0);
        w.Tri(span * 0.5f, deckH, deckZ1, span * 0.5f, deckH, deckZ0, 0, crownPeak, deckZ0 + l * 0.08f);
        w.Tri(-span * 0.5f, deckH * 0.92f, deckZ1, span * 0.5f, deckH * 0.92f, deckZ1, span * 0.5f, deckH * 0.92f, deckZ0);
        w.Tri(-span * 0.5f, deckH * 0.92f, deckZ1, span * 0.5f, deckH * 0.92f, deckZ0, -span * 0.5f, deckH * 0.92f, deckZ0);

        for (int row = 0; row < panelRows; row++)
        {
            for (int col = 0; col < panelCols; col++)
            {
                float u = col / (float)Math.Max(1, panelCols - 1);
                float v = row / (float)Math.Max(1, panelRows - 1);
                float x = MathHelper.Lerp(-span * 0.42f, span * 0.42f, u);
                float z0 = MathHelper.Lerp(deckZ0, deckZ1 - l * 0.08f, v);
                float z1 = z0 + l * 0.1f;
                float yPeak = deckH * (1.35f + v * 0.45f);
                w.Tri(x - span * 0.05f, deckH, z0, x + span * 0.05f, deckH, z0, x, yPeak, z1);
                w.Tri(x - span * 0.05f, deckH, z0, x, yPeak, z1, x - span * 0.04f, deckH, z1);
                w.Tri(x + span * 0.05f, deckH, z0, x + span * 0.04f, deckH, z1, x, yPeak, z1);
            }
        }
    }

    private static void BuildRadiantDeck(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        switch (hullKey)
        {
            case "fighter":
            case "fighter_basic":
                BuildRadiantFighter(w, len, wid, hgt);
                return;
            case "hero":
            case "hero_default":
                BuildRadiantHero(w, len, wid, hgt);
                return;
            case "scout":
            case "scout_light":
                BuildRadiantScout(w, len, wid, hgt);
                return;
            case "interceptor":
            case "interceptor_mk2":
                BuildRadiantInterceptor(w, len, wid, hgt);
                return;
            case "drone":
            case "drone_swarm":
                BuildRadiantDrone(w, len, wid, hgt);
                return;
        }

        BuildRadiantDeckGeneric(w, hullKey, len, wid, hgt);
    }

    private static void BuildRadiantScout(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float l = len * 0.90f;
        float hw = wid * 0.5f;
        float deckH = hgt * 0.30f;
        float span = wid * 1.00f;

        AddRadiantFuselagePod(w, hull, l, hw * 0.40f, hgt * 0.72f, deckH, 8, -0.50f, 0.74f);
        AddRadiantSolarCrownDeck(w, hull, panel, accent, l, span, deckH, 2, 2, 0.38f, 0.40f);
        AddRadiantBowProbe(w, hull, accent, hw * 0.07f, deckH * 1.42f, l * 0.56f, l * 0.07f);
        AddRadiantDorsalCrownSpine(w, accent, panel, hw, deckH, l, 0.32f, 0.36f, 4);
        AddRadiantBowElongation(w, hull, accent, hw * 0.05f, deckH * 1.36f, l * 0.54f, l * 0.07f);

        for (int side = -1; side <= 1; side += 2)
            AddBoxMat(w, weapon, side * hw * 0.36f - hw * 0.05f, side * hw * 0.36f + hw * 0.05f,
                deckH * 0.08f, deckH * 0.16f, l * 0.06f, l * 0.14f);
        for (int e = 0; e < 3; e++)
        {
            float x = (e - 1) * hw * 0.12f;
            AddBoxMat(w, engine, x - hw * 0.04f, x + hw * 0.04f, 0, deckH * 0.12f, -l * 0.36f, -l * 0.26f);
        }
        AddBoxMat(w, shield, -hw * 0.05f, hw * 0.05f, deckH * 1.18f, deckH * 1.30f, l * 0.04f, l * 0.12f);
    }

    private static void BuildRadiantFighter(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float l = len * 0.90f;
        float hw = wid * 0.5f;
        float deckH = hgt * 0.28f;
        float span = wid * 1.35f;

        AddRadiantFuselagePod(w, hull, l, hw * 0.55f, hgt * 0.75f, deckH, 10, -0.55f, 0.70f);
        AddRadiantSolarCrownDeck(w, hull, panel, accent, l, span, deckH, 2, 3, 0.38f, 0.40f);
        AddRadiantBowProbe(w, hull, accent, hw * 0.12f, deckH * 1.48f, l * 0.52f, l * 0.07f);
        AddRadiantBowElongation(w, hull, accent, hw * 0.08f, deckH * 1.42f, l * 0.50f, l * 0.06f);
        AddRadiantDorsalCrownSpine(w, accent, panel, hw, deckH, l, 0.34f, 0.24f, 4);
        AddRadiantDorsalSolarKeelBand(w, hw, deckH, l * 0.04f, l * 0.48f, 2);

        for (int side = -1; side <= 1; side += 2)
            AddRadiantSweptSolarFin(w, accent, panel, side, l, hw, deckH, 0.28f, 0.34f);
        for (int e = 0; e < 3; e++)
        {
            float x = (e - 1) * hw * 0.16f;
            AddBoxMat(w, engine, x - hw * 0.05f, x + hw * 0.05f, 0, deckH * 0.14f, -l * 0.36f, -l * 0.26f);
        }
        for (int side = -1; side <= 1; side += 2)
            AddBoxMat(w, weapon, side * hw * 0.44f - hw * 0.06f, side * hw * 0.44f + hw * 0.06f,
                deckH * 0.08f, deckH * 0.16f, l * 0.02f, l * 0.10f);
        AddBoxMat(w, shield, -hw * 0.07f, hw * 0.07f, deckH * 1.22f, deckH * 1.34f, l * 0.04f, l * 0.12f);
    }

    private static void BuildRadiantInterceptor(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float l = len * 0.90f;
        float hw = wid * 0.5f;
        float deckH = hgt * 0.29f;
        float span = wid * 1.22f;

        AddRadiantFuselagePod(w, hull, l, hw * 0.50f, hgt * 0.74f, deckH, 9, -0.52f, 0.76f);
        AddRadiantSolarCrownDeck(w, hull, panel, accent, l, span, deckH, 2, 2, 0.36f, 0.40f);
        AddRadiantBowProbe(w, hull, accent, hw * 0.10f, deckH * 1.46f, l * 0.58f, l * 0.07f);
        AddRadiantDorsalCrownSpine(w, accent, panel, hw, deckH, l, 0.32f, 0.30f, 4);
        AddRadiantDorsalSolarKeelBand(w, hw, deckH, l * 0.08f, l * 0.56f, 3);
        AddRadiantBowElongation(w, hull, accent, hw * 0.08f, deckH * 1.40f, l * 0.56f, l * 0.05f);

        for (int side = -1; side <= 1; side += 2)
            AddRadiantSweptSolarFin(w, accent, panel, side, l, hw, deckH, 0.66f, 0.48f);
        AddBoxMat(w, weapon, -hw * 0.07f, hw * 0.07f, deckH * 0.10f, deckH * 0.20f, l * 0.26f, l * 0.36f);
        for (int side = -1; side <= 1; side += 2)
            AddBoxMat(w, weapon, side * hw * 0.40f - hw * 0.05f, side * hw * 0.40f + hw * 0.05f,
                deckH * 0.08f, deckH * 0.16f, l * 0.02f, l * 0.10f);
        for (int e = 0; e < 3; e++)
        {
            float x = (e - 1) * hw * 0.14f;
            AddBoxMat(w, engine, x - hw * 0.05f, x + hw * 0.05f, 0, deckH * 0.14f, -l * 0.34f, -l * 0.24f);
        }
        AddBoxMat(w, shield, -hw * 0.06f, hw * 0.06f, deckH * 1.20f, deckH * 1.32f, l * 0.04f, l * 0.12f);
    }

    private static void BuildRadiantDrone(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float l = len * 0.94f;
        float hw = wid * 0.5f;
        float deckH = hgt * 0.26f;
        float span = wid * 0.92f;

        AddRadiantFuselagePod(w, hull, l, hw * 0.36f, hgt * 0.68f, deckH, 7, -0.48f, 0.68f);
        AddRadiantSolarCrownDeck(w, hull, panel, accent, l, span, deckH, 2, 2, 0.32f, 0.36f);
        AddRadiantBowProbe(w, hull, accent, hw * 0.05f, deckH * 1.32f, l * 0.52f, l * 0.07f);
        AddRadiantDorsalSolarKeelBand(w, hw, deckH, -l * 0.04f, l * 0.50f, 3);
        AddRadiantBowElongation(w, hull, accent, hw * 0.04f, deckH * 1.28f, l * 0.50f, l * 0.05f);

        float[] nodeZ = [-l * 0.08f, l * 0.12f, l * 0.26f];
        for (int n = 0; n < nodeZ.Length; n++)
            AddIntegratedBulb(w, 0, deckH * (1.10f + n * 0.08f), nodeZ[n], hw * (0.09f - n * 0.02f), deckH * 0.14f, 3);
        for (int side = -1; side <= 1; side += 2)
            AddIntegratedBulb(w, side * hw * 0.14f, deckH * 0.92f, l * 0.04f, hw * 0.06f, deckH * 0.10f, 3);

        AddBoxMat(w, engine, -hw * 0.09f, hw * 0.09f, 0, deckH * 0.12f, -l * 0.32f, -l * 0.20f);
        for (int side = -1; side <= 1; side += 2)
            AddBoxMat(w, weapon, side * hw * 0.22f - hw * 0.04f, side * hw * 0.22f + hw * 0.04f,
                deckH * 0.08f, deckH * 0.14f, l * 0.10f, l * 0.18f);
        AddBoxMat(w, shield, -hw * 0.04f, hw * 0.04f, deckH * 1.10f, deckH * 1.20f, l * 0.02f, l * 0.10f);
    }

    private static void BuildRadiantHero(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float l = len * 0.92f;
        float hw = wid * 0.5f;
        float deckH = hgt * 0.30f;
        float span = wid * 1.40f;

        AddRadiantFuselagePod(w, hull, l, hw * 0.58f, hgt * 0.78f, deckH, 10, -0.56f, 0.74f);
        AddRadiantSolarCrownDeck(w, hull, panel, accent, l, span, deckH, 2, 3, 0.40f, 0.42f);
        AddRadiantBowProbe(w, hull, accent, hw * 0.11f, deckH * 1.52f, l * 0.54f, l * 0.07f);
        AddRadiantDorsalCrownSpine(w, accent, panel, hw, deckH, l, 0.36f, 0.30f, 4);
        AddRadiantDorsalSolarKeelBand(w, hw, deckH, l * 0.06f, l * 0.52f, 2);
        AddRadiantBowElongation(w, hull, accent, hw * 0.09f, deckH * 1.46f, l * 0.52f, l * 0.05f);

        for (int side = -1; side <= 1; side += 2)
            AddRadiantSweptSolarFin(w, accent, panel, side, l, hw, deckH, 0.30f, 0.38f);
        AddIntegratedBulb(w, 0, deckH * 1.72f, l * 0.20f, hw * 0.14f, deckH * 0.22f, 6);
        for (int e = 0; e < 3; e++)
        {
            float x = (e - 1) * hw * 0.17f;
            AddBoxMat(w, engine, x - hw * 0.06f, x + hw * 0.06f, 0, deckH * 0.16f, -l * 0.38f, -l * 0.28f);
        }
        for (int side = -1; side <= 1; side += 2)
            AddBoxMat(w, weapon, side * hw * 0.48f - hw * 0.06f, side * hw * 0.48f + hw * 0.06f,
                deckH * 0.08f, deckH * 0.16f, l * 0.04f, l * 0.12f);
        AddBoxMat(w, shield, -hw * 0.08f, hw * 0.08f, deckH * 1.28f, deckH * 1.40f, l * 0.06f, l * 0.14f);
    }

    /// <summary>Industrial/logistics radiant hulls — lofted fuselage under wide solar crown with per-role attachments.</summary>
    private static void BuildRadiantUtilitySolid(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var solar = RaceMeshWriter.HullMaterial.Solar;
        float widthScale = hullKey is "freighter_bulk" ? 0.90f : hullKey is "miner_eva" ? 0.94f : 0.98f;
        float l = len * (hullKey switch
        {
            "freighter_bulk" => 1.00f,
            "transport_cargo" => 1.03f,
            "miner_basic" => 1.00f,
            "miner_eva" => 0.96f,
            "miner_tractor" => 1.00f,
            "support_repair" => 1.02f,
            _ => 0.94f
        });
        float bw = wid * widthScale * (hullKey is "transport_cargo" ? 1.00f : hullKey is "freighter_bulk" ? 1.00f : 1.04f);
        float bh = hgt * (hullKey is "freighter_bulk" ? 1.36f : hullKey is "transport_cargo" ? 1.22f : 1.26f);
        float deckH = bh * 0.30f;
        float span = bw * (hullKey is "freighter_bulk" ? 1.34f : hullKey is "transport_cargo" ? 1.22f : 1.28f);
        int sections = HullSections(hullKey, 10, 7, 14);
        float bowZ = hullKey switch
        {
            "freighter_bulk" => 0.74f,
            "transport_cargo" => 0.76f,
            "miner_basic" => 0.64f,
            "miner_eva" => 0.58f,
            "miner_tractor" => 0.62f,
            "support_repair" => 0.62f,
            _ => 0.54f
        };

        AddRadiantFuselagePod(w, hull, l, bw * 0.52f, bh * 0.72f, deckH, sections, -0.48f, bowZ);
        AddRadiantSolarCrownDeck(w, hull, panel, solar, l, span, deckH,
            hullKey is "freighter_bulk" ? 3 : 2,
            hullKey is "freighter_bulk" or "transport_cargo" ? 4 : 4,
            hullKey is "freighter_bulk" ? 0.40f : 0.38f,
            hullKey is "freighter_bulk" ? 0.38f : 0.38f);
        float spineZStart = hullKey is "freighter_bulk" ? 0.44f : hullKey is "transport_cargo" ? 0.40f : 0.30f;
        float spineZEnd = hullKey switch
        {
            "freighter_bulk" => 0.42f,
            "transport_cargo" => 0.46f,
            "miner_tractor" => 0.30f,
            "support_repair" => 0.28f,
            _ => 0.26f
        };
        AddRadiantDorsalCrownSpine(w, solar, panel, bw * 0.5f, deckH, l, spineZStart, spineZEnd,
            hullKey is "freighter_bulk" ? 3 : 4);
        float keelZ0 = hullKey switch
        {
            "freighter_bulk" => l * 0.04f,
            "transport_cargo" => l * 0.02f,
            "miner_tractor" => l * 0.02f,
            "support_repair" => l * 0.00f,
            "miner_basic" => l * 0.00f,
            "miner_eva" => -l * 0.02f,
            _ => l * 0.02f
        };
        float keelZ1 = hullKey switch
        {
            "freighter_bulk" => l * 0.44f,
            "transport_cargo" => l * 0.44f,
            "miner_tractor" => l * 0.38f,
            "support_repair" => l * 0.36f,
            "miner_basic" => l * 0.32f,
            "miner_eva" => l * 0.28f,
            _ => l * 0.30f
        };
        int keelSegs = hullKey is "freighter_bulk" ? 2 : hullKey is "transport_cargo" ? 3 : 3;
        AddRadiantDorsalSolarKeelBand(w, bw * 0.5f, deckH, keelZ0, keelZ1, keelSegs);

        float hw = wid * 0.5f * widthScale;
        switch (hullKey)
        {
            case "miner_basic": AddRadiantMiningToolPods(w, hw, hgt, len); break;
            case "miner_eva": AddRadiantEvaPodBloom(w, hw, hgt, len); break;
            case "miner_tractor": AddRadiantTractorBloom(w, hw, hgt, len); break;
            case "transport_cargo": AddRadiantCargoSpineCrown(w, hw, hgt, len); break;
            case "freighter_bulk": AddRadiantBulkSolarBands(w, hw, hgt, len); break;
            case "support_repair": AddRadiantRepairAntennaBlooms(w, hw, hgt, len); break;
        }
    }

    private static void AddRadiantMiningToolPods(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var solar = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        w.TriMat(hull, -hw * 0.06f, hgt * 0.28f, len * 0.52f, hw * 0.06f, hgt * 0.28f, len * 0.52f, 0, hgt * 0.38f, len * 0.60f);
        for (int side = -1; side <= 1; side += 2)
        {
            float xRoot = side * hw * 0.34f;
            float xTip = side * hw * 0.68f;
            float zTip = len * 0.58f;
            w.TriMat(weapon, xRoot, hgt * 0.14f, len * 0.08f, xTip, hgt * 0.20f, zTip, xRoot, hgt * 0.08f, zTip - len * 0.02f);
            w.TriMat(solar, xTip, hgt * 0.20f, zTip, xTip + side * hw * 0.05f, hgt * 0.28f, zTip + len * 0.02f, xTip, hgt * 0.12f, zTip + len * 0.03f);
            AddIntegratedBulb(w, xTip, hgt * 0.22f, zTip, hw * 0.11f, hgt * 0.14f, 7);
            w.TriMat(solar, xTip - side * hw * 0.04f, hgt * 0.30f, zTip + len * 0.01f,
                xTip + side * hw * 0.04f, hgt * 0.30f, zTip + len * 0.01f, xTip, hgt * 0.36f, zTip + len * 0.04f);
        }
    }

    private static void AddRadiantEvaPodBloom(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var solar = RaceMeshWriter.HullMaterial.Solar;
        AddIntegratedBulb(w, 0, hgt * 0.52f, len * 0.10f, hw * 0.28f, hgt * 0.18f, 8);
        w.TriMat(solar, -hw * 0.05f, hgt * 0.44f, len * 0.12f, hw * 0.05f, hgt * 0.44f, len * 0.12f, 0, hgt * 0.52f, len * 0.16f);
        w.TriMat(hull, -hw * 0.05f, hgt * 0.30f, len * 0.46f, hw * 0.05f, hgt * 0.30f, len * 0.46f, 0, hgt * 0.40f, len * 0.54f);
        AddIntegratedBulb(w, hw * 0.42f, hgt * 0.38f, len * 0.14f, hw * 0.10f, hgt * 0.12f, 6);
    }

    private static void AddRadiantTractorBloom(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var solar = RaceMeshWriter.HullMaterial.Solar;
        AddIntegratedBulb(w, 0, hgt * 0.66f, len * 0.40f, hw * 0.22f, hgt * 0.14f, 8);
        w.TriMat(hull, -hw * 0.05f, hgt * 0.32f, len * 0.44f, hw * 0.05f, hgt * 0.32f, len * 0.44f, 0, hgt * 0.42f, len * 0.54f);
        w.TriMat(solar, -hw * 0.04f, hgt * 0.48f, len * 0.36f, hw * 0.04f, hgt * 0.48f, len * 0.36f, 0, hgt * 0.56f, len * 0.42f);
        for (int side = -1; side <= 1; side += 2)
        {
            float xTip = side * hw * 0.64f;
            w.TriMat(solar, side * hw * 0.14f, hgt * 0.24f, len * 0.06f, xTip, hgt * 0.30f, len * 0.20f, xTip, hgt * 0.20f, len * 0.22f);
            AddIntegratedBulb(w, xTip, hgt * 0.30f, len * 0.18f, hw * 0.09f, hgt * 0.10f, 6);
        }
    }

    private static void AddRadiantCargoSpineCrown(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var solar = RaceMeshWriter.HullMaterial.Solar;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var hull = RaceMeshWriter.HullMaterial.Hull;
        for (int s = 0; s < 3; s++)
        {
            float t = s / 2f;
            float z = MathHelper.Lerp(-len * 0.02f, len * 0.52f, t);
            AddIntegratedBulb(w, 0, hgt * 0.50f + t * hgt * 0.08f, z, hw * MathHelper.Lerp(0.12f, 0.05f, t), hgt * 0.13f, 8);
            w.TriMat(solar, -hw * 0.04f, hgt * (0.58f + t * 0.04f), z, hw * 0.04f, hgt * (0.58f + t * 0.04f), z,
                0, hgt * (0.66f + t * 0.04f), z + len * 0.008f);
        }
        AddLoftedHull(w, len * 0.30f, hw * 0.09f, hgt * 0.54f, 4, _ => hw * 0.04f, t => hgt * (0.48f + t * 0.14f), 0.02f, 0.48f);
        w.TriMat(hull, -hw * 0.04f, hgt * 0.30f, len * 0.46f, hw * 0.04f, hgt * 0.30f, len * 0.46f, 0, hgt * 0.36f, len * 0.52f);
        w.TriMat(panel, -hw * 0.06f, hgt * 0.34f, len * 0.32f, hw * 0.06f, hgt * 0.34f, len * 0.32f, 0, hgt * 0.42f, len * 0.38f);
    }

    private static void AddRadiantBulkSolarBands(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var solar = RaceMeshWriter.HullMaterial.Solar;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        for (int b = 0; b < 2; b++)
        {
            float z = len * (0.10f + b * 0.14f);
            AddIntegratedBulb(w, 0, hgt * 0.16f, z, hw * 0.26f, hgt * 0.11f, 8);
            w.TriMat(solar, -hw * 0.10f, hgt * 0.22f, z, hw * 0.10f, hgt * 0.22f, z, 0, hgt * 0.30f, z + len * 0.04f);
        }
        w.TriMat(solar, -hw * 0.04f, hgt * 0.40f, len * 0.46f, hw * 0.04f, hgt * 0.40f, len * 0.46f, 0, hgt * 0.48f, len * 0.52f);
        for (int side = -1; side <= 1; side += 2)
        {
            float cx = side * hw * 0.34f;
            w.TriMat(panel, cx - hw * 0.05f, hgt * 0.14f, len * 0.30f, cx + hw * 0.05f, hgt * 0.14f, len * 0.30f, cx, hgt * 0.20f, len * 0.36f);
        }
    }

    private static void AddRadiantRepairAntennaBlooms(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var solar = RaceMeshWriter.HullMaterial.Solar;
        for (int a = 0; a < 2; a++)
        {
            float z = len * (0.08f + a * 0.070f);
            AddIntegratedBulb(w, 0, hgt * (0.72f + a * 0.06f), z, hw * 0.06f, hgt * 0.10f, 5);
            w.TriMat(solar, -hw * 0.03f, hgt * (0.78f + a * 0.04f), z, hw * 0.03f, hgt * (0.78f + a * 0.04f), z,
                0, hgt * (0.86f + a * 0.04f), z + len * 0.008f);
        }
        w.TriMat(hull, -hw * 0.02f, hgt * 0.86f, len * 0.16f, hw * 0.02f, hgt * 0.86f, len * 0.16f, 0, hgt * 0.96f, len * 0.22f);
        for (int side = -1; side <= 1; side += 2)
        {
            float xBoom = side * hw * 0.62f;
            w.Tri(side * hw * 0.10f, hgt * 0.42f, len * 0.06f, xBoom, hgt * 0.52f, len * 0.18f, xBoom, hgt * 0.38f, len * 0.22f);
            w.TriMat(solar, xBoom, hgt * 0.48f, len * 0.12f, xBoom + side * hw * 0.04f, hgt * 0.54f, len * 0.14f, xBoom, hgt * 0.42f, len * 0.16f);
        }
    }

    private static void BuildRadiantDeckGeneric(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float l = len * 0.9f;
        float span = wid * 1.35f;
        float deckH = hgt * 0.28f;
        int sections = HullSections(hullKey, 10, 6, 14);

        AddLoftedHull(w, l, wid * 0.55f, hgt * 0.75f, sections,
            t => wid * (0.22f + 0.18f * MathF.Sin(t * MathF.PI) + (t > 0.78f ? (1f - t) / 0.22f * 0.08f : 0f)),
            t => deckH * (0.55f + 0.35f * MathF.Sin(t * MathF.PI)),
            -0.55f, 0.65f);

        float deckZ0 = -l * 0.38f;
        float deckZ1 = l * 0.38f;
        w.Tri(-span * 0.5f, deckH, deckZ1, span * 0.5f, deckH, deckZ1, 0, deckH * 1.55f, deckZ0 + l * 0.08f);
        w.Tri(-span * 0.5f, deckH, deckZ1, 0, deckH * 1.55f, deckZ0 + l * 0.08f, -span * 0.5f, deckH, deckZ0);
        w.Tri(span * 0.5f, deckH, deckZ1, span * 0.5f, deckH, deckZ0, 0, deckH * 1.55f, deckZ0 + l * 0.08f);
        w.Tri(-span * 0.5f, deckH * 0.92f, deckZ1, span * 0.5f, deckH * 0.92f, deckZ1, span * 0.5f, deckH * 0.92f, deckZ0);
        w.Tri(-span * 0.5f, deckH * 0.92f, deckZ1, span * 0.5f, deckH * 0.92f, deckZ0, -span * 0.5f, deckH * 0.92f, deckZ0);
        w.Tri(-span * 0.5f, deckH * 0.92f, deckZ1, -span * 0.5f, deckH, deckZ1, span * 0.5f, deckH, deckZ1);
        w.Tri(-span * 0.5f, deckH * 0.92f, deckZ0, span * 0.5f, deckH, deckZ0, -span * 0.5f, deckH, deckZ0);
        w.Tri(span * 0.5f, deckH * 0.92f, deckZ0, span * 0.5f, deckH, deckZ0, span * 0.5f, deckH, deckZ1);
        w.Tri(span * 0.5f, deckH * 0.92f, deckZ1, span * 0.5f, deckH, deckZ1, span * 0.5f, deckH, deckZ0);

        int panelRows = hullKey is "dreadnought" or "carrier" or "carrier_command" ? 3 : 2;
        int panelCols = hullKey is "dreadnought" or "carrier" or "carrier_command" ? 5 : 3;
        for (int row = 0; row < panelRows; row++)
        {
            for (int col = 0; col < panelCols; col++)
            {
                float u = col / (float)Math.Max(1, panelCols - 1);
                float v = row / (float)Math.Max(1, panelRows - 1);
                float x = MathHelper.Lerp(-span * 0.42f, span * 0.42f, u);
                float z0 = MathHelper.Lerp(deckZ0, deckZ1 - l * 0.08f, v);
                float z1 = z0 + l * 0.1f;
                float yPeak = deckH * (1.35f + v * 0.45f);
                w.Tri(x - span * 0.05f, deckH, z0, x + span * 0.05f, deckH, z0, x, yPeak, z1);
                w.Tri(x - span * 0.05f, deckH, z0, x, yPeak, z1, x - span * 0.04f, deckH, z1);
                w.Tri(x + span * 0.05f, deckH, z0, x + span * 0.04f, deckH, z1, x, yPeak, z1);
            }
        }

        AddLoftedHull(w, l * 0.28f, wid * 0.32f, hgt, 6,
            t => wid * 0.1f * (1f - t * 0.4f),
            t => deckH + hgt * (0.45f + t * 0.55f),
            0.12f, 0.48f);
    }

    private static void AddRadiantFuselagePod(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial mat, float l, float bw, float bh, float deckH,
        int sections, float zStart, float zEnd)
    {
        AddLoftedHullMat(w, mat, l, bw, bh, sections,
            t => bw * (0.40f + 0.22f * MathF.Sin(t * MathF.PI) + (t > 0.78f ? (1f - t) / 0.22f * 0.06f : 0f)),
            t => deckH * (0.52f + 0.32f * MathF.Sin(t * MathF.PI)),
            zStart, zEnd);
    }

    private static void AddRadiantSolarCrownDeck(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial panel,
        RaceMeshWriter.HullMaterial accent, float l, float span, float deckH, int panelRows, int panelCols,
        float deckZ0Factor, float deckZ1Factor)
    {
        float deckZ0 = -l * deckZ0Factor;
        float deckZ1 = l * deckZ1Factor;
        w.TriMat(hull, -span * 0.5f, deckH, deckZ1, span * 0.5f, deckH, deckZ1, 0, deckH * 1.55f, deckZ0 + l * 0.08f);
        w.TriMat(hull, -span * 0.5f, deckH, deckZ1, 0, deckH * 1.55f, deckZ0 + l * 0.08f, -span * 0.5f, deckH, deckZ0);
        w.TriMat(hull, span * 0.5f, deckH, deckZ1, span * 0.5f, deckH, deckZ0, 0, deckH * 1.55f, deckZ0 + l * 0.08f);
        w.TriMat(panel, -span * 0.5f, deckH * 0.92f, deckZ1, span * 0.5f, deckH * 0.92f, deckZ1, span * 0.5f, deckH * 0.92f, deckZ0);
        w.TriMat(panel, -span * 0.5f, deckH * 0.92f, deckZ1, span * 0.5f, deckH * 0.92f, deckZ0, -span * 0.5f, deckH * 0.92f, deckZ0);

        for (int row = 0; row < panelRows; row++)
        {
            for (int col = 0; col < panelCols; col++)
            {
                float u = col / (float)Math.Max(1, panelCols - 1);
                float v = row / (float)Math.Max(1, panelRows - 1);
                float x = MathHelper.Lerp(-span * 0.42f, span * 0.42f, u);
                float z0 = MathHelper.Lerp(deckZ0, deckZ1 - l * 0.08f, v);
                float z1 = z0 + l * 0.1f;
                float yPeak = deckH * (1.35f + v * 0.45f);
                w.TriMat(accent, x - span * 0.05f, deckH, z0, x + span * 0.05f, deckH, z0, x, yPeak, z1);
                w.TriMat(panel, x - span * 0.05f, deckH, z0, x, yPeak, z1, x - span * 0.04f, deckH, z1);
                w.TriMat(panel, x + span * 0.05f, deckH, z0, x + span * 0.04f, deckH, z1, x, yPeak, z1);
            }
        }
    }

    /// <summary>Narrow dorsal solar keel embossed panel band along +Z — radiant loft read without wing widen.</summary>
    private static void AddRadiantDorsalSolarKeelBand(RaceMeshWriter w, float hw, float deckH, float z0, float z1, int segs = 3)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        for (int k = 0; k < segs; k++)
        {
            float t = k / MathF.Max(1f, segs - 1);
            float z = MathHelper.Lerp(z0, z1, t);
            float halfK = hw * MathHelper.Lerp(0.05f, 0.025f, t);
            var mat = k == segs - 1 ? accent : panel;
            w.TriMat(mat,
                -halfK, deckH * (1.14f + t * 0.10f), z, halfK, deckH * (1.14f + t * 0.10f), z,
                0, deckH * (1.24f + t * 0.08f), z + (z1 - z0) * 0.02f);
        }
    }

    /// <summary>Modest forward bow elongation — re-anchors nose toward hull +Z envelope bound.</summary>
    private static void AddRadiantBowElongation(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial accent,
        float halfW, float peakH, float baseZ, float tipExtension)
    {
        float tipZ = baseZ + tipExtension;
        w.TriMat(hull, -halfW, peakH * 0.16f, baseZ, halfW, peakH * 0.16f, baseZ, 0, peakH * 0.92f, tipZ);
        w.TriMat(accent, -halfW * 0.45f, peakH * 0.68f, baseZ + tipExtension * 0.40f,
            halfW * 0.45f, peakH * 0.68f, baseZ + tipExtension * 0.40f, 0, peakH * 0.86f, tipZ);
    }

    private static void AddRadiantBowProbe(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial accent,
        float halfW, float peakH, float baseZ, float tipExtension)
    {
        float tipZ = baseZ + tipExtension;
        w.TriMat(hull, -halfW, peakH * 0.20f, baseZ, halfW, peakH * 0.20f, baseZ, 0, peakH, tipZ);
        w.TriMat(accent, -halfW * 0.5f, peakH * 0.70f, baseZ + tipExtension * 0.35f,
            halfW * 0.5f, peakH * 0.70f, baseZ + tipExtension * 0.35f, 0, peakH * 0.84f, tipZ);
    }

    private static void AddRadiantDorsalCrownSpine(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial accent, RaceMeshWriter.HullMaterial panel,
        float hw, float deckH, float l, float zStartFactor, float zEndFactor, int segments)
    {
        for (int s = 0; s < segments; s++)
        {
            float t = s / MathF.Max(1f, segments - 1);
            float z = MathHelper.Lerp(l * zStartFactor, l * zEndFactor, t);
            float halfSpine = hw * 0.04f;
            w.TriMat(accent, -halfSpine, deckH * (1.18f + t * 0.10f), z,
                halfSpine, deckH * (1.18f + t * 0.10f), z,
                0, deckH * (1.28f + t * 0.10f), z + l * 0.005f);
            if (s % 2 == 0)
                w.TriMat(panel, -halfSpine * 1.4f, deckH * (1.12f + t * 0.08f), z - l * 0.003f,
                    halfSpine * 1.4f, deckH * (1.12f + t * 0.08f), z - l * 0.003f,
                    0, deckH * (1.22f + t * 0.09f), z);
        }
    }

    private static void AddRadiantSweptSolarFin(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial accent, RaceMeshWriter.HullMaterial panel,
        int side, float l, float hw, float deckH, float sweep, float reach)
    {
        float xRoot = side * hw * 0.22f;
        float xTip = side * hw * (0.22f + reach);
        float zRoot = l * 0.06f;
        float zTip = zRoot - l * sweep;
        w.TriMat(accent, xRoot, deckH * 0.96f, zRoot, xTip, deckH * 1.04f, zTip, xRoot, deckH * 0.88f, zRoot - l * 0.02f);
        w.TriMat(panel, xRoot, deckH * 0.90f, zRoot - l * 0.01f, xTip, deckH * 0.98f, zTip, xRoot, deckH * 0.84f, zRoot - l * 0.03f);
    }

    // â”€â”€ Spiny (Voidborn) â€” carapace spine hulls with surface-mounted spines â”€â”€â”€â”€â”€â”€

    private const float VoidbornAsym = 0.45f;
    private const float VoidbornWingSweep = 0.70f;

    private static void BuildVoidbornHull(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        switch (hullKey)
        {
            case "fighter":
            case "fighter_basic":
                BuildVoidbornFighter(w, len, wid, hgt);
                return;
            case "hero":
            case "hero_default":
                BuildVoidbornHero(w, len, wid, hgt);
                return;
            case "scout":
            case "scout_light":
                BuildVoidbornScout(w, len, wid, hgt);
                return;
            case "interceptor":
            case "interceptor_mk2":
                BuildVoidbornInterceptor(w, len, wid, hgt);
                return;
            case "drone":
            case "drone_swarm":
                BuildVoidbornDrone(w, len, wid, hgt);
                return;
            case "corvette":
            case "corvette_fast":
                BuildVoidbornCorvetteFast(w, len, wid, hgt);
                return;
            case "frigate":
            case "frigate_strike":
                BuildVoidbornFrigateStrike(w, len, wid, hgt);
                return;
            case "bomber":
            case "bomber_heavy":
                BuildVoidbornBomberHeavy(w, len, wid, hgt);
                return;
            case "gunship":
            case "gunship_heavy":
                BuildVoidbornGunshipHeavy(w, len, wid, hgt);
                return;
            case "destroyer":
            case "destroyer_assault":
                BuildVoidbornDestroyerAssault(w, len, wid, hgt);
                return;
            case "cruiser":
            case "cruiser_heavy":
                BuildSpinyCruiserHeavy(w, len, wid, hgt);
                return;
            case "carrier":
            case "carrier_command":
                BuildSpinyCarrierCommand(w, len, wid, hgt);
                return;
            case "dreadnought":
                BuildSpinyDreadnought(w, len, wid, hgt);
                return;
            default:
                BuildSpinyHull(w, hullKey, len, wid, hgt);
                break;
        }
    }

    private static void AddVoidbornCarapaceCore(
        RaceMeshWriter w, float l, float hw, float hh, int sections, float widthScale, float skewScale)
    {
        float asym = VoidbornAsym * skewScale;
        AddLoftedHull(w, l, hw * 2f * widthScale, hh, sections,
            t => hw * widthScale * (0.36f + 0.26f * MathF.Sin(t * MathF.PI) + (t > 0.78f ? (1f - t) / 0.22f * 0.08f : 0f)),
            t => hh * (0.40f + 0.50f * MathF.Sin(t * MathF.PI)),
            -0.54f, 0.72f,
            t => hw * asym * MathF.Sin(t * MathF.PI * 1.2f));
    }

    private static void AddVoidbornPlatingBands(RaceMeshWriter w, float l, float hw, float hh, int bands, float skewScale)
    {
        float asym = VoidbornAsym * skewScale;
        for (int b = 0; b < bands; b++)
        {
            float t = b / (float)Math.Max(1, bands - 1);
            float z = MathHelper.Lerp(-l * 0.48f, l * 0.52f, t);
            float skew = hw * asym * MathF.Sin(t * MathF.PI * 1.2f);
            AddEmbossedRing(w, hw * 0.82f, hh * 0.78f, z, skew, hh * 0.05f, 3);
        }
    }

    private static void AddVoidbornBowTip(RaceMeshWriter w, RaceMeshWriter.HullMaterial mat, float hw, float hh, float bowZ, float extension)
    {
        var accent = RaceMeshWriter.HullMaterial.Solar;
        w.TriMat(mat, -hw * 0.07f, hh * 0.40f, bowZ, hw * 0.07f, hh * 0.40f, bowZ, 0, hh * 0.52f, bowZ + extension);
        w.TriMat(accent, -hw * 0.04f, hh * 0.44f, bowZ + extension * 0.35f, hw * 0.04f, hh * 0.44f, bowZ + extension * 0.35f,
            0, hh * 0.50f, bowZ + extension);
    }

    /// <summary>Embossed dorsal needle keel — spiny exile crown spine band for compact/reference craft.</summary>
    private static void AddVoidbornDorsalNeedleKeel(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial membrane,
        RaceMeshWriter.HullMaterial accent, float hw, float hh, float l, string hullKey)
    {
        float keelHalfW = hw * (hullKey switch
        {
            "scout_light" => 0.032f,
            "interceptor_mk2" => 0.036f,
            "fighter_basic" => 0.038f,
            "drone_swarm" => 0.026f,
            "hero_default" => 0.042f,
            _ => 0.034f
        });
        float yBase = hh * 0.44f;
        float yCrest = hh * (hullKey is "hero_default" ? 0.60f : hullKey is "scout_light" ? 0.56f : 0.54f);
        float zEnd = l * (hullKey switch
        {
            "scout_light" => 0.90f,
            "interceptor_mk2" => 0.88f,
            "fighter_basic" => 0.86f,
            "drone_swarm" => 0.84f,
            "hero_default" => 0.88f,
            _ => 0.84f
        });
        int keelSegs = hullKey is "drone_swarm" ? 2 : hullKey is "hero_default" ? 3 : 2;
        for (int i = 0; i < keelSegs; i++)
        {
            float t = i / MathF.Max(1f, keelSegs - 1);
            float z = MathHelper.Lerp(l * 0.52f, zEnd, t);
            w.TriMat(hull, -keelHalfW, yBase + hh * t * 0.04f, z, keelHalfW, yBase + hh * t * 0.04f, z, 0, yCrest, z + l * 0.012f);
            if (i == keelSegs - 1)
                w.TriMat(accent, -keelHalfW * 0.55f, yCrest * 0.94f, z + l * 0.005f,
                    keelHalfW * 0.55f, yCrest * 0.94f, z + l * 0.005f, 0, yCrest, z + l * 0.012f);
        }
    }

    private static void AddVoidbornSpineFin(
        RaceMeshWriter w, int side, float hw, float hh, float l, float span, float sweep, float zAnchor)
    {
        var panel = RaceMeshWriter.HullMaterial.Truss;
        float xRoot = side * hw * 0.20f;
        float xTip = side * hw * span;
        float zTip = zAnchor - sweep * hw * 0.50f;
        w.TriMat(panel, xRoot, hh * 0.30f, zAnchor, xTip, hh * 0.16f, zTip, xRoot, hh * 0.10f, zAnchor - l * 0.03f);
        w.TriMat(panel, xRoot, hh * 0.10f, zAnchor - l * 0.03f, xTip, hh * 0.16f, zTip, xTip * 0.80f, hh * 0.06f, zTip - l * 0.02f);
    }

    private static void AddVoidbornSpineRidge(
        RaceMeshWriter w, int side, float hw, float hh, float l, float reach, float sweep, float zAnchor)
    {
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float xRoot = side * hw * 0.18f;
        float xTip = side * hw * (0.18f + reach);
        float zTip = zAnchor - l * sweep;
        w.TriMat(accent, xRoot, hh * 0.58f, zAnchor, xTip, hh * 0.72f, zTip, xRoot, hh * 0.48f, zAnchor - l * 0.02f);
        w.TriMat(accent, xRoot, hh * 0.48f, zAnchor - l * 0.02f, xTip, hh * 0.72f, zTip, xRoot, hh * 0.42f, zAnchor - l * 0.04f);
    }

    private static void AddVoidbornSurfaceSpines(RaceMeshWriter w, float l, float hw, float hh, int count, float zMin, float zMax)
    {
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)Math.Max(1, count - 1);
            float z = MathHelper.Lerp(zMin, zMax, t);
            float side = (i % 2 == 0 ? -1 : 1) * hw * (0.20f + (i % 3) * 0.07f);
            float baseY = hh * (0.26f + 0.10f * MathF.Sin(t * MathF.PI));
            float tipY = hh * (0.68f + (i % 3) * 0.08f);
            AddSurfaceSpine(w, side * 0.16f, baseY, z, side, tipY, z + hw * 0.04f, side * 0.20f, baseY * 0.72f, z - hw * 0.03f);
        }
    }

    /// <summary>Lateral carapace booms — widen spiny aspect toward ~0.74 (reach &gt;1 spans past half-width).</summary>
    private static void AddVoidbornAspectWingBooms(RaceMeshWriter w, float l, float hw, float hh, float reach = 2.15f)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var membrane = RaceMeshWriter.HullMaterial.Truss;
        float z0 = -l * 0.10f;
        float z1 = l * 0.14f;
        for (int side = -1; side <= 1; side += 2)
        {
            float xIn = side * hw * 0.18f;
            float xOut = side * hw * reach;
            w.TriMat(hull, xIn, hh * 0.20f, z0, xOut, hh * 0.16f, z1, xIn, hh * 0.06f, z0 - l * 0.03f);
            w.TriMat(membrane, xOut, hh * 0.16f, z1, xOut, hh * 0.08f, z0 - l * 0.05f, xIn, hh * 0.06f, z0 - l * 0.03f);
            AddBoxMat(w, hull, xOut - side * hw * 0.05f, xOut + side * hw * 0.03f, hh * 0.08f, hh * 0.18f, z0, z1);
        }
    }

    /// <summary>Fast corvette — swept spine wedge, offset carapace pods, modest +Z bow.</summary>
    private static void BuildVoidbornCorvetteFast(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float l = len * 0.98f;
        float hw = wid * 0.50f;
        float hh = hgt * 1.18f;

        AddVoidbornAspectWingBooms(w, l, hw, hh, 1.12f);
        AddVoidbornCarapaceCore(w, l, hw, hh, 5, 0.90f, 1.08f);
        AddVoidbornPlatingBands(w, l, hw, hh, 2, 1.08f);
        AddVoidbornSpineFin(w, -1, hw, hh, l, 0.82f, VoidbornWingSweep, l * 0.04f);
        AddVoidbornSpineFin(w, 1, hw, hh, l, 0.74f, VoidbornWingSweep * 0.82f, l * 0.02f);
        for (int p = 0; p < 2; p++)
        {
            float side = (p == 0 ? -1f : 1f) * hw * 0.36f;
            AddLoftedHull(w, l * 0.14f, hw * 0.10f, hh * 0.44f, 2,
                t => hw * 0.05f * (1f - t * 0.3f),
                t => hh * (0.52f + t * 0.28f),
                0.04f + p * 0.06f, 0.14f + p * 0.06f,
                t => side * 0.04f);
        }
        AddBoxMat(w, weapon, -hw * 0.48f, -hw * 0.34f, hh * 0.04f, hh * 0.14f, l * 0.02f, l * 0.10f);
        AddBoxMat(w, weapon, hw * 0.34f, hw * 0.48f, hh * 0.04f, hh * 0.14f, l * 0.02f, l * 0.10f);
        w.TriMat(accent, -hw * 0.05f, hh * 0.48f, l * 0.16f, hw * 0.05f, hh * 0.48f, l * 0.16f, 0, hh * 0.54f, l * 0.20f);
        AddBoxMat(w, engine, -hw * 0.12f, hw * 0.12f, 0, hh * 0.14f, -l * 0.44f, -l * 0.34f);
    }

    /// <summary>Strike frigate — balanced gun-pod carapace blooms with dorsal spine ridge.</summary>
    private static void BuildVoidbornFrigateStrike(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var panel = RaceMeshWriter.HullMaterial.Truss;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float l = len * 1.02f;
        float hw = wid * 0.50f;
        float hh = hgt * 1.22f;

        AddVoidbornAspectWingBooms(w, l, hw, hh, 1.10f);
        AddVoidbornCarapaceCore(w, l, hw, hh, 5, 0.94f, 1.08f);
        AddVoidbornPlatingBands(w, l, hw, hh, 2, 1.08f);
        for (int p = 0; p < 2; p++)
        {
            float side = (p == 0 ? -1f : 1f) * hw * 0.34f;
            AddLoftedHull(w, l * 0.14f, hw * 0.12f, hh * 0.48f, 2,
                t => hw * 0.08f,
                t => hh * (0.56f + t * 0.28f),
                0.02f + p * 0.06f, 0.14f + p * 0.06f,
                t => side * 0.05f);
            AddBoxMat(w, weapon, side - hw * 0.08f, side + hw * 0.08f, hh * 0.06f, hh * 0.18f, l * 0.02f, l * 0.12f);
        }
        AddBoxMat(w, weapon, -hw * 0.10f, hw * 0.10f, hh * 0.06f, hh * 0.18f, l * 0.20f, l * 0.30f);
        AddBoxMat(w, engine, -hw * 0.14f, hw * 0.14f, 0, hh * 0.16f, -l * 0.46f, -l * 0.36f);
        w.TriMat(panel, -hw * 0.08f, hh * 0.44f, l * 0.14f, hw * 0.08f, hh * 0.44f, l * 0.14f, 0, hh * 0.52f, l * 0.20f);
    }

    /// <summary>Heavy bomber — wide payload sac, dorsal spine ridge, modest elongation.</summary>
    private static void BuildVoidbornBomberHeavy(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float l = len * 1.02f;
        float hw = wid * 0.54f;
        float hh = hgt * 1.16f;

        AddVoidbornCarapaceCore(w, l, hw, hh, 8, 1.08f, 1.06f);
        AddLoftedHull(w, l * 0.52f, hw * 0.26f, hh * 0.66f, 3,
            t => hw * 0.10f * (1f - t * 0.25f),
            t => hh * (0.48f + t * 0.34f),
            0.06f, 0.54f);
        AddVoidbornPlatingBands(w, l, hw, hh, 3, 1.06f);
        w.TriMat(weapon, -hw * 0.26f, 0, -l * 0.02f, hw * 0.26f, 0, -l * 0.02f, 0, hh * 0.10f, l * 0.06f);
        AddVoidbornSurfaceSpines(w, l, hw, hh, 2, -l * 0.06f, l * 0.22f);
        for (int p = 0; p < 2; p++)
        {
            float side = (p == 0 ? -1f : 1f) * hw * 0.36f;
            AddLoftedHull(w, l * 0.14f, hw * 0.10f, hh * 0.44f, 3,
                t => hw * 0.05f,
                t => hh * (0.50f + t * 0.22f),
                0.04f, 0.12f,
                t => side * 0.04f);
        }
        w.TriMat(hull, -hw * 0.10f, hh * 0.22f, l * 0.64f, hw * 0.10f, hh * 0.22f, l * 0.64f, 0, hh * 0.36f, l * 0.70f);
        AddBoxMat(w, engine, -hw * 0.16f, hw * 0.16f, 0, hh * 0.14f, -l * 0.48f, -l * 0.38f);
        w.TriMat(accent, -hw * 0.06f, hh * 0.44f, l * 0.08f, hw * 0.06f, hh * 0.44f, l * 0.08f, 0, hh * 0.50f, l * 0.12f);
    }

    /// <summary>Heavy gunship — chin weapon pod mass, lateral spines, assault carapace frame.</summary>
    private static void BuildVoidbornGunshipHeavy(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float l = len * 1.04f;
        float hw = wid * 0.52f;
        float hh = hgt * 0.94f;

        AddVoidbornCarapaceCore(w, l, hw, hh, 8, 1.04f, 1.04f);
        AddVoidbornPlatingBands(w, l, hw, hh, 3, 1.04f);
        w.TriMat(weapon, -hw * 0.14f, 0, l * 0.20f, hw * 0.14f, 0, l * 0.20f, 0, hh * 0.06f, l * 0.36f);
        AddBoxMat(w, weapon, -hw * 0.42f, -hw * 0.28f, hh * 0.02f, hh * 0.10f, l * 0.04f, l * 0.12f);
        AddBoxMat(w, weapon, hw * 0.28f, hw * 0.42f, hh * 0.02f, hh * 0.10f, l * 0.04f, l * 0.12f);
        for (int p = 0; p < 2; p++)
        {
            float side = (p == 0 ? -1f : 1f) * hw * 0.30f;
            AddVoidbornSpineRidge(w, p == 0 ? -1 : 1, hw, hh, l, 0.10f, 0.03f, l * 0.04f);
            AddSurfaceSpine(w, side * 0.18f, hh * 0.24f, l * 0.02f, side, hh * 0.50f, l * 0.08f, side * 0.22f, hh * 0.18f, -l * 0.02f);
        }
        AddBoxMat(w, engine, -hw * 0.14f, hw * 0.14f, 0, hh * 0.12f, -l * 0.44f, -l * 0.34f);
        w.TriMat(panel, -hw * 0.18f, hh * 0.22f, -l * 0.02f, hw * 0.18f, hh * 0.22f, -l * 0.02f, 0, hh * 0.30f, l * 0.08f);
    }

    /// <summary>Assault destroyer — dorsal spine ridge, carapace ring stacks, multi-pod envelope.</summary>
    private static void BuildVoidbornDestroyerAssault(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float l = len * 0.96f;
        float hw = wid * 0.56f;
        float hh = hgt * 1.08f;

        AddVoidbornAspectWingBooms(w, l, hw, hh, 1.02f);
        AddVoidbornCarapaceCore(w, l, hw, hh, 5, 0.82f, 1.06f);
        AddLoftedHull(w, l * 0.44f, hw * 0.16f, hh * 0.72f, 2,
            t => hw * 0.06f * (1f - t * 0.22f),
            t => hh * (0.58f + t * 0.38f),
            0.16f, 0.68f);
        AddVoidbornPlatingBands(w, l, hw, hh, 2, 1.10f);
        for (int p = 0; p < 2; p++)
        {
            float side = (p == 0 ? -1f : 1f) * hw * 0.30f;
            float z = l * (p == 0 ? 0.08f : 0.20f);
            AddLoftedHull(w, l * 0.10f, hw * 0.08f, hh * 0.40f, 2,
                t => hw * 0.05f,
                t => hh * (0.52f + t * 0.24f),
                z / l - 0.04f, z / l + 0.04f,
                t => side * 0.04f);
        }
        AddHullRing(w, hw * 0.18f, hh * 0.48f, -l * 0.14f, 3);
        AddBoxMat(w, weapon, -hw * 0.10f, hw * 0.10f, hh * 0.08f, hh * 0.18f, l * 0.46f, l * 0.58f);
        AddBoxMat(w, engine, -hw * 0.16f, hw * 0.16f, 0, hh * 0.16f, -l * 0.50f, -l * 0.38f);
        w.TriMat(hull, -hw * 0.08f, hh * 0.28f, l * 0.64f, hw * 0.08f, hh * 0.28f, l * 0.64f, 0, hh * 0.40f, l * 0.68f);
    }

    /// <summary>Slim needle-spine pod scout profile.</summary>
    private static void BuildVoidbornScout(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float l = len * 0.94f;
        float hw = wid * 0.50f;
        float hh = hgt * 0.88f;
        float bowZ = len * 0.88f;
        float bowExt = len * 0.06f;

        AddVoidbornAspectWingBooms(w, l, hw, hh, 1.06f);
        AddVoidbornCarapaceCore(w, l, hw, hh, 3, 0.88f, 0.82f);
        AddVoidbornPlatingBands(w, l, hw, hh, 2, 0.82f);
        AddLoftedHull(w, l * 0.18f, hw * 0.42f, hh * 0.72f, 2,
            t => hw * 0.08f * (1f - t * 0.3f),
            t => hh * (0.52f + t * 0.42f),
            0.10f, 0.28f,
            t => hw * VoidbornAsym * 0.48f);
        AddVoidbornSurfaceSpines(w, l, hw, hh, 1, -l * 0.28f, l * 0.36f);
        AddVoidbornBowTip(w, hull, hw, hh, bowZ, bowExt);
        AddVoidbornDorsalNeedleKeel(w, hull, panel, accent, hw, hh, l, "scout_light");
        AddSurfaceSpine(w, hw * VoidbornAsym * 0.12f, hh * 0.52f, bowZ * 0.92f,
            hw * 0.04f, hh * 0.68f, bowZ + bowExt * 0.82f,
            hw * VoidbornAsym * 0.08f, hh * 0.46f, bowZ * 0.78f);
        AddIntegratedBulb(w, hw * 0.28f, hh * 0.42f, l * 0.06f, hw * 0.12f, hh * 0.16f, 3);
        for (int e = 0; e < 2; e++)
        {
            float x = (e - 0.5f) * hw * 0.16f + hw * VoidbornAsym * 0.08f;
            AddBoxMat(w, engine, x - hw * 0.05f, x + hw * 0.05f, 0, hh * 0.14f, -l * 0.30f, -l * 0.22f);
        }
        AddBoxMat(w, weapon, hw * 0.34f - hw * 0.05f, hw * 0.34f + hw * 0.05f, hh * 0.08f, hh * 0.16f, l * 0.02f, l * 0.10f);
        AddBoxMat(w, shield, -hw * 0.06f, hw * 0.06f, hh * 0.62f, hh * 0.74f, l * 0.04f, l * 0.12f);
        w.TriMat(panel, hw * 0.18f, hh * 0.48f, l * 0.04f, hw * 0.30f, hh * 0.36f, l * 0.02f, hw * 0.24f, hh * 0.56f, l * 0.08f);
        w.TriMat(accent, hw * 0.16f, hh * 0.50f, l * 0.06f, hw * 0.28f, hh * 0.42f, l * 0.02f, hw * 0.22f, hh * 0.58f, l * 0.10f);
    }

    /// <summary>Balanced carapace fighter with lateral spine fins.</summary>
    private static void BuildVoidbornFighter(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float l = len * 0.94f;
        float hw = wid * 0.50f;
        float hh = hgt * 0.92f;
        float bowZ = len * 0.88f;
        float bowExt = len * 0.06f;

        AddVoidbornAspectWingBooms(w, l, hw, hh, 1.04f);
        AddVoidbornCarapaceCore(w, l, hw, hh, 3, 0.86f, 1.0f);
        AddVoidbornPlatingBands(w, l, hw, hh, 2, 1.0f);
        AddVoidbornSpineFin(w, -1, hw, hh, l, 0.88f, VoidbornWingSweep, l * 0.06f);
        AddVoidbornSpineFin(w, 1, hw, hh, l, 0.82f, VoidbornWingSweep * 0.88f, l * 0.04f);
        AddVoidbornSurfaceSpines(w, l, hw, hh, 2, -l * 0.34f, l * 0.38f);
        AddVoidbornBowTip(w, hull, hw, hh, bowZ, bowExt);
        AddVoidbornDorsalNeedleKeel(w, hull, panel, accent, hw, hh, l, "fighter_basic");
        for (int e = 0; e < 2; e++)
        {
            float x = (e - 0.5f) * hw * 0.18f + hw * VoidbornAsym * 0.06f;
            AddBoxMat(w, engine, x - hw * 0.06f, x + hw * 0.06f, 0, hh * 0.16f, -l * 0.36f, -l * 0.26f);
        }
        for (int side = -1; side <= 1; side += 2)
            AddBoxMat(w, weapon, side * hw * 0.40f - hw * 0.06f, side * hw * 0.40f + hw * 0.06f,
                hh * 0.08f, hh * 0.16f, l * 0.02f, l * 0.10f);
        AddBoxMat(w, shield, -hw * 0.08f, hw * 0.08f, hh * 0.66f, hh * 0.78f, l * 0.06f, l * 0.14f);
        w.TriMat(panel, -hw * 0.06f, hh * 0.44f, l * 0.04f, hw * 0.06f, hh * 0.44f, l * 0.04f, 0, hh * 0.54f, l * 0.12f);
    }

    /// <summary>Aggressive swept spine-ridge interceptor profile.</summary>
    private static void BuildVoidbornInterceptor(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float l = len * 0.94f;
        float hw = wid * 0.50f;
        float hh = hgt * 0.90f;
        float bowZ = len * 0.88f;
        float bowExt = len * 0.05f;

        AddVoidbornAspectWingBooms(w, l, hw, hh, 1.06f);
        AddVoidbornCarapaceCore(w, l, hw, hh, 3, 0.84f, 0.94f);
        AddVoidbornPlatingBands(w, l, hw, hh, 1, 0.94f);
        AddVoidbornSpineRidge(w, 1, hw, hh, l, 0.82f, VoidbornWingSweep * 0.14f, l * 0.14f);
        AddVoidbornSpineFin(w, 1, hw, hh, l, 0.92f, VoidbornWingSweep * 1.10f, l * 0.14f);
        AddVoidbornSurfaceSpines(w, l, hw, hh, 1, -l * 0.30f, l * 0.36f);
        AddVoidbornBowTip(w, hull, hw, hh, bowZ, bowExt);
        AddVoidbornDorsalNeedleKeel(w, hull, RaceMeshWriter.HullMaterial.Truss, accent, hw, hh, l, "interceptor_mk2");
        AddBoxMat(w, weapon, -hw * 0.08f, hw * 0.08f, hh * 0.10f, hh * 0.20f, l * 0.28f, l * 0.38f);
        for (int side = -1; side <= 1; side += 2)
            AddBoxMat(w, weapon, side * hw * 0.38f - hw * 0.05f, side * hw * 0.38f + hw * 0.05f,
                hh * 0.08f, hh * 0.16f, l * 0.02f, l * 0.10f);
        for (int e = 0; e < 2; e++)
        {
            float x = (e - 0.5f) * hw * 0.14f;
            AddBoxMat(w, engine, x - hw * 0.05f, x + hw * 0.05f, 0, hh * 0.14f, -l * 0.34f, -l * 0.24f);
        }
        AddBoxMat(w, shield, -hw * 0.06f, hw * 0.06f, hh * 0.64f, hh * 0.76f, l * 0.04f, l * 0.12f);
        w.TriMat(accent, hw * 0.28f, hh * 0.46f, l * 0.08f, hw * 0.46f, hh * 0.30f, l * 0.04f, hw * 0.36f, hh * 0.54f, l * 0.14f);
    }

    /// <summary>Compact spine-cluster pod drone profile.</summary>
    private static void BuildVoidbornDrone(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Truss;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float l = len * 0.92f;
        float hw = wid * 0.50f;
        float hh = hgt * 0.72f;
        float bowZ = l * 0.52f;
        float bowExt = l * 0.06f;

        AddVoidbornAspectWingBooms(w, l, hw, hh, 1.04f);
        AddVoidbornCarapaceCore(w, l, hw, hh, 3, 0.72f, 0.74f);
        AddVoidbornPlatingBands(w, l, hw, hh, 2, 0.74f);
        float[] podX = [hw * 0.20f, -hw * 0.12f];
        float[] podZ = [-l * 0.04f, l * 0.08f];
        for (int p = 0; p < podX.Length; p++)
            AddIntegratedBulb(w, podX[p], hh * (0.28f + p * 0.06f), podZ[p], hw * (0.11f - p * 0.02f), hh * 0.13f, 3);
        AddVoidbornSurfaceSpines(w, l, hw, hh, 1, -l * 0.18f, l * 0.28f);
        AddVoidbornBowTip(w, hull, hw, hh, bowZ, bowExt);
        AddVoidbornDorsalNeedleKeel(w, hull, panel, RaceMeshWriter.HullMaterial.Solar, hw, hh, l, "drone_swarm");
        AddBoxMat(w, engine, -hw * 0.10f, hw * 0.10f, 0, hh * 0.14f, -l * 0.26f, -l * 0.18f);
        for (int side = -1; side <= 1; side += 2)
            AddBoxMat(w, weapon, side * hw * 0.22f - hw * 0.05f, side * hw * 0.22f + hw * 0.05f,
                hh * 0.08f, hh * 0.14f, l * 0.12f, l * 0.20f);
        AddBoxMat(w, shield, -hw * 0.05f, hw * 0.05f, hh * 0.58f, hh * 0.68f, l * 0.04f, l * 0.12f);
        w.TriMat(panel, -hw * 0.03f, hh * 0.34f, -l * 0.02f, hw * 0.03f, hh * 0.34f, -l * 0.02f, 0, hh * 0.42f, l * 0.06f);
    }

    /// <summary>Exile crown bloom hero with asymmetric dorsal spines.</summary>
    private static void BuildVoidbornHero(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float l = len * 0.92f;
        float hw = wid * 0.50f;
        float hh = hgt * 0.86f;
        float bowZ = len * 0.86f;
        float bowExt = len * 0.05f;

        AddVoidbornAspectWingBooms(w, l, hw, hh, 1.05f);
        AddVoidbornCarapaceCore(w, l, hw, hh, 3, 0.82f, 1.04f);
        AddVoidbornPlatingBands(w, l, hw, hh, 1, 1.04f);
        AddVoidbornSpineFin(w, -1, hw, hh, l, 0.84f, VoidbornWingSweep, l * 0.08f);
        AddLoftedHull(w, l * 0.20f, hw * 0.42f, hh * 0.86f, 2,
            t => hw * 0.12f * (1f - t * 0.22f),
            t => hh * (0.58f + t * 0.40f),
            0.18f, 0.34f,
            t => hw * VoidbornAsym * 0.38f);
        AddVoidbornBowTip(w, hull, hw, hh, bowZ, bowExt);
        AddVoidbornDorsalNeedleKeel(w, hull, RaceMeshWriter.HullMaterial.Truss, accent, hw, hh, l, "hero_default");
        for (int e = 0; e < 2; e++)
        {
            float x = (e - 0.5f) * hw * 0.20f + hw * VoidbornAsym * 0.10f;
            AddBoxMat(w, engine, x - hw * 0.06f, x + hw * 0.06f, 0, hh * 0.16f, -l * 0.38f, -l * 0.26f);
        }
        for (int side = -1; side <= 1; side += 2)
            AddBoxMat(w, weapon, side * hw * 0.42f - hw * 0.06f, side * hw * 0.42f + hw * 0.06f,
                hh * 0.08f, hh * 0.16f, l * 0.04f, l * 0.12f);
        AddBoxMat(w, shield, -hw * 0.08f, hw * 0.08f, hh * 0.72f, hh * 0.86f, l * 0.10f, l * 0.20f);
        w.TriMat(accent, -hw * 0.18f, hh * 0.76f, l * 0.14f, hw * 0.06f, hh * 0.76f, l * 0.14f, hw * 0.02f, hh * 0.90f, l * 0.20f);
    }

    /// <summary>Heavy cruiser — balanced carapace mass with dorsal spine bloom ridge.</summary>
    private static void BuildSpinyCruiserHeavy(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float l = len * 0.90f;
        float hw = wid * 0.58f;
        float hh = hgt * 0.72f;
        AddVoidbornCarapaceCore(w, l, hw, hh, 4, 0.72f, 1.0f);
        AddVoidbornPlatingBands(w, l, hw, hh, 2, 1.0f);
        AddLoftedHull(w, l * 0.28f, hw * 0.26f, hh * 0.92f, 2,
            t => hw * 0.10f * (1f - t * 0.30f),
            t => hh * (0.58f + t * 0.48f),
            0.14f, 0.50f,
            t => hw * VoidbornAsym * 0.22f * MathF.Sin(t * MathF.PI));
        for (int p = 0; p < 2; p++)
        {
            float side = (p == 0 ? -1f : 1f) * hw * 0.38f;
            float z = MathHelper.Lerp(-l * 0.06f, l * 0.20f, p);
            AddIntegratedBulb(w, side, hh * 0.38f, z, hw * 0.14f, hh * 0.18f, 3);
        }
        float ridgeZ = l * 0.18f;
        w.TriMat(accent, -hw * 0.08f, hh * 1.18f, ridgeZ, hw * 0.08f, hh * 1.18f, ridgeZ, 0, hh * 1.34f, ridgeZ + l * 0.05f);
        w.TriMat(panel, -hw * 0.10f, hh * 1.08f, ridgeZ - l * 0.03f, hw * 0.10f, hh * 1.08f, ridgeZ - l * 0.03f, 0, hh * 1.18f, ridgeZ);
        AddBoxMat(w, weapon, -hw * 0.10f, hw * 0.10f, hh * 0.10f, hh * 0.18f, l * 0.56f, l * 0.66f);
        w.TriMat(hull, -hw * 0.08f, hh * 0.28f, l * 0.66f, hw * 0.08f, hh * 0.28f, l * 0.66f, 0, hh * 0.44f, l * 0.72f);
        AddSpinyCapitalEngineWells(w, len, wid, hgt, 2);
    }

    /// <summary>Command carrier — flat deck/hangar spine cues with lateral mass bias.</summary>
    private static void BuildSpinyCarrierCommand(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var membrane = RaceMeshWriter.HullMaterial.Radiator;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float l = len * 1.06f;
        float hw = wid * 0.50f;
        float hh = hgt * 0.74f;
        float deckH = hh * 0.56f;
        float bowZ = l * 0.96f;
        AddVoidbornCarapaceCore(w, l, hw, hh, 5, 0.56f, 1.02f);
        AddVoidbornPlatingBands(w, l, hw, hh, 2, 1.02f);
        float hangarZ0 = -l * 0.28f;
        float hangarZ1 = l * 0.12f;
        w.TriMat(membrane, -hw * 0.28f, deckH, hangarZ1, hw * 0.28f, deckH, hangarZ1, 0, deckH * 1.04f, hangarZ0 + l * 0.05f);
        w.TriMat(membrane, -hw * 0.28f, deckH, hangarZ1, 0, deckH * 1.04f, hangarZ0 + l * 0.05f, -hw * 0.28f, deckH, hangarZ0);
        w.TriMat(membrane, hw * 0.28f, deckH, hangarZ1, hw * 0.28f, deckH, hangarZ0, 0, deckH * 1.04f, hangarZ0 + l * 0.05f);
        for (int h = 0; h < 3; h++)
        {
            float z = MathHelper.Lerp(hangarZ0 + l * 0.04f, hangarZ1 - l * 0.04f, h / 2f);
            w.TriMat(accent, -hw * 0.16f, deckH * 0.94f, z, hw * 0.16f, deckH * 0.94f, z, 0, deckH, z + l * 0.008f);
        }
        for (int p = 0; p < 2; p++)
        {
            float side = (p == 0 ? -1f : 1f) * hw * 0.26f + hw * VoidbornAsym * 0.06f;
            float z = MathHelper.Lerp(-l * 0.04f, l * 0.12f, p);
            AddIntegratedBulb(w, side, hh * 0.32f, z, hw * 0.10f, hh * 0.14f, 3);
        }
        AddLoftedHull(w, l * 0.22f, hw * 0.18f, hh * 0.78f, 2,
            t => hw * 0.06f * (1f - t * 0.30f),
            t => deckH + hh * (0.38f + t * 0.34f),
            0.12f, 0.46f,
            t => hw * VoidbornAsym * 0.22f);
        AddVoidbornBowTip(w, hull, hw, hh, bowZ, len * 0.06f);
        AddBoxMat(w, weapon, -hw * 0.18f, hw * 0.18f, hh * 0.12f, hh * 0.20f, -l * 0.04f, l * 0.04f);
        w.TriMat(hull, -hw * 0.06f, hh * 0.24f, bowZ, hw * 0.06f, hh * 0.24f, bowZ, 0, hh * 0.30f, l * 0.98f);
        for (int k = 0; k < 3; k++)
        {
            float z = MathHelper.Lerp(l * 0.08f, l * 0.44f, k / 2f);
            w.TriMat(membrane, -hw * 0.04f, deckH * 0.88f, z, hw * 0.04f, deckH * 0.88f, z, 0, deckH * 0.94f, z + l * 0.008f);
        }
        AddSpinyCapitalEngineWells(w, len, wid, hgt, 2);
    }

    /// <summary>Dreadnought — imposing prow spine cluster with embossed ridge stacks.</summary>
    private static void BuildSpinyDreadnought(RaceMeshWriter w, float len, float wid, float hgt)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float l = len;
        float hw = wid * 0.50f;
        float hh = hgt * 0.72f;
        float bowZ = len * 0.90f;

        AddVoidbornCarapaceCore(w, l, hw, hh, 8, 0.52f, 0.98f);
        AddVoidbornPlatingBands(w, l, hw, hh, 2, 0.98f);
        float prowZ = l * 0.58f;
        for (int p = 0; p < 2; p++)
        {
            float side = (p == 0 ? -1f : 1f) * hw * 0.22f;
            AddIntegratedBulb(w, side, hh * 0.40f, prowZ, hw * 0.12f, hh * 0.14f, 3);
        }
        w.TriMat(weapon, 0, hh * 0.44f, prowZ + l * 0.04f, -hw * 0.06f, hh * 0.38f, prowZ, hw * 0.06f, hh * 0.38f, prowZ);
        for (int stack = 0; stack < 2; stack++)
        {
            float z = MathHelper.Lerp(l * 0.38f, l * 0.50f, stack);
            AddBoxMat(w, panel, -hw * 0.10f, hw * 0.10f, hh * 0.34f + stack * hh * 0.06f, hh * 0.44f + stack * hh * 0.06f, z, z + l * 0.05f);
        }
        AddLoftedHull(w, l * 0.28f, hw * 0.22f, hh * 0.82f, 2,
            t => hw * 0.08f * (1f - t * 0.32f),
            t => hh * (0.52f + t * 0.38f),
            0.22f, 0.52f,
            t => hw * VoidbornAsym * 0.14f * MathF.Sin(t * MathF.PI));
        AddVoidbornSurfaceSpines(w, l, hw, hh, 1, -l * 0.18f, l * 0.34f);
        AddVoidbornBowTip(w, hull, hw, hh, bowZ, len * 0.06f);
        AddBoxMat(w, weapon, -hw * 0.10f, hw * 0.10f, hh * 0.08f, hh * 0.16f, l * 0.62f, l * 0.72f);
        w.TriMat(hull, -hw * 0.06f, hh * 0.22f, bowZ, hw * 0.06f, hh * 0.22f, bowZ, 0, hh * 0.34f, len * 0.96f);
        AddSpinyCapitalEngineWells(w, len, wid, hgt, 2);
    }

    private static void AddSpinyCapitalEngineWells(RaceMeshWriter w, float len, float wid, float hgt, int count)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        float hw = wid * 0.5f;
        count = Math.Clamp(count, 2, 2);
        for (int i = 0; i < count; i++)
        {
            float t = i;
            float cx = MathHelper.Lerp(-hw * 0.24f, hw * 0.24f, t);
            float halfW = hw * 0.11f;
            AddBoxMat(w, hull, cx - halfW, cx + halfW, 0, hgt * 0.20f, -len * 0.30f, -len * 0.14f);
            AddBoxMat(w, engine, cx - halfW * 0.52f, cx + halfW * 0.52f, hgt * 0.02f, hgt * 0.16f, -len * 0.32f, -len * 0.18f);
        }
    }

    private static void BuildSpinyHull(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        if (hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk" or "support_repair")
        {
            BuildSpinyUtilitySolid(w, hullKey, len, wid, hgt);
            return;
        }

        float l = len;
        int sections = HullSections(hullKey, 12, 7, 16);

        AddLoftedHull(w, l, wid * 0.72f, hgt, sections,
            t => wid * (0.18f + 0.14f * MathF.Sin(t * MathF.PI) + (t > 0.8f ? (1f - t) / 0.2f * 0.06f : 0f)),
            t => hgt * (0.38f + 0.42f * MathF.Sin(t * MathF.PI)),
            -0.52f, 0.68f);

        AddLoftedHull(w, l * 0.55f, wid * 0.42f, hgt * 0.85f, sections - 2,
            t => wid * 0.1f * (0.6f + 0.4f * MathF.Sin(t * MathF.PI)),
            t => hgt * (0.5f + 0.35f * MathF.Sin(t * MathF.PI)),
            -0.35f, 0.42f);

        int spines = hullKey is "dreadnought" or "carrier" ? 6 : hullKey is "drone" or "scout" ? 4 : 5;
        for (int i = 0; i < spines; i++)
        {
            float t = i / (float)Math.Max(1, spines - 1);
            float z = MathHelper.Lerp(-l * 0.4f, l * 0.42f, t);
            float side = (i % 2 == 0 ? -1 : 1) * wid * (0.22f + (i % 3) * 0.08f);
            float baseY = hgt * (0.28f + 0.12f * MathF.Sin(t * MathF.PI));
            float tipY = hgt * (0.72f + (i % 3) * 0.1f);
            AddSurfaceSpine(w, side * 0.18f, baseY, z, side, tipY, z + wid * 0.05f, side * 0.22f, baseY * 0.7f, z - wid * 0.04f);
        }

        if (hullKey is not "drone" and not "scout")
        {
            AddLoftedHull(w, l * 0.2f, wid * 0.35f, hgt * 1.1f, 5,
                t => wid * 0.06f,
                t => hgt * (0.65f + t * 0.45f),
                0.35f, 0.58f);
        }
    }

    /// <summary>Industrial/logistics voidborn hulls — lofted carapace base with per-role spine blooms.</summary>
    private static void BuildSpinyUtilitySolid(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float widthScale = hullKey is "freighter_bulk" ? 0.88f : hullKey is "miner_eva" ? 0.90f : 0.94f;
        float l = len * (hullKey switch
        {
            "freighter_bulk" => 1.00f,
            "transport_cargo" => 1.00f,
            "miner_basic" => 0.96f,
            "miner_eva" => 0.94f,
            "miner_tractor" => 0.95f,
            "support_repair" => 0.96f,
            _ => 0.92f
        });
        float hw = wid * 0.58f * widthScale;
        float hh = hgt * (hullKey is "support_repair" ? 0.68f : 0.72f);


        int sections = hullKey is "freighter_bulk" or "transport_cargo" ? 6 : 7;
        float bowZ = hullKey switch
        {
            "freighter_bulk" => 0.62f,
            "transport_cargo" => 0.64f,
            "miner_basic" => 0.56f,
            "miner_eva" => 0.52f,
            "miner_tractor" => 0.54f,
            "support_repair" => 0.60f,
            _ => 0.52f
        };
        float asym = 0.18f;

        AddLoftedHull(w, l, hw * 2f, hh, sections,
            t => hw * (0.36f + 0.18f * MathF.Sin(t * MathF.PI) + (t > 0.78f ? (1f - t) / 0.22f * 0.07f : 0f)),
            t => hh * (0.40f + 0.46f * MathF.Sin(t * MathF.PI)),
            -0.46f, bowZ,
            t => hw * asym * MathF.Sin(t * MathF.PI * 1.1f));

        AddLoftedHull(w, l * 0.30f, hw * 0.74f, hh * 0.86f, 3,
            t => hw * 0.11f * (1f - t * 0.38f),
            t => hh * (0.50f + t * 0.36f),
            0.06f, bowZ * 0.76f,
            t => hw * asym * 0.30f * MathF.Sin(t * MathF.PI));

        int spines = hullKey is "freighter_bulk" ? 2 : hullKey is "support_repair" ? 2 : 2;
        for (int i = 0; i < spines; i++)
        {
            float t = i / (float)Math.Max(1, spines - 1);
            float z = MathHelper.Lerp(-l * 0.34f, l * bowZ * 0.62f, t);
            float side = (i % 2 == 0 ? -1 : 1) * hw * (0.20f + (i % 3) * 0.06f) + asym * sideSign(i);
            float baseY = hh * (0.26f + 0.10f * MathF.Sin(t * MathF.PI));
            float tipY = hh * (0.66f + (i % 3) * 0.08f);
            AddSurfaceSpine(w, side * 0.16f, baseY, z, side, tipY, z + hw * 0.04f, side * 0.20f, baseY * 0.72f, z - hw * 0.03f);
        }

        int rings = hullKey is "freighter_bulk" or "transport_cargo" ? 2 : 3;
        for (int r = 0; r < rings; r++)
        {
            float t = r / (float)Math.Max(1, rings - 1);
            float z = MathHelper.Lerp(-l * 0.28f, l * bowZ * 0.44f, t);
            float skew = hw * asym * MathF.Sin(t * MathF.PI * 1.2f);
            AddEmbossedRing(w, hw * 0.82f, hh * 0.78f, z, skew, hh * 0.05f, 4);
        }

        switch (hullKey)
        {
            case "miner_basic": AddSpinyMiningToolPods(w, hw, hh, l); break;
            case "miner_eva": AddSpinyEvaPodBloom(w, hw, hh, l); break;
            case "miner_tractor": AddSpinyTractorBloom(w, hw, hh, l); break;
            case "transport_cargo": AddSpinyCargoSpineSac(w, hw, hh, l); break;
            case "freighter_bulk": AddSpinyBulkCarapaceHull(w, hw, hh, l); break;
            case "support_repair": AddSpinyRepairAntennaBlooms(w, hw, hh, l); break;
        }

    }

    private static float sideSign(int i) => (i % 2 == 0 ? -1f : 1f);

    private static void AddSpinyMiningToolPods(RaceMeshWriter w, float hw, float hh, float len)
    {
        var carapace = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        w.TriMat(carapace, -hw * 0.06f, hh * 0.24f, len * 0.50f, hw * 0.06f, hh * 0.24f, len * 0.50f, 0, hh * 0.34f, len * 0.56f);
        for (int side = -1; side <= 1; side += 2)
        {
            float xRoot = side * hw * 0.28f;
            float xTip = side * hw * 0.68f;
            float zTip = len * 0.54f;
            w.TriMat(weapon, xRoot, hh * 0.12f, len * 0.08f, xTip, hh * 0.18f, zTip, xRoot, hh * 0.06f, zTip - len * 0.02f);
            w.TriMat(accent, xTip, hh * 0.18f, zTip, xTip + side * hw * 0.04f, hh * 0.24f, zTip + len * 0.02f, xTip, hh * 0.10f, zTip + len * 0.03f);
            AddIntegratedBulb(w, xTip, hh * 0.20f, zTip, hw * 0.10f, hh * 0.12f, 3);
        }
    }

    private static void AddSpinyEvaPodBloom(RaceMeshWriter w, float hw, float hh, float len)
    {
        var carapace = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        AddIntegratedBulb(w, hw * 0.36f, hh * 0.48f, len * 0.10f, hw * 0.28f, hh * 0.17f, 4);
        w.TriMat(carapace, -hw * 0.05f, hh * 0.26f, len * 0.44f, hw * 0.05f, hh * 0.26f, len * 0.44f, 0, hh * 0.36f, len * 0.50f);
        w.TriMat(accent, hw * 0.42f, hh * 0.40f, len * 0.12f, hw * 0.50f, hh * 0.46f, len * 0.16f, hw * 0.44f, hh * 0.34f, len * 0.14f);
        AddLoftedHull(w, len * 0.10f, hw * 0.06f, hh * 0.42f, 3,
            _ => hw * 0.025f,
            t => hh * (0.40f + t * 0.08f),
            0.04f, 0.18f);
    }

    private static void AddSpinyTractorBloom(RaceMeshWriter w, float hw, float hh, float len)
    {
        var carapace = RaceMeshWriter.HullMaterial.Hull;
        AddIntegratedBulb(w, 0, hh * 0.64f, len * 0.36f, hw * 0.22f, hh * 0.12f, 4);
        w.TriMat(carapace, -hw * 0.05f, hh * 0.28f, len * 0.42f, hw * 0.05f, hh * 0.28f, len * 0.42f, 0, hh * 0.38f, len * 0.48f);
        for (int side = -1; side <= 1; side += 2)
        {
            float xTip = side * hw * 0.58f;
            w.Tri(side * hw * 0.10f, hh * 0.20f, len * 0.06f, xTip, hh * 0.26f, len * 0.14f, xTip, hh * 0.16f, len * 0.18f);
            AddIntegratedBulb(w, xTip, hh * 0.28f, len * 0.12f, hw * 0.07f, hh * 0.09f, 3);
        }
    }

    private static void AddSpinyCargoSpineSac(RaceMeshWriter w, float hw, float hh, float len)
    {
        for (int s = 0; s < 2; s++)
        {
            float t = s;
            float z = MathHelper.Lerp(-len * 0.02f, len * 0.46f, t);
            AddIntegratedBulb(w, 0, hh * 0.46f + t * hh * 0.08f, z, hw * MathHelper.Lerp(0.11f, 0.04f, t), hh * 0.11f, 4);
        }
        AddLoftedHull(w, len * 0.26f, hw * 0.08f, hh * 0.52f, 3,
            _ => hw * 0.032f,
            t => hh * (0.44f + t * 0.12f),
            0.02f, 0.40f);
        for (int side = -1; side <= 1; side += 2)
        {
            float cx = side * hw * 0.34f;
            AddIntegratedBulb(w, cx, hh * 0.24f, len * 0.28f, hw * 0.07f, hh * 0.09f, 3);
        }
    }

    private static void AddSpinyBulkCarapaceHull(RaceMeshWriter w, float hw, float hh, float len)
    {
        var carapace = RaceMeshWriter.HullMaterial.Hull;
        for (int b = 0; b < 2; b++)
        {
            float z = len * (0.06f + b * 0.11f);
            AddIntegratedBulb(w, 0, hh * 0.12f, z, hw * 0.22f, hh * 0.09f, 4);
        }
        w.TriMat(carapace, -hw * 0.06f, hh * 0.34f, len * 0.32f, hw * 0.06f, hh * 0.34f, len * 0.32f, 0, hh * 0.42f, len * 0.40f);
        for (int side = -1; side <= 1; side += 2)
            AddIntegratedBulb(w, side * hw * 0.32f, hh * 0.24f, len * 0.30f, hw * 0.10f, hh * 0.10f, 4);
    }

    private static void AddSpinyRepairAntennaBlooms(RaceMeshWriter w, float hw, float hh, float len)
    {
        var carapace = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        for (int a = 0; a < 2; a++)
            AddIntegratedBulb(w, 0, hh * (0.72f + a * 0.05f), len * (0.06f + a * 0.06f), hw * 0.05f, hh * 0.09f, 3);
        w.TriMat(carapace, -hw * 0.02f, hh * 0.86f, len * 0.14f, hw * 0.02f, hh * 0.86f, len * 0.14f, 0, hh * 0.96f, len * 0.18f);
        w.TriMat(accent, -hw * 0.015f, hh * 0.92f, len * 0.16f, hw * 0.015f, hh * 0.92f, len * 0.16f, 0, hh * 1.00f, len * 0.20f);
        for (int side = -1; side <= 1; side += 2)
        {
            float xBoom = side * hw * 0.58f;
            w.Tri(side * hw * 0.09f, hh * 0.40f, len * 0.06f, xBoom, hh * 0.50f, len * 0.14f, xBoom, hh * 0.36f, len * 0.18f);
        }
    }

    // ── Crystalline (Cryo) — closed faceted gem hull with mid-band and stern ─────

    /// <summary>Slim needle-facet scout spine — readable forward probe role.</summary>
    private static void BuildCryoScout(RaceMeshWriter w, float len, float wid, float hgt, float sharpness)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float hw = wid * 0.5f;
        float bh = hgt * 1.18f;
        float bowBase = len * 0.94f;
        float bowTip = len;
        float sternZ = -len * 0.06f;

        AddCrystallineAspectFacetBooms(w, len, hw, bh, 1.05f);
        AddCrystallineEnvelopeGemCore(w, len, wid, bh, sharpness, 2, 0.98f);
        AddCrystallineDorsalFacetKeel(w, hull, panel, accent, hw, bh, len, "scout_light");
        AddCrystallineBowFacet(w, hull, accent, hw * 0.06f, bh * 0.46f, bowBase, len * 0.06f);
        AddCrystallineBowElongation(w, hull, accent, hw * 0.05f, bh * 0.50f, bowBase, len * 0.06f);

        for (int side = -1; side <= 1; side += 2)
        {
            AddCrystallineFacetWing(w, panel, accent, side, len, hw, bh, 0.16f, 0.92f);
            AddBoxMat(w, weapon, side * hw * 0.40f - hw * 0.05f, side * hw * 0.40f + hw * 0.05f,
                bh * 0.08f, bh * 0.16f, len * 0.72f, len * 0.82f);
        }
        for (int e = 0; e < 2; e++)
        {
            float x = (e - 0.5f) * hw * 0.14f;
            AddBoxMat(w, engine, x - hw * 0.04f, x + hw * 0.04f, 0, bh * 0.12f, sternZ - len * 0.02f, sternZ + len * 0.04f);
        }
        AddBoxMat(w, shield, -hw * 0.05f, hw * 0.05f, bh * 0.66f, bh * 0.76f, len * 0.70f, len * 0.78f);
        w.TriMat(panel, -hw * 0.04f, bh * 0.40f, len * 0.68f, hw * 0.04f, bh * 0.40f, len * 0.68f, 0, bh * 0.50f, len * 0.76f);
    }

    /// <summary>Balanced gem-wing prism fighter profile.</summary>
    private static void BuildCryoFighter(RaceMeshWriter w, float len, float wid, float hgt, float sharpness)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float hw = wid * 0.5f;
        float bh = hgt * 1.26f;
        float bowBase = len * 0.94f;
        float sternZ = -len * 0.06f;

        AddCrystallineAspectFacetBooms(w, len, hw, bh, 1.02f);
        AddCrystallineEnvelopeGemCore(w, len, wid, bh, sharpness, 2, 0.98f);
        AddCrystallineDorsalFacetKeel(w, hull, panel, accent, hw, bh, len, "fighter_basic");
        AddCrystallineBowFacet(w, hull, accent, hw * 0.10f, bh * 0.44f, bowBase, len * 0.06f);
        AddCrystallineBowElongation(w, hull, accent, hw * 0.08f, bh * 0.48f, bowBase, len * 0.05f);

        for (int side = -1; side <= 1; side += 2)
            AddCrystallineFacetWing(w, panel, accent, side, len, hw, bh, 0.18f, 0.46f);
        for (int e = 0; e < 2; e++)
        {
            float x = (e - 0.5f) * hw * 0.16f;
            AddBoxMat(w, engine, x - hw * 0.05f, x + hw * 0.05f, 0, bh * 0.14f, sternZ - len * 0.02f, sternZ + len * 0.04f);
        }
        for (int side = -1; side <= 1; side += 2)
            AddBoxMat(w, weapon, side * hw * 0.44f - hw * 0.06f, side * hw * 0.44f + hw * 0.06f,
                bh * 0.08f, bh * 0.16f, len * 0.68f, len * 0.78f);
        AddBoxMat(w, shield, -hw * 0.07f, hw * 0.07f, bh * 0.68f, bh * 0.80f, len * 0.70f, len * 0.78f);
    }

    /// <summary>Aggressive swept facet-fin interceptor profile.</summary>
    private static void BuildCryoInterceptor(RaceMeshWriter w, float len, float wid, float hgt, float sharpness)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float hw = wid * 0.5f;
        float bh = hgt * 1.22f;
        float bowBase = len;
        float sternZ = -len * 0.02f;

        AddCrystallineAspectFacetBooms(w, len, hw, bh, 1.10f);
        AddCrystallineEnvelopeGemCore(w, len, wid, bh, sharpness, 1, 0.58f);
        AddCrystallineDorsalFacetKeel(w, hull, panel, accent, hw, bh, len, "interceptor_mk2");
        AddCrystallineBowFacet(w, hull, accent, hw * 0.07f, bh * 0.42f, bowBase, len * 0.08f);
        AddCrystallineBowElongation(w, hull, accent, hw * 0.05f, bh * 0.46f, bowBase, len * 0.08f);
        w.TriMat(hull, -hw * 0.03f, bh * 0.12f, sternZ, hw * 0.03f, bh * 0.12f, sternZ, 0, bh * 0.22f, sternZ - len * 0.03f);

        for (int side = -1; side <= 1; side += 2)
            AddCrystallineSweptFacetFin(w, panel, accent, side, len, hw, bh, 0.18f, 0.36f);
        AddBoxMat(w, weapon, -hw * 0.07f, hw * 0.07f, bh * 0.10f, bh * 0.20f, len * 0.78f, len * 0.88f);
        for (int side = -1; side <= 1; side += 2)
            AddBoxMat(w, weapon, side * hw * 0.40f - hw * 0.05f, side * hw * 0.40f + hw * 0.05f,
                bh * 0.08f, bh * 0.16f, len * 0.68f, len * 0.78f);
        for (int e = 0; e < 2; e++)
        {
            float x = (e - 0.5f) * hw * 0.14f;
            AddBoxMat(w, engine, x - hw * 0.05f, x + hw * 0.05f, 0, bh * 0.14f, sternZ - len * 0.02f, sternZ + len * 0.04f);
        }
        AddBoxMat(w, shield, -hw * 0.06f, hw * 0.06f, bh * 0.64f, bh * 0.76f, len * 0.70f, len * 0.78f);
    }

    /// <summary>Compact cluster crystal-pod drone profile.</summary>
    private static void BuildCryoDrone(RaceMeshWriter w, float len, float wid, float hgt, float sharpness)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float hw = wid * 0.5f;
        float bh = hgt * 1.08f;
        float bowBase = len * 0.94f;
        float sternZ = -len * 0.06f;

        AddCrystallineAspectFacetBooms(w, len, hw, bh, 1.0f);
        AddCrystallineEnvelopeGemCore(w, len, wid, bh, sharpness, 2, 0.94f);
        AddCrystallineDorsalFacetKeel(w, hull, panel, accent, hw, bh, len, "drone_swarm");
        AddCrystallineBowFacet(w, hull, accent, hw * 0.05f, bh * 0.34f, bowBase, len * 0.06f);
        AddCrystallineBowElongation(w, hull, accent, hw * 0.04f, bh * 0.38f, bowBase, len * 0.06f);
        w.TriMat(hull, -hw * 0.03f, bh * 0.10f, sternZ, hw * 0.03f, bh * 0.10f, sternZ, 0, bh * 0.18f, sternZ - len * 0.03f);

        float[] podZ = [len * 0.72f, len * 0.86f];
        for (int p = 0; p < podZ.Length; p++)
            AddCrystallineCrystalPod(w, hull, accent, 0, bh * (0.28f + p * 0.06f), podZ[p], hw * (0.10f - p * 0.02f), bh * 0.10f, 3);

        AddBoxMat(w, engine, -hw * 0.10f, hw * 0.10f, 0, bh * 0.12f, sternZ - len * 0.02f, sternZ + len * 0.04f);
        for (int side = -1; side <= 1; side += 2)
            AddBoxMat(w, weapon, side * hw * 0.24f - hw * 0.05f, side * hw * 0.24f + hw * 0.05f,
                bh * 0.08f, bh * 0.14f, len * 0.76f, len * 0.86f);
        AddBoxMat(w, shield, -hw * 0.05f, hw * 0.05f, bh * 0.56f, bh * 0.66f, len * 0.70f, len * 0.78f);
    }

    /// <summary>Command apex bloom hero with facet crown.</summary>
    private static void BuildCryoHero(RaceMeshWriter w, float len, float wid, float hgt, float sharpness)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        float hw = wid * 0.5f;
        float bh = hgt * 1.30f;
        float bowBase = len * 0.94f;
        float sternZ = -len * 0.06f;

        AddCrystallineAspectFacetBooms(w, len, hw, bh, 1.0f);
        AddCrystallineEnvelopeGemCore(w, len, wid, bh, sharpness, 2, 0.96f);
        AddCrystallineFacetCrown(w, hull, panel, accent, hw, bh, len);
        AddCrystallineDorsalFacetKeel(w, hull, panel, accent, hw, bh, len, "hero_default");
        w.TriMat(accent, -hw * 0.04f, bh * 0.78f, len * 0.82f, hw * 0.04f, bh * 0.78f, len * 0.82f, 0, bh * 0.86f, len * 0.88f);
        AddCrystallineBowFacet(w, hull, accent, hw * 0.10f, bh * 0.46f, bowBase, len * 0.06f);
        AddCrystallineBowElongation(w, hull, accent, hw * 0.08f, bh * 0.50f, bowBase, len * 0.05f);

        for (int side = -1; side <= 1; side += 2)
            AddCrystallineFacetWing(w, panel, accent, side, len, hw, bh, 0.18f, 0.44f);
        for (int e = 0; e < 2; e++)
        {
            float x = (e - 0.5f) * hw * 0.18f;
            AddBoxMat(w, engine, x - hw * 0.05f, x + hw * 0.05f, 0, bh * 0.14f, sternZ - len * 0.02f, sternZ + len * 0.04f);
        }
        for (int side = -1; side <= 1; side += 2)
            AddBoxMat(w, weapon, side * hw * 0.46f - hw * 0.06f, side * hw * 0.46f + hw * 0.06f,
                bh * 0.08f, bh * 0.16f, len * 0.68f, len * 0.78f);
        AddBoxMat(w, shield, -hw * 0.08f, hw * 0.08f, bh * 0.72f, bh * 0.84f, len * 0.70f, len * 0.78f);
    }

    private static void AddCrystallineGemCore(
        RaceMeshWriter w, float l, float hw, float bh, float sharpness, int bandCount)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        int facets = Math.Clamp(8 + (int)(sharpness * 8), 8, 18);
        float apex = bh * (1.1f + sharpness * 0.4f);
        float midY = bh * 0.42f;
        float sternZ = -l * 0.52f;

        for (int i = 0; i < facets; i++)
        {
            float a0 = MathF.PI * 2f * i / facets;
            float a1 = MathF.PI * 2f * (i + 1) / facets;
            float r0 = hw * (0.64f + 0.12f * MathF.Sin(i * 1.7f));
            float r1 = hw * (0.64f + 0.12f * MathF.Sin((i + 1) * 1.7f));
            float x0 = MathF.Cos(a0) * r0;
            float x1 = MathF.Cos(a1) * r1;
            float z0 = MathF.Sin(a0) * l * 0.22f;
            float z1 = MathF.Sin(a1) * l * 0.22f;

            w.TriMat(hull, 0, apex, l * 0.22f, x0, midY, z0, x1, midY, z1);
            w.TriMat(panel, x0, midY, z0, x1, midY, z1, x0 * 0.65f, 0, z0 * 0.65f);
            w.TriMat(panel, x1, midY, z1, x1 * 0.65f, 0, z1 * 0.65f, x0 * 0.65f, 0, z0 * 0.65f);
            w.TriMat(hull, x0 * 0.65f, 0, z0 * 0.65f, x1 * 0.65f, 0, z1 * 0.65f, 0, 0, sternZ);
        }

        for (int b = 0; b < bandCount; b++)
        {
            float t = b / MathF.Max(1f, bandCount - 1);
            float z = MathHelper.Lerp(-l * 0.15f, l * 0.35f, t);
            float r = hw * (0.56f + 0.06f * MathF.Sin(b * 2.1f));
            AddHullRing(w, r, midY * 0.85f, z, facets / 2);
        }
    }

    private static void AddCrystallineBowFacet(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial accent,
        float halfW, float baseY, float bowZ, float extension)
    {
        w.TriMat(accent, -halfW, baseY, bowZ, halfW, baseY, bowZ, 0, baseY + halfW * 1.4f, bowZ + extension);
        w.TriMat(hull, -halfW * 1.1f, baseY * 0.92f, bowZ - extension * 0.25f,
            halfW * 1.1f, baseY * 0.92f, bowZ - extension * 0.25f, 0, baseY + halfW * 0.8f, bowZ);
    }

    /// <summary>Modest +Z bow elongation — re-anchors nose toward hull envelope +Z bound (tip ≤ len×0.06).</summary>
    private static void AddCrystallineBowElongation(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial accent,
        float halfW, float peakH, float baseZ, float tipExtension)
    {
        float tipZ = baseZ + tipExtension;
        w.TriMat(hull, -halfW, peakH * 0.18f, baseZ, halfW, peakH * 0.18f, baseZ, 0, peakH * 0.90f, tipZ);
        w.TriMat(accent, -halfW * 0.45f, peakH * 0.66f, baseZ + tipExtension * 0.38f,
            halfW * 0.45f, peakH * 0.66f, baseZ + tipExtension * 0.38f, 0, peakH * 0.84f, tipZ);
    }

    /// <summary>Envelope-anchored gem core — bow near +len, stern near −Z, width toward hull bound.</summary>
    private static void AddCrystallineEnvelopeGemCore(
        RaceMeshWriter w, float len, float wid, float bh, float sharpness, int bandCount, float widthScale)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        float hw = wid * 0.5f * widthScale;
        int facets = Math.Clamp(6 + (int)(sharpness * 2), 6, 8);
        float apex = MathF.Min(bh, bh * (0.96f + sharpness * 0.18f));
        float midY = bh * 0.40f;
        float bowZ = len * 0.94f;
        float sternZ = -len * 0.14f;
        float zSpread = len * 0.16f;

        for (int i = 0; i < facets; i++)
        {
            float a0 = MathF.PI * 2f * i / facets;
            float a1 = MathF.PI * 2f * (i + 1) / facets;
            float r0 = hw * (0.88f + 0.08f * MathF.Sin(i * 1.7f));
            float r1 = hw * (0.88f + 0.08f * MathF.Sin((i + 1) * 1.7f));
            float x0 = MathF.Cos(a0) * r0;
            float x1 = MathF.Cos(a1) * r1;
            float z0 = bowZ + MathF.Sin(a0) * zSpread;
            float z1 = bowZ + MathF.Sin(a1) * zSpread;

            w.TriMat(hull, 0, apex, bowZ, x0, midY, z0, x1, midY, z1);
            w.TriMat(panel, x0, midY, z0, x1, midY, z1, x0 * 0.62f, 0, z0 * 0.62f + sternZ * 0.38f);
            w.TriMat(panel, x1, midY, z1, x1 * 0.62f, 0, z1 * 0.62f + sternZ * 0.38f, x0 * 0.62f, 0, z0 * 0.62f + sternZ * 0.38f);
            w.TriMat(hull, x0 * 0.62f, 0, z0 * 0.62f + sternZ * 0.38f, x1 * 0.62f, 0, z1 * 0.62f + sternZ * 0.38f, 0, 0, sternZ);
        }

        for (int b = 0; b < bandCount; b++)
        {
            float t = b / MathF.Max(1f, bandCount - 1);
            float z = MathHelper.Lerp(sternZ + len * 0.04f, bowZ, t);
            float r = hw * (0.78f + 0.05f * MathF.Sin(b * 2.1f));
            AddHullRing(w, r, midY * 0.85f, z, Math.Min(4, facets / 2));
        }
    }

    /// <summary>Embossed dorsal facet keel — compact craft crystal spine or medium-combat panel band.</summary>
    private static void AddCrystallineDorsalFacetKeel(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial panel,
        RaceMeshWriter.HullMaterial accent, float hw, float bh, float len, string hullKey)
    {
        bool isCompact = hullKey is "scout" or "scout_light" or "fighter" or "fighter_basic"
            or "interceptor" or "interceptor_mk2" or "drone" or "drone_swarm"
            or "hero" or "hero_default";
        if (!isCompact)
        {
            int segments = hullKey switch
            {
                "destroyer_assault" => 6,
                "frigate_strike" => 5,
                "gunship_heavy" => 5,
                "bomber_heavy" => 4,
                "corvette_fast" => 4,
                _ => 5
            };
            float medZEnd = hullKey switch
            {
                "destroyer_assault" => 0.78f,
                "frigate_strike" => 0.76f,
                "gunship_heavy" => 0.74f,
                "bomber_heavy" => 0.72f,
                _ => 0.74f
            };
            for (int s = 0; s < segments; s++)
            {
                float t = s / MathF.Max(1f, segments - 1);
                float z = MathHelper.Lerp(len * 0.10f, len * medZEnd, t);
                float halfSpine = hw * (0.05f + t * 0.02f);
                var mat = s % 2 == 0 ? accent : panel;
                w.TriMat(mat, -halfSpine, bh * (0.48f + t * 0.06f), z,
                    halfSpine, bh * (0.48f + t * 0.06f), z,
                    0, bh * (0.56f + t * 0.06f), z + len * 0.005f);
            }
            AddBoxMat(w, panel, -hw * 0.05f, hw * 0.05f, bh * 0.52f, bh * 0.58f, len * 0.14f, len * medZEnd);
            w.TriMat(hull, -hw * 0.04f, bh * 0.56f, len * (medZEnd - 0.04f), hw * 0.04f, bh * 0.56f, len * (medZEnd - 0.04f),
                0, bh * 0.62f, len * medZEnd);
            return;
        }

        float keelHalfW = hw * (hullKey switch
        {
            "scout_light" => 0.034f,
            "interceptor_mk2" => 0.036f,
            "fighter_basic" => 0.038f,
            "drone_swarm" => 0.028f,
            "hero_default" => 0.042f,
            _ => 0.036f
        });
        float yBase = bh * 0.46f;
        float yCrest = bh * (hullKey is "hero_default" ? 0.58f : 0.54f);
        float zStart = len * (hullKey switch
        {
            "scout_light" => 0.58f,
            "interceptor_mk2" => 0.56f,
            "fighter_basic" => 0.54f,
            "drone_swarm" => 0.52f,
            "hero_default" => 0.56f,
            _ => 0.52f
        });
        float zEnd = len * (hullKey switch
        {
            "scout_light" => 0.90f,
            "interceptor_mk2" => 0.88f,
            "fighter_basic" => 0.86f,
            "drone_swarm" => 0.84f,
            "hero_default" => 0.88f,
            _ => 0.84f
        });
        int keelSegs = hullKey is "drone_swarm" ? 3 : hullKey is "hero_default" ? 4 : 3;
        for (int i = 0; i < keelSegs; i++)
        {
            float t = i / MathF.Max(1f, keelSegs - 1);
            float z = MathHelper.Lerp(zStart, zEnd, t);
            w.TriMat(hull, -keelHalfW, yBase, z, keelHalfW, yBase, z, 0, yCrest, z + len * 0.008f);
            if (i % 2 == 0 || hullKey is "hero_default" or "interceptor_mk2" or "scout_light")
                w.TriMat(accent, -keelHalfW * 0.55f, yCrest * 0.94f, z + len * 0.004f,
                    keelHalfW * 0.55f, yCrest * 0.94f, z + len * 0.004f, 0, yCrest, z + len * 0.008f);
            if (hullKey is "interceptor_mk2")
                w.TriMat(panel, -keelHalfW * 0.68f, yBase + bh * 0.03f, z,
                    keelHalfW * 0.68f, yBase + bh * 0.03f, z, 0, yCrest * 0.90f, z + len * 0.006f);
            if (hullKey is "hero_default" && i % 2 == 0)
                w.TriMat(panel, -keelHalfW * 0.72f, yBase + bh * 0.04f, z,
                    keelHalfW * 0.72f, yBase + bh * 0.04f, z, 0, yCrest * 0.88f, z + len * 0.005f);
        }
    }

    private static void AddCrystallineDorsalFacetSpine(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial panel, RaceMeshWriter.HullMaterial accent,
        float hw, float bh, float l, int segments, float zStart, float zEnd)
    {
        for (int s = 0; s < segments; s++)
        {
            float t = s / MathF.Max(1f, segments - 1);
            float z = MathHelper.Lerp(l * zStart, l * zEnd, t);
            float halfSpine = hw * (0.04f + t * 0.01f);
            var mat = s % 2 == 0 ? accent : panel;
            w.TriMat(mat, -halfSpine, bh * (0.48f + t * 0.08f), z,
                halfSpine, bh * (0.48f + t * 0.08f), z,
                0, bh * (0.56f + t * 0.08f), z + l * 0.005f);
        }
    }

    private static void AddCrystallineFacetWing(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial panel, RaceMeshWriter.HullMaterial accent,
        int side, float len, float hw, float bh, float sweep, float span)
    {
        float xRoot = side * hw * 0.28f;
        float xTip = side * hw * span;
        float zRoot = len * 0.70f;
        float zTip = len * (0.68f - sweep * 0.08f);
        w.TriMat(panel, xRoot, bh * 0.28f, zRoot, xTip, bh * 0.18f, zTip, xRoot, bh * 0.12f, zRoot);
        w.TriMat(accent, xTip, bh * 0.18f, zTip, xTip - side * hw * 0.04f, bh * 0.14f, zTip + len * 0.006f, xRoot, bh * 0.12f, zRoot);
    }

    /// <summary>Lateral facet booms — widen crystalline aspect toward ~0.95 (reach &gt;1 spans past half-width).</summary>
    private static void AddCrystallineAspectFacetBooms(RaceMeshWriter w, float len, float hw, float bh, float reach = 2.55f)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float z0 = -len * 0.08f;
        float z1 = len * 0.12f;
        for (int side = -1; side <= 1; side += 2)
        {
            float xIn = side * hw * 0.14f;
            float xOut = side * hw * reach;
            w.TriMat(panel, xIn, bh * 0.24f, z0, xOut, bh * 0.20f, z1, xIn, bh * 0.10f, z0 - len * 0.03f);
            w.TriMat(accent, xOut, bh * 0.22f, z1, xOut - side * hw * 0.04f, bh * 0.18f, z1 + len * 0.03f, xIn, bh * 0.12f, z0);
            AddBoxMat(w, hull, xOut - side * hw * 0.05f, xOut + side * hw * 0.03f, bh * 0.08f, bh * 0.18f, z0, z1);
        }
    }

    private static void AddCrystallineSweptFacetFin(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial panel, RaceMeshWriter.HullMaterial accent,
        int side, float len, float hw, float bh, float sweep, float span)
    {
        float xRoot = side * hw * 0.24f;
        float xTip = side * hw * span;
        float zRoot = len * 0.72f;
        float zTip = len * (0.72f - sweep * 0.10f);
        w.TriMat(panel, xRoot, bh * 0.30f, zRoot, xTip, bh * 0.20f, zTip, xRoot, bh * 0.14f, zRoot);
        w.TriMat(accent, xTip, bh * 0.20f, zTip, xTip - side * hw * 0.04f, bh * 0.16f, zTip + len * 0.006f, xRoot, bh * 0.14f, zRoot);
    }

    private static void AddCrystallineCrystalPod(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial accent,
        float cx, float cy, float cz, float rx, float ry, int facets = 5)
    {
        for (int i = 0; i < facets; i++)
        {
            float a0 = MathF.PI * 2f * i / facets;
            float a1 = MathF.PI * 2f * (i + 1) / facets;
            float x0 = cx + MathF.Cos(a0) * rx;
            float x1 = cx + MathF.Cos(a1) * rx;
            float z0 = cz + MathF.Sin(a0) * rx * 0.45f;
            float z1 = cz + MathF.Sin(a1) * rx * 0.45f;
            w.TriMat(accent, cx, cy + ry, cz, x0, cy, z0, x1, cy, z1);
            w.TriMat(hull, cx, cy, cz, x0, cy - ry * 0.35f, z0, x1, cy - ry * 0.35f, z1);
        }
    }

    private static void AddCrystallineFacetCrown(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial panel,
        RaceMeshWriter.HullMaterial accent, float hw, float bh, float len)
    {
        for (int p = 0; p < 3; p++)
        {
            float t = p / 2f;
            float z = MathHelper.Lerp(len * 0.62f, len * 0.84f, t);
            float xSpan = hw * MathHelper.Lerp(0.28f, 0.08f, t);
            var mat = p % 2 == 0 ? accent : panel;
            w.TriMat(mat, -xSpan, bh * (0.64f + t * 0.06f), z, xSpan, bh * (0.64f + t * 0.06f), z,
                0, bh * (0.76f + t * 0.08f), z + len * 0.005f);
        }
        w.TriMat(hull, -hw * 0.06f, bh * 0.82f, len * 0.80f, hw * 0.06f, bh * 0.82f, len * 0.80f, 0, bh * 0.96f, len * 0.88f);
    }

    /// <summary>Industrial/logistics crystalline hulls — faceted ice prism base with per-role crystal blooms.</summary>
    private static void BuildCrystallineUtilitySolid(RaceMeshWriter w, string hullKey, float len, float wid, float hgt, float sharpness)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;

        float widthScale = hullKey is "freighter_bulk" ? 0.96f : hullKey is "miner_eva" ? 0.96f : 0.98f;
        float l = len;
        float bw = wid * widthScale;
        float bh = hgt;
        int facets = Math.Clamp(6 + (int)(sharpness * 2), 6, 8);
        float midY = bh * 0.42f;
        float apex = bh * (0.88f + sharpness * 0.22f);
        float sternZ = hullKey is "freighter_bulk" or "transport_cargo" ? -l * 2.0f : -l * 0.50f;
        float bowZ = l * (hullKey switch
        {
            "freighter_bulk" => 0.92f,
            "transport_cargo" => 0.92f,
            "miner_basic" => 0.92f,
            "miner_eva" => 0.90f,
            "miner_tractor" => 0.92f,
            "support_repair" => 0.92f,
            _ => 0.90f
        });

        for (int i = 0; i < facets; i++)
        {
            float a0 = MathF.PI * 2f * i / facets;
            float a1 = MathF.PI * 2f * (i + 1) / facets;
            float r0 = bw * (0.36f + 0.10f * MathF.Sin(i * 1.5f));
            float r1 = bw * (0.36f + 0.10f * MathF.Sin((i + 1) * 1.5f));
            float x0 = MathF.Cos(a0) * r0;
            float x1 = MathF.Cos(a1) * r1;
            float z0 = MathF.Sin(a0) * l * 0.18f;
            float z1 = MathF.Sin(a1) * l * 0.18f;

            w.Tri(0, apex, bowZ * 0.72f, x0, midY, z0, x1, midY, z1);
            w.Tri(x0, midY, z0, x1, midY, z1, x0 * 0.68f, 0, z0 * 0.68f);
            w.Tri(x1, midY, z1, x1 * 0.68f, 0, z1 * 0.68f, x0 * 0.68f, 0, z0 * 0.68f);
            w.Tri(x0 * 0.68f, 0, z0 * 0.68f, x1 * 0.68f, 0, z1 * 0.68f, 0, 0, sternZ);
        }

        int rings = hullKey switch
        {
            "freighter_bulk" or "transport_cargo" => 1,
            "support_repair" => 2,
            _ => 2
        };
        for (int i = 0; i < rings; i++)
        {
            float t = (i + 0.5f) / rings;
            float z = MathHelper.Lerp(-l * 0.32f, bowZ * 0.50f, t);
            float r = bw * (0.24f + 0.08f * MathF.Sin(t * MathF.PI * 2f));
            AddHullRing(w, r, midY * 0.88f, z, Math.Min(4, facets / 2));
        }

        AddLoftedHull(w, l * 0.28f, bw * 0.36f, bh * 0.80f, 2,
            t => bw * 0.08f * (1f - t * 0.45f),
            t => bh * (0.50f + t * 0.34f),
            0.46f, bowZ * 0.94f);

        if (hullKey is "freighter_bulk" or "transport_cargo")
        {
            float bowHalfW = bw * (hullKey is "freighter_bulk" ? 0.06f : 0.05f);
            float bowPeak = bh * (hullKey is "freighter_bulk" ? 0.52f : 0.48f);
            float bowBase = bowZ * 0.92f;
            float bowTip = l * 0.03f;
            w.TriMat(hull, -bowHalfW, midY, bowBase, bowHalfW, midY, bowBase, 0, bowPeak, bowTip);
        }

        float hw = wid * 0.5f * widthScale;
        if (hullKey is "transport_cargo" or "freighter_bulk")
            AddCrystallineAspectFacetBooms(w, l, bw * 0.5f, bh, 1.0f);

        switch (hullKey)
        {
            case "miner_basic": AddCrystallineMiningToolArms(w, hw, hgt, len); break;
            case "miner_eva": AddCrystallineEvaFacetBloom(w, hw, hgt, len); break;
            case "miner_tractor": AddCrystallineTractorFacetBloom(w, hw, hgt, len); break;
            case "transport_cargo": AddCrystallineCargoSpinePrism(w, hw, hgt, len); break;
            case "freighter_bulk": AddCrystallineBulkCrystalHull(w, hw, hgt, len); break;
            case "support_repair": AddCrystallineRepairAntennaFacets(w, hw, hgt, len); break;
        }

        AddCrystallineUtilityDorsalKeel(w, hullKey, hw, hgt, len);
        AddBoxMat(w, engine, -bw * 0.14f, -bw * 0.02f, 0, bh * 0.22f, sternZ - l * 0.02f, sternZ + l * 0.04f);
        AddBoxMat(w, engine, bw * 0.02f, bw * 0.14f, 0, bh * 0.22f, sternZ - l * 0.02f, sternZ + l * 0.04f);
        w.TriMat(accent, -bw * 0.04f, bh * 0.52f, bowZ * 0.84f, bw * 0.04f, bh * 0.52f, bowZ * 0.84f, 0, bh * 0.60f, bowZ);
    }

    private static void AddCrystallineUtilityDorsalKeel(RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float keelHalfW = hw * 0.034f;
        float yBase = hgt * 0.46f;
        float yCrest = hgt * (hullKey is "freighter_bulk" or "transport_cargo" ? 0.60f : 0.56f);
        float zEnd = len * (hullKey switch
        {
            "freighter_bulk" => 0.52f,
            "transport_cargo" => 0.50f,
            "miner_basic" => 0.36f,
            "miner_eva" => 0.28f,
            "miner_tractor" => 0.32f,
            "support_repair" => 0.32f,
            _ => 0.22f
        });
        int keelSegs = hullKey is "freighter_bulk" or "transport_cargo" ? 4 : hullKey is "support_repair" ? 3 : 3;
        for (int i = 0; i < keelSegs; i++)
        {
            float t = i / MathF.Max(1f, keelSegs - 1);
            float z = MathHelper.Lerp(-len * 0.02f, zEnd, t);
            w.TriMat(hull, -keelHalfW, yBase, z, keelHalfW, yBase, z, 0, yCrest, z + len * 0.012f);
            w.TriMat(accent, -keelHalfW * 0.55f, yCrest * 0.96f, z + len * 0.006f,
                keelHalfW * 0.55f, yCrest * 0.96f, z + len * 0.006f, 0, yCrest, z + len * 0.012f);
        }
    }

    private static void AddCrystallineMiningToolArms(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        w.TriMat(hull, -hw * 0.06f, hgt * 0.28f, len * 0.54f, hw * 0.06f, hgt * 0.28f, len * 0.54f, 0, hgt * 0.38f, len * 0.60f);
        w.TriMat(accent, -hw * 0.04f, hgt * 0.34f, len * 0.58f, hw * 0.04f, hgt * 0.34f, len * 0.58f, 0, hgt * 0.40f, len * 0.62f);
        for (int side = -1; side <= 1; side += 2)
        {
            float xRoot = side * hw * 0.34f;
            float xTip = side * hw * 0.68f;
            float zRoot = len * 0.10f;
            float zTip = len * 0.56f;
            w.TriMat(weapon, xRoot, hgt * 0.16f, zRoot, xTip, hgt * 0.22f, zTip, xRoot, hgt * 0.10f, zTip - len * 0.02f);
            w.TriMat(accent, xTip, hgt * 0.22f, zTip, xTip + side * hw * 0.04f, hgt * 0.26f, zTip + len * 0.02f, xTip, hgt * 0.12f, zTip + len * 0.03f);
        }
    }

    private static void AddCrystallineEvaFacetBloom(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float podR = hw * 0.32f;
        float podY = hgt * 0.50f;
        float podZ = len * 0.08f;
        AddHullRing(w, podR, podY + hgt * 0.10f, podZ, 6);
        AddQuadPanel(w, hw * 0.46f, hgt * 0.38f, len * 0.12f, hw * 0.52f, hgt * 0.44f, len * 0.16f);
        w.TriMat(hull, -hw * 0.05f, hgt * 0.30f, len * 0.48f, hw * 0.05f, hgt * 0.30f, len * 0.48f, 0, hgt * 0.40f, len * 0.54f);
        w.TriMat(accent, hw * 0.44f, hgt * 0.42f, len * 0.14f, hw * 0.50f, hgt * 0.48f, len * 0.18f, hw * 0.46f, hgt * 0.36f, len * 0.16f);
    }

    private static void AddCrystallineTractorFacetBloom(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        float dishZ = len * 0.42f;
        AddHullRing(w, hw * 0.24f, hgt * 0.68f, dishZ, 8);
        w.TriMat(hull, -hw * 0.05f, hgt * 0.32f, len * 0.46f, hw * 0.05f, hgt * 0.32f, len * 0.46f, 0, hgt * 0.42f, len * 0.52f);
        for (int side = -1; side <= 1; side += 2)
        {
            float xTip = side * hw * 0.66f;
            w.Tri(side * hw * 0.14f, hgt * 0.24f, len * 0.04f, xTip, hgt * 0.30f, len * 0.16f, xTip, hgt * 0.20f, len * 0.20f);
            AddQuadPanel(w, xTip, hgt * 0.30f, len * 0.14f, xTip + side * hw * 0.04f, hgt * 0.36f, len * 0.18f);
        }
    }

    private static void AddCrystallineCargoSpinePrism(RaceMeshWriter w, float hw, float hgt, float len)
    {
        for (int s = 0; s < 3; s++)
        {
            float t = s / 2f;
            float z = MathHelper.Lerp(-len * 1.6f, len * 0.42f, t);
            AddHullRing(w, hw * MathHelper.Lerp(0.08f, 0.06f, t), hgt * (0.34f + t * 0.24f), z, 3);
        }
        AddLoftedHull(w, len * 1.2f, hw * 0.08f, hgt * 0.42f, 2,
            _ => hw * 0.04f,
            t => hgt * (0.32f + t * 0.12f),
            -1.6f, 0.42f);
    }

    private static void AddCrystallineBulkCrystalHull(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        for (int b = 0; b < 3; b++)
        {
            float t = b / 2f;
            float z = MathHelper.Lerp(-len * 1.6f, len * 0.22f, t);
            AddHullRing(w, hw * MathHelper.Lerp(0.10f, 0.26f, t), hgt * (0.12f + t * 0.08f), z, 4);
        }
        w.TriMat(hull, -hw * 0.06f, hgt * 0.38f, len * 0.38f, hw * 0.06f, hgt * 0.38f, len * 0.38f, 0, hgt * 0.46f, len * 0.48f);
        for (int side = -1; side <= 1; side += 2)
        {
            float cx = side * hw * 0.36f;
            AddQuadPanel(w, cx, hgt * 0.28f, len * 0.36f, cx + side * hw * 0.06f, hgt * 0.34f, len * 0.40f);
        }
    }

    private static void AddCrystallineRepairAntennaFacets(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        for (int a = 0; a < 2; a++)
        {
            float z = len * (0.06f + a * 0.060f);
            AddHullRing(w, hw * 0.05f, hgt * (0.76f + a * 0.08f), z, 4);
        }
        w.TriMat(hull, -hw * 0.02f, hgt * 0.88f, len * 0.16f, hw * 0.02f, hgt * 0.88f, len * 0.16f, 0, hgt * 0.98f, len * 0.20f);
        w.TriMat(accent, -hw * 0.015f, hgt * 0.94f, len * 0.18f, hw * 0.015f, hgt * 0.94f, len * 0.18f, 0, hgt * 1.02f, len * 0.22f);
        for (int side = -1; side <= 1; side += 2)
        {
            float xBoom = side * hw * 0.60f;
            w.Tri(side * hw * 0.10f, hgt * 0.44f, len * 0.06f, xBoom, hgt * 0.52f, len * 0.14f, xBoom, hgt * 0.40f, len * 0.18f);
        }
    }

    /// <summary>Fast corvette — swept facet wedge, narrow prism pods, modest +Z bow.</summary>
    private static void BuildCryoCorvetteFast(RaceMeshWriter w, float len, float wid, float hgt, float sharpness)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var facet = RaceMeshWriter.HullMaterial.Truss;
        var crystal = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        float hw = wid * 0.5f;
        float bh = hgt;
        float bowBase = len * 0.94f;
        float sternZ = -len * 0.14f;

        AddCrystallineAspectFacetBooms(w, len, hw, bh, 1.02f);
        AddCrystallineEnvelopeGemCore(w, len, wid, bh, sharpness, 2, 0.96f);
        AddCrystallineDorsalFacetKeel(w, hull, panel, crystal, hw, bh, len, "corvette_fast");
        AddCrystallineBowFacet(w, hull, crystal, hw * 0.08f, bh * 0.30f, bowBase, len * 0.06f);
        AddCrystallineBowElongation(w, hull, crystal, hw * 0.06f, bh * 0.34f, bowBase, len * 0.06f);

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * hw * 0.72f;
            w.TriMat(facet, x, bh * 0.28f, len * 0.68f, x - side * hw * 0.10f, bh * 0.18f, len * 0.72f, x, bh * 0.12f, len * 0.78f);
            w.TriMat(crystal, x, bh * 0.32f, len * 0.76f, x - side * hw * 0.06f, bh * 0.24f, len * 0.80f, x, bh * 0.18f, len * 0.84f);
        }

        w.TriMat(hull, -hw * 0.18f, bh * 0.30f, len * 0.88f, hw * 0.18f, bh * 0.30f, len * 0.88f, 0, bh * 0.46f, len * 0.94f);
        AddBoxMat(w, weapon, -hw * 0.98f, -hw * 0.66f, bh * 0.04f, bh * 0.14f, len * 0.68f, len * 0.78f);
        AddBoxMat(w, weapon, hw * 0.66f, hw * 0.98f, bh * 0.04f, bh * 0.14f, len * 0.68f, len * 0.78f);
        AddBoxMat(w, engine, -hw * 0.28f, hw * 0.28f, 0, bh * 0.14f, sternZ - len * 0.02f, sternZ + len * 0.04f);
    }

    /// <summary>Strike frigate — balanced gun-pod crystal blooms on facet hull.</summary>
    private static void BuildCryoFrigateStrike(RaceMeshWriter w, float len, float wid, float hgt, float sharpness)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var facet = RaceMeshWriter.HullMaterial.Truss;
        var crystal = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        float hw = wid * 0.5f;
        float bh = hgt;
        float bowBase = len * 0.94f;
        float sternZ = -len * 0.14f;

        AddCrystallineAspectFacetBooms(w, len, hw, bh, 1.0f);
        AddCrystallineEnvelopeGemCore(w, len, wid, bh, sharpness, 2, 0.96f);
        AddCrystallineDorsalFacetKeel(w, hull, panel, crystal, hw, bh, len, "frigate_strike");
        AddCrystallineBowFacet(w, hull, crystal, hw * 0.10f, bh * 0.30f, bowBase, len * 0.06f);
        AddCrystallineBowElongation(w, hull, crystal, hw * 0.08f, bh * 0.34f, bowBase, len * 0.05f);

        for (int p = 0; p < 2; p++)
        {
            float side = (p == 0 ? -1f : 1f) * hw * 0.58f;
            float z = MathHelper.Lerp(len * 0.68f, len * 0.80f, p);
            AddIntegratedBulb(w, side, bh * 0.36f, z, hw * 0.22f, bh * 0.16f, 3);
            AddBoxMat(w, weapon, side - hw * 0.14f, side + hw * 0.14f, bh * 0.06f, bh * 0.18f, z - len * 0.04f, z + len * 0.04f);
        }

        w.TriMat(hull, -hw * 0.20f, bh * 0.30f, len * 0.86f, hw * 0.20f, bh * 0.30f, len * 0.86f, 0, bh * 0.46f, len * 0.92f);
        AddBoxMat(w, facet, -hw * 0.22f, hw * 0.22f, bh * 0.48f, bh * 0.60f, len * 0.58f, len * 0.76f);
        AddBoxMat(w, engine, -hw * 0.28f, hw * 0.28f, 0, bh * 0.16f, sternZ - len * 0.02f, sternZ + len * 0.04f);
        for (int v = 0; v < 2; v++)
        {
            float z = len * (0.60f + v * 0.08f);
            w.TriMat(crystal, -hw * 0.10f, bh * 0.52f, z, hw * 0.10f, bh * 0.52f, z, 0, bh * 0.58f, z + len * 0.006f);
        }
    }

    /// <summary>Heavy bomber — wide payload prism with dorsal facet stack.</summary>
    private static void BuildCryoBomberHeavy(RaceMeshWriter w, float len, float wid, float hgt, float sharpness)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var facet = RaceMeshWriter.HullMaterial.Truss;
        var crystal = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        float hw = wid * 0.5f;
        float bh = hgt;
        float bowBase = len * 0.94f;
        float sternZ = -len * 0.14f;

        AddCrystallineAspectFacetBooms(w, len, hw, bh, 0.98f);
        AddCrystallineEnvelopeGemCore(w, len, wid, bh, sharpness, 2, 0.96f);
        AddCrystallineDorsalFacetKeel(w, hull, panel, crystal, hw, bh, len, "bomber_heavy");
        AddCrystallineBowFacet(w, hull, crystal, hw * 0.12f, bh * 0.26f, bowBase, len * 0.05f);
        AddCrystallineBowElongation(w, hull, crystal, hw * 0.10f, bh * 0.30f, bowBase, len * 0.05f);
        AddLoftedHull(w, len * 0.42f, hw * 0.52f, bh * 0.62f, 2,
            t => hw * 0.16f * (1f - t * 0.35f),
            t => bh * (0.48f + t * 0.36f),
            0.58f, 0.78f);

        w.TriMat(weapon, -hw * 0.56f, 0, len * 0.62f, hw * 0.56f, 0, len * 0.62f, 0, bh * 0.10f, len * 0.72f);
        AddBoxMat(w, facet, -hw * 0.48f, hw * 0.48f, bh * 0.26f, bh * 0.44f, len * 0.52f, len * 0.72f);
        w.TriMat(hull, -hw * 0.20f, bh * 0.26f, len * 0.86f, hw * 0.20f, bh * 0.26f, len * 0.86f, 0, bh * 0.40f, len * 0.92f);
        AddBoxMat(w, engine, -hw * 0.32f, hw * 0.32f, 0, bh * 0.14f, sternZ - len * 0.02f, sternZ + len * 0.04f);
        for (int v = 0; v < 2; v++)
        {
            float z = len * (0.56f + v * 0.08f);
            float xSpan = hw * MathHelper.Lerp(0.28f, 0.14f, v);
            w.TriMat(crystal, -xSpan, bh * 0.44f, z, xSpan, bh * 0.44f, z, 0, bh * 0.50f, z + len * 0.006f);
        }
    }

    /// <summary>Heavy gunship — heavy chin weapon facet mass on crystalline frame.</summary>
    private static void BuildCryoGunshipHeavy(RaceMeshWriter w, float len, float wid, float hgt, float sharpness)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var facet = RaceMeshWriter.HullMaterial.Truss;
        var crystal = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        float hw = wid * 0.5f;
        float bh = hgt;
        float bowBase = len * 0.94f;
        float sternZ = -len * 0.14f;

        AddCrystallineAspectFacetBooms(w, len, hw, bh, 0.98f);
        AddCrystallineEnvelopeGemCore(w, len, wid, bh, sharpness, 2, 0.96f);
        AddCrystallineDorsalFacetKeel(w, hull, panel, crystal, hw, bh, len, "gunship_heavy");
        AddCrystallineBowFacet(w, hull, crystal, hw * 0.12f, bh * 0.24f, bowBase, len * 0.08f);
        AddCrystallineBowElongation(w, hull, crystal, hw * 0.10f, bh * 0.28f, bowBase, len * 0.08f);

        w.TriMat(weapon, -hw * 0.28f, 0, len * 0.72f, hw * 0.28f, 0, len * 0.72f, 0, bh * 0.08f, len * 0.84f);
        w.TriMat(weapon, -hw * 0.20f, 0, len * 0.78f, hw * 0.20f, 0, len * 0.78f, 0, bh * 0.08f, len * 0.88f);
        AddBoxMat(w, weapon, -hw * 0.84f, -hw * 0.56f, bh * 0.04f, bh * 0.12f, len * 0.66f, len * 0.76f);
        AddBoxMat(w, weapon, hw * 0.56f, hw * 0.84f, bh * 0.04f, bh * 0.12f, len * 0.66f, len * 0.76f);
        AddBoxMat(w, facet, -hw * 0.48f, hw * 0.48f, bh * 0.28f, bh * 0.40f, len * 0.52f, len * 0.72f);
        w.TriMat(hull, -hw * 0.20f, bh * 0.24f, len * 0.86f, hw * 0.20f, bh * 0.24f, len * 0.86f, 0, bh * 0.36f, len * 0.92f);
        AddBoxMat(w, engine, -hw * 0.28f, hw * 0.28f, 0, bh * 0.14f, sternZ - len * 0.02f, sternZ + len * 0.04f);
        w.TriMat(crystal, -hw * 0.12f, bh * 0.46f, len * 0.58f, hw * 0.12f, bh * 0.46f, len * 0.58f, 0, bh * 0.52f, len * 0.64f);
    }

    /// <summary>Assault destroyer — assault spine ridge with mid-band facet rings.</summary>
    private static void BuildCryoDestroyerAssault(RaceMeshWriter w, float len, float wid, float hgt, float sharpness)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var facet = RaceMeshWriter.HullMaterial.Truss;
        var crystal = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        float l = len;
        float hw = wid * 0.5f;
        float bh = hgt;
        float bowBase = l * 0.94f;
        float sternZ = -l * 0.14f;

        AddCrystallineAspectFacetBooms(w, l, hw, bh, 1.0f);
        AddCrystallineEnvelopeGemCore(w, l, wid, bh, sharpness, 2, 0.96f);
        AddCrystallineDorsalFacetKeel(w, hull, panel, crystal, hw, bh, l, "destroyer_assault");
        AddCrystallineBowFacet(w, hull, crystal, hw * 0.10f, bh * 0.32f, bowBase, l * 0.06f);
        AddCrystallineBowElongation(w, hull, crystal, hw * 0.08f, bh * 0.36f, bowBase, l * 0.05f);
        AddLoftedHull(w, l * 0.40f, hw * 0.34f, bh * 0.68f, 2,
            t => hw * 0.12f * (1f - t * 0.28f),
            t => bh * (0.58f + t * 0.38f),
            0.58f, 0.78f);

        for (int p = 0; p < 2; p++)
        {
            float side = (p == 0 ? -1f : 1f) * hw * 0.54f;
            float z = MathHelper.Lerp(l * 0.60f, l * 0.76f, p);
            AddIntegratedBulb(w, side, bh * 0.38f, z, hw * 0.16f, bh * 0.14f, 3);
        }

        AddHullRing(w, hw * 0.36f, bh * 0.48f, l * 0.58f, 3);

        AddBoxMat(w, weapon, -hw * 0.20f, hw * 0.20f, bh * 0.08f, bh * 0.20f, l * 0.72f, l * 0.84f);
        AddBoxMat(w, facet, -hw * 0.22f, hw * 0.22f, bh * 0.62f, bh * 0.78f, l * 0.58f, l * 0.76f);
        w.TriMat(hull, -hw * 0.16f, bh * 0.32f, l * 0.88f, hw * 0.16f, bh * 0.32f, l * 0.88f, 0, bh * 0.50f, l * 0.94f);
        AddBoxMat(w, engine, -hw * 0.32f, hw * 0.32f, 0, bh * 0.16f, sternZ - l * 0.02f, sternZ + l * 0.04f);
    }

    private static void BuildCryoCruiserHeavy(RaceMeshWriter w, float len, float wid, float hgt, float sharpness)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var crystal = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float l = len;
        float hw = wid * 0.5f;
        float bh = hgt;
        float bowBase = l * 0.94f;

        AddCrystallineAspectFacetBooms(w, l, hw, bh, 1.0f);
        AddCrystallineEnvelopeGemCore(w, l, wid, bh, sharpness, 2, 0.96f);
        AddLoftedHull(w, l * 0.40f, hw * 0.44f, bh * 0.76f, 2,
            t => hw * 0.14f * (1f - t * 0.25f),
            t => bh * (0.56f + t * 0.36f),
            -0.48f, 0.72f);
        AddHullRing(w, hw * 0.42f, bh * 0.52f, -l * 0.12f, 3);
        AddCrystallineDorsalFacetSpine(w, panel, crystal, hw, bh, l, 2, 0.08f, 0.22f);
        AddCrystallineBowFacet(w, hull, crystal, hw * 0.12f, bh * 0.34f, bowBase, l * 0.05f);
        AddCrystallineBowElongation(w, hull, crystal, hw * 0.10f, bh * 0.40f, bowBase, l * 0.04f);
        w.TriMat(hull, -hw * 0.16f, bh * 0.32f, l * 0.70f, hw * 0.16f, bh * 0.32f, l * 0.70f, 0, bh * 0.50f, l * 0.76f);
        AddBoxMat(w, weapon, -hw * 0.10f, hw * 0.10f, bh * 0.10f, bh * 0.18f, l * 0.54f, l * 0.64f);
        AddBoxMat(w, RaceMeshWriter.HullMaterial.Engine, -hw * 0.36f, hw * 0.36f, 0, bh * 0.18f, -l * 0.54f, -l * 0.40f);
    }

    private static void BuildCryoCarrierCommand(RaceMeshWriter w, float len, float wid, float hgt, float sharpness)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var crystal = RaceMeshWriter.HullMaterial.Solar;
        float l = len;
        float hw = wid * 0.5f;
        float bh = hgt;
        float bowBase = l * 0.94f;

        AddCrystallineAspectFacetBooms(w, l, hw, bh, 1.0f);
        AddCrystallineEnvelopeGemCore(w, l, wid, bh, sharpness, 2, 0.96f);
        AddLoftedHull(w, l * 0.36f, hw * 0.50f, bh * 0.68f, 2,
            t => hw * 0.18f * (1f - t * 0.22f),
            t => bh * (0.50f + t * 0.28f),
            -0.44f, 0.58f);
        AddIntegratedBulb(w, 0, bh * 0.22f, l * 0.08f, hw * 0.22f, bh * 0.11f, 3);
        AddCrystallineDorsalFacetSpine(w, panel, crystal, hw, bh, l, 2, 0.06f, 0.20f);
        AddCrystallineBowFacet(w, hull, crystal, hw * 0.10f, bh * 0.28f, bowBase, l * 0.04f);
        AddCrystallineBowElongation(w, hull, crystal, hw * 0.08f, bh * 0.34f, bowBase, l * 0.04f);
        w.TriMat(hull, -hw * 0.10f, bh * 0.24f, l * 0.12f, hw * 0.10f, bh * 0.24f, l * 0.12f, 0, bh * 0.30f, l * 0.16f);
        AddBoxMat(w, RaceMeshWriter.HullMaterial.Engine, -hw * 0.38f, hw * 0.38f, 0, bh * 0.16f, -l * 0.56f, -l * 0.42f);
    }

    private static void BuildCryoDreadnought(RaceMeshWriter w, float len, float wid, float hgt, float sharpness)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var facet = RaceMeshWriter.HullMaterial.Truss;
        var crystal = RaceMeshWriter.HullMaterial.Solar;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float l = len;
        float hw = wid * 0.5f;
        float bh = hgt;
        float bowBase = l * 0.94f;

        AddCrystallineAspectFacetBooms(w, l, hw, bh, 1.0f);
        AddCrystallineEnvelopeGemCore(w, l, wid, bh, sharpness, 2, 0.96f);
        AddLoftedHull(w, l * 0.44f, hw * 0.48f, bh * 0.82f, 2,
            t => hw * 0.16f * (1f - t * 0.28f),
            t => bh * (0.58f + t * 0.38f),
            -0.46f, 0.74f);
        AddHullRing(w, hw * 0.44f, bh * 0.56f, -l * 0.20f, 3);
        w.TriMat(facet, -hw * 0.10f, bh * 0.36f, l * 0.54f, hw * 0.10f, bh * 0.36f, l * 0.54f, 0, bh * 0.44f, l * 0.56f);
        AddCrystallineDorsalFacetSpine(w, panel, crystal, hw, bh, l, 2, 0.10f, 0.24f);
        AddCrystallineBowFacet(w, hull, crystal, hw * 0.14f, bh * 0.32f, bowBase, l * 0.05f);
        AddCrystallineBowElongation(w, hull, crystal, hw * 0.12f, bh * 0.38f, bowBase, l * 0.04f);
        w.TriMat(hull, -hw * 0.14f, bh * 0.28f, l * 0.66f, hw * 0.14f, bh * 0.28f, l * 0.66f, 0, bh * 0.44f, l * 0.72f);
        AddBoxMat(w, weapon, -hw * 0.24f, hw * 0.24f, bh * 0.10f, bh * 0.24f, l * 0.54f, l * 0.68f);
        AddBoxMat(w, RaceMeshWriter.HullMaterial.Engine, -hw * 0.40f, hw * 0.40f, 0, bh * 0.20f, -l * 0.58f, -l * 0.44f);
    }

    private static void BuildCrystallineHull(RaceMeshWriter w, string hullKey, float len, float wid, float hgt, float sharpness)
    {
        switch (hullKey)
        {
            case "scout":
            case "scout_light":
                BuildCryoScout(w, len, wid, hgt, sharpness);
                return;
            case "fighter":
            case "fighter_basic":
                BuildCryoFighter(w, len, wid, hgt, sharpness);
                return;
            case "interceptor":
            case "interceptor_mk2":
                BuildCryoInterceptor(w, len, wid, hgt, sharpness);
                return;
            case "drone":
            case "drone_swarm":
                BuildCryoDrone(w, len, wid, hgt, sharpness);
                return;
            case "hero":
            case "hero_default":
                BuildCryoHero(w, len, wid, hgt, sharpness);
                return;
            case "corvette_fast":
                BuildCryoCorvetteFast(w, len, wid, hgt, sharpness);
                return;
            case "frigate_strike":
                BuildCryoFrigateStrike(w, len, wid, hgt, sharpness);
                return;
            case "bomber_heavy":
                BuildCryoBomberHeavy(w, len, wid, hgt, sharpness);
                return;
            case "gunship_heavy":
                BuildCryoGunshipHeavy(w, len, wid, hgt, sharpness);
                return;
            case "destroyer_assault":
                BuildCryoDestroyerAssault(w, len, wid, hgt, sharpness);
                return;
            case "cruiser":
            case "cruiser_heavy":
                BuildCryoCruiserHeavy(w, len, wid, hgt, sharpness);
                return;
            case "carrier":
            case "carrier_command":
                BuildCryoCarrierCommand(w, len, wid, hgt, sharpness);
                return;
            case "dreadnought":
                BuildCryoDreadnought(w, len, wid, hgt, sharpness);
                return;
            case "miner_basic":
            case "miner_eva":
            case "miner_tractor":
            case "transport_cargo":
            case "freighter_bulk":
            case "support_repair":
                BuildCrystallineUtilitySolid(w, hullKey, len, wid, hgt, sharpness);
                return;
        }

        float l = len;
        int facets = Math.Clamp(8 + (int)(sharpness * 8) + (hullKey is "dreadnought" or "carrier" ? 4 : 0), 8, 18);
        float apex = hgt * (1.1f + sharpness * 0.4f);
        float midY = hgt * 0.42f;
        float sternZ = -l * 0.52f;

        for (int i = 0; i < facets; i++)
        {
            float a0 = MathF.PI * 2f * i / facets;
            float a1 = MathF.PI * 2f * (i + 1) / facets;
            float r0 = wid * (0.32f + 0.12f * MathF.Sin(i * 1.7f));
            float r1 = wid * (0.32f + 0.12f * MathF.Sin((i + 1) * 1.7f));
            float x0 = MathF.Cos(a0) * r0;
            float x1 = MathF.Cos(a1) * r1;
            float z0 = MathF.Sin(a0) * l * 0.22f;
            float z1 = MathF.Sin(a1) * l * 0.22f;

            w.Tri(0, apex, l * 0.22f, x0, midY, z0, x1, midY, z1);
            w.Tri(x0, midY, z0, x1, midY, z1, x0 * 0.65f, 0, z0 * 0.65f);
            w.Tri(x1, midY, z1, x1 * 0.65f, 0, z1 * 0.65f, x0 * 0.65f, 0, z0 * 0.65f);
            w.Tri(x0 * 0.65f, 0, z0 * 0.65f, x1 * 0.65f, 0, z1 * 0.65f, 0, 0, sternZ);
        }

        int bands = HullSections(hullKey, 4, 2, 6);
        for (int b = 0; b < bands; b++)
        {
            float t = b / (float)bands;
            float z = MathHelper.Lerp(-l * 0.15f, l * 0.35f, t);
            float r = wid * (0.28f + 0.06f * MathF.Sin(b * 2.1f));
            AddHullRing(w, r, midY * 0.85f, z, facets / 2);
        }

        if (hullKey is "dreadnought" or "carrier" or "cruiser")
        {
            AddLoftedHull(w, l * 0.35f, wid * 0.5f, hgt * 0.7f, 5,
                t => wid * 0.12f * (1f - t * 0.25f),
                t => hgt * (0.35f + t * 0.3f),
                0.15f, 0.42f);
        }
    }

    // â”€â”€ Angular wedge (Terran) â€” classic delta-wing fighter silhouette â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static void BuildAngularWedge(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        switch (hullKey)
        {
            case "scout":       BuildScoutWedge(w, len, wid, hgt); break;
            case "drone":       BuildDroneWedge(w, len, wid, hgt); break;
            case "bomber":
            case "gunship":     BuildWideWedge(w, len, wid, hgt); break;
            case "carrier":
            case "dreadnought":
            case "cruiser":     BuildCapitalWedge(w, len, wid, hgt); break;
            case "transport":
            case "freighter":
            case "miner":
            case "support":     BuildUtilityWedge(w, len, wid, hgt); break;
            default:            BuildFighterWedge(w, len, wid, hgt); break;
        }
    }

    private static void BuildFighterWedge(RaceMeshWriter w, float len, float wid, float hgt)
    {
        w.Tri(0, hgt * 0.35f, len * 0.95f, -wid * 0.55f, 0, -len * 0.12f, wid * 0.55f, 0, -len * 0.12f);
        w.Tri(-wid * 0.55f, 0, -len * 0.12f, wid * 0.55f, 0, -len * 0.12f, 0, hgt * 0.15f, -len * 0.55f);
        w.Tri(0, hgt * 1.15f, len * 0.5f, -wid * 0.14f, hgt * 0.35f, len * 0.08f, wid * 0.14f, hgt * 0.35f, len * 0.08f);
        w.Tri(-wid * 0.38f, hgt * 0.08f, len * 0.2f, wid * 0.38f, hgt * 0.08f, len * 0.2f, 0, hgt * 0.35f, len * 0.95f);
    }

    private static void BuildScoutWedge(RaceMeshWriter w, float len, float wid, float hgt)
    {
        w.Tri(0, hgt * 0.2f, len, -wid * 0.2f, 0, len * 0.2f, wid * 0.2f, 0, len * 0.2f);
        w.Tri(-wid * 0.35f, 0, -len * 0.1f, wid * 0.35f, 0, -len * 0.1f, 0, hgt * 0.55f, 0);
        w.Tri(0, hgt * 0.75f, -len * 0.35f, -wid * 0.1f, hgt * 0.15f, -len * 0.5f, wid * 0.1f, hgt * 0.15f, -len * 0.5f);
    }

    private static void BuildDroneWedge(RaceMeshWriter w, float len, float wid, float hgt)
    {
        w.Tri(0, hgt * 0.5f, len * 0.45f, -wid * 0.4f, 0, -len * 0.1f, wid * 0.4f, 0, -len * 0.1f);
        w.Tri(-wid * 0.25f, hgt * 0.05f, len * 0.12f, wid * 0.25f, hgt * 0.05f, len * 0.12f, 0, hgt * 0.5f, len * 0.45f);
    }

    private static void BuildWideWedge(RaceMeshWriter w, float len, float wid, float hgt)
    {
        w.Tri(0, hgt * 0.5f, len * 0.65f, -wid * 0.75f, hgt * 0.1f, len * 0.05f, wid * 0.75f, hgt * 0.1f, len * 0.05f);
        w.Tri(-wid * 0.75f, hgt * 0.1f, len * 0.05f, wid * 0.75f, hgt * 0.1f, len * 0.05f, 0, hgt * 0.35f, -len * 0.65f);
        w.Tri(-wid * 0.55f, 0, -len * 0.2f, wid * 0.55f, 0, -len * 0.2f, 0, hgt * 0.35f, -len * 0.65f);
    }

    private static void BuildCapitalWedge(RaceMeshWriter w, float len, float wid, float hgt)
    {
        w.Tri(0, hgt * 0.55f, len * 0.85f, -wid * 0.5f, hgt * 0.08f, len * 0.15f, wid * 0.5f, hgt * 0.08f, len * 0.15f);
        w.Tri(-wid * 0.5f, hgt * 0.08f, len * 0.15f, wid * 0.5f, hgt * 0.08f, len * 0.15f, 0, hgt * 0.38f, -len * 0.85f);
        w.Tri(-wid * 0.45f, hgt * 0.2f, -len * 0.15f, wid * 0.45f, hgt * 0.2f, -len * 0.15f, 0, hgt * 1.1f, len * 0.02f);
        w.Tri(-wid * 0.35f, hgt * 0.12f, len * 0.25f, wid * 0.35f, hgt * 0.12f, len * 0.25f, 0, hgt * 0.28f, -len * 0.35f);
    }

    private static void BuildUtilityWedge(RaceMeshWriter w, float len, float wid, float hgt)
    {
        w.Tri(0, hgt * 0.32f, len * 0.58f, -wid * 0.58f, 0, len * 0.08f, wid * 0.58f, 0, len * 0.08f);
        w.Tri(-wid * 0.58f, 0, len * 0.08f, wid * 0.58f, 0, len * 0.08f, 0, hgt * 0.22f, -len * 0.55f);
        w.Tri(-wid * 0.4f, hgt * 0.1f, 0, wid * 0.4f, hgt * 0.1f, 0, 0, hgt * 0.55f, len * 0.35f);
    }

    // â”€â”€ Hull smoothing primitives â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static int HullSections(string hullKey, int standard, int compact, int capital)
    {
        if (hullKey is "drone" or "drone_swarm" or "scout" or "scout_light") return compact;
        if (hullKey is "dreadnought" or "carrier" or "carrier_command" or "cruiser" or "cruiser_heavy") return capital;
        return standard;
    }

    private static void AddLoftedHull(RaceMeshWriter w, float len, float wid, float hgt, int sections,
        Func<float, float> radiusFn, Func<float, float> heightFn, float zStart, float zEnd)
        => AddLoftedHull(w, len, wid, hgt, sections, radiusFn, heightFn, zStart, zEnd, _ => 0f);

    private static void AddLoftedHull(RaceMeshWriter w, float len, float wid, float hgt, int sections,
        Func<float, float> radiusFn, Func<float, float> heightFn, float zStart, float zEnd, Func<float, float> skewFn)
    {
        for (int i = 0; i < sections; i++)
        {
            float t0 = i / (float)sections;
            float t1 = (i + 1) / (float)sections;
            float z0 = MathHelper.Lerp(len * zStart, len * zEnd, t0);
            float z1 = MathHelper.Lerp(len * zStart, len * zEnd, t1);
            float r0 = radiusFn(t0);
            float r1 = radiusFn(t1);
            float h0 = heightFn(t0);
            float h1 = heightFn(t1);
            float s0 = skewFn(t0);
            float s1 = skewFn(t1);
            float dz = z1 - z0;

            w.Tri(-r0 + s0, h0 * 0.32f, z0, r0 + s0, h0 * 0.32f, z0, s0, h0, z0 + dz * 0.45f);
            w.Tri(-r0 + s0, h0 * 0.32f, z0, s0, h0, z0 + dz * 0.45f, -r1 + s1, h1 * 0.32f, z1);
            w.Tri(r0 + s0, h0 * 0.32f, z0, r1 + s1, h1 * 0.32f, z1, s1, h1, z0 + dz * 0.45f);
            w.Tri(-r0 + s0, 0, z0, -r1 + s1, 0, z1, r0 + s0, 0, z0);
            w.Tri(r0 + s0, 0, z0, -r1 + s1, 0, z1, r1 + s1, 0, z1);
        }
    }

    private static void AddHullBand(RaceMeshWriter w, float hw, float hh, float z, float depth)
    {
        w.Tri(-hw, hh, z, hw, hh, z, hw, hh, z + depth);
        w.Tri(-hw, hh, z, hw, hh, z + depth, -hw, hh, z + depth);
        w.Tri(-hw, hh * 0.15f, z, hw, hh * 0.15f, z + depth, hw, hh * 0.15f, z);
        w.Tri(-hw, hh * 0.15f, z, -hw, hh * 0.15f, z + depth, hw, hh * 0.15f, z + depth);
    }

    private static void AddSolarPaddle(RaceMeshWriter w, float x0, float x1, float y0, float y1, float z0, float z1)
    {
        w.Tri(x0, y0, z0, x1, y0, z0, x1, y1, z0);
        w.Tri(x0, y0, z0, x1, y1, z0, x0, y1, z0);
        w.Tri(x0, y0, z1, x1, y1, z1, x1, y0, z1);
        w.Tri(x0, y0, z1, x0, y1, z1, x1, y1, z1);
        w.Tri(x0, y0, z0, x0, y0, z1, x1, y0, z1);
        w.Tri(x0, y0, z0, x1, y0, z1, x1, y0, z0);
        w.Tri(x0, y1, z0, x1, y1, z1, x0, y1, z1);
        w.Tri(x0, y1, z0, x1, y1, z0, x1, y1, z1);
    }

    private static void AddQuadPanel(RaceMeshWriter w, float x0, float y0, float z0, float x1, float y1, float z1)
    {
        w.Tri(x0, y0, z0, x1, y1, z1, x0, y0 * 0.92f, z0 + (z1 - z0) * 0.35f);
        w.Tri(x0, y0, z0, x0, y0 * 0.92f, z0 + (z1 - z0) * 0.35f, x1, y1, z1);
    }

    private static void AddIntegratedBulb(RaceMeshWriter w, float cx, float cy, float cz, float rx, float ry, int segments)
    {
        for (int i = 0; i < segments; i++)
        {
            float a0 = MathF.PI * 2f * i / segments;
            float a1 = MathF.PI * 2f * (i + 1) / segments;
            float x0 = cx + MathF.Cos(a0) * rx;
            float x1 = cx + MathF.Cos(a1) * rx;
            float z0 = cz + MathF.Sin(a0) * rx * 0.45f;
            float z1 = cz + MathF.Sin(a1) * rx * 0.45f;
            w.Tri(cx, cy + ry, cz, x0, cy, z0, x1, cy, z1);
            w.Tri(cx, cy, cz, x0, cy - ry * 0.35f, z0, x1, cy - ry * 0.35f, z1);
        }
    }

    private static void AddHullRing(RaceMeshWriter w, float radius, float height, float z, int segments)
    {
        for (int i = 0; i < segments; i++)
        {
            float a0 = MathF.PI * 2f * i / segments;
            float a1 = MathF.PI * 2f * (i + 1) / segments;
            float x0 = MathF.Cos(a0) * radius;
            float x1 = MathF.Cos(a1) * radius;
            float z0 = z + MathF.Sin(a0) * radius * 0.15f;
            float z1 = z + MathF.Sin(a1) * radius * 0.15f;
            w.Tri(x0, height, z0, x1, height, z1, 0, height * 1.12f, z);
            w.Tri(x0, height * 0.2f, z0, x1, height * 0.2f, z1, x0, height, z0);
            w.Tri(x1, height * 0.2f, z1, x1, height, z1, x0, height, z0);
        }
    }

    private static void AddEmbossedRing(RaceMeshWriter w, float hw, float hh, float z, float skew, float depth, int segments)
    {
        for (int i = 0; i < segments; i++)
        {
            float a0 = MathF.PI * 2f * i / segments;
            float a1 = MathF.PI * 2f * (i + 1) / segments;
            float x0 = MathF.Cos(a0) * hw + skew;
            float x1 = MathF.Cos(a1) * hw + skew;
            float y0 = hh * (0.35f + 0.65f * MathF.Sin(a0));
            float y1 = hh * (0.35f + 0.65f * MathF.Sin(a1));
            w.Tri(x0, y0, z, x1, y1, z, x1, y1, z + depth);
            w.Tri(x0, y0, z, x1, y1, z + depth, x0, y0, z + depth);
            w.Tri(x0, y0 * 0.25f, z, x1, y1 * 0.25f, z, x0, y0, z);
            w.Tri(x1, y1 * 0.25f, z, x1, y1, z, x0, y0, z);
        }
    }

    private static void AddSurfaceSpine(RaceMeshWriter w,
        float bx0, float by0, float bz0, float tx, float ty, float tz, float bx1, float by1, float bz1)
    {
        w.Tri(bx0, by0, bz0, tx, ty, tz, bx1, by1, bz1);
        w.Tri(bx0, by0, bz0, bx1, by1, bz1, 0, by0 * 0.85f, bz0);
        w.Tri(tx, ty, tz, bx0, by0, bz0, 0, by0 * 0.85f, bz0);
    }

    // â”€â”€ NASA truss primitives (late-90s station modules) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static void AddNasaCylinder(RaceMeshWriter w, float cx, float cy, float cz, float radius, float height, int segments)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        float y0 = cy;
        float y1 = cy + height;
        for (int i = 0; i < segments; i++)
        {
            float a0 = MathF.PI * 2f * i / segments;
            float a1 = MathF.PI * 2f * (i + 1) / segments;
            float x0 = cx + MathF.Cos(a0) * radius;
            float z0 = cz + MathF.Sin(a0) * radius;
            float x1 = cx + MathF.Cos(a1) * radius;
            float z1 = cz + MathF.Sin(a1) * radius;

            w.TriMat(hull, x0, y0, z0, x1, y0, z1, x1, y1, z1);
            w.TriMat(hull, x0, y0, z0, x1, y1, z1, x0, y1, z0);
            w.TriMat(hull, cx, y1, cz, x0, y1, z0, x1, y1, z1);
            w.TriMat(hull, cx, y0, cz, x1, y0, z1, x0, y0, z0);
        }
    }

    private static void AddDockingRing(RaceMeshWriter w, float cx, float cy, float cz, float innerR, float outerR, int segments)
    {
        var truss = RaceMeshWriter.HullMaterial.Truss;
        for (int i = 0; i < segments; i++)
        {
            float a0 = MathF.PI * 2f * i / segments;
            float a1 = MathF.PI * 2f * (i + 1) / segments;
            float xi0 = cx + MathF.Cos(a0) * innerR;
            float zi0 = cz + MathF.Sin(a0) * innerR;
            float xi1 = cx + MathF.Cos(a1) * innerR;
            float zi1 = cz + MathF.Sin(a1) * innerR;
            float xo0 = cx + MathF.Cos(a0) * outerR;
            float zo0 = cz + MathF.Sin(a0) * outerR;
            float xo1 = cx + MathF.Cos(a1) * outerR;
            float zo1 = cz + MathF.Sin(a1) * outerR;

            w.TriMat(truss, xo0, cy, zo0, xo1, cy, zo1, xi1, cy, zi1);
            w.TriMat(truss, xo0, cy, zo0, xi1, cy, zi1, xi0, cy, zi0);
            w.TriMat(truss, xo0, cy + outerR * 0.12f, zo0, xo1, cy + outerR * 0.12f, zo1, xo1, cy, zo1);
            w.TriMat(truss, xo0, cy + outerR * 0.12f, zo0, xo1, cy, zo1, xo0, cy, zo0);
        }
    }

    private static void AddNasaTrussSpine(RaceMeshWriter w, float hw, float hh, float z0, float z1, int segments)
    {
        var truss = RaceMeshWriter.HullMaterial.Truss;
        AddBoxMat(w, truss, -hw, hw, -hh * 0.12f, hh * 0.78f, z0, z1);

        for (int i = 0; i < segments; i++)
        {
            float z = MathHelper.Lerp(z0, z1, i / (float)Math.Max(1, segments - 1));
            float dz = (z1 - z0) / segments * 0.42f;
            float yMid = hh * 0.32f;
            w.TriMat(truss, -hw, yMid, z, hw, yMid, z, -hw, yMid, z + dz);
            w.TriMat(truss, -hw, yMid, z, hw, yMid, z + dz, hw, yMid, z);
            w.TriMat(truss, -hw * 0.55f, hh * 0.05f, z, hw * 0.55f, hh * 0.05f, z, -hw * 0.55f, -hh * 0.08f, z + dz);
            w.TriMat(truss, hw * 0.55f, hh * 0.05f, z, hw * 0.55f, -hh * 0.08f, z + dz, -hw * 0.55f, -hh * 0.08f, z + dz);
        }
    }

    private static void AddNasaSolarArray(RaceMeshWriter w, float x0, float x1, float y0, float y1, float z0, float z1, int rows, int cols)
    {
        var solar = RaceMeshWriter.HullMaterial.Solar;
        var truss = RaceMeshWriter.HullMaterial.Truss;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                float u0 = col / (float)cols;
                float u1 = (col + 1) / (float)cols;
                float v0 = row / (float)rows;
                float v1 = (row + 1) / (float)rows;
                float xa = MathHelper.Lerp(x0, x1, u0);
                float xb = MathHelper.Lerp(x0, x1, u1);
                float ya = MathHelper.Lerp(y0, y1, v0);
                float yb = MathHelper.Lerp(y0, y1, v1);

                w.TriMat(solar, xa, ya, z0, xb, ya, z0, xb, yb, z0);
                w.TriMat(solar, xa, ya, z0, xb, yb, z0, xa, yb, z0);
                w.TriMat(solar, xa, ya, z1, xb, yb, z1, xb, ya, z1);
                w.TriMat(solar, xa, ya, z1, xa, yb, z1, xb, yb, z1);
            }
        }

        for (int row = 0; row <= rows; row++)
        {
            float v = row / (float)rows;
            float y = MathHelper.Lerp(y0, y1, v);
            w.TriMat(truss, x0, y, z0, x1, y, z0, x1, y, z1);
            w.TriMat(truss, x0, y, z0, x1, y, z1, x0, y, z1);
        }

        for (int col = 0; col <= cols; col++)
        {
            float u = col / (float)cols;
            float x = MathHelper.Lerp(x0, x1, u);
            w.TriMat(truss, x, y0, z0, x, y1, z0, x, y1, z1);
            w.TriMat(truss, x, y0, z0, x, y1, z1, x, y0, z1);
        }
    }

    private static void AddMatBridge(RaceMeshWriter w, float xHull, float xPanel, float y0, float y1, float z)
    {
        var truss = RaceMeshWriter.HullMaterial.Truss;
        float depth = MathF.Abs(xPanel - xHull) * 0.08f;
        w.TriMat(truss, xHull, y0, z - depth, xPanel, y0, z - depth, xPanel, y1, z - depth);
        w.TriMat(truss, xHull, y0, z - depth, xPanel, y1, z - depth, xHull, y1, z - depth);
        w.TriMat(truss, xHull, y0, z + depth, xPanel, y1, z + depth, xPanel, y0, z + depth);
        w.TriMat(truss, xHull, y0, z + depth, xHull, y1, z + depth, xPanel, y1, z + depth);
        w.TriMat(truss, xHull, y0, z - depth, xHull, y0, z + depth, xPanel, y0, z + depth);
        w.TriMat(truss, xHull, y0, z - depth, xPanel, y0, z + depth, xPanel, y0, z - depth);
    }

    private static void AddRadiatorPanel(RaceMeshWriter w, float x0, float x1, float y0, float y1, float z0, float z1)
    {
        var radiator = RaceMeshWriter.HullMaterial.Radiator;
        int fins = 4;
        for (int f = 0; f < fins; f++)
        {
            float t = f / (float)Math.Max(1, fins - 1);
            float x = MathHelper.Lerp(x0, x1, t);
            float xn = MathHelper.Lerp(x0, x1, (f + 1) / (float)fins);
            w.TriMat(radiator, x, y0, z0, xn, y0, z0, xn, y1, z0);
            w.TriMat(radiator, x, y0, z0, xn, y1, z0, x, y1, z0);
            w.TriMat(radiator, x, y0, z1, xn, y1, z1, xn, y0, z1);
            w.TriMat(radiator, x, y0, z1, x, y1, z1, xn, y1, z1);
            w.TriMat(radiator, x, y0, z0, x, y0, z1, xn, y0, z1);
            w.TriMat(radiator, x, y0, z0, xn, y0, z1, xn, y0, z0);
        }
    }

    private static void AddNasaCupola(RaceMeshWriter w, float cx, float cy, float cz, float radius, float height, int segments)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        segments = Math.Max(6, segments);
        for (int i = 0; i < segments; i++)
        {
            float a0 = MathF.PI * 2f * i / segments;
            float a1 = MathF.PI * 2f * (i + 1) / segments;
            float x0 = cx + MathF.Cos(a0) * radius;
            float z0 = cz + MathF.Sin(a0) * radius;
            float x1 = cx + MathF.Cos(a1) * radius;
            float z1 = cz + MathF.Sin(a1) * radius;
            w.TriMat(hull, cx, cy + height, cz, x0, cy, z0, x1, cy, z1);
            w.TriMat(hull, cx, cy, cz, x0, cy - height * 0.15f, z0, x1, cy - height * 0.15f, z1);
        }
    }

    private static void AddCommunicationsMast(RaceMeshWriter w, float cx, float baseY, float cz, float height, float radius)
    {
        var truss = RaceMeshWriter.HullMaterial.Truss;
        float hw = radius;
        float topY = baseY + height;
        AddBoxMat(w, truss, cx - hw, cx + hw, baseY, topY, cz - hw, cz + hw);

        for (int i = 0; i < 3; i++)
        {
            float angle = MathF.PI * 2f * i / 3f + 0.4f;
            float dx = MathF.Cos(angle) * radius * 2.8f;
            float dz = MathF.Sin(angle) * radius * 2.8f;
            w.TriMat(truss, cx, topY, cz, cx + dx, topY - radius * 0.6f, cz + dz, cx + dx * 0.55f, topY - radius * 1.4f, cz + dz * 0.55f);
            w.TriMat(truss, cx, topY, cz, cx + dx * 0.55f, topY - radius * 1.4f, cz + dz * 0.55f, cx + dx * 0.2f, topY - radius * 0.9f, cz + dz * 0.2f);
        }
    }

    private static void AddBoxMat(RaceMeshWriter w, RaceMeshWriter.HullMaterial mat, float x0, float x1, float y0, float y1, float z0, float z1)
    {
        w.TriMat(mat, x0, y0, z0, x1, y0, z0, x1, y1, z0);
        w.TriMat(mat, x0, y0, z0, x1, y1, z0, x0, y1, z0);
        w.TriMat(mat, x0, y0, z1, x1, y1, z1, x1, y0, z1);
        w.TriMat(mat, x0, y0, z1, x0, y1, z1, x1, y1, z1);
        w.TriMat(mat, x0, y0, z0, x0, y0, z1, x1, y0, z1);
        w.TriMat(mat, x0, y0, z0, x1, y0, z1, x1, y0, z0);
        w.TriMat(mat, x0, y1, z0, x1, y1, z1, x0, y1, z1);
        w.TriMat(mat, x0, y1, z0, x1, y1, z0, x1, y1, z1);
        w.TriMat(mat, x0, y0, z0, x0, y1, z0, x0, y1, z1);
        w.TriMat(mat, x0, y0, z0, x0, y1, z1, x0, y0, z1);
        w.TriMat(mat, x1, y0, z0, x1, y0, z1, x1, y1, z1);
        w.TriMat(mat, x1, y0, z0, x1, y1, z1, x1, y1, z0);
    }

    // â”€â”€ Primitives â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static void AddBox(RaceMeshWriter w, float x0, float x1, float y0, float y1, float z0, float z1)
    {
        w.Tri(x0, y0, z0, x1, y0, z0, x1, y1, z0);
        w.Tri(x0, y0, z0, x1, y1, z0, x0, y1, z0);
        w.Tri(x0, y0, z1, x1, y1, z1, x1, y0, z1);
        w.Tri(x0, y0, z1, x0, y1, z1, x1, y1, z1);
        w.Tri(x0, y0, z0, x0, y0, z1, x1, y0, z1);
        w.Tri(x0, y0, z0, x1, y0, z1, x1, y0, z0);
        w.Tri(x0, y1, z0, x1, y1, z1, x0, y1, z1);
        w.Tri(x0, y1, z0, x1, y1, z0, x1, y1, z1);
        w.Tri(x0, y0, z0, x0, y1, z0, x0, y1, z1);
        w.Tri(x0, y0, z0, x0, y1, z1, x0, y0, z1);
        w.Tri(x1, y0, z0, x1, y0, z1, x1, y1, z1);
        w.Tri(x1, y0, z0, x1, y1, z1, x1, y1, z0);
    }

    private static float MassScale(string hullKey) => hullKey switch
    {
        "dreadnought" => 1.25f,
        "carrier" or "carrier_command" => 1.15f,
        "cruiser" or "cruiser_heavy" => 1.05f,
        "destroyer" or "frigate" or "gunship" or "bomber" or "corvette" => 0.95f,
        "drone" or "drone_swarm" or "scout" or "scout_light" => 0.55f,
        _ => 1f,
    };
}
