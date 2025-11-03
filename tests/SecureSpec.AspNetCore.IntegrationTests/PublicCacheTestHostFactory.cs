using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SecureSpec.AspNetCore.Configuration;
using HostProgram = SecureSpec.AspNetCore.TestHost.Program;

namespace SecureSpec.AspNetCore.IntegrationTests;

/// <summary>
/// Customizes the SecureSpec test host for scenarios that require public caching.
/// </summary>
public sealed class PublicCacheTestHostFactory : WebApplicationFactory<HostProgram>
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("IntegrationTesting");
        builder.ConfigureLogging(logging => logging.ClearProviders());

        builder.ConfigureServices(services =>
        {
            services.PostConfigure<SecureSpecOptions>(options =>
            {
                options.UI.Assets.AllowPublicCache = true;
                options.UI.Assets.EnableIntegrityRevalidation = false;
            });
        });
    }
}
