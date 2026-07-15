using SharpOpenGl.Engine.Persistence;
using SharpOpenGl.Engine.UI;
using SharpOpenGl.Engine.UI.Screens;
using SharpOpenGl.Engine.UI.Widgets;
using Xunit;

namespace SharpOpenGl.Tests.UI;

public class SaveLoadScreenTests
{
    [Fact]
    public void Pause_screen_includes_save_game_button()
    {
        var pause = new PauseScreen();
        bool saveRequested = false;
        pause.SaveGameRequested += () => saveRequested = true;

        IUIButton? saveBtn = pause.FindButton("SaveGame");
        Assert.NotNull(saveBtn);
        saveBtn!.Activate();

        Assert.True(saveRequested);
    }

    [Fact]
    public void Save_game_overlay_card_uses_menu_theme_panel_colours()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"save_theme_{Guid.NewGuid():N}");
        try
        {
            var screen = new SaveGameScreen(new SaveManager(dir));
            var card = Assert.IsType<Panel>(FindWidget(screen, "SaveCard"));

            Assert.Equal(MenuTheme.PanelBackground, card.BackgroundColor);
            Assert.Equal(MenuTheme.PanelBorder, card.BorderColor);
            Assert.True(card.DrawBorder);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Pause_screen_save_button_uses_nav_save_icon_button()
    {
        var pause = new PauseScreen();
        var save = Assert.IsType<IconButton>(pause.FindButton("SaveGame"));

        Assert.Equal(MenuIconKind.NavSave, save.Icon);
        Assert.Equal("Save Game", save.Label);
        Assert.Equal(IconButtonLayout.IconLeftOfLabel, save.Layout);
    }

    [Fact]
    public void Pause_screen_includes_load_game_button()
    {
        var pause = new PauseScreen();
        bool loadRequested = false;
        pause.LoadGameRequested += () => loadRequested = true;

        IUIButton? loadBtn = pause.FindButton("LoadGame");
        Assert.NotNull(loadBtn);
        loadBtn!.Activate();

        Assert.True(loadRequested);
    }

    [Fact]
    public void Pause_screen_load_button_uses_nav_load_game_icon_button()
    {
        var pause = new PauseScreen();
        var load = Assert.IsType<IconButton>(pause.FindButton("LoadGame"));

        Assert.Equal(MenuIconKind.NavLoadGame, load.Icon);
        Assert.Equal("Load Game", load.Label);
        Assert.Equal(IconButtonLayout.IconLeftOfLabel, load.Layout);
    }

    [Fact]
    public void Save_game_screen_back_uses_nav_back_icon_button()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"save_back_{Guid.NewGuid():N}");
        try
        {
            var screen = new SaveGameScreen(new SaveManager(dir));
            bool cancelled = false;
            screen.Cancelled += () => cancelled = true;

            var back = Assert.IsType<IconButton>(screen.FindButton("Back"));
            Assert.Equal(MenuIconKind.NavBack, back.Icon);
            Assert.Equal(IconButtonLayout.IconLeftOfLabel, back.Layout);
            back.Activate();

            Assert.True(cancelled);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Save_game_slot_rows_use_nav_save_icon_buttons()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"save_icons_{Guid.NewGuid():N}");
        try
        {
            var screen = new SaveGameScreen(new SaveManager(dir));
            var quickSave = Assert.IsType<IconButton>(screen.FindButton("QuickSave"));

            Assert.Equal(MenuIconKind.NavSave, quickSave.Icon);
            Assert.Equal(IconButtonLayout.IconLeftOfLabel, quickSave.Layout);

            foreach (string slot in SaveSlotNames.ManualSlots)
            {
                var slotButton = Assert.IsType<IconButton>(screen.FindButton(slot));
                Assert.Equal(MenuIconKind.NavSave, slotButton.Icon);
            }
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Load_game_screen_back_and_entries_use_icon_buttons()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"load_icons_{Guid.NewGuid():N}");
        try
        {
            var mgr = new SaveManager(dir);
            mgr.Save(new SaveData
            {
                SlotName = SaveSlotNames.ManualSlots[0],
                MissionId = "tutorial_01",
                ElapsedMissionTime = 90f,
                Entities = [new EntitySaveRecord { EntityId = 1, TemplateId = "hero_default", Health = 100f }],
            });

            var screen = new LoadGameScreen(mgr);
            bool backRequested = false;
            screen.BackRequested += () => backRequested = true;

            var back = Assert.IsType<IconButton>(screen.FindButton("Back"));
            Assert.Equal(MenuIconKind.NavBack, back.Icon);
            back.Activate();
            Assert.True(backRequested);

            var entry = Assert.IsType<IconButton>(screen.FindButton("Entry0"));
            Assert.Equal(MenuIconKind.NavLoadGame, entry.Icon);
            Assert.Equal(IconButtonLayout.IconLeftOfLabel, entry.Layout);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Main_menu_includes_load_game_button_when_saves_exist()
    {
        var menu = new MainMenuScreen(hasSave: true);
        bool loadRequested = false;
        menu.LoadGameRequested += () => loadRequested = true;

        IUIButton? loadBtn = menu.FindButton("LoadGame");
        Assert.NotNull(loadBtn);
        Assert.True(loadBtn!.IsEnabled);
        loadBtn.Activate();

        Assert.True(loadRequested);
    }

    [Fact]
    public void Load_game_screen_lists_save_metadata()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"loadui_{Guid.NewGuid():N}");
        var mgr = new SaveManager(dir);
        mgr.Save(new SaveData
        {
            SlotName = SaveSlotNames.ManualSlots[1],
            MissionId = "tutorial_01",
            ElapsedMissionTime = 125f,
            Entities = [new EntitySaveRecord { EntityId = 1, TemplateId = "hero_default", Health = 100f }],
        });

        var screen = new LoadGameScreen(mgr);
        Assert.Equal(1, screen.EntryCount);

        string? loadedSlot = null;
        screen.LoadRequested += slot => loadedSlot = slot;

        IUIButton? entry = screen.FindButton("Entry0");
        Assert.NotNull(entry);
        entry!.Activate();

        Assert.Equal(SaveSlotNames.ManualSlots[1], loadedSlot);

        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
    }

    [Fact]
    public void Save_game_overwrite_dialog_uses_scrim_and_title_hierarchy()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"save_confirm_theme_{Guid.NewGuid():N}");
        try
        {
            var mgr = new SaveManager(dir);
            mgr.Save(new SaveData
            {
                SlotName = SaveSlotNames.ManualSlots[0],
                MissionId = "tutorial_01",
                ElapsedMissionTime = 90f,
                Entities = [new EntitySaveRecord { EntityId = 1, TemplateId = "hero_default", Health = 100f }],
            });

            var screen = new SaveGameScreen(mgr);
            screen.RequestSave(SaveSlotNames.ManualSlots[0], () => new SaveData
            {
                SlotName = SaveSlotNames.ManualSlots[0],
                MissionId = "tutorial_01",
                Entities = [new EntitySaveRecord { EntityId = 2, TemplateId = "hero_default", Health = 100f }],
            });

            var scrim = Assert.IsType<Panel>(FindWidget(screen, "ConfirmScrim"));
            var dialog = Assert.IsType<Panel>(FindWidget(screen, "ConfirmDialog"));
            var title = Assert.IsType<Label>(FindWidget(screen, "ConfirmTitle"));
            var warning = Assert.IsType<Label>(FindWidget(screen, "ConfirmWarning"));

            Assert.True(scrim.BackgroundColor.W >= 0.5f, "Confirm scrim should dim the save card.");
            Assert.Equal(MenuTheme.PanelBackground, dialog.BackgroundColor);
            Assert.Equal(MenuTheme.TitleColor, title.TextColor);
            Assert.Equal("Overwrite Save?", title.Text);
            Assert.Equal(MenuTheme.MutedTextColor, warning.TextColor);
            Assert.DoesNotContain("—", title.Text);
            var detail = Assert.IsType<Label>(FindWidget(screen, "ConfirmText"));
            Assert.Contains("—", detail.Text);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Save_game_screen_quick_save_writes_autosave_slot()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"saveui_{Guid.NewGuid():N}");
        var mgr = new SaveManager(dir);
        var screen = new SaveGameScreen(mgr);

        screen.SlotSelected += slot =>
            screen.RequestSave(slot, () => new SaveData
            {
                SlotName = slot,
                MissionId = "tutorial_01",
                Entities = [new EntitySaveRecord { EntityId = 3, TemplateId = "hero_default", Health = 500f }],
            });

        IUIButton? quickSave = screen.FindButton("QuickSave");
        Assert.NotNull(quickSave);
        quickSave!.Activate();

        Assert.True(mgr.SlotExists(SaveSlotNames.Autosave));

        SaveData? loaded = mgr.Load(SaveSlotNames.Autosave);
        Assert.NotNull(loaded);
        Assert.Equal("tutorial_01", loaded!.MissionId);

        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
    }

    private static Widget? FindWidget(UIScreen screen, string name)
    {
        foreach (Widget root in GetRoots(screen))
        {
            Widget? match = FindWidgetInTree(root, name);
            if (match != null)
                return match;
        }

        return null;
    }

    private static Widget? FindWidgetInTree(Widget widget, string name)
    {
        if (widget.Name == name)
            return widget;

        foreach (Widget child in widget.Children)
        {
            Widget? match = FindWidgetInTree(child, name);
            if (match != null)
                return match;
        }

        return null;
    }

    private static IEnumerable<Widget> GetRoots(UIScreen screen)
    {
        var field = typeof(UIScreen).GetProperty(
            "Roots",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return (IReadOnlyList<Widget>)field!.GetValue(screen)!;
    }
}