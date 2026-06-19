using OpenTK.Mathematics;
using SharpOpenGl.Engine.Persistence;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Engine.UI.Screens;

/// <summary>
/// Settings screen for audio, display, and accessibility options.
/// </summary>
public sealed class SettingsScreen : UIScreen
{
    /// <inheritdoc/>
    public override string ScreenName => "Settings";

    /// <summary>Fired when the player clicks Back.</summary>
    public event Action? BackRequested;

    private readonly SettingsManager _settings;

    public SettingsScreen(SettingsManager settings)
    {
        _settings = settings;
        BuildLayout();
    }

    private void BuildLayout()
    {
        var title = new Button
        {
            Name = "SettingsTitle",
            Label = "Settings",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-200f, 80f),
            Size = new Vector2(400f, 56f),
            FontSize = 28f,
            IsEnabled = false,
        };
        AddWidget(title);

        float y = 200f;
        AddSettingButton("Master Volume +", () => AdjustVolume(0.1f), y);
        AddSettingButton("Master Volume -", () => AdjustVolume(-0.1f), y + 70f);
        AddSettingButton("Font Scale +", () => AdjustFontScale(0.1f), y + 140f);
        AddSettingButton("Font Scale -", () => AdjustFontScale(-0.1f), y + 210f);

        var back = new Button
        {
            Name = "Back",
            Label = "Back",
            Anchor = Anchor.BottomCenter,
            Position = new Vector2(-160f, -80f),
            Size = new Vector2(320f, 56f),
            FontSize = 22f,
        };
        back.Clicked += () => BackRequested?.Invoke();
        AddWidget(back);
    }

    private void AddSettingButton(string label, Action handler, float y)
    {
        var btn = new Button
        {
            Name = label.Replace(" ", ""),
            Label = label,
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-200f, y),
            Size = new Vector2(400f, 56f),
            FontSize = 20f,
        };
        btn.Clicked += handler;
        AddWidget(btn);
    }

    private void AdjustVolume(float delta)
    {
        _settings.Current.MasterVolume = Math.Clamp(_settings.Current.MasterVolume + delta, 0f, 1f);
        _settings.Save();
    }

    private void AdjustFontScale(float delta)
    {
        _settings.Current.Accessibility.FontScale =
            Math.Clamp(_settings.Current.Accessibility.FontScale + delta, 0.5f, 3f);
        _settings.Save();
    }
}