using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Race-specific primary hull silhouettes — each style replaces the shared fighter wedge
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
            case "sleek":       BuildStreamlined(w, hullKey, len, wid, hgt); break;
            case "organic":     BuildBulbous(w, hullKey, len, wid, hgt); break;
            case "asymmetric":  BuildLatticeFrame(w, hullKey, len, wid, hgt); break;
            case "radiant":     BuildRadiantDeck(w, hullKey, len, wid, hgt); break;
            case "spiny":       BuildSpinyHull(w, hullKey, len, wid, hgt); break;
            case "crystalline": BuildCrystallineHull(w, hullKey, len, wid, hgt, race.Modifiers.FacetSharpness); break;
            default:            BuildAngularWedge(w, hullKey, len, wid, hgt); break;
        }
    }


    // ── Retro (Terran) — 80s white slab hulls, stacked bridge boxes ───────────

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

    // ── Vasudan (Vesper) — 90s elongated reptilian hull, dorsal keel ──────────

    private static void BuildVasudanHull(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float l = len * 1.15f;
        float bw = wid * 0.58f;
        float bh = hgt * 0.72f;
        int sections = hullKey is "dreadnought" or "carrier" ? 8 : 6;

        for (int i = 0; i < sections; i++)
        {
            float t0 = i / (float)sections;
            float t1 = (i + 1) / (float)sections;
            float z0 = MathHelper.Lerp(-l * 0.55f, l * 0.82f, t0);
            float z1 = MathHelper.Lerp(-l * 0.55f, l * 0.82f, t1);
            float belly = 0.55f + 0.45f * MathF.Sin(t0 * MathF.PI);
            float belly1 = 0.55f + 0.45f * MathF.Sin(t1 * MathF.PI);
            float r0 = bw * belly * (t0 > 0.82f ? (1f - t0) / 0.18f : 1f);
            float r1 = bw * belly1 * (t1 > 0.82f ? (1f - t1) / 0.18f : 1f);

            w.Tri(-r0, bh * 0.35f, z0, r0, bh * 0.35f, z0, 0, bh * 0.72f, z0 + (z1 - z0) * 0.45f);
            w.Tri(-r0, bh * 0.35f, z0, 0, bh * 0.72f, z0 + (z1 - z0) * 0.45f, -r1, bh * 0.35f, z1);
            w.Tri(r0, bh * 0.35f, z0, r1, bh * 0.35f, z1, 0, bh * 0.72f, z0 + (z1 - z0) * 0.45f);
            w.Tri(-r0, 0, z0, -r1, 0, z1, r0, 0, z0);
            w.Tri(r0, 0, z0, -r1, 0, z1, r1, 0, z1);
        }

        for (int i = 0; i < sections - 1; i++)
        {
            float t = (i + 0.5f) / sections;
            float z = MathHelper.Lerp(-l * 0.55f, l * 0.82f, t);
            w.Tri(0, bh * 0.78f, z, -bw * 0.08f, bh * 0.62f, z - bw * 0.12f, bw * 0.08f, bh * 0.62f, z - bw * 0.12f);
        }

        if (hullKey is not "drone")
        {
            w.Tri(-bw * 0.42f, bh * 0.28f, -l * 0.15f, bw * 0.42f, bh * 0.28f, -l * 0.15f, 0, bh * 0.42f, -l * 0.28f);
        }
    }
    // ── Blocky (Korath) — stacked rectangular decks, flat armor faces ─────────

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

    // ── Streamlined (Vesper) — narrow needle fuselage, low profile ────────────

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

    // ── Bulbous / organic (Aetherian) — rounded pods, curved belly ────────────

    private static void BuildBulbous(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float l = len * 0.88f;
        float bw = wid * 1.05f;
        float bh = hgt * 1.35f;
        int segments = hullKey is "dreadnought" or "carrier" ? 14 : 10;

        for (int i = 0; i < segments; i++)
        {
            float a0 = MathF.PI * 2f * i / segments;
            float a1 = MathF.PI * 2f * (i + 1) / segments;
            float zMid = l * 0.08f;
            float rx = bw * 0.42f;
            float rz = l * 0.38f;

            var p0 = new Vector3(MathF.Cos(a0) * rx, bh * 0.25f + MathF.Sin(a0) * bh * 0.15f, zMid + MathF.Sin(a0) * rz * 0.2f);
            var p1 = new Vector3(MathF.Cos(a1) * rx, bh * 0.25f + MathF.Sin(a1) * bh * 0.15f, zMid + MathF.Sin(a1) * rz * 0.2f);
            var top = new Vector3(0, bh * 0.85f, zMid + l * 0.12f);
            var bot = new Vector3(0, bh * 0.05f, zMid - l * 0.08f);
            w.Tri(p0.X, p0.Y, p0.Z, p1.X, p1.Y, p1.Z, top.X, top.Y, top.Z);
            w.Tri(p0.X, p0.Y, p0.Z, bot.X, bot.Y, bot.Z, p1.X, p1.Y, p1.Z);
        }

        int pods = hullKey is "miner" or "support" or "transport" ? 4 : 3;
        for (int p = 0; p < pods; p++)
        {
            float side = (p - (pods - 1) * 0.5f) * bw * 0.38f;
            float z = -l * 0.12f + p * l * 0.18f;
            AddBulb(w, side, bh * 0.35f, z, bw * 0.18f, bh * 0.28f);
        }

        w.Tri(0, bh * 0.55f, l * 0.55f, -bw * 0.22f, bh * 0.2f, l * 0.35f, bw * 0.22f, bh * 0.2f, l * 0.35f);
        w.Tri(0, bh * 0.55f, l * 0.55f, 0, bh * 0.35f, l * 0.72f, -bw * 0.12f, bh * 0.25f, l * 0.42f);
    }

    private static void AddBulb(RaceMeshWriter w, float cx, float cy, float cz, float rx, float ry)
    {
        for (int i = 0; i < 6; i++)
        {
            float a0 = MathF.PI * 2f * i / 6f;
            float a1 = MathF.PI * 2f * (i + 1) / 6f;
            w.Tri(cx, cy + ry, cz, cx + MathF.Cos(a0) * rx, cy, cz + MathF.Sin(a0) * rx * 0.5f, cx + MathF.Cos(a1) * rx, cy, cz + MathF.Sin(a1) * rx * 0.5f);
        }
    }

    // ── Lattice frame (Nexar) — open truss grid with central pod ──────────────

    private static void BuildLatticeFrame(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float l = len;
        float hw = wid * 0.62f;
        float hh = hgt * 0.72f;
        int rings = hullKey is "dreadnought" or "carrier" ? 8 : 6;

        for (int r = 0; r < rings; r++)
        {
            float z = MathHelper.Lerp(-l * 0.58f, l * 0.58f, r / (float)(rings - 1));
            AddRing(w, hw, hh, z, 0.08f);
            if (r < rings - 1)
            {
                float z2 = MathHelper.Lerp(-l * 0.58f, l * 0.58f, (r + 1) / (float)(rings - 1));
                ConnectCorners(w, hw, hh, z, z2);
            }
        }

        AddStrut(w, -hw, hh, -l * 0.5f, hw, hh, l * 0.5f);
        AddStrut(w, -hw, -hh * 0.1f, -l * 0.5f, hw, -hh * 0.1f, l * 0.5f);
        AddStrut(w, -hw, hh, -l * 0.5f, -hw, -hh * 0.1f, l * 0.5f);
        AddStrut(w, hw, hh, -l * 0.5f, hw, -hh * 0.1f, l * 0.5f);

        AddBox(w, -hw * 0.22f, hw * 0.22f, -hh * 0.02f, hh * 0.45f, -l * 0.08f, l * 0.12f);

        if (hullKey is not "drone")
        {
            float skew = hw * 0.55f;
            AddStrut(w, skew, hh * 0.9f, l * 0.35f, -hw * 0.2f, hh * 0.3f, -l * 0.25f);
            AddStrut(w, -skew * 0.6f, hh * 0.5f, l * 0.45f, hw * 0.3f, -hh * 0.05f, -l * 0.35f);
        }
    }

    private static void AddRing(RaceMeshWriter w, float hw, float hh, float z, float thick)
    {
        w.Tri(-hw, hh, z, hw, hh, z, hw, hh, z + thick);
        w.Tri(-hw, hh, z, hw, hh, z + thick, -hw, hh, z + thick);
        w.Tri(-hw, -hh * 0.05f, z, hw, -hh * 0.05f, z + thick, hw, -hh * 0.05f, z);
        w.Tri(-hw, -hh * 0.05f, z, -hw, -hh * 0.05f, z + thick, hw, -hh * 0.05f, z + thick);
        AddStrut(w, -hw, hh, z, -hw, -hh * 0.05f, z);
        AddStrut(w, hw, hh, z, hw, -hh * 0.05f, z);
    }

    private static void ConnectCorners(RaceMeshWriter w, float hw, float hh, float z0, float z1)
    {
        AddStrut(w, -hw, hh, z0, -hw, hh, z1);
        AddStrut(w, hw, hh, z0, hw, hh, z1);
        AddStrut(w, -hw, -hh * 0.05f, z0, -hw, -hh * 0.05f, z1);
        AddStrut(w, hw, -hh * 0.05f, z0, hw, -hh * 0.05f, z1);
        AddStrut(w, -hw, hh, z0, hw, -hh * 0.05f, z1);
        AddStrut(w, hw, hh, z0, -hw, -hh * 0.05f, z1);
    }

    private static void AddStrut(RaceMeshWriter w, float x0, float y0, float z0, float x1, float y1, float z1)
    {
        w.Tri(x0, y0, z0, x1, y1, z1, x0, y0 * 0.85f, z0 + (z1 - z0) * 0.15f);
    }

    // ── Radiant (Solari) — wide flat solar deck, crown bridge ───────────────────

    private static void BuildRadiantDeck(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float l = len * 0.9f;
        float span = wid * 1.35f;
        float deckH = hgt * 0.28f;

        w.Tri(-span * 0.5f, deckH, l * 0.35f, span * 0.5f, deckH, l * 0.35f, 0, deckH * 1.6f, -l * 0.15f);
        w.Tri(-span * 0.5f, deckH, l * 0.35f, 0, deckH * 1.6f, -l * 0.15f, -span * 0.5f, deckH, -l * 0.35f);
        w.Tri(span * 0.5f, deckH, l * 0.35f, span * 0.5f, deckH, -l * 0.35f, 0, deckH * 1.6f, -l * 0.15f);
        w.Tri(-span * 0.5f, 0, l * 0.35f, span * 0.5f, 0, l * 0.35f, span * 0.5f, 0, -l * 0.35f);
        w.Tri(-span * 0.5f, 0, l * 0.35f, span * 0.5f, 0, -l * 0.35f, -span * 0.5f, 0, -l * 0.35f);

        int panels = hullKey is "dreadnought" or "carrier" ? 5 : 3;
        for (int i = 0; i < panels; i++)
        {
            float x = (i - (panels - 1) * 0.5f) * span * 0.38f;
            w.Tri(x, deckH, l * 0.4f, x + span * 0.12f, deckH * 1.8f, l * 0.15f, x - span * 0.06f, deckH, -l * 0.2f);
        }

        AddBox(w, -wid * 0.15f, wid * 0.15f, deckH, deckH + hgt * 0.9f, l * 0.1f, l * 0.45f);
    }

    // ── Spiny (Voidborn) — central spine with outward spines ────────────────────

    private static void BuildSpinyHull(RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float l = len;
        w.Tri(0, hgt * 0.45f, l * 0.5f, -wid * 0.28f, 0, 0, wid * 0.28f, 0, 0);
        w.Tri(-wid * 0.28f, 0, 0, wid * 0.28f, 0, 0, 0, 0, -l * 0.45f);

        int spines = hullKey is "dreadnought" or "carrier" ? 4 : 3;
        for (int i = 0; i < spines; i++)
        {
            float t = i / (float)Math.Max(1, spines - 1);
            float z = MathHelper.Lerp(-l * 0.42f, l * 0.38f, t);
            float side = (i % 2 == 0 ? -1 : 1) * wid * (0.35f + (i % 3) * 0.12f);
            float tipY = hgt * (0.55f + (i % 3) * 0.08f);
            w.Tri(side * 0.25f, hgt * 0.2f, z, side, tipY, z + wid * 0.04f, side * 0.32f, hgt * 0.12f, z - wid * 0.05f);
        }
    }

    // ── Crystalline (Cryo) — faceted gem hull ───────────────────────────────────

    private static void BuildCrystallineHull(RaceMeshWriter w, string hullKey, float len, float wid, float hgt, float sharpness)
    {
        float l = len;
        int facets = Math.Clamp(6 + (int)(sharpness * 6), 6, 12);
        float apex = hgt * (1.1f + sharpness * 0.4f);

        for (int i = 0; i < facets; i++)
        {
            float a0 = MathF.PI * 2f * i / facets;
            float a1 = MathF.PI * 2f * (i + 1) / facets;
            float r0 = wid * (0.35f + 0.1f * MathF.Sin(i * 1.7f));
            float r1 = wid * (0.35f + 0.1f * MathF.Sin((i + 1) * 1.7f));
            w.Tri(0, apex, l * 0.2f, MathF.Cos(a0) * r0, 0, MathF.Sin(a0) * l * 0.25f, MathF.Cos(a1) * r1, 0, MathF.Sin(a1) * l * 0.25f);
            w.Tri(MathF.Cos(a0) * r0, 0, MathF.Sin(a0) * l * 0.25f, MathF.Cos(a1) * r1, 0, MathF.Sin(a1) * l * 0.25f, 0, 0, -l * 0.5f);
        }
    }

    // ── Angular wedge (Terran) — classic delta-wing fighter silhouette ──────────

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

    // ── Primitives ──────────────────────────────────────────────────────────────

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