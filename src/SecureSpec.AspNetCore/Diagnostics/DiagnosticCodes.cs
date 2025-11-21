using System.Reflection;

namespace SecureSpec.AspNetCore.Diagnostics;

/// <summary>
/// Attribute to define metadata for diagnostic codes.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DiagnosticCodeAttribute"/> class.
/// </remarks>
/// <param name="description">Human-readable description of the diagnostic.</param>
/// <param name="level">Severity level of the diagnostic.</param>
/// <param name="recommendedAction">Recommended action to address the diagnostic.</param>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class DiagnosticCodeAttribute(string description, DiagnosticLevel level, string recommendedAction) : Attribute
{
    /// <summary>
    /// Human-readable description of the diagnostic.
    /// </summary>
    public string Description { get; } = description;

    /// <summary>
    /// Severity level of the diagnostic.
    /// </summary>
    public DiagnosticLevel Level { get; } = level;

    /// <summary>
    /// Recommended action to address the diagnostic.
    /// </summary>
    public string RecommendedAction { get; } = recommendedAction;
}

/// <summary>
/// Defines all diagnostic error codes used throughout SecureSpec.
/// Each code has a specific meaning, severity level, and recommended action.
/// </summary>
public static class DiagnosticCodes
{
    private static readonly Lazy<Dictionary<string, DiagnosticCodeMetadata>> _metadataCache =
        new(BuildMetadataCache);
    // ============================================
    // Discovery Codes (DISC)
    // ============================================

    /// <summary>
    /// Discovery-specific diagnostic codes.
    /// </summary>
    internal static class Discovery
    {
        /// <summary>
        /// Endpoints discovered successfully.
        /// </summary>
        [DiagnosticCode("Endpoints discovered successfully", DiagnosticLevel.Info, "None")]
        public const string EndpointsDiscovered = "DISC001";

        /// <summary>
        /// Metadata extraction failed for endpoint.
        /// </summary>
        [DiagnosticCode("Metadata extraction failed for endpoint", DiagnosticLevel.Error, "Review endpoint configuration")]
        public const string MetadataExtractionFailed = "DISC002";

        /// <summary>
        /// Endpoint filtered (excluded/included).
        /// </summary>
        [DiagnosticCode("Endpoint filtered (excluded/included)", DiagnosticLevel.Info, "Review filtering configuration")]
        public const string EndpointFiltered = "DISC003";

        /// <summary>
        /// Filter execution completed.
        /// </summary>
        [DiagnosticCode("Filter execution completed", DiagnosticLevel.Info, "None")]
        public const string FilterExecutionCompleted = "DISC004";
    }

    // ============================================
    // Security Codes (SEC)
    // ============================================

    /// <summary>
    /// Security-specific diagnostic codes.
    /// </summary>
    internal static class Security
    {
        /// <summary>
        /// Integrity check failed.
        /// </summary>
        [DiagnosticCode("Integrity check failed", DiagnosticLevel.Critical, "Abort load")]
        public const string IntegrityCheckFailed = "SEC001";

        /// <summary>
        /// Operation security requirements mutated (overridden from global).
        /// </summary>
        [DiagnosticCode("Operation security requirements mutated", DiagnosticLevel.Info, "Review security configuration")]
        public const string SecurityRequirementsMutated = "SEC002";
    }

    // ============================================
    // CSP Codes (CSP)
    // ============================================

    /// <summary>
    /// Content Security Policy diagnostic codes.
    /// </summary>
    internal static class Csp
    {
        /// <summary>
        /// CSP mismatch or missing directives.
        /// </summary>
        [DiagnosticCode("CSP mismatch or missing directives", DiagnosticLevel.Error, "Review policy")]
        public const string CspMismatch = "CSP001";
    }

    // ============================================
    // Schema Codes (SCH)
    // ============================================

    /// <summary>
    /// Schema-specific diagnostic codes.
    /// </summary>
    internal static class Schema
    {
        /// <summary>
        /// SchemaId collision suffix applied.
        /// </summary>
        [DiagnosticCode("SchemaId collision suffix applied", DiagnosticLevel.Info, "Confirm stability")]
        public const string SchemaIdCollision = "SCH001";

