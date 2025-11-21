using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.Security;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for OperationSecurityProcessor to verify per-operation security override behavior.
/// Tests cover AC 464-468: override semantics, empty arrays, ordering, and mutation logging.
/// </summary>
#pragma warning disable CA1861 // Prefer 'static readonly' fields over constant array arguments - test arrays are intentionally inline
public class OperationSecurityProcessorTests
{
    private readonly DiagnosticsLogger _logger;
    private readonly OperationSecurityProcessor _processor;

    public OperationSecurityProcessorTests()
    {
        _logger = new DiagnosticsLogger();
        _processor = new OperationSecurityProcessor(_logger);
    }

    #region AC 464: Operation-level security overrides global (no merge)

    [Fact]
    public void ApplySecurityRequirements_OperationWithSecurity_OverridesGlobal()
    {
        // Arrange
        var globalSecurity = new List<OpenApiSecurityRequirement>
        {
            CreateSecurityRequirement("bearerAuth")
        };

        var operation = new OpenApiOperation
        {
            OperationId = "testOp",
            Security = new List<OpenApiSecurityRequirement>
            {
                CreateSecurityRequirement("apiKey")
            }
        };

        // Act
        _processor.ApplySecurityRequirements(operation, globalSecurity, "testOp");

        // Assert - Operation security should completely replace global (no merge)
        Assert.Single(operation.Security);
        var scheme = operation.Security[0].Keys.First();
        Assert.Equal("apiKey", scheme.Reference.Id);

        // Global security should not be merged
        Assert.DoesNotContain(operation.Security[0].Keys, s => s.Reference.Id == "bearerAuth");
    }

    [Fact]
    public void ApplySecurityRequirements_OperationWithMultipleSecurity_OverridesGlobal()
    {
        // Arrange
        var globalSecurity = new List<OpenApiSecurityRequirement>
        {
            CreateSecurityRequirement("bearerAuth")
        };

        var operation = new OpenApiOperation
        {
            OperationId = "testOp",
            Security = new List<OpenApiSecurityRequirement>
            {
                CreateSecurityRequirement("apiKey"),
                CreateSecurityRequirement("oauth2", "read", "write")
            }
        };

        // Act
        _processor.ApplySecurityRequirements(operation, globalSecurity, "testOp");

        // Assert - Operation security completely replaces global
        Assert.Equal(2, operation.Security.Count);
        Assert.Equal("apiKey", operation.Security[0].Keys.First().Reference.Id);
        Assert.Equal("oauth2", operation.Security[1].Keys.First().Reference.Id);

        // Global security should not be present
        Assert.DoesNotContain(operation.Security[0].Keys, s => s.Reference.Id == "bearerAuth");
        Assert.DoesNotContain(operation.Security[1].Keys, s => s.Reference.Id == "bearerAuth");
    }

    #endregion

    #region AC 465: Empty operation security array clears global requirements

    [Fact]
    public void ApplySecurityRequirements_EmptyOperationSecurity_ClearsGlobal()
    {
        // Arrange
        var globalSecurity = new List<OpenApiSecurityRequirement>
        {
            CreateSecurityRequirement("bearerAuth"),
            CreateSecurityRequirement("apiKey")
        };

        var operation = new OpenApiOperation
        {
            OperationId = "publicEndpoint",
            Security = new List<OpenApiSecurityRequirement>() // Empty array
        };

        // Act
        _processor.ApplySecurityRequirements(operation, globalSecurity, "publicEndpoint");

        // Assert - Empty array means no authentication required
        Assert.NotNull(operation.Security);
        Assert.Empty(operation.Security);
    }

