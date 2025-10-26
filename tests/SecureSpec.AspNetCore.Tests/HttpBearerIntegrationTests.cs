using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Diagnostics;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Integration tests for HTTP Bearer security scheme registration and usage.
/// </summary>
public class HttpBearerIntegrationTests
{
    [Fact]
    public void SecuritySchemes_RegisteredViaAddHttpBearer_AppearInSchemeCollection()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddHttpBearer("bearerAuth", builder => builder
            .WithDescription("JWT Bearer authentication")
            .WithBearerFormat("JWT"));

        // Assert
        var schemes = options.Schemes;
        Assert.Single(schemes);
        Assert.True(schemes.ContainsKey("bearerAuth"));

        var scheme = schemes["bearerAuth"];
        Assert.Equal(SecuritySchemeType.Http, scheme.Type);
        Assert.Equal("bearer", scheme.Scheme);
        Assert.Equal("JWT", scheme.BearerFormat);
        Assert.Equal("JWT Bearer authentication", scheme.Description);
    }

    [Fact]
    public void MultipleSecuritySchemes_CanBeRegistered()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act - Register multiple schemes
        options.AddHttpBearer("jwtBearer", builder => builder
            .WithDescription("JWT tokens")
            .WithBearerFormat("JWT"));

        options.AddHttpBearer("opaqueBearer", builder => builder
            .WithDescription("Opaque tokens")
            .WithBearerFormat("Opaque"));

        // Assert
        Assert.Equal(2, options.Schemes.Count);
        Assert.True(options.Schemes.ContainsKey("jwtBearer"));
        Assert.True(options.Schemes.ContainsKey("opaqueBearer"));
        Assert.Equal("JWT", options.Schemes["jwtBearer"].BearerFormat);
        Assert.Equal("Opaque", options.Schemes["opaqueBearer"].BearerFormat);
    }

    [Fact]
    public void SecurityOptions_WithDiagnosticsLogger_TracksBasicAuthInferenceBlocking()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        var options = new SecurityOptions(logger);

        // Act
        options.BlockBasicAuthInference();

        // Assert
        var events = logger.GetEvents();
        Assert.Single(events);

        var evt = events[0];
        Assert.Equal("AUTH001", evt.Code);
        Assert.Equal(DiagnosticLevel.Warn, evt.Level);
        Assert.Contains("Basic auth inference blocked", evt.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void HttpBearerScheme_MinimalConfiguration_CreatesValidScheme()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act - Register with minimal configuration
        options.AddHttpBearer("minimalBearer");

        // Assert
        var scheme = options.Schemes["minimalBearer"];
        Assert.Equal(SecuritySchemeType.Http, scheme.Type);
        Assert.Equal("bearer", scheme.Scheme);
        Assert.Null(scheme.BearerFormat); // Optional
        Assert.Null(scheme.Description); // Optional
    }

    [Fact]
    public void HttpBearerScheme_WithCustomBearerFormat_StoresFormat()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddHttpBearer("customBearer", builder => builder
            .WithBearerFormat("CustomFormat"));

        // Assert
        Assert.Equal("CustomFormat", options.Schemes["customBearer"].BearerFormat);
    }

    [Fact]
    public void SecurityOptions_PreventsDuplicateSchemeNames_ByOverwriting()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act - Add same name twice
        options.AddHttpBearer("bearer", builder => builder.WithBearerFormat("JWT"));
        options.AddHttpBearer("bearer", builder => builder.WithBearerFormat("Opaque"));

        // Assert - Second registration overwrites first
        Assert.Single(options.Schemes);
        Assert.Equal("Opaque", options.Schemes["bearer"].BearerFormat);
    }

    [Fact]
    public void HttpBearerScheme_WithLongDescription_StoresCompleteDescription()
    {
        // Arrange
        var options = new SecurityOptions();
        const string longDescription = "This is a very long description that explains in detail " +
                             "how the bearer token authentication works in this API. " +
                             "It includes information about token format, expiration, " +
                             "and refresh mechanisms.";

        // Act
        options.AddHttpBearer("bearer", builder => builder
            .WithDescription(longDescription));

        // Assert
        Assert.Equal(longDescription, options.Schemes["bearer"].Description);
    }

    [Fact]
    public void SecuritySchemes_AreOrderedByRegistration()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddHttpBearer("first");
        options.AddHttpBearer("second");
        options.AddHttpBearer("third");

        // Assert - Keys should maintain insertion order in dictionary
        var keys = options.Schemes.Keys.ToList();
        Assert.Equal(3, keys.Count);
        Assert.Equal("first", keys[0]);
        Assert.Equal("second", keys[1]);
        Assert.Equal("third", keys[2]);
    }
}
