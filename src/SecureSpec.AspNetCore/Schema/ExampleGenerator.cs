using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;

namespace SecureSpec.AspNetCore.Schema;

/// <summary>
/// Generates deterministic fallback examples for OpenAPI schemas.
/// </summary>
public sealed class ExampleGenerator
{
    private readonly SchemaOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExampleGenerator"/> class.
    /// </summary>
    public ExampleGenerator(SchemaOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Generates a deterministic fallback example for the specified schema.
    /// </summary>
    /// <param name="schema">The schema to generate an example for.</param>
    /// <returns>A generated example value, or null if generation is not possible.</returns>
    public IOpenApiAny? GenerateDeterministicFallback(OpenApiSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        return schema.Type switch
        {
            "string" => GenerateStringExample(schema),
            "integer" => GenerateIntegerExample(schema),
            "number" => GenerateNumberExample(schema),
            "boolean" => new OpenApiBoolean(false),
            "array" => GenerateArrayExample(schema),
            "object" => GenerateObjectExample(schema),
            _ => null
        };
    }

    private OpenApiString GenerateStringExample(OpenApiSchema schema)
    {
        // Handle specific formats
        if (!string.IsNullOrEmpty(schema.Format))
        {
            return schema.Format switch
            {
                "uuid" => new OpenApiString("00000000-0000-0000-0000-000000000000"),
                "date-time" => new OpenApiString("2024-01-01T00:00:00Z"),
                "date" => new OpenApiString("2024-01-01"),
                "time" => new OpenApiString("00:00:00"),
                "byte" => new OpenApiString(""),
                "binary" => new OpenApiString(""),
                "email" => new OpenApiString("user@example.com"),
                "uri" => new OpenApiString("https://example.com"),
                _ => new OpenApiString("string")
            };
        }

        // Handle enum values
        if (schema.Enum?.Count > 0)
        {
            var firstEnum = schema.Enum[0];
            if (firstEnum is OpenApiString enumString)
            {
                return enumString;
            }
        }

        // Default string value
        return new OpenApiString("string");
    }

    private OpenApiInteger GenerateIntegerExample(OpenApiSchema schema)
    {
        // Use minimum if specified
        if (schema.Minimum.HasValue)
        {
            return new OpenApiInteger((int)schema.Minimum.Value);
        }

        // Handle enum values
        if (schema.Enum?.Count > 0)
        {
            var firstEnum = schema.Enum[0];
            if (firstEnum is OpenApiInteger enumInt)
            {
                return enumInt;
            }
        }

        return new OpenApiInteger(0);
    }

    private OpenApiDouble GenerateNumberExample(OpenApiSchema schema)
    {
        // Use minimum if specified
        if (schema.Minimum.HasValue)
        {
            return new OpenApiDouble((double)schema.Minimum.Value);
        }

        return new OpenApiDouble(0.0);
    }

    private OpenApiArray GenerateArrayExample(OpenApiSchema schema)
    {
        var array = new OpenApiArray();

        // Generate one example item if schema is defined
        if (schema.Items != null)
        {
            var itemExample = GenerateDeterministicFallback(schema.Items);
            if (itemExample != null)
            {
                array.Add(itemExample);
            }
        }

        return array;
    }

    private OpenApiObject GenerateObjectExample(OpenApiSchema schema)
    {
        var obj = new OpenApiObject();

        // Generate examples for required properties first
        if (schema.Properties?.Count > 0)
        {
            foreach (var property in schema.Properties.OrderBy(p => p.Key))
            {
                var propertyExample = GenerateDeterministicFallback(property.Value);
                if (propertyExample != null)
                {
                    obj[property.Key] = propertyExample;
                }
            }
        }

        return obj;
    }
}
