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
    private const float TileSize = 56f;
    private const float IconSize = 48f;
    private const float PanelPadding = 10f;
    private const float TitleHeight = 28f;
    private const float CategoryHeaderHeight = 20f;
    private const float HeaderFontSize = 12f;

    /// <summary>Local offset to the centre of the first 56×56 entry tile.</summary>
    private static Vector2 FirstEntryTileCenterOffset()
    {
        float contentTop = PanelPadding + TitleHeight + HeaderFontSize + 8f;
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

        var tap = new Vector2(1756f, 28f);
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
        Assert.Contains(renderer.TextDraws, draw => draw.Text == "Defense");
    }

    [Fact]
    public void Entry_tile_hit_area_is_56_by_56()
    {
        var panel = CreatePanelWithEntry(unlocked: true, afford: true);
        var (pos, _) = panel.Resolve(Vector2.Zero, ReferenceViewport);
        var tileCenter = pos + FirstEntryTileCenterOffset();
        float halfTile = TileSize * 0.5f;

        string? selected = null;
        panel.BuildingSelected += id => selected = id;

        Assert.True(panel.HandlePointerTapped(tileCenter, 0, Vector2.Zero, ReferenceViewport));
        Assert.Equal("defense_turret", selected);

        selected = null;
        Assert.True(panel.HandlePointerTapped(tileCenter + new Vector2(halfTile, 0f), 0, Vector2.Zero, ReferenceViewport));
        Assert.Null(selected);

        selected = null;
        Assert.True(panel.HandlePointerTapped(tileCenter + new Vector2(0f, halfTile), 0, Vector2.Zero, ReferenceViewport));
        Assert.Null(selected);
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
        Assert.Equal("E:60 M:90 D:0 C:1", content.CostLine);
        Assert.Equal("Footprint: 1×1", content.Footprint);
        Assert.Equal("Build: 18s", content.BuildTime);
        Assert.Contains("• Sensor Array", lines);
        Assert.Contains("Requires: Sensor Array", lines);
        Assert.Contains("Insufficient: minerals", lines);
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
            Size = new Vector2(420f, 640f),
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
}