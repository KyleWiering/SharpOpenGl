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

    [Fact]
    public void ObjectivePanel_wraps_long_objectives_at_1024x768()
    {
        var (inner, renderer) = CreateScaledRenderer(new Vector2(1024f, 768f));
        var panel = new ObjectivePanel
        {
            Size = new Vector2(520f, 200f),
            MissionTitle = "Training: Interceptor Duel",
            ProgressText = "0/3 complete",
            Objectives =
            [
                new ObjectiveLine
                {
                    Text = "Destroy the hostile interceptor squadron before they destroy your command ship and escort frigates in contested open space",
                    IsCompleted = false,
                },
            ],
        };

        panel.Draw(renderer, Vector2.Zero, panel.Size);

        float innerW = renderer.ScaleToPhysical(520f - 24f);
        var objectiveDraws = inner.TextDraws
            .Where(draw =>
                !draw.Text.StartsWith("Training", StringComparison.Ordinal)
                && !draw.Text.Contains("OBJECTIVES", StringComparison.Ordinal)
                && !draw.Text.Contains("LClick", StringComparison.Ordinal))
            .ToList();

        Assert.True(objectiveDraws.Count >= 2, "Long objective text should wrap to multiple lines at compact viewport.");
        Assert.True(objectiveDraws[1].Position.Y > objectiveDraws[0].Position.Y, "Wrapped objective lines should stack vertically.");
        Assert.All(objectiveDraws, draw =>
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            Assert.True(width <= innerW + 2f, draw.Text);
        });
    }

    [Fact]
    public void ObjectivePanel_scrolls_when_objectives_overflow_panel_height()
    {
        var panel = new ObjectivePanel
        {
            Size = new Vector2(720f, ObjectivePanel.MaxPanelHeight),
            MissionTitle = "Campaign: Outer Rim Siege",
            ProgressText = "0/6 complete",
            Objectives =
            [
                new ObjectiveLine { Text = "Establish orbital command relay near the primary asteroid belt", IsCompleted = false },
                new ObjectiveLine { Text = "Construct automated defense turret array along the northern approach", IsCompleted = false },
                new ObjectiveLine { Text = "Destroy enemy super-heavy dreadnought production facilities", IsCompleted = false },
                new ObjectiveLine { Text = "Escort mining barges through the contested debris field corridor", IsCompleted = false },
                new ObjectiveLine { Text = "Capture the abandoned research station before reinforcements arrive", IsCompleted = false },
                new ObjectiveLine { Text = "Survive the final interceptor wave without losing command ship", IsCompleted = false },
            ],
            HintText = "Select ships (LClick) · Move (RClick) · Attack enemies (RClick)",
        };

        var inner = new RecordingRenderer();
        panel.Draw(inner, Vector2.Zero, panel.Size);

        Assert.True(panel.MaxObjectiveScrollOffset > 0f, "Overflowing objectives should expose scroll range.");

        float beforeOffset = panel.ObjectiveScrollOffsetY;
        var (pos, size) = panel.Resolve(Vector2.Zero, panel.Size);
        bool consumed = panel.HandleScroll(pos + new Vector2(size.X * 0.5f, size.Y * 0.5f), 1f, Vector2.Zero, panel.Size);

        Assert.True(consumed);
        Assert.True(panel.ObjectiveScrollOffsetY > beforeOffset);
    }

    [Fact]
    public void EstimateContentHeight_caps_at_max_panel_height()
    {
        var objectives = Enumerable.Range(1, 8)
            .Select(i => new ObjectiveLine
            {
                Text = $"Complete extended objective task number {i} across multiple star systems",
                IsCompleted = false,
            })
            .ToList();

        float height = ObjectivePanel.EstimateContentHeight(
            "Extended Campaign Operation", objectives, "hint", 22f, 18f, 15f, 720f);

        Assert.Equal(ObjectivePanel.MaxPanelHeight, height, 0.5f);
    }

    private static (RecordingRenderer Inner, ScaledUIRenderer Renderer) CreateScaledRenderer(Vector2 viewport)
    {
        var inner = new RecordingRenderer(viewport);
        var scaler = new UIScaler(viewport);
        return (inner, new ScaledUIRenderer(inner, scaler));
    }

    private sealed class RecordingRenderer : IUIRenderer
    {
        public RecordingRenderer(Vector2? viewport = null) =>
            ViewportSize = viewport ?? UIScaler.ReferenceSize;

        public Vector2 ViewportSize { get; }
        public List<(string Text, float FontSize, Vector2 Position)> TextDraws { get; } = new();

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) { }
        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }

        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color)
            => TextDraws.Add((text, fontSize, position));
    }
}