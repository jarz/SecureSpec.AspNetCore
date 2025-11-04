#pragma warning disable CA1031 // Do not catch general exception types - intentional for metadata extraction resilience

using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Controllers;
using SecureSpec.AspNetCore.Diagnostics;

namespace SecureSpec.AspNetCore.Core;

/// <summary>
/// Discovers controller-based API endpoints using MVC API Explorer.
/// </summary>
public class ControllerDiscoveryStrategy : IEndpointDiscoveryStrategy
{
    private readonly IActionDescriptorCollectionProvider _actionDescriptorProvider;
    private readonly DiagnosticsLogger _diagnosticsLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ControllerDiscoveryStrategy"/> class.
    /// </summary>
    /// <param name="actionDescriptorProvider">Provider for action descriptors.</param>
    /// <param name="diagnosticsLogger">Logger for diagnostic events.</param>
    public ControllerDiscoveryStrategy(
        IActionDescriptorCollectionProvider actionDescriptorProvider,
        DiagnosticsLogger diagnosticsLogger)
    {
        _actionDescriptorProvider = actionDescriptorProvider ?? throw new ArgumentNullException(nameof(actionDescriptorProvider));
        _diagnosticsLogger = diagnosticsLogger ?? throw new ArgumentNullException(nameof(diagnosticsLogger));
    }

    /// <inheritdoc />
    public Task<IEnumerable<EndpointMetadata>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var endpoints = new List<EndpointMetadata>();

        var actionDescriptors = _actionDescriptorProvider.ActionDescriptors.Items;

        _diagnosticsLogger.LogInfo(
            DiagnosticCodes.Discovery.EndpointsDiscovered,
            $"Discovering controller endpoints. Found {actionDescriptors.Count} action descriptors.");

        foreach (var actionDescriptor in actionDescriptors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Only process controller actions
            if (actionDescriptor is not ControllerActionDescriptor controllerAction)
            {
                continue;
            }

            try
            {
                var metadata = CreateEndpointMetadata(controllerAction);
                endpoints.Add(metadata);
            }
            catch (Exception ex)
            {
                _diagnosticsLogger.LogError(
                    DiagnosticCodes.Discovery.MetadataExtractionFailed,
                    $"Failed to extract metadata from controller action {controllerAction.DisplayName}: {ex.Message}");
            }
        }

        _diagnosticsLogger.LogInfo(
            DiagnosticCodes.Discovery.EndpointsDiscovered,
            $"Controller discovery completed. Discovered {endpoints.Count} endpoints.");

        return Task.FromResult<IEnumerable<EndpointMetadata>>(endpoints);
    }

    private EndpointMetadata CreateEndpointMetadata(ControllerActionDescriptor actionDescriptor)
    {
        // Extract HTTP method from action descriptor
        var httpMethod = GetHttpMethod(actionDescriptor);

        // Build route pattern
        var routePattern = actionDescriptor.AttributeRouteInfo?.Template ?? string.Empty;

        return new EndpointMetadata
        {
            HttpMethod = httpMethod,
            RoutePattern = routePattern,
            OperationName = actionDescriptor.ActionName,
            MethodInfo = actionDescriptor.MethodInfo,
            ControllerType = actionDescriptor.ControllerTypeInfo.AsType(),
            ActionDescriptor = actionDescriptor
        };
    }

    private static string GetHttpMethod(ControllerActionDescriptor actionDescriptor)
    {
        // Check ActionConstraints for HTTP method metadata
        var httpMethodMetadata = actionDescriptor.ActionConstraints?
            .OfType<Microsoft.AspNetCore.Mvc.ActionConstraints.HttpMethodActionConstraint>()
            .FirstOrDefault();

        if (httpMethodMetadata != null && httpMethodMetadata.HttpMethods.Any())
        {
            return httpMethodMetadata.HttpMethods.First();
        }

        // Default to GET if no method specified
        return "GET";
    }
}
