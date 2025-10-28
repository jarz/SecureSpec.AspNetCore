# Security Schemes Implementation

## Overview
This document describes the security scheme implementations for SecureSpec.AspNetCore Phase 2.

## Implemented Security Schemes

### 1. HTTP Bearer (Issue 2.1)
HTTP Bearer token authentication scheme without Basic auth inference.

### 2. API Key Header (Issue 2.2)
API Key authentication via HTTP header parameter.

### 3. API Key Query (Issue 2.2)
API Key authentication via URL query parameter.

### 4. OAuth2 Client Credentials (Issue 2.4)
OAuth2 Client Credentials flow for service-to-service authentication.

### 5. Mutual TLS (Issue 2.5)
Mutual TLS authentication scheme (display only, no certificate upload).

## Features

### Security Scheme Builders
Fluent builder APIs for creating security schemes:

```csharp
// HTTP Bearer
options.Security.AddHttpBearer("bearerAuth", builder => builder
    .WithDescription("JWT Bearer token authentication")
    .WithBearerFormat("JWT"));

// API Key Header
options.Security.AddApiKeyHeader("apiKeyHeader", builder => builder
    .WithName("X-API-Key")
    .WithDescription("API Key authentication via header"));

// API Key Query
options.Security.AddApiKeyQuery("apiKeyQuery", builder => builder
    .WithName("api_key")
    .WithDescription("API Key authentication via query parameter"));

// OAuth2 Client Credentials
options.Security.AddOAuth2ClientCredentials("oauth2", builder => builder
    .WithTokenUrl(new Uri("https://auth.example.com/token"))
    .WithDescription("OAuth2 Client Credentials flow")
    .AddScope("api", "Full API access"));

// Mutual TLS
options.Security.AddMutualTls("mutualTLS", builder => builder
    .WithDescription("Mutual TLS authentication. Certificates configured externally."));
```

### Name Sanitization
Comprehensive security sanitization for header and parameter names:
- **CRLF Protection**: Removes `\r` and `\n` to prevent header injection attacks
- **Control Character Removal**: Filters all control characters
- **Whitespace Removal**: Strips whitespace to prevent manipulation
- **Unicode Normalization**: Uses FormC normalization to prevent homograph attacks

### AUTH001 Diagnostic
Explicit diagnostic when Basic authentication inference is attempted:
- Code: `AUTH001`
- Level: Warning
- Message: "Basic auth inference blocked. Define security schemes explicitly..."

## Usage

### HTTP Bearer Authentication

#### Basic Configuration
```csharp
builder.Services.AddSecureSpec(options =>
{
    options.Security.AddHttpBearer("bearerAuth");
});
```

#### Full Configuration
```csharp
builder.Services.AddSecureSpec(options =>
{
    options.Security.AddHttpBearer("bearerAuth", builder => builder
        .WithDescription("JWT Bearer token authentication")
        .WithBearerFormat("JWT"));
});
```

### API Key Header Authentication

#### Basic Configuration
```csharp
builder.Services.AddSecureSpec(options =>
{
    options.Security.AddApiKeyHeader("apiKeyHeader");
});
```

#### Full Configuration
```csharp
builder.Services.AddSecureSpec(options =>
{
    options.Security.AddApiKeyHeader("apiKeyHeader", builder => builder
        .WithName("X-API-Key")
        .WithDescription("API Key authentication via header"));
});
```

### API Key Query Authentication

#### Basic Configuration
```csharp
builder.Services.AddSecureSpec(options =>
{
    options.Security.AddApiKeyQuery("apiKeyQuery");
});
```

#### Full Configuration
```csharp
builder.Services.AddSecureSpec(options =>
{
    options.Security.AddApiKeyQuery("apiKeyQuery", builder => builder
        .WithName("api_key")
        .WithDescription("API Key authentication via query parameter"));
});
```

### Multiple Schemes
```csharp
builder.Services.AddSecureSpec(options =>
{
    options.Security.AddHttpBearer("bearerAuth", builder => builder
        .WithBearerFormat("JWT"));

    options.Security.AddApiKeyHeader("headerAuth", builder => builder
        .WithName("X-API-Key"));

    options.Security.AddApiKeyQuery("queryAuth", builder => builder
        .WithName("api_key"));
});
```

## OpenAPI Output

### HTTP Bearer
The HTTP Bearer security scheme is registered in the OpenAPI document's components:

```json
{
  "components": {
    "securitySchemes": {
      "bearerAuth": {
        "type": "http",
        "scheme": "bearer",
        "bearerFormat": "JWT",
        "description": "JWT Bearer token authentication"
      }
    }
  }
}
```

### API Key Header
The API Key header security scheme is registered in the OpenAPI document's components:

```json
{
  "components": {
    "securitySchemes": {
      "apiKeyHeader": {
        "type": "apiKey",
        "in": "header",
        "name": "X-API-Key",
        "description": "API Key authentication via header"
      }
    }
  }
}
```

### API Key Query
The API Key query security scheme is registered in the OpenAPI document's components:

```json
{
  "components": {
    "securitySchemes": {
      "apiKeyQuery": {
        "type": "apiKey",
        "in": "query",
        "name": "api_key",
        "description": "API Key authentication via query parameter"
      }
    }
  }
}
```

## Security Considerations

