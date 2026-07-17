using OpenTK.Mathematics;
using SharpOpenGl.Engine.Build;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class BuildMapPanelTests
{
    private static readonly Vector2 ReferenceViewport = UIScaler.ReferenceSize;
    private const float TileSize = 64f;
    private const float IconSize = 52f;
    private const float PanelPadding = 10f;
    private const float TitleHeight = 28f;
    private const float CategoryHeaderHeight = 20f;
    private const float HeaderFontSize = 12f;

    /// <summary>Local offset to the centre of the first 64×64 entry tile.</summary>
    private static Vector2 FirstEntryTileCenterOffset()
    {
        var panelSize = new Vector2(GameplayHudLayout.BuildMapPanelWidth, GameplayHudLayout.BuildMapPanelHeight);
        float contentTop = BuildMapPanel.GetHeaderBlockHeight(BuildMapPanel.IsCompactHeader(panelSize));
        float rowY = contentTop + CategoryHeaderHeight;
        return new Vector2(PanelPadding + TileSize * 0.5f, rowY + TileSize * 0.5f);
    }

    [Fact]
    public void Selecting_ready_building_fires_BuildingSelected()
    {
        var panel = CreatePanelWithEntry(unlocked: true, afford: true);
        string? selected = null;
        panel.BuildingSelected += id => selected = id;

        var (pos, _) = panel.Resolve(Vector2.Zero, ReferenceViewport);
        var tap = pos + FirstEntryTileCenterOffset();
        bool consumed = panel.HandlePointerTapped(tap, 0, Vector2.Zero, ReferenceViewport);

        Assert.True(consumed);
        Assert.Equal("defense_turret", selected);
    }

    [Fact]
    public void Locked_building_click_does_not_fire_selection()
    {
        var panel = CreatePanelWithEntry(unlocked: false, afford: true);
        string? selected = null;
        panel.BuildingSelected += id => selected = id;

        var (pos, _) = panel.Resolve(Vector2.Zero, ReferenceViewport);
        var tap = pos + FirstEntryTileCenterOffset();
        panel.HandlePointerTapped(tap, 0, Vector2.Zero, ReferenceViewport);

        Assert.Null(selected);
    }

    [Fact]
    public void GameplayHUD_routes_build_map_clicks_before_world()
    {
        var hud = new GameplayHUD();
        hud.BuildMapPanel.Visible = true;
        hud.BuildMapPanel.Categories = CreatePanelWithEntry(true, true).Categories;

        string? selected = null;
        hud.BuildMapPanel.BuildingSelected += id => selected = id;

        var (pos, _) = hud.BuildMapPanel.Resolve(Vector2.Zero, ReferenceViewport);
        var tap = pos + FirstEntryTileCenterOffset();
        bool consumed = hud.HandlePointerTapped(tap, 0, ReferenceViewport);

        Assert.True(consumed);
        Assert.Equal("defense_turret", selected);
    }

    [Fact]
    public void Build_map_button_fires_BuildMapRequested()
    {
        var hud = new GameplayHUD();
        bool opened = false;
        hud.BuildMapRequested += () => opened = true;

        // Build button: TopRight (-120, 8), size 112×44 → centre ≈ (1744, 30).
        var tap = new Vector2(1744f, 30f);
        bool consumed = hud.HandlePointerTapped(tap, 0, ReferenceViewport);

        Assert.True(consumed);
        Assert.True(opened);
    }

    [Fact]
    public void Draw_emits_icon_rects_for_unlocked_entry()
    {
        var panel = CreatePanelWithEntry(unlocked: true, afford: true);
        var renderer = new RecordingRenderer();

        panel.Draw(renderer, Vector2.Zero, ReferenceViewport);

        int buttonChromeRects = 2;
        int iconBaselineRects = 2;
        Assert.True(renderer.RectDrawCount > buttonChromeRects + iconBaselineRects,
            "Unlocked entry should emit BuildIconDrawing rects beyond button chrome.");
        Assert.True(renderer.MaxIconRectSize.X >= IconSize);
        Assert.True(renderer.MaxIconRectSize.Y >= IconSize);
    }

    [Fact]
    public void Draw_entry_tiles_emit_no_name_cost_or_footprint_text()
    {
        var panel = CreatePanelWithEntry(unlocked: true, afford: false);
        var renderer = new RecordingRenderer();

        panel.Draw(renderer, Vector2.Zero, ReferenceViewport);

        Assert.DoesNotContain(renderer.TextDraws, draw => draw.Text.Contains("Defense Turret", StringComparison.Ordinal));
        Assert.DoesNotContain(renderer.TextDraws, draw => draw.Text.Contains("E:", StringComparison.Ordinal));
        Assert.DoesNotContain(renderer.TextDraws, draw => draw.Text.Contains("M:", StringComparison.Ordinal));
        Assert.DoesNotContain(renderer.TextDraws, draw => draw.Text.Contains("Footprint", StringComparison.Ordinal));
        Assert.Contains(renderer.TextDraws, draw => draw.Text == "Build Structures");
        Assert.Contains(renderer.TextDraws, draw =>
            draw.Text.Contains("structure icon", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(renderer.TextDraws, draw =>
            draw.Text.Contains("Defense", StringComparison.Ordinal)
            && draw.Text.Contains("Tier", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(1024f, 768f)]
    [InlineData(1920f, 1080f)]
    public void Category_header_long_display_name_fits_panel_inner_width(float viewportWidth, float viewportHeight)
    {
        var viewport = new Vector2(viewportWidth, viewportHeight);
        var inner = new ScaledRecordingRenderer(viewport);
        var scaler = new UIScaler(viewport);
        var renderer = new ScaledUIRenderer(inner, scaler);

        var panel = new BuildMapPanel
        {
            Anchor = Anchor.MiddleLeft,
            Position = new Vector2(GameplayHudLayout.BuildMapPanelPositionX, -232f),
            Size = new Vector2(GameplayHudLayout.BuildMapPanelWidth, GameplayHudLayout.BuildMapPanelHeight),
            Visible = true,
            Categories =
            [
                new BuildMapCategoryView
                {
                    Id = "defense",
                    DisplayName = "Superheavy Orbital Defense Infrastructure",
                    TierIndex = 3,
                    Buildings =
                    [
                        new BuildMapEntryView
                        {
                            Id = "orbital_uplink",
                            Name = "Orbital Relay",
                            IsUnlocked = false,
                            CanAfford = true,
                            Icon = BuildIconCatalog.Get("orbital_uplink"),
                        },
                    ],
                },
            ],
        };

        panel.Draw(renderer, Vector2.Zero, ReferenceViewport);

        string expectedHeader = BuildMapPanel.FormatCategoryHeader(
            "Superheavy Orbital Defense Infrastructure", 3, 0, 1);
        float innerW = renderer.ScaleToPhysical(BuildMapPanel.ComputeInnerTextWidth(panel.Size.X));

        foreach (var draw in inner.TextDraws)
        {
            float width = UIFontMetrics.MeasureTextWidth(draw.Text, draw.FontSize);
            Assert.True(width <= innerW + 2f, draw.Text);
        }

        Assert.Contains(inner.TextDraws, draw =>
            draw.Text.Contains("Super", StringComparison.Ordinal)
            || draw.Text.Contains("Tier", StringComparison.Ordinal));
        Assert.NotEqual(
            expectedHeader,
            inner.TextDraws.First(draw => draw.Text.Contains("Super", StringComparison.Ordinal)).Text);
    }

    [Fact]
    public void Category_header_includes_tier_and_unlock_counts()
    {
        var panel = new BuildMapPanel
        {
            Anchor = Anchor.MiddleLeft,
            Position = new Vector2(GameplayHudLayout.BuildMapPanelPositionX, -232f),
            Size = new Vector2(GameplayHudLayout.BuildMapPanelWidth, GameplayHudLayout.BuildMapPanelHeight),
            Visible = true,
            Categories =
            [
                new BuildMapCategoryView
                {
                    Id = "economy",
                    DisplayName = "Economy",
                    TierIndex = 2,
                    UnlockedCount = 1,
                    TotalCount = 4,
                    Buildings =
                    [
                        new BuildMapEntryView
                        {
                            Id = "power_reactor",
                            Name = "Power Reactor",
                            IsUnlocked = true,
                            CanAfford = true,
                            Icon = BuildIconCatalog.Get("power_reactor"),
                        },
                    ],
                },
            ],
        };

        var renderer = new RecordingRenderer();
        panel.Draw(renderer, Vector2.Zero, ReferenceViewport);

        string expectedHeader = BuildMapPanel.FormatCategoryHeader("Economy", 2, 1, 1);
        Assert.Contains(renderer.TextDraws, draw => draw.Text == expectedHeader);
        Assert.Contains(renderer.TextDraws, draw =>
            draw.Text.Contains("Tier 2", StringComparison.Ordinal)
            && draw.Text.Contains("(1/1)", StringComparison.Ordinal));
    }

    [Fact]
    public void Locked_tile_tooltip_includes_prerequisite_chain()
    {
        var panel = new BuildMapPanel
        {
            Anchor = Anchor.MiddleLeft,
            Position = new Vector2(GameplayHudLayout.BuildMapPanelPositionX, -232f),
            Size = new Vector2(GameplayHudLayout.BuildMapPanelWidth, GameplayHudLayout.BuildMapPanelHeight),
            Visible = true,
            Categories =
            [
                new BuildMapCategoryView
                {
                    Id = "production",
                    DisplayName = "Production",
                    TierIndex = 1,
                    Buildings =
                    [
                        new BuildMapEntryView
                        {
                            Id = "shipyard_small",
                            Name = "Small Shipyard",
                            IsUnlocked = false,
                            CanAfford = true,
                            Prerequisites = ["Command Center", "Power Reactor"],
                            LockReason = "Requires: Power Reactor",
                            Icon = BuildIconCatalog.Get("shipyard_small"),
                        },
                    ],
                },
            ],
        };

        var (pos, _) = panel.Resolve(Vector2.Zero, ReferenceViewport);
        panel.UpdatePointerState(pos + FirstEntryTileCenterOffset(), false, Vector2.Zero, ReferenceViewport);

        TooltipContent? content = panel.GetTooltipContent();

        Assert.NotNull(content);
        IReadOnlyList<string> lines = content!.ToLines();
        Assert.Contains("Prerequisite chain: Command Center → Power Reactor", lines);
        Assert.DoesNotContain(lines, line => line.StartsWith('•'));
    }

    [Fact]
    public void Locked_tile_tooltip_long_prerequisite_chain_scrollable()
    {
        string[] prerequisites =
        [
            "Command Center",
            "Power Reactor",
            "Research Laboratory",
            "Advanced Fabrication Hub",
            "Orbital Communications Relay",
            "Defense Grid Emitter",
        ];

        var entry = new BuildMapEntryView
        {
            Id = "fortress_core",
            Name = "Fortress Core",
            CategoryId = "capstone",
            CategoryName = "Capstone",
            IsUnlocked = false,
            CanAfford = true,
            PrerequisiteMetCount = 1,
            PrerequisiteTotalCount = 6,
            Prerequisites = prerequisites,
            LockReason = "Requires: Defense Grid Emitter",
            Icon = BuildIconCatalog.Get("fortress_core"),
        };

        var panel = new BuildMapPanel
        {
            Anchor = Anchor.MiddleLeft,
            Position = new Vector2(GameplayHudLayout.BuildMapPanelPositionX, -232f),
            Size = new Vector2(GameplayHudLayout.BuildMapPanelWidth, GameplayHudLayout.BuildMapPanelHeight),
            Visible = true,
            Categories =
            [
                new BuildMapCategoryView
                {
                    Id = "capstone",
                    DisplayName = "Capstone",
                    TierIndex = 5,
                    Buildings = [entry],
                },
            ],
        };

        var (pos, _) = panel.Resolve(Vector2.Zero, ReferenceViewport);
        panel.UpdatePointerState(pos + FirstEntryTileCenterOffset(), false, Vector2.Zero, ReferenceViewport);

        TooltipContent? content = panel.GetTooltipContent();

        Assert.NotNull(content);
        IReadOnlyList<string> lines = content!.ToLines();
        foreach (string prereq in prerequisites)
            Assert.Contains(lines, line => line.Contains(prereq, StringComparison.Ordinal));

        string? chain = TooltipContent.FormatPrerequisiteChain(entry);
        Assert.NotNull(chain);
        Assert.Contains(lines, line => line == chain);

        var (_, scrollSize, scrolling) = TooltipLayout.MeasureScrollViewport(content);
        float twoLineCap = TooltipLayout.ScrollViewportCapHeight(TooltipWidget.FontSize);
        Assert.True(scrolling);
        Assert.True(scrollSize.Y <= twoLineCap + 0.5f);
    }

    [Fact]
    public void Locked_tile_tooltip_includes_category_tier_lock_hint()
    {
        var entry = new BuildMapEntryView
        {
            Id = "power_reactor",
            Name = "Power Reactor",
            IsUnlocked = false,
            CanAfford = true,
            LockReason = "Requires: Command Center",
            Icon = BuildIconCatalog.Get("power_reactor"),
        };

        var panel = new BuildMapPanel
        {
            Anchor = Anchor.MiddleLeft,
            Position = new Vector2(GameplayHudLayout.BuildMapPanelPositionX, -232f),
            Size = new Vector2(GameplayHudLayout.BuildMapPanelWidth, GameplayHudLayout.BuildMapPanelHeight),
            Visible = true,
            Categories =
            [
                new BuildMapCategoryView
                {
                    Id = "economy",
                    DisplayName = "Economy",
                    TierIndex = 2,
                    Buildings = [entry],
                },
            ],
        };

        var (pos, _) = panel.Resolve(Vector2.Zero, ReferenceViewport);
        panel.UpdatePointerState(pos + FirstEntryTileCenterOffset(), false, Vector2.Zero, ReferenceViewport);

        TooltipContent? content = panel.GetTooltipContent();

        Assert.NotNull(content);
        IReadOnlyList<string> lines = content!.ToLines();
        Assert.Contains(lines, line =>
            line.Contains("Tier locked", StringComparison.OrdinalIgnoreCase)
            && line.Contains("build prerequisites in earlier tiers", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FormatPrerequisiteMicroLabel_fits_width_before_ellipsis()
    {
        const string longName = "Electromagnetic Sensor Relay";
        var entry = new BuildMapEntryView
        {
            IsUnlocked = false,
            LockReason = $"Requires: {longName}",
        };

        string? label = BuildMapPanel.FormatPrerequisiteMicroLabel(entry);

        Assert.NotNull(label);
        Assert.Contains(longName, label, StringComparison.Ordinal);

        var panel = new BuildMapPanel
        {
            Anchor = Anchor.MiddleLeft,
            Position = new Vector2(GameplayHudLayout.BuildMapPanelPositionX, -232f),
            Size = new Vector2(GameplayHudLayout.BuildMapPanelWidth, GameplayHudLayout.BuildMapPanelHeight),
            Visible = true,
            Categories =
            [
                new BuildMapCategoryView
                {
                    Id = "defense",
                    DisplayName = "Defense",
                    TierIndex = 3,
                    Buildings =
                    [
                        new BuildMapEntryView
                        {
                            Id = "sensor_array",
                            Name = "Sensor Array",
                            IsUnlocked = false,
                            CanAfford = true,
                            LockReason = $"Requires: {longName}",
                            Icon = BuildIconCatalog.Get("sensor_array"),
                        },
                    ],
                },
            ],
        };

        var renderer = new RecordingRenderer();
        panel.Draw(renderer, Vector2.Zero, ReferenceViewport);

        var microDraw = renderer.TextDraws
            .First(draw => draw.Text.StartsWith("Needs:", StringComparison.Ordinal));

        Assert.True(microDraw.FontSize >= 8f);

        const float maxLabelWidth = TileSize - 4f;
        Assert.True(UIFontMetrics.MeasureTextWidth(microDraw.Text, microDraw.FontSize) <= maxLabelWidth + 0.5f);
        Assert.Contains("Needs:", microDraw.Text, StringComparison.Ordinal);
        Assert.Contains("Elec", microDraw.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Locked_entry_draws_prerequisite_micro_label()
    {
        var panel = new BuildMapPanel
        {
            Anchor = Anchor.MiddleLeft,
            Position = new Vector2(GameplayHudLayout.BuildMapPanelPositionX, -232f),
            Size = new Vector2(GameplayHudLayout.BuildMapPanelWidth, GameplayHudLayout.BuildMapPanelHeight),
            Visible = true,
            Categories =
            [
                new BuildMapCategoryView
                {
                    Id = "production",
                    DisplayName = "Production",
                    TierIndex = 1,
                    Buildings =
                    [
                        new BuildMapEntryView
                        {
                            Id = "shipyard_small",
                            Name = "Small Shipyard",
                            IsUnlocked = false,
                            CanAfford = true,
                            LockReason = "Requires: Power Reactor",
                            Icon = BuildIconCatalog.Get("shipyard_small"),
                        },
                    ],
                },
            ],
        };

        var renderer = new RecordingRenderer();
        panel.Draw(renderer, Vector2.Zero, ReferenceViewport);

        Assert.Contains(renderer.TextDraws, draw => draw.Text.StartsWith("Needs:", StringComparison.Ordinal));
        Assert.Contains(renderer.TextDraws, draw =>
            draw.Text.Contains("Powe", StringComparison.Ordinal)
            || draw.Text.Contains("Power", StringComparison.Ordinal));
        float microLabelSize = renderer.TextDraws
            .Where(draw => draw.Text.StartsWith("Needs:", StringComparison.Ordinal))
            .Select(draw => draw.FontSize)
            .Max();
        Assert.True(microLabelSize >= 8f, $"Locked micro-label font should be ≥8px, was {microLabelSize}");
    }

    [Fact]
    public void Entry_tile_hit_area_expands_beyond_visual_tile()
    {
        const float tileHitPadding = 2f;
        var panel = CreatePanelWithEntry(unlocked: true, afford: true);
        var (pos, _) = panel.Resolve(Vector2.Zero, ReferenceViewport);
        var tileCenter = pos + FirstEntryTileCenterOffset();
        float halfExpanded = TileSize * 0.5f + tileHitPadding;

        string? selected = null;
        panel.BuildingSelected += id => selected = id;

        Assert.True(panel.HandlePointerTapped(tileCenter, 0, Vector2.Zero, ReferenceViewport));
        Assert.Equal("defense_turret", selected);

        selected = null;
        Assert.True(panel.HandlePointerTapped(
            tileCenter + new Vector2(halfExpanded - 1f, 0f), 0, Vector2.Zero, ReferenceViewport));
        Assert.Equal("defense_turret", selected);

        selected = null;
        Assert.True(panel.HandlePointerTapped(
            tileCenter + new Vector2(halfExpanded + 1f, 0f), 0, Vector2.Zero, ReferenceViewport));
        Assert.Null(selected);
    }

    [Fact]
    public void Close_button_hit_rect_includes_vertical_padding()
    {
        const float closeButtonWidth = 48f;
        var panel = CreatePanelWithEntry(unlocked: true, afford: true);
        var (pos, size) = panel.Resolve(Vector2.Zero, ReferenceViewport);

        bool closed = false;
        panel.CloseRequested += () => closed = true;

        float closeCenterX = pos.X + size.X - PanelPadding - closeButtonWidth * 0.5f;
        float aboveVisualY = pos.Y + PanelPadding - 6f;
        panel.HandlePointerTapped(new Vector2(closeCenterX, aboveVisualY), 0, Vector2.Zero, ReferenceViewport);

        Assert.True(closed);
    }

    [Fact]
    public void Filtered_categories_preserve_tier_index_from_build_map()
    {
        var views = new List<BuildMapCategoryView>
        {
            new()
            {
                Id = "production",
                DisplayName = "Production",
                TierIndex = 1,
                Buildings = [CreateEntryView("command_center")],
            },
            new()
            {
                Id = "economy",
                DisplayName = "Economy",
                TierIndex = 2,
                Buildings = [CreateEntryView("power_reactor")],
            },
        };

        var filtered = FilterBuildViews(views, ["power_reactor"]);

        Assert.Single(filtered);
        Assert.Equal("Economy", filtered[0].DisplayName);
        Assert.Equal(2, filtered[0].TierIndex);

        var panel = new BuildMapPanel
        {
            Size = new Vector2(GameplayHudLayout.BuildMapPanelWidth, GameplayHudLayout.BuildMapPanelHeight),
            Visible = true,
            Categories = filtered,
        };
        var renderer = new RecordingRenderer();
        panel.Draw(renderer, Vector2.Zero, ReferenceViewport);

        string expectedHeader = BuildMapPanel.FormatCategoryHeader("Economy", 2, 1, 1);
        Assert.Contains(renderer.TextDraws, draw => draw.Text == expectedHeader);
    }

    [Fact]
    public void Filtered_categories_show_only_whitelisted_buildings()
    {
        var views = CreateMultiEntryViews();
        var whitelist = new[] { "defense_turret", "repair_bay" };

        var filtered = FilterBuildViews(views, whitelist);
        var visibleIds = filtered
            .SelectMany(category => category.Buildings)
            .Select(entry => entry.Id)
            .OrderBy(id => id)
            .ToArray();

        Assert.Equal(whitelist.OrderBy(id => id).ToArray(), visibleIds);
        Assert.DoesNotContain(
            filtered.SelectMany(category => category.Buildings),
            entry => !whitelist.Contains(entry.Id, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void Hovering_entry_returns_tooltip_content()
    {
        var panel = CreatePanelWithEntry(unlocked: true, afford: true);
        var (pos, _) = panel.Resolve(Vector2.Zero, ReferenceViewport);
        var hoverPoint = pos + FirstEntryTileCenterOffset();

        panel.UpdatePointerState(hoverPoint, false, Vector2.Zero, ReferenceViewport);

        TooltipContent? content = panel.GetTooltipContent();

        Assert.NotNull(content);
        Assert.Equal("Defense Turret", content!.Title);
        Assert.Contains("Build:", content.BuildTime ?? string.Empty, StringComparison.Ordinal);
        IReadOnlyList<string> lines = content.ToLines();
        Assert.Contains("Defense Turret", lines);
    }

    [Fact]
    public void Pointer_outside_panel_clears_tooltip_content()
    {
        var panel = CreatePanelWithEntry(unlocked: true, afford: true);
        var (pos, _) = panel.Resolve(Vector2.Zero, ReferenceViewport);
        var hoverPoint = pos + FirstEntryTileCenterOffset();
        panel.UpdatePointerState(hoverPoint, false, Vector2.Zero, ReferenceViewport);
        Assert.NotNull(panel.GetTooltipContent());

        panel.UpdatePointerState(new Vector2(5f, 5f), false, Vector2.Zero, ReferenceViewport);

        Assert.Null(panel.GetTooltipContent());
    }

    [Fact]
    public void Panel_hidden_clears_tooltip_content()
    {
        var panel = CreatePanelWithEntry(unlocked: true, afford: true);
        var (pos, _) = panel.Resolve(Vector2.Zero, ReferenceViewport);
        panel.UpdatePointerState(pos + FirstEntryTileCenterOffset(), false, Vector2.Zero, ReferenceViewport);
        Assert.NotNull(panel.GetTooltipContent());

        panel.Visible = false;
        panel.UpdatePointerState(pos + FirstEntryTileCenterOffset(), false, Vector2.Zero, ReferenceViewport);

        Assert.Null(panel.GetTooltipContent());
    }

    [Fact]
    public void TooltipContent_FromBuildEntry_maps_entry_fields()
    {
        var entry = new BuildMapEntryView
        {
            Id = "defense_turret",
            Name = "Defense Turret",
            CategoryName = "Defense",
            FootprintCols = 1,
            FootprintRows = 1,
            EnergyCost = 60,
            MineralsCost = 90,
            DataCost = 0,
            CrewCost = 1,
            BuildTime = 18f,
            Prerequisites = ["Sensor Array"],
            LockReason = "Requires: Sensor Array",
            AffordReason = "Insufficient: minerals",
        };

        TooltipContent content = TooltipContent.FromBuildEntry(entry);
        IReadOnlyList<string> lines = content.ToLines();

        Assert.Equal("Defense Turret", content.Title);
        Assert.Equal("Category: Defense", content.RoleLine);
        Assert.Equal("E:60 M:90 D:0 C:1", content.CostLine);
        Assert.Equal("Footprint: 1×1", content.Footprint);
        Assert.Equal("Build: 18s", content.BuildTime);
        Assert.Contains("Prerequisite chain: Sensor Array", lines);
        Assert.Contains("Insufficient: minerals", lines);
    }

    [Fact]
    public void TooltipContent_FormatPrerequisiteChain_returns_null_when_unlocked()
    {
        var entry = new BuildMapEntryView
        {
            IsUnlocked = true,
            Prerequisites = ["Sensor Array"],
        };

        Assert.Null(TooltipContent.FormatPrerequisiteChain(entry));
    }

    [Theory]
    [InlineData(1, "production", "T1")]
    [InlineData(2, "economy", "T2")]
    [InlineData(3, "defense", "T3")]
    [InlineData(4, "support", "T3")]
    [InlineData(5, "capstone", "Cap")]
    public void FormatTierBadge_maps_category_tiers(int tierIndex, string categoryId, string expected)
    {
        Assert.Equal(expected, BuildMapPanel.FormatTierBadge(tierIndex, categoryId));
    }

    [Fact]
    public void Draw_entry_tiles_emit_tier_badges()
    {
        var panel = CreatePanelWithEntry(unlocked: true, afford: true);
        var renderer = new RecordingRenderer();

        panel.Draw(renderer, Vector2.Zero, ReferenceViewport);

        Assert.Contains(renderer.TextDraws, draw => draw.Text == "T3");
    }

    [Fact]
    public void Capstone_locked_entry_draws_prerequisite_progress_micro_label()
    {
        var panel = new BuildMapPanel
        {
            Anchor = Anchor.MiddleLeft,
            Position = new Vector2(GameplayHudLayout.BuildMapPanelPositionX, -232f),
            Size = new Vector2(GameplayHudLayout.BuildMapPanelWidth, GameplayHudLayout.BuildMapPanelHeight),
            Visible = true,
            Categories =
            [
                new BuildMapCategoryView
                {
                    Id = "capstone",
                    DisplayName = "Capstone",
                    TierIndex = 5,
                    Buildings =
                    [
                        new BuildMapEntryView
                        {
                            Id = "orbital_uplink",
                            Name = "Orbital Uplink",
                            CategoryId = "capstone",
                            IsUnlocked = false,
                            CanAfford = true,
                            PrerequisiteMetCount = 2,
                            PrerequisiteTotalCount = 4,
                            Prerequisites = ["Command Center", "Medium Shipyard", "Sensor Array", "Comms Relay"],
                            LockReason = "Requires: Comms Relay, Sensor Array",
                            Icon = BuildIconCatalog.Get("orbital_uplink"),
                        },
                    ],
                },
            ],
        };

        var renderer = new RecordingRenderer();
        panel.Draw(renderer, Vector2.Zero, ReferenceViewport);

        Assert.Contains(renderer.TextDraws, draw => draw.Text == "Cap");
        Assert.Contains(renderer.TextDraws, draw =>
            draw.Text.Contains("2/4", StringComparison.Ordinal)
            && draw.Text.Contains("Needs", StringComparison.Ordinal));
    }

    [Fact]
    public void FormatPrerequisiteMicroLabel_shows_progress_and_unlock_path()
    {
        var entry = new BuildMapEntryView
        {
            CategoryId = "capstone",
            IsUnlocked = false,
            PrerequisiteMetCount = 2,
            PrerequisiteTotalCount = 3,
            LockReason = "Requires: Comms",
        };

        string? label = BuildMapPanel.FormatPrerequisiteMicroLabel(entry);

        Assert.Equal("2/3 · Needs Comms", label);
    }

    [Fact]
    public void Locked_entry_tooltip_includes_prerequisite_progress_for_capstone()
    {
        var entry = new BuildMapEntryView
        {
            Id = "fortress_core",
            Name = "Fortress Core",
            CategoryId = "capstone",
            IsUnlocked = false,
            PrerequisiteMetCount = 2,
            PrerequisiteTotalCount = 5,
            Prerequisites = ["Command Center", "Large Shipyard", "Shield Emitter", "Missile Battery", "Repair Bay"],
            LockReason = "Requires: Shield Emitter",
        };

        TooltipContent content = TooltipContent.FromBuildEntry(entry);
        IReadOnlyList<string> lines = content.ToLines();

        Assert.Contains(lines, line =>
            line.Contains("2/5 met", StringComparison.Ordinal)
            && line.Contains("Prerequisites:", StringComparison.Ordinal));
    }

    [Fact]
    public void Entry_states_use_distinct_fill_colors()
    {
        var panel = new BuildMapPanel
        {
            Anchor = Anchor.MiddleLeft,
            Position = new Vector2(GameplayHudLayout.BuildMapPanelPositionX, -232f),
            Size = new Vector2(GameplayHudLayout.BuildMapPanelWidth, GameplayHudLayout.BuildMapPanelHeight),
            Visible = true,
            Categories =
            [
                new BuildMapCategoryView
                {
                    Id = "defense",
                    DisplayName = "Defense",
                    Buildings =
                    [
                        new BuildMapEntryView
                        {
                            Id = "enabled",
                            Name = "Enabled Turret",
                            IsUnlocked = true,
                            CanAfford = true,
                            Icon = BuildIconCatalog.Get("defense_turret"),
                        },
                        new BuildMapEntryView
                        {
                            Id = "locked",
                            Name = "Locked Turret",
                            IsUnlocked = false,
                            CanAfford = true,
                            Icon = BuildIconCatalog.Get("defense_turret"),
                        },
                        new BuildMapEntryView
                        {
                            Id = "unaffordable",
                            Name = "Unaffordable Turret",
                            IsUnlocked = true,
                            CanAfford = false,
                            Icon = BuildIconCatalog.Get("defense_turret"),
                        },
                    ],
                },
            ],
        };

        var renderer = new ColorRecordingRenderer();
        panel.Draw(renderer, Vector2.Zero, ReferenceViewport);

        Assert.Contains(renderer.FillColors, color => color == panel.EnabledColor);
        Assert.Contains(renderer.FillColors, color => color == panel.LockedColor);
        Assert.Contains(renderer.FillColors, color => color == panel.UnaffordableColor);
    }

    [Fact]
    public void Hovering_entry_emits_hover_outline_ring()
    {
        var panel = CreatePanelWithEntry(unlocked: true, afford: true);
        var (pos, _) = panel.Resolve(Vector2.Zero, ReferenceViewport);
        panel.UpdatePointerState(pos + FirstEntryTileCenterOffset(), false, Vector2.Zero, ReferenceViewport);

        var renderer = new OutlineRecordingRenderer();
        panel.Draw(renderer, Vector2.Zero, ReferenceViewport);

        Assert.Contains(renderer.OutlineColors, color => color == panel.HoverOutlineColor);
        Assert.True(renderer.OutlineDrawCount >= 2,
            "Hovered entry should draw base outline plus hover ring.");
    }

    [Fact]
    public void Locked_entry_draws_overlay()
    {
        var panel = CreatePanelWithEntry(unlocked: false, afford: true);
        var renderer = new RecordingRenderer();

        panel.Draw(renderer, Vector2.Zero, ReferenceViewport);

        Assert.Contains(renderer.TextDraws, draw => draw.Text == "LOCK");
        Assert.True(renderer.RectDrawCount >= 4,
            "Locked entry should include icon rects plus lock overlay rect.");
    }

    private static List<BuildMapCategoryView> CreateMultiEntryViews() =>
    [
        new BuildMapCategoryView
        {
            Id = "defense",
            DisplayName = "Defense",
            Buildings =
            [
                CreateEntryView("defense_turret"),
                CreateEntryView("sensor_array"),
            ],
        },
        new BuildMapCategoryView
        {
            Id = "support",
            DisplayName = "Support",
            Buildings =
            [
                CreateEntryView("repair_bay"),
                CreateEntryView("supply_depot"),
            ],
        },
    ];

    private static BuildMapEntryView CreateEntryView(string id) =>
        new()
        {
            Id = id,
            Name = id,
            FootprintCols = 1,
            FootprintRows = 1,
            EnergyCost = 60,
            MineralsCost = 90,
            IsUnlocked = true,
            CanAfford = true,
            Icon = BuildIconCatalog.Get(id),
        };

    private static List<BuildMapCategoryView> FilterBuildViews(
        List<BuildMapCategoryView> views,
        IReadOnlyList<string> buildableIds)
    {
        var allowed = new HashSet<string>(buildableIds, StringComparer.OrdinalIgnoreCase);
        var filtered = new List<BuildMapCategoryView>();

        foreach (var category in views)
        {
            var buildings = category.Buildings
                .Where(entry => allowed.Contains(entry.Id))
                .ToList();
            if (buildings.Count == 0)
                continue;

            filtered.Add(new BuildMapCategoryView
            {
                Id = category.Id,
                DisplayName = category.DisplayName,
                TierIndex = category.TierIndex,
                UnlockedCount = buildings.Count(static entry => entry.IsUnlocked),
                TotalCount = buildings.Count,
                Buildings = buildings,
            });
        }

        return filtered;
    }

    private static BuildMapPanel CreatePanelWithEntry(bool unlocked, bool afford)
    {
        var panel = new BuildMapPanel
        {
            Anchor = Anchor.MiddleLeft,
            Position = new Vector2(12f, -220f),
            Size = new Vector2(GameplayHudLayout.BuildMapPanelWidth, GameplayHudLayout.BuildMapPanelHeight),
            Visible = true,
            Categories =
            [
                new BuildMapCategoryView
                {
                    Id = "defense",
                    DisplayName = "Defense",
                    TierIndex = 3,
                    Buildings =
                    [
                        new BuildMapEntryView
                        {
                            Id = "defense_turret",
                            Name = "Defense Turret",
                            FootprintCols = 1,
                            FootprintRows = 1,
                            EnergyCost = 60,
                            MineralsCost = 90,
                            IsUnlocked = unlocked,
                            CanAfford = afford,
                            Icon = BuildIconCatalog.Get("defense_turret"),
                        },
                    ],
                },
            ],
        };
        return panel;
    }

    private sealed class ScaledRecordingRenderer : IUIRenderer
    {
        public ScaledRecordingRenderer(Vector2 viewport) => ViewportSize = viewport;
        public Vector2 ViewportSize { get; }
        public List<(string Text, float FontSize, Vector2 Position)> TextDraws { get; } = new();

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) { }
        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }
        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color) =>
            TextDraws.Add((text, fontSize, position));
    }

    private sealed class RecordingRenderer : IUIRenderer
    {
        public Vector2 ViewportSize { get; } = ReferenceViewport;
        public int RectDrawCount { get; private set; }
        public Vector2 MaxRectSize { get; private set; }
        public Vector2 MaxIconRectSize { get; private set; }
        public List<(string Text, float FontSize, Vector2 Position)> TextDraws { get; } = new();

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color)
        {
            RectDrawCount++;
            MaxRectSize = new Vector2(
                MathF.Max(MaxRectSize.X, size.X),
                MathF.Max(MaxRectSize.Y, size.Y));
            if (size.X >= IconSize - 0.5f && size.Y >= IconSize - 0.5f)
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

    private sealed class ColorRecordingRenderer : IUIRenderer
    {
        public Vector2 ViewportSize { get; } = ReferenceViewport;
        public List<Vector4> FillColors { get; } = new();

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) => FillColors.Add(color);

        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }

        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color) { }
    }

    private sealed class OutlineRecordingRenderer : IUIRenderer
    {
        public Vector2 ViewportSize { get; } = ReferenceViewport;
        public int OutlineDrawCount { get; private set; }
        public List<Vector4> OutlineColors { get; } = new();

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) { }

        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color)
        {
            OutlineDrawCount++;
            OutlineColors.Add(color);
        }

        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color) { }
    }
}