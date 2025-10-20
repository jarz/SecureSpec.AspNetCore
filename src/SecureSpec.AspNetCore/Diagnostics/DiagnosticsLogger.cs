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
    public void Log(DiagnosticLevel level, string code, string message, object? context = null)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(message);

        var evt = new DiagnosticEvent
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = level,
            Code = code,
            Message = message,
            Context = context
        };

        lock (_lock)
        {
            _events.Add(evt);
        }
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
}

/// <summary>
/// Represents a diagnostic event.
/// </summary>
public class DiagnosticEvent
{
    /// <summary>
    /// Gets or sets the timestamp of the event.
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
    /// Gets or sets additional context information.
    /// </summary>
    public object? Context { get; set; }
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
