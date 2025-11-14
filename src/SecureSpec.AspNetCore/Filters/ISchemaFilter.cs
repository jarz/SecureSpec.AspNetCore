using Microsoft.OpenApi.Models;
using System.Reflection;

namespace SecureSpec.AspNetCore.Filters;

/// <summary>
/// Filter for modifying OpenAPI schemas during generation.
/// </summary>
public interface ISchemaFilter
{
    /// <summary>
    /// Applies modifications to a schema.
    /// </summary>
    /// <param name="schema">The schema to modify.</param>
    /// <param name="context">Filter context with metadata.</param>
    void Apply(OpenApiSchema schema, SchemaFilterContext context);
}

/// <summary>
/// Context information for schema filters.
/// </summary>
public class SchemaFilterContext
{
    /// <summary>
    /// Gets or sets the type being schema-generated.
    /// </summary>
    public required Type Type { get; init; }

    /// <summary>
    /// Gets or sets the schema ID.
    /// </summary>
    public required string SchemaId { get; init; }

    /// <summary>
    /// Gets or sets the member info (for properties/fields).
    /// </summary>
    public MemberInfo? MemberInfo { get; init; }

    /// <summary>
    /// Gets or sets the parameter info (for method parameters).
    /// </summary>
    public ParameterInfo? ParameterInfo { get; init; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public IDictionary<string, object?> Metadata { get; init; } = new Dictionary<string, object?>();
}
