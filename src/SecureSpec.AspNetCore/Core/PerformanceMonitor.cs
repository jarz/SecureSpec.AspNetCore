using System.Diagnostics;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Diagnostics;

namespace SecureSpec.AspNetCore.Core;

/// <summary>
/// Monitors document generation performance and emits diagnostics based on configured thresholds.
/// Tracks performance against targets defined in AC 297-300.
/// </summary>
public sealed class PerformanceMonitor : IDisposable
{
    private readonly PerformanceOptions _options;
    private readonly DiagnosticsLogger _logger;
    private readonly Stopwatch _stopwatch;
    private readonly string _operationName;
    private readonly int _operationCount;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceMonitor"/> class.
    /// </summary>
    /// <param name="options">Performance configuration options.</param>
    /// <param name="logger">Diagnostics logger for emitting performance events.</param>
    /// <param name="operationName">Name of the operation being monitored.</param>
    /// <param name="operationCount">Number of operations being performed (default 1000 for baseline).</param>
    public PerformanceMonitor(
        PerformanceOptions options,
        DiagnosticsLogger logger,
        string operationName,
        int operationCount = 1000)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(operationName);

        if (operationCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(operationCount), "Operation count must be positive");
        }

        _options = options;
        _logger = logger;
        _operationName = operationName;
        _operationCount = operationCount;
        _stopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Gets the elapsed time in milliseconds since monitoring started.
    /// </summary>
    public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;

    /// <summary>
    /// Gets the current performance status based on elapsed time and thresholds.
    /// </summary>
    public PerformanceStatus GetStatus()
    {
        if (!_options.EnablePerformanceMonitoring)
        {
            return PerformanceStatus.NotMonitored;
        }

        var elapsedMs = ElapsedMilliseconds;

        // Normalize to baseline operation count (typically 1000 operations)
        var normalizedMs = NormalizeToBaseline(elapsedMs);

        if (normalizedMs <= _options.TargetGenerationTimeMs)
        {
            return PerformanceStatus.Target;
        }

        if (normalizedMs <= _options.DegradedThresholdMs)
        {
            return PerformanceStatus.Degraded;
        }

        return PerformanceStatus.Failure;
    }

    /// <summary>
    /// Normalizes the elapsed time to the baseline operation count.
    /// </summary>
    private long NormalizeToBaseline(long elapsedMs)
    {
        if (_operationCount == _options.BaselineOperationCount)
        {
            return elapsedMs;
        }

        // Scale to baseline (e.g., if we did 500 ops in 250ms, that's 500ms per 1000 ops)
        return (long)((double)elapsedMs / _operationCount * _options.BaselineOperationCount);
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

        var status = GetStatus();
        var elapsedMs = ElapsedMilliseconds;
        var normalizedMs = NormalizeToBaseline(elapsedMs);

        var context = new
        {
            Operation = _operationName,
            OperationCount = _operationCount,
            ElapsedMs = elapsedMs,
            NormalizedMs = normalizedMs,
            BaselineOperations = _options.BaselineOperationCount,
            TargetMs = _options.TargetGenerationTimeMs,
            DegradedThresholdMs = _options.DegradedThresholdMs,
            FailureThresholdMs = _options.FailureThresholdMs,
            Status = status.ToString()
        };

        // Emit appropriate diagnostic based on status
        switch (status)
        {
            case PerformanceStatus.Target:
                _logger.LogInfo(
                    DiagnosticCodes.PerformanceTargetMet,
                    $"Performance target met for '{_operationName}': {normalizedMs}ms (normalized) <= {_options.TargetGenerationTimeMs}ms target",
                    context);
                break;

            case PerformanceStatus.Degraded:
                _logger.LogWarning(
                    DiagnosticCodes.PerformanceDegraded,
                    $"Performance degraded for '{_operationName}': {normalizedMs}ms (normalized) > {_options.TargetGenerationTimeMs}ms target",
                    context);
                break;

            case PerformanceStatus.Failure:
                _logger.LogError(
                    DiagnosticCodes.PerformanceFailure,
                    $"Performance failure for '{_operationName}': {normalizedMs}ms (normalized) > {_options.DegradedThresholdMs}ms threshold",
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
    /// Performance met the target threshold (AC 297: &lt;500ms for 1000 operations).
    /// </summary>
    Target,

    /// <summary>
    /// Performance is degraded but acceptable (500-2000ms for 1000 operations).
    /// </summary>
    Degraded,

    /// <summary>
    /// Performance failure (&gt;2000ms for 1000 operations).
    /// </summary>
    Failure
}
