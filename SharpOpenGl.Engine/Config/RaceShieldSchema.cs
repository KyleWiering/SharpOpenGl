using OpenTK.Mathematics;
using SharpOpenGl.Engine.Assets;

namespace SharpOpenGl.Engine.Config;

/// <summary>Loaded from <c>GameData/Config/race_shields.json</c> — per-race shield doctrine.</summary>
public sealed class RaceShieldsConfig
{
    public int Version { get; set; } = 1;
    public RaceShieldDefinition[] Races { get; set; } = [];
}

public sealed class RaceShieldDefinition
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool HasShields { get; set; } = true;
    public float ShieldMultiplier { get; set; } = 1f;
    public float? RegenPerSecond { get; set; }
    public float[] ShieldTint { get; set; } = [0.3f, 0.6f, 1f];
}

/// <summary>Runtime access to race shield doctrine with baked-in defaults if JSON is missing.</summary>
public static class RaceShieldSchema
{
    private static readonly object LoadLock = new();
    private static RaceShieldsConfig? _config;
    private static readonly Dictionary<string, RaceShieldDefinition> _races =
        new(StringComparer.OrdinalIgnoreCase);

    public static readonly Vector4 DefaultShieldTint = new(0.3f, 0.6f, 1f, 1f);

    public static IReadOnlyList<RaceShieldDefinition> AllRaces
    {
        get
        {
            EnsureLoaded();
            return _config?.Races ?? [];
        }
    }

    public static void Load(string? path = null)
    {
        lock (LoadLock)
        {
            RaceShieldsConfig? loaded = null;
            foreach (string candidate in ResolveConfigPaths(path))
            {
                loaded = JsonLoader.Load<RaceShieldsConfig>(candidate);
                if (loaded != null && loaded.Races.Length > 0)
                    break;
            }

            _config = loaded is { Races.Length: > 0 } ? loaded : CreateDefaults();
            RebuildIndex();
        }
    }

    public static void ResetForTests() => Load();

    public static bool TryGetRace(string raceId, out RaceShieldDefinition race)
    {
        EnsureLoaded();
        return _races.TryGetValue(raceId, out race!);
    }

    public static Vector4 ResolveShieldTint(string raceId)
    {
        if (!TryGetRace(raceId, out RaceShieldDefinition? race))
            return DefaultShieldTint;

        float[] tint = race.ShieldTint;
        if (tint.Length < 3)
            return DefaultShieldTint;

        return new Vector4(tint[0], tint[1], tint[2], 1f);
    }

    private static IEnumerable<string> ResolveConfigPaths(string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
            yield return explicitPath;

        yield return Path.Combine("GameData", "Config", "race_shields.json");

        string? dir = AppContext.BaseDirectory;
        for (int i = 0; i < 6 && dir != null; i++)
        {
            string candidate = Path.Combine(dir, "GameData", "Config", "race_shields.json");
            if (File.Exists(candidate))
                yield return candidate;
            dir = Directory.GetParent(dir)?.FullName;
        }
    }

    private static void EnsureLoaded()
    {
        if (_config == null) Load();
    }

    private static void RebuildIndex()
    {
        _races.Clear();
        if (_config?.Races == null) return;
        foreach (var race in _config.Races)
        {
            if (!string.IsNullOrWhiteSpace(race.Id))
                _races[race.Id] = race;
        }
    }

    private static RaceShieldsConfig CreateDefaults() => new()
    {
        Races =
        [
            new RaceShieldDefinition
            {
                Id = "terran", DisplayName = "Terran Coalition",
                HasShields = true, ShieldMultiplier = 1f, RegenPerSecond = 8,
                ShieldTint = [0.35f, 0.65f, 1f],
            },
            new RaceShieldDefinition
            {
                Id = "vesper", DisplayName = "Vesper Syndicate",
                HasShields = true, ShieldMultiplier = 0.75f, RegenPerSecond = 5,
                ShieldTint = [0.2f, 0.9f, 1f],
            },
            new RaceShieldDefinition
            {
                Id = "korath", DisplayName = "Korath Dominion",
                HasShields = false, ShieldMultiplier = 0f,
                ShieldTint = [0.6f, 0.3f, 0.2f],
            },
            new RaceShieldDefinition
            {
                Id = "aetherian", DisplayName = "Aetherian Collective",
                HasShields = true, ShieldMultiplier = 1.1f, RegenPerSecond = 12,
                ShieldTint = [0.55f, 0.35f, 1f],
            },
            new RaceShieldDefinition
            {
                Id = "nexar", DisplayName = "Nexar Hive",
                HasShields = false, ShieldMultiplier = 0f,
                ShieldTint = [0.75f, 0.55f, 0.15f],
            },
            new RaceShieldDefinition
            {
                Id = "solari", DisplayName = "Solari Ascendancy",
                HasShields = true, ShieldMultiplier = 1.25f, RegenPerSecond = 15,
                ShieldTint = [1f, 0.85f, 0.4f],
            },
            new RaceShieldDefinition
            {
                Id = "voidborn", DisplayName = "Voidborn Exiles",
                HasShields = true, ShieldMultiplier = 0.5f, RegenPerSecond = 3,
                ShieldTint = [0.55f, 0.2f, 0.9f],
            },
            new RaceShieldDefinition
            {
                Id = "cryo", DisplayName = "Cryo Legion",
                HasShields = true, ShieldMultiplier = 1.15f, RegenPerSecond = 10,
                ShieldTint = [0.5f, 0.9f, 1f],
            },
        ],
    };
}