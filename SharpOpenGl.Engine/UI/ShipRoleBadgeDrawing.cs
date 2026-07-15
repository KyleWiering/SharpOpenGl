using OpenTK.Mathematics;
using SharpOpenGl.Engine.Entities;

namespace SharpOpenGl.Engine.UI;

/// <summary>Draws procedural role badges for build-tree ship entries.</summary>
public static class ShipRoleBadgeDrawing
{
    /// <summary>Recommended badge width and height in logical pixels.</summary>
    public const float BadgeSize = 14f;

    private const float LineThickness = 1.5f;

    /// <summary>Render a coloured corner badge with a role-specific glyph.</summary>
    public static void Draw(IUIRenderer renderer, ShipRole role, Vector2 position, float size = BadgeSize)
    {
        float drawSize = MathF.Max(size, BadgeSize);
        var tile = new Vector2(drawSize, drawSize);
        var (fill, glyph) = Palette(role);

        renderer.DrawRect(position, tile, fill);
        renderer.DrawRectOutline(position, tile, Darken(fill, 0.55f));

        float pad = drawSize * 0.18f;
        var innerPos = position + new Vector2(pad, pad);
        float innerSize = drawSize - pad * 2f;

        switch (role)
        {
            case ShipRole.Military:
                DrawCrosshair(renderer, innerPos, innerSize, glyph);
                break;
            case ShipRole.Engineering:
                DrawGear(renderer, innerPos, innerSize, glyph);
                break;
            case ShipRole.Political:
                DrawStarChevron(renderer, innerPos, innerSize, glyph);
                break;
        }
    }

    private static (Vector4 Fill, Vector4 Glyph) Palette(ShipRole role) => role switch
    {
        ShipRole.Military => (new Vector4(0.62f, 0.14f, 0.14f, 0.95f), new Vector4(0.98f, 0.88f, 0.88f, 1f)),
        ShipRole.Engineering => (new Vector4(0.62f, 0.42f, 0.08f, 0.95f), new Vector4(0.98f, 0.92f, 0.72f, 1f)),
        ShipRole.Political => (new Vector4(0.62f, 0.50f, 0.10f, 0.95f), new Vector4(1f, 0.94f, 0.55f, 1f)),
        _ => (new Vector4(0.3f, 0.3f, 0.3f, 0.95f), new Vector4(1f, 1f, 1f, 1f)),
    };

    private static void DrawCrosshair(IUIRenderer renderer, Vector2 pos, float size, Vector4 color)
    {
        float cx = pos.X + size * 0.5f;
        float cy = pos.Y + size * 0.5f;
        float arm = size * 0.34f;
        float ring = size * 0.22f;

        DrawLine(renderer, new Vector2(cx - arm, cy), new Vector2(cx + arm, cy), color);
        DrawLine(renderer, new Vector2(cx, cy - arm), new Vector2(cx, cy + arm), color);

        float ringPad = (size - ring) * 0.5f;
        var ringPos = pos + new Vector2(ringPad, ringPad);
        renderer.DrawRectOutline(ringPos, new Vector2(ring, ring), color);
    }

    private static void DrawGear(IUIRenderer renderer, Vector2 pos, float size, Vector4 color)
    {
        float hub = size * 0.28f;
        var hubPos = pos + new Vector2((size - hub) * 0.5f, (size - hub) * 0.5f);
        renderer.DrawRect(hubPos, new Vector2(hub, hub), color);

        float toothW = size * 0.16f;
        float toothH = size * 0.22f;
        float cx = pos.X + (size - toothW) * 0.5f;
        renderer.DrawRect(new Vector2(cx, pos.Y), new Vector2(toothW, toothH), color);
        renderer.DrawRect(new Vector2(cx, pos.Y + size - toothH), new Vector2(toothW, toothH), color);
        renderer.DrawRect(new Vector2(pos.X, pos.Y + (size - toothW) * 0.5f), new Vector2(toothH, toothW), color);
        renderer.DrawRect(new Vector2(pos.X + size - toothH, pos.Y + (size - toothW) * 0.5f), new Vector2(toothH, toothW), color);
    }

    private static void DrawStarChevron(IUIRenderer renderer, Vector2 pos, float size, Vector4 color)
    {
        float star = size * 0.34f;
        var starPos = new Vector2(pos.X + (size - star) * 0.5f, pos.Y + size * 0.04f);
        renderer.DrawRect(starPos, new Vector2(star, star * 0.85f), color);

        float chevW = size * 0.52f;
        float chevH = size * 0.14f;
        float chevX = pos.X + (size - chevW) * 0.5f;
        float chevY = pos.Y + size * 0.62f;
        renderer.DrawRect(new Vector2(chevX, chevY), new Vector2(chevW * 0.42f, chevH), color);
        renderer.DrawRect(new Vector2(chevX + chevW * 0.58f, chevY), new Vector2(chevW * 0.42f, chevH), color);
        renderer.DrawRect(new Vector2(chevX + chevW * 0.36f, chevY - chevH * 0.55f), new Vector2(chevW * 0.28f, chevH), color);
    }

    private static void DrawLine(IUIRenderer renderer, Vector2 from, Vector2 to, Vector4 color)
    {
        float dx = to.X - from.X;
        float dy = to.Y - from.Y;
        if (MathF.Abs(dx) >= MathF.Abs(dy))
        {
            float x = MathF.Min(from.X, to.X);
            float w = MathF.Abs(dx);
            if (w < LineThickness) w = LineThickness;
            float y = from.Y - LineThickness * 0.5f;
            renderer.DrawRect(new Vector2(x, y), new Vector2(w, LineThickness), color);
        }
        else
        {
            float y = MathF.Min(from.Y, to.Y);
            float h = MathF.Abs(dy);
            if (h < LineThickness) h = LineThickness;
            float x = from.X - LineThickness * 0.5f;
            renderer.DrawRect(new Vector2(x, y), new Vector2(LineThickness, h), color);
        }
    }

    private static Vector4 Darken(Vector4 color, float factor) =>
        new(color.X * factor, color.Y * factor, color.Z * factor, color.W);
}