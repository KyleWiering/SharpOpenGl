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

    private static readonly (char Left, char Right)[] BuildLabelConfusablePairs =
    [
        ('M', 'N'), ('S', '5'), ('I', '1'), ('O', '0'), ('G', 'C'), ('W', 'V'), ('6', '8'), ('5', '6'),
    ];

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
    [InlineData(8f)]
    [InlineData(9f)]
    [InlineData(12f)]
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

    public static IEnumerable<object[]> BuildLabelConfusablePairData() =>
        BuildLabelConfusablePairs.Select(pair => new object[] { pair.Left, pair.Right });

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