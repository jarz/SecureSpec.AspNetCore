using Microsoft.Extensions.DependencyInjection;
using SecureSpec.AspNetCore.Configuration;

namespace SecureSpec.AspNetCore;

/// <summary>
/// Extension methods for setting up SecureSpec services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class SecureSpecServiceCollectionExtensions
{
    /// <summary>
    /// Adds SecureSpec services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configure">An <see cref="Action{SecureSpecOptions}"/> to configure the provided <see cref="SecureSpecOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddSecureSpec(
        this IServiceCollection services,
        Action<SecureSpecOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Configure options
        services.Configure(configure);

        // Register core services
        // TODO: Register services as they are implemented

        return services;
    }
}
