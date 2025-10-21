using System.Diagnostics.CodeAnalysis;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.Schema;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Acceptance tests for SchemaId Strategy (Phase 1.2).
/// Verifies all acceptance criteria (AC 401-408) are implemented correctly.
/// </summary>
public class SchemaIdStrategyAcceptanceTests
{
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Types are used via typeof for schema generation scenarios.")]
    private sealed class TypeA { }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Types are used via typeof for schema generation scenarios.")]
    private sealed class TypeB { }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Types are used via typeof for schema generation scenarios.")]
    private sealed class TypeC { }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Types are used via typeof for schema generation scenarios.")]
    private sealed class Generic<T> { }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Types are used via typeof for schema generation scenarios.")]
    private sealed class MultiGeneric<T1, T2, T3> { }

    /// <summary>
    /// AC 401: SchemaId generic naming deterministic
    /// Verifies that generic types use deterministic canonical notation with guillemets.
    /// </summary>
    [Fact]
    public void AC401_SchemaId_Generic_Naming_Is_Deterministic()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator1 = new SchemaGenerator(options, logger);
        var generator2 = new SchemaGenerator(options, logger);

        // Act - Generate same ID from two independent generators
        var id1 = generator1.GenerateSchemaId(typeof(Generic<string>));
        var id2 = generator2.GenerateSchemaId(typeof(Generic<string>));

