using SharpOpenGl.Engine.Persistence;
using Xunit;

namespace SharpOpenGl.Tests.Persistence;

public class SettingsManagerTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"sgtests_{Guid.NewGuid():N}");
    private string FilePath => Path.Combine(_dir, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Load_returns_false_when_no_file()
    {
        var mgr = new SettingsManager(FilePath);
        Assert.False(mgr.Load());
    }

    [Fact]
    public void Save_creates_file()
    {
        var mgr = new SettingsManager(FilePath);
        bool ok = mgr.Save();
        Assert.True(ok);
        Assert.True(File.Exists(FilePath));
    }

    [Fact]
    public void Apply_and_Load_roundtrip_settings()
    {
        var mgr = new SettingsManager(FilePath);
        var original = new GameSettings
        {
            MasterVolume = 0.5f,
            QualityTier  = "Low",
            Accessibility = new AccessibilitySettings
            {
                ColorblindMode = ColorblindMode.BlueYellow,
                FontScale      = 1.5f,
            },
        };
        mgr.Apply(original);

        var mgr2 = new SettingsManager(FilePath);
        mgr2.Load();

        Assert.Equal(0.5f, mgr2.Current.MasterVolume, 0.001f);
        Assert.Equal("Low", mgr2.Current.QualityTier);
        Assert.Equal(ColorblindMode.BlueYellow, mgr2.Current.Accessibility.ColorblindMode);
        Assert.Equal(1.5f, mgr2.Current.Accessibility.FontScale, 0.001f);
    }

    [Fact]
    public void ResetToDefaults_restores_default_settings()
    {
        var mgr = new SettingsManager(FilePath);
        mgr.Apply(new GameSettings { MasterVolume = 0.1f });
        mgr.ResetToDefaults();

        Assert.Equal(1.0f, mgr.Current.MasterVolume, 0.001f);
    }

    [Fact]
    public void Null_path_save_returns_false()
    {
        var mgr = new SettingsManager(null);
        Assert.False(mgr.Save());
    }

    [Fact]
    public void Null_path_load_returns_false()
    {
        var mgr = new SettingsManager(null);
        Assert.False(mgr.Load());
    }

    [Fact]
    public void Load_falls_back_to_defaults_on_corrupt_file()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(FilePath, "this is not json {{{{");

        var mgr = new SettingsManager(FilePath);
        bool loaded = mgr.Load();
        Assert.False(loaded);
        Assert.Equal(1.0f, mgr.Current.MasterVolume); // default
    }
}
