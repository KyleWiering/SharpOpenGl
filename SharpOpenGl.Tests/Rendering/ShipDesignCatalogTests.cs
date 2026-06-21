using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class ShipDesignCatalogTests
{
    [Fact]
    public void Catalog_contains_exactly_500_designs()
    {
        Assert.Equal(500, ShipDesignCatalog.TotalDesigns);
        Assert.Equal(500, ShipDesignCatalog.All.Count);
    }

    [Fact]
    public void Each_race_has_62_or_63_designs()
    {
        foreach (var race in RaceVisualSchema.AllRaces)
        {
            int count = ShipDesignCatalog.CountForRace(race.Id);
            Assert.InRange(count, 62, 63);
        }

        Assert.Equal(500, RaceVisualSchema.AllRaces.Sum(r => ShipDesignCatalog.CountForRace(r.Id)));
    }

    [Fact]
    public void All_design_ids_are_unique()
    {
        var ids = ShipDesignCatalog.All.Select(d => d.DesignId).ToList();
        Assert.Equal(500, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Every_hull_class_has_multiple_variants_per_race()
    {
        string[] hulls =
        [
            "scout", "fighter", "interceptor", "drone", "corvette", "frigate", "gunship",
            "bomber", "destroyer", "cruiser", "carrier", "dreadnought", "miner",
            "transport", "freighter", "support", "hero",
        ];

        foreach (var race in RaceVisualSchema.AllRaces)
        {
            foreach (string hull in hulls)
            {
                var variants = ShipDesignCatalog.GetByRaceAndHull(race.Id, hull);
                Assert.True(variants.Count >= 3, $"{race.Id}/{hull} has only {variants.Count} variants");
            }
        }
    }

    [Fact]
    public void All_500_designs_produce_distinct_mesh_geometry()
    {
        var signatures = new HashSet<string>();
        foreach (var design in ShipDesignCatalog.All)
        {
            float[] mesh = RaceShipMeshes.BuildDesign(design);
            string sig = MeshSignature(mesh);
            Assert.True(signatures.Add(sig), $"Duplicate mesh for design {design.DesignId}");
        }

        Assert.Equal(500, signatures.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(127)]
    [InlineData(499)]
    public void BuildDesign_by_index_returns_valid_mesh(int index)
    {
        float[] mesh = RaceShipMeshes.BuildDesign(index);
        Assert.NotEmpty(mesh);
        Assert.Equal(0, mesh.Length % ProceduralMeshes.Stride);
        Assert.True(ProceduralMeshes.VertexCount(mesh) >= 15);
    }

    [Fact]
    public void Resolve_picks_stable_design_for_definition_and_race()
    {
        var a = ShipDesignCatalog.Resolve("fighter_basic", "terran");
        var b = ShipDesignCatalog.Resolve("fighter_basic", "terran");
        Assert.Equal(a.DesignId, b.DesignId);
    }

    private static string MeshSignature(float[] mesh)
    {
        float sum = 0f;
        int limit = Math.Min(mesh.Length, 120);
        for (int i = 0; i < limit; i++)
            sum += mesh[i];
        return $"{mesh.Length}:{ProceduralMeshes.VertexCount(mesh)}:{sum:F4}";
    }
}
