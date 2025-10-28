using SecureSpec.AspNetCore.Core;
using SecureSpec.AspNetCore.Diagnostics;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Concurrency tests for the DocumentCache class to verify thread-safety.
/// </summary>
public class DocumentCacheConcurrencyTests
{
    private DiagnosticsLogger CreateLogger() => new DiagnosticsLogger();

    [Fact]
    public async Task ConcurrentReads_DoNotBlock()
    {
        // Arrange
        using var cache = new DocumentCache(CreateLogger());
        const string key = "doc1";
        const string content = "Test content for concurrent reads";
        var hash = Serialization.CanonicalSerializer.GenerateHash(content);
        cache.Set(key, content, hash);

        var readTasks = new List<Task<bool>>();
        const int numberOfReaders = 100;

        // Act - Simulate multiple concurrent readers
        for (int i = 0; i < numberOfReaders; i++)
        {
            readTasks.Add(Task.Run(() =>
            {
                var result = cache.TryGet(key, out var retrievedContent, out var retrievedHash);
                return result && retrievedContent == content && retrievedHash == hash;
            }));
        }

        await Task.WhenAll(readTasks);

        // Assert - All reads should succeed
        foreach (var task in readTasks)
        {
            Assert.True(await task);
        }
    }

    [Fact]
    public async Task ConcurrentWrites_DoNotCorruptCache()
    {
        // Arrange
        using var cache = new DocumentCache(CreateLogger());
        const int numberOfWriters = 50;
        var writeTasks = new List<Task>();

        // Act - Simulate multiple concurrent writers to different keys
        for (int i = 0; i < numberOfWriters; i++)
        {
            var index = i;
            writeTasks.Add(Task.Run(() =>
            {
                var key = $"doc{index}";
                var content = $"Content for doc{index}";
                var hash = Serialization.CanonicalSerializer.GenerateHash(content);
                cache.Set(key, content, hash);
            }));
        }

        await Task.WhenAll(writeTasks);

        // Assert - All entries should be present and valid
        Assert.Equal(numberOfWriters, cache.Count);

        for (int i = 0; i < numberOfWriters; i++)
        {
            var key = $"doc{i}";
            var expectedContent = $"Content for doc{i}";
            var result = cache.TryGet(key, out var content, out var hash);

            Assert.True(result);
            Assert.Equal(expectedContent, content);
            Assert.Equal(Serialization.CanonicalSerializer.GenerateHash(expectedContent), hash);
        }
    }

    [Fact]
    public async Task ConcurrentReadsAndWrites_MaintainConsistency()
    {
        // Arrange
        using var cache = new DocumentCache(CreateLogger());
        const int numberOfOperations = 100;
        var tasks = new List<Task>();

        // Pre-populate some entries
        for (int i = 0; i < 10; i++)
        {
            var key = $"doc{i}";
            var content = $"Initial content {i}";
            var hash = Serialization.CanonicalSerializer.GenerateHash(content);
            cache.Set(key, content, hash);
        }

        // Act - Mix of concurrent reads and writes
        for (int i = 0; i < numberOfOperations; i++)
        {
            var index = i % 10;

            // Half reads, half writes
            if (i % 2 == 0)
            {
                tasks.Add(Task.Run(() =>
                {
                    var key = $"doc{index}";
                    cache.TryGet(key, out _, out _);
                }));
            }
            else
            {
                tasks.Add(Task.Run(() =>
                {
                    var key = $"doc{index}";
                    var content = $"Updated content {index} at {DateTimeOffset.UtcNow.Ticks}";
                    var hash = Serialization.CanonicalSerializer.GenerateHash(content);
                    cache.Set(key, content, hash);
                }));
            }
        }

        await Task.WhenAll(tasks);

        // Assert - Cache should still be consistent
        Assert.Equal(10, cache.Count);

        // All entries should be valid (no corruption)
        for (int i = 0; i < 10; i++)
        {
            var key = $"doc{i}";
            var result = cache.TryGet(key, out var content, out var hash);

            Assert.True(result);
            Assert.NotNull(content);
            Assert.NotNull(hash);
            Assert.Equal(Serialization.CanonicalSerializer.GenerateHash(content), hash);
        }
    }

