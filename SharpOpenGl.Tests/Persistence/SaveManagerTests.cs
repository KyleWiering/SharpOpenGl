using SharpOpenGl.Engine.Persistence;
using Xunit;

namespace SharpOpenGl.Tests.Persistence;

public class SaveManagerTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"savemgr_{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    private SaveData MakeSave(string slot = "Slot1") => new SaveData
    {
        SlotName   = slot,
        MissionId  = "tutorial_01",
        ElapsedMissionTime = 120f,
        CameraX    = 100f,
        CameraY    = 200f,
        CameraZoom = 1.5f,
        PlayerResources = new()
        {
            new PlayerResourceRecord { PlayerId = 1, Energy = 500f, Minerals = 300f, Data = 50f, Crew = 10f },
        },
        Entities = new()
        {
            new EntitySaveRecord { EntityId = 1, TemplateId = "hero_default", X = 10f, Y = 20f, Health = 900f, PlayerId = 1 },
        },
        CompletedObjectiveIds = new() { "destroy_scout" },
        FiredTriggerIds       = new() { "spawn_wave_1" },
    };

    [Fact]
    public void ListSaveFiles_returns_empty_when_dir_missing()
    {
        var mgr = new SaveManager(Path.Combine(_dir, "saves"));
        Assert.Empty(mgr.ListSaveFiles());
    }

    [Fact]
    public void Save_creates_file()
    {
        var mgr = new SaveManager(_dir);
        bool ok = mgr.Save(MakeSave());
        Assert.True(ok);
        Assert.Single(mgr.ListSaveFiles());
    }

    [Fact]
    public void Load_returns_null_for_unknown_slot()
    {
        var mgr = new SaveManager(_dir);
        Assert.Null(mgr.Load("NoSuchSlot"));
    }

    [Fact]
    public void Save_and_Load_roundtrip()
    {
        var mgr  = new SaveManager(_dir);
        var data = MakeSave("Campaign1");
        mgr.Save(data);

        SaveData? loaded = mgr.Load("Campaign1");
        Assert.NotNull(loaded);
        Assert.Equal("tutorial_01",  loaded!.MissionId);
        Assert.Equal(120f,           loaded.ElapsedMissionTime, 0.001f);
        Assert.Equal(1.5f,           loaded.CameraZoom, 0.001f);
        Assert.Single(loaded.PlayerResources);
        Assert.Equal(500f, loaded.PlayerResources[0].Energy, 0.001f);
        Assert.Single(loaded.Entities);
        Assert.Equal("hero_default", loaded.Entities[0].TemplateId);
        Assert.Contains("destroy_scout", loaded.CompletedObjectiveIds);
    }

    [Fact]
    public void LoadLatest_returns_most_recent_save()
    {
        var mgr = new SaveManager(_dir);
        mgr.Save(MakeSave("SlotA"));
        // Small delay to ensure different write times.
        System.Threading.Thread.Sleep(10);
        mgr.Save(MakeSave("SlotB"));

        SaveData? latest = mgr.LoadLatest();
        Assert.NotNull(latest);
        Assert.Equal("SlotB", latest!.SlotName);
    }

    [Fact]
    public void LoadLatest_returns_null_when_no_saves()
    {
        var mgr = new SaveManager(_dir);
        Assert.Null(mgr.LoadLatest());
    }

    [Fact]
    public void Delete_removes_save_file()
    {
        var mgr = new SaveManager(_dir);
        mgr.Save(MakeSave("Slot1"));
        Assert.True(mgr.Delete("Slot1"));
        Assert.Empty(mgr.ListSaveFiles());
    }

    [Fact]
    public void Delete_returns_false_when_not_found()
    {
        var mgr = new SaveManager(_dir);
        Assert.False(mgr.Delete("Ghost"));
    }

    [Fact]
    public void SlotExists_returns_false_for_missing_slot()
    {
        var mgr = new SaveManager(_dir);
        Assert.False(mgr.SlotExists("Missing"));
    }

    [Fact]
    public void ListSaveSlots_includes_manual_and_autosave_entries()
    {
        var mgr = new SaveManager(_dir);
        mgr.Save(MakeSave(SaveSlotNames.ManualSlots[2]));

        var slots = mgr.ListSaveSlots();
        Assert.Contains(slots, s => s.SlotName == SaveSlotNames.Autosave && !s.HasData);
        Assert.Contains(slots, s => s.SlotName == SaveSlotNames.ManualSlots[2] && s.HasData);
        Assert.Equal(6, slots.Count);
    }

    [Fact]
    public void SavedAt_timestamp_is_populated()
    {
        var mgr  = new SaveManager(_dir);
        var data = MakeSave();
        mgr.Save(data);
        Assert.False(string.IsNullOrEmpty(data.SavedAt));
    }

    [Fact]
    public void CreateInMemory_round_trips_without_filesystem()
    {
        var mgr = SaveManager.CreateInMemory();
        var data = MakeSave("BrowserSlot");
        Assert.True(mgr.Save(data));

        Assert.Single(mgr.ListSaveFiles());
        Assert.True(mgr.SlotExists("BrowserSlot"));

        var loaded = mgr.Load("BrowserSlot");
        Assert.NotNull(loaded);
        Assert.Equal("tutorial_01", loaded!.MissionId);
        Assert.Equal(500f, loaded.PlayerResources[0].Energy);
    }
}
