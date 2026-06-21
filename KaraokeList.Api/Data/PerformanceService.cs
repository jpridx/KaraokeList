using Microsoft.Data.SqlClient;

namespace KaraokeList.Data;

public class Performance
{
    public int Id { get; set; }
    public int? Singer { get; set; }
    public int? Song { get; set; }
    public int? Venue { get; set; }
    public DateTime PerformedOn { get; set; }
    public int? KeyChangeSemitones { get; set; }
}

public class PerformanceHistoryEntry
{
    public int Id { get; set; }
    public DateTime PerformedOn { get; set; }
    public int? VenueId { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public int? KeyChangeSemitones { get; set; }
}

public class SongPerformanceSummary
{
    public int SongId { get; set; }
    public int PerformanceCount { get; set; }
    public int? LastKeyChangeSemitones { get; set; }
    public DateTime? LastPerformedOn { get; set; }
    public string? LastVenueName { get; set; }
    public List<PerformanceHistoryEntry> History { get; set; } = [];
}

public class RepertoireSong
{
    public int SongId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public int? GenreId { get; set; }
    public string GenreName { get; set; } = string.Empty;
    public DateTime? LastPerformedOn { get; set; }
    public int PerformanceCount { get; set; }
}

public class RepertoireGenre
{
    public int Id { get; set; }
    public string GenreName { get; set; } = string.Empty;
}

public class PerformanceService(string connectionString)
{
    private const string SelectColumns = "Id, Singer, Song, Venue, PerformedOn, KeyChangeSemitones";

    public async Task<List<Performance>> GetPerformancesAsync(int? singerId = null, int? songId = null)
    {
        var performances = new List<Performance>();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        var sql = $"SELECT {SelectColumns} FROM Performances WHERE 1=1";
        if (singerId is int singer)
        {
            sql += " AND Singer = @Singer";
            command.Parameters.AddWithValue("@Singer", singer);
        }

        if (songId is int song)
        {
            sql += " AND Song = @Song";
            command.Parameters.AddWithValue("@Song", song);
        }

        sql += " ORDER BY PerformedOn DESC, Id DESC";
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            performances.Add(ReadPerformance(reader));
        }

        return performances;
    }

