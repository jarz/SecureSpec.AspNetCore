namespace SecureSpec.AspNetCore.Configuration;

/// <summary>
/// Configuration options for performance monitoring and resource guards.
/// </summary>
public class PerformanceOptions
{
    /// <summary>
    /// Gets or sets the maximum allowed time for document generation in milliseconds.
    /// Default is 2000ms. If generation exceeds this time, a fallback document is generated.
    /// </summary>
    public int MaxGenerationTimeMs { get; set; } = 2000;

    /// <summary>
    /// Gets or sets the maximum allowed memory in bytes for document generation.
    /// Default is 10MB (10 * 1024 * 1024 bytes). If memory exceeds this during generation,
    /// a fallback document is generated.
    /// </summary>
    public long MaxMemoryBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Gets or sets whether resource guards are enabled.
    /// Default is true. When disabled, no time or memory limits are enforced.
    /// </summary>
    public bool EnableResourceGuards { get; set; } = true;

    /// <summary>
    /// Gets or sets whether performance monitoring is enabled.
    /// Default is true. When enabled, performance metrics are collected and diagnostic events are emitted.
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = true;

    /// <summary>
    /// Gets or sets the target generation time for 1000 operations in milliseconds.
    /// Default is 500ms (AC 297). Performance below this threshold is considered optimal.
    /// </summary>
    public int TargetGenerationTimeMs { get; set; } = 500;

    /// <summary>
    /// Gets or sets the degraded performance threshold for 1000 operations in milliseconds.
    /// Default is 2000ms. Performance between target and this threshold triggers a warning.
    /// Performance exceeding this threshold is considered a failure.
    /// </summary>
    public int DegradedThresholdMs { get; set; } = 2000;

    /// <summary>
    /// Gets or sets the number of operations used for performance baseline measurement.
    /// Default is 1000 operations (as per AC 297-300).
    /// </summary>
    public int BaselineOperationCount { get; set; } = 1000;
}
