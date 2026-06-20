using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.UI;

/// <summary>
/// A full-screen UI layer that owns a collection of root widgets.
/// </summary>
public abstract class UIScreen
{
    private readonly List<Widget> _roots = new();

    public abstract string ScreenName { get; }
    public virtual bool IsOverlay => false;
    public bool Visible { get; set; } = true;
    protected IReadOnlyList<Widget> Roots => _roots;

    public virtual void OnEnter() { }
    public virtual void OnExit() { }

    public virtual void Update(float deltaTime)
    {
        if (!Visible) return;
        foreach (Widget root in _roots)
            root.Update(deltaTime);
    }

    public virtual void Draw(IUIRenderer renderer)
    {
        if (!Visible) return;
        Vector2 logicalViewport = UIScaler.ReferenceSize;
        foreach (Widget root in _roots)
            root.Draw(renderer, Vector2.Zero, logicalViewport);
    }

    public virtual bool HandlePointerTapped(Vector2 screenPoint, int button, Vector2 viewportSize)
    {
        if (!Visible) return false;
        for (int i = _roots.Count - 1; i >= 0; i--)
        {
            if (_roots[i].HandlePointerTapped(screenPoint, button, Vector2.Zero, viewportSize))
                return true;
        }
        return false;
    }

    public virtual void UpdatePointerState(Vector2 screenPoint, bool isPointerDown, Vector2 viewportSize)
    {
        if (!Visible) return;
        for (int i = _roots.Count - 1; i >= 0; i--)
            _roots[i].UpdatePointerState(screenPoint, isPointerDown, Vector2.Zero, viewportSize);
    }

    protected void AddWidget(Widget widget) => _roots.Add(widget);
    protected void RemoveWidget(Widget widget) => _roots.Remove(widget);
}