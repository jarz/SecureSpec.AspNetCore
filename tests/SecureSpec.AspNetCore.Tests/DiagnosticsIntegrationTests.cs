using SecureSpec.AspNetCore.Diagnostics;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Integration tests demonstrating real-world usage of the diagnostics system.
/// Shows how different components would log diagnostic events.
/// </summary>
public class DiagnosticsIntegrationTests
{
    [Fact]
    public void IntegrityCheckFailure_LogsWithSanitization()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        const string expectedHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        const string actualHash = "f5a7924e621e84c9280a9a27e1bcb7f6cd456789012345678901234567890123";
        const string filePath = "/path/to/sensitive/document.json";

        // Act - Log integrity failure with sanitized data
        logger.LogCritical(
            DiagnosticCodes.IntegrityCheckFailed,
            "Integrity check failed for OpenAPI document",
            context: new
            {
                File = DiagnosticsLogger.SanitizePath(filePath),
                ExpectedHash = DiagnosticsLogger.SanitizeHash(expectedHash),
                ActualHash = DiagnosticsLogger.SanitizeHash(actualHash)
            },
            sanitized: true);

        // Assert
        var events = logger.GetEvents();
        Assert.Single(events);
        Assert.Equal(DiagnosticLevel.Critical, events[0].Level);
        Assert.Equal(DiagnosticCodes.IntegrityCheckFailed, events[0].Code);
        Assert.True(events[0].Sanitized);
        Assert.NotNull(events[0].Context);
    }

    [Fact]
    public void SchemaIdCollision_LogsWithContext()
    {
        // Arrange
        var logger = new DiagnosticsLogger();

        // Act - Log schema collision
        logger.LogWarning(
            DiagnosticCodes.SchemaIdCollision,
            "SchemaId collision detected, applying suffix",
            context: new
            {
                OriginalSchemaId = "Product",
                NewSchemaId = "Product_schemaDup1",
                TypeFullName = "MyNamespace.Product"
            });

        // Assert
        var events = logger.GetEvents();
        Assert.Single(events);
        Assert.Equal(DiagnosticLevel.Info, DiagnosticCodes.GetMetadata(events[0].Code)!.Level);
        Assert.False(events[0].Sanitized);
    }

    [Fact]
    public void RateLimitEnforced_LogsStructuredContext()
    {
        // Arrange
        var logger = new DiagnosticsLogger();

        // Act - Log rate limit enforcement
        logger.LogInfo(
            DiagnosticCodes.RateLimitEnforced,
            "Rate limit bucket enforced",
            context: new
            {
                Bucket = "TryItOut",
                Remaining = 0,
                WindowSeconds = 60,
                Reason = "limit_exceeded"
            });

        // Assert
        var events = logger.GetEvents();
        Assert.Single(events);
        Assert.Equal(DiagnosticLevel.Info, events[0].Level);
        Assert.Equal(DiagnosticCodes.RateLimitEnforced, events[0].Code);
        dynamic? ctx = events[0].Context;
        Assert.NotNull(ctx);
    }

    [Fact]
    public void MultipleEvents_MaintainChronologicalOrder()
    {
        // Arrange
        var logger = new DiagnosticsLogger();

        // Act - Log multiple events in sequence
        logger.LogInfo(DiagnosticCodes.SchemaIdCollision, "First event");
        Thread.Sleep(10); // Ensure timestamp difference
        logger.LogWarning(DiagnosticCodes.DataAnnotationsConflict, "Second event");
        Thread.Sleep(10);
        logger.LogError(DiagnosticCodes.NullabilityMismatch, "Third event");
        Thread.Sleep(10);
        logger.LogCritical(DiagnosticCodes.IntegrityCheckFailed, "Fourth event");

        // Assert
        var events = logger.GetEvents();
        Assert.Equal(4, events.Count);

        // Verify chronological order
        for (int i = 1; i < events.Count; i++)
        {
            Assert.True(events[i].Timestamp >= events[i - 1].Timestamp,
                "Events should be in chronological order");
        }

        // Verify severity escalation
        Assert.Equal(DiagnosticLevel.Info, events[0].Level);
        Assert.Equal(DiagnosticLevel.Warn, events[1].Level);
        Assert.Equal(DiagnosticLevel.Error, events[2].Level);
        Assert.Equal(DiagnosticLevel.Critical, events[3].Level);
    }

    [Fact]
    public void AllDefinedCodes_CanBeLogged()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        var allCodes = DiagnosticCodes.GetAllCodes();

        // Act - Log one event for each defined code
        foreach (var code in allCodes)
        {
            var metadata = DiagnosticCodes.GetMetadata(code);
            Assert.NotNull(metadata);

            logger.Log(
                metadata.Level,
                code,
                $"Test message for {code}",
                context: new { Code = code, Description = metadata.Description });
        }

        // Assert
        var events = logger.GetEvents();
        Assert.Equal(allCodes.Length, events.Count);

        // Verify all codes are present
        var loggedCodes = events.Select(e => e.Code).ToHashSet();
        foreach (var code in allCodes)
        {
            Assert.Contains(code, loggedCodes);
        }
    }

    [Fact]
    public void CspMismatch_LogsWithPolicyDetails()
    {
        // Arrange
        var logger = new DiagnosticsLogger();

        // Act
        logger.LogError(
            DiagnosticCodes.CspMismatch,
            "CSP directive mismatch detected",
            context: new
            {
                ExpectedDirective = "default-src 'self'",
                ActualDirective = "default-src *",
                Severity = "High"
            });

        // Assert
        var events = logger.GetEvents();
        Assert.Single(events);
        Assert.Equal(DiagnosticLevel.Error, events[0].Level);
        Assert.Equal("CSP directive mismatch detected", events[0].Message);
    }

    [Fact]
    public void VirtualizationThreshold_LogsPerformanceContext()
    {
        // Arrange
        var logger = new DiagnosticsLogger();

        // Act
        logger.LogInfo(
            DiagnosticCodes.VirtualizationThresholdTriggered,
            "Enum virtualization triggered due to size",
            context: new
            {
                EnumName = "LargeEnum",
                ValueCount = 15000,
                Threshold = 10000,
                VirtualizationStrategy = "Segmented"
            });

        // Assert
        var events = logger.GetEvents();
        Assert.Single(events);
        Assert.Equal(DiagnosticLevel.Info, events[0].Level);
    }

    [Fact]
    public void SanitizationScenario_RedactsPathAndHash()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        const string sensitiveFile = "/usr/local/secrets/api-keys/production.json";
        const string longHash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

        // Act
        var sanitizedFile = DiagnosticsLogger.SanitizePath(sensitiveFile);
        var sanitizedHash = DiagnosticsLogger.SanitizeHash(longHash);

        logger.LogError(
            DiagnosticCodes.IntegrityCheckFailed,
            "File integrity violation",
            context: new
            {
                File = sanitizedFile,
                Hash = sanitizedHash
            },
            sanitized: true);

        // Assert
        var events = logger.GetEvents();
        Assert.Single(events);
        Assert.True(events[0].Sanitized);

        // Verify sanitization worked
        Assert.Equal("production.json", sanitizedFile);
        Assert.Equal("01234567...", sanitizedHash);
    }
}
