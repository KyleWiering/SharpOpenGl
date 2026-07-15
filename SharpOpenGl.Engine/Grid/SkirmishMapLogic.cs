namespace SharpOpenGl.Engine.Grid;

/// <summary>Validation and spawn resolution for multiplayer skirmish maps.</summary>
public static class SkirmishMapLogic
{
    public const int GridExtent = MapCoordinates.DefaultGridExtent;

    public static readonly string[] DefaultStarterBuildingIds = ["command_center"];

    /// <summary>Tier-1 structures the Restoration Tender may place in skirmish/campaign builder flow.</summary>
    public static readonly string[] BuilderTier1BuildingIds =
    [
        "power_reactor",
        "resource_refinery",
        "supply_depot",
        "sensor_array",
        "defense_turret",
    ];

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

        errors.AddRange(ValidateEconomy(map));
        errors.AddRange(ValidateFairness(map));

        return errors;
    }

    /// <summary>Validates skirmish spawn parity: base sizes, home economy, and flank symmetry.</summary>
    public static IReadOnlyList<string> ValidateFairness(MapDefinition map)
    {
        var errors = new List<string>();

        if (!map.Skirmish || map.SpawnPoints.Length < 2)
            return errors;

        int gridExtent = MapCoordinates.ResolveGridExtent(map);
        var baseSizes = new List<(int Width, int Height)>();

        foreach (var spawn in map.SpawnPoints)
        {
            if (!TryParseBaseArea(spawn.BaseArea, out int minX, out int minY, out int maxX, out int maxY))
                continue;

            baseSizes.Add((maxX - minX + 1, maxY - minY + 1));

            if (spawn.Position is { Length: >= 2 })
            {
                int x = spawn.Position[0];
                int y = spawn.Position[1];
                if (x < minX || x > maxX || y < minY || y > maxY)
                {
                    errors.Add(
                        $"spawnPoints (player {spawn.Player}): position [{x}, {y}] must lie inside baseArea [{minX}, {minY}, {maxX}, {maxY}].");
                }
            }
        }

        if (baseSizes.Count > 1)
        {
            var distinct = baseSizes.Distinct().ToArray();
            if (distinct.Length > 1)
            {
                errors.Add(
                    $"spawn baseArea sizes must match across players (found {distinct.Length} distinct sizes: " +
                    $"{string.Join(", ", distinct.Select(s => $"{s.Width}x{s.Height}"))}).");
            }
        }

        errors.AddRange(ValidateStarterOffsetParity(map));
        errors.AddRange(ValidateHomeResourceParity(map));
        errors.AddRange(ValidateTerrainSymmetry(map, gridExtent));

        return errors;
    }

    /// <summary>True when every active spawn has identical starter building offsets.</summary>
    public static bool HasUniformStarterOffsets(MapDefinition map)
    {
        if (map.SpawnPoints.Length == 0)
            return true;

        string CanonicalOffset(MapSpawnPoint spawn)
        {
            if (spawn.StarterBuildings.Length == 0)
                return string.Join("|", DefaultStarterOffsets.Select(o => $"{o.X},{o.Y}"));

            return string.Join("|", spawn.StarterBuildings.Select(s =>
            {
                int ox = s.Offset is { Length: > 0 } ? s.Offset[0] : 0;
                int oy = s.Offset is { Length: > 1 } ? s.Offset[1] : 0;
                return $"{s.Id}:{ox},{oy}";
            }));
        }

        string expected = CanonicalOffset(map.SpawnPoints[0]);
        return map.SpawnPoints.All(sp => CanonicalOffset(sp) == expected);
    }

    /// <summary>Counts resource nodes whose position lies inside a spawn base area.</summary>
    public static int CountHomeResources(MapSpawnPoint spawn, MapResourceNode[] nodes)
    {
        if (!TryParseBaseArea(spawn.BaseArea, out int minX, out int minY, out int maxX, out int maxY))
            return 0;

        return nodes.Count(node =>
            node.Position is { Length: >= 2 } &&
            node.Position[0] >= minX && node.Position[0] <= maxX &&
            node.Position[1] >= minY && node.Position[1] <= maxY);
    }

    /// <summary>Validates that a map has sufficient mineable economy sources.</summary>
    public static IReadOnlyList<string> ValidateEconomy(MapDefinition map)
    {
        var errors = new List<string>();

        bool hasHarvestablePlanet = map.MapFeatures.Any(f =>
            f.Kind.Equals("harvestable_planet", StringComparison.OrdinalIgnoreCase));
        bool hasMineable = map.ResourceNodes.Length > 0 || hasHarvestablePlanet;

        if (map.Skirmish)
        {
            if (map.ResourceNodes.Length < 4 && !hasHarvestablePlanet)
                errors.Add("skirmish maps must have at least 4 resourceNodes or a harvestable_planet mapFeature.");
        }
        else if (!hasMineable)
        {
            errors.Add("campaign maps must have at least one mineable source (resourceNodes or harvestable_planet).");
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

    private static IReadOnlyList<string> ValidateStarterOffsetParity(MapDefinition map)
    {
        if (HasUniformStarterOffsets(map))
            return [];

        return ["starterBuildings offsets must match across all spawnPoints."];
    }

    private static IReadOnlyList<string> ValidateHomeResourceParity(MapDefinition map)
    {
        var errors = new List<string>();
        var homeTotals = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

        foreach (var spawn in map.SpawnPoints)
        {
            if (!TryParseBaseArea(spawn.BaseArea, out int minX, out int minY, out int maxX, out int maxY))
                continue;

            string key = $"player {spawn.Player}";
            var byType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var node in map.ResourceNodes)
            {
                if (node.Position is not { Length: >= 2 })
                    continue;

                int x = node.Position[0];
                int y = node.Position[1];
                if (x < minX || x > maxX || y < minY || y > maxY)
                    continue;

                string type = string.IsNullOrWhiteSpace(node.Type) ? "unknown" : node.Type;
                byType.TryGetValue(type, out int total);
                byType[type] = total + node.Amount;
            }

            homeTotals[key] = byType;
        }

        if (homeTotals.Count < 2)
            return errors;

        int homeCounts = map.SpawnPoints
            .Select(sp => CountHomeResources(sp, map.ResourceNodes))
            .Distinct()
            .Count();

        if (homeCounts > 1)
        {
            var detail = string.Join(", ", map.SpawnPoints.Select(sp =>
                $"player {sp.Player}={CountHomeResources(sp, map.ResourceNodes)}"));
            errors.Add($"home resourceNodes per spawn must match (found: {detail}).");
        }

        var reference = homeTotals.Values.First();
        foreach (var (player, totals) in homeTotals.Skip(1))
        {
            var refTypes = reference.Keys.OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToArray();
            var playerTypes = totals.Keys.OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToArray();

            if (!refTypes.SequenceEqual(playerTypes, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add($"{player} home resource types [{string.Join(", ", playerTypes)}] must match player 1 [{string.Join(", ", refTypes)}].");
                continue;
            }

            foreach (string type in refTypes)
            {
                if (reference[type] != totals[type])
                {
                    errors.Add(
                        $"{player} home {type} total ({totals[type]}) must match player 1 ({reference[type]}).");
                }
            }
        }

        return errors;
    }

    private static IReadOnlyList<string> ValidateTerrainSymmetry(MapDefinition map, int gridExtent)
    {
        if (map.PlayerCount != 2 || map.Terrain?.Regions == null)
            return [];

        var errors = new List<string>();
        var rects = map.Terrain.Regions
            .Where(r => r.Rect is { Length: >= 4 })
            .Select(r => (
                Type: r.Type ?? "space",
                MinX: r.Rect![0],
                MinY: r.Rect[1],
                MaxX: r.Rect[2],
                MaxY: r.Rect[3]))
            .Where(r => !SpansCenterColumn(r.MinX, r.MaxX, gridExtent))
            .ToList();

        foreach (var region in rects)
        {
            var mirror = MirrorRectHorizontally(region.MinX, region.MinY, region.MaxX, region.MaxY, gridExtent);
            bool hasMirror = rects.Any(other =>
                other.Type.Equals(region.Type, StringComparison.OrdinalIgnoreCase) &&
                other.MinX == mirror.MinX &&
                other.MinY == mirror.MinY &&
                other.MaxX == mirror.MaxX &&
                other.MaxY == mirror.MaxY);

            if (!hasMirror)
            {
                errors.Add(
                    $"terrain region '{region.Type}' rect [{region.MinX}, {region.MinY}, {region.MaxX}, {region.MaxY}] " +
                    $"must have a horizontal mirror [{mirror.MinX}, {mirror.MinY}, {mirror.MaxX}, {mirror.MaxY}] with the same type.");
            }
        }

        return errors;
    }

    private static bool SpansCenterColumn(int minX, int maxX, int gridExtent)
    {
        int center = gridExtent / 2;
        return minX <= center && maxX >= center;
    }

    private static (int MinX, int MinY, int MaxX, int MaxY) MirrorRectHorizontally(
        int minX, int minY, int maxX, int maxY, int gridExtent) =>
        (gridExtent - maxX, minY, gridExtent - minX, maxY);
}