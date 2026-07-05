using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>Race-styled station geometry — one mesh per building type × race substrate.</summary>
public static class RaceStationMeshes
{
    public static float[] Build(string buildingType, string raceId)
    {
        RaceVisualSchema.TryGetRace(raceId, out RaceVisualDefinition? race);
        race ??= RaceVisualSchema.AllRaces.FirstOrDefault() ?? new RaceVisualDefinition { Id = RaceShipMeshes.DefaultRace };

        Vector3 primary = ToVector3(race.Palette.Primary);
        Vector3 secondary = ToVector3(race.Palette.Secondary);
        Vector3 accent = ToVector3(race.Palette.Accent);
        var writer = new RaceMeshWriter();
        float styleScale = 0.85f + race.Modifiers.Superstructure * 0.3f;

        switch (buildingType.ToLowerInvariant())
        {
            case "command_center":
                BuildCommandCenter(writer, race, primary, secondary, accent, 7f * styleScale);
                break;
            case "shipyard_small":
                BuildShipyard(writer, race, primary, secondary, accent, 5.5f * styleScale, 1, 2);
                break;
            case "shipyard_medium":
            case "shipyard":
                BuildShipyard(writer, race, primary, secondary, accent, 7f * styleScale, 2, 3);
                break;
            case "shipyard_large":
                BuildShipyard(writer, race, primary, secondary, accent, 9f * styleScale, 3, 5);
                break;
            case "defense_turret":
                BuildDefenseTurret(writer, race, secondary, accent, 3.5f * styleScale);
                break;
            case "sensor_array":
                BuildSensorArray(writer, race, secondary, accent, 4.5f * styleScale);
                break;
            case "resource_refinery":
                BuildRefinery(writer, race, primary, secondary, 6f * styleScale);
                break;
            case "repair_bay":
                BuildRepairBay(writer, race, primary, secondary, accent, 6.5f * styleScale);
                break;
            case "power_reactor":
                BuildReactor(writer, race, accent, secondary, 5f * styleScale);
                break;
            case "supply_depot":
                BuildSupplyDepot(writer, race, primary, secondary, 4.5f * styleScale);
                break;
            default:
                BuildCommandCenter(writer, race, primary, secondary, accent, 6f * styleScale);
                break;
        }

        float detailScale = 0.85f + race.Modifiers.Superstructure * 0.3f;
        RaceSurfaceDetail.ApplyStationDetail(writer, race, buildingType, 7f * detailScale);
        writer.ApplySubstrateVariation(RaceSubstrateProfile.ForRace(race));
        writer.ApplyBakedLighting(new Vector3(0.3f, 0.88f, 0.35f));
        return writer.ToArray();
    }

    private static void BuildCommandCenter(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        for (int i = 0; i < 8; i++)
        {
            float a0 = MathF.PI * 2f * i / 8f;
            float a1 = MathF.PI * 2f * (i + 1) / 8f;
            var p0 = new Vector3(MathF.Cos(a0) * s, 0f, MathF.Sin(a0) * s);
            var p1 = new Vector3(MathF.Cos(a1) * s, 0f, MathF.Sin(a1) * s);
            var p2 = new Vector3(MathF.Cos(a0) * s * 0.55f, s * 0.2f, MathF.Sin(a0) * s * 0.55f);
            var p3 = new Vector3(MathF.Cos(a1) * s * 0.55f, s * 0.2f, MathF.Sin(a1) * s * 0.55f);
            w.TriColored(p0, p1, p2, i % 2 == 0 ? primary : secondary);
            w.TriColored(p1, p3, p2, secondary);
            w.TriColored(Vector3.Zero, p0, p1, secondary * 0.85f);
        }

        w.TriColored(new Vector3(0, s * 0.2f, 0), new Vector3(-s * 0.2f, s * 1.1f, 0), new Vector3(s * 0.2f, s * 1.1f, 0), accent);
        w.TriColored(new Vector3(-s * 0.2f, s * 1.1f, 0), new Vector3(0, s * 1.5f, s * 0.15f), new Vector3(s * 0.2f, s * 1.1f, 0), accent * 1.1f);
        ApplyRaceStationFins(w, race, s, s * 0.35f);
    }

