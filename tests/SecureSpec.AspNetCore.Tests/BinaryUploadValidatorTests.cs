using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.MediaTypes;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for AC 456: Binary size threshold enforcement logs binaryUploadBlocked before dispatch.
/// </summary>
public class BinaryUploadValidatorTests
{
    [Fact]
    public void ValidateBeforeDispatch_AllowsUploadUnderThreshold()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        var validator = new BinaryUploadValidator(logger, maxBinarySize: 10 * 1024 * 1024);

        // Act
        var result = validator.ValidateBeforeDispatch(5 * 1024 * 1024);

        // Assert
        Assert.True(result.IsAllowed);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(5 * 1024 * 1024, result.ContentLength);
        Assert.Equal(10 * 1024 * 1024, result.MaxBinarySize);
        Assert.Empty(logger.GetEvents());
    }

    [Fact]
    public void ValidateBeforeDispatch_AllowsUploadAtThreshold()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        var validator = new BinaryUploadValidator(logger, maxBinarySize: 10 * 1024 * 1024);

        // Act
        var result = validator.ValidateBeforeDispatch(10 * 1024 * 1024);

        // Assert
        Assert.True(result.IsAllowed);
        Assert.Empty(logger.GetEvents());
    }

    [Fact]
    public void ValidateBeforeDispatch_BlocksUploadOverThreshold_LogsBeforeDispatch()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        var validator = new BinaryUploadValidator(logger, maxBinarySize: 10 * 1024 * 1024);

        // Act - AC 456: Logs binaryUploadBlocked BEFORE dispatch
        var result = validator.ValidateBeforeDispatch(15 * 1024 * 1024);

        // Assert
        Assert.False(result.IsAllowed);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("15728640", result.ErrorMessage, StringComparison.Ordinal); // 15 * 1024 * 1024
        Assert.Contains("10485760", result.ErrorMessage, StringComparison.Ordinal); // 10 * 1024 * 1024
        Assert.Equal(15 * 1024 * 1024, result.ContentLength);
        Assert.Equal(10 * 1024 * 1024, result.MaxBinarySize);

        // Verify BIN001 diagnostic was logged
        var events = logger.GetEvents();
        Assert.Single(events);
        Assert.Equal("BIN001", events[0].Code);
        Assert.Equal(DiagnosticLevel.Error, events[0].Level);
        Assert.Contains("Binary upload blocked", events[0].Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateBeforeDispatch_LogsEventTypeInContext()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        var validator = new BinaryUploadValidator(logger, maxBinarySize: 1000);

        // Act
        validator.ValidateBeforeDispatch(2000);

        // Assert - AC 456: EventType should be "binaryUploadBlocked"
        var events = logger.GetEvents();
        Assert.Single(events);
        var context = events[0].Context;
        Assert.NotNull(context);

        // Verify context contains binaryUploadBlocked event type
        var contextString = context?.ToString() ?? "";
        Assert.Contains("binaryUploadBlocked", contextString, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_WithMetadata_BlocksOverThreshold()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        var validator = new BinaryUploadValidator(logger, maxBinarySize: 1024);

        var metadata = new BinaryUploadMetadata
        {
            ContentLength = 2048,
            ContentType = "application/octet-stream",
            FileName = "largefile.bin"
        };

        // Act
        var result = validator.Validate(metadata);

        // Assert
        Assert.False(result.IsAllowed);
        Assert.Equal(2048, result.ContentLength);

        // Verify metadata was logged in context
        var events = logger.GetEvents();
        Assert.Single(events);
        var context = events[0].Context?.ToString() ?? "";
        Assert.Contains("largefile.bin", context, StringComparison.Ordinal);
        Assert.Contains("application/octet-stream", context, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_UsesDefaultMaxBinarySize()
    {
        // Arrange & Act
        var logger = new DiagnosticsLogger();
        var validator = new BinaryUploadValidator(logger);

        // Assert
        Assert.Equal(BinaryUploadValidator.DefaultMaxBinarySize, validator.MaxBinarySize);
        Assert.Equal(10 * 1024 * 1024, validator.MaxBinarySize);
    }

    [Fact]
    public void Constructor_ThrowsOnNullLogger()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BinaryUploadValidator(null!));
    }

    [Fact]
    public void Constructor_ThrowsOnInvalidMaxBinarySize()
    {
        // Arrange
        var logger = new DiagnosticsLogger();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new BinaryUploadValidator(logger, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BinaryUploadValidator(logger, -1));
    }

    [Fact]
    public void ValidateBeforeDispatch_ThrowsOnNegativeContentLength()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        var validator = new BinaryUploadValidator(logger);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => validator.ValidateBeforeDispatch(-1));
    }

    [Fact]
    public void Validate_ThrowsOnNullMetadata()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        var validator = new BinaryUploadValidator(logger);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => validator.Validate(null!));
    }

    [Fact]
    public void ValidateBeforeDispatch_IncludesCustomContextInLog()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        var validator = new BinaryUploadValidator(logger, maxBinarySize: 100);

        var customContext = new { Operation = "FileUpload", UserId = "user123" };

        // Act
        validator.ValidateBeforeDispatch(200, customContext);

        // Assert
        var events = logger.GetEvents();
        Assert.Single(events);
        var contextString = events[0].Context?.ToString() ?? "";
        Assert.Contains("FileUpload", contextString, StringComparison.Ordinal);
        Assert.Contains("user123", contextString, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateBeforeDispatch_AllowsZeroSizeUpload()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        var validator = new BinaryUploadValidator(logger);

        // Act
        var result = validator.ValidateBeforeDispatch(0);

        // Assert
        Assert.True(result.IsAllowed);
        Assert.Equal(0, result.ContentLength);
        Assert.Empty(logger.GetEvents());
    }
}
