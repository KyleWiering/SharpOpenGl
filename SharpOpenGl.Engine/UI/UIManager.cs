using OpenTK.Mathematics;
using SharpOpenGl.Engine.Events;

namespace SharpOpenGl.Engine.UI;

/// <summary>
/// Manages a stack of <see cref="UIScreen"/>s, routes input to the topmost
/// screen, and renders all visible screens in back-to-front order.
/// </summary>
/// <remarks>
/// Screen lifecycle:
/// <list type="bullet">
///   <item><see cref="Push"/> — enter a new screen (overlay or full replacement).</item>
///   <item><see cref="Pop"/> — return to the previous screen.</item>
///   <item><see cref="Replace"/> — pop + push in one operation (no overlay).</item>
/// </list>
/// Screens flagged as <see cref="UIScreen.IsOverlay"/> keep the screen below
/// them visible so both render each frame.
/// </remarks>
public sealed class UIManager
{
    private readonly List<UIScreen> _stack = new();
    private readonly EventBus _eventBus;
    private readonly UIScaler _scaler;

    /// <param name="eventBus">Bus for publishing <see cref="UIScreenChangedEvent"/>.</param>
    public UIManager(EventBus eventBus)
    {
        _eventBus = eventBus;
        _scaler = new UIScaler(UIScaler.ReferenceSize);
    }

    /// <summary>Scaler mapping reference-resolution UI to the physical viewport.</summary>
    public UIScaler Scaler => _scaler;

    /// <summary>Update the scaler when the window/viewport is resized.</summary>
    public void Resize(Vector2 viewportSize) => _scaler.Resize(viewportSize);

    /// <summary>Number of screens currently on the stack.</summary>
    public int ScreenCount => _stack.Count;

    /// <summary>The topmost (active) screen, or <c>null</c> if the stack is empty.</summary>
    public UIScreen? Current => _stack.Count > 0 ? _stack[^1] : null;

    // ── Stack operations ──────────────────────────────────────────────────────

    /// <summary>Push a new screen onto the stack and call <see cref="UIScreen.OnEnter"/>.</summary>
    public void Push(UIScreen screen)
    {
        _stack.Add(screen);
        screen.OnEnter();
        _eventBus.Publish(new UIScreenChangedEvent(
            _stack.Count > 1 ? _stack[^2].ScreenName : string.Empty,
            screen.ScreenName,
            UIScreenAction.Pushed));
    }

    /// <summary>
    /// Pop the topmost screen, call <see cref="UIScreen.OnExit"/>, and return it.
    /// Returns <c>null</c> when the stack is empty.
    /// </summary>
    public UIScreen? Pop()
    {
        if (_stack.Count == 0) return null;

        UIScreen removed = _stack[^1];
        _stack.RemoveAt(_stack.Count - 1);
        removed.OnExit();

        _eventBus.Publish(new UIScreenChangedEvent(
            removed.ScreenName,
            Current?.ScreenName ?? string.Empty,
            UIScreenAction.Popped));

        return removed;
    }

    /// <summary>Pop the current screen and push <paramref name="screen"/> in its place.</summary>
    public void Replace(UIScreen screen)
    {
        if (_stack.Count > 0)
        {
            UIScreen old = _stack[^1];
            _stack.RemoveAt(_stack.Count - 1);
            old.OnExit();
        }
        Push(screen);
    }

    /// <summary>Remove all screens from the stack (exits each one).</summary>
    public void Clear()
    {
        for (int i = _stack.Count - 1; i >= 0; i--)
            _stack[i].OnExit();
        _stack.Clear();
    }

    // ── Per-frame ─────────────────────────────────────────────────────────────

    /// <summary>Update visible screens (topmost + any overlay ancestors).</summary>
    public void Update(float deltaTime)
    {
        int startIdx = GetLowestVisibleIndex();
        for (int i = startIdx; i < _stack.Count; i++)
            _stack[i].Update(deltaTime);
    }

    /// <summary>
    /// Draw visible screens back-to-front so the topmost appears on top.
    /// </summary>
    public void Draw(IUIRenderer renderer)
    {
        _scaler.Resize(renderer.ViewportSize);
        var scaledRenderer = new ScaledUIRenderer(renderer, _scaler);
        int startIdx = GetLowestVisibleIndex();
        for (int i = startIdx; i < _stack.Count; i++)
            _stack[i].Draw(scaledRenderer);
    }

    /// <summary>Route a pointer-tap event to the topmost screen that accepts it.</summary>
    public bool HandlePointerTapped(Vector2 screenPoint, int button, Vector2 viewportSize)
    {
        _scaler.Resize(viewportSize);
        Vector2 logicalPoint = _scaler.UnscalePosition(screenPoint);
        return Current?.HandlePointerTapped(logicalPoint, button, UIScaler.ReferenceSize) ?? false;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Walk down from the top of the stack until we reach a non-overlay screen.
    /// That screen is the lowest one that needs to be drawn/updated.
    /// </summary>
    private int GetLowestVisibleIndex()
    {
        if (_stack.Count == 0) return 0;

        int idx = _stack.Count - 1;
        while (idx > 0 && _stack[idx].IsOverlay)
            idx--;
        return idx;
    }
}