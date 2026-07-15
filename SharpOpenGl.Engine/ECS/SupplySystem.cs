using SharpOpenGl.Engine.Economy;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Tracks supply/crew capacity provided by buildings and enforces population cap.
/// Each building type contributes a fixed supply amount. Units consume supply based
/// on their crew cost.
/// </summary>
public sealed class SupplySystem : GameSystem
{
    private readonly ResourceManager _resources;

    /// <summary>Supply amounts provided per building type.</summary>
    private static readonly Dictionary<string, int> SupplyPerBuilding = new()
    {
        ["command_center"] = 20,
        ["shipyard_small"] = 6,
        ["shipyard_medium"] = 10,
        ["shipyard_large"] = 18,
        ["shipyard"] = 10,
        ["supply_depot"] = 15,
        ["resource_refinery"] = 5,
        ["repair_bay"] = 8,
        ["power_reactor"] = 10,
        ["defense_turret"] = 0,
        ["sensor_array"] = 0,
    };

    /// <summary>Current supply used per player.</summary>
    private readonly Dictionary<int, int> _used = new();

    /// <summary>Current supply cap per player.</summary>
    private readonly Dictionary<int, int> _cap = new();

    public SupplySystem(ResourceManager resources) => _resources = resources;

    /// <summary>Get current supply used for a player.</summary>
    public int GetUsed(int playerId) => _used.GetValueOrDefault(playerId);

    /// <summary>Get current supply cap for a player.</summary>
    public int GetCap(int playerId) => _cap.GetValueOrDefault(playerId);

    /// <summary>Check if player can afford the supply cost.</summary>
    public bool CanAffordSupply(int playerId, int cost)
    {
        int used = _used.GetValueOrDefault(playerId);
        int cap = _cap.GetValueOrDefault(playerId);
        return used + cost <= cap;
    }

    /// <summary>Consume supply for a newly produced unit.</summary>
    public void ConsumeSupply(int playerId, int cost)
    {
        _used[playerId] = _used.GetValueOrDefault(playerId) + cost;
    }

    /// <summary>Release supply reserved when a queued production order is cancelled.</summary>
    public void ReleaseSupply(int playerId, int cost)
    {
        if (cost <= 0) return;
        int used = _used.GetValueOrDefault(playerId);
        _used[playerId] = Math.Max(0, used - cost);
    }

    /// <inheritdoc/>
    public override void Update(World world, float deltaTime)
    {
        // Recalculate caps each frame from buildings
        _cap.Clear();

        foreach (var (_, building) in world.Query<BuildingComponent>())
        {
            int supply = SupplyPerBuilding.GetValueOrDefault(building.BuildingType);
            if (supply > 0)
            {
                int pid = building.PlayerId;
                _cap[pid] = _cap.GetValueOrDefault(pid) + supply;
            }
        }
    }
}
