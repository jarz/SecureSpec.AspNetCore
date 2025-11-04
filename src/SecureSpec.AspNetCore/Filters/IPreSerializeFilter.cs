using Microsoft.OpenApi.Models;

namespace SecureSpec.AspNetCore.Filters;

/// <summary>
/// Filter for final document mutations before serialization.
/// This is the last stage before the document is serialized.
/// </summary>
public interface IPreSerializeFilter
{
    /// <summary>
    /// Applies final modifications to a document before serialization.
    /// </summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="context">Filter context with metadata.</param>
    void Apply(OpenApiDocument document, PreSerializeFilterContext context);
}

/// <summary>
/// Context information for pre-serialize filters.
/// </summary>
public class PreSerializeFilterContext
{
    /// <summary>
    /// Gets or sets the document name.
    /// </summary>
    public required string DocumentName { get; init; }

    /// <summary>
    /// Gets or sets the output format (json, yaml).
    /// </summary>
    public required string OutputFormat { get; init; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public IDictionary<string, object?> Metadata { get; init; } = new Dictionary<string, object?>();
}
