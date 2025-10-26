using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Schema;
using Xunit;

namespace SecureSpec.AspNetCore.Tests;

public class ExamplePrecedenceEngineTests
{
    private readonly SchemaOptions _options;
    private readonly ExampleGenerator _generator;
    private readonly ExamplePrecedenceEngine _engine;

    public ExamplePrecedenceEngineTests()
    {
        _options = new SchemaOptions();
        _generator = new ExampleGenerator(_options);
        _engine = new ExamplePrecedenceEngine(_generator);
    }

    [Fact]
    public void ResolveExample_WhenBlocked_ReturnsNull()
    {
        // Arrange
        var context = new ExampleContext
        {
            IsBlocked = true,
            SingleExample = new OpenApiString("test"),
            Schema = new OpenApiSchema { Type = "string" }
        };

        // Act
        var result = _engine.ResolveExample(context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ResolveExample_NamedExamples_HasHighestPriority()
    {
        // Arrange
        var namedExample = new OpenApiString("named-value");
        var context = new ExampleContext
        {
            NamedExamples =
            {
                ["example1"] = new OpenApiExample { Value = namedExample }
            },
            SingleExample = new OpenApiString("single-value"),
            Schema = new OpenApiSchema { Type = "string" }
        };

        // Act
        var result = _engine.ResolveExample(context);

        // Assert
        Assert.NotNull(result);
        var stringResult = Assert.IsType<OpenApiString>(result);
        Assert.Equal("named-value", stringResult.Value);
    }

    [Fact]
    public void ResolveExample_SingleExample_UsedWhenNoNamedExamples()
    {
        // Arrange
        var singleExample = new OpenApiString("single-value");
        var context = new ExampleContext
        {
            SingleExample = singleExample,
            Schema = new OpenApiSchema { Type = "string" }
        };

        // Act
        var result = _engine.ResolveExample(context);

        // Assert
        Assert.NotNull(result);
        var stringResult = Assert.IsType<OpenApiString>(result);
        Assert.Equal("single-value", stringResult.Value);
    }

    [Fact]
    public void ResolveExample_ComponentExample_UsedWhenNoSingleExample()
    {
        // Arrange
        var context = new ExampleContext
        {
            ComponentExample = new OpenApiReference
            {
                Type = ReferenceType.Example,
                Id = "UserExample"
            }
        };

        // Act
        var result = _engine.ResolveExample(context);

        // Assert
        // Component examples return null (handled during serialization)
        Assert.Null(result);
    }

    [Fact]
    public void ResolveExample_GeneratedFallback_UsedWhenOnlySchemaProvided()
    {
        // Arrange
        var context = new ExampleContext
        {
            Schema = new OpenApiSchema { Type = "string" }
        };

        // Act
        var result = _engine.ResolveExample(context);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<OpenApiString>(result);
    }

    [Fact]
    public void ResolveExample_NoExamples_ReturnsNull()
    {
        // Arrange
        var context = new ExampleContext();

        // Act
        var result = _engine.ResolveExample(context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ResolveNamedExamples_WhenBlocked_ReturnsEmpty()
    {
        // Arrange
        var context = new ExampleContext
        {
            IsBlocked = true,
            SingleExample = new OpenApiString("test")
        };

        // Act
        var result = _engine.ResolveNamedExamples(context);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ResolveNamedExamples_NamedExamples_ReturnedDirectly()
    {
        // Arrange
        var context = new ExampleContext
        {
            NamedExamples =
            {
                ["example1"] = new OpenApiExample { Value = new OpenApiString("value1") },
                ["example2"] = new OpenApiExample { Value = new OpenApiString("value2") }
            }
        };

        // Act
        var result = _engine.ResolveNamedExamples(context);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("example1", result.Keys);
        Assert.Contains("example2", result.Keys);
    }

    [Fact]
    public void ResolveNamedExamples_SingleExample_ConvertedToDefaultNamed()
    {
        // Arrange
        var context = new ExampleContext
        {
            SingleExample = new OpenApiString("single-value")
        };

        // Act
        var result = _engine.ResolveNamedExamples(context);

        // Assert
        Assert.Single(result);
        Assert.Contains("default", result.Keys);
        Assert.Equal("single-value", ((OpenApiString)result["default"].Value).Value);
    }

    [Fact]
    public void ResolveNamedExamples_ComponentExample_ConvertedToDefaultNamed()
    {
        // Arrange
        var context = new ExampleContext
        {
            ComponentExample = new OpenApiReference
            {
                Type = ReferenceType.Example,
                Id = "UserExample"
            }
        };

        // Act
        var result = _engine.ResolveNamedExamples(context);

        // Assert
        Assert.Single(result);
        Assert.Contains("default", result.Keys);
        Assert.NotNull(result["default"].Reference);
        Assert.Equal(ReferenceType.Example, result["default"].Reference.Type);
        Assert.Equal("UserExample", result["default"].Reference.Id);
    }

    [Fact]
    public void ResolveNamedExamples_GeneratedFallback_ReturnsGeneratedNamed()
    {
        // Arrange
        var context = new ExampleContext
        {
            Schema = new OpenApiSchema { Type = "integer" }
        };

        // Act
        var result = _engine.ResolveNamedExamples(context);

        // Assert
        Assert.Single(result);
        Assert.Contains("generated", result.Keys);
        Assert.NotNull(result["generated"].Value);
    }

    [Fact]
    public void ResolveNamedExamples_NoExamples_ReturnsEmpty()
    {
        // Arrange
        var context = new ExampleContext();

        // Act
        var result = _engine.ResolveNamedExamples(context);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void PrecedenceOrder_NamedOverSingle()
    {
        // Arrange
        var context = new ExampleContext
        {
            NamedExamples =
            {
                ["first"] = new OpenApiExample { Value = new OpenApiString("named") }
            },
            SingleExample = new OpenApiString("single")
        };

        // Act
        var result = _engine.ResolveExample(context);

        // Assert
        Assert.Equal("named", ((OpenApiString)result!).Value);
    }

    [Fact]
    public void PrecedenceOrder_SingleOverComponent()
    {
        // Arrange
        var context = new ExampleContext
        {
            SingleExample = new OpenApiString("single"),
            ComponentExample = new OpenApiReference { Type = ReferenceType.Example, Id = "Ref" }
        };

        // Act
        var result = _engine.ResolveExample(context);

        // Assert
        Assert.Equal("single", ((OpenApiString)result!).Value);
    }

    [Fact]
    public void PrecedenceOrder_ComponentOverGenerated()
    {
        // Arrange
        var context = new ExampleContext
        {
            ComponentExample = new OpenApiReference { Type = ReferenceType.Example, Id = "Ref" },
            Schema = new OpenApiSchema { Type = "string" }
        };

        // Act
        var result = _engine.ResolveExample(context);

        // Assert
        // Component returns null (handled separately), but we verify it was checked before generated
        Assert.Null(result);
    }

    [Fact]
    public void PrecedenceOrder_GeneratedOverNone()
    {
        // Arrange
        var context = new ExampleContext
        {
            Schema = new OpenApiSchema { Type = "boolean" }
        };

        // Act
        var result = _engine.ResolveExample(context);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<OpenApiBoolean>(result);
    }

    [Fact]
    public void PrecedenceOrder_BlockedOverAll()
    {
        // Arrange - Blocked should override everything
        var context = new ExampleContext
        {
            IsBlocked = true,
            NamedExamples =
            {
                ["example"] = new OpenApiExample { Value = new OpenApiString("value") }
            },
            SingleExample = new OpenApiString("single"),
            ComponentExample = new OpenApiReference { Type = ReferenceType.Example, Id = "Ref" },
            Schema = new OpenApiSchema { Type = "string" }
        };

        // Act
        var result = _engine.ResolveExample(context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ResolveExample_NullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _engine.ResolveExample(null!));
    }

    [Fact]
    public void ResolveNamedExamples_NullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _engine.ResolveNamedExamples(null!));
    }
}