        /// <summary>
        /// Schema generation exceeded maximum depth.
        /// </summary>
        [DiagnosticCode("Schema generation exceeded maximum depth", DiagnosticLevel.Warn, "Review schema structure")]
        public const string SchemaDepthExceeded = "SCH002";
    }

    // ============================================
    // Annotation Codes (ANN)
    // ============================================

    /// <summary>
    /// Data annotation diagnostic codes.
    /// </summary>
    internal static class Annotations
    {
        /// <summary>
        /// DataAnnotations conflict (last wins).
        /// </summary>
        [DiagnosticCode("DataAnnotations conflict (last wins)", DiagnosticLevel.Warn, "Harmonize constraints")]
        public const string DataAnnotationsConflict = "ANN001";
    }

    // ============================================
    // Rate Limit Codes (LIM)
    // ============================================

    /// <summary>
    /// Rate-limiting diagnostic codes.
    /// </summary>
    internal static class RateLimit
    {
        /// <summary>
        /// Rate limit bucket enforced.
        /// </summary>
        [DiagnosticCode("Rate limit bucket enforced", DiagnosticLevel.Info, "Evaluate thresholds")]
        public const string RateLimitEnforced = "LIM001";

        /// <summary>
        /// Rate limit reset anomaly.
        /// </summary>
        [DiagnosticCode("Rate limit reset anomaly", DiagnosticLevel.Warn, "Check time source")]
        public const string RateLimitResetAnomaly = "LIM002";
    }

    // ============================================
    // Type Mapping Codes (MAP)
    // ============================================

    /// <summary>
    /// Type mapping diagnostic codes.
    /// </summary>
    internal static class TypeMapping
    {
        /// <summary>
        /// MapType override applied.
        /// </summary>
        [DiagnosticCode("MapType override applied", DiagnosticLevel.Info, "Validate mapping correctness")]
        public const string MapTypeOverride = "MAP001";
    }

    // ============================================
    // Nullability Codes (NRT)
    // ============================================

    /// <summary>
    /// Nullability diagnostic codes.
    /// </summary>
    internal static class Nullability
    {
        /// <summary>
        /// Nullability mismatch.
        /// </summary>
        [DiagnosticCode("Nullability mismatch", DiagnosticLevel.Error, "Adjust NRT config")]
        public const string NullabilityMismatch = "NRT001";
    }

    // ============================================
    // Performance Codes (PERF)
    // ============================================

    /// <summary>
    /// Performance diagnostic codes.
    /// </summary>
    internal static class Performance
    {
        /// <summary>
        /// Resource limit exceeded (time or memory).
        /// </summary>
        [DiagnosticCode("Resource limit exceeded (time or memory)", DiagnosticLevel.Warn, "Review resource configuration")]
        public const string ResourceLimitExceeded = "PERF001";

        /// <summary>
        /// Performance target met (&lt;500ms for 1000 operations).
        /// </summary>
        [DiagnosticCode("Performance target met (<500ms for 1000 operations)", DiagnosticLevel.Info, "None")]
        public const string PerformanceTargetMet = "PERF002";

        /// <summary>
        /// Performance degraded (500-2000ms for 1000 operations).
        /// </summary>
        [DiagnosticCode("Performance degraded (500-2000ms for 1000 operations)", DiagnosticLevel.Warn, "Review performance optimizations")]
        public const string PerformanceDegraded = "PERF003";

        /// <summary>
        /// Performance failure (&gt;2000ms for 1000 operations).
        /// </summary>
        [DiagnosticCode("Performance failure (>2000ms for 1000 operations)", DiagnosticLevel.Error, "Immediate optimization required")]
        public const string PerformanceFailure = "PERF004";

        /// <summary>
        /// Performance monitoring metrics collected.
        /// </summary>
        [DiagnosticCode("Performance monitoring metrics collected", DiagnosticLevel.Info, "Review performance trends")]
        public const string PerformanceMetrics = "PERF005";
    }

