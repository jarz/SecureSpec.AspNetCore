using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.Schema;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for schema ID generation with collision handling.
/// </summary>
public class SchemaGeneratorTests
{
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Types are used via typeof for schema generation scenarios.")]
    private sealed class SimpleClass { }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Types are used via typeof for schema generation scenarios.")]
    private sealed class AnotherClass { }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Types are used via typeof for schema generation scenarios.")]
    private sealed class GenericClass<T> { }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Types are used via typeof for schema generation scenarios.")]
    private sealed class NestedGeneric<TOuter, TInner> { }

    private enum TestEnum
    {
        Value1,
        Value2,
        Value3
    }

    private enum NumericEnum
    {
        Zero = 0,
        One = 1,
        Two = 2
    }

    private enum LongBackedEnum : long
    {
        Small = 1,
        Large = long.MaxValue
    }

    private enum HugeEnum : ulong
    {
        Small = 1,
        Huge = ulong.MaxValue
    }

    #region SchemaId Tests

    [Fact]
    public void GenerateSchemaId_WithSimpleType_ReturnsTypeName()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var id = generator.GenerateSchemaId(typeof(SimpleClass));

        // Assert
        Assert.Equal("SimpleClass", id);
    }

    [Fact]
    public void GenerateSchemaId_WithGenericType_UsesGuillemets()
    {
        // Arrange (AC 406)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var id = generator.GenerateSchemaId(typeof(GenericClass<string>));

        // Assert
        Assert.Equal("GenericClass«String»", id);
    }

    [Fact]
    public void GenerateSchemaId_WithNestedGeneric_UsesCanonicalForm()
    {
        // Arrange (AC 406)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var id = generator.GenerateSchemaId(typeof(GenericClass<GenericClass<int>>));

        // Assert
        Assert.Equal("GenericClass«GenericClass«Int32»»", id);
    }

    [Fact]
    public void GenerateSchemaId_WithMultipleGenericArgs_UsesCommaDelimiter()
    {
        // Arrange (AC 407)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var id = generator.GenerateSchemaId(typeof(NestedGeneric<string, int>));

        // Assert
        Assert.Equal("NestedGeneric«String,Int32»", id);
    }

    [Fact]
    public void GenerateSchemaId_WithCollision_AppliesSuffix()
    {
        // Arrange (AC 402)
        var options = new SchemaOptions
        {
            // Custom strategy that always returns same ID to force collision
            IdStrategy = _ => "SameId"
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var id1 = generator.GenerateSchemaId(typeof(SimpleClass));
        var id2 = generator.GenerateSchemaId(typeof(AnotherClass));

        // Assert
        Assert.Equal("SameId", id1);
        Assert.Equal("SameId_schemaDup1", id2);
    }

    [Fact]
    public void GenerateSchemaId_WithMultipleCollisions_Incrementssuffix()
    {
        // Arrange (AC 402, 403)
        var options = new SchemaOptions
        {
            IdStrategy = _ => "SameId"
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var id1 = generator.GenerateSchemaId(typeof(SimpleClass));
        var id2 = generator.GenerateSchemaId(typeof(AnotherClass));
        var id3 = generator.GenerateSchemaId(typeof(GenericClass<int>));

        // Assert
        Assert.Equal("SameId", id1);
        Assert.Equal("SameId_schemaDup1", id2);
        Assert.Equal("SameId_schemaDup2", id3);
    }

    [Fact]
    public void GenerateSchemaId_WithCollision_EmitsDiagnostic()
    {
        // Arrange (AC 405)
        var options = new SchemaOptions
        {
            IdStrategy = _ => "SameId"
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        generator.GenerateSchemaId(typeof(SimpleClass));
        generator.GenerateSchemaId(typeof(AnotherClass));
        var events = logger.GetEvents();

        // Assert
        Assert.Single(events);
        Assert.Equal("SCH001", events[0].Code);
        Assert.Equal(DiagnosticLevel.Warn, events[0].Level);
        Assert.Contains("collision", events[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateSchemaId_WithCustomStrategy_AppliesBeforeCollisionDetection()
    {
        // Arrange (AC 404)
        var options = new SchemaOptions
        {
            IdStrategy = t => $"Custom_{t.Name}"
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var id = generator.GenerateSchemaId(typeof(SimpleClass));

        // Assert
        Assert.Equal("Custom_SimpleClass", id);
    }

    [Fact]
    public void GenerateSchemaId_WithSameType_ReturnsCachedId()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var id1 = generator.GenerateSchemaId(typeof(SimpleClass));
        var id2 = generator.GenerateSchemaId(typeof(SimpleClass));

        // Assert
        Assert.Equal(id1, id2);
    }

    [Fact]
    public void GenerateSchemaId_AfterClearCache_RegeneratesIds()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);
        var id1 = generator.GenerateSchemaId(typeof(SimpleClass));

        // Act
        generator.ClearCache();
        var id2 = generator.GenerateSchemaId(typeof(SimpleClass));

        // Assert
        Assert.Equal(id1, id2); // Same ID but regenerated
    }

    [Fact]
    public void RemoveType_ReclaimsSuffixSequence()
    {
        // Arrange (AC 408)
        var options = new SchemaOptions
        {
            IdStrategy = _ => "SameId"
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        var id1 = generator.GenerateSchemaId(typeof(SimpleClass));
        var id2 = generator.GenerateSchemaId(typeof(AnotherClass));

        // Act
        generator.RemoveType(typeof(AnotherClass));
        logger.Clear(); // Clear previous diagnostics
        var id3 = generator.GenerateSchemaId(typeof(GenericClass<int>));

        // Assert
        Assert.Equal("SameId", id1);
        Assert.Equal("SameId_schemaDup1", id2);
        Assert.Equal("SameId_schemaDup1", id3); // Reclaimed suffix
    }

    [Fact]
    public void GenerateSchemaId_WithMultipleGenerators_ProducesSameIds()
    {
        // Arrange (AC 403) - stable across rebuilds
        var options1 = new SchemaOptions();
        var logger1 = new DiagnosticsLogger();
        var generator1 = new SchemaGenerator(options1, logger1);

        var options2 = new SchemaOptions();
        var logger2 = new DiagnosticsLogger();
        var generator2 = new SchemaGenerator(options2, logger2);

        // Act
        var id1 = generator1.GenerateSchemaId(typeof(GenericClass<string>));
        var id2 = generator2.GenerateSchemaId(typeof(GenericClass<string>));

        // Assert
        Assert.Equal(id1, id2);
    }

    [Fact]
    public void GenerateSchemaId_ThrowsOnNullType()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => generator.GenerateSchemaId(null!));
    }

    [Fact]
    public void RemoveType_ThrowsOnNullType()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => generator.RemoveType(null!));
    }

    #endregion

    #region Type Mapping Tests

    [Fact]
    public void GenerateSchema_WithGuid_ReturnsStringUuid()
    {
        // Arrange (AC 409)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(Guid));

        // Assert
        Assert.Equal("string", schema.Type);
        Assert.Equal("uuid", schema.Format);
    }

    [Fact]
    public void GenerateSchema_WithDateTime_ReturnsStringDateTime()
    {
        // Arrange (AC 410)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(DateTime));

        // Assert
        Assert.Equal("string", schema.Type);
        Assert.Equal("date-time", schema.Format);
    }

    [Fact]
    public void GenerateSchema_WithDateTimeOffset_ReturnsStringDateTime()
    {
        // Arrange (AC 410)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(DateTimeOffset));

        // Assert
        Assert.Equal("string", schema.Type);
        Assert.Equal("date-time", schema.Format);
    }

    [Fact]
    public void GenerateSchema_WithDateOnly_ReturnsStringDate()
    {
        // Arrange (AC 411)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(DateOnly));

        // Assert
        Assert.Equal("string", schema.Type);
        Assert.Equal("date", schema.Format);
    }

    [Fact]
    public void GenerateSchema_WithTimeOnly_ReturnsStringTime()
    {
        // Arrange (AC 412)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(TimeOnly));

        // Assert
        Assert.Equal("string", schema.Type);
        Assert.Equal("time", schema.Format);
    }

    [Fact]
    public void GenerateSchema_WithByteArray_ReturnsStringByte()
    {
        // Arrange (AC 413)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(byte[]));

        // Assert
        Assert.Equal("string", schema.Type);
        Assert.Equal("byte", schema.Format);
    }

    [Fact]
    public void GenerateSchema_WithIFormFile_ReturnsStringBinary()
    {
        // Arrange (AC 414)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(IFormFile));

        // Assert
        Assert.Equal("string", schema.Type);
        Assert.Equal("binary", schema.Format);
    }

    [Fact]
    public void GenerateSchema_WithDecimal_ReturnsNumber()
    {
        // Arrange (AC 415)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(decimal));

        // Assert
        Assert.Equal("number", schema.Type);
        Assert.Null(schema.Format);
    }

    [Fact]
    public void GenerateSchema_WithNullableInt_SetsNullableTrue()
    {
        // Arrange (AC 416)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(int?));

        // Assert
        Assert.Equal("integer", schema.Type);
        Assert.True(schema.Nullable);
    }

    [Fact]
    public void GenerateSchema_WithEnum_StringMode_PreservesDeclarationOrder()
    {
        // Arrange (AC 417)
        var options = new SchemaOptions { UseEnumStrings = true };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(TestEnum));

        // Assert
        Assert.Equal("string", schema.Type);
        Assert.Equal(3, schema.Enum.Count);
        Assert.Equal("Value1", ((Microsoft.OpenApi.Any.OpenApiString)schema.Enum[0]).Value);
        Assert.Equal("Value2", ((Microsoft.OpenApi.Any.OpenApiString)schema.Enum[1]).Value);
        Assert.Equal("Value3", ((Microsoft.OpenApi.Any.OpenApiString)schema.Enum[2]).Value);
    }

    [Fact]
    public void GenerateSchema_WithEnum_IntegerMode_UsesTypeInteger()
    {
        // Arrange (AC 418)
        var options = new SchemaOptions { UseEnumStrings = false };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(NumericEnum));

        // Assert
        Assert.Equal("integer", schema.Type);
        Assert.Equal("int32", schema.Format);
        Assert.Equal(3, schema.Enum.Count);
        Assert.Equal(0, ((Microsoft.OpenApi.Any.OpenApiInteger)schema.Enum[0]).Value);
        Assert.Equal(1, ((Microsoft.OpenApi.Any.OpenApiInteger)schema.Enum[1]).Value);
        Assert.Equal(2, ((Microsoft.OpenApi.Any.OpenApiInteger)schema.Enum[2]).Value);
    }

    [Fact]
    public void GenerateSchema_WithEnum_IntegerMode_WithLongValues_HandlesInt64()
    {
        // Arrange
        var options = new SchemaOptions { UseEnumStrings = false };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(LongBackedEnum));

        // Assert
        Assert.Equal("integer", schema.Type);
        Assert.Equal("int64", schema.Format);
        Assert.Equal(2, schema.Enum.Count);
        var firstValue = Assert.IsType<Microsoft.OpenApi.Any.OpenApiLong>(schema.Enum[0]);
        Assert.Equal(1L, firstValue.Value);
        var longValue = Assert.IsType<Microsoft.OpenApi.Any.OpenApiLong>(schema.Enum[1]);
        Assert.Equal(long.MaxValue, longValue.Value);
    }

    [Fact]
    public void GenerateSchema_WithEnum_IntegerMode_ValueExceedsInt64_FallsBackToString()
    {
        // Arrange
        var options = new SchemaOptions { UseEnumStrings = false };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(HugeEnum));
        var events = logger.GetEvents();

        // Assert
        Assert.Equal("string", schema.Type);
        Assert.Null(schema.Format);
        Assert.Equal(2, schema.Enum.Count);
        Assert.Equal("1", ((Microsoft.OpenApi.Any.OpenApiString)schema.Enum[0]).Value);
        Assert.Equal(ulong.MaxValue.ToString(CultureInfo.InvariantCulture), ((Microsoft.OpenApi.Any.OpenApiString)schema.Enum[1]).Value);
        Assert.Contains(events, e => e.Code == "SCH002" && e.Level == DiagnosticLevel.Warn);
    }

    [Fact]
    public void GenerateSchema_WithEnum_AppliesNamingPolicy()
    {
        // Arrange (AC 419)
        var options = new SchemaOptions
        {
            UseEnumStrings = true,
            EnumNamingPolicy = name => name.ToUpperInvariant()
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(TestEnum));

        // Assert
        Assert.Equal("string", schema.Type);
        Assert.Equal("VALUE1", ((Microsoft.OpenApi.Any.OpenApiString)schema.Enum[0]).Value);
        Assert.Equal("VALUE2", ((Microsoft.OpenApi.Any.OpenApiString)schema.Enum[1]).Value);
        Assert.Equal("VALUE3", ((Microsoft.OpenApi.Any.OpenApiString)schema.Enum[2]).Value);
    }

    [Fact]
    public void GenerateSchema_WithInt_ReturnsIntegerInt32()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(int));

        // Assert
        Assert.Equal("integer", schema.Type);
        Assert.Equal("int32", schema.Format);
    }

    [Fact]
    public void GenerateSchema_WithLong_ReturnsIntegerInt64()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(long));

        // Assert
        Assert.Equal("integer", schema.Type);
        Assert.Equal("int64", schema.Format);
    }

    [Fact]
    public void GenerateSchema_WithFloat_ReturnsNumberFloat()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(float));

        // Assert
        Assert.Equal("number", schema.Type);
        Assert.Equal("float", schema.Format);
    }

    [Fact]
    public void GenerateSchema_WithDouble_ReturnsNumberDouble()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(double));

        // Assert
        Assert.Equal("number", schema.Type);
        Assert.Equal("double", schema.Format);
    }

    [Fact]
    public void GenerateSchema_WithBool_ReturnsBoolean()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(bool));

        // Assert
        Assert.Equal("boolean", schema.Type);
    }

    [Fact]
    public void GenerateSchema_WithString_ReturnsString()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(string));

        // Assert
        Assert.Equal("string", schema.Type);
    }

    [Fact]
    public void GenerateSchema_WithChar_ReturnsStringLengthOne()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(char));

        // Assert
        Assert.Equal("string", schema.Type);
        Assert.Equal(1, schema.MinLength);
        Assert.Equal(1, schema.MaxLength);
        Assert.False(schema.Nullable);
    }

    [Fact]
    public void GenerateSchema_WithCustomMapping_UsesMapping()
    {
        // Arrange
        var options = new SchemaOptions();
        options.TypeMappings.Map<SimpleClass>(m =>
        {
            m.Type = "custom";
            m.Format = "custom-format";
        });
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(SimpleClass));

        // Assert
        Assert.Equal("custom", schema.Type);
        Assert.Equal("custom-format", schema.Format);
    }

    [Fact]
    public void GenerateSchema_ThrowsOnNullType()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => generator.GenerateSchema(null!));
    }

    #endregion
}
