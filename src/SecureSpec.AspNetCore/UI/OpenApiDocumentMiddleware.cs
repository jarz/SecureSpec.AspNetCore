using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using SecureSpec.AspNetCore.Configuration;
using System.Text;

namespace SecureSpec.AspNetCore.UI;

/// <summary>
/// Middleware for serving OpenAPI documents in JSON and YAML formats.
/// </summary>
public class OpenApiDocumentMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecureSpecOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiDocumentMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="options">The SecureSpec configuration options.</param>
    public OpenApiDocumentMiddleware(RequestDelegate next, SecureSpecOptions options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Processes the HTTP request.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var path = httpContext.Request.Path.Value?.TrimStart('/') ?? string.Empty;

        // Check if the request is for an OpenAPI document
        if (path.StartsWith("openapi/", StringComparison.OrdinalIgnoreCase))
        {
            var documentPath = path["openapi/".Length..];

            // Extract document name and format
            // Expected formats: v1.json, v1.yaml
            var parts = documentPath.Split('.');
            if (parts.Length == 2)
            {
                var documentName = parts[0];
#pragma warning disable CA1308 // Normalize strings to uppercase - file extensions are conventionally lowercase
                var format = parts[1].ToLowerInvariant();
#pragma warning restore CA1308

                if (format == "json" || format == "yaml")
                {
                    await ServeOpenApiDocumentAsync(httpContext, documentName, format);
                    return;
                }
            }
        }

        // Not an OpenAPI document request, pass to next middleware
        await _next(httpContext);
    }

    /// <summary>
    /// Serves an OpenAPI document.
    /// </summary>
    private async Task ServeOpenApiDocumentAsync(HttpContext context, string documentName, string format)
    {
        // Check if the document exists in configuration
        if (!_options.Documents.ContainsKey(documentName))
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("Document not found");
            return;
        }

        // Generate a sample OpenAPI document
        // In a real implementation, this would use the DocumentGenerator
        var document = CreateSampleDocument(documentName);

        // Serialize the document
        var content = SerializeDocument(document, format);

        // Set content type
        context.Response.ContentType = format == "json"
            ? "application/json; charset=utf-8"
            : "application/yaml; charset=utf-8";

        // Set caching headers
        context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";

        await context.Response.WriteAsync(content, Encoding.UTF8);
    }

    /// <summary>
    /// Creates a sample OpenAPI document for demonstration.
    /// This should be replaced with actual document generation.
    /// </summary>
    private OpenApiDocument CreateSampleDocument(string documentName)
    {
        _options.Documents.TryGetValue(documentName, out var doc);

        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = doc?.Info?.Title ?? "API",
                Version = doc?.Info?.Version ?? "1.0.0",
                Description = doc?.Info?.Description ?? "API Documentation"
            },
            Paths = new OpenApiPaths
            {
                ["/weatherforecast"] = new OpenApiPathItem
                {
                    Operations = new Dictionary<OperationType, OpenApiOperation>
                    {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Weather" } },
                            Summary = "Get weather forecast",
                            Description = "Retrieves a 5-day weather forecast",
                            OperationId = "GetWeatherForecast",
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Description = "Success",
                                    Content = new Dictionary<string, OpenApiMediaType>
                                    {
                                        ["application/json"] = new OpenApiMediaType
                                        {
                                            Schema = new OpenApiSchema
                                            {
                                                Type = "array",
                                                Items = new OpenApiSchema
                                                {
                                                    Reference = new OpenApiReference
                                                    {
                                                        Type = ReferenceType.Schema,
                                                        Id = "WeatherForecast"
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            },
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema>
                {
                    ["WeatherForecast"] = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema>
                        {
                            ["date"] = new OpenApiSchema
                            {
                                Type = "string",
                                Format = "date"
                            },
                            ["temperatureC"] = new OpenApiSchema
                            {
                                Type = "integer",
                                Format = "int32"
                            },
                            ["temperatureF"] = new OpenApiSchema
                            {
                                Type = "integer",
                                Format = "int32"
                            },
                            ["summary"] = new OpenApiSchema
                            {
                                Type = "string",
                                Nullable = true
                            }
                        },
                        Required = new HashSet<string> { "date", "temperatureC", "temperatureF" }
                    }
                }
            }
        };

        return document;
    }

    /// <summary>
    /// Serializes an OpenAPI document to JSON or YAML.
    /// </summary>
    private static string SerializeDocument(OpenApiDocument document, string format)
    {
        using var stringWriter = new StringWriter();

        if (format == "json")
        {
            var writer = new OpenApiJsonWriter(stringWriter);
            document.SerializeAsV3(writer);
        }
        else
        {
            var writer = new OpenApiYamlWriter(stringWriter);
            document.SerializeAsV3(writer);
        }

        return stringWriter.ToString();
    }
}
