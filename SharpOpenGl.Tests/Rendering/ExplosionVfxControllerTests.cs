using OpenTK.Mathematics;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class ExplosionVfxControllerTests
{
    [Fact]
    public void Bind_spawns_emitter_on_explosion_event()
    {
        var bus = new EventBus();
        var controller = new ExplosionVfxController();
        controller.Bind(bus);

        bus.Publish(new ExplosionVfxEvent(new Vector3(1f, 2f, 3f), ExplosionVfxKind.Impact));

        Assert.Equal(1, controller.ActiveEmitterCount);
        var (count, _) = controller.BuildVertexData();
        Assert.True(count > 0);
    }

    [Fact]
    public void Spawn_station_death_uses_larger_burst_than_impact()
    {
        var impact = new ExplosionVfxController();
        impact.Spawn(Vector3.Zero, ExplosionVfxKind.Impact);

        var station = new ExplosionVfxController();
        station.Spawn(Vector3.Zero, ExplosionVfxKind.StationDeath);

        var (impactCount, _) = impact.BuildVertexData();
        var (stationCount, _) = station.BuildVertexData();
        Assert.True(stationCount > impactCount);
    }

    [Fact]
    public void Update_removes_expired_emitters()
    {
        var controller = new ExplosionVfxController();
        controller.Spawn(Vector3.Zero, ExplosionVfxKind.Impact);

        controller.Update(2f);

        Assert.Equal(0, controller.ActiveEmitterCount);
        var (count, _) = controller.BuildVertexData();
        Assert.Equal(0, count);
    }

    [Fact]
    public void Disabled_controller_ignores_events()
    {
        var bus = new EventBus();
        var controller = new ExplosionVfxController { Enabled = false };
        controller.Bind(bus);

        bus.Publish(new ExplosionVfxEvent(Vector3.Zero, ExplosionVfxKind.ShipDeath));

        Assert.Equal(0, controller.ActiveEmitterCount);
    }
}