using OpenTK.Mathematics;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Grid;
using Xunit;

namespace SharpOpenGl.Tests.Grid;

public class FogOfWarSystemTests
{
    [Fact]
    public void FogOfWarSystem_reveals_cells_around_entity()
    {
        var grid = new GridSystem(16, 16);
        var fog = new FogOfWar(grid, playerCount: 1);
        var world = new World();
        var system = new FogOfWarSystem(grid, fog, playerId: 0);

        Entity ship = world.CreateEntity();
        world.AddComponent(ship, new TransformComponent
        {
            Position = grid.GridToWorld(8, 8)
        });
        world.AddComponent(ship, new SightRadiusComponent { Radius = 3 });

        system.Update(world, 0.016f);

        Assert.Equal(FogState.Visible, fog.GetState(0, 8, 8));
        Assert.Equal(FogState.Visible, fog.GetState(0, 9, 8));
        Assert.Equal(FogState.Unexplored, fog.GetState(0, 0, 0));

        world.Dispose();
    }

    [Fact]
    public void FogOfWarSystem_previously_visible_becomes_explored()
    {
        var grid = new GridSystem(16, 16);
        var fog = new FogOfWar(grid, playerCount: 1);
        var world = new World();
        var system = new FogOfWarSystem(grid, fog, playerId: 0);

        Entity ship = world.CreateEntity();
        world.AddComponent(ship, new TransformComponent
        {
            Position = grid.GridToWorld(4, 4)
        });
        world.AddComponent(ship, new SightRadiusComponent { Radius = 2 });

        system.Update(world, 0.016f);
        Assert.Equal(FogState.Visible, fog.GetState(0, 4, 4));

        world.GetComponent<TransformComponent>(ship)!.Position = grid.GridToWorld(12, 12);

        system.Update(world, 0.016f);
        Assert.Equal(FogState.Explored, fog.GetState(0, 4, 4));
        Assert.Equal(FogState.Visible, fog.GetState(0, 12, 12));

        world.Dispose();
    }

    [Fact]
    public void FogOfWarSystem_ignores_enemy_sight()
    {
        var grid = new GridSystem(16, 16);
        var fog = new FogOfWar(grid, playerCount: 1);
        var world = new World();
        var system = new FogOfWarSystem(grid, fog, playerId: 0);

        Entity enemy = world.CreateEntity();
        world.AddComponent(enemy, new TransformComponent { Position = grid.GridToWorld(12, 12) });
        world.AddComponent(enemy, new SightRadiusComponent { Radius = 8 });
        world.AddComponent(enemy, new AIControlledComponent { PlayerId = 2 });

        system.Update(world, 0.016f);

        Assert.Equal(FogState.Unexplored, fog.GetState(0, 12, 12));

        world.Dispose();
    }

    [Fact]
    public void FogOfWar_PlayerCount_exposes_configured_value()
    {
        var grid = new GridSystem(16, 16);
        var fog = new FogOfWar(grid, playerCount: 3);
        Assert.Equal(3, fog.PlayerCount);

        var fogDefault = new FogOfWar(grid, playerCount: 2);
        Assert.Equal(2, fogDefault.PlayerCount);
    }

    [Fact]
    public void SightRadiusComponent_defaults_to_5()
    {
        var comp = new SightRadiusComponent();
        Assert.Equal(5, comp.Radius);
    }
}