using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>
/// Panel that scrolls its children vertically when content exceeds the visible area.
/// </summary>
public sealed class ScrollPanel : Panel
{
    /// <summary>Vertical scroll offset in logical pixels.</summary>
    public float ScrollOffsetY { get; private set; }

    /// <summary>Distance moved per scroll-wheel notch or programmatic step.</summary>
    public float ScrollStep { get; set; } = 48f;

    /// <summary>Padding around laid-out content when computing scroll extents.</summary>
    public float ContentPadding { get; set; } = 8f;

    /// <summary>Whether to draw a slim scrollbar track when content overflows.</summary>
    public bool ShowScrollbar { get; set; } = true;

    /// <summary>
    /// When false, <see cref="SyncLabelWrapWidths"/> is a no-op so callers can manage
    /// <see cref="Label.WrapWidth"/> manually (e.g. compact-viewport scale-aware layout).
    /// </summary>
    public bool AutoSyncWrapWidths { get; set; } = true;

    /// <summary>Right inset reserved for the slim scrollbar track (matches <see cref="DrawScrollbar"/>).</summary>
    private const float ScrollbarGutter = 10f;

    /// <summary>Measured bottom extent of child layout relative to the panel top.</summary>
    public float ContentHeight { get; private set; }

    /// <summary>Maximum scroll offset based on content and visible height.</summary>
    public float MaxScrollOffset(Vector2 visibleSize) =>
        Math.Max(0f, ContentHeight - visibleSize.Y);

    /// <summary>
    /// Sync <see cref="Label.WrapWidth"/> on direct label children from layout width and padding.
    /// Accounts for scrollbar gutter when <see cref="ShowScrollbar"/> is enabled.
    /// </summary>
    public void SyncLabelWrapWidths()
    {
        if (!AutoSyncWrapWidths)
            return;

        float gutter = ShowScrollbar ? ScrollbarGutter : 0f;
        foreach (Widget child in Children)
        {
            if (child is not Label label || !child.Visible) continue;
            label.WrapWidth = UITextDrawing.ContentWrapWidth(label.Size.X - gutter, label.Padding);
        }
    }

    /// <summary>Recompute <see cref="ContentHeight"/> from child anchors and sizes.</summary>
    public void RecalculateContentHeight(Vector2 visibleSize)
    {
        SyncLabelWrapWidths();

        float maxBottom = ContentPadding;
        foreach (Widget child in Children)
        {
            if (!child.Visible) continue;

            float childBottom = child is Label label
                ? label.Position.Y + label.MeasureContentHeight()
                : child.Position.Y + child.Size.Y;
            maxBottom = MathF.Max(maxBottom, childBottom + ContentPadding);
        }

        ContentHeight = maxBottom;
        ScrollOffsetY = Math.Clamp(ScrollOffsetY, 0f, MaxScrollOffset(visibleSize));
    }

    /// <summary>Scroll by <paramref name="deltaY"/> pixels (positive scrolls down).</summary>
    public void ScrollBy(float deltaY, Vector2 visibleSize)
    {
        RecalculateContentHeight(visibleSize);
        ScrollOffsetY = Math.Clamp(ScrollOffsetY + deltaY, 0f, MaxScrollOffset(visibleSize));
    }

    /// <inheritdoc/>
    public override void Draw(IUIRenderer renderer, Vector2 containerPosition, Vector2 containerSize)
    {
        if (!Visible) return;

        var (pos, size) = Resolve(containerPosition, containerSize);
        RecalculateContentHeight(size);
        OnDraw(renderer, pos, size);

        Vector2 scrolledOrigin = pos - new Vector2(0f, ScrollOffsetY);
        foreach (Widget child in Children)
        {
            if (!child.Visible) continue;

            var (childPos, childSize) = child.Resolve(scrolledOrigin, size);
            if (!IntersectsVertical(childPos.Y, childSize.Y, pos.Y, size.Y))
                continue;

            child.Draw(renderer, scrolledOrigin, size);
        }

        if (ShowScrollbar && MaxScrollOffset(size) > 0f)
            DrawScrollbar(renderer, pos, size);
    }

    /// <inheritdoc/>
    public override bool HandlePointerTapped(
        Vector2 screenPoint, int button,
        Vector2 containerPosition, Vector2 containerSize)
    {
        if (!Visible) return false;

        var (pos, size) = Resolve(containerPosition, containerSize);
        RecalculateContentHeight(size);
        Vector2 scrolledOrigin = pos - new Vector2(0f, ScrollOffsetY);

        for (int i = Children.Count - 1; i >= 0; i--)
        {
            if (Children[i].HandlePointerTapped(screenPoint, button, scrolledOrigin, size))
                return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public override void UpdatePointerState(
        Vector2 pointerPosition, bool isPointerDown,
        Vector2 containerPosition, Vector2 containerSize)
    {
        if (!Visible) return;

        var (pos, size) = Resolve(containerPosition, containerSize);
        RecalculateContentHeight(size);
        Vector2 scrolledOrigin = pos - new Vector2(0f, ScrollOffsetY);

        foreach (Widget child in Children)
            child.UpdatePointerState(pointerPosition, isPointerDown, scrolledOrigin, size);
    }

    /// <inheritdoc/>
    public override bool HandleScroll(
        Vector2 screenPoint, float deltaY,
        Vector2 containerPosition, Vector2 containerSize)
    {
        if (HandleScrollWheel(screenPoint, deltaY, containerPosition, containerSize))
            return true;

        return base.HandleScroll(screenPoint, deltaY, containerPosition, containerSize);
    }

    /// <summary>Handle a scroll-wheel delta when the pointer is over this panel.</summary>
    public bool HandleScrollWheel(Vector2 screenPoint, float deltaY, Vector2 containerPosition, Vector2 containerSize)
    {
        if (!Visible || MathF.Abs(deltaY) < 0.001f) return false;

        var (pos, size) = Resolve(containerPosition, containerSize);
        if (screenPoint.X < pos.X || screenPoint.X >= pos.X + size.X ||
            screenPoint.Y < pos.Y || screenPoint.Y >= pos.Y + size.Y)
            return false;

        ScrollBy(deltaY > 0f ? ScrollStep : -ScrollStep, size);
        return true;
    }

    private void DrawScrollbar(IUIRenderer renderer, Vector2 pos, Vector2 size)
    {
        const float trackW = 6f;
        float trackX = pos.X + size.X - trackW - 4f;
        float trackY = pos.Y + 4f;
        float trackH = size.Y - 8f;
        float maxScroll = MaxScrollOffset(size);
        float thumbH = MathF.Max(24f, trackH * (size.Y / ContentHeight));
        float thumbY = trackY + (trackH - thumbH) * (ScrollOffsetY / maxScroll);

        renderer.DrawRect(new Vector2(trackX, trackY), new Vector2(trackW, trackH),
            new Vector4(0.12f, 0.14f, 0.2f, 0.65f));
        renderer.DrawRect(new Vector2(trackX, thumbY), new Vector2(trackW, thumbH),
            new Vector4(0.45f, 0.58f, 0.82f, 0.9f));
    }

    private static bool IntersectsVertical(float itemY, float itemH, float viewY, float viewH) =>
        itemY + itemH > viewY && itemY < viewY + viewH;
}