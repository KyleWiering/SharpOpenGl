using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Engine.UI.Screens;

/// <summary>
/// Pause-menu overlay drawn on top of the gameplay HUD.
/// Offers Resume, Settings, and Quit to Menu options.
/// </summary>
public sealed class PauseScreen : UIScreen
{
    /// <inheritdoc/>
    public override string ScreenName => "PauseMenu";

    /// <summary>
    /// Pause overlay keeps the gameplay scene visible beneath it.
    /// </summary>
    public override bool IsOverlay => true;

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Fired when the player chooses to resume the game.</summary>
    public event Action? ResumeRequested;

    /// <summary>Fired when the player opens settings from pause.</summary>
    public event Action? SettingsRequested;

    /// <summary>Fired when the player opens the save-game slot picker.</summary>
    public event Action? SaveGameRequested;

    /// <summary>Fired when the player quits to the main menu.</summary>
    public event Action? QuitToMenuRequested;

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>Build the pause-menu layout.</summary>
    public PauseScreen()
    {
        // Semi-transparent backdrop
        var backdrop = new Panel
        {
            Name = "Backdrop",
            Anchor = Anchor.Stretch,
            Position = Vector2.Zero,
            BackgroundColor = new Vector4(0f, 0f, 0f, 0.55f),
            DrawBorder = false,
        };
        AddWidget(backdrop);

        // Centre card
        var card = new Panel
        {
            Name = "PauseCard",
            Anchor = Anchor.Center,
            Position = Vector2.Zero,
            Size = new Vector2(360f, 390f),
            BackgroundColor = new Vector4(0.08f, 0.08f, 0.14f, 0.97f),
        };
        AddWidget(card);

        // Buttons inside the card
        float btnW = 280f;
        float btnH = 56f;
        float gap = 14f;
        float startY = 80f;  // relative to card top-left

        (string Label, Action Raise)[] items =
        [
            ("Resume",        () => ResumeRequested?.Invoke()),
            ("Save Game",     () => SaveGameRequested?.Invoke()),
            ("Settings",      () => SettingsRequested?.Invoke()),
            ("Quit to Menu",  () => QuitToMenuRequested?.Invoke()),
        ];

        for (int i = 0; i < items.Length; i++)
        {
            var (label, raise) = items[i];
            var btn = new Button
            {
                Name = label.Replace(" ", ""),
                Label = label,
                Anchor = Anchor.TopCenter,
                Position = new Vector2(-btnW / 2f, startY + i * (btnH + gap)),
                Size = new Vector2(btnW, btnH),
                FontSize = 20f,
            };
            btn.Clicked += raise;
            card.AddChild(btn);
        }
    }
}
