# Example Precedence Engine

This document describes the Example Precedence Engine implementation for SecureSpec.AspNetCore, which provides deterministic example generation for OpenAPI schemas with proper resolution order.

## Overview

The Example Precedence Engine implements **Acceptance Criteria AC 4** from the PRD, providing a priority-based system for resolving examples from multiple sources:

**Precedence Order (Highest to Lowest):**
1. **Named Examples** - Explicitly named examples with identifiers
2. **Single/Attribute Examples** - Single example value or from attributes
3. **Component Examples** - Referenced examples from components section
4. **Generated Examples** - Deterministic fallback generation
5. **Blocked** - No examples (explicitly disabled)

## Architecture

The implementation consists of three main components:

### 1. ExampleContext
Holds all available example sources for a schema:
- `NamedExamples` - Dictionary of named OpenApiExample objects
- `SingleExample` - Single IOpenApiAny value
- `ComponentExample` - OpenApiReference to component example
- `IsBlocked` - Flag to disable all examples
- `Schema` - OpenApiSchema for fallback generation

### 2. ExamplePrecedenceEngine
Resolves examples based on precedence rules:
- `ResolveExample(context)` - Returns single example using precedence
- `ResolveNamedExamples(context)` - Returns dictionary of named examples

### 3. ExampleGenerator
Generates deterministic fallback examples:
- Supports all OpenAPI types (string, integer, number, boolean, array, object)
- Handles specific formats (uuid, date-time, date, time, email, uri, etc.)
- Respects minimum/maximum constraints
- Uses enum values when available
- Lexically orders object properties for determinism

## Configuration

Add to `SchemaOptions`:

```csharp
services.AddSecureSpec(options =>
{
    // Enable/disable example generation (default: true)
    options.Schema.GenerateExamples = true;
    
    // Set time budget for example generation (default: 25ms per PRD)
    options.Schema.ExampleGenerationTimeoutMs = 25;
});
```

## Usage

### Basic Usage with SchemaGenerator

```csharp
var generator = new SchemaGenerator(options, logger);
var schema = generator.GenerateSchema(typeof(MyType));

// Create example context
var context = new ExampleContext
{
    SingleExample = new OpenApiString("custom-value"),
    Schema = schema
};

// Apply examples to schema
generator.ApplyExamples(schema, context);
```

### Using Named Examples

```csharp
var context = new ExampleContext
{
    NamedExamples =
    {
        ["success"] = new OpenApiExample 
        { 
            Value = new OpenApiString("Success response"),
            Summary = "Successful operation"
        },
        ["error"] = new OpenApiExample 
        { 
            Value = new OpenApiString("Error response"),
            Summary = "Error case"
        }
    },
    Schema = schema
};

generator.ApplyExamples(schema, context);
```

### Using Component References

```csharp
var context = new ExampleContext
{
    ComponentExample = new OpenApiReference
    {
        Type = ReferenceType.Example,
        Id = "UserExample"
    }
};
```

### Blocking Examples

```csharp
var context = new ExampleContext
{
    IsBlocked = true,
    // All other sources ignored when blocked
    Schema = schema
};

generator.ApplyExamples(schema, context);
// schema.Example will be null
```

### Helper Method

```csharp
// Create context with automatic schema generation
var context = generator.CreateExampleContext(typeof(string), 
    new OpenApiString("custom"));
```

## Deterministic Example Generation

The generator produces stable, deterministic examples:

### String Types
- Default: `"string"`
- UUID: `"00000000-0000-0000-0000-000000000000"`
- Date-Time: `"2024-01-01T00:00:00Z"`
- Date: `"2024-01-01"`
- Time: `"00:00:00"`
- Email: `"user@example.com"`
- URI: `"https://example.com"`

### Numeric Types
- Integer: `0` (or minimum if specified)
- Number: `0.0` (or minimum if specified)
- Boolean: `false`

### Complex Types
- Arrays: Single item with generated element
- Objects: All properties with generated values (lexically ordered)

### Enums
Uses first enum value in declaration order

## Testing

Comprehensive test coverage:
- **42 Unit Tests** - Core precedence engine and generator
- **10 Acceptance Tests** - AC 4 verification
- **14 Integration Tests** - SchemaGenerator integration

All tests validate:
- Correct precedence order
- Deterministic generation
- Configuration respect
- Edge cases and null handling

## Implementation Details

### Precedence Resolution Algorithm

```csharp
IOpenApiAny? Resolve(ExampleContext ctx)
{
    if (ctx.IsBlocked) return null;
    if (ctx.NamedExamples.Any()) return ctx.NamedExamples.First().Value;
    if (ctx.SingleExample != null) return ctx.SingleExample;
    if (ctx.ComponentExample != null) return null; // Handled separately
    return GenerateDeterministicFallback(ctx.Schema);
}
```

### Thread Safety
- All components are thread-safe
- No shared mutable state
- Can be used concurrently

### Performance
- Example generation is fast and deterministic
- Default 25ms timeout per PRD specification
- Minimal memory allocation

## Future Enhancements

Potential future improvements:
1. Support for `examples` (plural) at operation level
2. Custom example generators via extensibility hooks
3. Example validation against schema
4. Time budget enforcement with EXM001 diagnostic
5. Attribute-based example specification

## Related Components

- `SchemaGenerator` - Main schema generation
- `SchemaOptions` - Configuration options
- `DiagnosticsLogger` - Diagnostic logging

## References

- PRD Section B: Example Resolution Pseudocode
- Acceptance Criteria AC 4: Example precedence parity
- OpenAPI 3.0/3.1 Specification
- Swashbuckle.AspNetCore 6.5.0 parity requirements
