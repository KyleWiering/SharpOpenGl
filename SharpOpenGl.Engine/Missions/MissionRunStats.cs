using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Engine.Missions;

/// <summary>Session counters surfaced on the victory/defeat overlay (P08-D09).</summary>
public sealed class MissionRunStats
{
    public int EnemiesDestroyed { get; private set; }
    public int UnitsLost { get; private set; }
    public int StructuresLost { get; private set; }
    public int StructuresBuilt { get; set; }

    /// <summary>Record a unit death for run statistics.</summary>
    public void RecordDeath(World world, Entity victim, Entity killer)
    {
        int victimPlayer = TeamVisualResolver.ResolvePlayerId(world, victim);
        int killerPlayer = world.IsAlive(killer) ? TeamVisualResolver.ResolvePlayerId(world, killer) : 0;
        bool isStation = world.HasComponent<BuildingComponent>(victim);

        if (victimPlayer == 1)
        {
            if (isStation) StructuresLost++;
            else UnitsLost++;
            return;
        }

        if (killerPlayer == 1 && victimPlayer != 1)
            EnemiesDestroyed++;
    }

    /// <summary>Format stats lines for the victory overlay.</summary>
    public IReadOnlyList<string> FormatSummaryLines(bool isVictory)
    {
        var lines = new List<string>
        {
            $"Enemies destroyed: {EnemiesDestroyed}",
            $"Units lost: {UnitsLost}",
        };

        if (StructuresLost > 0)
            lines.Add($"Structures lost: {StructuresLost}");

        if (StructuresBuilt > 0)
            lines.Add($"Structures built: {StructuresBuilt}");

        if (!isVictory && EnemiesDestroyed == 0 && UnitsLost == 0)
            lines.Add("No combat engagements recorded");

        return lines;
    }
}