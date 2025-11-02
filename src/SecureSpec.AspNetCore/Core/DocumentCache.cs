using SecureSpec.AspNetCore.Diagnostics;

namespace SecureSpec.AspNetCore.Core;

/// <summary>
/// Thread-safe cache for OpenAPI documents using RW lock (multiple readers, single writer).
/// </summary>
public sealed class DocumentCache : IDisposable
{
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);
    private readonly Dictionary<string, CacheEntry> _cache = new(StringComparer.Ordinal);
    private readonly DiagnosticsLogger _logger;
    private readonly TimeSpan _defaultExpiration;
    private readonly TimeProvider _timeProvider;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentCache"/> class.
    /// </summary>
    /// <param name="logger">The diagnostics logger.</param>
    /// <param name="defaultExpiration">The default expiration time for cache entries. Defaults to 5 minutes.</param>
    /// <param name="timeProvider">Optional time provider used to determine cache entry lifetimes.</param>
    public DocumentCache(DiagnosticsLogger logger, TimeSpan? defaultExpiration = null, TimeProvider? timeProvider = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultExpiration = defaultExpiration ?? TimeSpan.FromMinutes(5);
        _timeProvider = timeProvider ?? TimeProvider.System;

        if (_defaultExpiration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(defaultExpiration), "Expiration time must be positive.");
        }
    }

    /// <summary>
    /// Tries to get a cached document by key.
    /// </summary>
    /// <param name="key">The cache key (typically document name).</param>
    /// <param name="content">The cached content if found and valid.</param>
    /// <param name="hash">The hash of the cached content if found and valid.</param>
    /// <returns>True if a valid cached entry was found; otherwise, false.</returns>
    public bool TryGet(string key, out string? content, out string? hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ObjectDisposedException.ThrowIf(_disposed, this);

        content = null;
        hash = null;

        _lock.EnterReadLock();
        try
        {
            if (!_cache.TryGetValue(key, out var entry))
            {
                _logger.LogInfo("CACHE002", $"Cache miss for document '{key}'", new { DocumentKey = key });
                return false;
            }

            // Check expiration
            if (entry.IsExpired(_timeProvider))
            {
                _logger.LogInfo("CACHE003", $"Cache entry expired for document '{key}'", new { DocumentKey = key, ExpiresAt = entry.ExpiresAt });
                return false;
            }

            // Validate integrity post-expiry or on retrieval
            if (!entry.ValidateIntegrity())
            {
                _logger.LogWarning(
                    "CACHE001",
                    $"Integrity validation failed for cached document '{key}'. Hash mismatch detected.",
                    new { DocumentKey = key, StoredHash = entry.Hash }
                );
                return false;
            }

            content = entry.Content;
            hash = entry.Hash;
            _logger.LogInfo("CACHE004", $"Cache hit for document '{key}'", new { DocumentKey = key });
            return true;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Sets or updates a cached document.
    /// </summary>
    /// <param name="key">The cache key (typically document name).</param>
    /// <param name="content">The serialized document content.</param>
    /// <param name="hash">The SHA256 hash of the content.</param>
    /// <param name="expiration">Optional custom expiration time. Uses default if not specified.</param>
    public void Set(string key, string content, string hash, TimeSpan? expiration = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var expirationTime = expiration ?? _defaultExpiration;
        if (expirationTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(expiration), "Expiration time must be positive.");
        }

        var now = _timeProvider.GetUtcNow();
        var entry = new CacheEntry(content, hash, now, now.Add(expirationTime));

        _lock.EnterWriteLock();
        try
        {
            _cache[key] = entry;
            _logger.LogInfo("CACHE005", $"Cached document '{key}' with expiration at {entry.ExpiresAt:O}", new { DocumentKey = key, ExpiresAt = entry.ExpiresAt });
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Invalidates (removes) a cached document by key.
    /// </summary>
    /// <param name="key">The cache key to invalidate.</param>
    /// <returns>True if the entry was found and removed; otherwise, false.</returns>
    public bool Invalidate(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ObjectDisposedException.ThrowIf(_disposed, this);

        _lock.EnterWriteLock();
        try
        {
            var removed = _cache.Remove(key);
            if (removed)
            {
                _logger.LogInfo("CACHE006", $"Invalidated cache entry for document '{key}'", new { DocumentKey = key });
            }
            return removed;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Invalidates all cached documents.
    /// </summary>
    public void InvalidateAll()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _lock.EnterWriteLock();
        try
        {
            var count = _cache.Count;
            _cache.Clear();
            _logger.LogInfo("CACHE007", $"Invalidated all cache entries ({count} entries cleared)", new { EntriesCleared = count });
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Evicts expired entries from the cache.
    /// </summary>
    /// <returns>The number of entries evicted.</returns>
    public int EvictExpired()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _lock.EnterWriteLock();
        try
        {
            var expiredKeys = _cache
                .Where(kvp => kvp.Value.IsExpired(_timeProvider))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _cache.Remove(key);
            }

            if (expiredKeys.Count > 0)
            {
                _logger.LogInfo("CACHE008", $"Evicted {expiredKeys.Count} expired cache entries", new { EntriesEvicted = expiredKeys.Count });
            }

            return expiredKeys.Count;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Gets the number of entries currently in the cache.
    /// </summary>
    public int Count
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            _lock.EnterReadLock();
            try
            {
                return _cache.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// Disposes the document cache and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _lock.Dispose();
        _disposed = true;
    }
}
