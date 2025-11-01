using Microsoft.AspNetCore.Http;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.UI;

#pragma warning disable CA1307 // Specify StringComparison for clarity - not needed in tests

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for the OpenAPI Document middleware.
/// </summary>
public class OpenApiDocumentMiddlewareTests
{
    [Fact]
    public void Constructor_WithNullNext_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new SecureSpecOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OpenApiDocumentMiddleware(null!, options));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OpenApiDocumentMiddleware(next, null!));
    }

    [Fact]
    public async Task InvokeAsync_WithJsonDocument_ReturnsJson()
    {
        // Arrange
        var options = new SecureSpecOptions();
        options.Documents.Add("v1", doc =>
        {
            doc.Info.Title = "Test API";
            doc.Info.Version = "1.0.0";
        });

        var middleware = new OpenApiDocumentMiddleware(
            _ => Task.CompletedTask,
            options);

        var context = new DefaultHttpContext();
        context.Request.Path = "/openapi/v1.json";
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();

        Assert.Contains("Test API", json);
        Assert.Contains("openapi", json);
    }

    [Fact]
    public async Task InvokeAsync_WithYamlDocument_ReturnsYaml()
    {
        // Arrange
        var options = new SecureSpecOptions();
        options.Documents.Add("v1", doc =>
        {
            doc.Info.Title = "Test API";
            doc.Info.Version = "1.0.0";
        });

        var middleware = new OpenApiDocumentMiddleware(
            _ => Task.CompletedTask,
            options);

        var context = new DefaultHttpContext();
        context.Request.Path = "/openapi/v1.yaml";
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal("application/yaml; charset=utf-8", context.Response.ContentType);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var yaml = await reader.ReadToEndAsync();

        Assert.Contains("Test API", yaml);
        Assert.Contains("openapi", yaml);
    }

    [Fact]
    public async Task InvokeAsync_WithNonExistentDocument_Returns404()
    {
        // Arrange
        var options = new SecureSpecOptions();
        options.Documents.Add("v1", doc =>
        {
            doc.Info.Title = "Test API";
        });

        var middleware = new OpenApiDocumentMiddleware(
            _ => Task.CompletedTask,
            options);

        var context = new DefaultHttpContext();
        context.Request.Path = "/openapi/v2.json";
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(404, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var response = await reader.ReadToEndAsync();

        Assert.Contains("not found", response, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidFormat_CallsNextMiddleware()
    {
        // Arrange
        var options = new SecureSpecOptions();
        var nextCalled = false;

        var middleware = new OpenApiDocumentMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            options);

        var context = new DefaultHttpContext();
        context.Request.Path = "/openapi/v1.xml";
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WithNonOpenApiPath_CallsNextMiddleware()
    {
        // Arrange
        var options = new SecureSpecOptions();
        var nextCalled = false;

        var middleware = new OpenApiDocumentMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            options);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/values";
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_SetsCacheHeaders()
    {
        // Arrange
        var options = new SecureSpecOptions();
        options.Documents.Add("v1", doc =>
        {
            doc.Info.Title = "Test API";
        });

        var middleware = new OpenApiDocumentMiddleware(
            _ => Task.CompletedTask,
            options);

        var context = new DefaultHttpContext();
        context.Request.Path = "/openapi/v1.json";
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("no-cache, no-store, must-revalidate", context.Response.Headers["Cache-Control"]);
        Assert.Equal("no-cache", context.Response.Headers["Pragma"]);
        Assert.Equal("0", context.Response.Headers["Expires"]);
    }

    [Fact]
    public async Task InvokeAsync_DocumentContainsWeatherForecastExample()
    {
        // Arrange
        var options = new SecureSpecOptions();
        options.Documents.Add("v1", doc =>
        {
            doc.Info.Title = "Weather API";
            doc.Info.Description = "Weather forecast API";
        });

        var middleware = new OpenApiDocumentMiddleware(
            _ => Task.CompletedTask,
            options);

        var context = new DefaultHttpContext();
        context.Request.Path = "/openapi/v1.json";
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();

        Assert.Contains("weatherforecast", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WeatherForecast", json);
        Assert.Contains("Weather", json);
    }

    [Theory]
    [InlineData("v1")]
    [InlineData("v2")]
    [InlineData("api-v1")]
    public async Task InvokeAsync_SupportsMultipleDocuments(string documentName)
    {
        // Arrange
        var options = new SecureSpecOptions();
        options.Documents.Add(documentName, doc =>
        {
            doc.Info.Title = $"API {documentName}";
            doc.Info.Version = "1.0.0";
        });

        var middleware = new OpenApiDocumentMiddleware(
            _ => Task.CompletedTask,
            options);

        var context = new DefaultHttpContext();
        context.Request.Path = $"/openapi/{documentName}.json";
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(200, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();

        Assert.Contains($"API {documentName}", json);
    }

    [Fact]
    public async Task InvokeAsync_WithNullHttpContext_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new SecureSpecOptions();
        var middleware = new OpenApiDocumentMiddleware(_ => Task.CompletedTask, options);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            middleware.InvokeAsync(null!));
    }
}
