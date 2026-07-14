using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Engine.UI;

/// <summary>Occupancy for one of the eight multiplayer lobby slots.</summary>
public enum MultiplayerSlotKind
{
    Empty,
    Human,
    Ai,
}

/// <summary>Mutable lobby slot used by the setup screen and tests.</summary>
public sealed class MultiplayerSlotState
{
    public int SlotIndex { get; init; }
    public MultiplayerSlotKind Kind { get; set; } = MultiplayerSlotKind.Empty;
    public int RaceIndex { get; set; }
}

/// <summary>One active player chosen on the multiplayer setup screen.</summary>
public sealed record MultiplayerPlayerSlot(int SlotIndex, bool IsHuman, string RaceId);

/// <summary>Validated multiplayer lobby configuration passed into gameplay spawn.</summary>
public sealed record MultiplayerSetupResult(
    IReadOnlyList<MultiplayerPlayerSlot> Players,
    string MapId,
    SkirmishDifficultyTier Difficulty = SkirmishDifficultyTier.Normal)
{
    public int ActivePlayerCount => Players.Count;
    public int AiCount => Players.Count(p => !p.IsHuman);
    public int HumanCount => Players.Count(p => p.IsHuman);
}

/// <summary>Slot validation, race cycling, and result building for multiplayer setup.</summary>
public static class MultiplayerSetupLogic
{
    public const int MaxSlots = 8;

    public static readonly string[] FallbackRaceIds =
    [
        "terran", "vesper", "korath", "aetherian", "nexar", "solari", "voidborn", "cryo",
    ];

