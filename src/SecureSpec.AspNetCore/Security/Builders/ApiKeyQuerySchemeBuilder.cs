using Microsoft.OpenApi.Models;

namespace SecureSpec.AspNetCore.Security;

/// <summary>
/// Builder for API Key authentication in query parameter security schemes.
/// </summary>
public class ApiKeyQuerySchemeBuilder : SecuritySchemeBuilder
{
    private string? _description;
    private string _name = "api_key";

    /// <summary>
    /// Sets the description for the security scheme.
    /// </summary>
    /// <param name="description">The description.</param>
    /// <returns>This builder for chaining.</returns>
    public ApiKeyQuerySchemeBuilder WithDescription(string description)
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
    /// Sets the name of the query parameter.
    /// </summary>
    /// <param name="name">The query parameter name (will be sanitized).</param>
    /// <returns>This builder for chaining.</returns>
    public ApiKeyQuerySchemeBuilder WithName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _name = SanitizeHeaderName(name);
        return this;
    }

    /// <summary>
    /// Builds the OpenAPI security scheme for API Key authentication in query.
    /// </summary>
    /// <returns>The configured security scheme.</returns>
    public override OpenApiSecurityScheme Build()
    {
        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Query,
            Name = _name,
            Description = _description
        };

        return scheme;
    }
}
