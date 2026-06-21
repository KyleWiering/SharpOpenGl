using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class ButtonScaledTextTests
{
    [Fact]
    public void Scaled_button_label_fits_physical_width_at_1024x768()
    {
        var inner = new RecordingRenderer(new Vector2(1024f, 768f));
        var scaler = new UIScaler(new Vector2(1024f, 768f));
        var renderer = new ScaledUIRenderer(inner, scaler);

        var button = new Button
        {
            Label = "Ship Designer",
            Size = new Vector2(400f, 64f),
            FontSize = 20f,
        };

        button.Draw(renderer, Vector2.Zero, UIScaler.ReferenceSize);

        Assert.NotNull(inner.LastText);
        float physicalWidth = UIFontMetrics.MeasureTextWidth(inner.LastText, inner.LastFontSize);
        float buttonPhysicalWidth = renderer.ScaleToPhysical(400f - 20f);
        Assert.True(physicalWidth <= buttonPhysicalWidth + 0.5f);
    }

    private sealed class RecordingRenderer : IUIRenderer
    {
        public RecordingRenderer(Vector2 viewport) => ViewportSize = viewport;
        public Vector2 ViewportSize { get; }
        public string? LastText { get; private set; }
        public float LastFontSize { get; private set; }

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) { }
        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }

        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color)
        {
            LastText = text;
            LastFontSize = fontSize;
        }
    }
}