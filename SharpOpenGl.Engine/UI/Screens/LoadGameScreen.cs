using OpenTK.Mathematics;
using SharpOpenGl.Engine.Persistence;
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
    private readonly List<Button> _entryButtons = new();
    private IReadOnlyList<SaveSlotInfo> _entries = [];

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
            BackgroundColor = new Vector4(0.08f, 0.08f, 0.14f, 0.92f),
        };
        AddWidget(_listPanel);

        _emptyLabel = new Label
        {
            Name = "EmptyMessage",
            Text = "No save files found.",
            Anchor = Anchor.Center,
            Position = new Vector2(-250f, -20f),
            Size = new Vector2(500f, 40f),
            FontSize = 22f,
            TextColor = MenuTheme.SubtitleColor,
        };
        _listPanel.AddChild(_emptyLabel);

        var back = new Button
        {
            Name = "Back",
            Label = "Back",
            Anchor = Anchor.BottomCenter,
            Position = new Vector2(-180f, -48f),
            Size = new Vector2(360f, 56f),
            FontSize = 20f,
        };
        MenuTheme.ApplyNavButton(back);
        back.Clicked += () => BackRequested?.Invoke();
        AddWidget(back);

        RefreshList();
    }

    /// <summary>Reload save metadata from disk.</summary>
    public void RefreshList()
    {
        foreach (Button button in _entryButtons)
            _listPanel.RemoveChild(button);
        _entryButtons.Clear();

        _entries = _saveManager.ListSaveFiles()
            .Select(path =>
            {
                string slotName = Path.GetFileNameWithoutExtension(
                    Path.GetFileNameWithoutExtension(path));
                SaveData? data = _saveManager.Load(slotName);
                return new SaveSlotInfo
                {
                    SlotName = slotName,
                    FilePath = path,
                    SavedAt = data?.SavedAt ?? string.Empty,
                    MissionId = data?.MissionId ?? string.Empty,
                    ElapsedMissionTime = data?.ElapsedMissionTime ?? 0f,
                    HasData = data != null,
                };
            })
            .Where(e => e.HasData)
            .ToList();

        _emptyLabel.Visible = _entries.Count == 0;
        if (_entries.Count == 0)
            return;

        const float btnW = 700f;
        const float btnH = 56f;
        const float gap = 10f;
        const float startY = 24f;
        const float labelFontSize = 17f;
        float labelMaxWidth = UITextDrawing.ContentWrapWidth(btnW, 12f);

        for (int i = 0; i < _entries.Count; i++)
        {
            SaveSlotInfo entry = _entries[i];
            var btn = new Button
            {
                Name = $"Entry{i}",
                Label = UITextDrawing.TruncateWithEllipsis(
                    FormatEntryLabel(entry), labelMaxWidth, labelFontSize),
                Anchor = Anchor.TopLeft,
                Position = new Vector2(30f, startY + i * (btnH + gap)),
                Size = new Vector2(btnW, btnH),
                FontSize = labelFontSize,
            };
            string slot = entry.SlotName;
            btn.Clicked += () => LoadRequested?.Invoke(slot);
            _listPanel.AddChild(btn);
            _entryButtons.Add(btn);
        }

        _listPanel.RecalculateContentHeight(_listPanel.Size);
    }

    /// <summary>Number of populated save entries currently shown.</summary>
    public int EntryCount => _entries.Count;

    private static string FormatEntryLabel(SaveSlotInfo entry)
    {
        string slot = SaveSlotNames.DisplayName(entry.SlotName);
        string mission = string.IsNullOrWhiteSpace(entry.MissionId) ? "Free Play" : entry.MissionId;
        string when = FormatSavedAt(entry.SavedAt);
        string elapsed = FormatElapsed(entry.ElapsedMissionTime);
        return $"{slot}  |  {mission}  |  {elapsed}  |  {when}";
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