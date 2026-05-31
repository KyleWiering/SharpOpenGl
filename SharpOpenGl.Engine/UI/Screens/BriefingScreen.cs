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
    // ── Widgets ───────────────────────────────────────────────────────────────

    private readonly Panel  _briefingPanel;
    private readonly Panel  _objectivesPanel;
    private readonly Button _startButton;
    private readonly Button _backButton;

    /// <inheritdoc/>
    public override string ScreenName => "Briefing";

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Fired when the player clicks "Start Mission".</summary>
    public event Action? StartRequested;

    /// <summary>Fired when the player clicks "Back".</summary>
    public event Action? BackRequested;

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>Build the briefing screen layout.</summary>
    public BriefingScreen()
    {
        // ── Main briefing text area ───────────────────────────────────────────
        _briefingPanel = new Panel
        {
            Name     = "BriefingText",
            Anchor   = Anchor.TopLeft,
            Position = new Vector2(80f, 120f),
            Size     = new Vector2(1760f, 500f),
        };
        AddWidget(_briefingPanel);

        // ── Objectives preview ────────────────────────────────────────────────
        _objectivesPanel = new Panel
        {
            Name     = "ObjectivesPreview",
            Anchor   = Anchor.TopLeft,
            Position = new Vector2(80f, 660f),
            Size     = new Vector2(1760f, 280f),
        };
        AddWidget(_objectivesPanel);

        // ── Action buttons ────────────────────────────────────────────────────
        _startButton = new Button
        {
            Name     = "StartMission",
            Label    = "Start Mission",
            Anchor   = Anchor.BottomRight,
            Position = new Vector2(-80f, -80f),
            Size     = new Vector2(280f, 56f),
            FontSize = 20f,
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
            FontSize = 20f,
        };
        _backButton.Clicked += () => BackRequested?.Invoke();
        AddWidget(_backButton);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Populate the screen with data from a <see cref="MissionDefinition"/>.
    /// Call before pushing this screen onto the <see cref="UIManager"/> stack.
    /// </summary>
    public void SetMission(MissionDefinition definition)
    {
        // Rebuild briefing panel content
        while (_briefingPanel.Children.Count > 0)
            _briefingPanel.RemoveChild(_briefingPanel.Children[0]);

        // Mission title banner
        _briefingPanel.AddChild(new Button
        {
            Name     = "MissionTitle",
            Label    = definition.DisplayName,
            Anchor   = Anchor.TopLeft,
            Position = new Vector2(16f, 12f),
            Size     = new Vector2(1728f, 48f),
            FontSize = 28f,
            IsEnabled = false,
        });

        // Briefing body text (shown as a disabled button for now — no Label widget yet)
        string briefingText = definition.Briefing?.Text ?? definition.Description;
        _briefingPanel.AddChild(new Button
        {
            Name      = "BriefingBody",
            Label     = briefingText,
            Anchor    = Anchor.TopLeft,
            Position  = new Vector2(16f, 76f),
            Size      = new Vector2(1728f, 400f),
            FontSize  = 18f,
            IsEnabled = false,
        });

        // Rebuild objectives preview
        while (_objectivesPanel.Children.Count > 0)
            _objectivesPanel.RemoveChild(_objectivesPanel.Children[0]);

        var previews = definition.Briefing?.ObjectivesPreview ?? [];
        for (int i = 0; i < previews.Length; i++)
        {
            _objectivesPanel.AddChild(new Button
            {
                Name      = $"Objective_{i}",
                Label     = $"• {previews[i]}",
                Anchor    = Anchor.TopLeft,
                Position  = new Vector2(16f, 16f + i * 52f),
                Size      = new Vector2(1728f, 44f),
                FontSize  = 16f,
                IsEnabled = false,
            });
        }
    }
}
