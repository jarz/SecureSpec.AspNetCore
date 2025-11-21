using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.Filters;
using SecureSpec.AspNetCore.IntegrationTests.Controllers;
using SecureSpec.AspNetCore.IntegrationTests.Infrastructure;
using Xunit;
using FilterPipelineCore = SecureSpec.AspNetCore.Filters.FilterPipeline;

namespace SecureSpec.AspNetCore.IntegrationTests.FilterPipeline;

/// <summary>
/// Integration tests ensuring all SecureSpec filter stages resolve and log correctly.
/// </summary>
public class FilterPipelineIntegrationTests
{
    /// <summary>
    /// Asserts that filter failures are logged and remaining document filters still execute.
    /// </summary>
    [Fact]
    public async Task DocumentFilters_LogFailuresAndContinueExecution()
    {
        using var host = await IntegrationTestHostFactory.StartHostAsync(
            services =>
            {
                services.AddSecureSpec(options =>
                {
                    options.Documents.Add("v1", doc =>
                    {
                        doc.Info.Title = "Diagnostics API";
                        doc.Info.Version = "1.0.0";
                    });

                    options.Filters
                        .AddDocumentFilter<FailingDocumentFilter>()
                        .AddDocumentFilter<SuccessDocumentFilter>();
                });
            });

        var logger = host.Services.GetRequiredService<DiagnosticsLogger>();
        logger.Clear();

        var pipeline = host.Services.GetRequiredService<FilterPipelineCore>();

        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = "Diagnostics API",
                Version = "1.0.0"
            }
        };

        var context = new DocumentFilterContext { DocumentName = "v1" };

        pipeline.ApplyDocumentFilters(document, context);

        Assert.True(document.Extensions.TryGetValue("x-success", out var successExtension));
        Assert.Equal("ok", Assert.IsType<OpenApiString>(successExtension).Value);

        var events = logger.GetEvents();

        Assert.Contains(events, e =>
            e.Code == DiagnosticCodes.FilterExecutionCompleted &&
            e.Level == DiagnosticLevel.Error &&
            e.Message.Contains(nameof(FailingDocumentFilter), System.StringComparison.Ordinal));

        Assert.Contains(events, e =>
            e.Code == DiagnosticCodes.FilterExecutionCompleted &&
            e.Level == DiagnosticLevel.Info &&
            e.Message.Contains("Applied", System.StringComparison.OrdinalIgnoreCase));

        await host.StopAsync();
    }

    /// <summary>
    /// Ensures each filter type is resolved via DI and annotates the OpenAPI artifacts.
    /// </summary>
    [Fact]
    public async Task AllFilterTypes_AreResolvedAndApplied()
    {
        using var host = await IntegrationTestHostFactory.StartHostAsync(
            services =>
            {
                services.AddSecureSpec(options =>
                {
                    options.Filters
                        .AddSchemaFilter<SchemaFlagFilter>()
                        .AddOperationFilter<OperationFlagFilter>()
                        .AddParameterFilter<ParameterFlagFilter>()
                        .AddRequestBodyFilter<RequestBodyFlagFilter>()
                        .AddDocumentFilter<DocumentFlagFilter>()
                        .AddPreSerializeFilter<PreSerializeFlagFilter>();
                });
            });

        var pipeline = host.Services.GetRequiredService<FilterPipelineCore>();

        var schema = new OpenApiSchema();
        var schemaContext = new SchemaFilterContext
        {
            Type = typeof(SampleResponse),
            SchemaId = "SampleResponse"
        };
        pipeline.ApplySchemaFilters(schema, schemaContext);
        Assert.True(schema.Extensions.ContainsKey("x-schema-flag"));

        var method = typeof(SampleController).GetMethod(nameof(SampleController.GetSample))!;
        var operation = new OpenApiOperation();
        var operationContext = new OperationFilterContext
        {
            MethodInfo = method,
            HttpMethod = "GET",
            RoutePattern = "/api/sample/with-input/{id}",
            ControllerType = typeof(SampleController)
        };
        pipeline.ApplyOperationFilters(operation, operationContext);
        Assert.True(operation.Extensions.ContainsKey("x-operation-flag"));

        var parameterInfo = method.GetParameters()[0];
        var parameter = new OpenApiParameter
        {
            Name = parameterInfo.Name!,
            In = ParameterLocation.Path,
            Extensions = new Dictionary<string, Microsoft.OpenApi.Interfaces.IOpenApiExtension>()
        };
        var parameterContext = new ParameterFilterContext
        {
            ParameterInfo = parameterInfo,
            OperationId = "GetSample"
        };
        pipeline.ApplyParameterFilters(parameter, parameterContext);
        Assert.True(parameter.Extensions.ContainsKey("x-parameter-flag"));

        var requestBody = new OpenApiRequestBody
        {
            Extensions = new Dictionary<string, Microsoft.OpenApi.Interfaces.IOpenApiExtension>()
        };
        var requestContext = new RequestBodyFilterContext
        {
            ParameterInfo = parameterInfo,
            OperationId = "GetSample"
        };
        pipeline.ApplyRequestBodyFilters(requestBody, requestContext);
        Assert.True(requestBody.Extensions.ContainsKey("x-request-flag"));

        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = "Flag API",
                Version = "1.0.0"
            }
        };
        var docContext = new DocumentFilterContext { DocumentName = "v1" };
        pipeline.ApplyDocumentFilters(document, docContext);
        Assert.True(document.Extensions.ContainsKey("x-document-flag"));

        var preContext = new PreSerializeFilterContext
        {
            DocumentName = "v1",
            OutputFormat = "json"
        };
        pipeline.ApplyPreSerializeFilters(document, preContext);
        Assert.True(document.Extensions.ContainsKey("x-pre-flag"));

        await host.StopAsync();
    }
}
