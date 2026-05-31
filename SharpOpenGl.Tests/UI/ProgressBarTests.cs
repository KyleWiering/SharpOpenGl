using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class ProgressBarTests
{
    private static readonly Vector2 Viewport = new(1920f, 1080f);

    [Fact]
    public void Value_defaults_to_zero()
    {
        var bar = new ProgressBar();
        Assert.Equal(0f, bar.Value);
    }

    [Fact]
    public void Value_clamps_above_one()
    {
        var bar = new ProgressBar { Value = 2f };
        Assert.Equal(1f, bar.Value);
    }

    [Fact]
    public void Value_clamps_below_zero()
    {
        var bar = new ProgressBar { Value = -0.5f };
        Assert.Equal(0f, bar.Value);
    }

    [Fact]
    public void Value_accepts_valid_fraction()
    {
        var bar = new ProgressBar { Value = 0.42f };
        Assert.Equal(0.42f, bar.Value, 0.0001f);
    }

    [Fact]
    public void Visible_false_prevents_update_of_children()
    {
        var bar = new ProgressBar
        {
            Anchor  = Anchor.TopLeft,
            Position = Vector2.Zero,
            Size    = new Vector2(400f, 24f),
            Visible = false,
        };
        // Should not throw even when invisible.
        bar.Update(0.016f);
    }

    [Fact]
    public void Label_can_be_set_and_read()
    {
        var bar = new ProgressBar { Label = "Loading…" };
        Assert.Equal("Loading…", bar.Label);
    }

    [Fact]
    public void HitTest_contains_inside_point()
    {
        var bar = new ProgressBar
        {
            Anchor   = Anchor.TopLeft,
            Position = new Vector2(100f, 200f),
            Size     = new Vector2(600f, 28f),
        };
        Assert.True(bar.Contains(new Vector2(400f, 214f), Vector2.Zero, Viewport));
    }

    [Fact]
    public void HitTest_excludes_outside_point()
    {
        var bar = new ProgressBar
        {
            Anchor   = Anchor.TopLeft,
            Position = new Vector2(100f, 200f),
            Size     = new Vector2(600f, 28f),
        };
        Assert.False(bar.Contains(new Vector2(50f, 214f), Vector2.Zero, Viewport));
    }
}
