using OpenTK.Mathematics;

namespace SharpOpenGl;

/// <summary>
/// Top-down RTS camera with pan (WASD/edge scroll), zoom (scroll wheel),
/// and slight tilt for depth perspective.
/// </summary>
public class RTSCameraController
{
    /// <summary>Camera look-at target on the XZ plane.</summary>
    public Vector3 Target { get; set; } = Vector3.Zero;

    /// <summary>Camera height above the target plane.</summary>
    public float Height { get; set; } = 80f;

    /// <summary>Camera tilt angle in degrees from vertical (0=straight down).</summary>
    public float TiltAngle { get; set; } = 35f;

    /// <summary>Pan speed in world units per second.</summary>
    public float PanSpeed { get; set; } = 60f;

    /// <summary>Zoom speed (units per scroll tick).</summary>
    public float ZoomSpeed { get; set; } = 10f;

    /// <summary>Minimum camera height.</summary>
    public float MinHeight { get; set; } = 20f;

    /// <summary>Maximum camera height.</summary>
    public float MaxHeight { get; set; } = 200f;

    /// <summary>Apply panning (X = horizontal, Y = vertical in world XZ).</summary>
    public void Pan(float dx, float dz, float deltaTime)
    {
        Target += new Vector3(dx * PanSpeed * deltaTime, 0f, dz * PanSpeed * deltaTime);
    }

    /// <summary>Apply zoom by changing height.</summary>
    public void Zoom(float delta)
    {
        Height = MathHelper.Clamp(Height - delta * ZoomSpeed, MinHeight, MaxHeight);
    }

    /// <summary>Rotate the camera around the Y axis (degrees per second).</summary>
    public void Rotate(float direction, float deltaTime, float speed = 90f)
    {
        if (MathF.Abs(direction) < 0.01f) return;
        float radians = MathHelper.DegreesToRadians(speed * direction * deltaTime);
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);
        float x = Target.X * cos - Target.Z * sin;
        float z = Target.X * sin + Target.Z * cos;
        Target = new Vector3(x, Target.Y, z);
    }

    /// <summary>Raise or lower the camera height.</summary>
    public void AdjustHeight(float direction, float deltaTime, float speed = 80f)
    {
        if (MathF.Abs(direction) < 0.01f) return;
        Height = MathHelper.Clamp(Height + direction * speed * deltaTime, MinHeight, MaxHeight);
    }

    /// <summary>Get the computed view matrix.</summary>
    public Matrix4 GetViewMatrix()
    {
        float tiltRad = MathHelper.DegreesToRadians(TiltAngle);
        float offsetZ = Height * MathF.Tan(tiltRad);
        Vector3 eye = Target + new Vector3(0f, Height, offsetZ);
        return Matrix4.LookAt(eye, Target, Vector3.UnitY);
    }

    /// <summary>Get the camera eye position (for raycasting).</summary>
    public Vector3 GetEyePosition()
    {
        float tiltRad = MathHelper.DegreesToRadians(TiltAngle);
        float offsetZ = Height * MathF.Tan(tiltRad);
        return Target + new Vector3(0f, Height, offsetZ);
    }
}