using Microsoft.Extensions.DependencyInjection;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.IntegrationTests.Controllers;
using SecureSpec.AspNetCore.IntegrationTests.Infrastructure;
using Xunit;

namespace SecureSpec.AspNetCore.IntegrationTests.Security;

/// <summary>
/// Integration coverage for <see cref="SecurityOptions"/> scenarios.
/// </summary>
public class SecurityOptionsIntegrationTests
{
    /// <summary>
    /// Ensures global API key security scheme is applied and respected by pipeline.
    /// </summary>
    [Fact]
    public async Task ApiKeyScheme_IsRegisteredAndApplied()
    {
        using var host = await IntegrationTestHostFactory.StartHostAsync(
            services =>
            {
                services.AddControllers().AddApplicationPart(typeof(SampleController).Assembly);
                services.AddSecureSpec(options =>
                {
                    options.Security.AddApiKeyHeader("apiKey", builder =>
                        builder.WithName("X-API-KEY").WithDescription("Test API key"));
                    options.Filters.AddOperationFilter<SecurityOperationFilter>();
                });
            },
            endpoints => Microsoft.AspNetCore.Builder.ControllerEndpointRouteBuilderExtensions.MapControllers(endpoints));

        var options = host.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<SecureSpecOptions>>().Value;
        Assert.True(options.Security.Schemes.ContainsKey("apiKey"));

        var scheme = options.Security.Schemes["apiKey"];
        Assert.Equal(Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey, scheme.Type);
        Assert.Equal(Microsoft.OpenApi.Models.ParameterLocation.Header, scheme.In);
        Assert.Equal("X-API-KEY", scheme.Name);
        Assert.Equal("Test API key", scheme.Description);

        await host.StopAsync();
    }

    /// <summary>
    /// Ensures HTTP bearer scheme can be added and retrieved from security options.
    /// </summary>
    [Fact]
    public void HttpBearerScheme_IsAddedToOptions()
    {
        var options = new SecurityOptions();
        options.AddHttpBearer("bearer", builder =>
            builder.WithDescription("Bearer tokens").WithBearerFormat("JWT"));

        Assert.True(options.Schemes.ContainsKey("bearer"));
        var scheme = options.Schemes["bearer"];
        Assert.Equal("Bearer tokens", scheme.Description);
        Assert.Equal("JWT", scheme.BearerFormat);
    }

    /// <summary>
    /// Validates policy to scope mappings integrate correctly via configuration.
    /// </summary>
    [Fact]
    public void PolicyScopeMappings_CanBeConfigured()
    {
        var options = new SecurityOptions();
        options.PolicyToScope = policy => policy + "-scope";
        options.RoleToScope = role => role.ToUpperInvariant();

        Assert.Equal("policy-scope", options.PolicyToScope!("policy"));
        Assert.Equal("ADMIN", options.RoleToScope!("admin"));
    }
}
