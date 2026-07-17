using OpenTK.Mathematics;
using SharpOpenGl.Engine.Missions;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Engine.UI.Screens;

/// <summary>
/// Full-screen overlay shown between mission selection and gameplay.
/// Displays the mission briefing text, objectives preview, and
/// provides Start and Back actions.
/// </summary>
public sealed class BriefingScreen : UIScreen
{
    private const float Margin = 80f;
    private const float PanelPadding = 16f;
    private const float BriefingPanelHeight = 500f;
    private const float BriefingPanelTop = 120f;
    private const float TitleRegionHeight = 72f;
    private const float ObjectivesGap = 40f;
    private const float BottomMargin = 136f;
    private const float TitleChromeWidth = 900f;
    private const float SubtitleChromeWidth = 700f;
    private const int ObjectiveMaxLines = 4;
    private const float ObjectiveItemGap = 6f;

    private readonly Label _briefingTitle;
    private readonly Label _briefingSubtitle;
    private readonly Panel _briefingPanel;
    private readonly ScrollPanel _briefingBodyScroll;
    private readonly ScrollPanel _objectivesPanel;
    private readonly IconButton _startButton;
    private readonly IconButton _backButton;
    private Action? _campaignStartInterceptor;

    /// <inheritdoc/>
    public override string ScreenName => "Briefing";

    /// <summary>Mission id from the last <see cref="SetMission"/> call.</summary>
    public string? MissionId { get; private set; }

    /// <summary>Display name from the last <see cref="SetMission"/> call.</summary>
    public string? MissionDisplayName { get; private set; }

    /// <summary>Fired when the player clicks "Start Mission" (after any loading overlay completes).</summary>
    public event Action? StartRequested;

    /// <summary>Fired when the player clicks "Back".</summary>
    public event Action? BackRequested;

    /// <summary>Build the briefing screen layout.</summary>
    public BriefingScreen()
    {
        AddWidget(new MenuStarfieldBackground(starCount: 240, seed: 112)
        {
            Name = "Starfield",
            Anchor = Anchor.Stretch,
            Position = Vector2.Zero,
        });

        _briefingTitle = new Label
        {
            Name = "BriefingTitle",
            Text = "MISSION BRIEFING",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(0f, 32f),
            Size = new Vector2(TitleChromeWidth, 48f),
            FontSize = 34f,
            TextColor = MenuTheme.ScreenHeadingColor,
        };
        AddWidget(_briefingTitle);

        _briefingSubtitle = new Label
        {
            Name = "BriefingSubtitle",
            Text = "Review objectives before launch",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(0f, 76f),
            Size = new Vector2(SubtitleChromeWidth, 28f),
            FontSize = 16f,
            TextColor = MenuTheme.SubtitleColor,
        };
        AddWidget(_briefingSubtitle);

        _briefingPanel = new Panel
        {
            Name     = "BriefingText",
            Anchor   = Anchor.TopLeft,
            Position = new Vector2(Margin, BriefingPanelTop),
            Size     = new Vector2(PanelContentWidth(UIScaler.ReferenceSize), BriefingPanelHeight),
        };
        MenuTheme.ApplyPanel(_briefingPanel);

        _briefingBodyScroll = new ScrollPanel
        {
            Name            = "BriefingBodyScroll",
            Anchor          = Anchor.TopLeft,
            Position        = new Vector2(PanelPadding, TitleRegionHeight),
            Size            = new Vector2(0f, BriefingPanelHeight - TitleRegionHeight),
            BackgroundColor = new Vector4(0f, 0f, 0f, 0f),
            DrawBorder      = false,
            ContentPadding  = 4f,
            AutoSyncWrapWidths = false,
        };
        _briefingPanel.AddChild(_briefingBodyScroll);
        AddWidget(_briefingPanel);

        _objectivesPanel = new ScrollPanel
        {
            Name     = "ObjectivesPreview",
            Anchor   = Anchor.TopLeft,
            Position = new Vector2(Margin, BriefingPanelTop + BriefingPanelHeight + ObjectivesGap),
            Size     = new Vector2(0f, 280f),
            AutoSyncWrapWidths = false,
        };
        MenuTheme.ApplyPanel(_objectivesPanel);
        AddWidget(_objectivesPanel);

        _startButton = new IconButton
        {
            Name     = "StartMission",
            Icon     = MenuIconKind.NavStartMission,
            Label    = "Start Mission",
            TooltipHint = "Launch mission",
            Anchor   = Anchor.BottomRight,
            Position = new Vector2(-80f, -80f),
            Size     = new Vector2(280f, 56f),
            Layout   = IconButtonLayout.IconLeftOfLabel,
            IconSize = IconButton.TitleNavIconSize,
            FontSize = 18f,
            RequireMinimumHitExtent = true,
        };
        IconButton.ApplyMenuTheme(_startButton, showGlow: true);
        _startButton.Clicked += OnStartClicked;
        AddWidget(_startButton);

        _backButton = new IconButton
        {
            Name     = "Back",
            Icon     = MenuIconKind.NavBack,
            Label    = "Back",
            TooltipHint = "Return to mission select",
            Anchor   = Anchor.BottomLeft,
            Position = new Vector2(80f, -80f),
            Size     = new Vector2(200f, 56f),
            Layout   = IconButtonLayout.IconLeftOfLabel,
            IconSize = IconButton.TitleNavIconSize,
            FontSize = 18f,
            RequireMinimumHitExtent = true,
        };
        IconButton.ApplySecondaryMenuTheme(_backButton);
        _backButton.Clicked += () => BackRequested?.Invoke();
        AddWidget(_backButton);

        ApplyViewportLayout(UIScaler.ReferenceSize, scaler: null);
    }

