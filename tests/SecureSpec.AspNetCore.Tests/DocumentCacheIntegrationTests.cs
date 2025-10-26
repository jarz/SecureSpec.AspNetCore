using Microsoft.Extensions.DependencyInjection;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Core;
using SecureSpec.AspNetCore.Diagnostics;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Integration tests for DocumentCache service registration.
/// </summary>
public class DocumentCacheIntegrationTests
{
    [Fact]
    public void DocumentCache_IsRegisteredAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSecureSpec(_ => { });

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var cache1 = serviceProvider.GetRequiredService<DocumentCache>();
        var cache2 = serviceProvider.GetRequiredService<DocumentCache>();

        // Assert
        Assert.NotNull(cache1);
        Assert.NotNull(cache2);
        Assert.Same(cache1, cache2); // Singleton should return same instance
    }

    [Fact]
    public void DocumentCache_UsesConfiguredExpiration()
    {
        // Arrange
        var customExpiration = TimeSpan.FromMinutes(15);
        var services = new ServiceCollection();
        services.AddSecureSpec(options =>
        {
            options.Cache.DefaultExpiration = customExpiration;
        });

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var cache = serviceProvider.GetRequiredService<DocumentCache>();
        const string testContent = "test content";
        var testHash = Serialization.CanonicalSerializer.GenerateHash(testContent);
        cache.Set("test", testContent, testHash);

        // Assert - Entry should not expire immediately
        var result = cache.TryGet("test", out var content, out var hash);
        Assert.True(result);
        Assert.Equal(testContent, content);
        Assert.Equal(testHash, hash);
    }

    [Fact]
    public void DiagnosticsLogger_IsRegisteredAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSecureSpec(_ => { });

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var logger1 = serviceProvider.GetRequiredService<DiagnosticsLogger>();
        var logger2 = serviceProvider.GetRequiredService<DiagnosticsLogger>();

        // Assert
        Assert.NotNull(logger1);
        Assert.NotNull(logger2);
        Assert.Same(logger1, logger2);
    }

    [Fact]
    public void DocumentCache_ReceivesDiagnosticsLogger()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSecureSpec(_ => { });

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var logger = serviceProvider.GetRequiredService<DiagnosticsLogger>();
        var cache = serviceProvider.GetRequiredService<DocumentCache>();

        // Perform cache operation that logs
        cache.TryGet("nonexistent", out _, out _);

        // Assert - Logger should have recorded the cache miss
        var events = logger.GetEvents();
        Assert.Contains(events, e => e.Code == "CACHE002"); // Cache miss code
    }

    [Fact]
    public void CacheOptions_DefaultValues_AreSet()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSecureSpec(_ => { });

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SecureSpecOptions>>().Value;

        // Assert
        Assert.NotNull(options.Cache);
        Assert.Equal(TimeSpan.FromMinutes(5), options.Cache.DefaultExpiration);
        Assert.True(options.Cache.Enabled);
        Assert.True(options.Cache.ValidateIntegrity);
        Assert.Null(options.Cache.AutoEvictionInterval);
    }

    [Fact]
    public void CacheOptions_CanBeConfigured()
    {
        // Arrange
        var customExpiration = TimeSpan.FromMinutes(10);
        var customEvictionInterval = TimeSpan.FromMinutes(1);

        var services = new ServiceCollection();
        services.AddSecureSpec(options =>
        {
            options.Cache.DefaultExpiration = customExpiration;
            options.Cache.Enabled = false;
            options.Cache.ValidateIntegrity = false;
            options.Cache.AutoEvictionInterval = customEvictionInterval;
        });

        var serviceProvider = services.BuildServiceProvider();
        var configuredOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SecureSpecOptions>>().Value;

        // Assert
        Assert.Equal(customExpiration, configuredOptions.Cache.DefaultExpiration);
        Assert.False(configuredOptions.Cache.Enabled);
        Assert.False(configuredOptions.Cache.ValidateIntegrity);
        Assert.Equal(customEvictionInterval, configuredOptions.Cache.AutoEvictionInterval);
    }

    [Fact]
    public void MultipleServiceProviders_GetIndependentCacheInstances()
    {
        // Arrange
        var services1 = new ServiceCollection();
        services1.AddSecureSpec(_ => { });
        var provider1 = services1.BuildServiceProvider();

        var services2 = new ServiceCollection();
        services2.AddSecureSpec(_ => { });
        var provider2 = services2.BuildServiceProvider();

        // Act
        var cache1 = provider1.GetRequiredService<DocumentCache>();
        var cache2 = provider2.GetRequiredService<DocumentCache>();

        const string testContent = "test";
        var testHash = Serialization.CanonicalSerializer.GenerateHash(testContent);
        cache1.Set("key", testContent, testHash);

        // Assert - Caches from different providers should be independent
        Assert.NotSame(cache1, cache2);
        Assert.True(cache1.TryGet("key", out _, out _));
        Assert.False(cache2.TryGet("key", out _, out _)); // cache2 should not have the entry
    }
}
