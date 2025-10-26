using System.Diagnostics;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Diagnostics;

namespace SecureSpec.AspNetCore.Core;

/// <summary>
/// Monitors resource usage (time and memory) during document generation and enforces limits.
/// </summary>
public sealed class ResourceGuard : IDisposable
{
    private readonly PerformanceOptions _options;
    private readonly DiagnosticsLogger _logger;
    private readonly Stopwatch _stopwatch;
    private readonly long _initialMemory;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceGuard"/> class.
    /// </summary>
    /// <param name="options">Performance configuration options.</param>
    /// <param name="logger">Diagnostics logger for emitting warnings.</param>
    public ResourceGuard(PerformanceOptions options, DiagnosticsLogger logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _logger = logger;
        _stopwatch = Stopwatch.StartNew();

        // Capture initial memory baseline
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        _initialMemory = GC.GetTotalMemory(forceFullCollection: false);
    }

    /// <summary>
    /// Gets the elapsed time in milliseconds since the guard was created.
    /// </summary>
    public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;

    /// <summary>
    /// Gets the estimated memory usage in bytes since the guard was created.
    /// </summary>
    public long MemoryUsageBytes
    {
        get
        {
            var currentMemory = GC.GetTotalMemory(forceFullCollection: false);
            return Math.Max(0, currentMemory - _initialMemory);
        }
    }

    /// <summary>
    /// Checks if any resource limits have been exceeded.
    /// </summary>
    /// <param name="reason">Output parameter that contains the reason if a limit was exceeded.</param>
    /// <returns>True if a limit was exceeded; otherwise, false.</returns>
    public bool IsLimitExceeded(out string? reason)
    {
        if (!_options.EnableResourceGuards)
        {
            reason = null;
            return false;
        }

        // Check time limit
        if (ElapsedMilliseconds > _options.MaxGenerationTimeMs)
        {
            reason = $"Generation time exceeded limit: {ElapsedMilliseconds}ms > {_options.MaxGenerationTimeMs}ms";
            _logger.LogWarning("PERF001", reason, new
            {
                ElapsedMs = ElapsedMilliseconds,
                LimitMs = _options.MaxGenerationTimeMs,
                ResourceType = "Time"
            });
            return true;
        }

        // Check memory limit
        var memoryUsage = MemoryUsageBytes;
        if (memoryUsage > _options.MaxMemoryBytes)
        {
            reason = $"Memory usage exceeded limit: {memoryUsage} bytes > {_options.MaxMemoryBytes} bytes";
            _logger.LogWarning("PERF001", reason, new
            {
                MemoryBytes = memoryUsage,
                LimitBytes = _options.MaxMemoryBytes,
                ResourceType = "Memory"
            });
            return true;
        }

        reason = null;
        return false;
    }

    /// <summary>
    /// Checks if any resource limits have been exceeded and throws an exception if so.
    /// </summary>
    /// <exception cref="ResourceLimitExceededException">Thrown when a resource limit is exceeded.</exception>
    public void CheckLimits()
    {
        if (IsLimitExceeded(out var reason))
        {
            throw new ResourceLimitExceededException(reason!);
        }
    }

    /// <summary>
    /// Disposes the resource guard and stops monitoring.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _stopwatch.Stop();
            _disposed = true;
        }
    }
}

/// <summary>
/// Exception thrown when a resource limit (time or memory) is exceeded during document generation.
/// </summary>
public class ResourceLimitExceededException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceLimitExceededException"/> class.
    /// </summary>
    public ResourceLimitExceededException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceLimitExceededException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public ResourceLimitExceededException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceLimitExceededException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ResourceLimitExceededException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
