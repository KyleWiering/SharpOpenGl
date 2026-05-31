using SharpOpenGl.Engine.Multiplayer;
using Xunit;

namespace SharpOpenGl.Tests.Multiplayer;

public class DeterministicClockTests
{
    [Fact]
    public void Clock_starts_at_tick_zero()
    {
        var clock = new DeterministicClock(20);
        Assert.Equal(0, clock.CurrentTick);
        Assert.False(clock.HasPendingTick);
    }

    [Fact]
    public void Clock_produces_tick_after_one_period()
    {
        var clock = new DeterministicClock(20); // 0.05 s per tick
        clock.Advance(0.05);
        Assert.True(clock.HasPendingTick);
        long tick = clock.ConsumeTick();
        Assert.Equal(1, tick);
        Assert.False(clock.HasPendingTick);
    }

    [Fact]
    public void Clock_accumulates_multiple_ticks()
    {
        var clock = new DeterministicClock(20);
        clock.Advance(0.15); // 3 ticks worth

        int count = 0;
        while (clock.HasPendingTick)
        {
            clock.ConsumeTick();
            count++;
        }
        Assert.Equal(3, count);
        Assert.Equal(3, clock.CurrentTick);
    }

    [Fact]
    public void Clock_ConsumeTick_throws_when_no_pending_tick()
    {
        var clock = new DeterministicClock(20);
        Assert.Throws<InvalidOperationException>(() => clock.ConsumeTick());
    }

    [Fact]
    public void Clock_Advance_throws_for_negative_delta()
    {
        var clock = new DeterministicClock(20);
        Assert.Throws<ArgumentOutOfRangeException>(() => clock.Advance(-0.1));
    }

    [Fact]
    public void Clock_Reset_returns_to_initial_state()
    {
        var clock = new DeterministicClock(20);
        clock.Advance(1.0);
        while (clock.HasPendingTick) clock.ConsumeTick();
        clock.Reset();

        Assert.Equal(0, clock.CurrentTick);
        Assert.False(clock.HasPendingTick);
    }

    [Fact]
    public void Clock_RenderAlpha_is_between_zero_and_one()
    {
        var clock = new DeterministicClock(20);
        clock.Advance(0.025); // half a tick
        Assert.InRange(clock.RenderAlpha, 0.0, 1.0);
    }

    [Fact]
    public void Clock_constructor_throws_for_zero_ticks_per_second()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DeterministicClock(0));
    }

    [Fact]
    public void Clock_tick_duration_matches_rate()
    {
        var clock = new DeterministicClock(10);
        Assert.Equal(0.1, clock.TickDuration, precision: 10);
    }
}
