using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class ProceduralMeshesTests
{
    [Fact]
    public void BuildAsteroidFieldCluster_returns_non_empty_mesh()
    {
        float[] mesh = ProceduralMeshes.BuildAsteroidFieldCluster(new Vector3(0.5f, 0.48f, 0.44f));
        Assert.NotEmpty(mesh);
        Assert.Equal(0, mesh.Length % ProceduralMeshes.Stride);
        Assert.True(ProceduralMeshes.VertexCount(mesh) > ProceduralMeshes.VertexCount(
            ProceduralMeshes.BuildSceneryCluster(new Vector3(0.5f, 0.52f, 0.55f))));
    }

    [Fact]
    public void BuildNebulaCloud_returns_non_empty_mesh_with_vibrant_colors()
    {
        float[] mesh = ProceduralMeshes.BuildNebulaCloud();
        Assert.NotEmpty(mesh);
        Assert.Equal(0, mesh.Length % ProceduralMeshes.Stride);

        bool hasPurple = false;
        bool hasCyan = false;
        for (int i = 3; i < mesh.Length; i += ProceduralMeshes.Stride)
        {
            float r = mesh[i];
            float g = mesh[i + 1];
            float b = mesh[i + 2];
            if (r > 0.35f && b > 0.55f && g < 0.45f)
                hasPurple = true;
            if (g > 0.55f && b > 0.85f)
                hasCyan = true;
        }

        Assert.True(hasPurple);
        Assert.True(hasCyan);
    }

    [Fact]
    public void Asteroid_field_and_nebula_meshes_are_distinct()
    {
        float[] asteroid = ProceduralMeshes.BuildAsteroidFieldCluster(new Vector3(0.52f, 0.48f, 0.44f));
        float[] nebula = ProceduralMeshes.BuildNebulaCloud();
        float[] debris = ProceduralMeshes.BuildSceneryCluster(new Vector3(0.5f, 0.52f, 0.55f));

        Assert.NotEqual(ProceduralMeshes.VertexCount(asteroid), ProceduralMeshes.VertexCount(nebula));
        Assert.NotEqual(ProceduralMeshes.VertexCount(asteroid), ProceduralMeshes.VertexCount(debris));
        Assert.NotEqual(VertexColorSignature(asteroid), VertexColorSignature(nebula));
    }

    private static string VertexColorSignature(float[] mesh)
    {
        double sum = 0;
        for (int i = 3; i < mesh.Length; i += ProceduralMeshes.Stride)
            sum += mesh[i] + mesh[i + 1] + mesh[i + 2];
        return $"{ProceduralMeshes.VertexCount(mesh)}:{sum:F4}";
    }
}