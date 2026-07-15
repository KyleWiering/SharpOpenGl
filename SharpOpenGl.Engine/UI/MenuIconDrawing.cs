using OpenTK.Mathematics;
using SharpOpenGl.Engine.Entities;

namespace SharpOpenGl.Engine.UI;

/// <summary>Procedural command glyphs for icon-oriented menu buttons.</summary>
public enum MenuIconKind
{
    Move,
    Stop,
    Patrol,
    Attack,
    AttackMove,
    StancePassive,
    StanceDefensive,
    StanceAggressive,
    FormationLine,
    FormationWedge,
    FormationColumn,
    FormationBox,
    FormationScatter,
    Build,
    Harvest,

    UnitFriendly,
    UnitHostile,
    UnitNeutral,
    UnitHarvestable,
    UnitScenery,

    StatHP,
    StatShield,
    StatArmor,
    StatCargo,
    StatHarvest,

    HullMilitary,
    HullEngineering,
    HullPolitical,

    NavNewGame,
    NavSandbox,
    NavMultiplayer,
    NavContinue,
    NavResume,
    NavSave,
    NavLoadGame,
    NavShipDesigner,
    NavSettings,
    NavQuit,
    NavBack,
    NavStartMission,
    NavBriefing,
    NavObjectives,
    NavCompleted,
}

/// <summary>Draws procedural ship-command icons using rect-only UI primitives.</summary>
public static class MenuIconDrawing
{
    /// <summary>Minimum recommended logical icon size for command slots.</summary>
    public const float MinimumSize = 32f;

    /// <summary>Minimum logical icon size for list-row and stat micro-glyphs.</summary>
    public const float MinimumListSize = 24f;

