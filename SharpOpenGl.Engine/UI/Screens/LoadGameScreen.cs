using OpenTK.Mathematics;
using SharpOpenGl.Engine.Persistence;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Engine.UI.Screens;

/// <summary>
/// Main-menu screen listing existing save files for full world restore.
/// </summary>
public sealed class LoadGameScreen : UIScreen
{
    private readonly SaveManager _saveManager;
    private readonly ScrollPanel _listPanel;
    private readonly Label _emptyLabel;
    private readonly List<Panel> _entryRows = new();
    private IReadOnlyList<LoadGameListEntry> _entries = [];

    private sealed record LoadGameListEntry(
        SaveSlotInfo Info,
        bool IsSandboxSession,
        string SandboxSeedText,
        int ProceduralMapSeed);

    /// <inheritdoc/>
    public override string ScreenName => "LoadGame";

    /// <summary>Fired with the chosen slot name when the player loads a save.</summary>
    public event Action<string>? LoadRequested;

    /// <summary>Fired when the player returns to the main menu.</summary>
    public event Action? BackRequested;

    /// <summary>Build the load-game layout.</summary>
    public LoadGameScreen(SaveManager saveManager)
    {
        _saveManager = saveManager;

        AddWidget(new MenuStarfieldBackground
        {
            Name = "Starfield",
            Anchor = Anchor.Stretch,
        });

        AddWidget(new Label
        {
            Name = "Title",
            Text = "Load Game",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(0f, 72f),
            Size = new Vector2(700f, 48f),
            FontSize = 40f,
            TextColor = MenuTheme.TitleColor,
        });

        _listPanel = new ScrollPanel
        {
            Name = "SaveList",
            Anchor = Anchor.Center,
            Size = new Vector2(760f, 520f),
        };
        MenuTheme.ApplyPanel(_listPanel);
        AddWidget(_listPanel);

        _emptyLabel = new Label
        {
            Name = "EmptyMessage",
            Text = "No save files found.",
            Anchor = Anchor.Center,
            Position = new Vector2(-250f, -20f),
            Size = new Vector2(500f, 40f),
            FontSize = 22f,
            WrapWidth = UITextDrawing.ContentWrapWidth(500f, 4f),
            TextColor = MenuTheme.SubtitleColor,
        };
        _listPanel.AddChild(_emptyLabel);

        var back = new IconButton
        {
            Name = "Back",
            Icon = MenuIconKind.NavBack,
            Label = "Back",
            TooltipHint = "Return to main menu",
            Layout = IconButtonLayout.IconLeftOfLabel,
            IconSize = IconButton.TitleNavIconSize,
            FontSize = 18f,
            RequireMinimumHitExtent = true,
            Anchor = Anchor.BottomLeft,
            Position = new Vector2(40f, -80f),
            Size = new Vector2(220f, 60f),
        };
        IconButton.ApplyMenuTheme(back, showGlow: true);
        back.Clicked += () => BackRequested?.Invoke();
        AddWidget(back);

        RefreshList();
    }

    /// <summary>Reload save metadata from disk.</summary>
    public void RefreshList()
    {
        foreach (Panel row in _entryRows)
            _listPanel.RemoveChild(row);
        _entryRows.Clear();

        _entries = _saveManager.ListSaveFiles()
            .Select(path =>
            {
                string slotName = Path.GetFileNameWithoutExtension(
                    Path.GetFileNameWithoutExtension(path));
                SaveData? data = _saveManager.Load(slotName);
                var info = new SaveSlotInfo
                {
                    SlotName = slotName,
                    FilePath = path,
                    SavedAt = data?.SavedAt ?? string.Empty,
                    MissionId = data?.MissionId ?? string.Empty,
                    ElapsedMissionTime = data?.ElapsedMissionTime ?? 0f,
                    HasData = data != null,
                };
                return new LoadGameListEntry(
                    info,
                    data?.IsSandboxSession ?? false,
                    data?.SandboxSeedText ?? string.Empty,
                    data?.ProceduralMapSeed ?? 0);
            })
            .Where(e => e.Info.HasData)
            .ToList();

        _emptyLabel.Visible = _entries.Count == 0;
        if (_entries.Count == 0)
        {
            _listPanel.SyncLabelWrapWidths();
            _listPanel.RecalculateContentHeight(_listPanel.Size);
            return;
        }

        float rowY = EntryStartY;
        for (int i = 0; i < _entries.Count; i++)
        {
            LoadGameListEntry entry = _entries[i];
            float rowHeight = BuildEntryRow(entry, i, rowY, out Panel row);
            rowY += rowHeight + EntryRowGap;
            _entryRows.Add(row);
            _listPanel.AddChild(row);
        }

        _listPanel.SyncLabelWrapWidths();
        _listPanel.RecalculateContentHeight(_listPanel.Size);
    }

    /// <summary>Number of populated save entries currently shown.</summary>
    public int EntryCount => _entries.Count;

