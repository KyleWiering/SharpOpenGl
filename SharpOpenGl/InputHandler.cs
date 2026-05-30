using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace SharpOpenGl;

/// <summary>
/// Handles keyboard input and maps to movement/rotation axes.
/// Modernized from the original ButtonHandler.cs.
/// </summary>
public class InputHandler
{
    public Vector3 AxisMovement { get; private set; }
    public Vector3 AxisRotation { get; private set; }

    public void Update(KeyboardState keyboard)
    {
        float moveX = 0, moveY = 0, moveZ = 0;
        float rotX = 0, rotY = 0;

        if (keyboard.IsKeyDown(Keys.W)) moveZ = -1.0f;
        if (keyboard.IsKeyDown(Keys.S)) moveZ = 1.0f;
        if (keyboard.IsKeyDown(Keys.Q)) moveX = -1.0f;
        if (keyboard.IsKeyDown(Keys.E)) moveX = 1.0f;
        if (keyboard.IsKeyDown(Keys.Z)) moveY = 1.0f;
        if (keyboard.IsKeyDown(Keys.X)) moveY = -1.0f;
        if (keyboard.IsKeyDown(Keys.A)) rotY = 1.0f;
        if (keyboard.IsKeyDown(Keys.D)) rotY = -1.0f;

        AxisMovement = new Vector3(moveX, moveY, moveZ);
        AxisRotation = new Vector3(rotX, rotY, 0);
    }
}
