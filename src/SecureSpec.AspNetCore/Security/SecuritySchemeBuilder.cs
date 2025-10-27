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
