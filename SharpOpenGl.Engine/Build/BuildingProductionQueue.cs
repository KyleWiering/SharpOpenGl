using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.Entities;

namespace SharpOpenGl.Engine.Build;

/// <summary>
/// Helpers for mutating <see cref="BuildingComponent"/> production queues with refunds.
/// </summary>
public static class BuildingProductionQueue
{
    /// <summary>
    /// Cancel the queue item at <paramref name="queueIndex1Based"/> (1 = front/current).
    /// Refunds full resource cost and releases reserved supply crew.
    /// </summary>
    public static bool TryCancelAtIndex(
        BuildingComponent building,
        int queueIndex1Based,
        Func<string, EntityDefinition?> definitionLookup,
        ResourceManager resources,
        SupplySystem? supply,
        int playerId)
    {
        if (queueIndex1Based < 1 || queueIndex1Based > building.BuildQueue.Count)
            return false;

        var items = building.BuildQueue.ToList();
        int index = queueIndex1Based - 1;
        string definitionId = items[index];
        EntityDefinition? definition = definitionLookup(definitionId);
        if (definition == null)
            return false;

        int energy = definition.Cost?.Energy ?? 0;
        int minerals = definition.Cost?.Minerals ?? 0;
        int data = definition.Cost?.Data ?? 0;
        int crew = definition.Cost?.Crew ?? 0;

        resources.Refund(playerId, energy, minerals, data, crew);
        if (supply != null && crew > 0)
            supply.ReleaseSupply(playerId, crew);

        items.RemoveAt(index);
        building.BuildQueue.Clear();
        foreach (string item in items)
            building.BuildQueue.Enqueue(item);

        if (index == 0)
            building.BuildProgress = 0f;

        return true;
    }
}