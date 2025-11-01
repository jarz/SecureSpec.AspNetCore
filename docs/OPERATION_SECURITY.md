# Operation Security Overrides

This document describes how per-operation security requirements work in SecureSpec.AspNetCore, implementing OpenAPI 3.0/3.1 security semantics with deterministic ordering and mutation logging.

## Overview

SecureSpec.AspNetCore provides the `OperationSecurityProcessor` class to handle security requirements at both the global (document) level and per-operation level. This implementation follows OpenAPI specifications while adding deterministic ordering and diagnostic logging for better observability.

## Security Requirement Semantics

### AND/OR Logic

OpenAPI defines two levels of security logic:

1. **AND within a single requirement**: All schemes in one `OpenApiSecurityRequirement` object must be satisfied
2. **OR across multiple requirements**: Only one of the multiple `OpenApiSecurityRequirement` objects needs to be satisfied

```csharp
// Example: API Key OR OAuth2 (OR logic)
var requirements = new List<OpenApiSecurityRequirement>
{
    new SecurityRequirementBuilder()
        .AddScheme("apiKey")
        .Build(),
    new SecurityRequirementBuilder()
        .AddScheme("oauth2", "read", "write")
        .Build()
};

// Example: API Key AND OAuth2 (AND logic)
var requirement = new SecurityRequirementBuilder()
    .AddScheme("apiKey")
    .AddScheme("oauth2", "admin")
    .Build();
```

## Per-Operation Override Behavior

### AC 464: Complete Override (No Merge)

When an operation defines its own security requirements, they **completely override** the global security requirements. There is no merging of global and operation-level security.

```csharp
// Global security: Bearer token
document.SecurityRequirements.Add(
    new SecurityRequirementBuilder()
        .AddScheme("bearerAuth")
        .Build());

// Operation overrides with API Key only (Bearer is NOT required)
operation.Security = new List<OpenApiSecurityRequirement>
{
    new SecurityRequirementBuilder()
        .AddScheme("apiKey")
        .Build()
};
```

### AC 465: Empty Array Makes Endpoint Public

An empty security array at the operation level explicitly clears all global requirements, making the endpoint public (no authentication required).

```csharp
// This endpoint is public - no authentication required
operation.Security = new List<OpenApiSecurityRequirement>();
```

When this occurs and global security exists, a `SEC002` diagnostic is logged:

```
SEC002: Operation 'GetPublicData' cleared global security requirements (empty array)
```

### AC 466: Lexical Ordering Within Requirements

Security schemes within each requirement object are ordered **lexically by their scheme reference ID** for deterministic output. This ensures consistent OpenAPI document generation across runs.

```csharp
// Input (any order)
var requirement = new OpenApiSecurityRequirement
{
    [CreateScheme("oauth2")] = new List<string> { "read" },
    [CreateScheme("apiKey")] = new List<string>(),
    [CreateScheme("bearerAuth")] = new List<string>()
};

// Output (lexically ordered)
// apiKey, bearerAuth, oauth2
```

### AC 467: Preserve Declaration Order of Requirements

While schemes **within** each requirement are ordered lexically, the declaration order of **multiple requirement objects** is preserved. This maintains the intended OR semantics priority.

```csharp
// Declaration order is preserved (OR logic):
// 1. Try option1 first
// 2. Then option2
// 3. Finally option3
operation.Security = new List<OpenApiSecurityRequirement>
{
    CreateSecurityRequirement("option1"),  // First alternative
    CreateSecurityRequirement("option2"),  // Second alternative
    CreateSecurityRequirement("option3")   // Third alternative
};
```

### AC 468: Mutation Logging

When operation security overrides global security, a `SEC002` diagnostic is logged with details:

```json
{
  "code": "SEC002",
  "level": "Info",
  "message": "Operation 'GetUsers' overrode global security requirements",
  "context": {
    "operationId": "GetUsers",
    "globalRequirementsCount": 1,
    "operationRequirementsCount": 2,
    "overrideType": "OperationDefined"
  }
}
```

