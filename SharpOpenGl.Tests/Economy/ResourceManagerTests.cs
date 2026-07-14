using SharpOpenGl.Engine.Economy;
using Xunit;

namespace SharpOpenGl.Tests.Economy;

public class ResourceManagerTests
{
    // ── PlayerResources ──────────────────────────────────────────────────────

    [Fact]
    public void SetStartingAmount_clamps_to_max()
    {
        var pr = new PlayerResources(energyMax: 100f);
        pr.SetStartingAmount(ResourceType.Energy, 9999f);
        Assert.Equal(100f, pr.GetAmount(ResourceType.Energy));
    }

    [Fact]
    public void TrySpend_succeeds_when_sufficient()
    {
        var pr = new PlayerResources();
        pr.SetStartingAmount(ResourceType.Minerals, 200f);
        bool ok = pr.TrySpend(ResourceType.Minerals, 150f);
        Assert.True(ok);
        Assert.Equal(50f, pr.GetAmount(ResourceType.Minerals));
    }

    [Fact]
    public void TrySpend_fails_when_insufficient()
    {
        var pr = new PlayerResources();
        pr.SetStartingAmount(ResourceType.Minerals, 10f);
        bool ok = pr.TrySpend(ResourceType.Minerals, 50f);
        Assert.False(ok);
        Assert.Equal(10f, pr.GetAmount(ResourceType.Minerals)); // unchanged
    }

    [Fact]
    public void Add_caps_at_max_storage()
    {
        var pr = new PlayerResources(energyMax: 100f);
        pr.SetStartingAmount(ResourceType.Energy, 90f);
        float added = pr.Add(ResourceType.Energy, 50f);
        Assert.Equal(10f, added);
        Assert.Equal(100f, pr.GetAmount(ResourceType.Energy));
    }

    [Fact]
    public void Tick_applies_income_and_caps_at_max()
    {
        var pr = new PlayerResources(energyMax: 100f);
        pr.SetStartingAmount(ResourceType.Energy, 95f);
        pr.AddIncome(ResourceType.Energy, 10f); // 10/sec
        pr.Tick(1f);
        Assert.Equal(100f, pr.GetAmount(ResourceType.Energy)); // capped
    }

    [Fact]
    public void Tick_does_not_go_below_zero_with_negative_income()
    {
        var pr = new PlayerResources();
        pr.SetStartingAmount(ResourceType.Crew, 5f);
        pr.AddIncome(ResourceType.Crew, -10f); // upkeep exceeds income
        pr.Tick(1f);
        Assert.Equal(0f, pr.GetAmount(ResourceType.Crew));
    }

    [Fact]
    public void GetDisplay_returns_correct_snapshot()
    {
        var pr = new PlayerResources(energyMax: 200f);
        pr.SetStartingAmount(ResourceType.Energy, 80f);
        pr.AddIncome(ResourceType.Energy, 5f);
        ResourceDisplay d = pr.GetDisplay(ResourceType.Energy);
        Assert.Equal(80f,  d.Current);
        Assert.Equal(200f, d.Max);
        Assert.Equal(5f,   d.IncomePerSecond);
        Assert.Equal(0.4f, d.Fraction, precision: 4);
        Assert.False(d.IsFull);
    }

    // ── ResourceManager ──────────────────────────────────────────────────────

    [Fact]
    public void ResourceManager_TrySpend_returns_false_for_unknown_player()
    {
        var rm = new ResourceManager();
        Assert.False(rm.TrySpend(99, ResourceType.Energy, 1f));
    }

    [Fact]
    public void ResourceManager_Add_returns_zero_for_unknown_player()
    {
        var rm = new ResourceManager();
        Assert.Equal(0f, rm.Add(99, ResourceType.Energy, 100f));
    }

    [Fact]
    public void ResourceManager_TrySpendCost_atomic_all_or_nothing()
    {
        var rm = new ResourceManager();
        var pr = rm.AddPlayer(1);
        pr.SetStartingAmount(ResourceType.Energy,   50f);
        pr.SetStartingAmount(ResourceType.Minerals, 30f);
        pr.SetStartingAmount(ResourceType.Data,     0f);
        pr.SetStartingAmount(ResourceType.Crew,     5f);

        // Data cost cannot be met.
        bool ok = rm.TrySpendCost(1, energy: 10, minerals: 10, data: 10, crew: 1);
        Assert.False(ok);

        // All amounts must be unchanged.
        Assert.Equal(50f, pr.GetAmount(ResourceType.Energy));
        Assert.Equal(30f, pr.GetAmount(ResourceType.Minerals));
    }

    [Fact]
    public void ResourceManager_TrySpendCost_deducts_all_types()
    {
        var rm = new ResourceManager();
        var pr = rm.AddPlayer(1);
        pr.SetStartingAmount(ResourceType.Energy,   100f);
        pr.SetStartingAmount(ResourceType.Minerals, 100f);
        pr.SetStartingAmount(ResourceType.Data,     100f);
        pr.SetStartingAmount(ResourceType.Crew,     10f);

        bool ok = rm.TrySpendCost(1, energy: 20, minerals: 30, data: 10, crew: 2);
        Assert.True(ok);
        Assert.Equal(80f, pr.GetAmount(ResourceType.Energy));
        Assert.Equal(70f, pr.GetAmount(ResourceType.Minerals));
        Assert.Equal(90f, pr.GetAmount(ResourceType.Data));
        Assert.Equal(8f,  pr.GetAmount(ResourceType.Crew));
    }

