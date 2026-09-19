using Microsoft.Data.Sqlite;

namespace KaraokeList.Data;

public static class KaraokeDbPaths
{
    /// <summary>
    /// Ensures the directory for a file-based SQLite connection string exists (no-op for :memory:).
    /// </summary>
    public static void EnsureDataSourceDirectory(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        var dataSource = builder.DataSource;
        if (string.IsNullOrWhiteSpace(dataSource) || dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var fullPath = Path.GetFullPath(dataSource);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
