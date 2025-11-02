using Microsoft.OpenApi.Models;
using System.Text;

namespace SecureSpec.AspNetCore.MediaTypes;

/// <summary>
/// Generates stable application/xml content that mirrors JSON structure.
/// Implements AC 458: application/xml generation stable and mirrors JSON structure where representable.
/// </summary>
public static class XmlContentGenerator
{
    /// <summary>
    /// Creates an application/xml media type object with a schema that mirrors JSON structure.
    /// </summary>
    /// <param name="schema">The base schema to mirror in XML.</param>
    /// <param name="rootElementName">Optional root element name. Defaults to "root".</param>
    /// <returns>An OpenApiMediaType configured for application/xml content.</returns>
    public static OpenApiMediaType CreateMediaType(OpenApiSchema schema, string? rootElementName = null)
    {
        ArgumentNullException.ThrowIfNull(schema);

        var xmlSchema = CreateXmlSchema(schema, rootElementName ?? "root");

        return new OpenApiMediaType
        {
            Schema = xmlSchema
        };
    }

    /// <summary>
    /// Creates an XML-compatible schema that mirrors the JSON structure.
    /// </summary>
    /// <param name="schema">The base schema.</param>
    /// <param name="elementName">The XML element name.</param>
    /// <returns>A schema configured with XML metadata.</returns>
    private static OpenApiSchema CreateXmlSchema(OpenApiSchema schema, string elementName)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(elementName);

        var xmlSchema = CloneSchemaForXml(schema, elementName);

        if (schema.Reference != null)
        {
            xmlSchema.Reference = schema.Reference;
            return xmlSchema;
        }

        ApplyXmlStructure(schema, xmlSchema, elementName);
        return xmlSchema;
    }

    private static void ApplyXmlStructure(OpenApiSchema source, OpenApiSchema target, string elementName)
    {
        CopyObjectProperties(source, target);
        CopyArraySchema(source, target, elementName);
        CopyEnumValues(source, target);
        CopyComposedSchemas(source, target, elementName);
    }

    private static OpenApiSchema CloneSchemaForXml(OpenApiSchema schema, string elementName)
    {
        return new OpenApiSchema
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
    }

    private static void CopyObjectProperties(OpenApiSchema source, OpenApiSchema target)
    {
        if (source.Properties == null || source.Properties.Count == 0)
        {
            return;
        }

        target.Properties = new Dictionary<string, OpenApiSchema>();
        foreach (var property in source.Properties)
        {
            target.Properties[property.Key] = CreateXmlSchema(property.Value, property.Key);
        }

        if (source.Required != null && source.Required.Count > 0)
        {
            target.Required = new HashSet<string>(source.Required);
        }
    }

    private static void CopyArraySchema(OpenApiSchema source, OpenApiSchema target, string elementName)
    {
        if (source.Items == null)
        {
            return;
        }

        var itemElementName = GetArrayItemElementName(elementName);
        target.Items = CreateXmlSchema(source.Items, itemElementName);
        target.Xml ??= new OpenApiXml();
        target.Xml.Wrapped = true;
    }

    private static void CopyEnumValues(OpenApiSchema source, OpenApiSchema target)
    {
        if (source.Enum == null || source.Enum.Count == 0)
        {
            return;
        }

        target.Enum = new List<Microsoft.OpenApi.Any.IOpenApiAny>(source.Enum);
    }

    private static void CopyComposedSchemas(OpenApiSchema source, OpenApiSchema target, string elementName)
    {
        AssignComposition(CloneSchemaList(source.OneOf, elementName), composition => target.OneOf = composition);
        AssignComposition(CloneSchemaList(source.AllOf, elementName), composition => target.AllOf = composition);
        AssignComposition(CloneSchemaList(source.AnyOf, elementName), composition => target.AnyOf = composition);
    }

    private static void AssignComposition(List<OpenApiSchema>? composition, Action<List<OpenApiSchema>> assign)
    {
        if (composition != null)
        {
            assign(composition);
        }
    }

    private static List<OpenApiSchema>? CloneSchemaList(IList<OpenApiSchema>? source, string elementName)
    {
        if (source == null || source.Count == 0)
        {
            return null;
        }

        return source.Select(s => CreateXmlSchema(s, elementName)).ToList();
    }

    /// <summary>
    /// Generates an appropriate element name for array items.
    /// </summary>
    /// <param name="arrayElementName">The name of the array element.</param>
    /// <returns>A singular form element name for items.</returns>
    private static string GetArrayItemElementName(string arrayElementName)
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
    public static string? GenerateXmlExample(OpenApiSchema schema, string elementName = "root")
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

    private static void GenerateXmlElement(StringBuilder builder, OpenApiSchema schema, string elementName, int indent)
    {
        var indentStr = new string(' ', indent * 2);
        switch (GetNodeKind(schema))
        {
            case XmlNodeKind.Object:
                WriteObjectElement(builder, schema, elementName, indent, indentStr);
                break;
            case XmlNodeKind.Array:
                WriteArrayElement(builder, schema, elementName, indent, indentStr);
                break;
            default:
                WriteScalarElement(builder, schema, elementName, indentStr);
                break;
        }
    }

    private static XmlNodeKind GetNodeKind(OpenApiSchema schema)
    {
        if (schema.Type == "object" && schema.Properties != null)
        {
            return XmlNodeKind.Object;
        }

        if (schema.Type == "array" && schema.Items != null)
        {
            return XmlNodeKind.Array;
        }

        return XmlNodeKind.Scalar;
    }

    private static void WriteObjectElement(StringBuilder builder, OpenApiSchema schema, string elementName, int indent, string indentStr)
    {
        builder.Append(indentStr).Append('<').Append(elementName).AppendLine(">");

        foreach (var prop in schema.Properties!.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            GenerateXmlElement(builder, prop.Value, prop.Key, indent + 1);
        }

        builder.Append(indentStr).Append("</").Append(elementName).AppendLine(">");
    }

    private static void WriteArrayElement(StringBuilder builder, OpenApiSchema schema, string elementName, int indent, string indentStr)
    {
        var itemName = GetArrayItemElementName(elementName);
        builder.Append(indentStr).Append('<').Append(elementName).AppendLine(">");
        GenerateXmlElement(builder, schema.Items!, itemName, indent + 1);
        builder.Append(indentStr).Append("</").Append(elementName).AppendLine(">");
    }

    private static void WriteScalarElement(StringBuilder builder, OpenApiSchema schema, string elementName, string indentStr)
    {
        var value = GetExampleValue(schema);
        builder.Append(indentStr).Append('<').Append(elementName).Append('>')
               .Append(value).Append("</").Append(elementName).AppendLine(">");
    }

    private static string GetExampleValue(OpenApiSchema schema)
    {
        if (schema.Example != null)
        {
            var value = (schema.Example as Microsoft.OpenApi.Any.OpenApiString)?.Value ?? schema.Example.ToString() ?? "";
            return EscapeXml(value);
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

    private enum XmlNodeKind
    {
        Scalar,
        Object,
        Array
    }
}
