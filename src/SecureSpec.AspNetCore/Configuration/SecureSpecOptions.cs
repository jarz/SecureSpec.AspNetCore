namespace SecureSpec.AspNetCore.Configuration;

/// <summary>
/// Options for configuring SecureSpec.AspNetCore.
/// </summary>
public class SecureSpecOptions
{
    /// <summary>
    /// Gets the collection of OpenAPI documents to generate.
    /// </summary>
    public DocumentCollection Documents { get; } = new();

    /// <summary>
    /// Gets security configuration options.
    /// </summary>
    public SecurityOptions Security { get; } = new();

    /// <summary>
    /// Gets schema generation options.
    /// </summary>
    public SchemaOptions Schema { get; } = new();

    /// <summary>
    /// Gets UI configuration options.
    /// </summary>
    public UIOptions UI { get; } = new();

    /// <summary>
    /// Gets serialization options.
    /// </summary>
    public SerializationOptions Serialization { get; } = new();

    /// <summary>
    /// Gets diagnostics options.
    /// </summary>
    public DiagnosticsOptions Diagnostics { get; } = new();
}
