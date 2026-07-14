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
        var backdrop = new Panel
        {
            Name = "LoadingBackdrop",
            Anchor = Anchor.Stretch,
            Position = Vector2.Zero,
            BackgroundColor = new Vector4(0.02f, 0.03f, 0.08f, 0.92f),
            DrawBorder = false,
        };
        AddWidget(backdrop);

        var panel = new Panel
        {
            Name = "LoadingPanel",
            Anchor = Anchor.Center,
            Position = new Vector2(-340f, -72f),
            Size = new Vector2(680f, 144f),
        };
        MenuTheme.ApplyPanel(panel);
        AddWidget(panel);

        _statusLabel = new Label
        {
            Name = "StatusLabel",
            Anchor = Anchor.TopLeft,
            Position = new Vector2(20f, 16f),
            Size = new Vector2(640f, 48f),
            FontSize = 20f,
            WrapWidth = 620f,
            MaxLines = 2,
            TextColor = MenuTheme.BodyTextColor,
            Text = "Loading…",
        };
        panel.AddChild(_statusLabel);

        _bar = new ProgressBar
        {
            Name = "LoadingBar",
            Anchor = Anchor.TopLeft,
            Position = new Vector2(20f, 84f),
            Size = new Vector2(640f, 28f),
            Value = 0f,
            Label = "0 %",
            TrackColor = MenuTheme.ButtonDisabled,
            FillColor = MenuTheme.ButtonHover,
            BorderColor = MenuTheme.ButtonBorder,
            LabelColor = MenuTheme.ButtonText,
        };
        panel.AddChild(_bar);
    }
}