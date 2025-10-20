namespace SecureSpec.AspNetCore.Configuration;

/// <summary>
/// Configuration options for canonical serialization.
/// </summary>
public class SerializationOptions
{
    /// <summary>
    /// Gets or sets whether to use deterministic ordering.
    /// This is required for stable hash generation and cannot be disabled.
    /// Default is true.
    /// </summary>
    public bool DeterministicOrdering { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to generate SHA256 hashes for documents.
    /// Default is true.
    /// </summary>
    public bool GenerateHashes { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to generate ETags.
    /// Default is true.
    /// </summary>
    public bool GenerateETags { get; set; } = true;
}
