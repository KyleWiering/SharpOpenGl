using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Events;

namespace SharpOpenGl.Engine.Missions;

/// <summary>
/// Executes the scripted actions defined in a <see cref="TriggerActionDefinition"/>.
/// Provides hooks (delegates) that the game layer can set to integrate with the
/// entity factory, UI, and camera systems without a hard compile-time dependency.
/// </summary>
public sealed class ScriptedEventRunner
{
    // ── Hooks set by the game layer ───────────────────────────────────────────

    /// <summary>
    /// Spawn a named unit definition at a grid position [x, y].
    /// Signature: (definitionId, worldX, worldY) → new entity.
    /// Default: no-op.
    /// </summary>
    public Action<string, float, float>? SpawnUnit { get; set; }

    /// <summary>
    /// Display a dialog line on the HUD.
    /// Signature: (speaker, text).
    /// Default: writes to console.
    /// </summary>
    public Action<string, string>? ShowDialog { get; set; }

    /// <summary>
    /// Pan the camera to a world position.
    /// Signature: (x, y).
    /// Default: no-op.
    /// </summary>
    public Action<float, float>? PanCamera { get; set; }

    /// <summary>
    /// Return the current resource amount for a named resource type (player 1).
    /// Default: always returns 0.
    /// </summary>
    public Func<string, float>? GetResourceAmount { get; set; }

    /// <summary>
    /// Return the current hero world position for area checks.
    /// Default: always returns origin.
    /// </summary>
    public Func<Vector2>? GetHeroPosition { get; set; }

    private readonly EventBus? _bus;

    public ScriptedEventRunner(EventBus? bus = null) => _bus = bus;

    // ── Execute ───────────────────────────────────────────────────────────────

    /// <summary>Execute a single scripted action within the current mission context.</summary>
    public void Execute(TriggerActionDefinition action, MissionState mission, World world)
    {
        switch (action.Type)
        {
            case "spawn_units":
                ExecuteSpawnUnits(action);
                break;

            case "dialog":
                ExecuteDialog(action);
                break;

            case "camera_pan":
                ExecuteCameraPan(action);
                break;

            case "complete_objective":
                ExecuteCompleteObjective(action, mission);
                break;

            case "fail_mission":
                ExecuteFailMission(action, mission);
                break;

            default:
                Console.WriteLine($"[ScriptedEventRunner] Unknown action type: {action.Type}");
                break;
        }
    }

    // ── Action implementations ────────────────────────────────────────────────

    private void ExecuteSpawnUnits(TriggerActionDefinition action)
    {
        if (SpawnUnit == null) return;

        float x = action.Position?.Length >= 1 ? action.Position[0] : 0f;
        float y = action.Position?.Length >= 2 ? action.Position[1] : 0f;

        foreach (string unit in action.Units)
            SpawnUnit(unit, x, y);
    }

    private void ExecuteDialog(TriggerActionDefinition action)
    {
        string speaker = action.Speaker ?? "Unknown";
        string text    = action.Text    ?? string.Empty;

        if (ShowDialog != null)
            ShowDialog(speaker, text);
        else
            Console.WriteLine($"[Dialog] {speaker}: {text}");

        _bus?.Publish(new DialogEvent(speaker, text));
    }

    private void ExecuteCameraPan(TriggerActionDefinition action)
    {
        if (PanCamera == null) return;

        float x = action.Position?.Length >= 1 ? action.Position[0] : 0f;
        float y = action.Position?.Length >= 2 ? action.Position[1] : 0f;
        PanCamera(x, y);
    }

    private void ExecuteCompleteObjective(TriggerActionDefinition action, MissionState mission)
    {
        if (string.IsNullOrEmpty(action.ObjectiveId)) return;

        ObjectiveRecord? obj = mission.FindObjective(action.ObjectiveId);
        if (obj == null || obj.IsCompleted) return;

        obj.Complete();
        _bus?.Publish(new ObjectiveChangedEvent(
            mission.Definition.Id, action.ObjectiveId, completed: true));
    }

    private void ExecuteFailMission(TriggerActionDefinition action, MissionState mission)
    {
        string reason = action.Reason ?? "scripted";
        mission.SetDefeat(reason);
        _bus?.Publish(new MissionFailedEvent(mission.Definition.Id, reason));
    }
}
