using System.Globalization;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Core;
using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.Security;
using SecureSpec.AspNetCore.Serialization;
using SecureSpec.AspNetCore.UI;

namespace SecureSpec.AspNetCore.TestHost;

/// <summary>
/// Builds the SecureSpec integration test host.
/// </summary>
public static class SecureSpecIntegrationApplication
{
    private static readonly IReadOnlyList<WeatherForecast> ForecastSamples = new List<WeatherForecast>
    {
        new(new DateOnly(2024, 1, 1), -3, "Freezing"),
        new(new DateOnly(2024, 1, 2), 2, "Chilly"),
        new(new DateOnly(2024, 1, 3), 6, "Cool"),
        new(new DateOnly(2024, 1, 4), 12, "Mild"),
        new(new DateOnly(2024, 1, 5), 18, "Warm")
    };

    private static readonly Uri OAuthTokenEndpoint = new("https://auth.example.com/token", UriKind.Absolute);

    /// <summary>
    /// Builds the WebApplication used for integration testing.
    /// </summary>
    public static WebApplication Build(string[]? args = null)
    {
        var options = new Microsoft.AspNetCore.Builder.WebApplicationOptions
        {
            Args = args ?? Array.Empty<string>(),
            EnvironmentName = "IntegrationTesting"
        };
        var builder = WebApplication.CreateBuilder(options);
        builder.Services.AddRouting();

        builder.Services.AddSecureSpec(options =>
        {
            options.Documents.Add("v1", document =>
            {
                document.Info.Title = "SecureSpec Integration API";
                document.Info.Version = "1.0.0";
                document.Info.Description = "SecureSpec integration testing surface";

                document.SecurityRequirements = new List<OpenApiSecurityRequirement>
                {
                    new SecurityRequirementBuilder()
                        .AddScheme("bearerAuth")
                        .Build(),
                    new SecurityRequirementBuilder()
                        .AddScheme("apiKeyHeader")
                        .Build()
                };
            });

            options.Schema.MaxDepth = 32;
            options.Schema.UseEnumStrings = true;

            options.Security.AddHttpBearer("bearerAuth", builder =>
                builder.WithDescription("JWT Bearer token authentication")
                    .WithBearerFormat("JWT"));

            options.Security.AddApiKeyHeader("apiKeyHeader", builder =>
                builder.WithName("X-API-Key")
                    .WithDescription("API Key authentication via header"));

            options.Security.AddOAuth2ClientCredentials("oauth2", builder => builder
                .WithTokenUrl(OAuthTokenEndpoint)
                .WithDescription("OAuth2 Client Credentials authentication")
                .AddScope("api", "Full API access")
                .AddScope("read", "Read access to weather data"));

            options.Security.AddMutualTls("mutualTLS", builder =>
                builder.WithDescription("Mutual TLS authentication for secure service-to-service communication."));

            options.UI.DocumentTitle = "SecureSpec Integration API";
            options.UI.DeepLinking = true;
            options.UI.DisplayOperationId = true;
            options.UI.DefaultModelsExpandDepth = 2;
            options.UI.EnableFiltering = true;
            options.UI.EnableTryItOut = true;

            options.UI.Assets.CacheLifetimeSeconds = 600;
            options.UI.Assets.EnableIntegrityRevalidation = true;
            options.UI.Assets.AllowPublicCache = false;

            options.Integrity.Enabled = true;
            options.Integrity.FailClosed = true;
            options.Integrity.GenerateSri = true;

            options.Serialization.GenerateHashes = true;
            options.Serialization.GenerateETags = true;

            options.Performance.EnableResourceGuards = true;
            options.Performance.MaxGenerationTimeMs = 2000;
            options.Performance.MaxMemoryBytes = 10 * 1024 * 1024;
        });

        builder.Services.AddSingleton<DocumentGenerator>(sp =>
        {
            var optionsAccessor = sp.GetRequiredService<IOptions<SecureSpecOptions>>();
            var logger = sp.GetRequiredService<DiagnosticsLogger>();
            return new DocumentGenerator(optionsAccessor.Value, logger);
        });

        var app = builder.Build();

        app.UseSecureSpecAssetCache();
        app.UseSecureSpecUI();

        MapDocumentIntegrationEndpoints(app);

        app.MapGet("/weatherforecast", () => ForecastSamples);

        app.MapGet("/healthz", () => Results.Ok("healthy"));

        return app;
    }

    private static void MapDocumentIntegrationEndpoints(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        MapDocumentGenerationEndpoint(app);
        MapDocumentCacheEndpoints(app);
        MapDocumentMaintenanceEndpoints(app);
    }

