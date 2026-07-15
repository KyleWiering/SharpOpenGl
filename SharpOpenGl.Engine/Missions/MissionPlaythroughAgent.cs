using OpenTK.Mathematics;
using SharpOpenGl.Engine.Build;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
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
    private float _stepElapsed;
    private long _tick;

    /// <summary>Skip stuck placement steps so headless demo recording does not hit the max-duration cap.</summary>
    private const float PlaceBuildingStepTimeoutSeconds = 20f;
    private const float WaitConstructionTimeoutSeconds = 120f;
    private bool _placeBuildingIssued;
    private bool _harvestWired;
    private int[]? _lastMoveGridPosition;

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

            case "harvest":
                if (!_harvestWired)
                {
                    WireHarvestForSelectedMiners(step);
                    _harvestWired = true;
                }

                _stepTimer += deltaTime;
                float harvestSeconds = step.Seconds > 0f
                    ? step.Seconds
                    : MiningVisualSystem.DroneShuttleDuration * 8f;
                if (_stepTimer >= harvestSeconds)
                    Advance();
                break;

            case "wait_objective":
                if (IsObjectiveComplete(step.ObjectiveId))
                    Advance();
                break;

            case "place_building":
                _stepElapsed += deltaTime;
                if (!_placeBuildingIssued)
                {
                    if (IssuePlaceBuilding(step))
                    {
                        _placeBuildingIssued = true;
                        if (!RequiresConstructionWait(step))
                            Advance();
                    }
                    else if (_stepElapsed >= PlaceBuildingStepTimeoutSeconds)
                        Advance();
                }
                else if (IsPlacedStructureComplete(step) || _stepElapsed >= PlaceBuildingStepTimeoutSeconds)
                    Advance();

                break;

            case "wait_for_construction":
                _stepElapsed += deltaTime;
                if (IsPlacedStructureComplete(step) || _stepElapsed >= WaitConstructionTimeoutSeconds)
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
                if (step.Position is { Length: >= 2 })
                    _lastMoveGridPosition = [(int)step.Position[0], (int)step.Position[1]];
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

            case "repair_target":
                IssueRepair(step.TargetTag);
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

    private void IssueRepair(string? targetTag)
    {
        if (string.IsNullOrWhiteSpace(targetTag)) return;

        var selected = GetSelectedEntities()
            .Where(e => _context.World.HasComponent<ShipRepairComponent>(e))
            .ToList();
        if (selected.Count == 0) return;

        if (!TryResolveRepairTarget(targetTag, out Entity target)) return;

        var cmd = new RepairCommand
        {
            PlayerId = _context.PlayerId,
            Tick = _tick++,
            RepairerIds = selected.Select(e => e.Index).ToArray(),
            TargetEntityId = target.Index,
        };

        _executor.Execute(_context, cmd);
    }

    private bool TryResolveRepairTarget(string targetTag, out Entity target)
    {
        target = Entity.Null;
        var world = _context.World;
        var state = _context.MissionState;

        if (state?.EntityTags.TryGetValue(targetTag, out Entity tagged) == true && world.IsAlive(tagged))
        {
            target = tagged;
            return true;
        }

        return false;
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

    private bool IssuePlaceBuilding(DemoScriptStepDefinition step)
    {
        if (string.IsNullOrWhiteSpace(step.BuildingId) || step.Position is not { Length: >= 2 })
            return false;

        if (_context.BuildMapCatalog != null)
        {
            var builtTypes = BuildingFootprint.GetBuiltTypes(_context.World, _context.PlayerId);
            var prerequisites = _context.BuildMapCatalog.GetPrerequisites(step.BuildingId);
            if (!BuildMapCatalog.IsUnlocked(prerequisites, builtTypes))
                return false;
        }

        Vector3 target = MapCoordinates.GridToWorld(step.Position[0], step.Position[1]);
        return _context.PlaceBuilding?.Invoke(step.BuildingId, target) ?? false;
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

    private bool RequiresConstructionWait(DemoScriptStepDefinition step)
    {
        if (string.IsNullOrWhiteSpace(step.BuildingId))
            return false;

        var def = _context.DefinitionLoader?.Invoke(step.BuildingId);
        return def is { BuildTime: > 0f };
    }

    private bool IsPlacedStructureComplete(DemoScriptStepDefinition step)
    {
        if (string.IsNullOrWhiteSpace(step.BuildingId))
            return true;

        var world = _context.World;
        Entity? match = null;

        if (step.Position is { Length: >= 2 })
        {
            Vector3 target = MapCoordinates.GridToWorld(step.Position[0], step.Position[1]);
            foreach (var (entity, transform) in world.Query<TransformComponent>())
            {
                if (!world.HasComponent<BuildingComponent>(entity))
                    continue;

                var building = world.GetComponent<BuildingComponent>(entity);
                if (building == null || building.PlayerId != _context.PlayerId)
                    continue;

                if (!building.BuildingType.Equals(step.BuildingId, StringComparison.OrdinalIgnoreCase))
                    continue;

                float dx = transform.Position.X - target.X;
                float dz = transform.Position.Z - target.Z;
                if (dx * dx + dz * dz > 25f)
                    continue;

                match = entity;
                break;
            }
        }
        else
        {
            foreach (var (entity, building) in world.Query<BuildingComponent>())
            {
                if (building.PlayerId != _context.PlayerId)
                    continue;

                if (!building.BuildingType.Equals(step.BuildingId, StringComparison.OrdinalIgnoreCase))
                    continue;

                match = entity;
                break;
            }
        }

        if (match == null)
            return true;

        var underConstruction = world.GetComponent<UnderConstructionComponent>(match.Value);
        if (underConstruction == null)
            return true;

        return underConstruction.BuildProgress >= underConstruction.TotalBuildTime;
    }

    private void Advance()
    {
        _stepIndex++;
        _stepTimer = 0f;
        _stepElapsed = 0f;
        _placeBuildingIssued = false;
        _harvestWired = false;
    }

    /// <summary>
    /// Assigns selected miners to the resource node nearest the last <c>move_to</c>
    /// grid cell (or optional harvest-step position) and sets deposit target to the
    /// nearest player refinery, matching desktop harvest command behaviour.
    /// </summary>
    private void WireHarvestForSelectedMiners(DemoScriptStepDefinition step)
    {
        var world = _context.World;
        int[]? grid = step.Position is { Length: >= 2 }
            ? [(int)step.Position[0], (int)step.Position[1]]
            : _lastMoveGridPosition;
        if (grid == null) return;

        Entity? node = FindResourceNodeNearGrid(world, grid[0], grid[1]);
        if (!node.HasValue) return;

        Entity? depositTarget = FindDepositTarget(world);
        if (!depositTarget.HasValue) return;

        foreach (var entity in GetSelectedEntities())
        {
            var collector = world.GetComponent<ResourceCollectorComponent>(entity);
            if (collector == null) continue;

            collector.AssignedNode = node.Value;
            collector.State = CollectorState.MovingToNode;
            collector.PlayerId = _context.PlayerId;
            collector.DepositTarget = depositTarget.Value;

            var nodeTransform = world.GetComponent<TransformComponent>(node.Value);
            if (nodeTransform != null)
            {
                var movement = world.GetComponent<MovementComponent>(entity);
                if (movement != null)
                    movement.PathTarget = nodeTransform.Position;
                else
                    RouteCommands.AssignDestination(world, entity, nodeTransform.Position);
            }
        }
    }

    private static Entity? FindResourceNodeNearGrid(World world, int gridX, int gridY)
    {
        Vector3 probe = MapCoordinates.GridToWorld(gridX, gridY);
        const float nodeRadius = 15f;
        Entity? closest = null;
        float closestDist = float.MaxValue;

        foreach (var (entity, node) in world.Query<ResourceNodeComponent>())
        {
            if (node.IsDepleted) continue;
            var transform = world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            float dist = (transform.Position - probe).Length;
            if (dist < nodeRadius && dist < closestDist)
            {
                closestDist = dist;
                closest = entity;
            }
        }

        return closest;
    }

    private Entity? FindDepositTarget(World world)
    {
        Entity? refinery = null;
        Entity? nearestBuilding = null;
        float nearestDist = float.MaxValue;

        foreach (var (entity, building) in world.Query<BuildingComponent>())
        {
            if (building.PlayerId != _context.PlayerId) continue;
            if (!world.IsAlive(entity)) continue;

            if (string.Equals(building.BuildingType, "resource_refinery", StringComparison.OrdinalIgnoreCase))
            {
                refinery = entity;
                break;
            }

            var transform = world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            float dist = transform.Position.LengthSquared;
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearestBuilding = entity;
            }
        }

        return refinery ?? nearestBuilding;
    }
}