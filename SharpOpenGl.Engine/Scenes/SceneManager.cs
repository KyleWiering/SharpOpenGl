using SharpOpenGl.Engine.Events;

namespace SharpOpenGl.Engine.Scenes;

/// <summary>
/// Manages the active scene and owns the top-level game state machine.
/// Scenes are identified by string names; the manager stores factories
/// (<see cref="Func{IScene}"/>) rather than pre-built instances.
/// </summary>
public sealed class SceneManager
{
    private readonly Dictionary<string, Func<IScene>> _factories = new();
    private readonly EventBus _eventBus;

    private IScene? _current;
    private GameState _state = GameState.None;

    /// <param name="eventBus">
    /// Bus used to publish <see cref="SceneTransitionEvent"/> on every transition.
    /// </param>
    public SceneManager(EventBus eventBus)
    {
        _eventBus = eventBus;
    }

    /// <summary>The currently active game state.</summary>
    public GameState State => _state;

    /// <summary>The name of the currently active scene, or <c>null</c> if none.</summary>
    public string? CurrentSceneName { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    // Registration
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Register a named scene factory. Overwrites any previous registration.</summary>
    public void Register(string name, Func<IScene> factory) =>
        _factories[name] = factory;

    // ─────────────────────────────────────────────────────────────────────────
    // Transitions
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Transition to the named scene and update the game state.
    /// The previous scene is unloaded before the new one is loaded.
    /// </summary>
    /// <param name="name">Registered scene name.</param>
    /// <param name="newState">Game state to apply during this scene.</param>
    /// <exception cref="KeyNotFoundException">Thrown if <paramref name="name"/> is not registered.</exception>
    public void TransitionTo(string name, GameState newState)
    {
        string fromScene = CurrentSceneName ?? string.Empty;

        _current?.Unload();
        _current = null;
        CurrentSceneName = null;

        if (!_factories.TryGetValue(name, out Func<IScene>? factory))
            throw new KeyNotFoundException($"Scene '{name}' is not registered.");

        _state = newState;
        _current = factory();
        CurrentSceneName = name;
        _current.Load();

        _eventBus.Publish(new SceneTransitionEvent(fromScene, name));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Per-frame
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Update the active scene.</summary>
    /// <param name="deltaTime">Elapsed time in seconds.</param>
    public void Update(float deltaTime) => _current?.Update(deltaTime);

    // ─────────────────────────────────────────────────────────────────────────
    // Cleanup
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Unload the active scene and clear all registrations.</summary>
    public void Dispose()
    {
        _current?.Unload();
        _current = null;
        _factories.Clear();
        CurrentSceneName = null;
        _state = GameState.None;
    }
}
