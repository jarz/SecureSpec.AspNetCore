namespace SecureSpec.AspNetCore.IntegrationTests;

/// <summary>
/// Helpers for loading and persisting schema fixtures.
/// </summary>
internal static class FixtureStore
{
    private static readonly string ProjectDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
    private static readonly string FixtureDirectory = Path.Combine(ProjectDirectory, "Fixtures");

    public static async Task<string> ReadAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var path = ResolveFixturePath(fileName);
        await using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async Task WriteAsync(string fileName, string content, CancellationToken cancellationToken = default)
    {
        var path = ResolveFixturePath(fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(content.AsMemory(), cancellationToken).ConfigureAwait(false);
    }

    private static string ResolveFixturePath(string fileName)
    {
        return Path.Combine(FixtureDirectory, fileName);
    }
}
