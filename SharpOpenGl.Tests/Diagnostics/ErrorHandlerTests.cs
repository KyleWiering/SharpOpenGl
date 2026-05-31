using SharpOpenGl.Engine.Diagnostics;
using Xunit;

namespace SharpOpenGl.Tests.Diagnostics;

public class ErrorHandlerTests : IDisposable
{
    private readonly string _logFile =
        Path.Combine(Path.GetTempPath(), $"errhandler_{Guid.NewGuid():N}.log");

    public void Dispose()
    {
        if (File.Exists(_logFile))
            File.Delete(_logFile);
    }

    [Fact]
    public void EntryCount_starts_at_zero()
    {
        var h = new ErrorHandler();
        Assert.Equal(0, h.EntryCount);
    }

    [Fact]
    public void LogInfo_increments_count()
    {
        var h = new ErrorHandler();
        h.LogInfo("startup complete");
        Assert.Equal(1, h.EntryCount);
    }

    [Fact]
    public void LogError_stores_message()
    {
        var h = new ErrorHandler();
        h.LogError("something went wrong");
        ErrorEntry? e = h.LatestEntry;
        Assert.NotNull(e);
        Assert.Equal(ErrorSeverity.Error, e!.Severity);
        Assert.Contains("something went wrong", e.Message);
    }

    [Fact]
    public void LogError_captures_exception_details()
    {
        var h  = new ErrorHandler();
        var ex = new InvalidOperationException("boom");
        h.LogError("unhandled op", ex);
        Assert.Contains("InvalidOperationException", h.LatestEntry!.ExceptionDetails);
    }

    [Fact]
    public void GetEntries_returns_all_entries_in_order()
    {
        var h = new ErrorHandler();
        h.LogInfo("first");
        h.LogWarning("second");
        h.LogFatal("third");
        ErrorEntry[] entries = h.GetEntries();
        Assert.Equal(3, entries.Length);
        Assert.Equal("first",  entries[0].Message);
        Assert.Equal("second", entries[1].Message);
        Assert.Equal("third",  entries[2].Message);
    }

    [Fact]
    public void LatestEntry_is_null_when_empty()
    {
        var h = new ErrorHandler();
        Assert.Null(h.LatestEntry);
    }

    [Fact]
    public void ErrorCaptured_event_fires_on_log()
    {
        var h = new ErrorHandler();
        ErrorEntry? received = null;
        h.ErrorCaptured += e => received = e;
        h.LogWarning("watch out");
        Assert.NotNull(received);
        Assert.Equal(ErrorSeverity.Warning, received!.Severity);
    }

    [Fact]
    public void Writes_to_log_file_when_path_provided()
    {
        var h = new ErrorHandler(_logFile);
        h.LogError("disk error test");
        Assert.True(File.Exists(_logFile));
        string content = File.ReadAllText(_logFile);
        Assert.Contains("disk error test", content);
    }

    [Fact]
    public void Severity_levels_are_stored_correctly()
    {
        var h = new ErrorHandler();
        h.LogInfo("i");
        h.LogWarning("w");
        h.LogError("e");
        h.LogFatal("f");
        ErrorEntry[] entries = h.GetEntries();
        Assert.Equal(ErrorSeverity.Info,    entries[0].Severity);
        Assert.Equal(ErrorSeverity.Warning, entries[1].Severity);
        Assert.Equal(ErrorSeverity.Error,   entries[2].Severity);
        Assert.Equal(ErrorSeverity.Fatal,   entries[3].Severity);
    }
}
