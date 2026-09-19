namespace KaraokeList.Api.IntegrationTests;

internal static class IntegrationTestConnection
{
    public const string SkipReason =
        "Unable to initialize the SQLite integration test database.";

    public static bool IntegrationTestsRequired =>
        string.Equals(
            Environment.GetEnvironmentVariable("KARAOKE_INTEGRATION_TESTS_REQUIRED"),
            "true",
            StringComparison.OrdinalIgnoreCase);

    public static string Resolve()
    {
        var fromEnv = Environment.GetEnvironmentVariable("KARAOKE_TEST_SQL_CONNECTION");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv;
        }

        var path = Path.Combine(Path.GetTempPath(), "KaraokeList_IntegrationTest.db");
        return $"Data Source={path}";
    }

    public static bool ShouldSkipIntegrationTests() => false;

    public static bool CanConnect(string connectionString) => TryPrepareDatabase(connectionString);

    public static bool WaitUntilReady(string connectionString, TimeSpan? timeout = null, TimeSpan? pollInterval = null) =>
        TryPrepareDatabase(connectionString);

    public static bool EnsureDatabaseReady() => TryPrepareDatabase(Resolve());

    private static bool TryPrepareDatabase(string connectionString)
    {
        try
        {
            KaraokeList.Data.KaraokeDbPaths.EnsureDataSourceDirectory(connectionString);
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
            connection.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
