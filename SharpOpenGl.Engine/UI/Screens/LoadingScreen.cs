using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Engine.UI.Screens;

/// <summary>
/// Full-screen loading overlay with a centred progress bar and status message.
/// Intended to cover the viewport while assets, maps, or missions are loading.
/// </summary>
public sealed class LoadingScreen : UIScreen
{
    /// <inheritdoc/>
    public override string ScreenName => "LoadingScreen";

    /// <summary>
    /// Loading screens draw over whatever is beneath them.
    /// </summary>
    public override bool IsOverlay => true;

    // ── Widgets ───────────────────────────────────────────────────────────────

    private readonly ProgressBar _bar;
    private readonly Panel _statusPanel;

    // ── Properties ───────────────────────────────────────────────────────────

    /// <summary>
    /// Current fill fraction in the range [0, 1].
    /// Set this each frame as loading work completes.
    /// </summary>
    public float Progress
    {
        get => _bar.Value;
        set
        {
            _bar.Value = value;
            _bar.Label = $"{(int)(value * 100f)} %";
        }
    }

    /// <summary>Short text displayed above the progress bar (e.g. "Loading assets…").</summary>
    public string StatusText
    {
        get => _statusLabel;
        set
        {
            _statusLabel = value;
            // The label is painted by OnDraw via the Panel; just store it.
        }
    }
    private string _statusLabel = "Loading…";

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>Build the loading-screen layout.</summary>
    public LoadingScreen()
    {
        // Opaque dark backdrop
        var backdrop = new Panel
        {
            Name = "LoadingBackdrop",
            Anchor = Anchor.Stretch,
            Position = Vector2.Zero,
            BackgroundColor = new Vector4(0.04f, 0.04f, 0.08f, 1f),
            DrawBorder = false,
        };
        AddWidget(backdrop);

        // Status panel (centre, above bar)
        _statusPanel = new Panel
        {
            Name = "StatusPanel",
            Anchor = Anchor.Center,
            Position = new Vector2(-300f, -40f),
            Size = new Vector2(600f, 32f),
            BackgroundColor = Vector4.Zero,
            DrawBorder = false,
        };
        AddWidget(_statusPanel);

        // Progress bar (centred, 600 × 28)
        _bar = new ProgressBar
        {
            Name = "LoadingBar",
            Anchor = Anchor.Center,
            Position = new Vector2(-300f, 8f),
            Size = new Vector2(600f, 28f),
            Value = 0f,
            Label = "0 %",
        };
        AddWidget(_bar);
    }

    // ── Per-frame ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override void Draw(IUIRenderer renderer)
    {
        base.Draw(renderer);

        // Draw the status text manually at a fixed position relative to viewport.
        Vector2 viewport = renderer.ViewportSize;
        float textX = viewport.X / 2f - 290f;
        float textY = viewport.Y / 2f - 40f;
        renderer.DrawText(
            _statusLabel,
            new Vector2(textX, textY),
            fontSize: 20f,
            color: new Vector4(0.75f, 0.85f, 1f, 1f));
    }
}
