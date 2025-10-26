using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace SecureSpec.AspNetCore.Schema;

public partial class SchemaGenerator
{
    /// <summary>
    /// Analyzes a type to determine if it requires virtualization based on property count thresholds.
    /// </summary>
    /// <param name="type">The type to analyze.</param>
    /// <returns>Virtualization analysis result.</returns>
    private VirtualizationAnalysis AnalyzeSchemaForVirtualization(Type type)
    {
        // Only analyze classes and structs that can have properties
        if (type.IsInterface || type.IsArray || type.IsPointer)
        {
            return new VirtualizationAnalysis { RequiresVirtualization = false };
        }

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var totalPropertyCount = properties.Length;

        if (totalPropertyCount == 0)
        {
            return new VirtualizationAnalysis { RequiresVirtualization = false };
        }

        // Count nested object properties
        var nestedObjectCount = 0;
        foreach (var prop in properties)
        {
            var propType = prop.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(propType) ?? propType;

            // Check if this is a complex/object type (not primitive, enum, or collection)
            if (IsComplexObjectType(underlyingType))
            {
                nestedObjectCount++;
            }
        }

        // AC 301-303: Check virtualization thresholds
        var requiresVirtualization =
            totalPropertyCount > _options.SchemaPropertyVirtualizationThreshold ||
            nestedObjectCount > _options.NestedObjectVirtualizationThreshold;

        return new VirtualizationAnalysis
        {
            RequiresVirtualization = requiresVirtualization,
            TotalPropertyCount = totalPropertyCount,
            NestedObjectCount = nestedObjectCount,
            Properties = properties
        };
    }

    /// <summary>
    /// Determines if a type is a complex object type (not primitive, enum, string, or collection).
    /// </summary>
    private static bool IsComplexObjectType(Type type)
    {
        // Not a complex object if it's a primitive type
        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal))
        {
            return false;
        }

        // Not a complex object if it's a well-known simple type
        if (type == typeof(Guid) || type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
            type == typeof(DateOnly) || type == typeof(TimeOnly) || type == typeof(byte[]))
        {
            return false;
        }

        // Not a complex object if it's a collection (but check this after primitives for efficiency)
        if (type != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
        {
            return false;
        }

        // It's a complex object type
        return true;
    }

    /// <summary>
    /// Applies virtualization metadata to a schema that exceeds virtualization thresholds.
    /// </summary>
    /// <param name="schema">The schema to apply virtualization to.</param>
    /// <param name="type">The type being virtualized.</param>
    /// <param name="analysis">The virtualization analysis results.</param>
    private void ApplySchemaVirtualization(OpenApiSchema schema, Type type, VirtualizationAnalysis analysis)
    {
        // Add virtualization metadata extensions
        schema.Extensions["x-schema-virtualized"] = new OpenApiBoolean(true);
        schema.Extensions["x-schema-total-properties"] = new OpenApiInteger(analysis.TotalPropertyCount);
        schema.Extensions["x-schema-nested-objects"] = new OpenApiInteger(analysis.NestedObjectCount);

        // Add placeholder token as description hint
        var existingDescription = schema.Description ?? string.Empty;
        const string placeholder = "<virtualized…>";

        if (!string.IsNullOrEmpty(existingDescription))
        {
            schema.Description = $"{existingDescription} {placeholder}";
        }
        else
        {
            schema.Description = $"Large schema with {analysis.TotalPropertyCount} properties. {placeholder}";
        }

        // Emit VIRT001 diagnostic
        var reason = analysis.TotalPropertyCount > _options.SchemaPropertyVirtualizationThreshold
            ? $"total property count ({analysis.TotalPropertyCount}) exceeds threshold ({_options.SchemaPropertyVirtualizationThreshold})"
            : $"nested object count ({analysis.NestedObjectCount}) exceeds threshold ({_options.NestedObjectVirtualizationThreshold})";

        _logger.LogInfo(
            "VIRT001",
            $"Schema '{type.FullName}' requires virtualization: {reason}. Lazy loading enabled.",
            new
            {
                SchemaType = type.FullName,
                TotalProperties = analysis.TotalPropertyCount,
                NestedObjects = analysis.NestedObjectCount,
                PropertyThreshold = _options.SchemaPropertyVirtualizationThreshold,
                NestedThreshold = _options.NestedObjectVirtualizationThreshold
            });
    }

    /// <summary>
    /// Represents the results of analyzing a schema for virtualization requirements.
    /// </summary>
    private sealed class VirtualizationAnalysis
    {
        /// <summary>
        /// Gets or sets whether the schema requires virtualization.
        /// </summary>
        public required bool RequiresVirtualization { get; init; }

        /// <summary>
        /// Gets or sets the total number of properties in the schema.
        /// </summary>
        public int TotalPropertyCount { get; init; }

        /// <summary>
        /// Gets or sets the number of nested object properties.
        /// </summary>
        public int NestedObjectCount { get; init; }

        /// <summary>
        /// Gets or sets the property information array.
        /// </summary>
        public PropertyInfo[]? Properties { get; init; }
    }
}
