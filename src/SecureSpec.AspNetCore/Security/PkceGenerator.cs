using System.Security.Cryptography;
using System.Text;

namespace SecureSpec.AspNetCore.Security;

/// <summary>
/// Generates PKCE (Proof Key for Code Exchange) code verifier and challenge
/// according to RFC 7636 for OAuth 2.0 Authorization Code Flow.
/// </summary>
/// <remarks>
/// PKCE is a security extension to OAuth 2.0 that prevents authorization code
/// interception attacks. It is REQUIRED for all authorization code flows in
/// SecureSpec.AspNetCore.
/// </remarks>
public static class PkceGenerator
{
    /// <summary>
    /// Minimum length for code verifier (RFC 7636 requires 43-128 characters).
    /// </summary>
    public const int MinVerifierLength = 43;

    /// <summary>
    /// Maximum length for code verifier (RFC 7636 requires 43-128 characters).
    /// </summary>
    public const int MaxVerifierLength = 128;

    /// <summary>
    /// Default length for code verifier (64 characters provides good security/size balance).
    /// </summary>
    public const int DefaultVerifierLength = 64;

    /// <summary>
    /// Characters allowed in code verifier: unreserved characters from RFC 3986
    /// (A-Z, a-z, 0-9, -, ., _, ~).
    /// </summary>
    private const string UnreservedCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";

    /// <summary>
    /// Generates a cryptographically secure PKCE code verifier.
    /// </summary>
    /// <param name="length">
    /// Length of the code verifier. Must be between 43 and 128 characters.
    /// Defaults to 64 characters.
    /// </param>
    /// <returns>A URL-safe code verifier string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when length is outside the valid range (43-128).
    /// </exception>
    /// <remarks>
    /// The code verifier is a cryptographically random string using characters
    /// from the unreserved character set: [A-Z] / [a-z] / [0-9] / "-" / "." / "_" / "~"
    /// </remarks>
    public static string GenerateCodeVerifier(int length = DefaultVerifierLength)
    {
        if (length < MinVerifierLength || length > MaxVerifierLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length),
                length,
                $"Code verifier length must be between {MinVerifierLength} and {MaxVerifierLength} characters.");
        }

        var randomBytes = new byte[length];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        var verifier = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            // Map random byte to unreserved character set
            verifier.Append(UnreservedCharacters[randomBytes[i] % UnreservedCharacters.Length]);
        }

        return verifier.ToString();
    }

    /// <summary>
    /// Generates a SHA256 code challenge from a code verifier.
    /// </summary>
    /// <param name="codeVerifier">The code verifier to create challenge from.</param>
    /// <returns>Base64url-encoded SHA256 hash of the code verifier.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when codeVerifier is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when codeVerifier is empty or contains invalid characters.
    /// </exception>
    /// <remarks>
    /// The code challenge is calculated as:
    /// BASE64URL(SHA256(ASCII(code_verifier)))
    ///
    /// Only the S256 method is supported. The "plain" method is not supported
    /// as it provides insufficient security.
    /// </remarks>
    public static string GenerateCodeChallenge(string codeVerifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codeVerifier, nameof(codeVerifier));

        if (codeVerifier.Length < MinVerifierLength || codeVerifier.Length > MaxVerifierLength)
        {
            throw new ArgumentException(
                $"Code verifier length must be between {MinVerifierLength} and {MaxVerifierLength} characters.",
                nameof(codeVerifier));
        }

        // Validate that verifier contains only unreserved characters
        if (!codeVerifier.All(c => UnreservedCharacters.Contains(c, StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                "Code verifier contains invalid characters. Only unreserved characters [A-Z] / [a-z] / [0-9] / \"-\" / \".\" / \"_\" / \"~\" are allowed.",
                nameof(codeVerifier));
        }

        // Calculate SHA256 hash of verifier
        var verifierBytes = Encoding.ASCII.GetBytes(codeVerifier);
        var challengeBytes = SHA256.HashData(verifierBytes);

        // Return Base64url-encoded challenge
        return Base64UrlEncode(challengeBytes);
    }

    /// <summary>
    /// Generates a complete PKCE parameter set (verifier and challenge).
    /// </summary>
    /// <param name="verifierLength">
    /// Length of the code verifier. Must be between 43 and 128 characters.
    /// Defaults to 64 characters.
    /// </param>
    /// <returns>A tuple containing the code verifier and code challenge.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when verifierLength is outside the valid range (43-128).
    /// </exception>
    public static (string CodeVerifier, string CodeChallenge) GeneratePkceParameters(
        int verifierLength = DefaultVerifierLength)
    {
        var verifier = GenerateCodeVerifier(verifierLength);
        var challenge = GenerateCodeChallenge(verifier);
        return (verifier, challenge);
    }

    /// <summary>
    /// Encodes a byte array to Base64url format (RFC 4648 Section 5).
    /// </summary>
    /// <param name="data">The byte array to encode.</param>
    /// <returns>Base64url-encoded string.</returns>
    /// <remarks>
    /// Base64url encoding is Base64 with:
    /// - '+' replaced with '-'
    /// - '/' replaced with '_'
    /// - Padding '=' characters removed
    /// </remarks>
    private static string Base64UrlEncode(byte[] data)
    {
        var base64 = Convert.ToBase64String(data);

        // Convert Base64 to Base64url
        return base64
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>
    /// Gets the challenge method identifier.
    /// </summary>
    /// <remarks>
    /// Always returns "S256" as only SHA256 is supported.
    /// The "plain" method is explicitly not supported for security reasons.
    /// </remarks>
    public static string ChallengeMethod => "S256";
}
