using SharpOpenGl.Engine.UI;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class UIFontMetricsTests
{
    [Fact]
    public void FitFontSize_shrinks_until_text_fits()
    {
        float size = UIFontMetrics.FitFontSize("Attack-Move", 20f, 90f, 10f);
        Assert.True(size < 20f);
        Assert.True(UIFontMetrics.MeasureTextWidth("Attack-Move", size) <= 90f);
    }
}