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
    private readonly List<IconButton> _entryButtons = new();
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
        foreach (IconButton button in _entryButtons)
            _listPanel.RemoveChild(button);
        _entryButtons.Clear();

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
            _listPanel.RecalculateContentHeight(_listPanel.Size);
            return;
        }

        const float btnW = 700f;
        const float btnH = 56f;
        const float gap = 10f;
        const float startY = 24f;
        const float labelFontSize = 17f;

        for (int i = 0; i < _entries.Count; i++)
        {
            LoadGameListEntry entry = _entries[i];
            var btn = new IconButton
            {
                Name = $"Entry{i}",
                Icon = MenuIconKind.NavLoadGame,
                Label = FormatEntryLabel(entry),
                TooltipHint = "Load this save",
                Layout = IconButtonLayout.IconLeftOfLabel,
                IconSize = IconButton.TitleNavIconSize,
                FontSize = labelFontSize,
                RequireMinimumHitExtent = true,
                Anchor = Anchor.TopLeft,
                Position = new Vector2(30f, startY + i * (btnH + gap)),
                Size = new Vector2(btnW, btnH),
            };
            IconButton.ApplyMenuTheme(btn, showGlow: true);
            string slot = entry.Info.SlotName;
            btn.Clicked += () => LoadRequested?.Invoke(slot);
            _listPanel.AddChild(btn);
            _entryButtons.Add(btn);
        }

        _listPanel.RecalculateContentHeight(_listPanel.Size);
    }

    /// <summary>Number of populated save entries currently shown.</summary>
    public int EntryCount => _entries.Count;

    private const float EntryButtonWidth = 700f;
    private const float EntryLabelFontSize = 17f;
    private const float EntryLabelInnerWidth =
        EntryButtonWidth - IconButton.TitleNavIconColumnWidth - IconButton.LabelPadding;

    private static string FormatEntryLabel(LoadGameListEntry entry)
    {
        string slot = SaveSlotNames.DisplayName(entry.Info.SlotName);
        string mission = FormatMissionLabel(entry);
        string when = FormatSavedAt(entry.Info.SavedAt);
        string elapsed = FormatElapsed(entry.Info.ElapsedMissionTime);

        string slotPart = $"{slot}  |  ";
        string tailPart = $"  |  {elapsed}  |  {when}";
        float slotWidth = UIFontMetrics.MeasureTextWidth(slotPart, EntryLabelFontSize);
        float tailWidth = UIFontMetrics.MeasureTextWidth(tailPart, EntryLabelFontSize);
        float missionBudget = EntryLabelInnerWidth - slotWidth - tailWidth;

        string missionFit = missionBudget > 0f
            ? UITextDrawing.TruncateWithEllipsis(mission, missionBudget, EntryLabelFontSize)
            : "…";

        return $"{slotPart}{missionFit}{tailPart}";
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