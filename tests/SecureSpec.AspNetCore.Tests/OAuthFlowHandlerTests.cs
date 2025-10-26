using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Security;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for OAuth 2.0 Authorization Code flow handler with PKCE and CSRF protection.
/// </summary>
public class OAuthFlowHandlerTests
{
    private static OAuthFlowConfiguration CreateTestConfiguration()
    {
        return new OAuthFlowConfiguration
        {
            AuthorizationUrl = new Uri("https://auth.example.com/authorize", UriKind.Absolute),
            TokenUrl = new Uri("https://auth.example.com/token", UriKind.Absolute),
            RefreshUrl = new Uri("https://auth.example.com/refresh", UriKind.Absolute)
        };
    }

    [Fact]
    public void Constructor_WithValidConfiguration_CreatesHandler()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var handler = new OAuthFlowHandler(config);

        // Assert
        Assert.NotNull(handler);
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OAuthFlowHandler(null!));
    }

    [Fact]
    public void Constructor_WithCustomCsrfManager_UsesProvidedManager()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var csrfManager = new CsrfTokenManager();

        // Act
        var handler = new OAuthFlowHandler(config, csrfManager);

        // Assert
        Assert.NotNull(handler);
    }

    [Fact]
    public void BuildAuthorizationRequest_WithValidParameters_ReturnsRequest()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var handler = new OAuthFlowHandler(config);
        var scopes = new[] { "read", "write" };
        var redirectUri = new Uri("https://app.example.com/callback", UriKind.Absolute);

        // Act
        var request = handler.BuildAuthorizationRequest(scopes, redirectUri);

        // Assert
        Assert.NotNull(request);
        Assert.NotNull(request.AuthorizationUrl);
        Assert.NotNull(request.CodeVerifier);
        Assert.NotNull(request.State);
        Assert.NotNull(request.CsrfToken);
        Assert.Equal(redirectUri, request.RedirectUri);
    }

    [Fact]
    public void BuildAuthorizationRequest_AuthorizationUrlContainsRequiredParameters()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var handler = new OAuthFlowHandler(config);
        var scopes = new[] { "read", "write" };
        var redirectUri = new Uri("https://app.example.com/callback", UriKind.Absolute);

        // Act
        var request = handler.BuildAuthorizationRequest(scopes, redirectUri);

        // Assert
        var url = request.AuthorizationUrl.ToString();
        Assert.Contains("response_type=code", url, StringComparison.Ordinal);
        Assert.Contains("code_challenge=", url, StringComparison.Ordinal);
        Assert.Contains("code_challenge_method=S256", url, StringComparison.Ordinal);
        Assert.Contains("state=", url, StringComparison.Ordinal);
        Assert.Contains("redirect_uri=", url, StringComparison.Ordinal);
        // URL encoding may use either %20 or + for spaces, or scopes may be split across params
        Assert.Contains("scope=", url, StringComparison.Ordinal);
        Assert.Contains("read", url, StringComparison.Ordinal);
        Assert.Contains("write", url, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildAuthorizationRequest_GeneratesUniqueRequests()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var handler = new OAuthFlowHandler(config);
        var scopes = new[] { "read" };
        var redirectUri = new Uri("https://app.example.com/callback", UriKind.Absolute);

        // Act
        var request1 = handler.BuildAuthorizationRequest(scopes, redirectUri);
        var request2 = handler.BuildAuthorizationRequest(scopes, redirectUri);

        // Assert
        Assert.NotEqual(request1.CodeVerifier, request2.CodeVerifier);
        Assert.NotEqual(request1.State, request2.State);
        Assert.NotEqual(request1.CsrfToken, request2.CsrfToken);
    }

    [Fact]
    public void BuildAuthorizationRequest_WithNullScopes_ThrowsArgumentNullException()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var handler = new OAuthFlowHandler(config);
        var redirectUri = new Uri("https://app.example.com/callback", UriKind.Absolute);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            handler.BuildAuthorizationRequest(null!, redirectUri));
    }

    [Fact]
    public void BuildAuthorizationRequest_WithNullRedirectUri_ThrowsArgumentNullException()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var handler = new OAuthFlowHandler(config);
        var scopes = new[] { "read" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            handler.BuildAuthorizationRequest(scopes, null!));
    }

    [Fact]
    public void BuildAuthorizationRequest_WithoutAuthorizationUrl_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new OAuthFlowConfiguration
        {
            TokenUrl = new Uri("https://auth.example.com/token", UriKind.Absolute)
        };
        var handler = new OAuthFlowHandler(config);
        var scopes = new[] { "read" };
        var redirectUri = new Uri("https://app.example.com/callback", UriKind.Absolute);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            handler.BuildAuthorizationRequest(scopes, redirectUri));
        Assert.Contains("Authorization URL", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildAuthorizationRequest_WithAdditionalParameters_IncludesThemInUrl()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var handler = new OAuthFlowHandler(config);
        var scopes = new[] { "read" };
        var redirectUri = new Uri("https://app.example.com/callback", UriKind.Absolute);
        var additionalParams = new Dictionary<string, string>
        {
            { "audience", "https://api.example.com" },
            { "prompt", "consent" }
        };

        // Act
        var request = handler.BuildAuthorizationRequest(scopes, redirectUri, additionalParams);

        // Assert
        var url = request.AuthorizationUrl.ToString();
        Assert.Contains("audience=", url, StringComparison.Ordinal);
        Assert.Contains("prompt=", url, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateAuthorizationResponse_WithValidParameters_ReturnsTrue()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var csrfManager = new CsrfTokenManager();
        var handler = new OAuthFlowHandler(config, csrfManager);

        var scopes = new[] { "read" };
        var redirectUri = new Uri("https://app.example.com/callback", UriKind.Absolute);
        var authRequest = handler.BuildAuthorizationRequest(scopes, redirectUri);

        // Act
        var isValid = handler.ValidateAuthorizationResponse(
            authRequest.State,
            authRequest.CsrfToken,
            out var code,
            out var error);

        // Assert
        Assert.True(isValid);
        Assert.Null(code);
        Assert.Null(error);
    }

    [Fact]
    public void ValidateAuthorizationResponse_WithMissingState_ReturnsFalse()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var handler = new OAuthFlowHandler(config);

        // Act
        var isValid = handler.ValidateAuthorizationResponse(
            null,
            "some-csrf-token",
            out var code,
            out var error);

        // Assert
        Assert.False(isValid);
        Assert.Null(code);
        Assert.NotNull(error);
        Assert.Contains("state", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateAuthorizationResponse_WithMissingCsrfToken_ReturnsFalse()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var handler = new OAuthFlowHandler(config);

        // Act
        var isValid = handler.ValidateAuthorizationResponse(
            "some-state",
            null,
            out var code,
            out var error);

        // Assert
        Assert.False(isValid);
        Assert.Null(code);
        Assert.NotNull(error);
        Assert.Contains("CSRF", error, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateAuthorizationResponse_WithInvalidCsrfToken_ReturnsFalse()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var handler = new OAuthFlowHandler(config);

        // Act
        var isValid = handler.ValidateAuthorizationResponse(
            "some-state",
            "invalid-csrf-token",
            out var code,
            out var error);

        // Assert
        Assert.False(isValid);
        Assert.Null(code);
        Assert.NotNull(error);
        Assert.Contains("CSRF", error, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateAuthorizationResponse_WithStateMismatch_ReturnsFalse()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var csrfManager = new CsrfTokenManager();
        var handler = new OAuthFlowHandler(config, csrfManager);

        var scopes = new[] { "read" };
        var redirectUri = new Uri("https://app.example.com/callback", UriKind.Absolute);
        var authRequest = handler.BuildAuthorizationRequest(scopes, redirectUri);

        // Act - Use different state than what was generated
        var isValid = handler.ValidateAuthorizationResponse(
            "different-state",
            authRequest.CsrfToken,
            out var code,
            out var error);

        // Assert
        Assert.False(isValid);
        Assert.Null(code);
        Assert.NotNull(error);
        Assert.Contains("mismatch", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateAuthorizationResponse_WithReplayedCsrfToken_ReturnsFalse()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var csrfManager = new CsrfTokenManager();
        var handler = new OAuthFlowHandler(config, csrfManager);

        var scopes = new[] { "read" };
        var redirectUri = new Uri("https://app.example.com/callback", UriKind.Absolute);
        var authRequest = handler.BuildAuthorizationRequest(scopes, redirectUri);

        // Act - First validation should succeed
        var firstValidation = handler.ValidateAuthorizationResponse(
            authRequest.State,
            authRequest.CsrfToken,
            out _,
            out _);

        // Second validation with same token should fail (token rotation)
        var replayValidation = handler.ValidateAuthorizationResponse(
            authRequest.State,
            authRequest.CsrfToken,
            out var code,
            out var error);

        // Assert
        Assert.True(firstValidation);
        Assert.False(replayValidation);
        Assert.Null(code);
        Assert.NotNull(error);
    }

    [Fact]
    public void BuildTokenRequest_WithValidParameters_ReturnsRequest()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var handler = new OAuthFlowHandler(config);
        const string authCode = "test-authorization-code";
        const string verifier = "test-code-verifier-with-minimum-length-forty-three";
        var redirectUri = new Uri("https://app.example.com/callback", UriKind.Absolute);

        // Act
        var tokenRequest = handler.BuildTokenRequest(authCode, verifier, redirectUri);

        // Assert
        Assert.NotNull(tokenRequest);
        Assert.Equal(config.TokenUrl, tokenRequest.TokenUrl);
        Assert.Contains("grant_type", tokenRequest.Parameters.Keys);
        Assert.Equal("authorization_code", tokenRequest.Parameters["grant_type"]);
        Assert.Equal(authCode, tokenRequest.Parameters["code"]);
        Assert.Equal(verifier, tokenRequest.Parameters["code_verifier"]);
        Assert.Equal(redirectUri.AbsoluteUri, tokenRequest.Parameters["redirect_uri"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void BuildTokenRequest_WithInvalidAuthorizationCode_ThrowsArgumentException(string? code)
    {
        // Arrange
        var config = CreateTestConfiguration();
        var handler = new OAuthFlowHandler(config);
        const string verifier = "test-code-verifier-with-minimum-length-forty-three";
        var redirectUri = new Uri("https://app.example.com/callback", UriKind.Absolute);

        // Act & Assert
        if (code is null)
        {
            Assert.Throws<ArgumentNullException>(() =>
                handler.BuildTokenRequest(code!, verifier, redirectUri));
        }
        else
        {
            Assert.Throws<ArgumentException>(() =>
                handler.BuildTokenRequest(code, verifier, redirectUri));
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void BuildTokenRequest_WithInvalidCodeVerifier_ThrowsArgumentException(string? verifier)
    {
        // Arrange
        var config = CreateTestConfiguration();
        var handler = new OAuthFlowHandler(config);
        const string authCode = "test-authorization-code";
        var redirectUri = new Uri("https://app.example.com/callback", UriKind.Absolute);

        // Act & Assert
        if (verifier is null)
        {
            Assert.Throws<ArgumentNullException>(() =>
                handler.BuildTokenRequest(authCode, verifier!, redirectUri));
        }
        else
        {
            Assert.Throws<ArgumentException>(() =>
                handler.BuildTokenRequest(authCode, verifier, redirectUri));
        }
    }

    [Fact]
    public void BuildTokenRequest_WithNullRedirectUri_ThrowsArgumentNullException()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var handler = new OAuthFlowHandler(config);
        const string authCode = "test-authorization-code";
        const string verifier = "test-code-verifier-with-minimum-length-forty-three";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            handler.BuildTokenRequest(authCode, verifier, null!));
    }

    [Fact]
    public void BuildTokenRequest_WithoutTokenUrl_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new OAuthFlowConfiguration
        {
            AuthorizationUrl = new Uri("https://auth.example.com/authorize", UriKind.Absolute)
        };
        var handler = new OAuthFlowHandler(config);
        const string authCode = "test-authorization-code";
        const string verifier = "test-code-verifier-with-minimum-length-forty-three";
        var redirectUri = new Uri("https://app.example.com/callback", UriKind.Absolute);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            handler.BuildTokenRequest(authCode, verifier, redirectUri));
        Assert.Contains("Token URL", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildRefreshTokenRequest_WithValidParameters_ReturnsRequest()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var handler = new OAuthFlowHandler(config);
        const string refreshToken = "test-refresh-token";

        // Act
        var tokenRequest = handler.BuildRefreshTokenRequest(refreshToken);

        // Assert
        Assert.NotNull(tokenRequest);
        Assert.Equal(config.RefreshUrl, tokenRequest.TokenUrl);
        Assert.Contains("grant_type", tokenRequest.Parameters.Keys);
        Assert.Equal("refresh_token", tokenRequest.Parameters["grant_type"]);
        Assert.Equal(refreshToken, tokenRequest.Parameters["refresh_token"]);
    }

    [Fact]
    public void BuildRefreshTokenRequest_WithScopes_IncludesScopesInRequest()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var handler = new OAuthFlowHandler(config);
        const string refreshToken = "test-refresh-token";
        var scopes = new[] { "read", "write" };

        // Act
        var tokenRequest = handler.BuildRefreshTokenRequest(refreshToken, scopes);

        // Assert
        Assert.Contains("scope", tokenRequest.Parameters.Keys);
        Assert.Equal("read write", tokenRequest.Parameters["scope"]);
    }

    [Fact]
    public void BuildRefreshTokenRequest_WithoutRefreshUrl_UsesTokenUrl()
    {
        // Arrange
        var config = new OAuthFlowConfiguration
        {
            TokenUrl = new Uri("https://auth.example.com/token", UriKind.Absolute)
        };
        var handler = new OAuthFlowHandler(config);
        const string refreshToken = "test-refresh-token";

        // Act
        var tokenRequest = handler.BuildRefreshTokenRequest(refreshToken);

        // Assert
        Assert.Equal(config.TokenUrl, tokenRequest.TokenUrl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void BuildRefreshTokenRequest_WithInvalidRefreshToken_ThrowsArgumentException(string? refreshToken)
    {
        // Arrange
        var config = CreateTestConfiguration();
        var handler = new OAuthFlowHandler(config);

        // Act & Assert
        if (refreshToken is null)
        {
            Assert.Throws<ArgumentNullException>(() =>
                handler.BuildRefreshTokenRequest(refreshToken!));
        }
        else
        {
            Assert.Throws<ArgumentException>(() =>
                handler.BuildRefreshTokenRequest(refreshToken));
        }
    }

    [Fact]
    public void BuildRefreshTokenRequest_WithoutTokenOrRefreshUrl_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new OAuthFlowConfiguration
        {
            AuthorizationUrl = new Uri("https://auth.example.com/authorize", UriKind.Absolute)
        };
        var handler = new OAuthFlowHandler(config);
        const string refreshToken = "test-refresh-token";

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            handler.BuildRefreshTokenRequest(refreshToken));
        Assert.Contains("Token URL", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CompleteOAuthFlow_Integration_WorksCorrectly()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var csrfManager = new CsrfTokenManager();
        var handler = new OAuthFlowHandler(config, csrfManager);

        var scopes = new[] { "read", "write" };
        var redirectUri = new Uri("https://app.example.com/callback", UriKind.Absolute);

        // Act - Step 1: Build authorization request
        var authRequest = handler.BuildAuthorizationRequest(scopes, redirectUri);

        // Step 2: Simulate authorization response
        var validationResult = handler.ValidateAuthorizationResponse(
            authRequest.State,
            authRequest.CsrfToken,
            out _,
            out var error);

        // Step 3: Build token exchange request
        const string authCode = "received-authorization-code";
        var tokenRequest = handler.BuildTokenRequest(
            authCode,
            authRequest.CodeVerifier,
            redirectUri);

        // Assert
        Assert.NotNull(authRequest);
        Assert.True(validationResult);
        Assert.Null(error);
        Assert.NotNull(tokenRequest);
        Assert.Equal("authorization_code", tokenRequest.Parameters["grant_type"]);
        Assert.Equal(authCode, tokenRequest.Parameters["code"]);
        Assert.Equal(authRequest.CodeVerifier, tokenRequest.Parameters["code_verifier"]);
    }
}
