using Microsoft.Extensions.Time.Testing;
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
        };
        var logger = new DiagnosticsLogger();

        // Act
        using var monitor = new PerformanceMonitor(options, logger, "test-operation");
        // Very short operation - should be under target
        var status = monitor.Status;

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
        };
        var logger = new DiagnosticsLogger();
        var fakeTime = new FakeTimeProvider();

        // Act
        using var monitor = new PerformanceMonitor(options, logger, "test-operation", fakeTime);
        fakeTime.Advance(TimeSpan.FromMilliseconds(10)); // Exceed target but not degraded threshold
        var status = monitor.Status;

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
        };
        var logger = new DiagnosticsLogger();
        var fakeTime = new FakeTimeProvider();

        // Act
        using var monitor = new PerformanceMonitor(options, logger, "test-operation", fakeTime);
        fakeTime.Advance(TimeSpan.FromMilliseconds(50)); // Exceed failure threshold
        var status = monitor.Status;

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
        var status = monitor.Status;

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
        };
        var logger = new DiagnosticsLogger();

        // Act
        using var monitor = new PerformanceMonitor(options, logger, "test-operation");
        monitor.Stop();

        // Assert
        var events = logger.GetEvents();
        Assert.Contains(events, e => e.Code == DiagnosticCodes.Performance.PerformanceTargetMet);
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
        };
        var logger = new DiagnosticsLogger();
        var fakeTime = new FakeTimeProvider();

        // Act
        using var monitor = new PerformanceMonitor(options, logger, "test-operation", fakeTime);
        fakeTime.Advance(TimeSpan.FromMilliseconds(10)); // Exceed target
        monitor.Stop();

        // Assert
        var events = logger.GetEvents();
        Assert.Contains(events, e => e.Code == DiagnosticCodes.Performance.PerformanceDegraded);
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
        };
        var logger = new DiagnosticsLogger();
        var fakeTime = new FakeTimeProvider();

        // Act
        using var monitor = new PerformanceMonitor(options, logger, "test-operation", fakeTime);
        fakeTime.Advance(TimeSpan.FromMilliseconds(50)); // Exceed failure threshold
        monitor.Stop();

        // Assert
        var events = logger.GetEvents();
        Assert.Contains(events, e => e.Code == DiagnosticCodes.Performance.PerformanceFailure);
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
        Assert.Contains(events, e => e.Code == DiagnosticCodes.Performance.PerformanceMetrics);
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
#pragma warning disable CA2202 // Intentional double dispose for idempotence verification
        monitor.Dispose();
        var eventsAfterFirstDispose = logger.GetEvents();

        // Assert
        Assert.Contains(eventsAfterFirstDispose, e => e.Code == DiagnosticCodes.Performance.PerformanceMetrics);
        var totalEventsAfterFirstDispose = eventsAfterFirstDispose.Count;

        // Second dispose should be a no-op with no additional diagnostics
        monitor.Dispose();
        var eventsAfterSecondDispose = logger.GetEvents();
        Assert.Equal(totalEventsAfterFirstDispose, eventsAfterSecondDispose.Count);
        Assert.Equal(1, eventsAfterSecondDispose.Count(e => e.Code == DiagnosticCodes.Performance.PerformanceMetrics));
