namespace SharpOpenGl.Engine.Economy;

/// <summary>
/// Immutable snapshot of a single resource's state, used for UI display.
/// </summary>
public readonly record struct ResourceDisplay(
    ResourceType Type,
    float Current,
    float Max,
    float IncomePerSecond
)
{
    /// <summary>Fill level in [0, 1].</summary>
    public float Fraction => Max > 0f ? Current / Max : 0f;

    /// <summary><c>true</c> when the resource has reached its storage cap.</summary>
    public bool IsFull => Current >= Max;
}
