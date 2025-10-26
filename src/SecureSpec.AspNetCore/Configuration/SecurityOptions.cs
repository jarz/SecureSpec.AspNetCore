using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.Security;

namespace SecureSpec.AspNetCore.Configuration;

/// <summary>
/// Configuration options for security schemes.
/// </summary>
public class SecurityOptions
{
    private readonly Dictionary<string, OpenApiSecurityScheme> _schemes = new(StringComparer.Ordinal);
    private readonly DiagnosticsLogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityOptions"/> class.
    /// </summary>
    public SecurityOptions()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityOptions"/> class with a diagnostics logger.
    /// </summary>
    /// <param name="logger">The diagnostics logger.</param>
    public SecurityOptions(DiagnosticsLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets OAuth configuration options.
    /// </summary>
    public OAuthOptions OAuth { get; } = new();

    /// <summary>
    /// Gets or sets the function to map a policy name to an OAuth scope.
    /// </summary>
    public Func<string, string>? PolicyToScope { get; set; }

    /// <summary>
    /// Gets or sets the function to map a role name to an OAuth scope.
    /// </summary>
    public Func<string, string>? RoleToScope { get; set; }

    /// <summary>
    /// Adds an HTTP Bearer authentication scheme.
    /// </summary>
    /// <param name="name">The name of the security scheme (e.g., "bearerAuth").</param>
    /// <param name="configure">An optional action to configure the scheme.</param>
    public void AddHttpBearer(string name, Action<HttpBearerSchemeBuilder>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var builder = new HttpBearerSchemeBuilder();
        configure?.Invoke(builder);

        var scheme = builder.Build();
        _schemes[name] = scheme;
    }

    /// <summary>
    /// Adds an API Key authentication scheme using a header parameter.
    /// </summary>
    /// <param name="name">The name of the security scheme (e.g., "apiKeyHeader").</param>
    /// <param name="configure">An optional action to configure the scheme.</param>
    public void AddApiKeyHeader(string name, Action<ApiKeyHeaderSchemeBuilder>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var builder = new ApiKeyHeaderSchemeBuilder();
        configure?.Invoke(builder);

        var scheme = builder.Build();
        _schemes[name] = scheme;
    }

    /// <summary>
    /// Adds an API Key authentication scheme using a query parameter.
    /// </summary>
    /// <param name="name">The name of the security scheme (e.g., "apiKeyQuery").</param>
    /// <param name="configure">An optional action to configure the scheme.</param>
    public void AddApiKeyQuery(string name, Action<ApiKeyQuerySchemeBuilder>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var builder = new ApiKeyQuerySchemeBuilder();
        configure?.Invoke(builder);

        var scheme = builder.Build();
        _schemes[name] = scheme;
    }

    /// <summary>
    /// Adds an OAuth2 Client Credentials flow authentication scheme.
    /// </summary>
    /// <param name="name">The name of the security scheme (e.g., "oauth2").</param>
    /// <param name="configure">An action to configure the scheme.</param>
    public void AddOAuth2ClientCredentials(string name, Action<OAuth2ClientCredentialsSchemeBuilder> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new OAuth2ClientCredentialsSchemeBuilder();
        configure(builder);

        var scheme = builder.Build();
        _schemes[name] = scheme;
    }

    /// <summary>
    /// Adds a Mutual TLS authentication scheme (display only).
    /// </summary>
    /// <param name="name">The name of the security scheme (e.g., "mutualTLS").</param>
    /// <param name="configure">An optional action to configure the scheme.</param>
    /// <remarks>
    /// Mutual TLS (mTLS) requires client certificates to be configured at the TLS/SSL layer.
    /// This method registers the scheme for OpenAPI documentation purposes only.
    /// Certificate upload functionality is not supported; certificates must be managed
    /// externally through infrastructure configuration (e.g., API Gateway, Load Balancer, or web server).
    /// </remarks>
    public void AddMutualTls(string name, Action<MutualTlsSchemeBuilder>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var builder = new MutualTlsSchemeBuilder();
        configure?.Invoke(builder);

        var scheme = builder.Build();
        _schemes[name] = scheme;
    }

    /// <summary>
    /// Gets all registered security schemes.
    /// </summary>
    public IReadOnlyDictionary<string, OpenApiSecurityScheme> Schemes => _schemes;

    /// <summary>
    /// Attempts to infer Basic authentication from authorization attributes.
    /// This method is intentionally designed to block Basic auth inference and emit the AUTH001 diagnostic.
    /// </summary>
    /// <remarks>
    /// Basic auth inference is explicitly blocked as a security measure (AUTH001).
    /// Users must explicitly define security schemes instead of relying on inference.
    /// </remarks>
    public void BlockBasicAuthInference()
    {
        // Emit AUTH001 diagnostic when Basic auth inference is attempted
        _logger?.LogWarning(
            "AUTH001",
            "Basic auth inference blocked. Define security schemes explicitly using AddHttpBearer or other security scheme methods.",
            new { Reason = "Explicit scheme definition required for security" });
    }
}

/// <summary>
/// Configuration options for OAuth flows.
/// </summary>
public class OAuthOptions
{
    /// <summary>
    /// Configures the Authorization Code flow with PKCE.
    /// PKCE is always required and cannot be disabled.
    /// </summary>
    public void AuthorizationCode(Action<OAuthFlowConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var config = new OAuthFlowConfiguration();
        configure(config);
        AuthorizationCodeFlow = config;
    }

    /// <summary>
    /// Configures the Client Credentials flow.
    /// </summary>
    public void ClientCredentials(Action<OAuthFlowConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var config = new OAuthFlowConfiguration();
        configure(config);
        ClientCredentialsFlow = config;
    }

    /// <summary>
    /// Gets the Authorization Code flow configuration, if configured.
    /// </summary>
    public OAuthFlowConfiguration? AuthorizationCodeFlow { get; private set; }

    /// <summary>
    /// Gets the Client Credentials flow configuration, if configured.
    /// </summary>
    public OAuthFlowConfiguration? ClientCredentialsFlow { get; private set; }
}

/// <summary>
/// Configuration for an OAuth flow.
/// </summary>
public class OAuthFlowConfiguration
{
    /// <summary>
    /// Gets or sets the authorization URL.
    /// </summary>
    public Uri? AuthorizationUrl { get; set; }

    /// <summary>
    /// Gets or sets the token URL.
    /// </summary>
    public Uri? TokenUrl { get; set; }

    /// <summary>
    /// Gets or sets the refresh URL.
    /// </summary>
    public Uri? RefreshUrl { get; set; }

    /// <summary>
    /// Gets the collection of scopes.
    /// </summary>
    public Dictionary<string, string> Scopes { get; } = new();
}
