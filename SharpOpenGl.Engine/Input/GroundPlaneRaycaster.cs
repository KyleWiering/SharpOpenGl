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

    /// <summary>
    /// Project a world position to screen-space pixels (top-left origin).
    /// </summary>
    public static bool TryWorldToScreen(
        Vector3 worldPos,
        Vector2 viewportSize,
        Matrix4 projection,
        Matrix4 view,
        out Vector2 screenPos)
    {
        screenPos = Vector2.Zero;
        if (viewportSize.X <= 0f || viewportSize.Y <= 0f)
            return false;

        var clip = new Vector4(worldPos, 1f) * view * projection;
        if (clip.W <= 1e-6f)
            return false;

        float invW = 1f / clip.W;
        float ndcX = clip.X * invW;
        float ndcY = clip.Y * invW;

        screenPos = new Vector2(
            (ndcX + 1f) * 0.5f * viewportSize.X,
            (1f - ndcY) * 0.5f * viewportSize.Y);
        return true;
    }

    /// <summary>Returns <c>true</c> when <paramref name="point"/> lies inside the screen rectangle.</summary>
    public static bool IsInsideScreenRect(Vector2 point, Vector2 rectMin, Vector2 rectMax) =>
        point.X >= rectMin.X && point.X <= rectMax.X
        && point.Y >= rectMin.Y && point.Y <= rectMax.Y;
}