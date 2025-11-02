using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SecureSpec.AspNetCore.Configuration;

namespace SecureSpec.AspNetCore.UI;

/// <summary>
/// Middleware for serving UI assets with Cache-Control headers and integrity verification.
/// </summary>
public class AssetCacheMiddleware
{
    private static readonly string[] CachedAssetExtensions =
    {
        ".JS",
        ".CSS",
        ".HTML",
        ".SVG",
        ".PNG",
        ".WOFF",
        ".WOFF2"
    };

    private readonly RequestDelegate _next;
    private readonly AssetCacheOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetCacheMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="secureSpecOptions">The SecureSpec configuration options.</param>
    public AssetCacheMiddleware(RequestDelegate next, IOptions<SecureSpecOptions> secureSpecOptions)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        ArgumentNullException.ThrowIfNull(secureSpecOptions);
        _options = secureSpecOptions.Value.UI.Assets;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!IsAssetRequest(context.Request.Path))
        {
            await _next(context);
            return;
        }

        await ProcessAssetRequestAsync(context);
    }

    private async Task ProcessAssetRequestAsync(HttpContext context)
    {
        var originalBodyStream = context.Response.Body;

        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
            await HandleBufferedAssetResponseAsync(context, originalBodyStream, responseBody);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task HandleBufferedAssetResponseAsync(HttpContext context, Stream originalBodyStream, MemoryStream responseBody)
    {
        if (!ShouldProcessResponse(context.Response, responseBody))
        {
            await FlushBufferedResponseAsync(context, originalBodyStream, responseBody);
            return;
        }

        var content = await ReadResponseBodyAsync(responseBody);
        if (HandleConditionalRequest(context, content))
        {
            return;
        }

        context.Response.Headers.CacheControl = BuildCacheControlHeader();
        await FlushBufferedResponseAsync(context, originalBodyStream, responseBody);
    }

    /// <summary>
    /// Determines if the request is for a SecureSpec UI asset.
    /// </summary>
    private static bool IsAssetRequest(PathString path)
    {
        var pathValue = path.Value?.ToUpperInvariant() ?? string.Empty;

        // Only match UI assets, not OpenAPI spec files
        // UI assets are typically served from /securespec-ui/, /swagger-ui/, /assets/, etc.
        // Exclude swagger.json and openapi.json files
        if (pathValue.Contains("/SWAGGER.JSON", StringComparison.Ordinal) ||
            pathValue.Contains("/OPENAPI.JSON", StringComparison.Ordinal))
        {
            return false;
        }

        // Match common UI asset extensions
        return HasCachedAssetExtension(pathValue);
    }

    private static bool HasCachedAssetExtension(string pathValue)
    {
        return Array.Exists(CachedAssetExtensions, extension => pathValue.EndsWith(extension, StringComparison.Ordinal));
    }

    /// <summary>
    /// Generates an ETag from content using SHA256 hash.
    /// Format: W/"sha256:{first16hex}"
    /// </summary>
    private static string GenerateETag(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        var hexHash = Convert.ToHexString(hash);
        return $"W/\"sha256:{hexHash[..16]}\"";
    }

    /// <summary>
    /// Builds the Cache-Control header value based on configuration.
    /// </summary>
    private string BuildCacheControlHeader()
    {
        var parts = new List<string>();

        // Add public/private directive
        parts.Add(_options.AllowPublicCache ? "public" : "private");

        // Add max-age directive
        parts.Add($"max-age={_options.CacheLifetimeSeconds}");

        // Add must-revalidate for integrity checks
        if (_options.EnableIntegrityRevalidation)
        {
            parts.Add("must-revalidate");
        }

        return string.Join(", ", parts);
    }

    private static bool ShouldProcessResponse(HttpResponse response, MemoryStream buffer)
    {
        return response.StatusCode == StatusCodes.Status200OK && buffer.Length > 0;
    }

    private static async Task<string> ReadResponseBodyAsync(MemoryStream responseBody)
    {
        responseBody.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(responseBody, leaveOpen: true);
        var content = await reader.ReadToEndAsync();
        responseBody.Seek(0, SeekOrigin.Begin);
        return content;
    }

    private bool HandleConditionalRequest(HttpContext context, string content)
    {
        if (!_options.EnableIntegrityRevalidation)
        {
            return false;
        }

        var etag = GenerateETag(content);
        context.Response.Headers.ETag = etag;

        if (!context.Request.Headers.TryGetValue("If-None-Match", out var ifNoneMatch))
        {
            return false;
        }

        if (!ifNoneMatch.Contains(etag))
        {
            return false;
        }

        context.Response.StatusCode = StatusCodes.Status304NotModified;
        return true;
    }

    private static async Task FlushBufferedResponseAsync(HttpContext context, Stream originalBodyStream, MemoryStream buffer)
    {
        buffer.Seek(0, SeekOrigin.Begin);
        context.Response.Body = originalBodyStream;
        await buffer.CopyToAsync(originalBodyStream);
    }
}
