using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl.Engine.Grid;

/// <summary>
/// Verifies and bootstraps fog-of-war on fixed (non-sandbox) campaign and skirmish maps.
/// </summary>
public static class FixedMapFogBootstrap
{
    /// <summary>Default spawn reveal radius for campaign missions (grid cells).</summary>
    public const int CampaignRevealRadius = 18;

    /// <summary>Default spawn reveal radius for skirmish human players (grid cells).</summary>
    public const int SkirmishHumanRevealRadius = 18;

    /// <summary>Default spawn reveal radius for skirmish AI players (grid cells).</summary>
    public const int SkirmishAiRevealRadius = 10;

    /// <summary>
    /// Reveal fog around a world-space spawn and return the number of cells now visible.
    /// </summary>
    public static int RevealSpawnArea(GridSystem grid, FogOfWar fog, Vector3 worldSpawn, int radiusCells,
        int playerId = 0)
    {
        if (!grid.WorldToGrid(worldSpawn, out int gx, out int gy))
            return 0;

        fog.Reveal(playerId, gx, gy, radiusCells);
        return CountVisibleCells(fog, playerId, grid);
    }

    /// <summary>
    /// Returns <c>true</c> when fog overlay contract holds: visible chunks produce no veil.
    /// </summary>
    public static bool VerifyOverlayContract(FogOfWar fog, GridSystem grid, int playerId,
        int sampleChunkX, int sampleChunkY)
    {
        fog.Reveal(playerId, sampleChunkX * FogNebulaOverlay.ChunkCells,
            sampleChunkY * FogNebulaOverlay.ChunkCells, radius: 2);

        return FogNebulaOverlay.ResolveOverlayState(fog, grid, playerId, sampleChunkX, sampleChunkY) is null;
    }

    /// <summary>
    /// Collect sight sources for player-owned units (mirrors <see cref="FogOfWarSystem"/> rules).
    /// </summary>
    public static IReadOnlyList<(int X, int Y, int SightRange)> CollectPlayerSightSources(
        World world, GridSystem grid)
    {
        var units = new List<(int X, int Y, int SightRange)>();

        foreach (var (entity, sight) in world.Query<SightRadiusComponent>())
        {
            if (world.HasComponent<AIControlledComponent>(entity))
                continue;

            var transform = world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            if (grid.WorldToGrid(transform.Position, out int gx, out int gy))
                units.Add((gx, gy, sight.Radius));
        }

        return units;
    }

    private static int CountVisibleCells(FogOfWar fog, int playerId, GridSystem grid)
    {
        int count = 0;
        foreach (GridCell cell in grid.AllCells())
        {
            if (fog.GetState(playerId, cell.X, cell.Y) == FogState.Visible)
                count++;
        }

        return count;
    }
}