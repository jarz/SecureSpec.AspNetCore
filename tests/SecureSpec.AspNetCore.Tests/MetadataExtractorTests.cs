using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Core;
using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.Schema;
using System.ComponentModel;

namespace SecureSpec.AspNetCore.Tests;

public class MetadataExtractorTests
{
    [Fact]
    public void EnrichMetadata_WithNullMetadata_ThrowsArgumentNullException()
    {
        // Arrange
        var extractor = CreateMetadataExtractor();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => extractor.EnrichMetadata(null!));
    }

    [Fact]
    public void EnrichMetadata_WithNoMethodInfo_DoesNotThrow()
    {
        // Arrange
        var extractor = CreateMetadataExtractor();
        var metadata = new EndpointMetadata
        {
            HttpMethod = "GET",
            RoutePattern = "/api/test"
        };

        // Act
        extractor.EnrichMetadata(metadata);

        // Assert - no exception thrown
        Assert.Null(metadata.MethodInfo);
    }

    [Fact]
    public void EnrichMetadata_WithDescriptionAttribute_SetsDescription()
    {
        // Arrange
        var extractor = CreateMetadataExtractor();
        var method = typeof(TestController).GetMethod(nameof(TestController.MethodWithDescription))!;
        var metadata = new EndpointMetadata
        {
            HttpMethod = "GET",
            RoutePattern = "/api/test",
            MethodInfo = method
        };

        // Act
        extractor.EnrichMetadata(metadata);

        // Assert
        Assert.Equal("This is a test description", metadata.Description);
    }

    [Fact]
    public void EnrichMetadata_WithControllerType_ExtractsTag()
    {
        // Arrange
        var extractor = CreateMetadataExtractor();
        var method = typeof(TestController).GetMethod(nameof(TestController.NormalMethod))!;
        var metadata = new EndpointMetadata
        {
            HttpMethod = "GET",
            RoutePattern = "/api/test",
            MethodInfo = method,
            ControllerType = typeof(TestController)
        };

        // Act
        extractor.EnrichMetadata(metadata);

        // Assert
        Assert.Contains("Test", metadata.Tags);
    }

    [Fact]
    public void EnrichMetadata_WithFromBodyParameter_CreatesRequestBody()
    {
        // Arrange
        var extractor = CreateMetadataExtractor();
        var method = typeof(TestController).GetMethod(nameof(TestController.MethodWithBody))!;
        var metadata = new EndpointMetadata
        {
            HttpMethod = "POST",
            RoutePattern = "/api/test",
            MethodInfo = method
        };

        // Act
        extractor.EnrichMetadata(metadata);

        // Assert
        Assert.NotNull(metadata.RequestBody);
        // Required may not be set depending on parameter attributes
        Assert.NotEmpty(metadata.RequestBody.Content);
    }

    [Fact]
    public void EnrichMetadata_WithQueryParameters_CreatesParameters()
    {
        // Arrange
        var extractor = CreateMetadataExtractor();
        var method = typeof(TestController).GetMethod(nameof(TestController.MethodWithQueryParams))!;
        var metadata = new EndpointMetadata
        {
            HttpMethod = "GET",
            RoutePattern = "/api/test",
            MethodInfo = method
        };

        // Act
        extractor.EnrichMetadata(metadata);

        // Assert
        Assert.NotEmpty(metadata.Parameters);
        var param = metadata.Parameters.FirstOrDefault(p => p.Name == "id");
        Assert.NotNull(param);
        Assert.Equal(ParameterLocation.Query, param.In);
    }

    [Fact]
    public void EnrichMetadata_WithPathParameter_MarksAsRequired()
    {
        // Arrange
        var extractor = CreateMetadataExtractor();
        var method = typeof(TestController).GetMethod(nameof(TestController.MethodWithPathParam))!;
        var metadata = new EndpointMetadata
        {
            HttpMethod = "GET",
            RoutePattern = "/api/test/{id}",
            MethodInfo = method
        };

        // Act
        extractor.EnrichMetadata(metadata);

        // Assert
        Assert.NotEmpty(metadata.Parameters);
        var param = metadata.Parameters.FirstOrDefault(p => p.Name == "id");
        Assert.NotNull(param);
        Assert.Equal(ParameterLocation.Path, param.In);
        Assert.True(param.Required);
    }

    [Fact]
    public void EnrichMetadata_WithProducesResponseType_CreatesResponses()
    {
        // Arrange
        var extractor = CreateMetadataExtractor();
        var method = typeof(TestController).GetMethod(nameof(TestController.MethodWithResponseTypes))!;
        var metadata = new EndpointMetadata
        {
            HttpMethod = "GET",
            RoutePattern = "/api/test",
            MethodInfo = method
        };

        // Act
        extractor.EnrichMetadata(metadata);

        // Assert
        Assert.Contains("200", metadata.Responses.Keys);
        Assert.Contains("404", metadata.Responses.Keys);
        Assert.Equal("Success", metadata.Responses["200"].Description);
    }

    [Fact]
    public void EnrichMetadata_WithReturnType_CreatesDefaultResponse()
    {
        // Arrange
        var extractor = CreateMetadataExtractor();
        var method = typeof(TestController).GetMethod(nameof(TestController.MethodWithReturnType))!;
        var metadata = new EndpointMetadata
        {
            HttpMethod = "GET",
            RoutePattern = "/api/test",
            MethodInfo = method
        };

        // Act
        extractor.EnrichMetadata(metadata);

        // Assert
        Assert.Contains("200", metadata.Responses.Keys);
        var response = metadata.Responses["200"];
        Assert.NotNull(response.Content);
        Assert.Contains("application/json", response.Content.Keys);
    }

    [Fact]
    public void EnrichMetadata_WithObsoleteAttribute_SetsDeprecatedFlag()
    {
        // Arrange
        var extractor = CreateMetadataExtractor();
        var method = typeof(TestController).GetMethod(nameof(TestController.ObsoleteMethod))!;
        var metadata = new EndpointMetadata
        {
            HttpMethod = "GET",
            RoutePattern = "/api/test",
            MethodInfo = method
        };

        // Act
        extractor.EnrichMetadata(metadata);

        // Assert
        Assert.True(metadata.Deprecated);
    }

    [Fact]
    public void EnrichMetadata_WithTaskReturnType_UnwrapsTaskType()
    {
        // Arrange
        var extractor = CreateMetadataExtractor();
        var method = typeof(TestController).GetMethod(nameof(TestController.AsyncMethodWithReturnType))!;
        var metadata = new EndpointMetadata
        {
            HttpMethod = "GET",
            RoutePattern = "/api/test",
            MethodInfo = method
        };

        // Act
        extractor.EnrichMetadata(metadata);

        // Assert
        Assert.Contains("200", metadata.Responses.Keys);
        Assert.NotNull(metadata.Responses["200"].Content);
    }

    [Fact]
    public void EnrichMetadata_WithOptionalParameter_MarksAsNotRequired()
    {
        // Arrange
        var extractor = CreateMetadataExtractor();
        var method = typeof(TestController).GetMethod(nameof(TestController.MethodWithOptionalParam))!;
        var metadata = new EndpointMetadata
        {
            HttpMethod = "GET",
            RoutePattern = "/api/test",
            MethodInfo = method
        };

        // Act
        extractor.EnrichMetadata(metadata);

        // Assert
        var param = metadata.Parameters.FirstOrDefault(p => p.Name == "page");
        Assert.NotNull(param);
        Assert.False(param.Required);
    }

    [Fact]
    public void EnrichMetadata_SkipsSpecialParameters()
    {
        // Arrange
        var extractor = CreateMetadataExtractor();
        var method = typeof(TestController).GetMethod(nameof(TestController.MethodWithSpecialParams))!;
        var metadata = new EndpointMetadata
        {
            HttpMethod = "GET",
            RoutePattern = "/api/test",
            MethodInfo = method
        };

        // Act
        extractor.EnrichMetadata(metadata);

        // Assert
        Assert.Empty(metadata.Parameters);
    }

    [Fact]
    public void EnrichMetadata_SetsOperationIdFromMethodName()
    {
        // Arrange
        var extractor = CreateMetadataExtractor();
        var method = typeof(TestController).GetMethod(nameof(TestController.NormalMethod))!;
        var metadata = new EndpointMetadata
        {
            HttpMethod = "GET",
            RoutePattern = "/api/test",
            MethodInfo = method
        };

        // Act
        extractor.EnrichMetadata(metadata);

        // Assert
        Assert.Equal("NormalMethod", metadata.OperationId);
    }

    // Helper methods and test classes

    private static MetadataExtractor CreateMetadataExtractor()
    {
        var diagnosticsLogger = new DiagnosticsLogger();
        var schemaOptions = new Configuration.SchemaOptions();
        var schemaGenerator = new SchemaGenerator(schemaOptions, diagnosticsLogger);
        return new MetadataExtractor(schemaGenerator, diagnosticsLogger);
    }

#pragma warning disable CA1812, CA1852
    private sealed class TestController
    {
        [Description("This is a test description")]
        public void MethodWithDescription() { }

        public void NormalMethod() { }

        public void MethodWithBody([FromBody] TestModel model) { _ = model; }

        public void MethodWithQueryParams([FromQuery] int id, [FromQuery] string? name) { _ = id; _ = name; }

        public void MethodWithPathParam([FromRoute] int id) { _ = id; }

        [ProducesResponseType(typeof(TestModel), 200)]
        [ProducesResponseType(404)]
        public void MethodWithResponseTypes() { }

        public TestModel MethodWithReturnType() => new TestModel();

        public Task<TestModel> AsyncMethodWithReturnType() => Task.FromResult(new TestModel());

        [Obsolete]
        public void ObsoleteMethod() { }

        public void MethodWithOptionalParam(int page = 1) { _ = page; }

        public void MethodWithSpecialParams(CancellationToken cancellationToken) { _ = cancellationToken; }
    }

#pragma warning disable CA1852
    private sealed class TestModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}
