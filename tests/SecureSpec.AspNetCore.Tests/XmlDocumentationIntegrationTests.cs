using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.Schema;

namespace SecureSpec.AspNetCore.Tests;

public class XmlDocumentationIntegrationTests
{
    [Fact]
    public void SchemaGenerator_ShouldApplyXmlDocumentationToTypeSchema()
    {
        // Arrange
        var xmlPath = CreateTempXmlFile(@"
<?xml version=""1.0""?>
<doc>
    <assembly><name>Test</name></assembly>
    <members>
        <member name=""T:SecureSpec.AspNetCore.Tests.TestModel"">
            <summary>A test model for documentation</summary>
        </member>
    </members>
</doc>");

        try
        {
            var options = new SchemaOptions();
            options.XmlDocumentationPaths.Add(xmlPath);
            var logger = new DiagnosticsLogger();
            var generator = new SchemaGenerator(options, logger);

            // Act
            var schema = generator.GenerateSchema(typeof(TestModel));

            // Assert
            Assert.NotNull(schema);
            Assert.Equal("A test model for documentation", schema.Description);
        }
        finally
        {
            File.Delete(xmlPath);
        }
    }

    [Fact]
    public void SchemaGenerator_ShouldHandleMultipleXmlFiles()
    {
        // Arrange
        var xmlPath1 = CreateTempXmlFile(@"
<?xml version=""1.0""?>
<doc>
    <assembly><name>Test1</name></assembly>
    <members>
        <member name=""T:SecureSpec.AspNetCore.Tests.TestModel"">
            <summary>First definition</summary>
        </member>
    </members>
</doc>");

        var xmlPath2 = CreateTempXmlFile(@"
<?xml version=""1.0""?>
<doc>
    <assembly><name>Test2</name></assembly>
    <members>
        <member name=""T:SecureSpec.AspNetCore.Tests.TestModel"">
            <summary>Second definition wins</summary>
        </member>
    </members>
</doc>");

        try
        {
            var options = new SchemaOptions();
            options.XmlDocumentationPaths.Add(xmlPath1);
            options.XmlDocumentationPaths.Add(xmlPath2);
            var logger = new DiagnosticsLogger();
            var generator = new SchemaGenerator(options, logger);

            // Act
            var schema = generator.GenerateSchema(typeof(TestModel));

            // Assert - last file wins
            Assert.NotNull(schema);
            Assert.Equal("Second definition wins", schema.Description);

            // Verify conflict was logged
            var events = logger.GetEvents();
            Assert.Contains(events, e => e.Code == "XML002");
        }
        finally
        {
            File.Delete(xmlPath1);
            File.Delete(xmlPath2);
        }
    }

    [Fact]
    public void SchemaGenerator_ShouldWorkWithoutXmlDocumentation()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(TestModel));

        // Assert - should still generate schema
        Assert.NotNull(schema);
        Assert.Null(schema.Description);
    }

    [Fact]
    public void XmlDocumentationPaths_ShouldAllowDynamicAddition()
    {
        // Arrange
        var options = new SchemaOptions();

        // Act
        options.XmlDocumentationPaths.Add("/path/to/file1.xml");
        options.XmlDocumentationPaths.Add("/path/to/file2.xml");

        // Assert
        Assert.Equal(2, options.XmlDocumentationPaths.Count);
        Assert.Contains("/path/to/file1.xml", options.XmlDocumentationPaths);
        Assert.Contains("/path/to/file2.xml", options.XmlDocumentationPaths);
    }

    private string CreateTempXmlFile(string content)
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, content.TrimStart());
        return tempFile;
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used via typeof() for reflection-based testing")]
internal sealed class TestModel
{
    public string? Name { get; set; }
    public int Age { get; set; }
}
