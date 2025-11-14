#pragma warning disable CA1031 // Do not catch general exception types - intentional for metadata extraction resilience

using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using SecureSpec.AspNetCore.Core.Attributes;
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
    /// <remarks>
    /// This method is thread-safe and can be called concurrently.
    /// </remarks>
    public Task<IEnumerable<EndpointMetadata>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        // Check cancellation before any work
        cancellationToken.ThrowIfCancellationRequested();

        var endpoints = new List<EndpointMetadata>();

        // Create snapshot to avoid collection modification issues
        List<RouteEndpoint> routeEndpoints;
        try
        {
            routeEndpoints = _endpointDataSource.Endpoints.OfType<RouteEndpoint>().ToList();
        }
        catch (ObjectDisposedException ex)
        {
            _diagnosticsLogger.LogError(
                DiagnosticCodes.Discovery.MetadataExtractionFailed,
                $"EndpointDataSource was disposed during enumeration: {ex.Message}");
            return Task.FromResult<IEnumerable<EndpointMetadata>>(endpoints);
        }
        catch (InvalidOperationException ex)
        {
            _diagnosticsLogger.LogError(
                DiagnosticCodes.Discovery.MetadataExtractionFailed,
                $"EndpointDataSource collection was modified during enumeration: {ex.Message}");
            return Task.FromResult<IEnumerable<EndpointMetadata>>(endpoints);
        }

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
                    $"Failed to extract metadata from minimal API endpoint {routeEndpoint.DisplayName ?? routeEndpoint.RoutePattern?.RawText ?? "<unknown>"}: {ex.Message}");
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

        // Defensive null check for metadata
        if (endpoint.Metadata == null)
        {
            return false;
        }

        // Check for explicit exclusion attribute
        if (endpoint.Metadata.GetMetadata<ExcludeFromSpecAttribute>() != null)
        {
            return false;
        }

        var routePattern = endpoint.RoutePattern.RawText;

        // Only filter true framework internals that should NEVER be documented
        // These are ASP.NET Core internal routes that are never user-facing APIs
        if (routePattern.StartsWith("/_", StringComparison.OrdinalIgnoreCase))
        {
            // Framework routes like /_framework/, /_blazor/, /_vs/browserLink, etc.
            return false;
        }

        // Check for HTTP method metadata - this is the primary indicator of an API endpoint
        var httpMethodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        return httpMethodMetadata != null;
    }

    private EndpointMetadata? CreateEndpointMetadata(RouteEndpoint routeEndpoint)
    {
        var httpMethodMetadata = routeEndpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        if (httpMethodMetadata?.HttpMethods == null || httpMethodMetadata.HttpMethods.Count == 0)
        {
            return null;
        }

        // Clean up HTTP methods - accept any methods that ASP.NET Core accepts
        // Use HashSet for efficient deduplication with case-sensitive comparison
        // since we normalize to uppercase
        var httpMethodsSet = new HashSet<string>(httpMethodMetadata.HttpMethods.Count, StringComparer.Ordinal);
        string? firstMethod = null;

        foreach (var method in httpMethodMetadata.HttpMethods)
        {
            if (string.IsNullOrWhiteSpace(method))
            {
                continue;
            }

            var normalizedMethod = method.Trim().ToUpperInvariant();
            if (httpMethodsSet.Add(normalizedMethod))
            {
                // Capture the first unique method
                firstMethod ??= normalizedMethod;
            }
        }

        // Additional safety check after conversion
        if (httpMethodsSet.Count == 0 || firstMethod == null)
        {
            return null;
        }

        // Use first method for HttpMethod property (for backward compatibility)
        // Consumers should use HttpMethods collection for complete information
        var httpMethod = firstMethod;
        var httpMethods = new List<string>(httpMethodsSet);

        // RoutePattern.RawText is guaranteed non-null by IsApiEndpoint validation
        var routePattern = routeEndpoint.RoutePattern.RawText!;

        // Extract MethodInfo if available
        MethodInfo? methodInfo = routeEndpoint.Metadata.GetMetadata<MethodInfo>();

        // Detect if this is actually a controller endpoint
        var actionDescriptor = routeEndpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
        var isMinimalApi = actionDescriptor == null;

        // Better operation name fallback chain: DisplayName → MethodInfo.Name → RoutePattern
        var operationName = routeEndpoint.DisplayName;
        if (string.IsNullOrWhiteSpace(operationName))
        {
            operationName = methodInfo?.Name;
            if (string.IsNullOrWhiteSpace(operationName))
            {
                operationName = routePattern;
            }
        }

        return new EndpointMetadata
        {
            HttpMethod = httpMethod,
            HttpMethods = httpMethods,
            RoutePattern = routePattern,
            OperationName = operationName,
            MethodInfo = methodInfo,
            RouteEndpoint = routeEndpoint,
            IsMinimalApi = isMinimalApi
        };
    }
}