#pragma warning restore CA2202
    }

    [Fact]
    public void PerformanceMonitor_TracksElapsedTime_Correctly()
    {
        // Arrange
        var options = new PerformanceOptions
        {
            EnablePerformanceMonitoring = true,
            TargetGenerationTimeMs = 500
        };
        var logger = new DiagnosticsLogger();

        // Act - perform actual work to consume some time
        using var monitor = new PerformanceMonitor(options, logger, "test-operation");

        // Do some actual work to consume time
        var sum = 0;
        for (var i = 0; i < 100000; i++)
        {
            sum += i;
        }
        Assert.True(sum > 0); // Ensure sum is used so the loop is not optimized away
        monitor.Stop();

        // Assert
        var events = logger.GetEvents();
        var metricsEvent = events.FirstOrDefault(e => e.Code == DiagnosticCodes.Performance.PerformanceMetrics);
        Assert.NotNull(metricsEvent);

        // Verify elapsed time is tracked
        Assert.NotNull(metricsEvent.Context);
        var contextType = metricsEvent.Context.GetType();

        var elapsedMsProp = contextType.GetProperty("ElapsedMs");
        Assert.NotNull(elapsedMsProp);
        var elapsedMs = (long)elapsedMsProp.GetValue(metricsEvent.Context)!;

        // Elapsed time should be positive and reasonable (some work was done)
        Assert.True(elapsedMs >= 0);

        // Verify context includes expected fields
        var targetMsProp = contextType.GetProperty("TargetMs");
        Assert.NotNull(targetMsProp);
        Assert.Equal(500, (int)targetMsProp.GetValue(metricsEvent.Context)!);

        var statusProp = contextType.GetProperty("Status");
        Assert.NotNull(statusProp);
        Assert.NotNull(statusProp.GetValue(metricsEvent.Context));
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
        Assert.Contains(events, e => e.Code == DiagnosticCodes.Performance.PerformanceMetrics);
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
        Assert.DoesNotContain(events, e => e.Code == DiagnosticCodes.Performance.PerformanceMetrics);
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
            }
        };
        var logger = new DiagnosticsLogger();
        var generator = new DocumentGenerator(options, logger);

        // Act - fast generation
        var doc = generator.GenerateWithGuards("test", () => new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test", Version = "1.0" },
            Paths = new OpenApiPaths()
        });

        // Assert
        Assert.NotNull(doc);
        var events = logger.GetEvents();

        // Should emit target met (PERF002) for fast operations
        var targetMetEvent = events.FirstOrDefault(e => e.Code == DiagnosticCodes.Performance.PerformanceTargetMet);
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
            }
        };
        var logger = new DiagnosticsLogger();
        var generator = new DocumentGenerator(options, logger);

        // Act - add some CPU work to exceed very low target
        var doc = generator.GenerateWithGuards("test", () =>
        {
            // Use actual CPU work instead of Thread.Sleep for cross-platform reliability
            var sw = System.Diagnostics.Stopwatch.StartNew();
            double dummy = 0;
            while (sw.ElapsedMilliseconds < 10)
            {
                dummy += Math.Sqrt(sw.ElapsedMilliseconds + 1);
            }
            return new OpenApiDocument
            {
                Info = new OpenApiInfo { Title = "Test", Version = "1.0" },
                Paths = new OpenApiPaths()
            };
        });

        // Assert
        Assert.NotNull(doc);
        var events = logger.GetEvents();

        // Should emit degraded warning (PERF003)
        var degradedEvent = events.FirstOrDefault(e => e.Code == DiagnosticCodes.Performance.PerformanceDegraded);
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
            }
        };
        var logger = new DiagnosticsLogger();
        var generator = new DocumentGenerator(options, logger);

        // Act - add significant CPU work to exceed failure threshold
        var doc = generator.GenerateWithGuards("test", () =>
        {
            // Use actual CPU work instead of Thread.Sleep for cross-platform reliability
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 50)
            {
                // Perform some work
                Math.Sqrt(sw.ElapsedMilliseconds);
            }
            return new OpenApiDocument
            {
                Info = new OpenApiInfo { Title = "Test", Version = "1.0" },
                Paths = new OpenApiPaths()
            };
        });

        // Assert
        Assert.NotNull(doc);
        var events = logger.GetEvents();

        // Should emit failure error (PERF004)
        var failureEvent = events.FirstOrDefault(e => e.Code == DiagnosticCodes.Performance.PerformanceFailure);
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
        });

        // Assert
        Assert.NotNull(doc);
        var events = logger.GetEvents();

        // Should always emit metrics (PERF005)
        var metricsEvent = events.FirstOrDefault(e => e.Code == DiagnosticCodes.Performance.PerformanceMetrics);
        Assert.NotNull(metricsEvent);

        // Verify metrics include expected fields using reflection
        Assert.NotNull(metricsEvent.Context);
        var contextType = metricsEvent.Context.GetType();

        var elapsedMsProp = contextType.GetProperty("ElapsedMs");
        Assert.NotNull(elapsedMsProp);
        Assert.True((long)elapsedMsProp.GetValue(metricsEvent.Context)! >= 0);

        var statusProp = contextType.GetProperty("Status");
        Assert.NotNull(statusProp);
        Assert.NotNull(statusProp.GetValue(metricsEvent.Context));
    }
}
