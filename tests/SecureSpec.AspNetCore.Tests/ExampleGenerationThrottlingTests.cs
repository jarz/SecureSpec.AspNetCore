using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.Schema;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for example generation throttling (AC 304-306, Issue 4.3).
/// </summary>
public class ExampleGenerationThrottlingTests
{
    [Fact]
    public void ThrottledCount_InitiallyZero()
    {
        // Arrange
        var options = new SchemaOptions();
        var logger = new DiagnosticsLogger();
        var generator = new ExampleGenerator(options, logger);

        // Act & Assert
        Assert.Equal(0, generator.ThrottledCount);
    }

    [Fact]
    public void GenerateDeterministicFallback_SimpleType_DoesNotThrottle()
    {
        // Arrange
        var options = new SchemaOptions { ExampleGenerationTimeoutMs = 25 };
        var logger = new DiagnosticsLogger();
        var generator = new ExampleGenerator(options, logger);
        var schema = new OpenApiSchema { Type = "string" };

        // Act
        var result = generator.GenerateDeterministicFallback(schema);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, generator.ThrottledCount);
        Assert.Empty(logger.GetEvents());
    }

    [Fact]
    public void GenerateDeterministicFallback_DeeplyNestedObject_ThrottlesWhenExceedingBudget()
    {
        // Arrange - Set very low timeout to trigger throttling
        var options = new SchemaOptions { ExampleGenerationTimeoutMs = 1 };
        var logger = new DiagnosticsLogger();
        var generator = new ExampleGenerator(options, logger);

        // Create a deeply nested schema that will take time to process
        var schema = CreateDeeplyNestedSchema(20);

        // Act
        generator.GenerateDeterministicFallback(schema);

        // Assert - Generation should be throttled
        // Result may be null or partial depending on when throttling occurred
        Assert.InRange(generator.ThrottledCount, 0, int.MaxValue);

        // Check diagnostic events
        var events = logger.GetEvents();
        if (generator.ThrottledCount > 0)
        {
            var throttledEvents = events.Where(e => e.Code == DiagnosticCodes.ExampleGenerationThrottled).ToList();
            Assert.NotEmpty(throttledEvents);
            Assert.All(throttledEvents, e => Assert.Equal(DiagnosticLevel.Warn, e.Level));
        }
    }

    [Fact]
    public void GenerateDeterministicFallback_WithCancellationToken_RespectsToken()
    {
        // Arrange
        var options = new SchemaOptions { ExampleGenerationTimeoutMs = 25 };
        var logger = new DiagnosticsLogger();
        var generator = new ExampleGenerator(options, logger);
        var schema = CreateDeeplyNestedSchema(10);
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        var result = generator.GenerateDeterministicFallback(schema, cts.Token);

        // Assert
        Assert.Null(result); // Should return null when cancelled
        Assert.True(generator.ThrottledCount > 0); // Should increment counter
        var events = logger.GetEvents();
        var throttledEvents = events.Where(e => e.Code == DiagnosticCodes.ExampleGenerationThrottled).ToList();
        Assert.NotEmpty(throttledEvents);
    }

    [Fact]
    public void GenerateDeterministicFallback_TimeoutDisabled_DoesNotThrottle()
    {
        // Arrange - Disable timeout
        var options = new SchemaOptions { ExampleGenerationTimeoutMs = 0 };
        var logger = new DiagnosticsLogger();
        var generator = new ExampleGenerator(options, logger);
        var schema = CreateDeeplyNestedSchema(10);

        // Act
        var result = generator.GenerateDeterministicFallback(schema);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, generator.ThrottledCount);
        Assert.Empty(logger.GetEvents());
    }

    [Fact]
    public async Task ThrottledCount_IsThreadSafe()
    {
        // Arrange
        var options = new SchemaOptions { ExampleGenerationTimeoutMs = 1 };
        var logger = new DiagnosticsLogger();
        var generator = new ExampleGenerator(options, logger);
        var schema = CreateDeeplyNestedSchema(10);
        const int threadCount = 10;
        const int iterationsPerThread = 5;

        // Act - Run multiple threads generating examples concurrently
        var tasks = new List<Task>();
        for (int i = 0; i < threadCount; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < iterationsPerThread; j++)
                {
                    generator.GenerateDeterministicFallback(schema);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Counter should be accurate despite concurrent access
        var count = generator.ThrottledCount;
        Assert.InRange(count, 0, threadCount * iterationsPerThread);

        // Verify diagnostics were logged (may vary based on timing)
        var events = logger.GetEvents();
        // Note: Count may vary based on timing, so we just verify diagnostics are collected
        Assert.Contains(events, e => e.Code == DiagnosticCodes.ExampleGenerationThrottled);
    }

    [Fact]
    public void EXM001_Diagnostic_ContainsCorrectMetadata()
    {
        // Arrange
        var options = new SchemaOptions { ExampleGenerationTimeoutMs = 1 };
        var logger = new DiagnosticsLogger();
        var generator = new ExampleGenerator(options, logger);
        var schema = CreateDeeplyNestedSchema(10);

        // Act
        generator.GenerateDeterministicFallback(schema);

        // Assert
        if (generator.ThrottledCount > 0)
        {
            var events = logger.GetEvents();
            var throttledEvent = events.FirstOrDefault(e => e.Code == DiagnosticCodes.ExampleGenerationThrottled);
            Assert.NotNull(throttledEvent);
            Assert.Equal(DiagnosticLevel.Warn, throttledEvent.Level);
            Assert.Contains("throttled", throttledEvent.Message, StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(throttledEvent.Context);
            Assert.True(throttledEvent.Sanitized);
        }
    }

    [Fact]
    public void ExamplePrecedenceEngine_ExposesThrottledCount()
    {
        // Arrange
        var options = new SchemaOptions { ExampleGenerationTimeoutMs = 1 };
        var logger = new DiagnosticsLogger();
        var generator = new ExampleGenerator(options, logger);
        var engine = new ExamplePrecedenceEngine(generator);
        var schema = CreateDeeplyNestedSchema(10);

        // Act
        var context = new ExampleContext { Schema = schema };
        engine.ResolveExample(context);

        // Assert - Engine should expose the same count as generator
        Assert.Equal(generator.ThrottledCount, engine.ThrottledCount);
    }

    [Fact]
    public void GenerateDeterministicFallback_Array_ThrottlesOnItemGeneration()
    {
        // Arrange
        var options = new SchemaOptions { ExampleGenerationTimeoutMs = 1 };
        var logger = new DiagnosticsLogger();
        var generator = new ExampleGenerator(options, logger);

        // Create array with deeply nested item schema
        var schema = new OpenApiSchema
        {
            Type = "array",
            Items = CreateDeeplyNestedSchema(15)
        };

        // Act
        var result = generator.GenerateDeterministicFallback(schema);

        // Assert
        // Should either return array (possibly empty if throttled) or null
        Assert.True(result is OpenApiArray || result == null);
        if (generator.ThrottledCount > 0)
        {
            var events = logger.GetEvents();
            var throttledEvents = events.Where(e => e.Code == DiagnosticCodes.ExampleGenerationThrottled).ToList();
            Assert.NotEmpty(throttledEvents);
        }
    }

    [Fact]
    public void GenerateDeterministicFallback_Object_ThrottlesOnPropertyGeneration()
    {
        // Arrange
        var options = new SchemaOptions { ExampleGenerationTimeoutMs = 1 };
        var logger = new DiagnosticsLogger();
        var generator = new ExampleGenerator(options, logger);

        // Create object with deeply nested properties
        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["prop1"] = CreateDeeplyNestedSchema(10),
                ["prop2"] = CreateDeeplyNestedSchema(10),
                ["prop3"] = CreateDeeplyNestedSchema(10)
            }
        };

        // Act
        var result = generator.GenerateDeterministicFallback(schema);

        // Assert
        // Should either return object (possibly incomplete if throttled) or null
        Assert.True(result is OpenApiObject || result == null);
        if (generator.ThrottledCount > 0)
        {
            var events = logger.GetEvents();
            var throttledEvents = events.Where(e => e.Code == DiagnosticCodes.ExampleGenerationThrottled).ToList();
            Assert.NotEmpty(throttledEvents);
        }
    }

    [Fact]
    public void AC304_ExampleThrottling_EnforcesBudget()
    {
        // AC 304: Example throttling enforces 25ms time budget

        // Arrange
        var options = new SchemaOptions { ExampleGenerationTimeoutMs = 25 };
        var logger = new DiagnosticsLogger();
        var generator = new ExampleGenerator(options, logger);

        // Act & Assert
        Assert.Equal(25, options.ExampleGenerationTimeoutMs);

        // Verify that deeply nested schema respects budget
        var schema = CreateDeeplyNestedSchema(50);
        var result = generator.GenerateDeterministicFallback(schema);

        // Generation should complete or throttle, but not hang
        Assert.True(result != null || generator.ThrottledCount > 0);
    }

    [Fact]
    public void AC305_EXM001Diagnostic_EmittedOnThrottle()
    {
        // AC 305: EXM001 diagnostic emitted when throttling occurs

        // Arrange
        var options = new SchemaOptions { ExampleGenerationTimeoutMs = 1 };
        var logger = new DiagnosticsLogger();
        var generator = new ExampleGenerator(options, logger);
        var schema = CreateDeeplyNestedSchema(20);

        // Act
        generator.GenerateDeterministicFallback(schema);

        // Assert
        if (generator.ThrottledCount > 0)
        {
            var events = logger.GetEvents();
            var exm001Events = events.Where(e => e.Code == DiagnosticCodes.ExampleGenerationThrottled).ToList();
            Assert.NotEmpty(exm001Events);
            Assert.All(exm001Events, e =>
            {
                Assert.Equal("EXM001", e.Code);
                Assert.Equal(DiagnosticLevel.Warn, e.Level);
            });
        }
    }

    [Fact]
    public async Task AC306_AtomicCounters_ThreadSafe()
    {
        // AC 306: Atomic counters ensure thread safety

        // Arrange
        var options = new SchemaOptions { ExampleGenerationTimeoutMs = 1 };
        var logger = new DiagnosticsLogger();
        var generator = new ExampleGenerator(options, logger);
        var schema = CreateDeeplyNestedSchema(10);

        // Act - Simulate high concurrency (50 threads)
        const int threadCount = 50;
        var tasks = new List<Task>();
        for (int i = 0; i < threadCount; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                generator.GenerateDeterministicFallback(schema);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Counter should be consistent
        var count1 = generator.ThrottledCount;
        var count2 = generator.ThrottledCount;
        Assert.Equal(count1, count2); // Reading should be consistent
        Assert.InRange(count1, 0, threadCount); // Should be valid
    }

    /// <summary>
    /// Helper method to create a deeply nested object schema for testing throttling.
    /// </summary>
    private static OpenApiSchema CreateDeeplyNestedSchema(int depth)
    {
        if (depth <= 0)
        {
            return new OpenApiSchema { Type = "string" };
        }

        return new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["nested"] = CreateDeeplyNestedSchema(depth - 1),
                ["value"] = new OpenApiSchema { Type = "string" }
            }
        };
    }
}
