using SharpOpenGl.Engine.Assets;
using Xunit;

namespace SharpOpenGl.Tests.Assets;

public class AssetManagerTests
{
    private static string CreateTempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void Load_returns_null_for_missing_asset()
    {
        string root = CreateTempDir();
        try
        {
            var manager = new AssetManager(root);
            var result = manager.Load<SampleAsset>("Ships/nonexistent");
            Assert.Null(result);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void Load_deserializes_existing_asset()
    {
        string root = CreateTempDir();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "Ships"));
            File.WriteAllText(
                Path.Combine(root, "Ships", "test_ship.json"),
                """{"id":"test_ship","speed":100}""");

            var manager = new AssetManager(root);
            var result = manager.Load<SampleAsset>("Ships/test_ship");
            Assert.NotNull(result);
            Assert.Equal("test_ship", result!.Id);
            Assert.Equal(100, result.Speed);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void Load_caches_after_first_access()
    {
        string root = CreateTempDir();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "Ships"));
            string file = Path.Combine(root, "Ships", "cached.json");
            File.WriteAllText(file, """{"id":"cached","speed":5}""");

            var manager = new AssetManager(root);
            var first = manager.Load<SampleAsset>("Ships/cached");
            File.Delete(file); // delete original to prove cache is used
            var second = manager.Load<SampleAsset>("Ships/cached");

            Assert.Same(first, second);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void Exists_returns_correct_bool()
    {
        string root = CreateTempDir();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "Ships"));
            File.WriteAllText(Path.Combine(root, "Ships", "exists.json"), "{}");

            var manager = new AssetManager(root);
            Assert.True(manager.Exists("Ships/exists"));
            Assert.False(manager.Exists("Ships/missing"));
        }
        finally { Directory.Delete(root, true); }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private class SampleAsset
    {
        public string Id { get; set; } = "";
        public int Speed { get; set; }
    }
}
