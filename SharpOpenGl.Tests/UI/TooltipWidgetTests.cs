using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class TooltipWidgetTests
{
    private static readonly Vector2 Viewport1024 = new(1024f, 768f);
    private static readonly Vector2 Viewport1920 = new(1920f, 1080f);

    [Fact]
    public void TooltipContent_ToLines_omits_empty_sections_and_formats_prereqs()
    {
        var content = new TooltipContent(
            Title: "Refinery",
            CostLine: "Cost: 200 minerals",
            Footprint: "Footprint: 2×2",
            BuildTime: "Build time: 30s",
            Prerequisites: ["Power Plant", "Extractor"],
            LockReason: "Locked: requires Power Plant",
            AffordReason: null);

        IReadOnlyList<string> lines = content.ToLines();

        Assert.Equal(7, lines.Count);
        Assert.Equal("Refinery", lines[0]);
        Assert.Equal("• Power Plant", lines[4]);
        Assert.Equal("• Extractor", lines[5]);
        Assert.DoesNotContain(lines, line => line.Contains("Afford", StringComparison.Ordinal));
    }

    [Fact]
    public void WrapLines_splits_long_line_at_max_width()
    {
        var source = new[] { "Alpha bravo charlie delta echo foxtrot golf hotel" };
        IReadOnlyList<string> wrapped = TooltipLayout.WrapLines(source, 120f, TooltipWidget.FontSize);

        Assert.True(wrapped.Count >= 2);
        foreach (string line in wrapped)
            Assert.True(UIFontMetrics.MeasureTextWidth(line, TooltipWidget.FontSize) <= 120f + 0.5f);
    }

    [Fact]
    public void MeasureTooltipSize_caps_box_width_at_max_text_width_plus_padding()
    {
        var content = new TooltipContent(
            Title: "Very long structure title that should wrap across multiple tooltip lines");

        var (_, size) = TooltipLayout.MeasureContent(content, TooltipWidget.MaxTextWidth, TooltipWidget.FontSize, TooltipWidget.Padding);
        float maxBoxWidth = TooltipWidget.MaxTextWidth + TooltipWidget.Padding * 2f;

        Assert.True(size.X <= maxBoxWidth + 0.5f);
        Assert.True(size.Y > 0f);
    }

    [Theory]
    [InlineData(950f, 700f)]
    [InlineData(5f, 5f)]
    public void ComputeBounds_keeps_tooltip_inside_1024x768_viewport(float pointerX, float pointerY)
    {
        var content = new TooltipContent(Title: "Structure", CostLine: "Cost: 100");
        var (_, tooltipSize) = TooltipLayout.MeasureContent(content);
        Vector2 pointer = new(pointerX, pointerY);

        Vector2 position = TooltipLayout.ComputeBounds(pointer, tooltipSize, Viewport1024, TooltipWidget.PointerOffset);

        Assert.True(position.X >= 0f);
        Assert.True(position.Y >= 0f);
        Assert.True(position.X + tooltipSize.X <= Viewport1024.X + 0.5f);
        Assert.True(position.Y + tooltipSize.Y <= Viewport1024.Y + 0.5f);
    }

    [Theory]
    [InlineData(1900f, 1050f)]
    [InlineData(10f, 10f)]
    public void ComputeBounds_keeps_tooltip_inside_1920x1080_viewport(float pointerX, float pointerY)
    {
        var content = new TooltipContent(
            Title: "Advanced Structure",
            CostLine: "Cost: 500 minerals / 200 gas",
            Prerequisites: ["Command Center", "Research Lab", "Power Plant"]);
        var (_, tooltipSize) = TooltipLayout.MeasureContent(content);
        Vector2 pointer = new(pointerX, pointerY);

        Vector2 position = TooltipLayout.ComputeBounds(pointer, tooltipSize, Viewport1920, TooltipWidget.PointerOffset);

        Assert.True(position.X >= 0f);
        Assert.True(position.Y >= 0f);
        Assert.True(position.X + tooltipSize.X <= Viewport1920.X + 0.5f);
        Assert.True(position.Y + tooltipSize.Y <= Viewport1920.Y + 0.5f);
    }

    [Fact]
    public void ComputeBounds_flips_left_when_default_anchor_overflows_right_edge()
    {
        Vector2 tooltipSize = new(200f, 80f);
        Vector2 pointer = new(950f, 200f);
        float offset = TooltipWidget.PointerOffset;

        Vector2 position = TooltipLayout.ComputeBounds(pointer, tooltipSize, Viewport1024, offset);
        float defaultX = pointer.X + offset;

        Assert.True(defaultX + tooltipSize.X > Viewport1024.X);
        Assert.True(position.X < pointer.X);
    }

    [Fact]
    public void SetHover_clears_immediately_and_hover_delay_before_show()
    {
        var widget = new TooltipWidget();
        var content = new TooltipContent(Title: "Refinery");

        widget.SetHover(content, new Vector2(100f, 100f));
        Assert.False(widget.IsShowing);

        widget.Update(0.1f);
        Assert.False(widget.IsShowing);

        widget.Update(0.15f);
        Assert.True(widget.IsShowing);

        widget.SetHover(null, Vector2.Zero);
        Assert.False(widget.IsShowing);
        Assert.Equal(0f, widget.HoverTimerSeconds);
    }

    [Fact]
    public void Widget_default_GetTooltipContent_returns_null()
    {
        var panel = new Panel();
        Assert.Null(panel.GetTooltipContent());
    }
}