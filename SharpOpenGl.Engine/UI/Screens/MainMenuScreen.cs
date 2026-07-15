using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Engine.UI.Screens;

/// <summary>
/// Polished title screen with animated starfield, logo, and keyboard-navigable menu buttons.
/// </summary>
public sealed class MainMenuScreen : UIScreen
{
    private readonly List<IconButton> _navButtons = new();
    private int _focusedIndex;

    /// <inheritdoc/>
    public override string ScreenName => "MainMenu";

    /// <summary>Fired when the "New Game" button is clicked.</summary>
    public event Action? NewGameRequested;

    /// <summary>Fired when the "Sandbox" button is clicked.</summary>
    public event Action? SandboxRequested;

    /// <summary>Fired when the "Multiplayer" button is clicked.</summary>
    public event Action? MultiplayerRequested;

    /// <summary>Fired when the "Continue" button is clicked.</summary>
    public event Action? ContinueRequested;

    /// <summary>Fired when the "Load Game" button is clicked.</summary>
    public event Action? LoadGameRequested;

    /// <summary>Fired when the "Ship Designer" button is clicked.</summary>
    public event Action? ShipDesignerRequested;

    /// <summary>Fired when the "Settings" button is clicked.</summary>
    public event Action? SettingsRequested;

    /// <summary>Fired when the "Quit" button is clicked.</summary>
    public event Action? QuitRequested;

    /// <summary>Whether a save file exists and Continue is available.</summary>
    public bool HasSave { get; private set; }

    /// <summary>Navigation buttons in screen order.</summary>
    public IReadOnlyList<IconButton> NavButtons => _navButtons;

    /// <summary>
    /// Initialise the main menu layout.
    /// </summary>
    /// <param name="hasSave">When <c>false</c>, the Continue button is disabled.</param>
    public MainMenuScreen(bool hasSave = false)
    {
        HasSave = hasSave;
        BuildLayout();
        SetFocusedIndex(0, fromKeyboard: false);
    }

    /// <summary>Refresh Continue availability after saves change.</summary>
    public void SetHasSave(bool hasSave)
    {
        HasSave = hasSave;
        IconButton? continueBtn = FindNavButton("Continue");
        if (continueBtn != null)
            continueBtn.IsEnabled = hasSave;
        if (!hasSave && _focusedIndex < _navButtons.Count && _navButtons[_focusedIndex].Name == "Continue")
            SetFocusedIndex(FindNextEnabledIndex(_focusedIndex, 1), fromKeyboard: false);
    }

    /// <summary>Find a navigation button by <see cref="Widget.Name"/>.</summary>
    public IconButton? FindNavButton(string name)
    {
        foreach (IconButton button in _navButtons)
        {
            if (button.Name == name)
                return button;
        }
        return null;
    }

    /// <inheritdoc/>
    public override bool HandleKey(UIKey key)
    {
        switch (key)
        {
            case UIKey.Up:
                SetFocusedIndex(FindNextEnabledIndex(_focusedIndex, -1), fromKeyboard: true);
                return true;
            case UIKey.Down:
                SetFocusedIndex(FindNextEnabledIndex(_focusedIndex, 1), fromKeyboard: true);
                return true;
            case UIKey.Enter:
                ActivateFocusedButton();
                return true;
            case UIKey.Escape:
                QuitRequested?.Invoke();
                return true;
            default:
                return false;
        }
    }

    private void BuildLayout()
    {
        AddWidget(new MenuStarfieldBackground
        {
            Name = "Starfield",
            Anchor = Anchor.Stretch,
        });

        var title = new Label
        {
            Name = "Title",
            Text = MenuTheme.GameTitle,
            Anchor = Anchor.TopCenter,
            Position = new Vector2(0f, 108f),
            Size = new Vector2(900f, 72f),
            FontSize = 56f,
            TextColor = MenuTheme.TitleColor,
        };
        AddWidget(title);

        const float subtitleWidth = 900f;
        const float subtitleScrimHeight = 48f;
        AddWidget(new Panel
        {
            Name = "SubtitleScrim",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(0f, 176f),
            Size = new Vector2(subtitleWidth, subtitleScrimHeight),
            BackgroundColor = MenuTheme.SubtitleScrimColor,
            DrawBorder = false,
        });

        var subtitle = new Label
        {
            Name = "Subtitle",
            Text = MenuTheme.GameSubtitle,
            Anchor = Anchor.TopCenter,
            Position = new Vector2(0f, 182f),
            Size = new Vector2(subtitleWidth, 40f),
            FontSize = 22f,
            WrapWidth = UITextDrawing.ContentWrapWidth(subtitleWidth, 4f),
            MaxLines = 2,
            TextColor = MenuTheme.SubtitleColor,
        };
        AddWidget(subtitle);

        float btnW = 400f;
        float btnH = 58f;
        float gap = 10f;
        float totalH = 8 * btnH + 7 * gap;
        float startY = MathF.Max(248f, (1080f - totalH) * 0.5f);

        (string label, string name, MenuIconKind icon, string tooltip, Action? handler, bool enabled)[] entries =
        [
            ("New Game", "NewGame", MenuIconKind.NavNewGame, "Start a new campaign", () => NewGameRequested?.Invoke(), true),
            ("Sandbox", "Sandbox", MenuIconKind.NavSandbox, "Custom skirmish setup", () => SandboxRequested?.Invoke(), true),
            ("Multiplayer", "Multiplayer", MenuIconKind.NavMultiplayer, "Host or join multiplayer", () => MultiplayerRequested?.Invoke(), true),
            ("Continue", "Continue", MenuIconKind.NavContinue, "Resume last save", () => ContinueRequested?.Invoke(), HasSave),
            ("Load Game", "LoadGame", MenuIconKind.NavLoadGame, "Choose a save slot", () => LoadGameRequested?.Invoke(), HasSave),
            ("Ship Designer", "ShipDesigner", MenuIconKind.NavShipDesigner, "Design custom hulls", () => ShipDesignerRequested?.Invoke(), true),
            ("Settings", "Settings", MenuIconKind.NavSettings, "Audio and display options", () => SettingsRequested?.Invoke(), true),
            ("Quit", "Quit", MenuIconKind.NavQuit, "Exit to desktop", () => QuitRequested?.Invoke(), true),
        ];

        for (int i = 0; i < entries.Length; i++)
        {
            (string label, string name, MenuIconKind icon, string tooltip, Action? handler, bool enabled) = entries[i];
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
            btn.Clicked += () => handler?.Invoke();
            AddWidget(btn);
            _navButtons.Add(btn);
        }
    }

    private void SetFocusedIndex(int index, bool fromKeyboard)
    {
        if (_navButtons.Count == 0) return;

        _focusedIndex = ((index % _navButtons.Count) + _navButtons.Count) % _navButtons.Count;
        for (int i = 0; i < _navButtons.Count; i++)
            _navButtons[i].IsKeyboardFocused = fromKeyboard && i == _focusedIndex;
    }

    private int FindNextEnabledIndex(int start, int direction)
    {
        if (_navButtons.Count == 0) return 0;

        int index = start;
        for (int step = 0; step < _navButtons.Count; step++)
        {
            index = (index + direction + _navButtons.Count) % _navButtons.Count;
            if (_navButtons[index].IsEnabled)
                return index;
        }

        return start;
    }

    private void ActivateFocusedButton()
    {
        if (_focusedIndex < 0 || _focusedIndex >= _navButtons.Count) return;
        _navButtons[_focusedIndex].Activate();
    }
}