using System.Diagnostics;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Diagnostics;

namespace SecureSpec.AspNetCore.Schema;

/// <summary>
/// Generates deterministic fallback examples for OpenAPI schemas.
/// </summary>
public sealed class ExampleGenerator
{
    private readonly SchemaOptions _options;
    private readonly DiagnosticsLogger _diagnosticsLogger;
    private int _throttledCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExampleGenerator"/> class.
    /// </summary>
    public ExampleGenerator(SchemaOptions options, DiagnosticsLogger diagnosticsLogger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _diagnosticsLogger = diagnosticsLogger ?? throw new ArgumentNullException(nameof(diagnosticsLogger));
    }

    /// <summary>
    /// Gets the number of times example generation was throttled (thread-safe atomic counter).
    /// </summary>
    public int ThrottledCount => Interlocked.CompareExchange(ref _throttledCount, 0, 0);

    /// <summary>
    /// Generates a deterministic fallback example for the specified schema.
    /// </summary>
    /// <param name="schema">The schema to generate an example for.</param>
    /// <returns>A generated example value, or null if generation is not possible.</returns>
    public IOpenApiAny? GenerateDeterministicFallback(OpenApiSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        return GenerateDeterministicFallback(schema, null);
    }

    /// <summary>
    /// Generates a deterministic fallback example for the specified schema with throttling support.
    /// </summary>
    /// <param name="schema">The schema to generate an example for.</param>
    /// <param name="cancellationToken">Optional cancellation token for timeout enforcement.</param>
    /// <returns>A generated example value, or null if generation is not possible or throttled.</returns>
    public IOpenApiAny? GenerateDeterministicFallback(OpenApiSchema schema, CancellationToken? cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schema);

        var timeoutMs = _options.ExampleGenerationTimeoutMs;
        CancellationToken effectiveToken;
        Stopwatch? stopwatch = null;

