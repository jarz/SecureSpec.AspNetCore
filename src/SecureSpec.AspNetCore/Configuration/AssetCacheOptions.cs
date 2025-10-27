namespace SecureSpec.AspNetCore.Configuration;

/// <summary>
/// Configuration options for UI asset caching with integrity revalidation.
/// </summary>
public class AssetCacheOptions
{
    /// <summary>
    /// Gets or sets the cache lifetime in seconds for UI assets.
    /// Default is 3600 seconds (1 hour).
    /// </summary>
    /// <remarks>
    /// This value is used in the Cache-Control max-age directive.
    /// After this time expires, the client will revalidate the asset using ETags.
    /// </remarks>
    public int CacheLifetimeSeconds { get; set; } = 3600;

    /// <summary>
    /// Gets or sets whether to enable integrity revalidation after cache expiry.
    /// Default is true.
    /// </summary>
    /// <remarks>
    /// When enabled, assets will be served with ETags based on SHA256 content hashes,
    /// allowing post-expiry integrity verification during revalidation.
    /// </remarks>
    public bool EnableIntegrityRevalidation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to allow public caching.
    /// Default is true.
    /// </summary>
    /// <remarks>
    /// When true, Cache-Control will include 'public' directive.
    /// When false, 'private' will be used instead.
    /// </remarks>
    public bool AllowPublicCache { get; set; } = true;
}
