using System.Security.Cryptography;
using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore;
using SecureSpec.AspNetCore.Security;
using SecureSpec.AspNetCore.UI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Add SecureSpec for OpenAPI documentation
builder.Services.AddSecureSpec(options =>
{
    options.Documents.Add("v1", doc =>
    {
        doc.Info.Title = "Weather API";
        doc.Info.Version = "1.0.0";
        doc.Info.Description = "A simple weather forecast API demonstrating security requirement AND/OR semantics";

        // Global security: Accept Bearer token OR API Key (OR semantics)
        doc.SecurityRequirements = new List<OpenApiSecurityRequirement>
        {
            new SecurityRequirementBuilder()
                .AddScheme("bearerAuth")
                .Build(),
            new SecurityRequirementBuilder()
                .AddScheme("apiKeyHeader")
                .Build()
        };
    });

    // Configure schema generation
    options.Schema.MaxDepth = 32;
    options.Schema.UseEnumStrings = true;

    // Load XML documentation files (if generated)
    var xmlFile = Path.Combine(AppContext.BaseDirectory, "BasicExample.xml");
    if (File.Exists(xmlFile))
    {
        options.Schema.XmlDocumentationPaths.Add(xmlFile);
    }

    // Configure security schemes
    options.Security.AddHttpBearer("bearerAuth", builder =>
        builder.WithDescription("JWT Bearer token authentication")
               .WithBearerFormat("JWT"));

    options.Security.AddApiKeyHeader("apiKeyHeader", builder =>
        builder.WithName("X-API-Key")
               .WithDescription("API Key authentication via header"));

    options.Security.AddApiKeyQuery("apiKeyQuery", builder =>
        builder.WithName("api_key")
               .WithDescription("API Key authentication via query parameter"));

    // Configure OAuth2 Client Credentials flow
    options.Security.AddOAuth2ClientCredentials("oauth2", builder => builder
        .WithTokenUrl(new Uri("https://auth.example.com/token", UriKind.Absolute))
        .WithDescription("OAuth2 Client Credentials authentication")
        .AddScope("api", "Full API access")
        .AddScope("read", "Read access to weather data"));

    // Add Mutual TLS for service-to-service authentication
    options.Security.AddMutualTls("mutualTLS", builder =>
        builder.WithDescription("Mutual TLS authentication for secure service-to-service communication. " +
                              "Client certificates must be configured at the infrastructure level (API Gateway, Load Balancer, or web server)."));

    // Configure UI
    options.UI.DocumentTitle = "Weather API Documentation";
    options.UI.DeepLinking = true;
    options.UI.DisplayOperationId = true;
    options.UI.DefaultModelsExpandDepth = 2;
    options.UI.EnableFiltering = true;
    options.UI.EnableTryItOut = true;

    // Configure asset caching with integrity revalidation
    options.UI.Assets.CacheLifetimeSeconds = 3600; // 1 hour
    options.UI.Assets.EnableIntegrityRevalidation = true;
    options.UI.Assets.AllowPublicCache = true;

    // Configure integrity enforcement (SHA256 + SRI)
    options.Integrity.Enabled = true;
    options.Integrity.FailClosed = true;  // Fail-closed for security
    options.Integrity.GenerateSri = true; // Generate SRI attributes

    // Configure serialization with hashing
    options.Serialization.GenerateHashes = true;
    options.Serialization.GenerateETags = true;

    // Configure performance and resource guards (AC 319-324)
    options.Performance.EnableResourceGuards = true;
    options.Performance.MaxGenerationTimeMs = 2000; // 2 seconds
    options.Performance.MaxMemoryBytes = 10 * 1024 * 1024; // 10 MB
});

var app = builder.Build();

// Configure the HTTP request pipeline.

// Enable asset caching middleware for UI assets
app.UseSecureSpecAssetCache();

app.UseHttpsRedirection();

// Enable SecureSpec UI at /securespec
app.UseSecureSpecUI();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            RandomNumberGenerator.GetInt32(-20, 55),
            summaries[RandomNumberGenerator.GetInt32(0, summaries.Length)]
        ))
        .ToArray();
    return forecast;
}).WithName("GetWeatherForecast");

await app.RunAsync();

/// <summary>
/// Represents a weather forecast for a specific date.
/// </summary>
/// <param name="Date">The date of the forecast.</param>
/// <param name="TemperatureC">The temperature in Celsius.</param>
/// <param name="Summary">A brief summary of the weather conditions.</param>
sealed record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    /// <summary>
    /// Gets the temperature in Fahrenheit, calculated from Celsius.
    /// </summary>
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
