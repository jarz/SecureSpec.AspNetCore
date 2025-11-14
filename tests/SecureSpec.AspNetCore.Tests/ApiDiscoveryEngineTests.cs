using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Core;
using SecureSpec.AspNetCore.Core.Attributes;
using SecureSpec.AspNetCore.Diagnostics;
using System.Reflection;
using OpenApiParameter = Microsoft.OpenApi.Models.OpenApiParameter;
using OpenApiSecurityRequirement = Microsoft.OpenApi.Models.OpenApiSecurityRequirement;

namespace SecureSpec.AspNetCore.Tests;

public class ApiDiscoveryEngineTests
{
    [Fact]
    public async Task DiscoverEndpointsAsync_WithNoStrategies_ReturnsEmptyCollection()
    {
        // Arrange
        var diagnosticsLogger = new DiagnosticsLogger();
        var metadataExtractor = CreateMetadataExtractor();
        var options = Options.Create(new SecureSpecOptions());
        var strategies = Array.Empty<IEndpointDiscoveryStrategy>();

        var engine = new ApiDiscoveryEngine(strategies, metadataExtractor, options, diagnosticsLogger);

        // Act
        var result = await engine.DiscoverEndpointsAsync();

        // Assert
        Assert.Empty(result);

        var events = diagnosticsLogger.GetEvents();
        Assert.Contains(events, e => e.Code == DiagnosticCodes.Discovery.EndpointFiltered && e.Message.Contains("No discovery strategies", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DiscoverEndpointsAsync_WithStrategies_AggregatesEndpoints()
    {
        // Arrange
        var diagnosticsLogger = new DiagnosticsLogger();
        var metadataExtractor = CreateMetadataExtractor();
        var options = Options.Create(new SecureSpecOptions());

        var endpoint1 = CreateTestEndpoint("GET", "/api/test1");
        var endpoint2 = CreateTestEndpoint("POST", "/api/test2");

        var strategy1 = new MockDiscoveryStrategy(new[] { endpoint1 });
        var strategy2 = new MockDiscoveryStrategy(new[] { endpoint2 });
        var strategies = new IEndpointDiscoveryStrategy[] { strategy1, strategy2 };

        var engine = new ApiDiscoveryEngine(strategies, metadataExtractor, options, diagnosticsLogger);

        // Act
        var result = await engine.DiscoverEndpointsAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task DiscoverEndpointsAsync_WithExcludeAttribute_FiltersEndpoint()
    {
        // Arrange
        var diagnosticsLogger = new DiagnosticsLogger();
        var metadataExtractor = CreateMetadataExtractor();
        var options = Options.Create(new SecureSpecOptions());

        var methodWithExclude = typeof(TestController).GetMethod(nameof(TestController.ExcludedMethod))!;
        var endpoint = CreateTestEndpoint("GET", "/api/excluded", methodInfo: methodWithExclude);

        var strategy = new MockDiscoveryStrategy(new[] { endpoint });
        var strategies = new IEndpointDiscoveryStrategy[] { strategy };

        var engine = new ApiDiscoveryEngine(strategies, metadataExtractor, options, diagnosticsLogger);

        // Act
        var result = await engine.DiscoverEndpointsAsync();

        // Assert
        Assert.Empty(result);

        var events = diagnosticsLogger.GetEvents();
        Assert.Contains(events, e => e.Code == DiagnosticCodes.Discovery.EndpointFiltered);
    }

    [Fact]
    public async Task DiscoverEndpointsAsync_WithIncludeAttribute_IncludesEndpoint()
    {
        // Arrange
        var diagnosticsLogger = new DiagnosticsLogger();
        var metadataExtractor = CreateMetadataExtractor();
        var secureSpecOptions = new SecureSpecOptions
        {
            Discovery =
            {
                IncludeOnlyApiControllers = true
            }
        };
        var options = Options.Create(secureSpecOptions);

        var methodWithInclude = typeof(NonApiController).GetMethod(nameof(NonApiController.IncludedMethod))!;
        var endpoint = CreateTestEndpoint("GET", "/api/included",
            methodInfo: methodWithInclude,
            controllerType: typeof(NonApiController));

        var strategy = new MockDiscoveryStrategy(new[] { endpoint });
        var strategies = new IEndpointDiscoveryStrategy[] { strategy };

        var engine = new ApiDiscoveryEngine(strategies, metadataExtractor, options, diagnosticsLogger);

        // Act
        var result = await engine.DiscoverEndpointsAsync();

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task DiscoverEndpointsAsync_WithIncludePredicate_AddsNonApiController()
    {
        // Arrange
        var diagnosticsLogger = new DiagnosticsLogger();
        var metadataExtractor = CreateMetadataExtractor();
        var secureSpecOptions = new SecureSpecOptions
        {
            Discovery =
            {
                IncludePredicate = endpoint => endpoint.ControllerType == typeof(NonApiController)
            }
        };
        var options = Options.Create(secureSpecOptions);

        var apiEndpoint = CreateTestEndpoint("GET", "/api/test", controllerType: typeof(TestController));
        var nonApiEndpoint = CreateTestEndpoint("GET", "/special/nonapi", controllerType: typeof(NonApiController));

        var strategy = new MockDiscoveryStrategy(new[] { apiEndpoint, nonApiEndpoint });
        var strategies = new IEndpointDiscoveryStrategy[] { strategy };

        var engine = new ApiDiscoveryEngine(strategies, metadataExtractor, options, diagnosticsLogger);

        // Act
        var result = await engine.DiscoverEndpointsAsync();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, e => e.RoutePattern == "/api/test");
        Assert.Contains(result, e => e.RoutePattern == "/special/nonapi");
    }

    [Fact]
    public async Task DiscoverEndpointsAsync_WithIncludePredicate_DoesNotExcludeConventionalMatches()
    {
        // Arrange
        var diagnosticsLogger = new DiagnosticsLogger();
        var metadataExtractor = CreateMetadataExtractor();
        var secureSpecOptions = new SecureSpecOptions
        {
            Discovery =
            {
                IncludePredicate = endpoint => endpoint.RoutePattern.Contains("special", StringComparison.Ordinal)
            }
        };
        var options = Options.Create(secureSpecOptions);

        var matchingEndpoint = CreateTestEndpoint("GET", "/api/controller/special", controllerType: typeof(TestController));
        var conventionalEndpoint = CreateTestEndpoint("GET", "/api/controller/regular", controllerType: typeof(TestController));

        var strategy = new MockDiscoveryStrategy(new[] { matchingEndpoint, conventionalEndpoint });
        var strategies = new IEndpointDiscoveryStrategy[] { strategy };

        var engine = new ApiDiscoveryEngine(strategies, metadataExtractor, options, diagnosticsLogger);

        // Act
        var result = await engine.DiscoverEndpointsAsync();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, e => e.RoutePattern == "/api/controller/special");
        Assert.Contains(result, e => e.RoutePattern == "/api/controller/regular");
    }

    [Fact]
    public async Task DiscoverEndpointsAsync_WithApiControllerConvention_FiltersCorrectly()
    {
        // Arrange
        var diagnosticsLogger = new DiagnosticsLogger();
        var metadataExtractor = CreateMetadataExtractor();
        var secureSpecOptions = new SecureSpecOptions
        {
            Discovery =
            {
                IncludeOnlyApiControllers = true
            }
        };
        var options = Options.Create(secureSpecOptions);

        var apiEndpoint = CreateTestEndpoint("GET", "/api/test", controllerType: typeof(TestController));
        var nonApiEndpoint = CreateTestEndpoint("GET", "/mvc/test", controllerType: typeof(NonApiController));

        var strategy = new MockDiscoveryStrategy(new[] { apiEndpoint, nonApiEndpoint });
        var strategies = new IEndpointDiscoveryStrategy[] { strategy };

        var engine = new ApiDiscoveryEngine(strategies, metadataExtractor, options, diagnosticsLogger);

        // Act
        var result = await engine.DiscoverEndpointsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("/api/test", result.First().RoutePattern);
    }

    [Fact]
    public async Task DiscoverEndpointsAsync_WithObsoleteEndpoint_IncludesByDefault()
    {
        // Arrange
        var diagnosticsLogger = new DiagnosticsLogger();
        var metadataExtractor = CreateMetadataExtractor();
        var options = Options.Create(new SecureSpecOptions());

        var methodWithObsolete = typeof(TestController).GetMethod(nameof(TestController.ObsoleteMethod))!;
        var endpoint = CreateTestEndpoint("GET", "/api/obsolete", methodInfo: methodWithObsolete);
        endpoint.Deprecated = true;

        var strategy = new MockDiscoveryStrategy(new[] { endpoint });
        var strategies = new IEndpointDiscoveryStrategy[] { strategy };

        var engine = new ApiDiscoveryEngine(strategies, metadataExtractor, options, diagnosticsLogger);

        // Act
        var result = await engine.DiscoverEndpointsAsync();

        // Assert
        Assert.Single(result);
        Assert.True(result.First().Deprecated);
    }

    [Fact]
    public async Task DiscoverEndpointsAsync_WithExcludeObsoleteOption_FiltersObsoleteEndpoints()
    {
        // Arrange
        var diagnosticsLogger = new DiagnosticsLogger();
        var metadataExtractor = CreateMetadataExtractor();
        var secureSpecOptions = new SecureSpecOptions
        {
            Discovery =
            {
                IncludeObsolete = false
            }
        };
        var options = Options.Create(secureSpecOptions);

        var normalEndpoint = CreateTestEndpoint("GET", "/api/normal");
        var obsoleteEndpoint = CreateTestEndpoint("GET", "/api/obsolete");
        obsoleteEndpoint.Deprecated = true;

        var strategy = new MockDiscoveryStrategy(new[] { normalEndpoint, obsoleteEndpoint });
        var strategies = new IEndpointDiscoveryStrategy[] { strategy };

        var engine = new ApiDiscoveryEngine(strategies, metadataExtractor, options, diagnosticsLogger);

        // Act
        var result = await engine.DiscoverEndpointsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("/api/normal", result.First().RoutePattern);
    }

    [Fact]
    public async Task DiscoverEndpointsAsync_LogsDiagnosticEvents()
    {
        // Arrange
        var diagnosticsLogger = new DiagnosticsLogger();
        var metadataExtractor = CreateMetadataExtractor();
        var options = Options.Create(new SecureSpecOptions());

        var endpoint = CreateTestEndpoint("GET", "/api/test");
        var strategy = new MockDiscoveryStrategy(new[] { endpoint });
        var strategies = new IEndpointDiscoveryStrategy[] { strategy };

        var engine = new ApiDiscoveryEngine(strategies, metadataExtractor, options, diagnosticsLogger);

        // Act
        await engine.DiscoverEndpointsAsync();

        // Assert
        var events = diagnosticsLogger.GetEvents();
        Assert.Contains(events, e => e.Code == DiagnosticCodes.Discovery.EndpointsDiscovered);
    }

    [Fact]
    public async Task DiscoverEndpointsAsync_ReturnsReadOnlyMetadataCollections()
    {
        // Arrange
        var diagnosticsLogger = new DiagnosticsLogger();
        var metadataExtractor = CreateMetadataExtractor();
        var options = Options.Create(new SecureSpecOptions());

        var endpoint = CreateTestEndpoint("GET", "/api/test", controllerType: typeof(TestController));
        endpoint.Tags.Add("custom");

        var strategy = new MockDiscoveryStrategy(new[] { endpoint });
        var engine = new ApiDiscoveryEngine(new[] { strategy }, metadataExtractor, options, diagnosticsLogger);

        // Act
        var result = await engine.DiscoverEndpointsAsync();
        var metadata = Assert.Single(result);

        // Assert
        Assert.True(((ICollection<string>)metadata.Tags).IsReadOnly);
        Assert.True(((ICollection<OpenApiParameter>)metadata.Parameters).IsReadOnly);
        Assert.True(((ICollection<OpenApiSecurityRequirement>)metadata.Security).IsReadOnly);
        Assert.True(metadata.Responses.IsReadOnly);
    }

    [Fact]
    public async Task DiscoverEndpointsAsync_WithMinimalApiDisabled_FiltersMinimalEndpoints()
    {
        // Arrange
        var diagnosticsLogger = new DiagnosticsLogger();
        var metadataExtractor = CreateMetadataExtractor();
        var secureSpecOptions = new SecureSpecOptions
        {
            Discovery =
            {
                IncludeMinimalApis = false
            }
        };
        var options = Options.Create(secureSpecOptions);

        var controllerEndpoint = CreateTestEndpoint("GET", "/api/controller", controllerType: typeof(TestController));
        var minimalEndpoint = CreateTestEndpoint("GET", "/api/minimal", isMinimalApi: true);
        minimalEndpoint.RouteEndpoint = new Microsoft.AspNetCore.Routing.RouteEndpoint(
            _ => Task.CompletedTask,
            Microsoft.AspNetCore.Routing.Patterns.RoutePatternFactory.Parse("/api/minimal"),
            0,
            null,
            null);

        var strategy = new MockDiscoveryStrategy(new[] { controllerEndpoint, minimalEndpoint });
        var strategies = new IEndpointDiscoveryStrategy[] { strategy };

        var engine = new ApiDiscoveryEngine(strategies, metadataExtractor, options, diagnosticsLogger);

        // Act
        var result = await engine.DiscoverEndpointsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("/api/controller", result.First().RoutePattern);
    }

    [Fact]
    public async Task DiscoverEndpointsAsync_WithControllerRouteEndpoint_NotExcludedWhenMinimalApisDisabled()
    {
        // Arrange
        var diagnosticsLogger = new DiagnosticsLogger();
        var metadataExtractor = CreateMetadataExtractor();
        var secureSpecOptions = new SecureSpecOptions
        {
            Discovery =
            {
                IncludeMinimalApis = false
            }
        };
        var options = Options.Create(secureSpecOptions);

        var controllerEndpoint = CreateTestEndpoint("GET", "/api/controller", controllerType: typeof(TestController));
        controllerEndpoint.RouteEndpoint = new Microsoft.AspNetCore.Routing.RouteEndpoint(
            _ => Task.CompletedTask,
            Microsoft.AspNetCore.Routing.Patterns.RoutePatternFactory.Parse("/api/controller"),
            0,
            null,
            null);

        var strategy = new MockDiscoveryStrategy(new[] { controllerEndpoint });
        var strategies = new IEndpointDiscoveryStrategy[] { strategy };

        var engine = new ApiDiscoveryEngine(strategies, metadataExtractor, options, diagnosticsLogger);

        // Act
        var result = await engine.DiscoverEndpointsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("/api/controller", result.First().RoutePattern);
    }

    // Helper methods and test classes

    private static MetadataExtractor CreateMetadataExtractor()
    {
        var diagnosticsLogger = new DiagnosticsLogger();
        var schemaOptions = new Configuration.SchemaOptions();
        var schemaGenerator = new Schema.SchemaGenerator(schemaOptions, diagnosticsLogger);
        return new MetadataExtractor(schemaGenerator, diagnosticsLogger);
    }

    private static EndpointMetadata CreateTestEndpoint(
        string httpMethod,
        string routePattern,
        MethodInfo? methodInfo = null,
        Type? controllerType = null,
        bool isMinimalApi = false)
    {
        return new EndpointMetadata
        {
            HttpMethod = httpMethod,
            RoutePattern = routePattern,
            OperationName = methodInfo?.Name,
            MethodInfo = methodInfo,
            ControllerType = controllerType,
            IsMinimalApi = isMinimalApi
        };
    }

#pragma warning disable CA1812, CA1852
    private sealed class MockDiscoveryStrategy : IEndpointDiscoveryStrategy
    {
        private readonly IEnumerable<EndpointMetadata> _endpoints;

        public MockDiscoveryStrategy(IEnumerable<EndpointMetadata> endpoints)
        {
            _endpoints = endpoints;
        }

        public Task<IEnumerable<EndpointMetadata>> DiscoverAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_endpoints);
        }
    }

#pragma warning disable CA1812, CA1852
    [ApiController]
    private sealed class TestController
    {
        [ExcludeFromSpec("Test exclusion")]
        public void ExcludedMethod()
        {
            // Intentionally empty: invoked via reflection only.
        }

        [Obsolete("This is obsolete")]
        public void ObsoleteMethod()
        {
            // Intentionally empty: invoked via reflection only.
        }

        public void NormalMethod()
        {
            // Intentionally empty: invoked via reflection only.
        }
    }

#pragma warning disable CA1812, CA1852
    private sealed class NonApiController
    {
        [IncludeInSpec]
        public void IncludedMethod()
        {
            // Intentionally empty: invoked via reflection only.
        }

        public void NotIncludedMethod()
        {
            // Intentionally empty: invoked via reflection only.
        }
    }
}
