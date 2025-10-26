using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace SecureSpec.AspNetCore.Schema;

public partial class SchemaGenerator
{
    /// <summary>
    /// Checks if a schema should be virtualized based on property and nested object thresholds.
    /// </summary>
    /// <param name="type">The type to analyze.</param>
    /// <param name="propertyCount">Output: Total number of properties.</param>
    /// <param name="nestedObjectCount">Output: Number of nested object properties.</param>
    /// <returns>True if virtualization should be applied, false otherwise.</returns>
    private bool ShouldVirtualizeSchema(Type type, out int propertyCount, out int nestedObjectCount)
    {
        propertyCount = 0;
        nestedObjectCount = 0;

        if (type.IsPrimitive || type == typeof(string) || type.IsEnum)
        {
            return false;
        }

        // Count all public properties
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        propertyCount = properties.Length;

        // Count nested object properties
        foreach (var property in properties)
        {
            if (IsComplexType(property.PropertyType))
            {
                nestedObjectCount++;
            }
        }

        // AC 301: >200 properties triggers virtualization
        // AC 302: >50 nested object properties triggers virtualization
        return propertyCount > _options.SchemaPropertyVirtualizationThreshold ||
               nestedObjectCount > _options.NestedObjectVirtualizationThreshold;
    }

    /// <summary>
    /// Determines if a type is a complex object type (not a primitive, string, or collection).
    /// </summary>
    private bool IsComplexType(Type type)
    {
        // Unwrap nullable
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            type = underlyingType;
        }

        // Not complex if it's a primitive, string, or enum
        if (type.IsPrimitive || type == typeof(string) || type.IsEnum ||
            type == typeof(Guid) || type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
            type == typeof(DateOnly) || type == typeof(TimeOnly) || type == typeof(decimal) ||
            type == typeof(byte[]))
        {
            return false;
        }

        // Not complex if it's a collection
        if (type.IsArray || IsGenericCollection(type))
        {
            return false;
        }

        // Everything else is considered complex
        return true;
    }

    /// <summary>
    /// Checks if a type is a generic collection type.
    /// </summary>
    private static bool IsGenericCollection(Type type)
    {
        if (!type.IsGenericType)
        {
            return false;
        }

        var genericDef = type.GetGenericTypeDefinition();
        return genericDef == typeof(List<>) ||
               genericDef == typeof(IList<>) ||
               genericDef == typeof(ICollection<>) ||
               genericDef == typeof(IEnumerable<>) ||
               genericDef == typeof(IReadOnlyList<>) ||
               genericDef == typeof(IReadOnlyCollection<>) ||
               genericDef == typeof(Dictionary<,>) ||
               genericDef == typeof(IDictionary<,>) ||
               genericDef == typeof(IReadOnlyDictionary<,>);
    }

    /// <summary>
    /// Applies virtualization metadata to a schema.
    /// </summary>
    /// <param name="schema">The schema to modify.</param>
    /// <param name="type">The type being virtualized.</param>
    /// <param name="totalPropertyCount">Total number of properties.</param>
    /// <param name="nestedObjectCount">Number of nested object properties.</param>
    private void ApplySchemaVirtualization(OpenApiSchema schema, Type type, int totalPropertyCount, int nestedObjectCount)
    {
        // Add virtualization metadata (AC 303)
        schema.Extensions["x-schema-virtualized"] = new OpenApiBoolean(true);
        schema.Extensions["x-property-total-count"] = new OpenApiInteger(totalPropertyCount);
        schema.Extensions["x-nested-object-count"] = new OpenApiInteger(nestedObjectCount);

        // Determine which threshold was exceeded
        var propertyThresholdExceeded = totalPropertyCount > _options.SchemaPropertyVirtualizationThreshold;
        var nestedThresholdExceeded = nestedObjectCount > _options.NestedObjectVirtualizationThreshold;

        // Add threshold information
        if (propertyThresholdExceeded)
        {
            schema.Extensions["x-property-threshold-exceeded"] = new OpenApiBoolean(true);
            schema.Extensions["x-property-threshold"] = new OpenApiInteger(_options.SchemaPropertyVirtualizationThreshold);
        }

        if (nestedThresholdExceeded)
        {
            schema.Extensions["x-nested-threshold-exceeded"] = new OpenApiBoolean(true);
            schema.Extensions["x-nested-threshold"] = new OpenApiInteger(_options.NestedObjectVirtualizationThreshold);
        }

        // Add description indicating virtualization
        string reason;
        if (propertyThresholdExceeded && nestedThresholdExceeded)
        {
            reason = $"exceeding both property count threshold ({_options.SchemaPropertyVirtualizationThreshold}) and nested object threshold ({_options.NestedObjectVirtualizationThreshold})";
        }
        else if (propertyThresholdExceeded)
        {
            reason = $"exceeding property count threshold ({_options.SchemaPropertyVirtualizationThreshold})";
        }
        else
        {
            reason = $"exceeding nested object threshold ({_options.NestedObjectVirtualizationThreshold})";
        }

        schema.Description = string.IsNullOrEmpty(schema.Description)
            ? $"Schema virtualized due to {reason}. Total properties: {totalPropertyCount}, Nested objects: {nestedObjectCount}."
            : $"{schema.Description}\n\nSchema virtualized due to {reason}. Total properties: {totalPropertyCount}, Nested objects: {nestedObjectCount}.";

        // Emit VIRT001 diagnostic (AC 303)
        _logger.LogInfo(
            "VIRT001",
            $"Schema for type '{type.FullName}' virtualized: {totalPropertyCount} properties ({nestedObjectCount} nested objects) " +
            $"exceeds threshold (property: {_options.SchemaPropertyVirtualizationThreshold}, nested: {_options.NestedObjectVirtualizationThreshold}).",
            new
            {
                TypeName = type.FullName,
                TotalProperties = totalPropertyCount,
                NestedObjects = nestedObjectCount,
                PropertyThreshold = _options.SchemaPropertyVirtualizationThreshold,
                NestedThreshold = _options.NestedObjectVirtualizationThreshold,
                PropertyThresholdExceeded = propertyThresholdExceeded,
                NestedThresholdExceeded = nestedThresholdExceeded
            });
    }
}
