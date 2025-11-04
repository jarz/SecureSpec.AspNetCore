using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Core.Attributes;
using SecureSpec.AspNetCore.Diagnostics;
using System.Reflection;

namespace SecureSpec.AspNetCore.Core;

/// <summary>
/// Discovers and catalogs ASP.NET Core endpoints for OpenAPI document generation.
/// </summary>
public class ApiDiscoveryEngine
{
    private readonly IEnumerable<IEndpointDiscoveryStrategy> _discoveryStrategies;
    private readonly MetadataExtractor _metadataExtractor;
    private readonly IOptions<SecureSpecOptions> _options;
    private readonly DiagnosticsLogger _diagnosticsLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiDiscoveryEngine"/> class.
    /// </summary>
    /// <param name="discoveryStrategies">Collection of discovery strategies.</param>
    /// <param name="metadataExtractor">Metadata extractor for enriching endpoint information.</param>
    /// <param name="options">Configuration options.</param>
    /// <param name="diagnosticsLogger">Logger for diagnostic events.</param>
    public ApiDiscoveryEngine(
        IEnumerable<IEndpointDiscoveryStrategy> discoveryStrategies,
        MetadataExtractor metadataExtractor,
        IOptions<SecureSpecOptions> options,
        DiagnosticsLogger diagnosticsLogger)
    {
        _discoveryStrategies = discoveryStrategies ?? throw new ArgumentNullException(nameof(discoveryStrategies));
        _metadataExtractor = metadataExtractor ?? throw new ArgumentNullException(nameof(metadataExtractor));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _diagnosticsLogger = diagnosticsLogger ?? throw new ArgumentNullException(nameof(diagnosticsLogger));
    }

    /// <summary>
    /// Discovers all API endpoints in the application.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of discovered endpoint metadata.</returns>
    public async Task<IEnumerable<EndpointMetadata>> DiscoverEndpointsAsync(CancellationToken cancellationToken = default)
    {
        var allEndpoints = new List<EndpointMetadata>();

        // Run all discovery strategies
        foreach (var strategy in _discoveryStrategies)
        {
            var endpoints = await strategy.DiscoverAsync(cancellationToken).ConfigureAwait(false);
            allEndpoints.AddRange(endpoints);
        }

        _diagnosticsLogger.LogInfo(
            DiagnosticCodes.Discovery.EndpointsDiscovered,
            $"Discovered {allEndpoints.Count} total endpoints from all strategies.");

        // Filter endpoints
        var filteredEndpoints = FilterEndpoints(allEndpoints);

        _diagnosticsLogger.LogInfo(
            DiagnosticCodes.Discovery.EndpointsDiscovered,
            $"After filtering: {filteredEndpoints.Count} endpoints included.");

        // Enrich metadata for included endpoints
        foreach (var endpoint in filteredEndpoints)
        {
            _metadataExtractor.EnrichMetadata(endpoint);
        }

        return filteredEndpoints;
    }

    private List<EndpointMetadata> FilterEndpoints(List<EndpointMetadata> endpoints)
    {
        var discoveryOptions = _options.Value.Discovery;
        var filtered = new List<EndpointMetadata>();

        foreach (var endpoint in endpoints)
        {
            var includeReason = ShouldIncludeEndpoint(endpoint, discoveryOptions, out var reason);

            if (includeReason)
            {
                filtered.Add(endpoint);
            }
            else
            {
                _diagnosticsLogger.LogInfo(
                    DiagnosticCodes.Discovery.EndpointFiltered,
                    $"Excluded endpoint {endpoint.HttpMethod} {endpoint.RoutePattern}: {reason}");
            }
        }

        return filtered;
    }

    private bool ShouldIncludeEndpoint(EndpointMetadata endpoint, DiscoveryOptions options, out string reason)
    {
        reason = string.Empty;

        // 1. Check for explicit [ExcludeFromSpec] attribute (highest precedence)
        if (HasExcludeAttribute(endpoint, out var excludeReason))
        {
            reason = excludeReason ?? "Explicit exclusion";
            return false;
        }

        // 2. Check for explicit [IncludeInSpec] attribute
        if (HasIncludeAttribute(endpoint))
        {
            reason = "Explicit inclusion";
            return true;
        }

        // 3. Apply custom exclusion predicate
        if (options.ExcludePredicate != null && options.ExcludePredicate(endpoint))
        {
            reason = "Excluded by custom predicate";
            return false;
        }

        // 4. Apply custom inclusion predicate
        if (options.IncludePredicate != null)
        {
            if (options.IncludePredicate(endpoint))
            {
                reason = "Included by custom predicate";
                return true;
            }
            else
            {
                reason = "Not matched by custom predicate";
                return false;
            }
        }

        // 5. Convention-based filtering
        return ApplyConventionFilters(endpoint, options, out reason);
    }

