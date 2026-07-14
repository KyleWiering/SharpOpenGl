using OpenTK.Mathematics;
using SharpOpenGl.Engine.Build;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Grid;
using Xunit;

namespace SharpOpenGl.Tests.Grid;

public class PathfindingTests
{
    // ── A* basic ─────────────────────────────────────────────────────────────

    [Fact]
    public void FindPath_same_cell_returns_empty()
    {
        var grid = new GridSystem(5, 5);
        GridCell start = grid.GetCell(2, 2)!;
        var path = Pathfinding.FindPath(grid, start, start);
        Assert.Empty(path);
    }

    [Fact]
    public void FindPath_adjacent_cells_returns_single_step()
    {
        var grid = new GridSystem(5, 5);
        GridCell start = grid.GetCell(0, 0)!;
        GridCell goal  = grid.GetCell(1, 0)!;
        var path = Pathfinding.FindPath(grid, start, goal);
        Assert.Single(path);
        Assert.Equal(goal, path[0]);
    }

    [Fact]
    public void FindPath_straight_line_horizontal()
    {
        var grid = new GridSystem(10, 5);
        GridCell start = grid.GetCell(0, 2)!;
        GridCell goal  = grid.GetCell(4, 2)!;
        var path = Pathfinding.FindPath(grid, start, goal);
        Assert.Equal(4, path.Count);
        Assert.Equal(goal, path[^1]);
    }

    [Fact]
    public void FindPath_path_starts_after_start_cell()
    {
        var grid = new GridSystem(10, 5);
        GridCell start = grid.GetCell(0, 0)!;
        GridCell goal  = grid.GetCell(3, 0)!;
        var path = Pathfinding.FindPath(grid, start, goal);
        Assert.DoesNotContain(start, path);
        Assert.Equal(goal, path[^1]);
    }

    // ── Obstacles ─────────────────────────────────────────────────────────────

    [Fact]
    public void FindPath_navigates_around_wall()
    {
        // 5×5 grid, wall from (2,0) to (2,3), open at (2,4)
        var grid = new GridSystem(5, 5);
        for (int y = 0; y <= 3; y++)
            grid.GetCell(2, y)!.Terrain = TerrainType.Impassable;

        GridCell start = grid.GetCell(0, 2)!;
        GridCell goal  = grid.GetCell(4, 2)!;
        var path = Pathfinding.FindPath(grid, start, goal);

        Assert.NotEmpty(path);
        Assert.Equal(goal, path[^1]);
        // Path must not cross impassable cells
        foreach (GridCell cell in path)
            Assert.True(cell.IsPassable);
    }

    [Fact]
    public void FindPath_returns_empty_when_goal_impassable()
    {
        var grid = new GridSystem(5, 5);
        GridCell start = grid.GetCell(0, 0)!;
        GridCell goal  = grid.GetCell(4, 4)!;
        goal.Terrain = TerrainType.Impassable;

        var path = Pathfinding.FindPath(grid, start, goal);
        Assert.Empty(path);
    }

    [Fact]
    public void FindPath_returns_empty_when_no_path_exists()
    {
        // Surround (0,0) with impassable wall so it cannot reach (4,4)
        var grid = new GridSystem(5, 5);
        grid.GetCell(1, 0)!.Terrain = TerrainType.Impassable;
        grid.GetCell(0, 1)!.Terrain = TerrainType.Impassable;

        GridCell start = grid.GetCell(0, 0)!;
        GridCell goal  = grid.GetCell(4, 4)!;
        var path = Pathfinding.FindPath(grid, start, goal);
        Assert.Empty(path);
    }

    [Fact]
    public void FindPath_avoids_occupied_footprint_cells()
    {
        var grid = new GridSystem(9, 5, cellSize: 1f);
        var world = new World();
        var building = world.CreateEntity();
        var buildingPos = grid.GridToWorld(4, 2);
        BuildingFootprint.Occupy(grid, building, buildingPos, [2, 2]);

        GridCell start = grid.GetCell(0, 2)!;
        GridCell goal = grid.GetCell(8, 2)!;
        var path = Pathfinding.FindPath(grid, start, goal);

        Assert.NotEmpty(path);
        Assert.Equal(goal, path[^1]);
        foreach (GridCell cell in path)
            Assert.Equal(Entity.Null, cell.Occupant);
    }

    [Fact]
    public void FindPath_returns_empty_when_goal_inside_occupied_footprint()
    {
        var grid = new GridSystem(9, 5, cellSize: 1f);
        var world = new World();
        var building = world.CreateEntity();
        var buildingPos = grid.GridToWorld(4, 2);
        BuildingFootprint.Occupy(grid, building, buildingPos, [2, 2]);

        GridCell start = grid.GetCell(0, 2)!;
        GridCell goal = grid.GetCell(4, 2)!;
        var path = Pathfinding.FindPath(grid, start, goal);
        Assert.Empty(path);
    }

    // ── Flow field ────────────────────────────────────────────────────────────

    [Fact]
    public void FlowField_goal_direction_is_zero()
    {
        var grid = new GridSystem(5, 5);
        GridCell goal = grid.GetCell(2, 2)!;
        var ff = FlowField.Build(grid, goal);
        Vector2? dir = ff.GetDirection(goal);
        Assert.NotNull(dir);
        Assert.Equal(Vector2.Zero, dir!.Value);
    }

