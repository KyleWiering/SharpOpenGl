using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.UI;

/// <summary>
/// A full-screen UI layer that owns a collection of root widgets.
/// Screens are managed by <see cref="UIManager"/>.
/// </summary>
public abstract class UIScreen
{
    private readonly List<Widget> _roots = new();

    /// <summary>Human-readable name used for debugging and navigation.</summary>
    public abstract string ScreenName { get; }

    /// <summary>
    /// When <c>true</c> the screens beneath this one on the stack remain visible
    /// (useful for pause overlays).  Default is <c>false</c>.
    /// </summary>
    public virtual bool IsOverlay => false;

    /// <summary>Whether this screen is currently visible.</summary>
    public bool Visible { get; set; } = true;

    /// <summary>Root-level widgets owned by this screen.</summary>
    protected IReadOnlyList<Widget> Roots => _roots;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    /// <summary>Called once when the screen is pushed onto the stack.</summary>
    public virtual void OnEnter() { }

    /// <summary>Called once when the screen is popped from the stack.</summary>
    public virtual void OnExit() { }

    // ── Per-frame ─────────────────────────────────────────────────────────────

    /// <summary>Update all root widgets.</summary>
    public virtual void Update(float deltaTime)
    {
        if (!Visible) return;
        foreach (Widget root in _roots)
            root.Update(deltaTime);
    }

    /// <summary>Draw all root widgets over the reference-resolution viewport.</summary>
    public virtual void Draw(IUIRenderer renderer)
    {
        if (!Visible) return;
        Vector2 logicalViewport = UIScaler.ReferenceSize;
        foreach (Widget root in _roots)
            root.Draw(renderer, Vector2.Zero, logicalViewport);
    }

    /// <summary>
    /// Route a pointer-tap event.  Returns <c>true</c> if a widget consumed it.
    /// </summary>
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

    // ── Widget management ─────────────────────────────────────────────────────

    /// <summary>Add a root-level widget to this screen.</summary>
    protected void AddWidget(Widget widget) => _roots.Add(widget);

    /// <summary>Remove a root-level widget.</summary>
    protected void RemoveWidget(Widget widget) => _roots.Remove(widget);
}