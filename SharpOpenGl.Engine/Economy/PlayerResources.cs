namespace SharpOpenGl.Engine.Economy;

/// <summary>
/// Tracks all four resource amounts for a single player.
/// Amounts are always kept within [0, maxStorage].
/// </summary>
public sealed class PlayerResources
{
    private static readonly int Count = Enum.GetValues<ResourceType>().Length; // 4

    private readonly float[] _amounts        = new float[Count];
    private readonly float[] _maxStorage     = new float[Count];
    private readonly float[] _incomePerSecond = new float[Count];

    /// <param name="energyMax">Max Plasma Core storage (default 2000).</param>
    /// <param name="mineralsMax">Max Astrium Ore storage (default 5000).</param>
    /// <param name="dataMax">Max Quantum Fragment storage (default 1000).</param>
    /// <param name="crewMax">Max Personnel storage (default 200).</param>
    public PlayerResources(
        float energyMax   = 2000f,
        float mineralsMax = 5000f,
        float dataMax     = 1000f,
        float crewMax     = 200f)
    {
        _maxStorage[(int)ResourceType.Energy]   = energyMax;
        _maxStorage[(int)ResourceType.Minerals] = mineralsMax;
        _maxStorage[(int)ResourceType.Data]     = dataMax;
        _maxStorage[(int)ResourceType.Crew]     = crewMax;
    }

    /// <summary>Current stored amount of <paramref name="type"/>.</summary>
    public float GetAmount(ResourceType type) => _amounts[(int)type];

    /// <summary>Maximum storable units of <paramref name="type"/>.</summary>
    public float GetMax(ResourceType type) => _maxStorage[(int)type];

    /// <summary>Net income rate (units per second) for <paramref name="type"/>. May be negative.</summary>
    public float GetIncomePerSecond(ResourceType type) => _incomePerSecond[(int)type];

    /// <summary>
    /// Adjust the income rate for <paramref name="type"/> by <paramref name="delta"/> units/sec.
    /// Use a negative delta to remove income (e.g. building destroyed).
    /// </summary>
    public void AddIncome(ResourceType type, float delta) =>
        _incomePerSecond[(int)type] += delta;

    /// <summary>
    /// Set the initial stored amount for <paramref name="type"/>, clamped to [0, max].
    /// </summary>
    public void SetStartingAmount(ResourceType type, float amount) =>
        _amounts[(int)type] = Math.Clamp(amount, 0f, _maxStorage[(int)type]);

    /// <summary>
    /// Attempt to subtract <paramref name="amount"/> from <paramref name="type"/>.
    /// Returns <c>true</c> and performs the deduction when sufficient funds exist;
    /// returns <c>false</c> without changing state otherwise.
    /// </summary>
    public bool TrySpend(ResourceType type, float amount)
    {
        int idx = (int)type;
        if (_amounts[idx] < amount) return false;
        _amounts[idx] -= amount;
        return true;
    }

    /// <summary>
    /// Add <paramref name="amount"/> units of <paramref name="type"/>,
    /// capped at max storage. Returns the actual amount added.
    /// </summary>
    public float Add(ResourceType type, float amount)
    {
        int idx = (int)type;
        float space  = _maxStorage[idx] - _amounts[idx];
        float actual = Math.Min(amount, space);
        _amounts[idx] += actual;
        return actual;
    }

    /// <summary>
    /// Advance one simulation tick: apply all income rates scaled by
    /// <paramref name="deltaTime"/>, clamping results to [0, max].
    /// </summary>
    public void Tick(float deltaTime)
    {
        for (int i = 0; i < Count; i++)
        {
            float newVal = _amounts[i] + _incomePerSecond[i] * deltaTime;
            _amounts[i] = Math.Clamp(newVal, 0f, _maxStorage[i]);
        }
    }

    /// <summary>Build a UI-ready snapshot for <paramref name="type"/>.</summary>
    public ResourceDisplay GetDisplay(ResourceType type) => new(
        type,
        _amounts[(int)type],
        _maxStorage[(int)type],
        _incomePerSecond[(int)type]
    );
}
