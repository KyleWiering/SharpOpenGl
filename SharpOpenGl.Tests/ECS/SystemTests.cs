using SharpOpenGl.Engine.ECS;
using Xunit;

namespace SharpOpenGl.Tests.ECS;

public class SystemTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class CounterComponent
    {
        public int Value { get; set; }
    }

    private sealed class IncrementSystem : GameSystem
    {
        public int InitCallCount { get; private set; }
        public int UpdateCallCount { get; private set; }
        public float LastDelta { get; private set; }

        public override void Initialize() => InitCallCount++;

        public override void Update(World world, float deltaTime)
        {
            UpdateCallCount++;
            LastDelta = deltaTime;
            foreach (var (_, counter) in world.Query<CounterComponent>())
                counter.Value++;
        }
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public void System_Initialize_called_once_before_first_update()
    {
        var world = new World();
        var sys = new IncrementSystem();
        world.AddSystem(sys);

        world.Update(0.016f);
        world.Update(0.016f);

        Assert.Equal(1, sys.InitCallCount);
    }

    [Fact]
    public void System_Update_called_every_frame()
    {
        var world = new World();
        var sys = new IncrementSystem();
        world.AddSystem(sys);

        world.Update(0.016f);
        world.Update(0.016f);
        world.Update(0.016f);

        Assert.Equal(3, sys.UpdateCallCount);
    }

    [Fact]
    public void System_receives_correct_deltaTime()
    {
        var world = new World();
        var sys = new IncrementSystem();
        world.AddSystem(sys);

        world.Update(0.033f);

        Assert.Equal(0.033f, sys.LastDelta, precision: 5);
    }

    [Fact]
    public void System_increments_counter_components()
    {
        var world = new World();
        var sys = new IncrementSystem();
        world.AddSystem(sys);

        Entity e1 = world.CreateEntity();
        Entity e2 = world.CreateEntity();
        world.AddComponent(e1, new CounterComponent());
        world.AddComponent(e2, new CounterComponent());

        world.Update(0.016f);
        world.Update(0.016f);

        Assert.Equal(2, world.GetComponent<CounterComponent>(e1)!.Value);
        Assert.Equal(2, world.GetComponent<CounterComponent>(e2)!.Value);
    }

    [Fact]
    public void RemoveSystem_stops_system_from_running()
    {
        var world = new World();
        var sys = new IncrementSystem();
        world.AddSystem(sys);

        world.Update(0.016f);
        world.RemoveSystem(sys);
        world.Update(0.016f);

        Assert.Equal(1, sys.UpdateCallCount);
    }

    [Fact]
    public void World_Dispose_disposes_all_systems()
    {
        var world = new World();
        var sys = new IncrementSystem();
        world.AddSystem(sys);

        // Dispose should not throw.
        world.Dispose();
    }
}
