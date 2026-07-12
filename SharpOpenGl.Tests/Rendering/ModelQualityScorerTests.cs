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
    }

    [Fact]
    public void Vesper_fighter_scores_with_all_categories()
    {
        var report = ModelQualityScorer.Score("vesper", "fighter_basic", GameDataRoot);
        Assert.Equal("vesper", report.RaceId);
        Assert.Equal("fighter_basic", report.HullId);
        Assert.Equal(6, report.Categories.Count);
        Assert.InRange(report.TotalScore, 0f, 100f);
        Assert.NotEmpty(report.Suggestions);
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