using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Persistence;
using SharpOpenGl.Engine.UI.Screens;
using Xunit;

namespace SharpOpenGl.Tests.Gameplay;

public class SandboxBootstrapTests
{
    [Fact]
    public void SandboxSetupResult_seed_threads_to_procedural_map_seed()
    {
        const string seedText = "frontier-alpha";
        var setup = new SandboxSetupResult(seedText, ProceduralSeedHelper.ParseSeed(seedText));

        Assert.Equal(ProceduralSeedHelper.HashString(seedText), setup.ParsedSeed);
        Assert.Equal(setup.ParsedSeed, ProceduralSeedHelper.ParseSeed(seedText));
        Assert.Equal(SandboxChunkCoords.ChunkSeed(setup.ParsedSeed, 0, 0),
            SandboxChunkCoords.ChunkSeed(ProceduralSeedHelper.ParseSeed(seedText), 0, 0));
    }

    [Fact]
    public void InitializeWorldCore_sandbox_skips_mission_controller()
    {
        var setup = new SandboxSetupResult("42", 42);

        Assert.False(SandboxSessionPolicy.ShouldLoadMission(null, setup));
        Assert.False(SandboxSessionPolicy.ShouldRegisterMissionSystems(null, setup, hasActiveMission: true));
        Assert.True(SandboxSessionPolicy.ShouldLoadMission("tutorial_01", sandboxSetup: null));
        Assert.True(SandboxSessionPolicy.ShouldRegisterMissionSystems("tutorial_01", null, hasActiveMission: true));
    }

    [Fact]
    public void SetupSandboxWorld_does_not_register_objective_system()
    {
        var setup = new SandboxSetupResult("sandbox-test", 9001);

        Assert.False(SandboxSessionPolicy.ShouldRegisterMissionSystems(null, setup, hasActiveMission: false));
        Assert.False(SandboxSessionPolicy.ShouldRegisterMissionSystems(null, setup, hasActiveMission: true));
    }

    [Fact]
    public void Sandbox_session_policy_treats_loaded_sandbox_as_objective_free()
    {
        const string seedText = "demo-42";
        var saveData = new SaveData
        {
            IsSandboxSession = true,
            ProceduralMapSeed = ProceduralSeedHelper.ParseSeed(seedText),
            SandboxSeedText = seedText,
        };
        var setup = new SandboxSetupResult(saveData.SandboxSeedText, saveData.ProceduralMapSeed);

        Assert.False(SandboxSessionPolicy.ShouldRegisterMissionSystems(null, setup, hasActiveMission: false));
        Assert.False(SandboxSessionPolicy.ShouldRegisterMissionSystems(null, setup, hasActiveMission: true));
        Assert.False(SandboxSessionPolicy.ShouldLoadMission(null, setup));
    }

    [Fact]
    public void Sandbox_starting_resources_applied()
    {
        string gameDataRoot = ResolveGameDataRoot();
        var config = SandboxConfig.Load(gameDataRoot);
        var resourceManager = new ResourceManager();

        config.ApplyStartingResources(resourceManager, playerId: 1);
        var player = resourceManager.GetPlayer(1);
        Assert.NotNull(player);

        var amounts = config.StartingResources ?? SandboxConfig.Defaults.StartingResources!;
        Assert.Equal(amounts.Energy, player!.GetAmount(ResourceType.Energy));
        Assert.Equal(amounts.Minerals, player.GetAmount(ResourceType.Minerals));
        Assert.Equal(amounts.Data, player.GetAmount(ResourceType.Data));
        Assert.Equal(amounts.Crew, player.GetAmount(ResourceType.Crew));
    }

    [Fact]
    public void Sandbox_quiet_start_policy_applies_for_menu_flow()
    {
        var setup = new SandboxSetupResult("quiet", 42);
        var config = SandboxConfig.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));

        Assert.True(SandboxSessionPolicy.ShouldUseQuietStart(setup, config));
        Assert.False(SandboxSessionPolicy.ShouldUseQuietStart(null, config));

        config.QuietStart = false;
        Assert.False(SandboxSessionPolicy.ShouldUseQuietStart(setup, config));
    }

    [Fact]
    public void Sandbox_config_uses_documented_defaults_when_file_missing()
    {
        string missingRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var config = SandboxConfig.Load(missingRoot);

        Assert.False(config.SpawnHostileAi);
        Assert.NotEqual(false, config.QuietStart);
        Assert.Equal("support_repair", config.StartingUnitId);
        Assert.Equal(36, config.InitialRevealRadius);
        Assert.Equal(2, config.ChunkLoadRadius);
        Assert.Equal(10_000f, config.StartingResources!.Energy);
        Assert.Equal(10_000f, config.StartingResources.Minerals);
        Assert.Equal(5_000f, config.StartingResources.Data);
        Assert.Equal(50f, config.StartingResources.Crew);
    }

    private static string ResolveGameDataRoot()
    {
        string dir = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            string candidate = Path.Combine(dir, "GameData");
            if (Directory.Exists(candidate))
                return candidate;

            string? parent = Directory.GetParent(dir)?.FullName;
            if (parent == null)
                break;
            dir = parent;
        }

        throw new InvalidOperationException("GameData root not found for sandbox config test.");
    }
}