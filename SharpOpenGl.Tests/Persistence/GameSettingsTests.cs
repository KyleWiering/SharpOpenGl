using SharpOpenGl.Engine.Persistence;
using Xunit;

namespace SharpOpenGl.Tests.Persistence;

public class GameSettingsTests
{
    [Fact]
    public void Default_settings_are_valid()
    {
        var s = new GameSettings();
        Assert.Equal(1.0f, s.MasterVolume);
        Assert.Equal(1920, s.ResolutionWidth);
        Assert.Equal(1080, s.ResolutionHeight);
        Assert.Equal("High", s.QualityTier);
        Assert.NotNull(s.Accessibility);
    }

    [Fact]
    public void Clamped_clamps_volumes_to_one()
    {
        var s = new GameSettings { MasterVolume = 5f, SfxVolume = -1f };
        GameSettings c = s.Clamped();
        Assert.Equal(1f, c.MasterVolume);
        Assert.Equal(0f, c.SfxVolume);
    }

    [Fact]
    public void Clamped_enforces_minimum_resolution()
    {
        var s = new GameSettings { ResolutionWidth = 100, ResolutionHeight = 100 };
        GameSettings c = s.Clamped();
        Assert.Equal(640, c.ResolutionWidth);
        Assert.Equal(480, c.ResolutionHeight);
    }

    [Fact]
    public void Clamped_clamps_font_scale()
    {
        var s = new GameSettings
        {
            Accessibility = new AccessibilitySettings { FontScale = 10f },
        };
        GameSettings c = s.Clamped();
        Assert.Equal(3f, c.Accessibility.FontScale);
    }

    [Fact]
    public void Clamped_preserves_colorblind_mode()
    {
        var s = new GameSettings
        {
            Accessibility = new AccessibilitySettings
            {
                ColorblindMode = ColorblindMode.RedGreen,
                FontScale = 1.2f,
            },
        };
        GameSettings c = s.Clamped();
        Assert.Equal(ColorblindMode.RedGreen, c.Accessibility.ColorblindMode);
        Assert.Equal(1.2f, c.Accessibility.FontScale, 0.001f);
    }

    [Fact]
    public void Clamped_clamps_camera_speed()
    {
        var s = new GameSettings { CameraPanSpeed = 100f, CameraZoomSpeed = 0f };
        GameSettings c = s.Clamped();
        Assert.Equal(5f, c.CameraPanSpeed);
        Assert.Equal(0.1f, c.CameraZoomSpeed);
    }
}
