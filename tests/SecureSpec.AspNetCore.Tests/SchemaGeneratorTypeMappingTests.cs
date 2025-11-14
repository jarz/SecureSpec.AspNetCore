using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.Schema;
using System.Globalization;
using SchemaTestTypes = SecureSpec.AspNetCore.Tests.SchemaGeneratorTestTypes;

namespace SecureSpec.AspNetCore.Tests;

public class SchemaGeneratorTypeMappingTests
{
    [Fact]
    public void GenerateSchema_WithGuid_ReturnsStringUuid()
    {
        var schema = CreateGenerator().GenerateSchema(typeof(Guid));

        Assert.Equal("string", schema.Type);
        Assert.Equal("uuid", schema.Format);
    }

    [Fact]
    public void GenerateSchema_WithDateTime_ReturnsStringDateTime()
    {
        var schema = CreateGenerator().GenerateSchema(typeof(DateTime));

        Assert.Equal("string", schema.Type);
        Assert.Equal("date-time", schema.Format);
    }

    [Fact]
    public void GenerateSchema_WithDateTimeOffset_ReturnsStringDateTime()
    {
        var schema = CreateGenerator().GenerateSchema(typeof(DateTimeOffset));

        Assert.Equal("string", schema.Type);
        Assert.Equal("date-time", schema.Format);
    }

    [Fact]
    public void GenerateSchema_WithDateOnly_ReturnsStringDate()
    {
        var schema = CreateGenerator().GenerateSchema(typeof(DateOnly));

        Assert.Equal("string", schema.Type);
        Assert.Equal("date", schema.Format);
    }

    [Fact]
    public void GenerateSchema_WithTimeOnly_ReturnsStringTime()
    {
        var schema = CreateGenerator().GenerateSchema(typeof(TimeOnly));

        Assert.Equal("string", schema.Type);
        Assert.Equal("time", schema.Format);
    }

    [Fact]
    public void GenerateSchema_WithByteArray_ReturnsStringByte()
    {
        var schema = CreateGenerator().GenerateSchema(typeof(byte[]));

        Assert.Equal("string", schema.Type);
        Assert.Equal("byte", schema.Format);
    }

    [Fact]
    public void GenerateSchema_WithIFormFile_ReturnsStringBinary()
    {
        var schema = CreateGenerator().GenerateSchema(typeof(IFormFile));

        Assert.Equal("string", schema.Type);
        Assert.Equal("binary", schema.Format);
    }

    [Fact]
    public void GenerateSchema_WithDecimal_ReturnsNumber()
    {
        var schema = CreateGenerator().GenerateSchema(typeof(decimal));

        Assert.Equal("number", schema.Type);
        Assert.Null(schema.Format);
    }

    [Fact]
    public void GenerateSchema_WithNullableInt_SetsNullableTrue()
    {
        var schema = CreateGenerator().GenerateSchema(typeof(int?));

        Assert.Equal("integer", schema.Type);
        Assert.True(schema.Nullable);
    }

    [Fact]
    public void GenerateSchema_WithNullableInt_OpenApi31_UsesAnyOfUnion()
    {
        var options = new SchemaOptions { SpecVersion = SchemaSpecVersion.OpenApi3_1 };
        var schema = CreateGenerator(options).GenerateSchema(typeof(int?));

        Assert.False(schema.Nullable);
        Assert.NotNull(schema.AnyOf);
        Assert.Equal(2, schema.AnyOf.Count);
        Assert.Equal("integer", schema.AnyOf[0].Type);
        Assert.Equal("null", schema.AnyOf[1].Type);
    }

    [Fact]
    public void GenerateSchema_WithNullableReferenceType_OpenApi30_SetsNullableTrue()
    {
        var options = new SchemaOptions { SpecVersion = SchemaSpecVersion.OpenApi3_0 };
        var schema = CreateGenerator(options).GenerateSchema(typeof(SchemaTestTypes.SimpleClass), isNullable: true);

        Assert.Equal("object", schema.Type);
        Assert.True(schema.Nullable);
        Assert.Empty(schema.AnyOf);
    }

    [Fact]
    public void GenerateSchema_WithNullableReferenceType_OpenApi31_UsesUnion()
    {
        var options = new SchemaOptions { SpecVersion = SchemaSpecVersion.OpenApi3_1 };
        var schema = CreateGenerator(options).GenerateSchema(typeof(SchemaTestTypes.SimpleClass), isNullable: true);

        Assert.False(schema.Nullable);
        Assert.Null(schema.Type);
        var variants = schema.AnyOf;
        Assert.NotNull(variants);
        Assert.Equal(2, variants.Count);
        Assert.Equal("object", variants[0].Type);
        Assert.Equal("null", variants[1].Type);
    }

