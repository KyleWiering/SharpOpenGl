using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class UnitInfoPanelMiningTests
{
    [Fact]
    public void UnitInfoPanel_renders_harvest_mode_and_cargo_bar()
    {
        var panel = new UnitInfoPanel
        {
            Size = new Vector2(320f, 140f),
            SelectedUnits =
            [
                new UnitInfo
                {
                    Name = "Mining Barge",
                    MaxHP = 150f,
                    CurrentHP = 150f,
                    HPFraction = 1f,
                    HarvestMode = "Drones",
                    CargoAmount = 45f,
                    CargoCapacity = 100f,
                    DisplayKind = EntityDisplayKind.Friendly,
                },
            ],
        };

        var renderer = new RecordingUIRenderer();
        panel.Draw(renderer, new Vector2(0f, 0f), new Vector2(320f, 120f));

        Assert.Contains(renderer.Rects, r => r.Color == panel.CargoColor);
        Assert.Contains(renderer.Texts, t => t.Text.Contains("Harvest: Drones"));
        Assert.Contains(renderer.Texts, t => t.Text.Contains("Cargo 45/100"));
    }

    private sealed class RecordingUIRenderer : IUIRenderer
    {
        public List<(Vector2 Position, Vector2 Size, Vector4 Color)> Rects { get; } = new();
        public List<(string Text, Vector2 Position, float FontSize, Vector4 Color)> Texts { get; } = new();

        public Vector2 ViewportSize => new(320f, 120f);

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) =>
            Rects.Add((position, size, color));

        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }

        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color) =>
            Texts.Add((text, position, fontSize, color));
    }
}