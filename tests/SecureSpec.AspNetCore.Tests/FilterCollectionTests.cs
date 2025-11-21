using SecureSpec.AspNetCore.Filters;

namespace SecureSpec.AspNetCore.Tests;

public class FilterCollectionTests
{
    [Fact]
    public void AddSchemaFilter_AddsFilterType()
    {
        // Arrange
        var collection = new FilterCollection();

        // Act
        collection.AddSchemaFilter<DummySchemaFilter>();

        // Assert
        Assert.Single(collection.SchemaFilters);
        Assert.Equal(typeof(DummySchemaFilter), collection.SchemaFilters[0]);
    }

    [Fact]
    public void AddSchemaFilter_ReturnsCollection_ForChaining()
    {
        // Arrange
        var collection = new FilterCollection();

        // Act
        var result = collection.AddSchemaFilter<DummySchemaFilter>();

        // Assert
        Assert.Same(collection, result);
    }

    [Fact]
    public void AddSchemaFilter_ByType_WithInvalidType_ThrowsArgumentException()
    {
        // Arrange
        var collection = new FilterCollection();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => collection.AddSchemaFilter(typeof(string)));
    }

    [Fact]
    public void AddSchemaFilter_ByType_WithNullType_ThrowsArgumentNullException()
    {
        // Arrange
        var collection = new FilterCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => collection.AddSchemaFilter(null!));
    }

    [Fact]
    public void AddOperationFilter_AddsFilterType()
    {
        // Arrange
        var collection = new FilterCollection();

        // Act
        collection.AddOperationFilter<DummyOperationFilter>();

        // Assert
        Assert.Single(collection.OperationFilters);
        Assert.Equal(typeof(DummyOperationFilter), collection.OperationFilters[0]);
    }

    [Fact]
    public void AddOperationFilter_ByType_WithInvalidType_ThrowsArgumentException()
    {
        // Arrange
        var collection = new FilterCollection();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => collection.AddOperationFilter(typeof(string)));
    }

    [Fact]
    public void AddParameterFilter_AddsFilterType()
    {
        // Arrange
        var collection = new FilterCollection();

        // Act
        collection.AddParameterFilter<DummyParameterFilter>();

        // Assert
        Assert.Single(collection.ParameterFilters);
        Assert.Equal(typeof(DummyParameterFilter), collection.ParameterFilters[0]);
    }

    [Fact]
    public void AddRequestBodyFilter_AddsFilterType()
    {
        // Arrange
        var collection = new FilterCollection();

        // Act
        collection.AddRequestBodyFilter<DummyRequestBodyFilter>();

        // Assert
        Assert.Single(collection.RequestBodyFilters);
        Assert.Equal(typeof(DummyRequestBodyFilter), collection.RequestBodyFilters[0]);
    }

    [Fact]
    public void AddDocumentFilter_AddsFilterType()
    {
        // Arrange
        var collection = new FilterCollection();

        // Act
        collection.AddDocumentFilter<DummyDocumentFilter>();

        // Assert
        Assert.Single(collection.DocumentFilters);
        Assert.Equal(typeof(DummyDocumentFilter), collection.DocumentFilters[0]);
    }

    [Fact]
    public void AddPreSerializeFilter_AddsFilterType()
    {
        // Arrange
        var collection = new FilterCollection();

        // Act
        collection.AddPreSerializeFilter<DummyPreSerializeFilter>();

        // Assert
        Assert.Single(collection.PreSerializeFilters);
        Assert.Equal(typeof(DummyPreSerializeFilter), collection.PreSerializeFilters[0]);
    }

    [Fact]
    public void AddMultipleFilters_MaintainsOrder()
    {
        // Arrange
        var collection = new FilterCollection();

        // Act
        collection.AddSchemaFilter<DummySchemaFilter>();
        collection.AddSchemaFilter<AnotherSchemaFilter>();

        // Assert
        Assert.Equal(2, collection.SchemaFilters.Count);
        Assert.Equal(typeof(DummySchemaFilter), collection.SchemaFilters[0]);
        Assert.Equal(typeof(AnotherSchemaFilter), collection.SchemaFilters[1]);
    }

    [Fact]
    public void FilterCollections_AreReadOnly()
    {
        // Arrange
        var collection = new FilterCollection();
        collection.AddSchemaFilter<DummySchemaFilter>();

        // Act & Assert
        Assert.IsAssignableFrom<IReadOnlyList<Type>>(collection.SchemaFilters);
    }

    // Dummy filter implementations for testing

#pragma warning disable CA1812, CA1852
    private sealed class DummySchemaFilter : ISchemaFilter
    {
        public void Apply(Microsoft.OpenApi.Models.OpenApiSchema schema, SchemaFilterContext context) { }
    }

#pragma warning disable CA1812, CA1852
    private sealed class AnotherSchemaFilter : ISchemaFilter
    {
        public void Apply(Microsoft.OpenApi.Models.OpenApiSchema schema, SchemaFilterContext context) { }
    }

#pragma warning disable CA1812, CA1852
    private sealed class DummyOperationFilter : IOperationFilter
    {
        public void Apply(Microsoft.OpenApi.Models.OpenApiOperation operation, OperationFilterContext context) { }
    }

#pragma warning disable CA1812, CA1852
    private sealed class DummyParameterFilter : IParameterFilter
    {
        public void Apply(Microsoft.OpenApi.Models.OpenApiParameter parameter, ParameterFilterContext context) { }
    }

#pragma warning disable CA1812, CA1852
    private sealed class DummyRequestBodyFilter : IRequestBodyFilter
    {
        public void Apply(Microsoft.OpenApi.Models.OpenApiRequestBody requestBody, RequestBodyFilterContext context) { }
    }

#pragma warning disable CA1812, CA1852
    private sealed class DummyDocumentFilter : IDocumentFilter
    {
        public void Apply(Microsoft.OpenApi.Models.OpenApiDocument document, DocumentFilterContext context) { }
    }

#pragma warning disable CA1812, CA1852
    private sealed class DummyPreSerializeFilter : IPreSerializeFilter
    {
        public void Apply(Microsoft.OpenApi.Models.OpenApiDocument document, PreSerializeFilterContext context) { }
    }
}
