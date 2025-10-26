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
- ✅ AC 221: Basic auth inference blocked with AUTH001
- ✅ Name sanitization and validation
- ✅ Diagnostic logging

## Dependencies
- Microsoft.OpenApi.Models (existing)
- SecureSpec.AspNetCore.Diagnostics (existing)

## Future Enhancements
This implementation provides a foundation for:
- API Key authentication (header/query)
- OAuth 2.0 flows (Authorization Code with PKCE, Client Credentials)
- Mutual TLS authentication

## References
- [OpenAPI 3.0 Security Scheme Object](https://spec.openapis.org/oas/v3.0.3#security-scheme-object)
- [HTTP Bearer Authentication](https://tools.ietf.org/html/rfc6750)
- SecureSpec.AspNetCore PRD (docs/PRD.md)
