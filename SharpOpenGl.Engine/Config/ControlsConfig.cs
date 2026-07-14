using SharpOpenGl.Engine.Assets;

namespace SharpOpenGl.Engine.Config;

/// <summary>
/// Desktop and touch bindings from <c>GameData/Config/controls.json</c>.
/// Ship Control Bar shortcuts live in <see cref="ShipControlBarBindings"/>.
/// </summary>
public sealed class ControlsConfig
{
    public Dictionary<string, string>? Keyboard { get; set; }
    public Dictionary<string, string>? Mouse { get; set; }
    public Dictionary<string, string>? Touch { get; set; }
    public ShipControlBarBindings? ShipControlBar { get; set; }

    public static ControlsConfig Load(string gameDataRoot) =>
        Load(gameDataRoot, keyBindingOverrides: null);

    /// <summary>Load controls and apply optional per-user key overrides from <see cref="GameSettings"/>.</summary>
    public static ControlsConfig Load(string gameDataRoot, IReadOnlyDictionary<string, string>? keyBindingOverrides)
    {
        string path = Path.Combine(gameDataRoot, "Config", "controls.json");
        var loaded = JsonLoader.Load<ControlsConfig>(path);
        if (loaded == null)
            loaded = Defaults;

        loaded.ShipControlBar ??= ShipControlBarBindings.Defaults;
        if (keyBindingOverrides is { Count: > 0 })
            loaded.ApplyKeyBindingOverrides(keyBindingOverrides);
        return loaded;
    }

    /// <summary>Merge persisted key overrides into the loaded keyboard map (stub for P11-D05 remapping).</summary>
    public void ApplyKeyBindingOverrides(IReadOnlyDictionary<string, string> overrides)
    {
        Keyboard ??= new Dictionary<string, string>();
        foreach (var (action, key) in overrides)
        {
            if (!string.IsNullOrWhiteSpace(action) && !string.IsNullOrWhiteSpace(key))
                Keyboard[action] = key;
        }
    }

    public static ControlsConfig Defaults { get; } = new()
    {
        ShipControlBar = ShipControlBarBindings.Defaults,
    };
}

/// <summary>Keyboard shortcuts mirroring the gameplay Ship Control Bar (units selected).</summary>
public sealed class ShipControlBarBindings
{
    public string Move { get; set; } = "M";
    public string Stop { get; set; } = "S";
    public string StopAlt { get; set; } = "X";
    public string Patrol { get; set; } = "P";
    public string Attack { get; set; } = "T";
    public string AttackMove { get; set; } = "A";
    public string AttackMoveAlt { get; set; } = "F";
    public string HoldPosition { get; set; } = "H";
    public string DefensiveStance { get; set; } = "V";
    public string Harvest { get; set; } = "H";
    public string FormationCycle { get; set; } = "G";
    public string BuildStructures { get; set; } = "B";

    public static ShipControlBarBindings Defaults { get; } = new();
}