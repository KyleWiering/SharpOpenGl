using SharpOpenGl.Engine.ECS;
using Xunit;

namespace SharpOpenGl.Tests.ECS;

public class ComponentTests
{
    private sealed class VelocityComponent
    {
        public float X { get; set; }
        public float Y { get; set; }
    }

    [Fact]
    public void AddComponent_and_GetComponent_round_trips()
    {
        var world = new World();
        Entity e = world.CreateEntity();
        var vel = new VelocityComponent { X = 3f, Y = 7f };
        world.AddComponent(e, vel);

        var result = world.GetComponent<VelocityComponent>(e);
        Assert.Same(vel, result);
    }

    [Fact]
    public void HasComponent_returns_false_before_add()
    {
        var world = new World();
        Entity e = world.CreateEntity();
        Assert.False(world.HasComponent<VelocityComponent>(e));
    }

    [Fact]
    public void HasComponent_returns_true_after_add()
    {
        var world = new World();
        Entity e = world.CreateEntity();
        world.AddComponent(e, new VelocityComponent());
        Assert.True(world.HasComponent<VelocityComponent>(e));
    }

    [Fact]
    public void RemoveComponent_removes_it()
    {
        var world = new World();
        Entity e = world.CreateEntity();
        world.AddComponent(e, new VelocityComponent());
        world.RemoveComponent<VelocityComponent>(e);
        Assert.False(world.HasComponent<VelocityComponent>(e));
        Assert.Null(world.GetComponent<VelocityComponent>(e));
    }

    [Fact]
    public void DestroyEntity_removes_all_components()
    {
        var world = new World();
        Entity e = world.CreateEntity();
        world.AddComponent(e, new VelocityComponent());
        world.DestroyEntity(e);
        // After destroy the entity is stale; GetComponent should return null.
        Assert.Null(world.GetComponent<VelocityComponent>(e));
    }

    [Fact]
    public void AddComponent_throws_for_dead_entity()
    {
        var world = new World();
        Entity e = world.CreateEntity();
        world.DestroyEntity(e);
        Assert.Throws<ArgumentException>(() =>
            world.AddComponent(e, new VelocityComponent()));
    }

    [Fact]
    public void Query_returns_only_entities_with_component()
    {
        var world = new World();
        Entity a = world.CreateEntity();
        Entity b = world.CreateEntity();
        Entity c = world.CreateEntity();

        world.AddComponent(a, new VelocityComponent { X = 1 });
        world.AddComponent(c, new VelocityComponent { X = 2 });
        // b has no component

        var results = world.Query<VelocityComponent>().ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Entity == a);
        Assert.Contains(results, r => r.Entity == c);
        Assert.DoesNotContain(results, r => r.Entity == b);
    }

    [Fact]
    public void Query_excludes_destroyed_entities()
    {
        var world = new World();
        Entity e = world.CreateEntity();
        world.AddComponent(e, new VelocityComponent());
        world.DestroyEntity(e);

        var results = world.Query<VelocityComponent>().ToList();
        Assert.Empty(results);
    }
}
