using OpenTK.Mathematics;
using SharpOpenGl.Engine.Build;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class HudTextFitTests
{
    private static readonly Vector2 HudViewport1024 = new(1024f, 768f);
    private static readonly Vector2 HudViewport1920 = UIScaler.ReferenceSize;
    private static readonly Vector2 HudViewportBrowser = new(390f, 844f);
    private static readonly Vector2 HudViewportUltrawide = new(2560f, 1440f);

    private static float BuildPanelContentWidth =>
        BuildPanel.ComputeContentWidth(GameplayHudLayout.BuildPanelWidth);

    private const float BuildPanelIconColumnWidth = 28f;
    private const float BuildPanelIconGlyphSize = 24f;
    private const float BuildPanelLabelLeftPad = 4f;
    private const float BuildPanelLabelRightPad = 4f;
    private const float BuildPanelQueueBadgeWidth = 22f;
    private const float BuildPanelCancelButtonWidth = 18f;

    private static float BuildPanelProductionLabelMaxWidth =>
        BuildPanelContentWidth
        - BuildPanelIconColumnWidth
        - BuildPanelLabelLeftPad
        - BuildPanelLabelRightPad;

    private static float BuildPanelQueueWaitingLabelMaxWidth =>
        BuildPanelContentWidth
        - BuildPanelQueueBadgeWidth
        - BuildPanelCancelButtonWidth
        - 6f;

    [Fact]
    public void ResourceBar_hover_returns_tooltip_with_full_resource_name()
    {
        var bar = CreateResourceBar();
        bar.UpdatePointerState(new Vector2(120f, 20f), false, Vector2.Zero, HudViewport1920);

        TooltipContent? content = bar.GetTooltipContent();

        Assert.NotNull(content);
        Assert.Equal("Energy (E)", content!.Title);
        Assert.Equal("450 / 1000", content.CostLine);
        Assert.Equal("Plasma Cores", content.RoleLine);
        Assert.Contains("Income:", content.BuildTime ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void ResourceBar_draws_abbreviation_badges_for_four_resources()
    {
        var inner = new RecordingRenderer(HudViewport1920);
        var bar = CreateResourceBar();

        bar.Draw(inner, Vector2.Zero, HudViewport1920);

        Assert.Equal(4, inner.TextDraws.Count(draw =>
            draw.Text is "E" or "M" or "D" or "C"));
        Assert.Contains(inner.TextDraws, draw => draw.Text.Contains("450/1000", StringComparison.Ordinal));
        Assert.Contains(inner.TextDraws, draw => draw.Text.Contains("+12/s", StringComparison.Ordinal));
    }

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
    public void ResourceBar_stress_labels_fit_slot_width_at_1024x768()
    {
        var (inner, renderer) = CreateScaledRenderer(HudViewport1024);
        var bar = new ResourceBar
        {
            Size = new Vector2(1920f, GameplayHudLayout.ResourceBarHeight),
            FontSize = GameplayHudLayout.ResourceBarFontSize,
            Resources =
            [
                new ResourceDisplay { Type = ResourceType.Energy, Current = 99999, Max = 100000, IncomePerSecond = 9999f },
                new ResourceDisplay { Type = ResourceType.Minerals, Current = 88888, Max = 90000, IncomePerSecond = -2500.5f },
                new ResourceDisplay { Type = ResourceType.Data, Current = 77777, Max = 80000, IncomePerSecond = 1234.5f },
                new ResourceDisplay { Type = ResourceType.Crew, Current = 66666, Max = 70000, IncomePerSecond = 0.05f },
            ],
        };

        bar.Draw(renderer, Vector2.Zero, HudViewport1920);

        float slotWidth = renderer.ScaleToPhysical(1920f / 4f - 12f);
        var labelDraws = inner.TextDraws
            .Where(draw => draw.Text.Contains('/', StringComparison.Ordinal))
            .ToList();

        Assert.Equal(4, labelDraws.Count);
        AssertTextDrawsFit(labelDraws, slotWidth);
        AssertTextDrawsWithinHorizontalBounds(inner.TextDraws, 0f, HudViewport1024.X, HudViewport1024.X);
    }

    [Fact]
    public void ResourceBar_stress_labels_fit_slot_width_at_2560x1440()
    {
        var (inner, renderer) = CreateScaledRenderer(HudViewportUltrawide);
        var bar = new ResourceBar
        {
            Size = new Vector2(1920f, GameplayHudLayout.ResourceBarHeight),
            FontSize = GameplayHudLayout.ResourceBarFontSize,
            Resources =
            [
                new ResourceDisplay { Type = ResourceType.Energy, Current = 99999, Max = 100000, IncomePerSecond = 9999f },
                new ResourceDisplay { Type = ResourceType.Minerals, Current = 88888, Max = 90000, IncomePerSecond = -2500.5f },
                new ResourceDisplay { Type = ResourceType.Data, Current = 77777, Max = 80000, IncomePerSecond = 1234.5f },
                new ResourceDisplay { Type = ResourceType.Crew, Current = 66666, Max = 70000, IncomePerSecond = 0.05f },
            ],
        };

        bar.Draw(renderer, Vector2.Zero, HudViewport1920);

        float slotWidth = renderer.ScaleToPhysical(1920f / 4f - 12f);
        var labelDraws = inner.TextDraws
            .Where(draw => draw.Text.Contains('/', StringComparison.Ordinal))
            .ToList();

        Assert.Equal(4, labelDraws.Count);
        AssertTextDrawsFit(labelDraws, slotWidth);
        AssertNoHorizontalViewportBleed(inner.TextDraws, HudViewportUltrawide);
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
            Size = new Vector2(GameplayHudLayout.BuildPanelWidth, GameplayHudLayout.BuildPanelHeight),
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

        float y = BuildPanelFirstButtonTop();
        var hover = new Vector2(20f, y + 4f);
        panel.UpdatePointerState(hover, false, Vector2.Zero, HudViewport1920);

        TooltipContent? content = panel.GetTooltipContent();

        Assert.NotNull(content);
        Assert.Equal("Interceptor Mk.I", content!.Title);
        Assert.Equal("E:50 M:80 D:0 C:1", content.CostLine);
        Assert.Equal("Build: 12s", content.BuildTime);
    }

    [Fact]
    public void BuildPanel_production_row_has_icon_and_concise_name()
    {
        var inner = new RecordingRenderer(HudViewport1024);
        var panel = new BuildPanel
        {
            Size = new Vector2(GameplayHudLayout.BuildPanelWidth, GameplayHudLayout.BuildPanelHeight),
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
                    Role = ShipRole.Military,
                },
            ],
        };

        panel.Draw(inner, Vector2.Zero, HudViewport1920);

        Assert.Contains(inner.TextDraws, draw => draw.Text == "Interceptor Mk.I");
        Assert.Contains(inner.TextDraws, draw => draw.Text.Contains("E50", StringComparison.Ordinal));
        Assert.DoesNotContain(inner.TextDraws, draw =>
            draw.Text.Contains("Interceptor", StringComparison.Ordinal)
            && draw.Text.Contains("E:", StringComparison.Ordinal));
        Assert.True(inner.ProductionIconColumnRectCount >= 3,
            "Production row should emit hull/role glyph rects in the reserved icon column.");
    }

    [Fact]
    public void BuildPanel_long_production_name_wraps_to_two_lines_at_1920()
    {
        var inner = new RecordingRenderer(HudViewport1920);
        var panel = new BuildPanel
        {
            Size = new Vector2(GameplayHudLayout.BuildPanelWidth, GameplayHudLayout.BuildPanelHeight),
            AvailableItems =
            [
                new BuildableItem
                {
                    Name = "Super Heavy Assault Cruiser Mk III Extended Range",
                    EnergyCost = 999,
                    MineralsCost = 999,
                    DataCost = 999,
                    CrewCost = 99,
                    BuildTime = 120f,
                    Role = ShipRole.Military,
                },
            ],
        };

        panel.Draw(inner, Vector2.Zero, HudViewport1920);

        float productionLabelX = 8f + BuildPanelIconColumnWidth + BuildPanelLabelLeftPad;
        var nameDraws = inner.ProductionLabelDraws
            .Where(draw => !IsAbbreviatedProductionCostLabel(draw.Text))
            .ToList();

        Assert.True(nameDraws.Count >= 2, "Long production names should wrap to two lines at reference resolution.");
        Assert.All(nameDraws, draw =>
            Assert.True(
                UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize) <= BuildPanelProductionLabelMaxWidth + 1f,
                draw.Text));
        Assert.Equal(productionLabelX, nameDraws[0].Position.X, precision: 1);
        Assert.True(nameDraws[1].Position.Y > nameDraws[0].Position.Y, "Second name line should sit below the first.");
    }

    [Fact]
    public void BuildPanel_queue_waiting_rows_fit_scaled_viewport_at_2560x1440()
    {
        var (inner, renderer) = CreateScaledRenderer(HudViewportUltrawide);
        var panel = new BuildPanel
        {
            Size = new Vector2(GameplayHudLayout.BuildPanelWidth, GameplayHudLayout.BuildPanelHeight),
            BuildingName = "Orbital Shipyard",
            Queue =
            [
                new QueuedItem
                {
                    Name = "Interceptor Mk.I",
                    Progress = 0.25f,
                    IsCurrent = true,
                    QueueIndex = 1,
                    State = QueuedState.Building,
                    RemainingSeconds = 9f,
                    TotalBuildTime = 12f,
                },
                new QueuedItem
                {
                    Name = "Orbital Defense Platform Extended Range Variant",
                    Progress = 0f,
                    IsCurrent = false,
                    QueueIndex = 2,
                    State = QueuedState.Queued,
                },
                new QueuedItem
                {
                    Name = "Deep Space Mining Barge Heavy Industrial Model",
                    Progress = 0f,
                    IsCurrent = false,
                    QueueIndex = 3,
                    State = QueuedState.Queued,
                },
            ],
            AvailableItems = [],
        };

        panel.Draw(renderer, Vector2.Zero, HudViewport1920);

        var (panelPos, panelSize) = panel.Resolve(Vector2.Zero, HudViewport1920);
        float panelLeft = renderer.ScaleToPhysical(panelPos.X + BuildPanel.HorizontalPadding);
        float panelRight = renderer.ScaleToPhysical(panelPos.X + panelSize.X - BuildPanel.HorizontalPadding);
        float queueLabelX = renderer.ScaleToPhysical(panelPos.X + BuildPanel.HorizontalPadding + BuildPanelQueueBadgeWidth + 4f);
        float queueMaxPhysical = renderer.ScaleToPhysical(BuildPanelQueueWaitingLabelMaxWidth);

        var queueNameDraws = inner.TextDraws
            .Where(draw =>
                draw.Position.X >= queueLabelX - 1f
                && !draw.Text.StartsWith("#", StringComparison.Ordinal)
                && draw.Text != "X"
                && !draw.Text.Contains("Building", StringComparison.Ordinal)
                && !draw.Text.Contains("Production Queue", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(queueNameDraws);
        Assert.All(queueNameDraws, draw =>
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            Assert.True(width <= queueMaxPhysical + 2f, draw.Text);
        });
        AssertTextDrawsWithinHorizontalBounds(inner.TextDraws, panelLeft, panelRight, HudViewportUltrawide.X);
        AssertNoHorizontalViewportBleed(inner.TextDraws, HudViewportUltrawide);
    }

    [Fact]
    public void BuildPanel_queue_waiting_rows_fit_scaled_viewport_at_1024x768()
    {
        var (inner, renderer) = CreateScaledRenderer(HudViewport1024);
        var panel = new BuildPanel
        {
            Size = new Vector2(GameplayHudLayout.BuildPanelWidth, GameplayHudLayout.BuildPanelHeight),
            BuildingName = "Orbital Shipyard",
            Queue =
            [
                new QueuedItem
                {
                    Name = "Interceptor Mk.I",
                    Progress = 0.25f,
                    IsCurrent = true,
                    QueueIndex = 1,
                    State = QueuedState.Building,
                    RemainingSeconds = 9f,
                    TotalBuildTime = 12f,
                },
                new QueuedItem
                {
                    Name = "Orbital Defense Platform Extended Range Variant",
                    Progress = 0f,
                    IsCurrent = false,
                    QueueIndex = 2,
                    State = QueuedState.Queued,
                },
                new QueuedItem
                {
                    Name = "Deep Space Mining Barge Heavy Industrial Model",
                    Progress = 0f,
                    IsCurrent = false,
                    QueueIndex = 3,
                    State = QueuedState.Queued,
                },
            ],
            AvailableItems = [],
        };

        panel.Draw(renderer, Vector2.Zero, HudViewport1920);

        var (panelPos, panelSize) = panel.Resolve(Vector2.Zero, HudViewport1920);
        float panelLeft = renderer.ScaleToPhysical(panelPos.X + BuildPanel.HorizontalPadding);
        float panelRight = renderer.ScaleToPhysical(panelPos.X + panelSize.X - BuildPanel.HorizontalPadding);
        float queueLabelX = renderer.ScaleToPhysical(panelPos.X + BuildPanel.HorizontalPadding + BuildPanelQueueBadgeWidth + 4f);
        float queueMaxPhysical = renderer.ScaleToPhysical(BuildPanelQueueWaitingLabelMaxWidth);

        var queueNameDraws = inner.TextDraws
            .Where(draw =>
                draw.Position.X >= queueLabelX - 1f
                && !draw.Text.StartsWith("#", StringComparison.Ordinal)
                && draw.Text != "X"
                && !draw.Text.Contains("Building", StringComparison.Ordinal)
                && !draw.Text.Contains("Production Queue", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(queueNameDraws);
        Assert.All(queueNameDraws, draw =>
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            Assert.True(width <= queueMaxPhysical + 2f, draw.Text);
        });
        AssertTextDrawsWithinHorizontalBounds(inner.TextDraws, panelLeft, panelRight, HudViewport1024.X);
    }

    [Fact]
    public void BuildPanel_role_icon_column_reserved()
    {
        var inner = new RecordingRenderer(HudViewport1024);
        var panel = new BuildPanel
        {
            Size = new Vector2(GameplayHudLayout.BuildPanelWidth, GameplayHudLayout.BuildPanelHeight),
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
                    Role = ShipRole.Military,
                },
            ],
        };

        panel.Draw(inner, Vector2.Zero, HudViewport1920);

        Assert.All(inner.ProductionLabelDraws, draw =>
            Assert.True(
                UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize) <= BuildPanelProductionLabelMaxWidth + 1f,
                draw.Text));
        Assert.True(inner.ProductionIconColumnRectCount >= 3,
            "Role hull glyph should reserve the left icon column.");
    }

    [Fact]
    public void BuildPanel_truncates_long_queue_and_item_labels()
    {
        var inner = new RecordingRenderer(HudViewport1024);
        var panel = new BuildPanel
        {
            Size = new Vector2(GameplayHudLayout.BuildPanelWidth, GameplayHudLayout.BuildPanelHeight),
            BuildingName = "Advanced Shipyard Production Facility",
            Queue =
            [
                new QueuedItem
                {
                    Name = "Super Heavy Assault Cruiser Mk III",
                    Progress = 0.42f,
                    IsCurrent = true,
                    QueueIndex = 1,
                    State = QueuedState.Building,
                },
                new QueuedItem
                {
                    Name = "Orbital Defense Platform Extended Range Variant",
                    Progress = 0f,
                    IsCurrent = false,
                    QueueIndex = 2,
                    State = QueuedState.Queued,
                },
                new QueuedItem
                {
                    Name = "Deep Space Mining Barge Heavy Industrial Model",
                    Progress = 0f,
                    IsCurrent = false,
                    QueueIndex = 3,
                    State = QueuedState.Queued,
                },
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

        Assert.Contains(inner.TextDraws, draw => draw.Text.Contains("Production Queue (1/3)", StringComparison.Ordinal));
        Assert.Contains(inner.TextDraws, draw => draw.Text == "#2");
        Assert.Contains(inner.TextDraws, draw => draw.Text == "#3");
        Assert.DoesNotContain(inner.TextDraws, draw => draw.Text.Contains("42%", StringComparison.Ordinal)
            && draw.Text.Contains("Orbital", StringComparison.Ordinal));
        AssertProductionAndQueueLabelsFit(inner.TextDraws);
    }

    [Fact]
    public void BuildPanel_compact_queue_collapses_deep_waiting_rows()
    {
        var inner = new RecordingRenderer(HudViewport1024);
        var panel = new BuildPanel
        {
            Size = new Vector2(GameplayHudLayout.BuildPanelWidth, GameplayHudLayout.BuildPanelHeight),
            BuildingName = "Orbital Shipyard",
            Queue =
            [
                new QueuedItem
                {
                    Name = "Interceptor Mk.I",
                    Progress = 0.25f,
                    IsCurrent = true,
                    QueueIndex = 1,
                    State = QueuedState.Building,
                },
                new QueuedItem
                {
                    Name = "Strike Fighter Squadron Alpha",
                    Progress = 0f,
                    IsCurrent = false,
                    QueueIndex = 2,
                    State = QueuedState.Queued,
                },
                new QueuedItem
                {
                    Name = "Heavy Bomber Wing Extended Range",
                    Progress = 0f,
                    IsCurrent = false,
                    QueueIndex = 3,
                    State = QueuedState.Queued,
                },
                new QueuedItem
                {
                    Name = "Orbital Defense Platform Array",
                    Progress = 0f,
                    IsCurrent = false,
                    QueueIndex = 4,
                    State = QueuedState.Queued,
                },
                new QueuedItem
                {
                    Name = "Deep Space Mining Barge Industrial",
                    Progress = 0f,
                    IsCurrent = false,
                    QueueIndex = 5,
                    State = QueuedState.Queued,
                },
            ],
            AvailableItems =
            [
                new BuildableItem
                {
                    Name = "Interceptor Mk.I",
                    EnergyCost = 50,
                    MineralsCost = 80,
                    BuildTime = 12f,
                },
            ],
        };

        panel.Draw(inner, Vector2.Zero, HudViewport1920);

        Assert.Contains(inner.TextDraws, draw => draw.Text.Contains("Production Queue (1/5)", StringComparison.Ordinal));
        Assert.Contains(inner.TextDraws, draw => draw.Text == "#2");
        Assert.Contains(inner.TextDraws, draw =>
            draw.Text.Contains("+3 more", StringComparison.Ordinal));
        Assert.DoesNotContain(inner.TextDraws, draw => draw.Text == "#3");
        Assert.DoesNotContain(inner.TextDraws, draw => draw.Text == "#4");
        Assert.DoesNotContain(inner.TextDraws, draw => draw.Text == "#5");
        AssertProductionAndQueueLabelsFit(inner.TextDraws);
    }

    [Fact]
    public void BuildPanel_short_queue_keeps_numbered_rows()
    {
        var inner = new RecordingRenderer(HudViewport1024);
        var panel = new BuildPanel
        {
            Size = new Vector2(GameplayHudLayout.BuildPanelWidth, GameplayHudLayout.BuildPanelHeight),
            BuildingName = "Orbital Shipyard",
            Queue =
            [
                new QueuedItem
                {
                    Name = "Interceptor Mk.I",
                    Progress = 0.5f,
                    IsCurrent = true,
                    QueueIndex = 1,
                    State = QueuedState.Building,
                },
                new QueuedItem
                {
                    Name = "Strike Fighter Squadron Alpha",
                    Progress = 0f,
                    IsCurrent = false,
                    QueueIndex = 2,
                    State = QueuedState.Queued,
                },
            ],
            AvailableItems =
            [
                new BuildableItem
                {
                    Name = "Interceptor Mk.I",
                    EnergyCost = 50,
                    MineralsCost = 80,
                    BuildTime = 12f,
                },
            ],
        };

        panel.Draw(inner, Vector2.Zero, HudViewport1920);

        Assert.Contains(inner.TextDraws, draw => draw.Text == "#2");
        Assert.DoesNotContain(inner.TextDraws, draw =>
            draw.Text.Contains("more in queue", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildPanel_empty_queue_shows_idle_production_header()
    {
        var inner = new RecordingRenderer(HudViewport1024);
        var panel = new BuildPanel
        {
            Size = new Vector2(GameplayHudLayout.BuildPanelWidth, GameplayHudLayout.BuildPanelHeight),
            BuildingName = "Small Shipyard",
            Queue = [],
            AvailableItems =
            [
                new BuildableItem
                {
                    Name = "Interceptor Mk.I",
                    EnergyCost = 50,
                    MineralsCost = 80,
                    BuildTime = 12f,
                },
            ],
        };

        panel.Draw(inner, Vector2.Zero, HudViewport1920);

        Assert.Contains(inner.TextDraws, draw =>
            draw.Text.Contains("Production Queue: Idle", StringComparison.Ordinal));
        Assert.Contains(inner.TextDraws, draw =>
            draw.Text.Contains("Click a unit below", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildPanel_current_queue_row_shows_progress_status_and_cancel_glyph()
    {
        var inner = new RecordingRenderer(HudViewport1024);
        var panel = new BuildPanel
        {
            Size = new Vector2(GameplayHudLayout.BuildPanelWidth, GameplayHudLayout.BuildPanelHeight),
            BuildingName = "Orbital Shipyard",
            Queue =
            [
                new QueuedItem
                {
                    Name = "Interceptor Mk.I",
                    Progress = 0.42f,
                    IsCurrent = true,
                    QueueIndex = 1,
                    State = QueuedState.Building,
                    RemainingSeconds = 7f,
                    TotalBuildTime = 12f,
                },
            ],
            AvailableItems = [],
        };

        panel.Draw(inner, Vector2.Zero, HudViewport1920);

        Assert.Contains(inner.TextDraws, draw =>
            draw.Text.Contains("Building · 42%", StringComparison.Ordinal)
            && draw.Text.Contains("7s left", StringComparison.Ordinal));
        Assert.Contains(inner.TextDraws, draw => draw.Text == "X");
    }

    [Fact]
    public void BuildPanel_queue_cancel_invokes_event_with_queue_index()
    {
        var panel = new BuildPanel
        {
            Size = new Vector2(GameplayHudLayout.BuildPanelWidth, GameplayHudLayout.BuildPanelHeight),
            Queue =
            [
                new QueuedItem
                {
                    Name = "Interceptor Mk.I",
                    Progress = 0.25f,
                    IsCurrent = true,
                    QueueIndex = 1,
                    State = QueuedState.Building,
                    RemainingSeconds = 9f,
                    TotalBuildTime = 12f,
                },
                new QueuedItem
                {
                    Name = "Strike Fighter",
                    Progress = 0f,
                    IsCurrent = false,
                    QueueIndex = 2,
                    State = QueuedState.Queued,
                },
            ],
            AvailableItems = [],
        };

        int cancelledIndex = 0;
        panel.QueueCancelRequested += index => cancelledIndex = index;

        float queueTop = 6f + 12f + 10f + 12f + 2f;
        float contentW = BuildPanelContentWidth;
        var waitingCancel = new Vector2(8f + contentW - 18f + 4f, queueTop + 26f + 1f);
        Assert.True(panel.HandlePointerTapped(waitingCancel, 0, Vector2.Zero, HudViewport1920));
        Assert.Equal(2, cancelledIndex);
    }

    [Fact]
    public void BuildPanel_queue_cancel_tooltip_shows_refund_hint()
    {
        var panel = new BuildPanel
        {
            Size = new Vector2(GameplayHudLayout.BuildPanelWidth, GameplayHudLayout.BuildPanelHeight),
            Queue =
            [
                new QueuedItem
                {
                    Name = "Interceptor Mk.I",
                    Progress = 0.5f,
                    IsCurrent = true,
                    QueueIndex = 1,
                    State = QueuedState.Building,
                    RemainingSeconds = 6f,
                    TotalBuildTime = 12f,
                },
            ],
            AvailableItems = [],
        };

        float queueTop = 6f + 12f + 10f + 12f + 2f;
        panel.UpdatePointerState(new Vector2(8f + BuildPanelContentWidth - 9f, queueTop + 8f), false, Vector2.Zero, HudViewport1920);

        TooltipContent? content = panel.GetTooltipContent();

        Assert.NotNull(content);
        Assert.Equal("Cancel current build", content!.Title);
        Assert.Equal("Full resource refund", content.CostLine);
        Assert.Equal("Progress is lost", content.BuildTime);
    }

    [Fact]
    public void BuildPanel_queued_button_tooltip_shows_in_production()
    {
        var panel = new BuildPanel
        {
            Size = new Vector2(GameplayHudLayout.BuildPanelWidth, GameplayHudLayout.BuildPanelHeight),
            AvailableItems =
            [
                new BuildableItem
                {
                    Id = "fighter_basic",
                    Name = "Interceptor Mk.I",
                    EnergyCost = 50,
                    MineralsCost = 80,
                    BuildTime = 12f,
                    CanAfford = true,
                    IsQueued = true,
                },
            ],
        };

        float y = BuildPanelFirstButtonTop();
        panel.UpdatePointerState(new Vector2(20f, y + 4f), false, Vector2.Zero, HudViewport1920);

        TooltipContent? content = panel.GetTooltipContent();

        Assert.NotNull(content);
        Assert.Equal("Interceptor Mk.I (Queued)", content!.Title);
        Assert.Equal("In production", content.AffordReason);
    }

    [Theory]
    [InlineData(1024f, 768f)]
    [InlineData(1920f, 1080f)]
    public void BuildMapPanel_category_headers_fit_panel_inner_width(float viewportWidth, float viewportHeight)
    {
        var viewport = new Vector2(viewportWidth, viewportHeight);
        var (inner, renderer) = CreateScaledRenderer(viewport);
        var panel = CreateLongHeaderBuildMapPanel();
        var layoutHud = new GameplayHUD { Visible = true };
        layoutHud.BuildMapPanel.Visible = true;
        GameplayHudLayout.ApplyDensityLayout(layoutHud);
        panel.Size = layoutHud.BuildMapPanel.Size;

        panel.Draw(renderer, Vector2.Zero, HudViewport1920);

        var (panelPos, panelSize) = panel.Resolve(Vector2.Zero, HudViewport1920);
        float innerW = renderer.ScaleToPhysical(BuildMapPanel.ComputeInnerTextWidth(panelSize.X));
        float panelLeft = renderer.ScaleToPhysical(panelPos.X + 10f);
        float panelRight = renderer.ScaleToPhysical(panelPos.X + panelSize.X - 10f);

        var headerDraws = inner.TextDraws
            .Where(draw =>
                draw.Text.Contains("Tier", StringComparison.Ordinal)
                || draw.Text.Contains("Build Structures", StringComparison.Ordinal)
                || draw.Text.Contains("prerequisites", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.NotEmpty(headerDraws);
        foreach (var draw in headerDraws)
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            Assert.True(width <= innerW + 2f, draw.Text);
            Assert.True(draw.Position.X >= panelLeft - 2f, draw.Text);
            Assert.True(draw.Position.X + width <= panelRight + 2f, draw.Text);
        }
    }

    [Fact]
    public void GameplayHUD_build_map_tooltip_stays_inside_viewport_at_1024x768()
    {
        var viewport = HudViewport1024;
        var inner = new RecordingRenderer(viewport);
        var scaler = new UIScaler(viewport);
        var mgr = new UIManager(new EventBus());
        var hud = CreateBuildMapTooltipHud();
        GameplayHudLayout.ApplyDensityLayout(hud);
        mgr.Push(hud);

        var (panelPos, panelSize) = hud.BuildMapPanel.Resolve(Vector2.Zero, HudViewport1920);
        var physicalHover = scaler.ScalePosition(panelPos + new Vector2(panelSize.X - 24f, panelSize.Y * 0.5f));
        mgr.HandlePointerMove(physicalHover, false, viewport);
        mgr.Update(TooltipWidget.HoverShowDelaySeconds + 0.05f);

        mgr.Draw(new ScaledUIRenderer(inner, scaler));

        var tooltipDraws = inner.TextDraws
            .Where(draw => draw.Text.Contains("Orbital", StringComparison.Ordinal)
                || draw.Text.Contains("Cost:", StringComparison.Ordinal)
                || draw.Text.Contains("Build:", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(tooltipDraws);
        AssertTextDrawsWithinHorizontalBounds(inner.TextDraws, 0f, viewport.X, viewport.X);
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
        Assert.Contains(inner.TextDraws, draw =>
            draw.Text.Contains("Defense", StringComparison.Ordinal)
            && draw.Text.Contains("Tier", StringComparison.Ordinal));
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
    public void GameplayHUD_top_buttons_fit_112x44_at_both_viewports(float viewportWidth, float viewportHeight)
    {
        var viewport = new Vector2(viewportWidth, viewportHeight);
        var (inner, renderer) = CreateScaledRenderer(viewport);
        var hud = new GameplayHUD { Visible = true };

        hud.Draw(renderer);

        float buttonInnerWidth = renderer.ScaleToPhysical(Button.GetInnerTextMaxWidth(new Vector2(112f, 44f)));
        var topButtonDraws = inner.TextDraws
            .Where(draw => draw.Text is "Build" or "Pause")
            .ToList();

        Assert.Equal(2, topButtonDraws.Count);
        AssertTextDrawsFit(topButtonDraws, buttonInnerWidth);
    }

    [Fact]
    public void UnitInfoPanel_long_name_fits_panel_width_at_1920x1080()
    {
        var inner = new RecordingRenderer(HudViewport1920);
        var panel = CreateStressUnitInfoPanel(
            GameplayHudLayout.UnitInfoStandardWidth,
            GameplayHudLayout.UnitInfoStandardHeight,
            17f);

        panel.Draw(inner, Vector2.Zero, HudViewport1920);

        float contentW = UnitInfoPanel.ComputeTextColumnWidth(
            GameplayHudLayout.UnitInfoStandardWidth, 8f, UnitInfoPanel.HeaderIconSize);
        AssertUnitInfoTextDrawsFit(inner.TextDraws, contentW);
    }

    [Fact]
    public void UnitInfoPanel_viewport_matrix_long_name_fits_standard_panel_at_2560x1440()
    {
        var viewport = HudViewportUltrawide;
        var (inner, renderer) = CreateScaledRenderer(viewport);
        var panel = CreateStressUnitInfoPanel(
            GameplayHudLayout.UnitInfoStandardWidth,
            GameplayHudLayout.UnitInfoStandardHeight,
            17f);

        panel.Draw(renderer, Vector2.Zero, HudViewport1920);

        float contentW = renderer.ScaleToPhysical(UnitInfoPanel.ComputeTextColumnWidth(
            GameplayHudLayout.UnitInfoStandardWidth, 8f, UnitInfoPanel.HeaderIconSize));
        AssertUnitInfoTextDrawsFit(inner.TextDraws, contentW);
        AssertNoHorizontalViewportBleed(inner.TextDraws, viewport);
    }

    [Theory]
    [InlineData(1024f, 768f)]
    [InlineData(390f, 844f)]
    public void UnitInfoPanel_viewport_matrix_long_name_fits_compact_panel(float viewportWidth, float viewportHeight)
    {
        var viewport = new Vector2(viewportWidth, viewportHeight);
        var (inner, renderer) = CreateScaledRenderer(viewport);
        var panel = CreateStressUnitInfoPanel(
            GameplayHudLayout.UnitInfoCompactWidth,
            GameplayHudLayout.UnitInfoCompactHeight,
            16f);

        panel.Draw(renderer, Vector2.Zero, HudViewport1920);

        float contentW = renderer.ScaleToPhysical(UnitInfoPanel.ComputeTextColumnWidth(
            GameplayHudLayout.UnitInfoCompactWidth, 6f, UnitInfoPanel.CompactHeaderIconSize));
        AssertUnitInfoTextDrawsFit(inner.TextDraws, contentW);
        AssertNoHorizontalViewportBleed(inner.TextDraws, viewport);
    }

    [Theory]
    [InlineData(GameplayHudLayout.UnitInfoStandardWidth, GameplayHudLayout.UnitInfoStandardHeight, 17f, 8f, UnitInfoPanel.HeaderIconSize)]
    [InlineData(GameplayHudLayout.UnitInfoCompactWidth, GameplayHudLayout.UnitInfoCompactHeight, 16f, 6f, UnitInfoPanel.CompactHeaderIconSize)]
    public void UnitInfoPanel_harvest_cargo_strings_fit_panel_bounds(
        float panelWidth, float panelHeight, float fontSize, float padding, float headerIconSize)
    {
        var inner = new RecordingRenderer(HudViewport1920);
        var panel = new UnitInfoPanel
        {
            Size = new Vector2(panelWidth, panelHeight),
            FontSize = fontSize,
            SelectedUnits =
            [
                new UnitInfo
                {
                    Name = "Deep Space Mining Barge Heavy Industrial Extraction Platform",
                    Subtitle = "Engineering hauler — extended-range mineral collection",
                    MaxHP = 1500f,
                    CurrentHP = 1500f,
                    HPFraction = 1f,
                    HarvestMode = "Automated Tractor Beam Array Extended Range",
                    CargoAmount = 240f,
                    CargoCapacity = 300f,
                },
            ],
        };

        panel.Draw(inner, Vector2.Zero, HudViewport1920);

        float contentW = UnitInfoPanel.ComputeTextColumnWidth(panelWidth, padding, headerIconSize);
        AssertUnitInfoTextDrawsFit(inner.TextDraws, contentW);
        Assert.Contains(inner.TextDraws, draw =>
            draw.Text.Contains("240/300", StringComparison.Ordinal)
            || draw.Text.Contains("Cargo", StringComparison.Ordinal));
    }

    [Fact]
    public void GameplayHUD_unit_info_and_ship_control_fit_viewport_at_1024x768()
    {
        var (inner, renderer) = CreateScaledRenderer(HudViewport1024);
        var hud = new GameplayHUD
        {
            Visible = true,
            ShowBuildFlowHint = false,
            ShowFirstMissionOnboardingHint = false,
        };

        GameplayHudLayout.ApplyDensityLayout(hud);
        hud.BuildMapPanel.Visible = true;
        hud.UnitInfoPanel.SelectedUnits = CreateStressUnitInfoPanel(
            hud.UnitInfoPanel.Size.X,
            hud.UnitInfoPanel.Size.Y,
            hud.UnitInfoPanel.FontSize).SelectedUnits;
        hud.BindShipControlBar(
            hasWeapons: true,
            hasMovement: true,
            hasResourceCollector: true,
            hasStructureBuilder: true,
            stance: Stance.Aggressive,
            anySelected: true,
            formation: FormationType.Column,
            showFormation: true);

        hud.Draw(renderer);

        float unitContentW = renderer.ScaleToPhysical(UnitInfoPanel.ComputeTextColumnWidth(
            hud.UnitInfoPanel.Size.X,
            UnitInfoPanel.IsCompactLayout(hud.UnitInfoPanel.Size) ? 6f : 8f,
            UnitInfoPanel.IsCompactLayout(hud.UnitInfoPanel.Size)
                ? UnitInfoPanel.CompactHeaderIconSize
                : UnitInfoPanel.HeaderIconSize));
        var (unitPos, unitSize) = hud.UnitInfoPanel.Resolve(Vector2.Zero, HudViewport1920);
        float unitLeft = renderer.ScaleToPhysical(unitPos.X + (UnitInfoPanel.IsCompactLayout(hud.UnitInfoPanel.Size) ? 6f : 8f));
        float unitRight = renderer.ScaleToPhysical(unitPos.X + unitSize.X - (UnitInfoPanel.IsCompactLayout(hud.UnitInfoPanel.Size) ? 6f : 8f));

        var unitDraws = inner.TextDraws
            .Where(draw =>
                draw.Position.X >= unitLeft - 2f
                && draw.Position.X < unitRight + 2f)
            .ToList();
        Assert.NotEmpty(unitDraws);
        AssertUnitInfoTextDrawsFit(unitDraws, unitContentW);
        AssertTextDrawsWithinHorizontalBounds(inner.TextDraws, 0f, HudViewport1024.X, HudViewport1024.X);

        float buttonInnerWidth = renderer.ScaleToPhysical(ShipControlBar.ButtonWidth - 20f);
        var shipBarDraws = inner.TextDraws
            .Where(draw => draw.Text is "Move" or "Stop" or "Patrol" or "Attack" or "Attack Move"
                or "Aggressive" or "Column" or "Build" or "Harvest" or "Formations")
            .ToList();
        Assert.NotEmpty(shipBarDraws);
        AssertTextDrawsFit(shipBarDraws, buttonInnerWidth);
    }

    [Fact]
    public void GameplayHUD_child_widgets_keep_text_inside_bounds_at_1920x1080()
    {
        var (inner, renderer) = CreateScaledRenderer(HudViewport1920);
        var hud = new GameplayHUD
        {
            Visible = true,
            ShowBuildFlowHint = false,
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

    private static float BuildPanelFirstButtonTop()
    {
        const float fontSize = 12f;
        const float queueRowHeight = 14f;
        return 6f + fontSize + 10f + fontSize + 2f + queueRowHeight + 4f;
    }

    private static ResourceBar CreateResourceBar() => new()
    {
        Size = new Vector2(1920f, GameplayHudLayout.ResourceBarHeight),
        FontSize = GameplayHudLayout.ResourceBarFontSize,
        Resources =
        [
            new ResourceDisplay { Type = ResourceType.Energy, Current = 450, Max = 1000, IncomePerSecond = 12f },
            new ResourceDisplay { Type = ResourceType.Minerals, Current = 220, Max = 500, IncomePerSecond = 4f },
            new ResourceDisplay { Type = ResourceType.Data, Current = 80, Max = 200, IncomePerSecond = 1.5f },
            new ResourceDisplay { Type = ResourceType.Crew, Current = 24, Max = 50, IncomePerSecond = 0.2f },
        ],
    };

    private static GameplayHUD CreateBuildMapTooltipHud()
    {
        var hud = new GameplayHUD { Visible = true };
        hud.BuildMapPanel.Visible = true;
        hud.BuildMapPanel.Categories = CreateLongHeaderBuildMapPanel().Categories;
        return hud;
    }

    private static BuildMapPanel CreateLongHeaderBuildMapPanel() => new()
    {
        Size = new Vector2(GameplayHudLayout.BuildMapPanelWidth, GameplayHudLayout.BuildMapPanelHeight),
        Visible = true,
        Categories =
        [
            new BuildMapCategoryView
            {
                DisplayName = "Superheavy Orbital Defense Infrastructure",
                TierIndex = 3,
                Buildings =
                [
                    new BuildMapEntryView
                    {
                        Id = "orbital_uplink",
                        Name = "Orbital Defense Relay Station Extended Range",
                        FootprintCols = 4,
                        FootprintRows = 4,
                        EnergyCost = 900,
                        MineralsCost = 1200,
                        BuildTime = 90f,
                        IsUnlocked = false,
                        CanAfford = true,
                        Prerequisites = ["Command Center", "Sensor Array", "Power Reactor"],
                        LockReason = "Requires: Sensor Array",
                        Icon = BuildIconCatalog.Get("orbital_uplink"),
                    },
                ],
            },
            new BuildMapCategoryView
            {
                DisplayName = "Capstone",
                TierIndex = 5,
                Buildings =
                [
                    new BuildMapEntryView
                    {
                        Id = "fortress_core",
                        Name = "Fortress Core",
                        CategoryId = "capstone",
                        IsUnlocked = false,
                        PrerequisiteMetCount = 0,
                        PrerequisiteTotalCount = 4,
                        Icon = BuildIconCatalog.Get("command_center"),
                    },
                ],
            },
        ],
    };

    private static BuildMapPanel CreateCrowdedBuildMapPanel() => new()
    {
        Size = new Vector2(GameplayHudLayout.BuildMapPanelWidth, GameplayHudLayout.BuildMapPanelHeight),
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

    private static void AssertProductionAndQueueLabelsFit(
        IReadOnlyList<(string Text, float FontSize, Vector2 Position)> draws)
    {
        float productionLabelX = 8f + BuildPanelIconColumnWidth + BuildPanelLabelLeftPad;
        float queueLabelX = 8f + BuildPanelQueueBadgeWidth + 4f;
        foreach (var draw in draws)
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            float maxWidth = draw.Position.X >= productionLabelX - 0.5f
                ? BuildPanelProductionLabelMaxWidth
                : draw.Position.X >= queueLabelX - 0.5f && draw.Text != "X" && !draw.Text.StartsWith('#')
                    ? BuildPanelQueueWaitingLabelMaxWidth
                    : BuildPanelContentWidth;
            Assert.True(width <= maxWidth + 1f, draw.Text);
        }
    }

    [Theory]
    [InlineData(1024f, 768f)]
    [InlineData(390f, 844f)]
    [InlineData(2560f, 1440f)]
    public void BuildPanel_viewport_matrix_text_stays_within_panel_bounds(float viewportWidth, float viewportHeight)
    {
        var viewport = new Vector2(viewportWidth, viewportHeight);
        var (inner, renderer) = CreateScaledRenderer(viewport);
        var panel = CreateCrowdedBuildPanel();

        panel.Draw(renderer, Vector2.Zero, HudViewport1920);

        var (panelPos, panelSize) = panel.Resolve(Vector2.Zero, HudViewport1920);
        float panelLeft = renderer.ScaleToPhysical(panelPos.X + BuildPanel.HorizontalPadding);
        float panelRight = renderer.ScaleToPhysical(panelPos.X + panelSize.X - BuildPanel.HorizontalPadding);

        AssertTextDrawsWithinHorizontalBounds(inner.TextDraws, panelLeft, panelRight, viewport.X);
        AssertNoHorizontalViewportBleed(inner.TextDraws, viewport);
    }

    [Theory]
    [InlineData(1024f, 768f)]
    [InlineData(390f, 844f)]
    [InlineData(2560f, 1440f)]
    public void GameplayHUD_viewport_matrix_build_panel_and_unit_info_avoid_horizontal_bleed(
        float viewportWidth, float viewportHeight)
    {
        var viewport = new Vector2(viewportWidth, viewportHeight);
        var (inner, renderer) = CreateScaledRenderer(viewport);
        var hud = new GameplayHUD
        {
            Visible = true,
            ShowBuildFlowHint = false,
            ShowFirstMissionOnboardingHint = false,
        };

        GameplayHudLayout.ApplyDensityLayout(hud);
        hud.BuildPanel.Visible = true;
        hud.BuildPanel.BuildingName = "Advanced Shipyard Production Facility";
        hud.BuildPanel.SupplyText = "18 / 24";
        hud.BuildPanel.Queue =
        [
            new QueuedItem
            {
                Name = "Super Heavy Assault Cruiser Mk III Extended Range",
                Progress = 0.35f,
                IsCurrent = true,
                QueueIndex = 1,
                State = QueuedState.Building,
                RemainingSeconds = 42f,
                TotalBuildTime = 120f,
            },
            new QueuedItem
            {
                Name = "Orbital Defense Platform Array Variant",
                Progress = 0f,
                IsCurrent = false,
                QueueIndex = 2,
                State = QueuedState.Queued,
            },
        ];
        hud.BuildPanel.AvailableItems =
        [
            new BuildableItem
            {
                Name = "Super Heavy Assault Cruiser Mk III",
                EnergyCost = 999,
                MineralsCost = 999,
                DataCost = 999,
                CrewCost = 99,
                BuildTime = 120f,
                Role = ShipRole.Military,
            },
        ];
        hud.BuildMapPanel.Visible = true;
        hud.UnitInfoPanel.SelectedUnits = CreateStressUnitInfoPanel(
            hud.UnitInfoPanel.Size.X,
            hud.UnitInfoPanel.Size.Y,
            hud.UnitInfoPanel.FontSize).SelectedUnits;
        hud.BindShipControlBar(
            hasWeapons: true,
            hasMovement: true,
            hasResourceCollector: true,
            hasStructureBuilder: true,
            stance: Stance.Aggressive,
            anySelected: true,
            formation: FormationType.Column,
            showFormation: true);

        hud.Draw(renderer);

        AssertNoHorizontalViewportBleed(inner.TextDraws, viewport);

        var (panelPos, panelSize) = hud.BuildPanel.Resolve(Vector2.Zero, HudViewport1920);
        float panelLeft = renderer.ScaleToPhysical(panelPos.X + BuildPanel.HorizontalPadding);
        float panelRight = renderer.ScaleToPhysical(panelPos.X + panelSize.X - BuildPanel.HorizontalPadding);
        var buildPanelDraws = inner.TextDraws
            .Where(draw =>
                draw.Position.X >= panelLeft - 2f
                && draw.Position.X < panelRight + 2f)
            .ToList();
        Assert.NotEmpty(buildPanelDraws);
        AssertTextDrawsWithinHorizontalBounds(buildPanelDraws, panelLeft, panelRight, viewport.X);
    }

    [Fact]
    public void GameplayHUD_placement_hint_band_stays_within_viewport_at_1024x768()
    {
        var (inner, renderer) = CreateScaledRenderer(HudViewport1024);
        var hud = new GameplayHUD
        {
            Visible = true,
            ShowBuildFlowHint = false,
            ShowFirstMissionOnboardingHint = false,
            PlacementHint = "Orbital Defense Platform Extended Range Variant — click valid terrain to place structure",
            PlacementHintIsValid = true,
        };

        hud.Draw(renderer);

        var placementDraws = inner.TextDraws
            .Where(draw => draw.Text.Contains("Orbital Defense", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(placementDraws);
        AssertTextDrawsWithinHorizontalBounds(placementDraws, 0f, HudViewport1024.X, HudViewport1024.X);
        Assert.All(placementDraws, draw =>
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            Assert.True(width <= renderer.ScaleToPhysical(1920f - 280f) + 2f, draw.Text);
        });
    }

    [Fact]
    public void GameplayHUD_build_panel_text_stays_within_viewport_at_1024x768()
    {
        var (inner, renderer) = CreateScaledRenderer(HudViewport1024);
        var hud = new GameplayHUD
        {
            Visible = true,
            ShowBuildFlowHint = false,
            ShowFirstMissionOnboardingHint = false,
        };

        hud.BuildPanel.Visible = true;
        hud.BuildPanel.BuildingName = "Advanced Shipyard Production Facility";
        hud.BuildPanel.SupplyText = "18 / 24";
        hud.BuildPanel.Queue =
        [
            new QueuedItem
            {
                Name = "Super Heavy Assault Cruiser Mk III Extended Range",
                Progress = 0.35f,
                IsCurrent = true,
                QueueIndex = 1,
                State = QueuedState.Building,
                RemainingSeconds = 42f,
                TotalBuildTime = 120f,
            },
            new QueuedItem
            {
                Name = "Orbital Defense Platform Array Variant",
                Progress = 0f,
                IsCurrent = false,
                QueueIndex = 2,
                State = QueuedState.Queued,
            },
        ];
        hud.BuildPanel.AvailableItems =
        [
            new BuildableItem
            {
                Name = "Super Heavy Assault Cruiser Mk III",
                EnergyCost = 999,
                MineralsCost = 999,
                DataCost = 999,
                CrewCost = 99,
                BuildTime = 120f,
                Role = ShipRole.Military,
            },
            new BuildableItem
            {
                Name = "Deep Space Mining Barge Heavy Industrial Model",
                EnergyCost = 450,
                MineralsCost = 600,
                BuildTime = 90f,
                Role = ShipRole.Engineering,
            },
        ];

        hud.Draw(renderer);

        foreach (var draw in inner.TextDraws)
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            Assert.True(draw.Position.X + width <= HudViewport1024.X + 1f, draw.Text);
        }

        var (panelPos, panelSize) = hud.BuildPanel.Resolve(Vector2.Zero, HudViewport1920);
        float panelLeft = renderer.ScaleToPhysical(panelPos.X + BuildPanel.HorizontalPadding);
        float panelRight = renderer.ScaleToPhysical(panelPos.X + panelSize.X - BuildPanel.HorizontalPadding);
        float panelTop = renderer.ScaleToPhysical(panelPos.Y);
        float panelBottom = renderer.ScaleToPhysical(panelPos.Y + panelSize.Y);

        var buildPanelDraws = inner.TextDraws
            .Where(draw =>
                draw.Position.X >= panelLeft - 2f
                && draw.Position.X < panelRight + 2f
                && draw.Position.Y >= panelTop - 2f
                && draw.Position.Y < panelBottom + 2f)
            .ToList();

        Assert.NotEmpty(buildPanelDraws);
        AssertTextDrawsWithinHorizontalBounds(buildPanelDraws, panelLeft, panelRight, HudViewport1024.X);
    }

    private static bool IsAbbreviatedProductionCostLabel(string text) =>
        text.StartsWith("E", StringComparison.Ordinal)
        && text.Length > 1
        && char.IsDigit(text[1]);

    private static UnitInfoPanel CreateStressUnitInfoPanel(float width, float height, float fontSize) => new()
    {
        Size = new Vector2(width, height),
        FontSize = fontSize,
        SelectedUnits =
        [
            new UnitInfo
            {
                Name = "Super Heavy Assault Cruiser Mk III Extended Production Variant",
                Subtitle = "Terran Battlecruiser — Elite Strike Wing — Long-Range Artillery Support Platform",
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

    private static void AssertUnitInfoTextDrawsFit(
        IReadOnlyList<(string Text, float FontSize, Vector2 Position)> draws, float maxWidth)
    {
        foreach (var draw in draws)
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            Assert.True(width <= maxWidth + 1f, draw.Text);
        }
    }

    private static BuildPanel CreateCrowdedBuildPanel() => new()
    {
        Size = new Vector2(GameplayHudLayout.BuildPanelWidth, GameplayHudLayout.BuildPanelHeight),
        BuildingName = "Advanced Shipyard Production Facility",
        SupplyText = "18 / 24",
        Queue =
        [
            new QueuedItem
            {
                Name = "Super Heavy Assault Cruiser Mk III Extended Production Variant",
                Progress = 0.42f,
                IsCurrent = true,
                QueueIndex = 1,
                State = QueuedState.Building,
                RemainingSeconds = 7f,
                TotalBuildTime = 12f,
            },
            new QueuedItem
            {
                Name = "Orbital Defense Platform Extended Range Variant",
                Progress = 0f,
                IsCurrent = false,
                QueueIndex = 2,
                State = QueuedState.Queued,
            },
            new QueuedItem
            {
                Name = "Deep Space Mining Barge Heavy Industrial Model",
                Progress = 0f,
                IsCurrent = false,
                QueueIndex = 3,
                State = QueuedState.Queued,
            },
        ],
        AvailableItems =
        [
            new BuildableItem
            {
                Name = "Super Heavy Assault Cruiser Mk III Extended Production Variant",
                EnergyCost = 999,
                MineralsCost = 999,
                DataCost = 999,
                CrewCost = 99,
                BuildTime = 120f,
                Role = ShipRole.Military,
            },
            new BuildableItem
            {
                Name = "Deep Space Mining Barge Heavy Industrial Model",
                EnergyCost = 450,
                MineralsCost = 600,
                BuildTime = 90f,
                Role = ShipRole.Engineering,
            },
        ],
    };

    private static void AssertTextDrawsWithinHorizontalBounds(
        IReadOnlyList<(string Text, float FontSize, Vector2 Position)> draws,
        float minX, float maxX, float viewportWidth)
    {
        float clampRight = MathF.Min(maxX, viewportWidth);
        foreach (var draw in draws)
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            Assert.True(draw.Position.X >= minX - 1f, draw.Text);
            Assert.True(draw.Position.X + width <= clampRight + 2f,
                $"Text '{draw.Text}' right edge {draw.Position.X + width:0.##} exceeds {clampRight:0.##}");
        }
    }

    private static void AssertNoHorizontalViewportBleed(
        IReadOnlyList<(string Text, float FontSize, Vector2 Position)> draws,
        Vector2 viewport)
    {
        foreach (var draw in draws)
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            Assert.True(draw.Position.X >= -1f, draw.Text);
            Assert.True(draw.Position.X + width <= viewport.X + 1f, draw.Text);
        }
    }

    private sealed class RecordingRenderer : IUIRenderer
    {
        public RecordingRenderer(Vector2 viewport) => ViewportSize = viewport;
        public Vector2 ViewportSize { get; }
        public Vector2 MaxIconRectSize { get; private set; }
        public int ProductionIconColumnRectCount { get; private set; }
        public List<(string Text, float FontSize, Vector2 Position)> TextDraws { get; } = new();

        public IEnumerable<(string Text, float FontSize, Vector2 Position)> ProductionLabelDraws
        {
            get
            {
                float productionLabelX = 8f + BuildPanelIconColumnWidth + BuildPanelLabelLeftPad;
                return TextDraws.Where(draw => draw.Position.X >= productionLabelX - 0.5f);
            }
        }

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color)
        {
            if (size.X >= 48f - 0.5f && size.Y >= 48f - 0.5f)
            {
                MaxIconRectSize = new Vector2(
                    MathF.Max(MaxIconRectSize.X, size.X),
                    MathF.Max(MaxIconRectSize.Y, size.Y));
            }

            if (position.X >= 8f - 0.5f
                && position.X <= 8f + BuildPanelIconColumnWidth + 0.5f
                && size.X <= BuildPanelIconGlyphSize
                && size.Y <= BuildPanelIconGlyphSize)
            {
                ProductionIconColumnRectCount++;
            }
        }

        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }

        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color) =>
            TextDraws.Add((text, fontSize, position));
    }
}