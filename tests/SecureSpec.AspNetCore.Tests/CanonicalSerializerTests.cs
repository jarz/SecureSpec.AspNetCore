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
        var serializer = new CanonicalSerializer();
        var content = "test content";

        // Act
        var hash = serializer.GenerateHash(content);

        // Assert
        Assert.NotNull(hash);
        Assert.Equal(64, hash.Length); // SHA256 produces 64 hex characters
        Assert.All(hash, c => Assert.True(char.IsDigit(c) || (c >= 'a' && c <= 'f')));
    }

    [Fact]
    public void GenerateETag_WithValidHash_ReturnsCorrectFormat()
    {
        // Arrange
        var serializer = new CanonicalSerializer();
        var hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

        // Act
        var etag = serializer.GenerateETag(hash);

        // Assert
        Assert.Equal("W/\"sha256:0123456789abcdef\"", etag);
    }

    [Fact]
    public void GenerateETag_WithShortHash_ThrowsArgumentException()
    {
        // Arrange
        var serializer = new CanonicalSerializer();
        var shortHash = "tooshort";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => serializer.GenerateETag(shortHash));
    }

    [Fact]
    public void SerializeToJson_WithSimpleDocument_ReturnsValidJson()
    {
        // Arrange
        var serializer = new CanonicalSerializer();
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
        var json = serializer.SerializeToJson(document);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\"openapi\":", json);
        Assert.Contains("\"info\":", json);
        Assert.Contains("\"Test API\"", json);
        Assert.Contains("\"1.0.0\"", json);
    }

    [Fact]
    public void SerializeToJson_UsesLfLineEndings()
    {
        // Arrange (AC 499)
        var serializer = new CanonicalSerializer();
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
        var json = serializer.SerializeToJson(document);

        // Assert
        Assert.DoesNotContain("\r\n", json); // No CRLF
        Assert.Contains("\n", json); // Has LF
    }

    [Fact]
    public void SerializeToYaml_WithSimpleDocument_ReturnsValidYaml()
    {
        // Arrange
        var serializer = new CanonicalSerializer();
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
        var yaml = serializer.SerializeToYaml(document);

        // Assert
        Assert.NotNull(yaml);
        Assert.Contains("openapi:", yaml);
        Assert.Contains("info:", yaml);
        Assert.Contains("Test API", yaml);
        Assert.Contains("1.0.0", yaml);
    }

    [Fact]
    public void SerializeToYaml_UsesLfLineEndings()
    {
        // Arrange
        var serializer = new CanonicalSerializer();
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
        var yaml = serializer.SerializeToYaml(document);

        // Assert
        Assert.DoesNotContain("\r\n", yaml); // No CRLF
        Assert.Contains("\n", yaml); // Has LF
    }

    [Fact]
    public void GenerateHash_NormalizesCrLfToLf()
    {
        // Arrange (AC 499)
        var serializer = new CanonicalSerializer();
        var contentWithCrLf = "line1\r\nline2\r\nline3";
        var contentWithLf = "line1\nline2\nline3";

        // Act
        var hashCrLf = serializer.GenerateHash(contentWithCrLf);
        var hashLf = serializer.GenerateHash(contentWithLf);

        // Assert - hashes should be identical after normalization
        Assert.Equal(hashLf, hashCrLf);
    }

    [Fact]
    public void SerializeToJson_ProducesDeterministicOutput()
    {
        // Arrange
        var serializer = new CanonicalSerializer();
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
        var json1 = serializer.SerializeToJson(document);
        var json2 = serializer.SerializeToJson(document);
        var hash1 = serializer.GenerateHash(json1);
        var hash2 = serializer.GenerateHash(json2);

        // Assert - output and hash should be identical
        Assert.Equal(json1, json2);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void SerializeToYaml_ProducesDeterministicOutput()
    {
        // Arrange
        var serializer = new CanonicalSerializer();
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
        var yaml1 = serializer.SerializeToYaml(document);
        var yaml2 = serializer.SerializeToYaml(document);
        var hash1 = serializer.GenerateHash(yaml1);
        var hash2 = serializer.GenerateHash(yaml2);

        // Assert - output and hash should be identical
        Assert.Equal(yaml1, yaml2);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void SerializeToJson_ThrowsOnNullDocument()
    {
        // Arrange
        var serializer = new CanonicalSerializer();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => serializer.SerializeToJson(null!));
    }

    [Fact]
    public void SerializeToYaml_ThrowsOnNullDocument()
    {
        // Arrange
        var serializer = new CanonicalSerializer();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => serializer.SerializeToYaml(null!));
    }

    [Fact]
    public void GenerateHash_ThrowsOnNullContent()
    {
        // Arrange
        var serializer = new CanonicalSerializer();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => serializer.GenerateHash(null!));
    }

    [Fact]
    public void GenerateETag_ThrowsOnNullHash()
    {
        // Arrange
        var serializer = new CanonicalSerializer();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => serializer.GenerateETag(null!));
    }
}
