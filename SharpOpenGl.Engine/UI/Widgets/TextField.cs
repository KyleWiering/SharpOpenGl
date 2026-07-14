using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>
/// Single-line text input with keyboard editing and optional placeholder text.
/// </summary>
public sealed class TextField : Widget
{
    /// <summary>Minimum logical height for touch-friendly hit targets (browser parity).</summary>
    public const float MinTouchHeight = 44f;

    private const float TextPadding = 12f;
    private const int MaxLength = 64;

    /// <summary>Current text buffer.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Hint shown when <see cref="Value"/> is empty.</summary>
    public string Placeholder { get; set; } = string.Empty;

    /// <summary>Whether keyboard navigation has highlighted this field.</summary>
    public bool IsKeyboardFocused { get; set; }

    /// <summary>Background fill colour.</summary>
    public Vector4 BackgroundColor { get; set; } = MenuTheme.PanelBackground;

    /// <summary>Border colour in the normal state.</summary>
    public Vector4 BorderColor { get; set; } = MenuTheme.PanelBorder;

    /// <summary>Border colour when hovered or keyboard-focused.</summary>
    public Vector4 FocusBorderColor { get; set; } = MenuTheme.ButtonBorderHover;

    /// <summary>Text colour for entered content.</summary>
    public Vector4 TextColor { get; set; } = MenuTheme.BodyTextColor;

    /// <summary>Text colour for placeholder content.</summary>
    public Vector4 PlaceholderColor { get; set; } = MenuTheme.MutedTextColor;

    /// <summary>Font size in logical pixels.</summary>
    public float FontSize { get; set; } = 20f;

    /// <summary>Raised when the user presses Enter while the field is focused.</summary>
    public event Action? Submitted;

    /// <summary>Append a printable character when focused.</summary>
    public bool HandleChar(char c)
    {
        if (!IsKeyboardFocused || !Visible)
            return false;

        if (char.IsControl(c))
            return false;

        if (Value.Length >= MaxLength)
            return true;

        Value += c;
        return true;
    }

    /// <summary>Handle navigation and editing keys while focused.</summary>
    public bool HandleKey(UIKey key)
    {
        if (!IsKeyboardFocused || !Visible)
            return false;

        switch (key)
        {
            case UIKey.Backspace:
                if (Value.Length > 0)
                    Value = Value[..^1];
                return true;
            case UIKey.Enter:
                Submitted?.Invoke();
                return true;
            default:
                return false;
        }
    }

    /// <inheritdoc/>
    public override bool HandlePointerTapped(
        Vector2 screenPoint, int button,
        Vector2 containerPosition, Vector2 containerSize)
    {
        if (!Visible) return false;

        if (Contains(screenPoint, containerPosition, containerSize))
        {
            IsKeyboardFocused = true;
            return true;
        }

        if (IsKeyboardFocused)
            IsKeyboardFocused = false;

        return false;
    }

    /// <inheritdoc/>
    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        float drawHeight = MathF.Max(size.Y, MinTouchHeight);
        Vector2 drawSize = new(size.X, drawHeight);
        float yOffset = (size.Y - drawHeight) * 0.5f;
        Vector2 drawPos = position + new Vector2(0f, yOffset);

        bool highlighted = IsKeyboardFocused;
        Vector4 border = highlighted ? FocusBorderColor : BorderColor;

        renderer.DrawRect(drawPos, drawSize, BackgroundColor);
        renderer.DrawRectOutline(drawPos, drawSize, border);

        bool showPlaceholder = string.IsNullOrEmpty(Value) && !string.IsNullOrEmpty(Placeholder);
        string displayText = showPlaceholder ? Placeholder : Value;
        Vector4 color = showPlaceholder ? PlaceholderColor : TextColor;
        if (string.IsNullOrEmpty(displayText))
            return;

        float physicalScale = MathF.Max(renderer.ScaleToPhysical(1f), 0.001f);
        float maxWidth = renderer.ScaleToPhysical(drawSize.X - TextPadding * 2f);
        float preferredPhysical = renderer.ResolveFontSize(FontSize);
        float minPhysical = ScaledUIRenderer.MinPhysicalFontSize;
        float fittedPhysical = UIFontMetrics.FitFontSize(displayText, preferredPhysical, maxWidth, minPhysical);
        float logicalDrawSize = fittedPhysical / physicalScale;

        if (UIFontMetrics.MeasureTextWidth(displayText, fittedPhysical) > maxWidth)
            displayText = UITextDrawing.TruncateWithEllipsis(displayText, maxWidth, fittedPhysical);

        float textWidthPhysical = UIFontMetrics.MeasureTextWidth(displayText, fittedPhysical);
        float textWidthLogical = textWidthPhysical / physicalScale;
        float lineHeight = fittedPhysical * UITextDrawing.LineHeightFactor / physicalScale;

        Vector2 textPos = new(
            drawPos.X + TextPadding,
            drawPos.Y + (drawSize.Y - lineHeight) * 0.5f);

        renderer.DrawText(displayText, textPos, logicalDrawSize, color);
    }
}