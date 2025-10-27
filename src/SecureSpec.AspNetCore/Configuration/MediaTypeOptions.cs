namespace SecureSpec.AspNetCore.Configuration;

/// <summary>
/// Configuration options for media type handling and content negotiation.
/// </summary>
public class MediaTypeOptions
{
    /// <summary>
    /// Gets or sets the maximum number of fields (names + files) allowed in multipart requests.
    /// Default is 200.
    /// </summary>
    public int MaxMultipartFieldCount { get; set; } = 200;

    /// <summary>
    /// Gets or sets the maximum binary upload size in bytes.
    /// Default is 10 MB (10,485,760 bytes).
    /// </summary>
    public long MaxBinaryUploadSize { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Gets or sets a value indicating whether to include application/xml in generated content.
    /// Default is true.
    /// </summary>
    public bool IncludeXml { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to include text/plain in generated content.
    /// Default is true.
    /// </summary>
    public bool IncludePlainText { get; set; } = true;

    /// <summary>
    /// Gets or sets the default root element name for XML content.
    /// Default is "root".
    /// </summary>
    public string XmlRootElementName { get; set; } = "root";

    /// <summary>
    /// Gets the collection of custom media type handlers.
    /// </summary>
    public MediaTypeHandlerCollection CustomHandlers { get; } = new();
}

/// <summary>
/// Collection of custom media type handlers.
/// </summary>
public class MediaTypeHandlerCollection
{
    private readonly Dictionary<string, Func<object, string>> _handlers = new();

    /// <summary>
    /// Registers a custom handler for a specific media type.
    /// </summary>
    /// <param name="mediaType">The media type (e.g., "application/custom").</param>
    /// <param name="handler">A function that converts an object to the media type representation.</param>
    public void Register(string mediaType, Func<object, string> handler)
    {
        ArgumentNullException.ThrowIfNull(mediaType);
        ArgumentNullException.ThrowIfNull(handler);

        // CA1308: Using ToLowerInvariant is appropriate for case-insensitive media type keys
#pragma warning disable CA1308
        _handlers[mediaType.ToLowerInvariant()] = handler;
#pragma warning restore CA1308
    }

    /// <summary>
    /// Tries to get a handler for the specified media type.
    /// </summary>
    /// <param name="mediaType">The media type.</param>
    /// <param name="handler">The handler if found.</param>
    /// <returns>True if a handler was found; otherwise, false.</returns>
    public bool TryGetHandler(string mediaType, out Func<object, string>? handler)
    {
        ArgumentNullException.ThrowIfNull(mediaType);
        // CA1308: Using ToLowerInvariant is appropriate for case-insensitive media type keys
#pragma warning disable CA1308
        return _handlers.TryGetValue(mediaType.ToLowerInvariant(), out handler);
#pragma warning restore CA1308
    }

    /// <summary>
    /// Gets the number of registered handlers.
    /// </summary>
    public int Count => _handlers.Count;
}
