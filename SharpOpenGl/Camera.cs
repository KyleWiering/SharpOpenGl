using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

namespace SharpOpenGl;

/// <summary>
/// Camera class managing position and orientation in 3D space.
/// Ported from original Camera.cs with modernized math.
/// </summary>
public class Camera
{
    public Vector3 Position { get; set; }
    public Vector3 Forward { get; set; }
    public Vector3 Right { get; set; }
    public Vector3 Up { get; set; }

    public Camera()
    {
        Position = new Vector3(0.0f, 0.0f, 5.0f);
        Forward = new Vector3(0.0f, 0.0f, -1.0f);
        Right = new Vector3(1.0f, 0.0f, 0.0f);
        Up = new Vector3(0.0f, 1.0f, 0.0f);
    }

    public Matrix4 GetViewMatrix()
    {
        return Matrix4.LookAt(Position, Position + Forward, Up);
    }

    public void MoveXAxis(float distance)
    {
        Position += Right * distance;
    }

    public void MoveYAxis(float distance)
    {
        Position += Up * distance;
    }

    public void MoveZAxis(float distance)
    {
        Position += Forward * distance;
    }

    public void RotateX(float angle)
    {
        Forward = Vector3.Normalize(
            Forward * MathF.Cos(angle) + Up * MathF.Sin(angle));
        Up = Vector3.Normalize(Vector3.Cross(Forward, Right) * -1.0f);
    }

    public void RotateY(float angle)
    {
        Forward = Vector3.Normalize(
            Forward * MathF.Cos(angle) - Right * MathF.Sin(angle));
        Right = Vector3.Normalize(Vector3.Cross(Forward, Up));
    }
}
