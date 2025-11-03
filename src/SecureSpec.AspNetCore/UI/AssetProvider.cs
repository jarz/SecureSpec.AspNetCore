using System.Reflection;
using System.Text;

namespace SecureSpec.AspNetCore.UI;

/// <summary>
/// Provides static assets for the SecureSpec UI using embedded resources.
/// </summary>
public static class AssetProvider
{
    private static readonly Dictionary<string, string> _assets = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Assembly _assembly = typeof(AssetProvider).Assembly;

    static AssetProvider()
    {
        // Load all static assets from embedded resources
        InitializeAssets();
    }

    /// <summary>
    /// Gets an asset by its path.
    /// </summary>
    /// <param name="path">The asset path (e.g., "assets/styles.css").</param>
    /// <returns>The asset content, or null if not found.</returns>
    public static string? GetAsset(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        // Normalize path (lowercase is appropriate for file paths)
#pragma warning disable CA1308 // Normalize strings to uppercase
        var normalizedPath = path.Replace('\\', '/').ToLowerInvariant();
#pragma warning restore CA1308

        return _assets.TryGetValue(normalizedPath, out var content) ? content : null;
    }

    /// <summary>
    /// Initializes all static assets from embedded resources.
    /// </summary>
    private static void InitializeAssets()
    {
        // Map of logical paths to embedded resource names
        var assetMappings = new Dictionary<string, string>
        {
            ["assets/styles.css"] = "SecureSpec.AspNetCore.UI.Assets.styles.css",
            ["assets/app.js"] = "SecureSpec.AspNetCore.UI.Assets.app.js",
            ["assets/components/router.js"] = "SecureSpec.AspNetCore.UI.Assets.components.router.js",
            ["assets/components/state.js"] = "SecureSpec.AspNetCore.UI.Assets.components.state.js",
            ["assets/components/operation-display.js"] = "SecureSpec.AspNetCore.UI.Assets.components.operation-display.js",
            ["assets/components/schema-viewer.js"] = "SecureSpec.AspNetCore.UI.Assets.components.schema-viewer.js",
            ["assets/components/utils.js"] = "SecureSpec.AspNetCore.UI.Assets.components.utils.js",
            ["assets/components/links-callbacks.js"] = "SecureSpec.AspNetCore.UI.Assets.components.links-callbacks.js"
        };

        foreach (var (logicalPath, resourceName) in assetMappings)
        {
            _assets[logicalPath] = LoadEmbeddedResource(resourceName);
        }
    }

    /// <summary>
    /// Loads an embedded resource as a string.
    /// </summary>
    /// <param name="resourceName">The full name of the embedded resource.</param>
    /// <returns>The resource content as a string.</returns>
    private static string LoadEmbeddedResource(string resourceName)
    {
        using var stream = _assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Embedded resource '{resourceName}' not found. Available resources: {string.Join(", ", _assembly.GetManifestResourceNames())}");
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
