using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Manages squad membership, formation layouts, and leader-follower move routing.
/// </summary>
public sealed class SquadSystem : GameSystem
{
    private const float TransitRefreshDistance = 6f;

    private readonly Dictionary<int, SquadState> _squads = new();
    private int _nextSquadId = 1;

    private sealed class SquadState
    {
        public Entity Leader { get; set; }
        public FormationType Formation { get; set; }
        public float TransitFacingYaw { get; set; }
        public Vector3 LastLeaderPosition { get; set; }
    }

    /// <summary>Form a squad from the given members. Returns squad id, or -1 for single-unit selections.</summary>
    public int FormSquad(World world, IReadOnlyList<Entity> members, FormationType formation = FormationType.Line)
    {
        if (members.Count <= 1)
            return -1;

        foreach (var entity in members)
            RemoveFromSquad(world, entity);

        int squadId = _nextSquadId++;
        Entity leader = members[0];
        ApplyFormation(world, squadId, leader, members, formation);
        return squadId;
    }

    /// <summary>Disband a squad and clear membership on all of its units.</summary>
    public void DisbandSquad(World world, int squadId)
    {
        if (!_squads.TryGetValue(squadId, out var state))
            return;

        foreach (var (entity, member) in world.Query<SquadMemberComponent>())
        {
            if (member.SquadId != squadId)
                continue;

            member.SquadId = -1;
            member.FormationSlot = -1;
            member.FormationOffset = Vector3.Zero;
        }

        _squads.Remove(squadId);
        _ = state;
    }

    /// <summary>Remove a single entity from its squad, disbanding if it was the last member.</summary>
    public void RemoveFromSquad(World world, Entity entity)
    {
        var member = world.GetComponent<SquadMemberComponent>(entity);
        if (member == null || member.SquadId < 0)
            return;

        int squadId = member.SquadId;
        member.SquadId = -1;
        member.FormationSlot = -1;
        member.FormationOffset = Vector3.Zero;

        if (!_squads.TryGetValue(squadId, out var state))
            return;

        var remaining = GetSquadMembers(world, squadId);
        if (remaining.Count == 0)
        {
            _squads.Remove(squadId);
            return;
        }

        if (remaining.Count == 1)
        {
            DisbandSquad(world, squadId);
            return;
        }

        if (entity == state.Leader)
            state.Leader = remaining[0];

        ApplyFormation(world, squadId, state.Leader, remaining, state.Formation);
    }

    /// <summary>Cycle formation type for the squad shared by the selected members.</summary>
    public FormationType? CycleFormation(World world, IReadOnlyList<Entity> members)
    {
        if (members.Count <= 1)
            return null;

        int squadId = EnsureSquad(world, members);
        if (squadId < 0 || !_squads.TryGetValue(squadId, out var state))
            return null;

        state.Formation = FormationLayout.NextFormation(state.Formation);
        ApplyFormation(world, squadId, state.Leader, members, state.Formation);
        return state.Formation;
    }

    /// <summary>Get the formation type for a shared squad among selected members.</summary>
    public FormationType? GetFormationForSelection(IReadOnlyList<Entity> members, World world)
    {
        int? squadId = null;
        foreach (var entity in members)
        {
            var member = world.GetComponent<SquadMemberComponent>(entity);
            if (member == null || member.SquadId < 0)
                return null;

            squadId ??= member.SquadId;
            if (member.SquadId != squadId)
                return null;
        }

        if (!squadId.HasValue || !_squads.TryGetValue(squadId.Value, out var state))
            return null;

        return state.Formation;
    }

