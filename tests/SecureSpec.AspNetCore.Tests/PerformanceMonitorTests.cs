using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Core;
using SecureSpec.AspNetCore.Diagnostics;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for performance monitoring and thresholds (AC 297-300).
/// </summary>
public class PerformanceMonitorTests
{
    [Fact]
    public void PerformanceMonitor_Constructor_InitializesCorrectly()
    {
        // Arrange
        var options = new PerformanceOptions();
        var logger = new DiagnosticsLogger();

        // Act
        using var monitor = new PerformanceMonitor(options, logger, "test-operation");

        // Assert
        Assert.NotNull(monitor);
        Assert.True(monitor.ElapsedMilliseconds >= 0);
    }

    [Fact]
    public void PerformanceMonitor_GetStatus_ReturnsTarget_WhenUnderThreshold()
    {
        // Arrange
        var options = new PerformanceOptions
        {
            EnablePerformanceMonitoring = true,
            TargetGenerationTimeMs = 500,
            DegradedThresholdMs = 2000,
            BaselineOperationCount = 1000
        };
        var logger = new DiagnosticsLogger();

        // Act
        using var monitor = new PerformanceMonitor(options, logger, "test-operation", 1000);
        // Very short operation - should be under target
        var status = monitor.GetStatus();

        // Assert
        Assert.Equal(PerformanceStatus.Target, status);
    }

    [Fact]
    public void PerformanceMonitor_GetStatus_ReturnsDegraded_WhenAboveTargetBelowFailure()
    {
        // Arrange
        var options = new PerformanceOptions
        {
            EnablePerformanceMonitoring = true,
            TargetGenerationTimeMs = 1, // Very low threshold
            DegradedThresholdMs = 2000,
            BaselineOperationCount = 1000
        };
        var logger = new DiagnosticsLogger();

        // Act
        using var monitor = new PerformanceMonitor(options, logger, "test-operation", 1000);
        Thread.Sleep(10); // Exceed target but not degraded threshold
        var status = monitor.GetStatus();

        // Assert
        Assert.Equal(PerformanceStatus.Degraded, status);
    }

    [Fact]
    public void PerformanceMonitor_GetStatus_ReturnsFailure_WhenAboveFailureThreshold()
    {
        // Arrange
        var options = new PerformanceOptions
        {
            EnablePerformanceMonitoring = true,
            TargetGenerationTimeMs = 1,
            DegradedThresholdMs = 10, // Very low threshold
            BaselineOperationCount = 1000
        };
        var logger = new DiagnosticsLogger();

        // Act
        using var monitor = new PerformanceMonitor(options, logger, "test-operation", 1000);
        Thread.Sleep(50); // Exceed failure threshold
        var status = monitor.GetStatus();

        // Assert
        Assert.Equal(PerformanceStatus.Failure, status);
    }

    [Fact]
    public void PerformanceMonitor_GetStatus_ReturnsNotMonitored_WhenDisabled()
    {
        // Arrange
        var options = new PerformanceOptions
        {
            EnablePerformanceMonitoring = false
        };
        var logger = new DiagnosticsLogger();

        // Act
        using var monitor = new PerformanceMonitor(options, logger, "test-operation");
        var status = monitor.GetStatus();

        // Assert
        Assert.Equal(PerformanceStatus.NotMonitored, status);
    }

    [Fact]
    public void PerformanceMonitor_Stop_EmitsPERF002_WhenTargetMet()
    {
        // Arrange
        var options = new PerformanceOptions
        {
            EnablePerformanceMonitoring = true,
            TargetGenerationTimeMs = 500,
            DegradedThresholdMs = 2000,
            BaselineOperationCount = 1000
        };
        var logger = new DiagnosticsLogger();

        // Act
        using var monitor = new PerformanceMonitor(options, logger, "test-operation", 1000);
        monitor.Stop();

        // Assert
        var events = logger.GetEvents();
        Assert.Contains(events, e => e.Code == DiagnosticCodes.PerformanceTargetMet);
    }

