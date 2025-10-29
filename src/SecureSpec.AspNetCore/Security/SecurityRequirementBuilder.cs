using Microsoft.OpenApi.Models;

namespace SecureSpec.AspNetCore.Security;

/// <summary>
/// Builder for creating OpenAPI security requirements with proper AND/OR semantics.
/// </summary>
/// <remarks>
/// <para>
/// OpenAPI security requirements follow AND/OR logic:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <strong>AND within a single requirement object</strong>: All security schemes
/// listed in a single <see cref="OpenApiSecurityRequirement"/> must be satisfied.
/// </description>
/// </item>
/// <item>
/// <description>
/// <strong>OR across multiple requirement objects</strong>: Only one of the
/// <see cref="OpenApiSecurityRequirement"/> objects in the array needs to be satisfied.
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>Example - OR semantics (multiple requirements):</strong>
/// </para>
/// <code>
/// // Either API Key OR OAuth2 can be used
/// var requirements = new List&lt;OpenApiSecurityRequirement&gt;
/// {
///     new SecurityRequirementBuilder()
///         .AddScheme("apiKey")
///         .Build(),
///     new SecurityRequirementBuilder()
///         .AddScheme("oauth2", "read", "write")
///         .Build()
/// };
/// </code>
/// <para>
/// <strong>Example - AND semantics (single requirement with multiple schemes):</strong>
/// </para>
/// <code>
/// // Both API Key AND OAuth2 must be satisfied
/// var requirement = new SecurityRequirementBuilder()
///     .AddScheme("apiKey")
///     .AddScheme("oauth2", "admin")
///     .Build();
/// </code>
/// </remarks>
public class SecurityRequirementBuilder
{
    private readonly Dictionary<OpenApiSecurityScheme, IList<string>> _schemes = new();

    /// <summary>
    /// Adds a security scheme to this requirement.
    /// When multiple schemes are added to the same builder, they are combined with AND logic
    /// (all must be satisfied).
    /// </summary>
    /// <param name="schemeName">The name of the security scheme as registered in components/securitySchemes.</param>
    /// <param name="scopes">Optional scopes required for this scheme (used for OAuth2 and OpenID Connect).</param>
    /// <returns>This builder for chaining.</returns>
    /// <remarks>
    /// For non-OAuth schemes (API Key, HTTP Bearer, Mutual TLS), the scopes array should be empty.
    /// For OAuth2 and OpenID Connect schemes, specify the required scopes.
    /// </remarks>
    public SecurityRequirementBuilder AddScheme(string schemeName, params string[] scopes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemeName);

        // Create a reference to the security scheme
        var schemeReference = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = schemeName
            }
        };

        // Add scopes (empty list for non-OAuth schemes)
        _schemes[schemeReference] = new List<string>(scopes ?? Array.Empty<string>());

        return this;
    }

    /// <summary>
    /// Adds a security scheme with a reference to an existing scheme definition.
    /// </summary>
    /// <param name="scheme">The security scheme with a reference to components/securitySchemes.</param>
    /// <param name="scopes">Optional scopes required for this scheme.</param>
    /// <returns>This builder for chaining.</returns>
    public SecurityRequirementBuilder AddScheme(OpenApiSecurityScheme scheme, params string[] scopes)
    {
        ArgumentNullException.ThrowIfNull(scheme);

        if (scheme.Reference == null)
        {
            throw new ArgumentException(
                "Security scheme must have a reference to a scheme defined in components/securitySchemes.",
                nameof(scheme));
        }

        _schemes[scheme] = new List<string>(scopes ?? Array.Empty<string>());

        return this;
    }

    /// <summary>
    /// Builds the security requirement.
    /// </summary>
    /// <returns>The configured security requirement.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no schemes have been added.</exception>
    /// <remarks>
    /// The resulting <see cref="OpenApiSecurityRequirement"/> contains all schemes added to this builder.
    /// All schemes must be satisfied (AND logic) for this requirement to be met.
    /// </remarks>
    public OpenApiSecurityRequirement Build()
    {
        if (_schemes.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one security scheme must be added to the requirement.");
        }

        var requirement = new OpenApiSecurityRequirement();
        foreach (var kvp in _schemes)
        {
            requirement[kvp.Key] = kvp.Value;
        }

        return requirement;
    }

    /// <summary>
    /// Creates a builder for a new security requirement in an OR relationship.
    /// </summary>
    /// <returns>A new builder instance.</returns>
    /// <remarks>
    /// Use this method when you want to create alternative authentication options.
    /// Each builder creates one requirement object, and multiple requirement objects
    /// have an OR relationship.
    /// </remarks>
    public static SecurityRequirementBuilder CreateAlternative()
    {
        return new SecurityRequirementBuilder();
    }
}
