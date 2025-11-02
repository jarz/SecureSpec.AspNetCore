using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Diagnostics;
using System.Reflection;

namespace SecureSpec.AspNetCore.Schema;

/// <summary>
/// Generates OpenAPI schemas from CLR types.
/// </summary>
public partial class SchemaGenerator
{
    private const string PlaceholderExtensionKey = "x-securespec-placeholder";

    private readonly SchemaOptions _options;
    private readonly DiagnosticsLogger _logger;
    private readonly Dictionary<string, List<Type>> _schemaIdMap = [];
    private readonly Dictionary<Type, string> _typeToSchemaId = [];
    private readonly ExampleGenerator _exampleGenerator;
    private readonly ExamplePrecedenceEngine _precedenceEngine;
    private readonly XmlDocumentationProvider _xmlDocumentation;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaGenerator"/> class.
    /// </summary>
    public SchemaGenerator(SchemaOptions options, DiagnosticsLogger logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _exampleGenerator = new ExampleGenerator(options);
        _precedenceEngine = new ExamplePrecedenceEngine(_exampleGenerator);
        _xmlDocumentation = new XmlDocumentationProvider(logger);

        // Load XML documentation files
        foreach (var xmlPath in _options.XmlDocumentationPaths)
        {
            _xmlDocumentation.LoadXmlDocumentation(xmlPath);
        }
    }

    /// <summary>
    /// Gets the XML documentation provider.
    /// </summary>
    internal XmlDocumentationProvider XmlDocumentation => _xmlDocumentation;

    /// <summary>
    /// Generates an OpenAPI schema for the specified type.
    /// </summary>
    /// <param name="type">The CLR type to generate a schema for.</param>
    /// <returns>The generated OpenAPI schema.</returns>
    public OpenApiSchema GenerateSchema(Type type) => GenerateSchema(type, isNullable: false);

    /// <summary>
    /// Generates an OpenAPI schema for the specified type with explicit nullability control.
    /// </summary>
    /// <param name="type">The CLR type to generate a schema for.</param>
    /// <param name="isNullable">Whether the schema should allow null values.</param>
    /// <returns>The generated OpenAPI schema.</returns>
    public OpenApiSchema GenerateSchema(Type type, bool isNullable)
    {
        ArgumentNullException.ThrowIfNull(type);

        var context = new SchemaGenerationContext(_options.MaxDepth, _logger);
        return GenerateSchemaRecursive(type, isNullable, context, depth: 0);
    }

    private OpenApiSchema GenerateSchemaRecursive(Type type, bool isNullable, SchemaGenerationContext context, int depth)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (!context.TryEnter(type, depth, out var placeholder))
        {
            return ApplyNullability(placeholder, isNullable);
        }

        OpenApiSchema schema;

        try
        {
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
            {
                return GenerateSchemaRecursive(underlyingType, isNullable: true, context, depth + 1);
            }

            if (TryCreateDictionarySchema(type, context, depth, out var dictionarySchema))
            {
                schema = dictionarySchema;
            }
            else if (TryCreateArraySchema(type, context, depth, out var arraySchema))
            {
                schema = arraySchema;
            }
            else if (_options.TypeMappings.TryGetMapping(type, out var customMapping) && customMapping != null)
            {
                schema = new OpenApiSchema
                {
                    Type = customMapping.Type,
                    Format = customMapping.Format
                };
            }
            else
            {
                schema = CreateSchemaForPrimitiveOrObject(type);
            }

            // Apply XML documentation if available
            ApplyXmlDocumentation(schema, type);
        }
        finally
        {
            context.Exit(type);
        }