    [Fact]
    public async Task ConcurrentInvalidations_DoNotThrow()
    {
        // Arrange
        using var cache = new DocumentCache(CreateLogger());
        const int numberOfEntries = 20;

        // Pre-populate
        for (int i = 0; i < numberOfEntries; i++)
        {
            var key = $"doc{i}";
            var content = $"Content {i}";
            var hash = Serialization.CanonicalSerializer.GenerateHash(content);
            cache.Set(key, content, hash);
        }

        var tasks = new List<Task>();

        // Act - Concurrent invalidations
        for (int i = 0; i < numberOfEntries; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                var key = $"doc{index}";
                cache.Invalidate(key);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public async Task ConcurrentEvictions_DoNotThrow()
    {
        // Arrange
        using var cache = new DocumentCache(CreateLogger(), TimeSpan.FromMilliseconds(100));

        // Pre-populate
        for (int i = 0; i < 20; i++)
        {
            var key = $"doc{i}";
            var content = $"Content {i}";
            var hash = Serialization.CanonicalSerializer.GenerateHash(content);
            cache.Set(key, content, hash);
        }

        // Wait for expiration
        await Task.Delay(150);

        var tasks = new List<Task<int>>();

        // Act - Concurrent evictions
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() => cache.EvictExpired()));
        }

        await Task.WhenAll(tasks);

        // Assert - Should not throw and cache should be empty
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public async Task ConcurrentCountReads_ReturnConsistentValues()
    {
        // Arrange
        using var cache = new DocumentCache(CreateLogger());

        // Pre-populate
        for (int i = 0; i < 10; i++)
        {
            var key = $"doc{i}";
            var content = $"Content {i}";
            var hash = Serialization.CanonicalSerializer.GenerateHash(content);
            cache.Set(key, content, hash);
        }

        var tasks = new List<Task<int>>();

        // Act - Multiple concurrent count reads
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() => cache.Count));
        }

        await Task.WhenAll(tasks);

        // Assert - All count reads should return the same value
        foreach (var task in tasks)
        {
            Assert.Equal(10, await task);
        }
    }

    [Fact]
    public async Task ConcurrentMixedOperations_MaintainCacheIntegrity()
    {
        // Arrange
        using var cache = new DocumentCache(CreateLogger());
        const int iterations = 200;
        var tasks = new List<Task>();

        // Act - Mix of all operations
        for (int i = 0; i < iterations; i++)
        {
            var index = i;
            // Use modulo for deterministic operation selection
            var operation = index % 5;

            tasks.Add(Task.Run(() =>
            {
                var key = $"doc{index % 10}";

                switch (operation)
                {
                    case 0: // Set
                        var content = $"Content {index}";
                        var hash = Serialization.CanonicalSerializer.GenerateHash(content);
                        cache.Set(key, content, hash);
                        break;

                    case 1: // Get
                        cache.TryGet(key, out _, out _);
                        break;

                    case 2: // Invalidate
                        cache.Invalidate(key);
                        break;

                    case 3: // Count
                        _ = cache.Count;
                        break;

                    case 4: // Evict
                        cache.EvictExpired();
                        break;
                }
            }));
        }

        // Wait for all operations to complete - should not throw
        await Task.WhenAll(tasks);

        // Assert - Cache should still be functional
        Assert.True(cache.Count >= 0);

        // Test that we can still perform operations
        cache.Set("final", "test", Serialization.CanonicalSerializer.GenerateHash("test"));
        Assert.True(cache.TryGet("final", out var finalContent, out _));
        Assert.Equal("test", finalContent);
    }

    [Fact]
    public async Task StressTest_HighConcurrency_DoesNotDeadlock()
    {
        // Arrange
        using var cache = new DocumentCache(CreateLogger());
        const int numberOfThreads = 50;
        const int operationsPerThread = 100;
        var tasks = new List<Task>();

        // Act - High concurrency stress test
        for (int thread = 0; thread < numberOfThreads; thread++)
        {
            var threadId = thread;
            tasks.Add(Task.Run(() =>
            {
                for (int op = 0; op < operationsPerThread; op++)
                {
                    var key = $"doc{threadId % 10}";
                    var content = $"Content from thread {threadId} op {op}";
                    var hash = Serialization.CanonicalSerializer.GenerateHash(content);

                    // Mix of operations
                    if (op % 3 == 0)
                    {
                        cache.Set(key, content, hash);
                    }
                    else if (op % 3 == 1)
                    {
                        cache.TryGet(key, out _, out _);
                    }
                    else
                    {
                        _ = cache.Count;
                    }
                }
            }));
        }

        // This should complete without deadlock
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
        var allTasksCompleted = Task.WhenAll(tasks);
        var firstCompleted = await Task.WhenAny(allTasksCompleted, timeoutTask);
        var completed = firstCompleted == allTasksCompleted;

        // Assert
        Assert.True(completed, "Operations should complete without deadlock");
        Assert.True(cache.Count >= 0 && cache.Count <= 10);
    }
}
