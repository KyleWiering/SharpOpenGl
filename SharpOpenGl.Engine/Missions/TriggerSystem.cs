using OpenTK.Mathematics;
using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Events;

namespace SharpOpenGl.Engine.Missions;

/// <summary>
/// ECS <see cref="GameSystem"/> that evaluates trigger conditions each frame and
/// executes their scripted actions when conditions are met.
/// <para>
/// Supported trigger condition types:
/// <list type="bullet">
///   <item><c>timer</c> — fires after N elapsed seconds (<c>seconds</c> field).</item>
///   <item><c>area_enter</c> — fires when any entity enters radius of <c>position</c>.</item>
///   <item><c>kill_count</c> — fires when kill counter reaches <c>count</c>.</item>
///   <item><c>resource_threshold</c> — fires when player has at least <c>threshold</c>
///         of <c>resourceType</c>.</item>
/// </list>
/// Supported action types: <c>spawn_units</c>, <c>dialog</c>, <c>camera_pan</c>.
/// </para>
/// </summary>
public sealed class TriggerSystem : GameSystem
{
    private readonly MissionState _state;
    private readonly EventBus _bus;
    private readonly ResourceManager? _resources;
    private readonly UnitFactory? _unitFactory;
    private readonly AssetManager? _assets;

    /// <summary>Player ID used for resource-threshold checks.</summary>
    public int PlayerId { get; set; } = 0;

    /// <param name="state">The running mission state.</param>
    /// <param name="bus">Event bus for publishing <see cref="TriggerFiredEvent"/> and actions.</param>
    /// <param name="resources">Optional resource manager for resource-threshold triggers.</param>
    /// <param name="unitFactory">Optional factory for <c>spawn_units</c> actions.</param>
    /// <param name="assets">Optional asset manager to load <see cref="EntityDefinition"/>s for spawning.</param>
    public TriggerSystem(
        MissionState state,
        EventBus bus,
        ResourceManager? resources = null,
        UnitFactory? unitFactory = null,
        AssetManager? assets = null)
    {
        _state       = state;
        _bus         = bus;
        _resources   = resources;
        _unitFactory = unitFactory;
        _assets      = assets;
    }

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        if (_state.Phase != MissionPhase.InProgress) return;

        foreach (var triggerProgress in _state.Triggers)
        {
            if (triggerProgress.HasFired && triggerProgress.Definition.OneShot) continue;

            triggerProgress.ElapsedSeconds += deltaTime;

            if (!EvaluateCondition(world, triggerProgress)) continue;

            ExecuteActions(world, triggerProgress);
            triggerProgress.HasFired = true;
            _bus.Publish(new TriggerFiredEvent(_state.Definition.Id, triggerProgress.Definition.Id));
        }
    }

    // ── Condition evaluators ──────────────────────────────────────────────────

    private bool EvaluateCondition(World world, TriggerProgress tp)
    {
        var cond = tp.Definition.Condition;
        if (cond == null) return false;

        return cond.Type switch
        {
            "timer"              => EvalTimer(tp, cond),
            "area_enter"         => EvalAreaEnter(world, cond),
            "kill_count"         => EvalKillCount(tp, cond),
            "resource_threshold" => EvalResourceThreshold(cond),
            _                    => false,
        };
    }

    private static bool EvalTimer(TriggerProgress tp, TriggerConditionDefinition cond) =>
        tp.ElapsedSeconds >= cond.Seconds;

    private static bool EvalAreaEnter(World world, TriggerConditionDefinition cond)
    {
        if (cond.Position == null || cond.Position.Length < 2) return false;

        var center = new Vector3(cond.Position[0], cond.Position[1], 0f);
        float radius = cond.Radius > 0f ? cond.Radius : 5f;

        foreach (var (_, tf) in world.Query<TransformComponent>())
        {
            if (Vector3.Distance(tf.Position, center) <= radius)
                return true;
        }
        return false;
    }

    private static bool EvalKillCount(TriggerProgress tp, TriggerConditionDefinition cond) =>
        tp.Count >= (cond.Count ?? 1);

    private bool EvalResourceThreshold(TriggerConditionDefinition cond)
    {
        if (_resources == null || string.IsNullOrEmpty(cond.ResourceType)) return false;

        if (!Enum.TryParse<ResourceType>(cond.ResourceType, ignoreCase: true, out ResourceType rt))
            return false;

        return _resources.GetDisplay(PlayerId, rt).Current >= cond.Threshold;
    }

    // ── Action executors ──────────────────────────────────────────────────────

    private void ExecuteActions(World world, TriggerProgress tp)
    {
        foreach (var action in tp.Definition.Actions)
        {
            switch (action.Type)
            {
                case "spawn_units":
                    ExecuteSpawnUnits(world, action);
                    break;
                case "dialog":
                    ExecuteDialog(action);
                    break;
                case "camera_pan":
                    ExecuteCameraPan(action);
                    break;
            }
        }
    }

    private void ExecuteSpawnUnits(World world, TriggerActionDefinition action)
    {
        if (_unitFactory == null || action.Units == null) return;

        var pos = Vector3.Zero;
        if (action.Position != null && action.Position.Length >= 2)
            pos = new Vector3(action.Position[0], action.Position[1], 0f);

        foreach (string unitId in action.Units)
        {
            // Try to load the entity definition; skip if not found.
            EntityDefinition? def = null;
            if (_assets != null)
            {
                def = _assets.Load<EntityDefinition>($"Ships/{unitId}")
                   ?? _assets.Load<EntityDefinition>($"Units/{unitId}")
                   ?? _assets.Load<EntityDefinition>($"Bases/{unitId}");
            }

            if (def == null)
            {
                // Fallback: create a minimal default definition.
                def = new EntityDefinition { Id = unitId, Category = "ship" };
            }

            Entity spawned = _unitFactory.Create(world, def);
            var tf = world.GetComponent<TransformComponent>(spawned);
            if (tf != null) tf.Position = pos;
        }
    }

    private void ExecuteDialog(TriggerActionDefinition action)
    {
        string speaker = action.Speaker ?? "Unknown";
        string text    = action.Text    ?? string.Empty;
        _bus.Publish(new DialogEvent(speaker, text));
    }

    private void ExecuteCameraPan(TriggerActionDefinition action)
    {
        if (action.CameraTarget == null || action.CameraTarget.Length < 2) return;

        // Publish a generic event that the camera system can pick up.
        _bus.Publish(new PointerTappedEvent(
            new OpenTK.Mathematics.Vector2(action.CameraTarget[0], action.CameraTarget[1]),
            -1));
    }

    // ── Public counter hook ───────────────────────────────────────────────────

    /// <summary>
    /// Increment the kill counter on all <c>kill_count</c> triggers that are still active.
    /// Call this from combat death handlers or by subscribing to <see cref="UnitDiedEvent"/>.
    /// </summary>
    public void RecordKill()
    {
        foreach (var tp in _state.Triggers)
        {
            if (tp.HasFired && tp.Definition.OneShot) continue;
            if (tp.Definition.Condition?.Type == "kill_count")
                tp.Count++;
        }
    }
}
