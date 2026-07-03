using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class ModelMigrationProofTests
{
    private static string GameDataRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "GameData"));

    [Theory]
    [MemberData(nameof(AllManifestEntries))]
    public void Manifest_entry_exists_and_parses(MeshManifestEntry entry)
    {
        var manifest = MeshManifest.Load(GameDataRoot);
        string path = manifest.ResolveMeshPath(GameDataRoot, entry.Key);
        Assert.True(File.Exists(path), $"Missing file for {entry.Key}");
        var data = ObjMeshLoader.Parse(path);
        Assert.NotNull(data);
        Assert.True(data!.VertexCount >= 3, entry.Key);
    }

    [Theory]
    [InlineData("terran")]
    [InlineData("vesper")]
    [InlineData("korath")]
    [InlineData("aetherian")]
    [InlineData("nexar")]
    [InlineData("solari")]
    [InlineData("voidborn")]
    [InlineData("cryo")]
    public void Race_shard_all_ships_and_stations_present(string raceId)
    {
        var manifest = MeshManifest.Load(GameDataRoot);
        foreach (string shipId in FleetGalleryLayout.AllShipIds)
        {
            string key = MeshManifest.ShipKey(raceId, shipId);
            Assert.True(manifest.TryGetByKey(key, out _), key);
            string path = manifest.ResolveMeshPath(GameDataRoot, key);
            Assert.True(File.Exists(path));
            Assert.NotNull(ObjMeshLoader.Parse(path));
        }

        foreach (string baseId in FleetGalleryLayout.AllBaseIds)
        {
            string key = MeshManifest.StationKey(raceId, baseId);
            Assert.True(manifest.TryGetByKey(key, out _), key);
            string path = manifest.ResolveMeshPath(GameDataRoot, key);
            Assert.True(File.Exists(path));
            Assert.NotNull(ObjMeshLoader.Parse(path));
        }
    }

    [Fact]
    public void Ship_designer_mesh_keys_resolve_for_all_race_hull_pairs()
    {
        var service = new MeshAssetService(GameDataRoot);
        foreach (string raceId in RaceTextureIndex.AllRaceIds)
        {
            foreach (string shipId in FleetGalleryLayout.AllShipIds)
            {
                Assert.True(service.TryResolveShip(raceId, shipId, out string key, out string path), $"{raceId}/{shipId}");
                Assert.True(File.Exists(path), path);
            }
        }
    }

    public static IEnumerable<object[]> AllManifestEntries() =>
        MeshManifest.Load(GameDataRoot).Entries.Select(e => new object[] { e });
}