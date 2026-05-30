using SharpOpenGl.Engine.ECS;
using Xunit;

namespace SharpOpenGl.Tests.ECS;

public class EntityTests
{
    [Fact]
    public void World_CreateEntity_returns_live_entity()
    {
        var world = new World();
        Entity e = world.CreateEntity();
        Assert.True(world.IsAlive(e));
    }

    [Fact]
    public void World_DestroyEntity_makes_entity_dead()
    {
        var world = new World();
        Entity e = world.CreateEntity();
        world.DestroyEntity(e);
        Assert.False(world.IsAlive(e));
    }

    [Fact]
    public void World_LiveCount_tracks_create_and_destroy()
    {
        var world = new World();
        Assert.Equal(0, world.LiveCount);

        Entity e1 = world.CreateEntity();
        Entity e2 = world.CreateEntity();
        Assert.Equal(2, world.LiveCount);

        world.DestroyEntity(e1);
        Assert.Equal(1, world.LiveCount);
    }

    [Fact]
    public void Recycled_slot_has_different_generation()
    {
        var world = new World();
        Entity first = world.CreateEntity();
        world.DestroyEntity(first);

        Entity second = world.CreateEntity();
        Assert.Equal(first.Index, second.Index);
        Assert.NotEqual(first.Generation, second.Generation);
        Assert.False(world.IsAlive(first));
        Assert.True(world.IsAlive(second));
    }

    [Fact]
    public void Entity_Null_is_not_alive()
    {
        var world = new World();
        Assert.False(world.IsAlive(Entity.Null));
    }

    [Fact]
    public void Can_create_1000_entities()
    {
        var world = new World();
        var entities = new List<Entity>();
        for (int i = 0; i < 1000; i++)
            entities.Add(world.CreateEntity());

        Assert.Equal(1000, world.LiveCount);
        foreach (Entity e in entities)
            Assert.True(world.IsAlive(e));
    }
}