    [Fact]
    public void ApplyNullability_WithExistingOneOf_AddsNullVariant()
    {
        var options = new SchemaOptions { SpecVersion = SchemaSpecVersion.OpenApi3_1 };
        var generator = CreateGenerator(options);
        var method = SchemaGeneratorTestsReflection.GetApplyNullabilityMethod();
        var composed = new OpenApiSchema();
        composed.OneOf.Add(new OpenApiSchema { Type = "string" });

        var result = (OpenApiSchema)method.Invoke(generator, new object[] { composed, true })!;

        Assert.Same(composed, result);
        Assert.Equal(2, composed.OneOf.Count);
        Assert.Equal("string", composed.OneOf[0].Type);
        Assert.Equal("null", composed.OneOf[1].Type);
    }

    [Fact]
    public void ApplyNullability_WithAllOf_MaintainsCompositionAndAddsUnion()
    {
        var options = new SchemaOptions { SpecVersion = SchemaSpecVersion.OpenApi3_1 };
        var generator = CreateGenerator(options);
        var method = SchemaGeneratorTestsReflection.GetApplyNullabilityMethod();
        var composed = new OpenApiSchema();
        composed.AllOf.Add(new OpenApiSchema { Type = "object" });

        var result = (OpenApiSchema)method.Invoke(generator, new object[] { composed, true })!;

        Assert.NotSame(composed, result);
        Assert.Equal(2, result.AnyOf.Count);
        Assert.Same(composed, result.AnyOf[0]);
        Assert.Equal("null", result.AnyOf[1].Type);
        Assert.Single(composed.AllOf);
    }

    [Fact]
    public void GenerateSchema_WithNullableDictionaryValue_OpenApi31_AddsNullVariant()
    {
        var options = new SchemaOptions { SpecVersion = SchemaSpecVersion.OpenApi3_1 };
        var schema = CreateGenerator(options).GenerateSchema(typeof(Dictionary<string, int?>));

        Assert.NotNull(schema.AdditionalProperties);
        var valueSchema = schema.AdditionalProperties!;
        Assert.Equal(2, valueSchema.AnyOf.Count);
        Assert.Equal("integer", valueSchema.AnyOf[0].Type);
        Assert.Equal("null", valueSchema.AnyOf[1].Type);
    }

    [Fact]
    public void GenerateSchema_WithNullableArray_OpenApi31_WrapsContainer()
    {
        var options = new SchemaOptions { SpecVersion = SchemaSpecVersion.OpenApi3_1 };
        var schema = CreateGenerator(options).GenerateSchema(typeof(int[]), isNullable: true);

        Assert.NotNull(schema.AnyOf);
        Assert.Equal(2, schema.AnyOf.Count);
        var arraySchema = schema.AnyOf[0];
        Assert.Equal("array", arraySchema.Type);
        Assert.Equal("integer", arraySchema.Items?.Type);
        Assert.Equal("null", schema.AnyOf[1].Type);
    }

    [Fact]
    public void GenerateSchema_WithNullableArrayItems_OpenApi31_DoesNotWrapContainer()
    {
        var options = new SchemaOptions { SpecVersion = SchemaSpecVersion.OpenApi3_1 };
        var schema = CreateGenerator(options).GenerateSchema(typeof(List<int?>));

        Assert.Equal("array", schema.Type);
        Assert.NotNull(schema.Items);
        Assert.Equal(2, schema.Items.AnyOf.Count);
        Assert.Equal("integer", schema.Items.AnyOf[0].Type);
        Assert.Equal("null", schema.Items.AnyOf[1].Type);
    }

    [Fact]
    public void GenerateSchema_WithNullableDictionaryValue_OpenApi30_SetsNullableFlag()
    {
        var options = new SchemaOptions { SpecVersion = SchemaSpecVersion.OpenApi3_0 };
        var schema = CreateGenerator(options).GenerateSchema(typeof(Dictionary<string, int?>));

        Assert.NotNull(schema.AdditionalProperties);
        var valueSchema = schema.AdditionalProperties!;
        Assert.True(valueSchema.Nullable);
        Assert.Empty(valueSchema.AnyOf);
    }

