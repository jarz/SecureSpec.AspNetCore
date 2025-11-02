using Microsoft.OpenApi.Models;
using System.Linq;

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

        if (TryGetArrayElementType(type, out var arrayElement))
        {
            return arrayElement;
        }

        var genericElement = GetGenericEnumerableElementType(type);
        return genericElement ?? GetInterfaceEnumerableElementType(type);
    }

    private static bool TryGetArrayElementType(Type type, out Type? elementType)
    {
        if (!type.IsArray)
        {
            elementType = null;
            return false;
        }

        elementType = type.GetElementType();
        return true;
    }

    private static Type? GetGenericEnumerableElementType(Type type)
    {
        return !type.IsGenericType
            ? null
            : TryGetEnumerableElementFromDefinition(type.GetGenericTypeDefinition(), type.GetGenericArguments());
    }

    private static Type? GetInterfaceEnumerableElementType(Type type)
    {
        return type
            .GetInterfaces()
            .Select(GetGenericEnumerableElementType)
            .FirstOrDefault(elementType => elementType != null);
    }

    private static Type? TryGetEnumerableElementFromDefinition(Type definition, Type[] arguments)
    {
        return IsSupportedEnumerableDefinition(definition) && !IsKeyValuePair(arguments[0])
            ? arguments[0]
            : null;
    }

    private static bool IsSupportedEnumerableDefinition(Type definition)
    {
        return definition == typeof(IEnumerable<>) ||
               definition == typeof(ICollection<>) ||
               definition == typeof(IList<>) ||
               definition == typeof(List<>) ||
               definition == typeof(IReadOnlyCollection<>) ||
               definition == typeof(IReadOnlyList<>);
    }

    private static bool TryGetDictionaryValueType(Type type, out Type valueType)
    {
        var directMatch = TryResolveDictionaryValueType(type);
        var interfaceMatch = directMatch ?? type
            .GetInterfaces()
            .Select(TryResolveDictionaryValueType)
            .FirstOrDefault(candidate => candidate != null);

        if (interfaceMatch is null)
        {
            valueType = null!;
            return false;
        }

        valueType = interfaceMatch;
        return true;
    }

    private static Type? TryResolveDictionaryValueType(Type candidate)
    {
        if (!candidate.IsGenericType)
        {
            return null;
        }

        var definition = candidate.GetGenericTypeDefinition();
        if (!IsSupportedDictionaryDefinition(definition))
        {
            return null;
        }

        var arguments = candidate.GetGenericArguments();
        return arguments[0] == typeof(string) ? arguments[1] : null;
    }

    private static bool IsSupportedDictionaryDefinition(Type definition)
    {
        return definition == typeof(Dictionary<,>) ||
               definition == typeof(IDictionary<,>) ||
               definition == typeof(IReadOnlyDictionary<,>);
    }

    private static bool IsKeyValuePair(Type candidate)
    {
        return candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
    }
}
