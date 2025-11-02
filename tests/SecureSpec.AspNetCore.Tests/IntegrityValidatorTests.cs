using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.Security;

namespace SecureSpec.AspNetCore.Tests;

public class IntegrityValidatorTests
{
    [Fact]
    public void ComputeHash_WithSimpleContent_ReturnsValidSha256Hash()
    {
        // Arrange
        const string content = "Hello, World!";

        // Act
        var hash = IntegrityValidator.ComputeHash(content);

        // Assert
        Assert.NotNull(hash);
        Assert.Equal(64, hash.Length); // SHA256 produces 64 hex characters
        Assert.All(hash, c => Assert.True(char.IsAsciiHexDigit(c) && !char.IsUpper(c)));
    }

    [Fact]
    public void ComputeHash_WithCrlfContent_NormalizesToLf()
    {
        // Arrange
        const string contentWithCrlf = "Line 1\r\nLine 2\r\nLine 3";
        const string contentWithLf = "Line 1\nLine 2\nLine 3";

        // Act
        var hashCrlf = IntegrityValidator.ComputeHash(contentWithCrlf);
        var hashLf = IntegrityValidator.ComputeHash(contentWithLf);

        // Assert - AC 499: SHA256 hashing performed after normalization (LF, UTF-8)
        Assert.Equal(hashLf, hashCrlf);
    }

    [Fact]
    public void ComputeHash_WithUtf8Content_HandlesCorrectly()
    {
        // Arrange
        const string content = "Hello 世界! 🌍";

        // Act
        var hash = IntegrityValidator.ComputeHash(content);

        // Assert
        Assert.NotNull(hash);
        Assert.Equal(64, hash.Length);
    }

    [Fact]
    public void ComputeHash_WithEmptyString_ReturnsHash()
    {
        // Arrange
        const string content = "";

        // Act
        var hash = IntegrityValidator.ComputeHash(content);

        // Assert
        Assert.NotNull(hash);
        Assert.Equal(64, hash.Length);
        // SHA256 of empty string is known value
        Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", hash);
    }

    [Fact]
    public void GenerateSri_WithSimpleContent_ReturnsValidSriFormat()
    {
        // Arrange
        const string content = "Hello, World!";

        // Act
        var sri = IntegrityValidator.GenerateSri(content);

        // Assert
        Assert.NotNull(sri);
        Assert.StartsWith("sha256-", sri, StringComparison.Ordinal);
        var base64Part = sri.Substring(7);
        Assert.NotEmpty(base64Part);
        // Verify it's valid base64
        var bytes = Convert.FromBase64String(base64Part);
        Assert.Equal(32, bytes.Length); // SHA256 produces 32 bytes
    }

    [Fact]
    public void GenerateSri_WithCrlfContent_NormalizesToLf()
    {
        // Arrange
        const string contentWithCrlf = "Line 1\r\nLine 2\r\nLine 3";
        const string contentWithLf = "Line 1\nLine 2\nLine 3";

        // Act
        var sriCrlf = IntegrityValidator.GenerateSri(contentWithCrlf);
        var sriLf = IntegrityValidator.GenerateSri(contentWithLf);

        // Assert - AC 499: SHA256 hashing performed after normalization
        Assert.Equal(sriLf, sriCrlf);
    }

    [Fact]
    public void GenerateSri_WithUtf8Content_HandlesCorrectly()
    {
        // Arrange
        const string content = "Hello 世界! 🌍";

        // Act
        var sri = IntegrityValidator.GenerateSri(content);

        // Assert
        Assert.NotNull(sri);
        Assert.StartsWith("sha256-", sri, StringComparison.Ordinal);
    }

