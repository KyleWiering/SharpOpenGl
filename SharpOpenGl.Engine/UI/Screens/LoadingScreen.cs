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
    private readonly Label _statusLabel;

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
        get => _statusLabel.Text;
        set => _statusLabel.Text = value;
    }

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

        _statusLabel = new Label
        {
            Name = "StatusLabel",
            Anchor = Anchor.Center,
            Position = new Vector2(-290f, -40f),
            Size = new Vector2(580f, 48f),
            FontSize = 20f,
            WrapWidth = 560f,
            MaxLines = 2,
            TextColor = new Vector4(0.75f, 0.85f, 1f, 1f),
            Text = "Loading…",
        };
        AddWidget(_statusLabel);

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

}