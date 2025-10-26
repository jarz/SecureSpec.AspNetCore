using SecureSpec.AspNetCore.Security;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for CSRF token management with double-submit cookie pattern and rotation.
/// </summary>
public class CsrfTokenManagerTests
{
    [Fact]
    public void Constructor_WithDefaultParameters_CreatesManager()
    {
        // Act
        var manager = new CsrfTokenManager();

        // Assert
        Assert.NotNull(manager);
        Assert.Equal(0, manager.ActiveTokenCount);
    }

    [Theory]
    [InlineData(16)]  // Minimum
    [InlineData(32)]  // Default
    [InlineData(64)]  // Maximum
    public void Constructor_WithValidTokenLength_CreatesManager(int tokenLength)
    {
        // Act
        var manager = new CsrfTokenManager(tokenLength);

        // Assert
        Assert.NotNull(manager);
        Assert.Equal(0, manager.ActiveTokenCount);
    }

    [Theory]
    [InlineData(15)]  // Below minimum
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65)]  // Above maximum
    [InlineData(100)]
    public void Constructor_WithInvalidTokenLength_ThrowsArgumentOutOfRangeException(int tokenLength)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new CsrfTokenManager(tokenLength));
        Assert.Equal("tokenLength", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithCustomLifetime_CreatesManager()
    {
        // Arrange
        var lifetime = TimeSpan.FromMinutes(30);

        // Act
        var manager = new CsrfTokenManager(tokenLifetime: lifetime);

        // Assert
        Assert.NotNull(manager);
    }

    [Fact]
    public void GenerateToken_WithValidState_GeneratesToken()
    {
        // Arrange
        var manager = new CsrfTokenManager();
        const string state = "test-state";

        // Act
        var token = manager.GenerateToken(state);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void GenerateToken_WithInvalidState_ThrowsArgumentException(string? state)
    {
        // Arrange
        var manager = new CsrfTokenManager();

        // Act & Assert
        if (state is null)
        {
            Assert.Throws<ArgumentNullException>(() => manager.GenerateToken(state!));
        }
        else
        {
            Assert.Throws<ArgumentException>(() => manager.GenerateToken(state));
        }
    }

    [Fact]
    public void GenerateToken_GeneratesUniqueTokens()
    {
        // Arrange
        var manager = new CsrfTokenManager();
        var tokens = new HashSet<string>();

        // Act - Generate 100 tokens
        for (int i = 0; i < 100; i++)
        {
            tokens.Add(manager.GenerateToken($"state-{i}"));
        }

        // Assert - All should be unique
        Assert.Equal(100, tokens.Count);
    }

    [Fact]
    public void GenerateToken_IncrementsActiveTokenCount()
    {
        // Arrange
        var manager = new CsrfTokenManager();

        // Act
        manager.GenerateToken("state1");
        manager.GenerateToken("state2");
        manager.GenerateToken("state3");

        // Assert
        Assert.Equal(3, manager.ActiveTokenCount);
    }

    [Fact]
    public void ValidateAndRotateToken_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var manager = new CsrfTokenManager();
        const string expectedState = "test-state";
        var token = manager.GenerateToken(expectedState);

        // Act
        var isValid = manager.ValidateAndRotateToken(token, out var actualState);

        // Assert
        Assert.True(isValid);
        Assert.Equal(expectedState, actualState);
    }

    [Fact]
    public void ValidateAndRotateToken_RemovesTokenAfterValidation()
    {
        // Arrange
        var manager = new CsrfTokenManager();
        const string state = "test-state";
        var token = manager.GenerateToken(state);

        // Act
        var firstValidation = manager.ValidateAndRotateToken(token, out var firstState);
        var secondValidation = manager.ValidateAndRotateToken(token, out var secondState);

        // Assert
        Assert.True(firstValidation);
        Assert.Equal(state, firstState);
        Assert.False(secondValidation);
        Assert.Null(secondState);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ValidateAndRotateToken_WithInvalidToken_ReturnsFalse(string? token)
    {
        // Arrange
        var manager = new CsrfTokenManager();

        // Act
        var isValid = manager.ValidateAndRotateToken(token, out var state);

        // Assert
        Assert.False(isValid);
        Assert.Null(state);
    }

    [Fact]
    public void ValidateAndRotateToken_WithNonexistentToken_ReturnsFalse()
    {
        // Arrange
        var manager = new CsrfTokenManager();

        // Act
        var isValid = manager.ValidateAndRotateToken("nonexistent-token", out var state);

        // Assert
        Assert.False(isValid);
        Assert.Null(state);
    }

    [Fact]
    public void ValidateAndRotateToken_WithExpiredToken_ReturnsFalse()
    {
        // Arrange
        var shortLifetime = TimeSpan.FromMilliseconds(50);
        var manager = new CsrfTokenManager(tokenLifetime: shortLifetime);
        var token = manager.GenerateToken("test-state");

        // Act - Wait for token to expire
        Thread.Sleep(100);
        var isValid = manager.ValidateAndRotateToken(token, out var state);

        // Assert
        Assert.False(isValid);
        Assert.Null(state);
    }

    [Fact]
    public void ValidateAndRotateToken_DecrementsActiveTokenCount()
    {
        // Arrange
        var manager = new CsrfTokenManager();
        var token = manager.GenerateToken("state");
        Assert.Equal(1, manager.ActiveTokenCount);

        // Act
        manager.ValidateAndRotateToken(token, out _);

        // Assert
        Assert.Equal(0, manager.ActiveTokenCount);
    }

    [Fact]
    public void ValidateToken_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var manager = new CsrfTokenManager();
        const string expectedState = "test-state";
        var token = manager.GenerateToken(expectedState);

        // Act
        var isValid = manager.ValidateToken(token, out var actualState);

        // Assert
        Assert.True(isValid);
        Assert.Equal(expectedState, actualState);
    }

    [Fact]
    public void ValidateToken_DoesNotRemoveToken()
    {
        // Arrange
        var manager = new CsrfTokenManager();
        const string state = "test-state";
        var token = manager.GenerateToken(state);

        // Act
        var firstValidation = manager.ValidateToken(token, out var firstState);
        var secondValidation = manager.ValidateToken(token, out var secondState);

        // Assert
        Assert.True(firstValidation);
        Assert.Equal(state, firstState);
        Assert.True(secondValidation);
        Assert.Equal(state, secondState);
        Assert.Equal(1, manager.ActiveTokenCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ValidateToken_WithInvalidToken_ReturnsFalse(string? token)
    {
        // Arrange
        var manager = new CsrfTokenManager();

        // Act
        var isValid = manager.ValidateToken(token, out var state);

        // Assert
        Assert.False(isValid);
        Assert.Null(state);
    }

    [Fact]
    public void ValidateToken_WithExpiredToken_ReturnsFalse()
    {
        // Arrange
        var shortLifetime = TimeSpan.FromMilliseconds(50);
        var manager = new CsrfTokenManager(tokenLifetime: shortLifetime);
        var token = manager.GenerateToken("test-state");

        // Act - Wait for token to expire
        Thread.Sleep(100);
        var isValid = manager.ValidateToken(token, out var state);

        // Assert
        Assert.False(isValid);
        Assert.Null(state);
    }

    [Fact]
    public void RemoveToken_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var manager = new CsrfTokenManager();
        var token = manager.GenerateToken("state");

        // Act
        var removed = manager.RemoveToken(token);

        // Assert
        Assert.True(removed);
        Assert.Equal(0, manager.ActiveTokenCount);
    }

    [Fact]
    public void RemoveToken_WithNonexistentToken_ReturnsFalse()
    {
        // Arrange
        var manager = new CsrfTokenManager();

        // Act
        var removed = manager.RemoveToken("nonexistent-token");

        // Assert
        Assert.False(removed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void RemoveToken_WithInvalidToken_ReturnsFalse(string? token)
    {
        // Arrange
        var manager = new CsrfTokenManager();

        // Act
        var removed = manager.RemoveToken(token);

        // Assert
        Assert.False(removed);
    }

    [Fact]
    public void ClearAll_RemovesAllTokens()
    {
        // Arrange
        var manager = new CsrfTokenManager();
        manager.GenerateToken("state1");
        manager.GenerateToken("state2");
        manager.GenerateToken("state3");
        Assert.Equal(3, manager.ActiveTokenCount);

        // Act
        manager.ClearAll();

        // Assert
        Assert.Equal(0, manager.ActiveTokenCount);
    }

    [Fact]
    public void CleanupExpiredTokens_RemovesOnlyExpiredTokens()
    {
        // Arrange
        var manager = new CsrfTokenManager(tokenLifetime: TimeSpan.FromMilliseconds(100));
        var token1 = manager.GenerateToken("state1");  // Will expire
        Thread.Sleep(50);
        var token2 = manager.GenerateToken("state2");  // Will not expire yet
        Thread.Sleep(60);  // Total 110ms, token1 expired, token2 has 40ms left

        // Act
        manager.CleanupExpiredTokens();

        // Assert
        Assert.Equal(1, manager.ActiveTokenCount);
        Assert.False(manager.ValidateToken(token1, out _));
        Assert.True(manager.ValidateToken(token2, out _));
    }

    [Fact]
    public void ActiveTokenCount_AutomaticallyCleanupExpiredTokens()
    {
        // Arrange
        var manager = new CsrfTokenManager(tokenLifetime: TimeSpan.FromMilliseconds(50));
        manager.GenerateToken("state1");
        manager.GenerateToken("state2");
        Assert.Equal(2, manager.ActiveTokenCount);

        // Act - Wait for tokens to expire
        Thread.Sleep(100);
        var count = manager.ActiveTokenCount;

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void GenerateToken_TokensAreBase64Encoded()
    {
        // Arrange
        var manager = new CsrfTokenManager();

        // Act
        var token = manager.GenerateToken("state");

        // Assert
        Assert.NotNull(token);
        // Should be able to decode from Base64
        var bytes = Convert.FromBase64String(token);
        Assert.Equal(CsrfTokenManager.DefaultTokenLength, bytes.Length);
    }

    [Fact]
    public void GenerateToken_WithDifferentStates_GeneratesDifferentTokens()
    {
        // Arrange
        var manager = new CsrfTokenManager();

        // Act
        var token1 = manager.GenerateToken("state1");
        var token2 = manager.GenerateToken("state2");

        // Assert
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void GenerateToken_WithSameState_GeneratesDifferentTokens()
    {
        // Arrange
        var manager = new CsrfTokenManager();
        const string state = "same-state";

        // Act
        var token1 = manager.GenerateToken(state);
        var token2 = manager.GenerateToken(state);

        // Assert
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void ValidateAndRotateToken_ImplementsDoubleSubmitPattern()
    {
        // Arrange
        var manager = new CsrfTokenManager();
        const string state = "oauth-state";
        var csrfToken = manager.GenerateToken(state);

        // Act - Simulate double-submit: token in cookie and in OAuth callback
        var isValid = manager.ValidateAndRotateToken(csrfToken, out var retrievedState);

        // Assert
        Assert.True(isValid);
        Assert.Equal(state, retrievedState);
        // Token is rotated (removed)
        Assert.False(manager.ValidateToken(csrfToken, out _));
    }

    [Fact]
    public void Constants_HaveCorrectValues()
    {
        // Assert
        Assert.Equal(32, CsrfTokenManager.DefaultTokenLength);
        Assert.Equal(16, CsrfTokenManager.MinTokenLength);
        Assert.Equal(64, CsrfTokenManager.MaxTokenLength);
        Assert.Equal(TimeSpan.FromMinutes(15), CsrfTokenManager.DefaultTokenLifetime);
    }

    [Fact]
    public async Task ThreadSafety_ConcurrentGenerateAndValidate()
    {
        // Arrange
        var manager = new CsrfTokenManager();
        const int threadCount = 10;
        const int tokensPerThread = 100;
        var tokens = new System.Collections.Concurrent.ConcurrentBag<string>();

        // Act - Generate tokens concurrently
        var generateTasks = Enumerable.Range(0, threadCount).Select(i =>
            Task.Run(() =>
            {
                for (int j = 0; j < tokensPerThread; j++)
                {
                    var token = manager.GenerateToken($"state-{i}-{j}");
                    tokens.Add(token);
                }
            })
        ).ToArray();

        await Task.WhenAll(generateTasks);

        // Assert - All tokens should be unique and valid
        Assert.Equal(threadCount * tokensPerThread, tokens.Count);
        Assert.Equal(tokens.Count, tokens.Distinct().Count());

        // Validate all tokens concurrently
        var validationTasks = tokens.Select(token =>
            Task.Run(() =>
            {
                var isValid = manager.ValidateToken(token, out var state);
                Assert.True(isValid);
                Assert.NotNull(state);
            })
        ).ToArray();

        await Task.WhenAll(validationTasks);
    }

    [Fact]
    public void TokenRotation_PreventsReplayAttacks()
    {
        // Arrange
        var manager = new CsrfTokenManager();
        const string state = "critical-operation";
        var token = manager.GenerateToken(state);

        // Act - Attacker tries to replay the token
        var firstAttempt = manager.ValidateAndRotateToken(token, out var firstState);
        var replayAttempt = manager.ValidateAndRotateToken(token, out var replayState);

        // Assert
        Assert.True(firstAttempt);
        Assert.Equal(state, firstState);
        Assert.False(replayAttempt);  // Replay attack prevented
        Assert.Null(replayState);
    }
}
