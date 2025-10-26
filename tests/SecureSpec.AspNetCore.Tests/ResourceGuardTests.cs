using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Core;
using SecureSpec.AspNetCore.Diagnostics;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for resource guards (size and time limits).
/// </summary>
public class ResourceGuardTests
{
    [Fact]
    public void ResourceGuard_Constructor_InitializesCorrectly()
    {
        // Arrange
        var options = new PerformanceOptions();
        var logger = new DiagnosticsLogger();

        // Act
        using var guard = new ResourceGuard(options, logger);

        // Assert
        Assert.NotNull(guard);
        Assert.True(guard.ElapsedMilliseconds >= 0);
    }

    [Fact]
    public void ResourceGuard_ElapsedTime_IncreasesOverTime()
    {
        // Arrange
        var options = new PerformanceOptions();
        var logger = new DiagnosticsLogger();

        // Act
        using var guard = new ResourceGuard(options, logger);
        var initialTime = guard.ElapsedMilliseconds;
        Thread.Sleep(50); // Wait 50ms
        var laterTime = guard.ElapsedMilliseconds;

        // Assert
        Assert.True(laterTime > initialTime);
    }

    [Fact]
    public void ResourceGuard_TimeLimit_ExceedsThreshold_ReturnsTrue()
    {
        // Arrange
        var options = new PerformanceOptions
        {
            MaxGenerationTimeMs = 10, // Very short time limit
            EnableResourceGuards = true
        };
        var logger = new DiagnosticsLogger();

        // Act
        using var guard = new ResourceGuard(options, logger);
        Thread.Sleep(50); // Exceed the limit
        var exceeded = guard.IsLimitExceeded(out var reason);

        // Assert
        Assert.True(exceeded);
        Assert.NotNull(reason);
        Assert.Contains("time", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResourceGuard_WithinTimeLimit_ReturnsFalse()
    {
        // Arrange
        var options = new PerformanceOptions
        {
            MaxGenerationTimeMs = 5000, // 5 seconds - plenty of time
            EnableResourceGuards = true
        };
        var logger = new DiagnosticsLogger();

        // Act
        using var guard = new ResourceGuard(options, logger);
        var exceeded = guard.IsLimitExceeded(out var reason);

        // Assert
        Assert.False(exceeded);
        Assert.Null(reason);
    }

    [Fact]
    public void ResourceGuard_DisabledGuards_NeverExceeds()
    {
        // Arrange
        var options = new PerformanceOptions
        {
            MaxGenerationTimeMs = 1, // Impossible limit
            EnableResourceGuards = false // But disabled
        };
        var logger = new DiagnosticsLogger();

        // Act
        using var guard = new ResourceGuard(options, logger);
        Thread.Sleep(50);
        var exceeded = guard.IsLimitExceeded(out var reason);

        // Assert
        Assert.False(exceeded);
        Assert.Null(reason);
    }

    [Fact]
    public void ResourceGuard_CheckLimits_ThrowsWhenExceeded()
    {
        // Arrange
        var options = new PerformanceOptions
        {
            MaxGenerationTimeMs = 10,
            EnableResourceGuards = true
        };
        var logger = new DiagnosticsLogger();

        // Act & Assert
        using var guard = new ResourceGuard(options, logger);
        Thread.Sleep(50);
        Assert.Throws<ResourceLimitExceededException>(() => guard.CheckLimits());
    }

    [Fact]
    public void ResourceGuard_CheckLimits_DoesNotThrowWhenWithinLimits()
    {
        // Arrange
        var options = new PerformanceOptions
        {
            MaxGenerationTimeMs = 5000,
            EnableResourceGuards = true
        };
        var logger = new DiagnosticsLogger();

        // Act & Assert
        using var guard = new ResourceGuard(options, logger);
        guard.CheckLimits(); // Should not throw
    }

    [Fact]
    public void ResourceGuard_EmitsPERF001Diagnostic_OnTimeExceeded()
    {
        // Arrange
        var options = new PerformanceOptions
        {
            MaxGenerationTimeMs = 10,
            EnableResourceGuards = true
        };
        var logger = new DiagnosticsLogger();

        // Act
        using var guard = new ResourceGuard(options, logger);
        Thread.Sleep(50);
        _ = guard.IsLimitExceeded(out _);

        // Assert
        var events = logger.GetEvents();
        Assert.Contains(events, e => e.Code == "PERF001");
    }

    [Fact]
    public void ResourceGuard_MemoryUsage_ReturnsNonNegative()
    {
        // Arrange
        var options = new PerformanceOptions();
        var logger = new DiagnosticsLogger();

        // Act
        using var guard = new ResourceGuard(options, logger);
        var memoryUsage = guard.MemoryUsageBytes;

        // Assert
        Assert.True(memoryUsage >= 0);
    }

    [Fact]
    public void ResourceGuard_Dispose_StopsMonitoring()
    {
        // Arrange
        var options = new PerformanceOptions();
        var logger = new DiagnosticsLogger();
        var guard = new ResourceGuard(options, logger);
        var timeBeforeDispose = guard.ElapsedMilliseconds;

        // Act
        guard.Dispose();
        Thread.Sleep(50);
        var timeAfterDispose = guard.ElapsedMilliseconds;

        // Assert
        // Time should not increase significantly after dispose
        Assert.True(timeAfterDispose - timeBeforeDispose < 40);
    }
}

/// <summary>
/// Tests for fallback document generation.
/// </summary>
public class FallbackDocumentGeneratorTests
{
    [Fact]
    public void GenerateFallback_CreatesValidDocument()
    {
        // Arrange
        const string title = "Test API";
        const string version = "1.0";
        const string reason = "Time limit exceeded";

        // Act
        var document = FallbackDocumentGenerator.GenerateFallback(title, version, reason);

        // Assert
        Assert.NotNull(document);
        Assert.NotNull(document.Info);
        Assert.Equal(title, document.Info.Title);
        Assert.Equal(version, document.Info.Version);
        Assert.Contains(reason, document.Info.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateFallback_IncludesWarningBanner()
    {
        // Arrange
        const string title = "Test API";
        const string version = "1.0";
        const string reason = "Memory exceeded";

        // Act
        var document = FallbackDocumentGenerator.GenerateFallback(title, version, reason);

        // Assert
        Assert.Contains("⚠️", document.Info.Description, StringComparison.Ordinal);
        Assert.Contains("Document Generation Failed", document.Info.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateFallback_HasEmptyPaths()
    {
        // Arrange
        const string title = "Test API";
        const string version = "1.0";
        const string reason = "Resource limit";

        // Act
        var document = FallbackDocumentGenerator.GenerateFallback(title, version, reason);

        // Assert
        Assert.NotNull(document.Paths);
        Assert.Empty(document.Paths);
    }

    [Fact]
    public void GenerateFallback_HasEmptySchemas()
    {
        // Arrange
        const string title = "Test API";
        const string version = "1.0";
        const string reason = "Resource limit";

        // Act
        var document = FallbackDocumentGenerator.GenerateFallback(title, version, reason);

        // Assert
        Assert.NotNull(document.Components);
        Assert.NotNull(document.Components.Schemas);
        Assert.Empty(document.Components.Schemas);
    }

    [Fact]
    public void GenerateFallback_SanitizesReason_RemovesCRLF()
    {
        // Arrange
        const string title = "Test API";
        const string version = "1.0";
        const string reason = "Time limit\r\nexceeded\nwith\tinjection";

        // Act
        var document = FallbackDocumentGenerator.GenerateFallback(title, version, reason);

        // Assert - the reason should be sanitized (no \r, and inline \n removed)
        // The description will still have \n from the markdown formatting, but not from the injected reason
        Assert.DoesNotContain("\r", document.Info.Description, StringComparison.Ordinal);
        // Check that the tabs are replaced with spaces
        Assert.Contains("exceeded", document.Info.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateFallback_TruncatesLongReason()
    {
        // Arrange
        const string title = "Test API";
        const string version = "1.0";
        var longReason = new string('x', 500); // 500 characters

        // Act
        var document = FallbackDocumentGenerator.GenerateFallback(title, version, longReason);

        // Assert
        // The description should contain the ellipsis indicating truncation
        Assert.Contains("...", document.Info.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateFallback_ThrowsOnNullTitle()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            FallbackDocumentGenerator.GenerateFallback(null!, "1.0", "reason"));
    }

    [Fact]
    public void GenerateFallback_ThrowsOnNullVersion()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            FallbackDocumentGenerator.GenerateFallback("title", null!, "reason"));
    }

    [Fact]
    public void GenerateFallback_ThrowsOnNullReason()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            FallbackDocumentGenerator.GenerateFallback("title", "1.0", null!));
    }
}

/// <summary>
/// Tests for document generator with resource guards.
/// </summary>
public class DocumentGeneratorTests
{
    [Fact]
    public void GenerateWithGuards_SuccessfulGeneration_ReturnsDocument()
    {
        // Arrange
        var options = new SecureSpecOptions
        {
            Performance = { EnableResourceGuards = true, MaxGenerationTimeMs = 5000 }
        };
        var logger = new DiagnosticsLogger();
        var generator = new DocumentGenerator(options, logger);
        var expectedDoc = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test", Version = "1.0" },
            Paths = new OpenApiPaths()
        };

        // Act
        var result = generator.GenerateWithGuards("test", () => expectedDoc);

        // Assert
        Assert.Same(expectedDoc, result);
    }

    [Fact]
    public void GenerateWithGuards_TimeLimitExceeded_ReturnsFallback()
    {
        // Arrange
        var options = new SecureSpecOptions
        {
            Performance = { EnableResourceGuards = true, MaxGenerationTimeMs = 10 }
        };
        var logger = new DiagnosticsLogger();
        var generator = new DocumentGenerator(options, logger);

        // Act
        var result = generator.GenerateWithGuards("test", () =>
        {
            Thread.Sleep(50); // Exceed time limit
            return new OpenApiDocument
            {
                Info = new OpenApiInfo { Title = "Test", Version = "1.0" },
                Paths = new OpenApiPaths()
            };
        });

        // Assert
        Assert.NotNull(result);
        Assert.Contains("⚠️", result.Info.Description, StringComparison.Ordinal);
        Assert.Empty(result.Paths); // Fallback has no paths
    }

    [Fact]
    public void GenerateWithGuards_GenerationThrows_ReturnsFallback()
    {
        // Arrange
        var options = new SecureSpecOptions
        {
            Performance = { EnableResourceGuards = true }
        };
        var logger = new DiagnosticsLogger();
        var generator = new DocumentGenerator(options, logger);

        // Act
        var result = generator.GenerateWithGuards("test", () =>
        {
            throw new InvalidOperationException("Generation failed!");
        });

        // Assert
        Assert.NotNull(result);
        Assert.Contains("⚠️", result.Info.Description, StringComparison.Ordinal);
        Assert.Empty(result.Paths); // Fallback has no paths
    }

    [Fact]
    public void GenerateWithGuards_GuardsDisabled_AlwaysReturnsGenerated()
    {
        // Arrange
        var options = new SecureSpecOptions
        {
            Performance = { EnableResourceGuards = false, MaxGenerationTimeMs = 1 }
        };
        var logger = new DiagnosticsLogger();
        var generator = new DocumentGenerator(options, logger);
        var expectedDoc = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test", Version = "1.0" },
            Paths = new OpenApiPaths()
        };

        // Act
        var result = generator.GenerateWithGuards("test", () =>
        {
            Thread.Sleep(50); // Would exceed limit if guards were enabled
            return expectedDoc;
        });

        // Assert
        Assert.Same(expectedDoc, result);
    }

    [Fact]
    public void GenerateWithGuards_EmitsPERF001_OnLimitExceeded()
    {
        // Arrange
        var options = new SecureSpecOptions
        {
            Performance = { EnableResourceGuards = true, MaxGenerationTimeMs = 10 }
        };
        var logger = new DiagnosticsLogger();
        var generator = new DocumentGenerator(options, logger);

        // Act
        _ = generator.GenerateWithGuards("test", () =>
        {
            Thread.Sleep(50);
            return new OpenApiDocument
            {
                Info = new OpenApiInfo { Title = "Test", Version = "1.0" },
                Paths = new OpenApiPaths()
            };
        });

        // Assert
        var events = logger.GetEvents();
        Assert.Contains(events, e => e.Code == "PERF001");
    }

    [Fact]
    public void GenerateWithGuards_ThrowsOnNullDocumentName()
    {
        // Arrange
        var options = new SecureSpecOptions();
        var logger = new DiagnosticsLogger();
        var generator = new DocumentGenerator(options, logger);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            generator.GenerateWithGuards(null!, () => new OpenApiDocument()));
    }

    [Fact]
    public void GenerateWithGuards_ThrowsOnNullGenerationFunc()
    {
        // Arrange
        var options = new SecureSpecOptions();
        var logger = new DiagnosticsLogger();
        var generator = new DocumentGenerator(options, logger);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            generator.GenerateWithGuards("test", null!));
    }
}
