namespace SharpOpenGl.Engine.Grid;

/// <summary>Validation and spawn resolution for multiplayer skirmish maps.</summary>
public static class SkirmishMapLogic
{
    public const int GridExtent = MapCoordinates.DefaultGridExtent;

    public static readonly string[] DefaultStarterBuildingIds = ["command_center"];

    public static readonly (int X, int Y)[] DefaultStarterOffsets =
    [
        (4, 6),
    ];

    /// <summary>Campaign sandbox floor — human skirmish starting energy.</summary>
    public const float SkirmishStartingEnergy = 4500f;

    /// <summary>Campaign sandbox floor — human skirmish starting minerals.</summary>
    public const float SkirmishStartingMinerals = 5500f;

    /// <summary>Campaign sandbox floor — human skirmish starting data.</summary>
    public const float SkirmishStartingData = 700f;

    /// <summary>Campaign sandbox floor — human skirmish starting crew.</summary>
    public const float SkirmishStartingCrew = 55f;

    /// <summary>Ordered spawn points for active factions (player 1..N).</summary>
    public static IReadOnlyList<MapSpawnPoint> ResolveActiveSpawns(MapDefinition map, int activePlayerCount)
    {
        if (map.SpawnPoints.Length == 0 || activePlayerCount <= 0)
            return [];

        int count = Math.Min(activePlayerCount, map.SpawnPoints.Length);
        return map.SpawnPoints
            .OrderBy(sp => sp.Player)
            .Take(count)
            .ToArray();
    }

    /// <summary>Grid cell positions for starter buildings inside a spawn base area.</summary>
    public static IReadOnlyList<(string BuildingId, int GridX, int GridY)> ResolveStarterPlacements(
        MapSpawnPoint spawn)
    {
        if (!TryParseBaseArea(spawn.BaseArea, out int minX, out int minY, out int maxX, out int maxY))
            return [];

        var placements = new List<(string, int, int)>();

        if (spawn.StarterBuildings.Length > 0)
        {
            foreach (var starter in spawn.StarterBuildings)
            {
                if (string.IsNullOrWhiteSpace(starter.Id)) continue;

                int offsetX = starter.Offset is { Length: > 0 } ? starter.Offset[0] : 0;
                int offsetY = starter.Offset is { Length: > 1 } ? starter.Offset[1] : 0;
                int gridX = minX + offsetX;
                int gridY = minY + offsetY;

                if (gridX < minX || gridX > maxX || gridY < minY || gridY > maxY)
                    continue;

                placements.Add((starter.Id, gridX, gridY));
            }

            return placements;
        }

        for (int i = 0; i < DefaultStarterBuildingIds.Length; i++)
        {
            var (offsetX, offsetY) = DefaultStarterOffsets[i];
            int gridX = minX + offsetX;
            int gridY = minY + offsetY;

            if (gridX > maxX || gridY > maxY)
                continue;

            placements.Add((DefaultStarterBuildingIds[i], gridX, gridY));
        }

        return placements;
    }

    /// <summary>Validates skirmish map schema and returns human-readable errors.</summary>
    public static IReadOnlyList<string> Validate(MapDefinition map)
    {
        var errors = new List<string>();

        if (!map.Skirmish)
            errors.Add("skirmish flag must be true.");

        if (map.PlayerCount < 2)
            errors.Add("playerCount must be at least 2.");

        if (map.SpawnPoints.Length < map.PlayerCount)
            errors.Add($"spawnPoints length ({map.SpawnPoints.Length}) must be >= playerCount ({map.PlayerCount}).");

        if (string.IsNullOrWhiteSpace(map.Id))
            errors.Add("id is required.");

        if (string.IsNullOrWhiteSpace(map.DisplayName))
            errors.Add("displayName is required.");

        for (int i = 0; i < map.SpawnPoints.Length; i++)
        {
            var spawn = map.SpawnPoints[i];
            string label = $"spawnPoints[{i}] (player {spawn.Player})";

            if (spawn.Position is not { Length: >= 2 })
            {
                errors.Add($"{label}: position must be [x, y].");
                continue;
            }

            if (!IsWithinGrid(spawn.Position[0], spawn.Position[1]))
                errors.Add($"{label}: position [{spawn.Position[0]}, {spawn.Position[1]}] is outside 0..{GridExtent - 1}.");

            if (!TryParseBaseArea(spawn.BaseArea, out int minX, out int minY, out int maxX, out int maxY))
            {
                errors.Add($"{label}: baseArea must be [minX, minY, maxX, maxY] with min <= max.");
                continue;
            }

            if (!IsRectWithinGrid(minX, minY, maxX, maxY))
                errors.Add($"{label}: baseArea [{minX}, {minY}, {maxX}, {maxY}] is outside grid bounds.");

            var placements = ResolveStarterPlacements(spawn);
            if (placements.Count == 0)
            {
                errors.Add($"{label}: no valid starterBuildings placements inside baseArea.");
                continue;
            }

            bool hasCommandCenter = placements.Any(p =>
                p.BuildingId.Equals("command_center", StringComparison.OrdinalIgnoreCase));

            if (!hasCommandCenter)
                errors.Add($"{label}: starterBuildings must include command_center.");
        }

        return errors;
    }

    /// <summary>True when the map supports the requested number of active lobby players.</summary>
    public static bool SupportsPlayerCount(MapDefinition map, int activePlayerCount) =>
        map.Skirmish &&
        map.PlayerCount >= activePlayerCount &&
        map.SpawnPoints.Length >= activePlayerCount &&
        Validate(map).Count == 0;

    public static bool TryParseBaseArea(int[]? area, out int minX, out int minY, out int maxX, out int maxY)
    {
        minX = minY = maxX = maxY = 0;
        if (area is not { Length: >= 4 }) return false;

        minX = area[0];
        minY = area[1];
        maxX = area[2];
        maxY = area[3];

        return minX <= maxX && minY <= maxY;
    }

    private static bool IsWithinGrid(int x, int y) =>
        x >= 0 && x < GridExtent && y >= 0 && y < GridExtent;

    private static bool IsRectWithinGrid(int minX, int minY, int maxX, int maxY) =>
        IsWithinGrid(minX, minY) && IsWithinGrid(maxX, maxY);
}