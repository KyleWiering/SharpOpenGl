using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

[Collection("ModelMigrationExport")]
public class ModelMigrationExporterTests
{
    private static string GameDataRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "GameData"));

    [Fact]
    public void ExportAll_writes_manifest_and_obj_files()
    {
        RaceVisualSchema.ResetForTests();
        var result = ModelMigrationExporter.ExportAll(GameDataRoot);

        Assert.True(result.Succeeded > 0, $"No models exported. Errors: {string.Join("; ", result.Errors)}");
        Assert.Equal(0, result.Failed);

        string manifestPath = Path.Combine(GameDataRoot, "Config", "mesh_manifest.json");
        Assert.True(File.Exists(manifestPath));

        var manifest = MeshManifest.Load(GameDataRoot);
        Assert.Equal(result.Succeeded, manifest.Entries.Count);

        foreach (var entry in manifest.Entries)
        {
            string path = manifest.ResolveMeshPath(GameDataRoot, entry.Key);
            Assert.True(File.Exists(path), $"Missing: {path}");
            var parsed = ObjMeshLoader.Parse(path);
            Assert.NotNull(parsed);
            Assert.True(parsed!.VertexCount >= 3, entry.Key);
        }
    }

    [Fact]
    public void Export_single_race_ships_match_substrate_catalog()
    {
        RaceVisualSchema.ResetForTests();
        ModelMigrationExporter.ExportAll(GameDataRoot);
        foreach (var entry in RaceSubstrateCatalog.AllEntries().Where(e => e.Kind == RaceSubstrateCatalog.SubstrateKind.Ship))
        {
            string rel = $"Ships/{entry.RaceId}/{entry.ModelId}.obj";
            string path = Path.Combine(GameDataRoot, "Meshes", rel);
            if (!File.Exists(path))
                continue;

            var parsed = ObjMeshLoader.Parse(path);
            Assert.NotNull(parsed);
            int proceduralVerts = ProceduralMeshes.VertexCount(RaceShipMeshes.Build(entry.RaceId, entry.ModelId));
            Assert.Equal(proceduralVerts, parsed!.VertexCount);
        }
    }
}