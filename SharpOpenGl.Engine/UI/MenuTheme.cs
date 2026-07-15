using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Engine.UI;

/// <summary>
/// Shared colours and typography for title-screen style menus.
/// </summary>
public static class MenuTheme
{
    public const string GameTitle = "SHARP OPENGL";
    public const string GameSubtitle = "Command the void. Forge your fleet.";

    public static readonly Vector4 TitleColor = new(0.55f, 0.85f, 1f, 1f);
    /// <summary>High-contrast subtitle text readable over animated starfield backgrounds.</summary>
    public static readonly Vector4 SubtitleColor = new(0.86f, 0.91f, 0.98f, 1f);

    /// <summary>Semi-opaque scrim behind subtitle labels on starfield screens.</summary>
    public static readonly Vector4 SubtitleScrimColor = new(0.04f, 0.06f, 0.12f, 0.78f);
    public static readonly Vector4 ScreenHeadingColor = new(0.55f, 0.85f, 1f, 1f);
    public static readonly Vector4 BodyTextColor = new(0.9f, 0.92f, 1f, 1f);
    public static readonly Vector4 MutedTextColor = new(0.65f, 0.72f, 0.82f, 1f);

    public static readonly Vector4 PanelBackground = new(0.06f, 0.08f, 0.14f, 0.82f);
    public static readonly Vector4 PanelBorder = new(0.28f, 0.42f, 0.72f, 0.9f);
    /// <summary>Dim scrim behind pause/save overlay cards.</summary>
    public static readonly Vector4 OverlayBackdrop = new(0f, 0f, 0f, 0.55f);

    public static readonly Vector4 ButtonNormal = new(0.1f, 0.14f, 0.24f, 0.95f);
    public static readonly Vector4 ButtonHover = new(0.16f, 0.26f, 0.46f, 1f);
    public static readonly Vector4 ButtonPressed = new(0.08f, 0.12f, 0.2f, 1f);
    public static readonly Vector4 ButtonDisabled = new(0.12f, 0.12f, 0.16f, 0.75f);
    public static readonly Vector4 ButtonBorder = new(0.35f, 0.5f, 0.78f, 0.85f);
    public static readonly Vector4 ButtonBorderHover = new(0.55f, 0.78f, 1f, 1f);
    public static readonly Vector4 ButtonGlow = new(0.35f, 0.6f, 1f, 0.35f);
    public static readonly Vector4 ButtonText = new(0.95f, 0.97f, 1f, 1f);
    public static readonly Vector4 ButtonTextDisabled = new(0.45f, 0.48f, 0.55f, 0.85f);

    public static readonly Vector4 StarfieldTop = new(0.02f, 0.03f, 0.08f, 1f);
    public static readonly Vector4 StarfieldBottom = new(0.04f, 0.06f, 0.14f, 1f);

    /// <summary>High-contrast planet labels on the galactic star map.</summary>
    public static readonly Vector4 StarMapLabelColor = new(0.95f, 0.97f, 1f, 1f);

    /// <summary>Semi-opaque scrim behind star-map planet labels.</summary>
    public static readonly Vector4 StarMapLabelScrimColor = new(0.04f, 0.06f, 0.12f, 0.84f);

    /// <summary>Muted label text for prerequisite-locked systems.</summary>
    public static readonly Vector4 StarMapLockedLabelColor = new(0.72f, 0.76f, 0.84f, 0.92f);

    /// <summary>Desaturated body tint applied to locked planet nodes.</summary>
    public static readonly Vector4 StarMapLockedBodyTint = new(0.38f, 0.42f, 0.5f, 0.65f);

    /// <summary>Selection ring accent for the active mission system.</summary>
    public static readonly Vector4 StarMapSelectionRingColor = new(0.55f, 0.85f, 1f, 0.95f);

    /// <summary>Dim dashed hyperlane color leading to locked systems.</summary>
    public static readonly Vector4 StarMapLockedLaneColor = new(0.45f, 0.5f, 0.62f, 0.3f);

    /// <summary>Human slot kind — cool accent fill.</summary>
    public static readonly Vector4 SlotHumanNormal = new(0.12f, 0.28f, 0.42f, 0.95f);

    /// <summary>Human slot kind border accent.</summary>
    public static readonly Vector4 SlotHumanBorder = new(0.45f, 0.78f, 1f, 0.95f);

    /// <summary>AI slot kind — warm amber fill.</summary>
    public static readonly Vector4 SlotAiNormal = new(0.28f, 0.18f, 0.08f, 0.95f);

    /// <summary>AI slot kind border accent.</summary>
    public static readonly Vector4 SlotAiBorder = new(0.95f, 0.62f, 0.28f, 0.9f);

    /// <summary>Empty slot kind — muted recessed fill.</summary>
    public static readonly Vector4 SlotEmptyNormal = new(0.1f, 0.11f, 0.14f, 0.75f);

