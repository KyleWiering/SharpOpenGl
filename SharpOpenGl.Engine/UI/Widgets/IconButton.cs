using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>Icon and label arrangement for <see cref="IconButton"/>.</summary>
public enum IconButtonLayout
{
    /// <summary>Gameplay command grid: icon in upper band, label below.</summary>
    IconAboveLabel,

    /// <summary>Title-stack navigation: icon column left of single-line label.</summary>
    IconLeftOfLabel,
}

/// <summary>
/// Command button with a procedural icon above a concise single-line label.
/// </summary>
public class IconButton : Widget, IUIButton
{
    /// <summary>Horizontal inset reserved on each side of the label band.</summary>
    public const float LabelPadding = 8f;

    /// <summary>Fraction of button height reserved for the icon slot (top band).</summary>
    public const float IconSlotFraction = 0.60f;

    /// <summary>Default logical icon size for 120×80 gameplay command buttons.</summary>
    public const float DefaultIconSize = 36f;

    /// <summary>Logical width reserved for the icon column in title-nav layout.</summary>
    public const float TitleNavIconColumnWidth = 44f;

    /// <summary>Default logical icon size for title-stack navigation buttons.</summary>
    public const float TitleNavIconSize = 24f;

    /// <summary>Left inset for the icon within the title-nav column.</summary>
    public const float TitleNavIconPadding = 10f;

    /// <summary>Default label font size for title-stack navigation buttons.</summary>
    public const float DefaultTitleNavFontSize = 20f;

    /// <summary>Procedural glyph drawn in the upper slot.</summary>
    public MenuIconKind Icon { get; set; }

    /// <summary>Icon and label arrangement; defaults to gameplay command grid layout.</summary>
    public IconButtonLayout Layout { get; set; } = IconButtonLayout.IconAboveLabel;

    /// <summary>Concise label shown in the lower band (max one line).</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Full command name shown in a tooltip on hover or keyboard focus.</summary>
    public string? TooltipHint { get; set; }

    /// <summary>Primary glyph tint; defaults to menu button text colour.</summary>
    public Vector4 IconPrimaryTint { get; set; } = MenuTheme.ButtonText;

    /// <summary>Accent glyph tint; defaults to menu button border colour.</summary>
    public Vector4 IconAccentTint { get; set; } = MenuTheme.ButtonBorder;

    /// <summary>Background colour in the normal state.</summary>
    public Vector4 NormalColor { get; set; } = new(0.2f, 0.2f, 0.3f, 1f);

    /// <summary>Background colour when the pointer is hovering over the button.</summary>
    public Vector4 HoverColor { get; set; } = new(0.35f, 0.35f, 0.5f, 1f);

    /// <summary>Background colour while the button is being pressed.</summary>
    public Vector4 PressedColor { get; set; } = new(0.15f, 0.15f, 0.25f, 1f);

    /// <summary>Background colour when the button is disabled.</summary>
    public Vector4 DisabledColor { get; set; } = new(0.14f, 0.14f, 0.18f, 0.8f);

    /// <summary>Background colour when <see cref="IsActive"/> is true (command selected).</summary>
    public Vector4 ActiveNormalColor { get; set; } = new(0.3f, 0.5f, 0.3f, 1f);

    /// <summary>Border colour in the normal state.</summary>
    public Vector4 BorderColor { get; set; } = new(0.6f, 0.6f, 0.8f, 1f);

    /// <summary>Border colour when hovered or keyboard-focused.</summary>
    public Vector4 HoverBorderColor { get; set; } = new(0.75f, 0.85f, 1f, 1f);

    /// <summary>Soft outer glow colour drawn behind the button when highlighted.</summary>
    public Vector4 HoverGlowColor { get; set; } = new(0.4f, 0.65f, 1f, 0.3f);

    /// <summary>Whether to draw the hover glow halo.</summary>
    public bool ShowHoverGlow { get; set; }

    /// <summary>Label text colour.</summary>
    public Vector4 TextColor { get; set; } = MenuTheme.ButtonText;

    /// <summary>Label text colour when the button is disabled.</summary>
    public Vector4 DisabledTextColor { get; set; } = MenuTheme.ButtonTextDisabled;

    /// <summary>Font size for the lower-band label in logical pixels.</summary>
    public float FontSize { get; set; } = 14f;

    /// <summary>Logical icon draw size; clamped to the icon slot when larger.</summary>
    public float IconSize { get; set; } = DefaultIconSize;

