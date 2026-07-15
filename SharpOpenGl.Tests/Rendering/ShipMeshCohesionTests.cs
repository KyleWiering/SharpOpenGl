using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class ShipMeshCohesionTests
{
    public ShipMeshCohesionTests() => RaceVisualSchema.ResetForTests();

    [Theory]
    [InlineData("terran", "fighter_basic", 1.58f, 1.35f)]
    [InlineData("vesper", "destroyer_assault", 1.15f, 1.25f)]
    [InlineData("korath", "cruiser_heavy", 1.65f, 1.65f)]
    public void Race_ships_keep_geometry_within_hull_envelope(
        string raceId, string hullId, float maxWidthFactor, float maxHeightFactor)
    {
        float[] mesh = RaceShipMeshes.Build(raceId, hullId);
        var (len, wid, hgt) = ResolveHullDimensions(raceId, hullId);
        var bounds = MeshBounds.From(mesh);

        Assert.InRange(bounds.MaxAbsX, 0f, wid * maxWidthFactor);
        Assert.InRange(bounds.MaxY, 0f, hgt * maxHeightFactor);
        Assert.InRange(bounds.MinZ, -len * 1.15f, len * 0.2f);
        Assert.InRange(bounds.MaxZ, -len * 0.1f, len * 1.1f);
    }

    [Fact]
    public void All_catalog_designs_build_nonempty_meshes()
    {
        foreach (var design in ShipDesignCatalog.All)
        {
            float[] mesh = RaceShipMeshes.BuildDesign(design);
            Assert.NotEmpty(mesh);
            Assert.Equal(0, mesh.Length % ProceduralMeshes.Stride);
            Assert.True(ProceduralMeshes.VertexCount(mesh) >= 12);
        }
    }

    private static (float len, float wid, float hgt) ResolveHullDimensions(string raceId, string hullId)
    {
        var design = ShipDesignCatalog.Resolve(hullId, raceId);
        var hull = RaceVisualSchema.ResolveHullProfile(design.HullClass);
        var variant = ShipDesignVariant.FromSpec(design);
        RaceVisualSchema.TryGetRace(raceId, out var race);
        race ??= RaceVisualSchema.AllRaces[0];

        float s = hull.Size * variant.LengthScale;
        float len = s * hull.LengthRatio * race.Modifiers.HullLength * variant.LengthScale;
        float wid = s * hull.WidthRatio * race.Modifiers.HullWidth * variant.WidthScale;
        float hgt = s * hull.HeightRatio * variant.HeightScale;
        len += s * variant.NoseExtension * 0.5f;
        len += s * variant.SternExtension * 0.35f;
        return (len, wid, hgt);
    }

    private readonly struct MeshBounds
    {
        public float MaxAbsX { get; init; }
        public float MaxY { get; init; }
        public float MaxZ { get; init; }
        public float MinZ { get; init; }

        public static MeshBounds From(float[] mesh)
        {
            float maxAbsX = 0f, maxY = float.MinValue, maxZ = float.MinValue, minZ = float.MaxValue;
            for (int i = 0; i < mesh.Length; i += ProceduralMeshes.Stride)
            {
                float x = MathF.Abs(mesh[i]);
                float y = mesh[i + 1];
                float z = mesh[i + 2];
                if (x > maxAbsX) maxAbsX = x;
                if (y > maxY) maxY = y;
                if (z > maxZ) maxZ = z;
                if (z < minZ) minZ = z;
            }

            return new MeshBounds { MaxAbsX = maxAbsX, MaxY = maxY, MaxZ = maxZ, MinZ = minZ };
        }
    }
}