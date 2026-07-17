using OpenTK.Mathematics;
using SharpOpenGl.Engine.Grid;
using SharpOpenGl.Engine.Rendering;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Engine.UI.Screens;

/// <summary>
/// Screen for configuring up to eight multiplayer slots with per-slot race selection.
/// </summary>
public sealed class MultiplayerSetupScreen : UIScreen
{
    public const int SlotCount = MultiplayerSetupLogic.MaxSlots;

    private const float MapLabelWidth = 640f;
    private const float MapLabelPadding = 10f;
    private const float ValidationLabelWidth = 900f;
    private const float ValidationLabelPadding = 10f;
    private const float RaceLabelWidth = 300f;
    private const float RaceLabelPadding = 10f;

    /// <inheritdoc/>
    public override string ScreenName => "MultiplayerSetup";

    /// <summary>Fired when the player wants to start the multiplayer match.</summary>
    public event Action<MultiplayerSetupResult>? StartRequested;

    /// <summary>Fired when the Back button is pressed.</summary>
    public event Action? BackRequested;

    private readonly string[] _raceIds;
    private readonly SkirmishMapEntry[] _skirmishMaps;
    private readonly MultiplayerSlotState[] _slots;
    private readonly SlotWidgets[] _slotWidgets;
    private readonly Button _startBtn;
    private readonly ScrollPanel _validationScroll;
    private readonly Label _validationLabel;
    private readonly Label _mapLabel;
    private readonly Label _difficultyLabel;
    private int _mapIndex;
    private SkirmishDifficultyTier _difficulty = SkirmishDifficultyTier.Normal;

    public MultiplayerSetupScreen()
    {
        _raceIds = MultiplayerSetupLogic.ResolveRaceIds();
        _skirmishMaps = SkirmishMapCatalog.ResolveEntries();
        _slots = MultiplayerSetupLogic.CreateDefaultSlots(_raceIds);
        _slotWidgets = new SlotWidgets[SlotCount];
        _mapIndex = MultiplayerSetupLogic.ResolveDefaultMapIndex(
            _skirmishMaps,
            MultiplayerSetupLogic.CountActiveSlots(_slots));

        AddWidget(new MenuStarfieldBackground
        {
            Name = "MPStarfield",
            Anchor = Anchor.Stretch,
        });

        AddWidget(new Label
        {
            Name = "MPTitle",
            Text = "MULTIPLAYER SETUP",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(0f, 48f),
            Size = new Vector2(900f, 48f),
            FontSize = 32f,
            TextColor = MenuTheme.ScreenHeadingColor,
        });

        AddWidget(new Label
        {
            Name = "MPSubtitle",
            Text = "Up to 8 players — 1 human + up to 7 AI for local skirmish",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(0f, 96f),
            Size = new Vector2(1100f, 32f),
            FontSize = 18f,
            TextColor = MenuTheme.BodyTextColor,
        });

        var mapPrev = new Button
        {
            Name = "MapPrev",
            Label = "<",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-360f, 132f),
            Size = new Vector2(44f, 44f),
            FontSize = 22f,
            RequireMinimumHitExtent = true,
        };
        MenuTheme.ApplyNavButton(mapPrev, showGlow: false);
        mapPrev.Clicked += () => CycleMap(-1);
        AddWidget(mapPrev);

