using OpenTK.Mathematics;
using SharpOpenGl.Engine.Assets;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Grid;

public class FixedMapFogTests
{
    [Fact]
    public void FixedMapFog_reveal_spawn_marks_visible_cells_on_campaign_grid()
    {
        var grid = CreateDesktopGrid();
        var fog = new FogOfWar(grid, playerCount: 2);

        Vector3 spawn = MapCoordinates.GridToWorld(8, 12);
        int visible = FixedMapFogBootstrap.RevealSpawnArea(
            grid, fog, spawn, FixedMapFogBootstrap.CampaignRevealRadius);

        Assert.True(visible > 0);
        Assert.Equal(FogState.Visible, fog.GetState(0, 8, 12));
        Assert.Equal(FogState.Unexplored, fog.GetState(0, 180, 180));
    }

    [Fact]
    public void FixedMapFog_overlay_contract_visible_chunk_has_no_veil()
    {
        var grid = new GridSystem(200, 200, cellSize: 10f);
        var fog = new FogOfWar(grid, playerCount: 1);

        Assert.True(FixedMapFogBootstrap.VerifyOverlayContract(fog, grid, playerId: 0, 10, 10));
    }

    [Fact]
    public void FixedMapFog_system_updates_after_unit_sight_on_loaded_map()
    {
        var map = new AssetManager(GetGameDataPath()).Load<MapDefinition>("Maps/duel_frontier");
        Assert.NotNull(map);

        var grid = CreateDesktopGrid();
        MapTerrainApplicator.ApplyToGrid(grid, map!);

        var fog = new FogOfWar(grid, playerCount: 1);
        var world = new World();
        var system = new FogOfWarSystem(grid, fog, playerId: 0);

        Entity ship = world.CreateEntity();
        world.AddComponent(ship, new TransformComponent
        {
            Position = grid.GridToWorld(28, 100),
        });
        world.AddComponent(ship, new SightRadiusComponent { Radius = 6 });

        system.Update(world, 0.016f);

        Assert.Equal(FogState.Visible, fog.GetState(0, 28, 100));
        Assert.Equal(FogState.Unexplored, fog.GetState(0, 0, 0));

        world.Dispose();
    }

    [Fact]
    public void FixedMapFog_explored_chunk_keeps_memory_overlay()
    {
        var grid = new GridSystem(40, 40, cellSize: 10f);
        var fog = new FogOfWar(grid, playerCount: 1);

        fog.Reveal(0, 5, 5, radius: 2);
        fog.Update(0, Enumerable.Empty<(int, int, int)>());

        Assert.Equal(FogState.Explored, fog.GetState(0, 5, 5));
        Assert.Equal(
            FogState.Explored,
            FogNebulaOverlay.ResolveOverlayState(fog, grid, playerId: 0, chunkX: 0, chunkY: 0));
    }

    [Fact]
    public void FixedMapFog_skirmish_human_reveal_radius_matches_bootstrap_constant()
    {
        Assert.Equal(18, FixedMapFogBootstrap.SkirmishHumanRevealRadius);
        Assert.Equal(10, FixedMapFogBootstrap.SkirmishAiRevealRadius);
    }

    private static GridSystem CreateDesktopGrid()
    {
        const int extent = 200;
        const float cellSize = 10f;
        float half = extent * cellSize * 0.5f;
        return new GridSystem(extent, extent, cellSize, new Vector2(-half, -half));
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