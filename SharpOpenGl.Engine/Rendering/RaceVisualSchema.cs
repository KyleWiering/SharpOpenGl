using SharpOpenGl.Engine.Assets;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>Loaded from <c>GameData/Config/race_visuals.json</c> — drives procedural race ship silhouettes.</summary>
public sealed class RaceVisualsConfig
{
    public int Version { get; set; } = 1;
    public RaceVisualDefinition[] Races { get; set; } = [];
    public Dictionary<string, HullClassProfile> HullClasses { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> DefinitionToHull { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class RaceVisualDefinition
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Style { get; set; } = "angular";
    public RacePalette Palette { get; set; } = new();
    public RaceModifiers Modifiers { get; set; } = new();
    public RaceSubstrateDefinition? Substrate { get; set; }
}

public sealed class RacePalette
{
    public float[] Primary { get; set; } = [0.5f, 0.5f, 0.5f];
    public float[] Secondary { get; set; } = [0.35f, 0.35f, 0.35f];
    public float[] Accent { get; set; } = [1f, 0.85f, 0.35f];
    public float[] Engine { get; set; } = [0.2f, 0.6f, 1f];
}

public sealed class RaceModifiers
{
    public float WingSweep { get; set; } = 0.3f;
    public float WingSpan { get; set; } = 1f;
    public float HullLength { get; set; } = 1f;
    public float HullWidth { get; set; } = 1f;
    public float Protrusion { get; set; } = 0.2f;
    public float Asymmetry { get; set; }
    public float FacetSharpness { get; set; }
    public int EngineCount { get; set; } = 2;
    public float Superstructure { get; set; } = 0.3f;
}

public sealed class HullClassProfile
{
    public float Size { get; set; } = 2f;
    public float LengthRatio { get; set; } = 1.2f;
    public float WidthRatio { get; set; } = 0.8f;
    public float HeightRatio { get; set; } = 0.25f;
    public float EngineScale { get; set; } = 1f;
}

/// <summary>Runtime access to race visual schema with baked-in defaults if JSON is missing.</summary>
public static class RaceVisualSchema
{
    private static readonly object LoadLock = new();
    private static RaceVisualsConfig? _config;
    private static readonly Dictionary<string, RaceVisualDefinition> _races = new(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<RaceVisualDefinition> AllRaces
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
            RaceVisualsConfig? loaded = null;
            foreach (string candidate in ResolveConfigPaths(path))
            {
                loaded = JsonLoader.Load<RaceVisualsConfig>(candidate);
                if (loaded != null && loaded.Races.Length > 0)
                    break;
            }

            _config = loaded is { Races.Length: > 0 } ? loaded : CreateDefaults();
            RebuildIndex();
        }
    }

    private static IEnumerable<string> ResolveConfigPaths(string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
            yield return explicitPath;

        yield return Path.Combine("GameData", "Config", "race_visuals.json");

        string? dir = AppContext.BaseDirectory;
        for (int i = 0; i < 6 && dir != null; i++)
        {
            string candidate = Path.Combine(dir, "GameData", "Config", "race_visuals.json");
            if (File.Exists(candidate))
                yield return candidate;
            dir = Directory.GetParent(dir)?.FullName;
        }
    }

    public static void ResetForTests() => Load();

    public static bool TryGetRace(string raceId, out RaceVisualDefinition race)
    {
        EnsureLoaded();
        return _races.TryGetValue(raceId, out race!);
    }

    public static HullClassProfile ResolveHullProfile(string hullOrDefinitionId)
    {
        EnsureLoaded();
        string hullKey = hullOrDefinitionId;
        if (_config!.DefinitionToHull.TryGetValue(hullOrDefinitionId, out string? mapped))
            hullKey = mapped;

        if (_config.HullClasses.TryGetValue(hullKey, out HullClassProfile? profile))
            return profile;

        return _config.HullClasses.TryGetValue("fighter", out HullClassProfile? fallback)
            ? fallback
            : new HullClassProfile { Size = 2f };
    }

    public static string ResolveHullKey(string hullOrDefinitionId)
    {
        EnsureLoaded();
        if (_config!.DefinitionToHull.TryGetValue(hullOrDefinitionId, out string? mapped))
            return mapped;
        if (_config.HullClasses.ContainsKey(hullOrDefinitionId))
            return hullOrDefinitionId;
        return "fighter";
    }

    public static string RaceFromSeed(string seed)
    {
        EnsureLoaded();
        var races = _config!.Races;
        if (races.Length == 0) return "terran";

        int hash = 0;
        foreach (char c in seed)
            hash = hash * 31 + c;

        int index = 1 + Math.Abs(hash) % Math.Max(1, races.Length - 1);
        if (index >= races.Length) index = 0;
        return races[index].Id;
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

    private static RaceVisualsConfig CreateDefaults()
    {
        return new RaceVisualsConfig
        {
            Races =
            [
                new RaceVisualDefinition
                {
                    Id = "terran",
                    DisplayName = "Terran Coalition",
                    Style = "angular",
                    Palette = new RacePalette
                    {
                        Primary = [0.32f, 0.55f, 0.78f],
                        Secondary = [0.18f, 0.32f, 0.52f],
                        Accent = [1f, 0.85f, 0.35f],
                        Engine = [0.2f, 0.65f, 1f],
                    },
                },
            ],
            HullClasses = new Dictionary<string, HullClassProfile>(StringComparer.OrdinalIgnoreCase)
            {
                ["fighter"] = new() { Size = 1.75f, LengthRatio = 1.2f, WidthRatio = 0.7f, HeightRatio = 0.22f },
            },
            DefinitionToHull = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["fighter_basic"] = "fighter",
            },
        };
    }
}