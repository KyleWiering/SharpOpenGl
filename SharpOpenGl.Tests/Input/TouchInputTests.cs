using OpenTK.Mathematics;
using SharpOpenGl.Engine.Input;
using SharpOpenGl.Engine.UI;
using Xunit;

namespace SharpOpenGl.Tests.Input;

// ── Helpers ───────────────────────────────────────────────────────────────────

internal static class TouchFactory
{
    /// <summary>Create a fresh touch point that just became active this frame.</summary>
    public static TouchPoint Down(int id, Vector2 position, float duration = 0f) => new()
    {
        Id              = id,
        Position        = position,
        StartPosition   = position,
        ContactDuration = duration,
        WasActive       = false,
        IsActive        = true,
    };

    /// <summary>Move a touch to a new position (was and is active).</summary>
    public static TouchPoint Move(int id, Vector2 startPos, Vector2 currentPos, float duration) => new()
    {
        Id              = id,
        Position        = currentPos,
        StartPosition   = startPos,
        ContactDuration = duration,
        WasActive       = true,
        IsActive        = true,
    };

    /// <summary>Create a touch point that was just released.</summary>
    public static TouchPoint Up(int id, Vector2 position, float duration) => new()
    {
        Id              = id,
        Position        = position,
        StartPosition   = position,
        ContactDuration = duration,
        WasActive       = true,
        IsActive        = false,
    };
}

// ── GestureRecognizer tests ───────────────────────────────────────────────────

public class GestureRecognizerTests
{
    private static GestureRecognizer Make() => new()
    {
        TapMaxDuration        = 0.3f,
        TapMaxMovePx          = 10f,
        LongPressDuration     = 0.5f,
        DoubleTapWindowSeconds = 0.4f,
    };

    [Fact]
    public void Single_tap_emitted_on_release()
    {
        var gr = Make();
        var touches = new[] { TouchFactory.Up(0, new Vector2(100, 100), 0.1f) };

        var events = gr.Update(touches, 0.1f);

        Assert.Single(events);
        Assert.Equal(GestureType.Tap, events[0].Type);
        Assert.Equal(new Vector2(100, 100), events[0].Position);
    }

    [Fact]
    public void Tap_not_emitted_when_duration_too_long()
    {
        var gr = Make();
        var touches = new[] { TouchFactory.Up(0, new Vector2(100, 100), 0.5f) };

        var events = gr.Update(touches, 0.5f);

        Assert.DoesNotContain(events, e => e.Type == GestureType.Tap);
    }

    [Fact]
    public void Double_tap_detected_within_window()
    {
        var gr = Make();
        var pos = new Vector2(50, 50);

        // First tap
        gr.Update(new[] { TouchFactory.Up(0, pos, 0.1f) }, 0.1f);
        // Second tap quickly after (within double-tap window)
        var second = gr.Update(new[] { TouchFactory.Up(0, pos, 0.1f) }, 0.1f);

        Assert.Contains(second, e => e.Type == GestureType.DoubleTap);
    }

    [Fact]
    public void Double_tap_not_detected_outside_window()
    {
        var gr = Make();
        var pos = new Vector2(50, 50);

        gr.Update(new[] { TouchFactory.Up(0, pos, 0.1f) }, 0.1f);
        // Advance time past the double-tap window
        var second = gr.Update(new[] { TouchFactory.Up(0, pos, 0.1f) }, 0.5f);

        Assert.DoesNotContain(second, e => e.Type == GestureType.DoubleTap);
        Assert.Contains(second, e => e.Type == GestureType.Tap);
    }

    [Fact]
    public void Long_press_emitted_when_held_stationary()
    {
        var gr = Make();
        var touch = TouchFactory.Move(0, new Vector2(50, 50), new Vector2(52, 51), 0.55f);

        var events = gr.Update(new[] { touch }, 0.55f);

        Assert.Contains(events, e => e.Type == GestureType.LongPress);
    }

    [Fact]
    public void Long_press_not_repeated_same_contact()
    {
        var gr = Make();
        var touch = TouchFactory.Move(0, new Vector2(50, 50), new Vector2(52, 51), 0.55f);

        gr.Update(new[] { touch }, 0.55f);
        var second = gr.Update(new[] { touch }, 0.1f);

        Assert.DoesNotContain(second, e => e.Type == GestureType.LongPress);
    }

    [Fact]
    public void Drag_emitted_when_finger_moves_beyond_threshold()
    {
        var gr = Make();
        var touch = TouchFactory.Move(0, new Vector2(0, 0), new Vector2(50, 0), 0.1f);

        var events = gr.Update(new[] { touch }, 0.1f);

        Assert.Contains(events, e => e.Type == GestureType.Drag);
    }

    [Fact]
    public void Pinch_emitted_for_two_fingers_spreading()
    {
        var gr = Make();

        // Establish previous span
        gr.Update(new[]
        {
            TouchFactory.Move(0, new Vector2(0,0), new Vector2(0, 0), 0.1f),
            TouchFactory.Move(1, new Vector2(100,0), new Vector2(100, 0), 0.1f),
        }, 0.1f);

        // Fingers spread apart
        var events = gr.Update(new[]
        {
            TouchFactory.Move(0, new Vector2(0,0), new Vector2(-20, 0), 0.1f),
            TouchFactory.Move(1, new Vector2(100,0), new Vector2(120, 0), 0.1f),
        }, 0.1f);

        Assert.Contains(events, e => e.Type == GestureType.Pinch && e.PinchScale > 1f);
    }

