using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Core;
using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.Filters;
using SecureSpec.AspNetCore.Schema;

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

        // Register schema generator as singleton
        services.AddSingleton<SchemaGenerator>();

        // Register discovery strategies as singletons
        services.AddSingleton<IEndpointDiscoveryStrategy, ControllerDiscoveryStrategy>();
        services.AddSingleton<IEndpointDiscoveryStrategy, MinimalApiDiscoveryStrategy>();

        // Register metadata extractor as singleton
        services.AddSingleton<MetadataExtractor>();

        // Register API discovery engine as singleton
        services.AddSingleton<ApiDiscoveryEngine>();

        // Register filter pipeline as singleton
        services.AddSingleton<FilterPipeline>(sp =>
        {
            var logger = sp.GetRequiredService<DiagnosticsLogger>();
            var options = sp.GetRequiredService<IOptions<SecureSpecOptions>>().Value;
            return new FilterPipeline(sp, options.Filters, logger);
        });

        // Register all filter types from the configuration
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<SecureSpecOptions>>().Value;

            // Register all schema filters
            foreach (var filterType in options.Filters.SchemaFilters)
            {
                if (!services.Any(d => d.ServiceType == filterType))
                {
                    services.AddSingleton(filterType);
                }
            }

            // Register all operation filters
            foreach (var filterType in options.Filters.OperationFilters)
            {
                if (!services.Any(d => d.ServiceType == filterType))
                {
                    services.AddSingleton(filterType);
                }
            }

            // Register all parameter filters
            foreach (var filterType in options.Filters.ParameterFilters)
            {
                if (!services.Any(d => d.ServiceType == filterType))
                {
                    services.AddSingleton(filterType);
                }
            }

            // Register all request body filters
            foreach (var filterType in options.Filters.RequestBodyFilters)
            {
                if (!services.Any(d => d.ServiceType == filterType))
                {
                    services.AddSingleton(filterType);
                }
            }

            // Register all document filters
            foreach (var filterType in options.Filters.DocumentFilters)
            {
                if (!services.Any(d => d.ServiceType == filterType))
                {
                    services.AddSingleton(filterType);
                }
            }

            // Register all pre-serialize filters
            foreach (var filterType in options.Filters.PreSerializeFilters)
            {
                if (!services.Any(d => d.ServiceType == filterType))
                {
                    services.AddSingleton(filterType);
                }
            }

            return new object(); // Dummy return for build action
        });

        return services;
    }
}
