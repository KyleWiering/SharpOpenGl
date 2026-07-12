using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class ComponentTextureIndexTests
{
    [Theory]
    [InlineData("engine", 0)]
    [InlineData("engines", 0)]
    [InlineData("engine_trail", 0)]
    [InlineData("weapon", 1)]
    [InlineData("weapons", 1)]
    [InlineData("projectile", 1)]
    [InlineData("shield", 2)]
    [InlineData("shield_generator", 2)]
    [InlineData("shieldgenerator", 2)]
    public void Resolve_maps_known_names(string name, int expected)
    {
        Assert.Equal(expected, ComponentTextureIndex.Resolve(name));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("hull")]
    public void Resolve_returns_negative_one_for_unknown(string? name)
    {
        Assert.Equal(-1, ComponentTextureIndex.Resolve(name));
    }

    [Fact]
    public void BuildShieldGenerator_returns_valid_mesh()
    {
        float[] mesh = ProceduralMeshes.BuildShieldGenerator(new Vector3(0.55f, 0.82f, 0.95f));
        Assert.NotEmpty(mesh);
        Assert.Equal(0, mesh.Length % ProceduralMeshes.Stride);
        Assert.True(ProceduralMeshes.VertexCount(mesh) >= 12);
    }
}