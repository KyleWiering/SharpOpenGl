using OpenTK.Mathematics;
using SharpOpenGl.Engine.Missions;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Engine.UI.Screens;

/// <summary>Summary data for one mission shown on the galactic star map.</summary>
public sealed class MissionEntry
{
    /// <summary>Unique mission identifier.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Human-readable mission title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Short description shown in the preview area.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Map id from the mission definition (e.g. sector_alpha).</summary>
    public string MapId { get; init; } = string.Empty;

    /// <summary>Briefing narrative shown in the preview panel.</summary>
    public string BriefingText { get; init; } = string.Empty;

    /// <summary>Objective lines shown in the preview panel.</summary>
    public string[] ObjectivesPreview { get; init; } = [];

    /// <summary>Whether the player has already completed this mission.</summary>
    public bool IsCompleted { get; init; }

    /// <summary>Planet label on the star map.</summary>
    public string PlanetName { get; init; } = string.Empty;

    /// <summary>Normalized position on the star map canvas (0–1).</summary>
    public Vector2 StarMapPosition { get; init; }

    /// <summary>Accent colour for the planet node.</summary>
    public Vector4 PlanetColor { get; init; } = new(0.3f, 0.65f, 1f, 1f);

    /// <summary>Mission that must be completed before this system unlocks.</summary>
    public string? PrerequisiteMissionId { get; init; }

    /// <summary>Whether the mission is currently locked on the star map.</summary>
    public bool IsLocked { get; init; }
}

/// <summary>
/// Cinematic galactic star map for mission selection with preview panel and launch controls.
/// </summary>
public sealed class MissionSelectScreen : UIScreen
{
    private readonly List<MissionEntry> _missions = new();
    private MissionEntry? _selected;

    private readonly MenuStarfieldBackground _starfield;
    private readonly StarMapCanvas _starMap;
    private readonly Panel _previewPanel;
    private readonly Button _startButton;
    private readonly Button _backButton;

    /// <inheritdoc/>
    public override string ScreenName => "MissionSelect";

    /// <summary>Fired with the mission ID when the player presses Start Mission.</summary>
    public event Action<string>? MissionStartRequested;

    /// <summary>Fired when the Back button is pressed.</summary>
    public event Action? BackRequested;

    /// <summary>Build the star map mission select layout.</summary>
    public MissionSelectScreen()
    {
        _starfield = new MenuStarfieldBackground(starCount: 260, seed: 91)
        {
            Name = "Starfield",
            Anchor = Anchor.Stretch,
            Position = Vector2.Zero,
        };
        AddWidget(_starfield);

        AddWidget(new Label
        {
            Name = "MissionSelectTitle",
            Text = "GALACTIC COMMAND",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(0f, 24f),
            Size = new Vector2(900f, 48f),
            FontSize = 34f,
            TextColor = MenuTheme.ScreenHeadingColor,
        });

        AddWidget(new Label
        {
            Name = "MissionSelectSubtitle",
            Text = "Select a system to view briefing",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(0f, 68f),
            Size = new Vector2(700f, 28f),
            FontSize = 16f,
            TextColor = MenuTheme.SubtitleColor,
        });

        _starMap = new StarMapCanvas
        {
            Name = "StarMap",
            Anchor = Anchor.TopLeft,
            Position = new Vector2(40f, 110f),
            Size = new Vector2(1320f, 860f),
        };
        _starMap.PlanetSelected += OnPlanetSelected;
        _starMap.PlanetActivated += OnPlanetActivated;
        AddWidget(_starMap);

        _previewPanel = new Panel
        {
            Name = "MissionPreview",
            Anchor = Anchor.TopLeft,
            Position = new Vector2(1380f, 110f),
            Size = new Vector2(500f, 700f),
        };
        MenuTheme.ApplyPanel(_previewPanel);
        AddWidget(_previewPanel);

        _startButton = new Button
        {
            Name = "StartMission",
            Label = "Start Mission",
            Anchor = Anchor.BottomLeft,
            Position = new Vector2(1380f, -80f),
            Size = new Vector2(340f, 60f),
            FontSize = 18f,
            IsEnabled = false,
        };
        MenuTheme.ApplyNavButton(_startButton);
        _startButton.Clicked += OnStartClicked;
        AddWidget(_startButton);

        _backButton = new Button
        {
            Name = "Back",
            Label = "Back",
            Anchor = Anchor.BottomLeft,
            Position = new Vector2(40f, -80f),
            Size = new Vector2(220f, 60f),
            FontSize = 18f,
        };
        MenuTheme.ApplyNavButton(_backButton);
        _backButton.Clicked += () => BackRequested?.Invoke();
        AddWidget(_backButton);
    }

