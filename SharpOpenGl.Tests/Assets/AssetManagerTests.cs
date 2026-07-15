using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.Rendering;
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
            File.Delete(file);
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

    [Fact]
    public void MeshExists_returns_true_for_obj_file()
    {
        string root = CreateTempDir();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "Meshes"));
            File.WriteAllText(Path.Combine(root, "Meshes", "scout_light.obj"), "# stub");

            var manager = new AssetManager(root);
            Assert.True(manager.MeshExists("meshes/scout_light.obj"));
            Assert.False(manager.MeshExists("meshes/missing.obj"));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void MeshExists_returns_true_for_vesper_fighter_on_disk()
    {
        var manager = new AssetManager(GetGameDataPath());
        Assert.True(manager.MeshExists("meshes/ships/vesper/fighter_basic.obj"));
    }

    [Fact]
    public void ResolveMeshPath_uses_manifest_and_disk_casing_for_stripped_keys()
    {
        string root = GetGameDataPath();
        var manifest = MeshManifest.Load(root);
        string path = manifest.ResolveMeshPath(root, "ships/vesper/fighter_basic.obj");
        Assert.Contains($"{Path.DirectorySeparatorChar}Ships{Path.DirectorySeparatorChar}", path);
        Assert.True(File.Exists(path));
    }

    private static string GetGameDataPath()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "SharpOpenGl.sln")))
            dir = Directory.GetParent(dir)?.FullName;

        if (dir == null)
            throw new InvalidOperationException("Could not locate SharpOpenGl.sln from base directory.");

        return Path.Combine(dir, "GameData");
    }
    [Fact]
    public void RegisterProceduralMesh_makes_mesh_available_without_file()
    {
        string root = CreateTempDir();
        try
        {
            var manager = new AssetManager(root);
            manager.RegisterProceduralMesh("meshes/cruiser_heavy.obj");
            Assert.True(manager.MeshExists("meshes/cruiser_heavy.obj"));
        }
        finally { Directory.Delete(root, true); }
    }

    private class SampleAsset
    {
        public string Id { get; set; } = "";
        public int Speed { get; set; }
    }
}