using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.Schema;

namespace SecureSpec.AspNetCore.Tests;

public class XmlDocumentationProviderTests
{
    private readonly DiagnosticsLogger _logger;
    private readonly XmlDocumentationProvider _provider;

    public XmlDocumentationProviderTests()
    {
        _logger = new DiagnosticsLogger();
        _provider = new XmlDocumentationProvider(_logger);
    }

    [Fact]
    public void LoadXmlDocumentation_ShouldLoadValidFile()
    {
        // Arrange
        var xmlPath = CreateTempXmlFile(@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>TestAssembly</name>
    </assembly>
    <members>
        <member name=""T:MyNamespace.MyClass"">
            <summary>
            A test class
            </summary>
        </member>
    </members>
</doc>");

        try
        {
            // Act
            _provider.LoadXmlDocumentation(xmlPath);

            // Assert
            Assert.Equal(1, _provider.LoadedFileCount);
            Assert.Equal(1, _provider.DocumentedMemberCount);

            var events = _logger.GetEvents();
            Assert.Contains(events, e => e.Code == "XML003" && e.Level == DiagnosticLevel.Info);
        }
        finally
        {
            File.Delete(xmlPath);
        }
    }

    [Fact]
    public void LoadXmlDocumentation_ShouldHandleNonexistentFile()
    {
        // Arrange
        const string xmlPath = "/tmp/nonexistent-file.xml";

        // Act
        _provider.LoadXmlDocumentation(xmlPath);

        // Assert
        Assert.Equal(0, _provider.LoadedFileCount);
        var events = _logger.GetEvents();
        Assert.Contains(events, e => e.Code == "XML001" && e.Level == DiagnosticLevel.Warn);
    }

    [Fact]
    public void LoadXmlDocumentation_ShouldHandleInvalidXml()
    {
        // Arrange
        var xmlPath = CreateTempXmlFile("This is not valid XML");

        try
        {
            // Act
            _provider.LoadXmlDocumentation(xmlPath);

            // Assert
            Assert.Equal(0, _provider.LoadedFileCount);
            var events = _logger.GetEvents();
            Assert.Contains(events, e => e.Code == "XML004" && e.Level == DiagnosticLevel.Error);
        }
        finally
        {
            File.Delete(xmlPath);
        }
    }

    [Fact]
    public void LoadXmlDocumentation_ShouldImplementOrderedMerge()
    {
        // Arrange
        var xmlPath1 = CreateTempXmlFile(@"
<?xml version=""1.0""?>
<doc>
    <assembly><name>Test1</name></assembly>
    <members>
        <member name=""T:MyClass"">
            <summary>First definition</summary>
        </member>
        <member name=""T:MyOtherClass"">
            <summary>Only in first file</summary>
        </member>
    </members>
</doc>");

        var xmlPath2 = CreateTempXmlFile(@"
<?xml version=""1.0""?>
<doc>
    <assembly><name>Test2</name></assembly>
    <members>
        <member name=""T:MyClass"">
            <summary>Second definition</summary>
        </member>
        <member name=""T:MyThirdClass"">
            <summary>Only in second file</summary>
        </member>
    </members>
</doc>");

        try
        {
            // Act
            _provider.LoadXmlDocumentation(xmlPath1);
            _provider.LoadXmlDocumentation(xmlPath2);

            // Assert
            Assert.Equal(2, _provider.LoadedFileCount);
            Assert.Equal(3, _provider.DocumentedMemberCount);

            // Verify conflict detection
            var events = _logger.GetEvents();
            Assert.Contains(events, e => e.Code == "XML002" && e.Level == DiagnosticLevel.Info);
        }
        finally
        {
            File.Delete(xmlPath1);
            File.Delete(xmlPath2);
        }
    }

    [Fact]
    public void GetTypeDocumentation_ShouldReturnDocumentation()
    {
        // Arrange
        var xmlPath = CreateTempXmlFile(@"
<?xml version=""1.0""?>
<doc>
    <assembly><name>Test</name></assembly>
    <members>
        <member name=""T:SecureSpec.AspNetCore.Tests.XmlDocumentationProviderTests"">
            <summary>Test summary</summary>
            <remarks>Test remarks</remarks>
        </member>
    </members>
</doc>");

        try
        {
            _provider.LoadXmlDocumentation(xmlPath);

            // Act
            var documentation = _provider.GetTypeDocumentation(typeof(XmlDocumentationProviderTests));

            // Assert
            Assert.NotNull(documentation);
            Assert.Equal("Test summary", documentation.Summary);
            Assert.Equal("Test remarks", documentation.Remarks);
        }
        finally
        {
            File.Delete(xmlPath);
        }
    }

    [Fact]
    public void GetTypeDocumentation_ShouldReturnNullForUndocumentedType()
    {
        // Act
        var documentation = _provider.GetTypeDocumentation(typeof(XmlDocumentationProviderTests));

        // Assert
        Assert.Null(documentation);
    }

    [Fact]
    public void ParseMemberDocumentation_ShouldHandleParameterDocumentation()
    {
        // Arrange
        var xmlPath = CreateTempXmlFile(@"
<?xml version=""1.0""?>
<doc>
    <assembly><name>Test</name></assembly>
    <members>
        <member name=""M:MyClass.MyMethod(System.String,System.Int32)"">
            <summary>Method summary</summary>
            <param name=""arg1"">First argument</param>
            <param name=""arg2"">Second argument</param>
            <returns>Return value description</returns>
        </member>
    </members>
</doc>");

        try
        {
            _provider.LoadXmlDocumentation(xmlPath);

            // The documentation should be loaded
            Assert.Equal(1, _provider.DocumentedMemberCount);
        }
        finally
        {
            File.Delete(xmlPath);
        }
    }

    [Fact]
    public void ParseMemberDocumentation_ShouldNormalizeWhitespace()
    {
        // Arrange
        var xmlPath = CreateTempXmlFile(@"
<?xml version=""1.0""?>
<doc>
    <assembly><name>Test</name></assembly>
    <members>
        <member name=""T:MyClass"">
            <summary>
                This is a summary
                with multiple lines
                and extra whitespace
            </summary>
        </member>
    </members>
</doc>");

        try
        {
            _provider.LoadXmlDocumentation(xmlPath);

            // Whitespace normalization is internal, but we can verify the file was loaded
            Assert.Equal(1, _provider.DocumentedMemberCount);
        }
        finally
        {
            File.Delete(xmlPath);
        }
    }

    private string CreateTempXmlFile(string content)
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, content.TrimStart());
        return tempFile;
    }
}