    [Fact]
    public void ApplySecurityRequirements_EmptyOperationSecurityWithGlobal_LogsMutation()
    {
        // Arrange
        var globalSecurity = new List<OpenApiSecurityRequirement>
        {
            CreateSecurityRequirement("bearerAuth")
        };

        var operation = new OpenApiOperation
        {
            OperationId = "publicEndpoint",
            Security = new List<OpenApiSecurityRequirement>()
        };

        // Act
        _processor.ApplySecurityRequirements(operation, globalSecurity, "publicEndpoint");

        // Assert - Should log mutation
        var diagnostics = _logger.GetEvents();
        Assert.Contains(diagnostics, d =>
            d.Code == DiagnosticCodes.SecurityRequirementsMutated &&
            d.Message.Contains("cleared global security requirements", StringComparison.Ordinal));
    }

    [Fact]
    public void ApplySecurityRequirements_EmptyOperationSecurityNoGlobal_DoesNotLogMutation()
    {
        // Arrange - No global security
        var operation = new OpenApiOperation
        {
            OperationId = "publicEndpoint",
            Security = new List<OpenApiSecurityRequirement>()
        };

        // Act
        _processor.ApplySecurityRequirements(operation, null, "publicEndpoint");

        // Assert - Should not log mutation when there was no global security to override
        var diagnostics = _logger.GetEvents();
        Assert.DoesNotContain(diagnostics, d => d.Code == DiagnosticCodes.SecurityRequirementsMutated);
    }

    #endregion

    #region AC 466: Security arrays ordering lexical by scheme key

    [Fact]
    public void ApplySecurityRequirements_MultipleSchemes_OrdersLexically()
    {
        // Arrange - Schemes added in reverse alphabetical order
        var requirement = new OpenApiSecurityRequirement
        {
            [CreateScheme("zulu")] = new List<string>(),
            [CreateScheme("alpha")] = new List<string>(),
            [CreateScheme("mike")] = new List<string>()
        };

        var operation = new OpenApiOperation
        {
            OperationId = "testOp",
            Security = new List<OpenApiSecurityRequirement> { requirement }
        };

        // Act
        _processor.ApplySecurityRequirements(operation, null, "testOp");

        // Assert - Schemes should be ordered lexically
        var orderedSchemes = operation.Security[0].Keys.Select(s => s.Reference.Id).ToList();
        Assert.Equal(new[] { "alpha", "mike", "zulu" }, orderedSchemes);
    }

    [Fact]
    public void ApplySecurityRequirements_GlobalSecurity_OrdersSchemesLexically()
    {
        // Arrange
        var globalRequirement = new OpenApiSecurityRequirement
        {
            [CreateScheme("oauth2")] = new List<string> { "read" },
            [CreateScheme("apiKey")] = new List<string>(),
            [CreateScheme("bearerAuth")] = new List<string>()
        };

        var globalSecurity = new List<OpenApiSecurityRequirement> { globalRequirement };

        var operation = new OpenApiOperation
        {
            OperationId = "testOp",
            Security = null // Will inherit from global
        };

        // Act
        _processor.ApplySecurityRequirements(operation, globalSecurity, "testOp");

        // Assert - Inherited global security should have schemes ordered lexically
        Assert.NotNull(operation.Security);
        Assert.Single(operation.Security);
        var orderedSchemes = operation.Security[0].Keys.Select(s => s.Reference.Id).ToList();
        Assert.Equal(new[] { "apiKey", "bearerAuth", "oauth2" }, orderedSchemes);
    }

    [Fact]
    public void OrderSecurityRequirements_StaticMethod_OrdersSchemesLexically()
    {
        // Arrange
        var requirement = new OpenApiSecurityRequirement
        {
            [CreateScheme("zulu")] = new List<string>(),
            [CreateScheme("alpha")] = new List<string>()
        };

        var requirements = new List<OpenApiSecurityRequirement> { requirement };

        // Act
        var ordered = OperationSecurityProcessor.OrderSecurityRequirements(requirements);

        // Assert
        var orderedSchemes = ordered[0].Keys.Select(s => s.Reference.Id).ToList();
        Assert.Equal(new[] { "alpha", "zulu" }, orderedSchemes);
    }

