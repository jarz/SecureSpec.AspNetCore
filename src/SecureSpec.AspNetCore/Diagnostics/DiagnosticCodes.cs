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

    // ============================================
    // Security Codes (SEC)
    // ============================================

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

    // ============================================
    // CSP Codes (CSP)
    // ============================================

    /// <summary>
    /// CSP mismatch or missing directives.
    /// Severity: Error
    /// Action: Review policy
    /// </summary>
    public const string CspMismatch = "CSP001";

    // ============================================
    // Schema Codes (SCH)
    // ============================================

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

    // ============================================
    // Annotation Codes (ANN)
    // ============================================

    /// <summary>
    /// DataAnnotations conflict (last wins).
    /// Severity: Warn
    /// Action: Harmonize constraints
    /// </summary>
    public const string DataAnnotationsConflict = "ANN001";

    // ============================================
    // Rate Limit Codes (LIM)
    // ============================================

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

    // ============================================
    // Type Mapping Codes (MAP)
    // ============================================

    /// <summary>
    /// MapType override applied.
    /// Severity: Info
    /// Action: Validate mapping correctness
    /// </summary>
    public const string MapTypeOverride = "MAP001";

    // ============================================
    // Nullability Codes (NRT)
    // ============================================

    /// <summary>
    /// Nullability mismatch.
    /// Severity: Error
    /// Action: Adjust NRT config
    /// </summary>
    public const string NullabilityMismatch = "NRT001";

    // ============================================
    // Performance Codes (PERF)
    // ============================================

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

    // ============================================
    // Example Generation Codes (EXM)
    // ============================================

    /// <summary>
    /// Example generation throttled.
    /// Severity: Warn
    /// Action: Provide explicit example
    /// </summary>
    public const string ExampleGenerationThrottled = "EXM001";

    // ============================================
    // Virtualization Codes (VIRT)
    // ============================================

    /// <summary>
    /// Virtualization threshold triggered.
    /// Severity: Info
    /// Action: Performance expectation
    /// </summary>
    public const string VirtualizationThresholdTriggered = "VIRT001";

    // ============================================
    // Retention Codes (RET)
    // ============================================

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

    // ============================================
    // Policy Codes (POL)
    // ============================================

    /// <summary>
    /// PolicyToScope mapping applied.
    /// Severity: Info
    /// Action: Validate scopes
    /// </summary>
    public const string PolicyToScopeMapping = "POL001";

    // ============================================
    // Configuration Codes (CFG)
    // ============================================

    /// <summary>
    /// Invalid per-doc route template attempt.
    /// Severity: Info
    /// Action: Use global template
    /// </summary>
    public const string InvalidPerDocRouteTemplate = "CFG001";

    // ============================================
    // Sanitization Codes (SAN)
    // ============================================

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

    // ============================================
    // Boundary Codes (BND)
    // ============================================

    /// <summary>
    /// Multipart field count limit exceeded.
    /// Severity: Warn
    /// Action: Review field count limits
    /// </summary>
    public const string MultipartFieldCountExceeded = "BND001";

    // ============================================
    // Link Codes (LNK)
    // ============================================

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

    // ============================================
    // Callback Codes (CBK)
    // ============================================

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

    /// <summary>
    /// Gets metadata for a diagnostic code.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <returns>Metadata for the code, or null if not found.</returns>
    public static DiagnosticCodeMetadata? GetMetadata(string code)
    {
        return code switch
        {
            EndpointsDiscovered => new("Endpoints discovered successfully", DiagnosticLevel.Info, "None"),
            MetadataExtractionFailed => new("Metadata extraction failed for endpoint", DiagnosticLevel.Error, "Review endpoint configuration"),
            EndpointFiltered => new("Endpoint filtered (excluded/included)", DiagnosticLevel.Info, "Review filtering configuration"),
            FilterExecutionCompleted => new("Filter execution completed", DiagnosticLevel.Info, "None"),
            IntegrityCheckFailed => new("Integrity check failed", DiagnosticLevel.Critical, "Abort load"),
            SecurityRequirementsMutated => new("Operation security requirements mutated", DiagnosticLevel.Info, "Review security configuration"),
            CspMismatch => new("CSP mismatch or missing directives", DiagnosticLevel.Error, "Review policy"),
            SchemaIdCollision => new("SchemaId collision suffix applied", DiagnosticLevel.Info, "Confirm stability"),
            SchemaDepthExceeded => new("Schema generation exceeded maximum depth", DiagnosticLevel.Warn, "Review schema structure"),
            DataAnnotationsConflict => new("DataAnnotations conflict (last wins)", DiagnosticLevel.Warn, "Harmonize constraints"),
            RateLimitEnforced => new("Rate limit bucket enforced", DiagnosticLevel.Info, "Evaluate thresholds"),
            RateLimitResetAnomaly => new("Rate limit reset anomaly", DiagnosticLevel.Warn, "Check time source"),
            MapTypeOverride => new("MapType override applied", DiagnosticLevel.Info, "Validate mapping correctness"),
            NullabilityMismatch => new("Nullability mismatch", DiagnosticLevel.Error, "Adjust NRT config"),
            ResourceLimitExceeded => new("Resource limit exceeded", DiagnosticLevel.Warn, "Review resource configuration"),
            PerformanceTargetMet => new("Performance target met", DiagnosticLevel.Info, "None"),
            PerformanceDegraded => new("Performance degraded", DiagnosticLevel.Warn, "Review performance optimizations"),
            PerformanceFailure => new("Performance failure", DiagnosticLevel.Error, "Immediate optimization required"),
            PerformanceMetrics => new("Performance monitoring metrics collected", DiagnosticLevel.Info, "Review performance trends"),
            ExampleGenerationThrottled => new("Example generation throttled", DiagnosticLevel.Warn, "Provide explicit example"),
            VirtualizationThresholdTriggered => new("Virtualization threshold triggered", DiagnosticLevel.Info, "Performance expectation"),
            RetentionSizePurge => new("Retention size purge executed", DiagnosticLevel.Info, "Monitor volume"),
            RetentionAgePurge => new("Retention age purge executed", DiagnosticLevel.Info, "Confirm retentionDays"),
            PolicyToScopeMapping => new("PolicyToScope mapping applied", DiagnosticLevel.Info, "Validate scopes"),
            InvalidPerDocRouteTemplate => new("Invalid per-doc route template attempt", DiagnosticLevel.Info, "Use global template"),
            DisallowedHeadInjection => new("Disallowed head injection", DiagnosticLevel.Warn, "Restrict to meta/link"),
            DisallowedHeadInjectionAttempt => new("Disallowed head injection attempt", DiagnosticLevel.Warn, "Use local meta/link"),
            MultipartFieldCountExceeded => new("Multipart field count limit exceeded", DiagnosticLevel.Warn, "Review field count limits"),
            CircularLinkDetected => new("Circular link detection", DiagnosticLevel.Warn, "Review link structure"),
            LinkOperationRefFallback => new("Missing operationId but operationRef present", DiagnosticLevel.Info, "Using operationRef fallback"),
            LinkMissingReference => new("Missing both operationId and operationRef", DiagnosticLevel.Warn, "Provide operationId or operationRef"),
            LinkBrokenReference => new("Broken $ref in link", DiagnosticLevel.Error, "Fix reference path"),
            LinkExternalReference => new("External or unsupported reference in link", DiagnosticLevel.Warn, "Use internal references only"),
            CallbackReadOnly => new("Callback section rendered read-only", DiagnosticLevel.Info, "Callbacks do not support Try It Out"),
            CallbackBrokenReference => new("Broken $ref in callback", DiagnosticLevel.Error, "Fix reference path"),
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
            EndpointsDiscovered,
            MetadataExtractionFailed,
            EndpointFiltered,
            FilterExecutionCompleted,
            IntegrityCheckFailed,
            SecurityRequirementsMutated,
            CspMismatch,
            SchemaIdCollision,
            SchemaDepthExceeded,
            DataAnnotationsConflict,
            RateLimitEnforced,
            RateLimitResetAnomaly,
            MapTypeOverride,
            NullabilityMismatch,
            ResourceLimitExceeded,
            PerformanceTargetMet,
            PerformanceDegraded,
            PerformanceFailure,
            PerformanceMetrics,
            ExampleGenerationThrottled,
            VirtualizationThresholdTriggered,
            RetentionSizePurge,
            RetentionAgePurge,
            PolicyToScopeMapping,
            InvalidPerDocRouteTemplate,
            DisallowedHeadInjection,
            DisallowedHeadInjectionAttempt,
            MultipartFieldCountExceeded,
            CircularLinkDetected,
            LinkOperationRefFallback,
            LinkMissingReference,
            LinkBrokenReference,
            LinkExternalReference,
            CallbackReadOnly,
            CallbackBrokenReference
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
