using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
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

    [Fact]
    public void ShipControlBar_build_button_label_fits_at_1920x1080()
    {
        var inner = new RecordingRenderer(UIScaler.ReferenceSize);
        var renderer = new ScaledUIRenderer(inner, new UIScaler(UIScaler.ReferenceSize));
        var bar = new ShipControlBar { Visible = true };
        bar.UpdateForShip(
            hasWeapons: false,
            hasMovement: true,
            hasResourceCollector: false,
            hasStructureBuilder: true,
            stance: null,
            formation: null,
            showFormation: false);

        bar.Draw(renderer, Vector2.Zero, UIScaler.ReferenceSize);

        float buttonInnerWidth = ShipControlBar.ButtonWidth - 20f;
        Assert.All(inner.TextDraws, draw =>
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            Assert.True(width <= buttonInnerWidth + 1f, draw.Text);
        });
    }

    [Fact]
    public void Abbreviated_command_button_exposes_tooltip_hint_on_hover()
    {
        var bar = new ShipControlBar { Visible = true };
        bar.UpdateForShip(
            hasWeapons: true,
            hasMovement: true,
            hasResourceCollector: false,
            hasStructureBuilder: false,
            stance: null,
            formation: null,
            showFormation: false);

        var (barPos, barSize) = bar.Resolve(Vector2.Zero, UIScaler.ReferenceSize);
        var patrolButton = Assert.IsType<IconButton>(bar.Children[2]);
        var (btnPos, btnSize) = patrolButton.Resolve(barPos, barSize);
        var center = btnPos + btnSize * 0.5f;

        patrolButton.UpdatePointerState(center, false, barPos, barSize);

        TooltipContent? tooltip = patrolButton.GetTooltipContent();
        Assert.NotNull(tooltip);
        Assert.Equal("Patrol (P)", tooltip!.Title);
    }

    private sealed class RecordingRenderer : IUIRenderer
    {
        public RecordingRenderer(Vector2 viewport) => ViewportSize = viewport;
        public Vector2 ViewportSize { get; }
        public string? LastText { get; private set; }
        public float LastFontSize { get; private set; }
        public List<(string Text, float FontSize)> TextDraws { get; } = new();

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) { }
        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }

        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color)
        {
            LastText = text;
            LastFontSize = fontSize;
            TextDraws.Add((text, fontSize));
        }
    }
}