    [Fact]
    public void PerformanceMonitor_Stop_EmitsPERF003_WhenDegraded()
    {
        // Arrange
        var options = new PerformanceOptions
        {
            EnablePerformanceMonitoring = true,
            TargetGenerationTimeMs = 1,
            DegradedThresholdMs = 2000,
            BaselineOperationCount = 1000
        };
        var logger = new DiagnosticsLogger();

        // Act
        using var monitor = new PerformanceMonitor(options, logger, "test-operation", 1000);
        Thread.Sleep(10); // Exceed target
        monitor.Stop();

        // Assert
        var events = logger.GetEvents();
        Assert.Contains(events, e => e.Code == DiagnosticCodes.PerformanceDegraded);
    }

    [Fact]
    public void PerformanceMonitor_Stop_EmitsPERF004_WhenFailure()
    {
        // Arrange
        var options = new PerformanceOptions
        {
            EnablePerformanceMonitoring = true,
            TargetGenerationTimeMs = 1,
            DegradedThresholdMs = 10,
            BaselineOperationCount = 1000
        };
        var logger = new DiagnosticsLogger();

        // Act
        using var monitor = new PerformanceMonitor(options, logger, "test-operation", 1000);
        Thread.Sleep(50); // Exceed failure threshold
        monitor.Stop();

        // Assert
        var events = logger.GetEvents();
        Assert.Contains(events, e => e.Code == DiagnosticCodes.PerformanceFailure);
    }

    [Fact]
    public void PerformanceMonitor_Stop_EmitsPERF005_ForMetrics()
    {
        // Arrange
        var options = new PerformanceOptions
        {
            EnablePerformanceMonitoring = true
        };
        var logger = new DiagnosticsLogger();

        // Act
        using var monitor = new PerformanceMonitor(options, logger, "test-operation");
        monitor.Stop();

        // Assert
        var events = logger.GetEvents();
        Assert.Contains(events, e => e.Code == DiagnosticCodes.PerformanceMetrics);
    }

    [Fact]
    public void PerformanceMonitor_Dispose_StopsMonitoring()
    {
        // Arrange
        var options = new PerformanceOptions
        {
            EnablePerformanceMonitoring = true
        };
        var logger = new DiagnosticsLogger();
        var monitor = new PerformanceMonitor(options, logger, "test-operation");

        // Act
        monitor.Dispose();

        // Assert - should not throw
        monitor.Dispose(); // Second dispose should be safe
    }

    [Fact]
    public void PerformanceMonitor_NormalizesToBaseline_Correctly()
    {
        // Arrange - simulate 500 operations
        var options = new PerformanceOptions
        {
            EnablePerformanceMonitoring = true,
            TargetGenerationTimeMs = 500,
            BaselineOperationCount = 1000
        };
        var logger = new DiagnosticsLogger();

        // Act
        using var monitor = new PerformanceMonitor(options, logger, "test-operation", 500);
        Thread.Sleep(100); // 100ms for 500 ops = 200ms for 1000 ops
        monitor.Stop();

        // Assert
        var events = logger.GetEvents();
        var metricsEvent = events.FirstOrDefault(e => e.Code == DiagnosticCodes.PerformanceMetrics);
        Assert.NotNull(metricsEvent);

        // The normalized time should be approximately 200ms (double the actual 100ms)
        // Use reflection to access anonymous type properties
        Assert.NotNull(metricsEvent.Context);
        var contextType = metricsEvent.Context.GetType();
        var normalizedMsProp = contextType.GetProperty("NormalizedMs");
        Assert.NotNull(normalizedMsProp);

        var normalizedMs = (long)normalizedMsProp.GetValue(metricsEvent.Context)!;
        // Allow some tolerance for timing variance
        Assert.InRange(normalizedMs, 150, 300);
    }

    [Fact]
    public void PerformanceMonitor_ThrowsOnNullOptions()
    {
        // Arrange
        var logger = new DiagnosticsLogger();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PerformanceMonitor(null!, logger, "test"));
    }

    [Fact]
    public void PerformanceMonitor_ThrowsOnNullLogger()
    {
        // Arrange
        var options = new PerformanceOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PerformanceMonitor(options, null!, "test"));
    }

    [Fact]
    public void PerformanceMonitor_ThrowsOnNullOperationName()
    {
        // Arrange
        var options = new PerformanceOptions();
        var logger = new DiagnosticsLogger();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PerformanceMonitor(options, logger, null!));
    }

    [Fact]
    public void PerformanceMonitor_ThrowsOnNegativeOperationCount()
    {
        // Arrange
        var options = new PerformanceOptions();
        var logger = new DiagnosticsLogger();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PerformanceMonitor(options, logger, "test", -1));
    }

    [Fact]
    public void PerformanceMonitor_ThrowsOnZeroOperationCount()
    {
        // Arrange
        var options = new PerformanceOptions();
        var logger = new DiagnosticsLogger();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PerformanceMonitor(options, logger, "test", 0));
    }
}

