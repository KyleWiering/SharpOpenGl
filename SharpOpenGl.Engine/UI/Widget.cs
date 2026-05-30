using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.UI;

/// <summary>
/// Abstract base class for all UI elements.
/// <para>
/// Widgets form a tree: each widget has an optional <see cref="Parent"/> and
/// zero or more <see cref="Children"/>.  Positions are resolved relative to the
/// parent's computed top-left corner using the chosen <see cref="Anchor"/>.
/// </para>
/// </summary>
public abstract class Widget
{
    private readonly List<Widget> _children = new();
    private Widget? _parent;

    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>Optional name for debugging and lookup.</summary>
    public string? Name { get; set; }

    // ── Layout ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Position in pixels, interpreted as an offset from the anchor origin.
    /// For <see cref="UI.Anchor.Stretch"/> this is the inset padding on each side.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>Size in pixels.  Ignored when <see cref="Anchor"/> is <see cref="UI.Anchor.Stretch"/>.</summary>
    public Vector2 Size { get; set; }

    /// <summary>Anchoring rule that determines where the widget attaches inside its container.</summary>
    public Anchor Anchor { get; set; } = Anchor.TopLeft;

    // ── Visibility ────────────────────────────────────────────────────────────

    /// <summary>
    /// When <c>false</c> the widget and its subtree are neither updated nor drawn.
    /// </summary>
    public bool Visible { get; set; } = true;

    // ── Hierarchy ─────────────────────────────────────────────────────────────

    /// <summary>Parent widget, or <c>null</c> when this is a root widget.</summary>
    public Widget? Parent => _parent;

    /// <summary>Ordered list of children (drawn back-to-front).</summary>
    public IReadOnlyList<Widget> Children => _children;

    /// <summary>
    /// Add <paramref name="child"/> as a child of this widget.
    /// Removes the child from its previous parent if it already has one.
    /// </summary>
    public void AddChild(Widget child)
    {
        child._parent?.RemoveChild(child);
        child._parent = this;
        _children.Add(child);
    }

    /// <summary>Remove a direct child.  No-op if <paramref name="child"/> is not a child.</summary>
    public void RemoveChild(Widget child)
    {
        if (_children.Remove(child))
            child._parent = null;
    }

    // ── Layout resolution ─────────────────────────────────────────────────────

    /// <summary>
    /// Resolve the absolute screen-space top-left position and effective size of
    /// this widget, given the container rectangle supplied by the parent (or the
    /// full viewport for root widgets).
    /// </summary>
    /// <param name="containerPosition">Top-left of the container in screen pixels.</param>
    /// <param name="containerSize">Size of the container in screen pixels.</param>
    /// <returns>Absolute top-left position and effective size.</returns>
    public (Vector2 AbsolutePosition, Vector2 EffectiveSize) Resolve(
        Vector2 containerPosition, Vector2 containerSize)
    {
        if (Anchor == Anchor.Stretch)
        {
            Vector2 inset = Position;
            return (
                containerPosition + inset,
                containerSize - inset * 2f
            );
        }

        Vector2 origin = ResolveAnchorOrigin(containerSize, Size, Anchor);
        return (containerPosition + origin + Position, Size);
    }

    /// <summary>
    /// Returns <c>true</c> if <paramref name="screenPoint"/> is inside this
    /// widget's computed bounds.
    /// </summary>
    public bool Contains(Vector2 screenPoint, Vector2 containerPosition, Vector2 containerSize)
    {
        var (pos, size) = Resolve(containerPosition, containerSize);
        return screenPoint.X >= pos.X && screenPoint.X < pos.X + size.X
            && screenPoint.Y >= pos.Y && screenPoint.Y < pos.Y + size.Y;
    }

    // ── Interaction ───────────────────────────────────────────────────────────

    /// <summary>
    /// Propagate a pointer-tap event depth-first through the subtree.
    /// Returns <c>true</c> when a descendant (or this widget) has consumed it.
    /// </summary>
    public virtual bool HandlePointerTapped(
        Vector2 screenPoint, int button,
        Vector2 containerPosition, Vector2 containerSize)
    {
        if (!Visible) return false;

        var (pos, size) = Resolve(containerPosition, containerSize);

        // Pass to children first (reverse order so topmost child gets priority).
        for (int i = _children.Count - 1; i >= 0; i--)
        {
            if (_children[i].HandlePointerTapped(screenPoint, button, pos, size))
                return true;
        }
        return false;
    }

    // ── Update & Draw ─────────────────────────────────────────────────────────

    /// <summary>Update this widget and its visible children.</summary>
    public virtual void Update(float deltaTime)
    {
        if (!Visible) return;
        foreach (Widget child in _children)
            child.Update(deltaTime);
    }

    /// <summary>
    /// Draw this widget and its visible children using the supplied renderer.
    /// </summary>
    /// <param name="renderer">UI renderer.</param>
    /// <param name="containerPosition">Top-left of the container in screen pixels.</param>
    /// <param name="containerSize">Size of the container in screen pixels.</param>
    public void Draw(IUIRenderer renderer, Vector2 containerPosition, Vector2 containerSize)
    {
        if (!Visible) return;

        var (pos, size) = Resolve(containerPosition, containerSize);
        OnDraw(renderer, pos, size);

        foreach (Widget child in _children)
            child.Draw(renderer, pos, size);
    }

    /// <summary>
    /// Override to implement custom rendering.  Called with this widget's
    /// resolved absolute position and effective size.
    /// </summary>
    protected virtual void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size) { }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Compute the anchor origin (top-left corner of the widget) in container space.</summary>
    private static Vector2 ResolveAnchorOrigin(
        Vector2 containerSize, Vector2 widgetSize, Anchor anchor)
    {
        float cx = (containerSize.X - widgetSize.X) / 2f;
        float cy = (containerSize.Y - widgetSize.Y) / 2f;
        float rx = containerSize.X - widgetSize.X;
        float ry = containerSize.Y - widgetSize.Y;

        return anchor switch
        {
            Anchor.TopLeft     => new Vector2(0,  0),
            Anchor.TopCenter   => new Vector2(cx, 0),
            Anchor.TopRight    => new Vector2(rx, 0),
            Anchor.MiddleLeft  => new Vector2(0,  cy),
            Anchor.Center      => new Vector2(cx, cy),
            Anchor.MiddleRight => new Vector2(rx, cy),
            Anchor.BottomLeft  => new Vector2(0,  ry),
            Anchor.BottomCenter => new Vector2(cx, ry),
            Anchor.BottomRight => new Vector2(rx, ry),
            _                  => Vector2.Zero,
        };
    }
}