    public async Task<Performance?> GetPerformanceByIdAsync(int id)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {SelectColumns} FROM Performances WHERE Id = @Id;";
        command.Parameters.AddWithValue("@Id", id);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadPerformance(reader) : null;
    }

    public async Task<List<RepertoireSong>> GetMyRepertoireAsync(
        int singerId,
        string sortBy = "lastPerformed",
        string sortDir = "desc",
        int? genreId = null,
        bool includeAll = false)
    {
        var orderColumn = sortBy.ToLowerInvariant() switch
        {
            "title" => "s.Title",
            "artist" => "ISNULL(a.SortableName, a.Name)",
            "genre" => "ISNULL(g.GenreName, N'')",
            _ => "MAX(p.PerformedOn)"
        };

        var direction = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";
        var nullsFirst = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
        var orderBy = sortBy.Equals("lastPerformed", StringComparison.OrdinalIgnoreCase)
            ? $"CASE WHEN MAX(p.PerformedOn) IS NULL THEN {(nullsFirst ? 0 : 1)} ELSE {(nullsFirst ? 1 : 0)} END, MAX(p.PerformedOn) {direction}"
            : $"{orderColumn} {direction}";
        var tiebreaker = sortBy.Equals("title", StringComparison.OrdinalIgnoreCase)
            ? "s.Id ASC"
            : "s.Title ASC";
        var orderClause = $"{orderBy}, {tiebreaker}";

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();

        if (includeAll)
        {
            command.CommandText = $"""
                SELECT s.Id,
                       s.Title,
                       ISNULL(a.Name, N'') AS ArtistName,
                       s.Genre,
                       ISNULL(g.GenreName, N'') AS GenreName,
                       MAX(p.PerformedOn) AS LastPerformedOn,
                       COUNT(p.Id) AS PerformanceCount
                FROM Songs s
                LEFT JOIN Performances p ON p.Song = s.Id AND p.Singer = @Singer
                LEFT JOIN Artists a ON a.Id = s.Artist
                LEFT JOIN Genres g ON g.Id = s.Genre
                WHERE (@GenreId IS NULL OR s.Genre = @GenreId)
                GROUP BY s.Id, s.Title, a.Name, a.SortableName, s.Genre, g.GenreName
                ORDER BY {orderClause}
                """;
        }
        else
        {
            command.CommandText = $"""
                SELECT s.Id,
                       s.Title,
                       ISNULL(a.Name, N'') AS ArtistName,
                       s.Genre,
                       ISNULL(g.GenreName, N'') AS GenreName,
                       MAX(p.PerformedOn) AS LastPerformedOn,
                       COUNT(*) AS PerformanceCount
                FROM Performances p
                INNER JOIN Songs s ON s.Id = p.Song
                LEFT JOIN Artists a ON a.Id = s.Artist
                LEFT JOIN Genres g ON g.Id = s.Genre
                WHERE p.Singer = @Singer
                  AND (@GenreId IS NULL OR s.Genre = @GenreId)
                GROUP BY s.Id, s.Title, a.Name, a.SortableName, s.Genre, g.GenreName
                ORDER BY {orderClause}
                """;
        }

        command.Parameters.AddWithValue("@Singer", singerId);
        command.Parameters.AddWithValue("@GenreId", (object?)genreId ?? DBNull.Value);

        var songs = new List<RepertoireSong>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            songs.Add(new RepertoireSong
            {
                SongId = reader.GetInt32(0),
                Title = reader.GetString(1),
                ArtistName = reader.GetString(2),
                GenreId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                GenreName = reader.GetString(4),
                LastPerformedOn = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                PerformanceCount = reader.GetInt32(6)
            });
        }

        return songs;
    }

    public async Task<List<RepertoireGenre>> GetMyRepertoireGenresAsync(int singerId)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT DISTINCT g.Id, g.GenreName
            FROM Performances p
            INNER JOIN Songs s ON s.Id = p.Song
            INNER JOIN Genres g ON g.Id = s.Genre
            WHERE p.Singer = @Singer
            ORDER BY g.GenreName
            """;
        command.Parameters.AddWithValue("@Singer", singerId);

        var genres = new List<RepertoireGenre>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            genres.Add(new RepertoireGenre
            {
                Id = reader.GetInt32(0),
                GenreName = reader.GetString(1)
            });
        }

        return genres;
    }

    public async Task<SongPerformanceSummary?> GetSongPerformanceSummaryAsync(int singerId, int songId)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var countCommand = connection.CreateCommand();
        countCommand.CommandText = """
            SELECT COUNT(*)
            FROM Performances
            WHERE Singer = @Singer AND Song = @Song
            """;
        countCommand.Parameters.AddWithValue("@Singer", singerId);
        countCommand.Parameters.AddWithValue("@Song", songId);
        var count = (int)(await countCommand.ExecuteScalarAsync() ?? 0);
        if (count == 0)
        {
            return new SongPerformanceSummary { SongId = songId, PerformanceCount = 0 };
        }

        await using var lastCommand = connection.CreateCommand();
        lastCommand.CommandText = """
            SELECT TOP (1) p.PerformedOn, p.KeyChangeSemitones, v.VenueName
            FROM Performances p
            LEFT JOIN Venues v ON v.Id = p.Venue
            WHERE p.Singer = @Singer AND p.Song = @Song
            ORDER BY p.PerformedOn DESC, p.Id DESC
            """;
        lastCommand.Parameters.AddWithValue("@Singer", singerId);
        lastCommand.Parameters.AddWithValue("@Song", songId);
        await using var lastReader = await lastCommand.ExecuteReaderAsync();
        DateTime? lastPerformedOn = null;
        int? lastKeyChange = null;
        string? lastVenueName = null;
        if (await lastReader.ReadAsync())
        {
            lastPerformedOn = lastReader.GetDateTime(0);
            lastKeyChange = lastReader.IsDBNull(1) ? null : lastReader.GetInt32(1);
            lastVenueName = lastReader.IsDBNull(2) ? null : lastReader.GetString(2);
        }

        await lastReader.CloseAsync();

        var history = new List<PerformanceHistoryEntry>();
        await using var historyCommand = connection.CreateCommand();
        historyCommand.CommandText = """
            SELECT p.Id, p.PerformedOn, ISNULL(v.VenueName, ''), p.KeyChangeSemitones, p.Venue
            FROM Performances p
            LEFT JOIN Venues v ON v.Id = p.Venue
            WHERE p.Singer = @Singer AND p.Song = @Song
            ORDER BY p.PerformedOn DESC, p.Id DESC
            """;
        historyCommand.Parameters.AddWithValue("@Singer", singerId);
        historyCommand.Parameters.AddWithValue("@Song", songId);
        await using var historyReader = await historyCommand.ExecuteReaderAsync();
        while (await historyReader.ReadAsync())
        {
            history.Add(new PerformanceHistoryEntry
            {
                Id = historyReader.GetInt32(0),
                PerformedOn = historyReader.GetDateTime(1),
                VenueName = historyReader.GetString(2),
                KeyChangeSemitones = historyReader.IsDBNull(3) ? null : historyReader.GetInt32(3),
                VenueId = historyReader.IsDBNull(4) ? null : historyReader.GetInt32(4)
            });
        }

        return new SongPerformanceSummary
        {
            SongId = songId,
            PerformanceCount = count,
            LastKeyChangeSemitones = lastKeyChange,
            LastPerformedOn = lastPerformedOn,
            LastVenueName = lastVenueName,
            History = history
        };
    }

    public async Task AddPerformanceAsync(Performance performance)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Performances (Singer, Song, Venue, PerformedOn, KeyChangeSemitones)
            VALUES (@Singer, @Song, @Venue, @PerformedOn, @KeyChangeSemitones);
            """;
        AddParameters(command, performance);
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdatePerformanceAsync(Performance performance)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Performances
            SET Singer = @Singer, Song = @Song, Venue = @Venue,
                PerformedOn = @PerformedOn, KeyChangeSemitones = @KeyChangeSemitones
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", performance.Id);
        AddParameters(command, performance);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeletePerformanceAsync(int id)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Performances WHERE Id = @Id;";
        command.Parameters.AddWithValue("@Id", id);
        await command.ExecuteNonQueryAsync();
    }

    private static void AddParameters(SqlCommand command, Performance performance)
    {
        command.Parameters.AddWithValue("@Singer", (object?)performance.Singer ?? DBNull.Value);
        command.Parameters.AddWithValue("@Song", (object?)performance.Song ?? DBNull.Value);
        command.Parameters.AddWithValue("@Venue", (object?)performance.Venue ?? DBNull.Value);
        command.Parameters.AddWithValue("@PerformedOn", performance.PerformedOn.Date);
        command.Parameters.AddWithValue("@KeyChangeSemitones", (object?)performance.KeyChangeSemitones ?? DBNull.Value);
    }

    private static Performance ReadPerformance(SqlDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        Singer = reader.IsDBNull(1) ? null : reader.GetInt32(1),
        Song = reader.IsDBNull(2) ? null : reader.GetInt32(2),
        Venue = reader.IsDBNull(3) ? null : reader.GetInt32(3),
        PerformedOn = reader.GetDateTime(4),
        KeyChangeSemitones = reader.IsDBNull(5) ? null : reader.GetInt32(5)
    };
}
