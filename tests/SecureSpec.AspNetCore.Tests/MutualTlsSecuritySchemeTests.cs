using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Security;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for Mutual TLS security scheme implementation (AC 214-216).
/// </summary>
public class MutualTlsSecuritySchemeTests
{
    #region AC 214-216: Mutual TLS Implementation

    [Fact]
    public void AddMutualTls_CreatesSchemeWithCorrectType()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddMutualTls("mutualTLS");

        // Assert
        var schemes = options.Schemes;
        Assert.Single(schemes);
        Assert.True(schemes.ContainsKey("mutualTLS"));
        // Using OpenIdConnect as placeholder until Microsoft.OpenApi supports mutualTLS natively
        Assert.Equal(SecuritySchemeType.OpenIdConnect, schemes["mutualTLS"].Type);
    }

    [Fact]
    public void AddMutualTls_AddsVendorExtension()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddMutualTls("mutualTLS");

        // Assert
        var scheme = options.Schemes["mutualTLS"];
        Assert.NotNull(scheme.Extensions);
        Assert.True(scheme.Extensions.ContainsKey("x-security-scheme-type"));

        var extension = scheme.Extensions["x-security-scheme-type"];
        Assert.NotNull(extension);

        // Verify the extension indicates this is a mutualTLS scheme
        var extensionValue = extension as Microsoft.OpenApi.Any.OpenApiString;
        Assert.NotNull(extensionValue);
        Assert.Equal("mutualTLS", extensionValue.Value);
    }

    [Fact]
    public void AddMutualTls_WithDescription_SetsDescription()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddMutualTls("mutualTLS", builder =>
            builder.WithDescription("Custom mutual TLS authentication for secure communication"));

        // Assert
        var scheme = options.Schemes["mutualTLS"];
        Assert.Equal("Custom mutual TLS authentication for secure communication", scheme.Description);
    }

    [Fact]
    public void AddMutualTls_WithoutDescription_SetsDefaultDescription()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddMutualTls("mutualTLS");

        // Assert
        var scheme = options.Schemes["mutualTLS"];
        Assert.NotNull(scheme.Description);
        Assert.Contains("Mutual TLS authentication", scheme.Description, StringComparison.Ordinal);
        Assert.Contains("externally", scheme.Description, StringComparison.Ordinal);
        Assert.Contains("not supported", scheme.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void AddMutualTls_WithoutConfiguration_CreatesMinimalScheme()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddMutualTls("mutualTLS");

        // Assert
        var scheme = options.Schemes["mutualTLS"];
        Assert.Equal(SecuritySchemeType.OpenIdConnect, scheme.Type);
        Assert.NotNull(scheme.Description);
        Assert.True(scheme.Extensions.ContainsKey("x-security-scheme-type"));
    }

    [Fact]
    public void AddMutualTls_WithMultipleSchemes_StoresAllSchemes()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act - Register multiple Mutual TLS schemes for different purposes
        options.AddMutualTls("clientMutualTLS", builder => builder
            .WithDescription("Client certificate authentication"));
        options.AddMutualTls("serviceMutualTLS", builder => builder
            .WithDescription("Service-to-service mTLS"));

        // Assert
        Assert.Equal(2, options.Schemes.Count);
        Assert.True(options.Schemes.ContainsKey("clientMutualTLS"));
        Assert.True(options.Schemes.ContainsKey("serviceMutualTLS"));
        Assert.Contains("Client certificate", options.Schemes["clientMutualTLS"].Description!, StringComparison.Ordinal);
        Assert.Contains("Service-to-service", options.Schemes["serviceMutualTLS"].Description!, StringComparison.Ordinal);
    }

    [Fact]
    public void AddMutualTls_WithNullName_ThrowsArgumentException()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => options.AddMutualTls(null!));
    }

    [Fact]
    public void AddMutualTls_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => options.AddMutualTls(""));
    }

    [Fact]
    public void AddMutualTls_WithWhitespaceName_ThrowsArgumentException()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => options.AddMutualTls("   "));
    }

    [Fact]
    public void WithDescription_WithNullDescription_ThrowsArgumentException()
    {
        // Arrange
        var builder = new MutualTlsSchemeBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithDescription(null!));
    }

    [Fact]
    public void WithDescription_WithEmptyDescription_ThrowsArgumentException()
    {
        // Arrange
        var builder = new MutualTlsSchemeBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithDescription(""));
    }

    [Fact]
    public void WithDescription_WithWhitespaceDescription_ThrowsArgumentException()
    {
        // Arrange
        var builder = new MutualTlsSchemeBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithDescription("   "));
    }

    #endregion

    #region Documentation and External Certificate Management

    [Fact]
    public void AddMutualTls_DefaultDescription_DocumentsExternalCertificateManagement()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddMutualTls("mutualTLS");

        // Assert
        var scheme = options.Schemes["mutualTLS"];
        Assert.NotNull(scheme.Description);

        // Verify documentation mentions external management
        Assert.Contains("externally", scheme.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TLS", scheme.Description, StringComparison.Ordinal);

        // Verify documentation states certificate upload is not supported
        Assert.Contains("not supported", scheme.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddMutualTls_Description_IndicatesNoCertificateUpload()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddMutualTls("mutualTLS");

        // Assert
        var scheme = options.Schemes["mutualTLS"];

        // Verify the description clearly states that certificate upload is not available
        Assert.Contains("Certificate upload is not supported", scheme.Description!, StringComparison.Ordinal);
    }

    [Fact]
    public void AddMutualTls_Description_IndicatesInfrastructureConfiguration()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddMutualTls("mutualTLS");

        // Assert
        var scheme = options.Schemes["mutualTLS"];

        // Verify the description mentions infrastructure configuration
        Assert.Contains("infrastructure", scheme.Description!, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Integration with Other Schemes

    [Fact]
    public void MutualTls_CanCoexist_WithHttpBearer()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddHttpBearer("bearerAuth", builder => builder.WithBearerFormat("JWT"));
        options.AddMutualTls("mutualTLS", builder => builder.WithDescription("mTLS for internal services"));

        // Assert
        Assert.Equal(2, options.Schemes.Count);
        Assert.True(options.Schemes.ContainsKey("bearerAuth"));
        Assert.True(options.Schemes.ContainsKey("mutualTLS"));
        Assert.Equal(SecuritySchemeType.Http, options.Schemes["bearerAuth"].Type);
        Assert.Equal(SecuritySchemeType.OpenIdConnect, options.Schemes["mutualTLS"].Type);
    }

    [Fact]
    public void MutualTls_OverwritesExisting_WhenSameNameUsed()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddMutualTls("auth", builder => builder.WithDescription("First description"));
        options.AddMutualTls("auth", builder => builder.WithDescription("Second description"));

        // Assert
        Assert.Single(options.Schemes);
        Assert.Equal("Second description", options.Schemes["auth"].Description);
    }

    #endregion

    #region Vendor Extension Verification

    [Fact]
    public void MutualTls_VendorExtension_HasCorrectKey()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddMutualTls("mutualTLS");

        // Assert
        var scheme = options.Schemes["mutualTLS"];
        Assert.True(scheme.Extensions.ContainsKey("x-security-scheme-type"));
    }

    [Fact]
    public void MutualTls_VendorExtension_HasCorrectValue()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddMutualTls("mutualTLS");

        // Assert
        var scheme = options.Schemes["mutualTLS"];
        var extension = scheme.Extensions["x-security-scheme-type"] as Microsoft.OpenApi.Any.OpenApiString;

        Assert.NotNull(extension);
        Assert.Equal("mutualTLS", extension.Value);
    }

    [Fact]
    public void MutualTls_ExtensionsCollection_IsNotNull()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddMutualTls("mutualTLS");

        // Assert
        var scheme = options.Schemes["mutualTLS"];
        Assert.NotNull(scheme.Extensions);
        Assert.NotEmpty(scheme.Extensions);
    }

    #endregion
}
