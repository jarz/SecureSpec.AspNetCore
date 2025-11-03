namespace SecureSpec.AspNetCore.IntegrationTests;

public static class Program
{
	public static Task<int> Main(string[] args)
	{
		ArgumentNullException.ThrowIfNull(args);
		return FixtureRegenerator.RunAsync(args);
	}
}
