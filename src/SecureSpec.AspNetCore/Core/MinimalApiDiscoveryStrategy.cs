#pragma warning disable CA1031 // Do not catch general exception types - intentional for metadata extraction resilience

using Microsoft.AspNetCore.Routing;
using SecureSpec.AspNetCore.Diagnostics;
using System.Reflection;

namespace SecureSpec.AspNetCore.Core;

/// <summary>
/// Discovers minimal API endpoints using endpoint data sources.
/// </summary>
public class MinimalApiDiscoveryStrategy : IEndpointDiscoveryStrategy
{
    private readonly EndpointDataSource _endpointDataSource;
    private readonly DiagnosticsLogger _diagnosticsLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MinimalApiDiscoveryStrategy"/> class.
    /// </summary>
    /// <param name="endpointDataSource">Source for endpoint data.</param>
    /// <param name="diagnosticsLogger">Logger for diagnostic events.</param>
    public MinimalApiDiscoveryStrategy(
        EndpointDataSource endpointDataSource,
        DiagnosticsLogger diagnosticsLogger)
    {
        _endpointDataSource = endpointDataSource ?? throw new ArgumentNullException(nameof(endpointDataSource));
        _diagnosticsLogger = diagnosticsLogger ?? throw new ArgumentNullException(nameof(diagnosticsLogger));
    }

    /// <inheritdoc />
    public Task<IEnumerable<EndpointMetadata>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var endpoints = new List<EndpointMetadata>();
        var routeEndpoints = _endpointDataSource.Endpoints.OfType<RouteEndpoint>().ToList();

        _diagnosticsLogger.LogInfo(
            DiagnosticCodes.Discovery.EndpointsDiscovered,
            $"Discovering minimal API endpoints. Found {routeEndpoints.Count} route endpoints.");

        foreach (var routeEndpoint in routeEndpoints)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Skip non-API endpoints (e.g., Razor Pages, static files)
            if (!IsApiEndpoint(routeEndpoint))
            {
                continue;
            }

            try
            {
                var metadata = CreateEndpointMetadata(routeEndpoint);
                if (metadata != null)
                {
                    endpoints.Add(metadata);
                }
            }
            catch (Exception ex)
            {
                _diagnosticsLogger.LogError(
                    DiagnosticCodes.Discovery.MetadataExtractionFailed,
                    $"Failed to extract metadata from minimal API endpoint {routeEndpoint.DisplayName}: {ex.Message}");
            }
        }

        _diagnosticsLogger.LogInfo(
            DiagnosticCodes.Discovery.EndpointsDiscovered,
            $"Minimal API discovery completed. Discovered {endpoints.Count} endpoints.");

        return Task.FromResult<IEnumerable<EndpointMetadata>>(endpoints);
    }

    private static bool IsApiEndpoint(RouteEndpoint endpoint)
    {
        // Skip endpoints without a route pattern
        if (string.IsNullOrEmpty(endpoint.RoutePattern?.RawText))
        {
            return false;
        }

        // Skip framework endpoints (Blazor, SignalR, etc.)
        var displayName = endpoint.DisplayName ?? string.Empty;
        if (displayName.Contains("/_framework/", StringComparison.Ordinal) ||
            displayName.Contains("/_blazor/", StringComparison.Ordinal) ||
            displayName.Contains("/hub/", StringComparison.Ordinal))
        {
            return false;
        }

        // Check for HTTP method metadata
        var httpMethodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        return httpMethodMetadata != null;
    }

    private EndpointMetadata? CreateEndpointMetadata(RouteEndpoint routeEndpoint)
    {
        var httpMethodMetadata = routeEndpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        if (httpMethodMetadata == null || httpMethodMetadata.HttpMethods.Count == 0)
        {
            return null;
        }

        var httpMethod = httpMethodMetadata.HttpMethods[0];
        var routePattern = routeEndpoint.RoutePattern.RawText ?? string.Empty;

        // Extract MethodInfo if available
        MethodInfo? methodInfo = routeEndpoint.Metadata.GetMetadata<MethodInfo>();

        return new EndpointMetadata
        {
            HttpMethod = httpMethod,
            RoutePattern = routePattern,
            OperationName = routeEndpoint.DisplayName,
            MethodInfo = methodInfo,
            RouteEndpoint = routeEndpoint
        };
    }
}
