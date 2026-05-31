using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Input;

/// <summary>
/// Touch-screen implementation of <see cref="IInputProvider"/>.
/// Feeds raw <see cref="TouchPoint"/> data into a <see cref="GestureRecognizer"/>
/// and maps the resulting <see cref="GestureEvent"/>s to logical <see cref="InputAction"/>s.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// var touchInput = new TouchInput();
/// // Each frame, update touch points and advance the provider:
/// touchInput.SetTouchPoints(platformTouchPoints);
/// touchInput.Update(deltaTime);
/// // Then query via IInputProvider:
/// if (touchInput.IsActionPressed(InputAction.Select)) { ... }
/// </code>
/// </remarks>
public sealed class TouchInput : IInputProvider
{
    private readonly GestureRecognizer _recognizer = new();
    private readonly List<TouchPoint> _touches = new();

    // Actions pressed/held/released this frame
    private readonly HashSet<InputAction> _pressed  = new();
    private readonly HashSet<InputAction> _held     = new();
    private readonly HashSet<InputAction> _released = new();

    // Last known pointer position (centroid of active touches, or last tap)
    private Vector2 _pointerPosition = new(-1f, -1f);

    // Camera pan axis (fed from two-finger drag)
    private Vector2 _panAxis;

    // Pinch zoom delta (positive = zoom in)
    private float _pinchZoom;

    // ── IInputProvider ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Vector2 PointerPosition => _pointerPosition;

    /// <inheritdoc/>
    public bool IsActionPressed(InputAction action)  => _pressed.Contains(action);

    /// <inheritdoc/>
    public bool IsActionHeld(InputAction action)     => _held.Contains(action);

    /// <inheritdoc/>
    public bool IsActionReleased(InputAction action) => _released.Contains(action);

    /// <inheritdoc/>
    public Vector2 GetAxis(string axisName) => axisName switch
    {
        "MoveHorizontal" => new Vector2(_panAxis.X, 0f),
        "MoveVertical"   => new Vector2(0f, _panAxis.Y),
        "PinchZoom"      => new Vector2(_pinchZoom, 0f),
        _                => Vector2.Zero,
    };

    // ── Touch data ingestion ──────────────────────────────────────────────────

    /// <summary>
    /// Replace the current touch-point list with data from the platform layer.
    /// Call this before <see cref="Update"/> each frame.
    /// </summary>
    public void SetTouchPoints(IEnumerable<TouchPoint> points)
    {
        _touches.Clear();
        _touches.AddRange(points);

        // Update pointer position to centroid of active touches
        int activeCount = 0;
        Vector2 sum = Vector2.Zero;
        foreach (var t in _touches)
        {
            if (!t.IsActive) continue;
            sum += t.Position;
            activeCount++;
        }
        if (activeCount > 0)
            _pointerPosition = sum / activeCount;
    }

    // ── IInputProvider.Update ─────────────────────────────────────────────────

    /// <inheritdoc/>
    public void Update() => Update(0f);

    /// <summary>Advance gesture recognition by <paramref name="dt"/> seconds.</summary>
    public void Update(float dt)
    {
        _pressed.Clear();
        _released.Clear();
        _panAxis   = Vector2.Zero;
        _pinchZoom = 0f;

        IReadOnlyList<GestureEvent> gestures = _recognizer.Update(_touches, dt);

        foreach (GestureEvent g in gestures)
        {
            switch (g.Type)
            {
                case GestureType.Tap:
                    _pressed.Add(InputAction.Select);
                    _pointerPosition = g.Position;
                    break;

                case GestureType.DoubleTap:
                    _pressed.Add(InputAction.MoveCommand);
                    _pointerPosition = g.Position;
                    break;

                case GestureType.LongPress:
                    _pressed.Add(InputAction.AttackCommand);
                    _pointerPosition = g.Position;
                    break;

                case GestureType.Drag:
                    // Single-finger drag — treat as box-select (held MultiSelect)
                    _held.Add(InputAction.MultiSelect);
                    break;

                case GestureType.TwoFingerDrag:
                    // Two-finger drag — camera pan
                    _panAxis = -g.Delta; // invert so drag right moves map right
                    break;

                case GestureType.Pinch:
                    // Pinch — positive scale > 1 means fingers apart → zoom in
                    _pinchZoom = g.PinchScale - 1f;
                    if (_pinchZoom > 0f)
                        _held.Add(InputAction.CameraZoomIn);
                    else if (_pinchZoom < 0f)
                        _held.Add(InputAction.CameraZoomOut);
                    break;
            }
        }
    }
}
