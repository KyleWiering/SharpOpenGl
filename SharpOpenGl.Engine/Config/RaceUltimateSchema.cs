using OpenTK.Mathematics;
using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Engine.Config;

/// <summary>Loaded from <c>GameData/Config/race_ultimates.json</c> — per-race hero ultimate weapon.</summary>
public sealed class RaceUltimatesConfig
{
    public int Version { get; set; } = 1;
    public RaceUltimateDefinition[] Ultimates { get; set; } = [];
}

/// <summary>Data-driven ultimate ability parameters for one playable race.</summary>
public sealed class RaceUltimateDefinition
{
    public string RaceId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AbilityId { get; set; } = string.Empty;
    public int Slot { get; set; } = 2;
    public float Cooldown { get; set; } = 90f;
    public float Damage { get; set; }
    public float AoeRadius { get; set; }
    public string EffectType { get; set; } = "aoe";
    public string ProjectileVisual { get; set; } = "bomb";
    public float ProjectileSpeed { get; set; } = 200f;
    public float ProjectileLifetime { get; set; } = 4f;
    public int SalvoCount { get; set; } = 1;
    public float DisableDuration { get; set; }

    /// <summary>RGB overlay tint for ultimate cast VFX (0–1 per channel).</summary>
    public float[]? CastTint { get; set; }

    /// <summary>Resolves cast overlay tint, falling back to race accent palette.</summary>
    public Vector3 ResolveCastTint()
    {
        if (CastTint is { Length: >= 3 })
            return new Vector3(CastTint[0], CastTint[1], CastTint[2]);

        if (!string.IsNullOrEmpty(RaceId)
            && RaceVisualSchema.TryGetRace(RaceId, out var race)
            && race.Palette.Accent.Length >= 3)
        {
            return new Vector3(
                race.Palette.Accent[0],
                race.Palette.Accent[1],
                race.Palette.Accent[2]);
        }

        return new Vector3(0.55f, 0.85f, 1f);
    }
}

