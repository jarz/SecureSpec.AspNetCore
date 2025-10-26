using System.Security.Cryptography;

namespace SecureSpec.AspNetCore.Security;

/// <summary>
/// Manages CSRF (Cross-Site Request Forgery) tokens using double-submit cookie pattern
/// with token rotation for OAuth flows.
/// </summary>
/// <remarks>
/// Implements CSRF protection for OAuth 2.0 Authorization Code flow using:
/// - Double-submit cookie pattern
/// - Token rotation on each request
/// - Cryptographically secure random tokens
/// - Time-based token expiration
/// </remarks>
public class CsrfTokenManager
{
    /// <summary>
    /// Default token length in bytes (32 bytes = 256 bits).
    /// </summary>
    public const int DefaultTokenLength = 32;

    /// <summary>
    /// Minimum token length in bytes (16 bytes = 128 bits).
    /// </summary>
    public const int MinTokenLength = 16;

    /// <summary>
    /// Maximum token length in bytes (64 bytes = 512 bits).
    /// </summary>
    public const int MaxTokenLength = 64;

    /// <summary>
    /// Default token lifetime (15 minutes).
    /// </summary>
    public static readonly TimeSpan DefaultTokenLifetime = TimeSpan.FromMinutes(15);

    private readonly int _tokenLength;
    private readonly TimeSpan _tokenLifetime;
    private readonly Dictionary<string, CsrfTokenEntry> _tokens;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CsrfTokenManager"/> class.
    /// </summary>
    /// <param name="tokenLength">
    /// Length of the CSRF token in bytes. Must be between 16 and 64 bytes.
    /// Defaults to 32 bytes (256 bits).
    /// </param>
    /// <param name="tokenLifetime">
    /// Lifetime of CSRF tokens. Defaults to 15 minutes.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when tokenLength is outside the valid range (16-64 bytes).
    /// </exception>
    public CsrfTokenManager(
        int tokenLength = DefaultTokenLength,
        TimeSpan? tokenLifetime = null)
    {
        if (tokenLength < MinTokenLength || tokenLength > MaxTokenLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tokenLength),
                tokenLength,
                $"Token length must be between {MinTokenLength} and {MaxTokenLength} bytes.");
        }

        _tokenLength = tokenLength;
        _tokenLifetime = tokenLifetime ?? DefaultTokenLifetime;
        _tokens = new Dictionary<string, CsrfTokenEntry>(StringComparer.Ordinal);
    }

    /// <summary>
    /// Generates a new CSRF token with associated state.
    /// </summary>
    /// <param name="state">
    /// OAuth state parameter to associate with this CSRF token.
    /// </param>
    /// <returns>A cryptographically secure CSRF token.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when state is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when state is empty or whitespace.
    /// </exception>
    public string GenerateToken(string state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(state, nameof(state));

        var tokenBytes = new byte[_tokenLength];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(tokenBytes);
        }

        var token = Convert.ToBase64String(tokenBytes);

        lock (_lock)
        {
            // Clean up expired tokens before adding new one
            CleanupExpiredTokens();

            _tokens[token] = new CsrfTokenEntry
            {
                State = state,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.Add(_tokenLifetime)
            };
        }

        return token;
    }

    /// <summary>
    /// Validates a CSRF token and retrieves its associated state.
    /// </summary>
    /// <param name="token">The CSRF token to validate.</param>
    /// <param name="state">
    /// When this method returns, contains the state associated with the token
    /// if validation was successful; otherwise, null.
    /// </param>
    /// <returns>
    /// <c>true</c> if the token is valid and not expired; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method implements token rotation: after successful validation,
    /// the token is removed and cannot be used again.
    /// </remarks>
    public bool ValidateAndRotateToken(string? token, out string? state)
    {
        state = null;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        lock (_lock)
        {
            // Clean up expired tokens
            CleanupExpiredTokens();

            if (!_tokens.TryGetValue(token, out var entry))
            {
                return false;
            }

            // Check if token has expired
            if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                _tokens.Remove(token);
                return false;
            }

            // Token is valid - retrieve state and rotate (remove) token
            state = entry.State;
            _tokens.Remove(token);
            return true;
        }
    }

    /// <summary>
    /// Validates a CSRF token without rotation (peek operation).
    /// </summary>
    /// <param name="token">The CSRF token to validate.</param>
    /// <param name="state">
    /// When this method returns, contains the state associated with the token
    /// if validation was successful; otherwise, null.
    /// </param>
    /// <returns>
    /// <c>true</c> if the token is valid and not expired; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Unlike <see cref="ValidateAndRotateToken"/>, this method does not remove
    /// the token after validation. Use this for read-only validation scenarios.
    /// </remarks>
    public bool ValidateToken(string? token, out string? state)
    {
        state = null;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        lock (_lock)
        {
            if (!_tokens.TryGetValue(token, out var entry))
            {
                return false;
            }

            // Check if token has expired
            if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                return false;
            }

            state = entry.State;
            return true;
        }
    }

    /// <summary>
    /// Removes a specific CSRF token.
    /// </summary>
    /// <param name="token">The token to remove.</param>
    /// <returns>
    /// <c>true</c> if the token was found and removed; otherwise, <c>false</c>.
    /// </returns>
    public bool RemoveToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        lock (_lock)
        {
            return _tokens.Remove(token);
        }
    }

    /// <summary>
    /// Clears all CSRF tokens.
    /// </summary>
    public void ClearAll()
    {
        lock (_lock)
        {
            _tokens.Clear();
        }
    }

    /// <summary>
    /// Gets the number of active CSRF tokens.
    /// </summary>
    public int ActiveTokenCount
    {
        get
        {
            lock (_lock)
            {
                CleanupExpiredTokens();
                return _tokens.Count;
            }
        }
    }

    /// <summary>
    /// Removes all expired tokens from the internal storage.
    /// </summary>
    /// <remarks>
    /// This method is called automatically during token operations.
    /// It can also be called manually to perform cleanup.
    /// </remarks>
    public void CleanupExpiredTokens()
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            var expiredTokens = _tokens
                .Where(kvp => kvp.Value.ExpiresAt <= now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var expiredToken in expiredTokens)
            {
                _tokens.Remove(expiredToken);
            }
        }
    }

    /// <summary>
    /// Represents an entry in the CSRF token storage.
    /// </summary>
    private sealed class CsrfTokenEntry
    {
        /// <summary>
        /// Gets or sets the OAuth state associated with this token.
        /// </summary>
        public required string State { get; init; }

        /// <summary>
        /// Gets or sets when the token was created.
        /// </summary>
        public required DateTimeOffset CreatedAt { get; init; }

        /// <summary>
        /// Gets or sets when the token expires.
        /// </summary>
        public required DateTimeOffset ExpiresAt { get; init; }
    }
}
