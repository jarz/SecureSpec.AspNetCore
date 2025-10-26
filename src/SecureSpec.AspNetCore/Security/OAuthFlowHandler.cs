using Microsoft.AspNetCore.Http;
using SecureSpec.AspNetCore.Configuration;

namespace SecureSpec.AspNetCore.Security;

/// <summary>
/// Handles OAuth 2.0 Authorization Code flow with PKCE and CSRF protection.
/// </summary>
/// <remarks>
/// Implements the complete OAuth 2.0 Authorization Code flow with:
/// - Required PKCE (Proof Key for Code Exchange) per RFC 7636
/// - CSRF protection via double-submit cookie pattern
/// - State parameter validation
/// - Token exchange and refresh
/// </remarks>
public class OAuthFlowHandler
{
    private readonly CsrfTokenManager _csrfTokenManager;
    private readonly OAuthFlowConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="OAuthFlowHandler"/> class.
    /// </summary>
    /// <param name="configuration">The OAuth flow configuration.</param>
    /// <param name="csrfTokenManager">
    /// Optional CSRF token manager. If not provided, a default instance will be created.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when configuration is null.
    /// </exception>
    public OAuthFlowHandler(
        OAuthFlowConfiguration configuration,
        CsrfTokenManager? csrfTokenManager = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _configuration = configuration;
        _csrfTokenManager = csrfTokenManager ?? new CsrfTokenManager();
    }

    /// <summary>
    /// Builds an OAuth 2.0 authorization request with PKCE and CSRF protection.
    /// </summary>
    /// <param name="scopes">The OAuth scopes to request.</param>
    /// <param name="redirectUri">The redirect URI where the authorization response will be sent.</param>
    /// <param name="additionalParameters">
    /// Optional additional parameters to include in the authorization request.
    /// </param>
    /// <returns>An <see cref="OAuthAuthorizationRequest"/> containing the request details.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when scopes or redirectUri is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the configuration does not include an authorization URL.
    /// </exception>
    public OAuthAuthorizationRequest BuildAuthorizationRequest(
        IEnumerable<string> scopes,
        Uri redirectUri,
        IDictionary<string, string>? additionalParameters = null)
    {
        ArgumentNullException.ThrowIfNull(scopes);
        ArgumentNullException.ThrowIfNull(redirectUri);

        if (_configuration.AuthorizationUrl is null)
        {
            throw new InvalidOperationException(
                "Authorization URL is not configured for this OAuth flow.");
        }

        // Generate PKCE parameters
        var (codeVerifier, codeChallenge) = PkceGenerator.GeneratePkceParameters();

        // Generate OAuth state parameter
        var state = GenerateStateParameter();

        // Generate CSRF token associated with state
        var csrfToken = _csrfTokenManager.GenerateToken(state);

        // Build authorization URL
        var authorizationUrl = BuildAuthorizationUrl(
            _configuration.AuthorizationUrl,
            codeChallenge,
            state,
            scopes,
            redirectUri,
            additionalParameters);

        return new OAuthAuthorizationRequest
        {
            AuthorizationUrl = authorizationUrl,
            CodeVerifier = codeVerifier,
            State = state,
            CsrfToken = csrfToken,
            RedirectUri = redirectUri
        };
    }

    /// <summary>
    /// Validates an OAuth authorization response and extracts the authorization code.
    /// </summary>
    /// <param name="state">The state parameter from the authorization response.</param>
    /// <param name="csrfToken">
    /// The CSRF token from the cookie (double-submit pattern).
    /// </param>
    /// <param name="code">
    /// When this method returns successfully, contains the authorization code.
    /// </param>
    /// <param name="error">
    /// When validation fails, contains the error message; otherwise, null.
    /// </param>
    /// <returns>
    /// <c>true</c> if the response is valid; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method validates:
    /// - CSRF token matches and has not been used before (rotation)
    /// - State parameter matches the one associated with the CSRF token
    /// </remarks>
    public bool ValidateAuthorizationResponse(
        string? state,
        string? csrfToken,
        out string? code,
        out string? error)
    {
        code = null;
        error = null;

        if (string.IsNullOrWhiteSpace(state))
        {
            error = "Missing or invalid state parameter";
            return false;
        }

        if (string.IsNullOrWhiteSpace(csrfToken))
        {
            error = "Missing CSRF token";
            return false;
        }

        // Validate CSRF token and rotate it (prevents replay attacks)
        if (!_csrfTokenManager.ValidateAndRotateToken(csrfToken, out var expectedState))
        {
            error = "Invalid or expired CSRF token";
            return false;
        }

        // Validate state parameter matches
        if (!string.Equals(state, expectedState, StringComparison.Ordinal))
        {
            error = "State parameter mismatch";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Builds a token exchange request to exchange authorization code for access token.
    /// </summary>
    /// <param name="authorizationCode">The authorization code from the authorization response.</param>
    /// <param name="codeVerifier">The PKCE code verifier that was used in the authorization request.</param>
    /// <param name="redirectUri">The redirect URI that was used in the authorization request.</param>
    /// <returns>An <see cref="OAuthTokenRequest"/> containing the token request details.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when any parameter is empty or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the configuration does not include a token URL.
    /// </exception>
    public OAuthTokenRequest BuildTokenRequest(
        string authorizationCode,
        string codeVerifier,
        Uri redirectUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authorizationCode, nameof(authorizationCode));
        ArgumentException.ThrowIfNullOrWhiteSpace(codeVerifier, nameof(codeVerifier));
        ArgumentNullException.ThrowIfNull(redirectUri);

        if (_configuration.TokenUrl is null)
        {
            throw new InvalidOperationException(
                "Token URL is not configured for this OAuth flow.");
        }

        var parameters = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", authorizationCode },
            { "redirect_uri", redirectUri.AbsoluteUri },
            { "code_verifier", codeVerifier }
        };

        return new OAuthTokenRequest
        {
            TokenUrl = _configuration.TokenUrl,
            Parameters = parameters
        };
    }

