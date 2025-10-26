using Microsoft.OpenApi.Models;

namespace SecureSpec.AspNetCore.Security;

/// <summary>
/// Base class for building OpenAPI security schemes with header sanitization.
/// </summary>
public abstract class SecuritySchemeBuilder
{
    /// <summary>
    /// Builds the OpenAPI security scheme.
    /// </summary>
    /// <returns>The configured security scheme.</returns>
    public abstract OpenApiSecurityScheme Build();

    /// <summary>
    /// Sanitizes a header or parameter name according to security best practices.
    /// Removes control characters, normalizes Unicode, and validates allowed characters.
    /// </summary>
    /// <param name="name">The name to sanitize.</param>
    /// <returns>The sanitized name.</returns>
    protected static string SanitizeHeaderName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        // Normalize Unicode to prevent homograph attacks
        var normalized = name.Normalize(System.Text.NormalizationForm.FormC);

        // Remove control characters and whitespace (CRLF protection)
        var sanitized = new string(normalized.Where(c =>
            !char.IsControl(c) &&
            !char.IsWhiteSpace(c)).ToArray());

        if (string.IsNullOrEmpty(sanitized))
        {
            throw new ArgumentException("Header name contains only invalid characters.", nameof(name));
        }

        return sanitized;
    }
}

/// <summary>
/// Builder for HTTP Bearer authentication security schemes.
/// </summary>
public class HttpBearerSchemeBuilder : SecuritySchemeBuilder
{
    private string? _description;
    private string? _bearerFormat;

    /// <summary>
    /// Sets the description for the security scheme.
    /// </summary>
    /// <param name="description">The description.</param>
    /// <returns>This builder for chaining.</returns>
    public HttpBearerSchemeBuilder WithDescription(string description)
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
    /// Sets the bearer format (e.g., "JWT").
    /// </summary>
    /// <param name="format">The bearer token format.</param>
    /// <returns>This builder for chaining.</returns>
    public HttpBearerSchemeBuilder WithBearerFormat(string format)
    {
        ArgumentNullException.ThrowIfNull(format);
        if (string.IsNullOrWhiteSpace(format))
        {
            throw new ArgumentException("Format cannot be whitespace.", nameof(format));
        }
        _bearerFormat = format;
        return this;
    }

    /// <summary>
    /// Builds the OpenAPI security scheme for HTTP Bearer authentication.
    /// </summary>
    /// <returns>The configured security scheme.</returns>
    public override OpenApiSecurityScheme Build()
    {
        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = _bearerFormat,
            Description = _description
        };

        return scheme;
    }
}

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
