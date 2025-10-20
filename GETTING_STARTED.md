# Getting Started with SecureSpec.AspNetCore

This guide shows you how to get started with SecureSpec.AspNetCore in your ASP.NET Core application.

## Installation

**Note:** The package is not yet published to NuGet. For now, you can build from source.

```bash
git clone https://github.com/jarz/SecureSpec.AspNetCore.git
cd SecureSpec.AspNetCore
dotnet build
```

## Quick Start

### 1. Add SecureSpec to your services

In your `Program.cs` (or `Startup.cs` for older ASP.NET Core versions):

```csharp
using SecureSpec.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add controllers or minimal APIs
builder.Services.AddControllers();

// Add SecureSpec
builder.Services.AddSecureSpec(options =>
{
    // Configure at least one OpenAPI document
    options.Documents.Add("v1", doc =>
    {
        doc.Info.Title = "My API";
        doc.Info.Version = "1.0.0";
        doc.Info.Description = "My awesome API description";
        
        // Optionally add servers
        doc.Servers.Add(server =>
        {
            server.Url = "https://api.example.com";
            server.Description = "Production server";
        });
    });

    // Configure schema generation (optional)
    options.Schema.MaxDepth = 32;  // Maximum traversal depth
    options.Schema.UseEnumStrings = true;  // Use strings for enums
    
    // Configure security (optional)
    options.Security.OAuth.AuthorizationCode(oauth =>
    {
        oauth.AuthorizationUrl = "https://auth.example.com/authorize";
        oauth.TokenUrl = "https://auth.example.com/token";
        oauth.Scopes.Add("read", "Read access");
        oauth.Scopes.Add("write", "Write access");
        // Note: PKCE is always required and cannot be disabled
    });
    
    // Configure UI (optional)
    options.UI.DeepLinking = true;
    options.UI.DisplayOperationId = true;
    options.UI.EnableTryItOut = true;
});

var app = builder.Build();

// Map your controllers or endpoints
app.MapControllers();

app.Run();
```

### 2. Configuration Options

#### Document Configuration

You can add multiple OpenAPI documents for API versioning:

```csharp
options.Documents.Add("v1", doc =>
{
    doc.Info.Title = "My API v1";
    doc.Info.Version = "1.0.0";
});

options.Documents.Add("v2", doc =>
{
    doc.Info.Title = "My API v2";
    doc.Info.Version = "2.0.0";
});
```

#### Schema Options

Customize how schemas are generated:

```csharp
// Custom schema ID strategy
options.Schema.IdStrategy = type => $"Custom_{type.Name}";

// Maximum recursion depth
options.Schema.MaxDepth = 32;

// Enum behavior
options.Schema.UseEnumStrings = true;
options.Schema.EnumNamingPolicy = name => name.ToLowerInvariant();

// Custom type mapping
options.Schema.TypeMappings.Map<MyCustomType>(mapping =>
{
    mapping.Type = "string";
    mapping.Format = "custom";
});
```

#### Security Options

Configure OAuth flows (PKCE is always required):

```csharp
// Authorization Code flow with PKCE
options.Security.OAuth.AuthorizationCode(oauth =>
{
    oauth.AuthorizationUrl = "https://auth.example.com/authorize";
    oauth.TokenUrl = "https://auth.example.com/token";
    oauth.RefreshUrl = "https://auth.example.com/refresh";
    oauth.Scopes.Add("api", "Full API access");
});

// Client Credentials flow
options.Security.OAuth.ClientCredentials(oauth =>
{
    oauth.TokenUrl = "https://auth.example.com/token";
    oauth.Scopes.Add("api", "Full API access");
});

// Map policies to scopes
options.Security.PolicyToScope = policy => $"policy:{policy}";
options.Security.RoleToScope = role => $"role:{role}";
```

#### UI Options

Customize the interactive UI:

```csharp
options.UI.DeepLinking = true;
options.UI.DisplayOperationId = false;
options.UI.DefaultModelsExpandDepth = 1;
options.UI.EnableFiltering = true;
options.UI.EnableTryItOut = true;
options.UI.DocumentTitle = "My API Documentation";
```

#### Serialization Options

Configure canonical serialization:

```csharp
// These are secure defaults and should generally not be changed
options.Serialization.DeterministicOrdering = true;  // Required for stable hashes
options.Serialization.GenerateHashes = true;
options.Serialization.GenerateETags = true;
```

#### Diagnostics Options

Configure diagnostics logging:

```csharp
options.Diagnostics.EnableDiagnostics = true;
options.Diagnostics.MaxRetentionCount = 10000;
options.Diagnostics.MaxRetentionHours = 24;
options.Diagnostics.IncludeDetailedErrors = false;  // Only enable in development
```

## What's Implemented

✅ **Configuration API** - Fluent configuration builders for all options
✅ **Project Structure** - Organized namespace structure
✅ **Test Infrastructure** - xUnit test project with initial tests

## What's Next

The following phases are planned (see [ROADMAP.md](docs/ROADMAP.md)):

- **Phase 1**: Core OpenAPI Generation & Schema Fidelity
- **Phase 2**: Security Schemes & OAuth Flows  
- **Phase 3**: UI & Interactive Exploration
- **Phase 4**: Performance, Guards & Virtualization
- **Phase 5**: Diagnostics, Retention & Concurrency
- **Phase 6**: Accessibility, CSP & Final Hardening

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