    private static void BuildShipyard(RaceMeshWriter w, RaceVisualDefinition race,
        Vector3 primary, Vector3 secondary, Vector3 accent, float s, int gantries, int bays)
    {
        w.TriColored(new Vector3(-s, 0, -s * 0.6f), new Vector3(s, 0, -s * 0.6f), new Vector3(s, 0, s * 0.6f), primary);
        w.TriColored(new Vector3(-s, 0, -s * 0.6f), new Vector3(s, 0, s * 0.6f), new Vector3(-s, 0, s * 0.6f), secondary);
        w.TriColored(new Vector3(-s * 0.9f, s * 0.15f, -s * 0.5f), new Vector3(s * 0.9f, s * 0.15f, -s * 0.5f), new Vector3(s * 0.9f, s * 0.15f, s * 0.5f), secondary);
        w.TriColored(new Vector3(-s * 0.9f, s * 0.15f, -s * 0.5f), new Vector3(s * 0.9f, s * 0.15f, s * 0.5f), new Vector3(-s * 0.9f, s * 0.15f, s * 0.5f), primary * 0.9f);

        for (int g = 0; g < gantries; g++)
        {
            float x = (g - (gantries - 1) * 0.5f) * s * 0.55f;
            w.TriColored(new Vector3(x, 0, -s * 0.55f), new Vector3(x + s * 0.08f, s * 0.7f, -s * 0.55f), new Vector3(x, s * 0.7f, s * 0.55f), accent);
            w.TriColored(new Vector3(x + s * 0.08f, s * 0.7f, -s * 0.55f), new Vector3(x + s * 0.08f, s * 0.7f, s * 0.55f), new Vector3(x, s * 0.7f, s * 0.55f), accent * 0.85f);
        }

        for (int b = 0; b < bays; b++)
        {
            float z = -s * 0.35f + b * s * 0.28f;
            w.TriColored(new Vector3(-s * 0.35f, s * 0.16f, z), new Vector3(s * 0.35f, s * 0.16f, z), new Vector3(0, s * 0.35f, z + s * 0.12f), primary * 0.95f);
        }

        ApplyRaceStationFins(w, race, s * 0.8f, s * 0.22f);
    }

    private static void BuildDefenseTurret(RaceMeshWriter w, RaceVisualDefinition race, Vector3 secondary, Vector3 accent, float s)
    {
        w.TriColored(new Vector3(0, s * 0.35f, 0), new Vector3(-s * 0.4f, 0, -s * 0.35f), new Vector3(s * 0.4f, 0, -s * 0.35f), secondary);
        w.TriColored(new Vector3(-s * 0.4f, 0, -s * 0.35f), new Vector3(s * 0.4f, 0, s * 0.35f), new Vector3(s * 0.4f, 0, -s * 0.35f), secondary * 0.9f);
        w.TriColored(new Vector3(-s * 0.4f, 0, -s * 0.35f), new Vector3(-s * 0.4f, 0, s * 0.35f), new Vector3(s * 0.4f, 0, s * 0.35f), secondary * 0.85f);
        w.TriColored(new Vector3(0, s * 0.15f, s * 0.55f), new Vector3(-s * 0.12f, s * 0.1f, s * 0.35f), new Vector3(s * 0.12f, s * 0.1f, s * 0.35f), accent);
        w.TriColored(new Vector3(0, s * 0.15f, s * 0.55f), new Vector3(s * 0.12f, s * 0.1f, s * 0.35f), new Vector3(0, s * 0.15f, s * 0.75f), accent * 1.15f);
        if (race.Modifiers.Protrusion > 0.3f)
            w.TriColored(new Vector3(0, s * 0.2f, s * 0.8f), new Vector3(-s * 0.08f, s * 0.12f, s * 0.65f), new Vector3(s * 0.08f, s * 0.12f, s * 0.65f), accent);
    }

