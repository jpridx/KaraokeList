using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

var sqlitePath = ResolveSqlitePath(args);
var sqlConnectionString = Environment.GetEnvironmentVariable("KARAOKE_SQL_CONNECTION")
    ?? throw new InvalidOperationException(
        "Set KARAOKE_SQL_CONNECTION to your Azure SQL / SQL Server source database.");

if (!File.Exists(sqlitePath))
{
    throw new FileNotFoundException(
        "SQLite file not found. Create schema first, for example:\n" +
        $"  dotnet ef database update --project KaraokeList.Api --connection \"Data Source={sqlitePath}\"",
        sqlitePath);
}

Console.WriteLine($"Source SQL Server: (from KARAOKE_SQL_CONNECTION)");
Console.WriteLine($"Target SQLite: {Path.GetFullPath(sqlitePath)}");

await using var sql = new SqlConnection(sqlConnectionString);
await sql.OpenAsync();

await using var sqlite = new SqliteConnection($"Data Source={sqlitePath}");
await sqlite.OpenAsync();

await using (var pragma = sqlite.CreateCommand())
{
    pragma.CommandText = "PRAGMA foreign_keys = OFF;";
    await pragma.ExecuteNonQueryAsync();
}

// Dependency-safe insert order (Identity and lists after catalog + performances).
string[] tables =
[
    "Genres",
    "Artists",
    "Singers",
    "Venues",
    "GenreGroups",
    "Songs",
    "GenreGroupGenres",
    "SongArtists",
    "Performances",
    "PerformanceParticipants",
    "SingerLists",
    "SingerListSongs",
    "SingerSongTicklerExclusions",
    "AspNetRoles",
    "AspNetUsers",
    "AspNetRoleClaims",
    "AspNetUserClaims",
    "AspNetUserLogins",
    "AspNetUserRoles",
    "AspNetUserTokens",
];

foreach (var table in tables)
{
    var copied = await CopyTableAsync(sql, sqlite, table);
    Console.WriteLine($"Copied {table}: {copied} row(s)");
}

await using (var pragma = sqlite.CreateCommand())
{
    pragma.CommandText = "PRAGMA foreign_keys = ON;";
    await pragma.ExecuteNonQueryAsync();
}

Console.WriteLine();
Console.WriteLine("Row count check (SQL Server → SQLite):");
foreach (var table in tables)
{
    var sourceCount = await CountSqlServerAsync(sql, table);
    var targetCount = await CountSqliteAsync(sqlite, table);
    var mark = sourceCount == targetCount ? "OK" : "MISMATCH";
    Console.WriteLine($"  {table,-32} {sourceCount,6} → {targetCount,6}  {mark}");
    if (sourceCount != targetCount)
    {
        throw new InvalidOperationException(
            $"Row count mismatch for {table}: {sourceCount} source row(s), {targetCount} target row(s).");
    }
}

Console.WriteLine();
Console.WriteLine("Done. Point KaraokeList.Api at this file and verify GET /api/version.");

static string ResolveSqlitePath(string[] args)
{
    if (args.Length > 0 && !args[0].StartsWith('-'))
    {
        return Path.GetFullPath(args[0]);
    }

    var fromEnv = Environment.GetEnvironmentVariable("KARAOKE_SQLITE_PATH");
    if (!string.IsNullOrWhiteSpace(fromEnv))
    {
        return Path.GetFullPath(fromEnv);
    }

    return Path.GetFullPath(Path.Combine("KaraokeList.Api", "Data", "karaokelist.dev.db"));
}

