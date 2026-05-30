using OpenTK.Mathematics;
using SharpOpenGl.Engine.Grid;
using Xunit;

namespace SharpOpenGl.Tests.Grid;

public class GridSystemTests
{
    // ── Construction ──────────────────────────────────────────────────────────

    [Fact]
    public void GridSystem_creates_correct_dimensions()
    {
        var grid = new GridSystem(32, 16);
        Assert.Equal(32, grid.Width);
        Assert.Equal(16, grid.Height);
    }

    [Fact]
    public void GetCell_returns_non_null_for_valid_coords()
    {
        var grid = new GridSystem(10, 10);
        Assert.NotNull(grid.GetCell(0, 0));
        Assert.NotNull(grid.GetCell(9, 9));
        Assert.NotNull(grid.GetCell(5, 5));
    }

    [Fact]
    public void GetCell_returns_null_for_out_of_bounds()
    {
        var grid = new GridSystem(10, 10);
        Assert.Null(grid.GetCell(-1, 0));
        Assert.Null(grid.GetCell(0, -1));
        Assert.Null(grid.GetCell(10, 0));
        Assert.Null(grid.GetCell(0, 10));
    }

    [Fact]
    public void AllCells_returns_width_times_height_cells()
    {
        var grid = new GridSystem(8, 4);
        Assert.Equal(32, grid.AllCells().Count());
    }

    // ── InBounds ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(9, 9, true)]
    [InlineData(10, 0, false)]
    [InlineData(0, 10, false)]
    [InlineData(-1, 5, false)]
    public void InBounds_returns_correct_result(int x, int y, bool expected)
    {
        var grid = new GridSystem(10, 10);
        Assert.Equal(expected, grid.InBounds(x, y));
    }

    // ── Coordinate conversion ─────────────────────────────────────────────────

    [Fact]
    public void GridToWorld_returns_cell_centre()
    {
        var grid = new GridSystem(10, 10, cellSize: 2.0f);
        Vector3 world = grid.GridToWorld(0, 0);
        Assert.Equal(1.0f, world.X, precision: 4); // centre of cell (0,0): 0 + 0.5 * 2
        Assert.Equal(1.0f, world.Z, precision: 4);
        Assert.Equal(0.0f, world.Y, precision: 4); // surface layer
    }

    [Fact]
    public void GridToWorld_orbital_layer_has_height_offset()
    {
        var grid = new GridSystem(10, 10, cellSize: 1.0f);
        Vector3 surface = grid.GridToWorld(3, 3, GridLayer.Surface);
        Vector3 orbital = grid.GridToWorld(3, 3, GridLayer.Orbital);
        Assert.Equal(surface.X, orbital.X, precision: 4);
        Assert.Equal(surface.Z, orbital.Z, precision: 4);
        Assert.True(orbital.Y > surface.Y);
    }

    [Fact]
    public void WorldToGrid_converts_back_from_grid_centre()
    {
        var grid = new GridSystem(10, 10, cellSize: 1.0f);
        Vector3 world = grid.GridToWorld(5, 7);
        bool inBounds = grid.WorldToGrid(world, out int x, out int y);
        Assert.True(inBounds);
        Assert.Equal(5, x);
        Assert.Equal(7, y);
    }

    [Fact]
    public void WorldToGrid_returns_false_for_out_of_bounds()
    {
        var grid = new GridSystem(10, 10, cellSize: 1.0f);
        bool inBounds = grid.WorldToGrid(new Vector3(-5, 0, -5), out _, out _);
        Assert.False(inBounds);
    }

    [Fact]
    public void WorldToGrid_roundtrip_all_cells()
    {
        var grid = new GridSystem(8, 8, cellSize: 2.0f);
        for (int x = 0; x < 8; x++)
        for (int y = 0; y < 8; y++)
        {
            Vector3 world = grid.GridToWorld(x, y);
            bool ok = grid.WorldToGrid(world, out int rx, out int ry);
            Assert.True(ok);
            Assert.Equal(x, rx);
            Assert.Equal(y, ry);
        }
    }

    // ── Neighbours ────────────────────────────────────────────────────────────

    [Fact]
    public void GetNeighbours_cardinal_interior_returns_4()
    {
        var grid = new GridSystem(5, 5);
        GridCell? cell = grid.GetCell(2, 2);
        Assert.NotNull(cell);
        Assert.Equal(4, grid.GetNeighbours(cell).Count());
    }

    [Fact]
    public void GetNeighbours_corner_returns_2()
    {
        var grid = new GridSystem(5, 5);
        GridCell? cell = grid.GetCell(0, 0);
        Assert.NotNull(cell);
        Assert.Equal(2, grid.GetNeighbours(cell).Count());
    }

    [Fact]
    public void GetNeighbours_diagonal_interior_returns_8()
    {
        var grid = new GridSystem(5, 5);
        GridCell? cell = grid.GetCell(2, 2);
        Assert.NotNull(cell);
        Assert.Equal(8, grid.GetNeighbours(cell, diagonal: true).Count());
    }

    [Fact]
    public void GetNeighbours_excludes_impassable_cells()
    {
        var grid = new GridSystem(5, 5);
        // Block all cardinal neighbours of (2,2)
        grid.GetCell(3, 2)!.Terrain = TerrainType.Impassable;
        grid.GetCell(1, 2)!.Terrain = TerrainType.Impassable;
        grid.GetCell(2, 3)!.Terrain = TerrainType.Impassable;
        grid.GetCell(2, 1)!.Terrain = TerrainType.Impassable;

        GridCell? cell = grid.GetCell(2, 2);
        Assert.NotNull(cell);
        Assert.Empty(grid.GetNeighbours(cell));
    }

    // ── Layers ────────────────────────────────────────────────────────────────

    [Fact]
    public void Surface_and_orbital_cells_are_independent()
    {
        var grid = new GridSystem(5, 5);
        GridCell? surface = grid.GetCell(2, 2, GridLayer.Surface);
        GridCell? orbital = grid.GetCell(2, 2, GridLayer.Orbital);
        Assert.NotNull(surface);
        Assert.NotNull(orbital);
        Assert.NotSame(surface, orbital);

        surface!.Terrain = TerrainType.AsteroidField;
        Assert.Equal(TerrainType.Space, orbital!.Terrain);
    }
}
