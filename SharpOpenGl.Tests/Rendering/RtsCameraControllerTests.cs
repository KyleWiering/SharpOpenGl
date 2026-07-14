using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class RtsCameraControllerTests
{
    [Fact]
    public void ApplyEdgeScroll_pans_left_when_pointer_near_left_edge()
    {
        var camera = new RtsCameraController
        {
            Target = Vector3.Zero,
            PanSpeed = 100f,
            EdgeScrollMarginPx = 20f,
        };

        bool scrolled = camera.ApplyEdgeScroll(
            new Vector2(5f, 200f),
            new Vector2(800f, 400f),
            deltaTime: 0.1f);

        Assert.True(scrolled);
        Assert.True(camera.Target.X < 0f);
        Assert.Equal(0f, camera.Target.Z);
    }

    [Fact]
    public void ApplyEdgeScroll_is_idle_in_viewport_center()
    {
        var camera = new RtsCameraController { Target = Vector3.Zero };

        bool scrolled = camera.ApplyEdgeScroll(
            new Vector2(400f, 200f),
            new Vector2(800f, 400f),
            deltaTime: 0.1f);

        Assert.False(scrolled);
        Assert.Equal(Vector3.Zero, camera.Target);
    }

    [Fact]
    public void Zoom_clamps_to_min_and_max_height()
    {
        var camera = new RtsCameraController
        {
            Height = 50f,
            MinHeight = 20f,
            MaxHeight = 80f,
            ZoomSpeed = 100f,
        };

        camera.Zoom(10f);
        Assert.Equal(20f, camera.Height);

        camera.Zoom(-10f);
        Assert.Equal(80f, camera.Height);
    }

    [Fact]
    public void ZoomTowardScreenPoint_keeps_cursor_anchor_stable()
    {
        var camera = new RtsCameraController
        {
            Target = Vector3.Zero,
            Height = 100f,
            MinHeight = 20f,
            MaxHeight = 200f,
            ZoomSpeed = 10f,
        };

        Vector2 viewport = new(1024f, 768f);
        Vector2 cursor = new(512f, 384f);
        Vector3 before = ScreenToGround(camera, cursor, viewport);

        camera.ZoomTowardScreenPoint(1f, cursor, viewport);
        Vector3 after = ScreenToGround(camera, cursor, viewport);

        Assert.InRange(after.X, before.X - 0.75f, before.X + 0.75f);
        Assert.InRange(after.Z, before.Z - 0.75f, before.Z + 0.75f);
        Assert.True(camera.Height < 100f);
    }

    [Fact]
    public void NormalizedHeight_maps_between_min_and_max()
    {
        var camera = new RtsCameraController
        {
            MinHeight = 40f,
            MaxHeight = 140f,
            Height = 90f,
        };

        Assert.Equal(0.5f, camera.NormalizedHeight, precision: 3);
    }

    private static Vector3 ScreenToGround(RtsCameraController camera, Vector2 screenPoint, Vector2 viewport)
    {
        float aspect = viewport.X / viewport.Y;
        Matrix4 view = camera.GetViewMatrix();
        Matrix4 projection = camera.GetProjectionMatrix(aspect);
        Matrix4 inv = Matrix4.Invert(view * projection);

        float ndcX = (screenPoint.X / viewport.X) * 2f - 1f;
        float ndcY = 1f - (screenPoint.Y / viewport.Y) * 2f;
        Vector4 near = Vector4.TransformRow(new Vector4(ndcX, ndcY, -1f, 1f), inv);
        Vector4 far = Vector4.TransformRow(new Vector4(ndcX, ndcY, 1f, 1f), inv);
        near /= near.W;
        far /= far.W;

        float t = -near.Y / (far.Y - near.Y);
        return new Vector3(
            near.X + (far.X - near.X) * t,
            0f,
            near.Z + (far.Z - near.Z) * t);
    }
}