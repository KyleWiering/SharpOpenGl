using System.Reflection;
using SharpOpenGl.Engine.Persistence;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class SettingsScreenTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"sg_settings_{Guid.NewGuid():N}");
    private readonly SettingsManager _settings;

    public SettingsScreenTests()
    {
        Directory.CreateDirectory(_dir);
        _settings = new SettingsManager(Path.Combine(_dir, "settings.json"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void SettingsScreen_exposes_audio_graphics_controls_accessibility_sections()
    {
        var screen = new SettingsScreen(_settings);

        Assert.NotNull(FindWidget<Button>(screen, "SectionAudio"));
        Assert.NotNull(FindWidget<Button>(screen, "SectionGraphics"));
        Assert.NotNull(FindWidget<Button>(screen, "SectionControls"));
        Assert.NotNull(FindWidget<Button>(screen, "SectionAccessibility"));
    }

    [Fact]
    public void Quality_cycle_persists_tier()
    {
        var screen = new SettingsScreen(_settings);
        var cycle = FindWidget<Button>(screen, "QualityCycle");
        Assert.NotNull(cycle);
        Assert.Equal("Quality: High", cycle!.Label);

        cycle.Activate();
        Assert.Equal("Medium", _settings.Current.QualityTier);
        Assert.Equal("Quality: Medium", cycle.Label);

        cycle.Activate();
        Assert.Equal("Low", _settings.Current.QualityTier);
        Assert.Equal("Quality: Low", cycle.Label);
    }

    [Fact]
    public void Settings_toggle_buttons_show_live_values()
    {
        _settings.Current.VSync = true;
        _settings.Current.EdgeScrolling = false;
        _settings.Current.Accessibility.VisualAlerts = true;
        _settings.Current.Accessibility.HighContrastSelections = false;
        _settings.Current.MasterVolume = 0.7f;
        _settings.Current.MusicVolume = 0.5f;
        _settings.Current.SfxVolume = 0.3f;
        _settings.Current.CameraPanSpeed = 1.2f;
        _settings.Current.CameraZoomSpeed = 0.8f;
        _settings.Current.Accessibility.FontScale = 1.1f;

        var screen = new SettingsScreen(_settings);

        Assert.Equal("Quality: High", FindWidget<Button>(screen, "QualityCycle")!.Label);
        Assert.Equal("VSync: On", FindWidget<Button>(screen, "VSyncToggle")!.Label);
        Assert.Equal("Edge Scroll: Off", FindWidget<Button>(screen, "EdgeScroll")!.Label);
        Assert.Equal("Colorblind: None", FindWidget<Button>(screen, "Colorblind")!.Label);
        Assert.Equal("Visual Alerts: On", FindWidget<Button>(screen, "VisualAlerts")!.Label);
        Assert.Equal("Hi-Contrast: Off", FindWidget<Button>(screen, "Hi-Contrast")!.Label);
        Assert.Equal("Master: 70%", FindWidget<Button>(screen, "MasterValue")!.Label);
        Assert.Equal("Music: 50%", FindWidget<Button>(screen, "MusicValue")!.Label);
        Assert.Equal("SFX: 30%", FindWidget<Button>(screen, "SFXValue")!.Label);
        Assert.Equal("Pan: 1.2", FindWidget<Button>(screen, "PanValue")!.Label);
        Assert.Equal("Zoom: 0.8", FindWidget<Button>(screen, "ZoomValue")!.Label);
        Assert.Equal("Font: 1.1", FindWidget<Button>(screen, "FontValue")!.Label);
    }

    [Fact]
    public void Edge_scroll_toggle_persists()
    {
        _settings.Current.EdgeScrolling = true;
        var screen = new SettingsScreen(_settings);
        var toggle = FindWidget<Button>(screen, "EdgeScroll");
        Assert.NotNull(toggle);

        toggle!.Activate();
        Assert.False(_settings.Current.EdgeScrolling);
    }

    [Fact]
    public void Hud_minimal_and_difficulty_toggles_persist()
    {
        var screen = new SettingsScreen(_settings);
        var hud = FindWidget<Button>(screen, "HUD Minimal");
        var difficulty = FindWidget<Button>(screen, "Difficulty");
        Assert.NotNull(hud);
        Assert.NotNull(difficulty);

        hud!.Activate();
        Assert.True(_settings.Current.HudMinimalMode);
        Assert.Equal("HUD Minimal: On", hud.Label);

        difficulty!.Activate();
        Assert.Equal("Hard", _settings.Current.DefaultSkirmishDifficulty);
    }

    [Fact]
    public void Key_rebind_stub_persists_override_count()
    {
        var screen = new SettingsScreen(_settings);
        var rebind = FindWidget<Button>(screen, "KeyRebind");
        Assert.NotNull(rebind);
        Assert.Equal("Key Rebind: 0 override(s)", rebind!.Label);

        rebind.Activate();
        Assert.Single(_settings.Current.KeyBindingOverrides);
        Assert.Equal("Key Rebind: 1 override(s)", rebind.Label);
    }

    [Fact]
    public void Music_volume_adjustment_persists()
    {
        var screen = new SettingsScreen(_settings);
        var plus = FindWidget<Button>(screen, "Music+");
        var value = FindWidget<Button>(screen, "MusicValue");
        Assert.NotNull(plus);
        Assert.NotNull(value);
        Assert.Equal("Music: 70%", value!.Label);

        plus!.Activate();
        Assert.Equal(0.8f, _settings.Current.MusicVolume, 0.001f);
        Assert.Equal("Music: 80%", value.Label);
    }

    private static T? FindWidget<T>(UIScreen screen, string name) where T : Widget
    {
        foreach (Widget root in GetRoots(screen))
        {
            T? match = FindWidgetInTree<T>(root, name);
            if (match != null)
                return match;
        }
        return null;
    }

    private static T? FindWidgetInTree<T>(Widget widget, string name) where T : Widget
    {
        if (widget.Name == name && widget is T match)
            return match;

        foreach (Widget child in widget.Children)
        {
            T? childMatch = FindWidgetInTree<T>(child, name);
            if (childMatch != null)
                return childMatch;
        }

        return null;
    }

    private static IEnumerable<Widget> GetRoots(UIScreen screen)
    {
        FieldInfo? field = typeof(UIScreen).GetField("_roots", BindingFlags.Instance | BindingFlags.NonPublic);
        return (IReadOnlyList<Widget>)(field?.GetValue(screen) ?? Array.Empty<Widget>());
    }
}