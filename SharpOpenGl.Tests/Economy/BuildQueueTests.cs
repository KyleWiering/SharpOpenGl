using SharpOpenGl.Engine.Economy;
using Xunit;

namespace SharpOpenGl.Tests.Economy;

public class BuildQueueTests
{
    private static ResourceManager MakeManager(int playerId, int energy = 1000, int minerals = 1000)
    {
        var rm = new ResourceManager();
        var pr = rm.AddPlayer(playerId);
        pr.SetStartingAmount(ResourceType.Energy,   energy);
        pr.SetStartingAmount(ResourceType.Minerals, minerals);
        return rm;
    }

    [Fact]
    public void Enqueue_deducts_resources_on_success()
    {
        var rm = MakeManager(1, energy: 200, minerals: 100);
        var bq = new BuildQueue();

        bool ok = bq.Enqueue("fighter_basic", 50, 30, 0, 0, 10f, rm, 1);

        Assert.True(ok);
        Assert.Equal(1, bq.Count);
        Assert.Equal(150f, rm.GetPlayer(1)!.GetAmount(ResourceType.Energy));
        Assert.Equal(70f,  rm.GetPlayer(1)!.GetAmount(ResourceType.Minerals));
    }

    [Fact]
    public void Enqueue_returns_false_when_insufficient_resources()
    {
        var rm = MakeManager(1, energy: 10);
        var bq = new BuildQueue();

        bool ok = bq.Enqueue("fighter_basic", 100, 0, 0, 0, 5f, rm, 1);

        Assert.False(ok);
        Assert.Equal(0, bq.Count);
        Assert.Equal(10f, rm.GetPlayer(1)!.GetAmount(ResourceType.Energy)); // unchanged
    }

    [Fact]
    public void Advance_returns_null_while_in_progress()
    {
        var rm = MakeManager(1);
        var bq = new BuildQueue();
        bq.Enqueue("unit_a", 0, 0, 0, 0, 10f, rm, 1);

        BuildQueueItem? done = bq.Advance(5f);
        Assert.Null(done);
        Assert.Equal(1, bq.Count);
    }

    [Fact]
    public void Advance_returns_item_when_complete()
    {
        var rm = MakeManager(1);
        var bq = new BuildQueue();
        bq.Enqueue("unit_a", 0, 0, 0, 0, 10f, rm, 1);

        BuildQueueItem? done = bq.Advance(10f);
        Assert.NotNull(done);
        Assert.Equal("unit_a", done!.DefinitionId);
        Assert.Equal(0, bq.Count);
    }

    [Fact]
    public void Advance_uses_production_rate_multiplier()
    {
        var rm = MakeManager(1);
        var bq = new BuildQueue();
        bq.Enqueue("unit_b", 0, 0, 0, 0, 10f, rm, 1);

        // 2× rate means 5 seconds of elapsed time completes a 10-second build.
        BuildQueueItem? done = bq.Advance(5f, productionRate: 2f);
        Assert.NotNull(done);
    }

    [Fact]
    public void CancelLast_refunds_and_removes_tail()
    {
        var rm = MakeManager(1, energy: 1000);
        var bq = new BuildQueue();
        bq.Enqueue("unit_a", 100, 0, 0, 0, 5f, rm, 1);
        bq.Enqueue("unit_b", 200, 0, 0, 0, 5f, rm, 1);

        // After enqueue: 1000 - 100 - 200 = 700
        Assert.Equal(700f, rm.GetPlayer(1)!.GetAmount(ResourceType.Energy));

        bool ok = bq.CancelLast(rm, 1);

        Assert.True(ok);
        Assert.Equal(1, bq.Count);
        Assert.Equal("unit_a", bq.Current!.DefinitionId);
        Assert.Equal(900f, rm.GetPlayer(1)!.GetAmount(ResourceType.Energy)); // 700 + 200 refund
    }

    [Fact]
    public void CancelCurrent_refunds_front_item()
    {
        var rm = MakeManager(1, energy: 500);
        var bq = new BuildQueue();
        bq.Enqueue("unit_a", 100, 0, 0, 0, 5f, rm, 1);
        bq.Enqueue("unit_b", 50,  0, 0, 0, 5f, rm, 1);

        bool ok = bq.CancelCurrent(rm, 1);

        Assert.True(ok);
        Assert.Equal(1, bq.Count);
        Assert.Equal("unit_b", bq.Current!.DefinitionId);
        Assert.Equal(450f, rm.GetPlayer(1)!.GetAmount(ResourceType.Energy)); // 350 + 100 refund
    }

    [Fact]
    public void CancelAll_refunds_everything()
    {
        var rm = MakeManager(1, energy: 1000, minerals: 1000);
        var bq = new BuildQueue();
        bq.Enqueue("unit_a", 200, 100, 0, 0, 5f, rm, 1);
        bq.Enqueue("unit_b", 300, 200, 0, 0, 5f, rm, 1);

        bq.CancelAll(rm, 1);

        Assert.Equal(0, bq.Count);
        Assert.Equal(1000f, rm.GetPlayer(1)!.GetAmount(ResourceType.Energy));
        Assert.Equal(1000f, rm.GetPlayer(1)!.GetAmount(ResourceType.Minerals));
    }

    [Fact]
    public void CancelLast_returns_false_on_empty_queue()
    {
        var rm = MakeManager(1);
        var bq = new BuildQueue();
        Assert.False(bq.CancelLast(rm, 1));
    }

    [Fact]
    public void CancelCurrent_returns_false_on_empty_queue()
    {
        var rm = MakeManager(1);
        var bq = new BuildQueue();
        Assert.False(bq.CancelCurrent(rm, 1));
    }

    [Fact]
    public void Queue_processes_multiple_items_in_order()
    {
        var rm = MakeManager(1);
        var bq = new BuildQueue();
        bq.Enqueue("first",  0, 0, 0, 0, 5f, rm, 1);
        bq.Enqueue("second", 0, 0, 0, 0, 5f, rm, 1);

        var done1 = bq.Advance(5f);
        Assert.Equal("first", done1!.DefinitionId);

        var done2 = bq.Advance(5f);
        Assert.Equal("second", done2!.DefinitionId);

        Assert.Equal(0, bq.Count);
    }
}
