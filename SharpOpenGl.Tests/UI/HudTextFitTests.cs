using OpenTK.Mathematics;
using SharpOpenGl.Engine.Build;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class HudTextFitTests
{
    private static readonly Vector2 HudViewport1024 = new(1024f, 768f);
    private static readonly Vector2 HudViewport1920 = UIScaler.ReferenceSize;

    [Fact]
    public void ResourceBar_labels_fit_slot_width_at_1024x768()
    {
        var (inner, renderer) = CreateScaledRenderer(HudViewport1024);
        var bar = CreateResourceBar();

        bar.Draw(renderer, Vector2.Zero, HudViewport1920);

        float slotWidth = renderer.ScaleToPhysical(1920f / 4f - 12f);
        AssertTextDrawsFit(inner.TextDraws, slotWidth);
    }

    [Fact]
    public void ResourceBar_labels_fit_slot_width_at_1920x1080()
    {
        var (inner, renderer) = CreateScaledRenderer(HudViewport1920);
        var bar = CreateResourceBar();

        bar.Draw(renderer, Vector2.Zero, HudViewport1920);

        float slotWidth = 1920f / 4f - 12f;
        AssertTextDrawsFit(inner.TextDraws, slotWidth);
    }

    [Fact]
    public void BuildPanel_hover_returns_tooltip_for_production_button()
    {
        var panel = new BuildPanel
        {
            Size = new Vector2(270f, 500f),
            AvailableItems =
            [
                new BuildableItem
                {
                    Id = "fighter_basic",
                    Name = "Interceptor Mk.I",
                    EnergyCost = 50,
                    MineralsCost = 80,
                    DataCost = 0,
                    CrewCost = 1,
                    BuildTime = 12f,
                },
            ],
        };

        float y = 6f + 12f + 10f;
        var hover = new Vector2(20f, y + 4f);
        panel.UpdatePointerState(hover, false, Vector2.Zero, HudViewport1920);

        TooltipContent? content = panel.GetTooltipContent();

        Assert.NotNull(content);
        Assert.Equal("Interceptor Mk.I", content!.Title);
        Assert.Equal("E:50 M:80 D:0 C:1", content.CostLine);
        Assert.Equal("Build: 12s", content.BuildTime);
    }

    [Fact]
    public void BuildPanel_truncates_long_queue_and_item_labels()
    {
        var inner = new RecordingRenderer(HudViewport1024);
        var panel = new BuildPanel
        {
            Size = new Vector2(270f, 500f),
            BuildingName = "Advanced Shipyard Production Facility",
            Queue =
            [
                new QueuedItem { Name = "Super Heavy Assault Cruiser Mk III", Progress = 0.42f },
            ],
            AvailableItems =
            [
                new BuildableItem
                {
                    Name = "Super Heavy Assault Cruiser Mk III",
                    EnergyCost = 999,
                    MineralsCost = 999,
                    DataCost = 999,
                    CrewCost = 99,
                    BuildTime = 120f,
                },
            ],
        };

        panel.Draw(inner, Vector2.Zero, HudViewport1920);

        Assert.All(inner.TextDraws, draw =>
            Assert.True(UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize) <= 260f));
    }

    [Theory]
    [InlineData(1024f, 768f)]
    [InlineData(1920f, 1080f)]
    public void BuildMapPanel_icon_tiles_have_no_entry_text_and_icons_at_least_48(float viewportWidth, float viewportHeight)
    {
        var viewport = new Vector2(viewportWidth, viewportHeight);
        var inner = new RecordingRenderer(viewport);
        var panel = CreateCrowdedBuildMapPanel();

        panel.Draw(inner, Vector2.Zero, HudViewport1920);

        Assert.DoesNotContain(
            inner.TextDraws,
            draw => draw.Text.Contains("Automated", StringComparison.Ordinal));
        Assert.DoesNotContain(
            inner.TextDraws,
            draw => draw.Text.Contains("Planetary", StringComparison.Ordinal));
        Assert.DoesNotContain(inner.TextDraws, draw => draw.Text.Contains("E:", StringComparison.Ordinal));
        Assert.DoesNotContain(inner.TextDraws, draw => draw.Text.Contains("M:", StringComparison.Ordinal));
        Assert.DoesNotContain(inner.TextDraws, draw => draw.Text.Contains("Footprint", StringComparison.Ordinal));
        Assert.True(inner.MaxIconRectSize.X >= 48f);
        Assert.True(inner.MaxIconRectSize.Y >= 48f);
        Assert.Contains(inner.TextDraws, draw => draw.Text == "Defense");
    }

    [Fact]
    public void ShipControlBar_command_labels_fit_button_inner_width_at_1024x768()
    {
        var (inner, renderer) = CreateScaledRenderer(HudViewport1024);
        var bar = new ShipControlBar { Visible = true };
        bar.UpdateForShip(
            hasWeapons: true,
            hasMovement: true,
            hasResourceCollector: true,
            hasStructureBuilder: true,
            stance: Stance.Aggressive,
            formation: FormationType.Column,
            showFormation: true);

        bar.Draw(renderer, Vector2.Zero, HudViewport1920);

        float buttonInnerWidth = renderer.ScaleToPhysical(ShipControlBar.ButtonWidth - 20f);
        AssertTextDrawsFit(inner.TextDraws, buttonInnerWidth);
    }

    [Fact]
    public void ShipControlBar_command_labels_fit_button_inner_width_at_1920x1080()
    {
        var (inner, renderer) = CreateScaledRenderer(HudViewport1920);
        var bar = new ShipControlBar { Visible = true };
        bar.UpdateForShip(
            hasWeapons: true,
            hasMovement: true,
            hasResourceCollector: true,
            hasStructureBuilder: true,
            stance: Stance.Aggressive,
            formation: FormationType.Column,
            showFormation: true);

        bar.Draw(renderer, Vector2.Zero, HudViewport1920);

        float buttonInnerWidth = ShipControlBar.ButtonWidth - 20f;
        AssertTextDrawsFit(inner.TextDraws, buttonInnerWidth);
    }

    [Theory]
    [InlineData(1024f, 768f)]
    [InlineData(1920f, 1080f)]
    public void GameplayHUD_top_buttons_fit_56x40_at_both_viewports(float viewportWidth, float viewportHeight)
    {
        var viewport = new Vector2(viewportWidth, viewportHeight);
        var (inner, renderer) = CreateScaledRenderer(viewport);
        var hud = new GameplayHUD { Visible = true };

        hud.Draw(renderer);

        float buttonInnerWidth = renderer.ScaleToPhysical(Button.GetInnerTextMaxWidth(new Vector2(56f, 40f)));
        var topButtonDraws = inner.TextDraws
            .Where(draw => draw.Text is "B" or "II")
            .ToList();

        Assert.Equal(2, topButtonDraws.Count);
        AssertTextDrawsFit(topButtonDraws, buttonInnerWidth);
    }

    [Fact]
    public void UnitInfoPanel_long_name_fits_panel_width_at_1920x1080()
    {
        var inner = new RecordingRenderer(HudViewport1920);
        var panel = new UnitInfoPanel
        {
            Size = new Vector2(560f, 170f),
            FontSize = 18f,
            SelectedUnits =
            [
                new UnitInfo
                {
                    Name = "Super Heavy Assault Cruiser Mk III Extended Production Variant",
                    Subtitle = "Terran Battlecruiser — Elite Strike Wing",
                    CurrentHP = 4200,
                    MaxHP = 5000,
                    CurrentShields = 1800,
                    MaxShields = 2000,
                    Armor = 12,
                    HPFraction = 0.84f,
                    ShieldFraction = 0.9f,
                },
            ],
        };

        panel.Draw(inner, Vector2.Zero, HudViewport1920);

        float contentW = 560f - 16f;
        Assert.All(inner.TextDraws, draw =>
            Assert.True(UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize) <= contentW + 1f, draw.Text));
    }

    [Fact]
    public void GameplayHUD_child_widgets_keep_text_inside_bounds_at_1920x1080()
    {
        var (inner, renderer) = CreateScaledRenderer(HudViewport1920);
        var hud = new GameplayHUD
        {
            Visible = true,
        };

        hud.ResourceBar.Resources = CreateResourceBar().Resources;
        hud.BuildMapPanel.Visible = true;
        hud.BuildMapPanel.Categories = CreateCrowdedBuildMapPanel().Categories;
        hud.UnitInfoPanel.SelectedUnits =
        [
            new UnitInfo
            {
                Name = "Super Heavy Assault Cruiser Mk III Extended Production Variant",
                CurrentHP = 900,
                MaxHP = 1000,
                HPFraction = 0.9f,
            },
        ];
        hud.BindShipControlBar(
            hasWeapons: true,
            hasMovement: true,
            hasResourceCollector: true,
            hasStructureBuilder: true,
            stance: Stance.Defensive,
            anySelected: true,
            formation: FormationType.Wedge,
            showFormation: true);

        hud.Draw(renderer);

        foreach (var draw in inner.TextDraws)
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            Assert.True(draw.Position.X >= -1f, draw.Text);
            Assert.True(draw.Position.X + width <= HudViewport1920.X + 1f, draw.Text);
            Assert.True(draw.Position.Y >= -1f, draw.Text);
            Assert.True(draw.Position.Y + draw.FontSize <= HudViewport1920.Y + 1f, draw.Text);
        }
    }

    private static ResourceBar CreateResourceBar() => new()
    {
        Size = new Vector2(1920f, 48f),
        FontSize = 18f,
        Resources =
        [
            new ResourceDisplay { Type = ResourceType.Energy, Current = 450, Max = 1000, IncomePerSecond = 12f },
            new ResourceDisplay { Type = ResourceType.Minerals, Current = 220, Max = 500, IncomePerSecond = 4f },
            new ResourceDisplay { Type = ResourceType.Data, Current = 80, Max = 200, IncomePerSecond = 1.5f },
            new ResourceDisplay { Type = ResourceType.Crew, Current = 24, Max = 50, IncomePerSecond = 0.2f },
        ],
    };

    private static BuildMapPanel CreateCrowdedBuildMapPanel() => new()
    {
        Size = new Vector2(420f, 640f),
        Categories =
        [
            new BuildMapCategoryView
            {
                DisplayName = "Defense",
                Buildings =
                [
                    new BuildMapEntryView
                    {
                        Id = "turret",
                        Name = "Automated Defense Turret Array Complex",
                        FootprintCols = 12,
                        FootprintRows = 12,
                        EnergyCost = 9999,
                        MineralsCost = 9999,
                        DataCost = 9999,
                        CrewCost = 999,
                        IsUnlocked = true,
                        CanAfford = false,
                        Icon = BuildIconCatalog.Get("defense_turret"),
                    },
                    new BuildMapEntryView
                    {
                        Id = "shield",
                        Name = "Planetary Shield Generator Relay Station",
                        FootprintCols = 8,
                        FootprintRows = 8,
                        EnergyCost = 500,
                        MineralsCost = 500,
                        DataCost = 500,
                        CrewCost = 50,
                        IsUnlocked = false,
                        CanAfford = true,
                        Icon = BuildIconCatalog.Get("shield_emitter"),
                    },
                ],
            },
        ],
    };

    private static (RecordingRenderer Inner, ScaledUIRenderer Renderer) CreateScaledRenderer(Vector2 viewport)
    {
        var inner = new RecordingRenderer(viewport);
        var scaler = new UIScaler(viewport);
        return (inner, new ScaledUIRenderer(inner, scaler));
    }

    private static void AssertTextDrawsFit(
        IReadOnlyList<(string Text, float FontSize, Vector2 Position)> draws, float maxWidth)
    {
        foreach (var draw in draws)
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            Assert.True(width <= maxWidth + 1f, draw.Text);
        }
    }

    private sealed class RecordingRenderer : IUIRenderer
    {
        public RecordingRenderer(Vector2 viewport) => ViewportSize = viewport;
        public Vector2 ViewportSize { get; }
        public Vector2 MaxIconRectSize { get; private set; }
        public List<(string Text, float FontSize, Vector2 Position)> TextDraws { get; } = new();

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color)
        {
            if (size.X >= 48f - 0.5f && size.Y >= 48f - 0.5f)
            {
                MaxIconRectSize = new Vector2(
                    MathF.Max(MaxIconRectSize.X, size.X),
                    MathF.Max(MaxIconRectSize.Y, size.Y));
            }
        }

        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }

        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color) =>
            TextDraws.Add((text, fontSize, position));
    }
}