    #endregion

    #region AC 467: Multiple operation security objects preserve declaration order

    [Fact]
    public void ApplySecurityRequirements_MultipleRequirements_PreservesDeclarationOrder()
    {
        // Arrange - Multiple security requirements (OR logic)
        var operation = new OpenApiOperation
        {
            OperationId = "testOp",
            Security = new List<OpenApiSecurityRequirement>
            {
                CreateSecurityRequirement("option1"),
                CreateSecurityRequirement("option2"),
                CreateSecurityRequirement("option3")
            }
        };

        // Act
        _processor.ApplySecurityRequirements(operation, null, "testOp");

        // Assert - Declaration order of requirements should be preserved
        Assert.Equal(3, operation.Security.Count);
        Assert.Equal("option1", operation.Security[0].Keys.First().Reference.Id);
        Assert.Equal("option2", operation.Security[1].Keys.First().Reference.Id);
        Assert.Equal("option3", operation.Security[2].Keys.First().Reference.Id);
    }

    [Fact]
    public void ApplySecurityRequirements_MultipleRequirementsWithMultipleSchemes_PreservesRequirementOrderAndOrdersSchemes()
    {
        // Arrange - Multiple requirements, each with multiple schemes in non-alphabetical order
        var req1 = new OpenApiSecurityRequirement
        {
            [CreateScheme("zulu")] = new List<string>(),
            [CreateScheme("alpha")] = new List<string>()
        };

        var req2 = new OpenApiSecurityRequirement
        {
            [CreateScheme("yankee")] = new List<string>(),
            [CreateScheme("bravo")] = new List<string>()
        };

        var operation = new OpenApiOperation
        {
            OperationId = "testOp",
            Security = new List<OpenApiSecurityRequirement> { req1, req2 }
        };

        // Act
        _processor.ApplySecurityRequirements(operation, null, "testOp");

        // Assert - Requirement order preserved, but schemes within each are ordered
        Assert.Equal(2, operation.Security.Count);

        // First requirement: alpha, zulu (ordered)
        var firstReqSchemes = operation.Security[0].Keys.Select(s => s.Reference.Id).ToList();
        Assert.Equal(new[] { "alpha", "zulu" }, firstReqSchemes);

        // Second requirement: bravo, yankee (ordered)
        var secondReqSchemes = operation.Security[1].Keys.Select(s => s.Reference.Id).ToList();
        Assert.Equal(new[] { "bravo", "yankee" }, secondReqSchemes);
    }

    #endregion

    #region AC 468: Operation security mutation logged

    [Fact]
    public void ApplySecurityRequirements_OperationOverridesGlobal_LogsMutation()
    {
        // Arrange
        var globalSecurity = new List<OpenApiSecurityRequirement>
        {
            CreateSecurityRequirement("bearerAuth")
        };

        var operation = new OpenApiOperation
        {
            OperationId = "customAuth",
            Security = new List<OpenApiSecurityRequirement>
            {
                CreateSecurityRequirement("apiKey")
            }
        };

        // Act
        _processor.ApplySecurityRequirements(operation, globalSecurity, "customAuth");

        // Assert - Should log SEC002 diagnostic
        var diagnostics = _logger.GetEvents();
        Assert.Contains(diagnostics, d =>
            d.Code == DiagnosticCodes.SecurityRequirementsMutated &&
            d.Message.Contains("overrode global security requirements", StringComparison.Ordinal));
    }

