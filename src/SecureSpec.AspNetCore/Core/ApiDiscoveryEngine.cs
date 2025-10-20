namespace SecureSpec.AspNetCore.Core;

/// <summary>
/// Discovers and catalogs ASP.NET Core endpoints for OpenAPI document generation.
/// </summary>
public class ApiDiscoveryEngine
{
    /// <summary>
    /// Discovers all API endpoints in the application.
    /// </summary>
    /// <returns>A collection of discovered endpoint metadata.</returns>
    public Task<IEnumerable<EndpointMetadata>> DiscoverEndpointsAsync()
    {
        // TODO: Implement endpoint discovery
        throw new NotImplementedException();
    }
}

/// <summary>
/// Metadata for a discovered API endpoint.
/// </summary>
public class EndpointMetadata
{
    /// <summary>
    /// Gets or sets the HTTP method (GET, POST, etc.).
    /// </summary>
    public required string HttpMethod { get; set; }

    /// <summary>
    /// Gets or sets the route pattern.
    /// </summary>
    public required string RoutePattern { get; set; }

    /// <summary>
    /// Gets or sets the operation name.
    /// </summary>
    public string? OperationName { get; set; }
}
