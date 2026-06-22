namespace SharpOpenGl.Engine.Rendering;

/// <summary>Maps race ids to shader texture pattern indices (0–7).</summary>
public static class RaceTextureIndex
{
    private static readonly string[] OrderedRaces =
    [
        "terran", "vesper", "korath", "aetherian", "nexar", "solari", "voidborn", "cryo",
    ];

    private static readonly Dictionary<string, int> IndexByRace =
        OrderedRaces.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i, StringComparer.OrdinalIgnoreCase);

    public static int Resolve(string? raceId)
    {
        if (string.IsNullOrWhiteSpace(raceId)) return 0;
        return IndexByRace.TryGetValue(raceId, out int index) ? index : 0;
    }

    public static IReadOnlyList<string> AllRaceIds => OrderedRaces;
}