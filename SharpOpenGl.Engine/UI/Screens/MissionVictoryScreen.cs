using OpenTK.Mathematics;
using SharpOpenGl.Engine.Missions;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Engine.UI.Screens;

/// <summary>
/// Overlay shown when a mission ends in victory or defeat.
/// Keeps the gameplay HUD visible beneath a semi-transparent backdrop.
/// </summary>
public sealed class MissionVictoryScreen : UIScreen
{
    private readonly Panel _card;
    private readonly Label _titleLabel;
    private readonly Label _missionNameLabel;
    private readonly Label _elapsedLabel;
    private readonly Label _xpLabel;
    private readonly Label _defeatReasonLabel;
    private readonly ScrollPanel _objectivesPanel;

    /// <inheritdoc/>
    public override string ScreenName => "MissionVictory";

    /// <inheritdoc/>
    public override bool IsOverlay => true;

    /// <summary>Fired when the player chooses to return to the main menu flow.</summary>
    public event Action? ReturnToMenuRequested;

    /// <summary>Fired when the player chooses to replay the current mission.</summary>
    public event Action? ReplayMissionRequested;

    /// <summary>Build the victory/defeat overlay layout.</summary>
    public MissionVictoryScreen()
    {
        AddWidget(new Panel
        {
            Name = "Backdrop",
            Anchor = Anchor.Stretch,
            Position = Vector2.Zero,
            BackgroundColor = new Vector4(0f, 0f, 0f, 0.55f),
            DrawBorder = false,
        });

        _card = new Panel
        {
            Name = "VictoryCard",
            Anchor = Anchor.Center,
            Position = Vector2.Zero,
            Size = new Vector2(420f, 520f),
            BackgroundColor = new Vector4(0.08f, 0.08f, 0.14f, 0.97f),
        };
        AddWidget(_card);

        _titleLabel = new Label
        {
            Name = "ResultTitle",
            Text = "MISSION COMPLETE",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-180f, 24f),
            Size = new Vector2(360f, 40f),
            FontSize = 26f,
            TextColor = new Vector4(0.45f, 1f, 0.55f, 1f),
        };
        _card.AddChild(_titleLabel);

