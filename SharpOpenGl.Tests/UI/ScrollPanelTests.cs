using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class ScrollPanelTests
{
    [Fact]
    public void ScrollPanel_clamps_offset_to_content_height()
    {
        var panel = new ScrollPanel
        {
            Size = new Vector2(300f, 200f),
        };
        panel.AddChild(new Button
        {
            Position = new Vector2(0f, 0f),
            Size = new Vector2(280f, 48f),
        });
        panel.AddChild(new Button
        {
            Position = new Vector2(0f, 400f),
            Size = new Vector2(280f, 48f),
        });

        panel.RecalculateContentHeight(panel.Size);
        panel.ScrollBy(1000f, panel.Size);

        Assert.True(panel.ScrollOffsetY > 0f);
        Assert.True(panel.ScrollOffsetY <= panel.MaxScrollOffset(panel.Size));
    }

    [Fact]
    public void ScrollPanel_consumes_scroll_when_pointer_is_inside()
    {
        var panel = new ScrollPanel
        {
            Anchor = Anchor.TopLeft,
            Position = Vector2.Zero,
            Size = new Vector2(300f, 200f),
        };
        panel.AddChild(new Button
        {
            Position = new Vector2(0f, 500f),
            Size = new Vector2(280f, 48f),
        });
        panel.RecalculateContentHeight(panel.Size);

        bool consumed = panel.HandleScroll(new Vector2(50f, 50f), 1f, Vector2.Zero, UIScaler.ReferenceSize);
        Assert.True(consumed);
        Assert.True(panel.ScrollOffsetY > 0f);
    }

    [Fact]
    public void Label_MaxLines_limits_rendered_line_count()
    {
        var inner = new RecordingRenderer();
        var label = new Label
        {
            Text = "One two three four five six seven eight nine ten eleven twelve",
            Size = new Vector2(200f, 80f),
            FontSize = 20f,
            WrapWidth = 70f,
            MaxLines = 2,
        };

        label.Draw(inner, Vector2.Zero, new Vector2(200f, 80f));

        int unlimited = UITextDrawing.WrapText(label.Text, label.WrapWidth, label.FontSize).Count;
        Assert.True(unlimited > 2);
        Assert.Equal(2, inner.DrawnLines.Count);
    }

    private sealed class RecordingRenderer : IUIRenderer
    {
        public Vector2 ViewportSize { get; } = UIScaler.ReferenceSize;
        public List<string> DrawnLines { get; } = new();

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) { }
        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }

        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color) =>
            DrawnLines.Add(text);
    }
}