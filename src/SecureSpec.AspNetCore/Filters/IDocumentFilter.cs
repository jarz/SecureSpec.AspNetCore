using Microsoft.OpenApi.Models;

namespace SecureSpec.AspNetCore.Filters;

/// <summary>
/// Filter for modifying complete OpenAPI documents during generation.
/// </summary>
public interface IDocumentFilter
{
    /// <summary>
    /// Applies modifications to a document.
    /// </summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="context">Filter context with metadata.</param>
    void Apply(OpenApiDocument document, DocumentFilterContext context);
}

/// <summary>
/// Context information for document filters.
/// </summary>
public class DocumentFilterContext
{
    /// <summary>
    /// Gets or sets the document name.
    /// </summary>
    public required string DocumentName { get; init; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public IDictionary<string, object?> Metadata { get; init; } = new Dictionary<string, object?>();
}
