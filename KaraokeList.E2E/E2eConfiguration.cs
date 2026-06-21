namespace KaraokeList.E2E;

internal static class E2eConfiguration
{
    public const string DefaultWebBaseUrl = "http://localhost:5262";
    public const string DefaultApiBaseUrl = "http://localhost:5299";
    public const string TestPassword = "TestPassw0rd!23";
    public const string TestInviteCode = "e2e-test-invite";

    public static string WebBaseUrl =>
        TrimTrailingSlash(Environment.GetEnvironmentVariable("KARAOKE_E2E_WEB_URL") ?? DefaultWebBaseUrl);

    public static string ApiBaseUrl =>
        TrimTrailingSlash(Environment.GetEnvironmentVariable("KARAOKE_E2E_API_URL") ?? DefaultApiBaseUrl);

    public static bool ManualServers =>
        string.Equals(Environment.GetEnvironmentVariable("KARAOKE_E2E_MANUAL"), "true", StringComparison.OrdinalIgnoreCase);

    public static bool AutoStartServers =>
        !ManualServers
        && !string.Equals(Environment.GetEnvironmentVariable("KARAOKE_E2E_NO_AUTOSTART"), "true", StringComparison.OrdinalIgnoreCase);

    private static string TrimTrailingSlash(string url) => url.TrimEnd('/');
}
