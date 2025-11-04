namespace SecureSpec.AspNetCore.Diagnostics;

/// <summary>
/// Defines all diagnostic error codes used throughout SecureSpec.
/// Each code has a specific meaning, severity level, and recommended action.
/// </summary>
public static class DiagnosticCodes
{
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
        /// Severity: Info
        /// Action: None
        /// </summary>
        public const string EndpointsDiscovered = "DISC001";

        /// <summary>
        /// Metadata extraction failed for endpoint.
        /// Severity: Error
        /// Action: Review endpoint configuration
        /// </summary>
        public const string MetadataExtractionFailed = "DISC002";

        /// <summary>
        /// Endpoint filtered (excluded/included).
        /// Severity: Info
        /// Action: Review filtering configuration
        /// </summary>
        public const string EndpointFiltered = "DISC003";

        /// <summary>
        /// Filter execution completed.
        /// Severity: Info
        /// Action: None
        /// </summary>
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
        /// Severity: Critical
        /// Action: Abort load
        /// </summary>
        public const string IntegrityCheckFailed = "SEC001";

        /// <summary>
        /// Operation security requirements mutated (overridden from global).
        /// Severity: Info
        /// Action: Review security configuration
        /// </summary>
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
        /// Severity: Error
        /// Action: Review policy
        /// </summary>
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
        /// Severity: Info
        /// Action: Confirm stability
        /// </summary>
        public const string SchemaIdCollision = "SCH001";

        /// <summary>
        /// Schema generation exceeded maximum depth.
        /// Severity: Warn
        /// Action: Review schema structure
        /// </summary>
        public const string SchemaDepthExceeded = "SCH001-DEPTH";
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
        /// Severity: Warn
        /// Action: Harmonize constraints
        /// </summary>
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
        /// Severity: Info
        /// Action: Evaluate thresholds
        /// </summary>
        public const string RateLimitEnforced = "LIM001";

        /// <summary>
        /// Rate limit reset anomaly.
        /// Severity: Warn
        /// Action: Check time source
        /// </summary>
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
        /// Severity: Info
        /// Action: Validate mapping correctness
        /// </summary>
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
        /// Severity: Error
        /// Action: Adjust NRT config
        /// </summary>
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
        /// Severity: Warn
        /// Action: Review resource configuration
        /// </summary>
        public const string ResourceLimitExceeded = "PERF001";

        /// <summary>
        /// Performance target met (&lt;500ms for 1000 operations).
        /// Severity: Info
        /// Action: None
        /// </summary>
        public const string PerformanceTargetMet = "PERF002";

        /// <summary>
        /// Performance degraded (500-2000ms for 1000 operations).
        /// Severity: Warn
        /// Action: Review performance optimizations
        /// </summary>
        public const string PerformanceDegraded = "PERF003";

        /// <summary>
        /// Performance failure (&gt;2000ms for 1000 operations).
        /// Severity: Error
        /// Action: Immediate optimization required
        /// </summary>
        public const string PerformanceFailure = "PERF004";

        /// <summary>
        /// Performance monitoring metrics collected.
        /// Severity: Info
        /// Action: Review performance trends
        /// </summary>
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
        /// Severity: Warn
        /// Action: Provide explicit example
        /// </summary>
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
        /// Severity: Info
        /// Action: Performance expectation
        /// </summary>
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
        /// Severity: Info
        /// Action: Monitor volume
        /// </summary>
        public const string RetentionSizePurge = "RET001";

        /// <summary>
        /// Retention age purge executed.
        /// Severity: Info
        /// Action: Confirm retentionDays
        /// </summary>
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
        /// Severity: Info
        /// Action: Validate scopes
        /// </summary>
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
        /// Severity: Info
        /// Action: Use global template
        /// </summary>
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
        /// Severity: Warn
        /// Action: Restrict to meta/link
        /// </summary>
        public const string DisallowedHeadInjection = "SAN001";

        /// <summary>
        /// Disallowed head injection attempt.
        /// Severity: Warn
        /// Action: Use local meta/link
        /// </summary>
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
        /// Severity: Warn
        /// Action: Review field count limits
        /// </summary>
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
        /// Severity: Warn
        /// Action: Review link structure
        /// </summary>
        public const string CircularLinkDetected = "LNK001";

