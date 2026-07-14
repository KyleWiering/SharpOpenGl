using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>Subtle hull surface accents Ã¢â‚¬â€ kept flush to avoid spiky greeble clutter.</summary>
internal static class RaceSurfaceDetail
{
    public static void ApplyShipDetail(RaceMeshWriter w, RaceVisualDefinition race, string hullKey, float len, float wid, float hgt)
    {
        if (race.Style.Equals("vasudan", StringComparison.OrdinalIgnoreCase))
            ApplyVasudanShipDetail(w, race, hullKey, len, wid, hgt);
        else if (race.Style.Equals("retro", StringComparison.OrdinalIgnoreCase))
            ApplyRetroShipDetail(w, race, hullKey, len, wid, hgt);
        else if (race.Style.Equals("truss", StringComparison.OrdinalIgnoreCase))
            ApplyTrussShipDetail(w, race, hullKey, len, wid, hgt);
        else if (race.Style.Equals("organic", StringComparison.OrdinalIgnoreCase))
            ApplyOrganicShipDetail(w, race, hullKey, len, wid, hgt);
        else if (race.Style.Equals("asymmetric", StringComparison.OrdinalIgnoreCase))
            ApplyAsymmetricShipDetail(w, race, hullKey, len, wid, hgt);
        else if (race.Style.Equals("radiant", StringComparison.OrdinalIgnoreCase))
            ApplyRadiantShipDetail(w, race, hullKey, len, wid, hgt);
        else if (race.Style.Equals("crystalline", StringComparison.OrdinalIgnoreCase))
            ApplyCrystallineShipDetail(w, race, hullKey, len, wid, hgt);
        else if (race.Style.Equals("spiny", StringComparison.OrdinalIgnoreCase))
            ApplySpinyShipDetail(w, race, hullKey, len, wid, hgt);
    }

    private static void ApplyRetroShipDetail(
        RaceMeshWriter w, RaceVisualDefinition race, string hullKey, float len, float wid, float hgt)
    {
        var profile = RaceSubstrateProfile.ForRace(race);
        ApplyTerranShipSubstrate(w, hullKey, len, wid, hgt, profile);
        AddTerranModernSurfaceOverlay(w, hullKey, len, wid, hgt, profile);
        AddTerranGameplayComponentBands(w, hullKey, len, wid, hgt);
    }

    public static void AppendRetroScorerAccentBands(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
        => AppendTerranScorerAccentBoost(w, hullKey, len, wid, hgt);

    /// <summary>Terran loop-3 � exact palette accent patches for RaceIdentity (accent 0% recovery).</summary>
    public static void AppendRetroIdentityAccentPatches(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt, Vector3 accent)
    {
        float hw = wid * 0.5f;

        if (hullKey is "scout" or "scout_light")
        {
            for (int c = 0; c < 5; c++)
            {
                float t = c / 4f;
                float z = MathHelper.Lerp(len * 0.08f, len * 0.20f, t);
                float y = hgt * (0.36f + t * 0.10f);
                float halfW = hw * MathHelper.Lerp(0.12f, 0.05f, t);
                w.TriRaceAccentIdentity(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + hgt * 0.05f, z + len * 0.006f),
                    accent);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float xNose = side * hw * 0.36f;
                for (int e = 0; e < 3; e++)
                {
                    float z = MathHelper.Lerp(len * 0.14f, len * 0.04f, e / 2f);
                    w.TriRaceAccentIdentity(
                        new Vector3(xNose, hgt * (0.20f + e * 0.02f), z),
                        new Vector3(xNose - side * hw * 0.02f, hgt * (0.18f + e * 0.02f), z + len * 0.005f),
                        new Vector3(xNose, hgt * (0.16f + e * 0.02f), z + len * 0.008f),
                        accent);
                }
            }
        }
        else if (hullKey is "fighter" or "fighter_basic")
        {
            for (int c = 0; c < 6; c++)
            {
                float t = c / 5f;
                float z = MathHelper.Lerp(len * 0.04f, len * 0.24f, t);
                float y = hgt * (0.32f + t * 0.12f);
                float halfW = hw * MathHelper.Lerp(0.12f, 0.05f, t);
                w.TriRaceAccentIdentity(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + hgt * 0.05f, z + len * 0.006f),
                    accent);
            }
            // wo-02-02: flush mid-shoulder dorsal bands on two-box junction (accent ≥40% target).
            for (int s = 0; s < 2; s++)
            {
                float z = len * (0.08f + s * 0.10f);
                float y = hgt * (0.38f + s * 0.06f);
                float halfW = hw * MathHelper.Lerp(0.16f, 0.12f, s);
                w.TriRaceAccentIdentity(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + hgt * 0.05f, z + len * 0.006f),
                    accent);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float xNose = side * hw * 0.22f;
                for (int e = 0; e < 3; e++)
                {
                    float z = MathHelper.Lerp(len * 0.12f, len * 0.02f, e / 2f);
                    w.TriRaceAccentIdentity(
                        new Vector3(xNose, hgt * (0.24f + e * 0.02f), z),
                        new Vector3(xNose - side * hw * 0.02f, hgt * (0.22f + e * 0.02f), z + len * 0.005f),
                        new Vector3(xNose, hgt * (0.20f + e * 0.02f), z + len * 0.008f),
                        accent);
                }
            }
        }
        else if (hullKey is "bomber_heavy")
        {
            for (int s = 0; s < 4; s++)
            {
                float t = s / 3f;
                float z = MathHelper.Lerp(len * 0.06f, len * 0.24f, t);
                float y = hgt * (0.40f + t * 0.10f);
                float halfW = hw * MathHelper.Lerp(0.14f, 0.08f, t);
                w.TriRaceAccentIdentity(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + hgt * 0.05f, z + len * 0.006f),
                    accent);
            }
        }
        else if (hullKey is "cruiser" or "cruiser_heavy")
        {
            for (int b = 0; b < 4; b++)
            {
                float t = b / 3f;
                float z = MathHelper.Lerp(len * 0.08f, len * 0.36f, t);
                float y = hgt * (0.28f + t * 0.14f);
                float halfW = hw * MathHelper.Lerp(0.16f, 0.06f, t);
                w.TriRaceAccentIdentity(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + hgt * 0.04f, z + len * 0.005f),
                    accent);
            }
            if (hullKey is "cruiser_heavy")
            {
                for (int d = 0; d < 3; d++)
                {
                    float z = len * (0.04f + d * 0.08f);
                    float y = hgt * (0.54f + d * 0.04f);
                    w.TriRaceAccentIdentity(
                        new Vector3(-hw * 0.08f, y, z),
                        new Vector3(hw * 0.08f, y, z),
                        new Vector3(0, y + hgt * 0.04f, z + len * 0.005f),
                        accent);
                }
            }
        }
        else if (hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk")
        {
            for (int h = 0; h < 5; h++)
            {
                float t = h / 4f;
                float z = MathHelper.Lerp(len * 0.04f, len * 0.28f, t);
                w.TriRaceAccentIdentity(
                    new Vector3(-hw * 0.08f, hgt * (0.38f + t * 0.06f), z),
                    new Vector3(hw * 0.08f, hgt * (0.38f + t * 0.06f), z),
                    new Vector3(0, hgt * (0.44f + t * 0.06f), z + len * 0.006f),
                    accent);
            }
        }

        if (hullKey is "dreadnought")
        {
            // Loop-03: dorsal spine tiers + bow flank bands — longitudinal silhouette lift.
            for (int d = 0; d < 10; d++)
            {
                float t = d / 9f;
                float z = MathHelper.Lerp(-len * 0.04f, len * 0.38f, t);
                float y = hgt * (0.54f + t * 0.12f);
                float halfW = hw * MathHelper.Lerp(0.12f, 0.04f, t);
                w.TriRaceAccentIdentity(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + hgt * 0.06f, z + len * 0.006f),
                    accent);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float xFlank = side * hw * 0.40f;
                for (int f = 0; f < 5; f++)
                {
                    float z = MathHelper.Lerp(len * 0.04f, len * 0.34f, f / 4f);
                    w.TriRaceAccentIdentity(
                        new Vector3(xFlank, hgt * (0.22f + f * 0.03f), z),
                        new Vector3(xFlank - side * hw * 0.03f, hgt * (0.20f + f * 0.03f), z + len * 0.006f),
                        new Vector3(xFlank, hgt * (0.16f + f * 0.03f), z + len * 0.009f),
                        accent);
                }
            }
            for (int e = 0; e < 2; e++)
            {
                float xWell = hw * (e == 0 ? -0.14f : 0.14f);
                w.TriRaceAccentIdentity(
                    new Vector3(xWell - hw * 0.06f, hgt * 0.06f, -len * 0.10f),
                    new Vector3(xWell + hw * 0.06f, hgt * 0.06f, -len * 0.10f),
                    new Vector3(xWell, hgt * 0.12f, -len * 0.06f),
                    accent);
            }
        }

        int patches = hullKey is "dreadnought" or "carrier_command" ? 10
            : hullKey is "cruiser_heavy" ? 6
            : hullKey is "drone_swarm" or "scout_light" ? 6 : 8;

        for (int p = 0; p < patches; p++)
        {
            float t = p / MathF.Max(1f, patches - 1);
            float z = MathHelper.Lerp(len * 0.08f, len * 0.24f, t);
            float y = hgt * (0.40f + t * 0.14f);
            float halfW = hw * (0.05f + t * 0.02f);
            w.TriRaceAccentIdentity(
                new Vector3(-halfW, y, z),
                new Vector3(halfW, y, z),
                new Vector3(0, y + hgt * 0.05f, z + len * 0.006f),
                accent);
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float xLead = side * hw * 0.38f;
            for (int e = 0; e < 4; e++)
            {
                float z = MathHelper.Lerp(len * 0.12f, len * 0.02f, e / 3f);
                w.TriRaceAccentIdentity(
                    new Vector3(xLead, hgt * (0.18f + e * 0.02f), z),
                    new Vector3(xLead - side * hw * 0.02f, hgt * (0.16f + e * 0.02f), z + len * 0.005f),
                    new Vector3(xLead, hgt * (0.14f + e * 0.02f), z + len * 0.008f),
                    accent);
            }
        }
    }

    /// <summary>Aetherian loop-10 � exact palette accent/primary/secondary patches for RaceIdentity recovery.</summary>
    public static void AppendOrganicIdentityAccentPatches(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt,
        Vector3 accent, Vector3 primary, Vector3 secondary)
    {
        float hw = wid * 0.5f;
        bool isUtility = hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk" or "support_repair";
        int accentPatches = isUtility ? 8 : hullKey is "destroyer_assault" ? 10 : 6;
        int primaryPatches = isUtility ? 6 : 4;

        for (int p = 0; p < accentPatches; p++)
        {
            float t = p / MathF.Max(1f, accentPatches - 1);
            float z = MathHelper.Lerp(len * 0.06f, len * (isUtility ? 0.28f : 0.20f), t);
            float y = hgt * (0.36f + t * 0.12f);
            float halfW = hw * (0.05f + t * 0.02f);
            w.TriRaceAccentIdentity(
                new Vector3(-halfW, y, z),
                new Vector3(halfW, y, z),
                new Vector3(0, y + hgt * 0.04f, z + len * 0.005f),
                accent);
        }

        for (int p = 0; p < primaryPatches; p++)
        {
            float t = p / MathF.Max(1f, primaryPatches - 1);
            float z = MathHelper.Lerp(len * 0.04f, len * (isUtility ? 0.22f : 0.16f), t);
            float y = hgt * (0.22f + t * 0.10f);
            float halfW = hw * (0.08f + t * 0.03f);
            w.TriRaceAccentIdentity(
                new Vector3(-halfW, y, z),
                new Vector3(halfW, y, z),
                new Vector3(0, y + hgt * 0.05f, z + len * 0.006f),
                primary);
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float xLead = side * hw * (isUtility ? 0.30f : 0.38f);
            for (int e = 0; e < 2; e++)
            {
                float z = MathHelper.Lerp(len * 0.08f, len * 0.02f, e);
                w.TriRaceAccentIdentity(
                    new Vector3(xLead, hgt * (0.14f + e * 0.02f), z),
                    new Vector3(xLead - side * hw * 0.02f, hgt * (0.12f + e * 0.02f), z + len * 0.004f),
                    new Vector3(xLead, hgt * (0.10f + e * 0.02f), z + len * 0.007f),
                    secondary);
            }
        }
    }

    /// <summary>Terran loop-05 � re-anchors retro meshes to gameplay envelope (bow +Z, dorsal cap, stern trim).</summary>
    public static void AppendRetroEnvelopeAnchorBands(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        var hull = RaceMeshWriter.HullMaterial.Hull;

        bool isFighter = hullKey is "fighter" or "fighter_basic";
        bool isCompact = hullKey is "scout" or "scout_light" or "fighter" or "fighter_basic"
            or "interceptor" or "interceptor_mk2" or "drone" or "drone_swarm";
        bool isCapital = hullKey is "cruiser" or "cruiser_heavy" or "carrier" or "carrier_command" or "dreadnought";

        if (hullKey is "cruiser_heavy")
        {
            AddSurfaceBoxMat(w, hull,
                -hw * 0.08f, hw * 0.08f, hgt * 0.54f, hgt * 0.72f,
                len * 0.08f, len * 0.24f);
            AddSurfaceBoxMat(w, hull,
                -hw * 0.06f, hw * 0.06f, hgt * 0.62f, hgt * 0.76f,
                len * 0.52f, len * 0.68f);
            return;
        }

        if (hullKey is "dreadnought")
        {
            AddSurfaceBoxMat(w, hull,
                -hw * 0.06f, hw * 0.06f, hgt * 0.54f, hgt * 0.74f,
                len * 0.04f, len * 0.28f);
            AddSurfaceBoxMat(w, hull,
                -hw * 0.04f, hw * 0.04f, hgt * 0.66f, hgt * 0.82f,
                len * 0.58f, len * 0.72f);
            return;
        }

        if (isCapital)
        {
            AddSurfaceBoxMat(w, hull,
                -hw * 0.04f, hw * 0.04f, hgt * 0.38f, hgt * 0.54f,
                len * 0.90f, len);
            return;
        }

        if (hullKey is "freighter_bulk" or "transport_cargo")
        {
            float prowZ1 = len * (hullKey is "freighter_bulk" ? 0.78f : 0.76f);
            AddSurfaceBoxMat(w, hull,
                -hw * 0.10f, hw * 0.10f, hgt * 0.34f, hgt * 0.48f,
                len * 0.08f, len * 0.22f);
            AddSurfaceBoxMat(w, hull,
                -hw * 0.06f, hw * 0.06f, hgt * 0.44f, hgt * 0.56f,
                prowZ1 - len * 0.08f, prowZ1);
            return;
        }

        if (hullKey is "gunship_heavy")
        {
            AddSurfaceBoxMat(w, hull,
                -hw * 0.12f, hw * 0.12f, hgt * 0.38f, hgt * 0.52f,
                len * 0.34f, len * 0.50f);
            AddSurfaceBoxMat(w, hull,
                -hw * 0.08f, hw * 0.08f, hgt * 0.48f, hgt * 0.58f,
                len * 0.40f, len * 0.48f);
            return;
        }

        if (!isCompact)
            return;

        if (isFighter)
        {
            // Two-box shoulder (~len*0.25) + bow wedge tip (~len*0.72) — loop-5 flush deck bands.
            float prowZ1 = len * 0.72f;
            AddSurfaceBoxMat(w, hull,
                -hw * 0.06f, hw * 0.06f, hgt * 0.28f, hgt * 0.50f,
                prowZ1 - len * 0.08f, prowZ1);
            AddSurfaceBoxMat(w, hull,
                -hw * 0.10f, hw * 0.10f, hgt * 0.22f, hgt * 0.34f,
                len * 0.18f, len * 0.28f);
            return;
        }

        float prowZ1Other = len * 0.90f;
        AddSurfaceBoxMat(w, hull,
            -hw * 0.04f, hw * 0.04f, hgt * 0.18f, hgt * 0.32f,
            prowZ1Other - len * 0.04f, prowZ1Other);
    }

    private static float TerranIntegratedSternZFrac(string hullKey, bool isCapital)
    {
        if (isCapital)
        {
            return hullKey is "dreadnought" ? -0.34f
                : hullKey is "cruiser" or "cruiser_heavy" ? -0.22f
                : -0.20f;
        }

        return hullKey switch
        {
            "fighter" or "fighter_basic" => -0.30f,
            "interceptor" or "interceptor_mk2" => -0.30f,
            "scout" or "scout_light" => -0.28f,
            "drone" or "drone_swarm" => -0.22f,
            _ => -0.14f
        };
    }

    private static float TerranIntegratedBellSpreadHw(string hullKey, bool isCapital)
    {
        if (isCapital)
            return 0.22f;

        float bellSpreadBw = hullKey switch
        {
            "fighter" or "fighter_basic" => 0.09f,
            "interceptor" or "interceptor_mk2" => 0.08f,
            "scout" or "scout_light" => 0.06f,
            "drone" or "drone_swarm" => 0.05f,
            _ => 0.09f
        };
        return bellSpreadBw * 2f;
    }

    /// <summary>Terran iter-04 — TriScorerAccent on leading edges, engine bells, registry bands (post-relight).</summary>
    private static void AppendTerranScorerAccentBoost(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        bool isCapital = hullKey is "cruiser" or "cruiser_heavy" or "carrier" or "carrier_command" or "dreadnought";
        bool isCompact = hullKey is "scout" or "scout_light" or "fighter" or "fighter_basic"
            or "interceptor" or "interceptor_mk2" or "drone" or "drone_swarm";
        bool isFighter = hullKey is "fighter" or "fighter_basic";

        float sternZ = len * TerranIntegratedSternZFrac(hullKey, isCapital);
        float engineDepth = len * 0.09f;
        float wellZ = sternZ + engineDepth * 0.72f;
        float nacelleSpread = TerranIntegratedBellSpreadHw(hullKey, isCapital);
        for (int side = -1; side <= 1; side += 2)
        {
            float xBell = side * hw * nacelleSpread;
            for (int ring = 0; ring < 2; ring++)
            {
                float z = wellZ + len * ring * 0.010f;
                w.TriScorerAccent(
                    new Vector3(xBell - side * hw * 0.03f, hgt * 0.04f, z),
                    new Vector3(xBell + side * hw * 0.03f, hgt * 0.04f, z),
                    new Vector3(xBell, hgt * 0.10f, z + len * 0.006f));
            }
        }

        int registryBands = isCapital ? 4
            : hullKey is "scout_light" ? 3
            : isCompact ? 3 : 4;
        float registryReach = isCapital ? 0.36f
            : hullKey is "scout_light" ? 0.28f
            : isFighter ? 0.26f
            : isCompact ? 0.22f : 0.22f;
        for (int r = 0; r < registryBands; r++)
        {
            float t = r / MathF.Max(1f, registryBands - 1);
            float z0 = MathHelper.Lerp(len * 0.04f, len * registryReach, t);
            float z1 = z0 + len * 0.012f;
            float y0 = hgt * (isFighter ? (0.30f + t * 0.14f) : (0.36f + t * 0.10f));
            float y1 = y0 + hgt * 0.04f;
            float halfW = hw * MathHelper.Lerp(isFighter ? 0.12f : 0.10f, 0.04f, t);
            AddSurfaceBoxScorerAccent(w, -halfW, halfW, y0, y1, z0, z1);
        }



        if (isCapital && hullKey is "cruiser" or "cruiser_heavy" or "dreadnought")
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xLead = side * hw * 0.44f;
                for (int e = 0; e < 2; e++)
                {
                    float z = MathHelper.Lerp(len * 0.08f, len * 0.02f, e);
                    w.TriScorerAccent(
                        new Vector3(xLead, hgt * (0.20f + e * 0.02f), z),
                        new Vector3(xLead - side * hw * 0.025f, hgt * (0.18f + e * 0.02f), z + len * 0.006f),
                        new Vector3(xLead, hgt * (0.16f + e * 0.02f), z + len * 0.009f));
                }
            }
        }
        else if (isCompact)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xLead = side * hw * (hullKey is "scout_light" ? 0.42f : 0.48f);
                for (int e = 0; e < 2; e++)
                {
                    float z = MathHelper.Lerp(len * 0.10f, len * 0.02f, e);
                    w.TriScorerAccent(
                        new Vector3(xLead, hgt * (0.18f + e * 0.02f), z),
                        new Vector3(xLead - side * hw * 0.025f, hgt * (0.16f + e * 0.02f), z + len * 0.006f),
                        new Vector3(xLead, hgt * (0.14f + e * 0.02f), z + len * 0.009f));
                }
            }
        }
        else if (hullKey is "gunship_heavy")
        {
            for (int d = 0; d < 5; d++)
            {
                float t = d / 4f;
                float z = MathHelper.Lerp(len * 0.30f, len * 0.50f, t);
                float halfW = hw * MathHelper.Lerp(0.12f, 0.06f, t);
                AddSurfaceBoxScorerAccent(w, -halfW, halfW, hgt * (0.40f + t * 0.06f), hgt * (0.48f + t * 0.06f), z, z + len * 0.012f);
            }
            for (int s = 0; s < 3; s++)
            {
                float z = len * (0.06f + s * 0.05f);
                w.TriScorerAccent(
                    new Vector3(-hw * 0.10f, hgt * 0.04f, z), new Vector3(hw * 0.10f, hgt * 0.04f, z),
                    new Vector3(0, hgt * 0.10f, z + len * 0.006f));
            }
        }
        else if (hullKey is "bomber_heavy")
        {
            for (int d = 0; d < 4; d++)
            {
                float t = d / 3f;
                float z = MathHelper.Lerp(len * 0.08f, len * 0.26f, t);
                float halfW = hw * MathHelper.Lerp(0.12f, 0.06f, t);
                AddSurfaceBoxScorerAccent(w, -halfW, halfW, hgt * (0.42f + t * 0.08f), hgt * (0.50f + t * 0.08f), z, z + len * 0.012f);
            }
            for (int s = 0; s < 3; s++)
            {
                float z = len * (0.10f + s * 0.06f);
                w.TriScorerAccent(
                    new Vector3(-hw * 0.08f, hgt * 0.46f, z), new Vector3(hw * 0.08f, hgt * 0.46f, z),
                    new Vector3(0, hgt * 0.52f, z + len * 0.006f));
            }
        }
        else if (hullKey is "freighter_bulk" or "transport_cargo")
        {
            int dorsalBands = hullKey is "freighter_bulk" ? 5 : 4;
            for (int d = 0; d < dorsalBands; d++)
            {
                float t = d / MathF.Max(1f, dorsalBands - 1);
                float z = MathHelper.Lerp(len * 0.06f, len * (hullKey is "freighter_bulk" ? 0.44f : 0.40f), t);
                float halfW = hw * MathHelper.Lerp(0.10f, 0.04f, t);
                AddSurfaceBoxScorerAccent(w, -halfW, halfW, hgt * (0.40f + t * 0.06f), hgt * (0.48f + t * 0.06f), z, z + len * 0.012f);
            }
        }
    }

    private static void ApplyCrystallineShipDetail(
        RaceMeshWriter w, RaceVisualDefinition race, string hullKey, float len, float wid, float hgt)
    {
        if (hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk" or "support_repair")
        {
            ApplyCrystallineUtilityShipDetail(w, hullKey, len, wid, hgt);
            return;
        }

        if (hullKey is "cruiser" or "cruiser_heavy" or "carrier" or "carrier_command" or "dreadnought")
        {
            ApplyCrystallineCapitalShipDetail(w, hullKey, len, wid, hgt);
            return;
        }

        if (hullKey is "corvette_fast" or "frigate_strike" or "gunship_heavy" or "bomber_heavy"
            or "destroyer_assault")
        {
            ApplyCrystallineMediumCombatShipDetail(w, hullKey, len, wid, hgt);
            return;
        }

        bool isScout = hullKey is "scout" or "scout_light";
        bool isFighter = hullKey is "fighter" or "fighter_basic";
        bool isInterceptor = hullKey is "interceptor" or "interceptor_mk2";
        bool isDrone = hullKey is "drone" or "drone_swarm";
        bool isHero = hullKey is "hero" or "hero_default";
        bool isCompactCraft = isScout || isFighter || isInterceptor || isDrone;
        bool isReferenceCraft = isFighter || isHero;
        if (!isCompactCraft && !isHero)
            return;

        float hw = wid * 0.5f;
        var crystal = RaceMeshWriter.HullMaterial.Solar;

        int flankSegs = isScout ? 3 : isDrone ? 3 : isInterceptor ? 3 : 4;
        for (int side = -1; side <= 1; side += 2)
        {
            float xLead = side * hw * (isScout ? 0.40f : isDrone ? 0.22f : isFighter ? 0.56f : 0.38f);
            for (int e = 0; e < flankSegs; e++)
            {
                float z = MathHelper.Lerp(len * (isScout ? 0.10f : 0.08f), -len * 0.02f, e / MathF.Max(1f, flankSegs - 1));
                TriMat(w, crystal,
                    new Vector3(xLead, hgt * (0.20f + e * 0.012f), z),
                    new Vector3(xLead - side * hw * 0.03f, hgt * (0.18f + e * 0.012f), z + len * 0.008f),
                    new Vector3(xLead, hgt * (0.16f + e * 0.012f), z + len * 0.012f));
            }
        }

        if (isHero)
        {
            for (int p = 0; p < 4; p++)
            {
                float t = p / 3f;
                float z = MathHelper.Lerp(-len * 0.06f, len * 0.22f, t);
                float xSpan = hw * MathHelper.Lerp(0.32f, 0.10f, t);
                TriMat(w, crystal,
                    new Vector3(-xSpan, hgt * (0.62f + t * 0.06f), z), new Vector3(xSpan, hgt * (0.62f + t * 0.06f), z),
                    new Vector3(0, hgt * (0.70f + t * 0.06f), z + len * 0.006f));
            }
        }

        if (isReferenceCraft)
            AddCrystallineReferenceCraftSubstrate(w, hw, hgt, len, isHero);
        else
            AddCrystallineCompactCraftSubstrate(w, hullKey, hw, hgt, len);

        AddCrystallineGameplayComponentBands(w, hullKey, hw, hgt, len);
    }

    private static void AddCrystallineCompactCraftSubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var recess = RaceMeshWriter.HullMaterial.Radiator;
        var facet = RaceMeshWriter.HullMaterial.Truss;
        var crystal = RaceMeshWriter.HullMaterial.Solar;
        var engineGlow = RaceMeshWriter.HullMaterial.Engine;
        bool isScout = hullKey is "scout" or "scout_light";
        bool isInterceptor = hullKey is "interceptor" or "interceptor_mk2";
        bool isInterceptorMk2 = hullKey is "interceptor_mk2";
        bool isDrone = hullKey is "drone" or "drone_swarm";

        int bellyPlates = isInterceptorMk2 ? 3 : 2;
        float bellyZStart = len * 0.52f;
        float bellyZEnd = len * (isInterceptorMk2 ? 0.92f : 0.88f);
        float bellyHalfW = isScout ? 0.24f : isDrone ? 0.16f : isInterceptorMk2 ? 0.22f : 0.26f;
        for (int i = 0; i < bellyPlates; i++)
        {
            float t = i / MathF.Max(1f, bellyPlates - 1);
            float z = MathHelper.Lerp(bellyZStart, bellyZEnd, t);
            float halfW = hw * MathHelper.Lerp(bellyHalfW, bellyHalfW * 0.68f, t);
            TriMat(w, recess,
                new Vector3(-halfW, hgt * 0.03f, z), new Vector3(halfW, hgt * 0.03f, z),
                new Vector3(0, hgt * 0.16f, z + len * 0.007f));
            if (i == bellyPlates - 1)
                TriMat(w, crystal,
                    new Vector3(-halfW * 0.42f, hgt * 0.08f, z + len * 0.002f),
                    new Vector3(halfW * 0.42f, hgt * 0.08f, z + len * 0.002f),
                    new Vector3(0, hgt * 0.14f, z + len * 0.006f));
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float xFlank = side * hw * (isDrone ? 0.20f : isScout ? 0.34f : isInterceptorMk2 ? 0.26f : isInterceptor ? 0.30f : 0.28f);
            int flankFacets = isInterceptorMk2 ? 3 : 2;
            for (int f = 0; f < flankFacets; f++)
            {
                float z = len * (0.56f + f * 0.08f);
                TriMat(w, facet,
                    new Vector3(xFlank, hgt * (0.22f + f * 0.03f), z),
                    new Vector3(xFlank - side * hw * 0.028f, hgt * (0.19f + f * 0.03f), z + len * 0.006f),
                    new Vector3(xFlank, hgt * (0.16f + f * 0.03f), z + len * 0.009f));
            }
        }

        int keelSegs = isInterceptorMk2 ? 3 : 2;
        for (int k = 0; k < keelSegs; k++)
        {
            float t = k / MathF.Max(1f, keelSegs - 1);
            float z = MathHelper.Lerp(len * 0.56f, len * 0.90f, t);
            float yBase = hgt * (0.44f + t * (isDrone ? 0.22f : 0.18f));
            float halfKeel = hw * (isDrone ? 0.032f : 0.048f);
            var keelMat = (isScout || isInterceptor) && k == keelSegs - 1 ? crystal : k % 2 == 0 ? crystal : facet;
            TriMat(w, keelMat,
                new Vector3(-halfKeel, yBase, z), new Vector3(halfKeel, yBase, z),
                new Vector3(0, yBase + hgt * 0.07f, z + len * 0.004f));
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float xEng = side * hw * (isDrone ? 0.22f : isScout ? 0.10f : 0.12f);
            float zEng = -len * 0.05f;
            TriMat(w, engineGlow,
                new Vector3(xEng, hgt * 0.08f, zEng),
                new Vector3(xEng - side * hw * 0.02f, hgt * 0.05f, zEng - len * 0.008f),
                new Vector3(xEng, hgt * 0.03f, zEng + len * 0.004f));
        }
    }

    private static void AddCrystallineReferenceCraftSubstrate(RaceMeshWriter w, float hw, float hgt, float len, bool isHero)
    {
        var recess = RaceMeshWriter.HullMaterial.Radiator;
        var facet = RaceMeshWriter.HullMaterial.Truss;
        var crystal = RaceMeshWriter.HullMaterial.Solar;
        var engineGlow = RaceMeshWriter.HullMaterial.Engine;

        int bellyBands = isHero ? 2 : 2;
        for (int i = 0; i < bellyBands; i++)
        {
            float t = i / MathF.Max(1f, bellyBands - 1);
            float z = MathHelper.Lerp(len * 0.54f, len * (isHero ? 0.90f : 0.86f), t);
            float halfW = hw * MathHelper.Lerp(0.30f, 0.18f, t);
            TriMat(w, recess,
                new Vector3(-halfW, hgt * 0.04f, z), new Vector3(halfW, hgt * 0.04f, z),
                new Vector3(0, hgt * (0.13f + t * 0.02f), z + len * 0.007f));
            TriMat(w, facet,
                new Vector3(-halfW * 0.76f, hgt * 0.06f, z - len * 0.003f),
                new Vector3(halfW * 0.76f, hgt * 0.06f, z - len * 0.003f),
                new Vector3(0, hgt * (0.10f + t * 0.02f), z));
        }

        int spineSegs = 2;
        for (int s = 0; s < spineSegs; s++)
        {
            float t = s / MathF.Max(1f, spineSegs - 1);
            float z = MathHelper.Lerp(len * 0.58f, len * (isHero ? 0.90f : 0.86f), t);
            float halfSpine = hw * 0.042f;
            TriMat(w, crystal,
                new Vector3(-halfSpine, hgt * (0.48f + t * 0.06f), z),
                new Vector3(halfSpine, hgt * (0.48f + t * 0.06f), z),
                new Vector3(0, hgt * (0.56f + t * 0.06f), z + len * 0.004f));
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float xLead = side * hw * (isHero ? 0.44f : 0.50f);
            for (int e = 0; e < 3; e++)
            {
                float z = MathHelper.Lerp(len * 0.72f, len * 0.58f, e / 2f);
                TriMat(w, crystal,
                    new Vector3(xLead, hgt * (0.18f + e * 0.02f), z),
                    new Vector3(xLead - side * hw * 0.026f, hgt * (0.16f + e * 0.02f), z + len * 0.006f),
                    new Vector3(xLead, hgt * (0.14f + e * 0.02f), z + len * 0.009f));
            }
        }

        for (int e = 0; e < 2; e++)
        {
            float x = (e - 0.5f) * hw * 0.16f;
            TriMat(w, engineGlow,
                new Vector3(x - hw * 0.04f, hgt * 0.06f, -len * 0.05f),
                new Vector3(x + hw * 0.04f, hgt * 0.06f, -len * 0.05f),
                new Vector3(x, hgt * 0.12f, -len * 0.03f));
        }
    }

    private static void ApplyCrystallineUtilityShipDetail(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        var vein = RaceMeshWriter.HullMaterial.Solar;
        var shadow = RaceMeshWriter.HullMaterial.Radiator;
        var facet = RaceMeshWriter.HullMaterial.Truss;
        var engineMat = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float hw = wid * 0.5f;
        float widthScale = hullKey is "freighter_bulk" ? 0.90f : hullKey is "miner_eva" ? 0.92f : 0.96f;
        hw *= widthScale;
        bool triBudgetTight = hullKey is "freighter_bulk" or "transport_cargo";
        bool isMiner = hullKey is "miner_basic" or "miner_eva" or "miner_tractor";
        bool isSupport = hullKey is "support_repair";
        int veinSegs = hullKey is "freighter_bulk" ? 3 : triBudgetTight ? 4 : isMiner ? 5 : 4;

        for (int side = -1; side <= 1; side += 2)
        {
            float xOut = side * hw * (hullKey is "freighter_bulk" ? 0.48f : hullKey is "transport_cargo" ? 0.46f : 0.56f);
            for (int i = 0; i < veinSegs; i++)
            {
                float t = i / MathF.Max(1f, veinSegs - 1);
                float z = MathHelper.Lerp(-len * 0.04f, len * (hullKey is "freighter_bulk" ? 0.38f : triBudgetTight ? 0.36f : isMiner ? 0.24f : isSupport ? 0.20f : 0.16f), t);
                TriMat(w, vein,
                    new Vector3(xOut, hgt * (0.16f + t * 0.05f), z),
                    new Vector3(xOut - side * hw * 0.035f, hgt * (0.13f + t * 0.05f), z + len * 0.010f),
                    new Vector3(xOut, hgt * (0.11f + t * 0.04f), z + len * 0.014f));
            }
        }

        if (hullKey is "miner_basic")
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xTip = side * hw * 0.72f;
                TriMat(w, weapon,
                    new Vector3(xTip, hgt * 0.20f, len * 0.46f),
                    new Vector3(xTip + side * hw * 0.04f, hgt * 0.26f, len * 0.52f),
                    new Vector3(xTip, hgt * 0.14f, len * 0.54f));
                TriMat(w, engineMat,
                    new Vector3(xTip - side * hw * 0.03f, hgt * 0.06f, len * 0.10f),
                    new Vector3(xTip + side * hw * 0.03f, hgt * 0.06f, len * 0.10f),
                    new Vector3(xTip, hgt * 0.12f, len * 0.14f));
            }
            TriMat(w, engineMat,
                new Vector3(-hw * 0.20f, hgt * 0.04f, -len * 0.08f),
                new Vector3(hw * 0.20f, hgt * 0.04f, -len * 0.08f),
                new Vector3(0, hgt * 0.10f, -len * 0.04f));
        }

        if (hullKey is "support_repair")
        {
            for (int a = 0; a < 2; a++)
            {
                float z = len * (0.06f + a * 0.060f);
                TriMat(w, vein,
                    new Vector3(-hw * 0.04f, hgt * (0.76f + a * 0.04f), z),
                    new Vector3(hw * 0.04f, hgt * (0.76f + a * 0.04f), z),
                    new Vector3(0, hgt * (0.84f + a * 0.04f), z + len * 0.008f));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float xBoom = side * hw * 0.66f;
                TriMat(w, weapon,
                    new Vector3(xBoom, hgt * 0.56f, len * 0.12f),
                    new Vector3(xBoom + side * hw * 0.05f, hgt * 0.62f, len * 0.14f),
                    new Vector3(xBoom, hgt * 0.50f, len * 0.16f));
            }
        }

        if (hullKey is "transport_cargo")
        {
            for (int s = 0; s < 3; s++)
            {
                float z = len * (0.10f + s * 0.10f);
                TriMat(w, facet,
                    new Vector3(-hw * 0.06f, hgt * 0.42f, z),
                    new Vector3(hw * 0.06f, hgt * 0.42f, z),
                    new Vector3(0, hgt * 0.48f, z + len * 0.010f));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float cx = side * hw * 0.44f;
                TriMat(w, weapon,
                    new Vector3(cx - hw * 0.06f, hgt * 0.24f, len * 0.28f),
                    new Vector3(cx + hw * 0.06f, hgt * 0.24f, len * 0.28f),
                    new Vector3(cx, hgt * 0.30f, len * 0.32f));
            }
        }

        if (hullKey is "freighter_bulk")
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float cx = side * hw * 0.40f;
                TriMat(w, shadow,
                    new Vector3(cx - hw * 0.06f, hgt * 0.18f, len * 0.30f),
                    new Vector3(cx + hw * 0.06f, hgt * 0.18f, len * 0.30f),
                    new Vector3(cx, hgt * 0.24f, len * 0.36f));
            }
            TriMat(w, vein,
                new Vector3(-hw * 0.08f, hgt * 0.36f, len * 0.38f),
                new Vector3(hw * 0.08f, hgt * 0.36f, len * 0.38f),
                new Vector3(0, hgt * 0.42f, len * 0.44f));
        }

        AddCrystallineUtilitySubstrate(w, hullKey, hw, hgt, len);

        if (!triBudgetTight)
        {
            TriMat(w, engineMat,
                new Vector3(-hw * 0.12f, 0, -len * 0.08f), new Vector3(hw * 0.12f, 0, -len * 0.08f),
                new Vector3(0, hgt * 0.06f, -len * 0.12f));
        }
    }

    /// <summary>Belly/flank ice facet substrate bands � cyan crystal vein accents under team tint.</summary>
    private static void AddCrystallineUtilitySubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var membrane = RaceMeshWriter.HullMaterial.Hull;
        var facet = RaceMeshWriter.HullMaterial.Truss;
        var vein = RaceMeshWriter.HullMaterial.Solar;
        var shadow = RaceMeshWriter.HullMaterial.Radiator;
        bool isCargo = hullKey is "freighter_bulk" or "transport_cargo";
        bool isMiner = hullKey is "miner_basic" or "miner_eva" or "miner_tractor";
        bool isSupport = hullKey is "support_repair";
        int bellyPlates = isCargo ? 4 : isSupport ? 4 : isMiner ? 5 : 3;

        for (int i = 0; i < bellyPlates; i++)
        {
            float t = i / MathF.Max(1f, bellyPlates - 1);
            float z = MathHelper.Lerp(-len * 0.06f, len * (isCargo ? 0.30f : isSupport ? 0.22f : isMiner ? 0.24f : 0.16f), t);
            float halfW = hw * MathHelper.Lerp(
                hullKey is "freighter_bulk" ? 0.30f : isCargo ? 0.24f : 0.26f,
                hullKey is "freighter_bulk" ? 0.14f : 0.10f, t);
            TriMat(w, shadow,
                new Vector3(-halfW, hgt * 0.02f, z), new Vector3(halfW, hgt * 0.02f, z),
                new Vector3(0, hgt * (0.16f + t * 0.04f), z + len * 0.012f));
            if (i % 2 == 0)
            {
                TriMat(w, facet,
                    new Vector3(-halfW * 0.82f, hgt * 0.04f, z - len * 0.004f),
                    new Vector3(halfW * 0.82f, hgt * 0.04f, z - len * 0.004f),
                    new Vector3(0, hgt * (0.10f + t * 0.02f), z));
            }
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * hw * (hullKey is "freighter_bulk" ? 0.26f : isCargo ? 0.20f : 0.24f);
            int flankSegs = isCargo ? 4 : isSupport ? 4 : isMiner ? 4 : 3;
            for (int s = 0; s < flankSegs; s++)
            {
                float z = MathHelper.Lerp(-len * 0.02f, len * (isCargo ? 0.22f : isSupport ? 0.18f : isMiner ? 0.20f : 0.14f), s / MathF.Max(1f, flankSegs - 1));
                TriMat(w, membrane,
                    new Vector3(x, hgt * 0.14f, z), new Vector3(x - side * hw * 0.03f, hgt * 0.26f, z + len * 0.012f),
                    new Vector3(x, hgt * 0.10f, z + len * 0.014f));
                if (s % 2 == 0)
                {
                    TriMat(w, facet,
                        new Vector3(x - side * hw * 0.02f, hgt * 0.18f, z + len * 0.004f),
                        new Vector3(x, hgt * 0.22f, z + len * 0.008f),
                        new Vector3(x - side * hw * 0.01f, hgt * 0.14f, z + len * 0.010f));
                }
            }
        }

        int spineSegs = isCargo ? 5 : isSupport ? 5 : isMiner ? 5 : 4;
        for (int i = 0; i < spineSegs; i++)
        {
            float t = i / MathF.Max(1f, spineSegs - 1);
            float z = MathHelper.Lerp(-len * 0.04f, len * (hullKey switch
            {
                "freighter_bulk" => 0.48f,
                "transport_cargo" => 0.46f,
                "miner_basic" => 0.34f,
                "miner_eva" => 0.28f,
                "miner_tractor" => 0.32f,
                "support_repair" => 0.26f,
                _ => 0.18f
            }), t);
            float xSpan = hw * MathHelper.Lerp(0.14f, 0.03f, t);
            TriMat(w, shadow,
                new Vector3(-xSpan, hgt * (0.44f + t * 0.06f), z), new Vector3(xSpan, hgt * (0.44f + t * 0.06f), z),
                new Vector3(0, hgt * (0.56f + t * 0.04f), z + len * 0.010f));
            TriMat(w, membrane,
                new Vector3(-xSpan * 0.86f, hgt * (0.48f + t * 0.04f), z + len * 0.003f),
                new Vector3(xSpan * 0.86f, hgt * (0.48f + t * 0.04f), z + len * 0.003f),
                new Vector3(0, hgt * (0.52f + t * 0.04f), z + len * 0.007f));
            if (i % 2 == 0 || isMiner || isSupport || isCargo)
            {
                TriMat(w, vein,
                    new Vector3(-xSpan * 0.7f, hgt * (0.54f + t * 0.04f), z + len * 0.006f),
                    new Vector3(xSpan * 0.7f, hgt * (0.54f + t * 0.04f), z + len * 0.006f),
                    new Vector3(0, hgt * (0.60f + t * 0.03f), z + len * 0.012f));
            }
        }
    }

    private static void ApplyCrystallineMediumCombatShipDetail(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        var crystal = RaceMeshWriter.HullMaterial.Solar;
        var facet = RaceMeshWriter.HullMaterial.Truss;
        var engineGlow = RaceMeshWriter.HullMaterial.Engine;
        float hw = wid * 0.5f;
        bool isCorvette = hullKey is "corvette_fast";
        bool isFrigate = hullKey is "frigate_strike";
        bool isGunship = hullKey is "gunship_heavy";
        bool isBomber = hullKey is "bomber_heavy";
        bool isDestroyer = hullKey is "destroyer_assault";

        int crestBands = isDestroyer ? 3 : 2;
        float crestReach = isFrigate ? 0.28f : isBomber ? 0.28f : isDestroyer ? 0.28f : isCorvette ? 0.22f : 0.20f;
        for (int p = 0; p < crestBands; p++)
        {
            float t = p / MathF.Max(1f, crestBands - 1);
            float z = MathHelper.Lerp(-len * 0.04f, len * crestReach, t);
            float xSpan = hw * MathHelper.Lerp(0.34f, 0.10f, t);
            TriMat(w, crystal,
                new Vector3(-xSpan, hgt * (0.48f + t * 0.04f), z), new Vector3(xSpan, hgt * (0.48f + t * 0.04f), z),
                new Vector3(0, hgt * (0.56f + t * 0.04f), z + len * 0.006f));
        }

        if (isGunship)
        {
            float zChin = len * 0.12f;
            TriMat(w, facet,
                new Vector3(-hw * 0.20f, hgt * 0.02f, zChin), new Vector3(hw * 0.20f, hgt * 0.02f, zChin),
                new Vector3(0, hgt * 0.10f, zChin + len * 0.010f));
            for (int s = 0; s < 4; s++)
            {
                float z = len * (0.06f + s * 0.05f);
                TriMat(w, facet,
                    new Vector3(-hw * 0.14f, hgt * 0.03f, z), new Vector3(hw * 0.14f, hgt * 0.03f, z),
                    new Vector3(0, hgt * 0.09f, z + len * 0.008f));
            }
            TriMat(w, engineGlow,
                new Vector3(-hw * 0.10f, hgt * 0.02f, -len * 0.10f), new Vector3(hw * 0.10f, hgt * 0.02f, -len * 0.10f),
                new Vector3(0, hgt * 0.08f, -len * 0.08f));
        }

        if (isBomber)
        {
            for (int b = 0; b < 5; b++)
            {
                float z = len * (0.04f + b * 0.06f);
                float halfW = hw * MathHelper.Lerp(0.30f, 0.12f, b / 4f);
                TriMat(w, crystal,
                    new Vector3(-halfW, hgt * 0.05f, z), new Vector3(halfW, hgt * 0.05f, z),
                    new Vector3(0, hgt * 0.11f, z + len * 0.008f));
            }
            for (int e = 0; e < 3; e++)
            {
                float z = -len * (0.08f + e * 0.04f);
                TriMat(w, engineGlow,
                    new Vector3(-hw * 0.12f, hgt * 0.04f, z), new Vector3(hw * 0.12f, hgt * 0.04f, z),
                    new Vector3(0, hgt * 0.10f, z + len * 0.006f));
            }
        }

        if (isFrigate)
        {
            for (int d = 0; d < 4; d++)
            {
                float z = len * (0.04f + d * 0.06f);
                TriMat(w, crystal,
                    new Vector3(-hw * 0.08f, hgt * 0.50f, z), new Vector3(hw * 0.08f, hgt * 0.50f, z),
                    new Vector3(0, hgt * 0.56f, z + len * 0.005f));
            }
        }

        AddCrystallineMediumCombatSubstrate(w, hullKey, hw, hgt, len);
        AddCrystallineGameplayComponentBands(w, hullKey, hw, hgt, len);
    }

    /// <summary>Ice facet substrate bands for Cryo medium combat � vertex luminance under team tint.</summary>
    private static void AddCrystallineMediumCombatSubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var recess = RaceMeshWriter.HullMaterial.Radiator;
        var facet = RaceMeshWriter.HullMaterial.Truss;
        var crystal = RaceMeshWriter.HullMaterial.Solar;
        var engineGlow = RaceMeshWriter.HullMaterial.Engine;

        bool isCorvette = hullKey is "corvette_fast";
        bool isFrigate = hullKey is "frigate_strike";
        bool isGunship = hullKey is "gunship_heavy";
        bool isBomber = hullKey is "bomber_heavy";
        bool isDestroyer = hullKey is "destroyer_assault";

        int bellyBands = isCorvette ? 5 : isBomber ? 5 : isFrigate ? 5 : isDestroyer ? 4 : 4;
        float bellyWidth = isBomber ? 0.48f : isDestroyer ? 0.40f : isFrigate ? 0.38f : isGunship ? 0.36f : 0.34f;
        float bellyReach = isCorvette ? 0.22f : isBomber ? 0.26f : isFrigate ? 0.28f : isDestroyer ? 0.34f : 0.20f;
        for (int i = 0; i < bellyBands; i++)
        {
            float t = bellyBands > 1 ? i / (bellyBands - 1f) : 0f;
            float z = MathHelper.Lerp(-len * 0.06f, len * bellyReach, t);
            float halfW = hw * MathHelper.Lerp(bellyWidth, bellyWidth * 0.66f, t);
            TriMat(w, recess,
                new Vector3(-halfW, hgt * 0.04f, z), new Vector3(halfW, hgt * 0.04f, z),
                new Vector3(0, hgt * (0.12f + t * 0.02f), z + len * 0.008f));
            if (i % 2 == 1 && (isBomber || isGunship))
            {
                TriMat(w, facet,
                    new Vector3(-halfW * 0.82f, hgt * 0.06f, z + len * 0.003f),
                    new Vector3(halfW * 0.82f, hgt * 0.06f, z + len * 0.003f),
                    new Vector3(0, hgt * (0.10f + t * 0.02f), z + len * 0.010f));
            }
        }

        int flankBands = isDestroyer ? 5 : isFrigate ? 5 : isBomber ? 4 : isGunship ? 4 : 4;
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * hw * (isGunship ? 0.38f : isFrigate ? 0.36f : isBomber ? 0.36f : isDestroyer ? 0.34f : 0.32f);
            for (int s = 0; s < flankBands; s++)
            {
                float z = len * (0.02f + s * (isDestroyer ? 0.07f : 0.06f));
                TriMat(w, facet,
                    new Vector3(x, hgt * 0.20f, z), new Vector3(x - side * hw * 0.03f, hgt * 0.24f, z + len * 0.01f),
                    new Vector3(x, hgt * 0.16f, z + len * 0.012f));
                if (s % 2 == 0)
                {
                    TriMat(w, crystal,
                        new Vector3(x - side * hw * 0.02f, hgt * 0.18f, z + len * 0.004f),
                        new Vector3(x, hgt * 0.22f, z + len * 0.008f),
                        new Vector3(x - side * hw * 0.01f, hgt * 0.14f, z + len * 0.010f));
                }
            }
        }

        int dorsalStripes = isCorvette ? 5 : isFrigate ? 5 : isBomber ? 5 : isGunship ? 5 : isDestroyer ? 5 : 4;
        float dorsalReach = isFrigate ? 0.30f : isBomber ? 0.26f : isDestroyer ? 0.32f : isCorvette ? 0.24f : 0.22f;
        for (int d = 0; d < dorsalStripes; d++)
        {
            float t = d / MathF.Max(1f, dorsalStripes - 1);
            float z = MathHelper.Lerp(-len * 0.02f, len * dorsalReach, t);
            float xSpan = hw * MathHelper.Lerp(0.26f, 0.08f, t);
            TriMat(w, recess,
                new Vector3(-xSpan, hgt * (0.46f + t * 0.04f), z), new Vector3(xSpan, hgt * (0.46f + t * 0.04f), z),
                new Vector3(0, hgt * (0.50f + t * 0.04f), z + len * 0.006f));
            if (d % 2 == 0)
            {
                TriMat(w, crystal,
                    new Vector3(-xSpan * 0.72f, hgt * (0.48f + t * 0.04f), z + len * 0.003f),
                    new Vector3(xSpan * 0.72f, hgt * (0.48f + t * 0.04f), z + len * 0.003f),
                    new Vector3(0, hgt * (0.52f + t * 0.04f), z + len * 0.007f));
            }
        }

        if (isGunship)
        {
            for (int c = 0; c < 3; c++)
            {
                float z = len * (0.08f + c * 0.05f);
                TriMat(w, recess,
                    new Vector3(-hw * 0.12f, hgt * 0.02f, z), new Vector3(hw * 0.12f, hgt * 0.02f, z),
                    new Vector3(0, hgt * 0.08f, z + len * 0.006f));
            }
        }

        if (isBomber)
        {
            for (int p = 0; p < 4; p++)
            {
                float z = len * (0.02f + p * 0.06f);
                float halfW = hw * MathHelper.Lerp(0.22f, 0.08f, p / 3f);
                TriMat(w, recess,
                    new Vector3(-halfW, hgt * 0.08f, z), new Vector3(halfW, hgt * 0.08f, z),
                    new Vector3(0, hgt * 0.14f, z + len * 0.006f));
            }
        }

        if (isDestroyer)
        {
            for (int e = 0; e < 3; e++)
            {
                float z = -len * (0.08f + e * 0.04f);
                float halfW = hw * MathHelper.Lerp(0.14f, 0.08f, e / 2f);
                TriMat(w, engineGlow,
                    new Vector3(-halfW, hgt * 0.05f, z), new Vector3(halfW, hgt * 0.05f, z),
                    new Vector3(0, hgt * 0.12f, z + len * 0.006f));
            }
        }
        else
        {
            TriMat(w, engineGlow,
                new Vector3(-hw * 0.10f, hgt * 0.04f, -len * 0.10f), new Vector3(hw * 0.10f, hgt * 0.04f, -len * 0.10f),
                new Vector3(0, hgt * 0.10f, -len * 0.08f));
        }
    }

    private static void AddCrystallineGameplayComponentBands(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        bool isGunship = hullKey is "gunship_heavy";
        bool isDestroyer = hullKey is "destroyer_assault";
        bool isBomber = hullKey is "bomber_heavy";

        if (isGunship)
        {
            TriMat(w, weapon,
                new Vector3(-hw * 0.18f, hgt * 0.04f, len * 0.14f), new Vector3(hw * 0.18f, hgt * 0.04f, len * 0.14f),
                new Vector3(0, hgt * 0.10f, len * 0.20f));
        }

        if (isBomber)
        {
            TriMat(w, weapon,
                new Vector3(-hw * 0.22f, hgt * 0.04f, len * 0.06f), new Vector3(hw * 0.22f, hgt * 0.04f, len * 0.06f),
                new Vector3(0, hgt * 0.10f, len * 0.12f));
        }

        if (isDestroyer)
        {
            TriMat(w, weapon,
                new Vector3(-hw * 0.08f, hgt * 0.10f, len * 0.54f), new Vector3(hw * 0.08f, hgt * 0.10f, len * 0.54f),
                new Vector3(0, hgt * 0.18f, len * 0.60f));
        }

        TriMat(w, engine,
            new Vector3(-hw * 0.12f, hgt * 0.04f, -len * 0.12f), new Vector3(hw * 0.12f, hgt * 0.04f, -len * 0.12f),
            new Vector3(0, hgt * 0.10f, -len * 0.08f));
    }

    private static void ApplyCrystallineCapitalShipDetail(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        var crystal = RaceMeshWriter.HullMaterial.Solar;
        float hw = wid * 0.5f;
        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDreadnought = hullKey is "dreadnought";

        int dorsalSegs = isDreadnought ? 6 : isCruiser ? 7 : 6;
        float dorsalBase = isCarrier ? 0.42f : 0.52f;
        float dorsalRise = isCarrier ? 0.05f : 0.06f;
        for (int i = 0; i < dorsalSegs; i++)
        {
            float t = i / MathF.Max(1f, dorsalSegs - 1);
            float z = MathHelper.Lerp(len * 0.06f, len * (isCarrier ? 0.68f : isDreadnought ? 0.64f : 0.62f), t);
            TriMat(w, crystal,
                new Vector3(0, hgt * (dorsalBase + t * dorsalRise), z),
                new Vector3(-hw * 0.10f, hgt * (dorsalBase - 0.10f + t * 0.03f), z - len * 0.02f),
                new Vector3(hw * 0.10f, hgt * (dorsalBase - 0.10f + t * 0.03f), z - len * 0.02f));
        }

        AddCrystallineCapitalSubstrate(w, hullKey, hw, hgt, len);
        if (isCarrier)
            AddCrystallineCarrierDeckMembranes(w, hw, hgt, len);
        AddCrystallineAccentRingVeins(w, hullKey, hw, hgt, len);
        AddCrystallineGameplayComponentBands(w, hullKey, hw, hgt, len);
    }

    /// <summary>Ice facet substrate bands for Cryo capital hulls � belly/flank/dorsal luminance under team tint.</summary>
    private static void AddCrystallineCapitalSubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var recess = RaceMeshWriter.HullMaterial.Radiator;
        var facet = RaceMeshWriter.HullMaterial.Truss;
        var crystal = RaceMeshWriter.HullMaterial.Solar;
        var hull = RaceMeshWriter.HullMaterial.Hull;
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        bool isDreadnought = hullKey is "dreadnought";
        int bellyPlates = isCruiser ? 3 : 2;

        for (int i = 0; i < bellyPlates; i++)
        {
            float t = bellyPlates == 1 ? 0.5f : i / (float)(bellyPlates - 1);
            float z = MathHelper.Lerp(-len * 0.12f, len * (isCarrier ? 0.30f : isDreadnought ? 0.26f : 0.24f), t);
            float halfW = hw * MathHelper.Lerp(
                isCarrier ? 0.38f : isDreadnought ? 0.34f : 0.32f, 0.18f, t);
            TriMat(w, recess,
                new Vector3(-halfW, hgt * 0.05f, z), new Vector3(halfW, hgt * 0.05f, z),
                new Vector3(0, hgt * 0.14f, z + len * 0.010f));
            if (i == bellyPlates - 1)
                TriMat(w, crystal,
                    new Vector3(-halfW * 0.5f, hgt * 0.08f, z + len * 0.004f),
                    new Vector3(halfW * 0.5f, hgt * 0.08f, z + len * 0.004f),
                    new Vector3(0, hgt * 0.12f, z + len * 0.008f));
            if (isCarrier && i == bellyPlates - 1)
            {
                TriMat(w, hull,
                    new Vector3(-halfW * 0.72f, hgt * 0.08f, z + len * 0.004f),
                    new Vector3(halfW * 0.72f, hgt * 0.08f, z + len * 0.004f),
                    new Vector3(0, hgt * 0.11f, z + len * 0.008f));
            }
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float xOut = side * hw * (isCarrier ? 0.40f : isDreadnought ? 0.48f : 0.42f);
            int flankBands = isDreadnought ? 5 : isCarrier ? 4 : 4;
            for (int f = 0; f < flankBands; f++)
            {
                float t = f / MathF.Max(1f, flankBands - 1);
                float z = MathHelper.Lerp(-len * 0.08f, len * (isCarrier ? 0.20f : 0.18f), t);
                TriMat(w, recess,
                    new Vector3(xOut, hgt * 0.12f, z),
                    new Vector3(xOut - side * hw * 0.04f, hgt * 0.18f, z + len * 0.008f),
                    new Vector3(xOut, hgt * 0.08f, z + len * 0.012f));
                if (f % 2 == 0)
                {
                    TriMat(w, crystal,
                        new Vector3(xOut - side * hw * 0.02f, hgt * 0.14f, z + len * 0.004f),
                        new Vector3(xOut, hgt * 0.18f, z + len * 0.008f),
                        new Vector3(xOut - side * hw * 0.01f, hgt * 0.10f, z + len * 0.010f));
                }
            }
        }

        if (isCruiser)
        {
            for (int k = 0; k < 5; k++)
            {
                float z = MathHelper.Lerp(len * 0.02f, len * 0.26f, k / 4f);
                TriMat(w, facet,
                    new Vector3(-hw * 0.14f, hgt * 0.32f, z), new Vector3(hw * 0.14f, hgt * 0.32f, z),
                    new Vector3(0, hgt * 0.36f, z + len * 0.006f));
            }
            for (int d = 0; d < 4; d++)
            {
                float t = d / 3f;
                float z = MathHelper.Lerp(len * 0.04f, len * 0.22f, t);
                float xSpan = hw * MathHelper.Lerp(0.12f, 0.06f, t);
                TriMat(w, crystal,
                    new Vector3(-xSpan, hgt * (0.48f + t * 0.06f), z), new Vector3(xSpan, hgt * (0.48f + t * 0.06f), z),
                    new Vector3(0, hgt * (0.54f + t * 0.06f), z + len * 0.005f));
            }
        }

        if (isDreadnought)
        {
            for (int k = 0; k < 4; k++)
            {
                float z = MathHelper.Lerp(len * 0.44f, len * 0.66f, k / 3f);
                TriMat(w, facet,
                    new Vector3(-hw * 0.12f, hgt * 0.28f, z), new Vector3(hw * 0.12f, hgt * 0.28f, z),
                    new Vector3(0, hgt * 0.34f, z + len * 0.008f));
            }
            for (int p = 0; p < 3; p++)
            {
                float z = len * (0.50f + p * 0.06f);
                TriMat(w, crystal,
                    new Vector3(-hw * 0.08f, hgt * 0.30f, z), new Vector3(hw * 0.08f, hgt * 0.30f, z),
                    new Vector3(0, hgt * 0.36f, z + len * 0.006f));
            }
        }
    }

    private static void AddCrystallineCarrierDeckMembranes(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var recess = RaceMeshWriter.HullMaterial.Radiator;
        var facet = RaceMeshWriter.HullMaterial.Truss;
        var crystal = RaceMeshWriter.HullMaterial.Solar;
        float deckZ = MathHelper.Lerp(-len * 0.16f, len * 0.08f, 0.5f);
        TriMat(w, recess,
            new Vector3(-hw * 0.46f, hgt * 0.20f, deckZ), new Vector3(hw * 0.46f, hgt * 0.20f, deckZ),
            new Vector3(0, hgt * 0.24f, deckZ + len * 0.006f));
        TriMat(w, recess,
            new Vector3(-hw * 0.24f, hgt * 0.10f, -len * 0.06f), new Vector3(hw * 0.24f, hgt * 0.10f, -len * 0.06f),
            new Vector3(0, hgt * 0.14f, -len * 0.02f));
        for (int h = 0; h < 2; h++)
        {
            float z = len * (0.04f - h * 0.08f);
            TriMat(w, facet,
                new Vector3(-hw * 0.30f, hgt * 0.22f, z), new Vector3(hw * 0.30f, hgt * 0.22f, z),
                new Vector3(0, hgt * 0.26f, z + len * 0.008f));
        }
        for (int r = 0; r < 2; r++)
        {
            float z = len * (-0.02f + r * 0.08f);
            TriMat(w, crystal,
                new Vector3(-hw * 0.18f, hgt * 0.08f, z), new Vector3(hw * 0.18f, hgt * 0.08f, z),
                new Vector3(0, hgt * 0.12f, z + len * 0.006f));
        }
    }

    private static void AddCrystallineAccentRingVeins(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var crystal = RaceMeshWriter.HullMaterial.Solar;
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDreadnought = hullKey is "dreadnought";
        int veins = isDreadnought ? 6 : isCarrier ? 6 : 5;
        for (int v = 0; v < veins; v++)
        {
            float t = v / MathF.Max(1f, veins - 1);
            float z = MathHelper.Lerp(-len * 0.06f, len * (isCarrier ? 0.24f : 0.22f), t);
            for (int side = -1; side <= 1; side += 2)
            {
                float xOut = side * hw * (isCarrier ? 0.52f : isDreadnought ? 0.58f : 0.48f);
                TriMat(w, crystal,
                    new Vector3(xOut, hgt * (0.20f + t * 0.04f), z),
                    new Vector3(xOut - side * hw * 0.03f, hgt * (0.24f + t * 0.04f), z + len * 0.008f),
                    new Vector3(xOut, hgt * (0.16f + t * 0.04f), z + len * 0.012f));
            }
        }
    }

    public static void AppendCrystallineScorerAccentBands(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        if (hullKey is "carrier" or "carrier_command")
        {
            AppendCrystallineCapitalCarrierScorerAccentBands(w, len, wid, hgt);
            return;
        }

        if (hullKey is "cruiser" or "cruiser_heavy")
        {
            AppendCrystallineCapitalCruiserScorerAccentBands(w, len, wid, hgt);
            return;
        }

        if (hullKey is "dreadnought")
        {
            AppendCrystallineCapitalDreadnoughtScorerAccentBands(w, len, wid, hgt);
            return;
        }

        if (hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk" or "support_repair")
        {
            AppendCrystallineUtilityScorerAccentBands(w, hullKey, len, wid, hgt);
            return;
        }

        if (hullKey is "scout" or "scout_light" or "fighter" or "fighter_basic"
            or "interceptor" or "interceptor_mk2" or "drone" or "drone_swarm"
            or "hero" or "hero_default")
        {
            AppendCrystallineCompactScorerAccentBands(w, hullKey, len, wid, hgt);
            return;
        }

        if (hullKey is "corvette_fast" or "frigate_strike" or "gunship_heavy"
            or "bomber_heavy" or "destroyer_assault" or "corvette" or "frigate" or "gunship"
            or "bomber" or "destroyer")
            AppendCrystallineMediumScorerAccentBands(w, hullKey, len, wid, hgt);
    }

    private static void AppendCrystallineMediumScorerAccentBands(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        switch (hullKey)
        {
            case "gunship":
            case "gunship_heavy":
                for (int c = 0; c < 4; c++)
                {
                    float z = len * (0.06f + c * 0.05f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.10f, hgt * 0.04f, z), new Vector3(hw * 0.10f, hgt * 0.04f, z),
                        new Vector3(0, hgt * 0.10f, z + len * 0.006f));
                }
                for (int d = 0; d < 5; d++)
                {
                    float t = d / 4f;
                    float z = MathHelper.Lerp(len * 0.02f, len * 0.22f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.06f, hgt * (0.46f + t * 0.04f), z),
                        new Vector3(hw * 0.06f, hgt * (0.46f + t * 0.04f), z),
                        new Vector3(0, hgt * (0.54f + t * 0.04f), z + len * 0.005f));
                }
                break;

            case "destroyer":
            case "destroyer_assault":
                for (int s = 0; s < 4; s++)
                {
                    float t = s / 3f;
                    float z = MathHelper.Lerp(len * 0.12f, len * 0.32f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.05f, hgt * (0.50f + t * 0.04f), z),
                        new Vector3(hw * 0.05f, hgt * (0.50f + t * 0.04f), z),
                        new Vector3(0, hgt * (0.58f + t * 0.04f), z + len * 0.005f));
                }
                break;

            case "bomber":
            case "bomber_heavy":
                for (int p = 0; p < 4; p++)
                {
                    float z = len * (0.04f + p * 0.06f);
                    float xSpan = hw * MathHelper.Lerp(0.22f, 0.08f, p / 3f);
                    w.TriScorerAccent(
                        new Vector3(-xSpan, hgt * 0.06f, z), new Vector3(xSpan, hgt * 0.06f, z),
                        new Vector3(0, hgt * 0.12f, z + len * 0.006f));
                }
                break;

            case "frigate":
            case "frigate_strike":
                for (int d = 0; d < 4; d++)
                {
                    float z = len * (0.04f + d * 0.06f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.08f, hgt * 0.50f, z), new Vector3(hw * 0.08f, hgt * 0.50f, z),
                        new Vector3(0, hgt * 0.56f, z + len * 0.005f));
                }
                break;

            case "corvette":
            case "corvette_fast":
                for (int wv = 0; wv < 4; wv++)
                {
                    float z = len * (0.58f + wv * 0.06f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.06f, hgt * 0.34f, z), new Vector3(hw * 0.06f, hgt * 0.34f, z),
                        new Vector3(0, hgt * 0.40f, z + len * 0.005f));
                }
                break;
        }
    }

    private static void AppendCrystallineCompactScorerAccentBands(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        switch (hullKey)
        {
            case "scout":
            case "scout_light":
                for (int s = 0; s < 2; s++)
                {
                    float t = s;
                    float z = MathHelper.Lerp(len * 0.58f, len * 0.92f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.04f, hgt * (0.48f + t * 0.08f), z),
                        new Vector3(hw * 0.04f, hgt * (0.48f + t * 0.08f), z),
                        new Vector3(0, hgt * (0.56f + t * 0.08f), z + len * 0.004f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float xLead = side * hw * 0.42f;
                    w.TriScorerAccent(
                        new Vector3(xLead, hgt * 0.20f, len * 0.72f),
                        new Vector3(xLead - side * hw * 0.024f, hgt * 0.18f, len * 0.74f),
                        new Vector3(xLead, hgt * 0.16f, len * 0.76f));
                }
                break;

            case "fighter":
            case "fighter_basic":
                for (int s = 0; s < 4; s++)
                {
                    float t = s / 3f;
                    float z = MathHelper.Lerp(len * 0.56f, len * 0.88f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.05f, hgt * (0.50f + t * 0.06f), z),
                        new Vector3(hw * 0.05f, hgt * (0.50f + t * 0.06f), z),
                        new Vector3(0, hgt * (0.58f + t * 0.06f), z + len * 0.004f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float xLead = side * hw * 0.44f;
                    for (int e = 0; e < 4; e++)
                    {
                        float z = MathHelper.Lerp(len * 0.76f, len * 0.58f, e / 3f);
                        w.TriScorerAccent(
                            new Vector3(xLead, hgt * (0.18f + e * 0.016f), z),
                            new Vector3(xLead - side * hw * 0.026f, hgt * (0.16f + e * 0.016f), z + len * 0.006f),
                            new Vector3(xLead, hgt * (0.14f + e * 0.016f), z + len * 0.009f));
                    }
                }
                break;

            case "interceptor":
                for (int s = 0; s < 5; s++)
                {
                    float t = s / 4f;
                    float z = MathHelper.Lerp(len * 0.58f, len * 0.92f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.038f, hgt * (0.48f + t * 0.06f), z),
                        new Vector3(hw * 0.038f, hgt * (0.48f + t * 0.06f), z),
                        new Vector3(0, hgt * (0.56f + t * 0.06f), z + len * 0.004f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float xCap = side * hw * 0.36f;
                    for (int e = 0; e < 5; e++)
                    {
                        float z = MathHelper.Lerp(len * 0.82f, len * 0.58f, e / 4f);
                        w.TriScorerAccent(
                            new Vector3(xCap, hgt * (0.18f + e * 0.016f), z),
                            new Vector3(xCap - side * hw * 0.028f, hgt * (0.16f + e * 0.016f), z + len * 0.006f),
                            new Vector3(xCap, hgt * (0.14f + e * 0.016f), z + len * 0.009f));
                    }
                }
                break;

            case "interceptor_mk2":
                for (int s = 0; s < 6; s++)
                {
                    float t = s / 5f;
                    float z = MathHelper.Lerp(len * 0.56f, len * 0.94f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.034f, hgt * (0.46f + t * 0.08f), z),
                        new Vector3(hw * 0.034f, hgt * (0.46f + t * 0.08f), z),
                        new Vector3(0, hgt * (0.54f + t * 0.08f), z + len * 0.004f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float xCap = side * hw * 0.30f;
                    for (int e = 0; e < 6; e++)
                    {
                        float z = MathHelper.Lerp(len * 0.88f, len * 0.56f, e / 5f);
                        w.TriScorerAccent(
                            new Vector3(xCap, hgt * (0.16f + e * 0.014f), z),
                            new Vector3(xCap - side * hw * 0.024f, hgt * (0.14f + e * 0.014f), z + len * 0.006f),
                            new Vector3(xCap, hgt * (0.12f + e * 0.014f), z + len * 0.009f));
                    }
                }
                for (int prow = 0; prow < 4; prow++)
                {
                    float z = len * (0.90f + prow * 0.025f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.07f, hgt * 0.20f, z),
                        new Vector3(hw * 0.07f, hgt * 0.20f, z),
                        new Vector3(0, hgt * 0.30f, z + len * 0.005f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float xOut = side * hw * 0.88f;
                    w.TriScorerAccent(
                        new Vector3(xOut, hgt * 0.22f, len * 0.84f),
                        new Vector3(xOut - side * hw * 0.04f, hgt * 0.18f, len * 0.88f),
                        new Vector3(xOut, hgt * 0.16f, len * 0.92f));
                }
                break;

            case "drone":
            case "drone_swarm":
                for (int s = 0; s < 5; s++)
                {
                    float t = s / 4f;
                    float z = MathHelper.Lerp(len * 0.56f, len * 0.90f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.032f, hgt * (0.40f + t * 0.10f), z),
                        new Vector3(hw * 0.032f, hgt * (0.40f + t * 0.10f), z),
                        new Vector3(0, hgt * (0.48f + t * 0.10f), z + len * 0.004f));
                }
                for (int n = 0; n < 4; n++)
                {
                    float z = MathHelper.Lerp(len * 0.64f, len * 0.86f, n / 3f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.055f, hgt * 0.30f, z),
                        new Vector3(hw * 0.055f, hgt * 0.30f, z),
                        new Vector3(0, hgt * 0.36f, z + len * 0.005f));
                }
                break;

            case "hero":
            case "hero_default":
                for (int s = 0; s < 4; s++)
                {
                    float t = s / 3f;
                    float z = MathHelper.Lerp(len * 0.58f, len * 0.90f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.05f, hgt * (0.52f + t * 0.06f), z),
                        new Vector3(hw * 0.05f, hgt * (0.52f + t * 0.06f), z),
                        new Vector3(0, hgt * (0.62f + t * 0.06f), z + len * 0.004f));
                }
                for (int p = 0; p < 3; p++)
                {
                    float z = MathHelper.Lerp(len * 0.66f, len * 0.86f, p / 2f);
                    float xSpan = hw * MathHelper.Lerp(0.26f, 0.08f, p / 2f);
                    w.TriScorerAccent(
                        new Vector3(-xSpan, hgt * (0.66f + p * 0.04f), z),
                        new Vector3(xSpan, hgt * (0.66f + p * 0.04f), z),
                        new Vector3(0, hgt * (0.74f + p * 0.04f), z + len * 0.005f));
                }
                break;
        }
    }

    /// <summary>Crystalline utility accent pass � cargo spine veins, tool-arm facet strips, repair antenna blooms.</summary>
    private static void AppendCrystallineUtilityScorerAccentBands(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        float widthScale = hullKey is "freighter_bulk" ? 0.90f : hullKey is "miner_eva" ? 0.92f : 0.96f;
        hw *= widthScale;

        switch (hullKey)
        {
            case "miner_basic":
            case "miner_eva":
            case "miner_tractor":
                for (int s = 0; s < 5; s++)
                {
                    float t = s / 4f;
                    float z = MathHelper.Lerp(len * 0.02f, len * (hullKey is "miner_basic" ? 0.34f : 0.28f), t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.05f, hgt * (0.48f + t * 0.06f), z),
                        new Vector3(hw * 0.05f, hgt * (0.48f + t * 0.06f), z),
                        new Vector3(0, hgt * (0.56f + t * 0.06f), z + len * 0.005f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float xTip = side * hw * (hullKey is "miner_basic" ? 0.74f : 0.66f);
                    float zTip = len * (hullKey is "miner_basic" ? 0.60f : hullKey is "miner_tractor" ? 0.18f : 0.14f);
                    for (int e = 0; e < 3; e++)
                    {
                        float z = zTip - len * (e * 0.04f);
                        w.TriScorerAccent(
                            new Vector3(xTip, hgt * (0.20f + e * 0.02f), z),
                            new Vector3(xTip + side * hw * 0.04f, hgt * (0.26f + e * 0.02f), z + len * 0.02f),
                            new Vector3(xTip, hgt * (0.14f + e * 0.02f), z + len * 0.01f));
                    }
                }
                if (hullKey is "miner_tractor")
                {
                    for (int d = 0; d < 3; d++)
                    {
                        float z = len * (0.36f + d * 0.04f);
                        w.TriScorerAccent(
                            new Vector3(-hw * 0.06f, hgt * 0.66f, z), new Vector3(hw * 0.06f, hgt * 0.66f, z),
                            new Vector3(0, hgt * 0.74f, z + len * 0.006f));
                    }
                }
                break;

            case "transport_cargo":
                for (int s = 0; s < 6; s++)
                {
                    float t = s / 5f;
                    float z = MathHelper.Lerp(len * 0.04f, len * 0.42f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.06f, hgt * (0.44f + t * 0.04f), z),
                        new Vector3(hw * 0.06f, hgt * (0.44f + t * 0.04f), z),
                        new Vector3(0, hgt * (0.52f + t * 0.04f), z + len * 0.006f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float cx = side * hw * 0.42f;
                    w.TriScorerAccent(
                        new Vector3(cx - hw * 0.05f, hgt * 0.22f, len * 0.30f),
                        new Vector3(cx + hw * 0.05f, hgt * 0.22f, len * 0.30f),
                        new Vector3(cx, hgt * 0.28f, len * 0.34f));
                    w.TriScorerAccent(
                        new Vector3(cx, hgt * 0.18f, len * 0.24f),
                        new Vector3(cx - side * hw * 0.04f, hgt * 0.22f, len * 0.28f),
                        new Vector3(cx, hgt * 0.14f, len * 0.30f));
                }
                break;

            case "freighter_bulk":
                for (int s = 0; s < 5; s++)
                {
                    float t = s / 4f;
                    float z = MathHelper.Lerp(len * 0.06f, len * 0.44f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.08f, hgt * (0.36f + t * 0.04f), z),
                        new Vector3(hw * 0.08f, hgt * (0.36f + t * 0.04f), z),
                        new Vector3(0, hgt * (0.44f + t * 0.04f), z + len * 0.006f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float cx = side * hw * 0.36f;
                    w.TriScorerAccent(
                        new Vector3(cx - hw * 0.05f, hgt * 0.24f, len * 0.32f),
                        new Vector3(cx + hw * 0.05f, hgt * 0.24f, len * 0.32f),
                        new Vector3(cx, hgt * 0.30f, len * 0.36f));
                }
                w.TriScorerAccent(
                    new Vector3(-hw * 0.05f, hgt * 0.40f, len * 0.38f),
                    new Vector3(hw * 0.05f, hgt * 0.40f, len * 0.38f),
                    new Vector3(0, hgt * 0.46f, len * 0.42f));
                break;

            case "support_repair":
                for (int a = 0; a < 3; a++)
                {
                    float z = len * (0.06f + a * 0.050f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.04f, hgt * (0.74f + a * 0.04f), z),
                        new Vector3(hw * 0.04f, hgt * (0.74f + a * 0.04f), z),
                        new Vector3(0, hgt * (0.82f + a * 0.04f), z + len * 0.006f));
                }
                w.TriScorerAccent(
                    new Vector3(-hw * 0.03f, hgt * 1.00f, len * 0.18f),
                    new Vector3(hw * 0.03f, hgt * 1.00f, len * 0.18f),
                    new Vector3(0, hgt * 1.08f, len * 0.20f));
                for (int side = -1; side <= 1; side += 2)
                {
                    float xBoom = side * hw * 0.66f;
                    w.TriScorerAccent(
                        new Vector3(xBoom, hgt * 0.52f, len * 0.14f),
                        new Vector3(xBoom + side * hw * 0.04f, hgt * 0.58f, len * 0.16f),
                        new Vector3(xBoom, hgt * 0.46f, len * 0.18f));
                    w.TriScorerAccent(
                        new Vector3(xBoom, hgt * 0.44f, len * 0.08f),
                        new Vector3(xBoom + side * hw * 0.03f, hgt * 0.48f, len * 0.12f),
                        new Vector3(xBoom, hgt * 0.38f, len * 0.14f));
                }
                break;
        }
    }

    private static void AppendCrystallineCapitalCarrierScorerAccentBands(
        RaceMeshWriter w, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        for (int d = 0; d < 5; d++)
        {
            float t = d / 4f;
            float z = MathHelper.Lerp(-len * 0.08f, len * 0.20f, t);
            float xSpan = hw * MathHelper.Lerp(0.36f, 0.12f, t);
            float yBase = hgt * (0.50f + t * 0.06f);
            w.TriScorerAccent(
                new Vector3(-xSpan, yBase, z), new Vector3(xSpan, yBase, z),
                new Vector3(0, yBase + hgt * 0.06f, z + len * 0.005f));
        }
        for (int h = 0; h < 4; h++)
        {
            float z = len * (-0.06f + h * 0.05f);
            w.TriScorerAccent(
                new Vector3(-hw * 0.22f, hgt * 0.10f, z), new Vector3(hw * 0.22f, hgt * 0.10f, z),
                new Vector3(0, hgt * 0.14f, z + len * 0.006f));
        }
        for (int side = -1; side <= 1; side += 2)
        {
            float xOut = side * hw * 0.46f;
            for (int r = 0; r < 4; r++)
            {
                float z = len * (0.04f + r * 0.08f);
                w.TriScorerAccent(
                    new Vector3(xOut, hgt * 0.20f, z),
                    new Vector3(xOut - side * hw * 0.03f, hgt * 0.24f, z + len * 0.008f),
                    new Vector3(xOut, hgt * 0.16f, z + len * 0.010f));
            }
        }
        for (int p = 0; p < 3; p++)
        {
            float z = len * (0.06f + p * 0.06f);
            w.TriScorerAccent(
                new Vector3(-hw * 0.14f, hgt * 0.28f, z), new Vector3(hw * 0.14f, hgt * 0.28f, z),
                new Vector3(0, hgt * 0.32f, z + len * 0.006f));
        }
    }

    private static void AppendCrystallineCapitalCruiserScorerAccentBands(
        RaceMeshWriter w, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        for (int d = 0; d < 5; d++)
        {
            float t = d / 4f;
            float z = MathHelper.Lerp(len * 0.02f, len * 0.24f, t);
            w.TriScorerAccent(
                new Vector3(-hw * 0.08f, hgt * (0.50f + t * 0.06f), z),
                new Vector3(hw * 0.08f, hgt * (0.50f + t * 0.06f), z),
                new Vector3(0, hgt * (0.58f + t * 0.06f), z + len * 0.005f));
        }
        for (int p = 0; p < 3; p++)
        {
            float z = len * (0.54f + p * 0.06f);
            w.TriScorerAccent(
                new Vector3(-hw * 0.05f, hgt * 0.30f, z), new Vector3(hw * 0.05f, hgt * 0.30f, z),
                new Vector3(0, hgt * 0.36f, z + len * 0.006f));
        }
        for (int side = -1; side <= 1; side += 2)
        {
            float xOut = side * hw * 0.40f;
            for (int r = 0; r < 4; r++)
            {
                float z = len * (0.04f + r * 0.07f);
                w.TriScorerAccent(
                    new Vector3(xOut, hgt * 0.16f, z),
                    new Vector3(xOut - side * hw * 0.03f, hgt * 0.20f, z + len * 0.008f),
                    new Vector3(xOut, hgt * 0.12f, z + len * 0.010f));
            }
        }
        for (int c = 0; c < 3; c++)
        {
            float z = len * (0.08f + c * 0.06f);
            w.TriScorerAccent(
                new Vector3(-hw * 0.06f, hgt * 0.56f, z), new Vector3(hw * 0.06f, hgt * 0.56f, z),
                new Vector3(0, hgt * 0.62f, z + len * 0.005f));
        }
    }

    private static void AppendCrystallineCapitalDreadnoughtScorerAccentBands(
        RaceMeshWriter w, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        for (int d = 0; d < 4; d++)
        {
            float z = len * (0.34f + d * 0.08f);
            w.TriScorerAccent(
                new Vector3(-hw * 0.06f, hgt * 0.34f, z), new Vector3(hw * 0.06f, hgt * 0.34f, z),
                new Vector3(0, hgt * 0.40f, z + len * 0.005f));
        }
        for (int side = -1; side <= 1; side += 2)
        {
            float xTip = side * hw * 0.28f;
            w.TriScorerAccent(
                new Vector3(xTip, hgt * 0.28f, len * 0.54f),
                new Vector3(xTip - side * hw * 0.03f, hgt * 0.32f, len * 0.58f),
                new Vector3(xTip, hgt * 0.24f, len * 0.56f));
            for (int r = 0; r < 3; r++)
            {
                float z = len * (0.46f + r * 0.06f);
                w.TriScorerAccent(
                    new Vector3(xTip, hgt * (0.18f + r * 0.02f), z),
                    new Vector3(xTip - side * hw * 0.03f, hgt * (0.22f + r * 0.02f), z + len * 0.008f),
                    new Vector3(xTip, hgt * (0.14f + r * 0.02f), z + len * 0.010f));
            }
        }
        for (int p = 0; p < 3; p++)
        {
            float z = len * (0.52f + p * 0.05f);
            w.TriScorerAccent(
                new Vector3(-hw * 0.05f, hgt * 0.38f, z), new Vector3(hw * 0.05f, hgt * 0.38f, z),
                new Vector3(0, hgt * 0.44f, z + len * 0.006f));
        }
    }

    private static void ApplySpinyShipDetail(
        RaceMeshWriter w, RaceVisualDefinition race, string hullKey, float len, float wid, float hgt)
    {
        if (hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk" or "support_repair")
        {
            ApplySpinyUtilityShipDetail(w, hullKey, len, wid, hgt);
            return;
        }

        if (hullKey is "cruiser" or "cruiser_heavy" or "carrier" or "carrier_command" or "dreadnought")
        {
            ApplyOrganicCapitalShipDetail(w, hullKey, len, wid, hgt);
            return;
        }

        if (hullKey is "corvette" or "corvette_fast" or "frigate" or "frigate_strike"
            or "gunship" or "gunship_heavy" or "bomber" or "bomber_heavy"
            or "destroyer" or "destroyer_assault")
        {
            ApplySpinyMediumCombatShipDetail(w, hullKey, len, wid, hgt);
            return;
        }

        bool isScout = hullKey is "scout" or "scout_light";
        bool isFighter = hullKey is "fighter" or "fighter_basic";
        bool isInterceptor = hullKey is "interceptor" or "interceptor_mk2";
        bool isDrone = hullKey is "drone" or "drone_swarm";
        bool isHero = hullKey is "hero" or "hero_default";
        bool isCompactCraft = isScout || isFighter || isInterceptor || isDrone;
        bool isReferenceCraft = isFighter || isHero;
        if (!isCompactCraft && !isHero)
            return;

        float hw = wid * 0.5f;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float asymBias = 0.05f;
        int spineSegs = isDrone ? 4 : isScout ? 5 : isInterceptor ? 4 : isFighter ? 4 : 5;
        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < spineSegs; i++)
            {
                float t = i / MathF.Max(1f, spineSegs - 1);
                float z = MathHelper.Lerp(-len * 0.04f, len * (isScout ? 0.18f : isDrone ? 0.16f : 0.14f), t);
                float xOut = side * hw * MathHelper.Lerp(isScout ? 0.56f : isDrone ? 0.42f : 0.52f, 0.34f, t) + asymBias * side;
                TriMat(w, accent,
                    new Vector3(xOut, hgt * (0.16f + t * 0.04f), z),
                    new Vector3(xOut - side * hw * 0.03f, hgt * (0.13f + t * 0.04f), z + len * 0.008f),
                    new Vector3(xOut, hgt * (0.11f + t * 0.03f), z + len * 0.012f));
            }
        }

        if (isHero)
        {
            for (int p = 0; p < 4; p++)
            {
                float t = p / 3f;
                float z = MathHelper.Lerp(-len * 0.06f, len * 0.22f, t);
                float xSpan = hw * MathHelper.Lerp(0.30f, 0.08f, t) + asymBias;
                TriMat(w, accent,
                    new Vector3(-xSpan, hgt * (0.60f + t * 0.06f), z), new Vector3(xSpan, hgt * (0.60f + t * 0.06f), z),
                    new Vector3(asymBias, hgt * (0.68f + t * 0.06f), z + len * 0.006f));
            }
        }

        if (isReferenceCraft)
            AddSpinyReferenceCraftSubstrate(w, hw, hgt, len, isHero);
        else
            AddSpinyCompactCraftSubstrate(w, hullKey, hw, hgt, len);

        AddAsymmetricSmallCraftGameplayComponentBands(w, hullKey, hw, hgt, len);
    }

    private static void AddSpinyCompactCraftSubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var carapace = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        bool isScout = hullKey is "scout" or "scout_light";
        bool isInterceptor = hullKey is "interceptor" or "interceptor_mk2";
        bool isDrone = hullKey is "drone" or "drone_swarm";
        float asymBias = 0.05f;

        int bellyPlates = 2;
        for (int i = 0; i < bellyPlates; i++)
        {
            float t = i / MathF.Max(1f, bellyPlates - 1);
            float z = MathHelper.Lerp(isDrone ? -len * 0.06f : -len * 0.05f, isScout ? len * 0.22f : len * 0.20f, t);
            float halfW = hw * MathHelper.Lerp(isScout ? 0.18f : isDrone ? 0.12f : 0.20f, 0.14f, t);
            TriMat(w, panel,
                new Vector3(-halfW + asymBias, hgt * 0.03f, z), new Vector3(halfW + asymBias, hgt * 0.03f, z),
                new Vector3(asymBias, hgt * (isDrone ? 0.14f : 0.16f), z + len * 0.008f));
            if (i == bellyPlates - 1)
                TriMat(w, accent,
                    new Vector3(-halfW * 0.42f + asymBias, hgt * 0.06f, z + len * 0.002f),
                    new Vector3(halfW * 0.42f + asymBias, hgt * 0.06f, z + len * 0.002f),
                    new Vector3(asymBias, hgt * 0.12f, z + len * 0.006f));
        }

        int dorsalBands = 2;
        for (int d = 0; d < dorsalBands; d++)
        {
            float t = d / MathF.Max(1f, dorsalBands - 1);
            float z = MathHelper.Lerp(len * 0.02f, len * (isScout ? 0.20f : isDrone ? 0.16f : 0.18f), t);
            TriMat(w, carapace,
                new Vector3(-hw * 0.06f + asymBias, hgt * (0.42f + t * 0.08f), z),
                new Vector3(hw * 0.04f + asymBias, hgt * (0.42f + t * 0.08f), z),
                new Vector3(asymBias, hgt * (0.50f + t * 0.08f), z + len * 0.006f));
            if (d == dorsalBands - 1)
                TriMat(w, accent,
                    new Vector3(-hw * 0.03f + asymBias, hgt * (0.48f + t * 0.08f), z + len * 0.003f),
                    new Vector3(hw * 0.02f + asymBias, hgt * (0.48f + t * 0.08f), z + len * 0.003f),
                    new Vector3(asymBias, hgt * (0.54f + t * 0.08f), z + len * 0.006f));
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * hw * (isScout ? 0.24f : isDrone ? 0.14f : isInterceptor ? 0.28f : 0.26f) + asymBias * side;
            for (int s = 0; s < 2; s++)
            {
                float z = len * (isDrone ? (-0.02f + s * 0.10f) : (0.02f + s * 0.08f));
                TriMat(w, carapace,
                    new Vector3(x, hgt * 0.24f, z), new Vector3(x - side * hw * 0.03f, hgt * 0.28f, z + len * 0.01f),
                    new Vector3(x, hgt * 0.20f, z + len * 0.014f));
            }
        }
    }

    private static void AddSpinyReferenceCraftSubstrate(RaceMeshWriter w, float hw, float hgt, float len, bool isHero)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var carapace = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float asymBias = 0.06f;
        int dorsalBands = isHero ? 3 : 2;
        for (int i = 0; i < dorsalBands; i++)
        {
            float t = i / MathF.Max(1f, dorsalBands - 1);
            float z = MathHelper.Lerp(-len * 0.04f, len * (isHero ? 0.24f : 0.18f), t);
            TriMat(w, carapace,
                new Vector3(-hw * 0.08f + asymBias, hgt * (0.44f + t * 0.06f), z),
                new Vector3(hw * 0.04f + asymBias, hgt * (0.44f + t * 0.06f), z),
                new Vector3(asymBias, hgt * (0.52f + t * 0.06f), z + len * 0.006f));
            if (i == dorsalBands - 1)
                TriMat(w, accent,
                    new Vector3(-hw * 0.05f + asymBias, hgt * (0.50f + t * 0.06f), z + len * 0.003f),
                    new Vector3(hw * 0.03f + asymBias, hgt * (0.50f + t * 0.06f), z + len * 0.003f),
                    new Vector3(asymBias, hgt * (0.56f + t * 0.06f), z + len * 0.006f));
        }

        for (int i = 0; i < 2; i++)
        {
            float t = i / 3f;
            float z = MathHelper.Lerp(-len * 0.06f, len * 0.16f, t);
            float halfW = hw * MathHelper.Lerp(0.24f, 0.16f, t);
            TriMat(w, panel,
                new Vector3(-halfW + asymBias, hgt * 0.04f, z), new Vector3(halfW + asymBias, hgt * 0.04f, z),
                new Vector3(asymBias, hgt * 0.16f, z + len * 0.008f));
            TriMat(w, carapace,
                new Vector3(-halfW * 0.78f + asymBias, hgt * 0.06f, z - len * 0.004f),
                new Vector3(halfW * 0.78f + asymBias, hgt * 0.06f, z - len * 0.004f),
                new Vector3(asymBias, hgt * 0.12f, z));
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * hw * (isHero ? 0.30f : 0.28f) + asymBias * side;
            for (int s = 0; s < 2; s++)
            {
                float z = len * (0.04f + s * 0.06f);
                TriMat(w, carapace,
                    new Vector3(x, hgt * 0.26f, z), new Vector3(x - side * hw * 0.03f, hgt * 0.30f, z + len * 0.01f),
                    new Vector3(x, hgt * 0.22f, z + len * 0.012f));
            }
        }
    }

    private static void ApplySpinyUtilityShipDetail(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var shadow = RaceMeshWriter.HullMaterial.Radiator;
        var carapace = RaceMeshWriter.HullMaterial.Truss;
        var engineMat = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float hw = wid * 0.5f;
        float widthScale = hullKey is "freighter_bulk" ? 0.88f : hullKey is "miner_eva" ? 0.90f : 0.94f;
        hw *= widthScale;
        bool triBudgetTight = hullKey is "freighter_bulk" or "transport_cargo";
        bool isMiner = hullKey is "miner_basic" or "miner_eva" or "miner_tractor";
        bool isSupport = hullKey is "support_repair";
        float asymBias = 0.07f;
        int veinSegs = hullKey is "freighter_bulk" ? 2 : triBudgetTight ? 3 : isMiner ? 4 : 4;

        for (int side = -1; side <= 1; side += 2)
        {
            float xOut = side * hw * (hullKey is "freighter_bulk" ? 0.44f : hullKey is "transport_cargo" ? 0.42f : 0.52f)
                + asymBias * side;
            for (int i = 0; i < veinSegs; i++)
            {
                float t = i / MathF.Max(1f, veinSegs - 1);
                float z = MathHelper.Lerp(-len * 0.04f, len * (hullKey is "freighter_bulk" ? 0.30f : triBudgetTight ? 0.28f : isMiner ? 0.16f : isSupport ? 0.12f : 0.10f), t);
                TriMat(w, accent,
                    new Vector3(xOut, hgt * (0.14f + t * 0.05f), z),
                    new Vector3(xOut - side * hw * 0.032f, hgt * (0.11f + t * 0.05f), z + len * 0.010f),
                    new Vector3(xOut, hgt * (0.09f + t * 0.04f), z + len * 0.014f));
            }
        }

        if (hullKey is "miner_basic")
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xTip = side * hw * 0.66f;
                TriMat(w, weapon,
                    new Vector3(xTip, hgt * 0.18f, len * 0.42f),
                    new Vector3(xTip + side * hw * 0.04f, hgt * 0.24f, len * 0.48f),
                    new Vector3(xTip, hgt * 0.12f, len * 0.50f));
                TriMat(w, engineMat,
                    new Vector3(xTip - side * hw * 0.03f, hgt * 0.05f, len * 0.06f),
                    new Vector3(xTip + side * hw * 0.03f, hgt * 0.05f, len * 0.06f),
                    new Vector3(xTip, hgt * 0.11f, len * 0.10f));
            }
            TriMat(w, engineMat,
                new Vector3(-hw * 0.16f, hgt * 0.03f, -len * 0.08f),
                new Vector3(hw * 0.16f, hgt * 0.03f, -len * 0.08f),
                new Vector3(0, hgt * 0.09f, -len * 0.04f));
        }

        if (hullKey is "support_repair")
        {
            for (int a = 0; a < 3; a++)
            {
                float z = len * (0.04f + a * 0.050f);
                TriMat(w, accent,
                    new Vector3(-hw * 0.04f + asymBias, hgt * (0.72f + a * 0.04f), z),
                    new Vector3(hw * 0.04f + asymBias, hgt * (0.72f + a * 0.04f), z),
                    new Vector3(asymBias, hgt * (0.80f + a * 0.04f), z + len * 0.008f));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float xBoom = side * hw * 0.60f;
                TriMat(w, weapon,
                    new Vector3(xBoom, hgt * 0.52f, len * 0.10f),
                    new Vector3(xBoom + side * hw * 0.05f, hgt * 0.58f, len * 0.12f),
                    new Vector3(xBoom, hgt * 0.46f, len * 0.14f));
                TriMat(w, accent,
                    new Vector3(xBoom, hgt * 0.64f, len * 0.08f),
                    new Vector3(xBoom + side * hw * 0.03f, hgt * 0.70f, len * 0.10f),
                    new Vector3(xBoom, hgt * 0.58f, len * 0.12f));
            }
        }

        if (hullKey is "transport_cargo")
        {
            for (int s = 0; s < 3; s++)
            {
                float z = len * (0.08f + s * 0.10f);
                TriMat(w, carapace,
                    new Vector3(-hw * 0.06f + asymBias, hgt * 0.38f, z),
                    new Vector3(hw * 0.06f + asymBias, hgt * 0.38f, z),
                    new Vector3(asymBias, hgt * 0.44f, z + len * 0.010f));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float cx = side * hw * 0.40f + asymBias * side;
                TriMat(w, weapon,
                    new Vector3(cx - hw * 0.06f, hgt * 0.20f, len * 0.24f),
                    new Vector3(cx + hw * 0.06f, hgt * 0.20f, len * 0.24f),
                    new Vector3(cx, hgt * 0.26f, len * 0.28f));
            }
        }

        if (hullKey is "freighter_bulk")
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float cx = side * hw * 0.36f + asymBias * side;
                TriMat(w, shadow,
                    new Vector3(cx - hw * 0.06f, hgt * 0.14f, len * 0.26f),
                    new Vector3(cx + hw * 0.06f, hgt * 0.14f, len * 0.26f),
                    new Vector3(cx, hgt * 0.20f, len * 0.32f));
            }
            TriMat(w, accent,
                new Vector3(-hw * 0.08f + asymBias, hgt * 0.32f, len * 0.34f),
                new Vector3(hw * 0.08f + asymBias, hgt * 0.32f, len * 0.34f),
                new Vector3(asymBias, hgt * 0.38f, len * 0.40f));
        }

        AddSpinyUtilitySubstrate(w, hullKey, hw, hgt, len);

        if (!triBudgetTight)
        {
            TriMat(w, engineMat,
                new Vector3(-hw * 0.12f, 0, -len * 0.08f), new Vector3(hw * 0.12f, 0, -len * 0.08f),
                new Vector3(0, hgt * 0.06f, -len * 0.12f));
        }
    }

    /// <summary>Belly/flank carapace membrane substrate bands � magenta spine veins readable under team tint.</summary>
    private static void AddSpinyUtilitySubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var membrane = RaceMeshWriter.HullMaterial.Hull;
        var vein = RaceMeshWriter.HullMaterial.Solar;
        var shadow = RaceMeshWriter.HullMaterial.Radiator;
        var emboss = RaceMeshWriter.HullMaterial.Truss;
        bool isCargo = hullKey is "freighter_bulk" or "transport_cargo";
        bool isMiner = hullKey is "miner_basic" or "miner_eva" or "miner_tractor";
        bool isSupport = hullKey is "support_repair";
        float asymBias = 0.05f;
        int bellyPlates = 2;

        for (int i = 0; i < bellyPlates; i++)
        {
            float t = i / MathF.Max(1f, bellyPlates - 1);
            float z = MathHelper.Lerp(-len * 0.06f, len * (isCargo ? 0.26f : isSupport ? 0.16f : isMiner ? 0.18f : 0.14f), t);
            float halfW = hw * MathHelper.Lerp(
                hullKey is "freighter_bulk" ? 0.28f : isCargo ? 0.20f : 0.22f,
                hullKey is "freighter_bulk" ? 0.12f : 0.10f, t);
            TriMat(w, shadow,
                new Vector3(-halfW + asymBias, hgt * 0.02f, z), new Vector3(halfW + asymBias, hgt * 0.02f, z),
                new Vector3(asymBias, hgt * (0.16f + t * 0.04f), z + len * 0.012f));
            if (i == bellyPlates - 1)
            {
                TriMat(w, vein,
                    new Vector3(-halfW * 0.5f + asymBias, hgt * (0.12f + t * 0.03f), z + len * 0.006f),
                    new Vector3(halfW * 0.5f + asymBias, hgt * (0.12f + t * 0.03f), z + len * 0.006f),
                    new Vector3(asymBias, hgt * (0.18f + t * 0.03f), z + len * 0.014f));
            }
        }

        for (int side = -1; side <= 1; side += 2)
        {
            int flankSegs = 2;
            for (int i = 0; i < flankSegs; i++)
            {
                float t = i / MathF.Max(1f, flankSegs - 1);
                float z = MathHelper.Lerp(-len * 0.04f, len * (isCargo ? 0.28f : isSupport ? 0.14f : 0.20f), t);
                float xOut = side * hw * MathHelper.Lerp(isCargo ? 0.36f : 0.44f, 0.26f, t) + asymBias * side;
                TriMat(w, vein,
                    new Vector3(xOut, hgt * (0.14f + t * 0.04f), z),
                    new Vector3(xOut - side * hw * 0.022f, hgt * (0.12f + t * 0.04f), z + len * 0.006f),
                    new Vector3(xOut, hgt * (0.10f + t * 0.03f), z + len * 0.010f));
            }
        }

        int spineSegs = 2;
        for (int i = 0; i < spineSegs; i++)
        {
            float t = i / MathF.Max(1f, spineSegs - 1);
            float z = MathHelper.Lerp(-len * 0.04f, len * (hullKey switch
            {
                "freighter_bulk" => 0.36f,
                "transport_cargo" => 0.40f,
                "miner_basic" => 0.28f,
                "miner_eva" => 0.22f,
                "miner_tractor" => 0.26f,
                "support_repair" => 0.18f,
                _ => 0.14f
            }), t);
            float xSpan = hw * MathHelper.Lerp(
                hullKey is "freighter_bulk" ? 0.14f : isCargo ? 0.12f : 0.10f,
                hullKey is "freighter_bulk" ? 0.04f : 0.03f, t);
            TriMat(w, shadow,
                new Vector3(-xSpan + asymBias, hgt * (0.42f + t * 0.06f), z),
                new Vector3(xSpan + asymBias, hgt * (0.42f + t * 0.06f), z),
                new Vector3(asymBias, hgt * (0.52f + t * 0.04f), z + len * 0.010f));
            TriMat(w, vein,
                new Vector3(-xSpan * 0.7f + asymBias, hgt * (0.52f + t * 0.04f), z + len * 0.006f),
                new Vector3(xSpan * 0.7f + asymBias, hgt * (0.52f + t * 0.04f), z + len * 0.006f),
                new Vector3(asymBias, hgt * (0.58f + t * 0.03f), z + len * 0.012f));
        }
    }

    private static void ApplySpinyCapitalShipDetail(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float hw = wid * 0.5f;
        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDreadnought = hullKey is "dreadnought";

        int dorsalSegs = isDreadnought ? 6 : isCruiser ? 7 : 5;
        float dorsalBase = isCarrier ? 0.42f : 0.52f;
        float dorsalRise = isCarrier ? 0.05f : 0.07f;
        for (int i = 0; i < dorsalSegs; i++)
        {
            float t = i / MathF.Max(1f, dorsalSegs - 1);
            float z = MathHelper.Lerp(len * 0.06f, len * (isCarrier ? 0.66f : 0.60f), t);
            TriMat(w, accent,
                new Vector3(0, hgt * (dorsalBase + t * dorsalRise), z),
                new Vector3(-hw * 0.10f, hgt * (dorsalBase - 0.10f + t * 0.03f), z - len * 0.02f),
                new Vector3(hw * 0.10f, hgt * (dorsalBase - 0.10f + t * 0.03f), z - len * 0.02f));
        }

        AddSpinyCapitalSubstrate(w, hullKey, hw, hgt, len);
        if (isCarrier)
            AddSpinyCarrierDeckMembranes(w, hw, hgt, len);
        AddSpinyAccentRidgeBands(w, hullKey, hw, hgt, len);
        AddSpinyCapitalGameplayComponentBands(w, hullKey, hw, hgt, len);
    }

    private static void AddSpinyCapitalSubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var emboss = RaceMeshWriter.HullMaterial.Truss;
        var recess = RaceMeshWriter.HullMaterial.Hull;
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        bool isDreadnought = hullKey is "dreadnought";
        int bellyPlates = isCruiser ? 3 : 2;

        for (int i = 0; i < bellyPlates; i++)
        {
            float t = bellyPlates == 1 ? 0.5f : i / (float)(bellyPlates - 1);
            float z = MathHelper.Lerp(-len * 0.12f, len * (isCarrier ? 0.30f : isDreadnought ? 0.26f : 0.24f), t);
            float halfW = hw * MathHelper.Lerp(
                isCarrier ? 0.38f : isDreadnought ? 0.34f : 0.32f, 0.18f, t);
            TriMat(w, panel,
                new Vector3(-halfW, hgt * 0.05f, z), new Vector3(halfW, hgt * 0.05f, z),
                new Vector3(0, hgt * 0.14f, z + len * 0.010f));
            if (isCarrier && i == bellyPlates - 1)
            {
                TriMat(w, recess,
                    new Vector3(-halfW * 0.72f, hgt * 0.08f, z + len * 0.004f),
                    new Vector3(halfW * 0.72f, hgt * 0.08f, z + len * 0.004f),
                    new Vector3(0, hgt * 0.11f, z + len * 0.008f));
            }
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float xOut = side * hw * (isCarrier ? 0.40f : isDreadnought ? 0.48f : 0.42f);
            int flankBands = isDreadnought ? 5 : isCarrier ? 4 : 4;
            for (int f = 0; f < flankBands; f++)
            {
                float t = f / MathF.Max(1f, flankBands - 1);
                float z = MathHelper.Lerp(-len * 0.08f, len * (isCarrier ? 0.20f : 0.18f), t);
                TriMat(w, panel,
                    new Vector3(xOut, hgt * 0.12f, z),
                    new Vector3(xOut - side * hw * 0.04f, hgt * 0.18f, z + len * 0.008f),
                    new Vector3(xOut, hgt * 0.08f, z + len * 0.012f));
            }
        }

        if (isCruiser)
        {
            for (int k = 0; k < 2; k++)
            {
                float z = MathHelper.Lerp(len * 0.02f, len * 0.26f, k);
                TriMat(w, emboss,
                    new Vector3(-hw * 0.14f, hgt * 0.32f, z), new Vector3(hw * 0.14f, hgt * 0.32f, z),
                    new Vector3(0, hgt * 0.36f, z + len * 0.006f));
            }
        }

        if (isDreadnought)
        {
            for (int k = 0; k < 2; k++)
            {
                float z = MathHelper.Lerp(len * 0.44f, len * 0.66f, k);
                TriMat(w, emboss,
                    new Vector3(-hw * 0.12f, hgt * 0.28f, z), new Vector3(hw * 0.12f, hgt * 0.28f, z),
                    new Vector3(0, hgt * 0.34f, z + len * 0.008f));
            }
        }
    }

    private static void AddSpinyCarrierDeckMembranes(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var recess = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float deckZ = MathHelper.Lerp(-len * 0.16f, len * 0.08f, 0.5f);
        TriMat(w, panel,
            new Vector3(-hw * 0.46f, hgt * 0.20f, deckZ), new Vector3(hw * 0.46f, hgt * 0.20f, deckZ),
            new Vector3(0, hgt * 0.24f, deckZ + len * 0.006f));
        for (int row = 0; row < 3; row++)
        {
            float z = MathHelper.Lerp(-len * 0.10f, len * 0.06f, row / 2f);
            float xSpan = hw * MathHelper.Lerp(0.34f, 0.18f, row / 2f);
            TriMat(w, recess,
                new Vector3(-xSpan, hgt * 0.18f, z), new Vector3(xSpan, hgt * 0.18f, z),
                new Vector3(0, hgt * 0.21f, z + len * 0.005f));
        }
        for (int h = 0; h < 4; h++)
        {
            float z = len * (0.08f - h * 0.07f);
            TriMat(w, accent,
                new Vector3(-hw * 0.30f, hgt * 0.22f, z), new Vector3(hw * 0.30f, hgt * 0.22f, z),
                new Vector3(0, hgt * 0.26f, z + len * 0.008f));
        }
        for (int ring = 0; ring < 3; ring++)
        {
            float z = len * (-0.02f + ring * 0.08f);
            for (int side = -1; side <= 1; side += 2)
            {
                float xOut = side * hw * 0.26f;
                TriMat(w, accent,
                    new Vector3(xOut, hgt * 0.24f, z),
                    new Vector3(xOut - side * hw * 0.03f, hgt * 0.28f, z + len * 0.006f),
                    new Vector3(xOut, hgt * 0.20f, z + len * 0.008f));
            }
        }
    }

    private static void AddSpinyAccentRidgeBands(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var accent = RaceMeshWriter.HullMaterial.Solar;
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDreadnought = hullKey is "dreadnought";
        int rings = isDreadnought ? 5 : isCarrier ? 5 : 4;
        for (int v = 0; v < rings; v++)
        {
            float t = v / MathF.Max(1f, rings - 1);
            float z = MathHelper.Lerp(-len * 0.06f, len * 0.22f, t);
            for (int side = -1; side <= 1; side += 2)
            {
                float xOut = side * hw * (isCarrier ? 0.52f : isDreadnought ? 0.58f : 0.48f);
                TriMat(w, accent,
                    new Vector3(xOut, hgt * (0.20f + t * 0.04f), z),
                    new Vector3(xOut - side * hw * 0.03f, hgt * (0.24f + t * 0.04f), z + len * 0.008f),
                    new Vector3(xOut, hgt * (0.16f + t * 0.04f), z + len * 0.012f));
            }
        }
    }

    private static void AddSpinyCapitalGameplayComponentBands(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDreadnought = hullKey is "dreadnought";

        for (int e = 0; e < 2; e++)
        {
            float x = MathHelper.Lerp(-hw * 0.22f, hw * 0.22f, e);
            TriMat(w, engine,
                new Vector3(x - hw * 0.06f, hgt * 0.04f, -len * 0.14f), new Vector3(x + hw * 0.06f, hgt * 0.04f, -len * 0.14f),
                new Vector3(x, hgt * 0.10f, -len * 0.10f));
        }

        if (isDreadnought)
        {
            TriMat(w, weapon,
                new Vector3(-hw * 0.10f, hgt * 0.08f, len * 0.58f), new Vector3(hw * 0.10f, hgt * 0.08f, len * 0.58f),
                new Vector3(0, hgt * 0.14f, len * 0.64f));
        }
        else if (isCarrier)
        {
            TriMat(w, weapon,
                new Vector3(-hw * 0.28f, hgt * 0.26f, len * 0.04f), new Vector3(hw * 0.28f, hgt * 0.26f, len * 0.04f),
                new Vector3(0, hgt * 0.30f, len * 0.08f));
        }
        else
        {
            TriMat(w, weapon,
                new Vector3(-hw * 0.08f, hgt * 0.16f, len * 0.56f), new Vector3(hw * 0.08f, hgt * 0.16f, len * 0.56f),
                new Vector3(0, hgt * 0.22f, len * 0.62f));
        }
    }

    private static void ApplySpinyMediumCombatShipDetail(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var carapace = RaceMeshWriter.HullMaterial.Truss;
        var engineGlow = RaceMeshWriter.HullMaterial.Engine;
        float hw = wid * 0.5f;
        float asymBias = 0.05f;
        bool isCorvette = hullKey is "corvette" or "corvette_fast";
        bool isFrigate = hullKey is "frigate" or "frigate_strike";
        bool isGunship = hullKey is "gunship" or "gunship_heavy";
        bool isBomber = hullKey is "bomber" or "bomber_heavy";
        bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";

        int crestBands = isDestroyer ? 3 : 2;
        float crestReach = isFrigate ? 0.28f : isBomber ? 0.28f : isDestroyer ? 0.32f : isCorvette ? 0.22f : 0.20f;
        for (int p = 0; p < crestBands; p++)
        {
            float t = p / MathF.Max(1f, crestBands - 1);
            float z = MathHelper.Lerp(-len * 0.04f, len * crestReach, t);
            float xSpan = hw * MathHelper.Lerp(0.34f, 0.10f, t) + asymBias;
            TriMat(w, accent,
                new Vector3(-xSpan, hgt * (0.48f + t * 0.04f), z), new Vector3(xSpan, hgt * (0.48f + t * 0.04f), z),
                new Vector3(asymBias, hgt * (0.56f + t * 0.04f), z + len * 0.006f));
        }

        if (isGunship)
        {
            float zChin = len * 0.12f;
            TriMat(w, carapace,
                new Vector3(-hw * 0.20f + asymBias, hgt * 0.02f, zChin), new Vector3(hw * 0.20f + asymBias, hgt * 0.02f, zChin),
                new Vector3(asymBias, hgt * 0.08f, zChin + len * 0.010f));
            TriMat(w, engineGlow,
                new Vector3(-hw * 0.10f + asymBias, hgt * 0.02f, -len * 0.10f), new Vector3(hw * 0.10f + asymBias, hgt * 0.02f, -len * 0.10f),
                new Vector3(asymBias, hgt * 0.08f, -len * 0.08f));
        }

        if (isBomber)
        {
            for (int b = 0; b < 2; b++)
            {
                float z = len * (0.04f + b * 0.06f);
                float halfW = hw * MathHelper.Lerp(0.30f, 0.12f, b / 4f);
                TriMat(w, accent,
                    new Vector3(-halfW + asymBias, hgt * 0.05f, z), new Vector3(halfW + asymBias, hgt * 0.05f, z),
                    new Vector3(asymBias, hgt * 0.11f, z + len * 0.008f));
            }
        }

        AddSpinyMediumCombatSubstrate(w, hullKey, hw, hgt, len);
        AddAsymmetricMediumCombatGameplayComponentBands(w, hullKey, hw, hgt, len);
    }

    /// <summary>Carapace membrane substrate bands for Voidborn medium combat � vertex luminance under team tint.</summary>
    private static void AddSpinyMediumCombatSubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var recess = RaceMeshWriter.HullMaterial.Radiator;
        var carapace = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engineGlow = RaceMeshWriter.HullMaterial.Engine;
        float asymBias = 0.05f;

        bool isCorvette = hullKey is "corvette" or "corvette_fast";
        bool isFrigate = hullKey is "frigate" or "frigate_strike";
        bool isGunship = hullKey is "gunship" or "gunship_heavy";
        bool isBomber = hullKey is "bomber" or "bomber_heavy";
        bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";

        int bellyBands = 2;
        float bellyWidth = isBomber ? 0.48f : isDestroyer ? 0.40f : isFrigate ? 0.38f : isGunship ? 0.36f : 0.34f;
        float bellyReach = isCorvette ? 0.22f : isBomber ? 0.26f : isFrigate ? 0.28f : isDestroyer ? 0.34f : 0.20f;
        for (int i = 0; i < bellyBands; i++)
        {
            float t = bellyBands > 1 ? i / (bellyBands - 1f) : 0f;
            float z = MathHelper.Lerp(-len * 0.06f, len * bellyReach, t);
            float halfW = hw * MathHelper.Lerp(bellyWidth, bellyWidth * 0.66f, t);
            TriMat(w, recess,
                new Vector3(-halfW + asymBias, hgt * 0.04f, z), new Vector3(halfW + asymBias, hgt * 0.04f, z),
                new Vector3(asymBias, hgt * (0.12f + t * 0.02f), z + len * 0.008f));
        }

        int flankBands = 2;
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * hw * (isGunship ? 0.38f : isFrigate ? 0.36f : isBomber ? 0.36f : isDestroyer ? 0.34f : 0.32f)
                + asymBias * side;
            for (int s = 0; s < flankBands; s++)
            {
                float z = len * (0.02f + s * (isDestroyer ? 0.07f : 0.06f));
                TriMat(w, carapace,
                    new Vector3(x, hgt * 0.20f, z), new Vector3(x - side * hw * 0.03f, hgt * 0.24f, z + len * 0.01f),
                    new Vector3(x, hgt * 0.16f, z + len * 0.012f));
                if (s % 2 == 0)
                {
                    TriMat(w, accent,
                        new Vector3(x - side * hw * 0.02f, hgt * 0.18f, z + len * 0.004f),
                        new Vector3(x, hgt * 0.22f, z + len * 0.008f),
                        new Vector3(x - side * hw * 0.01f, hgt * 0.14f, z + len * 0.010f));
                }
            }
        }

        int dorsalStripes = isCorvette ? 5 : isFrigate ? 6 : isBomber ? 5 : isGunship ? 4 : 7;
        float dorsalReach = isFrigate ? 0.30f : isBomber ? 0.26f : isDestroyer ? 0.36f : isCorvette ? 0.24f : 0.22f;
        for (int d = 0; d < dorsalStripes; d++)
        {
            float t = d / MathF.Max(1f, dorsalStripes - 1);
            float z = MathHelper.Lerp(-len * 0.02f, len * dorsalReach, t);
            float xSpan = hw * MathHelper.Lerp(0.26f, 0.08f, t) + asymBias;
            TriMat(w, recess,
                new Vector3(-xSpan, hgt * (0.46f + t * 0.04f), z), new Vector3(xSpan, hgt * (0.46f + t * 0.04f), z),
                new Vector3(asymBias, hgt * (0.50f + t * 0.04f), z + len * 0.006f));
            if (d % 2 == 0)
            {
                TriMat(w, accent,
                    new Vector3(-xSpan * 0.72f, hgt * (0.48f + t * 0.04f), z + len * 0.003f),
                    new Vector3(xSpan * 0.72f, hgt * (0.48f + t * 0.04f), z + len * 0.003f),
                    new Vector3(asymBias, hgt * (0.52f + t * 0.04f), z + len * 0.007f));
            }
        }

        if (isDestroyer)
        {
            for (int e = 0; e < 3; e++)
            {
                float z = -len * (0.08f + e * 0.04f);
                float halfW = hw * MathHelper.Lerp(0.14f, 0.08f, e / 2f);
                TriMat(w, engineGlow,
                    new Vector3(-halfW + asymBias, hgt * 0.05f, z), new Vector3(halfW + asymBias, hgt * 0.05f, z),
                    new Vector3(asymBias, hgt * 0.12f, z + len * 0.006f));
            }
        }
        else
        {
            TriMat(w, engineGlow,
                new Vector3(-hw * 0.10f + asymBias, hgt * 0.04f, -len * 0.10f), new Vector3(hw * 0.10f + asymBias, hgt * 0.04f, -len * 0.10f),
                new Vector3(asymBias, hgt * 0.10f, -len * 0.08f));
        }
    }

    /// <summary>Final-pass magenta accent bands � TriScorerAccent after relight, excluded from weapon snap.</summary>
    private static void AppendSpinyMediumCombatScorerAccentBands(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        float asymBias = 0.04f;

        switch (hullKey)
        {
            case "corvette":
            case "corvette_fast":
                for (int side = -1; side <= 1; side += 2)
                {
                    float xBelt = side * hw * 0.30f + asymBias * side;
                    for (int b = 0; b < 4; b++)
                    {
                        float z = len * (0.02f + b * 0.06f);
                        w.TriScorerAccent(
                            new Vector3(xBelt, hgt * 0.18f, z),
                            new Vector3(xBelt - side * hw * 0.03f, hgt * 0.22f, z + len * 0.006f),
                            new Vector3(xBelt, hgt * 0.14f, z + len * 0.008f));
                    }
                }
                for (int d = 0; d < 6; d++)
                {
                    float z = len * (0.02f + d * 0.05f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.05f + asymBias, hgt * 0.48f, z), new Vector3(hw * 0.05f + asymBias, hgt * 0.48f, z),
                        new Vector3(asymBias, hgt * 0.54f, z + len * 0.005f));
                }
                w.TriScorerAccent(
                    new Vector3(-hw * 0.08f + asymBias, hgt * 0.06f, -len * 0.08f), new Vector3(hw * 0.08f + asymBias, hgt * 0.06f, -len * 0.08f),
                    new Vector3(asymBias, hgt * 0.12f, -len * 0.06f));
                break;

            case "frigate":
            case "frigate_strike":
                for (int d = 0; d < 7; d++)
                {
                    float z = len * (0.04f + d * 0.05f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.06f + asymBias, hgt * 0.48f, z), new Vector3(hw * 0.06f + asymBias, hgt * 0.48f, z),
                        new Vector3(asymBias, hgt * 0.54f, z + len * 0.005f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float xPod = side * hw * 0.34f + asymBias * side;
                    for (int p = 0; p < 4; p++)
                    {
                        float z = len * (0.06f + p * 0.06f);
                        w.TriScorerAccent(
                            new Vector3(xPod, hgt * 0.16f, z),
                            new Vector3(xPod - side * hw * 0.03f, hgt * 0.20f, z + len * 0.006f),
                            new Vector3(xPod, hgt * 0.12f, z + len * 0.008f));
                    }
                }
                w.TriScorerAccent(
                    new Vector3(-hw * 0.06f + asymBias, hgt * 0.08f, len * 0.24f), new Vector3(hw * 0.06f + asymBias, hgt * 0.08f, len * 0.24f),
                    new Vector3(asymBias, hgt * 0.14f, len * 0.28f));
                break;

            case "bomber":
            case "bomber_heavy":
                for (int s = 0; s < 6; s++)
                {
                    float t = s / 5f;
                    float z = MathHelper.Lerp(len * 0.02f, len * 0.26f, t);
                    float halfW = hw * MathHelper.Lerp(0.16f, 0.05f, t);
                    float y = hgt * (0.04f + t * 0.04f);
                    w.TriScorerAccent(
                        new Vector3(-halfW + asymBias, y, z), new Vector3(halfW + asymBias, y, z),
                        new Vector3(asymBias, y + hgt * 0.05f, z + len * 0.006f));
                }
                for (int r = 0; r < 3; r++)
                {
                    float z = len * (0.08f + r * 0.06f);
                    float halfW = hw * MathHelper.Lerp(0.12f, 0.07f, r / 2f);
                    w.TriScorerAccent(
                        new Vector3(-halfW + asymBias, hgt * 0.08f, z), new Vector3(halfW + asymBias, hgt * 0.08f, z),
                        new Vector3(asymBias, hgt * 0.14f, z + len * 0.006f));
                }
                for (int d = 0; d < 5; d++)
                {
                    float t = d / 4f;
                    float z = MathHelper.Lerp(-len * 0.02f, len * 0.22f, t);
                    float xSpan = hw * MathHelper.Lerp(0.10f, 0.04f, t);
                    float yBase = hgt * (0.44f + t * 0.06f);
                    w.TriScorerAccent(
                        new Vector3(-xSpan + asymBias, yBase, z), new Vector3(xSpan + asymBias, yBase, z),
                        new Vector3(asymBias, yBase + hgt * 0.06f, z + len * 0.005f));
                }
                for (int e = 0; e < 3; e++)
                {
                    float z = -len * (0.08f + e * 0.03f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.10f + asymBias, hgt * 0.06f, z), new Vector3(hw * 0.10f + asymBias, hgt * 0.06f, z),
                        new Vector3(asymBias, hgt * 0.12f, z + len * 0.005f));
                }
                break;

            case "gunship":
            case "gunship_heavy":
                for (int c = 0; c < 6; c++)
                {
                    float z = len * (0.06f + c * 0.05f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.14f + asymBias, hgt * 0.03f, z), new Vector3(hw * 0.14f + asymBias, hgt * 0.03f, z),
                        new Vector3(asymBias, hgt * 0.09f, z + len * 0.008f));
                }
                for (int d = 0; d < 6; d++)
                {
                    float t = d / 5f;
                    float z = MathHelper.Lerp(len * 0.02f, len * 0.20f, t);
                    float xSpan = hw * MathHelper.Lerp(0.08f, 0.04f, t);
                    float yBase = hgt * (0.44f + t * 0.04f);
                    w.TriScorerAccent(
                        new Vector3(-xSpan + asymBias, yBase, z), new Vector3(xSpan + asymBias, yBase, z),
                        new Vector3(asymBias, yBase + hgt * 0.06f, z + len * 0.005f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float xPod = side * hw * 0.34f + asymBias * side;
                    for (int p = 0; p < 3; p++)
                    {
                        float z = len * (0.04f + p * 0.06f);
                        w.TriScorerAccent(
                            new Vector3(xPod, hgt * 0.10f, z),
                            new Vector3(xPod - side * hw * 0.03f, hgt * 0.14f, z + len * 0.006f),
                            new Vector3(xPod, hgt * 0.06f, z + len * 0.008f));
                    }
                }
                w.TriScorerAccent(
                    new Vector3(-hw * 0.08f + asymBias, hgt * 0.06f, -len * 0.08f), new Vector3(hw * 0.08f + asymBias, hgt * 0.06f, -len * 0.08f),
                    new Vector3(asymBias, hgt * 0.12f, -len * 0.06f));
                break;

            case "destroyer":
            case "destroyer_assault":
                for (int v = 0; v < 10; v++)
                {
                    float t = v / 9f;
                    float z = MathHelper.Lerp(-len * 0.04f, len * 0.30f, t);
                    float xSpan = hw * MathHelper.Lerp(0.10f, 0.04f, t);
                    float yBase = hgt * (0.50f + t * 0.08f);
                    w.TriScorerAccent(
                        new Vector3(-xSpan + asymBias, yBase, z), new Vector3(xSpan + asymBias, yBase, z),
                        new Vector3(asymBias, yBase + hgt * 0.06f, z + len * 0.005f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float xOut = side * hw * 0.34f + asymBias * side;
                    for (int r = 0; r < 4; r++)
                    {
                        float z = len * (0.06f + r * 0.07f);
                        w.TriScorerAccent(
                            new Vector3(xOut, hgt * 0.18f, z),
                            new Vector3(xOut - side * hw * 0.03f, hgt * 0.22f, z + len * 0.008f),
                            new Vector3(xOut, hgt * 0.14f, z + len * 0.010f));
                    }
                }
                for (int e = 0; e < 3; e++)
                {
                    float z = -len * (0.06f + e * 0.03f);
                    float halfW = hw * MathHelper.Lerp(0.12f, 0.06f, e / 2f);
                    w.TriScorerAccent(
                        new Vector3(-halfW + asymBias, hgt * 0.08f, z), new Vector3(halfW + asymBias, hgt * 0.08f, z),
                        new Vector3(asymBias, hgt * 0.14f, z + len * 0.005f));
                }
                break;
        }
    }

    /// <summary>Final-pass magenta deck-spine / carapace seam accent bands for voidborn capital hulls � post-relight.</summary>
    private static void AppendSpinyCapitalScorerAccentBands(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        float asymBias = 0.04f;

        switch (hullKey)
        {
            case "cruiser":
            case "cruiser_heavy":
                for (int d = 0; d < 7; d++)
                {
                    float t = d / 6f;
                    float z = MathHelper.Lerp(len * 0.02f, len * 0.26f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.08f + asymBias, hgt * (0.50f + t * 0.06f), z),
                        new Vector3(hw * 0.08f + asymBias, hgt * (0.50f + t * 0.06f), z),
                        new Vector3(asymBias, hgt * (0.58f + t * 0.06f), z + len * 0.005f));
                }
                for (int p = 0; p < 4; p++)
                {
                    float z = len * (0.52f + p * 0.05f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.05f + asymBias, hgt * 0.30f, z),
                        new Vector3(hw * 0.05f + asymBias, hgt * 0.30f, z),
                        new Vector3(asymBias, hgt * 0.36f, z + len * 0.006f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float xOut = side * hw * 0.40f + asymBias * side;
                    for (int r = 0; r < 5; r++)
                    {
                        float z = len * (0.04f + r * 0.06f);
                        w.TriScorerAccent(
                            new Vector3(xOut, hgt * 0.16f, z),
                            new Vector3(xOut - side * hw * 0.03f, hgt * 0.20f, z + len * 0.008f),
                            new Vector3(xOut, hgt * 0.12f, z + len * 0.010f));
                    }
                }
                for (int e = 0; e < 3; e++)
                {
                    float z = -len * (0.10f + e * 0.03f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.10f + asymBias, hgt * 0.06f, z),
                        new Vector3(hw * 0.10f + asymBias, hgt * 0.06f, z),
                        new Vector3(asymBias, hgt * 0.12f, z + len * 0.005f));
                }
                break;

            case "carrier":
            case "carrier_command":
                for (int d = 0; d < 8; d++)
                {
                    float t = d / 7f;
                    float z = MathHelper.Lerp(-len * 0.10f, len * 0.24f, t);
                    float xSpan = hw * MathHelper.Lerp(0.38f, 0.10f, t);
                    float yBase = hgt * (0.48f + t * 0.06f);
                    w.TriScorerAccent(
                        new Vector3(-xSpan + asymBias, yBase, z),
                        new Vector3(xSpan + asymBias, yBase, z),
                        new Vector3(asymBias, yBase + hgt * 0.06f, z + len * 0.005f));
                }
                for (int h = 0; h < 7; h++)
                {
                    float z = len * (-0.08f + h * 0.04f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.24f + asymBias, hgt * 0.10f, z),
                        new Vector3(hw * 0.24f + asymBias, hgt * 0.10f, z),
                        new Vector3(asymBias, hgt * 0.14f, z + len * 0.006f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float xOut = side * hw * 0.44f + asymBias * side;
                    for (int r = 0; r < 5; r++)
                    {
                        float z = len * (0.02f + r * 0.07f);
                        w.TriScorerAccent(
                            new Vector3(xOut, hgt * 0.20f, z),
                            new Vector3(xOut - side * hw * 0.03f, hgt * 0.24f, z + len * 0.008f),
                            new Vector3(xOut, hgt * 0.16f, z + len * 0.010f));
                    }
                }
                for (int ring = 0; ring < 4; ring++)
                {
                    float z = len * (-0.04f + ring * 0.06f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.18f + asymBias, hgt * 0.46f, z),
                        new Vector3(hw * 0.18f + asymBias, hgt * 0.46f, z),
                        new Vector3(asymBias, hgt * 0.52f, z + len * 0.006f));
                }
                break;

            case "dreadnought":
                for (int d = 0; d < 6; d++)
                {
                    float z = len * (0.32f + d * 0.06f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.06f + asymBias, hgt * 0.34f, z),
                        new Vector3(hw * 0.06f + asymBias, hgt * 0.34f, z),
                        new Vector3(asymBias, hgt * 0.40f, z + len * 0.005f));
                }
                for (int p = 0; p < 4; p++)
                {
                    float z = len * (0.48f + p * 0.05f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.05f + asymBias, hgt * 0.28f, z),
                        new Vector3(hw * 0.05f + asymBias, hgt * 0.28f, z),
                        new Vector3(asymBias, hgt * 0.34f, z + len * 0.006f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float xTip = side * hw * 0.30f + asymBias * side;
                    for (int r = 0; r < 3; r++)
                    {
                        float z = len * (0.50f + r * 0.05f);
                        w.TriScorerAccent(
                            new Vector3(xTip, hgt * 0.28f, z),
                            new Vector3(xTip - side * hw * 0.03f, hgt * 0.32f, z + len * 0.006f),
                            new Vector3(xTip, hgt * 0.24f, z + len * 0.008f));
                    }
                }
                for (int prow = 0; prow < 4; prow++)
                {
                    float z = len * (0.54f + prow * 0.04f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.08f + asymBias, hgt * 0.44f, z),
                        new Vector3(hw * 0.08f + asymBias, hgt * 0.44f, z),
                        new Vector3(asymBias, hgt * 0.50f, z + len * 0.006f));
                }
                break;
        }
    }

    /// <summary>Final-pass magenta dorsal spine / leading-edge accent bands for voidborn compact + hero craft.</summary>
    private static void AppendSpinyCompactCraftScorerAccentBands(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        float asymBias = 0.04f;
        int extraDorsal = hullKey is "drone" or "drone_swarm" ? 6 : hullKey is "hero" or "hero_default" ? 8 : 7;
        for (int d = 0; d < extraDorsal; d++)
        {
            float t = d / MathF.Max(1f, extraDorsal - 1);
            float z = MathHelper.Lerp(len * 0.04f, len * (hullKey is "drone" or "drone_swarm" ? 0.20f : 0.26f), t);
            w.TriScorerAccent(
                new Vector3(-hw * 0.06f + asymBias, hgt * (0.46f + t * 0.08f), z),
                new Vector3(hw * 0.06f + asymBias, hgt * (0.46f + t * 0.08f), z),
                new Vector3(asymBias, hgt * (0.58f + t * 0.08f), z + len * 0.005f));
            if (d % 2 == 0)
                w.TriScorerAccent(
                    new Vector3(-hw * 0.04f + asymBias, hgt * (0.50f + t * 0.06f), z + len * 0.003f),
                    new Vector3(hw * 0.04f + asymBias, hgt * (0.50f + t * 0.06f), z + len * 0.003f),
                    new Vector3(asymBias, hgt * (0.54f + t * 0.06f), z + len * 0.007f));
        }
        for (int side = -1; side <= 1; side += 2)
        {
            float xLead = side * hw * (hullKey is "scout" or "scout_light" ? 0.92f : 0.88f) + asymBias * side;
            for (int e = 0; e < 2; e++)
            {
                float z = len * (0.05f + e * 0.04f);
                w.TriScorerAccent(
                    new Vector3(xLead, hgt * (0.20f - e * 0.02f), z),
                    new Vector3(xLead - side * hw * 0.03f, hgt * (0.18f - e * 0.02f), z + len * 0.02f),
                    new Vector3(xLead, hgt * (0.16f - e * 0.02f), z + len * 0.03f));
            }
        }
    }

    public static void AppendSpinyScorerAccentBands(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        if (hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk" or "support_repair")
        {
            AppendSpinyUtilityScorerAccentBands(w, hullKey, len, wid, hgt);
            return;
        }

        if (hullKey is "cruiser" or "cruiser_heavy" or "carrier" or "carrier_command" or "dreadnought")
        {
            AppendSpinyCapitalScorerAccentBands(w, hullKey, len, wid, hgt);
            return;
        }

        if (hullKey is "scout" or "scout_light" or "fighter" or "fighter_basic"
            or "interceptor" or "interceptor_mk2" or "drone" or "drone_swarm"
            or "hero" or "hero_default")
        {
            AppendSpinyCompactCraftScorerAccentBands(w, hullKey, len, wid, hgt);
            return;
        }

        if (hullKey is "corvette" or "corvette_fast" or "frigate" or "frigate_strike"
            or "gunship" or "gunship_heavy" or "bomber" or "bomber_heavy"
            or "destroyer" or "destroyer_assault")
        {
            AppendSpinyMediumCombatScorerAccentBands(w, hullKey, len, wid, hgt);
            return;
        }
    }

    /// <summary>Post-relight magenta carapace seam strips + tool-arm/cargo spine accent veins for voidborn utility hulls.</summary>
    private static void AppendSpinyUtilityScorerAccentBands(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        float widthScale = hullKey is "freighter_bulk" ? 0.88f : hullKey is "miner_eva" ? 0.90f : 0.94f;
        hw *= widthScale;
        float asymBias = 0.05f;

        switch (hullKey)
        {
            case "miner_basic":
            case "miner_eva":
            case "miner_tractor":
                for (int s = 0; s < 5; s++)
                {
                    float t = s / 4f;
                    float z = MathHelper.Lerp(len * 0.02f, len * (hullKey is "miner_basic" ? 0.32f : 0.26f), t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.05f + asymBias, hgt * (0.48f + t * 0.06f), z),
                        new Vector3(hw * 0.05f + asymBias, hgt * (0.48f + t * 0.06f), z),
                        new Vector3(asymBias, hgt * (0.56f + t * 0.06f), z + len * 0.005f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float xTip = side * hw * (hullKey is "miner_basic" ? 0.70f : 0.62f);
                    float zTip = len * (hullKey is "miner_basic" ? 0.55f : hullKey is "miner_tractor" ? 0.18f : 0.14f);
                    for (int e = 0; e < 3; e++)
                    {
                        float z = zTip - len * (e * 0.04f);
                        w.TriScorerAccent(
                            new Vector3(xTip, hgt * (0.20f + e * 0.02f), z),
                            new Vector3(xTip + side * hw * 0.04f, hgt * (0.26f + e * 0.02f), z + len * 0.02f),
                            new Vector3(xTip, hgt * (0.14f + e * 0.02f), z + len * 0.01f));
                    }
                }
                if (hullKey is "miner_tractor")
                {
                    for (int d = 0; d < 3; d++)
                    {
                        float z = len * (0.34f + d * 0.04f);
                        w.TriScorerAccent(
                            new Vector3(-hw * 0.06f + asymBias, hgt * 0.64f, z),
                            new Vector3(hw * 0.06f + asymBias, hgt * 0.64f, z),
                            new Vector3(asymBias, hgt * 0.72f, z + len * 0.006f));
                    }
                }
                break;

            case "transport_cargo":
                for (int s = 0; s < 7; s++)
                {
                    float t = s / 6f;
                    float z = MathHelper.Lerp(len * 0.04f, len * 0.40f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.06f + asymBias, hgt * (0.44f + t * 0.04f), z),
                        new Vector3(hw * 0.06f + asymBias, hgt * (0.44f + t * 0.04f), z),
                        new Vector3(asymBias, hgt * (0.52f + t * 0.04f), z + len * 0.006f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float cx = side * hw * 0.38f + asymBias * side;
                    w.TriScorerAccent(
                        new Vector3(cx - hw * 0.05f, hgt * 0.22f, len * 0.30f),
                        new Vector3(cx + hw * 0.05f, hgt * 0.22f, len * 0.30f),
                        new Vector3(cx, hgt * 0.28f, len * 0.34f));
                    w.TriScorerAccent(
                        new Vector3(cx, hgt * 0.18f, len * 0.24f),
                        new Vector3(cx - side * hw * 0.04f, hgt * 0.22f, len * 0.28f),
                        new Vector3(cx, hgt * 0.14f, len * 0.30f));
                }
                break;

            case "freighter_bulk":
                for (int s = 0; s < 6; s++)
                {
                    float t = s / 5f;
                    float z = MathHelper.Lerp(len * 0.06f, len * 0.38f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.08f + asymBias, hgt * (0.36f + t * 0.04f), z),
                        new Vector3(hw * 0.08f + asymBias, hgt * (0.36f + t * 0.04f), z),
                        new Vector3(asymBias, hgt * (0.44f + t * 0.04f), z + len * 0.006f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float cx = side * hw * 0.32f + asymBias * side;
                    w.TriScorerAccent(
                        new Vector3(cx - hw * 0.05f, hgt * 0.24f, len * 0.30f),
                        new Vector3(cx + hw * 0.05f, hgt * 0.24f, len * 0.30f),
                        new Vector3(cx, hgt * 0.30f, len * 0.34f));
                }
                w.TriScorerAccent(
                    new Vector3(-hw * 0.05f + asymBias, hgt * 0.40f, len * 0.34f),
                    new Vector3(hw * 0.05f + asymBias, hgt * 0.40f, len * 0.34f),
                    new Vector3(asymBias, hgt * 0.46f, len * 0.38f));
                break;

            case "support_repair":
                for (int a = 0; a < 6; a++)
                {
                    float t = a / 5f;
                    float z = len * (0.04f + t * 0.24f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.05f + asymBias, hgt * (0.68f + t * 0.08f), z),
                        new Vector3(hw * 0.05f + asymBias, hgt * (0.68f + t * 0.08f), z),
                        new Vector3(asymBias, hgt * (0.76f + t * 0.08f), z + len * 0.006f));
                    if (a % 2 == 0)
                        w.TriScorerAccent(
                            new Vector3(-hw * 0.04f + asymBias, hgt * (0.72f + t * 0.06f), z + len * 0.003f),
                            new Vector3(hw * 0.04f + asymBias, hgt * (0.72f + t * 0.06f), z + len * 0.003f),
                            new Vector3(asymBias, hgt * (0.78f + t * 0.06f), z + len * 0.007f));
                }
                w.TriScorerAccent(
                    new Vector3(-hw * 0.03f + asymBias, hgt * 0.96f, len * 0.14f),
                    new Vector3(hw * 0.03f + asymBias, hgt * 0.96f, len * 0.14f),
                    new Vector3(asymBias, hgt * 1.04f, len * 0.16f));
                for (int side = -1; side <= 1; side += 2)
                {
                    float xBoom = side * hw * 0.82f;
                    for (int e = 0; e < 2; e++)
                    {
                        float z = len * (0.08f + e * 0.06f);
                        w.TriScorerAccent(
                            new Vector3(xBoom, hgt * (0.48f + e * 0.04f), z),
                            new Vector3(xBoom + side * hw * 0.04f, hgt * (0.54f + e * 0.04f), z + len * 0.02f),
                            new Vector3(xBoom, hgt * (0.42f + e * 0.04f), z + len * 0.03f));
                    }
                }
                break;
        }
    }

    private static void ApplyRadiantShipDetail(
        RaceMeshWriter w, RaceVisualDefinition race, string hullKey, float len, float wid, float hgt)
    {
        if (hullKey is "cruiser" or "cruiser_heavy" or "carrier" or "carrier_command" or "dreadnought")
        {
            ApplyRadiantCapitalShipDetail(w, hullKey, len, wid, hgt);
            return;
        }

        if (hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk" or "support_repair")
        {
            ApplyRadiantUtilityShipDetail(w, hullKey, len, wid, hgt);
            return;
        }

        if (hullKey is "corvette" or "corvette_fast" or "frigate" or "frigate_strike"
            or "gunship" or "gunship_heavy" or "bomber" or "bomber_heavy"
            or "destroyer" or "destroyer_assault")
        {
            ApplyRadiantMediumCombatShipDetail(w, hullKey, len, wid, hgt);
            return;
        }

        bool isScout = hullKey is "scout" or "scout_light";
        bool isFighter = hullKey is "fighter" or "fighter_basic";
        bool isInterceptor = hullKey is "interceptor" or "interceptor_mk2";
        bool isDrone = hullKey is "drone" or "drone_swarm";
        bool isHero = hullKey is "hero" or "hero_default";
        bool isCompactCraft = isScout || isFighter || isInterceptor || isDrone;
        bool isReferenceCraft = isFighter || isHero;
        if (!isCompactCraft && !isHero)
            return;

        var solar = RaceMeshWriter.HullMaterial.Solar;
        float hw = wid * 0.5f;

        int crownRows = isScout ? 2 : isDrone ? 2 : isInterceptor ? 2 : isHero ? 2 : 2;
        int crownCols = isScout ? 2 : isDrone ? 2 : isFighter ? 3 : isHero ? 3 : 2;
        float crownZ0 = isScout ? -0.32f : isDrone ? -0.30f : -0.36f;
        float crownZ1 = isScout ? 0.34f : isDrone ? 0.28f : isInterceptor ? 0.34f : isHero ? 0.36f : 0.30f;
        AddRadiantSolarCrownSubstrate(w, hw, hgt, len, crownRows, crownCols, crownZ0, crownZ1);

        int flankSegs = isScout ? 3 : isDrone ? 3 : isInterceptor ? 3 : 4;
        for (int side = -1; side <= 1; side += 2)
        {
            float xLead = side * hw * (isScout ? 0.40f : isDrone ? 0.22f : isFighter ? 0.56f : 0.38f);
            for (int e = 0; e < flankSegs; e++)
            {
                float z = MathHelper.Lerp(len * (isScout ? 0.10f : 0.08f), -len * 0.02f, e / MathF.Max(1f, flankSegs - 1));
                TriMat(w, solar,
                    new Vector3(xLead, hgt * (0.20f + e * 0.012f), z),
                    new Vector3(xLead - side * hw * 0.03f, hgt * (0.18f + e * 0.012f), z + len * 0.008f),
                    new Vector3(xLead, hgt * (0.16f + e * 0.012f), z + len * 0.012f));
            }
        }

        if (isHero)
        {
            for (int p = 0; p < 4; p++)
            {
                float t = p / 3f;
                float z = MathHelper.Lerp(-len * 0.06f, len * 0.22f, t);
                float xSpan = hw * MathHelper.Lerp(0.32f, 0.10f, t);
                TriMat(w, solar,
                    new Vector3(-xSpan, hgt * (0.62f + t * 0.06f), z), new Vector3(xSpan, hgt * (0.62f + t * 0.06f), z),
                    new Vector3(0, hgt * (0.70f + t * 0.06f), z + len * 0.006f));
            }
        }

        if (isReferenceCraft)
            AddRadiantReferenceCraftSubstrate(w, hw, hgt, len, isHero);
        else
            AddRadiantCompactCraftSubstrate(w, hullKey, hw, hgt, len);

        AddRadiantGameplayComponentBands(w, hullKey, hw, hgt, len);
    }

    private static void ApplyRadiantUtilityShipDetail(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        var solar = RaceMeshWriter.HullMaterial.Solar;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var emboss = RaceMeshWriter.HullMaterial.Truss;
        var engineMat = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float hw = wid * 0.5f;
        float widthScale = hullKey is "freighter_bulk" ? 0.90f : hullKey is "miner_eva" ? 0.92f : 0.96f;
        hw *= widthScale;
        bool triBudgetTight = hullKey is "freighter_bulk" or "transport_cargo";
        bool isMiner = hullKey is "miner_basic" or "miner_eva" or "miner_tractor";
        bool isSupport = hullKey is "support_repair";
        int veinSegs = hullKey is "freighter_bulk" ? 2 : triBudgetTight ? 3 : isMiner ? 4 : 4;

        for (int side = -1; side <= 1; side += 2)
        {
            float xOut = side * hw * (hullKey is "freighter_bulk" ? 0.48f : hullKey is "transport_cargo" ? 0.46f : 0.56f);
            for (int i = 0; i < veinSegs; i++)
            {
                float t = i / MathF.Max(1f, veinSegs - 1);
                float z = MathHelper.Lerp(-len * 0.04f, len * (hullKey is "freighter_bulk" ? 0.34f : triBudgetTight ? 0.32f : isMiner ? 0.20f : isSupport ? 0.16f : 0.14f), t);
                TriMat(w, solar,
                    new Vector3(xOut, hgt * (0.16f + t * 0.05f), z),
                    new Vector3(xOut - side * hw * 0.035f, hgt * (0.13f + t * 0.05f), z + len * 0.010f),
                    new Vector3(xOut, hgt * (0.11f + t * 0.04f), z + len * 0.014f));
            }
        }

        if (hullKey is "miner_basic")
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xTip = side * hw * 0.72f;
                TriMat(w, weapon,
                    new Vector3(xTip, hgt * 0.20f, len * 0.46f),
                    new Vector3(xTip + side * hw * 0.04f, hgt * 0.26f, len * 0.52f),
                    new Vector3(xTip, hgt * 0.14f, len * 0.54f));
                TriMat(w, engineMat,
                    new Vector3(xTip - side * hw * 0.03f, hgt * 0.06f, len * 0.10f),
                    new Vector3(xTip + side * hw * 0.03f, hgt * 0.06f, len * 0.10f),
                    new Vector3(xTip, hgt * 0.12f, len * 0.14f));
            }
            TriMat(w, engineMat,
                new Vector3(-hw * 0.20f, hgt * 0.04f, -len * 0.08f),
                new Vector3(hw * 0.20f, hgt * 0.04f, -len * 0.08f),
                new Vector3(0, hgt * 0.10f, -len * 0.04f));
        }

        if (hullKey is "support_repair")
        {
            for (int a = 0; a < 3; a++)
            {
                float z = len * (0.06f + a * 0.050f);
                TriMat(w, solar,
                    new Vector3(-hw * 0.04f, hgt * (0.76f + a * 0.04f), z),
                    new Vector3(hw * 0.04f, hgt * (0.76f + a * 0.04f), z),
                    new Vector3(0, hgt * (0.84f + a * 0.04f), z + len * 0.008f));
            }
            for (int s = 0; s < 2; s++)
            {
                float z = len * (0.10f + s * 0.06f);
                TriMat(w, solar,
                    new Vector3(-hw * 0.05f, hgt * 0.68f, z),
                    new Vector3(hw * 0.05f, hgt * 0.68f, z),
                    new Vector3(0, hgt * 0.74f, z + len * 0.008f));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float xBoom = side * hw * 0.66f;
                TriMat(w, weapon,
                    new Vector3(xBoom, hgt * 0.56f, len * 0.12f),
                    new Vector3(xBoom + side * hw * 0.05f, hgt * 0.62f, len * 0.14f),
                    new Vector3(xBoom, hgt * 0.50f, len * 0.16f));
            }
        }

        if (hullKey is "transport_cargo")
        {
            for (int s = 0; s < 4; s++)
            {
                float z = len * (0.08f + s * 0.10f);
                TriMat(w, emboss,
                    new Vector3(-hw * 0.06f, hgt * 0.42f, z),
                    new Vector3(hw * 0.06f, hgt * 0.42f, z),
                    new Vector3(0, hgt * 0.48f, z + len * 0.010f));
                if (s % 2 == 0)
                    TriMat(w, solar,
                        new Vector3(-hw * 0.04f, hgt * 0.50f, z + len * 0.006f),
                        new Vector3(hw * 0.04f, hgt * 0.50f, z + len * 0.006f),
                        new Vector3(0, hgt * 0.56f, z + len * 0.012f));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float cx = side * hw * 0.44f;
                TriMat(w, weapon,
                    new Vector3(cx - hw * 0.06f, hgt * 0.24f, len * 0.28f),
                    new Vector3(cx + hw * 0.06f, hgt * 0.24f, len * 0.28f),
                    new Vector3(cx, hgt * 0.30f, len * 0.32f));
            }
            TriMat(w, engineMat,
                new Vector3(-hw * 0.10f, hgt * 0.04f, -len * 0.10f),
                new Vector3(hw * 0.10f, hgt * 0.04f, -len * 0.10f),
                new Vector3(0, hgt * 0.10f, -len * 0.06f));
        }

        if (hullKey is "freighter_bulk")
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float cx = side * hw * 0.38f;
                TriMat(w, panel,
                    new Vector3(cx - hw * 0.06f, hgt * 0.18f, len * 0.30f),
                    new Vector3(cx + hw * 0.06f, hgt * 0.18f, len * 0.30f),
                    new Vector3(cx, hgt * 0.24f, len * 0.36f));
                TriMat(w, solar,
                    new Vector3(cx - hw * 0.04f, hgt * 0.28f, len * 0.34f),
                    new Vector3(cx + hw * 0.04f, hgt * 0.28f, len * 0.34f),
                    new Vector3(cx, hgt * 0.34f, len * 0.40f));
            }
            TriMat(w, solar,
                new Vector3(-hw * 0.06f, hgt * 0.36f, len * 0.38f),
                new Vector3(hw * 0.06f, hgt * 0.36f, len * 0.38f),
                new Vector3(0, hgt * 0.42f, len * 0.44f));
            TriMat(w, panel,
                new Vector3(-hw * 0.05f, hgt * 0.44f, len * 0.20f),
                new Vector3(hw * 0.05f, hgt * 0.44f, len * 0.20f),
                new Vector3(0, hgt * 0.50f, len * 0.26f));
        }

        if (hullKey is "miner_tractor")
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xTip = side * hw * 0.62f;
                TriMat(w, solar,
                    new Vector3(xTip, hgt * 0.34f, len * 0.14f),
                    new Vector3(xTip + side * hw * 0.04f, hgt * 0.40f, len * 0.18f),
                    new Vector3(xTip, hgt * 0.28f, len * 0.20f));
            }
            for (int g = 0; g < 2; g++)
            {
                float z = len * (0.30f + g * 0.06f);
                TriMat(w, solar,
                    new Vector3(-hw * 0.05f, hgt * (0.52f + g * 0.04f), z),
                    new Vector3(hw * 0.05f, hgt * (0.52f + g * 0.04f), z),
                    new Vector3(0, hgt * (0.60f + g * 0.04f), z + len * 0.008f));
            }
            TriMat(w, solar,
                new Vector3(-hw * 0.05f, hgt * 0.54f, len * 0.34f),
                new Vector3(hw * 0.05f, hgt * 0.54f, len * 0.34f),
                new Vector3(0, hgt * 0.62f, len * 0.40f));
            TriMat(w, panel,
                new Vector3(-hw * 0.04f, hgt * 0.50f, len * 0.32f),
                new Vector3(hw * 0.04f, hgt * 0.50f, len * 0.32f),
                new Vector3(0, hgt * 0.56f, len * 0.38f));
        }

        if (hullKey is "miner_basic" or "miner_eva")
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xTip = side * hw * (hullKey is "miner_basic" ? 0.66f : 0.48f);
                TriMat(w, solar,
                    new Vector3(xTip, hgt * (hullKey is "miner_basic" ? 0.22f : 0.36f), len * 0.12f),
                    new Vector3(xTip + side * hw * 0.03f, hgt * (hullKey is "miner_basic" ? 0.28f : 0.42f), len * 0.16f),
                    new Vector3(xTip, hgt * (hullKey is "miner_basic" ? 0.16f : 0.30f), len * 0.18f));
            }
        }

        if (hullKey is "transport_cargo")
        {
            TriMat(w, solar,
                new Vector3(-hw * 0.05f, hgt * 0.46f, len * 0.24f),
                new Vector3(hw * 0.05f, hgt * 0.46f, len * 0.24f),
                new Vector3(0, hgt * 0.52f, len * 0.28f));
        }

        AddRadiantUtilitySubstrate(w, hullKey, hw, hgt, len);
        AddTrussUtilityGameplayComponentBands(w, hullKey, hw, hgt, len);

        if (!triBudgetTight)
        {
            TriMat(w, engineMat,
                new Vector3(-hw * 0.12f, 0, -len * 0.08f), new Vector3(hw * 0.12f, 0, -len * 0.08f),
                new Vector3(0, hgt * 0.06f, -len * 0.12f));
        }
    }

    /// <summary>Belly/flank solar membrane substrate bands � amber panel veins under team tint.</summary>
    private static void AddRadiantUtilitySubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var membrane = RaceMeshWriter.HullMaterial.Hull;
        var vein = RaceMeshWriter.HullMaterial.Solar;
        var shadow = RaceMeshWriter.HullMaterial.Radiator;
        bool isCargo = hullKey is "freighter_bulk" or "transport_cargo";
        bool isMiner = hullKey is "miner_basic" or "miner_eva" or "miner_tractor";
        bool isSupport = hullKey is "support_repair";
        int bellyPlates = hullKey is "freighter_bulk" ? 3 : isCargo ? 4 : isSupport ? 3 : isMiner ? 4 : 3;

        for (int i = 0; i < bellyPlates; i++)
        {
            float t = i / MathF.Max(1f, bellyPlates - 1);
            float z = MathHelper.Lerp(-len * 0.06f, len * (hullKey switch
            {
                "freighter_bulk" => 0.32f,
                "transport_cargo" => 0.28f,
                "support_repair" => 0.20f,
                "miner_tractor" => 0.24f,
                "miner_basic" => 0.22f,
                "miner_eva" => 0.18f,
                _ => 0.16f
            }), t);
            float halfW = hw * MathHelper.Lerp(
                hullKey is "freighter_bulk" ? 0.30f : isCargo ? 0.22f : 0.24f,
                hullKey is "freighter_bulk" ? 0.12f : 0.10f, t);
            TriMat(w, shadow,
                new Vector3(-halfW, hgt * 0.02f, z), new Vector3(halfW, hgt * 0.02f, z),
                new Vector3(0, hgt * (0.18f + t * 0.04f), z + len * 0.014f));
            if (hullKey is "freighter_bulk" && i % 2 == 0)
            {
                TriMat(w, vein,
                    new Vector3(-halfW * 0.5f, hgt * (0.12f + t * 0.02f), z + len * 0.010f),
                    new Vector3(halfW * 0.5f, hgt * (0.12f + t * 0.02f), z + len * 0.010f),
                    new Vector3(0, hgt * (0.20f + t * 0.03f), z + len * 0.016f));
            }
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * hw * (hullKey is "freighter_bulk" ? 0.22f : isCargo ? 0.16f : 0.22f);
            int flankSegs = isCargo ? 4 : isSupport ? 3 : isMiner ? 3 : 2;
            for (int s = 0; s < flankSegs; s++)
            {
                float z = MathHelper.Lerp(-len * 0.02f, len * (hullKey switch
                {
                    "freighter_bulk" => 0.24f,
                    "transport_cargo" => 0.22f,
                    "miner_tractor" => 0.20f,
                    "support_repair" => 0.16f,
                    "miner_basic" => 0.18f,
                    "miner_eva" => 0.14f,
                    _ => 0.12f
                }), s / MathF.Max(1f, flankSegs - 1));
                TriMat(w, membrane,
                    new Vector3(x, hgt * 0.14f, z), new Vector3(x - side * hw * 0.03f, hgt * 0.28f, z + len * 0.012f),
                    new Vector3(x, hgt * 0.10f, z + len * 0.014f));
                if (isCargo || hullKey is "miner_tractor")
                {
                    TriMat(w, vein,
                        new Vector3(x, hgt * 0.22f, z + len * 0.006f),
                        new Vector3(x - side * hw * 0.02f, hgt * 0.26f, z + len * 0.010f),
                        new Vector3(x, hgt * 0.18f, z + len * 0.012f));
                }
            }
        }

        int spineSegs = hullKey is "freighter_bulk" ? 2 : isCargo ? 3 : isSupport ? 4 : isMiner ? 4 : 3;
        for (int i = 0; i < spineSegs; i++)
        {
            float t = i / MathF.Max(1f, spineSegs - 1);
            float z = MathHelper.Lerp(-len * 0.04f, len * (hullKey switch
            {
                "freighter_bulk" => 0.42f,
                "transport_cargo" => 0.40f,
                "miner_basic" => 0.34f,
                "miner_eva" => 0.28f,
                "miner_tractor" => 0.34f,
                "support_repair" => 0.28f,
                _ => 0.16f
            }), t);
            float xSpan = hw * MathHelper.Lerp(0.12f, 0.03f, t);
            TriMat(w, shadow,
                new Vector3(-xSpan, hgt * (0.46f + t * 0.06f), z), new Vector3(xSpan, hgt * (0.46f + t * 0.06f), z),
                new Vector3(0, hgt * (0.58f + t * 0.04f), z + len * 0.012f));
            if (isMiner || isSupport || isCargo)
            {
                TriMat(w, vein,
                    new Vector3(-xSpan * 0.7f, hgt * (0.56f + t * 0.04f), z + len * 0.008f),
                    new Vector3(xSpan * 0.7f, hgt * (0.56f + t * 0.04f), z + len * 0.008f),
                    new Vector3(0, hgt * (0.62f + t * 0.03f), z + len * 0.014f));
            }
        }
    }

    /// <summary>Embossed solar crown panel rows � vertex luminance bands for mesh-preview.</summary>
    private static void AddRadiantSolarCrownSubstrate(
        RaceMeshWriter w, float hw, float hgt, float len, int rows, int cols, float z0Factor, float z1Factor)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var emboss = RaceMeshWriter.HullMaterial.Truss;
        var solar = RaceMeshWriter.HullMaterial.Solar;
        float deckH = hgt * 0.30f;
        float span = hw * 2.1f;
        float deckZ0 = len * z0Factor;
        float deckZ1 = len * z1Factor;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                float u = col / (float)Math.Max(1, cols - 1);
                float v = row / (float)Math.Max(1, rows - 1);
                float x = MathHelper.Lerp(-span * 0.40f, span * 0.40f, u);
                float z0 = MathHelper.Lerp(deckZ0, deckZ1 - len * 0.06f, v);
                float z1 = z0 + len * 0.08f;
                float yPeak = deckH * (1.30f + v * 0.40f);
                TriMat(w, solar, new Vector3(x - span * 0.04f, deckH, z0), new Vector3(x + span * 0.04f, deckH, z0), new Vector3(x, yPeak, z1));
                TriMat(w, panel, new Vector3(x - span * 0.04f, deckH, z0), new Vector3(x, yPeak, z1), new Vector3(x - span * 0.03f, deckH, z1));
                TriMat(w, emboss, new Vector3(x + span * 0.04f, deckH, z0), new Vector3(x + span * 0.03f, deckH, z1), new Vector3(x, yPeak, z1));
            }
        }
    }

    private static void AddRadiantCompactCraftSubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var emboss = RaceMeshWriter.HullMaterial.Truss;
        var solar = RaceMeshWriter.HullMaterial.Solar;
        var membrane = RaceMeshWriter.HullMaterial.Hull;
        bool isScout = hullKey is "scout" or "scout_light";
        bool isInterceptor = hullKey is "interceptor" or "interceptor_mk2";
        bool isDrone = hullKey is "drone" or "drone_swarm";

        int bellyPlates = isScout ? 4 : isDrone ? 4 : 4;
        float bellyZStart = isDrone ? -len * 0.08f : -len * 0.06f;
        float bellyZEnd = isScout ? len * 0.28f : isDrone ? len * 0.30f : len * 0.28f;
        float bellyHalfW = isScout ? 0.18f : isDrone ? 0.12f : 0.20f;
        float bellyLift = isDrone ? 0.15f : 0.18f;
        for (int i = 0; i < bellyPlates; i++)
        {
            float t = i / MathF.Max(1f, bellyPlates - 1);
            float z = MathHelper.Lerp(bellyZStart, bellyZEnd, t);
            float halfW = hw * MathHelper.Lerp(bellyHalfW, bellyHalfW * 0.72f, t);
            TriMat(w, panel,
                new Vector3(-halfW, hgt * 0.03f, z), new Vector3(halfW, hgt * 0.03f, z),
                new Vector3(0, hgt * bellyLift, z + len * 0.008f));
            TriMat(w, emboss,
                new Vector3(-halfW * 0.82f, hgt * 0.05f, z - len * 0.004f),
                new Vector3(halfW * 0.82f, hgt * 0.05f, z - len * 0.004f),
                new Vector3(0, hgt * (bellyLift * 0.68f), z));
        }

        int seamCount = isDrone ? 3 : 3;
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * hw * (isScout ? 0.24f : isDrone ? 0.14f : 0.28f);
            for (int s = 0; s < seamCount; s++)
            {
                float z = len * (isDrone ? (-0.02f + s * 0.10f) : (0.02f + s * 0.08f));
                TriMat(w, membrane,
                    new Vector3(x, hgt * 0.24f, z), new Vector3(x - side * hw * 0.03f, hgt * 0.28f, z + len * 0.01f),
                    new Vector3(x, hgt * 0.20f, z + len * 0.012f));
            }
        }

        int keelSegs = isScout ? 5 : isDrone ? 4 : isInterceptor ? 4 : 3;
        float keelZEnd = isScout ? 0.28f : isDrone ? 0.26f : isInterceptor ? 0.30f : 0.22f;
        for (int k = 0; k < keelSegs; k++)
        {
            float t = k / MathF.Max(1f, keelSegs - 1);
            float z = MathHelper.Lerp(len * (isDrone ? -0.04f : 0.02f), len * keelZEnd, t);
            float yBase = hgt * (0.44f + t * (isDrone ? 0.26f : 0.22f));
            float halfKeel = hw * (isDrone ? 0.03f : isScout ? 0.04f : 0.05f);
            bool dorsalAccent = isScout || isInterceptor || (isDrone && k >= 1);
            var keelMat = dorsalAccent ? solar : k % 2 == 0 ? solar : emboss;
            float keelLift = dorsalAccent ? (isInterceptor ? 0.12f : 0.10f) : 0.08f;
            TriMat(w, keelMat,
                new Vector3(-halfKeel, yBase, z), new Vector3(halfKeel, yBase, z),
                new Vector3(0, yBase + hgt * keelLift, z + len * 0.005f));
        }

        if (isScout)
        {
            for (int k = 0; k < 4; k++)
            {
                float t = k / 3f;
                float z = MathHelper.Lerp(len * 0.06f, len * 0.26f, t);
                TriMat(w, solar,
                    new Vector3(-hw * 0.03f, hgt * (0.50f + t * 0.06f), z),
                    new Vector3(hw * 0.03f, hgt * (0.50f + t * 0.06f), z),
                    new Vector3(0, hgt * (0.58f + t * 0.06f), z + len * 0.005f));
            }
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float xLead = side * hw * (isScout ? 0.40f : isDrone ? 0.22f : isInterceptor ? 0.36f : 0.38f);
            int edgeSegs = isScout ? 4 : isDrone ? 4 : isInterceptor ? 4 : 3;
            for (int e = 0; e < edgeSegs; e++)
            {
                float z = MathHelper.Lerp(len * (isScout ? 0.12f : 0.10f), -len * 0.02f, e / MathF.Max(1f, edgeSegs - 1));
                TriMat(w, solar,
                    new Vector3(xLead, hgt * (0.18f + e * 0.012f), z),
                    new Vector3(xLead - side * hw * 0.03f, hgt * (0.16f + e * 0.012f), z + len * 0.008f),
                    new Vector3(xLead, hgt * (0.14f + e * 0.012f), z + len * 0.012f));
            }
        }

        int spineSegs = isScout ? 6 : isDrone ? 6 : isInterceptor ? 5 : 4;
        for (int s = 0; s < spineSegs; s++)
        {
            float t = s / MathF.Max(1f, spineSegs - 1);
            float z = MathHelper.Lerp(len * (isDrone ? -0.04f : 0.02f), len * (isScout ? 0.30f : isDrone ? 0.26f : isInterceptor ? 0.32f : 0.22f), t);
            float halfSpine = hw * (isDrone ? 0.03f : 0.04f);
            TriMat(w, solar,
                new Vector3(-halfSpine, hgt * (0.50f + t * 0.06f), z),
                new Vector3(halfSpine, hgt * (0.50f + t * 0.06f), z),
                new Vector3(0, hgt * (0.56f + t * 0.06f), z + len * 0.005f));
        }

        var engineGlow = RaceMeshWriter.HullMaterial.Engine;
        for (int side = -1; side <= 1; side += 2)
        {
            float xEng = side * hw * (isDrone ? 0.20f : isScout ? 0.08f : 0.10f);
            float zEng = -len * (isDrone ? 0.24f : 0.08f);
            TriMat(w, engineGlow,
                new Vector3(xEng, hgt * 0.08f, zEng),
                new Vector3(xEng - side * hw * 0.02f, hgt * 0.05f, zEng - len * 0.01f),
                new Vector3(xEng, hgt * 0.04f, zEng - len * 0.012f));
        }
    }

    private static void AddRadiantReferenceCraftSubstrate(RaceMeshWriter w, float hw, float hgt, float len, bool isHero)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var emboss = RaceMeshWriter.HullMaterial.Truss;
        var solar = RaceMeshWriter.HullMaterial.Solar;
        var membrane = RaceMeshWriter.HullMaterial.Hull;

        int bellyPlates = isHero ? 5 : 4;
        for (int i = 0; i < bellyPlates; i++)
        {
            float t = i / MathF.Max(1f, bellyPlates - 1);
            float z = MathHelper.Lerp(-len * 0.06f, len * (isHero ? 0.24f : 0.20f), t);
            float halfW = hw * MathHelper.Lerp(0.26f, 0.14f, t);
            TriMat(w, panel,
                new Vector3(-halfW, hgt * 0.03f, z), new Vector3(halfW, hgt * 0.03f, z),
                new Vector3(0, hgt * 0.18f, z + len * 0.008f));
            TriMat(w, emboss,
                new Vector3(-halfW * 0.80f, hgt * 0.05f, z - len * 0.004f),
                new Vector3(halfW * 0.80f, hgt * 0.05f, z - len * 0.004f),
                new Vector3(0, hgt * 0.11f, z));
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * hw * (isHero ? 0.30f : 0.34f);
            for (int s = 0; s < 3; s++)
            {
                float z = len * (0.04f + s * 0.08f);
                TriMat(w, membrane,
                    new Vector3(x, hgt * 0.26f, z), new Vector3(x - side * hw * 0.03f, hgt * 0.30f, z + len * 0.01f),
                    new Vector3(x, hgt * 0.22f, z + len * 0.012f));
            }
        }

        int spineSegs = isHero ? 5 : 5;
        for (int s = 0; s < spineSegs; s++)
        {
            float t = s / MathF.Max(1f, spineSegs - 1);
            float z = MathHelper.Lerp(len * 0.02f, len * (isHero ? 0.30f : 0.22f), t);
            float halfSpine = hw * MathHelper.Lerp(0.05f, 0.025f, t);
            var spineMat = s == spineSegs - 1 ? solar : s % 2 == 0 ? solar : panel;
            TriMat(w, spineMat,
                new Vector3(-halfSpine, hgt * (0.52f + t * 0.06f), z),
                new Vector3(halfSpine, hgt * (0.52f + t * 0.06f), z),
                new Vector3(0, hgt * (0.60f + t * 0.06f), z + len * 0.005f));
        }

        if (isHero)
        {
            for (int p = 0; p < 3; p++)
            {
                float z = len * (0.10f + p * 0.06f);
                TriMat(w, solar,
                    new Vector3(-hw * 0.06f, hgt * 0.64f, z), new Vector3(hw * 0.06f, hgt * 0.64f, z),
                    new Vector3(0, hgt * 0.72f, z + len * 0.006f));
            }
        }
    }

    private static void ApplyRadiantMediumCombatShipDetail(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        var solar = RaceMeshWriter.HullMaterial.Solar;
        var panel = RaceMeshWriter.HullMaterial.Truss;
        var engineGlow = RaceMeshWriter.HullMaterial.Engine;
        float hw = wid * 0.5f;
        bool isCorvette = hullKey is "corvette" or "corvette_fast";
        bool isFrigate = hullKey is "frigate" or "frigate_strike";
        bool isGunship = hullKey is "gunship" or "gunship_heavy";
        bool isBomber = hullKey is "bomber" or "bomber_heavy";
        bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";

        int crestBands = isFrigate ? 6 : isBomber ? 5 : isGunship ? 5 : isCorvette ? 5 : 7;
        float crestReach = isFrigate ? 0.26f : isBomber ? 0.28f : isDestroyer ? 0.32f : isCorvette ? 0.20f : 0.18f;
        for (int p = 0; p < crestBands; p++)
        {
            float t = p / MathF.Max(1f, crestBands - 1);
            float z = MathHelper.Lerp(-len * 0.04f, len * crestReach, t);
            float xSpan = hw * MathHelper.Lerp(0.34f, 0.10f, t);
            TriMat(w, solar,
                new Vector3(-xSpan, hgt * (0.48f + t * 0.04f), z), new Vector3(xSpan, hgt * (0.48f + t * 0.04f), z),
                new Vector3(0, hgt * (0.56f + t * 0.04f), z + len * 0.006f));
        }

        if (isGunship)
        {
            float zChin = len * 0.12f;
            TriMat(w, panel,
                new Vector3(-hw * 0.20f, hgt * 0.02f, zChin), new Vector3(hw * 0.20f, hgt * 0.02f, zChin),
                new Vector3(0, hgt * 0.10f, zChin + len * 0.010f));
            for (int s = 0; s < 3; s++)
            {
                float z = len * (0.08f + s * 0.05f);
                TriMat(w, panel,
                    new Vector3(-hw * 0.14f, hgt * 0.04f, z), new Vector3(hw * 0.14f, hgt * 0.04f, z),
                    new Vector3(0, hgt * 0.08f, z + len * 0.006f));
            }
            for (int g = 0; g < 4; g++)
            {
                float t = g / 3f;
                float z = MathHelper.Lerp(len * 0.04f, len * 0.20f, t);
                float xSpan = hw * MathHelper.Lerp(0.14f, 0.06f, t);
                TriMat(w, solar,
                    new Vector3(-xSpan, hgt * (0.50f + t * 0.04f), z), new Vector3(xSpan, hgt * (0.50f + t * 0.04f), z),
                    new Vector3(0, hgt * (0.58f + t * 0.04f), z + len * 0.006f));
            }
            for (int seam = 0; seam < 4; seam++)
            {
                float z = len * (0.06f + seam * 0.04f);
                TriMat(w, solar,
                    new Vector3(-hw * 0.10f, hgt * 0.02f, z), new Vector3(hw * 0.10f, hgt * 0.02f, z),
                    new Vector3(0, hgt * 0.06f, z + len * 0.005f));
            }
            TriMat(w, engineGlow,
                new Vector3(-hw * 0.10f, hgt * 0.02f, -len * 0.10f), new Vector3(hw * 0.10f, hgt * 0.02f, -len * 0.10f),
                new Vector3(0, hgt * 0.08f, -len * 0.08f));
        }

        if (isBomber)
        {
            for (int b = 0; b < 4; b++)
            {
                float z = len * (0.04f + b * 0.07f);
                float halfW = hw * MathHelper.Lerp(0.30f, 0.14f, b / 3f);
                TriMat(w, solar,
                    new Vector3(-halfW, hgt * 0.06f, z), new Vector3(halfW, hgt * 0.06f, z),
                    new Vector3(0, hgt * 0.12f, z + len * 0.008f));
            }
            for (int g = 0; g < 4; g++)
            {
                float t = g / 3f;
                float z = MathHelper.Lerp(len * 0.06f, len * 0.22f, t);
                float xSpan = hw * MathHelper.Lerp(0.12f, 0.05f, t);
                TriMat(w, solar,
                    new Vector3(-xSpan, hgt * (0.44f + t * 0.04f), z), new Vector3(xSpan, hgt * (0.44f + t * 0.04f), z),
                    new Vector3(0, hgt * (0.52f + t * 0.04f), z + len * 0.006f));
            }
            for (int r = 0; r < 3; r++)
            {
                float z = len * (0.04f + r * 0.06f);
                float ringW = hw * MathHelper.Lerp(0.18f, 0.10f, r / 2f);
                TriMat(w, solar,
                    new Vector3(-ringW, hgt * 0.04f, z), new Vector3(ringW, hgt * 0.04f, z),
                    new Vector3(0, hgt * 0.08f, z + len * 0.005f));
            }
            for (int e = 0; e < 2; e++)
            {
                float z = -len * (0.08f + e * 0.04f);
                TriMat(w, engineGlow,
                    new Vector3(-hw * 0.12f, hgt * 0.04f, z), new Vector3(hw * 0.12f, hgt * 0.04f, z),
                    new Vector3(0, hgt * 0.10f, z + len * 0.006f));
            }
        }

        if (isCorvette || isFrigate)
        {
            int crownGlow = isFrigate ? 4 : 3;
            for (int g = 0; g < crownGlow; g++)
            {
                float t = g / MathF.Max(1f, crownGlow - 1);
                float z = MathHelper.Lerp(len * 0.04f, len * (isFrigate ? 0.22f : 0.18f), t);
                float xSpan = hw * MathHelper.Lerp(0.10f, 0.05f, t);
                TriMat(w, solar,
                    new Vector3(-xSpan, hgt * (0.50f + t * 0.04f), z), new Vector3(xSpan, hgt * (0.50f + t * 0.04f), z),
                    new Vector3(0, hgt * (0.56f + t * 0.04f), z + len * 0.006f));
            }
        }

        AddRadiantMediumCombatSubstrate(w, hullKey, hw, hgt, len);
        AddRadiantGameplayComponentBands(w, hullKey, hw, hgt, len);
    }

    /// <summary>Solar panel substrate bands for Solari medium combat � vertex luminance under team tint.</summary>
    private static void AddRadiantMediumCombatSubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var recess = RaceMeshWriter.HullMaterial.Radiator;
        var emboss = RaceMeshWriter.HullMaterial.Truss;
        var solar = RaceMeshWriter.HullMaterial.Solar;
        var engineGlow = RaceMeshWriter.HullMaterial.Engine;

        bool isCorvette = hullKey is "corvette" or "corvette_fast";
        bool isFrigate = hullKey is "frigate" or "frigate_strike";
        bool isGunship = hullKey is "gunship" or "gunship_heavy";
        bool isBomber = hullKey is "bomber" or "bomber_heavy";
        bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";

        int bellyBands = isCorvette ? 5 : isBomber ? 5 : isFrigate ? 6 : isDestroyer ? 6 : 4;
        float bellyWidth = isBomber ? 0.48f : isDestroyer ? 0.40f : isFrigate ? 0.38f : isGunship ? 0.36f : 0.34f;
        float bellyReach = isCorvette ? 0.22f : isBomber ? 0.26f : isFrigate ? 0.28f : isDestroyer ? 0.34f : 0.20f;
        for (int i = 0; i < bellyBands; i++)
        {
            float t = bellyBands > 1 ? i / (bellyBands - 1f) : 0f;
            float z = MathHelper.Lerp(-len * 0.06f, len * bellyReach, t);
            float halfW = hw * MathHelper.Lerp(bellyWidth, bellyWidth * 0.66f, t);
            TriMat(w, recess,
                new Vector3(-halfW, hgt * 0.04f, z), new Vector3(halfW, hgt * 0.04f, z),
                new Vector3(0, hgt * (0.12f + t * 0.02f), z + len * 0.008f));
        }

        int flankBands = isDestroyer ? 5 : isFrigate ? 5 : isBomber ? 4 : isGunship ? 4 : 4;
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * hw * (isGunship ? 0.38f : isFrigate ? 0.36f : isBomber ? 0.36f : isDestroyer ? 0.34f : 0.32f);
            for (int s = 0; s < flankBands; s++)
            {
                float z = len * (0.02f + s * (isDestroyer ? 0.07f : 0.06f));
                TriMat(w, emboss,
                    new Vector3(x, hgt * 0.20f, z), new Vector3(x - side * hw * 0.03f, hgt * 0.24f, z + len * 0.01f),
                    new Vector3(x, hgt * 0.16f, z + len * 0.012f));
                if (s % 2 == 0)
                {
                    TriMat(w, solar,
                        new Vector3(x - side * hw * 0.02f, hgt * 0.18f, z + len * 0.004f),
                        new Vector3(x, hgt * 0.22f, z + len * 0.008f),
                        new Vector3(x - side * hw * 0.01f, hgt * 0.14f, z + len * 0.010f));
                }
            }
        }

        int dorsalStripes = isCorvette ? 5 : isFrigate ? 6 : isBomber ? 5 : isGunship ? 4 : 7;
        float dorsalReach = isFrigate ? 0.30f : isBomber ? 0.26f : isDestroyer ? 0.36f : isCorvette ? 0.24f : 0.22f;
        for (int d = 0; d < dorsalStripes; d++)
        {
            float t = d / MathF.Max(1f, dorsalStripes - 1);
            float z = MathHelper.Lerp(-len * 0.02f, len * dorsalReach, t);
            float xSpan = hw * MathHelper.Lerp(0.26f, 0.08f, t);
            TriMat(w, recess,
                new Vector3(-xSpan, hgt * (0.46f + t * 0.04f), z), new Vector3(xSpan, hgt * (0.46f + t * 0.04f), z),
                new Vector3(0, hgt * (0.50f + t * 0.04f), z + len * 0.006f));
            if (d % 2 == 0)
            {
                TriMat(w, solar,
                    new Vector3(-xSpan * 0.72f, hgt * (0.48f + t * 0.04f), z + len * 0.003f),
                    new Vector3(xSpan * 0.72f, hgt * (0.48f + t * 0.04f), z + len * 0.003f),
                    new Vector3(0, hgt * (0.52f + t * 0.04f), z + len * 0.007f));
            }
        }

        if (isDestroyer)
        {
            for (int e = 0; e < 3; e++)
            {
                float z = -len * (0.08f + e * 0.04f);
                float halfW = hw * MathHelper.Lerp(0.14f, 0.08f, e / 2f);
                TriMat(w, engineGlow,
                    new Vector3(-halfW, hgt * 0.05f, z), new Vector3(halfW, hgt * 0.05f, z),
                    new Vector3(0, hgt * 0.12f, z + len * 0.006f));
            }
        }
        else
        {
            TriMat(w, engineGlow,
                new Vector3(-hw * 0.10f, hgt * 0.04f, -len * 0.10f), new Vector3(hw * 0.10f, hgt * 0.04f, -len * 0.10f),
                new Vector3(0, hgt * 0.10f, -len * 0.08f));
        }
    }

    private static void AddRadiantGameplayComponentBands(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        bool isGunship = hullKey is "gunship" or "gunship_heavy";
        bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";
        bool isBomber = hullKey is "bomber" or "bomber_heavy";

        if (isGunship)
        {
            TriMat(w, weapon,
                new Vector3(-hw * 0.18f, hgt * 0.04f, len * 0.14f), new Vector3(hw * 0.18f, hgt * 0.04f, len * 0.14f),
                new Vector3(0, hgt * 0.10f, len * 0.20f));
        }

        if (isBomber)
        {
            TriMat(w, weapon,
                new Vector3(-hw * 0.22f, hgt * 0.04f, len * 0.06f), new Vector3(hw * 0.22f, hgt * 0.04f, len * 0.06f),
                new Vector3(0, hgt * 0.10f, len * 0.12f));
            TriMat(w, RaceMeshWriter.HullMaterial.ShieldGen,
                new Vector3(-hw * 0.06f, hgt * 0.44f, len * 0.06f), new Vector3(hw * 0.06f, hgt * 0.44f, len * 0.06f),
                new Vector3(0, hgt * 0.50f, len * 0.10f));
        }

        if (isDestroyer)
        {
            TriMat(w, weapon,
                new Vector3(-hw * 0.08f, hgt * 0.10f, len * 0.54f), new Vector3(hw * 0.08f, hgt * 0.10f, len * 0.54f),
                new Vector3(0, hgt * 0.18f, len * 0.60f));
            TriMat(w, RaceMeshWriter.HullMaterial.ShieldGen,
                new Vector3(-hw * 0.06f, hgt * 0.58f, len * 0.24f), new Vector3(hw * 0.06f, hgt * 0.58f, len * 0.24f),
                new Vector3(0, hgt * 0.64f, len * 0.28f));
        }

        if (hullKey is "corvette" or "corvette_fast" or "frigate" or "frigate_strike")
        {
            TriMat(w, RaceMeshWriter.HullMaterial.ShieldGen,
                new Vector3(-hw * 0.06f, hgt * 0.48f, len * 0.12f), new Vector3(hw * 0.06f, hgt * 0.48f, len * 0.12f),
                new Vector3(0, hgt * 0.54f, len * 0.16f));
        }

        TriMat(w, engine,
            new Vector3(-hw * 0.12f, hgt * 0.04f, -len * 0.12f), new Vector3(hw * 0.12f, hgt * 0.04f, -len * 0.12f),
            new Vector3(0, hgt * 0.10f, -len * 0.08f));
    }

    private static void ApplyRadiantCapitalShipDetail(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float hw = wid * 0.5f;
        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDreadnought = hullKey is "dreadnought";

        int dorsalSegs = isDreadnought ? 6 : isCruiser ? 6 : 6;
        float dorsalBase = isCarrier ? 0.42f : 0.52f;
        float dorsalRise = isCarrier ? 0.05f : 0.06f;
        for (int i = 0; i < dorsalSegs; i++)
        {
            float t = i / MathF.Max(1f, dorsalSegs - 1);
            float z = MathHelper.Lerp(len * 0.06f, len * (isCarrier ? 0.68f : isDreadnought ? 0.64f : 0.62f), t);
            TriMat(w, accent,
                new Vector3(0, hgt * (dorsalBase + t * dorsalRise), z),
                new Vector3(-hw * 0.10f, hgt * (dorsalBase - 0.10f + t * 0.03f), z - len * 0.02f),
                new Vector3(hw * 0.10f, hgt * (dorsalBase - 0.10f + t * 0.03f), z - len * 0.02f));
        }

        AddRadiantCapitalSubstrate(w, hullKey, hw, hgt, len);
        if (isCarrier)
            AddRadiantCarrierDeckPanels(w, hw, hgt, len);
        AddRadiantAccentRingBands(w, hullKey, hw, hgt, len);
        AddRadiantCapitalGameplayComponentBands(w, hullKey, hw, hgt, len);
    }

    private static void AddRadiantCapitalSubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var emboss = RaceMeshWriter.HullMaterial.Truss;
        var recess = RaceMeshWriter.HullMaterial.Hull;
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        bool isDreadnought = hullKey is "dreadnought";
        int bellyPlates = isDreadnought ? 6 : isCarrier ? 6 : 4;

        for (int i = 0; i < bellyPlates; i++)
        {
            float t = bellyPlates == 1 ? 0.5f : i / (float)(bellyPlates - 1);
            float z = MathHelper.Lerp(-len * 0.12f, len * (isCarrier ? 0.30f : isDreadnought ? 0.26f : 0.24f), t);
            float halfW = hw * MathHelper.Lerp(
                isCarrier ? 0.38f : isDreadnought ? 0.34f : 0.32f, 0.18f, t);
            TriMat(w, panel,
                new Vector3(-halfW, hgt * 0.05f, z), new Vector3(halfW, hgt * 0.05f, z),
                new Vector3(0, hgt * 0.14f, z + len * 0.010f));
            if (isCarrier && i % 2 == 0)
            {
                TriMat(w, recess,
                    new Vector3(-halfW * 0.72f, hgt * 0.08f, z + len * 0.004f),
                    new Vector3(halfW * 0.72f, hgt * 0.08f, z + len * 0.004f),
                    new Vector3(0, hgt * 0.11f, z + len * 0.008f));
            }
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float xOut = side * hw * (isCarrier ? 0.40f : isDreadnought ? 0.48f : 0.42f);
            int flankBands = isDreadnought ? 5 : isCarrier ? 4 : 3;
            for (int f = 0; f < flankBands; f++)
            {
                float t = f / MathF.Max(1f, flankBands - 1);
                float z = MathHelper.Lerp(-len * 0.08f, len * (isCarrier ? 0.20f : 0.18f), t);
                TriMat(w, panel,
                    new Vector3(xOut, hgt * 0.12f, z),
                    new Vector3(xOut - side * hw * 0.04f, hgt * 0.18f, z + len * 0.008f),
                    new Vector3(xOut, hgt * 0.08f, z + len * 0.012f));
            }
        }

        if (isCruiser)
        {
            for (int k = 0; k < 4; k++)
            {
                float z = MathHelper.Lerp(len * 0.02f, len * 0.26f, k / 3f);
                TriMat(w, emboss,
                    new Vector3(-hw * 0.14f, hgt * 0.32f, z), new Vector3(hw * 0.14f, hgt * 0.32f, z),
                    new Vector3(0, hgt * 0.36f, z + len * 0.006f));
            }
        }

        if (isDreadnought)
        {
            for (int k = 0; k < 4; k++)
            {
                float z = MathHelper.Lerp(len * 0.44f, len * 0.66f, k / 3f);
                TriMat(w, emboss,
                    new Vector3(-hw * 0.12f, hgt * 0.28f, z), new Vector3(hw * 0.12f, hgt * 0.28f, z),
                    new Vector3(0, hgt * 0.34f, z + len * 0.008f));
            }
        }
    }

    private static void AddRadiantCarrierDeckPanels(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var solar = RaceMeshWriter.HullMaterial.Solar;
        var recess = RaceMeshWriter.HullMaterial.Truss;
        float deckZ = MathHelper.Lerp(-len * 0.16f, len * 0.08f, 0.5f);
        TriMat(w, panel,
            new Vector3(-hw * 0.46f, hgt * 0.20f, deckZ), new Vector3(hw * 0.46f, hgt * 0.20f, deckZ),
            new Vector3(0, hgt * 0.24f, deckZ + len * 0.006f));
        TriMat(w, panel,
            new Vector3(-hw * 0.24f, hgt * 0.10f, -len * 0.06f), new Vector3(hw * 0.24f, hgt * 0.10f, -len * 0.06f),
            new Vector3(0, hgt * 0.14f, -len * 0.02f));
        for (int h = 0; h < 3; h++)
        {
            float z = len * (0.06f - h * 0.08f);
            TriMat(w, recess,
                new Vector3(-hw * 0.30f, hgt * 0.22f, z), new Vector3(hw * 0.30f, hgt * 0.22f, z),
                new Vector3(0, hgt * 0.26f, z + len * 0.008f));
        }
        for (int r = 0; r < 2; r++)
        {
            float z = len * (-0.04f + r * 0.10f);
            TriMat(w, solar,
                new Vector3(-hw * 0.18f, hgt * 0.08f, z), new Vector3(hw * 0.18f, hgt * 0.08f, z),
                new Vector3(0, hgt * 0.12f, z + len * 0.006f));
        }
    }

    private static void AddRadiantAccentRingBands(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var accent = RaceMeshWriter.HullMaterial.Solar;
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDreadnought = hullKey is "dreadnought";
        int rings = isDreadnought ? 6 : isCarrier ? 6 : 5;
        for (int v = 0; v < rings; v++)
        {
            float t = v / MathF.Max(1f, rings - 1);
            float z = MathHelper.Lerp(-len * 0.06f, len * (isCarrier ? 0.24f : 0.22f), t);
            for (int side = -1; side <= 1; side += 2)
            {
                float xOut = side * hw * (isCarrier ? 0.52f : isDreadnought ? 0.58f : 0.48f);
                TriMat(w, accent,
                    new Vector3(xOut, hgt * (0.20f + t * 0.04f), z),
                    new Vector3(xOut - side * hw * 0.03f, hgt * (0.24f + t * 0.04f), z + len * 0.008f),
                    new Vector3(xOut, hgt * (0.16f + t * 0.04f), z + len * 0.012f));
            }
        }
    }

    private static void AddRadiantCapitalGameplayComponentBands(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDreadnought = hullKey is "dreadnought";

        for (int e = 0; e < 3; e++)
        {
            float t = e / 2f;
            float x = MathHelper.Lerp(-hw * 0.22f, hw * 0.22f, t);
            TriMat(w, engine,
                new Vector3(x - hw * 0.06f, hgt * 0.04f, -len * 0.14f), new Vector3(x + hw * 0.06f, hgt * 0.04f, -len * 0.14f),
                new Vector3(x, hgt * 0.10f, -len * 0.10f));
        }

        if (isDreadnought)
        {
            TriMat(w, weapon,
                new Vector3(-hw * 0.10f, hgt * 0.08f, len * 0.58f), new Vector3(hw * 0.10f, hgt * 0.08f, len * 0.58f),
                new Vector3(0, hgt * 0.14f, len * 0.64f));
        }
        else if (isCarrier)
        {
            TriMat(w, weapon,
                new Vector3(-hw * 0.28f, hgt * 0.26f, len * 0.04f), new Vector3(hw * 0.28f, hgt * 0.26f, len * 0.04f),
                new Vector3(0, hgt * 0.30f, len * 0.08f));
        }
        else
        {
            TriMat(w, weapon,
                new Vector3(-hw * 0.08f, hgt * 0.16f, len * 0.56f), new Vector3(hw * 0.08f, hgt * 0.16f, len * 0.56f),
                new Vector3(0, hgt * 0.22f, len * 0.62f));
        }
    }

    /// <summary>Final-pass solar accent bands � TriScorerAccent after relight, excluded from weapon snap.</summary>
    public static void AppendRadiantScorerAccentBands(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        if (hullKey is "scout" or "scout_light" or "fighter" or "fighter_basic"
            or "interceptor" or "interceptor_mk2" or "drone" or "drone_swarm"
            or "hero" or "hero_default")
        {
            AppendRadiantSmallCraftScorerAccentBands(w, hullKey, len, wid, hgt);
            return;
        }

        if (hullKey is "carrier" or "carrier_command")
        {
            AppendRadiantCapitalCarrierScorerAccentBands(w, len, wid, hgt);
            return;
        }

        if (hullKey is "cruiser" or "cruiser_heavy")
        {
            AppendRadiantCapitalCruiserScorerAccentBands(w, len, wid, hgt);
            return;
        }

        if (hullKey is "dreadnought")
        {
            AppendRadiantCapitalDreadnoughtScorerAccentBands(w, len, wid, hgt);
            return;
        }

        if (hullKey is "corvette_fast" or "frigate_strike" or "gunship_heavy" or "bomber_heavy"
            or "destroyer_assault" or "corvette" or "frigate" or "gunship" or "bomber" or "destroyer"
            or "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk" or "support_repair")
            AppendOrganicScorerAccentBands(w, hullKey, len, wid, hgt);
    }

    private static void AppendRadiantCapitalCarrierScorerAccentBands(
        RaceMeshWriter w, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        for (int d = 0; d < 5; d++)
        {
            float t = d / 4f;
            float z = MathHelper.Lerp(-len * 0.08f, len * 0.20f, t);
            float xSpan = hw * MathHelper.Lerp(0.36f, 0.12f, t);
            float yBase = hgt * (0.50f + t * 0.06f);
            w.TriScorerAccent(
                new Vector3(-xSpan, yBase, z), new Vector3(xSpan, yBase, z),
                new Vector3(0, yBase + hgt * 0.06f, z + len * 0.005f));
        }
        for (int h = 0; h < 4; h++)
        {
            float z = len * (-0.06f + h * 0.05f);
            w.TriScorerAccent(
                new Vector3(-hw * 0.22f, hgt * 0.10f, z), new Vector3(hw * 0.22f, hgt * 0.10f, z),
                new Vector3(0, hgt * 0.14f, z + len * 0.006f));
        }
        for (int side = -1; side <= 1; side += 2)
        {
            float xOut = side * hw * 0.46f;
            for (int r = 0; r < 4; r++)
            {
                float z = len * (0.04f + r * 0.08f);
                w.TriScorerAccent(
                    new Vector3(xOut, hgt * 0.20f, z),
                    new Vector3(xOut - side * hw * 0.03f, hgt * 0.24f, z + len * 0.008f),
                    new Vector3(xOut, hgt * 0.16f, z + len * 0.010f));
            }
        }
    }

    private static void AppendRadiantCapitalCruiserScorerAccentBands(
        RaceMeshWriter w, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        for (int d = 0; d < 5; d++)
        {
            float t = d / 4f;
            float z = MathHelper.Lerp(len * 0.02f, len * 0.24f, t);
            w.TriScorerAccent(
                new Vector3(-hw * 0.08f, hgt * (0.50f + t * 0.06f), z),
                new Vector3(hw * 0.08f, hgt * (0.50f + t * 0.06f), z),
                new Vector3(0, hgt * (0.58f + t * 0.06f), z + len * 0.005f));
        }
        for (int p = 0; p < 3; p++)
        {
            float z = len * (0.54f + p * 0.06f);
            w.TriScorerAccent(
                new Vector3(-hw * 0.05f, hgt * 0.30f, z), new Vector3(hw * 0.05f, hgt * 0.30f, z),
                new Vector3(0, hgt * 0.36f, z + len * 0.006f));
        }
        for (int side = -1; side <= 1; side += 2)
        {
            float xOut = side * hw * 0.40f;
            for (int r = 0; r < 4; r++)
            {
                float z = len * (0.04f + r * 0.07f);
                w.TriScorerAccent(
                    new Vector3(xOut, hgt * 0.16f, z),
                    new Vector3(xOut - side * hw * 0.03f, hgt * 0.20f, z + len * 0.008f),
                    new Vector3(xOut, hgt * 0.12f, z + len * 0.010f));
            }
        }
    }

    private static void AppendRadiantCapitalDreadnoughtScorerAccentBands(
        RaceMeshWriter w, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        for (int d = 0; d < 4; d++)
        {
            float z = len * (0.34f + d * 0.08f);
            w.TriScorerAccent(
                new Vector3(-hw * 0.06f, hgt * 0.34f, z), new Vector3(hw * 0.06f, hgt * 0.34f, z),
                new Vector3(0, hgt * 0.40f, z + len * 0.005f));
        }
        for (int side = -1; side <= 1; side += 2)
        {
            float xTip = side * hw * 0.28f;
            w.TriScorerAccent(
                new Vector3(xTip, hgt * 0.28f, len * 0.54f),
                new Vector3(xTip - side * hw * 0.03f, hgt * 0.32f, len * 0.58f),
                new Vector3(xTip, hgt * 0.24f, len * 0.56f));
            for (int r = 0; r < 3; r++)
            {
                float z = len * (0.46f + r * 0.06f);
                w.TriScorerAccent(
                    new Vector3(xTip, hgt * (0.18f + r * 0.02f), z),
                    new Vector3(xTip - side * hw * 0.03f, hgt * (0.22f + r * 0.02f), z + len * 0.008f),
                    new Vector3(xTip, hgt * (0.14f + r * 0.02f), z + len * 0.010f));
            }
        }
    }

    /// <summary>Radiant compact/reference craft accent � dorsal crown spine + leading-edge amber solar strips.</summary>
    private static void AppendRadiantSmallCraftScorerAccentBands(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;

        switch (hullKey)
        {
            case "scout":
            case "scout_light":
                for (int s = 0; s < 7; s++)
                {
                    float t = s / 6f;
                    float z = MathHelper.Lerp(len * 0.04f, len * 0.32f, t);
                    float yBase = hgt * (0.48f + t * 0.08f);
                    float halfW = hw * 0.04f;
                    w.TriScorerAccent(
                        new Vector3(-halfW, yBase, z),
                        new Vector3(halfW, yBase, z),
                        new Vector3(0, yBase + hgt * 0.06f, z + len * 0.005f));
                }

                for (int side = -1; side <= 1; side += 2)
                {
                    float xLead = side * hw * 0.40f;
                    for (int e = 0; e < 6; e++)
                    {
                        float z = MathHelper.Lerp(len * 0.16f, -len * 0.02f, e / 5f);
                        w.TriScorerAccent(
                            new Vector3(xLead, hgt * (0.20f + e * 0.018f), z),
                            new Vector3(xLead - side * hw * 0.025f, hgt * (0.18f + e * 0.018f), z + len * 0.006f),
                            new Vector3(xLead, hgt * (0.16f + e * 0.018f), z + len * 0.009f));
                    }
                }
                break;

            case "fighter":
            case "fighter_basic":
                for (int s = 0; s < 7; s++)
                {
                    float t = s / 6f;
                    float z = MathHelper.Lerp(len * 0.02f, len * 0.24f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.05f, hgt * (0.50f + t * 0.06f), z),
                        new Vector3(hw * 0.05f, hgt * (0.50f + t * 0.06f), z),
                        new Vector3(0, hgt * (0.58f + t * 0.06f), z + len * 0.005f));
                }

                for (int side = -1; side <= 1; side += 2)
                {
                    float xLead = side * hw * 0.54f;
                    for (int e = 0; e < 5; e++)
                    {
                        float z = MathHelper.Lerp(len * 0.12f, -len * 0.02f, e / 4f);
                        w.TriScorerAccent(
                            new Vector3(xLead, hgt * (0.18f + e * 0.015f), z),
                            new Vector3(xLead - side * hw * 0.028f, hgt * (0.16f + e * 0.015f), z + len * 0.007f),
                            new Vector3(xLead, hgt * (0.14f + e * 0.015f), z + len * 0.010f));
                    }
                }
                break;

            case "interceptor":
            case "interceptor_mk2":
                for (int s = 0; s < 5; s++)
                {
                    float t = s / 4f;
                    float z = MathHelper.Lerp(len * 0.08f, len * 0.30f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.04f, hgt * (0.48f + t * 0.06f), z),
                        new Vector3(hw * 0.04f, hgt * (0.48f + t * 0.06f), z),
                        new Vector3(0, hgt * (0.56f + t * 0.06f), z + len * 0.005f));
                }

                for (int side = -1; side <= 1; side += 2)
                {
                    float xCap = side * hw * 0.36f;
                    for (int e = 0; e < 5; e++)
                    {
                        float z = MathHelper.Lerp(len * 0.16f, len * 0.02f, e / 4f);
                        w.TriScorerAccent(
                            new Vector3(xCap, hgt * (0.18f + e * 0.02f), z),
                            new Vector3(xCap - side * hw * 0.03f, hgt * (0.16f + e * 0.02f), z + len * 0.007f),
                            new Vector3(xCap, hgt * (0.14f + e * 0.02f), z + len * 0.010f));
                    }
                }
                break;

            case "drone":
            case "drone_swarm":
                for (int s = 0; s < 9; s++)
                {
                    float t = s / 8f;
                    float z = MathHelper.Lerp(-len * 0.08f, len * 0.30f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.035f, hgt * (0.40f + t * 0.10f), z),
                        new Vector3(hw * 0.035f, hgt * (0.40f + t * 0.10f), z),
                        new Vector3(0, hgt * (0.48f + t * 0.10f), z + len * 0.005f));
                }

                for (int n = 0; n < 7; n++)
                {
                    float z = MathHelper.Lerp(len * 0.02f, len * 0.28f, n / 6f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.06f, hgt * 0.32f, z),
                        new Vector3(hw * 0.06f, hgt * 0.32f, z),
                        new Vector3(0, hgt * 0.38f, z + len * 0.006f));
                }
                break;

            case "hero":
            case "hero_default":
                for (int s = 0; s < 4; s++)
                {
                    float t = s / 3f;
                    float z = MathHelper.Lerp(len * 0.06f, len * 0.24f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.05f, hgt * (0.58f + t * 0.06f), z),
                        new Vector3(hw * 0.05f, hgt * (0.58f + t * 0.06f), z),
                        new Vector3(0, hgt * (0.66f + t * 0.06f), z + len * 0.005f));
                }

                for (int side = -1; side <= 1; side += 2)
                {
                    float xLead = side * hw * 0.48f;
                    for (int e = 0; e < 4; e++)
                    {
                        float z = MathHelper.Lerp(len * 0.12f, len * 0.02f, e / 3f);
                        w.TriScorerAccent(
                            new Vector3(xLead, hgt * (0.22f + e * 0.015f), z),
                            new Vector3(xLead - side * hw * 0.028f, hgt * (0.20f + e * 0.015f), z + len * 0.007f),
                            new Vector3(xLead, hgt * (0.18f + e * 0.015f), z + len * 0.010f));
                    }
                }
                break;
        }
    }

    private static void ApplyAsymmetricShipDetail(
        RaceMeshWriter w, RaceVisualDefinition race, string hullKey, float len, float wid, float hgt)
    {
        if (hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk" or "support_repair")
        {
            ApplyAsymmetricUtilityShipDetail(w, hullKey, len, wid, hgt);
            return;
        }

        if (hullKey is "corvette" or "corvette_fast" or "frigate" or "frigate_strike"
            or "gunship" or "gunship_heavy" or "bomber" or "bomber_heavy"
            or "destroyer" or "destroyer_assault")
        {
            ApplyAsymmetricMediumCombatShipDetail(w, hullKey, len, wid, hgt);
            return;
        }

        if (hullKey is "cruiser" or "cruiser_heavy" or "carrier" or "carrier_command" or "dreadnought")
        {
            ApplyAsymmetricCapitalShipDetail(w, hullKey, len, wid, hgt);
            return;
        }

        bool isScout = hullKey is "scout" or "scout_light";
        bool isFighter = hullKey is "fighter" or "fighter_basic";
        bool isInterceptor = hullKey is "interceptor" or "interceptor_mk2";
        bool isDrone = hullKey is "drone" or "drone_swarm";
        bool isHero = hullKey is "hero" or "hero_default";
        bool isCompactCraft = isScout || isFighter || isInterceptor || isDrone;
        bool isReferenceCraft = isFighter || isHero;

        if (!isCompactCraft && !isHero)
            return;

        float hw = wid * 0.5f;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        int chitinSegs = isDrone ? 4 : isScout ? 5 : isInterceptor ? 4 : isFighter ? 4 : 5;
        float asymBias = 0.06f;
        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < chitinSegs; i++)
            {
                float t = i / MathF.Max(1f, chitinSegs - 1);
                float z = MathHelper.Lerp(-len * 0.04f, len * (isScout ? 0.18f : isDrone ? 0.16f : 0.14f), t);
                float xOut = side * hw * MathHelper.Lerp(isScout ? 0.58f : isDrone ? 0.44f : 0.54f, 0.36f, t) + asymBias * side;
                TriMat(w, accent,
                    new Vector3(xOut, hgt * (0.16f + t * 0.04f), z),
                    new Vector3(xOut - side * hw * 0.03f, hgt * (0.13f + t * 0.04f), z + len * 0.008f),
                    new Vector3(xOut, hgt * (0.11f + t * 0.03f), z + len * 0.012f));
            }
        }

        if (isHero)
        {
            for (int p = 0; p < 4; p++)
            {
                float t = p / 3f;
                float z = MathHelper.Lerp(-len * 0.06f, len * 0.22f, t);
                float xSpan = hw * MathHelper.Lerp(0.32f, 0.10f, t) + asymBias;
                TriMat(w, accent,
                    new Vector3(-xSpan, hgt * (0.60f + t * 0.06f), z), new Vector3(xSpan, hgt * (0.60f + t * 0.06f), z),
                    new Vector3(asymBias, hgt * (0.68f + t * 0.06f), z + len * 0.006f));
            }
        }

        if (isReferenceCraft)
            AddAsymmetricReferenceCraftSubstrate(w, hw, hgt, len, isHero);
        else
            AddAsymmetricCompactCraftSubstrate(w, hullKey, hw, hgt, len);

        AddAsymmetricSmallCraftGameplayComponentBands(w, hullKey, hw, hgt, len);
    }

    /// <summary>Belly/flank chitin substrate bands for Nexar compact craft.</summary>
    private static void AddAsymmetricCompactCraftSubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var chitin = RaceMeshWriter.HullMaterial.Truss;
        bool isScout = hullKey is "scout" or "scout_light";
        bool isInterceptor = hullKey is "interceptor" or "interceptor_mk2";
        bool isDrone = hullKey is "drone" or "drone_swarm";
        float asymBias = 0.05f;

        int bellyPlates = 3;
        float bellyZStart = isDrone ? -len * 0.06f : -len * 0.05f;
        float bellyZEnd = isScout ? len * 0.20f : isDrone ? len * 0.22f : len * 0.18f;
        float bellyHalfW = isScout ? 0.18f : isDrone ? 0.12f : 0.20f;
        float bellyLift = isDrone ? 0.14f : 0.16f;

        for (int i = 0; i < bellyPlates; i++)
        {
            float t = i / MathF.Max(1f, bellyPlates - 1);
            float z = MathHelper.Lerp(bellyZStart, bellyZEnd, t);
            float halfW = hw * MathHelper.Lerp(bellyHalfW, bellyHalfW * 0.72f, t);
            TriMat(w, panel,
                new Vector3(-halfW + asymBias, hgt * 0.03f, z), new Vector3(halfW + asymBias, hgt * 0.03f, z),
                new Vector3(asymBias, hgt * bellyLift, z + len * 0.008f));
            TriMat(w, chitin,
                new Vector3(-halfW * 0.82f + asymBias, hgt * 0.05f, z - len * 0.004f),
                new Vector3(halfW * 0.82f + asymBias, hgt * 0.05f, z - len * 0.004f),
                new Vector3(asymBias, hgt * (bellyLift * 0.68f), z));
        }

        int seamCount = 3;
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * hw * (isScout ? 0.24f : isDrone ? 0.14f : isInterceptor ? 0.28f : 0.26f) + asymBias * side;
            for (int s = 0; s < seamCount; s++)
            {
                float z = len * (isDrone ? (-0.02f + s * 0.10f) : (0.02f + s * 0.08f));
                TriMat(w, chitin,
                    new Vector3(x, hgt * 0.24f, z), new Vector3(x - side * hw * 0.03f, hgt * 0.28f, z + len * 0.01f),
                    new Vector3(x, hgt * 0.20f, z + len * 0.014f));
            }
        }
    }

    private static void AddAsymmetricReferenceCraftSubstrate(RaceMeshWriter w, float hw, float hgt, float len, bool isHero)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var chitin = RaceMeshWriter.HullMaterial.Truss;
        float asymBias = 0.06f;
        int dorsalBands = isHero ? 5 : 4;
        for (int i = 0; i < dorsalBands; i++)
        {
            float t = i / MathF.Max(1f, dorsalBands - 1);
            float z = MathHelper.Lerp(-len * 0.04f, len * (isHero ? 0.22f : 0.16f), t);
            TriMat(w, chitin,
                new Vector3(-hw * 0.08f + asymBias, hgt * (0.44f + t * 0.06f), z),
                new Vector3(hw * 0.04f + asymBias, hgt * (0.44f + t * 0.06f), z),
                new Vector3(asymBias, hgt * (0.52f + t * 0.06f), z + len * 0.006f));
        }

        for (int i = 0; i < 3; i++)
        {
            float z = MathHelper.Lerp(-len * 0.06f, len * 0.14f, i / 2f);
            float halfW = hw * MathHelper.Lerp(0.24f, 0.16f, i / 2f);
            TriMat(w, panel,
                new Vector3(-halfW + asymBias, hgt * 0.04f, z), new Vector3(halfW + asymBias, hgt * 0.04f, z),
                new Vector3(asymBias, hgt * 0.16f, z + len * 0.008f));
        }
    }

    private static void AddAsymmetricSmallCraftGameplayComponentBands(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        bool isDrone = hullKey is "drone" or "drone_swarm";
        bool isInterceptor = hullKey is "interceptor" or "interceptor_mk2";

        for (int e = 0; e < 3; e++)
        {
            float x = (e - 1) * hw * 0.14f + hw * 0.04f;
            TriMat(w, engine,
                new Vector3(x - hw * 0.05f, hgt * 0.04f, -len * 0.14f), new Vector3(x + hw * 0.05f, hgt * 0.04f, -len * 0.14f),
                new Vector3(x, hgt * 0.10f, -len * 0.10f));
        }

        if (isInterceptor)
        {
            TriMat(w, weapon,
                new Vector3(-hw * 0.08f, hgt * 0.10f, len * 0.30f), new Vector3(hw * 0.08f, hgt * 0.10f, len * 0.30f),
                new Vector3(0, hgt * 0.16f, len * 0.36f));
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float xOut = side * hw * (isDrone ? 0.22f : 0.38f);
            TriMat(w, weapon,
                new Vector3(xOut, hgt * 0.10f, len * 0.04f),
                new Vector3(xOut - side * hw * 0.04f, hgt * 0.14f, len * 0.08f),
                new Vector3(xOut, hgt * 0.08f, len * 0.10f));
        }

        TriMat(w, shield,
            new Vector3(-hw * 0.06f, hgt * 0.66f, len * 0.06f), new Vector3(hw * 0.06f, hgt * 0.66f, len * 0.06f),
            new Vector3(0, hgt * 0.74f, len * 0.10f));
    }

    private static void ApplyAsymmetricUtilityShipDetail(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var shadow = RaceMeshWriter.HullMaterial.Radiator;
        var chitin = RaceMeshWriter.HullMaterial.Truss;
        var engineMat = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float hw = wid * 0.5f;
        float widthScale = hullKey is "freighter_bulk" ? 0.90f : hullKey is "miner_eva" ? 0.92f : 0.96f;
        hw *= widthScale;
        bool triBudgetTight = hullKey is "freighter_bulk" or "transport_cargo";
        bool isMiner = hullKey is "miner_basic" or "miner_eva" or "miner_tractor";
        bool isSupport = hullKey is "support_repair";
        float asymBias = 0.06f;
        int veinSegs = hullKey is "freighter_bulk" ? 2 : triBudgetTight ? 3 : isMiner ? 4 : 4;

        for (int side = -1; side <= 1; side += 2)
        {
            float xOut = side * hw * (hullKey is "freighter_bulk" ? 0.46f : hullKey is "transport_cargo" ? 0.44f : 0.54f)
                + asymBias * side;
            for (int i = 0; i < veinSegs; i++)
            {
                float t = i / MathF.Max(1f, veinSegs - 1);
                float z = MathHelper.Lerp(-len * 0.04f, len * (hullKey is "freighter_bulk" ? 0.32f : triBudgetTight ? 0.30f : isMiner ? 0.18f : isSupport ? 0.14f : 0.12f), t);
                TriMat(w, accent,
                    new Vector3(xOut, hgt * (0.16f + t * 0.05f), z),
                    new Vector3(xOut - side * hw * 0.035f, hgt * (0.13f + t * 0.05f), z + len * 0.010f),
                    new Vector3(xOut, hgt * (0.11f + t * 0.04f), z + len * 0.014f));
            }
        }

        if (hullKey is "miner_basic")
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xTip = side * hw * 0.70f;
                TriMat(w, weapon,
                    new Vector3(xTip, hgt * 0.20f, len * 0.44f),
                    new Vector3(xTip + side * hw * 0.04f, hgt * 0.26f, len * 0.50f),
                    new Vector3(xTip, hgt * 0.14f, len * 0.52f));
                TriMat(w, engineMat,
                    new Vector3(xTip - side * hw * 0.03f, hgt * 0.06f, len * 0.08f),
                    new Vector3(xTip + side * hw * 0.03f, hgt * 0.06f, len * 0.08f),
                    new Vector3(xTip, hgt * 0.12f, len * 0.12f));
            }
            TriMat(w, engineMat,
                new Vector3(-hw * 0.18f, hgt * 0.04f, -len * 0.08f),
                new Vector3(hw * 0.18f, hgt * 0.04f, -len * 0.08f),
                new Vector3(0, hgt * 0.10f, -len * 0.04f));
        }

        if (hullKey is "support_repair")
        {
            for (int a = 0; a < 2; a++)
            {
                float z = len * (0.06f + a * 0.060f);
                TriMat(w, accent,
                    new Vector3(-hw * 0.04f + asymBias, hgt * (0.74f + a * 0.04f), z),
                    new Vector3(hw * 0.04f + asymBias, hgt * (0.74f + a * 0.04f), z),
                    new Vector3(asymBias, hgt * (0.82f + a * 0.04f), z + len * 0.008f));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float xBoom = side * hw * 0.64f;
                TriMat(w, weapon,
                    new Vector3(xBoom, hgt * 0.54f, len * 0.12f),
                    new Vector3(xBoom + side * hw * 0.05f, hgt * 0.60f, len * 0.14f),
                    new Vector3(xBoom, hgt * 0.48f, len * 0.16f));
            }
        }

        if (hullKey is "transport_cargo")
        {
            for (int s = 0; s < 3; s++)
            {
                float z = len * (0.10f + s * 0.10f);
                TriMat(w, chitin,
                    new Vector3(-hw * 0.06f + asymBias, hgt * 0.40f, z),
                    new Vector3(hw * 0.06f + asymBias, hgt * 0.40f, z),
                    new Vector3(asymBias, hgt * 0.46f, z + len * 0.010f));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float cx = side * hw * 0.42f + asymBias * side;
                TriMat(w, weapon,
                    new Vector3(cx - hw * 0.06f, hgt * 0.22f, len * 0.26f),
                    new Vector3(cx + hw * 0.06f, hgt * 0.22f, len * 0.26f),
                    new Vector3(cx, hgt * 0.28f, len * 0.30f));
            }
        }

        if (hullKey is "freighter_bulk")
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float cx = side * hw * 0.38f + asymBias * side;
                TriMat(w, shadow,
                    new Vector3(cx - hw * 0.06f, hgt * 0.16f, len * 0.28f),
                    new Vector3(cx + hw * 0.06f, hgt * 0.16f, len * 0.28f),
                    new Vector3(cx, hgt * 0.22f, len * 0.34f));
            }
            TriMat(w, accent,
                new Vector3(-hw * 0.08f + asymBias, hgt * 0.34f, len * 0.36f),
                new Vector3(hw * 0.08f + asymBias, hgt * 0.34f, len * 0.36f),
                new Vector3(asymBias, hgt * 0.40f, len * 0.42f));
        }

        AddAsymmetricUtilitySubstrate(w, hullKey, hw, hgt, len);

        if (!triBudgetTight)
        {
            TriMat(w, engineMat,
                new Vector3(-hw * 0.12f, 0, -len * 0.08f), new Vector3(hw * 0.12f, 0, -len * 0.08f),
                new Vector3(0, hgt * 0.06f, -len * 0.12f));
        }
    }

    /// <summary>Belly/flank chitin membrane substrate bands � amber vein accents under team tint.</summary>
    private static void AddAsymmetricUtilitySubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var membrane = RaceMeshWriter.HullMaterial.Hull;
        var vein = RaceMeshWriter.HullMaterial.Solar;
        var shadow = RaceMeshWriter.HullMaterial.Radiator;
        bool isCargo = hullKey is "freighter_bulk" or "transport_cargo";
        bool isMiner = hullKey is "miner_basic" or "miner_eva" or "miner_tractor";
        bool isSupport = hullKey is "support_repair";
        float asymBias = 0.05f;
        // Loop-07 tri trim: utility substrate bands under 180 sweet max.
        int bellyPlates = isCargo ? 2 : isSupport ? 2 : isMiner ? 3 : 2;

        for (int i = 0; i < bellyPlates; i++)
        {
            float t = i / MathF.Max(1f, bellyPlates - 1);
            float z = MathHelper.Lerp(-len * 0.06f, len * (isCargo ? 0.24f : isSupport ? 0.16f : isMiner ? 0.18f : 0.14f), t);
            float halfW = hw * MathHelper.Lerp(
                hullKey is "freighter_bulk" ? 0.26f : isCargo ? 0.20f : 0.22f,
                hullKey is "freighter_bulk" ? 0.12f : 0.10f, t);
            TriMat(w, shadow,
                new Vector3(-halfW + asymBias, hgt * 0.02f, z), new Vector3(halfW + asymBias, hgt * 0.02f, z),
                new Vector3(asymBias, hgt * (0.16f + t * 0.03f), z + len * 0.012f));
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * hw * (hullKey is "freighter_bulk" ? 0.22f : isCargo ? 0.16f : 0.20f) + asymBias * side;
            int flankSegs = isCargo ? 2 : isSupport ? 2 : isMiner ? 2 : 2;
            for (int s = 0; s < flankSegs; s++)
            {
                float z = MathHelper.Lerp(-len * 0.02f, len * (isCargo ? 0.16f : isSupport ? 0.12f : isMiner ? 0.14f : 0.10f), s / MathF.Max(1f, flankSegs - 1));
                TriMat(w, membrane,
                    new Vector3(x, hgt * 0.14f, z),
                    new Vector3(x - side * hw * 0.03f, hgt * 0.26f, z + len * 0.012f),
                    new Vector3(x, hgt * 0.10f, z + len * 0.014f));
            }
        }

        int spineSegs = isCargo ? 2 : isSupport ? 3 : isMiner ? 3 : 2;
        for (int i = 0; i < spineSegs; i++)
        {
            float t = i / MathF.Max(1f, spineSegs - 1);
            float z = MathHelper.Lerp(-len * 0.04f, len * (hullKey switch
            {
                "freighter_bulk" => 0.40f,
                "transport_cargo" => 0.38f,
                "miner_basic" => 0.28f,
                "miner_eva" => 0.22f,
                "miner_tractor" => 0.26f,
                "support_repair" => 0.20f,
                _ => 0.14f
            }), t);
            float xSpan = hw * MathHelper.Lerp(0.12f, 0.03f, t);
            TriMat(w, shadow,
                new Vector3(-xSpan + asymBias, hgt * (0.44f + t * 0.06f), z),
                new Vector3(xSpan + asymBias, hgt * (0.44f + t * 0.06f), z),
                new Vector3(asymBias, hgt * (0.56f + t * 0.04f), z + len * 0.010f));
            if (isMiner || isSupport || isCargo)
            {
                TriMat(w, vein,
                    new Vector3(-xSpan * 0.7f + asymBias, hgt * (0.54f + t * 0.04f), z + len * 0.006f),
                    new Vector3(xSpan * 0.7f + asymBias, hgt * (0.54f + t * 0.04f), z + len * 0.006f),
                    new Vector3(asymBias, hgt * (0.60f + t * 0.03f), z + len * 0.012f));
            }
        }
    }

    private static void ApplyAsymmetricCapitalShipDetail(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float hw = wid * 0.5f;
        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDreadnought = hullKey is "dreadnought";

        int dorsalSegs = isDreadnought ? 4 : isCruiser ? 6 : 5;
        float dorsalBase = isCarrier ? 0.44f : 0.54f;
        float dorsalRise = isCarrier ? 0.05f : 0.06f;
        float asymBias = isCarrier ? 0.06f : 0.04f;
        for (int i = 0; i < dorsalSegs; i++)
        {
            float t = i / MathF.Max(1f, dorsalSegs - 1);
            float z = MathHelper.Lerp(len * 0.06f, len * (isCarrier ? 0.68f : 0.62f), t);
            TriMat(w, accent,
                new Vector3(asymBias, hgt * (dorsalBase + t * dorsalRise), z),
                new Vector3(-hw * 0.10f + asymBias, hgt * (dorsalBase - 0.10f + t * 0.03f), z - len * 0.02f),
                new Vector3(hw * 0.10f + asymBias, hgt * (dorsalBase - 0.10f + t * 0.03f), z - len * 0.02f));
        }

        AddAsymmetricCapitalSubstrate(w, hullKey, hw, hgt, len);
        if (isCarrier)
            AddAsymmetricCarrierDeckChitin(w, hw, hgt, len);
        AddAsymmetricAccentRingStrips(w, hullKey, hw, hgt, len);
        AddAsymmetricGameplayComponentBands(w, hullKey, hw, hgt, len);
    }

    private static void AddAsymmetricCapitalSubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var fold = RaceMeshWriter.HullMaterial.Truss;
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        bool isDreadnought = hullKey is "dreadnought";
        int bellyPlates = isDreadnought ? 5 : isCarrier ? 5 : 4;
        float asymBias = isCarrier ? 0.08f : 0.05f;

        for (int i = 0; i < bellyPlates; i++)
        {
            float t = bellyPlates == 1 ? 0.5f : i / (float)(bellyPlates - 1);
            float z = MathHelper.Lerp(-len * 0.12f, len * (isCarrier ? 0.28f : 0.22f), t);
            float halfW = hw * MathHelper.Lerp(
                isCarrier ? 0.38f : isDreadnought ? 0.34f : 0.32f, 0.18f, t);
            TriMat(w, panel,
                new Vector3(-halfW + asymBias, hgt * 0.05f, z), new Vector3(halfW + asymBias, hgt * 0.05f, z),
                new Vector3(asymBias, hgt * 0.14f, z + len * 0.010f));
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float xOut = side * hw * (isCarrier ? 0.40f : isDreadnought ? 0.48f : 0.42f) + asymBias * side;
            int flankBands = isDreadnought ? 4 : 3;
            for (int f = 0; f < flankBands; f++)
            {
                float t = f / MathF.Max(1f, flankBands - 1);
                float z = MathHelper.Lerp(-len * 0.08f, len * 0.18f, t);
                TriMat(w, panel,
                    new Vector3(xOut, hgt * 0.12f, z),
                    new Vector3(xOut - side * hw * 0.04f, hgt * 0.18f, z + len * 0.008f),
                    new Vector3(xOut, hgt * 0.08f, z + len * 0.012f));
            }
        }

        if (isCruiser)
        {
            for (int k = 0; k < 4; k++)
            {
                float z = MathHelper.Lerp(len * 0.02f, len * 0.24f, k / 3f);
                TriMat(w, fold,
                    new Vector3(-hw * 0.14f + asymBias, hgt * 0.32f, z), new Vector3(hw * 0.14f + asymBias, hgt * 0.32f, z),
                    new Vector3(asymBias, hgt * 0.36f, z + len * 0.006f));
            }
        }

        if (isDreadnought)
        {
            for (int k = 0; k < 3; k++)
            {
                float z = MathHelper.Lerp(len * 0.46f, len * 0.64f, k / 2f);
                TriMat(w, fold,
                    new Vector3(-hw * 0.12f + asymBias, hgt * 0.28f, z), new Vector3(hw * 0.12f + asymBias, hgt * 0.28f, z),
                    new Vector3(asymBias, hgt * 0.34f, z + len * 0.008f));
            }
        }
    }

    private static void AddAsymmetricCarrierDeckChitin(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var recess = RaceMeshWriter.HullMaterial.Truss;
        float asymBias = 0.08f;
        float deckZ = MathHelper.Lerp(-len * 0.16f, len * 0.08f, 0.5f);
        TriMat(w, panel,
            new Vector3(-hw * 0.46f + asymBias, hgt * 0.20f, deckZ), new Vector3(hw * 0.46f + asymBias, hgt * 0.20f, deckZ),
            new Vector3(asymBias, hgt * 0.24f, deckZ + len * 0.006f));
        TriMat(w, panel,
            new Vector3(-hw * 0.24f + asymBias, hgt * 0.10f, -len * 0.06f), new Vector3(hw * 0.24f + asymBias, hgt * 0.10f, -len * 0.06f),
            new Vector3(asymBias, hgt * 0.14f, -len * 0.02f));
        for (int h = 0; h < 3; h++)
        {
            float z = len * (0.06f - h * 0.08f);
            TriMat(w, recess,
                new Vector3(-hw * 0.30f + asymBias, hgt * 0.22f, z), new Vector3(hw * 0.30f + asymBias, hgt * 0.22f, z),
                new Vector3(asymBias, hgt * 0.26f, z + len * 0.008f));
        }
    }

    private static void AddAsymmetricAccentRingStrips(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var accent = RaceMeshWriter.HullMaterial.Solar;
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDreadnought = hullKey is "dreadnought";
        int strips = isDreadnought ? 4 : isCarrier ? 4 : 4;
        float asymBias = isCarrier ? 0.07f : 0.05f;
        for (int v = 0; v < strips; v++)
        {
            float t = v / MathF.Max(1f, strips - 1);
            float z = MathHelper.Lerp(-len * 0.06f, len * 0.22f, t);
            for (int side = -1; side <= 1; side += 2)
            {
                float xOut = side * hw * (isCarrier ? 0.52f : isDreadnought ? 0.58f : 0.48f) + asymBias * side;
                TriMat(w, accent,
                    new Vector3(xOut, hgt * (0.20f + t * 0.04f), z),
                    new Vector3(xOut - side * hw * 0.03f, hgt * (0.24f + t * 0.04f), z + len * 0.008f),
                    new Vector3(xOut, hgt * (0.16f + t * 0.04f), z + len * 0.012f));
            }
        }
    }

    private static void AddAsymmetricGameplayComponentBands(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDreadnought = hullKey is "dreadnought";

        for (int e = 0; e < 3; e++)
        {
            float t = e / 2f;
            float x = MathHelper.Lerp(-hw * 0.22f, hw * 0.22f, t);
            TriMat(w, engine,
                new Vector3(x - hw * 0.06f, hgt * 0.04f, -len * 0.14f), new Vector3(x + hw * 0.06f, hgt * 0.04f, -len * 0.14f),
                new Vector3(x, hgt * 0.10f, -len * 0.10f));
        }

        if (isDreadnought)
        {
            TriMat(w, weapon,
                new Vector3(-hw * 0.10f, hgt * 0.08f, len * 0.58f), new Vector3(hw * 0.10f, hgt * 0.08f, len * 0.58f),
                new Vector3(0, hgt * 0.14f, len * 0.64f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xOut = side * hw * 0.36f;
                TriMat(w, weapon,
                    new Vector3(xOut, hgt * 0.10f, len * 0.48f),
                    new Vector3(xOut - side * hw * 0.04f, hgt * 0.14f, len * 0.52f),
                    new Vector3(xOut, hgt * 0.08f, len * 0.54f));
            }
        }
        else if (isCarrier)
        {
            TriMat(w, weapon,
                new Vector3(-hw * 0.22f, hgt * 0.12f, -len * 0.02f), new Vector3(hw * 0.22f, hgt * 0.12f, -len * 0.02f),
                new Vector3(0, hgt * 0.16f, len * 0.02f));
        }
        else
        {
            TriMat(w, weapon,
                new Vector3(-hw * 0.08f, hgt * 0.10f, len * 0.52f), new Vector3(hw * 0.08f, hgt * 0.10f, len * 0.52f),
                new Vector3(0, hgt * 0.16f, len * 0.58f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xOut = side * hw * 0.34f;
                TriMat(w, weapon,
                    new Vector3(xOut, hgt * 0.12f, len * 0.40f),
                    new Vector3(xOut - side * hw * 0.04f, hgt * 0.16f, len * 0.44f),
                    new Vector3(xOut, hgt * 0.10f, len * 0.46f));
            }
        }
    }

    private static void ApplyAsymmetricMediumCombatShipDetail(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var chitin = RaceMeshWriter.HullMaterial.Truss;
        float hw = wid * 0.5f;
        float asymBias = 0.05f;
        bool isCorvette = hullKey is "corvette" or "corvette_fast";
        bool isFrigate = hullKey is "frigate" or "frigate_strike";
        bool isGunship = hullKey is "gunship" or "gunship_heavy";
        bool isBomber = hullKey is "bomber" or "bomber_heavy";
        bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";

        int chitinSegs = isBomber ? 5 : isFrigate ? 4 : isGunship ? 3 : isCorvette ? 3 : 4;
        float chitinReach = isDestroyer ? 0.28f : isFrigate ? 0.24f : isBomber ? 0.26f : isCorvette ? 0.18f : 0.16f;
        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < chitinSegs; i++)
            {
                float t = i / MathF.Max(1f, chitinSegs - 1);
                float z = MathHelper.Lerp(-len * 0.04f, len * chitinReach, t);
                float xOut = side * hw * MathHelper.Lerp(isBomber ? 0.58f : 0.72f, 0.48f, t) + asymBias * side;
                TriMat(w, accent,
                    new Vector3(xOut, hgt * (0.18f + t * 0.04f), z),
                    new Vector3(xOut - side * hw * 0.03f, hgt * (0.14f + t * 0.04f), z + len * 0.008f),
                    new Vector3(xOut, hgt * (0.12f + t * 0.03f), z + len * 0.012f));
            }
        }

        int crestBands = isDestroyer ? 2 : isFrigate ? 4 : isBomber ? 5 : isGunship ? 3 : isCorvette ? 3 : 5;
        float crestReach = isFrigate ? 0.24f : isBomber ? 0.28f : isDestroyer ? 0.30f : isCorvette ? 0.18f : 0.16f;
        for (int p = 0; p < crestBands; p++)
        {
            float t = p / MathF.Max(1f, crestBands - 1);
            float z = MathHelper.Lerp(-len * 0.06f, len * crestReach, t);
            float xSpan = hw * MathHelper.Lerp(0.32f, 0.10f, t) + asymBias;
            TriMat(w, accent,
                new Vector3(-xSpan, hgt * (0.46f + t * 0.04f), z), new Vector3(xSpan, hgt * (0.46f + t * 0.04f), z),
                new Vector3(asymBias, hgt * (0.52f + t * 0.04f), z + len * 0.006f));
        }

        if (isGunship)
        {
            float zChin = len * 0.10f;
            TriMat(w, chitin,
                new Vector3(-hw * 0.18f + asymBias, hgt * 0.02f, zChin), new Vector3(hw * 0.18f + asymBias, hgt * 0.02f, zChin),
                new Vector3(asymBias, hgt * 0.10f, zChin + len * 0.010f));
        }

        AddAsymmetricMediumCombatSubstrate(w, hullKey, hw, hgt, len);
        AddAsymmetricMediumCombatGameplayComponentBands(w, hullKey, hw, hgt, len);
    }

    /// <summary>Chitin substrate bands for Nexar medium combat � vertex luminance under team tint.</summary>
    private static void AddAsymmetricMediumCombatSubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var recess = RaceMeshWriter.HullMaterial.Radiator;
        var chitin = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engineGlow = RaceMeshWriter.HullMaterial.Engine;
        float asymBias = 0.05f;

        bool isCorvette = hullKey is "corvette" or "corvette_fast";
        bool isFrigate = hullKey is "frigate" or "frigate_strike";
        bool isGunship = hullKey is "gunship" or "gunship_heavy";
        bool isBomber = hullKey is "bomber" or "bomber_heavy";
        bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";

        int bellyBands = isCorvette ? 4 : isBomber ? 3 : isFrigate ? 5 : isDestroyer ? 5 : isGunship ? 3 : 3;
        float bellyWidth = isBomber ? 0.46f : isDestroyer ? 0.38f : isFrigate ? 0.36f : isGunship ? 0.34f : 0.32f;
        float bellyReach = isCorvette ? 0.20f : isBomber ? 0.24f : isFrigate ? 0.26f : isDestroyer ? 0.32f : isGunship ? 0.18f : 0.16f;
        for (int i = 0; i < bellyBands; i++)
        {
            float t = bellyBands > 1 ? i / (bellyBands - 1f) : 0f;
            float z = MathHelper.Lerp(-len * 0.06f, len * bellyReach, t);
            float halfW = hw * MathHelper.Lerp(bellyWidth, bellyWidth * 0.66f, t);
            TriMat(w, recess,
                new Vector3(-halfW + asymBias, hgt * 0.04f, z), new Vector3(halfW + asymBias, hgt * 0.04f, z),
                new Vector3(asymBias, hgt * (0.12f + t * 0.02f), z + len * 0.008f));
        }

        int flankBands = isDestroyer ? 4 : isFrigate ? 4 : isBomber ? 3 : isGunship ? 3 : 3;
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * hw * (isGunship ? 0.36f : isFrigate ? 0.34f : isBomber ? 0.34f : isDestroyer ? 0.32f : 0.30f) + asymBias * side;
            for (int s = 0; s < flankBands; s++)
            {
                float z = len * (0.02f + s * (isDestroyer ? 0.07f : 0.06f));
                TriMat(w, chitin,
                    new Vector3(x, hgt * 0.20f, z), new Vector3(x - side * hw * 0.03f, hgt * 0.24f, z + len * 0.01f),
                    new Vector3(x, hgt * 0.16f, z + len * 0.012f));
                if (s % 2 == 0 && (isDestroyer || isFrigate || isBomber))
                {
                    TriMat(w, accent,
                        new Vector3(x - side * hw * 0.02f, hgt * 0.18f, z + len * 0.004f),
                        new Vector3(x, hgt * 0.22f, z + len * 0.008f),
                        new Vector3(x - side * hw * 0.01f, hgt * 0.14f, z + len * 0.010f));
                }
            }
        }

        int dorsalStripes = isCorvette ? 4 : isFrigate ? 5 : isBomber ? 3 : isGunship ? 3 : isDestroyer ? 6 : 3;
        float dorsalReach = isFrigate ? 0.28f : isBomber ? 0.24f : isDestroyer ? 0.34f : isCorvette ? 0.22f : isGunship ? 0.20f : 0.18f;
        for (int d = 0; d < dorsalStripes; d++)
        {
            float t = d / MathF.Max(1f, dorsalStripes - 1);
            float z = MathHelper.Lerp(-len * 0.02f, len * dorsalReach, t);
            float xSpan = hw * MathHelper.Lerp(0.24f, 0.08f, t) + asymBias;
            TriMat(w, recess,
                new Vector3(-xSpan, hgt * (0.44f + t * 0.04f), z), new Vector3(xSpan, hgt * (0.44f + t * 0.04f), z),
                new Vector3(asymBias, hgt * (0.48f + t * 0.04f), z + len * 0.006f));
        }

        if (isDestroyer)
        {
            for (int e = 0; e < 3; e++)
            {
                float z = -len * (0.08f + e * 0.04f);
                float halfW = hw * MathHelper.Lerp(0.14f, 0.08f, e / 2f);
                TriMat(w, engineGlow,
                    new Vector3(-halfW + asymBias, hgt * 0.05f, z), new Vector3(halfW + asymBias, hgt * 0.05f, z),
                    new Vector3(asymBias, hgt * 0.12f, z + len * 0.006f));
            }
        }
        else
        {
            TriMat(w, engineGlow,
                new Vector3(-hw * 0.10f + asymBias, hgt * 0.04f, -len * 0.10f), new Vector3(hw * 0.10f + asymBias, hgt * 0.04f, -len * 0.10f),
                new Vector3(asymBias, hgt * 0.10f, -len * 0.08f));
        }
    }

    private static void AddAsymmetricMediumCombatGameplayComponentBands(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        bool isGunship = hullKey is "gunship" or "gunship_heavy";
        bool isBomber = hullKey is "bomber" or "bomber_heavy";
        bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";

        for (int e = 0; e < 3; e++)
        {
            float t = e / 2f;
            float x = MathHelper.Lerp(-hw * 0.18f, hw * 0.18f, t);
            TriMat(w, engine,
                new Vector3(x - hw * 0.06f, hgt * 0.04f, -len * 0.12f), new Vector3(x + hw * 0.06f, hgt * 0.04f, -len * 0.12f),
                new Vector3(x, hgt * 0.10f, -len * 0.08f));
        }

        if (isDestroyer)
        {
            TriMat(w, weapon,
                new Vector3(-hw * 0.10f, hgt * 0.08f, len * 0.54f), new Vector3(hw * 0.10f, hgt * 0.08f, len * 0.54f),
                new Vector3(0, hgt * 0.14f, len * 0.60f));
        }
        else if (isGunship)
        {
            TriMat(w, weapon,
                new Vector3(-hw * 0.14f, hgt * 0.02f, len * 0.32f), new Vector3(hw * 0.14f, hgt * 0.02f, len * 0.32f),
                new Vector3(0, hgt * 0.08f, len * 0.38f));
        }
        else if (isBomber)
        {
            TriMat(w, weapon,
                new Vector3(-hw * 0.20f, hgt * 0.04f, len * 0.06f), new Vector3(hw * 0.20f, hgt * 0.04f, len * 0.06f),
                new Vector3(0, hgt * 0.10f, len * 0.10f));
        }
        else
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xOut = side * hw * 0.32f;
                TriMat(w, weapon,
                    new Vector3(xOut, hgt * 0.10f, len * 0.14f),
                    new Vector3(xOut - side * hw * 0.04f, hgt * 0.14f, len * 0.18f),
                    new Vector3(xOut, hgt * 0.08f, len * 0.20f));
            }
        }
    }

    private static void ApplyOrganicShipDetail(
        RaceMeshWriter w, RaceVisualDefinition race, string hullKey, float len, float wid, float hgt)
    {
        if (hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk" or "support_repair")
        {
            ApplyOrganicUtilityShipDetail(w, hullKey, len, wid, hgt);
            return;
        }

        if (hullKey is "corvette" or "corvette_fast" or "frigate" or "frigate_strike"
            or "gunship" or "gunship_heavy" or "bomber" or "bomber_heavy"
            or "destroyer" or "destroyer_assault")
        {
            ApplyOrganicMediumCombatShipDetail(w, hullKey, len, wid, hgt);
            return;
        }

        if (hullKey is "cruiser" or "cruiser_heavy" or "carrier" or "carrier_command" or "dreadnought")
        {
            ApplyOrganicCapitalShipDetail(w, hullKey, len, wid, hgt);
            return;
        }

        var accent = RaceMeshWriter.HullMaterial.Solar;
        float hw = wid * 0.5f;

        bool isScout = hullKey is "scout" or "scout_light";
        bool isFighter = hullKey is "fighter" or "fighter_basic";
        bool isInterceptor = hullKey is "interceptor" or "interceptor_mk2";
        bool isDrone = hullKey is "drone" or "drone_swarm";
        bool isHero = hullKey is "hero" or "hero_default";
        bool isCompactCraft = isScout || isFighter || isInterceptor || isDrone;
        bool isReferenceCraft = isFighter || isHero;

        if (!isCompactCraft && !isHero)
            return;

        int veinSegs = isDrone ? 4 : isScout ? 5 : isInterceptor ? 4 : isFighter ? 4 : 5;
        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < veinSegs; i++)
            {
                float t = i / MathF.Max(1f, veinSegs - 1);
                float z = MathHelper.Lerp(-len * 0.04f, len * (isScout ? 0.20f : isDrone ? 0.18f : isInterceptor ? 0.16f : 0.14f), t);
                float xOut = side * hw * MathHelper.Lerp(isScout ? 0.62f : isDrone ? 0.48f : 0.58f, 0.40f, t);
                TriMat(w, accent,
                    new Vector3(xOut, hgt * (0.16f + t * 0.04f), z),
                    new Vector3(xOut - side * hw * 0.03f, hgt * (0.13f + t * 0.04f), z + len * 0.008f),
                    new Vector3(xOut, hgt * (0.11f + t * 0.03f), z + len * 0.012f));
            }
        }

        if (isHero)
        {
            for (int p = 0; p < 4; p++)
            {
                float t = p / 3f;
                float z = MathHelper.Lerp(-len * 0.06f, len * 0.20f, t);
                float xSpan = hw * MathHelper.Lerp(0.30f, 0.08f, t);
                TriMat(w, accent,
                    new Vector3(-xSpan, hgt * (0.58f + t * 0.06f), z), new Vector3(xSpan, hgt * (0.58f + t * 0.06f), z),
                    new Vector3(0, hgt * (0.66f + t * 0.06f), z + len * 0.006f));
            }
        }

        if (isReferenceCraft)
            AddOrganicReferenceCraftSubstrate(w, hw, hgt, len, isHero);
        else
            AddOrganicCompactCraftSubstrate(w, hullKey, hw, hgt, len);

        AddOrganicGameplayComponentBands(w, hullKey, hw, hgt, len);
    }

    /// <summary>Belly/flank membrane substrate bands for Aetherian compact craft.</summary>
    private static void AddOrganicCompactCraftSubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var membrane = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        bool isScout = hullKey is "scout" or "scout_light";
        bool isInterceptor = hullKey is "interceptor" or "interceptor_mk2";
        bool isDrone = hullKey is "drone" or "drone_swarm";

        int bellyPlates = isScout ? 3 : isDrone ? 3 : isInterceptor ? 3 : 4;
        float bellyZStart = isDrone ? -len * 0.06f : -len * 0.05f;
        float bellyZEnd = isScout ? len * 0.22f : isDrone ? len * 0.26f : isInterceptor ? len * 0.24f : len * 0.20f;
        float bellyHalfW = isScout ? 0.20f : isDrone ? 0.12f : 0.22f;
        float bellyLift = isDrone ? 0.14f : 0.17f;

        for (int i = 0; i < bellyPlates; i++)
        {
            float t = i / MathF.Max(1f, bellyPlates - 1);
            float z = MathHelper.Lerp(bellyZStart, bellyZEnd, t);
            float halfW = hw * MathHelper.Lerp(bellyHalfW, bellyHalfW * 0.72f, t);
            float recessY = hgt * (isScout && i == 0 ? 0.02f : 0.03f);
            TriMat(w, panel,
                new Vector3(-halfW, recessY, z), new Vector3(halfW, recessY, z),
                new Vector3(0, hgt * bellyLift, z + len * 0.008f));
            if (!isScout && !isInterceptor)
                TriMat(w, membrane,
                    new Vector3(-halfW * 0.82f, hgt * 0.05f, z - len * 0.004f),
                    new Vector3(halfW * 0.82f, hgt * 0.05f, z - len * 0.004f),
                    new Vector3(0, hgt * (bellyLift * 0.62f), z));
            else if (i % 2 == 0)
                TriMat(w, membrane,
                    new Vector3(-halfW * 0.82f, hgt * 0.05f, z - len * 0.004f),
                    new Vector3(halfW * 0.82f, hgt * 0.05f, z - len * 0.004f),
                    new Vector3(0, hgt * (bellyLift * 0.62f), z));
        }

        int seamCount = isInterceptor ? 3 : isScout ? 2 : 3;
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * hw * (isScout ? 0.24f : isDrone ? 0.14f : 0.28f);
            for (int s = 0; s < seamCount; s++)
            {
                float z = len * (isDrone ? (-0.02f + s * 0.10f) : (0.02f + s * 0.08f));
                TriMat(w, membrane,
                    new Vector3(x, hgt * 0.24f, z), new Vector3(x - side * hw * 0.03f, hgt * 0.28f, z + len * 0.01f),
                    new Vector3(x, hgt * 0.20f, z + len * 0.012f));
            }
        }

        int spineSegs = isScout ? 4 : isDrone ? 4 : isInterceptor ? 4 : 4;
        for (int s = 0; s < spineSegs; s++)
        {
            float t = s / MathF.Max(1f, spineSegs - 1);
            float z = MathHelper.Lerp(len * (isDrone ? -0.02f : 0.02f), len * (isScout ? 0.28f : isDrone ? 0.26f : isInterceptor ? 0.24f : 0.20f), t);
            float halfSpine = hw * (isDrone ? 0.03f : 0.04f);
            TriMat(w, accent,
                new Vector3(-halfSpine, hgt * (0.48f + t * 0.06f), z),
                new Vector3(halfSpine, hgt * (0.48f + t * 0.06f), z),
                new Vector3(0, hgt * (0.54f + t * 0.06f), z + len * 0.005f));
            if (s % 2 == 0)
                TriMat(w, membrane,
                    new Vector3(-halfSpine * 1.4f, hgt * (0.44f + t * 0.04f), z - len * 0.003f),
                    new Vector3(halfSpine * 1.4f, hgt * (0.44f + t * 0.04f), z - len * 0.003f),
                    new Vector3(0, hgt * (0.50f + t * 0.05f), z));
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float xLead = side * hw * (isScout ? 0.42f : isDrone ? 0.20f : 0.36f);
            int edgeSegs = isScout ? 3 : isDrone ? 3 : isInterceptor ? 3 : 3;
            for (int e = 0; e < edgeSegs; e++)
            {
                float z = MathHelper.Lerp(len * (isScout ? 0.10f : 0.08f), -len * 0.02f, e / MathF.Max(1f, edgeSegs - 1));
                TriMat(w, accent,
                    new Vector3(xLead, hgt * (0.18f + e * 0.012f), z),
                    new Vector3(xLead - side * hw * 0.03f, hgt * (0.16f + e * 0.012f), z + len * 0.008f),
                    new Vector3(xLead, hgt * (0.14f + e * 0.012f), z + len * 0.012f));
            }
        }
    }

    /// <summary>Belly/flank membrane substrate bands for Aetherian fighter/hero reference craft.</summary>
    private static void AddOrganicReferenceCraftSubstrate(RaceMeshWriter w, float hw, float hgt, float len, bool isHero)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var membrane = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;

        for (int i = 0; i < (isHero ? 5 : 4); i++)
        {
            float t = i / MathF.Max(1f, (isHero ? 4 : 3));
            float z = MathHelper.Lerp(-len * 0.06f, len * (isHero ? 0.24f : 0.20f), t);
            float halfW = hw * MathHelper.Lerp(0.28f, 0.18f, t);
            TriMat(w, panel,
                new Vector3(-halfW, hgt * 0.03f, z), new Vector3(halfW, hgt * 0.03f, z),
                new Vector3(0, hgt * 0.18f, z + len * 0.008f));
            TriMat(w, membrane,
                new Vector3(-halfW * 0.80f, hgt * 0.05f, z - len * 0.004f),
                new Vector3(halfW * 0.80f, hgt * 0.05f, z - len * 0.004f),
                new Vector3(0, hgt * 0.11f, z));
            if (i % 2 == 0)
                TriMat(w, panel,
                    new Vector3(-halfW * 0.70f, hgt * 0.04f, z + len * 0.003f),
                    new Vector3(halfW * 0.70f, hgt * 0.04f, z + len * 0.003f),
                    new Vector3(0, hgt * 0.14f, z + len * 0.006f));
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * hw * (isHero ? 0.34f : 0.30f);
            for (int s = 0; s < 4; s++)
            {
                float z = len * (0.02f + s * 0.06f);
                TriMat(w, membrane,
                    new Vector3(x, hgt * 0.26f, z), new Vector3(x - side * hw * 0.03f, hgt * 0.30f, z + len * 0.01f),
                    new Vector3(x, hgt * 0.22f, z + len * 0.012f));
            }
        }

        int spineSegs = isHero ? 6 : 5;
        for (int s = 0; s < spineSegs; s++)
        {
            float t = s / MathF.Max(1f, spineSegs - 1);
            float z = MathHelper.Lerp(len * 0.02f, len * (isHero ? 0.26f : 0.18f), t);
            TriMat(w, accent,
                new Vector3(-hw * 0.05f, hgt * (0.50f + t * 0.06f), z), new Vector3(hw * 0.05f, hgt * (0.50f + t * 0.06f), z),
                new Vector3(0, hgt * (0.56f + t * 0.06f), z + len * 0.005f));
            if (isHero && s % 2 == 0)
                TriMat(w, membrane,
                    new Vector3(-hw * 0.07f, hgt * (0.46f + t * 0.04f), z - len * 0.003f),
                    new Vector3(hw * 0.07f, hgt * (0.46f + t * 0.04f), z - len * 0.003f),
                    new Vector3(0, hgt * (0.52f + t * 0.05f), z));
        }
    }

    /// <summary>Gameplay-readable engine/weapon/shield markers on Aetherian organic hulls.</summary>
    private static void AddOrganicGameplayComponentBands(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;

        if (hullKey is "scout" or "scout_light")
        {
            TriMat(w, shield,
                new Vector3(-hw * 0.05f, hgt * 0.74f, len * 0.08f), new Vector3(hw * 0.05f, hgt * 0.74f, len * 0.08f),
                new Vector3(0, hgt * 0.80f, len * 0.10f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xPod = side * hw * 0.52f;
                TriMat(w, weapon,
                    new Vector3(xPod, hgt * 0.14f, len * 0.06f),
                    new Vector3(xPod - side * hw * 0.04f, hgt * 0.10f, len * 0.09f),
                    new Vector3(xPod, hgt * 0.08f, len * 0.08f));
            }
            TriMat(w, engine,
                new Vector3(-hw * 0.06f, hgt * 0.06f, -len * 0.08f), new Vector3(hw * 0.06f, hgt * 0.06f, -len * 0.08f),
                new Vector3(0, hgt * 0.12f, -len * 0.06f));
            return;
        }

        if (hullKey is "interceptor" or "interceptor_mk2")
        {
            TriMat(w, weapon,
                new Vector3(-hw * 0.08f, hgt * 0.10f, len * 0.32f), new Vector3(hw * 0.08f, hgt * 0.10f, len * 0.32f),
                new Vector3(0, hgt * 0.16f, len * 0.36f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xPod = side * hw * 0.46f;
                TriMat(w, weapon,
                    new Vector3(xPod, hgt * 0.12f, len * 0.04f),
                    new Vector3(xPod - side * hw * 0.04f, hgt * 0.08f, len * 0.07f),
                    new Vector3(xPod, hgt * 0.06f, len * 0.06f));
            }
            TriMat(w, engine,
                new Vector3(-hw * 0.10f, hgt * 0.06f, -len * 0.06f), new Vector3(hw * 0.10f, hgt * 0.06f, -len * 0.06f),
                new Vector3(0, hgt * 0.12f, -len * 0.04f));
            TriMat(w, shield,
                new Vector3(-hw * 0.05f, hgt * 0.72f, len * 0.06f), new Vector3(hw * 0.05f, hgt * 0.72f, len * 0.06f),
                new Vector3(0, hgt * 0.78f, len * 0.08f));
            return;
        }

        if (hullKey is "drone" or "drone_swarm")
        {
            TriMat(w, engine,
                new Vector3(-hw * 0.08f, hgt * 0.06f, -len * 0.18f), new Vector3(hw * 0.08f, hgt * 0.06f, -len * 0.18f),
                new Vector3(0, hgt * 0.12f, -len * 0.14f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xPod = side * hw * 0.28f;
                TriMat(w, weapon,
                    new Vector3(xPod, hgt * 0.10f, len * 0.10f),
                    new Vector3(xPod - side * hw * 0.03f, hgt * 0.06f, len * 0.12f),
                    new Vector3(xPod, hgt * 0.04f, len * 0.11f));
            }
            TriMat(w, shield,
                new Vector3(-hw * 0.05f, hgt * 0.70f, len * 0.06f), new Vector3(hw * 0.05f, hgt * 0.70f, len * 0.06f),
                new Vector3(0, hgt * 0.76f, len * 0.08f));
            return;
        }

        if (hullKey is "fighter_basic" or "hero_default")
        {
            TriMat(w, engine,
                new Vector3(-hw * 0.12f, hgt * 0.06f, -len * 0.08f), new Vector3(hw * 0.12f, hgt * 0.06f, -len * 0.08f),
                new Vector3(0, hgt * 0.12f, -len * 0.06f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xPod = side * hw * (hullKey is "hero_default" ? 0.50f : 0.44f);
                TriMat(w, weapon,
                    new Vector3(xPod, hgt * 0.10f, len * 0.04f),
                    new Vector3(xPod - side * hw * 0.04f, hgt * 0.06f, len * 0.07f),
                    new Vector3(xPod, hgt * 0.04f, len * 0.06f));
            }
            TriMat(w, shield,
                new Vector3(-hw * 0.06f, hgt * (hullKey is "hero_default" ? 0.76f : 0.72f), len * 0.08f),
                new Vector3(hw * 0.06f, hgt * (hullKey is "hero_default" ? 0.76f : 0.72f), len * 0.08f),
                new Vector3(0, hgt * (hullKey is "hero_default" ? 0.82f : 0.78f), len * 0.10f));
            return;
        }

        if (hullKey is "corvette" or "corvette_fast")
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xPod = side * hw * 0.40f;
                TriMat(w, weapon,
                    new Vector3(xPod, hgt * 0.12f, len * 0.06f),
                    new Vector3(xPod - side * hw * 0.04f, hgt * 0.08f, len * 0.09f),
                    new Vector3(xPod, hgt * 0.06f, len * 0.08f));
            }
            TriMat(w, engine,
                new Vector3(-hw * 0.10f, hgt * 0.06f, -len * 0.08f), new Vector3(hw * 0.10f, hgt * 0.06f, -len * 0.08f),
                new Vector3(0, hgt * 0.12f, -len * 0.06f));
            return;
        }

        if (hullKey is "frigate" or "frigate_strike")
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xPod = side * hw * 0.36f;
                TriMat(w, weapon,
                    new Vector3(xPod, hgt * 0.14f, len * 0.10f),
                    new Vector3(xPod - side * hw * 0.04f, hgt * 0.10f, len * 0.13f),
                    new Vector3(xPod, hgt * 0.08f, len * 0.12f));
            }
            TriMat(w, weapon,
                new Vector3(-hw * 0.08f, hgt * 0.08f, len * 0.22f), new Vector3(hw * 0.08f, hgt * 0.08f, len * 0.22f),
                new Vector3(0, hgt * 0.14f, len * 0.26f));
            TriMat(w, engine,
                new Vector3(-hw * 0.12f, hgt * 0.06f, -len * 0.08f), new Vector3(hw * 0.12f, hgt * 0.06f, -len * 0.08f),
                new Vector3(0, hgt * 0.12f, -len * 0.06f));
            return;
        }

        if (hullKey is "bomber" or "bomber_heavy")
        {
            TriMat(w, weapon,
                new Vector3(-hw * 0.24f, hgt * 0.04f, len * 0.04f), new Vector3(hw * 0.24f, hgt * 0.04f, len * 0.04f),
                new Vector3(0, hgt * 0.10f, len * 0.08f));
            TriMat(w, engine,
                new Vector3(-hw * 0.14f, hgt * 0.04f, -len * 0.10f), new Vector3(hw * 0.14f, hgt * 0.04f, -len * 0.10f),
                new Vector3(0, hgt * 0.10f, -len * 0.08f));
            return;
        }

        if (hullKey is "gunship" or "gunship_heavy")
        {
            TriMat(w, weapon,
                new Vector3(-hw * 0.12f, hgt * 0.02f, len * 0.18f), new Vector3(hw * 0.12f, hgt * 0.02f, len * 0.18f),
                new Vector3(0, hgt * 0.08f, len * 0.24f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xPod = side * hw * 0.38f;
                TriMat(w, weapon,
                    new Vector3(xPod, hgt * 0.08f, len * 0.08f),
                    new Vector3(xPod - side * hw * 0.04f, hgt * 0.04f, len * 0.11f),
                    new Vector3(xPod, hgt * 0.02f, len * 0.10f));
            }
            TriMat(w, engine,
                new Vector3(-hw * 0.12f, hgt * 0.06f, -len * 0.08f), new Vector3(hw * 0.12f, hgt * 0.06f, -len * 0.08f),
                new Vector3(0, hgt * 0.12f, -len * 0.06f));
            TriMat(w, shield,
                new Vector3(-hw * 0.06f, hgt * 0.46f, len * 0.06f), new Vector3(hw * 0.06f, hgt * 0.46f, len * 0.06f),
                new Vector3(0, hgt * 0.52f, len * 0.10f));
            return;
        }

        if (hullKey is "destroyer" or "destroyer_assault")
        {
            TriMat(w, weapon,
                new Vector3(-hw * 0.08f, hgt * 0.10f, len * 0.50f), new Vector3(hw * 0.08f, hgt * 0.10f, len * 0.50f),
                new Vector3(0, hgt * 0.16f, len * 0.56f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xPod = side * hw * 0.34f;
                TriMat(w, weapon,
                    new Vector3(xPod, hgt * 0.14f, len * 0.12f),
                    new Vector3(xPod - side * hw * 0.04f, hgt * 0.10f, len * 0.15f),
                    new Vector3(xPod, hgt * 0.08f, len * 0.14f));
            }
            TriMat(w, engine,
                new Vector3(-hw * 0.14f, hgt * 0.06f, -len * 0.10f), new Vector3(hw * 0.14f, hgt * 0.06f, -len * 0.10f),
                new Vector3(0, hgt * 0.12f, -len * 0.08f));
            return;
        }

        if (hullKey is "cruiser" or "cruiser_heavy")
        {
            TriMat(w, engine,
                new Vector3(-hw * 0.12f, hgt * 0.08f, -len * 0.22f), new Vector3(hw * 0.12f, hgt * 0.08f, -len * 0.22f),
                new Vector3(0, hgt * 0.14f, -len * 0.18f));
            TriMat(w, weapon,
                new Vector3(-hw * 0.08f, hgt * 0.18f, len * 0.58f), new Vector3(hw * 0.08f, hgt * 0.18f, len * 0.58f),
                new Vector3(0, hgt * 0.24f, len * 0.64f));
            TriMat(w, shield,
                new Vector3(-hw * 0.07f, hgt * 0.76f, len * 0.08f), new Vector3(hw * 0.07f, hgt * 0.76f, len * 0.08f),
                new Vector3(0, hgt * 0.82f, len * 0.11f));
            return;
        }

        if (hullKey is "carrier" or "carrier_command")
        {
            TriMat(w, engine,
                new Vector3(-hw * 0.12f, hgt * 0.08f, -len * 0.22f), new Vector3(hw * 0.12f, hgt * 0.08f, -len * 0.22f),
                new Vector3(0, hgt * 0.14f, -len * 0.18f));
            TriMat(w, weapon,
                new Vector3(-hw * 0.30f, hgt * 0.28f, len * 0.04f), new Vector3(hw * 0.30f, hgt * 0.28f, len * 0.04f),
                new Vector3(0, hgt * 0.32f, len * 0.08f));
            TriMat(w, weapon,
                new Vector3(-hw * 0.20f, hgt * 0.12f, -len * 0.04f), new Vector3(hw * 0.20f, hgt * 0.12f, -len * 0.04f),
                new Vector3(0, hgt * 0.16f, len * 0.02f));
            TriMat(w, shield,
                new Vector3(-hw * 0.08f, hgt * 0.72f, len * 0.06f), new Vector3(hw * 0.08f, hgt * 0.72f, len * 0.06f),
                new Vector3(0, hgt * 0.78f, len * 0.09f));
            return;
        }

        if (hullKey is "dreadnought")
        {
            TriMat(w, engine,
                new Vector3(-hw * 0.14f, hgt * 0.08f, -len * 0.24f), new Vector3(hw * 0.14f, hgt * 0.08f, -len * 0.24f),
                new Vector3(0, hgt * 0.14f, -len * 0.20f));
            TriMat(w, weapon,
                new Vector3(-hw * 0.10f, hgt * 0.16f, len * 0.66f), new Vector3(hw * 0.10f, hgt * 0.16f, len * 0.66f),
                new Vector3(0, hgt * 0.22f, len * 0.72f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xPod = side * hw * 0.42f;
                TriMat(w, weapon,
                    new Vector3(xPod, hgt * 0.14f, len * 0.58f),
                    new Vector3(xPod - side * hw * 0.04f, hgt * 0.10f, len * 0.62f),
                    new Vector3(xPod, hgt * 0.08f, len * 0.60f));
            }
            TriMat(w, shield,
                new Vector3(-hw * 0.08f, hgt * 0.74f, len * 0.08f), new Vector3(hw * 0.08f, hgt * 0.74f, len * 0.08f),
                new Vector3(0, hgt * 0.80f, len * 0.11f));
        }
    }

    private static void ApplyOrganicMediumCombatShipDetail(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        var vein = RaceMeshWriter.HullMaterial.Solar;
        var fold = RaceMeshWriter.HullMaterial.Truss;
        float hw = wid * 0.5f;
        bool isCorvette = hullKey is "corvette" or "corvette_fast";
        bool isFrigate = hullKey is "frigate" or "frigate_strike";
        bool isGunship = hullKey is "gunship" or "gunship_heavy";
        bool isBomber = hullKey is "bomber" or "bomber_heavy";
        bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";

        // Loop-10 tri trim: destroyer_assault geometry =180 sweet max.
        int veinSegs = isDestroyer ? 2 : isBomber ? 5 : isFrigate ? 4 : isGunship ? 3 : isCorvette ? 3 : 4;
        float veinReach = isDestroyer ? 0.28f : isFrigate ? 0.24f : isBomber ? 0.26f : isCorvette ? 0.18f : 0.16f;
        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < veinSegs; i++)
            {
                float t = i / MathF.Max(1f, veinSegs - 1);
                float z = MathHelper.Lerp(-len * 0.04f, len * veinReach, t);
                float xOut = side * hw * MathHelper.Lerp(isBomber ? 0.58f : 0.72f, 0.48f, t);
                TriMat(w, vein,
                    new Vector3(xOut, hgt * (0.18f + t * 0.04f), z),
                    new Vector3(xOut - side * hw * 0.03f, hgt * (0.14f + t * 0.04f), z + len * 0.008f),
                    new Vector3(xOut, hgt * (0.12f + t * 0.03f), z + len * 0.012f));
            }
        }

        int crestBands = isFrigate ? 4 : isBomber ? 5 : isGunship ? 3 : isCorvette ? 3 : 5;
        float crestReach = isFrigate ? 0.24f : isBomber ? 0.28f : isDestroyer ? 0.30f : isCorvette ? 0.18f : 0.16f;
        for (int p = 0; p < crestBands; p++)
        {
            float t = p / MathF.Max(1f, crestBands - 1);
            float z = MathHelper.Lerp(-len * 0.06f, len * crestReach, t);
            float xSpan = hw * MathHelper.Lerp(0.32f, 0.10f, t);
            TriMat(w, vein,
                new Vector3(-xSpan, hgt * (0.46f + t * 0.04f), z), new Vector3(xSpan, hgt * (0.46f + t * 0.04f), z),
                new Vector3(0, hgt * (0.52f + t * 0.04f), z + len * 0.006f));
        }

        if (isGunship)
        {
            float zChin = len * 0.10f;
            TriMat(w, fold,
                new Vector3(-hw * 0.18f, hgt * 0.02f, zChin), new Vector3(hw * 0.18f, hgt * 0.02f, zChin),
                new Vector3(0, hgt * 0.10f, zChin + len * 0.010f));
        }

        AddOrganicMediumCombatSubstrate(w, hullKey, hw, hgt, len);
        AddOrganicGameplayComponentBands(w, hullKey, hw, hgt, len);
    }

    /// <summary>Membrane substrate bands for Aetherian medium combat — vertex luminance under team tint.</summary>
    private static void AddOrganicMediumCombatSubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var recess = RaceMeshWriter.HullMaterial.Radiator;
        var fold = RaceMeshWriter.HullMaterial.Truss;
        var vein = RaceMeshWriter.HullMaterial.Solar;
        var engineGlow = RaceMeshWriter.HullMaterial.Engine;

        bool isCorvette = hullKey is "corvette" or "corvette_fast";
        bool isFrigate = hullKey is "frigate" or "frigate_strike";
        bool isGunship = hullKey is "gunship" or "gunship_heavy";
        bool isBomber = hullKey is "bomber" or "bomber_heavy";
        bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";

        int bellyBands = isDestroyer ? 2 : isCorvette ? 4 : isBomber ? 3 : isFrigate ? 5 : isGunship ? 3 : 3;
        float bellyWidth = isBomber ? 0.46f : isDestroyer ? 0.38f : isFrigate ? 0.36f : isGunship ? 0.34f : 0.32f;
        float bellyReach = isCorvette ? 0.20f : isBomber ? 0.24f : isFrigate ? 0.26f : isDestroyer ? 0.32f : isGunship ? 0.18f : 0.16f;
        for (int i = 0; i < bellyBands; i++)
        {
            float t = bellyBands > 1 ? i / (bellyBands - 1f) : 0f;
            float z = MathHelper.Lerp(-len * 0.06f, len * bellyReach, t);
            float halfW = hw * MathHelper.Lerp(bellyWidth, bellyWidth * 0.66f, t);
            TriMat(w, recess,
                new Vector3(-halfW, hgt * 0.04f, z), new Vector3(halfW, hgt * 0.04f, z),
                new Vector3(0, hgt * (0.12f + t * 0.02f), z + len * 0.008f));
        }

        int flankBands = isDestroyer ? 4 : isFrigate ? 4 : isBomber ? 3 : isGunship ? 3 : 3;
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * hw * (isGunship ? 0.36f : isFrigate ? 0.34f : isBomber ? 0.34f : isDestroyer ? 0.32f : 0.30f);
            for (int s = 0; s < flankBands; s++)
            {
                float z = len * (0.02f + s * (isDestroyer ? 0.07f : 0.06f));
                TriMat(w, fold,
                    new Vector3(x, hgt * 0.20f, z), new Vector3(x - side * hw * 0.03f, hgt * 0.24f, z + len * 0.01f),
                    new Vector3(x, hgt * 0.16f, z + len * 0.012f));
                if (s % 2 == 0 && (isDestroyer || isFrigate || isBomber))
                {
                    TriMat(w, vein,
                        new Vector3(x - side * hw * 0.02f, hgt * 0.18f, z + len * 0.004f),
                        new Vector3(x, hgt * 0.22f, z + len * 0.008f),
                        new Vector3(x - side * hw * 0.01f, hgt * 0.14f, z + len * 0.010f));
                }
            }
        }

        if (isBomber)
        {
            for (int b = 0; b < 2; b++)
            {
                float z = len * (0.06f + b * 0.08f);
                float halfW = hw * MathHelper.Lerp(0.28f, 0.16f, b);
                TriMat(w, b == 0 ? vein : fold,
                    new Vector3(-halfW, hgt * 0.05f, z), new Vector3(halfW, hgt * 0.05f, z),
                    new Vector3(0, hgt * 0.10f, z + len * 0.008f));
            }
        }

        int dorsalStripes = isCorvette ? 4 : isFrigate ? 5 : isBomber ? 3 : isGunship ? 3 : isDestroyer ? 6 : 3;
        float dorsalReach = isFrigate ? 0.28f : isBomber ? 0.24f : isDestroyer ? 0.34f : isCorvette ? 0.22f : isGunship ? 0.20f : 0.18f;
        for (int d = 0; d < dorsalStripes; d++)
        {
            float t = d / MathF.Max(1f, dorsalStripes - 1);
            float z = MathHelper.Lerp(-len * 0.02f, len * dorsalReach, t);
            float xSpan = hw * MathHelper.Lerp(0.24f, 0.08f, t);
            TriMat(w, recess,
                new Vector3(-xSpan, hgt * (0.44f + t * 0.04f), z), new Vector3(xSpan, hgt * (0.44f + t * 0.04f), z),
                new Vector3(0, hgt * (0.48f + t * 0.04f), z + len * 0.006f));
            if (d % 2 == 0)
            {
                TriMat(w, vein,
                    new Vector3(-xSpan * 0.72f, hgt * (0.46f + t * 0.04f), z + len * 0.003f),
                    new Vector3(xSpan * 0.72f, hgt * (0.46f + t * 0.04f), z + len * 0.003f),
                    new Vector3(0, hgt * (0.50f + t * 0.04f), z + len * 0.007f));
            }
        }

        if (isDestroyer)
        {
            for (int e = 0; e < 3; e++)
            {
                float z = -len * (0.08f + e * 0.04f);
                float halfW = hw * MathHelper.Lerp(0.14f, 0.08f, e / 2f);
                TriMat(w, engineGlow,
                    new Vector3(-halfW, hgt * 0.05f, z), new Vector3(halfW, hgt * 0.05f, z),
                    new Vector3(0, hgt * 0.12f, z + len * 0.006f));
            }
        }
        else
        {
            TriMat(w, engineGlow,
                new Vector3(-hw * 0.10f, hgt * 0.04f, -len * 0.10f), new Vector3(hw * 0.10f, hgt * 0.04f, -len * 0.10f),
                new Vector3(0, hgt * 0.10f, -len * 0.08f));
        }
    }

    private static void ApplyOrganicCapitalShipDetail(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float hw = wid * 0.5f;
        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDreadnought = hullKey is "dreadnought";

        int dorsalSegs = isDreadnought ? 4 : isCruiser ? 6 : 5;
        float dorsalBase = isCarrier ? 0.44f : 0.54f;
        float dorsalRise = isCarrier ? 0.05f : 0.06f;
        for (int i = 0; i < dorsalSegs; i++)
        {
            float t = i / MathF.Max(1f, dorsalSegs - 1);
            float z = MathHelper.Lerp(len * 0.06f, len * (isCarrier ? 0.68f : 0.62f), t);
            TriMat(w, accent,
                new Vector3(0, hgt * (dorsalBase + t * dorsalRise), z),
                new Vector3(-hw * 0.10f, hgt * (dorsalBase - 0.10f + t * 0.03f), z - len * 0.02f),
                new Vector3(hw * 0.10f, hgt * (dorsalBase - 0.10f + t * 0.03f), z - len * 0.02f));
        }

        AddOrganicCapitalSubstrate(w, hullKey, hw, hgt, len);
        if (isCarrier)
            AddOrganicCarrierDeckMembranes(w, hw, hgt, len);
        AddOrganicAccentRingVeins(w, hullKey, hw, hgt, len);
        AddOrganicGameplayComponentBands(w, hullKey, hw, hgt, len);
    }

    /// <summary>Belly/flank organic substrate bands for Aetherian capital hulls.</summary>
    private static void AddOrganicCapitalSubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var fold = RaceMeshWriter.HullMaterial.Truss;
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        bool isDreadnought = hullKey is "dreadnought";
        int bellyPlates = isDreadnought ? 5 : isCarrier ? 5 : 4;

        for (int i = 0; i < bellyPlates; i++)
        {
            float t = bellyPlates == 1 ? 0.5f : i / (float)(bellyPlates - 1);
            float z = MathHelper.Lerp(-len * 0.12f, len * (isCarrier ? 0.28f : 0.22f), t);
            float halfW = hw * MathHelper.Lerp(
                isCarrier ? 0.38f : isDreadnought ? 0.34f : 0.32f, 0.18f, t);
            TriMat(w, panel,
                new Vector3(-halfW, hgt * 0.05f, z), new Vector3(halfW, hgt * 0.05f, z),
                new Vector3(0, hgt * 0.14f, z + len * 0.010f));
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float xOut = side * hw * (isCarrier ? 0.40f : isDreadnought ? 0.48f : 0.42f);
            int flankBands = isDreadnought ? 4 : 3;
            for (int f = 0; f < flankBands; f++)
            {
                float t = f / MathF.Max(1f, flankBands - 1);
                float z = MathHelper.Lerp(-len * 0.08f, len * 0.18f, t);
                TriMat(w, panel,
                    new Vector3(xOut, hgt * 0.12f, z),
                    new Vector3(xOut - side * hw * 0.04f, hgt * 0.18f, z + len * 0.008f),
                    new Vector3(xOut, hgt * 0.08f, z + len * 0.012f));
            }
        }

        if (isCruiser)
        {
            for (int k = 0; k < 4; k++)
            {
                float z = MathHelper.Lerp(len * 0.02f, len * 0.24f, k / 3f);
                TriMat(w, fold,
                    new Vector3(-hw * 0.14f, hgt * 0.32f, z), new Vector3(hw * 0.14f, hgt * 0.32f, z),
                    new Vector3(0, hgt * 0.36f, z + len * 0.006f));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float xOut = side * hw * 0.36f;
                for (int f = 0; f < 2; f++)
                {
                    float z = len * (0.08f + f * 0.10f);
                    TriMat(w, panel,
                        new Vector3(xOut, hgt * 0.14f, z),
                        new Vector3(xOut - side * hw * 0.04f, hgt * 0.18f, z + len * 0.008f),
                        new Vector3(xOut, hgt * 0.10f, z + len * 0.012f));
                }
            }
        }

        if (isDreadnought)
        {
            for (int k = 0; k < 3; k++)
            {
                float z = MathHelper.Lerp(len * 0.46f, len * 0.64f, k / 2f);
                TriMat(w, fold,
                    new Vector3(-hw * 0.12f, hgt * 0.28f, z), new Vector3(hw * 0.12f, hgt * 0.28f, z),
                    new Vector3(0, hgt * 0.34f, z + len * 0.008f));
            }
        }
    }

    private static void AddOrganicCarrierDeckMembranes(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var recess = RaceMeshWriter.HullMaterial.Truss;
        float deckZ = MathHelper.Lerp(-len * 0.16f, len * 0.08f, 0.5f);
        TriMat(w, panel,
            new Vector3(-hw * 0.46f, hgt * 0.20f, deckZ), new Vector3(hw * 0.46f, hgt * 0.20f, deckZ),
            new Vector3(0, hgt * 0.24f, deckZ + len * 0.006f));
        TriMat(w, panel,
            new Vector3(-hw * 0.24f, hgt * 0.10f, -len * 0.06f), new Vector3(hw * 0.24f, hgt * 0.10f, -len * 0.06f),
            new Vector3(0, hgt * 0.14f, -len * 0.02f));
        for (int h = 0; h < 3; h++)
        {
            float z = len * (0.06f - h * 0.08f);
            TriMat(w, recess,
                new Vector3(-hw * 0.30f, hgt * 0.22f, z), new Vector3(hw * 0.30f, hgt * 0.22f, z),
                new Vector3(0, hgt * 0.26f, z + len * 0.008f));
        }
        for (int r = 0; r < 2; r++)
        {
            float z = len * (-0.04f + r * 0.10f);
            TriMat(w, panel,
                new Vector3(-hw * 0.18f, hgt * 0.08f, z), new Vector3(hw * 0.18f, hgt * 0.08f, z),
                new Vector3(0, hgt * 0.12f, z + len * 0.006f));
        }
    }

    private static void AddOrganicAccentRingVeins(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var accent = RaceMeshWriter.HullMaterial.Solar;
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDreadnought = hullKey is "dreadnought";
        int veins = isDreadnought ? 4 : isCarrier ? 4 : 4;
        for (int v = 0; v < veins; v++)
        {
            float t = v / MathF.Max(1f, veins - 1);
            float z = MathHelper.Lerp(-len * 0.06f, len * 0.22f, t);
            for (int side = -1; side <= 1; side += 2)
            {
                float xOut = side * hw * (isCarrier ? 0.52f : isDreadnought ? 0.58f : 0.48f);
                TriMat(w, accent,
                    new Vector3(xOut, hgt * (0.20f + t * 0.04f), z),
                    new Vector3(xOut - side * hw * 0.03f, hgt * (0.24f + t * 0.04f), z + len * 0.008f),
                    new Vector3(xOut, hgt * (0.16f + t * 0.04f), z + len * 0.012f));
            }
        }
    }

    private static void ApplyTrussShipDetail(
        RaceMeshWriter w, RaceVisualDefinition race, string hullKey, float len, float wid, float hgt)
    {
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        var engineMat = RaceMeshWriter.HullMaterial.Engine;
        float hw = wid * 0.5f;

        bool isUtility = hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk" or "support_repair";
        if (isUtility)
        {
            float widthScale = hullKey is "freighter_bulk" ? 0.90f : hullKey is "miner_eva" ? 0.92f : 0.96f;
            hw *= widthScale;
            bool triBudgetTight = hullKey is "freighter_bulk" or "transport_cargo";
            bool isMiner = hullKey is "miner_basic" or "miner_eva" or "miner_tractor";
            bool isSupport = hullKey is "support_repair";
            int lateralSegs = hullKey is "freighter_bulk" ? 1 : triBudgetTight ? 2 : isMiner ? 3 : isSupport ? 3 : 4;
            int dorsalSegs = hullKey is "freighter_bulk" ? 1 : triBudgetTight ? 2 : isMiner ? 3 : isSupport ? 3 : 4;
            int bellySegs = triBudgetTight ? 2 : isMiner ? 3 : 3;

            for (int side = -1; side <= 1; side += 2)
            {
                float xOut = side * hw * (hullKey is "freighter_bulk" ? 0.52f : hullKey is "transport_cargo" ? 0.50f : 0.60f);
                for (int i = 0; i < lateralSegs; i++)
                {
                    float t = i / MathF.Max(1f, lateralSegs - 1);
                    float z = MathHelper.Lerp(-len * 0.04f, len * (hullKey is "freighter_bulk" ? 0.36f : triBudgetTight ? 0.34f : isMiner ? 0.22f : hullKey is "support_repair" ? 0.18f : 0.14f), t);
                    TriMat(w, frame,
                        new Vector3(xOut, hgt * (0.14f + t * 0.05f), z),
                        new Vector3(xOut - side * hw * 0.04f, hgt * (0.11f + t * 0.05f), z + len * 0.010f),
                        new Vector3(xOut, hgt * (0.09f + t * 0.04f), z + len * 0.014f));
                }
            }

            for (int p = 0; p < dorsalSegs; p++)
            {
                float t = p / MathF.Max(1f, dorsalSegs - 1);
                float z = MathHelper.Lerp(-len * 0.06f, len * (hullKey is "freighter_bulk" ? 0.40f : triBudgetTight ? 0.38f : isMiner ? 0.24f : hullKey is "support_repair" ? 0.20f : 0.18f), t);
                float xSpan = hw * MathHelper.Lerp(0.36f, 0.10f, t);
                TriMat(w, accent,
                    new Vector3(-xSpan, hgt * (0.34f + t * 0.05f), z), new Vector3(xSpan, hgt * (0.34f + t * 0.05f), z),
                    new Vector3(0, hgt * (0.40f + t * 0.05f), z + len * 0.008f));
            }

            for (int b = 0; b < bellySegs; b++)
            {
                float z = MathHelper.Lerp(-len * 0.06f, len * (triBudgetTight ? 0.20f : isMiner ? 0.18f : 0.16f), b / MathF.Max(1f, bellySegs - 1));
                TriMat(w, panel,
                    new Vector3(-hw * 0.24f, hgt * 0.36f, z), new Vector3(hw * 0.24f, hgt * 0.36f, z),
                    new Vector3(0, hgt * 0.42f, z + len * 0.010f));
            }

            if (isMiner)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    float xTip = side * hw * (hullKey is "miner_basic" ? 0.78f : 0.68f);
                    float zTip = len * (hullKey is "miner_basic" ? 0.61f : hullKey is "miner_tractor" ? 0.18f : 0.14f);
                    TriMat(w, accent,
                        new Vector3(xTip, hgt * 0.22f, zTip),
                        new Vector3(xTip + side * hw * 0.04f, hgt * 0.28f, zTip + len * 0.02f),
                        new Vector3(xTip, hgt * 0.16f, zTip + len * 0.01f));
                }
            }

            if (hullKey is "support_repair")
            {
                for (int a = 0; a < 2; a++)
                {
                    float z = len * (0.06f + a * 0.060f);
                    TriMat(w, accent,
                        new Vector3(-hw * 0.04f, hgt * (0.74f + a * 0.04f), z),
                        new Vector3(hw * 0.04f, hgt * (0.74f + a * 0.04f), z),
                        new Vector3(0, hgt * (0.82f + a * 0.04f), z + len * 0.008f));
                }
                TriMat(w, accent,
                    new Vector3(-hw * 0.03f, hgt * 1.04f, len * 0.16f),
                    new Vector3(hw * 0.03f, hgt * 1.04f, len * 0.16f),
                    new Vector3(0, hgt * 1.12f, len * 0.18f));
                for (int side = -1; side <= 1; side += 2)
                {
                    float xBoom = side * hw * 0.72f;
                    TriMat(w, panel,
                        new Vector3(xBoom, hgt * 0.38f, len * 0.08f),
                        new Vector3(xBoom + side * hw * 0.03f, hgt * 0.44f, len * 0.12f),
                        new Vector3(xBoom, hgt * 0.34f, len * 0.14f));
                    TriMat(w, accent,
                        new Vector3(xBoom, hgt * 0.60f, len * 0.14f),
                        new Vector3(xBoom + side * hw * 0.05f, hgt * 0.66f, len * 0.16f),
                        new Vector3(xBoom, hgt * 0.54f, len * 0.18f));
                    TriMat(w, panel,
                        new Vector3(xBoom, hgt * 0.30f, len * 0.10f),
                        new Vector3(xBoom + side * hw * 0.04f, hgt * 0.34f, len * 0.12f),
                        new Vector3(xBoom, hgt * 0.26f, len * 0.14f));
                    TriMat(w, RaceMeshWriter.HullMaterial.Weapon,
                        new Vector3(xBoom, hgt * 0.58f, len * 0.10f),
                        new Vector3(xBoom + side * hw * 0.05f, hgt * 0.64f, len * 0.12f),
                        new Vector3(xBoom, hgt * 0.52f, len * 0.14f));
                    TriMat(w, engineMat,
                        new Vector3(xBoom - side * hw * 0.04f, hgt * 0.08f, -len * 0.02f),
                        new Vector3(xBoom + side * hw * 0.04f, hgt * 0.08f, -len * 0.02f),
                        new Vector3(xBoom, hgt * 0.14f, len * 0.02f));
                }
            }

            if (hullKey is "miner_eva")
            {
                TriMat(w, RaceMeshWriter.HullMaterial.Weapon,
                    new Vector3(hw * 0.46f, hgt * 0.38f, len * 0.08f),
                    new Vector3(hw * 0.58f, hgt * 0.46f, len * 0.12f),
                    new Vector3(hw * 0.50f, hgt * 0.32f, len * 0.10f));
                TriMat(w, accent,
                    new Vector3(hw * 0.50f, hgt * 0.44f, len * 0.10f),
                    new Vector3(hw * 0.56f, hgt * 0.50f, len * 0.12f),
                    new Vector3(hw * 0.52f, hgt * 0.40f, len * 0.11f));
            }

            if (hullKey is "miner_tractor")
            {
                TriMat(w, accent,
                    new Vector3(-hw * 0.08f, hgt * 0.72f, len * 0.38f),
                    new Vector3(hw * 0.08f, hgt * 0.72f, len * 0.38f),
                    new Vector3(0, hgt * 0.78f, len * 0.42f));
            }

            if (hullKey is "miner_basic")
            {
                TriMat(w, engineMat,
                    new Vector3(-hw * 0.22f, hgt * 0.04f, -len * 0.08f),
                    new Vector3(hw * 0.22f, hgt * 0.04f, -len * 0.08f),
                    new Vector3(0, hgt * 0.10f, -len * 0.04f));
                for (int side = -1; side <= 1; side += 2)
                {
                    float xTip = side * hw * 0.78f;
                    TriMat(w, panel,
                        new Vector3(xTip, hgt * 0.12f, len * 0.48f),
                        new Vector3(xTip + side * hw * 0.03f, hgt * 0.18f, len * 0.54f),
                        new Vector3(xTip, hgt * 0.08f, len * 0.56f));
                    TriMat(w, RaceMeshWriter.HullMaterial.Weapon,
                        new Vector3(xTip, hgt * 0.22f, len * 0.58f),
                        new Vector3(xTip + side * hw * 0.04f, hgt * 0.28f, len * 0.62f),
                        new Vector3(xTip, hgt * 0.16f, len * 0.64f));
                    TriMat(w, engineMat,
                        new Vector3(xTip - side * hw * 0.03f, hgt * 0.06f, len * 0.10f),
                        new Vector3(xTip + side * hw * 0.03f, hgt * 0.06f, len * 0.10f),
                        new Vector3(xTip, hgt * 0.12f, len * 0.14f));
                }
            }

            if (hullKey is "freighter_bulk")
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    float cx = side * hw * 0.42f;
                    TriMat(w, panel,
                        new Vector3(cx - hw * 0.06f, hgt * 0.22f, len * 0.30f),
                        new Vector3(cx + hw * 0.06f, hgt * 0.22f, len * 0.30f),
                        new Vector3(cx, hgt * 0.28f, len * 0.34f));
                }
                TriMat(w, accent,
                    new Vector3(-hw * 0.08f, hgt * 0.38f, len * 0.36f),
                    new Vector3(hw * 0.08f, hgt * 0.38f, len * 0.36f),
                    new Vector3(0, hgt * 0.44f, len * 0.40f));
            }

            if (hullKey is "transport_cargo")
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    float cx = side * hw * 0.46f;
                    TriMat(w, RaceMeshWriter.HullMaterial.Weapon,
                        new Vector3(cx - hw * 0.06f, hgt * 0.24f, len * 0.28f),
                        new Vector3(cx + hw * 0.06f, hgt * 0.24f, len * 0.28f),
                        new Vector3(cx, hgt * 0.30f, len * 0.32f));
                    TriMat(w, panel,
                        new Vector3(cx, hgt * 0.14f, len * 0.20f),
                        new Vector3(cx + side * hw * 0.04f, hgt * 0.20f, len * 0.24f),
                        new Vector3(cx, hgt * 0.10f, len * 0.26f));
                }
                for (int s = 0; s < 3; s++)
                {
                    float z = len * (0.10f + s * 0.12f);
                    TriMat(w, accent,
                        new Vector3(-hw * 0.06f, hgt * 0.40f, z),
                        new Vector3(hw * 0.06f, hgt * 0.40f, z),
                        new Vector3(0, hgt * 0.46f, z + len * 0.010f));
                }
            }

            AddTrussUtilitySubstrate(w, hullKey, hw, hgt, len);
            AddTrussUtilityGameplayComponentBands(w, hullKey, hw, hgt, len);

            if (!triBudgetTight)
            {
                TriMat(w, engineMat,
                    new Vector3(-hw * 0.12f, 0, -len * 0.08f), new Vector3(hw * 0.12f, 0, -len * 0.08f),
                    new Vector3(0, hgt * 0.06f, -len * 0.12f));
            }
            return;
        }

        hw = wid * 0.5f;

        bool isMediumCombat = hullKey is "corvette" or "corvette_fast" or "frigate" or "frigate_strike"
            or "gunship" or "gunship_heavy" or "bomber" or "bomber_heavy";
        bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";
        bool isCorvette = hullKey is "corvette" or "corvette_fast";
        bool isFrigateStrike = hullKey is "frigate_strike";
        bool isFrigate = hullKey is "frigate" or "frigate_strike";
        bool isGunship = hullKey is "gunship" or "gunship_heavy";
        bool isBomber = hullKey is "bomber" or "bomber_heavy";

        if (isMediumCombat || isDestroyer)
        {
            // Loop-05 tri trim on bomber_heavy/gunship_heavy medium-combat hulls.
            int riggingSegs = isBomber ? 3 : isFrigateStrike ? 5 : isFrigate ? 4 : isGunship ? 2 : isCorvette ? 5 : 4;
            float boomSpan = isBomber ? 0.62f : isFrigateStrike ? 0.80f : isFrigate ? 0.84f : isGunship ? 0.70f : isCorvette ? 0.76f : 0.82f;
            float riggingReach = isDestroyer ? 0.26f : isFrigateStrike ? 0.26f : isFrigate ? 0.22f : isCorvette ? 0.18f : isBomber ? 0.24f : 0.14f;
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < riggingSegs; i++)
                {
                    float t = i / MathF.Max(1f, riggingSegs - 1);
                    float z = MathHelper.Lerp(-len * 0.04f, len * riggingReach, t);
                    float xOut = side * hw * MathHelper.Lerp(boomSpan, 0.52f, t);
                    TriMat(w, accent,
                        new Vector3(xOut, hgt * (0.16f + t * 0.03f), z),
                        new Vector3(xOut - side * hw * 0.03f, hgt * (0.12f + t * 0.03f), z + len * 0.008f),
                        new Vector3(xOut, hgt * (0.10f + t * 0.02f), z + len * 0.012f));
                }
            }

            int spineBands = isFrigateStrike ? 5 : isFrigate ? 4 : isBomber ? 5 : isGunship ? 3 : isCorvette ? 3 : 5;
            float spineReach = isDestroyer ? 0.26f : isFrigateStrike ? 0.26f : isFrigate ? 0.22f : isCorvette ? 0.18f : isBomber ? 0.26f : 0.16f;
            for (int p = 0; p < spineBands; p++)
            {
                float t = p / MathF.Max(1f, spineBands - 1);
                float z = MathHelper.Lerp(-len * 0.06f, len * spineReach, t);
                float xSpan = hw * MathHelper.Lerp(0.34f, 0.10f, t);
                TriMat(w, accent,
                    new Vector3(-xSpan, hgt * (0.44f + t * 0.04f), z), new Vector3(xSpan, hgt * (0.44f + t * 0.04f), z),
                    new Vector3(0, hgt * (0.50f + t * 0.04f), z + len * 0.006f));
            }

            if (isGunship)
            {
                float zChin = len * 0.10f;
                TriMat(w, panel,
                    new Vector3(-hw * 0.20f, hgt * 0.02f, zChin), new Vector3(hw * 0.20f, hgt * 0.02f, zChin),
                    new Vector3(0, hgt * 0.10f, zChin + len * 0.010f));
            }

            AddTrussMediumCombatSubstrate(w, hullKey, hw, hgt, len);
            AddTrussGameplayComponentBands(w, hullKey, hw, hgt, len);
            return;
        }

        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDreadnought = hullKey is "dreadnought";
        if (isCruiser || isCarrier || isDreadnought)
        {
            float dorsalBase = isCarrier ? 0.44f : isDreadnought ? 0.50f : 0.54f;
            float dorsalRise = isCarrier ? 0.04f : isDreadnought ? 0.08f : 0.06f;
            // Loop-03: dreadnought dorsal spine tiers — bow-elongated capital panel readability.
            int dorsalSegs = isDreadnought ? 10 : isCarrier ? 2 : isCruiser ? 6 : 3;
            for (int i = 0; i < dorsalSegs; i++)
            {
                float t = i / MathF.Max(1f, dorsalSegs - 1);
                float z = MathHelper.Lerp(len * 0.06f, len * (isDreadnought ? 0.74f : isCarrier ? 0.72f : 0.66f), t);
                TriMat(w, accent,
                    new Vector3(0, hgt * (dorsalBase + t * dorsalRise), z),
                    new Vector3(-hw * 0.10f, hgt * (dorsalBase - 0.10f + t * 0.03f), z - len * 0.02f),
                    new Vector3(hw * 0.10f, hgt * (dorsalBase - 0.10f + t * 0.03f), z - len * 0.02f));
            }

            for (int side = -1; side <= 1; side += 2)
            {
                float xOut = side * hw * (isDreadnought ? 1.30f : isCarrier ? 1.08f : isCruiser ? 0.92f : 1.86f);
                int boomSegs = isCarrier ? 1 : isDreadnought ? 4 : isCruiser ? 2 : 4;
                for (int e = 0; e < boomSegs; e++)
                {
                    float z = MathHelper.Lerp(len * 0.16f, len * (isDreadnought ? 0.74f : isCarrier ? 0.64f : 0.58f), e / MathF.Max(1f, boomSegs - 1));
                    TriMat(w, frame,
                        new Vector3(xOut, hgt * (0.14f + e * 0.015f), z),
                        new Vector3(xOut - side * hw * 0.04f, hgt * (0.12f + e * 0.015f), z + len * 0.006f),
                        new Vector3(xOut, hgt * (0.10f + e * 0.015f), z + len * 0.010f));
                }
            }

            AddTrussCapitalSubstrate(w, hullKey, hw, hgt, len);
            AddTrussCapitalEnvelopeBooms(w, hullKey, hw, hgt, len);
            AddTrussGameplayComponentBands(w, hullKey, hw, hgt, len);
            return;
        }

        bool isScout = hullKey is "scout" or "scout_light";
        bool isFighter = hullKey is "fighter" or "fighter_basic";
        bool isInterceptor = hullKey is "interceptor" or "interceptor_mk2";
        bool isDrone = hullKey is "drone" or "drone_swarm";
        bool isHero = hullKey is "hero" or "hero_default";
        bool isCompactCraft = isScout || isFighter || isInterceptor || isDrone;
        bool isReferenceCraft = isFighter || isHero;

        if (isCompactCraft || isHero)
        {
            int riggingSegs = isDrone ? 4 : isScout ? 5 : isInterceptor ? 4 : isFighter ? 4 : 5;
            float boomSpan = isScout ? 0.68f : isDrone ? 0.54f : isInterceptor ? 0.82f : isFighter ? 0.66f : 0.74f;
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < riggingSegs; i++)
                {
                    float t = i / MathF.Max(1f, riggingSegs - 1);
                    float z = MathHelper.Lerp(-len * 0.04f, len * (isScout ? 0.16f : isDrone ? 0.14f : 0.12f), t);
                    float xOut = side * hw * MathHelper.Lerp(boomSpan, 0.46f, t);
                    TriMat(w, accent,
                        new Vector3(xOut, hgt * (0.16f + t * 0.04f), z),
                        new Vector3(xOut - side * hw * 0.03f, hgt * (0.13f + t * 0.04f), z + len * 0.008f),
                        new Vector3(xOut, hgt * (0.11f + t * 0.03f), z + len * 0.012f));
                }
            }

            if (isHero)
            {
                for (int p = 0; p < 4; p++)
                {
                    float t = p / 3f;
                    float z = MathHelper.Lerp(-len * 0.06f, len * 0.20f, t);
                    float xSpan = hw * MathHelper.Lerp(0.30f, 0.08f, t);
                    TriMat(w, accent,
                        new Vector3(-xSpan, hgt * (0.58f + t * 0.06f), z), new Vector3(xSpan, hgt * (0.58f + t * 0.06f), z),
                        new Vector3(0, hgt * (0.66f + t * 0.06f), z + len * 0.006f));
                }
            }

            if (isReferenceCraft)
                AddTrussReferenceCraftSubstrate(w, hw, hgt, len, isHero);
            else
                AddTrussCompactCraftSubstrate(w, hullKey, hw, hgt, len);

            AddTrussGameplayComponentBands(w, hullKey, hw, hgt, len);
            return;
        }

        float compact = hullKey is "scout" or "scout_light" or "interceptor" or "interceptor_mk2" ? 0.82f : 1f;
        int bellyPlates = hullKey is "dreadnought" or "carrier" or "carrier_command" ? 7 : 5;
        for (int i = 0; i < bellyPlates; i++)
        {
            float t = (i + 0.5f) / bellyPlates;
            float z = MathHelper.Lerp(-len * 0.22f, len * 0.62f, t) * compact;
            float halfW = wid * 0.22f * (0.8f + 0.2f * MathF.Sin(t * MathF.PI));
            TriMat(w, panel,
                new Vector3(-halfW, hgt * 0.05f, z), new Vector3(halfW, hgt * 0.05f, z),
                new Vector3(0, hgt * 0.22f, z + len * 0.03f));
        }

        for (int i = 0; i < 5; i++)
        {
            float t = (i + 1f) / 6f;
            float z = MathHelper.Lerp(-len * 0.16f, len * 0.72f, t) * compact;
            TriMat(w, accent,
                new Vector3(0, hgt * 0.55f, z), new Vector3(-wid * 0.07f, hgt * 0.42f, z - len * 0.045f),
                new Vector3(wid * 0.07f, hgt * 0.42f, z - len * 0.045f));
        }

        if (hullKey is not "drone" and not "drone_swarm")
        {
            float sternZ = -len * 0.38f * compact;
            TriMat(w, engineMat,
                new Vector3(-wid * 0.14f, 0, sternZ - len * 0.045f), new Vector3(wid * 0.14f, 0, sternZ - len * 0.045f),
                new Vector3(0, hgt * 0.05f, sternZ - len * 0.09f));
        }
    }

    /// <summary>Terran retro ship substrate — flush box panel tiers scaled from race substrate profile.</summary>
    private static void ApplyTerranShipSubstrate(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt, RaceSubstrateProfile profile)
    {
        float hw = wid * 0.5f;

        if (hullKey is "fighter" or "fighter_basic" or "hero" or "hero_default")
        {
            AddTerranReferenceCraftSubstrate(w, hullKey, hw, hgt, len, profile);
            return;
        }

        if (hullKey is "scout" or "scout_light" or "interceptor" or "interceptor_mk2" or "drone" or "drone_swarm")
        {
            AddTerranCompactCraftSubstrate(w, hullKey, hw, hgt, len, profile);
            return;
        }

        if (hullKey is "corvette" or "corvette_fast" or "frigate" or "frigate_strike"
            or "gunship" or "gunship_heavy" or "bomber" or "bomber_heavy"
            or "destroyer" or "destroyer_assault")
        {
            AddTerranMediumCombatSubstrate(w, hullKey, hw, hgt, len, profile);
            return;
        }

        if (hullKey is "cruiser" or "cruiser_heavy" or "carrier" or "carrier_command" or "dreadnought")
        {
            AddTerranCapitalSubstrate(w, hullKey, hw, hgt, len, profile);
            return;
        }

        if (hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk" or "support_repair")
        {
            AddTerranUtilitySubstrate(w, hullKey, hw, hgt, len, profile);
        }
    }

    private static int TerranScaledTiers(int baseTiers, RaceSubstrateProfile profile)
    {
        float scale = profile.MicroFrequency / 2.4f * (0.85f + profile.PanelDepth * 1.2f);
        return Math.Clamp((int)MathF.Round(baseTiers * scale), Math.Max(2, baseTiers - 1), baseTiers + 3);
    }

    private static float TerranBandDepth(float hgt, RaceSubstrateProfile profile, float baseFraction)
        => hgt * (baseFraction + profile.PanelDepth * 0.25f);

    private static float TerranBandLength(float len, RaceSubstrateProfile profile, float baseFraction)
        => len * (baseFraction + profile.Grit * 0.08f);

    private static void AddTerranReferenceCraftSubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len, RaceSubstrateProfile profile)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        bool isFighter = hullKey is "fighter" or "fighter_basic";
        bool isHero = hullKey is "hero" or "hero_default";
        int dorsalRows = TerranScaledTiers(isFighter ? 3 : 3, profile);
        float bandLen = TerranBandLength(len, profile, 0.08f);
        float bandDepth = TerranBandDepth(hgt, profile, 0.06f);
        float dorsalZStart = isFighter ? -len * 0.06f : -len * 0.10f;
        float dorsalZEnd = isFighter ? len * 0.26f : len * 0.16f;

        for (int r = 0; r < dorsalRows; r++)
        {
            float t = r / MathF.Max(1f, dorsalRows - 1);
            float z0 = MathHelper.Lerp(dorsalZStart, dorsalZEnd, t);
            float z1 = z0 + bandLen;
            float halfW = hw * MathHelper.Lerp(isFighter ? 0.28f : 0.20f, isFighter ? 0.18f : 0.12f, t);
            float y0 = hgt * (isFighter ? (0.26f + t * 0.06f) : (0.08f + t * 0.05f));
            float y1 = y0 + bandDepth;
            var mat = (r % 3) switch { 0 => panel, 1 => frame, _ => hull };
            AddSurfaceBoxMat(w, mat, -halfW, halfW, y0, y1, z0, z1);
        }

        int bellyRows = TerranScaledTiers(2, profile);
        float bellyZStart = isFighter ? -len * 0.20f : -len * 0.04f;
        float bellyZEnd = isFighter ? len * 0.14f : len * 0.16f;
        for (int b = 0; b < bellyRows; b++)
        {
            float t = b / MathF.Max(1f, bellyRows - 1);
            float z0 = MathHelper.Lerp(bellyZStart, bellyZEnd, t);
            float z1 = z0 + bandLen * 0.95f;
            float halfW = hw * MathHelper.Lerp(isFighter ? 0.30f : 0.26f, isFighter ? 0.22f : 0.18f, t);
            float y0 = hgt * 0.02f;
            float y1 = y0 + bandDepth;
            var mat = b % 2 == 0 ? panel : frame;
            AddSurfaceBoxMat(w, mat, -halfW, halfW, y0, y1, z0, z1);
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float xNacelle = side * hw * (isFighter ? 0.18f : 0.16f);
            AddSurfaceBoxMat(w, panel,
                xNacelle - hw * 0.04f, xNacelle + hw * 0.04f,
                hgt * 0.04f, hgt * 0.14f,
                isFighter ? -len * 0.28f : -len * 0.10f,
                isFighter ? len * 0.04f : len * 0.06f);
            if (!isHero)
            {
                float xFlank = side * hw * 0.30f;
                AddSurfaceBoxMat(w, frame,
                    xFlank - side * hw * 0.03f, xFlank + side * hw * 0.03f,
                    hgt * 0.18f, hgt * 0.26f, len * 0.02f, len * 0.10f);
            }
        }

        if (isHero)
        {
            int keelRows = TerranScaledTiers(4, profile);
            for (int k = 0; k < keelRows; k++)
            {
                float t = k / MathF.Max(1f, keelRows - 1);
                float z0 = MathHelper.Lerp(len * 0.02f, len * 0.22f, t);
                float z1 = z0 + bandLen * 0.7f;
                float halfW = hw * MathHelper.Lerp(0.06f, 0.03f, t);
                float y0 = hgt * (0.46f + t * 0.08f);
                float y1 = y0 + bandDepth * 0.8f;
                var mat = k % 2 == 0 ? panel : hull;
                AddSurfaceBoxMat(w, mat, -halfW, halfW, y0, y1, z0, z1);
            }
        }
    }

    private static void AddTerranCompactCraftSubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len, RaceSubstrateProfile profile)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        bool isScout = hullKey is "scout" or "scout_light";
        bool isDrone = hullKey is "drone" or "drone_swarm";
        float bandLen = TerranBandLength(len, profile, 0.05f);
        float bandDepth = TerranBandDepth(hgt, profile, 0.05f);

        int bellyRows = TerranScaledTiers(2, profile);
        float bellyZEnd = isScout ? len * 0.14f : isDrone ? len * 0.18f : len * 0.16f;
        float bellyHalfW = isScout ? 0.22f : isDrone ? 0.14f : 0.24f;
        for (int i = 0; i < bellyRows; i++)
        {
            float t = i / MathF.Max(1f, bellyRows - 1);
            float z0 = MathHelper.Lerp(-len * 0.05f, bellyZEnd, t);
            float z1 = z0 + bandLen;
            float halfW = hw * MathHelper.Lerp(bellyHalfW, bellyHalfW * 0.72f, t);
            float y0 = hgt * 0.02f;
            float y1 = y0 + bandDepth;
            var mat = i % 2 == 0 ? panel : frame;
            AddSurfaceBoxMat(w, mat, -halfW, halfW, y0, y1, z0, z1);
        }

        int flankRows = TerranScaledTiers(3, profile);
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * hw * (isScout ? 0.26f : isDrone ? 0.16f : 0.30f);
            for (int s = 0; s < flankRows; s++)
            {
                float z0 = len * (isDrone ? (-0.02f + s * 0.10f) : (0.02f + s * 0.08f));
                float z1 = z0 + bandLen * 0.75f;
                AddSurfaceBoxMat(w, frame,
                    x - side * hw * 0.03f, x + side * hw * 0.03f,
                    hgt * 0.20f, hgt * 0.28f, z0, z1);
            }
        }

        AddSurfaceBoxMat(w, engine,
            -hw * (isDrone ? 0.08f : 0.10f), hw * (isDrone ? 0.08f : 0.10f),
            0, hgt * 0.10f, -len * (isDrone ? 0.14f : 0.10f), -len * 0.04f);
    }

    private static void AddTerranMediumCombatSubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len, RaceSubstrateProfile profile)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        bool isCorvette = hullKey is "corvette" or "corvette_fast";
        bool isFrigateStrike = hullKey is "frigate_strike";
        bool isFrigate = hullKey is "frigate" or "frigate_strike";
        bool isGunship = hullKey is "gunship" or "gunship_heavy";
        bool isBomber = hullKey is "bomber" or "bomber_heavy";
        bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";
        float bandLen = TerranBandLength(len, profile, 0.045f);
        float bandDepth = TerranBandDepth(hgt, profile, 0.05f);

        int dorsalRows = TerranScaledTiers(
            isCorvette ? 4 : isFrigateStrike ? 5 : isFrigate ? 4 : isBomber ? 3 : isGunship ? 3 : isDestroyer ? 3 : 3,
            profile);
        float dorsalReach = isFrigateStrike ? 0.28f : isFrigate ? 0.24f : isBomber ? 0.20f : isDestroyer ? 0.30f : isCorvette ? 0.22f : 0.16f;
        for (int d = 0; d < dorsalRows; d++)
        {
            float t = d / MathF.Max(1f, dorsalRows - 1);
            float z0 = MathHelper.Lerp(-len * 0.02f, len * dorsalReach, t);
            float z1 = z0 + bandLen;
            float halfW = hw * MathHelper.Lerp(0.22f, 0.08f, t);
            float y0 = hgt * (0.42f + t * 0.04f);
            float y1 = y0 + bandDepth;
            var mat = (d % 3) switch { 0 => panel, 1 => frame, _ => hull };
            AddSurfaceBoxMat(w, mat, -halfW, halfW, y0, y1, z0, z1);
        }

        int bellyRows = TerranScaledTiers(
            isCorvette ? 4 : isBomber ? 3 : isGunship ? 3 : isFrigateStrike ? 5 : isFrigate ? 4 : isDestroyer ? 3 : 3,
            profile);
        float bellyReach = isCorvette ? 0.22f : isBomber ? 0.20f : isFrigateStrike ? 0.26f : isFrigate ? 0.22f : isDestroyer ? 0.28f : 0.16f;
        for (int i = 0; i < bellyRows; i++)
        {
            float t = bellyRows > 1 ? i / (bellyRows - 1f) : 0f;
            float z0 = MathHelper.Lerp(-len * 0.06f, len * bellyReach, t);
            float z1 = z0 + bandLen * 0.9f;
            float halfW = hw * MathHelper.Lerp(0.32f, 0.20f, t);
            float y0 = hgt * 0.02f;
            float y1 = y0 + bandDepth;
            AddSurfaceBoxMat(w, panel, -halfW, halfW, y0, y1, z0, z1);
        }

        int flankRows = TerranScaledTiers(isDestroyer ? 4 : isFrigateStrike ? 5 : 3, profile);
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * hw * (isGunship ? 0.38f : isFrigate ? 0.36f : isBomber ? 0.36f : isDestroyer ? 0.38f : 0.32f);
            for (int s = 0; s < flankRows; s++)
            {
                float z0 = len * (0.02f + s * (isDestroyer ? 0.08f : isFrigateStrike ? 0.06f : 0.07f));
                float z1 = z0 + bandLen * 0.8f;
                AddSurfaceBoxMat(w, frame,
                    x - side * hw * 0.03f, x + side * hw * 0.03f,
                    hgt * 0.16f, hgt * 0.24f, z0, z1);
            }
        }
    }

    private static void AddTerranCapitalSubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len, RaceSubstrateProfile profile)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        bool isDreadnought = hullKey is "dreadnought";
        float bandLen = TerranBandLength(len, profile, 0.04f);
        float bandDepth = TerranBandDepth(hgt, profile, 0.06f);

        int dorsalRows = TerranScaledTiers(isCruiser ? 3 : isDreadnought ? 3 : isCarrier ? 2 : 2, profile);
        for (int d = 0; d < dorsalRows; d++)
        {
            float t = d / MathF.Max(1f, dorsalRows - 1);
            float z0 = MathHelper.Lerp(-len * 0.06f, len * (isCruiser ? 0.36f : isDreadnought ? 0.32f : 0.28f), t);
            float z1 = z0 + bandLen;
            float halfW = hw * MathHelper.Lerp(0.18f, 0.05f, t);
            float y0 = hgt * (0.44f + t * 0.10f);
            float y1 = y0 + bandDepth;
            var mat = (d % 3) switch { 0 => panel, 1 => frame, _ => hull };
            AddSurfaceBoxMat(w, mat, -halfW, halfW, y0, y1, z0, z1);
        }

        int flankRows = TerranScaledTiers(isCruiser ? 4 : isDreadnought ? 4 : 3, profile);
        for (int side = -1; side <= 1; side += 2)
        {
            float xFlank = side * hw * (isCruiser ? 0.48f : isDreadnought ? 0.52f : 0.40f);
            for (int f = 0; f < flankRows; f++)
            {
                float t = f / MathF.Max(1f, flankRows - 1);
                float z0 = MathHelper.Lerp(-len * 0.08f, len * (isCruiser ? 0.42f : isDreadnought ? 0.38f : 0.36f), t);
                float z1 = z0 + bandLen;
                var mat = (f % 3) switch { 0 => panel, 1 => frame, _ => hull };
                AddSurfaceBoxMat(w, mat,
                    xFlank - side * hw * 0.03f, xFlank + side * hw * 0.03f,
                    hgt * (0.12f + t * 0.08f), hgt * (0.18f + t * 0.08f),
                    z0, z1);
            }
        }

        int bellyRows = TerranScaledTiers(isDreadnought ? 4 : isCarrier ? 3 : 4, profile);
        for (int i = 0; i < bellyRows; i++)
        {
            float t = bellyRows == 1 ? 0.5f : i / (float)(bellyRows - 1);
            float z0 = MathHelper.Lerp(-len * 0.12f, len * (isCarrier ? 0.28f : isDreadnought ? 0.26f : 0.22f), t);
            float z1 = z0 + bandLen;
            float halfW = hw * MathHelper.Lerp(
                isCarrier ? 0.36f : isDreadnought ? 0.38f : 0.30f, isDreadnought ? 0.22f : 0.18f, t);
            float y0 = hgt * 0.04f;
            float y1 = y0 + bandDepth;
            AddSurfaceBoxMat(w, panel, -halfW, halfW, y0, y1, z0, z1);
        }
    }

    private static void AddTerranUtilitySubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len, RaceSubstrateProfile profile)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        bool isCargo = hullKey is "freighter_bulk" or "transport_cargo";
        bool isMiner = hullKey is "miner_basic" or "miner_eva" or "miner_tractor";
        bool isSupport = hullKey is "support_repair";
        float bandLen = TerranBandLength(len, profile, 0.05f);
        float bandDepth = TerranBandDepth(hgt, profile, 0.06f);

        int hazardRows = TerranScaledTiers(isMiner ? 6 : isCargo ? 4 : 4, profile);
        for (int h = 0; h < hazardRows; h++)
        {
            float t = h / MathF.Max(1f, hazardRows - 1);
            float z0 = MathHelper.Lerp(-len * 0.04f, len * (isCargo ? 0.30f : isSupport ? 0.20f : 0.24f), t);
            float z1 = z0 + bandLen;
            float y0 = hgt * (0.26f + t * 0.08f);
            float y1 = y0 + bandDepth;
            var mat = h % 2 == 0 ? panel : frame;
            AddSurfaceBoxMat(w, mat, -hw * 0.22f, hw * 0.22f, y0, y1, z0, z1);
        }

        int bellyRows = TerranScaledTiers(isCargo ? 2 : 3, profile);
        for (int i = 0; i < bellyRows; i++)
        {
            float t = i / MathF.Max(1f, bellyRows - 1);
            float z0 = MathHelper.Lerp(-len * 0.06f, len * (isCargo ? 0.24f : isSupport ? 0.18f : 0.18f), t);
            float z1 = z0 + bandLen;
            float halfW = hw * MathHelper.Lerp(
                hullKey is "freighter_bulk" ? 0.30f : isCargo ? 0.24f : 0.26f,
                hullKey is "freighter_bulk" ? 0.16f : 0.12f, t);
            float y0 = hgt * 0.02f;
            float y1 = y0 + bandDepth;
            AddSurfaceBoxMat(w, panel, -halfW, halfW, y0, y1, z0, z1);
        }

        int lateralRows = TerranScaledTiers(isCargo ? 2 : isSupport ? 3 : 2, profile);
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * hw * (hullKey is "freighter_bulk" ? 0.26f : isCargo ? 0.22f : 0.24f);
            for (int s = 0; s < lateralRows; s++)
            {
                float t = s / MathF.Max(1f, lateralRows - 1);
                float z0 = MathHelper.Lerp(-len * 0.02f, len * (isCargo ? 0.18f : isSupport ? 0.14f : 0.14f), t);
                float z1 = z0 + bandLen * 0.85f;
                AddSurfaceBoxMat(w, frame,
                    x - side * hw * 0.035f, x + side * hw * 0.035f,
                    hgt * 0.12f, hgt * 0.22f, z0, z1);
            }
        }

        int dorsalRows = TerranScaledTiers(isSupport ? 4 : isMiner ? 3 : 3, profile);
        for (int i = 0; i < dorsalRows; i++)
        {
            float t = i / MathF.Max(1f, dorsalRows - 1);
            float z0 = MathHelper.Lerp(-len * 0.04f, len * (hullKey switch
            {
                "freighter_bulk" => 0.42f,
                "transport_cargo" => 0.40f,
                "miner_basic" => 0.26f,
                "miner_eva" => 0.20f,
                "miner_tractor" => 0.24f,
                "support_repair" => 0.20f,
                _ => 0.18f
            }), t);
            float z1 = z0 + bandLen * 0.8f;
            float halfW = hw * MathHelper.Lerp(0.14f, 0.04f, t);
            float y0 = hgt * (0.44f + t * 0.06f);
            float y1 = y0 + bandDepth;
            AddSurfaceBoxMat(w, hull, -halfW, halfW, y0, y1, z0, z1);
        }
    }

    /// <summary>Gameplay-readable engine/weapon/shield wells on Terran retro hulls — flush boxes.</summary>
    private static void AddTerranGameplayComponentBands(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDreadnought = hullKey is "dreadnought";
        bool isGunship = hullKey is "gunship" or "gunship_heavy";
        bool isBomber = hullKey is "bomber" or "bomber_heavy";
        bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";

        if (hullKey is "scout" or "scout_light")
        {
            AddSurfaceBoxMat(w, shield,
                -hw * 0.05f, hw * 0.05f, hgt * 0.74f, hgt * 0.80f, len * 0.08f, len * 0.10f);
            for (int side = -1; side <= 1; side += 2)
            {
                float xPod = side * hw * 0.52f;
                AddSurfaceBoxMat(w, weapon,
                    xPod - side * hw * 0.04f, xPod + side * hw * 0.04f,
                    hgt * 0.08f, hgt * 0.14f, len * 0.06f, len * 0.09f);
            }
            AddSurfaceBoxMat(w, engine,
                -hw * 0.06f, hw * 0.06f, hgt * 0.04f, hgt * 0.12f, -len * 0.08f, -len * 0.04f);
            return;
        }

        if (hullKey is "interceptor" or "interceptor_mk2")
        {
            AddSurfaceBoxMat(w, weapon,
                -hw * 0.08f, hw * 0.08f, hgt * 0.10f, hgt * 0.16f, len * 0.32f, len * 0.36f);
            for (int side = -1; side <= 1; side += 2)
            {
                float xPod = side * hw * 0.46f;
                AddSurfaceBoxMat(w, weapon,
                    xPod - side * hw * 0.04f, xPod + side * hw * 0.04f,
                    hgt * 0.06f, hgt * 0.12f, len * 0.04f, len * 0.07f);
            }
            AddSurfaceBoxMat(w, engine,
                -hw * 0.10f, hw * 0.10f, hgt * 0.04f, hgt * 0.12f, -len * 0.06f, -len * 0.02f);
            AddSurfaceBoxMat(w, shield,
                -hw * 0.05f, hw * 0.05f, hgt * 0.72f, hgt * 0.78f, len * 0.06f, len * 0.08f);
            return;
        }

        if (hullKey is "drone" or "drone_swarm")
        {
            AddSurfaceBoxMat(w, engine,
                -hw * 0.08f, hw * 0.08f, hgt * 0.04f, hgt * 0.12f, -len * 0.18f, -len * 0.12f);
            for (int side = -1; side <= 1; side += 2)
            {
                float xPod = side * hw * 0.28f;
                AddSurfaceBoxMat(w, weapon,
                    xPod - side * hw * 0.03f, xPod + side * hw * 0.03f,
                    hgt * 0.04f, hgt * 0.10f, len * 0.10f, len * 0.12f);
            }
            AddSurfaceBoxMat(w, shield,
                -hw * 0.05f, hw * 0.05f, hgt * 0.70f, hgt * 0.76f, len * 0.06f, len * 0.08f);
            return;
        }

        if (hullKey is "fighter_basic" or "hero_default" or "fighter" or "hero")
        {
            bool isFighterHull = hullKey is "fighter" or "fighter_basic";
            float engineZ0 = isFighterHull ? -len * 0.33f : -len * 0.08f;
            float engineZ1 = isFighterHull ? -len * 0.21f : -len * 0.04f;
            AddSurfaceBoxMat(w, engine,
                -hw * 0.12f, hw * 0.12f,
                isFighterHull ? hgt * 0.02f : hgt * 0.04f,
                isFighterHull ? hgt * 0.14f : hgt * 0.12f,
                engineZ0, engineZ1);
            for (int side = -1; side <= 1; side += 2)
            {
                float xPylon = side * hw * (hullKey is "hero" or "hero_default" ? 0.50f : 0.44f);
                AddSurfaceBoxMat(w, weapon,
                    xPylon - side * hw * 0.04f, xPylon + side * hw * 0.04f,
                    hgt * 0.04f, hgt * 0.10f, len * 0.04f, len * 0.07f);
                float xBell = side * hw * (isFighterHull ? 0.18f : 0.18f);
                AddSurfaceBoxMat(w, engine,
                    xBell - hw * 0.04f, xBell + hw * 0.04f,
                    isFighterHull ? hgt * 0.02f : 0,
                    isFighterHull ? hgt * 0.14f : hgt * 0.10f,
                    isFighterHull ? -len * 0.33f : -len * 0.12f,
                    isFighterHull ? -len * 0.20f : -len * 0.04f);
            }
            AddSurfaceBoxMat(w, shield,
                -hw * 0.06f, hw * 0.06f,
                hgt * (hullKey is "hero" or "hero_default" ? 0.76f : 0.72f),
                hgt * (hullKey is "hero" or "hero_default" ? 0.82f : 0.78f),
                len * 0.08f, len * 0.10f);
            return;
        }

        if (isCruiser)
        {
            AddSurfaceBoxMat(w, engine,
                -hw * 0.12f, hw * 0.12f, hgt * 0.06f, hgt * 0.14f, -len * 0.22f, -len * 0.16f);
            AddSurfaceBoxMat(w, weapon,
                -hw * 0.08f, hw * 0.08f, hgt * 0.18f, hgt * 0.24f, len * 0.62f, len * 0.68f);
            AddSurfaceBoxMat(w, shield,
                -hw * 0.07f, hw * 0.07f, hgt * 0.76f, hgt * 0.82f, len * 0.08f, len * 0.11f);
            return;
        }

        if (isCarrier)
        {
            AddSurfaceBoxMat(w, engine,
                -hw * 0.12f, hw * 0.12f, hgt * 0.06f, hgt * 0.14f, -len * 0.22f, -len * 0.16f);
            AddSurfaceBoxMat(w, weapon,
                -hw * 0.34f, hw * 0.34f, hgt * 0.28f, hgt * 0.34f, len * 0.04f, len * 0.08f);
            AddSurfaceBoxMat(w, weapon,
                -hw * 0.22f, hw * 0.22f, hgt * 0.10f, hgt * 0.16f, -len * 0.04f, len * 0.02f);
            AddSurfaceBoxMat(w, shield,
                hw * 0.16f, hw * 0.22f, hgt * 0.64f, hgt * 0.74f, len * 0.08f, len * 0.11f);
            return;
        }

        if (isDreadnought)
        {
            AddSurfaceBoxMat(w, engine,
                -hw * 0.14f, hw * 0.14f, hgt * 0.06f, hgt * 0.16f, -len * 0.24f, -len * 0.18f);
            for (int side = -1; side <= 1; side += 2)
            {
                float xPod = side * hw * 0.50f;
                AddSurfaceBoxMat(w, weapon,
                    xPod - side * hw * 0.05f, xPod + side * hw * 0.05f,
                    hgt * 0.06f, hgt * 0.12f, len * 0.68f, len * 0.72f);
            }
            AddSurfaceBoxMat(w, shield,
                -hw * 0.07f, hw * 0.07f, hgt * 0.78f, hgt * 0.84f, len * 0.08f, len * 0.11f);
            return;
        }

        if (hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk" or "support_repair")
        {
            AddSurfaceBoxMat(w, engine,
                -hw * 0.14f, hw * 0.14f, hgt * 0.04f, hgt * 0.12f, -len * 0.10f, -len * 0.04f);
            return;
        }

        AddSurfaceBoxMat(w, engine,
            -hw * 0.14f, hw * 0.14f, hgt * 0.04f, hgt * 0.12f, -len * 0.10f, -len * 0.04f);

        if (isGunship)
        {
            AddSurfaceBoxMat(w, weapon,
                -hw * 0.12f, hw * 0.12f, hgt * 0.02f, hgt * 0.10f, len * 0.16f, len * 0.24f);
            for (int side = -1; side <= 1; side += 2)
            {
                float xBay = side * hw * 0.42f;
                AddSurfaceBoxMat(w, weapon,
                    xBay - side * hw * 0.04f, xBay + side * hw * 0.04f,
                    hgt * 0.02f, hgt * 0.08f, -len * 0.02f, len * 0.04f);
            }
        }
        else if (isBomber)
        {
            AddSurfaceBoxMat(w, weapon,
                -hw * 0.28f, hw * 0.28f, hgt * 0.02f, hgt * 0.10f, len * 0.02f, len * 0.08f);
        }
        else if (isDestroyer)
        {
            AddSurfaceBoxMat(w, weapon,
                -hw * 0.10f, hw * 0.10f, hgt * 0.08f, hgt * 0.16f, len * 0.50f, len * 0.56f);
        }
        else
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xBay = side * hw * 0.48f;
                AddSurfaceBoxMat(w, weapon,
                    xBay - side * hw * 0.04f, xBay + side * hw * 0.04f,
                    hgt * 0.02f, hgt * 0.08f, -len * 0.02f, len * 0.04f);
            }
        }

        AddSurfaceBoxMat(w, shield,
            -hw * 0.06f, hw * 0.06f, hgt * 0.70f, hgt * 0.76f, len * 0.06f, len * 0.09f);
    }

    /// <summary>Terran modern flush panel overlay — accent stripes and supplemental hazard bands.</summary>
    private static void AddTerranModernSurfaceOverlay(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt, RaceSubstrateProfile profile)
    {
        float hw = wid * 0.5f;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var frame = RaceMeshWriter.HullMaterial.Truss;

        bool isUtility = hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk" or "support_repair";
        bool isCapital = hullKey is "cruiser" or "cruiser_heavy" or "carrier" or "carrier_command" or "dreadnought";
        bool isFighter = hullKey is "fighter" or "fighter_basic";
        bool isReference = isFighter || hullKey is "hero" or "hero_default";

        if (isReference)
        {
            if (isFighter)
            {
                for (int c = 0; c < 6; c++)
                {
                    float t = c / 5f;
                    float z0 = MathHelper.Lerp(len * 0.22f, len * 0.04f, t);
                    float z1 = z0 + len * 0.010f;
                    float y0 = hgt * (0.32f + t * 0.06f);
                    float y1 = y0 + hgt * 0.05f;
                    float halfW = hw * MathHelper.Lerp(0.10f, 0.05f, t);
                    AddSurfaceBoxScorerAccent(w, -halfW, halfW, y0, y1, z0, z1);
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float xLead = side * hw * 0.40f;
                    for (int e = 0; e < 3; e++)
                    {
                        float z = MathHelper.Lerp(len * 0.10f, len * 0.02f, e / 2f);
                        w.TriScorerAccent(
                            new Vector3(xLead, hgt * (0.22f + e * 0.02f), z),
                            new Vector3(xLead - side * hw * 0.02f, hgt * (0.20f + e * 0.02f), z + len * 0.005f),
                            new Vector3(xLead, hgt * (0.18f + e * 0.02f), z + len * 0.008f));
                    }
                }
            }
        }
        else if (isCapital && hullKey is "cruiser" or "cruiser_heavy")
        {
            for (int a = 0; a < 8; a++)
            {
                float t = a / 7f;
                float z = MathHelper.Lerp(len * 0.06f, len * 0.32f, t);
                float y = hgt * (0.30f + t * 0.10f);
                float halfW = hw * MathHelper.Lerp(0.12f, 0.05f, t);
                w.TriScorerAccent(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + hgt * 0.04f, z + len * 0.005f));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float xLead = side * hw * 0.42f;
                for (int e = 0; e < 3; e++)
                {
                    float z = MathHelper.Lerp(len * 0.10f, len * 0.02f, e / 2f);
                    w.TriScorerAccent(
                        new Vector3(xLead, hgt * (0.18f + e * 0.03f), z),
                        new Vector3(xLead - side * hw * 0.02f, hgt * (0.16f + e * 0.03f), z + len * 0.005f),
                        new Vector3(xLead, hgt * (0.14f + e * 0.03f), z + len * 0.008f));
                }
            }
        }
        else if (isUtility)
        {
            bool isMiner = hullKey is "miner_basic" or "miner_eva" or "miner_tractor";
            int hazardBands = TerranScaledTiers(isMiner ? 6 : 4, profile);
            float bandLen = TerranBandLength(len, profile, 0.04f);
            float bandDepth = TerranBandDepth(hgt, profile, 0.05f);
            for (int h = 0; h < hazardBands; h++)
            {
                float t = h / MathF.Max(1f, hazardBands - 1);
                float z0 = MathHelper.Lerp(-len * 0.04f, len * 0.24f, t);
                float z1 = z0 + bandLen * 0.75f;
                float y0 = hgt * (0.34f + t * 0.08f);
                float y1 = y0 + bandDepth;
                var tierMat = h % 2 == 0 ? panel : frame;
                AddSurfaceBoxMat(w, tierMat, -hw * 0.10f, hw * 0.10f, y0, y1, z0, z1);
                if (isMiner && h % 2 == 0)
                {
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.06f, hgt * (0.40f + t * 0.06f), z0 + len * 0.004f),
                        new Vector3(hw * 0.06f, hgt * (0.40f + t * 0.06f), z0 + len * 0.004f),
                        new Vector3(0, hgt * (0.46f + t * 0.06f), z0 + len * 0.008f));
                }
            }
        }
    }

    /// <summary>Shared industrial panel tiers for retro stations — flush box bands (no facet-seam strips).</summary>
    private static void AddTerranIndustrialPanelTiers(
        RaceMeshWriter w, float s, int tiers, float zStart, float zStep, RaceSubstrateProfile profile)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        float bandDepth = TerranBandDepth(s, profile, 0.06f);
        float bandLen = TerranBandLength(s, profile, 0.04f);
        float yBase = s * (0.04f + profile.PanelDepth * 0.02f);
        float yStep = bandDepth * (0.10f + profile.Grit * 0.04f);
        for (int p = 0; p < tiers; p++)
        {
            float z0 = zStart + p * zStep;
            float z1 = z0 + bandLen;
            float halfW = s * MathHelper.Lerp(0.78f, 0.48f, p / MathF.Max(1f, tiers - 1));
            float y0 = yBase + p * yStep;
            float y1 = y0 + bandDepth;
            var mat = (p % 3) switch { 0 => panel, 1 => frame, _ => hull };
            AddSurfaceBoxMat(w, mat, -halfW, halfW, y0, y1, z0, z1);
        }
    }

    /// <summary>Flush coplanar pad/deck tier rings — uniform luminance per zone, no facet-seam tris.</summary>
    private static void AddTerranStationPadFlushTiers(
        RaceMeshWriter w, float s, float padR, int rings, RaceSubstrateProfile profile)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        float bandDepth = TerranBandDepth(s, profile, 0.05f);
        int tierRings = SubstrateScaledTiers(rings, profile);
        for (int ring = 0; ring < tierRings; ring++)
        {
            float t = ring / MathF.Max(1f, tierRings - 1);
            float halfSpan = padR * MathHelper.Lerp(0.88f, 0.62f, t);
            float y0 = s * (0.04f + t * 0.02f);
            float y1 = y0 + bandDepth;
            var mat = ring % 2 == 0 ? panel : frame;
            AddSurfaceBoxMat(w, mat, -halfSpan, halfSpan, y0, y1, -halfSpan, halfSpan);
        }
    }

    /// <summary>Flush vasudan terrace pad rings — sandstone/gunmetal tiers, no facet-seam tris.</summary>
    private static void AddVasudanStationPadFlushTiers(
        RaceMeshWriter w, float s, float padR, int rings, RaceSubstrateProfile profile)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float bandDepth = StationBandDepth(s, profile, 0.05f);
        int tierRings = SubstrateScaledTiers(rings, profile);
        for (int ring = 0; ring < tierRings; ring++)
        {
            float t = ring / MathF.Max(1f, tierRings - 1);
            float halfSpan = padR * MathHelper.Lerp(0.90f, 0.64f, t);
            float y0 = s * (0.04f + t * 0.02f);
            float y1 = y0 + bandDepth;
            var mat = ring % 3 == 0 ? accent : ring % 2 == 0 ? panel : hull;
            AddSurfaceBoxMat(w, mat, -halfSpan, halfSpan, y0, y1, -halfSpan, halfSpan);
        }
    }

    /// <summary>Profile-scaled vasudan terrace panel tiers — flush box bands on pad/deck zones.</summary>
    private static void AddVasudanStationTerraceTiers(
        RaceMeshWriter w, float s, int tiers, float zStart, float zStep, RaceSubstrateProfile profile)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float bandDepth = StationBandDepth(s, profile, 0.06f);
        float bandLen = s * (0.04f + profile.Grit * 0.02f);
        float yBase = s * (0.04f + profile.PanelDepth * 0.02f);
        float yStep = bandDepth * (0.10f + profile.Grit * 0.04f);
        for (int p = 0; p < tiers; p++)
        {
            float z0 = zStart + p * zStep;
            float z1 = z0 + bandLen;
            float halfW = s * MathHelper.Lerp(0.78f, 0.48f, p / MathF.Max(1f, tiers - 1));
            float y0 = yBase + p * yStep;
            float y1 = y0 + bandDepth;
            var mat = (p % 3) switch { 0 => panel, 1 => hull, _ => accent };
            AddSurfaceBoxMat(w, mat, -halfW, halfW, y0, y1, z0, z1);
        }
    }

    /// <summary>Flush truss pad tier rings — NASA panel / truss frame boxes, no facet-seam tris.</summary>
    private static void AddTrussStationPadFlushTiers(
        RaceMeshWriter w, float s, float padR, int rings, RaceSubstrateProfile profile)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        float bandDepth = StationBandDepth(s, profile, 0.05f);
        int tierRings = SubstrateScaledTiers(rings, profile);
        for (int ring = 0; ring < tierRings; ring++)
        {
            float t = ring / MathF.Max(1f, tierRings - 1);
            float halfSpan = padR * MathHelper.Lerp(0.90f, 0.66f, t);
            float y0 = s * (0.04f + t * 0.02f);
            float y1 = y0 + bandDepth;
            var mat = ring % 2 == 0 ? panel : frame;
            AddSurfaceBoxMat(w, mat, -halfSpan, halfSpan, y0, y1, -halfSpan, halfSpan);
        }
    }

    /// <summary>Profile-scaled truss NASA panel tiers — flush box bands on pad/gantry zones.</summary>
    private static void AddTrussStationTerraceTiers(
        RaceMeshWriter w, float s, int tiers, float zStart, float zStep, RaceSubstrateProfile profile)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float bandDepth = StationBandDepth(s, profile, 0.06f);
        float bandLen = s * (0.04f + profile.Grit * 0.02f);
        float yBase = s * (0.04f + profile.PanelDepth * 0.02f);
        float yStep = bandDepth * (0.10f + profile.Grit * 0.04f);
        for (int p = 0; p < tiers; p++)
        {
            float z0 = zStart + p * zStep;
            float z1 = z0 + bandLen;
            float halfW = s * MathHelper.Lerp(0.80f, 0.50f, p / MathF.Max(1f, tiers - 1));
            float y0 = yBase + p * yStep;
            float y1 = y0 + bandDepth;
            var mat = (p % 3) switch { 0 => panel, 1 => frame, _ => accent };
            AddSurfaceBoxMat(w, mat, -halfW, halfW, y0, y1, z0, z1);
        }
    }

    /// <summary>Belly/flank substrate bands for Korath compact craft — truss plating under team tint.</summary>
    private static void AddTrussCompactCraftSubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var frameMat = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        bool isScout = hullKey is "scout" or "scout_light";
        bool isInterceptor = hullKey is "interceptor" or "interceptor_mk2";
        bool isDrone = hullKey is "drone" or "drone_swarm";

        int bellyPlates = isScout ? 3 : isDrone ? 3 : 3;
        float bellyZStart = isDrone ? -len * 0.06f : -len * 0.05f;
        float bellyZEnd = isScout ? len * 0.14f : isDrone ? len * 0.18f : len * 0.16f;
        float bellyHalfW = isScout ? 0.22f : isDrone ? 0.14f : 0.24f;
        float bellyLift = isDrone ? 0.15f : 0.18f;

        for (int i = 0; i < bellyPlates; i++)
        {
            float t = i / MathF.Max(1f, bellyPlates - 1);
            float z = MathHelper.Lerp(bellyZStart, bellyZEnd, t);
            float halfW = hw * MathHelper.Lerp(bellyHalfW, bellyHalfW * 0.72f, t);
            TriMat(w, panel,
                new Vector3(-halfW, hgt * 0.03f, z), new Vector3(halfW, hgt * 0.03f, z),
                new Vector3(0, hgt * bellyLift, z + len * 0.008f));
            TriMat(w, frameMat,
                new Vector3(-halfW * 0.82f, hgt * 0.05f, z - len * 0.004f),
                new Vector3(halfW * 0.82f, hgt * 0.05f, z - len * 0.004f),
                new Vector3(0, hgt * (bellyLift * 0.68f), z));
        }

        int seamCount = isDrone ? 3 : isScout ? 3 : 3;
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * hw * (isScout ? 0.26f : isDrone ? 0.16f : 0.30f);
            for (int s = 0; s < seamCount; s++)
            {
                float z = len * (isDrone ? (-0.02f + s * 0.10f) : (0.02f + s * 0.08f));
                TriMat(w, frameMat,
                    new Vector3(x, hgt * 0.24f, z), new Vector3(x - side * hw * 0.03f, hgt * 0.28f, z + len * 0.01f),
                    new Vector3(x, hgt * 0.20f, z + len * 0.012f));
            }
        }

        if (isInterceptor)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xOut = side * hw * 0.12f;
                TriMat(w, panel,
                    new Vector3(xOut, hgt * 0.52f, len * 0.18f), new Vector3(xOut - side * hw * 0.04f, hgt * 0.56f, len * 0.22f),
                    new Vector3(xOut, hgt * 0.46f, len * 0.24f));
            }
        }

        int keelSegs = 3;
        float keelZEnd = isScout ? 0.18f : isInterceptor ? 0.20f : 0.18f;
        for (int k = 0; k < keelSegs; k++)
        {
            float t = k / MathF.Max(1f, keelSegs - 1);
            float z = MathHelper.Lerp(len * (isDrone ? -0.02f : 0.04f), len * keelZEnd, t);
            float yBase = hgt * (0.44f + t * (isDrone ? 0.24f : 0.20f));
            float halfKeel = hw * (isDrone ? 0.03f : 0.05f);
            bool dorsalAccent = (isScout || isInterceptor) && k == keelSegs - 1;
            bool droneSpineAccent = isDrone && k >= 1;
            var keelMat = droneSpineAccent || dorsalAccent ? accent : isDrone ? frameMat : k % 2 == 0 ? accent : frameMat;
            float keelLift = dorsalAccent ? (isInterceptor ? 0.11f : 0.10f) : isDrone && k == keelSegs - 1 ? 0.10f : isDrone ? 0.09f : isInterceptor ? 0.09f : 0.07f;
            TriMat(w, keelMat,
                new Vector3(-halfKeel, yBase, z), new Vector3(halfKeel, yBase, z),
                new Vector3(0, yBase + hgt * keelLift, z + len * 0.005f));
        }

        if (isDrone)
        {
            for (int k = 0; k < 2; k++)
            {
                float z = MathHelper.Lerp(-len * 0.02f, len * 0.14f, k);
                TriMat(w, frameMat,
                    new Vector3(-hw * 0.04f, hgt * 0.34f, z), new Vector3(hw * 0.04f, hgt * 0.34f, z),
                    new Vector3(0, hgt * 0.40f, z + len * 0.006f));
            }
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float xLead = side * hw * (isScout ? 0.44f : isDrone ? 0.22f : 0.38f);
            int edgeSegs = isScout ? 4 : isDrone ? 3 : isInterceptor ? 4 : 3;
            for (int e = 0; e < edgeSegs; e++)
            {
                float z = MathHelper.Lerp(len * (isScout ? 0.10f : 0.08f), -len * 0.02f, e / MathF.Max(1f, edgeSegs - 1));
                TriMat(w, accent,
                    new Vector3(xLead, hgt * (0.18f + e * 0.012f), z),
                    new Vector3(xLead - side * hw * 0.03f, hgt * (0.16f + e * 0.012f), z + len * 0.008f),
                    new Vector3(xLead, hgt * (0.14f + e * 0.012f), z + len * 0.012f));
            }
        }

        int spineSegs = isScout ? 5 : isDrone ? 3 : isInterceptor ? 4 : 3;
        for (int s = 0; s < spineSegs; s++)
        {
            float t = s / MathF.Max(1f, spineSegs - 1);
            float z = MathHelper.Lerp(len * (isDrone ? -0.02f : 0.02f), len * (isScout ? 0.18f : isDrone ? 0.16f : 0.14f), t);
            float halfSpine = hw * (isDrone ? 0.03f : 0.04f);
            TriMat(w, accent,
                new Vector3(-halfSpine, hgt * (0.48f + t * 0.06f), z),
                new Vector3(halfSpine, hgt * (0.48f + t * 0.06f), z),
                new Vector3(0, hgt * (0.54f + t * 0.06f), z + len * 0.005f));
        }

        var engineGlow = RaceMeshWriter.HullMaterial.Engine;
        for (int side = -1; side <= 1; side += 2)
        {
            float xEng = side * hw * (isDrone ? 0.24f : isScout ? 0.08f : 0.10f);
            float zEng = -len * (isDrone ? 0.26f : 0.08f);
            TriMat(w, engineGlow,
                new Vector3(xEng, hgt * 0.08f, zEng),
                new Vector3(xEng - side * hw * 0.02f, hgt * 0.05f, zEng - len * 0.01f),
                new Vector3(xEng, hgt * 0.03f, zEng + len * 0.005f));
        }
    }

    /// <summary>Dorsal keel + belly substrate for Korath fighter/hero reference craft.</summary>
    private static void AddTrussReferenceCraftSubstrate(
        RaceMeshWriter w, float hw, float hgt, float len, bool isHero)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var frameMat = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;

        for (int i = 0; i < 3; i++)
        {
            float t = i / 2f;
            float z = MathHelper.Lerp(-len * 0.04f, len * 0.16f, t);
            float halfW = hw * MathHelper.Lerp(0.26f, 0.18f, t);
            TriMat(w, panel,
                new Vector3(-halfW, hgt * 0.03f, z), new Vector3(halfW, hgt * 0.03f, z),
                new Vector3(0, hgt * 0.16f, z + len * 0.008f));
            TriMat(w, frameMat,
                new Vector3(-halfW * 0.84f, hgt * 0.05f, z - len * 0.004f),
                new Vector3(halfW * 0.84f, hgt * 0.05f, z - len * 0.004f),
                new Vector3(0, hgt * 0.12f, z));
        }

        if (!isHero)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * hw * 0.30f;
                for (int s = 0; s < 2; s++)
                {
                    float z = len * (0.02f + s * 0.08f);
                    TriMat(w, frameMat,
                        new Vector3(x, hgt * 0.22f, z), new Vector3(x - side * hw * 0.03f, hgt * 0.26f, z + len * 0.01f),
                        new Vector3(x, hgt * 0.18f, z + len * 0.012f));
                }
            }
        }

        int keelSegs = isHero ? 4 : 3;
        for (int k = 0; k < keelSegs; k++)
        {
            float t = k / MathF.Max(1f, keelSegs - 1);
            float z = MathHelper.Lerp(len * 0.02f, len * (isHero ? 0.22f : 0.18f), t);
            float halfKeel = hw * MathHelper.Lerp(0.06f, 0.03f, t);
            bool prowAccent = isHero && k == keelSegs - 1;
            TriMat(w, prowAccent || k % 2 == 0 ? accent : frameMat,
                new Vector3(-halfKeel, hgt * (0.46f + t * 0.08f), z), new Vector3(halfKeel, hgt * (0.46f + t * 0.08f), z),
                new Vector3(0, hgt * (0.54f + t * 0.08f), z + len * 0.005f));
        }

        if (!isHero)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xLead = side * hw * 0.62f;
                for (int e = 0; e < 4; e++)
                {
                    float z = MathHelper.Lerp(len * 0.08f, -len * 0.02f, e / 3f);
                    TriMat(w, accent,
                        new Vector3(xLead, hgt * (0.20f + e * 0.01f), z),
                        new Vector3(xLead - side * hw * 0.03f, hgt * (0.18f + e * 0.01f), z + len * 0.008f),
                        new Vector3(xLead, hgt * (0.16f + e * 0.01f), z + len * 0.012f));
                }
            }
        }

        if (isHero)
        {
            for (int s = 0; s < 3; s++)
            {
                float t = s / 2f;
                float z = MathHelper.Lerp(len * 0.04f, len * 0.18f, t);
                TriMat(w, accent,
                    new Vector3(-hw * 0.05f, hgt * (0.72f + t * 0.04f), z),
                    new Vector3(hw * 0.05f, hgt * (0.72f + t * 0.04f), z),
                    new Vector3(0, hgt * (0.78f + t * 0.04f), z + len * 0.005f));
            }
        }
    }

    /// <summary>Belly/flank substrate bands for Korath medium combat — truss plating under team tint.</summary>
    private static void AddTrussMediumCombatSubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var engineGlow = RaceMeshWriter.HullMaterial.Engine;

        bool isCorvette = hullKey is "corvette" or "corvette_fast";
        bool isFrigateStrike = hullKey is "frigate_strike";
        bool isFrigate = hullKey is "frigate" or "frigate_strike";
        bool isGunship = hullKey is "gunship" or "gunship_heavy";
        bool isBomber = hullKey is "bomber" or "bomber_heavy";
        bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";

        int bellyBands = isCorvette ? 4 : isBomber ? 3 : isGunship ? 3 : isFrigateStrike ? 5 : isFrigate ? 4 : isDestroyer ? 5 : 3;
        float bellyWidth = isBomber ? 0.42f : isDestroyer ? 0.34f : isFrigateStrike ? 0.36f : isFrigate ? 0.34f : 0.32f;
        float bellyReach = isCorvette ? 0.22f : isBomber ? 0.20f : isFrigateStrike ? 0.26f : isFrigate ? 0.22f : isDestroyer ? 0.28f : 0.16f;
        for (int i = 0; i < bellyBands; i++)
        {
            float t = bellyBands > 1 ? i / (bellyBands - 1f) : 0f;
            float z = MathHelper.Lerp(-len * 0.06f, len * bellyReach, t);
            float halfW = hw * MathHelper.Lerp(bellyWidth, bellyWidth * 0.66f, t);
            TriMat(w, panel,
                new Vector3(-halfW, hgt * 0.04f, z), new Vector3(halfW, hgt * 0.04f, z),
                new Vector3(0, hgt * (0.12f + t * 0.02f), z + len * 0.008f));
        }

        int recessBands = isCorvette ? 2 : isFrigateStrike ? 3 : isFrigate ? 2 : isBomber ? 1 : isGunship ? 2 : isDestroyer ? 3 : 2;
        for (int r = 0; r < recessBands; r++)
        {
            float t = r / MathF.Max(1f, recessBands - 1);
            float z = MathHelper.Lerp(-len * 0.04f, len * (bellyReach * 0.72f), t);
            float halfW = hw * MathHelper.Lerp(bellyWidth * 0.84f, bellyWidth * 0.52f, t);
            TriMat(w, frame,
                new Vector3(-halfW, hgt * 0.02f, z), new Vector3(halfW, hgt * 0.02f, z),
                new Vector3(0, hgt * (0.08f + t * 0.02f), z + len * 0.006f));
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * hw * (isGunship ? 0.38f : isFrigate ? 0.36f : isBomber ? 0.36f : isDestroyer ? 0.38f : 0.32f);
            int segs = isDestroyer ? 4 : isFrigateStrike ? 5 : isFrigate ? 4 : 3;
            for (int s = 0; s < segs; s++)
            {
                float z = len * (0.02f + s * (isDestroyer ? 0.08f : isFrigateStrike ? 0.06f : 0.07f));
                TriMat(w, frame,
                    new Vector3(x, hgt * 0.20f, z), new Vector3(x - side * hw * 0.03f, hgt * 0.24f, z + len * 0.01f),
                    new Vector3(x, hgt * 0.16f, z + len * 0.012f));
            }
        }

        if (isBomber)
        {
            for (int b = 0; b < 4; b++)
            {
                float z = len * (0.04f + b * 0.06f);
                float halfW = hw * MathHelper.Lerp(0.28f, 0.14f, b / 3f);
                TriMat(w, b % 2 == 0 ? accent : frame,
                    new Vector3(-halfW, hgt * 0.05f, z), new Vector3(halfW, hgt * 0.05f, z),
                    new Vector3(0, hgt * 0.10f, z + len * 0.008f));
            }
        }

        if (isGunship)
        {
            float zChin = len * 0.10f;
            TriMat(w, panel,
                new Vector3(-hw * 0.18f, hgt * 0.02f, zChin), new Vector3(hw * 0.18f, hgt * 0.02f, zChin),
                new Vector3(0, hgt * 0.08f, zChin + len * 0.010f));
        }

        int dorsalStripes = isCorvette ? 4 : isFrigateStrike ? 5 : isFrigate ? 4 : isBomber ? 3 : isGunship ? 3 : isDestroyer ? 4 : 3;
        float dorsalReach = isFrigateStrike ? 0.28f : isFrigate ? 0.24f : isBomber ? 0.20f : isDestroyer ? 0.30f : isCorvette ? 0.22f : 0.16f;
        for (int d = 0; d < dorsalStripes; d++)
        {
            float t = d / MathF.Max(1f, dorsalStripes - 1);
            float z = MathHelper.Lerp(-len * 0.02f, len * dorsalReach, t);
            float xSpan = hw * MathHelper.Lerp(0.22f, 0.08f, t);
            TriMat(w, panel,
                new Vector3(-xSpan, hgt * (0.42f + t * 0.04f), z), new Vector3(xSpan, hgt * (0.42f + t * 0.04f), z),
                new Vector3(0, hgt * (0.46f + t * 0.04f), z + len * 0.006f));
        }

        int spineSegs = isCorvette ? 4 : isFrigateStrike ? 5 : isFrigate ? 4 : isBomber ? 5 : isGunship ? 3 : isDestroyer ? 5 : 3;
        float spineReach = isFrigateStrike ? 0.28f : isFrigate ? 0.24f : isBomber ? 0.28f : isDestroyer ? 0.30f : isCorvette ? 0.22f : 0.16f;
        int accentStride = isBomber ? 1 : isCorvette ? 2 : isFrigateStrike ? 2 : 3;
        for (int s = 0; s < spineSegs; s++)
        {
            float t = s / MathF.Max(1f, spineSegs - 1);
            float z = MathHelper.Lerp(-len * 0.04f, len * spineReach, t);
            float yBase = hgt * (0.46f + t * (isGunship ? 0.14f : 0.12f));
            if (s % accentStride == 0)
            {
                TriMat(w, accent,
                    new Vector3(-hw * 0.05f, yBase + hgt * 0.04f, z), new Vector3(hw * 0.05f, yBase + hgt * 0.04f, z),
                    new Vector3(0, yBase + hgt * 0.10f, z + len * 0.005f));
            }
            else
            {
                TriMat(w, frame,
                    new Vector3(-hw * 0.06f, yBase, z), new Vector3(hw * 0.06f, yBase, z),
                    new Vector3(0, yBase + hgt * 0.06f, z + len * 0.006f));
            }
        }

        TriMat(w, engineGlow,
            new Vector3(-hw * 0.10f, hgt * 0.04f, -len * 0.10f), new Vector3(hw * 0.10f, hgt * 0.04f, -len * 0.10f),
            new Vector3(0, hgt * 0.10f, -len * 0.06f));
        if (isCorvette)
        {
            TriMat(w, engineGlow,
                new Vector3(-hw * 0.08f, hgt * 0.02f, -len * 0.14f), new Vector3(hw * 0.08f, hgt * 0.02f, -len * 0.14f),
                new Vector3(0, hgt * 0.08f, -len * 0.10f));
        }
    }

    /// <summary>Belly/flank substrate bands for Korath capital hulls — truss plating depth under team tint.</summary>
    private static void AddTrussCapitalSubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        bool isDreadnought = hullKey is "dreadnought";
        int bellyPlates = isDreadnought ? 7 : isCarrier ? 5 : 4;

        for (int i = 0; i < bellyPlates; i++)
        {
            float t = bellyPlates == 1 ? 0.5f : i / (float)(bellyPlates - 1);
            float z = MathHelper.Lerp(-len * 0.12f, len * (isCarrier ? 0.28f : isDreadnought ? 0.26f : 0.22f), t);
            float halfW = hw * MathHelper.Lerp(
                isCarrier ? 0.36f : isDreadnought ? 0.38f : 0.30f, isDreadnought ? 0.22f : 0.18f, t);
            TriMat(w, panel,
                new Vector3(-halfW, hgt * 0.05f, z), new Vector3(halfW, hgt * 0.05f, z),
                new Vector3(0, hgt * 0.14f, z + len * 0.010f));
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float xOut = side * hw * (isCarrier ? 0.36f : isDreadnought ? 0.52f : 0.44f);
            int flankBands = isDreadnought ? 7 : isCruiser ? 4 : 3;
            for (int f = 0; f < flankBands; f++)
            {
                float t = f / MathF.Max(1f, flankBands - 1);
                float z = MathHelper.Lerp(-len * 0.06f, len * 0.16f, t);
                TriMat(w, panel,
                    new Vector3(xOut, hgt * 0.12f, z),
                    new Vector3(xOut - side * hw * 0.04f, hgt * 0.18f, z + len * 0.008f),
                    new Vector3(xOut, hgt * 0.08f, z + len * 0.012f));
            }
        }

        if (isCarrier)
        {
            TriMat(w, panel,
                new Vector3(-hw * 0.22f, hgt * 0.08f, -len * 0.06f),
                new Vector3(hw * 0.22f, hgt * 0.08f, -len * 0.06f),
                new Vector3(0, hgt * 0.12f, -len * 0.02f));
            for (int h = 0; h < 2; h++)
            {
                float z = len * (0.02f - h * 0.12f);
                TriMat(w, frame,
                    new Vector3(-hw * 0.32f, hgt * 0.22f, z), new Vector3(hw * 0.32f, hgt * 0.22f, z),
                    new Vector3(0, hgt * 0.25f, z + len * 0.008f));
            }
            float deckZ = MathHelper.Lerp(-len * 0.14f, len * 0.08f, 0.5f);
            TriMat(w, panel,
                new Vector3(-hw * 0.48f, hgt * 0.18f, deckZ), new Vector3(hw * 0.48f, hgt * 0.18f, deckZ),
                new Vector3(0, hgt * 0.22f, deckZ + len * 0.006f));
        }

        if (isCruiser)
        {
            for (int k = 0; k < 4; k++)
            {
                float z = MathHelper.Lerp(len * 0.02f, len * 0.22f, k / 3f);
                TriMat(w, frame,
                    new Vector3(-hw * 0.14f, hgt * 0.34f, z), new Vector3(hw * 0.14f, hgt * 0.34f, z),
                    new Vector3(0, hgt * 0.38f, z + len * 0.006f));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float xBelt = side * hw * 0.40f;
                for (int b = 0; b < 3; b++)
                {
                    float z = MathHelper.Lerp(len * 0.38f, len * 0.52f, b / 2f);
                    TriMat(w, frame,
                        new Vector3(xBelt, hgt * 0.16f, z),
                        new Vector3(xBelt - side * hw * 0.04f, hgt * 0.22f, z + len * 0.006f),
                        new Vector3(xBelt, hgt * 0.12f, z + len * 0.010f));
                }
            }
        }

        if (isDreadnought)
        {
            for (int k = 0; k < 4; k++)
            {
                float z = MathHelper.Lerp(len * 0.44f, len * 0.72f, k / 3f);
                TriMat(w, frame,
                    new Vector3(-hw * 0.12f, hgt * 0.26f, z), new Vector3(hw * 0.12f, hgt * 0.26f, z),
                    new Vector3(0, hgt * 0.32f, z + len * 0.008f));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float xTip = side * hw * 0.82f;
                TriMat(w, accent,
                    new Vector3(xTip, hgt * 0.20f, len * 0.38f),
                    new Vector3(xTip - side * hw * 0.03f, hgt * 0.24f, len * 0.42f),
                    new Vector3(xTip, hgt * 0.16f, len * 0.40f));
                TriMat(w, accent,
                    new Vector3(xTip, hgt * 0.18f, len * 0.46f),
                    new Vector3(xTip - side * hw * 0.02f, hgt * 0.22f, len * 0.50f),
                    new Vector3(xTip, hgt * 0.14f, len * 0.48f));
            }
        }
    }

    /// <summary>Post-pipeline high-lum amber accent tris for Korath capitals — bypasses RecolorTrussNasa wash.</summary>
    public static void AppendTrussCapitalAccentBands(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDreadnought = hullKey is "dreadnought";
        if (!isCarrier && !isDreadnought)
            return;

        var amber = new Vector3(0.96f, 0.78f, 0.44f);
        float hw = wid * 0.5f;

        if (isCarrier)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xOut = side * hw * 1.08f;
                for (int tier = 0; tier < 2; tier++)
                {
                    float z = len * (0.36f + tier * 0.06f);
                    float yBase = hgt * (0.14f + tier * 0.04f);
                    w.TriColored(
                        new Vector3(xOut, yBase, z),
                        new Vector3(xOut - side * hw * 0.03f, yBase + hgt * 0.04f, z + len * 0.008f),
                        new Vector3(xOut, yBase - hgt * 0.04f, z + len * 0.012f),
                        amber);
                }
            }
        }

        if (isDreadnought)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xTip = side * hw * 0.82f;
                w.TriColored(
                    new Vector3(xTip, hgt * 0.24f, len * 0.44f),
                    new Vector3(xTip - side * hw * 0.03f, hgt * 0.28f, len * 0.48f),
                    new Vector3(xTip, hgt * 0.20f, len * 0.46f),
                    amber);
                w.TriColored(
                    new Vector3(0, hgt * 0.30f, len * 0.48f),
                    new Vector3(xTip * 0.40f, hgt * 0.26f, len * 0.44f),
                    new Vector3(xTip * 0.40f, hgt * 0.34f, len * 0.50f),
                    amber * 0.96f);
                w.TriColored(
                    new Vector3(xTip, hgt * 0.16f, len * 0.52f),
                    new Vector3(xTip - side * hw * 0.02f, hgt * 0.20f, len * 0.56f),
                    new Vector3(0, hgt * 0.24f, len * 0.54f),
                    amber * 0.94f);
            }
        }
    }

    /// <summary>Wide envelope truss booms — capital-scale structural framing readable at fleet distance.</summary>
    private static void AddTrussCapitalEnvelopeBooms(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var frame = RaceMeshWriter.HullMaterial.Truss;
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        float boomReach = isCarrier ? 1.14f : isCruiser ? 0.92f : 1.12f;
        int crossMembers = isCarrier ? 2 : isCruiser ? 2 : 2;

        for (int side = -1; side <= 1; side += 2)
        {
            float xBoom = side * hw * boomReach;
            for (int c = 0; c < crossMembers; c++)
            {
                float z = MathHelper.Lerp(-len * 0.08f, len * 0.24f, c / MathF.Max(1f, crossMembers - 1));
                TriMat(w, frame,
                    new Vector3(xBoom, hgt * 0.28f, z),
                    new Vector3(xBoom - side * hw * 0.06f, hgt * 0.34f, z + len * 0.012f),
                    new Vector3(xBoom, hgt * 0.22f, z + len * 0.014f));
            }
        }
    }

    /// <summary>Gameplay-readable engine/weapon/shield markers on Korath truss hulls.</summary>
    private static void AddTrussGameplayComponentBands(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;
        bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";
        bool isGunship = hullKey is "gunship" or "gunship_heavy";
        bool isBomber = hullKey is "bomber" or "bomber_heavy";
        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDreadnought = hullKey is "dreadnought";

        if (hullKey is "scout" or "scout_light")
        {
            TriMat(w, shield,
                new Vector3(-hw * 0.05f, hgt * 0.74f, len * 0.08f), new Vector3(hw * 0.05f, hgt * 0.74f, len * 0.08f),
                new Vector3(0, hgt * 0.80f, len * 0.10f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xPod = side * hw * 0.52f;
                TriMat(w, weapon,
                    new Vector3(xPod, hgt * 0.14f, len * 0.06f),
                    new Vector3(xPod - side * hw * 0.04f, hgt * 0.10f, len * 0.09f),
                    new Vector3(xPod, hgt * 0.08f, len * 0.08f));
            }
            TriMat(w, engine,
                new Vector3(-hw * 0.06f, hgt * 0.06f, -len * 0.08f), new Vector3(hw * 0.06f, hgt * 0.06f, -len * 0.08f),
                new Vector3(0, hgt * 0.12f, -len * 0.06f));
            return;
        }

        if (hullKey is "interceptor" or "interceptor_mk2")
        {
            TriMat(w, weapon,
                new Vector3(-hw * 0.08f, hgt * 0.10f, len * 0.32f), new Vector3(hw * 0.08f, hgt * 0.10f, len * 0.32f),
                new Vector3(0, hgt * 0.16f, len * 0.36f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xPod = side * hw * 0.46f;
                TriMat(w, weapon,
                    new Vector3(xPod, hgt * 0.12f, len * 0.04f),
                    new Vector3(xPod - side * hw * 0.04f, hgt * 0.08f, len * 0.07f),
                    new Vector3(xPod, hgt * 0.06f, len * 0.06f));
            }
            TriMat(w, engine,
                new Vector3(-hw * 0.10f, hgt * 0.06f, -len * 0.06f), new Vector3(hw * 0.10f, hgt * 0.06f, -len * 0.06f),
                new Vector3(0, hgt * 0.12f, -len * 0.04f));
            TriMat(w, shield,
                new Vector3(-hw * 0.05f, hgt * 0.72f, len * 0.06f), new Vector3(hw * 0.05f, hgt * 0.72f, len * 0.06f),
                new Vector3(0, hgt * 0.78f, len * 0.08f));
            return;
        }

        if (hullKey is "drone" or "drone_swarm")
        {
            TriMat(w, engine,
                new Vector3(-hw * 0.08f, hgt * 0.06f, -len * 0.18f), new Vector3(hw * 0.08f, hgt * 0.06f, -len * 0.18f),
                new Vector3(0, hgt * 0.12f, -len * 0.14f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xPod = side * hw * 0.28f;
                TriMat(w, weapon,
                    new Vector3(xPod, hgt * 0.10f, len * 0.10f),
                    new Vector3(xPod - side * hw * 0.03f, hgt * 0.06f, len * 0.12f),
                    new Vector3(xPod, hgt * 0.04f, len * 0.11f));
            }
            TriMat(w, shield,
                new Vector3(-hw * 0.05f, hgt * 0.70f, len * 0.06f), new Vector3(hw * 0.05f, hgt * 0.70f, len * 0.06f),
                new Vector3(0, hgt * 0.76f, len * 0.08f));
            return;
        }

        if (hullKey is "fighter_basic" or "hero_default")
        {
            TriMat(w, engine,
                new Vector3(-hw * 0.12f, hgt * 0.06f, -len * 0.08f), new Vector3(hw * 0.12f, hgt * 0.06f, -len * 0.08f),
                new Vector3(0, hgt * 0.12f, -len * 0.06f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xPod = side * hw * (hullKey is "hero_default" ? 0.50f : 0.44f);
                TriMat(w, weapon,
                    new Vector3(xPod, hgt * 0.10f, len * 0.04f),
                    new Vector3(xPod - side * hw * 0.04f, hgt * 0.06f, len * 0.07f),
                    new Vector3(xPod, hgt * 0.04f, len * 0.06f));
            }
            TriMat(w, shield,
                new Vector3(-hw * 0.06f, hgt * (hullKey is "hero_default" ? 0.76f : 0.72f), len * 0.08f),
                new Vector3(hw * 0.06f, hgt * (hullKey is "hero_default" ? 0.76f : 0.72f), len * 0.08f),
                new Vector3(0, hgt * (hullKey is "hero_default" ? 0.82f : 0.78f), len * 0.10f));
            return;
        }

        if (isCruiser)
        {
            TriMat(w, engine,
                new Vector3(-hw * 0.12f, hgt * 0.08f, -len * 0.22f), new Vector3(hw * 0.12f, hgt * 0.08f, -len * 0.22f),
                new Vector3(0, hgt * 0.14f, -len * 0.18f));
            TriMat(w, weapon,
                new Vector3(-hw * 0.08f, hgt * 0.18f, len * 0.62f), new Vector3(hw * 0.08f, hgt * 0.18f, len * 0.62f),
                new Vector3(0, hgt * 0.24f, len * 0.68f));
            TriMat(w, shield,
                new Vector3(-hw * 0.07f, hgt * 0.76f, len * 0.08f), new Vector3(hw * 0.07f, hgt * 0.76f, len * 0.08f),
                new Vector3(0, hgt * 0.82f, len * 0.11f));
            return;
        }

        if (isCarrier)
        {
            TriMat(w, engine,
                new Vector3(-hw * 0.12f, hgt * 0.08f, -len * 0.22f), new Vector3(hw * 0.12f, hgt * 0.08f, -len * 0.22f),
                new Vector3(0, hgt * 0.14f, -len * 0.18f));
            TriMat(w, weapon,
                new Vector3(-hw * 0.34f, hgt * 0.30f, len * 0.04f), new Vector3(hw * 0.34f, hgt * 0.30f, len * 0.04f),
                new Vector3(0, hgt * 0.34f, len * 0.08f));
            TriMat(w, weapon,
                new Vector3(-hw * 0.22f, hgt * 0.12f, -len * 0.04f), new Vector3(hw * 0.22f, hgt * 0.12f, -len * 0.04f),
                new Vector3(0, hgt * 0.16f, len * 0.02f));
            TriMat(w, shield,
                new Vector3(hw * 0.18f, hgt * 0.68f, len * 0.08f),
                new Vector3(hw * 0.22f, hgt * 0.74f, len * 0.10f),
                new Vector3(hw * 0.16f, hgt * 0.64f, len * 0.11f));
            return;
        }

        if (isDreadnought)
        {
            TriMat(w, engine,
                new Vector3(-hw * 0.14f, hgt * 0.08f, -len * 0.24f), new Vector3(hw * 0.14f, hgt * 0.08f, -len * 0.24f),
                new Vector3(0, hgt * 0.16f, -len * 0.20f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xPod = side * hw * 0.50f;
                TriMat(w, weapon,
                    new Vector3(xPod, hgt * 0.12f, len * 0.68f),
                    new Vector3(xPod - side * hw * 0.05f, hgt * 0.08f, len * 0.72f),
                    new Vector3(xPod, hgt * 0.06f, len * 0.70f));
            }
            TriMat(w, shield,
                new Vector3(-hw * 0.07f, hgt * 0.78f, len * 0.08f), new Vector3(hw * 0.07f, hgt * 0.78f, len * 0.08f),
                new Vector3(0, hgt * 0.84f, len * 0.11f));
            return;
        }

        TriMat(w, engine,
            new Vector3(-hw * 0.14f, hgt * 0.06f, -len * 0.10f), new Vector3(hw * 0.14f, hgt * 0.06f, -len * 0.10f),
            new Vector3(0, hgt * 0.12f, -len * 0.06f));

        if (isGunship)
        {
            TriMat(w, weapon,
                new Vector3(-hw * 0.12f, hgt * 0.02f, len * 0.16f), new Vector3(hw * 0.12f, hgt * 0.02f, len * 0.16f),
                new Vector3(0, hgt * 0.10f, len * 0.24f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xBay = side * hw * 0.42f;
                TriMat(w, weapon,
                    new Vector3(xBay, hgt * 0.06f, len * 0.04f),
                    new Vector3(xBay - side * hw * 0.04f, hgt * 0.02f, len * 0.06f),
                    new Vector3(xBay, hgt * 0.01f, -len * 0.02f));
            }
        }
        else if (isBomber)
        {
            TriMat(w, weapon,
                new Vector3(-hw * 0.28f, hgt * 0.04f, len * 0.02f),
                new Vector3(hw * 0.28f, hgt * 0.04f, len * 0.02f),
                new Vector3(0, hgt * 0.10f, len * 0.08f));
        }
        else if (isDestroyer)
        {
            TriMat(w, weapon,
                new Vector3(-hw * 0.10f, hgt * 0.08f, len * 0.50f), new Vector3(hw * 0.10f, hgt * 0.08f, len * 0.50f),
                new Vector3(0, hgt * 0.16f, len * 0.56f));
        }
        else
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xBay = side * hw * 0.48f;
                TriMat(w, weapon,
                    new Vector3(xBay, hgt * 0.06f, len * 0.02f),
                    new Vector3(xBay - side * hw * 0.04f, hgt * 0.02f, len * 0.04f),
                    new Vector3(xBay, hgt * 0.01f, -len * 0.02f));
            }
        }

        TriMat(w, shield,
            new Vector3(-hw * 0.06f, hgt * 0.70f, len * 0.06f), new Vector3(hw * 0.06f, hgt * 0.70f, len * 0.06f),
            new Vector3(0, hgt * 0.74f, len * 0.09f));
    }

    /// <summary>Belly/flank truss substrate bands for korath utility hulls — luminance depth under team tint.</summary>
    private static void AddTrussUtilitySubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        bool isCargo = hullKey is "freighter_bulk" or "transport_cargo";
        bool isMiner = hullKey is "miner_basic" or "miner_eva" or "miner_tractor";
        bool isSupport = hullKey is "support_repair";
        int bellyPlates = isCargo ? 2 : isSupport ? 3 : 3;

        for (int i = 0; i < bellyPlates; i++)
        {
            float t = i / MathF.Max(1f, bellyPlates - 1);
            float z = MathHelper.Lerp(-len * 0.06f, len * (isCargo ? 0.24f : isSupport ? 0.18f : 0.18f), t);
            float halfW = hw * MathHelper.Lerp(
                hullKey is "freighter_bulk" ? 0.30f : isCargo ? 0.24f : 0.26f,
                hullKey is "freighter_bulk" ? 0.16f : 0.12f, t);
            TriMat(w, panel,
                new Vector3(-halfW, hgt * 0.02f, z), new Vector3(halfW, hgt * 0.02f, z),
                new Vector3(0, hgt * (0.16f + t * 0.02f), z + len * 0.012f));
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * hw * (hullKey is "freighter_bulk" ? 0.26f : isCargo ? 0.22f : 0.24f);
            int trussSegs = isCargo ? 1 : isSupport ? 3 : 2;
            for (int s = 0; s < trussSegs; s++)
            {
                float z = MathHelper.Lerp(-len * 0.02f, len * (isCargo ? 0.18f : isSupport ? 0.14f : 0.14f), s / MathF.Max(1f, trussSegs - 1));
                TriMat(w, frame,
                    new Vector3(x, hgt * 0.16f, z), new Vector3(x - side * hw * 0.035f, hgt * 0.26f, z + len * 0.012f),
                    new Vector3(x, hgt * 0.12f, z + len * 0.014f));
            }
        }

        int spineSegs = isCargo ? 2 : isSupport ? 4 : isMiner ? 3 : 3;
        for (int i = 0; i < spineSegs; i++)
        {
            float t = i / MathF.Max(1f, spineSegs - 1);
            float z = MathHelper.Lerp(-len * 0.04f, len * (hullKey switch
            {
                "freighter_bulk" => 0.42f,
                "transport_cargo" => 0.40f,
                "miner_basic" => 0.26f,
                "miner_eva" => 0.20f,
                "miner_tractor" => 0.24f,
                "support_repair" => 0.20f,
                _ => 0.18f
            }), t);
            float xSpan = hw * MathHelper.Lerp(0.14f, 0.04f, t);
            TriMat(w, panel,
                new Vector3(-xSpan, hgt * (0.46f + t * 0.06f), z), new Vector3(xSpan, hgt * (0.46f + t * 0.06f), z),
                new Vector3(0, hgt * (0.56f + t * 0.04f), z + len * 0.010f));
            if ((isMiner || isSupport) && i % 2 == 0)
            {
                TriMat(w, accent,
                    new Vector3(-xSpan * 0.7f, hgt * (0.54f + t * 0.04f), z + len * 0.006f),
                    new Vector3(xSpan * 0.7f, hgt * (0.54f + t * 0.04f), z + len * 0.006f),
                    new Vector3(0, hgt * (0.60f + t * 0.03f), z + len * 0.012f));
            }
        }
    }

    /// <summary>Gameplay-readable engine/weapon/shield markers on korath industrial hulls.</summary>
    private static void AddTrussUtilityGameplayComponentBands(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;

        TriMat(w, engine,
            new Vector3(-hw * 0.14f, hgt * 0.06f, -len * 0.10f), new Vector3(hw * 0.14f, hgt * 0.06f, -len * 0.10f),
            new Vector3(0, hgt * 0.12f, -len * 0.06f));

        if (hullKey is "miner_basic")
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xTip = side * hw * 0.72f;
                float xRoot = side * hw * 0.36f;
                TriMat(w, weapon,
                    new Vector3(xRoot, hgt * 0.16f, len * 0.12f),
                    new Vector3(xTip, hgt * 0.20f, len * 0.38f),
                    new Vector3(xRoot, hgt * 0.10f, len * 0.20f));
            }
            return;
        }

        if (hullKey is "support_repair")
        {
            TriMat(w, shield,
                new Vector3(-hw * 0.04f, hgt * 0.78f, len * 0.06f), new Vector3(hw * 0.04f, hgt * 0.78f, len * 0.06f),
                new Vector3(0, hgt * 0.84f, len * 0.08f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xTip = side * hw * 0.74f;
                TriMat(w, weapon,
                    new Vector3(xTip, hgt * 0.56f, len * 0.12f),
                    new Vector3(xTip + side * hw * 0.06f, hgt * 0.62f, len * 0.14f),
                    new Vector3(xTip, hgt * 0.50f, len * 0.16f));
            }
            return;
        }

        if (hullKey is "miner_eva")
        {
            TriMat(w, shield,
                new Vector3(-hw * 0.06f, hgt * 0.72f, len * 0.10f), new Vector3(hw * 0.06f, hgt * 0.72f, len * 0.10f),
                new Vector3(0, hgt * 0.78f, len * 0.12f));
            TriMat(w, weapon,
                new Vector3(hw * 0.48f, hgt * 0.36f, len * 0.10f),
                new Vector3(hw * 0.56f, hgt * 0.42f, len * 0.14f),
                new Vector3(hw * 0.50f, hgt * 0.30f, len * 0.12f));
            return;
        }

        if (hullKey is "miner_tractor")
        {
            TriMat(w, engine,
                new Vector3(-hw * 0.08f, hgt * 0.18f, len * 0.36f), new Vector3(hw * 0.08f, hgt * 0.18f, len * 0.36f),
                new Vector3(0, hgt * 0.24f, len * 0.40f));
            TriMat(w, weapon,
                new Vector3(-hw * 0.10f, hgt * 0.68f, len * 0.38f), new Vector3(hw * 0.10f, hgt * 0.68f, len * 0.38f),
                new Vector3(0, hgt * 0.76f, len * 0.42f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xTip = side * hw * 0.68f;
                TriMat(w, weapon,
                    new Vector3(xTip, hgt * 0.34f, len * 0.14f),
                    new Vector3(xTip + side * hw * 0.06f, hgt * 0.40f, len * 0.16f),
                    new Vector3(xTip, hgt * 0.30f, len * 0.18f));
            }
            return;
        }

        if (hullKey is "transport_cargo" or "freighter_bulk")
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float cx = side * hw * (hullKey is "freighter_bulk" ? 0.42f : 0.46f);
                TriMat(w, weapon,
                    new Vector3(cx - hw * 0.08f, hgt * 0.26f, len * (hullKey is "freighter_bulk" ? 0.34f : 0.32f)),
                    new Vector3(cx + hw * 0.08f, hgt * 0.26f, len * (hullKey is "freighter_bulk" ? 0.34f : 0.32f)),
                    new Vector3(cx, hgt * 0.32f, len * (hullKey is "freighter_bulk" ? 0.38f : 0.36f)));
            }
            TriMat(w, engine,
                new Vector3(-hw * 0.16f, hgt * 0.04f, -len * 0.10f), new Vector3(hw * 0.16f, hgt * 0.04f, -len * 0.10f),
                new Vector3(0, hgt * 0.10f, -len * 0.06f));
            TriMat(w, shield,
                new Vector3(-hw * 0.05f, hgt * 0.66f, len * 0.08f), new Vector3(hw * 0.05f, hgt * 0.66f, len * 0.08f),
                new Vector3(0, hgt * 0.70f, len * 0.10f));
        }
    }

    private static void ApplyOrganicUtilityShipDetail(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        var vein = RaceMeshWriter.HullMaterial.Solar;
        var shadow = RaceMeshWriter.HullMaterial.Radiator;
        var engineMat = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        float hw = wid * 0.5f;
        float widthScale = hullKey is "freighter_bulk" ? 0.90f : hullKey is "miner_eva" ? 0.92f : 0.96f;
        hw *= widthScale;
        bool triBudgetTight = hullKey is "freighter_bulk" or "transport_cargo" or "miner_basic"
            or "miner_eva" or "miner_tractor";
        bool isMiner = hullKey is "miner_basic" or "miner_eva" or "miner_tractor";
        int veinSegs = hullKey is "freighter_bulk" ? 1 : triBudgetTight ? 2 : isMiner ? 2 : 4;

        for (int side = -1; side <= 1; side += 2)
        {
            float xOut = side * hw * (hullKey is "freighter_bulk" ? 0.48f : hullKey is "transport_cargo" ? 0.46f : 0.56f);
            for (int i = 0; i < veinSegs; i++)
            {
                float t = i / MathF.Max(1f, veinSegs - 1);
                float z = MathHelper.Lerp(-len * 0.04f, len * (hullKey is "freighter_bulk" ? 0.34f : triBudgetTight ? 0.32f : isMiner ? 0.20f : hullKey is "support_repair" ? 0.16f : 0.14f), t);
                TriMat(w, vein,
                    new Vector3(xOut, hgt * (0.16f + t * 0.05f), z),
                    new Vector3(xOut - side * hw * 0.035f, hgt * (0.13f + t * 0.05f), z + len * 0.010f),
                    new Vector3(xOut, hgt * (0.11f + t * 0.04f), z + len * 0.014f));
            }
        }

        if (hullKey is "support_repair")
        {
            for (int a = 0; a < 2; a++)
            {
                float z = len * (0.06f + a * 0.060f);
                TriMat(w, vein,
                    new Vector3(-hw * 0.04f, hgt * (0.76f + a * 0.04f), z),
                    new Vector3(hw * 0.04f, hgt * (0.76f + a * 0.04f), z),
                    new Vector3(0, hgt * (0.84f + a * 0.04f), z + len * 0.008f));
            }
            TriMat(w, vein,
                new Vector3(-hw * 0.03f, hgt * 1.00f, len * 0.16f),
                new Vector3(hw * 0.03f, hgt * 1.00f, len * 0.16f),
                new Vector3(0, hgt * 1.08f, len * 0.18f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xBoom = side * hw * 0.66f;
                TriMat(w, weapon,
                    new Vector3(xBoom, hgt * 0.56f, len * 0.12f),
                    new Vector3(xBoom + side * hw * 0.05f, hgt * 0.62f, len * 0.14f),
                    new Vector3(xBoom, hgt * 0.50f, len * 0.16f));
            }
        }

        if (hullKey is "transport_cargo" && !triBudgetTight)
        {
            for (int s = 0; s < 2; s++)
            {
                float z = len * (0.10f + s * 0.10f);
                TriMat(w, vein,
                    new Vector3(-hw * 0.06f, hgt * 0.42f, z),
                    new Vector3(hw * 0.06f, hgt * 0.42f, z),
                    new Vector3(0, hgt * 0.48f, z + len * 0.010f));
            }
        }

        if (hullKey is "freighter_bulk" && !triBudgetTight)
        {
            TriMat(w, vein,
                new Vector3(-hw * 0.08f, hgt * 0.36f, len * 0.38f),
                new Vector3(hw * 0.08f, hgt * 0.36f, len * 0.38f),
                new Vector3(0, hgt * 0.42f, len * 0.44f));
        }

        if (!triBudgetTight)
            AddOrganicUtilitySubstrate(w, hullKey, hw, hgt, len);

        if (!triBudgetTight)
        {
            TriMat(w, engineMat,
                new Vector3(-hw * 0.12f, 0, -len * 0.08f), new Vector3(hw * 0.12f, 0, -len * 0.08f),
                new Vector3(0, hgt * 0.06f, -len * 0.12f));
        }
    }

    /// <summary>Belly/flank organic membrane substrate bands — teal vein accents under team tint.</summary>
    private static void AddOrganicUtilitySubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var membrane = RaceMeshWriter.HullMaterial.Hull;
        var vein = RaceMeshWriter.HullMaterial.Solar;
        var shadow = RaceMeshWriter.HullMaterial.Radiator;
        bool isCargo = hullKey is "freighter_bulk" or "transport_cargo";
        bool isMiner = hullKey is "miner_basic" or "miner_eva" or "miner_tractor";
        bool isSupport = hullKey is "support_repair";
        // Loop-10 tri trim: utility roster geometry =180 sweet max.
        int bellyPlates = isCargo ? 2 : isSupport ? 2 : isMiner ? 2 : 3;

        for (int i = 0; i < bellyPlates; i++)
        {
            float t = i / MathF.Max(1f, bellyPlates - 1);
            float z = MathHelper.Lerp(-len * 0.06f, len * (isCargo ? 0.26f : isSupport ? 0.18f : isMiner ? 0.20f : 0.16f), t);
            float halfW = hw * MathHelper.Lerp(
                hullKey is "freighter_bulk" ? 0.28f : isCargo ? 0.22f : 0.24f,
                hullKey is "freighter_bulk" ? 0.14f : 0.10f, t);
            TriMat(w, shadow,
                new Vector3(-halfW, hgt * 0.02f, z), new Vector3(halfW, hgt * 0.02f, z),
                new Vector3(0, hgt * (0.16f + t * 0.03f), z + len * 0.012f));
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * hw * (hullKey is "freighter_bulk" ? 0.24f : isCargo ? 0.18f : 0.22f);
            int flankSegs = isCargo ? 2 : isSupport ? 2 : isMiner ? 2 : 2;
            for (int s = 0; s < flankSegs; s++)
            {
                float z = MathHelper.Lerp(-len * 0.02f, len * (isCargo ? 0.18f : isSupport ? 0.14f : isMiner ? 0.16f : 0.12f), s / MathF.Max(1f, flankSegs - 1));
                TriMat(w, membrane,
                    new Vector3(x, hgt * 0.14f, z), new Vector3(x - side * hw * 0.03f, hgt * 0.26f, z + len * 0.012f),
                    new Vector3(x, hgt * 0.10f, z + len * 0.014f));
            }
        }

        int spineSegs = isCargo ? 2 : isSupport ? 2 : isMiner ? 2 : 3;
        for (int i = 0; i < spineSegs; i++)
        {
            float t = i / MathF.Max(1f, spineSegs - 1);
            float z = MathHelper.Lerp(-len * 0.04f, len * (hullKey switch
            {
                "freighter_bulk" => 0.44f,
                "transport_cargo" => 0.42f,
                "miner_basic" => 0.30f,
                "miner_eva" => 0.24f,
                "miner_tractor" => 0.28f,
                "support_repair" => 0.22f,
                _ => 0.16f
            }), t);
            float xSpan = hw * MathHelper.Lerp(0.12f, 0.03f, t);
            TriMat(w, shadow,
                new Vector3(-xSpan, hgt * (0.44f + t * 0.06f), z), new Vector3(xSpan, hgt * (0.44f + t * 0.06f), z),
                new Vector3(0, hgt * (0.56f + t * 0.04f), z + len * 0.010f));
            if (isMiner || isSupport || isCargo)
            {
                TriMat(w, vein,
                    new Vector3(-xSpan * 0.7f, hgt * (0.54f + t * 0.04f), z + len * 0.006f),
                    new Vector3(xSpan * 0.7f, hgt * (0.54f + t * 0.04f), z + len * 0.006f),
                    new Vector3(0, hgt * (0.60f + t * 0.03f), z + len * 0.012f));
            }
        }
    }

    private static void ApplyVasudanShipDetail(
        RaceMeshWriter w, RaceVisualDefinition race, string hullKey, float len, float wid, float hgt)
    {
        bool isUtility = hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk" or "support_repair";
        bool isCapital = hullKey is "destroyer" or "destroyer_assault"
            or "cruiser" or "cruiser_heavy"
            or "carrier" or "carrier_command"
            or "dreadnought";
        bool isSolidCraft = hullKey is "fighter" or "fighter_basic"
            or "hero" or "hero_default"
            or "scout" or "scout_light"
            or "interceptor" or "interceptor_mk2"
            or "drone" or "drone_swarm"
            or "corvette" or "corvette_fast" or "frigate" or "frigate_strike" or "gunship" or "gunship_heavy" or "bomber" or "bomber_heavy";
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var engineMat = RaceMeshWriter.HullMaterial.Engine;

        if (isUtility)
        {
            float hw = wid * 0.5f;
            float widthScale = hullKey is "freighter_bulk" ? 1.02f : hullKey is "transport_cargo" ? 0.98f : hullKey is "miner_eva" ? 1.02f : 0.96f;
            hw *= widthScale;
            bool triBudgetTight = hullKey is "freighter_bulk" or "transport_cargo" or "miner_basic";
            bool isSupport = hullKey is "support_repair";
            int lateralSegs = hullKey is "freighter_bulk" ? 1 : triBudgetTight ? 2 : isSupport ? 3 : 5;
            int dorsalSegs = hullKey is "freighter_bulk" ? 1 : triBudgetTight ? 2 : isSupport ? 3 : 5;
            int bellySegs = triBudgetTight ? 2 : isSupport ? 2 : 4;
            float utilityLateralZ = hullKey is "freighter_bulk" ? 0.26f
                : hullKey is "transport_cargo" ? 0.28f
                : hullKey is "miner_basic" ? 0.16f
                : 0.14f;
            float utilityDorsalZ = hullKey is "freighter_bulk" ? 0.28f
                : hullKey is "transport_cargo" ? 0.30f
                : hullKey is "miner_basic" ? 0.18f
                : 0.16f;

            for (int side = -1; side <= 1; side += 2)
            {
                float xOut = side * hw * (hullKey is "freighter_bulk" ? 0.78f : hullKey is "transport_cargo" ? 0.74f : hullKey is "miner_eva" ? 0.76f : 0.68f);
                for (int i = 0; i < lateralSegs; i++)
                {
                    float t = i / MathF.Max(1f, lateralSegs - 1);
                    float z = MathHelper.Lerp(-len * 0.04f, len * utilityLateralZ, t);
                    TriMat(w, accent,
                        new Vector3(xOut, hgt * (0.16f + t * 0.05f), z),
                        new Vector3(xOut - side * hw * 0.04f, hgt * (0.13f + t * 0.05f), z + len * 0.010f),
                        new Vector3(xOut, hgt * (0.11f + t * 0.04f), z + len * 0.014f));
                }
            }

            for (int p = 0; p < dorsalSegs; p++)
            {
                float t = p / MathF.Max(1f, dorsalSegs - 1);
                float z = MathHelper.Lerp(-len * 0.06f, len * utilityDorsalZ, t);
                float xSpan = hw * MathHelper.Lerp(0.38f, 0.14f, t);
                TriMat(w, accent,
                    new Vector3(-xSpan, hgt * (0.36f + t * 0.05f), z), new Vector3(xSpan, hgt * (0.36f + t * 0.05f), z),
                    new Vector3(0, hgt * (0.42f + t * 0.05f), z + len * 0.008f));
            }

            for (int b = 0; b < bellySegs; b++)
            {
                float bellyZEnd = hullKey is "transport_cargo" ? 0.22f : hullKey is "miner_basic" ? 0.20f : triBudgetTight ? 0.20f : 0.18f;
                float z = MathHelper.Lerp(-len * 0.06f, len * bellyZEnd, b / MathF.Max(1f, bellySegs - 1));
                TriMat(w, accent,
                    new Vector3(-hw * 0.22f, hgt * 0.38f, z), new Vector3(hw * 0.22f, hgt * 0.38f, z),
                    new Vector3(0, hgt * 0.44f, z + len * 0.010f));
            }

            if (hullKey is "support_repair")
            {
                for (int a = 0; a < 2; a++)
                {
                    float z = len * (0.06f + a * 0.05f);
                    TriMat(w, accent,
                        new Vector3(-hw * 0.04f, hgt * (0.74f + a * 0.02f), z),
                        new Vector3(hw * 0.04f, hgt * (0.74f + a * 0.02f), z),
                        new Vector3(0, hgt * (0.80f + a * 0.02f), z + len * 0.008f));
                }
            }

            AddVasudanUtilitySubstrate(w, hullKey, hw, hgt, len);
            AddVasudanUtilityGameplayComponentBands(w, hullKey, hw, hgt, len);
            if (hullKey is "miner_basic")
                AddVasudanMinerBasicSilhouetteAccents(w, hw, hgt, len);
            else if (hullKey is "transport_cargo")
                AddVasudanTransportCargoSilhouetteAccents(w, hw, hgt, len);
            else if (hullKey is "freighter_bulk")
                AddVasudanFreighterBulkSilhouetteAccents(w, hw, hgt, len);
            if (!triBudgetTight)
            {
                TriMat(w, engineMat,
                    new Vector3(-hw * 0.12f, 0, -len * 0.08f), new Vector3(hw * 0.12f, 0, -len * 0.08f),
                    new Vector3(0, hgt * 0.06f, -len * 0.12f));
            }
            return;
        }

        if (isCapital)
        {
            float hw = wid * 0.5f;
            bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
            int dorsalSegs = hullKey is "dreadnought" ? 6
                : isCruiser ? 2
                : hullKey is "carrier" or "carrier_command" ? 3
                : hullKey is "destroyer_assault" ? 3
                : 5;
            float dorsalBaseY = isCruiser ? 0.50f : 0.56f;
            float dorsalRise = isCruiser ? 0.05f : 0.06f;
            float dorsalSideY = isCruiser ? 0.40f : 0.44f;
            float dorsalZEnd = isCruiser ? 0.72f : 0.68f;
            for (int i = 0; i < dorsalSegs; i++)
            {
                float t = i / MathF.Max(1f, dorsalSegs - 1);
                float z = MathHelper.Lerp(len * 0.08f, len * dorsalZEnd, t);
                TriMat(w, accent,
                    new Vector3(0, hgt * (dorsalBaseY + t * dorsalRise), z),
                    new Vector3(-hw * 0.10f, hgt * (dorsalSideY + t * 0.04f), z - len * 0.02f),
                    new Vector3(hw * 0.10f, hgt * (dorsalSideY + t * 0.04f), z - len * 0.02f));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float xOut = side * hw * (hullKey is "dreadnought" ? 1.52f
                    : hullKey is "carrier" or "carrier_command" ? 1.08f
                    : hullKey is "cruiser_heavy" ? 1.72f
                    : hullKey is "destroyer_assault" ? 1.82f
                    : 1.98f);
                int edgeSegs = hullKey is "cruiser_heavy" ? 0
                    : hullKey is "carrier" or "carrier_command" ? 2
                    : hullKey is "destroyer_assault" ? 3 : 4;
                for (int e = 0; e < edgeSegs; e++)
                {
                    float z = MathHelper.Lerp(len * 0.20f, len * 0.62f, e / MathF.Max(1f, edgeSegs - 1));
                    TriMat(w, accent,
                        new Vector3(xOut, hgt * (0.18f + e * 0.02f), z),
                        new Vector3(xOut - side * hw * 0.04f, hgt * (0.16f + e * 0.02f), z + len * 0.006f),
                        new Vector3(xOut, hgt * (0.14f + e * 0.02f), z + len * 0.010f));
                }
            }
            if (hullKey is "destroyer" or "destroyer_assault")
                AddVasudanDestroyerAssaultSilhouetteAccents(w, hw, hgt, len);
            AddVasudanCapitalSubstrate(w, hullKey, hw, hgt, len);
            if (hullKey is "destroyer" or "destroyer_assault" or "carrier" or "carrier_command" or "dreadnought")
                AddVasudanGameplayComponentBands(w, hullKey, hw, hgt, len);
            return;
        }

        if (isSolidCraft)
        {
            float hw = wid * 0.5f;
            bool isDrone = hullKey is "drone" or "drone_swarm";
            bool isDroneSwarm = hullKey is "drone_swarm";
            bool isScout = hullKey is "scout" or "scout_light";
            bool isHero = hullKey is "hero" or "hero_default";
            int wingStrips = isDroneSwarm ? 2 : isDrone ? 4 : hullKey is "scout_light" ? 4 : isScout ? 6 : isHero ? 0 : hullKey is "interceptor_mk2" ? 0 : 5;
            bool isMedium = hullKey is "corvette" or "corvette_fast" or "frigate" or "frigate_strike"
                or "gunship" or "gunship_heavy" or "bomber" or "bomber_heavy";
            bool isWideMedium = hullKey is "gunship" or "gunship_heavy" or "bomber" or "bomber_heavy";
            float wingSpan = hullKey is "scout_light" ? 1.88f
                : isScout ? 2.08f
                : hullKey is "interceptor" or "interceptor_mk2" ? 1.42f
                : isDroneSwarm ? 0.72f
                : isDrone ? 0.68f
                : hullKey is "corvette" or "corvette_fast" ? 1.14f
                : hullKey is "frigate" or "frigate_strike" ? 1.18f
                : hullKey is "gunship" or "gunship_heavy" ? 0.72f
                : hullKey is "bomber" or "bomber_heavy" ? 0.68f
                : isHero ? 1.05f : 1.02f;
            bool isFrigate = hullKey is "frigate" or "frigate_strike";
            if (isMedium) wingStrips = isWideMedium ? 3 : isFrigate ? 5 : 6;
            float leadingEdgeMul = isWideMedium ? 0.62f : 1.14f;
            int leadingEdgeSegs = hullKey is "scout_light" ? 3 : hullKey is "interceptor_mk2" ? 1 : isWideMedium ? 4 : isFrigate ? 4 : isHero ? 0 : 4;
            int spineBands = hullKey is "scout_light" ? 3 : isDroneSwarm ? 2 : isWideMedium ? 4 : isFrigate ? 4 : isHero ? 0 : hullKey is "interceptor_mk2" ? 1 : 4;
            int bellyAccents = hullKey is "scout_light" ? 2 : isDroneSwarm ? 2 : isWideMedium ? 2 : isHero ? 0 : hullKey is "interceptor_mk2" ? 1 : 3;

            if (!isHero)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    float xIn = side * hw * 0.34f;
                    float xOut = side * hw * 0.40f;
                    TriMat(w, accent,
                        new Vector3(xIn, hgt * 0.16f, len * 0.06f), new Vector3(xOut, hgt * 0.14f, len * 0.10f),
                        new Vector3(xIn, hgt * 0.12f, len * 0.11f));
                }
            }

            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < wingStrips; i++)
                {
                    float t = i / MathF.Max(1f, wingStrips - 1);
                    float z = MathHelper.Lerp(-len * 0.05f, len * 0.15f, t);
                    float xOut = side * hw * MathHelper.Lerp(wingSpan, 0.60f, t);
                    float xIn = side * hw * MathHelper.Lerp(0.28f, 0.42f, t);
                    float yEdge = hgt * (0.15f + t * 0.04f);
                    TriMat(w, accent,
                        new Vector3(xOut, yEdge, z), new Vector3(xIn, yEdge * 0.92f, z + len * 0.008f),
                        new Vector3(xOut, yEdge * 0.85f, z + len * 0.012f));
                }
            }

            for (int side = -1; side <= 1; side += 2)
            {
                float xLead = side * hw * leadingEdgeMul;
                for (int e = 0; e < leadingEdgeSegs; e++)
                {
                    float z = MathHelper.Lerp(len * 0.06f, -len * 0.03f, e / MathF.Max(1f, leadingEdgeSegs - 1));
                    TriMat(w, accent,
                        new Vector3(xLead, hgt * (0.20f + e * 0.015f), z),
                        new Vector3(xLead - side * hw * 0.03f, hgt * (0.18f + e * 0.015f), z + len * 0.008f),
                        new Vector3(xLead, hgt * (0.16f + e * 0.015f), z + len * 0.012f));
                }
            }

            for (int p = 0; p < spineBands; p++)
            {
                float t = p / MathF.Max(1f, spineBands - 1);
                float z = MathHelper.Lerp(-len * 0.06f, len * 0.18f, t);
                float xSpan = hw * MathHelper.Lerp(0.38f, 0.14f, t);
                TriMat(w, accent,
                    new Vector3(-xSpan, hgt * (0.34f + t * 0.04f), z), new Vector3(xSpan, hgt * (0.34f + t * 0.04f), z),
                    new Vector3(0, hgt * (0.38f + t * 0.05f), z + len * 0.006f));
            }

            for (int b = 0; b < bellyAccents; b++)
            {
                float z = MathHelper.Lerp(-len * 0.06f, len * 0.20f, b / MathF.Max(1f, bellyAccents - 1));
                TriMat(w, accent,
                    new Vector3(-hw * 0.22f, hgt * 0.36f, z), new Vector3(hw * 0.22f, hgt * 0.36f, z),
                    new Vector3(0, hgt * 0.40f, z + len * 0.008f));
            }

            if (!isHero)
                TriMat(w, accent,
                    new Vector3(-hw * 0.02f, hgt * 0.74f, len * 0.24f), new Vector3(hw * 0.02f, hgt * 0.74f, len * 0.24f),
                    new Vector3(0, hgt * 0.78f, len * 0.26f));

            if (isScout || isDrone)
            {
                int spineSegs = hullKey is "scout_light" ? 3 : isScout ? 5 : isDroneSwarm ? 3 : isDrone ? 5 : 6;
                float spineReach = isDroneSwarm ? 0.28f : hullKey is "scout_light" ? 0.16f : isScout ? 0.20f : 0.22f;
                for (int s = 0; s < spineSegs; s++)
                {
                    float t = s / MathF.Max(1f, spineSegs - 1);
                    float z = MathHelper.Lerp(-len * 0.04f, len * spineReach, t);
                    float yBase = hgt * (isDrone ? 0.58f + t * 0.28f : 0.48f + t * 0.22f);
                    TriMat(w, accent,
                        new Vector3(-hw * 0.06f, yBase, z), new Vector3(hw * 0.06f, yBase, z),
                        new Vector3(0, yBase + hgt * 0.08f, z + len * 0.006f));
                }
            }

            if (isDroneSwarm)
                AddVasudanDroneSwarmSpineGeometry(w, hw, hgt, len);

            bool isInterceptor = hullKey is "interceptor" or "interceptor_mk2";
            if (isScout || isInterceptor)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    float xTip = side * hw * (hullKey is "scout_light" ? 1.62f : isScout ? 2.02f : hullKey is "interceptor_mk2" ? 1.32f : isInterceptor ? 1.28f : 0.96f);
                    float zTip = len * (isScout ? 0.04f : 0.06f);
                    TriMat(w, accent,
                        new Vector3(xTip, hgt * 0.20f, zTip),
                        new Vector3(xTip - side * hw * 0.025f, hgt * 0.17f, zTip + len * 0.01f),
                        new Vector3(xTip, hgt * 0.14f, zTip + len * 0.012f));
                }
            }
            bool isReferenceCraft = hullKey is "fighter_basic" or "hero_default";
            bool isMediumCombat = hullKey is "corvette" or "corvette_fast" or "frigate" or "frigate_strike"
                or "gunship" or "gunship_heavy" or "bomber" or "bomber_heavy";
            if (isReferenceCraft)
            {
                AddVasudanReferenceCraftSubstrate(w, hw, hgt, len, isHero);
                if (hullKey is "hero_default")
                    AddVasudanHeroDefaultSilhouetteAccents(w, hw, hgt, len);
            }
            else if (isScout || isInterceptor || isDrone)
            {
                AddVasudanCompactCraftSubstrate(w, hullKey, hw, hgt, len);
                if (hullKey is "scout_light")
                    AddVasudanScoutLightSilhouetteAccents(w, hw, hgt, len);
                else if (hullKey is "interceptor_mk2")
                    AddVasudanInterceptorMk2SilhouetteAccents(w, hw, hgt, len);
            }
            else if (isMediumCombat)
                AddVasudanMediumCombatSubstrate(w, hullKey, hw, hgt, len);
            if (isReferenceCraft || isScout || isInterceptor || isDrone || isMediumCombat)
            {
                // Medium combat: component zones via lum snap on silhouette pods — skip redundant band tris (loop-4 tri discipline).
                if (hullKey is not "corvette" and not "corvette_fast"
                    and not "frigate" and not "frigate_strike"
                    and not "gunship" and not "gunship_heavy"
                    and not "bomber" and not "bomber_heavy"
                    and not "scout_light"
                    and not "hero_default")
                    AddVasudanGameplayComponentBands(w, hullKey, hw, hgt, len);
            }
            return;
        }

        float compact = hullKey is "scout" or "scout_light" or "interceptor" or "interceptor_mk2" ? 0.82f : 1f;
        var hullBand = RaceMeshWriter.HullMaterial.Truss;
        int bellyPlates = hullKey is "dreadnought" or "carrier" ? 7 : 5;
        for (int i = 0; i < bellyPlates; i++)
        {
            float t = (i + 0.5f) / bellyPlates;
            float z = MathHelper.Lerp(-len * 0.22f, len * 0.62f, t) * compact;
            float halfW = wid * 0.22f * (0.8f + 0.2f * MathF.Sin(t * MathF.PI));
            TriMat(w, panel,
                new Vector3(-halfW, hgt * 0.05f, z), new Vector3(halfW, hgt * 0.05f, z),
                new Vector3(0, hgt * 0.22f, z + len * 0.03f));
        }

        for (int i = 0; i < 5; i++)
        {
            float t = (i + 1f) / 6f;
            float z = MathHelper.Lerp(-len * 0.16f, len * 0.72f, t) * compact;
            TriMat(w, accent,
                new Vector3(0, hgt * 0.55f, z), new Vector3(-wid * 0.07f, hgt * 0.42f, z - len * 0.045f),
                new Vector3(wid * 0.07f, hgt * 0.42f, z - len * 0.045f));
        }

        if (hullKey is not "drone" and not "drone_swarm")
        {
            float sternZ = -len * 0.38f * compact;
            TriMat(w, engineMat,
                new Vector3(-wid * 0.14f, 0, sternZ - len * 0.045f), new Vector3(wid * 0.14f, 0, sternZ - len * 0.045f),
                new Vector3(0, hgt * 0.05f, sternZ - len * 0.09f));
        }
    }

    /// <summary>Belly/flank substrate bands for utility hulls — gunmetal plating depth under team tint.</summary>
    private static void AddVasudanUtilitySubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        bool isCargo = hullKey is "freighter_bulk" or "transport_cargo";
        bool isSupport = hullKey is "support_repair";
        int bellyPlates = isCargo ? 2 : isSupport ? 2 : 3;

        for (int i = 0; i < bellyPlates; i++)
        {
            float t = i / MathF.Max(1f, bellyPlates - 1);
            float z = MathHelper.Lerp(-len * 0.06f, len * (isCargo ? 0.24f : 0.18f), t);
            float halfW = hw * MathHelper.Lerp(
                hullKey is "freighter_bulk" ? 0.34f : 0.28f,
                hullKey is "freighter_bulk" ? 0.20f : 0.16f, t);
            TriMat(w, panel,
                new Vector3(-halfW, hgt * 0.02f, z), new Vector3(halfW, hgt * 0.02f, z),
                new Vector3(0, hgt * 0.16f, z + len * 0.012f));
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * hw * (hullKey is "freighter_bulk" ? 0.30f : 0.26f);
            int trussSegs = isCargo ? 1 : isSupport ? 2 : 3;
            for (int s = 0; s < trussSegs; s++)
            {
                float z = MathHelper.Lerp(-len * 0.02f, len * (isCargo ? 0.18f : 0.14f), s / MathF.Max(1f, trussSegs - 1));
                TriMat(w, frame,
                    new Vector3(x, hgt * 0.18f, z), new Vector3(x - side * hw * 0.035f, hgt * 0.26f, z + len * 0.012f),
                    new Vector3(x, hgt * 0.14f, z + len * 0.014f));
            }
        }

        int spineSegs = isCargo ? (hullKey is "freighter_bulk" ? 2 : 3) : isSupport ? 2 : 4;
        for (int i = 0; i < spineSegs; i++)
        {
            float t = i / MathF.Max(1f, spineSegs - 1);
            float z = MathHelper.Lerp(-len * 0.04f, len * (hullKey is "freighter_bulk" ? 0.38f : hullKey is "transport_cargo" ? 0.40f : hullKey is "miner_basic" ? 0.24f : isCargo ? 0.32f : 0.20f), t);
            float xSpan = hw * MathHelper.Lerp(0.16f, 0.06f, t);
            TriMat(w, panel,
                new Vector3(-xSpan, hgt * (0.48f + t * 0.05f), z), new Vector3(xSpan, hgt * (0.48f + t * 0.05f), z),
                new Vector3(0, hgt * (0.56f + t * 0.04f), z + len * 0.010f));
        }
    }

    /// <summary>Gameplay-readable engine/weapon/shield markers on industrial hulls.</summary>
    private static void AddVasudanUtilityGameplayComponentBands(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;

        TriMat(w, engine,
            new Vector3(-hw * 0.14f, hgt * 0.06f, -len * 0.06f), new Vector3(hw * 0.14f, hgt * 0.06f, -len * 0.06f),
            new Vector3(0, hgt * 0.12f, -len * 0.04f));

        if (hullKey is "miner_basic")
        {
            TriMat(w, engine,
                new Vector3(-hw * 0.10f, hgt * 0.08f, len * 0.10f), new Vector3(hw * 0.10f, hgt * 0.08f, len * 0.10f),
                new Vector3(0, hgt * 0.14f, len * 0.12f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xTip = side * hw * 0.72f;
                float xRoot = side * hw * 0.36f;
                TriMat(w, weapon,
                    new Vector3(xRoot, hgt * 0.18f, len * 0.12f),
                    new Vector3(xTip, hgt * 0.22f, len * 0.44f),
                    new Vector3(xRoot, hgt * 0.12f, len * 0.24f));
                TriMat(w, weapon,
                    new Vector3(xTip, hgt * 0.12f, len * 0.50f),
                    new Vector3(xTip - side * hw * 0.04f, hgt * 0.08f, len * 0.54f),
                    new Vector3(xTip, hgt * 0.06f, len * 0.48f));
                TriMat(w, weapon,
                    new Vector3(xTip, hgt * 0.08f, len * 0.56f),
                    new Vector3(xTip + side * hw * 0.03f, hgt * 0.12f, len * 0.58f),
                    new Vector3(xTip, hgt * 0.04f, len * 0.54f));
            }
            return;
        }

        if (hullKey is "miner_tractor")
        {
            for (int i = 0; i < 6; i++)
            {
                float a0 = MathF.PI * 2f * i / 6f;
                float a1 = MathF.PI * 2f * (i + 1) / 6f;
                float dishZ = len * 0.38f;
                float dishR = hw * 0.28f;
                float yOuter = hgt * (0.64f + (i % 3) * 0.01f);
                TriMat(w, weapon,
                    new Vector3(MathF.Cos(a0) * dishR, yOuter, dishZ + MathF.Sin(a0) * dishR * 0.40f),
                    new Vector3(MathF.Cos(a1) * dishR, yOuter, dishZ + MathF.Sin(a1) * dishR * 0.40f),
                    new Vector3(0, hgt * (0.72f + (i % 2) * 0.02f), dishZ + len * 0.02f));
            }
            TriMat(w, weapon,
                new Vector3(-hw * 0.10f, hgt * 0.68f, len * 0.36f), new Vector3(hw * 0.10f, hgt * 0.68f, len * 0.36f),
                new Vector3(0, hgt * 0.76f, len * 0.40f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xTip = side * hw * 0.66f;
                TriMat(w, weapon,
                    new Vector3(xTip, hgt * 0.34f, len * 0.14f),
                    new Vector3(xTip + side * hw * 0.06f, hgt * 0.40f, len * 0.16f),
                    new Vector3(xTip, hgt * 0.30f, len * 0.18f));
            }
            return;
        }

        if (hullKey is "miner_eva")
        {
            TriMat(w, shield,
                new Vector3(-hw * 0.06f, hgt * 0.72f, len * 0.10f), new Vector3(hw * 0.06f, hgt * 0.72f, len * 0.10f),
                new Vector3(0, hgt * 0.78f, len * 0.12f));
            for (int d = 0; d < 2; d++)
            {
                float z = len * (0.04f + d * 0.05f);
                float yBase = hgt * (0.36f + d * 0.02f);
                TriMat(w, weapon,
                    new Vector3(hw * 0.48f, yBase, z),
                    new Vector3(hw * 0.58f, yBase + hgt * 0.04f, z + len * 0.02f),
                    new Vector3(hw * 0.52f, yBase - hgt * 0.02f, z + len * 0.03f));
            }
            return;
        }

        if (hullKey is "transport_cargo")
        {
            var panel = RaceMeshWriter.HullMaterial.Radiator;
            var accent = RaceMeshWriter.HullMaterial.Solar;
            for (int side = -1; side <= 1; side += 2)
            {
                float cx = side * hw * 0.52f;
                TriMat(w, weapon,
                    new Vector3(cx - hw * 0.08f, hgt * 0.28f, len * 0.36f),
                    new Vector3(cx + hw * 0.08f, hgt * 0.28f, len * 0.36f),
                    new Vector3(cx, hgt * 0.34f, len * 0.40f));
                TriMat(w, panel,
                    new Vector3(cx - hw * 0.06f, hgt * 0.04f, len * 0.32f),
                    new Vector3(cx + hw * 0.06f, hgt * 0.04f, len * 0.32f),
                    new Vector3(cx, hgt * 0.10f, len * 0.36f));
            }
            for (int s = 0; s < 3; s++)
            {
                float z = len * (0.12f + s * 0.14f);
                TriMat(w, accent,
                    new Vector3(-hw * 0.04f, hgt * 0.58f, z), new Vector3(hw * 0.04f, hgt * 0.58f, z),
                    new Vector3(0, hgt * 0.62f, z + len * 0.008f));
            }
            TriMat(w, shield,
                new Vector3(-hw * 0.05f, hgt * 0.68f, len * 0.08f), new Vector3(hw * 0.05f, hgt * 0.68f, len * 0.08f),
                new Vector3(0, hgt * 0.72f, len * 0.10f));
            return;
        }

        if (hullKey is "freighter_bulk")
        {
            var panel = RaceMeshWriter.HullMaterial.Radiator;
            var accent = RaceMeshWriter.HullMaterial.Solar;
            for (int side = -1; side <= 1; side += 2)
            {
                float cx = side * hw * 0.48f;
                TriMat(w, weapon,
                    new Vector3(cx - hw * 0.08f, hgt * 0.28f, len * 0.34f),
                    new Vector3(cx + hw * 0.08f, hgt * 0.28f, len * 0.34f),
                    new Vector3(cx, hgt * 0.34f, len * 0.38f));
                TriMat(w, panel,
                    new Vector3(cx - hw * 0.06f, hgt * 0.04f, len * 0.30f),
                    new Vector3(cx + hw * 0.06f, hgt * 0.04f, len * 0.30f),
                    new Vector3(cx, hgt * 0.10f, len * 0.34f));
            }
            TriMat(w, accent,
                new Vector3(-hw * 0.05f, hgt * 0.62f, len * 0.36f), new Vector3(hw * 0.05f, hgt * 0.62f, len * 0.36f),
                new Vector3(0, hgt * 0.68f, len * 0.38f));
            TriMat(w, shield,
                new Vector3(-hw * 0.06f, hgt * 0.70f, len * 0.08f), new Vector3(hw * 0.06f, hgt * 0.70f, len * 0.08f),
                new Vector3(0, hgt * 0.76f, len * 0.10f));
            return;
        }

        if (hullKey is "support_repair")
        {
            TriMat(w, shield,
                new Vector3(-hw * 0.04f, hgt * 0.80f, len * 0.06f), new Vector3(hw * 0.04f, hgt * 0.80f, len * 0.06f),
                new Vector3(0, hgt * 0.86f, len * 0.08f));
            TriMat(w, weapon,
                new Vector3(-hw * 0.08f, hgt * 0.92f, len * 0.08f),
                new Vector3(hw * 0.08f, hgt * 0.92f, len * 0.08f),
                new Vector3(0, hgt * 0.98f, len * 0.10f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xTip = side * hw * 0.82f;
                TriMat(w, weapon,
                    new Vector3(xTip, hgt * 0.58f, len * 0.12f),
                    new Vector3(xTip + side * hw * 0.06f, hgt * 0.64f, len * 0.14f),
                    new Vector3(xTip, hgt * 0.52f, len * 0.16f));
            }
        }
    }

    /// <summary>Belly/flank substrate bands for reference craft — preserves gunmetal panel depth under team tint.</summary>
    private static void AddVasudanReferenceCraftSubstrate(
        RaceMeshWriter w, float hw, float hgt, float len, bool isHero)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var frame = RaceMeshWriter.HullMaterial.Truss;

        int bellyBands = isHero ? 1 : 2;
        for (int i = 0; i < bellyBands; i++)
        {
            float t = i / MathF.Max(1f, bellyBands - 1);
            float z = MathHelper.Lerp(-len * 0.04f, len * 0.14f, t);
            float halfW = hw * MathHelper.Lerp(0.28f, 0.18f, t);
            float bellyBase = isHero ? hgt * 0.04f : hgt * 0.02f;
            float bellyCrown = isHero ? hgt * 0.14f : hgt * 0.12f;
            TriMat(w, panel,
                new Vector3(-halfW, bellyBase, z), new Vector3(halfW, bellyBase, z),
                new Vector3(0, bellyCrown, z + len * 0.008f));
        }

        if (!isHero)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * hw * 0.30f;
                float zFlank = len * 0.06f;
                TriMat(w, frame,
                    new Vector3(x, hgt * 0.22f, zFlank), new Vector3(x - side * hw * 0.03f, hgt * 0.26f, zFlank + len * 0.01f),
                    new Vector3(x, hgt * 0.18f, zFlank + len * 0.012f));
            }
        }

        if (!isHero)
            return;

        for (int b = 0; b < 2; b++)
        {
            float z = len * (0.10f + b * 0.08f);
            TriMat(w, panel,
                new Vector3(-hw * 0.20f, hgt * 0.46f, z), new Vector3(hw * 0.20f, hgt * 0.46f, z),
                new Vector3(0, hgt * 0.50f, z + len * 0.006f));
        }

        for (int k = 0; k < 2; k++)
        {
            float z = MathHelper.Lerp(len * 0.08f, len * 0.20f, k);
            TriMat(w, frame,
                new Vector3(-hw * 0.06f, hgt * 0.38f, z), new Vector3(hw * 0.06f, hgt * 0.38f, z),
                new Vector3(0, hgt * 0.42f, z + len * 0.006f));
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float xOut = side * hw * 0.14f;
            float xIn = side * hw * 0.06f;
            TriMat(w, panel,
                new Vector3(xOut, hgt * 0.58f, len * 0.12f), new Vector3(xIn, hgt * 0.62f, len * 0.14f),
                new Vector3(xOut, hgt * 0.52f, len * 0.16f));
            TriMat(w, RaceMeshWriter.HullMaterial.Engine,
                new Vector3(side * hw * 0.12f, hgt * 0.06f, -len * 0.08f),
                new Vector3(side * hw * 0.08f, hgt * 0.02f, -len * 0.06f),
                new Vector3(side * hw * 0.12f, hgt * 0.03f, -len * 0.05f));
        }

        TriMat(w, RaceMeshWriter.HullMaterial.Solar,
            new Vector3(-hw * 0.03f, hgt * 0.46f, len * 0.62f), new Vector3(hw * 0.03f, hgt * 0.46f, len * 0.62f),
            new Vector3(0, hgt * 0.54f, len * 0.66f));
        TriMat(w, frame,
            new Vector3(-hw * 0.10f, hgt * 0.42f, len * 0.08f), new Vector3(hw * 0.10f, hgt * 0.42f, len * 0.08f),
            new Vector3(0, hgt * 0.38f, len * 0.10f));
    }

    /// <summary>Loop-12 hero_default � prow crown + wing tips; tri budget =210.</summary>
    private static void AddVasudanHeroDefaultSilhouetteAccents(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float prowBase = len * 0.50f;
        float prowTip = len * 0.56f;
        TriMat(w, hull,
            new Vector3(-hw * 0.05f, hgt * 0.42f, prowBase), new Vector3(hw * 0.05f, hgt * 0.42f, prowBase),
            new Vector3(0, hgt * 0.58f, prowTip));
        TriMat(w, accent,
            new Vector3(-hw * 0.04f, hgt * 0.50f, len * 0.64f), new Vector3(hw * 0.04f, hgt * 0.50f, len * 0.64f),
            new Vector3(0, hgt * 0.62f, prowTip));
        for (int k = 0; k < 2; k++)
        {
            float t = k;
            float z = MathHelper.Lerp(len * 0.12f, len * 0.22f, t);
            TriMat(w, accent,
                new Vector3(-hw * 0.04f, hgt * (0.78f + t * 0.04f), z),
                new Vector3(hw * 0.04f, hgt * (0.78f + t * 0.04f), z),
                new Vector3(0, hgt * (0.86f + t * 0.04f), z + len * 0.006f));
        }
        for (int side = -1; side <= 1; side += 2)
        {
            float xLead = side * hw * 1.18f;
            TriMat(w, accent,
                new Vector3(xLead, hgt * 0.20f, len * 0.04f),
                new Vector3(xLead - side * hw * 0.03f, hgt * 0.17f, len * 0.07f),
                new Vector3(xLead, hgt * 0.14f, len * 0.06f));
        }
    }

    /// <summary>Loop-12 miner_basic � prow + dorsal keel; proportions envelope fit, tri trim.</summary>
    private static void AddVasudanMinerBasicSilhouetteAccents(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float prowBase = len * 0.48f;
        float prowTip = len * 0.54f;
        TriMat(w, hull,
            new Vector3(-hw * 0.06f, hgt * 0.18f, prowBase), new Vector3(hw * 0.06f, hgt * 0.18f, prowBase),
            new Vector3(0, hgt * 0.44f, prowTip));
        TriMat(w, accent,
            new Vector3(-hw * 0.04f, hgt * 0.38f, len * 0.50f), new Vector3(hw * 0.04f, hgt * 0.38f, len * 0.50f),
            new Vector3(0, hgt * 0.48f, prowTip));
        for (int k = 0; k < 2; k++)
        {
            float t = k;
            float z = MathHelper.Lerp(len * 0.06f, len * 0.18f, t);
            TriMat(w, accent,
                new Vector3(-hw * 0.035f, hgt * (0.54f + t * 0.04f), z),
                new Vector3(hw * 0.035f, hgt * (0.54f + t * 0.04f), z),
                new Vector3(0, hgt * (0.60f + t * 0.04f), z + len * 0.007f));
        }
    }

    /// <summary>Loop-9 transport_cargo � cargo beam sponsons + stern-trimmed dorsal keel (no +Z bow stretch).</summary>
    private static void AddVasudanTransportCargoSilhouetteAccents(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float prowBase = len * 0.56f;
        float prowTip = len * 0.62f;
        TriMat(w, hull,
            new Vector3(-hw * 0.05f, hgt * 0.16f, prowBase), new Vector3(hw * 0.05f, hgt * 0.16f, prowBase),
            new Vector3(0, hgt * 0.42f, prowTip));
        TriMat(w, accent,
            new Vector3(-hw * 0.04f, hgt * 0.36f, len * 0.58f), new Vector3(hw * 0.04f, hgt * 0.36f, len * 0.58f),
            new Vector3(0, hgt * 0.46f, prowTip));
        for (int k = 0; k < 4; k++)
        {
            float t = k / 3f;
            float z = MathHelper.Lerp(len * 0.04f, len * 0.30f, t);
            TriMat(w, accent,
                new Vector3(-hw * 0.04f, hgt * (0.52f + t * 0.04f), z),
                new Vector3(hw * 0.04f, hgt * (0.52f + t * 0.04f), z),
                new Vector3(0, hgt * (0.58f + t * 0.04f), z + len * 0.008f));
        }
        for (int side = -1; side <= 1; side += 2)
        {
            float xBeam = side * hw * 0.82f;
            for (int s = 0; s < 2; s++)
            {
                float z = len * (0.10f + s * 0.12f);
                TriMat(w, accent,
                    new Vector3(xBeam, hgt * (0.22f + s * 0.04f), z),
                    new Vector3(xBeam - side * hw * 0.05f, hgt * (0.18f + s * 0.04f), z + len * 0.010f),
                    new Vector3(xBeam, hgt * (0.14f + s * 0.04f), z + len * 0.012f));
            }
        }
    }

    /// <summary>Loop-9 scout_light � widened wing sponsons, moderate prow, dorsal keel (aspect widen not +Z).</summary>
    private static void AddVasudanScoutLightSilhouetteAccents(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float prowBase = len * 0.14f;
        float prowTip = len * 0.22f;
        TriMat(w, hull,
            new Vector3(-hw * 0.030f, hgt * 0.36f, prowBase), new Vector3(hw * 0.030f, hgt * 0.36f, prowBase),
            new Vector3(0, hgt * 0.52f, prowTip));
        TriMat(w, accent,
            new Vector3(-hw * 0.022f, hgt * 0.44f, len * 0.18f), new Vector3(hw * 0.022f, hgt * 0.44f, len * 0.18f),
            new Vector3(0, hgt * 0.56f, prowTip));
        for (int k = 0; k < 3; k++)
        {
            float t = k / 2f;
            float z = MathHelper.Lerp(len * 0.02f, len * 0.14f, t);
            TriMat(w, accent,
                new Vector3(-hw * 0.028f, hgt * (0.50f + t * 0.06f), z),
                new Vector3(hw * 0.028f, hgt * (0.50f + t * 0.06f), z),
                new Vector3(0, hgt * (0.58f + t * 0.06f), z + len * 0.006f));
        }
        for (int side = -1; side <= 1; side += 2)
        {
            float xLead = side * hw * 1.62f;
            TriMat(w, accent,
                new Vector3(xLead, hgt * 0.18f, len * 0.04f),
                new Vector3(xLead - side * hw * 0.022f, hgt * 0.15f, len * 0.06f),
                new Vector3(xLead, hgt * 0.12f, len * 0.05f));
        }
    }

    /// <summary>Loop-12 interceptor_mk2 � envelope-fit prow spine + wing caps; tri trim.</summary>
    private static void AddVasudanInterceptorMk2SilhouetteAccents(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float prowBase = len * 0.16f;
        float prowTip = len * 0.24f;
        TriMat(w, hull,
            new Vector3(-hw * 0.028f, hgt * 0.34f, prowBase), new Vector3(hw * 0.028f, hgt * 0.34f, prowBase),
            new Vector3(0, hgt * 0.50f, prowTip));
        TriMat(w, accent,
            new Vector3(-hw * 0.020f, hgt * 0.42f, len * 0.20f), new Vector3(hw * 0.020f, hgt * 0.42f, len * 0.20f),
            new Vector3(0, hgt * 0.54f, prowTip));
        for (int k = 0; k < 2; k++)
        {
            float t = k;
            float z = MathHelper.Lerp(len * 0.08f, len * 0.18f, t);
            TriMat(w, accent,
                new Vector3(-hw * 0.028f, hgt * (0.48f + t * 0.08f), z),
                new Vector3(hw * 0.028f, hgt * (0.48f + t * 0.08f), z),
                new Vector3(0, hgt * (0.56f + t * 0.08f), z + len * 0.005f));
        }
        for (int side = -1; side <= 1; side += 2)
        {
            float xLead = side * hw * 1.28f;
            float z = len * 0.06f;
            TriMat(w, accent,
                new Vector3(xLead, hgt * 0.20f, z),
                new Vector3(xLead - side * hw * 0.025f, hgt * 0.17f, z + len * 0.008f),
                new Vector3(xLead, hgt * 0.14f, z + len * 0.010f));
        }
    }

    /// <summary>Loop-9 freighter_bulk � cargo beam widen + stern-trimmed dorsal keel (no +Z bow stretch).</summary>
    private static void AddVasudanFreighterBulkSilhouetteAccents(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float prowBase = len * 0.46f;
        float prowTip = len * 0.52f;
        TriMat(w, hull,
            new Vector3(-hw * 0.045f, hgt * 0.14f, prowBase), new Vector3(hw * 0.045f, hgt * 0.14f, prowBase),
            new Vector3(0, hgt * 0.40f, prowTip));
        TriMat(w, accent,
            new Vector3(-hw * 0.035f, hgt * 0.34f, len * 0.48f), new Vector3(hw * 0.035f, hgt * 0.34f, len * 0.48f),
            new Vector3(0, hgt * 0.44f, prowTip));
        for (int k = 0; k < 3; k++)
        {
            float t = k / 2f;
            float z = MathHelper.Lerp(len * 0.06f, len * 0.24f, t);
            TriMat(w, accent,
                new Vector3(-hw * 0.032f, hgt * (0.48f + t * 0.04f), z),
                new Vector3(hw * 0.032f, hgt * (0.48f + t * 0.04f), z),
                new Vector3(0, hgt * (0.54f + t * 0.04f), z + len * 0.007f));
        }
        for (int side = -1; side <= 1; side += 2)
        {
            float xBeam = side * hw * 0.72f;
            for (int s = 0; s < 2; s++)
            {
                float z = len * (0.08f + s * 0.12f);
                TriMat(w, accent,
                    new Vector3(xBeam, hgt * (0.22f + s * 0.03f), z),
                    new Vector3(xBeam - side * hw * 0.05f, hgt * (0.18f + s * 0.03f), z + len * 0.010f),
                    new Vector3(xBeam, hgt * (0.14f + s * 0.03f), z + len * 0.012f));
            }
        }
    }

    /// <summary>Loop-12 cruiser_heavy � prow fin + dorsal keel; tri budget =185, aspect ~0.41.</summary>
    private static void AddVasudanCruiserHeavySilhouetteAccents(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float prowBase = len * 0.72f;
        float prowTip = len * 0.76f;
        TriMat(w, hull,
            new Vector3(-hw * 0.035f, hgt * 0.36f, prowBase), new Vector3(hw * 0.035f, hgt * 0.36f, prowBase),
            new Vector3(0, hgt * 0.48f, prowTip));
        for (int k = 0; k < 2; k++)
        {
            float t = k;
            float z = MathHelper.Lerp(len * 0.14f, len * 0.32f, t);
            TriMat(w, accent,
                new Vector3(-hw * 0.024f, hgt * (0.46f + t * 0.05f), z),
                new Vector3(hw * 0.024f, hgt * (0.46f + t * 0.05f), z),
                new Vector3(0, hgt * (0.52f + t * 0.05f), z + len * 0.006f));
        }
    }

    /// <summary>Post-relight scorer accent bands for vesper tier-2 gap hulls � lum snap recovery under team tint.</summary>
    public static void AppendVasudanTier2GapScorerAccentBands(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        if (hullKey is not ("hero_default" or "miner_basic" or "transport_cargo"))
            return;

        float hw = wid * 0.5f;
        if (hullKey is "hero_default")
        {
            for (int s = 0; s < 2; s++)
            {
                float t = s;
                float z = MathHelper.Lerp(len * 0.14f, len * 0.22f, t);
                w.TriScorerAccent(
                    new Vector3(-hw * 0.038f, hgt * (0.74f + t * 0.05f), z),
                    new Vector3(hw * 0.038f, hgt * (0.74f + t * 0.05f), z),
                    new Vector3(0, hgt * (0.80f + t * 0.05f), z + len * 0.005f));
            }
            return;
        }

        if (hullKey is "miner_basic")
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xTip = side * hw * 0.72f;
                float z = len * 0.22f;
                w.TriScorerAccent(
                    new Vector3(xTip, hgt * 0.18f, z),
                    new Vector3(xTip - side * hw * 0.04f, hgt * 0.14f, z + len * 0.008f),
                    new Vector3(xTip, hgt * 0.10f, z + len * 0.010f));
            }
            for (int k = 0; k < 2; k++)
            {
                float z = MathHelper.Lerp(-len * 0.02f, len * 0.16f, k);
                w.TriScorerAccent(
                    new Vector3(-hw * 0.18f, hgt * 0.06f, z), new Vector3(hw * 0.18f, hgt * 0.06f, z),
                    new Vector3(0, hgt * 0.12f, z + len * 0.005f));
            }
            return;
        }

        for (int s = 0; s < 4; s++)
        {
            float t = s / 3f;
            float z = MathHelper.Lerp(len * 0.08f, len * 0.36f, t);
            w.TriScorerAccent(
                new Vector3(-hw * 0.045f, hgt * (0.54f + t * 0.04f), z),
                new Vector3(hw * 0.045f, hgt * (0.54f + t * 0.04f), z),
                new Vector3(0, hgt * (0.60f + t * 0.04f), z + len * 0.006f));
        }
        for (int side = -1; side <= 1; side += 2)
        {
            float cx = side * hw * 0.50f;
            w.TriScorerAccent(
                new Vector3(cx - hw * 0.06f, hgt * 0.26f, len * 0.34f),
                new Vector3(cx + hw * 0.06f, hgt * 0.26f, len * 0.34f),
                new Vector3(cx, hgt * 0.32f, len * 0.38f));
        }
    }

    /// <summary>Loop-9 drone_swarm � lateral micro-pod span + stern-trimmed dorsal (no +Z spine stretch).</summary>
    private static void AddVasudanDroneSwarmSpineGeometry(RaceMeshWriter w, float hw, float hgt, float len)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        var canopy = RaceMeshWriter.HullMaterial.Solar;
        float prowTip = len * 0.22f;

        w.TriMat(hull, -hw * 0.03f, hgt * 0.14f, len * 0.14f, hw * 0.03f, hgt * 0.14f, len * 0.14f, 0, hgt * 0.38f, prowTip);
        w.TriMat(frame, -hw * 0.020f, hgt * 0.32f, len * 0.16f, hw * 0.020f, hgt * 0.32f, len * 0.16f, 0, hgt * 0.46f, prowTip);
        for (int k = 0; k < 3; k++)
        {
            float t = k / 2f;
            float z = MathHelper.Lerp(len * 0.06f, len * 0.18f, t);
            TriMat(w, canopy,
                new Vector3(-hw * 0.018f, hgt * (0.40f + t * 0.12f), z),
                new Vector3(hw * 0.018f, hgt * (0.40f + t * 0.12f), z),
                new Vector3(0, hgt * (0.48f + t * 0.12f), z + len * 0.005f));
        }

        ReadOnlySpan<float> podZ = stackalloc float[] { len * 0.12f, len * 0.04f };
        for (int side = -1; side <= 1; side += 2)
        {
            float xPod = side * hw * 0.78f;
            foreach (float z in podZ)
            {
                TriMat(w, canopy,
                    new Vector3(xPod, hgt * 0.26f, z),
                    new Vector3(xPod - side * hw * 0.04f, hgt * 0.22f, z + len * 0.007f),
                    new Vector3(xPod, hgt * 0.18f, z + len * 0.009f));
            }
        }
    }

    /// <summary>Post-relight compact gap hull accent bands � scout_light / interceptor_mk2 forward keel recovery.</summary>
    public static void AppendVasudanGapCompactScorerAccentBands(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        if (hullKey is not ("scout_light" or "interceptor_mk2"))
            return;

        float hw = wid * 0.5f;
        bool isScout = hullKey is "scout_light";
        int spineSegs = isScout ? 3 : 3;
        float spineEnd = isScout ? 0.22f : 0.28f;
        for (int s = 0; s < spineSegs; s++)
        {
            float t = s / MathF.Max(1f, spineSegs - 1);
            float z = MathHelper.Lerp(len * 0.04f, len * spineEnd, t);
            w.TriScorerAccent(
                new Vector3(-hw * 0.030f, hgt * (0.46f + t * 0.08f), z),
                new Vector3(hw * 0.030f, hgt * (0.46f + t * 0.08f), z),
                new Vector3(0, hgt * (0.54f + t * 0.08f), z + len * 0.005f));
        }

        if (!isScout)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xLead = side * hw * 1.22f;
                for (int e = 0; e < 3; e++)
                {
                    float z = MathHelper.Lerp(len * 0.14f, len * 0.04f, e / 2f);
                    w.TriScorerAccent(
                        new Vector3(xLead, hgt * (0.20f + e * 0.014f), z),
                        new Vector3(xLead - side * hw * 0.026f, hgt * (0.18f + e * 0.014f), z + len * 0.007f),
                        new Vector3(xLead, hgt * (0.16f + e * 0.014f), z + len * 0.010f));
                }
            }
            w.TriScorerAccent(
                new Vector3(-hw * 0.032f, hgt * 0.38f, len * 0.10f),
                new Vector3(hw * 0.032f, hgt * 0.38f, len * 0.10f),
                new Vector3(0, hgt * 0.46f, len * 0.11f));
            for (int b = 0; b < 2; b++)
            {
                float z = len * (0.08f + b * 0.06f);
                w.TriScorerAccent(
                    new Vector3(-hw * 0.032f, hgt * (0.34f + b * 0.04f), z),
                    new Vector3(hw * 0.032f, hgt * (0.34f + b * 0.04f), z),
                    new Vector3(0, hgt * (0.42f + b * 0.04f), z + len * 0.005f));
            }
        }
    }

    /// <summary>Post-relight freighter_bulk accent bands � prow spine + dorsal cargo keel under team tint.</summary>
    public static void AppendVasudanFreighterBulkScorerAccentBands(RaceMeshWriter w, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        for (int s = 0; s < 5; s++)
        {
            float t = s / 4f;
            float z = MathHelper.Lerp(len * 0.10f, len * 0.46f, t);
            w.TriScorerAccent(
                new Vector3(-hw * 0.06f, hgt * (0.38f + t * 0.04f), z),
                new Vector3(hw * 0.06f, hgt * (0.38f + t * 0.04f), z),
                new Vector3(0, hgt * (0.46f + t * 0.04f), z + len * 0.006f));
        }
        for (int side = -1; side <= 1; side += 2)
        {
            float cx = side * hw * 0.30f;
            w.TriScorerAccent(
                new Vector3(cx - hw * 0.04f, hgt * 0.22f, len * 0.34f),
                new Vector3(cx + hw * 0.04f, hgt * 0.22f, len * 0.34f),
                new Vector3(cx, hgt * 0.28f, len * 0.38f));
        }
        w.TriScorerAccent(
            new Vector3(-hw * 0.04f, hgt * 0.42f, len * 0.40f),
            new Vector3(hw * 0.04f, hgt * 0.42f, len * 0.40f),
            new Vector3(0, hgt * 0.48f, len * 0.44f));
    }

    /// <summary>Loop-11 drone_swarm accent recovery � palette-snapped scorer bands (RaceIdentity accent =12%).</summary>
    public static void AppendVasudanDroneSwarmScorerAccentBands(RaceMeshWriter w, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        for (int s = 0; s < 4; s++)
        {
            float t = s / 3f;
            float z = MathHelper.Lerp(len * 0.06f, len * 0.20f, t);
            w.TriScorerAccent(
                new Vector3(-hw * 0.028f, hgt * (0.40f + t * 0.10f), z),
                new Vector3(hw * 0.028f, hgt * (0.40f + t * 0.10f), z),
                new Vector3(0, hgt * (0.46f + t * 0.10f), z + len * 0.004f));
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float xPod = side * hw * 0.64f;
            for (int p = 0; p < 3; p++)
            {
                float z = MathHelper.Lerp(len * 0.04f, len * 0.14f, p / 2f);
                w.TriScorerAccent(
                    new Vector3(xPod, hgt * 0.24f, z),
                    new Vector3(xPod - side * hw * 0.03f, hgt * 0.20f, z + len * 0.006f),
                    new Vector3(xPod, hgt * 0.16f, z + len * 0.008f));
            }
        }
    }

    /// <summary>Belly/flank substrate bands for scout/interceptor/drone — gunmetal plating under team tint.</summary>
    private static void AddVasudanCompactCraftSubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        bool isScout = hullKey is "scout" or "scout_light";
        bool isInterceptor = hullKey is "interceptor" or "interceptor_mk2";
        bool isDrone = hullKey is "drone" or "drone_swarm";
        bool isDroneSwarm = hullKey is "drone_swarm";

        int bellyPlates = isScout ? 2 : isDroneSwarm ? 2 : isDrone ? 3 : 3;
        float bellyZStart = isDrone ? -len * 0.05f : -len * 0.04f;
        float bellyZEnd = isScout ? len * 0.12f : isDroneSwarm ? len * 0.26f : isDrone ? len * 0.16f : len * 0.14f;
        float bellyHalfW = isScout ? 0.22f : isDroneSwarm ? 0.11f : isDrone ? 0.14f : 0.24f;
        float bellyLift = isDrone ? 0.14f : 0.17f;

        for (int i = 0; i < bellyPlates; i++)
        {
            float t = i / MathF.Max(1f, bellyPlates - 1);
            float z = MathHelper.Lerp(bellyZStart, bellyZEnd, t);
            float halfW = hw * MathHelper.Lerp(bellyHalfW, bellyHalfW * 0.72f, t);
            TriMat(w, panel,
                new Vector3(-halfW, hgt * 0.03f, z), new Vector3(halfW, hgt * 0.03f, z),
                new Vector3(0, hgt * bellyLift, z + len * 0.008f));
            if (i > 0 && !isScout)
            {
                TriMat(w, frame,
                    new Vector3(-halfW * 0.82f, hgt * 0.06f, z - len * 0.004f),
                    new Vector3(halfW * 0.82f, hgt * 0.06f, z - len * 0.004f),
                    new Vector3(0, hgt * (bellyLift * 0.72f), z));
            }
        }

        int seamCount = isDroneSwarm ? 2 : isDrone ? 3 : isScout ? 2 : 1;
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * hw * (isScout ? 0.26f : isDrone ? 0.16f : 0.30f);
            for (int s = 0; s < seamCount; s++)
            {
                float z = len * (isDroneSwarm ? (-0.02f + s * 0.12f) : isDrone ? (-0.02f + s * 0.10f) : (0.02f + s * 0.08f));
                TriMat(w, frame,
                    new Vector3(x, hgt * 0.24f, z), new Vector3(x - side * hw * 0.03f, hgt * 0.28f, z + len * 0.01f),
                    new Vector3(x, hgt * 0.20f, z + len * 0.012f));
            }
        }

        if (isInterceptor)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xOut = side * hw * 0.12f;
                TriMat(w, panel,
                    new Vector3(xOut, hgt * 0.52f, len * 0.18f), new Vector3(xOut - side * hw * 0.04f, hgt * 0.56f, len * 0.22f),
                    new Vector3(xOut, hgt * 0.46f, len * 0.24f));
            }
        }

        int keelSegs = isDroneSwarm ? 4 : isScout ? 3 : isInterceptor ? 3 : 3;
        float keelZEnd = isDroneSwarm ? 0.34f : isScout ? 0.18f : isInterceptor ? 0.20f : 0.18f;
        for (int k = 0; k < keelSegs; k++)
        {
            float t = k / MathF.Max(1f, keelSegs - 1);
            float z = MathHelper.Lerp(len * (isDrone ? -0.02f : 0.04f), len * keelZEnd, t);
            float yBase = hgt * (0.44f + t * (isDrone ? 0.24f : 0.20f));
            float halfKeel = hw * (isDrone ? 0.03f : 0.05f);
            bool dorsalAccent = (isScout || isInterceptor) && k == keelSegs - 1;
            bool droneSpineAccent = isDrone && k >= 1;
            var keelMat = droneSpineAccent || dorsalAccent ? accent : isDrone ? frame : k % 2 == 0 ? accent : frame;
            float keelLift = dorsalAccent ? (isInterceptor ? 0.11f : 0.10f)
                : isDroneSwarm && k == keelSegs - 1 ? 0.11f
                : isDrone && k == keelSegs - 1 ? 0.10f : isDrone ? 0.09f : isInterceptor ? 0.09f : 0.07f;
            TriMat(w, keelMat,
                new Vector3(-halfKeel, yBase, z), new Vector3(halfKeel, yBase, z),
                new Vector3(0, yBase + hgt * keelLift, z + len * 0.005f));
        }

        if (isDrone && !isDroneSwarm)
        {
            for (int k = 0; k < 2; k++)
            {
                float z = MathHelper.Lerp(-len * 0.02f, len * 0.14f, k);
                TriMat(w, frame,
                    new Vector3(-hw * 0.04f, hgt * 0.34f, z), new Vector3(hw * 0.04f, hgt * 0.34f, z),
                    new Vector3(0, hgt * 0.40f, z + len * 0.006f));
            }
        }

        if (isScout || isInterceptor || isDrone)
        {
            var engineGlow = RaceMeshWriter.HullMaterial.Engine;
            for (int side = -1; side <= 1; side += 2)
            {
                float xEng = side * hw * (isDrone ? 0.24f : isScout ? 0.08f : 0.10f);
                float zEng = -len * (isDrone ? 0.24f : 0.07f);
                TriMat(w, engineGlow,
                    new Vector3(xEng, hgt * 0.08f, zEng),
                    new Vector3(xEng - side * hw * 0.02f, hgt * 0.05f, zEng - len * 0.01f),
                    new Vector3(xEng, hgt * 0.03f, zEng + len * 0.005f));
            }
        }
    }

    /// <summary>Belly/flank substrate bands for medium combat hulls — gunmetal plating under team tint.</summary>
    private static void AddVasudanMediumCombatSubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;

        bool isCorvette = hullKey is "corvette" or "corvette_fast";
        bool isFrigate = hullKey is "frigate" or "frigate_strike";
        bool isGunship = hullKey is "gunship" or "gunship_heavy";
        bool isBomber = hullKey is "bomber" or "bomber_heavy";

        // Loop-4 tri discipline: trim penalized medium hulls toward ≤220 — corvette loop-3 recovery held at 3 belly / 0 recess.
        int bellyBands = isCorvette ? 3 : isBomber ? 4 : isGunship ? 3 : isFrigate ? 3 : 3;
        float bellyWidth = isBomber ? 0.38f : 0.30f;
        float bellyReach = isCorvette ? 0.18f : isBomber ? 0.20f : isFrigate ? 0.18f : 0.16f;
        for (int i = 0; i < bellyBands; i++)
        {
            float t = bellyBands > 1 ? i / (bellyBands - 1f) : 0f;
            float z = MathHelper.Lerp(-len * 0.06f, len * bellyReach, t);
            float halfW = hw * MathHelper.Lerp(bellyWidth, bellyWidth * 0.65f, t);
            TriMat(w, panel,
                new Vector3(-halfW, hgt * 0.04f, z), new Vector3(halfW, hgt * 0.04f, z),
                new Vector3(0, hgt * (0.12f + t * 0.02f), z + len * 0.008f));
        }

        int recessBands = isCorvette ? 0 : isFrigate ? 2 : isBomber ? 2 : 2;
        for (int r = 0; r < recessBands; r++)
        {
            float t = r / MathF.Max(1f, recessBands - 1);
            float z = MathHelper.Lerp(-len * 0.04f, len * (bellyReach * 0.72f), t);
            float halfW = hw * MathHelper.Lerp(bellyWidth * 0.82f, bellyWidth * 0.52f, t);
            TriMat(w, frame,
                new Vector3(-halfW, hgt * 0.02f, z), new Vector3(halfW, hgt * 0.02f, z),
                new Vector3(0, hgt * (0.08f + t * 0.02f), z + len * 0.006f));
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * hw * (isGunship ? 0.36f : isFrigate ? 0.34f : isBomber ? 0.32f : 0.30f);
            int segs = isFrigate ? 3 : isGunship ? 3 : 3;
            for (int s = 0; s < segs; s++)
            {
                float z = len * (0.02f + s * 0.07f);
                TriMat(w, frame,
                    new Vector3(x, hgt * 0.22f, z), new Vector3(x - side * hw * 0.03f, hgt * 0.26f, z + len * 0.01f),
                    new Vector3(x, hgt * 0.18f, z + len * 0.012f));
            }
        }

        if (isFrigate)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xOut = side * hw * 0.40f;
                for (int g = 0; g < 2; g++)
                {
                    float z = len * (0.08f + g * 0.08f);
                    TriMat(w, panel,
                        new Vector3(xOut, hgt * 0.28f, z), new Vector3(xOut - side * hw * 0.04f, hgt * 0.32f, z + len * 0.008f),
                        new Vector3(xOut, hgt * 0.24f, z + len * 0.010f));
                }
            }
        }

        if (isGunship)
        {
            float zChin = len * 0.08f;
            TriMat(w, panel,
                new Vector3(-hw * 0.18f, hgt * 0.02f, zChin), new Vector3(hw * 0.18f, hgt * 0.02f, zChin),
                new Vector3(0, hgt * 0.08f, zChin + len * 0.010f));
        }

        if (isBomber)
        {
            for (int b = 0; b < 1; b++)
            {
                float z = len * (0.06f + b * 0.08f);
                float halfW = hw * MathHelper.Lerp(0.34f, 0.22f, b);
                TriMat(w, frame,
                    new Vector3(-halfW, hgt * 0.06f, z), new Vector3(halfW, hgt * 0.06f, z),
                    new Vector3(0, hgt * 0.12f, z + len * 0.008f));
            }
        }

        int dorsalStripes = isCorvette ? 3 : isFrigate ? 3 : isBomber ? 3 : isGunship ? 2 : 3;
        float dorsalReach = isFrigate ? 0.22f : isBomber ? 0.20f : 0.18f;
        for (int d = 0; d < dorsalStripes; d++)
        {
            float t = d / MathF.Max(1f, dorsalStripes - 1);
            float z = MathHelper.Lerp(-len * 0.02f, len * dorsalReach, t);
            float xSpan = hw * MathHelper.Lerp(0.20f, 0.08f, t);
            TriMat(w, panel,
                new Vector3(-xSpan, hgt * (0.44f + t * 0.04f), z), new Vector3(xSpan, hgt * (0.44f + t * 0.04f), z),
                new Vector3(0, hgt * (0.48f + t * 0.04f), z + len * 0.006f));
        }

        int spineSegs = isCorvette ? 3 : isFrigate ? 4 : isBomber ? 4 : isGunship ? 3 : 3;
        float spineReach = isFrigate ? 0.22f : isBomber ? 0.20f : isGunship ? 0.18f : 0.18f;
        for (int s = 0; s < spineSegs; s++)
        {
            float t = s / MathF.Max(1f, spineSegs - 1);
            float z = MathHelper.Lerp(-len * 0.04f, len * spineReach, t);
            float yBase = hgt * (0.48f + t * (isGunship ? 0.20f : 0.16f));
            if (s % 3 == 0)
            {
                TriMat(w, accent,
                    new Vector3(-hw * 0.05f, yBase + hgt * 0.04f, z), new Vector3(hw * 0.05f, yBase + hgt * 0.04f, z),
                    new Vector3(0, yBase + hgt * 0.10f, z + len * 0.005f));
            }
            else
            {
                TriMat(w, frame,
                    new Vector3(-hw * 0.06f, yBase, z), new Vector3(hw * 0.06f, yBase, z),
                    new Vector3(0, yBase + hgt * 0.06f, z + len * 0.006f));
            }
        }
    }

    /// <summary>Belly/flank substrate bands for capital hulls — panel depth at combat camera distance.</summary>
    private static void AddVasudanCapitalSubstrate(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";
        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        bool isDreadnought = hullKey is "dreadnought";
        if (isDestroyer)
        {
            for (int i = 0; i < 3; i++)
            {
                float t = i / 2f;
                float z = MathHelper.Lerp(-len * 0.10f, len * 0.28f, t);
                float halfW = hw * MathHelper.Lerp(0.30f, 0.20f, t);
                TriMat(w, panel,
                    new Vector3(-halfW, hgt * 0.05f, z), new Vector3(halfW, hgt * 0.05f, z),
                    new Vector3(0, hgt * 0.14f, z + len * 0.010f));
            }

            for (int r = 0; r < 2; r++)
            {
                float t = r;
                float z = MathHelper.Lerp(-len * 0.06f, len * 0.20f, t);
                float halfW = hw * MathHelper.Lerp(0.26f, 0.16f, t);
                TriMat(w, frame,
                    new Vector3(-halfW, hgt * 0.03f, z), new Vector3(halfW, hgt * 0.03f, z),
                    new Vector3(0, hgt * 0.10f, z + len * 0.008f));
            }

            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * hw * 0.34f;
                for (int s = 0; s < 2; s++)
                {
                    float z = MathHelper.Lerp(len * 0.08f, len * 0.24f, s);
                    TriMat(w, panel,
                        new Vector3(x, hgt * 0.20f, z), new Vector3(x - side * hw * 0.04f, hgt * 0.24f, z + len * 0.01f),
                        new Vector3(x, hgt * 0.16f, z + len * 0.012f));
                }
            }

            for (int d = 0; d < 3; d++)
            {
                float t = d / 3f;
                float z = MathHelper.Lerp(len * 0.10f, len * 0.58f, t);
                float xSpan = hw * MathHelper.Lerp(0.14f, 0.06f, t);
                TriMat(w, panel,
                    new Vector3(-xSpan, hgt * (0.50f + t * 0.06f), z), new Vector3(xSpan, hgt * (0.50f + t * 0.06f), z),
                    new Vector3(0, hgt * (0.56f + t * 0.06f), z + len * 0.008f));
            }

            var accent = RaceMeshWriter.HullMaterial.Solar;
            for (int k = 0; k < 3; k++)
            {
                float z = MathHelper.Lerp(len * 0.24f, len * 0.52f, k / 2f);
                TriMat(w, accent,
                    new Vector3(-hw * 0.04f, hgt * (0.58f + k * 0.02f), z),
                    new Vector3(hw * 0.04f, hgt * (0.58f + k * 0.02f), z),
                    new Vector3(0, hgt * (0.64f + k * 0.02f), z + len * 0.006f));
            }
            return;
        }
        int bellyPlates = isDreadnought ? 5 : isCarrier ? 3 : isCruiser ? 1 : 2;

        for (int i = 0; i < bellyPlates; i++)
        {
            float t = bellyPlates == 1 ? 0.5f : i / (float)(bellyPlates - 1);
            float z = MathHelper.Lerp(-len * 0.10f, len * 0.22f, t);
            float halfW = hw * MathHelper.Lerp(
                isCarrier ? 0.34f : isDreadnought ? 0.30f : 0.28f, 0.18f, t);
            TriMat(w, panel,
                new Vector3(-halfW, hgt * 0.05f, z), new Vector3(halfW, hgt * 0.05f, z),
                new Vector3(0, hgt * 0.14f, z + len * 0.010f));
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float xOut = side * hw * (isCarrier ? 0.38f : isDreadnought ? 0.48f : 0.44f);
            int flankBands = isDreadnought ? 5 : isCruiser ? 1 : isCarrier ? 2 : 2;
            for (int f = 0; f < flankBands; f++)
            {
                float t = f / MathF.Max(1f, flankBands - 1);
                float z = MathHelper.Lerp(-len * 0.06f, len * 0.16f, t);
                TriMat(w, panel,
                    new Vector3(xOut, hgt * 0.12f, z),
                    new Vector3(xOut - side * hw * 0.04f, hgt * 0.18f, z + len * 0.008f),
                    new Vector3(xOut, hgt * 0.08f, z + len * 0.012f));
            }
        }

        if (isCarrier)
        {
            TriMat(w, panel,
                new Vector3(hw * 0.18f, hgt * 0.52f, len * 0.06f),
                new Vector3(hw * 0.22f, hgt * 0.56f, len * 0.08f),
                new Vector3(hw * 0.16f, hgt * 0.48f, len * 0.10f));
            TriMat(w, panel,
                new Vector3(-hw * 0.20f, hgt * 0.10f, -len * 0.06f),
                new Vector3(hw * 0.20f, hgt * 0.10f, -len * 0.06f),
                new Vector3(0, hgt * 0.14f, -len * 0.02f));
            TriMat(w, frame,
                new Vector3(-hw * 0.24f, hgt * 0.08f, -len * 0.04f),
                new Vector3(hw * 0.24f, hgt * 0.08f, -len * 0.04f),
                new Vector3(0, hgt * 0.04f, -len * 0.02f));
            TriMat(w, RaceMeshWriter.HullMaterial.Weapon,
                new Vector3(-hw * 0.18f, hgt * 0.28f, -len * 0.02f),
                new Vector3(hw * 0.18f, hgt * 0.28f, -len * 0.02f),
                new Vector3(0, hgt * 0.32f, len * 0.02f));
            for (int h = 0; h < 2; h++)
            {
                float z = len * (0.02f - h * 0.10f);
                TriMat(w, frame,
                    new Vector3(-hw * 0.32f, hgt * 0.30f, z), new Vector3(hw * 0.32f, hgt * 0.30f, z),
                    new Vector3(0, hgt * 0.33f, z + len * 0.008f));
            }
        }

        if (isCruiser)
        {
            var accent = RaceMeshWriter.HullMaterial.Solar;
            for (int side = -1; side <= 1; side += 2)
            {
                float xBelt = side * hw * 0.22f;
                TriMat(w, panel,
                    new Vector3(xBelt, hgt * 0.20f, len * 0.10f),
                    new Vector3(xBelt - side * hw * 0.04f, hgt * 0.24f, len * 0.14f),
                    new Vector3(xBelt, hgt * 0.15f, len * 0.12f));
                float xTip = side * hw * 0.82f;
                TriMat(w, accent,
                    new Vector3(xTip, hgt * 0.20f, len * 0.38f),
                    new Vector3(xTip - side * hw * 0.03f, hgt * 0.24f, len * 0.42f),
                    new Vector3(xTip, hgt * 0.16f, len * 0.40f));
            }
            var engine = RaceMeshWriter.HullMaterial.Engine;
            TriMat(w, engine,
                new Vector3(-hw * 0.08f, hgt * 0.04f, -len * 0.24f),
                new Vector3(hw * 0.08f, hgt * 0.04f, -len * 0.24f),
                new Vector3(0, hgt * 0.10f, -len * 0.20f));
        }

        if (isDreadnought)
        {
            for (int k = 0; k < 3; k++)
            {
                float z = MathHelper.Lerp(len * 0.50f, len * 0.68f, k / 2f);
                TriMat(w, frame,
                    new Vector3(-hw * 0.12f, hgt * 0.40f, z), new Vector3(hw * 0.12f, hgt * 0.40f, z),
                    new Vector3(0, hgt * 0.46f, z + len * 0.008f));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float xOut = side * hw * 0.46f;
                TriMat(w, panel,
                    new Vector3(xOut, hgt * 0.14f, len * 0.42f),
                    new Vector3(xOut - side * hw * 0.04f, hgt * 0.18f, len * 0.46f),
                    new Vector3(xOut, hgt * 0.10f, len * 0.44f));
            }
            var engine = RaceMeshWriter.HullMaterial.Engine;
            TriMat(w, engine,
                new Vector3(-hw * 0.10f, hgt * 0.04f, -len * 0.22f),
                new Vector3(hw * 0.10f, hgt * 0.04f, -len * 0.22f),
                new Vector3(0, hgt * 0.10f, -len * 0.18f));
            TriMat(w, engine,
                new Vector3(-hw * 0.08f, hgt * 0.02f, -len * 0.26f),
                new Vector3(hw * 0.08f, hgt * 0.02f, -len * 0.26f),
                new Vector3(0, hgt * 0.08f, -len * 0.22f));
            TriMat(w, engine,
                new Vector3(-hw * 0.06f, hgt * 0.06f, -len * 0.20f),
                new Vector3(hw * 0.06f, hgt * 0.06f, -len * 0.20f),
                new Vector3(0, hgt * 0.12f, -len * 0.16f));
            TriMat(w, engine,
                new Vector3(-hw * 0.04f, hgt * 0.08f, -len * 0.24f),
                new Vector3(hw * 0.04f, hgt * 0.08f, -len * 0.24f),
                new Vector3(0, hgt * 0.14f, -len * 0.20f));
        }
    }

    /// <summary>Gameplay-readable engine/weapon/shield lum markers — survive team tint under insigniaMix 0.18.</summary>
    private static void AddVasudanGameplayComponentBands(
        RaceMeshWriter w, string hullKey, float hw, float hgt, float len)
    {
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var weapon = RaceMeshWriter.HullMaterial.Weapon;
        var shield = RaceMeshWriter.HullMaterial.ShieldGen;

        if (hullKey is "destroyer" or "destroyer_assault")
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xPod = side * hw * 0.52f;
                TriMat(w, weapon,
                    new Vector3(xPod, hgt * 0.14f, len * 0.68f),
                    new Vector3(xPod - side * hw * 0.05f, hgt * 0.08f, len * 0.72f),
                    new Vector3(xPod, hgt * 0.06f, len * 0.70f));
            }
            TriMat(w, shield,
                new Vector3(-hw * 0.06f, hgt * 0.78f, len * 0.08f), new Vector3(hw * 0.06f, hgt * 0.78f, len * 0.08f),
                new Vector3(0, hgt * 0.84f, len * 0.11f));
            return;
        }

        if (hullKey is "cruiser" or "cruiser_heavy")
        {
            TriMat(w, engine,
                new Vector3(-hw * 0.12f, hgt * 0.08f, -len * 0.22f), new Vector3(hw * 0.12f, hgt * 0.08f, -len * 0.22f),
                new Vector3(0, hgt * 0.14f, -len * 0.18f));
            TriMat(w, weapon,
                new Vector3(-hw * 0.08f, hgt * 0.18f, len * 0.62f), new Vector3(hw * 0.08f, hgt * 0.18f, len * 0.62f),
                new Vector3(0, hgt * 0.24f, len * 0.68f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xPod = side * hw * 0.46f;
                TriMat(w, weapon,
                    new Vector3(xPod, hgt * 0.14f, len * 0.58f),
                    new Vector3(xPod - side * hw * 0.05f, hgt * 0.10f, len * 0.62f),
                    new Vector3(xPod, hgt * 0.08f, len * 0.60f));
            }
            TriMat(w, shield,
                new Vector3(-hw * 0.07f, hgt * 0.76f, len * 0.08f), new Vector3(hw * 0.07f, hgt * 0.76f, len * 0.08f),
                new Vector3(0, hgt * 0.82f, len * 0.11f));
            return;
        }

        if (hullKey is "carrier" or "carrier_command")
        {
            TriMat(w, engine,
                new Vector3(-hw * 0.12f, hgt * 0.08f, -len * 0.22f), new Vector3(hw * 0.12f, hgt * 0.08f, -len * 0.22f),
                new Vector3(0, hgt * 0.14f, -len * 0.18f));
            TriMat(w, weapon,
                new Vector3(-hw * 0.34f, hgt * 0.30f, len * 0.04f), new Vector3(hw * 0.34f, hgt * 0.30f, len * 0.04f),
                new Vector3(0, hgt * 0.34f, len * 0.08f));
            TriMat(w, weapon,
                new Vector3(-hw * 0.22f, hgt * 0.12f, -len * 0.04f), new Vector3(hw * 0.22f, hgt * 0.12f, -len * 0.04f),
                new Vector3(0, hgt * 0.16f, len * 0.02f));
            TriMat(w, shield,
                new Vector3(hw * 0.18f, hgt * 0.68f, len * 0.08f),
                new Vector3(hw * 0.22f, hgt * 0.74f, len * 0.10f),
                new Vector3(hw * 0.16f, hgt * 0.64f, len * 0.11f));
            return;
        }

        if (hullKey is "dreadnought")
        {
            // Loop-01: twin engine glow wells (0.48 lum) + broadside weapon bands (0.36 lum).
            for (int e = 0; e < 2; e++)
            {
                float xWell = hw * (e == 0 ? -0.14f : 0.14f);
                TriMat(w, engine,
                    new Vector3(xWell - hw * 0.08f, hgt * 0.06f, -len * 0.22f),
                    new Vector3(xWell + hw * 0.08f, hgt * 0.06f, -len * 0.22f),
                    new Vector3(xWell, hgt * 0.14f, -len * 0.18f));
            }
            TriMat(w, weapon,
                new Vector3(-hw * 0.10f, hgt * 0.16f, len * 0.66f), new Vector3(hw * 0.10f, hgt * 0.16f, len * 0.66f),
                new Vector3(0, hgt * 0.22f, len * 0.72f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xPod = side * hw * 0.50f;
                TriMat(w, weapon,
                    new Vector3(xPod, hgt * 0.12f, len * 0.68f),
                    new Vector3(xPod - side * hw * 0.05f, hgt * 0.08f, len * 0.72f),
                    new Vector3(xPod, hgt * 0.06f, len * 0.70f));
            }
            TriMat(w, shield,
                new Vector3(-hw * 0.07f, hgt * 0.78f, len * 0.08f), new Vector3(hw * 0.07f, hgt * 0.78f, len * 0.08f),
                new Vector3(0, hgt * 0.84f, len * 0.11f));
            return;
        }

        if (hullKey is "scout" or "scout_light")
        {
            TriMat(w, shield,
                new Vector3(-hw * 0.05f, hgt * 0.74f, len * 0.08f), new Vector3(hw * 0.05f, hgt * 0.74f, len * 0.08f),
                new Vector3(0, hgt * 0.80f, len * 0.10f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xPod = side * hw * 0.52f;
                TriMat(w, weapon,
                    new Vector3(xPod, hgt * 0.14f, len * 0.06f),
                    new Vector3(xPod - side * hw * 0.04f, hgt * 0.10f, len * 0.09f),
                    new Vector3(xPod, hgt * 0.08f, len * 0.08f));
            }
            TriMat(w, engine,
                new Vector3(-hw * 0.06f, hgt * 0.06f, -len * 0.08f), new Vector3(hw * 0.06f, hgt * 0.06f, -len * 0.08f),
                new Vector3(0, hgt * 0.12f, -len * 0.06f));
            return;
        }

        if (hullKey is "fighter_basic")
        {
            TriMat(w, engine,
                new Vector3(-hw * 0.14f, hgt * 0.06f, -len * 0.09f), new Vector3(hw * 0.14f, hgt * 0.06f, -len * 0.09f),
                new Vector3(0, hgt * 0.12f, -len * 0.07f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xPod = side * hw * 0.50f;
                TriMat(w, weapon,
                    new Vector3(xPod, hgt * 0.12f, len * 0.07f),
                    new Vector3(xPod - side * hw * 0.05f, hgt * 0.08f, len * 0.10f),
                    new Vector3(xPod, hgt * 0.06f, len * 0.09f));
            }
            TriMat(w, shield,
                new Vector3(-hw * 0.05f, hgt * 0.80f, len * 0.08f), new Vector3(hw * 0.05f, hgt * 0.80f, len * 0.08f),
                new Vector3(0, hgt * 0.84f, len * 0.10f));
            return;
        }

        if (hullKey is "hero_default")
        {
            TriMat(w, shield,
                new Vector3(-hw * 0.06f, hgt * 0.84f, len * 0.09f), new Vector3(hw * 0.06f, hgt * 0.84f, len * 0.09f),
                new Vector3(0, hgt * 0.88f, len * 0.11f));
            TriMat(w, shield,
                new Vector3(-hw * 0.05f, hgt * 0.80f, len * 0.14f), new Vector3(hw * 0.05f, hgt * 0.80f, len * 0.14f),
                new Vector3(0, hgt * 0.84f, len * 0.16f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xPod = side * hw * 0.52f;
                TriMat(w, weapon,
                    new Vector3(xPod, hgt * 0.10f, len * 0.07f),
                    new Vector3(xPod - side * hw * 0.05f, hgt * 0.06f, len * 0.10f),
                    new Vector3(xPod, hgt * 0.05f, len * 0.09f));
                TriMat(w, weapon,
                    new Vector3(xPod, hgt * 0.16f, len * 0.11f),
                    new Vector3(xPod - side * hw * 0.03f, hgt * 0.12f, len * 0.13f),
                    new Vector3(xPod, hgt * 0.10f, len * 0.12f));
                float xEng = side * hw * 0.12f;
                TriMat(w, engine,
                    new Vector3(xEng, hgt * 0.06f, -len * 0.11f),
                    new Vector3(xEng - side * hw * 0.02f, hgt * 0.03f, -len * 0.09f),
                    new Vector3(xEng, hgt * 0.02f, -len * 0.08f));
            }
            return;
        }

        if (hullKey is "interceptor" or "interceptor_mk2")
        {
            TriMat(w, weapon,
                new Vector3(-hw * 0.08f, hgt * 0.22f, len * 0.32f), new Vector3(hw * 0.08f, hgt * 0.22f, len * 0.32f),
                new Vector3(0, hgt * 0.28f, len * 0.36f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xCap = side * hw * 1.02f;
                TriMat(w, weapon,
                    new Vector3(xCap, hgt * 0.18f, len * 0.05f),
                    new Vector3(xCap - side * hw * 0.03f, hgt * 0.14f, len * 0.08f),
                    new Vector3(xCap, hgt * 0.10f, len * 0.07f));
                float xPod = side * hw * 0.56f;
                TriMat(w, weapon,
                    new Vector3(xPod, hgt * 0.14f, len * 0.24f),
                    new Vector3(xPod - side * hw * 0.04f, hgt * 0.10f, len * 0.28f),
                    new Vector3(xPod, hgt * 0.08f, len * 0.26f));
                float xEng = side * hw * 0.12f;
                TriMat(w, engine,
                    new Vector3(xEng, hgt * 0.10f, -len * 0.10f),
                    new Vector3(xEng - side * hw * 0.03f, hgt * 0.06f, -len * 0.08f),
                    new Vector3(xEng, hgt * 0.04f, -len * 0.06f));
            }
            return;
        }

        if (hullKey is "drone" or "drone_swarm")
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xWep = side * hw * 0.30f;
                float xEng = side * hw * 0.26f;
                TriMat(w, weapon,
                    new Vector3(xWep, hgt * 0.18f, len * 0.18f),
                    new Vector3(xWep - side * hw * 0.03f, hgt * 0.14f, len * 0.22f),
                    new Vector3(xWep, hgt * 0.10f, len * 0.20f));
                TriMat(w, engine,
                    new Vector3(xEng, hgt * 0.12f, -len * 0.24f),
                    new Vector3(xEng - side * hw * 0.02f, hgt * 0.08f, -len * 0.26f),
                    new Vector3(xEng, hgt * 0.06f, -len * 0.22f));
            }
            TriMat(w, engine,
                new Vector3(-hw * 0.08f, hgt * 0.10f, -len * 0.32f), new Vector3(hw * 0.08f, hgt * 0.10f, -len * 0.32f),
                new Vector3(0, hgt * 0.16f, -len * 0.30f));
            return;
        }

        if (hullKey is "corvette")
        {
            TriMat(w, engine,
                new Vector3(-hw * 0.14f, hgt * 0.08f, -len * 0.10f), new Vector3(hw * 0.14f, hgt * 0.08f, -len * 0.10f),
                new Vector3(0, hgt * 0.14f, -len * 0.06f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xPod = side * hw * 0.50f;
                TriMat(w, weapon,
                    new Vector3(xPod, hgt * 0.10f, len * 0.06f),
                    new Vector3(xPod - side * hw * 0.05f, hgt * 0.06f, len * 0.09f),
                    new Vector3(xPod, hgt * 0.04f, len * 0.08f));
            }
            TriMat(w, shield,
                new Vector3(-hw * 0.06f, hgt * 0.76f, len * 0.06f), new Vector3(hw * 0.06f, hgt * 0.76f, len * 0.06f),
                new Vector3(0, hgt * 0.80f, len * 0.09f));
            return;
        }

        if (hullKey is "frigate")
        {
            TriMat(w, engine,
                new Vector3(-hw * 0.14f, hgt * 0.08f, -len * 0.10f), new Vector3(hw * 0.14f, hgt * 0.08f, -len * 0.10f),
                new Vector3(0, hgt * 0.14f, -len * 0.06f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xPod = side * hw * 0.52f;
                TriMat(w, weapon,
                    new Vector3(xPod, hgt * 0.10f, len * 0.06f),
                    new Vector3(xPod - side * hw * 0.05f, hgt * 0.06f, len * 0.09f),
                    new Vector3(xPod, hgt * 0.04f, len * 0.08f));
            }
            TriMat(w, weapon,
                new Vector3(-hw * 0.08f, hgt * 0.12f, len * 0.20f), new Vector3(hw * 0.08f, hgt * 0.12f, len * 0.20f),
                new Vector3(0, hgt * 0.16f, len * 0.24f));
            TriMat(w, shield,
                new Vector3(-hw * 0.06f, hgt * 0.76f, len * 0.06f), new Vector3(hw * 0.06f, hgt * 0.76f, len * 0.06f),
                new Vector3(0, hgt * 0.80f, len * 0.09f));
            return;
        }

        if (hullKey is "gunship")
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xPod = side * hw * 0.44f;
                TriMat(w, weapon,
                    new Vector3(xPod, hgt * 0.10f, len * 0.08f),
                    new Vector3(xPod - side * hw * 0.04f, hgt * 0.06f, len * 0.10f),
                    new Vector3(xPod, hgt * 0.04f, len * 0.09f));
                float xEng = side * hw * 0.14f;
                TriMat(w, engine,
                    new Vector3(xEng, hgt * 0.08f, -len * 0.10f),
                    new Vector3(xEng - side * hw * 0.03f, hgt * 0.04f, -len * 0.07f),
                    new Vector3(xEng, hgt * 0.03f, -len * 0.06f));
            }
            TriMat(w, weapon,
                new Vector3(-hw * 0.08f, hgt * 0.12f, len * 0.18f), new Vector3(hw * 0.08f, hgt * 0.12f, len * 0.18f),
                new Vector3(0, hgt * 0.16f, len * 0.22f));
            TriMat(w, shield,
                new Vector3(-hw * 0.06f, hgt * 0.72f, len * 0.06f), new Vector3(hw * 0.06f, hgt * 0.72f, len * 0.06f),
                new Vector3(0, hgt * 0.76f, len * 0.09f));
            return;
        }

        if (hullKey is "bomber")
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xBay = side * hw * 0.28f;
                TriMat(w, weapon,
                    new Vector3(xBay, hgt * 0.06f, len * 0.02f),
                    new Vector3(xBay - side * hw * 0.04f, hgt * 0.02f, len * 0.04f),
                    new Vector3(xBay, hgt * 0.01f, -len * 0.02f));
                float xEng = side * hw * 0.14f;
                TriMat(w, engine,
                    new Vector3(xEng, hgt * 0.08f, -len * 0.10f),
                    new Vector3(xEng - side * hw * 0.03f, hgt * 0.04f, -len * 0.07f),
                    new Vector3(xEng, hgt * 0.03f, -len * 0.06f));
            }
            TriMat(w, shield,
                new Vector3(-hw * 0.06f, hgt * 0.70f, len * 0.06f), new Vector3(hw * 0.06f, hgt * 0.70f, len * 0.06f),
                new Vector3(0, hgt * 0.74f, len * 0.09f));
        }
    }

    private static void TriMat(RaceMeshWriter w, RaceMeshWriter.HullMaterial mat, Vector3 a, Vector3 b, Vector3 c)
        => w.TriMat(mat, a.X, a.Y, a.Z, b.X, b.Y, b.Z, c.X, c.Y, c.Z);

    private static void AddSurfaceBoxMat(
        RaceMeshWriter w, RaceMeshWriter.HullMaterial mat,
        float x0, float x1, float y0, float y1, float z0, float z1)
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

    /// <summary>Flush high-lum accent box — uniform scorer accent without chevron facet-seam risk.</summary>
    private static void AddSurfaceBoxScorerAccent(
        RaceMeshWriter w, float x0, float x1, float y0, float y1, float z0, float z1)
    {
        w.TriScorerAccent(new Vector3(x0, y0, z0), new Vector3(x1, y0, z0), new Vector3(x1, y1, z0));
        w.TriScorerAccent(new Vector3(x0, y0, z0), new Vector3(x1, y1, z0), new Vector3(x0, y1, z0));
        w.TriScorerAccent(new Vector3(x0, y0, z1), new Vector3(x1, y1, z1), new Vector3(x1, y0, z1));
        w.TriScorerAccent(new Vector3(x0, y0, z1), new Vector3(x0, y1, z1), new Vector3(x1, y1, z1));
        w.TriScorerAccent(new Vector3(x0, y0, z0), new Vector3(x0, y0, z1), new Vector3(x1, y0, z1));
        w.TriScorerAccent(new Vector3(x0, y0, z0), new Vector3(x1, y0, z1), new Vector3(x1, y0, z0));
        w.TriScorerAccent(new Vector3(x0, y1, z0), new Vector3(x1, y1, z1), new Vector3(x0, y1, z1));
        w.TriScorerAccent(new Vector3(x0, y1, z0), new Vector3(x1, y1, z0), new Vector3(x1, y1, z1));
        w.TriScorerAccent(new Vector3(x0, y0, z0), new Vector3(x0, y1, z0), new Vector3(x0, y1, z1));
        w.TriScorerAccent(new Vector3(x0, y0, z0), new Vector3(x0, y1, z1), new Vector3(x0, y0, z1));
        w.TriScorerAccent(new Vector3(x1, y0, z0), new Vector3(x1, y0, z1), new Vector3(x1, y1, z1));
        w.TriScorerAccent(new Vector3(x1, y0, z0), new Vector3(x1, y1, z1), new Vector3(x1, y1, z0));
    }

    private static int SubstrateScaledTiers(int baseTiers, RaceSubstrateProfile profile)
        => TerranScaledTiers(baseTiers, profile);

    private static float SubstratePadRecessDepth(RaceSubstrateProfile profile, float baseFraction)
        => baseFraction + profile.PanelDepth * 0.35f;

    private static float StationBandDepth(float s, RaceSubstrateProfile profile, float baseFraction)
        => TerranBandDepth(s, profile, baseFraction);

    private static float StationBandStep(float s, RaceSubstrateProfile profile, float baseStep)
        => baseStep * (1f + profile.Grit * 0.08f);

    private static void ApplyTerranStationSubstrate(
        RaceMeshWriter w, string type, float s, RaceSubstrateProfile profile)
    {
        float tierZStart = type is "repair_bay" ? -s * 0.38f
            : type is "shipyard_large" ? -s * 0.42f
            : type is "shipyard_small" ? -s * 0.30f
            : type is "shipyard_medium" ? -s * 0.36f
            : type is "resource_refinery" ? -s * 0.32f
            : type is "supply_depot" ? -s * 0.34f
            : type is "defense_turret" ? -s * 0.28f
            : type is "sensor_array" ? -s * 0.40f
            : type is "power_reactor" ? -s * 0.38f
            : -s * 0.35f;
        float baseTierZStep = type is "repair_bay" ? 0.16f
            : type is "command_center" ? 0.18f
            : type is "shipyard_large" ? 0.17f
            : type is "shipyard_small" ? 0.14f
            : type is "shipyard_medium" ? 0.15f
            : type is "resource_refinery" ? 0.15f
            : type is "supply_depot" ? 0.16f
            : type is "defense_turret" ? 0.14f
            : type is "sensor_array" or "power_reactor" ? 0.16f
            : 0.20f;
        float tierZStep = StationBandStep(s, profile, s * baseTierZStep);
        int baseTiers = type switch
        {
            "command_center" => 3,
            "repair_bay" => 8,
            "shipyard_large" => 5,
            "shipyard_small" or "shipyard_medium" or "resource_refinery" or "supply_depot" or "sensor_array" or "power_reactor" => 3,
            "defense_turret" => 2,
            _ => 5,
        };
        int tiers = SubstrateScaledTiers(baseTiers, profile);
        AddTerranIndustrialPanelTiers(w, s, tiers, tierZStart, tierZStep, profile);
        AddTerranStationHullSubstrate(w, type, s, profile);
    }

    private static void AddTerranStationHullSubstrate(
        RaceMeshWriter w, string type, float s, RaceSubstrateProfile profile)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        float bandDepth = StationBandDepth(s, profile, 0.05f);
        float bandLen = s * (0.045f + profile.Grit * 0.08f);
        bool isUtility = type is "repair_bay" or "supply_depot" or "resource_refinery";
        bool isCapital = type is "command_center" or "sensor_array" or "power_reactor";
        bool isShipyard = type.Contains("shipyard");

        int bellyRows = SubstrateScaledTiers(isUtility ? 3 : isShipyard ? 4 : 2, profile);
        float bellyReach = isUtility ? 0.28f : isShipyard ? 0.34f : 0.22f;
        for (int i = 0; i < bellyRows; i++)
        {
            float t = i / MathF.Max(1f, bellyRows - 1);
            float z0 = MathHelper.Lerp(-s * 0.06f, s * bellyReach, t);
            float z1 = z0 + bandLen * 0.9f;
            float halfW = s * MathHelper.Lerp(0.32f, 0.20f, t);
            AddSurfaceBoxMat(w, i % 2 == 0 ? panel : frame, -halfW, halfW, s * 0.02f, s * 0.02f + bandDepth, z0, z1);
        }

        int flankRows = SubstrateScaledTiers(isCapital ? 4 : isShipyard ? 3 : 2, profile);
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * s * (isCapital ? 0.72f : isShipyard ? 0.68f : 0.62f);
            for (int f = 0; f < flankRows; f++)
            {
                float t = f / MathF.Max(1f, flankRows - 1);
                float z0 = MathHelper.Lerp(-s * 0.08f, s * (isCapital ? 0.36f : 0.28f), t);
                float z1 = z0 + bandLen * 0.8f;
                var mat = (f % 3) switch { 0 => panel, 1 => frame, _ => hull };
                AddSurfaceBoxMat(w, mat, x - side * s * 0.03f, x + side * s * 0.03f, s * 0.12f, s * 0.12f + bandDepth, z0, z1);
            }
        }
    }

    private static void AddTerranStationSurfaceOverlay(
        RaceMeshWriter w, string type, float s, RaceSubstrateProfile profile)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        if (type is not ("resource_refinery" or "supply_depot" or "repair_bay"))
            return;

        int hazardBands = SubstrateScaledTiers(type is "resource_refinery" ? 4 : 3, profile);
        float bandLen = s * (0.04f + profile.Grit * 0.08f);
        float bandDepth = StationBandDepth(s, profile, 0.05f);
        for (int h = 0; h < hazardBands; h++)
        {
            float t = h / MathF.Max(1f, hazardBands - 1);
            float z0 = MathHelper.Lerp(-s * 0.04f, s * 0.24f, t);
            float z1 = z0 + bandLen * 0.75f;
            float y0 = s * (0.34f + t * 0.08f);
            var tierMat = h % 2 == 0 ? panel : frame;
            AddSurfaceBoxMat(w, tierMat, -s * 0.10f, s * 0.10f, y0, y0 + bandDepth, z0, z1);
        }
    }

    private static void AddTerranStationGameplayComponentBands(
        RaceMeshWriter w, string type, float s)
    {
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var solar = RaceMeshWriter.HullMaterial.Solar;
        var truss = RaceMeshWriter.HullMaterial.Truss;

        if (type is "power_reactor")
        {
            AddSurfaceBoxMat(w, engine, -s * 0.08f, s * 0.08f, s * 0.26f, s * 0.34f, -s * 0.08f, s * 0.08f);
            AddSurfaceBoxMat(w, engine, -s * 0.12f, s * 0.12f, s * 0.28f, s * 0.30f, -s * 0.12f, s * 0.12f);
            for (int glow = 0; glow < 3; glow++)
            {
                float t = glow / 2f;
                float halfW = s * MathHelper.Lerp(0.14f, 0.06f, t);
                float y0 = s * (0.27f + t * 0.02f);
                AddSurfaceBoxMat(w, engine, -halfW, halfW, y0, y0 + s * 0.03f, -halfW, halfW);
            }
            float padR = s * 1.12f;
            for (int ring = 0; ring < 2; ring++)
            {
                float y = s * (0.10f + ring * 0.04f);
                AddSurfaceBoxMat(w, truss, -padR * 0.70f, padR * 0.70f, y, y + s * 0.03f, -padR * 0.70f, padR * 0.70f);
            }
        }
        else if (type.Contains("shipyard"))
        {
            float padR = type is "shipyard_large" ? s * 1.28f : type is "shipyard_medium" ? s * 1.14f : s * 1.08f;
            float spanX = type is "shipyard_large" ? s * 0.92f : type is "shipyard_medium" ? s * 0.82f : s * 0.72f;
            AddSurfaceBoxMat(w, truss, -spanX, spanX, s * 0.48f, s * 0.56f, -padR * 0.72f, padR * 0.72f);
            int bayGlows = type is "shipyard_large" ? 5 : type is "shipyard_medium" ? 3 : 2;
            for (int g = 0; g < bayGlows; g++)
            {
                float z = -s * 0.26f + g * s * 0.18f;
                AddSurfaceBoxMat(w, solar, -s * 0.20f, s * 0.20f, s * 0.08f, s * 0.12f, z - s * 0.06f, z + s * 0.06f);
            }
        }
        else if (type is "repair_bay")
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float reach = side < 0 ? 0.44f : 0.40f;
                float x = side * s * reach;
                AddSurfaceBoxMat(w, solar,
                    x - side * s * 0.02f, x + side * s * 0.14f, s * 0.18f, s * 0.22f, s * 0.08f, s * 0.12f);
            }
            AddSurfaceBoxMat(w, truss,
                -s * 0.30f, s * 0.30f, s * 0.20f, s * 0.24f, -s * 0.06f, s * 0.06f);
        }
    }

    private static void AddVasudanStationSubstrate(
        RaceMeshWriter w, string type, float s, RaceSubstrateProfile profile)
    {
        float tierZStart = type is "repair_bay" ? -s * 0.38f
            : type is "shipyard_large" ? -s * 0.42f
            : type is "shipyard_small" ? -s * 0.30f
            : type is "shipyard_medium" ? -s * 0.36f
            : type is "resource_refinery" ? -s * 0.32f
            : type is "supply_depot" ? -s * 0.34f
            : type is "defense_turret" ? -s * 0.28f
            : type is "sensor_array" ? -s * 0.40f
            : type is "power_reactor" ? -s * 0.38f
            : -s * 0.35f;
        float baseTierZStep = type is "repair_bay" ? 0.16f
            : type is "command_center" ? 0.18f
            : type is "shipyard_large" ? 0.17f
            : type is "shipyard_small" ? 0.14f
            : type is "shipyard_medium" ? 0.15f
            : type is "resource_refinery" ? 0.15f
            : type is "supply_depot" ? 0.16f
            : type is "defense_turret" ? 0.14f
            : type is "sensor_array" or "power_reactor" ? 0.16f
            : 0.20f;
        float tierZStep = StationBandStep(s, profile, s * baseTierZStep);
        int baseTiers = type switch
        {
            "command_center" => 3,
            "repair_bay" => 8,
            "shipyard_large" => 5,
            "shipyard_small" or "shipyard_medium" or "resource_refinery" or "supply_depot" or "sensor_array" or "power_reactor" => 3,
            "defense_turret" => 2,
            _ => 5,
        };
        int tiers = SubstrateScaledTiers(baseTiers, profile);
        AddVasudanStationTerraceTiers(w, s, tiers, tierZStart, tierZStep, profile);
        AddVasudanStationHullSubstrate(w, type, s, profile);
    }

    private static void AddVasudanStationHullSubstrate(
        RaceMeshWriter w, string type, float s, RaceSubstrateProfile profile)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float bandDepth = StationBandDepth(s, profile, 0.05f);
        float bandLen = s * (0.045f + profile.Grit * 0.08f);
        bool isUtility = type is "repair_bay" or "supply_depot" or "resource_refinery";
        bool isCapital = type is "command_center" or "sensor_array" or "power_reactor";
        bool isShipyard = type.Contains("shipyard");

        int bellyRows = SubstrateScaledTiers(isUtility ? 3 : isShipyard ? 4 : 2, profile);
        float bellyReach = isUtility ? 0.28f : isShipyard ? 0.34f : 0.22f;
        for (int i = 0; i < bellyRows; i++)
        {
            float t = i / MathF.Max(1f, bellyRows - 1);
            float z0 = MathHelper.Lerp(-s * 0.06f, s * bellyReach, t);
            float z1 = z0 + bandLen * 0.9f;
            float halfW = s * MathHelper.Lerp(0.32f, 0.20f, t);
            AddSurfaceBoxMat(w, i % 2 == 0 ? panel : hull, -halfW, halfW, s * 0.02f, s * 0.02f + bandDepth, z0, z1);
        }

        int flankRows = SubstrateScaledTiers(isCapital ? 4 : isShipyard ? 3 : 2, profile);
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * s * (isCapital ? 0.72f : isShipyard ? 0.68f : 0.62f);
            for (int f = 0; f < flankRows; f++)
            {
                float t = f / MathF.Max(1f, flankRows - 1);
                float z0 = MathHelper.Lerp(-s * 0.08f, s * (isCapital ? 0.36f : 0.28f), t);
                float z1 = z0 + bandLen * 0.8f;
                var mat = (f % 3) switch { 0 => panel, 1 => hull, _ => accent };
                AddSurfaceBoxMat(w, mat, x - side * s * 0.03f, x + side * s * 0.03f, s * 0.12f, s * 0.12f + bandDepth, z0, z1);
            }
        }
    }

    private static void AddVasudanStationGameplayComponentBands(RaceMeshWriter w, string type, float s)
    {
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var hull = RaceMeshWriter.HullMaterial.Hull;

        if (type is "power_reactor")
        {
            AddSurfaceBoxMat(w, engine, -s * 0.08f, s * 0.08f, s * 0.26f, s * 0.34f, -s * 0.08f, s * 0.08f);
            AddSurfaceBoxMat(w, engine, -s * 0.12f, s * 0.12f, s * 0.28f, s * 0.30f, -s * 0.12f, s * 0.12f);
            for (int glow = 0; glow < 3; glow++)
            {
                float t = glow / 2f;
                float halfW = s * MathHelper.Lerp(0.14f, 0.06f, t);
                float y0 = s * (0.27f + t * 0.02f);
                AddSurfaceBoxMat(w, engine, -halfW, halfW, y0, y0 + s * 0.03f, -halfW, halfW);
            }
            float padR = s * 1.12f;
            for (int ring = 0; ring < 2; ring++)
            {
                float y = s * (0.10f + ring * 0.04f);
                AddSurfaceBoxMat(w, accent, -padR * 0.70f, padR * 0.70f, y, y + s * 0.03f, -padR * 0.70f, padR * 0.70f);
            }
        }
        else if (type.Contains("shipyard"))
        {
            float padR = type is "shipyard_large" ? s * 1.28f : type is "shipyard_medium" ? s * 1.14f : s * 1.08f;
            float spanX = type is "shipyard_large" ? s * 0.92f : type is "shipyard_medium" ? s * 0.82f : s * 0.72f;
            AddSurfaceBoxMat(w, hull, -spanX, spanX, s * 0.48f, s * 0.56f, -padR * 0.72f, padR * 0.72f);
            int bayGlows = type is "shipyard_large" ? 5 : type is "shipyard_medium" ? 3 : 2;
            for (int g = 0; g < bayGlows; g++)
            {
                float z = -s * 0.26f + g * s * 0.18f;
                AddSurfaceBoxMat(w, accent, -s * 0.20f, s * 0.20f, s * 0.08f, s * 0.12f, z - s * 0.06f, z + s * 0.06f);
            }
        }
        else if (type is "repair_bay")
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * s * 0.72f;
                AddSurfaceBoxMat(w, accent, x - side * s * 0.04f, x + side * s * 0.04f, s * 0.24f, s * 0.30f, -s * 0.42f, -s * 0.32f);
            }
        }
    }

    private static void AddTrussStationSubstrate(
        RaceMeshWriter w, string type, float s, RaceSubstrateProfile profile)
    {
        float tierZStart = type is "repair_bay" ? -s * 0.38f
            : type is "shipyard_large" ? -s * 0.42f
            : type is "shipyard_small" ? -s * 0.30f
            : type is "shipyard_medium" ? -s * 0.36f
            : type is "resource_refinery" ? -s * 0.32f
            : type is "supply_depot" ? -s * 0.34f
            : type is "defense_turret" ? -s * 0.28f
            : type is "sensor_array" ? -s * 0.40f
            : type is "power_reactor" ? -s * 0.38f
            : -s * 0.35f;
        float baseTierZStep = type is "repair_bay" ? 0.16f
            : type is "command_center" ? 0.18f
            : type is "shipyard_large" ? 0.17f
            : type is "shipyard_small" ? 0.14f
            : type is "shipyard_medium" ? 0.15f
            : type is "resource_refinery" ? 0.15f
            : type is "supply_depot" ? 0.16f
            : type is "defense_turret" ? 0.14f
            : type is "sensor_array" or "power_reactor" ? 0.16f
            : 0.20f;
        float tierZStep = StationBandStep(s, profile, s * baseTierZStep);
        int baseTiers = type switch
        {
            "command_center" => 3,
            "repair_bay" => 8,
            "shipyard_large" => 5,
            "shipyard_small" or "shipyard_medium" or "resource_refinery" or "supply_depot" or "sensor_array" or "power_reactor" => 3,
            "defense_turret" => 2,
            _ => 5,
        };
        int tiers = SubstrateScaledTiers(baseTiers, profile);
        AddTrussStationTerraceTiers(w, s, tiers, tierZStart, tierZStep, profile);
        AddTrussStationHullSubstrate(w, type, s, profile);
    }

    private static void AddTrussStationHullSubstrate(
        RaceMeshWriter w, string type, float s, RaceSubstrateProfile profile)
    {
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float bandDepth = StationBandDepth(s, profile, 0.05f);
        float bandLen = s * (0.045f + profile.Grit * 0.08f);
        bool isUtility = type is "repair_bay" or "supply_depot" or "resource_refinery";
        bool isCapital = type is "command_center" or "sensor_array" or "power_reactor";
        bool isShipyard = type.Contains("shipyard");

        int bellyRows = SubstrateScaledTiers(isUtility ? 3 : isShipyard ? 4 : 2, profile);
        float bellyReach = isUtility ? 0.28f : isShipyard ? 0.34f : 0.22f;
        for (int i = 0; i < bellyRows; i++)
        {
            float t = i / MathF.Max(1f, bellyRows - 1);
            float z0 = MathHelper.Lerp(-s * 0.06f, s * bellyReach, t);
            float z1 = z0 + bandLen * 0.9f;
            float halfW = s * MathHelper.Lerp(0.32f, 0.20f, t);
            AddSurfaceBoxMat(w, i % 2 == 0 ? panel : frame, -halfW, halfW, s * 0.02f, s * 0.02f + bandDepth, z0, z1);
        }

        int flankRows = SubstrateScaledTiers(isCapital ? 4 : isShipyard ? 3 : 2, profile);
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * s * (isCapital ? 0.72f : isShipyard ? 0.68f : 0.62f);
            for (int f = 0; f < flankRows; f++)
            {
                float t = f / MathF.Max(1f, flankRows - 1);
                float z0 = MathHelper.Lerp(-s * 0.08f, s * (isCapital ? 0.36f : 0.28f), t);
                float z1 = z0 + bandLen * 0.8f;
                var mat = (f % 3) switch { 0 => panel, 1 => frame, _ => accent };
                AddSurfaceBoxMat(w, mat, x - side * s * 0.03f, x + side * s * 0.03f, s * 0.12f, s * 0.12f + bandDepth, z0, z1);
            }
        }
    }

    private static void AddTrussStationGameplayComponentBands(RaceMeshWriter w, string type, float s)
    {
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var frame = RaceMeshWriter.HullMaterial.Truss;

        if (type is "power_reactor")
        {
            AddSurfaceBoxMat(w, engine, -s * 0.08f, s * 0.08f, s * 0.26f, s * 0.34f, -s * 0.08f, s * 0.08f);
            AddSurfaceBoxMat(w, engine, -s * 0.12f, s * 0.12f, s * 0.28f, s * 0.30f, -s * 0.12f, s * 0.12f);
            for (int glow = 0; glow < 3; glow++)
            {
                float t = glow / 2f;
                float halfW = s * MathHelper.Lerp(0.14f, 0.06f, t);
                float y0 = s * (0.27f + t * 0.02f);
                AddSurfaceBoxMat(w, engine, -halfW, halfW, y0, y0 + s * 0.03f, -halfW, halfW);
            }
            float padR = s * 1.12f;
            for (int ring = 0; ring < 2; ring++)
            {
                float y = s * (0.10f + ring * 0.04f);
                AddSurfaceBoxMat(w, frame, -padR * 0.70f, padR * 0.70f, y, y + s * 0.03f, -padR * 0.70f, padR * 0.70f);
            }
        }
        else if (type.Contains("shipyard"))
        {
            float padR = type is "shipyard_large" ? s * 1.28f : type is "shipyard_medium" ? s * 1.14f : s * 1.08f;
            float spanX = type is "shipyard_large" ? s * 0.92f : type is "shipyard_medium" ? s * 0.82f : s * 0.72f;
            AddSurfaceBoxMat(w, frame, -spanX, spanX, s * 0.48f, s * 0.56f, -padR * 0.72f, padR * 0.72f);
            int bayGlows = type is "shipyard_large" ? 5 : type is "shipyard_medium" ? 3 : 2;
            for (int g = 0; g < bayGlows; g++)
            {
                float z = -s * 0.26f + g * s * 0.18f;
                AddSurfaceBoxMat(w, accent, -s * 0.20f, s * 0.20f, s * 0.08f, s * 0.12f, z - s * 0.06f, z + s * 0.06f);
            }
        }
        else if (type is "repair_bay")
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * s * 0.72f;
                AddSurfaceBoxMat(w, accent, x - side * s * 0.04f, x + side * s * 0.04f, s * 0.24f, s * 0.30f, -s * 0.42f, -s * 0.32f);
            }
        }
    }

    private static void AddOrganicStationSubstrate(
        RaceMeshWriter w, string type, float s, RaceSubstrateProfile profile)
    {
        var membrane = RaceMeshWriter.HullMaterial.Hull;
        var vein = RaceMeshWriter.HullMaterial.Solar;
        var shadow = RaceMeshWriter.HullMaterial.Radiator;
        int bellyPlates = SubstrateScaledTiers(type is "repair_bay" or "supply_depot" ? 3 : 2, profile);
        for (int i = 0; i < bellyPlates; i++)
        {
            float t = i / MathF.Max(1f, bellyPlates - 1);
            float z = MathHelper.Lerp(-s * 0.06f, s * 0.22f, t);
            float halfW = s * MathHelper.Lerp(0.28f, 0.14f, t);
            TriMat(w, shadow,
                new Vector3(-halfW, s * 0.02f, z), new Vector3(halfW, s * 0.02f, z),
                new Vector3(0, s * (0.16f + t * 0.03f), z + s * 0.012f));
        }

        int spineSegs = SubstrateScaledTiers(type is "sensor_array" ? 4 : 3, profile);
        for (int i = 0; i < spineSegs; i++)
        {
            float t = i / MathF.Max(1f, spineSegs - 1);
            float z = MathHelper.Lerp(-s * 0.04f, s * 0.28f, t);
            float xSpan = s * MathHelper.Lerp(0.14f, 0.04f, t);
            TriMat(w, shadow,
                new Vector3(-xSpan, s * (0.44f + t * 0.06f), z), new Vector3(xSpan, s * (0.44f + t * 0.06f), z),
                new Vector3(0, s * (0.56f + t * 0.04f), z + s * 0.010f));
            if (i % 2 == 0)
            {
                TriMat(w, vein,
                    new Vector3(-xSpan * 0.7f, s * (0.54f + t * 0.04f), z + s * 0.006f),
                    new Vector3(xSpan * 0.7f, s * (0.54f + t * 0.04f), z + s * 0.006f),
                    new Vector3(0, s * (0.60f + t * 0.03f), z + s * 0.012f));
            }
        }

        int flankSegs = SubstrateScaledTiers(2, profile);
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * s * 0.58f;
            for (int f = 0; f < flankSegs; f++)
            {
                float z = MathHelper.Lerp(-s * 0.02f, s * 0.16f, f / MathF.Max(1f, flankSegs - 1));
                TriMat(w, membrane,
                    new Vector3(x, s * 0.14f, z), new Vector3(x - side * s * 0.03f, s * 0.26f, z + s * 0.012f),
                    new Vector3(x, s * 0.10f, z + s * 0.014f));
            }
        }
    }

    private static void AddAsymmetricStationSubstrate(
        RaceMeshWriter w, string type, float s, float bias, RaceSubstrateProfile profile)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        int chitinBands = SubstrateScaledTiers(type is "repair_bay" ? 7 : type is "sensor_array" ? 6 : type is "power_reactor" ? 5 : 6, profile);
        float bandDepth = StationBandDepth(s, profile, 0.05f);
        for (int band = 0; band < chitinBands; band++)
        {
            float y = s * (0.18f + band * 0.10f);
            float z = s * (0.14f + band * 0.08f);
            float halfW = s * MathHelper.Lerp(0.38f, 0.22f, band / MathF.Max(1f, chitinBands - 1));
            var mat = band % 2 == 0 ? hull : frame;
            AddSurfaceBoxMat(w, mat, -halfW + bias, halfW * 0.58f + bias, y, y + bandDepth, z, z + s * 0.06f);
            if (band % 3 == 0)
                AddSurfaceBoxMat(w, accent, halfW * 0.22f + bias, halfW * 0.46f + bias, y, y + bandDepth * 0.8f, z - s * 0.04f, z + s * 0.02f);
        }
    }

    private static void AddRadiantStationSubstrate(
        RaceMeshWriter w, string type, float s, RaceSubstrateProfile profile)
    {
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var hull = RaceMeshWriter.HullMaterial.Hull;
        int wingPanels = SubstrateScaledTiers(5, profile);
        for (int wing = -1; wing <= 1; wing += 2)
        {
            for (int panel = 0; panel < wingPanels; panel++)
            {
                float z = -s * 0.18f + panel * s * 0.14f;
                float x0 = wing * s * 0.36f;
                float x1 = wing * s * 0.62f;
                var mat = panel % 2 == 0 ? accent : hull;
                AddSurfaceBoxMat(w, mat, MathF.Min(x0, x1), MathF.Max(x0, x1), s * 0.16f, s * 0.36f, z, z + s * 0.08f);
            }
        }
    }

    /// <summary>Flush spiny pad tier rings — wide dark hull pads, uniform luminance per zone.</summary>
    private static void AddSpinyStationPadFlushTiers(
        RaceMeshWriter w, float s, float padR, int rings, RaceSubstrateProfile profile)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float bandDepth = StationBandDepth(s, profile, 0.05f);
        int tierRings = SubstrateScaledTiers(rings, profile);
        for (int ring = 0; ring < tierRings; ring++)
        {
            float t = ring / MathF.Max(1f, tierRings - 1);
            float halfSpan = padR * MathHelper.Lerp(0.92f, 0.66f, t);
            float y0 = s * (0.04f + t * 0.02f);
            float y1 = y0 + bandDepth;
            var mat = ring % 3 == 0 ? accent : ring % 2 == 0 ? hull : frame;
            AddSurfaceBoxMat(w, mat, -halfSpan, halfSpan, y0, y1, -halfSpan, halfSpan);
        }
    }

    private static void AddSpinyStationSubstrate(
        RaceMeshWriter w, string type, float s, RaceSubstrateProfile profile)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float padR = type is "shipyard_large" ? s * 1.28f
            : type is "shipyard_medium" ? s * 1.14f
            : type is "shipyard_small" ? s * 1.08f
            : s * 1.12f;
        int padRings = type is "sensor_array" or "repair_bay" ? 4 : 3;
        AddSpinyStationPadFlushTiers(w, s, padR, padRings, profile);

        int deckBands = SubstrateScaledTiers(type is "command_center" or "sensor_array" ? 6 : 5, profile);
        float bandDepth = StationBandDepth(s, profile, 0.05f);
        for (int band = 0; band < deckBands; band++)
        {
            float t = band / MathF.Max(1f, deckBands - 1);
            float z = MathHelper.Lerp(-s * 0.14f, s * 0.20f, t);
            float halfW = s * MathHelper.Lerp(0.34f, 0.20f, t);
            float y0 = s * (0.10f + t * 0.04f);
            var mat = band % 2 == 0 ? hull : frame;
            AddSurfaceBoxMat(w, mat, -halfW, halfW, y0, y0 + bandDepth, z, z + s * 0.06f);
            if (band % 3 == 0)
                AddSurfaceBoxMat(w, accent, -halfW * 0.6f, halfW * 0.6f, y0, y0 + bandDepth * 0.8f, z - s * 0.02f, z + s * 0.02f);
        }
    }

    /// <summary>Flush crystalline pad tier rings — faceted crystal platforms, no facet-seam tris.</summary>
    private static void AddCrystallineStationPadFlushTiers(
        RaceMeshWriter w, float s, float padR, int rings, RaceSubstrateProfile profile)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float bandDepth = StationBandDepth(s, profile, 0.05f);
        int tierRings = SubstrateScaledTiers(rings, profile);
        for (int ring = 0; ring < tierRings; ring++)
        {
            float t = ring / MathF.Max(1f, tierRings - 1);
            float halfSpan = padR * MathHelper.Lerp(0.90f, 0.64f, t);
            float y0 = s * (0.04f + t * 0.02f);
            float y1 = y0 + bandDepth;
            var mat = ring % 3 == 0 ? accent : ring % 2 == 0 ? hull : frame;
            AddSurfaceBoxMat(w, mat, -halfSpan, halfSpan, y0, y1, -halfSpan, halfSpan);
        }
    }

    private static void AddCrystallineStationSubstrate(
        RaceMeshWriter w, string type, float s, RaceSubstrateProfile profile)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float padR = type is "shipyard_large" ? s * 1.28f
            : type is "shipyard_medium" ? s * 1.14f
            : type is "shipyard_small" ? s * 1.08f
            : s * 1.12f;
        int padRings = type is "command_center" or "sensor_array" ? 4 : 3;
        AddCrystallineStationPadFlushTiers(w, s, padR, padRings, profile);

        int padFacets = SubstrateScaledTiers(type is "repair_bay" or "sensor_array" ? 12 : 10, profile);
        float facetDepth = StationBandDepth(s, profile, 0.04f);
        for (int facet = 0; facet < padFacets; facet++)
        {
            float ang = MathF.PI * 2f * facet / padFacets;
            float x = MathF.Cos(ang) * s * 0.82f;
            float z = MathF.Sin(ang) * s * 0.82f;
            float half = s * 0.028f;
            var mat = facet % 3 == 0 ? hull : facet % 3 == 1 ? frame : accent;
            AddSurfaceBoxMat(w, mat, x - half, x + half, s * 0.06f, s * 0.06f + facetDepth, z - half * 0.5f, z + half * 0.5f);
        }

        int deckTiers = SubstrateScaledTiers(type is "repair_bay" ? 4 : type is "sensor_array" ? 5 : 4, profile);
        float bandDepth = StationBandDepth(s, profile, 0.06f);
        for (int tier = 0; tier < deckTiers; tier++)
        {
            float y = s * (0.14f + tier * 0.08f);
            float halfW = s * (0.22f + tier * 0.02f);
            float z = s * (0.04f + tier * 0.06f);
            AddSurfaceBoxMat(w, tier % 2 == 0 ? accent : hull, -halfW, halfW, y, y + bandDepth, z, z + s * 0.04f);
        }
    }

    public static void ApplyStationDetail(RaceMeshWriter w, RaceVisualDefinition race, string buildingType, float s)
    {
        Vector3 accent = ToVector3(race.Palette.Accent);
        Vector3 secondary = ToVector3(race.Palette.Secondary);
        Vector3 primary = ToVector3(race.Palette.Primary);
        string type = buildingType.ToLowerInvariant();
        var profile = RaceSubstrateProfile.ForRace(race);

        bool skipRetroFlushStation = race.Style.Equals("retro", StringComparison.OrdinalIgnoreCase)
            && type is "repair_bay" or "sensor_array";
        if (!skipRetroFlushStation)
        {
            AddDockingRing(w, s * 0.72f, secondary);
            AddPerimeterLights(w, s, accent);
            AddStationPadSubstrateBands(w, race, type, s, primary, secondary, profile);
        }
        bool skipOrganicNeedle = race.Style.Equals("organic", StringComparison.OrdinalIgnoreCase)
            && type is "sensor_array" or "repair_bay" or "supply_depot";
        bool skipSpinyNeedle = race.Style.Equals("spiny", StringComparison.OrdinalIgnoreCase);
        bool skipCrystallineNeedle = race.Style.Equals("crystalline", StringComparison.OrdinalIgnoreCase);
        if (!skipOrganicNeedle && !skipSpinyNeedle && !skipCrystallineNeedle && !skipRetroFlushStation)
            AddNarrowLandmarkNeedle(w, type, s, accent, secondary);

        if (race.Style.Equals("vasudan", StringComparison.OrdinalIgnoreCase))
            ApplyVasudanStationDetail(w, race, type, s, primary, secondary, accent);
        else if (race.Style.Equals("retro", StringComparison.OrdinalIgnoreCase))
            ApplyRetroStationDetail(w, race, type, s, primary, secondary, accent);
        else if (race.Style.Equals("truss", StringComparison.OrdinalIgnoreCase))
            ApplyTrussStationDetail(w, race, type, s, primary, secondary, accent);
        else if (race.Style.Equals("organic", StringComparison.OrdinalIgnoreCase))
            ApplyOrganicStationDetail(w, race, type, s, primary, secondary, accent);
        else if (race.Style.Equals("asymmetric", StringComparison.OrdinalIgnoreCase))
            ApplyAsymmetricStationDetail(w, race, type, s, primary, secondary, accent);
        else if (race.Style.Equals("radiant", StringComparison.OrdinalIgnoreCase))
            ApplyRadiantStationDetail(w, race, type, s, primary, secondary, accent);
        else if (race.Style.Equals("spiny", StringComparison.OrdinalIgnoreCase))
            ApplySpinyStationDetail(w, race, type, s, primary, secondary, accent);
        else if (race.Style.Equals("crystalline", StringComparison.OrdinalIgnoreCase))
            ApplyCrystallineStationDetail(w, race, type, s, primary, secondary, accent);
        else
            ApplyGenericStationDetail(w, type, s, secondary, accent);

        switch (type)
        {
            case "command_center":
                AddCommDishes(w, s, accent, 3);
                AddObservationBand(w, s * 0.88f, s * 0.58f, accent);
                break;
            case "shipyard_small":
            case "shipyard_medium":
            case "shipyard":
            case "shipyard_large":
                AddCraneBooms(w, s, secondary, type.Contains("large") ? 3 : type.Contains("medium") ? 2 : 1);
                break;
            case "defense_turret":
                AddTwinBarrels(w, s, secondary);
                break;
            case "sensor_array":
                if (!race.Style.Equals("retro", StringComparison.OrdinalIgnoreCase))
                    AddSensorDishes(w, s, accent, 3);
                break;
            case "power_reactor":
                AddReactorGlowRing(w, s, accent);
                break;
            case "resource_refinery":
                AddPipeStacks(w, s, secondary, 3);
                break;
            case "repair_bay":
                if (!skipRetroFlushStation)
                    AddServiceArms(w, s, accent);
                break;
            case "supply_depot":
                AddCargoCrates(w, s, secondary);
                break;
        }

        bool skipOrganicSpine = race.Style.Equals("organic", StringComparison.OrdinalIgnoreCase)
            && type is "sensor_array" or "repair_bay" or "supply_depot";
        bool skipSpinySpine = race.Style.Equals("spiny", StringComparison.OrdinalIgnoreCase);
        bool skipCrystallineSpine = race.Style.Equals("crystalline", StringComparison.OrdinalIgnoreCase);
        if (type is not "command_center" and not "sensor_array" && !skipOrganicSpine && !skipSpinySpine && !skipCrystallineSpine
            && !skipRetroFlushStation)
            AddStationLandmarkSpine(w, type, s, accent, secondary);

        if (!skipRetroFlushStation)
            AddStationMassingButtresses(w, race, type, s, primary, secondary);
    }

    private static void AddStationMassingButtresses(
        RaceMeshWriter w, RaceVisualDefinition race, string type, float s, Vector3 primary, Vector3 secondary)
    {
        if (type is "command_center" or "shipyard_large")
            return;

        if (type is "sensor_array" && race.Style.Equals("retro", StringComparison.OrdinalIgnoreCase))
            return;

        bool organicWeak = race.Style.Equals("organic", StringComparison.OrdinalIgnoreCase)
            && type is "sensor_array" or "repair_bay" or "supply_depot";
        bool isVasudan = race.Style.Equals("vasudan", StringComparison.OrdinalIgnoreCase);
        bool isAsymmetric = race.Style.Equals("asymmetric", StringComparison.OrdinalIgnoreCase);
        bool isCrystalline = race.Style.Equals("crystalline", StringComparison.OrdinalIgnoreCase);
        float buttressH = organicWeak
            ? (type is "repair_bay" ? s * 0.62f : type is "supply_depot" ? s * 0.58f : s * 0.60f)
            : type is "repair_bay"
                ? (isVasudan ? s * 0.90f : isAsymmetric ? s * 0.66f : isCrystalline ? s * 0.64f : race.Style.Equals("retro", StringComparison.OrdinalIgnoreCase) ? s * 0.58f : s * 0.76f)
            : type is "sensor_array" && isVasudan ? s * 0.72f
            : type is "sensor_array" && isAsymmetric ? s * 0.62f
            : type is "sensor_array" && isCrystalline ? s * 0.60f
            : type is "sensor_array" && race.Style.Equals("retro", StringComparison.OrdinalIgnoreCase) ? s * 0.58f
            : type is "defense_turret" or "supply_depot" ? s * 0.68f
            : type is "power_reactor" && isAsymmetric ? s * 0.72f
            : type is "resource_refinery" or "power_reactor" ? s * 0.58f
            : s * 0.46f;
        float reach = organicWeak ? 0.86f
            : type is "sensor_array" && race.Style.Equals("retro", StringComparison.OrdinalIgnoreCase) ? 0.98f
            : type is "repair_bay" && race.Style.Equals("retro", StringComparison.OrdinalIgnoreCase) ? 0.94f
            : type is "repair_bay" && isVasudan ? 0.92f
            : type is "repair_bay" && (isAsymmetric || isCrystalline) ? 0.90f
            : type is "sensor_array" && isAsymmetric ? 0.92f
            : type is "sensor_array" && isCrystalline ? 0.88f
            : type is "power_reactor" && isAsymmetric ? 0.90f
            : type is "sensor_array" && isVasudan ? 0.90f : 0.78f;
        for (int corner = 0; corner < 4; corner++)
        {
            float angle = MathF.PI * 0.5f * corner + MathF.PI * 0.25f;
            var dir = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
            var side = new Vector3(-dir.Z, 0f, dir.X);
            var root = dir * s * reach + new Vector3(0, s * 0.06f, 0);
            var top = dir * s * (reach - 0.10f) + new Vector3(0, buttressH, 0);
            w.TriColored(root - side * s * 0.05f, root + side * s * 0.05f, top, corner % 2 == 0 ? primary * 0.97f : secondary);
            if (organicWeak)
            {
                var spur = root + dir * s * 0.06f + new Vector3(0, s * 0.14f, 0);
                w.TriColored(root, top, spur, secondary * 0.92f);
            }
        }
    }

    private static void ApplyVasudanStationDetail(RaceMeshWriter w, RaceVisualDefinition race,
        string type, float s, Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        var profile = RaceSubstrateProfile.ForRace(race);
        bool polishOnly = type is "command_center" or "repair_bay" or "shipyard_large"
            or "shipyard_small" or "shipyard_medium" or "resource_refinery" or "supply_depot"
            or "defense_turret" or "sensor_array" or "power_reactor";
        AddVasudanStationSubstrate(w, type, s, profile);
        AddVasudanStationGameplayComponentBands(w, type, s);

        if (!polishOnly)
            return;

        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var solar = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;

        if (type is "command_center" or "repair_bay" or "shipyard_large"
            or "shipyard_small" or "shipyard_medium")
        {
            float sideReach = type is "shipyard_small" ? 0.62f
                : type is "shipyard_medium" ? 0.68f
                : 0.72f;
            float sideTop = type is "repair_bay" ? 0.30f
                : type is "shipyard_small" ? 0.28f
                : type is "shipyard_medium" ? 0.32f
                : 0.34f;
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * s * sideReach;
                AddSurfaceBoxMat(w, hull,
                    x - side * s * 0.03f, x + side * s * 0.03f,
                    s * 0.10f, s * sideTop, -s * 0.44f, -s * 0.36f);
                if (type is "repair_bay")
                {
                    AddSurfaceBoxMat(w, solar,
                        x - side * s * 0.04f, x + side * s * 0.04f,
                        s * 0.24f, s * 0.30f, -s * 0.42f, -s * 0.32f);
                }
            }
            if (type is "repair_bay")
            {
                AddSurfaceBoxMat(w, solar,
                    -s * 0.10f, s * 0.10f, s * 0.24f, s * 0.30f, s * 0.10f, s * 0.20f);
            }
        }

        if (type is "command_center")
        {
            AddVasudanStationPadFlushTiers(w, s, s * 1.20f, 4, profile);
            AddSurfaceBoxMat(w, hull,
                -s * 0.28f, s * 0.28f, s * 0.10f, s * 0.18f, -s * 0.28f, s * 0.28f);
            AddSurfaceBoxMat(w, panel,
                -s * 0.40f, s * 0.40f, s * 0.06f, s * 0.12f, -s * 0.40f, s * 0.40f);
            for (int dish = 0; dish < 4; dish++)
            {
                float a = MathF.PI * 0.5f * dish;
                float dx = MathF.Cos(a) * s * 0.54f;
                float dz = MathF.Sin(a) * s * 0.54f;
                AddSurfaceBoxMat(w, solar,
                    dx - s * 0.06f, dx + s * 0.06f, s * 0.08f, s * 0.12f, dz - s * 0.06f, dz + s * 0.06f);
            }
        }
        else if (type is "shipyard_large")
        {
            float spanX = s * 0.92f;
            float padR = s * 1.28f;
            AddVasudanStationPadFlushTiers(w, s, padR, 4, profile);
            AddSurfaceBoxMat(w, hull,
                -spanX, spanX, s * 0.56f, s * 0.60f, -padR * 0.74f, padR * 0.74f);
            for (int g = -1; g <= 1; g++)
            {
                float x = g * s * 0.38f;
                AddSurfaceBoxMat(w, solar,
                    x - s * 0.04f, x + s * 0.20f, s * 0.48f, s * 0.52f, -padR * 0.66f, -padR * 0.56f);
            }
            for (int bay = 0; bay < 5; bay++)
            {
                float z = -s * 0.26f + bay * s * 0.18f;
                AddSurfaceBoxMat(w, panel,
                    -s * 0.20f, s * 0.20f, s * 0.08f, s * 0.12f, z - s * 0.06f, z + s * 0.06f);
            }
        }
        else if (type is "defense_turret")
        {
            float ringR = s * 0.22f;
            AddSurfaceBoxMat(w, hull,
                -ringR, ringR, s * 0.16f, s * 0.22f, -ringR, ringR);
            AddSurfaceBoxMat(w, panel,
                -s * 0.04f, s * 0.04f, s * 0.22f, s * 0.26f, -s * 0.04f, s * 0.04f);
            for (int barrel = 0; barrel < 4; barrel++)
            {
                float angle = MathF.PI * 0.5f * barrel;
                float bx = MathF.Cos(angle) * s * 0.34f;
                float bz = MathF.Sin(angle) * s * 0.34f;
                AddSurfaceBoxMat(w, solar,
                    bx - s * 0.03f, bx + s * 0.03f, s * 0.16f, s * 0.20f, bz + s * 0.04f, bz + s * 0.12f);
            }
        }
        else if (type is "sensor_array")
        {
            float padR = s * 1.14f;
            AddVasudanStationPadFlushTiers(w, s, padR, 3, profile);
            AddSurfaceBoxMat(w, hull,
                -s * 0.16f, s * 0.16f, s * 0.20f, s * 0.24f, -s * 0.16f, s * 0.16f);
            for (int dish = 0; dish < 5; dish++)
            {
                float a = dish * 1.1f;
                float dx = MathF.Cos(a) * s * (0.48f + dish * 0.04f);
                float dz = MathF.Sin(a) * s * (0.48f + dish * 0.04f);
                AddSurfaceBoxMat(w, solar,
                    dx - s * 0.06f, dx + s * 0.06f, s * 0.08f, s * 0.12f, dz - s * 0.06f, dz + s * 0.06f);
            }
            for (int band = 0; band < 5; band++)
            {
                float y = s * (0.56f + band * 0.08f);
                float halfW = s * (0.04f + band * 0.02f);
                AddSurfaceBoxMat(w, solar,
                    -halfW, halfW, y, y + s * 0.04f, s * 0.04f, s * 0.10f);
            }
        }
        else if (type is "power_reactor")
        {
            float padR = s * 1.12f;
            AddVasudanStationPadFlushTiers(w, s, padR, 3, profile);
            AddSurfaceBoxMat(w, hull,
                -s * 0.12f, s * 0.12f, s * 0.26f, s * 0.32f, -s * 0.12f, s * 0.12f);
            AddSurfaceBoxMat(w, engine,
                -s * 0.08f, s * 0.08f, s * 0.26f, s * 0.34f, -s * 0.08f, s * 0.08f);
            AddSurfaceBoxMat(w, engine,
                -s * 0.12f, s * 0.12f, s * 0.28f, s * 0.30f, -s * 0.12f, s * 0.12f);
            for (int glow = 0; glow < 3; glow++)
            {
                float t = glow / 2f;
                float halfW = s * MathHelper.Lerp(0.14f, 0.06f, t);
                float y0 = s * (0.27f + t * 0.02f);
                AddSurfaceBoxMat(w, engine, -halfW, halfW, y0, y0 + s * 0.03f, -halfW, halfW);
            }
            for (int ring = 0; ring < 2; ring++)
            {
                float y = s * (0.10f + ring * 0.04f);
                AddSurfaceBoxMat(w, solar, -padR * 0.70f, padR * 0.70f, y, y + s * 0.03f, -padR * 0.70f, padR * 0.70f);
            }
        }
        else if (type is "shipyard_small")
        {
            float spanX = s * 0.72f;
            float padR = s * 1.08f;
            AddVasudanStationPadFlushTiers(w, s, padR, 3, profile);
            AddSurfaceBoxMat(w, hull,
                -spanX, spanX, s * 0.46f, s * 0.50f, -padR * 0.70f, padR * 0.70f);
            for (int g = -1; g <= 1; g += 2)
            {
                float x = g * s * 0.38f;
                AddSurfaceBoxMat(w, solar,
                    x - s * 0.03f, x + s * 0.14f, s * 0.40f, s * 0.44f, s * 0.22f, s * 0.40f);
            }
        }
        else if (type is "shipyard_medium")
        {
            float spanX = s * 0.82f;
            float padR = s * 1.14f;
            AddVasudanStationPadFlushTiers(w, s, padR, 3, profile);
            AddSurfaceBoxMat(w, hull,
                -spanX, spanX, s * 0.52f, s * 0.56f, -padR * 0.72f, padR * 0.72f);
            for (int g = -1; g <= 1; g++)
            {
                float x = g * s * 0.34f;
                AddSurfaceBoxMat(w, solar,
                    x - s * 0.04f, x + s * 0.16f, s * 0.44f, s * 0.48f, -padR * 0.64f, -padR * 0.56f);
            }
        }
        else if (type is "resource_refinery")
        {
            float padR = s * 1.16f;
            AddVasudanStationPadFlushTiers(w, s, padR, 3, profile);
            AddSurfaceBoxMat(w, hull,
                -s * 0.14f, s * 0.14f, s * 0.08f, s * 0.14f, -s * 0.10f, s * 0.10f);
            ReadOnlySpan<float> stackX = stackalloc float[] { -0.30f, 0.24f, -0.08f, 0.32f };
            ReadOnlySpan<float> stackZ = stackalloc float[] { 0.20f, -0.14f, 0.32f, -0.26f };
            for (int t = 0; t < 4; t++)
            {
                float x = stackX[t] * s;
                float z = stackZ[t] * s;
                AddSurfaceBoxMat(w, panel,
                    x - s * 0.10f, x + s * 0.10f, s * 0.22f, s * 0.26f, z - s * 0.08f, z + s * 0.08f);
            }
        }
        else if (type is "supply_depot")
        {
            float padR = s * 1.16f;
            AddVasudanStationPadFlushTiers(w, s, padR, 3, profile);
            ReadOnlySpan<float> contX = stackalloc float[] { -0.30f, 0.30f, -0.30f, 0.30f, -0.10f, 0.10f };
            ReadOnlySpan<float> contZ = stackalloc float[] { -0.22f, -0.22f, 0.22f, 0.22f, 0.02f, 0.02f };
            for (int c = 0; c < 6; c++)
            {
                float x = contX[c] * s;
                float z = contZ[c] * s;
                AddSurfaceBoxMat(w, hull,
                    x - s * 0.08f, x - s * 0.04f, s * 0.14f, s * 0.20f, z - s * 0.08f, z + s * 0.08f);
            }
        }
        else if (type is "repair_bay")
        {
            AddSurfaceBoxMat(w, hull,
                -s * 0.30f, s * 0.30f, s * 0.24f, s * 0.30f, -s * 0.06f, s * 0.14f);
        }
    }

    private static void ApplyRetroStationDetail(RaceMeshWriter w, RaceVisualDefinition race,
        string type, float s, Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        var profile = RaceSubstrateProfile.ForRace(race);
        bool polishOnly = type is "command_center" or "repair_bay" or "shipyard_large"
            or "shipyard_small" or "shipyard_medium" or "resource_refinery" or "supply_depot"
            or "defense_turret" or "sensor_array" or "power_reactor";
        bool retroFlushStation = type is "repair_bay" or "sensor_array";
        if (retroFlushStation)
        {
            float padR = type is "repair_bay" ? s * 1.18f : s * 1.12f;
            AddTerranStationPadFlushTiers(w, s, padR, 4, profile);
        }
        else
        {
            ApplyTerranStationSubstrate(w, type, s, profile);
            AddTerranStationSurfaceOverlay(w, type, s, profile);
        }
        AddTerranStationGameplayComponentBands(w, type, s);

        if (polishOnly)
        {
            var frame = RaceMeshWriter.HullMaterial.Truss;
            if (type is "command_center" or "repair_bay" or "shipyard_large"
                or "shipyard_small" or "shipyard_medium")
            {
                float sideReach = type is "shipyard_small" ? 0.62f
                    : type is "shipyard_medium" ? 0.68f
                    : 0.72f;
                float sideTop = type is "repair_bay" ? 0.30f
                    : type is "shipyard_small" ? 0.28f
                    : type is "shipyard_medium" ? 0.32f
                    : 0.34f;
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = side * s * sideReach;
                    AddSurfaceBoxMat(w, frame,
                        x - side * s * 0.03f, x + side * s * 0.03f,
                        s * 0.10f, s * sideTop, -s * 0.44f, -s * 0.36f);
                    if (type is "repair_bay")
                    {
                        AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Solar,
                            x - side * s * 0.04f, x + side * s * 0.04f,
                            s * 0.24f, s * 0.30f, -s * 0.42f, -s * 0.32f);
                    }
                }
                if (type is "repair_bay")
                {
                    AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Solar,
                        -s * 0.10f, s * 0.10f, s * 0.24f, s * 0.30f, s * 0.10f, s * 0.20f);
                }
            }
            if (type is "command_center")
            {
                AddTerranStationPadFlushTiers(w, s, s * 1.20f, 4, profile);
                AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Hull,
                    -s * 0.28f, s * 0.28f, s * 0.10f, s * 0.18f, -s * 0.28f, s * 0.28f);
                AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Truss,
                    -s * 0.40f, s * 0.40f, s * 0.06f, s * 0.12f, -s * 0.40f, s * 0.40f);
                for (int dish = 0; dish < 4; dish++)
                {
                    float a = MathF.PI * 0.5f * dish;
                    float dx = MathF.Cos(a) * s * 0.54f;
                    float dz = MathF.Sin(a) * s * 0.54f;
                    AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Solar,
                        dx - s * 0.06f, dx + s * 0.06f, s * 0.08f, s * 0.12f, dz - s * 0.06f, dz + s * 0.06f);
                }
            }
            else if (type is "shipyard_large")
            {
                float spanX = s * 0.92f;
                float padR = s * 1.28f;
                AddTerranStationPadFlushTiers(w, s, padR, 4, profile);
                AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Truss,
                    -spanX, spanX, s * 0.56f, s * 0.60f, -padR * 0.74f, padR * 0.74f);
                for (int g = -1; g <= 1; g++)
                {
                    float x = g * s * 0.38f;
                    AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Solar,
                        x - s * 0.04f, x + s * 0.20f, s * 0.48f, s * 0.52f, -padR * 0.66f, -padR * 0.56f);
                }
                for (int bay = 0; bay < 5; bay++)
                {
                    float z = -s * 0.26f + bay * s * 0.18f;
                    AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Hull,
                        -s * 0.20f, s * 0.20f, s * 0.08f, s * 0.12f, z - s * 0.06f, z + s * 0.06f);
                }
            }
            else if (type is "defense_turret")
            {
                float ringR = s * 0.22f;
                AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Hull,
                    -ringR, ringR, s * 0.16f, s * 0.22f, -ringR, ringR);
                AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Truss,
                    -s * 0.04f, s * 0.04f, s * 0.22f, s * 0.26f, -s * 0.04f, s * 0.04f);
                for (int barrel = 0; barrel < 4; barrel++)
                {
                    float angle = MathF.PI * 0.5f * barrel;
                    float bx = MathF.Cos(angle) * s * 0.34f;
                    float bz = MathF.Sin(angle) * s * 0.34f;
                    AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Truss,
                        bx - s * 0.03f, bx + s * 0.03f, s * 0.16f, s * 0.20f, bz + s * 0.04f, bz + s * 0.12f);
                }
            }
            else if (type is "sensor_array")
            {
                float padR = s * 1.12f;
                AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Truss,
                    -s * 0.12f, s * 0.12f, s * 0.08f, s * 0.11f, -s * 0.12f, s * 0.12f);
                AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Hull,
                    -s * 0.08f, s * 0.08f, s * 0.11f, s * 0.14f, -s * 0.08f, s * 0.08f);
                for (int dish = 0; dish < 8; dish++)
                {
                    float a = MathF.PI * 2f * dish / 8f + MathF.PI * 0.25f;
                    float rad = dish % 2 == 0 ? s * 0.58f : s * 0.44f;
                    float dx = MathF.Cos(a) * rad;
                    float dz = MathF.Sin(a) * rad;
                    AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Solar,
                        dx - s * 0.07f, dx + s * 0.07f, s * 0.08f, s * 0.11f, dz - s * 0.07f, dz + s * 0.07f);
                }
                for (int band = 0; band < 4; band++)
                {
                    float y = s * (0.06f + band * 0.03f);
                    AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Truss,
                        -padR * 0.78f, padR * 0.78f, y, y + s * 0.025f, -padR * 0.78f, padR * 0.78f);
                }
            }
            else if (type is "power_reactor")
            {
                float padR = s * 1.12f;
                AddTerranStationPadFlushTiers(w, s, padR, 3, profile);
                AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Hull,
                    -s * 0.12f, s * 0.12f, s * 0.26f, s * 0.32f, -s * 0.12f, s * 0.12f);
                AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Engine,
                    -s * 0.08f, s * 0.08f, s * 0.26f, s * 0.34f, -s * 0.08f, s * 0.08f);
                AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Engine,
                    -s * 0.12f, s * 0.12f, s * 0.28f, s * 0.30f, -s * 0.12f, s * 0.12f);
                for (int glow = 0; glow < 3; glow++)
                {
                    float t = glow / 2f;
                    float halfW = s * MathHelper.Lerp(0.14f, 0.06f, t);
                    float y0 = s * (0.27f + t * 0.02f);
                    AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Engine,
                        -halfW, halfW, y0, y0 + s * 0.03f, -halfW, halfW);
                }
                for (int ring = 0; ring < 2; ring++)
                {
                    float y = s * (0.10f + ring * 0.04f);
                    AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Truss,
                        -padR * 0.70f, padR * 0.70f, y, y + s * 0.03f, -padR * 0.70f, padR * 0.70f);
                }
                for (int c = 0; c < 4; c++)
                {
                    float angle = MathF.PI * 0.25f + MathF.PI * 0.5f * c;
                    float cx = MathF.Cos(angle) * s * 0.52f;
                    float cz = MathF.Sin(angle) * s * 0.52f;
                    AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Hull,
                        cx - s * 0.05f, cx + s * 0.05f, s * 0.06f, s * 0.14f, cz - s * 0.05f, cz + s * 0.05f);
                }
            }
            else if (type is "shipyard_small")
            {
                float spanX = s * 0.72f;
                float padR = s * 1.08f;
                AddTerranStationPadFlushTiers(w, s, padR, 3, profile);
                AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Truss,
                    -spanX, spanX, s * 0.46f, s * 0.50f, -padR * 0.70f, padR * 0.70f);
                for (int g = -1; g <= 1; g += 2)
                {
                    float x = g * s * 0.38f;
                    AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Solar,
                        x - s * 0.03f, x + s * 0.14f, s * 0.40f, s * 0.44f, s * 0.22f, s * 0.40f);
                }
                for (int bay = 0; bay < 2; bay++)
                {
                    float z = -s * 0.18f + bay * s * 0.22f;
                    AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Hull,
                        -s * 0.16f, s * 0.16f, s * 0.08f, s * 0.12f, z - s * 0.06f, z + s * 0.06f);
                }
            }
            else if (type is "shipyard_medium")
            {
                float spanX = s * 0.82f;
                float padR = s * 1.14f;
                AddTerranStationPadFlushTiers(w, s, padR, 3, profile);
                AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Truss,
                    -spanX, spanX, s * 0.52f, s * 0.56f, -padR * 0.72f, padR * 0.72f);
                for (int g = -1; g <= 1; g++)
                {
                    float x = g * s * 0.34f;
                    AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Solar,
                        x - s * 0.04f, x + s * 0.16f, s * 0.44f, s * 0.48f, -padR * 0.64f, -padR * 0.56f);
                }
                for (int bay = 0; bay < 3; bay++)
                {
                    float z = -s * 0.22f + bay * s * 0.22f;
                    AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Hull,
                        -s * 0.18f, s * 0.18f, s * 0.08f, s * 0.12f, z - s * 0.06f, z + s * 0.06f);
                }
            }
            else if (type is "resource_refinery")
            {
                float padR = s * 1.16f;
                AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Hull,
                    -s * 0.14f, s * 0.14f, s * 0.08f, s * 0.14f, -s * 0.10f, s * 0.10f);
                ReadOnlySpan<float> stackX = stackalloc float[] { -0.30f, 0.24f, -0.08f, 0.32f };
                ReadOnlySpan<float> stackZ = stackalloc float[] { 0.20f, -0.14f, 0.32f, -0.26f };
                for (int t = 0; t < 4; t++)
                {
                    float x = stackX[t] * s;
                    float z = stackZ[t] * s;
                    AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Radiator,
                        x - s * 0.10f, x + s * 0.10f, s * 0.22f, s * 0.26f, z - s * 0.08f, z + s * 0.08f);
                }
                for (int i = 0; i < 8; i++)
                {
                    float a = MathF.PI * 2f * i / 8f;
                    float cx = MathF.Cos(a) * padR * 0.84f;
                    float cz = MathF.Sin(a) * padR * 0.84f;
                    AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Truss,
                        cx - s * 0.03f, cx + s * 0.03f, s * 0.04f, s * 0.08f, cz - s * 0.03f, cz + s * 0.03f);
                }
            }
            else if (type is "supply_depot")
            {
                float padR = s * 1.16f;
                ReadOnlySpan<float> contX = stackalloc float[] { -0.30f, 0.30f, -0.30f, 0.30f, -0.10f, 0.10f };
                ReadOnlySpan<float> contZ = stackalloc float[] { -0.22f, -0.22f, 0.22f, 0.22f, 0.02f, 0.02f };
                for (int c = 0; c < 6; c++)
                {
                    float x = contX[c] * s;
                    float z = contZ[c] * s;
                    AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Hull,
                        x - s * 0.08f, x - s * 0.04f, s * 0.14f, s * 0.20f, z - s * 0.08f, z + s * 0.08f);
                }
                for (int edge = 0; edge < 4; edge++)
                {
                    float a = MathF.PI * 0.5f * edge;
                    float ex = MathF.Cos(a) * padR * 0.88f;
                    float ez = MathF.Sin(a) * padR * 0.88f;
                    AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Truss,
                        ex - s * 0.04f, ex + s * 0.04f, s * 0.08f, s * 0.12f, ez - s * 0.04f, ez + s * 0.04f);
                }
            }
            else if (type is "repair_bay")
            {
                float padR = s * 1.18f;
                var truss = RaceMeshWriter.HullMaterial.Truss;
                var solar = RaceMeshWriter.HullMaterial.Solar;
                var panel = RaceMeshWriter.HullMaterial.Radiator;
                AddSurfaceBoxMat(w, truss,
                    -s * 0.34f, s * 0.34f, s * 0.12f, s * 0.16f, -s * 0.22f, s * 0.14f);
                for (int slot = 0; slot < 3; slot++)
                {
                    float x = -s * 0.22f + slot * s * 0.22f;
                    AddSurfaceBoxMat(w, truss,
                        x - s * 0.08f, x + s * 0.08f, s * 0.10f, s * 0.14f, -s * 0.26f, -s * 0.14f);
                }
                AddSurfaceBoxMat(w, truss,
                    -s * 0.30f, s * 0.30f, s * 0.20f, s * 0.24f, -s * 0.06f, s * 0.06f);
                for (int side = -1; side <= 1; side += 2)
                {
                    float reach = side < 0 ? 0.44f : 0.40f;
                    float x = side * s * reach;
                    AddSurfaceBoxMat(w, solar,
                        x - side * s * 0.02f, x + side * s * 0.14f, s * 0.18f, s * 0.22f, s * 0.08f, s * 0.12f);
                }
                for (int ring = 0; ring < 2; ring++)
                {
                    float y = s * (0.06f + ring * 0.03f);
                    float halfSpan = padR * (0.72f - ring * 0.06f);
                    AddSurfaceBoxMat(w, panel,
                        -halfSpan, halfSpan, y, y + s * 0.03f, -halfSpan, halfSpan);
                }
            }
            else
            {
                AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Hull,
                    -s * 0.30f, s * 0.30f, s * 0.48f, s * 0.56f, -s * 0.06f, s * 0.14f);
            }
            return;
        }

        int basePanelTiers = type switch
        {
            "repair_bay" => 6,
            "sensor_array" => 5,
            _ => 4,
        };
        int panelTiers = SubstrateScaledTiers(basePanelTiers, profile);
        float bandDepth = StationBandDepth(s, profile, 0.06f);
        float bandLen = s * (0.04f + profile.Grit * 0.02f);
        float baseZStep = type is "repair_bay" ? 0.18f : type is "sensor_array" ? 0.20f : 0.22f;
        for (int p = 0; p < panelTiers; p++)
        {
            float z0 = -s * 0.35f + p * StationBandStep(s, profile, s * baseZStep);
            float z1 = z0 + bandLen;
            float halfW = s * MathHelper.Lerp(0.78f, 0.48f, p / MathF.Max(1f, panelTiers - 1));
            float y0 = s * 0.04f;
            float y1 = y0 + bandDepth;
            var mat = (p % 3) switch
            {
                0 => RaceMeshWriter.HullMaterial.Radiator,
                1 => RaceMeshWriter.HullMaterial.Truss,
                _ => RaceMeshWriter.HullMaterial.Hull,
            };
            AddSurfaceBoxMat(w, mat, -halfW, halfW, y0, y1, z0, z1);
        }
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * s * 0.78f;
            w.TriColored(new Vector3(x, s * 0.10f, -s * 0.42f), new Vector3(x, s * 0.32f, -s * 0.48f), new Vector3(x - side * s * 0.04f, s * 0.22f, -s * 0.52f), primary * 0.97f);
            if (type is "repair_bay")
            {
                w.TriColored(new Vector3(x, s * 0.34f, -s * 0.44f), new Vector3(x - side * s * 0.06f, s * 0.46f, -s * 0.40f),
                    new Vector3(x, s * 0.42f, -s * 0.36f), accent * (side < 0 ? 1.08f : 1.02f));
            }
            else if (type is "sensor_array")
            {
                w.TriColored(new Vector3(x, s * 0.36f, -s * 0.08f), new Vector3(x - side * s * 0.05f, s * 0.52f, -s * 0.04f),
                    new Vector3(x, s * 0.48f, s * 0.02f), secondary * 0.90f);
            }
        }
        w.TriColored(new Vector3(-s * 0.10f, s * 0.38f, s * 0.40f), new Vector3(s * 0.10f, s * 0.38f, s * 0.40f), new Vector3(0, s * 0.48f, s * 0.48f), accent);

        if (type is "repair_bay")
        {
            for (int crest = 0; crest < 6; crest++)
            {
                float t = crest / 5f;
                float y = s * (0.18f + t * 0.26f);
                float halfW = s * (0.14f + t * 0.10f);
                float z = s * (0.08f + t * 0.14f);
                w.TriColored(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + s * 0.03f, z + s * 0.03f),
                    crest % 2 == 0 ? accent * 1.04f : secondary * 0.92f);
            }
            for (int lobe = 0; lobe < 10; lobe++)
            {
                float a = MathF.PI * 2f * lobe / 10f;
                float x = MathF.Cos(a) * s * 0.98f;
                float z = MathF.Sin(a) * s * 0.98f;
                w.TriColored(
                    new Vector3(x, s * 0.06f, z),
                    new Vector3(x + s * 0.04f, s * 0.12f, z + s * 0.02f),
                    new Vector3(x - s * 0.02f, s * 0.10f, z + s * 0.03f),
                    lobe % 2 == 0 ? primary * 0.97f : secondary);
            }
        }
        else if (type is "sensor_array")
        {
            // Loop-03: widen dish lateral read, trim crest Y for scale envelope.
            for (int crest = 0; crest < 6; crest++)
            {
                float t = crest / 5f;
                float y = s * (0.28f + t * 0.50f);
                float halfW = s * (0.08f + t * 0.05f);
                float z = s * (0.02f + t * 0.06f);
                w.TriColored(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + s * 0.03f, z + s * 0.02f),
                    crest % 2 == 0 ? primary * 0.98f : secondary * 0.90f);
            }

            for (int spine = 0; spine < 7; spine++)
            {
                float ang = MathF.PI * 2f * spine / 7f;
                var root = new Vector3(MathF.Cos(ang) * s * 0.20f, s * 0.12f, MathF.Sin(ang) * s * 0.20f);
                var mid = new Vector3(MathF.Cos(ang) * s * 0.14f, s * 0.52f, MathF.Sin(ang) * s * 0.14f);
                var tip = new Vector3(MathF.Cos(ang) * s * 0.08f, s * 0.88f, MathF.Sin(ang) * s * 0.08f);
                w.TriColored(root, tip, mid, spine % 2 == 0 ? secondary * 0.92f : primary * 0.96f);
                w.TriColored(mid, tip, root + new Vector3(0, s * 0.03f, 0), accent * 0.96f);
            }

            for (int ring = 0; ring < 7; ring++)
            {
                float t = ring / 6f;
                float r = s * (0.92f - t * 0.30f);
                float y = s * (0.04f + t * 0.05f);
                AddStationRingBand(w, r, y, primary * (0.94f - t * 0.04f), secondary * (0.90f - t * 0.03f), 10);
            }

            for (int lobe = 0; lobe < 10; lobe++)
            {
                float a = MathF.PI * 2f * lobe / 10f;
                float x = MathF.Cos(a) * s * 1.02f;
                float z = MathF.Sin(a) * s * 1.02f;
                w.TriColored(
                    new Vector3(x, s * 0.05f, z),
                    new Vector3(x + s * 0.04f, s * 0.11f, z + s * 0.02f),
                    new Vector3(x - s * 0.02f, s * 0.09f, z + s * 0.03f),
                    lobe % 2 == 0 ? primary * 0.97f : secondary);
            }

            for (int dish = -1; dish <= 1; dish += 2)
            {
                float x = dish * s * 0.62f;
                for (int rim = 0; rim < 3; rim++)
                {
                    float z = s * (-0.06f + rim * 0.06f);
                    w.TriColored(
                        new Vector3(x - s * 0.05f, s * 0.74f, z),
                        new Vector3(x + s * 0.05f, s * 0.74f, z),
                        new Vector3(x, s * 0.78f, z + s * 0.03f),
                        accent * (1.02f + rim * 0.04f));
                }
            }
        }
    }

    private static void ApplyTrussStationDetail(RaceMeshWriter w, RaceVisualDefinition race,
        string type, float s, Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        var profile = RaceSubstrateProfile.ForRace(race);
        bool polishOnly = type is "command_center" or "repair_bay" or "shipyard_large"
            or "shipyard_small" or "shipyard_medium" or "resource_refinery" or "supply_depot"
            or "defense_turret" or "sensor_array" or "power_reactor";
        AddTrussStationSubstrate(w, type, s, profile);
        AddTrussStationGameplayComponentBands(w, type, s);

        if (!polishOnly)
            return;

        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var frame = RaceMeshWriter.HullMaterial.Truss;
        var solar = RaceMeshWriter.HullMaterial.Solar;
        var engine = RaceMeshWriter.HullMaterial.Engine;

        AddTrussBooms(w, s, secondary);
        AddSolarArrayWings(w, s, accent);

        if (type is "command_center" or "repair_bay" or "shipyard_large"
            or "shipyard_small" or "shipyard_medium")
        {
            float sideReach = type is "shipyard_small" ? 0.62f
                : type is "shipyard_medium" ? 0.68f
                : 0.72f;
            float sideTop = type is "repair_bay" ? 0.30f
                : type is "shipyard_small" ? 0.28f
                : type is "shipyard_medium" ? 0.32f
                : 0.34f;
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * s * sideReach;
                AddSurfaceBoxMat(w, frame,
                    x - side * s * 0.03f, x + side * s * 0.03f,
                    s * 0.10f, s * sideTop, -s * 0.44f, -s * 0.36f);
                if (type is "repair_bay")
                {
                    AddSurfaceBoxMat(w, solar,
                        x - side * s * 0.04f, x + side * s * 0.04f,
                        s * 0.24f, s * 0.30f, -s * 0.42f, -s * 0.32f);
                }
            }
            if (type is "repair_bay")
            {
                AddSurfaceBoxMat(w, solar,
                    -s * 0.10f, s * 0.10f, s * 0.24f, s * 0.30f, s * 0.10f, s * 0.20f);
            }
        }

        if (type is "command_center")
        {
            AddTrussStationPadFlushTiers(w, s, s * 1.20f, 4, profile);
            AddSurfaceBoxMat(w, panel,
                -s * 0.28f, s * 0.28f, s * 0.10f, s * 0.18f, -s * 0.28f, s * 0.28f);
            AddSurfaceBoxMat(w, frame,
                -s * 0.40f, s * 0.40f, s * 0.06f, s * 0.12f, -s * 0.40f, s * 0.40f);
            for (int dish = 0; dish < 4; dish++)
            {
                float a = MathF.PI * 0.5f * dish;
                float dx = MathF.Cos(a) * s * 0.54f;
                float dz = MathF.Sin(a) * s * 0.54f;
                AddSurfaceBoxMat(w, solar,
                    dx - s * 0.06f, dx + s * 0.06f, s * 0.08f, s * 0.12f, dz - s * 0.06f, dz + s * 0.06f);
            }
        }
        else if (type is "shipyard_large")
        {
            float spanX = s * 0.92f;
            float padR = s * 1.28f;
            AddTrussStationPadFlushTiers(w, s, padR, 4, profile);
            AddSurfaceBoxMat(w, frame,
                -spanX, spanX, s * 0.56f, s * 0.60f, -padR * 0.74f, padR * 0.74f);
            for (int g = -1; g <= 1; g++)
            {
                float x = g * s * 0.38f;
                AddSurfaceBoxMat(w, solar,
                    x - s * 0.04f, x + s * 0.20f, s * 0.48f, s * 0.52f, -padR * 0.66f, -padR * 0.56f);
            }
            for (int bay = 0; bay < 5; bay++)
            {
                float z = -s * 0.26f + bay * s * 0.18f;
                AddSurfaceBoxMat(w, panel,
                    -s * 0.20f, s * 0.20f, s * 0.08f, s * 0.12f, z - s * 0.06f, z + s * 0.06f);
            }
            AddSurfaceBoxMat(w, solar,
                -s * 0.45f, s * 0.45f, s * 0.42f, s * 0.55f, -s * 0.62f, -s * 0.52f);
        }
        else if (type is "defense_turret")
        {
            float ringR = s * 0.22f;
            AddSurfaceBoxMat(w, panel,
                -ringR, ringR, s * 0.16f, s * 0.22f, -ringR, ringR);
            AddSurfaceBoxMat(w, frame,
                -s * 0.04f, s * 0.04f, s * 0.22f, s * 0.26f, -s * 0.04f, s * 0.04f);
            for (int barrel = 0; barrel < 4; barrel++)
            {
                float angle = MathF.PI * 0.5f * barrel;
                float bx = MathF.Cos(angle) * s * 0.34f;
                float bz = MathF.Sin(angle) * s * 0.34f;
                AddSurfaceBoxMat(w, solar,
                    bx - s * 0.03f, bx + s * 0.03f, s * 0.16f, s * 0.20f, bz + s * 0.04f, bz + s * 0.12f);
            }
        }
        else if (type is "sensor_array")
        {
            float padR = s * 1.14f;
            AddTrussStationPadFlushTiers(w, s, padR, 3, profile);
            AddSurfaceBoxMat(w, frame,
                -s * 0.16f, s * 0.16f, s * 0.20f, s * 0.24f, -s * 0.16f, s * 0.16f);
            for (int dish = 0; dish < 5; dish++)
            {
                float a = dish * 1.1f;
                float dx = MathF.Cos(a) * s * (0.48f + dish * 0.04f);
                float dz = MathF.Sin(a) * s * (0.48f + dish * 0.04f);
                AddSurfaceBoxMat(w, solar,
                    dx - s * 0.06f, dx + s * 0.06f, s * 0.08f, s * 0.12f, dz - s * 0.06f, dz + s * 0.06f);
            }
            for (int band = 0; band < 3; band++)
            {
                float y = s * (0.08f + band * 0.04f);
                AddSurfaceBoxMat(w, frame,
                    -padR * 0.72f, padR * 0.72f, y, y + s * 0.03f, -padR * 0.72f, padR * 0.72f);
            }
        }
        else if (type is "power_reactor")
        {
            float padR = s * 1.12f;
            AddTrussStationPadFlushTiers(w, s, padR, 3, profile);
            AddSurfaceBoxMat(w, panel,
                -s * 0.12f, s * 0.12f, s * 0.26f, s * 0.32f, -s * 0.12f, s * 0.12f);
            AddSurfaceBoxMat(w, engine,
                -s * 0.08f, s * 0.08f, s * 0.26f, s * 0.34f, -s * 0.08f, s * 0.08f);
            AddSurfaceBoxMat(w, engine,
                -s * 0.12f, s * 0.12f, s * 0.28f, s * 0.30f, -s * 0.12f, s * 0.12f);
            for (int glow = 0; glow < 3; glow++)
            {
                float t = glow / 2f;
                float halfW = s * MathHelper.Lerp(0.14f, 0.06f, t);
                float y0 = s * (0.27f + t * 0.02f);
                AddSurfaceBoxMat(w, engine, -halfW, halfW, y0, y0 + s * 0.03f, -halfW, halfW);
            }
            for (int ring = 0; ring < 2; ring++)
            {
                float y = s * (0.10f + ring * 0.04f);
                AddSurfaceBoxMat(w, frame, -padR * 0.70f, padR * 0.70f, y, y + s * 0.03f, -padR * 0.70f, padR * 0.70f);
            }
        }
        else if (type is "shipyard_small")
        {
            float spanX = s * 0.72f;
            float padR = s * 1.08f;
            AddTrussStationPadFlushTiers(w, s, padR, 3, profile);
            AddSurfaceBoxMat(w, frame,
                -spanX, spanX, s * 0.46f, s * 0.50f, -padR * 0.70f, padR * 0.70f);
            for (int g = -1; g <= 1; g += 2)
            {
                float x = g * s * 0.38f;
                AddSurfaceBoxMat(w, solar,
                    x - s * 0.03f, x + s * 0.14f, s * 0.40f, s * 0.44f, s * 0.22f, s * 0.40f);
            }
            AddSurfaceBoxMat(w, solar,
                -s * 0.30f, s * 0.30f, s * 0.40f, s * 0.48f, -s * 0.50f, -s * 0.40f);
        }
        else if (type is "shipyard_medium")
        {
            float spanX = s * 0.82f;
            float padR = s * 1.14f;
            AddTrussStationPadFlushTiers(w, s, padR, 3, profile);
            AddSurfaceBoxMat(w, frame,
                -spanX, spanX, s * 0.52f, s * 0.56f, -padR * 0.72f, padR * 0.72f);
            for (int g = -1; g <= 1; g++)
            {
                float x = g * s * 0.34f;
                AddSurfaceBoxMat(w, solar,
                    x - s * 0.04f, x + s * 0.16f, s * 0.44f, s * 0.48f, -padR * 0.64f, -padR * 0.56f);
            }
            AddSurfaceBoxMat(w, solar,
                -s * 0.38f, s * 0.38f, s * 0.42f, s * 0.50f, -s * 0.58f, -s * 0.48f);
        }
        else if (type is "resource_refinery")
        {
            float padR = s * 1.16f;
            AddTrussStationPadFlushTiers(w, s, padR, 3, profile);
            AddSurfaceBoxMat(w, panel,
                -s * 0.14f, s * 0.14f, s * 0.08f, s * 0.14f, -s * 0.10f, s * 0.10f);
            ReadOnlySpan<float> stackX = stackalloc float[] { -0.30f, 0.24f, -0.08f, 0.32f };
            ReadOnlySpan<float> stackZ = stackalloc float[] { 0.20f, -0.14f, 0.32f, -0.26f };
            for (int t = 0; t < 4; t++)
            {
                float x = stackX[t] * s;
                float z = stackZ[t] * s;
                AddSurfaceBoxMat(w, frame,
                    x - s * 0.10f, x + s * 0.10f, s * 0.22f, s * 0.26f, z - s * 0.08f, z + s * 0.08f);
            }
        }
        else if (type is "supply_depot")
        {
            float padR = s * 1.16f;
            AddTrussStationPadFlushTiers(w, s, padR, 3, profile);
            ReadOnlySpan<float> contX = stackalloc float[] { -0.30f, 0.30f, -0.30f, 0.30f, -0.10f, 0.10f };
            ReadOnlySpan<float> contZ = stackalloc float[] { -0.22f, -0.22f, 0.22f, 0.22f, 0.02f, 0.02f };
            for (int c = 0; c < 6; c++)
            {
                float x = contX[c] * s;
                float z = contZ[c] * s;
                AddSurfaceBoxMat(w, panel,
                    x - s * 0.08f, x - s * 0.04f, s * 0.14f, s * 0.20f, z - s * 0.08f, z + s * 0.08f);
            }
        }
        else if (type is "repair_bay")
        {
            AddSurfaceBoxMat(w, panel,
                -s * 0.30f, s * 0.30f, s * 0.24f, s * 0.30f, -s * 0.06f, s * 0.14f);
        }

        for (int i = 0; i < 4; i++)
        {
            float z = (i - 1.5f) * s * 0.18f;
            AddSurfaceBoxMat(w, frame,
                -s * 0.82f, s * 0.82f, s * 0.14f, s * 0.32f, z, z + s * 0.08f);
        }
    }

    private static void AddOrganicStationVeinBands(
        RaceMeshWriter w, string type, float s, Vector3 accent, RaceSubstrateProfile profile, float bias)
    {
        var vein = RaceMeshWriter.HullMaterial.Solar;
        int rimSegments = SubstrateScaledTiers(10, profile);
        float padR = s * 0.88f;
        float rimY = s * 0.06f;
        float rimDepth = StationBandDepth(s, profile, 0.014f);
        for (int i = 0; i < rimSegments; i++)
        {
            float a = MathF.PI * 2f * i / rimSegments + bias * 0.02f;
            float x = MathF.Cos(a) * padR + bias;
            float z = MathF.Sin(a) * padR;
            float half = s * 0.025f;
            AddSurfaceBoxMat(w, vein, x - half, x + half, rimY, rimY + rimDepth, z - half * 0.6f, z + half * 0.6f);
        }

        int crownBands = SubstrateScaledTiers(type is "sensor_array" or "repair_bay" ? 6 : 5, profile);
        float crownDepth = StationBandDepth(s, profile, 0.016f);
        for (int b = 0; b < crownBands; b++)
        {
            float t = b / MathF.Max(1f, crownBands - 1);
            float y = s * (0.38f + t * 0.22f);
            float halfW = s * (0.10f + t * 0.06f);
            float z = s * (0.08f + t * 0.12f);
            AddSurfaceBoxMat(w, vein, -halfW + bias, halfW + bias, y, y + crownDepth, z, z + s * 0.04f);
        }
    }

    private static void AddOrganicStationCrownBands(
        RaceMeshWriter w, string type, float s, Vector3 primary, Vector3 secondary, float bias)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        int domeTiers = type is "command_center" ? 3 : type.Contains("shipyard") ? 2 : 2;
        for (int tier = 0; tier < domeTiers; tier++)
        {
            float t = tier / MathF.Max(1f, domeTiers - 1);
            float y = s * (0.28f + t * 0.18f);
            float halfW = s * (0.16f + t * 0.08f);
            float z = s * (0.14f + t * 0.10f);
            AddSurfaceBoxMat(w, tier == 0 ? hull : panel, -halfW + bias, halfW + bias, y, y + s * 0.04f, z, z + s * 0.06f);
        }

        for (int lobe = 0; lobe < 5; lobe++)
        {
            float a = MathF.PI * 2f * lobe / 5f + bias;
            float x = MathF.Cos(a) * s * 0.58f + bias;
            float z = MathF.Sin(a) * s * 0.58f;
            AddSurfaceBoxMat(w, panel, x - s * 0.03f, x + s * 0.03f, s * 0.10f, s * 0.22f, z, z + s * 0.05f);
        }
    }

    private static void AddOrganicStationEngineGlowWell(RaceMeshWriter w, float s, float bias)
    {
        var engine = RaceMeshWriter.HullMaterial.Engine;
        for (int tier = 0; tier < 4; tier++)
        {
            float t = tier / 3f;
            float y = s * (0.40f + t * 0.28f);
            float halfW = s * (0.12f - t * 0.04f);
            TriMat(w, engine,
                new Vector3(-halfW + bias, y, s * 0.10f),
                new Vector3(halfW + bias, y, s * 0.10f),
                new Vector3(bias, y + s * 0.08f, s * 0.18f));
        }
        for (int well = 0; well < 2; well++)
        {
            float x = (well - 0.5f) * s * 0.14f + bias;
            TriMat(w, engine,
                new Vector3(x - s * 0.05f, s * 0.36f, s * 0.06f),
                new Vector3(x + s * 0.05f, s * 0.36f, s * 0.06f),
                new Vector3(x, s * 0.48f, s * 0.14f));
        }
    }

    private static void AddOrganicCommandCenterSurfaceDetail(
        RaceMeshWriter w, float s, Vector3 accent, Vector3 secondary, Vector3 primary, float bias)
    {
        var vein = RaceMeshWriter.HullMaterial.Solar;
        for (int arm = 0; arm < 4; arm++)
        {
            float a = MathF.PI * 0.5f * arm + bias * 0.01f;
            float x = MathF.Cos(a) * s * 0.62f + bias;
            float z = MathF.Sin(a) * s * 0.62f;
            AddSurfaceBoxMat(w, vein, x - s * 0.03f, x + s * 0.03f, s * 0.12f, s * 0.24f, z, z + s * 0.05f);
        }
        AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Hull,
            -s * 0.14f + bias, s * 0.14f + bias, s * 0.30f, s * 0.42f, s * 0.12f, s * 0.22f);
    }

    private static void AddOrganicShipyardSurfaceDetail(
        RaceMeshWriter w, string type, float s, Vector3 accent, Vector3 secondary, Vector3 primary, float bias)
    {
        int bays = type is "shipyard_large" ? 3 : type is "shipyard_medium" ? 2 : 1;
        for (int bay = 0; bay < bays; bay++)
        {
            float z = -s * 0.18f + bay * s * 0.22f;
            AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Radiator,
                -s * 0.12f + bias, s * 0.12f + bias, s * 0.10f, s * 0.22f, z, z + s * 0.10f);
            AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Solar,
                -s * 0.06f + bias, s * 0.06f + bias, s * 0.22f, s * 0.30f, z + s * 0.02f, z + s * 0.08f);
        }
    }

    private static void ApplyOrganicStationDetail(RaceMeshWriter w, RaceVisualDefinition race,
        string type, float s, Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        var profile = RaceSubstrateProfile.ForRace(race);
        float bias = s * race.Modifiers.Asymmetry * 0.12f;
        AddOrganicStationSubstrate(w, type, s, profile);
        AddOrganicStationVeinBands(w, type, s, accent, profile, bias);
        AddOrganicStationCrownBands(w, type, s, primary, secondary, bias);

        if (type is "power_reactor" or "repair_bay")
            AddOrganicStationEngineGlowWell(w, s, bias);

        if (type is "command_center")
            AddOrganicCommandCenterSurfaceDetail(w, s, accent, secondary, primary, bias);
        if (type.Contains("shipyard") && type is not "shipyard_large")
            AddOrganicShipyardSurfaceDetail(w, type, s, accent, secondary, primary, bias);
        if (type is "sensor_array" or "repair_bay" or "supply_depot")
            AddOrganicStationLandmarkMassing(w, type, s, accent, secondary, primary, bias);
        if (type is "defense_turret")
            AddOrganicDefenseTurretSurfaceDetail(w, s, accent, secondary, primary, bias);
        if (type is "shipyard_large")
            AddOrganicShipyardLargeSurfaceDetail(w, s, accent, secondary, primary, bias);
        if (type is "resource_refinery")
            AddOrganicRefinerySurfaceDetail(w, s, accent, secondary, primary, bias);
    }

    private static void AddOrganicShipyardLargeSurfaceDetail(
        RaceMeshWriter w, float s, Vector3 accent, Vector3 secondary, Vector3 primary, float bias)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * s * 1.02f;
            for (int tier = 0; tier < 2; tier++)
            {
                float y0 = s * (0.22f + tier * 0.28f);
                float y1 = y0 + s * 0.12f;
                float z = -s * (0.44f - tier * 0.02f);
                float legReach = s * (0.20f + tier * 0.02f);
                w.TriColored(
                    new Vector3(x, y0, z), new Vector3(x - side * legReach, y0, z + s * 0.02f),
                    new Vector3(x - side * (legReach - s * 0.02f), y1, z + s * 0.04f), tier % 2 == 0 ? primary * 0.98f : secondary * 0.90f);
                w.TriColored(
                    new Vector3(x, y0, -z), new Vector3(x - side * (legReach - s * 0.02f), y1, -z - s * 0.04f),
                    new Vector3(x - side * legReach, y0, -z - s * 0.02f), accent * (0.96f + tier * 0.04f));
            }
        }

        for (int bay = 0; bay < 5; bay++)
        {
            float z = -s * 0.32f + bay * s * 0.26f;
            w.TriColored(
                new Vector3(-s * 0.10f + bias, s * 0.12f, z + s * 0.04f),
                new Vector3(s * 0.10f + bias, s * 0.12f, z + s * 0.04f),
                new Vector3(bias, s * 0.08f, z + s * 0.08f), accent * (1.02f + (bay % 2) * 0.04f));
        }
    }

    private static void AddOrganicRefinerySurfaceDetail(
        RaceMeshWriter w, float s, Vector3 accent, Vector3 secondary, Vector3 primary, float bias)
    {
        ReadOnlySpan<float> tankX = stackalloc float[] { -0.36f, 0.24f, -0.06f };
        ReadOnlySpan<float> tankZ = stackalloc float[] { 0.22f, -0.16f, 0.30f };
        ReadOnlySpan<float> tankH = stackalloc float[] { 0.50f, 0.58f, 0.44f };
        for (int t = 0; t < 3; t++)
        {
            float x = tankX[t] * s + bias;
            float z = tankZ[t] * s;
            float h = tankH[t] * s;
            for (int band = 0; band < 2; band++)
            {
                float y = s * 0.18f + h * (0.28f + band * 0.22f);
                float halfW = s * (0.15f + t * 0.015f);
                w.TriColored(
                    new Vector3(x - halfW, y, z + s * 0.08f),
                    new Vector3(x + halfW, y, z + s * 0.08f),
                    new Vector3(x, y + s * 0.03f, z + s * 0.12f),
                    band % 2 == 0 ? accent * (1.02f + band * 0.03f) : secondary * 0.90f);
            }
        }

        ReadOnlySpan<float> chimneyX = stackalloc float[] { 0.14f, -0.20f };
        ReadOnlySpan<float> chimneyPeak = stackalloc float[] { 0.82f, 0.68f };
        for (int c = 0; c < 2; c++)
        {
            float cx = chimneyX[c] * s + bias;
            float peak = chimneyPeak[c] * s;
            for (int band = 0; band < 2; band++)
            {
                float t = band;
                float y = s * (0.28f + t * (chimneyPeak[c] - 0.28f));
                float halfW = s * (0.10f - c * 0.01f) * (1f - t * 0.18f);
                w.TriColored(
                    new Vector3(cx - halfW, y, s * (0.06f + t * 0.06f)),
                    new Vector3(cx + halfW, y, s * (0.06f + t * 0.06f)),
                    new Vector3(cx, y + s * 0.04f, s * (0.10f + t * 0.04f)),
                    band % 2 == 0 ? accent * (1.04f + c * 0.04f) : secondary * 0.88f);
            }
            w.TriColored(
                new Vector3(cx - s * 0.04f, peak, s * 0.08f),
                new Vector3(cx + s * 0.04f, peak, s * 0.08f),
                new Vector3(cx, peak + s * 0.04f, s * 0.14f),
                accent * (1.14f + c * 0.04f));
        }

        for (int arm = 0; arm < 1; arm++)
        {
            float tx = tankX[arm] * s + bias;
            float tz = tankZ[arm] * s;
            float cx = chimneyX[0] * s + bias;
            float cy = s * 0.44f;
            w.TriColored(
                new Vector3(tx, s * tankH[arm] * 0.55f, tz + s * 0.04f),
                new Vector3(cx, cy, s * 0.10f),
                new Vector3((tx + cx) * 0.5f, cy + s * 0.03f, (tz + s * 0.10f) * 0.5f),
                accent * 0.94f);
        }

        for (int lobe = 0; lobe < 3; lobe++)
        {
            float a = MathF.PI * 2f * lobe / 3f + bias * 0.02f;
            float x = MathF.Cos(a) * s * 0.90f;
            float z = MathF.Sin(a) * s * 0.90f;
            w.TriColored(
                new Vector3(x, s * 0.06f, z),
                new Vector3(x + s * 0.05f, s * 0.14f, z + s * 0.04f),
                new Vector3(x - s * 0.02f, s * 0.10f, z + s * 0.05f),
                lobe % 2 == 0 ? primary * 0.97f : secondary);
        }
    }

    private static void AddOrganicDefenseTurretSurfaceDetail(
        RaceMeshWriter w, float s, Vector3 accent, Vector3 secondary, Vector3 primary, float bias)
    {
        float y = s * 0.12f;
        float outerR = s * 0.78f;
        float innerR = s * 0.62f;
        for (int f = 0; f < 6; f++)
        {
            float a0 = MathF.PI * 2f * f / 6f;
            float a1 = MathF.PI * 2f * (f + 1) / 6f;
            var o0 = new Vector3(MathF.Cos(a0) * outerR + bias, y, MathF.Sin(a0) * outerR);
            var o1 = new Vector3(MathF.Cos(a1) * outerR + bias, y, MathF.Sin(a1) * outerR);
            var i0 = new Vector3(MathF.Cos(a0) * innerR + bias, y + s * 0.02f, MathF.Sin(a0) * innerR);
            w.TriColored(o0, o1, i0, f % 2 == 0 ? secondary * 0.90f : primary * 0.96f);
        }

        for (int buttress = 0; buttress < 4; buttress++)
        {
            float angle = MathF.PI * 0.5f * buttress;
            var dir = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
            var tip = dir * s * 0.50f + new Vector3(bias, s * 0.84f, 0);
            var glow = tip + dir * s * 0.03f + new Vector3(0, s * 0.04f, 0);
            var flank = tip + new Vector3(-dir.Z * s * 0.04f, 0, dir.X * s * 0.04f);
            w.TriColored(tip, glow, flank, accent * (1.06f + buttress * 0.02f));
        }

        for (int band = 0; band < 2; band++)
        {
            float bandY = s * (0.50f + band * 0.12f);
            float halfW = s * (0.10f + band * 0.02f);
            w.TriColored(
                new Vector3(-halfW + bias, bandY, s * 0.06f),
                new Vector3(halfW + bias, bandY, s * 0.06f),
                new Vector3(bias, bandY + s * 0.03f, s * 0.10f),
                band % 2 == 0 ? accent * 1.04f : secondary * 0.90f);
        }
    }

    private static void AddOrganicStationLandmarkMassing(
        RaceMeshWriter w, string type, float s, Vector3 accent, Vector3 secondary, Vector3 primary, float bias)
    {
        float crownH = type switch
        {
            "sensor_array" => s * 1.02f,
            "repair_bay" => s * 1.00f,
            "supply_depot" => s * 0.96f,
            _ => s * 0.72f,
        };
        float crownR = s * 0.10f;
        for (int i = 0; i < 6; i++)
        {
            float a0 = MathF.PI * 2f * i / 6f;
            float a1 = MathF.PI * 2f * (i + 1) / 6f;
            var p0 = new Vector3(MathF.Cos(a0) * crownR + bias, s * 0.36f, MathF.Sin(a0) * crownR);
            var p1 = new Vector3(MathF.Cos(a1) * crownR + bias, s * 0.36f, MathF.Sin(a1) * crownR);
            var top = new Vector3(bias, crownH, s * 0.03f);
            w.TriColored(p0, p1, top, i % 2 == 0 ? accent : accent * 0.94f);
        }
        w.TriColored(
            new Vector3(-crownR * 0.5f + bias, crownH, s * 0.02f),
            new Vector3(crownR * 0.5f + bias, crownH, s * 0.02f),
            new Vector3(bias, crownH + s * 0.06f, s * 0.05f),
            accent * 1.12f);

        int padLobes = type is "repair_bay" ? 8 : 6;
        for (int l = 0; l < padLobes; l++)
        {
            float a = MathF.PI * 2f * l / padLobes + bias;
            float x = MathF.Cos(a) * s * 0.88f;
            float z = MathF.Sin(a) * s * 0.88f;
            w.TriColored(
                new Vector3(x, s * 0.05f, z),
                new Vector3(x + s * 0.06f, s * 0.12f, z + s * 0.04f),
                new Vector3(x - s * 0.03f, s * 0.10f, z + s * 0.05f),
                l % 2 == 0 ? primary * 0.97f : secondary);
            w.TriColored(
                new Vector3(x, s * 0.12f, z),
                new Vector3(x + s * 0.04f, s * 0.20f, z + s * 0.02f),
                new Vector3(x - s * 0.02f, s * 0.16f, z + s * 0.04f),
                accent * 0.96f);
        }

        int crestBands = type is "sensor_array" ? 6 : 5;
        for (int b = 0; b < crestBands; b++)
        {
            float t = b / MathF.Max(1f, crestBands - 1);
            float y = s * (0.22f + t * 0.38f);
            float halfW = s * (0.14f + t * 0.06f);
            float z = s * (0.10f + t * 0.16f);
            w.TriColored(
                new Vector3(-halfW + bias, y, z),
                new Vector3(halfW + bias, y, z),
                new Vector3(bias, y + s * 0.04f, z + s * 0.03f),
                b % 2 == 0 ? accent * 1.05f : secondary * 0.92f);
        }

        if (type is "sensor_array")
        {
            for (int dish = -1; dish <= 1; dish += 2)
            {
                float x = dish * s * 0.34f;
                w.TriColored(new Vector3(x, s * 0.28f, s * 0.22f), new Vector3(x + dish * s * 0.10f, s * 0.36f, s * 0.30f),
                    new Vector3(x, s * 0.42f, s * 0.26f), accent);
                w.TriColored(new Vector3(x, s * 0.28f, -s * 0.22f), new Vector3(x + dish * s * 0.10f, s * 0.36f, -s * 0.30f),
                    new Vector3(x, s * 0.42f, -s * 0.26f), accent * 0.95f);
            }
        }
        else if (type is "repair_bay")
        {
            for (int arm = -1; arm <= 1; arm += 2)
            {
                float x = arm * s * 0.46f;
                w.TriColored(new Vector3(x, s * 0.18f, s * 0.18f), new Vector3(x + arm * s * 0.12f, s * 0.30f, s * 0.28f),
                    new Vector3(x, s * 0.34f, s * 0.22f), accent * 0.98f);
                w.TriColored(new Vector3(x, s * 0.18f, -s * 0.18f), new Vector3(x + arm * s * 0.12f, s * 0.30f, -s * 0.28f),
                    new Vector3(x, s * 0.34f, -s * 0.22f), primary * 0.98f);
            }
        }
        else if (type is "supply_depot")
        {
            for (int crate = 0; crate < 6; crate++)
            {
                float x = (crate % 3 - 1) * s * 0.22f;
                float z = (crate / 3 - 0.5f) * s * 0.32f;
                w.TriColored(new Vector3(x, s * 0.08f, z), new Vector3(x + s * 0.08f, s * 0.08f, z),
                    new Vector3(x, s * 0.18f, z + s * 0.06f), crate % 2 == 0 ? secondary : primary * 0.97f);
            }
        }
    }

    private static void ApplyGenericStationDetail(RaceMeshWriter w, string type, float s, Vector3 secondary, Vector3 accent)
    {
        for (int i = 0; i < 6; i++)
        {
            float a = MathF.PI * 2f * i / 6f;
            float x = MathF.Cos(a) * s * 0.70f;
            float z = MathF.Sin(a) * s * 0.70f;
            w.TriColored(new Vector3(x, s * 0.06f, z), new Vector3(x + s * 0.03f, s * 0.14f, z), new Vector3(x, s * 0.14f, z + s * 0.03f), secondary * 0.88f);
        }
    }

    private static void AddAsymmetricStationDiagonalStripes(
        RaceMeshWriter w, string type, float s, float bias, Vector3 accent, RaceSubstrateProfile profile)
    {
        var stripe = RaceMeshWriter.HullMaterial.Solar;
        int extensions = type.Contains("shipyard") ? 3 : type is "supply_depot" or "resource_refinery" ? 2 : 4;
        float stripeDepth = StationBandDepth(s, profile, 0.012f);
        for (int ext = 0; ext < extensions; ext++)
        {
            float zBase = -s * 0.20f + ext * s * 0.22f;
            float x0 = bias + s * 0.18f;
            float x1 = x0 + s * 0.28f;
            for (int stripeIdx = 0; stripeIdx < 3; stripeIdx++)
            {
                float t = stripeIdx / 2f;
                float dx = t * s * 0.08f;
                float dz = t * s * 0.06f;
                AddSurfaceBoxMat(w, stripe, x0 + dx, x1 + dx, s * 0.10f, s * 0.10f + stripeDepth,
                    zBase + dz, zBase + s * 0.14f + dz);
            }
        }

        int padStripes = SubstrateScaledTiers(8, profile);
        for (int i = 0; i < padStripes; i++)
        {
            float a = MathF.PI * 2f * i / padStripes;
            float x = MathF.Cos(a) * s * 0.82f + bias;
            float z = MathF.Sin(a) * s * 0.82f;
            TriMat(w, stripe,
                new Vector3(x, s * 0.08f, z),
                new Vector3(x + s * 0.04f, s * 0.14f, z + s * 0.03f),
                new Vector3(x + s * 0.02f, s * 0.10f, z + s * 0.05f));
        }
    }

    private static void AddAsymmetricStationAccentWells(
        RaceMeshWriter w, string type, float s, float bias, Vector3 accent, RaceSubstrateProfile profile)
    {
        int crownWells = SubstrateScaledTiers(type is "command_center" ? 8 : 6, profile);
        for (int well = 0; well < crownWells; well++)
        {
            float t = well / MathF.Max(1f, crownWells - 1);
            float y = s * (0.36f + t * 0.42f);
            float halfW = s * (0.04f + t * 0.03f);
            float z = s * (0.06f + t * 0.14f);
            w.TriColored(
                new Vector3(-halfW + bias, y, z),
                new Vector3(halfW + bias * 1.08f, y, z),
                new Vector3(bias, y + s * 0.05f, z + s * 0.04f),
                accent * (1.06f + t * 0.08f));
        }

        for (int block = 0; block < 3; block++)
        {
            float x = bias + s * (0.22f + block * 0.10f);
            float y = s * (0.22f + block * 0.12f);
            float z = s * (0.10f + block * 0.08f);
            AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Solar,
                x, x + s * 0.12f, y, y + s * 0.08f, z, z + s * 0.06f);
        }
    }

    private static void AddAsymmetricStationGenericDetail(
        RaceMeshWriter w, string type, float s, float bias, Vector3 primary, Vector3 secondary, Vector3 accent,
        RaceSubstrateProfile profile)
    {
        int padLobes = type is "defense_turret" ? 6 : type is "supply_depot" ? 8 : 7;
        for (int lobe = 0; lobe < padLobes; lobe++)
        {
            float a = MathF.PI * 2f * lobe / padLobes;
            float x = MathF.Cos(a) * s * 0.90f + bias;
            float z = MathF.Sin(a) * s * 0.90f;
            AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Hull,
                x - s * 0.04f, x + s * 0.04f, s * 0.06f, s * 0.14f, z, z + s * 0.04f);
        }

        if (type.Contains("shipyard"))
        {
            int bays = type is "shipyard_large" ? 3 : type is "shipyard_medium" ? 2 : 1;
            for (int bay = 0; bay < bays; bay++)
            {
                float z = -s * 0.16f + bay * s * 0.20f;
                AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Truss,
                    bias + s * 0.08f, bias + s * 0.36f, s * 0.12f, s * 0.24f, z, z + s * 0.10f);
            }
        }
        else if (type is "resource_refinery")
        {
            for (int stack = 0; stack < 3; stack++)
            {
                float x = (stack - 1) * s * 0.22f + bias;
                AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Truss,
                    x - s * 0.06f, x + s * 0.06f, s * 0.20f, s * 0.48f, s * 0.04f, s * 0.12f);
            }
        }
        else if (type is "defense_turret")
        {
            AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Solar,
                bias - s * 0.08f, bias + s * 0.08f, s * 0.44f, s * 0.56f, s * 0.04f, s * 0.14f);
        }
        else if (type is "supply_depot")
        {
            for (int crate = 0; crate < 4; crate++)
            {
                float x = (crate % 2 - 0.5f) * s * 0.28f + bias;
                float z = (crate / 2 - 0.5f) * s * 0.24f;
                AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Radiator,
                    x - s * 0.06f, x + s * 0.06f, s * 0.08f, s * 0.18f, z, z + s * 0.08f);
            }
        }
    }

    private static void ApplyAsymmetricStationDetail(RaceMeshWriter w, RaceVisualDefinition race,
        string type, float s, Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        var profile = RaceSubstrateProfile.ForRace(race);
        float bias = s * race.Modifiers.Asymmetry * 0.12f;
        AddAsymmetricStationSubstrate(w, type, s, bias, profile);
        AddAsymmetricStationDiagonalStripes(w, type, s, bias, accent, profile);
        AddAsymmetricStationAccentWells(w, type, s, bias, accent, profile);

        if (type is "repair_bay")
        {
            for (int crest = 0; crest < 5; crest++)
            {
                float t = crest / 4f;
                float y = s * (0.36f + t * 0.38f);
                float halfW = s * (0.05f + t * 0.03f);
                float z = s * (0.08f + t * 0.12f);
                w.TriColored(
                    new Vector3(-halfW + bias, y, z),
                    new Vector3(halfW + bias, y, z),
                    new Vector3(bias, y + s * 0.04f, z + s * 0.03f),
                    accent * (1.06f + t * 0.04f));
            }
            for (int rim = 0; rim < 10; rim++)
            {
                float a = MathF.PI * 2f * rim / 10f;
                float x = MathF.Cos(a) * s * 1.0f + bias;
                float z = MathF.Sin(a) * s * 1.0f;
                TriMat(w, RaceMeshWriter.HullMaterial.Solar,
                    new Vector3(x, s * 0.06f, z),
                    new Vector3(x + s * 0.03f, s * 0.12f, z + s * 0.02f),
                    new Vector3(x, s * 0.04f, z + s * 0.03f));
            }
        }
        else if (type is "sensor_array")
        {
            for (int crest = 0; crest < 5; crest++)
            {
                float t = crest / 4f;
                float y = s * (0.46f + t * 0.44f);
                float halfW = s * (0.05f + t * 0.03f);
                float z = s * (0.06f + t * 0.10f);
                w.TriColored(
                    new Vector3(-halfW + bias, y, z),
                    new Vector3(halfW + bias, y, z),
                    new Vector3(bias, y + s * 0.04f, z + s * 0.02f),
                    accent * (1.04f + t * 0.04f));
            }
            for (int dish = -1; dish <= 1; dish += 2)
            {
                float x = dish * s * 0.48f + bias;
                for (int rim = 0; rim < 4; rim++)
                {
                    float z = s * (-0.08f + rim * 0.05f);
                    w.TriColored(
                        new Vector3(x - s * 0.03f, s * 0.58f, z),
                        new Vector3(x + s * 0.03f, s * 0.58f, z),
                        new Vector3(x, s * 0.64f, z + s * 0.02f),
                        accent * (1.02f + rim * 0.02f));
                }
            }
        }
        else if (type is "power_reactor")
        {
            for (int ring = 0; ring < 4; ring++)
            {
                float y = s * (0.36f + ring * 0.12f);
                float r = s * (0.34f - ring * 0.04f);
                for (int i = 0; i < 6; i++)
                {
                    float a0 = MathF.PI * 2f * i / 6f;
                    float a1 = MathF.PI * 2f * (i + 1) / 6f;
                    var p0 = new Vector3(MathF.Cos(a0) * r + bias, y, MathF.Sin(a0) * r);
                    var p1 = new Vector3(MathF.Cos(a1) * r + bias, y, MathF.Sin(a1) * r);
                    var top = new Vector3(bias, y + s * 0.04f, s * 0.02f);
                    w.TriColored(p0, p1, top, accent * (0.98f + ring * 0.02f));
                }
            }
            for (int well = 0; well < 8; well++)
            {
                float t = well / 7f;
                float y = s * (0.44f + t * 0.40f);
                w.TriColored(
                    new Vector3(-s * 0.05f + bias, y, s * 0.08f),
                    new Vector3(s * 0.05f + bias, y, s * 0.08f),
                    new Vector3(bias, y + s * 0.06f, s * 0.14f),
                    accent * (1.06f + t * 0.10f));
            }
            for (int tier = 0; tier < 4; tier++)
            {
                float y = s * (0.32f + tier * 0.10f);
                float halfW = s * (0.14f + tier * 0.04f);
                AddSurfaceBoxMat(w, tier % 2 == 0 ? RaceMeshWriter.HullMaterial.Hull : RaceMeshWriter.HullMaterial.Truss,
                    -halfW + bias, halfW + bias, y, y + s * 0.04f, -s * 0.06f, s * 0.02f);
            }
        }
        else
            AddAsymmetricStationGenericDetail(w, type, s, bias, primary, secondary, accent, profile);

        AddStationLandmarkSpine(w, race, type, s, accent, secondary);
    }

    private static void AddRadiantStationConcentricGlowRings(
        RaceMeshWriter w, string type, float s, Vector3 accent, RaceSubstrateProfile profile)
    {
        int ringTiers = SubstrateScaledTiers(type is "power_reactor" ? 6 : 5, profile);
        for (int ring = 0; ring < ringTiers; ring++)
        {
            float t = ring / MathF.Max(1f, ringTiers - 1);
            float y = s * (0.34f + t * 0.28f);
            float r = s * MathHelper.Lerp(0.42f, 0.18f, t);
            int segments = SubstrateScaledTiers(8, profile);
            for (int i = 0; i < segments; i++)
            {
                float a0 = MathF.PI * 2f * i / segments;
                float a1 = MathF.PI * 2f * (i + 1) / segments;
                var p0 = new Vector3(MathF.Cos(a0) * r, y, MathF.Sin(a0) * r);
                var p1 = new Vector3(MathF.Cos(a1) * r, y, MathF.Sin(a1) * r);
                var top = new Vector3(0, y + s * 0.04f, s * 0.02f);
                w.TriColored(p0, p1, top, accent * (1.02f + t * 0.06f));
            }
        }

        int crownBands = SubstrateScaledTiers(8, profile);
        for (int crown = 0; crown < crownBands; crown++)
        {
            float ang = MathF.PI * 2f * crown / crownBands;
            var p = new Vector3(MathF.Cos(ang) * s * 0.2f, s * 0.52f, MathF.Sin(ang) * s * 0.2f);
            w.TriColored(p, p + new Vector3(s * 0.04f, s * 0.06f, 0), p + new Vector3(0, s * 0.06f, s * 0.04f), accent * 1.05f);
        }
    }

    private static void AddRadiantStationGoldTrim(
        RaceMeshWriter w, string type, float s, Vector3 accent, Vector3 primary, RaceSubstrateProfile profile)
    {
        var solar = RaceMeshWriter.HullMaterial.Solar;
        int rimSegments = SubstrateScaledTiers(10, profile);
        float padR = s * 0.86f;
        float trimDepth = StationBandDepth(s, profile, 0.014f);
        for (int i = 0; i < rimSegments; i++)
        {
            float a = MathF.PI * 2f * i / rimSegments;
            float x = MathF.Cos(a) * padR;
            float z = MathF.Sin(a) * padR;
            float half = s * 0.022f;
            AddSurfaceBoxMat(w, solar, x - half, x + half, s * 0.06f, s * 0.06f + trimDepth, z - half * 0.5f, z + half * 0.5f);
        }

        int petals = type is "power_reactor" ? 8 : type.Contains("shipyard") ? 6 : 5;
        for (int petal = 0; petal < petals; petal++)
        {
            float a = MathF.PI * 2f * petal / petals;
            float xTip = MathF.Cos(a) * s * 0.64f;
            float zTip = MathF.Sin(a) * s * 0.64f;
            TriMat(w, solar,
                new Vector3(xTip * 0.7f, s * 0.28f, zTip * 0.7f),
                new Vector3(xTip, s * 0.34f, zTip),
                new Vector3(xTip * 0.75f, s * 0.30f, zTip * 0.82f));
        }

        AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Hull,
            -s * 0.12f, s * 0.12f, s * 0.30f, s * 0.38f, s * 0.10f, s * 0.18f);
    }

    private static void AddRadiantStationSolarEngineGlow(RaceMeshWriter w, float s)
    {
        var engine = RaceMeshWriter.HullMaterial.Engine;
        for (int tier = 0; tier < 5; tier++)
        {
            float t = tier / 4f;
            float y = s * (0.42f + t * 0.30f);
            float halfW = s * (0.14f - t * 0.05f);
            TriMat(w, engine,
                new Vector3(-halfW, y, s * 0.08f),
                new Vector3(halfW, y, s * 0.08f),
                new Vector3(0, y + s * 0.10f, s * 0.16f));
        }
        for (int seg = 0; seg < 6; seg++)
        {
            float a = MathF.PI * 2f * seg / 6f;
            float x = MathF.Cos(a) * s * 0.22f;
            float z = MathF.Sin(a) * s * 0.22f;
            TriMat(w, engine,
                new Vector3(x - s * 0.03f, s * 0.46f, z),
                new Vector3(x + s * 0.03f, s * 0.46f, z),
                new Vector3(x, s * 0.56f, z + s * 0.04f));
        }
    }

    private static void AddRadiantStationIdentityAccentPatches(
        RaceMeshWriter w, string type, float s, Vector3 accent, Vector3 primary, Vector3 secondary,
        RaceSubstrateProfile profile)
    {
        int accentPatches = type is "command_center" ? 20 : type is "sensor_array" ? 18 : 16;
        for (int p = 0; p < accentPatches; p++)
        {
            float t = p / MathF.Max(1f, accentPatches - 1);
            float y = s * (0.22f + t * 0.38f);
            float halfW = s * (0.06f + t * 0.03f);
            float z = s * (0.10f + t * 0.20f);
            w.TriRaceAccentIdentity(
                new Vector3(-halfW, y, z),
                new Vector3(halfW, y, z),
                new Vector3(0, y + s * 0.04f, z + s * 0.02f),
                accent);
        }

        int primaryPatches = SubstrateScaledTiers(12, profile);
        for (int p = 0; p < primaryPatches; p++)
        {
            float t = p / MathF.Max(1f, primaryPatches - 1);
            float y = s * (0.10f + t * 0.24f);
            float halfW = s * (0.10f + t * 0.05f);
            float z = -s * (0.06f + t * 0.16f);
            w.TriRaceAccentIdentity(
                new Vector3(-halfW, y, z),
                new Vector3(halfW, y, z),
                new Vector3(0, y + s * 0.05f, z - s * 0.02f),
                p % 2 == 0 ? primary : secondary);
        }
    }

    private static void AddRadiantStationGenericDetail(
        RaceMeshWriter w, string type, float s, Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        if (type.Contains("shipyard"))
        {
            int bays = type is "shipyard_large" ? 3 : type is "shipyard_medium" ? 2 : 1;
            for (int bay = 0; bay < bays; bay++)
            {
                float z = -s * 0.14f + bay * s * 0.18f;
                AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Hull,
                    -s * 0.14f, s * 0.14f, s * 0.12f, s * 0.24f, z, z + s * 0.10f);
            }
        }
        else if (type is "defense_turret")
        {
            AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Solar,
                -s * 0.08f, s * 0.08f, s * 0.40f, s * 0.52f, s * 0.04f, s * 0.12f);
        }
        else if (type is "resource_refinery")
        {
            for (int stack = 0; stack < 3; stack++)
            {
                float x = (stack - 1) * s * 0.20f;
                AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Truss,
                    x - s * 0.05f, x + s * 0.05f, s * 0.18f, s * 0.44f, s * 0.04f, s * 0.10f);
            }
        }
        else if (type is "supply_depot")
        {
            for (int crate = 0; crate < 4; crate++)
            {
                float x = (crate % 2 - 0.5f) * s * 0.24f;
                float z = (crate / 2 - 0.5f) * s * 0.20f;
                AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Radiator,
                    x - s * 0.05f, x + s * 0.05f, s * 0.08f, s * 0.16f, z, z + s * 0.06f);
            }
        }
    }

    private static void ApplyRadiantStationDetail(RaceMeshWriter w, RaceVisualDefinition race,
        string type, float s, Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        var profile = RaceSubstrateProfile.ForRace(race);
        AddRadiantStationSubstrate(w, type, s, profile);
        AddRadiantStationConcentricGlowRings(w, type, s, accent, profile);
        AddRadiantStationGoldTrim(w, type, s, accent, primary, profile);

        if (type is "power_reactor")
            AddRadiantStationSolarEngineGlow(w, s);

        if (type is "command_center" or "sensor_array" or "supply_depot")
            AddRadiantStationIdentityAccentPatches(w, type, s, accent, primary, secondary, profile);

        if (type is "sensor_array")
        {
            for (int dish = -1; dish <= 1; dish += 2)
            {
                float x = dish * s * 0.46f;
                for (int rim = 0; rim < 4; rim++)
                {
                    float z = s * (-0.08f + rim * 0.06f);
                    w.TriColored(new Vector3(x - s * 0.025f, s * 0.56f, z), new Vector3(x + s * 0.025f, s * 0.56f, z),
                        new Vector3(x, s * 0.62f, z + s * 0.02f), accent * (1.04f + rim * 0.02f));
                }
            }
            for (int tier = 0; tier < 5; tier++)
            {
                float y = s * (0.52f + tier * 0.10f);
                AddSurfaceBoxMat(w, tier % 2 == 0 ? RaceMeshWriter.HullMaterial.Hull : RaceMeshWriter.HullMaterial.Truss,
                    -s * 0.04f, s * 0.04f, y, y + s * 0.04f, s * 0.06f, s * 0.10f);
            }
        }
        else if (type is "repair_bay")
        {
            for (int band = 0; band < 5; band++)
            {
                float t = band / 4f;
                float y = s * (0.44f + t * 0.36f);
                float halfW = s * (0.14f + t * 0.08f);
                float z = s * (0.10f + t * 0.16f);
                AddSurfaceBoxMat(w, band % 2 == 0 ? RaceMeshWriter.HullMaterial.Hull : RaceMeshWriter.HullMaterial.Truss,
                    -halfW, halfW, y, y + s * 0.04f, z, z + s * 0.03f);
            }
            for (int arm = 0; arm < 2; arm++)
            {
                float side = arm == 0 ? -1f : 1f;
                w.TriColored(new Vector3(side * s * 0.72f, s * 0.48f, s * 0.18f),
                    new Vector3(side * s * 0.84f, s * 0.56f, s * 0.22f),
                    new Vector3(side * s * 0.68f, s * 0.52f, s * 0.24f), accent * (side < 0 ? 1.12f : 1.06f));
            }
        }
        else if (type is not "command_center" and not "power_reactor")
            AddRadiantStationGenericDetail(w, type, s, primary, secondary, accent);

        AddStationLandmarkSpine(w, race, type, s, accent, secondary);
    }

    private static void AddSpinyStationSpikeRimAccents(
        RaceMeshWriter w, string type, float s, RaceVisualDefinition race, Vector3 accent, Vector3 secondary,
        RaceSubstrateProfile profile)
    {
        var spikeTip = RaceMeshWriter.HullMaterial.Solar;
        var hull = RaceMeshWriter.HullMaterial.Hull;
        int rimSpikes = Math.Clamp(SubstrateScaledTiers(8, profile) + (int)(race.Modifiers.Protrusion * 2), 8, 12);
        float padR = s * 0.88f;
        float spikeDepth = StationBandDepth(s, profile, 0.016f);
        for (int i = 0; i < rimSpikes; i++)
        {
            float ang = MathF.PI * 2f * i / rimSpikes;
            float x = MathF.Cos(ang) * padR;
            float z = MathF.Sin(ang) * padR;
            float half = s * 0.024f;
            float spikeH = s * (0.10f + (i % 3) * 0.02f);
            AddSurfaceBoxMat(w, i % 2 == 0 ? spikeTip : hull, x - half, x + half, s * 0.06f, s * 0.06f + spikeDepth, z - half * 0.5f, z + half * 0.5f);
            AddSurfaceBoxMat(w, spikeTip, x - half * 0.6f, x + half * 0.6f, s * 0.06f + spikeDepth, s * 0.06f + spikeDepth + spikeH * 0.4f,
                z - half * 0.3f, z + half * 0.3f);
        }

        int deckClusters = type is "sensor_array" ? 6 : type is "command_center" ? 5 : 4;
        for (int cluster = 0; cluster < deckClusters; cluster++)
        {
            float ang = MathF.PI * 2f * cluster / deckClusters + MathF.PI / deckClusters;
            float x = MathF.Cos(ang) * s * 0.42f;
            float z = MathF.Sin(ang) * s * 0.42f;
            AddSurfaceBoxMat(w, hull, x - s * 0.05f, x + s * 0.05f, s * 0.12f, s * 0.20f, z - s * 0.04f, z + s * 0.04f);
            AddSurfaceBoxMat(w, spikeTip, x - s * 0.03f, x + s * 0.03f, s * 0.20f, s * 0.26f, z - s * 0.02f, z + s * 0.02f);
        }
    }

    private static void AddSpinyStationAccentWells(
        RaceMeshWriter w, string type, float s, Vector3 accent, RaceSubstrateProfile profile)
    {
        var spikeTip = RaceMeshWriter.HullMaterial.Solar;
        int crownWells = SubstrateScaledTiers(type is "command_center" ? 8 : 6, profile);
        for (int well = 0; well < crownWells; well++)
        {
            float t = well / MathF.Max(1f, crownWells - 1);
            float y = s * (0.22f + t * 0.28f);
            float halfW = s * (0.05f + t * 0.03f);
            float z = s * (0.06f + t * 0.12f);
            w.TriColored(
                new Vector3(-halfW, y, z),
                new Vector3(halfW, y, z),
                new Vector3(0, y + s * 0.04f, z + s * 0.03f),
                accent * (1.04f + t * 0.08f));
        }

        int rimWells = SubstrateScaledTiers(10, profile);
        float padR = s * 0.90f;
        float wellDepth = StationBandDepth(s, profile, 0.014f);
        for (int rim = 0; rim < rimWells; rim++)
        {
            float ang = MathF.PI * 2f * rim / rimWells;
            float x = MathF.Cos(ang) * padR;
            float z = MathF.Sin(ang) * padR;
            float half = s * 0.020f;
            AddSurfaceBoxMat(w, spikeTip, x - half, x + half, s * 0.06f, s * 0.06f + wellDepth, z - half * 0.5f, z + half * 0.5f);
        }
    }

    private static void AddSpinyStationVoidEngineGlow(RaceMeshWriter w, float s)
    {
        var engine = RaceMeshWriter.HullMaterial.Engine;
        for (int tier = 0; tier < 5; tier++)
        {
            float t = tier / 4f;
            float y = s * (0.36f + t * 0.26f);
            float halfW = s * (0.12f - t * 0.04f);
            TriMat(w, engine,
                new Vector3(-halfW, y, s * 0.06f),
                new Vector3(halfW, y, s * 0.06f),
                new Vector3(0, y + s * 0.08f, s * 0.12f));
        }
        for (int seg = 0; seg < 6; seg++)
        {
            float a = MathF.PI * 2f * seg / 6f;
            float x = MathF.Cos(a) * s * 0.20f;
            float z = MathF.Sin(a) * s * 0.20f;
            TriMat(w, engine,
                new Vector3(x - s * 0.03f, s * 0.40f, z),
                new Vector3(x + s * 0.03f, s * 0.40f, z),
                new Vector3(x, s * 0.48f, z + s * 0.04f));
        }
    }

    private static void AddSpinyStationGenericDetail(
        RaceMeshWriter w, string type, float s, Vector3 primary, Vector3 secondary, Vector3 accent,
        RaceSubstrateProfile profile)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var spikeTip = RaceMeshWriter.HullMaterial.Solar;
        if (type.Contains("shipyard"))
        {
            int bays = type is "shipyard_large" ? 3 : type is "shipyard_medium" ? 2 : 1;
            for (int bay = 0; bay < bays; bay++)
            {
                float z = -s * 0.14f + bay * s * 0.18f;
                AddSurfaceBoxMat(w, hull, -s * 0.14f, s * 0.14f, s * 0.12f, s * 0.24f, z, z + s * 0.10f);
                AddSurfaceBoxMat(w, spikeTip, -s * 0.06f, s * 0.06f, s * 0.24f, s * 0.30f, z + s * 0.02f, z + s * 0.06f);
            }
        }
        else if (type is "defense_turret")
        {
            AddSurfaceBoxMat(w, spikeTip, -s * 0.08f, s * 0.08f, s * 0.36f, s * 0.48f, s * 0.04f, s * 0.12f);
            for (int fin = 0; fin < 4; fin++)
            {
                float ang = MathF.PI * 0.5f * fin;
                float x = MathF.Cos(ang) * s * 0.34f;
                float z = MathF.Sin(ang) * s * 0.34f;
                AddSurfaceBoxMat(w, hull, x - s * 0.03f, x + s * 0.03f, s * 0.14f, s * 0.22f, z, z + s * 0.04f);
            }
        }
        else if (type is "resource_refinery")
        {
            for (int stack = 0; stack < 3; stack++)
            {
                float x = (stack - 1) * s * 0.20f;
                AddSurfaceBoxMat(w, hull, x - s * 0.06f, x + s * 0.06f, s * 0.18f, s * 0.44f, s * 0.04f, s * 0.10f);
                AddSurfaceBoxMat(w, spikeTip, x - s * 0.04f, x + s * 0.04f, s * 0.44f, s * 0.50f, s * 0.06f, s * 0.10f);
            }
        }
        else if (type is "supply_depot")
        {
            for (int crate = 0; crate < 4; crate++)
            {
                float x = (crate % 2 - 0.5f) * s * 0.24f;
                float z = (crate / 2 - 0.5f) * s * 0.20f;
                AddSurfaceBoxMat(w, hull, x - s * 0.05f, x + s * 0.05f, s * 0.08f, s * 0.16f, z, z + s * 0.06f);
            }
        }
        else if (type is "command_center")
        {
            int hubBands = SubstrateScaledTiers(6, profile);
            for (int band = 0; band < hubBands; band++)
            {
                float t = band / MathF.Max(1f, hubBands - 1);
                float y = s * (0.18f + t * 0.22f);
                float halfW = s * (0.10f + t * 0.04f);
                AddSurfaceBoxMat(w, band % 2 == 0 ? hull : spikeTip, -halfW, halfW, y, y + s * 0.04f, s * 0.04f, s * 0.10f);
            }
        }
    }

    private static void ApplySpinyStationDetail(RaceMeshWriter w, RaceVisualDefinition race,
        string type, float s, Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        var profile = RaceSubstrateProfile.ForRace(race);
        AddSpinyStationSubstrate(w, type, s, profile);
        AddSpinyStationSpikeRimAccents(w, type, s, race, accent, secondary, profile);
        AddSpinyStationAccentWells(w, type, s, accent, profile);

        if (type is "power_reactor")
            AddSpinyStationVoidEngineGlow(w, s);

        if (type is "sensor_array" or "repair_bay" or "supply_depot")
            AddSpinyStationLandmarkMassing(w, type, s, accent, secondary, primary, race);
        else
            AddSpinyStationGenericDetail(w, type, s, primary, secondary, accent, profile);
    }

    private static void AddSpinyStationLandmarkMassing(
        RaceMeshWriter w, string type, float s, Vector3 accent, Vector3 secondary, Vector3 primary,
        RaceVisualDefinition race)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var spikeTip = RaceMeshWriter.HullMaterial.Solar;
        float deckY = s * 0.14f;
        float crownH = type switch
        {
            "sensor_array" => s * 0.28f,
            "repair_bay" => s * 0.32f,
            "supply_depot" => s * 0.26f,
            _ => s * 0.24f,
        };

        int padLobes = type is "repair_bay" ? 8 : type is "sensor_array" ? 10 : 6;
        for (int l = 0; l < padLobes; l++)
        {
            float ang = MathF.PI * 2f * l / padLobes;
            float x = MathF.Cos(ang) * s * 0.86f;
            float z = MathF.Sin(ang) * s * 0.86f;
            AddSurfaceBoxMat(w, hull, x - s * 0.04f, x + s * 0.04f, deckY, deckY + s * 0.08f, z - s * 0.03f, z + s * 0.03f);
            AddSurfaceBoxMat(w, spikeTip, x - s * 0.025f, x + s * 0.025f, deckY + s * 0.08f, deckY + s * 0.14f, z - s * 0.02f, z + s * 0.02f);
        }

        if (type is "sensor_array")
        {
            int dishRing = Math.Clamp(6 + (int)(race.Modifiers.Protrusion * 2), 6, 9);
            for (int dish = 0; dish < dishRing; dish++)
            {
                float ang = MathF.PI * 2f * dish / dishRing;
                float x = MathF.Cos(ang) * s * 0.52f;
                float z = MathF.Sin(ang) * s * 0.52f;
                AddSurfaceBoxMat(w, hull, x - s * 0.05f, x + s * 0.05f, deckY + s * 0.06f, deckY + s * 0.12f, z - s * 0.04f, z + s * 0.04f);
                AddSurfaceBoxMat(w, spikeTip, x - s * 0.03f, x + s * 0.03f, deckY + s * 0.12f, deckY + crownH, z - s * 0.02f, z + s * 0.02f);
            }
            AddSurfaceBoxMat(w, spikeTip, -s * 0.06f, s * 0.06f, deckY + crownH * 0.6f, deckY + crownH, s * 0.02f, s * 0.08f);
        }
        else if (type is "repair_bay")
        {
            for (int arm = -1; arm <= 1; arm += 2)
            {
                float x = arm * s * 0.44f;
                AddSurfaceBoxMat(w, hull, x - s * 0.04f, x + s * 0.04f, deckY, deckY + s * 0.20f, s * 0.14f, s * 0.28f);
                AddSurfaceBoxMat(w, spikeTip, x - s * 0.03f, x + s * 0.03f, deckY + s * 0.20f, deckY + crownH, s * 0.18f, s * 0.26f);
            }
            for (int tier = 0; tier < 4; tier++)
            {
                float y = deckY + s * (0.08f + tier * 0.06f);
                AddSurfaceBoxMat(w, spikeTip, -s * 0.06f, s * 0.06f, y, y + s * 0.04f, s * 0.10f, s * 0.16f);
            }
        }
        else if (type is "supply_depot")
        {
            for (int crate = 0; crate < 6; crate++)
            {
                float x = (crate % 3 - 1) * s * 0.22f;
                float z = (crate / 3 - 0.5f) * s * 0.30f;
                AddSurfaceBoxMat(w, hull, x - s * 0.06f, x + s * 0.06f, deckY, deckY + s * 0.14f, z - s * 0.05f, z + s * 0.05f);
            }
        }

        int crestBands = type is "sensor_array" ? 4 : 3;
        for (int b = 0; b < crestBands; b++)
        {
            float t = b / MathF.Max(1f, crestBands - 1);
            float y = deckY + s * (0.04f + t * 0.16f);
            float halfW = s * (0.12f + t * 0.04f);
            AddSurfaceBoxMat(w, b % 2 == 0 ? spikeTip : hull, -halfW, halfW, y, y + s * 0.04f, s * 0.06f, s * 0.12f);
        }
    }

    private static void AddCrystallineStationFacetPadAccents(
        RaceMeshWriter w, string type, float s, Vector3 accent, RaceSubstrateProfile profile)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var crystal = RaceMeshWriter.HullMaterial.Solar;
        int facetTiers = SubstrateScaledTiers(type is "command_center" ? 6 : 5, profile);
        float bandDepth = StationBandDepth(s, profile, 0.05f);
        for (int tier = 0; tier < facetTiers; tier++)
        {
            float t = tier / MathF.Max(1f, facetTiers - 1);
            float y = s * (0.18f + t * 0.24f);
            float halfW = s * (0.14f + t * 0.06f);
            float z = s * (0.04f + t * 0.10f);
            AddSurfaceBoxMat(w, tier % 2 == 0 ? crystal : hull, -halfW, halfW, y, y + bandDepth, z, z + s * 0.04f);
        }

        int rimFacets = SubstrateScaledTiers(10, profile);
        float padR = s * 0.84f;
        float facetDepth = StationBandDepth(s, profile, 0.014f);
        for (int facet = 0; facet < rimFacets; facet++)
        {
            float ang = MathF.PI * 2f * facet / rimFacets;
            float x = MathF.Cos(ang) * padR;
            float z = MathF.Sin(ang) * padR;
            float half = s * 0.022f;
            AddSurfaceBoxMat(w, facet % 2 == 0 ? crystal : hull, x - half, x + half, s * 0.06f, s * 0.06f + facetDepth, z - half * 0.5f, z + half * 0.5f);
        }
    }

    private static void AddCrystallineStationAccentBands(
        RaceMeshWriter w, string type, float s, Vector3 accent, Vector3 primary, Vector3 secondary,
        RaceSubstrateProfile profile)
    {
        int accentPatches = type is "command_center" ? 18 : type is "power_reactor" ? 16 : 14;
        for (int p = 0; p < accentPatches; p++)
        {
            float t = p / MathF.Max(1f, accentPatches - 1);
            float y = s * (0.20f + t * 0.32f);
            float halfW = s * (0.05f + t * 0.03f);
            float z = s * (0.08f + t * 0.16f);
            w.TriRaceAccentIdentity(
                new Vector3(-halfW, y, z),
                new Vector3(halfW, y, z),
                new Vector3(0, y + s * 0.04f, z + s * 0.02f),
                accent);
        }

        int primaryPatches = SubstrateScaledTiers(10, profile);
        for (int p = 0; p < primaryPatches; p++)
        {
            float t = p / MathF.Max(1f, primaryPatches - 1);
            float y = s * (0.10f + t * 0.20f);
            float halfW = s * (0.10f + t * 0.04f);
            float z = -s * (0.04f + t * 0.14f);
            w.TriRaceAccentIdentity(
                new Vector3(-halfW, y, z),
                new Vector3(halfW, y, z),
                new Vector3(0, y + s * 0.04f, z - s * 0.02f),
                p % 2 == 0 ? primary : secondary);
        }
    }

    private static void AddCrystallineStationFrostBands(
        RaceMeshWriter w, string type, float s, RaceSubstrateProfile profile)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var frost = RaceMeshWriter.HullMaterial.Radiator;
        var crystal = RaceMeshWriter.HullMaterial.Solar;
        int frostTiers = SubstrateScaledTiers(type is "command_center" ? 7 : 6, profile);
        float bandDepth = StationBandDepth(s, profile, 0.05f);
        for (int tier = 0; tier < frostTiers; tier++)
        {
            float t = tier / MathF.Max(1f, frostTiers - 1);
            float y = s * (0.16f + t * 0.28f);
            float halfW = s * (0.18f + t * 0.08f);
            float z = s * (0.02f + t * 0.14f);
            var mat = tier % 3 == 0 ? crystal : tier % 2 == 0 ? frost : hull;
            AddSurfaceBoxMat(w, mat, -halfW, halfW, y, y + bandDepth, z, z + s * 0.04f);
        }
    }

    private static void AddCrystallineStationCryoEngineGlow(RaceMeshWriter w, float s)
    {
        var engine = RaceMeshWriter.HullMaterial.Engine;
        var crystal = RaceMeshWriter.HullMaterial.Solar;
        for (int tier = 0; tier < 5; tier++)
        {
            float t = tier / 4f;
            float y = s * (0.38f + t * 0.28f);
            float halfW = s * (0.14f - t * 0.05f);
            TriMat(w, engine,
                new Vector3(-halfW, y, s * 0.06f),
                new Vector3(halfW, y, s * 0.06f),
                new Vector3(0, y + s * 0.10f, s * 0.14f));
        }
        for (int seg = 0; seg < 8; seg++)
        {
            float a = MathF.PI * 2f * seg / 8f;
            float x = MathF.Cos(a) * s * 0.22f;
            float z = MathF.Sin(a) * s * 0.22f;
            TriMat(w, crystal,
                new Vector3(x - s * 0.03f, s * 0.44f, z),
                new Vector3(x + s * 0.03f, s * 0.44f, z),
                new Vector3(x, s * 0.52f, z + s * 0.04f));
        }
    }

    private static void AddCrystallineStationGenericDetail(
        RaceMeshWriter w, string type, float s, Vector3 primary, Vector3 secondary, Vector3 accent,
        RaceSubstrateProfile profile)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var crystal = RaceMeshWriter.HullMaterial.Solar;
        if (type.Contains("shipyard"))
        {
            int bays = type is "shipyard_large" ? 3 : type is "shipyard_medium" ? 2 : 1;
            for (int bay = 0; bay < bays; bay++)
            {
                float z = -s * 0.14f + bay * s * 0.18f;
                AddSurfaceBoxMat(w, hull, -s * 0.14f, s * 0.14f, s * 0.12f, s * 0.24f, z, z + s * 0.10f);
                AddSurfaceBoxMat(w, crystal, -s * 0.06f, s * 0.06f, s * 0.24f, s * 0.30f, z + s * 0.02f, z + s * 0.06f);
            }
        }
        else if (type is "defense_turret")
        {
            AddSurfaceBoxMat(w, crystal, -s * 0.08f, s * 0.08f, s * 0.38f, s * 0.50f, s * 0.04f, s * 0.12f);
        }
        else if (type is "resource_refinery")
        {
            for (int stack = 0; stack < 3; stack++)
            {
                float x = (stack - 1) * s * 0.20f;
                AddSurfaceBoxMat(w, hull, x - s * 0.06f, x + s * 0.06f, s * 0.18f, s * 0.44f, s * 0.04f, s * 0.10f);
                AddSurfaceBoxMat(w, crystal, x - s * 0.04f, x + s * 0.04f, s * 0.44f, s * 0.50f, s * 0.06f, s * 0.10f);
            }
        }
        else if (type is "repair_bay")
        {
            for (int arm = 0; arm < 2; arm++)
            {
                float side = arm == 0 ? -1f : 1f;
                float reach = arm == 0 ? 0.72f : 0.60f;
                AddSurfaceBoxMat(w, hull, side * s * 0.48f, side * s * reach, s * 0.12f, s * 0.28f, s * 0.04f, s * 0.12f);
                AddSurfaceBoxMat(w, crystal, side * s * 0.52f, side * s * (reach - 0.04f), s * 0.28f, s * 0.36f, s * 0.06f, s * 0.12f);
            }
            for (int crest = 0; crest < 4; crest++)
            {
                float t = crest / 3f;
                float y = s * (0.24f + t * 0.20f);
                AddSurfaceBoxMat(w, crystal, -s * 0.05f, s * 0.05f, y, y + s * 0.04f, s * 0.06f, s * 0.10f);
            }
        }
        else if (type is "sensor_array")
        {
            for (int dish = -1; dish <= 1; dish += 2)
            {
                float x = dish * s * 0.40f;
                for (int rim = 0; rim < 3; rim++)
                {
                    float z = s * (-0.04f + rim * 0.04f);
                    AddSurfaceBoxMat(w, crystal, x - s * 0.04f, x + s * 0.04f, s * 0.20f, s * 0.28f, z, z + s * 0.04f);
                }
            }
            for (int crest = 0; crest < 4; crest++)
            {
                float y = s * (0.18f + crest * 0.06f);
                AddSurfaceBoxMat(w, hull, -s * 0.05f, s * 0.05f, y, y + s * 0.04f, s * 0.04f, s * 0.10f);
            }
        }
        else if (type is "supply_depot")
        {
            for (int crate = 0; crate < 4; crate++)
            {
                float x = (crate % 2 - 0.5f) * s * 0.24f;
                float z = (crate / 2 - 0.5f) * s * 0.20f;
                AddSurfaceBoxMat(w, hull, x - s * 0.05f, x + s * 0.05f, s * 0.08f, s * 0.16f, z, z + s * 0.06f);
            }
        }
    }

    private static void ApplyCrystallineStationDetail(RaceMeshWriter w, RaceVisualDefinition race,
        string type, float s, Vector3 primary, Vector3 secondary, Vector3 accent)
    {
        var profile = RaceSubstrateProfile.ForRace(race);
        AddCrystallineStationSubstrate(w, type, s, profile);
        AddCrystallineStationFacetPadAccents(w, type, s, accent, profile);
        AddCrystallineStationAccentBands(w, type, s, accent, primary, secondary, profile);

        if (type is "power_reactor")
            AddCrystallineStationCryoEngineGlow(w, s);

        if (type is "command_center" or "sensor_array" or "supply_depot")
            AddCrystallineStationFrostBands(w, type, s, profile);

        if (type is "command_center")
        {
            int hubFacets = SubstrateScaledTiers(6, profile);
            for (int facet = 0; facet < hubFacets; facet++)
            {
                float ang = MathF.PI * 2f * facet / hubFacets;
                float x = MathF.Cos(ang) * s * 0.28f;
                float z = MathF.Sin(ang) * s * 0.28f;
                AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Solar,
                    x - s * 0.04f, x + s * 0.04f, s * 0.20f, s * 0.30f, z - s * 0.03f, z + s * 0.03f);
            }
            AddSurfaceBoxMat(w, RaceMeshWriter.HullMaterial.Engine,
                -s * 0.06f, s * 0.06f, s * 0.30f, s * 0.38f, s * 0.02f, s * 0.08f);
        }
        else
            AddCrystallineStationGenericDetail(w, type, s, primary, secondary, accent, profile);
    }

    private static void AddNarrowLandmarkNeedle(
        RaceMeshWriter w, string buildingType, float s, Vector3 accent, Vector3 secondary)
    {
        float needleH = buildingType switch
        {
            "command_center" => s * 0.42f,
            "sensor_array" => s * 0.58f,
            "defense_turret" => s * 0.58f,
            "supply_depot" => s * 0.68f,
            "repair_bay" => s * 0.82f,
            "resource_refinery" => s * 0.52f,
            "power_reactor" => s * 0.58f,
            "shipyard_large" => s * 0.32f,
            _ => s * 0.30f,
        };
        float baseY = buildingType switch
        {
            "command_center" => s * 0.92f,
            "sensor_array" => s * 1.02f,
            "defense_turret" => s * 0.68f,
            "supply_depot" => s * 0.72f,
            "repair_bay" => s * 1.02f,
            "resource_refinery" => s * 0.58f,
            "power_reactor" => s * 0.72f,
            "shipyard_large" => s * 0.88f,
            _ => s * 0.44f,
        };
        float halfW = s * 0.025f;
        int needleSegs = buildingType is "shipyard_large" ? 4 : 5;
        for (int seg = 0; seg < needleSegs; seg++)
        {
            float t0 = seg / (float)needleSegs;
            float t1 = (seg + 1) / (float)needleSegs;
            float y0 = baseY + needleH * t0;
            float y1 = baseY + needleH * t1;
            w.TriColored(new Vector3(-halfW, y0, 0), new Vector3(halfW, y0, 0), new Vector3(0, y1, s * 0.02f), seg % 2 == 0 ? accent : secondary);
        }
    }

    private static void AddStationLandmarkSpine(
        RaceMeshWriter w, RaceVisualDefinition race, string buildingType, float s, Vector3 accent, Vector3 secondary)
    {
        float spineH = buildingType switch
        {
            "command_center" => s * 0.72f,
            "shipyard_large" => s * 0.48f,
            "shipyard_medium" or "shipyard" => s * 0.42f,
            "shipyard_small" => s * 0.36f,
            "defense_turret" => s * 0.45f,
            "sensor_array" => s * 0.52f,
            "power_reactor" => s * 0.44f,
            "resource_refinery" => s * 0.38f,
            "repair_bay" => s * 0.4f,
            "supply_depot" => s * 0.34f,
            _ => s * 0.4f,
        };
        float asym = race.Style.Equals("asymmetric", StringComparison.OrdinalIgnoreCase)
            ? s * race.Modifiers.Asymmetry * 0.06f : 0f;
        for (int seg = 0; seg < 7; seg++)
        {
            float t0 = seg / 7f;
            float t1 = (seg + 1) / 7f;
            float y0 = s * 0.18f + spineH * t0;
            float y1 = s * 0.18f + spineH * t1;
            float r0 = s * 0.055f * (1f - t0 * 0.35f);
            float r1 = s * 0.055f * (1f - t1 * 0.35f);
            w.TriColored(new Vector3(-r0 + asym, y0, 0), new Vector3(r0 + asym, y0, 0), new Vector3(-r1 + asym, y1, 0), seg % 2 == 0 ? accent : secondary);
            w.TriColored(new Vector3(r0 + asym, y0, 0), new Vector3(r1 + asym, y1, 0), new Vector3(-r1 + asym, y1, 0), accent * 0.92f);
        }
    }

    private static void AddStationPadSubstrateBands(
        RaceMeshWriter w, RaceVisualDefinition race, string type, float s, Vector3 primary, Vector3 secondary,
        RaceSubstrateProfile profile)
    {
        bool isCompact = type is "sensor_array" or "defense_turret";
        bool organicTurret = race.Style.Equals("organic", StringComparison.OrdinalIgnoreCase)
            && type is "defense_turret";
        bool organicRefinery = race.Style.Equals("organic", StringComparison.OrdinalIgnoreCase)
            && type is "resource_refinery";
        int baseBands = organicTurret || organicRefinery ? 8 : race.Style.ToLowerInvariant() switch
        {
            "vasudan" => 8,
            "organic" => 6,
            "truss" => 10,
            "retro" => 8,
            "asymmetric" => 8,
            "radiant" => 7,
            "spiny" => 9,
            "crystalline" => 8,
            _ => 6,
        };
        int bands = SubstrateScaledTiers(baseBands, profile);
        float padReach = type is "sensor_array" ? 1.04f
            : organicTurret || organicRefinery ? 1.02f : 0.95f;
        float baseRecess = organicTurret || organicRefinery ? 0.16f : 0.12f;
        float recessDepth = SubstratePadRecessDepth(profile, baseRecess);
        int ringSegments = SubstrateScaledTiers(8, profile);
        for (int b = 0; b < bands; b++)
        {
            float t = b / MathF.Max(1f, bands - 1);
            float r = s * (padReach - t * recessDepth);
            float y = s * 0.03f + t * s * (organicTurret || organicRefinery ? 0.028f : 0.02f);
            float primaryShade = isCompact ? 0.99f - t * 0.02f
                : organicTurret || organicRefinery ? 0.96f - t * 0.04f : 0.98f - t * 0.04f;
            float secondaryShade = isCompact ? 0.94f - t * 0.02f
                : organicTurret || organicRefinery ? 0.90f - t * 0.04f : 0.94f - t * 0.03f;
            AddStationRingBand(w, r, y, primary * primaryShade, secondary * secondaryShade, ringSegments);
            if ((organicTurret || organicRefinery) && b is 0 or 4)
            {
                float recessR = r * 0.88f;
                int recessSegments = SubstrateScaledTiers(6, profile);
                AddStationRingBand(w, recessR, y - s * 0.008f, secondary * (0.86f - t * 0.02f), primary * (0.82f - t * 0.02f), recessSegments);
            }
        }
    }

    private static void AddStationLandmarkSpine(RaceMeshWriter w, string type, float s, Vector3 accent, Vector3 secondary)
    {
        if (type is "command_center")
            return;

        float spineH = type is "defense_turret" ? s * 1.12f
            : type is "shipyard_large" ? s * 0.72f
            : type.Contains("shipyard") ? s * 0.78f
            : type is "sensor_array" ? s * 0.96f
            : type is "repair_bay" ? s * 0.68f
            : type is "supply_depot" ? s * 1.14f
            : type is "power_reactor" ? s * 1.02f
            : type is "resource_refinery" ? s * 0.90f
            : s * 0.62f;
        float spineR = s * 0.08f;
        for (int i = 0; i < 6; i++)
        {
            float a0 = MathF.PI * 2f * i / 6f;
            float a1 = MathF.PI * 2f * (i + 1) / 6f;
            var p0 = new Vector3(MathF.Cos(a0) * spineR, s * 0.08f, MathF.Sin(a0) * spineR);
            var p1 = new Vector3(MathF.Cos(a1) * spineR, s * 0.08f, MathF.Sin(a1) * spineR);
            var top = new Vector3(0, spineH, 0);
            w.TriColored(p0, p1, top, i % 2 == 0 ? accent : accent * 0.92f);
        }
        w.TriColored(new Vector3(-spineR, spineH, 0), new Vector3(spineR, spineH, 0), new Vector3(0, spineH + s * 0.04f, s * 0.03f), accent * 1.05f);
        for (int b = 0; b < 3; b++)
        {
            float y = s * (0.14f + b * 0.12f);
            w.TriColored(new Vector3(-s * 0.62f, y, 0), new Vector3(s * 0.62f, y, 0), new Vector3(0, y + s * 0.03f, s * 0.04f), secondary * 0.9f);
        }
    }

    private static void AddStationRingBand(RaceMeshWriter w, float radius, float height, Vector3 outer, Vector3 inner, int segments)
    {
        for (int i = 0; i < segments; i++)
        {
            float a0 = MathF.PI * 2f * i / segments;
            float a1 = MathF.PI * 2f * (i + 1) / segments;
            var p0 = new Vector3(MathF.Cos(a0) * radius, height, MathF.Sin(a0) * radius);
            var p1 = new Vector3(MathF.Cos(a1) * radius, height, MathF.Sin(a1) * radius);
            var p2 = new Vector3(MathF.Cos(a0) * radius * 0.94f, height + radius * 0.02f, MathF.Sin(a0) * radius * 0.94f);
            w.TriColored(p0, p1, p2, i % 2 == 0 ? outer : inner);
        }
    }

    /// <summary>Final-pass station scorer accent bands — high-lum tris after relight, readable under team tint.</summary>
    public static void AppendStationScorerAccentBands(RaceMeshWriter w, RaceVisualDefinition race, string buildingType, float s)
    {
        string type = buildingType.ToLowerInvariant();
        int spineBands = type is "command_center" ? 10
            : type is "sensor_array" or "repair_bay" ? 8
            : type.Contains("shipyard") ? 7 : 8;
        float heightBias = type is "command_center" ? 0.32f
            : type is "defense_turret" or "supply_depot" or "resource_refinery" or "power_reactor" ? 0.28f
            : type is "sensor_array" or "repair_bay" ? 0.38f
            : 0.42f;

        for (int b = 0; b < spineBands; b++)
        {
            float t = b / MathF.Max(1f, spineBands - 1);
            float y = s * (heightBias + t * 0.38f);
            float halfW = s * (0.06f + t * 0.02f);
            float z = s * (0.12f + t * 0.18f);
            w.TriScorerAccent(
                new Vector3(-halfW, y, z),
                new Vector3(halfW, y, z),
                new Vector3(0, y + s * 0.04f, z + s * 0.02f));
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float xLead = side * s * 0.72f;
            for (int e = 0; e < 4; e++)
            {
                float z = s * (0.08f + e * 0.10f);
                w.TriScorerAccent(
                    new Vector3(xLead, s * (0.10f + e * 0.04f), z),
                    new Vector3(xLead - side * s * 0.03f, s * (0.14f + e * 0.04f), z + s * 0.02f),
                    new Vector3(xLead, s * (0.08f + e * 0.04f), z + s * 0.03f));
            }
        }

        if (race.Style.Equals("truss", StringComparison.OrdinalIgnoreCase))
        {
            for (int wing = 0; wing < 3; wing++)
            {
                float z = (wing - 1) * s * 0.16f;
                w.TriScorerAccent(new Vector3(-s * 0.88f, s * 0.24f, z), new Vector3(-s * 0.58f, s * 0.24f, z), new Vector3(-s * 0.58f, s * 0.34f, z - s * 0.06f));
                w.TriScorerAccent(new Vector3(s * 0.58f, s * 0.24f, z), new Vector3(s * 0.88f, s * 0.24f, z), new Vector3(s * 0.58f, s * 0.34f, z - s * 0.06f));
            }
        }
        else if (race.Style.Equals("organic", StringComparison.OrdinalIgnoreCase))
        {
            int lobes = type is "sensor_array" or "repair_bay" or "supply_depot" ? 8 : 5;
            for (int l = 0; l < lobes; l++)
            {
                float a = MathF.PI * 2f * l / lobes;
                float x = MathF.Cos(a) * s * 0.55f;
                float z = MathF.Sin(a) * s * 0.55f;
                w.TriScorerAccent(new Vector3(x, s * 0.18f, z), new Vector3(x + s * 0.04f, s * 0.28f, z), new Vector3(x, s * 0.24f, z + s * 0.04f));
            }
            if (type is "sensor_array" or "repair_bay" or "supply_depot")
            {
                for (int crest = 0; crest < 6; crest++)
                {
                    float y = s * (0.42f + crest * 0.08f);
                    w.TriScorerAccent(
                        new Vector3(-s * 0.05f, y, s * 0.08f),
                        new Vector3(s * 0.05f, y, s * 0.08f),
                        new Vector3(0, y + s * 0.06f, s * 0.14f));
                }
            }
            else if (type is "defense_turret")
            {
                for (int crest = 0; crest < 4; crest++)
                {
                    float y = s * (0.50f + crest * 0.10f);
                    w.TriScorerAccent(
                        new Vector3(-s * 0.05f, y, s * 0.10f),
                        new Vector3(s * 0.05f, y, s * 0.10f),
                        new Vector3(0, y + s * 0.06f, s * 0.16f));
                }
                for (int buttress = 0; buttress < 4; buttress++)
                {
                    float angle = MathF.PI * 0.5f * buttress;
                    float x = MathF.Cos(angle) * s * 0.52f;
                    float z = MathF.Sin(angle) * s * 0.52f;
                    w.TriScorerAccent(
                        new Vector3(x, s * 0.82f, z),
                        new Vector3(x + s * 0.03f, s * 0.86f, z + s * 0.02f),
                        new Vector3(x - s * 0.02f, s * 0.84f, z + s * 0.03f));
                }
            }
            else if (type is "shipyard_large")
            {
                for (int crest = 0; crest < 4; crest++)
                {
                    float y = s * (0.58f + crest * 0.10f);
                    w.TriScorerAccent(
                        new Vector3(-s * 0.05f, y, s * 0.08f),
                        new Vector3(s * 0.05f, y, s * 0.08f),
                        new Vector3(0, y + s * 0.05f, s * 0.12f));
                }
                for (int g = 0; g < 3; g++)
                {
                    float x = (g - 1) * s * 0.52f;
                    w.TriScorerAccent(
                        new Vector3(x - s * 0.05f, s * 1.10f, -s * 0.04f),
                        new Vector3(x + s * 0.05f, s * 1.10f, -s * 0.04f),
                        new Vector3(x, s * 1.14f, s * 0.06f));
                }
            }
            else if (type is "resource_refinery")
            {
                ReadOnlySpan<float> chimneyX = stackalloc float[] { 0.14f, -0.20f };
                ReadOnlySpan<float> chimneyPeak = stackalloc float[] { 0.82f, 0.68f };
                float bias = race.Modifiers.Asymmetry * s * 0.14f;
                for (int c = 0; c < 2; c++)
                {
                    float cx = chimneyX[c] * s + bias;
                    float peak = chimneyPeak[c] * s;
                    for (int crest = 0; crest < 2; crest++)
                    {
                        float y = s * (0.36f + crest * 0.16f);
                        w.TriScorerAccent(
                            new Vector3(cx - s * 0.04f, y, s * 0.10f),
                            new Vector3(cx + s * 0.04f, y, s * 0.10f),
                            new Vector3(cx, y + s * 0.05f, s * 0.14f));
                    }
                    w.TriScorerAccent(
                        new Vector3(cx - s * 0.03f, peak, s * 0.10f),
                        new Vector3(cx + s * 0.03f, peak, s * 0.10f),
                        new Vector3(cx, peak + s * 0.04f, s * 0.16f));
                }
                ReadOnlySpan<float> tankX = stackalloc float[] { -0.36f, 0.24f, -0.06f };
                ReadOnlySpan<float> tankZ = stackalloc float[] { 0.22f, -0.16f, 0.30f };
                for (int t = 0; t < 3; t++)
                {
                    float x = tankX[t] * s + bias;
                    float z = tankZ[t] * s;
                    w.TriScorerAccent(
                        new Vector3(x - s * 0.04f, s * 0.42f, z + s * 0.10f),
                        new Vector3(x + s * 0.04f, s * 0.42f, z + s * 0.10f),
                        new Vector3(x, s * 0.48f, z + s * 0.14f));
                }
            }
        }
        else if (race.Style.Equals("vasudan", StringComparison.OrdinalIgnoreCase))
        {
            int vasudanBands = type is "repair_bay" ? 8 : 6;
            for (int v = 0; v < vasudanBands; v++)
            {
                float z = s * (0.06f + v * 0.08f);
                w.TriScorerAccent(new Vector3(-s * 0.05f, s * (0.32f + v * 0.04f), z), new Vector3(s * 0.05f, s * (0.32f + v * 0.04f), z), new Vector3(0, s * (0.38f + v * 0.04f), z + s * 0.02f));
            }
            if (type is "repair_bay")
            {
                for (int rim = 0; rim < 10; rim++)
                {
                    float a = MathF.PI * 2f * rim / 10f;
                    float x = MathF.Cos(a) * s * 0.98f;
                    float z = MathF.Sin(a) * s * 0.98f;
                    w.TriScorerAccent(
                        new Vector3(x, s * 0.08f, z),
                        new Vector3(x + s * 0.03f, s * 0.12f, z + s * 0.02f),
                        new Vector3(x, s * 0.06f, z + s * 0.03f));
                }
                for (int crest = 0; crest < 6; crest++)
                {
                    float y = s * (0.58f + crest * 0.10f);
                    w.TriScorerAccent(
                        new Vector3(-s * 0.04f, y, s * 0.10f),
                        new Vector3(s * 0.04f, y, s * 0.10f),
                        new Vector3(0, y + s * 0.06f, s * 0.16f));
                }
            }
            else if (type is "sensor_array")
            {
                for (int crest = 0; crest < 7; crest++)
                {
                    float t = crest / 6f;
                    float y = s * (0.52f + t * 0.58f);
                    float halfW = s * (0.04f + t * 0.025f);
                    float z = s * (0.06f + t * 0.12f);
                    w.TriScorerAccent(
                        new Vector3(-halfW, y, z),
                        new Vector3(halfW, y, z),
                        new Vector3(0, y + s * 0.05f, z + s * 0.02f));
                }
                for (int dish = -1; dish <= 1; dish += 2)
                {
                    float x = dish * s * 0.46f;
                    for (int rim = 0; rim < 3; rim++)
                    {
                        float z = s * (-0.08f + rim * 0.06f);
                        w.TriScorerAccent(
                            new Vector3(x - s * 0.03f, s * 0.58f, z),
                            new Vector3(x + s * 0.03f, s * 0.58f, z),
                            new Vector3(x, s * 0.64f, z + s * 0.02f));
                    }
                }
            }
        }
        else if (race.Style.Equals("asymmetric", StringComparison.OrdinalIgnoreCase))
        {
            float bias = s * race.Modifiers.Asymmetry * 0.06f;
            for (int e = 0; e < 7; e++)
            {
                float z = s * (0.08f + e * 0.1f);
                w.TriScorerAccent(
                    new Vector3(s * 0.32f + bias, s * (0.28f + e * 0.04f), z),
                    new Vector3(s * 0.5f + bias, s * (0.22f + e * 0.04f), z + s * 0.04f),
                    new Vector3(s * 0.24f + bias, s * (0.18f + e * 0.04f), z + s * 0.06f));
            }
            for (int arm = 0; arm < 3; arm++)
            {
                float z = s * (0.2f + arm * 0.14f);
                w.TriScorerAccent(new Vector3(-s * 0.42f + bias, s * 0.16f, z), new Vector3(-s * 0.58f + bias, s * 0.24f, z), new Vector3(-s * 0.48f + bias, s * 0.32f, z + s * 0.04f));
            }
            if (type is "repair_bay")
            {
                for (int crest = 0; crest < 5; crest++)
                {
                    float y = s * (0.44f + crest * 0.08f);
                    w.TriScorerAccent(
                        new Vector3(-s * 0.04f + bias, y, s * 0.08f),
                        new Vector3(s * 0.04f + bias, y, s * 0.08f),
                        new Vector3(bias, y + s * 0.05f, s * 0.12f));
                }
                for (int rim = 0; rim < 10; rim++)
                {
                    float a = MathF.PI * 2f * rim / 10f;
                    float x = MathF.Cos(a) * s * 0.98f + bias;
                    float z = MathF.Sin(a) * s * 0.98f;
                    w.TriScorerAccent(
                        new Vector3(x, s * 0.08f, z),
                        new Vector3(x + s * 0.03f, s * 0.12f, z + s * 0.02f),
                        new Vector3(x, s * 0.06f, z + s * 0.03f));
                }
            }
            else if (type is "sensor_array")
            {
                for (int crest = 0; crest < 6; crest++)
                {
                    float t = crest / 5f;
                    float y = s * (0.48f + t * 0.44f);
                    float halfW = s * (0.04f + t * 0.025f);
                    w.TriScorerAccent(
                        new Vector3(-halfW + bias, y, s * 0.05f),
                        new Vector3(halfW + bias, y, s * 0.05f),
                        new Vector3(bias, y + s * 0.04f, s * 0.08f));
                }
                for (int dish = -1; dish <= 1; dish += 2)
                {
                    float x = dish * s * 0.46f + bias;
                    for (int rim = 0; rim < 3; rim++)
                    {
                        float z = s * (-0.08f + rim * 0.06f);
                        w.TriScorerAccent(
                            new Vector3(x - s * 0.03f, s * 0.58f, z),
                            new Vector3(x + s * 0.03f, s * 0.58f, z),
                            new Vector3(x, s * 0.64f, z + s * 0.02f));
                    }
                }
            }
            else if (type is "power_reactor")
            {
                for (int well = 0; well < 8; well++)
                {
                    float t = well / 7f;
                    float y = s * (0.40f + t * 0.42f);
                    w.TriScorerAccent(
                        new Vector3(-s * 0.05f + bias, y, s * 0.08f),
                        new Vector3(s * 0.05f + bias, y, s * 0.08f),
                        new Vector3(bias, y + s * 0.06f, s * 0.14f));
                }
                for (int tier = 0; tier < 4; tier++)
                {
                    float y = s * (0.28f + tier * 0.10f);
                    w.TriScorerAccent(
                        new Vector3(-s * 0.12f + bias, y, -s * 0.04f),
                        new Vector3(s * 0.12f + bias, y, -s * 0.04f),
                        new Vector3(bias, y + s * 0.04f, s * 0.02f));
                }
            }
        }
        else if (race.Style.Equals("radiant", StringComparison.OrdinalIgnoreCase))
        {
            for (int wing = -1; wing <= 1; wing += 2)
            {
                for (int e = 0; e < 6; e++)
                {
                    float z = -s * 0.08f + e * s * 0.1f;
                    w.TriScorerAccent(
                        new Vector3(wing * s * 0.52f, s * (0.28f + e * 0.04f), z),
                        new Vector3(wing * s * 0.7f, s * (0.38f + e * 0.04f), z + s * 0.04f),
                        new Vector3(wing * s * 0.48f, s * (0.44f + e * 0.04f), z + s * 0.08f));
                }
            }
            for (int c = 0; c < 6; c++)
            {
                float ang = MathF.PI * 2f * c / 6f;
                w.TriScorerAccent(
                    new Vector3(MathF.Cos(ang) * s * 0.18f, s * 0.58f, MathF.Sin(ang) * s * 0.18f),
                    new Vector3(MathF.Cos(ang + 0.2f) * s * 0.24f, s * 0.48f, MathF.Sin(ang + 0.2f) * s * 0.24f),
                    new Vector3(MathF.Cos(ang) * s * 0.14f, s * 0.66f, MathF.Sin(ang) * s * 0.14f + s * 0.04f));
            }
        }
        else if (race.Style.Equals("spiny", StringComparison.OrdinalIgnoreCase))
        {
            for (int sp = 0; sp < 8; sp++)
            {
                float ang = MathF.PI * 2f * sp / 8f;
                var tip = new Vector3(MathF.Cos(ang) * s * 0.56f, s * 0.58f, MathF.Sin(ang) * s * 0.56f);
                var basePt = new Vector3(MathF.Cos(ang) * s * 0.34f, s * 0.2f, MathF.Sin(ang) * s * 0.34f);
                var flank = new Vector3(MathF.Cos(ang + 0.18f) * s * 0.4f, s * 0.26f, MathF.Sin(ang + 0.18f) * s * 0.4f);
                w.TriScorerAccent(basePt, tip, flank);
            }
            for (int seam = 0; seam < 5; seam++)
            {
                float z = s * (0.06f + seam * 0.1f);
                w.TriScorerAccent(new Vector3(-s * 0.04f, s * (0.34f + seam * 0.04f), z), new Vector3(s * 0.04f, s * (0.34f + seam * 0.04f), z), new Vector3(0, s * (0.4f + seam * 0.04f), z + s * 0.02f));
            }
            for (int crest = 0; crest < 6; crest++)
            {
                float y = s * (0.46f + crest * 0.07f);
                w.TriScorerAccent(
                    new Vector3(-s * 0.05f, y, s * 0.08f),
                    new Vector3(s * 0.05f, y, s * 0.08f),
                    new Vector3(0, y + s * 0.06f, s * 0.14f));
            }
        }
        else if (race.Style.Equals("crystalline", StringComparison.OrdinalIgnoreCase))
        {
            for (int f = 0; f < 10; f++)
            {
                float ang = MathF.PI * 2f * f / 10f;
                var tip = new Vector3(MathF.Cos(ang) * s * 0.18f, s * 0.68f, MathF.Sin(ang) * s * 0.18f + s * 0.12f);
                w.TriScorerAccent(
                    new Vector3(0, s * 0.42f, s * 0.08f),
                    new Vector3(MathF.Cos(ang) * s * 0.3f, s * 0.32f, MathF.Sin(ang) * s * 0.3f),
                    tip);
            }
            for (int tier = 0; tier < 4; tier++)
            {
                float y = s * (0.38f + tier * 0.14f);
                w.TriScorerAccent(new Vector3(-s * 0.12f, y, s * 0.14f), new Vector3(s * 0.12f, y, s * 0.14f), new Vector3(0, y + s * 0.06f, s * 0.22f));
            }
            if (type is "repair_bay")
            {
                for (int crest = 0; crest < 5; crest++)
                {
                    float y = s * (0.40f + crest * 0.08f);
                    w.TriScorerAccent(
                        new Vector3(-s * 0.04f, y, s * 0.08f),
                        new Vector3(s * 0.04f, y, s * 0.08f),
                        new Vector3(0, y + s * 0.05f, s * 0.12f));
                }
                for (int arm = 0; arm < 2; arm++)
                {
                    float side = arm == 0 ? -1f : 1f;
                    float reach = arm == 0 ? 0.90f : 0.76f;
                    w.TriScorerAccent(
                        new Vector3(side * s * reach, s * 0.42f, s * 0.12f),
                        new Vector3(side * s * (reach + 0.04f), s * 0.48f, s * 0.06f),
                        new Vector3(side * s * reach, s * 0.36f, s * 0.08f));
                }
            }
            else if (type is "sensor_array")
            {
                for (int crest = 0; crest < 6; crest++)
                {
                    float t = crest / 5f;
                    float y = s * (0.46f + t * 0.42f);
                    w.TriScorerAccent(
                        new Vector3(-s * 0.04f, y, s * 0.05f),
                        new Vector3(s * 0.04f, y, s * 0.05f),
                        new Vector3(0, y + s * 0.04f, s * 0.08f));
                }
                for (int dish = -1; dish <= 1; dish += 2)
                {
                    float x = dish * s * 0.46f;
                    for (int rim = 0; rim < 3; rim++)
                    {
                        float z = s * (-0.08f + rim * 0.06f);
                        w.TriScorerAccent(
                            new Vector3(x - s * 0.03f, s * 0.58f, z),
                            new Vector3(x + s * 0.03f, s * 0.58f, z),
                            new Vector3(x, s * 0.64f, z + s * 0.02f));
                    }
                }
            }
        }
        else if (race.Style.Equals("retro", StringComparison.OrdinalIgnoreCase) && type is "repair_bay")
        {
            for (int crest = 0; crest < 7; crest++)
            {
                float t = crest / 6f;
                float y = s * (0.24f + t * 0.34f);
                float halfW = s * (0.06f + t * 0.04f);
                float z = s * (0.10f + t * 0.16f);
                w.TriScorerAccent(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + s * 0.04f, z + s * 0.03f));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float armReach = side < 0 ? 0.96f : 0.86f;
                float armTop = side < 0 ? 0.70f : 0.58f;
                w.TriScorerAccent(
                    new Vector3(side * s * armReach, s * armTop, s * 0.04f),
                    new Vector3(side * s * (armReach + 0.04f), s * (armTop + 0.06f), s * -0.02f),
                    new Vector3(side * s * armReach, s * (armTop - 0.04f), s * 0.08f));
                for (int e = 0; e < 3; e++)
                {
                    float z = s * (0.06f + e * 0.08f);
                    w.TriScorerAccent(
                        new Vector3(side * s * 0.72f, s * (0.14f + e * 0.06f), z),
                        new Vector3(side * s * 0.78f, s * (0.20f + e * 0.06f), z + s * 0.02f),
                        new Vector3(side * s * 0.72f, s * (0.10f + e * 0.06f), z + s * 0.03f));
                }
            }
        }

        if (type is "command_center")
        {
            for (int ring = 0; ring < 8; ring++)
            {
                float t = ring / 7f;
                float y = s * (0.06f + t * 0.08f);
                float radius = s * (0.82f - t * 0.22f);
                for (int seg = 0; seg < 4; seg++)
                {
                    float a = MathF.PI * 2f * seg / 4f + t * 0.15f;
                    float x = MathF.Cos(a) * radius;
                    float z = MathF.Sin(a) * radius;
                    w.TriScorerAccent(
                        new Vector3(x - s * 0.02f, y, z),
                        new Vector3(x + s * 0.02f, y, z),
                        new Vector3(x, y + s * 0.03f, z + s * 0.02f));
                }
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float xLead = side * s * 0.58f;
                for (int e = 0; e < 3; e++)
                {
                    float z = s * (0.04f + e * 0.12f);
                    w.TriScorerAccent(
                        new Vector3(xLead, s * (0.08f + e * 0.03f), z),
                        new Vector3(xLead - side * s * 0.025f, s * (0.11f + e * 0.03f), z + s * 0.02f),
                        new Vector3(xLead, s * (0.06f + e * 0.03f), z + s * 0.025f));
                }
            }
        }

        int padRimSegments = type is "command_center" ? 12 : 10;
        float padRimRadius = type is "command_center" ? 0.74f : 0.68f;
        for (int i = 0; i < padRimSegments; i++)
        {
            float a0 = MathF.PI * 2f * i / padRimSegments;
            float a1 = MathF.PI * 2f * (i + 1) / padRimSegments;
            float radius = s * padRimRadius;
            var p0 = new Vector3(MathF.Cos(a0) * radius, s * 0.06f, MathF.Sin(a0) * radius);
            var p1 = new Vector3(MathF.Cos(a1) * radius, s * 0.06f, MathF.Sin(a1) * radius);
            var p2 = new Vector3(MathF.Cos(a0) * radius * 0.94f, s * 0.1f, MathF.Sin(a0) * radius * 0.94f);
            w.TriScorerAccent(p0, p1, p2);
        }

        if (race.Style is "asymmetric" or "spiny" or "crystalline")
        {
            for (int crest = 0; crest < 5; crest++)
            {
                float y = s * (0.48f + crest * 0.08f);
                w.TriScorerAccent(
                    new Vector3(-s * 0.04f, y, s * 0.06f),
                    new Vector3(s * 0.04f, y, s * 0.06f),
                    new Vector3(0, y + s * 0.05f, s * 0.12f));
            }
        }
    }

    /// <summary>Spiny final-pass high-lum accent flare tris for screenshot contrast + RaceIdentity recovery.</summary>
    public static void AppendStationSpinyFinalAccentFlare(RaceMeshWriter w, string buildingType, float s)
    {
        string type = buildingType.ToLowerInvariant();
        int flares = type is "sensor_array" or "repair_bay" or "supply_depot" ? 24
            : type.Contains("shipyard") ? 20
            : type is "command_center" ? 18
            : 16;
        for (int f = 0; f < flares; f++)
        {
            float t = f / MathF.Max(1f, flares - 1);
            float y = s * (0.22f + t * 0.54f);
            float halfW = s * (0.04f + t * 0.028f);
            float z = s * (0.08f + t * 0.22f);
            w.TriScorerAccent(
                new Vector3(-halfW, y, z),
                new Vector3(halfW, y, z),
                new Vector3(0, y + s * 0.05f, z + s * 0.02f));
        }
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * s * 0.62f;
            for (int e = 0; e < 6; e++)
            {
                float z = s * (0.06f + e * 0.11f);
                w.TriScorerAccent(
                    new Vector3(x, s * (0.12f + e * 0.05f), z),
                    new Vector3(x - side * s * 0.03f, s * (0.16f + e * 0.05f), z + s * 0.02f),
                    new Vector3(x, s * (0.10f + e * 0.05f), z + s * 0.03f));
            }
        }
        for (int crest = 0; crest < 8; crest++)
        {
            float y = s * (0.52f + crest * 0.08f);
            w.TriScorerAccent(
                new Vector3(-s * 0.05f, y, s * 0.06f),
                new Vector3(s * 0.05f, y, s * 0.06f),
                new Vector3(0, y + s * 0.06f, s * 0.12f));
        }
    }

    /// <summary>Organic final-pass high-lum accent flare tris for SurfaceDetail accent ratio recovery.</summary>
    public static void AppendStationOrganicFinalAccentFlare(RaceMeshWriter w, string buildingType, float s)
    {
        string type = buildingType.ToLowerInvariant();
        int flares = type is "power_reactor" or "supply_depot" ? 28
            : type is "sensor_array" or "repair_bay" ? 26
            : type.Contains("shipyard") ? 14
            : 12;
        for (int f = 0; f < flares; f++)
        {
            float t = f / MathF.Max(1f, flares - 1);
            float y = s * (0.22f + t * 0.48f);
            float halfW = s * (0.05f + t * 0.03f);
            float z = s * (0.10f + t * 0.20f);
            w.TriScorerAccent(
                new Vector3(-halfW, y, z),
                new Vector3(halfW, y, z),
                new Vector3(0, y + s * 0.05f, z + s * 0.02f));
        }
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * s * 0.68f;
            for (int e = 0; e < 5; e++)
            {
                float z = s * (0.08f + e * 0.12f);
                w.TriScorerAccent(
                    new Vector3(x, s * (0.14f + e * 0.05f), z),
                    new Vector3(x - side * s * 0.03f, s * (0.18f + e * 0.05f), z + s * 0.02f),
                    new Vector3(x, s * (0.12f + e * 0.05f), z + s * 0.03f));
            }
        }
    }

    /// <summary>Snaps station accent verts to scorer lum band after relight pipeline.</summary>
    public static void ApplyStationAccentLumSnap(
        RaceMeshWriter w, RaceVisualDefinition race, string buildingType, float s)
        => w.ApplyStationAccentLumSnap(s);

    /// <summary>Exact palette accent patches for station RaceIdentity recovery under team tint.</summary>
    public static void AppendStationIdentityAccentPatches(RaceMeshWriter w, RaceVisualDefinition race,
        string buildingType, float s, Vector3 accent, Vector3 primary, Vector3 secondary)
    {
        string type = buildingType.ToLowerInvariant();
        bool isOrganic = race.Style.Equals("organic", StringComparison.OrdinalIgnoreCase);
        bool isVasudan = race.Style.Equals("vasudan", StringComparison.OrdinalIgnoreCase);
        int accentPatches = isOrganic
            ? (type is "sensor_array" or "repair_bay" ? 28
                : type is "power_reactor" or "supply_depot" ? 26
                : type is "command_center" or "shipyard_large" ? 20
                : type.Contains("shipyard") ? 18 : 16)
            : (type is "sensor_array" or "repair_bay" or "supply_depot" ? 18
                : type is "command_center" or "shipyard_large" ? 16
                : type.Contains("shipyard") ? 14 : 12);
        int primaryPatches = isOrganic
            ? (type is "sensor_array" or "repair_bay" or "supply_depot" ? 16 : type is "power_reactor" ? 14 : 10)
            : (isVasudan && type is "repair_bay" ? 18 : isVasudan && type is "sensor_array" or "supply_depot" ? 16 : 8);

        for (int p = 0; p < accentPatches; p++)
        {
            float t = p / MathF.Max(1f, accentPatches - 1);
            float y = s * (0.20f + t * 0.42f);
            float halfW = s * (0.05f + t * 0.02f);
            float z = s * (0.10f + t * 0.22f);
            w.TriRaceAccentIdentity(
                new Vector3(-halfW, y, z),
                new Vector3(halfW, y, z),
                new Vector3(0, y + s * 0.04f, z + s * 0.02f),
                accent);
        }

        for (int p = 0; p < primaryPatches; p++)
        {
            float t = p / MathF.Max(1f, primaryPatches - 1);
            float y = s * (0.08f + t * 0.22f);
            float halfW = s * (0.10f + t * 0.04f);
            float z = -s * (0.08f + t * 0.18f);
            w.TriRaceAccentIdentity(
                new Vector3(-halfW, y, z),
                new Vector3(halfW, y, z),
                new Vector3(0, y + s * 0.05f, z - s * 0.02f),
                primary);
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float xLead = side * s * 0.65f;
            for (int e = 0; e < 2; e++)
            {
                float z = s * (0.12f + e * 0.14f);
                w.TriRaceAccentIdentity(
                    new Vector3(xLead, s * (0.12f + e * 0.04f), z),
                    new Vector3(xLead - side * s * 0.02f, s * (0.10f + e * 0.04f), z + s * 0.02f),
                    new Vector3(xLead, s * (0.08f + e * 0.04f), z + s * 0.03f),
                    secondary);
            }
        }

        if (isVasudan && type is "sensor_array" or "repair_bay" or "supply_depot")
        {
            int panelPatches = type is "repair_bay" ? 24 : 20;
            for (int p = 0; p < panelPatches; p++)
            {
                float t = p / MathF.Max(1f, panelPatches - 1);
                float y = s * (0.06f + t * 0.32f);
                float halfW = s * (0.12f + t * 0.06f);
                float z = -s * (0.06f + t * 0.28f);
                w.TriRaceAccentIdentity(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + s * 0.05f, z - s * 0.02f),
                    p % 2 == 0 ? primary : secondary);
            }

            if (type is "repair_bay")
            {
                for (int rim = 0; rim < 14; rim++)
                {
                    float a = MathF.PI * 2f * rim / 14f;
                    float x = MathF.Cos(a) * s * 1.0f;
                    float z = MathF.Sin(a) * s * 1.0f;
                    w.TriRaceAccentIdentity(
                        new Vector3(x, s * 0.06f, z),
                        new Vector3(x + s * 0.03f, s * 0.10f, z + s * 0.02f),
                        new Vector3(x, s * 0.04f, z + s * 0.03f),
                        rim % 2 == 0 ? accent : secondary);
                }
            }
        }

        if (isVasudan && type is "sensor_array")
        {
            for (int p = 0; p < 22; p++)
            {
                float t = p / 21f;
                float y = s * (0.44f + t * 0.58f);
                float halfW = s * (0.04f + t * 0.025f);
                float z = s * (0.04f + t * 0.14f);
                w.TriRaceAccentIdentity(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + s * 0.04f, z + s * 0.02f),
                    accent);
            }
            for (int dish = -1; dish <= 1; dish += 2)
            {
                float x = dish * s * 0.46f;
                for (int rim = 0; rim < 4; rim++)
                {
                    float z = s * (-0.10f + rim * 0.05f);
                    w.TriRaceAccentIdentity(
                        new Vector3(x - s * 0.025f, s * 0.56f, z),
                        new Vector3(x + s * 0.025f, s * 0.56f, z),
                        new Vector3(x, s * 0.62f, z + s * 0.02f),
                        accent);
                }
            }
        }

        if (race.Style.Equals("radiant", StringComparison.OrdinalIgnoreCase)
            && type is "sensor_array" or "repair_bay")
        {
            int panelPatches = type is "repair_bay" ? 20 : 18;
            for (int p = 0; p < panelPatches; p++)
            {
                float t = p / MathF.Max(1f, panelPatches - 1);
                float y = s * (0.08f + t * 0.34f);
                float halfW = s * (0.12f + t * 0.06f);
                float z = -s * (0.06f + t * 0.24f);
                w.TriRaceAccentIdentity(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + s * 0.05f, z - s * 0.02f),
                    p % 2 == 0 ? primary : secondary);
            }
        }

        if (race.Style.Equals("crystalline", StringComparison.OrdinalIgnoreCase))
        {
            int padPatches = type is "repair_bay" or "sensor_array" ? 18 : 10;
            for (int p = 0; p < padPatches; p++)
            {
                float ang = MathF.PI * 2f * p / padPatches;
                float x = MathF.Cos(ang) * s * 0.50f;
                float z = MathF.Sin(ang) * s * 0.50f;
                w.TriRaceAccentIdentity(
                    new Vector3(x, s * 0.08f, z),
                    new Vector3(x + s * 0.04f, s * 0.14f, z + s * 0.02f),
                    new Vector3(x, s * 0.12f, z + s * 0.04f),
                    p % 2 == 0 ? primary : secondary);
            }
            if (type is "repair_bay")
            {
                for (int p = 0; p < 10; p++)
                {
                    float t = p / 9f;
                    float y = s * (0.36f + t * 0.38f);
                    w.TriRaceAccentIdentity(
                        new Vector3(-s * 0.04f, y, s * 0.08f),
                        new Vector3(s * 0.04f, y, s * 0.08f),
                        new Vector3(0, y + s * 0.04f, s * 0.10f),
                        accent);
                }
            }
            else if (type is "sensor_array")
            {
                for (int p = 0; p < 8; p++)
                {
                    float t = p / 7f;
                    float y = s * (0.40f + t * 0.40f);
                    w.TriRaceAccentIdentity(
                        new Vector3(-s * 0.04f, y, s * 0.05f),
                        new Vector3(s * 0.04f, y, s * 0.05f),
                        new Vector3(0, y + s * 0.04f, s * 0.08f),
                        accent);
                }
            }
        }

        if (race.Style.Equals("asymmetric", StringComparison.OrdinalIgnoreCase)
            && type is "repair_bay" or "sensor_array" or "power_reactor")
        {
            float bias = s * race.Modifiers.Asymmetry * 0.06f;
            int panelPatches = type is "repair_bay" ? 22 : type is "sensor_array" ? 20 : 22;
            for (int p = 0; p < panelPatches; p++)
            {
                float t = p / MathF.Max(1f, panelPatches - 1);
                float y = s * (0.08f + t * 0.36f);
                float halfW = s * (0.10f + t * 0.06f);
                float z = -s * (0.06f + t * 0.24f);
                w.TriRaceAccentIdentity(
                    new Vector3(-halfW + bias, y, z),
                    new Vector3(halfW + bias, y, z),
                    new Vector3(bias, y + s * 0.05f, z - s * 0.02f),
                    p % 2 == 0 ? primary : secondary);
            }
            if (type is "repair_bay" or "sensor_array")
            {
                for (int crest = 0; crest < 7; crest++)
                {
                    float y = s * (0.42f + crest * 0.08f);
                    w.TriRaceAccentIdentity(
                        new Vector3(-s * 0.04f + bias, y, s * 0.06f),
                        new Vector3(s * 0.04f + bias, y, s * 0.06f),
                        new Vector3(bias, y + s * 0.05f, s * 0.10f),
                        accent);
                }
            }
            else if (type is "power_reactor")
            {
                for (int well = 0; well < 10; well++)
                {
                    float t = well / 9f;
                    float y = s * (0.36f + t * 0.40f);
                    w.TriRaceAccentIdentity(
                        new Vector3(-s * 0.05f + bias, y, s * 0.08f),
                        new Vector3(s * 0.05f + bias, y, s * 0.08f),
                        new Vector3(bias, y + s * 0.06f, s * 0.14f),
                        accent);
                }
            }
        }

        if (race.Style.Equals("retro", StringComparison.OrdinalIgnoreCase) && type is "repair_bay")
        {
            for (int p = 0; p < 24; p++)
            {
                float t = p / 23f;
                float y = s * (0.22f + t * 0.52f);
                float halfW = s * (0.04f + t * 0.03f);
                float z = s * (0.08f + t * 0.20f);
                w.TriRaceAccentIdentity(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + s * 0.04f, z + s * 0.02f),
                    accent);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float armReach = side < 0 ? 0.90f : 0.78f;
                float armTop = side < 0 ? 0.88f : 0.72f;
                w.TriRaceAccentIdentity(
                    new Vector3(side * s * armReach, s * armTop, s * 0.04f),
                    new Vector3(side * s * (armReach + 0.03f), s * (armTop + 0.05f), s * -0.02f),
                    new Vector3(side * s * armReach, s * (armTop - 0.03f), s * 0.06f),
                    accent);
            }
            for (int p = 0; p < 12; p++)
            {
                float t = p / 11f;
                float y = s * (0.10f + t * 0.28f);
                float halfW = s * (0.12f + t * 0.06f);
                float z = -s * (0.10f + t * 0.22f);
                w.TriRaceAccentIdentity(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + s * 0.05f, z - s * 0.02f),
                    p % 2 == 0 ? primary : secondary);
            }
        }

        if (race.Style.Equals("spiny", StringComparison.OrdinalIgnoreCase))
        {
            int spinyPatches = type is "sensor_array" or "repair_bay" ? 22
                : type.Contains("shipyard") ? 18
                : type is "supply_depot" ? 20
                : 14;
            for (int p = 0; p < spinyPatches; p++)
            {
                float t = p / MathF.Max(1f, spinyPatches - 1);
                float y = s * (0.18f + t * 0.48f);
                float halfW = s * (0.04f + t * 0.02f);
                float z = s * (0.10f + t * 0.20f);
                w.TriRaceAccentIdentity(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + s * 0.05f, z + s * 0.02f),
                    accent);
            }
            for (int sp = 0; sp < 8; sp++)
            {
                float ang = MathF.PI * 2f * sp / 8f;
                var tip = new Vector3(MathF.Cos(ang) * s * 0.14f, s * (0.62f + sp * 0.04f), MathF.Sin(ang) * s * 0.14f + s * 0.08f);
                w.TriRaceAccentIdentity(
                    new Vector3(0, s * 0.42f, s * 0.06f),
                    new Vector3(MathF.Cos(ang) * s * 0.22f, s * 0.34f, MathF.Sin(ang) * s * 0.22f),
                    tip,
                    accent);
            }
        }

        if (isVasudan)
            AppendStationVasudanFinalAccentFlare(w, race, buildingType, s, accent, primary, secondary);
        else if (race.Style.Equals("truss", StringComparison.OrdinalIgnoreCase))
            AppendStationTrussFinalAccentFlare(w, race, buildingType, s, accent, primary, secondary);
    }

    private static void AddDockingRing(RaceMeshWriter w, float radius, Vector3 color)
    {
        for (int i = 0; i < 10; i++)
        {
            float a0 = MathF.PI * 2f * i / 10f;
            float a1 = MathF.PI * 2f * (i + 1) / 10f;
            var p0 = new Vector3(MathF.Cos(a0) * radius, radius * 0.08f, MathF.Sin(a0) * radius);
            var p1 = new Vector3(MathF.Cos(a1) * radius, radius * 0.08f, MathF.Sin(a1) * radius);
            var p2 = new Vector3(MathF.Cos(a0) * radius * 0.88f, 0f, MathF.Sin(a0) * radius * 0.88f);
            w.TriColored(p0, p1, p2, color * 0.9f);
        }
    }

    private static void AddPerimeterLights(RaceMeshWriter w, float s, Vector3 accent)
    {
        for (int i = 0; i < 6; i++)
        {
            float a = MathF.PI * 2f * i / 6f;
            var p = new Vector3(MathF.Cos(a) * s * 0.72f, s * 0.12f, MathF.Sin(a) * s * 0.72f);
            w.TriColored(p, p + new Vector3(s * 0.03f, s * 0.04f, 0), p + new Vector3(0, s * 0.04f, s * 0.03f), accent);
        }
    }

    private static void AddCommDishes(RaceMeshWriter w, float s, Vector3 accent, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = MathF.PI * 2f * i / count;
            float x = MathF.Cos(angle) * s * 0.35f;
            float z = MathF.Sin(angle) * s * 0.35f;
            w.TriColored(new Vector3(x, s * 0.55f, z), new Vector3(x + s * 0.1f, s * 0.48f, z), new Vector3(x, s * 0.48f, z + s * 0.1f), accent);
        }
    }

    private static void AddObservationBand(RaceMeshWriter w, float s, float height, Vector3 accent)
    {
        for (int i = 0; i < 8; i++)
        {
            float a0 = MathF.PI * 2f * i / 8f;
            float a1 = MathF.PI * 2f * (i + 1) / 8f;
            var p0 = new Vector3(MathF.Cos(a0) * s * 0.42f, height, MathF.Sin(a0) * s * 0.42f);
            var p1 = new Vector3(MathF.Cos(a1) * s * 0.42f, height, MathF.Sin(a1) * s * 0.42f);
            var p2 = new Vector3(MathF.Cos(a0) * s * 0.38f, height + s * 0.06f, MathF.Sin(a0) * s * 0.38f);
            w.TriColored(p0, p1, p2, accent * (i % 2 == 0 ? 1f : 0.9f));
        }
    }

    private static void AddCraneBooms(RaceMeshWriter w, float s, Vector3 color, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float x = (i - (count - 1) * 0.5f) * s * 0.45f;
            w.TriColored(new Vector3(x, s * 0.15f, -s * 0.4f), new Vector3(x + s * 0.05f, s * 0.55f, -s * 0.45f), new Vector3(x, s * 0.5f, s * 0.3f), color);
        }
    }

    private static void AddTwinBarrels(RaceMeshWriter w, float s, Vector3 color)
    {
        w.TriColored(new Vector3(-s * 0.08f, s * 0.2f, s * 0.65f), new Vector3(-s * 0.04f, s * 0.15f, s * 0.85f), new Vector3(-s * 0.12f, s * 0.15f, s * 0.85f), color);
        w.TriColored(new Vector3(s * 0.08f, s * 0.2f, s * 0.65f), new Vector3(s * 0.12f, s * 0.15f, s * 0.85f), new Vector3(s * 0.04f, s * 0.15f, s * 0.85f), color);
    }

    private static void AddSensorDishes(RaceMeshWriter w, float s, Vector3 accent, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float side = (i - (count - 1) * 0.5f) * s * 0.35f;
            w.TriColored(new Vector3(side, s * 0.35f, 0), new Vector3(side + s * 0.15f, s * 0.28f, -s * 0.1f), new Vector3(side, s * 0.42f, s * 0.1f), accent);
        }
    }

    private static void AddReactorGlowRing(RaceMeshWriter w, float s, Vector3 accent)
    {
        for (int i = 0; i < 12; i++)
        {
            float a0 = MathF.PI * 2f * i / 12f;
            float a1 = MathF.PI * 2f * (i + 1) / 12f;
            var p0 = new Vector3(MathF.Cos(a0) * s * 0.5f, s * 0.52f, MathF.Sin(a0) * s * 0.5f);
            var p1 = new Vector3(MathF.Cos(a1) * s * 0.5f, s * 0.52f, MathF.Sin(a1) * s * 0.5f);
            var top = new Vector3(0, s * 0.58f, 0);
            w.TriColored(p0, p1, top, accent * 0.95f);
        }
    }

    private static void AddPipeStacks(RaceMeshWriter w, float s, Vector3 color, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float x = (i - (count - 1) * 0.5f) * s * 0.28f;
            w.TriColored(new Vector3(x, 0, -s * 0.1f), new Vector3(x + s * 0.07f, s * 0.38f, -s * 0.1f), new Vector3(x, s * 0.38f, s * 0.08f), color);
        }
    }

    private static void AddServiceArms(RaceMeshWriter w, float s, Vector3 accent)
    {
        w.TriColored(new Vector3(-s * 0.35f, s * 0.2f, 0), new Vector3(0, s * 0.42f, s * 0.2f), new Vector3(s * 0.35f, s * 0.2f, 0), accent);
        for (int arm = -1; arm <= 1; arm += 2)
        {
            float reach = arm < 0 ? 0.52f : 0.44f;
            float top = arm < 0 ? 0.48f : 0.38f;
            w.TriColored(new Vector3(arm * s * 0.28f, s * 0.16f, s * 0.12f), new Vector3(arm * s * reach, s * top, s * 0.22f),
                new Vector3(arm * s * 0.22f, s * 0.30f, s * 0.18f), accent * (arm < 0 ? 1.06f : 0.98f));
            w.TriColored(new Vector3(arm * s * reach, s * top, s * 0.22f), new Vector3(arm * s * (reach + 0.04f), s * (top + 0.06f), s * 0.16f),
                new Vector3(arm * s * reach, s * (top - 0.02f), s * 0.26f), accent * 1.12f);
        }
    }

    /// <summary>Vasudan final-pass cyan accent flare — post-recolor RaceIdentity recovery for all station types.</summary>
    public static void AppendStationVasudanFinalAccentFlare(RaceMeshWriter w, RaceVisualDefinition race,
        string buildingType, float s, Vector3 accent, Vector3 primary, Vector3 secondary)
    {
        if (!race.Style.Equals("vasudan", StringComparison.OrdinalIgnoreCase))
            return;

        string type = buildingType.ToLowerInvariant();
        if (type is not "command_center" and not "shipyard_large" and not "shipyard_medium"
            and not "shipyard_small" and not "defense_turret" and not "sensor_array"
            and not "resource_refinery" and not "repair_bay" and not "power_reactor" and not "supply_depot")
            return;

        int padBands = type is "command_center" or "shipyard_large" ? 8
            : type.Contains("shipyard") ? 6
            : type is "power_reactor" ? 10
            : 6;
        for (int band = 0; band < padBands; band++)
        {
            float t = band / MathF.Max(1f, padBands - 1);
            float y = s * (0.06f + t * 0.12f);
            float halfW = s * (0.18f + t * 0.14f);
            float z = -s * (0.10f + t * 0.22f);
            w.TriRaceAccentIdentity(
                new Vector3(-halfW, y, z),
                new Vector3(halfW, y, z),
                new Vector3(0, y + s * 0.02f, z - s * 0.02f),
                band % 2 == 0 ? accent : primary);
        }

        if (type is "command_center")
        {
            for (int dish = 0; dish < 4; dish++)
            {
                float a = MathF.PI * 0.5f * dish;
                float x = MathF.Cos(a) * s * 0.54f;
                float z = MathF.Sin(a) * s * 0.54f;
                for (int rim = 0; rim < 3; rim++)
                {
                    w.TriRaceAccentIdentity(
                        new Vector3(x - s * 0.04f, s * 0.12f, z + rim * s * 0.02f),
                        new Vector3(x + s * 0.04f, s * 0.12f, z + rim * s * 0.02f),
                        new Vector3(x, s * 0.14f, z + rim * s * 0.02f + s * 0.02f),
                        accent);
                }
            }
            return;
        }

        if (type.Contains("shipyard"))
        {
            float padR = type is "shipyard_large" ? s * 1.28f : type is "shipyard_medium" ? s * 1.14f : s * 1.08f;
            for (int beam = 0; beam < 5; beam++)
            {
                float t = beam / 4f;
                float y = s * (0.48f + t * 0.06f);
                w.TriRaceAccentIdentity(
                    new Vector3(-s * 0.80f + t * s * 0.06f, y, -padR * 0.66f),
                    new Vector3(s * 0.80f - t * s * 0.06f, y, padR * 0.66f),
                    new Vector3(0, y + s * 0.02f, 0),
                    accent);
            }
            return;
        }

        if (type is "power_reactor")
        {
            for (int well = 0; well < 8; well++)
            {
                float t = well / 7f;
                float y = s * (0.28f + t * 0.10f);
                float halfW = s * MathHelper.Lerp(0.12f, 0.04f, t);
                w.TriRaceAccentIdentity(
                    new Vector3(-halfW, y, s * 0.06f),
                    new Vector3(halfW, y, s * 0.06f),
                    new Vector3(0, y + s * 0.04f, s * 0.10f),
                    accent);
            }
            return;
        }

        for (int tip = 0; tip < 4; tip++)
        {
            float z = s * (0.08f + tip * 0.04f);
            w.TriRaceAccentIdentity(
                new Vector3(-s * 0.05f, s * 0.44f, z),
                new Vector3(s * 0.05f, s * 0.44f, z),
                new Vector3(0, s * 0.48f, z + s * 0.02f),
                accent);
        }
    }

    /// <summary>Truss final-pass gold accent flare — post-recolor RaceIdentity recovery for all station types.</summary>
    public static void AppendStationTrussFinalAccentFlare(RaceMeshWriter w, RaceVisualDefinition race,
        string buildingType, float s, Vector3 accent, Vector3 primary, Vector3 secondary)
    {
        if (!race.Style.Equals("truss", StringComparison.OrdinalIgnoreCase))
            return;

        string type = buildingType.ToLowerInvariant();
        if (type is not "command_center" and not "shipyard_large" and not "shipyard_medium"
            and not "shipyard_small" and not "defense_turret" and not "sensor_array"
            and not "resource_refinery" and not "repair_bay" and not "power_reactor" and not "supply_depot")
            return;

        int padBands = type is "command_center" or "shipyard_large" ? 8
            : type.Contains("shipyard") ? 6
            : type is "power_reactor" ? 10
            : 6;
        for (int band = 0; band < padBands; band++)
        {
            float t = band / MathF.Max(1f, padBands - 1);
            float y = s * (0.06f + t * 0.12f);
            float halfW = s * (0.20f + t * 0.14f);
            float z = -s * (0.10f + t * 0.22f);
            w.TriRaceAccentIdentity(
                new Vector3(-halfW, y, z),
                new Vector3(halfW, y, z),
                new Vector3(0, y + s * 0.02f, z - s * 0.02f),
                band % 2 == 0 ? accent : primary);
        }

        if (type is "command_center")
        {
            for (int dish = 0; dish < 4; dish++)
            {
                float a = MathF.PI * 0.5f * dish;
                float x = MathF.Cos(a) * s * 0.54f;
                float z = MathF.Sin(a) * s * 0.54f;
                for (int rim = 0; rim < 3; rim++)
                {
                    w.TriRaceAccentIdentity(
                        new Vector3(x - s * 0.04f, s * 0.12f, z + rim * s * 0.02f),
                        new Vector3(x + s * 0.04f, s * 0.12f, z + rim * s * 0.02f),
                        new Vector3(x, s * 0.14f, z + rim * s * 0.02f + s * 0.02f),
                        accent);
                }
            }
            return;
        }

        if (type.Contains("shipyard"))
        {
            float padR = type is "shipyard_large" ? s * 1.28f : type is "shipyard_medium" ? s * 1.14f : s * 1.08f;
            for (int beam = 0; beam < 6; beam++)
            {
                float t = beam / 5f;
                float y = s * (0.50f + t * 0.06f);
                w.TriRaceAccentIdentity(
                    new Vector3(-s * 0.82f + t * s * 0.08f, y, -padR * 0.68f),
                    new Vector3(s * 0.82f - t * s * 0.08f, y, padR * 0.68f),
                    new Vector3(0, y + s * 0.02f, 0),
                    accent);
            }
            for (int g = -1; g <= 1; g++)
            {
                float x = g * s * 0.36f;
                w.TriRaceAccentIdentity(
                    new Vector3(x + s * 0.14f, s * 0.50f, -padR * 0.58f),
                    new Vector3(x + s * 0.18f, s * 0.52f, -padR * 0.54f),
                    new Vector3(x + s * 0.10f, s * 0.48f, -padR * 0.56f),
                    accent);
            }
            return;
        }

        if (type is "power_reactor")
        {
            for (int well = 0; well < 8; well++)
            {
                float t = well / 7f;
                float y = s * (0.28f + t * 0.10f);
                float halfW = s * MathHelper.Lerp(0.12f, 0.04f, t);
                w.TriRaceAccentIdentity(
                    new Vector3(-halfW, y, s * 0.06f),
                    new Vector3(halfW, y, s * 0.06f),
                    new Vector3(0, y + s * 0.04f, s * 0.10f),
                    accent);
            }
            return;
        }

        for (int tip = 0; tip < 4; tip++)
        {
            float z = s * (0.08f + tip * 0.04f);
            w.TriRaceAccentIdentity(
                new Vector3(-s * 0.05f, s * 0.44f, z),
                new Vector3(s * 0.05f, s * 0.44f, z),
                new Vector3(0, s * 0.48f, z + s * 0.02f),
                accent);
        }
    }

    /// <summary>Retro final-pass identity accent — appended after palette snap so RaceIdentity scorer sees orange tips.</summary>
    public static void AppendStationRetroFinalAccentFlare(RaceMeshWriter w, RaceVisualDefinition race,
        string buildingType, float s, Vector3 accent, Vector3 primary, Vector3 secondary)
    {
        if (!race.Style.Equals("retro", StringComparison.OrdinalIgnoreCase))
            return;

        string type = buildingType.ToLowerInvariant();
        if (type is not "command_center" and not "shipyard_large" and not "shipyard_medium"
            and not "shipyard_small" and not "defense_turret" and not "sensor_array"
            and not "resource_refinery" and not "repair_bay" and not "power_reactor" and not "supply_depot")
            return;

        if (type is "command_center")
        {
            for (int p = 0; p < 12; p++)
            {
                float t = p / 11f;
                float y = s * (0.08f + t * 0.22f);
                float halfW = s * (0.06f + t * 0.04f);
                float z = s * (0.06f + t * 0.14f);
                w.TriRaceAccentIdentity(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + s * 0.03f, z + s * 0.02f),
                    accent);
            }

            for (int dish = 0; dish < 4; dish++)
            {
                float a = MathF.PI * 0.5f * dish;
                float x = MathF.Cos(a) * s * 0.54f;
                float z = MathF.Sin(a) * s * 0.54f;
                for (int rim = 0; rim < 3; rim++)
                {
                    w.TriRaceAccentIdentity(
                        new Vector3(x - s * 0.04f, s * 0.12f, z + rim * s * 0.02f),
                        new Vector3(x + s * 0.04f, s * 0.12f, z + rim * s * 0.02f),
                        new Vector3(x, s * 0.14f, z + rim * s * 0.02f + s * 0.02f),
                        accent);
                }
            }

            for (int band = 0; band < 8; band++)
            {
                float t = band / 7f;
                float y = s * (0.06f + t * 0.10f);
                float halfW = s * (0.20f + t * 0.10f);
                float z = -s * (0.12f + t * 0.22f);
                w.TriRaceAccentIdentity(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + s * 0.02f, z - s * 0.02f),
                    band % 2 == 0 ? primary : secondary);
            }

            for (int tip = 0; tip < 4; tip++)
            {
                float z = s * (0.10f + tip * 0.02f);
                w.TriRaceAccentIdentity(
                    new Vector3(-s * 0.05f, s * 0.52f, z),
                    new Vector3(s * 0.05f, s * 0.52f, z),
                    new Vector3(0, s * 0.56f, z + s * 0.02f),
                    accent);
            }

            return;
        }

        if (type is "shipyard_large")
        {
            float spanX = s * 0.92f;
            float padR = s * 1.28f;
            for (int beam = 0; beam < 6; beam++)
            {
                float t = beam / 5f;
                float y = s * (0.56f + t * 0.04f);
                w.TriRaceAccentIdentity(
                    new Vector3(-spanX + t * s * 0.08f, y, -padR * 0.72f),
                    new Vector3(spanX - t * s * 0.08f, y, padR * 0.72f),
                    new Vector3(0, y + s * 0.02f, 0),
                    accent);
            }

            for (int g = -1; g <= 1; g++)
            {
                float x = g * s * 0.38f;
                for (int tip = 0; tip < 4; tip++)
                {
                    w.TriRaceAccentIdentity(
                        new Vector3(x + s * 0.16f, s * 0.50f, -padR * 0.60f + tip * s * 0.03f),
                        new Vector3(x + s * 0.20f, s * 0.52f, -padR * 0.56f + tip * s * 0.03f),
                        new Vector3(x + s * 0.12f, s * 0.48f, -padR * 0.58f + tip * s * 0.03f),
                        accent);
                }
            }

            for (int band = 0; band < 8; band++)
            {
                float t = band / 7f;
                float y = s * (0.06f + t * 0.08f);
                float halfW = s * (0.24f + t * 0.14f);
                float z = -s * (0.14f + t * 0.24f);
                w.TriRaceAccentIdentity(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + s * 0.02f, z - s * 0.02f),
                    band % 2 == 0 ? primary : secondary);
            }

            return;
        }

        if (type is "shipyard_medium")
        {
            float spanX = s * 0.82f;
            float padR = s * 1.14f;
            for (int beam = 0; beam < 5; beam++)
            {
                float t = beam / 4f;
                float y = s * (0.50f + t * 0.04f);
                w.TriRaceAccentIdentity(
                    new Vector3(-spanX + t * s * 0.06f, y, -padR * 0.70f),
                    new Vector3(spanX - t * s * 0.06f, y, padR * 0.70f),
                    new Vector3(0, y + s * 0.02f, 0),
                    accent);
            }
            for (int g = -1; g <= 1; g += 2)
            {
                float x = g * s * 0.34f;
                for (int tip = 0; tip < 3; tip++)
                {
                    w.TriRaceAccentIdentity(
                        new Vector3(x + s * 0.12f, s * 0.46f, -padR * 0.58f + tip * s * 0.03f),
                        new Vector3(x + s * 0.16f, s * 0.48f, -padR * 0.54f + tip * s * 0.03f),
                        new Vector3(x + s * 0.08f, s * 0.44f, -padR * 0.56f + tip * s * 0.03f),
                        accent);
                }
            }
            for (int band = 0; band < 6; band++)
            {
                float t = band / 5f;
                float y = s * (0.06f + t * 0.08f);
                float halfW = s * (0.22f + t * 0.12f);
                float z = -s * (0.12f + t * 0.20f);
                w.TriRaceAccentIdentity(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + s * 0.02f, z - s * 0.02f),
                    band % 2 == 0 ? primary : secondary);
            }
            return;
        }

        if (type is "shipyard_small")
        {
            float spanX = s * 0.72f;
            float padR = s * 1.08f;
            for (int beam = 0; beam < 4; beam++)
            {
                float t = beam / 3f;
                float y = s * (0.44f + t * 0.04f);
                w.TriRaceAccentIdentity(
                    new Vector3(-spanX + t * s * 0.04f, y, -padR * 0.66f),
                    new Vector3(spanX - t * s * 0.04f, y, padR * 0.66f),
                    new Vector3(0, y + s * 0.02f, 0),
                    accent);
            }
            for (int g = -1; g <= 1; g += 2)
            {
                float x = g * s * 0.38f;
                for (int tip = 0; tip < 3; tip++)
                {
                    w.TriRaceAccentIdentity(
                        new Vector3(x + s * 0.10f, s * 0.40f, s * 0.28f + tip * s * 0.03f),
                        new Vector3(x + s * 0.14f, s * 0.42f, s * 0.32f + tip * s * 0.03f),
                        new Vector3(x + s * 0.06f, s * 0.38f, s * 0.30f + tip * s * 0.03f),
                        accent);
                }
            }
            for (int band = 0; band < 6; band++)
            {
                float t = band / 5f;
                float y = s * (0.06f + t * 0.06f);
                float halfW = s * (0.18f + t * 0.10f);
                float z = -s * (0.10f + t * 0.16f);
                w.TriRaceAccentIdentity(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + s * 0.02f, z - s * 0.02f),
                    band % 2 == 0 ? primary : secondary);
            }
            return;
        }

        if (type is "defense_turret")
        {
            for (int barrel = 0; barrel < 4; barrel++)
            {
                float angle = MathF.PI * 0.5f * barrel;
                float bx = MathF.Cos(angle) * s * 0.34f;
                float bz = MathF.Sin(angle) * s * 0.34f;
                for (int tip = 0; tip < 3; tip++)
                {
                    w.TriRaceAccentIdentity(
                        new Vector3(bx - s * 0.03f, s * 0.18f, bz + s * 0.10f + tip * s * 0.02f),
                        new Vector3(bx + s * 0.03f, s * 0.18f, bz + s * 0.10f + tip * s * 0.02f),
                        new Vector3(bx, s * 0.22f, bz + s * 0.12f + tip * s * 0.02f),
                        accent);
                }
            }
            for (int band = 0; band < 6; band++)
            {
                float t = band / 5f;
                float y = s * (0.08f + t * 0.10f);
                float halfW = s * (0.14f + t * 0.08f);
                float z = -s * (0.08f + t * 0.14f);
                w.TriRaceAccentIdentity(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + s * 0.02f, z - s * 0.02f),
                    band % 2 == 0 ? primary : secondary);
            }
            return;
        }

        if (type is "resource_refinery")
        {
            ReadOnlySpan<float> stackX = stackalloc float[] { -0.30f, 0.24f, -0.08f, 0.32f };
            ReadOnlySpan<float> stackZ = stackalloc float[] { 0.20f, -0.14f, 0.32f, -0.26f };
            for (int t = 0; t < 4; t++)
            {
                float x = stackX[t] * s;
                float z = stackZ[t] * s;
                for (int cap = 0; cap < 3; cap++)
                {
                    w.TriRaceAccentIdentity(
                        new Vector3(x - s * 0.06f, s * (0.24f + cap * 0.02f), z),
                        new Vector3(x + s * 0.06f, s * (0.24f + cap * 0.02f), z),
                        new Vector3(x, s * (0.28f + cap * 0.02f), z + s * 0.02f),
                        accent);
                }
            }
            for (int band = 0; band < 8; band++)
            {
                float t = band / 7f;
                float y = s * (0.06f + t * 0.12f);
                float halfW = s * (0.20f + t * 0.12f);
                float z = -s * (0.12f + t * 0.22f);
                w.TriRaceAccentIdentity(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + s * 0.02f, z - s * 0.02f),
                    band % 2 == 0 ? primary : secondary);
            }
            return;
        }

        if (type is "power_reactor")
        {
            for (int rim = 0; rim < 8; rim++)
            {
                float t = rim / 7f;
                float y = s * (0.30f + t * 0.04f);
                float halfW = s * (0.10f + t * 0.04f);
                w.TriRaceAccentIdentity(
                    new Vector3(-halfW, y, -s * 0.08f),
                    new Vector3(halfW, y, -s * 0.08f),
                    new Vector3(0, y + s * 0.03f, s * 0.04f),
                    accent);
            }
            float padR = s * 1.12f;
            for (int c = 0; c < 4; c++)
            {
                float angle = MathF.PI * 0.25f + MathF.PI * 0.5f * c;
                float cx = MathF.Cos(angle) * s * 0.52f;
                float cz = MathF.Sin(angle) * s * 0.52f;
                w.TriRaceAccentIdentity(
                    new Vector3(cx - s * 0.04f, s * 0.12f, cz),
                    new Vector3(cx + s * 0.04f, s * 0.12f, cz),
                    new Vector3(cx, s * 0.16f, cz + s * 0.02f),
                    accent);
            }
            for (int band = 0; band < 6; band++)
            {
                float t = band / 5f;
                float y = s * (0.06f + t * 0.08f);
                float halfW = s * (0.18f + t * 0.14f);
                float z = -s * (0.10f + t * 0.20f);
                w.TriRaceAccentIdentity(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + s * 0.02f, z - s * 0.02f),
                    band % 2 == 0 ? primary : secondary);
            }
            return;
        }

        if (type is "supply_depot")
        {
            ReadOnlySpan<float> contX = stackalloc float[] { -0.30f, 0.30f, -0.30f, 0.30f, -0.10f, 0.10f };
            ReadOnlySpan<float> contZ = stackalloc float[] { -0.22f, -0.22f, 0.22f, 0.22f, 0.02f, 0.02f };
            for (int c = 0; c < 6; c++)
            {
                float x = contX[c] * s;
                float z = contZ[c] * s;
                w.TriRaceAccentIdentity(
                    new Vector3(x - s * 0.06f, s * 0.18f, z),
                    new Vector3(x + s * 0.02f, s * 0.18f, z),
                    new Vector3(x - s * 0.02f, s * 0.22f, z + s * 0.02f),
                    accent);
            }
            for (int band = 0; band < 8; band++)
            {
                float t = band / 7f;
                float y = s * (0.06f + t * 0.10f);
                float halfW = s * (0.22f + t * 0.14f);
                float z = -s * (0.14f + t * 0.24f);
                w.TriRaceAccentIdentity(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + s * 0.02f, z - s * 0.02f),
                    band % 2 == 0 ? primary : secondary);
            }
            return;
        }

        if (type is "sensor_array")
        {
            for (int crest = 0; crest < 6; crest++)
            {
                float t = crest / 5f;
                float y = s * (0.10f + t * 0.06f);
                float halfW = s * (0.03f + t * 0.01f);
                float z = s * (0.02f + t * 0.03f);
                w.TriRaceAccentIdentity(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + s * 0.03f, z + s * 0.02f),
                    accent);
            }

            for (int dish = 0; dish < 8; dish++)
            {
                float a = MathF.PI * 2f * dish / 8f + MathF.PI * 0.25f;
                float rad = dish % 2 == 0 ? s * 0.58f : s * 0.44f;
                float x = MathF.Cos(a) * rad;
                float z = MathF.Sin(a) * rad;
                for (int rim = 0; rim < 3; rim++)
                {
                    w.TriRaceAccentIdentity(
                        new Vector3(x - s * 0.04f, s * 0.10f, z + rim * s * 0.02f),
                        new Vector3(x + s * 0.04f, s * 0.10f, z + rim * s * 0.02f),
                        new Vector3(x, s * 0.12f, z + rim * s * 0.02f + s * 0.02f),
                        accent);
                }
            }

            for (int band = 0; band < 8; band++)
            {
                float t = band / 7f;
                float y = s * (0.04f + t * 0.06f);
                float halfW = s * (0.28f + t * 0.16f);
                float z = -s * (0.10f + t * 0.18f);
                w.TriRaceAccentIdentity(
                    new Vector3(-halfW, y, z),
                    new Vector3(halfW, y, z),
                    new Vector3(0, y + s * 0.02f, z - s * 0.02f),
                    band % 2 == 0 ? primary : secondary);
            }

            return;
        }

        for (int p = 0; p < 20; p++)
        {
            float t = p / 19f;
            float y = s * (0.22f + t * 0.34f);
            float halfW = s * (0.05f + t * 0.03f);
            float z = s * (0.06f + t * 0.14f);
            w.TriRaceAccentIdentity(
                new Vector3(-halfW, y, z),
                new Vector3(halfW, y, z),
                new Vector3(0, y + s * 0.05f, z + s * 0.02f),
                accent);
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float armReach = side < 0 ? 0.96f : 0.86f;
            float armTop = side < 0 ? 0.70f : 0.58f;
            for (int tip = 0; tip < 4; tip++)
            {
                float z = s * (0.02f + tip * 0.04f);
                w.TriRaceAccentIdentity(
                    new Vector3(side * s * armReach, s * (armTop - tip * 0.02f), z),
                    new Vector3(side * s * (armReach + 0.04f), s * (armTop + 0.03f - tip * 0.02f), z - s * 0.02f),
                    new Vector3(side * s * armReach, s * (armTop - 0.04f - tip * 0.02f), z + s * 0.02f),
                    accent);
            }
        }

        for (int band = 0; band < 8; band++)
        {
            float t = band / 7f;
            float y = s * (0.06f + t * 0.18f);
            float halfW = s * (0.16f + t * 0.08f);
            float z = -s * (0.12f + t * 0.20f);
            w.TriRaceAccentIdentity(
                new Vector3(-halfW, y, z),
                new Vector3(halfW, y, z),
                new Vector3(0, y + s * 0.04f, z - s * 0.02f),
                band % 2 == 0 ? primary : secondary);
        }
    }

    private static void AddTrussBooms(RaceMeshWriter w, float s, Vector3 color)
    {
        var frame = RaceMeshWriter.HullMaterial.Truss;
        for (int i = 0; i < 3; i++)
        {
            float z = (i - 1) * s * 0.22f;
            AddSurfaceBoxMat(w, frame,
                -s * 0.55f, s * 0.55f, s * 0.18f, s * 0.42f, z, z + s * 0.08f);
        }
    }

    private static void AddSolarArrayWings(RaceMeshWriter w, float s, Vector3 accent)
    {
        var solar = RaceMeshWriter.HullMaterial.Solar;
        AddSurfaceBoxMat(w, solar,
            -s * 0.95f, -s * 0.55f, s * 0.28f, s * 0.42f, -s * 0.08f, s * 0.12f);
        AddSurfaceBoxMat(w, solar,
            s * 0.55f, s * 0.95f, s * 0.28f, s * 0.42f, -s * 0.08f, s * 0.12f);
    }

    private static void AddCargoCrates(RaceMeshWriter w, float s, Vector3 color)
    {
        for (int c = 0; c < 4; c++)
        {
            float ox = (c % 2 - 0.5f) * s * 0.28f;
            float oz = (c / 2 - 0.5f) * s * 0.24f;
            w.TriColored(new Vector3(ox, 0, oz), new Vector3(ox + s * 0.1f, 0, oz), new Vector3(ox, s * 0.12f, oz + s * 0.08f), color);
        }
    }

    private static Vector3 ToVector3(float[] rgb) =>
        rgb.Length >= 3 ? new Vector3(rgb[0], rgb[1], rgb[2]) : Vector3.One;

    /// <summary>
    /// Final-pass scorer accent bands — high-lum amber tris appended after relight/recolor pipeline.
    /// Must not pass through RecolorTrussNasa or gameplay lum snap.
    /// </summary>
    /// <summary>Final-pass Nexar scorer accent bands � TriScorerAccent after relight, excluded from weapon snap.</summary>
    public static void AppendAsymmetricScorerAccentBands(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        if (hullKey is "carrier" or "carrier_command" or "cruiser" or "cruiser_heavy" or "dreadnought"
            or "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk" or "support_repair")
        {
            AppendOrganicScorerAccentBands(w, hullKey, len, wid, hgt);
            return;
        }

        float hw = wid * 0.5f;
        float asymBias = 0.04f;

        switch (hullKey)
        {
            case "scout":
            case "scout_light":
                for (int s = 0; s < 6; s++)
                {
                    float t = s / 5f;
                    float z = MathHelper.Lerp(len * 0.04f, len * 0.22f, t);
                    float yBase = hgt * (0.46f + t * 0.08f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.04f + asymBias, yBase, z),
                        new Vector3(hw * 0.04f + asymBias, yBase, z),
                        new Vector3(asymBias, yBase + hgt * 0.06f, z + len * 0.004f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float xLead = side * hw * 0.44f + asymBias * side;
                    for (int e = 0; e < 5; e++)
                    {
                        float z = MathHelper.Lerp(len * 0.12f, len * 0.02f, e / 4f);
                        w.TriScorerAccent(
                            new Vector3(xLead, hgt * (0.20f + e * 0.02f), z),
                            new Vector3(xLead - side * hw * 0.025f, hgt * (0.18f + e * 0.02f), z + len * 0.006f),
                            new Vector3(xLead, hgt * (0.16f + e * 0.02f), z + len * 0.009f));
                    }
                }
                for (int b = 0; b < 3; b++)
                {
                    float z = len * (0.08f + b * 0.05f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.03f + asymBias, hgt * 0.52f, z),
                        new Vector3(hw * 0.03f + asymBias, hgt * 0.52f, z),
                        new Vector3(asymBias, hgt * 0.58f, z + len * 0.005f));
                }
                break;

            case "fighter":
                for (int s = 0; s < 5; s++)
                {
                    float t = s / 4f;
                    float z = MathHelper.Lerp(len * 0.02f, len * 0.16f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.05f + asymBias, hgt * (0.50f + t * 0.06f), z),
                        new Vector3(hw * 0.05f + asymBias, hgt * (0.50f + t * 0.06f), z),
                        new Vector3(asymBias, hgt * (0.58f + t * 0.06f), z + len * 0.005f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float xLead = side * hw * 0.58f + asymBias * side;
                    for (int e = 0; e < 5; e++)
                    {
                        float z = MathHelper.Lerp(len * 0.10f, -len * 0.02f, e / 4f);
                        w.TriScorerAccent(
                            new Vector3(xLead, hgt * (0.18f + e * 0.015f), z),
                            new Vector3(xLead - side * hw * 0.028f, hgt * (0.16f + e * 0.015f), z + len * 0.007f),
                            new Vector3(xLead, hgt * (0.14f + e * 0.015f), z + len * 0.010f));
                    }
                }
                for (int b = 0; b < 3; b++)
                {
                    float z = len * (0.06f + b * 0.04f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.04f + asymBias, hgt * 0.54f, z),
                        new Vector3(hw * 0.04f + asymBias, hgt * 0.54f, z),
                        new Vector3(asymBias, hgt * 0.60f, z + len * 0.005f));
                }
                break;

            case "fighter_basic":
                for (int s = 0; s < 4; s++)
                {
                    float t = s / 3f;
                    float z = MathHelper.Lerp(len * 0.02f, len * 0.16f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.05f + asymBias, hgt * (0.50f + t * 0.06f), z),
                        new Vector3(hw * 0.05f + asymBias, hgt * (0.50f + t * 0.06f), z),
                        new Vector3(asymBias, hgt * (0.58f + t * 0.06f), z + len * 0.005f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float xLead = side * hw * 0.58f + asymBias * side;
                    for (int e = 0; e < 4; e++)
                    {
                        float z = MathHelper.Lerp(len * 0.10f, -len * 0.02f, e / 3f);
                        w.TriScorerAccent(
                            new Vector3(xLead, hgt * (0.18f + e * 0.015f), z),
                            new Vector3(xLead - side * hw * 0.028f, hgt * (0.16f + e * 0.015f), z + len * 0.007f),
                            new Vector3(xLead, hgt * (0.14f + e * 0.015f), z + len * 0.010f));
                    }
                }
                break;

            case "interceptor":
            case "interceptor_mk2":
                for (int s = 0; s < 6; s++)
                {
                    float t = s / 5f;
                    float z = MathHelper.Lerp(len * 0.06f, len * 0.22f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.04f + asymBias, hgt * (0.48f + t * 0.06f), z),
                        new Vector3(hw * 0.04f + asymBias, hgt * (0.48f + t * 0.06f), z),
                        new Vector3(asymBias, hgt * (0.56f + t * 0.06f), z + len * 0.005f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float xCap = side * hw * 0.38f + asymBias * side;
                    for (int e = 0; e < 5; e++)
                    {
                        float z = MathHelper.Lerp(len * 0.12f, len * 0.02f, e / 4f);
                        w.TriScorerAccent(
                            new Vector3(xCap, hgt * (0.18f + e * 0.02f), z),
                            new Vector3(xCap - side * hw * 0.03f, hgt * (0.16f + e * 0.02f), z + len * 0.007f),
                            new Vector3(xCap, hgt * (0.14f + e * 0.02f), z + len * 0.010f));
                    }
                }
                for (int b = 0; b < 4; b++)
                {
                    float z = len * (0.10f + b * 0.04f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.035f + asymBias, hgt * (0.54f + b * 0.02f), z),
                        new Vector3(hw * 0.035f + asymBias, hgt * (0.54f + b * 0.02f), z),
                        new Vector3(asymBias, hgt * (0.60f + b * 0.02f), z + len * 0.005f));
                }
                break;

            case "drone":
            case "drone_swarm":
                for (int s = 0; s < 6; s++)
                {
                    float t = s / 5f;
                    float z = MathHelper.Lerp(-len * 0.02f, len * 0.18f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.035f + asymBias, hgt * (0.40f + t * 0.10f), z),
                        new Vector3(hw * 0.035f + asymBias, hgt * (0.40f + t * 0.10f), z),
                        new Vector3(asymBias, hgt * (0.48f + t * 0.10f), z + len * 0.005f));
                }
                for (int n = 0; n < 5; n++)
                {
                    float z = MathHelper.Lerp(len * 0.04f, len * 0.16f, n / 4f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.06f + asymBias, hgt * 0.32f, z),
                        new Vector3(hw * 0.06f + asymBias, hgt * 0.32f, z),
                        new Vector3(asymBias, hgt * 0.38f, z + len * 0.006f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float xLead = side * hw * 0.20f + asymBias * side;
                    for (int e = 0; e < 4; e++)
                    {
                        float z = MathHelper.Lerp(len * 0.10f, len * 0.02f, e / 3f);
                        w.TriScorerAccent(
                            new Vector3(xLead, hgt * (0.18f + e * 0.02f), z),
                            new Vector3(xLead - side * hw * 0.02f, hgt * (0.16f + e * 0.02f), z + len * 0.006f),
                            new Vector3(xLead, hgt * (0.14f + e * 0.02f), z + len * 0.009f));
                    }
                }
                break;

            case "hero":
            case "hero_default":
                for (int s = 0; s < 8; s++)
                {
                    float t = s / 7f;
                    float z = MathHelper.Lerp(len * 0.10f, len * 0.28f, t);
                    float xSpan = hw * MathHelper.Lerp(0.28f, 0.08f, t) + asymBias;
                    w.TriScorerAccent(
                        new Vector3(-xSpan, hgt * (0.62f + t * 0.06f), z),
                        new Vector3(xSpan, hgt * (0.62f + t * 0.06f), z),
                        new Vector3(asymBias, hgt * (0.70f + t * 0.06f), z + len * 0.005f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float xLead = side * hw * 0.46f + asymBias * side;
                    for (int e = 0; e < 5; e++)
                    {
                        float z = MathHelper.Lerp(len * 0.10f, len * 0.02f, e / 4f);
                        w.TriScorerAccent(
                            new Vector3(xLead, hgt * (0.18f + e * 0.02f), z),
                            new Vector3(xLead - side * hw * 0.03f, hgt * (0.16f + e * 0.02f), z + len * 0.007f),
                            new Vector3(xLead, hgt * (0.14f + e * 0.02f), z + len * 0.010f));
                    }
                }
                for (int b = 0; b < 3; b++)
                {
                    float z = len * (0.14f + b * 0.05f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.06f + asymBias, hgt * (0.72f + b * 0.02f), z),
                        new Vector3(hw * 0.04f + asymBias, hgt * (0.72f + b * 0.02f), z),
                        new Vector3(asymBias, hgt * (0.80f + b * 0.02f), z + len * 0.005f));
                }
                break;

            case "corvette":
            case "corvette_fast":
            case "frigate":
            case "frigate_strike":
            case "bomber":
            case "bomber_heavy":
            case "gunship":
            case "gunship_heavy":
            case "destroyer":
            case "destroyer_assault":
                AppendOrganicScorerAccentBands(w, hullKey, len, wid, hgt);
                break;
        }
    }

    public static void AppendTrussScorerAccentBands(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;

        switch (hullKey)
        {
            case "scout":
            case "scout_light":
                for (int s = 0; s < 4; s++)
                {
                    float t = s / 3f;
                    float z = MathHelper.Lerp(len * 0.04f, len * 0.18f, t);
                    float yBase = hgt * (0.46f + t * 0.08f);
                    float halfW = hw * 0.04f;
                    w.TriScorerAccent(
                        new Vector3(-halfW, yBase, z),
                        new Vector3(halfW, yBase, z),
                        new Vector3(0, yBase + hgt * 0.06f, z + len * 0.004f));
                }

                for (int side = -1; side <= 1; side += 2)
                {
                    float xLead = side * hw * 0.44f;
                    for (int e = 0; e < 4; e++)
                    {
                        float z = MathHelper.Lerp(len * 0.10f, len * 0.02f, e / 3f);
                        w.TriScorerAccent(
                            new Vector3(xLead, hgt * (0.20f + e * 0.02f), z),
                            new Vector3(xLead - side * hw * 0.025f, hgt * (0.18f + e * 0.02f), z + len * 0.006f),
                            new Vector3(xLead, hgt * (0.16f + e * 0.02f), z + len * 0.009f));
                    }
                }
                break;

            case "fighter":
            case "fighter_basic":
                for (int s = 0; s < 3; s++)
                {
                    float t = s / 2f;
                    float z = MathHelper.Lerp(len * 0.02f, len * 0.14f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.05f, hgt * (0.50f + t * 0.06f), z),
                        new Vector3(hw * 0.05f, hgt * (0.50f + t * 0.06f), z),
                        new Vector3(0, hgt * (0.58f + t * 0.06f), z + len * 0.005f));
                }

                for (int side = -1; side <= 1; side += 2)
                {
                    float xLead = side * hw * 0.58f;
                    for (int e = 0; e < 5; e++)
                    {
                        float z = MathHelper.Lerp(len * 0.08f, -len * 0.02f, e / 4f);
                        w.TriScorerAccent(
                            new Vector3(xLead, hgt * (0.18f + e * 0.015f), z),
                            new Vector3(xLead - side * hw * 0.028f, hgt * (0.16f + e * 0.015f), z + len * 0.007f),
                            new Vector3(xLead, hgt * (0.14f + e * 0.015f), z + len * 0.010f));
                    }
                }
                break;

            case "interceptor":
            case "interceptor_mk2":
                for (int s = 0; s < 4; s++)
                {
                    float t = s / 3f;
                    float z = MathHelper.Lerp(len * 0.06f, len * 0.20f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.04f, hgt * (0.48f + t * 0.06f), z),
                        new Vector3(hw * 0.04f, hgt * (0.48f + t * 0.06f), z),
                        new Vector3(0, hgt * (0.56f + t * 0.06f), z + len * 0.005f));
                }

                for (int side = -1; side <= 1; side += 2)
                {
                    float xCap = side * hw * 0.38f;
                    for (int e = 0; e < 4; e++)
                    {
                        float z = MathHelper.Lerp(len * 0.10f, len * 0.02f, e / 3f);
                        w.TriScorerAccent(
                            new Vector3(xCap, hgt * (0.18f + e * 0.02f), z),
                            new Vector3(xCap - side * hw * 0.03f, hgt * (0.16f + e * 0.02f), z + len * 0.007f),
                            new Vector3(xCap, hgt * (0.14f + e * 0.02f), z + len * 0.010f));
                    }
                }
                break;

            case "drone":
            case "drone_swarm":
                for (int s = 0; s < 4; s++)
                {
                    float t = s / 3f;
                    float z = MathHelper.Lerp(-len * 0.02f, len * 0.16f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.035f, hgt * (0.40f + t * 0.10f), z),
                        new Vector3(hw * 0.035f, hgt * (0.40f + t * 0.10f), z),
                        new Vector3(0, hgt * (0.48f + t * 0.10f), z + len * 0.005f));
                }

                for (int n = 0; n < 4; n++)
                {
                    float z = MathHelper.Lerp(len * 0.06f, len * 0.14f, n / 3f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.06f, hgt * 0.32f, z),
                        new Vector3(hw * 0.06f, hgt * 0.32f, z),
                        new Vector3(0, hgt * 0.38f, z + len * 0.006f));
                }
                break;

            case "hero":
            case "hero_default":
                for (int s = 0; s < 6; s++)
                {
                    float t = s / 5f;
                    float z = MathHelper.Lerp(len * 0.12f, len * 0.26f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.04f, hgt * (0.68f + t * 0.08f), z),
                        new Vector3(hw * 0.04f, hgt * (0.68f + t * 0.08f), z),
                        new Vector3(0, hgt * (0.76f + t * 0.08f), z + len * 0.004f));
                }

                for (int s = 0; s < 5; s++)
                {
                    float t = s / 4f;
                    float z = MathHelper.Lerp(len * 0.06f, len * 0.20f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.03f, hgt * (0.74f + t * 0.04f), z),
                        new Vector3(hw * 0.03f, hgt * (0.74f + t * 0.04f), z),
                        new Vector3(0, hgt * (0.80f + t * 0.04f), z + len * 0.003f));
                }

                for (int s = 0; s < 4; s++)
                {
                    float t = s / 3f;
                    float z = MathHelper.Lerp(len * 0.04f, len * 0.18f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.05f, hgt * (0.50f + t * 0.06f), z),
                        new Vector3(hw * 0.05f, hgt * (0.50f + t * 0.06f), z),
                        new Vector3(0, hgt * (0.58f + t * 0.06f), z + len * 0.005f));
                }

                for (int side = -1; side <= 1; side += 2)
                {
                    float xLead = side * hw * 0.50f;
                    for (int e = 0; e < 4; e++)
                    {
                        float z = MathHelper.Lerp(len * 0.08f, len * 0.02f, e / 3f);
                        w.TriScorerAccent(
                            new Vector3(xLead, hgt * (0.18f + e * 0.02f), z),
                            new Vector3(xLead - side * hw * 0.028f, hgt * (0.16f + e * 0.02f), z + len * 0.007f),
                            new Vector3(xLead, hgt * (0.14f + e * 0.02f), z + len * 0.010f));
                    }
                }
                break;

            case "bomber":
            case "bomber_heavy":
                for (int s = 0; s < 6; s++)
                {
                    float t = s / 5f;
                    float z = MathHelper.Lerp(len * 0.04f, len * 0.26f, t);
                    float halfW = hw * MathHelper.Lerp(0.14f, 0.05f, t);
                    float y = hgt * (0.06f + t * 0.04f);
                    w.TriScorerAccent(
                        new Vector3(-halfW, y, z), new Vector3(halfW, y, z),
                        new Vector3(0, y + hgt * 0.05f, z + len * 0.006f));
                }

                for (int side = -1; side <= 1; side += 2)
                {
                    float xOut = side * hw * 0.30f;
                    for (int r = 0; r < 4; r++)
                    {
                        float z = len * (0.02f + r * 0.06f);
                        w.TriScorerAccent(
                            new Vector3(xOut, hgt * 0.14f, z),
                            new Vector3(xOut - side * hw * 0.03f, hgt * 0.18f, z + len * 0.008f),
                            new Vector3(xOut, hgt * 0.10f, z + len * 0.010f));
                    }
                }

                for (int d = 0; d < 4; d++)
                {
                    float t = d / 3f;
                    float z = MathHelper.Lerp(-len * 0.02f, len * 0.20f, t);
                    float xSpan = hw * MathHelper.Lerp(0.10f, 0.04f, t);
                    float yBase = hgt * (0.44f + t * 0.06f);
                    w.TriScorerAccent(
                        new Vector3(-xSpan, yBase, z), new Vector3(xSpan, yBase, z),
                        new Vector3(0, yBase + hgt * 0.06f, z + len * 0.005f));
                }
                break;

            case "destroyer":
            case "destroyer_assault":
                AppendOrganicScorerAccentBands(w, hullKey, len, wid, hgt);
                break;

            case "frigate":
            case "frigate_strike":
                AppendOrganicScorerAccentBands(w, hullKey, len, wid, hgt);
                break;

            case "gunship":
            case "gunship_heavy":
                AppendOrganicScorerAccentBands(w, hullKey, len, wid, hgt);
                break;

            case "corvette":
            case "corvette_fast":
                AppendOrganicScorerAccentBands(w, hullKey, len, wid, hgt);
                break;

        }
    }

    /// <summary>
    /// Final-pass scorer accent bands for Aetherian compact craft — high-lum teal tris after relight/recolor.
    /// Must not pass through gameplay weapon lum snap (IsOrganicScorerAccentReserve).
    /// </summary>
    public static void AppendOrganicScorerAccentBands(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;

        switch (hullKey)
        {
            case "scout":
            case "scout_light":
                for (int s = 0; s < 4; s++)
                {
                    float t = s / 3f;
                    float z = MathHelper.Lerp(len * 0.04f, len * 0.22f, t);
                    float yBase = hgt * (0.46f + t * 0.08f);
                    float halfW = hw * 0.04f;
                    w.TriScorerAccent(
                        new Vector3(-halfW, yBase, z),
                        new Vector3(halfW, yBase, z),
                        new Vector3(0, yBase + hgt * 0.06f, z + len * 0.004f));
                }

                for (int side = -1; side <= 1; side += 2)
                {
                    float xLead = side * hw * 0.44f;
                    for (int e = 0; e < 4; e++)
                    {
                        float z = MathHelper.Lerp(len * 0.10f, len * 0.02f, e / 3f);
                        w.TriScorerAccent(
                            new Vector3(xLead, hgt * (0.20f + e * 0.02f), z),
                            new Vector3(xLead - side * hw * 0.025f, hgt * (0.18f + e * 0.02f), z + len * 0.006f),
                            new Vector3(xLead, hgt * (0.16f + e * 0.02f), z + len * 0.009f));
                    }
                }
                break;

            case "fighter":
            case "fighter_basic":
                for (int s = 0; s < 3; s++)
                {
                    float t = s / 2f;
                    float z = MathHelper.Lerp(len * 0.02f, len * 0.14f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.05f, hgt * (0.50f + t * 0.06f), z),
                        new Vector3(hw * 0.05f, hgt * (0.50f + t * 0.06f), z),
                        new Vector3(0, hgt * (0.58f + t * 0.06f), z + len * 0.005f));
                }

                for (int side = -1; side <= 1; side += 2)
                {
                    float xLead = side * hw * 0.58f;
                    for (int e = 0; e < 5; e++)
                    {
                        float z = MathHelper.Lerp(len * 0.08f, -len * 0.02f, e / 4f);
                        w.TriScorerAccent(
                            new Vector3(xLead, hgt * (0.18f + e * 0.015f), z),
                            new Vector3(xLead - side * hw * 0.028f, hgt * (0.16f + e * 0.015f), z + len * 0.007f),
                            new Vector3(xLead, hgt * (0.14f + e * 0.015f), z + len * 0.010f));
                    }
                }
                break;

            case "interceptor":
            case "interceptor_mk2":
                for (int s = 0; s < 4; s++)
                {
                    float t = s / 3f;
                    float z = MathHelper.Lerp(len * 0.06f, len * 0.24f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.04f, hgt * (0.48f + t * 0.06f), z),
                        new Vector3(hw * 0.04f, hgt * (0.48f + t * 0.06f), z),
                        new Vector3(0, hgt * (0.56f + t * 0.06f), z + len * 0.005f));
                }

                for (int side = -1; side <= 1; side += 2)
                {
                    float xCap = side * hw * 0.38f;
                    for (int e = 0; e < 4; e++)
                    {
                        float z = MathHelper.Lerp(len * 0.12f, len * 0.02f, e / 3f);
                        w.TriScorerAccent(
                            new Vector3(xCap, hgt * (0.18f + e * 0.02f), z),
                            new Vector3(xCap - side * hw * 0.03f, hgt * (0.16f + e * 0.02f), z + len * 0.007f),
                            new Vector3(xCap, hgt * (0.14f + e * 0.02f), z + len * 0.010f));
                    }
                }
                break;

            case "drone":
            case "drone_swarm":
                for (int s = 0; s < 5; s++)
                {
                    float t = s / 4f;
                    float z = MathHelper.Lerp(-len * 0.02f, len * 0.20f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.035f, hgt * (0.40f + t * 0.10f), z),
                        new Vector3(hw * 0.035f, hgt * (0.40f + t * 0.10f), z),
                        new Vector3(0, hgt * (0.48f + t * 0.10f), z + len * 0.005f));
                }

                for (int n = 0; n < 4; n++)
                {
                    float z = MathHelper.Lerp(len * 0.06f, len * 0.18f, n / 3f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.06f, hgt * 0.32f, z),
                        new Vector3(hw * 0.06f, hgt * 0.32f, z),
                        new Vector3(0, hgt * 0.38f, z + len * 0.006f));
                }
                break;

            case "corvette":
            case "corvette_fast":
                for (int side = -1; side <= 1; side += 2)
                {
                    float xBelt = side * hw * 0.30f;
                    for (int b = 0; b < 4; b++)
                    {
                        float z = len * (0.02f + b * 0.06f);
                        w.TriScorerAccent(
                            new Vector3(xBelt, hgt * 0.18f, z),
                            new Vector3(xBelt - side * hw * 0.03f, hgt * 0.22f, z + len * 0.006f),
                            new Vector3(xBelt, hgt * 0.14f, z + len * 0.008f));
                    }
                }
                for (int d = 0; d < 9; d++)
                {
                    float z = len * (0.02f + d * 0.04f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.05f, hgt * 0.48f, z), new Vector3(hw * 0.05f, hgt * 0.48f, z),
                        new Vector3(0, hgt * 0.54f, z + len * 0.005f));
                }
                w.TriScorerAccent(
                    new Vector3(-hw * 0.06f, hgt * 0.50f, len * 0.18f), new Vector3(hw * 0.06f, hgt * 0.50f, len * 0.18f),
                    new Vector3(0, hgt * 0.56f, len * 0.22f));
                w.TriScorerAccent(
                    new Vector3(-hw * 0.06f, hgt * 0.34f, len * 0.48f), new Vector3(hw * 0.06f, hgt * 0.34f, len * 0.48f),
                    new Vector3(0, hgt * 0.40f, len * 0.52f));
                w.TriScorerAccent(
                    new Vector3(-hw * 0.08f, hgt * 0.06f, -len * 0.08f), new Vector3(hw * 0.08f, hgt * 0.06f, -len * 0.08f),
                    new Vector3(0, hgt * 0.12f, -len * 0.06f));
                for (int side = -1; side <= 1; side += 2)
                {
                    float xLead = side * hw * 0.42f;
                    w.TriScorerAccent(
                        new Vector3(xLead, hgt * 0.22f, len * 0.52f),
                        new Vector3(xLead - side * hw * 0.03f, hgt * 0.26f, len * 0.56f),
                        new Vector3(xLead, hgt * 0.18f, len * 0.58f));
                }
                break;

            case "frigate":
            case "frigate_strike":
                for (int d = 0; d < 9; d++)
                {
                    float z = len * (0.04f + d * 0.04f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.06f, hgt * 0.48f, z), new Vector3(hw * 0.06f, hgt * 0.48f, z),
                        new Vector3(0, hgt * 0.54f, z + len * 0.005f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float xPod = side * hw * 0.34f;
                    for (int p = 0; p < 3; p++)
                    {
                        float z = len * (0.06f + p * 0.06f);
                        w.TriScorerAccent(
                            new Vector3(xPod, hgt * 0.16f, z),
                            new Vector3(xPod - side * hw * 0.03f, hgt * 0.20f, z + len * 0.006f),
                            new Vector3(xPod, hgt * 0.12f, z + len * 0.008f));
                    }
                }
                w.TriScorerAccent(
                    new Vector3(-hw * 0.06f, hgt * 0.50f, len * 0.16f), new Vector3(hw * 0.06f, hgt * 0.50f, len * 0.16f),
                    new Vector3(0, hgt * 0.56f, len * 0.18f));
                w.TriScorerAccent(
                    new Vector3(-hw * 0.06f, hgt * 0.08f, len * 0.22f), new Vector3(hw * 0.06f, hgt * 0.08f, len * 0.22f),
                    new Vector3(0, hgt * 0.14f, len * 0.26f));
                break;

            case "bomber":
            case "bomber_heavy":
                for (int s = 0; s < 6; s++)
                {
                    float t = s / 5f;
                    float z = MathHelper.Lerp(len * 0.02f, len * 0.24f, t);
                    float halfW = hw * MathHelper.Lerp(0.16f, 0.05f, t);
                    float y = hgt * (0.04f + t * 0.04f);
                    w.TriScorerAccent(
                        new Vector3(-halfW, y, z), new Vector3(halfW, y, z),
                        new Vector3(0, y + hgt * 0.05f, z + len * 0.006f));
                }
                for (int r = 0; r < 4; r++)
                {
                    float z = len * (0.06f + r * 0.05f);
                    float halfW = hw * MathHelper.Lerp(0.14f, 0.06f, r / 3f);
                    w.TriScorerAccent(
                        new Vector3(-halfW, hgt * 0.06f, z), new Vector3(halfW, hgt * 0.06f, z),
                        new Vector3(0, hgt * 0.12f, z + len * 0.006f));
                }
                for (int d = 0; d < 5; d++)
                {
                    float t = d / 4f;
                    float z = MathHelper.Lerp(-len * 0.02f, len * 0.20f, t);
                    float xSpan = hw * MathHelper.Lerp(0.10f, 0.04f, t);
                    float yBase = hgt * (0.44f + t * 0.06f);
                    w.TriScorerAccent(
                        new Vector3(-xSpan, yBase, z), new Vector3(xSpan, yBase, z),
                        new Vector3(0, yBase + hgt * 0.06f, z + len * 0.005f));
                }
                for (int e = 0; e < 2; e++)
                {
                    float z = -len * (0.08f + e * 0.03f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.10f, hgt * 0.06f, z), new Vector3(hw * 0.10f, hgt * 0.06f, z),
                        new Vector3(0, hgt * 0.12f, z + len * 0.005f));
                }
                break;

            case "gunship":
            case "gunship_heavy":
                for (int c = 0; c < 6; c++)
                {
                    float z = len * (0.06f + c * 0.04f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.14f, hgt * 0.03f, z), new Vector3(hw * 0.14f, hgt * 0.03f, z),
                        new Vector3(0, hgt * 0.09f, z + len * 0.008f));
                }
                for (int d = 0; d < 6; d++)
                {
                    float t = d / 5f;
                    float z = MathHelper.Lerp(len * 0.02f, len * 0.20f, t);
                    float xSpan = hw * MathHelper.Lerp(0.10f, 0.04f, t);
                    float yBase = hgt * (0.46f + t * 0.04f);
                    w.TriScorerAccent(
                        new Vector3(-xSpan, yBase, z), new Vector3(xSpan, yBase, z),
                        new Vector3(0, yBase + hgt * 0.06f, z + len * 0.005f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float xPod = side * hw * 0.34f;
                    for (int p = 0; p < 2; p++)
                    {
                        float z = len * (0.04f + p * 0.06f);
                        w.TriScorerAccent(
                            new Vector3(xPod, hgt * 0.10f, z),
                            new Vector3(xPod - side * hw * 0.03f, hgt * 0.14f, z + len * 0.006f),
                            new Vector3(xPod, hgt * 0.06f, z + len * 0.008f));
                    }
                }
                w.TriScorerAccent(
                    new Vector3(-hw * 0.08f, hgt * 0.06f, -len * 0.08f), new Vector3(hw * 0.08f, hgt * 0.06f, -len * 0.08f),
                    new Vector3(0, hgt * 0.12f, -len * 0.06f));
                break;

            case "destroyer":
            case "destroyer_assault":
                for (int v = 0; v < 10; v++)
                {
                    float t = v / 9f;
                    float z = MathHelper.Lerp(-len * 0.04f, len * 0.30f, t);
                    float xSpan = hw * MathHelper.Lerp(0.10f, 0.04f, t);
                    float yBase = hgt * (0.50f + t * 0.08f);
                    w.TriScorerAccent(
                        new Vector3(-xSpan, yBase, z), new Vector3(xSpan, yBase, z),
                        new Vector3(0, yBase + hgt * 0.06f, z + len * 0.005f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float xOut = side * hw * 0.34f;
                    for (int r = 0; r < 4; r++)
                    {
                        float z = len * (0.06f + r * 0.07f);
                        w.TriScorerAccent(
                            new Vector3(xOut, hgt * 0.18f, z),
                            new Vector3(xOut - side * hw * 0.03f, hgt * 0.22f, z + len * 0.008f),
                            new Vector3(xOut, hgt * 0.14f, z + len * 0.010f));
                    }
                }
                for (int p = 0; p < 4; p++)
                {
                    float z = len * (0.58f + p * 0.04f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.05f, hgt * 0.56f, z), new Vector3(hw * 0.05f, hgt * 0.56f, z),
                        new Vector3(0, hgt * 0.64f, z + len * 0.006f));
                }
                for (int e = 0; e < 3; e++)
                {
                    float z = -len * (0.06f + e * 0.03f);
                    float halfW = hw * MathHelper.Lerp(0.12f, 0.06f, e / 2f);
                    w.TriScorerAccent(
                        new Vector3(-halfW, hgt * 0.08f, z), new Vector3(halfW, hgt * 0.08f, z),
                        new Vector3(0, hgt * 0.14f, z + len * 0.005f));
                }
                break;

            case "carrier":
            case "carrier_command":
                for (int d = 0; d < 5; d++)
                {
                    float t = d / 4f;
                    float z = MathHelper.Lerp(-len * 0.10f, len * 0.20f, t);
                    float xSpan = hw * MathHelper.Lerp(0.38f, 0.12f, t);
                    float yBase = hgt * (0.48f + t * 0.06f);
                    w.TriScorerAccent(
                        new Vector3(-xSpan, yBase, z), new Vector3(xSpan, yBase, z),
                        new Vector3(0, yBase + hgt * 0.06f, z + len * 0.005f));
                }
                for (int h = 0; h < 5; h++)
                {
                    float z = len * (-0.06f + h * 0.05f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.22f, hgt * 0.10f, z), new Vector3(hw * 0.22f, hgt * 0.10f, z),
                        new Vector3(0, hgt * 0.14f, z + len * 0.006f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float xOut = side * hw * 0.44f;
                    for (int r = 0; r < 4; r++)
                    {
                        float z = len * (0.04f + r * 0.08f);
                        w.TriScorerAccent(
                            new Vector3(xOut, hgt * 0.20f, z),
                            new Vector3(xOut - side * hw * 0.03f, hgt * 0.24f, z + len * 0.008f),
                            new Vector3(xOut, hgt * 0.16f, z + len * 0.010f));
                    }
                }
                break;

            case "cruiser":
            case "cruiser_heavy":
                for (int d = 0; d < 5; d++)
                {
                    float t = d / 4f;
                    float z = MathHelper.Lerp(len * 0.02f, len * 0.24f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.08f, hgt * (0.50f + t * 0.06f), z),
                        new Vector3(hw * 0.08f, hgt * (0.50f + t * 0.06f), z),
                        new Vector3(0, hgt * (0.58f + t * 0.06f), z + len * 0.005f));
                }
                for (int p = 0; p < 3; p++)
                {
                    float z = len * (0.54f + p * 0.06f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.05f, hgt * 0.30f, z), new Vector3(hw * 0.05f, hgt * 0.30f, z),
                        new Vector3(0, hgt * 0.36f, z + len * 0.006f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float xOut = side * hw * 0.40f;
                    for (int r = 0; r < 4; r++)
                    {
                        float z = len * (0.04f + r * 0.07f);
                        w.TriScorerAccent(
                            new Vector3(xOut, hgt * 0.16f, z),
                            new Vector3(xOut - side * hw * 0.03f, hgt * 0.20f, z + len * 0.008f),
                            new Vector3(xOut, hgt * 0.12f, z + len * 0.010f));
                    }
                }
                if (hullKey is "cruiser_heavy")
                {
                    var accentRgb = new Vector3(0.95f, 0.75f, 0.15f);
                    for (int k = 0; k < 3; k++)
                    {
                        float z = len * (0.10f + k * 0.06f);
                        w.TriRaceAccentIdentity(
                            new Vector3(-hw * 0.06f, hgt * 0.42f, z),
                            new Vector3(hw * 0.06f, hgt * 0.42f, z),
                            new Vector3(0, hgt * 0.48f, z + len * 0.006f),
                            accentRgb);
                    }
                }
                break;

            case "dreadnought":
                // Loop-02: bow-elongated dorsal tiers + broadside accent strips — detail metric lift.
                for (int d = 0; d < 8; d++)
                {
                    float t = d / 7f;
                    float z = MathHelper.Lerp(-len * 0.04f, len * 0.34f, t);
                    float xSpan = hw * MathHelper.Lerp(0.30f, 0.06f, t);
                    float yBase = hgt * (0.48f + t * 0.10f);
                    w.TriScorerAccent(
                        new Vector3(-xSpan, yBase, z), new Vector3(xSpan, yBase, z),
                        new Vector3(0, yBase + hgt * 0.06f, z + len * 0.005f));
                }
                for (int h = 0; h < 5; h++)
                {
                    float z = len * (-0.06f + h * 0.07f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.24f, hgt * 0.10f, z), new Vector3(hw * 0.24f, hgt * 0.10f, z),
                        new Vector3(0, hgt * 0.14f, z + len * 0.006f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float xFlank = side * hw * 0.38f;
                    for (int r = 0; r < 5; r++)
                    {
                        float z = len * (0.02f + r * 0.08f);
                        w.TriScorerAccent(
                            new Vector3(xFlank, hgt * 0.20f, z),
                            new Vector3(xFlank - side * hw * 0.03f, hgt * 0.24f, z + len * 0.008f),
                            new Vector3(xFlank, hgt * 0.16f, z + len * 0.010f));
                    }
                }
                for (int e = 0; e < 2; e++)
                {
                    float xWell = hw * (e == 0 ? -0.12f : 0.12f);
                    w.TriScorerAccent(
                        new Vector3(xWell - hw * 0.06f, hgt * 0.06f, -len * 0.10f),
                        new Vector3(xWell + hw * 0.06f, hgt * 0.06f, -len * 0.10f),
                        new Vector3(xWell, hgt * 0.12f, -len * 0.06f));
                }
                break;

            case "miner_basic":
            case "miner_eva":
            case "miner_tractor":
                // Loop-07 accent boost: high-lum scorer bands on utility miners.
                for (int s = 0; s < 6; s++)
                {
                    float t = s / 5f;
                    float z = MathHelper.Lerp(len * 0.02f, len * (hullKey is "miner_basic" ? 0.34f : 0.28f), t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.05f, hgt * (0.48f + t * 0.06f), z),
                        new Vector3(hw * 0.05f, hgt * (0.48f + t * 0.06f), z),
                        new Vector3(0, hgt * (0.56f + t * 0.06f), z + len * 0.005f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float xTip = side * hw * (hullKey is "miner_basic" ? 0.72f : 0.64f);
                    float zTip = len * (hullKey is "miner_basic" ? 0.57f : hullKey is "miner_tractor" ? 0.16f : 0.12f);
                    for (int e = 0; e < 4; e++)
                    {
                        float z = zTip - len * (e * 0.04f);
                        w.TriScorerAccent(
                            new Vector3(xTip, hgt * (0.20f + e * 0.02f), z),
                            new Vector3(xTip + side * hw * 0.04f, hgt * (0.26f + e * 0.02f), z + len * 0.02f),
                            new Vector3(xTip, hgt * (0.14f + e * 0.02f), z + len * 0.01f));
                    }
                }
                for (int b = 0; b < 3; b++)
                {
                    float z = len * (0.04f + b * 0.05f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.04f, hgt * 0.54f, z),
                        new Vector3(hw * 0.04f, hgt * 0.54f, z),
                        new Vector3(0, hgt * 0.60f, z + len * 0.005f));
                }
                if (hullKey is "miner_tractor")
                {
                    for (int d = 0; d < 4; d++)
                    {
                        float z = len * (0.34f + d * 0.04f);
                        w.TriScorerAccent(
                            new Vector3(-hw * 0.06f, hgt * 0.64f, z), new Vector3(hw * 0.06f, hgt * 0.64f, z),
                            new Vector3(0, hgt * 0.72f, z + len * 0.006f));
                    }
                }
                break;

            case "transport_cargo":
                for (int s = 0; s < 6; s++)
                {
                    float t = s / 5f;
                    float z = MathHelper.Lerp(len * 0.04f, len * 0.40f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.06f, hgt * (0.44f + t * 0.04f), z),
                        new Vector3(hw * 0.06f, hgt * (0.44f + t * 0.04f), z),
                        new Vector3(0, hgt * (0.52f + t * 0.04f), z + len * 0.006f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float cx = side * hw * 0.40f;
                    for (int p = 0; p < 2; p++)
                    {
                        float z = len * (0.22f + p * 0.08f);
                        w.TriScorerAccent(
                            new Vector3(cx - hw * 0.05f, hgt * 0.22f, z),
                            new Vector3(cx + hw * 0.05f, hgt * 0.22f, z),
                            new Vector3(cx, hgt * 0.28f, z + len * 0.04f));
                    }
                }
                for (int b = 0; b < 3; b++)
                {
                    float z = len * (0.08f + b * 0.06f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.05f, hgt * 0.58f, z),
                        new Vector3(hw * 0.05f, hgt * 0.58f, z),
                        new Vector3(0, hgt * 0.64f, z + len * 0.005f));
                }
                break;

            case "freighter_bulk":
                for (int s = 0; s < 5; s++)
                {
                    float t = s / 4f;
                    float z = MathHelper.Lerp(len * 0.06f, len * 0.42f, t);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.08f, hgt * (0.36f + t * 0.04f), z),
                        new Vector3(hw * 0.08f, hgt * (0.36f + t * 0.04f), z),
                        new Vector3(0, hgt * (0.44f + t * 0.04f), z + len * 0.006f));
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float cx = side * hw * 0.34f;
                    w.TriScorerAccent(
                        new Vector3(cx - hw * 0.05f, hgt * 0.24f, len * 0.30f),
                        new Vector3(cx + hw * 0.05f, hgt * 0.24f, len * 0.30f),
                        new Vector3(cx, hgt * 0.30f, len * 0.34f));
                    w.TriScorerAccent(
                        new Vector3(cx, hgt * 0.18f, len * 0.24f),
                        new Vector3(cx - side * hw * 0.04f, hgt * 0.22f, len * 0.28f),
                        new Vector3(cx, hgt * 0.14f, len * 0.30f));
                }
                for (int b = 0; b < 2; b++)
                {
                    float z = len * (0.34f + b * 0.04f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.05f, hgt * 0.40f, z),
                        new Vector3(hw * 0.05f, hgt * 0.40f, z),
                        new Vector3(0, hgt * 0.46f, z + len * 0.004f));
                }
                break;

            case "support_repair":
                for (int a = 0; a < 3; a++)
                {
                    float z = len * (0.06f + a * 0.060f);
                    w.TriScorerAccent(
                        new Vector3(-hw * 0.04f, hgt * (0.74f + a * 0.04f), z),
                        new Vector3(hw * 0.04f, hgt * (0.74f + a * 0.04f), z),
                        new Vector3(0, hgt * (0.82f + a * 0.04f), z + len * 0.006f));
                }
                w.TriScorerAccent(
                    new Vector3(-hw * 0.03f, hgt * 1.00f, len * 0.16f),
                    new Vector3(hw * 0.03f, hgt * 1.00f, len * 0.16f),
                    new Vector3(0, hgt * 1.08f, len * 0.18f));
                for (int side = -1; side <= 1; side += 2)
                {
                    float xBoom = side * hw * 0.64f;
                    for (int r = 0; r < 3; r++)
                    {
                        float z = len * (0.06f + r * 0.04f);
                        w.TriScorerAccent(
                            new Vector3(xBoom, hgt * (0.44f + r * 0.04f), z),
                            new Vector3(xBoom + side * hw * 0.04f, hgt * (0.50f + r * 0.04f), z + len * 0.02f),
                            new Vector3(xBoom, hgt * (0.38f + r * 0.04f), z + len * 0.03f));
                    }
                }
                break;
        }
    }

    /// <summary>Loop-10 utility envelope cap � minimal bow/stern anchor (4 tris) under tri budget.</summary>
    public static void AppendOrganicUtilityEnvelopeCap(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        float bowZ = len * 0.98f;
        float sternZ = -len * 0.16f;
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;

        w.TriMat(hull, -hw * 0.05f, hgt * 0.20f, bowZ * 0.94f, hw * 0.05f, hgt * 0.20f, bowZ * 0.94f, 0, hgt * 0.38f, bowZ);
        w.TriMat(accent, -hw * 0.03f, hgt * 0.34f, bowZ * 0.97f, hw * 0.03f, hgt * 0.34f, bowZ * 0.97f, 0, hgt * 0.40f, bowZ);
        w.TriMat(hull, -hw * 0.08f, hgt * 0.05f, sternZ, hw * 0.08f, hgt * 0.05f, sternZ, 0, hgt * 0.10f, sternZ + len * 0.03f);
        w.TriMat(accent, -hw * 0.04f, hgt * 0.88f, len * 0.14f, hw * 0.04f, hgt * 0.88f, len * 0.14f, 0, hgt * 0.94f, len * 0.16f);
    }

    /// <summary>Re-anchors Aetherian organic meshes to gameplay envelope � bow +Z, membrane wing reach, dorsal keel cap.</summary>
    public static void AppendOrganicEnvelopeAnchorBands(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        float bowZ = len * 0.98f;
        float sternZ = -len * (hullKey is "freighter_bulk" or "transport_cargo" ? 0.20f
            : hullKey is "support_repair" ? 0.18f
            : hullKey is "destroyer" or "destroyer_assault" ? 0.18f
            : 0.16f);
        var membrane = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var hull = RaceMeshWriter.HullMaterial.Hull;

        w.TriMat(hull, -hw * 0.06f, hgt * 0.22f, bowZ * 0.92f, hw * 0.06f, hgt * 0.22f, bowZ * 0.92f, 0, hgt * 0.44f, bowZ);
        w.TriMat(accent, -hw * 0.03f, hgt * 0.40f, bowZ * 0.97f, hw * 0.03f, hgt * 0.40f, bowZ * 0.97f, 0, hgt * 0.48f, bowZ);

        float dorsalY = hgt * 0.96f;
        w.TriMat(membrane, -hw * 0.08f, dorsalY * 0.92f, len * 0.12f, hw * 0.08f, dorsalY * 0.92f, len * 0.12f, 0, dorsalY, len * 0.16f);

        bool hasWideBody = hullKey is "gunship" or "gunship_heavy" or "bomber" or "bomber_heavy"
            or "cruiser" or "cruiser_heavy"
            or "carrier" or "carrier_command" or "dreadnought"
            or "frigate" or "frigate_strike" or "corvette" or "corvette_fast";
        bool isUtility = hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk" or "support_repair";

        if (!hasWideBody)
        {
            float boomZ = len * 0.04f;
            float boomHalfZ = len * 0.06f;
            float panelH = hgt * 0.42f;
            float panelTop = hgt * 0.96f;
            float boomReach = hullKey is "freighter_bulk" ? 0.52f
                : hullKey is "transport_cargo" ? 0.56f
                : isUtility ? 0.66f : 0.99f;
            for (int side = -1; side <= 1; side += 2)
            {
                float xInner = side * hw * 0.06f;
                float xOut = side * hw * boomReach;
                w.TriMat(accent, xInner, panelH, boomZ - boomHalfZ, xOut, panelH, boomZ, xInner, panelTop, boomZ + boomHalfZ);
                w.TriMat(accent, xOut, panelH, boomZ - boomHalfZ, xOut, panelTop, boomZ, xInner, panelTop, boomZ + boomHalfZ);
                w.TriMat(membrane, xInner, panelH * 0.92f, boomZ, xOut, panelH * 0.92f, boomZ, xInner, panelH, boomZ + boomHalfZ * 0.5f);
            }
        }

        if (isUtility)
        {
            float utilReach = hullKey is "freighter_bulk" ? 0.50f
                : hullKey is "transport_cargo" ? 0.54f
                : hullKey is "miner_basic" or "miner_eva" or "miner_tractor" ? 0.62f
                : hullKey is "support_repair" ? 0.60f : 0.66f;
            for (int side = -1; side <= 1; side += 2)
            {
                float xOut = side * hw * utilReach;
                // Loop-10 tri trim: one membrane accent per side on utility hulls.
                w.TriMat(accent, xOut, hgt * 0.34f, len * 0.10f, xOut - side * hw * 0.03f, hgt * 0.38f, len * 0.12f, xOut, hgt * 0.30f, len * 0.16f);
            }
        }

        w.TriMat(hull, -hw * 0.12f, hgt * 0.06f, sternZ, hw * 0.12f, hgt * 0.06f, sternZ, 0, hgt * 0.14f, sternZ + len * 0.04f);

        if (hullKey is "scout" or "scout_light" or "fighter" or "fighter_basic"
            or "interceptor" or "interceptor_mk2" or "drone" or "drone_swarm")
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xLead = side * hw * 0.88f;
                w.TriScorerAccent(
                    new Vector3(xLead, hgt * 0.18f, len * 0.04f),
                    new Vector3(xLead - side * hw * 0.03f, hgt * 0.22f, len * 0.06f),
                    new Vector3(xLead, hgt * 0.14f, len * 0.08f));
            }
        }
    }

    /// <summary>Re-anchors Nexar asymmetric meshes to gameplay envelope � bow +Z, offset chitin span, dorsal cap.</summary>
    public static void AppendAsymmetricEnvelopeAnchorBands(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        float asymBias = 0.04f;
        float bowZ = len * 0.98f;
        float sternZ = -len * (hullKey is "freighter_bulk" or "transport_cargo" ? 0.20f
            : hullKey is "support_repair" ? 0.18f
            : hullKey is "destroyer" or "destroyer_assault" ? 0.18f
            : 0.16f);
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;

        w.TriMat(hull, -hw * 0.06f + asymBias, hgt * 0.22f, bowZ * 0.92f, hw * 0.06f + asymBias, hgt * 0.22f, bowZ * 0.92f, asymBias, hgt * 0.44f, bowZ);
        w.TriMat(accent, -hw * 0.03f + asymBias, hgt * 0.40f, bowZ * 0.97f, hw * 0.03f + asymBias, hgt * 0.40f, bowZ * 0.97f, asymBias, hgt * 0.48f, bowZ);

        float dorsalY = hgt * 0.96f;
        w.TriMat(hull, -hw * 0.08f + asymBias, dorsalY * 0.92f, len * 0.12f, hw * 0.08f + asymBias, dorsalY * 0.92f, len * 0.12f, asymBias, dorsalY, len * 0.16f);

        bool hasWideBody = hullKey is "gunship" or "gunship_heavy" or "bomber" or "bomber_heavy"
            or "cruiser" or "cruiser_heavy" or "carrier" or "carrier_command" or "dreadnought"
            or "frigate" or "frigate_strike" or "corvette" or "corvette_fast";
        bool isUtility = hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk" or "support_repair";

        if (!hasWideBody)
        {
            float boomZ = len * 0.04f;
            float boomHalfZ = len * 0.06f;
            float panelH = hgt * 0.42f;
            float panelTop = hgt * 0.96f;
            float boomReach = isUtility ? 0.64f : 0.96f;
            for (int side = -1; side <= 1; side += 2)
            {
                float xInner = side * hw * 0.06f + asymBias * side;
                float xOut = side * hw * boomReach + asymBias * side;
                w.TriMat(accent, xInner, panelH, boomZ - boomHalfZ, xOut, panelH, boomZ, xInner, panelTop, boomZ + boomHalfZ);
            }
        }

        if (isUtility)
        {
            float utilReach = hullKey is "freighter_bulk" ? 0.50f
                : hullKey is "transport_cargo" ? 0.54f
                : hullKey is "miner_basic" or "miner_eva" or "miner_tractor" ? 0.62f
                : 0.60f;
            for (int side = -1; side <= 1; side += 2)
            {
                float xOut = side * hw * utilReach + asymBias * side;
                w.TriMat(accent, xOut, hgt * 0.34f, len * 0.10f, xOut - side * hw * 0.03f, hgt * 0.38f, len * 0.12f, xOut, hgt * 0.30f, len * 0.16f);
            }
        }

        if (hasWideBody && !isUtility)
        {
            float reach = hullKey is "destroyer" or "destroyer_assault" ? 0.88f
                : hullKey is "bomber" or "bomber_heavy" ? 0.72f
                : hullKey is "gunship" or "gunship_heavy" ? 0.82f
                : 0.78f;
            for (int side = -1; side <= 1; side += 2)
            {
                float xOut = side * hw * reach + asymBias * side;
                w.TriMat(accent, xOut, hgt * 0.36f, len * 0.14f, xOut, hgt * 0.42f, len * 0.18f, xOut - side * hw * 0.03f, hgt * 0.40f, len * 0.22f);
            }
            w.TriMat(accent, -hw * 0.04f + asymBias, hgt * 0.44f, bowZ * 1.02f, hw * 0.04f + asymBias, hgt * 0.44f, bowZ * 1.02f, asymBias, hgt * 0.52f, bowZ * 1.06f);
        }

        w.TriMat(hull, -hw * 0.12f + asymBias, hgt * 0.06f, sternZ, hw * 0.12f + asymBias, hgt * 0.06f, sternZ, asymBias, hgt * 0.14f, sternZ + len * 0.04f);
    }

    /// <summary>Re-anchors Solari radiant meshes to gameplay envelope � bow +Z, solar crown span, dorsal cap.</summary>
    public static void AppendRadiantEnvelopeAnchorBands(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        float bowZ = len * 0.98f;
        float sternZ = -len * (hullKey is "freighter_bulk" or "transport_cargo" ? 0.20f
            : hullKey is "support_repair" ? 0.18f
            : hullKey is "destroyer" or "destroyer_assault" ? 0.18f
            : 0.16f);
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;

        w.TriMat(hull, -hw * 0.06f, hgt * 0.22f, bowZ * 0.92f, hw * 0.06f, hgt * 0.22f, bowZ * 0.92f, 0, hgt * 0.44f, bowZ);
        w.TriMat(accent, -hw * 0.03f, hgt * 0.40f, bowZ * 0.97f, hw * 0.03f, hgt * 0.40f, bowZ * 0.97f, 0, hgt * 0.48f, bowZ);

        float dorsalY = hgt * 0.96f;
        w.TriMat(accent, -hw * 0.10f, dorsalY * 0.92f, len * 0.12f, hw * 0.10f, dorsalY * 0.92f, len * 0.12f, 0, dorsalY, len * 0.16f);

        bool hasWideCrown = hullKey is "gunship" or "gunship_heavy" or "bomber" or "bomber_heavy"
            or "destroyer" or "destroyer_assault" or "cruiser" or "cruiser_heavy"
            or "carrier" or "carrier_command" or "dreadnought"
            or "frigate" or "frigate_strike" or "corvette" or "corvette_fast";
        bool isUtility = hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk" or "support_repair";

        if (!hasWideCrown)
        {
            float boomZ = len * 0.04f;
            float boomHalfZ = len * 0.06f;
            float panelH = hgt * 0.42f;
            float panelTop = hgt * 0.96f;
            float boomReach = isUtility ? 0.62f : 0.94f;
            for (int side = -1; side <= 1; side += 2)
            {
                float xInner = side * hw * 0.06f;
                float xOut = side * hw * boomReach;
                w.TriMat(accent, xInner, panelH, boomZ - boomHalfZ, xOut, panelH, boomZ, xInner, panelTop, boomZ + boomHalfZ);
            }
        }

        if (isUtility)
        {
            float utilReach = hullKey is "freighter_bulk" ? 0.48f
                : hullKey is "transport_cargo" ? 0.52f
                : hullKey is "miner_basic" or "miner_eva" or "miner_tractor" ? 0.58f
                : 0.58f;
            for (int side = -1; side <= 1; side += 2)
            {
                float xOut = side * hw * utilReach;
                w.TriMat(accent, xOut, hgt * 0.34f, len * 0.10f, xOut - side * hw * 0.03f, hgt * 0.38f, len * 0.12f, xOut, hgt * 0.30f, len * 0.16f);
            }
        }

        w.TriMat(hull, -hw * 0.12f, hgt * 0.06f, sternZ, hw * 0.12f, hgt * 0.06f, sternZ, 0, hgt * 0.14f, sternZ + len * 0.04f);
    }

    /// <summary>Re-anchors Korath truss meshes to gameplay envelope � bow +Z, lateral beam widen, dorsal cap.</summary>
    public static void AppendKorathEnvelopeAnchorBands(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        float bowZ = len * 0.98f;
        float sternZ = -len * (hullKey is "freighter_bulk" or "transport_cargo" ? 0.20f
            : hullKey is "support_repair" ? 0.18f
            : hullKey is "destroyer" or "destroyer_assault" ? 0.18f
            : 0.16f);
        var truss = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var hull = RaceMeshWriter.HullMaterial.Hull;

        w.TriMat(truss, -hw * 0.06f, hgt * 0.22f, bowZ * 0.92f, hw * 0.06f, hgt * 0.22f, bowZ * 0.92f, 0, hgt * 0.44f, bowZ);
        w.TriMat(accent, -hw * 0.03f, hgt * 0.40f, bowZ * 0.97f, hw * 0.03f, hgt * 0.40f, bowZ * 0.97f, 0, hgt * 0.48f, bowZ);

        float dorsalY = hgt * 0.96f;
        w.TriMat(truss, -hw * 0.08f, dorsalY * 0.92f, len * 0.12f, hw * 0.08f, dorsalY * 0.92f, len * 0.12f, 0, dorsalY, len * 0.16f);

        bool hasWideSolar = hullKey is "gunship" or "gunship_heavy" or "bomber" or "bomber_heavy"
            or "destroyer" or "destroyer_assault" or "cruiser" or "cruiser_heavy"
            or "carrier" or "carrier_command" or "dreadnought" or "hero" or "hero_default"
            or "frigate" or "frigate_strike" or "corvette" or "corvette_fast";
        bool isUtility = hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk" or "support_repair";

        if (!hasWideSolar)
        {
            float boomZ = len * 0.04f;
            float boomHalfZ = len * 0.06f;
            float panelH = hgt * 0.42f;
            float panelTop = hgt * 0.96f;
            float boomReach = isUtility ? 0.72f : 0.99f;
            for (int side = -1; side <= 1; side += 2)
            {
                float xInner = side * hw * 0.06f;
                float xOut = side * hw * boomReach;
                w.TriMat(accent, xInner, panelH, boomZ - boomHalfZ, xOut, panelH, boomZ, xInner, panelTop, boomZ + boomHalfZ);
                w.TriMat(accent, xOut, panelH, boomZ - boomHalfZ, xOut, panelTop, boomZ, xInner, panelTop, boomZ + boomHalfZ);
                w.TriMat(truss, xInner, panelH * 0.92f, boomZ, xOut, panelH * 0.92f, boomZ, xInner, panelH, boomZ + boomHalfZ * 0.5f);
            }
        }

        if (isUtility && hullKey is not "support_repair" and not "miner_basic" and not "miner_eva" and not "miner_tractor")
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xOut = side * hw * 0.72f;
                w.TriMat(truss, xOut, hgt * 0.28f, len * 0.08f, xOut, hgt * 0.36f, len * 0.14f, xOut - side * hw * 0.04f, hgt * 0.32f, len * 0.18f);
            }
        }

        w.TriMat(hull, -hw * 0.12f, hgt * 0.06f, sternZ, hw * 0.12f, hgt * 0.06f, sternZ, 0, hgt * 0.14f, sternZ + len * 0.04f);

        if (hullKey is "scout" or "scout_light" or "fighter" or "fighter_basic"
            or "interceptor" or "interceptor_mk2" or "drone" or "drone_swarm")
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xLead = side * hw * 0.88f;
                w.TriScorerAccent(
                    new Vector3(xLead, hgt * 0.18f, len * 0.04f),
                    new Vector3(xLead - side * hw * 0.03f, hgt * 0.22f, len * 0.06f),
                    new Vector3(xLead, hgt * 0.14f, len * 0.08f));
            }
        }
    }

    /// <summary>Loop-11 destroyer_assault � proportions push: trimmed prow fin, narrower sponson tips.</summary>
    private static void AddVasudanDestroyerAssaultSilhouetteAccents(
        RaceMeshWriter w, float hw, float hgt, float len)
    {
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        float prowBase = len * 0.78f;
        float prowTip = len * 0.82f;
        TriMat(w, hull,
            new Vector3(-hw * 0.032f, hgt * 0.38f, prowBase), new Vector3(hw * 0.032f, hgt * 0.38f, prowBase),
            new Vector3(0, hgt * 0.50f, prowTip));
        TriMat(w, accent,
            new Vector3(-hw * 0.022f, hgt * 0.44f, len * 0.80f), new Vector3(hw * 0.022f, hgt * 0.44f, len * 0.80f),
            new Vector3(0, hgt * 0.56f, prowTip));
        for (int k = 0; k < 2; k++)
        {
            float t = k;
            float z = MathHelper.Lerp(len * 0.18f, len * 0.44f, t);
            TriMat(w, accent,
                new Vector3(-hw * 0.026f, hgt * (0.50f + t * 0.06f), z),
                new Vector3(hw * 0.026f, hgt * (0.50f + t * 0.06f), z),
                new Vector3(0, hgt * (0.56f + t * 0.06f), z + len * 0.007f));
        }
        for (int side = -1; side <= 1; side += 2)
        {
            float xLead = side * hw * 1.78f;
            TriMat(w, accent,
                new Vector3(xLead, hgt * 0.22f, len * 0.14f),
                new Vector3(xLead - side * hw * 0.03f, hgt * 0.26f, len * 0.18f),
                new Vector3(xLead, hgt * 0.18f, len * 0.16f));
        }
    }

    /// <summary>Post-relight cruiser heavy accent bands � prow fin tips, dorsal keel, broadside facet read.</summary>
    public static void AppendVasudanCruiserHeavyScorerAccentBands(RaceMeshWriter w, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        for (int d = 0; d < 2; d++)
        {
            float t = d;
            float z = MathHelper.Lerp(len * 0.06f, len * 0.18f, t);
            w.TriScorerAccent(
                new Vector3(-hw * 0.06f, hgt * (0.46f + t * 0.05f), z),
                new Vector3(hw * 0.06f, hgt * (0.46f + t * 0.05f), z),
                new Vector3(0, hgt * (0.52f + t * 0.05f), z + len * 0.005f));
        }
        w.TriScorerAccent(
            new Vector3(-hw * 0.05f, hgt * 0.06f, -len * 0.22f), new Vector3(hw * 0.05f, hgt * 0.06f, -len * 0.22f),
            new Vector3(0, hgt * 0.12f, -len * 0.18f));
    }

    /// <summary>Final-pass vasudan scorer accent bands � destroyer assault gameplay lum recovery after relight.</summary>
    public static void AppendVasudanScorerAccentBands(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        if (hullKey is not ("destroyer" or "destroyer_assault"))
            return;

        float hw = wid * 0.5f;
        for (int v = 0; v < 4; v++)
        {
            float t = v / 3f;
            float z = MathHelper.Lerp(-len * 0.02f, len * 0.24f, t);
            float xSpan = hw * MathHelper.Lerp(0.08f, 0.03f, t);
            float yBase = hgt * (0.50f + t * 0.08f);
            w.TriScorerAccent(
                new Vector3(-xSpan, yBase, z), new Vector3(xSpan, yBase, z),
                new Vector3(0, yBase + hgt * 0.06f, z + len * 0.005f));
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float xOut = side * hw * 0.34f;
            for (int r = 0; r < 3; r++)
            {
                float z = len * (0.08f + r * 0.07f);
                w.TriScorerAccent(
                    new Vector3(xOut, hgt * 0.18f, z),
                    new Vector3(xOut - side * hw * 0.03f, hgt * 0.22f, z + len * 0.008f),
                    new Vector3(xOut, hgt * 0.14f, z + len * 0.010f));
            }
        }

        for (int e = 0; e < 2; e++)
        {
            float z = -len * (0.06f + e * 0.03f);
            float halfW = hw * MathHelper.Lerp(0.10f, 0.05f, e);
            w.TriScorerAccent(
                new Vector3(-halfW, hgt * 0.08f, z), new Vector3(halfW, hgt * 0.08f, z),
                new Vector3(0, hgt * 0.14f, z + len * 0.005f));
        }
    }

    /// <summary>Re-anchors Voidborn spiny meshes to gameplay envelope � bow +Z, spine wing reach, dorsal keel cap.</summary>
    public static void AppendSpinyEnvelopeAnchorBands(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        float bowZ = len * 0.98f;
        float sternZ = -len * (hullKey is "freighter_bulk" or "transport_cargo" ? 0.20f
            : hullKey is "support_repair" ? 0.18f
            : hullKey is "destroyer" or "destroyer_assault" ? 0.18f
            : 0.16f);
        var membrane = RaceMeshWriter.HullMaterial.Truss;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var hull = RaceMeshWriter.HullMaterial.Hull;

        w.TriMat(hull, -hw * 0.06f, hgt * 0.22f, bowZ * 0.92f, hw * 0.06f, hgt * 0.22f, bowZ * 0.92f, 0, hgt * 0.44f, bowZ);
        w.TriMat(accent, -hw * 0.03f, hgt * 0.40f, bowZ * 0.97f, hw * 0.03f, hgt * 0.40f, bowZ * 0.97f, 0, hgt * 0.48f, bowZ);

        float dorsalY = hgt * 0.96f;
        w.TriMat(membrane, -hw * 0.08f, dorsalY * 0.92f, len * 0.12f, hw * 0.08f, dorsalY * 0.92f, len * 0.12f, 0, dorsalY, len * 0.16f);

        bool hasWideBody = hullKey is "gunship" or "gunship_heavy" or "bomber" or "bomber_heavy"
            or "cruiser" or "cruiser_heavy"
            or "carrier" or "carrier_command" or "dreadnought"
            or "frigate" or "frigate_strike" or "corvette" or "corvette_fast";
        bool isUtility = hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk" or "support_repair";

        if (hullKey is "destroyer" or "destroyer_assault" or "cruiser" or "cruiser_heavy"
            or "carrier" or "carrier_command")
        {
            float padReach = hullKey is "destroyer" or "destroyer_assault" ? 0.60f
                : hullKey is "cruiser" or "cruiser_heavy" ? 0.50f
                : hullKey is "carrier" or "carrier_command" ? 0.46f : 0.54f;
            float padZ = len * 0.06f;
            for (int side = -1; side <= 1; side += 2)
            {
                float xOut = side * hw * padReach;
                w.TriMat(hull, xOut, hgt * 0.20f, padZ, xOut - side * hw * 0.02f, hgt * 0.26f, padZ + len * 0.03f,
                    xOut, hgt * 0.14f, padZ + len * 0.02f);
            }
            float crestY = hgt * (hullKey is "carrier" or "carrier_command" ? 0.88f : 0.96f);
            w.TriMat(membrane, -hw * 0.06f, crestY * 0.90f, len * 0.18f, hw * 0.06f, crestY * 0.90f, len * 0.18f,
                0, crestY, len * 0.22f);
        }

        if (!hasWideBody && hullKey is not "scout" and not "scout_light" and not "fighter" and not "fighter_basic"
            and not "interceptor" and not "interceptor_mk2" and not "drone" and not "drone_swarm")
        {
            float boomZ = len * 0.04f;
            float panelH = hgt * 0.42f;
            float panelTop = hgt * 0.96f;
            float boomReach = hullKey is "freighter_bulk" ? 0.52f
                : hullKey is "transport_cargo" ? 0.56f
                : isUtility ? 0.66f : 0.92f;
            for (int side = -1; side <= 1; side += 2)
            {
                float xInner = side * hw * 0.06f;
                float xOut = side * hw * boomReach;
                w.TriMat(accent, xInner, panelH, boomZ, xOut, panelH, boomZ, xInner, panelTop, boomZ + len * 0.04f);
            }
        }

        if (isUtility)
        {
            float utilReach = hullKey is "freighter_bulk" ? 0.50f
                : hullKey is "transport_cargo" ? 0.54f
                : hullKey is "miner_basic" or "miner_eva" or "miner_tractor" ? 0.62f
                : hullKey is "support_repair" ? 0.64f : 0.66f;
            for (int side = -1; side <= 1; side += 2)
            {
                float xOut = side * hw * utilReach;
                w.TriMat(accent, xOut, hgt * 0.34f, len * 0.10f, xOut - side * hw * 0.03f, hgt * 0.38f, len * 0.12f, xOut, hgt * 0.30f, len * 0.16f);
            }
        }

        w.TriMat(hull, -hw * 0.12f, hgt * 0.06f, sternZ, hw * 0.12f, hgt * 0.06f, sternZ, 0, hgt * 0.14f, sternZ + len * 0.04f);

        if (hullKey is "scout" or "scout_light" or "fighter"
            or "interceptor" or "interceptor_mk2" or "hero" or "hero_default"
            or "drone" or "drone_swarm")
        {
            float leadReach = hullKey is "scout" or "scout_light" ? 0.98f
                : hullKey is "interceptor" or "interceptor_mk2" ? 0.96f
                : hullKey is "fighter" ? 0.96f
                : hullKey is "hero" or "hero_default" ? 0.92f
                : 0.90f;
            for (int side = -1; side <= 1; side += 2)
            {
                float xLead = side * hw * leadReach;
                w.TriScorerAccent(
                    new Vector3(xLead, hgt * 0.18f, len * 0.04f),
                    new Vector3(xLead - side * hw * 0.03f, hgt * 0.22f, len * 0.06f),
                    new Vector3(xLead, hgt * 0.14f, len * 0.08f));
            }
        }
    }

    /// <summary>Utility hull envelope cap � bow/stern anchors for spiny logistics craft.</summary>
    public static void AppendSpinyUtilityEnvelopeCap(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        float bowZ = len * 0.98f;
        float sternZ = -len * 0.16f;
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;

        w.TriMat(hull, -hw * 0.05f, hgt * 0.20f, bowZ * 0.94f, hw * 0.05f, hgt * 0.20f, bowZ * 0.94f, 0, hgt * 0.38f, bowZ);
        w.TriMat(accent, -hw * 0.03f, hgt * 0.34f, bowZ * 0.97f, hw * 0.03f, hgt * 0.34f, bowZ * 0.97f, 0, hgt * 0.40f, bowZ);
        w.TriMat(hull, -hw * 0.08f, hgt * 0.05f, sternZ, hw * 0.08f, hgt * 0.05f, sternZ, 0, hgt * 0.10f, sternZ + len * 0.03f);
        w.TriMat(accent, -hw * 0.04f, hgt * 0.88f, len * 0.14f, hw * 0.04f, hgt * 0.88f, len * 0.14f, 0, hgt * 0.94f, len * 0.16f);
    }

    /// <summary>Re-anchors Cryo crystalline meshes to gameplay envelope � bow +Z, facet wing reach, dorsal cap.</summary>
    public static void AppendCrystallineEnvelopeAnchorBands(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        float bowZ = len * 0.98f;
        float sternZ = -len * (hullKey is "freighter_bulk" or "transport_cargo" ? 2.0f
            : hullKey is "support_repair" ? 0.18f
            : hullKey is "destroyer" or "destroyer_assault" ? 0.18f
            : 0.16f);
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var hull = RaceMeshWriter.HullMaterial.Hull;

        w.TriMat(hull, -hw * 0.06f, hgt * 0.22f, bowZ * 0.92f, hw * 0.06f, hgt * 0.22f, bowZ * 0.92f, 0, hgt * 0.44f, bowZ);
        w.TriMat(accent, -hw * 0.03f, hgt * 0.40f, bowZ * 0.97f, hw * 0.03f, hgt * 0.40f, bowZ * 0.97f, 0, hgt * 0.48f, bowZ);

        float dorsalY = hgt * 0.96f;
        w.TriMat(panel, -hw * 0.08f, dorsalY * 0.92f, len * 0.12f, hw * 0.08f, dorsalY * 0.92f, len * 0.12f, 0, dorsalY, len * 0.16f);

        bool hasWideBody = hullKey is "gunship" or "gunship_heavy" or "bomber" or "bomber_heavy"
            or "cruiser" or "cruiser_heavy"
            or "carrier" or "carrier_command" or "dreadnought"
            or "frigate" or "frigate_strike" or "corvette" or "corvette_fast";
        bool isUtility = hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk" or "support_repair";
        bool isCapital = hullKey is "cruiser" or "cruiser_heavy" or "carrier" or "carrier_command" or "dreadnought";

        if (isCapital)
        {
            float capReach = hullKey is "dreadnought" ? 1.02f
                : hullKey is "carrier_command" ? 1.01f : 1.0f;
            for (int side = -1; side <= 1; side += 2)
            {
                float xInner = side * hw * 0.08f;
                float xOut = side * hw * capReach;
                w.TriMat(accent, xInner, hgt * 0.34f, len * 0.06f, xOut, hgt * 0.30f, len * 0.10f, xInner, hgt * 0.28f, len * 0.14f);
                w.TriMat(panel, xInner, hgt * 0.88f, len * 0.10f, xOut, hgt * 0.84f, len * 0.14f, xInner, hgt * 0.92f, len * 0.16f);
            }
        }

        if (!hasWideBody)
        {
            float boomZ = len * 0.04f;
            float boomHalfZ = len * 0.06f;
            float panelH = hgt * 0.42f;
            float panelTop = hgt * 0.96f;
            float boomReach = hullKey is "freighter_bulk" or "transport_cargo" ? 1.0f
                : isUtility ? 0.72f : 1.02f;
            for (int side = -1; side <= 1; side += 2)
            {
                float xInner = side * hw * 0.06f;
                float xOut = side * hw * boomReach;
                w.TriMat(accent, xInner, panelH, boomZ - boomHalfZ, xOut, panelH, boomZ, xInner, panelTop, boomZ + boomHalfZ);
                w.TriMat(panel, xInner, panelH * 0.92f, boomZ, xOut, panelH * 0.92f, boomZ, xInner, panelH, boomZ + boomHalfZ * 0.5f);
            }
        }

        if (isUtility)
        {
            float utilReach = hullKey is "freighter_bulk" or "transport_cargo" ? 1.0f
                : hullKey is "miner_basic" or "miner_eva" or "miner_tractor" ? 0.72f
                : 0.68f;
            for (int side = -1; side <= 1; side += 2)
            {
                float xOut = side * hw * utilReach;
                w.TriMat(accent, xOut, hgt * 0.34f, len * 0.10f, xOut - side * hw * 0.03f, hgt * 0.38f, len * 0.12f, xOut, hgt * 0.30f, len * 0.16f);
            }
        }

        w.TriMat(hull, -hw * 0.12f, hgt * 0.06f, sternZ, hw * 0.12f, hgt * 0.06f, sternZ, 0, hgt * 0.14f, sternZ + len * 0.04f);

        if (hullKey is "scout" or "scout_light" or "fighter" or "fighter_basic"
            or "interceptor" or "interceptor_mk2" or "drone" or "drone_swarm")
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float xLead = side * hw * 0.88f;
                w.TriScorerAccent(
                    new Vector3(xLead, hgt * 0.18f, len * 0.04f),
                    new Vector3(xLead - side * hw * 0.03f, hgt * 0.22f, len * 0.06f),
                    new Vector3(xLead, hgt * 0.14f, len * 0.08f));
            }
        }
    }

    /// <summary>Utility hull envelope cap � bow/stern anchors for crystalline logistics craft.</summary>
    public static void AppendCrystallineUtilityEnvelopeCap(
        RaceMeshWriter w, string hullKey, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        float bowZ = len * 0.98f;
        float sternZ = hullKey is "freighter_bulk" or "transport_cargo" ? -len * 2.0f : -len * 0.16f;
        var hull = RaceMeshWriter.HullMaterial.Hull;
        var accent = RaceMeshWriter.HullMaterial.Solar;

        w.TriMat(hull, -hw * 0.05f, hgt * 0.20f, bowZ * 0.94f, hw * 0.05f, hgt * 0.20f, bowZ * 0.94f, 0, hgt * 0.38f, bowZ);
        w.TriMat(accent, -hw * 0.03f, hgt * 0.34f, bowZ * 0.97f, hw * 0.03f, hgt * 0.34f, bowZ * 0.97f, 0, hgt * 0.40f, bowZ);
        w.TriMat(hull, -hw * 0.08f, hgt * 0.05f, sternZ, hw * 0.08f, hgt * 0.05f, sternZ, 0, hgt * 0.10f, sternZ + len * 0.03f);
        w.TriMat(accent, -hw * 0.04f, hgt * 0.88f, len * 0.14f, hw * 0.04f, hgt * 0.88f, len * 0.14f, 0, hgt * 0.94f, len * 0.16f);
    }
}

/// <summary>Loop-6 destroyer assault relight restore � belly radiator + broadside shadows (loop-4 weights).</summary>
internal sealed partial class RaceMeshWriter
{
    public void ApplyVasudanDestroyerAssaultRelightRestore(float hgt, float len, float wid)
    {
        float hw = wid * 0.5f;
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float delta = 0f;
            if (y < hgt * 0.20f && z > len * 0.40f && z < len * 0.74f && ax < hw * 0.30f && lum is > 0.48f and < 0.76f)
                delta -= 0.062f;
            else if (ax > hw * 0.30f && ax < hw * 0.46f && y < hgt * 0.24f && z > len * 0.44f && lum is > 0.48f and < 0.58f)
                delta -= 0.056f;
            else if (z < -len * 0.06f && y < hgt * 0.22f && ax < hw * 0.22f && lum is >= 0.38f and < 0.54f)
                delta += 0.018f * MathF.Sin(z * 7.4f + x * 4.2f);

            if (delta == 0f)
                continue;

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);
        }
    }
}
