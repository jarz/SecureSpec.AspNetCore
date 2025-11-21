using Microsoft.OpenApi.Models;

namespace SecureSpec.AspNetCore.Filters;

/// <summary>
/// Filter for modifying OpenAPI parameters during generation.
/// </summary>
public interface IParameterFilter
{
    /// <summary>
    /// Applies modifications to a parameter.
    /// </summary>
    /// <param name="parameter">The parameter to modify.</param>
    /// <param name="context">Filter context with metadata.</param>
    void Apply(OpenApiParameter parameter, ParameterFilterContext context);
}

/// <summary>
/// Context information for parameter filters.
/// </summary>
public class ParameterFilterContext
{
    /// <summary>
    /// Gets or sets the parameter info.
    /// </summary>
    public required System.Reflection.ParameterInfo ParameterInfo { get; init; }

    /// <summary>
    /// Gets or sets the operation ID.
    /// </summary>
    public required string OperationId { get; init; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public IDictionary<string, object?> Metadata { get; init; } = new Dictionary<string, object?>();
}
