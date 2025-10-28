using System.Diagnostics;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Diagnostics;

namespace SecureSpec.AspNetCore.Core;

/// <summary>
/// Monitors document generation performance and emits diagnostics based on configured thresholds.
/// Tracks performance against targets defined in AC 297-300 (target: &lt;500ms, degraded: 500-2000ms, failure: &gt;2000ms).
/// </summary>
public sealed class PerformanceMonitor : IDisposable
{
    private readonly PerformanceOptions _options;
    private readonly DiagnosticsLogger _logger;
    private readonly Stopwatch _stopwatch;
    private readonly string _operationName;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceMonitor"/> class.
    /// </summary>
    /// <param name="options">Performance configuration options.</param>
    /// <param name="logger">Diagnostics logger for emitting performance events.</param>
    /// <param name="operationName">Name of the operation being monitored.</param>
    public PerformanceMonitor(
        PerformanceOptions options,
        DiagnosticsLogger logger,
        string operationName)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(operationName);

        _options = options;
        _logger = logger;
        _operationName = operationName;
        _stopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Gets the elapsed time in milliseconds since monitoring started.
    /// </summary>
    public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;

    /// <summary>
    /// Gets the current performance status based on elapsed time and thresholds.
    /// </summary>
    public PerformanceStatus Status
    {
        get
        {
            if (!_options.EnablePerformanceMonitoring)
            {
                return PerformanceStatus.NotMonitored;
            }

            var elapsedMs = ElapsedMilliseconds;

            if (elapsedMs <= _options.TargetGenerationTimeMs)
            {
                return PerformanceStatus.Target;
            }

            if (elapsedMs <= _options.DegradedThresholdMs)
            {
                return PerformanceStatus.Degraded;
            }

            return PerformanceStatus.Failure;
        }
    }

    /// <summary>
    /// Stops monitoring and emits performance diagnostics.
    /// </summary>
    public void Stop()
    {
        if (_disposed)
        {
            return;
        }

        _stopwatch.Stop();

        if (!_options.EnablePerformanceMonitoring)
        {
            return;
        }

        var status = Status;
        var elapsedMs = ElapsedMilliseconds;

        var context = new
        {
            Operation = _operationName,
            ElapsedMs = elapsedMs,
            TargetMs = _options.TargetGenerationTimeMs,
            DegradedThresholdMs = _options.DegradedThresholdMs,
            Status = status.ToString()
        };

        // Emit appropriate diagnostic based on status
        switch (status)
        {
            case PerformanceStatus.Target:
                _logger.LogInfo(
                    DiagnosticCodes.PerformanceTargetMet,
                    $"Performance target met for '{_operationName}': {elapsedMs}ms <= {_options.TargetGenerationTimeMs}ms target",
                    context);
                break;

            case PerformanceStatus.Degraded:
                _logger.LogWarning(
                    DiagnosticCodes.PerformanceDegraded,
                    $"Performance degraded for '{_operationName}': {elapsedMs}ms > {_options.TargetGenerationTimeMs}ms target",
                    context);
                break;

            case PerformanceStatus.Failure:
                _logger.LogError(
                    DiagnosticCodes.PerformanceFailure,
                    $"Performance failure for '{_operationName}': {elapsedMs}ms > {_options.DegradedThresholdMs}ms threshold",
                    context);
                break;

            case PerformanceStatus.NotMonitored:
                // No diagnostic needed
                break;
        }

        // Always emit metrics for tracking
        _logger.LogInfo(
            DiagnosticCodes.PerformanceMetrics,
            $"Performance metrics collected for '{_operationName}'",
            context);
    }

    /// <summary>
    /// Disposes the performance monitor and stops monitoring.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _disposed = true;
    }
}

/// <summary>
/// Represents the performance status of an operation.
/// </summary>
public enum PerformanceStatus
{
    /// <summary>
    /// Performance monitoring is not enabled.
    /// </summary>
    NotMonitored,

    /// <summary>
    /// Performance met the target threshold (AC 297: &lt;500ms).
    /// </summary>
    Target,

    /// <summary>
    /// Performance is degraded but acceptable (AC 298: 500-2000ms).
    /// </summary>
    Degraded,

    /// <summary>
    /// Performance failure (AC 299: &gt;2000ms).
    /// </summary>
    Failure
}
