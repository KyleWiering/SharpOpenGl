using OpenTK.Mathematics;
using SharpOpenGl.Engine.Input;
using Xunit;

namespace SharpOpenGl.Tests.Input;

public class GroundPlaneRaycasterTests
{
    [Fact]
    public void ScreenToGround_center_of_viewport_hits_target_on_Y0_plane()
    {
        var viewport = new Vector2(1024f, 768f);
        var projection = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(45f), viewport.X / viewport.Y, 0.1f, 10000f);
        var view = Matrix4.LookAt(new Vector3(0f, 80f, 80f), Vector3.Zero, Vector3.UnitY);

        Vector3? hit = GroundPlaneRaycaster.ScreenToGround(
            new Vector2(viewport.X / 2f, viewport.Y / 2f),
            viewport, projection, view);

        Assert.NotNull(hit);
        Assert.Equal(0f, hit!.Value.Y, 0.5f);
        Assert.True(hit.Value.Length < 200f);
    }

    [Fact]
    public void ScreenToGround_returns_null_for_zero_viewport()
    {
        var projection = Matrix4.Identity;
        var view = Matrix4.Identity;
        Assert.Null(GroundPlaneRaycaster.ScreenToGround(Vector2.Zero, Vector2.Zero, projection, view));
    }
}