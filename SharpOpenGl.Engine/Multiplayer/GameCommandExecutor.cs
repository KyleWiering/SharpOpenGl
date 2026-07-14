using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;

namespace SharpOpenGl.Engine.Multiplayer;

/// <summary>
/// Applies serialisable <see cref="IGameCommand"/> instances to a live <see cref="World"/>.
/// Mirrors the gameplay mutations performed by desktop input handlers.
/// </summary>
public sealed class GameCommandExecutor
{
    /// <summary>Execute a command. Returns <c>false</c> when preconditions are not met (no-op).</summary>
    public bool Execute(GameCommandContext context, IGameCommand command)
    {
        return command switch
        {
            MoveCommand move => ExecuteMove(context, move),
            AttackCommand attack => ExecuteAttack(context, attack),
            BuildCommand build => ExecuteBuild(context, build),
            StopCommand stop => ExecuteStop(context, stop),
            RepairCommand repair => ExecuteRepair(context, repair),
            _ => false,
        };
    }

    private static bool ExecuteMove(GameCommandContext context, MoveCommand command)
    {
        var world = context.World;
        var entities = ResolveEntities(world, command.EntityIds);
        if (entities.Count == 0) return false;

        var target = new Vector3(command.TargetX, 0f, command.TargetZ);
        var movable = new List<Entity>();

        foreach (var entity in entities)
        {
            if (!IsPlayerSelectable(world, entity, context.PlayerId)) continue;
            if (world.GetComponent<MovementComponent>(entity) == null) continue;
            movable.Add(entity);
        }

        if (movable.Count == 0) return false;

        if (command.AttackMove)
            ApplyAttackMoveStance(world, movable);

        if (movable.Count > 1 && context.AssignSquadMove != null)
            context.AssignSquadMove(world, movable, target, false);
        else
        {
            foreach (var entity in movable)
                RouteCommands.AssignDestination(world, entity, target);
        }

        return true;
    }

    private static bool ExecuteAttack(GameCommandContext context, AttackCommand command)
    {
        var world = context.World;
        if (!TryResolveEntity(world, command.TargetEntityId, out Entity target)) return false;

        var attackers = ResolveEntities(world, command.AttackerIds)
            .Where(e => IsPlayerSelectable(world, e, context.PlayerId))
            .Where(e => world.HasComponent<WeaponListComponent>(e))
            .ToList();

        if (attackers.Count == 0) return false;

        foreach (var entity in attackers)
        {
            var ct = world.GetComponent<CombatTargetComponent>(entity);
            if (ct == null)
            {
                ct = new CombatTargetComponent { Faction = context.PlayerId };
                world.AddComponent(entity, ct);
            }

            ct.CurrentTarget = target;
            ct.ManualTarget = true;

            var stance = world.GetComponent<StanceComponent>(entity);
            if (stance == null)
                world.AddComponent(entity, new StanceComponent { CurrentStance = Stance.Aggressive });
            else
                stance.CurrentStance = Stance.Aggressive;

            RouteCommands.ClearRoute(world, entity);

            var movement = world.GetComponent<MovementComponent>(entity);
            if (movement != null)
                movement.Velocity = Vector3.Zero;
        }

        return true;
    }

    private static bool ExecuteBuild(GameCommandContext context, BuildCommand command)
    {
        var world = context.World;
        if (context.Resources == null || context.DefinitionLoader == null) return false;
        if (!TryResolveEntity(world, command.BuilderEntityId, out Entity builderEntity)) return false;

        var building = world.GetComponent<BuildingComponent>(builderEntity);
        if (building == null || building.PlayerId != context.PlayerId) return false;
        if (!building.Producible.Contains(command.ItemId)) return false;

        var def = context.DefinitionLoader(command.ItemId);
        if (def == null) return false;

        int energy = def.Cost?.Energy ?? 0;
        int minerals = def.Cost?.Minerals ?? 0;
        int data = def.Cost?.Data ?? 0;
        int crew = def.Cost?.Crew ?? 0;

        if (context.Supply != null && crew > 0 &&
            !context.Supply.CanAffordSupply(building.PlayerId, crew))
            return false;

        if (!context.Resources.TrySpendCost(building.PlayerId, energy, minerals, data, crew))
            return false;

        if (context.Supply != null && crew > 0)
            context.Supply.ConsumeSupply(building.PlayerId, crew);

        building.BuildQueue.Enqueue(command.ItemId);
        return true;
    }

    private static bool ExecuteRepair(GameCommandContext context, RepairCommand command)
    {
        var world = context.World;
        if (!TryResolveEntity(world, command.TargetEntityId, out Entity target)) return false;

        var health = world.GetComponent<HealthComponent>(target);
        if (health == null || health.IsDead) return false;

        var repairers = ResolveEntities(world, command.RepairerIds)
            .Where(e => IsPlayerSelectable(world, e, context.PlayerId))
            .Where(e => world.HasComponent<ShipRepairComponent>(e))
            .ToList();

        if (repairers.Count == 0) return false;

        foreach (var repairer in repairers)
        {
            var order = world.GetComponent<RepairOrderComponent>(repairer);
            if (order == null)
            {
                order = new RepairOrderComponent();
                world.AddComponent(repairer, order);
            }

            order.Target = target;
        }

        return true;
    }

    private static bool ExecuteStop(GameCommandContext context, StopCommand command)
    {
        var world = context.World;
        var entities = ResolveEntities(world, command.EntityIds);
        if (entities.Count == 0) return false;

        bool any = false;
        foreach (var entity in entities)
        {
            if (!IsPlayerSelectable(world, entity, context.PlayerId)) continue;

            var movement = world.GetComponent<MovementComponent>(entity);
            if (movement != null)
            {
                movement.PathTarget = null;
                movement.Velocity = Vector3.Zero;
            }

            RouteCommands.ClearRoute(world, entity);

            var ct = world.GetComponent<CombatTargetComponent>(entity);
            if (ct != null)
            {
                ct.CurrentTarget = Entity.Null;
                ct.ManualTarget = false;
            }

            any = true;
        }

        return any;
    }

    private static void ApplyAttackMoveStance(World world, IReadOnlyList<Entity> entities)
    {
        foreach (var entity in entities)
        {
            var stance = world.GetComponent<StanceComponent>(entity);
            if (stance != null)
                stance.CurrentStance = Stance.Aggressive;
            else
                world.AddComponent(entity, new StanceComponent { CurrentStance = Stance.Aggressive });

            if (!world.HasComponent<CombatTargetComponent>(entity))
                world.AddComponent(entity, new CombatTargetComponent { Faction = 1 });
        }
    }

    private static List<Entity> ResolveEntities(World world, uint[] ids)
    {
        var result = new List<Entity>(ids.Length);
        foreach (uint id in ids)
        {
            if (TryResolveEntity(world, id, out Entity entity))
                result.Add(entity);
        }

        return result;
    }

    private static bool TryResolveEntity(World world, uint index, out Entity entity) =>
        world.TryGetEntityByIndex(index, out entity);

    private static bool IsPlayerSelectable(World world, Entity entity, int playerId)
    {
        if (!world.IsAlive(entity)) return false;
        if (world.HasComponent<AIControlledComponent>(entity)) return false;
        if (world.HasComponent<ResourceNodeComponent>(entity)) return false;
        if (!world.HasComponent<SelectionComponent>(entity)) return false;

        var building = world.GetComponent<BuildingComponent>(entity);
        if (building != null && building.PlayerId != playerId) return false;

        return true;
    }
}