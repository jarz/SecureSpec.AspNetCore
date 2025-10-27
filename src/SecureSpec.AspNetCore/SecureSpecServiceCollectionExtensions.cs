using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Core;
using SecureSpec.AspNetCore.Diagnostics;

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

        // Register diagnostics logger as singleton
        services.AddSingleton<DiagnosticsLogger>();

        // Register document cache as singleton
        services.AddSingleton<DocumentCache>(sp =>
        {
            var logger = sp.GetRequiredService<DiagnosticsLogger>();
            var options = sp.GetRequiredService<IOptions<SecureSpecOptions>>().Value;
            return new DocumentCache(logger, options.Cache.DefaultExpiration);
        });

        // Register core services
        // TODO: Register additional services as they are implemented

        return services;
    }
}
