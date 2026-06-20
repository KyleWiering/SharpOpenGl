using SharpOpenGl.Engine.Input;
using Xunit;

namespace SharpOpenGl.Tests.Input;

public class CameraInputMappingTests
{
    [Fact]
    public void A_and_D_strafe_when_no_units_selected()
    {
        var left = CameraInputMapping.Resolve(false, false, true, false, false, false, false, false, false, false);
        var right = CameraInputMapping.Resolve(false, false, false, true, false, false, false, false, false, false);

        Assert.Equal(-1f, left.Strafe);
        Assert.Equal(1f, right.Strafe);
    }

    [Fact]
    public void S_does_not_pan_camera_when_units_selected_without_shift()
    {
        var axes = CameraInputMapping.Resolve(false, true, false, false, false, false, false, false, true, false);
        Assert.Equal(0f, axes.Forward);
    }

    [Fact]
    public void Shift_allows_camera_while_units_selected()
    {
        var axes = CameraInputMapping.Resolve(false, true, true, false, false, false, false, false, true, true);
        Assert.Equal(1f, axes.Forward);
        Assert.Equal(-1f, axes.Strafe);
    }
}