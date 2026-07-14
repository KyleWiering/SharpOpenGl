using OpenTK.Mathematics;
using SharpOpenGl.Engine.Missions;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class ObjectivePanelTests
{
    [Fact]
    public void Long_objective_text_wraps_within_panel_width()
    {
        var panel = new ObjectivePanel
        {
            Size = new Vector2(720f, 200f),
            MissionTitle = "Training: Interceptor Duel",
            ProgressText = "0/1 complete",
            Objectives =
            [
                new ObjectiveLine
                {
                    Text = "Destroy the hostile interceptor before it destroys your command ship in open space",
                    IsCompleted = false,
                },
            ],
        };

        var inner = new RecordingRenderer();
        panel.Draw(inner, Vector2.Zero, panel.Size);

        float innerW = panel.Size.X - 24f;
        Assert.All(inner.TextDraws, draw =>
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            Assert.True(width <= innerW + 1f, draw.Text);
        });
        Assert.Contains(inner.TextDraws, d => d.Text.Contains("OBJECTIVES", StringComparison.Ordinal));
        Assert.Contains(inner.TextDraws, d => d.Text.Contains("0/1 complete", StringComparison.Ordinal));
    }

    [Fact]
    public void MissionHudBinder_shows_first_mission_onboarding_for_training_01()
    {
        var definition = new MissionDefinition
        {
            Id = MissionHudBinder.FirstTrainingMissionId,
            DisplayName = "Training: Interceptor Duel",
            Objectives = new ObjectivesDefinition
            {
                Primary =
                [
                    new ObjectiveDefinition
                    {
                        Id = "destroy_enemy",
                        Type = "destroy_target",
                        Description = "Destroy the hostile interceptor",
                    },
                ],
            },
        };

        var hud = new GameplayHUD();
        MissionHudBinder.BindObjectivePanel(hud, new MissionState(definition));

        Assert.True(hud.ObjectivePanel.Visible);
        Assert.True(hud.ShowFirstMissionOnboardingHint);
        Assert.Contains("attack", hud.ObjectivePanel.HintText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EstimateContentHeight_grows_with_wrapped_objectives()
    {
        var shortLine = new ObjectiveLine { Text = "Destroy scout", IsCompleted = false };
        var longLine = new ObjectiveLine
        {
            Text = "Destroy all enemy super-heavy dreadnought production facilities in the outer rim sector",
            IsCompleted = false,
        };

        float shortHeight = ObjectivePanel.EstimateContentHeight(
            "Mission", [shortLine], "hint", 22f, 18f, 15f, 720f);
        float longHeight = ObjectivePanel.EstimateContentHeight(
            "Mission", [longLine], "hint", 22f, 18f, 15f, 720f);

        Assert.True(longHeight > shortHeight);
    }

    private sealed class RecordingRenderer : IUIRenderer
    {
        public Vector2 ViewportSize { get; } = UIScaler.ReferenceSize;
        public List<(string Text, float FontSize, Vector2 Position)> TextDraws { get; } = new();

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) { }
        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }

        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color)
            => TextDraws.Add((text, fontSize, position));
    }
}