using Microsoft.OpenApi.Models;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace SecureSpec.AspNetCore.Configuration;

/// <summary>
/// Collection of OpenAPI documents to generate.
/// </summary>
public sealed class DocumentCollection : IReadOnlyDictionary<string, OpenApiDocument>
{
    private readonly Dictionary<string, OpenApiDocument> _documents = new(StringComparer.Ordinal);

    /// <summary>
    /// Adds a new OpenAPI document with the specified name.
    /// </summary>
    /// <param name="name">The name of the document (e.g., "v1", "v2").</param>
    /// <param name="configure">An action to configure the document.</param>
    public void Add(string name, Action<OpenApiDocument> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
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
    /// Gets the registered documents as a read-only dictionary view.
    /// </summary>
    public IReadOnlyDictionary<string, OpenApiDocument> Items => _documents;

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, OpenApiDocument>> GetEnumerator() => _documents.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public int Count => _documents.Count;

    /// <inheritdoc />
    public bool ContainsKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _documents.ContainsKey(key);
    }

    /// <inheritdoc />
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out OpenApiDocument value)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _documents.TryGetValue(key, out value);
    }

    /// <inheritdoc />
    public OpenApiDocument this[string key]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(key);
            return _documents[key];
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> Keys => _documents.Keys;

    /// <inheritdoc />
    public IEnumerable<OpenApiDocument> Values => _documents.Values;
}
