using OpenTK.Mathematics;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Engine.UI.Screens;

/// <summary>Summary data for one mission shown in the selection list.</summary>
public sealed class MissionEntry
{
    /// <summary>Unique mission identifier.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Human-readable mission title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Short description shown in the preview area.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Whether the player has already completed this mission.</summary>
    public bool IsCompleted { get; init; }
}

/// <summary>
/// Screen that lists available missions, shows a preview, and lets the player start one.
/// </summary>
public sealed class MissionSelectScreen : UIScreen
{
    private readonly List<MissionEntry> _missions = new();
    private MissionEntry? _selected;

    // ── Widgets ───────────────────────────────────────────────────────────────

    private readonly Panel _listPanel;
    private readonly Panel _previewPanel;
    private readonly Button _startButton;
    private readonly Button _backButton;

    /// <inheritdoc/>
    public override string ScreenName => "MissionSelect";

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Fired with the mission ID when the player presses Start Mission.</summary>
    public event Action<string>? MissionStartRequested;

    /// <summary>Fired when the Back button is pressed.</summary>
    public event Action? BackRequested;

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>Build the mission select layout.</summary>
    public MissionSelectScreen()
    {
        // ── Left list panel ───────────────────────────────────────────────────
        _listPanel = new Panel
        {
            Name = "MissionList",
            Anchor = Anchor.TopLeft,
            Position = new Vector2(40f, 80f),
            Size = new Vector2(400f, 880f),
        };
        AddWidget(_listPanel);

        // ── Right preview panel ───────────────────────────────────────────────
        _previewPanel = new Panel
        {
            Name = "MissionPreview",
            Anchor = Anchor.TopLeft,
            Position = new Vector2(480f, 80f),
            Size = new Vector2(1380f, 780f),
        };
        AddWidget(_previewPanel);

        // ── Start / Back buttons ──────────────────────────────────────────────
        _startButton = new Button
        {
            Name = "StartMission",
            Label = "Start Mission",
            Anchor = Anchor.BottomLeft,
            Position = new Vector2(480f, -80f),
            Size = new Vector2(280f, 56f),
            FontSize = 20f,
            IsEnabled = false,
        };
        _startButton.Clicked += OnStartClicked;
        AddWidget(_startButton);

        _backButton = new Button
        {
            Name = "Back",
            Label = "Back",
            Anchor = Anchor.BottomLeft,
            Position = new Vector2(40f, -80f),
            Size = new Vector2(200f, 56f),
            FontSize = 20f,
        };
        _backButton.Clicked += () => BackRequested?.Invoke();
        AddWidget(_backButton);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Populate the mission list.  Existing entries are replaced.
    /// Call before pushing this screen.
    /// </summary>
    public void SetMissions(IEnumerable<MissionEntry> missions)
    {
        _missions.Clear();
        _missions.AddRange(missions);
        RebuildList();
        _selected = null;
        _startButton.IsEnabled = false;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void RebuildList()
    {
        // Remove old mission buttons
        while (_listPanel.Children.Count > 0)
            _listPanel.RemoveChild(_listPanel.Children[0]);

        float btnH = 60f;
        float gap = 8f;

        for (int i = 0; i < _missions.Count; i++)
        {
            MissionEntry entry = _missions[i];
            string checkmark = entry.IsCompleted ? "✓ " : string.Empty;

            var btn = new Button
            {
                Name = $"Mission_{entry.Id}",
                Label = checkmark + entry.Title,
                Anchor = Anchor.TopLeft,
                Position = new Vector2(8f, 8f + i * (btnH + gap)),
                Size = new Vector2(384f, btnH),
                FontSize = 16f,
            };
            btn.Clicked += () => SelectMission(entry);
            _listPanel.AddChild(btn);
        }
    }

    private void SelectMission(MissionEntry entry)
    {
        _selected = entry;
        _startButton.IsEnabled = true;
    }

    private void OnStartClicked()
    {
        if (_selected != null)
            MissionStartRequested?.Invoke(_selected.Id);
    }
}
