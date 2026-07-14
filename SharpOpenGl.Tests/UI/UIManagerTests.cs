using OpenTK.Mathematics;
using SharpOpenGl.Engine.Build;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

// ── Test stubs ────────────────────────────────────────────────────────────────

internal sealed class FakeScreen : UIScreen
{
    public override string ScreenName { get; }
    public override bool IsOverlay { get; }
    public int EnterCount { get; private set; }
    public int ExitCount  { get; private set; }

    public FakeScreen(string name, bool isOverlay = false)
    {
        ScreenName = name;
        IsOverlay  = isOverlay;
    }

    public override void OnEnter() => EnterCount++;
    public override void OnExit()  => ExitCount++;
}

// ── UIManager tests ───────────────────────────────────────────────────────────

public class UIManagerTests
{
    private static UIManager CreateManager() => new(new EventBus());

    // ── Initial state ─────────────────────────────────────────────────────────

    [Fact]
    public void Initial_stack_is_empty()
    {
        var mgr = CreateManager();
        Assert.Equal(0, mgr.ScreenCount);
        Assert.Null(mgr.Current);
    }

    // ── Push ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Push_calls_OnEnter_and_sets_Current()
    {
        var mgr = CreateManager();
        var screen = new FakeScreen("A");
        mgr.Push(screen);

        Assert.Equal(1, mgr.ScreenCount);
        Assert.Same(screen, mgr.Current);
        Assert.Equal(1, screen.EnterCount);
    }

    [Fact]
    public void Push_stacks_multiple_screens()
    {
        var mgr = CreateManager();
        var a = new FakeScreen("A");
        var b = new FakeScreen("B");
        mgr.Push(a);
        mgr.Push(b);

        Assert.Equal(2, mgr.ScreenCount);
        Assert.Same(b, mgr.Current);
    }

    // ── Pop ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Pop_removes_topmost_screen_and_calls_OnExit()
    {
        var mgr = CreateManager();
        var a = new FakeScreen("A");
        var b = new FakeScreen("B");
        mgr.Push(a);
        mgr.Push(b);

        UIScreen? removed = mgr.Pop();

        Assert.Same(b, removed);
        Assert.Equal(1, b.ExitCount);
        Assert.Equal(1, mgr.ScreenCount);
        Assert.Same(a, mgr.Current);
    }

    [Fact]
    public void Pop_on_empty_stack_returns_null()
    {
        var mgr = CreateManager();
        Assert.Null(mgr.Pop());
    }

    // ── Replace ───────────────────────────────────────────────────────────────

    [Fact]
    public void Replace_exits_old_and_enters_new()
    {
        var mgr = CreateManager();
        var a = new FakeScreen("A");
        var b = new FakeScreen("B");
        mgr.Push(a);
        mgr.Replace(b);

        Assert.Equal(1, mgr.ScreenCount);
        Assert.Same(b, mgr.Current);
        Assert.Equal(1, a.ExitCount);
        Assert.Equal(1, b.EnterCount);
    }

    // ── Clear ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Clear_exits_all_screens()
    {
        var mgr = CreateManager();
        var a = new FakeScreen("A");
        var b = new FakeScreen("B");
        mgr.Push(a);
        mgr.Push(b);

        mgr.Clear();

        Assert.Equal(0, mgr.ScreenCount);
        Assert.Equal(1, a.ExitCount);
        Assert.Equal(1, b.ExitCount);
    }

    // ── Overlay visibility ────────────────────────────────────────────────────

    [Fact]
    public void Overlay_screen_keeps_screen_below_visible()
    {
        // Verify GetLowestVisibleIndex logic by checking Update reaches base screen.
        var mgr = CreateManager();
        var base_ = new FakeScreen("Base", isOverlay: false);
        var overlay = new FakeScreen("Overlay", isOverlay: true);
        mgr.Push(base_);
        mgr.Push(overlay);

        // Both screens should be updated (the non-overlay base is included).
        // FakeScreen.Update is not overridden so no side-effects — just ensure
        // no exception is thrown.
        mgr.Update(0.016f);
    }

