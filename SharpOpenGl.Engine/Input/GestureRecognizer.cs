using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Input;

/// <summary>
/// Processes raw touch-point data each frame and emits high-level
/// <see cref="GestureEvent"/>s for consumption by <see cref="TouchInput"/>.
/// </summary>
/// <remarks>
/// Gesture detection rules:
/// <list type="bullet">
///   <item><b>Tap</b> — one finger lifts within <see cref="TapMaxDuration"/> seconds and
///     moved less than <see cref="TapMaxMovePx"/> pixels.</item>
///   <item><b>DoubleTap</b> — two taps within <see cref="DoubleTapWindowSeconds"/> seconds
///     at positions within <see cref="TapMaxMovePx"/> of each other.</item>
///   <item><b>LongPress</b> — one finger held for at least <see cref="LongPressDuration"/>
///     seconds without moving more than <see cref="TapMaxMovePx"/> pixels.</item>
///   <item><b>Drag</b> — one finger moves more than <see cref="TapMaxMovePx"/> pixels.</item>
///   <item><b>TwoFingerDrag</b> — two fingers move in the same direction.</item>
///   <item><b>Pinch</b> — two fingers change their distance apart.</item>
/// </list>
/// </remarks>
public sealed class GestureRecognizer
{
    // ── Tunable thresholds ────────────────────────────────────────────────────

    /// <summary>Maximum contact duration in seconds for a tap to be recognised.</summary>
    public float TapMaxDuration { get; set; } = 0.3f;

    /// <summary>Maximum movement in pixels for a contact to be considered stationary (tap / long-press).</summary>
    public float TapMaxMovePx { get; set; } = 10f;

    /// <summary>Contact duration threshold in seconds for a long-press.</summary>
    public float LongPressDuration { get; set; } = 0.5f;

    /// <summary>Maximum interval between two taps for them to count as a double-tap.</summary>
    public float DoubleTapWindowSeconds { get; set; } = 0.4f;

    // ── State ─────────────────────────────────────────────────────────────────

    private float _lastTapTime = float.MinValue;
    private Vector2 _lastTapPosition;
    private float _elapsedTime;
    private float _prevSpan;
    private Vector2 _prevTwoFingerMid;
    private bool _longPressEmitted;

    // Per-touch previous position keyed by touch id
    private readonly Dictionary<int, Vector2> _prevPositions = new();

    // ── API ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Advance time and evaluate touch points.
    /// Returns all gestures recognised this frame (may be empty).
    /// </summary>
    /// <param name="touches">Current active and just-released touch points.</param>
    /// <param name="dt">Delta time in seconds.</param>
    public IReadOnlyList<GestureEvent> Update(IReadOnlyList<TouchPoint> touches, float dt)
    {
        _elapsedTime += dt;

        var events = new List<GestureEvent>();
        var active = FilterActive(touches);
        var justReleased = FilterJustReleased(touches);

        switch (active.Count)
        {
            case 1:
                HandleSingleFinger(active[0], justReleased, dt, events);
                break;

            case 2:
                HandleTwoFingers(active[0], active[1], dt, events);
                _longPressEmitted = false;
                break;

            default:
                _longPressEmitted = false;
                _prevSpan = 0f;
                break;
        }

        // Handle releases for one-finger discrete gestures
        if (active.Count == 0 && justReleased.Count == 1)
            HandleRelease(justReleased[0], events);

        // Advance stored previous positions
        var activeIds = new HashSet<int>();
        foreach (var t in active)
        {
            _prevPositions[t.Id] = t.Position;
            activeIds.Add(t.Id);
        }
        // Remove positions for fingers that are no longer present
        foreach (int id in new List<int>(_prevPositions.Keys))
            if (!activeIds.Contains(id)) _prevPositions.Remove(id);

        return events;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static List<TouchPoint> FilterActive(IReadOnlyList<TouchPoint> touches)
    {
        var list = new List<TouchPoint>(touches.Count);
        foreach (var t in touches)
            if (t.IsActive) list.Add(t);
        return list;
    }

    private static List<TouchPoint> FilterJustReleased(IReadOnlyList<TouchPoint> touches)
    {
        var list = new List<TouchPoint>(touches.Count);
        foreach (var t in touches)
            if (t.WasActive && !t.IsActive) list.Add(t);
        return list;
    }

    private void HandleSingleFinger(
        TouchPoint touch, IReadOnlyList<TouchPoint> released, float dt,
        List<GestureEvent> events)
    {
        float moved = Vector2.Distance(touch.Position, touch.StartPosition);

        if (moved > TapMaxMovePx)
        {
            // Drag — compute delta against stored previous position
            _longPressEmitted = false;
            Vector2 prevPos = _prevPositions.TryGetValue(touch.Id, out Vector2 pp)
                ? pp : touch.Position;
            Vector2 delta = touch.Position - prevPos;
            events.Add(new GestureEvent
            {
                Type     = GestureType.Drag,
                Position = touch.Position,
                Delta    = delta,
            });
        }
        else if (!_longPressEmitted && touch.ContactDuration >= LongPressDuration)
        {
            _longPressEmitted = true;
            events.Add(new GestureEvent
            {
                Type     = GestureType.LongPress,
                Position = touch.Position,
            });
        }
    }

    private void HandleRelease(TouchPoint released, List<GestureEvent> events)
    {
        float moved = Vector2.Distance(released.Position, released.StartPosition);
        if (moved > TapMaxMovePx || released.ContactDuration > TapMaxDuration)
        {
            _longPressEmitted = false;
            return;
        }

        _longPressEmitted = false;

        // Check for double-tap
        bool isDoubleTap = (_elapsedTime - _lastTapTime) <= DoubleTapWindowSeconds
                        && Vector2.Distance(released.Position, _lastTapPosition) <= TapMaxMovePx * 2f;

        if (isDoubleTap)
        {
            _lastTapTime = float.MinValue; // reset so third tap isn't another double
            events.Add(new GestureEvent
            {
                Type     = GestureType.DoubleTap,
                Position = released.Position,
            });
        }
        else
        {
            _lastTapTime     = _elapsedTime;
            _lastTapPosition = released.Position;
            events.Add(new GestureEvent
            {
                Type     = GestureType.Tap,
                Position = released.Position,
            });
        }
    }

    private void HandleTwoFingers(
        TouchPoint a, TouchPoint b, float dt,
        List<GestureEvent> events)
    {
        Vector2 mid  = (a.Position + b.Position) * 0.5f;
        float   span = Vector2.Distance(a.Position, b.Position);

        if (_prevSpan > 0f)
        {
            // Pinch scale
            float scale = span / _prevSpan;
            if (MathF.Abs(scale - 1f) > 0.005f)
            {
                events.Add(new GestureEvent
                {
                    Type       = GestureType.Pinch,
                    Position   = mid,
                    PinchScale = scale,
                });
            }

            // Two-finger pan
            Vector2 midDelta = mid - _prevTwoFingerMid;
            if (midDelta.LengthSquared > 0.01f)
            {
                events.Add(new GestureEvent
                {
                    Type     = GestureType.TwoFingerDrag,
                    Position = mid,
                    Delta    = midDelta,
                });
            }
        }

        _prevSpan         = span;
        _prevTwoFingerMid = mid;
    }
}
