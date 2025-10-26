using Microsoft.OpenApi.Models;
using System.Text;

namespace SecureSpec.AspNetCore.MediaTypes;

/// <summary>
/// Generates stable application/xml content that mirrors JSON structure.
/// Implements AC 458: application/xml generation stable and mirrors JSON structure where representable.
/// </summary>
public class XmlContentGenerator
{
    /// <summary>
    /// Creates an application/xml media type object with a schema that mirrors JSON structure.
    /// </summary>
    /// <param name="schema">The base schema to mirror in XML.</param>
    /// <param name="rootElementName">Optional root element name. Defaults to "root".</param>
    /// <returns>An OpenApiMediaType configured for application/xml content.</returns>
    public OpenApiMediaType CreateMediaType(OpenApiSchema schema, string? rootElementName = null)
    {
        ArgumentNullException.ThrowIfNull(schema);

        var xmlSchema = CreateXmlSchema(schema, rootElementName ?? "root");

        var mediaType = new OpenApiMediaType
        {
            Schema = xmlSchema
        };

        return mediaType;
    }

    /// <summary>
    /// Creates an XML-compatible schema that mirrors the JSON structure.
    /// </summary>
    /// <param name="schema">The base schema.</param>
    /// <param name="elementName">The XML element name.</param>
    /// <returns>A schema configured with XML metadata.</returns>
    private OpenApiSchema CreateXmlSchema(OpenApiSchema schema, string elementName)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(elementName);

        // Create a new schema that mirrors the original
        var xmlSchema = new OpenApiSchema
        {
            Type = schema.Type,
            Format = schema.Format,
            Nullable = schema.Nullable,
            Title = schema.Title,
            Description = schema.Description,
            Default = schema.Default,
            Minimum = schema.Minimum,
            Maximum = schema.Maximum,
            MinLength = schema.MinLength,
            MaxLength = schema.MaxLength,
            Pattern = schema.Pattern,
            Xml = new OpenApiXml
            {
                Name = elementName
            }
        };

        // Handle reference schemas
        if (schema.Reference != null)
        {
            xmlSchema.Reference = schema.Reference;
            return xmlSchema;
        }

        // Mirror properties for object schemas
        if (schema.Properties != null && schema.Properties.Count > 0)
        {
            xmlSchema.Properties = new Dictionary<string, OpenApiSchema>();
            foreach (var prop in schema.Properties)
            {
                var propSchema = CreateXmlSchema(prop.Value, prop.Key);
                xmlSchema.Properties[prop.Key] = propSchema;
            }

            // Preserve required properties
            if (schema.Required != null && schema.Required.Count > 0)
            {
                xmlSchema.Required = new HashSet<string>(schema.Required);
            }
        }

        // Mirror array items
        if (schema.Items != null)
        {
            var itemElementName = GetArrayItemElementName(elementName);
            xmlSchema.Items = CreateXmlSchema(schema.Items, itemElementName);

            // Set wrapped array if appropriate
            xmlSchema.Xml ??= new OpenApiXml();
            xmlSchema.Xml.Wrapped = true;
        }

        // Mirror enum values (stable ordering)
        if (schema.Enum != null && schema.Enum.Count > 0)
        {
            xmlSchema.Enum = new List<Microsoft.OpenApi.Any.IOpenApiAny>(schema.Enum);
        }

        // Mirror oneOf/anyOf/allOf (where representable)
        if (schema.OneOf != null && schema.OneOf.Count > 0)
        {
            xmlSchema.OneOf = schema.OneOf.Select(s => CreateXmlSchema(s, elementName)).ToList();
        }

        if (schema.AllOf != null && schema.AllOf.Count > 0)
        {
            xmlSchema.AllOf = schema.AllOf.Select(s => CreateXmlSchema(s, elementName)).ToList();
        }

        if (schema.AnyOf != null && schema.AnyOf.Count > 0)
        {
            xmlSchema.AnyOf = schema.AnyOf.Select(s => CreateXmlSchema(s, elementName)).ToList();
        }

        return xmlSchema;
    }

    /// <summary>
    /// Generates an appropriate element name for array items.
    /// </summary>
    /// <param name="arrayElementName">The name of the array element.</param>
    /// <returns>A singular form element name for items.</returns>
    private string GetArrayItemElementName(string arrayElementName)
    {
        // Simple singularization: remove trailing 's' if present
        // This is a basic implementation; could be enhanced with proper pluralization rules
        if (arrayElementName.EndsWith("ies", StringComparison.OrdinalIgnoreCase))
        {
            return arrayElementName[..^3] + "y";
        }

        if (arrayElementName.Length > 1 &&
            arrayElementName[^1] is 's' or 'S')
        {
            return arrayElementName[..^1];
        }

        return arrayElementName + "Item";
    }

    /// <summary>
    /// Generates a stable XML example from a schema.
    /// </summary>
    /// <param name="schema">The schema to generate an example for.</param>
    /// <param name="elementName">The root element name.</param>
    /// <returns>An XML string example, or null if generation is not applicable.</returns>
    public string? GenerateXmlExample(OpenApiSchema schema, string elementName = "root")
    {
        ArgumentNullException.ThrowIfNull(schema);

        if (schema.Type == null)
        {
            return null;
        }

        var builder = new StringBuilder();
        GenerateXmlElement(builder, schema, elementName, indent: 0);
        return builder.ToString();
    }

    private void GenerateXmlElement(StringBuilder builder, OpenApiSchema schema, string elementName, int indent)
    {
        var indentStr = new string(' ', indent * 2);

        if (schema.Type == "object" && schema.Properties != null)
        {
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{indentStr}<{elementName}>");

            // Sort properties for stable output
            foreach (var prop in schema.Properties.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                GenerateXmlElement(builder, prop.Value, prop.Key, indent + 1);
            }

            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{indentStr}</{elementName}>");
        }
        else if (schema.Type == "array" && schema.Items != null)
        {
            var itemName = GetArrayItemElementName(elementName);
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{indentStr}<{elementName}>");
            GenerateXmlElement(builder, schema.Items, itemName, indent + 1);
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{indentStr}</{elementName}>");
        }
        else
        {
            var value = GetExampleValue(schema);
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{indentStr}<{elementName}>{value}</{elementName}>");
        }
    }

    private string GetExampleValue(OpenApiSchema schema)
    {
        if (schema.Example != null)
        {
            return EscapeXml(schema.Example.ToString() ?? "");
        }

        return schema.Type switch
        {
            "string" => "string",
            "integer" => "0",
            "number" => "0.0",
            "boolean" => "true",
            _ => ""
        };
    }

    private static string EscapeXml(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&apos;", StringComparison.Ordinal);
    }
}
