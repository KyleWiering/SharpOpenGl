using OpenTK.Mathematics;
using SharpOpenGl.Engine.Build;

namespace SharpOpenGl.Engine.UI;

/// <summary>Draws procedural structure thumbnails using rect-only UI primitives.</summary>
public static class BuildIconDrawing
{
    /// <summary>Minimum recommended logical icon size for build-menu slots.</summary>
    public const float MinimumSize = 32f;

    /// <summary>
    /// Render a filled tile with simple accent rects that hint at the structure silhouette.
    /// Uses at most eight <see cref="IUIRenderer.DrawRect"/> calls per icon.
    /// </summary>
    public static void Draw(IUIRenderer renderer, BuildIconDescriptor icon, Vector2 position, float size)
    {
        float drawSize = MathF.Max(size, MinimumSize);
        var tile = new Vector2(drawSize, drawSize);
        Vector4 background = Darken(icon.PrimaryTint, 0.72f);
        renderer.DrawRect(position, tile, background);
        renderer.DrawRectOutline(position, tile, Darken(icon.AccentTint, 0.55f));

        float pad = drawSize * 0.12f;
        var innerPos = position + new Vector2(pad, pad);
        float innerSize = drawSize - pad * 2f;

        switch (icon.Shape)
        {
            case BuildIconShape.CommandCenter:
                DrawCommandCenter(renderer, icon, innerPos, innerSize);
                break;
            case BuildIconShape.Shipyard:
                DrawShipyard(renderer, icon, innerPos, innerSize);
                break;
            case BuildIconShape.Reactor:
                DrawReactor(renderer, icon, innerPos, innerSize);
                break;
            case BuildIconShape.Turret:
                DrawTurret(renderer, icon, innerPos, innerSize);
                break;
            case BuildIconShape.Sensor:
                DrawSensor(renderer, icon, innerPos, innerSize);
                break;
            case BuildIconShape.Depot:
                DrawDepot(renderer, icon, innerPos, innerSize);
                break;
            case BuildIconShape.Capstone:
                DrawCapstone(renderer, icon, innerPos, innerSize);
                break;
        }
    }

