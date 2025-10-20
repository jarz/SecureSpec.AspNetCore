using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Diagnostics;
using System.Text;

namespace SecureSpec.AspNetCore.Schema;

/// <summary>
/// Generates OpenAPI schemas from CLR types.
/// </summary>
public class SchemaGenerator
{
    private readonly SchemaOptions _options;
    private readonly DiagnosticsLogger _logger;
    private readonly Dictionary<string, List<Type>> _schemaIdMap = new();
    private readonly Dictionary<Type, string> _typeToSchemaId = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaGenerator"/> class.
    /// </summary>
    public SchemaGenerator(SchemaOptions options, DiagnosticsLogger logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates an OpenAPI schema for the specified type.
    /// </summary>
    /// <param name="type">The CLR type to generate a schema for.</param>
    /// <returns>The generated OpenAPI schema.</returns>
    public OpenApiSchema GenerateSchema(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        // TODO: Implement schema generation
        // - CLR type mapping
        // - Nullability resolution
        // - DataAnnotations integration
        // - Polymorphism handling
        // - Recursion detection

        return new OpenApiSchema
        {
            Type = "object"
        };
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
        var backtickIndex = genericTypeName.IndexOf('`');
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
            typesWithId = new List<Type>();
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
        do
        {
            candidateId = $"{baseId}_schemaDup{suffix}";
            suffix++;
        }
        while (_schemaIdMap.ContainsKey(candidateId) && 
               !_schemaIdMap[candidateId].Contains(type));

        // Emit diagnostic for collision (AC 405)
        _logger.LogWarning(
            "SCH001", 
            $"Schema ID collision detected for type '{type.FullName}'. Using '{candidateId}' instead of '{baseId}'.");

        // Track the collision
        if (!_schemaIdMap.ContainsKey(candidateId))
        {
            _schemaIdMap[candidateId] = new List<Type>();
        }
        _schemaIdMap[candidateId].Add(type);

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
}
