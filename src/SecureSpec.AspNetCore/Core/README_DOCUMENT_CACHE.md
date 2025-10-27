# Document Cache Implementation

## Overview

The `DocumentCache` class provides thread-safe caching for OpenAPI documents with integrity validation and configurable expiration policies. It uses a Reader-Writer lock strategy to allow multiple concurrent readers while ensuring exclusive write access.

## Architecture

### Thread Safety Model

The cache employs `ReaderWriterLockSlim` with a no-recursion policy:

- **Multiple Readers**: Multiple threads can read from the cache simultaneously
- **Single Writer**: Only one thread can write to the cache at a time
- **Read Lock Operations**: `TryGet`, `Count`
- **Write Lock Operations**: `Set`, `Invalidate`, `InvalidateAll`, `EvictExpired`

### Cache Entry Structure

Each cache entry consists of:

```csharp
public sealed class CacheEntry
{
    string Content        // Serialized document content
    string Hash           // SHA256 hash of the content
    DateTimeOffset Timestamp   // Entry creation time
    DateTimeOffset ExpiresAt   // Entry expiration time
    
    bool IsExpired        // Property: checks if entry has expired
    bool ValidateIntegrity()   // Method: recomputes and validates hash
}
```

### Integrity Validation

Every cache retrieval validates integrity by:
1. Checking if the entry has expired
2. Recomputing the SHA256 hash of the cached content
3. Comparing with the stored hash
4. Logging diagnostic event (CACHE001) if validation fails

## Configuration

### Cache Options

Configure caching behavior via `CacheOptions` in `SecureSpecOptions`:

```csharp
services.AddSecureSpec(options =>
{
    options.Cache.DefaultExpiration = TimeSpan.FromMinutes(10);
    options.Cache.Enabled = true;
    options.Cache.ValidateIntegrity = true;
    options.Cache.AutoEvictionInterval = TimeSpan.FromMinutes(5);
});
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DefaultExpiration` | `TimeSpan` | 5 minutes | How long cache entries remain valid |
| `Enabled` | `bool` | `true` | Whether caching is enabled |
| `ValidateIntegrity` | `bool` | `true` | Whether to validate hashes on retrieval |
| `AutoEvictionInterval` | `TimeSpan?` | `null` | Interval for automatic eviction (disabled if null) |

### Service Registration

The cache is registered as a singleton in the dependency injection container:

```csharp
services.AddSecureSpec(options => { });

// Retrieve the cache from DI
var cache = serviceProvider.GetRequiredService<DocumentCache>();
```

## Usage Examples

### Basic Usage

```csharp
// Store a document
var content = SerializeDocument(document);
var hash = CanonicalSerializer.GenerateHash(content);
cache.Set("v1", content, hash);

// Retrieve a document
if (cache.TryGet("v1", out var cachedContent, out var cachedHash))
{
    // Use cached content
    var document = DeserializeDocument(cachedContent);
}
else
{
    // Cache miss - generate document
}
```

### Custom Expiration

```csharp
// Set with custom expiration
cache.Set("v1", content, hash, TimeSpan.FromHours(1));
```

### Cache Invalidation

```csharp
// Invalidate specific document
cache.Invalidate("v1");

// Invalidate all documents
cache.InvalidateAll();

// Manually evict expired entries
var evictedCount = cache.EvictExpired();
```

## Diagnostic Events

The cache emits the following diagnostic codes:

| Code | Level | Description |
|------|-------|-------------|
| `CACHE001` | Warning | Integrity validation failed (hash mismatch) |
| `CACHE002` | Info | Cache miss |
| `CACHE003` | Info | Cache entry expired |
| `CACHE004` | Info | Cache hit |
| `CACHE005` | Info | Document cached |
| `CACHE006` | Info | Cache entry invalidated |
| `CACHE007` | Info | All cache entries invalidated |
| `CACHE008` | Info | Expired entries evicted |

### Monitoring Cache Events

