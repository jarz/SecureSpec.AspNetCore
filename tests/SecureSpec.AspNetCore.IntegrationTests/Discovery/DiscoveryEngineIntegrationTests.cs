using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SecureSpec.AspNetCore.Core;
using SecureSpec.AspNetCore.Core.Attributes;
using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.IntegrationTests.Controllers;
using SecureSpec.AspNetCore.IntegrationTests.Infrastructure;
using Xunit;

namespace SecureSpec.AspNetCore.IntegrationTests.Discovery;

/// <summary>
/// Integration tests covering endpoint discovery options, diagnostics, and concurrency behavior.
/// </summary>
public class DiscoveryEngineIntegrationTests
{
    /// <summary>
    /// Ensures controller and minimal API endpoints are discovered and key diagnostics are emitted.
    /// </summary>
    [Fact]
    public async Task DiscoverEndpoints_EndToEnd_CapturesDiagnosticsAndFilters()
    {
        using var host = await IntegrationTestHostFactory.StartHostAsync(
            services =>
            {
                services
                    .AddControllers()
                    .AddApplicationPart(typeof(SampleController).Assembly);

                services.AddSecureSpec(options =>
                {
                    options.Documents.Add("v1", doc =>
                    {
                        doc.Info.Title = "Integration API";
                        doc.Info.Version = "1.0.0";
                    });
                });
            },
            endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGet("/minimal/widgets", () => Results.Ok(new MinimalWidget("ok")))
                    .WithMetadata(new IncludeInSpecAttribute());
                endpoints.MapGet("/minimal/widgets", () => Results.Ok())
                    .WithMetadata(new IncludeInSpecAttribute());
            });

        var diagnostics = host.Services.GetRequiredService<DiagnosticsLogger>();
        diagnostics.Clear();

        var discoveryEngine = host.Services.GetRequiredService<ApiDiscoveryEngine>();
        var endpoints = (await discoveryEngine.DiscoverEndpointsAsync()).ToList();

