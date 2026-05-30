using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Events;

namespace SharpOpenGl.Engine.Missions;

/// <summary>
/// Distributes the rewards defined in a <see cref="MissionRewardDefinition"/>
/// to the specified player via the <see cref="ResourceManager"/>.
/// </summary>
public sealed class MissionRewards
{
    private readonly ResourceManager _resources;
    private readonly EventBus?       _bus;

    public MissionRewards(ResourceManager resources, EventBus? bus = null)
    {
        _resources = resources;
        _bus       = bus;
    }

    // ── Apply ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Apply all rewards from a completed mission to the given player.
    /// Publishes a <see cref="MissionCompletedEvent"/> after distributing resources.
    /// </summary>
    /// <param name="playerId">The player who completed the mission.</param>
    /// <param name="mission">The completed mission state.</param>
    /// <returns>
    /// The <see cref="MissionRewardDefinition"/> that was applied,
    /// for further processing (e.g. displaying unlocks in the UI).
    /// </returns>
    public MissionRewardDefinition Apply(int playerId, MissionState mission)
    {
        MissionRewardDefinition rewards = mission.Definition.Rewards;

        // Distribute resources
        _resources.Add(playerId, ResourceType.Energy,   rewards.Resources.Energy);
        _resources.Add(playerId, ResourceType.Minerals, rewards.Resources.Minerals);
        _resources.Add(playerId, ResourceType.Data,     rewards.Resources.Data);
        _resources.Add(playerId, ResourceType.Crew,     rewards.Resources.Crew);

        return rewards;
    }
}
