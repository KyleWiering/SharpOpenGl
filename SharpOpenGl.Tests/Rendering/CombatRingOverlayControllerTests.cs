using OpenTK.Mathematics;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.Rendering;
using Xunit;

namespace SharpOpenGl.Tests.Rendering;

public class CombatRingOverlayControllerTests
{
    [Fact]
    public void Bind_spawns_ring_on_combat_ring_event()
    {
        var bus = new EventBus();
        var controller = new CombatRingOverlayController();
        controller.Bind(bus);

        bus.Publish(new CombatRingVfxEvent(
            new Vector3(1f, 0f, 2f),
            CombatRingVfxKind.ShieldBreak,
            8f,
            new Vector4(0.3f, 0.6f, 1f, 1f)));

        Assert.Equal(1, controller.ActiveRingCount);
    }

    [Fact]
    public void Shield_break_ring_expands_and_fades_over_lifetime()
    {
        var controller = new CombatRingOverlayController();
        controller.Spawn(Vector3.Zero, CombatRingVfxKind.ShieldBreak, 7f);

        var start = controller.BuildDrawStates()[0];
        controller.Update(0.14f);
        var mid = controller.BuildDrawStates()[0];

        Assert.True(mid.Scale > start.Scale);
        Assert.True(mid.Alpha < start.Alpha);
    }

    [Fact]
    public void Death_expand_ring_uses_longer_duration_than_shield_break()
    {
        var controller = new CombatRingOverlayController();
        controller.Spawn(Vector3.Zero, CombatRingVfxKind.ShieldBreak, 7f);
        controller.Spawn(Vector3.Zero, CombatRingVfxKind.DeathExpand, 9f);

        controller.Update(0.3f);

        Assert.Equal(1, controller.ActiveRingCount);
    }

    [Fact]
    public void Disabled_controller_ignores_events()
    {
        var bus = new EventBus();
        var controller = new CombatRingOverlayController { Enabled = false };
        controller.Bind(bus);

        bus.Publish(new CombatRingVfxEvent(Vector3.Zero, CombatRingVfxKind.DeathExpand));

        Assert.Equal(0, controller.ActiveRingCount);
    }

    [Fact]
    public void Ultimate_cast_ring_spawns_with_race_tint()
    {
        var controller = new CombatRingOverlayController();
        var tint = new Vector4(0.9f, 0.7f, 0.2f, 1f);
        controller.Spawn(Vector3.Zero, CombatRingVfxKind.UltimateCast, 8f, tint);

        var state = controller.BuildDrawStates()[0];
        Assert.Equal(tint.X, state.Color.X, 3);
        Assert.Equal(tint.Y, state.Color.Y, 3);
    }
}