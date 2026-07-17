using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.UI.Widgets;

/// <summary>
/// Viewport overlay that shows multi-line tooltip text after a short hover delay.
/// Long detail uses an internal <see cref="ScrollPanel"/> with a wrapped <see cref="Label"/>.
/// </summary>
public sealed class TooltipWidget : Widget
{
    public const float HoverShowDelaySeconds = 0.2f;
    public const float PointerOffset = TooltipLayout.DefaultPointerOffset;
    public const float MaxTextWidth = TooltipLayout.DefaultMaxTextWidth;
    public const float Padding = TooltipLayout.DefaultPadding;
    public const float FontSize = TooltipLayout.DefaultFontSize;
    public const int ScrollViewportMaxLines = TooltipLayout.DefaultScrollViewportMaxLines;

    private readonly ScrollPanel _scrollPanel;
    private readonly Label _bodyLabel;

    private TooltipContent? _pendingContent;
    private Vector2 _pointerPosition;
    private float _hoverTimer;
    private bool _isShowing;
    private TooltipContent? _visibleContent;
    private bool _layoutDirty = true;
    private Vector2 _tooltipSize;
    private Vector2 _scrollViewportSize;

    public Vector4 BackgroundColor { get; set; } = new(0.08f, 0.1f, 0.16f, 0.92f);
    public Vector4 BorderColor { get; set; } = new(0.45f, 0.55f, 0.75f, 1f);
    public Vector4 TextColor { get; set; } = new(0.95f, 0.96f, 0.98f, 1f);

    /// <summary>Whether the tooltip is currently visible after the hover delay.</summary>
    public bool IsShowing => _isShowing;

    /// <summary>Elapsed hover time toward the show delay.</summary>
    public float HoverTimerSeconds => _hoverTimer;

    /// <summary>Measured scroll content height for the visible tooltip body.</summary>
    public float ContentHeight => _scrollPanel.ContentHeight;

    /// <summary>Maximum vertical scroll offset for the current viewport.</summary>
    public float MaxScrollOffset => _scrollPanel.MaxScrollOffset(_scrollViewportSize);

    /// <summary>Height of the capped scroll viewport (≤ ~2 lines when scrolling).</summary>
    public float ScrollViewportHeight => _scrollViewportSize.Y;

    /// <summary>Wrapped body label (for layout tests).</summary>
    internal Label BodyLabel => _bodyLabel;

    /// <summary>Scroll host (for layout tests).</summary>
    internal ScrollPanel ScrollHost => _scrollPanel;

    /// <summary>Measured tooltip box size in logical coordinates (for layout tests).</summary>
    internal Vector2 TooltipSize => _tooltipSize;

    /// <summary>Resolve viewport-safe tooltip origin for the current pointer and renderer.</summary>
    internal Vector2 ResolveTooltipOrigin(IUIRenderer renderer, Vector2 logicalViewportSize) =>
        TooltipLayout.ComputeBounds(
            _pointerPosition, _tooltipSize, logicalViewportSize, PointerOffset, renderer);

    public TooltipWidget()
    {
        _scrollPanel = new ScrollPanel
        {
            ContentPadding = 0f,
            ShowScrollbar = true,
            ScrollStep = FontSize * UITextDrawing.LineHeightFactor,
        };

        _bodyLabel = new Label
        {
            Position = Vector2.Zero,
            FontSize = FontSize,
            Padding = 0f,
        };
        _scrollPanel.AddChild(_bodyLabel);
    }

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

        bool contentChanged = _pendingContent is null || !Equals(_pendingContent, content);

        _pendingContent = content;
        _pointerPosition = pointerPosition;

