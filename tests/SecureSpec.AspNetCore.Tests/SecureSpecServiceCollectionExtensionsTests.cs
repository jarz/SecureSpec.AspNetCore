using Microsoft.Extensions.DependencyInjection;

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
}
