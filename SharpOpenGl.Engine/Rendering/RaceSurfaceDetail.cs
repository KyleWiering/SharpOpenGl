using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>Subtle hull surface accents — kept flush to avoid spiky greeble clutter.</summary>
internal static class RaceSurfaceDetail
{
    public static void ApplyShipDetail(RaceMeshWriter w, RaceVisualDefinition race, string hullKey, float len, float wid, float hgt)
    {
        // Plating variation is baked into vertex substrate + model-space shader panels on the silhouette.
    }

    public static void ApplyStationDetail(RaceMeshWriter w, RaceVisualDefinition race, string buildingType, float s)
    {
        Vector3 accent = ToVector3(race.Palette.Accent);
        Vector3 secondary = ToVector3(race.Palette.Secondary);

        AddDockingRing(w, s * 0.65f, secondary);
        AddPerimeterLights(w, s, accent);

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