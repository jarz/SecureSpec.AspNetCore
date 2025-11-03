using Microsoft.OpenApi.Readers;

namespace SecureSpec.AspNetCore.IntegrationTests;

/// <summary>
/// Provides helpers for canonicalizing OpenAPI documents.
/// </summary>
internal static class SchemaCanonicalizer
{
    private static readonly OpenApiStringReader Reader = new();

    public static string CanonicalizeJson(string payload)
    {
        var document = Reader.Read(payload, out var diagnostic);
        EnsureSuccess(diagnostic, "JSON");
        return CanonicalSerializer.SerializeToJson(document);
    }

    public static string CanonicalizeYaml(string payload)
    {
        var document = Reader.Read(payload, out var diagnostic);
        EnsureSuccess(diagnostic, "YAML");
        return CanonicalSerializer.SerializeToYaml(document);
    }

    private static void EnsureSuccess(OpenApiDiagnostic diagnostic, string format)
    {
        if (diagnostic.Errors.Count == 0)
        {
            return;
        }

        var message = string.Join(Environment.NewLine, diagnostic.Errors.Select(e => e.Message));
        throw new InvalidOperationException($"Failed to parse OpenAPI {format}:{Environment.NewLine}{message}");
    }
}
