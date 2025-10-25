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
}
