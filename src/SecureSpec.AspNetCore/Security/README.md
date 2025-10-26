# Security Scheme Implementations

## Overview
This document describes the security scheme implementations for SecureSpec.AspNetCore Phase 2, including HTTP Bearer (Issue 2.1) and Mutual TLS (Issue 2.5).

## Features Implemented

### HTTP Bearer Authentication (Phase 2, Issue 2.1)

#### 1. Security Scheme Builder
A fluent builder API for creating HTTP Bearer authentication schemes:

```csharp
options.Security.AddHttpBearer("bearerAuth", builder => builder
    .WithDescription("JWT Bearer token authentication")
    .WithBearerFormat("JWT"));
```

#### 2. Header Sanitization
Comprehensive security sanitization for header names:
- **CRLF Protection**: Removes `\r` and `\n` to prevent header injection attacks
- **Control Character Removal**: Filters all control characters
- **Whitespace Removal**: Strips whitespace to prevent manipulation
- **Unicode Normalization**: Uses FormC normalization to prevent homograph attacks

#### 3. AUTH001 Diagnostic
Explicit diagnostic when Basic authentication inference is attempted:
- Code: `AUTH001`
- Level: Warning
- Message: "Basic auth inference blocked. Define security schemes explicitly..."

### Mutual TLS Authentication (Phase 2, Issue 2.5)

#### 1. Security Scheme Builder
A fluent builder API for creating Mutual TLS authentication schemes:

```csharp
options.Security.AddMutualTls("mutualTLS", builder => builder
    .WithDescription("Mutual TLS authentication for secure API communication"));
```

#### 2. Display-Only Implementation
Mutual TLS scheme is for **display purposes only**:
- **No Certificate Upload**: The library does not provide certificate upload functionality
- **External Management**: Client certificates must be configured externally at the infrastructure level
- **Documentation**: Clear guidance that certificates are managed through API Gateway, Load Balancer, or web server

#### 3. Microsoft.OpenApi Limitation Workaround
The current version of Microsoft.OpenApi (1.6.22) does not support the `mutualTLS` SecuritySchemeType from OpenAPI 3.1:
- **Placeholder Type**: Uses `OpenIdConnect` as a placeholder type
- **Vendor Extension**: Adds `x-security-scheme-type: mutualTLS` to indicate the intended security scheme type
- **Future Compatibility**: When Microsoft.OpenApi adds native support, the implementation can be updated

## Usage

### HTTP Bearer - Basic Configuration
```csharp
builder.Services.AddSecureSpec(options =>
{
    options.Security.AddHttpBearer("bearerAuth");
});
```

### HTTP Bearer - Full Configuration
```csharp
builder.Services.AddSecureSpec(options =>
{
    options.Security.AddHttpBearer("bearerAuth", builder => builder
        .WithDescription("JWT Bearer token authentication")
        .WithBearerFormat("JWT"));
});
```

### HTTP Bearer - Multiple Schemes
```csharp
builder.Services.AddSecureSpec(options =>
{
    options.Security.AddHttpBearer("jwtAuth", builder => builder
        .WithBearerFormat("JWT"));
    
    options.Security.AddHttpBearer("opaqueAuth", builder => builder
        .WithBearerFormat("Opaque"));
});
```

### Mutual TLS - Basic Configuration
```csharp
builder.Services.AddSecureSpec(options =>
{
    options.Security.AddMutualTls("mutualTLS");
});
```

### Mutual TLS - Full Configuration
```csharp
builder.Services.AddSecureSpec(options =>
{
    options.Security.AddMutualTls("mutualTLS", builder => builder
        .WithDescription("Mutual TLS authentication. Client certificates must be configured at the infrastructure level (API Gateway, Load Balancer, or web server)."));
});
```

### Mutual TLS - Multiple Schemes
```csharp
builder.Services.AddSecureSpec(options =>
{
    options.Security.AddMutualTls("clientAuth", builder => builder
        .WithDescription("Client certificate authentication"));

    options.Security.AddMutualTls("serviceAuth", builder => builder
        .WithDescription("Service-to-service mTLS authentication"));
});
```

### Mixed Security Schemes
```csharp
builder.Services.AddSecureSpec(options =>
{
    // HTTP Bearer for user authentication
    options.Security.AddHttpBearer("userAuth", builder => builder
        .WithBearerFormat("JWT")
        .WithDescription("User authentication with JWT tokens"));

    // Mutual TLS for service-to-service
    options.Security.AddMutualTls("serviceAuth", builder => builder
        .WithDescription("Service-to-service authentication"));
});
```

## OpenAPI Output

### HTTP Bearer Output
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

### Mutual TLS Output
The Mutual TLS security scheme is registered with a vendor extension to indicate the true type:

```json
{
  "components": {
    "securitySchemes": {
      "mutualTLS": {
        "type": "openIdConnect",
        "description": "Mutual TLS authentication. Client certificates must be configured externally at the TLS layer. Certificate upload is not supported; certificates are managed through infrastructure configuration.",
        "x-security-scheme-type": "mutualTLS"
      }
    }
  }
}
```

