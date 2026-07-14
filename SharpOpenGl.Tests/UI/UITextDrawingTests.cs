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

    [Fact]
    public void TruncateWithEllipsis_shortens_text_that_exceeds_width()
    {
        string truncated = UITextDrawing.TruncateWithEllipsis(
            "Very long mission title that should not overflow", 120f, 20f);
        Assert.EndsWith("…", truncated);
        Assert.True(UIFontMetrics.MeasureTextWidth(truncated, 20f) <= 120f + 0.5f);
    }

    [Fact]
    public void WrapTextLimited_caps_line_count_with_ellipsis()
    {
        var lines = UITextDrawing.WrapTextLimited(
            "Alpha bravo charlie delta echo foxtrot golf hotel india juliet", 60f, 20f, 2);
        Assert.Equal(2, lines.Count);
        Assert.True(lines.Count < UITextDrawing.WrapText("Alpha bravo charlie delta echo foxtrot golf hotel india juliet", 60f, 20f).Count);
        Assert.EndsWith("…", lines[1]);
    }

    [Fact]
    public void ContentWrapWidth_subtracts_padding_from_container()
    {
        Assert.Equal(468f, UITextDrawing.ContentWrapWidth(500f, 16f));
    }
}