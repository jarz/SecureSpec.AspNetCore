namespace SecureSpec.AspNetCore.Configuration;

/// <summary>
/// Configuration options for security schemes.
/// </summary>
public class SecurityOptions
{
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
    public string? AuthorizationUrl { get; set; }

    /// <summary>
    /// Gets or sets the token URL.
    /// </summary>
    public string? TokenUrl { get; set; }

    /// <summary>
    /// Gets or sets the refresh URL.
    /// </summary>
    public string? RefreshUrl { get; set; }

    /// <summary>
    /// Gets the collection of scopes.
    /// </summary>
    public Dictionary<string, string> Scopes { get; } = new();
}
