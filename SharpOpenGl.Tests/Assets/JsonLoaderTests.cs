using SharpOpenGl.Engine.Assets;
using Xunit;

namespace SharpOpenGl.Tests.Assets;

public class JsonLoaderTests
{
    private static string WriteTempJson(string content)
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void Load_returns_null_for_missing_file()
    {
        var result = JsonLoader.Load<SampleData>("/nonexistent/path.json");
        Assert.Null(result);
    }

    [Fact]
    public void Load_deserializes_valid_json()
    {
        string path = WriteTempJson("""{"name":"test","value":42}""");
        try
        {
            var result = JsonLoader.Load<SampleData>(path);
            Assert.NotNull(result);
            Assert.Equal("test", result!.Name);
            Assert.Equal(42, result.Value);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Load_is_case_insensitive()
    {
        string path = WriteTempJson("""{"NAME":"hello","VALUE":7}""");
        try
        {
            var result = JsonLoader.Load<SampleData>(path);
            Assert.NotNull(result);
            Assert.Equal("hello", result!.Name);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Load_returns_null_for_malformed_json()
    {
        string path = WriteTempJson("{ not valid json }");
        try
        {
            var result = JsonLoader.Load<SampleData>(path);
            Assert.Null(result);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void LoadStrict_throws_for_missing_file()
    {
        Assert.Throws<FileNotFoundException>(
            () => JsonLoader.LoadStrict<SampleData>("/nonexistent/path.json"));
    }

    [Fact]
    public void Save_then_Load_roundtrip()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");
        var original = new SampleData { Name = "roundtrip", Value = 99 };
        try
        {
            JsonLoader.Save(path, original);
            var loaded = JsonLoader.Load<SampleData>(path);
            Assert.NotNull(loaded);
            Assert.Equal(original.Name, loaded!.Name);
            Assert.Equal(original.Value, loaded.Value);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private class SampleData
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }
}
