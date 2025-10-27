using Microsoft.OpenApi.Models;

namespace SecureSpec.AspNetCore.Security;

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
