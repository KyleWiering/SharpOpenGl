using OpenTK.Mathematics;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Per-frame articulated part angle updates: building combat aim, smooth aim slew toward targets,
/// idle sweep when unaimed, and distance gating via <see cref="ArticulationVisibility"/>.
/// </summary>
public sealed class ArticulationSystem : GameSystem
{
    private readonly Dictionary<uint, int> _idleSweepDirections = new();
    private readonly Dictionary<uint, (float Timer, float TargetYaw)> _alternatingIdle = new();
    private readonly HashSet<uint> _productionIdleCraneParts = new();
    private readonly CombatFogGate _fogGate;

    private static readonly HashSet<ArticulatedPartType> TurretCombatAimTypes =
    [
        ArticulatedPartType.TurretYaw,
        ArticulatedPartType.TurretPitch,
    ];

    private const float AlternatingIdleIntervalSeconds = 3f;
    private const float AlternatingIdleYawDegrees = 30f;
    private const float ShipyardBuildProgressEpsilon = 0.001f;

    private static readonly HashSet<string> ProductionArticulationTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "shipyard",
        "shipyard_small",
        "shipyard_medium",
        "shipyard_large",
        "repair_bay",
    };

    private float _shipyardIdleTime;

    /// <summary>Camera world position used for owner distance gating (set each frame from gameplay camera).</summary>
    public Vector3 CameraPosition { get; set; } = Vector3.Zero;

    /// <summary>Fog gate reused from combat engage rules. When unset, all targets are allowed.</summary>
    public CombatFogGate? FogGate { get; set; }

    /// <summary>
    /// Optional definition loader for normalizing <see cref="BuildingComponent.BuildProgress"/> to 0..1.
    /// When unset, progress values in [0, 1] are treated as fractions (unit tests).
    /// </summary>
    public Func<string, EntityDefinition?>? DefinitionLoader { get; set; }

    public ArticulationSystem(CombatFogGate? fogGate = null)
    {
        _fogGate = fogGate ?? new CombatFogGate();
    }

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        UpdateCombatAim(world, deltaTime);
        UpdateBuildingProductionArticulation(world, deltaTime);

        var toDestroy = new List<Entity>();

        foreach (var (partEntity, part) in world.Query<ArticulatedPartComponent>())
        {
            Entity rootOwner = ArticulationDrawHelper.ResolveRootOwner(world, part);
            if (!world.IsAlive(rootOwner))
            {
                toDestroy.Add(partEntity);
                continue;
            }

            if (!ArticulationVisibility.IsActive(world, rootOwner, CameraPosition))
                continue;

            if (_productionIdleCraneParts.Contains(partEntity.Index))
                continue;

            if (part.HasAimTarget)
            {
                _idleSweepDirections.Remove(partEntity.Index);
                UpdateAimSlew(part, deltaTime);
            }
            else if (part.IdleSweepEnabled)
            {
                _alternatingIdle.Remove(partEntity.Index);
                UpdateIdleSweep(partEntity, part, deltaTime);
            }
        }

        foreach (Entity entity in toDestroy)
        {
            _idleSweepDirections.Remove(entity.Index);
            _alternatingIdle.Remove(entity.Index);
            _productionIdleCraneParts.Remove(entity.Index);
            world.DestroyEntity(entity);
        }
    }

    private void UpdateBuildingProductionArticulation(World world, float deltaTime)
    {
        _productionIdleCraneParts.Clear();
        _shipyardIdleTime += deltaTime;

        foreach (var (partEntity, part) in world.Query<ArticulatedPartComponent>())
        {
            Entity rootOwner = ArticulationDrawHelper.ResolveRootOwner(world, part);
            if (!world.IsAlive(rootOwner))
                continue;

            BuildingComponent? building = world.GetComponent<BuildingComponent>(rootOwner);
            if (building == null || !ProductionArticulationTypes.Contains(building.BuildingType))
                continue;

            bool isProducing = building.BuildQueue.Count > 0
                || building.BuildProgress > ShipyardBuildProgressEpsilon;
            float buildFraction = ResolveBuildFraction(building);

            if (part.PartType == ArticulatedPartType.Crane)
            {
                if (isProducing)
                {
                    part.HasAimTarget = true;
                    part.TargetYaw = Math.Clamp(buildFraction * 90f, part.YawMin, part.YawMax);
                    part.TargetPitch = 0f;
                }
                else
                {
                    part.HasAimTarget = false;
                    part.TargetYaw = 0f;
                    part.TargetPitch = 0f;
                    part.CurrentYaw = MathF.Sin(_shipyardIdleTime) * 45f;
                    (part.CurrentYaw, part.CurrentPitch) = ArticulationMath.ClampAngles(
                        part.CurrentYaw,
                        part.CurrentPitch,
                        part.YawMin,
                        part.YawMax,
                        part.PitchMin,
                        part.PitchMax);
                    _productionIdleCraneParts.Add(partEntity.Index);
                }
            }
            else if (part.PartType == ArticulatedPartType.BayDoor)
            {
                if (isProducing)
                {
                    part.HasAimTarget = true;
                    part.TargetPitch = Math.Clamp(buildFraction * 75f, part.PitchMin, part.PitchMax);
                    part.TargetYaw = 0f;
                }
                else
                {
                    part.HasAimTarget = false;
                    part.TargetPitch = 0f;
                    part.TargetYaw = 0f;
                }
            }
        }
    }

    private float ResolveBuildFraction(BuildingComponent building)
    {
        if (building.BuildQueue.Count == 0 && building.BuildProgress <= ShipyardBuildProgressEpsilon)
            return 0f;

        if (building.BuildQueue.Count > 0 && DefinitionLoader != null)
        {
            EntityDefinition? def = DefinitionLoader(building.BuildQueue.Peek());
            if (def is { BuildTime: > 0f })
                return Math.Clamp(building.BuildProgress / def.BuildTime, 0f, 1f);
        }

        return Math.Clamp(building.BuildProgress, 0f, 1f);
    }

    private void UpdateCombatAim(World world, float deltaTime)
    {
        CombatFogGate fogGate = FogGate ?? _fogGate;

        foreach (var (partEntity, part) in world.Query<ArticulatedPartComponent>())
        {
            if (part.PartType != ArticulatedPartType.TurretYaw
                && part.PartType != ArticulatedPartType.TurretPitch)
                continue;

            Entity rootOwner = ArticulationDrawHelper.ResolveRootOwner(world, part);
            if (!world.IsAlive(rootOwner))
                continue;

            CombatTargetComponent? combatTarget = world.GetComponent<CombatTargetComponent>(rootOwner);
            WeaponListComponent? weaponList = world.GetComponent<WeaponListComponent>(rootOwner);
            if (combatTarget == null || weaponList == null || weaponList.Weapons.Count == 0)
                continue;

            BuildingComponent? building = world.GetComponent<BuildingComponent>(rootOwner);

            if (SpecialHullArticulationAimSystem.TryResolveCombatAim(
                    world,
                    fogGate,
                    rootOwner,
                    part,
                    TurretCombatAimTypes,
                    out float targetYaw,
                    out float targetPitch))
            {
                _alternatingIdle.Remove(partEntity.Index);
                ApplyAimTarget(part, targetYaw, targetPitch);
                continue;
            }

            part.HasAimTarget = false;
            part.TargetYaw = 0f;
            part.TargetPitch = 0f;

            if (building != null && IsMissileBatteryAlternatingIdle(building, part))
                UpdateAlternatingIdle(partEntity, part, deltaTime);
            else
                _alternatingIdle.Remove(partEntity.Index);
        }
    }

    private static void ApplyAimTarget(ArticulatedPartComponent part, float targetYaw, float targetPitch)
    {
        part.HasAimTarget = true;

        switch (part.PartType)
        {
            case ArticulatedPartType.TurretYaw:
                part.TargetYaw = targetYaw;
                break;
            case ArticulatedPartType.TurretPitch:
                part.TargetPitch = targetPitch;
                break;
            default:
                part.TargetYaw = targetYaw;
                part.TargetPitch = targetPitch;
                break;
        }
    }

    private static bool IsMissileBatteryAlternatingIdle(BuildingComponent building, ArticulatedPartComponent part) =>
        building.BuildingType.Equals("missile_battery", StringComparison.OrdinalIgnoreCase)
        && part.PartType == ArticulatedPartType.TurretYaw
        && !part.IdleSweepEnabled;

    private void UpdateAlternatingIdle(Entity partEntity, ArticulatedPartComponent part, float deltaTime)
    {
        if (!_alternatingIdle.TryGetValue(partEntity.Index, out var state))
        {
            state = (AlternatingIdleIntervalSeconds, AlternatingIdleYawDegrees);
            _alternatingIdle[partEntity.Index] = state;
        }

        state.Timer -= deltaTime;
        if (state.Timer <= 0f)
        {
            state.TargetYaw = Math.Abs(state.TargetYaw - AlternatingIdleYawDegrees) < 1f
                ? -AlternatingIdleYawDegrees
                : AlternatingIdleYawDegrees;
            state.Timer = AlternatingIdleIntervalSeconds;
        }

        _alternatingIdle[partEntity.Index] = state;
        part.HasAimTarget = true;
        part.TargetYaw = state.TargetYaw;
        part.TargetPitch = 0f;
    }

    private static void UpdateAimSlew(ArticulatedPartComponent part, float deltaTime)
    {
        float maxStep = part.SlewRateDegreesPerSecond * deltaTime;
        float yaw = MoveToward(part.CurrentYaw, part.TargetYaw, maxStep);
        float pitch = MoveToward(part.CurrentPitch, part.TargetPitch, maxStep);
        (part.CurrentYaw, part.CurrentPitch) = ArticulationMath.ClampAngles(
            yaw,
            pitch,
            part.YawMin,
            part.YawMax,
            part.PitchMin,
            part.PitchMax);
    }

    private void UpdateIdleSweep(Entity partEntity, ArticulatedPartComponent part, float deltaTime)
    {
        bool sweepPitch = part.PartType == ArticulatedPartType.TurretPitch;
        float min = sweepPitch ? part.PitchMin : part.YawMin;
        float max = sweepPitch ? part.PitchMax : part.YawMax;
        float current = sweepPitch ? part.CurrentPitch : part.CurrentYaw;

        if (!_idleSweepDirections.TryGetValue(partEntity.Index, out int direction))
        {
            direction = current <= (min + max) * 0.5f ? 1 : -1;
            _idleSweepDirections[partEntity.Index] = direction;
        }

        float step = part.IdleSweepSpeed * deltaTime * direction;
        float next = current + step;

        if (next >= max)
        {
            next = max;
            _idleSweepDirections[partEntity.Index] = -1;
        }
        else if (next <= min)
        {
            next = min;
            _idleSweepDirections[partEntity.Index] = 1;
        }

        if (sweepPitch)
            part.CurrentPitch = next;
        else
            part.CurrentYaw = next;

        (part.CurrentYaw, part.CurrentPitch) = ArticulationMath.ClampAngles(
            part.CurrentYaw,
            part.CurrentPitch,
            part.YawMin,
            part.YawMax,
            part.PitchMin,
            part.PitchMax);
    }

    private static float MoveToward(float current, float target, float maxDelta)
    {
        float delta = target - current;
        if (MathF.Abs(delta) <= maxDelta)
            return target;

        return current + MathF.Sign(delta) * maxDelta;
    }
}