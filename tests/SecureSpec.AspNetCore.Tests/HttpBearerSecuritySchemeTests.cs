using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.Security;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for HTTP Bearer security scheme implementation (AC 189-195, AC 221).
/// </summary>
public class HttpBearerSecuritySchemeTests
{
    #region AC 189-195: HTTP Bearer Implementation

    [Fact]
    public void AddHttpBearer_CreatesSchemeWithCorrectType()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddHttpBearer("bearerAuth");

        // Assert
        var schemes = options.Schemes;
        Assert.Single(schemes);
        Assert.True(schemes.ContainsKey("bearerAuth"));
        Assert.Equal(SecuritySchemeType.Http, schemes["bearerAuth"].Type);
    }

    [Fact]
    public void AddHttpBearer_SetsSchemeToBearer()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddHttpBearer("bearerAuth");

        // Assert
        var scheme = options.Schemes["bearerAuth"];
        Assert.Equal("bearer", scheme.Scheme);
    }

    [Fact]
    public void AddHttpBearer_WithBearerFormat_SetsBearerFormat()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddHttpBearer("bearerAuth", builder =>
            builder.WithBearerFormat("JWT"));

        // Assert
        var scheme = options.Schemes["bearerAuth"];
        Assert.Equal("JWT", scheme.BearerFormat);
    }

    [Fact]
    public void AddHttpBearer_WithDescription_SetsDescription()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddHttpBearer("bearerAuth", builder =>
            builder.WithDescription("JWT Bearer token authentication"));

        // Assert
        var scheme = options.Schemes["bearerAuth"];
        Assert.Equal("JWT Bearer token authentication", scheme.Description);
    }

    [Fact]
    public void AddHttpBearer_WithoutConfiguration_CreatesMinimalScheme()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddHttpBearer("bearerAuth");

        // Assert
        var scheme = options.Schemes["bearerAuth"];
        Assert.Equal(SecuritySchemeType.Http, scheme.Type);
        Assert.Equal("bearer", scheme.Scheme);
        Assert.Null(scheme.BearerFormat);
        Assert.Null(scheme.Description);
    }

    [Fact]
    public void AddHttpBearer_WithFluentConfiguration_ChainsMethods()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddHttpBearer("bearerAuth", builder => builder
            .WithDescription("Bearer authentication")
            .WithBearerFormat("JWT"));

        // Assert
        var scheme = options.Schemes["bearerAuth"];
        Assert.Equal("Bearer authentication", scheme.Description);
        Assert.Equal("JWT", scheme.BearerFormat);
    }

    [Fact]
    public void AddHttpBearer_WithMultipleSchemes_StoresAllSchemes()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddHttpBearer("bearerAuth1", builder => builder.WithBearerFormat("JWT"));
        options.AddHttpBearer("bearerAuth2", builder => builder.WithBearerFormat("Opaque"));

        // Assert
        Assert.Equal(2, options.Schemes.Count);
        Assert.Equal("JWT", options.Schemes["bearerAuth1"].BearerFormat);
        Assert.Equal("Opaque", options.Schemes["bearerAuth2"].BearerFormat);
    }

    [Fact]
    public void AddHttpBearer_WithNullName_ThrowsArgumentException()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => options.AddHttpBearer(null!));
    }

    [Fact]
    public void AddHttpBearer_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => options.AddHttpBearer(""));
    }

    [Fact]
    public void WithBearerFormat_WithNullFormat_ThrowsArgumentException()
    {
        // Arrange
        var builder = new HttpBearerSchemeBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithBearerFormat(null!));
    }

    [Fact]
    public void WithDescription_WithNullDescription_ThrowsArgumentException()
    {
        // Arrange
        var builder = new HttpBearerSchemeBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithDescription(null!));
    }

    #endregion

    #region Header Sanitization Tests

    [Fact]
    public void SanitizeHeaderName_WithValidName_ReturnsUnchanged()
    {
        // Arrange
        var builder = new HttpBearerSchemeBuilder();
        const string validName = "Authorization";

        // Act
        var result = InvokeSanitizeHeaderName(validName);

        // Assert
        Assert.Equal("Authorization", result);
    }

    [Fact]
    public void SanitizeHeaderName_RemovesControlCharacters()
    {
        // Arrange
        const string nameWithControl = "Auth\r\nHeader";

        // Act
        var result = InvokeSanitizeHeaderName(nameWithControl);

        // Assert
        Assert.Equal("AuthHeader", result);
        Assert.DoesNotContain("\r", result, StringComparison.Ordinal);
        Assert.DoesNotContain("\n", result, StringComparison.Ordinal);
    }

    [Fact]
    public void SanitizeHeaderName_RemovesWhitespace()
    {
        // Arrange
        const string nameWithWhitespace = "Auth Header";

        // Act
        var result = InvokeSanitizeHeaderName(nameWithWhitespace);

        // Assert
        Assert.Equal("AuthHeader", result);
        Assert.DoesNotContain(" ", result, StringComparison.Ordinal);
    }

    [Fact]
    public void SanitizeHeaderName_NormalizesUnicode()
    {
        // Arrange - Using composed vs decomposed Unicode
        const string composed = "Authorizatión"; // Single character ó (U+00F3)
        const string decomposed = "Authorizatio\u0301n"; // o + combining acute accent (U+006F + U+0301)

        // Act
        var result1 = InvokeSanitizeHeaderName(composed);
        var result2 = InvokeSanitizeHeaderName(decomposed);

        // Assert - Both should normalize to the same form
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void SanitizeHeaderName_WithNullInput_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(
            () => InvokeSanitizeHeaderName(null!));
        Assert.IsType<ArgumentNullException>(exception.InnerException);
    }

    [Fact]
    public void SanitizeHeaderName_WithEmptyInput_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(
            () => InvokeSanitizeHeaderName(""));
        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    [Fact]
    public void SanitizeHeaderName_WithWhitespaceOnly_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(
            () => InvokeSanitizeHeaderName("   "));
        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    [Fact]
    public void SanitizeHeaderName_WithControlCharactersOnly_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(
            () => InvokeSanitizeHeaderName("\r\n\t"));
        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    #endregion

    #region AC 221: Basic Auth Inference Blocked (AUTH001)

    [Fact]
    public void BlockBasicAuthInference_EmitsAUTH001Diagnostic()
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
        Assert.Contains("Define security schemes explicitly", evt.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BlockBasicAuthInference_WithoutLogger_DoesNotThrow()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act & Assert - Should not throw
        options.BlockBasicAuthInference();
    }

    [Fact]
    public void BlockBasicAuthInference_IncludesContextInformation()
    {
        // Arrange
        var logger = new DiagnosticsLogger();
        var options = new SecurityOptions(logger);

        // Act
        options.BlockBasicAuthInference();

        // Assert
        var events = logger.GetEvents();
        var evt = events[0];
        Assert.NotNull(evt.Context);
    }

    #endregion

    #region Schemes Property Tests

    [Fact]
    public void Schemes_ReturnsReadOnlyDictionary()
    {
        // Arrange
        var options = new SecurityOptions();
        options.AddHttpBearer("bearerAuth");

        // Act
        var schemes = options.Schemes;

        // Assert
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, OpenApiSecurityScheme>>(schemes);
    }

    [Fact]
    public void Schemes_EmptyWhenNoSchemesAdded()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        var schemes = options.Schemes;

        // Assert
        Assert.Empty(schemes);
    }

    [Fact]
    public void Schemes_ReflectsAddedSchemes()
    {
        // Arrange
        var options = new SecurityOptions();

        // Act
        options.AddHttpBearer("scheme1");
        options.AddHttpBearer("scheme2");
        options.AddHttpBearer("scheme3");

        // Assert
        Assert.Equal(3, options.Schemes.Count);
        Assert.True(options.Schemes.ContainsKey("scheme1"));
        Assert.True(options.Schemes.ContainsKey("scheme2"));
        Assert.True(options.Schemes.ContainsKey("scheme3"));
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Uses reflection to invoke the protected SanitizeHeaderName method for testing.
    /// </summary>
    private static string InvokeSanitizeHeaderName(string name)
    {
        var method = typeof(SecuritySchemeBuilder).GetMethod(
            "SanitizeHeaderName",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        if (method == null)
        {
            throw new InvalidOperationException("SanitizeHeaderName method not found");
        }

        return (string)method.Invoke(null, new object[] { name })!;
    }

    #endregion
}