    [Fact]
    public void GenerateSchema_WithEnum_StringMode_PreservesDeclarationOrder()
    {
        var options = new SchemaOptions { UseEnumStrings = true };
        var schema = CreateGenerator(options).GenerateSchema(typeof(SchemaTestTypes.TestEnum));

        Assert.Equal("string", schema.Type);
        Assert.Equal(3, schema.Enum.Count);
        Assert.Equal("Value1", Assert.IsType<OpenApiString>(schema.Enum[0]).Value);
        Assert.Equal("Value2", Assert.IsType<OpenApiString>(schema.Enum[1]).Value);
        Assert.Equal("Value3", Assert.IsType<OpenApiString>(schema.Enum[2]).Value);
    }

    [Fact]
    public void GenerateSchema_WithEnum_IntegerMode_UsesTypeInteger()
    {
        var options = new SchemaOptions { UseEnumStrings = false };
        var schema = CreateGenerator(options).GenerateSchema(typeof(SchemaTestTypes.NumericEnum));

        Assert.Equal("integer", schema.Type);
        Assert.Equal("int32", schema.Format);
        Assert.Equal(3, schema.Enum.Count);
        Assert.Equal(0, Assert.IsType<OpenApiInteger>(schema.Enum[0]).Value);
        Assert.Equal(1, Assert.IsType<OpenApiInteger>(schema.Enum[1]).Value);
        Assert.Equal(2, Assert.IsType<OpenApiInteger>(schema.Enum[2]).Value);
    }

    [Fact]
    public void GenerateSchema_WithEnum_IntegerMode_WithLongValues_HandlesInt64()
    {
        var options = new SchemaOptions { UseEnumStrings = false };
        var schema = CreateGenerator(options).GenerateSchema(typeof(SchemaTestTypes.LongBackedEnum));

        Assert.Equal("integer", schema.Type);
        Assert.Equal("int64", schema.Format);
        Assert.Equal(2, schema.Enum.Count);
        var firstValue = Assert.IsType<OpenApiLong>(schema.Enum[0]);
        Assert.Equal(1L, firstValue.Value);
        var longValue = Assert.IsType<OpenApiLong>(schema.Enum[1]);
        Assert.Equal(long.MaxValue, longValue.Value);
    }

    [Fact]
    public void GenerateSchema_WithEnum_IntegerMode_ValueExceedsInt64_FallsBackToString()
    {
        var options = new SchemaOptions { UseEnumStrings = false };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        var schema = generator.GenerateSchema(typeof(SchemaTestTypes.HugeEnum));
        var events = logger.GetEvents();

        Assert.Equal("string", schema.Type);
        Assert.Null(schema.Format);
        Assert.Equal(2, schema.Enum.Count);
        Assert.Equal("1", Assert.IsType<OpenApiString>(schema.Enum[0]).Value);
        Assert.Equal(ulong.MaxValue.ToString(CultureInfo.InvariantCulture), Assert.IsType<OpenApiString>(schema.Enum[1]).Value);
        Assert.Contains(events, e => e.Code == "SCH002" && e.Level == DiagnosticLevel.Warn);
    }

    [Fact]
    public void GenerateSchema_WithEnum_AppliesNamingPolicy()
    {
        var options = new SchemaOptions
        {
            UseEnumStrings = true,
            EnumNamingPolicy = name => name.ToUpperInvariant()
        };
        var schema = CreateGenerator(options).GenerateSchema(typeof(SchemaTestTypes.TestEnum));

        Assert.Equal("string", schema.Type);
        Assert.Equal("VALUE1", Assert.IsType<OpenApiString>(schema.Enum[0]).Value);
        Assert.Equal("VALUE2", Assert.IsType<OpenApiString>(schema.Enum[1]).Value);
        Assert.Equal("VALUE3", Assert.IsType<OpenApiString>(schema.Enum[2]).Value);
    }

    [Fact]
    public void GenerateSchema_WithInt_ReturnsIntegerInt32()
    {
        var schema = CreateGenerator().GenerateSchema(typeof(int));

        Assert.Equal("integer", schema.Type);
        Assert.Equal("int32", schema.Format);
    }

    [Fact]
    public void GenerateSchema_WithLong_ReturnsIntegerInt64()
    {
        var schema = CreateGenerator().GenerateSchema(typeof(long));

        Assert.Equal("integer", schema.Type);
        Assert.Equal("int64", schema.Format);
    }

    [Fact]
    public void GenerateSchema_WithFloat_ReturnsNumberFloat()
    {
        var schema = CreateGenerator().GenerateSchema(typeof(float));

        Assert.Equal("number", schema.Type);
        Assert.Equal("float", schema.Format);
    }

    [Fact]
    public void GenerateSchema_WithDouble_ReturnsNumberDouble()
    {
        var schema = CreateGenerator().GenerateSchema(typeof(double));

        Assert.Equal("number", schema.Type);
        Assert.Equal("double", schema.Format);
    }

