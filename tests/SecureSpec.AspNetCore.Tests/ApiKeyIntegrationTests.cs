using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Integration tests for API Key security schemes with service registration (AC 196-198).
/// </summary>
public class ApiKeyIntegrationTests
{
    [Fact]
    public void AddSecureSpec_WithApiKeyHeader_RegistersSchemeCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSecureSpec(options =>
        {
            options.Security.AddApiKeyHeader("apiKeyAuth", builder => builder
                .WithName("X-API-Key")
                .WithDescription("API Key authentication"));
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var secureSpecOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SecureSpecOptions>>();

        var schemes = secureSpecOptions.Value.Security.Schemes;
        Assert.Single(schemes);
        Assert.True(schemes.ContainsKey("apiKeyAuth"));

        var scheme = schemes["apiKeyAuth"];
        Assert.Equal(SecuritySchemeType.ApiKey, scheme.Type);
        Assert.Equal(ParameterLocation.Header, scheme.In);
        Assert.Equal("X-API-Key", scheme.Name);
        Assert.Equal("API Key authentication", scheme.Description);
    }

    [Fact]
    public void AddSecureSpec_WithApiKeyQuery_RegistersSchemeCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSecureSpec(options =>
        {
            options.Security.AddApiKeyQuery("apiKeyAuth", builder => builder
                .WithName("api_key")
                .WithDescription("Query parameter API key"));
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var secureSpecOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SecureSpecOptions>>();

        var schemes = secureSpecOptions.Value.Security.Schemes;
        Assert.Single(schemes);
        Assert.True(schemes.ContainsKey("apiKeyAuth"));

        var scheme = schemes["apiKeyAuth"];
        Assert.Equal(SecuritySchemeType.ApiKey, scheme.Type);
        Assert.Equal(ParameterLocation.Query, scheme.In);
        Assert.Equal("api_key", scheme.Name);
        Assert.Equal("Query parameter API key", scheme.Description);
    }

    [Fact]
    public void AddSecureSpec_WithMultipleApiKeySchemes_RegistersAllSchemes()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSecureSpec(options =>
        {
            options.Security.AddApiKeyHeader("headerAuth", builder => builder
                .WithName("X-API-Key"));

            options.Security.AddApiKeyQuery("queryAuth", builder => builder
                .WithName("api_key"));

            options.Security.AddHttpBearer("bearerAuth");
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var secureSpecOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SecureSpecOptions>>();

        var schemes = secureSpecOptions.Value.Security.Schemes;
        Assert.Equal(3, schemes.Count);

        // Verify header API key
        Assert.True(schemes.ContainsKey("headerAuth"));
        Assert.Equal(SecuritySchemeType.ApiKey, schemes["headerAuth"].Type);
        Assert.Equal(ParameterLocation.Header, schemes["headerAuth"].In);

        // Verify query API key
        Assert.True(schemes.ContainsKey("queryAuth"));
        Assert.Equal(SecuritySchemeType.ApiKey, schemes["queryAuth"].Type);
        Assert.Equal(ParameterLocation.Query, schemes["queryAuth"].In);

        // Verify bearer
        Assert.True(schemes.ContainsKey("bearerAuth"));
        Assert.Equal(SecuritySchemeType.Http, schemes["bearerAuth"].Type);
    }

    [Fact]
    public void AddSecureSpec_WithApiKeyHeaderDefaults_UsesDefaultName()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSecureSpec(options =>
        {
            options.Security.AddApiKeyHeader("apiKeyAuth");
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var secureSpecOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SecureSpecOptions>>();

        var scheme = secureSpecOptions.Value.Security.Schemes["apiKeyAuth"];
        Assert.Equal("X-API-Key", scheme.Name);
        Assert.Null(scheme.Description);
    }

    [Fact]
    public void AddSecureSpec_WithApiKeyQueryDefaults_UsesDefaultName()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSecureSpec(options =>
        {
            options.Security.AddApiKeyQuery("apiKeyAuth");
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var secureSpecOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SecureSpecOptions>>();

        var scheme = secureSpecOptions.Value.Security.Schemes["apiKeyAuth"];
        Assert.Equal("api_key", scheme.Name);
        Assert.Null(scheme.Description);
    }

    [Fact]
    public void AddSecureSpec_WithApiKeyHeaderSanitization_SanitizesName()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSecureSpec(options =>
        {
            options.Security.AddApiKeyHeader("apiKeyAuth", builder => builder
                .WithName("X-API\r\n-Key")); // Contains CRLF
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var secureSpecOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SecureSpecOptions>>();

        var scheme = secureSpecOptions.Value.Security.Schemes["apiKeyAuth"];
        Assert.Equal("X-API-Key", scheme.Name);
        Assert.DoesNotContain("\r", scheme.Name, StringComparison.Ordinal);
        Assert.DoesNotContain("\n", scheme.Name, StringComparison.Ordinal);
    }

    [Fact]
    public void AddSecureSpec_WithApiKeyQuerySanitization_SanitizesName()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSecureSpec(options =>
        {
            options.Security.AddApiKeyQuery("apiKeyAuth", builder => builder
                .WithName("api key")); // Contains whitespace
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var secureSpecOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SecureSpecOptions>>();

        var scheme = secureSpecOptions.Value.Security.Schemes["apiKeyAuth"];
        Assert.Equal("apikey", scheme.Name);
        Assert.DoesNotContain(" ", scheme.Name, StringComparison.Ordinal);
    }

    [Fact]
    public void AddSecureSpec_WithComplexApiKeyConfiguration_AllPropertiesSet()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSecureSpec(options =>
        {
            options.Documents.Add("v1", doc =>
            {
                doc.Info.Title = "Test API";
                doc.Info.Version = "1.0";
            });

            options.Security.AddApiKeyHeader("headerAuth", builder => builder
                .WithName("X-Custom-API-Key")
                .WithDescription("Custom API key in header"));

            options.Security.AddApiKeyQuery("queryAuth", builder => builder
                .WithName("access_token")
                .WithDescription("Access token in query"));
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var secureSpecOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SecureSpecOptions>>();

        Assert.Single(secureSpecOptions.Value.Documents);
        Assert.Equal(2, secureSpecOptions.Value.Security.Schemes.Count);

        var headerScheme = secureSpecOptions.Value.Security.Schemes["headerAuth"];
        Assert.Equal("X-Custom-API-Key", headerScheme.Name);
        Assert.Equal("Custom API key in header", headerScheme.Description);

        var queryScheme = secureSpecOptions.Value.Security.Schemes["queryAuth"];
        Assert.Equal("access_token", queryScheme.Name);
        Assert.Equal("Access token in query", queryScheme.Description);
    }
}
