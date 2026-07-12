using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>Subtle hull surface accents Ã¢â‚¬â€ kept flush to avoid spiky greeble clutter.</summary>
internal static class RaceSurfaceDetail
{
    public static void ApplyShipDetail(RaceMeshWriter w, RaceVisualDefinition race, string hullKey, float len, float wid, float hgt)
    {
        if (race.Style.Equals("vasudan", StringComparison.OrdinalIgnoreCase))
            ApplyVasudanShipDetail(w, race, hullKey, len, wid, hgt);
    }

    private static void ApplyVasudanShipDetail(
        RaceMeshWriter w, RaceVisualDefinition race, string hullKey, float len, float wid, float hgt)
    {
        bool isFighter = hullKey is "fighter" or "fighter_basic";
        var accent = RaceMeshWriter.HullMaterial.Solar;
        var panel = RaceMeshWriter.HullMaterial.Radiator;
        var engineMat = RaceMeshWriter.HullMaterial.Engine;

        if (isFighter)
        {
            float hw = wid * 0.5f;
            var frame = RaceMeshWriter.HullMaterial.Truss;
            var weapon = RaceMeshWriter.HullMaterial.Weapon;
            var shieldGen = RaceMeshWriter.HullMaterial.ShieldGen;

            TriMat(w, shieldGen,
                new Vector3(-hw * 0.05f, hgt * 0.80f, len * 0.06f), new Vector3(hw * 0.05f, hgt * 0.80f, len * 0.06f),
                new Vector3(0, hgt * 0.84f, len * 0.09f));
            for (int h = 0; h < 4; h++)
            {
                float ang = h * MathF.PI * 0.5f;
                float hx = MathF.Cos(ang) * hw * 0.04f;
                float hz = len * 0.08f + MathF.Sin(ang) * len * 0.02f;
                TriMat(w, shieldGen,
                    new Vector3(hx, hgt * 0.90f, hz), new Vector3(hx + hw * 0.02f, hgt * 0.86f, hz + len * 0.006f),
                    new Vector3(hx, hgt * 0.88f, hz + len * 0.01f));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float xTip = side * hw * 0.70f;
                TriMat(w, shieldGen,
                    new Vector3(xTip, hgt * 0.24f, -len * 0.02f), new Vector3(xTip - side * hw * 0.03f, hgt * 0.20f, len * 0.01f),
                    new Vector3(xTip, hgt * 0.18f, len * 0.02f));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float xPod = side * hw * 0.56f;
                TriMat(w, weapon,
                    new Vector3(xPod, hgt * 0.12f, len * 0.05f), new Vector3(xPod - side * hw * 0.03f, hgt * 0.08f, len * 0.08f),
                    new Vector3(xPod, hgt * 0.06f, len * 0.11f));
            }
            TriMat(w, weapon,
                new Vector3(-hw * 0.03f, hgt * 0.16f, len * 0.27f), new Vector3(hw * 0.03f, hgt * 0.16f, len * 0.27f),
                new Vector3(0, hgt * 0.20f, len * 0.29f));
            for (int side = -1; side <= 1; side += 2)
            {
                float xBay = side * hw * 0.42f;
                for (int m = 0; m < 4; m++)
                {
                    float mz = MathHelper.Lerp(len * 0.02f, len * 0.12f, m / 3f);
                    TriMat(w, weapon,
                        new Vector3(xBay, hgt * 0.10f, mz), new Vector3(xBay + side * hw * 0.04f, hgt * 0.08f, mz + len * 0.006f),
                        new Vector3(xBay, hgt * 0.06f, mz + len * 0.01f));
                }
            }
            for (int m = 0; m < 5; m++)
            {
                float mz = MathHelper.Lerp(-len * 0.04f, len * 0.08f, m / 4f);
                TriMat(w, weapon,
                    new Vector3(-hw * 0.18f, hgt * 0.04f, mz), new Vector3(hw * 0.18f, hgt * 0.04f, mz),
                    new Vector3(0, hgt * 0.02f, mz + len * 0.008f));
            }

            for (int side = -1; side <= 1; side += 2)
            {
                float xIn = side * hw * 0.34f;
                float xOut = side * hw * 0.40f;
                TriMat(w, accent,
                    new Vector3(xIn, hgt * 0.16f, len * 0.06f), new Vector3(xOut, hgt * 0.14f, len * 0.10f),
                    new Vector3(xIn, hgt * 0.12f, len * 0.11f));
            }

            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * hw * 0.40f;
                for (int t = 0; t < 3; t++)
                {
                    float z = MathHelper.Lerp(len * 0.06f, len * 0.20f, t / 2f);
                    TriMat(w, frame,
                        new Vector3(x, hgt * 0.28f, z), new Vector3(x + side * hw * 0.03f, hgt * 0.26f, z),
                        new Vector3(x, hgt * 0.24f, z + len * 0.01f));
                }
            }

            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 6; i++)
                {
                    float t = i / 5f;
                    float z = MathHelper.Lerp(-len * 0.05f, len * 0.15f, t);
                    float xOut = side * hw * MathHelper.Lerp(1.02f, 0.60f, t);
                    float xIn = side * hw * MathHelper.Lerp(0.28f, 0.42f, t);
                    float yEdge = hgt * (0.15f + t * 0.04f);
                    TriMat(w, accent,
                        new Vector3(xOut, yEdge, z), new Vector3(xIn, yEdge * 0.92f, z + len * 0.008f),
                        new Vector3(xOut, yEdge * 0.85f, z + len * 0.012f));
                }
            }

            for (int i = 0; i < 5; i++)
            {
                float z = MathHelper.Lerp(-len * 0.10f, len * 0.16f, i / 4f);
                float x = hw * 0.46f;
                TriMat(w, accent,
                    new Vector3(-x, hgt * 0.18f, z), new Vector3(-x + hw * 0.06f, hgt * 0.16f, z + len * 0.01f),
                    new Vector3(-x, hgt * 0.14f, z + len * 0.015f));
                TriMat(w, accent,
                    new Vector3(x, hgt * 0.18f, z), new Vector3(x - hw * 0.06f, hgt * 0.16f, z + len * 0.01f),
                    new Vector3(x, hgt * 0.14f, z + len * 0.015f));
            }

            TriMat(w, accent,
                new Vector3(-hw * 0.10f, hgt * 0.58f, len * 0.14f), new Vector3(hw * 0.10f, hgt * 0.58f, len * 0.14f),
                new Vector3(0, hgt * 0.66f, len * 0.18f));

            for (int b = 0; b < 6; b++)
            {
                float z = MathHelper.Lerp(-len * 0.06f, len * 0.24f, b / 5f);
                TriMat(w, accent,
                    new Vector3(-hw * 0.22f, hgt * 0.36f, z), new Vector3(hw * 0.22f, hgt * 0.36f, z),
                    new Vector3(0, hgt * 0.40f, z + len * 0.008f));
            }

            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * hw * 0.62f;
                TriMat(w, panel,
                    new Vector3(x, hgt * 0.06f, len * 0.02f), new Vector3(x + side * hw * 0.04f, hgt * 0.04f, len * 0.04f),
                    new Vector3(x, hgt * 0.02f, len * 0.05f));
            }

            TriMat(w, accent,
                new Vector3(-hw * 0.02f, hgt * 0.68f, len * 0.16f), new Vector3(hw * 0.02f, hgt * 0.68f, len * 0.18f),
                new Vector3(0, hgt * 0.72f, len * 0.17f));
            for (int k = 0; k < 3; k++)
            {
                float z = MathHelper.Lerp(len * 0.08f, len * 0.20f, k / 2f);
                TriMat(w, accent,
                    new Vector3(-hw * 0.08f, hgt * 0.52f, z), new Vector3(hw * 0.08f, hgt * 0.52f, z),
                    new Vector3(0, hgt * 0.56f, z + len * 0.006f));
            }
            TriMat(w, accent,
                new Vector3(-hw * 0.04f, hgt * 0.62f, len * 0.20f), new Vector3(hw * 0.04f, hgt * 0.62f, len * 0.20f),
                new Vector3(0, hgt * 0.70f, len * 0.22f));
            TriMat(w, accent,
                new Vector3(-hw * 0.03f, hgt * 0.58f, -len * 0.04f), new Vector3(hw * 0.03f, hgt * 0.58f, -len * 0.04f),
                new Vector3(0, hgt * 0.66f, -len * 0.06f));

            for (int side = -1; side <= 1; side += 2)
            {
                float xRoot = side * hw * 0.62f;
                TriMat(w, weapon,
                    new Vector3(xRoot, hgt * 0.08f, len * 0.04f), new Vector3(xRoot - side * hw * 0.06f, hgt * 0.05f, len * 0.07f),
                    new Vector3(xRoot - side * hw * 0.03f, hgt * 0.03f, len * 0.09f));
                TriMat(w, weapon,
                    new Vector3(xRoot, hgt * 0.07f, len * 0.02f), new Vector3(xRoot - side * hw * 0.04f, hgt * 0.04f, len * 0.05f),
                    new Vector3(xRoot, hgt * 0.04f, len * 0.06f));
                TriMat(w, weapon,
                    new Vector3(xRoot - side * hw * 0.02f, hgt * 0.06f, len * 0.03f), new Vector3(xRoot - side * hw * 0.05f, hgt * 0.04f, len * 0.06f),
                    new Vector3(xRoot, hgt * 0.05f, len * 0.05f));
                TriMat(w, weapon,
                    new Vector3(xRoot, hgt * 0.09f, len * 0.01f), new Vector3(xRoot - side * hw * 0.03f, hgt * 0.06f, len * 0.04f),
                    new Vector3(xRoot - side * hw * 0.01f, hgt * 0.07f, len * 0.02f));
            }

            for (int side = -1; side <= 1; side += 2)
            {
                float xLead = side * hw * 1.10f;
                for (int e = 0; e < 3; e++)
                {
                    float z = MathHelper.Lerp(len * 0.04f, -len * 0.02f, e / 2f);
                    TriMat(w, accent,
                        new Vector3(xLead, hgt * (0.20f + e * 0.02f), z),
                        new Vector3(xLead - side * hw * 0.03f, hgt * (0.18f + e * 0.02f), z + len * 0.008f),
                        new Vector3(xLead, hgt * (0.16f + e * 0.02f), z + len * 0.012f));
                }
            }

            for (int side = -1; side <= 1; side += 2)
            {
                float xTip = side * hw * 0.64f;
                float zTip = -len * 0.035f;
                TriMat(w, accent,
                    new Vector3(xTip, hgt * 0.22f, zTip), new Vector3(xTip - side * hw * 0.04f, hgt * 0.20f, zTip + len * 0.01f),
                    new Vector3(xTip, hgt * 0.18f, zTip + len * 0.015f));
                TriMat(w, accent,
                    new Vector3(xTip, hgt * 0.24f, zTip - len * 0.008f), new Vector3(xTip - side * hw * 0.03f, hgt * 0.22f, zTip),
                    new Vector3(xTip, hgt * 0.20f, zTip + len * 0.008f));
            }

            for (int p = 0; p < 8; p++)
            {
                float t = p / 7f;
                float z = MathHelper.Lerp(-len * 0.08f, len * 0.22f, t);
                float xSpan = hw * MathHelper.Lerp(0.38f, 0.12f, t);
                TriMat(w, accent,
                    new Vector3(-xSpan, hgt * (0.34f + t * 0.04f), z), new Vector3(xSpan, hgt * (0.34f + t * 0.04f), z),
                    new Vector3(0, hgt * (0.38f + t * 0.05f), z + len * 0.006f));
            }
            TriMat(w, accent,
                new Vector3(-hw * 0.02f, hgt * 0.74f, len * 0.24f), new Vector3(hw * 0.02f, hgt * 0.74f, len * 0.24f),
                new Vector3(0, hgt * 0.78f, len * 0.26f));
            TriMat(w, accent,
                new Vector3(-hw * 0.03f, hgt * 0.64f, -len * 0.08f), new Vector3(hw * 0.03f, hgt * 0.64f, -len * 0.08f),
                new Vector3(0, hgt * 0.70f, -len * 0.10f));
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

    private static void TriMat(RaceMeshWriter w, RaceMeshWriter.HullMaterial mat, Vector3 a, Vector3 b, Vector3 c)
        => w.TriMat(mat, a.X, a.Y, a.Z, b.X, b.Y, b.Z, c.X, c.Y, c.Z);

    public static void ApplyStationDetail(RaceMeshWriter w, RaceVisualDefinition race, string buildingType, float s)
    {
        Vector3 accent = ToVector3(race.Palette.Accent);
        Vector3 secondary = ToVector3(race.Palette.Secondary);

        AddDockingRing(w, s * 0.65f, secondary);
        AddPerimeterLights(w, s, accent);

        if (race.Style.Equals("truss", StringComparison.OrdinalIgnoreCase))
        {
            AddTrussBooms(w, s, secondary);
            AddSolarArrayWings(w, s, accent);
        }
        switch (buildingType.ToLowerInvariant())
        {
            case "command_center":
                AddCommDishes(w, s, accent, 2);
                AddObservationBand(w, s * 0.85f, s * 0.55f, accent);
                break;
            case "shipyard_small":
            case "shipyard_medium":
            case "shipyard":
            case "shipyard_large":
                AddCraneBooms(w, s, secondary, buildingType.Contains("large") ? 2 : 1);
                break;
            case "defense_turret":
                AddTwinBarrels(w, s, secondary);
                break;
            case "sensor_array":
                AddSensorDishes(w, s, accent, 2);
                break;
            case "power_reactor":
                AddReactorGlowRing(w, s, accent);
                break;
            case "resource_refinery":
                AddPipeStacks(w, s, secondary, 2);
                break;
            case "repair_bay":
                AddServiceArms(w, s, accent);
                break;
            case "supply_depot":
                AddCargoCrates(w, s, secondary);
                break;
        }
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
    }

    private static void AddTrussBooms(RaceMeshWriter w, float s, Vector3 color)
    {
        for (int i = 0; i < 3; i++)
        {
            float z = (i - 1) * s * 0.22f;
            w.TriColored(new Vector3(-s * 0.55f, s * 0.18f, z), new Vector3(s * 0.55f, s * 0.18f, z + s * 0.08f), new Vector3(-s * 0.55f, s * 0.42f, z + s * 0.05f), color);
            w.TriColored(new Vector3(s * 0.55f, s * 0.18f, z + s * 0.08f), new Vector3(s * 0.55f, s * 0.42f, z + s * 0.05f), new Vector3(-s * 0.55f, s * 0.42f, z + s * 0.05f), color * 0.92f);
        }
    }

    private static void AddSolarArrayWings(RaceMeshWriter w, float s, Vector3 accent)
    {
        w.TriColored(new Vector3(-s * 0.95f, s * 0.28f, s * 0.12f), new Vector3(-s * 0.55f, s * 0.28f, s * 0.12f), new Vector3(-s * 0.55f, s * 0.42f, -s * 0.08f), accent);
        w.TriColored(new Vector3(s * 0.55f, s * 0.28f, s * 0.12f), new Vector3(s * 0.95f, s * 0.28f, s * 0.12f), new Vector3(s * 0.55f, s * 0.42f, -s * 0.08f), accent * 0.95f);
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
}
