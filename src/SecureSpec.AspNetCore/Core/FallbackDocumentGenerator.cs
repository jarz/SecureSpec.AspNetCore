using Microsoft.OpenApi.Models;

namespace SecureSpec.AspNetCore.Core;

/// <summary>
/// Generates minimal fallback OpenAPI documents when resource limits are exceeded.
/// </summary>
public static class FallbackDocumentGenerator
{
    /// <summary>
    /// Generates a minimal fallback document with basic info and a warning banner.
    /// No paths, operations, or security arrays are included to prevent leaking stale data.
    /// </summary>
    /// <param name="title">The title for the document.</param>
    /// <param name="version">The version for the document.</param>
    /// <param name="reason">The reason for generating the fallback document.</param>
    /// <returns>A minimal OpenAPI document.</returns>
    public static OpenApiDocument GenerateFallback(string title, string version, string reason)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(reason);

        var sanitizedReason = SanitizeReason(reason);

        return new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = title,
                Version = version,
                Description = "⚠️ **Document Generation Failed**\n\n" +
                             $"A fallback document has been generated because: {sanitizedReason}\n\n" +
                             "The full API specification could not be generated within the configured resource limits. " +
                             "Please contact your administrator to review the configuration or optimize the API structure."
            },
            Paths = new OpenApiPaths(), // Empty paths - no operations leaked
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema>() // Empty schemas
            }
            // No security schemes or requirements - clean fallback
        };
    }

    /// <summary>
    /// Sanitizes the reason string to prevent injection attacks and limit length.
    /// </summary>
    private static string SanitizeReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return "Unknown resource limit exceeded";
        }

        // Remove CRLF and other control characters to prevent injection
        var sanitized = reason
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal)
            .Replace("\t", " ", StringComparison.Ordinal);

        // Limit length to prevent DoS via extremely long messages
        const int maxLength = 200;
        if (sanitized.Length > maxLength)
        {
            sanitized = string.Concat(sanitized.AsSpan(0, maxLength), "...");
        }

        return sanitized;
    }
}
