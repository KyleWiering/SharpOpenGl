using SharpOpenGl.Engine.Persistence;
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

        Button? saveBtn = pause.FindButton("SaveGame");
        Assert.NotNull(saveBtn);
        saveBtn!.Activate();

        Assert.True(saveRequested);
    }

    [Fact]
    public void Main_menu_includes_load_game_button_when_saves_exist()
    {
        var menu = new MainMenuScreen(hasSave: true);
        bool loadRequested = false;
        menu.LoadGameRequested += () => loadRequested = true;

        Button? loadBtn = menu.FindButton("LoadGame");
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

        Button? entry = screen.FindButton("Entry0");
        Assert.NotNull(entry);
        entry!.Activate();

        Assert.Equal(SaveSlotNames.ManualSlots[1], loadedSlot);

        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
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

        Button? quickSave = screen.FindButton("QuickSave");
        Assert.NotNull(quickSave);
        quickSave!.Activate();

        Assert.True(mgr.SlotExists(SaveSlotNames.Autosave));

        SaveData? loaded = mgr.Load(SaveSlotNames.Autosave);
        Assert.NotNull(loaded);
        Assert.Equal("tutorial_01", loaded!.MissionId);

        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
    }
}