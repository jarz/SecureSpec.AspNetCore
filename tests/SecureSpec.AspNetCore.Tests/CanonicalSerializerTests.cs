using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Serialization;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for the canonical serializer.
/// </summary>
public class CanonicalSerializerTests
{
    [Fact]
    public void GenerateHash_WithValidContent_ReturnsLowercaseHex()
    {
        // Arrange
        const string content = "test content";

        // Act
        var hash = CanonicalSerializer.GenerateHash(content);

        // Assert
        Assert.NotNull(hash);
        Assert.Equal(64, hash.Length); // SHA256 produces 64 hex characters
        Assert.All(hash, c => Assert.True(char.IsDigit(c) || (c >= 'a' && c <= 'f')));
    }

    [Fact]
    public void GenerateETag_WithValidHash_ReturnsCorrectFormat()
    {
        // Arrange
        const string hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

        // Act
        var etag = CanonicalSerializer.GenerateETag(hash);

        // Assert
        Assert.Equal("W/\"sha256:0123456789abcdef\"", etag);
    }

    [Fact]
    public void GenerateETag_WithShortHash_ThrowsArgumentException()
    {
        const string shortHash = "tooshort";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => CanonicalSerializer.GenerateETag(shortHash));
    }

    [Fact]
    public void SerializeToJson_WithSimpleDocument_ReturnsValidJson()
    {
        // Arrange
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = "Test API",
                Version = "1.0.0"
            },
            Paths = new OpenApiPaths()
        };

        // Act
        var json = CanonicalSerializer.SerializeToJson(document);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\"openapi\":", json, StringComparison.Ordinal);
        Assert.Contains("\"info\":", json, StringComparison.Ordinal);
        Assert.Contains("\"Test API\"", json, StringComparison.Ordinal);
        Assert.Contains("\"1.0.0\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeToJson_UsesLfLineEndings()
    {
        // Arrange (AC 499)
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = "Test API",
                Version = "1.0"
            },
            Paths = new OpenApiPaths()
        };

        // Act
        var json = CanonicalSerializer.SerializeToJson(document);

        // Assert
        Assert.DoesNotContain("\r\n", json, StringComparison.Ordinal); // No CRLF
        Assert.Contains("\n", json, StringComparison.Ordinal); // Has LF
    }

    [Fact]
    public void SerializeToYaml_WithSimpleDocument_ReturnsValidYaml()
    {
        // Arrange
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = "Test API",
                Version = "1.0.0"
            },
            Paths = new OpenApiPaths()
        };

        // Act
        var yaml = CanonicalSerializer.SerializeToYaml(document);

        // Assert
        Assert.NotNull(yaml);
        Assert.Contains("openapi:", yaml, StringComparison.Ordinal);
        Assert.Contains("info:", yaml, StringComparison.Ordinal);
        Assert.Contains("Test API", yaml, StringComparison.Ordinal);
        Assert.Contains("1.0.0", yaml, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeToYaml_UsesLfLineEndings()
    {
        // Arrange
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = "Test API",
                Version = "1.0"
            },
            Paths = new OpenApiPaths()
        };

        // Act
        var yaml = CanonicalSerializer.SerializeToYaml(document);

        // Assert
        Assert.DoesNotContain("\r\n", yaml, StringComparison.Ordinal); // No CRLF
        Assert.Contains("\n", yaml, StringComparison.Ordinal); // Has LF
    }

    [Fact]
    public void GenerateHash_NormalizesCrLfToLf()
    {
        // Arrange (AC 499)
        const string contentWithCrLf = "line1\r\nline2\r\nline3";
        const string contentWithLf = "line1\nline2\nline3";

        // Act
        var hashCrLf = CanonicalSerializer.GenerateHash(contentWithCrLf);
        var hashLf = CanonicalSerializer.GenerateHash(contentWithLf);

        // Assert - hashes should be identical after normalization
        Assert.Equal(hashLf, hashCrLf);
    }

    [Fact]
    public void SerializeToJson_ProducesDeterministicOutput()
    {
        // Arrange
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = "Test API",
                Version = "1.0.0",
                Description = "Test Description"
            },
            Paths = new OpenApiPaths()
        };

        // Act - serialize multiple times
        var json1 = CanonicalSerializer.SerializeToJson(document);
        var json2 = CanonicalSerializer.SerializeToJson(document);
        var hash1 = CanonicalSerializer.GenerateHash(json1);
        var hash2 = CanonicalSerializer.GenerateHash(json2);

        // Assert - output and hash should be identical
        Assert.Equal(json1, json2);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void SerializeToYaml_ProducesDeterministicOutput()
    {
        // Arrange
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = "Test API",
                Version = "1.0.0",
                Description = "Test Description"
            },
            Paths = new OpenApiPaths()
        };

        // Act - serialize multiple times
        var yaml1 = CanonicalSerializer.SerializeToYaml(document);
        var yaml2 = CanonicalSerializer.SerializeToYaml(document);
        var hash1 = CanonicalSerializer.GenerateHash(yaml1);
        var hash2 = CanonicalSerializer.GenerateHash(yaml2);

        // Assert - output and hash should be identical
        Assert.Equal(yaml1, yaml2);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void SerializeToJson_ThrowsOnNullDocument()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => CanonicalSerializer.SerializeToJson(null!));
    }

    [Fact]
    public void SerializeToYaml_ThrowsOnNullDocument()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => CanonicalSerializer.SerializeToYaml(null!));
    }

    [Fact]
    public void GenerateHash_ThrowsOnNullContent()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => CanonicalSerializer.GenerateHash(null!));
    }

    [Fact]
    public void GenerateETag_ThrowsOnNullHash()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => CanonicalSerializer.GenerateETag(null!));
    }
}
