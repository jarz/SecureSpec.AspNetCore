using System.Security.Cryptography;
using SecureSpec.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Add SecureSpec for OpenAPI documentation
builder.Services.AddSecureSpec(options =>
{
    options.Documents.Add("v1", doc =>
    {
        doc.Info.Title = "Weather API";
        doc.Info.Version = "1.0.0";
        doc.Info.Description = "A simple weather forecast API";
    });

    // Configure schema generation
    options.Schema.MaxDepth = 32;
    options.Schema.UseEnumStrings = true;

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

    // Configure UI
    options.UI.DeepLinking = true;
    options.UI.DisplayOperationId = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

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
});

app.Run();

sealed record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
