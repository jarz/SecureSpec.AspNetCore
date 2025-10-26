using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Integration tests for Mutual TLS security scheme registration and usage.
/// </summary>
public class MutualTlsIntegrationTests
{
    [Fact]
    public void MutualTls_RegisteredViaAddMutualTls_AppearsInSchemeCollection()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddMutualTls("mutualTLS", builder => builder
            .WithDescription("Mutual TLS authentication for API gateway"));

        // Assert
        var schemes = options.Schemes;
        Assert.Single(schemes);
        Assert.True(schemes.ContainsKey("mutualTLS"));

        var scheme = schemes["mutualTLS"];
        Assert.Equal(SecuritySchemeType.OpenIdConnect, scheme.Type);
        Assert.Equal("Mutual TLS authentication for API gateway", scheme.Description);
        Assert.True(scheme.Extensions.ContainsKey("x-security-scheme-type"));
    }

    [Fact]
    public void MultipleMutualTlsSchemes_CanBeRegistered()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act - Register multiple mTLS schemes for different purposes
        options.AddMutualTls("clientAuth", builder => builder
            .WithDescription("Client certificate authentication"));

        options.AddMutualTls("serviceAuth", builder => builder
            .WithDescription("Service-to-service authentication"));

        // Assert
        Assert.Equal(2, options.Schemes.Count);
        Assert.True(options.Schemes.ContainsKey("clientAuth"));
        Assert.True(options.Schemes.ContainsKey("serviceAuth"));
        Assert.Contains("Client certificate", options.Schemes["clientAuth"].Description!, StringComparison.Ordinal);
        Assert.Contains("Service-to-service", options.Schemes["serviceAuth"].Description!, StringComparison.Ordinal);
    }

    [Fact]
    public void MutualTls_MinimalConfiguration_CreatesValidScheme()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act - Register with minimal configuration
        options.AddMutualTls("minimalMutualTLS");

        // Assert
        var scheme = options.Schemes["minimalMutualTLS"];
        Assert.Equal(SecuritySchemeType.OpenIdConnect, scheme.Type);
        Assert.NotNull(scheme.Description);
        Assert.Contains("Mutual TLS", scheme.Description, StringComparison.Ordinal);
        Assert.True(scheme.Extensions.ContainsKey("x-security-scheme-type"));

        var extension = scheme.Extensions["x-security-scheme-type"] as Microsoft.OpenApi.Any.OpenApiString;
        Assert.NotNull(extension);
        Assert.Equal("mutualTLS", extension.Value);
    }

    [Fact]
    public void MutualTls_WithCustomDescription_StoresDescription()
    {
        // Arrange
        var options = new SecurityOptions();
        const string customDescription = "This API requires client certificates issued by the corporate CA. " +
                                        "Contact IT security to obtain a valid certificate.";

        // Act
        options.AddMutualTls("mutualTLS", builder => builder
            .WithDescription(customDescription));

        // Assert
        Assert.Equal(customDescription, options.Schemes["mutualTLS"].Description);
    }

    [Fact]
    public void MutualTls_PreventsDuplicateSchemeNames_ByOverwriting()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act - Add same name twice
        options.AddMutualTls("mutualTLS", builder => builder
            .WithDescription("First description"));
        options.AddMutualTls("mutualTLS", builder => builder
            .WithDescription("Second description"));

        // Assert - Second registration overwrites first
        Assert.Single(options.Schemes);
        Assert.Equal("Second description", options.Schemes["mutualTLS"].Description);
    }

    [Fact]
    public void MutualTls_WithLongDescription_StoresCompleteDescription()
    {
        // Arrange
        var options = new SecurityOptions();
        const string longDescription = "This is a comprehensive description of the Mutual TLS authentication scheme. " +
                             "It requires client certificates to be installed on the client machine and configured " +
                             "at the infrastructure level (API Gateway, Load Balancer, or web server). " +
                             "The certificate must be issued by a trusted Certificate Authority (CA). " +
                             "This library provides documentation only; actual certificate management is external.";

        // Act
        options.AddMutualTls("mutualTLS", builder => builder
            .WithDescription(longDescription));

        // Assert
        Assert.Equal(longDescription, options.Schemes["mutualTLS"].Description);
    }

    [Fact]
    public void MutualTlsSchemes_AreOrderedByRegistration()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddMutualTls("first");
        options.AddMutualTls("second");
        options.AddMutualTls("third");

        // Assert - Keys should maintain insertion order in dictionary
        var keys = options.Schemes.Keys.ToList();
        Assert.Equal(3, keys.Count);
        Assert.Equal("first", keys[0]);
        Assert.Equal("second", keys[1]);
        Assert.Equal("third", keys[2]);
    }

    [Fact]
    public void MutualTls_MixedWithOtherSchemes_MaintainsAllSchemes()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act - Mix different security scheme types
        options.AddHttpBearer("bearerAuth", builder => builder.WithBearerFormat("JWT"));
        options.AddMutualTls("mutualTLS", builder => builder.WithDescription("mTLS authentication"));
        options.AddHttpBearer("apiKeyAuth", builder => builder.WithDescription("API key authentication"));

        // Assert
        Assert.Equal(3, options.Schemes.Count);
        Assert.Equal(SecuritySchemeType.Http, options.Schemes["bearerAuth"].Type);
        Assert.Equal(SecuritySchemeType.OpenIdConnect, options.Schemes["mutualTLS"].Type);
        Assert.Equal(SecuritySchemeType.Http, options.Schemes["apiKeyAuth"].Type);
    }

    [Fact]
    public void MutualTls_VendorExtension_IndicatesRealType()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddMutualTls("mutualTLS");

        // Assert
        var scheme = options.Schemes["mutualTLS"];

        // The scheme should use OpenIdConnect as placeholder
        Assert.Equal(SecuritySchemeType.OpenIdConnect, scheme.Type);

        // But the vendor extension should indicate it's actually mutualTLS
        var extension = scheme.Extensions["x-security-scheme-type"] as Microsoft.OpenApi.Any.OpenApiString;
        Assert.NotNull(extension);
        Assert.Equal("mutualTLS", extension.Value);
    }

    [Fact]
    public void MutualTls_DefaultDescription_ProvidesClearGuidance()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddMutualTls("mutualTLS");

        // Assert
        var description = options.Schemes["mutualTLS"].Description;
        Assert.NotNull(description);

        // Should mention it's Mutual TLS
        Assert.Contains("Mutual TLS", description, StringComparison.Ordinal);

        // Should indicate certificates are external
        Assert.Contains("externally", description, StringComparison.OrdinalIgnoreCase);

        // Should mention TLS layer
        Assert.Contains("TLS", description, StringComparison.Ordinal);

        // Should state upload not supported
        Assert.Contains("not supported", description, StringComparison.OrdinalIgnoreCase);

        // Should mention infrastructure
        Assert.Contains("infrastructure", description, StringComparison.OrdinalIgnoreCase);
    }
}
