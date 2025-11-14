using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.Filters;

namespace SecureSpec.AspNetCore.Tests;

public class FilterPipelineTests
{
    [Fact]
    public void ApplySchemaFilters_WithNoFilters_DoesNotThrow()
    {
        // Arrange
        var (pipeline, _) = CreatePipeline();
        var schema = new OpenApiSchema { Type = "object" };
        var context = new SchemaFilterContext
        {
            Type = typeof(string),
            SchemaId = "String"
        };

        // Act
        pipeline.ApplySchemaFilters(schema, context);

        // Assert - no exception
        Assert.NotNull(schema);
    }

    [Fact]
    public void ApplySchemaFilters_WithFilter_ExecutesFilter()
    {
        // Arrange
        var filters = new FilterCollection();
        filters.AddSchemaFilter<TestSchemaFilter>();
        var (pipeline, _) = CreatePipeline(filters);

        var schema = new OpenApiSchema { Type = "object" };
        var context = new SchemaFilterContext
        {
            Type = typeof(string),
            SchemaId = "String"
        };

        // Act
        pipeline.ApplySchemaFilters(schema, context);

        // Assert
        Assert.Contains("x-test-applied", schema.Extensions.Keys);
    }

    [Fact]
    public void ApplyOperationFilters_WithFilter_ExecutesFilter()
    {
        // Arrange
        var filters = new FilterCollection();
        filters.AddOperationFilter<TestOperationFilter>();
        var (pipeline, _) = CreatePipeline(filters);

        var operation = new OpenApiOperation();
        var context = new OperationFilterContext
        {
            MethodInfo = typeof(TestMethodContainer).GetMethod(nameof(TestMethodContainer.TestMethod))!,
            HttpMethod = "GET",
            RoutePattern = "/test"
        };

        // Act
        pipeline.ApplyOperationFilters(operation, context);

        // Assert
        Assert.Contains("x-test-applied", operation.Extensions.Keys);
    }

    [Fact]
    public void ApplyDocumentFilters_WithFilter_ExecutesFilter()
    {
        // Arrange
        var filters = new FilterCollection();
        filters.AddDocumentFilter<TestDocumentFilter>();
        var (pipeline, logger) = CreatePipeline(filters);

        var document = new OpenApiDocument();
        var context = new DocumentFilterContext
        {
            DocumentName = "v1"
        };

        // Act
        pipeline.ApplyDocumentFilters(document, context);

        // Assert
        Assert.Contains("x-test-applied", document.Extensions.Keys);

        var events = logger.GetEvents();
        Assert.Contains(events, e => e.Code == DiagnosticCodes.Discovery.FilterExecutionCompleted);
    }

    [Fact]
    public void ApplyPreSerializeFilters_WithFilter_ExecutesFilter()
    {
        // Arrange
        var filters = new FilterCollection();
        filters.AddPreSerializeFilter<TestPreSerializeFilter>();
        var (pipeline, logger) = CreatePipeline(filters);

        var document = new OpenApiDocument();
        var context = new PreSerializeFilterContext
        {
            DocumentName = "v1",
            OutputFormat = "json"
        };

        // Act
        pipeline.ApplyPreSerializeFilters(document, context);

        // Assert
        Assert.Contains("x-test-applied", document.Extensions.Keys);

        var events = logger.GetEvents();
        Assert.Contains(events, e => e.Code == DiagnosticCodes.Discovery.FilterExecutionCompleted);
    }

    [Fact]
    public void ApplySchemaFilters_WithFailingFilter_LogsError()
    {
        // Arrange
        var filters = new FilterCollection();
        filters.AddSchemaFilter<FailingSchemaFilter>();
        var (pipeline, logger) = CreatePipeline(filters);

        var schema = new OpenApiSchema { Type = "object" };
        var context = new SchemaFilterContext
        {
            Type = typeof(string),
            SchemaId = "String"
        };

        // Act
        pipeline.ApplySchemaFilters(schema, context);

        // Assert
        var events = logger.GetEvents();
        Assert.Contains(events, e =>
            e.Level == DiagnosticLevel.Error &&
            e.Message.Contains("Schema filter", StringComparison.Ordinal));
    }

    [Fact]
    public void ApplySchemaFilters_WithNullSchema_ThrowsArgumentNullException()
    {
        // Arrange
        var (pipeline, _) = CreatePipeline();
        var context = new SchemaFilterContext
        {
            Type = typeof(string),
            SchemaId = "String"
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => pipeline.ApplySchemaFilters(null!, context));
    }

