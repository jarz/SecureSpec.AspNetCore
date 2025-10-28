namespace SecureSpec.AspNetCore.Configuration;

/// <summary>
/// Configuration options for integrity enforcement.
/// </summary>
public class IntegrityOptions
{
    /// <summary>
    /// Gets or sets whether integrity checking is enabled.
    /// Default is true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to fail closed on integrity mismatches.
    /// When true, integrity failures will prevent resource loading.
    /// When false, integrity failures will be logged but allow loading.
    /// Default is true (fail-closed for security).
    /// </summary>
    public bool FailClosed { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to generate SRI (Subresource Integrity) attributes.
    /// Default is true.
    /// </summary>
    public bool GenerateSri { get; set; } = true;

    /// <summary>
    /// Gets the hash algorithm to use for integrity verification.
    /// Only SHA256 is supported for security and determinism.
    /// </summary>
    public string Algorithm { get; } = "sha256";
}
