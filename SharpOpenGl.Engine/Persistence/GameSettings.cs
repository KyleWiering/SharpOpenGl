namespace SharpOpenGl.Engine.Persistence;

/// <summary>
/// Colorblind-accessibility filter applied to the rendered output.
/// </summary>
public enum ColorblindMode
{
    /// <summary>No filter — full-color rendering.</summary>
    None,

    /// <summary>Red-green deficiency (deuteranopia / protanopia) simulation.</summary>
    RedGreen,

    /// <summary>Blue-yellow deficiency (tritanopia) simulation.</summary>
    BlueYellow,

    /// <summary>Monochrome / grayscale output for complete color blindness.</summary>
    Monochrome,
}

/// <summary>
/// Accessibility options surfaced in the Settings screen.
/// </summary>
public sealed class AccessibilitySettings
{
    /// <summary>Active colorblind filter. Default is <see cref="ColorblindMode.None"/>.</summary>
    public ColorblindMode ColorblindMode { get; set; } = ColorblindMode.None;

    /// <summary>
    /// Global font-scale multiplier applied to all UI text.
    /// 1.0 = default; 1.5 = 50 % larger.
    /// Clamped to [0.5, 3.0] by the settings manager.
    /// </summary>
    public float FontScale { get; set; } = 1.0f;

    /// <summary>
    /// When <c>true</c>, game-critical messages are also communicated via
    /// screen-edge flashes (removes reliance on audio cues alone).
    /// </summary>
    public bool VisualAlerts { get; set; } = false;

    /// <summary>
    /// When <c>true</c>, unit selection rings use high-contrast outlines.
    /// </summary>
    public bool HighContrastSelections { get; set; } = false;
}

/// <summary>
/// Persistent user-configurable settings for the game.
/// Serialised to/from JSON by <see cref="SettingsManager"/>.
/// </summary>
public sealed class GameSettings
{
    // ── Audio ─────────────────────────────────────────────────────────────────

    /// <summary>Master volume in [0, 1].</summary>
    public float MasterVolume { get; set; } = 1.0f;

    /// <summary>Music volume in [0, 1] (relative to master).</summary>
    public float MusicVolume { get; set; } = 0.7f;

    /// <summary>Sound-effects volume in [0, 1] (relative to master).</summary>
    public float SfxVolume { get; set; } = 1.0f;

    // ── Display ───────────────────────────────────────────────────────────────

    /// <summary>Horizontal resolution in pixels (desktop only; browser uses canvas size).</summary>
    public int ResolutionWidth { get; set; } = 1920;

    /// <summary>Vertical resolution in pixels.</summary>
    public int ResolutionHeight { get; set; } = 1080;

    /// <summary>Whether the application runs in fullscreen mode.</summary>
    public bool Fullscreen { get; set; } = false;

    /// <summary>Vertical sync enabled flag.</summary>
    public bool VSync { get; set; } = true;

    // ── Graphics quality ──────────────────────────────────────────────────────

    /// <summary>
    /// Selected quality tier ("High", "Medium", or "Low").
    /// Mapped to <see cref="Rendering.PerformanceTier"/> by the engine.
    /// </summary>
    public string QualityTier { get; set; } = "High";

    // ── Camera / controls ─────────────────────────────────────────────────────

    /// <summary>Camera pan speed multiplier.</summary>
    public float CameraPanSpeed { get; set; } = 1.0f;

    /// <summary>Camera zoom speed multiplier.</summary>
    public float CameraZoomSpeed { get; set; } = 1.0f;

    /// <summary>Whether the camera edge-scrolls when the pointer is near the viewport edge.</summary>
    public bool EdgeScrolling { get; set; } = true;

    /// <summary>
    /// Optional keyboard binding overrides keyed by <c>controls.json</c> action names.
    /// Full remapping UI is deferred; JSON persistence enables future Settings integration (P11-D05).
    /// </summary>
    public Dictionary<string, string> KeyBindingOverrides { get; set; } = new();

    /// <summary>Default skirmish difficulty persisted for Multiplayer Setup (Easy / Normal / Hard).</summary>
    public string DefaultSkirmishDifficulty { get; set; } = "Normal";

    /// <summary>When true, non-essential HUD chrome is hidden for lower cognitive load (P11-D07).</summary>
    public bool HudMinimalMode { get; set; }

    // ── Accessibility ─────────────────────────────────────────────────────────

    /// <summary>Accessibility overrides.</summary>
    public AccessibilitySettings Accessibility { get; set; } = new();

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Return a copy of this object with all numeric values clamped to valid ranges.</summary>
    public GameSettings Clamped()
    {
        return new GameSettings
        {
            MasterVolume       = Math.Clamp(MasterVolume, 0f, 1f),
            MusicVolume        = Math.Clamp(MusicVolume,  0f, 1f),
            SfxVolume          = Math.Clamp(SfxVolume,    0f, 1f),
            ResolutionWidth    = Math.Max(640, ResolutionWidth),
            ResolutionHeight   = Math.Max(480, ResolutionHeight),
            Fullscreen         = Fullscreen,
            VSync              = VSync,
            QualityTier        = QualityTier,
            CameraPanSpeed     = Math.Clamp(CameraPanSpeed,  0.1f, 5f),
            CameraZoomSpeed    = Math.Clamp(CameraZoomSpeed, 0.1f, 5f),
            EdgeScrolling      = EdgeScrolling,
            KeyBindingOverrides = new Dictionary<string, string>(KeyBindingOverrides),
            DefaultSkirmishDifficulty = DefaultSkirmishDifficulty,
            HudMinimalMode     = HudMinimalMode,
            Accessibility      = new AccessibilitySettings
            {
                ColorblindMode         = Accessibility.ColorblindMode,
                FontScale              = Math.Clamp(Accessibility.FontScale, 0.5f, 3f),
                VisualAlerts           = Accessibility.VisualAlerts,
                HighContrastSelections = Accessibility.HighContrastSelections,
            },
        };
    }
}