/// <summary>
/// Integration tests for document generation with performance monitoring (AC 297-300).
/// </summary>
public class DocumentGenerationPerformanceTests
{
    [Fact]
    public void DocumentGenerator_WithMonitoring_EmitsPerformanceMetrics()
    {
        // Arrange
        var options = new SecureSpecOptions
        {
            Performance =
            {
                EnableResourceGuards = true,
                EnablePerformanceMonitoring = true,
                TargetGenerationTimeMs = 500
            }
        };
        var logger = new DiagnosticsLogger();
        var generator = new DocumentGenerator(options, logger);

        // Act
        var doc = generator.GenerateWithGuards("test", () => new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test", Version = "1.0" },
            Paths = new OpenApiPaths()
        });

        // Assert
        Assert.NotNull(doc);
        var events = logger.GetEvents();
        Assert.Contains(events, e => e.Code == DiagnosticCodes.PerformanceMetrics);
    }

    [Fact]
    public void DocumentGenerator_WithMonitoring_TracksOperationCount()
    {
        // Arrange
        var options = new SecureSpecOptions
        {
            Performance =
            {
                EnableResourceGuards = true,
                EnablePerformanceMonitoring = true,
                TargetGenerationTimeMs = 500
            }
        };
        var logger = new DiagnosticsLogger();
        var generator = new DocumentGenerator(options, logger);

        // Act - simulate 1000 operations
        var doc = generator.GenerateWithGuards("test", () => new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test", Version = "1.0" },
            Paths = new OpenApiPaths()
        }, operationCount: 1000);

        // Assert
        Assert.NotNull(doc);
        var events = logger.GetEvents();
        var metricsEvent = events.FirstOrDefault(e => e.Code == DiagnosticCodes.PerformanceMetrics);
        Assert.NotNull(metricsEvent);

        // Use reflection to access anonymous type properties
        Assert.NotNull(metricsEvent.Context);
        var contextType = metricsEvent.Context.GetType();
        var operationCountProp = contextType.GetProperty("OperationCount");
        Assert.NotNull(operationCountProp);
        Assert.Equal(1000, (int)operationCountProp.GetValue(metricsEvent.Context)!);
    }

    [Fact]
    public void DocumentGenerator_WithMonitoringDisabled_NoMetrics()
    {
        // Arrange
        var options = new SecureSpecOptions
        {
            Performance =
            {
                EnableResourceGuards = true,
                EnablePerformanceMonitoring = false
            }
        };
        var logger = new DiagnosticsLogger();
        var generator = new DocumentGenerator(options, logger);

        // Act
        var doc = generator.GenerateWithGuards("test", () => new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test", Version = "1.0" },
            Paths = new OpenApiPaths()
        });

        // Assert
        Assert.NotNull(doc);
        var events = logger.GetEvents();
        Assert.DoesNotContain(events, e => e.Code == DiagnosticCodes.PerformanceMetrics);
    }

    [Fact]
    public void DocumentGenerator_MeetsTargetThreshold_AC297()
    {
        // Arrange - AC 297: <500ms for 1000 operations
        var options = new SecureSpecOptions
        {
            Performance =
            {
                EnableResourceGuards = true,
                EnablePerformanceMonitoring = true,
                TargetGenerationTimeMs = 500,
                BaselineOperationCount = 1000
            }
        };
        var logger = new DiagnosticsLogger();
        var generator = new DocumentGenerator(options, logger);

        // Act - fast generation
        var doc = generator.GenerateWithGuards("test", () => new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test", Version = "1.0" },
            Paths = new OpenApiPaths()
        }, operationCount: 1000);

        // Assert
        Assert.NotNull(doc);
        var events = logger.GetEvents();

        // Should emit target met (PERF002) for fast operations
        var targetMetEvent = events.FirstOrDefault(e => e.Code == DiagnosticCodes.PerformanceTargetMet);
        Assert.NotNull(targetMetEvent);
    }

    [Fact]
    public void DocumentGenerator_DetectsDegradedPerformance_AC298()
    {
        // Arrange - AC 298: Degraded threshold (500-2000ms)
        var options = new SecureSpecOptions
        {
            Performance =
            {
                EnableResourceGuards = true,
                EnablePerformanceMonitoring = true,
                TargetGenerationTimeMs = 1, // Very low to trigger degraded
                DegradedThresholdMs = 2000,
                BaselineOperationCount = 1000
            }
        };
        var logger = new DiagnosticsLogger();
        var generator = new DocumentGenerator(options, logger);

        // Act - add some delay to exceed target
        var doc = generator.GenerateWithGuards("test", () =>
        {
            Thread.Sleep(10); // Small delay to exceed very low target
            return new OpenApiDocument
            {
                Info = new OpenApiInfo { Title = "Test", Version = "1.0" },
                Paths = new OpenApiPaths()
            };
        }, operationCount: 1000);

        // Assert
        Assert.NotNull(doc);
        var events = logger.GetEvents();

        // Should emit degraded warning (PERF003)
        var degradedEvent = events.FirstOrDefault(e => e.Code == DiagnosticCodes.PerformanceDegraded);
        Assert.NotNull(degradedEvent);
    }

    [Fact]
    public void DocumentGenerator_DetectsPerformanceFailure_AC299()
    {
        // Arrange - AC 299: Failure threshold (>2000ms)
        var options = new SecureSpecOptions
        {
            Performance =
            {
                EnableResourceGuards = true,
                EnablePerformanceMonitoring = true,
                TargetGenerationTimeMs = 1,
                DegradedThresholdMs = 10, // Very low to trigger failure
                BaselineOperationCount = 1000
            }
        };
        var logger = new DiagnosticsLogger();
        var generator = new DocumentGenerator(options, logger);

        // Act - add significant delay to exceed failure threshold
        var doc = generator.GenerateWithGuards("test", () =>
        {
            Thread.Sleep(50); // Enough delay to exceed very low threshold
            return new OpenApiDocument
            {
                Info = new OpenApiInfo { Title = "Test", Version = "1.0" },
                Paths = new OpenApiPaths()
            };
        }, operationCount: 1000);

        // Assert
        Assert.NotNull(doc);
        var events = logger.GetEvents();

        // Should emit failure error (PERF004)
        var failureEvent = events.FirstOrDefault(e => e.Code == DiagnosticCodes.PerformanceFailure);
        Assert.NotNull(failureEvent);
    }

    [Fact]
    public void DocumentGenerator_CollectsMetrics_AC300()
    {
        // Arrange - AC 300: Performance monitoring
        var options = new SecureSpecOptions
        {
            Performance =
            {
                EnableResourceGuards = true,
                EnablePerformanceMonitoring = true
            }
        };
        var logger = new DiagnosticsLogger();
        var generator = new DocumentGenerator(options, logger);

        // Act
        var doc = generator.GenerateWithGuards("test", () => new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test", Version = "1.0" },
            Paths = new OpenApiPaths()
        }, operationCount: 1000);

        // Assert
        Assert.NotNull(doc);
        var events = logger.GetEvents();

        // Should always emit metrics (PERF005)
        var metricsEvent = events.FirstOrDefault(e => e.Code == DiagnosticCodes.PerformanceMetrics);
        Assert.NotNull(metricsEvent);

        // Verify metrics include expected fields using reflection
        Assert.NotNull(metricsEvent.Context);
        var contextType = metricsEvent.Context.GetType();

        var operationCountProp = contextType.GetProperty("OperationCount");
        Assert.NotNull(operationCountProp);
        Assert.Equal(1000, (int)operationCountProp.GetValue(metricsEvent.Context)!);

        var elapsedMsProp = contextType.GetProperty("ElapsedMs");
        Assert.NotNull(elapsedMsProp);
        Assert.True((long)elapsedMsProp.GetValue(metricsEvent.Context)! >= 0);

        var statusProp = contextType.GetProperty("Status");
        Assert.NotNull(statusProp);
        Assert.NotNull(statusProp.GetValue(metricsEvent.Context));
    }
}
