using System.Text.Json;

namespace SharpOpenGl.Engine.Rendering;

public static partial class ModelQualityScorer
{
    public sealed record RaceAssetScore(
        string AssetKind,
        string ModelId,
        float TotalScore,
        float RaceIdentityScore);

    public sealed record RaceQualityReport(
        string RaceId,
        string DisplayName,
        float OverallScore,
        float ShipFleetScore,
        float StationFleetScore,
        float RaceIdentityAverage,
        int ShipCount,
        int StationCount,
        IReadOnlyList<RaceAssetScore> Assets,
        IReadOnlyList<string> WeakestAssets,
        IReadOnlyList<string> Suggestions)
    {
        public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

        public static RaceQualityReport FromJson(string json) =>
            JsonSerializer.Deserialize<RaceQualityReport>(json, JsonOptions)
            ?? throw new InvalidOperationException("Invalid race score JSON.");
    }

    public static void WriteRaceReport(string path, RaceQualityReport report)
        => File.WriteAllText(path, report.ToJson());

    /// <summary>Scores every ship and station for one of the eight playable races.</summary>
    public static RaceQualityReport ScoreRace(string raceId, string gameDataRoot)
    {
        RaceVisualSchema.ResetForTests();
        RaceVisualSchema.Load();

        var assets = new List<RaceAssetScore>();
        foreach (string shipId in FleetGalleryLayout.AllShipIds)
        {
            var report = ScoreAsset(ModelMeshSource.KindShip, shipId, gameDataRoot, raceId);
            assets.Add(new RaceAssetScore(ModelMeshSource.KindShip, shipId, report.TotalScore, report.RaceIdentityScore));
        }

        foreach (string stationId in FleetGalleryLayout.AllBaseIds)
        {
            var report = ScoreAsset(ModelMeshSource.KindStation, stationId, gameDataRoot, raceId);
            assets.Add(new RaceAssetScore(ModelMeshSource.KindStation, stationId, report.TotalScore, report.RaceIdentityScore));
        }

        return BuildRaceReport(raceId, assets);
    }

    /// <summary>
    /// Builds a fleet audit from per-asset score JSON files written by mesh-loop capture+score
    /// (e.g. model-improvement/&lt;race&gt;/&lt;model&gt;/scores/loop-01.json).
    /// </summary>
    public static RaceQualityReport AggregateRaceFromScoreDirectory(
        string raceId,
        string repoRoot,
        string scoreFileName = "loop-01.json")
    {
        RaceVisualSchema.ResetForTests();
        RaceVisualSchema.Load();

        var assets = new List<RaceAssetScore>();
        foreach (string shipId in FleetGalleryLayout.AllShipIds)
        {
            assets.Add(ReadRaceAssetScore(repoRoot, raceId, ModelMeshSource.KindShip, shipId, scoreFileName));
        }

        foreach (string stationId in FleetGalleryLayout.AllBaseIds)
        {
            assets.Add(ReadRaceAssetScore(repoRoot, raceId, ModelMeshSource.KindStation, stationId, scoreFileName));
        }

        return BuildRaceReport(raceId, assets);
    }

    private static RaceAssetScore ReadRaceAssetScore(
        string repoRoot,
        string raceId,
        string assetKind,
        string modelId,
        string scoreFileName)
    {
        string scorePath = Path.Combine(
            repoRoot,
            "model-improvement",
            raceId,
            modelId,
            "scores",
            scoreFileName);

        if (!File.Exists(scorePath))
            throw new FileNotFoundException($"Missing score file for {raceId}/{modelId}: {scorePath}");

        var report = ModelQualityReport.FromJson(File.ReadAllText(scorePath));
        return new RaceAssetScore(assetKind, modelId, report.TotalScore, report.RaceIdentityScore);
    }

