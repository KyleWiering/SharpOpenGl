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
    private readonly Label _validationLabel;
    private readonly Label _mapLabel;
    private int _mapIndex;

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
            Size = new Vector2(640f, 32f),
            FontSize = 18f,
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
        };
        MenuTheme.ApplyNavButton(mapNext, showGlow: false);
        mapNext.Clicked += () => CycleMap(1);
        AddWidget(mapNext);

        const float columnWidth = 860f;
        const float rowHeight = 96f;
        const float rowStartY = 188f;
        const float leftColumnX = -columnWidth - 20f;
        const float rightColumnX = 20f;

        for (int i = 0; i < SlotCount; i++)
        {
            int column = i / 4;
            int row = i % 4;
            float x = column == 0 ? leftColumnX : rightColumnX;
            float y = rowStartY + row * rowHeight;
            _slotWidgets[i] = CreateSlotRow(i, x, y);
        }

        _validationLabel = new Label
        {
            Name = "MPValidation",
            Text = string.Empty,
            Anchor = Anchor.TopCenter,
            Position = new Vector2(0f, 598f),
            Size = new Vector2(900f, 28f),
            FontSize = 16f,
            TextColor = new Vector4(1f, 0.55f, 0.35f, 1f),
        };
        AddWidget(_validationLabel);

        _startBtn = new Button
        {
            Name = "StartMP",
            Label = "Start Match",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-170f, 638f),
            Size = new Vector2(340f, 58f),
            FontSize = 22f,
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
        };
        MenuTheme.ApplyNavButton(backBtn);
        backBtn.Clicked += () => BackRequested?.Invoke();
        AddWidget(backBtn);

        RefreshMapLabel();
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

    /// <summary>Builds the setup result from the current slot state.</summary>
    public MultiplayerSetupResult? BuildResult() =>
        _skirmishMaps.Length == 0
            ? null
            : MultiplayerSetupLogic.BuildResult(_slots, _raceIds, _skirmishMaps[_mapIndex]);

    /// <summary>Advances skirmish map selection by <paramref name="delta"/>.</summary>
    public void CycleMap(int delta)
    {
        if (_skirmishMaps.Length == 0) return;
        _mapIndex = MultiplayerSetupLogic.CycleMapIndex(_mapIndex, delta, _skirmishMaps.Length);
        RefreshMapLabel();
        RefreshStartState();
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
        };
        MenuTheme.ApplyNavButton(widgets.KindButton, showGlow: false);
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
            Size = new Vector2(300f, 32f),
            FontSize = 18f,
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

        widgets.RaceLabel.Text = active
            ? FormatRaceName(GetSlotRaceId(slotIndex))
            : "—";

        widgets.RaceLabel.Visible = true;
        widgets.RacePrev.Visible = active;
        widgets.RaceNext.Visible = active;
        widgets.RaceLabel.TextColor = active
            ? MenuTheme.BodyTextColor
            : new Vector4(0.45f, 0.48f, 0.55f, 0.8f);
    }

    private void RefreshMapLabel()
    {
        if (_skirmishMaps.Length == 0)
        {
            _mapLabel.Text = "No skirmish maps found";
            return;
        }

        var map = _skirmishMaps[_mapIndex];
        _mapLabel.Text = $"Map: {map.DisplayName} ({map.PlayerCount} players)";
    }

    private void RefreshStartState()
    {
        if (_skirmishMaps.Length == 0)
        {
            _startBtn.IsEnabled = false;
            _validationLabel.Text = "No skirmish maps available.";
            return;
        }

        bool canStart = CanStartMatch;
        _startBtn.IsEnabled = canStart;
        _validationLabel.Text = canStart
            ? string.Empty
            : MultiplayerSetupLogic.DescribeMapValidation(_slots, _skirmishMaps[_mapIndex]);
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