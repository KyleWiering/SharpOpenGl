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
    private static readonly float ContentWidth = UIScaler.ReferenceSize.X - Margin * 2f;

    private readonly Panel  _briefingPanel;
    private readonly ScrollPanel _objectivesPanel;
    private readonly Button _startButton;
    private readonly Button _backButton;

    /// <inheritdoc/>
    public override string ScreenName => "Briefing";

    /// <summary>Fired when the player clicks "Start Mission".</summary>
    public event Action? StartRequested;

    /// <summary>Fired when the player clicks "Back".</summary>
    public event Action? BackRequested;

    /// <summary>Build the briefing screen layout.</summary>
    public BriefingScreen()
    {
        _briefingPanel = new Panel
        {
            Name     = "BriefingText",
            Anchor   = Anchor.TopLeft,
            Position = new Vector2(Margin, 120f),
            Size     = new Vector2(ContentWidth, 500f),
        };
        AddWidget(_briefingPanel);

        _objectivesPanel = new ScrollPanel
        {
            Name     = "ObjectivesPreview",
            Anchor   = Anchor.TopLeft,
            Position = new Vector2(Margin, 660f),
            Size     = new Vector2(ContentWidth, 280f),
        };
        AddWidget(_objectivesPanel);

        _startButton = new Button
        {
            Name     = "StartMission",
            Label    = "Start Mission",
            Anchor   = Anchor.BottomRight,
            Position = new Vector2(-80f, -80f),
            Size     = new Vector2(280f, 56f),
            FontSize = 22f,
        };
        _startButton.Clicked += () => StartRequested?.Invoke();
        AddWidget(_startButton);

        _backButton = new Button
        {
            Name     = "Back",
            Label    = "Back",
            Anchor   = Anchor.BottomLeft,
            Position = new Vector2(80f, -80f),
            Size     = new Vector2(200f, 56f),
            FontSize = 22f,
        };
        _backButton.Clicked += () => BackRequested?.Invoke();
        AddWidget(_backButton);
    }

    /// <summary>
    /// Populate the screen with data from a <see cref="MissionDefinition"/>.
    /// </summary>
    public void SetMission(MissionDefinition definition)
    {
        while (_briefingPanel.Children.Count > 0)
            _briefingPanel.RemoveChild(_briefingPanel.Children[0]);

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
            TextColor = new Vector4(0.55f, 0.85f, 1f, 1f),
        });

        string briefingText = definition.Briefing?.Text ?? definition.Description;
        _briefingPanel.AddChild(new Label
        {
            Name      = "BriefingBody",
            Text      = briefingText,
            Anchor    = Anchor.TopLeft,
            Position  = new Vector2(PanelPadding, 72f),
            Size      = new Vector2(ContentWidth - PanelPadding * 2f, 410f),
            FontSize  = 20f,
            WrapWidth = wrapWidth,
            TextColor = new Vector4(0.92f, 0.93f, 0.98f, 1f),
        });

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
        for (int i = 0; i < previews.Length; i++)
        {
            _objectivesPanel.AddChild(new Label
            {
                Name      = $"Objective_{i}",
                Text      = $"• {previews[i]}",
                Anchor    = Anchor.TopLeft,
                Position  = new Vector2(PanelPadding, objectiveY),
                Size      = new Vector2(ContentWidth - PanelPadding * 2f, 72f),
                FontSize  = 18f,
                WrapWidth = wrapWidth,
                MaxLines  = 3,
                TextColor = new Vector4(0.88f, 0.9f, 0.95f, 1f),
            });
            objectiveY += 72f;
        }

        _objectivesPanel.RecalculateContentHeight(_objectivesPanel.Size);
    }
}