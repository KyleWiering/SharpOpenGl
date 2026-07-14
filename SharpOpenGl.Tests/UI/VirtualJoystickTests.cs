using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class VirtualJoystickTests
{
    [Fact]
    public void NormalizeAxis_returns_zero_inside_deadzone()
    {
        var joystick = new VirtualJoystick
        {
            BaseRadius = 60f,
            DeadzoneFraction = 0.12f,
        };

        Vector2 axis = joystick.NormalizeAxis(new Vector2(4f, 0f));

        Assert.Equal(Vector2.Zero, axis);
    }

    [Fact]
    public void NormalizeAxis_applies_response_curve_at_full_deflection()
    {
        var joystick = new VirtualJoystick
        {
            BaseRadius = 60f,
            DeadzoneFraction = 0.1f,
            ResponseExponent = 0.72f,
        };

        Vector2 axis = joystick.NormalizeAxis(new Vector2(0f, 60f));

        Assert.InRange(axis.Y, 0.95f, 1.01f);
        Assert.Equal(0f, axis.X, precision: 3);
    }

    [Fact]
    public void NormalizeAxis_is_snappier_than_linear_mid_deflection()
    {
        var joystick = new VirtualJoystick
        {
            BaseRadius = 100f,
            DeadzoneFraction = 0.1f,
            ResponseExponent = 0.72f,
        };

        Vector2 curved = joystick.NormalizeAxis(new Vector2(50f, 0f));
        float linear = (0.5f - 0.1f) / 0.9f;

        Assert.True(curved.X > linear);
    }
}