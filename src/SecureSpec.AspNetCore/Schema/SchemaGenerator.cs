using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Diagnostics;
using System.Globalization;

namespace SecureSpec.AspNetCore.Schema;

/// <summary>
/// Generates OpenAPI schemas from CLR types.
/// </summary>
public class SchemaGenerator
{
    private readonly SchemaOptions _options;
    private readonly DiagnosticsLogger _logger;
    private readonly Dictionary<string, List<Type>> _schemaIdMap = [];
    private readonly Dictionary<Type, string> _typeToSchemaId = [];

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

        // Handle nullable value types
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            var schema = GenerateSchema(underlyingType);
            schema.Nullable = true; // AC 416
            return schema;
        }

        // Check for custom type mappings first
        if (_options.TypeMappings.TryGetMapping(type, out var customMapping) && customMapping != null)
        {
            return new OpenApiSchema
            {
                Type = customMapping.Type,
                Format = customMapping.Format
            };
        }

        // Handle primitive types (AC 409-418)
        return type switch
        {
            // AC 409: Guid → type:string format:uuid
            Type t when t == typeof(Guid) => new OpenApiSchema { Type = "string", Format = "uuid" },

            // AC 410: DateTime/DateTimeOffset → type:string format:date-time
            Type t when t == typeof(DateTime) || t == typeof(DateTimeOffset) =>
                new OpenApiSchema { Type = "string", Format = "date-time" },

            // AC 411: DateOnly → type:string format:date
            Type t when t == typeof(DateOnly) =>
                new OpenApiSchema { Type = "string", Format = "date" },

            // AC 412: TimeOnly → type:string format:time
            Type t when t == typeof(TimeOnly) =>
                new OpenApiSchema { Type = "string", Format = "time" },

            // AC 413: byte[] → type:string format:byte (base64url)
            Type t when t == typeof(byte[]) =>
                new OpenApiSchema { Type = "string", Format = "byte" },

            // AC 414: IFormFile → type:string format:binary
            Type t when t == typeof(IFormFile) =>
                new OpenApiSchema { Type = "string", Format = "binary" },

            // AC 415: Decimal → type:number (no format)
            Type t when t == typeof(decimal) =>
                new OpenApiSchema { Type = "number" },

            // CLR primitive char maps to single-character string
            Type t when t == typeof(char) =>
                new OpenApiSchema { Type = "string", MinLength = 1, MaxLength = 1 },

            // Standard numeric types
            Type t when t == typeof(int) || t == typeof(long) ||
                        t == typeof(short) || t == typeof(byte) ||
                        t == typeof(sbyte) || t == typeof(uint) ||
                        t == typeof(ulong) || t == typeof(ushort) =>
                new OpenApiSchema { Type = "integer", Format = GetIntegerFormat(t) },

            Type t when t == typeof(float) =>
                new OpenApiSchema { Type = "number", Format = "float" },

            Type t when t == typeof(double) =>
                new OpenApiSchema { Type = "number", Format = "double" },

            Type t when t == typeof(bool) =>
                new OpenApiSchema { Type = "boolean" },

            Type t when t == typeof(string) =>
                new OpenApiSchema { Type = "string" },

            // AC 417-419: Enum handling
            Type t when t.IsEnum => GenerateEnumSchema(t),

            // Default to object for complex types
            _ => new OpenApiSchema { Type = "object" }
        };
    }

    /// <summary>
    /// Generates a schema for an enum type.
    /// </summary>
    private OpenApiSchema GenerateEnumSchema(Type enumType)
    {
        var schema = new OpenApiSchema();

        if (_options.UseEnumStrings)
        {
            // AC 417: String mode preserves declaration order
            schema.Type = "string";
            var enumNames = Enum.GetNames(enumType);

            // Apply naming policy if configured (AC 419)
            var processedNames = _options.EnumNamingPolicy != null
                ? enumNames.Select(n => _options.EnumNamingPolicy(n))
                : enumNames;

            foreach (var name in processedNames)
            {
                schema.Enum.Add(new OpenApiString(name));
            }
        }
        else
        {
            var rawValues = Enum.GetValues(enumType);
            var enumValues = new object[rawValues.Length];
            rawValues.CopyTo(enumValues, 0);
            var underlyingType = Enum.GetUnderlyingType(enumType);
            var rangeEvaluation = EvaluateEnumNumericRange(enumValues, underlyingType);

            if (rangeEvaluation.RequiresStringFallback)
            {
                schema.Type = "string";
                foreach (var value in enumValues)
                {
                    schema.Enum.Add(new OpenApiString(ConvertEnumValueToString(value, underlyingType)));
                }

                _logger.LogWarning(
                    "SCH002",
                    $"Enum '{enumType.FullName}' contains values that exceed Int64 range. Falling back to string representation.");
            }
            else
            {
                // AC 418: Integer mode uses type:integer
                schema.Type = "integer";
                schema.Format = rangeEvaluation.UseInt64 ? "int64" : "int32";

                foreach (var value in enumValues)
                {
                    schema.Enum.Add(CreateNumericEnumValue(value, underlyingType));
                }
            }
        }

        return schema;
    }

    /// <summary>
    /// Creates an <see cref="IOpenApiAny"/> representing an enum numeric value without overflow.
    /// </summary>
    private static IOpenApiAny CreateNumericEnumValue(object value, Type underlyingType)
    {
        return Type.GetTypeCode(underlyingType) switch
        {
            TypeCode.SByte => new OpenApiInteger((sbyte)value),
            TypeCode.Byte => new OpenApiInteger((byte)value),
            TypeCode.Int16 => new OpenApiInteger((short)value),
            TypeCode.UInt16 => new OpenApiInteger((ushort)value),
            TypeCode.Int32 => new OpenApiInteger((int)value),
            TypeCode.UInt32 => CreateIntegerFromUInt32((uint)value),
            TypeCode.Int64 => new OpenApiLong((long)value),
            TypeCode.UInt64 => CreateIntegerFromUInt64((ulong)value),
            _ => throw new NotSupportedException($"Enum underlying type '{underlyingType.FullName}' is not supported.")
        };
    }

    private static IOpenApiAny CreateIntegerFromUInt32(uint value)
    {
        return value <= int.MaxValue
            ? new OpenApiInteger((int)value)
            : new OpenApiLong(value);
    }

    /// <summary>
    /// Converts a UInt64 value to OpenApiLong.
    /// Precondition: value must be less than or equal to long.MaxValue.
    /// </summary>
    private static OpenApiLong CreateIntegerFromUInt64(ulong value)
    {
        // Precondition: value <= long.MaxValue (checked by EvaluateEnumNumericRange)
        return new OpenApiLong((long)value);
    }

    private static EnumNumericEvaluation EvaluateEnumNumericRange(IReadOnlyList<object> values, Type underlyingType)
    {
        var useInt64 = false;
        var typeCode = Type.GetTypeCode(underlyingType);

        foreach (var value in values)
        {
            switch (typeCode)
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                    break;

                case TypeCode.UInt32:
                    if ((uint)value > int.MaxValue)
                    {
                        useInt64 = true;
                    }

                    break;

                case TypeCode.Int64:
                    var int64Value = (long)value;
                    if (int64Value > int.MaxValue || int64Value < int.MinValue)
                    {
                        useInt64 = true;
                    }

                    break;

                case TypeCode.UInt64:
                    var uint64Value = (ulong)value;
                    if (uint64Value > long.MaxValue)
                    {
                        return new EnumNumericEvaluation
                        {
                            RequiresStringFallback = true,
                            UseInt64 = true
                        };
                    }

                    if (uint64Value > int.MaxValue)
                    {
                        useInt64 = true;
                    }

                    break;

                default:
                    throw new NotSupportedException($"Enum underlying type '{underlyingType.FullName}' is not supported.");
            }
        }

        return new EnumNumericEvaluation
        {
            RequiresStringFallback = false,
            UseInt64 = useInt64
        };
    }

    private static string ConvertEnumValueToString(object value, Type underlyingType)
    {
        return Type.GetTypeCode(underlyingType) switch
        {
            TypeCode.SByte => ((sbyte)value).ToString(CultureInfo.InvariantCulture),
            TypeCode.Byte => ((byte)value).ToString(CultureInfo.InvariantCulture),
            TypeCode.Int16 => ((short)value).ToString(CultureInfo.InvariantCulture),
            TypeCode.UInt16 => ((ushort)value).ToString(CultureInfo.InvariantCulture),
            TypeCode.Int32 => ((int)value).ToString(CultureInfo.InvariantCulture),
            TypeCode.UInt32 => ((uint)value).ToString(CultureInfo.InvariantCulture),
            TypeCode.Int64 => ((long)value).ToString(CultureInfo.InvariantCulture),
            TypeCode.UInt64 => ((ulong)value).ToString(CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    /// <summary>
    /// Represents the evaluation result for how an enum's numeric values should be handled when generating OpenAPI schemas.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="RequiresStringFallback"/> is <c>true</c> when the enum's underlying type is <c>ulong</c> and at least one value exceeds <c>long.MaxValue</c>.
    /// In this case, the numeric value cannot be represented in JSON as a number, so a string fallback is required.
    /// </para>
    /// <para>
    /// <see cref="UseInt64"/> is <c>true</c> when the enum's underlying type is <c>long</c>, <c>ulong</c>, or <c>uint</c> and at least one value exceeds the range of <c>int</c>.
    /// This indicates that the schema should use the <c>int64</c> format instead of <c>int32</c>.
    /// </para>
    /// </remarks>
    private readonly struct EnumNumericEvaluation
    {
        /// <summary>
        /// Indicates whether the enum values require a string fallback due to exceeding the numeric range supported by JSON.
        /// </summary>
        public bool RequiresStringFallback { get; init; }

        /// <summary>
        /// Indicates whether the enum values require the use of 64-bit integers (<c>int64</c> format) in the schema.
        /// </summary>
        public bool UseInt64 { get; init; }
    }

    /// <summary>
    /// Gets the integer format for a numeric type.
    /// </summary>
    private static string? GetIntegerFormat(Type type)
    {
        return type switch
        {
            Type t when t == typeof(long) || t == typeof(ulong) => "int64",
            Type t when t == typeof(int) || t == typeof(uint) => "int32",
            _ => null
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
}