    /// <summary>
    /// Render a command glyph at <paramref name="position"/> with side length <paramref name="size"/>.
    /// Uses at most eight <see cref="IUIRenderer.DrawRect"/> calls per glyph.
    /// </summary>
    public static void Draw(
        IUIRenderer renderer, MenuIconKind kind,
        Vector2 position, float size,
        Vector4 primaryTint, Vector4 accentTint)
    {
        float drawSize = MathF.Max(size, MinimumSizeFor(kind));
        float pad = drawSize * 0.10f;
        var innerPos = position + new Vector2(pad, pad);
        float innerSize = drawSize - pad * 2f;

        switch (kind)
        {
            case MenuIconKind.Move:
                DrawMove(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.Stop:
                DrawStop(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.Patrol:
                DrawPatrol(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.Attack:
                DrawAttack(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.AttackMove:
                DrawAttackMove(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.StancePassive:
                DrawStancePassive(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.StanceDefensive:
                DrawStanceDefensive(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.StanceAggressive:
                DrawStanceAggressive(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.FormationLine:
                DrawFormationLine(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.FormationWedge:
                DrawFormationWedge(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.FormationColumn:
                DrawFormationColumn(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.FormationBox:
                DrawFormationBox(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.FormationScatter:
                DrawFormationScatter(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.Build:
                DrawBuild(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.Harvest:
                DrawHarvest(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.UnitFriendly:
                DrawUnitFriendly(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.UnitHostile:
                DrawUnitHostile(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.UnitNeutral:
                DrawUnitNeutral(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.UnitHarvestable:
                DrawUnitHarvestable(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.UnitScenery:
                DrawUnitScenery(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.StatHP:
                DrawStatHP(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.StatShield:
                DrawStatShield(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.StatArmor:
                DrawStatArmor(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.StatCargo:
                DrawStatCargo(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.StatHarvest:
                DrawStatHarvest(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.HullMilitary:
                DrawHullMilitary(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.HullEngineering:
                DrawHullEngineering(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.HullPolitical:
                DrawHullPolitical(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.NavNewGame:
                DrawNavNewGame(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.NavSandbox:
                DrawNavSandbox(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.NavMultiplayer:
                DrawNavMultiplayer(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.NavContinue:
                DrawNavContinue(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.NavResume:
                DrawNavResume(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.NavSave:
                DrawNavSave(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.NavLoadGame:
                DrawNavLoadGame(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.NavShipDesigner:
                DrawNavShipDesigner(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.NavSettings:
                DrawNavSettings(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.NavQuit:
                DrawNavQuit(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.NavBack:
                DrawNavBack(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.NavStartMission:
                DrawNavStartMission(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.NavBriefing:
                DrawNavBriefing(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.NavObjectives:
                DrawNavObjectives(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
            case MenuIconKind.NavCompleted:
                DrawNavCompleted(renderer, innerPos, innerSize, primaryTint, accentTint);
                break;
        }
    }

    /// <summary>Maps <see cref="EntityDisplayKind"/> to a tinted entity-kind glyph.</summary>
    public static void DrawEntityKind(
        IUIRenderer renderer, EntityDisplayKind kind,
        Vector2 position, float size,
        Vector4? primaryTint = null, Vector4? accentTint = null)
    {
        var (primary, accent) = EntityKindTints(kind);
        Draw(renderer, EntityKindToIcon(kind), position, size, primaryTint ?? primary, accentTint ?? accent);
    }

    /// <summary>Maps <see cref="ShipRole"/> to a hull silhouette glyph.</summary>
    public static void DrawShipRole(
        IUIRenderer renderer, ShipRole role,
        Vector2 position, float size,
        Vector4? primaryTint = null, Vector4? accentTint = null)
    {
        var (primary, accent) = HullRoleTints(role);
        Draw(renderer, HullRoleToIcon(role), position, size, primaryTint ?? primary, accentTint ?? accent);
    }

    /// <summary>Entity display kind → procedural entity icon kind.</summary>
    public static MenuIconKind EntityKindToIcon(EntityDisplayKind kind) => kind switch
    {
        EntityDisplayKind.Hostile => MenuIconKind.UnitHostile,
        EntityDisplayKind.Neutral => MenuIconKind.UnitNeutral,
        EntityDisplayKind.Harvestable => MenuIconKind.UnitHarvestable,
        EntityDisplayKind.Scenery => MenuIconKind.UnitScenery,
        _ => MenuIconKind.UnitFriendly,
    };

    /// <summary>Ship role → procedural hull icon kind.</summary>
    public static MenuIconKind HullRoleToIcon(ShipRole role) => role switch
    {
        ShipRole.Engineering => MenuIconKind.HullEngineering,
        ShipRole.Political => MenuIconKind.HullPolitical,
        _ => MenuIconKind.HullMilitary,
    };

    private static float MinimumSizeFor(MenuIconKind kind) => kind switch
    {
        MenuIconKind.UnitFriendly or MenuIconKind.UnitHostile or MenuIconKind.UnitNeutral
            or MenuIconKind.UnitHarvestable or MenuIconKind.UnitScenery
            or MenuIconKind.StatHP or MenuIconKind.StatShield or MenuIconKind.StatArmor
            or MenuIconKind.StatCargo or MenuIconKind.StatHarvest
            or MenuIconKind.HullMilitary or MenuIconKind.HullEngineering or MenuIconKind.HullPolitical
            or MenuIconKind.NavNewGame or MenuIconKind.NavSandbox or MenuIconKind.NavMultiplayer
            or MenuIconKind.NavContinue or MenuIconKind.NavResume or MenuIconKind.NavSave
            or MenuIconKind.NavLoadGame or MenuIconKind.NavShipDesigner
            or MenuIconKind.NavSettings or MenuIconKind.NavQuit or MenuIconKind.NavBack
            or MenuIconKind.NavStartMission or MenuIconKind.NavBriefing or MenuIconKind.NavObjectives
            or MenuIconKind.NavCompleted
            => MinimumListSize,
        _ => MinimumSize,
    };

    private static (Vector4 Primary, Vector4 Accent) EntityKindTints(EntityDisplayKind kind)
    {
        Vector4 color = GameplayEntityDisplay.LabelColor(kind);
        return (Darken(color, 0.72f), color);
    }

    private static (Vector4 Primary, Vector4 Accent) HullRoleTints(ShipRole role) => role switch
    {
        ShipRole.Military => (new Vector4(0.62f, 0.14f, 0.14f, 0.95f), new Vector4(0.98f, 0.88f, 0.88f, 1f)),
        ShipRole.Engineering => (new Vector4(0.62f, 0.42f, 0.08f, 0.95f), new Vector4(0.98f, 0.92f, 0.72f, 1f)),
        ShipRole.Political => (new Vector4(0.62f, 0.50f, 0.10f, 0.95f), new Vector4(1f, 0.94f, 0.55f, 1f)),
        _ => (new Vector4(0.3f, 0.3f, 0.3f, 0.95f), new Vector4(1f, 1f, 1f, 1f)),
    };

    private static void DrawMove(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float shaftW = size * 0.14f;
        float shaftH = size * 0.42f;
        float shaftX = pos.X + (size - shaftW) * 0.5f;
        float shaftY = pos.Y + size * 0.34f;
        renderer.DrawRect(new Vector2(shaftX, shaftY), new Vector2(shaftW, shaftH), primary);

        float headW = size * 0.52f;
        float headH = size * 0.16f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - headW) * 0.5f, pos.Y + size * 0.14f),
            new Vector2(headW, headH),
            accent);

        float wing = size * 0.18f;
        renderer.DrawRect(
            new Vector2(pos.X + size * 0.12f, pos.Y + size * 0.26f),
            new Vector2(wing, wing * 0.55f),
            Brighten(accent, 1.1f));
        renderer.DrawRect(
            new Vector2(pos.X + size - size * 0.12f - wing, pos.Y + size * 0.26f),
            new Vector2(wing, wing * 0.55f),
            Brighten(accent, 1.1f));
    }

    private static void DrawStop(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float frame = size * 0.62f;
        float frameX = pos.X + (size - frame) * 0.5f;
        float frameY = pos.Y + size * 0.12f;
        renderer.DrawRect(new Vector2(frameX, frameY), new Vector2(frame, frame), Darken(primary, 0.75f));

        float barW = size * 0.38f;
        float barH = size * 0.12f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - barW) * 0.5f, pos.Y + size * 0.40f),
            new Vector2(barW, barH),
            accent);

        float corner = size * 0.10f;
        renderer.DrawRect(new Vector2(frameX, frameY), new Vector2(corner, corner), primary);
        renderer.DrawRect(new Vector2(frameX + frame - corner, frameY), new Vector2(corner, corner), primary);
        renderer.DrawRect(new Vector2(frameX, frameY + frame - corner), new Vector2(corner, corner), primary);
        renderer.DrawRect(new Vector2(frameX + frame - corner, frameY + frame - corner), new Vector2(corner, corner), primary);
    }

    private static void DrawPatrol(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float dot = size * 0.14f;
        float y = pos.Y + size * 0.58f;
        renderer.DrawRect(new Vector2(pos.X + size * 0.10f, y), new Vector2(dot, dot), accent);
        renderer.DrawRect(new Vector2(pos.X + size * 0.42f, pos.Y + size * 0.22f), new Vector2(dot, dot), accent);
        renderer.DrawRect(new Vector2(pos.X + size * 0.74f, y), new Vector2(dot, dot), accent);

        float segW = size * 0.22f;
        float segH = size * 0.08f;
        renderer.DrawRect(new Vector2(pos.X + size * 0.18f, pos.Y + size * 0.44f), new Vector2(segW, segH), primary);
        renderer.DrawRect(new Vector2(pos.X + size * 0.50f, pos.Y + size * 0.36f), new Vector2(segW, segH), primary);
    }

    private static void DrawAttack(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float armW = size * 0.56f;
        float armH = size * 0.10f;
        float armX = pos.X + (size - armW) * 0.5f;
        float armY = pos.Y + (size - armH) * 0.5f;
        renderer.DrawRect(new Vector2(armX, armY), new Vector2(armW, armH), primary);
        renderer.DrawRect(
            new Vector2(pos.X + (size - armH) * 0.5f, pos.Y + size * 0.16f),
            new Vector2(armH, armW),
            primary);

        float core = size * 0.14f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - core) * 0.5f, pos.Y + (size - core) * 0.5f),
            new Vector2(core, core),
            accent);

        float bracket = size * 0.12f;
        float thick = size * 0.06f;
        renderer.DrawRect(new Vector2(pos.X + size * 0.08f, pos.Y + size * 0.10f), new Vector2(bracket, thick), Darken(accent, 0.85f));
        renderer.DrawRect(new Vector2(pos.X + size * 0.08f, pos.Y + size * 0.10f), new Vector2(thick, bracket), Darken(accent, 0.85f));
        renderer.DrawRect(new Vector2(pos.X + size - size * 0.08f - bracket, pos.Y + size - size * 0.10f - thick), new Vector2(bracket, thick), Darken(accent, 0.85f));
    }

    private static void DrawAttackMove(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float shaftW = size * 0.12f;
        float shaftH = size * 0.34f;
        renderer.DrawRect(
            new Vector2(pos.X + size * 0.58f, pos.Y + size * 0.18f),
            new Vector2(shaftW, shaftH),
            primary);

        float head = size * 0.16f;
        renderer.DrawRect(new Vector2(pos.X + size * 0.66f, pos.Y + size * 0.12f), new Vector2(head, head * 0.55f), accent);
        renderer.DrawRect(new Vector2(pos.X + size * 0.74f, pos.Y + size * 0.20f), new Vector2(head * 0.7f, head * 0.55f), accent);

        float ring = size * 0.34f;
        renderer.DrawRect(
            new Vector2(pos.X + size * 0.10f, pos.Y + size * 0.42f),
            new Vector2(ring, ring * 0.12f),
            primary);
        renderer.DrawRect(
            new Vector2(pos.X + size * 0.22f, pos.Y + size * 0.34f),
            new Vector2(ring * 0.12f, ring),
            primary);

        float core = size * 0.10f;
        renderer.DrawRect(
            new Vector2(pos.X + size * 0.22f, pos.Y + size * 0.50f),
            new Vector2(core, core),
            Brighten(accent, 1.15f));
    }

    private static void DrawStancePassive(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float bodyW = size * 0.48f;
        float bodyH = size * 0.44f;
        float bodyX = pos.X + (size - bodyW) * 0.5f;
        float bodyY = pos.Y + size * 0.30f;
        renderer.DrawRect(new Vector2(bodyX, bodyY), new Vector2(bodyW, bodyH), primary);

        float capW = size * 0.34f;
        float capH = size * 0.12f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - capW) * 0.5f, pos.Y + size * 0.18f),
            new Vector2(capW, capH),
            accent);

        float barW = size * 0.28f;
        float barH = size * 0.08f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - barW) * 0.5f, pos.Y + size * 0.48f),
            new Vector2(barW, barH),
            Brighten(accent, 1.1f));
    }

    private static void DrawStanceDefensive(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float bodyW = size * 0.50f;
        float bodyH = size * 0.46f;
        float bodyX = pos.X + (size - bodyW) * 0.5f;
        float bodyY = pos.Y + size * 0.28f;
        renderer.DrawRect(new Vector2(bodyX, bodyY), new Vector2(bodyW, bodyH), primary);

        float capW = size * 0.36f;
        float capH = size * 0.12f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - capW) * 0.5f, pos.Y + size * 0.16f),
            new Vector2(capW, capH),
            accent);

        float crestW = size * 0.14f;
        float crestH = size * 0.20f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - crestW) * 0.5f, pos.Y + size * 0.40f),
            new Vector2(crestW, crestH),
            Brighten(accent, 1.15f));

        float point = size * 0.12f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - point) * 0.5f, pos.Y + size * 0.66f),
            new Vector2(point, point * 0.7f),
            Darken(primary, 0.9f));
    }

    private static void DrawStanceAggressive(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float bladeW = size * 0.12f;
        float bladeH = size * 0.46f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - bladeW) * 0.5f, pos.Y + size * 0.12f),
            new Vector2(bladeW, bladeH),
            accent);

        float guardW = size * 0.34f;
        float guardH = size * 0.10f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - guardW) * 0.5f, pos.Y + size * 0.50f),
            new Vector2(guardW, guardH),
            primary);

        float hiltW = size * 0.14f;
        float hiltH = size * 0.16f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - hiltW) * 0.5f, pos.Y + size * 0.60f),
            new Vector2(hiltW, hiltH),
            Darken(primary, 0.85f));
    }

    private static void DrawFormationLine(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float unit = size * 0.12f;
        float gap = size * 0.08f;
        float y = pos.Y + size * 0.44f;
        float startX = pos.X + (size - (4f * unit + 3f * gap)) * 0.5f;
        for (int i = 0; i < 4; i++)
        {
            float x = startX + i * (unit + gap);
            renderer.DrawRect(new Vector2(x, y), new Vector2(unit, unit), i % 2 == 0 ? accent : primary);
        }
    }

    private static void DrawFormationWedge(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float unit = size * 0.14f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - unit) * 0.5f, pos.Y + size * 0.16f),
            new Vector2(unit, unit),
            accent);

        float wingY = pos.Y + size * 0.38f;
        renderer.DrawRect(new Vector2(pos.X + size * 0.22f, wingY), new Vector2(unit, unit), primary);
        renderer.DrawRect(new Vector2(pos.X + size * 0.64f, wingY), new Vector2(unit, unit), primary);

        float tailY = pos.Y + size * 0.60f;
        renderer.DrawRect(new Vector2(pos.X + size * 0.14f, tailY), new Vector2(unit, unit), Darken(primary, 0.9f));
        renderer.DrawRect(new Vector2(pos.X + size * 0.72f, tailY), new Vector2(unit, unit), Darken(primary, 0.9f));
    }

    private static void DrawFormationColumn(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float unit = size * 0.12f;
        float gap = size * 0.08f;
        float x = pos.X + (size - unit) * 0.5f;
        float startY = pos.Y + size * 0.14f;
        for (int i = 0; i < 4; i++)
        {
            float y = startY + i * (unit + gap);
            renderer.DrawRect(new Vector2(x, y), new Vector2(unit, unit), i == 0 ? accent : primary);
        }
    }

    private static void DrawFormationBox(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float unit = size * 0.12f;
        float inset = size * 0.18f;
        float topY = pos.Y + inset;
        float bottomY = pos.Y + size - inset - unit;
        float leftX = pos.X + inset;
        float rightX = pos.X + size - inset - unit;
        renderer.DrawRect(new Vector2(leftX, topY), new Vector2(unit, unit), accent);
        renderer.DrawRect(new Vector2(rightX, topY), new Vector2(unit, unit), accent);
        renderer.DrawRect(new Vector2(leftX, bottomY), new Vector2(unit, unit), primary);
        renderer.DrawRect(new Vector2(rightX, bottomY), new Vector2(unit, unit), primary);
    }

    private static void DrawFormationScatter(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float unit = size * 0.11f;
        renderer.DrawRect(new Vector2(pos.X + size * 0.14f, pos.Y + size * 0.20f), new Vector2(unit, unit), accent);
        renderer.DrawRect(new Vector2(pos.X + size * 0.58f, pos.Y + size * 0.14f), new Vector2(unit, unit), primary);
        renderer.DrawRect(new Vector2(pos.X + size * 0.72f, pos.Y + size * 0.44f), new Vector2(unit, unit), accent);
        renderer.DrawRect(new Vector2(pos.X + size * 0.24f, pos.Y + size * 0.52f), new Vector2(unit, unit), primary);
        renderer.DrawRect(new Vector2(pos.X + size * 0.48f, pos.Y + size * 0.66f), new Vector2(unit, unit), Darken(accent, 0.9f));
    }

    private static void DrawBuild(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float headW = size * 0.30f;
        float headH = size * 0.18f;
        renderer.DrawRect(
            new Vector2(pos.X + size * 0.16f, pos.Y + size * 0.18f),
            new Vector2(headW, headH),
            accent);

        float handleW = size * 0.10f;
        float handleH = size * 0.44f;
        renderer.DrawRect(
            new Vector2(pos.X + size * 0.26f, pos.Y + size * 0.34f),
            new Vector2(handleW, handleH),
            primary);

        float jawW = size * 0.22f;
        float jawH = size * 0.10f;
        renderer.DrawRect(
            new Vector2(pos.X + size * 0.54f, pos.Y + size * 0.48f),
            new Vector2(jawW, jawH),
            primary);
        renderer.DrawRect(
            new Vector2(pos.X + size * 0.62f, pos.Y + size * 0.36f),
            new Vector2(jawH, jawW),
            Brighten(accent, 1.1f));
    }

    private static void DrawHarvest(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float node = size * 0.22f;
        renderer.DrawRect(
            new Vector2(pos.X + size * 0.56f, pos.Y + size * 0.16f),
            new Vector2(node, node),
            Brighten(accent, 1.15f));

        float pickW = size * 0.10f;
        float pickH = size * 0.46f;
        renderer.DrawRect(
            new Vector2(pos.X + size * 0.22f, pos.Y + size * 0.34f),
            new Vector2(pickW, pickH),
            primary);

        float pickHead = size * 0.18f;
        renderer.DrawRect(
            new Vector2(pos.X + size * 0.14f, pos.Y + size * 0.26f),
            new Vector2(pickHead, pickW),
            accent);
        renderer.DrawRect(
            new Vector2(pos.X + size * 0.30f, pos.Y + size * 0.22f),
            new Vector2(pickHead * 0.7f, pickW),
            Darken(accent, 0.9f));
    }

    private static void DrawUnitFriendly(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float core = size * 0.22f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - core) * 0.5f, pos.Y + (size - core) * 0.5f),
            new Vector2(core, core),
            accent);

        float wing = size * 0.16f;
        float wingH = size * 0.10f;
        renderer.DrawRect(new Vector2(pos.X + (size - wing) * 0.5f, pos.Y + size * 0.08f), new Vector2(wing, wingH), primary);
        renderer.DrawRect(new Vector2(pos.X + (size - wing) * 0.5f, pos.Y + size - size * 0.08f - wingH), new Vector2(wing, wingH), primary);
        renderer.DrawRect(new Vector2(pos.X + size * 0.08f, pos.Y + (size - wingH) * 0.5f), new Vector2(wingH, wing), primary);
        renderer.DrawRect(new Vector2(pos.X + size - size * 0.08f - wingH, pos.Y + (size - wing) * 0.5f), new Vector2(wingH, wing), primary);
    }

    private static void DrawUnitHostile(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float chevW = size * 0.52f;
        float chevH = size * 0.14f;
        float chevX = pos.X + (size - chevW) * 0.5f;
        float chevY = pos.Y + size * 0.18f;
        renderer.DrawRect(new Vector2(chevX, chevY), new Vector2(chevW * 0.42f, chevH), accent);
        renderer.DrawRect(new Vector2(chevX + chevW * 0.58f, chevY), new Vector2(chevW * 0.42f, chevH), accent);
        renderer.DrawRect(new Vector2(chevX + chevW * 0.36f, chevY + chevH * 0.55f), new Vector2(chevW * 0.28f, chevH), primary);

        float baseW = size * 0.34f;
        float baseH = size * 0.12f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - baseW) * 0.5f, pos.Y + size * 0.62f),
            new Vector2(baseW, baseH),
            primary);
    }

