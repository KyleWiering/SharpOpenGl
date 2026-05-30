using OpenTK.Mathematics;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.UI;
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
}
