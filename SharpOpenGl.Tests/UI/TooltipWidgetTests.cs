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
        var (tooltipSize, _, _) = TooltipLayout.MeasureScrollViewport(content);
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
        var (tooltipSize, _, _) = TooltipLayout.MeasureScrollViewport(content);
        Vector2 pointer = new(pointerX, pointerY);

        Vector2 position = TooltipLayout.ComputeBounds(pointer, tooltipSize, Viewport1920, TooltipWidget.PointerOffset);

        Assert.True(position.X >= 0f);
        Assert.True(position.Y >= 0f);
        Assert.True(position.X + tooltipSize.X <= Viewport1920.X + 0.5f);
        Assert.True(position.Y + tooltipSize.Y <= Viewport1920.Y + 0.5f);
    }

    [Fact]
    public void ComputeBounds_with_renderer_clamps_physical_right_edge_at_1024x768()
    {
        var content = new TooltipContent(
            Title: "Orbital Defense Relay Station Extended Range",
            CostLine: "Cost: 900 minerals / 400 gas",
            Footprint: "Footprint: 4×4",
            BuildTime: "Build: 90s",
            Prerequisites: ["Command Center", "Sensor Array", "Power Reactor"]);
        var inner = new BoundsRecordingRenderer(Viewport1024);
        var scaler = new UIScaler(Viewport1024);
        var scaledRenderer = new ScaledUIRenderer(inner, scaler);

        var widget = new TooltipWidget();
        widget.SetHover(content, new Vector2(400f, 400f));
        widget.Update(TooltipWidget.HoverShowDelaySeconds + 0.05f);
        widget.Draw(scaledRenderer, Vector2.Zero, Viewport1920);

        Vector2 origin = widget.ResolveTooltipOrigin(scaledRenderer, Viewport1920);
        float physX = scaledRenderer.ScaleToPhysical(origin.X);
        float physW = scaledRenderer.ScaleToPhysical(widget.TooltipSize.X);

        Assert.True(physX >= -0.5f);
        Assert.True(physX + physW <= Viewport1024.X + 2f);

        foreach (var draw in inner.TextDraws)
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            Assert.True(draw.Position.X + width <= Viewport1024.X + 2f, draw.Text);
        }
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
    public void TooltipContent_FromCommandButton_includes_button_label_for_abbreviated_commands()
    {
        TooltipContent content = TooltipContent.FromCommandButton("Attack Move", "Attack Move (A)");
        IReadOnlyList<string> lines = content.ToLines();

        Assert.Equal("Attack Move (A)", content.Title);
        Assert.Equal("Button: Attack Move", content.RoleLine);
        Assert.Equal(2, lines.Count);
    }

    [Fact]
    public void TooltipContent_FormatPrerequisiteChain_joins_locked_prerequisites()
    {
        var entry = new SharpOpenGl.Engine.Build.BuildMapEntryView
        {
            IsUnlocked = false,
            Prerequisites = ["Command Center", "Power Reactor"],
        };

        string? chain = TooltipContent.FormatPrerequisiteChain(entry);

        Assert.Equal("Prerequisite chain: Command Center → Power Reactor", chain);
    }

    [Fact]
    public void Widget_default_GetTooltipContent_returns_null()
    {
        var panel = new Panel();
        Assert.Null(panel.GetTooltipContent());
    }

    [Fact]
    public void TooltipWidget_long_prerequisite_chain_enables_scroll_viewport()
    {
        var widget = new TooltipWidget();
        var content = new TooltipContent(
            Title: "Capstone Structure",
            CostLine: "Cost: 900 minerals / 400 gas",
            Footprint: "Footprint: 4×4",
            BuildTime: "Build time: 120s",
            Prerequisites:
            [
                "Command Center",
                "Power Reactor",
                "Research Lab",
                "Advanced Factory",
                "Orbital Relay",
                "Defense Grid",
            ],
            LockReason: "Locked: complete prerequisite chain");

        widget.SetHover(content, new Vector2(200f, 200f));
        widget.Update(TooltipWidget.HoverShowDelaySeconds + 0.05f);

        float twoLineCap = TooltipLayout.ScrollViewportCapHeight(TooltipWidget.FontSize);
        Assert.True(widget.ScrollViewportHeight <= twoLineCap + 0.5f);
        Assert.True(widget.ContentHeight > widget.ScrollViewportHeight);
        Assert.True(widget.MaxScrollOffset > 0f);
    }

    [Fact]
    public void TooltipWidget_recalculates_content_height_after_set_hover()
    {
        var widget = new TooltipWidget();
        var shortContent = new TooltipContent(Title: "Refinery", CostLine: "Cost: 200");
        var longContent = new TooltipContent(
            Title: "Capstone Structure",
            CostLine: "Cost: 900 minerals / 400 gas",
            Prerequisites: ["Command Center", "Power Reactor", "Research Lab", "Advanced Factory", "Orbital Relay"],
            LockReason: "Locked: complete prerequisite chain");

        widget.SetHover(shortContent, new Vector2(100f, 100f));
        widget.Update(TooltipWidget.HoverShowDelaySeconds + 0.05f);
        float shortMaxScroll = widget.MaxScrollOffset;

        widget.SetHover(longContent, new Vector2(100f, 100f));
        widget.Update(TooltipWidget.HoverShowDelaySeconds + 0.05f);
        float longMaxScroll = widget.MaxScrollOffset;

        widget.SetHover(shortContent, new Vector2(100f, 100f));
        widget.Update(TooltipWidget.HoverShowDelaySeconds + 0.05f);
        float shortAgainMaxScroll = widget.MaxScrollOffset;

        Assert.Equal(0f, shortMaxScroll);
        Assert.True(longMaxScroll > shortMaxScroll);
        Assert.True(shortAgainMaxScroll < longMaxScroll);
        Assert.Equal(0f, shortAgainMaxScroll);
    }

    [Fact]
    public void TooltipWidget_wrap_width_uses_content_wrap_width()
    {
        var widget = new TooltipWidget();
        var content = new TooltipContent(
            Title: "Capstone Structure",
            Prerequisites: ["Command Center", "Power Reactor", "Research Lab", "Advanced Factory", "Orbital Relay"]);

        widget.SetHover(content, new Vector2(150f, 150f));
        widget.Update(TooltipWidget.HoverShowDelaySeconds + 0.05f);

        float gutter = widget.ScrollHost.ShowScrollbar ? 10f : 0f;
        float expectedWrap = UITextDrawing.ContentWrapWidth(widget.BodyLabel.Size.X - gutter, widget.BodyLabel.Padding);

        Assert.Equal(expectedWrap, widget.BodyLabel.WrapWidth);
    }

    [Fact]
    public void TooltipWidget_handle_scroll_consumes_wheel_over_visible_tooltip()
    {
        var widget = new TooltipWidget();
        var content = new TooltipContent(
            Title: "Capstone Structure",
            Prerequisites: ["Command Center", "Power Reactor", "Research Lab", "Advanced Factory", "Orbital Relay"],
            LockReason: "Locked: complete prerequisite chain");

        widget.SetHover(content, new Vector2(200f, 200f));
        widget.Update(TooltipWidget.HoverShowDelaySeconds + 0.05f);

        Vector2 pointer = new(250f, 250f);
        bool consumed = widget.HandleScroll(pointer, 1f, Vector2.Zero, Viewport1024);

        Assert.True(consumed);
        Assert.True(widget.ScrollHost.ScrollOffsetY > 0f);
    }

    private sealed class BoundsRecordingRenderer : IUIRenderer
    {
        public BoundsRecordingRenderer(Vector2 viewport) => ViewportSize = viewport;
        public Vector2 ViewportSize { get; }
        public List<(string Text, float FontSize, Vector2 Position)> TextDraws { get; } = new();

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) { }
        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }
        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color) =>
            TextDraws.Add((text, fontSize, position));
    }
}