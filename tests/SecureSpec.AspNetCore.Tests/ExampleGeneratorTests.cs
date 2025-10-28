using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Schema;

namespace SecureSpec.AspNetCore.Tests;

public class ExampleGeneratorTests
{
    private readonly ExampleGenerator _generator;

    public ExampleGeneratorTests()
    {
        _generator = new ExampleGenerator(new SchemaOptions());
    }

    [Fact]
    public void GenerateDeterministicFallback_String_ReturnsDefaultString()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "string" };

        // Act
        var result = _generator.GenerateDeterministicFallback(schema);

        // Assert
        var stringResult = Assert.IsType<OpenApiString>(result);
        Assert.Equal("string", stringResult.Value);
    }

    [Fact]
    public void GenerateDeterministicFallback_StringUuid_ReturnsUuidFormat()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "string", Format = "uuid" };

        // Act
        var result = _generator.GenerateDeterministicFallback(schema);

        // Assert
        var stringResult = Assert.IsType<OpenApiString>(result);
        Assert.Equal("00000000-0000-0000-0000-000000000000", stringResult.Value);
    }

    [Fact]
    public void GenerateDeterministicFallback_StringDateTime_ReturnsDateTimeFormat()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "string", Format = "date-time" };

        // Act
        var result = _generator.GenerateDeterministicFallback(schema);

        // Assert
        var stringResult = Assert.IsType<OpenApiString>(result);
        Assert.Equal("2024-01-01T00:00:00Z", stringResult.Value);
    }

    [Fact]
    public void GenerateDeterministicFallback_StringDate_ReturnsDateFormat()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "string", Format = "date" };

        // Act
        var result = _generator.GenerateDeterministicFallback(schema);

        // Assert
        var stringResult = Assert.IsType<OpenApiString>(result);
        Assert.Equal("2024-01-01", stringResult.Value);
    }

    [Fact]
    public void GenerateDeterministicFallback_StringTime_ReturnsTimeFormat()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "string", Format = "time" };

        // Act
        var result = _generator.GenerateDeterministicFallback(schema);

        // Assert
        var stringResult = Assert.IsType<OpenApiString>(result);
        Assert.Equal("00:00:00", stringResult.Value);
    }

    [Fact]
    public void GenerateDeterministicFallback_StringEmail_ReturnsEmailFormat()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "string", Format = "email" };

        // Act
        var result = _generator.GenerateDeterministicFallback(schema);

        // Assert
        var stringResult = Assert.IsType<OpenApiString>(result);
        Assert.Equal("user@example.com", stringResult.Value);
    }

    [Fact]
    public void GenerateDeterministicFallback_StringUri_ReturnsUriFormat()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "string", Format = "uri" };

        // Act
        var result = _generator.GenerateDeterministicFallback(schema);

        // Assert
        var stringResult = Assert.IsType<OpenApiString>(result);
        Assert.Equal("https://example.com", stringResult.Value);
    }

    [Fact]
    public void GenerateDeterministicFallback_StringEnum_ReturnsFirstEnumValue()
    {
        // Arrange
        var schema = new OpenApiSchema
        {
            Type = "string",
            Enum = new List<IOpenApiAny>
            {
                new OpenApiString("value1"),
                new OpenApiString("value2")
            }
        };

        // Act
        var result = _generator.GenerateDeterministicFallback(schema);

        // Assert
        var stringResult = Assert.IsType<OpenApiString>(result);
        Assert.Equal("value1", stringResult.Value);
    }

    [Fact]
    public void GenerateDeterministicFallback_Integer_ReturnsZero()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "integer" };

        // Act
        var result = _generator.GenerateDeterministicFallback(schema);

        // Assert
        var intResult = Assert.IsType<OpenApiInteger>(result);
        Assert.Equal(0, intResult.Value);
    }

    [Fact]
    public void GenerateDeterministicFallback_IntegerWithMinimum_ReturnsMinimum()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "integer", Minimum = 10 };

        // Act
        var result = _generator.GenerateDeterministicFallback(schema);

        // Assert
        var intResult = Assert.IsType<OpenApiInteger>(result);
        Assert.Equal(10, intResult.Value);
    }

    [Fact]
    public void GenerateDeterministicFallback_IntegerEnum_ReturnsFirstEnumValue()
    {
        // Arrange
        var schema = new OpenApiSchema
        {
            Type = "integer",
            Enum = new List<IOpenApiAny>
            {
                new OpenApiInteger(100),
                new OpenApiInteger(200)
            }
        };

        // Act
        var result = _generator.GenerateDeterministicFallback(schema);

        // Assert
        var intResult = Assert.IsType<OpenApiInteger>(result);
        Assert.Equal(100, intResult.Value);
    }

    [Fact]
    public void GenerateDeterministicFallback_Number_ReturnsZero()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "number" };

        // Act
        var result = _generator.GenerateDeterministicFallback(schema);

        // Assert
        var doubleResult = Assert.IsType<OpenApiDouble>(result);
        Assert.Equal(0.0, doubleResult.Value);
    }

    [Fact]
    public void GenerateDeterministicFallback_NumberWithMinimum_ReturnsMinimum()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "number", Minimum = 5.5m };

        // Act
        var result = _generator.GenerateDeterministicFallback(schema);

        // Assert
        var doubleResult = Assert.IsType<OpenApiDouble>(result);
        Assert.Equal(5.5, doubleResult.Value);
    }

    [Fact]
    public void GenerateDeterministicFallback_Boolean_ReturnsFalse()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "boolean" };

        // Act
        var result = _generator.GenerateDeterministicFallback(schema);

        // Assert
        var boolResult = Assert.IsType<OpenApiBoolean>(result);
        Assert.False(boolResult.Value);
    }

    [Fact]
    public void GenerateDeterministicFallback_Array_ReturnsArrayWithOneItem()
    {
        // Arrange
        var schema = new OpenApiSchema
        {
            Type = "array",
            Items = new OpenApiSchema { Type = "string" }
        };

        // Act
        var result = _generator.GenerateDeterministicFallback(schema);

        // Assert
        var arrayResult = Assert.IsType<OpenApiArray>(result);
        Assert.Single(arrayResult);
        Assert.IsType<OpenApiString>(arrayResult[0]);
    }

    [Fact]
    public void GenerateDeterministicFallback_ArrayWithoutItems_ReturnsEmptyArray()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "array" };

        // Act
        var result = _generator.GenerateDeterministicFallback(schema);

        // Assert
        var arrayResult = Assert.IsType<OpenApiArray>(result);
        Assert.Empty(arrayResult);
    }

    [Fact]
    public void GenerateDeterministicFallback_Object_ReturnsObjectWithProperties()
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
        var result = _generator.GenerateDeterministicFallback(schema);

        // Assert
        var objResult = Assert.IsType<OpenApiObject>(result);
        Assert.Equal(2, objResult.Count);
        Assert.Contains("name", objResult.Keys);
        Assert.Contains("age", objResult.Keys);
        Assert.IsType<OpenApiString>(objResult["name"]);
        Assert.IsType<OpenApiInteger>(objResult["age"]);
    }

    [Fact]
    public void GenerateDeterministicFallback_ObjectWithoutProperties_ReturnsEmptyObject()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "object" };

        // Act
        var result = _generator.GenerateDeterministicFallback(schema);

        // Assert
        var objResult = Assert.IsType<OpenApiObject>(result);
        Assert.Empty(objResult);
    }

    [Fact]
    public void GenerateDeterministicFallback_ObjectProperties_SortedLexically()
    {
        // Arrange
        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["zebra"] = new OpenApiSchema { Type = "string" },
                ["alpha"] = new OpenApiSchema { Type = "string" },
                ["middle"] = new OpenApiSchema { Type = "string" }
            }
        };

        // Act
        var result = _generator.GenerateDeterministicFallback(schema);

        // Assert
        var objResult = Assert.IsType<OpenApiObject>(result);
        var keys = objResult.Keys.ToList();
        Assert.Equal(3, keys.Count);
        // Properties should be in lexical order
        Assert.Equal("alpha", keys[0]);
        Assert.Equal("middle", keys[1]);
        Assert.Equal("zebra", keys[2]);
    }

    [Fact]
    public void GenerateDeterministicFallback_UnknownType_ReturnsNull()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "unknown" };

        // Act
        var result = _generator.GenerateDeterministicFallback(schema);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GenerateDeterministicFallback_NoType_ReturnsNull()
    {
        // Arrange
        var schema = new OpenApiSchema();

        // Act
        var result = _generator.GenerateDeterministicFallback(schema);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GenerateDeterministicFallback_NullSchema_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _generator.GenerateDeterministicFallback(null!));
    }

    [Fact]
    public void GenerateDeterministicFallback_NestedObject_GeneratesCorrectly()
    {
        // Arrange
        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["user"] = new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>
                    {
                        ["name"] = new OpenApiSchema { Type = "string" },
                        ["id"] = new OpenApiSchema { Type = "integer" }
                    }
                }
            }
        };

        // Act
        var result = _generator.GenerateDeterministicFallback(schema);

        // Assert
        var objResult = Assert.IsType<OpenApiObject>(result);
        Assert.Single(objResult);
        var userObj = Assert.IsType<OpenApiObject>(objResult["user"]);
        Assert.Equal(2, userObj.Count);
        Assert.IsType<OpenApiString>(userObj["name"]);
        Assert.IsType<OpenApiInteger>(userObj["id"]);
    }
}
