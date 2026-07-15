using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI;
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

    /// <summary>Fired when the player opens the load-game slot picker.</summary>
    public event Action? LoadGameRequested;

    /// <summary>Fired when the player quits to the main menu.</summary>
    public event Action? QuitToMenuRequested;

    /// <summary>Whether at least one save file exists (gates Load Game).</summary>
    public bool HasSave { get; }

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>Build the pause-menu layout.</summary>
    /// <param name="hasSave">When <c>false</c>, the Load Game button is disabled.</param>
    public PauseScreen(bool hasSave = true)
    {
        HasSave = hasSave;

        var backdrop = new Panel
        {
            Name = "Backdrop",
            Anchor = Anchor.Stretch,
            Position = Vector2.Zero,
            BackgroundColor = MenuTheme.OverlayBackdrop,
            DrawBorder = false,
        };
        AddWidget(backdrop);

        var card = new Panel
        {
            Name = "PauseCard",
            Anchor = Anchor.Center,
            Position = Vector2.Zero,
            Size = new Vector2(380f, 460f),
        };
        MenuTheme.ApplyPanel(card);
        AddWidget(card);

        card.AddChild(new Label
        {
            Name = "Title",
            Text = "Paused",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-160f, 18f),
            Size = new Vector2(320f, 40f),
            FontSize = 26f,
            TextColor = MenuTheme.TitleColor,
        });

        float btnW = 320f;
        float btnH = 56f;
        float gap = 14f;
        float startY = 80f;

        (MenuIconKind Icon, string Name, string Label, string Tooltip, Action? Raise, bool Enabled)[] items =
        [
            (MenuIconKind.NavResume, "Resume", "Resume", "Return to gameplay", () => ResumeRequested?.Invoke(), true),
            (MenuIconKind.NavSave, "SaveGame", "Save Game", "Save progress", () => SaveGameRequested?.Invoke(), true),
            (MenuIconKind.NavLoadGame, "LoadGame", "Load Game", "Choose a save slot", () => LoadGameRequested?.Invoke(), hasSave),
            (MenuIconKind.NavSettings, "Settings", "Settings", "Game settings", () => SettingsRequested?.Invoke(), true),
            (MenuIconKind.NavQuit, "QuitToMenu", "Quit to Menu", "Return to main menu", () => QuitToMenuRequested?.Invoke(), true),
        ];

        for (int i = 0; i < items.Length; i++)
        {
            (MenuIconKind icon, string name, string label, string tooltip, Action? raise, bool enabled) = items[i];
            var btn = new IconButton
            {
                Name = name,
                Icon = icon,
                Label = label,
                TooltipHint = tooltip,
                Layout = IconButtonLayout.IconLeftOfLabel,
                IconSize = IconButton.TitleNavIconSize,
                FontSize = 20f,
                RequireMinimumHitExtent = true,
                Anchor = Anchor.TopCenter,
                Position = new Vector2(-btnW / 2f, startY + i * (btnH + gap)),
                Size = new Vector2(btnW, btnH),
                IsEnabled = enabled,
            };
            IconButton.ApplyMenuTheme(btn, showGlow: true);
            btn.Clicked += () => raise?.Invoke();
            card.AddChild(btn);
        }
    }
}