        _missionNameLabel = new Label
        {
            Name = "MissionName",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-180f, 68f),
            Size = new Vector2(360f, 36f),
            FontSize = 22f,
            TextColor = new Vector4(0.85f, 0.92f, 1f, 1f),
        };
        _card.AddChild(_missionNameLabel);

        _elapsedLabel = new Label
        {
            Name = "ElapsedTime",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-180f, 108f),
            Size = new Vector2(360f, 28f),
            FontSize = 18f,
            TextColor = MenuTheme.BodyTextColor,
        };
        _card.AddChild(_elapsedLabel);

        _xpLabel = new Label
        {
            Name = "XpReward",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-180f, 138f),
            Size = new Vector2(360f, 28f),
            FontSize = 18f,
            TextColor = new Vector4(0.95f, 0.85f, 0.35f, 1f),
        };
        _card.AddChild(_xpLabel);

        _defeatReasonLabel = new Label
        {
            Name = "DefeatReason",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-180f, 138f),
            Size = new Vector2(360f, 48f),
            FontSize = 17f,
            WrapWidth = 340f,
            TextColor = new Vector4(1f, 0.55f, 0.45f, 1f),
            Visible = false,
        };
        _card.AddChild(_defeatReasonLabel);

        _objectivesPanel = new ScrollPanel
        {
            Name = "ObjectivesSummary",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-190f, 176f),
            Size = new Vector2(380f, 220f),
        };
        _card.AddChild(_objectivesPanel);

        const float btnW = 300f;
        const float btnH = 52f;
        const float gap = 12f;
        const float startY = 412f;

        var returnBtn = new Button
        {
            Name = "ReturnToMenu",
            Label = "Return to Menu",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-btnW / 2f, startY),
            Size = new Vector2(btnW, btnH),
            FontSize = 20f,
        };
        returnBtn.Clicked += () => ReturnToMenuRequested?.Invoke();
        _card.AddChild(returnBtn);

        var replayBtn = new Button
        {
            Name = "ReplayMission",
            Label = "Replay Mission",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-btnW / 2f, startY + btnH + gap),
            Size = new Vector2(btnW, btnH),
            FontSize = 20f,
        };
        replayBtn.Clicked += () => ReplayMissionRequested?.Invoke();
        _card.AddChild(replayBtn);
    }

    /// <summary>Populate the overlay from the active mission state.</summary>
    public void SetMissionResult(MissionState state, bool isVictory, string? defeatReason = null)
    {
        string displayName = string.IsNullOrWhiteSpace(state.Definition.DisplayName)
            ? state.Definition.Id.Replace('_', ' ')
            : state.Definition.DisplayName;

        _titleLabel.Text = isVictory ? "MISSION COMPLETE" : "MISSION FAILED";
        _titleLabel.TextColor = isVictory
            ? new Vector4(0.45f, 1f, 0.55f, 1f)
            : new Vector4(1f, 0.45f, 0.4f, 1f);

        _missionNameLabel.Text = displayName;
        _elapsedLabel.Text = $"Time: {FormatElapsedTime(state.ElapsedTime)}";

        int xp = state.Definition.Rewards?.Xp ?? 0;
        _xpLabel.Text = isVictory ? $"XP Earned: {xp}" : string.Empty;
        _xpLabel.Visible = isVictory;

        if (!isVictory && !string.IsNullOrWhiteSpace(defeatReason))
        {
            _defeatReasonLabel.Text = defeatReason;
            _defeatReasonLabel.Visible = true;
        }
        else
        {
            _defeatReasonLabel.Text = string.Empty;
            _defeatReasonLabel.Visible = false;
        }

        RebuildObjectives(state);
    }

    /// <summary>Format mission elapsed time for display.</summary>
    public static string FormatElapsedTime(float seconds)
    {
        if (seconds < 60f)
            return $"{seconds:F1} s";

        int total = Math.Max(0, (int)seconds);
        int minutes = total / 60;
        int secs = total % 60;
        return $"{minutes}:{secs:D2}";
    }

    private void RebuildObjectives(MissionState state)
    {
        while (_objectivesPanel.Children.Count > 0)
            _objectivesPanel.RemoveChild(_objectivesPanel.Children[0]);

        const float padding = 12f;
        float y = padding;
        float contentW = _objectivesPanel.Size.X - padding * 2f;
        float wrapWidth = UITextDrawing.ContentWrapWidth(_objectivesPanel.Size.X, padding);

        _objectivesPanel.AddChild(new Label
        {
            Name = "ObjectivesHeader",
            Text = "PRIMARY OBJECTIVES",
            Anchor = Anchor.TopLeft,
            Position = new Vector2(padding, y),
            Size = new Vector2(contentW, 24f),
            FontSize = 16f,
            TextColor = new Vector4(0.7f, 0.8f, 1f, 1f),
        });
        y += 28f;

        foreach (ObjectiveProgress objective in state.PrimaryObjectives)
        {
            string marker = objective.IsCompleted ? "✓" : "—";
            string description = objective.Description;
            if (string.IsNullOrWhiteSpace(description))
                description = objective.Id.Replace('_', ' ');

            _objectivesPanel.AddChild(new Label
            {
                Name = $"Objective_{objective.Id}",
                Text = $"{marker} {description}",
                Anchor = Anchor.TopLeft,
                Position = new Vector2(padding, y),
                Size = new Vector2(contentW, 40f),
                FontSize = 16f,
                WrapWidth = wrapWidth,
                TextColor = objective.IsCompleted
                    ? new Vector4(0.55f, 0.95f, 0.65f, 1f)
                    : MenuTheme.BodyTextColor,
            });
            y += 36f;
        }

        _objectivesPanel.RecalculateContentHeight(_objectivesPanel.Size);
    }
}