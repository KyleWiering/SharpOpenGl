using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Drives non-turret special-hull articulation aim targets (bay doors, launcher pod, sensor dish, wing flaps).
/// Runs before <see cref="ArticulationSystem"/> so slew smoothing applies the same frame.
/// </summary>
public sealed class SpecialHullArticulationAimSystem : GameSystem
{
    private const float CarrierDeckIdlePeriodSeconds = 12f;
    private const float CarrierDeckLaunchPitchDegrees = 30f;
    private const float BomberBayOpenPitchDegrees = 90f;
    private const float ThrustVelocityThresholdSq = 0.25f;
    private const float WingFlapMaxDeflectionDegrees = 15f;

    private float _carrierDeckIdleTime;

    /// <summary>Fog gate reused from combat engage rules. When unset, all targets are allowed.</summary>
    public CombatFogGate? FogGate { get; set; }

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        _carrierDeckIdleTime += deltaTime;
        CombatFogGate fogGate = FogGate ?? new CombatFogGate();
        var toDestroy = new List<Entity>();

        foreach (var (partEntity, part) in world.Query<ArticulatedPartComponent>())
        {
            if (!IsSpecialHullDriverType(part.PartType))
                continue;

            Entity rootOwner = ArticulationDrawHelper.ResolveRootOwner(world, part);
            if (!world.IsAlive(rootOwner))
            {
                toDestroy.Add(partEntity);
                continue;
            }

            if (world.HasComponent<BuildingComponent>(rootOwner))
                continue;

            string hullKey = ResolveOwnerHullKey(world, rootOwner);

            switch (part.PartType)
            {
                case ArticulatedPartType.BayDoor when hullKey == "bomber_heavy":
                    UpdateBomberBayDoor(world, rootOwner, part);
                    break;
                case ArticulatedPartType.BayDoor when hullKey == "carrier_command":
                    UpdateCarrierDeck(world, fogGate, rootOwner, part);
                    break;
                case ArticulatedPartType.LauncherPod:
                    UpdateLauncherPod(world, fogGate, rootOwner, part);
                    break;
                case ArticulatedPartType.SensorDish when hullKey == "scout_light":
                    UpdateScoutSensor(world, fogGate, rootOwner, part);
                    break;
                case ArticulatedPartType.WingFlap:
                case ArticulatedPartType.EngineGimbal when hullKey is "fighter_basic" or "interceptor_mk2":
                    UpdateWingFlap(world, rootOwner, part);
                    break;
            }
        }

