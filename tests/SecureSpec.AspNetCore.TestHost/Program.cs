namespace SecureSpec.AspNetCore.TestHost;

#pragma warning disable CA1052 // Static holder types should be Static
public partial class Program
{
    protected Program()
    {
    }

    public static async Task Main(string[] args)
    {
        var app = SecureSpecIntegrationApplication.Build(args);
        await app.RunAsync().ConfigureAwait(false);
    }
}
#pragma warning restore CA1052 // Static holder types should be Static
