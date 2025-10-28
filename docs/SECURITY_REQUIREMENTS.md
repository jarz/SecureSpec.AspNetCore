# Security Requirements: AND/OR Semantics

## Overview

SecureSpec.AspNetCore implements OpenAPI 3.0/3.1 security requirements with proper AND/OR semantics. Understanding these semantics is crucial for correctly configuring authentication and authorization for your API.

## Core Concepts

### 1. Security Requirement Objects

A **Security Requirement Object** is a dictionary that maps security schemes to their required scopes:

```csharp
OpenApiSecurityRequirement
{
    [schemeReference] = ["scope1", "scope2", ...]
}
```

### 2. AND Logic (Within a Single Requirement)

**All** security schemes listed in a single requirement object must be satisfied.

```csharp
// This requirement needs BOTH API Key AND OAuth2
var requirement = new SecurityRequirementBuilder()
    .AddScheme("apiKey")           // AND
    .AddScheme("oauth2", "read")   // must both be satisfied
    .Build();
```

**OpenAPI YAML Representation:**
```yaml
security:
  - apiKey: []
    oauth2: [read]
```

In this example, a client must provide:
- Valid API key credentials, **AND**
- Valid OAuth2 token with `read` scope

### 3. OR Logic (Across Multiple Requirements)

**Any one** of the requirement objects in the security array can satisfy the authentication.

```csharp
// Either API Key OR OAuth2 can be used
var securityRequirements = new List<OpenApiSecurityRequirement>
{
    new SecurityRequirementBuilder()
        .AddScheme("apiKey")
        .Build(),
    new SecurityRequirementBuilder()
        .AddScheme("oauth2", "read", "write")
        .Build()
};
```

**OpenAPI YAML Representation:**
```yaml
security:
  - apiKey: []
  - oauth2: [read, write]
```

In this example, a client can authenticate using:
- Valid API key credentials, **OR**
- Valid OAuth2 token with `read` and `write` scopes

## Common Patterns

### Pattern 1: Single Scheme (Most Common)

Allow authentication with a single scheme.

```csharp
// Global security: HTTP Bearer only
document.SecurityRequirements = new List<OpenApiSecurityRequirement>
{
    new SecurityRequirementBuilder()
        .AddScheme("bearerAuth")
        .Build()
};
```

### Pattern 2: Alternative Schemes (OR)

Allow multiple authentication options.

```csharp
// Accept either Bearer token OR API Key
document.SecurityRequirements = new List<OpenApiSecurityRequirement>
{
    new SecurityRequirementBuilder()
        .AddScheme("bearerAuth")
        .Build(),
    new SecurityRequirementBuilder()
        .AddScheme("apiKeyHeader")
        .Build()
};
```

### Pattern 3: Composite Authentication (AND)

Require multiple authentication mechanisms simultaneously.

```csharp
// Require both API Key AND OAuth2
document.SecurityRequirements = new List<OpenApiSecurityRequirement>
{
    new SecurityRequirementBuilder()
        .AddScheme("apiKey")
        .AddScheme("oauth2", "admin")
        .Build()
};
```

### Pattern 4: Complex Mix (AND within, OR across)

Combine both patterns for complex scenarios.

```csharp
// Option 1: API Key + OAuth2 (both required)
// OR
// Option 2: Mutual TLS alone
document.SecurityRequirements = new List<OpenApiSecurityRequirement>
{
    new SecurityRequirementBuilder()
        .AddScheme("apiKey")
        .AddScheme("oauth2", "read")
        .Build(),
    new SecurityRequirementBuilder()
        .AddScheme("mutualTLS")
        .Build()
};
```

### Pattern 5: Optional Security (Public Endpoint)

Use an empty security array to make an endpoint public.

```csharp
// Override operation-level to make this endpoint public
operation.Security = new List<OpenApiSecurityRequirement>();
```

**IMPORTANT:** An empty array `[]` means "no security required", which is different from `null` (use global security).

## Operation-Level Security Override

Operation-level security **completely replaces** global security (no merging).

