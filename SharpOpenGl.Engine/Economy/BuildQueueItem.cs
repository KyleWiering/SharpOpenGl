namespace SharpOpenGl.Engine.Economy;

/// <summary>
/// A single item pending in a <see cref="BuildQueue"/>.
/// Resources are deducted when this item is enqueued; the cost is stored here
/// so it can be refunded if the item is cancelled.
/// </summary>
public sealed class BuildQueueItem
{
    internal BuildQueueItem(
        string definitionId,
        int energy, int minerals, int data, int crew,
        float buildTime)
    {
        DefinitionId = definitionId;
        EnergyCost   = energy;
        MineralsCost = minerals;
        DataCost     = data;
        CrewCost     = crew;
        TotalBuildTime = buildTime;
    }

    /// <summary>Entity-definition key (matches GameData JSON filename without extension).</summary>
    public string DefinitionId { get; }

    /// <summary>Energy cost paid at enqueue time.</summary>
    public int EnergyCost { get; }

    /// <summary>Minerals cost paid at enqueue time.</summary>
    public int MineralsCost { get; }

    /// <summary>Data cost paid at enqueue time.</summary>
    public int DataCost { get; }

    /// <summary>Crew cost paid at enqueue time.</summary>
    public int CrewCost { get; }

    /// <summary>Total seconds required to produce this item at 1× production rate.</summary>
    public float TotalBuildTime { get; }

    /// <summary>Seconds elapsed building this item so far.</summary>
    public float Progress { get; internal set; }

    /// <summary>Build completion fraction in [0, 1].</summary>
    public float Fraction => TotalBuildTime > 0f ? Progress / TotalBuildTime : 1f;

    /// <summary><c>true</c> when the item has finished building.</summary>
    public bool IsComplete => Progress >= TotalBuildTime;
}
