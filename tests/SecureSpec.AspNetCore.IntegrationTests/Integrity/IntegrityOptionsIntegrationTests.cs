using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.Security;
using Xunit;

namespace SecureSpec.AspNetCore.IntegrationTests.Integrity;

/// <summary>
/// Integration-like coverage for <see cref="IntegrityValidator"/> behavior.
/// </summary>
public class IntegrityValidatorIntegrationTests
{
    /// <summary>
    /// Ensures integrity mismatches log a critical diagnostic with redacted metadata.
    /// </summary>
    [Fact]
    public void VerifyIntegrity_MismatchLogsCriticalEvent()
    {
        var logger = new DiagnosticsLogger();
        var validator = new IntegrityValidator(logger);

        var isValid = validator.VerifyIntegrity("content", "deadbeef", "/var/www/app.js");

        Assert.False(isValid);
        var events = logger.GetEvents();
        var criticalEvent = Assert.Single(events);
        Assert.Equal("SEC001", criticalEvent.Code);
        Assert.Equal(DiagnosticLevel.Critical, criticalEvent.Level);
        Assert.NotNull(criticalEvent.Context);
    }

    /// <summary>
    /// Ensures invalid SRI algorithm emits an integrity failure diagnostic.
    /// </summary>
    [Fact]
    public void VerifySri_InvalidAlgorithmLogsError()
    {
        var logger = new DiagnosticsLogger();
        var validator = new IntegrityValidator(logger);

        var result = validator.VerifySri("body", "sha1-abc", "index.js");

        Assert.False(result);
        var events = logger.GetEvents();
        var evt = Assert.Single(events);
        Assert.Equal(DiagnosticCodes.IntegrityCheckFailed, evt.Code);
        Assert.Equal(DiagnosticLevel.Critical, evt.Level);
    }

    /// <summary>
    /// Verifies SRI generation returns the expected prefix and matches verification.
    /// </summary>
    [Fact]
    public void GenerateSri_CanBeVerified()
    {
        const string content = "window.alert('hash me');";

        var sri = IntegrityValidator.GenerateSri(content);
        Assert.StartsWith("sha256-", sri, System.StringComparison.Ordinal);

        var logger = new DiagnosticsLogger();
        var validator = new IntegrityValidator(logger);

        var result = validator.VerifySri(content, sri);
        Assert.True(result);
        Assert.Empty(logger.GetEvents());
    }
}
