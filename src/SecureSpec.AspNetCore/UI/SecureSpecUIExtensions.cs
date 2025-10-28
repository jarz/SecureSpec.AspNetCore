using Microsoft.AspNetCore.Builder;
using SecureSpec.AspNetCore.Configuration;

namespace SecureSpec.AspNetCore.UI;

/// <summary>
/// Extension methods for adding SecureSpec UI middleware.
/// </summary>
public static class SecureSpecUIExtensions
{
    /// <summary>
    /// Adds the SecureSpec UI middleware to the application pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="routePrefix">The route prefix for the UI (default: "securespec").</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseSecureSpecUI(
        this IApplicationBuilder app,
        string routePrefix = "securespec")
    {
        ArgumentNullException.ThrowIfNull(app);

        // Get options from DI if available, otherwise use defaults
        var options = app.ApplicationServices.GetService(typeof(SecureSpecOptions)) as SecureSpecOptions
            ?? new SecureSpecOptions();

        // Add OpenAPI document middleware first
        app.UseMiddleware<OpenApiDocumentMiddleware>(options);

        // Then add UI middleware
        app.UseMiddleware<SecureSpecUIMiddleware>(options, routePrefix);

        return app;
    }

    /// <summary>
    /// Adds the SecureSpec UI middleware to the application pipeline with configuration.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="configure">Action to configure the UI options.</param>
    /// <param name="routePrefix">The route prefix for the UI (default: "securespec").</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseSecureSpecUI(
        this IApplicationBuilder app,
        Action<UIOptions> configure,
        string routePrefix = "securespec")
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new SecureSpecOptions();
        configure(options.UI);

        // Add OpenAPI document middleware first
        app.UseMiddleware<OpenApiDocumentMiddleware>(options);

        // Then add UI middleware
        app.UseMiddleware<SecureSpecUIMiddleware>(options, routePrefix);

        return app;
    }
}
