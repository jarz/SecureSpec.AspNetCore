using SecureSpec.AspNetCore.Diagnostics;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for the DiagnosticCodes class.
/// Ensures all diagnostic codes are defined and properly categorized.
/// AC 381-390: Diagnostic event structure with all error codes defined.
/// </summary>
public class DiagnosticCodesTests
{
    [Fact]
    public void AllDefinedCodes_AreValid()
    {
        // Arrange
        var allCodes = DiagnosticCodes.GetAllCodes();

        // Act & Assert
        foreach (var code in allCodes)
        {
            Assert.True(DiagnosticCodes.IsValidCode(code),
                $"Code {code} should be valid but IsValidCode returned false");
        }
    }

    [Fact]
    public void GetAllCodes_ReturnsNoDuplicates()
    {
        // Act
        var allCodes = DiagnosticCodes.GetAllCodes();

        // Assert - Verify no duplicate codes
        var uniqueCodes = allCodes.Distinct().ToArray();
        Assert.Equal(allCodes.Length, uniqueCodes.Length);
    }

    [Fact]
    public void IsValidCode_ReturnsFalse_ForUnknownCode()
    {
        // Act
        var result = DiagnosticCodes.IsValidCode("UNKNOWN001");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetMetadata_ReturnsNull_ForUnknownCode()
    {
        // Act
        var metadata = DiagnosticCodes.GetMetadata("UNKNOWN001");

        // Assert
        Assert.Null(metadata);
    }

    // Security Codes (SEC)

    [Fact]
    public void SEC001_IntegrityCheckFailed_HasCorrectMetadata()
    {
        // Act
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.IntegrityCheckFailed);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Integrity check failed", metadata.Description);
        Assert.Equal(DiagnosticLevel.Critical, metadata.Level);
        Assert.Equal("Abort load", metadata.RecommendedAction);
    }

