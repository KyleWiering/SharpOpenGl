using SharpOpenGl.Engine.UI;
using Xunit;

namespace SharpOpenGl.Tests.UI;

/// <summary>
/// Locks segment-font distinctness for characters that appear on build-tree structure labels.
/// </summary>
public class BuildLabelGlyphTests
{
    private static readonly string[] StructureDisplayNames =
    [
        "Production", "Economy", "Defense", "Support", "Capstone",
        "Small Shipyard", "Medium Shipyard", "Large Shipyard",
        "Command Center", "Power Reactor", "Resource Refinery", "Supply Depot",
        "Fabrication Hub", "Sensor Array", "Defense Turret", "Shield Emitter",
        "Missile Battery", "Repair Bay", "Comms Relay", "Orbital Uplink", "Fortress Core",
    ];

    private static readonly string[] StructureNamesWithS =
    [
        "Sensor Array", "Small Shipyard", "Defense", "Missile Battery",
    ];

    private static readonly (char Left, char Right)[] BuildLabelConfusablePairs =
    [
        ('M', 'N'), ('S', '5'), ('I', '1'), ('I', 'L'), ('O', '0'), ('G', 'C'), ('W', 'V'), ('6', '8'), ('5', '6'),
    ];

    private static readonly (char Left, char Right)[] MicroConfusablePairs =
    [
        ('S', '5'), ('O', '0'), ('I', '1'), ('I', 'L'),
    ];

    private static readonly float[] MicroFontSizes = [8f, 12f, 16f, 20f];

    [Fact]
    public void Build_label_alphabet_chars_have_explicit_glyphs()
    {
        foreach (char c in GetBuildLabelAlphabet())
            AssertNonDefaultGlyph(c);
    }

    [Theory]
    [MemberData(nameof(BuildLabelConfusablePairData))]
    public void Build_label_confusable_pairs_have_distinct_signatures(char left, char right)
    {
        Assert.NotEqual(
            UIFontGlyphSegments.GetSignature(left),
            UIFontGlyphSegments.GetSignature(right));
    }

    [Theory]
    [MemberData(nameof(MicroConfusablePairSizeData))]
    public void Micro_confusable_pairs_have_distinct_signatures_and_readable_stroke(
        char left, char right, float fontSize)
    {
        Assert.NotEqual(
            UIFontGlyphSegments.GetSignature(left),
            UIFontGlyphSegments.GetSignature(right));
        Assert.True(UIFontMetrics.GetLineThickness(fontSize) >= UIFontMetrics.MinLineThickness);
    }

    [Theory]
    [InlineData(8f)]
    [InlineData(9f)]
    [InlineData(12f)]
    [InlineData(16f)]
    [InlineData(20f)]
    public void Build_label_micro_font_sizes_meet_line_thickness_minimum(float fontSize)
    {
        Assert.True(UIFontMetrics.GetLineThickness(fontSize) >= UIFontMetrics.MinLineThickness);
    }

    [Fact]
    public void Build_label_alphabet_includes_priority_structure_chars()
    {
        var alphabet = GetBuildLabelAlphabet();
        foreach (char required in "MSIOCG".ToCharArray())
            Assert.Contains(required, alphabet);
    }

    [Theory]
    [MemberData(nameof(StructureNamesWithSData))]
    public void Structure_display_names_with_S_use_explicit_non_default_glyphs(string name)
    {
        Assert.True(name.IndexOf('S', StringComparison.OrdinalIgnoreCase) >= 0, $"Expected S in structure name: {name}");
        AssertNonDefaultGlyph('S');
        Assert.Contains(UIFontGlyphSegments.Segment.TopPeakLeft, UIFontGlyphSegments.GetSegments('S'));
        Assert.Contains(UIFontGlyphSegments.Segment.BottomValleyRight, UIFontGlyphSegments.GetSegments('S'));
    }

    [Theory]
    [InlineData(8f)]
    [InlineData(9f)]
    [InlineData(12f)]
    public void Single_char_S_measure_text_width_stable_at_micro_sizes(float fontSize)
    {
        float width = UIFontMetrics.MeasureTextWidth("S", fontSize);
        float expected = UIFontMetrics.GetCharWidth(fontSize);
        Assert.Equal(expected, width, precision: 3);
        Assert.True(width > 0f);
    }

    [Fact]
    public void Build_map_locked_micro_label_fit_holds_8px_floor_for_S_names()
    {
        const float preferredSize = 9f;
        const float minSize = 8f;
        const float maxLabelWidth = 60f;

        foreach (string name in StructureNamesWithS)
        {
            string label = $"Needs {name}";
            float size = UIFontMetrics.FitFontSize(label, preferredSize, maxLabelWidth, minSize);
            Assert.True(size >= minSize);
        }
    }

    public static IEnumerable<object[]> BuildLabelConfusablePairData() =>
        BuildLabelConfusablePairs.Select(pair => new object[] { pair.Left, pair.Right });

    public static IEnumerable<object[]> MicroConfusablePairSizeData()
    {
        foreach (var pair in MicroConfusablePairs)
        foreach (float size in MicroFontSizes)
            yield return new object[] { pair.Left, pair.Right, size };
    }

    public static IEnumerable<object[]> StructureNamesWithSData() =>
        StructureNamesWithS.Select(name => new object[] { name });

    private static HashSet<char> GetBuildLabelAlphabet()
    {
        var chars = new HashSet<char>();
        foreach (string name in StructureDisplayNames)
        {
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c))
                    chars.Add(char.ToUpperInvariant(c));
            }
        }

        chars.Add('·');
        chars.Add(':');
        return chars;
    }

    private static void AssertNonDefaultGlyph(char c)
    {
        var segments = UIFontGlyphSegments.GetSegments(c);
        Assert.NotEmpty(segments);
        Assert.NotEqual(UIFontGlyphSegments.GetSignature('-'), UIFontGlyphSegments.GetSignature(c));
    }
}