        /// <summary>
        /// Missing operationId but operationRef present.
        /// Severity: Info
        /// Action: Using operationRef fallback
        /// </summary>
        public const string LinkOperationRefFallback = "LNK002";

        /// <summary>
        /// Missing both operationId and operationRef.
        /// Severity: Warn
        /// Action: Provide operationId or operationRef
        /// </summary>
        public const string LinkMissingReference = "LNK003";

        /// <summary>
        /// Broken $ref in link.
        /// Severity: Error
        /// Action: Fix reference path
        /// </summary>
        public const string LinkBrokenReference = "LNK004";

        /// <summary>
        /// External or unsupported reference in link.
        /// Severity: Warn
        /// Action: Use internal references only
        /// </summary>
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
        /// Severity: Info
        /// Action: Callbacks do not support Try It Out
        /// </summary>
        public const string CallbackReadOnly = "CBK001";

        /// <summary>
        /// Broken $ref in callback.
        /// Severity: Error
        /// Action: Fix reference path
        /// </summary>
        public const string CallbackBrokenReference = "CBK002";
    }

    /// <summary>
    /// Gets metadata for a diagnostic code.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <returns>Metadata for the code, or null if not found.</returns>
    public static DiagnosticCodeMetadata? GetMetadata(string code)
    {
        return code switch
        {
            Discovery.EndpointsDiscovered => new("Endpoints discovered successfully", DiagnosticLevel.Info, "None"),
            Discovery.MetadataExtractionFailed => new("Metadata extraction failed for endpoint", DiagnosticLevel.Error, "Review endpoint configuration"),
            Discovery.EndpointFiltered => new("Endpoint filtered (excluded/included)", DiagnosticLevel.Info, "Review filtering configuration"),
            Discovery.FilterExecutionCompleted => new("Filter execution completed", DiagnosticLevel.Info, "None"),
            Security.IntegrityCheckFailed => new("Integrity check failed", DiagnosticLevel.Critical, "Abort load"),
            Security.SecurityRequirementsMutated => new("Operation security requirements mutated", DiagnosticLevel.Info, "Review security configuration"),
            Csp.CspMismatch => new("CSP mismatch or missing directives", DiagnosticLevel.Error, "Review policy"),
            Schema.SchemaIdCollision => new("SchemaId collision suffix applied", DiagnosticLevel.Info, "Confirm stability"),
            Schema.SchemaDepthExceeded => new("Schema generation exceeded maximum depth", DiagnosticLevel.Warn, "Review schema structure"),
            Annotations.DataAnnotationsConflict => new("DataAnnotations conflict (last wins)", DiagnosticLevel.Warn, "Harmonize constraints"),
            RateLimit.RateLimitEnforced => new("Rate limit bucket enforced", DiagnosticLevel.Info, "Evaluate thresholds"),
            RateLimit.RateLimitResetAnomaly => new("Rate limit reset anomaly", DiagnosticLevel.Warn, "Check time source"),
            TypeMapping.MapTypeOverride => new("MapType override applied", DiagnosticLevel.Info, "Validate mapping correctness"),
            Nullability.NullabilityMismatch => new("Nullability mismatch", DiagnosticLevel.Error, "Adjust NRT config"),
            Performance.ResourceLimitExceeded => new("Resource limit exceeded", DiagnosticLevel.Warn, "Review resource configuration"),
            Performance.PerformanceTargetMet => new("Performance target met", DiagnosticLevel.Info, "None"),
            Performance.PerformanceDegraded => new("Performance degraded", DiagnosticLevel.Warn, "Review performance optimizations"),
            Performance.PerformanceFailure => new("Performance failure", DiagnosticLevel.Error, "Immediate optimization required"),
            Performance.PerformanceMetrics => new("Performance monitoring metrics collected", DiagnosticLevel.Info, "Review performance trends"),
            ExampleGeneration.ExampleGenerationThrottled => new("Example generation throttled", DiagnosticLevel.Warn, "Provide explicit example"),
            Virtualization.VirtualizationThresholdTriggered => new("Virtualization threshold triggered", DiagnosticLevel.Info, "Performance expectation"),
            Retention.RetentionSizePurge => new("Retention size purge executed", DiagnosticLevel.Info, "Monitor volume"),
            Retention.RetentionAgePurge => new("Retention age purge executed", DiagnosticLevel.Info, "Confirm retentionDays"),
            Policy.PolicyToScopeMapping => new("PolicyToScope mapping applied", DiagnosticLevel.Info, "Validate scopes"),
            Configuration.InvalidPerDocRouteTemplate => new("Invalid per-doc route template attempt", DiagnosticLevel.Info, "Use global template"),
            Sanitization.DisallowedHeadInjection => new("Disallowed head injection", DiagnosticLevel.Warn, "Restrict to meta/link"),
            Sanitization.DisallowedHeadInjectionAttempt => new("Disallowed head injection attempt", DiagnosticLevel.Warn, "Use local meta/link"),
            Boundary.MultipartFieldCountExceeded => new("Multipart field count limit exceeded", DiagnosticLevel.Warn, "Review field count limits"),
            Link.CircularLinkDetected => new("Circular link detection", DiagnosticLevel.Warn, "Review link structure"),
            Link.LinkOperationRefFallback => new("Missing operationId but operationRef present", DiagnosticLevel.Info, "Using operationRef fallback"),
            Link.LinkMissingReference => new("Missing both operationId and operationRef", DiagnosticLevel.Warn, "Provide operationId or operationRef"),
            Link.LinkBrokenReference => new("Broken $ref in link", DiagnosticLevel.Error, "Fix reference path"),
            Link.LinkExternalReference => new("External or unsupported reference in link", DiagnosticLevel.Warn, "Use internal references only"),
            Callback.CallbackReadOnly => new("Callback section rendered read-only", DiagnosticLevel.Info, "Callbacks do not support Try It Out"),
            Callback.CallbackBrokenReference => new("Broken $ref in callback", DiagnosticLevel.Error, "Fix reference path"),
            _ => null
        };
    }

    /// <summary>
    /// Validates that a diagnostic code is recognized.
    /// </summary>
    /// <param name="code">The code to validate.</param>
    /// <returns>True if the code is recognized, false otherwise.</returns>
    public static bool IsValidCode(string code)
    {
        return GetMetadata(code) != null;
    }

    /// <summary>
    /// Gets all defined diagnostic codes.
    /// </summary>
    /// <returns>An array of all defined diagnostic codes.</returns>
    public static string[] GetAllCodes()
    {
        return new[]
        {
            Discovery.EndpointsDiscovered,
            Discovery.MetadataExtractionFailed,
            Discovery.EndpointFiltered,
            Discovery.FilterExecutionCompleted,
            Security.IntegrityCheckFailed,
            Security.SecurityRequirementsMutated,
            Csp.CspMismatch,
            Schema.SchemaIdCollision,
            Schema.SchemaDepthExceeded,
            Annotations.DataAnnotationsConflict,
            RateLimit.RateLimitEnforced,
            RateLimit.RateLimitResetAnomaly,
            TypeMapping.MapTypeOverride,
            Nullability.NullabilityMismatch,
            Performance.ResourceLimitExceeded,
            Performance.PerformanceTargetMet,
            Performance.PerformanceDegraded,
            Performance.PerformanceFailure,
            Performance.PerformanceMetrics,
            ExampleGeneration.ExampleGenerationThrottled,
            Virtualization.VirtualizationThresholdTriggered,
            Retention.RetentionSizePurge,
            Retention.RetentionAgePurge,
            Policy.PolicyToScopeMapping,
            Configuration.InvalidPerDocRouteTemplate,
            Sanitization.DisallowedHeadInjection,
            Sanitization.DisallowedHeadInjectionAttempt,
            Boundary.MultipartFieldCountExceeded,
            Link.CircularLinkDetected,
            Link.LinkOperationRefFallback,
            Link.LinkMissingReference,
            Link.LinkBrokenReference,
            Link.LinkExternalReference,
            Callback.CallbackReadOnly,
            Callback.CallbackBrokenReference
        };
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
