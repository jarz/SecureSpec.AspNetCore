using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SecureSpec.AspNetCore.IntegrationTests.Infrastructure;

/// <summary>
/// Provides a consistent way to spin up in-memory ASP.NET Core hosts for integration tests.
/// </summary>
internal static class IntegrationTestHostFactory
{
    /// <summary>
    /// Starts a new in-memory host with optional service and endpoint customization.
    /// </summary>
    /// <param name="configureServices">Custom service registrations.</param>
    /// <param name="configureEndpoints">Endpoint mappings executed within <c>UseEndpoints</c>.</param>
    /// <returns>The started <see cref="IHost"/> instance.</returns>
    public static Task<IHost> StartHostAsync(
        Action<IServiceCollection>? configureServices = null,
        Action<IEndpointRouteBuilder>? configureEndpoints = null)
    {
        return new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddRouting();
                    configureServices?.Invoke(services);
                });

                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        configureEndpoints?.Invoke(endpoints);
                    });
                });
            })
            .StartAsync();
    }
}