    /// <summary>Whether the pointer is currently hovering over this button.</summary>
    public bool IsHovered { get; protected set; }

    /// <summary>Whether the button is currently in a pressed state.</summary>
    public bool IsPressed { get; protected set; }

    /// <summary>Whether keyboard navigation has highlighted this button.</summary>
    public bool IsKeyboardFocused { get; set; }

    /// <summary>Whether the button can receive input.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Whether this command is the active selection (green-teal fill).</summary>
    public bool IsActive { get; set; }

    /// <summary>Invisible padding expanding the pointer hit rect beyond <see cref="Size"/>.</summary>
    public float HitPadding { get; set; }

    /// <summary>When true, hit rect is clamped to at least 44×44 logical pixels.</summary>
    public bool RequireMinimumHitExtent { get; set; }

    /// <summary>Minimum touch-target extent used with <see cref="RequireMinimumHitExtent"/>.</summary>
    public const float MinimumHitExtent = 44f;

    /// <summary>Raised when the pointer first enters the button bounds.</summary>
    public event Action? HoverEntered;

    /// <summary>Raised when the button is clicked (pointer tap within bounds).</summary>
    public event Action? Clicked;

    /// <summary>Apply title-screen navigation colours from <see cref="MenuTheme"/>.</summary>
    public static void ApplyMenuTheme(IconButton button, bool showGlow = true)
    {
        button.NormalColor = MenuTheme.ButtonNormal;
        button.HoverColor = MenuTheme.ButtonHover;
        button.PressedColor = MenuTheme.ButtonPressed;
        button.DisabledColor = MenuTheme.ButtonDisabled;
        button.BorderColor = MenuTheme.ButtonBorder;
        button.HoverBorderColor = MenuTheme.ButtonBorderHover;
        button.TextColor = MenuTheme.ButtonText;
        button.DisabledTextColor = MenuTheme.ButtonTextDisabled;
        button.HoverGlowColor = MenuTheme.ButtonGlow;
        button.ShowHoverGlow = showGlow;
        button.IconPrimaryTint = MenuTheme.ButtonText;
        button.IconAccentTint = MenuTheme.ButtonBorderHover;
    }

    /// <summary>Muted secondary navigation styling (Back / Cancel) without hover glow.</summary>
    public static void ApplySecondaryMenuTheme(IconButton button)
    {
        ApplyMenuTheme(button, showGlow: false);
        button.TextColor = MenuTheme.MutedTextColor;
        button.IconPrimaryTint = MenuTheme.MutedTextColor;
        button.IconAccentTint = new Vector4(0.45f, 0.52f, 0.65f, 0.85f);
        button.NormalColor = new Vector4(0.08f, 0.1f, 0.16f, 0.88f);
        button.BorderColor = new Vector4(0.28f, 0.35f, 0.5f, 0.65f);
        button.HoverBorderColor = new Vector4(0.4f, 0.52f, 0.72f, 0.85f);
    }

    /// <summary>
    /// Apply gameplay HUD button colours aligned with <see cref="MenuTheme"/> panel/button tones.
    /// Keeps the green-teal active-command fill for selected ship orders.
    /// </summary>
    public static void ApplyGameplayHudTheme(IconButton button, bool showGlow = false)
    {
        button.NormalColor = MenuTheme.ButtonNormal;
        button.HoverColor = MenuTheme.ButtonHover;
        button.PressedColor = MenuTheme.ButtonPressed;
        button.DisabledColor = MenuTheme.ButtonDisabled;
        button.ActiveNormalColor = new Vector4(0.3f, 0.5f, 0.3f, 1f);
        button.BorderColor = MenuTheme.ButtonBorder;
        button.HoverBorderColor = MenuTheme.ButtonBorderHover;
        button.TextColor = MenuTheme.ButtonText;
        button.DisabledTextColor = MenuTheme.ButtonTextDisabled;
        button.HoverGlowColor = MenuTheme.ButtonGlow;
        button.ShowHoverGlow = showGlow;
        button.IconPrimaryTint = MenuTheme.ButtonText;
        button.IconAccentTint = MenuTheme.ButtonBorder;
    }

    /// <inheritdoc/>
    public override TooltipContent? GetTooltipContent()
    {
        if (!IsHovered && !IsKeyboardFocused) return null;
        if (string.IsNullOrWhiteSpace(TooltipHint)) return null;
        return TooltipContent.FromCommandButton(Label, TooltipHint.Trim());
    }