### CRLF Injection Protection
Header and parameter names are sanitized to prevent CRLF injection:
```csharp
// Input: "Auth\r\nMalicious: Header"
// Output: "AuthMalicious:Header"
```

### Unicode Normalization
Unicode is normalized to FormC to prevent homograph attacks:
```csharp
// Both inputs normalize to the same output:
// "Authorizatión" (composed)
// "Authorizatio\u0301n" (decomposed)
```

### Name Sanitization Examples
```csharp
// API Key Header with sanitization
options.Security.AddApiKeyHeader("apiKey", builder => builder
    .WithName("X-API\r\n-Key")); // CRLF removed
// Result: Name = "X-API-Key"

// API Key Query with whitespace
options.Security.AddApiKeyQuery("apiKey", builder => builder
    .WithName("api key")); // Whitespace removed
// Result: Name = "apikey"
```

## Architecture

### Class Hierarchy
```
SecuritySchemeBuilder (abstract)
├── HttpBearerSchemeBuilder (concrete)
├── ApiKeyHeaderSchemeBuilder (concrete)
└── ApiKeyQuerySchemeBuilder (concrete)
```

### Key Methods

#### SecurityOptions
- `AddHttpBearer(string name, Action<HttpBearerSchemeBuilder>? configure)`: Register HTTP Bearer scheme
- `AddApiKeyHeader(string name, Action<ApiKeyHeaderSchemeBuilder>? configure)`: Register API Key header scheme
- `AddApiKeyQuery(string name, Action<ApiKeyQuerySchemeBuilder>? configure)`: Register API Key query scheme
- `AddOAuth2ClientCredentials(string name, Action<OAuth2ClientCredentialsSchemeBuilder> configure)`: Register OAuth2 Client Credentials scheme
- `AddMutualTls(string name, Action<MutualTlsSchemeBuilder>? configure)`: Register Mutual TLS scheme
- `BlockBasicAuthInference()`: Emit AUTH001 diagnostic

#### HttpBearerSchemeBuilder
- `WithDescription(string description)`: Set description
- `WithBearerFormat(string format)`: Set bearer format

#### ApiKeyHeaderSchemeBuilder
- `WithName(string name)`: Set header parameter name (sanitized)
- `WithDescription(string description)`: Set description

#### ApiKeyQuerySchemeBuilder
- `WithName(string name)`: Set query parameter name (sanitized)
- `WithDescription(string description)`: Set description

#### OAuth2ClientCredentialsSchemeBuilder
- `WithTokenUrl(Uri tokenUrl)`: Set token endpoint URL (required)
- `WithRefreshUrl(Uri refreshUrl)`: Set refresh endpoint URL (optional)
- `AddScope(string name, string description)`: Add OAuth2 scope
- `WithDescription(string description)`: Set description

#### MutualTlsSchemeBuilder
- `WithDescription(string description)`: Set description

## Testing
Comprehensive test coverage includes:
- Unit tests for all builder functionality
- Header and parameter name sanitization tests
- AUTH001 diagnostic tests
- Integration tests with service registration
- Multiple security schemes tests

## Acceptance Criteria
- ✅ AC 189-195: HTTP Bearer implementation
- ✅ AC 196-198: API Key header and query implementation
- ✅ AC 209-213: OAuth2 Client Credentials flow implementation
- ✅ AC 214-216: Mutual TLS display implementation
- ✅ AC 221: Basic auth inference blocked with AUTH001
- ✅ Name sanitization and validation
- ✅ Diagnostic logging
- ✅ External certificate management documentation (Mutual TLS)

## Dependencies
- Microsoft.OpenApi.Models (existing, v1.6.22)
- Microsoft.OpenApi.Any (existing, for vendor extensions)
- SecureSpec.AspNetCore.Diagnostics (existing)

## Limitations

### Microsoft.OpenApi Library
The current version of Microsoft.OpenApi (1.6.22) does not include native support for the `mutualTLS` SecuritySchemeType defined in OpenAPI 3.1. The Mutual TLS implementation uses a workaround:
- Uses `OpenIdConnect` as a placeholder type
- Adds vendor extension `x-security-scheme-type: mutualTLS` to indicate the true type
- When Microsoft.OpenApi adds native support, the implementation can be updated to use the native type

### Certificate Management
The Mutual TLS implementation provides **documentation only**:
- No certificate upload functionality
- No certificate storage or processing
- Certificates must be configured externally at the infrastructure level
- See docs/MUTUAL_TLS_GUIDE.md for detailed guidance

## Future Enhancements
This implementation provides a foundation for:
- OAuth 2.0 Authorization Code flow with PKCE
- Additional OAuth2 flows
- OpenID Connect authentication

## References
- [OpenAPI 3.0 Security Scheme Object](https://spec.openapis.org/oas/v3.0.3#security-scheme-object)
- [OpenAPI 3.1 Security Scheme Object](https://spec.openapis.org/oas/v3.1.0#security-scheme-object)
- [HTTP Bearer Authentication](https://tools.ietf.org/html/rfc6750)
- [Mutual TLS in OpenAPI](https://www.speakeasy.com/openapi/security/security-schemes/security-mutualtls)
- SecureSpec.AspNetCore PRD (docs/PRD.md)
- Mutual TLS Guide (docs/MUTUAL_TLS_GUIDE.md)
