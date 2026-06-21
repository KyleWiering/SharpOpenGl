namespace SharpOpenGl.Engine.Rendering;

/// <summary>One of 500 procedural ship silhouettes — race + hull + variant tier + seed.</summary>
public sealed class ShipDesignSpec
{
    public int Index { get; init; }
    public string DesignId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string RaceId { get; init; } = string.Empty;
    public string HullClass { get; init; } = string.Empty;
    public string VariantTag { get; init; } = string.Empty;
    public int Tier { get; init; }
    public int Seed { get; init; }
    public bool IsSpecial { get; init; }
}

/// <summary>
/// Catalog of 500 ship designs: 8 races × 62–63 variants each.
/// 51 standard (17 hulls × mk1/mk2/mk3) + 11–12 special archetypes per race.
/// </summary>
public static class ShipDesignCatalog
{
    public const int TotalDesigns = 500;

    private static readonly string[] HullOrder =
    [
        "scout", "fighter", "interceptor", "drone", "corvette", "frigate", "gunship",
        "bomber", "destroyer", "cruiser", "carrier", "dreadnought", "miner",
        "transport", "freighter", "support", "hero",
    ];

    private static readonly string[] MkTiers = ["mk1", "mk2", "mk3"];

    private static readonly string[] SpecialArchetypes =
    [
        "strike", "patrol", "elite", "veteran", "prototype", "ace",
        "phantom", "bulwark", "spear", "storm", "eclipse", "sovereign",
    ];

    private static readonly ShipDesignSpec[] Designs = Generate();
    private static readonly Dictionary<string, ShipDesignSpec> ById = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, List<ShipDesignSpec>> ByRaceHull = new(StringComparer.OrdinalIgnoreCase);

    static ShipDesignCatalog()
    {
        RaceVisualSchema.Load();
        foreach (var d in Designs)
        {
            ById[d.DesignId] = d;
            string key = Key(d.RaceId, d.HullClass);
            if (!ByRaceHull.TryGetValue(key, out List<ShipDesignSpec>? list))
            {
                list = [];
                ByRaceHull[key] = list;
            }
            list.Add(d);
        }
    }

    public static IReadOnlyList<ShipDesignSpec> All => Designs;

    public static ShipDesignSpec Get(int index)
    {
        if (index < 0 || index >= TotalDesigns)
            index = Math.Clamp(index, 0, TotalDesigns - 1);
        return Designs[index];
    }

    public static bool TryGetById(string designId, out ShipDesignSpec spec)
        => ById.TryGetValue(designId, out spec!);

    public static ShipDesignSpec GetById(string designId)
        => TryGetById(designId, out ShipDesignSpec? spec) ? spec : Designs[0];

    public static IReadOnlyList<ShipDesignSpec> GetByRace(string raceId)
        => Designs.Where(d => d.RaceId.Equals(raceId, StringComparison.OrdinalIgnoreCase)).ToArray();

    public static IReadOnlyList<ShipDesignSpec> GetByRaceAndHull(string raceId, string hullClass)
    {
        string hull = RaceVisualSchema.ResolveHullKey(hullClass);
        return ByRaceHull.TryGetValue(Key(raceId, hull), out List<ShipDesignSpec>? list)
            ? list
            : Array.Empty<ShipDesignSpec>();
    }

    /// <summary>Pick a catalog design for a gameplay definition + faction race.</summary>
    public static ShipDesignSpec Resolve(string definitionId, string raceId)
    {
        string hull = RaceVisualSchema.ResolveHullKey(definitionId);
        var candidates = GetByRaceAndHull(raceId, hull);
        if (candidates.Count == 0)
            return Designs.First(d => d.RaceId.Equals(raceId, StringComparison.OrdinalIgnoreCase));

        int hash = StableHash($"{definitionId}:{raceId}");
        return candidates[Math.Abs(hash) % candidates.Count];
    }

    public static ShipDesignSpec ResolveForEnemy(string definitionId)
        => Resolve(definitionId, RaceVisualSchema.RaceFromSeed(definitionId));

    public static int CountForRace(string raceId)
        => Designs.Count(d => d.RaceId.Equals(raceId, StringComparison.OrdinalIgnoreCase));

    private static ShipDesignSpec[] Generate()
    {
        RaceVisualSchema.Load();
        var races = RaceVisualSchema.AllRaces.Select(r => r.Id).ToArray();
        if (races.Length == 0)
            races = [RaceShipMeshes.DefaultRace, "vesper", "korath", "aetherian", "nexar", "solari", "voidborn", "cryo"];

        var result = new ShipDesignSpec[TotalDesigns];
        for (int i = 0; i < TotalDesigns; i++)
        {
            int raceIndex = i % races.Length;
            int slot = i / races.Length;
            string raceId = races[raceIndex];
            int maxSlot = MaxSlotForRace(raceIndex, races.Length);
            if (slot > maxSlot) slot = maxSlot;

            string hull;
            string variantTag;
            int tier;
            bool isSpecial;

            if (slot < 51)
            {
                int hullIndex = slot % HullOrder.Length;
                tier = slot / HullOrder.Length;
                hull = HullOrder[hullIndex];
                variantTag = MkTiers[Math.Clamp(tier, 0, MkTiers.Length - 1)];
                isSpecial = false;
            }
            else
            {
                int specialIndex = slot - 51;
                int archetypeCap = maxSlot >= 62 ? SpecialArchetypes.Length : SpecialArchetypes.Length - 1;
                int archetypeIndex = specialIndex % archetypeCap;
                hull = HullOrder[(specialIndex + raceIndex) % HullOrder.Length];
                variantTag = SpecialArchetypes[archetypeIndex];
                tier = 3 + archetypeIndex;
                isSpecial = true;
            }

            string designId = $"{raceId}_{hull}_{variantTag}_{slot:D2}";
            int seed = StableHash(designId);
            string displayName = FormatDisplayName(raceId, hull, variantTag, slot);

            result[i] = new ShipDesignSpec
            {
                Index = i,
                DesignId = designId,
                DisplayName = displayName,
                RaceId = raceId,
                HullClass = hull,
                VariantTag = variantTag,
                Tier = tier,
                Seed = seed,
                IsSpecial = isSpecial,
            };
        }

        return result;
    }

    private static int MaxSlotForRace(int raceIndex, int raceCount)
        => raceIndex < 4 ? 62 : 61;

    private static string FormatDisplayName(string raceId, string hull, string tag, int slot)
    {
        string raceName = RaceVisualSchema.TryGetRace(raceId, out var race)
            ? race.DisplayName
            : raceId;
        string hullTitle = char.ToUpper(hull[0]) + hull[1..];
        return $"{raceName} {hullTitle} {tag.ToUpperInvariant()}-{slot:D2}";
    }

    private static string Key(string raceId, string hullClass)
        => $"{raceId}/{hullClass}";

    private static int StableHash(string text)
    {
        int hash = 17;
        foreach (char c in text)
            hash = hash * 31 + c;
        return hash;
    }
}