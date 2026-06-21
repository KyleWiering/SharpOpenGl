using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class GridRenderLodTests
{
    [Theory]
    [InlineData(40f, 1)]
    [InlineData(120f, 1)]
    [InlineData(300f, 2)]
    [InlineData(600f, 5)]
    [InlineData(1200f, 20)]
    [InlineData(1500f, 20)]
    public void ResolveLineStep_increases_with_camera_height(float height, int expectedStep)
    {
        Assert.Equal(expectedStep, GridRenderLod.ResolveLineStep(height, 40f, 1500f));
    }

    [Fact]
    public void BuildGrid_with_larger_step_uses_fewer_vertices()
    {
        var color = new Vector3(0.1f, 0.1f, 0.1f);
        int full = ProceduralMeshes.BuildGrid(200, 200, 10f, color, 1).Length;
        int sparse = ProceduralMeshes.BuildGrid(200, 200, 10f, color, 10).Length;

        Assert.True(sparse < full / 5);
    }

    [Fact]
    public void PanByScreenDelta_drag_down_moves_target_negative_z()
    {
        var camera = new RtsCameraController
        {
            Height = 80f,
            Target = Vector3.Zero,
        };

        camera.PanByScreenDelta(new Vector2(0f, 20f), new Vector2(1024f, 768f));

        Assert.True(camera.Target.Z < 0f);
    }
}