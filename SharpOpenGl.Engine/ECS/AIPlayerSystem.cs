using OpenTK.Mathematics;
using SharpOpenGl.Engine.Economy;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Simple AI system that gives computer-controlled entities patrol, harvest, and attack-move behavior.
/// Entities with <see cref="AIControlledComponent"/> receive periodic move orders.
/// </summary>
public sealed class AIPlayerSystem : GameSystem
{
    private readonly Random _rng = new();
    private float _decisionTimer;
    private const float DecisionInterval = 3f;
    internal const float RetreatHpFraction = 0.25f;
    private readonly float _mapSize;

    public AIPlayerSystem(float mapSize = 1000f)
    {
        _mapSize = mapSize;
    }

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        _decisionTimer -= deltaTime;
        if (_decisionTimer > 0f) return;
        _decisionTimer = DecisionInterval;

        foreach (var (entity, ai) in world.Query<AIControlledComponent>())
        {
            var movement = world.GetComponent<MovementComponent>(entity);
            if (movement == null) continue;

            var transform = world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            var collector = world.GetComponent<ResourceCollectorComponent>(entity);
            if (collector != null)
            {
                collector.PlayerId = ai.PlayerId;
                if (collector.DepositTarget == Entity.Null)
                    collector.DepositTarget = FindDepositTarget(world, ai.PlayerId, transform.Position);

                if (collector.State == CollectorState.Idle)
                {
                    if (TryAssignHarvester(world, entity, collector, transform.Position))
                        continue;
                }
                else
                    continue;
            }

            if (world.GetComponent<WeaponListComponent>(entity) is { Weapons.Count: > 0 } weapons)
            {
                var health = world.GetComponent<HealthComponent>(entity);
                if (health != null && health.MaxHP > 0f && health.HPFraction < RetreatHpFraction)
                {
                    if (TryRetreatWhenWounded(world, entity, ai, movement, transform.Position))
                        continue;
                }

                if (ai.Aggressiveness >= 0.5f)
                {
                    var combatTarget = world.GetComponent<CombatTargetComponent>(entity);
                    if (combatTarget != null)
                    {
                        TryFocusFireWeakestInRange(
                            world, entity, ai, combatTarget, transform.Position, weapons);

                        if (combatTarget.CurrentTarget != Entity.Null)
                        {
                            var targetPos = world.GetComponent<TransformComponent>(combatTarget.CurrentTarget)
                                ?.Position ?? Vector3.Zero;
                            float engageRange = MaxWeaponRange(weapons) * 0.9f;
                            if (HorizontalDistance(transform.Position, targetPos) <= engageRange)
                                continue;
                        }
                    }

                    if (TryAdvanceTowardEnemy(world, entity, ai, movement, transform.Position, weapons))
                        continue;
                }
            }

            if (movement.PathTarget != null)
            {
                float dist = HorizontalDistance(movement.PathTarget.Value, transform.Position);
                if (dist > 5f) continue;
            }

            IssuePatrolOrder(movement);
        }
    }

    /// <summary>Assign an idle AI collector to the nearest non-depleted resource node.</summary>
    internal static bool TryAssignHarvester(
        World world, Entity entity, ResourceCollectorComponent collector, Vector3 position)
    {
        if (collector.State != CollectorState.Idle) return false;

        Entity? node = FindNearestResourceNode(world, position);
        if (!node.HasValue) return false;

        collector.AssignedNode = node.Value;
        collector.State = CollectorState.MovingToNode;
        collector.OrbitAngle = HarvestOrbitHelper.AssignOrbitAngle(entity, node.Value);

        var movement = world.GetComponent<MovementComponent>(entity);
        if (movement != null)
            movement.PathTarget = null;

        return true;
    }

    /// <summary>Move an aggressive AI combatant toward the nearest hostile unit.</summary>
    internal static bool TryAdvanceTowardEnemy(
        World world,
        Entity entity,
        AIControlledComponent ai,
        MovementComponent movement,
        Vector3 position,
        WeaponListComponent weapons)
    {
        var selfCt = world.GetComponent<CombatTargetComponent>(entity);
        int faction = selfCt?.Faction ?? ai.PlayerId;

        Entity? enemy = FindNearestHostile(world, entity, faction, position);
        if (!enemy.HasValue) return false;

        var enemyPos = world.GetComponent<TransformComponent>(enemy.Value)?.Position ?? Vector3.Zero;
        float engageRange = MaxWeaponRange(weapons) * 0.9f;
        float dist = HorizontalDistance(position, enemyPos);

        if (dist <= engageRange) return false;

        movement.PathTarget = enemyPos;
        return true;
    }

    private void IssuePatrolOrder(MovementComponent movement)
    {
        float halfMap = _mapSize * 0.4f;
        float x = (_rng.NextSingle() - 0.5f) * 2f * halfMap;
        float z = (_rng.NextSingle() - 0.5f) * 2f * halfMap;
        movement.PathTarget = new Vector3(x, 0f, z);
    }

    internal static Entity? FindNearestResourceNode(World world, Vector3 position)
    {
        Entity? closest = null;
        float closestDist = float.MaxValue;

        foreach (var (entity, node) in world.Query<ResourceNodeComponent>())
        {
            if (node.IsDepleted) continue;

            var nodePos = world.GetComponent<TransformComponent>(entity)?.Position ?? Vector3.Zero;
            float dist = HorizontalDistance(position, nodePos);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = entity;
            }
        }

        return closest;
    }

    public static Entity FindDepositTarget(World world, int playerId, Vector3 position)
    {
        Entity? commandCenter = null;
        float commandDist = float.MaxValue;
        Entity? anyBase = null;
        float anyDist = float.MaxValue;

        foreach (var (entity, building) in world.Query<BuildingComponent>())
        {
            if (building.PlayerId != playerId) continue;

            var transform = world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            float dist = HorizontalDistance(position, transform.Position);
            if (building.BuildingType.Equals("command_center", StringComparison.OrdinalIgnoreCase)
                && dist < commandDist)
            {
                commandDist = dist;
                commandCenter = entity;
            }

            if (dist < anyDist)
            {
                anyDist = dist;
                anyBase = entity;
            }
        }

        return commandCenter ?? anyBase ?? Entity.Null;
    }

    /// <summary>Retreat toward the faction base when HP drops below <see cref="RetreatHpFraction"/>.</summary>
    internal static bool TryRetreatWhenWounded(
        World world,
        Entity entity,
        AIControlledComponent ai,
        MovementComponent movement,
        Vector3 position)
    {
        var health = world.GetComponent<HealthComponent>(entity);
        if (health == null || health.MaxHP <= 0f || health.HPFraction >= RetreatHpFraction)
            return false;

        var selfCt = world.GetComponent<CombatTargetComponent>(entity);
        int faction = selfCt?.Faction ?? ai.PlayerId;

        Entity? enemy = FindNearestHostile(world, entity, faction, position);
        if (!enemy.HasValue) return false;

        var enemyPos = world.GetComponent<TransformComponent>(enemy.Value)?.Position ?? Vector3.Zero;
        Entity deposit = FindDepositTarget(world, ai.PlayerId, position);

        Vector3 retreatTarget;
        if (deposit != Entity.Null)
        {
            retreatTarget = world.GetComponent<TransformComponent>(deposit)?.Position ?? Vector3.Zero;
        }
        else
        {
            Vector3 away = position - enemyPos;
            if (away.LengthSquared < 0.01f)
                away = new Vector3(1f, 0f, 0f);
            else
                away = Vector3.Normalize(away);
            retreatTarget = position + away * 150f;
        }

        movement.PathTarget = retreatTarget;
        if (selfCt != null)
            selfCt.CurrentTarget = Entity.Null;

        return true;
    }

    /// <summary>Lock combat target to the lowest-HP hostile within weapon range (focus fire).</summary>
    internal static bool TryFocusFireWeakestInRange(
        World world,
        Entity entity,
        AIControlledComponent ai,
        CombatTargetComponent combatTarget,
        Vector3 position,
        WeaponListComponent weapons)
    {
        int faction = combatTarget.Faction != 0 ? combatTarget.Faction : ai.PlayerId;
        float range = MaxWeaponRange(weapons);

        Entity? weakest = FindLowestHpHostileInRange(world, entity, faction, position, range);
        if (!weakest.HasValue) return false;

        combatTarget.TargetingMode = TargetPriority.LowestHP;
        combatTarget.CurrentTarget = weakest.Value;
        return true;
    }

    internal static Entity? FindLowestHpHostileInRange(
        World world, Entity self, int faction, Vector3 position, float range)
    {
        Entity? weakest = null;
        float lowestHp = float.MaxValue;

        foreach (var (candidate, health) in world.Query<HealthComponent>())
        {
            if (candidate == self || health.IsDead) continue;

            var candidateCt = world.GetComponent<CombatTargetComponent>(candidate);
            if (candidateCt != null)
            {
                if (candidateCt.Faction == faction) continue;
            }
            else if (!world.HasComponent<AIControlledComponent>(candidate))
            {
                continue;
            }

            var candidatePos = world.GetComponent<TransformComponent>(candidate)?.Position ?? Vector3.Zero;
            float dist = HorizontalDistance(position, candidatePos);
            if (dist > range) continue;

            if (health.CurrentHP < lowestHp)
            {
                lowestHp = health.CurrentHP;
                weakest = candidate;
            }
        }

        return weakest;
    }

    internal static Entity? FindNearestHostile(
        World world, Entity self, int faction, Vector3 position)
    {
        Entity? closest = null;
        float closestDist = float.MaxValue;

        foreach (var (candidate, health) in world.Query<HealthComponent>())
        {
            if (candidate == self || health.IsDead) continue;

            var candidateCt = world.GetComponent<CombatTargetComponent>(candidate);
            if (candidateCt != null)
            {
                if (candidateCt.Faction == faction) continue;
            }
            else if (!world.HasComponent<AIControlledComponent>(candidate))
            {
                continue;
            }

            var candidatePos = world.GetComponent<TransformComponent>(candidate)?.Position ?? Vector3.Zero;
            float dist = HorizontalDistance(position, candidatePos);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = candidate;
            }
        }

        return closest;
    }

    private static float MaxWeaponRange(WeaponListComponent weapons)
    {
        float max = 0f;
        foreach (var weapon in weapons.Weapons)
            max = MathF.Max(max, weapon.Range);
        return max > 0f ? max : 1f;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = a.X - b.X;
        float dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }
}