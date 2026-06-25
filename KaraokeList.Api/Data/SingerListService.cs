using KaraokeList.Shared;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KaraokeList.Data;

public sealed class SingerListService(
    ApplicationDbContext db,
    CatalogIntegrityService integrity,
    string connectionString)
{
    public async Task EnsureSystemListsAsync(int singerId)
    {
        if (!await integrity.SingerExistsAsync(singerId))
        {
            return;
        }

        var existingKinds = await db.SingerLists
            .Where(l => l.SingerId == singerId)
            .Select(l => l.Kind)
            .ToListAsync();

        var utcNow = DateTime.UtcNow;
        foreach (var kind in Enum.GetValues<SingerListKind>())
        {
            if (existingKinds.Contains(kind))
            {
                continue;
            }

            db.SingerLists.Add(new SingerList
            {
                SingerId = singerId,
                Kind = kind,
                CreatedUtc = utcNow,
                IsSystem = true
            });
        }

        await db.SaveChangesAsync();
    }

    public async Task<List<SingerList>> GetListsAsync(int singerId)
    {
        await EnsureSystemListsAsync(singerId);
        return await db.SingerLists
            .Where(l => l.SingerId == singerId)
            .OrderBy(l => l.Kind)
            .ToListAsync();
    }

    public Task<SingerList?> GetOwnedListAsync(int singerId, int listId) =>
        db.SingerLists.FirstOrDefaultAsync(l => l.Id == listId && l.SingerId == singerId);

    public async Task<List<RepertoireSong>> GetListSongsAsync(
        int singerId,
        int listId,
        string sortBy = "title",
        string sortDir = "asc",
        int? genreId = null)
    {
        var list = await GetOwnedListAsync(singerId, listId);
        if (list is null)
        {
            return [];
        }

        var orderColumn = sortBy.ToLowerInvariant() switch
        {
            "artist" => "ISNULL(a.SortableName, a.Name)",
            "genre" => "ISNULL(g.GenreName, N'')",
            "lastPerformed" => "MAX(p.PerformedOn)",
            _ => "s.Title"
        };

        var direction = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
        var orderClause = sortBy.Equals("lastPerformed", StringComparison.OrdinalIgnoreCase)
            ? $"MAX(p.PerformedOn) {direction}, s.Title ASC"
            : sortBy.Equals("title", StringComparison.OrdinalIgnoreCase)
                ? $"s.Title {direction}"
                : $"{orderColumn} {direction}, s.Title ASC";

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT s.Id,
                   s.Title,
                   ISNULL(a.Name, N'') AS ArtistName,
                   g.Id AS GenreId,
                   ISNULL(g.GenreName, N'') AS GenreName,
                   MAX(p.PerformedOn) AS LastPerformedOn,
                   COUNT(p.Id) AS PerformanceCount
            FROM SingerListSongs sls
            INNER JOIN Songs s ON s.Id = sls.SongId
            LEFT JOIN Artists a ON a.Id = s.Artist
            LEFT JOIN Genres g ON g.Id = s.Genre
            LEFT JOIN Performances p ON p.Song = s.Id AND p.Singer = @SingerId
            WHERE sls.ListId = @ListId
              AND (@GenreId IS NULL OR s.Genre = @GenreId)
            GROUP BY s.Id, s.Title, a.Name, g.Id, g.GenreName
            ORDER BY {orderClause}
            """;
        command.Parameters.AddWithValue("@ListId", listId);
        command.Parameters.AddWithValue("@SingerId", singerId);
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

    public async Task<(bool Succeeded, string? Error)> TryAddSongAsync(int singerId, int listId, int songId)
    {
        var list = await GetOwnedListAsync(singerId, listId);
        if (list is null)
        {
            return (false, "List was not found.");
        }

        if (!await integrity.SongExistsAsync(songId))
        {
            return (false, "Song was not found.");
        }

        var validationError = await ValidateAddAsync(singerId, list.Kind, songId);
        if (validationError is not null)
        {
            return (false, validationError);
        }

        var exists = await db.SingerListSongs.AnyAsync(s => s.ListId == listId && s.SongId == songId);
        if (exists)
        {
            return (true, null);
        }

        db.SingerListSongs.Add(new SingerListSong
        {
            ListId = listId,
            SongId = songId,
            AddedUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> RemoveSongAsync(int singerId, int listId, int songId)
    {
        var list = await GetOwnedListAsync(singerId, listId);
        if (list is null)
        {
            return false;
        }

        var row = await db.SingerListSongs.FirstOrDefaultAsync(s => s.ListId == listId && s.SongId == songId);
        if (row is null)
        {
            return false;
        }

        db.SingerListSongs.Remove(row);
        await db.SaveChangesAsync();
        return true;
    }

    public const int MaxImportSongCount = 5000;

    public async Task<(bool Succeeded, string? Error, ImportSingerListSongsResponse? Result)> ImportSongsAsync(
        int singerId,
        SingerListKind kind,
        IReadOnlyList<int> songIds)
    {
        if (songIds.Count == 0)
        {
            return (false, "At least one songId is required.", null);
        }

        if (songIds.Count > MaxImportSongCount)
        {
            return (false, $"Too many songs in one import (max {MaxImportSongCount}).", null);
        }

        await EnsureSystemListsAsync(singerId);
        var list = await db.SingerLists
            .FirstOrDefaultAsync(l => l.SingerId == singerId && l.Kind == kind);
        if (list is null)
        {
            return (false, "List was not found.", null);
        }

        var response = new ImportSingerListSongsResponse();
        var seen = new HashSet<int>();
        foreach (var songId in songIds)
        {
            if (!seen.Add(songId))
            {
                response.Skipped++;
                continue;
            }

            var alreadyOnList = await db.SingerListSongs
                .AnyAsync(s => s.ListId == list.Id && s.SongId == songId);
            if (alreadyOnList)
            {
                response.Skipped++;
                continue;
            }

            var result = await TryAddSongAsync(singerId, list.Id, songId);
            if (result.Succeeded)
            {
                response.Added++;
            }
            else
            {
                response.Rejected++;
            }
        }

        return (true, null, response);
    }

    public async Task AddToMyRepertoireAsync(int singerId, int songId)
    {
        await EnsureSystemListsAsync(singerId);
        var list = await db.SingerLists
            .FirstAsync(l => l.SingerId == singerId && l.Kind == SingerListKind.MyRepertoire);

        if (!await integrity.SongExistsAsync(songId))
        {
            return;
        }

        var exists = await db.SingerListSongs.AnyAsync(s => s.ListId == list.Id && s.SongId == songId);
        if (exists)
        {
            return;
        }

        db.SingerListSongs.Add(new SingerListSong
        {
            ListId = list.Id,
            SongId = songId,
            AddedUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private async Task<string?> ValidateAddAsync(int singerId, SingerListKind kind, int songId)
    {
        if (kind != SingerListKind.WantToSing)
        {
            return null;
        }

        if (await HasPerformanceForSingerSongAsync(singerId, songId))
        {
            return "Want to sing is only for songs you have not performed.";
        }

        var onRepertoire = await (
            from sls in db.SingerListSongs
            join sl in db.SingerLists on sls.ListId equals sl.Id
            where sl.SingerId == singerId
                  && sl.Kind == SingerListKind.MyRepertoire
                  && sls.SongId == songId
            select sls).AnyAsync();
        if (onRepertoire)
        {
            return "Remove this song from My repertoire before adding it to Want to sing.";
        }

        return null;
    }

    private async Task<bool> HasPerformanceForSingerSongAsync(int singerId, int songId)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM Performances WHERE Singer = @SingerId AND Song = @SongId";
        command.Parameters.AddWithValue("@SingerId", singerId);
        command.Parameters.AddWithValue("@SongId", songId);
        var result = await command.ExecuteScalarAsync();
        return result is not null;
    }
}
