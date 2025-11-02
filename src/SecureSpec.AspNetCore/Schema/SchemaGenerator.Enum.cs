using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using System.Globalization;

namespace SecureSpec.AspNetCore.Schema;

public partial class SchemaGenerator
{
    /// <summary>
    /// Generates a schema for an enum type.
    /// </summary>
    private OpenApiSchema GenerateEnumSchema(Type enumType)
    {
        if (_options.UseEnumStrings)
        {
            return GenerateStringEnumSchema(enumType);
        }

        var analysis = AnalyzeEnum(enumType);
        return analysis.NumericEvaluation.RequiresStringFallback
            ? CreateStringFallbackEnumSchema(analysis)
            : CreateNumericEnumSchema(analysis);
    }

    private static EnumAnalysis AnalyzeEnum(Type enumType)
    {
        var values = GetEnumValues(enumType);
        var underlyingType = Enum.GetUnderlyingType(enumType);
        var evaluation = EvaluateEnumNumericRange(values, underlyingType);
        return new EnumAnalysis(enumType, underlyingType, values, evaluation);
    }

    private OpenApiSchema GenerateStringEnumSchema(Type enumType)
    {
        // AC 417, AC 438: String mode preserves declaration order
        var schema = new OpenApiSchema { Type = "string" };
        var names = ApplyEnumNamingPolicy(Enum.GetNames(enumType));

        if (TryVirtualizeStringEnum(schema, names, enumType))
        {
            return schema;
        }

        AddStringEnumValues(schema, names);

        return schema;
    }

    private List<string> ApplyEnumNamingPolicy(IEnumerable<string> enumNames)
    {
        if (_options.EnumNamingPolicy == null)
        {
            return enumNames.ToList();
        }

        return enumNames.Select(n => _options.EnumNamingPolicy(n)).ToList();
    }

    private static object[] GetEnumValues(Type enumType)
    {
        var rawValues = Enum.GetValues(enumType);
        var enumValues = new object[rawValues.Length];
        rawValues.CopyTo(enumValues, 0);
        return enumValues;
    }

    private OpenApiSchema CreateStringFallbackEnumSchema(EnumAnalysis analysis)
    {
        var schema = new OpenApiSchema { Type = "string" };
        var stringValues = analysis.Values
            .Select(v => ConvertEnumValueToString(v, analysis.UnderlyingType))
            .ToList();

        if (TryVirtualizeStringEnum(schema, stringValues, analysis.EnumType))
        {
            LogEnumStringFallback(analysis.EnumType);
            return schema;
        }

        AddStringEnumValues(schema, stringValues);
        LogEnumStringFallback(analysis.EnumType);

        return schema;
    }

    private OpenApiSchema CreateNumericEnumSchema(EnumAnalysis analysis)
    {
        // AC 418, AC 439: Integer mode uses type:integer
        var schema = new OpenApiSchema
        {
            Type = "integer",
            Format = analysis.NumericEvaluation.UseInt64 ? "int64" : "int32"
        };

        if (!TryVirtualizeNumericEnum(schema, analysis))
        {
            AddNumericEnumValues(schema, analysis.Values, analysis.UnderlyingType);
        }

        return schema;
    }

    private void AddVirtualizedNumericValues(OpenApiSchema schema, EnumAnalysis analysis)
    {
        var limit = Math.Min(_options.EnumVirtualizationThreshold, analysis.Values.Length);
        for (var i = 0; i < limit; i++)
        {
            schema.Enum.Add(CreateNumericEnumValue(analysis.Values[i], analysis.UnderlyingType));
        }
    }

    private bool TryVirtualizeNumericEnum(OpenApiSchema schema, EnumAnalysis analysis)
    {
        if (!ShouldVirtualize(analysis.Values.Length))
        {
            return false;
        }

        AddVirtualizedNumericValues(schema, analysis);
        AddVirtualizationMetadata(schema, analysis.Values.Length, analysis.EnumType);
        return true;
    }

    private void LogEnumStringFallback(Type enumType)
    {
        _logger.LogWarning(
            "SCH002",
            $"Enum '{enumType.FullName}' contains values that exceed Int64 range. Falling back to string representation.");
    }

