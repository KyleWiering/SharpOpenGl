using OpenTK.Mathematics;
using SharpOpenGl.Engine.Missions;
using SharpOpenGl.Engine.UI;
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
    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;
    private readonly StarMapCanvas _starMap;
    private readonly ScrollPanel _previewPanel;
    private const float PreviewPadding = 20f;
    private const float LabelPadding = 4f;
    private const float PreviewEmptyIconSize = 24f;
    private const float PreviewMicroIconSize = 18f;
    private const float PreviewIconGap = 8f;
    private const float PreviewTitleGap = 4f;
    private const float PreviewMetaGap = 4f;
    private const float PreviewSectionGap = 8f;
    private const int PreviewObjectiveMaxLines = 4;
    private const float PreviewObjectiveItemGap = 6f;
    private readonly IconButton _startButton;
    private readonly IconButton _backButton;

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

        _titleLabel = new Label
        {
            Name = "MissionSelectTitle",
            Text = "GALACTIC COMMAND",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(0f, 24f),
            Size = new Vector2(900f, 48f),
            FontSize = 34f,
            TextColor = MenuTheme.ScreenHeadingColor,
        };
        AddWidget(_titleLabel);

        _subtitleLabel = new Label
        {
            Name = "MissionSelectSubtitle",
            Text = "Select a system to view briefing",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(0f, 68f),
            Size = new Vector2(700f, 28f),
            FontSize = 16f,
            TextColor = MenuTheme.SubtitleColor,
        };
        AddWidget(_subtitleLabel);

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

        _previewPanel = new ScrollPanel
        {
            Name = "MissionPreview",
            Anchor = Anchor.TopLeft,
            Position = new Vector2(1380f, 110f),
            Size = new Vector2(500f, 700f),
            AutoSyncWrapWidths = false,
        };
        MenuTheme.ApplyPanel(_previewPanel);
        AddWidget(_previewPanel);

        _startButton = new IconButton
        {
            Name = "StartMission",
            Icon = MenuIconKind.NavStartMission,
            Label = "Start Mission",
            TooltipHint = "Launch selected mission",
            Anchor = Anchor.BottomLeft,
            Position = new Vector2(1380f, -80f),
            Size = new Vector2(340f, 60f),
            Layout = IconButtonLayout.IconLeftOfLabel,
            IconSize = IconButton.TitleNavIconSize,
            FontSize = 18f,
            RequireMinimumHitExtent = true,
            IsEnabled = false,
        };
        IconButton.ApplyMenuTheme(_startButton, showGlow: true);
        _startButton.Clicked += OnStartClicked;
        AddWidget(_startButton);

        _backButton = new IconButton
        {
            Name = "Back",
            Icon = MenuIconKind.NavBack,
            Label = "Back",
            TooltipHint = "Return to main menu",
            Anchor = Anchor.BottomLeft,
            Position = new Vector2(40f, -80f),
            Size = new Vector2(220f, 60f),
            Layout = IconButtonLayout.IconLeftOfLabel,
            IconSize = IconButton.TitleNavIconSize,
            FontSize = 18f,
            RequireMinimumHitExtent = true,
        };
        IconButton.ApplyMenuTheme(_backButton, showGlow: true);
        _backButton.Clicked += () => BackRequested?.Invoke();
        AddWidget(_backButton);
    }

    /// <inheritdoc/>
    public override void Draw(IUIRenderer renderer)
    {
        UIScaler? scaler = renderer.ViewportSize != UIScaler.ReferenceSize
            ? new UIScaler(renderer.ViewportSize)
            : null;
        SyncChromeLayout(scaler);
        SyncPreviewContentLayout(scaler);
        base.Draw(renderer);
    }

    private void SyncChromeLayout(UIScaler? scaler)
    {
        Vector2 viewport = UIScaler.ReferenceSize;
        float titleW = MathF.Min(900f, viewport.X - 80f);
        _titleLabel.Size = new Vector2(titleW, 48f);
        _titleLabel.WrapWidth = UITextDrawing.ScaleAwareWrapWidth(
            UITextDrawing.ContentWrapWidth(titleW, 4f), _titleLabel.FontSize, scaler);

        float subtitleW = MathF.Min(700f, viewport.X - 80f);
        _subtitleLabel.Size = new Vector2(subtitleW, 28f);
        _subtitleLabel.WrapWidth = UITextDrawing.ScaleAwareWrapWidth(
            UITextDrawing.ContentWrapWidth(subtitleW, 4f), _subtitleLabel.FontSize, scaler);
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

    /// <summary>Whether a mission entry is locked after the last <see cref="SetMissions"/> call.</summary>
    internal bool IsMissionLocked(string missionId) =>
        _missions.FirstOrDefault(m => m.Id == missionId)?.IsLocked ?? true;

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
        ResetPreviewScroll();

        while (_previewPanel.Children.Count > 0)
            _previewPanel.RemoveChild(_previewPanel.Children[0]);

        float contentW = _previewPanel.Size.X - PreviewPadding * 2f;

        if (_selected == null)
        {
            _previewPanel.AddChild(new PreviewIconRow
            {
                Name = "PreviewPlaceholder",
                Icon = MenuIconKind.NavBriefing,
                Text = "Select an unlocked system to view the mission briefing.",
                Anchor = Anchor.TopLeft,
                Position = new Vector2(PreviewPadding, PreviewPadding),
                Size = new Vector2(contentW, 80f),
                IconSize = PreviewEmptyIconSize,
                IconGap = PreviewIconGap,
                FontSize = 18f,
                TextColor = MenuTheme.MutedTextColor,
            });
            SyncPreviewContentLayout(scaler: null);
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
            Position = new Vector2(PreviewPadding, PreviewPadding),
            Size = new Vector2(contentW, 52f),
            FontSize = 28f,
            MaxLines = 2,
            TextColor = new Vector4(0.55f, 0.85f, 1f, 1f),
        });

        if (!string.IsNullOrEmpty(planetLine))
        {
            _previewPanel.AddChild(new Label
            {
                Name = "PreviewPlanet",
                Text = planetLine,
                Anchor = Anchor.TopLeft,
                Position = new Vector2(PreviewPadding, 0f),
                Size = new Vector2(contentW, 0f),
                FontSize = 16f,
                MaxLines = 1,
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
                Position = new Vector2(PreviewPadding, 0f),
                Size = new Vector2(contentW, 0f),
                FontSize = 16f,
                MaxLines = 1,
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
            Position = new Vector2(PreviewPadding, 0f),
            Size = new Vector2(contentW, 0f),
            FontSize = 17f,
            TextColor = MenuTheme.BodyTextColor,
        });

        _previewPanel.AddChild(new PreviewIconRow
        {
            Name = "PreviewObjectivesHeader",
            Icon = MenuIconKind.NavObjectives,
            Text = "OBJECTIVES",
            Anchor = Anchor.TopLeft,
            Position = new Vector2(PreviewPadding, 0f),
            Size = new Vector2(contentW, 28f),
            IconSize = PreviewMicroIconSize,
            IconGap = PreviewIconGap,
            FontSize = 18f,
            MaxLines = 1,
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
                Position = new Vector2(PreviewPadding, 0f),
                Size = new Vector2(contentW, 0f),
                FontSize = 16f,
                MaxLines = PreviewObjectiveMaxLines,
                TextColor = new Vector4(0.88f, 0.9f, 0.95f, 1f),
            });
        }

        if (_selected.IsCompleted)
        {
            _previewPanel.AddChild(new PreviewIconRow
            {
                Name = "PreviewCompleted",
                Icon = MenuIconKind.NavCompleted,
                Text = "Mission completed",
                Anchor = Anchor.TopLeft,
                Position = new Vector2(PreviewPadding, 0f),
                Size = new Vector2(contentW, 0f),
                IconSize = PreviewMicroIconSize,
                IconGap = PreviewIconGap,
                FontSize = 16f,
                MaxLines = 1,
                TextColor = new Vector4(1f, 0.85f, 0.3f, 1f),
            });
        }

        SyncPreviewContentLayout(scaler: null);
    }

    private void SyncPreviewContentLayout(UIScaler? scaler)
    {
        float panelW = _previewPanel.Size.X;
        float contentW = panelW - PreviewPadding * 2f;
        float gutter = _previewPanel.ShowScrollbar ? 10f : 0f;

        foreach (Widget child in _previewPanel.Children)
        {
            if (child is Label label)
            {
                label.Size = new Vector2(contentW, label.Size.Y);
                float baseWrap = UITextDrawing.ContentWrapWidth(label.Size.X - gutter, label.Padding);
                label.WrapWidth = UITextDrawing.ScaleAwareWrapWidth(baseWrap, label.FontSize, scaler);

                if (label.Name is "PreviewBody"
                    || (label.Name?.StartsWith("PreviewObjective_", StringComparison.Ordinal) ?? false)
                    || label.Name is "PreviewPlanet" or "PreviewMap")
                {
                    label.Size = new Vector2(contentW, label.MeasureContentHeight());
                }
                else if (label.Name == "PreviewTitle")
                {
                    label.Size = new Vector2(contentW, MathF.Max(52f, label.MeasureContentHeight()));
                }
            }
            else if (child is PreviewIconRow row)
            {
                row.Size = new Vector2(contentW, row.Size.Y);
                float textW = MathF.Max(0f, contentW - row.IconSize - row.IconGap - gutter);
                row.WrapWidth = UITextDrawing.ScaleAwareWrapWidth(textW, row.FontSize, scaler);
                row.Size = new Vector2(contentW, row.MeasureContentHeight());
            }
        }

        RelayoutPreviewChildren();
        _previewPanel.RecalculateContentHeight(_previewPanel.Size);
    }

    private void RelayoutPreviewChildren()
    {
        float y = PreviewPadding;
        foreach (Widget child in _previewPanel.Children)
        {
            child.Position = new Vector2(PreviewPadding, y);
            y += child.Size.Y + PreviewGapAfter(child);
        }
    }

    private static float PreviewGapAfter(Widget child) =>
        child switch
        {
            Label { Name: "PreviewTitle" } => PreviewTitleGap,
            Label { Name: "PreviewPlanet" or "PreviewMap" } => PreviewMetaGap,
            Label { Name: "PreviewBody" } => PreviewSectionGap,
            PreviewIconRow { Name: "PreviewObjectivesHeader" } => 4f,
            Label label when label.Name?.StartsWith("PreviewObjective_", StringComparison.Ordinal) == true =>
                PreviewObjectiveItemGap,
            _ => PreviewSectionGap,
        };

    private void ResetPreviewScroll()
    {
        float offset = _previewPanel.ScrollOffsetY;
        if (offset > 0f)
            _previewPanel.ScrollBy(-offset, _previewPanel.Size);
    }

    private void OnStartClicked()
    {
        if (_selected != null && !_selected.IsLocked)
            MissionStartRequested?.Invoke(_selected.Id);
    }

    /// <summary>Icon-left preview row for briefing, objectives, and completion affordances.</summary>
    private sealed class PreviewIconRow : Widget
    {
        public MenuIconKind Icon { get; init; }
        public string Text { get; init; } = string.Empty;
        public float FontSize { get; init; } = 16f;
        public Vector4 TextColor { get; init; } = MenuTheme.BodyTextColor;
        public float IconSize { get; init; } = PreviewMicroIconSize;
        public float IconGap { get; init; } = PreviewIconGap;
        public float WrapWidth { get; set; }
        public int MaxLines { get; init; }

        public float MeasureContentHeight()
        {
            if (string.IsNullOrEmpty(Text))
                return Size.Y;

            float textW = WrapWidth > 0f
                ? WrapWidth
                : MathF.Max(0f, Size.X - IconSize - IconGap);
            int lineCount = MaxLines > 0
                ? UITextDrawing.WrapTextLimited(Text, textW, FontSize, MaxLines).Count
                : UITextDrawing.WrapText(Text, textW, FontSize).Count;
            float textH = lineCount * FontSize * UITextDrawing.LineHeightFactor;
            return MathF.Max(IconSize, textH);
        }

        protected override void OnDraw(IUIRenderer renderer, Vector2 position, Vector2 size)
        {
            if (string.IsNullOrEmpty(Text))
                return;

            float iconY = position.Y + MathF.Max(0f, (size.Y - IconSize) * 0.5f);
            var iconPos = new Vector2(position.X, iconY);
            var primary = TextColor;
            var accent = MenuTheme.ButtonBorderHover;
            MenuIconDrawing.Draw(renderer, Icon, iconPos, IconSize, primary, accent);

            float textX = position.X + IconSize + IconGap;
            float textW = MathF.Max(0f, size.X - IconSize - IconGap);
            float wrap = WrapWidth > 0f ? MathF.Min(WrapWidth, textW) : textW;
            float maxHeight = MathF.Max(0f, size.Y);
            UITextDrawing.DrawTextBlock(renderer, Text, new Vector2(textX, position.Y), FontSize, TextColor,
                wrap, MaxLines, maxHeight);
        }
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