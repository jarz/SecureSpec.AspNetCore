namespace SecureSpec.AspNetCore.Configuration;

/// <summary>
/// Configuration options for schema generation.
/// </summary>
public class SchemaOptions
{
    /// <summary>
    /// Gets or sets the strategy for generating schema IDs.
    /// Default is to use the type name with generic notation (e.g., "Outer«Inner»").
    /// </summary>
    public Func<Type, string>? IdStrategy { get; set; }

    /// <summary>
    /// Gets or sets the maximum depth for schema traversal to prevent infinite recursion.
    /// Default is 32.
    /// </summary>
    public int MaxDepth { get; set; } = 32;

    /// <summary>
    /// Gets or sets whether to use string representation for enums.
    /// Default is true.
    /// </summary>
    public bool UseEnumStrings { get; set; } = true;

    /// <summary>
    /// Gets or sets the naming policy for enum values.
    /// </summary>
    public Func<string, string>? EnumNamingPolicy { get; set; }

    /// <summary>
    /// Gets the type mappings for custom types.
    /// </summary>
    public TypeMappingCollection TypeMappings { get; } = new();
}

/// <summary>
/// Collection of custom type mappings.
/// </summary>
public class TypeMappingCollection
{
    private readonly Dictionary<Type, TypeMapping> _mappings = new();

    /// <summary>
    /// Maps a CLR type to an OpenAPI type and format.
    /// </summary>
    public void Map<T>(Action<TypeMapping> configure)
    {
        var mapping = new TypeMapping();
        configure(mapping);
        _mappings[typeof(T)] = mapping;
    }

    /// <summary>
    /// Gets the mapping for the specified type, if any.
    /// </summary>
    public bool TryGetMapping(Type type, out TypeMapping? mapping)
    {
        return _mappings.TryGetValue(type, out mapping);
    }
}

/// <summary>
/// Represents a custom type mapping.
/// </summary>
public class TypeMapping
{
    /// <summary>
    /// Gets or sets the OpenAPI type (e.g., "string", "number", "integer").
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the OpenAPI format (e.g., "date-time", "uuid", "byte").
    /// </summary>
    public string? Format { get; set; }
}