    [Fact]
    public void Two_finger_drag_emitted_for_pan_gesture()
    {
        var gr = Make();

        gr.Update(new[]
        {
            TouchFactory.Move(0, new Vector2(50,50), new Vector2(50, 50), 0.1f),
            TouchFactory.Move(1, new Vector2(150,50), new Vector2(150, 50), 0.1f),
        }, 0.1f);

        var events = gr.Update(new[]
        {
            TouchFactory.Move(0, new Vector2(50,50), new Vector2(60, 50), 0.1f),
            TouchFactory.Move(1, new Vector2(150,50), new Vector2(160, 50), 0.1f),
        }, 0.1f);

        Assert.Contains(events, e => e.Type == GestureType.TwoFingerDrag);
    }
}

// ── TouchInput tests ──────────────────────────────────────────────────────────

public class TouchInputTests
{
    [Fact]
    public void Tap_maps_to_Select_pressed()
    {
        var input = new TouchInput();
        input.SetTouchPoints(new[] { TouchFactory.Up(0, new Vector2(100, 100), 0.1f) });
        input.Update(0.1f);

        Assert.True(input.IsActionPressed(InputAction.Select));
        Assert.False(input.IsActionPressed(InputAction.MoveCommand));
    }

    [Fact]
    public void Double_tap_maps_to_MoveCommand()
    {
        var input = new TouchInput();
        var pos = new Vector2(100, 100);

        // First tap
        input.SetTouchPoints(new[] { TouchFactory.Up(0, pos, 0.1f) });
        input.Update(0.1f);

        // Second tap (quick)
        input.SetTouchPoints(new[] { TouchFactory.Up(0, pos, 0.1f) });
        input.Update(0.1f);

        Assert.True(input.IsActionPressed(InputAction.MoveCommand));
    }

    [Fact]
    public void Long_press_maps_to_AttackCommand()
    {
        var input = new TouchInput();
        var touch = TouchFactory.Move(0, new Vector2(50, 50), new Vector2(52, 50), 0.55f);
        input.SetTouchPoints(new[] { touch });
        input.Update(0.55f);

        Assert.True(input.IsActionPressed(InputAction.AttackCommand));
    }

    [Fact]
    public void Tap_updates_PointerPosition()
    {
        var input = new TouchInput();
        var pos = new Vector2(300f, 200f);
        input.SetTouchPoints(new[] { TouchFactory.Up(0, pos, 0.1f) });
        input.Update(0.1f);

        Assert.Equal(pos, input.PointerPosition);
    }

    [Fact]
    public void Pressed_actions_cleared_next_frame()
    {
        var input = new TouchInput();
        input.SetTouchPoints(new[] { TouchFactory.Up(0, new Vector2(100, 100), 0.1f) });
        input.Update(0.1f);
        Assert.True(input.IsActionPressed(InputAction.Select));

        // Next frame — no new touches
        input.SetTouchPoints(Array.Empty<TouchPoint>());
        input.Update(0.016f);
        Assert.False(input.IsActionPressed(InputAction.Select));
    }
}

// ── AdaptiveLayout tests ──────────────────────────────────────────────────────

public class AdaptiveLayoutTests
{
    [Theory]
    [InlineData(1920, 1080, LayoutProfile.Desktop)]
    [InlineData(1280, 800,  LayoutProfile.Desktop)]
    [InlineData(1024, 768,  LayoutProfile.TabletLandscape)]
    [InlineData(768,  1024, LayoutProfile.TabletPortrait)]
    [InlineData(375,  812,  LayoutProfile.PhonePortrait)]
    [InlineData(812,  375,  LayoutProfile.PhoneLandscape)]
    public void Detect_returns_correct_profile(int w, int h, LayoutProfile expected)
    {
        var profile = AdaptiveLayout.Detect(new Vector2(w, h));
        Assert.Equal(expected, profile);
    }

    [Fact]
    public void Desktop_is_not_touch_device()
    {
        Assert.False(AdaptiveLayout.IsTouchDevice(LayoutProfile.Desktop));
    }

    [Theory]
    [InlineData(LayoutProfile.PhonePortrait)]
    [InlineData(LayoutProfile.PhoneLandscape)]
    [InlineData(LayoutProfile.TabletPortrait)]
    [InlineData(LayoutProfile.TabletLandscape)]
    public void Mobile_profiles_are_touch_devices(LayoutProfile profile)
    {
        Assert.True(AdaptiveLayout.IsTouchDevice(profile));
    }

    [Fact]
    public void Edge_scroll_disabled_on_mobile()
    {
        Assert.True(AdaptiveLayout.DisableEdgeScroll(LayoutProfile.PhonePortrait));
        Assert.False(AdaptiveLayout.DisableEdgeScroll(LayoutProfile.Desktop));
    }

    [Theory]
    [InlineData(LayoutProfile.PhonePortrait)]
    [InlineData(LayoutProfile.PhoneLandscape)]
    [InlineData(LayoutProfile.TabletPortrait)]
    [InlineData(LayoutProfile.TabletLandscape)]
    public void Mobile_button_size_meets_min_touch_target(LayoutProfile profile)
    {
        Vector2 size = AdaptiveLayout.RecommendedButtonSize(profile);
        Assert.True(size.X >= AdaptiveLayout.MinTouchTargetPx,
            $"{profile} button width {size.X} < {AdaptiveLayout.MinTouchTargetPx}px");
        Assert.True(size.Y >= AdaptiveLayout.MinTouchTargetPx,
            $"{profile} button height {size.Y} < {AdaptiveLayout.MinTouchTargetPx}px");
    }
}
