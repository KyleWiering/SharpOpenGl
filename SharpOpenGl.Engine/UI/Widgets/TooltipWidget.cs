using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>
/// Viewport overlay that shows multi-line tooltip text after a short hover delay.
/// </summary>
public sealed class TooltipWidget : Widget
{
    public const float HoverShowDelaySeconds = 0.2f;
    public const float PointerOffset = TooltipLayout.DefaultPointerOffset;
    public const float MaxTextWidth = TooltipLayout.DefaultMaxTextWidth;
    public const float Padding = TooltipLayout.DefaultPadding;
    public const float FontSize = TooltipLayout.DefaultFontSize;

    private TooltipContent? _pendingContent;
    private Vector2 _pointerPosition;
    private float _hoverTimer;
    private bool _isShowing;
    private TooltipContent? _visibleContent;

    public Vector4 BackgroundColor { get; set; } = new(0.08f, 0.1f, 0.16f, 0.92f);
    public Vector4 BorderColor { get; set; } = new(0.45f, 0.55f, 0.75f, 1f);
    public Vector4 TextColor { get; set; } = new(0.95f, 0.96f, 0.98f, 1f);

    /// <summary>Whether the tooltip is currently visible after the hover delay.</summary>
    public bool IsShowing => _isShowing;

    /// <summary>Elapsed hover time toward the show delay.</summary>
    public float HoverTimerSeconds => _hoverTimer;

    /// <summary>
    /// Update hover target. Pass null content to hide immediately.
    /// </summary>
    public void SetHover(TooltipContent? content, Vector2 pointerPosition)
    {
        if (content is null)
        {
            Clear();
            return;
        }

        bool contentChanged = !ReferenceEquals(_pendingContent, content)
            && (_pendingContent is null || !Equals(_pendingContent, content));

        _pendingContent = content;
        _pointerPosition = pointerPosition;

        if (contentChanged)
        {
            _hoverTimer = 0f;
            _isShowing = false;
            _visibleContent = null;
        }
    }

    /// <summary>Hide the tooltip immediately and reset the hover timer.</summary>
    public void Clear()
    {
        _pendingContent = null;
        _visibleContent = null;
        _isShowing = false;
        _hoverTimer = 0f;
    }

    public override void Update(float deltaTime)
    {
        if (_pendingContent is null)
        {
            base.Update(deltaTime);
            return;
        }

        if (!_isShowing)
        {
            _hoverTimer += deltaTime;
            if (_hoverTimer >= HoverShowDelaySeconds)
            {
                _visibleContent = _pendingContent;
                _isShowing = true;
            }
        }

        base.Update(deltaTime);
    }

    protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
    {
        if (!_isShowing || _visibleContent is null)
            return;

        var (wrappedLines, tooltipSize) = TooltipLayout.MeasureContent(
            _visibleContent, MaxTextWidth, FontSize, Padding);
        Vector2 tooltipOrigin = TooltipLayout.ComputeBounds(
            _pointerPosition, tooltipSize, size, PointerOffset);
        Vector2 drawPos = position + tooltipOrigin;

        renderer.DrawRect(drawPos, tooltipSize, BackgroundColor);
        renderer.DrawRectOutline(drawPos, tooltipSize, BorderColor);

        string text = string.Join('\n', wrappedLines);
        Vector2 textPos = drawPos + new Vector2(Padding, Padding);
        UITextDrawing.DrawTextBlock(renderer, text, textPos, FontSize, TextColor, MaxTextWidth);
    }
}