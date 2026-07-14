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
    public void Priority_pairs_have_distinct_segment_signatures(char left, char right)
    {
        Assert.NotEqual(UIFontGlyphSegments.GetSignature(left), UIFontGlyphSegments.GetSignature(right));
    }

    [Theory]
    [InlineData(12f)]
    [InlineData(16f)]
    public void Line_thickness_meets_minimum_at_small_sizes(float fontSize)
    {
        Assert.True(UIFontMetrics.GetLineThickness(fontSize) >= UIFontMetrics.MinLineThickness);
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