    public static string[] ResolveRaceIds()
    {
        var ids = RaceVisualSchema.AllRaces
            .Select(r => r.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return ids.Length >= 2 ? ids : FallbackRaceIds;
    }

    public static MultiplayerSlotState[] CreateDefaultSlots(string[] raceIds)
    {
        var slots = new MultiplayerSlotState[MaxSlots];
        for (int i = 0; i < MaxSlots; i++)
        {
            slots[i] = new MultiplayerSlotState
            {
                SlotIndex = i,
                Kind = i switch
                {
                    0 => MultiplayerSlotKind.Human,
                    1 => MultiplayerSlotKind.Ai,
                    _ => MultiplayerSlotKind.Empty,
                },
                RaceIndex = ResolveDefaultRaceIndex(i, raceIds),
            };
        }

        return slots;
    }

    public static MultiplayerSlotKind CycleKind(MultiplayerSlotKind kind) => kind switch
    {
        MultiplayerSlotKind.Empty => MultiplayerSlotKind.Human,
        MultiplayerSlotKind.Human => MultiplayerSlotKind.Ai,
        _ => MultiplayerSlotKind.Empty,
    };

    public static int CycleRaceIndex(int current, int delta, int raceCount)
    {
        if (raceCount <= 0) return 0;
        int wrapped = current + delta;
        wrapped %= raceCount;
        return wrapped < 0 ? wrapped + raceCount : wrapped;
    }

    public static int CountActiveSlots(IReadOnlyList<MultiplayerSlotState> slots) =>
        slots.Count(slot => IsActive(slot));

    public static bool CanStart(IReadOnlyList<MultiplayerSlotState> slots) =>
        slots.Any(slot => slot.Kind == MultiplayerSlotKind.Human);

    public static bool CanStart(
        IReadOnlyList<MultiplayerSlotState> slots,
        SkirmishMapEntry selectedMap)
    {
        if (!CanStart(slots)) return false;
        return CountActiveSlots(slots) <= selectedMap.PlayerCount;
    }

    public static int CycleMapIndex(int current, int delta, int mapCount)
    {
        if (mapCount <= 0) return 0;
        int wrapped = current + delta;
        wrapped %= mapCount;
        return wrapped < 0 ? wrapped + mapCount : wrapped;
    }

    public static int ResolveDefaultMapIndex(IReadOnlyList<SkirmishMapEntry> maps, int activePlayerCount)
    {
        if (maps.Count == 0) return 0;

        for (int i = 0; i < maps.Count; i++)
        {
            if (maps[i].PlayerCount == activePlayerCount)
                return i;
        }

        for (int i = 0; i < maps.Count; i++)
        {
            if (maps[i].PlayerCount >= activePlayerCount)
                return i;
        }

        return 0;
    }

    public static bool IsActive(MultiplayerSlotState slot) =>
        slot.Kind != MultiplayerSlotKind.Empty;

    public static string ResolveRaceId(MultiplayerSlotState slot, string[] raceIds)
    {
        if (raceIds.Length == 0) return RaceShipMeshes.DefaultRace;
        int index = Math.Clamp(slot.RaceIndex, 0, raceIds.Length - 1);
        return raceIds[index];
    }

    public static MultiplayerSetupResult? BuildResult(
        IReadOnlyList<MultiplayerSlotState> slots,
        string[] raceIds,
        SkirmishMapEntry selectedMap,
        SkirmishDifficultyTier difficulty = SkirmishDifficultyTier.Normal)
    {
        if (!CanStart(slots, selectedMap)) return null;

        var players = new List<MultiplayerPlayerSlot>(MaxSlots);
        foreach (var slot in slots.OrderBy(s => s.SlotIndex))
        {
            if (!IsActive(slot)) continue;

            players.Add(new MultiplayerPlayerSlot(
                slot.SlotIndex,
                slot.Kind == MultiplayerSlotKind.Human,
                ResolveRaceId(slot, raceIds)));
        }

        return players.Count == 0
            ? null
            : new MultiplayerSetupResult(players, selectedMap.Id, difficulty);
    }

    public static string DescribeMapValidation(
        IReadOnlyList<MultiplayerSlotState> slots,
        SkirmishMapEntry selectedMap)
    {
        if (!CanStart(slots))
            return "At least one player slot must be set to Human.";

        int active = CountActiveSlots(slots);
        if (active > selectedMap.PlayerCount)
        {
            return $"{selectedMap.DisplayName} supports up to {selectedMap.PlayerCount} players " +
                   $"({active} selected). Choose a larger map.";
        }

        return string.Empty;
    }

    public static MultiplayerSetupResult ParseConfiguration(
        IReadOnlyList<(int SlotIndex, MultiplayerSlotKind Kind, string RaceId)> slots,
        string[]? raceIds = null)
    {
        raceIds ??= ResolveRaceIds();
        var state = new MultiplayerSlotState[MaxSlots];
        for (int i = 0; i < MaxSlots; i++)
        {
            state[i] = new MultiplayerSlotState
            {
                SlotIndex = i,
                Kind = MultiplayerSlotKind.Empty,
                RaceIndex = 0,
            };
        }

        foreach (var (slotIndex, kind, raceId) in slots)
        {
            if (slotIndex < 0 || slotIndex >= MaxSlots) continue;
            state[slotIndex].Kind = kind;
            state[slotIndex].RaceIndex = FindRaceIndex(raceId, raceIds);
        }

        var map = SkirmishMapCatalog.FallbackMaps[
            ResolveDefaultMapIndex(SkirmishMapCatalog.FallbackMaps, CountActiveSlots(state))];

        return BuildResult(state, raceIds, map)
            ?? throw new InvalidOperationException("Configuration must include at least one human player.");
    }

    private static int ResolveDefaultRaceIndex(int slotIndex, string[] raceIds)
    {
        string preferred = slotIndex switch
        {
            0 => "terran",
            1 => "korath",
            2 => "vesper",
            3 => "aetherian",
            4 => "nexar",
            5 => "solari",
            6 => "voidborn",
            7 => "cryo",
            _ => RaceShipMeshes.DefaultRace,
        };

        return FindRaceIndex(preferred, raceIds);
    }

    private static int FindRaceIndex(string raceId, string[] raceIds)
    {
        int index = Array.FindIndex(raceIds, id => id.Equals(raceId, StringComparison.OrdinalIgnoreCase));
        return index < 0 ? 0 : index;
    }
}