using OpenTK.Mathematics;
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
    public static readonly Vector4 SubtitleColor = new(0.62f, 0.72f, 0.88f, 0.9f);
    public static readonly Vector4 ScreenHeadingColor = new(0.55f, 0.85f, 1f, 1f);
    public static readonly Vector4 BodyTextColor = new(0.9f, 0.92f, 1f, 1f);
    public static readonly Vector4 MutedTextColor = new(0.65f, 0.72f, 0.82f, 1f);

    public static readonly Vector4 PanelBackground = new(0.06f, 0.08f, 0.14f, 0.82f);
    public static readonly Vector4 PanelBorder = new(0.28f, 0.42f, 0.72f, 0.9f);

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
}