    /// <summary>
    /// Populate the star map. Existing entries are replaced.
    /// </summary>
    /// <param name="missions">Campaign missions with star-map coordinates.</param>
    /// <param name="completedMissionIds">Optional set of completed mission IDs for unlock state.</param>
    public void SetMissions(IEnumerable<MissionEntry> missions, IReadOnlySet<string>? completedMissionIds = null)
    {
        _missions.Clear();
        var completed = completedMissionIds ?? new HashSet<string>();

        foreach (MissionEntry mission in missions)
        {
            bool unlocked = StarMapLogic.IsMissionUnlocked(mission.PrerequisiteMissionId, completed);
            _missions.Add(new MissionEntry
            {
                Id = mission.Id,
                Title = mission.Title,
                Description = mission.Description,
                MapId = mission.MapId,
                BriefingText = mission.BriefingText,
                ObjectivesPreview = mission.ObjectivesPreview,
                IsCompleted = mission.IsCompleted,
                PlanetName = mission.PlanetName,
                StarMapPosition = mission.StarMapPosition,
                PlanetColor = mission.PlanetColor,
                PrerequisiteMissionId = mission.PrerequisiteMissionId,
                IsLocked = !unlocked,
            });
        }

        RebuildStarMap();

        MissionEntry? firstUnlocked = _missions.FirstOrDefault(m => !m.IsLocked);
        if (firstUnlocked != null)
            SelectMission(firstUnlocked);
        else
        {
            _selected = null;
            _startButton.IsEnabled = false;
            RebuildPreview();
        }
    }

    /// <summary>Return star-map nodes used for rendering and hit-testing.</summary>
    internal IReadOnlyList<StarMapNode> GetStarMapNodes()
    {
        return _missions.Select(m => new StarMapNode(
            m.Id,
            string.IsNullOrWhiteSpace(m.PlanetName) ? m.Title : m.PlanetName,
            m.StarMapPosition,
            m.PlanetColor,
            m.PrerequisiteMissionId,
            !m.IsLocked,
            m.IsCompleted)).ToList();
    }

    private void RebuildStarMap() => _starMap.SetNodes(GetStarMapNodes());

    private void OnPlanetSelected(string missionId)
    {
        MissionEntry? entry = _missions.FirstOrDefault(m => m.Id == missionId);
        if (entry == null || entry.IsLocked)
            return;

        SelectMission(entry);
    }

    private void OnPlanetActivated(string missionId)
    {
        MissionEntry? entry = _missions.FirstOrDefault(m => m.Id == missionId);
        if (entry == null || entry.IsLocked)
            return;

        SelectMission(entry);
        MissionStartRequested?.Invoke(entry.Id);
    }

    private void SelectMission(MissionEntry entry)
    {
        _selected = entry;
        _starMap.SetSelectedMission(entry.Id);
        _startButton.IsEnabled = !entry.IsLocked;
        RebuildPreview();
    }