    [Fact]
    public void FlowField_adjacent_cell_points_toward_goal()
    {
        var grid = new GridSystem(5, 5);
        GridCell goal = grid.GetCell(2, 2)!;
        var ff = FlowField.Build(grid, goal);

        // Cell to the right of goal should point left (toward goal)
        Vector2? dir = ff.GetDirection(3, 2);
        Assert.NotNull(dir);
        Assert.True(dir!.Value.X < 0f, "Direction should be negative X (toward goal)");
    }

    [Fact]
    public void FlowField_unreachable_cell_returns_null()
    {
        // Isolate (0,0) completely
        var grid = new GridSystem(5, 5);
        grid.GetCell(1, 0)!.Terrain = TerrainType.Impassable;
        grid.GetCell(0, 1)!.Terrain = TerrainType.Impassable;

        GridCell goal = grid.GetCell(3, 3)!;
        var ff = FlowField.Build(grid, goal);

        Vector2? dir = ff.GetDirection(0, 0);
        Assert.Null(dir);
    }

    // ── MapLoader ─────────────────────────────────────────────────────────────

    [Fact]
    public void MapLoader_FromDefinition_creates_correct_grid()
    {
        var def = new MapDefinition
        {
            Id = "test",
            GridSize = [16, 8],
            CellSize = 2.0f,
        };

        GridSystem grid = MapLoader.FromDefinition(def);
        Assert.Equal(16, grid.Width);
        Assert.Equal(8,  grid.Height);
        Assert.Equal(2.0f, grid.CellSize);
    }

    [Fact]
    public void MapLoader_applies_terrain_regions()
    {
        var def = new MapDefinition
        {
            GridSize = [10, 10],
            Terrain = new MapTerrain
            {
                Default = "space",
                Regions =
                [
                    new MapTerrainRegion
                    {
                        Type  = "asteroid_field",
                        Cells = [[2, 2], [3, 2]],
                    },
                ],
            },
        };

        GridSystem grid = MapLoader.FromDefinition(def);
        Assert.Equal(TerrainType.AsteroidField, grid.GetCell(2, 2)!.Terrain);
        Assert.Equal(TerrainType.AsteroidField, grid.GetCell(3, 2)!.Terrain);
        Assert.Equal(TerrainType.Space,         grid.GetCell(4, 2)!.Terrain);
    }

    [Fact]
    public void MapLoader_applies_rect_terrain_region()
    {
        var def = new MapDefinition
        {
            GridSize = [20, 20],
            Terrain = new MapTerrain
            {
                Default = "space",
                Regions =
                [
                    new MapTerrainRegion
                    {
                        Type = "nebula",
                        Rect = [5, 5, 8, 8],
                    },
                ],
            },
        };

        GridSystem grid = MapLoader.FromDefinition(def);
        for (int x = 5; x <= 8; x++)
        for (int y = 5; y <= 8; y++)
            Assert.Equal(TerrainType.Nebula, grid.GetCell(x, y)!.Terrain);

        Assert.Equal(TerrainType.Space, grid.GetCell(0, 0)!.Terrain);
    }

    // ── FogOfWar ──────────────────────────────────────────────────────────────

    [Fact]
    public void FogOfWar_cells_start_unexplored()
    {
        var grid = new GridSystem(8, 8);
        var fog  = new FogOfWar(grid, playerCount: 2);
        Assert.Equal(FogState.Unexplored, fog.GetState(0, 4, 4));
    }

    [Fact]
    public void FogOfWar_Reveal_marks_cells_visible()
    {
        var grid = new GridSystem(16, 16);
        var fog  = new FogOfWar(grid, playerCount: 2);
        fog.Reveal(playerId: 0, cx: 8, cy: 8, radius: 3);
        Assert.Equal(FogState.Visible, fog.GetState(0, 8, 8));
        Assert.Equal(FogState.Visible, fog.GetState(0, 8, 9));
    }

    [Fact]
    public void FogOfWar_Update_downgrades_visible_to_explored()
    {
        var grid = new GridSystem(16, 16);
        var fog  = new FogOfWar(grid, playerCount: 1);
        fog.Reveal(0, 8, 8, 3);
        Assert.Equal(FogState.Visible, fog.GetState(0, 8, 8));

        // Update with no units — all visible cells should become explored
        fog.Update(0, Enumerable.Empty<(int, int, int)>());
        Assert.Equal(FogState.Explored, fog.GetState(0, 8, 8));
    }

    // ── MapGenerator ─────────────────────────────────────────────────────────

    [Fact]
    public void MapGenerator_same_seed_produces_same_map()
    {
        var gen1 = new MapGenerator(seed: 42);
        var gen2 = new MapGenerator(seed: 42);
        GridSystem g1 = gen1.Generate(16, 16);
        GridSystem g2 = gen2.Generate(16, 16);

        for (int x = 0; x < 16; x++)
        for (int y = 0; y < 16; y++)
            Assert.Equal(g1.GetCell(x, y)!.Terrain, g2.GetCell(x, y)!.Terrain);
    }

    [Fact]
    public void MapGenerator_corners_are_clear()
    {
        var gen  = new MapGenerator(seed: 1);
        GridSystem grid = gen.Generate(16, 16);
        // Top-left corner 3×3 must be passable
        for (int x = 0; x < 3; x++)
        for (int y = 0; y < 3; y++)
            Assert.True(grid.GetCell(x, y)!.IsPassable);
    }
}