    /// <summary>
    /// Assign move routes: leader paths to <paramref name="destination"/>;
    /// followers path to formation slots relative to the leader's facing.
    /// </summary>
    public void AssignMoveRoutes(
        World world,
        IReadOnlyList<Entity> members,
        Vector3 destination,
        bool append = false)
    {
        if (members.Count == 0)
            return;

        if (members.Count == 1)
        {
            RouteCommands.AssignDestination(world, members[0], destination, append);
            return;
        }

        int squadId = EnsureSquad(world, members);
        if (squadId < 0 || !_squads.TryGetValue(squadId, out var state))
            return;

        var leaderTransform = world.GetComponent<TransformComponent>(state.Leader);
        if (leaderTransform == null)
            return;

        float facing = FormationLayout.FacingYaw(leaderTransform.Position, destination);
        state.TransitFacingYaw = facing;
        state.LastLeaderPosition = leaderTransform.Position;

        RouteCommands.AssignDestination(world, state.Leader, destination, append);

        foreach (var entity in members)
        {
            if (entity == state.Leader)
                continue;

            var member = world.GetComponent<SquadMemberComponent>(entity);
            if (member == null)
                continue;

            Vector3 slot = destination + FormationLayout.RotateOffset(member.FormationOffset, facing);
            RouteCommands.AssignDestination(world, entity, slot, append);
        }
    }

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        foreach (var (squadId, state) in _squads)
        {
            if (!world.IsAlive(state.Leader))
            {
                DisbandSquad(world, squadId);
                continue;
            }

            if (!IsLeaderInTransit(world, state.Leader))
                continue;

            var leaderTransform = world.GetComponent<TransformComponent>(state.Leader);
            if (leaderTransform == null)
                continue;

            float moved = (leaderTransform.Position - state.LastLeaderPosition).Length;
            if (moved < TransitRefreshDistance)
                continue;

            state.LastLeaderPosition = leaderTransform.Position;
            float facing = ResolveTransitFacing(world, state);

            foreach (var (entity, member) in world.Query<SquadMemberComponent>())
            {
                if (member.SquadId != squadId || member.FormationSlot <= 0)
                    continue;

                if (!IsEntityInTransit(world, entity))
                    continue;

                Vector3 slot = leaderTransform.Position
                    + FormationLayout.RotateOffset(member.FormationOffset, facing);
                RefreshTransitDestination(world, entity, slot);
            }
        }
    }

    private int EnsureSquad(World world, IReadOnlyList<Entity> members, FormationType? formation = null)
    {
        if (members.Count <= 1)
            return -1;

        int? sharedSquadId = null;
        foreach (var entity in members)
        {
            var member = GetOrAddSquadMember(world, entity);
            if (member.SquadId < 0)
            {
                sharedSquadId = null;
                break;
            }

            sharedSquadId ??= member.SquadId;
            if (member.SquadId != sharedSquadId)
            {
                sharedSquadId = null;
                break;
            }
        }

        if (sharedSquadId.HasValue
            && _squads.TryGetValue(sharedSquadId.Value, out var existing)
            && MembersMatchSquad(world, sharedSquadId.Value, members))
        {
            if (formation.HasValue && existing.Formation != formation.Value)
            {
                existing.Formation = formation.Value;
                ApplyFormation(world, sharedSquadId.Value, existing.Leader, members, formation.Value);
            }

            return sharedSquadId.Value;
        }

        FormationType resolved = formation
            ?? (sharedSquadId.HasValue && _squads.TryGetValue(sharedSquadId.Value, out var prior)
                ? prior.Formation
                : FormationType.Line);

        return FormSquad(world, members, resolved);
    }

    private void ApplyFormation(
        World world,
        int squadId,
        Entity leader,
        IReadOnlyList<Entity> members,
        FormationType formation)
    {
        var offsets = FormationLayout.ComputeOffsets(formation, members.Count);

        for (int i = 0; i < members.Count; i++)
        {
            var member = GetOrAddSquadMember(world, members[i]);
            member.SquadId = squadId;
            member.FormationSlot = i;
            member.FormationOffset = offsets[i];
        }

        _squads[squadId] = new SquadState
        {
            Leader = leader,
            Formation = formation,
            LastLeaderPosition = world.GetComponent<TransformComponent>(leader)?.Position ?? Vector3.Zero,
        };
    }

    private static SquadMemberComponent GetOrAddSquadMember(World world, Entity entity)
    {
        var member = world.GetComponent<SquadMemberComponent>(entity);
        if (member != null)
            return member;

        member = new SquadMemberComponent();
        world.AddComponent(entity, member);
        return member;
    }

    private static List<Entity> GetSquadMembers(World world, int squadId)
    {
        var members = new List<Entity>();
        foreach (var (entity, member) in world.Query<SquadMemberComponent>())
        {
            if (member.SquadId == squadId)
                members.Add(entity);
        }

        members.Sort((a, b) =>
        {
            int slotA = world.GetComponent<SquadMemberComponent>(a)!.FormationSlot;
            int slotB = world.GetComponent<SquadMemberComponent>(b)!.FormationSlot;
            return slotA.CompareTo(slotB);
        });

        return members;
    }

    private static bool MembersMatchSquad(World world, int squadId, IReadOnlyList<Entity> members)
    {
        var existing = new HashSet<Entity>();
        foreach (var (entity, member) in world.Query<SquadMemberComponent>())
        {
            if (member.SquadId == squadId)
                existing.Add(entity);
        }

        if (existing.Count != members.Count)
            return false;

        foreach (var entity in members)
        {
            if (!existing.Contains(entity))
                return false;
        }

        return true;
    }

    private static bool IsLeaderInTransit(World world, Entity leader)
    {
        return world.HasComponent<DestinationComponent>(leader)
            || world.HasComponent<WaypointQueueComponent>(leader);
    }

    private static bool IsEntityInTransit(World world, Entity entity)
    {
        return world.HasComponent<DestinationComponent>(entity)
            || world.HasComponent<WaypointQueueComponent>(entity);
    }

    private static float ResolveTransitFacing(World world, SquadState state)
    {
        var destination = world.GetComponent<DestinationComponent>(state.Leader);
        var leaderTransform = world.GetComponent<TransformComponent>(state.Leader);
        if (destination != null && leaderTransform != null)
            return FormationLayout.FacingYaw(leaderTransform.Position, destination.Target);

        var queue = world.GetComponent<WaypointQueueComponent>(state.Leader);
        if (queue != null && queue.Waypoints.Count > 0 && leaderTransform != null)
        {
            int index = Math.Clamp(queue.CurrentIndex, 0, queue.Waypoints.Count - 1);
            return FormationLayout.FacingYaw(leaderTransform.Position, queue.Waypoints[index]);
        }

        if (leaderTransform != null)
            return leaderTransform.EulerAngles.Y;

        return state.TransitFacingYaw;
    }

    private static void RefreshTransitDestination(World world, Entity entity, Vector3 slot)
    {
        var destination = world.GetComponent<DestinationComponent>(entity);
        if (destination != null)
        {
            destination.Target = slot;
            destination.GridX = (int)slot.X;
            destination.GridY = (int)slot.Z;
            world.RemoveComponent<PathComponent>(entity);
            return;
        }

        var queue = world.GetComponent<WaypointQueueComponent>(entity);
        if (queue == null || queue.Waypoints.Count == 0)
            return;

        int index = Math.Clamp(queue.CurrentIndex, 0, queue.Waypoints.Count - 1);
        queue.Waypoints[index] = slot;
        world.RemoveComponent<PathComponent>(entity);
        world.RemoveComponent<DestinationComponent>(entity);
    }
}