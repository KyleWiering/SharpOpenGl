using SharpOpenGl.Engine.UI;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class UITextDrawingTests
{
    [Fact]
    public void WrapText_splits_long_line_by_words()
    {
        var lines = UITextDrawing.WrapText("Move fleet to waypoint", 120f, 20f);
        Assert.True(lines.Count >= 2);
    }

    [Fact]
    public void WrapText_preserves_explicit_newlines()
    {
        var lines = UITextDrawing.WrapText("Line one\nLine two", 500f, 20f);
        Assert.Equal(2, lines.Count);
        Assert.Equal("Line one", lines[0]);
        Assert.Equal("Line two", lines[1]);
    }

    [Fact]
    public void MeasureTextBlockHeight_scales_with_line_count()
    {
        float one = UITextDrawing.MeasureTextBlockHeight("Hello", 20f, 200f);
        float two = UITextDrawing.MeasureTextBlockHeight("Hello\nWorld", 20f, 200f);
        Assert.True(two > one);
    }
}