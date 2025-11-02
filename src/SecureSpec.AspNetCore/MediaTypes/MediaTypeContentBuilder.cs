using Microsoft.OpenApi.Models;

namespace SecureSpec.AspNetCore.MediaTypes;

/// <summary>
/// Builds OpenAPI content objects with shared schema references across media types.
/// Implements AC 453: Shared schema across media types uses single $ref (no duplication).
/// </summary>
public static class MediaTypeContentBuilder
{
    /// <summary>
    /// Creates a content dictionary with multiple media types sharing the same schema reference.
    /// </summary>
    /// <param name="schema">The schema to use across all media types.</param>
    /// <param name="mediaTypes">The media types to include.</param>
    /// <param name="options">Optional configuration for specific media type handling.</param>
    /// <returns>A dictionary of media type content objects.</returns>
    public static IDictionary<string, OpenApiMediaType> CreateContent(
        OpenApiSchema schema,
        IEnumerable<string> mediaTypes,
        MediaTypeContentOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(mediaTypes);

        options ??= new MediaTypeContentOptions();

        var content = new Dictionary<string, OpenApiMediaType>();

        // Sort media types for deterministic ordering (AC 452)
        var sortedMediaTypes = MediaTypeOrdering.Sort(mediaTypes);

        foreach (var mediaType in sortedMediaTypes)
        {
            var mediaTypeObj = CreateMediaTypeObject(schema, mediaType, options);
            content[mediaType] = mediaTypeObj;
        }

        return content;
    }

    /// <summary>
    /// Creates a media type object for a specific media type.
    /// Ensures schema reference is shared (not duplicated).
    /// </summary>
    /// <param name="schema">The schema to reference.</param>
    /// <param name="mediaType">The media type string.</param>
    /// <param name="options">Configuration options.</param>
    /// <returns>An OpenApiMediaType object.</returns>
    private static OpenApiMediaType CreateMediaTypeObject(
        OpenApiSchema schema,
        string mediaType,
        MediaTypeContentOptions options)
    {
        // CA1308: Using ToLowerInvariant is appropriate for case-insensitive media type comparison
#pragma warning disable CA1308
        var normalizedMediaType = mediaType.ToLowerInvariant();
#pragma warning restore CA1308

        // Handle special media types with specific requirements
        return normalizedMediaType switch
        {
            "text/plain" => CreateTextPlainMediaType(schema, options),
            "application/xml" or "text/xml" => CreateXmlMediaType(schema, options),
            _ => CreateDefaultMediaType(schema, options)
        };
    }

    /// <summary>
    /// Creates a text/plain media type with raw example or fallback (AC 457).
    /// </summary>
    private static OpenApiMediaType CreateTextPlainMediaType(OpenApiSchema schema, MediaTypeContentOptions options)
    {
        return PlainTextContentHandler.CreateMediaType(schema, options.PlainTextExample);
    }

    /// <summary>
    /// Creates an application/xml media type that mirrors JSON structure (AC 458).
    /// </summary>
    private static OpenApiMediaType CreateXmlMediaType(OpenApiSchema schema, MediaTypeContentOptions options)
    {
        return XmlContentGenerator.CreateMediaType(schema, options.XmlRootElementName);
    }

    /// <summary>
    /// Creates a default media type object with shared schema reference.
    /// </summary>
    private static OpenApiMediaType CreateDefaultMediaType(OpenApiSchema schema, MediaTypeContentOptions options)
    {
        var mediaType = new OpenApiMediaType
        {
            // Use the same schema reference - no duplication (AC 453)
            Schema = schema
        };

        // Add encoding for multipart/form-data if applicable
        if (options.MultipartEncodings != null && options.MultipartEncodings.Count > 0)
        {
            mediaType.Encoding = options.MultipartEncodings;
        }

        return mediaType;
    }

    /// <summary>
    /// Creates content with a schema reference (most common case).
    /// </summary>
    /// <param name="schemaReference">The schema reference.</param>
    /// <param name="mediaTypes">The media types to include.</param>
    /// <param name="options">Optional configuration.</param>
    /// <returns>A dictionary of media type content objects all sharing the same reference.</returns>
    public static IDictionary<string, OpenApiMediaType> CreateContentWithReference(
        OpenApiReference schemaReference,
        IEnumerable<string> mediaTypes,
        MediaTypeContentOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(schemaReference);

        // Create a schema with the reference
        var schema = new OpenApiSchema
        {
            Reference = schemaReference
        };

        return CreateContent(schema, mediaTypes, options);
    }
}

/// <summary>
/// Configuration options for media type content generation.
/// </summary>
public class MediaTypeContentOptions
{
    /// <summary>
    /// Gets or sets the raw example to use for text/plain content.
    /// </summary>
    public string? PlainTextExample { get; set; }

    /// <summary>
    /// Gets or sets the root element name for XML content.
    /// </summary>
    public string? XmlRootElementName { get; set; }

    /// <summary>
    /// Gets or sets encoding specifications for multipart/form-data.
    /// </summary>
    public IDictionary<string, OpenApiEncoding>? MultipartEncodings { get; set; }
}
