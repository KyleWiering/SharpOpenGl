namespace SharpOpenGl.Engine.Diagnostics;

/// <summary>
/// Severity level for a logged error entry.
/// </summary>
public enum ErrorSeverity
{
    /// <summary>Informational message (non-fatal).</summary>
    Info,

    /// <summary>Something unexpected happened but the game can continue.</summary>
    Warning,

    /// <summary>A serious error that may cause instability; recovery was attempted.</summary>
    Error,

    /// <summary>An unrecoverable failure; the game must exit or restart the session.</summary>
    Fatal,
}

/// <summary>
/// A single error entry captured by <see cref="ErrorHandler"/>.
/// </summary>
public sealed class ErrorEntry
{
    /// <summary>UTC timestamp of the error.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>Error severity.</summary>
    public ErrorSeverity Severity { get; init; }

    /// <summary>Human-readable description.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Full exception details, or empty when no exception is attached.</summary>
    public string ExceptionDetails { get; init; } = string.Empty;
}

/// <summary>
/// Central error-handling service.  Captures exceptions, logs them to an
/// in-memory ring buffer, writes them to a crash log file when a path is
/// configured, and raises events so the game can show recovery UI.
/// </summary>
public sealed class ErrorHandler
{
    private const int MaxEntries = 256;

    private readonly ErrorEntry[] _log = new ErrorEntry[MaxEntries];
    private int _head;
    private int _count;
    private readonly string? _logFilePath;
    private readonly object _lock = new();

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>
    /// Create an error handler.
    /// </summary>
    /// <param name="logFilePath">
    /// Optional path for the crash log.  When <c>null</c> no file is written.
    /// </param>
    /// <param name="attachToAppDomain">
    /// When <c>true</c>, subscribe to <see cref="AppDomain.UnhandledException"/>
    /// so that uncaught exceptions are captured automatically.
    /// </param>
    public ErrorHandler(string? logFilePath = null, bool attachToAppDomain = false)
    {
        _logFilePath = logFilePath;
        if (attachToAppDomain)
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Raised after every captured error entry, on the capturing thread.</summary>
    public event Action<ErrorEntry>? ErrorCaptured;

    // ── Logging ───────────────────────────────────────────────────────────────

    /// <summary>Log an informational message.</summary>
    public void LogInfo(string message) =>
        Capture(ErrorSeverity.Info, message, null);

    /// <summary>Log a non-fatal warning.</summary>
    public void LogWarning(string message, Exception? ex = null) =>
        Capture(ErrorSeverity.Warning, message, ex);

    /// <summary>Log a recoverable error.</summary>
    public void LogError(string message, Exception? ex = null) =>
        Capture(ErrorSeverity.Error, message, ex);

    /// <summary>Log a fatal error (game must shut down or restart the scene).</summary>
    public void LogFatal(string message, Exception? ex = null) =>
        Capture(ErrorSeverity.Fatal, message, ex);

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>Number of entries currently stored (up to 256).</summary>
    public int EntryCount
    {
        get { lock (_lock) return _count; }
    }

    /// <summary>
    /// Return all stored entries ordered oldest to newest.
    /// </summary>
    public ErrorEntry[] GetEntries()
    {
        lock (_lock)
        {
            var result = new ErrorEntry[_count];
            for (int i = 0; i < _count; i++)
                result[i] = _log[(_head - _count + i + MaxEntries) % MaxEntries];
            return result;
        }
    }

    /// <summary>Return the most recent entry, or <c>null</c> when empty.</summary>
    public ErrorEntry? LatestEntry
    {
        get
        {
            lock (_lock)
            {
                if (_count == 0) return null;
                return _log[(_head - 1 + MaxEntries) % MaxEntries];
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void Capture(ErrorSeverity severity, string message, Exception? ex)
    {
        var entry = new ErrorEntry
        {
            Severity         = severity,
            Message          = message,
            ExceptionDetails = ex?.ToString() ?? string.Empty,
        };

        lock (_lock)
        {
            _log[_head] = entry;
            _head = (_head + 1) % MaxEntries;
            if (_count < MaxEntries) _count++;
        }

        WriteToFile(entry);
        ErrorCaptured?.Invoke(entry);
    }

    private void WriteToFile(ErrorEntry entry)
    {
        if (_logFilePath is null) return;
        try
        {
            string? dir = Path.GetDirectoryName(_logFilePath);
            if (dir is { Length: > 0 } && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string line = $"[{entry.Timestamp:o}] [{entry.Severity}] {entry.Message}";
            if (!string.IsNullOrEmpty(entry.ExceptionDetails))
                line += Environment.NewLine + entry.ExceptionDetails;
            line += Environment.NewLine;

            File.AppendAllText(_logFilePath, line);
        }
        catch
        {
            // Cannot write to log; swallow to avoid infinite recursion.
        }
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        string msg = e.IsTerminating
            ? "FATAL: Unhandled exception — application terminating"
            : "FATAL: Unhandled exception";

        LogFatal(msg, e.ExceptionObject as Exception);
    }
}
