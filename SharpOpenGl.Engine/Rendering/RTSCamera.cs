using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Top-down RTS camera with configurable tilt, zoom, and pan.
/// The camera looks down at the XZ plane from a height-adjustable position.
/// </summary>
/// <remarks>
/// Coordinate conventions (OpenTK / OpenGL):
/// <list type="bullet">
///   <item>+X → right, +Y → up (camera height axis), +Z → toward viewer (out of screen).</item>
///   <item>The grid lies in the XZ plane.</item>
///   <item>Camera is always positioned above its focus point.</item>
/// </list>
/// </remarks>
public sealed class RTSCamera
{
    // ── Configuration ─────────────────────────────────────────────────────────

    /// <summary>Minimum camera height above the grid.</summary>
    public float MinHeight { get; set; } = 5f;

    /// <summary>Maximum camera height above the grid.</summary>
    public float MaxHeight { get; set; } = 120f;

    /// <summary>Tilt angle in degrees when at minimum height (near-isometric view).</summary>
    public float MinTiltDeg { get; set; } = 30f;

    /// <summary>Tilt angle in degrees when at maximum height (top-down strategic view).</summary>
    public float MaxTiltDeg { get; set; } = 80f;

    /// <summary>Pan speed in world units per second.</summary>
    public float PanSpeed { get; set; } = 20f;

    /// <summary>Zoom speed (height units per second per scroll unit).</summary>
    public float ZoomSpeed { get; set; } = 30f;

    // ── State ─────────────────────────────────────────────────────────────────

    /// <summary>World-space XZ focus point (where the camera looks at on the grid).</summary>
    public Vector2 FocusPoint { get; set; } = Vector2.Zero;

    /// <summary>Camera height above the grid plane.</summary>
    public float Height { get; set; } = 30f;

    // ── Computed properties ───────────────────────────────────────────────────

    /// <summary>
    /// Tilt angle derived from the current height, linearly interpolated
    /// between <see cref="MinTiltDeg"/> and <see cref="MaxTiltDeg"/>.
    /// </summary>
    public float TiltDegrees
    {
        get
        {
            float t = MathHelper.Clamp(
                (Height - MinHeight) / (MaxHeight - MinHeight), 0f, 1f);
            return MathHelper.Lerp(MinTiltDeg, MaxTiltDeg, t);
        }
    }

    /// <summary>Camera world-space position derived from focus point, height and tilt.</summary>
    public Vector3 Position
    {
        get
        {
            float tiltRad = MathHelper.DegreesToRadians(
                Math.Clamp(TiltDegrees, 5f, 85f)); // guard against tan(0) / tan(90°)
            float backOffset = Height / MathF.Tan(tiltRad);
            return new Vector3(FocusPoint.X, Height, FocusPoint.Y + backOffset);
        }
    }

    /// <summary>Point the camera is looking at — the focus point at grid height.</summary>
    public Vector3 Target => new(FocusPoint.X, 0f, FocusPoint.Y);

    // ── View & Projection ─────────────────────────────────────────────────────

    /// <summary>Build the view matrix for the current camera state.</summary>
    public Matrix4 GetViewMatrix() =>
        Matrix4.LookAt(Position, Target, Vector3.UnitY);

    /// <summary>Build a perspective projection matrix.</summary>
    /// <param name="fovDeg">Vertical field of view in degrees.</param>
    /// <param name="aspectRatio">Viewport width / height.</param>
    /// <param name="nearPlane">Near clip distance.</param>
    /// <param name="farPlane">Far clip distance.</param>
    public Matrix4 GetProjectionMatrix(float fovDeg = 45f, float aspectRatio = 16f / 9f,
                                       float nearPlane = 0.1f, float farPlane = 1000f) =>
        Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(fovDeg), aspectRatio, nearPlane, farPlane);

    // ── Movement API ──────────────────────────────────────────────────────────

    /// <summary>Pan the camera in XZ world space.</summary>
    /// <param name="dx">X-axis movement (right/left).</param>
    /// <param name="dz">Z-axis movement (forward/back).</param>
    /// <param name="deltaTime">Elapsed seconds.</param>
    public void Pan(float dx, float dz, float deltaTime)
    {
        float speed = PanSpeed * deltaTime;
        FocusPoint = new Vector2(FocusPoint.X + dx * speed, FocusPoint.Y + dz * speed);
    }

    /// <summary>
    /// Zoom in (negative) or out (positive). Height is clamped to
    /// [<see cref="MinHeight"/>, <see cref="MaxHeight"/>].
    /// </summary>
    public void Zoom(float scrollDelta, float deltaTime)
    {
        Height = Math.Clamp(Height + scrollDelta * ZoomSpeed * deltaTime,
                            MinHeight, MaxHeight);
    }

    /// <summary>Move the camera to focus on <paramref name="worldXZ"/>.</summary>
    public void LookAt(Vector2 worldXZ) => FocusPoint = worldXZ;
}
