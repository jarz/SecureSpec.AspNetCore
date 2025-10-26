using Microsoft.OpenApi.Models;

namespace SecureSpec.AspNetCore.Security;

/// <summary>
/// Builder for API Key authentication in header security schemes.
/// </summary>
public class ApiKeyHeaderSchemeBuilder : SecuritySchemeBuilder
{
    private string? _description;
    private string _name = "X-API-Key";

    /// <summary>
    /// Sets the description for the security scheme.
    /// </summary>
    /// <param name="description">The description.</param>
    /// <returns>This builder for chaining.</returns>
    public ApiKeyHeaderSchemeBuilder WithDescription(string description)
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
    /// Sets the name of the header parameter.
    /// </summary>
    /// <param name="name">The header parameter name (will be sanitized).</param>
    /// <returns>This builder for chaining.</returns>
    public ApiKeyHeaderSchemeBuilder WithName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _name = SanitizeHeaderName(name);
        return this;
    }

    /// <summary>
    /// Builds the OpenAPI security scheme for API Key authentication in header.
    /// </summary>
    /// <returns>The configured security scheme.</returns>
    public override OpenApiSecurityScheme Build()
    {
        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = _name,
            Description = _description
        };

        return scheme;
    }
}
