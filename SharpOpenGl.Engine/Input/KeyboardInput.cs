using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace SharpOpenGl.Engine.Input;

/// <summary>
/// Desktop keyboard implementation of <see cref="IInputProvider"/>.
/// Bindings mirror the default controls defined in controls.json.
/// Pass the OpenTK <see cref="KeyboardState"/> each frame via <see cref="Update(KeyboardState)"/>.
/// </summary>
public class KeyboardInput : IInputProvider
{
    private KeyboardState _current = default!;
    private KeyboardState _previous = default!;

    // Default key bindings — can be made data-driven via controls.json later
    private static readonly Dictionary<InputAction, Keys> Bindings = new()
    {
        { InputAction.CameraMoveForward,  Keys.W },
        { InputAction.CameraMoveBack,     Keys.S },
        { InputAction.CameraMoveLeft,     Keys.Q },
        { InputAction.CameraMoveRight,    Keys.E },
        { InputAction.CameraHeightUp,     Keys.Z },
        { InputAction.CameraHeightDown,   Keys.X },
        { InputAction.CameraRotateLeft,   Keys.A },
        { InputAction.CameraRotateRight,  Keys.D },
        { InputAction.Pause,              Keys.Escape },
        { InputAction.Ability1,           Keys.D1 },
        { InputAction.Ability2,           Keys.D2 },
        { InputAction.Ability3,           Keys.D3 },
        { InputAction.Ability4,           Keys.D4 },
        { InputAction.BuildMenu,          Keys.B },
        { InputAction.Confirm,            Keys.Enter },
        { InputAction.Cancel,             Keys.Escape },
    };

    /// <summary>Update with the current frame's keyboard state.</summary>
    public void Update(KeyboardState current)
    {
        _previous = _current;
        _current = current;
    }

    /// <inheritdoc/>
    public void Update() { /* Called by interface; use Update(KeyboardState) from OpenTK. */ }

    /// <inheritdoc/>
    public bool IsActionHeld(InputAction action) =>
        Bindings.TryGetValue(action, out Keys key) && _current.IsKeyDown(key);

    /// <inheritdoc/>
    public bool IsActionPressed(InputAction action) =>
        Bindings.TryGetValue(action, out Keys key) &&
        _current.IsKeyDown(key) && !_previous.IsKeyDown(key);

    /// <inheritdoc/>
    public bool IsActionReleased(InputAction action) =>
        Bindings.TryGetValue(action, out Keys key) &&
        !_current.IsKeyDown(key) && _previous.IsKeyDown(key);

    /// <inheritdoc/>
    public Vector2 GetAxis(string axisName) => axisName switch
    {
        "MoveHorizontal" => new Vector2(
            IsActionHeld(InputAction.CameraMoveRight) ? 1f : IsActionHeld(InputAction.CameraMoveLeft) ? -1f : 0f,
            0f),
        "MoveVertical" => new Vector2(
            0f,
            IsActionHeld(InputAction.CameraMoveForward) ? -1f : IsActionHeld(InputAction.CameraMoveBack) ? 1f : 0f),
        _ => Vector2.Zero,
    };

    /// <inheritdoc/>
    public Vector2 PointerPosition => new(-1f, -1f);
}
