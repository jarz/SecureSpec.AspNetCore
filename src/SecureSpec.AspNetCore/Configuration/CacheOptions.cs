namespace SecureSpec.AspNetCore.Configuration;

/// <summary>
/// Configuration options for document caching.
/// </summary>
public sealed class CacheOptions
{
    /// <summary>
    /// Gets or sets the default expiration time for cache entries.
    /// </summary>
    /// <remarks>
    /// Default is 5 minutes. Must be a positive timespan.
    /// </remarks>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets a value indicating whether caching is enabled.
    /// </summary>
    /// <remarks>
    /// Default is true. When disabled, documents are generated fresh on each request.
    /// </remarks>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to validate integrity on cache retrieval.
    /// </summary>
    /// <remarks>
    /// Default is true. When enabled, hash validation is performed on every cache hit
    /// to ensure the cached content has not been tampered with.
    /// </remarks>
    public bool ValidateIntegrity { get; set; } = true;

    /// <summary>
    /// Gets or sets the interval for automatic eviction of expired cache entries.
    /// </summary>
    /// <remarks>
    /// Default is null (disabled). When set, a background task will periodically
    /// evict expired entries. Must be a positive timespan if not null.
    /// </remarks>
    public TimeSpan? AutoEvictionInterval { get; set; }
}
