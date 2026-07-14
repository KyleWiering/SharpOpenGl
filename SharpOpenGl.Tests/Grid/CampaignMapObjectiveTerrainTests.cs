using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Missions;
using Xunit;

namespace SharpOpenGl.Tests.Grid;

/// <summary>
/// P03-D07 campaign map objective terrain review — reach objectives vs authored terrain hazards.
/// </summary>
public class CampaignMapObjectiveTerrainTests
{
    [Fact]
    public void Sector_alpha_salvage_objectives_near_debris_and_nebula_terrain()
    {
        var assets = new AssetManager(GetGameDataPath());
        var map = assets.Load<MapDefinition>("Maps/sector_alpha");
        var mission = new MissionLoader(assets).Load("mission_abandoned_salvage");

        Assert.NotNull(map);
        Assert.NotNull(mission);

        var grid = CreateDesktopGrid();
        MapTerrainApplicator.ApplyToGrid(grid, map!);

        var reachObjectives = mission!.Objectives.Primary
            .Where(o => o.Type == "reach_area" && o.Position.Length >= 2)
            .ToList();

        Assert.Equal(2, reachObjectives.Count);

        foreach (var objective in reachObjectives)
        {
            int gx = (int)objective.Position![0];
            int gy = (int)objective.Position[1];
            GridCell? cell = grid.GetCell(gx, gy);
            Assert.NotNull(cell);

            bool nearHazard = HasPathingTerrainWithin(grid, gx, gy, radius: 8);
            Assert.True(nearHazard,
                $"Objective {objective.Id} at [{gx},{gy}] should sit near authored terrain for campaign readability.");
        }
    }

    [Fact]
    public void Sector_alpha_training_harvest_spawn_can_reach_mineral_terrain_corridor()
    {
        var assets = new AssetManager(GetGameDataPath());
        var map = assets.Load<MapDefinition>("Maps/sector_alpha");
        var mission = new MissionLoader(assets).Load("training_03_harvest");

        Assert.NotNull(map);
        Assert.NotNull(mission);

        var grid = CreateDesktopGrid();
        MapTerrainApplicator.ApplyToGrid(grid, map!);

        int[] spawn = mission!.StartConditions.PlayerSpawn;
        var mineral = map!.ResourceNodes.First(n => n.Type == "minerals");

        Assert.False(PathBlockedBetween(grid, spawn[0], spawn[1], mineral.Position[0], mineral.Position[1]));
    }

    private static bool HasPathingTerrainWithin(GridSystem grid, int cx, int cy, int radius)
    {
        for (int x = cx - radius; x <= cx + radius; x++)
        for (int y = cy - radius; y <= cy + radius; y++)
        {
            GridCell? cell = grid.GetCell(x, y);
            if (cell != null && cell.Terrain != TerrainType.Space)
                return true;
        }

        return false;
    }

    private static bool PathBlockedBetween(GridSystem grid, int sx, int sy, int gx, int gy)
    {
        GridCell? start = grid.GetCell(sx, sy);
        GridCell? goal = grid.GetCell(gx, gy);
        if (start == null || goal == null) return true;

        var path = Pathfinding.FindPath(grid, start, goal);
        return path.Count == 0;
    }

    private static GridSystem CreateDesktopGrid()
    {
        const int extent = 200;
        const float cellSize = 10f;
        float half = extent * cellSize * 0.5f;
        return new GridSystem(extent, extent, cellSize, new OpenTK.Mathematics.Vector2(-half, -half));
    }

    private static string GetGameDataPath()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "SharpOpenGl.sln")))
            dir = Directory.GetParent(dir)?.FullName;

        if (dir == null)
            throw new InvalidOperationException("Could not locate SharpOpenGl.sln from base directory.");

        return Path.Combine(dir, "GameData");
    }
}