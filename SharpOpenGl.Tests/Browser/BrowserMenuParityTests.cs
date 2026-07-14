using OpenTK.Mathematics;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Missions;
using SharpOpenGl.Engine.Persistence;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.Browser;

public class BrowserMenuParityTests
{
    private static readonly Vector2 BrowserHostViewport = new(1024f, 768f);

    [Fact]
    public void BrowserHost_wires_settings_main_menu_and_briefing_handlers()
    {
        string source = File.ReadAllText(BrowserGameHostSourcePath);

        Assert.Contains("menu.SettingsRequested += ShowSettings", source);
        Assert.Contains("internal void ShowSettings()", source);
        Assert.Contains("new SettingsScreen(_settingsManager)", source);
        Assert.Contains("internal void ShowMissionBriefing", source);
        Assert.Contains("new MainMenuScreen()", source);
        Assert.DoesNotContain("SettingsRequested += () => { }", source);
    }

    [Fact]
    public void BrowserHost_pause_menu_has_save_load_settings_parity()
    {
        string source = File.ReadAllText(BrowserGameHostSourcePath);

        Assert.Contains("pause.SaveGameRequested += ShowSaveGameScreen", source);
        Assert.Contains("pause.LoadGameRequested += ShowLoadGameScreen", source);
        Assert.Contains("pause.SettingsRequested += ShowSettings", source);
        Assert.Contains("new SaveGameScreen(_saveManager)", source);
        Assert.Contains("new LoadGameScreen(_saveManager)", source);
        Assert.Contains("SaveManager.CreateInMemory()", source);
    }

    [Fact]
    public void Pause_save_load_settings_handlers_fire()
    {
        var pause = new PauseScreen(hasSave: true);
        bool saveOpened = false;
        bool loadOpened = false;
        bool settingsOpened = false;

        pause.SaveGameRequested += () => saveOpened = true;
        pause.LoadGameRequested += () => loadOpened = true;
        pause.SettingsRequested += () => settingsOpened = true;

        pause.FindButton("SaveGame")!.Activate();
        pause.FindButton("LoadGame")!.Activate();
        pause.FindButton("Settings")!.Activate();

        Assert.True(saveOpened);
        Assert.True(loadOpened);
        Assert.True(settingsOpened);
    }

    [Fact]
    public void MainMenu_settings_navigation_works_at_1024x768()
    {
        var mgr = new UIManager(new EventBus());
        mgr.Resize(BrowserHostViewport);

        var menu = new MainMenuScreen();
        bool settingsOpened = false;
        menu.SettingsRequested += () => settingsOpened = true;
        mgr.Push(menu);

        var scaler = new UIScaler(BrowserHostViewport);
        Vector2 settingsCenter = MainMenuButtonCenter(buttonIndex: 6);
        Vector2 physicalTap = scaler.ScalePosition(settingsCenter);

        Assert.True(mgr.HandlePointerTapped(physicalTap, 0, BrowserHostViewport));
        Assert.True(settingsOpened);
    }

    [Fact]
    public void Settings_back_navigation_works_at_1024x768()
    {
        var mgr = new UIManager(new EventBus());
        mgr.Resize(BrowserHostViewport);

        var settings = new SettingsScreen(new SettingsManager(null));
        bool backRequested = false;
        settings.BackRequested += () => backRequested = true;
        mgr.Push(settings);

        var scaler = new UIScaler(BrowserHostViewport);
        Vector2 backCenter = new(800f, 990f);
        Vector2 physicalTap = scaler.ScalePosition(backCenter);

        Assert.True(mgr.HandlePointerTapped(physicalTap, 0, BrowserHostViewport));
        Assert.True(backRequested);
    }

    [Fact]
    public void Briefing_back_navigation_works_at_1024x768()
    {
        var mgr = new UIManager(new EventBus());
        mgr.Resize(BrowserHostViewport);

        var briefing = new BriefingScreen();
        briefing.SetMission(CreateSampleMission());
        bool backed = false;
        briefing.BackRequested += () => backed = true;
        mgr.Push(briefing);

        var scaler = new UIScaler(BrowserHostViewport);
        var backBtn = Assert.IsType<IconButton>(briefing.FindButton("Back"));
        var (backPos, backSize) = backBtn.Resolve(Vector2.Zero, UIScaler.ReferenceSize);

        Assert.True(mgr.HandlePointerTapped(scaler.ScalePosition(backPos + backSize * 0.5f), 0, BrowserHostViewport));
        Assert.True(backed);
    }

    private static Vector2 MainMenuButtonCenter(int buttonIndex)
    {
        const float btnH = 58f;
        const float gap = 10f;
        float totalH = 8 * btnH + 7 * gap;
        float startY = MathF.Max(248f, (1080f - totalH) * 0.5f);
        float centerY = startY + buttonIndex * (btnH + gap) + btnH * 0.5f;
        return new Vector2(960f, centerY);
    }

    private static MissionDefinition CreateSampleMission() => new()
    {
        Id = "tutorial_01",
        DisplayName = "Tutorial - First Steps",
        Briefing = new BriefingDefinition
        {
            Text = "Commander, sensors have detected an enemy scout in Sector Alpha.",
            ObjectivesPreview = ["Destroy the enemy scout"],
        },
        Objectives = new ObjectivesDefinition
        {
            Primary =
            [
                new ObjectiveDefinition { Id = "destroy_scout", Type = "destroy_target", Description = "Destroy the enemy scout" },
            ],
        },
    };

    private static string BrowserGameHostSourcePath
    {
        get
        {
            string? dir = AppDomain.CurrentDomain.BaseDirectory;
            while (dir != null && !File.Exists(Path.Combine(dir, "SharpOpenGl.sln")))
                dir = Directory.GetParent(dir)?.FullName;
            if (dir == null)
                throw new InvalidOperationException("Could not locate SharpOpenGl.sln.");
            return Path.Combine(dir, "SharpOpenGl.Browser", "Game", "BrowserGameHost.cs");
        }
    }
}