    private static void BuildSensorArray(RaceMeshWriter w, RaceVisualDefinition race, Vector3 secondary, Vector3 accent, float s)
    {
        w.TriColored(new Vector3(0, s * 0.55f, 0), new Vector3(-s * 0.08f, 0, 0), new Vector3(s * 0.08f, 0, 0), secondary);
        w.TriColored(new Vector3(-s * 0.45f, s * 0.08f, 0), new Vector3(s * 0.45f, s * 0.08f, 0), new Vector3(0, s * 0.55f, 0), secondary * 0.9f);
        w.TriColored(new Vector3(-s * 0.35f, s * 0.12f, -s * 0.2f), new Vector3(s * 0.35f, s * 0.12f, -s * 0.2f), new Vector3(0, s * 0.45f, s * 0.15f), accent);
        w.TriColored(new Vector3(-s * 0.35f, s * 0.12f, s * 0.2f), new Vector3(0, s * 0.45f, s * 0.15f), new Vector3(s * 0.35f, s * 0.12f, s * 0.2f), accent * 0.9f);
    }

    private static void BuildRefinery(RaceMeshWriter w, RaceVisualDefinition race, Vector3 primary, Vector3 secondary, float s)
    {
        w.TriColored(new Vector3(-s * 0.5f, 0, -s * 0.4f), new Vector3(s * 0.5f, 0, -s * 0.4f), new Vector3(s * 0.5f, 0, s * 0.4f), primary);
        w.TriColored(new Vector3(-s * 0.5f, 0, -s * 0.4f), new Vector3(s * 0.5f, 0, s * 0.4f), new Vector3(-s * 0.5f, 0, s * 0.4f), secondary);
        int stacks = Math.Clamp(2 + race.Modifiers.EngineCount / 2, 2, 5);
        for (int i = 0; i < stacks; i++)
        {
            float x = (i - (stacks - 1) * 0.5f) * s * 0.35f;
            w.TriColored(new Vector3(x, 0, -s * 0.15f), new Vector3(x + s * 0.12f, s * 0.55f, -s * 0.15f), new Vector3(x, s * 0.55f, s * 0.15f), secondary * 0.95f);
            w.TriColored(new Vector3(x + s * 0.12f, s * 0.55f, -s * 0.15f), new Vector3(x + s * 0.12f, s * 0.55f, s * 0.15f), new Vector3(x, s * 0.55f, s * 0.15f), primary * 0.9f);
        }
    }

    private static void BuildRepairBay(RaceMeshWriter w, RaceVisualDefinition race, Vector3 primary, Vector3 secondary, Vector3 accent, float s)
    {
        w.TriColored(new Vector3(-s * 0.55f, 0, -s * 0.35f), new Vector3(s * 0.55f, 0, -s * 0.35f), new Vector3(s * 0.55f, s * 0.35f, -s * 0.35f), primary);
        w.TriColored(new Vector3(-s * 0.55f, 0, -s * 0.35f), new Vector3(s * 0.55f, s * 0.35f, -s * 0.35f), new Vector3(-s * 0.55f, s * 0.35f, -s * 0.35f), secondary);
        w.TriColored(new Vector3(-s * 0.55f, 0, s * 0.35f), new Vector3(s * 0.55f, 0, s * 0.35f), new Vector3(s * 0.55f, s * 0.35f, s * 0.35f), primary * 0.95f);
        w.TriColored(new Vector3(-s * 0.55f, 0, s * 0.35f), new Vector3(s * 0.55f, s * 0.35f, s * 0.35f), new Vector3(-s * 0.55f, s * 0.35f, s * 0.35f), secondary * 0.9f);
        w.TriColored(new Vector3(-s * 0.3f, s * 0.05f, 0), new Vector3(s * 0.3f, s * 0.05f, 0), new Vector3(0, s * 0.25f, s * 0.2f), accent);
    }

