# Diagnostics System Usage Examples

This document demonstrates how to use the SecureSpec diagnostics system in your application.

## Basic Usage

```csharp
using SecureSpec.AspNetCore.Diagnostics;

// Create a diagnostics logger instance
var logger = new DiagnosticsLogger();

// Log an informational event
logger.LogInfo(
    DiagnosticCodes.SchemaIdCollision,
    "SchemaId collision detected",
    context: new { SchemaId = "Product", NewId = "Product_schemaDup1" });

// Log a warning
logger.LogWarning(
    DiagnosticCodes.DataAnnotationsConflict,
    "DataAnnotations conflict detected");

// Log an error
logger.LogError(
    DiagnosticCodes.NullabilityMismatch,
    "Nullability mismatch in schema generation");

// Log a critical error
logger.LogCritical(
    DiagnosticCodes.IntegrityCheckFailed,
    "Integrity check failed for document");
```

## Using Structured Context

```csharp
// Log rate limit enforcement with structured context
logger.LogInfo(
    DiagnosticCodes.RateLimitEnforced,
    "Rate limit bucket enforced",
    context: new
    {
        Bucket = "TryItOut",
        Remaining = 0,
        WindowSeconds = 60,
        Reason = "limit_exceeded"
    });
```

## Using Sanitization for Sensitive Data

```csharp
// Sanitize sensitive information before logging
var filePath = "/usr/local/secrets/api-keys/production.json";
var hash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

logger.LogCritical(
    DiagnosticCodes.IntegrityCheckFailed,
    "Integrity check failed",
    context: new
    {
        File = DiagnosticsLogger.SanitizePath(filePath),  // "production.json"
        Hash = DiagnosticsLogger.SanitizeHash(hash)        // "e3b0c442..."
    },
    sanitized: true);
```

## Retrieving Diagnostic Events

```csharp
// Get all logged events
var events = logger.GetEvents();

foreach (var evt in events)
{
    Console.WriteLine($"[{evt.Timestamp:O}] {evt.Level} - {evt.Code}: {evt.Message}");
    if (evt.Context != null)
    {
        Console.WriteLine($"  Context: {JsonSerializer.Serialize(evt.Context)}");
    }
    if (evt.Sanitized)
    {
        Console.WriteLine("  (Sanitized)");
    }
}
```

## Event Schema

Each diagnostic event has the following structure:

```json
{
  "timestamp": "2025-10-26T17:27:52.116Z",
  "level": "Info|Warn|Error|Critical",
  "code": "SEC001",
  "message": "Integrity check failed",
  "context": {
    "file": "document.json",
    "hash": "e3b0c442..."
  },
  "sanitized": true
}
```

## Defined Diagnostic Codes

| Category | Code | Description | Severity | Action |
|----------|------|-------------|----------|--------|
| **Security** | SEC001 | Integrity check failed | Critical | Abort load |
| **CSP** | CSP001 | CSP mismatch or missing directives | Error | Review policy |
| **Schema** | SCH001 | SchemaId collision suffix applied | Info | Confirm stability |
| **Schema** | SCH001-DEPTH | Schema generation exceeded maximum depth | Warn | Review schema structure |
| **Annotations** | ANN001 | DataAnnotations conflict (last wins) | Warn | Harmonize constraints |
| **Rate Limiting** | LIM001 | Rate limit bucket enforced | Info | Evaluate thresholds |
| **Rate Limiting** | LIM002 | Rate limit reset anomaly | Warn | Check time source |
| **Type Mapping** | MAP001 | MapType override applied | Info | Validate mapping correctness |
| **Nullability** | NRT001 | Nullability mismatch | Error | Adjust NRT config |
| **Examples** | EXM001 | Example generation throttled | Warn | Provide explicit example |
| **Virtualization** | VIRT001 | Virtualization threshold triggered | Info | Performance expectation |
| **Retention** | RET001 | Retention size purge executed | Info | Monitor volume |
| **Retention** | RET002 | Retention age purge executed | Info | Confirm retentionDays |
| **Policy** | POL001 | PolicyToScope mapping applied | Info | Validate scopes |
| **Configuration** | CFG001 | Invalid per-doc route template attempt | Info | Use global template |
| **Sanitization** | SAN001 | Disallowed head injection | Warn | Restrict to meta/link |
| **Sanitization** | HD001 | Disallowed head injection attempt | Warn | Use local meta/link |
| **Boundaries** | BND001 | Multipart field count limit exceeded | Warn | Review field count limits |
| **Links** | LNK001 | Circular link detection | Warn | Review link structure |

## Getting Code Metadata

```csharp
// Get metadata for a diagnostic code
var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.IntegrityCheckFailed);
Console.WriteLine($"Description: {metadata.Description}");
Console.WriteLine($"Severity: {metadata.Level}");
Console.WriteLine($"Action: {metadata.RecommendedAction}");

// Check if a code is valid
bool isValid = DiagnosticCodes.IsValidCode("SEC001");  // true
isValid = DiagnosticCodes.IsValidCode("UNKNOWN001");   // false

// Get all defined codes
var allCodes = DiagnosticCodes.GetAllCodes();
Console.WriteLine($"Total defined codes: {allCodes.Length}");
```

## Clearing Events

```csharp
// Clear all diagnostic events (useful for testing or log rotation)
logger.Clear();
```

## Thread Safety

The DiagnosticsLogger is thread-safe and can be used from multiple threads concurrently.

```csharp
// Safe to use from multiple threads
Parallel.For(0, 100, i =>
{
    logger.LogInfo($"INFO{i:D3}", $"Message {i}");
});
```