**Note**: The `type` is set to `openIdConnect` as a placeholder because Microsoft.OpenApi 1.6.22 doesn't support the `mutualTLS` type. The vendor extension `x-security-scheme-type` indicates the intended security scheme type.

## Security Considerations

### HTTP Bearer - CRLF Injection Protection
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

### Mutual TLS - External Certificate Management
Mutual TLS authentication has specific security considerations:

#### No Certificate Upload
- **Library Responsibility**: This library provides **documentation only** for Mutual TLS
- **No Storage**: The library does not store, process, or upload certificates
- **Infrastructure Layer**: Certificates must be configured at the TLS/SSL layer, typically through:
  - API Gateway (e.g., Azure API Management, AWS API Gateway)
  - Load Balancer (e.g., Azure Application Gateway, AWS ALB)
  - Web Server (e.g., Nginx, IIS)
  - Reverse Proxy (e.g., Envoy, Traefik)

#### Certificate Validation
- **Trust Chain**: Ensure client certificates are issued by a trusted Certificate Authority (CA)
- **Revocation**: Implement certificate revocation checking (CRL or OCSP)
- **Expiration**: Monitor and alert on certificate expiration
- **Subject Validation**: Validate certificate subject/SAN matches expected identity

#### Best Practices
1. **Certificate Rotation**: Implement automated certificate rotation processes
2. **Separate Concerns**: Keep certificate management separate from application code
3. **Monitoring**: Monitor certificate usage and failed authentication attempts
4. **Documentation**: Provide clear documentation for API consumers on how to obtain and configure certificates
5. **Testing**: Use test certificates in non-production environments

## Architecture

### Class Hierarchy
```
SecuritySchemeBuilder (abstract)
├── HttpBearerSchemeBuilder (concrete)
└── MutualTlsSchemeBuilder (concrete)
```

### Key Methods

#### SecurityOptions
- `AddHttpBearer(string name, Action<HttpBearerSchemeBuilder>? configure)`: Register HTTP Bearer scheme
- `AddMutualTls(string name, Action<MutualTlsSchemeBuilder>? configure)`: Register Mutual TLS scheme
- `BlockBasicAuthInference()`: Emit AUTH001 diagnostic

#### HttpBearerSchemeBuilder
- `WithDescription(string description)`: Set description
- `WithBearerFormat(string format)`: Set bearer format

#### MutualTlsSchemeBuilder
- `WithDescription(string description)`: Set description

## Testing
Comprehensive test coverage includes:

### HTTP Bearer Tests
- Unit tests for builder functionality
- Header sanitization tests
- AUTH001 diagnostic tests
- Integration tests for real-world scenarios

### Mutual TLS Tests
- Unit tests for builder functionality
- Vendor extension validation
- External certificate management documentation tests
- Integration tests with multiple schemes
- Mixed security scheme scenarios

## Acceptance Criteria
- ✅ AC 189-195: HTTP Bearer implementation
- ✅ AC 214-216: Mutual TLS display implementation
- ✅ AC 221: Basic auth inference blocked with AUTH001
- ✅ Header sanitization
- ✅ No certificate upload capability (display only)
- ✅ External cert management documentation

## Dependencies
- Microsoft.OpenApi.Models (existing, v1.6.22)
- Microsoft.OpenApi.Any (existing, for vendor extensions)
- SecureSpec.AspNetCore.Diagnostics (existing)

## Limitations

### Microsoft.OpenApi Library
The current version of Microsoft.OpenApi (1.6.22) does not include native support for the `mutualTLS` SecuritySchemeType defined in OpenAPI 3.1. This implementation uses a workaround:
- Uses `OpenIdConnect` as a placeholder type
- Adds vendor extension `x-security-scheme-type: mutualTLS` to indicate the true type
- When Microsoft.OpenApi adds native support, the implementation can be updated to use the native type

### Certificate Management
This library provides **documentation only** for Mutual TLS:
- No certificate upload functionality
- No certificate storage or processing
- Certificates must be configured externally at the infrastructure level
- See "Security Considerations - Mutual TLS" section for best practices

## Future Enhancements
This implementation provides a foundation for:
- API Key authentication (header/query)
- OAuth 2.0 flows (Authorization Code with PKCE, Client Credentials)
- Additional security schemes as defined in OpenAPI specification

## References
- [OpenAPI 3.0 Security Scheme Object](https://spec.openapis.org/oas/v3.0.3#security-scheme-object)
- [OpenAPI 3.1 Security Scheme Object](https://spec.openapis.org/oas/v3.1.0#security-scheme-object)
- [HTTP Bearer Authentication](https://tools.ietf.org/html/rfc6750)
- [Mutual TLS in OpenAPI](https://www.speakeasy.com/openapi/security/security-schemes/security-mutualtls)
- SecureSpec.AspNetCore PRD (docs/PRD.md)