    [Fact]
    public void SEC002_SecurityRequirementsMutated_HasCorrectMetadata()
    {
        // Act
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.SecurityRequirementsMutated);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Operation security requirements mutated", metadata.Description);
        Assert.Equal(DiagnosticLevel.Info, metadata.Level);
        Assert.Equal("Review security configuration", metadata.RecommendedAction);
    }

    // CSP Codes (CSP)

    [Fact]
    public void CSP001_CspMismatch_HasCorrectMetadata()
    {
        // Act
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.CspMismatch);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("CSP mismatch or missing directives", metadata.Description);
        Assert.Equal(DiagnosticLevel.Error, metadata.Level);
        Assert.Equal("Review policy", metadata.RecommendedAction);
    }

    // Schema Codes (SCH)

    [Fact]
    public void SCH001_SchemaIdCollision_HasCorrectMetadata()
    {
        // Act
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.SchemaIdCollision);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("SchemaId collision suffix applied", metadata.Description);
        Assert.Equal(DiagnosticLevel.Info, metadata.Level);
        Assert.Equal("Confirm stability", metadata.RecommendedAction);
    }

    [Fact]
    public void SCH001_DEPTH_SchemaDepthExceeded_HasCorrectMetadata()
    {
        // Act
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.SchemaDepthExceeded);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Schema generation exceeded maximum depth", metadata.Description);
        Assert.Equal(DiagnosticLevel.Warn, metadata.Level);
        Assert.Equal("Review schema structure", metadata.RecommendedAction);
    }

    // Annotation Codes (ANN)

    [Fact]
    public void ANN001_DataAnnotationsConflict_HasCorrectMetadata()
    {
        // Act
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.DataAnnotationsConflict);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("DataAnnotations conflict (last wins)", metadata.Description);
        Assert.Equal(DiagnosticLevel.Warn, metadata.Level);
        Assert.Equal("Harmonize constraints", metadata.RecommendedAction);
    }

    // Rate Limit Codes (LIM)

    [Fact]
    public void LIM001_RateLimitEnforced_HasCorrectMetadata()
    {
        // Act
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.RateLimitEnforced);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Rate limit bucket enforced", metadata.Description);
        Assert.Equal(DiagnosticLevel.Info, metadata.Level);
        Assert.Equal("Evaluate thresholds", metadata.RecommendedAction);
    }

    [Fact]
    public void LIM002_RateLimitResetAnomaly_HasCorrectMetadata()
    {
        // Act
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.RateLimitResetAnomaly);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Rate limit reset anomaly", metadata.Description);
        Assert.Equal(DiagnosticLevel.Warn, metadata.Level);
        Assert.Equal("Check time source", metadata.RecommendedAction);
    }

    // Type Mapping Codes (MAP)

    [Fact]
    public void MAP001_MapTypeOverride_HasCorrectMetadata()
    {
        // Act
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.MapTypeOverride);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("MapType override applied", metadata.Description);
        Assert.Equal(DiagnosticLevel.Info, metadata.Level);
        Assert.Equal("Validate mapping correctness", metadata.RecommendedAction);
    }

    // Nullability Codes (NRT)

    [Fact]
    public void NRT001_NullabilityMismatch_HasCorrectMetadata()
    {
        // Act
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.NullabilityMismatch);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Nullability mismatch", metadata.Description);
        Assert.Equal(DiagnosticLevel.Error, metadata.Level);
        Assert.Equal("Adjust NRT config", metadata.RecommendedAction);
    }

    // Example Generation Codes (EXM)

    [Fact]
    public void EXM001_ExampleGenerationThrottled_HasCorrectMetadata()
    {
        // Act
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.ExampleGenerationThrottled);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Example generation throttled", metadata.Description);
        Assert.Equal(DiagnosticLevel.Warn, metadata.Level);
        Assert.Equal("Provide explicit example", metadata.RecommendedAction);
    }

    // Virtualization Codes (VIRT)

    [Fact]
    public void VIRT001_VirtualizationThresholdTriggered_HasCorrectMetadata()
    {
        // Act
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.VirtualizationThresholdTriggered);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Virtualization threshold triggered", metadata.Description);
        Assert.Equal(DiagnosticLevel.Info, metadata.Level);
        Assert.Equal("Performance expectation", metadata.RecommendedAction);
    }

    // Retention Codes (RET)

    [Fact]
    public void RET001_RetentionSizePurge_HasCorrectMetadata()
    {
        // Act
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.RetentionSizePurge);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Retention size purge executed", metadata.Description);
        Assert.Equal(DiagnosticLevel.Info, metadata.Level);
        Assert.Equal("Monitor volume", metadata.RecommendedAction);
    }

    [Fact]
    public void RET002_RetentionAgePurge_HasCorrectMetadata()
    {
        // Act
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.RetentionAgePurge);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Retention age purge executed", metadata.Description);
        Assert.Equal(DiagnosticLevel.Info, metadata.Level);
        Assert.Equal("Confirm retentionDays", metadata.RecommendedAction);
    }

    // Policy Codes (POL)

    [Fact]
    public void POL001_PolicyToScopeMapping_HasCorrectMetadata()
    {
        // Act
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.PolicyToScopeMapping);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("PolicyToScope mapping applied", metadata.Description);
        Assert.Equal(DiagnosticLevel.Info, metadata.Level);
        Assert.Equal("Validate scopes", metadata.RecommendedAction);
    }

    // Configuration Codes (CFG)

    [Fact]
    public void CFG001_InvalidPerDocRouteTemplate_HasCorrectMetadata()
    {
        // Act
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.InvalidPerDocRouteTemplate);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Invalid per-doc route template attempt", metadata.Description);
        Assert.Equal(DiagnosticLevel.Info, metadata.Level);
        Assert.Equal("Use global template", metadata.RecommendedAction);
    }

    // Sanitization Codes (SAN)

    [Fact]
    public void SAN001_DisallowedHeadInjection_HasCorrectMetadata()
    {
        // Act
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.DisallowedHeadInjection);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Disallowed head injection", metadata.Description);
        Assert.Equal(DiagnosticLevel.Warn, metadata.Level);
        Assert.Equal("Restrict to meta/link", metadata.RecommendedAction);
    }

    [Fact]
    public void HD001_DisallowedHeadInjectionAttempt_HasCorrectMetadata()
    {
        // Act
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.DisallowedHeadInjectionAttempt);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Disallowed head injection attempt", metadata.Description);
        Assert.Equal(DiagnosticLevel.Warn, metadata.Level);
        Assert.Equal("Use local meta/link", metadata.RecommendedAction);
    }

    // Boundary Codes (BND)

    [Fact]
    public void BND001_MultipartFieldCountExceeded_HasCorrectMetadata()
    {
        // Act
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.MultipartFieldCountExceeded);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Multipart field count limit exceeded", metadata.Description);
        Assert.Equal(DiagnosticLevel.Warn, metadata.Level);
        Assert.Equal("Review field count limits", metadata.RecommendedAction);
    }

    // Link Codes (LNK)

    [Fact]
    public void LNK001_CircularLinkDetected_HasCorrectMetadata()
    {
        // Act
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.CircularLinkDetected);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Circular link detection", metadata.Description);
        Assert.Equal(DiagnosticLevel.Warn, metadata.Level);
        Assert.Equal("Review link structure", metadata.RecommendedAction);
    }

    [Fact]
    public void LNK002_LinkOperationRefFallback_HasCorrectMetadata()
    {
        // Act
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.LinkOperationRefFallback);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Missing operationId but operationRef present", metadata.Description);
        Assert.Equal(DiagnosticLevel.Info, metadata.Level);
        Assert.Equal("Using operationRef fallback", metadata.RecommendedAction);
    }

    [Fact]
    public void LNK003_LinkMissingReference_HasCorrectMetadata()
    {
        // Act
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.LinkMissingReference);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Missing both operationId and operationRef", metadata.Description);
        Assert.Equal(DiagnosticLevel.Warn, metadata.Level);
        Assert.Equal("Provide operationId or operationRef", metadata.RecommendedAction);
    }

    [Fact]
    public void LNK004_LinkBrokenReference_HasCorrectMetadata()
    {
        // Act
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.LinkBrokenReference);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Broken $ref in link", metadata.Description);
        Assert.Equal(DiagnosticLevel.Error, metadata.Level);
        Assert.Equal("Fix reference path", metadata.RecommendedAction);
    }

    [Fact]
    public void LNK005_LinkExternalReference_HasCorrectMetadata()
    {
        // Act
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.LinkExternalReference);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("External or unsupported reference in link", metadata.Description);
        Assert.Equal(DiagnosticLevel.Warn, metadata.Level);
        Assert.Equal("Use internal references only", metadata.RecommendedAction);
    }

    // Callback Codes (CBK)

    [Fact]
    public void CBK001_CallbackReadOnly_HasCorrectMetadata()
    {
        // Act
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.CallbackReadOnly);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Callback section rendered read-only", metadata.Description);
        Assert.Equal(DiagnosticLevel.Info, metadata.Level);
        Assert.Equal("Callbacks do not support Try It Out", metadata.RecommendedAction);
    }

    [Fact]
    public void CBK002_CallbackBrokenReference_HasCorrectMetadata()
    {
        // Act
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.CallbackBrokenReference);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Broken $ref in callback", metadata.Description);
        Assert.Equal(DiagnosticLevel.Error, metadata.Level);
        Assert.Equal("Fix reference path", metadata.RecommendedAction);
    }

    [Theory]
    [InlineData("SEC001")]
    [InlineData("CSP001")]
    [InlineData("SCH001")]
    [InlineData("SCH001-DEPTH")]
    [InlineData("ANN001")]
    [InlineData("LIM001")]
    [InlineData("LIM002")]
    [InlineData("MAP001")]
    [InlineData("NRT001")]
    [InlineData("EXM001")]
    [InlineData("VIRT001")]
    [InlineData("RET001")]
    [InlineData("RET002")]
    [InlineData("POL001")]
    [InlineData("CFG001")]
    [InlineData("SAN001")]
    [InlineData("HD001")]
    [InlineData("BND001")]
    [InlineData("LNK001")]
    [InlineData("LNK002")]
    [InlineData("LNK003")]
    [InlineData("LNK004")]
    [InlineData("LNK005")]
    [InlineData("CBK001")]
    [InlineData("CBK002")]
    public void AllDefinedCodeConstants_AreRecognizedByIsValidCode(string code)
    {
        // Act
        var isValid = DiagnosticCodes.IsValidCode(code);

        // Assert
        Assert.True(isValid, $"Code {code} should be recognized as valid");
    }

    [Fact]
    public void DiagnosticCodeMetadata_HasAllRequiredProperties()
    {
        // Arrange
        var metadata = new DiagnosticCodeMetadata(
            "Test description",
            DiagnosticLevel.Info,
            "Test action");

        // Assert
        Assert.Equal("Test description", metadata.Description);
        Assert.Equal(DiagnosticLevel.Info, metadata.Level);
        Assert.Equal("Test action", metadata.RecommendedAction);
    }

    [Fact]
    public void AllSeverityLevels_AreRepresentedInDefinedCodes()
    {
        // Arrange
        var allCodes = DiagnosticCodes.GetAllCodes();
        var severityLevels = new HashSet<DiagnosticLevel>();

        // Act
        foreach (var code in allCodes)
        {
            var metadata = DiagnosticCodes.GetMetadata(code);
            if (metadata != null)
            {
                severityLevels.Add(metadata.Level);
            }
        }

        // Assert
        Assert.Contains(DiagnosticLevel.Info, severityLevels);
        Assert.Contains(DiagnosticLevel.Warn, severityLevels);
        Assert.Contains(DiagnosticLevel.Error, severityLevels);
        Assert.Contains(DiagnosticLevel.Critical, severityLevels);
    }

    [Fact]
    public void GetAllCodes_ReturnsUniqueValues()
    {
        // Act
        var allCodes = DiagnosticCodes.GetAllCodes();
        var uniqueCodes = new HashSet<string>(allCodes);

        // Assert
        Assert.Equal(allCodes.Length, uniqueCodes.Count);
    }
}
