using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.Schema;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for schema ID generation with collision handling.
/// </summary>
public class SchemaGeneratorTests
{
    private class SimpleClass { }
    private class AnotherClass { }
    private class GenericClass<T> { }
    private class NestedGeneric<TOuter, TInner> { }

    [Fact]
    public void GenerateSchemaId_WithSimpleType_ReturnsTypeName()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var id = generator.GenerateSchemaId(typeof(SimpleClass));

        // Assert
        Assert.Equal("SimpleClass", id);
    }

    [Fact]
    public void GenerateSchemaId_WithGenericType_UsesGuillemets()
    {
        // Arrange (AC 406)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var id = generator.GenerateSchemaId(typeof(GenericClass<string>));

        // Assert
        Assert.Equal("GenericClass«String»", id);
    }

    [Fact]
    public void GenerateSchemaId_WithNestedGeneric_UsesCanonicalForm()
    {
        // Arrange (AC 406)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var id = generator.GenerateSchemaId(typeof(GenericClass<GenericClass<int>>));

        // Assert
        Assert.Equal("GenericClass«GenericClass«Int32»»", id);
    }

    [Fact]
    public void GenerateSchemaId_WithMultipleGenericArgs_UsesCommaDelimiter()
    {
        // Arrange (AC 407)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var id = generator.GenerateSchemaId(typeof(NestedGeneric<string, int>));

        // Assert
        Assert.Equal("NestedGeneric«String,Int32»", id);
    }

    [Fact]
    public void GenerateSchemaId_WithCollision_AppliesSuffix()
    {
        // Arrange (AC 402)
        var options = new SchemaOptions
        {
            // Custom strategy that always returns same ID to force collision
            IdStrategy = _ => "SameId"
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var id1 = generator.GenerateSchemaId(typeof(SimpleClass));
        var id2 = generator.GenerateSchemaId(typeof(AnotherClass));

        // Assert
        Assert.Equal("SameId", id1);
        Assert.Equal("SameId_schemaDup1", id2);
    }

    [Fact]
    public void GenerateSchemaId_WithMultipleCollisions_Incrementssuffix()
    {
        // Arrange (AC 402, 403)
        var options = new SchemaOptions
        {
            IdStrategy = _ => "SameId"
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var id1 = generator.GenerateSchemaId(typeof(SimpleClass));
        var id2 = generator.GenerateSchemaId(typeof(AnotherClass));
        var id3 = generator.GenerateSchemaId(typeof(GenericClass<int>));

        // Assert
        Assert.Equal("SameId", id1);
        Assert.Equal("SameId_schemaDup1", id2);
        Assert.Equal("SameId_schemaDup2", id3);
    }

    [Fact]
    public void GenerateSchemaId_WithCollision_EmitsDiagnostic()
    {
        // Arrange (AC 405)
        var options = new SchemaOptions
        {
            IdStrategy = _ => "SameId"
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        generator.GenerateSchemaId(typeof(SimpleClass));
        generator.GenerateSchemaId(typeof(AnotherClass));
        var events = logger.GetEvents();

        // Assert
        Assert.Single(events);
        Assert.Equal("SCH001", events[0].Code);
        Assert.Equal(DiagnosticLevel.Warn, events[0].Level);
        Assert.Contains("collision", events[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateSchemaId_WithCustomStrategy_AppliesBeforeCollisionDetection()
    {
        // Arrange (AC 404)
        var options = new SchemaOptions
        {
            IdStrategy = t => $"Custom_{t.Name}"
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var id = generator.GenerateSchemaId(typeof(SimpleClass));

        // Assert
        Assert.Equal("Custom_SimpleClass", id);
    }

    [Fact]
    public void GenerateSchemaId_WithSameType_ReturnsCachedId()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var id1 = generator.GenerateSchemaId(typeof(SimpleClass));
        var id2 = generator.GenerateSchemaId(typeof(SimpleClass));

        // Assert
        Assert.Equal(id1, id2);
    }

    [Fact]
    public void GenerateSchemaId_AfterClearCache_RegeneratesIds()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);
        var id1 = generator.GenerateSchemaId(typeof(SimpleClass));

        // Act
        generator.ClearCache();
        var id2 = generator.GenerateSchemaId(typeof(SimpleClass));

        // Assert
        Assert.Equal(id1, id2); // Same ID but regenerated
    }

    [Fact]
    public void RemoveType_ReclaimsSuffixSequence()
    {
        // Arrange (AC 408)
        var options = new SchemaOptions
        {
            IdStrategy = _ => "SameId"
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        var id1 = generator.GenerateSchemaId(typeof(SimpleClass));
        var id2 = generator.GenerateSchemaId(typeof(AnotherClass));

        // Act
        generator.RemoveType(typeof(AnotherClass));
        logger.Clear(); // Clear previous diagnostics
        var id3 = generator.GenerateSchemaId(typeof(GenericClass<int>));

        // Assert
        Assert.Equal("SameId", id1);
        Assert.Equal("SameId_schemaDup1", id2);
        Assert.Equal("SameId_schemaDup1", id3); // Reclaimed suffix
    }

    [Fact]
    public void GenerateSchemaId_WithMultipleGenerators_ProducesSameIds()
    {
        // Arrange (AC 403) - stable across rebuilds
        var options1 = new SchemaOptions();
        var logger1 = new DiagnosticsLogger();
        var generator1 = new SchemaGenerator(options1, logger1);

        var options2 = new SchemaOptions();
        var logger2 = new DiagnosticsLogger();
        var generator2 = new SchemaGenerator(options2, logger2);

        // Act
        var id1 = generator1.GenerateSchemaId(typeof(GenericClass<string>));
        var id2 = generator2.GenerateSchemaId(typeof(GenericClass<string>));

        // Assert
        Assert.Equal(id1, id2);
    }

    [Fact]
    public void GenerateSchemaId_ThrowsOnNullType()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => generator.GenerateSchemaId(null!));
    }

    [Fact]
    public void RemoveType_ThrowsOnNullType()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => generator.RemoveType(null!));
    }
}
