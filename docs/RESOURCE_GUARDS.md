# Resource Guards Guide

This guide explains how to configure and use resource guards to protect against resource exhaustion during OpenAPI document generation.

## Overview

Resource guards monitor time and memory usage during document generation and provide automatic fallback behavior when limits are exceeded. This prevents denial-of-service scenarios and ensures the application remains responsive even with extremely large or complex API schemas.

## Configuration

Resource guards are configured via the `Performance` property of `SecureSpecOptions`:

```csharp
builder.Services.AddSecureSpec(options =>
{
    // Configure resource guards
    options.Performance.EnableResourceGuards = true;
    options.Performance.MaxGenerationTimeMs = 2000; // 2 seconds
    options.Performance.MaxMemoryBytes = 10 * 1024 * 1024; // 10 MB
});
```

### Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EnableResourceGuards` | `bool` | `true` | Enables or disables resource guard monitoring |
| `MaxGenerationTimeMs` | `int` | `2000` | Maximum time allowed for document generation in milliseconds |
| `MaxMemoryBytes` | `long` | `10485760` | Maximum memory allowed for document generation in bytes (10 MB) |

## How It Works

### 1. Time Monitoring

The resource guard uses a high-precision `Stopwatch` to track elapsed time during document generation. If generation exceeds the configured time limit, a fallback document is returned instead.

### 2. Memory Monitoring

Memory usage is tracked by comparing GC-reported memory before and after generation. If memory usage exceeds the configured limit, a fallback document is returned.

### 3. Fallback Document

When a resource limit is exceeded, a minimal fallback document is generated that includes:

- ✅ Basic `Info` section with title and version
- ✅ Warning banner explaining why full generation failed
- ✅ Empty `Paths` collection (no operations)
- ✅ Empty `Schemas` collection (no components)
- ❌ No security schemes or requirements

This ensures that:
- No stale or partial operations leak into the document
- The API remains discoverable (basic info is present)
- Users receive clear feedback about the issue
- The application doesn't crash or hang

### 4. Diagnostic Emission

When a resource limit is exceeded, a `PERF001` diagnostic event is emitted with details about:
- The resource type that was exceeded (Time or Memory)
- The actual usage vs. the configured limit
- The document name being generated
- Additional context for troubleshooting

## Usage Example

### Basic Setup

```csharp
using SecureSpec.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSecureSpec(options =>
{
    options.Documents.Add("v1", doc =>
    {
        doc.Info.Title = "My API";
        doc.Info.Version = "1.0.0";
    });

    // Configure resource guards
    options.Performance.EnableResourceGuards = true;
    options.Performance.MaxGenerationTimeMs = 2000; // 2 seconds
    options.Performance.MaxMemoryBytes = 10 * 1024 * 1024; // 10 MB
});

var app = builder.Build();
app.Run();
```

### Disabling Resource Guards (Not Recommended)

For testing or development purposes, you can disable resource guards:

```csharp
options.Performance.EnableResourceGuards = false;
```

⚠️ **Warning:** Disabling resource guards in production is not recommended as it can leave your application vulnerable to resource exhaustion attacks.

### Custom Limits

You can adjust limits based on your application's needs:

```csharp
// For very large APIs
options.Performance.MaxGenerationTimeMs = 5000; // 5 seconds
options.Performance.MaxMemoryBytes = 50 * 1024 * 1024; // 50 MB

// For constrained environments
options.Performance.MaxGenerationTimeMs = 500; // 0.5 seconds
options.Performance.MaxMemoryBytes = 5 * 1024 * 1024; // 5 MB
```

## Monitoring and Diagnostics

### Checking for PERF001 Events

You can monitor for PERF001 diagnostics to identify when resource limits are being hit:

```csharp
var logger = app.Services.GetRequiredService<DiagnosticsLogger>();
var events = logger.GetEvents();

var perfEvents = events.Where(e => e.Code == "PERF001");
foreach (var evt in perfEvents)
{
    Console.WriteLine($"Performance limit exceeded: {evt.Message}");
    Console.WriteLine($"Context: {evt.Context}");
}
```

