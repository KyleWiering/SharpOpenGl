using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>
/// Full-screen animated parallax starfield drawn in the UI layer.
/// </summary>
public sealed class MenuStarfieldBackground : Widget
{
    private readonly Star[] _stars;
    private float _time;

    private readonly struct Star
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Depth;
        public readonly float Size;
        public readonly Vector4 Color;

        public Star(float x, float y, float depth, float size, Vector4 color)
        {
            X = x;
            Y = y;
            Depth = depth;
            Size = size;
            Color = color;
        }
    }

    public MenuStarfieldBackground(int starCount = 220, int seed = 42)
    {
        var random = new Random(seed);
        _stars = new Star[starCount];
        for (int i = 0; i < starCount; i++)
        {
            float depth = 0.15f + (float)random.NextDouble() * 0.85f;
            float brightness = 0.45f + (float)random.NextDouble() * 0.55f;
            float tint = (float)random.NextDouble();
            var color = new Vector4(
                0.75f + tint * 0.2f,
                0.8f + (1f - tint) * 0.15f,
                0.95f + tint * 0.05f,
                brightness * (0.35f + depth * 0.65f));
            _stars[i] = new Star(
                (float)random.NextDouble(),
                (float)random.NextDouble(),
                depth,
                1.5f + depth * 2.5f,
                color);
        }
    }

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        _time += deltaTime;
    }

    /// <inheritdoc/>
    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        DrawVerticalGradient(renderer, position, size, MenuTheme.StarfieldTop, MenuTheme.StarfieldBottom);

        for (int i = 0; i < _stars.Length; i++)
        {
            Star star = _stars[i];
            float driftX = MathF.Sin(_time * (0.08f + star.Depth * 0.12f) + star.Y * 6f) * (18f * star.Depth);
            float driftY = _time * (8f + star.Depth * 22f);
            float px = position.X + star.X * size.X + driftX;
            float py = position.Y + ((star.Y * size.Y + driftY) % (size.Y + 40f)) - 20f;
            float starSize = star.Size;
            renderer.DrawRect(new Vector2(px, py), new Vector2(starSize, starSize), star.Color);
        }
    }

    private static void DrawVerticalGradient(
        IUIRenderer renderer, Vector2 position, Vector2 size,
        Vector4 top, Vector4 bottom)
    {
        const int bands = 12;
        float bandHeight = size.Y / bands;
        for (int i = 0; i < bands; i++)
        {
            float t = i / (float)(bands - 1);
            Vector4 color = Vector4.Lerp(top, bottom, t);
            renderer.DrawRect(
                new Vector2(position.X, position.Y + i * bandHeight),
                new Vector2(size.X, bandHeight + 1f),
                color);
        }
    }
}