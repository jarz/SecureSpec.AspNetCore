using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Security;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for SecurityRequirementBuilder to verify proper AND/OR semantics.
/// </summary>
public class SecurityRequirementBuilderTests
{
    [Fact]
    public void AddScheme_WithSchemeName_CreatesRequirementWithSchemeReference()
    {
        // Arrange
        var builder = new SecurityRequirementBuilder();

        // Act
        var requirement = builder
            .AddScheme("bearerAuth")
            .Build();

        // Assert
        Assert.Single(requirement);
        var scheme = requirement.Keys.First();
        Assert.NotNull(scheme.Reference);
        Assert.Equal("bearerAuth", scheme.Reference.Id);
        Assert.Equal(ReferenceType.SecurityScheme, scheme.Reference.Type);
        Assert.Empty(requirement[scheme]);
    }

    [Fact]
    public void AddScheme_WithScopes_CreatesRequirementWithScopes()
    {
        // Arrange
        var builder = new SecurityRequirementBuilder();

        // Act
        var requirement = builder
            .AddScheme("oauth2", "read", "write")
            .Build();

        // Assert
        Assert.Single(requirement);
        var scheme = requirement.Keys.First();
        Assert.Equal("oauth2", scheme.Reference.Id);
        Assert.Collection(requirement[scheme],
            scope => Assert.Equal("read", scope),
            scope => Assert.Equal("write", scope));
    }

