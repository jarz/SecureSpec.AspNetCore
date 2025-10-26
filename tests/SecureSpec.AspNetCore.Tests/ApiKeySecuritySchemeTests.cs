using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Security;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for API Key security scheme implementation (AC 196-198).
/// </summary>
public class ApiKeySecuritySchemeTests
{
    #region AC 196-198: API Key Header Implementation

    [Fact]
    public void AddApiKeyHeader_CreatesSchemeWithCorrectType()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddApiKeyHeader("apiKeyHeader");

        // Assert
        var schemes = options.Schemes;
        Assert.Single(schemes);
        Assert.True(schemes.ContainsKey("apiKeyHeader"));
        Assert.Equal(SecuritySchemeType.ApiKey, schemes["apiKeyHeader"].Type);
    }

    [Fact]
    public void AddApiKeyHeader_SetsLocationToHeader()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddApiKeyHeader("apiKeyHeader");

        // Assert
        var scheme = options.Schemes["apiKeyHeader"];
        Assert.Equal(ParameterLocation.Header, scheme.In);
    }

    [Fact]
    public void AddApiKeyHeader_WithDefaultName_UsesXApiKey()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddApiKeyHeader("apiKeyHeader");

        // Assert
        var scheme = options.Schemes["apiKeyHeader"];
        Assert.Equal("X-API-Key", scheme.Name);
    }

    [Fact]
    public void AddApiKeyHeader_WithCustomName_SanitizesAndUsesName()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddApiKeyHeader("apiKeyHeader", builder =>
            builder.WithName("Custom-API-Key"));

        // Assert
        var scheme = options.Schemes["apiKeyHeader"];
        Assert.Equal("Custom-API-Key", scheme.Name);
    }

    [Fact]
    public void AddApiKeyHeader_WithDescription_SetsDescription()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddApiKeyHeader("apiKeyHeader", builder =>
            builder.WithDescription("API Key authentication via header"));

        // Assert
        var scheme = options.Schemes["apiKeyHeader"];
        Assert.Equal("API Key authentication via header", scheme.Description);
    }

    [Fact]
    public void AddApiKeyHeader_WithFluentConfiguration_ChainsMethods()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddApiKeyHeader("apiKeyHeader", builder => builder
            .WithName("Authorization-Key")
            .WithDescription("Custom API key in header"));

        // Assert
        var scheme = options.Schemes["apiKeyHeader"];
        Assert.Equal("Authorization-Key", scheme.Name);
        Assert.Equal("Custom API key in header", scheme.Description);
    }

    [Fact]
    public void AddApiKeyHeader_WithNullName_ThrowsArgumentException()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => options.AddApiKeyHeader(null!));
    }

    [Fact]
    public void AddApiKeyHeader_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => options.AddApiKeyHeader(""));
    }

    #endregion

    #region AC 196-198: API Key Query Implementation

    [Fact]
    public void AddApiKeyQuery_CreatesSchemeWithCorrectType()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddApiKeyQuery("apiKeyQuery");

        // Assert
        var schemes = options.Schemes;
        Assert.Single(schemes);
        Assert.True(schemes.ContainsKey("apiKeyQuery"));
        Assert.Equal(SecuritySchemeType.ApiKey, schemes["apiKeyQuery"].Type);
    }

    [Fact]
    public void AddApiKeyQuery_SetsLocationToQuery()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddApiKeyQuery("apiKeyQuery");

        // Assert
        var scheme = options.Schemes["apiKeyQuery"];
        Assert.Equal(ParameterLocation.Query, scheme.In);
    }

    [Fact]
    public void AddApiKeyQuery_WithDefaultName_UsesApiKey()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddApiKeyQuery("apiKeyQuery");

        // Assert
        var scheme = options.Schemes["apiKeyQuery"];
        Assert.Equal("api_key", scheme.Name);
    }

    [Fact]
    public void AddApiKeyQuery_WithCustomName_SanitizesAndUsesName()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddApiKeyQuery("apiKeyQuery", builder =>
            builder.WithName("custom_api_key"));

        // Assert
        var scheme = options.Schemes["apiKeyQuery"];
        Assert.Equal("custom_api_key", scheme.Name);
    }

    [Fact]
    public void AddApiKeyQuery_WithDescription_SetsDescription()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddApiKeyQuery("apiKeyQuery", builder =>
            builder.WithDescription("API Key authentication via query parameter"));

        // Assert
        var scheme = options.Schemes["apiKeyQuery"];
        Assert.Equal("API Key authentication via query parameter", scheme.Description);
    }

    [Fact]
    public void AddApiKeyQuery_WithFluentConfiguration_ChainsMethods()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddApiKeyQuery("apiKeyQuery", builder => builder
            .WithName("access_token")
            .WithDescription("Query parameter API key"));

        // Assert
        var scheme = options.Schemes["apiKeyQuery"];
        Assert.Equal("access_token", scheme.Name);
        Assert.Equal("Query parameter API key", scheme.Description);
    }

    [Fact]
    public void AddApiKeyQuery_WithNullName_ThrowsArgumentException()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => options.AddApiKeyQuery(null!));
    }

    [Fact]
    public void AddApiKeyQuery_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => options.AddApiKeyQuery(""));
    }

    #endregion

    #region Name Sanitization Tests

    [Fact]
    public void ApiKeyHeader_WithName_SanitizesControlCharacters()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddApiKeyHeader("apiKeyHeader", builder =>
            builder.WithName("API\r\nKey"));

        // Assert
        var scheme = options.Schemes["apiKeyHeader"];
        Assert.Equal("APIKey", scheme.Name);
        Assert.DoesNotContain("\r", scheme.Name, StringComparison.Ordinal);
        Assert.DoesNotContain("\n", scheme.Name, StringComparison.Ordinal);
    }

    [Fact]
    public void ApiKeyHeader_WithName_SanitizesWhitespace()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddApiKeyHeader("apiKeyHeader", builder =>
            builder.WithName("API Key"));

        // Assert
        var scheme = options.Schemes["apiKeyHeader"];
        Assert.Equal("APIKey", scheme.Name);
        Assert.DoesNotContain(" ", scheme.Name, StringComparison.Ordinal);
    }

    [Fact]
    public void ApiKeyHeader_WithName_NormalizesUnicode()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act - Using composed vs decomposed Unicode
        options.AddApiKeyHeader("scheme1", builder =>
            builder.WithName("Authorizatión")); // Single character ó
        options.AddApiKeyHeader("scheme2", builder =>
            builder.WithName("Authorizatio\u0301n")); // o + combining acute accent

        // Assert - Both should normalize to the same form
        Assert.Equal(
            options.Schemes["scheme1"].Name,
            options.Schemes["scheme2"].Name);
    }

    [Fact]
    public void ApiKeyQuery_WithName_SanitizesControlCharacters()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddApiKeyQuery("apiKeyQuery", builder =>
            builder.WithName("api\r\nkey"));

        // Assert
        var scheme = options.Schemes["apiKeyQuery"];
        Assert.Equal("apikey", scheme.Name);
        Assert.DoesNotContain("\r", scheme.Name, StringComparison.Ordinal);
        Assert.DoesNotContain("\n", scheme.Name, StringComparison.Ordinal);
    }

    [Fact]
    public void ApiKeyQuery_WithName_SanitizesWhitespace()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddApiKeyQuery("apiKeyQuery", builder =>
            builder.WithName("api key"));

        // Assert
        var scheme = options.Schemes["apiKeyQuery"];
        Assert.Equal("apikey", scheme.Name);
        Assert.DoesNotContain(" ", scheme.Name, StringComparison.Ordinal);
    }

    [Fact]
    public void ApiKeyQuery_WithName_NormalizesUnicode()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act - Using composed vs decomposed Unicode
        options.AddApiKeyQuery("scheme1", builder =>
            builder.WithName("tokén")); // Single character é
        options.AddApiKeyQuery("scheme2", builder =>
            builder.WithName("toke\u0301n")); // e + combining acute accent

        // Assert - Both should normalize to the same form
        Assert.Equal(
            options.Schemes["scheme1"].Name,
            options.Schemes["scheme2"].Name);
    }

    [Fact]
    public void ApiKeyHeader_WithOnlyInvalidCharacters_ThrowsArgumentException()
    {
        // Arrange
        var builder = new ApiKeyHeaderSchemeBuilder();

        // Act & Assert
        // The sanitization removes all invalid characters, leaving an empty string
        // which is then rejected by ThrowIfNullOrWhiteSpace
        Assert.Throws<ArgumentException>(() => builder.WithName("\r\n\t"));
    }

    [Fact]
    public void ApiKeyQuery_WithOnlyInvalidCharacters_ThrowsArgumentException()
    {
        // Arrange
        var builder = new ApiKeyQuerySchemeBuilder();

        // Act & Assert
        // The sanitization removes all invalid characters, leaving an empty string
        // which is then rejected by ThrowIfNullOrWhiteSpace
        Assert.Throws<ArgumentException>(() => builder.WithName("\r\n\t"));
    }

    #endregion

    #region Builder Validation Tests

    [Fact]
    public void ApiKeyHeaderBuilder_WithNullDescription_ThrowsArgumentException()
    {
        // Arrange
        var builder = new ApiKeyHeaderSchemeBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithDescription(null!));
    }

    [Fact]
    public void ApiKeyHeaderBuilder_WithEmptyDescription_ThrowsArgumentException()
    {
        // Arrange
        var builder = new ApiKeyHeaderSchemeBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithDescription(""));
    }

    [Fact]
    public void ApiKeyHeaderBuilder_WithWhitespaceDescription_ThrowsArgumentException()
    {
        // Arrange
        var builder = new ApiKeyHeaderSchemeBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithDescription("   "));
    }

    [Fact]
    public void ApiKeyQueryBuilder_WithNullDescription_ThrowsArgumentException()
    {
        // Arrange
        var builder = new ApiKeyQuerySchemeBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithDescription(null!));
    }

    [Fact]
    public void ApiKeyQueryBuilder_WithEmptyDescription_ThrowsArgumentException()
    {
        // Arrange
        var builder = new ApiKeyQuerySchemeBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithDescription(""));
    }

    [Fact]
    public void ApiKeyQueryBuilder_WithWhitespaceDescription_ThrowsArgumentException()
    {
        // Arrange
        var builder = new ApiKeyQuerySchemeBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithDescription("   "));
    }

    [Fact]
    public void ApiKeyHeaderBuilder_WithNullName_ThrowsArgumentException()
    {
        // Arrange
        var builder = new ApiKeyHeaderSchemeBuilder();

        // Act & Assert - ThrowIfNullOrWhiteSpace throws ArgumentNullException for null
        Assert.Throws<ArgumentNullException>(() => builder.WithName(null!));
    }

    [Fact]
    public void ApiKeyHeaderBuilder_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var builder = new ApiKeyHeaderSchemeBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithName(""));
    }

    [Fact]
    public void ApiKeyQueryBuilder_WithNullName_ThrowsArgumentException()
    {
        // Arrange
        var builder = new ApiKeyQuerySchemeBuilder();

        // Act & Assert - ThrowIfNullOrWhiteSpace throws ArgumentNullException for null
        Assert.Throws<ArgumentNullException>(() => builder.WithName(null!));
    }

    [Fact]
    public void ApiKeyQueryBuilder_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var builder = new ApiKeyQuerySchemeBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithName(""));
    }

    #endregion

    #region Multiple Schemes Tests

    [Fact]
    public void SecurityOptions_WithMultipleApiKeySchemes_StoresAllSchemes()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddApiKeyHeader("headerAuth1", builder => builder.WithName("X-API-Key-1"));
        options.AddApiKeyHeader("headerAuth2", builder => builder.WithName("X-API-Key-2"));
        options.AddApiKeyQuery("queryAuth1", builder => builder.WithName("api_key_1"));
        options.AddApiKeyQuery("queryAuth2", builder => builder.WithName("api_key_2"));

        // Assert
        Assert.Equal(4, options.Schemes.Count);
        Assert.Equal("X-API-Key-1", options.Schemes["headerAuth1"].Name);
        Assert.Equal("X-API-Key-2", options.Schemes["headerAuth2"].Name);
        Assert.Equal("api_key_1", options.Schemes["queryAuth1"].Name);
        Assert.Equal("api_key_2", options.Schemes["queryAuth2"].Name);
    }

    [Fact]
    public void SecurityOptions_MixedSecuritySchemes_AllStoredCorrectly()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddHttpBearer("bearerAuth");
        options.AddApiKeyHeader("apiKeyHeader");
        options.AddApiKeyQuery("apiKeyQuery");

        // Assert
        Assert.Equal(3, options.Schemes.Count);
        Assert.Equal(SecuritySchemeType.Http, options.Schemes["bearerAuth"].Type);
        Assert.Equal(SecuritySchemeType.ApiKey, options.Schemes["apiKeyHeader"].Type);
        Assert.Equal(SecuritySchemeType.ApiKey, options.Schemes["apiKeyQuery"].Type);
        Assert.Equal(ParameterLocation.Header, options.Schemes["apiKeyHeader"].In);
        Assert.Equal(ParameterLocation.Query, options.Schemes["apiKeyQuery"].In);
    }

    #endregion
}
