using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class MultiplayerSetupScreenTests
{
    private static readonly Vector2 ReferenceViewport = UIScaler.ReferenceSize;
    private static readonly Vector2 StartButtonCenter = new(790f, 667f);
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
}