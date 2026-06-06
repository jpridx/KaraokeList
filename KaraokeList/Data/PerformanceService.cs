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
    public DateTime PerformedOn { get; set; }
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
            SELECT p.PerformedOn, ISNULL(v.VenueName, ''), p.KeyChangeSemitones
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
                PerformedOn = historyReader.GetDateTime(0),
                VenueName = historyReader.GetString(1),
                KeyChangeSemitones = historyReader.IsDBNull(2) ? null : historyReader.GetInt32(2)
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
