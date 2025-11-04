using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace SecureSpec.AspNetCore.Core;

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

    /// <summary>
    /// Gets or sets a value indicating whether this endpoint was discovered from a minimal API route.
    /// </summary>
    public bool IsMinimalApi { get; set; }
}
