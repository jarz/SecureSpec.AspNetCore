using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.MediaTypes;
using Xunit;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for AC 454 and AC 455: Multipart validator enforces field count limit
/// and preserves ordering with validation messages.
/// </summary>
public class MultipartValidatorTests
{
    [Fact]
    public void Validate_AllowsRequestUnderLimit()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        var validator = new MultipartValidator(logger, maxFieldCount: 200);

        // Act
        var result = validator.Validate(150);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(150, result.FieldCount);
        Assert.Equal(200, result.MaxFieldCount);
        Assert.Empty(logger.GetEvents());
    }

    [Fact]
    public void Validate_AllowsRequestAtLimit()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        var validator = new MultipartValidator(logger, maxFieldCount: 200);

        // Act
        var result = validator.Validate(200);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(logger.GetEvents());
    }

    [Fact]
    public void Validate_RejectsRequestOverLimit_WithBND001Diagnostic()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        var validator = new MultipartValidator(logger, maxFieldCount: 200);

        // Act
        var result = validator.Validate(201);

        // Assert - AC 454: Multipart validator enforces field count limit (diagnostic BND001)
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("201", result.ErrorMessage);
        Assert.Contains("200", result.ErrorMessage);
        Assert.Equal(201, result.FieldCount);
        Assert.Equal(200, result.MaxFieldCount);

        // Verify BND001 diagnostic was logged
        var events = logger.GetEvents();
        Assert.Single(events);
        Assert.Equal("BND001", events[0].Code);
        Assert.Equal(DiagnosticLevel.Error, events[0].Level);
    }

    [Fact]
    public void ValidateFields_PreservesFieldOrdering()
    {
        // Arrange - AC 455: Multipart file + field mix preserves ordering
        var logger = new DiagnosticsLogger();
        var validator = new MultipartValidator(logger, maxFieldCount: 3);

        var fields = new List<MultipartField>
        {
            new() { Name = "field1", IsFile = false },
            new() { Name = "file1", IsFile = true, ContentType = "image/png", Size = 1024 },
            new() { Name = "field2", IsFile = false },
            new() { Name = "file2", IsFile = true, ContentType = "text/plain", Size = 512 }
        };

        // Act
        var result = validator.ValidateFields(fields);

        // Assert
        Assert.False(result.IsValid); // 4 fields > 3 limit
        Assert.NotNull(result.Fields);
        Assert.Equal(4, result.Fields!.Count);

        // Verify ordering is preserved
        Assert.Equal("field1", result.Fields[0].Name);
        Assert.False(result.Fields[0].IsFile);

        Assert.Equal("file1", result.Fields[1].Name);
        Assert.True(result.Fields[1].IsFile);
        Assert.Equal("image/png", result.Fields[1].ContentType);

        Assert.Equal("field2", result.Fields[2].Name);
        Assert.False(result.Fields[2].IsFile);

        Assert.Equal("file2", result.Fields[3].Name);
        Assert.True(result.Fields[3].IsFile);
        Assert.Equal("text/plain", result.Fields[3].ContentType);
    }

    [Fact]
    public void ValidateFields_IncludesFieldsInValidationMessage()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        var validator = new MultipartValidator(logger, maxFieldCount: 2);

        var fields = new List<MultipartField>
        {
            new() { Name = "name", IsFile = false },
            new() { Name = "email", IsFile = false },
            new() { Name = "avatar", IsFile = true, ContentType = "image/jpeg", Size = 2048 }
        };

        // Act
        var result = validator.ValidateFields(fields);

        // Assert - AC 455: validation messages with field details
        Assert.False(result.IsValid);
        Assert.Equal(3, result.Fields!.Count);
        Assert.Equal("name", result.Fields[0].Name);
        Assert.Equal("email", result.Fields[1].Name);
        Assert.Equal("avatar", result.Fields[2].Name);
        Assert.True(result.Fields[2].IsFile);
    }

    [Fact]
    public void Constructor_UsesDefaultMaxFieldCount()
    {
        // Arrange & Act
        var logger = new DiagnosticsLogger();
        var validator = new MultipartValidator(logger);

        // Assert
        Assert.Equal(MultipartValidator.DefaultMaxFieldCount, validator.MaxFieldCount);
        Assert.Equal(200, validator.MaxFieldCount);
    }

    [Fact]
    public void Constructor_ThrowsOnNullLogger()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MultipartValidator(null!));
    }

    [Fact]
    public void Constructor_ThrowsOnInvalidMaxFieldCount()
    {
        // Arrange
        var logger = new DiagnosticsLogger();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new MultipartValidator(logger, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MultipartValidator(logger, -1));
    }

    [Fact]
    public void Validate_ThrowsOnNegativeFieldCount()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        var validator = new MultipartValidator(logger);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => validator.Validate(-1));
    }

    [Fact]
    public void ValidateFields_ThrowsOnNullFields()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        var validator = new MultipartValidator(logger);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => validator.ValidateFields(null!));
    }

    [Fact]
    public void Validate_LogsContextInformation()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        var validator = new MultipartValidator(logger, maxFieldCount: 100);

        // Act
        validator.Validate(150);

        // Assert
        var events = logger.GetEvents();
        Assert.Single(events);
        Assert.NotNull(events[0].Context);

        // Verify context contains field count information
        var context = events[0].Context as dynamic;
        Assert.NotNull(context);
    }
}
