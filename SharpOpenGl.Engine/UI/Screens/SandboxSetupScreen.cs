using OpenTK.Mathematics;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Engine.UI.Screens;

/// <summary>Configuration captured when the player starts a sandbox universe.</summary>
public sealed record SandboxSetupResult(string SeedText, int ParsedSeed);

/// <summary>
/// Setup screen for sandbox mode with optional world seed input.
/// </summary>
public sealed class SandboxSetupScreen : UIScreen
{
    private const float SeedFieldWidth = 520f;
    private const float SeedFieldHeight = 52f;

    private readonly TextField _seedField;
    private readonly Button _startBtn;
    private readonly Random _random = new();

    /// <inheritdoc/>
    public override string ScreenName => "SandboxSetup";

    /// <summary>Fired when the player confirms sandbox start.</summary>
    public event Action<SandboxSetupResult>? StartRequested;

    /// <summary>Fired when the Back button is pressed.</summary>
    public event Action? BackRequested;

    public SandboxSetupScreen()
    {
        AddWidget(new MenuStarfieldBackground
        {
            Name = "SandboxStarfield",
            Anchor = Anchor.Stretch,
        });

        AddWidget(new Label
        {
            Name = "SandboxTitle",
            Text = "SANDBOX UNIVERSE",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(0f, 48f),
            Size = new Vector2(900f, 48f),
            FontSize = 32f,
            TextColor = MenuTheme.ScreenHeadingColor,
        });

        AddWidget(new Label
        {
            Name = "SandboxSubtitle",
            Text = "Same seed always generates the same procedural layout.",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(0f, 96f),
            Size = new Vector2(900f, 32f),
            FontSize = 18f,
            TextColor = MenuTheme.BodyTextColor,
        });

        AddWidget(new Label
        {
            Name = "SeedLabel",
            Text = "World seed",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-SeedFieldWidth / 2f, 156f),
            Size = new Vector2(SeedFieldWidth, 28f),
            FontSize = 18f,
            TextColor = MenuTheme.BodyTextColor,
        });

        _seedField = new TextField
        {
            Name = "SeedField",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-SeedFieldWidth / 2f, 192f),
            Size = new Vector2(SeedFieldWidth, SeedFieldHeight),
            Placeholder = "Enter seed…",
            FontSize = 20f,
            IsKeyboardFocused = true,
        };
        AddWidget(_seedField);

        var randomizeBtn = new Button
        {
            Name = "RandomizeSeed",
            Label = "Randomize",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(SeedFieldWidth / 2f + 16f, 192f),
            Size = new Vector2(180f, SeedFieldHeight),
            FontSize = 20f,
        };
        MenuTheme.ApplyNavButton(randomizeBtn, showGlow: false);
        randomizeBtn.Clicked += RandomizeSeed;
        AddWidget(randomizeBtn);

        _startBtn = new Button
        {
            Name = "StartSandbox",
            Label = "Start Sandbox",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-170f, 638f),
            Size = new Vector2(340f, 58f),
            FontSize = 22f,
        };
        MenuTheme.ApplyNavButton(_startBtn);
        _startBtn.Clicked += OnStartClicked;
        AddWidget(_startBtn);

        var backBtn = new Button
        {
            Name = "BackSandbox",
            Label = "Back",
            Anchor = Anchor.BottomLeft,
            Position = new Vector2(40f, -80f),
            Size = new Vector2(200f, 56f),
            FontSize = 20f,
        };
        MenuTheme.ApplyNavButton(backBtn);
        backBtn.Clicked += () => BackRequested?.Invoke();
        AddWidget(backBtn);
    }

    /// <summary>Seed text field for tests and diagnostics.</summary>
    public TextField SeedField => _seedField;

    /// <summary>Builds the setup result from the current seed field value.</summary>
    public SandboxSetupResult BuildResult()
    {
        string seedText = _seedField.Value;
        return new SandboxSetupResult(seedText, ProceduralSeedHelper.ParseSeed(seedText));
    }

    /// <summary>Fills the seed field with a random 8-character alphanumeric string.</summary>
    public void RandomizeSeed()
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var chars = new char[8];
        for (int i = 0; i < chars.Length; i++)
            chars[i] = alphabet[_random.Next(alphabet.Length)];
        _seedField.Value = new string(chars);
    }

    /// <inheritdoc/>
    public override bool HandleKey(UIKey key)
    {
        if (_seedField.HandleKey(key))
            return true;

        switch (key)
        {
            case UIKey.Enter:
                OnStartClicked();
                return true;
            case UIKey.Escape:
                BackRequested?.Invoke();
                return true;
            default:
                return false;
        }
    }

    /// <inheritdoc/>
    public override bool HandleChar(char c) => _seedField.HandleChar(c);

    private void OnStartClicked()
    {
        StartRequested?.Invoke(BuildResult());
    }
}