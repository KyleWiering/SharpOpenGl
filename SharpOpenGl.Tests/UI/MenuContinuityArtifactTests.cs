using OpenTK.Mathematics;
using SharpOpenGl.Engine.Missions;
using SharpOpenGl.Engine.Persistence;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class MenuContinuityArtifactTests
{
    private static readonly Vector2 CompactViewport = new(1024f, 768f);

    private static string ArtifactDir =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            ".grok", "org", "universe-expansion-upgrade", "artifacts"));

    [Fact]
    public void Capture_briefing_nav_icons_artifact_at_1024x768()
    {
        var screen = new BriefingScreen();
        screen.SetMission(new MissionDefinition
        {
            Id = "tutorial_01",
            DisplayName = "First Contact",
            Description = "Hostiles detected in Sector Alpha.",
            Briefing = new BriefingDefinition
            {
                Text = "Commander, reconnaissance probes report hostile staging areas along the frontier.",
                ObjectivesPreview = ["Destroy the scout", "Protect your base"],
            },
        });

        string path = Path.Combine(ArtifactDir, "briefing-nav-icons.png");
        CaptureScreen(screen, path);

        Assert.True(File.Exists(path));
        Assert.True(new FileInfo(path).Length > 1024);
    }

    [Fact]
    public void Capture_pause_menu_icons_artifact_at_1024x768()
    {
        var screen = new PauseScreen();
        string path = Path.Combine(ArtifactDir, "pause-menu-icons.png");
        CaptureScreen(screen, path);

        Assert.True(File.Exists(path));
        Assert.True(new FileInfo(path).Length > 1024);
    }

    [Fact]
    public void Capture_save_load_icons_artifact_at_1024x768()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"save_load_artifact_{Guid.NewGuid():N}");
        try
        {
            var mgr = new SaveManager(dir);
            mgr.Save(new SaveData
            {
                SlotName = SaveSlotNames.ManualSlots[0],
                MissionId = "tutorial_01",
                ElapsedMissionTime = 360f,
                Entities = [new EntitySaveRecord { EntityId = 1, TemplateId = "hero_default", Health = 100f }],
            });

            var save = new SaveGameScreen(mgr);
            var load = new LoadGameScreen(mgr);

            string savePath = Path.Combine(ArtifactDir, "save-load-icons-save.png");
            string loadPath = Path.Combine(ArtifactDir, "save-load-icons-load.png");
            string combinedPath = Path.Combine(ArtifactDir, "save-load-icons.png");

            CaptureScreen(save, savePath);
            CaptureScreen(load, loadPath);
            File.Copy(savePath, combinedPath, overwrite: true);

            Assert.True(File.Exists(combinedPath));
            Assert.True(new FileInfo(combinedPath).Length > 1024);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Capture_main_menu_and_mission_select_icon_artifacts_at_1024x768()
    {
        var menu = new MainMenuScreen(hasSave: true);
        var missionSelect = new MissionSelectScreen();
        missionSelect.SetMissions(
        [
            new MissionEntry
            {
                Id = "tutorial_01",
                Title = "First Contact",
                Description = "Learn basics.",
                BriefingText = "Hostiles detected.",
                ObjectivesPreview = ["Destroy the scout"],
                PlanetName = "Helios Prime",
                StarMapPosition = new Vector2(0.2f, 0.5f),
            },
        ]);

        CaptureScreen(menu, Path.Combine(ArtifactDir, "main-menu-nav-icons.png"));
        CaptureScreen(new MainMenuScreen(hasSave: false), Path.Combine(ArtifactDir, "main-menu-continue-disabled.png"));
        CaptureScreen(missionSelect, Path.Combine(ArtifactDir, "mission-select-nav-icons.png"));
        CaptureScreen(missionSelect, Path.Combine(ArtifactDir, "mission-select-preview-icons.png"));

        var emptyMissionSelect = new MissionSelectScreen();
        emptyMissionSelect.SetMissions([]);
        CaptureScreen(emptyMissionSelect, Path.Combine(ArtifactDir, "mission-select-empty-preview.png"));

        foreach (string file in new[]
        {
            "main-menu-nav-icons.png",
            "main-menu-continue-disabled.png",
            "mission-select-nav-icons.png",
            "mission-select-preview-icons.png",
            "mission-select-empty-preview.png",
        })
        {
            string path = Path.Combine(ArtifactDir, file);
            Assert.True(File.Exists(path), path);
            Assert.True(new FileInfo(path).Length > 1024, path);
        }
    }

    private static void CaptureScreen(UIScreen screen, string path)
    {
        var bitmap = new MenuUiBitmapRenderer(CompactViewport);
        var scaler = new UIScaler(CompactViewport);
        var renderer = new ScaledUIRenderer(bitmap, scaler);
        screen.Draw(renderer);
        bitmap.SavePng(path);
    }
}