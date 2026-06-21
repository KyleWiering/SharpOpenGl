using OpenTK.Mathematics;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Engine.UI;

/// <summary>
/// Manages a stack of <see cref="UIScreen"/>s, routes input to the topmost
/// screen, and renders all visible screens in back-to-front order.
/// </summary>
public sealed class UIManager
{
    private readonly List<UIScreen> _stack = new();
    private readonly EventBus _eventBus;
    private readonly UIScaler _scaler;

    public UIManager(EventBus eventBus)
    {
        _eventBus = eventBus;
        _scaler = new UIScaler(UIScaler.ReferenceSize);
    }

    public UIScaler Scaler => _scaler;
    public void Resize(Vector2 viewportSize) => _scaler.Resize(viewportSize);
    public int ScreenCount => _stack.Count;
    public UIScreen? Current => _stack.Count > 0 ? _stack[^1] : null;

    public void Push(UIScreen screen)
    {
        _stack.Add(screen);
        screen.OnEnter();
        _eventBus.Publish(new UIScreenChangedEvent(
            _stack.Count > 1 ? _stack[^2].ScreenName : string.Empty,
            screen.ScreenName,
            UIScreenAction.Pushed));
    }

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

    public void Clear()
    {
        for (int i = _stack.Count - 1; i >= 0; i--)
            _stack[i].OnExit();
        _stack.Clear();
    }

    public void Update(float deltaTime)
    {
        int startIdx = GetLowestVisibleIndex();
        for (int i = startIdx; i < _stack.Count; i++)
            _stack[i].Update(deltaTime);
    }

    public void Draw(IUIRenderer renderer)
    {
        _scaler.Resize(renderer.ViewportSize);
        var scaledRenderer = new ScaledUIRenderer(renderer, _scaler);
        int startIdx = GetLowestVisibleIndex();
        for (int i = startIdx; i < _stack.Count; i++)
            _stack[i].Draw(scaledRenderer);
    }

    public bool HandlePointerTapped(Vector2 screenPoint, int button, Vector2 viewportSize)
    {
        _scaler.Resize(viewportSize);
        Vector2 logicalPoint = _scaler.UnscalePosition(screenPoint);
        return Current?.HandlePointerTapped(logicalPoint, button, UIScaler.ReferenceSize) ?? false;
    }

    public void HandlePointerMove(Vector2 screenPoint, bool isPointerDown, Vector2 viewportSize)
    {
        _scaler.Resize(viewportSize);
        Vector2 logicalPoint = _scaler.UnscalePosition(screenPoint);
        Current?.UpdatePointerState(logicalPoint, isPointerDown, UIScaler.ReferenceSize);
    }

    /// <summary>Route a navigation key to the active screen.</summary>
    public bool HandleKey(UIKey key) => Current?.HandleKey(key) ?? false;

    /// <summary>Return the hovered button on the active screen, if any.</summary>
    public Button? FindHoveredButton() => Current?.FindHoveredButton();

    private int GetLowestVisibleIndex()
    {
        if (_stack.Count == 0) return 0;

        int idx = _stack.Count - 1;
        while (idx > 0 && _stack[idx].IsOverlay)
            idx--;
        return idx;
    }
}