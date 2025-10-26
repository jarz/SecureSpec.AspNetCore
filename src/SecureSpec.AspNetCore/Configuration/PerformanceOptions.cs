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
}
