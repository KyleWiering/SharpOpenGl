using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Missions;
using SharpOpenGl.Engine.UI.Screens;

namespace SharpOpenGl.Engine.Config;

/// <summary>Gameplay tuning for sandbox universe sessions (loaded from <c>GameData/Config/sandbox.json</c>).</summary>
public sealed class SandboxConfig
{
    public ResourceAmounts? StartingResources { get; set; }
    public bool SpawnHostileAi { get; set; }
    /// <summary>When true, menu sandbox spawns only <see cref="StartingUnitId"/> — no free base, fleet, or miners.</summary>
    public bool? QuietStart { get; set; }
    public string StartingUnitId { get; set; } = "support_repair";
    public int InitialRevealRadius { get; set; } = 36;
    public int ChunkLoadRadius { get; set; } = 2;

    /// <summary>Documented defaults when <c>sandbox.json</c> is missing or incomplete.</summary>
    public static SandboxConfig Defaults { get; } = new()
    {
        StartingResources = new ResourceAmounts
        {
            Energy = 10_000f,
            Minerals = 10_000f,
            Data = 5_000f,
            Crew = 50f,
        },
        SpawnHostileAi = false,
        QuietStart = true,
        StartingUnitId = "support_repair",
        InitialRevealRadius = 36,
        ChunkLoadRadius = 2,
    };

    /// <summary>Load sandbox config from disk, falling back to <see cref="Defaults"/>.</summary>
    public static SandboxConfig Load(string gameDataRoot)
    {
        string path = Path.Combine(gameDataRoot, "Config", "sandbox.json");
        var loaded = JsonLoader.Load<SandboxConfig>(path);
        if (loaded == null)
            return CloneDefaults();

        return MergeWithDefaults(loaded);
    }

    /// <summary>Apply configured starting balances to the human player pool.</summary>
    public void ApplyStartingResources(ResourceManager resourceManager, int playerId = 1)
    {
        var amounts = StartingResources ?? Defaults.StartingResources!;
        var res = new PlayerResources(
            energyMax: Math.Max(2000f, amounts.Energy),
            mineralsMax: Math.Max(5000f, amounts.Minerals),
            dataMax: Math.Max(1000f, amounts.Data),
            crewMax: Math.Max(200f, amounts.Crew));
        resourceManager.AddPlayer(playerId, res);
        res.SetStartingAmount(ResourceType.Energy, amounts.Energy);
        res.SetStartingAmount(ResourceType.Minerals, amounts.Minerals);
        res.SetStartingAmount(ResourceType.Data, amounts.Data);
        res.SetStartingAmount(ResourceType.Crew, amounts.Crew);
    }

    private static SandboxConfig MergeWithDefaults(SandboxConfig loaded)
    {
        var defaults = Defaults;
        return new SandboxConfig
        {
            StartingResources = loaded.StartingResources ?? defaults.StartingResources,
            SpawnHostileAi = loaded.SpawnHostileAi,
            QuietStart = loaded.QuietStart ?? defaults.QuietStart,
            StartingUnitId = string.IsNullOrWhiteSpace(loaded.StartingUnitId)
                ? defaults.StartingUnitId
                : loaded.StartingUnitId,
            InitialRevealRadius = loaded.InitialRevealRadius > 0
                ? loaded.InitialRevealRadius
                : defaults.InitialRevealRadius,
            ChunkLoadRadius = loaded.ChunkLoadRadius > 0
                ? loaded.ChunkLoadRadius
                : defaults.ChunkLoadRadius,
        };
    }

    private static SandboxConfig CloneDefaults()
    {
        var d = Defaults;
        return new SandboxConfig
        {
            StartingResources = d.StartingResources,
            SpawnHostileAi = d.SpawnHostileAi,
            QuietStart = d.QuietStart ?? true,
            StartingUnitId = d.StartingUnitId,
            InitialRevealRadius = d.InitialRevealRadius,
            ChunkLoadRadius = d.ChunkLoadRadius,
        };
    }
}

/// <summary>Determines whether mission systems participate in a gameplay session.</summary>
public static class SandboxSessionPolicy
{
    /// <summary>True when a mission id should be loaded and started (not sandbox menu flow).</summary>
    public static bool ShouldLoadMission(string? missionId, SandboxSetupResult? sandboxSetup) =>
        !string.IsNullOrWhiteSpace(missionId) && sandboxSetup == null;

    /// <summary>True when objective/trigger ECS systems should be registered for the session.</summary>
    public static bool ShouldRegisterMissionSystems(
        string? missionId,
        SandboxSetupResult? sandboxSetup,
        bool hasActiveMission) =>
        hasActiveMission && sandboxSetup == null && ShouldLoadMission(missionId, sandboxSetup);

    /// <summary>Menu sandbox uses builder-only quiet start when config allows.</summary>
    public static bool ShouldUseQuietStart(SandboxSetupResult? sandboxSetup, SandboxConfig config) =>
        sandboxSetup != null && config.QuietStart != false;
}