using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Stores position, rotation (Euler angles in degrees), and scale for an entity
/// in 3-D world space.
/// </summary>
public sealed class TransformComponent
{
    /// <summary>World-space position.</summary>
    public Vector3 Position { get; set; } = Vector3.Zero;

    /// <summary>
    /// Rotation expressed as Euler angles in degrees: X = Pitch, Y = Yaw, Z = Roll.
    /// </summary>
    public Vector3 EulerAngles { get; set; } = Vector3.Zero;

    /// <summary>Non-uniform scale.</summary>
    public Vector3 Scale { get; set; } = Vector3.One;

    /// <summary>
    /// Compute the model matrix in OpenTK's row-vector convention (v × M).
    /// Matrices are applied left-to-right, so the order here is:
    /// scale → yaw (Y) → pitch (X) → roll (Z) → translate.
    /// </summary>
    public Matrix4 GetModelMatrix()
    {
        Matrix4 s = Matrix4.CreateScale(Scale);
        Matrix4 rx = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(EulerAngles.X));
        Matrix4 ry = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(EulerAngles.Y));
        Matrix4 rz = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(EulerAngles.Z));
        Matrix4 t = Matrix4.CreateTranslation(Position);
        return s * ry * rx * rz * t;
    }
}
