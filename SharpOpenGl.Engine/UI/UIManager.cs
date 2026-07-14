using OpenTK.Mathematics;
using SharpOpenGl.Engine.Events;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Engine.UI;

/// <summary>
/// Manages a stack of <see cref="UIScreen"/>s, routes input to the topmost
/// screen, and renders all visible screens in back-to-front order.
/// </summary>
public sealed class UIManager
{
    private const float CampaignLoadingDurationSeconds = 0.75f;

    private readonly List<UIScreen> _stack = new();
    private readonly EventBus _eventBus;
    private readonly UIScaler _scaler;
    private readonly TooltipWidget _tooltip = new();
    private Vector2 _pointerLogicalPosition;
    private UIScreen? _tooltipScreen;
    private CampaignLoadingTransition? _campaignLoading;

    public UIManager(EventBus eventBus)
    {
        _eventBus = eventBus;
        _scaler = new UIScaler(UIScaler.ReferenceSize);
    }

    public UIScaler Scaler => _scaler;
    public void Resize(Vector2 viewportSize) => _scaler.Resize(viewportSize);
    public int ScreenCount => _stack.Count;
    public UIScreen? Current => _stack.Count > 0 ? _stack[^1] : null;

    public void Push(UIScreen screen)
    {
        ClearTooltip();
        _stack.Add(screen);
        screen.OnEnter();
        AttachCampaignBriefingTransition(screen);
        _eventBus.Publish(new UIScreenChangedEvent(
            _stack.Count > 1 ? _stack[^2].ScreenName : string.Empty,
            screen.ScreenName,
            UIScreenAction.Pushed));
    }

    public UIScreen? Pop()
    {
        if (_stack.Count == 0) return null;

        ClearTooltip();
        UIScreen removed = _stack[^1];
        _stack.RemoveAt(_stack.Count - 1);
        removed.OnExit();

        _eventBus.Publish(new UIScreenChangedEvent(
            removed.ScreenName,
            Current?.ScreenName ?? string.Empty,
            UIScreenAction.Popped));

        return removed;
    }

    public void Replace(UIScreen screen)
    {
        ClearTooltip();
        if (_stack.Count > 0)
        {
            UIScreen old = _stack[^1];
            _stack.RemoveAt(_stack.Count - 1);
            old.OnExit();
        }
        Push(screen);
    }

    public void Clear()
    {
        ClearTooltip();
        for (int i = _stack.Count - 1; i >= 0; i--)
            _stack[i].OnExit();
        _stack.Clear();
    }

    public void Update(float deltaTime)
    {
        int startIdx = GetLowestVisibleIndex();
        for (int i = startIdx; i < _stack.Count; i++)
            _stack[i].Update(deltaTime);

        UpdateCampaignLoadingTransition(deltaTime);
        _tooltip.Update(deltaTime);
    }

    public void Draw(IUIRenderer renderer)
    {
        _scaler.Resize(renderer.ViewportSize);
        var scaledRenderer = new ScaledUIRenderer(renderer, _scaler);
        int startIdx = GetLowestVisibleIndex();
        for (int i = startIdx; i < _stack.Count; i++)
            _stack[i].Draw(scaledRenderer);

        _tooltip.Draw(scaledRenderer, Vector2.Zero, UIScaler.ReferenceSize);
    }

    public bool HandlePointerTapped(Vector2 screenPoint, int button, Vector2 viewportSize)
    {
        _scaler.Resize(viewportSize);
        Vector2 logicalPoint = _scaler.UnscalePosition(screenPoint);
        return Current?.HandlePointerTapped(logicalPoint, button, UIScaler.ReferenceSize) ?? false;
    }

