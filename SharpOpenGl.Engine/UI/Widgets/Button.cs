using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>
/// A clickable button widget with a label and optional hover-state colour.
/// </summary>
public sealed class Button : Widget
{
    private const float TextPadding = 20f;
    private const float HitPaddingTop = 10f;
    private const float HitPaddingBottom = 4f;

    /// <summary>Text displayed on the button face.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Background colour in the normal state.</summary>
    public Vector4 NormalColor { get; set; } = new Vector4(0.2f, 0.2f, 0.3f, 1f);

    /// <summary>Background colour when the pointer is hovering over the button.</summary>
    public Vector4 HoverColor { get; set; } = new Vector4(0.35f, 0.35f, 0.5f, 1f);

    /// <summary>Background colour while the button is being pressed.</summary>
    public Vector4 PressedColor { get; set; } = new Vector4(0.15f, 0.15f, 0.25f, 1f);

    /// <summary>Background colour when the button is disabled.</summary>
    public Vector4 DisabledColor { get; set; } = new Vector4(0.14f, 0.14f, 0.18f, 0.8f);

    /// <summary>Border colour in the normal state.</summary>
    public Vector4 BorderColor { get; set; } = new Vector4(0.6f, 0.6f, 0.8f, 1f);

    /// <summary>Border colour when hovered or keyboard-focused.</summary>
    public Vector4 HoverBorderColor { get; set; } = new Vector4(0.75f, 0.85f, 1f, 1f);

    /// <summary>Soft outer glow colour drawn behind the button when highlighted.</summary>
    public Vector4 HoverGlowColor { get; set; } = new Vector4(0.4f, 0.65f, 1f, 0.3f);

    /// <summary>Whether to draw the hover glow halo.</summary>
    public bool ShowHoverGlow { get; set; }

    /// <summary>Text colour.</summary>
    public Vector4 TextColor { get; set; } = new Vector4(1f, 1f, 1f, 1f);

    /// <summary>Text colour when the button is disabled.</summary>
    public Vector4 DisabledTextColor { get; set; } = new Vector4(0.5f, 0.52f, 0.58f, 0.85f);

    /// <summary>Font size in logical pixels.</summary>
    public float FontSize { get; set; } = 20f;

    /// <summary>Whether the pointer is currently hovering over this button.</summary>
    public bool IsHovered { get; private set; }

    /// <summary>Whether the button is currently in a pressed state.</summary>
    public bool IsPressed { get; private set; }

    /// <summary>Whether keyboard navigation has highlighted this button.</summary>
    public bool IsKeyboardFocused { get; set; }

    /// <summary>Whether the button can receive input.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Raised when the pointer first enters the button bounds.</summary>
    public event Action? HoverEntered;

    /// <summary>Raised when the button is clicked (pointer tap within bounds).</summary>
    public event Action? Clicked;

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

        IsPressed = true;
        Clicked?.Invoke();
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
            HoverEntered?.Invoke();
    }

    private bool PointInHitArea(Vector2 point, Vector2 containerPosition, Vector2 containerSize)
    {
        var (pos, size) = Resolve(containerPosition, containerSize);
        return point.X >= pos.X && point.X < pos.X + size.X
            && point.Y >= pos.Y - HitPaddingTop && point.Y < pos.Y + size.Y + HitPaddingBottom;
    }
    /// <inheritdoc/>
    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        bool highlighted = IsHovered || IsKeyboardFocused;
        Vector4 bg = !IsEnabled
            ? DisabledColor
            : IsPressed
                ? PressedColor
                : highlighted
                    ? HoverColor
                    : NormalColor;
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

        if (string.IsNullOrEmpty(Label)) return;

        float physicalScale = MathF.Max(renderer.ScaleToPhysical(1f), 0.001f);
        float maxWidth = renderer.ScaleToPhysical(size.X - TextPadding);
        float preferredPhysical = renderer.ResolveFontSize(FontSize);
        float minPhysical = ScaledUIRenderer.MinPhysicalFontSize;
        float fittedPhysical = UIFontMetrics.FitFontSize(Label, preferredPhysical, maxWidth, minPhysical);
        float logicalDrawSize = fittedPhysical / physicalScale;

        var lines = UITextDrawing.WrapText(Label, maxWidth, fittedPhysical);
        float lineHeight = fittedPhysical * UITextDrawing.LineHeightFactor;
        float blockHeight = lines.Count * lineHeight;
        float startY = position.Y + (size.Y - blockHeight / physicalScale) * 0.5f;

        for (int i = 0; i < lines.Count; i++)
        {
            float textWidthPhysical = UIFontMetrics.MeasureTextWidth(lines[i], fittedPhysical);
            float textWidthLogical = textWidthPhysical / physicalScale;
            Vector2 textPos = new(
                position.X + (size.X - textWidthLogical) * 0.5f,
                startY + i * (lineHeight / physicalScale));
            renderer.DrawText(lines[i], textPos, logicalDrawSize, labelColor);
        }
    }
}