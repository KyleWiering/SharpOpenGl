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
            case "organic":     BuildBulbous(w, hullKey, len, wid, hgt); break;
            case "asymmetric":  BuildLatticeFrame(w, hullKey, len, wid, hgt); break;
            case "radiant":     BuildRadiantDeck(w, hullKey, len, wid, hgt); break;
            case "spiny":       BuildSpinyHull(w, hullKey, len, wid, hgt); break;
            case "crystalline": BuildCrystallineHull(w, hullKey, len, wid, hgt, race.Modifiers.FacetSharpness); break;
            default:            BuildAngularWedge(w, hullKey, len, wid, hgt); break;
        }
    }


    // â”€â”€ Retro (Terran) â€” 80s white slab hulls, stacked bridge boxes â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static void BuildRetroBlocky(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float l = len * 0.9f;
        float bw = wid * 1.2f;
        float bh = hgt * 0.95f;

        AddBox(w, -bw * 0.5f, bw * 0.5f, 0, bh * 0.55f, -l * 0.5f, l * 0.45f);
        AddBox(w, -bw * 0.35f, bw * 0.35f, bh * 0.55f, bh * 0.95f, l * 0.02f, l * 0.38f);
        AddBox(w, -bw * 0.22f, bw * 0.22f, bh * 0.95f, bh * 1.25f, l * 0.12f, l * 0.28f);

        if (hullKey is not "scout" and not "drone")
        {
            AddBox(w, -bw * 0.62f, -bw * 0.38f, bh * 0.2f, bh * 0.5f, -l * 0.08f, l * 0.12f);
            AddBox(w, bw * 0.38f, bw * 0.62f, bh * 0.2f, bh * 0.5f, -l * 0.08f, l * 0.12f);
        }

        AddBox(w, -bw * 0.15f, bw * 0.15f, bh * 0.35f, bh * 0.65f, l * 0.42f, l * 0.58f);
    }

    // â”€â”€ Vasudan (Vesper) â€” 90s elongated reptilian hull, dorsal keel â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static void BuildVasudanHull(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        if (hullKey is "fighter" or "fighter_basic")
        {
            BuildVasudanFighterSolid(w, len, wid, hgt);
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
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var engine = RaceMeshWriter.HullMaterial.Engine;

        float hw = wid * 0.5f;

        AddBoxMat(w, hull, -hw * 0.50f, hw * 0.50f, 0, hgt * 0.22f, -len * 0.08f, len * 0.16f);
        AddBoxMat(w, hull, -hw * 0.36f, hw * 0.36f, hgt * 0.08f, hgt * 0.42f, -len * 0.04f, len * 0.28f);
        AddBoxMat(w, frame, -hw * 0.24f, hw * 0.24f, hgt * 0.40f, hgt * 0.54f, len * 0.02f, len * 0.22f);
        AddBoxMat(w, panel, -hw * 0.12f, hw * 0.12f, 0, hgt * 0.06f, len * 0.10f, len * 0.20f);

        AddVasudanFighterNose(w, hull, frame, canopy, hw, hgt, len);
        AddVasudanFighterCanopy(w, canopy, frame, hw, hgt, len);
        AddVasudanModernWing(w, hull, frame, canopy, hw, hgt, len, -1f);
        AddVasudanModernWing(w, hull, frame, canopy, hw, hgt, len, 1f);

        AddBoxMat(w, frame, -hw * 0.07f, hw * 0.07f, hgt * 0.38f, hgt * 0.62f, -len * 0.08f, -len * 0.01f);
        AddVasudanFighterIntakes(w, hull, panel, hw, hgt, len);
        AddVasudanFighterTail(w, hull, frame, canopy, hw, hgt, len);
        AddVasudanEngineNacelle(w, hull, engine, canopy, -hw * 0.18f, hw * 0.06f, hgt, len);
        AddVasudanEngineNacelle(w, hull, engine, canopy, hw * 0.06f, hw * 0.18f, hgt, len);

        AddBoxMat(w, panel, -hw * 0.09f, hw * 0.09f, hgt * 0.02f, hgt * 0.10f, len * 0.14f, len * 0.20f);
        for (int m = 0; m < 4; m++)
        {
            float mz = MathHelper.Lerp(-len * 0.06f, len * 0.10f, m / 3f);
            float inset = hw * (0.22f - m * 0.02f);
            AddBoxMat(w, RaceMeshWriter.HullMaterial.Weapon, -inset, -inset + hw * 0.04f, 0, hgt * 0.04f, mz, mz + len * 0.012f);
            AddBoxMat(w, RaceMeshWriter.HullMaterial.Weapon, inset - hw * 0.04f, inset, 0, hgt * 0.04f, mz, mz + len * 0.012f);
        }
        AddVasudanFuselagePanels(w, frame, panel, hw, hgt, len);
        AddVasudanFighterHardpoints(w, hull, frame, engine, RaceMeshWriter.HullMaterial.Weapon,
            RaceMeshWriter.HullMaterial.ShieldGen, hw, hgt, len);
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

    private static void AddVasudanFighterNose(RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial frame, RaceMeshWriter.HullMaterial canopy, float hw, float hgt, float len)
    {
        float baseZ = len * 0.29f;
        float tipZ = len * 0.538f;
        float baseW = hw * 0.22f;
        float midZ = len * 0.405f;
        float midW = hw * 0.12f;

        w.TriMat(hull, -baseW, hgt * 0.12f, baseZ, baseW, hgt * 0.12f, baseZ, 0, hgt * 0.44f, midZ);
        w.TriMat(hull, -baseW, hgt * 0.12f, baseZ, 0, hgt * 0.44f, midZ, -midW, hgt * 0.28f, midZ);
        w.TriMat(hull, baseW, hgt * 0.12f, baseZ, midW, hgt * 0.28f, midZ, 0, hgt * 0.44f, midZ);
        w.TriMat(hull, -midW, hgt * 0.28f, midZ, 0, hgt * 0.44f, midZ, midW, hgt * 0.28f, midZ);
        w.TriMat(hull, -midW, hgt * 0.28f, midZ, 0, hgt * 0.54f, tipZ, midW, hgt * 0.28f, midZ);
        w.TriMat(hull, -baseW, 0, baseZ, 0, hgt * 0.12f, tipZ * 0.98f, baseW, 0, baseZ);
        w.TriMat(hull, -baseW, 0, baseZ, -baseW, hgt * 0.12f, baseZ, 0, hgt * 0.12f, tipZ * 0.98f);
        w.TriMat(hull, baseW, 0, baseZ, 0, hgt * 0.12f, tipZ * 0.98f, baseW, hgt * 0.12f, baseZ);
        w.TriMat(frame, -hw * 0.06f, hgt * 0.46f, midZ, hw * 0.06f, hgt * 0.46f, midZ, 0, hgt * 0.50f, tipZ * 0.92f);
        w.TriMat(canopy, -hw * 0.04f, hgt * 0.48f, tipZ * 0.88f, hw * 0.04f, hgt * 0.48f, tipZ * 0.88f, 0, hgt * 0.52f, tipZ * 0.95f);
    }

    private static void AddVasudanFighterCanopy(RaceMeshWriter w, RaceMeshWriter.HullMaterial canopy, RaceMeshWriter.HullMaterial frame, float hw, float hgt, float len)
    {
        AddBoxMat(w, canopy, -hw * 0.15f, hw * 0.15f, hgt * 0.46f, hgt * 0.78f, len * 0.10f, len * 0.22f);
        AddBoxMat(w, frame, -hw * 0.17f, hw * 0.17f, hgt * 0.44f, hgt * 0.48f, len * 0.08f, len * 0.24f);
        AddBoxMat(w, frame, -hw * 0.12f, hw * 0.12f, hgt * 0.72f, hgt * 0.76f, len * 0.12f, len * 0.20f);
    }

    private static void AddVasudanModernWing(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial hull, RaceMeshWriter.HullMaterial frame,
        RaceMeshWriter.HullMaterial accent, float hw, float hgt, float len, float side)
    {
        float s = MathF.Sign(side);
        if (s == 0) return;

        float rootIn = hw * 0.28f;
        float rootOut = hw * 1.12f;
        float midOut = hw * 0.94f;
        float tipOut = hw * 0.80f;
        float rootZ = len * 0.14f;
        float midZ = len * 0.02f;
        float tipZ = -len * 0.04f;
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
        RaceMeshWriter.HullMaterial ring, float x0, float x1, float hgt, float len)
    {
        float podH = hgt * 0.22f;
        float podW = x1 - x0;
        AddBoxMat(w, hull, x0, x1, 0, podH * 0.72f, -len * 0.15f, -len * 0.04f);
        AddBoxMat(w, engine, x0 + podW * 0.18f, x1 - podW * 0.18f, podH * 0.06f, podH * 0.48f, -len * 0.14f, -len * 0.08f);
        AddBoxMat(w, ring, x0 + podW * 0.10f, x1 - podW * 0.10f, podH * 0.50f, podH * 0.62f, -len * 0.12f, -len * 0.09f);
        AddBoxMat(w, engine, x0 + podW * 0.26f, x1 - podW * 0.26f, 0, podH * 0.14f, -len * 0.16f, -len * 0.13f);
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
        float finZ = -len * 0.06f;
        AddBoxMat(w, hull, -hw * 0.06f, hw * 0.06f, hgt * 0.48f, hgt * 0.66f, finZ, -len * 0.01f);
        for (int side = -1; side <= 1; side += 2)
        {
            float xIn = side * hw * 0.08f;
            float xOut = side * hw * 0.22f;
            AddBoxMat(w, hull, MathF.Min(xIn, xOut), MathF.Max(xIn, xOut), hgt * 0.20f, hgt * 0.42f, finZ, -len * 0.02f);
            AddBoxMat(w, accent, MathF.Min(xIn, xOut), MathF.Max(xIn, xOut), hgt * 0.38f, hgt * 0.44f, finZ - len * 0.01f, finZ + len * 0.01f);
        }
        AddBoxMat(w, frame, -hw * 0.04f, hw * 0.04f, hgt * 0.64f, hgt * 0.68f, finZ - len * 0.01f, finZ + len * 0.01f);
    }

    // ── Orbital truss (Korath) — late-90s NASA stacked modules on filled truss spine ─

    private static void BuildOrbitalTruss(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float mass = MassScale(hullKey);
        float l = len * (0.88f + mass * 0.08f);
        float hw = wid * 0.36f;
        float hh = hgt * 0.4f;
        bool compact = hullKey is "drone" or "scout";
        bool capital = hullKey is "dreadnought" or "carrier" or "cruiser";
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

    // â”€â”€ Bulbous / organic (Aetherian) â€” smooth bio-loft with integrated pod bulges â”€

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

    // â”€â”€ Lattice frame (Nexar) â€” solid asymmetric hull with embossed ring bands â”€

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

    // â”€â”€ Radiant (Solari) â€” lofted fuselage under wide solar crown deck â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static void BuildRadiantDeck(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
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

        int panelRows = hullKey is "dreadnought" or "carrier" ? 3 : 2;
        int panelCols = hullKey is "dreadnought" or "carrier" ? 5 : 3;
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

    // â”€â”€ Spiny (Voidborn) â€” smooth carapace spine with surface-mounted spines â”€â”€â”€

    private static void BuildSpinyHull(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
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

    // â”€â”€ Crystalline (Cryo) â€” closed faceted gem hull with mid-band and stern â”€â”€â”€

    private static void BuildCrystallineHull(RaceMeshWriter w, string hullKey, float len, float wid, float hgt, float sharpness)
    {
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
        if (hullKey is "drone" or "scout") return compact;
        if (hullKey is "dreadnought" or "carrier" or "cruiser") return capital;
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
        "carrier" => 1.15f,
        "cruiser" => 1.05f,
        "destroyer" or "frigate" or "gunship" => 0.95f,
        "drone" or "scout" => 0.55f,
        _ => 1f,
    };
}
