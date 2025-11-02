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
    // OpenAPI type constants as defined in the OpenAPI specification
    private const string TypeString = "string";
    private const string TypeInteger = "integer";
    private const string TypeNumber = "number";
    private const string TypeBoolean = "boolean";
    private const string TypeArray = "array";
    private const string TypeObject = "object";

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

        // Scenario 1: External cancellation token provided
        if (cancellationToken.HasValue)
        {
            return GenerateWithExternalToken(schema, timeoutMs, cancellationToken.Value);
        }

        // Scenario 2: Internal timeout enabled
        if (timeoutMs > 0)
        {
            return GenerateWithTimeout(schema, timeoutMs);
        }

        // Scenario 3: No timeout or cancellation
        return GenerateByType(schema, null, 0, CancellationToken.None);
    }

    /// <summary>
    /// Generates example with external cancellation token, optionally combined with timeout.
    /// </summary>
    private IOpenApiAny? GenerateWithExternalToken(OpenApiSchema schema, int timeoutMs, CancellationToken externalToken)
    {
        var stopwatch = Stopwatch.StartNew();

        if (timeoutMs > 0)
        {
            // Combine external token with timeout
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            linkedCts.CancelAfter(timeoutMs);

            try
            {
                return GenerateByType(schema, stopwatch, timeoutMs, linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                OnThrottled(schema, stopwatch.ElapsedMilliseconds);
                return null;
            }
        }

        // External token without timeout
        try
        {
            return GenerateByType(schema, stopwatch, timeoutMs, externalToken);
        }
        catch (OperationCanceledException)
        {
            OnThrottled(schema, stopwatch.ElapsedMilliseconds);
            return null;
        }
    }

    /// <summary>
    /// Generates example with internal timeout.
    /// </summary>
    private IOpenApiAny? GenerateWithTimeout(OpenApiSchema schema, int timeoutMs)
    {
        var stopwatch = Stopwatch.StartNew();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));

        try
        {
            return GenerateByType(schema, stopwatch, timeoutMs, cts.Token);
        }
        catch (OperationCanceledException)
        {
            OnThrottled(schema, stopwatch.ElapsedMilliseconds);
            return null;
        }
    }

    /// <summary>
    /// Generates an example value based on the schema type.
    /// Uses strings for type matching because OpenAPI schema types are defined as strings in the OpenAPI specification.
    /// </summary>
    private IOpenApiAny? GenerateByType(OpenApiSchema schema, Stopwatch? stopwatch, int timeoutMs, CancellationToken cancellationToken)
    {
        // OpenAPI defines type as a string property, not an enum
        // Possible values: "string", "number", "integer", "boolean", "array", "object", "null"
        return schema.Type switch
        {
            TypeString => GenerateStringExample(schema),
            TypeInteger => GenerateIntegerExample(schema),
            TypeNumber => GenerateNumberExample(schema),
            TypeBoolean => new OpenApiBoolean(false),
            TypeArray => GenerateArrayExample(schema, stopwatch, timeoutMs, cancellationToken),
            TypeObject => GenerateObjectExample(schema, stopwatch, timeoutMs, cancellationToken),
            _ => null
        };
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

    private OpenApiArray GenerateArrayExample(OpenApiSchema schema, Stopwatch? stopwatch, int timeoutMs, CancellationToken cancellationToken)
    {
        var array = new OpenApiArray();

        // Check time budget before generating nested example
        if (stopwatch != null)
        {
            CheckTimeBudget(stopwatch, timeoutMs, cancellationToken);
        }

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

    private OpenApiObject GenerateObjectExample(OpenApiSchema schema, Stopwatch? stopwatch, int timeoutMs, CancellationToken cancellationToken)
    {
        var obj = new OpenApiObject();

        // Generate examples for required properties first
        if (schema.Properties?.Count > 0)
        {
            foreach (var property in schema.Properties.OrderBy(p => p.Key))
            {
                // Check time budget before generating each property
                if (stopwatch != null)
                {
                    CheckTimeBudget(stopwatch, timeoutMs, cancellationToken);
                }

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
    private IOpenApiAny? GenerateDeterministicFallbackInternal(OpenApiSchema schema, Stopwatch? stopwatch, int timeoutMs, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schema);

        // Check time budget before processing
        if (stopwatch != null)
        {
            CheckTimeBudget(stopwatch, timeoutMs, cancellationToken);
        }

        return schema.Type switch
        {
            TypeString => GenerateStringExample(schema),
            TypeInteger => GenerateIntegerExample(schema),
            TypeNumber => GenerateNumberExample(schema),
            TypeBoolean => new OpenApiBoolean(false),
            TypeArray => GenerateArrayExample(schema, stopwatch, timeoutMs, cancellationToken),
            TypeObject => GenerateObjectExample(schema, stopwatch, timeoutMs, cancellationToken),
            _ => null
        };
    }

    /// <summary>
    /// Checks if the time budget has been exceeded and throws OperationCanceledException if so.
    /// </summary>
    private static void CheckTimeBudget(Stopwatch stopwatch, int timeoutMs, CancellationToken cancellationToken)
    {
        // Check cancellation token first (fast path)
        cancellationToken.ThrowIfCancellationRequested();

        // Manual time check for external cancellation tokens that don't have built-in timeout
        if (timeoutMs > 0 && stopwatch.ElapsedMilliseconds >= timeoutMs)
        {
            throw new OperationCanceledException();
        }
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