    public void HandlePointerMove(Vector2 screenPoint, bool isPointerDown, Vector2 viewportSize)
    {
        _scaler.Resize(viewportSize);
        Vector2 logicalPoint = _scaler.UnscalePosition(screenPoint);
        _pointerLogicalPosition = logicalPoint;
        Current?.UpdatePointerState(logicalPoint, isPointerDown, UIScaler.ReferenceSize);
        RefreshTooltip();
    }

    /// <summary>Route a navigation key to the active screen.</summary>
    public bool HandleKey(UIKey key) => Current?.HandleKey(key) ?? false;

    /// <summary>Route a printable character to the active screen.</summary>
    public bool HandleChar(char c) => Current?.HandleChar(c) ?? false;

    /// <summary>Route a scroll-wheel delta to the active screen.</summary>
    public bool HandleScroll(Vector2 screenPoint, float deltaY, Vector2 viewportSize)
    {
        _scaler.Resize(viewportSize);
        Vector2 logicalPoint = _scaler.UnscalePosition(screenPoint);
        return Current?.HandleScroll(logicalPoint, deltaY, UIScaler.ReferenceSize) ?? false;
    }

    /// <summary>Return the hovered button on the active screen, if any.</summary>
    public IUIButton? FindHoveredButton() => Current?.FindHoveredButton();

    private int GetLowestVisibleIndex()
    {
        if (_stack.Count == 0) return 0;

        int idx = _stack.Count - 1;
        while (idx > 0 && _stack[idx].IsOverlay)
            idx--;
        return idx;
    }

    private void RefreshTooltip()
    {
        UIScreen? screen = Current;
        if (screen != _tooltipScreen)
        {
            _tooltip.Clear();
            _tooltipScreen = screen;
        }

        TooltipContent? content = screen?.FindTooltipContent();
        if (content is null)
            _tooltip.Clear();
        else
            _tooltip.SetHover(content, _pointerLogicalPosition);
    }

    private void ClearTooltip()
    {
        _tooltip.Clear();
        _tooltipScreen = null;
    }

    private void AttachCampaignBriefingTransition(UIScreen screen)
    {
        if (screen is not BriefingScreen briefing)
            return;

        briefing.SetCampaignStartInterceptor(() => BeginCampaignLoadingTransition(briefing));
    }

    private void BeginCampaignLoadingTransition(BriefingScreen briefing)
    {
        if (_campaignLoading != null)
            return;

        var loading = new LoadingScreen
        {
            StatusText = BuildCampaignLoadingStatus(briefing),
            Progress = 0f,
        };

        _campaignLoading = new CampaignLoadingTransition(briefing, loading);
        Push(loading);
    }

    private void UpdateCampaignLoadingTransition(float deltaTime)
    {
        if (_campaignLoading == null)
            return;

        _campaignLoading.ElapsedSeconds += deltaTime;
        float progress = Math.Clamp(_campaignLoading.ElapsedSeconds / CampaignLoadingDurationSeconds, 0f, 1f);
        _campaignLoading.Loading.Progress = progress;

        if (progress < 1f)
            return;

        BriefingScreen briefing = _campaignLoading.Briefing;
        LoadingScreen loading = _campaignLoading.Loading;
        _campaignLoading = null;

        if (Current == loading)
            Pop();

        briefing.RaiseStartRequested();
    }

    private static string BuildCampaignLoadingStatus(BriefingScreen briefing)
    {
        if (!string.IsNullOrWhiteSpace(briefing.MissionDisplayName))
            return $"Preparing {briefing.MissionDisplayName}…";

        if (!string.IsNullOrWhiteSpace(briefing.MissionId))
            return $"Loading mission {briefing.MissionId}…";

        return "Loading mission assets…";
    }

    private sealed class CampaignLoadingTransition
    {
        public CampaignLoadingTransition(BriefingScreen briefing, LoadingScreen loading)
        {
            Briefing = briefing;
            Loading = loading;
        }

        public BriefingScreen Briefing { get; }
        public LoadingScreen Loading { get; }
        public float ElapsedSeconds { get; set; }
    }
}