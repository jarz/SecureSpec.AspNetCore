namespace SecureSpec.AspNetCore.Diagnostics;

/// <summary>
/// Provides structured diagnostic logging for SecureSpec operations.
/// </summary>
public class DiagnosticsLogger
{
    private readonly List<DiagnosticEvent> _events = new();
    private readonly object _lock = new();

    /// <summary>
    /// Logs a diagnostic event.
    /// </summary>
    /// <param name="level">The severity level.</param>
    /// <param name="code">The diagnostic code (e.g., "SEC001", "CSP001").</param>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="context">Optional context information.</param>
    /// <param name="sanitized">Indicates whether sensitive data has been sanitized.</param>
    public void Log(DiagnosticLevel level, string code, string message, object? context = null, bool sanitized = false)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(message);

        var evt = new DiagnosticEvent
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = level,
            Code = code,
            Message = message,
            Context = context,
            Sanitized = sanitized
        };

        lock (_lock)
        {
            _events.Add(evt);
        }
    }

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    public void LogInfo(string code, string message, object? context = null, bool sanitized = false)
    {
        Log(DiagnosticLevel.Info, code, message, context, sanitized);
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    public void LogWarning(string code, string message, object? context = null, bool sanitized = false)
    {
        Log(DiagnosticLevel.Warn, code, message, context, sanitized);
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    public void LogError(string code, string message, object? context = null, bool sanitized = false)
    {
        Log(DiagnosticLevel.Error, code, message, context, sanitized);
    }

    /// <summary>
    /// Logs a critical error message.
    /// </summary>
    public void LogCritical(string code, string message, object? context = null, bool sanitized = false)
    {
        Log(DiagnosticLevel.Critical, code, message, context, sanitized);
    }

    /// <summary>
    /// Sanitizes a hash or sensitive string by keeping only the first 8 characters.
    /// </summary>
    /// <param name="value">The value to sanitize.</param>
    /// <returns>The sanitized value (first 8 characters + "...").</returns>
    public static string SanitizeHash(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Length <= 8 ? value : string.Concat(value.AsSpan(0, 8), "...");
    }

    /// <summary>
    /// Sanitizes a file path by removing directory information.
    /// Handles both Windows and Unix path separators.
    /// </summary>
    /// <param name="path">The path to sanitize.</param>
    /// <returns>Just the filename portion of the path.</returns>
    public static string SanitizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        // Handle both Windows and Unix path separators
        var lastBackslash = path.LastIndexOf('\\');
        var lastForwardslash = path.LastIndexOf('/');
        var lastSeparator = Math.Max(lastBackslash, lastForwardslash);

        return lastSeparator >= 0 ? path[(lastSeparator + 1)..] : path;
    }

    /// <summary>
    /// Gets all diagnostic events.
    /// </summary>
    public IReadOnlyList<DiagnosticEvent> GetEvents()
    {
        lock (_lock)
        {
            return _events.ToList();
        }
    }

    /// <summary>
    /// Clears all diagnostic events.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _events.Clear();
        }
    }
}

/// <summary>
/// Represents a diagnostic event.
/// </summary>
public class DiagnosticEvent
{
    /// <summary>
    /// Gets or sets the timestamp of the event in ISO 8601 format.
    /// </summary>
    public required DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the severity level.
    /// </summary>
    public required DiagnosticLevel Level { get; set; }

    /// <summary>
    /// Gets or sets the diagnostic code.
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Gets or sets additional context information as structured metadata.
    /// This can include details like bucket name, file paths, error details, etc.
    /// </summary>
    public object? Context { get; set; }

    /// <summary>
    /// Gets or sets whether sensitive data in this event has been sanitized.
    /// When true, indicates that paths, hashes, or other sensitive information
    /// have been redacted to the first 8 characters or otherwise protected.
    /// </summary>
    public bool Sanitized { get; set; }
}

/// <summary>
/// Diagnostic severity levels.
/// </summary>
public enum DiagnosticLevel
{
    /// <summary>
    /// Informational message.
    /// </summary>
    Info,

    /// <summary>
    /// Warning message.
    /// </summary>
    Warn,

    /// <summary>
    /// Error message.
    /// </summary>
    Error,

    /// <summary>
    /// Critical error message.
    /// </summary>
    Critical
}
