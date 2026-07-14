using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Multiplayer;
using Xunit;

namespace SharpOpenGl.Tests.Multiplayer;

public class FogCommandSyncTests
{
    private const int GridSize = 32;
    private const float CellSize = 10f;

    [Fact]
    public void Move_command_reveals_destination_area_for_commanding_player()
    {
        var grid = new GridSystem(GridSize, GridSize, CellSize);
        var fog = new FogOfWar(grid, playerCount: 1);
        var world = new World();
        var executor = new GameCommandExecutor();

        const int startX = 5;
        const int startY = 5;
        const int destX = 10;
        const int destY = 10;

        Entity scout = CreateScout(world, grid, startX, startY, sightRadius: 8);

        var target = grid.GridToWorld(destX, destY);
        var cmd = new MoveCommand
        {
            PlayerId = 1,
            Tick = 1,
            EntityIds = [scout.Index],
            TargetX = target.X,
            TargetZ = target.Z,
        };

        var context = new GameCommandContext
        {
            World = world,
            PlayerId = 1,
            Grid = grid,
            Fog = fog,
        };

        Assert.True(executor.Execute(context, cmd));
        Assert.Equal(FogState.Visible, fog.GetState(0, destX, destY));

        world.Dispose();
    }

    [Fact]
    public void Fog_refresh_is_deterministic_for_same_commands()
    {
        const int destX = 12;
        const int destY = 14;

        var gridA = new GridSystem(GridSize, GridSize, CellSize);
        var fogA = new FogOfWar(gridA, playerCount: 1);
        var worldA = new World();
        Entity scoutA = CreateScout(worldA, gridA, 4, 6, sightRadius: 6);

        var gridB = new GridSystem(GridSize, GridSize, CellSize);
        var fogB = new FogOfWar(gridB, playerCount: 1);
        var worldB = new World();
        Entity scoutB = CreateScout(worldB, gridB, 4, 6, sightRadius: 6);

        var executor = new GameCommandExecutor();
        var target = gridA.GridToWorld(destX, destY);
        var secondTarget = gridA.GridToWorld(8, 8);

        var contextA = new GameCommandContext { World = worldA, PlayerId = 1, Grid = gridA, Fog = fogA };
        var contextB = new GameCommandContext { World = worldB, PlayerId = 1, Grid = gridB, Fog = fogB };

        Assert.True(executor.Execute(contextA, new MoveCommand
        {
            PlayerId = 1,
            Tick = 1,
            EntityIds = [scoutA.Index],
            TargetX = target.X,
            TargetZ = target.Z,
        }));
        Assert.True(executor.Execute(contextB, new MoveCommand
        {
            PlayerId = 1,
            Tick = 1,
            EntityIds = [scoutB.Index],
            TargetX = target.X,
            TargetZ = target.Z,
        }));

        Assert.True(executor.Execute(contextA, new MoveCommand
        {
            PlayerId = 1,
            Tick = 2,
            EntityIds = [scoutA.Index],
            TargetX = secondTarget.X,
            TargetZ = secondTarget.Z,
        }));
        Assert.True(executor.Execute(contextB, new MoveCommand
        {
            PlayerId = 1,
            Tick = 2,
            EntityIds = [scoutB.Index],
            TargetX = secondTarget.X,
            TargetZ = secondTarget.Z,
        }));

        Assert.Equal(CaptureFogSnapshot(gridA, 0), CaptureFogSnapshot(gridB, 0));

        worldA.Dispose();
        worldB.Dispose();
    }

    [Fact]
    public void Executor_skips_fog_when_context_grid_null()
    {
        var grid = new GridSystem(GridSize, GridSize, CellSize);
        var fog = new FogOfWar(grid, playerCount: 1);
        var world = new World();
        var executor = new GameCommandExecutor();

        Entity scout = CreateScout(world, grid, 8, 8, sightRadius: 5);
        var before = CaptureFogSnapshot(grid, 0);

        var cmd = new MoveCommand
        {
            PlayerId = 1,
            Tick = 1,
            EntityIds = [scout.Index],
            TargetX = grid.GridToWorld(15, 15).X,
            TargetZ = grid.GridToWorld(15, 15).Z,
        };

        var context = new GameCommandContext
        {
            World = world,
            PlayerId = 1,
            Fog = fog,
        };

        var ex = Record.Exception(() => Assert.True(executor.Execute(context, cmd)));
        Assert.Null(ex);
        Assert.Equal(before, CaptureFogSnapshot(grid, 0));

        world.Dispose();
    }

    private static Entity CreateScout(World world, GridSystem grid, int gridX, int gridY, int sightRadius)
    {
        Entity scout = world.CreateEntity();
        world.AddComponent(scout, new TransformComponent { Position = grid.GridToWorld(gridX, gridY) });
        world.AddComponent(scout, new SelectionComponent { IsSelected = true, SelectionRadius = 5f });
        world.AddComponent(scout, new MovementComponent { Speed = 50f });
        world.AddComponent(scout, new SightRadiusComponent { Radius = sightRadius });
        world.AddComponent(scout, new CombatTargetComponent { Faction = 1 });
        return scout;
    }

    private static Dictionary<string, int> CaptureFogSnapshot(GridSystem grid, int fogPlayerId)
    {
        var snapshot = new Dictionary<string, int>();
        foreach (GridCell cell in grid.AllCells())
        {
            FogState state = cell.GetFog(fogPlayerId);
            if (state == FogState.Unexplored)
                continue;

            snapshot[$"{fogPlayerId}:{cell.X}:{cell.Y}"] = (int)state;
        }

        return snapshot;
    }
}