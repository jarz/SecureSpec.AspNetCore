using Microsoft.OpenApi.Models;

namespace SecureSpec.AspNetCore.Filters;

/// <summary>
/// Filter for modifying OpenAPI request bodies during generation.
/// </summary>
public interface IRequestBodyFilter
{
    /// <summary>
    /// Applies modifications to a request body.
    /// </summary>
    /// <param name="requestBody">The request body to modify.</param>
    /// <param name="context">Filter context with metadata.</param>
    void Apply(OpenApiRequestBody requestBody, RequestBodyFilterContext context);
}

/// <summary>
/// Context information for request body filters.
/// </summary>
public class RequestBodyFilterContext
{
    /// <summary>
    /// Gets or sets the parameter info for the body parameter.
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
