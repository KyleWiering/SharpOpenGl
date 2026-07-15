using SharpOpenGl.Engine.Grid;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Basic AI production — queues miners, fighters, and heavier hulls at idle AI shipyards when build slots are free.
/// </summary>
public sealed class AIBuildQueueSystem : GameSystem
{
    private static readonly string[] ProductionLadder =
    [
        "miner_basic",
        "miner_eva",
        "miner_tractor",
        "fighter_basic",
        "scout_light",
        "corvette_fast",
        "frigate_strike",
        "destroyer_assault",
        "gunship_heavy",
        "bomber_heavy",
    ];

    private float _decisionTimer;
    private SkirmishDifficultyTier _difficulty = SkirmishDifficultyTier.Normal;

    public void SetDifficulty(SkirmishDifficultyTier tier) => _difficulty = tier;

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        _decisionTimer -= deltaTime;
        if (_decisionTimer > 0f) return;
        _decisionTimer = SkirmishDifficultyTuning.AiBuildQueueIntervalSeconds(_difficulty);

        foreach (var (_, building) in world.Query<BuildingComponent>())
        {
            if (building.PlayerId <= 1) continue;
            if (!IsAiOwnedPlayer(world, building.PlayerId)) continue;
            if (!IsShipyard(building.BuildingType)) continue;
            if (building.BuildQueue.Count >= 2) continue;
            if (building.Producible.Count == 0) continue;

            string? next = PickProduction(building);
            if (next == null) continue;

            building.BuildQueue.Enqueue(next);
        }
    }

    internal static string? PickProduction(BuildingComponent building)
    {
        var queued = building.BuildQueue.ToArray();

        foreach (string ladderId in ProductionLadder)
        {
            string? match = building.Producible.FirstOrDefault(id =>
                id.Equals(ladderId, StringComparison.OrdinalIgnoreCase));
            if (match == null) continue;

            bool alreadyQueued = queued.Any(id =>
                id.Equals(match, StringComparison.OrdinalIgnoreCase)
                || (ladderId.Contains("miner", StringComparison.OrdinalIgnoreCase)
                    && id.Contains("miner", StringComparison.OrdinalIgnoreCase)));
            if (!alreadyQueued)
                return match;
        }

        return building.Producible.FirstOrDefault(id =>
            !queued.Any(q => q.Equals(id, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool IsShipyard(string buildingType) =>
        buildingType.StartsWith("shipyard", StringComparison.OrdinalIgnoreCase);

    private static bool IsAiOwnedPlayer(World world, int playerId)
    {
        if (playerId <= 1) return false;

        foreach (var (_, ai) in world.Query<AIControlledComponent>())
        {
            if (ai.PlayerId == playerId)
                return true;
        }

        return false;
    }
}