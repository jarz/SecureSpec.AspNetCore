# API Design Guidelines

This document defines the API design principles and patterns for SecureSpec.AspNetCore.

## Table of Contents

- [Design Principles](#design-principles)
- [Configuration API](#configuration-api)
- [Extension Points](#extension-points)
- [Naming Conventions](#naming-conventions)
- [Error Handling](#error-handling)
- [Versioning Strategy](#versioning-strategy)

## Design Principles

### 1. Consistency with ASP.NET Core

SecureSpec.AspNetCore follows ASP.NET Core conventions:

- Builder pattern for configuration
- Dependency injection for services
- Options pattern for settings
- Middleware pipeline integration

### 2. Fail-Safe Defaults

All defaults prioritize security and correctness:

```csharp
// Secure by default
options.Security.RequirePKCE = true;  // Cannot be disabled
options.Security.AllowImplicitFlow = false;  // OMIT-SECURE
options.Integrity.EnforceChecks = true;
options.RateLimiting.Enabled = true;
```

### 3. Explicit Over Implicit

Configuration must be explicit, no magic behavior:

```csharp
// ❌ Bad: Implicit behavior
options.IncludeAllControllers();  // What does "all" mean?

// ✅ Good: Explicit selection
options.Discovery.IncludeControllers(c => 
    c.Where(controller => controller.HasAttribute<ApiControllerAttribute>())
);
```

### 4. Discoverability

APIs should be discoverable through IntelliSense:

- Fluent interfaces with method chaining
- XML documentation on all public members
- Descriptive parameter names
- Example code in documentation

## Configuration API

### Service Registration

```csharp
public static class SecureSpecServiceCollectionExtensions
{
    /// <summary>
    /// Adds SecureSpec services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSecureSpec(
        this IServiceCollection services,
        Action<SecureSpecOptions> configure)
    {
        // Implementation
    }
}
```

### Document Configuration

```csharp
public class SecureSpecOptions
{
    /// <summary>
    /// Gets the collection of OpenAPI documents to generate.
    /// </summary>
    public DocumentCollection Documents { get; }

    /// <summary>
    /// Gets security configuration options.
    /// </summary>
    public SecurityOptions Security { get; }

    /// <summary>
    /// Gets schema generation options.
    /// </summary>
    public SchemaOptions Schema { get; }

    /// <summary>
    /// Gets UI configuration options.
    /// </summary>
    public UIOptions UI { get; }
}
```

### Fluent Configuration

```csharp
builder.Services.AddSecureSpec(options =>
{
    // Document configuration
    options.Documents.Add("v1", doc =>
    {
        doc.Info.Title = "My API";
        doc.Info.Version = "1.0.0";
        doc.Info.Description = "API description";
        
        doc.Servers.Add(server =>
        {
            server.Url = "https://api.example.com";
            server.Description = "Production server";
        });
    });

    // Schema configuration
    options.Schema.IdStrategy = type => $"{type.Namespace}.{type.Name}";
    options.Schema.MaxDepth = 32;
    
    // Security configuration
    options.Security.OAuth.AuthorizationCode(oauth =>
    {
        oauth.AuthorizationUrl = new Uri("https://auth.example.com/authorize", UriKind.Absolute);
        oauth.TokenUrl = new Uri("https://auth.example.com/token", UriKind.Absolute);
        oauth.Scopes.Add("read", "Read access");
        oauth.Scopes.Add("write", "Write access");
        // PKCE is always required, cannot be disabled
    });

    // UI configuration
    options.UI.DeepLinking = true;
    options.UI.DisplayOperationId = true;
    options.UI.DefaultModelsExpandDepth = 1;
});
```

## Extension Points

### 1. Filters

Filters provide non-destructive document modification:

```csharp
public interface ISchemaFilter
{
    /// <summary>
    /// Applies modifications to a schema.
    /// </summary>
    /// <param name="schema">The schema to modify.</param>
    /// <param name="context">Filter context with metadata.</param>
    void Apply(OpenApiSchema schema, SchemaFilterContext context);
}

public interface IOperationFilter
{
    void Apply(OpenApiOperation operation, OperationFilterContext context);
}

public interface IDocumentFilter
{
    void Apply(OpenApiDocument document, DocumentFilterContext context);
}
```

**Usage**:
```csharp
public class AddSchemaExamplesFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(Product))
        {
            schema.Example = new OpenApiObject
            {
                ["id"] = new OpenApiInteger(1),
                ["name"] = new OpenApiString("Widget")
            };
        }
    }
}

// Registration
options.Filters.AddSchemaFilter<AddSchemaExamplesFilter>();
```

### 2. Type Mappers

Custom type-to-schema mappings:

```csharp
public interface ITypeMapper
{
    bool CanMap(Type type);
    OpenApiSchema Map(Type type, SchemaRepository repository);
}

// Usage
options.Schema.TypeMappings.Add<CustomType>(mapping =>
{
    mapping.Type = "string";
    mapping.Format = "custom";
    mapping.Pattern = @"^[A-Z]{3}-\d{4}$";
});
```

### 3. Operation Processors

Add or modify operation metadata:

```csharp
public interface IOperationProcessor
{
    void Process(OpenApiOperation operation, MethodInfo methodInfo);
}
```

## Naming Conventions

### Property Names

- **PascalCase** for all properties
- **Descriptive** names, avoid abbreviations
- **No Hungarian notation**

```csharp
// ✅ Good
public Uri AuthorizationUrl { get; set; }
public int MaxDepth { get; set; }

// ❌ Bad
public string authUrl { get; set; }  // Wrong casing
public int max { get; set; }         // Too vague
public int iMaxDepth { get; set; }   // Hungarian notation
```

### Method Names

- **Verb-noun** pattern for actions
- **Get** prefix for retrieval methods
- **Set** prefix for modification methods

```csharp
// ✅ Good
public void AddDocument(string name, Action<Document> configure);
public Document GetDocument(string name);
public void RemoveDocument(string name);

// ❌ Bad
public void Document(string name);  // Unclear action
public Document Retrieve(string name);  // Use "Get" instead
```

### Option Class Names

- Suffix with **Options**
- Grouped by concern

```csharp
public class SecurityOptions { }
public class SchemaOptions { }
public class UIOptions { }
```

## Error Handling

### Exception Types

```csharp
/// <summary>
/// Base exception for all SecureSpec errors.
/// </summary>
public class SecureSpecException : Exception
{
    public string DiagnosticCode { get; }
}

/// <summary>
/// Thrown when schema generation fails.
/// </summary>
public class SchemaGenerationException : SecureSpecException { }

/// <summary>
/// Thrown when integrity checks fail.
/// </summary>
public class IntegrityException : SecureSpecException { }

/// <summary>
/// Thrown when configuration is invalid.
/// </summary>
public class ConfigurationException : SecureSpecException { }
```

### Error Messages

Error messages should:
- Be clear and actionable
- Include diagnostic codes
- Not leak sensitive information
- Provide context

```csharp
// ✅ Good
throw new SchemaGenerationException(
    "Schema generation failed for type 'Customer'. " +
    "Maximum depth of 32 exceeded. [SCH001-DEPTH]",
    diagnosticCode: "SCH001-DEPTH"
);

// ❌ Bad
throw new Exception("Error");  // Too vague
throw new Exception($"Failed: {sensitiveData}");  // Leaks data
```

### Validation

Configuration validation at startup:

```csharp
public void ValidateOptions(SecureSpecOptions options)
{
    if (options.Documents.Count == 0)
    {
        throw new ConfigurationException(
            "At least one document must be configured. " +
            "Use options.Documents.Add(...) to add a document."
        );
    }

    foreach (var doc in options.Documents)
    {
        if (string.IsNullOrEmpty(doc.Info.Title))
        {
            throw new ConfigurationException(
                $"Document '{doc.Name}' must have a title. " +
                $"Set doc.Info.Title in configuration."
            );
        }
    }
}
```

## Versioning Strategy

### Semantic Versioning

SecureSpec.AspNetCore follows SemVer 2.0:

- **Major**: Breaking API changes
- **Minor**: New features, backward compatible
- **Patch**: Bug fixes, backward compatible

### API Stability

**Stable APIs** (will not break in minor versions):
- Public classes and interfaces
- Configuration options
- Extension points (filters, mappers)

**Unstable APIs** (may change):
- Internal implementation details
- Experimental features (marked with `[Experimental]`)

### Breaking Change Policy

Breaking changes only in major versions:

```csharp
// v1.0 - Original API
public void AddDocument(string name);

// v1.1 - Backward compatible addition
public void AddDocument(string name, string version = "1.0");

// v2.0 - Breaking change (parameter reordered)
public void AddDocument(string version, string name);
```

### Deprecation Process

1. Mark as `[Obsolete]` in current version
2. Document replacement in XML comments
3. Remove in next major version

```csharp
[Obsolete("Use AddDocument(name, configure) instead. Will be removed in v2.0.")]
public void AddDocument(string name)
{
    // Old implementation
}

/// <summary>
/// Adds a document to the collection.
/// </summary>
public void AddDocument(string name, Action<Document> configure)
{
    // New implementation
}
```

## Code Examples

### Complete Configuration Example

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSecureSpec(options =>
{
    // Document
    options.Documents.Add("v1", doc =>
    {
        doc.Info.Title = "My API";
        doc.Info.Version = "1.0";
        doc.Info.Contact.Name = "API Team";
        doc.Info.Contact.Email = "api@example.com";
        
        doc.Servers.Add(server =>
        {
            server.Url = "https://api.example.com";
            server.Variables.Add("environment", variable =>
            {
                variable.Default = "production";
                variable.Enum = new[] { "production", "staging" };
            });
        });
    });

    // Schema
    options.Schema.IdStrategy = type => 
        $"{type.Namespace}.{type.Name}";
    options.Schema.MaxDepth = 32;
    options.Schema.TypeMappings.Add<CustomerId>(m =>
    {
        m.Type = "string";
        m.Format = "customer-id";
        m.Pattern = @"^CUST-\d{6}$";
    });

    // Security
    options.Security.OAuth.AuthorizationCode(oauth =>
    {
        oauth.AuthorizationUrl = new Uri("https://auth.example.com/authorize", UriKind.Absolute);
        oauth.TokenUrl = new Uri("https://auth.example.com/token", UriKind.Absolute);
        oauth.Scopes.Add("api.read", "Read access");
        oauth.Scopes.Add("api.write", "Write access");
    });

    // Filters
    options.Filters.AddSchemaFilter<AddExamplesFilter>();
    options.Filters.AddOperationFilter<AddSecurityFilter>();
    
    // UI
    options.UI.DeepLinking = true;
    options.UI.DisplayOperationId = true;
});

var app = builder.Build();

app.UseSecureSpec();
app.UseSecureSpecUI();

app.Run();
```

## Best Practices

1. **Use dependency injection** for extensibility
2. **Validate early** at configuration time
3. **Provide defaults** for all optional settings
4. **Document behavior** with XML comments
5. **Follow async patterns** where appropriate
6. **Use cancellation tokens** for long operations
7. **Throw specific exceptions** with diagnostic codes
8. **Log important events** with structured data

## References

- [ARCHITECTURE.md](../ARCHITECTURE.md) - System architecture
- [DESIGN.md](DESIGN.md) - Design decisions
- [PRD.md](PRD.md) - Product requirements

---

**Last Updated**: 2025-10-19  
**Version**: 1.0