    /// <inheritdoc/>
    public override void Draw(IUIRenderer renderer)
    {
        UIScaler? scaler = renderer.ViewportSize != UIScaler.ReferenceSize
            ? new UIScaler(renderer.ViewportSize)
            : null;
        ApplyViewportLayout(UIScaler.ReferenceSize, scaler);
        base.Draw(renderer);
    }

    /// <summary>
    /// Populate the screen with data from a <see cref="MissionDefinition"/>.
    /// </summary>
    public void SetMission(MissionDefinition definition)
    {
        MissionId = definition.Id;
        MissionDisplayName = definition.DisplayName;

        while (_briefingPanel.Children.Count > 1)
            _briefingPanel.RemoveChild(_briefingPanel.Children[1]);

        Vector2 viewport = UIScaler.ReferenceSize;
        float panelW = PanelContentWidth(viewport);
        float innerW = panelW - PanelPadding * 2f;
        float missionTitleWrap = UITextDrawing.ContentWrapWidth(innerW, PanelPadding);

        _briefingPanel.AddChild(new Label
        {
            Name      = "MissionTitle",
            Text      = definition.DisplayName,
            Anchor    = Anchor.TopLeft,
            Position  = new Vector2(PanelPadding, 12f),
            Size      = new Vector2(innerW, 52f),
            FontSize  = 32f,
            WrapWidth = missionTitleWrap,
            MaxLines  = 2,
            TextColor = MenuTheme.ScreenHeadingColor,
        });

        while (_briefingBodyScroll.Children.Count > 0)
            _briefingBodyScroll.RemoveChild(_briefingBodyScroll.Children[0]);

        string briefingText = definition.Briefing?.Text ?? definition.Description;
        const float bodyFontSize = 20f;

        var briefingBody = new Label
        {
            Name      = "BriefingBody",
            Text      = briefingText,
            Anchor    = Anchor.TopLeft,
            Position  = new Vector2(0f, 0f),
            Size      = new Vector2(innerW, 0f),
            FontSize  = bodyFontSize,
            Padding   = _briefingBodyScroll.ContentPadding,
            TextColor = MenuTheme.BodyTextColor,
        };
        _briefingBodyScroll.AddChild(briefingBody);

        while (_objectivesPanel.Children.Count > 0)
            _objectivesPanel.RemoveChild(_objectivesPanel.Children[0]);

        float objectivesContentW = panelW - PanelPadding * 2f;

        _objectivesPanel.AddChild(new Label
        {
            Name      = "ObjectivesHeader",
            Text      = "OBJECTIVES",
            Anchor    = Anchor.TopLeft,
            Position  = new Vector2(PanelPadding, 8f),
            Size      = new Vector2(MathF.Min(400f, objectivesContentW), 32f),
            FontSize  = 20f,
            TextColor = new Vector4(0.7f, 0.8f, 1f, 1f),
        });

        var previews = definition.Briefing?.ObjectivesPreview ?? [];
        float objectiveY = 44f;
        const float objectiveFontSize = 18f;
        for (int i = 0; i < previews.Length; i++)
        {
            string objectiveText = $"• {previews[i]}";
            var objectiveLabel = new Label
            {
                Name      = $"Objective_{i}",
                Text      = objectiveText,
                Anchor    = Anchor.TopLeft,
                Position  = new Vector2(PanelPadding, objectiveY),
                Size      = new Vector2(objectivesContentW, 0f),
                FontSize  = objectiveFontSize,
                MaxLines  = ObjectiveMaxLines,
                TextColor = new Vector4(0.88f, 0.9f, 0.95f, 1f),
            };
            _objectivesPanel.AddChild(objectiveLabel);
            objectiveY += objectiveLabel.MeasureContentHeight() + ObjectiveItemGap;
        }

        ApplyViewportLayout(viewport, scaler: null);
    }

