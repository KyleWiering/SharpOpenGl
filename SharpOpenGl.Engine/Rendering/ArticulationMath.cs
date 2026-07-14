using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Shared math for articulated sub-parts: aim-angle extraction, clamping, and model-matrix composition.
/// Matrix order matches OpenTK row-vector convention on <see cref="ECS.TransformComponent.GetModelMatrix"/>.
/// </summary>
public static class ArticulationMath
{
    private const float Epsilon = 1e-6f;

    /// <summary>
    /// Computes yaw (Y) and pitch (X) degrees to aim from <paramref name="pivotWorld"/> toward
    /// <paramref name="targetWorld"/>, relative to <paramref name="ownerForward"/>, then clamps to limits.
    /// </summary>
    public static (float yaw, float pitch) ComputeAimAngles(
        Vector3 pivotWorld,
        Vector3 targetWorld,
        Vector3 ownerForward,
        float yawMin,
        float yawMax,
        float pitchMin,
        float pitchMax)
    {
        Vector3 toTarget = targetWorld - pivotWorld;
        if (toTarget.LengthSquared < Epsilon * Epsilon)
            return ClampAngles(0f, 0f, yawMin, yawMax, pitchMin, pitchMax);

        toTarget = Vector3.Normalize(toTarget);

        Vector3 forward = ownerForward.LengthSquared < Epsilon * Epsilon
            ? -Vector3.UnitZ
            : Vector3.Normalize(ownerForward);

        Vector3 forwardFlat = new Vector3(forward.X, 0f, forward.Z);
        Vector3 aimFlat = new Vector3(toTarget.X, 0f, toTarget.Z);

        float yaw = 0f;
        if (forwardFlat.LengthSquared > Epsilon * Epsilon && aimFlat.LengthSquared > Epsilon * Epsilon)
        {
            forwardFlat = Vector3.Normalize(forwardFlat);
            aimFlat = Vector3.Normalize(aimFlat);
            float sin = Vector3.Dot(Vector3.Cross(aimFlat, forwardFlat), Vector3.UnitY);
            float cos = Vector3.Dot(forwardFlat, aimFlat);
            yaw = MathHelper.RadiansToDegrees(MathF.Atan2(sin, cos));
        }

        float horizontal = MathF.Sqrt(toTarget.X * toTarget.X + toTarget.Z * toTarget.Z);
        float pitch = MathHelper.RadiansToDegrees(MathF.Atan2(toTarget.Y, horizontal));

        return ClampAngles(yaw, pitch, yawMin, yawMax, pitchMin, pitchMax);
    }

    /// <summary>Clamps yaw and pitch to their respective min/max limits.</summary>
    public static (float yaw, float pitch) ClampAngles(
        float yaw,
        float pitch,
        float yawMin,
        float yawMax,
        float pitchMin,
        float pitchMax)
    {
        float clampedYaw = Math.Clamp(yaw, yawMin, yawMax);
        float clampedPitch = Math.Clamp(pitch, pitchMin, pitchMax);
        return (clampedYaw, clampedPitch);
    }

    /// <summary>
    /// Composes the part model matrix:
    /// <c>parentModel * T(pivot) * R_yaw * R_pitch * T(meshOffset) * S(partScale)</c>.
    /// Owner scale is applied to the pivot offset (see <see cref="ECS.ShipEngineEmitterSystem"/>).
    /// </summary>
    public static Matrix4 ComposePartModelMatrix(
        Matrix4 parentModel,
        Vector3 localPivotOffset,
        float yawDegrees,
        float pitchDegrees,
        Vector3 meshLocalOffset,
        Vector3 ownerScale)
    {
        Vector3 scaledPivot = ScaleByOwner(localPivotOffset, ownerScale);

        Matrix4 tPivot = Matrix4.CreateTranslation(scaledPivot);
        Matrix4 rYaw = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(yawDegrees));
        Matrix4 rPitch = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(pitchDegrees));
        Matrix4 tMesh = Matrix4.CreateTranslation(meshLocalOffset);
        Matrix4 sPart = Matrix4.CreateScale(ownerScale);

        return parentModel * tPivot * rYaw * rPitch * tMesh * sPart;
    }

    /// <summary>
    /// Returns the world-space pivot position implied by <paramref name="parentModel"/> and the
    /// scaled local pivot offset.
    /// </summary>
    public static Vector3 ComputePivotWorld(
        Matrix4 parentModel,
        Vector3 localPivotOffset,
        Vector3 ownerScale)
    {
        Vector3 scaledPivot = ScaleByOwner(localPivotOffset, ownerScale);
        return TransformPoint(scaledPivot, parentModel);
    }

    /// <summary>Transforms a point using OpenTK row-vector convention (point × matrix).</summary>
    public static Vector3 TransformPoint(Vector3 point, Matrix4 matrix)
    {
        Vector4 homogeneous = new Vector4(point, 1f);
        Vector4 transformed = homogeneous * matrix;
        return new Vector3(transformed.X, transformed.Y, transformed.Z);
    }

    private static Vector3 ScaleByOwner(Vector3 localOffset, Vector3 ownerScale) =>
        new Vector3(
            localOffset.X * ownerScale.X,
            localOffset.Y * ownerScale.Y,
            localOffset.Z * ownerScale.Z);
}