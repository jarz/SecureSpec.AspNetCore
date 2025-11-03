namespace SecureSpec.AspNetCore.IntegrationTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PlaywrightCollection : ICollectionFixture<PlaywrightHostFixture>
{
    public const string Name = "SecureSpec.Playwright";
}
