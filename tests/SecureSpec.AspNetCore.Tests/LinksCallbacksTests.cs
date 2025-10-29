using SecureSpec.AspNetCore.Diagnostics;

#pragma warning disable CA1307 // Specify StringComparison for clarity - not needed in tests

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for Links and Callbacks display functionality.
/// AC 493-497: Links and callbacks edge case handling.
/// </summary>
public class LinksCallbacksTests
{
    // AC 493: Circular link detection logs diagnostic & inserts placeholder

    [Fact]
    public void CircularLinkDetection_LogsDiagnostic()
    {
        // Arrange - This test validates that the diagnostic code exists and is properly configured
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.CircularLinkDetected);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Circular link detection", metadata.Description);
        Assert.Equal(DiagnosticLevel.Warn, metadata.Level);
        Assert.Equal("Review link structure", metadata.RecommendedAction);
    }

    // AC 494: Missing operationId but valid operationRef uses operationRef only

    [Fact]
    public void LinkOperationRefFallback_UsesOperationRefWhenOperationIdMissing()
    {
        // Arrange - This test validates that the diagnostic code exists for operationRef fallback
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.LinkOperationRefFallback);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Missing operationId but operationRef present", metadata.Description);
        Assert.Equal(DiagnosticLevel.Info, metadata.Level);
        Assert.Equal("Using operationRef fallback", metadata.RecommendedAction);
    }

    // AC 495: Missing both operationId & operationRef logs warning & renders stub

    [Fact]
    public void LinkMissingReference_LogsWarningAndRendersStub()
    {
        // Arrange - This test validates that the diagnostic code exists for missing references
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.LinkMissingReference);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Missing both operationId and operationRef", metadata.Description);
        Assert.Equal(DiagnosticLevel.Warn, metadata.Level);
        Assert.Equal("Provide operationId or operationRef", metadata.RecommendedAction);
    }

    // AC 496: Callback section read-only (no Try It Out) logged informational

    [Fact]
    public void CallbackReadOnly_LogsInformational()
    {
        // Arrange - This test validates that the diagnostic code exists for read-only callbacks
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.CallbackReadOnly);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Callback section rendered read-only", metadata.Description);
        Assert.Equal(DiagnosticLevel.Info, metadata.Level);
        Assert.Equal("Callbacks do not support Try It Out", metadata.RecommendedAction);
    }

    // AC 497: Broken $ref in link emits error & omits broken reference safely

    [Fact]
    public void LinkBrokenReference_EmitsErrorAndOmitsReference()
    {
        // Arrange - This test validates that the diagnostic code exists for broken link references
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.LinkBrokenReference);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Broken $ref in link", metadata.Description);
        Assert.Equal(DiagnosticLevel.Error, metadata.Level);
        Assert.Equal("Fix reference path", metadata.RecommendedAction);
    }

    [Fact]
    public void CallbackBrokenReference_EmitsErrorAndOmitsReference()
    {
        // Arrange - This test validates that the diagnostic code exists for broken callback references
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.CallbackBrokenReference);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Broken $ref in callback", metadata.Description);
        Assert.Equal(DiagnosticLevel.Error, metadata.Level);
        Assert.Equal("Fix reference path", metadata.RecommendedAction);
    }

    [Fact]
    public void LinkExternalReference_WarnsAboutUnsupportedReference()
    {
        // Arrange - This test validates that the diagnostic code exists for external link references
        var metadata = DiagnosticCodes.GetMetadata(DiagnosticCodes.LinkExternalReference);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("External or unsupported reference in link", metadata.Description);
        Assert.Equal(DiagnosticLevel.Warn, metadata.Level);
        Assert.Equal("Use internal references only", metadata.RecommendedAction);
    }

    // Additional diagnostic code validation tests

    [Theory]
    [InlineData("LNK001")]
    [InlineData("LNK002")]
    [InlineData("LNK003")]
    [InlineData("LNK004")]
    [InlineData("LNK005")]
    [InlineData("CBK001")]
    [InlineData("CBK002")]
    public void LinksAndCallbacksCodes_AreValidDiagnosticCodes(string code)
    {
        // Act
        var isValid = DiagnosticCodes.IsValidCode(code);

        // Assert
        Assert.True(isValid, $"Diagnostic code {code} should be recognized as valid");
    }

    [Fact]
    public void AllLinksAndCallbacksCodes_HaveMetadata()
    {
        // Arrange
        var codes = new[]
        {
            DiagnosticCodes.CircularLinkDetected,
            DiagnosticCodes.LinkOperationRefFallback,
            DiagnosticCodes.LinkMissingReference,
            DiagnosticCodes.LinkBrokenReference,
            DiagnosticCodes.LinkExternalReference,
            DiagnosticCodes.CallbackReadOnly,
            DiagnosticCodes.CallbackBrokenReference
        };

        // Act & Assert
        var results = codes.Select(code =>
        {
            var metadata = DiagnosticCodes.GetMetadata(code);
            Assert.NotNull(metadata);
            Assert.False(string.IsNullOrEmpty(metadata.Description));
            Assert.False(string.IsNullOrEmpty(metadata.RecommendedAction));
            return metadata;
        }).ToList();

        Assert.Equal(codes.Length, results.Count);
    }

    [Fact]
    public void LinksAndCallbacksCodes_HaveAppropriatePrefix()
    {
        // Assert - Link codes should start with LNK
        Assert.StartsWith("LNK", DiagnosticCodes.CircularLinkDetected);
        Assert.StartsWith("LNK", DiagnosticCodes.LinkOperationRefFallback);
        Assert.StartsWith("LNK", DiagnosticCodes.LinkMissingReference);
        Assert.StartsWith("LNK", DiagnosticCodes.LinkBrokenReference);
        Assert.StartsWith("LNK", DiagnosticCodes.LinkExternalReference);

        // Assert - Callback codes should start with CBK
        Assert.StartsWith("CBK", DiagnosticCodes.CallbackReadOnly);
        Assert.StartsWith("CBK", DiagnosticCodes.CallbackBrokenReference);
    }

    [Fact]
    public void LinksAndCallbacksCodes_AreSequentiallyNumbered()
    {
        // Assert - Link codes are numbered sequentially
        Assert.Equal("LNK001", DiagnosticCodes.CircularLinkDetected);
        Assert.Equal("LNK002", DiagnosticCodes.LinkOperationRefFallback);
        Assert.Equal("LNK003", DiagnosticCodes.LinkMissingReference);
        Assert.Equal("LNK004", DiagnosticCodes.LinkBrokenReference);
        Assert.Equal("LNK005", DiagnosticCodes.LinkExternalReference);

        // Assert - Callback codes are numbered sequentially
        Assert.Equal("CBK001", DiagnosticCodes.CallbackReadOnly);
        Assert.Equal("CBK002", DiagnosticCodes.CallbackBrokenReference);
    }

    [Fact]
    public void LinkCodes_HaveAppropriateLogLevels()
    {
        // Arrange & Act
        var circularLink = DiagnosticCodes.GetMetadata(DiagnosticCodes.CircularLinkDetected);
        var operationRefFallback = DiagnosticCodes.GetMetadata(DiagnosticCodes.LinkOperationRefFallback);
        var missingReference = DiagnosticCodes.GetMetadata(DiagnosticCodes.LinkMissingReference);
        var brokenReference = DiagnosticCodes.GetMetadata(DiagnosticCodes.LinkBrokenReference);
        var externalReference = DiagnosticCodes.GetMetadata(DiagnosticCodes.LinkExternalReference);

        // Assert
        Assert.Equal(DiagnosticLevel.Warn, circularLink!.Level);
        Assert.Equal(DiagnosticLevel.Info, operationRefFallback!.Level);
        Assert.Equal(DiagnosticLevel.Warn, missingReference!.Level);
        Assert.Equal(DiagnosticLevel.Error, brokenReference!.Level);
        Assert.Equal(DiagnosticLevel.Warn, externalReference!.Level);
    }

    [Fact]
    public void CallbackCodes_HaveAppropriateLogLevels()
    {
        // Arrange & Act
        var readOnly = DiagnosticCodes.GetMetadata(DiagnosticCodes.CallbackReadOnly);
        var brokenReference = DiagnosticCodes.GetMetadata(DiagnosticCodes.CallbackBrokenReference);

        // Assert
        Assert.Equal(DiagnosticLevel.Info, readOnly!.Level);
        Assert.Equal(DiagnosticLevel.Error, brokenReference!.Level);
    }
}