        // Assert - Both generators produce identical deterministic output
        Assert.Equal("Generic«String»", id1);
        Assert.Equal("Generic«String»", id2);
        Assert.Equal(id1, id2);
    }

    /// <summary>
    /// AC 402: Collision applies _schemaDup{N} suffix
    /// Verifies that when two different types map to the same base ID, the second gets _schemaDup1 suffix.
    /// </summary>
    [Fact]
    public void AC402_Collision_Applies_SchemaDup_Suffix()
    {
        // Arrange - Custom strategy that forces collision by returning same ID for all types
        var options = new SchemaOptions
        {
            IdStrategy = _ => "CollisionTest"
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var id1 = generator.GenerateSchemaId(typeof(TypeA));
        var id2 = generator.GenerateSchemaId(typeof(TypeB));
        var id3 = generator.GenerateSchemaId(typeof(TypeC));

        // Assert - First type gets base ID, subsequent types get _schemaDup{N} suffix
        Assert.Equal("CollisionTest", id1);
        Assert.Equal("CollisionTest_schemaDup1", id2);
        Assert.Equal("CollisionTest_schemaDup2", id3);
    }

    /// <summary>
    /// AC 403: Collision suffix numbering stable
    /// Verifies that suffix numbering is deterministic across rebuilds and generator instances.
    /// </summary>
    [Fact]
    public void AC403_Collision_Suffix_Numbering_Is_Stable()
    {
        // Arrange - Two independent generators with same configuration
        var options1 = new SchemaOptions { IdStrategy = _ => "Same" };
        var logger1 = new DiagnosticsLogger();
        var generator1 = new SchemaGenerator(options1, logger1);

        var options2 = new SchemaOptions { IdStrategy = _ => "Same" };
        var logger2 = new DiagnosticsLogger();
        var generator2 = new SchemaGenerator(options2, logger2);

        // Act - Register types in same order on both generators
        var gen1_type1 = generator1.GenerateSchemaId(typeof(TypeA));
        var gen1_type2 = generator1.GenerateSchemaId(typeof(TypeB));

        var gen2_type1 = generator2.GenerateSchemaId(typeof(TypeA));
        var gen2_type2 = generator2.GenerateSchemaId(typeof(TypeB));

        // Assert - Both generators produce identical suffix sequence
        Assert.Equal(gen1_type1, gen2_type1);
        Assert.Equal(gen1_type2, gen2_type2);
        Assert.Equal("Same_schemaDup1", gen1_type2);
        Assert.Equal("Same_schemaDup1", gen2_type2);
    }

    /// <summary>
    /// AC 404: SchemaId strategy override applied
    /// Verifies that custom IdStrategy function is applied before collision detection.
    /// </summary>
    [Fact]
    public void AC404_SchemaId_Strategy_Override_Is_Applied()
    {
        // Arrange - Custom strategy that prefixes all IDs with "Custom_"
        var options = new SchemaOptions
        {
            IdStrategy = type => $"Custom_{type.Name}"
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var id = generator.GenerateSchemaId(typeof(TypeA));

        // Assert - Custom strategy is applied
        Assert.Equal("Custom_TypeA", id);
    }

    /// <summary>
    /// AC 405: Collision diagnostic SCH001 emitted
    /// Verifies that SCH001 warning is emitted when a collision is detected.
    /// </summary>
    [Fact]
    public void AC405_Collision_Diagnostic_SCH001_Is_Emitted()
    {
        // Arrange
        var options = new SchemaOptions { IdStrategy = _ => "Same" };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act - First registration: no collision
        generator.GenerateSchemaId(typeof(TypeA));
        var eventsAfterFirst = logger.GetEvents();

        // Second registration: collision
        generator.GenerateSchemaId(typeof(TypeB));
        var eventsAfterSecond = logger.GetEvents();

        // Assert
        Assert.Empty(eventsAfterFirst); // No diagnostic for first type
        Assert.Single(eventsAfterSecond); // One diagnostic for collision
        Assert.Equal("SCH001", eventsAfterSecond[0].Code);
        Assert.Equal(DiagnosticLevel.Warn, eventsAfterSecond[0].Level);
        Assert.Contains("collision", eventsAfterSecond[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// AC 406: Generic nested types canonical form
    /// Verifies that nested generic types use guillemet notation in canonical form.
    /// </summary>
    [Fact]
    public void AC406_Generic_Nested_Types_Use_Canonical_Form()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var simple = generator.GenerateSchemaId(typeof(Generic<int>));
        var nested = generator.GenerateSchemaId(typeof(Generic<Generic<string>>));
        var multiArg = generator.GenerateSchemaId(typeof(MultiGeneric<int, string, double>));

        // Assert - Canonical notation with guillemets
        Assert.Equal("Generic«Int32»", simple);
        Assert.Equal("Generic«Generic«String»»", nested);
        Assert.Equal("MultiGeneric«Int32,String,Double»", multiArg);
    }

    /// <summary>
    /// AC 407: Nullable generic arguments canonical ordering
    /// Verifies that nullable value types in generics use canonical Nullable«T» form.
    /// </summary>
    [Fact]
    public void AC407_Nullable_Generic_Arguments_Use_Canonical_Ordering()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var nullableInt = generator.GenerateSchemaId(typeof(Generic<int?>));
        var nestedNullable = generator.GenerateSchemaId(typeof(Generic<Generic<int?>>));
        var multiWithNullable = generator.GenerateSchemaId(typeof(MultiGeneric<string, int?, double>));

        // Assert - Nullable types represented as Nullable«T»
        Assert.Equal("Generic«Nullable«Int32»»", nullableInt);
        Assert.Equal("Generic«Generic«Nullable«Int32»»»", nestedNullable);
        Assert.Equal("MultiGeneric«String,Nullable«Int32»,Double»", multiWithNullable);
    }

    /// <summary>
    /// AC 408: Removing type reclaims suffix sequence
    /// Verifies that RemoveType() reclaims the suffix for reuse.
    /// </summary>
    [Fact]
    public void AC408_Removing_Type_Reclaims_Suffix_Sequence()
    {
        // Arrange
        var options = new SchemaOptions { IdStrategy = _ => "Same" };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act - Register three types to get suffixes
        var id1 = generator.GenerateSchemaId(typeof(TypeA));
        var id2 = generator.GenerateSchemaId(typeof(TypeB));

        // Remove TypeB and register TypeC - should reclaim TypeB's suffix
        generator.RemoveType(typeof(TypeB));
        logger.Clear(); // Clear previous diagnostics
        var id3 = generator.GenerateSchemaId(typeof(TypeC));

        // Assert - TypeC reclaims TypeB's suffix
        Assert.Equal("Same", id1);
        Assert.Equal("Same_schemaDup1", id2);
        Assert.Equal("Same_schemaDup1", id3); // Reclaimed suffix
    }

    /// <summary>
    /// Integration test: All acceptance criteria working together
    /// </summary>
    [Fact]
    public void AllAcceptanceCriteria_WorkTogether()
    {
        // Arrange - Use default strategy for this comprehensive test
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act - Exercise multiple features
        var generic = generator.GenerateSchemaId(typeof(Generic<int?>)); // AC 407
        var nested = generator.GenerateSchemaId(typeof(Generic<Generic<string>>)); // AC 406
        var multiNullable = generator.GenerateSchemaId(typeof(MultiGeneric<string, int?, double>)); // AC 407

        // Test collision and diagnostics with custom strategy
        var collisionOptions = new SchemaOptions { IdStrategy = _ => "Same" };
        var collisionLogger = new DiagnosticsLogger();
        var collisionGenerator = new SchemaGenerator(collisionOptions, collisionLogger);

        var typeA = collisionGenerator.GenerateSchemaId(typeof(TypeA)); // First type
        var typeB = collisionGenerator.GenerateSchemaId(typeof(TypeB)); // AC 402, 405 - collision

        // Assert
        Assert.Equal("Generic«Nullable«Int32»»", generic); // AC 407
        Assert.Equal("Generic«Generic«String»»", nested); // AC 406
        Assert.Equal("MultiGeneric«String,Nullable«Int32»,Double»", multiNullable); // AC 407

        Assert.Equal("Same", typeA); // AC 404
        Assert.Equal("Same_schemaDup1", typeB); // AC 402

        // Verify diagnostic emitted for collision (AC 405)
        var events = collisionLogger.GetEvents();
        Assert.Single(events);
        Assert.Equal("SCH001", events[0].Code);

        // AC 408 - Remove and reclaim
        collisionGenerator.RemoveType(typeof(TypeB));
        collisionLogger.Clear();
        var typeC = collisionGenerator.GenerateSchemaId(typeof(TypeC));
        Assert.Equal("Same_schemaDup1", typeC); // Suffix reclaimed
    }
}
