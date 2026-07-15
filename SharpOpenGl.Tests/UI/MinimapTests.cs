using OpenTK.Mathematics;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Rendering;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class MinimapTests
{
    private static readonly Vector2 ReferenceViewport = UIScaler.ReferenceSize;

    [Fact]
    public void Hover_returns_navigation_tooltip()
    {
        var minimap = new Minimap
        {
            Anchor = Anchor.BottomLeft,
            Position = new Vector2(8f, -248f),
            Size = new Vector2(240f, 240f),
        };

        minimap.UpdatePointerState(new Vector2(120f, 700f), false, Vector2.Zero, ReferenceViewport);

        TooltipContent? content = minimap.GetTooltipContent();

        Assert.NotNull(content);
        Assert.Equal("Tactical Minimap", content!.Title);
        Assert.Equal("Click to pan camera", content.RoleLine);
        Assert.Contains("viewport", content.Footprint ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryGetClick_returns_normalized_position_inside_minimap()
    {
        var minimap = new Minimap
        {
            Anchor = Anchor.BottomLeft,
            Position = new Vector2(8f, -248f),
            Size = new Vector2(240f, 240f),
        };

        bool hit = minimap.TryGetClick(
            new Vector2(120f, 700f),
            Vector2.Zero,
            ReferenceViewport,
            out Vector2 norm);

        Assert.True(hit);
        Assert.InRange(norm.X, 0.4f, 0.5f);
        Assert.InRange(norm.Y, 0.4f, 0.5f);
    }

    [Fact]
    public void Default_fog_colors_sync_with_FogVisualPalette()
    {
        var minimap = new Minimap();
        minimap.SyncFogPaletteFromWorld();

        Assert.Equal(
            FogVisualPalette.ToColor(FogVisualPalette.UnexploredCore, FogVisualPalette.UnexploredAlpha),
            minimap.UnexploredColor);
        Assert.Equal(
            FogVisualPalette.ToColor(FogVisualPalette.ExploredCore, FogVisualPalette.ExploredAlpha),
            minimap.ExploredColor);
        Assert.Equal(
            FogVisualPalette.ToColor(FogVisualPalette.ExploredVeil, 0.15f),
            minimap.VisibleColor);
    }

    [Fact]
    public void TryGetClick_hits_inclusive_edges()
    {
        var minimap = new Minimap
        {
            Anchor = Anchor.BottomLeft,
            Position = new Vector2(8f, -248f),
            Size = new Vector2(240f, 240f),
        };

        var (pos, size) = minimap.Resolve(Vector2.Zero, ReferenceViewport);

        Assert.True(minimap.TryGetClick(pos, Vector2.Zero, ReferenceViewport, out Vector2 topLeft));
        Assert.Equal(0f, topLeft.X, 2);
        Assert.Equal(0f, topLeft.Y, 2);

        Assert.True(minimap.TryGetClick(pos + size, Vector2.Zero, ReferenceViewport, out Vector2 bottomRight));
        Assert.Equal(1f, bottomRight.X, 2);
        Assert.Equal(1f, bottomRight.Y, 2);
    }

    [Fact]
    public void TryGetClick_misses_outside_minimap()
    {
        var minimap = new Minimap
        {
            Anchor = Anchor.BottomLeft,
            Position = new Vector2(8f, -248f),
            Size = new Vector2(240f, 240f),
        };

        bool hit = minimap.TryGetClick(
            new Vector2(960f, 540f),
            Vector2.Zero,
            ReferenceViewport,
            out _);

        Assert.False(hit);
    }

    private static readonly Vector4 LegacyFlatGrayExplored = new(0.15f, 0.15f, 0.25f, 1f);

    [Fact]
    public void Fog_display_uses_nebula_palette_not_flat_gray()
    {
        var grid = new GridSystem(20, 20);
        var fog = new FogOfWar(grid, playerCount: 1);

        var minimap = new Minimap
        {
            GridSize = new Vector2i(20, 20),
            FogDisplayCells = new Vector2i(4, 4),
            FogOfWar = fog,
            PlayerId = 0,
        };

        var renderer = new RecordingUIRenderer();
        minimap.Draw(renderer, Vector2.Zero, ReferenceViewport);

        var fogRects = renderer.Rects.Where(r => r.Color != minimap.BackgroundColor).ToList();
        Assert.NotEmpty(fogRects);
        Assert.Contains(fogRects, r => MatchesPaletteCore(r.Color, FogVisualPalette.UnexploredCore));
        Assert.DoesNotContain(fogRects, r => r.Color == LegacyFlatGrayExplored);
    }

    [Fact]
    public void Fog_display_samples_coarse_grid()
    {
        var grid = new GridSystem(20, 20);
        var fog = new FogOfWar(grid, playerCount: 1);
        fog.Reveal(0, 10, 10, 2);

        var minimap = new Minimap
        {
            GridSize = new Vector2i(20, 20),
            FogDisplayCells = new Vector2i(4, 4),
            FogOfWar = fog,
            PlayerId = 0,
        };

        var renderer = new RecordingUIRenderer();
        minimap.Draw(renderer, Vector2.Zero, ReferenceViewport);

        var fogRects = renderer.Rects
            .Where(r => r.Color != minimap.BackgroundColor)
            .ToList();

        Assert.Contains(fogRects, r => MatchesPaletteCore(r.Color, FogVisualPalette.UnexploredCore));

        var allFogRenderer = new RecordingUIRenderer();
        var allFogGrid = new GridSystem(20, 20);
        var allFogMinimap = new Minimap
        {
            GridSize = new Vector2i(20, 20),
            FogDisplayCells = new Vector2i(4, 4),
            FogOfWar = new FogOfWar(allFogGrid, playerCount: 1),
            PlayerId = 0,
        };
        allFogMinimap.Draw(allFogRenderer, Vector2.Zero, ReferenceViewport);

        int revealedNebulaRects = fogRects.Count(r => IsNebulaFogColor(r.Color));
        int fullMapNebulaRects = allFogRenderer.Rects.Count(r => IsNebulaFogColor(r.Color));
        Assert.True(revealedNebulaRects < fullMapNebulaRects);
    }

    [Fact]
    public void Nebula_fog_skips_visible_tiles()
    {
        var grid = new GridSystem(20, 20);
        var fog = new FogOfWar(grid, playerCount: 1);
        for (int y = 0; y < grid.Height; y++)
        for (int x = 0; x < grid.Width; x++)
            fog.Reveal(0, x, y, radius: 0);

        var minimap = new Minimap
        {
            GridSize = new Vector2i(20, 20),
            FogDisplayCells = new Vector2i(4, 4),
            FogOfWar = fog,
            PlayerId = 0,
        };

        var renderer = new RecordingUIRenderer();
        minimap.Draw(renderer, Vector2.Zero, ReferenceViewport);

        var nebulaFogRects = renderer.Rects.Where(r => IsNebulaFogColor(r.Color)).ToList();
        Assert.Empty(nebulaFogRects);
    }

    private static bool MatchesPaletteCore(Vector4 color, Vector3 core, float tolerance = 0.05f) =>
        Math.Abs(color.X - core.X) <= tolerance
        && Math.Abs(color.Y - core.Y) <= tolerance
        && Math.Abs(color.Z - core.Z) <= tolerance;

    private static bool IsNebulaFogColor(Vector4 color) =>
        MatchesPaletteCore(color, FogVisualPalette.UnexploredCore)
        || MatchesPaletteCore(color, FogVisualPalette.UnexploredVeil)
        || MatchesPaletteCore(color, FogVisualPalette.ExploredCore)
        || MatchesPaletteCore(color, FogVisualPalette.ExploredVeil);

    [Fact]
    public void Feature_markers_draw_larger_than_unit_dots()
    {
        var minimap = new Minimap
        {
            Size = new Vector2(240f, 240f),
            FeatureMarkers =
            [
                new MinimapFeatureMarker(new Vector2(0.5f, 0.5f), MinimapFeatureKind.ResourceEnergy,
                    Minimap.ResourceEnergyColor),
            ],
            Units =
            [
                new MinimapUnit(new Vector2(0.25f, 0.25f), GameplayEntityDisplay.FriendlyColor, true),
            ],
        };

        var renderer = new RecordingUIRenderer();
        minimap.Draw(renderer, Vector2.Zero, ReferenceViewport);

        float featureArea = renderer.Rects
            .Where(r => r.Color == Minimap.ResourceEnergyColor)
            .Max(r => r.Size.X * r.Size.Y);
        float unitArea = renderer.Rects
            .Where(r => r.Color == GameplayEntityDisplay.FriendlyColor)
            .Max(r => r.Size.X * r.Size.Y);

        Assert.True(featureArea > unitArea);
    }

    [Fact]
    public void Feature_markers_use_kind_specific_colors()
    {
        var minimap = new Minimap
        {
            Size = new Vector2(240f, 240f),
            FeatureMarkers =
            [
                new MinimapFeatureMarker(new Vector2(0.3f, 0.5f), MinimapFeatureKind.ResourceEnergy,
                    Minimap.ColorForFeature(MinimapFeatureKind.ResourceEnergy)),
                new MinimapFeatureMarker(new Vector2(0.7f, 0.5f), MinimapFeatureKind.ResourceMinerals,
                    Minimap.ColorForFeature(MinimapFeatureKind.ResourceMinerals)),
            ],
        };

        var renderer = new RecordingUIRenderer();
        minimap.Draw(renderer, Vector2.Zero, ReferenceViewport);

        Assert.Contains(renderer.Rects, r => r.Color == Minimap.ResourceEnergyColor);
        Assert.Contains(renderer.Rects, r => r.Color == Minimap.ResourceMineralsColor);
        Assert.NotEqual(Minimap.ResourceEnergyColor, Minimap.ResourceMineralsColor);
    }

    [Fact]
    public void Neutral_planet_marker_uses_outline_only()
    {
        var neutralColor = Minimap.ColorForFeature(MinimapFeatureKind.NeutralPlanet);
        var minimap = new Minimap
        {
            Size = new Vector2(240f, 240f),
            FeatureMarkers =
            [
                new MinimapFeatureMarker(new Vector2(0.5f, 0.5f), MinimapFeatureKind.NeutralPlanet, neutralColor),
            ],
        };

        var renderer = new RecordingUIRenderer();
        minimap.Draw(renderer, Vector2.Zero, ReferenceViewport);

        Assert.Contains(renderer.Outlines, o => o.Color == neutralColor);
        Assert.DoesNotContain(renderer.Rects, r => r.Color == neutralColor);
    }

    private sealed class RecordingUIRenderer : IUIRenderer
    {
        public List<(Vector2 Position, Vector2 Size, Vector4 Color)> Rects { get; } = new();
        public List<(Vector2 Position, Vector2 Size, Vector4 Color)> Outlines { get; } = new();

        public Vector2 ViewportSize => ReferenceViewport;

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) =>
            Rects.Add((position, size, color));

        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) =>
            Outlines.Add((position, size, color));

        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color) { }
    }
}