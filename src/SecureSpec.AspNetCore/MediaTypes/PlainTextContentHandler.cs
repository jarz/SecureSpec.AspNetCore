using Microsoft.OpenApi.Models;

namespace SecureSpec.AspNetCore.MediaTypes;

/// <summary>
/// Handles text/plain content generation for OpenAPI requests and responses.
/// Implements AC 457: text/plain request uses raw example or generated fallback correctly.
/// </summary>
public class PlainTextContentHandler
{
    /// <summary>
    /// Creates a text/plain media type object with appropriate example or fallback.
    /// </summary>
    /// <param name="schema">The schema for the content.</param>
    /// <param name="rawExample">Optional raw example string to use directly.</param>
    /// <returns>An OpenApiMediaType configured for text/plain content.</returns>
    public OpenApiMediaType CreateMediaType(OpenApiSchema schema, string? rawExample = null)
    {
        ArgumentNullException.ThrowIfNull(schema);

        var mediaType = new OpenApiMediaType
        {
            Schema = schema
        };

        // Use raw example if provided
        if (!string.IsNullOrEmpty(rawExample))
        {
            mediaType.Example = new Microsoft.OpenApi.Any.OpenApiString(rawExample);
        }
        else
        {
            // Generate fallback based on schema
            var fallback = GenerateFallbackExample(schema);
            if (fallback != null)
            {
                mediaType.Example = new Microsoft.OpenApi.Any.OpenApiString(fallback);
            }
        }

        return mediaType;
    }

    /// <summary>
    /// Generates a fallback example for text/plain based on the schema.
    /// </summary>
    /// <param name="schema">The schema to generate an example for.</param>
    /// <returns>A generated example string, or null if no suitable example can be generated.</returns>
    private string? GenerateFallbackExample(OpenApiSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        // Check if schema has explicit example
        if (schema.Example != null)
        {
            return schema.Example.ToString();
        }

        // Generate based on schema type
        return schema.Type switch
        {
            "string" => GenerateStringExample(schema),
            "integer" => GenerateIntegerExample(schema),
            "number" => GenerateNumberExample(schema),
            "boolean" => "true",
            "array" => GenerateArrayExample(schema),
            "object" => GenerateObjectExample(schema),
            _ => null
        };
    }

    private string? GenerateStringExample(OpenApiSchema schema)
    {
        // Use default if provided
        if (schema.Default != null)
        {
            return schema.Default.ToString();
        }

        // Use enum values if available
        if (schema.Enum != null && schema.Enum.Count > 0)
        {
            return schema.Enum[0].ToString();
        }

        // Use format-specific examples
        return schema.Format switch
        {
            "date" => "2025-01-01",
            "date-time" => "2025-01-01T00:00:00Z",
            "time" => "12:00:00",
            "uuid" => "00000000-0000-0000-0000-000000000000",
            "email" => "example@example.com",
            "uri" => "https://example.com",
            "byte" => "SGVsbG8gV29ybGQ=",
            "binary" => "[binary data]",
            _ => "example string"
        };
    }

    private string GenerateIntegerExample(OpenApiSchema schema)
    {
        if (schema.Default != null)
        {
            return schema.Default.ToString() ?? "0";
        }

        if (schema.Minimum.HasValue)
        {
            return schema.Minimum.Value.ToString("F0", System.Globalization.CultureInfo.InvariantCulture);
        }

        return "0";
    }

    private string GenerateNumberExample(OpenApiSchema schema)
    {
        if (schema.Default != null)
        {
            return schema.Default.ToString() ?? "0.0";
        }

        if (schema.Minimum.HasValue)
        {
            return schema.Minimum.Value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        }

        return "0.0";
    }

    private string GenerateArrayExample(OpenApiSchema schema)
    {
        if (schema.Items == null)
        {
            return "[]";
        }

        var itemExample = GenerateFallbackExample(schema.Items);
        return itemExample != null ? $"[{itemExample}]" : "[]";
    }

    private string GenerateObjectExample(OpenApiSchema schema)
    {
        if (schema.Properties == null || schema.Properties.Count == 0)
        {
            return "{}";
        }

        // Generate simple key-value representation
        var parts = new List<string>();
        foreach (var prop in schema.Properties.Take(3)) // Limit to first 3 properties
        {
            var value = GenerateFallbackExample(prop.Value) ?? "null";
            parts.Add($"{prop.Key}: {value}");
        }

        return "{ " + string.Join(", ", parts) + " }";
    }
}
