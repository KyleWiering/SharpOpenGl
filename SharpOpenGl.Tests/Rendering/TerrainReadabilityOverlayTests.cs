using OpenTK.Mathematics;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class TerrainReadabilityOverlayTests
{
    [Fact]
    public void Sync_skips_unexplored_cells()
    {
        var grid = new GridSystem(32, 32, cellSize: 10f);
        grid.GetCell(10, 10)!.Terrain = TerrainType.Nebula;

        var fog = new FogOfWar(grid, playerCount: 1);
        var overlay = new TerrainReadabilityOverlay();
        var bounds = new Vector4(-200f, 200f, -200f, 200f);

        overlay.Sync(grid, fog, playerId: 0, bounds);
        Assert.Equal(0, overlay.CellCount);

        fog.Reveal(0, 10, 10, radius: 1);
        overlay.Sync(grid, fog, playerId: 0, bounds);
        Assert.True(overlay.CellCount > 0);
    }

    [Fact]
    public void Sync_includes_explored_cells_with_reduced_alpha()
    {
        var grid = new GridSystem(16, 16, cellSize: 10f);
        grid.GetCell(4, 4)!.Terrain = TerrainType.Debris;

        var fog = new FogOfWar(grid, playerCount: 1);
        fog.Reveal(0, 4, 4, radius: 1);
        fog.Update(0, Enumerable.Empty<(int, int, int)>());

        var overlay = new TerrainReadabilityOverlay();
        overlay.Sync(grid, fog, playerId: 0, new Vector4(-500f, 500f, -500f, 500f));

        Assert.Equal(1, overlay.CellCount);
        Assert.True(overlay.Cells[0].Color.W < TerrainVisualPalette.ResolveTint(TerrainType.Debris)!.Value.W);
    }
}