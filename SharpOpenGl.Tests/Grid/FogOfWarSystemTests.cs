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

        // First update reveals cells around (4,4)
        system.Update(world, 0.016f);
        Assert.Equal(FogState.Visible, fog.GetState(0, 4, 4));

        // Move ship away
        world.GetComponent<TransformComponent>(ship)!.Position = grid.GridToWorld(12, 12);

        // Second update should downgrade old cells
        system.Update(world, 0.016f);
        Assert.Equal(FogState.Explored, fog.GetState(0, 4, 4));
        Assert.Equal(FogState.Visible, fog.GetState(0, 12, 12));

        world.Dispose();
    }

    [Fact]
    public void SightRadiusComponent_defaults_to_5()
    {
        var comp = new SightRadiusComponent();
        Assert.Equal(5, comp.Radius);
    }
}
