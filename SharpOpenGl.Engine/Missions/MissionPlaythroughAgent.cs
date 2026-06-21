using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Multiplayer;

namespace SharpOpenGl.Engine.Missions;

/// <summary>
/// Executes a mission <c>demoScript</c> by injecting player commands each tick
/// until primary objectives complete or the script ends.
/// </summary>
public sealed class MissionPlaythroughAgent
{
    private readonly DemoScriptStepDefinition[] _steps;
    private readonly GameCommandExecutor _executor = new();
    private readonly GameCommandContext _context;
    private readonly Action<Vector3, float?>? _cameraPan;

    private int _stepIndex;
    private float _stepTimer;
    private long _tick;

    public MissionPlaythroughAgent(
        MissionDefinition mission,
        GameCommandContext context,
        Action<Vector3, float?>? cameraPan = null)
    {
        _steps = mission.DemoScript ?? [];
        _context = context;
        _cameraPan = cameraPan;
    }

    /// <summary>Current step index (0-based).</summary>
    public int StepIndex => _stepIndex;

    /// <summary>Total scripted steps.</summary>
    public int StepCount => _steps.Length;

    /// <summary>All scripted steps have been consumed.</summary>
    public bool ScriptFinished => _stepIndex >= _steps.Length;

    /// <summary>Every primary objective is complete.</summary>
    public bool MissionObjectivesComplete =>
        _context.MissionState?.AllPrimaryComplete ?? false;

    /// <summary>Script ended or mission victory objectives met.</summary>
    public bool IsFinished => ScriptFinished || MissionObjectivesComplete;

    /// <summary>Advance simulation and issue commands for the active step.</summary>
    public void Tick(float deltaTime)
    {
        if (IsFinished || _steps.Length == 0) return;

        var step = _steps[_stepIndex];
        string type = step.Type.Trim().ToLowerInvariant();

        switch (type)
        {
            case "wait":
                _stepTimer += deltaTime;
                if (_stepTimer >= Math.Max(0f, step.Seconds))
                    Advance();
                break;

            case "wait_objective":
                if (IsObjectiveComplete(step.ObjectiveId))
                    Advance();
                break;

            default:
                ExecuteAction(step, type);
                Advance();
                break;
        }
    }

    private void ExecuteAction(DemoScriptStepDefinition step, string type)
    {
        switch (type)
        {
            case "select_units":
                SelectUnits(step.Filter);
                break;

            case "move_to":
                IssueMove(step, attackMove: false);
                break;

            case "attack_move":
                IssueMove(step, attackMove: true);
                break;

            case "attack_target":
                IssueAttack(step.TargetTag);
                break;

            case "camera_pan":
                PanCamera(step);
                break;

            case "build_unit":
                IssueBuild(step);
                break;

            case "place_building":
                IssuePlaceBuilding(step);
                break;
        }
    }

    private void SelectUnits(string? filter)
    {
        var world = _context.World;
        string key = string.IsNullOrWhiteSpace(filter) ? "all" : filter.Trim();

        foreach (var (entity, sel) in world.Query<SelectionComponent>())
            sel.IsSelected = false;

        foreach (var entity in ResolveSelection(world, key))
        {
            var sel = world.GetComponent<SelectionComponent>(entity);
            if (sel != null)
                sel.IsSelected = true;
        }
    }

    private IEnumerable<Entity> ResolveSelection(World world, string filter)
    {
        if (filter.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var (entity, _) in world.Query<SelectionComponent>())
            {
                if (IsSelectablePlayerUnit(world, entity))
                    yield return entity;
            }

            yield break;
        }

        if (filter.Equals("hero", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var (entity, _) in world.Query<HeroComponent>())
            {
                if (IsSelectablePlayerUnit(world, entity))
                    yield return entity;
            }

            yield break;
        }

        if (_context.MissionState?.EntityTags.TryGetValue(filter, out Entity tagged) == true &&
            world.IsAlive(tagged) && IsSelectablePlayerUnit(world, tagged))
        {
            yield return tagged;
            yield break;
        }

