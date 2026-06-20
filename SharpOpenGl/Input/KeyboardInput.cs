using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SharpOpenGl.Engine.Input;
using InputAction = SharpOpenGl.Engine.Input.InputAction;

namespace SharpOpenGl.Input;

/// <summary>
/// Desktop keyboard implementation of <see cref="IInputProvider"/>.
/// </summary>
public class KeyboardInput : IInputProvider
{
    private KeyboardState _current = default!;
    private KeyboardState _previous = default!;

    private static readonly Dictionary<InputAction, Keys> Bindings = new()
    {
        { InputAction.CameraMoveForward,  Keys.W },
        { InputAction.CameraMoveBack,     Keys.S },
        { InputAction.CameraMoveLeft,     Keys.A },
        { InputAction.CameraMoveRight,    Keys.D },
        { InputAction.CameraHeightUp,     Keys.Z },
        { InputAction.CameraHeightDown,   Keys.X },
        { InputAction.Pause,              Keys.Escape },
        { InputAction.Ability1,           Keys.D1 },
        { InputAction.Ability2,           Keys.D2 },
        { InputAction.Ability3,           Keys.D3 },
        { InputAction.Ability4,           Keys.D4 },
        { InputAction.BuildMenu,          Keys.B },
        { InputAction.Confirm,            Keys.Enter },
        { InputAction.Cancel,             Keys.Escape },
    };

    public void Update(KeyboardState current)
    {
        _previous = _current;
        _current = current;
    }

    public void Update() { }

    public bool IsActionHeld(InputAction action) =>
        Bindings.TryGetValue(action, out Keys key) && _current.IsKeyDown(key);

    public bool IsActionPressed(InputAction action) =>
        Bindings.TryGetValue(action, out Keys key) &&
        _current.IsKeyDown(key) && !_previous.IsKeyDown(key);

    public bool IsActionReleased(InputAction action) =>
        Bindings.TryGetValue(action, out Keys key) &&
        !_current.IsKeyDown(key) && _previous.IsKeyDown(key);

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

    public Vector2 PointerPosition => new(-1f, -1f);
}