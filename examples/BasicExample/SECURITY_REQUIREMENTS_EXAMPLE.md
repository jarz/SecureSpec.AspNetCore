# Security Requirements Example

This document demonstrates how to configure security requirements using the SecurityRequirementBuilder in SecureSpec.AspNetCore.

## Understanding AND/OR Semantics

OpenAPI security requirements follow these rules:

- **AND within a single requirement**: All schemes in one requirement object must be satisfied
- **OR across requirements**: Any one of the requirement objects can be satisfied

## Example 1: Simple OR (Global Security)

Most common pattern: Accept either Bearer token OR API Key.

```csharp
options.Documents.Add("v1", doc =>
{
    doc.Info.Title = "My API";
    doc.Info.Version = "1.0";

    // Global security: Bearer OR API Key
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
```

**Result**: Clients can authenticate with either a Bearer token OR an API key.

## Example 2: Composite Authentication (AND)

Require multiple authentication methods simultaneously.

```csharp
// Require BOTH API Key (identifies the app) AND OAuth2 (identifies the user)
doc.SecurityRequirements = new List<OpenApiSecurityRequirement>
{
    new SecurityRequirementBuilder()
        .AddScheme("appKey")          // AND
        .AddScheme("oauth2", "read")  // both required
        .Build()
};
```

**Result**: Clients must provide both an API key AND an OAuth2 token with "read" scope.

## Example 3: Complex Mixed Scenario

Real-world scenario with multiple authentication options.

```csharp
doc.SecurityRequirements = new List<OpenApiSecurityRequirement>
{
    // Option 1: Standard user authentication
    new SecurityRequirementBuilder()
        .AddScheme("bearerAuth")
        .Build(),

    // Option 2: Service-to-service (API Key AND OAuth2)
    new SecurityRequirementBuilder()
        .AddScheme("serviceKey")
        .AddScheme("oauth2ClientCreds", "service:call")
        .Build(),

    // Option 3: Internal network (Mutual TLS only)
    new SecurityRequirementBuilder()
        .AddScheme("mutualTLS")
        .Build()
};
```

**Result**: Clients can authenticate using:
- Bearer token, OR
- Service key + OAuth2 client credentials (both), OR
- Mutual TLS certificate

## Example 4: Operation-Level Override

Override global security for specific operations.

```csharp
// In a filter or when manually creating operations
app.MapGet("/public", () => "Public data")
    .WithMetadata(new OpenApiOperation
    {
        OperationId = "getPublicData",
        // Empty array = no authentication required
        Security = new List<OpenApiSecurityRequirement>()
    });

app.MapGet("/admin", () => "Admin data")
    .WithMetadata(new OpenApiOperation
    {
        OperationId = "getAdminData",
        // Override: requires different security than global
        Security = new List<OpenApiSecurityRequirement>
        {
            new SecurityRequirementBuilder()
                .AddScheme("adminKey")
                .Build()
        }
    });
```

## Example 5: OAuth2 with Scopes

Specify required scopes for OAuth2 authentication.

```csharp
// Different endpoints with different scope requirements
var readOperation = new OpenApiOperation
{
    Security = new List<OpenApiSecurityRequirement>
    {
        new SecurityRequirementBuilder()
            .AddScheme("oauth2", "read:data")
            .Build()
    }
};

var writeOperation = new OpenApiOperation
{
    Security = new List<OpenApiSecurityRequirement>
    {
        new SecurityRequirementBuilder()
            .AddScheme("oauth2", "read:data", "write:data")
            .Build()
    }
};

var adminOperation = new OpenApiOperation
{
    Security = new List<OpenApiSecurityRequirement>
    {
        new SecurityRequirementBuilder()
            .AddScheme("oauth2", "read:data", "write:data", "admin:all")
            .Build()
    }
};
```

## Example 6: Tiered Access with OR

Provide different authentication options for different access levels.

