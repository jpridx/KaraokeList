using KaraokeList.Shared;
using Microsoft.Data.SqlClient;

namespace KaraokeList.Data;

public class Performance
{
    public int Id { get; set; }
    public int Singer { get; set; }
    public int Song { get; set; }
    public int? Venue { get; set; }
    public DateTime PerformedOn { get; set; }
    public int? KeyChangeSemitones { get; set; }
}

public class PerformanceParticipant
{
    public int Id { get; set; }
    public int PerformanceId { get; set; }
    public int? SingerId { get; set; }
    public string? DisplayName { get; set; }
    public int SortOrder { get; set; }
}

public class CoPerformerInfo
{
    public int? SingerId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class PerformanceHistoryEntry
{
    public int Id { get; set; }
    public DateTime PerformedOn { get; set; }
    public int? VenueId { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public int? KeyChangeSemitones { get; set; }
    public List<CoPerformerInfo> OtherPerformers { get; set; } = [];
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

public class StaleSong
{
    public int SongId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public DateTime? LastPerformedOn { get; set; }
    public int PerformanceCount { get; set; }
}

public class SingerStats
{
    public int TotalPerformances { get; set; }
    public int UniqueSongs { get; set; }
    public DateTime? LastPerformedOn { get; set; }
    public string? LastVenueName { get; set; }
    public int PerformancesThisMonth { get; set; }
    public int PerformancesThisYear { get; set; }
    public List<VenuePerformanceCount> TopVenues { get; set; } = [];
    public List<SongPerformanceCount> TopSongs { get; set; } = [];
    public List<ArtistPerformanceCount> TopArtists { get; set; } = [];
    public List<NewRepertoireSong> NewRepertoireSongs { get; set; } = [];
    public int NewRepertoireDays { get; set; }
}

public class VenuePerformanceCount
{
    public string VenueName { get; set; } = string.Empty;
    public int PerformanceCount { get; set; }
}

public class SongPerformanceCount
{
    public int SongId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public int PerformanceCount { get; set; }
}

public class ArtistPerformanceCount
{
    public int ArtistId { get; set; }
    public string ArtistName { get; set; } = string.Empty;
    public int PerformanceCount { get; set; }
}

public class NewRepertoireSong
{
    public int SongId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public DateTime FirstPerformedOn { get; set; }
}

public class RepertoireGenre
{
    public int Id { get; set; }
    public string GenreName { get; set; } = string.Empty;
}

public class MyPerformanceEntry
{
    public int Id { get; set; }
    public int SongId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public DateTime PerformedOn { get; set; }
    public int? VenueId { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public int? KeyChangeSemitones { get; set; }
    public List<CoPerformerInfo> OtherPerformers { get; set; } = [];
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

    public async Task<List<MyPerformanceEntry>> GetMyPerformancesAsync(
        int singerId,
        int? venueId = null,
        string sortDir = "desc")
    {
        var direction = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";
        var performances = new List<MyPerformanceEntry>();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        var sql = """
            SELECT p.Id, p.Song, s.Title, ISNULL(a.Name, N''), p.PerformedOn,
                   p.Venue, ISNULL(v.VenueName, N''), p.KeyChangeSemitones
            FROM Performances p
            INNER JOIN Songs s ON s.Id = p.Song
            LEFT JOIN Artists a ON a.Id = s.Artist
            LEFT JOIN Venues v ON v.Id = p.Venue
            WHERE p.Singer = @Singer
            """;
        command.Parameters.AddWithValue("@Singer", singerId);
        if (venueId is int venue)
        {
            sql += " AND p.Venue = @Venue";
            command.Parameters.AddWithValue("@Venue", venue);
        }

        sql += $" ORDER BY p.PerformedOn {direction}, p.Id {direction}";
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            performances.Add(new MyPerformanceEntry
            {
                Id = reader.GetInt32(0),
                SongId = reader.GetInt32(1),
                Title = reader.GetString(2),
                ArtistName = reader.GetString(3),
                PerformedOn = reader.GetDateTime(4),
                VenueId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                VenueName = reader.GetString(6),
                KeyChangeSemitones = reader.IsDBNull(7) ? null : reader.GetInt32(7)
            });
        }

        var coPerformers = await GetCoPerformersByPerformanceIdsAsync(performances.Select(p => p.Id).ToList());
        foreach (var performance in performances)
        {
            if (coPerformers.TryGetValue(performance.Id, out var performers))
            {
                performance.OtherPerformers = performers;
            }
        }

        return performances;
    }

    public async Task<Dictionary<int, List<CoPerformerInfo>>> GetCoPerformersByPerformanceIdsAsync(
        IReadOnlyCollection<int> performanceIds)
    {
        var result = new Dictionary<int, List<CoPerformerInfo>>();
        if (performanceIds.Count == 0)
        {
            return result;
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        var idList = string.Join(",", performanceIds);
        command.CommandText = $"""
            SELECT pp.PerformanceId, pp.SingerId, ISNULL(s.Name, N''), ISNULL(pp.DisplayName, N''), pp.SortOrder
            FROM PerformanceParticipants pp
            LEFT JOIN Singers s ON s.Id = pp.SingerId
            WHERE pp.PerformanceId IN ({idList})
            ORDER BY pp.PerformanceId, pp.SortOrder, pp.Id
            """;
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var performanceId = reader.GetInt32(0);
            var singerId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1);
            var singerName = reader.GetString(2);
            var displayName = reader.GetString(3);
            var name = singerId is int ? singerName : displayName;
            if (!result.TryGetValue(performanceId, out var performers))
            {
                performers = [];
                result[performanceId] = performers;
            }

            performers.Add(new CoPerformerInfo
            {
                SingerId = singerId,
                Name = name
            });
        }

        return result;
    }

    public async Task SetCoPerformersAsync(int performanceId, IReadOnlyList<CoPerformerInputDto> performers)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using (var deleteCommand = connection.CreateCommand())
        {
            deleteCommand.CommandText = "DELETE FROM PerformanceParticipants WHERE PerformanceId = @PerformanceId;";
            deleteCommand.Parameters.AddWithValue("@PerformanceId", performanceId);
            await deleteCommand.ExecuteNonQueryAsync();
        }

        for (var i = 0; i < performers.Count; i++)
        {
            var performer = performers[i];
            await using var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = """
                INSERT INTO PerformanceParticipants (PerformanceId, SingerId, DisplayName, SortOrder)
                VALUES (@PerformanceId, @SingerId, @DisplayName, @SortOrder);
                """;
            insertCommand.Parameters.AddWithValue("@PerformanceId", performanceId);
            insertCommand.Parameters.AddWithValue("@SingerId", (object?)performer.SingerId ?? DBNull.Value);
            insertCommand.Parameters.AddWithValue(
                "@DisplayName",
                performer.SingerId is int
                    ? DBNull.Value
                    : performer.DisplayName?.Trim() ?? string.Empty);
            insertCommand.Parameters.AddWithValue("@SortOrder", i);
            await insertCommand.ExecuteNonQueryAsync();
        }
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
            ? "ISNULL(a.SortableName, a.Name) ASC"
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

    public async Task<List<StaleSong>> GetStaleSongsAsync(int singerId, int staleAfterDays, int limit)
    {
        var cutoff = DateTime.Today.AddDays(-staleAfterDays);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (@Limit)
                   SongId,
                   Title,
                   ArtistName,
                   LastPerformedOn,
                   PerformanceCount
            FROM (
                SELECT s.Id AS SongId,
                       s.Title,
                       ISNULL(a.Name, N'') AS ArtistName,
                       MAX(p.PerformedOn) AS LastPerformedOn,
                       COUNT(*) AS PerformanceCount
                FROM Performances p
                INNER JOIN Songs s ON s.Id = p.Song
                LEFT JOIN Artists a ON a.Id = s.Artist
                WHERE p.Singer = @Singer
                GROUP BY s.Id, s.Title, a.Name
                HAVING MAX(p.PerformedOn) < @Cutoff
                  AND NOT EXISTS (
                      SELECT 1
                      FROM SingerSongTicklerExclusions ex
                      WHERE ex.SingerId = @Singer
                        AND ex.SongId = s.Id)

                UNION ALL

                SELECT s.Id AS SongId,
                       s.Title,
                       ISNULL(a.Name, N'') AS ArtistName,
                       CAST(NULL AS datetime2) AS LastPerformedOn,
                       0 AS PerformanceCount
                FROM SingerListSongs sls
                INNER JOIN SingerLists sl ON sl.Id = sls.ListId
                INNER JOIN Songs s ON s.Id = sls.SongId
                LEFT JOIN Artists a ON a.Id = s.Artist
                WHERE sl.SingerId = @Singer
                  AND sl.Kind = @RepertoireKind
                  AND NOT EXISTS (
                      SELECT 1
                      FROM Performances p
                      WHERE p.Singer = @Singer
                        AND p.Song = s.Id)
                  AND NOT EXISTS (
                      SELECT 1
                      FROM SingerSongTicklerExclusions ex
                      WHERE ex.SingerId = @Singer
                        AND ex.SongId = s.Id)
            ) AS candidates
            ORDER BY NEWID()
            """;
        command.Parameters.AddWithValue("@Singer", singerId);
        command.Parameters.AddWithValue("@Cutoff", cutoff);
        command.Parameters.AddWithValue("@Limit", limit);
        command.Parameters.AddWithValue("@RepertoireKind", (int)SingerListKind.MyRepertoire);

        var songs = new List<StaleSong>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            songs.Add(new StaleSong
            {
                SongId = reader.GetInt32(0),
                Title = reader.GetString(1),
                ArtistName = reader.GetString(2),
                LastPerformedOn = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                PerformanceCount = reader.GetInt32(4)
            });
        }

        return songs;
    }

    public async Task<SingerStats> GetSingerStatsAsync(
        int singerId,
        int topVenueLimit = 3,
        int topSongLimit = 0,
        int topArtistLimit = 0,
        int newRepertoireDays = 0)
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var yearStart = new DateTime(today.Year, 1, 1);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var stats = new SingerStats();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT COUNT(*) AS TotalPerformances,
                       COUNT(DISTINCT Song) AS UniqueSongs,
                       SUM(CASE WHEN PerformedOn >= @MonthStart THEN 1 ELSE 0 END) AS MonthCount,
                       SUM(CASE WHEN PerformedOn >= @YearStart THEN 1 ELSE 0 END) AS YearCount
                FROM Performances
                WHERE Singer = @Singer
                """;
            command.Parameters.AddWithValue("@Singer", singerId);
            command.Parameters.AddWithValue("@MonthStart", monthStart);
            command.Parameters.AddWithValue("@YearStart", yearStart);
            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                stats.TotalPerformances = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                stats.UniqueSongs = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                stats.PerformancesThisMonth = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                stats.PerformancesThisYear = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
            }
        }

        if (stats.TotalPerformances == 0)
        {
            return stats;
        }

        await using (var lastCommand = connection.CreateCommand())
        {
            lastCommand.CommandText = """
                SELECT TOP (1) p.PerformedOn, ISNULL(v.VenueName, N'')
                FROM Performances p
                LEFT JOIN Venues v ON v.Id = p.Venue
                WHERE p.Singer = @Singer
                ORDER BY p.PerformedOn DESC, p.Id DESC
                """;
            lastCommand.Parameters.AddWithValue("@Singer", singerId);
            await using var reader = await lastCommand.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                stats.LastPerformedOn = reader.GetDateTime(0);
                var venueName = reader.GetString(1);
                stats.LastVenueName = string.IsNullOrWhiteSpace(venueName) ? null : venueName;
            }
        }