    // ── Events ────────────────────────────────────────────────────────────────

    [Fact]
    public void Push_publishes_UIScreenChangedEvent()
    {
        var bus = new EventBus();
        var mgr = new UIManager(bus);
        UIScreenChangedEvent? received = null;
        bus.Subscribe<UIScreenChangedEvent>(e => received = e);

        mgr.Push(new FakeScreen("Main"));

        Assert.NotNull(received);
        Assert.Equal("Main", received!.Current);
        Assert.Equal(UIScreenAction.Pushed, received.Action);
    }

    [Fact]
    public void Pop_publishes_UIScreenChangedEvent()
    {
        var bus = new EventBus();
        var mgr = new UIManager(bus);
        mgr.Push(new FakeScreen("A"));
        mgr.Push(new FakeScreen("B"));

        UIScreenChangedEvent? received = null;
        bus.Subscribe<UIScreenChangedEvent>(e => received = e);

        mgr.Pop();

        Assert.NotNull(received);
        Assert.Equal("B", received!.Previous);
        Assert.Equal(UIScreenAction.Popped, received.Action);
    }

    // ── Tooltip integration ───────────────────────────────────────────────────

    [Fact]
    public void Draw_renders_tooltip_after_active_screen()
    {
        var mgr = CreateManager();
        var hud = CreateHudWithBuildMapEntry();
        mgr.Push(hud);

        var (pos, _) = hud.BuildMapPanel.Resolve(Vector2.Zero, UIScaler.ReferenceSize);
        var hoverPoint = pos + new Vector2(20f, 80f);
        mgr.HandlePointerMove(hoverPoint, false, UIScaler.ReferenceSize);
        mgr.Update(TooltipWidget.HoverShowDelaySeconds + 0.05f);

        var renderer = new DrawOrderRecordingRenderer(UIScaler.ReferenceSize);
        mgr.Draw(renderer);

        int screenMarkerIndex = renderer.TextDraws.FindIndex(draw => draw.Text == "Build Structures");
        int tooltipIndex = renderer.TextDraws.FindIndex(draw => draw.Text.Contains("Defense Turret", StringComparison.Ordinal));

        Assert.True(screenMarkerIndex >= 0, "HUD text should draw with the screen.");
        Assert.True(tooltipIndex > screenMarkerIndex, "Tooltip should draw after all screens.");
    }

    [Fact]
    public void HandlePointerMove_shows_build_map_tooltip_after_hover_delay()
    {
        var mgr = CreateManager();
        var hud = CreateHudWithBuildMapEntry();
        mgr.Push(hud);

        var (pos, _) = hud.BuildMapPanel.Resolve(Vector2.Zero, UIScaler.ReferenceSize);
        var hoverPoint = pos + new Vector2(20f, 80f);
        mgr.HandlePointerMove(hoverPoint, false, UIScaler.ReferenceSize);
        mgr.Update(0.1f);

        var rendererEarly = new DrawOrderRecordingRenderer(UIScaler.ReferenceSize);
        mgr.Draw(rendererEarly);
        Assert.DoesNotContain(rendererEarly.TextDraws, draw => draw.Text.Contains("Defense Turret", StringComparison.Ordinal));

        mgr.Update(0.15f);
        var rendererLate = new DrawOrderRecordingRenderer(UIScaler.ReferenceSize);
        mgr.Draw(rendererLate);
        Assert.Contains(rendererLate.TextDraws, draw => draw.Text.Contains("Defense Turret", StringComparison.Ordinal));
    }

