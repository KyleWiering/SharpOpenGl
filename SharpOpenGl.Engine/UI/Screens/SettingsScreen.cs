using OpenTK.Mathematics;
using SharpOpenGl.Engine.Persistence;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Engine.UI.Screens;

/// <summary>
/// Settings screen for audio, graphics, controls, and accessibility options.
/// </summary>
public sealed class SettingsScreen : UIScreen
{
    private static readonly string[] QualityTiers = ["High", "Medium", "Low"];

    /// <inheritdoc/>
    public override string ScreenName => "Settings";

    /// <summary>Fired when the player clicks Back.</summary>
    public event Action? BackRequested;

    private readonly SettingsManager _settings;
    private Button? _qualityButton;
    private Button? _vSyncButton;
    private Button? _edgeScrollButton;
    private Button? _colorblindButton;
    private Button? _visualAlertsButton;
    private Button? _highContrastButton;
    private Button? _masterValueButton;
    private Button? _musicValueButton;
    private Button? _sfxValueButton;
    private Button? _panValueButton;
    private Button? _zoomValueButton;
    private Button? _fontScaleButton;
    private Button? _difficultyButton;
    private Button? _hudMinimalButton;
    private Button? _rebindStubButton;
    private ScrollPanel _helpScroll = null!;
    private Label _helpLabel = null!;

    private const float HelpScrollWidth = 820f;
    private const float HelpScrollHeight = 72f;
    private const float HelpScrollPadding = 8f;

    public SettingsScreen(SettingsManager settings)
    {
        _settings = settings;
        BuildLayout();
        RefreshValueLabels();
    }

    private void BuildLayout()
    {
        var title = new Button
        {
            Name = "SettingsTitle",
            Label = "Settings",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-200f, 48f),
            Size = new Vector2(400f, 52f),
            FontSize = 28f,
            IsEnabled = false,
        };
        AddWidget(title);

        const float columnWidth = 380f;
        const float buttonHeight = 46f;
        const float rowGap = 8f;
        const float sectionGap = 14f;
        const float leftX = -410f;
        const float rightX = 10f;
        float leftY = 120f;
        float rightY = 120f;

        leftY = AddSectionHeader("Audio", leftX, leftY, columnWidth);
        leftY = AddVolumeRow("Master", leftX, leftY, columnWidth, buttonHeight, rowGap,
            () => AdjustVolume(masterDelta: 0.1f), () => AdjustVolume(masterDelta: -0.1f),
            out _masterValueButton);
        leftY = AddVolumeRow("Music", leftX, leftY, columnWidth, buttonHeight, rowGap,
            () => AdjustVolume(musicDelta: 0.1f), () => AdjustVolume(musicDelta: -0.1f),
            out _musicValueButton);
        leftY = AddVolumeRow("SFX", leftX, leftY, columnWidth, buttonHeight, rowGap,
            () => AdjustVolume(sfxDelta: 0.1f), () => AdjustVolume(sfxDelta: -0.1f),
            out _sfxValueButton);
        leftY += sectionGap;

        leftY = AddSectionHeader("Graphics", leftX, leftY, columnWidth);
        leftY = AddSettingButton("QualityCycle", CycleQualityTier, leftX, leftY, columnWidth, buttonHeight, rowGap,
            out _qualityButton);
        leftY = AddSettingButton("VSyncToggle", ToggleVSync, leftX, leftY, columnWidth, buttonHeight, rowGap,
            out _vSyncButton);
        leftY += sectionGap;

        rightY = AddSectionHeader("Controls", rightX, rightY, columnWidth);
        rightY = AddVolumeRow("Pan", rightX, rightY, columnWidth, buttonHeight, rowGap,
            () => AdjustCameraSpeed(panDelta: 0.1f), () => AdjustCameraSpeed(panDelta: -0.1f),
            out _panValueButton);
        rightY = AddVolumeRow("Zoom", rightX, rightY, columnWidth, buttonHeight, rowGap,
            () => AdjustCameraSpeed(zoomDelta: 0.1f), () => AdjustCameraSpeed(zoomDelta: -0.1f),
            out _zoomValueButton);
        rightY = AddSettingButton("EdgeScroll", ToggleEdgeScrolling, rightX, rightY, columnWidth, buttonHeight, rowGap,
            out _edgeScrollButton);
        rightY = AddSettingButton("Difficulty", CycleDefaultDifficulty, rightX, rightY, columnWidth, buttonHeight, rowGap,
            out _difficultyButton);
        rightY = AddSettingButton("KeyRebind", ShowRebindStub, rightX, rightY, columnWidth, buttonHeight, rowGap,
            out _rebindStubButton);
        rightY += sectionGap;

