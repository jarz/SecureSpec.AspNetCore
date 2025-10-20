using Microsoft.OpenApi.Models;

namespace SecureSpec.AspNetCore.Configuration;

/// <summary>
/// Collection of OpenAPI documents to generate.
/// </summary>
public class DocumentCollection
{
    private readonly Dictionary<string, OpenApiDocument> _documents = new();

    /// <summary>
    /// Adds a new OpenAPI document with the specified name.
    /// </summary>
    /// <param name="name">The name of the document (e.g., "v1", "v2").</param>
    /// <param name="configure">An action to configure the document.</param>
    public void Add(string name, Action<OpenApiDocument> configure)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(configure);

        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo(),
            Paths = new OpenApiPaths(),
            Components = new OpenApiComponents()
        };

        configure(document);
        _documents[name] = document;
    }

    /// <summary>
    /// Gets all registered documents.
    /// </summary>
    public IReadOnlyDictionary<string, OpenApiDocument> GetAll() => _documents;
}