```csharp
doc.SecurityRequirements = new List<OpenApiSecurityRequirement>
{
    // Basic users: read-only scope
    new SecurityRequirementBuilder()
        .AddScheme("oauth2", "read:data")
        .Build(),

    // Power users: read and write scopes
    new SecurityRequirementBuilder()
        .AddScheme("oauth2", "read:data", "write:data")
        .Build(),

    // Admins: all scopes
    new SecurityRequirementBuilder()
        .AddScheme("oauth2", "read:data", "write:data", "admin:all")
        .Build()
};
```

## Example 7: Flexible Multi-Factor

Support different multi-factor authentication combinations.

```csharp
doc.SecurityRequirements = new List<OpenApiSecurityRequirement>
{
    // Option 1: Password + TOTP
    new SecurityRequirementBuilder()
        .AddScheme("basicAuth")
        .AddScheme("totpToken")
        .Build(),

    // Option 2: Certificate alone (high assurance)
    new SecurityRequirementBuilder()
        .AddScheme("mutualTLS")
        .Build(),

    // Option 3: API Key + IP Whitelist
    new SecurityRequirementBuilder()
        .AddScheme("apiKey")
        .AddScheme("ipWhitelist")
        .Build()
};
```

## Complete Working Example

Here's a complete, runnable example:

```csharp
using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore;
using SecureSpec.AspNetCore.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSecureSpec(options =>
{
    // Define all security schemes
    options.Security.AddHttpBearer("userAuth", b =>
        b.WithBearerFormat("JWT")
         .WithDescription("User authentication token"));

    options.Security.AddApiKeyHeader("appKey", b =>
        b.WithName("X-App-Key")
         .WithDescription("Application API key"));

    options.Security.AddOAuth2ClientCredentials("oauth2", b =>
    {
        b.WithTokenUrl(new Uri("https://auth.example.com/token"));
        b.AddScope("api:read", "Read API data");
        b.AddScope("api:write", "Write API data");
        b.AddScope("api:admin", "Admin operations");
    });

    options.Documents.Add("v1", doc =>
    {
        doc.Info.Title = "Multi-Auth API";
        doc.Info.Version = "1.0";

        // Global: Accept user token OR app key
        doc.SecurityRequirements = new List<OpenApiSecurityRequirement>
        {
            new SecurityRequirementBuilder()
                .AddScheme("userAuth")
                .Build(),
            new SecurityRequirementBuilder()
                .AddScheme("appKey")
                .Build()
        };
    });
});

var app = builder.Build();

app.UseSecureSpecUI();

// Public endpoint - no auth
app.MapGet("/public", () => "Public data")
    .WithOpenApi(op =>
    {
        op.Security = new List<OpenApiSecurityRequirement>();
        return op;
    });

// Standard endpoint - uses global security
app.MapGet("/data", () => "Protected data");

// Admin endpoint - requires specific auth
app.MapGet("/admin", () => "Admin data")
    .WithOpenApi(op =>
    {
        op.Security = new List<OpenApiSecurityRequirement>
        {
            new SecurityRequirementBuilder()
                .AddScheme("appKey")
                .AddScheme("oauth2", "api:admin")
                .Build()
        };
        return op;
    });

app.Run();
```

## Best Practices

1. **Keep it Simple**: Start with simple OR alternatives, add complexity only when needed
2. **Document Your Strategy**: Always document why you need specific security combinations
3. **Use Global Defaults**: Set sensible global security, override only when necessary
4. **Validate References**: Always ensure security schemes are defined before referencing them
5. **Consider Empty Arrays**: Use `Security = new List<OpenApiSecurityRequirement>()` for truly public endpoints

## Common Pitfalls

1. **Null vs Empty**: `null` means "use global", `[]` means "no auth required"
2. **Scope Requirements**: Only OAuth2 and OpenID Connect use scopes; for other schemes (API Key, HTTP Bearer, Mutual TLS), omit the scopes parameter entirely
3. **Operation Override**: Operation-level security completely replaces global (no merging)

## Additional Resources

- [SECURITY_REQUIREMENTS.md](../../docs/SECURITY_REQUIREMENTS.md) - Comprehensive documentation
- [OpenAPI 3.0 Security](https://spec.openapis.org/oas/v3.0.3#security-requirement-object)
- [OpenAPI 3.1 Security](https://spec.openapis.org/oas/v3.1.0#security-requirement-object)
