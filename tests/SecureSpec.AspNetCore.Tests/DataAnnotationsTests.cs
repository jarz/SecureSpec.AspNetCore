using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.Schema;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for DataAnnotations ingestion and conflict detection.
/// </summary>
public class DataAnnotationsTests
{
    #region Test Models

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for testing DataAnnotations")]
    private sealed class ModelWithRange
    {
        [Range(1, 100)]
        public int Age { get; set; }
    }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for testing DataAnnotations")]
    private sealed class ModelWithMinLength
    {
        [MinLength(5)]
        public string? Name { get; set; }
    }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for testing DataAnnotations")]
    private sealed class ModelWithMaxLength
    {
        [MaxLength(50)]
        public string? Description { get; set; }
    }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for testing DataAnnotations")]
    private sealed class ModelWithStringLength
    {
        [StringLength(100, MinimumLength = 10)]
        public string? Content { get; set; }
    }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for testing DataAnnotations")]
    private sealed class ModelWithRegularExpression
    {
        [RegularExpression(@"^[a-zA-Z0-9]+$")]
        public string? Username { get; set; }
    }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for testing DataAnnotations")]
    private sealed class ModelWithRequired
    {
        [Required]
        public string? Email { get; set; }
    }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for testing DataAnnotations")]
    private sealed class ModelWithMultipleAttributes
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        [RegularExpression(@"^[a-zA-Z]+$")]
        public string? Name { get; set; }
    }

    #endregion

    #region AC 31-36: Individual DataAnnotations Mapping

    [Fact]
    public void ApplyDataAnnotations_WithRangeAttribute_SetsMinimumAndMaximum()
    {
        // Arrange (AC 32)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);
        var schema = new OpenApiSchema { Type = "integer" };
        var propertyInfo = typeof(ModelWithRange).GetProperty("Age")!;

        // Act
        generator.ApplyDataAnnotations(schema, propertyInfo);

        // Assert
        Assert.Equal(1, schema.Minimum);
        Assert.Equal(100, schema.Maximum);
    }

    [Fact]
    public void ApplyDataAnnotations_WithMinLengthAttribute_SetsMinLength()
    {
        // Arrange (AC 33)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);
        var schema = new OpenApiSchema { Type = "string" };
        var propertyInfo = typeof(ModelWithMinLength).GetProperty("Name")!;

        // Act
        generator.ApplyDataAnnotations(schema, propertyInfo);

        // Assert
        Assert.Equal(5, schema.MinLength);
    }

    [Fact]
    public void ApplyDataAnnotations_WithMaxLengthAttribute_SetsMaxLength()
    {
        // Arrange (AC 34)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);
        var schema = new OpenApiSchema { Type = "string" };
        var propertyInfo = typeof(ModelWithMaxLength).GetProperty("Description")!;

        // Act
        generator.ApplyDataAnnotations(schema, propertyInfo);

        // Assert
        Assert.Equal(50, schema.MaxLength);
    }

    [Fact]
    public void ApplyDataAnnotations_WithStringLengthAttribute_SetsMinAndMaxLength()
    {
        // Arrange (AC 35)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);
        var schema = new OpenApiSchema { Type = "string" };
        var propertyInfo = typeof(ModelWithStringLength).GetProperty("Content")!;

        // Act
        generator.ApplyDataAnnotations(schema, propertyInfo);

        // Assert
        Assert.Equal(10, schema.MinLength);
        Assert.Equal(100, schema.MaxLength);
    }

    [Fact]
    public void ApplyDataAnnotations_WithRegularExpressionAttribute_SetsPattern()
    {
        // Arrange (AC 36)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);
        var schema = new OpenApiSchema { Type = "string" };
        var propertyInfo = typeof(ModelWithRegularExpression).GetProperty("Username")!;

        // Act
        generator.ApplyDataAnnotations(schema, propertyInfo);

        // Assert
        Assert.Equal(@"^[a-zA-Z0-9]+$", schema.Pattern);
    }

    [Fact]
    public void ApplyDataAnnotations_WithRequiredAttribute_DoesNotModifySchema()
    {
        // Arrange (AC 31)
        // Note: Required attribute is handled at the parent schema level,
        // not on the property schema itself
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);
        var schema = new OpenApiSchema { Type = "string" };
        var propertyInfo = typeof(ModelWithRequired).GetProperty("Email")!;

        // Act
        generator.ApplyDataAnnotations(schema, propertyInfo);

        // Assert
        // Required attribute should not modify the schema directly
        // It should be handled when building the parent's required array
        Assert.Null(schema.MinLength);
        Assert.Null(schema.MaxLength);
        Assert.Null(schema.Pattern);
    }

    #endregion

    #region AC 37-40: Multiple Attributes and Edge Cases

    [Fact]
    public void ApplyDataAnnotations_WithMultipleAttributes_AppliesAll()
    {
        // Arrange (AC 37)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);
        var schema = new OpenApiSchema { Type = "string" };
        var propertyInfo = typeof(ModelWithMultipleAttributes).GetProperty("Name")!;

        // Act
        generator.ApplyDataAnnotations(schema, propertyInfo);

        // Assert
        Assert.Equal(3, schema.MinLength);
        Assert.Equal(50, schema.MaxLength);
        Assert.Equal(@"^[a-zA-Z]+$", schema.Pattern);
    }

    [Fact]
    public void ApplyDataAnnotations_WithNoAttributes_DoesNotModifySchema()
    {
        // Arrange (AC 38)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);
        var schema = new OpenApiSchema { Type = "string" };
        var propertyInfo = typeof(string).GetProperty("Length")!;

        // Act
        generator.ApplyDataAnnotations(schema, propertyInfo);

        // Assert
        Assert.Null(schema.MinLength);
        Assert.Null(schema.MaxLength);
        Assert.Null(schema.Pattern);
        Assert.Null(schema.Minimum);
        Assert.Null(schema.Maximum);
    }

    [Fact]
    public void ApplyDataAnnotations_WithParameterInfo_AppliesAttributes()
    {
        // Arrange (AC 39)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);
        var schema = new OpenApiSchema { Type = "string" };

        // Create a method with a parameter that has attributes
        var method = typeof(DataAnnotationsTests).GetMethod(nameof(TestMethodWithParameter),
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var parameter = method.GetParameters()[0];

        // Act
        generator.ApplyDataAnnotations(schema, parameter);

        // Assert
        Assert.Equal(5, schema.MinLength);
        Assert.Equal(100, schema.MaxLength);
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Helper method for testing")]
    [SuppressMessage("Style", "RCS1163:Unused parameter", Justification = "Parameter needed for attribute testing")]
    private static void TestMethodWithParameter([StringLength(100, MinimumLength = 5)] string value)
    {
        // This method is only used for reflection in tests
    }

    [Fact]
    public void ApplyDataAnnotations_WithStringLengthNoMinimum_OnlySetsMaxLength()
    {
        // Arrange (AC 40)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);
        var schema = new OpenApiSchema { Type = "string" };

        // Use reflection to create a StringLength attribute
        var method = typeof(DataAnnotationsTests).GetMethod(nameof(TestMethodWithMaxLengthOnly),
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var parameter = method.GetParameters()[0];

        // Act
        generator.ApplyDataAnnotations(schema, parameter);

        // Assert
        Assert.Null(schema.MinLength);
        Assert.Equal(50, schema.MaxLength);
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Helper method for testing")]
    [SuppressMessage("Style", "RCS1163:Unused parameter", Justification = "Parameter needed for attribute testing")]
    private static void TestMethodWithMaxLengthOnly([StringLength(50)] string value)
    {
        // This method is only used for reflection in tests
    }

    #endregion

    #region AC 433: Conflict Detection

    [Fact]
    public void ApplyDataAnnotations_WithRangeConflict_LogsANN001AndLastWins()
    {
        // Arrange (AC 433)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);
        var schema = new OpenApiSchema
        {
            Type = "integer",
            Minimum = 0,
            Maximum = 50
        };
        var propertyInfo = typeof(ModelWithRange).GetProperty("Age")!;

        // Act
        generator.ApplyDataAnnotations(schema, propertyInfo);
        var events = logger.GetEvents();

        // Assert
        Assert.Single(events);
        Assert.Equal("ANN001", events[0].Code);
        Assert.Equal(DiagnosticLevel.Warn, events[0].Level);
        Assert.Contains("conflict", events[0].Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Range", events[0].Message, StringComparison.Ordinal);

        // Last wins - Range attribute should override
        Assert.Equal(1, schema.Minimum);
        Assert.Equal(100, schema.Maximum);
    }

    [Fact]
    public void ApplyDataAnnotations_WithMinLengthConflict_LogsANN001AndLastWins()
    {
        // Arrange (AC 433)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);
        var schema = new OpenApiSchema
        {
            Type = "string",
            MinLength = 3
        };
        var propertyInfo = typeof(ModelWithMinLength).GetProperty("Name")!;

        // Act
        generator.ApplyDataAnnotations(schema, propertyInfo);
        var events = logger.GetEvents();

        // Assert
        Assert.Single(events);
        Assert.Equal("ANN001", events[0].Code);
        Assert.Equal(DiagnosticLevel.Warn, events[0].Level);
        Assert.Contains("conflict", events[0].Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MinLength", events[0].Message, StringComparison.Ordinal);

        // Last wins
        Assert.Equal(5, schema.MinLength);
    }

    [Fact]
    public void ApplyDataAnnotations_WithMaxLengthConflict_LogsANN001AndLastWins()
    {
        // Arrange (AC 433)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);
        var schema = new OpenApiSchema
        {
            Type = "string",
            MaxLength = 100
        };
        var propertyInfo = typeof(ModelWithMaxLength).GetProperty("Description")!;

        // Act
        generator.ApplyDataAnnotations(schema, propertyInfo);
        var events = logger.GetEvents();

        // Assert
        Assert.Single(events);
        Assert.Equal("ANN001", events[0].Code);
        Assert.Equal(DiagnosticLevel.Warn, events[0].Level);
        Assert.Contains("conflict", events[0].Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MaxLength", events[0].Message, StringComparison.Ordinal);

        // Last wins
        Assert.Equal(50, schema.MaxLength);
    }

    [Fact]
    public void ApplyDataAnnotations_WithStringLengthConflict_LogsANN001AndLastWins()
    {
        // Arrange (AC 433)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);
        var schema = new OpenApiSchema
        {
            Type = "string",
            MinLength = 1,
            MaxLength = 200
        };
        var propertyInfo = typeof(ModelWithStringLength).GetProperty("Content")!;

        // Act
        generator.ApplyDataAnnotations(schema, propertyInfo);
        var events = logger.GetEvents();

        // Assert
        Assert.Single(events);
        Assert.Equal("ANN001", events[0].Code);
        Assert.Equal(DiagnosticLevel.Warn, events[0].Level);
        Assert.Contains("conflict", events[0].Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("StringLength", events[0].Message, StringComparison.Ordinal);

        // Last wins
        Assert.Equal(10, schema.MinLength);
        Assert.Equal(100, schema.MaxLength);
    }

    [Fact]
    public void ApplyDataAnnotations_WithPatternConflict_LogsANN001AndLastWins()
    {
        // Arrange (AC 433)
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);
        var schema = new OpenApiSchema
        {
            Type = "string",
            Pattern = @"^[0-9]+$"
        };
        var propertyInfo = typeof(ModelWithRegularExpression).GetProperty("Username")!;

        // Act
        generator.ApplyDataAnnotations(schema, propertyInfo);
        var events = logger.GetEvents();

        // Assert
        Assert.Single(events);
        Assert.Equal("ANN001", events[0].Code);
        Assert.Equal(DiagnosticLevel.Warn, events[0].Level);
        Assert.Contains("conflict", events[0].Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("RegularExpression", events[0].Message, StringComparison.Ordinal);

        // Last wins
        Assert.Equal(@"^[a-zA-Z0-9]+$", schema.Pattern);
    }

    [Fact]
    public void ApplyDataAnnotations_WithNoConflict_DoesNotLogANN001()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);
        var schema = new OpenApiSchema { Type = "string" };
        var propertyInfo = typeof(ModelWithMinLength).GetProperty("Name")!;

        // Act
        generator.ApplyDataAnnotations(schema, propertyInfo);
        var events = logger.GetEvents();

        // Assert
        Assert.Empty(events);
    }

    #endregion

    #region Edge Cases and Additional Coverage

    [Fact]
    public void ApplyDataAnnotations_WithDecimalRange_SetsCorrectValues()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);
        var schema = new OpenApiSchema { Type = "number" };

        var method = typeof(DataAnnotationsTests).GetMethod(nameof(TestMethodWithDecimalRange),
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var parameter = method.GetParameters()[0];

        // Act
        generator.ApplyDataAnnotations(schema, parameter);

        // Assert
        Assert.Equal(0.5m, schema.Minimum);
        Assert.Equal(99.9m, schema.Maximum);
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Helper method for testing")]
    [SuppressMessage("Style", "RCS1163:Unused parameter", Justification = "Parameter needed for attribute testing")]
    private static void TestMethodWithDecimalRange([Range(0.5, 99.9)] double value)
    {
        // This method is only used for reflection in tests
    }

    [Fact]
    public void ApplyDataAnnotations_CalledMultipleTimes_LastWins()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);
        var schema = new OpenApiSchema { Type = "string" };

        var property1 = typeof(ModelWithMinLength).GetProperty("Name")!;
        var property2 = typeof(ModelWithMaxLength).GetProperty("Description")!;

        // Act
        generator.ApplyDataAnnotations(schema, property1);
        generator.ApplyDataAnnotations(schema, property2);

        // Assert
        Assert.Equal(5, schema.MinLength); // From first call
        Assert.Equal(50, schema.MaxLength); // From second call
    }

    #endregion
}
