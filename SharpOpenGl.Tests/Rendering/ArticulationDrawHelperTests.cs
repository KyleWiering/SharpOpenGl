using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class ArticulationDrawHelperTests
{
    private const float Tolerance = 1e-4f;

    [Fact]
    public void TryGetArticulatedModelMatrix_composes_parent_and_part_rotation()
    {
        var world = new World();

        var owner = world.CreateEntity();
        world.AddComponent(owner, new TransformComponent
        {
            Position = new Vector3(10f, 0f, 0f),
            EulerAngles = new Vector3(0f, 30f, 0f),
            Scale = new Vector3(2f, 2f, 2f),
        });

        var partEntity = world.CreateEntity();
        var partTransform = new TransformComponent
        {
            Position = Vector3.Zero,
            Scale = Vector3.One,
        };
        world.AddComponent(partEntity, partTransform);
        world.AddComponent(partEntity, new ArticulatedPartComponent
        {
            Owner = owner,
            LocalPivotOffset = new Vector3(0f, 1f, 0f),
            MeshLocalOffset = new Vector3(0f, 0f, 0.5f),
            CurrentYaw = 45f,
            CurrentPitch = 10f,
        });

        bool composed = ArticulationDrawHelper.TryGetArticulatedModelMatrix(
            world, partEntity, partTransform, out Matrix4 model);

        Assert.True(composed);

        var ownerTransform = world.GetComponent<TransformComponent>(owner)!;
        Matrix4 expected = ArticulationMath.ComposePartModelMatrix(
            ownerTransform.GetModelMatrix(),
            new Vector3(0f, 1f, 0f),
            45f,
            10f,
            new Vector3(0f, 0f, 0.5f),
            ownerTransform.Scale);

        Vector3 expectedOrigin = ArticulationMath.TransformPoint(Vector3.Zero, expected);
        Vector3 actualOrigin = ArticulationMath.TransformPoint(Vector3.Zero, model);

        Assert.Equal(expectedOrigin.X, actualOrigin.X, Tolerance);
        Assert.Equal(expectedOrigin.Y, actualOrigin.Y, Tolerance);
        Assert.Equal(expectedOrigin.Z, actualOrigin.Z, Tolerance);
    }

    [Fact]
    public void TryGetArticulatedModelMatrix_returns_false_for_plain_render_entity()
    {
        var world = new World();

        var entity = world.CreateEntity();
        var transform = new TransformComponent
        {
            Position = new Vector3(3f, 4f, 5f),
            Scale = Vector3.One,
        };
        world.AddComponent(entity, transform);
        world.AddComponent(entity, new RenderComponent());

        bool composed = ArticulationDrawHelper.TryGetArticulatedModelMatrix(
            world, entity, transform, out Matrix4 model);

        Assert.False(composed);
        Assert.Equal(default(Matrix4), model);
    }

    [Fact]
    public void TryGetArticulatedModelMatrix_nested_yaw_pitch_includes_yaw_rotation()
    {
        var world = new World();

        var hull = world.CreateEntity();
        world.AddComponent(hull, new TransformComponent
        {
            Position = Vector3.Zero,
            EulerAngles = Vector3.Zero,
            Scale = Vector3.One,
        });

        var yawEntity = world.CreateEntity();
        world.AddComponent(yawEntity, new ArticulatedPartComponent
        {
            Owner = hull,
            PartType = ArticulatedPartType.TurretYaw,
            LocalPivotOffset = new Vector3(0f, 0.4f, 0.6f),
            CurrentYaw = 90f,
        });

        var pitchEntity = world.CreateEntity();
        var pitchTransform = new TransformComponent { Position = Vector3.Zero, Scale = Vector3.One };
        world.AddComponent(pitchEntity, pitchTransform);
        world.AddComponent(pitchEntity, new ArticulatedPartComponent
        {
            Owner = yawEntity,
            PartType = ArticulatedPartType.TurretPitch,
            LocalPivotOffset = new Vector3(0f, 0.15f, 0.2f),
            CurrentPitch = 20f,
        });

        bool composed = ArticulationDrawHelper.TryGetArticulatedModelMatrix(
            world, pitchEntity, pitchTransform, out Matrix4 model);

        Assert.True(composed);

        Matrix4 hullModel = world.GetComponent<TransformComponent>(hull)!.GetModelMatrix();
        Matrix4 yawModel = ArticulationMath.ComposePartModelMatrix(
            hullModel,
            new Vector3(0f, 0.4f, 0.6f),
            90f,
            0f,
            Vector3.Zero,
            Vector3.One);
        Matrix4 expected = ArticulationMath.ComposePartModelMatrix(
            yawModel,
            new Vector3(0f, 0.15f, 0.2f),
            0f,
            20f,
            Vector3.Zero,
            Vector3.One);

        Vector3 expectedOrigin = ArticulationMath.TransformPoint(Vector3.Zero, expected);
        Vector3 actualOrigin = ArticulationMath.TransformPoint(Vector3.Zero, model);

        Assert.Equal(expectedOrigin.X, actualOrigin.X, Tolerance);
        Assert.Equal(expectedOrigin.Y, actualOrigin.Y, Tolerance);
        Assert.Equal(expectedOrigin.Z, actualOrigin.Z, Tolerance);
    }

    [Fact]
    public void GetVisibilityPosition_uses_owner_position_for_articulated_child()
    {
        var world = new World();

        var owner = world.CreateEntity();
        world.AddComponent(owner, new TransformComponent
        {
            Position = new Vector3(12f, 3f, -8f),
        });

        var partEntity = world.CreateEntity();
        world.AddComponent(partEntity, new TransformComponent
        {
            Position = Vector3.Zero,
        });
        world.AddComponent(partEntity, new ArticulatedPartComponent
        {
            Owner = owner,
        });

        Vector3 visibility = ArticulationDrawHelper.GetVisibilityPosition(
            world, partEntity, Vector3.Zero);

        Assert.Equal(12f, visibility.X, Tolerance);
        Assert.Equal(3f, visibility.Y, Tolerance);
        Assert.Equal(-8f, visibility.Z, Tolerance);
    }
}