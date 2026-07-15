using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.ECS;

public class ArticulationMathTests
{
    private const float Tolerance = 1e-4f;

    [Fact]
    public void ComposePartModelMatrix_pivot_offset_translates_child_origin()
    {
        Matrix4 parent = Matrix4.CreateTranslation(new Vector3(5f, 0f, 0f));
        Vector3 pivot = new Vector3(2f, 1f, -3f);
        Vector3 ownerScale = Vector3.One;

        Matrix4 composed = ArticulationMath.ComposePartModelMatrix(
            parent,
            pivot,
            yawDegrees: 0f,
            pitchDegrees: 0f,
            meshLocalOffset: Vector3.Zero,
            ownerScale);

        Vector3 meshOrigin = ArticulationMath.TransformPoint(Vector3.Zero, composed);

        Assert.Equal(7f, meshOrigin.X, Tolerance);
        Assert.Equal(1f, meshOrigin.Y, Tolerance);
        Assert.Equal(-3f, meshOrigin.Z, Tolerance);
    }

    [Fact]
    public void ComposePartModelMatrix_yaw_only_rotates_around_pivot()
    {
        Matrix4 parent = Matrix4.CreateTranslation(new Vector3(10f, 0f, 0f));
        Vector3 pivot = new Vector3(0f, 2f, 0f);
        Vector3 meshOffset = new Vector3(0f, 0f, 1f);
        Vector3 ownerScale = Vector3.One;

        Matrix4 yaw0 = ArticulationMath.ComposePartModelMatrix(
            parent, pivot, 0f, 0f, meshOffset, ownerScale);
        Matrix4 yaw90 = ArticulationMath.ComposePartModelMatrix(
            parent, pivot, 90f, 0f, meshOffset, ownerScale);

        Vector3 pivotWorld0 = ArticulationMath.ComputePivotWorld(parent, pivot, ownerScale);
        Vector3 pivotWorld90 = ArticulationMath.ComputePivotWorld(parent, pivot, ownerScale);

        Assert.Equal(pivotWorld0.X, pivotWorld90.X, Tolerance);
        Assert.Equal(pivotWorld0.Y, pivotWorld90.Y, Tolerance);
        Assert.Equal(pivotWorld0.Z, pivotWorld90.Z, Tolerance);

        Vector3 meshOrigin0 = ArticulationMath.TransformPoint(Vector3.Zero, yaw0);
        Vector3 meshOrigin90 = ArticulationMath.TransformPoint(Vector3.Zero, yaw90);

        Assert.NotEqual(meshOrigin0.X, meshOrigin90.X, Tolerance);
        Assert.NotEqual(meshOrigin0.Z, meshOrigin90.Z, Tolerance);
    }

    [Fact]
    public void ClampAngles_clamps_yaw_and_pitch_to_limits()
    {
        var (yaw, pitch) = ArticulationMath.ClampAngles(
            yaw: -120f,
            pitch: 75f,
            yawMin: -45f,
            yawMax: 45f,
            pitchMin: -10f,
            pitchMax: 30f);

        Assert.Equal(-45f, yaw, Tolerance);
        Assert.Equal(30f, pitch, Tolerance);
    }

    [Fact]
    public void ClampAngles_symmetric_limits()
    {
        var (negYaw, negPitch) = ArticulationMath.ClampAngles(
            yaw: -200f,
            pitch: -80f,
            yawMin: -90f,
            yawMax: 90f,
            pitchMin: -45f,
            pitchMax: 45f);

        Assert.Equal(-90f, negYaw, Tolerance);
        Assert.Equal(-45f, negPitch, Tolerance);

        var (posYaw, posPitch) = ArticulationMath.ClampAngles(
            yaw: 200f,
            pitch: 80f,
            yawMin: -90f,
            yawMax: 90f,
            pitchMin: -45f,
            pitchMax: 45f);

        Assert.Equal(90f, posYaw, Tolerance);
        Assert.Equal(45f, posPitch, Tolerance);
    }