    /// <summary>Raised by <see cref="UIManager"/> after the loading overlay completes.</summary>
    internal void RaiseStartRequested() => StartRequested?.Invoke();

    /// <summary>Intercept Start Mission to show loading overlay before gameplay transition.</summary>
    internal void SetCampaignStartInterceptor(Action? interceptor) => _campaignStartInterceptor = interceptor;

    private static float PanelContentWidth(Vector2 viewport) =>
        MathF.Max(0f, viewport.X - Margin * 2f);

    private void ApplyViewportLayout(Vector2 viewport, UIScaler? scaler)
    {
        float panelW = PanelContentWidth(viewport);
        float innerW = panelW - PanelPadding * 2f;

        float titleW = MathF.Min(TitleChromeWidth, panelW);
        _briefingTitle.Size = new Vector2(titleW, 48f);
        _briefingTitle.WrapWidth = UITextDrawing.ScaleAwareWrapWidth(
            UITextDrawing.ContentWrapWidth(titleW, 4f), _briefingTitle.FontSize, scaler);

        float subtitleW = MathF.Min(SubtitleChromeWidth, panelW);
        _briefingSubtitle.Size = new Vector2(subtitleW, 28f);
        _briefingSubtitle.WrapWidth = UITextDrawing.ScaleAwareWrapWidth(
            UITextDrawing.ContentWrapWidth(subtitleW, 4f), _briefingSubtitle.FontSize, scaler);

        _briefingPanel.Position = new Vector2(Margin, BriefingPanelTop);
        _briefingPanel.Size = new Vector2(panelW, BriefingPanelHeight);

        float bodyScrollH = BriefingPanelHeight - TitleRegionHeight;
        _briefingBodyScroll.Size = new Vector2(innerW, bodyScrollH);

        float objectivesTop = BriefingPanelTop + BriefingPanelHeight + ObjectivesGap;
        float objectivesH = MathF.Max(180f, viewport.Y - objectivesTop - BottomMargin);
        _objectivesPanel.Position = new Vector2(Margin, objectivesTop);
        _objectivesPanel.Size = new Vector2(panelW, objectivesH);

        SyncMissionContentLayout(innerW, panelW, scaler);
    }