    /// <summary>
    /// Builds a token refresh request to obtain a new access token using a refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token.</param>
    /// <param name="scopes">Optional scopes to request. If null, uses the original scopes.</param>
    /// <returns>An <see cref="OAuthTokenRequest"/> containing the refresh request details.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when refreshToken is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when refreshToken is empty or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the configuration does not include a refresh URL or token URL.
    /// </exception>
    public OAuthTokenRequest BuildRefreshTokenRequest(
        string refreshToken,
        IEnumerable<string>? scopes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken, nameof(refreshToken));

        var tokenUrl = _configuration.RefreshUrl ?? _configuration.TokenUrl;
        if (tokenUrl is null)
        {
            throw new InvalidOperationException(
                "Token URL or Refresh URL must be configured for token refresh.");
        }

        var parameters = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken }
        };

        if (scopes is not null)
        {
            var scopeList = scopes.ToList();
            if (scopeList.Count > 0)
            {
                parameters["scope"] = string.Join(" ", scopeList);
            }
        }

        return new OAuthTokenRequest
        {
            TokenUrl = tokenUrl,
            Parameters = parameters
        };
    }

    /// <summary>
    /// Generates a cryptographically secure state parameter.
    /// </summary>
    /// <returns>A Base64url-encoded state parameter.</returns>
    private static string GenerateStateParameter()
    {
        var stateBytes = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(stateBytes);

        return Convert.ToBase64String(stateBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>
    /// Builds the complete authorization URL with all parameters.
    /// </summary>
    private static Uri BuildAuthorizationUrl(
        Uri baseUrl,
        string codeChallenge,
        string state,
        IEnumerable<string> scopes,
        Uri redirectUri,
        IDictionary<string, string>? additionalParameters)
    {
        var queryParams = new Dictionary<string, string?>
        {
            { "response_type", "code" },
            { "code_challenge", codeChallenge },
            { "code_challenge_method", PkceGenerator.ChallengeMethod },
            { "state", state },
            { "redirect_uri", redirectUri.AbsoluteUri }
        };

        var scopeList = scopes.ToList();
        if (scopeList.Count > 0)
        {
            queryParams["scope"] = string.Join(" ", scopeList);
        }

        // Add any additional parameters
        if (additionalParameters is not null)
        {
            foreach (var (key, value) in additionalParameters)
            {
                if (!queryParams.ContainsKey(key))
                {
                    queryParams[key] = value;
                }
            }
        }

        var query = QueryString.Create(queryParams);
        var uriBuilder = new UriBuilder(baseUrl)
        {
            Query = query.ToString()
        };

        return uriBuilder.Uri;
    }
}

/// <summary>
/// Represents an OAuth 2.0 authorization request with PKCE and CSRF protection.
/// </summary>
public class OAuthAuthorizationRequest
{
    /// <summary>
    /// Gets or sets the complete authorization URL to redirect the user to.
    /// </summary>
    public required Uri AuthorizationUrl { get; init; }

    /// <summary>
    /// Gets or sets the PKCE code verifier.
    /// This must be stored securely and used in the token exchange.
    /// </summary>
    public required string CodeVerifier { get; init; }

    /// <summary>
    /// Gets or sets the OAuth state parameter.
    /// </summary>
    public required string State { get; init; }

    /// <summary>
    /// Gets or sets the CSRF token for double-submit cookie pattern.
    /// This should be set as a secure, HTTP-only cookie.
    /// </summary>
    public required string CsrfToken { get; init; }

    /// <summary>
    /// Gets or sets the redirect URI.
    /// </summary>
    public required Uri RedirectUri { get; init; }
}

/// <summary>
/// Represents an OAuth 2.0 token request (exchange or refresh).
/// </summary>
public class OAuthTokenRequest
{
    /// <summary>
    /// Gets or sets the token endpoint URL.
    /// </summary>
    public required Uri TokenUrl { get; init; }

    /// <summary>
    /// Gets or sets the request parameters to send to the token endpoint.
    /// </summary>
    public required Dictionary<string, string> Parameters { get; init; }
}
