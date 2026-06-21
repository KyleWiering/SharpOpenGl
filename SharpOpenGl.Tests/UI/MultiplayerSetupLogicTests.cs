using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.UI;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class MultiplayerSetupLogicTests
{
    private static readonly string[] RaceIds = MultiplayerSetupLogic.FallbackRaceIds;

    [Fact]
    public void Default_slots_include_one_human_and_one_ai()
    {
        var slots = MultiplayerSetupLogic.CreateDefaultSlots(RaceIds);

        Assert.Equal(MultiplayerSlotKind.Human, slots[0].Kind);
        Assert.Equal(MultiplayerSlotKind.Ai, slots[1].Kind);
        Assert.All(slots.Skip(2), slot => Assert.Equal(MultiplayerSlotKind.Empty, slot.Kind));
    }

    [Fact]
    public void CanStart_requires_at_least_one_human()
    {
        var slots = MultiplayerSetupLogic.CreateDefaultSlots(RaceIds);
        Assert.True(MultiplayerSetupLogic.CanStart(slots));

        slots[0].Kind = MultiplayerSlotKind.Ai;
        Assert.False(MultiplayerSetupLogic.CanStart(slots));

        slots[3].Kind = MultiplayerSlotKind.Human;
        Assert.True(MultiplayerSetupLogic.CanStart(slots));
    }

    [Theory]
    [InlineData(MultiplayerSlotKind.Empty, MultiplayerSlotKind.Human)]
    [InlineData(MultiplayerSlotKind.Human, MultiplayerSlotKind.Ai)]
    [InlineData(MultiplayerSlotKind.Ai, MultiplayerSlotKind.Empty)]
    public void CycleKind_rotates_empty_human_ai(MultiplayerSlotKind current, MultiplayerSlotKind expected) =>
        Assert.Equal(expected, MultiplayerSetupLogic.CycleKind(current));

    [Fact]
    public void CycleRaceIndex_wraps_across_all_races()
    {
        Assert.Equal(1, MultiplayerSetupLogic.CycleRaceIndex(0, 1, RaceIds.Length));
        Assert.Equal(RaceIds.Length - 1, MultiplayerSetupLogic.CycleRaceIndex(0, -1, RaceIds.Length));
        Assert.Equal(0, MultiplayerSetupLogic.CycleRaceIndex(RaceIds.Length - 1, 1, RaceIds.Length));
    }

    [Fact]
    public void BuildResult_includes_only_active_slots_with_race_ids()
    {
        var slots = MultiplayerSetupLogic.CreateDefaultSlots(RaceIds);
        slots[1].RaceIndex = Array.FindIndex(RaceIds, id => id == "korath");
        slots[4].Kind = MultiplayerSlotKind.Ai;
        slots[4].RaceIndex = Array.FindIndex(RaceIds, id => id == "nexar");

        var map = SkirmishMapCatalog.FallbackMaps.First(m => m.PlayerCount >= 3);
        var result = MultiplayerSetupLogic.BuildResult(slots, RaceIds, map);

        Assert.NotNull(result);
        Assert.Equal(map.Id, result!.MapId);
        Assert.Equal(3, result.ActivePlayerCount);
        Assert.Equal(1, result.HumanCount);
        Assert.Equal(2, result.AiCount);

        var human = Assert.Single(result.Players, p => p.IsHuman);
        Assert.Equal(0, human.SlotIndex);
        Assert.Equal("terran", human.RaceId);

        Assert.Contains(result.Players, p => !p.IsHuman && p.SlotIndex == 1 && p.RaceId == "korath");
        Assert.Contains(result.Players, p => !p.IsHuman && p.SlotIndex == 4 && p.RaceId == "nexar");
    }

    [Fact]
    public void ParseConfiguration_supports_eight_active_players_with_seven_ai()
    {
        var slots = new List<(int SlotIndex, MultiplayerSlotKind Kind, string RaceId)>
        {
            (0, MultiplayerSlotKind.Human, "terran"),
        };

        string[] aiRaces = ["vesper", "korath", "aetherian", "nexar", "solari", "voidborn", "cryo"];
        for (int i = 0; i < aiRaces.Length; i++)
            slots.Add((i + 1, MultiplayerSlotKind.Ai, aiRaces[i]));

        var result = MultiplayerSetupLogic.ParseConfiguration(slots, RaceIds);

        Assert.Equal(8, result.ActivePlayerCount);
        Assert.Equal(1, result.HumanCount);
        Assert.Equal(7, result.AiCount);
        Assert.Equal(8, result.Players.Count);
        Assert.Equal(
            RaceIds.OrderBy(id => id).ToArray(),
            result.Players.Select(p => p.RaceId).OrderBy(id => id).ToArray());
    }

    [Fact]
    public void CanStart_rejects_more_active_players_than_selected_map_supports()
    {
        var slots = MultiplayerSetupLogic.CreateDefaultSlots(RaceIds);
        for (int slot = 2; slot < MultiplayerSetupLogic.MaxSlots; slot++)
        {
            slots[slot].Kind = MultiplayerSlotKind.Ai;
        }

        var duelMap = new SkirmishMapEntry("duel_frontier", "Duel Frontier", 2);
        var octagonMap = new SkirmishMapEntry("octagon_rim", "Octagon Rim", 8);

        Assert.False(MultiplayerSetupLogic.CanStart(slots, duelMap));
        Assert.True(MultiplayerSetupLogic.CanStart(slots, octagonMap));
    }

    [Fact]
    public void ParseConfiguration_rejects_all_empty_or_all_ai()
    {
        var allAi = Enumerable.Range(0, 8)
            .Select(i => (i, MultiplayerSlotKind.Ai, RaceIds[i]))
            .ToList();

        Assert.Throws<InvalidOperationException>(() =>
            MultiplayerSetupLogic.ParseConfiguration(allAi, RaceIds));
    }
}