```csharp
var logger = serviceProvider.GetRequiredService<DiagnosticsLogger>();

// Perform cache operations
cache.TryGet("key", out _, out _);

// Retrieve diagnostic events
var events = logger.GetEvents();
var cacheEvents = events.Where(e => e.Code.StartsWith("CACHE"));
```

## Performance Characteristics

### Lock Contention

The RW lock strategy minimizes contention:
- **Read-Heavy Workloads**: Excellent performance with multiple concurrent readers
- **Write-Heavy Workloads**: Sequential writes may cause some contention
- **Mixed Workloads**: Good balance with configurable expiration to reduce write frequency

### Memory Management

- Cache entries are immutable strings (reduces allocation overhead)
- No automatic size limits (consider implementing if needed for your use case)
- Expired entries can be evicted manually or via periodic background task

### Concurrency Testing Results

Stress tests demonstrate:
- **100 concurrent readers**: No blocking, all reads succeed
- **50 concurrent writers**: All writes complete successfully without corruption
- **5000 mixed operations** (50 threads): Completes without deadlock in <10s

## Integration Patterns

### With OpenAPI Document Generation

```csharp
public class DocumentProvider
{
    private readonly DocumentCache _cache;
    private readonly IDocumentGenerator _generator;

    public async Task<OpenApiDocument> GetDocumentAsync(string name)
    {
        // Try cache first
        if (_cache.TryGet(name, out var content, out _))
        {
            return DeserializeDocument(content);
        }

        // Generate if not cached
        var document = await _generator.GenerateAsync(name);
        var serialized = SerializeDocument(document);
        var hash = CanonicalSerializer.GenerateHash(serialized);
        
        _cache.Set(name, serialized, hash);
        return document;
    }

    public void InvalidateDocument(string name)
    {
        _cache.Invalidate(name);
    }
}
```

### Schema Change Notifications

```csharp
public class SchemaChangeHandler
{
    private readonly DocumentCache _cache;

    public void OnSchemaChanged(string documentName)
    {
        // Invalidate affected document
        _cache.Invalidate(documentName);
    }

    public void OnGlobalSchemaChange()
    {
        // Invalidate all documents
        _cache.InvalidateAll();
    }
}
```

## Best Practices

1. **Expiration**: Set appropriate expiration times based on how frequently your schemas change
2. **Invalidation**: Always invalidate cache when underlying schemas or configurations change
3. **Monitoring**: Review CACHE001 events regularly to detect integrity issues
4. **Disposal**: Ensure the cache is disposed when the application shuts down (handled by DI container)
5. **Testing**: Use the concurrency tests as a template for your integration tests

## Thread Safety Guarantees

The `DocumentCache` class is fully thread-safe and provides the following guarantees:

- **Atomic Operations**: All cache operations are atomic
- **No Data Races**: All shared state is protected by locks
- **Deadlock Prevention**: No recursive locking; operations complete in bounded time
- **Memory Consistency**: Lock acquire/release ensures happens-before relationships

## Limitations

- **No Size Limits**: Cache can grow unbounded; implement eviction policies if needed
- **No Persistence**: Cache is in-memory only; entries are lost on application restart
- **No Distributed Caching**: Each process has its own independent cache
- **Synchronous API**: All operations are synchronous (appropriate for in-memory cache)

## Future Enhancements

Potential improvements for future versions:

1. **Size-Based Eviction**: LRU or LFU eviction when cache reaches size limit
2. **Distributed Cache Support**: Redis or similar for multi-instance deployments
3. **Async API**: Async methods for integration with async document generation
4. **Cache Warming**: Pre-populate cache on startup
5. **Background Eviction**: Automatic periodic eviction based on `AutoEvictionInterval`
6. **Cache Statistics**: Hit/miss ratios, average retrieval time, etc.

## References

- [ARCHITECTURE.md](../../../ARCHITECTURE.md) - Section 8: Document Cache
- [Issue #7](https://github.com/jarz/SecureSpec.AspNetCore/issues/7) - Canonical Serializer (dependency)
- [PRD.md](../../../docs/PRD.md) - Section 38: Thread Safety / Concurrency Guarantees
