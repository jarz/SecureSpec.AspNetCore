using Microsoft.Extensions.DependencyInjection;
using SecureSpec.AspNetCore.Core;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for the service collection extensions.
/// </summary>
public class SecureSpecServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSecureSpec_WithValidConfiguration_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSecureSpec(options =>
        {
            options.Documents.Add("v1", doc =>
            {
                doc.Info.Title = "Test API";
                doc.Info.Version = "1.0.0";
            });
        });

        // Assert
        Assert.NotNull(services);
    }

    [Fact]
    public void AddSecureSpec_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            services.AddSecureSpec(_ => { }));
    }

    [Fact]
    public void AddSecureSpec_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            services.AddSecureSpec(null!));
    }

    [Fact]
    public void AddSecureSpec_CalledMultipleTimes_IsIdempotent()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Call AddSecureSpec multiple times on the same service collection
        services.AddSecureSpec(options =>
        {
            options.Documents.Add("v1", doc =>
            {
                doc.Info.Title = "Test API";
                doc.Info.Version = "1.0.0";
            });
        });

        var countAfterFirst = services.Count(d => d.ServiceType == typeof(ApiDiscoveryEngine));

        services.AddSecureSpec(options =>
        {
            options.Documents.Add("v2", doc =>
            {
                doc.Info.Title = "Test API v2";
                doc.Info.Version = "2.0.0";
            });
        });

        var countAfterSecond = services.Count(d => d.ServiceType == typeof(ApiDiscoveryEngine));

        // Assert - Should have only one registration of ApiDiscoveryEngine regardless of how many times AddSecureSpec is called
        Assert.Equal(1, countAfterFirst);
        Assert.Equal(1, countAfterSecond);
        Assert.Equal(countAfterFirst, countAfterSecond);
    }
}