/// <summary>Runtime access to race ultimate definitions with baked-in defaults if JSON is missing.</summary>
public static class RaceUltimateSchema
{
    private static readonly object LoadLock = new();
    private static RaceUltimatesConfig? _config;
    private static readonly Dictionary<string, RaceUltimateDefinition> _byRace =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, RaceUltimateDefinition> _byAbilityId =
        new(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<RaceUltimateDefinition> AllUltimates
    {
        get
        {
            EnsureLoaded();
            return _config?.Ultimates ?? [];
        }
    }

    public static void Load(string? path = null)
    {
        lock (LoadLock)
        {
            RaceUltimatesConfig? loaded = null;
            foreach (string candidate in ResolveConfigPaths(path))
            {
                loaded = JsonLoader.Load<RaceUltimatesConfig>(candidate);
                if (loaded != null && loaded.Ultimates.Length > 0)
                    break;
            }

            _config = loaded is { Ultimates.Length: > 0 } ? loaded : CreateDefaults();
            RebuildIndex();
        }
    }

    public static void ResetForTests() => Load();

    public static bool TryGetForRace(string raceId, out RaceUltimateDefinition ultimate)
    {
        EnsureLoaded();
        return _byRace.TryGetValue(raceId, out ultimate!);
    }

    public static bool TryGetByAbilityId(string abilityId, out RaceUltimateDefinition ultimate)
    {
        EnsureLoaded();
        return _byAbilityId.TryGetValue(abilityId, out ultimate!);
    }

    /// <summary>Resolves ultimate cast overlay tint from <c>race_ultimates.json</c> <c>castTint</c>.</summary>
    public static Vector3 ResolveCastTint(string raceId, string? abilityId = null)
    {
        if (!string.IsNullOrEmpty(abilityId)
            && TryGetByAbilityId(abilityId, out RaceUltimateDefinition? byAbility))
            return byAbility.ResolveCastTint();

        if (!string.IsNullOrEmpty(raceId)
            && TryGetForRace(raceId, out RaceUltimateDefinition? byRace))
            return byRace.ResolveCastTint();

        return new Vector3(0.55f, 0.85f, 1f);
    }

    private static void EnsureLoaded()
    {
        if (_config == null) Load();
    }

    private static IEnumerable<string> ResolveConfigPaths(string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
            yield return explicitPath;

        yield return Path.Combine("GameData", "Config", "race_ultimates.json");

        string? dir = AppContext.BaseDirectory;
        for (int i = 0; i < 6 && dir != null; i++)
        {
            string candidate = Path.Combine(dir, "GameData", "Config", "race_ultimates.json");
            if (File.Exists(candidate))
                yield return candidate;
            dir = Directory.GetParent(dir)?.FullName;
        }
    }

    private static void RebuildIndex()
    {
        _byRace.Clear();
        _byAbilityId.Clear();
        if (_config?.Ultimates == null) return;

        foreach (var ultimate in _config.Ultimates)
        {
            if (!string.IsNullOrWhiteSpace(ultimate.RaceId))
                _byRace[ultimate.RaceId] = ultimate;
            if (!string.IsNullOrWhiteSpace(ultimate.AbilityId))
                _byAbilityId[ultimate.AbilityId] = ultimate;
        }
    }

    private static RaceUltimatesConfig CreateDefaults() => new()
    {
        Ultimates =
        [
            new RaceUltimateDefinition
            {
                RaceId = "terran", DisplayName = "Orbital Salvo", AbilityId = "terran_orbital_salvo",
                Slot = 2, Cooldown = 90, Damage = 200, AoeRadius = 50, EffectType = "aoe",
                ProjectileVisual = "bomb", ProjectileSpeed = 180, ProjectileLifetime = 3.5f, SalvoCount = 3,
                CastTint = [0.95f, 0.42f, 0.22f],
            },
            new RaceUltimateDefinition
            {
                RaceId = "vesper", DisplayName = "Precision Beam", AbilityId = "vesper_precision_beam",
                Slot = 2, Cooldown = 75, Damage = 350, EffectType = "beam", ProjectileVisual = "beam",
                CastTint = [0.62f, 0.82f, 0.95f],
            },
            new RaceUltimateDefinition
            {
                RaceId = "korath", DisplayName = "Siege Barrage", AbilityId = "korath_siege_barrage",
                Slot = 2, Cooldown = 85, Damage = 85, AoeRadius = 35, EffectType = "aoe",
                ProjectileVisual = "rocket", ProjectileSpeed = 280, ProjectileLifetime = 5, SalvoCount = 6,
                CastTint = [0.88f, 0.28f, 0.18f],
            },
            new RaceUltimateDefinition
            {
                RaceId = "aetherian", DisplayName = "Plague Cloud", AbilityId = "aetherian_plague_cloud",
                Slot = 2, Cooldown = 80, Damage = 140, AoeRadius = 55, EffectType = "aoe",
                ProjectileVisual = "wave", ProjectileSpeed = 160, ProjectileLifetime = 4,
                CastTint = [0.45f, 0.92f, 0.38f],
            },
            new RaceUltimateDefinition
            {
                RaceId = "nexar", DisplayName = "Swarm Strike", AbilityId = "nexar_swarm_strike",
                Slot = 2, Cooldown = 70, Damage = 55, AoeRadius = 40, EffectType = "aoe",
                ProjectileVisual = "rocket", ProjectileSpeed = 340, ProjectileLifetime = 4.5f, SalvoCount = 8,
                CastTint = [0.98f, 0.78f, 0.15f],
            },
            new RaceUltimateDefinition
            {
                RaceId = "solari", DisplayName = "Solar Nova", AbilityId = "solari_solar_nova",
                Slot = 2, Cooldown = 95, Damage = 250, AoeRadius = 65, EffectType = "aoe",
                ProjectileVisual = "energy_pulse", ProjectileLifetime = 0.5f,
                CastTint = [1f, 0.88f, 0.35f],
            },
            new RaceUltimateDefinition
            {
                RaceId = "voidborn", DisplayName = "Gravity Rift", AbilityId = "voidborn_gravity_rift",
                Slot = 2, Cooldown = 88, Damage = 60, AoeRadius = 45, EffectType = "disable",
                ProjectileVisual = "wave", DisableDuration = 4,
                CastTint = [0.55f, 0.25f, 0.88f],
            },
            new RaceUltimateDefinition
            {
                RaceId = "cryo", DisplayName = "Freeze Field", AbilityId = "cryo_freeze_field",
                Slot = 2, Cooldown = 82, Damage = 40, AoeRadius = 50, EffectType = "disable",
                ProjectileVisual = "beam", DisableDuration = 3.5f,
                CastTint = [0.55f, 0.85f, 1f],
            },
        ],
    };
}