    private static void BuildReactor(RaceMeshWriter w, RaceVisualDefinition race, Vector3 accent, Vector3 secondary, float s)
    {
        int segments = race.Style == "crystalline" ? 10 : 8;
        for (int i = 0; i < segments; i++)
        {
            float a0 = MathF.PI * 2f * i / segments;
            float a1 = MathF.PI * 2f * (i + 1) / segments;
            var p0 = new Vector3(MathF.Cos(a0) * s * 0.4f, 0, MathF.Sin(a0) * s * 0.4f);
            var p1 = new Vector3(MathF.Cos(a1) * s * 0.4f, 0, MathF.Sin(a1) * s * 0.4f);
            var top = new Vector3(0, s * 0.5f, 0);
            w.TriColored(p0, p1, top, i % 2 == 0 ? accent : accent * 0.85f);
            w.TriColored(p0, p1, Vector3.Zero, secondary);
        }

        w.TriColored(new Vector3(0, s * 0.52f, 0), new Vector3(-s * 0.08f, s * 0.62f, 0), new Vector3(s * 0.08f, s * 0.62f, 0), accent * 1.2f);
    }

    private static void BuildSupplyDepot(RaceMeshWriter w, RaceVisualDefinition race, Vector3 primary, Vector3 secondary, float s)
    {
        for (int c = 0; c < 4; c++)
        {
            float ox = (c % 2 == 0 ? -1 : 1) * s * 0.28f;
            float oz = (c < 2 ? -1 : 1) * s * 0.22f;
            w.TriColored(new Vector3(ox, 0, oz), new Vector3(ox + s * 0.22f, 0, oz), new Vector3(ox, s * 0.28f, oz + s * 0.18f), c % 2 == 0 ? primary : secondary);
            w.TriColored(new Vector3(ox + s * 0.22f, 0, oz), new Vector3(ox + s * 0.22f, s * 0.28f, oz + s * 0.18f), new Vector3(ox, s * 0.28f, oz + s * 0.18f), secondary * 0.9f);
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
                w.TriColored(new Vector3(0, h, s * 0.5f), new Vector3(-s * 0.15f, h * 0.4f, s * 0.62f), new Vector3(s * 0.15f, h * 0.4f, s * 0.62f), ToVector3(race.Palette.Accent) * 1.1f);
                break;
            case "spiny":
                w.TriColored(new Vector3(0, h * 0.8f, s * 0.6f), new Vector3(-s * 0.1f, h * 0.3f, s * 0.5f), new Vector3(s * 0.1f, h * 0.3f, s * 0.5f), ToVector3(race.Palette.Accent));
                break;
            case "asymmetric":
                w.TriColored(new Vector3(s * 0.2f, h * 0.5f, s * 0.55f), new Vector3(s * 0.45f, h * 0.25f, s * 0.7f), new Vector3(s * 0.05f, h * 0.2f, s * 0.65f), ToVector3(race.Palette.Accent));
                break;
            case "radiant":
                w.TriColored(new Vector3(-s * 0.5f, h * 0.15f, s * 0.5f), new Vector3(s * 0.5f, h * 0.15f, s * 0.5f), new Vector3(0, h * 0.45f, s * 0.72f), ToVector3(race.Palette.Accent) * 1.05f);
                break;
            default:
                w.TriColored(new Vector3(-s * 0.12f, h * 0.4f, s * 0.55f), new Vector3(0, h * 0.75f, s * 0.65f), new Vector3(s * 0.12f, h * 0.4f, s * 0.55f), ToVector3(race.Palette.Accent));
                break;
        }
    }

    private static Vector3 ToVector3(float[] rgb) =>
        rgb.Length >= 3 ? new Vector3(rgb[0], rgb[1], rgb[2]) : Vector3.One;
}