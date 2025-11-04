using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Diagnostics;

namespace SecureSpec.AspNetCore.Core;

/// <summary>
/// Generates OpenAPI documents with resource guard protection.
/// </summary>
public class DocumentGenerator
{
    private readonly SecureSpecOptions _options;
    private readonly DiagnosticsLogger _logger;
    private readonly IResourceGuardFactory _guardFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentGenerator"/> class.
    /// </summary>
    /// <param name="options">Configuration options.</param>
    /// <param name="logger">Diagnostics logger.</param>
    /// <param name="guardFactory">Factory for creating resource guards. If null, creates a default factory.</param>
    public DocumentGenerator(
        SecureSpecOptions options,
        DiagnosticsLogger logger,
        IResourceGuardFactory? guardFactory = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _logger = logger;
        _guardFactory = guardFactory ?? new ResourceGuardFactory(options.Performance, logger);
    }

    /// <summary>
    /// Generates an OpenAPI document with resource guard protection and performance monitoring.
    /// If resource limits are exceeded, a fallback document is generated instead.
    /// </summary>
    /// <param name="documentName">The name of the document to generate.</param>
    /// <param name="generationFunc">The function that generates the full document.</param>
    /// <returns>The generated OpenAPI document, or a fallback document if limits were exceeded.</returns>
    public OpenApiDocument GenerateWithGuards(
        string documentName,
        Func<OpenApiDocument> generationFunc)
    {
        ArgumentNullException.ThrowIfNull(documentName);
        ArgumentNullException.ThrowIfNull(generationFunc);

        // Start performance monitoring
        using var perfMonitor = _options.Performance.EnablePerformanceMonitoring
            ? new PerformanceMonitor(_options.Performance, _logger, $"Document generation: {documentName}")
            : null;

        // If resource guards are disabled, just generate normally
        if (!_options.Performance.EnableResourceGuards)
        {
            return generationFunc();
        }

        using var guard = _guardFactory.Create();

        try
        {
            // Attempt to generate the document
            var document = generationFunc();

            // Check if limits were exceeded after generation
            if (guard.IsLimitExceeded(out var reason))
            {
                _logger.LogWarning(DiagnosticCodes.Performance.ResourceLimitExceeded, $"Document '{documentName}' exceeded resource limits during generation", new
                {
                    DocumentName = documentName,
                    Reason = reason,
                    ElapsedMs = guard.ElapsedMilliseconds,
                    MemoryBytes = guard.MemoryUsageBytes
                });

                // Return fallback document instead of the full one
                return CreateFallbackDocument(documentName, reason!);
            }

            return document;
        }
        catch (ResourceLimitExceededException ex)
        {
            // Limit exceeded during generation - return fallback
            _logger.LogWarning(DiagnosticCodes.Performance.ResourceLimitExceeded, $"Document '{documentName}' generation aborted due to resource limits", new
            {
                DocumentName = documentName,
                Reason = ex.Message,
                ElapsedMs = guard.ElapsedMilliseconds,
                MemoryBytes = guard.MemoryUsageBytes
            });

            return CreateFallbackDocument(documentName, ex.Message);
        }
#pragma warning disable CA1031 // Do not catch general exception types - intentional for fallback behavior
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            // Other generation errors - also return fallback
            _logger.LogWarning(DiagnosticCodes.Performance.ResourceLimitExceeded, $"Document '{documentName}' generation failed", new
            {
                DocumentName = documentName,
                Error = ex.Message,
                ExceptionType = ex.GetType().Name
            });

            return CreateFallbackDocument(documentName, $"Generation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a fallback document for the specified document name.
    /// </summary>
    private static OpenApiDocument CreateFallbackDocument(string documentName, string reason)
    {
        // Try to get document info from configuration, or use defaults
        var title = documentName;
        const string version = "1.0";

        // Note: In a real implementation, we would try to extract title/version
        // from the document configuration if available. For now, use safe defaults.

        return FallbackDocumentGenerator.GenerateFallback(title, version, reason);
    }
}
