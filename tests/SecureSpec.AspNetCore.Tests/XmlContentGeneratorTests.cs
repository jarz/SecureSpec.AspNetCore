using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.MediaTypes;
using Xunit;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for AC 458: application/xml generation stable and mirrors JSON structure where representable.
/// </summary>
public class XmlContentGeneratorTests
{
    [Fact]
    public void CreateMediaType_ReturnsMediaTypeWithXmlSchema()
    {
        // Arrange
        var generator = new XmlContentGenerator();
        var schema = new OpenApiSchema { Type = "string" };

        // Act
        var mediaType = generator.CreateMediaType(schema);

        // Assert
        Assert.NotNull(mediaType);
        Assert.NotNull(mediaType.Schema);
        Assert.NotNull(mediaType.Schema.Xml);
        Assert.Equal("root", mediaType.Schema.Xml.Name);
    }

    [Fact]
    public void CreateMediaType_UsesCustomRootElementName()
    {
        // Arrange
        var generator = new XmlContentGenerator();
        var schema = new OpenApiSchema { Type = "string" };

        // Act
        var mediaType = generator.CreateMediaType(schema, "customRoot");

        // Assert
        Assert.Equal("customRoot", mediaType.Schema.Xml?.Name);
    }

    [Fact]
    public void CreateMediaType_MirrorsJsonStructureForObject()
    {
        // Arrange - AC 458: mirrors JSON structure
        var generator = new XmlContentGenerator();
        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["name"] = new OpenApiSchema { Type = "string" },
                ["age"] = new OpenApiSchema { Type = "integer" }
            }
        };

        // Act
        var mediaType = generator.CreateMediaType(schema);

        // Assert
        Assert.Equal("object", mediaType.Schema.Type);
        Assert.Equal(2, mediaType.Schema.Properties.Count);
        Assert.True(mediaType.Schema.Properties.ContainsKey("name"));
        Assert.True(mediaType.Schema.Properties.ContainsKey("age"));

        // Verify XML metadata is set on properties
        Assert.NotNull(mediaType.Schema.Properties["name"].Xml);
        Assert.Equal("name", mediaType.Schema.Properties["name"].Xml.Name);
    }

    [Fact]
    public void CreateMediaType_MirrorsJsonStructureForArray()
    {
        // Arrange
        var generator = new XmlContentGenerator();
        var schema = new OpenApiSchema
        {
            Type = "array",
            Items = new OpenApiSchema { Type = "string" }
        };

        // Act
        var mediaType = generator.CreateMediaType(schema, "items");

        // Assert
        Assert.Equal("array", mediaType.Schema.Type);
        Assert.NotNull(mediaType.Schema.Items);
        Assert.Equal("string", mediaType.Schema.Items.Type);

        // Verify array is wrapped
        Assert.NotNull(mediaType.Schema.Xml);
        Assert.True(mediaType.Schema.Xml.Wrapped);
    }

    [Fact]
    public void CreateMediaType_PreservesRequiredProperties()
    {
        // Arrange
        var generator = new XmlContentGenerator();
        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["id"] = new OpenApiSchema { Type = "integer" },
                ["name"] = new OpenApiSchema { Type = "string" }
            },
            Required = new HashSet<string> { "id", "name" }
        };

        // Act
        var mediaType = generator.CreateMediaType(schema);

        // Assert
        Assert.Equal(2, mediaType.Schema.Required.Count);
        Assert.Contains("id", mediaType.Schema.Required);
        Assert.Contains("name", mediaType.Schema.Required);
    }

    [Fact]
    public void CreateMediaType_MirrorsEnumValues()
    {
        // Arrange
        var generator = new XmlContentGenerator();
        var schema = new OpenApiSchema
        {
            Type = "string",
            Enum = new List<Microsoft.OpenApi.Any.IOpenApiAny>
            {
                new Microsoft.OpenApi.Any.OpenApiString("option1"),
                new Microsoft.OpenApi.Any.OpenApiString("option2"),
                new Microsoft.OpenApi.Any.OpenApiString("option3")
            }
        };

        // Act
        var mediaType = generator.CreateMediaType(schema);

        // Assert - AC 458: stable ordering
        Assert.Equal(3, mediaType.Schema.Enum.Count);
        Assert.Equal("option1", mediaType.Schema.Enum[0].ToString());
        Assert.Equal("option2", mediaType.Schema.Enum[1].ToString());
        Assert.Equal("option3", mediaType.Schema.Enum[2].ToString());
    }

    [Fact]
    public void CreateMediaType_PreservesSchemaReference()
    {
        // Arrange
        var generator = new XmlContentGenerator();
        var schema = new OpenApiSchema
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.Schema,
                Id = "User"
            }
        };

        // Act
        var mediaType = generator.CreateMediaType(schema);

        // Assert
        Assert.NotNull(mediaType.Schema.Reference);
        Assert.Equal("User", mediaType.Schema.Reference.Id);
        Assert.Equal(ReferenceType.Schema, mediaType.Schema.Reference.Type);
    }

    [Fact]
    public void GenerateXmlExample_CreatesStableOutput()
    {
        // Arrange - AC 458: stable generation
        var generator = new XmlContentGenerator();
        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["name"] = new OpenApiSchema { Type = "string" },
                ["age"] = new OpenApiSchema { Type = "integer" }
            }
        };

        // Act
        var xml1 = generator.GenerateXmlExample(schema);
        var xml2 = generator.GenerateXmlExample(schema);

        // Assert - stable across invocations
        Assert.Equal(xml1, xml2);
    }

    [Fact]
    public void GenerateXmlExample_SortsPropertiesLexically()
    {
        // Arrange
        var generator = new XmlContentGenerator();
        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["zebra"] = new OpenApiSchema { Type = "string" },
                ["alpha"] = new OpenApiSchema { Type = "string" },
                ["beta"] = new OpenApiSchema { Type = "string" }
            }
        };

        // Act
        var xml = generator.GenerateXmlExample(schema, "root");

        // Assert - properties should appear in lexical order
        Assert.NotNull(xml);
        var alphaIndex = xml!.IndexOf("<alpha>", StringComparison.Ordinal);
        var betaIndex = xml.IndexOf("<beta>", StringComparison.Ordinal);
        var zebraIndex = xml.IndexOf("<zebra>", StringComparison.Ordinal);

        Assert.True(alphaIndex < betaIndex);
        Assert.True(betaIndex < zebraIndex);
    }

    [Fact]
    public void GenerateXmlExample_EscapesXmlCharacters()
    {
        // Arrange
        var generator = new XmlContentGenerator();
        var schema = new OpenApiSchema
        {
            Type = "string",
            Example = new Microsoft.OpenApi.Any.OpenApiString("<tag> & \"quote\" 'apos'")
        };

        // Act
        var xml = generator.GenerateXmlExample(schema, "value");

        // Assert - special characters should be escaped
        Assert.NotNull(xml);
        Assert.Contains("&lt;tag&gt;", xml);
        Assert.Contains("&amp;", xml);
        Assert.Contains("&quot;", xml);
        Assert.Contains("&apos;", xml);
    }

    [Fact]
    public void GenerateXmlExample_SingularizesArrayItemNames()
    {
        // Arrange
        var generator = new XmlContentGenerator();
        var schema = new OpenApiSchema
        {
            Type = "array",
            Items = new OpenApiSchema { Type = "string" }
        };

        // Act
        var xml = generator.GenerateXmlExample(schema, "users");

        // Assert - "users" should become "user" for items
        Assert.NotNull(xml);
        Assert.Contains("<users>", xml);
        Assert.Contains("<user>", xml);
        Assert.Contains("</user>", xml);
        Assert.Contains("</users>", xml);
    }

    [Fact]
    public void GenerateXmlExample_HandlesIrregularPlurals()
    {
        // Arrange
        var generator = new XmlContentGenerator();
        var schema = new OpenApiSchema
        {
            Type = "array",
            Items = new OpenApiSchema { Type = "string" }
        };

        // Act - "categories" should become "category"
        var xml = generator.GenerateXmlExample(schema, "categories");

        // Assert
        Assert.NotNull(xml);
        Assert.Contains("<categories>", xml);
        Assert.Contains("<category>", xml);
    }

    [Fact]
    public void CreateMediaType_MirrorsOneOfPolymorphism()
    {
        // Arrange
        var generator = new XmlContentGenerator();
        var schema = new OpenApiSchema
        {
            OneOf = new List<OpenApiSchema>
            {
                new OpenApiSchema { Type = "string" },
                new OpenApiSchema { Type = "integer" }
            }
        };

        // Act
        var mediaType = generator.CreateMediaType(schema);

        // Assert
        Assert.Equal(2, mediaType.Schema.OneOf.Count);
        Assert.All(mediaType.Schema.OneOf, s => Assert.NotNull(s.Xml));
    }

    [Fact]
    public void CreateMediaType_ThrowsOnNullSchema()
    {
        // Arrange
        var generator = new XmlContentGenerator();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => generator.CreateMediaType(null!));
    }

    [Fact]
    public void GenerateXmlExample_ReturnsNullForNullType()
    {
        // Arrange
        var generator = new XmlContentGenerator();
        var schema = new OpenApiSchema(); // No type set

        // Act
        var xml = generator.GenerateXmlExample(schema);

        // Assert
        Assert.Null(xml);
    }
}
