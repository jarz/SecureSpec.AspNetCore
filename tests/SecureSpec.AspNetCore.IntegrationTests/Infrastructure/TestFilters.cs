using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Filters;

namespace SecureSpec.AspNetCore.IntegrationTests.Infrastructure;

#pragma warning disable CA1812 // Instantiated via dependency injection in tests
internal sealed class FailingDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument document, DocumentFilterContext context)
    {
        throw new InvalidOperationException("Intentional failure for diagnostics validation.");
    }
}

internal sealed class SuccessDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument document, DocumentFilterContext context)
    {
        document.Extensions["x-success"] = new OpenApiString("ok");
    }
}

internal sealed class SchemaFlagFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        schema.Extensions["x-schema-flag"] = new OpenApiString("true");
    }
}

internal sealed class OperationFlagFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Extensions["x-operation-flag"] = new OpenApiString("true");
    }
}

internal sealed class ParameterFlagFilter : IParameterFilter
{
    public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
    {
        parameter.Extensions["x-parameter-flag"] = new OpenApiString("true");
    }
}

internal sealed class RequestBodyFlagFilter : IRequestBodyFilter
{
    public void Apply(OpenApiRequestBody requestBody, RequestBodyFilterContext context)
    {
        requestBody.Extensions["x-request-flag"] = new OpenApiString("true");
    }
}

internal sealed class DocumentFlagFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument document, DocumentFilterContext context)
    {
        document.Extensions["x-document-flag"] = new OpenApiString("true");
    }
}

internal sealed class PreSerializeFlagFilter : IPreSerializeFilter
{
    public void Apply(OpenApiDocument document, PreSerializeFilterContext context)
    {
        document.Extensions["x-pre-flag"] = new OpenApiString("true");
    }
}
#pragma warning restore CA1812
