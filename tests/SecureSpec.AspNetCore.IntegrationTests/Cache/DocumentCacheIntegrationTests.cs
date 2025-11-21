using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Core;
using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.IntegrationTests.Infrastructure;
using SecureSpec.AspNetCore.Serialization;
using Xunit;

namespace SecureSpec.AspNetCore.IntegrationTests.Cache;

/// <summary>
/// Tests for <see cref="DocumentCache"/> behavior with configurable options.
/// </summary>
public class DocumentCacheIntegrationTests
{
    /// <summary>
    /// Verifies cache honors custom default expiration from <see cref="CacheOptions"/>.
    /// </summary>
    [Fact]
    public async Task CacheUsesCustomDefaultExpiration()
    {
        var start = DateTimeOffset.UtcNow;
        var timeProvider = new TestTimeProvider(start);

        using var host = await IntegrationTestHostFactory.StartHostAsync(
            services =>
            {
                services.AddSingleton<TimeProvider>(timeProvider);
                services.AddSecureSpec(options =>
                {
                    options.Cache.DefaultExpiration = TimeSpan.FromSeconds(1);
                });

                services.AddSingleton<DocumentCache>(sp =>
                {
                    var logger = sp.GetRequiredService<DiagnosticsLogger>();
                    var options = sp.GetRequiredService<IOptions<SecureSpecOptions>>().Value;
                    return new DocumentCache(logger, options.Cache.DefaultExpiration, timeProvider);
                });
            });

        var cache = host.Services.GetRequiredService<DocumentCache>();
        var payload = "{}";
        var hash = CanonicalSerializer.GenerateHash(payload);
        cache.Set("doc", payload, hash);

        Assert.True(cache.TryGet("doc", out _, out _));

        timeProvider.Advance(TimeSpan.FromSeconds(2));
        Assert.False(cache.TryGet("doc", out _, out _));

        await host.StopAsync();
    }

    /// <summary>
    /// Ensures eviction API removes expired entries and logs diagnostics.
    /// </summary>
    [Fact]
    public void EvictExpired_RemovesEntries()
    {
        var logger = new DiagnosticsLogger();
        var timeProvider = new TestTimeProvider(DateTimeOffset.UtcNow);
        using var cache = new DocumentCache(logger, TimeSpan.FromSeconds(1), timeProvider);

        cache.Set("doc", "value", "hash");
        timeProvider.Advance(TimeSpan.FromSeconds(2));

        var evicted = cache.EvictExpired();
        Assert.Equal(1, evicted);
        Assert.False(cache.TryGet("doc", out _, out _));
    }

    /// <summary>
    /// Validates that integrity mismatches prevent retrieval and raise warnings.
    /// </summary>
    [Fact]
    public void TryGet_WithIntegrityMismatch_ReturnsFalseAndLogsWarning()
    {
        var timeProvider = new TestTimeProvider(DateTimeOffset.UtcNow);
        var logger = new DiagnosticsLogger(timeProvider);
        using var cache = new DocumentCache(logger, TimeSpan.FromMinutes(5), timeProvider);

        var payload = "{\"hello\":\"world\"}";
        cache.Set("doc", payload, hash: "bad-hash");

        var result = cache.TryGet("doc", out var content, out var hash);

        Assert.False(result);
        Assert.Null(content);
        Assert.Null(hash);

        var warning = Assert.Single(logger.GetEvents().Where(e => e.Code == "CACHE001"));
        Assert.Equal(DiagnosticLevel.Warn, warning.Level);
    }

    /// <summary>
    /// Confirms per-call expiration overrides the default cache expiration window.
    /// </summary>
    [Fact]
    public void Set_WithCustomExpiration_OverridesDefault()
    {
        var timeProvider = new TestTimeProvider(DateTimeOffset.UtcNow);
        var logger = new DiagnosticsLogger(timeProvider);
        using var cache = new DocumentCache(logger, TimeSpan.FromMinutes(10), timeProvider);

        var payload = "{}";
        var hash = CanonicalSerializer.GenerateHash(payload);

        cache.Set("doc", payload, hash, expiration: TimeSpan.FromSeconds(1));

        Assert.True(cache.TryGet("doc", out _, out _));

        timeProvider.Advance(TimeSpan.FromSeconds(2));

        Assert.False(cache.TryGet("doc", out _, out _));
    }