        rightY = AddSectionHeader("Accessibility", rightX, rightY, columnWidth);
        rightY = AddVolumeRow("Font", rightX, rightY, columnWidth, buttonHeight, rowGap,
            () => AdjustFontScale(0.1f), () => AdjustFontScale(-0.1f),
            out _fontScaleButton);
        rightY = AddSettingButton("Colorblind", CycleColorblindMode, rightX, rightY, columnWidth, buttonHeight, rowGap,
            out _colorblindButton);
        rightY = AddSettingButton("VisualAlerts", ToggleVisualAlerts, rightX, rightY, columnWidth, buttonHeight, rowGap,
            out _visualAlertsButton);
        rightY = AddSettingButton("Hi-Contrast", ToggleHighContrast, rightX, rightY, columnWidth, buttonHeight, rowGap,
            out _highContrastButton);
        rightY = AddSettingButton("HUD Minimal", ToggleHudMinimal, rightX, rightY, columnWidth, buttonHeight, rowGap,
            out _hudMinimalButton);

        _helpScroll = new ScrollPanel
        {
            Name = "SettingsHelpScroll",
            Anchor = Anchor.BottomCenter,
            Position = new Vector2(0f, -132f),
            Size = new Vector2(HelpScrollWidth, HelpScrollHeight),
            BackgroundColor = new Vector4(0.08f, 0.1f, 0.14f, 0.85f),
            ContentPadding = HelpScrollPadding,
        };
        _helpLabel = new Label
        {
            Name = "SettingsHelp",
            Anchor = Anchor.TopLeft,
            Position = Vector2.Zero,
            Size = new Vector2(HelpScrollWidth, 0f),
            FontSize = 14f,
            Padding = HelpScrollPadding,
            TextColor = MenuTheme.SubtitleColor,
        };
        _helpScroll.AddChild(_helpLabel);
        AddWidget(_helpScroll);