    [Fact]
    public void HandlePointerMove_clears_tooltip_when_pointer_leaves_provider()
    {
        var mgr = CreateManager();
        var hud = CreateHudWithBuildMapEntry();
        mgr.Push(hud);

        var (pos, _) = hud.BuildMapPanel.Resolve(Vector2.Zero, UIScaler.ReferenceSize);
        mgr.HandlePointerMove(pos + new Vector2(20f, 80f), false, UIScaler.ReferenceSize);
        mgr.Update(TooltipWidget.HoverShowDelaySeconds + 0.05f);

        mgr.HandlePointerMove(new Vector2(5f, 5f), false, UIScaler.ReferenceSize);
        var renderer = new DrawOrderRecordingRenderer(UIScaler.ReferenceSize);
        mgr.Draw(renderer);

        Assert.DoesNotContain(renderer.TextDraws, draw => draw.Text.Contains("Defense Turret", StringComparison.Ordinal));
    }

    [Fact]
    public void HandlePointerMove_clears_tooltip_when_build_map_panel_closes()
    {
        var mgr = CreateManager();
        var hud = CreateHudWithBuildMapEntry();
        mgr.Push(hud);

        var (pos, _) = hud.BuildMapPanel.Resolve(Vector2.Zero, UIScaler.ReferenceSize);
        var hoverPoint = pos + new Vector2(20f, 80f);
        mgr.HandlePointerMove(hoverPoint, false, UIScaler.ReferenceSize);
        mgr.Update(TooltipWidget.HoverShowDelaySeconds + 0.05f);

        hud.BuildMapPanel.Visible = false;
        mgr.HandlePointerMove(hoverPoint, false, UIScaler.ReferenceSize);

        var renderer = new DrawOrderRecordingRenderer(UIScaler.ReferenceSize);
        mgr.Draw(renderer);

        Assert.DoesNotContain(renderer.TextDraws, draw => draw.Text.Contains("Defense Turret", StringComparison.Ordinal));
    }

    [Fact]
    public void Push_clears_active_tooltip()
    {
        var mgr = CreateManager();
        var hud = CreateHudWithBuildMapEntry();
        mgr.Push(hud);

        var (pos, _) = hud.BuildMapPanel.Resolve(Vector2.Zero, UIScaler.ReferenceSize);
        mgr.HandlePointerMove(pos + new Vector2(20f, 80f), false, UIScaler.ReferenceSize);
        mgr.Update(TooltipWidget.HoverShowDelaySeconds + 0.05f);

        mgr.Push(new FakeScreen("Overlay", isOverlay: true));
        var renderer = new DrawOrderRecordingRenderer(UIScaler.ReferenceSize);
        mgr.Draw(renderer);

        Assert.DoesNotContain(renderer.TextDraws, draw => draw.Text.Contains("Defense Turret", StringComparison.Ordinal));
    }

    private static GameplayHUD CreateHudWithBuildMapEntry()
    {
        var hud = new GameplayHUD { Visible = true };
        hud.BuildMapPanel.Visible = true;
        hud.BuildMapPanel.Categories =
        [
            new BuildMapCategoryView
            {
                DisplayName = "Defense",
                Buildings =
                [
                    new BuildMapEntryView
                    {
                        Id = "defense_turret",
                        Name = "Defense Turret",
                        FootprintCols = 1,
                        FootprintRows = 1,
                        EnergyCost = 60,
                        MineralsCost = 90,
                        BuildTime = 18f,
                        IsUnlocked = true,
                        CanAfford = true,
                        Icon = BuildIconCatalog.Get("defense_turret"),
                    },
                ],
            },
        ];

        return hud;
    }

    private sealed class DrawOrderRecordingRenderer : IUIRenderer
    {
        public DrawOrderRecordingRenderer(Vector2 viewport) => ViewportSize = viewport;

        public Vector2 ViewportSize { get; }
        public List<(string Text, float FontSize, Vector2 Position)> TextDraws { get; } = new();

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) { }
        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }

        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color) =>
            TextDraws.Add((text, fontSize, position));
    }
}
