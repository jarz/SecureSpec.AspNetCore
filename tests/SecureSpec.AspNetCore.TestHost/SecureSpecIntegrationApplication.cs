using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Core;
using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.Security;
using SecureSpec.AspNetCore.Serialization;
using SecureSpec.AspNetCore.UI;
using SecureSpec.AspNetCore.TestHost.Orders;
using Microsoft.AspNetCore.Mvc;

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
        builder.Services.ConfigureHttpJsonOptions(httpOptions =>
        {
            httpOptions.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            httpOptions.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            httpOptions.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        builder.Services.AddSingleton<OrderRepository>();

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
                        .Build(),
                    new SecurityRequirementBuilder()
                        .AddScheme("oauth2", "api")
                        .Build(),
                    new SecurityRequirementBuilder()
                        .AddScheme("mutualTLS")
                        .Build()
                };

                AddOrderSchemas(document);
                AddOrderPaths(document);
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
        MapOrderIntegrationEndpoints(app);

        app.MapGet("/weatherforecast", () => ForecastSamples);

        app.MapGet("/healthz", () => Results.Ok("healthy"));

        SeedOrderData(app);

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

            var simulation = ResolveGenerationSimulation(context.Request.Query);
            var expirationSeconds = ParseExpirationSeconds(context.Request.Query.TryGetValue("expireSeconds", out var expirationValues)
                ? expirationValues.ToString()
                : null);

            var document = generator.GenerateWithGuards(documentName, () =>
                GenerateDocument(optionsAccessor.Value, documentName, simulation));

            var (content, hash, sri) = CanonicalSerializer.SerializeWithIntegrity(document, serializationFormat);

            var cacheKey = BuildCacheKey(documentName, serializationFormat);
            cache.Set(cacheKey, content, hash, TimeSpan.FromSeconds(expirationSeconds));

            context.Response.Headers.ETag = CanonicalSerializer.GenerateETag(hash);
            context.Response.Headers["X-SecureSpec-Sri"] = sri;
            context.Response.Headers["X-SecureSpec-Cache-Key"] = cacheKey;
            var schemeCount = ComputeSecuritySchemeCount(document, optionsAccessor.Value);
            context.Response.Headers["X-SecureSpec-Security-Scheme-Count"] = schemeCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
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

    private static void AddOrderSchemas(OpenApiDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, OpenApiSchema>(StringComparer.Ordinal);

        document.Components.Schemas["Money"] = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["currency"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("USD") },
                ["amount"] = new OpenApiSchema { Type = "number", Format = "decimal", Example = new OpenApiDouble(19.99) }
            },
            Required = new HashSet<string> { "currency", "amount" }
        };

        document.Components.Schemas["OrderStatus"] = new OpenApiSchema
        {
            Type = "string",
            Enum = Enum.GetNames(typeof(OrderStatus)).Select(name => (IOpenApiAny)new OpenApiString(name)).ToList()
        };

        document.Components.Schemas["OrderItem"] = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["itemId"] = new OpenApiSchema { Type = "string", Format = "uuid" },
                ["sku"] = new OpenApiSchema { Type = "string" },
                ["name"] = new OpenApiSchema { Type = "string" },
                ["quantity"] = new OpenApiSchema { Type = "integer", Minimum = 1 },
                ["unitPrice"] = new OpenApiSchema { Reference = new OpenApiReference { Id = "Money", Type = ReferenceType.Schema } },
                ["lineTotal"] = new OpenApiSchema { Reference = new OpenApiReference { Id = "Money", Type = ReferenceType.Schema } },
                ["customizations"] = new OpenApiSchema
                {
                    Type = "object",
                    AdditionalProperties = new OpenApiSchema { Type = "string" }
                }
            },
            Required = new HashSet<string> { "itemId", "sku", "name", "quantity", "unitPrice", "lineTotal" }
        };

        document.Components.Schemas["Address"] = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["line1"] = new OpenApiSchema { Type = "string" },
                ["line2"] = new OpenApiSchema { Type = "string", Nullable = true },
                ["city"] = new OpenApiSchema { Type = "string" },
                ["region"] = new OpenApiSchema { Type = "string" },
                ["postalCode"] = new OpenApiSchema { Type = "string" },
                ["country"] = new OpenApiSchema { Type = "string" }
            },
            Required = new HashSet<string> { "line1", "city", "region", "postalCode", "country" }
        };

        document.Components.Schemas["CustomerProfile"] = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["id"] = new OpenApiSchema { Type = "string" },
                ["email"] = new OpenApiSchema { Type = "string", Format = "email" },
                ["firstName"] = new OpenApiSchema { Type = "string", Nullable = true },
                ["lastName"] = new OpenApiSchema { Type = "string", Nullable = true },
                ["loyaltyTier"] = new OpenApiSchema { Type = "string", Nullable = true },
                ["attributes"] = new OpenApiSchema
                {
                    Type = "object",
                    AdditionalProperties = new OpenApiSchema { Type = "string" },
                    Nullable = true
                }
            },
            Required = new HashSet<string> { "id", "email" }
        };

        document.Components.Schemas["PaymentSummary"] = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["method"] = new OpenApiSchema { Type = "string" },
                ["reference"] = new OpenApiSchema { Type = "string" },
                ["capturePending"] = new OpenApiSchema { Type = "boolean" },
                ["amountAuthorized"] = new OpenApiSchema { Type = "number", Format = "decimal" },
                ["amountCaptured"] = new OpenApiSchema { Type = "number", Format = "decimal" },
                ["currency"] = new OpenApiSchema { Type = "string" },
                ["authorizedAt"] = new OpenApiSchema { Type = "string", Format = "date-time" },
                ["capturedAt"] = new OpenApiSchema { Type = "string", Format = "date-time", Nullable = true },
                ["card"] = new OpenApiSchema { Reference = new OpenApiReference { Id = "CardPaymentDetails", Type = ReferenceType.Schema }, Nullable = true },
                ["metadata"] = new OpenApiSchema
                {
                    Type = "object",
                    AdditionalProperties = new OpenApiSchema { Type = "string" },
                    Nullable = true
                }
            },
            Required = new HashSet<string> { "method", "reference", "capturePending", "amountAuthorized", "amountCaptured", "currency", "authorizedAt" }
        };

        document.Components.Schemas["CardPaymentDetails"] = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["last4"] = new OpenApiSchema { Type = "string" },
                ["network"] = new OpenApiSchema { Type = "string" },
                ["expMonth"] = new OpenApiSchema { Type = "integer", Minimum = 1, Maximum = 12 },
                ["expYear"] = new OpenApiSchema { Type = "integer" },
                ["tokenized"] = new OpenApiSchema { Type = "boolean" },
                ["billingAddress"] = new OpenApiSchema { Reference = new OpenApiReference { Id = "Address", Type = ReferenceType.Schema }, Nullable = true }
            },
            Required = new HashSet<string> { "last4", "network", "expMonth", "expYear", "tokenized" }
        };

        document.Components.Schemas["Order"] = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["id"] = new OpenApiSchema { Type = "string", Format = "uuid" },
                ["clientReference"] = new OpenApiSchema { Type = "string", Nullable = true },
                ["status"] = new OpenApiSchema { Reference = new OpenApiReference { Id = "OrderStatus", Type = ReferenceType.Schema } },
                ["createdAt"] = new OpenApiSchema { Type = "string", Format = "date-time" },
                ["updatedAt"] = new OpenApiSchema { Type = "string", Format = "date-time" },
                ["customer"] = new OpenApiSchema { Reference = new OpenApiReference { Id = "CustomerProfile", Type = ReferenceType.Schema } },
                ["shippingAddress"] = new OpenApiSchema { Reference = new OpenApiReference { Id = "Address", Type = ReferenceType.Schema } },
                ["billingAddress"] = new OpenApiSchema { Reference = new OpenApiReference { Id = "Address", Type = ReferenceType.Schema }, Nullable = true },
                ["items"] = new OpenApiSchema
                {
                    Type = "array",
                    Items = new OpenApiSchema { Reference = new OpenApiReference { Id = "OrderItem", Type = ReferenceType.Schema } }
                },
                ["payment"] = new OpenApiSchema { Reference = new OpenApiReference { Id = "PaymentSummary", Type = ReferenceType.Schema } },
                ["subtotal"] = new OpenApiSchema { Reference = new OpenApiReference { Id = "Money", Type = ReferenceType.Schema } },
                ["tax"] = new OpenApiSchema { Reference = new OpenApiReference { Id = "Money", Type = ReferenceType.Schema } },
                ["total"] = new OpenApiSchema { Reference = new OpenApiReference { Id = "Money", Type = ReferenceType.Schema } },
                ["notes"] = new OpenApiSchema { Type = "string", Nullable = true },
                ["metadata"] = new OpenApiSchema
                {
                    Type = "object",
                    AdditionalProperties = new OpenApiSchema { Type = "string" }
                },
                ["auditTrail"] = new OpenApiSchema
                {
                    Type = "array",
                    Items = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema>
                        {
                            ["status"] = new OpenApiSchema { Reference = new OpenApiReference { Id = "OrderStatus", Type = ReferenceType.Schema } },
                            ["message"] = new OpenApiSchema { Type = "string" },
                            ["occurredAt"] = new OpenApiSchema { Type = "string", Format = "date-time" },
                            ["details"] = new OpenApiSchema { Type = "string", Nullable = true }
                        }
                    }
                }
            },
            Required = new HashSet<string> { "id", "status", "createdAt", "updatedAt", "customer", "shippingAddress", "items", "payment", "subtotal", "tax", "total", "metadata", "auditTrail" }
        };

        document.Components.Schemas["CreateOrderRequest"] = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["clientReference"] = new OpenApiSchema { Type = "string", Nullable = true },
                ["customer"] = new OpenApiSchema { Reference = new OpenApiReference { Id = "CustomerProfile", Type = ReferenceType.Schema } },
                ["shippingAddress"] = new OpenApiSchema { Reference = new OpenApiReference { Id = "Address", Type = ReferenceType.Schema } },
                ["billingAddress"] = new OpenApiSchema { Reference = new OpenApiReference { Id = "Address", Type = ReferenceType.Schema }, Nullable = true },
                ["items"] = new OpenApiSchema
                {
                    Type = "array",
                    Items = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema>
                        {
                            ["sku"] = new OpenApiSchema { Type = "string" },
                            ["name"] = new OpenApiSchema { Type = "string" },
                            ["quantity"] = new OpenApiSchema { Type = "integer", Minimum = 1 },
                            ["unitPrice"] = new OpenApiSchema { Reference = new OpenApiReference { Id = "Money", Type = ReferenceType.Schema } },
                            ["customizations"] = new OpenApiSchema
                            {
                                Type = "object",
                                AdditionalProperties = new OpenApiSchema { Type = "string" },
                                Nullable = true
                            }
                        },
                        Required = new HashSet<string> { "sku", "name", "quantity", "unitPrice" }
                    }
                },
                ["payment"] = new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>
                    {
                        ["method"] = new OpenApiSchema { Type = "string" },
                        ["reference"] = new OpenApiSchema { Type = "string", Nullable = true },
                        ["captureImmediately"] = new OpenApiSchema { Type = "boolean" },
                        ["card"] = new OpenApiSchema { Reference = new OpenApiReference { Id = "CardPaymentDetails", Type = ReferenceType.Schema }, Nullable = true },
                        ["metadata"] = new OpenApiSchema
                        {
                            Type = "object",
                            AdditionalProperties = new OpenApiSchema { Type = "string" },
                            Nullable = true
                        }
                    },
                    Required = new HashSet<string> { "method" }
                },
                ["notes"] = new OpenApiSchema { Type = "string", Nullable = true },
                ["metadata"] = new OpenApiSchema
                {
                    Type = "object",
                    AdditionalProperties = new OpenApiSchema { Type = "string" },
                    Nullable = true
                }
            },
            Required = new HashSet<string> { "customer", "shippingAddress", "items", "payment" }
        };

        document.Components.Schemas["UpdateOrderStatusRequest"] = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["status"] = new OpenApiSchema { Reference = new OpenApiReference { Id = "OrderStatus", Type = ReferenceType.Schema } },
                ["reason"] = new OpenApiSchema { Type = "string", Nullable = true },
                ["capturePayment"] = new OpenApiSchema { Type = "boolean", Nullable = true }
            },
            Required = new HashSet<string> { "status" }
        };

        document.Components.Schemas["OrderMetadataPatch"] = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["changes"] = new OpenApiSchema
                {
                    Type = "object",
                    AdditionalProperties = new OpenApiSchema { Type = "string", Nullable = true }
                }
            },
            Required = new HashSet<string> { "changes" }
        };

        document.Components.Schemas["OrderListResponse"] = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["statusFilter"] = new OpenApiSchema { Reference = new OpenApiReference { Id = "OrderStatus", Type = ReferenceType.Schema }, Nullable = true },
                ["total"] = new OpenApiSchema { Type = "integer" },
                ["items"] = new OpenApiSchema
                {
                    Type = "array",
                    Items = new OpenApiSchema { Reference = new OpenApiReference { Id = "Order", Type = ReferenceType.Schema } }
                }
            },
            Required = new HashSet<string> { "total", "items" }
        };
    }

    private static void AddOrderPaths(OpenApiDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        document.Paths ??= new OpenApiPaths();

        document.Paths["/orders"] = new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Get] = new OpenApiOperation
                {
                    Summary = "Lists orders",
                    OperationId = "ListOrders",
                    Parameters = new List<OpenApiParameter>
                    {
                        new()
                        {
                            Name = "status",
                            In = ParameterLocation.Query,
                            Schema = new OpenApiSchema { Reference = new OpenApiReference { Id = "OrderStatus", Type = ReferenceType.Schema }, Nullable = true },
                            Required = false
                        },
                        new()
                        {
                            Name = "limit",
                            In = ParameterLocation.Query,
                            Schema = new OpenApiSchema { Type = "integer", Minimum = 1, Maximum = 100 },
                            Required = false
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Orders retrieved successfully",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Reference = new OpenApiReference { Id = "OrderListResponse", Type = ReferenceType.Schema }
                                    }
                                }
                            }
                        }
                    }
                },
                [OperationType.Post] = new OpenApiOperation
                {
                    Summary = "Creates an order",
                    OperationId = "CreateOrder",
                    RequestBody = new OpenApiRequestBody
                    {
                        Required = true,
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema { Reference = new OpenApiReference { Id = "CreateOrderRequest", Type = ReferenceType.Schema } }
                            }
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["201"] = new OpenApiResponse
                        {
                            Description = "Order created",
                            Headers = new Dictionary<string, OpenApiHeader>
                            {
                                ["Location"] = new OpenApiHeader
                                {
                                    Description = "Resource location",
                                    Schema = new OpenApiSchema { Type = "string", Format = "uri" }
                                }
                            },
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType { Schema = new OpenApiSchema { Reference = new OpenApiReference { Id = "Order", Type = ReferenceType.Schema } } }
                            }
                        },
                        ["400"] = CreateProblemResponse("Invalid request payload"),
                        ["422"] = new OpenApiResponse
                        {
                            Description = "Validation problem",
                            Content = CreateProblemContent()
                        }
                    }
                }
            }
        };

        document.Paths["/orders/{orderId}"] = new OpenApiPathItem
        {
            Parameters = new List<OpenApiParameter>
            {
                new()
                {
                    Name = "orderId",
                    In = ParameterLocation.Path,
                    Required = true,
                    Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
                }
            },
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Get] = new OpenApiOperation
                {
                    Summary = "Gets an order",
                    OperationId = "GetOrder",
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Order retrieved",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType { Schema = new OpenApiSchema { Reference = new OpenApiReference { Id = "Order", Type = ReferenceType.Schema } } }
                            }
                        },
                        ["404"] = CreateProblemResponse("Order not found")
                    }
                },
                [OperationType.Delete] = new OpenApiOperation
                {
                    Summary = "Deletes an order",
                    OperationId = "DeleteOrder",
                    Responses = new OpenApiResponses
                    {
                        ["204"] = new OpenApiResponse { Description = "Order deleted" },
                        ["404"] = CreateProblemResponse("Order not found")
                    }
                }
            }
        };

        document.Paths["/orders/{orderId}/status"] = new OpenApiPathItem
        {
            Parameters = new List<OpenApiParameter>
            {
                new()
                {
                    Name = "orderId",
                    In = ParameterLocation.Path,
                    Required = true,
                    Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
                }
            },
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Put] = new OpenApiOperation
                {
                    Summary = "Updates order status",
                    OperationId = "UpdateOrderStatus",
                    RequestBody = new OpenApiRequestBody
                    {
                        Required = true,
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType { Schema = new OpenApiSchema { Reference = new OpenApiReference { Id = "UpdateOrderStatusRequest", Type = ReferenceType.Schema } } }
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Order updated",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType { Schema = new OpenApiSchema { Reference = new OpenApiReference { Id = "Order", Type = ReferenceType.Schema } } }
                            }
                        },
                        ["404"] = CreateProblemResponse("Order not found"),
                        ["409"] = CreateProblemResponse("Invalid status transition"),
                        ["400"] = CreateProblemResponse("Invalid payload")
                    }
                }
            }
        };

        document.Paths["/orders/{orderId}/metadata"] = new OpenApiPathItem
        {
            Parameters = new List<OpenApiParameter>
            {
                new()
                {
                    Name = "orderId",
                    In = ParameterLocation.Path,
                    Required = true,
                    Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
                }
            },
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Patch] = new OpenApiOperation
                {
                    Summary = "Applies metadata changes",
                    OperationId = "PatchOrderMetadata",
                    RequestBody = new OpenApiRequestBody
                    {
                        Required = true,
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType { Schema = new OpenApiSchema { Reference = new OpenApiReference { Id = "OrderMetadataPatch", Type = ReferenceType.Schema } } }
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Order updated",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType { Schema = new OpenApiSchema { Reference = new OpenApiReference { Id = "Order", Type = ReferenceType.Schema } } }
                            }
                        },
                        ["404"] = CreateProblemResponse("Order not found"),
                        ["400"] = CreateProblemResponse("Invalid metadata patch")
                    }
                }
            }
        };
    }

    private static OpenApiResponse CreateProblemResponse(string description)
    {
        return new OpenApiResponse
        {
            Description = description,
            Content = CreateProblemContent()
        };
    }

    private static Dictionary<string, OpenApiMediaType> CreateProblemContent()
    {
        return new Dictionary<string, OpenApiMediaType>
        {
            ["application/problem+json"] = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>
                    {
                        ["type"] = new OpenApiSchema { Type = "string", Nullable = true },
                        ["title"] = new OpenApiSchema { Type = "string" },
                        ["status"] = new OpenApiSchema { Type = "integer", Format = "int32" },
                        ["detail"] = new OpenApiSchema { Type = "string", Nullable = true },
                        ["instance"] = new OpenApiSchema { Type = "string", Nullable = true }
                    }
                }
            }
        };
    }

    private static void MapOrderIntegrationEndpoints(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/orders").WithTags("Orders");

        group.MapGet(string.Empty, ([AsParameters] OrderQueryOptions query, OrderRepository repository) =>
        {
            ArgumentNullException.ThrowIfNull(repository);
            var orders = repository.List(query.Status, query.Limit);
            var response = new OrderListResponse
            {
                StatusFilter = query.Status,
                Total = orders.Count,
                Items = orders
            };

            return Results.Ok(response);
        })
        .WithName("ListOrders")
        .Produces<OrderListResponse>(StatusCodes.Status200OK);

        group.MapGet("/{orderId:guid}", (Guid orderId, OrderRepository repository) =>
        {
            ArgumentNullException.ThrowIfNull(repository);
            return repository.TryGet(orderId, out var order)
                ? Results.Ok(order)
                : Results.NotFound();
        })
        .WithName("GetOrder")
        .Produces<Order>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapPost(string.Empty, (CreateOrderRequest request, OrderRepository repository) =>
        {
            ArgumentNullException.ThrowIfNull(repository);

            if (request is null)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid order payload",
                    Detail = "Request body is required.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var validationErrors = OrderValidator.Validate(request);
            if (validationErrors.Count > 0)
            {
                return Results.ValidationProblem(validationErrors);
            }

            var order = repository.Add(OrderFactory.Create(request));
            var location = $"/orders/{order.Id}";
            return Results.Created(location, order);
        })
        .WithName("CreateOrder")
        .Produces<Order>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/{orderId:guid}/status", (Guid orderId, UpdateOrderStatusRequest request, OrderRepository repository) =>
        {
            ArgumentNullException.ThrowIfNull(repository);

            if (request is null)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid status payload",
                    Detail = "Request body is required.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var outcome = repository.TryUpdateStatus(orderId, request.Status, request.Reason, request.CapturePayment ?? false, out var updated, out var error);

            return outcome switch
            {
                OrderMutationOutcome.NotFound => Results.NotFound(),
                OrderMutationOutcome.ValidationFailed => Results.Problem(
                    title: "Invalid status transition",
                    detail: error,
                    statusCode: StatusCodes.Status409Conflict,
                    type: "https://httpstatuses.io/409"),
                _ => Results.Ok(updated)
            };
        })
        .WithName("UpdateOrderStatus")
        .Produces<Order>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapMethods("/{orderId:guid}/metadata", new[] { HttpMethods.Patch }, (Guid orderId, OrderMetadataPatch patch, OrderRepository repository) =>
        {
            ArgumentNullException.ThrowIfNull(repository);

            if (patch?.Changes is null || patch.Changes.Count == 0)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid metadata patch",
                    Detail = "Specify at least one metadata entry to add or remove.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var outcome = repository.ApplyMetadataPatch(orderId, patch.Changes, out var updated);
            return outcome switch
            {
                OrderMutationOutcome.NotFound => Results.NotFound(),
                _ => Results.Ok(updated)
            };
        })
        .WithName("PatchOrderMetadata")
        .Produces<Order>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapDelete("/{orderId:guid}", (Guid orderId, OrderRepository repository) =>
        {
            ArgumentNullException.ThrowIfNull(repository);
            return repository.TryDelete(orderId)
                ? Results.NoContent()
                : Results.NotFound();
        })
        .WithName("DeleteOrder")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }

    private static void SeedOrderData(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        using var scope = app.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<OrderRepository>();
        if (!repository.Any())
        {
            repository.Seed(OrderSeedData.CreateDefaults());
        }
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

        if (double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value) && value > 0)
        {
            return value;
        }

        return 0.5;
    }

    private static OpenApiDocument GenerateDocument(SecureSpecOptions options, string documentName, GenerationSimulation simulation)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentName);

        if (simulation.Limit)
        {
            throw new ResourceLimitExceededException($"Simulated limit for document '{documentName}'");
        }

        if (simulation.Delay > TimeSpan.Zero)
        {
            System.Threading.Thread.Sleep(simulation.Delay);
        }

        byte[]? allocation = null;
        if (simulation.AllocatedBytes > 0)
        {
            var length = (int)Math.Min(simulation.AllocatedBytes, int.MaxValue);
            allocation = GC.AllocateArray<byte>(length, pinned: false);

            // Touch allocated pages to ensure the guard observes memory growth.
            for (var offset = 0; offset < allocation.Length; offset += 4096)
            {
                allocation[offset] = 1;
            }
        }

        try
        {
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

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();

            foreach (var scheme in options.Security.Schemes)
            {
                document.Components.SecuritySchemes[scheme.Key] = scheme.Value;
            }

            AddOrderSchemas(document);
            AddOrderPaths(document);

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
        finally
        {
            if (allocation is not null)
            {
                GC.KeepAlive(allocation);
            }
        }
    }

    private static GenerationSimulation ResolveGenerationSimulation(IQueryCollection query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var limit = query.ContainsKey("simulateLimit");

        var delay = TimeSpan.Zero;
        if (query.TryGetValue("simulateDelayMs", out var delayValues) &&
            double.TryParse(delayValues.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var delayMs) &&
            delayMs > 0)
        {
            delay = TimeSpan.FromMilliseconds(delayMs);
        }

        long allocatedBytes = 0;
        if (query.TryGetValue("simulateMemoryBytes", out var memoryValues) &&
            long.TryParse(memoryValues.ToString(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var memoryBytes) &&
            memoryBytes > 0)
        {
            allocatedBytes = memoryBytes;
        }

        return new GenerationSimulation(limit, delay, allocatedBytes);
    }

    private sealed record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary);

    private sealed record GenerationSimulation(bool Limit, TimeSpan Delay, long AllocatedBytes);

    private static int ComputeSecuritySchemeCount(OpenApiDocument document, SecureSpecOptions options)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(options);

        var schemeNames = new HashSet<string>(StringComparer.Ordinal);

        AddComponentSchemes(document, schemeNames);
        AddRequirementSchemes(document, schemeNames);
        AddConfiguredSchemesIfEmpty(options, schemeNames);

        return schemeNames.Count;
    }

    private static void AddComponentSchemes(OpenApiDocument document, HashSet<string> schemeNames)
    {
        if (document.Components?.SecuritySchemes is not { Count: > 0 })
        {
            return;
        }

        foreach (var key in document.Components.SecuritySchemes.Keys)
        {
            schemeNames.Add(key);
        }
    }

    private static void AddRequirementSchemes(OpenApiDocument document, HashSet<string> schemeNames)
    {
        if (document.SecurityRequirements is not { Count: > 0 })
        {
            return;
        }

        foreach (var requirement in document.SecurityRequirements)
        {
            foreach (var scheme in requirement.Keys)
            {
                var id = scheme.Reference?.Id;
                if (!string.IsNullOrEmpty(id))
                {
                    schemeNames.Add(id);
                }
            }
        }
    }

    private static void AddConfiguredSchemesIfEmpty(SecureSpecOptions options, HashSet<string> schemeNames)
    {
        if (schemeNames.Count > 0 || options.Security.Schemes.Count == 0)
        {
            return;
        }

        foreach (var key in options.Security.Schemes.Keys)
        {
            schemeNames.Add(key);
        }
    }
}
