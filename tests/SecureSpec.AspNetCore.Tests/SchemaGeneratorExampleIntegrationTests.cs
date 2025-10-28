using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.Schema;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Integration tests for SchemaGenerator with example precedence engine.
/// </summary>
public class SchemaGeneratorExampleIntegrationTests
{
    private readonly SchemaGenerator _generator;
    private readonly SchemaOptions _options;

    public SchemaGeneratorExampleIntegrationTests()
    {
        _options = new SchemaOptions();
        _generator = new SchemaGenerator(_options, new DiagnosticsLogger());
    }

    [Fact]
    public void ApplyExamples_WithSingleExample_SetsSchemaExample()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "string" };
        var context = new ExampleContext
        {
            SingleExample = new OpenApiString("test-value"),
            Schema = schema
        };

        // Act
        _generator.ApplyExamples(schema, context);

        // Assert
        Assert.NotNull(schema.Example);
        var result = Assert.IsType<OpenApiString>(schema.Example);
        Assert.Equal("test-value", result.Value);
    }

    [Fact]
    public void ApplyExamples_WithNamedExamples_SetsSchemaExample()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "integer" };
        var context = new ExampleContext
        {
            NamedExamples =
            {
                ["success"] = new OpenApiExample { Value = new OpenApiInteger(42) }
            },
            Schema = schema
        };

        // Act
        _generator.ApplyExamples(schema, context);

        // Assert
        Assert.NotNull(schema.Example);
        var result = Assert.IsType<OpenApiInteger>(schema.Example);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ApplyExamples_WithGeneratedFallback_SetsSchemaExample()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "boolean" };
        var context = new ExampleContext { Schema = schema };

        // Act
        _generator.ApplyExamples(schema, context);

        // Assert
        Assert.NotNull(schema.Example);
        var result = Assert.IsType<OpenApiBoolean>(schema.Example);
        Assert.False(result.Value);
    }

    [Fact]
    public void ApplyExamples_WhenBlocked_DoesNotSetExample()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "string" };
        var context = new ExampleContext
        {
            IsBlocked = true,
            SingleExample = new OpenApiString("blocked"),
            Schema = schema
        };

        // Act
        _generator.ApplyExamples(schema, context);

        // Assert
        Assert.Null(schema.Example);
    }

    [Fact]
    public void ApplyExamples_WhenDisabledInOptions_DoesNotSetExample()
    {
        // Arrange
        var options = new SchemaOptions { GenerateExamples = false };
        var generator = new SchemaGenerator(options, new DiagnosticsLogger());
        var schema = new OpenApiSchema { Type = "string" };
        var context = new ExampleContext { Schema = schema };

        // Act
        generator.ApplyExamples(schema, context);

        // Assert
        Assert.Null(schema.Example);
    }

    [Fact]
    public void ApplyExamples_PrecedenceNamedOverSingle_UsesNamedExample()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "string" };
        var context = new ExampleContext
        {
            NamedExamples =
            {
                ["priority"] = new OpenApiExample { Value = new OpenApiString("named") }
            },
            SingleExample = new OpenApiString("single"),
            Schema = schema
        };

        // Act
        _generator.ApplyExamples(schema, context);

        // Assert
        Assert.NotNull(schema.Example);
        var result = Assert.IsType<OpenApiString>(schema.Example);
        Assert.Equal("named", result.Value);
    }

    [Fact]
    public void ApplyExamples_PrecedenceSingleOverGenerated_UsesSingleExample()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "string" };
        var context = new ExampleContext
        {
            SingleExample = new OpenApiString("single"),
            Schema = schema
        };

        // Act
        _generator.ApplyExamples(schema, context);

        // Assert
        Assert.NotNull(schema.Example);
        var result = Assert.IsType<OpenApiString>(schema.Example);
        Assert.Equal("single", result.Value);
    }

    [Fact]
    public void CreateExampleContext_WithType_CreatesContextWithSchema()
    {
        // Act
        var context = _generator.CreateExampleContext(typeof(string));

        // Assert
        Assert.NotNull(context.Schema);
        Assert.Equal("string", context.Schema.Type);
        Assert.Null(context.SingleExample);
    }

    [Fact]
    public void CreateExampleContext_WithTypeAndExample_CreatesContextWithBoth()
    {
        // Arrange
        var example = new OpenApiString("custom");

        // Act
        var context = _generator.CreateExampleContext(typeof(string), example);

        // Assert
        Assert.NotNull(context.Schema);
        Assert.Equal("string", context.Schema.Type);
        Assert.NotNull(context.SingleExample);
        Assert.Equal("custom", ((OpenApiString)context.SingleExample).Value);
    }

    [Fact]
    public void ApplyExamples_ComplexObjectSchema_GeneratesCorrectExample()
    {
        // Arrange
        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["id"] = new OpenApiSchema { Type = "string", Format = "uuid" },
                ["name"] = new OpenApiSchema { Type = "string" },
                ["count"] = new OpenApiSchema { Type = "integer" }
            }
        };
        var context = new ExampleContext { Schema = schema };

        // Act
        _generator.ApplyExamples(schema, context);

        // Assert
        Assert.NotNull(schema.Example);
        var result = Assert.IsType<OpenApiObject>(schema.Example);
        Assert.Equal(3, result.Count);
        Assert.Contains("id", result.Keys);
        Assert.Contains("name", result.Keys);
        Assert.Contains("count", result.Keys);
    }

    [Fact]
    public void ApplyExamples_ArraySchema_GeneratesArrayExample()
    {
        // Arrange
        var schema = new OpenApiSchema
        {
            Type = "array",
            Items = new OpenApiSchema { Type = "string" }
        };
        var context = new ExampleContext { Schema = schema };

        // Act
        _generator.ApplyExamples(schema, context);

        // Assert
        Assert.NotNull(schema.Example);
        var result = Assert.IsType<OpenApiArray>(schema.Example);
        Assert.Single(result);
    }

    [Fact]
    public void ApplyExamples_NullSchema_ThrowsArgumentNullException()
    {
        // Arrange
        var context = new ExampleContext();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _generator.ApplyExamples(null!, context));
    }

    [Fact]
    public void ApplyExamples_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "string" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _generator.ApplyExamples(schema, null!));
    }

    [Fact]
    public void CreateExampleContext_NullType_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _generator.CreateExampleContext(null!));
    }
}
