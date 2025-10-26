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

        // Check if this is a SecureSpec UI asset request
        if (IsAssetRequest(context.Request.Path))
        {
            // Store the original response body stream
            var originalBodyStream = context.Response.Body;

            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Call the next middleware
            await _next(context);

            // Only process successful responses with content
            if (context.Response.StatusCode == 200 && responseBody.Length > 0)
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                string content;
                using (var reader = new StreamReader(responseBody, leaveOpen: true))
                {
                    content = await reader.ReadToEndAsync();
                }
                responseBody.Seek(0, SeekOrigin.Begin);

                // Generate ETag if integrity revalidation is enabled
                if (_options.EnableIntegrityRevalidation)
                {
                    var etag = GenerateETag(content);
                    context.Response.Headers.ETag = etag;

                    // Check If-None-Match header for conditional requests
                    if (context.Request.Headers.TryGetValue("If-None-Match", out var ifNoneMatch))
                    {
                        if (ifNoneMatch.Contains(etag))
                        {
                            context.Response.StatusCode = 304; // Not Modified
                            context.Response.Body = originalBodyStream;
                            return;
                        }
                    }
                }

                // Add Cache-Control header
                var cacheControl = BuildCacheControlHeader();
                context.Response.Headers.CacheControl = cacheControl;

                // Copy the response body back
                context.Response.Body = originalBodyStream;
                await responseBody.CopyToAsync(originalBodyStream);
            }
            else
            {
                // Just copy the response for non-200 or empty responses
                context.Response.Body = originalBodyStream;
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
        else
        {
            // Not an asset request, skip processing
            await _next(context);
        }
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
        if (pathValue.Contains("/SWAGGER.JSON", StringComparison.OrdinalIgnoreCase) ||
            pathValue.Contains("/OPENAPI.JSON", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Match common UI asset extensions
        return pathValue.EndsWith(".JS", StringComparison.OrdinalIgnoreCase) ||
               pathValue.EndsWith(".CSS", StringComparison.OrdinalIgnoreCase) ||
               pathValue.EndsWith(".HTML", StringComparison.OrdinalIgnoreCase) ||
               pathValue.EndsWith(".SVG", StringComparison.OrdinalIgnoreCase) ||
               pathValue.EndsWith(".PNG", StringComparison.OrdinalIgnoreCase) ||
               pathValue.EndsWith(".WOFF", StringComparison.OrdinalIgnoreCase) ||
               pathValue.EndsWith(".WOFF2", StringComparison.OrdinalIgnoreCase);
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
}
