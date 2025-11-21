using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Core.Attributes;
using SecureSpec.AspNetCore.Diagnostics;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SecureSpec.AspNetCore.Core;

/// <summary>
/// Discovers and catalogs ASP.NET Core endpoints for OpenAPI document generation.
/// </summary>
/// <remarks>
/// This class is registered as a Singleton and the DiscoverEndpointsAsync method is safe for
/// concurrent calls as it uses local variables for state. However, the class itself should not
/// be modified concurrently (e.g., changing constructor-injected dependencies).
/// </remarks>
public partial class ApiDiscoveryEngine
{
    // Compiled regex for normalizing route patterns (removing duplicate slashes)
    private static readonly Regex MultipleSlashesRegex = ForwardSlashRegex();

    // Constant for null placeholder in diagnostic messages
    private const string NullPlaceholder = "<null>";

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
        // Execute all discovery strategies and collect endpoints
        var allEndpoints = await ExecuteDiscoveryStrategiesAsync(cancellationToken).ConfigureAwait(false);

        _diagnosticsLogger.LogInfo(
            DiagnosticCodes.EndpointsDiscovered,
            $"Discovered {allEndpoints.Count} total endpoints from all strategies.");

        // Filter endpoints
        var filteredEndpoints = FilterEndpoints(allEndpoints, cancellationToken);

        _diagnosticsLogger.LogInfo(
            DiagnosticCodes.EndpointsDiscovered,
            $"After filtering: {filteredEndpoints.Count} endpoints included.");

        // Enrich and validate endpoints
        EnrichAndValidateEndpoints(filteredEndpoints, cancellationToken);

