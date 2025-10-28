using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Diagnostics;

namespace SecureSpec.AspNetCore.Core;

/// <summary>
/// Factory for creating ResourceGuard instances with proper dependency injection support.
/// </summary>
public interface IResourceGuardFactory
{
    /// <summary>
    /// Creates a new ResourceGuard instance.
    /// </summary>
    /// <returns>A new ResourceGuard instance configured with the current options.</returns>
    ResourceGuard Create();
}

/// <summary>
/// Default implementation of IResourceGuardFactory.
/// </summary>
public class ResourceGuardFactory : IResourceGuardFactory
{
    private readonly PerformanceOptions _options;
    private readonly DiagnosticsLogger _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceGuardFactory"/> class.
    /// </summary>
    /// <param name="options">Performance configuration options.</param>
    /// <param name="logger">Diagnostics logger.</param>
    /// <param name="timeProvider">Time provider for getting current time. If null, uses TimeProvider.System.</param>
    public ResourceGuardFactory(PerformanceOptions options, DiagnosticsLogger logger, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Creates a new ResourceGuard instance.
    /// </summary>
    /// <returns>A new ResourceGuard instance configured with the current options.</returns>
    public ResourceGuard Create()
    {
        return new ResourceGuard(_options, _logger, _timeProvider);
    }
}
