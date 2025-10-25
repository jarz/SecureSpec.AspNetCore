# Dictionary and AdditionalProperties Usage

This document provides examples of using dictionaries with SecureSpec.AspNetCore for OpenAPI schema generation.

## Overview

SecureSpec.AspNetCore automatically maps string-keyed dictionary types to OpenAPI's `additionalProperties` schema pattern. This allows you to define flexible, dynamic object structures in your API models.

## Supported Dictionary Types

The following dictionary types are automatically recognized and mapped:

- `Dictionary<string, T>`
- `IDictionary<string, T>`
- `IReadOnlyDictionary<string, T>`

**Note**: Only **string-keyed** dictionaries are mapped to `additionalProperties`. Dictionaries with other key types (e.g., `Dictionary<int, T>`) are treated as basic objects without additional properties, as OpenAPI does not support non-string keys for `additionalProperties`.

## Basic Dictionary Mapping

### Simple Value Types

```csharp
// Dictionary<string, int>
// OpenAPI: type: object, additionalProperties: { type: integer, format: int32 }
public class UserScores
{
    public Dictionary<string, int> Scores { get; set; }
}

// Dictionary<string, string>
// OpenAPI: type: object, additionalProperties: { type: string }
public class UserMetadata
{
    public Dictionary<string, string> Metadata { get; set; }
}
```

### Complex Value Types

```csharp
public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
}

// Dictionary<string, Address>
// OpenAPI: type: object, additionalProperties: { $ref: '#/components/schemas/Address' }
public class UserAddresses
{
    public Dictionary<string, Address> Addresses { get; set; }
}
```

## Nested Dictionaries

Dictionaries can be nested to create hierarchical structures:

```csharp
// Dictionary<string, Dictionary<string, int>>
// OpenAPI: type: object
//   additionalProperties:
//     type: object
//     additionalProperties: { type: integer, format: int32 }
public class DepartmentScores
{
    public Dictionary<string, Dictionary<string, int>> ScoresByDepartment { get; set; }
}
```

## Nullable Values

### OpenAPI 3.0

For OpenAPI 3.0, nullable dictionary values use the `nullable: true` flag:

```csharp
// Dictionary<string, int?>
// OpenAPI 3.0: type: object
//   additionalProperties: { type: integer, format: int32, nullable: true }
public class OptionalScores
{
    public Dictionary<string, int?> Scores { get; set; }
}
```

### OpenAPI 3.1

For OpenAPI 3.1, nullable values use a type union with `null`:

```csharp
// Dictionary<string, int?>
// OpenAPI 3.1: type: object
//   additionalProperties:
//     anyOf:
//       - type: integer
//         format: int32
//       - type: null
public class OptionalScores
{
    public Dictionary<string, int?> Scores { get; set; }
}
```

## Nullable Dictionaries

The dictionary itself can also be nullable:

### OpenAPI 3.0

```csharp
// Dictionary<string, int>?
// OpenAPI 3.0: type: object, nullable: true
//   additionalProperties: { type: integer, format: int32 }
public class UserData
{
    public Dictionary<string, int>? OptionalScores { get; set; }
}
```

### OpenAPI 3.1

```csharp
// Dictionary<string, int>?
// OpenAPI 3.1:
//   anyOf:
//     - type: object
//       additionalProperties: { type: integer, format: int32 }
//     - type: null
public class UserData
{
    public Dictionary<string, int>? OptionalScores { get; set; }
}
```

## Dictionary with Arrays

Dictionary values can be arrays or lists:

```csharp
// Dictionary<string, List<string>>
// OpenAPI: type: object
//   additionalProperties:
//     type: array
//     items: { type: string }
public class UserTags
{
    public Dictionary<string, List<string>> TagsByCategory { get; set; }
}
```

## Special Type Mappings

Dictionary values can use any supported CLR type, including:

```csharp
// Dictionary with Guid values
// OpenAPI: additionalProperties: { type: string, format: uuid }
public Dictionary<string, Guid> UserIds { get; set; }

// Dictionary with DateTime values
// OpenAPI: additionalProperties: { type: string, format: date-time }
public Dictionary<string, DateTime> Timestamps { get; set; }

// Dictionary with Decimal values
// OpenAPI: additionalProperties: { type: number }
public Dictionary<string, decimal> Prices { get; set; }
```

## Interface Types

Both `IDictionary<string, T>` and `IReadOnlyDictionary<string, T>` are supported:

```csharp
public class UserData
{
    // All three generate the same OpenAPI schema
    public Dictionary<string, int> Scores1 { get; set; }
    public IDictionary<string, int> Scores2 { get; set; }
    public IReadOnlyDictionary<string, int> Scores3 { get; set; }
}
```

## Non-String Keys

Dictionaries with non-string keys are **not** mapped to `additionalProperties`:

```csharp
// Dictionary<int, string>
// OpenAPI: type: object (basic object, no additionalProperties)
public class NumericIndex
{
    public Dictionary<int, string> Items { get; set; }
}
```

This is because OpenAPI's `additionalProperties` only supports string keys. Non-string-keyed dictionaries are treated as opaque objects.

## Configuration

### Spec Version

The OpenAPI specification version affects how nullability is represented:

```csharp
services.AddSecureSpec(options =>
{
    // Use OpenAPI 3.0 (nullable: true flag)
    options.Schema.SpecVersion = SchemaSpecVersion.OpenApi3_0;

    // Or use OpenAPI 3.1 (type unions with null)
    options.Schema.SpecVersion = SchemaSpecVersion.OpenApi3_1;
});
```

## Deterministic Serialization

Dictionary schemas are serialized deterministically, ensuring:

- Schema keys are in lexical order
- Hash values are consistent across rebuilds
- ETags are stable for caching

This is automatically handled by the `CanonicalSerializer` and verified by the test suite.

## Future Enhancements

The following features are planned for future releases:

- **AC 433**: DataAnnotations on dictionary value types (e.g., `[Range]`, `[StringLength]`)
- **AC 434**: Conflict detection when a class has both explicit properties and dictionary behavior
- **AC 435**: Unicode normalization for dictionary keys at runtime
- **AC 437**: Support for `additionalProperties: false` to block extension properties

## Examples

For complete working examples, see:

- [Basic Example](../examples/BasicExample/Program.cs) - Basic API setup
- [Dictionary Acceptance Tests](../tests/SecureSpec.AspNetCore.Tests/DictionaryAcceptanceTests.cs) - Comprehensive test coverage

## Related Documentation

- [Getting Started Guide](GETTING_STARTED.md)
- [PRD - Dictionaries & AdditionalProperties](PRD.md#545-dictionaries--additionalproperties-ac-432437)
- [Implementation Progress](../IMPLEMENTATION_PROGRESS.md)

## Acceptance Criteria Coverage

This implementation covers the following acceptance criteria from Issue 1.6:

- ✅ **AC 432**: Dictionary emits `additionalProperties` referencing value schema
- ✅ **AC 436**: Ordering of dictionary value schema keys is lexical (via canonical serialization)
- ⏳ **AC 433**: DataAnnotations on value type (requires Issue 1.7)
- ⏳ **AC 434**: Conflict detection (future enhancement)
- ⏳ **AC 435**: Unicode normalization (future enhancement)
- ⏳ **AC 437**: `additionalProperties: false` (future enhancement)
