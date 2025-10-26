namespace SecureSpec.AspNetCore.Core;

/// <summary>
/// Represents a cached OpenAPI document with its hash and timestamp.
/// </summary>
internal sealed class CacheEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CacheEntry"/> class.
    /// </summary>
    /// <param name="content">The serialized document content.</param>
    /// <param name="hash">The SHA256 hash of the document.</param>
    /// <param name="timestamp">The timestamp when the entry was created.</param>
    /// <param name="expiresAt">The timestamp when the entry expires.</param>
    public CacheEntry(string content, string hash, DateTimeOffset timestamp, DateTimeOffset expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);

        Content = content;
        Hash = hash;
        Timestamp = timestamp;
        ExpiresAt = expiresAt;
    }

    /// <summary>
    /// Gets the serialized document content.
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// Gets the SHA256 hash of the document.
    /// </summary>
    public string Hash { get; }

    /// <summary>
    /// Gets the timestamp when the entry was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the timestamp when the entry expires.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; }

    /// <summary>
    /// Gets a value indicating whether the entry has expired.
    /// </summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    /// <summary>
    /// Validates the integrity of the cached content by recomputing its hash.
    /// </summary>
    /// <returns>True if the hash matches the stored hash; otherwise, false.</returns>
    public bool ValidateIntegrity()
    {
        var computedHash = Serialization.CanonicalSerializer.GenerateHash(Content);
        return string.Equals(Hash, computedHash, StringComparison.Ordinal);
    }
}
