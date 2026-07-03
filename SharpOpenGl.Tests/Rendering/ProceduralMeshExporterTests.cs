using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class ProceduralMeshExporterTests
{
    [Fact]
    public void WriteObj_round_trips_through_loader()
    {
        RaceVisualSchema.ResetForTests();
        float[] mesh = RaceShipMeshes.Build("terran", "fighter_basic");
        string path = Path.Combine(Path.GetTempPath(), $"fighter_export_{Guid.NewGuid():N}.obj");

        try
        {
            ProceduralMeshExporter.WriteObj(mesh, path, "fighter_basic");
            var data = ObjMeshLoader.Parse(path);
            Assert.NotNull(data);
            Assert.Equal(ProceduralMeshes.VertexCount(mesh), data!.VertexCount);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void MeshManifest_resolves_ship_key()
    {
        var manifest = new MeshManifest(
        [
            new MeshManifestEntry
            {
                Key = "meshes/ships/terran/fighter_basic.obj",
                Category = "ship",
                RaceId = "terran",
                ModelId = "fighter_basic",
                RelativePath = "Ships/terran/fighter_basic.obj",
            },
        ]);

        Assert.True(manifest.TryResolve("ship", "terran", "fighter_basic", out var entry));
        Assert.Equal("fighter_basic", entry!.ModelId);
    }
}