using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace SecureSpec.AspNetCore.Schema;

/// <summary>
/// Implements example precedence resolution according to the order:
/// Named > Single/Attribute > Component > Generated > Blocked
/// </summary>
public sealed class ExamplePrecedenceEngine
{
    private readonly ExampleGenerator _generator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExamplePrecedenceEngine"/> class.
    /// </summary>
    public ExamplePrecedenceEngine(ExampleGenerator generator)
    {
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
    }

    /// <summary>
    /// Gets the throttled count from the underlying generator (thread-safe atomic counter).
    /// </summary>
    public int ThrottledCount => _generator.ThrottledCount;

    /// <summary>
    /// Resolves examples based on the precedence order.
    /// </summary>
    /// <param name="context">The example context containing all available sources.</param>
    /// <returns>The resolved example value, or null if blocked or no examples available.</returns>
    public IOpenApiAny? ResolveExample(ExampleContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Check if examples are blocked (lowest priority - return null)
        if (context.IsBlocked)
        {
            return null;
        }

        // Priority 1: Named examples (return first if any exist)
        if (context.NamedExamples.Count > 0)
        {
            var firstExample = context.NamedExamples.Values.FirstOrDefault();
            return firstExample?.Value;
        }

        // Priority 2: Single example (from property or attribute)
        if (context.SingleExample != null)
        {
            return context.SingleExample;
        }

        // Priority 3: Component example (reference)
        if (context.ComponentExample != null)
        {
            // Return a reference object - actual resolution happens during serialization
            // For now, we return null as component references are handled separately
            return null;
        }

        // Priority 4: Generated fallback
        if (context.Schema != null)
        {
            return _generator.GenerateDeterministicFallback(context.Schema);
        }

        // No examples available
        return null;
    }

    /// <summary>
    /// Resolves named examples based on the precedence order.
    /// </summary>
    /// <param name="context">The example context containing all available sources.</param>
    /// <returns>The dictionary of named examples, or empty if none available.</returns>
    public IDictionary<string, OpenApiExample> ResolveNamedExamples(ExampleContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Check if examples are blocked
        if (context.IsBlocked)
        {
            return new Dictionary<string, OpenApiExample>();
        }

        // Priority 1: Named examples
        if (context.NamedExamples.Count > 0)
        {
            return new Dictionary<string, OpenApiExample>(context.NamedExamples);
        }

        // Priority 2: Single example - convert to named example
        if (context.SingleExample != null)
        {
            return new Dictionary<string, OpenApiExample>
            {
                ["default"] = new OpenApiExample { Value = context.SingleExample }
            };
        }

        // Priority 3: Component example
        if (context.ComponentExample != null)
        {
            return new Dictionary<string, OpenApiExample>
            {
                ["default"] = new OpenApiExample { Reference = context.ComponentExample }
            };
        }

        // Priority 4: Generated fallback
        if (context.Schema != null)
        {
            var generatedValue = _generator.GenerateDeterministicFallback(context.Schema);
            if (generatedValue != null)
            {
                return new Dictionary<string, OpenApiExample>
                {
                    ["generated"] = new OpenApiExample { Value = generatedValue }
                };
            }
        }

        return new Dictionary<string, OpenApiExample>();
    }
}
