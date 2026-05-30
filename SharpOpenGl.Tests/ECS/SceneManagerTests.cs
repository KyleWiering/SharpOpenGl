using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Scenes;
using Xunit;

namespace SharpOpenGl.Tests.ECS;

public class SceneManagerTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class FakeScene : IScene
    {
        public int LoadCount { get; private set; }
        public int UnloadCount { get; private set; }
        public float LastDelta { get; private set; }

        public void Load() => LoadCount++;
        public void Update(float dt) => LastDelta = dt;
        public void Unload() => UnloadCount++;
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Initial_state_is_None_with_no_scene()
    {
        var bus = new EventBus();
        var mgr = new SceneManager(bus);
        Assert.Equal(GameState.None, mgr.State);
        Assert.Null(mgr.CurrentSceneName);
    }

    [Fact]
    public void TransitionTo_loads_new_scene_and_sets_state()
    {
        var bus = new EventBus();
        var mgr = new SceneManager(bus);
        var scene = new FakeScene();
        mgr.Register("Main", () => scene);

        mgr.TransitionTo("Main", GameState.MainMenu);

        Assert.Equal(GameState.MainMenu, mgr.State);
        Assert.Equal("Main", mgr.CurrentSceneName);
        Assert.Equal(1, scene.LoadCount);
    }

    [Fact]
    public void TransitionTo_unloads_previous_scene()
    {
        var bus = new EventBus();
        var mgr = new SceneManager(bus);
        var sceneA = new FakeScene();
        var sceneB = new FakeScene();

        mgr.Register("A", () => sceneA);
        mgr.Register("B", () => sceneB);

        mgr.TransitionTo("A", GameState.MainMenu);
        mgr.TransitionTo("B", GameState.Playing);

        Assert.Equal(1, sceneA.UnloadCount);
        Assert.Equal(1, sceneB.LoadCount);
    }

    [Fact]
    public void TransitionTo_publishes_SceneTransitionEvent()
    {
        var bus = new EventBus();
        var mgr = new SceneManager(bus);
        var scene = new FakeScene();
        mgr.Register("Game", () => scene);

        SceneTransitionEvent? received = null;
        bus.Subscribe<SceneTransitionEvent>(e => received = e);

        mgr.TransitionTo("Game", GameState.Playing);

        Assert.NotNull(received);
        Assert.Equal("Game", received!.ToScene);
    }

    [Fact]
    public void TransitionTo_unknown_scene_throws()
    {
        var bus = new EventBus();
        var mgr = new SceneManager(bus);
        Assert.Throws<KeyNotFoundException>(() =>
            mgr.TransitionTo("Unknown", GameState.Playing));
    }

    [Fact]
    public void Update_delegates_to_active_scene()
    {
        var bus = new EventBus();
        var mgr = new SceneManager(bus);
        var scene = new FakeScene();
        mgr.Register("S", () => scene);
        mgr.TransitionTo("S", GameState.Playing);

        mgr.Update(0.016f);

        Assert.Equal(0.016f, scene.LastDelta, precision: 5);
    }

    [Fact]
    public void Dispose_unloads_active_scene()
    {
        var bus = new EventBus();
        var mgr = new SceneManager(bus);
        var scene = new FakeScene();
        mgr.Register("S", () => scene);
        mgr.TransitionTo("S", GameState.Playing);

        mgr.Dispose();

        Assert.Equal(1, scene.UnloadCount);
        Assert.Equal(GameState.None, mgr.State);
    }
}