    private static void DrawUnitNeutral(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float core = size * 0.20f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - core) * 0.5f, pos.Y + (size - core) * 0.5f),
            new Vector2(core, core),
            Darken(primary, 0.85f));

        float barW = size * 0.44f;
        float barH = size * 0.08f;
        renderer.DrawRect(new Vector2(pos.X + (size - barW) * 0.5f, pos.Y + size * 0.34f), new Vector2(barW, barH), accent);
        renderer.DrawRect(new Vector2(pos.X + (size - barW) * 0.5f, pos.Y + size * 0.58f), new Vector2(barW, barH), accent);

        float wing = size * 0.12f;
        renderer.DrawRect(new Vector2(pos.X + (size - wing) * 0.5f, pos.Y + size * 0.10f), new Vector2(wing, wing * 0.55f), primary);
        renderer.DrawRect(new Vector2(pos.X + (size - wing) * 0.5f, pos.Y + size * 0.74f), new Vector2(wing, wing * 0.55f), primary);
    }

    private static void DrawUnitHarvestable(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float node = size * 0.18f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - node) * 0.5f, pos.Y + size * 0.14f),
            new Vector2(node, node),
            Brighten(accent, 1.12f));

        float wing = size * 0.14f;
        float wingH = size * 0.09f;
        renderer.DrawRect(new Vector2(pos.X + (size - wing) * 0.5f, pos.Y + size * 0.40f), new Vector2(wing, wingH), primary);
        renderer.DrawRect(new Vector2(pos.X + size * 0.12f, pos.Y + (size - wing) * 0.5f), new Vector2(wingH, wing), primary);
        renderer.DrawRect(new Vector2(pos.X + size - size * 0.12f - wingH, pos.Y + (size - wing) * 0.5f), new Vector2(wingH, wing), primary);
        renderer.DrawRect(new Vector2(pos.X + (size - wing) * 0.5f, pos.Y + size * 0.72f), new Vector2(wing, wingH), primary);
    }

    private static void DrawUnitScenery(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float frame = size * 0.56f;
        float frameX = pos.X + (size - frame) * 0.5f;
        float frameY = pos.Y + (size - frame) * 0.5f;
        float thick = size * 0.08f;

        renderer.DrawRect(new Vector2(frameX, frameY), new Vector2(frame, thick), primary);
        renderer.DrawRect(new Vector2(frameX, frameY + frame - thick), new Vector2(frame, thick), primary);
        renderer.DrawRect(new Vector2(frameX, frameY), new Vector2(thick, frame), primary);
        renderer.DrawRect(new Vector2(frameX + frame - thick, frameY), new Vector2(thick, frame), primary);

        float core = size * 0.12f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - core) * 0.5f, pos.Y + (size - core) * 0.5f),
            new Vector2(core, core),
            accent);
    }

    private static void DrawStatHP(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float trackW = size * 0.78f;
        float trackH = size * 0.18f;
        float trackX = pos.X + (size - trackW) * 0.5f;
        float trackY = pos.Y + size * 0.42f;
        renderer.DrawRect(new Vector2(trackX, trackY), new Vector2(trackW, trackH), Darken(primary, 0.7f));

        float fillW = trackW * 0.62f;
        renderer.DrawRect(new Vector2(trackX, trackY), new Vector2(fillW, trackH), accent);

        float cap = size * 0.10f;
        renderer.DrawRect(new Vector2(trackX - cap * 0.35f, trackY - cap * 0.25f), new Vector2(cap, cap * 0.55f), primary);
    }

    private static void DrawStatShield(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float bodyW = size * 0.44f;
        float bodyH = size * 0.34f;
        float bodyX = pos.X + (size - bodyW) * 0.5f;
        float bodyY = pos.Y + size * 0.34f;
        renderer.DrawRect(new Vector2(bodyX, bodyY), new Vector2(bodyW, bodyH), primary);

        float capW = size * 0.30f;
        float capH = size * 0.12f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - capW) * 0.5f, pos.Y + size * 0.22f),
            new Vector2(capW, capH),
            accent);

        float plus = size * 0.10f;
        renderer.DrawRect(new Vector2(pos.X + (size - plus) * 0.5f, pos.Y + size * 0.44f), new Vector2(plus, plus * 0.35f), Brighten(accent, 1.1f));
        renderer.DrawRect(new Vector2(pos.X + (size - plus * 0.35f) * 0.5f, pos.Y + size * 0.40f), new Vector2(plus * 0.35f, plus), Brighten(accent, 1.1f));
    }

    private static void DrawStatArmor(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float plateW = size * 0.56f;
        float plateH = size * 0.12f;
        float plateX = pos.X + (size - plateW) * 0.5f;
        renderer.DrawRect(new Vector2(plateX, pos.Y + size * 0.22f), new Vector2(plateW, plateH), accent);
        renderer.DrawRect(new Vector2(plateX + plateW * 0.08f, pos.Y + size * 0.38f), new Vector2(plateW * 0.84f, plateH), primary);
        renderer.DrawRect(new Vector2(plateX + plateW * 0.16f, pos.Y + size * 0.54f), new Vector2(plateW * 0.68f, plateH), Darken(primary, 0.88f));

        float rivet = size * 0.08f;
        renderer.DrawRect(new Vector2(plateX + plateW * 0.08f, pos.Y + size * 0.30f), new Vector2(rivet, rivet), Brighten(accent, 1.08f));
    }

    private static void DrawStatCargo(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float box = size * 0.50f;
        float boxX = pos.X + (size - box) * 0.5f;
        float boxY = pos.Y + size * 0.30f;
        renderer.DrawRect(new Vector2(boxX, boxY), new Vector2(box, box), Darken(primary, 0.78f));

        float lidH = size * 0.10f;
        renderer.DrawRect(new Vector2(boxX, boxY - lidH * 0.55f), new Vector2(box, lidH), accent);

        float strapW = size * 0.08f;
        renderer.DrawRect(new Vector2(boxX + box * 0.30f, boxY), new Vector2(strapW, box), primary);
        renderer.DrawRect(new Vector2(boxX + box * 0.62f, boxY), new Vector2(strapW, box), primary);
    }

    private static void DrawStatHarvest(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float pickW = size * 0.10f;
        float pickH = size * 0.44f;
        renderer.DrawRect(new Vector2(pos.X + size * 0.34f, pos.Y + size * 0.30f), new Vector2(pickW, pickH), primary);

        float head = size * 0.16f;
        renderer.DrawRect(new Vector2(pos.X + size * 0.26f, pos.Y + size * 0.24f), new Vector2(head, pickW), accent);
        renderer.DrawRect(new Vector2(pos.X + size * 0.40f, pos.Y + size * 0.20f), new Vector2(head * 0.65f, pickW), Darken(accent, 0.9f));

        float node = size * 0.14f;
        renderer.DrawRect(new Vector2(pos.X + size * 0.58f, pos.Y + size * 0.16f), new Vector2(node, node), Brighten(accent, 1.12f));
    }

    private static void DrawHullMilitary(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float cx = pos.X + size * 0.5f;
        float cy = pos.Y + size * 0.5f;
        float arm = size * 0.30f;
        float thick = size * 0.10f;

        renderer.DrawRect(new Vector2(cx - arm, cy - thick * 0.5f), new Vector2(arm * 2f, thick), accent);
        renderer.DrawRect(new Vector2(cx - thick * 0.5f, cy - arm), new Vector2(thick, arm * 2f), accent);

        float ring = size * 0.22f;
        float ringPad = (size - ring) * 0.5f;
        renderer.DrawRect(pos + new Vector2(ringPad, ringPad), new Vector2(ring, ring), Darken(primary, 0.82f));

        float core = size * 0.10f;
        renderer.DrawRect(
            new Vector2(cx - core * 0.5f, cy - core * 0.5f),
            new Vector2(core, core),
            Brighten(accent, 1.08f));
    }

    private static void DrawHullEngineering(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float hub = size * 0.26f;
        var hubPos = pos + new Vector2((size - hub) * 0.5f, (size - hub) * 0.5f);
        renderer.DrawRect(hubPos, new Vector2(hub, hub), Darken(primary, 0.82f));

        float toothW = size * 0.14f;
        float toothH = size * 0.20f;
        float cx = pos.X + (size - toothW) * 0.5f;
        renderer.DrawRect(new Vector2(cx, pos.Y + size * 0.06f), new Vector2(toothW, toothH), accent);
        renderer.DrawRect(new Vector2(cx, pos.Y + size - size * 0.06f - toothH), new Vector2(toothW, toothH), accent);
        renderer.DrawRect(new Vector2(pos.X + size * 0.06f, pos.Y + (size - toothW) * 0.5f), new Vector2(toothH, toothW), accent);
        renderer.DrawRect(new Vector2(pos.X + size - size * 0.06f - toothH, pos.Y + (size - toothW) * 0.5f), new Vector2(toothH, toothW), accent);

        float core = size * 0.10f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - core) * 0.5f, pos.Y + (size - core) * 0.5f),
            new Vector2(core, core),
            Brighten(accent, 1.1f));
    }

    private static void DrawHullPolitical(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float star = size * 0.30f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - star) * 0.5f, pos.Y + size * 0.10f),
            new Vector2(star, star * 0.82f),
            accent);

        float chevW = size * 0.48f;
        float chevH = size * 0.12f;
        float chevX = pos.X + (size - chevW) * 0.5f;
        float chevY = pos.Y + size * 0.58f;
        renderer.DrawRect(new Vector2(chevX, chevY), new Vector2(chevW * 0.40f, chevH), primary);
        renderer.DrawRect(new Vector2(chevX + chevW * 0.60f, chevY), new Vector2(chevW * 0.40f, chevH), primary);
        renderer.DrawRect(new Vector2(chevX + chevW * 0.34f, chevY - chevH * 0.50f), new Vector2(chevW * 0.32f, chevH), Darken(primary, 0.9f));
    }

    private static void DrawNavNewGame(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float chevW = size * 0.50f;
        float chevH = size * 0.16f;
        float x = pos.X + size * 0.26f;
        float y = pos.Y + (size - chevH * 3f) * 0.5f;
        renderer.DrawRect(new Vector2(x, y), new Vector2(chevW * 0.38f, chevH), accent);
        renderer.DrawRect(new Vector2(x + chevW * 0.08f, y + chevH * 0.82f), new Vector2(chevW * 0.62f, chevH), primary);
        renderer.DrawRect(new Vector2(x, y + chevH * 1.64f), new Vector2(chevW * 0.38f, chevH), accent);
    }

    private static void DrawNavSandbox(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float cell = size * 0.22f;
        float gap = size * 0.10f;
        float gridW = cell * 2f + gap;
        float startX = pos.X + (size - gridW) * 0.5f;
        float startY = pos.Y + (size - gridW) * 0.5f;
        renderer.DrawRect(new Vector2(startX, startY), new Vector2(cell, cell), accent);
        renderer.DrawRect(new Vector2(startX + cell + gap, startY), new Vector2(cell, cell), primary);
        renderer.DrawRect(new Vector2(startX, startY + cell + gap), new Vector2(cell, cell), primary);
        renderer.DrawRect(new Vector2(startX + cell + gap, startY + cell + gap), new Vector2(cell, cell), Darken(accent, 0.9f));
    }

    private static void DrawNavMultiplayer(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float node = size * 0.20f;
        float leftX = pos.X + size * 0.14f;
        float rightX = pos.X + size * 0.66f;
        float nodeY = pos.Y + (size - node) * 0.5f;
        renderer.DrawRect(new Vector2(leftX, nodeY), new Vector2(node, node), accent);
        renderer.DrawRect(new Vector2(rightX, nodeY), new Vector2(node, node), accent);
        float linkY = pos.Y + size * 0.46f;
        float linkH = size * 0.08f;
        renderer.DrawRect(new Vector2(leftX + node, linkY), new Vector2(rightX - leftX - node, linkH), primary);
    }

    private static void DrawNavContinue(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float ring = size * 0.40f;
        float ringX = pos.X + size * 0.10f;
        float ringY = pos.Y + (size - ring) * 0.5f;
        float thick = size * 0.08f;
        renderer.DrawRect(new Vector2(ringX, ringY), new Vector2(ring, thick), primary);
        renderer.DrawRect(new Vector2(ringX, ringY + ring - thick), new Vector2(ring, thick), primary);
        renderer.DrawRect(new Vector2(ringX, ringY), new Vector2(thick, ring), primary);
        renderer.DrawRect(new Vector2(ringX + ring - thick, ringY), new Vector2(thick, ring * 0.55f), primary);

        float handW = size * 0.06f;
        float handH = size * 0.16f;
        renderer.DrawRect(new Vector2(ringX + ring * 0.42f, ringY + ring * 0.30f), new Vector2(handW, handH), accent);

        float arrowW = size * 0.18f;
        float arrowH = size * 0.10f;
        renderer.DrawRect(new Vector2(pos.X + size * 0.62f, pos.Y + size * 0.44f), new Vector2(arrowW, arrowH), accent);
        renderer.DrawRect(new Vector2(pos.X + size * 0.72f, pos.Y + size * 0.36f), new Vector2(arrowH, arrowW), Brighten(accent, 1.1f));
    }

    private static void DrawNavResume(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float stemW = size * 0.12f;
        float stemH = size * 0.46f;
        float stemX = pos.X + size * 0.22f;
        float stemY = pos.Y + (size - stemH) * 0.5f;
        renderer.DrawRect(new Vector2(stemX, stemY), new Vector2(stemW, stemH), primary);

        float wingW = size * 0.34f;
        float wingH = size * 0.12f;
        float wingX = stemX + stemW * 0.55f;
        renderer.DrawRect(new Vector2(wingX, stemY), new Vector2(wingW, wingH), accent);
        renderer.DrawRect(new Vector2(wingX, stemY + stemH - wingH), new Vector2(wingW, wingH), accent);
        renderer.DrawRect(
            new Vector2(wingX + wingW * 0.42f, stemY + wingH * 0.55f),
            new Vector2(wingW * 0.72f, stemH - wingH * 1.1f),
            Brighten(accent, 1.08f));
    }

    private static void DrawNavSave(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float disk = size * 0.30f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - disk) * 0.5f, pos.Y + size * 0.14f),
            new Vector2(disk, disk * 0.72f),
            accent);

        float labelW = size * 0.18f;
        float labelH = size * 0.06f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - labelW) * 0.5f, pos.Y + size * 0.30f),
            new Vector2(labelW, labelH),
            Brighten(accent, 1.08f));

        float trayW = size * 0.56f;
        float trayH = size * 0.12f;
        float trayX = pos.X + (size - trayW) * 0.5f;
        float trayY = pos.Y + size * 0.58f;
        renderer.DrawRect(new Vector2(trayX, trayY), new Vector2(trayW, trayH), primary);

        float arrowW = size * 0.10f;
        float arrowH = size * 0.18f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - arrowW) * 0.5f, pos.Y + size * 0.38f),
            new Vector2(arrowW, arrowH),
            primary);
        renderer.DrawRect(
            new Vector2(pos.X + (size - arrowH) * 0.5f, pos.Y + size * 0.50f),
            new Vector2(arrowH, arrowW * 0.75f),
            Brighten(primary, 1.1f));
    }

    private static void DrawNavLoadGame(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float trayW = size * 0.56f;
        float trayH = size * 0.14f;
        float trayX = pos.X + (size - trayW) * 0.5f;
        float trayY = pos.Y + size * 0.58f;
        renderer.DrawRect(new Vector2(trayX, trayY), new Vector2(trayW, trayH), primary);

        float slotW = size * 0.44f;
        float slotH = size * 0.10f;
        renderer.DrawRect(new Vector2(pos.X + (size - slotW) * 0.5f, trayY - slotH * 0.65f), new Vector2(slotW, slotH), Darken(primary, 0.82f));

        float disk = size * 0.30f;
        renderer.DrawRect(new Vector2(pos.X + (size - disk) * 0.5f, pos.Y + size * 0.16f), new Vector2(disk, disk * 0.72f), accent);

        float labelW = size * 0.18f;
        float labelH = size * 0.06f;
        renderer.DrawRect(new Vector2(pos.X + (size - labelW) * 0.5f, pos.Y + size * 0.30f), new Vector2(labelW, labelH), Brighten(accent, 1.08f));
    }

    private static void DrawNavShipDesigner(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float hullW = size * 0.46f;
        float hullH = size * 0.14f;
        float hullX = pos.X + size * 0.12f;
        float hullY = pos.Y + size * 0.52f;
        renderer.DrawRect(new Vector2(hullX, hullY), new Vector2(hullW * 0.55f, hullH), primary);
        renderer.DrawRect(new Vector2(hullX + hullW * 0.42f, hullY - hullH * 0.55f), new Vector2(hullW * 0.42f, hullH), accent);

        float markW = size * 0.10f;
        float markH = size * 0.22f;
        renderer.DrawRect(new Vector2(pos.X + size * 0.62f, pos.Y + size * 0.22f), new Vector2(markW, markH), accent);
        renderer.DrawRect(new Vector2(pos.X + size * 0.54f, pos.Y + size * 0.38f), new Vector2(markH, markW * 0.65f), Brighten(accent, 1.1f));
    }

    private static void DrawNavSettings(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float hub = size * 0.22f;
        var hubPos = pos + new Vector2((size - hub) * 0.5f, (size - hub) * 0.5f);
        renderer.DrawRect(hubPos, new Vector2(hub, hub), Darken(primary, 0.82f));

        float toothW = size * 0.12f;
        float toothH = size * 0.18f;
        float cx = pos.X + (size - toothW) * 0.5f;
        renderer.DrawRect(new Vector2(cx, pos.Y + size * 0.08f), new Vector2(toothW, toothH), accent);
        renderer.DrawRect(new Vector2(cx, pos.Y + size - size * 0.08f - toothH), new Vector2(toothW, toothH), accent);
        renderer.DrawRect(new Vector2(pos.X + size * 0.08f, pos.Y + (size - toothW) * 0.5f), new Vector2(toothH, toothW), accent);
        renderer.DrawRect(new Vector2(pos.X + size - size * 0.08f - toothH, pos.Y + (size - toothW) * 0.5f), new Vector2(toothH, toothW), accent);

        float core = size * 0.08f;
        renderer.DrawRect(
            new Vector2(pos.X + (size - core) * 0.5f, pos.Y + (size - core) * 0.5f),
            new Vector2(core, core),
            Brighten(accent, 1.1f));
    }

    private static void DrawNavQuit(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float doorW = size * 0.30f;
        float doorH = size * 0.52f;
        float doorX = pos.X + size * 0.14f;
        float doorY = pos.Y + (size - doorH) * 0.5f;
        float frame = size * 0.08f;
        renderer.DrawRect(new Vector2(doorX, doorY), new Vector2(frame, doorH), primary);
        renderer.DrawRect(new Vector2(doorX + doorW - frame, doorY), new Vector2(frame, doorH), primary);
        renderer.DrawRect(new Vector2(doorX, doorY + doorH - frame), new Vector2(doorW, frame), primary);

        float arrowW = size * 0.22f;
        float arrowH = size * 0.10f;
        renderer.DrawRect(new Vector2(pos.X + size * 0.56f, pos.Y + size * 0.44f), new Vector2(arrowW, arrowH), accent);
        renderer.DrawRect(new Vector2(pos.X + size * 0.66f, pos.Y + size * 0.36f), new Vector2(arrowH, arrowW * 0.85f), Brighten(accent, 1.1f));
    }

    private static void DrawNavBack(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float barW = size * 0.10f;
        float barH = size * 0.44f;
        renderer.DrawRect(
            new Vector2(pos.X + size * 0.62f, pos.Y + (size - barH) * 0.5f),
            new Vector2(barW, barH),
            primary);

        float chevW = size * 0.34f;
        float chevH = size * 0.12f;
        float x = pos.X + size * 0.18f;
        float y = pos.Y + (size - chevH * 3f) * 0.5f;
        renderer.DrawRect(new Vector2(x + chevW * 0.42f, y), new Vector2(chevW * 0.38f, chevH), accent);
        renderer.DrawRect(new Vector2(x, y + chevH * 0.82f), new Vector2(chevW * 0.62f, chevH), primary);
        renderer.DrawRect(new Vector2(x + chevW * 0.42f, y + chevH * 1.64f), new Vector2(chevW * 0.38f, chevH), accent);
    }

    private static void DrawNavStartMission(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float bodyW = size * 0.16f;
        float bodyH = size * 0.44f;
        float bodyX = pos.X + (size - bodyW) * 0.5f;
        float bodyY = pos.Y + size * 0.30f;
        renderer.DrawRect(new Vector2(bodyX, bodyY), new Vector2(bodyW, bodyH), primary);

        float nose = size * 0.20f;
        renderer.DrawRect(new Vector2(bodyX - nose * 0.18f, pos.Y + size * 0.14f), new Vector2(nose, nose * 0.55f), accent);

        float fin = size * 0.14f;
        renderer.DrawRect(new Vector2(bodyX - fin * 0.55f, bodyY + bodyH - fin * 0.35f), new Vector2(fin, fin * 0.55f), Darken(primary, 0.88f));
        renderer.DrawRect(new Vector2(bodyX + bodyW - fin * 0.45f, bodyY + bodyH - fin * 0.35f), new Vector2(fin, fin * 0.55f), Darken(primary, 0.88f));

        float flameH = size * 0.10f;
        renderer.DrawRect(new Vector2(bodyX + (bodyW - bodyW * 0.7f) * 0.5f, bodyY + bodyH), new Vector2(bodyW * 0.7f, flameH), Brighten(accent, 1.12f));
    }

    private static void DrawNavBriefing(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float panelW = size * 0.52f;
        float panelH = size * 0.56f;
        float panelX = pos.X + (size - panelW) * 0.5f;
        float panelY = pos.Y + size * 0.18f;
        renderer.DrawRect(new Vector2(panelX, panelY), new Vector2(panelW, panelH), Darken(primary, 0.78f));
        renderer.DrawRect(new Vector2(panelX, panelY), new Vector2(panelW, size * 0.10f), accent);

        float lineW = panelW * 0.68f;
        float lineH = size * 0.06f;
        float lineX = panelX + panelW * 0.16f;
        renderer.DrawRect(new Vector2(lineX, panelY + panelH * 0.34f), new Vector2(lineW, lineH), primary);
        renderer.DrawRect(new Vector2(lineX, panelY + panelH * 0.54f), new Vector2(lineW * 0.78f, lineH), Brighten(primary, 1.05f));
        renderer.DrawRect(new Vector2(lineX, panelY + panelH * 0.74f), new Vector2(lineW * 0.55f, lineH), Brighten(primary, 1.05f));
    }

    private static void DrawNavObjectives(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float barW = size * 0.52f;
        float barH = size * 0.08f;
        float mark = size * 0.10f;
        float startX = pos.X + size * 0.30f;
        float y0 = pos.Y + size * 0.22f;
        float gap = size * 0.14f;

        renderer.DrawRect(new Vector2(pos.X + size * 0.14f, y0 + barH * 0.15f), new Vector2(mark, mark), accent);
        renderer.DrawRect(new Vector2(startX, y0), new Vector2(barW, barH), primary);

        float y1 = y0 + gap + barH;
        renderer.DrawRect(new Vector2(pos.X + size * 0.14f, y1 + barH * 0.15f), new Vector2(mark, mark * 0.55f), Darken(accent, 0.9f));
        renderer.DrawRect(new Vector2(startX, y1), new Vector2(barW * 0.86f, barH), primary);

        float y2 = y1 + gap + barH;
        renderer.DrawRect(new Vector2(pos.X + size * 0.14f, y2 + barH * 0.15f), new Vector2(mark, mark * 0.55f), Darken(accent, 0.9f));
        renderer.DrawRect(new Vector2(startX, y2), new Vector2(barW * 0.72f, barH), primary);
    }

    private static void DrawNavCompleted(IUIRenderer renderer, Vector2 pos, float size, Vector4 primary, Vector4 accent)
    {
        float starW = size * 0.30f;
        float starH = size * 0.24f;
        float starX = pos.X + size * 0.14f;
        float starY = pos.Y + size * 0.16f;
        renderer.DrawRect(new Vector2(starX + starW * 0.28f, starY), new Vector2(starW * 0.44f, starH), accent);
        renderer.DrawRect(new Vector2(starX, starY + starH * 0.55f), new Vector2(starW * 0.36f, starH * 0.70f), Brighten(accent, 1.08f));
        renderer.DrawRect(new Vector2(starX + starW * 0.64f, starY + starH * 0.55f), new Vector2(starW * 0.36f, starH * 0.70f), Brighten(accent, 1.08f));

        float checkW = size * 0.22f;
        float checkH = size * 0.08f;
        float checkX = pos.X + size * 0.50f;
        float checkY = pos.Y + size * 0.52f;
        renderer.DrawRect(new Vector2(checkX, checkY), new Vector2(checkW, checkH), primary);
        renderer.DrawRect(new Vector2(checkX + checkW * 0.55f, checkY - checkH * 1.1f), new Vector2(checkH * 1.4f, checkH * 1.8f), Brighten(primary, 1.1f));
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