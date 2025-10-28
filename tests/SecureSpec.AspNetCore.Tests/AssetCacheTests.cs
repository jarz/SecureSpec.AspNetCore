using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.UI;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for asset caching configuration and middleware.
/// </summary>
public class AssetCacheTests
{
    #region Configuration Tests

    [Fact]
    public void AssetCacheOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new AssetCacheOptions();

        // Assert
        Assert.Equal(3600, options.CacheLifetimeSeconds);
        Assert.True(options.EnableIntegrityRevalidation);
        Assert.True(options.AllowPublicCache);
    }

    [Fact]
    public void AssetCacheOptions_CustomValues_CanBeSet()
    {
        // Arrange
        var options = new AssetCacheOptions
        {
            CacheLifetimeSeconds = 7200,
            EnableIntegrityRevalidation = false,
            AllowPublicCache = false
        };

        // Assert
        Assert.Equal(7200, options.CacheLifetimeSeconds);
        Assert.False(options.EnableIntegrityRevalidation);
        Assert.False(options.AllowPublicCache);
    }

    [Fact]
    public void UIOptions_AssetsProperty_IsInitialized()
    {
        // Arrange & Act
        var options = new UIOptions();

        // Assert
        Assert.NotNull(options.Assets);
        Assert.Equal(3600, options.Assets.CacheLifetimeSeconds);
    }

    #endregion

    #region Middleware Tests

    [Theory]
    [InlineData("/swagger-ui/index.html")]
    [InlineData("/api-docs/swagger.js")]
    [InlineData("/assets/styles.css")]
    [InlineData("/ui/main.JS")]
    [InlineData("/static/icon.PNG")]
    public async Task AssetCacheMiddleware_AssetRequest_AddsCacheControlHeader(string path)
    {
        // Arrange
        var context = CreateHttpContext(path);
        var options = CreateOptions();
        var middleware = new AssetCacheMiddleware(
            async (ctx) =>
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("test content");
            },
            options);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("Cache-Control"));
        var cacheControl = context.Response.Headers.CacheControl.ToString();
        Assert.Contains("max-age=3600", cacheControl, StringComparison.Ordinal);
        Assert.Contains("public", cacheControl, StringComparison.Ordinal);
        Assert.Contains("must-revalidate", cacheControl, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/api/values")]
    [InlineData("/swagger/v1/swagger.json")]
    [InlineData("/health")]
    public async Task AssetCacheMiddleware_NonAssetRequest_DoesNotAddCacheControl(string path)
    {
        // Arrange
        var context = CreateHttpContext(path);
        var options = CreateOptions();
        var middleware = new AssetCacheMiddleware(
            async (ctx) =>
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("test content");
            },
            options);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.False(context.Response.Headers.ContainsKey("Cache-Control"));
    }

    [Fact]
    public async Task AssetCacheMiddleware_AssetRequest_AddsETagWhenIntegrityEnabled()
    {
        // Arrange
        var context = CreateHttpContext("/assets/app.js");
        var options = CreateOptions();
        var middleware = new AssetCacheMiddleware(
            async (ctx) =>
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("const app = {};");
            },
            options);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("ETag"));
        var etag = context.Response.Headers.ETag.ToString();
        Assert.StartsWith("W/\"sha256:", etag, StringComparison.Ordinal);
        Assert.EndsWith("\"", etag, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AssetCacheMiddleware_AssetRequest_NoETagWhenIntegrityDisabled()
    {
        // Arrange
        var context = CreateHttpContext("/assets/app.js");
        var options = CreateOptions(enableIntegrityRevalidation: false);
        var middleware = new AssetCacheMiddleware(
            async (ctx) =>
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("const app = {};");
            },
            options);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.False(context.Response.Headers.ContainsKey("ETag"));
    }

    [Fact]
    public async Task AssetCacheMiddleware_MatchingIfNoneMatch_Returns304()
    {
        // Arrange
        var context = CreateHttpContext("/assets/app.js");
        const string content = "const app = {};";

        // First request to get the ETag
        var options = CreateOptions();
        var middleware = new AssetCacheMiddleware(
            async (ctx) =>
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync(content);
            },
            options);

        await middleware.InvokeAsync(context);
        var etag = context.Response.Headers.ETag.ToString();

        // Second request with If-None-Match
        var context2 = CreateHttpContext("/assets/app.js");
        context2.Request.Headers.IfNoneMatch = etag;
        var middleware2 = new AssetCacheMiddleware(
            async (ctx) =>
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync(content);
            },
            options);

        // Act
        await middleware2.InvokeAsync(context2);

        // Assert
        Assert.Equal(304, context2.Response.StatusCode);
    }

    [Fact]
    public async Task AssetCacheMiddleware_DifferentContent_GeneratesDifferentETag()
    {
        // Arrange
        var context1 = CreateHttpContext("/assets/app.js");
        var context2 = CreateHttpContext("/assets/app.js");
        var options = CreateOptions();

        var middleware1 = new AssetCacheMiddleware(
            async (ctx) =>
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("content v1");
            },
            options);

        var middleware2 = new AssetCacheMiddleware(
            async (ctx) =>
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("content v2");
            },
            options);

        // Act
        await middleware1.InvokeAsync(context1);
        await middleware2.InvokeAsync(context2);

        // Assert
        var etag1 = context1.Response.Headers.ETag.ToString();
        var etag2 = context2.Response.Headers.ETag.ToString();
        Assert.NotEqual(etag1, etag2);
    }

    [Fact]
    public async Task AssetCacheMiddleware_CustomCacheLifetime_UsesConfiguredValue()
    {
        // Arrange
        var context = CreateHttpContext("/assets/app.js");
        var options = CreateOptions(cacheLifetimeSeconds: 7200);
        var middleware = new AssetCacheMiddleware(
            async (ctx) =>
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("test");
            },
            options);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var cacheControl = context.Response.Headers.CacheControl.ToString();
        Assert.Contains("max-age=7200", cacheControl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AssetCacheMiddleware_PrivateCache_UsesPrivateDirective()
    {
        // Arrange
        var context = CreateHttpContext("/assets/app.js");
        var options = CreateOptions(allowPublicCache: false);
        var middleware = new AssetCacheMiddleware(
            async (ctx) =>
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("test");
            },
            options);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var cacheControl = context.Response.Headers.CacheControl.ToString();
        Assert.Contains("private", cacheControl, StringComparison.Ordinal);
        Assert.DoesNotContain("public", cacheControl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AssetCacheMiddleware_NonSuccessfulResponse_DoesNotAddHeaders()
    {
        // Arrange
        var context = CreateHttpContext("/assets/app.js");
        var options = CreateOptions();
        var middleware = new AssetCacheMiddleware(
            (ctx) =>
            {
                ctx.Response.StatusCode = 404;
                return Task.CompletedTask;
            },
            options);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.False(context.Response.Headers.ContainsKey("ETag"));
        Assert.False(context.Response.Headers.ContainsKey("Cache-Control"));
    }

    [Theory]
    [InlineData(".js")]
    [InlineData(".css")]
    [InlineData(".html")]
    [InlineData(".svg")]
    [InlineData(".png")]
    [InlineData(".woff")]
    [InlineData(".woff2")]
    public async Task AssetCacheMiddleware_SupportsCommonAssetExtensions(string extension)
    {
        // Arrange
        var context = CreateHttpContext($"/assets/file{extension}");
        var options = CreateOptions();
        var middleware = new AssetCacheMiddleware(
            async (ctx) =>
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("content");
            },
            options);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("Cache-Control"));
    }

    [Fact]
    public async Task AssetCacheMiddleware_SameContent_GeneratesSameETag()
    {
        // Arrange
        const string content = "const app = { version: '1.0.0' };";
        var context1 = CreateHttpContext("/assets/app.js");
        var context2 = CreateHttpContext("/assets/app.js");
        var options = CreateOptions();

        var middleware1 = new AssetCacheMiddleware(
            async (ctx) =>
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync(content);
            },
            options);

        var middleware2 = new AssetCacheMiddleware(
            async (ctx) =>
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync(content);
            },
            options);

        // Act
        await middleware1.InvokeAsync(context1);
        await middleware2.InvokeAsync(context2);

        // Assert
        var etag1 = context1.Response.Headers.ETag.ToString();
        var etag2 = context2.Response.Headers.ETag.ToString();
        Assert.Equal(etag1, etag2);
    }

    #endregion

    #region Helper Methods

    private static DefaultHttpContext CreateHttpContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static IOptions<SecureSpecOptions> CreateOptions(
        int cacheLifetimeSeconds = 3600,
        bool enableIntegrityRevalidation = true,
        bool allowPublicCache = true)
    {
        var options = new SecureSpecOptions();
        options.UI.Assets.CacheLifetimeSeconds = cacheLifetimeSeconds;
        options.UI.Assets.EnableIntegrityRevalidation = enableIntegrityRevalidation;
        options.UI.Assets.AllowPublicCache = allowPublicCache;
        return Options.Create(options);
    }

    #endregion
}
