using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

string? sqlitePathArg = args.Length > 0 ? args[0] : null;

static string? FindFirstExisting(params string[] paths)
{
    foreach (var p in paths)
    {
        if (File.Exists(p))
        {
            return p;
        }
    }
    return null;
}

var sqlitePath = sqlitePathArg
    ?? FindFirstExisting(
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "scripts", "data", "Karaoke.sqlite3")),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "scripts", "data", "Karaoke.sqlite3")))
    ?? throw new FileNotFoundException(
        "Could not locate Karaoke.sqlite3. Pass its path as the first argument.",
        sqlitePathArg ?? "(none)");

var sqlConnectionString = Environment.GetEnvironmentVariable("KARAOKE_SQL_CONNECTION")
    ?? throw new InvalidOperationException("Set KARAOKE_SQL_CONNECTION to your Azure SQL / SQL Server connection string.");

if (!File.Exists(sqlitePath))
{
    throw new FileNotFoundException("SQLite database not found.", sqlitePath);
}

Console.WriteLine($"SQLite: {sqlitePath}");
Console.WriteLine("SQL Server: (from KARAOKE_SQL_CONNECTION)");

await using var sqlite = new SqliteConnection($"Data Source={sqlitePath}");
await sqlite.OpenAsync();

await using var sql = new SqlConnection(sqlConnectionString);
await sql.OpenAsync();

var tables = new (string Table, string[] Columns, bool HasIdentity)[]
{
    ("Genres", ["Id", "GenreName"], true),
    ("Artists", ["Id", "Name", "SortableName", "MainGenre"], true),
    ("Singers", ["Id", "Name"], true),
    ("Venues", ["Id", "VenueName"], true),
    ("Songs", ["Id", "Title", "Genre", "Year"], true),
    ("Performances", ["Id", "Singer", "Song", "Venue", "PerformedOn", "KeyChangeSemitones"], true),
};

foreach (var (table, columns, _) in tables)
{
    await MigrateTableAsync(sqlite, sql, table, columns);
    Console.WriteLine($"Migrated {table}");
}

await MigrateSongArtistsAsync(sqlite, sql);
Console.WriteLine("Migrated SongArtists");

Console.WriteLine("Done.");

static async Task MigrateTableAsync(SqliteConnection sqlite, SqlConnection sql, string table, string[] columns)
{
    await using var clear = sql.CreateCommand();
    clear.CommandText = $"DELETE FROM dbo.[{table}]";
    try
    {
        await clear.ExecuteNonQueryAsync();
    }
    catch (SqlException)
    {
        // Table may not exist yet; app startup creates schema.
    }

    // SQL Server allows a column named Count only with brackets.
    // (Count is a built-in function name in T-SQL.)
    var columnListSelect = string.Join(", ", columns.Select(c => c == "Count" ? "[Count]" : c));
    
    await using var read = sqlite.CreateCommand();
    read.CommandText = $"SELECT {columnListSelect} FROM {table}";
    await using var reader = await read.ExecuteReaderAsync();

    await using var identityOn = sql.CreateCommand();
    identityOn.CommandText = $"SET IDENTITY_INSERT dbo.[{table}] ON";
    await identityOn.ExecuteNonQueryAsync();

    var columnList = string.Join(", ", columns.Select(c => c == "Count" ? "[Count]" : c));
    var paramList = string.Join(", ", columns.Select((_, i) => $"@p{i}"));

    while (await reader.ReadAsync())
    {
        await using var insert = sql.CreateCommand();
        insert.CommandText = $"INSERT INTO dbo.[{table}] ({columnList}) VALUES ({paramList})";
        for (var i = 0; i < columns.Length; i++)
        {
            insert.Parameters.AddWithValue($"@p{i}", reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i));
        }

        await insert.ExecuteNonQueryAsync();
    }

    await using var identityOff = sql.CreateCommand();
    identityOff.CommandText = $"SET IDENTITY_INSERT dbo.[{table}] OFF";
    await identityOff.ExecuteNonQueryAsync();
}

static async Task MigrateSongArtistsAsync(SqliteConnection sqlite, SqlConnection sql)
{
    await using var clear = sql.CreateCommand();
    clear.CommandText = "DELETE FROM dbo.[SongArtists]";
    try
    {
        await clear.ExecuteNonQueryAsync();
    }
    catch (SqlException)
    {
        // Table may not exist yet; app startup creates schema.
    }

    await using var read = sqlite.CreateCommand();
    read.CommandText = "SELECT Id, Artist, SecondaryArtist FROM Songs";
    await using var reader = await read.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        var songId = reader.GetInt32(0);
        if (!reader.IsDBNull(1))
        {
            await InsertSongArtistAsync(sql, songId, reader.GetInt32(1), 0);
        }

        if (!reader.IsDBNull(2))
        {
            var secondaryArtist = reader.GetInt32(2);
            if (secondaryArtist != 0)
            {
                await InsertSongArtistAsync(sql, songId, secondaryArtist, 1);
            }
        }
    }
}

static async Task InsertSongArtistAsync(SqlConnection sql, int songId, int artistId, int displayOrder)
{
    await using var insert = sql.CreateCommand();
    insert.CommandText = """
        IF NOT EXISTS (SELECT 1 FROM dbo.SongArtists WHERE SongId = @SongId AND ArtistId = @ArtistId)
        INSERT INTO dbo.SongArtists (SongId, ArtistId, DisplayOrder)
        VALUES (@SongId, @ArtistId, @DisplayOrder);
        """;
    insert.Parameters.AddWithValue("@SongId", songId);
    insert.Parameters.AddWithValue("@ArtistId", artistId);
    insert.Parameters.AddWithValue("@DisplayOrder", displayOrder);
    await insert.ExecuteNonQueryAsync();
}