    private static bool HasExcludeAttribute(EndpointMetadata endpoint, out string? excludeReason)
    {
        excludeReason = null;

        // Check method-level attribute
        var methodAttribute = endpoint.MethodInfo?.GetCustomAttribute<ExcludeFromSpecAttribute>();
        if (methodAttribute != null)
        {
            excludeReason = methodAttribute.Reason;
            return true;
        }

        // Check controller-level attribute
        var controllerAttribute = endpoint.ControllerType?.GetCustomAttribute<ExcludeFromSpecAttribute>();
        if (controllerAttribute != null)
        {
            excludeReason = controllerAttribute.Reason;
            return true;
        }

        return false;
    }

    private static bool HasIncludeAttribute(EndpointMetadata endpoint)
    {
        // Check method-level attribute
        if (endpoint.MethodInfo?.GetCustomAttribute<IncludeInSpecAttribute>() != null)
        {
            return true;
        }

        // Check controller-level attribute
        if (endpoint.ControllerType?.GetCustomAttribute<IncludeInSpecAttribute>() != null)
        {
            return true;
        }

        return false;
    }

    private static bool ApplyConventionFilters(EndpointMetadata endpoint, DiscoveryOptions options, out string reason)
    {
        reason = string.Empty;

        // For controller endpoints
        if (endpoint.ControllerType != null)
        {
            // Check if [ApiController] is required and present
            if (options.IncludeOnlyApiControllers)
            {
                if (!endpoint.ControllerType.GetCustomAttributes<ApiControllerAttribute>().Any())
                {
                    reason = "Controller missing [ApiController] attribute";
                    return false;
                }
            }
        }

        // For minimal API endpoints
        if (endpoint.RouteEndpoint != null)
        {
            if (!options.IncludeMinimalApis)
            {
                reason = "Minimal APIs are disabled";
                return false;
            }
        }

        // Check obsolete status
        if (!options.IncludeObsolete && endpoint.Deprecated)
        {
            reason = "Endpoint is obsolete and obsolete endpoints are excluded";
            return false;
        }

        reason = "Included by convention";
        return true;
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

    /// <summary>
    /// Gets or sets the operation ID for OpenAPI.
    /// </summary>
    public string? OperationId { get; set; }

    /// <summary>
    /// Gets or sets the summary description.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Gets or sets the detailed description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether the endpoint is deprecated.
    /// </summary>
    public bool Deprecated { get; set; }

    /// <summary>
    /// Gets or sets the tags for grouping operations.
    /// </summary>
    public IList<string> Tags { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the parameters for this endpoint.
    /// </summary>
    public IList<OpenApiParameter> Parameters { get; set; } = new List<OpenApiParameter>();

    /// <summary>
    /// Gets or sets the request body definition.
    /// </summary>
    public OpenApiRequestBody? RequestBody { get; set; }

    /// <summary>
    /// Gets or sets the response definitions.
    /// </summary>
    public IDictionary<string, OpenApiResponse> Responses { get; set; } = new Dictionary<string, OpenApiResponse>();

    /// <summary>
    /// Gets or sets the security requirements for this endpoint.
    /// </summary>
    public IList<OpenApiSecurityRequirement> Security { get; set; } = new List<OpenApiSecurityRequirement>();

    /// <summary>
    /// Gets or sets the MethodInfo for reflection (controllers and some minimal APIs).
    /// </summary>
    public MethodInfo? MethodInfo { get; set; }

    /// <summary>
    /// Gets or sets the controller type (for controller-based endpoints).
    /// </summary>
    public Type? ControllerType { get; set; }

    /// <summary>
    /// Gets or sets the controller action descriptor (for controller-based endpoints).
    /// </summary>
    public ControllerActionDescriptor? ActionDescriptor { get; set; }

    /// <summary>
    /// Gets or sets the route endpoint (for minimal API endpoints).
    /// </summary>
    public RouteEndpoint? RouteEndpoint { get; set; }
}
