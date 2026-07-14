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

    [Fact]
    public void Terran_export_count_matches_catalog()
    {
        var manifest = MeshManifest.Load(GameDataRoot);
        var terranEntries = manifest.Entries
            .Where(e => string.Equals(e.RaceId, "terran", StringComparison.OrdinalIgnoreCase))
            .ToList();

        int ships = terranEntries.Count(e => e.Category == "ship");
        int designs = terranEntries.Count(e => e.Category == "design");
        Assert.Equal(19, ships);
        Assert.Equal(63, designs);
        Assert.Equal(82, ships + designs);
        Assert.Equal(ShipDesignCatalog.CountForRace("terran"), designs);
    }

    [Fact]
    public void Terran_manifest_entries_resolve_and_parse()
    {
        var manifest = MeshManifest.Load(GameDataRoot);
        var terranEntries = manifest.Entries
            .Where(e => string.Equals(e.RaceId, "terran", StringComparison.OrdinalIgnoreCase)
                        && (e.Category == "ship" || e.Category == "design"))
            .ToList();

        Assert.Equal(82, terranEntries.Count);

        foreach (var entry in terranEntries)
        {
            Assert.Equal("terran", entry.RaceId);
            Assert.Equal(Path.GetFileNameWithoutExtension(entry.RelativePath), entry.ModelId);

            string expectedRelative = entry.Category == "ship"
                ? $"Ships/terran/{entry.ModelId}.obj"
                : $"Designs/terran/{entry.ModelId}.obj";
            Assert.Equal(expectedRelative, entry.RelativePath.Replace('\\', '/'), ignoreCase: true);

            string path = manifest.ResolveMeshPath(GameDataRoot, entry.Key);
            Assert.True(File.Exists(path), entry.Key);

            var parsed = ObjMeshLoader.Parse(path);
            Assert.NotNull(parsed);
            Assert.True(parsed!.VertexCount >= 3, entry.Key);
        }
    }

    [Theory]
    [MemberData(nameof(TerranShipHullIds))]
    public void Terran_ship_obj_vertex_counts_match_procedural(string hullId)
    {
        RaceVisualSchema.ResetForTests();
        var manifest = MeshManifest.Load(GameDataRoot);
        string key = MeshManifest.ShipKey("terran", hullId);
        string path = manifest.ResolveMeshPath(GameDataRoot, key);

        Assert.True(File.Exists(path), key);
        var parsed = ObjMeshLoader.Parse(path);
        Assert.NotNull(parsed);

        int proceduralVerts = ProceduralMeshes.VertexCount(RaceShipMeshes.Build("terran", hullId));
        Assert.Equal(proceduralVerts, parsed!.VertexCount);
    }

    [Theory]
    [InlineData("terran_fighter_mk1_01")]
    [InlineData("terran_destroyer_mk2_25")]
    [InlineData("terran_carrier_mk3_44")]
    public void Terran_design_tier_spot_check_vertex_parity(string designId)
    {
        RaceVisualSchema.ResetForTests();
        var manifest = MeshManifest.Load(GameDataRoot);
        string key = MeshManifest.DesignKey("terran", designId);
        string path = manifest.ResolveMeshPath(GameDataRoot, key);

        Assert.True(File.Exists(path), key);
        var parsed = ObjMeshLoader.Parse(path);
        Assert.NotNull(parsed);

        var design = ShipDesignCatalog.GetById(designId);
        Assert.Equal("terran", design.RaceId);

        int proceduralVerts = ProceduralMeshes.VertexCount(RaceShipMeshes.BuildDesign(design));
        Assert.Equal(proceduralVerts, parsed!.VertexCount);
        Assert.True(parsed.VertexCount >= 3);
        Assert.Equal(0, parsed.VertexCount % 3);
    }

    public static IEnumerable<object[]> TerranShipHullIds() =>
        FleetGalleryLayout.AllShipIds.Select(id => new object[] { id });
}