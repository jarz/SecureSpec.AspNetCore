using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.Schema;
using SchemaTestTypes = SecureSpec.AspNetCore.Tests.SchemaGeneratorTestTypes;

namespace SecureSpec.AspNetCore.Tests;

public class SchemaGeneratorSchemaIdTests
{
    [Fact]
    public void GenerateSchemaId_WithSimpleType_ReturnsTypeName()
    {
        var generator = CreateGenerator();

        var id = generator.GenerateSchemaId(typeof(SchemaTestTypes.SimpleClass));

        Assert.Equal("SimpleClass", id);
    }

    [Fact]
    public void GenerateSchemaId_WithGenericType_UsesGuillemets()
    {
        var generator = CreateGenerator();

        var id = generator.GenerateSchemaId(typeof(SchemaTestTypes.GenericClass<string>));

        Assert.Equal("GenericClass«String»", id);
    }

    [Fact]
    public void GenerateSchemaId_WithNestedGeneric_UsesCanonicalForm()
    {
        var generator = CreateGenerator();

        var id = generator.GenerateSchemaId(typeof(SchemaTestTypes.GenericClass<SchemaTestTypes.GenericClass<int>>));

        Assert.Equal("GenericClass«GenericClass«Int32»»", id);
    }

    [Fact]
    public void GenerateSchemaId_WithMultipleGenericArgs_UsesCommaDelimiter()
    {
        var generator = CreateGenerator();

        var id = generator.GenerateSchemaId(typeof(SchemaTestTypes.NestedGeneric<string, int>));

        Assert.Equal("NestedGeneric«String,Int32»", id);
    }

    [Fact]
    public void GenerateSchemaId_WithNullableValueTypeGeneric_UsesCanonicalForm()
    {
        var generator = CreateGenerator();

        var id = generator.GenerateSchemaId(typeof(SchemaTestTypes.GenericClass<int?>));

        Assert.Equal("GenericClass«Nullable«Int32»»", id);
    }

    [Fact]
    public void GenerateSchemaId_WithNestedNullableGeneric_UsesCanonicalForm()
    {
        var generator = CreateGenerator();

        var id = generator.GenerateSchemaId(typeof(SchemaTestTypes.GenericClass<SchemaTestTypes.GenericClass<int?>>));

        Assert.Equal("GenericClass«GenericClass«Nullable«Int32»»»", id);
    }

    [Fact]
    public void GenerateSchemaId_WithMultipleGenericArgsIncludingNullable_MaintainsOrder()
    {
        var generator = CreateGenerator();

        var id = generator.GenerateSchemaId(typeof(SchemaTestTypes.NestedGeneric<string, int?>));

        Assert.Equal("NestedGeneric«String,Nullable«Int32»»", id);
    }

    [Fact]
    public void GenerateSchemaId_WithCollision_AppliesSuffix()
    {
        var options = new SchemaOptions
        {
            IdStrategy = _ => "SameId"
        };
        var generator = CreateGenerator(options);

        var id1 = generator.GenerateSchemaId(typeof(SchemaTestTypes.SimpleClass));
        var id2 = generator.GenerateSchemaId(typeof(SchemaTestTypes.AnotherClass));

        Assert.Equal("SameId", id1);
        Assert.Equal("SameId_schemaDup1", id2);
    }

    [Fact]
    public void GenerateSchemaId_WithMultipleCollisions_IncrementsSuffix()
    {
        var options = new SchemaOptions
        {
            IdStrategy = _ => "SameId"
        };
        var generator = CreateGenerator(options);

        var id1 = generator.GenerateSchemaId(typeof(SchemaTestTypes.SimpleClass));
        var id2 = generator.GenerateSchemaId(typeof(SchemaTestTypes.AnotherClass));
        var id3 = generator.GenerateSchemaId(typeof(SchemaTestTypes.GenericClass<int>));

        Assert.Equal("SameId", id1);
        Assert.Equal("SameId_schemaDup1", id2);
        Assert.Equal("SameId_schemaDup2", id3);
    }

    [Fact]
    public void GenerateSchemaId_WithCollision_EmitsDiagnostic()
    {
        var options = new SchemaOptions
        {
            IdStrategy = _ => "SameId"
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        generator.GenerateSchemaId(typeof(SchemaTestTypes.SimpleClass));
        generator.GenerateSchemaId(typeof(SchemaTestTypes.AnotherClass));
        var events = logger.GetEvents();

        Assert.Single(events);
        Assert.Equal("SCH001", events[0].Code);
        Assert.Equal(DiagnosticLevel.Warn, events[0].Level);
        Assert.Contains("collision", events[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateSchemaId_WithCustomStrategy_AppliesBeforeCollisionDetection()
    {
        var options = new SchemaOptions
        {
            IdStrategy = t => $"Custom_{t.Name}"
        };
        var generator = CreateGenerator(options);

        var id = generator.GenerateSchemaId(typeof(SchemaTestTypes.SimpleClass));

        Assert.Equal("Custom_SimpleClass", id);
    }

    [Fact]
    public void GenerateSchemaId_WithSameType_ReturnsCachedId()
    {
        var generator = CreateGenerator();

        var id1 = generator.GenerateSchemaId(typeof(SchemaTestTypes.SimpleClass));
        var id2 = generator.GenerateSchemaId(typeof(SchemaTestTypes.SimpleClass));

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void GenerateSchemaId_AfterClearCache_RegeneratesIds()
    {
        var generator = CreateGenerator();
        var id1 = generator.GenerateSchemaId(typeof(SchemaTestTypes.SimpleClass));

        generator.ClearCache();
        var id2 = generator.GenerateSchemaId(typeof(SchemaTestTypes.SimpleClass));

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void RemoveType_ReclaimsSuffixSequence()
    {
        var options = new SchemaOptions
        {
            IdStrategy = _ => "SameId"
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        var id1 = generator.GenerateSchemaId(typeof(SchemaTestTypes.SimpleClass));
        var id2 = generator.GenerateSchemaId(typeof(SchemaTestTypes.AnotherClass));

        generator.RemoveType(typeof(SchemaTestTypes.AnotherClass));
        logger.Clear();
        var id3 = generator.GenerateSchemaId(typeof(SchemaTestTypes.GenericClass<int>));

        Assert.Equal("SameId", id1);
        Assert.Equal("SameId_schemaDup1", id2);
        Assert.Equal("SameId_schemaDup1", id3);
    }

    [Fact]
    public void GenerateSchemaId_WithMultipleGenerators_ProducesSameIds()
    {
        var generator1 = CreateGenerator();
        var generator2 = CreateGenerator();

        var id1 = generator1.GenerateSchemaId(typeof(SchemaTestTypes.GenericClass<string>));
        var id2 = generator2.GenerateSchemaId(typeof(SchemaTestTypes.GenericClass<string>));

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void GenerateSchemaId_ThrowsOnNullType()
    {
        var generator = CreateGenerator();

        Assert.Throws<ArgumentNullException>(() => generator.GenerateSchemaId(null!));
    }

    [Fact]
    public void RemoveType_ThrowsOnNullType()
    {
        var generator = CreateGenerator();

        Assert.Throws<ArgumentNullException>(() => generator.RemoveType(null!));
    }

    private static SchemaGenerator CreateGenerator(SchemaOptions? options = null)
    {
        options ??= new SchemaOptions();
        var logger = new DiagnosticsLogger();
        return new SchemaGenerator(options, logger);
    }
}
