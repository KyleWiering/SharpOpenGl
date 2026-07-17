using SharpOpenGl.Engine.UI;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class UIFontMetricsTests
{
    [Fact]
    public void FitFontSize_shrinks_until_text_fits()
    {
        float size = UIFontMetrics.FitFontSize("Attack-Move", 20f, 90f, 10f);
        Assert.True(size < 20f);
        Assert.True(UIFontMetrics.MeasureTextWidth("Attack-Move", size) <= 90f);
    }

    [Theory]
    [InlineData(8f, 1.5f)]
    [InlineData(9f, 1.5f)]
    [InlineData(12f, 1.5f)]
    [InlineData(16f, 1.5f)]
    [InlineData(20f, 1.5f)]
    public void GetLineThickness_clamps_to_minimum(float fontSize, float minimum)
    {
        Assert.True(UIFontMetrics.GetLineThickness(fontSize) >= minimum);
    }

    [Fact]
    public void FitFontSize_holds_8px_floor_for_tight_build_label_micro_width()
    {
        const float preferredSize = 9f;
        const float minSize = 8f;
        const float maxLabelWidth = 28f;

        float size = UIFontMetrics.FitFontSize("Sensor Array", preferredSize, maxLabelWidth, minSize);

        Assert.Equal(minSize, size);
        Assert.True(UIFontMetrics.GetLineThickness(size) >= UIFontMetrics.MinLineThickness);
    }

    [Fact]
    public void FitFontSize_respects_build_map_micro_label_width_at_9px()
    {
        const float preferredSize = 9f;
        const float minSize = 8f;
        const float maxLabelWidth = 60f;
        const string label = "Needs Sensor";

        float size = UIFontMetrics.FitFontSize(label, preferredSize, maxLabelWidth, minSize);

        Assert.True(size >= minSize);
        Assert.True(size <= preferredSize);
        Assert.True(UIFontMetrics.MeasureTextWidth(label, size) <= maxLabelWidth + 0.5f || size == minSize);
    }
}