    [Fact]
    public void ResourceManager_Refund_adds_back_all_types()
    {
        var rm = new ResourceManager();
        var pr = rm.AddPlayer(1);
        rm.Refund(1, energy: 50, minerals: 20, data: 10, crew: 1);
        Assert.Equal(50f, pr.GetAmount(ResourceType.Energy));
        Assert.Equal(20f, pr.GetAmount(ResourceType.Minerals));
        Assert.Equal(10f, pr.GetAmount(ResourceType.Data));
        Assert.Equal(1f,  pr.GetAmount(ResourceType.Crew));
    }

    [Fact]
    public void ResourceManager_Tick_applies_income_for_all_players()
    {
        var rm = new ResourceManager();
        var p1 = rm.AddPlayer(1);
        var p2 = rm.AddPlayer(2);
        p1.AddIncome(ResourceType.Energy, 10f);
        p2.AddIncome(ResourceType.Energy, 5f);

        rm.Tick(2f);

        Assert.Equal(20f, p1.GetAmount(ResourceType.Energy));
        Assert.Equal(10f, p2.GetAmount(ResourceType.Energy));
    }

    [Fact]
    public void ResourceManager_GetDisplay_returns_zero_display_for_unknown_player()
    {
        var rm = new ResourceManager();
        ResourceDisplay d = rm.GetDisplay(99, ResourceType.Energy);
        Assert.Equal(0f, d.Current);
        Assert.Equal(0f, d.Max);
    }

    [Fact]
    public void ResourceManager_publishes_event_on_spend()
    {
        var bus = new SharpOpenGl.Engine.Events.EventBus();
        var rm  = new ResourceManager(bus);
        var pr  = rm.AddPlayer(1);
        pr.SetStartingAmount(ResourceType.Energy, 100f);

        SharpOpenGl.Engine.Events.ResourceChangedEvent? received = null;
        bus.Subscribe<SharpOpenGl.Engine.Events.ResourceChangedEvent>(e => received = e);

        rm.TrySpend(1, ResourceType.Energy, 30f);

        Assert.NotNull(received);
        Assert.Equal(1, received!.PlayerId);
        Assert.Equal(70f, received.NewAmount);
    }

    // ── Unlimited resources ──────────────────────────────────────────────────

    [Fact]
    public void UnlimitedResources_TrySpendCost_always_succeeds_without_deducting()
    {
        var rm = new ResourceManager { UnlimitedResources = true };
        var pr = rm.AddPlayer(1);
        pr.SetStartingAmount(ResourceType.Energy, 50f);
        pr.SetStartingAmount(ResourceType.Minerals, 50f);
        pr.SetStartingAmount(ResourceType.Data, 10f);
        pr.SetStartingAmount(ResourceType.Crew, 5f);

        for (int i = 0; i < 20; i++)
        {
            Assert.True(rm.TrySpendCost(1, energy: 40, minerals: 40, data: 5, crew: 2));
        }

        // Pools unchanged — can still afford further builds.
        Assert.Equal(50f, pr.GetAmount(ResourceType.Energy));
        Assert.Equal(50f, pr.GetAmount(ResourceType.Minerals));
        Assert.Equal(10f, pr.GetAmount(ResourceType.Data));
        Assert.Equal(5f, pr.GetAmount(ResourceType.Crew));
        Assert.True(rm.TrySpendCost(1, energy: 40, minerals: 40, data: 5, crew: 2));
    }

    [Fact]
    public void UnlimitedResources_disabled_still_deducts_normally()
    {
        var rm = new ResourceManager { UnlimitedResources = false };
        var pr = rm.AddPlayer(1);
        pr.SetStartingAmount(ResourceType.Energy, 100f);
        pr.SetStartingAmount(ResourceType.Minerals, 100f);
        pr.SetStartingAmount(ResourceType.Data, 100f);
        pr.SetStartingAmount(ResourceType.Crew, 10f);

        Assert.True(rm.TrySpendCost(1, energy: 20, minerals: 30, data: 10, crew: 2));
        Assert.Equal(80f, pr.GetAmount(ResourceType.Energy));
        Assert.Equal(70f, pr.GetAmount(ResourceType.Minerals));
        Assert.Equal(90f, pr.GetAmount(ResourceType.Data));
        Assert.Equal(8f, pr.GetAmount(ResourceType.Crew));
    }

    [Fact]
    public void UnlimitedResources_TrySpend_succeeds_when_pool_would_be_insufficient()
    {
        var rm = new ResourceManager { UnlimitedResources = true };
        var pr = rm.AddPlayer(1);
        pr.SetStartingAmount(ResourceType.Minerals, 5f);

        Assert.True(rm.TrySpend(1, ResourceType.Minerals, 999f));
        Assert.Equal(5f, pr.GetAmount(ResourceType.Minerals));
    }
}
