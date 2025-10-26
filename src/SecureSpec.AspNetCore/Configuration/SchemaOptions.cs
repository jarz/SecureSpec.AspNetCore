using System.Collections;

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
    /// Gets or sets the threshold for enum virtualization.
    /// Enums with more values than this threshold will be virtualized.
    /// Default is 10,000.
    /// </summary>
    public int EnumVirtualizationThreshold { get; set; } = 10_000;

    /// <summary>
    /// Gets or sets the OpenAPI specification version used for schema generation.
    /// Default is OpenAPI 3.0.
    /// </summary>
    public SchemaSpecVersion SpecVersion { get; set; } = SchemaSpecVersion.OpenApi3_0;

    /// <summary>
    /// Gets or sets whether example generation is enabled.
    /// Default is true.
    /// </summary>
    public bool GenerateExamples { get; set; } = true;

    /// <summary>
    /// Gets or sets the time budget in milliseconds for example generation per schema.
    /// Default is 25ms as per PRD specification.
    /// </summary>
    public int ExampleGenerationTimeoutMs { get; set; } = 25;

    /// <summary>
    /// Gets the type mappings for custom types.
    /// </summary>
    public TypeMappingCollection TypeMappings { get; } = new();
}

/// <summary>
/// Collection of custom type mappings.
/// </summary>
public sealed class TypeMappingCollection : IEnumerable<KeyValuePair<Type, TypeMapping>>
{
    private readonly Dictionary<Type, TypeMapping> _mappings = new();

    /// <summary>
    /// Maps a CLR type to an OpenAPI type and format.
    /// </summary>
    public void Map<T>(Action<TypeMapping> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var mapping = new TypeMapping();
        configure(mapping);
        _mappings[typeof(T)] = mapping;
    }

    /// <summary>
    /// Gets the mapping for the specified type, if any.
    /// </summary>
    public bool TryGetMapping(Type type, out TypeMapping? mapping)
    {
        ArgumentNullException.ThrowIfNull(type);
        return _mappings.TryGetValue(type, out mapping);
    }

    /// <summary>
    /// Gets the number of registered mappings.
    /// </summary>
    public int Count => _mappings.Count;

    /// <summary>
    /// Returns an enumerator over registered mappings.
    /// </summary>
    public IEnumerator<KeyValuePair<Type, TypeMapping>> GetEnumerator() => _mappings.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
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

/// <summary>
/// Supported OpenAPI specification versions for schema generation.
/// </summary>
public enum SchemaSpecVersion
{
    /// <summary>
    /// OpenAPI 3.0.x (uses <c>nullable: true</c> for nullability).
    /// </summary>
    OpenApi3_0,

    /// <summary>
    /// OpenAPI 3.1.x (uses JSON Schema union semantics for nullability).
    /// </summary>
    OpenApi3_1
}
