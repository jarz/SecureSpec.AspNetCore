using Microsoft.OpenApi.Models;

namespace SecureSpec.AspNetCore.Security;

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
