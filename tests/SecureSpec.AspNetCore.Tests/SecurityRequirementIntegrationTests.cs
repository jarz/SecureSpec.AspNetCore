using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Security;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Integration tests demonstrating real-world security requirement scenarios.
/// </summary>
public class SecurityRequirementIntegrationTests
{
    [Fact]
    public void GlobalSecurityWithAlternatives_WorksCorrectly()
    {
        // Arrange - Configure global security with alternatives (OR)
        var options = new SecureSpecOptions();
        options.Security.AddHttpBearer("bearerAuth");
        options.Security.AddApiKeyHeader("apiKey", b => b.WithName("X-API-Key"));

        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test API", Version = "1.0" },
            Components = new OpenApiComponents
            {
                SecuritySchemes = options.Security.Schemes.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value)
            }
        };

        // Act - Set global security: Bearer OR API Key
        document.SecurityRequirements = new List<OpenApiSecurityRequirement>
        {
            new SecurityRequirementBuilder()
                .AddScheme("bearerAuth")
                .Build(),
            new SecurityRequirementBuilder()
                .AddScheme("apiKey")
                .Build()
        };

        // Assert
        Assert.Equal(2, document.SecurityRequirements.Count);
        foreach (var req in document.SecurityRequirements)
        {
            Assert.Single(req);
        }
    }

    [Fact]
    public void CompositeAuthentication_RequiresBothSchemes()
    {
        // Arrange - Configure schemes
        var options = new SecureSpecOptions();
        options.Security.AddApiKeyHeader("appKey", b => b.WithName("X-App-Key"));
        options.Security.AddOAuth2ClientCredentials("oauth2", b =>
        {
            b.WithTokenUrl(new Uri("https://auth.example.com/token"));
            b.AddScope("user:read", "Read user data");
        });

        // Act - Create composite requirement: API Key AND OAuth2 (both required)
        var requirement = new SecurityRequirementBuilder()
            .AddScheme("appKey")
            .AddScheme("oauth2", "user:read")
            .Build();

        // Assert - Both schemes are in the same requirement
        Assert.Equal(2, requirement.Count);
        Assert.Contains(requirement.Keys, s => s.Reference.Id == "appKey");
        Assert.Contains(requirement.Keys, s => s.Reference.Id == "oauth2");

        // Verify OAuth2 has the scope
        var oauth2Scheme = requirement.Keys.First(s => s.Reference.Id == "oauth2");
        Assert.Single(requirement[oauth2Scheme]);
        Assert.Equal("user:read", requirement[oauth2Scheme][0]);
    }

    [Fact]
    public void OperationLevelOverride_ReplacesGlobalSecurity()
    {
        // Arrange - Global security
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test API", Version = "1.0" },
            SecurityRequirements = new List<OpenApiSecurityRequirement>
            {
                new SecurityRequirementBuilder()
                    .AddScheme("bearerAuth")
                    .Build()
            }
        };

        // Create an operation that overrides global security
        var operation = new OpenApiOperation
        {
            OperationId = "publicEndpoint",
            Summary = "Public endpoint with no authentication",
            // Empty array = no authentication required
            Security = new List<OpenApiSecurityRequirement>()
        };

        // Assert - Document has security, operation has none
        Assert.Single(document.SecurityRequirements);
        Assert.Empty(operation.Security);
    }

    [Fact]
    public void OperationLevelOverride_WithDifferentScheme()
    {
        // Arrange - Global security uses Bearer
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test API", Version = "1.0" },
            SecurityRequirements = new List<OpenApiSecurityRequirement>
            {
                new SecurityRequirementBuilder()
                    .AddScheme("bearerAuth")
                    .Build()
            }
        };

        // Create an operation that requires API Key instead
        var adminOperation = new OpenApiOperation
        {
            OperationId = "adminEndpoint",
            Summary = "Admin endpoint requiring API key",
            Security = new List<OpenApiSecurityRequirement>
            {
                new SecurityRequirementBuilder()
                    .AddScheme("adminApiKey")
                    .Build()
            }
        };

        // Assert - Different schemes at different levels
        Assert.Equal("bearerAuth", document.SecurityRequirements[0].Keys.First().Reference.Id);
        Assert.Equal("adminApiKey", adminOperation.Security[0].Keys.First().Reference.Id);
    }

    [Fact]
    public void ComplexMixedScenario_ANDWithinORAcross()
    {
        // Arrange - Real-world scenario with multiple options
        var requirements = new List<OpenApiSecurityRequirement>
        {
            // Option 1: Standard user authentication (Bearer token)
            new SecurityRequirementBuilder()
                .AddScheme("bearerAuth")
                .Build(),

            // Option 2: Service-to-service (API Key + OAuth2 Client Credentials)
            new SecurityRequirementBuilder()
                .AddScheme("serviceKey")
                .AddScheme("oauth2ClientCreds", "service:call")
                .Build(),

            // Option 3: Internal network (Mutual TLS only)
            new SecurityRequirementBuilder()
                .AddScheme("mutualTLS")
                .Build()
        };

        // Assert
        Assert.Equal(3, requirements.Count);

        // Option 1: Single scheme
        Assert.Single(requirements[0]);
        Assert.Equal("bearerAuth", requirements[0].Keys.First().Reference.Id);

        // Option 2: Two schemes (AND)
        Assert.Equal(2, requirements[1].Count);
        Assert.Contains(requirements[1].Keys, s => s.Reference.Id == "serviceKey");
        Assert.Contains(requirements[1].Keys, s => s.Reference.Id == "oauth2ClientCreds");

        // Option 3: Single scheme
        Assert.Single(requirements[2]);
        Assert.Equal("mutualTLS", requirements[2].Keys.First().Reference.Id);
    }

    [Fact]
    public void OAuth2WithScopes_MultipleScopes()
    {
        // Arrange & Act - OAuth2 with multiple scopes
        var requirement = new SecurityRequirementBuilder()
            .AddScheme("oauth2", "read:users", "write:users", "delete:users")
            .Build();

        // Assert
        var oauth2Scheme = requirement.Keys.First();
        Assert.Equal("oauth2", oauth2Scheme.Reference.Id);
        Assert.Equal(3, requirement[oauth2Scheme].Count);
        Assert.Contains("read:users", requirement[oauth2Scheme]);
        Assert.Contains("write:users", requirement[oauth2Scheme]);
        Assert.Contains("delete:users", requirement[oauth2Scheme]);
    }

    [Fact]
    public void MultipleOAuth2WithDifferentScopes_ORSemantics()
    {
        // Arrange & Act - Different scope requirements for different access levels
        var requirements = new List<OpenApiSecurityRequirement>
        {
            // Regular users need read permission
            new SecurityRequirementBuilder()
                .AddScheme("oauth2", "read:data")
                .Build(),

            // Power users need read and write
            new SecurityRequirementBuilder()
                .AddScheme("oauth2", "read:data", "write:data")
                .Build(),

            // Admins need all permissions
            new SecurityRequirementBuilder()
                .AddScheme("oauth2", "read:data", "write:data", "admin:all")
                .Build()
        };

        // Assert
        Assert.Equal(3, requirements.Count);

        // Regular user scopes
        var regularScopes = requirements[0][requirements[0].Keys.First()];
        Assert.Single(regularScopes);
        Assert.Equal("read:data", regularScopes[0]);

        // Power user scopes
        var powerScopes = requirements[1][requirements[1].Keys.First()];
        Assert.Equal(2, powerScopes.Count);

        // Admin scopes
        var adminScopes = requirements[2][requirements[2].Keys.First()];
        Assert.Equal(3, adminScopes.Count);
    }

    [Fact]
    public void RealWorldAPIScenario_MixedAuthStrategies()
    {
        // This test demonstrates a real API with different security for different endpoints

        // Arrange - Configure all security schemes
        var options = new SecureSpecOptions();
        options.Security.AddHttpBearer("userAuth", b =>
            b.WithBearerFormat("JWT")
             .WithDescription("User JWT token"));

        options.Security.AddApiKeyHeader("serviceKey", b =>
            b.WithName("X-Service-Key")
             .WithDescription("Service-to-service API key"));

        options.Security.AddOAuth2ClientCredentials("oauth2", b =>
        {
            b.WithTokenUrl(new Uri("https://auth.example.com/token"));
            b.AddScope("api:read", "Read API data");
            b.AddScope("api:write", "Write API data");
            b.AddScope("api:admin", "Admin operations");
        });

        // Global security - Accept user JWT or service key
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Multi-Auth API", Version = "1.0" },
            SecurityRequirements = new List<OpenApiSecurityRequirement>
            {
                new SecurityRequirementBuilder()
                    .AddScheme("userAuth")
                    .Build(),
                new SecurityRequirementBuilder()
                    .AddScheme("serviceKey")
                    .Build()
            }
        };

        // Public endpoint - no auth
        var publicOperation = new OpenApiOperation
        {
            OperationId = "getPublicData",
            Security = new List<OpenApiSecurityRequirement>()
        };

        // Admin endpoint - requires user auth with admin scope OR service key + OAuth2
        var adminOperation = new OpenApiOperation
        {
            OperationId = "adminAction",
            Security = new List<OpenApiSecurityRequirement>
            {
                new SecurityRequirementBuilder()
                    .AddScheme("userAuth")
                    .AddScheme("oauth2", "api:admin")
                    .Build(),
                new SecurityRequirementBuilder()
                    .AddScheme("serviceKey")
                    .AddScheme("oauth2", "api:admin")
                    .Build()
            }
        };

        // Assert
        // Global: 2 options (OR)
        Assert.Equal(2, document.SecurityRequirements.Count);

        // Public: no security
        Assert.Empty(publicOperation.Security);

        // Admin: 2 options (OR), each with 2 schemes (AND)
        Assert.Equal(2, adminOperation.Security.Count);
        foreach (var req in adminOperation.Security)
        {
            Assert.Equal(2, req.Count);
        }

        // Verify admin requirement has OAuth2 with admin scope
        foreach (var req in adminOperation.Security)
        {
            var oauth2 = req.Keys.First(s => s.Reference.Id == "oauth2");
            Assert.Contains("api:admin", req[oauth2]);
        }
    }

    [Fact]
    public void DocumentationExample_BasicUsage()
    {
        // This matches the example from SECURITY_REQUIREMENTS.md

        // Arrange
        var options = new SecureSpecOptions();
        options.Security.AddHttpBearer("bearerAuth", b =>
            b.WithBearerFormat("JWT")
             .WithDescription("JWT token authentication"));

        options.Security.AddApiKeyHeader("apiKey", b =>
            b.WithName("X-API-Key")
             .WithDescription("API key in header"));

        // Act - Create document with OR semantics
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "My API", Version = "1.0" },
            SecurityRequirements = new List<OpenApiSecurityRequirement>
            {
                new SecurityRequirementBuilder()
                    .AddScheme("bearerAuth")
                    .Build(),
                new SecurityRequirementBuilder()
                    .AddScheme("apiKey")
                    .Build()
            }
        };

        // Assert - Matches documentation example
        Assert.Equal(2, document.SecurityRequirements.Count);
        Assert.Equal("bearerAuth", document.SecurityRequirements[0].Keys.First().Reference.Id);
        Assert.Equal("apiKey", document.SecurityRequirements[1].Keys.First().Reference.Id);
    }
}