        var back = new Button
        {
            Name = "Back",
            Label = "Back",
            Anchor = Anchor.BottomCenter,
            Position = new Vector2(-160f, -64f),
            Size = new Vector2(320f, 52f),
            FontSize = 22f,
        };
        back.Clicked += () => BackRequested?.Invoke();
        AddWidget(back);
    }

    private float AddSectionHeader(string label, float x, float y, float width)
    {
        var header = new Button
        {
            Name = $"Section{label.Replace(" ", "")}",
            Label = label,
            Anchor = Anchor.TopCenter,
            Position = new Vector2(x, y),
            Size = new Vector2(width, 36f),
            FontSize = 18f,
            IsEnabled = false,
        };
        AddWidget(header);
        return y + 36f + 6f;
    }

    private float AddSettingButton(
        string name,
        Action handler,
        float x,
        float y,
        float width,
        float height,
        float gap,
        out Button button)
    {
        button = new Button
        {
            Name = name,
            Label = name,
            Anchor = Anchor.TopCenter,
            Position = new Vector2(x, y),
            Size = new Vector2(width, height),
            FontSize = 18f,
        };
        button.Clicked += handler;
        AddWidget(button);
        return y + height + gap;
    }

    private float AddVolumeRow(
        string channel,
        float x,
        float y,
        float width,
        float height,
        float gap,
        Action onPlus,
        Action onMinus,
        out Button valueButton)
    {
        const float controlGap = 8f;
        float minusWidth = 72f;
        float plusWidth = 72f;
        float valueWidth = width - minusWidth - plusWidth - controlGap * 2f;

        var minus = new Button
        {
            Name = $"{channel}-",
            Label = "−",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(x, y),
            Size = new Vector2(minusWidth, height),
            FontSize = 22f,
        };
        minus.Clicked += onMinus;
        AddWidget(minus);

        valueButton = new Button
        {
            Name = $"{channel}Value",
            Label = channel,
            Anchor = Anchor.TopCenter,
            Position = new Vector2(x + minusWidth + controlGap, y),
            Size = new Vector2(valueWidth, height),
            FontSize = 16f,
            IsEnabled = false,
        };
        AddWidget(valueButton);

        var plus = new Button
        {
            Name = $"{channel}+",
            Label = "+",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(x + minusWidth + controlGap + valueWidth + controlGap, y),
            Size = new Vector2(plusWidth, height),
            FontSize = 22f,
        };
        plus.Clicked += onPlus;
        AddWidget(plus);

        return y + height + gap;
    }

    private void RefreshValueLabels()
    {
        if (_qualityButton != null)
            _qualityButton.Label = $"Quality: {_settings.Current.QualityTier}";
        if (_vSyncButton != null)
            _vSyncButton.Label = FormatOnOff("VSync", _settings.Current.VSync);
        if (_edgeScrollButton != null)
            _edgeScrollButton.Label = FormatOnOff("Edge Scroll", _settings.Current.EdgeScrolling);
        if (_colorblindButton != null)
            _colorblindButton.Label = $"Colorblind: {FormatColorblindMode(_settings.Current.Accessibility.ColorblindMode)}";
        if (_visualAlertsButton != null)
            _visualAlertsButton.Label = FormatOnOff("Visual Alerts", _settings.Current.Accessibility.VisualAlerts);
        if (_highContrastButton != null)
            _highContrastButton.Label = FormatOnOff("Hi-Contrast", _settings.Current.Accessibility.HighContrastSelections);
        if (_masterValueButton != null)
            _masterValueButton.Label = $"Master: {FormatPercent(_settings.Current.MasterVolume)}";
        if (_musicValueButton != null)
            _musicValueButton.Label = $"Music: {FormatPercent(_settings.Current.MusicVolume)}";
        if (_sfxValueButton != null)
            _sfxValueButton.Label = $"SFX: {FormatPercent(_settings.Current.SfxVolume)}";
        if (_panValueButton != null)
            _panValueButton.Label = $"Pan: {_settings.Current.CameraPanSpeed:0.0}";
        if (_zoomValueButton != null)
            _zoomValueButton.Label = $"Zoom: {_settings.Current.CameraZoomSpeed:0.0}";
        if (_fontScaleButton != null)
            _fontScaleButton.Label = $"Font: {_settings.Current.Accessibility.FontScale:0.0}";
        if (_difficultyButton != null)
            _difficultyButton.Label = $"Skirmish: {_settings.Current.DefaultSkirmishDifficulty}";
        if (_rebindStubButton != null)
            _rebindStubButton.Label = $"Key Rebind: {_settings.Current.KeyBindingOverrides.Count} override(s)";
        if (_hudMinimalButton != null)
            _hudMinimalButton.Label = FormatOnOff("HUD Minimal", _settings.Current.HudMinimalMode);

        RefreshHelpScroll();
    }

    private void RefreshHelpScroll()
    {
        _helpLabel.Text = BuildHelpText();
        _helpScroll.SyncLabelWrapWidths();
        float labelHeight = _helpLabel.MeasureContentHeight();
        _helpLabel.Size = new Vector2(_helpScroll.Size.X, labelHeight);
        _helpScroll.RecalculateContentHeight(_helpScroll.Size);
    }

    private string BuildHelpText()
    {
        var lines = new List<string>
        {
            $"Graphics quality tier: {_settings.Current.QualityTier}",
            $"Colorblind assist: {FormatColorblindModeLong(_settings.Current.Accessibility.ColorblindMode)}",
            $"Default skirmish difficulty: {_settings.Current.DefaultSkirmishDifficulty}",
            $"Key rebind overrides: {_settings.Current.KeyBindingOverrides.Count}",
        };

        if (_settings.Current.KeyBindingOverrides.Count > 0)
        {
            foreach (var pair in _settings.Current.KeyBindingOverrides.OrderBy(p => p.Key, StringComparer.Ordinal))
                lines.Add($"  {pair.Key} → {pair.Value}");
        }

        return string.Join('\n', lines);
    }

    private static string FormatColorblindModeLong(ColorblindMode mode) => mode switch
    {
        ColorblindMode.RedGreen => "Red-Green (protanopia / deuteranopia simulation)",
        ColorblindMode.BlueYellow => "Blue-Yellow (tritanopia simulation)",
        ColorblindMode.Monochrome => "Monochrome (achromatopsia simulation)",
        _ => "None",
    };

    private static string FormatOnOff(string prefix, bool enabled) => $"{prefix}: {(enabled ? "On" : "Off")}";

    private static string FormatPercent(float value) => $"{MathF.Round(value * 100f):0}%";

    private static string FormatColorblindMode(ColorblindMode mode) => mode switch
    {
        ColorblindMode.RedGreen => "Red-Green",
        ColorblindMode.BlueYellow => "Blue-Yellow",
        ColorblindMode.Monochrome => "Mono",
        _ => "None",
    };

    private void AdjustVolume(float masterDelta = 0f, float musicDelta = 0f, float sfxDelta = 0f)
    {
        if (masterDelta != 0f)
            _settings.Current.MasterVolume = Math.Clamp(_settings.Current.MasterVolume + masterDelta, 0f, 1f);
        if (musicDelta != 0f)
            _settings.Current.MusicVolume = Math.Clamp(_settings.Current.MusicVolume + musicDelta, 0f, 1f);
        if (sfxDelta != 0f)
            _settings.Current.SfxVolume = Math.Clamp(_settings.Current.SfxVolume + sfxDelta, 0f, 1f);
        _settings.Save();
        RefreshValueLabels();
    }

    private void AdjustFontScale(float delta)
    {
        _settings.Current.Accessibility.FontScale =
            Math.Clamp(_settings.Current.Accessibility.FontScale + delta, 0.5f, 3f);
        _settings.Save();
        RefreshValueLabels();
    }

    private void AdjustCameraSpeed(float panDelta = 0f, float zoomDelta = 0f)
    {
        if (panDelta != 0f)
            _settings.Current.CameraPanSpeed = Math.Clamp(_settings.Current.CameraPanSpeed + panDelta, 0.1f, 5f);
        if (zoomDelta != 0f)
            _settings.Current.CameraZoomSpeed = Math.Clamp(_settings.Current.CameraZoomSpeed + zoomDelta, 0.1f, 5f);
        _settings.Save();
        RefreshValueLabels();
    }

    private void CycleQualityTier()
    {
        int index = Array.IndexOf(QualityTiers, _settings.Current.QualityTier);
        if (index < 0) index = 0;
        _settings.Current.QualityTier = QualityTiers[(index + 1) % QualityTiers.Length];
        _settings.Save();
        RefreshValueLabels();
    }

    private void ToggleVSync()
    {
        _settings.Current.VSync = !_settings.Current.VSync;
        _settings.Save();
        RefreshValueLabels();
    }

    private void ToggleEdgeScrolling()
    {
        _settings.Current.EdgeScrolling = !_settings.Current.EdgeScrolling;
        _settings.Save();
        RefreshValueLabels();
    }

    private void CycleColorblindMode()
    {
        _settings.Current.Accessibility.ColorblindMode = _settings.Current.Accessibility.ColorblindMode switch
        {
            ColorblindMode.None => ColorblindMode.RedGreen,
            ColorblindMode.RedGreen => ColorblindMode.BlueYellow,
            ColorblindMode.BlueYellow => ColorblindMode.Monochrome,
            _ => ColorblindMode.None,
        };
        _settings.Save();
        RefreshValueLabels();
    }

    private void ToggleVisualAlerts()
    {
        _settings.Current.Accessibility.VisualAlerts = !_settings.Current.Accessibility.VisualAlerts;
        _settings.Save();
        RefreshValueLabels();
    }

    private void ToggleHighContrast()
    {
        _settings.Current.Accessibility.HighContrastSelections =
            !_settings.Current.Accessibility.HighContrastSelections;
        _settings.Save();
        RefreshValueLabels();
    }

    private void ToggleHudMinimal()
    {
        _settings.Current.HudMinimalMode = !_settings.Current.HudMinimalMode;
        _settings.Save();
        RefreshValueLabels();
    }

    private void CycleDefaultDifficulty()
    {
        _settings.Current.DefaultSkirmishDifficulty = _settings.Current.DefaultSkirmishDifficulty switch
        {
            "Easy" => "Normal",
            "Normal" => "Hard",
            _ => "Easy",
        };
        _settings.Save();
        RefreshValueLabels();
    }

    /// <summary>Stub cycle — persists a sample override to prove JSON rebind path (P11-D05).</summary>
    private void ShowRebindStub()
    {
        if (_settings.Current.KeyBindingOverrides.ContainsKey("Pause"))
            _settings.Current.KeyBindingOverrides.Remove("Pause");
        else
            _settings.Current.KeyBindingOverrides["Pause"] = "P";
        _settings.Save();
        RefreshValueLabels();
    }
}