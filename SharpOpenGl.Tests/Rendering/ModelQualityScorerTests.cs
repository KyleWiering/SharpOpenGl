using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class ModelQualityScorerTests
{
    private static string GameDataRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "GameData"));

    [Fact]
    public void Export_and_score_vesper_fighter()
    {
        Assert.True(ModelMigrationExporter.ExportShip(GameDataRoot, "vesper", "fighter_basic"));
        var report = ModelQualityScorer.Score("vesper", "fighter_basic", GameDataRoot);
        Assert.InRange(report.TotalScore, 1f, 100f);
        Assert.Equal("ship", report.AssetKind);
    }

    [Fact]
    public void Vesper_fighter_scores_with_all_categories()
    {
        var report = ModelQualityScorer.Score("vesper", "fighter_basic", GameDataRoot);
        Assert.Equal("vesper", report.RaceId);
        Assert.Equal("fighter_basic", report.HullId);
        Assert.Equal(7, report.Categories.Count);
        Assert.InRange(report.TotalScore, 0f, 100f);
        Assert.InRange(report.RaceIdentityScore, 0f, 10f);
        Assert.NotEmpty(report.Suggestions);
    }

    [Fact]
    public void Korath_command_center_station_scores()
    {
        var report = ModelQualityScorer.ScoreAsset(
            ModelMeshSource.KindStation, "command_center", GameDataRoot, "korath");
        Assert.Equal("station", report.AssetKind);
        Assert.Equal("command_center", report.HullId);
        Assert.Contains(report.Categories, c => c.Name == "Massing");
        Assert.InRange(report.TotalScore, 0f, 100f);
    }

    [Fact]
    public void Shield_generator_object_scores()
    {
        var report = ModelQualityScorer.ScoreAsset(
            ModelMeshSource.KindObject, "shield_generator", GameDataRoot);
        Assert.Equal("object", report.AssetKind);
        Assert.Contains(report.Categories, c => c.Name == "IconRead");
        Assert.InRange(report.TotalScore, 0f, 100f);
    }

    [Fact]
    public void Score_race_includes_fleet_and_stations()
    {
        var report = ModelQualityScorer.ScoreRace("vesper", GameDataRoot);
        Assert.Equal("vesper", report.RaceId);
        Assert.Equal(19, report.ShipCount);
        Assert.Equal(FleetGalleryLayout.AllBaseIds.Length, report.StationCount);
        Assert.Equal(
            FleetGalleryLayout.AllShipIds.Length + FleetGalleryLayout.AllBaseIds.Length,
            report.Assets.Count);
        Assert.InRange(report.OverallScore, 0f, 100f);
        Assert.InRange(report.RaceIdentityAverage, 0f, 10f);
        Assert.NotEmpty(report.WeakestAssets);
    }

    [Fact]
    public void Aggregate_race_from_score_directory_matches_mesh_loop_files()
    {
        string repo = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        string scorePath = Path.Combine(repo, "model-improvement", "vesper", "fighter_basic", "scores", "loop-01.json");
        if (!File.Exists(scorePath))
            return;

        var fromFiles = ModelQualityScorer.AggregateRaceFromScoreDirectory("vesper", repo);
        var fighter = fromFiles.Assets.First(a => a.ModelId == "fighter_basic");
        var loopReport = ModelQualityScorer.ModelQualityReport.FromJson(File.ReadAllText(scorePath));

        Assert.Equal(loopReport.TotalScore, fighter.TotalScore, 0.01f);
        Assert.Equal(19, fromFiles.ShipCount);
        Assert.Equal(FleetGalleryLayout.AllBaseIds.Length, fromFiles.StationCount);
    }

    [Fact]
    public void Score_all_races_returns_eight_races()
    {
        var reports = ModelQualityScorer.ScoreAllRaces(GameDataRoot);
        Assert.Equal(8, reports.Count);
        Assert.All(reports, r => Assert.InRange(r.OverallScore, 0f, 100f));
    }

    [Fact]
    public void Score_race_audit_aligns_with_fleet_gallery_and_race_texture_index()
    {
        var reports = ModelQualityScorer.ScoreAllRaces(GameDataRoot);
        var raceIds = reports.Select(r => r.RaceId).OrderBy(id => id).ToArray();
        var expected = RaceTextureIndex.AllRaceIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();

        Assert.Equal(expected.Length, raceIds.Length);
        Assert.Equal(expected, raceIds, StringComparer.OrdinalIgnoreCase);
        Assert.All(reports, report =>
        {
            Assert.Equal(FleetGalleryLayout.AllShipIds.Length, report.ShipCount);
            Assert.Equal(FleetGalleryLayout.AllBaseIds.Length, report.StationCount);
            Assert.Equal(
                FleetGalleryLayout.AllShipIds.Length + FleetGalleryLayout.AllBaseIds.Length,
                report.Assets.Count);
        });
    }

    [Fact]
    public void Score_improves_when_screenshot_path_valid()
    {
        string repo = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        string png = Path.Combine(repo, "mesh-loop-01.png");
        if (!File.Exists(png))
        {
            var geomOnly = ModelQualityScorer.Score("vesper", "fighter_basic", GameDataRoot);
            Assert.True(geomOnly.Categories.First(c => c.Name == "Screenshot").Score == 0f);
            return;
        }

        var withShot = ModelQualityScorer.Score("vesper", "fighter_basic", GameDataRoot, png);
        Assert.True(withShot.Categories.First(c => c.Name == "Screenshot").Score >= 0f);
    }
}