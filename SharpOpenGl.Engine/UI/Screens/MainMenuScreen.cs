using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Engine.UI.Screens;

/// <summary>
/// The main menu screen with navigation buttons:
/// New Game, Continue, Ship Designer, Settings, and Quit.
/// </summary>
public sealed class MainMenuScreen : UIScreen
{
    /// <inheritdoc/>
    public override string ScreenName => "MainMenu";

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Fired when the "New Game" button is clicked.</summary>
    public event Action? NewGameRequested;

    /// <summary>Fired when the "Continue" button is clicked.</summary>
    public event Action? ContinueRequested;

    /// <summary>Fired when the "Ship Designer" button is clicked.</summary>
    public event Action? ShipDesignerRequested;

    /// <summary>Fired when the "Settings" button is clicked.</summary>
    public event Action? SettingsRequested;

    /// <summary>Fired when the "Quit" button is clicked.</summary>
    public event Action? QuitRequested;

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>
    /// Initialise the main menu layout.
    /// </summary>
    /// <remarks>
    /// Buttons are centred horizontally and stacked vertically in the middle of
    /// the viewport using the reference resolution (1920 × 1080).
    /// </remarks>
    public MainMenuScreen()
    {
        BuildLayout();
    }

    // ── Layout ────────────────────────────────────────────────────────────────

    private void BuildLayout()
    {
        // Title label area (top-centre).
        var titlePanel = new Panel
        {
            Name = "TitlePanel",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(0f, 80f),
            Size = new Vector2(500f, 80f),
            BackgroundColor = new Vector4(0f, 0f, 0f, 0f),
            DrawBorder = false,
        };
        AddWidget(titlePanel);

        // Button stack (centre of screen).
        float btnW = 320f;
        float btnH = 60f;
        float gap = 16f;
        float totalH = 5 * btnH + 4 * gap;
        float startY = (1080f - totalH) / 2f;

        string[] labels = ["New Game", "Continue", "Ship Designer", "Settings", "Quit"];
        Action?[] handlers =
        [
            () => NewGameRequested?.Invoke(),
            () => ContinueRequested?.Invoke(),
            () => ShipDesignerRequested?.Invoke(),
            () => SettingsRequested?.Invoke(),
            () => QuitRequested?.Invoke(),
        ];

        for (int i = 0; i < labels.Length; i++)
        {
            string label = labels[i];
            Action? handler = handlers[i];

            var btn = new Button
            {
                Name = label.Replace(" ", ""),
                Label = label,
                Anchor = Anchor.TopCenter,
                Position = new Vector2(-btnW / 2f, startY + i * (btnH + gap)),
                Size = new Vector2(btnW, btnH),
                FontSize = 22f,
            };
            btn.Clicked += () => handler?.Invoke();
            AddWidget(btn);
        }
    }
}
