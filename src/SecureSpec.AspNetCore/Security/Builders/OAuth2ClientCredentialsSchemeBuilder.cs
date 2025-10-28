using Microsoft.OpenApi.Models;

namespace SecureSpec.AspNetCore.Security;

/// <summary>
/// Builder for OAuth2 Client Credentials flow security schemes.
/// </summary>
public class OAuth2ClientCredentialsSchemeBuilder : SecuritySchemeBuilder
{
    private string? _description;
    private Uri? _tokenUrl;
    private Uri? _refreshUrl;
    private readonly Dictionary<string, string> _scopes = new(StringComparer.Ordinal);

    /// <summary>
    /// Sets the description for the security scheme.
    /// </summary>
    /// <param name="description">The description.</param>
    /// <returns>This builder for chaining.</returns>
    public OAuth2ClientCredentialsSchemeBuilder WithDescription(string description)
    {
        ArgumentNullException.ThrowIfNull(description);
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description cannot be empty or whitespace.", nameof(description));
        }

        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the token URL for the OAuth2 flow.
    /// </summary>
    /// <param name="tokenUrl">The token endpoint URL.</param>
    /// <returns>This builder for chaining.</returns>
    public OAuth2ClientCredentialsSchemeBuilder WithTokenUrl(Uri tokenUrl)
    {
        ArgumentNullException.ThrowIfNull(tokenUrl);
        if (!tokenUrl.IsAbsoluteUri)
        {
            throw new ArgumentException("Token URL must be an absolute URI.", nameof(tokenUrl));
        }

        _tokenUrl = tokenUrl;
        return this;
    }

    /// <summary>
    /// Sets the refresh URL for the OAuth2 flow.
    /// </summary>
    /// <param name="refreshUrl">The refresh endpoint URL.</param>
    /// <returns>This builder for chaining.</returns>
    public OAuth2ClientCredentialsSchemeBuilder WithRefreshUrl(Uri refreshUrl)
    {
        ArgumentNullException.ThrowIfNull(refreshUrl);
        if (!refreshUrl.IsAbsoluteUri)
        {
            throw new ArgumentException("Refresh URL must be an absolute URI.", nameof(refreshUrl));
        }

        _refreshUrl = refreshUrl;
        return this;
    }

    /// <summary>
    /// Adds a scope to the OAuth2 flow.
    /// </summary>
    /// <param name="name">The scope name.</param>
    /// <param name="description">The scope description.</param>
    /// <returns>This builder for chaining.</returns>
    public OAuth2ClientCredentialsSchemeBuilder AddScope(string name, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(description);

        _scopes[name] = description;
        return this;
    }

    /// <summary>
    /// Builds the OpenAPI security scheme for OAuth2 Client Credentials flow.
    /// </summary>
    /// <returns>The configured security scheme.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required configuration is missing.</exception>
    public override OpenApiSecurityScheme Build()
    {
        if (_tokenUrl == null)
        {
            throw new InvalidOperationException("Token URL is required for OAuth2 Client Credentials flow.");
        }

        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Description = _description,
            Flows = new OpenApiOAuthFlows
            {
                ClientCredentials = new OpenApiOAuthFlow
                {
                    TokenUrl = _tokenUrl,
                    RefreshUrl = _refreshUrl,
                    Scopes = new Dictionary<string, string>(_scopes, StringComparer.Ordinal)
                }
            }
        };

        return scheme;
    }
}
