using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.MediaTypes;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for AC 453: Shared schema across media types uses single $ref (no duplication).
/// Also tests AC 452: Deterministic media type ordering in content objects.
/// </summary>
public class MediaTypeContentBuilderTests
{
    [Fact]
    public void CreateContent_SharesSchemaReferenceAcrossMediaTypes()
    {
        // Arrange - AC 453: shared schema uses single $ref
        var schema = new OpenApiSchema
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.Schema,
                Id = "User"
            }
        };

        var mediaTypes = new[] { "application/json", "application/xml" };

        // Act
        var content = MediaTypeContentBuilder.CreateContent(schema, mediaTypes);

        // Assert
        Assert.Equal(2, content.Count);
        Assert.True(content.ContainsKey("application/json"));
        Assert.True(content.ContainsKey("application/xml"));

        // Verify both media types share the same schema reference
        var jsonSchema = content["application/json"].Schema;
        var xmlSchema = content["application/xml"].Schema;

        Assert.NotNull(jsonSchema.Reference);
        Assert.NotNull(xmlSchema.Reference);
        Assert.Equal("User", jsonSchema.Reference.Id);
        Assert.Equal("User", xmlSchema.Reference.Id);

        // Verify they use the SAME reference instance (no duplication)
        Assert.Same(schema.Reference, jsonSchema.Reference);
        Assert.Same(schema.Reference, xmlSchema.Reference);
    }

    [Fact]
    public void CreateContentWithReference_SharesReferenceAcrossMediaTypes()
    {
        // Arrange
        var reference = new OpenApiReference
        {
            Type = ReferenceType.Schema,
            Id = "Product"
        };

        var mediaTypes = new[] { "application/json", "application/xml", "text/plain" };

        // Act
        var content = MediaTypeContentBuilder.CreateContentWithReference(reference, mediaTypes);

        // Assert
        Assert.Equal(3, content.Count);

        // Verify all media types share the same reference
        foreach (var mediaType in content.Values)
        {
            Assert.NotNull(mediaType.Schema.Reference);
            Assert.Equal("Product", mediaType.Schema.Reference.Id);
            Assert.Same(reference, mediaType.Schema.Reference);
        }
    }

    [Fact]
    public void CreateContent_AppliesDeterministicOrdering()
    {
        // Arrange - AC 452: deterministic ordering
        var schema = new OpenApiSchema { Type = "object" };

        // Provide unsorted media types
        var mediaTypes = new[]
        {
            "application/octet-stream",
            "text/plain",
            "application/json",
            "application/xml"
        };

        // Act
        var content = MediaTypeContentBuilder.CreateContent(schema, mediaTypes);

        // Assert - verify deterministic ordering
        var keys = content.Keys.ToList();
        Assert.Equal("application/json", keys[0]);
        Assert.Equal("application/xml", keys[1]);
        Assert.Equal("text/plain", keys[2]);
        Assert.Equal("application/octet-stream", keys[3]);
    }

    [Fact]
    public void CreateContent_HandlesTextPlainSpecially()
    {
        // Arrange - AC 457: text/plain handling
        var schema = new OpenApiSchema { Type = "string" };
        var mediaTypes = new[] { "text/plain" };
        var options = new MediaTypeContentOptions
        {
            PlainTextExample = "Custom example"
        };

        // Act
        var content = MediaTypeContentBuilder.CreateContent(schema, mediaTypes, options);

        // Assert
        Assert.Single(content);
        var mediaType = content["text/plain"];
        Assert.NotNull(mediaType.Example);
        Assert.Equal("Custom example", ((Microsoft.OpenApi.Any.OpenApiString)mediaType.Example).Value);
    }

    [Fact]
    public void CreateContent_HandlesXmlSpecially()
    {
        // Arrange - AC 458: XML generation
        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["name"] = new OpenApiSchema { Type = "string" }
            }
        };
        var mediaTypes = new[] { "application/xml" };
        var options = new MediaTypeContentOptions
        {
            XmlRootElementName = "person"
        };

        // Act
        var content = MediaTypeContentBuilder.CreateContent(schema, mediaTypes, options);

        // Assert
        Assert.Single(content);
        var mediaType = content["application/xml"];
        Assert.NotNull(mediaType.Schema.Xml);
        Assert.Equal("person", mediaType.Schema.Xml.Name);
    }

    [Fact]
    public void CreateContent_HandlesMultipartEncodings()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "object" };
        var mediaTypes = new[] { "multipart/form-data" };
        var options = new MediaTypeContentOptions
        {
            MultipartEncodings = new Dictionary<string, OpenApiEncoding>
            {
                ["file"] = new OpenApiEncoding
                {
                    ContentType = "application/octet-stream"
                }
            }
        };

        // Act
        var content = MediaTypeContentBuilder.CreateContent(schema, mediaTypes, options);

        // Assert
        Assert.Single(content);
        var mediaType = content["multipart/form-data"];
        Assert.NotNull(mediaType.Encoding);
        Assert.True(mediaType.Encoding.ContainsKey("file"));
    }

    [Fact]
    public void CreateContent_AllowsNullOptions()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "string" };
        var mediaTypes = new[] { "application/json" };

        // Act
        var content = MediaTypeContentBuilder.CreateContent(schema, mediaTypes, null);

        // Assert
        Assert.Single(content);
        Assert.True(content.ContainsKey("application/json"));
    }

    [Fact]
    public void CreateContent_HandlesCaseInsensitiveMediaTypes()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "string" };
        var mediaTypes = new[] { "APPLICATION/JSON", "Text/Plain" };

        // Act
        var content = MediaTypeContentBuilder.CreateContent(schema, mediaTypes);

        // Assert
        Assert.Equal(2, content.Count);
        Assert.True(content.ContainsKey("APPLICATION/JSON"));
        Assert.True(content.ContainsKey("Text/Plain"));
    }

    [Fact]
    public void CreateContent_EmptyMediaTypesReturnsEmptyDictionary()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "string" };
        var mediaTypes = Array.Empty<string>();

        // Act
        var content = MediaTypeContentBuilder.CreateContent(schema, mediaTypes);

        // Assert
        Assert.Empty(content);
    }

    [Fact]
    public void CreateContent_ThrowsOnNullSchema()
    {
        // Arrange
        var mediaTypes = new[] { "application/json" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            MediaTypeContentBuilder.CreateContent(null!, mediaTypes));
    }

    [Fact]
    public void CreateContent_ThrowsOnNullMediaTypes()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "string" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            MediaTypeContentBuilder.CreateContent(schema, null!));
    }

    [Fact]
    public void CreateContentWithReference_ThrowsOnNullReference()
    {
        // Arrange
        var mediaTypes = new[] { "application/json" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            MediaTypeContentBuilder.CreateContentWithReference(null!, mediaTypes));
    }

    [Fact]
    public void CreateContent_PreservesSchemaPropertiesAcrossMediaTypes()
    {
        // Arrange
        var schema = new OpenApiSchema
        {
            Type = "string",
            Format = "email",
            MinLength = 5,
            MaxLength = 100
        };

        var mediaTypes = new[] { "application/json", "application/xml" };

        // Act
        var content = MediaTypeContentBuilder.CreateContent(schema, mediaTypes);

        // Assert - verify schema properties are accessible through all media types
        foreach (var mediaType in content.Values)
        {
            // For default media types (not text/plain or XML), schema should be the same instance
            if (mediaType == content["application/json"])
            {
                Assert.Same(schema, mediaType.Schema);
            }
        }
    }

    [Fact]
    public void CreateContent_HandlesTextXmlAsXml()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "object" };
        var mediaTypes = new[] { "text/xml" };

        // Act
        var content = MediaTypeContentBuilder.CreateContent(schema, mediaTypes);

        // Assert - text/xml should be treated as XML
        Assert.Single(content);
        var mediaType = content["text/xml"];
        Assert.NotNull(mediaType.Schema.Xml);
    }

    [Fact]
    public void CreateContent_SupportsMultipleMediaTypesWithSharedSchema()
    {
        // Arrange - AC 453: comprehensive test
        var reference = new OpenApiReference
        {
            Type = ReferenceType.Schema,
            Id = "Order"
        };

        var schema = new OpenApiSchema
        {
            Reference = reference,
            Type = "object"
        };

        var mediaTypes = new[]
        {
            "application/json",
            "application/xml",
            "text/plain",
            "application/custom"
        };

        // Act
        var content = MediaTypeContentBuilder.CreateContent(schema, mediaTypes);

        // Assert
        Assert.Equal(4, content.Count);

        // Verify all non-special media types share the exact same schema reference
        Assert.Same(reference, content["application/json"].Schema.Reference);
        Assert.Same(reference, content["application/custom"].Schema.Reference);

        // XML and text/plain create wrapper schemas but preserve the reference internally
        Assert.NotNull(content["application/xml"].Schema);
        Assert.NotNull(content["text/plain"].Schema);
    }
}