    private void RebuildPreview()
    {
        while (_previewPanel.Children.Count > 0)
            _previewPanel.RemoveChild(_previewPanel.Children[0]);

        if (_selected == null)
        {
            _previewPanel.AddChild(new Label
            {
                Name = "PreviewPlaceholder",
                Text = "Select an unlocked system to view the mission briefing.",
                Anchor = Anchor.TopLeft,
                Position = new Vector2(20f, 20f),
                Size = new Vector2(460f, 80f),
                FontSize = 18f,
                WrapWidth = 440f,
                TextColor = MenuTheme.MutedTextColor,
            });
            return;
        }

        string planetLine = string.IsNullOrWhiteSpace(_selected.PlanetName)
            ? string.Empty
            : $"System: {_selected.PlanetName}";

        _previewPanel.AddChild(new Label
        {
            Name = "PreviewTitle",
            Text = _selected.Title,
            Anchor = Anchor.TopLeft,
            Position = new Vector2(20f, 16f),
            Size = new Vector2(460f, 44f),
            FontSize = 28f,
            TextColor = new Vector4(0.55f, 0.85f, 1f, 1f),
        });

        if (!string.IsNullOrEmpty(planetLine))
        {
            _previewPanel.AddChild(new Label
            {
                Name = "PreviewPlanet",
                Text = planetLine,
                Anchor = Anchor.TopLeft,
                Position = new Vector2(20f, 58f),
                Size = new Vector2(460f, 28f),
                FontSize = 16f,
                TextColor = MenuTheme.MutedTextColor,
            });
        }

        string mapLine = string.IsNullOrWhiteSpace(_selected.MapId)
            ? string.Empty
            : $"Map: {_selected.MapId.Replace('_', ' ')}";
        if (!string.IsNullOrEmpty(mapLine))
        {
            _previewPanel.AddChild(new Label
            {
                Name = "PreviewMap",
                Text = mapLine,
                Anchor = Anchor.TopLeft,
                Position = new Vector2(20f, 88f),
                Size = new Vector2(460f, 24f),
                FontSize = 16f,
                TextColor = new Vector4(0.65f, 0.75f, 0.9f, 1f),
            });
        }

        string body = !string.IsNullOrWhiteSpace(_selected.BriefingText)
            ? _selected.BriefingText
            : _selected.Description;
        _previewPanel.AddChild(new Label
        {
            Name = "PreviewBody",
            Text = body,
            Anchor = Anchor.TopLeft,
            Position = new Vector2(20f, 124f),
            Size = new Vector2(460f, 360f),
            FontSize = 17f,
            WrapWidth = 440f,
            TextColor = MenuTheme.BodyTextColor,
        });

        _previewPanel.AddChild(new Label
        {
            Name = "PreviewObjectivesHeader",
            Text = "OBJECTIVES",
            Anchor = Anchor.TopLeft,
            Position = new Vector2(20f, 500f),
            Size = new Vector2(400f, 28f),
            FontSize = 18f,
            TextColor = new Vector4(0.7f, 0.8f, 1f, 1f),
        });

        string[] objectives = _selected.ObjectivesPreview;
        for (int i = 0; i < objectives.Length; i++)
        {
            _previewPanel.AddChild(new Label
            {
                Name = $"PreviewObjective_{i}",
                Text = $"• {objectives[i]}",
                Anchor = Anchor.TopLeft,
                Position = new Vector2(20f, 532f + i * 32f),
                Size = new Vector2(460f, 30f),
                FontSize = 16f,
                WrapWidth = 440f,
                TextColor = new Vector4(0.88f, 0.9f, 0.95f, 1f),
            });
        }

        if (_selected.IsCompleted)
        {
            _previewPanel.AddChild(new Label
            {
                Name = "PreviewCompleted",
                Text = "✓ Mission completed",
                Anchor = Anchor.TopLeft,
                Position = new Vector2(20f, 640f),
                Size = new Vector2(460f, 28f),
                FontSize = 16f,
                TextColor = new Vector4(1f, 0.85f, 0.3f, 1f),
            });
        }
    }

    private void OnStartClicked()
    {
        if (_selected != null && !_selected.IsLocked)
            MissionStartRequested?.Invoke(_selected.Id);
    }
}

/// <summary>Maps loaded mission definitions into star-map selection entries.</summary>
public static class MissionEntryMapper
{
    private static readonly Vector4 DefaultPlanetColor = new(0.3f, 0.65f, 1f, 1f);

    /// <summary>Convert a <see cref="MissionDefinition"/> into a <see cref="MissionEntry"/>.</summary>
    public static MissionEntry FromDefinition(
        MissionDefinition definition,
        IReadOnlySet<string>? completedMissionIds = null)
    {
        var completed = completedMissionIds ?? new HashSet<string>();

        string title = string.IsNullOrWhiteSpace(definition.DisplayName)
            ? definition.Id.Replace('_', ' ')
            : definition.DisplayName;

        string[] objectives = definition.Briefing?.ObjectivesPreview ?? [];
        if (objectives.Length == 0 && definition.Objectives?.Primary.Length > 0)
            objectives = definition.Objectives.Primary.Select(o => o.Description).ToArray();

        Vector2 position = definition.StarMapPosition.Length >= 2
            ? new Vector2(definition.StarMapPosition[0], definition.StarMapPosition[1])
            : Vector2.Zero;

        bool unlocked = StarMapLogic.IsMissionUnlocked(definition.PrerequisiteMissionId, completed);

        return new MissionEntry
        {
            Id = definition.Id,
            Title = title,
            Description = definition.Description,
            MapId = definition.Map,
            BriefingText = definition.Briefing?.Text ?? definition.Description,
            ObjectivesPreview = objectives,
            IsCompleted = completed.Contains(definition.Id),
            PlanetName = string.IsNullOrWhiteSpace(definition.PlanetName) ? title : definition.PlanetName,
            StarMapPosition = position,
            PlanetColor = StarMapLogic.ParsePlanetColor(definition.PlanetColor, DefaultPlanetColor),
            PrerequisiteMissionId = definition.PrerequisiteMissionId,
            IsLocked = !unlocked,
        };
    }
}