    private static RaceQualityReport BuildRaceReport(string raceId, IReadOnlyList<RaceAssetScore> assets)
    {
        RaceVisualSchema.TryGetRace(raceId, out var race);
        race ??= RaceVisualSchema.AllRaces.FirstOrDefault(r => r.Id == raceId)
            ?? RaceVisualSchema.AllRaces[0];

        float shipSum = 0f, stationSum = 0f, identitySum = 0f;
        int shipCount = 0, stationCount = 0;

        foreach (var asset in assets)
        {
            if (asset.AssetKind == ModelMeshSource.KindShip)
            {
                shipSum += asset.TotalScore;
                shipCount++;
            }
            else if (asset.AssetKind == ModelMeshSource.KindStation)
            {
                stationSum += asset.TotalScore;
                stationCount++;
            }

            identitySum += asset.RaceIdentityScore;
        }

        float shipFleet = shipCount > 0 ? shipSum / shipCount : 0f;
        float stationFleet = stationCount > 0 ? stationSum / stationCount : 0f;
        float overall = shipCount + stationCount > 0
            ? (shipSum + stationSum) / (shipCount + stationCount)
            : 0f;
        float identityAvg = shipCount + stationCount > 0
            ? identitySum / (shipCount + stationCount)
            : 0f;

        var weakest = assets
            .OrderBy(a => a.TotalScore)
            .Take(5)
            .Select(a => $"{a.AssetKind}/{a.ModelId} ({a.TotalScore:F1})")
            .ToList();

        var suggestions = BuildRaceSuggestions(race, assets, shipFleet, stationFleet, identityAvg);

        return new RaceQualityReport(
            raceId,
            race.DisplayName,
            overall,
            shipFleet,
            stationFleet,
            identityAvg,
            shipCount,
            stationCount,
            assets,
            weakest,
            suggestions);
    }

    /// <summary>Scores all eight races; returns reports sorted by overall score descending.</summary>
    public static IReadOnlyList<RaceQualityReport> ScoreAllRaces(string gameDataRoot)
    {
        RaceVisualSchema.ResetForTests();
        RaceVisualSchema.Load();
        return RaceVisualSchema.AllRaces
            .Select(r => ScoreRace(r.Id, gameDataRoot))
            .OrderByDescending(r => r.OverallScore)
            .ToList();
    }

    private static List<string> BuildRaceSuggestions(
        RaceVisualDefinition race,
        IReadOnlyList<RaceAssetScore> assets,
        float shipFleet,
        float stationFleet,
        float identityAvg)
    {
        var suggestions = new List<string>();

        if (identityAvg < 7f)
            suggestions.Add($"Strengthen {race.DisplayName} identity: push palette primary/secondary/accent into hull bands and station superstructure (style={race.Style}).");

        if (shipFleet < stationFleet - 5f)
            suggestions.Add("Fleet ships lag behind stations — prioritize ship silhouettes and race texture contrast across hull classes.");
        else if (stationFleet < shipFleet - 5f)
            suggestions.Add("Stations lag behind ships — add landmark massing, pad segmentation, and vertical kit-bash for base readability.");

        var weakShips = assets
            .Where(a => a.AssetKind == ModelMeshSource.KindShip && a.TotalScore < shipFleet - 8f)
            .Take(3)
            .Select(a => a.ModelId);
        if (weakShips.Any())
            suggestions.Add($"Weakest hull outliers: {string.Join(", ", weakShips)} — bring up to fleet average.");

        var weakStations = assets
            .Where(a => a.AssetKind == ModelMeshSource.KindStation && a.TotalScore < stationFleet - 8f)
            .Take(3)
            .Select(a => a.ModelId);
        if (weakStations.Any())
            suggestions.Add($"Weakest station outliers: {string.Join(", ", weakStations)} — improve pad silhouette and baked shadow separation.");

        if (suggestions.Count == 0)
            suggestions.Add($"Polish {race.DisplayName} micro-variation: accent tips, engine glow wells, and substrate grit so all 8 races stay visually distinct.");

        return suggestions;
    }
}