    [Fact]
    public void ApplySchemaFilters_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var (pipeline, _) = CreatePipeline();
        var schema = new OpenApiSchema { Type = "object" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => pipeline.ApplySchemaFilters(schema, null!));
    }

    [Fact]
    public void ApplyOperationFilters_MultipleFilters_ExecutesInOrder()
    {
        // Arrange
        var filters = new FilterCollection();
        filters.AddOperationFilter<FirstOperationFilter>();
        filters.AddOperationFilter<SecondOperationFilter>();
        var (pipeline, _) = CreatePipeline(filters);

        var operation = new OpenApiOperation();
        var context = new OperationFilterContext
        {
            MethodInfo = typeof(TestMethodContainer).GetMethod(nameof(TestMethodContainer.TestMethod))!,
            HttpMethod = "GET",
            RoutePattern = "/test"
        };

        // Act
        pipeline.ApplyOperationFilters(operation, context);

        // Assert
        Assert.Contains("x-first", operation.Extensions.Keys);
        Assert.Contains("x-second", operation.Extensions.Keys);
    }

    [Fact]
    public void ApplyParameterFilters_WithFilter_ExecutesFilter()
    {
        // Arrange
        var filters = new FilterCollection();
        filters.AddParameterFilter<TestParameterFilter>();
        var (pipeline, _) = CreatePipeline(filters);

        var parameter = new OpenApiParameter
        {
            Name = "test",
            In = ParameterLocation.Query,
            Extensions = new Dictionary<string, Microsoft.OpenApi.Interfaces.IOpenApiExtension>()
        };
        var testMethod = typeof(TestMethodContainer).GetMethod(nameof(TestMethodContainer.TestMethod))!;
        var paramInfo = testMethod.GetParameters()[0];
        var context = new ParameterFilterContext
        {
            ParameterInfo = paramInfo,
            OperationId = "TestOp"
        };

        // Act
        pipeline.ApplyParameterFilters(parameter, context);

        // Assert
        Assert.Contains("x-test-applied", parameter.Extensions.Keys);
    }

    [Fact]
    public void ApplyRequestBodyFilters_WithFilter_ExecutesFilter()
    {
        // Arrange
        var filters = new FilterCollection();
        filters.AddRequestBodyFilter<TestRequestBodyFilter>();
        var (pipeline, _) = CreatePipeline(filters);

        var requestBody = new OpenApiRequestBody
        {
            Extensions = new Dictionary<string, Microsoft.OpenApi.Interfaces.IOpenApiExtension>()
        };
        var testMethod = typeof(TestMethodContainer).GetMethod(nameof(TestMethodContainer.TestMethod))!;
        var paramInfo = testMethod.GetParameters()[0];
        var context = new RequestBodyFilterContext
        {
            ParameterInfo = paramInfo,
            OperationId = "TestOp"
        };

        // Act
        pipeline.ApplyRequestBodyFilters(requestBody, context);

        // Assert
        Assert.Contains("x-test-applied", requestBody.Extensions.Keys);
    }

    // Helper methods and test classes

    private static (FilterPipeline, DiagnosticsLogger) CreatePipeline(FilterCollection? filters = null)
    {
        var services = new ServiceCollection();
        var logger = new DiagnosticsLogger();
        services.AddSingleton(logger);

        filters ??= new FilterCollection();

        // Register filter types
        services.AddSingleton<TestSchemaFilter>();
        services.AddSingleton<TestOperationFilter>();
        services.AddSingleton<TestDocumentFilter>();
        services.AddSingleton<TestPreSerializeFilter>();
        services.AddSingleton<TestParameterFilter>();
        services.AddSingleton<TestRequestBodyFilter>();
        services.AddSingleton<FailingSchemaFilter>();
        services.AddSingleton<FirstOperationFilter>();
        services.AddSingleton<SecondOperationFilter>();

        var serviceProvider = services.BuildServiceProvider();
        var pipeline = new FilterPipeline(serviceProvider, filters, logger);

        return (pipeline, logger);
    }

#pragma warning disable CA1812, CA1852
    private sealed class TestSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            schema.Extensions["x-test-applied"] = new Microsoft.OpenApi.Any.OpenApiString("true");
        }
    }

#pragma warning disable CA1812, CA1852
    private sealed class TestOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.Extensions["x-test-applied"] = new Microsoft.OpenApi.Any.OpenApiString("true");
        }
    }

#pragma warning disable CA1812, CA1852
    private sealed class TestDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument document, DocumentFilterContext context)
        {
            document.Extensions["x-test-applied"] = new Microsoft.OpenApi.Any.OpenApiString("true");
        }
    }

#pragma warning disable CA1812, CA1852
    private sealed class TestPreSerializeFilter : IPreSerializeFilter
    {
        public void Apply(OpenApiDocument document, PreSerializeFilterContext context)
        {
            document.Extensions["x-test-applied"] = new Microsoft.OpenApi.Any.OpenApiString("true");
        }
    }

#pragma warning disable CA1812, CA1852
    private sealed class TestParameterFilter : IParameterFilter
    {
        public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
        {
            parameter.Extensions["x-test-applied"] = new Microsoft.OpenApi.Any.OpenApiString("true");
        }
    }

#pragma warning disable CA1812, CA1852
    private sealed class TestRequestBodyFilter : IRequestBodyFilter
    {
        public void Apply(OpenApiRequestBody requestBody, RequestBodyFilterContext context)
        {
            requestBody.Extensions["x-test-applied"] = new Microsoft.OpenApi.Any.OpenApiString("true");
        }
    }

#pragma warning disable CA1812, CA1852
    private sealed class FailingSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            throw new InvalidOperationException("Test filter failure");
        }
    }

#pragma warning disable CA1812, CA1852
    private sealed class FirstOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.Extensions["x-first"] = new Microsoft.OpenApi.Any.OpenApiString("1");
        }
    }

#pragma warning disable CA1812, CA1852
    private sealed class SecondOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.Extensions["x-second"] = new Microsoft.OpenApi.Any.OpenApiString("2");
        }
    }
}

// Public test class for reflection in filter tests - placed outside to avoid CA1034
public sealed class TestMethodContainer
{
    public void TestMethod(string param) { _ = param; }
}
