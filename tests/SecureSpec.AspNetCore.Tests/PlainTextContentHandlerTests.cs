using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.MediaTypes;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for AC 457: text/plain request uses raw example or generated fallback correctly.
/// </summary>
public class PlainTextContentHandlerTests
{
    [Fact]
    public void CreateMediaType_UsesRawExampleWhenProvided()
    {
        // Arrange - AC 457: raw example takes precedence
        var schema = new OpenApiSchema { Type = "string" };
        const string rawExample = "Custom raw example text";

        // Act
        var mediaType = PlainTextContentHandler.CreateMediaType(schema, rawExample);

        // Assert
        Assert.NotNull(mediaType);
        Assert.Same(schema, mediaType.Schema);
        Assert.NotNull(mediaType.Example);
        Assert.Equal(rawExample, ((Microsoft.OpenApi.Any.OpenApiString)mediaType.Example).Value);
    }

    [Fact]
    public void CreateMediaType_GeneratesFallbackWhenNoRawExample()
    {
        // Arrange - AC 457: generated fallback when no raw example
        var schema = new OpenApiSchema { Type = "string" };

        // Act
        var mediaType = PlainTextContentHandler.CreateMediaType(schema);

        // Assert
        Assert.NotNull(mediaType);
        Assert.Same(schema, mediaType.Schema);
        Assert.NotNull(mediaType.Example);
    }

    [Fact]
    public void CreateMediaType_GeneratesFallbackForStringType()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "string" };

        // Act
        var mediaType = PlainTextContentHandler.CreateMediaType(schema);

        // Assert
        Assert.Equal("example string", ((Microsoft.OpenApi.Any.OpenApiString?)mediaType.Example)?.Value);
    }

    [Fact]
    public void CreateMediaType_GeneratesFallbackForDateFormat()
    {
        // Arrange
        var schema = new OpenApiSchema
        {
            Type = "string",
            Format = "date"
        };

        // Act
        var mediaType = PlainTextContentHandler.CreateMediaType(schema);

        // Assert
        Assert.Equal("2025-01-01", ((Microsoft.OpenApi.Any.OpenApiString?)mediaType.Example)?.Value);
    }

    [Fact]
    public void CreateMediaType_GeneratesFallbackForDateTimeFormat()
    {
        // Arrange
        var schema = new OpenApiSchema
        {
            Type = "string",
            Format = "date-time"
        };

        // Act
        var mediaType = PlainTextContentHandler.CreateMediaType(schema);

        // Assert
        Assert.Equal("2025-01-01T00:00:00Z", ((Microsoft.OpenApi.Any.OpenApiString?)mediaType.Example)?.Value);
    }

    [Fact]
    public void CreateMediaType_GeneratesFallbackForUuidFormat()
    {
        // Arrange
        var schema = new OpenApiSchema
        {
            Type = "string",
            Format = "uuid"
        };

        // Act
        var mediaType = PlainTextContentHandler.CreateMediaType(schema);

        // Assert
        Assert.Equal("00000000-0000-0000-0000-000000000000", ((Microsoft.OpenApi.Any.OpenApiString?)mediaType.Example)?.Value);
    }

    [Fact]
    public void CreateMediaType_GeneratesFallbackForIntegerType()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "integer" };

        // Act
        var mediaType = PlainTextContentHandler.CreateMediaType(schema);

        // Assert
        Assert.Equal("0", ((Microsoft.OpenApi.Any.OpenApiString?)mediaType.Example)?.Value);
    }

    [Fact]
    public void CreateMediaType_GeneratesFallbackForNumberType()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "number" };

        // Act
        var mediaType = PlainTextContentHandler.CreateMediaType(schema);

        // Assert
        Assert.Equal("0.0", ((Microsoft.OpenApi.Any.OpenApiString?)mediaType.Example)?.Value);
    }

    [Fact]
    public void CreateMediaType_GeneratesFallbackForBooleanType()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "boolean" };

        // Act
        var mediaType = PlainTextContentHandler.CreateMediaType(schema);

        // Assert
        Assert.Equal("true", ((Microsoft.OpenApi.Any.OpenApiString?)mediaType.Example)?.Value);
    }

    [Fact]
    public void CreateMediaType_GeneratesFallbackForArrayType()
    {
        // Arrange
        var schema = new OpenApiSchema
        {
            Type = "array",
            Items = new OpenApiSchema { Type = "string" }
        };

        // Act
        var mediaType = PlainTextContentHandler.CreateMediaType(schema);

        // Assert
        Assert.NotNull(mediaType.Example);
        Assert.Contains("[", ((Microsoft.OpenApi.Any.OpenApiString?)mediaType.Example)?.Value, StringComparison.Ordinal);
        Assert.Contains("]", ((Microsoft.OpenApi.Any.OpenApiString?)mediaType.Example)?.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateMediaType_GeneratesFallbackForObjectType()
    {
        // Arrange
        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["name"] = new OpenApiSchema { Type = "string" },
                ["age"] = new OpenApiSchema { Type = "integer" }
            }
        };

        // Act
        var mediaType = PlainTextContentHandler.CreateMediaType(schema);

        // Assert
        Assert.NotNull(mediaType.Example);
        var example = ((Microsoft.OpenApi.Any.OpenApiString?)mediaType.Example)?.Value ?? "";
        Assert.Contains("name", example, StringComparison.Ordinal);
        Assert.Contains("age", example, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateMediaType_UsesSchemaExampleIfAvailable()
    {
        // Arrange
        var schema = new OpenApiSchema
        {
            Type = "string",
            Example = new Microsoft.OpenApi.Any.OpenApiString("schema example")
        };

        // Act
        var mediaType = PlainTextContentHandler.CreateMediaType(schema);

        // Assert
        Assert.Equal("schema example", ((Microsoft.OpenApi.Any.OpenApiString?)mediaType.Example)?.Value);
    }

    [Fact]
    public void CreateMediaType_UsesEnumValueIfAvailable()
    {
        // Arrange
        var schema = new OpenApiSchema
        {
            Type = "string",
            Enum = new List<Microsoft.OpenApi.Any.IOpenApiAny>
            {
                new Microsoft.OpenApi.Any.OpenApiString("option1"),
                new Microsoft.OpenApi.Any.OpenApiString("option2")
            }
        };

        // Act
        var mediaType = PlainTextContentHandler.CreateMediaType(schema);

        // Assert
        Assert.Equal("option1", ((Microsoft.OpenApi.Any.OpenApiString?)mediaType.Example)?.Value);
    }

    [Fact]
    public void CreateMediaType_UsesMinimumValueForInteger()
    {
        // Arrange
        var schema = new OpenApiSchema
        {
            Type = "integer",
            Minimum = 10
        };

        // Act
        var mediaType = PlainTextContentHandler.CreateMediaType(schema);

        // Assert
        Assert.Equal("10", ((Microsoft.OpenApi.Any.OpenApiString?)mediaType.Example)?.Value);
    }

    [Fact]
    public void CreateMediaType_UsesMinimumValueForNumber()
    {
        // Arrange
        var schema = new OpenApiSchema
        {
            Type = "number",
            Minimum = 5.5m
        };

        // Act
        var mediaType = PlainTextContentHandler.CreateMediaType(schema);

        // Assert
        Assert.Equal("5.5", ((Microsoft.OpenApi.Any.OpenApiString?)mediaType.Example)?.Value);
    }

    [Fact]
    public void CreateMediaType_ThrowsOnNullSchema()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => PlainTextContentHandler.CreateMediaType(null!));
    }
}