static async Task<long> CopyTableAsync(SqlConnection sql, SqliteConnection sqlite, string table)
{
    if (!await SqlServerTableExistsAsync(sql, table))
    {
        Console.WriteLine($"  (skip {table}: not present on SQL Server)");
        return 0;
    }

    if (!await SqliteTableExistsAsync(sqlite, table))
    {
        Console.WriteLine($"  (skip {table}: not present on SQLite — run EF migrations on target file first)");
        return 0;
    }

    await using var read = sql.CreateCommand();
    read.CommandText = $"SELECT * FROM [dbo].[{table}]";
    await using var reader = await read.ExecuteReaderAsync();
    if (!reader.HasRows && reader.FieldCount == 0)
    {
        return 0;
    }

    var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();

    await using (var delete = sqlite.CreateCommand())
    {
        delete.CommandText = $"DELETE FROM [{table}]";
        await delete.ExecuteNonQueryAsync();
    }

    long rows = 0;
    var columnList = string.Join(", ", columns.Select(c => $"[{c}]"));
    var paramList = string.Join(", ", columns.Select((_, i) => $"@p{i}"));

    while (await reader.ReadAsync())
    {
        await using var insert = sqlite.CreateCommand();
        insert.CommandText = $"INSERT INTO [{table}] ({columnList}) VALUES ({paramList})";
        for (var i = 0; i < columns.Length; i++)
        {
            insert.Parameters.AddWithValue($"@p{i}", reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i));
        }

        await insert.ExecuteNonQueryAsync();
        rows++;
    }

    if (columns.Contains("Id", StringComparer.OrdinalIgnoreCase))
    {
        await UpdateSqliteSequenceAsync(sqlite, table);
    }

    return rows;
}

static async Task UpdateSqliteSequenceAsync(SqliteConnection sqlite, string table)
{
    await using var cmd = sqlite.CreateCommand();
    cmd.CommandText = $"SELECT MAX(Id) FROM [{table}]";
    var max = await cmd.ExecuteScalarAsync();
    if (max is null or DBNull)
    {
        return;
    }

    // AspNetUsers / AspNetRoles use string GUID Ids — no sqlite_sequence entry.
    if (!TryConvertToInt64(max, out var seq))
    {
        return;
    }

    cmd.CommandText = "INSERT OR REPLACE INTO sqlite_sequence (name, seq) VALUES (@name, @seq)";
    cmd.Parameters.Clear();
    cmd.Parameters.AddWithValue("@name", table);
    cmd.Parameters.AddWithValue("@seq", seq);
    await cmd.ExecuteNonQueryAsync();
}

static bool TryConvertToInt64(object value, out long result)
{
    switch (value)
    {
        case long l:
            result = l;
            return true;
        case int i:
            result = i;
            return true;
        case short s:
            result = s;
            return true;
        case byte b:
            result = b;
            return true;
        case string s when long.TryParse(s, out result):
            return true;
        default:
            result = 0;
            return false;
    }
}

static async Task<bool> SqlServerTableExistsAsync(SqlConnection sql, string table)
{
    await using var cmd = sql.CreateCommand();
    cmd.CommandText = """
        SELECT 1
        FROM INFORMATION_SCHEMA.TABLES
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @Table
        """;
    cmd.Parameters.AddWithValue("@Table", table);
    return await cmd.ExecuteScalarAsync() is not null;
}

static async Task<bool> SqliteTableExistsAsync(SqliteConnection sqlite, string table)
{
    await using var cmd = sqlite.CreateCommand();
    cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = @Table";
    cmd.Parameters.AddWithValue("@Table", table);
    return await cmd.ExecuteScalarAsync() is not null;
}

static async Task<long> CountSqlServerAsync(SqlConnection sql, string table)
{
    if (!await SqlServerTableExistsAsync(sql, table))
    {
        return 0;
    }

    await using var cmd = sql.CreateCommand();
    cmd.CommandText = $"SELECT COUNT(*) FROM [dbo].[{table}]";
    return Convert.ToInt64(await cmd.ExecuteScalarAsync() ?? 0);
}

static async Task<long> CountSqliteAsync(SqliteConnection sqlite, string table)
{
    if (!await SqliteTableExistsAsync(sqlite, table))
    {
        return 0;
    }

    await using var cmd = sqlite.CreateCommand();
    cmd.CommandText = $"SELECT COUNT(*) FROM [{table}]";
    return Convert.ToInt64(await cmd.ExecuteScalarAsync() ?? 0);
}
