using Microsoft.Extensions.DependencyInjection;
using SecureSpec.AspNetCore.Core;
using SecureSpec.AspNetCore.Filters;
using SecureSpec.AspNetCore.IntegrationTests.Controllers;
using SecureSpec.AspNetCore.IntegrationTests.Infrastructure;
using Xunit;

namespace SecureSpec.AspNetCore.IntegrationTests.Discovery;

/// <summary>
/// Tests verifying the <see cref="SecureSpecServiceCollectionExtensions"/> behavior via integration scenarios.
/// </summary>
public class SecureSpecServiceCollectionIntegrationTests
{
    /// <summary>
    /// Ensures calling <c>AddSecureSpec</c> multiple times remains idempotent and filter registrations accumulate.
    /// </summary>
    [Fact]
    public async Task AddSecureSpec_IdempotentAcrossCalls()
    {
        var services = new ServiceCollection();
        services.AddRouting();
        services.AddControllers().AddApplicationPart(typeof(SampleController).Assembly);

        services.AddSecureSpec(options =>
        {
            options.Filters.AddDocumentFilter<SuccessDocumentFilter>();
            options.Documents.Add("v1", doc =>
            {
                doc.Info.Title = "API";
            });
        });

        services.AddSecureSpec(options =>
        {
            options.Filters.AddDocumentFilter<DocumentFlagFilter>();
            options.Documents.Add("v2", doc =>
            {
                doc.Info.Title = "API v2";
            });
        });

        var discoveryEngineRegistrations = services.Count(d => d.ServiceType == typeof(ApiDiscoveryEngine));
        Assert.Equal(1, discoveryEngineRegistrations);

        await using var provider = services.BuildServiceProvider();

        var filterPipeline = provider.GetRequiredService<Filters.FilterPipeline>();
        var document = new Microsoft.OpenApi.Models.OpenApiDocument
        {
            Info = new Microsoft.OpenApi.Models.OpenApiInfo { Title = "API" }
        };

        filterPipeline.ApplyDocumentFilters(document, new DocumentFilterContext { DocumentName = "v1" });
        Assert.True(document.Extensions.ContainsKey("x-success"));
        Assert.True(document.Extensions.ContainsKey("x-document-flag"));
    }

    /// <summary>
    /// Validates security filter extensions can add security requirements without collisions.
    /// </summary>
    [Fact]
    public async Task AddSecureSpec_AllowsSecurityFilterRegistration()
    {
        using var host = await IntegrationTestHostFactory.StartHostAsync(
            services =>
            {
                services.AddControllers().AddApplicationPart(typeof(SampleController).Assembly);
                services.AddSecureSpec(options =>
                {
                    options.Filters.AddOperationFilter<SecurityOperationFilter>();
                });
            },
            endpoints => Microsoft.AspNetCore.Builder.ControllerEndpointRouteBuilderExtensions.MapControllers(endpoints));

        var filterPipeline = host.Services.GetRequiredService<Filters.FilterPipeline>();

        var operation = new Microsoft.OpenApi.Models.OpenApiOperation();
        var method = typeof(SampleController).GetMethod(nameof(SampleController.GetSample))!;
        var context = new OperationFilterContext
        {
            MethodInfo = method,
            HttpMethod = "GET",
            RoutePattern = "/api/sample/with-input/{id}",
            ControllerType = typeof(SampleController)
        };

        filterPipeline.ApplyOperationFilters(operation, context);

        Assert.Single(operation.Security);
        var requirement = operation.Security[0];
        Assert.Single(requirement.Keys);
        Assert.Contains("write", requirement.Single().Value);

        await host.StopAsync();
    }
}
