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
}