        _mapLabel = new Label
        {
            Name = "MapLabel",
            Text = "Map",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-40f, 138f),
            Size = new Vector2(MapLabelWidth, 32f),
            FontSize = 18f,
            WrapWidth = UITextDrawing.ContentWrapWidth(MapLabelWidth, MapLabelPadding),
            MaxLines = 1,
            TextColor = MenuTheme.BodyTextColor,
        };
        AddWidget(_mapLabel);

        var mapNext = new Button
        {
            Name = "MapNext",
            Label = ">",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(280f, 132f),
            Size = new Vector2(44f, 44f),
            FontSize = 22f,
            RequireMinimumHitExtent = true,
        };
        MenuTheme.ApplyNavButton(mapNext, showGlow: false);
        mapNext.Clicked += () => CycleMap(1);
        AddWidget(mapNext);

        var diffPrev = new Button
        {
            Name = "DiffPrev",
            Label = "<",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-360f, 176f),
            Size = new Vector2(44f, 44f),
            FontSize = 22f,
            RequireMinimumHitExtent = true,
        };
        MenuTheme.ApplyNavButton(diffPrev, showGlow: false);
        diffPrev.Clicked += () => CycleDifficulty(-1);
        AddWidget(diffPrev);

        _difficultyLabel = new Label
        {
            Name = "DifficultyLabel",
            Text = "Difficulty: Normal",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-40f, 182f),
            Size = new Vector2(MapLabelWidth, 32f),
            FontSize = 18f,
            TextColor = MenuTheme.BodyTextColor,
        };
        AddWidget(_difficultyLabel);

        var diffNext = new Button
        {
            Name = "DiffNext",
            Label = ">",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(280f, 176f),
            Size = new Vector2(44f, 44f),
            FontSize = 22f,
            RequireMinimumHitExtent = true,
        };
        MenuTheme.ApplyNavButton(diffNext, showGlow: false);
        diffNext.Clicked += () => CycleDifficulty(1);
        AddWidget(diffNext);

        for (int i = 0; i < SlotCount; i++)
        {
            int column = i / MultiplayerSetupLayout.SlotRowsPerColumn;
            int row = i % MultiplayerSetupLayout.SlotRowsPerColumn;
            float x = column == 0
                ? MultiplayerSetupLayout.LeftColumnX
                : MultiplayerSetupLayout.RightColumnX;
            float y = MultiplayerSetupLayout.SlotRowStartY + row * MultiplayerSetupLayout.SlotRowHeight;
            _slotWidgets[i] = CreateSlotRow(i, x, y);
        }

        _validationScroll = new ScrollPanel
        {
            Name = "MPValidationScroll",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(0f, MultiplayerSetupLayout.ValidationTop),
            Size = new Vector2(ValidationLabelWidth, MultiplayerSetupLayout.ValidationHeight),
            BackgroundColor = new Vector4(0f, 0f, 0f, 0f),
            DrawBorder = false,
            ContentPadding = ValidationLabelPadding,
        };
        _validationLabel = new Label
        {
            Name = "MPValidation",
            Text = string.Empty,
            Anchor = Anchor.TopLeft,
            Position = Vector2.Zero,
            Size = new Vector2(ValidationLabelWidth, 0f),
            FontSize = 16f,
            Padding = ValidationLabelPadding,
            TextColor = new Vector4(1f, 0.55f, 0.35f, 1f),
        };
        _validationScroll.AddChild(_validationLabel);
        AddWidget(_validationScroll);

        _startBtn = new Button
        {
            Name = "StartMP",
            Label = "Start Match",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-170f, MultiplayerSetupLayout.StartButtonTop),
            Size = new Vector2(340f, MultiplayerSetupLayout.StartButtonHeight),
            FontSize = 22f,
            RequireMinimumHitExtent = true,
        };
        MenuTheme.ApplyNavButton(_startBtn);
        _startBtn.Clicked += OnStartClicked;
        AddWidget(_startBtn);

        var backBtn = new Button
        {
            Name = "BackMP",
            Label = "Back",
            Anchor = Anchor.BottomLeft,
            Position = new Vector2(40f, -80f),
            Size = new Vector2(200f, 56f),
            FontSize = 20f,
            RequireMinimumHitExtent = true,
        };
        MenuTheme.ApplyNavButton(backBtn);
        backBtn.Clicked += () => BackRequested?.Invoke();
        AddWidget(backBtn);

        RefreshMapLabel();
        RefreshDifficultyLabel();
        RefreshAllSlots();
        RefreshStartState();
    }

    /// <summary>Whether the current slot configuration can start a match.</summary>
    public bool CanStartMatch =>
        _skirmishMaps.Length > 0 &&
        MultiplayerSetupLogic.CanStart(_slots, _skirmishMaps[_mapIndex]);

    /// <summary>Returns the slot kind for testing and diagnostics.</summary>
    public MultiplayerSlotKind GetSlotKind(int slotIndex) => _slots[slotIndex].Kind;

    /// <summary>Returns the selected race id for a slot.</summary>
    public string GetSlotRaceId(int slotIndex) =>
        MultiplayerSetupLogic.ResolveRaceId(_slots[slotIndex], _raceIds);

    /// <summary>Selected skirmish map id for tests and diagnostics.</summary>
    public string GetSelectedMapId() =>
        _skirmishMaps.Length == 0 ? SkirmishMapCatalog.FallbackMaps[0].Id : _skirmishMaps[_mapIndex].Id;

    /// <summary>Selected skirmish difficulty for tests and diagnostics.</summary>
    public SkirmishDifficultyTier GetDifficulty() => _difficulty;

    /// <summary>Builds the setup result from the current slot state.</summary>
    public MultiplayerSetupResult? BuildResult() =>
        _skirmishMaps.Length == 0
            ? null
            : MultiplayerSetupLogic.BuildResult(_slots, _raceIds, _skirmishMaps[_mapIndex], _difficulty);

    /// <summary>Advances skirmish map selection by <paramref name="delta"/>.</summary>
    public void CycleMap(int delta)
    {
        if (_skirmishMaps.Length == 0) return;
        _mapIndex = MultiplayerSetupLogic.CycleMapIndex(_mapIndex, delta, _skirmishMaps.Length);
        RefreshMapLabel();
        RefreshStartState();
    }

    /// <summary>Advances AI difficulty tier (Easy / Normal / Hard).</summary>
    public void CycleDifficulty(int delta)
    {
        _difficulty = SkirmishDifficultyTuning.Cycle(_difficulty, delta);
        RefreshDifficultyLabel();
    }

    /// <summary>Advances a slot through Empty → Human → AI.</summary>
    public void CycleSlotKind(int slotIndex)
    {
        var slot = _slots[slotIndex];
        slot.Kind = MultiplayerSetupLogic.CycleKind(slot.Kind);
        RefreshSlot(slotIndex);
        RefreshStartState();
    }

    /// <summary>Advances a slot race selection by <paramref name="delta"/>.</summary>
    public void CycleSlotRace(int slotIndex, int delta)
    {
        var slot = _slots[slotIndex];
        slot.RaceIndex = MultiplayerSetupLogic.CycleRaceIndex(slot.RaceIndex, delta, _raceIds.Length);
        RefreshSlot(slotIndex);
    }

    private SlotWidgets CreateSlotRow(int slotIndex, float x, float y)
    {
        var widgets = new SlotWidgets();

        widgets.Header = new Label
        {
            Name = $"Slot{slotIndex}Header",
            Text = $"Player {slotIndex + 1}",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(x, y),
            Size = new Vector2(120f, 28f),
            FontSize = 18f,
            TextColor = MenuTheme.BodyTextColor,
        };
        AddWidget(widgets.Header);

        widgets.KindButton = new Button
        {
            Name = $"Slot{slotIndex}Kind",
            Label = "Empty",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(x + 70f, y + 4f),
            Size = new Vector2(150f, 44f),
            FontSize = 17f,
            RequireMinimumHitExtent = true,
        };
        int captured = slotIndex;
        widgets.KindButton.Clicked += () => CycleSlotKind(captured);
        AddWidget(widgets.KindButton);

        widgets.RacePrev = new Button
        {
            Name = $"Slot{slotIndex}RacePrev",
            Label = "<",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(x + 250f, y + 4f),
            Size = new Vector2(44f, 44f),
            FontSize = 22f,
            RequireMinimumHitExtent = true,
        };
        MenuTheme.ApplyNavButton(widgets.RacePrev, showGlow: false);
        widgets.RacePrev.Clicked += () => CycleSlotRace(captured, -1);
        AddWidget(widgets.RacePrev);

        widgets.RaceLabel = new Label
        {
            Name = $"Slot{slotIndex}Race",
            Text = "Race",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(x + 300f, y + 10f),
            Size = new Vector2(RaceLabelWidth, 32f),
            FontSize = 18f,
            WrapWidth = UITextDrawing.ContentWrapWidth(RaceLabelWidth, RaceLabelPadding),
            MaxLines = 1,
            TextColor = MenuTheme.BodyTextColor,
        };
        AddWidget(widgets.RaceLabel);

        widgets.RaceNext = new Button
        {
            Name = $"Slot{slotIndex}RaceNext",
            Label = ">",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(x + 610f, y + 4f),
            Size = new Vector2(44f, 44f),
            FontSize = 22f,
            RequireMinimumHitExtent = true,
        };
        MenuTheme.ApplyNavButton(widgets.RaceNext, showGlow: false);
        widgets.RaceNext.Clicked += () => CycleSlotRace(captured, 1);
        AddWidget(widgets.RaceNext);

        return widgets;
    }

    private void OnStartClicked()
    {
        var result = BuildResult();
        if (result == null)
        {
            RefreshStartState();
            return;
        }

        StartRequested?.Invoke(result);
    }

    private void RefreshAllSlots()
    {
        for (int i = 0; i < SlotCount; i++)
            RefreshSlot(i);
    }

    private void RefreshSlot(int slotIndex)
    {
        var slot = _slots[slotIndex];
        var widgets = _slotWidgets[slotIndex];
        bool active = MultiplayerSetupLogic.IsActive(slot);

        widgets.KindButton.Label = slot.Kind switch
        {
            MultiplayerSlotKind.Human => "Human",
            MultiplayerSlotKind.Ai => "AI",
            _ => "Empty",
        };
        MenuTheme.ApplySlotKindButton(widgets.KindButton, slot.Kind);

        Vector4? raceAccent = active
            ? MenuTheme.ResolveRaceAccentColor(GetSlotRaceId(slotIndex))
            : null;
        MenuTheme.ApplyRaceToggleButton(widgets.RacePrev, active, raceAccent);
        MenuTheme.ApplyRaceToggleButton(widgets.RaceNext, active, raceAccent);

        widgets.RaceLabel.Text = active
            ? UITextDrawing.TruncateWithEllipsis(
                FormatRaceName(GetSlotRaceId(slotIndex)),
                UITextDrawing.ContentWrapWidth(RaceLabelWidth, RaceLabelPadding),
                18f)
            : "—";

        widgets.RaceLabel.Visible = true;
        widgets.RacePrev.Visible = active;
        widgets.RaceNext.Visible = active;
        widgets.RaceLabel.TextColor = active
            ? MenuTheme.BodyTextColor
            : new Vector4(0.45f, 0.48f, 0.55f, 0.8f);
    }

    private void RefreshDifficultyLabel()
    {
        _difficultyLabel.Text =
            $"Difficulty: {SkirmishDifficultyTuning.DisplayName(_difficulty)}";
    }

    private void RefreshMapLabel()
    {
        if (_skirmishMaps.Length == 0)
        {
            _mapLabel.Text = "No skirmish maps found";
            return;
        }

        var map = _skirmishMaps[_mapIndex];
        _mapLabel.Text = UITextDrawing.TruncateWithEllipsis(
            $"Map: {map.DisplayName} ({map.PlayerCount} players)",
            UITextDrawing.ContentWrapWidth(MapLabelWidth, MapLabelPadding),
            18f);
    }

    private void RefreshStartState()
    {
        if (_skirmishMaps.Length == 0)
        {
            _startBtn.IsEnabled = false;
            _validationLabel.Text = "No skirmish maps available.";
            RefreshValidationScroll();
            return;
        }

        bool canStart = CanStartMatch;
        _startBtn.IsEnabled = canStart;
        _validationLabel.Text = canStart
            ? string.Empty
            : MultiplayerSetupLogic.DescribeMapValidation(_slots, _skirmishMaps[_mapIndex]);
        RefreshValidationScroll();
    }

    private void RefreshValidationScroll()
    {
        _validationScroll.SyncLabelWrapWidths();

        if (string.IsNullOrEmpty(_validationLabel.Text))
        {
            _validationLabel.Size = new Vector2(_validationScroll.Size.X, 0f);
            _validationScroll.RecalculateContentHeight(_validationScroll.Size);
            return;
        }

        float labelHeight = _validationLabel.MeasureContentHeight();
        _validationLabel.Size = new Vector2(_validationScroll.Size.X, labelHeight);
        _validationScroll.RecalculateContentHeight(_validationScroll.Size);
    }

    private static string FormatRaceName(string raceId)
    {
        if (RaceVisualSchema.TryGetRace(raceId, out var race) &&
            !string.IsNullOrWhiteSpace(race.DisplayName))
            return race.DisplayName;

        return raceId.Replace('_', ' ');
    }

    private sealed class SlotWidgets
    {
        public Label Header { get; set; } = null!;
        public Button KindButton { get; set; } = null!;
        public Button RacePrev { get; set; } = null!;
        public Label RaceLabel { get; set; } = null!;
        public Button RaceNext { get; set; } = null!;
    }
}