        await using (var venuesCommand = connection.CreateCommand())
        {
            if (topVenueLimit > 0)
            {
                venuesCommand.CommandText = """
                    SELECT TOP (@Limit) ISNULL(v.VenueName, N'Unknown venue') AS VenueName,
                           COUNT(*) AS PerformanceCount
                    FROM Performances p
                    LEFT JOIN Venues v ON v.Id = p.Venue
                    WHERE p.Singer = @Singer
                    GROUP BY v.VenueName
                    ORDER BY COUNT(*) DESC, ISNULL(v.VenueName, N'') ASC
                    """;
                venuesCommand.Parameters.AddWithValue("@Singer", singerId);
                venuesCommand.Parameters.AddWithValue("@Limit", topVenueLimit);
                await using var reader = await venuesCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    stats.TopVenues.Add(new VenuePerformanceCount
                    {
                        VenueName = reader.GetString(0),
                        PerformanceCount = reader.GetInt32(1)
                    });
                }
            }
        }

        if (topSongLimit > 0)
        {
            await using var songsCommand = connection.CreateCommand();
            songsCommand.CommandText = """
                SELECT TOP (@Limit) s.Id, s.Title, ISNULL(a.Name, N''), COUNT(*) AS PerformanceCount
                FROM Performances p
                INNER JOIN Songs s ON s.Id = p.Song
                LEFT JOIN Artists a ON a.Id = s.Artist
                WHERE p.Singer = @Singer
                GROUP BY s.Id, s.Title, a.Name
                ORDER BY COUNT(*) DESC, s.Title ASC
                """;
            songsCommand.Parameters.AddWithValue("@Singer", singerId);
            songsCommand.Parameters.AddWithValue("@Limit", topSongLimit);
            await using var reader = await songsCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                stats.TopSongs.Add(new SongPerformanceCount
                {
                    SongId = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    ArtistName = reader.GetString(2),
                    PerformanceCount = reader.GetInt32(3)
                });
            }
        }

        if (topArtistLimit > 0)
        {
            await using var artistsCommand = connection.CreateCommand();
            artistsCommand.CommandText = """
                SELECT TOP (@Limit) a.Id, a.Name, COUNT(*) AS PerformanceCount
                FROM Performances p
                INNER JOIN Songs s ON s.Id = p.Song
                INNER JOIN Artists a ON a.Id = s.Artist
                WHERE p.Singer = @Singer
                GROUP BY a.Id, a.Name
                ORDER BY COUNT(*) DESC, a.Name ASC
                """;
            artistsCommand.Parameters.AddWithValue("@Singer", singerId);
            artistsCommand.Parameters.AddWithValue("@Limit", topArtistLimit);
            await using var reader = await artistsCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                stats.TopArtists.Add(new ArtistPerformanceCount
                {
                    ArtistId = reader.GetInt32(0),
                    ArtistName = reader.GetString(1),
                    PerformanceCount = reader.GetInt32(2)
                });
            }
        }

        if (newRepertoireDays > 0)
        {
            stats.NewRepertoireDays = newRepertoireDays;
            var cutoff = today.AddDays(-newRepertoireDays);
            await using var newSongsCommand = connection.CreateCommand();
            newSongsCommand.CommandText = """
                SELECT s.Id, s.Title, ISNULL(a.Name, N''), MIN(p.PerformedOn) AS FirstPerformedOn
                FROM Performances p
                INNER JOIN Songs s ON s.Id = p.Song
                LEFT JOIN Artists a ON a.Id = s.Artist
                WHERE p.Singer = @Singer
                GROUP BY s.Id, s.Title, a.Name
                HAVING MIN(p.PerformedOn) >= @Cutoff
                ORDER BY MIN(p.PerformedOn) DESC, s.Title ASC
                """;
            newSongsCommand.Parameters.AddWithValue("@Singer", singerId);
            newSongsCommand.Parameters.AddWithValue("@Cutoff", cutoff);
            await using var reader = await newSongsCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                stats.NewRepertoireSongs.Add(new NewRepertoireSong
                {
                    SongId = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    ArtistName = reader.GetString(2),
                    FirstPerformedOn = reader.GetDateTime(3)
                });
            }
        }

        return stats;
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

        var historyIds = history.Select(h => h.Id).ToList();
        var coPerformers = await GetCoPerformersByPerformanceIdsAsync(historyIds);
        foreach (var entry in history)
        {
            if (coPerformers.TryGetValue(entry.Id, out var performers))
            {
                entry.OtherPerformers = performers;
            }
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

    public async Task<int> AddPerformanceAsync(Performance performance)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Performances (Singer, Song, Venue, PerformedOn, KeyChangeSemitones)
            OUTPUT INSERTED.Id
            VALUES (@Singer, @Song, @Venue, @PerformedOn, @KeyChangeSemitones);
            """;
        AddParameters(command, performance);
        return (int)(await command.ExecuteScalarAsync() ?? 0);
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
        command.Parameters.AddWithValue("@Singer", performance.Singer);
        command.Parameters.AddWithValue("@Song", performance.Song);
        command.Parameters.AddWithValue("@Venue", (object?)performance.Venue ?? DBNull.Value);
        command.Parameters.AddWithValue("@PerformedOn", performance.PerformedOn.Date);
        command.Parameters.AddWithValue("@KeyChangeSemitones", (object?)performance.KeyChangeSemitones ?? DBNull.Value);
    }

    private static Performance ReadPerformance(SqlDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        Singer = reader.GetInt32(1),
        Song = reader.GetInt32(2),
        Venue = reader.IsDBNull(3) ? null : reader.GetInt32(3),
        PerformedOn = reader.GetDateTime(4),
        KeyChangeSemitones = reader.IsDBNull(5) ? null : reader.GetInt32(5)
    };
}