    [Fact]
    public void GenerateSchema_WithBool_ReturnsBoolean()
    {
        var schema = CreateGenerator().GenerateSchema(typeof(bool));

        Assert.Equal("boolean", schema.Type);
    }

    [Fact]
    public void GenerateSchema_WithString_ReturnsString()
    {
        var schema = CreateGenerator().GenerateSchema(typeof(string));

        Assert.Equal("string", schema.Type);
    }

    [Fact]
    public void GenerateSchema_WithChar_ReturnsStringLengthOne()
    {
        var schema = CreateGenerator().GenerateSchema(typeof(char));

        Assert.Equal("string", schema.Type);
        Assert.Equal(1, schema.MinLength);
        Assert.Equal(1, schema.MaxLength);
        Assert.False(schema.Nullable);
    }

    [Fact]
    public void GenerateSchema_WithCustomMapping_UsesMapping()
    {
        var options = new SchemaOptions();
        options.TypeMappings.Map<SchemaTestTypes.SimpleClass>(m =>
        {
            m.Type = "custom";
            m.Format = "custom-format";
        });

        var schema = CreateGenerator(options).GenerateSchema(typeof(SchemaTestTypes.SimpleClass));

        Assert.Equal("custom", schema.Type);
        Assert.Equal("custom-format", schema.Format);
    }

    [Fact]
    public void GenerateSchema_WithRecursiveEnumerable_UsesCyclePlaceholder()
    {
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(new SchemaOptions(), logger);

        var schema = generator.GenerateSchema(typeof(SchemaTestTypes.RecursiveEnumerable));

        Assert.Equal("array", schema.Type);
        Assert.NotNull(schema.Items);
        var placeholder = schema.Items!;
        Assert.True(placeholder.Extensions.ContainsKey("x-securespec-placeholder"));
        var metadata = Assert.IsType<OpenApiObject>(placeholder.Extensions["x-securespec-placeholder"]);
        Assert.Equal("cycle", Assert.IsType<OpenApiString>(metadata["kind"]).Value);
        Assert.Equal(typeof(SchemaTestTypes.RecursiveEnumerable).FullName ?? typeof(SchemaTestTypes.RecursiveEnumerable).Name, Assert.IsType<OpenApiString>(metadata["type"]).Value);
        Assert.Empty(logger.GetEvents());
    }

    [Fact]
    public void GenerateSchema_DepthLimitExceeded_LogsDiagnosticAndUsesPlaceholder()
    {
        var options = new SchemaOptions { MaxDepth = 2 };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);
        var deepType = SchemaGeneratorTestHelpers.CreateNestedListType(typeof(int), depth: 5);

        var schema = generator.GenerateSchema(deepType);

        var placeholder = SchemaGeneratorTestHelpers.FindFirstPlaceholder(schema);
        Assert.NotNull(placeholder);
        var metadata = Assert.IsType<OpenApiObject>(placeholder!.Extensions["x-securespec-placeholder"]);
        Assert.Equal("depth", Assert.IsType<OpenApiString>(metadata["kind"]).Value);

        var events = logger.GetEvents();
        var depthEvent = Assert.Single(events, e => e.Code == "SCH002");
        Assert.Equal(DiagnosticLevel.Warn, depthEvent.Level);
    }

    [Fact]
    public void GenerateSchema_MaxDepthAdjustment_RecomputesTraversal()
    {
        var options = new SchemaOptions { MaxDepth = 2 };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);
        var deepType = SchemaGeneratorTestHelpers.CreateNestedListType(typeof(int), depth: 4);

        var limitedSchema = generator.GenerateSchema(deepType);
        var limitedPlaceholder = SchemaGeneratorTestHelpers.FindFirstPlaceholder(limitedSchema);

        options.MaxDepth = 8;
        logger.Clear();
        var relaxedSchema = generator.GenerateSchema(deepType);
        var relaxedPlaceholder = SchemaGeneratorTestHelpers.FindFirstPlaceholder(relaxedSchema);

        Assert.NotNull(limitedPlaceholder);
        Assert.Null(relaxedPlaceholder);
        Assert.DoesNotContain(logger.GetEvents(), e => e.Code == "SCH002");
    }

    [Fact]
    public void GenerateSchema_ThrowsOnNullType()
    {
        var generator = CreateGenerator();

        Assert.Throws<ArgumentNullException>(() => generator.GenerateSchema(null!));
    }

    private static SchemaGenerator CreateGenerator(SchemaOptions? options = null)
    {
        options ??= new SchemaOptions();
        var logger = new DiagnosticsLogger();
        return new SchemaGenerator(options, logger);
    }
}
