using SecureSpec.AspNetCore.UI;

#pragma warning disable CA1307 // Specify StringComparison for clarity - not needed in tests

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for the asset provider.
/// </summary>
public class AssetProviderTests
{
    [Fact]
    public void GetAsset_WithValidStylesPath_ReturnsContent()
    {
        // Act
        var content = AssetProvider.GetAsset("assets/styles.css");

        // Assert
        Assert.NotNull(content);
        Assert.Contains("SecureSpec UI Styles", content);
    }

    [Fact]
    public void GetAsset_WithValidAppScriptPath_ReturnsContent()
    {
        // Act
        var content = AssetProvider.GetAsset("assets/app.js");

        // Assert
        Assert.NotNull(content);
        Assert.Contains("SecureSpec UI - Main Application", content);
    }

    [Fact]
    public void GetAsset_WithValidRouterPath_ReturnsContent()
    {
        // Act
        var content = AssetProvider.GetAsset("assets/components/router.js");

        // Assert
        Assert.NotNull(content);
        Assert.Contains("Router Component", content);
    }

    [Fact]
    public void GetAsset_WithValidStatePath_ReturnsContent()
    {
        // Act
        var content = AssetProvider.GetAsset("assets/components/state.js");

        // Assert
        Assert.NotNull(content);
        Assert.Contains("State Manager Component", content);
    }

    [Fact]
    public void GetAsset_WithValidOperationDisplayPath_ReturnsContent()
    {
        // Act
        var content = AssetProvider.GetAsset("assets/components/operation-display.js");

        // Assert
        Assert.NotNull(content);
        Assert.Contains("Operation Display Component", content);
    }

    [Fact]
    public void GetAsset_WithValidSchemaViewerPath_ReturnsContent()
    {
        // Act
        var content = AssetProvider.GetAsset("assets/components/schema-viewer.js");

        // Assert
        Assert.NotNull(content);
        Assert.Contains("Schema Viewer Component", content);
    }

    [Fact]
    public void GetAsset_WithNonExistentPath_ReturnsNull()
    {
        // Act
        var content = AssetProvider.GetAsset("assets/nonexistent.js");

        // Assert
        Assert.Null(content);
    }

    [Fact]
    public void GetAsset_WithBackslashPath_ReturnsContent()
    {
        // Act
        var content = AssetProvider.GetAsset(@"assets\styles.css");

        // Assert
        Assert.NotNull(content);
        Assert.Contains("SecureSpec UI Styles", content);
    }

    [Fact]
    public void GetAsset_WithUppercasePath_ReturnsContent()
    {
        // Act
        var content = AssetProvider.GetAsset("ASSETS/STYLES.CSS");

        // Assert
        Assert.NotNull(content);
        Assert.Contains("SecureSpec UI Styles", content);
    }

    [Fact]
    public void GetAsset_WithNullPath_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            AssetProvider.GetAsset(null!));
    }

    [Theory]
    [InlineData("assets/styles.css")]
    [InlineData("assets/app.js")]
    [InlineData("assets/components/router.js")]
    [InlineData("assets/components/state.js")]
    [InlineData("assets/components/operation-display.js")]
    [InlineData("assets/components/schema-viewer.js")]
    public void GetAsset_WithKnownAssets_ReturnsNonEmptyContent(string path)
    {
        // Act
        var content = AssetProvider.GetAsset(path);

        // Assert
        Assert.NotNull(content);
        Assert.NotEmpty(content);
    }
}
