using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Engine.UI.Screens;

/// <summary>
/// Polished title screen with animated starfield, logo, and keyboard-navigable menu buttons.
/// </summary>
public sealed class MainMenuScreen : UIScreen
{
    private readonly List<Button> _navButtons = new();
    private int _focusedIndex;

    /// <inheritdoc/>
    public override string ScreenName => "MainMenu";

    /// <summary>Fired when the "New Game" button is clicked.</summary>
    public event Action? NewGameRequested;

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
    public IReadOnlyList<Button> NavButtons => _navButtons;

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
        Button? continueBtn = FindButton("Continue");
        if (continueBtn != null)
            continueBtn.IsEnabled = hasSave;
        if (!hasSave && _focusedIndex < _navButtons.Count && _navButtons[_focusedIndex].Name == "Continue")
            SetFocusedIndex(FindNextEnabledIndex(_focusedIndex, 1), fromKeyboard: false);
    }

    /// <summary>Find a navigation button by <see cref="Widget.Name"/>.</summary>
    public new Button? FindButton(string name)
    {
        foreach (Button button in _navButtons)
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

        var subtitle = new Label
        {
            Name = "Subtitle",
            Text = MenuTheme.GameSubtitle,
            Anchor = Anchor.TopCenter,
            Position = new Vector2(0f, 182f),
            Size = new Vector2(900f, 40f),
            FontSize = 22f,
            TextColor = MenuTheme.SubtitleColor,
        };
        AddWidget(subtitle);

        float btnW = 400f;
        float btnH = 64f;
        float gap = 14f;
        float totalH = 7 * btnH + 6 * gap;
        float startY = (1080f - totalH) / 2f + 24f;

        (string label, string name, Action? handler, bool enabled)[] entries =
        [
            ("New Game", "NewGame", () => NewGameRequested?.Invoke(), true),
            ("Multiplayer", "Multiplayer", () => MultiplayerRequested?.Invoke(), true),
            ("Continue", "Continue", () => ContinueRequested?.Invoke(), HasSave),
            ("Load Game", "LoadGame", () => LoadGameRequested?.Invoke(), HasSave),
            ("Ship Designer", "ShipDesigner", () => ShipDesignerRequested?.Invoke(), true),
            ("Settings", "Settings", () => SettingsRequested?.Invoke(), true),
            ("Quit", "Quit", () => QuitRequested?.Invoke(), true),
        ];

        for (int i = 0; i < entries.Length; i++)
        {
            (string label, string name, Action? handler, bool enabled) = entries[i];
            var btn = new Button
            {
                Name = name,
                Label = label,
                Anchor = Anchor.TopCenter,
                Position = new Vector2(-btnW / 2f, startY + i * (btnH + gap)),
                Size = new Vector2(btnW, btnH),
                FontSize = 20f,
                IsEnabled = enabled,
            };
            MenuTheme.ApplyNavButton(btn);
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