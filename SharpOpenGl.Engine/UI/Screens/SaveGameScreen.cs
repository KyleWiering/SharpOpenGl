using OpenTK.Mathematics;
using SharpOpenGl.Engine.Persistence;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Widgets;

namespace SharpOpenGl.Engine.UI.Screens;

/// <summary>
/// Pause-menu overlay for choosing a save slot or quick-saving.
/// </summary>
public sealed class SaveGameScreen : UIScreen
{
    private readonly SaveManager _saveManager;
    private ScrollPanel _slotScroll = null!;
    private readonly List<IconButton> _slotButtons = new();
    private const float SlotLabelFontSize = 18f;
    private const float SlotButtonWidth = 420f;
    private const float SlotButtonHeight = 48f;


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
            Size = new Vector2(520f, 560f),
        };
        MenuTheme.ApplyPanel(card);
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

        _slotScroll = new ScrollPanel
        {
            Name = "SlotScroll",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-SlotButtonWidth / 2f, 72f),
            Size = new Vector2(SlotButtonWidth, 470f),
            BackgroundColor = Vector4.Zero,
            DrawBorder = false,
        };
        card.AddChild(_slotScroll);

        var quickSave = new IconButton
        {
            Name = "QuickSave",
            Icon = MenuIconKind.NavSave,
            Label = "Quick Save",
            TooltipHint = "Save to quick-save slot",
            Layout = IconButtonLayout.IconLeftOfLabel,
            IconSize = IconButton.TitleNavIconSize,
            FontSize = SlotLabelFontSize,
            RequireMinimumHitExtent = true,
            Anchor = Anchor.TopLeft,
            Position = new Vector2(0f, 0f),
            Size = new Vector2(SlotButtonWidth, SlotButtonHeight),
        };
        IconButton.ApplyMenuTheme(quickSave, showGlow: true);
        quickSave.Clicked += () => OnSlotClicked(SaveSlotNames.Autosave);
        _slotScroll.AddChild(quickSave);
        _slotButtons.Add(quickSave);

        for (int i = 0; i < SaveSlotNames.ManualSlots.Length; i++)
        {
            string slot = SaveSlotNames.ManualSlots[i];
            var btn = new IconButton
            {
                Name = slot,
                Icon = MenuIconKind.NavSave,
                Label = SaveSlotNames.DisplayName(slot),
                TooltipHint = $"Save to {SaveSlotNames.DisplayName(slot)}",
                Layout = IconButtonLayout.IconLeftOfLabel,
                IconSize = IconButton.TitleNavIconSize,
                FontSize = SlotLabelFontSize,
                RequireMinimumHitExtent = true,
                Anchor = Anchor.TopLeft,
                Position = new Vector2(0f, 0f),
                Size = new Vector2(SlotButtonWidth, SlotButtonHeight),
            };
            IconButton.ApplyMenuTheme(btn, showGlow: true);
            btn.Clicked += () => OnSlotClicked(slot);
            _slotScroll.AddChild(btn);
            _slotButtons.Add(btn);
        }

        RefreshSlots();

        var back = new IconButton
        {
            Name = "Back",
            Icon = MenuIconKind.NavBack,
            Label = "Back",
            TooltipHint = "Return to pause menu",
            Layout = IconButtonLayout.IconLeftOfLabel,
            IconSize = IconButton.TitleNavIconSize,
            FontSize = 18f,
            RequireMinimumHitExtent = true,
            Anchor = Anchor.BottomLeft,
            Position = new Vector2(40f, -80f),
            Size = new Vector2(220f, 60f),
        };
        IconButton.ApplyMenuTheme(back, showGlow: true);
        back.Clicked += () => Cancelled?.Invoke();
        AddWidget(back);
    }

    private void RefreshSlots()
    {
        var slots = _saveManager.ListSaveSlots().ToDictionary(s => s.SlotName);
        float gap = 10f;
        float rowY = 0f;

        foreach (IconButton button in _slotButtons)
        {
            ClearSlotMetadataLabels(button);
            button.Position = new Vector2(0f, rowY);

            string slotKey = button.Name ?? string.Empty;
            if (!slots.TryGetValue(slotKey, out SaveSlotInfo? info) || !info.HasData)
            {
                if (slotKey == SaveSlotNames.Autosave)
                    button.Label = "Quick Save";
                else
                    button.Label = $"{SaveSlotNames.DisplayName(slotKey)} — Empty";

                button.Size = new Vector2(SlotButtonWidth, SlotButtonHeight);
                rowY += SlotButtonHeight + gap;
                continue;
            }

            button.Label = string.Empty;
            string mission = string.IsNullOrWhiteSpace(info.MissionId) ? "Free Play" : info.MissionId;
            string when = FormatSavedAt(info.SavedAt);
            float rowHeight = ApplyOccupiedSlotLabels(button, slotKey, mission, when);
            button.Size = new Vector2(SlotButtonWidth, rowHeight);
            rowY += rowHeight + gap;
        }

        _slotScroll.SyncLabelWrapWidths();
        _slotScroll.RecalculateContentHeight(_slotScroll.Size);
    }

    private void OnSlotClicked(string slotName)
    {
        SlotSelected?.Invoke(slotName);
    }

    /// <summary>Fired when the player picks a slot; host supplies snapshot and calls <see cref="RequestSave"/>.</summary>
    public event Action<string>? SlotSelected;

    private void ShowOverwriteConfirmation(string slotName, Func<SaveData> buildSnapshot)
    {
        var scrim = new Panel
        {
            Name = "ConfirmScrim",
            Anchor = Anchor.Stretch,
            BackgroundColor = new Vector4(0f, 0f, 0f, 0.65f),
            DrawBorder = false,
        };
        AddWidget(scrim);

        var dialog = new Panel
        {
            Name = "ConfirmDialog",
            Anchor = Anchor.Center,
            Size = new Vector2(480f, 260f),
        };
        MenuTheme.ApplyPanel(dialog);

        dialog.AddChild(new Label
        {
            Name = "ConfirmTitle",
            Text = "Overwrite Save?",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-210f, 18f),
            Size = new Vector2(420f, 36f),
            FontSize = 24f,
            TextColor = MenuTheme.TitleColor,
        });

        string detail = FormatOverwriteDetail(slotName);
        dialog.AddChild(new Label
        {
            Name = "ConfirmText",
            Text = detail,
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-210f, 62f),
            Size = new Vector2(420f, 56f),
            FontSize = 18f,
            WrapWidth = UITextDrawing.ContentWrapWidth(420f, 4f),
            TextColor = MenuTheme.BodyTextColor,
        });

        dialog.AddChild(new Label
        {
            Name = "ConfirmWarning",
            Text = "Existing data will be replaced.",
            Anchor = Anchor.TopCenter,
            Position = new Vector2(-200f, 126f),
            Size = new Vector2(400f, 28f),
            FontSize = 16f,
            TextColor = MenuTheme.MutedTextColor,
        });

        var yes = new Button
        {
            Name = "ConfirmYes",
            Label = "Overwrite",
            Anchor = Anchor.BottomCenter,
            Position = new Vector2(-170f, -72f),
            Size = new Vector2(160f, 52f),
            FontSize = 18f,
        };
        MenuTheme.ApplyNavButton(yes);
        yes.Clicked += () =>
        {
            RemoveWidget(scrim);
            RemoveWidget(dialog);
            PerformSave(slotName, buildSnapshot);
        };
        dialog.AddChild(yes);

        var no = new Button
        {
            Name = "ConfirmNo",
            Label = "Cancel",
            Anchor = Anchor.BottomCenter,
            Position = new Vector2(20f, -72f),
            Size = new Vector2(160f, 52f),
            FontSize = 18f,
        };
        MenuTheme.ApplyNavButton(no);
        no.Clicked += () =>
        {
            RemoveWidget(scrim);
            RemoveWidget(dialog);
        };
        dialog.AddChild(no);

        AddWidget(dialog);
    }

    private string FormatOverwriteDetail(string slotName)
    {
        var slots = _saveManager.ListSaveSlots().ToDictionary(s => s.SlotName);
        if (!slots.TryGetValue(slotName, out SaveSlotInfo? info) || !info.HasData)
            return SaveSlotNames.DisplayName(slotName);

        string mission = string.IsNullOrWhiteSpace(info.MissionId) ? "Free Play" : info.MissionId;
        string when = FormatSavedAt(info.SavedAt);
        return FormatOccupiedSlotLabel(slotName, mission, when);
    }

    private const float SlotTextLeft =
        IconButton.TitleNavIconColumnWidth + IconButton.LabelPadding;
    private const float SlotLabelPadding = 4f;
    private const float SlotRowPadding = 6f;
    private const float SlotLineGap = 2f;

    private float SlotLabelAreaWidth =>
        SlotButtonWidth - SlotTextLeft - IconButton.LabelPadding;

    private float SlotMissionWrapWidth =>
        UITextDrawing.ContentWrapWidth(SlotLabelAreaWidth, SlotLabelPadding);

    private static void ClearSlotMetadataLabels(IconButton button)
    {
        for (int i = button.Children.Count - 1; i >= 0; i--)
        {
            if (button.Children[i] is Label)
                button.RemoveChild(button.Children[i]);
        }
    }

    private float ApplyOccupiedSlotLabels(IconButton button, string slotKey, string mission, string when)
    {
        float labelWidth = SlotLabelAreaWidth;
        float labelY = SlotRowPadding;
        float wrapWidth = SlotMissionWrapWidth;

        var slotLabel = new Label
        {
            Name = $"{slotKey}Slot",
            Text = $"{SaveSlotNames.DisplayName(slotKey)} —",
            Anchor = Anchor.TopLeft,
            Position = new Vector2(SlotTextLeft, labelY),
            Size = new Vector2(labelWidth, 0f),
            FontSize = SlotLabelFontSize,
            Padding = SlotLabelPadding,
            TextColor = MenuTheme.ButtonText,
        };
        button.AddChild(slotLabel);
        labelY += slotLabel.MeasureContentHeight() + SlotLineGap;

        var missionLabel = new Label
        {
            Name = $"{slotKey}Mission",
            Text = mission,
            Anchor = Anchor.TopLeft,
            Position = new Vector2(SlotTextLeft, labelY),
            Size = new Vector2(labelWidth, 0f),
            FontSize = SlotLabelFontSize,
            Padding = SlotLabelPadding,
            WrapWidth = wrapWidth,
            TextColor = MenuTheme.BodyTextColor,
        };
        button.AddChild(missionLabel);
        labelY += missionLabel.MeasureContentHeight() + SlotLineGap;

        var whenLabel = new Label
        {
            Name = $"{slotKey}When",
            Text = $"({when})",
            Anchor = Anchor.TopLeft,
            Position = new Vector2(SlotTextLeft, labelY),
            Size = new Vector2(labelWidth, 0f),
            FontSize = SlotLabelFontSize,
            Padding = SlotLabelPadding,
            TextColor = MenuTheme.MutedTextColor,
        };
        button.AddChild(whenLabel);
        labelY += whenLabel.MeasureContentHeight() + SlotRowPadding;

        return MathF.Max(SlotButtonHeight, labelY);
    }

    private static string FormatOccupiedSlotLabel(string slotKey, string mission, string when) =>
        $"{SaveSlotNames.DisplayName(slotKey)} — {mission} ({when})";

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