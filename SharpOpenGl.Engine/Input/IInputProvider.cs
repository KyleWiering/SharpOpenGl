using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Input;

/// <summary>
/// Platform-agnostic input provider. Implementations wrap OpenTK keyboard,
/// touch screen, or gamepad APIs so game code stays portable.
/// </summary>
public interface IInputProvider
{
    /// <summary>Returns true while the given action is held.</summary>
    bool IsActionHeld(InputAction action);

    /// <summary>Returns true on the first frame the action was pressed.</summary>
    bool IsActionPressed(InputAction action);

    /// <summary>Returns true on the first frame the action was released.</summary>
    bool IsActionReleased(InputAction action);

    /// <summary>
    /// Returns a 2D axis value in [-1, 1] range.
    /// Axis names are defined in controls.json (e.g. "MoveHorizontal").
    /// </summary>
    Vector2 GetAxis(string axisName);

    /// <summary>
    /// Current cursor / touch position in screen pixels.
    /// Returns (-1,-1) if unavailable.
    /// </summary>
    Vector2 PointerPosition { get; }

    /// <summary>Called once per frame so the provider can update its state.</summary>
    void Update();
}
