using OpenTK.Mathematics;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.Events;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// Manages short-lived expanding line-ring overlays for shield break, death, and ultimate cast.
/// Renderers draw using a pre-uploaded selection-ring VAO — no per-frame mesh upload.
/// </summary>
public sealed class CombatRingOverlayController
{
    private const int MaxRings = 24;

    private readonly List<RingOverlay> _rings = new();

    /// <summary>When false, incoming events are ignored (e.g. low performance tier).</summary>
    public bool Enabled { get; set; } = true;

    public int ActiveRingCount => _rings.Count;

    public void Bind(EventBus bus) =>
        bus.Subscribe<CombatRingVfxEvent>(OnRingEvent);

    public void Spawn(Vector3 position, CombatRingVfxKind kind, float radius, Vector4? tint = null)
    {
        if (!Enabled) return;

        var overlay = kind switch
        {
            CombatRingVfxKind.ShieldBreak => new RingOverlay
            {
                Position = position,
                BaseRadius = radius,
                ExpandFactor = 0.55f,
                Duration = 0.28f,
                Remaining = 0.28f,
                PeakAlpha = 0.72f,
                Color = tint ?? RaceShieldSchema.DefaultShieldTint,
            },
            CombatRingVfxKind.DeathExpand => new RingOverlay
            {
                Position = position,
                BaseRadius = radius,
                ExpandFactor = 1.25f,
                Duration = 0.42f,
                Remaining = 0.42f,
                PeakAlpha = 0.68f,
                Color = tint ?? new Vector4(1f, 0.45f, 0.12f, 1f),
            },
            CombatRingVfxKind.UltimateCast => new RingOverlay
            {
                Position = position,
                BaseRadius = radius,
                ExpandFactor = 0.85f,
                Duration = 0.36f,
                Remaining = 0.36f,
                PeakAlpha = 0.78f,
                Color = tint ?? new Vector4(0.55f, 0.85f, 1f, 1f),
            },
            _ => new RingOverlay
            {
                Position = position,
                BaseRadius = radius,
                ExpandFactor = 0.5f,
                Duration = 0.25f,
                Remaining = 0.25f,
                PeakAlpha = 0.6f,
                Color = tint ?? Vector4.One,
            },
        };

        if (_rings.Count >= MaxRings)
            _rings.RemoveAt(0);

        _rings.Add(overlay);
    }

    public void Update(float deltaTime)
    {
        for (int i = _rings.Count - 1; i >= 0; i--)
        {
            _rings[i].Remaining -= deltaTime;
            if (_rings[i].Remaining <= 0f)
                _rings.RemoveAt(i);
        }
    }

    /// <summary>Returns draw states for the current frame (scale + alpha derived from lifetime).</summary>
    public IReadOnlyList<CombatRingDrawState> BuildDrawStates()
    {
        if (_rings.Count == 0)
            return Array.Empty<CombatRingDrawState>();

        var states = new CombatRingDrawState[_rings.Count];
        for (int i = 0; i < _rings.Count; i++)
        {
            var ring = _rings[i];
            float progress = 1f - MathF.Max(0f, ring.Remaining / ring.Duration);
            float scale = ring.BaseRadius * (1f + progress * ring.ExpandFactor);
            float alpha = (1f - progress) * ring.PeakAlpha;

            states[i] = new CombatRingDrawState
            {
                Position = ring.Position,
                Scale = scale,
                Alpha = alpha,
                Color = ring.Color,
            };
        }

        return states;
    }

    private void OnRingEvent(CombatRingVfxEvent evt) =>
        Spawn(evt.Position, evt.Kind, evt.Radius, evt.Tint);

    private sealed class RingOverlay
    {
        public Vector3 Position;
        public float BaseRadius;
        public float ExpandFactor;
        public float Duration;
        public float Remaining;
        public float PeakAlpha;
        public Vector4 Color;
    }
}

/// <summary>Per-frame ring overlay draw parameters for line-mode selection VAO.</summary>
public readonly struct CombatRingDrawState
{
    public Vector3 Position { get; init; }
    public float Scale { get; init; }
    public float Alpha { get; init; }
    public Vector4 Color { get; init; }
}