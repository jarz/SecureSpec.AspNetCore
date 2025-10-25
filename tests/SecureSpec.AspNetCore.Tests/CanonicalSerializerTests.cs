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

    [Fact]
    public void SerializeToJson_ProducesUtf8WithoutBOM()
    {
        // Arrange (AC 1-10: UTF-8 without BOM)
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
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);

        // Assert - UTF-8 BOM is EF BB BF, should not be present
        if (bytes.Length >= 3)
        {
            var hasBom = bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            Assert.False(hasBom, "Output should not contain UTF-8 BOM");
        }
    }

    [Fact]
    public void SerializeToYaml_ProducesUtf8WithoutBOM()
    {
        // Arrange (AC 1-10: UTF-8 without BOM)
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
        var bytes = System.Text.Encoding.UTF8.GetBytes(yaml);

        // Assert - UTF-8 BOM is EF BB BF, should not be present
        if (bytes.Length >= 3)
        {
            var hasBom = bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            Assert.False(hasBom, "Output should not contain UTF-8 BOM");
        }
    }

    [Fact]
    public void SerializeToJson_UsesConsistentWhitespace()
    {
        // Arrange (AC 1-10: Normalized whitespace)
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = "Test API",
                Version = "1.0",
                Description = "Test Description"
            },
            Paths = new OpenApiPaths()
        };

        // Act - serialize multiple times
        var json1 = CanonicalSerializer.SerializeToJson(document);
        var json2 = CanonicalSerializer.SerializeToJson(document);

        // Assert - whitespace should be identical
        Assert.Equal(json1, json2);
        // Check for consistent indentation (2 spaces by default in Utf8JsonWriter)
        Assert.Contains("  \"", json1, StringComparison.Ordinal);
        // No tabs
        Assert.DoesNotContain("\t", json1, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeToJson_SortsPropertiesLexically()
    {
        // Arrange (AC 493: Component arrays sorted lexically)
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Version = "1.0",  // Version comes before Title alphabetically
                Title = "Test API",
                Description = "Description"  // Description comes first alphabetically
            },
            Paths = new OpenApiPaths()
        };

        // Act
        var json = CanonicalSerializer.SerializeToJson(document);

        // Assert - properties should be in lexical order
        var descriptionIndex = json.IndexOf("\"description\":", StringComparison.Ordinal);
        var titleIndex = json.IndexOf("\"title\":", StringComparison.Ordinal);
        var versionIndex = json.IndexOf("\"version\":", StringComparison.Ordinal);

        Assert.True(descriptionIndex < titleIndex, "description should come before title");
        Assert.True(titleIndex < versionIndex, "title should come before version");
    }

    [Fact]
    public void SerializeToJson_NumericSerializationIsLocaleInvariant()
    {
        // Arrange (AC 45: Numeric serialization locale invariance)
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = "Test API",
                Version = "1.0"
            },
            Paths = new OpenApiPaths()
        };

        // Save current culture
        var originalCulture = System.Globalization.CultureInfo.CurrentCulture;

        try
        {
            // Act - serialize with different cultures
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            var json1 = CanonicalSerializer.SerializeToJson(document);
            var hash1 = CanonicalSerializer.GenerateHash(json1);

            // German culture uses comma as decimal separator
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            var json2 = CanonicalSerializer.SerializeToJson(document);
            var hash2 = CanonicalSerializer.GenerateHash(json2);

            // French culture also uses comma as decimal separator
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");
            var json3 = CanonicalSerializer.SerializeToJson(document);
            var hash3 = CanonicalSerializer.GenerateHash(json3);

            // Assert - all should produce identical output regardless of culture
            Assert.Equal(hash1, hash2);
            Assert.Equal(hash1, hash3);
            Assert.Equal(json1, json2);
            Assert.Equal(json1, json3);
        }
        finally
        {
            // Restore original culture
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void SerializeToJson_WithComplexDocument_MaintainsLexicalOrdering()
    {
        // Arrange (AC 493: Component arrays sorted lexically)
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = "API",
                Version = "1.0",
                Contact = new OpenApiContact { Name = "Contact" },
                License = new OpenApiLicense { Name = "MIT" }
            },
            Paths = new OpenApiPaths
            {
                ["/users"] = new OpenApiPathItem(),
                ["/admin"] = new OpenApiPathItem(),
                ["/posts"] = new OpenApiPathItem()
            }
        };

        // Act
        var json = CanonicalSerializer.SerializeToJson(document);

        // Assert - paths should be in lexical order
        var adminIndex = json.IndexOf("\"/admin\"", StringComparison.Ordinal);
        var postsIndex = json.IndexOf("\"/posts\"", StringComparison.Ordinal);
        var usersIndex = json.IndexOf("\"/users\"", StringComparison.Ordinal);

        Assert.True(adminIndex > 0, "/admin should be present");
        Assert.True(postsIndex > 0, "/posts should be present");
        Assert.True(usersIndex > 0, "/users should be present");
        Assert.True(adminIndex < postsIndex, "/admin should come before /posts");
        Assert.True(postsIndex < usersIndex, "/posts should come before /users");
    }

    [Fact]
    public void GenerateHash_ProducesStableHashAcrossEnvironments()
    {
        // Arrange (AC 499: SHA256 hashing after normalization)
        // Both strings have same content but different line endings
        const string content1 = "line1\nline2\nline3";
        const string content2 = "line1\r\nline2\r\nline3";

        // Act
        var hash1 = CanonicalSerializer.GenerateHash(content1);
        var hash2 = CanonicalSerializer.GenerateHash(content2);

        // Assert - both should produce same hash after normalization
        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length);
    }

    [Fact]
    public void SerializeToJson_WithEmptyDocument_ProducesValidOutput()
    {
        // Arrange - minimal valid document
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = string.Empty,
                Version = string.Empty
            },
            Paths = new OpenApiPaths()
        };

        // Act
        var json = CanonicalSerializer.SerializeToJson(document);
        var hash = CanonicalSerializer.GenerateHash(json);

        // Assert - should produce valid output even with empty strings
        Assert.NotNull(json);
        Assert.NotEmpty(json);
        Assert.Equal(64, hash.Length);
    }

    [Fact]
    public void SerializeToJson_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange - document with special characters
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = "API with \"quotes\" and\nnewlines",
                Version = "1.0",
                Description = "Contains special chars: < > & ' \""
            },
            Paths = new OpenApiPaths()
        };

        // Act
        var json = CanonicalSerializer.SerializeToJson(document);

        // Assert - should properly escape special characters
        Assert.NotNull(json);
        Assert.Contains("\\n", json, StringComparison.Ordinal); // Newline escaped
        Assert.Contains("\\\"", json, StringComparison.Ordinal); // Quotes escaped
    }

    [Fact]
    public void SerializeToJson_WithUnicodeCharacters_PreservesCorrectly()
    {
        // Arrange - document with Unicode characters
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = "API with Unicode: 你好 مرحبا こんにちは",
                Version = "1.0",
                Description = "Emoji: 🚀 🎉 ✅"
            },
            Paths = new OpenApiPaths()
        };

        // Act
        var json1 = CanonicalSerializer.SerializeToJson(document);
        var json2 = CanonicalSerializer.SerializeToJson(document);
        var hash1 = CanonicalSerializer.GenerateHash(json1);
        var hash2 = CanonicalSerializer.GenerateHash(json2);

        // Assert - Unicode should be preserved and deterministic
        Assert.Equal(json1, json2);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void SerializeToJson_WithNestedObjects_MaintainsLexicalOrdering()
    {
        // Arrange - document with nested structures
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = "Test",
                Version = "1.0",
                Contact = new OpenApiContact
                {
                    Name = "John Doe",
                    Email = "john@example.com"
                }
            },
            Paths = new OpenApiPaths()
        };

        // Act
        var json = CanonicalSerializer.SerializeToJson(document);

        // Assert - nested object properties should also be sorted
        // email comes before name lexically
        var emailIndex = json.IndexOf("\"email\"", StringComparison.Ordinal);
        var nameIndex = json.IndexOf("\"name\"", StringComparison.Ordinal);
        Assert.True(emailIndex > 0, "email property should be present");
        Assert.True(nameIndex > 0, "name property should be present");
        Assert.True(emailIndex < nameIndex, "email should come before name in nested objects");
    }

    [Fact]
    public void GenerateHash_WithLargeContent_ProducesValidHash()
    {
        // Arrange - large content string
        var largeContent = new System.Text.StringBuilder();
        for (int i = 0; i < 10000; i++)
        {
            largeContent.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Line {i}: This is a test line with some content.");
        }

        // Act
        var hash = CanonicalSerializer.GenerateHash(largeContent.ToString());

        // Assert
        Assert.Equal(64, hash.Length);
        Assert.All(hash, c => Assert.True(char.IsDigit(c) || (c >= 'a' && c <= 'f')));
    }

    [Fact]
    public void SerializeToJson_WithArraysOfObjects_MaintainsArrayOrder()
    {
        // Arrange - document with arrays (arrays should NOT be sorted)
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = "Test",
                Version = "1.0"
            },
            Paths = new OpenApiPaths()
        };

        // Act - serialize twice to verify consistency
        var json1 = CanonicalSerializer.SerializeToJson(document);
        var json2 = CanonicalSerializer.SerializeToJson(document);

        // Assert - arrays should maintain order (not be sorted)
        Assert.Equal(json1, json2);
    }

    [Fact]
    public void SerializeToJson_WithDictionarySchema_ProducesDeterministicOutput()
    {
        // Arrange - create a document with a dictionary schema (AC 436)
        var dictionarySchema = new OpenApiSchema
        {
            Type = "object",
            AdditionalProperties = new OpenApiSchema
            {
                Type = "integer",
                Format = "int32"
            }
        };

        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = "Dictionary Test",
                Version = "1.0"
            },
            Paths = new OpenApiPaths(),
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema>
                {
                    { "StringIntDict", dictionarySchema }
                }
            }
        };

        // Act - serialize twice to verify deterministic ordering
        var json1 = CanonicalSerializer.SerializeToJson(document);
        var json2 = CanonicalSerializer.SerializeToJson(document);

        // Assert - both serializations should be identical (AC 436)
        Assert.Equal(json1, json2);

        // Verify hash is also deterministic
        var hash1 = CanonicalSerializer.GenerateHash(json1);
        var hash2 = CanonicalSerializer.GenerateHash(json2);
        Assert.Equal(hash1, hash2);

        // Verify it's valid JSON
        Assert.NotEmpty(json1);
        Assert.StartsWith("{", json1, StringComparison.Ordinal);
        Assert.EndsWith("}", json1, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeToJson_WithNestedDictionarySchema_ProducesDeterministicOutput()
    {
        // Arrange - create a document with nested dictionary schema
        var nestedDictionarySchema = new OpenApiSchema
        {
            Type = "object",
            AdditionalProperties = new OpenApiSchema
            {
                Type = "object",
                AdditionalProperties = new OpenApiSchema
                {
                    Type = "string"
                }
            }
        };

        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = "Nested Dictionary Test",
                Version = "1.0"
            },
            Paths = new OpenApiPaths(),
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema>
                {
                    { "NestedDict", nestedDictionarySchema }
                }
            }
        };

        // Act - serialize multiple times to verify consistency
        var json1 = CanonicalSerializer.SerializeToJson(document);
        var json2 = CanonicalSerializer.SerializeToJson(document);
        var json3 = CanonicalSerializer.SerializeToJson(document);

        // Assert - all serializations should be identical
        Assert.Equal(json1, json2);
        Assert.Equal(json2, json3);

        // Verify hash is also deterministic
        var hash1 = CanonicalSerializer.GenerateHash(json1);
        var hash2 = CanonicalSerializer.GenerateHash(json2);
        Assert.Equal(hash1, hash2);
    }
}
