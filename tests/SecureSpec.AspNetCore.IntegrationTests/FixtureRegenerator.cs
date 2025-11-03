using System.Diagnostics.CodeAnalysis;

namespace SecureSpec.AspNetCore.IntegrationTests;

/// <summary>
/// Provides a tiny CLI surface for regenerating schema fixtures.
/// </summary>
[SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "Console output in fluent expressions")]
internal static class FixtureRegenerator
{
    private const string Command = "regenerate-fixtures";

    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0)
        {
            // No CLI invocation requested; running under test host.
            return 0;
        }

        if (IsHelp(args[0]))
        {
            PrintUsage();
            return 0;
        }

        if (!string.Equals(args[0], Command, StringComparison.OrdinalIgnoreCase))
        {
            PrintUsage();
            return 1;
        }

        await RegenerateAsync().ConfigureAwait(false);
        return 0;
    }

    private static bool IsHelp(string argument)
    {
        return string.Equals(argument, "--help", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(argument, "-h", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(argument, "help", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task RegenerateAsync()
    {
        Console.WriteLine("Regenerating SecureSpec schema fixtures...");

        await using var app = SecureSpecIntegrationApplication.Build();
        app.Urls.Clear();
        app.Urls.Add("http://127.0.0.1:0");

        await app.StartAsync().ConfigureAwait(false);
        try
        {
            var baseAddress = new Uri(app.Urls.Single(), UriKind.Absolute);
            using var client = new HttpClient { BaseAddress = baseAddress };

            var jsonPayload = await FetchAsync(client, "/openapi/v1.json").ConfigureAwait(false);
            var canonicalJson = SchemaCanonicalizer.CanonicalizeJson(jsonPayload);
            await FixtureStore.WriteAsync("v1.json", canonicalJson).ConfigureAwait(false);
            Console.WriteLine("✔ Updated Fixtures/v1.json");

            var yamlPayload = await FetchAsync(client, "/openapi/v1.yaml").ConfigureAwait(false);
            var canonicalYaml = SchemaCanonicalizer.CanonicalizeYaml(yamlPayload);
            await FixtureStore.WriteAsync("v1.yaml", canonicalYaml).ConfigureAwait(false);
            Console.WriteLine("✔ Updated Fixtures/v1.yaml");
        }
        finally
        {
            await app.StopAsync().ConfigureAwait(false);
        }

        Console.WriteLine("Fixture regeneration complete.");
    }

    private static async Task<string> FetchAsync(HttpClient client, string relativePath)
    {
        var response = await client.GetAsync(new Uri(relativePath, UriKind.Relative)).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }

    private static void PrintUsage()
    {
        Console.WriteLine("SecureSpec.AspNetCore integration testing utilities");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project tests/SecureSpec.AspNetCore.IntegrationTests -- regenerate-fixtures");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  regenerate-fixtures   Recreates canonical JSON and YAML documents under Fixtures/");
    }
}
