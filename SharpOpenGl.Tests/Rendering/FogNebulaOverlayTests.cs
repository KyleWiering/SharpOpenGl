using OpenTK.Mathematics;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class FogNebulaOverlayTests
{
    [Fact]
    public void FogNebulaOverlay_skips_visible_chunks()
    {
        var grid = new GridSystem(20, 20, cellSize: 2f);
        var fog = new FogOfWar(grid, playerCount: 1);
        fog.Reveal(0, 10, 10, radius: 1);

        var overlay = new FogNebulaOverlay();
        overlay.Sync(fog, grid, playerId: 0, chunkWorldSize: FogNebulaOverlay.ChunkCells * 2f);

        Assert.Null(FogNebulaOverlay.ResolveOverlayState(fog, grid, playerId: 0, chunkX: 1, chunkY: 1));
        Assert.False(overlay.HasEmitterForChunk(1, 1));

        Assert.Equal(FogState.Unexplored,
            FogNebulaOverlay.ResolveOverlayState(fog, grid, playerId: 0, chunkX: 0, chunkY: 0));
        Assert.True(overlay.HasEmitterForChunk(0, 0));
    }

    [Fact]
    public void FogNebulaOverlay_distinguishes_explored_and_unexplored_veil_alphas()
    {
        Assert.True(FogNebulaOverlay.Config.UnexploredVeilAlpha > FogNebulaOverlay.Config.ExploredVeilAlpha);
        Assert.True(FogVisualPalette.UnexploredAlpha > FogVisualPalette.ExploredAlpha);
    }

    [Fact]
    public void Sync_builds_static_veil_quads_not_particles()
    {
        var grid = new GridSystem(12, 12, cellSize: 10f);
        var fog = new FogOfWar(grid, playerCount: 1);
        var overlay = new FogNebulaOverlay();

        overlay.Sync(fog, grid, playerId: 0, chunkWorldSize: 100f);
        overlay.Update(0.5f);

        Assert.True(overlay.VeilQuads.Count > 0);
        Assert.All(overlay.VeilQuads, q => Assert.True(q.Color.W > 0f));
        Assert.All(overlay.VeilQuads, q => Assert.True(q.SizeX > 0f && q.SizeZ > 0f));
    }

    [Fact]
    public void Sync_skips_visible_cells_in_camera_view()
    {
        var grid = new GridSystem(20, 20, cellSize: 10f);
        var fog = new FogOfWar(grid, playerCount: 1);
        fog.Reveal(0, 10, 10, radius: 2);

        var overlay = new FogNebulaOverlay();
        overlay.Sync(fog, grid, playerId: 0, chunkWorldSize: 100f);

        Assert.DoesNotContain(overlay.VeilQuads, q => q.GridX == 10 && q.GridY == 10);
        Assert.Contains(overlay.VeilQuads, q => q.GridX == 0 && q.GridY == 0);
    }

    [Fact]
    public void ResolveOverlayState_returns_explored_for_explored_only_chunk()
    {
        var grid = new GridSystem(20, 20, cellSize: 2f);
        var fog = new FogOfWar(grid, playerCount: 1);

        for (int x = 0; x < 10; x++)
        for (int y = 0; y < 10; y++)
            grid.GetCell(x, y)!.SetFog(0, FogState.Explored);

        Assert.Equal(
            FogState.Explored,
            FogNebulaOverlay.ResolveOverlayState(fog, grid, playerId: 0, chunkX: 0, chunkY: 0));
    }

    [Fact]
    public void ResolveOverlayState_returns_unexplored_for_unseen_chunk()
    {
        var grid = new GridSystem(20, 20, cellSize: 2f);
        var fog = new FogOfWar(grid, playerCount: 1);

        Assert.Equal(
            FogState.Unexplored,
            FogNebulaOverlay.ResolveOverlayState(fog, grid, playerId: 0, chunkX: 0, chunkY: 0));
    }

    [Fact]
    public void FogNebulaOverlay_recycles_emitters_when_chunk_becomes_visible()
    {
        var grid = new GridSystem(20, 20, cellSize: 2f);
        var fog = new FogOfWar(grid, playerCount: 1);
        var overlay = new FogNebulaOverlay();
        float chunkWorld = FogNebulaOverlay.ChunkCells * 2f;

        overlay.Sync(fog, grid, playerId: 0, chunkWorld);
        Assert.True(overlay.HasEmitterForChunk(0, 0));

        fog.Reveal(0, 1, 1, radius: 2);
        overlay.Sync(fog, grid, playerId: 0, chunkWorld);

        Assert.Null(FogNebulaOverlay.ResolveOverlayState(fog, grid, playerId: 0, chunkX: 0, chunkY: 0));
        Assert.False(overlay.HasEmitterForChunk(0, 0));
    }
}