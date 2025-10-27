using SecureSpec.AspNetCore.Core;
using SecureSpec.AspNetCore.Diagnostics;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for the DocumentCache class.
/// </summary>
public class DocumentCacheTests
{
    private DiagnosticsLogger CreateLogger() => new DiagnosticsLogger();

    [Fact]
    public void Constructor_SetsDefaultExpiration()
    {
        // Arrange & Act
        using var cache = new DocumentCache(CreateLogger());

        // Assert
        Assert.NotNull(cache);
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void Constructor_WithCustomExpiration_SetsExpiration()
    {
        // Arrange & Act
        var customExpiration = TimeSpan.FromMinutes(10);
        using var cache = new DocumentCache(CreateLogger(), customExpiration);

        // Assert
        Assert.NotNull(cache);
    }

    [Fact]
    public void Constructor_WithZeroExpiration_ThrowsArgumentOutOfRangeException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new DocumentCache(CreateLogger(), TimeSpan.Zero));
    }

    [Fact]
    public void Constructor_WithNegativeExpiration_ThrowsArgumentOutOfRangeException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new DocumentCache(CreateLogger(), TimeSpan.FromMinutes(-1)));
    }

    [Fact]
    public void TryGet_WithEmptyCache_ReturnsFalse()
    {
        // Arrange
        using var cache = new DocumentCache(CreateLogger());

        // Act
        var result = cache.TryGet("doc1", out var content, out var hash);

        // Assert
        Assert.False(result);
        Assert.Null(content);
        Assert.Null(hash);
    }

    [Fact]
    public void Set_AndTryGet_ReturnsStoredValues()
    {
        // Arrange
        using var cache = new DocumentCache(CreateLogger());
        const string key = "doc1";
        const string testContent = "Test OpenAPI document content";
        var testHash = Serialization.CanonicalSerializer.GenerateHash(testContent);

        // Act
        cache.Set(key, testContent, testHash);
        var result = cache.TryGet(key, out var content, out var hash);

        // Assert
        Assert.True(result);
        Assert.Equal(testContent, content);
        Assert.Equal(testHash, hash);
    }

    [Fact]
    public void Set_MultipleTimes_UpdatesCache()
    {
        // Arrange
        using var cache = new DocumentCache(CreateLogger());
        const string key = "doc1";
        const string content1 = "Content 1";
        const string content2 = "Content 2";
        var hash1 = Serialization.CanonicalSerializer.GenerateHash(content1);
        var hash2 = Serialization.CanonicalSerializer.GenerateHash(content2);

        // Act
        cache.Set(key, content1, hash1);
        cache.Set(key, content2, hash2);
        var result = cache.TryGet(key, out var content, out var hash);

        // Assert
        Assert.True(result);
        Assert.Equal(content2, content);
        Assert.Equal(hash2, hash);
    }

    [Fact]
    public void Count_ReturnsCorrectNumberOfEntries()
    {
        // Arrange
        using var cache = new DocumentCache(CreateLogger());

        // Act & Assert
        Assert.Equal(0, cache.Count);

        cache.Set("doc1", "content1", Serialization.CanonicalSerializer.GenerateHash("content1"));
        Assert.Equal(1, cache.Count);

        cache.Set("doc2", "content2", Serialization.CanonicalSerializer.GenerateHash("content2"));
        Assert.Equal(2, cache.Count);

        cache.Set("doc1", "content1-updated", Serialization.CanonicalSerializer.GenerateHash("content1-updated"));
        Assert.Equal(2, cache.Count); // Update doesn't increase count
    }

    [Fact]
    public void Invalidate_RemovesEntry()
    {
        // Arrange
        using var cache = new DocumentCache(CreateLogger());
        const string key = "doc1";
        cache.Set(key, "content", Serialization.CanonicalSerializer.GenerateHash("content"));

        // Act
        var invalidated = cache.Invalidate(key);
        var result = cache.TryGet(key, out _, out _);

        // Assert
        Assert.True(invalidated);
        Assert.False(result);
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void Invalidate_NonExistentKey_ReturnsFalse()
    {
        // Arrange
        using var cache = new DocumentCache(CreateLogger());

        // Act
        var invalidated = cache.Invalidate("nonexistent");

        // Assert
        Assert.False(invalidated);
    }

    [Fact]
    public void InvalidateAll_ClearsAllEntries()
    {
        // Arrange
        using var cache = new DocumentCache(CreateLogger());
        cache.Set("doc1", "content1", Serialization.CanonicalSerializer.GenerateHash("content1"));
        cache.Set("doc2", "content2", Serialization.CanonicalSerializer.GenerateHash("content2"));
        cache.Set("doc3", "content3", Serialization.CanonicalSerializer.GenerateHash("content3"));

        // Act
        cache.InvalidateAll();

        // Assert
        Assert.Equal(0, cache.Count);
        Assert.False(cache.TryGet("doc1", out _, out _));
        Assert.False(cache.TryGet("doc2", out _, out _));
        Assert.False(cache.TryGet("doc3", out _, out _));
    }

    [Fact]
    public void TryGet_WithExpiredEntry_ReturnsFalse()
    {
        // Arrange
        using var cache = new DocumentCache(CreateLogger(), TimeSpan.FromMilliseconds(50));
        const string key = "doc1";
        cache.Set(key, "content", Serialization.CanonicalSerializer.GenerateHash("content"));

        // Act
        Thread.Sleep(100); // Wait for expiration
        var result = cache.TryGet(key, out var content, out var hash);

        // Assert
        Assert.False(result);
        Assert.Null(content);
        Assert.Null(hash);
    }

    [Fact]
    public void EvictExpired_RemovesExpiredEntries()
    {
        // Arrange
        using var cache = new DocumentCache(CreateLogger(), TimeSpan.FromMilliseconds(50));
        cache.Set("doc1", "content1", Serialization.CanonicalSerializer.GenerateHash("content1"));
        cache.Set("doc2", "content2", Serialization.CanonicalSerializer.GenerateHash("content2"));

        // Act
        Thread.Sleep(100); // Wait for expiration
        var evictedCount = cache.EvictExpired();

        // Assert
        Assert.Equal(2, evictedCount);
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void EvictExpired_WithNoExpiredEntries_ReturnsZero()
    {
        // Arrange
        using var cache = new DocumentCache(CreateLogger(), TimeSpan.FromMinutes(10));
        cache.Set("doc1", "content1", Serialization.CanonicalSerializer.GenerateHash("content1"));

        // Act
        var evictedCount = cache.EvictExpired();

        // Assert
        Assert.Equal(0, evictedCount);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void EvictExpired_WithMixedEntries_RemovesOnlyExpired()
    {
        // Arrange
        using var cache = new DocumentCache(CreateLogger());

        // Add an entry with short expiration
        cache.Set("doc1", "content1", Serialization.CanonicalSerializer.GenerateHash("content1"), TimeSpan.FromMilliseconds(50));

        // Wait a bit
        Thread.Sleep(100);

        // Add an entry with long expiration (after the first has expired)
        cache.Set("doc2", "content2", Serialization.CanonicalSerializer.GenerateHash("content2"), TimeSpan.FromMinutes(10));

        // Act
        var evictedCount = cache.EvictExpired();

        // Assert
        Assert.Equal(1, evictedCount);
        Assert.Equal(1, cache.Count);
        Assert.False(cache.TryGet("doc1", out _, out _));
        Assert.True(cache.TryGet("doc2", out _, out _));
    }

    [Fact]
    public void TryGet_WithInvalidHash_ReturnsFalse()
    {
        // Arrange
        var logger = CreateLogger();
        using var cache = new DocumentCache(logger);
        const string key = "doc1";
        const string content = "content";
        const string invalidHash = "0000000000000000000000000000000000000000000000000000000000000000";

        // Act
        cache.Set(key, content, invalidHash);
        var result = cache.TryGet(key, out var retrievedContent, out var retrievedHash);

        // Assert
        Assert.False(result);
        Assert.Null(retrievedContent);
        Assert.Null(retrievedHash);

        // Verify that a warning was logged
        var events = logger.GetEvents();
        Assert.Contains(events, e => e.Code == "CACHE001" && e.Level == DiagnosticLevel.Warn);
    }

    [Fact]
    public void Set_WithNullOrWhiteSpaceKey_ThrowsArgumentException()
    {
        // Arrange
        using var cache = new DocumentCache(CreateLogger());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => cache.Set("", "content", "hash"));
        Assert.Throws<ArgumentException>(() => cache.Set("   ", "content", "hash"));
    }

    [Fact]
    public void Set_WithNullOrWhiteSpaceContent_ThrowsArgumentException()
    {
        // Arrange
        using var cache = new DocumentCache(CreateLogger());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => cache.Set("key", "", "hash"));
        Assert.Throws<ArgumentException>(() => cache.Set("key", "   ", "hash"));
    }

    [Fact]
    public void Set_WithNullOrWhiteSpaceHash_ThrowsArgumentException()
    {
        // Arrange
        using var cache = new DocumentCache(CreateLogger());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => cache.Set("key", "content", ""));
        Assert.Throws<ArgumentException>(() => cache.Set("key", "content", "   "));
    }

    [Fact]
    public void Set_WithZeroCustomExpiration_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var cache = new DocumentCache(CreateLogger());

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            cache.Set("key", "content", "hash", TimeSpan.Zero));
    }

    [Fact]
    public void TryGet_WithNullOrWhiteSpaceKey_ThrowsArgumentException()
    {
        // Arrange
        using var cache = new DocumentCache(CreateLogger());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => cache.TryGet("", out _, out _));
        Assert.Throws<ArgumentException>(() => cache.TryGet("   ", out _, out _));
    }

    [Fact]
    public void Invalidate_WithNullOrWhiteSpaceKey_ThrowsArgumentException()
    {
        // Arrange
        using var cache = new DocumentCache(CreateLogger());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => cache.Invalidate(""));
        Assert.Throws<ArgumentException>(() => cache.Invalidate("   "));
    }

    [Fact]
    public void Dispose_AllowsOnlyOnce()
    {
        // Arrange
        var cache = new DocumentCache(CreateLogger());

        // Act
        cache.Dispose();
        cache.Dispose(); // Should not throw

        // Assert
        Assert.Throws<ObjectDisposedException>(() => cache.Set("key", "content", "hash"));
    }

    [Fact]
    public void Operations_AfterDispose_ThrowObjectDisposedException()
    {
        // Arrange
        var cache = new DocumentCache(CreateLogger());
        cache.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => cache.Set("key", "content", "hash"));
        Assert.Throws<ObjectDisposedException>(() => cache.TryGet("key", out _, out _));
        Assert.Throws<ObjectDisposedException>(() => cache.Invalidate("key"));
        Assert.Throws<ObjectDisposedException>(() => cache.InvalidateAll());
        Assert.Throws<ObjectDisposedException>(() => cache.EvictExpired());
        Assert.Throws<ObjectDisposedException>(() => _ = cache.Count);
    }

    [Fact]
    public void DiagnosticEvents_AreLoggedCorrectly()
    {
        // Arrange
        var logger = CreateLogger();
        using var cache = new DocumentCache(logger);
        const string key = "doc1";
        const string content = "content";
        var hash = Serialization.CanonicalSerializer.GenerateHash(content);

        // Act
        cache.TryGet(key, out _, out _); // Miss
        cache.Set(key, content, hash); // Set
        cache.TryGet(key, out _, out _); // Hit
        cache.Invalidate(key); // Invalidate

        // Assert
        var events = logger.GetEvents();
        Assert.Contains(events, e => e.Code == "CACHE002"); // Cache miss
        Assert.Contains(events, e => e.Code == "CACHE005"); // Cache set
        Assert.Contains(events, e => e.Code == "CACHE004"); // Cache hit
        Assert.Contains(events, e => e.Code == "CACHE006"); // Invalidate
    }
}