```csharp
// Global configuration
builder.Services.AddSecureSpec(options =>
{
    options.Security.AddHttpBearer("bearerAuth");
    
    options.Documents.Add("v1", doc =>
    {
        // Global security requirement
        doc.SecurityRequirements = new List<OpenApiSecurityRequirement>
        {
            new SecurityRequirementBuilder()
                .AddScheme("bearerAuth")
                .Build()
        };
    });
});

// In a filter or operation processor
operation.Security = new List<OpenApiSecurityRequirement>
{
    // This REPLACES global security for this operation only
    new SecurityRequirementBuilder()
        .AddScheme("apiKey")
        .Build()
};
```

## Scopes for OAuth2 and OpenID Connect

For OAuth2 and OpenID Connect schemes, specify required scopes in the security requirement:

```csharp
// OAuth2 with specific scopes
new SecurityRequirementBuilder()
    .AddScheme("oauth2", "read:users", "write:users")
    .Build();
```

For other schemes (API Key, HTTP Bearer, Mutual TLS), use an empty scopes array:

```csharp
// API Key (no scopes)
new SecurityRequirementBuilder()
    .AddScheme("apiKey")
    .Build();
```

## Validation and Best Practices

### 1. Always Reference Defined Schemes

Security requirements must reference schemes defined in `components/securitySchemes`:

```csharp
// Define the scheme first
options.Security.AddHttpBearer("bearerAuth", builder =>
{
    builder.WithDescription("JWT Bearer token authentication");
});

// Then reference it in requirements
new SecurityRequirementBuilder()
    .AddScheme("bearerAuth")  // Must match the name above
    .Build();
```

### 2. Empty vs Null Security

- `null` or not set: Use global security requirements
- Empty array `[]`: No authentication required (public endpoint)

```csharp
// Public endpoint
operation.Security = new List<OpenApiSecurityRequirement>();

// Use global security
operation.Security = null; // or don't set it
```

### 3. Avoid Over-Complication

Keep security requirements simple and easy to understand:

```csharp
// ✅ GOOD: Simple and clear
security:
  - bearerAuth: []

// ❌ AVOID: Unnecessarily complex
security:
  - apiKey: []
    oauth2: [read]
    bearerAuth: []
  - mutualTLS: []
    apiKey: []
```

### 4. Document Your Security Strategy

Always document which pattern you're using and why:

```csharp
// Document why you need composite authentication
// We require both API Key (identifies the application) AND OAuth2 (identifies the user)
new SecurityRequirementBuilder()
    .AddScheme("apiKey")
    .AddScheme("oauth2", "user:read")
    .Build();
```

## Complete Example

```csharp
builder.Services.AddSecureSpec(options =>
{
    // Define security schemes
    options.Security.AddHttpBearer("bearerAuth", b => 
        b.WithBearerFormat("JWT")
         .WithDescription("JWT token authentication"));
    
    options.Security.AddApiKeyHeader("apiKey", b =>
        b.WithHeaderName("X-API-Key")
         .WithDescription("API key in header"));
    
    options.Security.AddOAuth2ClientCredentials("oauth2", b =>
    {
        b.WithTokenUrl(new Uri("https://auth.example.com/token"))
         .AddScope("read", "Read access")
         .AddScope("write", "Write access");
    });

    options.Documents.Add("v1", doc =>
    {
        doc.Info.Title = "My API";
        doc.Info.Version = "1.0";

        // Global security: Accept Bearer OR API Key
        doc.SecurityRequirements = new List<OpenApiSecurityRequirement>
        {
            new SecurityRequirementBuilder()
                .AddScheme("bearerAuth")
                .Build(),
            new SecurityRequirementBuilder()
                .AddScheme("apiKey")
                .Build()
        };
    });
});
```

## Reference

- [OpenAPI 3.0 Specification - Security Requirement Object](https://spec.openapis.org/oas/v3.0.3#security-requirement-object)
- [OpenAPI 3.1 Specification - Security Requirement Object](https://spec.openapis.org/oas/v3.1.0#security-requirement-object)
- [Swagger Documentation - Authentication](https://swagger.io/docs/specification/authentication/)

## See Also

- [SecurityRequirementBuilder API](./SecurityRequirementBuilder.cs)
- [SecurityOptions API](../Configuration/SecurityOptions.cs)
- [Security Scheme Builders](./Builders/)