## Usage

### Basic Usage

```csharp
using SecureSpec.AspNetCore.Security;
using SecureSpec.AspNetCore.Diagnostics;

// Create processor with logger
var logger = new DiagnosticsLogger();
var processor = new OperationSecurityProcessor(logger);

// Define global security
var globalSecurity = new List<OpenApiSecurityRequirement>
{
    new SecurityRequirementBuilder()
        .AddScheme("bearerAuth")
        .Build()
};

// Apply to operation (will inherit from global if operation.Security is null)
processor.ApplySecurityRequirements(operation, globalSecurity, operation.OperationId);
```

### Override Global Security

```csharp
// Override with custom security for this operation
operation.Security = new List<OpenApiSecurityRequirement>
{
    new SecurityRequirementBuilder()
        .AddScheme("apiKey")
        .Build()
};

processor.ApplySecurityRequirements(operation, globalSecurity, operation.OperationId);
// Logs: SEC002 - Operation overrode global security requirements
```

### Make Endpoint Public

```csharp
// Explicitly make endpoint public
operation.Security = new List<OpenApiSecurityRequirement>();

processor.ApplySecurityRequirements(operation, globalSecurity, operation.OperationId);
// Logs: SEC002 - Operation cleared global security requirements (empty array)
```

### Static Ordering Helper

You can also use the static method to order security requirements:

```csharp
var orderedRequirements = OperationSecurityProcessor.OrderSecurityRequirements(globalSecurity);
```

## Diagnostic Code Reference

### SEC002: SecurityRequirementsMutated

**Severity**: Info  
**Description**: Operation security requirements were mutated (overridden from global)  
**Recommended Action**: Review security configuration to ensure intended behavior

**When Logged**:
- Operation defines non-empty security that differs from global
- Operation defines empty security when global security exists

**Not Logged When**:
- Operation inherits from global (no override)
- No global security exists
- Operation security is null (inherits global)

## Best Practices

1. **Document Intent**: Use clear operation IDs that indicate security requirements
2. **Monitor Mutations**: Review SEC002 diagnostics to ensure security overrides are intentional
3. **Explicit Over Implicit**: Use empty arrays `[]` explicitly when making endpoints public
4. **Test Security**: Verify security requirements in integration tests
5. **Consistent Ordering**: Rely on automatic lexical ordering for deterministic output

## Examples

### Multiple Authentication Options (OR)

```csharp
// Allow either Bearer token OR API Key
operation.Security = new List<OpenApiSecurityRequirement>
{
    new SecurityRequirementBuilder()
        .AddScheme("bearerAuth")
        .Build(),
    new SecurityRequirementBuilder()
        .AddScheme("apiKey")
        .Build()
};
```

### Multi-Factor Authentication (AND)

```csharp
// Require both API Key AND OAuth2
operation.Security = new List<OpenApiSecurityRequirement>
{
    new SecurityRequirementBuilder()
        .AddScheme("apiKey")
        .AddScheme("oauth2", "admin")
        .Build()
};
```

### Complex Security Scenario

```csharp
// Option 1: API Key AND OAuth2 (high security)
// Option 2: Mutual TLS only (alternative)
operation.Security = new List<OpenApiSecurityRequirement>
{
    new SecurityRequirementBuilder()
        .AddScheme("apiKey")
        .AddScheme("oauth2", "read", "write")
        .Build(),
    new SecurityRequirementBuilder()
        .AddScheme("mutualTLS")
        .Build()
};
```

## Implementation Notes

- The processor is **thread-safe** when used with a thread-safe logger
- Ordering is performed **in-place** on the operation's security collection
- All mutations trigger diagnostic logging when appropriate
- The implementation follows OpenAPI 3.0 and 3.1 specifications
- Null and empty collections are handled gracefully

## Related Documentation

- [Security Schemes](./SECURITY.md)
- [Diagnostic Codes](./DIAGNOSTICS_USAGE.md)
- [OpenAPI Specification](https://spec.openapis.org/oas/v3.1.0#security-requirement-object)
