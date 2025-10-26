using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Security;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for OAuth2 Client Credentials flow implementation (AC 209-213).
/// </summary>
public class OAuth2ClientCredentialsTests
{
    #region AC 209-213: OAuth2 Client Credentials Flow Implementation

    [Fact]
    public void AddOAuth2ClientCredentials_CreatesSchemeWithCorrectType()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddOAuth2ClientCredentials("oauth2", builder =>
            builder.WithTokenUrl(new Uri("https://auth.example.com/token", UriKind.Absolute)));

        // Assert
        var schemes = options.Schemes;
        Assert.Single(schemes);
        Assert.True(schemes.ContainsKey("oauth2"));
        Assert.Equal(SecuritySchemeType.OAuth2, schemes["oauth2"].Type);
    }

    [Fact]
    public void AddOAuth2ClientCredentials_SetsClientCredentialsFlow()
    {
        // Arrange
        var options = new SecurityOptions();
        var tokenUrl = new Uri("https://auth.example.com/token", UriKind.Absolute);

        // Act
        options.AddOAuth2ClientCredentials("oauth2", builder =>
            builder.WithTokenUrl(tokenUrl));

        // Assert
        var scheme = options.Schemes["oauth2"];
        Assert.NotNull(scheme.Flows);
        Assert.NotNull(scheme.Flows.ClientCredentials);
        Assert.Equal(tokenUrl, scheme.Flows.ClientCredentials.TokenUrl);
    }

    [Fact]
    public void AddOAuth2ClientCredentials_WithTokenUrl_SetsTokenUrl()
    {
        // Arrange
        var options = new SecurityOptions();
        var tokenUrl = new Uri("https://auth.example.com/oauth/token", UriKind.Absolute);

        // Act
        options.AddOAuth2ClientCredentials("oauth2", builder =>
            builder.WithTokenUrl(tokenUrl));

        // Assert
        var scheme = options.Schemes["oauth2"];
        Assert.Equal(tokenUrl, scheme.Flows.ClientCredentials.TokenUrl);
    }

    [Fact]
    public void AddOAuth2ClientCredentials_WithRefreshUrl_SetsRefreshUrl()
    {
        // Arrange
        var options = new SecurityOptions();
        var tokenUrl = new Uri("https://auth.example.com/token", UriKind.Absolute);
        var refreshUrl = new Uri("https://auth.example.com/refresh", UriKind.Absolute);

        // Act
        options.AddOAuth2ClientCredentials("oauth2", builder => builder
            .WithTokenUrl(tokenUrl)
            .WithRefreshUrl(refreshUrl));

        // Assert
        var scheme = options.Schemes["oauth2"];
        Assert.Equal(refreshUrl, scheme.Flows.ClientCredentials.RefreshUrl);
    }

    [Fact]
    public void AddOAuth2ClientCredentials_WithDescription_SetsDescription()
    {
        // Arrange
        var options = new SecurityOptions();
        var tokenUrl = new Uri("https://auth.example.com/token", UriKind.Absolute);

        // Act
        options.AddOAuth2ClientCredentials("oauth2", builder => builder
            .WithTokenUrl(tokenUrl)
            .WithDescription("OAuth2 client credentials authentication"));

        // Assert
        var scheme = options.Schemes["oauth2"];
        Assert.Equal("OAuth2 client credentials authentication", scheme.Description);
    }

    [Fact]
    public void AddOAuth2ClientCredentials_WithScopes_AddsScopes()
    {
        // Arrange
        var options = new SecurityOptions();
        var tokenUrl = new Uri("https://auth.example.com/token", UriKind.Absolute);

        // Act
        options.AddOAuth2ClientCredentials("oauth2", builder => builder
            .WithTokenUrl(tokenUrl)
            .AddScope("read", "Read access")
            .AddScope("write", "Write access"));

        // Assert
        var scheme = options.Schemes["oauth2"];
        Assert.Equal(2, scheme.Flows.ClientCredentials.Scopes.Count);
        Assert.Equal("Read access", scheme.Flows.ClientCredentials.Scopes["read"]);
        Assert.Equal("Write access", scheme.Flows.ClientCredentials.Scopes["write"]);
    }

    [Fact]
    public void AddOAuth2ClientCredentials_WithMultipleScopes_PreservesAllScopes()
    {
        // Arrange
        var options = new SecurityOptions();
        var tokenUrl = new Uri("https://auth.example.com/token", UriKind.Absolute);

        // Act
        options.AddOAuth2ClientCredentials("oauth2", builder => builder
            .WithTokenUrl(tokenUrl)
            .AddScope("api", "Full API access")
            .AddScope("read:users", "Read user data")
            .AddScope("write:users", "Write user data"));

        // Assert
        var scheme = options.Schemes["oauth2"];
        Assert.Equal(3, scheme.Flows.ClientCredentials.Scopes.Count);
        Assert.True(scheme.Flows.ClientCredentials.Scopes.ContainsKey("api"));
        Assert.True(scheme.Flows.ClientCredentials.Scopes.ContainsKey("read:users"));
        Assert.True(scheme.Flows.ClientCredentials.Scopes.ContainsKey("write:users"));
    }

    [Fact]
    public void AddOAuth2ClientCredentials_WithFluentConfiguration_ChainsMethods()
    {
        // Arrange
        var options = new SecurityOptions();
        var tokenUrl = new Uri("https://auth.example.com/token", UriKind.Absolute);
        var refreshUrl = new Uri("https://auth.example.com/refresh", UriKind.Absolute);

        // Act
        options.AddOAuth2ClientCredentials("oauth2", builder => builder
            .WithTokenUrl(tokenUrl)
            .WithRefreshUrl(refreshUrl)
            .WithDescription("OAuth2 authentication")
            .AddScope("api", "Full access"));

        // Assert
        var scheme = options.Schemes["oauth2"];
        Assert.Equal(tokenUrl, scheme.Flows.ClientCredentials.TokenUrl);
        Assert.Equal(refreshUrl, scheme.Flows.ClientCredentials.RefreshUrl);
        Assert.Equal("OAuth2 authentication", scheme.Description);
        Assert.Single(scheme.Flows.ClientCredentials.Scopes);
    }

    [Fact]
    public void AddOAuth2ClientCredentials_WithoutConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            options.AddOAuth2ClientCredentials("oauth2", null!));
    }

    [Fact]
    public void AddOAuth2ClientCredentials_WithNullName_ThrowsArgumentException()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            options.AddOAuth2ClientCredentials(null!, builder =>
                builder.WithTokenUrl(new Uri("https://auth.example.com/token", UriKind.Absolute))));
    }

    [Fact]
    public void AddOAuth2ClientCredentials_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            options.AddOAuth2ClientCredentials("", builder =>
                builder.WithTokenUrl(new Uri("https://auth.example.com/token", UriKind.Absolute))));
    }

    [Fact]
    public void AddOAuth2ClientCredentials_WithoutTokenUrl_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            options.AddOAuth2ClientCredentials("oauth2", builder =>
                builder.AddScope("api", "Full access")));

        Assert.Contains("Token URL is required", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WithTokenUrl_WithNullUrl_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new OAuth2ClientCredentialsSchemeBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithTokenUrl(null!));
    }

    [Fact]
    public void WithTokenUrl_WithRelativeUrl_ThrowsArgumentException()
    {
        // Arrange
        var builder = new OAuth2ClientCredentialsSchemeBuilder();
        var relativeUri = new Uri("/token", UriKind.Relative);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => builder.WithTokenUrl(relativeUri));
        Assert.Contains("absolute URI", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WithRefreshUrl_WithNullUrl_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new OAuth2ClientCredentialsSchemeBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithRefreshUrl(null!));
    }

    [Fact]
    public void WithRefreshUrl_WithRelativeUrl_ThrowsArgumentException()
    {
        // Arrange
        var builder = new OAuth2ClientCredentialsSchemeBuilder();
        var relativeUri = new Uri("/refresh", UriKind.Relative);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => builder.WithRefreshUrl(relativeUri));
        Assert.Contains("absolute URI", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WithDescription_WithNullDescription_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new OAuth2ClientCredentialsSchemeBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithDescription(null!));
    }

    [Fact]
    public void WithDescription_WithEmptyDescription_ThrowsArgumentException()
    {
        // Arrange
        var builder = new OAuth2ClientCredentialsSchemeBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithDescription(""));
    }

    [Fact]
    public void WithDescription_WithWhitespaceDescription_ThrowsArgumentException()
    {
        // Arrange
        var builder = new OAuth2ClientCredentialsSchemeBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithDescription("   "));
    }

    [Fact]
    public void AddScope_WithNullName_ThrowsArgumentException()
    {
        // Arrange
        var builder = new OAuth2ClientCredentialsSchemeBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.AddScope(null!, "description"));
    }

    [Fact]
    public void AddScope_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var builder = new OAuth2ClientCredentialsSchemeBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.AddScope("", "description"));
    }

    [Fact]
    public void AddScope_WithNullDescription_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new OAuth2ClientCredentialsSchemeBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.AddScope("api", null!));
    }

    [Fact]
    public void AddOAuth2ClientCredentials_WithMultipleSchemes_StoresAllSchemes()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddOAuth2ClientCredentials("oauth2_1", builder => builder
            .WithTokenUrl(new Uri("https://auth1.example.com/token", UriKind.Absolute)));
        options.AddOAuth2ClientCredentials("oauth2_2", builder => builder
            .WithTokenUrl(new Uri("https://auth2.example.com/token", UriKind.Absolute)));

        // Assert
        Assert.Equal(2, options.Schemes.Count);
        Assert.True(options.Schemes.ContainsKey("oauth2_1"));
        Assert.True(options.Schemes.ContainsKey("oauth2_2"));
    }

    [Fact]
    public void AddOAuth2ClientCredentials_ScopesAreMutable_CanAddMultipleScopes()
    {
        // Arrange
        var options = new SecurityOptions();
        var tokenUrl = new Uri("https://auth.example.com/token", UriKind.Absolute);

        // Act
        options.AddOAuth2ClientCredentials("oauth2", builder =>
        {
            builder.WithTokenUrl(tokenUrl);
            builder.AddScope("scope1", "Description 1");
            builder.AddScope("scope2", "Description 2");
            builder.AddScope("scope3", "Description 3");
        });

        // Assert
        var scheme = options.Schemes["oauth2"];
        Assert.Equal(3, scheme.Flows.ClientCredentials.Scopes.Count);
    }

    [Fact]
    public void AddOAuth2ClientCredentials_DuplicateScopeName_OverwritesPreviousValue()
    {
        // Arrange
        var options = new SecurityOptions();
        var tokenUrl = new Uri("https://auth.example.com/token", UriKind.Absolute);

        // Act
        options.AddOAuth2ClientCredentials("oauth2", builder => builder
            .WithTokenUrl(tokenUrl)
            .AddScope("api", "First description")
            .AddScope("api", "Second description"));

        // Assert
        var scheme = options.Schemes["oauth2"];
        Assert.Single(scheme.Flows.ClientCredentials.Scopes);
        Assert.Equal("Second description", scheme.Flows.ClientCredentials.Scopes["api"]);
    }

    [Fact]
    public void OAuth2ClientCredentials_OnlyClientCredentialsFlowSet()
    {
        // Arrange
        var options = new SecurityOptions();
        var tokenUrl = new Uri("https://auth.example.com/token", UriKind.Absolute);

        // Act
        options.AddOAuth2ClientCredentials("oauth2", builder =>
            builder.WithTokenUrl(tokenUrl));

        // Assert
        var scheme = options.Schemes["oauth2"];
        Assert.NotNull(scheme.Flows.ClientCredentials);
        Assert.Null(scheme.Flows.AuthorizationCode);
        Assert.Null(scheme.Flows.Implicit);
        Assert.Null(scheme.Flows.Password);
    }

    [Fact]
    public void AddOAuth2ClientCredentials_EmptyScopes_CreatesSchemeWithoutScopes()
    {
        // Arrange
        var options = new SecurityOptions();
        var tokenUrl = new Uri("https://auth.example.com/token", UriKind.Absolute);

        // Act
        options.AddOAuth2ClientCredentials("oauth2", builder =>
            builder.WithTokenUrl(tokenUrl));

        // Assert
        var scheme = options.Schemes["oauth2"];
        Assert.Empty(scheme.Flows.ClientCredentials.Scopes);
    }

    [Fact]
    public void AddOAuth2ClientCredentials_IntegrationWithHttpBearer_BothSchemesWork()
    {
        // Arrange
        var options = new SecurityOptions();
        var tokenUrl = new Uri("https://auth.example.com/token", UriKind.Absolute);

        // Act
        options.AddHttpBearer("bearerAuth", builder =>
            builder.WithBearerFormat("JWT"));
        options.AddOAuth2ClientCredentials("oauth2", builder => builder
            .WithTokenUrl(tokenUrl)
            .AddScope("api", "Full access"));

        // Assert
        Assert.Equal(2, options.Schemes.Count);
        Assert.Equal(SecuritySchemeType.Http, options.Schemes["bearerAuth"].Type);
        Assert.Equal(SecuritySchemeType.OAuth2, options.Schemes["oauth2"].Type);
    }

    #endregion

    #region Scoped Client Authentication Tests (AC 209-213)

    [Fact]
    public void ClientCredentials_SupportsClientScopedAuthentication()
    {
        // Arrange
        var options = new SecurityOptions();
        var tokenUrl = new Uri("https://auth.example.com/token", UriKind.Absolute);

        // Act
        options.AddOAuth2ClientCredentials("oauth2", builder => builder
            .WithTokenUrl(tokenUrl)
            .AddScope("client:read", "Client read access")
            .AddScope("client:write", "Client write access"));

        // Assert
        var scheme = options.Schemes["oauth2"];
        Assert.Contains("client:read", scheme.Flows.ClientCredentials.Scopes.Keys);
        Assert.Contains("client:write", scheme.Flows.ClientCredentials.Scopes.Keys);
    }

    [Fact]
    public void ClientCredentials_SupportsPolicyToScopeMapping()
    {
        // Arrange
        var options = new SecurityOptions
        {
            PolicyToScope = policy => $"policy:{policy}"
        };

        // Act & Assert - Verify the mapping function is set
        Assert.NotNull(options.PolicyToScope);
        Assert.Equal("policy:admin", options.PolicyToScope("admin"));
        Assert.Equal("policy:user", options.PolicyToScope("user"));
    }

    [Fact]
    public void ClientCredentials_SupportsRoleToScopeMapping()
    {
        // Arrange
        var options = new SecurityOptions
        {
            RoleToScope = role => $"role:{role}"
        };

        // Act & Assert - Verify the mapping function is set
        Assert.NotNull(options.RoleToScope);
        Assert.Equal("role:admin", options.RoleToScope("admin"));
        Assert.Equal("role:user", options.RoleToScope("user"));
    }

    [Fact]
    public void ClientCredentials_TokenManagement_HasRequiredTokenUrl()
    {
        // Arrange
        var options = new SecurityOptions();
        var tokenUrl = new Uri("https://auth.example.com/oauth/token", UriKind.Absolute);

        // Act
        options.AddOAuth2ClientCredentials("oauth2", builder =>
            builder.WithTokenUrl(tokenUrl));

        // Assert - Verify token URL is properly configured for token management
        var scheme = options.Schemes["oauth2"];
        Assert.Equal(tokenUrl, scheme.Flows.ClientCredentials.TokenUrl);
        Assert.NotNull(scheme.Flows.ClientCredentials.TokenUrl);
    }

    [Fact]
    public void ClientCredentials_TokenManagement_SupportsOptionalRefreshUrl()
    {
        // Arrange
        var options = new SecurityOptions();
        var tokenUrl = new Uri("https://auth.example.com/token", UriKind.Absolute);
        var refreshUrl = new Uri("https://auth.example.com/refresh", UriKind.Absolute);

        // Act
        options.AddOAuth2ClientCredentials("oauth2", builder => builder
            .WithTokenUrl(tokenUrl)
            .WithRefreshUrl(refreshUrl));

        // Assert - Verify refresh URL is configured for token refresh management
        var scheme = options.Schemes["oauth2"];
        Assert.Equal(refreshUrl, scheme.Flows.ClientCredentials.RefreshUrl);
    }

    #endregion
}
