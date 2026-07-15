using OpenTK.Mathematics;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class SandboxSetupScreenTests
{
    private static readonly Vector2 ReferenceViewport = UIScaler.ReferenceSize;
    private static readonly Vector2 StartButtonCenter = new(790f, 667f);

    [Fact]
    public void SandboxSetupScreen_Start_emits_parsed_seed()
    {
        var screen = new SandboxSetupScreen();
        screen.SeedField.Value = "frontier-alpha";

        SandboxSetupResult? result = null;
        screen.StartRequested += r => result = r;

        bool consumed = screen.HandlePointerTapped(StartButtonCenter, 0, ReferenceViewport);

        Assert.True(consumed);
        Assert.NotNull(result);
        Assert.Equal("frontier-alpha", result!.SeedText);
        Assert.Equal(ProceduralSeedHelper.HashString("frontier-alpha"), result.ParsedSeed);
    }

    [Fact]
    public void SandboxSetupScreen_Start_emits_numeric_seed()
    {
        var screen = new SandboxSetupScreen();
        screen.SeedField.Value = "1337";

        var result = screen.BuildResult();

        Assert.Equal("1337", result.SeedText);
        Assert.Equal(1337, result.ParsedSeed);
    }

    [Fact]
    public void SandboxSetupScreen_empty_seed_uses_default_on_start()
    {
        var screen = new SandboxSetupScreen();
        SandboxSetupResult? result = null;
        screen.StartRequested += r => result = r;

        screen.FindButton("StartSandbox")!.Activate();

        Assert.NotNull(result);
        Assert.Equal(ProceduralSeedHelper.EmptyInputDefaultSeed, result!.ParsedSeed);
    }

    [Fact]
    public void MainMenuScreen_includes_Sandbox_button()
    {
        var screen = new MainMenuScreen();

        var sandboxButton = Assert.IsType<IconButton>(screen.FindButton("Sandbox"));

        Assert.Equal("Sandbox", sandboxButton.Label);
        Assert.True(sandboxButton.IsEnabled);
    }
}