    [Fact]
    public void VerifyIntegrity_WithMatchingHash_ReturnsTrue()
    {
        // Arrange
        var validator = new IntegrityValidator();
        const string content = "Hello, World!";
        var expectedHash = IntegrityValidator.ComputeHash(content);

        // Act
        var result = validator.VerifyIntegrity(content, expectedHash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyIntegrity_WithMismatchedHash_ReturnsFalse()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        var validator = new IntegrityValidator(logger);
        const string content = "Hello, World!";
        const string wrongHash = "0000000000000000000000000000000000000000000000000000000000000000";

        // Act
        var result = validator.VerifyIntegrity(content, wrongHash);

        // Assert
        Assert.False(result);

        // Verify SEC001 diagnostic was logged
        var events = logger.GetEvents();
        var sec001Event = events.FirstOrDefault(e => e.Code == "SEC001");
        Assert.NotNull(sec001Event);
        Assert.Equal(DiagnosticLevel.Critical, sec001Event.Level);
        Assert.Equal("Integrity check failed", sec001Event.Message);
    }

    [Fact]
    public void VerifyIntegrity_WithMismatchedHash_RedactsPathInDiagnostic()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        var validator = new IntegrityValidator(logger);
        const string content = "Hello, World!";
        const string wrongHash = "0000000000000000000000000000000000000000000000000000000000000000";
        const string resourcePath = "/api/swagger/v1/swagger.json";

        // Act
        var result = validator.VerifyIntegrity(content, wrongHash, resourcePath);

        // Assert
        Assert.False(result);

        // AC 500: Integrity mismatch diagnostic redacts path & partial hash only
        var events = logger.GetEvents();
        var sec001Event = events.FirstOrDefault(e => e.Code == "SEC001");
        Assert.NotNull(sec001Event);

        // Verify context contains redacted path
        var context = sec001Event.Context;
        Assert.NotNull(context);
        var contextDict = context.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(context)?.ToString());
        Assert.Equal("[REDACTED]", contextDict["ResourcePathRedacted"]);

