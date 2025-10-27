using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.UI;

#pragma warning disable CA1307 // Specify StringComparison for clarity - not needed in tests

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for the UI template generator.
/// </summary>
public class UITemplateGeneratorTests
{
    [Fact]
    public void GenerateIndexHtml_WithDefaultOptions_GeneratesValidHtml()
    {
        // Arrange
        var options = new SecureSpecOptions();

        // Act
        var html = UITemplateGenerator.GenerateIndexHtml(options);

        // Assert
        Assert.NotNull(html);
        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("<html lang=\"en\">", html);
        Assert.Contains("SecureSpec API Documentation", html);
    }

    [Fact]
    public void GenerateIndexHtml_WithCustomTitle_IncludesTitle()
    {
        // Arrange
        var options = new SecureSpecOptions
        {
            UI = { DocumentTitle = "My Custom API" }
        };

        // Act
        var html = UITemplateGenerator.GenerateIndexHtml(options);

        // Assert
        Assert.Contains("My Custom API", html);
        Assert.Contains("<title>My Custom API</title>", html);
    }

    [Fact]
    public void GenerateIndexHtml_EscapesHtmlInTitle()
    {
        // Arrange
        var options = new SecureSpecOptions
        {
            UI = { DocumentTitle = "<script>alert('xss')</script>" }
        };

        // Act
        var html = UITemplateGenerator.GenerateIndexHtml(options);

        // Assert
        Assert.Contains("&lt;script&gt;", html);
        Assert.DoesNotContain("<script>alert", html);
    }

    [Fact]
    public void GenerateIndexHtml_IncludesConfiguration()
    {
        // Arrange
        var options = new SecureSpecOptions
        {
            UI =
            {
                DeepLinking = true,
                DisplayOperationId = true,
                DefaultModelsExpandDepth = 2,
                EnableFiltering = false,
                EnableTryItOut = false
            }
        };

        // Act
        var html = UITemplateGenerator.GenerateIndexHtml(options);

        // Assert
        Assert.Contains("\"deepLinking\": true", html);
        Assert.Contains("\"displayOperationId\": true", html);
        Assert.Contains("\"defaultModelsExpandDepth\": 2", html);
        Assert.Contains("\"enableFiltering\": false", html);
        Assert.Contains("\"enableTryItOut\": false", html);
    }

    [Fact]
    public void GenerateIndexHtml_IncludesRequiredAssets()
    {
        // Arrange
        var options = new SecureSpecOptions();

        // Act
        var html = UITemplateGenerator.GenerateIndexHtml(options);

        // Assert
        Assert.Contains("assets/styles.css", html);
        Assert.Contains("assets/app.js", html);
        Assert.Contains("type=\"module\"", html);
    }

    [Fact]
    public void GenerateIndexHtml_IncludesMainElements()
    {
        // Arrange
        var options = new SecureSpecOptions();

        // Act
        var html = UITemplateGenerator.GenerateIndexHtml(options);

        // Assert
        Assert.Contains("id=\"securespec-ui\"", html);
        Assert.Contains("id=\"navigation\"", html);
        Assert.Contains("id=\"content\"", html);
        Assert.Contains("id=\"ui-config\"", html);
    }

    [Fact]
    public void GenerateIndexHtml_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            UITemplateGenerator.GenerateIndexHtml(null!));
    }

    [Fact]
    public void GenerateIndexHtml_WithSpecialCharactersInTitle_EscapesCorrectly()
    {
        // Arrange
        var options = new SecureSpecOptions
        {
            UI = { DocumentTitle = "Test & \"API\" <v1.0>" }
        };

        // Act
        var html = UITemplateGenerator.GenerateIndexHtml(options);

        // Assert
        Assert.Contains("Test &amp; &quot;API&quot; &lt;v1.0&gt;", html);
    }
}