        foreach (var (entity, name) in world.Query<EntityNameComponent>())
        {
            if (!IsSelectablePlayerUnit(world, entity)) continue;
            if (name.DefinitionId.Equals(filter, StringComparison.OrdinalIgnoreCase))
                yield return entity;
        }
    }

    private void IssueMove(DemoScriptStepDefinition step, bool attackMove)
    {
        if (step.Position is not { Length: >= 2 }) return;

        var selected = GetSelectedEntities();
        if (selected.Count == 0) return;

        Vector3 target = MapCoordinates.GridToWorld(step.Position[0], step.Position[1]);
        var cmd = new MoveCommand
        {
            PlayerId = _context.PlayerId,
            Tick = _tick++,
            EntityIds = selected.Select(e => e.Index).ToArray(),
            TargetX = target.X,
            TargetZ = target.Z,
            AttackMove = attackMove,
        };

        _executor.Execute(_context, cmd);
    }

    private void IssueAttack(string? targetTag)
    {
        if (string.IsNullOrWhiteSpace(targetTag)) return;

        var selected = GetSelectedEntities();
        if (selected.Count == 0) return;

        if (!TryResolveAttackTarget(targetTag, out Entity target)) return;

        var cmd = new AttackCommand
        {
            PlayerId = _context.PlayerId,
            Tick = _tick++,
            AttackerIds = selected.Select(e => e.Index).ToArray(),
            TargetEntityId = target.Index,
        };

        _executor.Execute(_context, cmd);
    }

    private bool TryResolveAttackTarget(string targetTag, out Entity target)
    {
        target = Entity.Null;
        var world = _context.World;
        var state = _context.MissionState;

        if (state?.EntityGroups.TryGetValue(targetTag, out HashSet<Entity>? group) == true)
        {
            foreach (var entity in group)
            {
                if (world.IsAlive(entity))
                {
                    target = entity;
                    return true;
                }
            }
        }

        if (state?.EntityTags.TryGetValue(targetTag, out Entity tagged) == true && world.IsAlive(tagged))
        {
            target = tagged;
            return true;
        }

        foreach (var (entity, name) in world.Query<EntityNameComponent>())
        {
            if (!world.IsAlive(entity)) continue;
            if (!world.HasComponent<AIControlledComponent>(entity)) continue;
            if (name.DefinitionId.Equals(targetTag, StringComparison.OrdinalIgnoreCase))
            {
                target = entity;
                return true;
            }
        }

        return false;
    }

    private void IssuePlaceBuilding(DemoScriptStepDefinition step)
    {
        if (string.IsNullOrWhiteSpace(step.BuildingId) || step.Position is not { Length: >= 2 })
            return;

        Vector3 target = MapCoordinates.GridToWorld(step.Position[0], step.Position[1]);
        _context.PlaceBuilding?.Invoke(step.BuildingId, target);
    }

    private void IssueBuild(DemoScriptStepDefinition step)
    {
        if (string.IsNullOrWhiteSpace(step.UnitId)) return;

        var world = _context.World;
        Entity? builder = null;

        if (!string.IsNullOrWhiteSpace(step.BuildingTag))
        {
            if (_context.MissionState?.EntityTags.TryGetValue(step.BuildingTag, out Entity tagged) == true &&
                world.IsAlive(tagged))
                builder = tagged;

            if (builder == null)
            {
                foreach (var (entity, building) in world.Query<BuildingComponent>())
                {
                    if (building.PlayerId != _context.PlayerId) continue;
                    if (building.BuildingType.Equals(step.BuildingTag, StringComparison.OrdinalIgnoreCase))
                    {
                        builder = entity;
                        break;
                    }
                }
            }
        }
        else
        {
            foreach (var (entity, building) in world.Query<BuildingComponent>())
            {
                if (building.PlayerId == _context.PlayerId)
                {
                    builder = entity;
                    break;
                }
            }
        }

        if (builder == null) return;

        var cmd = new BuildCommand
        {
            PlayerId = _context.PlayerId,
            Tick = _tick++,
            BuilderEntityId = builder.Value.Index,
            ItemId = step.UnitId,
        };

        _executor.Execute(_context, cmd);
    }

    private void PanCamera(DemoScriptStepDefinition step)
    {
        if (step.Position is not { Length: >= 2 }) return;

        Vector3 target = MapCoordinates.GridToWorld(step.Position[0], step.Position[1]);
        float? height = step.Height > 0f ? step.Height : null;
        _cameraPan?.Invoke(target, height);
    }

    private List<Entity> GetSelectedEntities()
    {
        var world = _context.World;
        var result = new List<Entity>();

        foreach (var (entity, sel) in world.Query<SelectionComponent>())
        {
            if (sel.IsSelected && IsSelectablePlayerUnit(world, entity))
                result.Add(entity);
        }

        return result;
    }

    private bool IsObjectiveComplete(string? objectiveId)
    {
        if (string.IsNullOrWhiteSpace(objectiveId)) return false;
        var objective = _context.MissionState?.FindObjective(objectiveId);
        return objective?.IsCompleted ?? false;
    }

    private static bool IsSelectablePlayerUnit(World world, Entity entity)
    {
        if (!world.IsAlive(entity)) return false;
        if (world.HasComponent<AIControlledComponent>(entity)) return false;
        if (world.HasComponent<ResourceNodeComponent>(entity)) return false;
        if (!world.HasComponent<SelectionComponent>(entity)) return false;

        var building = world.GetComponent<BuildingComponent>(entity);
        return building == null;
    }

    private void Advance()
    {
        _stepIndex++;
        _stepTimer = 0f;
    }
}