    /// <summary>Programmatically activate the button (keyboard / accessibility).</summary>
    public void Activate()
    {
        if (!Visible || !IsEnabled) return;
        Clicked?.Invoke();
    }

    /// <inheritdoc/>
    public override bool HandlePointerTapped(
        Vector2 screenPoint, int button,
        Vector2 containerPosition, Vector2 containerSize)
    {
        if (!Visible || !IsEnabled) return false;

        if (!PointInHitArea(screenPoint, containerPosition, containerSize)) return false;

        NotifyClicked();
        return true;
    }

    /// <inheritdoc/>
    public override void UpdatePointerState(
        Vector2 pointerPosition, bool isPointerDown,
        Vector2 containerPosition, Vector2 containerSize)
    {
        bool wasHovered = IsHovered;
        IsHovered = IsEnabled && PointInHitArea(pointerPosition, containerPosition, containerSize);
        IsPressed = IsHovered && isPointerDown;
        if (IsHovered && !wasHovered)
            NotifyHoverEntered();
    }

    /// <summary>Whether a screen point is inside this button's hit area.</summary>
    protected virtual bool PointInHitArea(Vector2 point, Vector2 containerPosition, Vector2 containerSize)
    {
        var (pos, size) = Resolve(containerPosition, containerSize);
        float hitW = size.X + HitPadding * 2f;
        float hitH = size.Y + HitPadding * 2f;
        if (RequireMinimumHitExtent)
        {
            hitW = MathF.Max(hitW, MinimumHitExtent);
            hitH = MathF.Max(hitH, MinimumHitExtent);
        }

        float expandX = (hitW - size.X) * 0.5f;
        float expandY = (hitH - size.Y) * 0.5f;
        return point.X >= pos.X - expandX && point.X < pos.X + size.X + expandX
            && point.Y >= pos.Y - expandY && point.Y < pos.Y + size.Y + expandY;
    }

    /// <summary>Notify subscribers that the button was clicked.</summary>
    protected void NotifyClicked()
    {
        IsPressed = true;
        Clicked?.Invoke();
    }

    /// <summary>Notify subscribers that the pointer entered the button.</summary>
    protected void NotifyHoverEntered() => HoverEntered?.Invoke();

    /// <inheritdoc/>
    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        bool highlighted = IsHovered || IsKeyboardFocused;
        Vector4 baseNormal = IsActive ? ActiveNormalColor : NormalColor;
        Vector4 bg = !IsEnabled
            ? DisabledColor
            : IsPressed
                ? PressedColor
                : highlighted
                    ? HoverColor
                    : baseNormal;
        Vector4 border = !IsEnabled
            ? BorderColor with { W = BorderColor.W * 0.45f }
            : highlighted
                ? HoverBorderColor
                : BorderColor;
        Vector4 labelColor = IsEnabled ? TextColor : DisabledTextColor;

        if (ShowHoverGlow && IsEnabled && highlighted)
        {
            const float glowPad = 4f;
            renderer.DrawRect(
                position - new Vector2(glowPad),
                size + new Vector2(glowPad * 2f),
                HoverGlowColor);
        }

        renderer.DrawRect(position, size, bg);
        renderer.DrawRectOutline(position, size, border);

