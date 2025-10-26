# Integrity Enforcement Example

This example demonstrates SecureSpec's integrity enforcement features:

## Features Demonstrated

### 1. SHA256 Hash Generation
- Deterministic hash generation for OpenAPI documents
- Normalization of line endings (CRLF → LF) before hashing (AC 499)
- UTF-8 encoding

### 2. SRI (Subresource Integrity) Support
- Generation of SRI values in `sha256-{base64}` format
- SRI validation for assets
- Fail-closed mode for security

### 3. SEC001 Critical Diagnostic
- Critical diagnostic code for integrity failures
- Redacted error messages (AC 500):
  - Only partial hashes shown
  - Resource paths redacted as `[REDACTED]`

### 4. Integrity Options Configuration

```csharp
options.Integrity.Enabled = true;          // Enable integrity checking
options.Integrity.FailClosed = true;       // Fail on integrity mismatch
options.Integrity.GenerateSri = true;      // Generate SRI attributes
```

## Usage Examples

### Hash and SRI Generation

```csharp
using SecureSpec.AspNetCore.Serialization;
using SecureSpec.AspNetCore.Security;

// Method 1: Using CanonicalSerializer
var document = new OpenApiDocument { /* ... */ };
var (content, hash, sri) = CanonicalSerializer.SerializeWithIntegrity(document);

Console.WriteLine($"SHA256: {hash}");
Console.WriteLine($"SRI: {sri}");
Console.WriteLine($"ETag: {CanonicalSerializer.GenerateETag(hash)}");

// Method 2: Using IntegrityValidator directly
var validator = new IntegrityValidator();
var content = "Hello, World!";
var hash = validator.ComputeHash(content);
var sri = validator.GenerateSri(content);
```

### Integrity Verification

```csharp
using SecureSpec.AspNetCore.Security;
using SecureSpec.AspNetCore.Diagnostics;

var logger = new DiagnosticsLogger();
var validator = new IntegrityValidator(logger);

// Verify with hex hash
var isValid = validator.VerifyIntegrity(content, expectedHash, "/api/swagger/v1");

// Verify with SRI
var isValidSri = validator.VerifySri(content, "sha256-abc123...", "/assets/script.js");

if (!isValid)
{
    // Check diagnostics for SEC001 critical error
    var events = logger.GetEvents();
    var failures = events.Where(e => e.Code == "SEC001");
    foreach (var failure in failures)
    {
        Console.WriteLine($"Integrity failure: {failure.Message}");
        // Context will show partial hashes and [REDACTED] paths
    }
}
```

## HTML Integration with SRI

When serving static assets, use SRI attributes:

```html
<script src="/swagger-ui.js" 
        integrity="sha256-47DEQpj8HBSa+/TImW+5JCeuQeRkm5NMpJWZG3hSuFU="
        crossorigin="anonymous"></script>

<link rel="stylesheet" 
      href="/swagger-ui.css"
      integrity="sha256-abc123def456..."
      crossorigin="anonymous">
```

## Security Benefits

1. **Tampering Detection**: Detects if documents or assets are modified
2. **Fail-Closed**: Prevents loading of tampered resources when enabled
3. **Deterministic**: Same content always produces same hash/SRI
4. **Audit Trail**: SEC001 diagnostics provide security audit trail
5. **Privacy**: Redacted diagnostics prevent information leakage

## Acceptance Criteria Met

- ✅ **AC 499**: SHA256 hashing performed after normalization (LF, UTF-8)
- ✅ **AC 500**: Integrity mismatch diagnostic redacts path & partial hash only
- ✅ **AC 19-21, 304-306**: Tampering mitigation with SHA256 + SRI
- ✅ **SEC001**: Critical diagnostic for integrity check failures

## Running the Example

```bash
dotnet run
```

Then access:
- OpenAPI specification: https://localhost:5001/openapi/v1
- Swagger UI: https://localhost:5001/swagger
- Weather forecast API: https://localhost:5001/weatherforecast

## Testing Integrity

```bash
# Get the OpenAPI document with hash
curl -v https://localhost:5001/openapi/v1 | tee openapi.json

# Compute hash
cat openapi.json | openssl dgst -sha256

# This should match the ETag header: W/"sha256:{first16hex}"
```