        var controllerEndpoint = endpoints.FirstOrDefault(endpoint => endpoint.ControllerType == typeof(SampleController));
        Assert.NotNull(controllerEndpoint);
        Assert.False(controllerEndpoint!.IsMinimalApi);
        Assert.Contains(controllerEndpoint.HttpMethods, method => string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("with-input", controllerEndpoint.RoutePattern, StringComparison.OrdinalIgnoreCase);

        Assert.Contains(endpoints, endpoint =>
            endpoint.IsMinimalApi &&
            string.Equals(endpoint.RoutePattern, "/minimal/widgets", StringComparison.OrdinalIgnoreCase));

        var discoveryEvents = diagnostics.GetEvents();
        Assert.Contains(discoveryEvents, e => e.Code == DiagnosticCodes.EndpointsDiscovered && e.Message.Contains("Discovered", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(discoveryEvents, e => e.Code == DiagnosticCodes.EndpointFiltered && e.Message.Contains("Duplicate", StringComparison.OrdinalIgnoreCase));

        await host.StopAsync();
    }

    /// <summary>
    /// Validates inclusion/exclusion attributes and discovery option interactions.
    /// </summary>
    [Fact]
    public async Task DiscoverEndpoints_AppliesOptionsAndAttributes()
    {
        using var host = await IntegrationTestHostFactory.StartHostAsync(
            services =>
            {
                services
                    .AddControllers()
                    .AddApplicationPart(typeof(SampleController).Assembly)
                    .AddApplicationPart(typeof(MiscController).Assembly);

                services.AddSecureSpec(options =>
                {
                    options.Discovery.IncludeOnlyApiControllers = true;
                    options.Discovery.IncludeMinimalApis = false;
                    options.Discovery.ExcludePredicate = metadata =>
                        metadata.RoutePattern.Contains("filtered", StringComparison.OrdinalIgnoreCase);
                    options.Discovery.IncludePredicate = metadata =>
                        string.Equals(metadata.RoutePattern, "/minimal/included", StringComparison.OrdinalIgnoreCase);
                });
            },
            endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGet("/minimal/included", () => Results.Ok())
                    .WithMetadata(new IncludeInSpecAttribute());
                endpoints.MapGet("/minimal/filtered", () => Results.Ok());
            });

        var discoveryEngine = host.Services.GetRequiredService<ApiDiscoveryEngine>();
        var endpoints = (await discoveryEngine.DiscoverEndpointsAsync()).ToList();

        Assert.Contains(endpoints, endpoint =>
            endpoint.ControllerType == typeof(MiscController) &&
            endpoint.RoutePattern.Contains("misc/included", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(endpoints, endpoint =>
            endpoint.ControllerType == typeof(MiscController) &&
            endpoint.RoutePattern.Contains("misc/excluded", StringComparison.OrdinalIgnoreCase));

        Assert.Contains(endpoints, endpoint =>
            endpoint.IsMinimalApi &&
            string.Equals(endpoint.RoutePattern, "/minimal/included", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(endpoints, endpoint =>
            string.Equals(endpoint.RoutePattern, "/minimal/filtered", StringComparison.OrdinalIgnoreCase));

        await host.StopAsync();
    }

    /// <summary>
    /// Confirms diagnostics are published when endpoints are filtered out.
    /// </summary>
    [Fact]
    public async Task DiscoverEndpoints_EmitsDiagnosticsForFilteredEndpoints()
    {
        using var host = await IntegrationTestHostFactory.StartHostAsync(
            services =>
            {
                services
                    .AddControllers()
                    .AddApplicationPart(typeof(MiscController).Assembly);

                services.AddSecureSpec(options =>
                {
                    options.Discovery.IncludeOnlyApiControllers = false;
                    options.Discovery.IncludeMinimalApis = false;
                    options.Discovery.ExcludePredicate = metadata =>
                        metadata.RoutePattern.Contains("filtered", StringComparison.OrdinalIgnoreCase);
                });
            },
            endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGet("/minimal/diagnostics", () => Results.Ok());
            });

        var logger = host.Services.GetRequiredService<DiagnosticsLogger>();
        logger.Clear();

        var discoveryEngine = host.Services.GetRequiredService<ApiDiscoveryEngine>();
        _ = await discoveryEngine.DiscoverEndpointsAsync();

        var events = logger.GetEvents();

        Assert.Contains(events, e => e.Code == DiagnosticCodes.EndpointsDiscovered);
        Assert.Contains(events, e =>
            e.Code == DiagnosticCodes.EndpointFiltered &&
            e.Message.Contains("/minimal/diagnostics", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(events, e =>
            e.Code == DiagnosticCodes.EndpointFiltered &&
            e.Message.Contains("Integration test exclusion", StringComparison.OrdinalIgnoreCase));

        await host.StopAsync();
    }

    /// <summary>
    /// Verifies multiple concurrent discovery executions produce stable results.
    /// </summary>
    [Fact]
    public async Task DiscoverEndpoints_RunsConcurrentlyWithoutDivergence()
    {
        using var host = await IntegrationTestHostFactory.StartHostAsync(
            services =>
            {
                services
                    .AddControllers()
                    .AddApplicationPart(typeof(SampleController).Assembly)
                    .AddApplicationPart(typeof(MiscController).Assembly);

                services.AddSecureSpec(options =>
                {
                    options.Discovery.IncludeMinimalApis = true;
                    options.Discovery.IncludeOnlyApiControllers = false;
                });
            },
            endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGet("/minimal/concurrency", () => Results.Ok(new MinimalWidget("parallel")))
                    .WithMetadata(new IncludeInSpecAttribute());
            });

        var engine = host.Services.GetRequiredService<ApiDiscoveryEngine>();

        var discoveryTasks = Enumerable
            .Range(0, 8)
            .Select(_ => engine.DiscoverEndpointsAsync())
            .ToArray();

        var results = await Task.WhenAll(discoveryTasks);

        Assert.All(results, endpoints =>
        {
            Assert.NotNull(endpoints);
            Assert.NotEmpty(endpoints);
        });

        var baseline = results[0]
            .OrderBy(e => e.HttpMethod + ":" + e.RoutePattern, StringComparer.OrdinalIgnoreCase)
            .Select(e => (Method: e.HttpMethod, Route: e.RoutePattern))
            .ToList();

        foreach (var endpoints in results.Skip(1))
        {
            var comparison = endpoints
                .OrderBy(e => e.HttpMethod + ":" + e.RoutePattern, StringComparer.OrdinalIgnoreCase)
                .Select(e => (Method: e.HttpMethod, Route: e.RoutePattern))
                .ToList();

            Assert.Equal(baseline, comparison);
        }

        await host.StopAsync();
    }
}
