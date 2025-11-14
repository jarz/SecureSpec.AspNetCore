namespace SecureSpec.AspNetCore.Configuration;

/// <summary>
/// Options for controlling endpoint discovery.
/// </summary>
public class DiscoveryOptions
{
    /// <summary>
    /// Gets or sets whether to include only controllers with [ApiController] attribute.
    /// Default: true (convention-based filtering).
    /// </summary>
    public bool IncludeOnlyApiControllers { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include minimal API endpoints.
    /// Default: true.
    /// </summary>
    public bool IncludeMinimalApis { get; set; } = true;

    /// <summary>
    /// Gets or sets a custom predicate for endpoint inclusion.
    /// This is evaluated after explicit attributes but before convention defaults.
    /// </summary>
    public Func<Core.EndpointMetadata, bool>? IncludePredicate { get; set; }

    /// <summary>
    /// Gets or sets a custom predicate for endpoint exclusion.
    /// This is evaluated after explicit exclude attributes.
    /// </summary>
    public Func<Core.EndpointMetadata, bool>? ExcludePredicate { get; set; }

    /// <summary>
    /// Gets or sets whether to include endpoints marked as [Obsolete].
    /// Default: true (include but mark as deprecated).
    /// </summary>
    public bool IncludeObsolete { get; set; } = true;
}