    private static void MapDocumentGenerationEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/integration/documents/{documentName}", (
            string documentName,
            HttpContext context,
            DocumentGenerator generator,
            DocumentCache cache,
            IOptions<SecureSpecOptions> optionsAccessor) =>
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(generator);
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(optionsAccessor);

            var serializationFormat = ResolveSerializationFormat(context.Request.Query.TryGetValue("format", out var formatValues)
                ? formatValues.ToString()
                : null);

            var simulateLimit = context.Request.Query.ContainsKey("simulateLimit");
            var expirationSeconds = ParseExpirationSeconds(context.Request.Query.TryGetValue("expireSeconds", out var expirationValues)
                ? expirationValues.ToString()
                : null);

            var document = generator.GenerateWithGuards(documentName, () =>
                GenerateDocument(optionsAccessor.Value, documentName, simulateLimit));

            var (content, hash, sri) = CanonicalSerializer.SerializeWithIntegrity(document, serializationFormat);

            var cacheKey = BuildCacheKey(documentName, serializationFormat);
            cache.Set(cacheKey, content, hash, TimeSpan.FromSeconds(expirationSeconds));

            context.Response.Headers.ETag = CanonicalSerializer.GenerateETag(hash);
            context.Response.Headers["X-SecureSpec-Sri"] = sri;
            context.Response.Headers["X-SecureSpec-Cache-Key"] = cacheKey;
            context.Response.Headers["X-SecureSpec-Fallback"] = document.Info.Description?.Contains("fallback", StringComparison.OrdinalIgnoreCase) == true
                ? "true"
                : "false";

            var contentType = serializationFormat == SerializationFormat.Json
                ? "application/json; charset=utf-8"
                : "application/yaml; charset=utf-8";

            return Results.Text(content, contentType);
        });
    }

    private static void MapDocumentCacheEndpoints(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/integration/documents/{documentName}/cache", (
            string documentName,
            HttpContext context,
            DocumentCache cache) =>
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(cache);

            var serializationFormat = ResolveSerializationFormat(context.Request.Query.TryGetValue("format", out var formatValues)
                ? formatValues.ToString()
                : null);
            var cacheKey = BuildCacheKey(documentName, serializationFormat);

            if (cache.TryGet(cacheKey, out var cachedContent, out var cachedHash))
            {
                return Results.Json(new
                {
                    found = true,
                    cacheKey,
                    hash = cachedHash,
                    length = cachedContent?.Length ?? 0
                });
            }

            return Results.Json(new { found = false, cacheKey });
        });

        app.MapDelete("/integration/documents/{documentName}", (
            string documentName,
            HttpContext context,
            DocumentCache cache) =>
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(cache);

            var serializationFormat = ResolveSerializationFormat(context.Request.Query.TryGetValue("format", out var formatValues)
                ? formatValues.ToString()
                : null);
            var cacheKey = BuildCacheKey(documentName, serializationFormat);
            var removed = cache.Invalidate(cacheKey);
            return Results.Json(new { removed, cacheKey, remaining = cache.Count });
        });
    }

    private static void MapDocumentMaintenanceEndpoints(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/integration/documents/evict", (DocumentCache cache) =>
        {
            ArgumentNullException.ThrowIfNull(cache);
            var evicted = cache.EvictExpired();
            return Results.Json(new { evicted, remaining = cache.Count });
        });

        app.MapPost("/integration/documents/invalidate-all", (DocumentCache cache) =>
        {
            ArgumentNullException.ThrowIfNull(cache);
            cache.InvalidateAll();
            return Results.Json(new { remaining = cache.Count });
        });
    }

    private static string BuildCacheKey(string documentName, SerializationFormat format)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentName);
        return format == SerializationFormat.Yaml
            ? $"{documentName}.yaml"
            : $"{documentName}.json";
    }

    private static SerializationFormat ResolveSerializationFormat(string? format)
    {
        return string.Equals(format, "yaml", StringComparison.OrdinalIgnoreCase)
            ? SerializationFormat.Yaml
            : SerializationFormat.Json;
    }

    private static double ParseExpirationSeconds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return 30;
        }

        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value > 0)
        {
            return value;
        }

        return 0.5;
    }

    private static OpenApiDocument GenerateDocument(SecureSpecOptions options, string documentName, bool simulateLimit)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentName);

        if (simulateLimit)
        {
            throw new ResourceLimitExceededException($"Simulated limit for document '{documentName}'");
        }

        if (!options.Documents.TryGetValue(documentName, out var documentTemplate))
        {
            documentTemplate = new OpenApiDocument
            {
                Info = new OpenApiInfo
                {
                    Title = documentName,
                    Version = "1.0.0"
                },
                Paths = new OpenApiPaths(),
                Components = new OpenApiComponents()
            };
        }

        var document = new OpenApiDocument
        {
            Info = documentTemplate.Info,
            Components = documentTemplate.Components,
            Paths = documentTemplate.Paths,
            SecurityRequirements = documentTemplate.SecurityRequirements ?? new List<OpenApiSecurityRequirement>()
        };

        if (!document.Paths.ContainsKey("/weatherforecast"))
        {
            document.Paths["/weatherforecast"] = new OpenApiPathItem
            {
                Operations = new Dictionary<OperationType, OpenApiOperation>
                {
                    [OperationType.Get] = new OpenApiOperation
                    {
                        Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Weather" } },
                        Summary = "Get weather forecast",
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
            };
        }

        if (!document.Components.Schemas.ContainsKey("WeatherForecast"))
        {
            document.Components.Schemas["WeatherForecast"] = new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["date"] = new OpenApiSchema { Type = "string", Format = "date" },
                    ["temperatureC"] = new OpenApiSchema { Type = "integer", Format = "int32" },
                    ["temperatureF"] = new OpenApiSchema { Type = "integer", Format = "int32" },
                    ["summary"] = new OpenApiSchema { Type = "string", Nullable = true }
                },
                Required = new HashSet<string> { "date", "temperatureC", "temperatureF" }
            };
        }

        return document;
    }

    private sealed record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary);
}
