using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using HostProgram = SecureSpec.AspNetCore.TestHost.Program;

namespace SecureSpec.AspNetCore.IntegrationTests;

/// <summary>
/// Provides an ASP.NET Core factory for integration tests.
/// </summary>
public sealed class SecureSpecTestHostFactory : WebApplicationFactory<HostProgram>
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTesting");
        builder.ConfigureLogging(logging => logging.ClearProviders());
    }
}