        return ApplyNullability(schema, isNullable);
    }

    private void ApplyXmlDocumentation(OpenApiSchema schema, Type type)
    {
        var documentation = _xmlDocumentation.GetTypeDocumentation(type);
        if (!string.IsNullOrEmpty(documentation?.Summary))
        {
            schema.Description = documentation.Summary;
        }

        // If the schema is for an object type, also try to apply documentation to properties
        if (schema.Type == "object" && schema.Properties != null)
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var propertyDoc = _xmlDocumentation.GetPropertyDocumentation(property);
                if (propertyDoc != null && !string.IsNullOrEmpty(propertyDoc.Summary))
                {
                    var propertyName = GetPropertyName(property);
                    if (schema.Properties.TryGetValue(propertyName, out var propertySchema))
                    {
                        propertySchema.Description = propertyDoc.Summary;
                    }
                }
            }
        }
    }

    private static string GetPropertyName(PropertyInfo property)
    {
        // Use camelCase by default (standard JSON serialization convention)
        var name = property.Name;
        if (name.Length > 0)
        {
            return char.ToLowerInvariant(name[0]) + name[1..];
        }
        return name;
    }

    private OpenApiSchema CreateSchemaForPrimitiveOrObject(Type type)
    {
        return type switch
        {
            Type t when t == typeof(Guid) => new OpenApiSchema { Type = "string", Format = "uuid" },
            Type t when t == typeof(DateTime) || t == typeof(DateTimeOffset) => new OpenApiSchema { Type = "string", Format = "date-time" },
            Type t when t == typeof(DateOnly) => new OpenApiSchema { Type = "string", Format = "date" },
            Type t when t == typeof(TimeOnly) => new OpenApiSchema { Type = "string", Format = "time" },
            Type t when t == typeof(byte[]) => new OpenApiSchema { Type = "string", Format = "byte" },
            Type t when t == typeof(IFormFile) => new OpenApiSchema { Type = "string", Format = "binary" },
            Type t when t == typeof(decimal) => new OpenApiSchema { Type = "number" },
            Type t when t == typeof(char) => new OpenApiSchema { Type = "string", MinLength = 1, MaxLength = 1 },
            Type t when t == typeof(int) || t == typeof(long) ||
                        t == typeof(short) || t == typeof(byte) ||
                        t == typeof(sbyte) || t == typeof(uint) ||
                        t == typeof(ulong) || t == typeof(ushort) => new OpenApiSchema { Type = "integer", Format = GetIntegerFormat(t) },
            Type t when t == typeof(float) => new OpenApiSchema { Type = "number", Format = "float" },
            Type t when t == typeof(double) => new OpenApiSchema { Type = "number", Format = "double" },
            Type t when t == typeof(bool) => new OpenApiSchema { Type = "boolean" },
            Type t when t == typeof(string) => new OpenApiSchema { Type = "string" },
            Type t when t.IsEnum => GenerateEnumSchema(t),
            _ => CreateObjectSchema(type)
        };
    }

    /// <summary>
    /// Creates an object schema with virtualization support.
    /// </summary>
    private OpenApiSchema CreateObjectSchema(Type type)
    {
        var schema = new OpenApiSchema { Type = "object" };

        // AC 301-303: Check if schema requires virtualization
        var analysis = AnalyzeSchemaForVirtualization(type);
        if (analysis.RequiresVirtualization)
        {
            ApplySchemaVirtualization(schema, type, analysis);
        }

        return schema;
    }

    /// <summary>
    /// Generates a schema ID for the specified type.
    /// </summary>
    /// <param name="type">The CLR type.</param>
    /// <returns>The schema ID.</returns>
    public string GenerateSchemaId(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        // Check if we already generated an ID for this type
        if (_typeToSchemaId.TryGetValue(type, out var existingId))
        {
            return existingId;
        }

        // Apply custom strategy if provided (AC 404)
        string baseId;
        if (_options.IdStrategy != null)
        {
            baseId = _options.IdStrategy(type);
        }
        else
        {
            baseId = GenerateDefaultSchemaId(type);
        }

        // Handle collisions with deterministic suffix (AC 402, 403)
        var finalId = ResolveCollision(type, baseId);

        // Track the mapping
        _typeToSchemaId[type] = finalId;

        return finalId;
    }

    /// <summary>
    /// Generates the default schema ID using canonical generic notation.
    /// </summary>
    private string GenerateDefaultSchemaId(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        // Handle generic types with canonical notation: Outer«Inner» (AC 406)
        var genericTypeName = type.Name;
        var backtickIndex = genericTypeName.IndexOf('`', StringComparison.Ordinal);
        if (backtickIndex > 0)
        {
            genericTypeName = genericTypeName[..backtickIndex];
        }

        var genericArgs = type.GetGenericArguments();
        var argNames = genericArgs.Select(GenerateDefaultSchemaId);

        // Use guillemet characters for generic notation (AC 406)
        return $"{genericTypeName}«{string.Join(",", argNames)}»";
    }

    /// <summary>
    /// Resolves schema ID collisions by applying deterministic suffix.
    /// </summary>
    private string ResolveCollision(Type type, string baseId)
    {
        // Track types that use this base ID
        if (!_schemaIdMap.TryGetValue(baseId, out var typesWithId))
        {
            typesWithId = [];
            _schemaIdMap[baseId] = typesWithId;
        }

        // Check if this exact type already has this ID
        if (typesWithId.Contains(type))
        {
            return baseId;
        }

        // If this is the first type with this ID, use it as-is
        if (typesWithId.Count == 0)
        {
            typesWithId.Add(type);
            return baseId;
        }

        // Collision detected - find the next available suffix (AC 402, 403)
        var suffix = 1;
        string candidateId;

        // Deterministic suffix numbering: _schemaDup{N} starting at 1
        List<Type>? candidateTypes;
        do
        {
            candidateId = $"{baseId}_schemaDup{suffix}";
            suffix++;
        }
        while (_schemaIdMap.TryGetValue(candidateId, out candidateTypes) &&
               !candidateTypes.Contains(type));

        // Emit diagnostic for collision (AC 405)
        _logger.LogWarning(
            "SCH001",
            $"Schema ID collision detected for type '{type.FullName}'. Using '{candidateId}' instead of '{baseId}'.");

        // Track the collision
        if (!_schemaIdMap.TryGetValue(candidateId, out var candidates))
        {
            candidates = [];
            _schemaIdMap[candidateId] = candidates;
        }
        candidates.Add(type);

        return candidateId;
    }

    /// <summary>
    /// Removes a type from the schema ID registry (for testing and dynamic scenarios).
    /// </summary>
    /// <remarks>
    /// When a type is removed, its suffix sequence is reclaimed deterministically (AC 408).
    /// </remarks>
    public void RemoveType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (_typeToSchemaId.TryGetValue(type, out var schemaId))
        {
            _typeToSchemaId.Remove(type);

            if (_schemaIdMap.TryGetValue(schemaId, out var types))
            {
                types.Remove(type);
                if (types.Count == 0)
                {
                    _schemaIdMap.Remove(schemaId);
                }
            }
        }
    }

    /// <summary>
    /// Clears all cached schema IDs.
    /// </summary>
    public void ClearCache()
    {
        _schemaIdMap.Clear();
        _typeToSchemaId.Clear();
    }

    /// <summary>
    /// Applies DataAnnotations attributes to an OpenAPI schema.
    /// </summary>
    /// <param name="schema">The schema to modify.</param>
    /// <param name="memberInfo">The member (property or parameter) to extract attributes from.</param>
    /// <param name="memberName">The name of the member for diagnostic messages.</param>
    public void ApplyDataAnnotations(OpenApiSchema schema, MemberInfo memberInfo, string? memberName = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(memberInfo);

        var attributes = memberInfo.GetCustomAttributes(true);
        ApplyDataAnnotationsFromAttributes(schema, attributes, memberName ?? memberInfo.Name);
    }

    /// <summary>
    /// Applies DataAnnotations attributes to an OpenAPI schema from a ParameterInfo.
    /// </summary>
    /// <param name="schema">The schema to modify.</param>
    /// <param name="parameterInfo">The parameter to extract attributes from.</param>
    /// <param name="parameterName">The name of the parameter for diagnostic messages.</param>
    public void ApplyDataAnnotations(OpenApiSchema schema, ParameterInfo parameterInfo, string? parameterName = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(parameterInfo);

        var attributes = parameterInfo.GetCustomAttributes(true);
        ApplyDataAnnotationsFromAttributes(schema, attributes, parameterName ?? parameterInfo.Name ?? "parameter");
    }
}
