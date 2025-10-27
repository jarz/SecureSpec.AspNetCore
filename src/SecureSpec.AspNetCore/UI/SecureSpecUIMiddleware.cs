using Microsoft.AspNetCore.Http;
using SecureSpec.AspNetCore.Configuration;
using System.Text;

namespace SecureSpec.AspNetCore.UI;

/// <summary>
/// Middleware for serving the SecureSpec UI.
/// </summary>
public class SecureSpecUIMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecureSpecOptions _options;
    private readonly string _routePrefix;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecureSpecUIMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="options">The SecureSpec configuration options.</param>
    /// <param name="routePrefix">The route prefix for the UI (e.g., "securespec").</param>
    public SecureSpecUIMiddleware(RequestDelegate next, SecureSpecOptions options, string routePrefix = "securespec")
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        ArgumentNullException.ThrowIfNull(routePrefix);
        _routePrefix = routePrefix.Trim('/');
    }

    /// <summary>
    /// Processes the HTTP request.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var path = httpContext.Request.Path.Value?.TrimStart('/') ?? string.Empty;

        // Check if the request is for the UI
        if (path.StartsWith(_routePrefix, StringComparison.OrdinalIgnoreCase))
        {
            var subPath = path.Substring(_routePrefix.Length).TrimStart('/');

            // Route to appropriate handler
            if (string.IsNullOrEmpty(subPath) || subPath == "index.html")
            {
                await ServeIndexHtmlAsync(httpContext);
                return;
            }
            else if (subPath.StartsWith("assets/", StringComparison.OrdinalIgnoreCase))
            {
                await ServeAssetAsync(httpContext, subPath);
                return;
            }
        }

        // Not a UI request, pass to next middleware
        await _next(httpContext);
    }

    /// <summary>
    /// Serves the main index.html page.
    /// </summary>
    private async Task ServeIndexHtmlAsync(HttpContext context)
    {
        var html = UITemplateGenerator.GenerateIndexHtml(_options);

        // Set security headers
        SetSecurityHeaders(context.Response);

        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.StatusCode = 200;

        await context.Response.WriteAsync(html, Encoding.UTF8);
    }

    /// <summary>
    /// Serves static assets (CSS, JS, etc.).
    /// </summary>
    private async Task ServeAssetAsync(HttpContext context, string subPath)
    {
        var assetContent = AssetProvider.GetAsset(subPath);

        if (assetContent == null)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("Asset not found");
            return;
        }

        // Determine content type
        var contentType = GetContentType(subPath);
        context.Response.ContentType = contentType;

        // Set caching headers
        context.Response.Headers["Cache-Control"] = "public, max-age=3600";

        // Set security headers
        SetSecurityHeaders(context.Response);

        await context.Response.WriteAsync(assetContent, Encoding.UTF8);
    }

    /// <summary>
    /// Sets security headers on the response.
    /// </summary>
    private void SetSecurityHeaders(HttpResponse response)
    {
        // Content Security Policy - strict by default
        response.Headers["Content-Security-Policy"] =
            "default-src 'none'; " +
            "script-src 'self'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; " +
            "font-src 'self'; " +
            "connect-src 'self'; " +
            "base-uri 'self'; " +
            "form-action 'self';";

        // Additional security headers
        response.Headers["X-Content-Type-Options"] = "nosniff";
        response.Headers["X-Frame-Options"] = "DENY";
        response.Headers["Referrer-Policy"] = "no-referrer";
    }

    /// <summary>
    /// Gets the content type for a file based on its extension.
    /// </summary>
    private static string GetContentType(string path)
    {
#pragma warning disable CA1308 // Normalize strings to uppercase - file extensions are conventionally lowercase
        var extension = Path.GetExtension(path).ToLowerInvariant();
#pragma warning restore CA1308
        return extension switch
        {
            ".html" => "text/html; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".js" => "application/javascript; charset=utf-8",
            ".json" => "application/json; charset=utf-8",
            ".svg" => "image/svg+xml",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            _ => "application/octet-stream"
        };
    }
}
