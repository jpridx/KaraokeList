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

    public static bool CanConnect(string connectionString)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
