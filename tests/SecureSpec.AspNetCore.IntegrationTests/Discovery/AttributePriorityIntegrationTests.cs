using Microsoft.Extensions.DependencyInjection;
using SecureSpec.AspNetCore.Core;
using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.IntegrationTests.Controllers;
using SecureSpec.AspNetCore.IntegrationTests.Infrastructure;
using Xunit;

namespace SecureSpec.AspNetCore.IntegrationTests.Discovery;

/// <summary>
/// Integration tests focused on attribute precedence when combining include/exclude markers.
/// </summary>
public class AttributePriorityIntegrationTests
{
    /// <summary>
    /// Ensures an endpoint marked for inclusion is discovered even when its controller is excluded.
    /// </summary>
    [Fact]
    public async Task IncludeAttribute_OverridesControllerExclusion()
    {
        using var host = await IntegrationTestHostFactory.StartHostAsync(
            services =>
            {
                services
                    .AddControllers()
                    .AddApplicationPart(typeof(DiagnosticsController).Assembly);

                services.AddSecureSpec(options =>
                {
                    options.Discovery.IncludeOnlyApiControllers = true;
                });
            },
            endpoints => Microsoft.AspNetCore.Builder.ControllerEndpointRouteBuilderExtensions.MapControllers(endpoints));

        var logger = host.Services.GetRequiredService<DiagnosticsLogger>();
        logger.Clear();

        var discoveryEngine = host.Services.GetRequiredService<ApiDiscoveryEngine>();
        var endpoints = (await discoveryEngine.DiscoverEndpointsAsync()).ToList();

        Assert.DoesNotContain(endpoints, endpoint =>
            endpoint.RoutePattern.Contains("diagnostics/", System.StringComparison.OrdinalIgnoreCase));

        var events = logger.GetEvents();
        Assert.Contains(events, e =>
            e.Code == DiagnosticCodes.EndpointFiltered &&
            e.Message.Contains("Controller excluded by default", System.StringComparison.OrdinalIgnoreCase));

        await host.StopAsync();
    }
}
