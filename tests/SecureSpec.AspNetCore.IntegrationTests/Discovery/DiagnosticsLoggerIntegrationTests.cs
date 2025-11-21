using Microsoft.Extensions.DependencyInjection;
using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.Filters;
using SecureSpec.AspNetCore.IntegrationTests.Infrastructure;
using Xunit;
using FilterPipelineCore = SecureSpec.AspNetCore.Filters.FilterPipeline;

namespace SecureSpec.AspNetCore.IntegrationTests.Discovery;

/// <summary>
/// Tests against <see cref="DiagnosticsLogger"/> usage patterns within SecureSpec integration scenarios.
/// </summary>
public class DiagnosticsLoggerIntegrationTests
{
    /// <summary>
    /// Ensures sanitized output from <see cref="DiagnosticsLogger.SanitizePath(string)"/> matches expectations.
    /// </summary>
    [Fact]
    public void SanitizePath_TrimsDirectorySegments()
    {
        var sanitized = DiagnosticsLogger.SanitizePath("/var/tmp/secure/trace.log");
        Assert.Equal("trace.log", sanitized);

        sanitized = DiagnosticsLogger.SanitizePath("C:\\temp\\trace.log");
        Assert.Equal("trace.log", sanitized);
    }

    /// <summary>
    /// Confirms logging performed by filter pipeline contains expected metadata when failures occur.
    /// </summary>
    [Fact]
    public async Task Logging_PipelineFailuresContainMetadata()
    {
        using var host = await IntegrationTestHostFactory.StartHostAsync(
            services =>
            {
                services.AddSecureSpec(options =>
                {
                    options.Documents.Add("v1", doc =>
                    {
                        doc.Info.Title = "Diagnostics";
                    });
                    options.Filters.AddDocumentFilter<FailingDocumentFilter>();
                });
            });

        var logger = host.Services.GetRequiredService<DiagnosticsLogger>();
        logger.Clear();

        var pipeline = host.Services.GetRequiredService<FilterPipelineCore>();

        var document = new Microsoft.OpenApi.Models.OpenApiDocument
        {
            Info = new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Diagnostics" }
        };

        pipeline.ApplyDocumentFilters(document, new DocumentFilterContext { DocumentName = "v1" });

        var events = logger.GetEvents();
        var errorEvent = Assert.Single(events, e => e.Level == DiagnosticLevel.Error && e.Code == DiagnosticCodes.FilterExecutionCompleted);
        Assert.Contains(nameof(FailingDocumentFilter), errorEvent.Message, System.StringComparison.Ordinal);
        Assert.False(errorEvent.Sanitized);

        await host.StopAsync();
    }
}
