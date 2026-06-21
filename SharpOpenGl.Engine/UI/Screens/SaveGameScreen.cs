using OpenTK.Mathematics;
using SharpOpenGl.Engine.Persistence;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Engine.UI.Screens;

/// <summary>
/// Pause-menu overlay for choosing a save slot or quick-saving.
/// </summary>
public sealed class SaveGameScreen : UIScreen
{
    private readonly SaveManager _saveManager;
    private readonly List<Button> _slotButtons = new();
    /// <inheritdoc/>
    public override string ScreenName => "SaveGame";

    /// <inheritdoc/>
    public override bool IsOverlay => true;

    /// <summary>Fired after a save completes successfully.</summary>
    public event Action<string>? SaveCompleted;

    /// <summary>Fired when the player cancels back to the pause menu.</summary>
    public event Action? Cancelled;

    /// <summary>Builds the slot-picker layout.</summary>
    public SaveGameScreen(SaveManager saveManager)
    {
        _saveManager = saveManager;
        BuildLayout();
        RefreshSlots();
    }

    /// <summary>Request a save into <paramref name="slotName"/> using the supplied snapshot builder.</summary>
    public void RequestSave(string slotName, Func<SaveData> buildSnapshot)
    {
        if (_saveManager.SlotExists(slotName))
        {
            ShowOverwriteConfirmation(slotName, buildSnapshot);
            return;
        }

        PerformSave(slotName, buildSnapshot);
    }

    private void BuildLayout()
    {
        AddWidget(new Panel
        {
            Name = "Backdrop",
            Anchor = Anchor.Stretch,
            BackgroundColor = new Vector4(0f, 0f, 0f, 0.55f),
            DrawBorder = false,
        });

        var card = new Panel
        {
            Name = "SaveCard",
            Anchor = Anchor.Center,
            Size = new Vector2(520f, 620f),
            BackgroundColor = new Vector4(0.08f, 0.08f, 0.14f, 0.97f),
        };
        AddWidget(card);

        card.AddChild(new Label
        {
            Name = "Title",
            Text = "Save Game",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-200f, 18f),
            Size = new Vector2(400f, 40f),
            FontSize = 26f,
            TextColor = MenuTheme.TitleColor,
        });

        float btnW = 420f;
        float btnH = 48f;
        float gap = 10f;
        float startY = 72f;

        var quickSave = new Button
        {
            Name = "QuickSave",
            Label = "Quick Save",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-btnW / 2f, startY),
            Size = new Vector2(btnW, btnH),
            FontSize = 18f,
        };
        quickSave.Clicked += () => OnSlotClicked(SaveSlotNames.Autosave);
        card.AddChild(quickSave);
        _slotButtons.Add(quickSave);

        for (int i = 0; i < SaveSlotNames.ManualSlots.Length; i++)
        {
            string slot = SaveSlotNames.ManualSlots[i];
            var btn = new Button
            {
                Name = slot,
                Label = SaveSlotNames.DisplayName(slot),
                Anchor = Anchor.TopCenter,
                Position = new Vector2(-btnW / 2f, startY + (i + 1) * (btnH + gap)),
                Size = new Vector2(btnW, btnH),
                FontSize = 18f,
            };
            btn.Clicked += () => OnSlotClicked(slot);
            card.AddChild(btn);
            _slotButtons.Add(btn);
        }

        var back = new Button
        {
            Name = "Back",
            Label = "Back",
            Anchor = Anchor.BottomCenter,
            Position = new Vector2(-btnW / 2f, -24f),
            Size = new Vector2(btnW, btnH),
            FontSize = 18f,
        };
        back.Clicked += () => Cancelled?.Invoke();
        card.AddChild(back);
    }

    private void RefreshSlots()
    {
        var slots = _saveManager.ListSaveSlots().ToDictionary(s => s.SlotName);

        foreach (Button button in _slotButtons)
        {
            string slotKey = button.Name ?? string.Empty;
            if (!slots.TryGetValue(slotKey, out SaveSlotInfo? info) || !info.HasData)
            {
                if (slotKey == SaveSlotNames.Autosave)
                    button.Label = "Quick Save";
                else
                    button.Label = $"{SaveSlotNames.DisplayName(slotKey)} — Empty";
                continue;
            }

            string mission = string.IsNullOrWhiteSpace(info.MissionId) ? "Free Play" : info.MissionId;
            string when = FormatSavedAt(info.SavedAt);
            button.Label = $"{SaveSlotNames.DisplayName(slotKey)} — {mission} ({when})";
        }
    }

    private void OnSlotClicked(string slotName)
    {
        SlotSelected?.Invoke(slotName);
    }

    /// <summary>Fired when the player picks a slot; host supplies snapshot and calls <see cref="RequestSave"/>.</summary>
    public event Action<string>? SlotSelected;

    private void ShowOverwriteConfirmation(string slotName, Func<SaveData> buildSnapshot)
    {
        var dialog = new Panel
        {
            Name = "ConfirmDialog",
            Anchor = Anchor.Center,
            Size = new Vector2(420f, 220f),
            BackgroundColor = new Vector4(0.12f, 0.1f, 0.18f, 0.98f),
        };

        dialog.AddChild(new Label
        {
            Name = "ConfirmText",
            Text = $"Overwrite {SaveSlotNames.DisplayName(slotName)}?",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-180f, 24f),
            Size = new Vector2(360f, 60f),
            FontSize = 20f,
            WrapWidth = 340f,
        });

        var yes = new Button
        {
            Name = "ConfirmYes",
            Label = "Overwrite",
            Anchor = Anchor.BottomCenter,
            Position = new Vector2(-170f, -70f),
            Size = new Vector2(150f, 48f),
            FontSize = 18f,
        };
        yes.Clicked += () =>
        {
            RemoveWidget(dialog);
            PerformSave(slotName, buildSnapshot);
        };
        dialog.AddChild(yes);

        var no = new Button
        {
            Name = "ConfirmNo",
            Label = "Cancel",
            Anchor = Anchor.BottomCenter,
            Position = new Vector2(20f, -70f),
            Size = new Vector2(150f, 48f),
            FontSize = 18f,
        };
        no.Clicked += () =>
        {
            RemoveWidget(dialog);
        };
        dialog.AddChild(no);

        AddWidget(dialog);
    }

    private void PerformSave(string slotName, Func<SaveData> buildSnapshot)
    {
        SaveData data = buildSnapshot();
        data.SlotName = slotName;
        if (_saveManager.Save(data))
        {
            RefreshSlots();
            SaveCompleted?.Invoke(slotName);
        }
    }

    private static string FormatSavedAt(string savedAt)
    {
        if (string.IsNullOrWhiteSpace(savedAt))
            return "unknown time";

        if (DateTime.TryParse(savedAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime dt))
            return dt.ToLocalTime().ToString("g");

        return savedAt;
    }
}