    /// <summary>
    /// Ensures eviction diagnostics include contextual information about removed entries.
    /// </summary>
    [Fact]
    public void EvictExpired_LogsDiagnosticEvents()
    {
        var timeProvider = new TestTimeProvider(DateTimeOffset.UtcNow);
        var logger = new DiagnosticsLogger(timeProvider);
        using var cache = new DocumentCache(logger, TimeSpan.FromMinutes(5), timeProvider);

        var payload = "{}";
        var hash = CanonicalSerializer.GenerateHash(payload);

        cache.Set("expired", payload, hash, TimeSpan.FromSeconds(1));
        cache.Set("fresh", payload, hash, TimeSpan.FromMinutes(1));

        timeProvider.Advance(TimeSpan.FromSeconds(2));

        var evicted = cache.EvictExpired();

        Assert.Equal(1, evicted);

        var diagnostic = Assert.Single(logger.GetEvents().Where(e => e.Code == "CACHE008"));
        Assert.Equal(DiagnosticLevel.Info, diagnostic.Level);
        Assert.NotNull(diagnostic.Context);

        var entriesEvictedProperty = diagnostic.Context!.GetType().GetProperty("EntriesEvicted");
        Assert.NotNull(entriesEvictedProperty);
        Assert.Equal(1, entriesEvictedProperty!.GetValue(diagnostic.Context));
    }

    /// <summary>
    /// Ensures targeted invalidation removes only the specified entry and logs diagnostics.
    /// </summary>
    [Fact]
    public void Invalidate_RemovesSingleEntryAndLogs()
    {
        var timeProvider = new TestTimeProvider(DateTimeOffset.UtcNow);
        var logger = new DiagnosticsLogger(timeProvider);
        using var cache = new DocumentCache(logger, TimeSpan.FromMinutes(5), timeProvider);

        var payloadPrimary = "{}";
        var payloadSecondary = "{\"id\":1}";

        var hashPrimary = CanonicalSerializer.GenerateHash(payloadPrimary);
        var hashSecondary = CanonicalSerializer.GenerateHash(payloadSecondary);

        cache.Set("doc-primary", payloadPrimary, hashPrimary);
        cache.Set("doc-secondary", payloadSecondary, hashSecondary);

        var removed = cache.Invalidate("doc-primary");

        Assert.True(removed);
        Assert.False(cache.TryGet("doc-primary", out _, out _));

        Assert.True(cache.TryGet("doc-secondary", out var content, out var hash));
        Assert.Equal(payloadSecondary, content);
        Assert.Equal(hashSecondary, hash);

        var diagnostic = Assert.Single(logger.GetEvents().Where(e => e.Code == "CACHE006"));
        Assert.Equal(DiagnosticLevel.Info, diagnostic.Level);
        Assert.NotNull(diagnostic.Context);
    }

    /// <summary>
    /// Ensures clearing the entire cache removes all entries and emits diagnostics.
    /// </summary>
    [Fact]
    public void InvalidateAll_RemovesEverythingAndLogs()
    {
        var timeProvider = new TestTimeProvider(DateTimeOffset.UtcNow);
        var logger = new DiagnosticsLogger(timeProvider);
        using var cache = new DocumentCache(logger, TimeSpan.FromMinutes(5), timeProvider);

        var payload = "{}";
        var hash = CanonicalSerializer.GenerateHash(payload);

        cache.Set("doc-1", payload, hash);
        cache.Set("doc-2", payload, hash);

        cache.InvalidateAll();

        Assert.False(cache.TryGet("doc-1", out _, out _));
        Assert.False(cache.TryGet("doc-2", out _, out _));

        var diagnostic = Assert.Single(logger.GetEvents().Where(e => e.Code == "CACHE007"));
        Assert.Equal(DiagnosticLevel.Info, diagnostic.Level);
        Assert.NotNull(diagnostic.Context);

        var entriesClearedProperty = diagnostic.Context!.GetType().GetProperty("EntriesCleared");
        Assert.NotNull(entriesClearedProperty);
        Assert.Equal(2, entriesClearedProperty!.GetValue(diagnostic.Context));
    }