    private void SyncMissionContentLayout(float innerW, float panelW, UIScaler? scaler)
    {
        foreach (Widget child in _briefingPanel.Children)
        {
            if (child is not Label { Name: "MissionTitle" } missionTitle)
                continue;

            float wrap = UITextDrawing.ScaleAwareWrapWidth(
                UITextDrawing.ContentWrapWidth(innerW, PanelPadding), missionTitle.FontSize, scaler);
            missionTitle.Size = new Vector2(innerW, 52f);
            missionTitle.WrapWidth = wrap;
        }

        _briefingBodyScroll.Size = new Vector2(innerW, BriefingPanelHeight - TitleRegionHeight);

        Label? briefingBody = null;
        foreach (Widget child in _briefingBodyScroll.Children)
        {
            if (child is Label { Name: "BriefingBody" } body)
                briefingBody = body;
        }

        if (briefingBody != null)
        {
            briefingBody.Size = new Vector2(_briefingBodyScroll.Size.X, 0f);
            float scrollbarGutter = _briefingBodyScroll.ShowScrollbar ? 10f : 0f;
            float bodyWrap = UITextDrawing.ContentWrapWidth(
                briefingBody.Size.X - scrollbarGutter, briefingBody.Padding);
            briefingBody.WrapWidth = UITextDrawing.ScaleAwareWrapWidth(bodyWrap, briefingBody.FontSize, scaler);
            briefingBody.Size = new Vector2(briefingBody.Size.X, briefingBody.MeasureContentHeight());
            _briefingBodyScroll.RecalculateContentHeight(_briefingBodyScroll.Size);
        }

        float objectivesContentW = panelW - PanelPadding * 2f;
        foreach (Widget child in _objectivesPanel.Children)
        {
            if (child is not Label label)
                continue;

            if (label.Name == "ObjectivesHeader")
                label.Size = new Vector2(MathF.Min(400f, objectivesContentW), 32f);
            else if (!string.IsNullOrEmpty(label.Name)
                     && label.Name.StartsWith("Objective_", StringComparison.Ordinal))
                label.Size = new Vector2(objectivesContentW, label.Size.Y);
        }

        float objectivesGutter = _objectivesPanel.ShowScrollbar ? 10f : 0f;
        foreach (Widget child in _objectivesPanel.Children)
        {
            if (child is not Label label || string.IsNullOrEmpty(label.Name))
                continue;

            float baseWrap = UITextDrawing.ContentWrapWidth(
                label.Size.X - objectivesGutter, label.Padding);

            if (label.Name.StartsWith("Objective_", StringComparison.Ordinal))
            {
                label.WrapWidth = UITextDrawing.ScaleAwareWrapWidth(baseWrap, label.FontSize, scaler);
                label.Size = new Vector2(label.Size.X, label.MeasureContentHeight());
            }
            else if (label.Name == "ObjectivesHeader")
            {
                label.WrapWidth = UITextDrawing.ScaleAwareWrapWidth(baseWrap, label.FontSize, scaler);
            }
        }

        RelayoutObjectiveRows();
        _objectivesPanel.RecalculateContentHeight(_objectivesPanel.Size);
    }

    private void RelayoutObjectiveRows()
    {
        float objectiveY = 44f;
        foreach (Widget child in _objectivesPanel.Children)
        {
            if (child is not Label { Name: var name } label || string.IsNullOrEmpty(name))
                continue;

            if (!name.StartsWith("Objective_", StringComparison.Ordinal))
                continue;

            label.Position = new Vector2(PanelPadding, objectiveY);
            objectiveY += label.Size.Y + ObjectiveItemGap;
        }
    }

    private void OnStartClicked()
    {
        if (_campaignStartInterceptor != null)
            _campaignStartInterceptor();
        else
            StartRequested?.Invoke();
    }
}