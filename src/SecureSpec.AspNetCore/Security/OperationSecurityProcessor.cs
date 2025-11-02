using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Diagnostics;

namespace SecureSpec.AspNetCore.Security;

/// <summary>
/// Processes and applies security requirements to OpenAPI operations with deterministic ordering.
/// </summary>
/// <remarks>
/// <para>
/// This processor implements the following behavior as per OpenAPI specification:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <strong>AC 464</strong>: When an operation has security requirements defined,
/// they completely override the global security requirements (no merge).
/// </description>
/// </item>
/// <item>
/// <description>
/// <strong>AC 465</strong>: An empty security array at the operation level
/// explicitly clears all global security requirements, making the endpoint public.
/// </description>
/// </item>
/// <item>
/// <description>
/// <strong>AC 466</strong>: Security schemes within each requirement are ordered
/// lexically by their scheme key for deterministic output.
/// </description>
/// </item>
/// <item>
/// <description>
/// <strong>AC 467</strong>: Multiple security requirement objects preserve their
/// declaration order (OR semantics), only schemes within each object are ordered.
/// </description>
/// </item>
/// <item>
/// <description>
/// <strong>AC 468</strong>: When operation security overrides global security,
/// a diagnostic log (SEC002) is emitted for traceability.
/// </description>
/// </item>
/// </list>
/// </remarks>
public class OperationSecurityProcessor
{
    private readonly DiagnosticsLogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationSecurityProcessor"/> class.
    /// </summary>
    /// <param name="logger">The diagnostics logger for mutation tracking.</param>
    public OperationSecurityProcessor(DiagnosticsLogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Applies security requirements to an operation, handling overrides and ordering.
    /// </summary>
    /// <param name="operation">The OpenAPI operation to process.</param>
    /// <param name="globalSecurity">The global security requirements from the document.</param>
    /// <param name="operationId">The operation identifier for diagnostic logging.</param>
    /// <remarks>
    /// <para>
    /// This method implements the complete security override logic:
    /// </para>
    /// <list type="number">
    /// <item>If operation.Security is null, apply global security with ordering.</item>
    /// <item>If operation.Security is empty, leave it empty (public endpoint).</item>
    /// <item>If operation.Security has items, use them and log mutation.</item>
    /// <item>In all cases, apply deterministic ordering to the security requirements.</item>
    /// </list>
    /// </remarks>
    public void ApplySecurityRequirements(
        OpenApiOperation operation,
        IList<OpenApiSecurityRequirement>? globalSecurity,
        string operationId)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);

        if (operation.Security == null)
        {
            ApplyInheritedSecurity(operation, globalSecurity);
        }
        else if (operation.Security.Count == 0)
        {
            HandleEmptySecurityArray(globalSecurity, operationId);
        }
        else
        {
            ApplyOperationDefinedSecurity(operation, globalSecurity, operationId);
        }
    }

    /// <summary>
    /// Applies inherited security from global requirements when operation has no security defined.
    /// </summary>
    private static void ApplyInheritedSecurity(
        OpenApiOperation operation,
        IList<OpenApiSecurityRequirement>? globalSecurity)
    {
        operation.Security = (globalSecurity != null && globalSecurity.Count > 0)
            ? OrderSecurityRequirements(globalSecurity).ToList()
            : new List<OpenApiSecurityRequirement>();
    }

    /// <summary>
    /// Handles empty security array (public endpoint) and logs mutation if needed.
    /// AC 465: Empty array clears global requirements.
    /// </summary>
    private void HandleEmptySecurityArray(
        IList<OpenApiSecurityRequirement>? globalSecurity,
        string operationId)
    {
        if (globalSecurity != null && globalSecurity.Count > 0)
        {
            LogSecurityMutation(operationId, globalSecurity.Count, 0, "EmptyArray",
                $"Operation '{operationId}' cleared global security requirements (empty array)");
        }
    }

    /// <summary>
    /// Applies operation-defined security requirements with ordering and logs mutation.
    /// AC 464: Operation-level security overrides global (no merge).
    /// AC 466: Order schemes within each requirement lexically.
    /// AC 467: Preserve declaration order of requirement objects.
    /// </summary>
    private void ApplyOperationDefinedSecurity(
        OpenApiOperation operation,
        IList<OpenApiSecurityRequirement>? globalSecurity,
        string operationId)
    {
        operation.Security = OrderSecurityRequirements(operation.Security).ToList();

        if (globalSecurity?.Count > 0)
        {
            LogSecurityMutation(operationId, globalSecurity.Count, operation.Security.Count, "OperationDefined",
                $"Operation '{operationId}' overrode global security requirements");
        }
    }

    /// <summary>
    /// Logs security requirement mutation diagnostic.
    /// AC 468: Operation security mutation logged.
    /// </summary>
    private void LogSecurityMutation(
        string operationId,
        int globalCount,
        int operationCount,
        string overrideType,
        string message)
    {
        _logger.LogInfo(
            DiagnosticCodes.SecurityRequirementsMutated,
            message,
            new
            {
                OperationId = operationId,
                GlobalRequirementsCount = globalCount,
                OperationRequirementsCount = operationCount,
                OverrideType = overrideType
            });
    }

    /// <summary>
    /// Orders the security schemes within a single security requirement lexically by scheme key.
    /// </summary>
    /// <param name="requirement">The security requirement to order.</param>
    /// <returns>A new security requirement with schemes ordered lexically.</returns>
    /// <remarks>
    /// <para>
    /// AC 466: Within a single requirement object (AND logic), schemes are ordered
    /// lexically by their reference ID for deterministic output. The declaration order
    /// of multiple requirement objects (OR logic) is preserved by the caller.
    /// </para>
    /// </remarks>
    private static OpenApiSecurityRequirement OrderSecurityRequirement(OpenApiSecurityRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(requirement);

        // Order schemes lexically by their reference ID
        var orderedSchemes = requirement
            .OrderBy(kvp => kvp.Key.Reference?.Id ?? string.Empty, StringComparer.Ordinal)
            .ToList();

        var orderedRequirement = new OpenApiSecurityRequirement();
        foreach (var kvp in orderedSchemes)
        {
            orderedRequirement[kvp.Key] = kvp.Value;
        }

        return orderedRequirement;
    }

    /// <summary>
    /// Orders a list of security requirements, maintaining declaration order but ordering schemes within each.
    /// </summary>
    /// <param name="requirements">The security requirements to order.</param>
    /// <returns>A new list with schemes ordered within each requirement.</returns>
    /// <remarks>
    /// <para>
    /// This method is a convenience helper that can be used to order global security requirements.
    /// It preserves the declaration order of requirement objects (OR logic) while ordering
    /// the schemes within each requirement (AND logic) lexically.
    /// </para>
    /// </remarks>
    public static IList<OpenApiSecurityRequirement> OrderSecurityRequirements(
        IList<OpenApiSecurityRequirement>? requirements)
    {
        if (requirements == null || requirements.Count == 0)
        {
            return new List<OpenApiSecurityRequirement>();
        }

        var ordered = new List<OpenApiSecurityRequirement>();
        foreach (var requirement in requirements)
        {
            ordered.Add(OrderSecurityRequirement(requirement));
        }

        return ordered;
    }
}
