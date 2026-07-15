using OpenTK.Mathematics;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Missions;
using SharpOpenGl.Engine.Persistence;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class MenuContinuityFlowTests
{
    private static readonly Vector2 ReferenceViewport = UIScaler.ReferenceSize;
    private static readonly Vector2 CompactViewport = new(1024f, 768f);

    [Fact]
    public void Campaign_path_mission_select_to_briefing_preserves_mission_context()
    {
        var mgr = new UIManager(new EventBus());
        var missionSelect = new MissionSelectScreen();
        missionSelect.SetMissions(
        [
            new MissionEntry
            {
                Id = "tutorial_01",
                Title = "First Contact",
                Description = "Learn basics.",
                StarMapPosition = new Vector2(0.2f, 0.5f),
            },
        ]);

        string? briefingMissionId = null;
        missionSelect.MissionStartRequested += id =>
        {
            briefingMissionId = id;
            var briefing = new BriefingScreen();
            briefing.SetMission(new MissionDefinition
            {
                Id = id,
                DisplayName = "First Contact",
                Description = "Learn basics.",
            });
            mgr.Push(briefing);
        };

        mgr.Push(missionSelect);
        missionSelect.FindButton("StartMission")!.Activate();

        Assert.Equal("tutorial_01", briefingMissionId);
        Assert.Equal(2, mgr.ScreenCount);
        Assert.IsType<BriefingScreen>(mgr.Current);

        var activeBriefing = (BriefingScreen)mgr.Current!;
        Assert.Equal("tutorial_01", activeBriefing.MissionId);
    }

    [Fact]
    public void Campaign_briefing_start_pushes_loading_then_fires_start_after_progress()
    {
        var mgr = new UIManager(new EventBus());
        var briefing = new BriefingScreen();
        briefing.SetMission(new MissionDefinition
        {
            Id = "tutorial_01",
            DisplayName = "First Contact",
            Description = "Learn basics.",
        });

        bool started = false;
        briefing.StartRequested += () => started = true;
        mgr.Push(briefing);

        briefing.FindButton("StartMission")!.Activate();

        Assert.Equal(2, mgr.ScreenCount);
        Assert.IsType<LoadingScreen>(mgr.Current);

        var loading = (LoadingScreen)mgr.Current!;
        Assert.Contains("First Contact", loading.StatusText, StringComparison.Ordinal);

        for (int i = 0; i < 40; i++)
            mgr.Update(0.02f);

        Assert.Equal(1, mgr.ScreenCount);
        Assert.IsType<BriefingScreen>(mgr.Current);
        Assert.True(started);
    }

    [Fact]
    public void Campaign_briefing_back_pops_to_mission_select()
    {
        var mgr = new UIManager(new EventBus());
        var missionSelect = new MissionSelectScreen();
        var briefing = new BriefingScreen();
        briefing.SetMission(new MissionDefinition { Id = "tutorial_01", DisplayName = "First Contact" });

        briefing.BackRequested += () => mgr.Pop();
        mgr.Push(missionSelect);
        mgr.Push(briefing);

        briefing.FindButton("Back")!.Activate();

        Assert.Equal(1, mgr.ScreenCount);
        Assert.Same(missionSelect, mgr.Current);
    }

    [Fact]
    public void Pause_load_back_flow_returns_to_pause_menu()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"pause_load_flow_{Guid.NewGuid():N}");
        try
        {
            var mgr = new UIManager(new EventBus());
            var pause = new PauseScreen();
            var loadMgr = new SaveManager(dir);
            var load = new LoadGameScreen(loadMgr);

            bool backRequested = false;
            load.BackRequested += () =>
            {
                backRequested = true;
                mgr.Pop();
            };

            pause.LoadGameRequested += () => mgr.Push(load);

            mgr.Push(new FakeGameplayScreen());
            mgr.Push(pause);
            pause.FindButton("LoadGame")!.Activate();

            Assert.Equal(3, mgr.ScreenCount);
            Assert.IsType<LoadGameScreen>(mgr.Current);

            load.FindButton("Back")!.Activate();

            Assert.True(backRequested);
            Assert.Equal(2, mgr.ScreenCount);
            Assert.IsType<PauseScreen>(mgr.Current);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Pause_save_back_flow_returns_to_pause_menu()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"pause_save_flow_{Guid.NewGuid():N}");
        try
        {
            var mgr = new UIManager(new EventBus());
            var pause = new PauseScreen();
            var saveMgr = new SaveManager(dir);
            var save = new SaveGameScreen(saveMgr);

            bool cancelled = false;
            save.Cancelled += () =>
            {
                cancelled = true;
                mgr.Pop();
            };

            pause.SaveGameRequested += () => mgr.Push(save);

            mgr.Push(new FakeGameplayScreen());
            mgr.Push(pause);
            pause.FindButton("SaveGame")!.Activate();

            Assert.Equal(3, mgr.ScreenCount);
            Assert.IsType<SaveGameScreen>(mgr.Current);

            save.FindButton("Back")!.Activate();

            Assert.True(cancelled);
            Assert.Equal(2, mgr.ScreenCount);
            Assert.IsType<PauseScreen>(mgr.Current);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Load_game_back_requested_pops_to_main_menu_stack()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"load_back_flow_{Guid.NewGuid():N}");
        try
        {
            var mgr = new UIManager(new EventBus());
            var menu = new MainMenuScreen(hasSave: true);
            var loadMgr = new SaveManager(dir);
            var load = new LoadGameScreen(loadMgr);

            load.BackRequested += () => mgr.Pop();
            mgr.Push(menu);
            mgr.Push(load);

            load.FindButton("Back")!.Activate();

            Assert.Equal(1, mgr.ScreenCount);
            Assert.Same(menu, mgr.Current);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Save_and_load_screens_use_icon_left_nav_back_at_bottom_left()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"save_load_layout_{Guid.NewGuid():N}");
        try
        {
            var saveMgr = new SaveManager(dir);
            var save = new SaveGameScreen(saveMgr);
            var load = new LoadGameScreen(saveMgr);

            AssertNavBackPlacement(save.FindButton("Back") as IconButton);
            AssertNavBackPlacement(load.FindButton("Back") as IconButton);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Theory]
    [InlineData(1920f, 1080f)]
    [InlineData(1024f, 768f)]
    public void Briefing_and_pause_screens_render_without_throw_at_reference_and_compact(float viewportX, float viewportY)
    {
        var viewport = new Vector2(viewportX, viewportY);
        var briefing = new BriefingScreen();
        briefing.SetMission(new MissionDefinition
        {
            Id = "tutorial_01",
            DisplayName = "First Contact",
            Description = "Learn basics.",
        });

        var pause = new PauseScreen();
        var inner = new RecordingRenderer(viewport);
        var renderer = new ScaledUIRenderer(inner, new UIScaler(viewport));

        var briefingException = Record.Exception(() => briefing.Draw(renderer));
        var pauseException = Record.Exception(() => pause.Draw(renderer));

        Assert.Null(briefingException);
        Assert.Null(pauseException);
        Assert.NotEmpty(inner.Rects);
    }

    private static void AssertNavBackPlacement(IconButton? back)
    {
        Assert.NotNull(back);
        Assert.Equal(MenuIconKind.NavBack, back!.Icon);
        Assert.Equal(IconButtonLayout.IconLeftOfLabel, back.Layout);
        Assert.Equal(Anchor.BottomLeft, back.Anchor);
        Assert.Equal(new Vector2(40f, -80f), back.Position);
    }

    private sealed class FakeGameplayScreen : UIScreen
    {
        public override string ScreenName => "Gameplay";
        public override bool IsOverlay => false;
    }

    private sealed class RecordingRenderer : IUIRenderer
    {
        public RecordingRenderer(Vector2 viewport) => ViewportSize = viewport;
        public Vector2 ViewportSize { get; }
        public List<(Vector2 Position, Vector2 Size)> Rects { get; } = new();

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) =>
            Rects.Add((position, size));

        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }
        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color) { }
    }
}