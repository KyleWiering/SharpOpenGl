using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class TextFieldTests
{
    private const float FieldWidth = 200f;
    private const float FieldHeight = 52f;
    private const float InnerWidth = FieldWidth - 24f;

    [Fact]
    public void TextField_long_value_exceeds_viewport_without_ellipsis()
    {
        string value = new string('W', 40);
        var field = CreateField(value);
        var renderer = new RecordingRenderer();

        float textWidth = UIFontMetrics.MeasureTextWidth(value, field.FontSize);
        Assert.True(textWidth > InnerWidth);
        Assert.Equal(0f, field.ScrollOffsetX);

        field.Draw(renderer, Vector2.Zero, UIScaler.ReferenceSize);

        Assert.DoesNotContain("…", renderer.LastText);
        float visibleWidth = UIFontMetrics.MeasureTextWidth(renderer.LastText!, field.FontSize);
        Assert.True(visibleWidth <= InnerWidth + UITextDrawing.WidthTolerance);
        Assert.True(field.MaxScrollOffset(field.Size, renderer) > 0f);
    }

    [Fact]
    public void TextField_scroll_offset_reveals_tail_of_long_value()
    {
        string value = "START-" + new string('M', 48) + "-END";
        var field = CreateField(value);
        var renderer = new RecordingRenderer();

        field.ScrollToStart();
        field.Draw(renderer, Vector2.Zero, UIScaler.ReferenceSize);
        Assert.StartsWith("START", renderer.LastText);
        Assert.DoesNotContain("END", renderer.LastText);

        field.ScrollToEnd(field.Size, renderer);
        Assert.True(field.ScrollOffsetX > 0f);
        field.Draw(renderer, Vector2.Zero, UIScaler.ReferenceSize);
        Assert.EndsWith("END", renderer.LastText);
        Assert.DoesNotContain("START", renderer.LastText);
    }

    [Fact]
    public void TextField_wheel_and_programmatic_scroll_change_offset()
    {
        string value = new string('X', 40);
        var field = CreateField(value);
        var renderer = new RecordingRenderer();
        field.IsKeyboardFocused = true;

        Assert.Equal(0f, field.ScrollOffsetX);

        bool consumed = field.HandleScroll(new Vector2(10f, 10f), 1f, Vector2.Zero, UIScaler.ReferenceSize);
        Assert.True(consumed);
        Assert.True(field.ScrollOffsetX > 0f);

        float afterWheel = field.ScrollOffsetX;
        field.ScrollHorizontalBy(96f, field.Size, renderer);
        Assert.True(field.ScrollOffsetX > afterWheel);

        field.ScrollToStart();
        Assert.Equal(0f, field.ScrollOffsetX);
    }

    [Fact]
    public void TextField_short_value_keeps_zero_offset()
    {
        var field = CreateField("hi");
        var renderer = new RecordingRenderer();

        field.Draw(renderer, Vector2.Zero, UIScaler.ReferenceSize);

        Assert.Equal(0f, field.ScrollOffsetX);
        Assert.Equal("hi", renderer.LastText);
        Assert.Equal(0f, field.MaxScrollOffset(field.Size, renderer));
    }

    [Fact]
    public void TextField_long_placeholder_scrolls_when_value_empty()
    {
        string placeholder = new string('P', 40);
        var field = new TextField
        {
            Value = string.Empty,
            Placeholder = placeholder,
            Size = new Vector2(FieldWidth, FieldHeight),
            FontSize = 20f,
            IsKeyboardFocused = true,
        };
        var renderer = new RecordingRenderer();

        field.Draw(renderer, Vector2.Zero, UIScaler.ReferenceSize);
        Assert.DoesNotContain("…", renderer.LastText);
        float visibleWidth = UIFontMetrics.MeasureTextWidth(renderer.LastText!, field.FontSize);
        Assert.True(visibleWidth <= InnerWidth + UITextDrawing.WidthTolerance);

        field.ScrollToEnd(field.Size, renderer);
        Assert.True(field.ScrollOffsetX > 0f);
        field.Draw(renderer, Vector2.Zero, UIScaler.ReferenceSize);
        Assert.EndsWith("P", renderer.LastText);
    }

    [Fact]
    public void TextField_typing_scrolls_to_end_for_caret_visibility()
    {
        string seed = new string('A', 38);
        var field = CreateField(seed);
        var renderer = new RecordingRenderer();
        field.IsKeyboardFocused = true;
        field.ScrollToStart();

        Assert.True(field.HandleChar('Z'));
        Assert.True(field.ScrollOffsetX > 0f);
        field.Draw(renderer, Vector2.Zero, UIScaler.ReferenceSize);
        Assert.EndsWith("Z", renderer.LastText);
    }

    private static TextField CreateField(string value) =>
        new()
        {
            Value = value,
            Size = new Vector2(FieldWidth, FieldHeight),
            FontSize = 20f,
        };

    private sealed class RecordingRenderer : IUIRenderer
    {
        public Vector2 ViewportSize { get; } = UIScaler.ReferenceSize;
        public string? LastText { get; private set; }

        public float ScaleToPhysical(float logicalPixels) => logicalPixels;
        public float ResolveFontSize(float logicalFontSize) => logicalFontSize;

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) { }
        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }

        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color) =>
            LastText = text;
    }
}