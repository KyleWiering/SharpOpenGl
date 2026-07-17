using Xunit;

namespace SharpOpenGl.Tests.Browser;

/// <summary>
/// Headless smoke checks for the Blazor WASM boot chain (no browser runtime required).
/// </summary>
public class BrowserBootSmokeTests
{
    [Fact]
    public void GameApp_boots_host_and_starts_render_loop()
    {
        string razor = File.ReadAllText(GameAppSourcePath);
        string gameLoop = File.ReadAllText(GameLoopJsPath);

        Assert.Contains("await _host.InitializeAsync(width, height)", razor);
        Assert.Contains("sharpLoop.start", razor);
        Assert.Contains("sharpGameInput.attach", razor);
        Assert.Contains("OnFrame", razor);
        Assert.Contains("OnScroll", razor);
        Assert.Contains("invokeMethodAsync('OnFrame', dt)", gameLoop);

        string gameInput = File.ReadAllText(GameInputJsPath);
        Assert.Contains("addEventListener('wheel'", gameInput);
        Assert.Contains("invokeMethodAsync('OnScroll'", gameInput);
    }

    [Fact]
    public void BrowserHost_initialize_wires_scenes_assets_and_main_menu()
    {
        string source = File.ReadAllText(BrowserGameHostSourcePath);

        Assert.Contains("public async Task InitializeAsync(int width, int height)", source);
        Assert.Contains("await PreloadAssetsAsync()", source);
        Assert.Contains("await _uiRenderer.InitializeAsync(\"ui-canvas\"", source);
        Assert.Contains("await _glRenderer.InitializeAsync(\"gl-canvas\"", source);
        Assert.Contains("await _meshes.InitializeAsync(_glRenderer)", source);
        Assert.Contains("_sceneManager.TransitionTo(SceneMainMenu, GameState.MainMenu)", source);
        Assert.Contains("_initialized = true", source);
    }

    [Fact]
    public void BrowserHost_on_frame_updates_scene_world_and_ui()
    {
        string source = File.ReadAllText(BrowserGameHostSourcePath);

        Assert.Contains("if (!_initialized) return", source);
        Assert.Contains("_sceneManager.Update(deltaTime)", source);
        Assert.Contains("_world.Update(deltaTime)", source);
        Assert.Contains("_uiManager.Draw(_uiRenderer)", source);
    }

    [Fact]
    public void BrowserHost_preloads_mission_and_ship_assets()
    {
        string source = File.ReadAllText(BrowserGameHostSourcePath);

        Assert.Contains("Missions/tutorial_01", source);
        Assert.Contains("Ships/hero_default", source);
        Assert.Contains("Ships/fighter_basic", source);
        Assert.Contains("await _assetSource.PreloadAsync(keys, GameDataRoot)", source);
    }

    private static string GameAppSourcePath => ResolveRepoFile("SharpOpenGl.Browser", "Components", "GameApp.razor");

    private static string GameLoopJsPath =>
        ResolveRepoFile("SharpOpenGl.Browser", "wwwroot", "js", "game-loop.js");

    private static string GameInputJsPath =>
        ResolveRepoFile("SharpOpenGl.Browser", "wwwroot", "js", "game-input.js");

    private static string BrowserGameHostSourcePath =>
        ResolveRepoFile("SharpOpenGl.Browser", "Game", "BrowserGameHost.cs");

    private static string ResolveRepoFile(params string[] parts)
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "SharpOpenGl.sln")))
            dir = Directory.GetParent(dir)?.FullName;
        if (dir == null)
            throw new InvalidOperationException("Could not locate SharpOpenGl.sln.");
        return Path.Combine(new[] { dir }.Concat(parts).ToArray());
    }
}