    private static void AddNumericEnumValues(OpenApiSchema schema, IEnumerable<object> enumValues, Type underlyingType)
    {
        foreach (var value in enumValues)
        {
            schema.Enum.Add(CreateNumericEnumValue(value, underlyingType));
        }
    }

    private static void AddStringEnumValues(OpenApiSchema schema, IEnumerable<string> values)
    {
        foreach (var value in values)
        {
            schema.Enum.Add(new OpenApiString(value));
        }
    }

    private bool TryVirtualizeStringEnum(OpenApiSchema schema, IReadOnlyList<string> values, Type enumType)
    {
        if (!ShouldVirtualize(values.Count))
        {
            return false;
        }

        AddVirtualizedStringValues(schema, values);
        AddVirtualizationMetadata(schema, values.Count, enumType);
        return true;
    }

    /// <summary>
    /// Applies virtualization to an enum schema with string values.
    /// </summary>
    private void AddVirtualizedStringValues(OpenApiSchema schema, IReadOnlyList<string> values)
    {
        // AC 440: Truncate to threshold
        var limit = Math.Min(_options.EnumVirtualizationThreshold, values.Count);
        for (int i = 0; i < limit; i++)
        {
            schema.Enum.Add(new OpenApiString(values[i]));
        }
    }

    private bool ShouldVirtualize(int valueCount) => valueCount > _options.EnumVirtualizationThreshold;

    /// <summary>
    /// Adds virtualization metadata to an enum schema.
    /// </summary>
    private void AddVirtualizationMetadata(OpenApiSchema schema, int totalCount, Type enumType)
    {
        schema.Extensions["x-enum-virtualized"] = new OpenApiBoolean(true);
        schema.Extensions["x-enum-total-count"] = new OpenApiInteger(totalCount);
        schema.Extensions["x-enum-truncated-count"] = new OpenApiInteger(totalCount - Math.Min(totalCount, _options.EnumVirtualizationThreshold));

        // AC 440: Emit VIRT001 diagnostic
        _logger.LogInfo(
            "VIRT001",
            $"Enum '{enumType.FullName}' has {totalCount} values, exceeding virtualization threshold of {_options.EnumVirtualizationThreshold}. Truncated to first {_options.EnumVirtualizationThreshold} values.",
            new { EnumType = enumType.FullName, TotalCount = totalCount, Threshold = _options.EnumVirtualizationThreshold });
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
        return Type.GetTypeCode(underlyingType) switch
        {
            TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 => default,
            TypeCode.UInt32 => EvaluateUInt32Range(values),
            TypeCode.Int64 => EvaluateInt64Range(values),
            TypeCode.UInt64 => EvaluateUInt64Range(values),
            _ => throw new NotSupportedException($"Enum underlying type '{underlyingType.FullName}' is not supported.")
        };
    }

    private static EnumNumericEvaluation EvaluateUInt32Range(IEnumerable<object> values)
    {
        return values.Cast<uint>().Any(v => v > int.MaxValue)
            ? new EnumNumericEvaluation { RequiresStringFallback = false, UseInt64 = true }
            : default;
    }

    private static EnumNumericEvaluation EvaluateInt64Range(IEnumerable<object> values)
    {
        return values.Cast<long>().Any(v => v is > int.MaxValue or < int.MinValue)
            ? new EnumNumericEvaluation { RequiresStringFallback = false, UseInt64 = true }
            : default;
    }

    private static EnumNumericEvaluation EvaluateUInt64Range(IEnumerable<object> values)
    {
        var castValues = values.Cast<ulong>().ToList();

        if (castValues.Any(v => v > long.MaxValue))
        {
            return new EnumNumericEvaluation
            {
                RequiresStringFallback = true,
                UseInt64 = true
            };
        }

        var requiresInt64 = castValues.Any(v => v > int.MaxValue);
        return new EnumNumericEvaluation
        {
            RequiresStringFallback = false,
            UseInt64 = requiresInt64
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

    private sealed record EnumAnalysis(Type EnumType, Type UnderlyingType, object[] Values, EnumNumericEvaluation NumericEvaluation);
}
