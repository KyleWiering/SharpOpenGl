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
    public float BuildTime { get; init; }
    public IReadOnlyList<string> Prerequisites { get; init; } = [];
    /// <summary>Prerequisites satisfied out of <see cref="PrerequisiteTotalCount"/>.</summary>
    public int PrerequisiteMetCount { get; init; }
    /// <summary>Total prerequisite structures required before unlock.</summary>
    public int PrerequisiteTotalCount { get; init; }
    public string? LockReason { get; init; }
    public string? AffordReason { get; init; }
    public bool IsUnlocked { get; set; }
    public bool CanAfford { get; set; }
    public BuildIconDescriptor Icon { get; init; }
    public bool IsSelectable => IsUnlocked && CanAfford;
}

/// <summary>Category grouping for the build-map UI.</summary>
public sealed class BuildMapCategoryView
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    /// <summary>1-based tier index from category order in <c>build_map.json</c>.</summary>
    public int TierIndex { get; init; }
    public int UnlockedCount { get; set; }
    public int TotalCount { get; set; }
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
        int tierIndex = 0;

        foreach (var category in _config.Categories)
        {
            tierIndex++;
            var categoryView = new BuildMapCategoryView
            {
                Id = category.Id,
                DisplayName = category.DisplayName,
                TierIndex = tierIndex,
            };

            foreach (var entry in category.Buildings)
            {
                if (!_definitions.TryGetValue(entry.Id, out var def))
                    continue;

                var (cols, rows) = BuildingFootprint.GetSize(def.Components?.Building?.Footprint);
                int crew = def.Cost?.Crew ?? 0;
                int prereqTotal = entry.Prerequisites.Count;
                int prereqMet = CountMetPrerequisites(entry.Prerequisites, builtTypes);
                bool unlocked = prereqTotal == 0 || prereqMet == prereqTotal;
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
                    BuildTime = def.BuildTime,
                    Prerequisites = FormatPrerequisiteNames(entry.Prerequisites),
                    PrerequisiteMetCount = prereqMet,
                    PrerequisiteTotalCount = prereqTotal,
                    LockReason = unlocked ? null : FormatLockReason(entry.Prerequisites, builtTypes),
                    AffordReason = unlocked && !afford
                        ? FormatAffordReason(def, player, supply, playerId)
                        : null,
                    IsUnlocked = unlocked,
                    CanAfford = afford,
                    Icon = BuildIconCatalog.Get(entry.Id),
                });
            }

            if (categoryView.Buildings.Count > 0)
            {
                categoryView.TotalCount = categoryView.Buildings.Count;
                categoryView.UnlockedCount = categoryView.Buildings.Count(static b => b.IsUnlocked);
                views.Add(categoryView);
            }
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

    private IReadOnlyList<string> FormatPrerequisiteNames(IReadOnlyList<string> prerequisiteIds)
    {
        if (prerequisiteIds.Count == 0)
            return [];

        var names = new List<string>(prerequisiteIds.Count);
        foreach (string id in prerequisiteIds)
            names.Add(ResolveDisplayName(id));
        return names;
    }

    private string? FormatLockReason(IReadOnlyList<string> prerequisiteIds, IReadOnlySet<string> builtTypes)
    {
        var missing = new List<string>();
        foreach (string required in prerequisiteIds)
        {
            if (!builtTypes.Contains(required))
                missing.Add(ResolveDisplayName(required));
        }

        if (missing.Count == 0)
            return null;

        return $"Requires: {string.Join(", ", missing)}";
    }

    private static string? FormatAffordReason(
        EntityDefinition def,
        PlayerResources? player,
        SupplySystem? supply,
        int playerId)
    {
        var shortages = new List<string>();
        int energy = def.Cost?.Energy ?? 0;
        int minerals = def.Cost?.Minerals ?? 0;
        int data = def.Cost?.Data ?? 0;
        int crew = def.Cost?.Crew ?? 0;

        if (player != null)
        {
            if (energy > 0 && player.GetAmount(ResourceType.Energy) < energy)
                shortages.Add("energy");
            if (minerals > 0 && player.GetAmount(ResourceType.Minerals) < minerals)
                shortages.Add("minerals");
            if (data > 0 && player.GetAmount(ResourceType.Data) < data)
                shortages.Add("data");
            if (crew > 0 && player.GetAmount(ResourceType.Crew) < crew)
                shortages.Add("crew");
        }
        else if (energy > 0 || minerals > 0 || data > 0 || crew > 0)
        {
            shortages.Add("resources");
        }

        if (!HasSupplyHeadroom(supply, playerId, crew))
            shortages.Add("supply");

        if (shortages.Count == 0)
            return null;

        return $"Insufficient: {string.Join(", ", shortages)}";
    }

    private static int CountMetPrerequisites(
        IReadOnlyList<string> prerequisiteIds, IReadOnlySet<string> builtTypes)
    {
        if (prerequisiteIds.Count == 0)
            return 0;

        int met = 0;
        foreach (string required in prerequisiteIds)
        {
            if (builtTypes.Contains(required))
                met++;
        }

        return met;
    }

    private string ResolveDisplayName(string id) =>
        _definitions.TryGetValue(id, out var def) ? def.DisplayName : id;
}