        foreach (Entity entity in toDestroy)
            world.DestroyEntity(entity);
    }

    private static void UpdateBomberBayDoor(World world, Entity owner, ArticulatedPartComponent part)
    {
        bool shouldOpen = IsBomberAttackRun(world, owner);
        part.HasAimTarget = true;
        part.TargetYaw = 0f;
        part.TargetPitch = shouldOpen ? BomberBayOpenPitchDegrees : 0f;
    }

    private static bool IsBomberAttackRun(World world, Entity owner)
    {
        StanceComponent? stance = world.GetComponent<StanceComponent>(owner);
        if (stance?.CurrentStance == Stance.Aggressive)
            return true;

        WeaponListComponent? weaponList = world.GetComponent<WeaponListComponent>(owner);
        return weaponList != null
            && weaponList.Weapons.Any(w => w.Cooldown > 0f);
    }

    private void UpdateCarrierDeck(
        World world,
        CombatFogGate fogGate,
        Entity owner,
        ArticulatedPartComponent part)
    {
        if (TryResolveCarrierCombatTarget(world, fogGate, owner, out _))
        {
            part.HasAimTarget = true;
            part.TargetYaw = 0f;
            part.TargetPitch = CarrierDeckLaunchPitchDegrees;
            return;
        }

        float phase = _carrierDeckIdleTime * (MathF.Tau / CarrierDeckIdlePeriodSeconds);
        float idlePitch = 30f * (1f + MathF.Sin(phase));
        part.HasAimTarget = true;
        part.TargetYaw = 0f;
        part.TargetPitch = Math.Clamp(idlePitch, part.PitchMin, part.PitchMax);
    }

    private static bool TryResolveCarrierCombatTarget(
        World world,
        CombatFogGate fogGate,
        Entity owner,
        out Entity target)
    {
        target = Entity.Null;
        CombatTargetComponent? combatTarget = world.GetComponent<CombatTargetComponent>(owner);
        WeaponListComponent? weaponList = world.GetComponent<WeaponListComponent>(owner);
        if (combatTarget == null || weaponList == null || weaponList.Weapons.Count == 0)
            return false;

        target = combatTarget.CurrentTarget;
        if (!world.IsAlive(target))
            return false;

        HealthComponent? targetHealth = world.GetComponent<HealthComponent>(target);
        if (targetHealth == null || targetHealth.IsDead)
            return false;

        if (!fogGate.CanEngage(world, owner, target))
            return false;

        TransformComponent? ownerTransform = world.GetComponent<TransformComponent>(owner);
        TransformComponent? targetTransform = world.GetComponent<TransformComponent>(target);
        if (ownerTransform == null || targetTransform == null)
            return false;

        float maxRange = weaponList.Weapons.Max(w => w.Range);
        float dist = (targetTransform.Position - ownerTransform.Position).Length;
        return dist <= maxRange;
    }

    private static void UpdateLauncherPod(
        World world,
        CombatFogGate fogGate,
        Entity owner,
        ArticulatedPartComponent part)
    {
        if (TryResolveCombatAim(
                world,
                fogGate,
                owner,
                part,
                allowedTypes: [ArticulatedPartType.LauncherPod],
                out float targetYaw,
                out _))
        {
            part.HasAimTarget = true;
            part.TargetYaw = targetYaw;
            part.TargetPitch = 0f;
            return;
        }

        part.HasAimTarget = false;
        part.TargetYaw = 0f;
        part.TargetPitch = 0f;
    }

    private static void UpdateScoutSensor(
        World world,
        CombatFogGate fogGate,
        Entity owner,
        ArticulatedPartComponent part)
    {
        if (TryResolveCombatAim(
                world,
                fogGate,
                owner,
                part,
                allowedTypes: [ArticulatedPartType.SensorDish],
                out float targetYaw,
                out float targetPitch))
        {
            part.HasAimTarget = true;
            part.TargetYaw = targetYaw;
            part.TargetPitch = targetPitch;
            return;
        }

        part.HasAimTarget = false;
        part.TargetYaw = 0f;
        part.TargetPitch = 0f;
    }

    private static void UpdateWingFlap(World world, Entity owner, ArticulatedPartComponent part)
    {
        MovementComponent? movement = world.GetComponent<MovementComponent>(owner);
        float velocitySq = movement?.Velocity.LengthSquared ?? 0f;

        if (velocitySq > ThrustVelocityThresholdSq)
        {
            float thrustHint = MathF.Min(1f, MathF.Sqrt(velocitySq) / 4f);
            float deflection = WingFlapMaxDeflectionDegrees * thrustHint;
            bool leftFlap = part.LocalPivotOffset.X < 0f;
            part.HasAimTarget = true;
            part.TargetYaw = 0f;
            part.TargetPitch = Math.Clamp(leftFlap ? deflection : -deflection, part.PitchMin, part.PitchMax);
            return;
        }

        part.HasAimTarget = false;
        part.TargetYaw = 0f;
        part.TargetPitch = 0f;
    }

    /// <summary>
    /// Shared combat aim resolution for special-hull parts (parameterized allowed part types).
    /// </summary>
    internal static bool TryResolveCombatAim(
        World world,
        CombatFogGate fogGate,
        Entity owner,
        ArticulatedPartComponent part,
        HashSet<ArticulatedPartType> allowedTypes,
        out float targetYaw,
        out float targetPitch)
    {
        targetYaw = 0f;
        targetPitch = 0f;

        if (!allowedTypes.Contains(part.PartType))
            return false;

        CombatTargetComponent? combatTarget = world.GetComponent<CombatTargetComponent>(owner);
        WeaponListComponent? weaponList = world.GetComponent<WeaponListComponent>(owner);
        if (combatTarget == null || weaponList == null || weaponList.Weapons.Count == 0)
            return false;

        Entity target = combatTarget.CurrentTarget;
        if (!world.IsAlive(target))
            return false;

        HealthComponent? targetHealth = world.GetComponent<HealthComponent>(target);
        if (targetHealth == null || targetHealth.IsDead)
            return false;

        if (!fogGate.CanEngage(world, owner, target))
            return false;

        TransformComponent? ownerTransform = world.GetComponent<TransformComponent>(owner);
        TransformComponent? targetTransform = world.GetComponent<TransformComponent>(target);
        if (ownerTransform == null || targetTransform == null)
            return false;

        float maxRange = weaponList.Weapons.Max(w => w.Range);
        float dist = (targetTransform.Position - ownerTransform.Position).Length;
        if (dist > maxRange)
            return false;

        Matrix4 parentModel = ArticulationDrawHelper.ResolveOwnerModelMatrix(world, part.Owner);
        Vector3 ownerScale = ResolveOwnerScale(world, part.Owner);
        Vector3 pivotWorld = ArticulationMath.ComputePivotWorld(parentModel, part.LocalPivotOffset, ownerScale);
        Vector3 ownerForward = ComputeOwnerForward(ownerTransform);

        (targetYaw, targetPitch) = ArticulationMath.ComputeAimAngles(
            pivotWorld,
            targetTransform.Position,
            ownerForward,
            part.YawMin,
            part.YawMax,
            part.PitchMin,
            part.PitchMax);

        return true;
    }

    private static Vector3 ResolveOwnerScale(World world, Entity owner)
    {
        if (!world.IsAlive(owner))
            return Vector3.One;

        ArticulatedPartComponent? ownerPart = world.GetComponent<ArticulatedPartComponent>(owner);
        if (ownerPart != null)
            return ResolveOwnerScale(world, ownerPart.Owner);

        TransformComponent? transform = world.GetComponent<TransformComponent>(owner);
        return transform?.Scale ?? Vector3.One;
    }

    private static Vector3 ComputeOwnerForward(TransformComponent transform)
    {
        float yawRad = MathHelper.DegreesToRadians(transform.EulerAngles.Y);
        return Vector3.Normalize(new Vector3(MathF.Sin(yawRad), 0f, -MathF.Cos(yawRad)));
    }

    private static string ResolveOwnerHullKey(World world, Entity owner)
    {
        var name = world.GetComponent<EntityNameComponent>(owner);
        if (name != null && !string.IsNullOrWhiteSpace(name.DefinitionId))
            return name.DefinitionId.ToLowerInvariant();

        return string.Empty;
    }

    private static bool IsSpecialHullDriverType(ArticulatedPartType type) =>
        type is ArticulatedPartType.BayDoor
            or ArticulatedPartType.SensorDish
            or ArticulatedPartType.LauncherPod
            or ArticulatedPartType.WingFlap
            or ArticulatedPartType.EngineGimbal;
}