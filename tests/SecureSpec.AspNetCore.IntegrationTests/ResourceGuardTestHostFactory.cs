using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SecureSpec.AspNetCore.Configuration;
using HostProgram = SecureSpec.AspNetCore.TestHost.Program;

namespace SecureSpec.AspNetCore.IntegrationTests;

/// <summary>
/// Custom SecureSpec host factory that lowers resource guard thresholds for targeted guard testing.
/// </summary>
public sealed class ResourceGuardTestHostFactory : WebApplicationFactory<HostProgram>
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
                options.Performance.MaxGenerationTimeMs = 150;
                options.Performance.MaxMemoryBytes = 256 * 1024;
            });
        });
    }
}
