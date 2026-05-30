using SharpOpenGl.Engine.Events;
using Xunit;

namespace SharpOpenGl.Tests.Events;

public class EventBusTests
{
    [Fact]
    public void Subscribe_and_Publish_delivers_event()
    {
        var bus = new EventBus();
        int received = 0;

        bus.Subscribe<FrameUpdateEvent>(_ => received++);
        bus.Publish(new FrameUpdateEvent(0.016f));

        Assert.Equal(1, received);
    }

    [Fact]
    public void Multiple_subscribers_all_receive_event()
    {
        var bus = new EventBus();
        int count = 0;

        bus.Subscribe<FrameUpdateEvent>(_ => count++);
        bus.Subscribe<FrameUpdateEvent>(_ => count++);
        bus.Publish(new FrameUpdateEvent(0.016f));

        Assert.Equal(2, count);
    }

    [Fact]
    public void Unsubscribe_removes_handler()
    {
        var bus = new EventBus();
        int count = 0;
        Action<FrameUpdateEvent> handler = _ => count++;

        bus.Subscribe(handler);
        bus.Publish(new FrameUpdateEvent(0.016f));
        bus.Unsubscribe(handler);
        bus.Publish(new FrameUpdateEvent(0.016f));

        Assert.Equal(1, count);
    }

    [Fact]
    public void Publish_with_no_subscribers_does_not_throw()
    {
        var bus = new EventBus();
        var ex = Record.Exception(() => bus.Publish(new FrameUpdateEvent(0.016f)));
        Assert.Null(ex);
    }

    [Fact]
    public void Faulting_handler_does_not_prevent_other_handlers()
    {
        var bus = new EventBus();
        int count = 0;

        bus.Subscribe<FrameUpdateEvent>(_ => throw new InvalidOperationException("test"));
        bus.Subscribe<FrameUpdateEvent>(_ => count++);

        bus.Publish(new FrameUpdateEvent(0.016f));

        Assert.Equal(1, count);
    }

    [Fact]
    public void Clear_removes_all_subscribers()
    {
        var bus = new EventBus();
        int count = 0;

        bus.Subscribe<FrameUpdateEvent>(_ => count++);
        bus.Clear();
        bus.Publish(new FrameUpdateEvent(0.016f));

        Assert.Equal(0, count);
    }

    [Fact]
    public void Different_event_types_do_not_cross_fire()
    {
        var bus = new EventBus();
        bool frameReceived = false;
        bool resizeReceived = false;

        bus.Subscribe<FrameUpdateEvent>(_ => frameReceived = true);
        bus.Subscribe<WindowResizedEvent>(_ => resizeReceived = true);

        bus.Publish(new FrameUpdateEvent(0.016f));

        Assert.True(frameReceived);
        Assert.False(resizeReceived);
    }
}
