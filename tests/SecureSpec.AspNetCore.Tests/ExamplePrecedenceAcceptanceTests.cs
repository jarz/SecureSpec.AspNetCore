using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Schema;
using Xunit;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Acceptance tests for example precedence engine (AC 4).
/// </summary>
public class ExamplePrecedenceAcceptanceTests
{
    private readonly SchemaOptions _options;
    private readonly ExampleGenerator _generator;
    private readonly ExamplePrecedenceEngine _engine;

    public ExamplePrecedenceAcceptanceTests()
    {
        _options = new SchemaOptions();
        _generator = new ExampleGenerator(_options);
        _engine = new ExamplePrecedenceEngine(_generator);
    }

    [Fact]
    public void AC4_ExamplePrecedence_NamedExamplesHaveHighestPriority()
    {
        // Arrange - All sources provided
        var context = new ExampleContext
        {
            NamedExamples =
            {
                ["priority"] = new OpenApiExample { Value = new OpenApiString("named-example") }
            },
            SingleExample = new OpenApiString("single-example"),
            ComponentExample = new OpenApiReference { Type = ReferenceType.Example, Id = "ComponentRef" },
            Schema = new OpenApiSchema { Type = "string" }
        };

        // Act
        var result = _engine.ResolveExample(context);

        // Assert - Named example wins
        Assert.NotNull(result);
        var stringResult = Assert.IsType<OpenApiString>(result);
        Assert.Equal("named-example", stringResult.Value);
    }

    [Fact]
    public void AC4_ExamplePrecedence_SingleExampleOverComponentAndGenerated()
    {
        // Arrange - No named examples
        var context = new ExampleContext
        {
            SingleExample = new OpenApiString("single-example"),
            ComponentExample = new OpenApiReference { Type = ReferenceType.Example, Id = "ComponentRef" },
            Schema = new OpenApiSchema { Type = "string" }
        };

        // Act
        var result = _engine.ResolveExample(context);

        // Assert - Single example wins over component and generated
        Assert.NotNull(result);
        var stringResult = Assert.IsType<OpenApiString>(result);
        Assert.Equal("single-example", stringResult.Value);
    }

    [Fact]
    public void AC4_ExamplePrecedence_ComponentExampleOverGenerated()
    {
        // Arrange - No named or single examples
        var context = new ExampleContext
        {
            ComponentExample = new OpenApiReference 
            { 
                Type = ReferenceType.Example, 
                Id = "UserExample" 
            },
            Schema = new OpenApiSchema { Type = "string" }
        };

        // Act
        var namedResults = _engine.ResolveNamedExamples(context);

        // Assert - Component reference is used (as named example)
        Assert.Single(namedResults);
        Assert.Contains("default", namedResults.Keys);
        Assert.NotNull(namedResults["default"].Reference);
        Assert.Equal("UserExample", namedResults["default"].Reference.Id);
    }

    [Fact]
    public void AC4_ExamplePrecedence_GeneratedFallbackWhenNoExamplesProvided()
    {
        // Arrange - Only schema provided
        var context = new ExampleContext
        {
            Schema = new OpenApiSchema 
            { 
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["id"] = new OpenApiSchema { Type = "integer" },
                    ["name"] = new OpenApiSchema { Type = "string" }
                }
            }
        };

        // Act
        var result = _engine.ResolveExample(context);

