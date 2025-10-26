using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.UI;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for the SecureSpec UI extensions.
/// </summary>
public class SecureSpecUIExtensionsTests
{
    [Fact]
    public void UseSecureSpecUI_WithValidBuilder_ReturnsBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(new SecureSpecOptions());
        var serviceProvider = services.BuildServiceProvider();
        var app = new ApplicationBuilder(serviceProvider);

        // Act
        var result = app.UseSecureSpecUI();

        // Assert
        Assert.NotNull(result);
        Assert.Same(app, result);
    }

    [Fact]
    public void UseSecureSpecUI_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        IApplicationBuilder app = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            app.UseSecureSpecUI());
    }

    [Fact]
    public void UseSecureSpecUI_WithConfiguration_ReturnsBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var app = new ApplicationBuilder(serviceProvider);

        // Act
        var result = app.UseSecureSpecUI(options =>
        {
            options.DocumentTitle = "Test API";
            options.DeepLinking = false;
        });

        // Assert
        Assert.NotNull(result);
        Assert.Same(app, result);
    }

    [Fact]
    public void UseSecureSpecUI_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var app = new ApplicationBuilder(serviceProvider);
        Action<UIOptions> nullConfig = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            app.UseSecureSpecUI(nullConfig));
    }

    [Fact]
    public void UseSecureSpecUI_WithCustomRoutePrefix_ReturnsBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(new SecureSpecOptions());
        var serviceProvider = services.BuildServiceProvider();
        var app = new ApplicationBuilder(serviceProvider);

        // Act
        var result = app.UseSecureSpecUI("custom-docs");

        // Assert
        Assert.NotNull(result);
        Assert.Same(app, result);
    }
}
