using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.Schema;

namespace SecureSpec.AspNetCore.Tests;

public class ExampleGenerationTests
{
    private readonly ExampleGenerator _generator;
    private readonly ExamplePrecedenceEngine _precedenceEngine;

    public ExampleGenerationTests()
    {
        var options = new SecureSpecOptions();
        var logger = new DiagnosticsLogger();
        _generator = new ExampleGenerator(options.Schema, logger);
        _precedenceEngine = new ExamplePrecedenceEngine(_generator);
    }

    [Fact]
    public void ResolveExample_HonorsPrecedenceOrder()
    {
        var schema = new OpenApiSchema { Type = "string" };
        var context = new ExampleContext
        {
            Schema = schema,
            SingleExample = new OpenApiString("single-example"),
            ComponentExample = new OpenApiReference
            {
                Type = ReferenceType.Schema,
                Id = "SharedExample"
            }
        };

        context.NamedExamples["named"] = new OpenApiExample
        {
            Value = new OpenApiString("named-example")
        };

        var resolved = _precedenceEngine.ResolveExample(context);

        Assert.IsType<OpenApiString>(resolved);
        Assert.Equal("named-example", ((OpenApiString)resolved!).Value);

        context.NamedExamples.Clear();
        resolved = _precedenceEngine.ResolveExample(context);

        Assert.IsType<OpenApiString>(resolved);
        Assert.Equal("single-example", ((OpenApiString)resolved!).Value);

        context.SingleExample = null;
        resolved = _precedenceEngine.ResolveExample(context);

        Assert.Null(resolved);

        context.ComponentExample = null;
        context.Schema = new OpenApiSchema
        {
            Type = "boolean"
        };

        resolved = _precedenceEngine.ResolveExample(context);

        Assert.IsType<OpenApiBoolean>(resolved);
        Assert.False(((OpenApiBoolean)resolved!).Value);
    }

    [Fact]
    public void ResolveNamedExamples_FallsBackToGeneratedExample()
    {
        var schema = new OpenApiSchema
        {
            Type = "integer",
            Minimum = 5
        };

        var context = new ExampleContext
        {
            Schema = schema
        };

        var named = _precedenceEngine.ResolveNamedExamples(context);

        Assert.Single(named);
        var defaultExample = Assert.Contains("generated", named);
        Assert.IsType<OpenApiInteger>(defaultExample.Value);
        Assert.Equal(5, ((OpenApiInteger)defaultExample.Value).Value);
    }

    [Fact]
    public void GenerateDeterministicFallback_ForArrayProducesDeterministicValues()
    {
        var schema = new OpenApiSchema
        {
            Type = "array",
            Items = new OpenApiSchema
            {
                Type = "string",
                Enum =
                {
                    new OpenApiString("first"),
                    new OpenApiString("second")
                }
            }
        };

        var example = _generator.GenerateDeterministicFallback(schema);

        var array = Assert.IsType<OpenApiArray>(example);
        Assert.Single(array);
        var value = Assert.IsType<OpenApiString>(array[0]);
        Assert.Equal("first", value.Value);
    }
}