        // Assert - Generated example is created
        Assert.NotNull(result);
        var objResult = Assert.IsType<OpenApiObject>(result);
        Assert.Equal(2, objResult.Count);
        Assert.Contains("id", objResult.Keys);
        Assert.Contains("name", objResult.Keys);
    }

    [Fact]
    public void AC4_ExamplePrecedence_BlockedOverridesAllSources()
    {
        // Arrange - Blocked even with all sources available
        var context = new ExampleContext
        {
            IsBlocked = true,
            NamedExamples =
            {
                ["example"] = new OpenApiExample { Value = new OpenApiString("named") }
            },
            SingleExample = new OpenApiString("single"),
            ComponentExample = new OpenApiReference { Type = ReferenceType.Example, Id = "Ref" },
            Schema = new OpenApiSchema { Type = "string" }
        };

        // Act
        var result = _engine.ResolveExample(context);
        var namedResults = _engine.ResolveNamedExamples(context);

        // Assert - No examples returned when blocked
        Assert.Null(result);
        Assert.Empty(namedResults);
    }

    [Fact]
    public void AC4_CompleteFlow_MultipleNamedExamples()
    {
        // Arrange - Multiple named examples scenario
        var context = new ExampleContext
        {
            NamedExamples =
            {
                ["success"] = new OpenApiExample 
                { 
                    Value = new OpenApiString("success-value"),
                    Summary = "Success case"
                },
                ["error"] = new OpenApiExample 
                { 
                    Value = new OpenApiString("error-value"),
                    Summary = "Error case"
                }
            }
        };

        // Act
        var namedResults = _engine.ResolveNamedExamples(context);

        // Assert - All named examples preserved
        Assert.Equal(2, namedResults.Count);
        Assert.Contains("success", namedResults.Keys);
        Assert.Contains("error", namedResults.Keys);
        Assert.Equal("Success case", namedResults["success"].Summary);
        Assert.Equal("Error case", namedResults["error"].Summary);
    }

    [Fact]
    public void AC4_DeterministicGeneration_ComplexNestedSchema()
    {
        // Arrange - Complex schema to test deterministic generation
        var context = new ExampleContext
        {
            Schema = new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["user"] = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema>
                        {
                            ["id"] = new OpenApiSchema { Type = "string", Format = "uuid" },
                            ["email"] = new OpenApiSchema { Type = "string", Format = "email" },
                            ["created"] = new OpenApiSchema { Type = "string", Format = "date-time" }
                        }
                    },
                    ["tags"] = new OpenApiSchema
                    {
                        Type = "array",
                        Items = new OpenApiSchema { Type = "string" }
                    },
                    ["active"] = new OpenApiSchema { Type = "boolean" }
                }
            }
        };

        // Act
        var result = _engine.ResolveExample(context);

        // Assert - Complex nested structure generated correctly
        Assert.NotNull(result);
        var obj = Assert.IsType<OpenApiObject>(result);
        
        // Verify user object
        var userObj = Assert.IsType<OpenApiObject>(obj["user"]);
        Assert.Equal("00000000-0000-0000-0000-000000000000", ((OpenApiString)userObj["id"]).Value);
        Assert.Equal("user@example.com", ((OpenApiString)userObj["email"]).Value);
        Assert.Equal("2024-01-01T00:00:00Z", ((OpenApiString)userObj["created"]).Value);
        
        // Verify tags array
        var tagsArray = Assert.IsType<OpenApiArray>(obj["tags"]);
        Assert.Single(tagsArray);
        
        // Verify active boolean
        var activeBool = Assert.IsType<OpenApiBoolean>(obj["active"]);
        Assert.False(activeBool.Value);
    }

    [Fact]
    public void AC4_ConfigurationRespected_ExamplesCanBeDisabled()
    {
        // Arrange - Configuration with examples disabled
        var disabledOptions = new SchemaOptions { GenerateExamples = false };
        
        // Act & Assert - Verify configuration property exists and can be set
        Assert.False(disabledOptions.GenerateExamples);
        Assert.Equal(25, disabledOptions.ExampleGenerationTimeoutMs); // Default timeout
    }

    [Fact]
    public void AC4_TimeoutConfiguration_CanBeSet()
    {
        // Arrange
        var customOptions = new SchemaOptions { ExampleGenerationTimeoutMs = 50 };
        
        // Act & Assert
        Assert.Equal(50, customOptions.ExampleGenerationTimeoutMs);
    }

    [Fact]
    public void AC4_PrecedenceOrder_VerifyAllLevels()
    {
        // Test 1: Named > all others
        var namedContext = new ExampleContext
        {
            NamedExamples = { ["n"] = new OpenApiExample { Value = new OpenApiString("1") } },
            SingleExample = new OpenApiString("2"),
            Schema = new OpenApiSchema { Type = "string" }
        };
        Assert.Equal("1", ((OpenApiString)_engine.ResolveExample(namedContext)!).Value);

        // Test 2: Single > Component & Generated
        var singleContext = new ExampleContext
        {
            SingleExample = new OpenApiString("2"),
            ComponentExample = new OpenApiReference { Type = ReferenceType.Example, Id = "R" },
            Schema = new OpenApiSchema { Type = "string" }
        };
        Assert.Equal("2", ((OpenApiString)_engine.ResolveExample(singleContext)!).Value);

        // Test 3: Generated as fallback
        var generatedContext = new ExampleContext
        {
            Schema = new OpenApiSchema { Type = "integer" }
        };
        Assert.IsType<OpenApiInteger>(_engine.ResolveExample(generatedContext));

        // Test 4: Blocked > all
        var blockedContext = new ExampleContext
        {
            IsBlocked = true,
            NamedExamples = { ["n"] = new OpenApiExample { Value = new OpenApiString("x") } }
        };
        Assert.Null(_engine.ResolveExample(blockedContext));
    }
}