        if (cancellationToken.HasValue)
        {
            effectiveToken = cancellationToken.Value;
            stopwatch = Stopwatch.StartNew();
        }
        else if (timeoutMs > 0)
        {
            stopwatch = Stopwatch.StartNew();
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs))
            {
                effectiveToken = cts.Token;

                try
                {
                    return schema.Type switch
                    {
                        "string" => GenerateStringExample(schema),
                        "integer" => GenerateIntegerExample(schema),
                        "number" => GenerateNumberExample(schema),
                        "boolean" => new OpenApiBoolean(false),
                        "array" => GenerateArrayExample(schema, stopwatch, timeoutMs, effectiveToken),
                        "object" => GenerateObjectExample(schema, stopwatch, timeoutMs, effectiveToken),
                        _ => null
                    };
                }
                catch (OperationCanceledException)
                {
                    OnThrottled(schema, stopwatch.ElapsedMilliseconds);
                    return null;
                }
            }
        else
        {
            effectiveToken = CancellationToken.None;
        }

        // No timeout scenario - no stopwatch needed
        try
        {
            return schema.Type switch
            {
                "string" => GenerateStringExample(schema),
                "integer" => GenerateIntegerExample(schema),
                "number" => GenerateNumberExample(schema),
                "boolean" => new OpenApiBoolean(false),
                "array" => GenerateArrayExample(schema, null, timeoutMs, effectiveToken),
                "object" => GenerateObjectExample(schema, null, timeoutMs, effectiveToken),
                _ => null
            };
        }
        catch (OperationCanceledException)
        {
            OnThrottled(schema, stopwatch?.ElapsedMilliseconds ?? 0);
            return null;
        }
    }

    private OpenApiString GenerateStringExample(OpenApiSchema schema)
    {
        // Handle specific formats
        if (!string.IsNullOrEmpty(schema.Format))
        {
            return schema.Format switch
            {
                "uuid" => new OpenApiString("00000000-0000-0000-0000-000000000000"),
                "date-time" => new OpenApiString("2024-01-01T00:00:00Z"),
                "date" => new OpenApiString("2024-01-01"),
                "time" => new OpenApiString("00:00:00"),
                "byte" => new OpenApiString(""),
                "binary" => new OpenApiString(""),
                "email" => new OpenApiString("user@example.com"),
                "uri" => new OpenApiString("https://example.com"),
                _ => new OpenApiString("string")
            };
        }

        // Handle enum values
        if (schema.Enum?.Count > 0)
        {
            var firstEnum = schema.Enum[0];
            if (firstEnum is OpenApiString enumString)
            {
                return enumString;
            }
        }

        // Default string value
        return new OpenApiString("string");
    }

    private OpenApiInteger GenerateIntegerExample(OpenApiSchema schema)
    {
        // Use minimum if specified
        if (schema.Minimum.HasValue)
        {
            return new OpenApiInteger((int)schema.Minimum.Value);
        }

        // Handle enum values
        if (schema.Enum?.Count > 0)
        {
            var firstEnum = schema.Enum[0];
            if (firstEnum is OpenApiInteger enumInt)
            {
                return enumInt;
            }
        }

        return new OpenApiInteger(0);
    }

    private OpenApiDouble GenerateNumberExample(OpenApiSchema schema)
    {
        // Use minimum if specified
        if (schema.Minimum.HasValue)
        {
            return new OpenApiDouble((double)schema.Minimum.Value);
        }

        return new OpenApiDouble(0.0);
    }

    private OpenApiArray GenerateArrayExample(OpenApiSchema schema, Stopwatch stopwatch, int timeoutMs, CancellationToken cancellationToken)
    {
        var array = new OpenApiArray();

        // Check time budget before generating nested example
        CheckTimeBudget(stopwatch, timeoutMs, cancellationToken);

        // Generate one example item if schema is defined
        if (schema.Items != null)
        {
            var itemExample = GenerateDeterministicFallbackInternal(schema.Items, stopwatch, timeoutMs, cancellationToken);
            if (itemExample != null)
            {
                array.Add(itemExample);
            }
        }

        return array;
    }

    private OpenApiObject GenerateObjectExample(OpenApiSchema schema, Stopwatch stopwatch, int timeoutMs, CancellationToken cancellationToken)
    {
        var obj = new OpenApiObject();

        // Generate examples for required properties first
        if (schema.Properties?.Count > 0)
        {
            foreach (var property in schema.Properties.OrderBy(p => p.Key))
            {
                // Check time budget before generating each property
                CheckTimeBudget(stopwatch, timeoutMs, cancellationToken);

                var propertyExample = GenerateDeterministicFallbackInternal(property.Value, stopwatch, timeoutMs, cancellationToken);
                if (propertyExample != null)
                {
                    obj[property.Key] = propertyExample;
                }
            }
        }

        return obj;
    }

    /// <summary>
    /// Internal method for recursive generation with time budget tracking.
    /// </summary>
    private IOpenApiAny? GenerateDeterministicFallbackInternal(OpenApiSchema schema, Stopwatch stopwatch, int timeoutMs, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schema);

        // Check time budget before processing
        CheckTimeBudget(stopwatch, timeoutMs, cancellationToken);

        return schema.Type switch
        {
            "string" => GenerateStringExample(schema),
            "integer" => GenerateIntegerExample(schema),
            "number" => GenerateNumberExample(schema),
            "boolean" => new OpenApiBoolean(false),
            "array" => GenerateArrayExample(schema, stopwatch, timeoutMs, cancellationToken),
            "object" => GenerateObjectExample(schema, stopwatch, timeoutMs, cancellationToken),
            _ => null
        };
    }

    /// <summary>
    /// Checks if the time budget has been exceeded and throws OperationCanceledException if so.
    /// </summary>
    private static void CheckTimeBudget(Stopwatch stopwatch, int timeoutMs, CancellationToken cancellationToken)
    {
        // Check cancellation token (fast path)
        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>
    /// Called when example generation is throttled due to time budget exceeded.
    /// </summary>
    private void OnThrottled(OpenApiSchema schema, long elapsedMs)
    {
        // Increment atomic counter
        Interlocked.Increment(ref _throttledCount);

        // Emit EXM001 diagnostic
        _diagnosticsLogger.LogWarning(
            DiagnosticCodes.ExampleGenerationThrottled,
            $"Example generation throttled after {elapsedMs}ms (budget: {_options.ExampleGenerationTimeoutMs}ms)",
            new
            {
                SchemaType = schema.Type,
                ElapsedMs = elapsedMs,
                BudgetMs = _options.ExampleGenerationTimeoutMs
            },
            sanitized: true
        );
    }
}
