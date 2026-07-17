using System.Reflection;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class MultiplayerSetupScreenTests
{
    private static readonly Vector2 ReferenceViewport = UIScaler.ReferenceSize;
    private static readonly Vector2 StartButtonCenter = new(790f, 681f);
    private static readonly Vector2 Slot0RaceNextCenter = new(690f, 214f);
    private static readonly Vector2 MapNextCenter = new(1240f, 154f);

    [Fact]
    public void Default_configuration_can_start_match()
    {
        var screen = new MultiplayerSetupScreen();

        Assert.True(screen.CanStartMatch);
        var result = screen.BuildResult();

        Assert.NotNull(result);
        Assert.Equal(2, result!.ActivePlayerCount);
        Assert.Equal("terran", screen.GetSlotRaceId(0));
        Assert.Equal("korath", screen.GetSlotRaceId(1));
        Assert.Equal("duel_frontier", screen.GetSelectedMapId());
    }

    [Fact]
    public void Start_match_reports_active_slots()
    {
        var screen = new MultiplayerSetupScreen();
        MultiplayerSetupResult? result = null;
        screen.StartRequested += r => result = r;

        bool consumed = screen.HandlePointerTapped(StartButtonCenter, 0, ReferenceViewport);

        Assert.True(consumed);
        Assert.NotNull(result);
        Assert.Equal(2, result!.ActivePlayerCount);
        Assert.Contains(result.Players, p => p.IsHuman && p.RaceId == "terran");
        Assert.Contains(result.Players, p => !p.IsHuman && p.RaceId == "korath");
    }

    [Fact]
    public void Cycling_slot_race_changes_selection()
    {
        var screen = new MultiplayerSetupScreen();
        string first = screen.GetSlotRaceId(0);

        screen.CycleSlotRace(0, 1);

        Assert.NotEqual(first, screen.GetSlotRaceId(0));
    }

    [Fact]
    public void Cycling_slot_kind_through_empty_disables_start_until_human_added()
    {
        var screen = new MultiplayerSetupScreen();

        screen.CycleSlotKind(0); // Human -> AI
        screen.CycleSlotKind(0); // AI -> Empty
        Assert.False(screen.CanStartMatch);

        screen.CycleSlotKind(0); // Empty -> Human
        Assert.True(screen.CanStartMatch);
    }

    [Fact]
    public void Race_next_button_cycles_first_slot_race()
    {
        var screen = new MultiplayerSetupScreen();
        string first = screen.GetSlotRaceId(0);

        screen.HandlePointerTapped(Slot0RaceNextCenter, 0, ReferenceViewport);

        Assert.NotEqual(first, screen.GetSlotRaceId(0));
    }

    [Fact]
    public void Cycling_map_changes_selected_map_id()
    {
        var screen = new MultiplayerSetupScreen();
        string initial = screen.GetSelectedMapId();

        screen.CycleMap(1);

        Assert.NotEqual(initial, screen.GetSelectedMapId());
    }

    [Fact]
    public void Map_next_button_cycles_selected_map()
    {
        var screen = new MultiplayerSetupScreen();
        string initial = screen.GetSelectedMapId();

        screen.HandlePointerTapped(MapNextCenter, 0, ReferenceViewport);

        Assert.NotEqual(initial, screen.GetSelectedMapId());
    }

    [Theory]
    [InlineData(390f, 844f)]
    [InlineData(1024f, 768f)]
    [InlineData(1920f, 1080f)]
    public void Kind_and_race_toggle_buttons_meet_minimum_logical_hit_extent(float viewportWidth, float viewportHeight)
    {
        var screen = new MultiplayerSetupScreen();

        string[] toggleNames =
        [
            "MapPrev", "MapNext",
            "Slot0Kind", "Slot0RacePrev", "Slot0RaceNext",
        ];

        foreach (string name in toggleNames)
        {
            var button = FindButton(screen, name);
            Assert.NotNull(button);
            Assert.True(button.RequireMinimumHitExtent, name);

            var (pos, size) = button.Resolve(Vector2.Zero, UIScaler.ReferenceSize);
            var (_, hitSize) = Button.GetExpandedHitRect(pos, size, button.HitPadding, button.RequireMinimumHitExtent);
            Assert.True(hitSize.X >= Button.MinimumHitExtent, $"{name} width {hitSize.X} at {viewportWidth}x{viewportHeight}");
            Assert.True(hitSize.Y >= Button.MinimumHitExtent, $"{name} height {hitSize.Y} at {viewportWidth}x{viewportHeight}");
        }
    }

    [Fact]
    public void Browser_viewport_taps_cycle_map_and_slot_kind_controls()
    {
        var viewport = new Vector2(390f, 844f);
        var mgr = new UIManager(new EventBus());
        mgr.Resize(viewport);

        var screen = new MultiplayerSetupScreen();
        string initialMap = screen.GetSelectedMapId();
        mgr.Push(screen);

        var scaler = new UIScaler(viewport);
        var mapNext = FindButton(screen, "MapNext")!;
        var (mapPos, mapSize) = mapNext.Resolve(Vector2.Zero, UIScaler.ReferenceSize);
        Vector2 mapTap = scaler.ScalePosition(mapPos + mapSize * 0.5f);

        Assert.True(mgr.HandlePointerTapped(mapTap, 0, viewport));
        Assert.NotEqual(initialMap, screen.GetSelectedMapId());

        var kindBtn = FindButton(screen, "Slot0Kind")!;
        string initialKind = screen.GetSlotKind(0).ToString();
        var (kindPos, kindSize) = kindBtn.Resolve(Vector2.Zero, UIScaler.ReferenceSize);
        Vector2 kindTap = scaler.ScalePosition(kindPos + kindSize * 0.5f);

        Assert.True(mgr.HandlePointerTapped(kindTap, 0, viewport));
        Assert.NotEqual(initialKind, screen.GetSlotKind(0).ToString());

        string initialRace = screen.GetSlotRaceId(0);
        var raceNext = FindButton(screen, "Slot0RaceNext")!;
        var (racePos, raceSize) = raceNext.Resolve(Vector2.Zero, UIScaler.ReferenceSize);
        Vector2 raceTap = scaler.ScalePosition(racePos + raceSize * 0.5f);

        Assert.True(mgr.HandlePointerTapped(raceTap, 0, viewport));
        Assert.NotEqual(initialRace, screen.GetSlotRaceId(0));
    }

    [Fact]
    public void Compact_viewport_layout_regions_do_not_overlap_under_stress_configuration()
    {
        var screen = CreateStressLayoutScreen();
        var viewport = UIScaler.ReferenceSize;

        var regions = new[]
        {
            MultiplayerSetupLayout.GetRegionBounds(MultiplayerSetupLayout.RegionKind.MapPicker, viewport),
            MultiplayerSetupLayout.GetRegionBounds(MultiplayerSetupLayout.RegionKind.SlotGrid, viewport),
            MultiplayerSetupLayout.GetRegionBounds(MultiplayerSetupLayout.RegionKind.Validation, viewport),
            MultiplayerSetupLayout.GetRegionBounds(MultiplayerSetupLayout.RegionKind.StartActions, viewport),
        };

        Assert.False(MultiplayerSetupLayout.AnyOverlap(regions));

        var mapLabel = FindLabel(screen, "MapLabel")!;
        var slotRow = FindButton(screen, "Slot7Kind")!;
        var validationScroll = FindWidget<ScrollPanel>(screen, "MPValidationScroll")!;
        var validation = FindLabel(screen, "MPValidation")!;
        var start = FindButton(screen, "StartMP")!;

        var mapBounds = MultiplayerSetupLayout.ToRect(mapLabel.Resolve(Vector2.Zero, viewport));
        var slotBounds = MultiplayerSetupLayout.ToRect(slotRow.Resolve(Vector2.Zero, viewport));
        var validationBounds = MultiplayerSetupLayout.ToRect(validationScroll.Resolve(Vector2.Zero, viewport));
        var startBounds = MultiplayerSetupLayout.ToRect(start.Resolve(Vector2.Zero, viewport));

        Assert.True(mapBounds.Bottom + MultiplayerSetupLayout.RegionGap <= slotBounds.Top);
        Assert.True(slotBounds.Bottom + MultiplayerSetupLayout.RegionGap <= validationBounds.Top);
        Assert.True(validationBounds.Bottom + MultiplayerSetupLayout.RegionGap <= startBounds.Top);
        Assert.False(string.IsNullOrWhiteSpace(validation.Text));
    }

    [Theory]
    [InlineData(MultiplayerSlotKind.Human, nameof(MenuTheme.SlotHumanNormal), nameof(MenuTheme.SlotHumanBorder))]
    [InlineData(MultiplayerSlotKind.Ai, nameof(MenuTheme.SlotAiNormal), nameof(MenuTheme.SlotAiBorder))]
    [InlineData(MultiplayerSlotKind.Empty, nameof(MenuTheme.SlotEmptyNormal), nameof(MenuTheme.SlotEmptyBorder))]
    public void Kind_toggle_applies_distinct_visual_state(
        MultiplayerSlotKind kind,
        string expectedNormalName,
        string expectedBorderName)
    {
        var screen = new MultiplayerSetupScreen();
        while (screen.GetSlotKind(2) != kind)
            screen.CycleSlotKind(2);

        var button = FindButton(screen, "Slot2Kind")!;
        Vector4 expectedNormal = ReadThemeColor(expectedNormalName);
        Vector4 expectedBorder = ReadThemeColor(expectedBorderName);

        Assert.Equal(expectedNormal, button.NormalColor);
        Assert.Equal(expectedBorder, button.BorderColor);
    }

    [Fact]
    public void Active_slot_race_toggles_use_race_accent_border()
    {
        var screen = new MultiplayerSetupScreen();
        string raceId = screen.GetSlotRaceId(0);
        Vector4 accent = MenuTheme.ResolveRaceAccentColor(raceId);

        var racePrev = FindButton(screen, "Slot0RacePrev")!;
        var raceNext = FindButton(screen, "Slot0RaceNext")!;

        Assert.Equal(accent, racePrev.BorderColor);
        Assert.Equal(accent, raceNext.BorderColor);
        Assert.Equal(MenuTheme.ButtonText, racePrev.TextColor);
    }

    [Fact]
    public void Empty_slot_hides_race_toggles_and_uses_muted_kind_state()
    {
        var screen = new MultiplayerSetupScreen();
        screen.CycleSlotKind(0);
        screen.CycleSlotKind(0);
        Assert.Equal(MultiplayerSlotKind.Empty, screen.GetSlotKind(0));

        var kindButton = FindButton(screen, "Slot0Kind")!;
        var racePrev = FindButton(screen, "Slot0RacePrev")!;
        var raceNext = FindButton(screen, "Slot0RaceNext")!;

        Assert.Equal(MenuTheme.SlotEmptyNormal, kindButton.NormalColor);
        Assert.False(racePrev.Visible);
        Assert.False(raceNext.Visible);
    }

    [Fact]
    public void All_slot_kind_and_race_toggles_require_minimum_hit_extent()
    {
        var screen = new MultiplayerSetupScreen();

        for (int slot = 0; slot < MultiplayerSetupScreen.SlotCount; slot++)
        {
            foreach (string suffix in new[] { "Kind", "RacePrev", "RaceNext" })
            {
                var button = FindButton(screen, $"Slot{slot}{suffix}");
                Assert.NotNull(button);
                Assert.True(button.RequireMinimumHitExtent, $"Slot{slot}{suffix}");
            }
        }
    }

    [Fact]
    public void Validation_scroll_panel_recalculates_when_message_wraps()
    {
        var screen = CreateStressLayoutScreen();
        var validationScroll = FindWidget<ScrollPanel>(screen, "MPValidationScroll");
        var validationLabel = FindLabel(screen, "MPValidation");
        Assert.NotNull(validationScroll);
        Assert.NotNull(validationLabel);
        Assert.False(string.IsNullOrWhiteSpace(validationLabel!.Text));

        validationScroll!.SyncLabelWrapWidths();
        float labelHeight = validationLabel.MeasureContentHeight();
        validationLabel.Size = new Vector2(validationScroll.Size.X, labelHeight);
        validationScroll.RecalculateContentHeight(validationScroll.Size);

        Assert.True(validationScroll.ContentHeight >= labelHeight + validationScroll.ContentPadding - 0.01f);

        validationLabel.Text =
            "Duel Frontier supports up to 2 players (8 selected). " +
            "Choose a larger map with enough spawn capacity for every active combatant.";
        validationScroll.SyncLabelWrapWidths();
        labelHeight = validationLabel.MeasureContentHeight();
        validationLabel.Size = new Vector2(validationScroll.Size.X, labelHeight);
        validationScroll.RecalculateContentHeight(validationScroll.Size);

        Assert.True(validationScroll.ContentHeight > validationScroll.Size.Y);
        Assert.True(validationScroll.MaxScrollOffset(validationScroll.Size) > 0f);
    }

    [Fact]
    public void Eight_slot_configuration_with_seven_ai_requires_large_map()
    {
        var screen = new MultiplayerSetupScreen();

        for (int slot = 2; slot < MultiplayerSetupScreen.SlotCount; slot++)
        {
            screen.CycleSlotKind(slot); // Empty -> Human
            screen.CycleSlotKind(slot); // Human -> AI
        }

        Assert.False(screen.CanStartMatch);

        while (screen.GetSelectedMapId() != "octagon_rim")
            screen.CycleMap(1);

        Assert.True(screen.CanStartMatch);
        var result = screen.BuildResult();

        Assert.NotNull(result);
        Assert.Equal("octagon_rim", result!.MapId);
        Assert.Equal(8, result.ActivePlayerCount);
        Assert.Equal(1, result.HumanCount);
        Assert.Equal(7, result.AiCount);
    }

    private static MultiplayerSetupScreen CreateStressLayoutScreen()
    {
        var screen = new MultiplayerSetupScreen();

        for (int slot = 2; slot < MultiplayerSetupScreen.SlotCount; slot++)
        {
            screen.CycleSlotKind(slot);
            screen.CycleSlotKind(slot);
        }

        return screen;
    }

    private static Vector4 ReadThemeColor(string fieldName)
    {
        var field = typeof(MenuTheme).GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(field);
        return (Vector4)field!.GetValue(null)!;
    }

    private static T? FindWidget<T>(UIScreen screen, string name) where T : Widget
    {
        foreach (Widget root in GetRoots(screen))
        {
            T? match = FindWidgetInTree<T>(root, name);
            if (match != null)
                return match;
        }

        return null;
    }

    private static T? FindWidgetInTree<T>(Widget widget, string name) where T : Widget
    {
        if (widget.Name == name && widget is T match)
            return match;

        foreach (Widget child in widget.Children)
        {
            T? childMatch = FindWidgetInTree<T>(child, name);
            if (childMatch != null)
                return childMatch;
        }

        return null;
    }

    private static Label? FindLabel(UIScreen screen, string name)
    {
        foreach (Widget root in GetRoots(screen))
        {
            Label? match = FindLabelInTree(root, name);
            if (match != null)
                return match;
        }

        return null;
    }

    private static Label? FindLabelInTree(Widget widget, string name)
    {
        if (widget is Label label && label.Name == name)
            return label;

        foreach (Widget child in widget.Children)
        {
            Label? match = FindLabelInTree(child, name);
            if (match != null)
                return match;
        }

        return null;
    }

    private static Button? FindButton(UIScreen screen, string name)
    {
        foreach (Widget root in GetRoots(screen))
        {
            Button? match = FindButtonInTree(root, name);
            if (match != null)
                return match;
        }

        return null;
    }

    private static IEnumerable<Widget> GetRoots(UIScreen screen)
    {
        FieldInfo? field = typeof(UIScreen).GetField("_roots", BindingFlags.Instance | BindingFlags.NonPublic);
        return (IReadOnlyList<Widget>)(field?.GetValue(screen) ?? Array.Empty<Widget>());
    }

    private static Button? FindButtonInTree(Widget widget, string name)
    {
        if (widget is Button button && button.Name == name)
            return button;

        foreach (Widget child in widget.Children)
        {
            Button? match = FindButtonInTree(child, name);
            if (match != null)
                return match;
        }

        return null;
    }
}