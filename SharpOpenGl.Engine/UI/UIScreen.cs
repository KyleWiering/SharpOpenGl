using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI.Widgets;

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

    /// <summary>Route a scroll-wheel delta to scrollable widgets on this screen.</summary>
    public virtual bool HandleScroll(Vector2 screenPoint, float deltaY, Vector2 viewportSize)
    {
        if (!Visible) return false;
        for (int i = _roots.Count - 1; i >= 0; i--)
        {
            if (_roots[i].HandleScroll(screenPoint, deltaY, Vector2.Zero, viewportSize))
                return true;
        }
        return false;
    }

    /// <summary>Handle a navigation key pressed while this screen is active.</summary>
    public virtual bool HandleKey(UIKey key) => false;

    /// <summary>Return tooltip content from the topmost hovered provider on this screen.</summary>
    public TooltipContent? FindTooltipContent()
    {
        if (!Visible) return null;

        for (int i = _roots.Count - 1; i >= 0; i--)
        {
            TooltipContent? content = FindTooltipContentInWidget(_roots[i]);
            if (content != null)
                return content;
        }

        return null;
    }

    /// <summary>Return the topmost hovered button on this screen, if any.</summary>
    public Button? FindHoveredButton()
    {
        if (!Visible) return null;

        Button? hovered = null;
        for (int i = _roots.Count - 1; i >= 0; i--)
            hovered = FindHoveredButtonInWidget(_roots[i]) ?? hovered;
        return hovered;
    }

    private static TooltipContent? FindTooltipContentInWidget(Widget widget)
    {
        if (!widget.Visible) return null;

        for (int i = widget.Children.Count - 1; i >= 0; i--)
        {
            TooltipContent? childContent = FindTooltipContentInWidget(widget.Children[i]);
            if (childContent != null)
                return childContent;
        }

        if (widget is ITooltipProvider provider)
        {
            TooltipContent? content = provider.GetTooltipContent();
            if (content != null)
                return content;
        }

        return null;
    }

    private static Button? FindHoveredButtonInWidget(Widget widget)
    {
        if (widget is Button { IsHovered: true } button)
            return button;

        for (int i = widget.Children.Count - 1; i >= 0; i--)
        {
            Button? childMatch = FindHoveredButtonInWidget(widget.Children[i]);
            if (childMatch != null)
                return childMatch;
        }

        return null;
    }

    /// <summary>Find a button anywhere in the widget tree by <see cref="Widget.Name"/>.</summary>
    public Button? FindButton(string name)
    {
        foreach (Widget root in _roots)
        {
            Button? match = FindButtonInWidget(root, name);
            if (match != null)
                return match;
        }

        return null;
    }

    private static Button? FindButtonInWidget(Widget widget, string name)
    {
        if (widget is Button button && button.Name == name)
            return button;

        for (int i = widget.Children.Count - 1; i >= 0; i--)
        {
            Button? childMatch = FindButtonInWidget(widget.Children[i], name);
            if (childMatch != null)
                return childMatch;
        }

        return null;
    }

    protected void AddWidget(Widget widget) => _roots.Add(widget);
    protected void RemoveWidget(Widget widget) => _roots.Remove(widget);
}