        if (contentChanged)
        {
            _hoverTimer = 0f;
            _isShowing = false;
            _visibleContent = null;
            _layoutDirty = true;
        }
    }

    /// <summary>Hide the tooltip immediately and reset the hover timer.</summary>
    public void Clear()
    {
        _pendingContent = null;
        _visibleContent = null;
        _isShowing = false;
        _hoverTimer = 0f;
        _layoutDirty = true;
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
                _layoutDirty = true;
            }
        }

        if (_isShowing && _visibleContent is not null)
            EnsureLayout(_visibleContent);

        base.Update(deltaTime);
    }

    public override void Draw(IUIRenderer renderer, Vector2 containerPosition, Vector2 containerSize)
    {
        if (!Visible || !_isShowing || _visibleContent is null)
            return;

        EnsureLayout(_visibleContent, renderer);

        Vector2 tooltipOrigin = TooltipLayout.ComputeBounds(
            _pointerPosition, _tooltipSize, containerSize, PointerOffset, renderer);
        Vector2 drawPos = containerPosition + tooltipOrigin;

        renderer.DrawRect(drawPos, _tooltipSize, BackgroundColor);
        renderer.DrawRectOutline(drawPos, _tooltipSize, BorderColor);

        Vector2 scrollPos = drawPos + new Vector2(Padding, Padding);
        _scrollPanel.Draw(renderer, scrollPos, _scrollViewportSize);
    }

    /// <inheritdoc/>
    public override bool HandleScroll(
        Vector2 screenPoint, float deltaY,
        Vector2 containerPosition, Vector2 containerSize)
    {
        if (!_isShowing || _visibleContent is null)
            return false;

        EnsureLayout(_visibleContent);

        Vector2 tooltipOrigin = TooltipLayout.ComputeBounds(
            _pointerPosition, _tooltipSize, containerSize, PointerOffset);
        Vector2 scrollPos = containerPosition + tooltipOrigin + new Vector2(Padding, Padding);

        return _scrollPanel.HandleScroll(screenPoint, deltaY, scrollPos, _scrollViewportSize);
    }

    private void EnsureLayout(TooltipContent content, IUIRenderer? renderer = null)
    {
        if (renderer == null)
        {
            if (!_layoutDirty)
                return;

            RebuildScrollLayout(content);
            _layoutDirty = false;
            return;
        }

        RebuildScrollLayout(content, renderer);
        _layoutDirty = false;
    }

    private void RebuildScrollLayout(TooltipContent content, IUIRenderer? renderer = null)
    {
        _bodyLabel.Text = string.Join('\n', content.ToLines());
        _bodyLabel.FontSize = FontSize;
        _bodyLabel.TextColor = TextColor;

        float innerWrapWidth = UITextDrawing.ScaleAwareWrapWidth(MaxTextWidth, FontSize, renderer);
        float innerTextWidth = TooltipLayout.MeasureInnerTextWidth(
            content, MaxTextWidth, FontSize, renderer);

        _bodyLabel.Size = new Vector2(innerTextWidth, 0f);
        _bodyLabel.WrapWidth = innerWrapWidth;
        _scrollPanel.Size = new Vector2(innerTextWidth, 10000f);
        _scrollPanel.ShowScrollbar = false;
        _scrollPanel.SyncLabelWrapWidths();
        _scrollPanel.RecalculateContentHeight(_scrollPanel.Size);

        float measureFont = renderer?.ResolveFontSize(FontSize) ?? FontSize;
        float measureScale = renderer != null
            ? MathF.Max(renderer.ScaleToPhysical(1f), 0.001f)
            : 1f;
        IReadOnlyList<string> wrapped = TooltipLayout.WrapLines(content.ToLines(), innerWrapWidth, FontSize);
        float measuredPhysicalHeight = wrapped.Count * TooltipLayout.LineHeight(measureFont);
        float measuredHeight = measuredPhysicalHeight / measureScale;

        var (tooltipSize, scrollSize, scrolling) = TooltipLayout.MeasureScrollViewport(
            measuredHeight, innerTextWidth, FontSize, Padding, ScrollViewportMaxLines);

        _scrollPanel.ShowScrollbar = scrolling;
        _tooltipSize = tooltipSize;
        _scrollViewportSize = scrollSize;
        _bodyLabel.Size = new Vector2(scrollSize.X, 0f);
        _bodyLabel.WrapWidth = UITextDrawing.ContentWrapWidth(scrollSize.X, 0f);
        _scrollPanel.Size = scrollSize;
        _scrollPanel.SyncLabelWrapWidths();
        _scrollPanel.RecalculateContentHeight(_scrollViewportSize);
    }
}