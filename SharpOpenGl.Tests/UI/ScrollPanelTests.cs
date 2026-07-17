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
    public void ScrollPanel_briefing_pattern_recalculates_after_children_added()
    {
        var panel = new ScrollPanel
        {
            Size = new Vector2(1728f, 428f),
            ContentPadding = 4f,
        };

        string paragraph = string.Join(
            " ",
            Enumerable.Repeat(
                "Commander, reconnaissance probes report hostile staging areas along the contested frontier corridor.",
                8));

        float y = 0f;
        for (int i = 0; i < 5; i++)
        {
            var label = new Label
            {
                Position = new Vector2(0f, y),
                Size = new Vector2(1728f, 40f),
                Text = $"{paragraph} Briefing paragraph {i}.",
                FontSize = 20f,
            };
            panel.AddChild(label);
            panel.RecalculateContentHeight(panel.Size);
            y += label.MeasureContentHeight();
        }

        panel.RecalculateContentHeight(panel.Size);

        var firstLabel = (Label)panel.Children[0];
        Assert.True(firstLabel.MeasureContentHeight() > 40f);

        Assert.True(panel.ContentHeight > panel.Size.Y);
        Assert.True(panel.MaxScrollOffset(panel.Size) > 0f);
    }

    [Fact]
    public void ScrollPanel_syncs_label_wrap_width_on_resize()
    {
        var panel = new ScrollPanel
        {
            Size = new Vector2(400f, 200f),
            ShowScrollbar = true,
        };
        var label = new Label
        {
            Position = Vector2.Zero,
            Size = new Vector2(400f, 100f),
            Text = "Wrapped briefing text",
            Padding = 8f,
        };
        panel.AddChild(label);

        panel.RecalculateContentHeight(panel.Size);
        Assert.Equal(UITextDrawing.ContentWrapWidth(400f - 10f, 8f), label.WrapWidth);

        panel.Size = new Vector2(300f, 200f);
        label.Size = new Vector2(300f, 100f);
        panel.RecalculateContentHeight(panel.Size);
        Assert.Equal(UITextDrawing.ContentWrapWidth(300f - 10f, 8f), label.WrapWidth);
    }

    [Fact]
    public void ScrollPanel_recalculates_height_after_label_text_change()
    {
        var panel = new ScrollPanel
        {
            Size = new Vector2(300f, 200f),
            ShowScrollbar = false,
            ContentPadding = 4f,
        };
        string longText = string.Join(" ", Enumerable.Repeat("operations", 40));
        var label = new Label
        {
            Position = Vector2.Zero,
            Size = new Vector2(300f, 48f),
            Text = longText,
            FontSize = 20f,
        };
        panel.AddChild(label);

        panel.RecalculateContentHeight(panel.Size);
        float longContentHeight = panel.ContentHeight;
        float longMaxScroll = panel.MaxScrollOffset(panel.Size);
        float measuredLongBottom = label.Position.Y + label.MeasureContentHeight() + panel.ContentPadding;
        Assert.True(longContentHeight >= measuredLongBottom - 0.01f);
        Assert.True(longMaxScroll > 0f);

        label.Text = "Short";
        panel.RecalculateContentHeight(panel.Size);

        Assert.True(panel.ContentHeight < longContentHeight);
        Assert.True(panel.MaxScrollOffset(panel.Size) < longMaxScroll);
        Assert.True(label.MeasureContentHeight() < 48f);
    }

    [Fact]
    public void ScrollPanel_mission_preview_resets_offset_on_content_shrink()
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

        while (panel.Children.Count > 0)
            panel.RemoveChild(panel.Children[0]);

        panel.RecalculateContentHeight(panel.Size);

        Assert.Equal(0f, panel.ScrollOffsetY);
        Assert.Equal(0f, panel.MaxScrollOffset(panel.Size));
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