using Microsoft.Data.SqlClient;

namespace KaraokeList.Api.IntegrationTests;

internal static class IntegrationTestConnection
{
    public const string SkipReason =
        "SQL Server not available. Install LocalDB or set KARAOKE_TEST_SQL_CONNECTION.";

    public static string Resolve()
    {
        var fromEnv = Environment.GetEnvironmentVariable("KARAOKE_TEST_SQL_CONNECTION");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv;
        }

        return "Server=(localdb)\\MSSQLLocalDB;Database=KaraokeList_IntegrationTest;Trusted_Connection=True;TrustServerCertificate=True";
    }

    /// <summary>
    /// Probes SQL Server reachability. Uses master because the app database may not exist until MigrateAsync runs.
    /// </summary>
    public static bool CanConnect(string connectionString)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = "master"
            };
            using var connection = new SqlConnection(builder.ConnectionString);
            connection.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
