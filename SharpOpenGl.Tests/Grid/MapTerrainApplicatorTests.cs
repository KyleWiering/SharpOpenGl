using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.Grid;
using Xunit;

namespace SharpOpenGl.Tests.Grid;

public class MapTerrainApplicatorTests
{
    [Fact]
    public void ApplyToGrid_sector_alpha_sets_authored_regions()
    {
        var map = new AssetManager(GetGameDataPath()).Load<MapDefinition>("Maps/sector_alpha");
        Assert.NotNull(map);

        var grid = CreateDesktopGrid();
        int applied = MapTerrainApplicator.ApplyToGrid(grid, map!);

        Assert.True(applied > 0);
        Assert.Equal(TerrainType.AsteroidField, grid.GetCell(10, 10)!.Terrain);
        Assert.Equal(TerrainType.Nebula, grid.GetCell(25, 25)!.Terrain);
        Assert.Equal(TerrainType.Debris, grid.GetCell(31, 5)!.Terrain);
        Assert.Equal(TerrainType.Space, grid.GetCell(0, 0)!.Terrain);
    }

    [Fact]
    public void ApplyToGrid_duel_frontier_sets_lane_hazards()
    {
        var map = new AssetManager(GetGameDataPath()).Load<MapDefinition>("Maps/duel_frontier");
        Assert.NotNull(map);

        var grid = CreateDesktopGrid();
        MapTerrainApplicator.ApplyToGrid(grid, map!);

        Assert.Equal(TerrainType.AsteroidField, grid.GetCell(50, 150)!.Terrain);
        Assert.Equal(TerrainType.Nebula, grid.GetCell(100, 100)!.Terrain);
        Assert.Equal(TerrainType.IonStorm, grid.GetCell(60, 100)!.Terrain);
        Assert.Equal(TerrainType.WormholeRemnant, grid.GetCell(100, 118)!.Terrain);
        Assert.Equal(TerrainType.Space, grid.GetCell(100, 80)!.Terrain);
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

    [Fact]
    public void MapLoader_FromDefinition_still_applies_terrain()
    {
        var def = new MapDefinition
        {
            GridSize = [8, 8],
            CellSize = 1f,
            Terrain = new MapTerrain
            {
                Default = "space",
                Regions =
                [
                    new MapTerrainRegion { Type = "nebula", Rect = [2, 2, 4, 4] }
                ],
            },
        };

        GridSystem grid = MapLoader.FromDefinition(def);
        Assert.Equal(TerrainType.Nebula, grid.GetCell(3, 3)!.Terrain);
    }
}