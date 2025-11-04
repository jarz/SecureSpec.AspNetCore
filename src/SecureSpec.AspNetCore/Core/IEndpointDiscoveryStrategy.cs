namespace SecureSpec.AspNetCore.Core;

/// <summary>
/// Strategy for discovering API endpoints from ASP.NET Core applications.
/// </summary>
public interface IEndpointDiscoveryStrategy
{
    /// <summary>
    /// Discovers endpoints using this strategy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of discovered endpoint metadata.</returns>
    Task<IEnumerable<EndpointMetadata>> DiscoverAsync(CancellationToken cancellationToken = default);
}