        ResolveIconTints(highlighted, out Vector4 primaryTint, out Vector4 accentTint);
        DrawIcon(renderer, position, size, primaryTint, accentTint);
        DrawLabel(renderer, position, size, labelColor);
    }

    private void ResolveIconTints(bool highlighted, out Vector4 primaryTint, out Vector4 accentTint)
    {
        primaryTint = IconPrimaryTint;
        accentTint = IconAccentTint;

        if (!IsEnabled)
        {
            primaryTint = Darken(primaryTint, 0.55f) with { W = primaryTint.W * 0.65f };
            accentTint = Darken(accentTint, 0.55f) with { W = accentTint.W * 0.65f };
            return;
        }

        if (IsPressed)
        {
            primaryTint = Darken(primaryTint, 0.82f);
            accentTint = Darken(accentTint, 0.82f);
        }
        else if (highlighted)
        {
            primaryTint = Brighten(primaryTint, 1.12f);
            accentTint = Brighten(accentTint, 1.18f);
        }
        else if (IsActive)
        {
            accentTint = Brighten(accentTint, 1.25f);
        }
        else
        {
            primaryTint = Darken(primaryTint, 0.92f);
        }
    }

    private void DrawIcon(IUIRenderer renderer, Vector2 position, Vector2 size, Vector4 primaryTint, Vector4 accentTint)
    {
        if (Layout == IconButtonLayout.IconLeftOfLabel)
        {
            float drawSize = IconSize == DefaultIconSize ? TitleNavIconSize : IconSize;
            float maxIcon = MathF.Min(TitleNavIconColumnWidth - TitleNavIconPadding, size.Y * 0.82f);
            drawSize = MathF.Min(drawSize, maxIcon);
            if (drawSize <= 0f) return;

            var iconPos = new Vector2(
                position.X + TitleNavIconPadding,
                position.Y + (size.Y - drawSize) * 0.5f);
            MenuIconDrawing.Draw(renderer, Icon, iconPos, drawSize, primaryTint, accentTint);
            return;
        }

        float iconSlotHeight = size.Y * IconSlotFraction;
        float maxCommandIcon = MathF.Min(size.X * 0.72f, iconSlotHeight * 0.88f);
        float commandDrawSize = MathF.Min(IconSize, maxCommandIcon);
        if (maxCommandIcon >= MenuIconDrawing.MinimumSize)
            commandDrawSize = MathF.Max(commandDrawSize, MenuIconDrawing.MinimumSize);
        commandDrawSize = MathF.Min(commandDrawSize, maxCommandIcon);
        if (commandDrawSize <= 0f) return;

        var commandIconPos = new Vector2(
            position.X + (size.X - commandDrawSize) * 0.5f,
            position.Y + (iconSlotHeight - commandDrawSize) * 0.5f);

        MenuIconDrawing.Draw(renderer, Icon, commandIconPos, commandDrawSize, primaryTint, accentTint);
    }

    private void DrawLabel(IUIRenderer renderer, Vector2 position, Vector2 size, Vector4 labelColor)
    {
        if (string.IsNullOrEmpty(Label)) return;

        float physicalScale = MathF.Max(renderer.ScaleToPhysical(1f), 0.001f);
        float preferredPhysical = renderer.ResolveFontSize(FontSize);
        float minPhysical = ScaledUIRenderer.MinPhysicalFontSize;

        if (Layout == IconButtonLayout.IconLeftOfLabel)
        {
            float labelLeft = position.X + TitleNavIconColumnWidth;
            float labelWidthLogical = MathF.Max(0f, size.X - TitleNavIconColumnWidth - LabelPadding);
            float maxWidth = renderer.ScaleToPhysical(labelWidthLogical);
            float fittedPhysical = UIFontMetrics.FitFontSize(Label, preferredPhysical, maxWidth, minPhysical);
            float logicalDrawSize = fittedPhysical / physicalScale;

            string displayText = Label;
            if (UIFontMetrics.MeasureTextWidth(Label, fittedPhysical) > maxWidth)
                displayText = UITextDrawing.TruncateWithEllipsis(Label, maxWidth, fittedPhysical);

            float lineHeight = fittedPhysical * UITextDrawing.LineHeightFactor / physicalScale;
            Vector2 textPos = new(labelLeft, position.Y + (size.Y - lineHeight) * 0.5f);
            renderer.DrawText(displayText, textPos, logicalDrawSize, labelColor);
            return;
        }

        float labelBandTop = position.Y + size.Y * IconSlotFraction;
        float labelBandHeight = size.Y * (1f - IconSlotFraction);
        float commandMaxWidth = renderer.ScaleToPhysical(size.X - LabelPadding * 2f);
        float commandFittedPhysical = UIFontMetrics.FitFontSize(Label, preferredPhysical, commandMaxWidth, minPhysical);
        float commandLogicalDrawSize = commandFittedPhysical / physicalScale;

        string commandDisplayText = Label;
        if (UIFontMetrics.MeasureTextWidth(Label, commandFittedPhysical) > commandMaxWidth)
            commandDisplayText = UITextDrawing.TruncateWithEllipsis(Label, commandMaxWidth, commandFittedPhysical);

        float commandTextWidthPhysical = UIFontMetrics.MeasureTextWidth(commandDisplayText, commandFittedPhysical);
        float commandTextWidthLogical = commandTextWidthPhysical / physicalScale;
        float commandLineHeight = commandFittedPhysical * UITextDrawing.LineHeightFactor / physicalScale;
        float commandTextY = labelBandTop + (labelBandHeight - commandLineHeight) * 0.5f;
        Vector2 commandTextPos = new(
            position.X + (size.X - commandTextWidthLogical) * 0.5f,
            commandTextY);

        renderer.DrawText(commandDisplayText, commandTextPos, commandLogicalDrawSize, labelColor);
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