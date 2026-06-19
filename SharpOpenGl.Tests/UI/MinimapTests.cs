using OpenTK.Mathematics;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class MinimapTests
{
    private static readonly Vector2 ReferenceViewport = UIScaler.ReferenceSize;

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

        Assert.Contains(renderer.Rects, r => r.Color == minimap.VisibleColor);
        Assert.Contains(renderer.Rects, r => r.Color == minimap.UnexploredColor);
    }

    private sealed class RecordingUIRenderer : IUIRenderer
    {
        public List<(Vector2 Position, Vector2 Size, Vector4 Color)> Rects { get; } = new();

        public Vector2 ViewportSize => ReferenceViewport;

        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) =>
            Rects.Add((position, size, color));

        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }

        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color) { }
    }
}