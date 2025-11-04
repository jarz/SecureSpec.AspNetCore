using Microsoft.OpenApi.Models;
using System.Reflection;

namespace SecureSpec.AspNetCore.Filters;

/// <summary>
/// Filter for modifying OpenAPI operations during generation.
/// </summary>
public interface IOperationFilter
{
    /// <summary>
    /// Applies modifications to an operation.
    /// </summary>
    /// <param name="operation">The operation to modify.</param>
    /// <param name="context">Filter context with metadata.</param>
    void Apply(OpenApiOperation operation, OperationFilterContext context);
}

/// <summary>
/// Context information for operation filters.
/// </summary>
public class OperationFilterContext
{
    /// <summary>
    /// Gets or sets the method info for this operation.
    /// </summary>
    public required MethodInfo MethodInfo { get; init; }

    /// <summary>
    /// Gets or sets the HTTP method.
    /// </summary>
    public required string HttpMethod { get; init; }

    /// <summary>
    /// Gets or sets the route pattern.
    /// </summary>
    public required string RoutePattern { get; init; }

    /// <summary>
    /// Gets or sets the controller type (for controller-based endpoints).
    /// </summary>
    public Type? ControllerType { get; init; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public IDictionary<string, object?> Metadata { get; init; } = new Dictionary<string, object?>();
}