    // ============================================
    // Example Generation Codes (EXM)
    // ============================================

    /// <summary>
    /// Example generation diagnostic codes.
    /// </summary>
    internal static class ExampleGeneration
    {
        /// <summary>
        /// Example generation throttled.
        /// </summary>
        [DiagnosticCode("Example generation throttled", DiagnosticLevel.Warn, "Provide explicit example")]
        public const string ExampleGenerationThrottled = "EXM001";
    }

    // ============================================
    // Virtualization Codes (VIRT)
    // ============================================

    /// <summary>
    /// Virtualization diagnostic codes.
    /// </summary>
    internal static class Virtualization
    {
        /// <summary>
        /// Virtualization threshold triggered.
        /// </summary>
        [DiagnosticCode("Virtualization threshold triggered", DiagnosticLevel.Info, "Performance expectation")]
        public const string VirtualizationThresholdTriggered = "VIRT001";
    }

    // ============================================
    // Retention Codes (RET)
    // ============================================

    /// <summary>
    /// Retention diagnostic codes.
    /// </summary>
    internal static class Retention
    {
        /// <summary>
        /// Retention size purge executed.
        /// </summary>
        [DiagnosticCode("Retention size purge executed", DiagnosticLevel.Info, "Monitor volume")]
        public const string RetentionSizePurge = "RET001";

        /// <summary>
        /// Retention age purge executed.
        /// </summary>
        [DiagnosticCode("Retention age purge executed", DiagnosticLevel.Info, "Confirm retentionDays")]
        public const string RetentionAgePurge = "RET002";
    }

    // ============================================
    // Policy Codes (POL)
    // ============================================

    /// <summary>
    /// Policy diagnostic codes.
    /// </summary>
    internal static class Policy
    {
        /// <summary>
        /// PolicyToScope mapping applied.
        /// </summary>
        [DiagnosticCode("PolicyToScope mapping applied", DiagnosticLevel.Info, "Validate scopes")]
        public const string PolicyToScopeMapping = "POL001";
    }

    // ============================================
    // Configuration Codes (CFG)
    // ============================================

    /// <summary>
    /// Configuration diagnostic codes.
    /// </summary>
    internal static class Configuration
    {
        /// <summary>
        /// Invalid per-doc route template attempt.
        /// </summary>
        [DiagnosticCode("Invalid per-doc route template attempt", DiagnosticLevel.Info, "Use global template")]
        public const string InvalidPerDocRouteTemplate = "CFG001";
    }

    // ============================================
    // Sanitization Codes (SAN)
    // ============================================

    /// <summary>
    /// Sanitization diagnostic codes.
    /// </summary>
    internal static class Sanitization
    {
        /// <summary>
        /// Disallowed head injection.
        /// </summary>
        [DiagnosticCode("Disallowed head injection", DiagnosticLevel.Warn, "Restrict to meta/link")]
        public const string DisallowedHeadInjection = "SAN001";

        /// <summary>
        /// Disallowed head injection attempt.
        /// </summary>
        [DiagnosticCode("Disallowed head injection attempt", DiagnosticLevel.Warn, "Use local meta/link")]
        public const string DisallowedHeadInjectionAttempt = "HD001";
    }

    // ============================================
    // Boundary Codes (BND)
    // ============================================

    /// <summary>
    /// Boundary diagnostic codes.
    /// </summary>
    internal static class Boundary
    {
        /// <summary>
        /// Multipart field count limit exceeded.
        /// </summary>
        [DiagnosticCode("Multipart field count limit exceeded", DiagnosticLevel.Warn, "Review field count limits")]
        public const string MultipartFieldCountExceeded = "BND001";
    }

    // ============================================
    // Link Codes (LNK)
    // ============================================

    /// <summary>
    /// Link diagnostic codes.
    /// </summary>
    internal static class Link
    {
        /// <summary>
        /// Circular link detection.
        /// </summary>
        [DiagnosticCode("Circular link detection", DiagnosticLevel.Warn, "Review link structure")]
        public const string CircularLinkDetected = "LNK001";

