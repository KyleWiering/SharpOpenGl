using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.Missions;
using Xunit;

namespace SharpOpenGl.Tests.Missions;

public class MissionLoaderTests
{
    private static string GetTestDataPath()
    {
        // Navigate from test bin output to GameData
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "SharpOpenGl.sln")))
            dir = Directory.GetParent(dir)?.FullName;

        if (dir == null)
            throw new InvalidOperationException(
                "Could not locate SharpOpenGl.sln from base directory.");

        return Path.Combine(dir, "GameData");
    }

    private static MissionLoader CreateLoader()
    {
        var assets = new AssetManager(GetTestDataPath());
        return new MissionLoader(assets);
    }

    // ── Load single mission ──────────────────────────────────────────────────

    [Fact]
    public void Load_returns_mission_for_valid_file()
    {
        var loader = CreateLoader();
        var mission = loader.Load("tutorial_01");

        Assert.NotNull(mission);
        Assert.Equal("tutorial_01", mission.Id);
        Assert.Equal("First Contact", mission.DisplayName);
    }

    [Fact]
    public void Load_returns_null_for_missing_file()
    {
        var loader = CreateLoader();
        var mission = loader.Load("nonexistent_mission");

        Assert.Null(mission);
    }

    [Fact]
    public void Load_caches_result()
    {
        var loader = CreateLoader();
        var first = loader.Load("tutorial_01");
        var second = loader.Load("tutorial_01");

        Assert.Same(first, second);
    }

    // ── Mission fields ───────────────────────────────────────────────────────

    [Fact]
    public void Load_parses_map_reference()
    {
        var loader = CreateLoader();
        var mission = loader.Load("tutorial_01")!;

        Assert.Equal("sector_alpha", mission.Map);
    }

    [Fact]
    public void Load_parses_briefing()
    {
        var loader = CreateLoader();
        var mission = loader.Load("tutorial_01")!;

        Assert.NotNull(mission.Briefing);
        Assert.Contains("enemy scout", mission.Briefing.Text);
        Assert.NotEmpty(mission.Briefing.ObjectivesPreview);
    }

    [Fact]
    public void Load_parses_start_conditions()
    {
        var loader = CreateLoader();
        var mission = loader.Load("tutorial_01")!;

        Assert.NotNull(mission.StartConditions);
        Assert.Equal(new[] { 5, 5 }, mission.StartConditions.PlayerSpawn);
        Assert.Contains("hero_default", mission.StartConditions.StartingUnits);
        Assert.NotNull(mission.StartConditions.StartingResources);
        Assert.Equal(500f, mission.StartConditions.StartingResources.Energy);
    }

    [Fact]
    public void Load_parses_objectives()
    {
        var loader = CreateLoader();
        var mission = loader.Load("tutorial_01")!;

        Assert.NotNull(mission.Objectives);
        Assert.Single(mission.Objectives.Primary);
        Assert.Equal("destroy_scout", mission.Objectives.Primary[0].Id);
        Assert.Equal("destroy_target", mission.Objectives.Primary[0].Type);
        Assert.Single(mission.Objectives.Secondary);
    }

    [Fact]
    public void Load_parses_triggers()
    {
        var loader = CreateLoader();
        var mission = loader.Load("tutorial_01")!;

        Assert.Single(mission.Triggers);
        var trigger = mission.Triggers[0];
        Assert.Equal("spawn_wave_1", trigger.Id);
        Assert.NotNull(trigger.Condition);
        Assert.Equal("timer", trigger.Condition.Type);
        Assert.Equal(30f, trigger.Condition.Seconds);
        Assert.Equal(2, trigger.Actions.Length);
        Assert.True(trigger.OneShot);
    }

    [Fact]
    public void Load_parses_victory_and_defeat()
    {
        var loader = CreateLoader();
        var mission = loader.Load("tutorial_01")!;

        Assert.NotNull(mission.Victory);
        Assert.Equal("all_primary_complete", mission.Victory.Type);
        Assert.NotNull(mission.Defeat);
        Assert.Equal("hero_destroyed", mission.Defeat.Type);
    }

    [Fact]
    public void Load_parses_rewards()
    {
        var loader = CreateLoader();
        var mission = loader.Load("tutorial_01")!;

        Assert.NotNull(mission.Rewards);
        Assert.Equal(100, mission.Rewards.Xp);
        Assert.Contains("fighter_advanced", mission.Rewards.Unlocks);
        Assert.NotNull(mission.Rewards.Resources);
        Assert.Equal(200f, mission.Rewards.Resources.Energy);
    }

    // ── LoadAll ──────────────────────────────────────────────────────────────

    [Fact]
    public void LoadAll_finds_missions_excluding_templates()
    {
        var loader = CreateLoader();
        string missionsPath = Path.Combine(GetTestDataPath(), "Missions");
        var missions = loader.LoadAll(missionsPath);

        Assert.NotEmpty(missions);
        Assert.All(missions, m => Assert.False(m.Id.StartsWith('_')));
    }

    [Fact]
    public void LoadAll_returns_empty_for_missing_directory()
    {
        var loader = CreateLoader();
        var missions = loader.LoadAll("/nonexistent/path");

        Assert.Empty(missions);
    }

    // ── Exists / Invalidate ──────────────────────────────────────────────────

    [Fact]
    public void Exists_returns_true_for_valid_mission()
    {
        var loader = CreateLoader();
        Assert.True(loader.Exists("tutorial_01"));
    }

    [Fact]
    public void Exists_returns_false_for_missing_mission()
    {
        var loader = CreateLoader();
        Assert.False(loader.Exists("nonexistent"));
    }

    [Fact]
    public void Invalidate_clears_cache()
    {
        var loader = CreateLoader();
        var first = loader.Load("tutorial_01");
        loader.Invalidate("tutorial_01");
        var second = loader.Load("tutorial_01");

        // After invalidation, a new instance is loaded
        Assert.NotSame(first, second);
        Assert.Equal(first!.Id, second!.Id);
    }
}
