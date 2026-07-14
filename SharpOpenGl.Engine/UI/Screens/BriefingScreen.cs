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
    private const float TitleRegionHeight = 72f;
    private const int ObjectiveMaxLines = 4;
    private const float ObjectiveItemGap = 6f;
    private static readonly float ContentWidth = UIScaler.ReferenceSize.X - Margin * 2f;

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

        AddWidget(new Label
        {
            Name = "BriefingTitle",
            Text = "MISSION BRIEFING",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(0f, 32f),
            Size = new Vector2(900f, 48f),
            FontSize = 34f,
            TextColor = MenuTheme.ScreenHeadingColor,
        });

        AddWidget(new Label
        {
            Name = "BriefingSubtitle",
            Text = "Review objectives before launch",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(0f, 76f),
            Size = new Vector2(700f, 28f),
            FontSize = 16f,
            TextColor = MenuTheme.SubtitleColor,
        });

        _briefingPanel = new Panel
        {
            Name     = "BriefingText",
            Anchor   = Anchor.TopLeft,
            Position = new Vector2(Margin, 120f),
            Size     = new Vector2(ContentWidth, BriefingPanelHeight),
        };
        MenuTheme.ApplyPanel(_briefingPanel);

        _briefingBodyScroll = new ScrollPanel
        {
            Name            = "BriefingBodyScroll",
            Anchor          = Anchor.TopLeft,
            Position        = new Vector2(PanelPadding, TitleRegionHeight),
            Size            = new Vector2(ContentWidth - PanelPadding * 2f, BriefingPanelHeight - TitleRegionHeight),
            BackgroundColor = new Vector4(0f, 0f, 0f, 0f),
            DrawBorder      = false,
            ContentPadding  = 4f,
        };
        _briefingPanel.AddChild(_briefingBodyScroll);
        AddWidget(_briefingPanel);

        _objectivesPanel = new ScrollPanel
        {
            Name     = "ObjectivesPreview",
            Anchor   = Anchor.TopLeft,
            Position = new Vector2(Margin, 660f),
            Size     = new Vector2(ContentWidth, 280f),
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

        float wrapWidth = UITextDrawing.ContentWrapWidth(ContentWidth, PanelPadding);

        _briefingPanel.AddChild(new Label
        {
            Name      = "MissionTitle",
            Text      = definition.DisplayName,
            Anchor    = Anchor.TopLeft,
            Position  = new Vector2(PanelPadding, 12f),
            Size      = new Vector2(ContentWidth - PanelPadding * 2f, 52f),
            FontSize  = 32f,
            WrapWidth = wrapWidth,
            MaxLines  = 2,
            TextColor = MenuTheme.ScreenHeadingColor,
        });

        while (_briefingBodyScroll.Children.Count > 0)
            _briefingBodyScroll.RemoveChild(_briefingBodyScroll.Children[0]);

        string briefingText = definition.Briefing?.Text ?? definition.Description;
        const float bodyFontSize = 20f;
        float bodyHeight = UITextDrawing.MeasureTextBlockHeight(briefingText, bodyFontSize, wrapWidth);

        _briefingBodyScroll.AddChild(new Label
        {
            Name      = "BriefingBody",
            Text      = briefingText,
            Anchor    = Anchor.TopLeft,
            Position  = new Vector2(0f, 0f),
            Size      = new Vector2(ContentWidth - PanelPadding * 2f, bodyHeight + 8f),
            FontSize  = bodyFontSize,
            WrapWidth = wrapWidth,
            TextColor = MenuTheme.BodyTextColor,
        });

        _briefingBodyScroll.RecalculateContentHeight(_briefingBodyScroll.Size);

        while (_objectivesPanel.Children.Count > 0)
            _objectivesPanel.RemoveChild(_objectivesPanel.Children[0]);

        _objectivesPanel.AddChild(new Label
        {
            Name      = "ObjectivesHeader",
            Text      = "OBJECTIVES",
            Anchor    = Anchor.TopLeft,
            Position  = new Vector2(PanelPadding, 8f),
            Size      = new Vector2(400f, 32f),
            FontSize  = 20f,
            TextColor = new Vector4(0.7f, 0.8f, 1f, 1f),
        });

        var previews = definition.Briefing?.ObjectivesPreview ?? [];
        float objectiveY = 44f;
        const float objectiveFontSize = 18f;
        float objectiveLineHeight = objectiveFontSize * UITextDrawing.LineHeightFactor;
        for (int i = 0; i < previews.Length; i++)
        {
            string objectiveText = $"• {previews[i]}";
            int lineCount = UITextDrawing.WrapTextLimited(
                objectiveText, wrapWidth, objectiveFontSize, maxLines: ObjectiveMaxLines).Count;
            float labelHeight = Math.Max(objectiveLineHeight, lineCount * objectiveLineHeight) + ObjectiveItemGap;

            _objectivesPanel.AddChild(new Label
            {
                Name      = $"Objective_{i}",
                Text      = objectiveText,
                Anchor    = Anchor.TopLeft,
                Position  = new Vector2(PanelPadding, objectiveY),
                Size      = new Vector2(ContentWidth - PanelPadding * 2f, labelHeight),
                FontSize  = objectiveFontSize,
                WrapWidth = wrapWidth,
                MaxLines  = ObjectiveMaxLines,
                TextColor = new Vector4(0.88f, 0.9f, 0.95f, 1f),
            });
            objectiveY += labelHeight;
        }

        _objectivesPanel.RecalculateContentHeight(_objectivesPanel.Size);
    }

    /// <summary>Raised by <see cref="UIManager"/> after the loading overlay completes.</summary>
    internal void RaiseStartRequested() => StartRequested?.Invoke();

    /// <summary>Intercept Start Mission to show loading overlay before gameplay transition.</summary>
    internal void SetCampaignStartInterceptor(Action? interceptor) => _campaignStartInterceptor = interceptor;

    private void OnStartClicked()
    {
        if (_campaignStartInterceptor != null)
            _campaignStartInterceptor();
        else
            StartRequested?.Invoke();
    }
}