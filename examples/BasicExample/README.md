# BasicExample - SecureSpec.AspNetCore

This is a minimal example demonstrating how to integrate SecureSpec.AspNetCore into an ASP.NET Core Web API application.

## What This Example Shows

- How to add SecureSpec to your service collection
- Basic configuration of OpenAPI documents
- Schema generation options
- Security scheme configuration (HTTP Bearer, API Key, OAuth2)
- UI configuration
- Asset caching with integrity revalidation

## Running the Example

```bash
dotnet run
```

The application will start and be available at `https://localhost:5001` (or the port shown in the console).

## Code Highlights

### Service Registration

```csharp
builder.Services.AddSecureSpec(options =>
{
    options.Documents.Add("v1", doc =>
    {
        doc.Info.Title = "Weather API";
        doc.Info.Version = "1.0.0";
        doc.Info.Description = "A simple weather forecast API";
    });

    options.Schema.MaxDepth = 32;
    options.Schema.UseEnumStrings = true;

    // Security schemes
    options.Security.AddHttpBearer("bearerAuth", builder =>
        builder.WithDescription("JWT Bearer token authentication")
               .WithBearerFormat("JWT"));

    options.Security.AddOAuth2ClientCredentials("oauth2", builder => builder
        .WithTokenUrl(new Uri("https://auth.example.com/token", UriKind.Absolute))
        .WithDescription("OAuth2 Client Credentials authentication")
        .AddScope("api", "Full API access"));

    options.UI.DeepLinking = true;
    options.UI.DisplayOperationId = true;

    // Configure asset caching
    options.UI.Assets.CacheLifetimeSeconds = 3600; // 1 hour
    options.UI.Assets.EnableIntegrityRevalidation = true;
});
```

This registers SecureSpec with:
- A single OpenAPI document named "v1"
- Basic schema generation settings
- Security schemes (HTTP Bearer, API Key, OAuth2)
- UI configuration for deep linking and operation ID display
- Asset caching with 1-hour lifetime and integrity revalidation

### Middleware Configuration

```csharp
var app = builder.Build();

// Enable asset caching middleware for UI assets
app.UseSecureSpecAssetCache();

app.UseHttpsRedirection();
```

The `UseSecureSpecAssetCache()` middleware:
- Adds Cache-Control headers to UI assets (JS, CSS, HTML, etc.)
- Generates ETags based on SHA256 content hashes
- Supports conditional requests with If-None-Match
- Enables post-expiry integrity verification

## Next Steps

Once the full implementation is complete, you'll be able to:
- Access the OpenAPI JSON at `/swagger/v1/swagger.json`
- Access the interactive UI at `/swagger`
- Try out API endpoints directly from the browser

## Learn More

- [Getting Started Guide](../../GETTING_STARTED.md)
- [Main Documentation](../../docs/README.md)
- [Architecture Overview](../../ARCHITECTURE.md)
