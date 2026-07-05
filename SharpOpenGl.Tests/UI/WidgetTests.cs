using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class WidgetTests
{
    private static readonly Vector2 Viewport = new(1920f, 1080f);

    // ── Anchor resolution ─────────────────────────────────────────────────────

    [Fact]
    public void Anchor_TopLeft_position_equals_offset()
    {
        var w = new Panel { Anchor = Anchor.TopLeft, Position = new Vector2(10f, 20f), Size = new Vector2(100f, 50f) };
        var (pos, _) = w.Resolve(Vector2.Zero, Viewport);
        Assert.Equal(new Vector2(10f, 20f), pos);
    }

    [Fact]
    public void Anchor_Center_positions_widget_in_viewport_centre()
    {
        var w = new Panel { Anchor = Anchor.Center, Position = Vector2.Zero, Size = new Vector2(200f, 100f) };
        var (pos, size) = w.Resolve(Vector2.Zero, Viewport);
        // Centre should be exactly at viewport mid-point.
        Assert.Equal(Viewport.X / 2f, pos.X + size.X / 2f, 0.01f);
        Assert.Equal(Viewport.Y / 2f, pos.Y + size.Y / 2f, 0.01f);
    }

    [Fact]
    public void Anchor_TopRight_aligns_right_edge()
    {
        var w = new Panel { Anchor = Anchor.TopRight, Position = Vector2.Zero, Size = new Vector2(150f, 50f) };
        var (pos, _) = w.Resolve(Vector2.Zero, Viewport);
        Assert.Equal(Viewport.X - 150f, pos.X, 0.01f);
    }

    [Fact]
    public void Anchor_BottomLeft_aligns_bottom_edge()
    {
        var w = new Panel { Anchor = Anchor.BottomLeft, Position = Vector2.Zero, Size = new Vector2(100f, 60f) };
        var (pos, _) = w.Resolve(Vector2.Zero, Viewport);
        Assert.Equal(Viewport.Y - 60f, pos.Y, 0.01f);
    }

    [Fact]
    public void Anchor_BottomRight_aligns_corner()
    {
        var w = new Panel { Anchor = Anchor.BottomRight, Position = Vector2.Zero, Size = new Vector2(120f, 80f) };
        var (pos, _) = w.Resolve(Vector2.Zero, Viewport);
        Assert.Equal(Viewport.X - 120f, pos.X, 0.01f);
        Assert.Equal(Viewport.Y - 80f,  pos.Y, 0.01f);
    }

    [Fact]
    public void Anchor_Stretch_fills_container()
    {
        var w = new Panel { Anchor = Anchor.Stretch, Position = new Vector2(4f, 4f) };
        var (pos, size) = w.Resolve(Vector2.Zero, Viewport);
        Assert.Equal(new Vector2(4f, 4f), pos);
        Assert.Equal(Viewport - new Vector2(8f, 8f), size);
    }

    [Fact]
    public void Position_offset_is_applied_after_anchor_origin()
    {
        var w = new Panel { Anchor = Anchor.TopRight, Position = new Vector2(-8f, 4f), Size = new Vector2(56f, 40f) };
        var (pos, _) = w.Resolve(Vector2.Zero, Viewport);
        // Expected: right edge aligned then offset by (-8, 4)
        Assert.Equal(Viewport.X - 56f - 8f, pos.X, 0.01f);
        Assert.Equal(4f, pos.Y, 0.01f);
    }

    // ── Hit testing ───────────────────────────────────────────────────────────

    [Fact]
    public void Contains_returns_true_for_point_inside_bounds()
    {
        var w = new Panel { Anchor = Anchor.TopLeft, Position = new Vector2(100f, 100f), Size = new Vector2(200f, 100f) };
        Assert.True(w.Contains(new Vector2(150f, 150f), Vector2.Zero, Viewport));
    }

    [Fact]
    public void Contains_returns_false_for_point_outside_bounds()
    {
        var w = new Panel { Anchor = Anchor.TopLeft, Position = new Vector2(100f, 100f), Size = new Vector2(200f, 100f) };
        Assert.False(w.Contains(new Vector2(50f, 50f), Vector2.Zero, Viewport));
    }

    // ── Hierarchy ─────────────────────────────────────────────────────────────

    [Fact]
    public void AddChild_sets_parent_reference()
    {
        var parent = new Panel { Anchor = Anchor.TopLeft, Position = Vector2.Zero, Size = new Vector2(400f, 300f) };
        var child  = new Panel { Anchor = Anchor.TopLeft, Position = Vector2.Zero, Size = new Vector2(100f, 100f) };
        parent.AddChild(child);
        Assert.Same(parent, child.Parent);
        Assert.Contains(child, parent.Children);
    }

    [Fact]
    public void RemoveChild_clears_parent_reference()
    {
        var parent = new Panel { Anchor = Anchor.TopLeft, Position = Vector2.Zero, Size = new Vector2(400f, 300f) };
        var child  = new Panel { Anchor = Anchor.TopLeft, Position = Vector2.Zero, Size = new Vector2(100f, 100f) };
        parent.AddChild(child);
        parent.RemoveChild(child);
        Assert.Null(child.Parent);
        Assert.DoesNotContain(child, parent.Children);
    }

    [Fact]
    public void AddChild_reparents_widget_from_old_parent()
    {
        var p1 = new Panel { Anchor = Anchor.TopLeft, Size = new Vector2(200f, 200f) };
        var p2 = new Panel { Anchor = Anchor.TopLeft, Size = new Vector2(200f, 200f) };
        var child = new Panel { Size = new Vector2(50f, 50f) };

        p1.AddChild(child);
        p2.AddChild(child);

        Assert.DoesNotContain(child, p1.Children);
        Assert.Contains(child, p2.Children);
        Assert.Same(p2, child.Parent);
    }

    // ── Button interaction ────────────────────────────────────────────────────

    [Fact]
    public void Button_Clicked_fires_on_pointer_tapped_inside()
    {
        var btn = new Button
        {
            Anchor   = Anchor.TopLeft,
            Position = new Vector2(100f, 100f),
            Size     = new Vector2(200f, 60f),
        };
        bool clicked = false;
        btn.Clicked += () => clicked = true;

        btn.HandlePointerTapped(new Vector2(150f, 130f), 0, Vector2.Zero, Viewport);

        Assert.True(clicked);
    }

    [Fact]
    public void Button_Clicked_does_not_fire_outside_bounds()
    {
        var btn = new Button
        {
            Anchor   = Anchor.TopLeft,
            Position = new Vector2(100f, 100f),
            Size     = new Vector2(200f, 60f),
        };
        bool clicked = false;
        btn.Clicked += () => clicked = true;

        btn.HandlePointerTapped(new Vector2(50f, 50f), 0, Vector2.Zero, Viewport);

        Assert.False(clicked);
    }

    [Fact]
    public void Button_Clicked_fires_at_top_edge_of_visual_bounds()
    {
        var btn = new Button
        {
            Anchor   = Anchor.TopLeft,
            Position = new Vector2(100f, 100f),
            Size     = new Vector2(200f, 60f),
        };
        bool clicked = false;
        btn.Clicked += () => clicked = true;

        btn.HandlePointerTapped(new Vector2(150f, 100f), 0, Vector2.Zero, Viewport);

        Assert.True(clicked);
    }

    [Fact]
    public void Button_Clicked_does_not_fire_below_bottom_edge()
    {
        var btn = new Button
        {
            Anchor   = Anchor.TopLeft,
            Position = new Vector2(100f, 100f),
            Size     = new Vector2(200f, 60f),
        };
        bool clicked = false;
        btn.Clicked += () => clicked = true;

        btn.HandlePointerTapped(new Vector2(150f, 160f), 0, Vector2.Zero, Viewport);

        Assert.False(clicked);
    }

    [Fact]
    public void Button_disabled_does_not_fire_Clicked()
    {
        var btn = new Button
        {
            Anchor    = Anchor.TopLeft,
            Position  = new Vector2(0f, 0f),
            Size      = new Vector2(200f, 60f),
            IsEnabled = false,
        };
        bool clicked = false;
        btn.Clicked += () => clicked = true;

        btn.HandlePointerTapped(new Vector2(100f, 30f), 0, Vector2.Zero, Viewport);

        Assert.False(clicked);
    }

    [Fact]
    public void Invisible_button_does_not_consume_taps()
    {
        var btn = new Button
        {
            Anchor  = Anchor.TopLeft,
            Position = Vector2.Zero,
            Size    = new Vector2(200f, 60f),
            Visible = false,
        };
        bool consumed = btn.HandlePointerTapped(new Vector2(100f, 30f), 0, Vector2.Zero, Viewport);
        Assert.False(consumed);
    }

    // ── UIScaler ─────────────────────────────────────────────────────────────

    [Fact]
    public void UIScaler_scale_is_one_at_reference_resolution()
    {
        var scaler = new UIScaler(UIScaler.ReferenceSize);
        Assert.Equal(1f, scaler.Scale.X, 0.001f);
        Assert.Equal(1f, scaler.Scale.Y, 0.001f);
    }

    [Fact]
    public void UIScaler_scale_doubles_at_double_resolution()
    {
        var scaler = new UIScaler(UIScaler.ReferenceSize * 2f);
        Assert.Equal(2f, scaler.Scale.X, 0.001f);
        Assert.Equal(2f, scaler.Scale.Y, 0.001f);
    }

    [Fact]
    public void UIScaler_resize_updates_scale()
    {
        var scaler = new UIScaler(UIScaler.ReferenceSize);
        scaler.Resize(new Vector2(960f, 540f));
        Assert.Equal(0.5f, scaler.Scale.X, 0.001f);
        Assert.Equal(0.5f, scaler.Scale.Y, 0.001f);
    }
}