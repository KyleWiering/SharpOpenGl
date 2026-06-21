using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;

namespace SharpOpenGl.Engine.Build;

/// <summary>Runtime view of a build-map entry with affordability and unlock state.</summary>
public sealed class BuildMapEntryView
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string CategoryId { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public int FootprintCols { get; init; } = 1;
    public int FootprintRows { get; init; } = 1;
    public int EnergyCost { get; init; }
    public int MineralsCost { get; init; }
    public int DataCost { get; init; }
    public int CrewCost { get; init; }
    public IReadOnlyList<string> Prerequisites { get; init; } = [];
    public bool IsUnlocked { get; set; }
    public bool CanAfford { get; set; }
    public bool IsSelectable => IsUnlocked && CanAfford;
}

/// <summary>Category grouping for the build-map UI.</summary>
public sealed class BuildMapCategoryView
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public List<BuildMapEntryView> Buildings { get; init; } = [];
}

/// <summary>
/// Loads <c>build_map.json</c> and evaluates unlock/afford rules against world state.
/// </summary>
public sealed class BuildMapCatalog
{
    private readonly BuildMapConfig _config;
    private readonly IReadOnlyDictionary<string, EntityDefinition> _definitions;

    public BuildMapCatalog(BuildMapConfig config, IReadOnlyDictionary<string, EntityDefinition> definitions)
    {
        _config = config;
        _definitions = definitions;
    }

    /// <summary>All categories from the config document.</summary>
    public IReadOnlyList<BuildMapCategoryConfig> Categories => _config.Categories;

    /// <summary>
    /// Returns <c>true</c> when every prerequisite building type exists for the player.
    /// </summary>
    public static bool IsUnlocked(IReadOnlyList<string> prerequisites, IReadOnlySet<string> builtTypes)
    {
        if (prerequisites.Count == 0)
            return true;

        foreach (string required in prerequisites)
        {
            if (!builtTypes.Contains(required))
                return false;
        }

        return true;
    }

    /// <summary>Check whether the player can pay the entity's resource cost.</summary>
    public static bool CanAfford(EntityDefinition def, PlayerResources? player)
    {
        if (player == null) return false;
        return player.GetAmount(ResourceType.Energy) >= (def.Cost?.Energy ?? 0)
            && player.GetAmount(ResourceType.Minerals) >= (def.Cost?.Minerals ?? 0)
            && player.GetAmount(ResourceType.Data) >= (def.Cost?.Data ?? 0)
            && player.GetAmount(ResourceType.Crew) >= (def.Cost?.Crew ?? 0);
    }

    /// <summary>Check supply headroom when the building's crew cost consumes supply slots.</summary>
    public static bool HasSupplyHeadroom(SupplySystem? supply, int playerId, int crewCost)
    {
        if (crewCost <= 0 || supply == null)
            return true;
        return supply.CanAffordSupply(playerId, crewCost);
    }

    /// <summary>Build categorized entries with current unlock and afford flags.</summary>
    public List<BuildMapCategoryView> BuildViews(
        World world,
        int playerId,
        ResourceManager resources,
        SupplySystem? supply)
    {
        var builtTypes = BuildingFootprint.GetBuiltTypes(world, playerId);
        var player = resources.GetPlayer(playerId);
        var views = new List<BuildMapCategoryView>();

        foreach (var category in _config.Categories)
        {
            var categoryView = new BuildMapCategoryView
            {
                Id = category.Id,
                DisplayName = category.DisplayName,
            };

            foreach (var entry in category.Buildings)
            {
                if (!_definitions.TryGetValue(entry.Id, out var def))
                    continue;

                var (cols, rows) = BuildingFootprint.GetSize(def.Components?.Building?.Footprint);
                int crew = def.Cost?.Crew ?? 0;
                bool unlocked = IsUnlocked(entry.Prerequisites, builtTypes);
                bool afford = CanAfford(def, player) && HasSupplyHeadroom(supply, playerId, crew);

                categoryView.Buildings.Add(new BuildMapEntryView
                {
                    Id = entry.Id,
                    Name = def.DisplayName,
                    CategoryId = category.Id,
                    CategoryName = category.DisplayName,
                    FootprintCols = cols,
                    FootprintRows = rows,
                    EnergyCost = def.Cost?.Energy ?? 0,
                    MineralsCost = def.Cost?.Minerals ?? 0,
                    DataCost = def.Cost?.Data ?? 0,
                    CrewCost = crew,
                    Prerequisites = entry.Prerequisites,
                    IsUnlocked = unlocked,
                    CanAfford = afford,
                });
            }

            if (categoryView.Buildings.Count > 0)
                views.Add(categoryView);
        }

        return views;
    }

    /// <summary>Look up prerequisites for a building id.</summary>
    public IReadOnlyList<string> GetPrerequisites(string buildingId)
    {
        foreach (var category in _config.Categories)
        {
            foreach (var entry in category.Buildings)
            {
                if (string.Equals(entry.Id, buildingId, StringComparison.OrdinalIgnoreCase))
                    return entry.Prerequisites;
            }
        }

        return [];
    }
}