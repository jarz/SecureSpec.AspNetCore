using Microsoft.AspNetCore.Builder;

namespace SecureSpec.AspNetCore.UI;

/// <summary>
/// Extension methods for <see cref="IApplicationBuilder"/> to configure SecureSpec UI.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds asset caching middleware to the application pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for method chaining.</returns>
    public static IApplicationBuilder UseSecureSpecAssetCache(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<AssetCacheMiddleware>();
    }
}
