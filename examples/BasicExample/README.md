# BasicExample - SecureSpec.AspNetCore

This is a minimal example demonstrating how to integrate SecureSpec.AspNetCore into an ASP.NET Core Web API application.

## What This Example Shows

- How to add SecureSpec to your service collection
- Basic configuration of OpenAPI documents
- Schema generation options
- UI configuration

## Running the Example

```bash
dotnet run
```

The application will start and be available at `https://localhost:5001` (or the port shown in the console).

## Code Highlights

### Service Registration

```csharp
builder.Services.AddSecureSpec(options =>
{
    options.Documents.Add("v1", doc =>
    {
        doc.Info.Title = "Weather API";
        doc.Info.Version = "1.0.0";
        doc.Info.Description = "A simple weather forecast API";
    });

    options.Schema.MaxDepth = 32;
    options.Schema.UseEnumStrings = true;

    options.UI.DeepLinking = true;
    options.UI.DisplayOperationId = true;
});
```

This registers SecureSpec with:
- A single OpenAPI document named "v1"
- Basic schema generation settings
- UI configuration for deep linking and operation ID display

## Next Steps

Once the full implementation is complete, you'll be able to:
- Access the OpenAPI JSON at `/swagger/v1/swagger.json`
- Access the interactive UI at `/swagger`
- Try out API endpoints directly from the browser

## Learn More

- [Getting Started Guide](../../GETTING_STARTED.md)
- [Main Documentation](../../docs/README.md)
- [Architecture Overview](../../ARCHITECTURE.md)
