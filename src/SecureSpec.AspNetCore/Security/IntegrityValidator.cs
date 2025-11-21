using System.Security.Cryptography;
using System.Text;
using SecureSpec.AspNetCore.Diagnostics;

namespace SecureSpec.AspNetCore.Security;

/// <summary>
/// Provides integrity validation with SHA256 and SRI (Subresource Integrity) support.
/// </summary>
public class IntegrityValidator
{
    private const string RedactedPlaceholder = "[REDACTED]";

    private readonly DiagnosticsLogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrityValidator"/> class.
    /// </summary>
    public IntegrityValidator()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrityValidator"/> class with a diagnostics logger.
    /// </summary>
    /// <param name="logger">The diagnostics logger.</param>
    public IntegrityValidator(DiagnosticsLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Computes the SHA256 hash of the content and returns it as a hexadecimal string.
    /// Content is normalized to LF line endings and UTF-8 encoding before hashing (AC 499).
    /// </summary>
    /// <param name="content">The content to hash.</param>
    /// <returns>The SHA256 hash as a lowercase hexadecimal string.</returns>
    public static string ComputeHash(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        // Normalize line endings to LF before hashing (AC 499)
        content = content.Replace("\r\n", "\n", StringComparison.Ordinal);

        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return ConvertToLowerHex(hash);
    }

    /// <summary>
    /// Generates an SRI (Subresource Integrity) attribute value from content.
    /// Format: sha256-{base64_hash}
    /// </summary>
    /// <param name="content">The content to generate SRI for.</param>
    /// <returns>The SRI attribute value (e.g., "sha256-abc123...").</returns>
    public static string GenerateSri(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        // Normalize line endings to LF before hashing (AC 499)
        content = content.Replace("\r\n", "\n", StringComparison.Ordinal);

        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        var base64Hash = Convert.ToBase64String(hash);

        return $"sha256-{base64Hash}";
    }

    /// <summary>
    /// Verifies the integrity of content against an expected hash.
    /// </summary>
    /// <param name="content">The content to verify.</param>
    /// <param name="expectedHash">The expected SHA256 hash (hexadecimal string).</param>
    /// <param name="resourcePath">Optional resource path for diagnostic logging (will be redacted per AC 500).</param>
    /// <returns>True if the hash matches; false otherwise.</returns>
    public bool VerifyIntegrity(string content, string expectedHash, string? resourcePath = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(expectedHash);

        var actualHash = ComputeHash(content);
        var isValid = string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);

        if (!isValid)
        {
            // AC 500: Integrity mismatch diagnostic redacts path & partial hash only
            var partialHash = actualHash.Length >= 8 ? actualHash[..8] : actualHash;
            var partialExpected = expectedHash.Length >= 8 ? expectedHash[..8] : expectedHash;

            _logger?.LogCritical(
                "SEC001",
                "Integrity check failed",
                new
                {
                    Expected = partialExpected + "...",
                    Actual = partialHash + "...",
                    ResourcePathRedacted = resourcePath != null ? RedactedPlaceholder : null
                });
        }

        return isValid;
    }

    /// <summary>
    /// Verifies the integrity of content against an SRI attribute value.
    /// </summary>
    /// <param name="content">The content to verify.</param>
    /// <param name="sriValue">The SRI attribute value (e.g., "sha256-abc123...").</param>
    /// <param name="resourcePath">Optional resource path for diagnostic logging (will be redacted per AC 500).</param>
    /// <returns>True if the integrity check passes; false otherwise.</returns>
    public bool VerifySri(string content, string sriValue, string? resourcePath = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(sriValue);

        // Parse SRI format: algorithm-base64hash
        var parts = sriValue.Split('-', 2);
        if (parts.Length != 2)
        {
            _logger?.LogCritical(
                DiagnosticCodes.IntegrityCheckFailed,
                "Invalid SRI format",
                new { SriValue = sriValue, ResourcePathRedacted = resourcePath != null ? RedactedPlaceholder : null });
            return false;
        }

        var algorithm = parts[0];
        var expectedBase64Hash = parts[1];

        // Only SHA256 is supported (per PRD: "Algorithm: SHA256 only")
        if (!string.Equals(algorithm, "sha256", StringComparison.OrdinalIgnoreCase))
        {
            _logger?.LogCritical(
                DiagnosticCodes.IntegrityCheckFailed,
                "Unsupported SRI algorithm",
                new { Algorithm = algorithm, Supported = "sha256", ResourcePathRedacted = resourcePath != null ? RedactedPlaceholder : null });
            return false;
        }

        // Compute actual hash
        content = content.Replace("\r\n", "\n", StringComparison.Ordinal);
        var bytes = Encoding.UTF8.GetBytes(content);
        var actualHash = SHA256.HashData(bytes);
        var actualBase64Hash = Convert.ToBase64String(actualHash);

        var isValid = string.Equals(actualBase64Hash, expectedBase64Hash, StringComparison.Ordinal);

        if (!isValid)
        {
            // AC 500: Integrity mismatch diagnostic redacts path & partial hash only
            var partialExpected = expectedBase64Hash.Length >= 12 ? expectedBase64Hash[..12] : expectedBase64Hash;
            var partialActual = actualBase64Hash.Length >= 12 ? actualBase64Hash[..12] : actualBase64Hash;

            _logger?.LogCritical(
                DiagnosticCodes.IntegrityCheckFailed,
                "SRI integrity check failed",
                new
                {
                    Expected = partialExpected + "...",
                    Actual = partialActual + "...",
                    ResourcePathRedacted = resourcePath != null ? RedactedPlaceholder : null
                });
        }

        return isValid;
    }

    /// <summary>
    /// Converts the provided hash bytes to a lowercase hexadecimal string.
    /// </summary>
    private static string ConvertToLowerHex(byte[] hash)
    {
        const string hexTable = "0123456789abcdef";
        var chars = new char[hash.Length * 2];
        for (var i = 0; i < hash.Length; i++)
        {
            var value = hash[i];
            chars[i * 2] = hexTable[value >> 4];
            chars[(i * 2) + 1] = hexTable[value & 0x0F];
        }

        return new string(chars);
    }
}
