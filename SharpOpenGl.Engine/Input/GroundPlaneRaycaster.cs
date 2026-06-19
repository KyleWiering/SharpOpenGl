using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Input;

/// <summary>
/// Converts screen-space pointer coordinates to world XZ positions on the Y=0 ground plane.
/// </summary>
public static class GroundPlaneRaycaster
{
    /// <summary>
    /// Cast a ray from the camera through <paramref name="screenPos"/> and intersect the Y=0 plane.
    /// </summary>
    public static Vector3? ScreenToGround(
        Vector2 screenPos,
        Vector2 viewportSize,
        Matrix4 projection,
        Matrix4 view)
    {
        if (viewportSize.X <= 0f || viewportSize.Y <= 0f)
            return null;

        float ndcX = (2f * screenPos.X / viewportSize.X) - 1f;
        float ndcY = 1f - (2f * screenPos.Y / viewportSize.Y);

        Matrix4 invProj = Matrix4.Invert(projection);
        Matrix4 invView = Matrix4.Invert(view);

        var nearPoint = new Vector4(ndcX, ndcY, -1f, 1f) * invProj * invView;
        var farPoint = new Vector4(ndcX, ndcY, 1f, 1f) * invProj * invView;

        if (MathF.Abs(nearPoint.W) < 1e-6f || MathF.Abs(farPoint.W) < 1e-6f)
            return null;

        Vector3 near = new Vector3(nearPoint.X, nearPoint.Y, nearPoint.Z) / nearPoint.W;
        Vector3 far = new Vector3(farPoint.X, farPoint.Y, farPoint.Z) / farPoint.W;
        Vector3 dir = far - near;

        if (MathF.Abs(dir.Y) < 1e-6f)
            return null;

        float t = -near.Y / dir.Y;
        if (t < 0f)
            return null;

        return near + dir * t;
    }
}