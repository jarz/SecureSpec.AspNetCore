# XML Documentation Ingestion

SecureSpec.AspNetCore supports loading XML documentation from multiple files to enrich your OpenAPI schemas with descriptions from your code's XML comments.

## Features

- **Multi-file support**: Load documentation from multiple XML files
- **Ordered merge**: Later files override earlier ones on conflicts
- **Conflict detection**: Diagnostics emitted when the same member is documented in multiple files
- **Automatic integration**: Documentation automatically applied to generated schemas

## Configuration

Add XML documentation file paths to your `SchemaOptions`:

```csharp
builder.Services.AddSecureSpec(options =>
{
    // Load XML documentation files in order
    options.Schema.XmlDocumentationPaths.Add("MyApi.xml");
    options.Schema.XmlDocumentationPaths.Add("MyApi.Extensions.xml");
    
    // Later files override earlier ones on conflicts
});
```

## Enabling XML Documentation Generation

In your `.csproj` file:

```xml
<PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

## Supported XML Tags

The following XML documentation tags are currently supported:

- `<summary>` - Applied to schema descriptions
- `<remarks>` - Additional documentation
- `<param>` - Parameter descriptions (for methods)
- `<returns>` - Return value descriptions

## Example

```csharp
/// <summary>
/// Represents a weather forecast for a specific date.
/// </summary>
/// <param name="Date">The date of the forecast.</param>
/// <param name="TemperatureC">The temperature in Celsius.</param>
/// <param name="Summary">A brief summary of the weather conditions.</param>
sealed record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    /// <summary>
    /// Gets the temperature in Fahrenheit, calculated from Celsius.
    /// </summary>
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
```

The `<summary>` tag on `WeatherForecast` will be applied to the schema's `description` field in the generated OpenAPI document.

## Diagnostic Codes

- **XML001** (Warning): XML documentation file not found
- **XML002** (Info): XML documentation conflict - member redefined in later file
- **XML003** (Info): XML documentation loaded successfully
- **XML004** (Error): Failed to load XML documentation file

## Implementation Details

### Ordered Merge

When multiple XML documentation files define the same member, the **last file wins**:

```csharp
options.Schema.XmlDocumentationPaths.Add("Base.xml");      // First
options.Schema.XmlDocumentationPaths.Add("Override.xml");  // Wins if same members
```

This allows you to:
- Override base library documentation
- Provide environment-specific documentation
- Layer documentation from multiple sources

### Whitespace Normalization

XML documentation whitespace is normalized:
- Leading/trailing whitespace removed from each line
- Empty lines removed
- Lines joined with single spaces

This ensures clean, readable descriptions in the OpenAPI output.

## Best Practices

1. **Generate XML docs in Release builds**: Add XML generation only for Release configuration to avoid debug overhead
2. **Deploy XML files with your application**: Ensure XML files are copied to the output directory
3. **Use consistent member names**: XML member names must match exactly (e.g., `T:Namespace.Type`)
4. **Handle missing files gracefully**: The provider logs warnings but continues if files are not found
5. **Order matters**: Place most specific/override files last in the collection

## Performance Considerations

- XML files are loaded once during application startup
- Documentation is cached in memory for fast lookup
- No performance impact during schema generation (constant-time lookups)
- Memory usage scales with the number of documented members

## Future Enhancements

Planned enhancements include:
- Support for `<example>` tags
- Support for `<exception>` tags
- Property-level documentation application
- Method parameter documentation
- Inherited documentation resolution
