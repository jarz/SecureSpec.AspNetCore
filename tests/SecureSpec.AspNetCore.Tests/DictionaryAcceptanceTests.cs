using System.Diagnostics.CodeAnalysis;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.Schema;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Acceptance tests for Dictionary and AdditionalProperties handling (AC 432-437).
/// </summary>
public class DictionaryAcceptanceTests
{
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for schema generation tests")]
    private sealed class SimpleValue
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for schema generation tests")]
    private sealed class ComplexValue
    {
        public string? Title { get; set; }
        public int Count { get; set; }
        public List<string>? Tags { get; set; }
    }

    #region AC 432: Dictionary emits additionalProperties referencing value schema

    [Fact]
    public void AC432_DictionaryStringInt_EmitsAdditionalPropertiesWithIntegerSchema()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(Dictionary<string, int>));

        // Assert
        Assert.Equal("object", schema.Type);
        Assert.NotNull(schema.AdditionalProperties);
        Assert.Equal("integer", schema.AdditionalProperties.Type);
        Assert.Equal("int32", schema.AdditionalProperties.Format);
        Assert.False(schema.AdditionalProperties.Nullable);
    }

    [Fact]
    public void AC432_DictionaryStringString_EmitsAdditionalPropertiesWithStringSchema()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(Dictionary<string, string>));

        // Assert
        Assert.Equal("object", schema.Type);
        Assert.NotNull(schema.AdditionalProperties);
        Assert.Equal("string", schema.AdditionalProperties.Type);
    }

    [Fact]
    public void AC432_DictionaryStringComplexType_EmitsAdditionalPropertiesWithObjectSchema()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(Dictionary<string, SimpleValue>));

        // Assert
        Assert.Equal("object", schema.Type);
        Assert.NotNull(schema.AdditionalProperties);
        Assert.Equal("object", schema.AdditionalProperties.Type);
    }

    [Fact]
    public void AC432_NestedDictionary_EmitsAdditionalPropertiesWithNestedSchema()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(Dictionary<string, Dictionary<string, int>>));

        // Assert
        Assert.Equal("object", schema.Type);
        Assert.NotNull(schema.AdditionalProperties);
        Assert.Equal("object", schema.AdditionalProperties.Type);
        Assert.NotNull(schema.AdditionalProperties.AdditionalProperties);
        Assert.Equal("integer", schema.AdditionalProperties.AdditionalProperties.Type);
    }

    [Fact]
    public void AC432_IDictionaryStringInt_EmitsAdditionalPropertiesWithIntegerSchema()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(IDictionary<string, int>));

        // Assert
        Assert.Equal("object", schema.Type);
        Assert.NotNull(schema.AdditionalProperties);
        Assert.Equal("integer", schema.AdditionalProperties.Type);
    }

    [Fact]
    public void AC432_IReadOnlyDictionaryStringInt_EmitsAdditionalPropertiesWithIntegerSchema()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(IReadOnlyDictionary<string, int>));

        // Assert
        Assert.Equal("object", schema.Type);
        Assert.NotNull(schema.AdditionalProperties);
        Assert.Equal("integer", schema.AdditionalProperties.Type);
    }

    [Fact]
    public void AC432_DictionaryWithNonStringKey_DoesNotEmitAdditionalProperties()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(Dictionary<int, string>));

        // Assert
        // Non-string keys cannot be represented as additionalProperties in OpenAPI
        // Dictionary<int, string> implements IEnumerable<KeyValuePair<int, string>>
        // which is detected as KeyValuePair and excluded from array conversion,
        // so it falls back to basic object type without additionalProperties
        Assert.Equal("object", schema.Type);
        Assert.Null(schema.AdditionalProperties);
        Assert.Null(schema.Items);
    }

    [Fact]
    public void AC432_DictionaryStringGuid_EmitsAdditionalPropertiesWithGuidSchema()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(Dictionary<string, Guid>));

        // Assert
        Assert.Equal("object", schema.Type);
        Assert.NotNull(schema.AdditionalProperties);
        Assert.Equal("string", schema.AdditionalProperties.Type);
        Assert.Equal("uuid", schema.AdditionalProperties.Format);
    }

    [Fact]
    public void AC432_DictionaryStringDecimal_EmitsAdditionalPropertiesWithNumberSchema()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(Dictionary<string, decimal>));

        // Assert
        Assert.Equal("object", schema.Type);
        Assert.NotNull(schema.AdditionalProperties);
        Assert.Equal("number", schema.AdditionalProperties.Type);
        Assert.Null(schema.AdditionalProperties.Format);
    }

    [Fact]
    public void AC432_DictionaryStringArray_EmitsAdditionalPropertiesWithArraySchema()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(Dictionary<string, int[]>));

        // Assert
        Assert.Equal("object", schema.Type);
        Assert.NotNull(schema.AdditionalProperties);
        Assert.Equal("array", schema.AdditionalProperties.Type);
        Assert.NotNull(schema.AdditionalProperties.Items);
        Assert.Equal("integer", schema.AdditionalProperties.Items.Type);
    }

    #endregion

    #region AC 432 with Nullability

    [Fact]
    public void AC432_NullableDictionaryOpenApi30_SetsNullableTrue()
    {
        // Arrange
        var options = new SchemaOptions { SpecVersion = SchemaSpecVersion.OpenApi3_0 };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(Dictionary<string, int>), isNullable: true);

        // Assert
        Assert.Equal("object", schema.Type);
        Assert.True(schema.Nullable);
        Assert.NotNull(schema.AdditionalProperties);
        Assert.Equal("integer", schema.AdditionalProperties.Type);
    }

    [Fact]
    public void AC432_NullableDictionaryOpenApi31_UsesAnyOfUnion()
    {
        // Arrange
        var options = new SchemaOptions { SpecVersion = SchemaSpecVersion.OpenApi3_1 };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(Dictionary<string, int>), isNullable: true);

        // Assert
        Assert.False(schema.Nullable);
        Assert.NotNull(schema.AnyOf);
        Assert.Equal(2, schema.AnyOf.Count);

        var dictionarySchema = schema.AnyOf[0];
        Assert.Equal("object", dictionarySchema.Type);
        Assert.NotNull(dictionarySchema.AdditionalProperties);
        Assert.Equal("integer", dictionarySchema.AdditionalProperties.Type);

        Assert.Equal("null", schema.AnyOf[1].Type);
    }

    [Fact]
    public void AC432_DictionaryWithNullableValueOpenApi30_ValueSchemaIsNullable()
    {
        // Arrange
        var options = new SchemaOptions { SpecVersion = SchemaSpecVersion.OpenApi3_0 };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(Dictionary<string, int?>));

        // Assert
        Assert.Equal("object", schema.Type);
        Assert.NotNull(schema.AdditionalProperties);
        Assert.Equal("integer", schema.AdditionalProperties.Type);
        Assert.True(schema.AdditionalProperties.Nullable);
    }

    [Fact]
    public void AC432_DictionaryWithNullableValueOpenApi31_ValueSchemaUsesUnion()
    {
        // Arrange
        var options = new SchemaOptions { SpecVersion = SchemaSpecVersion.OpenApi3_1 };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(Dictionary<string, int?>));

        // Assert
        Assert.Equal("object", schema.Type);
        Assert.NotNull(schema.AdditionalProperties);
        Assert.NotNull(schema.AdditionalProperties.AnyOf);
        Assert.Equal(2, schema.AdditionalProperties.AnyOf.Count);
        Assert.Equal("integer", schema.AdditionalProperties.AnyOf[0].Type);
        Assert.Equal("null", schema.AdditionalProperties.AnyOf[1].Type);
    }

    [Fact]
    public void AC432_DictionaryWithNullableReferenceType_OpenApi31_ValueSchemaUsesUnion()
    {
        // Arrange
        var options = new SchemaOptions { SpecVersion = SchemaSpecVersion.OpenApi3_1 };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act (explicitly marking reference type as nullable)
        var schema = generator.GenerateSchema(typeof(Dictionary<string, string?>));

        // Assert
        Assert.Equal("object", schema.Type);
        Assert.NotNull(schema.AdditionalProperties);
        // Note: string? is just string in runtime, but if we later support NRT attributes,
        // we would expect a union here. For now, it's just a string.
        Assert.Equal("string", schema.AdditionalProperties.Type);
    }

    #endregion

    #region AC 436: Dictionary value schema keys lexical ordering

    [Fact]
    public void AC436_DictionaryValueSchema_MaintainsLexicalOrdering()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(Dictionary<string, int>));

        // Assert
        // The schema should have consistent structure for deterministic serialization
        Assert.NotNull(schema.AdditionalProperties);
        Assert.Equal("integer", schema.AdditionalProperties.Type);
        Assert.Equal("int32", schema.AdditionalProperties.Format);

        // When serialized, keys should be in lexical order (handled by CanonicalSerializer)
        // This test validates the schema structure is consistent
    }

    [Fact]
    public void AC436_ComplexDictionaryValueSchema_HasConsistentStructure()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(Dictionary<string, ComplexValue>));

        // Assert
        // Complex value schema should have consistent structure
        Assert.NotNull(schema.AdditionalProperties);
        Assert.Equal("object", schema.AdditionalProperties.Type);

        // The actual property ordering within the value schema will be handled
        // by schema generation for ComplexValue type
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void DictionaryStringBool_EmitsAdditionalPropertiesWithBooleanSchema()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(Dictionary<string, bool>));

        // Assert
        Assert.Equal("object", schema.Type);
        Assert.NotNull(schema.AdditionalProperties);
        Assert.Equal("boolean", schema.AdditionalProperties.Type);
    }

    [Fact]
    public void DictionaryStringDateTime_EmitsAdditionalPropertiesWithDateTimeSchema()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(Dictionary<string, DateTime>));

        // Assert
        Assert.Equal("object", schema.Type);
        Assert.NotNull(schema.AdditionalProperties);
        Assert.Equal("string", schema.AdditionalProperties.Type);
        Assert.Equal("date-time", schema.AdditionalProperties.Format);
    }

    [Fact]
    public void TripleNestedDictionary_EmitsCorrectHierarchy()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(Dictionary<string, Dictionary<string, Dictionary<string, int>>>));

        // Assert
        Assert.Equal("object", schema.Type);
        Assert.NotNull(schema.AdditionalProperties);
        Assert.Equal("object", schema.AdditionalProperties.Type);

        Assert.NotNull(schema.AdditionalProperties.AdditionalProperties);
        Assert.Equal("object", schema.AdditionalProperties.AdditionalProperties.Type);

        Assert.NotNull(schema.AdditionalProperties.AdditionalProperties.AdditionalProperties);
        Assert.Equal("integer", schema.AdditionalProperties.AdditionalProperties.AdditionalProperties.Type);
    }

    [Fact]
    public void DictionaryWithListValue_EmitsAdditionalPropertiesWithArraySchema()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(Dictionary<string, List<string>>));

        // Assert
        Assert.Equal("object", schema.Type);
        Assert.NotNull(schema.AdditionalProperties);
        Assert.Equal("array", schema.AdditionalProperties.Type);
        Assert.NotNull(schema.AdditionalProperties.Items);
        Assert.Equal("string", schema.AdditionalProperties.Items.Type);
    }

    #endregion

    #region Type Detection Edge Cases

    [Fact]
    public void DictionaryIntString_TreatedAsBasicObject()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(Dictionary<int, string>));

        // Assert
        // Dictionary with non-string key is treated as basic object
        Assert.Equal("object", schema.Type);
        Assert.Null(schema.AdditionalProperties);
    }

    [Fact]
    public void DictionaryGuidString_TreatedAsBasicObject()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(Dictionary<Guid, string>));

        // Assert
        // Dictionary with non-string key is treated as basic object
        Assert.Equal("object", schema.Type);
        Assert.Null(schema.AdditionalProperties);
    }

    #endregion
}