    [Fact]
    public void AddScheme_WithNullSchemeName_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SecurityRequirementBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.AddScheme((string)null!));
    }

    [Fact]
    public void AddScheme_WithEmptySchemeName_ThrowsArgumentException()
    {
        // Arrange
        var builder = new SecurityRequirementBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.AddScheme(""));
    }

    [Fact]
    public void AddScheme_WithWhitespaceSchemeName_ThrowsArgumentException()
    {
        // Arrange
        var builder = new SecurityRequirementBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.AddScheme("   "));
    }

    [Fact]
    public void AddScheme_WithSchemeObject_CreatesRequirement()
    {
        // Arrange
        var builder = new SecurityRequirementBuilder();
        var scheme = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "customScheme"
            }
        };

        // Act
        var requirement = builder
            .AddScheme(scheme)
            .Build();

        // Assert
        Assert.Single(requirement);
        Assert.Contains(scheme, requirement.Keys);
        Assert.Empty(requirement[scheme]);
    }

    [Fact]
    public void AddScheme_WithSchemeObjectAndScopes_CreatesRequirementWithScopes()
    {
        // Arrange
        var builder = new SecurityRequirementBuilder();
        var scheme = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "oauth2"
            }
        };

        // Act
        var requirement = builder
            .AddScheme(scheme, "admin", "user")
            .Build();

        // Assert
        Assert.Single(requirement);
        Assert.Contains(scheme, requirement.Keys);
        Assert.Collection(requirement[scheme],
            scope => Assert.Equal("admin", scope),
            scope => Assert.Equal("user", scope));
    }

    [Fact]
    public void AddScheme_WithNullSchemeObject_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new SecurityRequirementBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.AddScheme((OpenApiSecurityScheme)null!));
    }

    [Fact]
    public void AddScheme_WithSchemeObjectWithoutReference_ThrowsArgumentException()
    {
        // Arrange
        var builder = new SecurityRequirementBuilder();
        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer"
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => builder.AddScheme(scheme));
        Assert.Contains("must have a reference", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_WithNoSchemes_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new SecurityRequirementBuilder();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("At least one security scheme", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddScheme_MultipleSchemes_ImplementsANDSemantics()
    {
        // Arrange & Act - Adding multiple schemes to ONE builder creates AND relationship
        var requirement = new SecurityRequirementBuilder()
            .AddScheme("apiKey")
            .AddScheme("oauth2", "read")
            .Build();

        // Assert - Both schemes are in the SAME requirement object (AND logic)
        Assert.Equal(2, requirement.Count);

        var apiKeyScheme = requirement.Keys.First(s => s.Reference.Id == "apiKey");
        var oauth2Scheme = requirement.Keys.First(s => s.Reference.Id == "oauth2");

        Assert.Empty(requirement[apiKeyScheme]);
        Assert.Single(requirement[oauth2Scheme]);
        Assert.Equal("read", requirement[oauth2Scheme][0]);
    }

    [Fact]
    public void MultipleRequirements_ImplementsORSemantics()
    {
        // Arrange & Act - Multiple requirement objects in array creates OR relationship
        var requirements = new List<OpenApiSecurityRequirement>
        {
            new SecurityRequirementBuilder()
                .AddScheme("apiKey")
                .Build(),
            new SecurityRequirementBuilder()
                .AddScheme("oauth2", "read", "write")
                .Build()
        };

        // Assert - Two separate requirement objects (OR logic)
        Assert.Equal(2, requirements.Count);

        // First requirement: API Key only
        Assert.Single(requirements[0]);
        Assert.Equal("apiKey", requirements[0].Keys.First().Reference.Id);

        // Second requirement: OAuth2 only
        Assert.Single(requirements[1]);
        var oauth2Scheme = requirements[1].Keys.First();
        Assert.Equal("oauth2", oauth2Scheme.Reference.Id);
        Assert.Collection(requirements[1][oauth2Scheme],
            scope => Assert.Equal("read", scope),
            scope => Assert.Equal("write", scope));
    }

    [Fact]
    public void CreateAlternative_CreatesNewBuilderInstance()
    {
        // Act
        var builder1 = SecurityRequirementBuilder.CreateAlternative();
        var builder2 = SecurityRequirementBuilder.CreateAlternative();

        // Assert
        Assert.NotNull(builder1);
        Assert.NotNull(builder2);
        Assert.NotSame(builder1, builder2);
    }

    [Fact]
    public void ComplexScenario_ANDWithinORAcross_WorksCorrectly()
    {
        // Arrange & Act - Complex scenario demonstrating both AND and OR
        var requirements = new List<OpenApiSecurityRequirement>
        {
            // Option 1: API Key AND OAuth2 (both required)
            new SecurityRequirementBuilder()
                .AddScheme("apiKey")
                .AddScheme("oauth2", "read")
                .Build(),

            // Option 2: Mutual TLS alone
            new SecurityRequirementBuilder()
                .AddScheme("mutualTLS")
                .Build()
        };

        // Assert
        Assert.Equal(2, requirements.Count);

        // Option 1 has 2 schemes (AND)
        Assert.Equal(2, requirements[0].Count);
        Assert.Contains(requirements[0].Keys, s => s.Reference.Id == "apiKey");
        Assert.Contains(requirements[0].Keys, s => s.Reference.Id == "oauth2");

        // Option 2 has 1 scheme
        Assert.Single(requirements[1]);
        Assert.Equal("mutualTLS", requirements[1].Keys.First().Reference.Id);
    }

    [Fact]
    public void AddScheme_ChainedCalls_MaintainsFluentInterface()
    {
        // Arrange
        var builder = new SecurityRequirementBuilder();

        // Act
        var result = builder
            .AddScheme("scheme1")
            .AddScheme("scheme2", "scope1")
            .AddScheme("scheme3", "scope2", "scope3");

        // Assert
        Assert.Same(builder, result); // Fluent interface returns same instance

        var requirement = result.Build();
        Assert.Equal(3, requirement.Count);
    }

    [Fact]
    public void AddScheme_WithNullScopes_TreatsAsEmptyArray()
    {
        // Arrange
        var builder = new SecurityRequirementBuilder();

        // Act
        var requirement = builder
            .AddScheme("apiKey", null!)
            .Build();

        // Assert
        var scheme = requirement.Keys.First();
        Assert.Empty(requirement[scheme]);
    }

    [Fact]
    public void AddScheme_WithEmptyScopes_CreatesEmptyList()
    {
        // Arrange
        var builder = new SecurityRequirementBuilder();

        // Act
        var requirement = builder
            .AddScheme("bearerAuth", Array.Empty<string>())
            .Build();

        // Assert
        var scheme = requirement.Keys.First();
        Assert.Empty(requirement[scheme]);
    }

    [Fact]
    public void DocumentLevelSecurity_Example()
    {
        // This test demonstrates how security requirements would be used at document level

        // Arrange
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test API", Version = "1.0" },
            SecurityRequirements = new List<OpenApiSecurityRequirement>()
        };

        // Act - Add global security: Bearer OR API Key
        document.SecurityRequirements.Add(
            new SecurityRequirementBuilder()
                .AddScheme("bearerAuth")
                .Build());

        document.SecurityRequirements.Add(
            new SecurityRequirementBuilder()
                .AddScheme("apiKey")
                .Build());

        // Assert
        Assert.Equal(2, document.SecurityRequirements.Count);
        Assert.Single(document.SecurityRequirements[0]); // First option: Bearer only
        Assert.Single(document.SecurityRequirements[1]); // Second option: API Key only
    }

    [Fact]
    public void OperationLevelSecurity_OverridesGlobal()
    {
        // This test demonstrates operation-level security override

        // Arrange
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

        var operation = new OpenApiOperation
        {
            // Override with different security
            Security = new List<OpenApiSecurityRequirement>
            {
                new SecurityRequirementBuilder()
                    .AddScheme("apiKey")
                    .Build()
            }
        };

        // Assert
        Assert.Single(document.SecurityRequirements);
        Assert.Equal("bearerAuth", document.SecurityRequirements[0].Keys.First().Reference.Id);

        Assert.Single(operation.Security);
        Assert.Equal("apiKey", operation.Security[0].Keys.First().Reference.Id);
    }

    [Fact]
    public void EmptySecurityArray_MakesEndpointPublic()
    {
        // This test demonstrates making an endpoint public

        // Arrange & Act
        var operation = new OpenApiOperation
        {
            // Empty array means no authentication required
            Security = new List<OpenApiSecurityRequirement>()
        };

        // Assert
        Assert.Empty(operation.Security);
    }
}
