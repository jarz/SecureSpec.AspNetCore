using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace SecureSpec.AspNetCore.Schema;

/// <summary>
/// Represents the context for example resolution with all available example sources.
/// </summary>
public sealed class ExampleContext
{
    /// <summary>
    /// Gets the named examples collection (highest priority).
    /// </summary>
    public IDictionary<string, OpenApiExample> NamedExamples { get; } = new Dictionary<string, OpenApiExample>();

    /// <summary>
    /// Gets or sets the single example value (from example property or attribute).
    /// </summary>
    public IOpenApiAny? SingleExample { get; set; }

    /// <summary>
    /// Gets or sets the component example reference.
    /// </summary>
    public OpenApiReference? ComponentExample { get; set; }

    /// <summary>
    /// Gets or sets whether example generation is blocked.
    /// </summary>
    public bool IsBlocked { get; set; }

    /// <summary>
    /// Gets or sets the schema for fallback example generation.
    /// </summary>
    public OpenApiSchema? Schema { get; set; }
}
