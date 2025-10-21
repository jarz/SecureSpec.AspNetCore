namespace SecureSpec.AspNetCore.Configuration;

/// <summary>
/// Configuration options for diagnostics and logging.
/// </summary>
public class DiagnosticsOptions
{
    /// <summary>
    /// Gets or sets whether to enable structured diagnostics logging.
    /// Default is true.
    /// </summary>
    public bool EnableDiagnostics { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of diagnostic events to retain.
    /// Default is 10000.
    /// </summary>
    public int MaxRetentionCount { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the maximum age (in hours) for diagnostic events.
    /// Events older than this will be purged.
    /// Default is 24 hours.
    /// </summary>
    public int MaxRetentionHours { get; set; } = 24;

    /// <summary>
    /// Gets or sets whether to include detailed error information.
    /// Default is false (should only be enabled in development).
    /// </summary>
    public bool IncludeDetailedErrors { get; set; }
}
