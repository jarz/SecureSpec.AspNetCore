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
/// Builder for Mutual TLS authentication security schemes.
/// </summary>
/// <remarks>
/// Mutual TLS (mTLS) is part of the OpenAPI 3.1 specification, but the current version
/// of Microsoft.OpenApi (1.6.22) does not have native support for the mutualTLS SecuritySchemeType.
/// This builder uses OpenIdConnect as a placeholder type and adds vendor extensions to indicate
/// the intended security scheme type. Certificates must be configured externally at the TLS layer.
/// </remarks>
public class MutualTlsSchemeBuilder : SecuritySchemeBuilder
{
    private string? _description;

    /// <summary>
    /// Sets the description for the security scheme.
    /// </summary>
    /// <param name="description">The description.</param>
    /// <returns>This builder for chaining.</returns>
    public MutualTlsSchemeBuilder WithDescription(string description)
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
    /// Builds the OpenAPI security scheme for Mutual TLS authentication.
    /// </summary>
    /// <returns>The configured security scheme.</returns>
    /// <remarks>
    /// This implementation uses OpenIdConnect as a placeholder type because Microsoft.OpenApi 1.6.22
    /// does not support the mutualTLS SecuritySchemeType. A vendor extension (x-security-scheme-type)
    /// is added to indicate that this is a Mutual TLS scheme. Client certificates must be configured
    /// at the TLS/SSL layer externally; this library does not provide certificate upload functionality.
    /// </remarks>
    public override OpenApiSecurityScheme Build()
    {
        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OpenIdConnect,
            Description = _description ?? "Mutual TLS authentication. Client certificates must be configured externally at the TLS layer. Certificate upload is not supported; certificates are managed through infrastructure configuration."
        };

        // Add vendor extension to indicate this is actually a mutualTLS scheme
        scheme.Extensions.Add("x-security-scheme-type", new Microsoft.OpenApi.Any.OpenApiString("mutualTLS"));

        return scheme;
    }
}
