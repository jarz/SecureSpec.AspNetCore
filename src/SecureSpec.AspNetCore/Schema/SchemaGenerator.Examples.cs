using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace SecureSpec.AspNetCore.Schema;

/// <summary>
/// Partial class for SchemaGenerator containing example-related functionality.
/// </summary>
public partial class SchemaGenerator
{
    /// <summary>
    /// Applies examples to a schema based on the precedence engine.
    /// </summary>
    /// <param name="schema">The schema to apply examples to.</param>
    /// <param name="context">The example context containing available example sources.</param>
    public void ApplyExamples(OpenApiSchema schema, ExampleContext context)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);

        // Check if example generation is disabled
        if (!_options.GenerateExamples)
        {
            return;
        }

        // Ensure schema is set in context for fallback generation
        if (context.Schema == null)
        {
            context.Schema = schema;
        }

        // Resolve example using precedence engine
        var example = _precedenceEngine.ResolveExample(context);
        if (example != null)
        {
            schema.Example = example;
        }

        // Also resolve named examples if any
        var namedExamples = _precedenceEngine.ResolveNamedExamples(context);
        if (namedExamples.Count > 0 && (namedExamples.Count > 1 || !namedExamples.ContainsKey("generated")))
        {
            // Clear the single example in favor of named examples
            schema.Example = null;

            // Note: OpenApiSchema doesn't have an Examples property in Microsoft.OpenApi 1.6.22
            // This would be added at the operation level, not schema level
            // For now, we keep the single example approach
            var firstExample = namedExamples.Values.FirstOrDefault();
            if (firstExample?.Value != null)
            {
                schema.Example = firstExample.Value;
            }
        }
    }

    /// <summary>
    /// Creates an example context from a type and optional explicit examples.
    /// </summary>
    /// <param name="type">The CLR type to create context for.</param>
    /// <param name="singleExample">Optional single example value.</param>
    /// <returns>An example context configured for the type.</returns>
    public ExampleContext CreateExampleContext(Type type, IOpenApiAny? singleExample = null)
    {
        ArgumentNullException.ThrowIfNull(type);

        var context = new ExampleContext
        {
            SingleExample = singleExample,
            Schema = GenerateSchema(type)
        };

        return context;
    }
}
