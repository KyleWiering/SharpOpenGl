using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.UI;

/// <summary>
/// Abstract base class for all UI elements.
/// </summary>
public abstract class Widget : ITooltipProvider
{
    private readonly List<Widget> _children = new();
    private Widget? _parent;

    public string? Name { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public Anchor Anchor { get; set; } = Anchor.TopLeft;
    public bool Visible { get; set; } = true;
    public Widget? Parent => _parent;
    public IReadOnlyList<Widget> Children => _children;

    /// <inheritdoc/>
    public virtual TooltipContent? GetTooltipContent() => null;

    public void AddChild(Widget child)
    {
        child._parent?.RemoveChild(child);
        child._parent = this;
        _children.Add(child);
    }

    public void RemoveChild(Widget child)
    {
        if (_children.Remove(child))
            child._parent = null;
    }

    public (Vector2 AbsolutePosition, Vector2 EffectiveSize) Resolve(
        Vector2 containerPosition, Vector2 containerSize)
    {
        if (Anchor == Anchor.Stretch)
        {
            Vector2 inset = Position;
            return (containerPosition + inset, containerSize - inset * 2f);
        }

        Vector2 origin = ResolveAnchorOrigin(containerSize, Size, Anchor);
        return (containerPosition + origin + Position, Size);
    }

    public bool Contains(Vector2 screenPoint, Vector2 containerPosition, Vector2 containerSize)
    {
        var (pos, size) = Resolve(containerPosition, containerSize);
        return screenPoint.X >= pos.X && screenPoint.X < pos.X + size.X
            && screenPoint.Y >= pos.Y && screenPoint.Y < pos.Y + size.Y;
    }

    public virtual bool HandlePointerTapped(
        Vector2 screenPoint, int button,
        Vector2 containerPosition, Vector2 containerSize)
    {
        if (!Visible) return false;

        var (pos, size) = Resolve(containerPosition, containerSize);
        for (int i = _children.Count - 1; i >= 0; i--)
        {
            if (_children[i].HandlePointerTapped(screenPoint, button, pos, size))
                return true;
        }
        return false;
    }

    public virtual void UpdatePointerState(
        Vector2 pointerPosition, bool isPointerDown,
        Vector2 containerPosition, Vector2 containerSize)
    {
        if (!Visible) return;

        var (pos, size) = Resolve(containerPosition, containerSize);
        foreach (Widget child in _children)
            child.UpdatePointerState(pointerPosition, isPointerDown, pos, size);
    }

    public virtual void Update(float deltaTime)
    {
        if (!Visible) return;
        foreach (Widget child in _children)
            child.Update(deltaTime);
    }

    /// <summary>Handle a scroll-wheel delta. Return true when consumed.</summary>
    public virtual bool HandleScroll(
        Vector2 screenPoint, float deltaY,
        Vector2 containerPosition, Vector2 containerSize)
    {
        if (!Visible) return false;

        var (pos, size) = Resolve(containerPosition, containerSize);
        for (int i = _children.Count - 1; i >= 0; i--)
        {
            if (_children[i].HandleScroll(screenPoint, deltaY, pos, size))
                return true;
        }

        return false;
    }

    public virtual void Draw(IUIRenderer renderer, Vector2 containerPosition, Vector2 containerSize)
    {
        if (!Visible) return;

        var (pos, size) = Resolve(containerPosition, containerSize);
        OnDraw(renderer, pos, size);

        foreach (Widget child in _children)
            child.Draw(renderer, pos, size);
    }

    protected virtual void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size) { }

    private static Vector2 ResolveAnchorOrigin(
        Vector2 containerSize, Vector2 widgetSize, Anchor anchor)
    {
        float cx = (containerSize.X - widgetSize.X) / 2f;
        float cy = (containerSize.Y - widgetSize.Y) / 2f;
        float rx = containerSize.X - widgetSize.X;
        float ry = containerSize.Y - widgetSize.Y;

        return anchor switch
        {
            Anchor.TopLeft => new Vector2(0, 0),
            Anchor.TopCenter => new Vector2(cx, 0),
            Anchor.TopRight => new Vector2(rx, 0),
            Anchor.MiddleLeft => new Vector2(0, cy),
            Anchor.Center => new Vector2(cx, cy),
            Anchor.MiddleRight => new Vector2(rx, cy),
            Anchor.BottomLeft => new Vector2(0, ry),
            Anchor.BottomCenter => new Vector2(cx, ry),
            Anchor.BottomRight => new Vector2(rx, ry),
            _ => Vector2.Zero,
        };
    }
}