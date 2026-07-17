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

    /// <summary>Horizontal scroll distance per wheel notch or programmatic step.</summary>
    public float ScrollStep { get; set; } = 48f;

    /// <summary>Current text buffer.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Hint shown when <see cref="Value"/> is empty.</summary>
    public string Placeholder { get; set; } = string.Empty;

    /// <summary>Whether keyboard navigation has highlighted this field.</summary>
    public bool IsKeyboardFocused { get; set; }

    /// <summary>Horizontal scroll offset in logical pixels (reveals obscured tail when increased).</summary>
    public float ScrollOffsetX { get; private set; }

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
        ScrollToEnd();
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
                {
                    Value = Value[..^1];
                    ClampScrollOffset();
                }
                return true;
            case UIKey.Enter:
                Submitted?.Invoke();
                return true;
            default:
                return false;
        }
    }

    /// <summary>Maximum horizontal scroll offset for the current display text and field size.</summary>
    public float MaxScrollOffset(Vector2 fieldSize, IUIRenderer renderer)
    {
        string displayText = ResolveDisplayText();
        if (string.IsNullOrEmpty(displayText))
            return 0f;

        float innerWidth = InnerTextWidth(fieldSize);
        float textWidth = MeasureDisplayTextWidth(displayText, renderer);
        if (textWidth <= innerWidth)
            return 0f;

        float physicalScale = MathF.Max(renderer.ScaleToPhysical(1f), 0.001f);
        float innerWidthPhysical = renderer.ScaleToPhysical(innerWidth);
        float fittedPhysical = renderer.ResolveFontSize(FontSize);
        int maxStartIndex = MaxScrollStartIndex(displayText, innerWidthPhysical, fittedPhysical);
        float maxScrollPhysical = maxStartIndex == 0
            ? 0f
            : UIFontMetrics.MeasureTextWidth(displayText[..maxStartIndex], fittedPhysical);

        return maxScrollPhysical / physicalScale;
    }

    /// <summary>Scroll horizontally by <paramref name="deltaX"/> logical pixels (positive reveals tail).</summary>
    public void ScrollHorizontalBy(float deltaX, Vector2 fieldSize, IUIRenderer renderer)
    {
        float max = MaxScrollOffset(fieldSize, renderer);
        ScrollOffsetX = Math.Clamp(ScrollOffsetX + deltaX, 0f, max);
    }

    /// <summary>Reveal the start of the current display text.</summary>
    public void ScrollToStart() => ScrollOffsetX = 0f;

    /// <summary>Reveal the end of the current display text.</summary>
    public void ScrollToEnd(Vector2 fieldSize, IUIRenderer renderer) =>
        ScrollOffsetX = MaxScrollOffset(fieldSize, renderer);

    /// <summary>Reveal the end using reference-resolution sizing (for tests and caret-follow).</summary>
    public void ScrollToEnd() => ScrollToEnd(Size, CreateMeasurementRenderer());

    /// <inheritdoc/>
    public override bool HandleScroll(
        Vector2 screenPoint, float deltaY,
        Vector2 containerPosition, Vector2 containerSize)
    {
        if (!IsKeyboardFocused || !Visible || MathF.Abs(deltaY) < 0.001f)
            return false;

        var (pos, size) = Resolve(containerPosition, containerSize);
        if (screenPoint.X < pos.X || screenPoint.X >= pos.X + size.X ||
            screenPoint.Y < pos.Y || screenPoint.Y >= pos.Y + size.Y)
            return false;

        ScrollHorizontalBy(deltaY > 0f ? ScrollStep : -ScrollStep, size, CreateMeasurementRenderer());
        return true;
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

        string displayText = ResolveDisplayText();
        if (string.IsNullOrEmpty(displayText))
        {
            ScrollOffsetX = 0f;
            return;
        }

        bool showPlaceholder = string.IsNullOrEmpty(Value) && !string.IsNullOrEmpty(Placeholder);
        Vector4 color = showPlaceholder ? PlaceholderColor : TextColor;

        float physicalScale = MathF.Max(renderer.ScaleToPhysical(1f), 0.001f);
        float innerWidthLogical = InnerTextWidth(drawSize);
        float innerWidthPhysical = renderer.ScaleToPhysical(innerWidthLogical);
        float fittedPhysical = renderer.ResolveFontSize(FontSize);
        float logicalDrawSize = fittedPhysical / physicalScale;
        float textWidthLogical = MeasureDisplayTextWidth(displayText, renderer);

        float maxScrollLogical = MaxScrollOffset(drawSize, renderer);
        ScrollOffsetX = Math.Clamp(ScrollOffsetX, 0f, maxScrollLogical);
        float scrollPhysical = renderer.ScaleToPhysical(ScrollOffsetX);

        float lineHeight = fittedPhysical * UITextDrawing.LineHeightFactor / physicalScale;
        Vector2 textPos = new(
            drawPos.X + TextPadding,
            drawPos.Y + (drawSize.Y - lineHeight) * 0.5f);

        string visibleText = SliceTextToViewport(displayText, scrollPhysical, innerWidthPhysical, fittedPhysical);
        renderer.DrawText(visibleText, textPos, logicalDrawSize, color);
    }

    private void ClampScrollOffset()
    {
        ScrollOffsetX = Math.Clamp(ScrollOffsetX, 0f, MaxScrollOffset(Size, CreateMeasurementRenderer()));
    }

    private string ResolveDisplayText()
    {
        bool showPlaceholder = string.IsNullOrEmpty(Value) && !string.IsNullOrEmpty(Placeholder);
        return showPlaceholder ? Placeholder : Value;
    }

    private static float InnerTextWidth(Vector2 drawSize) =>
        MathF.Max(0f, drawSize.X - TextPadding * 2f);

    private float MeasureDisplayTextWidth(string displayText, IUIRenderer renderer)
    {
        float physicalScale = MathF.Max(renderer.ScaleToPhysical(1f), 0.001f);
        float fittedPhysical = renderer.ResolveFontSize(FontSize);
        return UIFontMetrics.MeasureTextWidth(displayText, fittedPhysical) / physicalScale;
    }

    private static string SliceTextToViewport(
        string text, float scrollOffsetPhysical, float viewportPhysical, float fontSize)
    {
        if (string.IsNullOrEmpty(text) || viewportPhysical <= 0f)
            return string.Empty;

        int startIndex = IndexAtPhysicalOffset(text, scrollOffsetPhysical, fontSize);
        var builder = new System.Text.StringBuilder();
        for (int i = startIndex; i < text.Length; i++)
        {
            string candidate = builder.ToString() + text[i];
            if (UIFontMetrics.MeasureTextWidth(candidate, fontSize) > viewportPhysical + UITextDrawing.WidthTolerance)
                break;
            builder.Append(text[i]);
        }

        return builder.Length == 0 && startIndex < text.Length
            ? text[startIndex].ToString()
            : builder.ToString();
    }

    private static int IndexAtPhysicalOffset(string text, float offsetPhysical, float fontSize)
    {
        if (offsetPhysical <= 0f)
            return 0;

        for (int i = 0; i < text.Length; i++)
        {
            float width = UIFontMetrics.MeasureTextWidth(text[..i], fontSize);
            if (width >= offsetPhysical - UITextDrawing.WidthTolerance)
                return i;
        }

        return text.Length;
    }

    private static int MaxScrollStartIndex(string text, float viewportPhysical, float fontSize)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (UIFontMetrics.MeasureTextWidth(text[i..], fontSize) <= viewportPhysical + UITextDrawing.WidthTolerance)
                return i;
        }

        return text.Length;
    }

    private static IUIRenderer CreateMeasurementRenderer() =>
        new MeasurementRenderer();

    private sealed class MeasurementRenderer : IUIRenderer
    {
        public Vector2 ViewportSize => UIScaler.ReferenceSize;
        public float ScaleToPhysical(float logicalPixels) => logicalPixels;
        public float ResolveFontSize(float logicalFontSize) => logicalFontSize;
        public void DrawRect(Vector2 position, Vector2 size, Vector4 color) { }
        public void DrawRectOutline(Vector2 position, Vector2 size, Vector4 color) { }
        public void DrawText(string text, Vector2 position, float fontSize, Vector4 color) { }
    }
}