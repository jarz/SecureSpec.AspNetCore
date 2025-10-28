using SecureSpec.AspNetCore.Configuration;

namespace SecureSpec.AspNetCore.Tests;

public class IntegrityOptionsTests
{
    [Fact]
    public void IntegrityOptions_DefaultValues_AreSecure()
    {
        // Arrange & Act
        var options = new IntegrityOptions();

        // Assert - Secure defaults
        Assert.True(options.Enabled);
        Assert.True(options.FailClosed);
        Assert.True(options.GenerateSri);
        Assert.Equal("sha256", options.Algorithm);
    }

    [Fact]
    public void IntegrityOptions_CanBeDisabled()
    {
        // Arrange
        var options = new IntegrityOptions();

        // Act
        options.Enabled = false;

        // Assert
        Assert.False(options.Enabled);
    }

    [Fact]
    public void IntegrityOptions_CanConfigureFailClosed()
    {
        // Arrange
        var options = new IntegrityOptions();

        // Act
        options.FailClosed = false;

        // Assert
        Assert.False(options.FailClosed);
    }

    [Fact]
    public void IntegrityOptions_CanConfigureGenerateSri()
    {
        // Arrange
        var options = new IntegrityOptions();

        // Act
        options.GenerateSri = false;

        // Assert
        Assert.False(options.GenerateSri);
    }

    [Fact]
    public void IntegrityOptions_AlgorithmIsReadOnly()
    {
        // Arrange
        var options = new IntegrityOptions();

        // Assert - Algorithm should be SHA256 only per PRD
        Assert.Equal("sha256", options.Algorithm);

        // Verify it's a property with only a getter
        var algorithmProperty = typeof(IntegrityOptions).GetProperty(nameof(IntegrityOptions.Algorithm));
        Assert.NotNull(algorithmProperty);
        Assert.Null(algorithmProperty.SetMethod);
    }

    [Fact]
    public void SecureSpecOptions_IncludesIntegrityOptions()
    {
        // Arrange
        var options = new SecureSpecOptions();

        // Assert
        Assert.NotNull(options.Integrity);
        Assert.IsType<IntegrityOptions>(options.Integrity);
    }

    [Fact]
    public void SecureSpecOptions_IntegrityOptions_HasSecureDefaults()
    {
        // Arrange
        var options = new SecureSpecOptions();

        // Assert
        Assert.True(options.Integrity.Enabled);
        Assert.True(options.Integrity.FailClosed);
        Assert.True(options.Integrity.GenerateSri);
    }
}
