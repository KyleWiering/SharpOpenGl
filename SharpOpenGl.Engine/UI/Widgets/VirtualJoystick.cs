using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>
/// An on-screen virtual joystick widget for touch-based camera movement.
/// The joystick consists of a static base ring and a movable thumb nub.
/// The thumb is dragged within the base radius; its offset is normalised to
/// a [-1, 1] axis value readable via <see cref="Axis"/>.
/// </summary>
public sealed class VirtualJoystick : Widget
{
    // ── Configuration ─────────────────────────────────────────────────────────

    /// <summary>Radius of the outer base ring in logical pixels.</summary>
    public float BaseRadius { get; set; } = 60f;

    /// <summary>Radius of the movable thumb nub in logical pixels.</summary>
    public float ThumbRadius { get; set; } = 24f;

    /// <summary>Base ring colour.</summary>
    public Vector4 BaseColor  { get; set; } = new Vector4(1f, 1f, 1f, 0.25f);

    /// <summary>Thumb nub colour.</summary>
    public Vector4 ThumbColor { get; set; } = new Vector4(1f, 1f, 1f, 0.55f);

    // ── State ─────────────────────────────────────────────────────────────────

    private Vector2 _thumbOffset;   // pixels, clamped to BaseRadius
    private bool    _isDragging;
    private int     _activeTouchId = -1;

    /// <summary>
    /// Current normalised axis output in the range [-1, 1] per axis.
    /// Zero when the thumb is at centre.
    /// </summary>
    public Vector2 Axis
    {
        get
        {
            float r = BaseRadius;
            return r > 0f ? new Vector2(_thumbOffset.X / r, _thumbOffset.Y / r) : Vector2.Zero;
        }
    }

    /// <summary>Whether the joystick is currently being held.</summary>
    public bool IsActive => _isDragging;

    // ── Touch API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Feed the current touch points each frame so the joystick can track drags.
    /// Call this before reading <see cref="Axis"/>.
    /// </summary>
    public void UpdateTouches(
        IReadOnlyList<Input.TouchPoint> touches,
        Vector2 containerPosition, Vector2 containerSize)
    {
        var (centre, _) = Resolve(containerPosition, containerSize);
        centre += new Vector2(Size.X * 0.5f, Size.Y * 0.5f);

        if (_activeTouchId >= 0)
        {
            // Track the finger we already claimed
            Input.TouchPoint? tracked = null;
            foreach (var t in touches)
                if (t.Id == _activeTouchId) { tracked = t; break; }

            if (tracked == null || !tracked.IsActive)
            {
                // Finger lifted
                _activeTouchId = -1;
                _isDragging    = false;
                _thumbOffset   = Vector2.Zero;
            }
            else
            {
                _thumbOffset = ClampToRadius(tracked.Position - centre);
            }
        }
        else
        {
            // Look for a new touch that lands within the base
            foreach (var t in touches)
            {
                if (!t.IsActive || t.WasActive) continue;
                float dist = Vector2.Distance(t.Position, centre);
                if (dist <= BaseRadius)
                {
                    _activeTouchId = t.Id;
                    _isDragging    = true;
                    _thumbOffset   = ClampToRadius(t.Position - centre);
                    break;
                }
            }
        }
    }

    // ── Drawing ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        Vector2 centre = position + size * 0.5f;

        // Draw base as a square approximation (renderer may not support circles)
        float d = BaseRadius * 2f;
        Vector2 basePos = centre - new Vector2(BaseRadius, BaseRadius);
        renderer.DrawRect(basePos, new Vector2(d, d), BaseColor);

        // Draw thumb nub
        Vector2 thumbCentre = centre + _thumbOffset;
        float td = ThumbRadius * 2f;
        Vector2 thumbPos = thumbCentre - new Vector2(ThumbRadius, ThumbRadius);
        renderer.DrawRect(thumbPos, new Vector2(td, td), ThumbColor);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Vector2 ClampToRadius(Vector2 offset)
    {
        float len = offset.Length;
        return len > BaseRadius ? offset * (BaseRadius / len) : offset;
    }
}
