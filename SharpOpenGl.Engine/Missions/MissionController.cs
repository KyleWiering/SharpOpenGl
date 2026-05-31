using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Events;

namespace SharpOpenGl.Engine.Missions;

/// <summary>
/// Orchestrates the full mission lifecycle:
/// start, rewards distribution, and replay (restart with same conditions).
/// <para>
/// Typical call sequence:
/// <code>
/// var controller = new MissionController(loader, resources, bus);
/// controller.StartMission("tutorial_01");          // transitions to Briefing
/// controller.BeginGameplay();                       // transitions to InProgress
/// // …game runs, ObjectiveSystem checks victory/defeat…
/// controller.DistributeRewards(playerId);           // call on victory
/// controller.ReplayMission();                       // restart same mission
/// </code>
/// </para>
/// </summary>
public sealed class MissionController
{
    private readonly MissionLoader _loader;
    private readonly ResourceManager? _resources;
    private readonly EventBus _bus;

    /// <summary>The active mission state, or <c>null</c> when no mission is loaded.</summary>
    public MissionState? CurrentMission { get; private set; }

    /// <param name="loader">Used to (re-)load mission definitions.</param>
    /// <param name="resources">Used to apply rewards on victory. May be <c>null</c>.</param>
    /// <param name="bus">Event bus for publishing lifecycle events.</param>
    public MissionController(MissionLoader loader, ResourceManager? resources, EventBus bus)
    {
        _loader    = loader;
        _resources = resources;
        _bus       = bus;
    }

    // ── Mission lifecycle ─────────────────────────────────────────────────────

    /// <summary>
    /// Load a mission definition and transition to <see cref="MissionPhase.Briefing"/>.
    /// Replaces any previously active mission.
    /// Returns the new state, or <c>null</c> if the mission definition was not found.
    /// </summary>
    public MissionState? StartMission(string missionId)
    {
        var definition = _loader.Load(missionId);
        if (definition == null) return null;

        CurrentMission = new MissionState(definition)
        {
            Phase = MissionPhase.Briefing,
        };

        return CurrentMission;
    }

    /// <summary>
    /// Transition from <see cref="MissionPhase.Briefing"/> to
    /// <see cref="MissionPhase.InProgress"/> and publish <see cref="MissionStartedEvent"/>.
    /// No-op if no mission is loaded or if already in progress.
    /// </summary>
    public void BeginGameplay()
    {
        if (CurrentMission == null) return;
        if (CurrentMission.Phase != MissionPhase.Briefing) return;

        CurrentMission.Phase = MissionPhase.InProgress;
        _bus.Publish(new MissionStartedEvent(CurrentMission.Definition.Id));
    }

    /// <summary>
    /// Apply mission rewards to the specified player.
    /// Should be called after the mission transitions to <see cref="MissionPhase.Victory"/>.
    /// </summary>
    /// <param name="playerId">The player ID to receive rewards.</param>
    public void DistributeRewards(int playerId)
    {
        if (CurrentMission == null) return;

        var rewards = CurrentMission.Definition.Rewards;
        if (rewards == null || _resources == null) return;

        var res = rewards.Resources;
        if (res != null)
        {
            _resources.Add(playerId, ResourceType.Energy,   res.Energy);
            _resources.Add(playerId, ResourceType.Minerals, res.Minerals);
            _resources.Add(playerId, ResourceType.Data,     res.Data);
            _resources.Add(playerId, ResourceType.Crew,     res.Crew);
        }

        // XP is awarded directly to any hero entity in the world.
        // Callers that have access to the World may query HeroComponent separately.
    }

    /// <summary>
    /// Restart the current mission from scratch.
    /// Reloads the definition, resets state to <see cref="MissionPhase.Briefing"/>,
    /// and fires <see cref="MissionReplayRequestedEvent"/>.
    /// Returns the fresh state, or <c>null</c> if no mission was active.
    /// </summary>
    public MissionState? ReplayMission()
    {
        if (CurrentMission == null) return null;

        string missionId = CurrentMission.Definition.Id;
        _loader.Invalidate(missionId);
        _bus.Publish(new MissionReplayRequestedEvent(missionId));
        return StartMission(missionId);
    }

    /// <summary>Unload the current mission without firing a replay event.</summary>
    public void Unload()
    {
        CurrentMission = null;
    }
}