    [Fact]
    public void ApplySecurityRequirements_OperationOverridesGlobal_LogsWithOperationId()
    {
        // Arrange
        var globalSecurity = new List<OpenApiSecurityRequirement>
        {
            CreateSecurityRequirement("bearerAuth")
        };

        var operation = new OpenApiOperation
        {
            OperationId = "GetUsers",
            Security = new List<OpenApiSecurityRequirement>
            {
                CreateSecurityRequirement("oauth2", "users.read")
            }
        };

        // Act
        _processor.ApplySecurityRequirements(operation, globalSecurity, "GetUsers");

        // Assert - Log should contain operation ID
        var diagnostics = _logger.GetEvents();
        var mutationLog = diagnostics.FirstOrDefault(d => d.Code == DiagnosticCodes.SecurityRequirementsMutated);
        Assert.NotNull(mutationLog);
        Assert.Contains("GetUsers", mutationLog.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplySecurityRequirements_NoGlobalSecurity_DoesNotLogMutation()
    {
        // Arrange - No global security
        var operation = new OpenApiOperation
        {
            OperationId = "testOp",
            Security = new List<OpenApiSecurityRequirement>
            {
                CreateSecurityRequirement("apiKey")
            }
        };

        // Act
        _processor.ApplySecurityRequirements(operation, null, "testOp");

        // Assert - Should not log mutation when there's no global security to override
        var diagnostics = _logger.GetEvents();
        Assert.DoesNotContain(diagnostics, d => d.Code == DiagnosticCodes.SecurityRequirementsMutated);
    }

    [Fact]
    public void ApplySecurityRequirements_InheritingGlobal_DoesNotLogMutation()
    {
        // Arrange
        var globalSecurity = new List<OpenApiSecurityRequirement>
        {
            CreateSecurityRequirement("bearerAuth")
        };

        var operation = new OpenApiOperation
        {
            OperationId = "testOp",
            Security = null // Will inherit from global
        };

        // Act
        _processor.ApplySecurityRequirements(operation, globalSecurity, "testOp");

        // Assert - Inheriting global security should not log mutation
        var diagnostics = _logger.GetEvents();
        Assert.DoesNotContain(diagnostics, d => d.Code == DiagnosticCodes.SecurityRequirementsMutated);
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public void ApplySecurityRequirements_NullOperation_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _processor.ApplySecurityRequirements(null!, null, "testOp"));
    }

    [Fact]
    public void ApplySecurityRequirements_NullOrEmptyOperationId_ThrowsArgumentException()
    {
        // Arrange
        var operation = new OpenApiOperation();

        // Act & Assert - null throws ArgumentNullException
        Assert.Throws<ArgumentNullException>(() =>
            _processor.ApplySecurityRequirements(operation, null, null!));

        // Empty and whitespace throw ArgumentException
        Assert.Throws<ArgumentException>(() =>
            _processor.ApplySecurityRequirements(operation, null, ""));

        Assert.Throws<ArgumentException>(() =>
            _processor.ApplySecurityRequirements(operation, null, "   "));
    }

    [Fact]
    public void ApplySecurityRequirements_NullOperationSecurity_InheritsGlobal()
    {
        // Arrange
        var globalSecurity = new List<OpenApiSecurityRequirement>
        {
            CreateSecurityRequirement("bearerAuth")
        };

        var operation = new OpenApiOperation
        {
            OperationId = "testOp",
            Security = null
        };

        // Act
        _processor.ApplySecurityRequirements(operation, globalSecurity, "testOp");

        // Assert - Should inherit from global
        Assert.NotNull(operation.Security);
        Assert.Single(operation.Security);
        Assert.Equal("bearerAuth", operation.Security[0].Keys.First().Reference.Id);
    }

    [Fact]
    public void ApplySecurityRequirements_NullGlobalAndNullOperation_CreatesEmptyList()
    {
        // Arrange
        var operation = new OpenApiOperation
        {
            OperationId = "testOp",
            Security = null
        };

        // Act
        _processor.ApplySecurityRequirements(operation, null, "testOp");

        // Assert - Should create empty list
        Assert.NotNull(operation.Security);
        Assert.Empty(operation.Security);
    }

    [Fact]
    public void ApplySecurityRequirements_EmptyGlobalAndNullOperation_CreatesEmptyList()
    {
        // Arrange
        var operation = new OpenApiOperation
        {
            OperationId = "testOp",
            Security = null
        };

        // Act
        _processor.ApplySecurityRequirements(operation, new List<OpenApiSecurityRequirement>(), "testOp");

        // Assert - Should create empty list
        Assert.NotNull(operation.Security);
        Assert.Empty(operation.Security);
    }

    [Fact]
    public void ApplySecurityRequirements_ComplexScenario_WorksCorrectly()
    {
        // Arrange - Global has 2 requirements, operation overrides with 3
        var globalSecurity = new List<OpenApiSecurityRequirement>
        {
            CreateSecurityRequirement("globalAuth1"),
            CreateSecurityRequirement("globalAuth2")
        };

        var req1 = new OpenApiSecurityRequirement
        {
            [CreateScheme("zulu")] = new List<string>(),
            [CreateScheme("alpha")] = new List<string>()
        };

        var req2 = new OpenApiSecurityRequirement
        {
            [CreateScheme("oauth2")] = new List<string> { "read", "write" }
        };

        var req3 = new OpenApiSecurityRequirement
        {
            [CreateScheme("beta")] = new List<string>()
        };

        var operation = new OpenApiOperation
        {
            OperationId = "ComplexOp",
            Security = new List<OpenApiSecurityRequirement> { req1, req2, req3 }
        };

        // Act
        _processor.ApplySecurityRequirements(operation, globalSecurity, "ComplexOp");

        // Assert
        Assert.Equal(3, operation.Security.Count);

        // First requirement: alpha, zulu (ordered)
        var req1Schemes = operation.Security[0].Keys.Select(s => s.Reference.Id).ToList();
        Assert.Equal(new[] { "alpha", "zulu" }, req1Schemes);

        // Second requirement: oauth2 (single scheme)
        Assert.Single(operation.Security[1]);
        Assert.Equal("oauth2", operation.Security[1].Keys.First().Reference.Id);

        // Third requirement: beta (single scheme)
        Assert.Single(operation.Security[2]);
        Assert.Equal("beta", operation.Security[2].Keys.First().Reference.Id);

        // Should have logged mutation
        var diagnostics = _logger.GetEvents();
        Assert.Contains(diagnostics, d => d.Code == DiagnosticCodes.SecurityRequirementsMutated);
    }

    [Fact]
    public void OrderSecurityRequirements_NullRequirements_ReturnsEmptyList()
    {
        // Act
        var result = OperationSecurityProcessor.OrderSecurityRequirements(null);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void OrderSecurityRequirements_EmptyRequirements_ReturnsEmptyList()
    {
        // Act
        var result = OperationSecurityProcessor.OrderSecurityRequirements(new List<OpenApiSecurityRequirement>());

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void ApplySecurityRequirements_SchemesWithScopes_PreservesScopes()
    {
        // Arrange
        var requirement = new OpenApiSecurityRequirement
        {
            [CreateScheme("oauth2")] = new List<string> { "read", "write", "admin" },
            [CreateScheme("apiKey")] = new List<string>()
        };

        var operation = new OpenApiOperation
        {
            OperationId = "testOp",
            Security = new List<OpenApiSecurityRequirement> { requirement }
        };

        // Act
        _processor.ApplySecurityRequirements(operation, null, "testOp");

        // Assert - Scopes should be preserved
        var oauth2Scheme = operation.Security[0].Keys.First(s => s.Reference.Id == "oauth2");
        var scopes = operation.Security[0][oauth2Scheme];
        Assert.Equal(new[] { "read", "write", "admin" }, scopes);
    }

    #endregion

    #region Helper Methods

    private static OpenApiSecurityRequirement CreateSecurityRequirement(string schemeName, params string[] scopes)
    {
        return new OpenApiSecurityRequirement
        {
            [CreateScheme(schemeName)] = new List<string>(scopes)
        };
    }

    private static OpenApiSecurityScheme CreateScheme(string name)
    {
        return new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = name
            }
        };
    }

    #endregion
}
