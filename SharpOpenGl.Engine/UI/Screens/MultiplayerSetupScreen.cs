using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Engine.UI.Screens;

/// <summary>
/// Screen for configuring multiplayer game options including computer player toggle.
/// </summary>
public sealed class MultiplayerSetupScreen : UIScreen
{
    /// <inheritdoc/>
    public override string ScreenName => "MultiplayerSetup";

    /// <summary>Whether to include a computer player in the match.</summary>
    public bool IncludeAIPlayer { get; private set; } = true;

    /// <summary>Fired when the player wants to start the multiplayer match.</summary>
    public event Action<bool>? StartRequested;

    /// <summary>Fired when the Back button is pressed.</summary>
    public event Action? BackRequested;

    private readonly Button _aiToggleBtn;

    public MultiplayerSetupScreen()
    {
        // Title area
        var titlePanel = new Panel
        {
            Name = "MPTitle",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(0f, 80f),
            Size = new Vector2(600f, 60f),
            BackgroundColor = new Vector4(0f, 0f, 0f, 0f),
            DrawBorder = false,
        };
        AddWidget(titlePanel);

        // AI Toggle button
        _aiToggleBtn = new Button
        {
            Name = "AIToggle",
            Label = "Computer Player: ON",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-200f, 300f),
            Size = new Vector2(400f, 60f),
            FontSize = 20f,
        };
        _aiToggleBtn.Clicked += ToggleAI;
        AddWidget(_aiToggleBtn);

        // Start button
        var startBtn = new Button
        {
            Name = "StartMP",
            Label = "Start Match",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-160f, 420f),
            Size = new Vector2(320f, 60f),
            FontSize = 22f,
        };
        startBtn.Clicked += () => StartRequested?.Invoke(IncludeAIPlayer);
        AddWidget(startBtn);

        // Back button
        var backBtn = new Button
        {
            Name = "BackMP",
            Label = "Back",
            Anchor = Anchor.BottomLeft,
            Position = new Vector2(40f, -80f),
            Size = new Vector2(200f, 56f),
            FontSize = 20f,
        };
        backBtn.Clicked += () => BackRequested?.Invoke();
        AddWidget(backBtn);
    }

    private void ToggleAI()
    {
        IncludeAIPlayer = !IncludeAIPlayer;
        _aiToggleBtn.Label = IncludeAIPlayer
            ? "Computer Player: ON"
            : "Computer Player: OFF";
    }
}
