using SharpOpenGl.Engine.UI;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class GlyphSegmentTests
{
    [Theory]
    [InlineData('O', '0')]
    [InlineData('M', 'N')]
    [InlineData('G', 'C')]
    [InlineData('W', 'V')]
    [InlineData('5', '6')]
    [InlineData('6', '8')]
    [InlineData('S', '5')]
    [InlineData('I', '1')]
    [InlineData('I', 'L')]
    public void Priority_pairs_have_distinct_segment_signatures(char left, char right)
    {
        Assert.NotEqual(UIFontGlyphSegments.GetSignature(left), UIFontGlyphSegments.GetSignature(right));
    }

    [Theory]
    [InlineData(8f)]
    [InlineData(12f)]
    [InlineData(16f)]
    [InlineData(20f)]
    public void Line_thickness_meets_minimum_at_small_sizes(float fontSize)
    {
        Assert.True(UIFontMetrics.GetLineThickness(fontSize) >= UIFontMetrics.MinLineThickness);
    }

    [Fact]
    public void S_uses_opposing_curve_segments_distinct_from_5()
    {
        var sSegments = UIFontGlyphSegments.GetSegments('S');
        Assert.Contains(UIFontGlyphSegments.Segment.TopPeakLeft, sSegments);
        Assert.Contains(UIFontGlyphSegments.Segment.BottomValleyRight, sSegments);
        Assert.DoesNotContain(UIFontGlyphSegments.Segment.TopHalfLeft, sSegments);
        Assert.DoesNotContain(UIFontGlyphSegments.Segment.BottomHalfLeft, sSegments);
        Assert.DoesNotContain(UIFontGlyphSegments.Segment.Top, sSegments);

        var fiveSegments = UIFontGlyphSegments.GetSegments('5');
        Assert.Contains(UIFontGlyphSegments.Segment.Top, fiveSegments);
        Assert.Contains(UIFontGlyphSegments.Segment.TopHalfRight, fiveSegments);
        Assert.DoesNotContain(UIFontGlyphSegments.Segment.TopPeakLeft, fiveSegments);
        Assert.DoesNotContain(UIFontGlyphSegments.Segment.BottomValleyRight, fiveSegments);
    }

    [Fact]
    public void Zero_has_diagonal_slash_distinct_from_O()
    {
        var zero = UIFontGlyphSegments.GetSegments('0');
        var o = UIFontGlyphSegments.GetSegments('O');
        Assert.Contains(UIFontGlyphSegments.Segment.DiagTopRightToBottomLeft, zero);
        Assert.DoesNotContain(UIFontGlyphSegments.Segment.DiagTopRightToBottomLeft, o);
    }

    [Theory]
    [InlineData(8f)]
    [InlineData(12f)]
    [InlineData(16f)]
    [InlineData(20f)]
    public void O_and_zero_remain_distinct_at_menu_font_sizes(float fontSize)
    {
        Assert.NotEqual(UIFontGlyphSegments.GetSignature('O'), UIFontGlyphSegments.GetSignature('0'));

        var zero = UIFontGlyphSegments.GetSegments('0');
        var o = UIFontGlyphSegments.GetSegments('O');
        Assert.Contains(UIFontGlyphSegments.Segment.DiagTopRightToBottomLeft, zero);
        Assert.DoesNotContain(UIFontGlyphSegments.Segment.DiagTopRightToBottomLeft, o);
        Assert.True(UIFontMetrics.GetLineThickness(fontSize) >= UIFontMetrics.MinLineThickness);
    }

    [Fact]
    public void I_1_L_have_distinct_serif_and_stem_segments()
    {
        var i = UIFontGlyphSegments.GetSegments('I');
        var one = UIFontGlyphSegments.GetSegments('1');
        var l = UIFontGlyphSegments.GetSegments('L');
        Assert.Contains(UIFontGlyphSegments.Segment.TopCenterCap, i);
        Assert.Contains(UIFontGlyphSegments.Segment.BottomCenterCap, i);
        Assert.Contains(UIFontGlyphSegments.Segment.TopTick, one);
        Assert.DoesNotContain(UIFontGlyphSegments.Segment.TopCenterCap, one);
        Assert.Contains(UIFontGlyphSegments.Segment.TopLeft, l);
        Assert.DoesNotContain(UIFontGlyphSegments.Segment.CenterVert, l);
    }

    [Fact]
    public void All_alphanumeric_glyphs_have_explicit_non_default_segments()
    {
        for (char c = 'A'; c <= 'Z'; c++)
            AssertNonDefaultGlyph(c);

        for (char c = '0'; c <= '9'; c++)
            AssertNonDefaultGlyph(c);
    }

    [Fact]
    public void Alphanumeric_glyphs_have_unique_segment_signatures()
    {
        var signatures = new Dictionary<string, char>();
        for (char c = 'A'; c <= 'Z'; c++)
            TrackSignature(signatures, c);

        for (char c = '0'; c <= '9'; c++)
            TrackSignature(signatures, c);
    }

    [Theory]
    [InlineData('%')]
    [InlineData(',')]
    [InlineData('\'')]
    [InlineData('!')]
    [InlineData('?')]
    [InlineData(':')]
    [InlineData('.')]
    [InlineData('/')]
    public void Hud_punctuation_has_explicit_glyph(char symbol)
    {
        var segments = UIFontGlyphSegments.GetSegments(symbol);
        Assert.NotEmpty(segments);
    }

    [Fact]
    public void Dash_has_middle_segment_only()
    {
        Assert.Equal("Middle", UIFontGlyphSegments.GetSignature('-'));
    }

    private static void AssertNonDefaultGlyph(char c)
    {
        var segments = UIFontGlyphSegments.GetSegments(c);
        Assert.NotEmpty(segments);
        Assert.NotEqual(UIFontGlyphSegments.GetSignature('-'), UIFontGlyphSegments.GetSignature(c));
    }

    private static void TrackSignature(Dictionary<string, char> signatures, char c)
    {
        string signature = UIFontGlyphSegments.GetSignature(c);
        Assert.True(signatures.TryAdd(signature, c),
            $"Duplicate segment map: '{signatures[signature]}' and '{c}' share signature [{signature}]");
    }
}