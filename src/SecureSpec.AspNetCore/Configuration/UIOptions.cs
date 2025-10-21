namespace SecureSpec.AspNetCore.Configuration;

/// <summary>
/// Configuration options for the SecureSpec UI.
/// </summary>
public class UIOptions
{
    /// <summary>
    /// Gets or sets whether to enable deep linking.
    /// Default is true.
    /// </summary>
    public bool DeepLinking { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to display operation IDs.
    /// Default is false.
    /// </summary>
    public bool DisplayOperationId { get; set; }

    /// <summary>
    /// Gets or sets the default models expand depth.
    /// Default is 1.
    /// </summary>
    public int DefaultModelsExpandDepth { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether to enable filtering.
    /// Default is true.
    /// </summary>
    public bool EnableFiltering { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable the "Try it out" feature.
    /// Default is true.
    /// </summary>
    public bool EnableTryItOut { get; set; } = true;

    /// <summary>
    /// Gets or sets the document title.
    /// </summary>
    public string? DocumentTitle { get; set; }
}
