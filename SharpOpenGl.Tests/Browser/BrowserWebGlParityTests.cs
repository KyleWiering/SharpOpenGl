using Xunit;

namespace SharpOpenGl.Tests.Browser;

/// <summary>Smoke checks for WebGL2 parity items from docs/WEBGL2_PARITY.md.</summary>
public class BrowserWebGlParityTests
{
    [Fact]
    public void WebGl_renderer_supports_line_strip_primitive()
    {
        string source = File.ReadAllText(WebGlRendererJsPath);

        Assert.Contains("primitiveType === 3 ? gl.LINE_STRIP", source);
        Assert.Contains("function drawLineStrip", source);
        Assert.Contains("drawLineStrip", source);
    }

    [Fact]
    public void WebGl_renderer_uses_glsl_es_300_shaders()
    {
        string source = File.ReadAllText(WebGlRendererJsPath);

        Assert.Contains("#version 300 es", source);
        Assert.Contains("precision mediump float", source);
        Assert.DoesNotContain("#version 330 core", source);
    }

    [Fact]
    public void Browser_gameplay_renderer_draws_waypoint_route_previews()
    {
        string source = File.ReadAllText(BrowserGameplayRendererSourcePath);

        Assert.Contains("RenderRoutePreviews", source);
        Assert.Contains("RoutePreviewHelper.BuildSegments", source);
        Assert.Contains("DrawLineStrip", source);
        Assert.Contains("ResolveProjectileTint", source);
        Assert.Contains("HomingTrailSteerTint", source);
    }

    [Fact]
    public void Browser_terrain_readability_overlay_stub_matches_desktop_contract()
    {
        string renderer = File.ReadAllText(BrowserGameplayRendererSourcePath);
        string overlay = File.ReadAllText(BrowserTerrainOverlaySourcePath);

        Assert.Contains("BrowserTerrainReadabilityOverlay", renderer);
        Assert.Contains("SyncAndRenderTerrainOverlay", renderer);
        Assert.Contains("TerrainReadabilityOverlay", overlay);
        Assert.Contains("IsDrawEnabled", overlay);
        Assert.DoesNotContain("IsDrawEnabled => false", overlay);
    }

    [Fact]
    public void Browser_mesh_library_allocates_route_preview_buffer()
    {
        string source = File.ReadAllText(BrowserMeshLibrarySourcePath);

        Assert.Contains("RoutePreviewBuffer", source);
    }

    private static string WebGlRendererJsPath =>
        ResolveRepoFile("SharpOpenGl.Browser", "wwwroot", "js", "webgl-renderer.js");

    private static string BrowserGameplayRendererSourcePath =>
        ResolveRepoFile("SharpOpenGl.Browser", "Rendering", "BrowserGameplayRenderer.cs");

    private static string BrowserMeshLibrarySourcePath =>
        ResolveRepoFile("SharpOpenGl.Browser", "Rendering", "BrowserMeshLibrary.cs");

    private static string BrowserTerrainOverlaySourcePath =>
        ResolveRepoFile("SharpOpenGl.Browser", "Rendering", "BrowserTerrainReadabilityOverlay.cs");

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