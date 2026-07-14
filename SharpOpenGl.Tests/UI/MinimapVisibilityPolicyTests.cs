using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.UI;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class MinimapVisibilityPolicyTests
{
    [Theory]
    [InlineData(FogState.Unexplored, false)]
    [InlineData(FogState.Explored, true)]
    [InlineData(FogState.Visible, true)]
    public void Feature_visibility_requires_discovery(FogState state, bool expected)
    {
        Assert.Equal(expected, MinimapVisibilityPolicy.ShouldShowFeature(state));
    }

    [Theory]
    [InlineData(FogState.Unexplored, true, false)]
    [InlineData(FogState.Explored, true, true)]
    [InlineData(FogState.Visible, true, true)]
    [InlineData(FogState.Unexplored, false, false)]
    [InlineData(FogState.Explored, false, false)]
    [InlineData(FogState.Visible, false, true)]
    public void Unit_visibility_depends_on_faction(FogState state, bool friendly, bool expected)
    {
        Assert.Equal(expected, MinimapVisibilityPolicy.ShouldShowUnit(state, friendly));
    }
}