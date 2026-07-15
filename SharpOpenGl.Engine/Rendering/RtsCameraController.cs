using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Top-down RTS camera with pan, zoom, and tilt — shared by desktop and browser.
/// </summary>
public sealed class RtsCameraController
{
    public Vector3 Target { get; set; } = Vector3.Zero;
    public float Height { get; set; } = 80f;
    public float TiltAngle { get; set; } = 35f;
    public float PanSpeed { get; set; } = 60f;
    public float ZoomSpeed { get; set; } = 10f;
    public float MinHeight { get; set; } = 20f;
    public float MaxHeight { get; set; } = 200f;

    /// <summary>User settings multiplier applied to <see cref="PanSpeed"/>.</summary>
    public float PanSpeedMultiplier { get; set; } = 1f;

    /// <summary>User settings multiplier applied to <see cref="ZoomSpeed"/>.</summary>
    public float ZoomSpeedMultiplier { get; set; } = 1f;

    /// <summary>Pixels from the viewport edge where edge-scroll begins.</summary>
    public float EdgeScrollMarginPx { get; set; } = 22f;

    /// <summary>Extra pan multiplier while edge-scrolling (on top of <see cref="PanSpeedMultiplier"/>).</summary>
    public float EdgeScrollSpeedMultiplier { get; set; } = 1.35f;

    public void Pan(float dx, float dz, float deltaTime)
    {
        float speed = PanSpeed * MathF.Max(0.1f, PanSpeedMultiplier);
        Target += new Vector3(dx * speed * deltaTime, 0f, dz * speed * deltaTime);
    }

    public void PanByScreenDelta(Vector2 screenDelta, Vector2 viewportSize)
    {
        if (viewportSize.X <= 0f || viewportSize.Y <= 0f) return;
        float scale = Height / viewportSize.Y * 2.2f;
        Target += new Vector3(-screenDelta.X * scale, 0f, -screenDelta.Y * scale);
    }

    /// <summary>
    /// Pans the camera when the pointer sits inside the viewport edge band.
    /// Returns <c>true</c> when any edge-scroll axis is active.
    /// </summary>
    public bool ApplyEdgeScroll(Vector2 mousePosition, Vector2 viewportSize, float deltaTime)
    {
        if (viewportSize.X <= 0f || viewportSize.Y <= 0f)
            return false;

        float margin = MathF.Max(4f, EdgeScrollMarginPx);
        float dx = ResolveEdgeAxis(mousePosition.X, viewportSize.X, margin);
        float dz = ResolveEdgeAxis(mousePosition.Y, viewportSize.Y, margin);

        if (MathF.Abs(dx) < 0.01f && MathF.Abs(dz) < 0.01f)
            return false;

        float speed = PanSpeed * MathF.Max(0.1f, PanSpeedMultiplier) * EdgeScrollSpeedMultiplier;
        Target += new Vector3(dx * speed * deltaTime, 0f, dz * speed * deltaTime);
        return true;
    }

    public void Zoom(float delta) =>
        Height = ClampHeight(Height - delta * EffectiveZoomSpeed());

    /// <summary>
    /// Zoom toward a screen-space anchor so the world point under the cursor stays stable.
    /// </summary>
    public void ZoomTowardScreenPoint(float wheelDelta, Vector2 screenPoint, Vector2 viewportSize)
    {
        if (MathF.Abs(wheelDelta) < 0.001f || viewportSize.X <= 0f || viewportSize.Y <= 0f)
            return;

        float oldHeight = Height;
        float newHeight = ClampHeight(oldHeight - wheelDelta * EffectiveZoomSpeed());
        if (MathF.Abs(newHeight - oldHeight) < 0.001f)
            return;

        Vector2 ndc = new(
            (screenPoint.X / viewportSize.X) * 2f - 1f,
            1f - (screenPoint.Y / viewportSize.Y) * 2f);

        float heightRatio = newHeight / MathF.Max(oldHeight, 0.001f);
        float panScale = (1f - heightRatio) * oldHeight / viewportSize.Y * 2.2f;
        Target += new Vector3(-ndc.X * panScale, 0f, -ndc.Y * panScale);
        Height = newHeight;
    }

    public void AdjustHeight(float direction, float deltaTime, float speed = 80f)
    {
        if (MathF.Abs(direction) < 0.01f) return;
        Height = ClampHeight(Height + direction * speed * deltaTime);
    }

    /// <summary>Normalised zoom level in [0, 1] between <see cref="MinHeight"/> and <see cref="MaxHeight"/>.</summary>
    public float NormalizedHeight
    {
        get
        {
            if (MaxHeight <= MinHeight)
                return 0f;
            return Math.Clamp((Height - MinHeight) / (MaxHeight - MinHeight), 0f, 1f);
        }
    }

    public Matrix4 GetViewMatrix()
    {
        float tiltRad = MathHelper.DegreesToRadians(TiltAngle);
        float offsetZ = Height * MathF.Tan(tiltRad);
        Vector3 eye = Target + new Vector3(0f, Height, offsetZ);
        return Matrix4.LookAt(eye, Target, Vector3.UnitY);
    }

    public Matrix4 GetProjectionMatrix(float aspectRatio) =>
        Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(45f), aspectRatio, 0.1f, 10000f);

    /// <summary>World-space XZ bounds (minX, maxX, minZ, maxZ) for fog/terrain culling.</summary>
    public Vector4 GetVisibleBoundsXZ(float aspectRatio, float margin = 0f)
    {
        float viewW = Height * 1.4f;
        float viewH = viewW / MathF.Max(0.01f, aspectRatio);
        return new Vector4(
            Target.X - viewW * 0.5f - margin,
            Target.X + viewW * 0.5f + margin,
            Target.Z - viewH * 0.5f - margin,
            Target.Z + viewH * 0.5f + margin);
    }

    private float EffectiveZoomSpeed()
    {
        float t = NormalizedHeight;
        float heightScale = 0.55f + (1.15f - 0.55f) * t;
        return ZoomSpeed * MathF.Max(0.1f, ZoomSpeedMultiplier) * heightScale;
    }

    private float ClampHeight(float value) =>
        MathHelper.Clamp(value, MinHeight, MaxHeight);

    private static float ResolveEdgeAxis(float coordinate, float viewportSpan, float margin)
    {
        if (coordinate <= margin)
            return -SmoothEdgeStrength(margin - coordinate, margin);
        if (coordinate >= viewportSpan - margin)
            return SmoothEdgeStrength(coordinate - (viewportSpan - margin), margin);
        return 0f;
    }

    private static float SmoothEdgeStrength(float distanceIntoBand, float margin) =>
        MathF.Pow(Math.Clamp(distanceIntoBand / margin, 0f, 1f), 1.35f);
}