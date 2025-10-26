# HTTP Bearer Security Scheme Implementation

## Overview
This document describes the HTTP Bearer token authentication scheme implementation for SecureSpec.AspNetCore Phase 2, Issue 2.1.

## Features Implemented

### 1. Security Scheme Builder
A fluent builder API for creating HTTP Bearer authentication schemes:

```csharp
options.Security.AddHttpBearer("bearerAuth", builder => builder
    .WithDescription("JWT Bearer token authentication")
    .WithBearerFormat("JWT"));
```

### 2. Header Sanitization
Comprehensive security sanitization for header names:
- **CRLF Protection**: Removes `\r` and `\n` to prevent header injection attacks
- **Control Character Removal**: Filters all control characters
- **Whitespace Removal**: Strips whitespace to prevent manipulation
- **Unicode Normalization**: Uses FormC normalization to prevent homograph attacks

### 3. AUTH001 Diagnostic
Explicit diagnostic when Basic authentication inference is attempted:
- Code: `AUTH001`
- Level: Warning
- Message: "Basic auth inference blocked. Define security schemes explicitly..."

## Usage

### Basic Configuration
```csharp
builder.Services.AddSecureSpec(options =>
{
    options.Security.AddHttpBearer("bearerAuth");
});
```

### Full Configuration
```csharp
builder.Services.AddSecureSpec(options =>
{
    options.Security.AddHttpBearer("bearerAuth", builder => builder
        .WithDescription("JWT Bearer token authentication")
        .WithBearerFormat("JWT"));
});
```

### Multiple Schemes
```csharp
builder.Services.AddSecureSpec(options =>
{
    options.Security.AddHttpBearer("jwtAuth", builder => builder
        .WithBearerFormat("JWT"));
    
    options.Security.AddHttpBearer("opaqueAuth", builder => builder
        .WithBearerFormat("Opaque"));
});
```

## OpenAPI Output
The security scheme is registered in the OpenAPI document's components:

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

## Security Considerations

### CRLF Injection Protection
Header names are sanitized to prevent CRLF injection:
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

## Architecture

### Class Hierarchy
```
SecuritySchemeBuilder (abstract)
└── HttpBearerSchemeBuilder (concrete)
```

### Key Methods
- `AddHttpBearer(string name, Action<HttpBearerSchemeBuilder>? configure)`: Register scheme
- `WithDescription(string description)`: Set description
- `WithBearerFormat(string format)`: Set bearer format
- `BlockBasicAuthInference()`: Emit AUTH001 diagnostic

## Testing
Comprehensive test coverage includes:
- Unit tests for builder functionality
- Header sanitization tests
- AUTH001 diagnostic tests
- Integration tests for real-world scenarios

## Acceptance Criteria
- ✅ AC 189-195: HTTP Bearer implementation
- ✅ AC 221: Basic auth inference blocked with AUTH001
- ✅ Header sanitization

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