    /// <summary>
    /// Exercises cache operations under concurrent load to validate thread-safety.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task ConcurrentSetAndGet_RemainsConsistent()
    {
        var timeProvider = new TestTimeProvider(DateTimeOffset.UtcNow);
        var logger = new DiagnosticsLogger(timeProvider);
        using var cache = new DocumentCache(logger, TimeSpan.FromMinutes(5), timeProvider);

        const int workers = 8;
        const int iterationsPerWorker = 50;
        var payload = "{\"mode\":\"parallel\"}";
        var hash = CanonicalSerializer.GenerateHash(payload);

        var tasks = Enumerable.Range(0, workers)
            .Select(worker => System.Threading.Tasks.Task.Run(() =>
            {
                for (var i = 0; i < iterationsPerWorker; i++)
                {
                    var key = $"doc-{worker}-{i}";
                    cache.Set(key, payload, hash);

                    Assert.True(cache.TryGet(key, out var content, out var storedHash));
                    Assert.Equal(payload, content);
                    Assert.Equal(hash, storedHash);
                }
            }));

        await System.Threading.Tasks.Task.WhenAll(tasks);

        Assert.Equal(workers * iterationsPerWorker, cache.Count);
    }

    /// <summary>
    /// Simulates a mixed workload combining reads, writes, and evictions to ensure consistency.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task ConcurrentGetSetAndEvict_MaintainsCacheIntegrity()
    {
        var timeProvider = new TestTimeProvider(DateTimeOffset.UtcNow);
        var logger = new DiagnosticsLogger(timeProvider);
        using var cache = new DocumentCache(logger, TimeSpan.FromSeconds(5), timeProvider);

        var payload = "{\"mode\":\"churn\"}";
        var hash = CanonicalSerializer.GenerateHash(payload);

        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(2));

        var writerTask = System.Threading.Tasks.Task.Run(() =>
        {
            var i = 0;
            while (!cts.IsCancellationRequested)
            {
                var key = $"doc-writer-{i++}";
                cache.Set(key, payload, hash, TimeSpan.FromMilliseconds(200));
            }
        }, cts.Token);

        var keysToProbe = new[] { "doc-writer-0", "doc-writer-1", "doc-writer-2" };

        var readerTask = System.Threading.Tasks.Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                foreach (var key in keysToProbe)
                {
                    cache.TryGet(key, out _, out _);
                }

                timeProvider.Advance(TimeSpan.FromMilliseconds(50));
            }
        }, cts.Token);

        var evictorTask = System.Threading.Tasks.Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                timeProvider.Advance(TimeSpan.FromMilliseconds(200));
                cache.EvictExpired();
            }
        }, cts.Token);

        await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(1.5));
        await cts.CancelAsync();

        await System.Threading.Tasks.Task.WhenAll(writerTask, readerTask, evictorTask);

        var events = logger.GetEvents();
        Assert.Contains(events, evt => evt.Code is "CACHE002" or "CACHE003" or "CACHE008");
    }

    /// <summary>
    /// Verifies cache throws when used after disposal to prevent undefined behavior.
    /// </summary>
    [Fact]
    public void Dispose_PreventsFurtherUsage()
    {
        var timeProvider = new TestTimeProvider(DateTimeOffset.UtcNow);
        var logger = new DiagnosticsLogger(timeProvider);
        var cache = new DocumentCache(logger, TimeSpan.FromMinutes(5), timeProvider);

        var payload = "{}";
        var hash = CanonicalSerializer.GenerateHash(payload);

        cache.Set("doc", payload, hash);
        cache.Dispose();

        Assert.Throws<ObjectDisposedException>(() => cache.Set("other", payload, hash));
        Assert.Throws<ObjectDisposedException>(() => cache.TryGet("doc", out _, out _));
        Assert.Throws<ObjectDisposedException>(() => cache.Invalidate("doc"));
    }
}
