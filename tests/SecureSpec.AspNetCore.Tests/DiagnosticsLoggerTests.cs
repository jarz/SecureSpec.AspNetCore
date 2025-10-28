using SecureSpec.AspNetCore.Diagnostics;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for the DiagnosticsLogger class.
/// </summary>
public class DiagnosticsLoggerTests
{
    [Fact]
    public void Log_StoresEventWithAllProperties()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        var context = new { Key = "Value" };

        // Act
        logger.Log(DiagnosticLevel.Error, "TEST001", "Test message", context, sanitized: true);

        // Assert
        var events = logger.GetEvents();
        Assert.Single(events);
        Assert.Equal(DiagnosticLevel.Error, events[0].Level);
        Assert.Equal("TEST001", events[0].Code);
        Assert.Equal("Test message", events[0].Message);
        Assert.Equal(context, events[0].Context);
        Assert.True(events[0].Sanitized);
    }

    [Fact]
    public void LogInfo_LogsInfoLevelEvent()
    {
        // Arrange
        var logger = new DiagnosticsLogger();

        // Act
        logger.LogInfo("INFO001", "Info message");

        // Assert
        var events = logger.GetEvents();
        Assert.Single(events);
        Assert.Equal(DiagnosticLevel.Info, events[0].Level);
        Assert.Equal("INFO001", events[0].Code);
        Assert.Equal("Info message", events[0].Message);
        Assert.False(events[0].Sanitized);
    }

    [Fact]
    public void LogWarning_LogsWarnLevelEvent()
    {
        // Arrange
        var logger = new DiagnosticsLogger();

        // Act
        logger.LogWarning("WARN001", "Warning message");

        // Assert
        var events = logger.GetEvents();
        Assert.Single(events);
        Assert.Equal(DiagnosticLevel.Warn, events[0].Level);
        Assert.Equal("WARN001", events[0].Code);
        Assert.Equal("Warning message", events[0].Message);
    }

    [Fact]
    public void LogError_LogsErrorLevelEvent()
    {
        // Arrange
        var logger = new DiagnosticsLogger();

        // Act
        logger.LogError("ERR001", "Error message");

        // Assert
        var events = logger.GetEvents();
        Assert.Single(events);
        Assert.Equal(DiagnosticLevel.Error, events[0].Level);
        Assert.Equal("ERR001", events[0].Code);
        Assert.Equal("Error message", events[0].Message);
    }

    [Fact]
    public void LogCritical_LogsCriticalLevelEvent()
    {
        // Arrange
        var logger = new DiagnosticsLogger();

        // Act
        logger.LogCritical("CRIT001", "Critical message");

        // Assert
        var events = logger.GetEvents();
        Assert.Single(events);
        Assert.Equal(DiagnosticLevel.Critical, events[0].Level);
        Assert.Equal("CRIT001", events[0].Code);
        Assert.Equal("Critical message", events[0].Message);
    }

    [Fact]
    public void Log_WithSanitizedFlag_SetsSanitizedProperty()
    {
        // Arrange
        var logger = new DiagnosticsLogger();

        // Act
        logger.LogError("SEC001", "Sanitized error", null, sanitized: true);

        // Assert
        var events = logger.GetEvents();
        Assert.Single(events);
        Assert.True(events[0].Sanitized);
    }

    [Fact]
    public void Log_WithoutSanitizedFlag_DefaultsToFalse()
    {
        // Arrange
        var logger = new DiagnosticsLogger();

        // Act
        logger.LogError("SEC001", "Non-sanitized error");

        // Assert
        var events = logger.GetEvents();
        Assert.Single(events);
        Assert.False(events[0].Sanitized);
    }

    [Fact]
    public void Log_WithContext_StoresContextObject()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        var context = new
        {
            Bucket = "TryItOut",
            Remaining = 0,
            WindowSeconds = 60
        };

        // Act
        logger.LogInfo("LIM001", "Rate limit enforced", context);

        // Assert
        var events = logger.GetEvents();
        Assert.Single(events);
        Assert.NotNull(events[0].Context);
        Assert.Equal(context, events[0].Context);
    }

    [Fact]
    public void GetEvents_ReturnsAllLoggedEvents()
    {
        // Arrange
        var logger = new DiagnosticsLogger();

        // Act
        logger.LogInfo("INFO001", "First");
        logger.LogWarning("WARN001", "Second");
        logger.LogError("ERR001", "Third");

        // Assert
        var events = logger.GetEvents();
        Assert.Equal(3, events.Count);
        Assert.Equal("INFO001", events[0].Code);
        Assert.Equal("WARN001", events[1].Code);
        Assert.Equal("ERR001", events[2].Code);
    }

    [Fact]
    public void Clear_RemovesAllEvents()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        logger.LogInfo("INFO001", "Test");
        logger.LogWarning("WARN001", "Test");

        // Act
        logger.Clear();

        // Assert
        var events = logger.GetEvents();
        Assert.Empty(events);
    }

    [Fact]
    public void Log_ThrowsArgumentNullException_WhenCodeIsNull()
    {
        // Arrange
        var logger = new DiagnosticsLogger();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            logger.Log(DiagnosticLevel.Info, null!, "Message"));
    }

    [Fact]
    public void Log_ThrowsArgumentNullException_WhenMessageIsNull()
    {
        // Arrange
        var logger = new DiagnosticsLogger();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            logger.Log(DiagnosticLevel.Info, "CODE001", null!));
    }

    [Fact]
    public void SanitizeHash_ReturnsEmptyString_WhenInputIsNull()
    {
        // Act
        var result = DiagnosticsLogger.SanitizeHash(null!);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SanitizeHash_ReturnsEmptyString_WhenInputIsEmpty()
    {
        // Act
        var result = DiagnosticsLogger.SanitizeHash(string.Empty);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SanitizeHash_ReturnsFullString_WhenLengthIs8OrLess()
    {
        // Act
        var result1 = DiagnosticsLogger.SanitizeHash("abc123");
        var result2 = DiagnosticsLogger.SanitizeHash("12345678");

        // Assert
        Assert.Equal("abc123", result1);
        Assert.Equal("12345678", result2);
    }

    [Fact]
    public void SanitizeHash_TruncatesAndAddsEllipsis_WhenLengthExceeds8()
    {
        // Act
        var result = DiagnosticsLogger.SanitizeHash("a1b2c3d4e5f6g7h8i9");

        // Assert
        Assert.Equal("a1b2c3d4...", result);
    }

    [Fact]
    public void SanitizeHash_HandlesExactly64CharacterSha256Hash()
    {
        // Arrange
        const string sha256Hash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        // Act
        var result = DiagnosticsLogger.SanitizeHash(sha256Hash);

        // Assert
        Assert.Equal("e3b0c442...", result);
    }

    [Fact]
    public void SanitizePath_ReturnsEmptyString_WhenInputIsNull()
    {
        // Act
        var result = DiagnosticsLogger.SanitizePath(null!);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SanitizePath_ReturnsEmptyString_WhenInputIsEmpty()
    {
        // Act
        var result = DiagnosticsLogger.SanitizePath(string.Empty);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SanitizePath_ExtractsFilename_FromWindowsPath()
    {
        // Act
        var result = DiagnosticsLogger.SanitizePath(@"C:\Users\Admin\Documents\file.txt");

        // Assert
        Assert.Equal("file.txt", result);
    }

    [Fact]
    public void SanitizePath_ExtractsFilename_FromUnixPath()
    {
        // Act
        var result = DiagnosticsLogger.SanitizePath("/home/user/documents/file.txt");

        // Assert
        Assert.Equal("file.txt", result);
    }

    [Fact]
    public void SanitizePath_ReturnsFilename_WhenNoDirectoryPresent()
    {
        // Act
        var result = DiagnosticsLogger.SanitizePath("file.txt");

        // Assert
        Assert.Equal("file.txt", result);
    }

    [Fact]
    public void Log_SetsTimestamp_ToCurrentUtcTime()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        var before = DateTimeOffset.UtcNow;

        // Act
        logger.LogInfo("INFO001", "Test");
        var after = DateTimeOffset.UtcNow;

        // Assert
        var events = logger.GetEvents();
        Assert.Single(events);
        Assert.InRange(events[0].Timestamp, before, after);
    }

    [Fact]
    public void GetEvents_ReturnsSnapshot_NotLiveCollection()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        logger.LogInfo("INFO001", "First");

        // Act
        var events1 = logger.GetEvents();
        logger.LogInfo("INFO002", "Second");
        var events2 = logger.GetEvents();

        // Assert
        Assert.Single(events1);
        Assert.Equal(2, events2.Count);
    }
}