    private const float EntryRowWidth = 700f;
    private const float EntryRowLeft = 30f;
    private const float EntryRowGap = 10f;
    private const float EntryStartY = 24f;
    private const float EntryRowMinHeight = 56f;
    private const float EntryLabelFontSize = 17f;
    private const float EntryLabelPadding = 4f;
    private const float EntryRowPadding = 8f;
    private const float EntryLineGap = 2f;
    private const float EntryTextLeft =
        IconButton.TitleNavIconColumnWidth + IconButton.LabelPadding;

    private float EntryLabelAreaWidth =>
        EntryRowWidth - EntryTextLeft - IconButton.LabelPadding;

    private float EntryMissionWrapWidth =>
        UITextDrawing.ContentWrapWidth(EntryLabelAreaWidth, EntryLabelPadding);

    private float BuildEntryRow(LoadGameListEntry entry, int index, float rowY, out Panel row)
    {
        row = new Panel
        {
            Name = $"EntryRow{index}",
            Anchor = Anchor.TopLeft,
            Position = new Vector2(EntryRowLeft, rowY),
            Size = new Vector2(EntryRowWidth, EntryRowMinHeight),
            BackgroundColor = Vector4.Zero,
            DrawBorder = false,
        };

        var btn = new IconButton
        {
            Name = $"Entry{index}",
            Icon = MenuIconKind.NavLoadGame,
            Label = string.Empty,
            TooltipHint = "Load this save",
            Layout = IconButtonLayout.IconLeftOfLabel,
            IconSize = IconButton.TitleNavIconSize,
            FontSize = EntryLabelFontSize,
            RequireMinimumHitExtent = true,
            Anchor = Anchor.TopLeft,
            Position = Vector2.Zero,
            Size = new Vector2(EntryRowWidth, EntryRowMinHeight),
        };
        IconButton.ApplyMenuTheme(btn, showGlow: true);
        string slot = entry.Info.SlotName;
        btn.Clicked += () => LoadRequested?.Invoke(slot);
        row.AddChild(btn);

        float labelWidth = EntryLabelAreaWidth;
        float labelY = EntryRowPadding;
        float wrapWidth = EntryMissionWrapWidth;

        var slotLabel = new Label
        {
            Name = $"Entry{index}Slot",
            Text = SaveSlotNames.DisplayName(entry.Info.SlotName),
            Anchor = Anchor.TopLeft,
            Position = new Vector2(EntryTextLeft, labelY),
            Size = new Vector2(labelWidth, 0f),
            FontSize = EntryLabelFontSize,
            Padding = EntryLabelPadding,
            TextColor = MenuTheme.ButtonText,
        };
        row.AddChild(slotLabel);
        labelY += slotLabel.MeasureContentHeight() + EntryLineGap;

        string mission = FormatMissionLabel(entry);
        var missionLabel = new Label
        {
            Name = $"Entry{index}Mission",
            Text = mission,
            Anchor = Anchor.TopLeft,
            Position = new Vector2(EntryTextLeft, labelY),
            Size = new Vector2(labelWidth, 0f),
            FontSize = EntryLabelFontSize,
            Padding = EntryLabelPadding,
            WrapWidth = wrapWidth,
            TextColor = MenuTheme.BodyTextColor,
        };
        row.AddChild(missionLabel);
        labelY += missionLabel.MeasureContentHeight() + EntryLineGap;

        string meta = $"{FormatElapsed(entry.Info.ElapsedMissionTime)}  |  {FormatSavedAt(entry.Info.SavedAt)}";
        var metaLabel = new Label
        {
            Name = $"Entry{index}Meta",
            Text = meta,
            Anchor = Anchor.TopLeft,
            Position = new Vector2(EntryTextLeft, labelY),
            Size = new Vector2(labelWidth, 0f),
            FontSize = EntryLabelFontSize,
            Padding = EntryLabelPadding,
            TextColor = MenuTheme.MutedTextColor,
        };
        row.AddChild(metaLabel);
        labelY += metaLabel.MeasureContentHeight() + EntryRowPadding;

        float rowHeight = MathF.Max(EntryRowMinHeight, labelY);
        row.Size = new Vector2(EntryRowWidth, rowHeight);
        btn.Size = new Vector2(EntryRowWidth, rowHeight);

        return rowHeight;
    }

    private static string FormatMissionLabel(LoadGameListEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.Info.MissionId))
            return entry.Info.MissionId;

        if (entry.IsSandboxSession)
        {
            string seed = !string.IsNullOrWhiteSpace(entry.SandboxSeedText)
                ? entry.SandboxSeedText
                : entry.ProceduralMapSeed.ToString();
            return $"Sandbox · {seed}";
        }

        return "Free Play";
    }

    private static string FormatSavedAt(string savedAt)
    {
        if (string.IsNullOrWhiteSpace(savedAt))
            return "—";

        if (DateTime.TryParse(savedAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime dt))
            return dt.ToLocalTime().ToString("g");

        return savedAt;
    }

    private static string FormatElapsed(float seconds)
    {
        if (seconds <= 0f)
            return "0:00";

        int total = (int)seconds;
        int minutes = total / 60;
        int secs = total % 60;
        return $"{minutes}:{secs:D2}";
    }
}