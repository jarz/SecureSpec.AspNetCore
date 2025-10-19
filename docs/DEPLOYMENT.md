# Deployment Guide

This document provides deployment architecture, patterns, and best practices for SecureSpec.AspNetCore.

## Table of Contents

- [Deployment Options](#deployment-options)
- [Infrastructure Requirements](#infrastructure-requirements)
- [Configuration](#configuration)
- [Security Hardening](#security-hardening)
- [Monitoring](#monitoring)
- [Scaling](#scaling)
- [Troubleshooting](#troubleshooting)

## Deployment Options

### 1. Embedded in ASP.NET Core Application

**Recommended**: SecureSpec as part of your API application

```
┌─────────────────────────────┐
│   ASP.NET Core Application  │
│  ┌──────────────────────┐   │
│  │   Your API           │   │
│  │   Controllers        │   │
│  └──────────────────────┘   │
│  ┌──────────────────────┐   │
│  │   SecureSpec         │   │
│  │   Documentation      │   │
│  └──────────────────────┘   │
└─────────────────────────────┘
```

**Pros**:
- Single deployment
- Shared authentication
- Simple configuration
- Auto-updates with API

**Cons**:
- Increases application size
- Documentation tied to API lifecycle

**Usage**:
```csharp
builder.Services.AddSecureSpec(/* config */);
app.UseSecureSpec();
app.UseSecureSpecUI("/docs");
```

---

### 2. Separate Documentation Service

**Use Case**: Large organizations, multiple APIs

```
┌──────────────┐     ┌──────────────────┐
│  API Service │────▶│  Documentation   │
│              │     │  Aggregator      │
└──────────────┘     │  (SecureSpec)    │
                     └──────────────────┘
┌──────────────┐            │
│  API Service │────────────┘
└──────────────┘
```

**Pros**:
- Centralized documentation
- Independent scaling
- Single UI for multiple APIs

**Cons**:
- More infrastructure
- Network dependency
- Additional security considerations

---

### 3. Static Generation + CDN

**Use Case**: Public documentation, high traffic

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│  Build       │────▶│  Static      │────▶│     CDN      │
│  Pipeline    │     │  Storage     │     │              │
└──────────────┘     └──────────────┘     └──────────────┘
```

**Pros**:
- Global distribution
- High performance
- Low cost at scale

**Cons**:
- No Try It Out (unless proxied)
- Build-time generation only
- Stale documentation possible

**Usage**:
```bash
# Generate static OpenAPI file
securespec generate --output openapi.json

# Deploy to CDN
aws s3 cp openapi.json s3://docs-bucket/
```

---

## Infrastructure Requirements

### Minimum Requirements

| Resource | Minimum | Recommended |
|----------|---------|-------------|
| CPU | 1 core | 2 cores |
| Memory | 512 MB | 1 GB |
| Storage | 50 MB | 100 MB |
| .NET Version | 8.0 | 8.0+ |

### Scaling Recommendations

| API Size | CPU | Memory | Notes |
|----------|-----|--------|-------|
| <100 operations | 1 core | 512 MB | Minimal |
| 100-500 operations | 2 cores | 1 GB | Standard |
| 500-2000 operations | 4 cores | 2 GB | Large API |
| >2000 operations | 8 cores | 4 GB | Enterprise, consider virtualization |

### Network Requirements

- **Inbound**: HTTPS (443) for documentation access
- **Outbound**: OAuth authorization servers (if configured)
- **Internal**: None (no external dependencies required)

## Configuration

### Environment-Specific Configuration

#### Development

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSecureSpec(options =>
    {
        options.UI.DeepLinking = true;
        options.UI.TryItOutEnabled = true;
        options.RateLimiting.Enabled = false; // For easier testing
        options.Diagnostics.RetentionSize = 10_000; // More logs
    });
}
```

#### Staging

```csharp
if (builder.Environment.IsStaging())
{
    builder.Services.AddSecureSpec(options =>
    {
        options.UI.TryItOutEnabled = true;
        options.RateLimiting.Enabled = true;
        options.RateLimiting.TryItOut.RequestsPerMinute = 100;
        options.Diagnostics.RetentionSize = 5_000;
    });
}
```

#### Production

```csharp
if (builder.Environment.IsProduction())
{
    builder.Services.AddSecureSpec(options =>
    {
        // Strict security
        options.Security.CSP.Enforce = true;
        options.Integrity.EnforceChecks = true;
        
        // Conservative rate limits
        options.RateLimiting.Enabled = true;
        options.RateLimiting.TryItOut.RequestsPerMinute = 30;
        options.RateLimiting.OAuth.RequestsPerMinute = 10;
        options.RateLimiting.SpecDownload.RequestsPerMinute = 60;
        
        // Resource guards
        options.Performance.MaxDocumentSizeMB = 25;
        options.Performance.GenerationTimeoutMs = 2000;
        
        // Diagnostics
        options.Diagnostics.RetentionSize = 1_000;
        options.Diagnostics.RetentionDays = 7;
    });
}
```

### Configuration Sources

#### appsettings.json

```json
{
  "SecureSpec": {
    "Documents": {
      "v1": {
        "Title": "My API",
        "Version": "1.0.0"
      }
    },
    "Security": {
      "OAuth": {
        "AuthorizationUrl": "https://auth.example.com/authorize",
        "TokenUrl": "https://auth.example.com/token"
      }
    },
    "RateLimiting": {
      "TryItOut": {
        "RequestsPerMinute": 30
      }
    }
  }
}
```

#### Environment Variables

```bash
# Docker/Kubernetes
SECURESPEC__RATELIMITING__TRYITOUT__REQUESTSPERMINUTE=30
SECURESPEC__OAUTH__AUTHORIZATIONURL=https://auth.example.com/authorize
```

#### Azure App Configuration

```csharp
builder.Configuration.AddAzureAppConfiguration(options =>
{
    options.Connect(connectionString)
        .Select("SecureSpec:*");
});
```

## Security Hardening

### 1. HTTPS/TLS

**Always use HTTPS in production**:

```csharp
if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
    app.UseHsts(); // HTTP Strict Transport Security
}
```

### 2. Authentication

Protect documentation access:

```csharp
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/docs/{*path}", async context =>
{
    if (!context.User.Identity?.IsAuthenticated ?? true)
    {
        context.Response.StatusCode = 401;
        return;
    }
    
    await context.Response.SendFileAsync("securespec.html");
})
.RequireAuthorization();
```

### 3. Content Security Policy

Verify CSP headers:

```csharp
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/docs"))
    {
        context.Response.Headers.Add("Content-Security-Policy", 
            "default-src 'none'; " +
            "script-src 'nonce-{NONCE}'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; " +
            "font-src 'self'; " +
            "connect-src 'self'");
    }
    await next();
});
```

### 4. Integrity Checks

Enable in production:

```csharp
options.Integrity.EnforceChecks = true;
options.Integrity.FailClosed = true; // Fail on integrity mismatch
```

### 5. Rate Limiting

Configure appropriately:

```csharp
options.RateLimiting.Enabled = true;
options.RateLimiting.TryItOut.RequestsPerMinute = 30;
options.RateLimiting.BurstAllowance = 10; // Allow bursts
```

## Monitoring

### Metrics to Track

#### Performance Metrics

```csharp
// Document generation latency
Histogram generationLatency = Metrics.CreateHistogram(
    "securespec_generation_duration_ms",
    "Document generation duration in milliseconds");

// Cache hit rate
Counter cacheHits = Metrics.CreateCounter(
    "securespec_cache_hits_total",
    "Number of cache hits");

Counter cacheMisses = Metrics.CreateCounter(
    "securespec_cache_misses_total",
    "Number of cache misses");
```

#### Security Metrics

```csharp
// Rate limit violations
Counter rateLimitViolations = Metrics.CreateCounter(
    "securespec_rate_limit_violations_total",
    "Number of rate limit violations");

// Integrity failures
Counter integrityFailures = Metrics.CreateCounter(
    "securespec_integrity_failures_total",
    "Number of integrity check failures");

// Sandbox violations
Counter sandboxViolations = Metrics.CreateCounter(
    "securespec_sandbox_violations_total",
    "Number of sandbox violations");
```

### Logging

**Structured logging with Serilog**:

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.Seq("http://seq-server:5341")
    .Enrich.WithProperty("Application", "SecureSpec")
    .CreateLogger();

builder.Host.UseSerilog();
```

**Log important events**:

```csharp
_logger.LogInformation(
    "Document generated for {DocumentName} in {Duration}ms",
    documentName, duration);

_logger.LogWarning(
    "Rate limit exceeded for {ClientIP}: {DiagnosticCode}",
    clientIp, "LIM001");

_logger.LogError(
    "Integrity check failed for {AssetPath}: {DiagnosticCode}",
    assetPath, "SEC001");
```

### Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<SecureSpecHealthCheck>("securespec");

app.MapHealthChecks("/health");
```

```csharp
public class SecureSpecHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check document cache
            // Check diagnostics retention
            // Check critical components
            
            return Task.FromResult(HealthCheckResult.Healthy());
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("SecureSpec unhealthy", ex));
        }
    }
}
```

## Scaling

### Horizontal Scaling

SecureSpec supports horizontal scaling:

```yaml
# Kubernetes deployment
apiVersion: apps/v1
kind: Deployment
metadata:
  name: api-with-docs
spec:
  replicas: 3  # Scale horizontally
  selector:
    matchLabels:
      app: api
  template:
    metadata:
      labels:
        app: api
    spec:
      containers:
      - name: api
        image: myapi:latest
        resources:
          requests:
            memory: "1Gi"
            cpu: "500m"
          limits:
            memory: "2Gi"
            cpu: "1000m"
```

**Considerations**:
- Document cache is per-instance (acceptable)
- Diagnostics are per-instance (aggregate in log collector)
- Rate limiting is per-instance (multiply limits by replica count)

### Vertical Scaling

For large APIs (>1000 operations):

- Increase memory for document cache
- Increase CPU for schema generation
- Consider virtualization thresholds

### Caching Strategy

```csharp
// Aggressive caching for production
options.Caching.DocumentCacheEnabled = true;
options.Caching.SchemaCacheEnabled = true;
options.Caching.CacheDuration = TimeSpan.FromHours(1);

// ETag support
app.Use(async (context, next) =>
{
    var ifNoneMatch = context.Request.Headers["If-None-Match"];
    if (!string.IsNullOrEmpty(ifNoneMatch) && 
        ifNoneMatch == currentETag)
    {
        context.Response.StatusCode = 304; // Not Modified
        return;
    }
    
    context.Response.Headers.ETag = currentETag;
    await next();
});
```

## Troubleshooting

### Common Issues

#### High Memory Usage

**Symptom**: Memory grows over time

**Diagnosis**:
```bash
# Check diagnostics retention
curl http://localhost:5000/securespec/diagnostics | jq '.count'
```

**Solution**:
```csharp
// Reduce retention
options.Diagnostics.RetentionSize = 500;
options.Diagnostics.RetentionDays = 3;
```

---

#### Slow Document Generation

**Symptom**: Generation takes >500ms

**Diagnosis**:
```csharp
_logger.LogInformation("Generation took {Duration}ms", duration);
```

**Solution**:
```csharp
// Enable virtualization
options.Schema.VirtualizationThreshold = 150; // Lower threshold
options.Performance.MaxDepth = 24; // Reduce depth
```

---

#### Rate Limit Issues

**Symptom**: Users getting 429 Too Many Requests

**Diagnosis**:
```bash
# Check logs for LIM001
grep "LIM001" application.log
```

**Solution**:
```csharp
// Adjust limits
options.RateLimiting.TryItOut.RequestsPerMinute = 60;
options.RateLimiting.BurstAllowance = 20;
```

---

#### CSP Violations

**Symptom**: Browser console shows CSP errors

**Diagnosis**: Check browser console for CSP violation reports

**Solution**:
```csharp
// Verify nonce generation
app.Use(async (context, next) =>
{
    var nonce = GenerateNonce();
    context.Items["csp-nonce"] = nonce;
    await next();
});
```

## Deployment Checklist

### Pre-Deployment

- [ ] Configure production OAuth URLs
- [ ] Set appropriate rate limits
- [ ] Enable integrity checks
- [ ] Configure CSP policy
- [ ] Set up logging infrastructure
- [ ] Configure health checks
- [ ] Review resource limits
- [ ] Test fail-closed behavior

### Post-Deployment

- [ ] Verify HTTPS/TLS
- [ ] Test authentication
- [ ] Check CSP headers
- [ ] Verify integrity checks
- [ ] Monitor generation latency
- [ ] Check cache hit rate
- [ ] Review diagnostic logs
- [ ] Test rate limiting

### Ongoing Monitoring

- [ ] Document generation duration (p95 <500ms)
- [ ] Cache hit rate (>80%)
- [ ] Rate limit violations
- [ ] Integrity failures
- [ ] Sandbox violations
- [ ] Memory usage
- [ ] Error rates

## References

- [ARCHITECTURE.md](../ARCHITECTURE.md) - System architecture
- [SECURITY.md](../SECURITY.md) - Security policy
- [PRD.md](PRD.md) - Performance targets (Section 5)

---

**Last Updated**: 2025-10-19  
**Version**: 1.0
