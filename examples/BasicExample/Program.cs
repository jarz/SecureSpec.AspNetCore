using SecureSpec.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Add SecureSpec for OpenAPI documentation
builder.Services.AddSecureSpec(options =>
{
    options.Documents.Add("v1", doc =>
    {
        doc.Info.Title = "Weather API";
        doc.Info.Version = "1.0.0";
        doc.Info.Description = "A simple weather forecast API";
    });

    // Configure schema generation
    options.Schema.MaxDepth = 32;
    options.Schema.UseEnumStrings = true;

    // Configure UI
    options.UI.DeepLinking = true;
    options.UI.DisplayOperationId = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