    [Fact]
    public void ComputeAimAngles_world_to_local_yaw_pitch()
    {
        var ownerTransform = new TransformComponent
        {
            Position = Vector3.Zero,
            EulerAngles = new Vector3(0f, 90f, 0f),
            Scale = Vector3.One,
        };

        Vector3 pivotWorld = Vector3.Zero;
        Vector3 targetWorld = new Vector3(10f, 0f, 0f);
        Vector3 ownerForward = ComputeOwnerForward(ownerTransform);

        var (yaw, pitch) = ArticulationMath.ComputeAimAngles(
            pivotWorld,
            targetWorld,
            ownerForward,
            yawMin: -180f,
            yawMax: 180f,
            pitchMin: -45f,
            pitchMax: 45f);

        Assert.Equal(0f, yaw, Tolerance);
        Assert.Equal(0f, pitch, Tolerance);

        ownerTransform.EulerAngles = new Vector3(0f, 0f, 0f);
        ownerForward = ComputeOwnerForward(ownerTransform);

        var (yawUnrotated, _) = ArticulationMath.ComputeAimAngles(
            Vector3.Zero,
            new Vector3(0f, 0f, -10f),
            ownerForward,
            yawMin: -180f,
            yawMax: 180f,
            pitchMin: -45f,
            pitchMax: 45f);

        Assert.Equal(0f, yawUnrotated, Tolerance);

        var (yawRotated, _) = ArticulationMath.ComputeAimAngles(
            Vector3.Zero,
            new Vector3(0f, 0f, -10f),
            ComputeOwnerForward(new TransformComponent { EulerAngles = new Vector3(0f, 90f, 0f) }),
            yawMin: -180f,
            yawMax: 180f,
            pitchMin: -45f,
            pitchMax: 45f);

        Assert.Equal(90f, MathF.Abs(yawRotated), Tolerance);
    }

    [Fact]
    public void ComposePartModelMatrix_matches_transform_component_convention()
    {
        var ownerTransform = new TransformComponent
        {
            Position = new Vector3(3f, 1f, -2f),
            EulerAngles = new Vector3(5f, 30f, -10f),
            Scale = new Vector3(1.2f, 1.5f, 0.9f),
        };

        Vector3 pivot = new Vector3(0.4f, 0.6f, -0.2f);
        float yawDegrees = 25f;
        float pitchDegrees = -12f;
        Vector3 meshOffset = new Vector3(0f, 0.1f, 0.3f);

        Matrix4 composed = ArticulationMath.ComposePartModelMatrix(
            ownerTransform.GetModelMatrix(),
            pivot,
            yawDegrees,
            pitchDegrees,
            meshOffset,
            ownerTransform.Scale);

        Vector3 scaledPivot = new Vector3(
            pivot.X * ownerTransform.Scale.X,
            pivot.Y * ownerTransform.Scale.Y,
            pivot.Z * ownerTransform.Scale.Z);

        Matrix4 manual = ownerTransform.GetModelMatrix()
            * Matrix4.CreateTranslation(scaledPivot)
            * Matrix4.CreateRotationY(MathHelper.DegreesToRadians(yawDegrees))
            * Matrix4.CreateRotationX(MathHelper.DegreesToRadians(pitchDegrees))
            * Matrix4.CreateTranslation(meshOffset)
            * Matrix4.CreateScale(ownerTransform.Scale);

        for (int row = 0; row < 4; row++)
        {
            Assert.Equal(manual.Row0[row], composed.Row0[row], Tolerance);
            Assert.Equal(manual.Row1[row], composed.Row1[row], Tolerance);
            Assert.Equal(manual.Row2[row], composed.Row2[row], Tolerance);
            Assert.Equal(manual.Row3[row], composed.Row3[row], Tolerance);
        }
    }

    [Fact]
    public void ComputeAimAngles_returns_clamped_angles_for_offset_target()
    {
        Vector3 pivot = Vector3.Zero;
        Vector3 target = new Vector3(1f, 1f, -1f);
        Vector3 ownerForward = -Vector3.UnitZ;

        var (yaw, pitch) = ArticulationMath.ComputeAimAngles(
            pivot,
            target,
            ownerForward,
            yawMin: -90f,
            yawMax: 90f,
            pitchMin: -45f,
            pitchMax: 45f);

        Assert.Equal(45f, yaw, Tolerance);
        Assert.Equal(35.2643897f, pitch, Tolerance);
    }

    [Fact]
    public void ComputeAimAngles_respects_pitch_limits_at_extreme_elevation()
    {
        Vector3 pivot = Vector3.Zero;
        Vector3 target = new Vector3(0f, 10f, -0.1f);
        Vector3 ownerForward = -Vector3.UnitZ;

        var (yaw, pitch) = ArticulationMath.ComputeAimAngles(
            pivot,
            target,
            ownerForward,
            yawMin: -180f,
            yawMax: 180f,
            pitchMin: -10f,
            pitchMax: 45f);

        Assert.Equal(0f, yaw, Tolerance);
        Assert.Equal(45f, pitch, precision: 2);
    }

    private static Vector3 ComputeOwnerForward(TransformComponent transform)
    {
        float yawRad = MathHelper.DegreesToRadians(transform.EulerAngles.Y);
        return Vector3.Normalize(new Vector3(MathF.Sin(yawRad), 0f, -MathF.Cos(yawRad)));
    }
}