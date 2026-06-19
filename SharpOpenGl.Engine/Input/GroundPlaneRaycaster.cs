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

        Matrix4 invView = Matrix4.Invert(view);
        Matrix4 invProj = Matrix4.Invert(projection);

        Vector3 RayPoint(float ndcZ)
        {
            Vector4 clip = new(ndcX, ndcY, ndcZ, 1f);
            Vector4 eye = clip * invProj;
            if (MathF.Abs(eye.W) < 1e-6f)
                return Vector3.Zero;
            eye /= eye.W;
            Vector4 world = eye * invView;
            return new Vector3(world.X, world.Y, world.Z);
        }

        Vector3 near = RayPoint(-1f);
        Vector3 far = RayPoint(1f);
        Vector3 dir = far - near;

        if (MathF.Abs(dir.Y) < 1e-6f)
            return null;

        float t = -near.Y / dir.Y;
        if (t < 0f)
            return null;

        return near + dir * t;
    }
}