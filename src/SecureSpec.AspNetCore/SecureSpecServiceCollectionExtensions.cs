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
    /// <remarks>
    /// This method is idempotent and can be called multiple times on the same service collection.
    /// Subsequent calls will skip registration if services are already registered, but will still apply configuration.
    /// </remarks>
    public static IServiceCollection AddSecureSpec(
        this IServiceCollection services,
        Action<SecureSpecOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Check if already registered - no lock needed as IServiceCollection operations are safe
        // and each service collection instance is independent
        if (services.Any(d => d.ServiceType == typeof(ApiDiscoveryEngine)))
        {
            // Still apply configuration even if services are already registered
            services.Configure(configure);
            return services;
        }

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

        // Register all filter types from the configuration BEFORE FilterPipeline
        // Build temporary options to get filter types
        var tempOptions = new SecureSpecOptions();
        configure(tempOptions);
        RegisterConfiguredFilters(services, tempOptions.Filters);

        // Register filter pipeline as singleton (after filters are registered)
        services.AddSingleton<FilterPipeline>(sp =>
        {
            var logger = sp.GetRequiredService<DiagnosticsLogger>();
            var options = sp.GetRequiredService<IOptions<SecureSpecOptions>>().Value;
            return new FilterPipeline(sp, options.Filters, logger);
        });

        return services;
    }

    private static void RegisterConfiguredFilters(IServiceCollection services, FilterCollection filters)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(filters);

        // Get already registered implementation types
        var registeredTypes = new HashSet<Type>(
            services
                .Where(d => d.ImplementationType != null)
                .Select(d => d.ImplementationType!));
        foreach (var filterType in GetConfiguredFilterTypes(filters).Where(filterType => !registeredTypes.Contains(filterType)))
        {
            services.AddSingleton(filterType);
            registeredTypes.Add(filterType);
        }
    }

    private static HashSet<Type> GetConfiguredFilterTypes(FilterCollection filters)
    {
        // Use HashSet to automatically deduplicate filter types
        var filterTypes = new HashSet<Type>();

        AddValidatedFilterTypes<ISchemaFilter>(filterTypes, filters.SchemaFilters, nameof(filters.SchemaFilters));
        AddValidatedFilterTypes<IOperationFilter>(filterTypes, filters.OperationFilters, nameof(filters.OperationFilters));
        AddValidatedFilterTypes<IParameterFilter>(filterTypes, filters.ParameterFilters, nameof(filters.ParameterFilters));
        AddValidatedFilterTypes<IRequestBodyFilter>(filterTypes, filters.RequestBodyFilters, nameof(filters.RequestBodyFilters));
        AddValidatedFilterTypes<IDocumentFilter>(filterTypes, filters.DocumentFilters, nameof(filters.DocumentFilters));
        AddValidatedFilterTypes<IPreSerializeFilter>(filterTypes, filters.PreSerializeFilters, nameof(filters.PreSerializeFilters));

        return filterTypes;
    }

    private static void AddValidatedFilterTypes<TFilter>(HashSet<Type> filterTypes, IReadOnlyList<Type>? types, string collectionName)
    {
        if (types == null)
        {
            return;
        }

        foreach (var type in types)
        {
            ValidateFilterType<TFilter>(type, collectionName);
            filterTypes.Add(type);
        }
    }

    private static void ValidateFilterType<TFilter>(Type filterType, string collectionName)
    {
        if (filterType.IsAbstract || filterType.IsInterface)
        {
            throw new ArgumentException(
                $"Filter type {filterType.Name} in {collectionName} cannot be abstract or an interface.",
                nameof(filterType));
        }

        if (!typeof(TFilter).IsAssignableFrom(filterType))
        {
            throw new ArgumentException(
                $"Type {filterType.Name} in {collectionName} does not implement {typeof(TFilter).Name}.",
                nameof(filterType));
        }
    }
}
