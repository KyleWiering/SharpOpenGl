using SharpOpenGl.Engine.Config;
using Xunit;

namespace SharpOpenGl.Tests.Config;

public class VisualBalanceTests
{
    [Fact]
    public void Apply_reads_ship_scale_from_config()
    {
        VisualBalance.ResetForTests();
        VisualBalance.Apply(new VisualConfig { GlobalShipScaleMultiplier = 1.5f });
        Assert.Equal(1.5f, VisualBalance.ShipScaleMultiplier);
    }
}