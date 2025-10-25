using Microsoft.OpenApi.Models;

namespace SecureSpec.AspNetCore.Schema;

public partial class SchemaGenerator
{
    /// <summary>
    /// Creates the structural schema for array-like types. Container nullability is applied by the caller via <see cref="ApplyNullability"/>.
    /// </summary>
    private bool TryCreateArraySchema(Type type, SchemaGenerationContext context, int depth, out OpenApiSchema schema)
    {
        if (type == typeof(byte[]))
        {
            schema = null!;
            return false;
        }

        var elementType = GetEnumerableElementType(type);
        if (elementType != null)
        {
            schema = new OpenApiSchema
            {
                Type = "array",
                Items = GenerateSchemaRecursive(elementType, isNullable: false, context, depth + 1)
            };

            return true;
        }

        schema = null!;
        return false;
    }

    /// <summary>
    /// Creates the structural schema for dictionary-like types keyed by string. Container nullability is applied by the caller via <see cref="ApplyNullability"/>.
    /// </summary>
    private bool TryCreateDictionarySchema(Type type, SchemaGenerationContext context, int depth, out OpenApiSchema schema)
    {
        if (TryGetDictionaryValueType(type, out var valueType))
        {
            schema = new OpenApiSchema
            {
                Type = "object",
                AdditionalProperties = GenerateSchemaRecursive(valueType, isNullable: false, context, depth + 1)
            };

            return true;
        }

        schema = null!;
        return false;
    }

    private static Type? GetEnumerableElementType(Type type)
    {
        if (type == typeof(string))
        {
            return null;
        }

        if (type.IsArray)
        {
            return type.GetElementType();
        }

        if (type.IsGenericType)
        {
            var genericDefinition = type.GetGenericTypeDefinition();
            if (genericDefinition == typeof(IEnumerable<>) ||
                genericDefinition == typeof(ICollection<>) ||
                genericDefinition == typeof(IList<>) ||
                genericDefinition == typeof(List<>) ||
                genericDefinition == typeof(IReadOnlyCollection<>) ||
                genericDefinition == typeof(IReadOnlyList<>))
            {
                var candidate = type.GetGenericArguments()[0];
                return IsKeyValuePair(candidate) ? null : candidate;
            }
        }

        foreach (var iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var candidate = iface.GetGenericArguments()[0];
                if (!IsKeyValuePair(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static bool TryGetDictionaryValueType(Type type, out Type valueType)
    {
        if (type.IsGenericType && TryGetDictionaryValueTypeCore(type, out valueType))
        {
            return true;
        }

        foreach (var iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && TryGetDictionaryValueTypeCore(iface, out valueType))
            {
                return true;
            }
        }

        valueType = null!;
        return false;
    }

    private static bool TryGetDictionaryValueTypeCore(Type candidate, out Type valueType)
    {
        var definition = candidate.GetGenericTypeDefinition();
        if (definition == typeof(Dictionary<,>) ||
            definition == typeof(IDictionary<,>) ||
            definition == typeof(IReadOnlyDictionary<,>))
        {
            var arguments = candidate.GetGenericArguments();
            if (arguments[0] == typeof(string))
            {
                valueType = arguments[1];
                return true;
            }
        }

        valueType = null!;
        return false;
    }

    private static bool IsKeyValuePair(Type candidate)
    {
        return candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
    }
}
