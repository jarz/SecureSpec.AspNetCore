using Microsoft.AspNetCore.Http;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.UI;

#pragma warning disable CA1307 // Specify StringComparison for clarity - not needed in tests

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for the SecureSpec UI middleware.
/// </summary>
public class SecureSpecUIMiddlewareTests
{
    [Fact]
    public void Constructor_WithNullNext_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new SecureSpecOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SecureSpecUIMiddleware(null!, options));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SecureSpecUIMiddleware(next, null!));
    }

    [Fact]
    public void Constructor_WithNullRoutePrefix_ThrowsArgumentNullException()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var options = new SecureSpecOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SecureSpecUIMiddleware(next, options, null!));
    }

    [Fact]
    public async Task InvokeAsync_WithIndexPath_ReturnsHtml()
    {
        // Arrange
        var options = new SecureSpecOptions
        {
            UI = { DocumentTitle = "Test API" }
        };

        var middleware = new SecureSpecUIMiddleware(
            _ => Task.CompletedTask,
            options,
            "securespec");

        var context = new DefaultHttpContext();
        context.Request.Path = "/securespec";
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal("text/html; charset=utf-8", context.Response.ContentType);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var html = await reader.ReadToEndAsync();

        Assert.Contains("Test API", html);
        Assert.Contains("<!DOCTYPE html>", html);
    }

    [Fact]
    public async Task InvokeAsync_WithIndexHtmlPath_ReturnsHtml()
    {
        // Arrange
        var options = new SecureSpecOptions();
        var middleware = new SecureSpecUIMiddleware(
            _ => Task.CompletedTask,
            options,
            "securespec");

        var context = new DefaultHttpContext();
        context.Request.Path = "/securespec/index.html";
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal("text/html; charset=utf-8", context.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_WithAssetPath_ReturnsAsset()
    {
        // Arrange
        var options = new SecureSpecOptions();
        var middleware = new SecureSpecUIMiddleware(
            _ => Task.CompletedTask,
            options,
            "securespec");

        var context = new DefaultHttpContext();
        context.Request.Path = "/securespec/assets/styles.css";
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal("text/css; charset=utf-8", context.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_WithNonUIPath_CallsNextMiddleware()
    {
        // Arrange
        var options = new SecureSpecOptions();
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new SecureSpecUIMiddleware(next, options, "securespec");

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/values";
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WithNullHttpContext_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new SecureSpecOptions();
        var middleware = new SecureSpecUIMiddleware(
            _ => Task.CompletedTask,
            options);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            middleware.InvokeAsync(null!));
    }

    [Fact]
    public async Task InvokeAsync_SetsSecurityHeaders()
    {
        // Arrange
        var options = new SecureSpecOptions();
        var middleware = new SecureSpecUIMiddleware(
            _ => Task.CompletedTask,
            options,
            "securespec");

        var context = new DefaultHttpContext();
        context.Request.Path = "/securespec";
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("Content-Security-Policy"));
        Assert.True(context.Response.Headers.ContainsKey("X-Content-Type-Options"));
        Assert.True(context.Response.Headers.ContainsKey("X-Frame-Options"));
        Assert.True(context.Response.Headers.ContainsKey("Referrer-Policy"));

        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"]);
        Assert.Equal("no-referrer", context.Response.Headers["Referrer-Policy"]);
    }

    [Fact]
    public async Task InvokeAsync_WithNonExistentAsset_Returns404()
    {
        // Arrange
        var options = new SecureSpecOptions();
        var middleware = new SecureSpecUIMiddleware(
            _ => Task.CompletedTask,
            options,
            "securespec");

        var context = new DefaultHttpContext();
        context.Request.Path = "/securespec/assets/nonexistent.js";
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(404, context.Response.StatusCode);
    }
}
