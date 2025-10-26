using SecureSpec.AspNetCore.Security;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for PKCE (Proof Key for Code Exchange) code verifier and challenge generation.
/// Validates RFC 7636 compliance for OAuth 2.0 Authorization Code Flow with PKCE.
/// </summary>
public class PkceGeneratorTests
{
    [Fact]
    public void GenerateCodeVerifier_WithDefaultLength_GeneratesVerifierOfCorrectLength()
    {
        // Act
        var verifier = PkceGenerator.GenerateCodeVerifier();

        // Assert
        Assert.Equal(PkceGenerator.DefaultVerifierLength, verifier.Length);
    }

    [Theory]
    [InlineData(43)]  // Minimum valid length
    [InlineData(64)]  // Default length
    [InlineData(100)]
    [InlineData(128)] // Maximum valid length
    public void GenerateCodeVerifier_WithValidLength_GeneratesVerifierOfSpecifiedLength(int length)
    {
        // Act
        var verifier = PkceGenerator.GenerateCodeVerifier(length);

        // Assert
        Assert.Equal(length, verifier.Length);
    }

    [Theory]
    [InlineData(42)]  // One below minimum
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(129)] // One above maximum
    [InlineData(200)]
    public void GenerateCodeVerifier_WithInvalidLength_ThrowsArgumentOutOfRangeException(int length)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => PkceGenerator.GenerateCodeVerifier(length));
        Assert.Equal("length", exception.ParamName);
    }

    [Fact]
    public void GenerateCodeVerifier_ContainsOnlyUnreservedCharacters()
    {
        // Arrange
        const string unreservedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";

        // Act
        var verifier = PkceGenerator.GenerateCodeVerifier();

        // Assert
        Assert.All(verifier, c => Assert.Contains(c, unreservedChars));
    }

    [Fact]
    public void GenerateCodeVerifier_GeneratesUniqueVerifiers()
    {
        // Arrange
        var verifiers = new HashSet<string>();

        // Act - Generate 100 verifiers
        for (int i = 0; i < 100; i++)
        {
            verifiers.Add(PkceGenerator.GenerateCodeVerifier());
        }

        // Assert - All should be unique
        Assert.Equal(100, verifiers.Count);
    }

    [Fact]
    public void GenerateCodeChallenge_WithValidVerifier_GeneratesValidChallenge()
    {
        // Arrange
        var verifier = PkceGenerator.GenerateCodeVerifier();

        // Act
        var challenge = PkceGenerator.GenerateCodeChallenge(verifier);

        // Assert
        Assert.NotNull(challenge);
        Assert.NotEmpty(challenge);
        // Base64url encoded SHA256 hash should be 43 characters (256 bits / 6 bits per char, rounded up)
        Assert.Equal(43, challenge.Length);
    }

    [Fact]
    public void GenerateCodeChallenge_IsDeterministic()
    {
        // Arrange
        const string verifier = "test-verifier-with-exactly-forty-three-characters";

        // Act
        var challenge1 = PkceGenerator.GenerateCodeChallenge(verifier);
        var challenge2 = PkceGenerator.GenerateCodeChallenge(verifier);

        // Assert
        Assert.Equal(challenge1, challenge2);
    }

    [Fact]
    public void GenerateCodeChallenge_UsesBase64UrlEncoding()
    {
        // Arrange
        var verifier = PkceGenerator.GenerateCodeVerifier();

        // Act
        var challenge = PkceGenerator.GenerateCodeChallenge(verifier);

        // Assert - Base64url should not contain '+', '/', or '='
        Assert.DoesNotContain('+', challenge);
        Assert.DoesNotContain('/', challenge);
        Assert.DoesNotContain('=', challenge);

        // Should only contain Base64url characters: A-Z, a-z, 0-9, -, _
        Assert.All(challenge, c => Assert.True(
            char.IsLetterOrDigit(c) || c == '-' || c == '_',
            $"Invalid Base64url character: {c}"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void GenerateCodeChallenge_WithNullOrWhiteSpace_ThrowsArgumentException(string? verifier)
    {
        // Act & Assert
        if (verifier is null)
        {
            Assert.Throws<ArgumentNullException>(() => PkceGenerator.GenerateCodeChallenge(verifier!));
        }
        else
        {
            Assert.Throws<ArgumentException>(() => PkceGenerator.GenerateCodeChallenge(verifier));
        }
    }

    [Theory]
    [InlineData("too-short")]  // Too short
    [InlineData("this-verifier-is-way-too-long-and-exceeds-the-maximum-allowed-length-of-128-characters-which-is-specified-in-rfc-7636-for-pkce-code-verifiers-used")]  // Too long
    public void GenerateCodeChallenge_WithInvalidLength_ThrowsArgumentException(string verifier)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => PkceGenerator.GenerateCodeChallenge(verifier));
        Assert.Contains("length", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("invalid@characters-in-verifier-for-pkce-test")]  // Contains @
    [InlineData("contains space and other invalid chars!!!!!!!!")]  // Contains space and !
    [InlineData("has#hash$dollar%percent&ampersand*asterisk()")]  // Various invalid chars
    public void GenerateCodeChallenge_WithInvalidCharacters_ThrowsArgumentException(string verifier)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => PkceGenerator.GenerateCodeChallenge(verifier));
        Assert.Contains("invalid characters", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateCodeChallenge_MatchesRfc7636TestVector()
    {
        // Arrange - Test vector from RFC 7636 Appendix B
        const string verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";

        // Expected challenge from RFC 7636:
        // BASE64URL(SHA256(ASCII(code_verifier)))
        const string expectedChallenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM";

        // Act
        var actualChallenge = PkceGenerator.GenerateCodeChallenge(verifier);

        // Assert
        Assert.Equal(expectedChallenge, actualChallenge);
    }

    [Fact]
    public void GeneratePkceParameters_GeneratesCompleteParameterSet()
    {
        // Act
        var (verifier, challenge) = PkceGenerator.GeneratePkceParameters();

        // Assert
        Assert.NotNull(verifier);
        Assert.NotNull(challenge);
        Assert.Equal(PkceGenerator.DefaultVerifierLength, verifier.Length);
        Assert.Equal(43, challenge.Length);
    }

    [Fact]
    public void GeneratePkceParameters_VerifierAndChallengeAreRelated()
    {
        // Act
        var (verifier, challenge) = PkceGenerator.GeneratePkceParameters();
        var recomputedChallenge = PkceGenerator.GenerateCodeChallenge(verifier);

        // Assert
        Assert.Equal(challenge, recomputedChallenge);
    }

    [Theory]
    [InlineData(43)]
    [InlineData(64)]
    [InlineData(128)]
    public void GeneratePkceParameters_WithSpecificLength_GeneratesCorrectLength(int length)
    {
        // Act
        var (verifier, challenge) = PkceGenerator.GeneratePkceParameters(length);

        // Assert
        Assert.Equal(length, verifier.Length);
        Assert.Equal(43, challenge.Length);
    }

    [Theory]
    [InlineData(42)]
    [InlineData(129)]
    public void GeneratePkceParameters_WithInvalidLength_ThrowsArgumentOutOfRangeException(int length)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PkceGenerator.GeneratePkceParameters(length));
    }

    [Fact]
    public void GeneratePkceParameters_GeneratesUniqueParameters()
    {
        // Arrange
        var parameters = new HashSet<(string, string)>();

        // Act - Generate 100 parameter sets
        for (int i = 0; i < 100; i++)
        {
            parameters.Add(PkceGenerator.GeneratePkceParameters());
        }

        // Assert - All should be unique
        Assert.Equal(100, parameters.Count);
    }

    [Fact]
    public void ChallengeMethod_ReturnsS256()
    {
        // Act
        var method = PkceGenerator.ChallengeMethod;

        // Assert
        Assert.Equal("S256", method);
    }

    [Fact]
    public void Constants_HaveCorrectValues()
    {
        // Assert
        Assert.Equal(43, PkceGenerator.MinVerifierLength);
        Assert.Equal(128, PkceGenerator.MaxVerifierLength);
        Assert.Equal(64, PkceGenerator.DefaultVerifierLength);
    }

    [Fact]
    public void GenerateCodeVerifier_IsCryptographicallyRandom()
    {
        // Arrange - Generate multiple verifiers and check distribution
        var verifiers = new List<string>();
        var charFrequency = new Dictionary<char, int>();

        // Act - Generate 1000 verifiers
        for (int i = 0; i < 1000; i++)
        {
            var verifier = PkceGenerator.GenerateCodeVerifier();
            verifiers.Add(verifier);

            foreach (var c in verifier)
            {
                charFrequency[c] = charFrequency.GetValueOrDefault(c, 0) + 1;
            }
        }

        // Assert - Check that we have good distribution of characters
        // With 1000 verifiers of 64 chars each = 64000 chars total
        // 66 possible chars, so average should be ~970 per char
        // We'll check that no char appears less than 500 or more than 1500 times
        // (very lenient bounds to avoid flaky tests while still catching obvious issues)
        Assert.All(charFrequency.Values, count =>
        {
            Assert.InRange(count, 500, 1500);
        });

        // All verifiers should be unique
        Assert.Equal(verifiers.Count, verifiers.Distinct().Count());
    }

    [Fact]
    public void GenerateCodeChallenge_DifferentVerifiers_ProduceDifferentChallenges()
    {
        // Arrange
        var verifier1 = PkceGenerator.GenerateCodeVerifier();
        var verifier2 = PkceGenerator.GenerateCodeVerifier();

        // Ensure verifiers are different
        Assert.NotEqual(verifier1, verifier2);

        // Act
        var challenge1 = PkceGenerator.GenerateCodeChallenge(verifier1);
        var challenge2 = PkceGenerator.GenerateCodeChallenge(verifier2);

        // Assert
        Assert.NotEqual(challenge1, challenge2);
    }

    [Fact]
    public void GenerateCodeChallenge_SimilarVerifiers_ProduceDifferentChallenges()
    {
        // Arrange - Create verifiers that differ by only one character
        const string baseVerifier = "a234567890123456789012345678901234567890123";
        const string verifier1 = baseVerifier;
        const string verifier2 = "b234567890123456789012345678901234567890123";

        Assert.Equal(verifier1.Length, verifier2.Length);
        Assert.Equal(43, verifier1.Length);

        // Act
        var challenge1 = PkceGenerator.GenerateCodeChallenge(verifier1);
        var challenge2 = PkceGenerator.GenerateCodeChallenge(verifier2);

        // Assert - Even one character difference should produce very different challenges
        Assert.NotEqual(challenge1, challenge2);
    }
}