    /// <summary>Empty slot kind border.</summary>
    public static readonly Vector4 SlotEmptyBorder = new(0.32f, 0.36f, 0.44f, 0.6f);

    /// <summary>Empty slot kind label text.</summary>
    public static readonly Vector4 SlotEmptyText = new(0.55f, 0.58f, 0.65f, 0.85f);

    /// <summary>Inactive race-cycle chevron styling.</summary>
    public static readonly Vector4 RaceToggleInactiveBorder = new(0.28f, 0.34f, 0.46f, 0.65f);

    public static void ApplyNavButton(Button button, bool showGlow = true)
    {
        button.NormalColor = ButtonNormal;
        button.HoverColor = ButtonHover;
        button.PressedColor = ButtonPressed;
        button.DisabledColor = ButtonDisabled;
        button.BorderColor = ButtonBorder;
        button.HoverBorderColor = ButtonBorderHover;
        button.TextColor = ButtonText;
        button.DisabledTextColor = ButtonTextDisabled;
        button.HoverGlowColor = ButtonGlow;
        button.ShowHoverGlow = showGlow;
    }

    public static void ApplyScreenTitle(Label label)
    {
        label.TextColor = ScreenHeadingColor;
    }

    public static void ApplyPanel(Panel panel)
    {
        panel.BackgroundColor = PanelBackground;
        panel.BorderColor = PanelBorder;
        panel.DrawBorder = true;
    }

    /// <summary>Applies per-kind fill/border colours to a slot kind toggle.</summary>
    public static void ApplySlotKindButton(Button button, MultiplayerSlotKind kind)
    {
        ApplyNavButton(button, showGlow: false);

        switch (kind)
        {
            case MultiplayerSlotKind.Human:
                button.NormalColor = SlotHumanNormal;
                button.HoverColor = new Vector4(0.18f, 0.34f, 0.5f, 1f);
                button.PressedColor = new Vector4(0.1f, 0.22f, 0.34f, 1f);
                button.BorderColor = SlotHumanBorder;
                button.HoverBorderColor = new Vector4(0.6f, 0.86f, 1f, 1f);
                button.TextColor = ButtonText;
                break;
            case MultiplayerSlotKind.Ai:
                button.NormalColor = SlotAiNormal;
                button.HoverColor = new Vector4(0.36f, 0.24f, 0.12f, 1f);
                button.PressedColor = new Vector4(0.22f, 0.14f, 0.06f, 1f);
                button.BorderColor = SlotAiBorder;
                button.HoverBorderColor = new Vector4(1f, 0.72f, 0.38f, 1f);
                button.TextColor = ButtonText;
                break;
            default:
                button.NormalColor = SlotEmptyNormal;
                button.HoverColor = new Vector4(0.14f, 0.16f, 0.2f, 0.9f);
                button.PressedColor = new Vector4(0.08f, 0.09f, 0.12f, 0.9f);
                button.BorderColor = SlotEmptyBorder;
                button.HoverBorderColor = new Vector4(0.42f, 0.48f, 0.58f, 0.85f);
                button.TextColor = SlotEmptyText;
                break;
        }
    }

    /// <summary>Applies active/inactive styling to race prev/next toggles.</summary>
    public static void ApplyRaceToggleButton(Button button, bool active, Vector4? raceAccent = null)
    {
        ApplyNavButton(button, showGlow: false);

        if (!active)
        {
            button.NormalColor = SlotEmptyNormal;
            button.BorderColor = RaceToggleInactiveBorder;
            button.TextColor = SlotEmptyText;
            return;
        }

        Vector4 accent = raceAccent ?? ButtonBorderHover;
        button.NormalColor = new Vector4(accent.X * 0.22f, accent.Y * 0.22f, accent.Z * 0.22f, 0.95f);
        button.HoverColor = new Vector4(accent.X * 0.32f, accent.Y * 0.32f, accent.Z * 0.32f, 1f);
        button.PressedColor = new Vector4(accent.X * 0.16f, accent.Y * 0.16f, accent.Z * 0.16f, 1f);
        button.BorderColor = accent;
        button.HoverBorderColor = new Vector4(
            MathF.Min(accent.X + 0.15f, 1f),
            MathF.Min(accent.Y + 0.15f, 1f),
            MathF.Min(accent.Z + 0.15f, 1f),
            1f);
        button.TextColor = ButtonText;
    }

    /// <summary>Resolves a race accent colour for multiplayer slot toggles.</summary>
    public static Vector4 ResolveRaceAccentColor(string raceId)
    {
        if (RaceVisualSchema.TryGetRace(raceId, out var race) && race.Palette.Accent.Length >= 3)
        {
            return new Vector4(
                race.Palette.Accent[0],
                race.Palette.Accent[1],
                race.Palette.Accent[2],
                0.95f);
        }

        return ButtonBorderHover;
    }
}