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

    public void Pan(float dx, float dz, float deltaTime) =>
        Target += new Vector3(dx * PanSpeed * deltaTime, 0f, dz * PanSpeed * deltaTime);

    public void PanByScreenDelta(Vector2 screenDelta, Vector2 viewportSize)
    {
        if (viewportSize.X <= 0f || viewportSize.Y <= 0f) return;
        float scale = Height / viewportSize.Y * 2.2f;
        Target += new Vector3(-screenDelta.X * scale, 0f, -screenDelta.Y * scale);
    }

    public void Zoom(float delta) =>
        Height = MathHelper.Clamp(Height - delta * ZoomSpeed, MinHeight, MaxHeight);

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
}