        return filteredEndpoints;
    }

    private async Task<List<EndpointMetadata>> ExecuteDiscoveryStrategiesAsync(CancellationToken cancellationToken)
    {
        // _discoveryStrategies is guaranteed non-null by constructor validation
        // Avoid double enumeration by checking if already materialized
        var strategies = _discoveryStrategies as IList<IEndpointDiscoveryStrategy> ?? _discoveryStrategies.ToList();

        if (strategies.Count == 0)
        {
            _diagnosticsLogger.LogWarning(
                DiagnosticCodes.EndpointFiltered,
                "No discovery strategies configured. No endpoints will be discovered.");
            return new List<EndpointMetadata>();
        }

        var allEndpoints = new List<EndpointMetadata>();
        // Use OrdinalIgnoreCase for consistency with response status code handling
        // This treats /api/users and /API/users as the same endpoint
        var endpointKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var strategy in strategies)
        {
            // Check for cancellation before processing each strategy
            cancellationToken.ThrowIfCancellationRequested();

            var endpoints = await strategy.DiscoverAsync(cancellationToken).ConfigureAwait(false);

            // Check for cancellation after strategy completes in case it ignored the token
            cancellationToken.ThrowIfCancellationRequested();

            if (endpoints == null)
            {
                continue;
            }

            foreach (var endpoint in endpoints)
            {
                ProcessDiscoveredEndpoint(endpoint, endpointKeys, allEndpoints, cancellationToken);
            }
        }

        return allEndpoints;
    }

    private void ProcessDiscoveredEndpoint(
        EndpointMetadata? endpoint,
        HashSet<string> endpointKeys,
        List<EndpointMetadata> allEndpoints,
        CancellationToken cancellationToken)
    {
        // Check for cancellation within inner loop for large endpoint collections
        cancellationToken.ThrowIfCancellationRequested();

        if (endpoint == null)
        {
            return;
        }

        // Initialize collections if null to prevent issues during enrichment
        endpoint.Tags ??= new List<string>();
        endpoint.Parameters ??= new List<OpenApiParameter>();
        endpoint.Security ??= new List<OpenApiSecurityRequirement>();
        endpoint.Responses ??= new Dictionary<string, OpenApiResponse>();

        // No need for ToUpperInvariant when using OrdinalIgnoreCase comparer
        var normalizedMethod = endpoint.HttpMethod?.Trim();
        // Normalize route pattern: trim and collapse multiple slashes
        var normalizedRoute = endpoint.RoutePattern?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedRoute))
        {
            // Replace multiple consecutive slashes with a single slash using compiled regex
            // Routes like "///" become "/" which is a valid root route
            normalizedRoute = MultipleSlashesRegex.Replace(normalizedRoute, "/");
        }

        if (string.IsNullOrWhiteSpace(normalizedMethod) || string.IsNullOrWhiteSpace(normalizedRoute))
        {
            _diagnosticsLogger.LogWarning(
                DiagnosticCodes.EndpointsDiscovered,
                "Skipped endpoint with missing HTTP method or route pattern during discovery.");
            return;
        }

        // Note: No need to check IsNullOrEmpty here as IsNullOrWhiteSpace above already handles it

        var key = $"{normalizedMethod}:{normalizedRoute}";
        if (endpointKeys.Add(key))
        {
            allEndpoints.Add(endpoint);
        }
        else
        {
            _diagnosticsLogger.LogInfo(
                DiagnosticCodes.EndpointFiltered,
                $"Duplicate endpoint ignored: {normalizedMethod} {normalizedRoute}");
        }
    }

    private List<EndpointMetadata> FilterEndpoints(List<EndpointMetadata> endpoints, CancellationToken cancellationToken)
    {
        // Note: _options.Value.Discovery is guaranteed to be non-null as it's initialized in SecureSpecOptions
        var discoveryOptions = _options.Value.Discovery;

        var filtered = new List<EndpointMetadata>();

        foreach (var endpoint in endpoints)
        {
            // Check for cancellation before processing each endpoint
            cancellationToken.ThrowIfCancellationRequested();

            // Skip null endpoints early
            if (endpoint == null)
            {
                continue;
            }

            var shouldInclude = ShouldIncludeEndpoint(endpoint, discoveryOptions, out var reason);

            if (shouldInclude)
            {
                filtered.Add(endpoint);
            }
            else
            {
                _diagnosticsLogger.LogInfo(
                    DiagnosticCodes.EndpointFiltered,
                    $"Excluded endpoint {endpoint.HttpMethod ?? NullPlaceholder} {endpoint.RoutePattern ?? NullPlaceholder}: {reason}");
            }
        }

        return filtered;
    }

    private static bool ShouldIncludeEndpoint(EndpointMetadata endpoint, DiscoveryOptions options, out string reason)
    {
        reason = string.Empty;

        // Validate inputs
        if (options == null)
        {
            // This should never happen as options are initialized in SecureSpecOptions
            // but if it does, fail safely by excluding rather than including everything
            throw new InvalidOperationException("Discovery options are not properly configured. This indicates a configuration error.");
        }

        // 1. Check for explicit [ExcludeFromSpec] attribute (highest precedence)
        if (HasExcludeAttribute(endpoint, out var excludeReason))
        {
            // Ensure reason is never null for consistency
            reason = excludeReason ?? string.Empty;
            if (string.IsNullOrWhiteSpace(reason))
            {
                reason = "Explicit exclusion";
            }
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
        if (options.IncludePredicate is { } includePredicate && includePredicate(endpoint))
        {
            reason = "Included by custom predicate";
            return true;
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
        // Check if [ApiController] is required and present
        if (endpoint.ControllerType != null && options.IncludeOnlyApiControllers && !endpoint.ControllerType.GetCustomAttributes<ApiControllerAttribute>().Any())
        {
            reason = "Controller missing [ApiController] attribute";
            return false;
        }

        // For minimal API endpoints (more robust detection)
        if (endpoint.IsMinimalApi && !options.IncludeMinimalApis)
        {
            reason = "Minimal APIs are disabled";
            return false;
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

    private void EnrichAndValidateEndpoints(List<EndpointMetadata> endpoints, CancellationToken cancellationToken)
    {
        // Track endpoints to remove due to corruption
        var endpointsToRemove = new List<EndpointMetadata>();

        // Enrich metadata for included endpoints
        foreach (var endpoint in endpoints)
        {
            // Check for cancellation before processing each endpoint
            cancellationToken.ThrowIfCancellationRequested();

            EnrichSingleEndpoint(endpoint, endpointsToRemove);
        }

        // Clean up corrupted or null endpoints
        CleanupCorruptedEndpoints(endpoints, endpointsToRemove);
    }

    private void EnrichSingleEndpoint(EndpointMetadata endpoint, List<EndpointMetadata> endpointsToRemove)
    {
        // Store original values for validation
        var originalMethod = endpoint.HttpMethod;
        var originalRoute = endpoint.RoutePattern;

        try
        {
            _metadataExtractor.EnrichMetadata(endpoint);
        }
        catch (OutOfMemoryException)
        {
            throw;
        }
        catch (StackOverflowException)
        {
            throw;
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            _diagnosticsLogger.LogWarning(
                DiagnosticCodes.MetadataExtractionFailed,
                $"Failed to enrich metadata for {originalMethod} {originalRoute}: {ex.GetType().Name}");
        }
        finally
        {
            ValidateEnrichedEndpoint(endpoint, originalMethod, originalRoute, endpointsToRemove);
        }
    }

    private void ValidateEnrichedEndpoint(
        EndpointMetadata? endpoint,
        string originalMethod,
        string originalRoute,
        List<EndpointMetadata> endpointsToRemove)
    {
        // Validate required properties are still valid after enrichment
        // Defensive check: if endpoint itself is somehow null (shouldn't happen), skip it
        if (endpoint == null)
        {
            _diagnosticsLogger.LogError(
                DiagnosticCodes.MetadataExtractionFailed,
                $"Endpoint {originalMethod ?? NullPlaceholder} {originalRoute ?? NullPlaceholder} became null during enrichment. This should not happen.");
            // Cannot process null endpoint, but we need to track it for removal
            // Note: We can't add null to endpointsToRemove, so filteredEndpoints will need null filtering
            return;
        }

        if (string.IsNullOrWhiteSpace(endpoint.HttpMethod) || string.IsNullOrWhiteSpace(endpoint.RoutePattern))
        {
            _diagnosticsLogger.LogError(
                DiagnosticCodes.MetadataExtractionFailed,
                $"Endpoint {originalMethod ?? NullPlaceholder} {originalRoute ?? NullPlaceholder} has invalid state after enrichment: required properties were modified to invalid values. Removing from results.");
            endpointsToRemove.Add(endpoint);
            return;
        }

        // Detect if endpoint identity changed during enrichment
        if (!string.Equals(endpoint.HttpMethod, originalMethod, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(endpoint.RoutePattern, originalRoute, StringComparison.OrdinalIgnoreCase))
        {
            _diagnosticsLogger.LogWarning(
                DiagnosticCodes.MetadataExtractionFailed,
                $"Endpoint identity changed during enrichment from {originalMethod} {originalRoute} to {endpoint.HttpMethod} {endpoint.RoutePattern}");
        }

        // Always freeze collections to maintain consistency
        // Only do this for endpoints we're keeping
        try
        {
            MakeEndpointCollectionsReadOnly(endpoint);
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception freezeEx)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            _diagnosticsLogger.LogWarning(
                DiagnosticCodes.MetadataExtractionFailed,
                $"Failed to freeze collections for {endpoint.HttpMethod} {endpoint.RoutePattern}: {freezeEx.GetType().Name}");
            // Collection freezing failed, but endpoint is still valid - add to removal list to be safe
            endpointsToRemove.Add(endpoint);
        }
    }

    private void CleanupCorruptedEndpoints(List<EndpointMetadata> endpoints, List<EndpointMetadata> endpointsToRemove)
    {
        // Remove corrupted endpoints using RemoveAll
        // Since EndpointMetadata uses reference equality, list Contains is as efficient as HashSet
        if (endpointsToRemove.Count > 0)
        {
            endpoints.RemoveAll(endpointsToRemove.Contains);
        }

        // Also remove any null endpoints that may have appeared during enrichment
        var nullCount = endpoints.RemoveAll(e => e == null);
        if (nullCount > 0)
        {
            _diagnosticsLogger.LogError(
                DiagnosticCodes.EndpointsDiscovered,
                $"Removed {nullCount} null endpoints after enrichment.");
        }

        if (endpointsToRemove.Count > 0)
        {
            _diagnosticsLogger.LogWarning(
                DiagnosticCodes.EndpointsDiscovered,
                $"Removed {endpointsToRemove.Count} corrupted endpoints after enrichment. Final count: {endpoints.Count}");
        }
    }

    private static void MakeEndpointCollectionsReadOnly(EndpointMetadata endpoint)
    {
        MakeListReadOnly(endpoint.Tags, list => endpoint.Tags = list);
        MakeListReadOnly(endpoint.Parameters, list => endpoint.Parameters = list);
        MakeListReadOnly(endpoint.Security, list => endpoint.Security = list);
        MakeDictionaryReadOnly(endpoint.Responses, dict => endpoint.Responses = dict);
    }

    private static void MakeListReadOnly<T>(IList<T>? collection, Action<IList<T>> setter)
    {
        if (collection == null)
        {
            // Ensure downstream consumers never see null collections
            setter(new ReadOnlyCollection<T>(Array.Empty<T>()));
            return;
        }

        if (collection.IsReadOnly)
        {
            return;
        }

        // Optimize for common collection types to avoid allocations
        var readOnlyCollection = collection switch
        {
            List<T> list => new ReadOnlyCollection<T>(list),
            T[] array => new ReadOnlyCollection<T>(array),
            _ => new ReadOnlyCollection<T>(collection.ToList())
        };

        setter(readOnlyCollection);
    }

    private static void MakeDictionaryReadOnly(IDictionary<string, OpenApiResponse>? dictionary, Action<IDictionary<string, OpenApiResponse>> setter)
    {
        if (dictionary == null)
        {
            // Replace null dictionary with an empty, case-insensitive read-only instance
            setter(new ReadOnlyDictionary<string, OpenApiResponse>(new Dictionary<string, OpenApiResponse>(StringComparer.OrdinalIgnoreCase)));
            return;
        }

        if (dictionary is ReadOnlyDictionary<string, OpenApiResponse>)
        {
            return;
        }

        // Check if dictionary already uses case-insensitive comparison and wrap directly
        if (dictionary is Dictionary<string, OpenApiResponse> dict &&
            ReferenceEquals(dict.Comparer, StringComparer.OrdinalIgnoreCase))
        {
            setter(new ReadOnlyDictionary<string, OpenApiResponse>(dict));
            return;
        }

        // Convert to case-insensitive dictionary, handling potential key conflicts
        try
        {
            var caseInsensitiveDict = new Dictionary<string, OpenApiResponse>(dictionary, StringComparer.OrdinalIgnoreCase);
            setter(new ReadOnlyDictionary<string, OpenApiResponse>(caseInsensitiveDict));
        }
        catch (ArgumentException)
        {
            // Dictionary contains keys that differ only in case, keep original with case-sensitive comparer
            var caseSensitiveDict = new Dictionary<string, OpenApiResponse>(dictionary);
            setter(new ReadOnlyDictionary<string, OpenApiResponse>(caseSensitiveDict));
        }
    }

    [GeneratedRegex(@"/+", RegexOptions.Compiled)]
    private static partial Regex ForwardSlashRegex();
}