    private static void DrawCommandCenter(IUIRenderer renderer, BuildIconDescriptor icon, Vector2 pos, float size)
    {
        float w = size * 0.34f;
        float h = size * 0.62f;
        float x = pos.X + (size - w) * 0.5f;
        float y = pos.Y + size * 0.16f;
        renderer.DrawRect(new Vector2(x, y), new Vector2(w, h), icon.PrimaryTint);

        float wingW = size * 0.22f;
        float wingH = size * 0.18f;
        float wingY = pos.Y + size * 0.46f;
        renderer.DrawRect(new Vector2(pos.X + size * 0.08f, wingY), new Vector2(wingW, wingH), icon.AccentTint);
        renderer.DrawRect(new Vector2(pos.X + size - size * 0.08f - wingW, wingY), new Vector2(wingW, wingH), icon.AccentTint);

        float capW = size * 0.20f;
        float capH = size * 0.10f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - capW) * 0.5f, pos.Y + size * 0.08f),
            new Vector2(capW, capH),
            Brighten(icon.AccentTint, 1.15f));
    }

    private static void DrawShipyard(IUIRenderer renderer, BuildIconDescriptor icon, Vector2 pos, float size)
    {
        int bays = icon.BuildingType switch
        {
            "shipyard_large" => 3,
            "shipyard_medium" => 2,
            _ => 1,
        };

        float padH = size * 0.52f;
        renderer.DrawRect(
            new Vector2(pos.X + size * 0.08f, pos.Y + size - padH - size * 0.06f),
            new Vector2(size * 0.84f, padH),
            icon.PrimaryTint);

        float armH = size * 0.10f;
        renderer.DrawRect(
            new Vector2(pos.X + size * 0.18f, pos.Y + size * 0.22f),
            new Vector2(size * 0.64f, armH),
            icon.AccentTint);

        float bayW = size * 0.18f;
        float bayH = size * 0.16f;
        float gap = size * 0.06f;
        float totalW = bays * bayW + (bays - 1) * gap;
        float startX = pos.X + (size - totalW) * 0.5f;
        float bayY = pos.Y + size * 0.36f;
        for (int i = 0; i < bays; i++)
        {
            float x = startX + i * (bayW + gap);
            renderer.DrawRect(new Vector2(x, bayY), new Vector2(bayW, bayH), Brighten(icon.AccentTint, 1.1f));
        }
    }

    private static void DrawReactor(IUIRenderer renderer, BuildIconDescriptor icon, Vector2 pos, float size)
    {
        float core = size * 0.34f;
        var corePos = new Vector2(pos.X + (size - core) * 0.5f, pos.Y + size * 0.30f);
        renderer.DrawRect(corePos, new Vector2(core, core), Brighten(icon.AccentTint, 1.2f));

        float coilW = size * 0.14f;
        float coilH = size * 0.42f;
        float coilY = pos.Y + size * 0.26f;
        renderer.DrawRect(new Vector2(pos.X + size * 0.10f, coilY), new Vector2(coilW, coilH), icon.PrimaryTint);
        renderer.DrawRect(new Vector2(pos.X + size - size * 0.10f - coilW, coilY), new Vector2(coilW, coilH), icon.PrimaryTint);

        float capW = size * 0.48f;
        float capH = size * 0.08f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - capW) * 0.5f, pos.Y + size * 0.12f),
            new Vector2(capW, capH),
            icon.AccentTint);
    }

    private static void DrawTurret(IUIRenderer renderer, BuildIconDescriptor icon, Vector2 pos, float size)
    {
        float baseSize = size * 0.44f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - baseSize) * 0.5f, pos.Y + size * 0.46f),
            new Vector2(baseSize, baseSize * 0.55f),
            icon.PrimaryTint);

        float head = size * 0.24f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - head) * 0.5f, pos.Y + size * 0.30f),
            new Vector2(head, head),
            icon.AccentTint);

        bool isMissile = string.Equals(icon.BuildingType, "missile_battery", StringComparison.OrdinalIgnoreCase);
        float barrelW = size * (isMissile ? 0.56f : 0.48f);
        float barrelH = size * (isMissile ? 0.10f : 0.12f);
        renderer.DrawRect(
            new Vector2(pos.X + size * 0.46f, pos.Y + size * 0.34f),
            new Vector2(barrelW, barrelH),
            Brighten(icon.AccentTint, isMissile ? 0.95f : 1.1f));
    }

    private static void DrawSensor(IUIRenderer renderer, BuildIconDescriptor icon, Vector2 pos, float size)
    {
        float mastW = size * 0.10f;
        float mastH = size * 0.40f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - mastW) * 0.5f, pos.Y + size * 0.34f),
            new Vector2(mastW, mastH),
            icon.PrimaryTint);

        float dishW = size * 0.62f;
        float dishH = size * 0.18f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - dishW) * 0.5f, pos.Y + size * 0.18f),
            new Vector2(dishW, dishH),
            icon.AccentTint);

        float pingW = size * 0.12f;
        float pingH = size * 0.06f;
        renderer.DrawRect(new Vector2(pos.X + size * 0.16f, pos.Y + size * 0.10f), new Vector2(pingW, pingH), Brighten(icon.AccentTint, 1.2f));
        renderer.DrawRect(new Vector2(pos.X + size * 0.72f, pos.Y + size * 0.10f), new Vector2(pingW, pingH), Brighten(icon.AccentTint, 1.2f));
    }

    private static void DrawDepot(IUIRenderer renderer, BuildIconDescriptor icon, Vector2 pos, float size)
    {
        bool isRefinery = icon.BuildingType is "resource_refinery" or "fabrication_hub";
        float mainW = size * 0.56f;
        float mainH = size * (isRefinery ? 0.46f : 0.40f);
        renderer.DrawRect(
            new Vector2(pos.X + (size - mainW) * 0.5f, pos.Y + size * 0.30f),
            new Vector2(mainW, mainH),
            icon.PrimaryTint);

        float tank = size * 0.16f;
        float tankY = pos.Y + size * 0.48f;
        renderer.DrawRect(new Vector2(pos.X + size * 0.10f, tankY), new Vector2(tank, tank), icon.AccentTint);
        renderer.DrawRect(new Vector2(pos.X + size - size * 0.10f - tank, tankY), new Vector2(tank, tank), icon.AccentTint);

        float stripeW = size * 0.40f;
        float stripeH = size * 0.06f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - stripeW) * 0.5f, pos.Y + size * 0.20f),
            new Vector2(stripeW, stripeH),
            Brighten(icon.AccentTint, isRefinery ? 1.15f : 1.05f));
    }

    private static void DrawCapstone(IUIRenderer renderer, BuildIconDescriptor icon, Vector2 pos, float size)
    {
        float spireW = size * 0.18f;
        float spireH = size * 0.52f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - spireW) * 0.5f, pos.Y + size * 0.14f),
            new Vector2(spireW, spireH),
            icon.PrimaryTint);

        float ringW = size * 0.58f;
        float ringH = size * 0.10f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - ringW) * 0.5f, pos.Y + size * 0.34f),
            new Vector2(ringW, ringH),
            icon.AccentTint);

        float pillar = size * 0.10f;
        float pillarH = size * 0.28f;
        float pillarY = pos.Y + size * 0.54f;
        renderer.DrawRect(new Vector2(pos.X + size * 0.16f, pillarY), new Vector2(pillar, pillarH), Brighten(icon.AccentTint, 0.9f));
        renderer.DrawRect(new Vector2(pos.X + size - size * 0.16f - pillar, pillarY), new Vector2(pillar, pillarH), Brighten(icon.AccentTint, 0.9f));
    }

    private static Vector4 Darken(Vector4 color, float factor) =>
        new(color.X * factor, color.Y * factor, color.Z * factor, color.W);

    private static Vector4 Brighten(Vector4 color, float factor) =>
        new(
            MathF.Min(color.X * factor, 1f),
            MathF.Min(color.Y * factor, 1f),
            MathF.Min(color.Z * factor, 1f),
            color.W);
}