        /// <summary>
        /// Missing operationId but operationRef present.
        /// </summary>
        [DiagnosticCode("Missing operationId but operationRef present", DiagnosticLevel.Info, "Using operationRef fallback")]
        public const string LinkOperationRefFallback = "LNK002";

        /// <summary>
        /// Missing both operationId and operationRef.
        /// </summary>
        [DiagnosticCode("Missing both operationId and operationRef", DiagnosticLevel.Warn, "Provide operationId or operationRef")]
        public const string LinkMissingReference = "LNK003";

        /// <summary>
        /// Broken $ref in link.
        /// </summary>
        [DiagnosticCode("Broken $ref in link", DiagnosticLevel.Error, "Fix reference path")]
        public const string LinkBrokenReference = "LNK004";

        /// <summary>
        /// External or unsupported reference in link.
        /// </summary>
        [DiagnosticCode("External or unsupported reference in link", DiagnosticLevel.Warn, "Use internal references only")]
        public const string LinkExternalReference = "LNK005";
    }

    // ============================================
    // Callback Codes (CBK)
    // ============================================

    /// <summary>
    /// Callback diagnostic codes.
    /// </summary>
    internal static class Callback
    {
        /// <summary>
        /// Callback section rendered read-only.
        /// </summary>
        [DiagnosticCode("Callback section rendered read-only", DiagnosticLevel.Info, "Callbacks do not support Try It Out")]
        public const string CallbackReadOnly = "CBK001";

        /// <summary>
        /// Broken $ref in callback.
        /// </summary>
        [DiagnosticCode("Broken $ref in callback", DiagnosticLevel.Error, "Fix reference path")]
        public const string CallbackBrokenReference = "CBK002";
    }

    /// <summary>
    /// Gets metadata for a diagnostic code.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <returns>Metadata for the code, or null if not found.</returns>
    public static DiagnosticCodeMetadata? GetMetadata(string code)
    {
        return _metadataCache.Value.TryGetValue(code, out var metadata) ? metadata : null;
    }

    /// <summary>
    /// Validates that a diagnostic code is recognized.
    /// </summary>
    /// <param name="code">The code to validate.</param>
    /// <returns>True if the code is recognized, false otherwise.</returns>
    public static bool IsValidCode(string code)
    {
        return _metadataCache.Value.ContainsKey(code);
    }

    /// <summary>
    /// Gets all defined diagnostic codes.
    /// </summary>
    /// <returns>An array of all defined diagnostic codes.</returns>
    public static string[] GetAllCodes()
    {
        return _metadataCache.Value.Keys.ToArray();
    }

    private static Dictionary<string, DiagnosticCodeMetadata> BuildMetadataCache()
    {
        var cache = new Dictionary<string, DiagnosticCodeMetadata>();

        // Get all nested static classes within DiagnosticCodes
        var nestedTypes = typeof(DiagnosticCodes).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        foreach (var nestedType in nestedTypes)
        {
            // Get all const string fields in each nested class
            var fields = nestedType.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string));

            foreach (var kvp in fields
                .Select(field => new { field, attribute = field.GetCustomAttribute<DiagnosticCodeAttribute>() })
                .Where(x => x.attribute != null))
            {
                var code = (string)kvp.field.GetValue(null)!;
                cache[code] = new DiagnosticCodeMetadata(
                    kvp.attribute.Description,
                    kvp.attribute.Level,
                    kvp.attribute.RecommendedAction);
            }
        }

        return cache;
    }
}

/// <summary>
/// Metadata for a diagnostic code.
/// </summary>
/// <param name="Description">Human-readable description of the diagnostic.</param>
/// <param name="Level">Severity level of the diagnostic.</param>
/// <param name="RecommendedAction">Recommended action to address the diagnostic.</param>
public record DiagnosticCodeMetadata(
    string Description,
    DiagnosticLevel Level,
    string RecommendedAction);