        // Verify only partial hashes are shown
        var expected = contextDict["Expected"];
        var actual = contextDict["Actual"];
        Assert.NotNull(expected);
        Assert.NotNull(actual);
        Assert.EndsWith("...", expected, StringComparison.Ordinal);
        Assert.EndsWith("...", actual, StringComparison.Ordinal);
    }

    [Fact]
    public void VerifyIntegrity_WithCaseInsensitiveHash_ReturnsTrue()
    {
        // Arrange
        var validator = new IntegrityValidator();
        const string content = "Hello, World!";
        var expectedHash = IntegrityValidator.ComputeHash(content);
        var upperCaseHash = expectedHash.ToUpperInvariant();

        // Act
        var result = validator.VerifyIntegrity(content, upperCaseHash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifySri_WithValidSri_ReturnsTrue()
    {
        // Arrange
        var validator = new IntegrityValidator();
        const string content = "Hello, World!";
        var expectedSri = IntegrityValidator.GenerateSri(content);

        // Act
        var result = validator.VerifySri(content, expectedSri);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifySri_WithMismatchedSri_ReturnsFalse()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        var validator = new IntegrityValidator(logger);
        const string content = "Hello, World!";
        const string wrongSri = "sha256-AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

        // Act
        var result = validator.VerifySri(content, wrongSri);

        // Assert
        Assert.False(result);

        // Verify SEC001 diagnostic was logged
        var events = logger.GetEvents();
        var sec001Event = events.FirstOrDefault(e => e.Code == "SEC001");
        Assert.NotNull(sec001Event);
        Assert.Equal(DiagnosticLevel.Critical, sec001Event.Level);
        Assert.Equal("SRI integrity check failed", sec001Event.Message);
    }

    [Fact]
    public void VerifySri_WithInvalidFormat_ReturnsFalse()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        var validator = new IntegrityValidator(logger);
        const string content = "Hello, World!";
        const string invalidSri = "invalidformat";

        // Act
        var result = validator.VerifySri(content, invalidSri);

        // Assert
        Assert.False(result);

        // Verify SEC001 diagnostic was logged
        var events = logger.GetEvents();
        var sec001Event = events.FirstOrDefault(e => e.Code == "SEC001");
        Assert.NotNull(sec001Event);
        Assert.Equal("Invalid SRI format", sec001Event.Message);
    }

    [Fact]
    public void VerifySri_WithUnsupportedAlgorithm_ReturnsFalse()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        var validator = new IntegrityValidator(logger);
        const string content = "Hello, World!";
        const string unsupportedSri = "sha512-AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

        // Act
        var result = validator.VerifySri(content, unsupportedSri);

        // Assert
        Assert.False(result);

        // Verify SEC001 diagnostic was logged
        var events = logger.GetEvents();
        var sec001Event = events.FirstOrDefault(e => e.Code == "SEC001");
        Assert.NotNull(sec001Event);
        Assert.Equal("Unsupported SRI algorithm", sec001Event.Message);
    }

    [Fact]
    public void VerifySri_WithCrlfContent_NormalizesToLf()
    {
        // Arrange
        var validator = new IntegrityValidator();
        const string contentWithCrlf = "Line 1\r\nLine 2\r\nLine 3";
        const string contentWithLf = "Line 1\nLine 2\nLine 3";
        var sri = IntegrityValidator.GenerateSri(contentWithLf);

        // Act
        var result = validator.VerifySri(contentWithCrlf, sri);

        // Assert - AC 499: SHA256 hashing performed after normalization
        Assert.True(result);
    }

    [Fact]
    public void VerifySri_WithResourcePath_RedactsInDiagnostic()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        var validator = new IntegrityValidator(logger);
        const string content = "Hello, World!";
        const string wrongSri = "sha256-AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";
        const string resourcePath = "/assets/script.js";

        // Act
        var result = validator.VerifySri(content, wrongSri, resourcePath);

        // Assert
        Assert.False(result);

        // AC 500: Integrity mismatch diagnostic redacts path & partial hash only
        var events = logger.GetEvents();
        var sec001Event = events.FirstOrDefault(e => e.Code == "SEC001");
        Assert.NotNull(sec001Event);

        // Verify context contains redacted path
        var context = sec001Event.Context;
        Assert.NotNull(context);
        var contextDict = context.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(context)?.ToString());
        Assert.Equal("[REDACTED]", contextDict["ResourcePathRedacted"]);
    }

    [Fact]
    public void ComputeHash_Deterministic_SameContentProducesSameHash()
    {
        // Arrange
        const string content = "Test content for determinism";

        // Act
        var hash1 = IntegrityValidator.ComputeHash(content);
        var hash2 = IntegrityValidator.ComputeHash(content);
        var hash3 = IntegrityValidator.ComputeHash(content);

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.Equal(hash2, hash3);
    }

    [Fact]
    public void GenerateSri_Deterministic_SameContentProducesSameSri()
    {
        // Arrange
        const string content = "Test content for determinism";

        // Act
        var sri1 = IntegrityValidator.GenerateSri(content);
        var sri2 = IntegrityValidator.GenerateSri(content);
        var sri3 = IntegrityValidator.GenerateSri(content);

        // Assert
        Assert.Equal(sri1, sri2);
        Assert.Equal(sri2, sri3);
    }

    [Theory]
    [InlineData(null)]
    public void ComputeHash_WithNullContent_ThrowsArgumentNullException(string? content)
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => IntegrityValidator.ComputeHash(content!));
    }

    [Theory]
    [InlineData(null)]
    public void GenerateSri_WithNullContent_ThrowsArgumentNullException(string? content)
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => IntegrityValidator.GenerateSri(content!));
    }

    [Theory]
    [InlineData(null, "hash")]
    [InlineData("content", null)]
    public void VerifyIntegrity_WithNullArguments_ThrowsArgumentNullException(string? content, string? hash)
    {
        // Arrange
        var validator = new IntegrityValidator();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => validator.VerifyIntegrity(content!, hash!));
    }

    [Theory]
    [InlineData(null, "sri")]
    [InlineData("content", null)]
    public void VerifySri_WithNullArguments_ThrowsArgumentNullException(string? content, string? sri)
    {
        // Arrange
        var validator = new IntegrityValidator();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => validator.VerifySri(content!, sri!));
    }
}