### Event Context

PERF001 events include rich context:

```json
{
  "DocumentName": "v1",
  "Reason": "Generation time exceeded limit: 2543ms > 2000ms",
  "ElapsedMs": 2543,
  "MemoryBytes": 8388608,
  "ResourceType": "Time"
}
```

## Best Practices

### 1. Set Realistic Limits

Choose limits based on your environment and requirements:

- **Development**: Use generous limits (5-10 seconds, 50+ MB) for easier debugging
- **Production (Standard)**: Use default limits (2 seconds, 10 MB) for good balance
- **Production (Strict)**: Use tight limits (500-1000ms, 5 MB) for maximum protection
- **Testing**: Test with actual API size to find appropriate limits

### 2. Optimize Schema Generation

If you're hitting resource limits frequently:

- Review your schema depth and complexity
- Consider using virtualization for large enums (>10K values)
- Reduce maximum traversal depth if appropriate
- Use schema filters to exclude unnecessary properties

### 3. Monitor in Production

- Set up alerts for PERF001 diagnostic events
- Track the frequency of fallback document generation
- Investigate root causes when limits are exceeded regularly

### 4. Handle Fallback Documents Gracefully

If you're using the generated OpenAPI document programmatically:

```csharp
if (document.Paths.Count == 0 && 
    document.Info.Description?.Contains("⚠️") == true)
{
    // This is a fallback document
    // Handle appropriately (log, alert, retry, etc.)
}
```

## Performance Targets

SecureSpec.AspNetCore is designed to meet these performance targets:

| Operation | Target | Degraded | Failure |
|-----------|--------|----------|---------|
| Generation (1000 ops) | <500ms | 500–2000ms | >2000ms |
| Recursive schema traversal | <100ms | 100–500ms | >500ms |
| Hash computation | <50ms | 50–200ms | >200ms |

**Note:** The default `MaxGenerationTimeMs` of 2000ms is set at the "degraded" threshold to allow reasonable generation time for most APIs while still providing protection against runaway generation. For optimal performance, APIs should target the <500ms range, but the guard provides a safety net at 2000ms. Adjust based on your API complexity and performance requirements.

## Troubleshooting

### "Generation time exceeded limit" Messages

**Symptoms:** PERF001 diagnostics with time limit exceeded

**Possible Causes:**
- API has too many operations (>1000)
- Deep schema nesting (>32 levels)
- Complex polymorphism with many variants
- Slow custom filters or schema generators

**Solutions:**
- Increase `MaxGenerationTimeMs`
- Reduce `Schema.MaxDepth`
- Optimize custom filters
- Split into multiple documents

### "Memory usage exceeded limit" Messages

**Symptoms:** PERF001 diagnostics with memory limit exceeded

**Possible Causes:**
- Very large schemas (>200 properties)
- Large enums (>10,000 values)
- Many duplicate schema registrations
- Memory leaks in custom filters

**Solutions:**
- Increase `MaxMemoryBytes`
- Enable enum virtualization
- Review schema ID collision handling
- Profile custom filter memory usage

### Fallback Documents in Production

**Symptoms:** Users see warning banners instead of full API documentation

**Immediate Action:**
1. Check PERF001 diagnostic logs
2. Identify which resource limit is being hit
3. Temporarily increase limits if safe to do so
4. Investigate root cause

**Long-term Solution:**
1. Profile document generation
2. Optimize schema complexity
3. Consider API restructuring
4. Add caching if appropriate

## Related Documentation

- [Performance Targets](PRD.md#5-performance-targets)
- [Error Code Reference](PRD.md#o-error-code-reference)
- [Monitoring & Observability](PRD.md#9-monitoring--observability)
- [Threat Model - DoS Mitigations](THREAT_MODEL.md)

## Support

For issues or questions about resource guards:

- 📖 Check the [documentation](INDEX.md)
- 🐛 Report bugs on [GitHub Issues](https://github.com/jarz/SecureSpec.AspNetCore/issues)
- 💬 Join [discussions](https://github.com/jarz/SecureSpec.AspNetCore/discussions)
