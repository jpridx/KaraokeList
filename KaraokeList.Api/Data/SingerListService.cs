using System.Data;
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
            "title" => "s.Title",
            "artist" => "ISNULL(a.SortableName, a.Name)",
            "genre" => "ISNULL(g.GenreName, N'')",
            _ => "MAX(p.PerformedOn)"
        };

        var direction = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
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
        command.CommandText = $"""
            SELECT s.Id,
                   s.Title,
                   {SongArtistSql.PrimaryArtistName} AS ArtistName,
                   {SongArtistSql.ArtistDisplay} AS ArtistDisplay,
                   g.Id AS GenreId,
                   ISNULL(g.GenreName, N'') AS GenreName,
                   MAX(p.PerformedOn) AS LastPerformedOn,
                   COUNT(p.Id) AS PerformanceCount
            FROM SingerListSongs sls
            INNER JOIN Songs s ON s.Id = sls.SongId
            {SongArtistSql.PrimaryArtistJoin}
            LEFT JOIN Genres g ON g.Id = s.Genre
            LEFT JOIN Performances p ON p.Song = s.Id AND p.Singer = @SingerId
            WHERE sls.ListId = @ListId
              AND (@GenreId IS NULL OR s.Genre = @GenreId)
            GROUP BY s.Id, s.Title, s.ArtistCreditDisplay, a.Name, a.SortableName, g.Id, g.GenreName
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
                ArtistDisplay = reader.GetString(3),
                GenreId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                GenreName = reader.GetString(5),
                LastPerformedOn = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                PerformanceCount = reader.GetInt32(7)
            });
        }

        return songs;
    }

    public async Task<TitleArtistCollisionDto?> FindTitleArtistCollisionOnListAsync(int listId, int songId)
    {
        var songKey = await GetSongMatchKeyAsync(songId);
        if (songKey is null)
        {
            return null;
        }

        var listSongs = await (
            from sls in db.SingerListSongs
            join s in db.Songs on sls.SongId equals s.Id
            join sa in db.SongArtists on s.Id equals sa.SongId
            join a in db.Artists on sa.ArtistId equals a.Id
            where sls.ListId == listId
                  && sls.SongId != songId
                  && sa.DisplayOrder == 0
            select new { s.Id, s.Title, ArtistName = a.Name })
            .ToListAsync();

        foreach (var candidate in listSongs)
        {
            if (SongMatchKey.Make(candidate.Title, candidate.ArtistName) == songKey)
            {
                return new TitleArtistCollisionDto
                {
                    ExistingSongId = candidate.Id,
                    Title = candidate.Title,
                    ArtistName = candidate.ArtistName
                };
            }
        }

        return null;
    }

    public async Task<AddListSongResult> TryAddSongAsync(
        int singerId,
        int listId,
        int songId,
        bool allowTitleArtistDuplicate = false)
    {
        var list = await GetOwnedListAsync(singerId, listId);
        if (list is null)
        {
            return AddListSongResult.Fail("List was not found.", AddListSongFailureKind.NotFound);
        }

        if (!await integrity.SongExistsAsync(songId))
        {
            return AddListSongResult.Fail("Song was not found.", AddListSongFailureKind.NotFound);
        }

        var validationError = await ValidateAddAsync(singerId, list.Kind, songId);
        if (validationError is not null)
        {
            return AddListSongResult.Fail(validationError, AddListSongFailureKind.Validation);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var exists = await db.SingerListSongs.AnyAsync(s => s.ListId == listId && s.SongId == songId);
            if (exists)
            {
                await transaction.CommitAsync();
                return AddListSongResult.Ok();
            }

            if (!allowTitleArtistDuplicate)
            {
                if (await GetSongMatchKeyAsync(songId) is null)
                {
                    await transaction.RollbackAsync();
                    return AddListSongResult.Fail(
                        "This song has no primary artist credit, so duplicate title/artist checks cannot run.",
                        AddListSongFailureKind.MissingPrimaryArtist);
                }

                var collision = await FindTitleArtistCollisionOnListAsync(listId, songId);
                if (collision is not null)
                {
                    await transaction.RollbackAsync();
                    return AddListSongResult.Fail(
                        FormatTitleArtistCollisionMessage(collision),
                        AddListSongFailureKind.TitleArtistCollision);
                }
            }

            db.SingerListSongs.Add(new SingerListSong
            {
                ListId = listId,
                SongId = songId,
                AddedUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return AddListSongResult.Ok();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
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

            var titleArtistCollision = await FindTitleArtistCollisionOnListAsync(list.Id, songId);
            if (titleArtistCollision is not null)
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

        var collision = await FindTitleArtistCollisionOnListAsync(list.Id, songId);
        if (collision is not null)
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

    public Task<bool> SongExistsAsync(int songId) => integrity.SongExistsAsync(songId);

    public async Task<List<SingerListKind>> GetListKindsForSongAsync(int singerId, int songId)
    {
        await EnsureSystemListsAsync(singerId);
        return await (
            from sls in db.SingerListSongs
            join sl in db.SingerLists on sls.ListId equals sl.Id
            where sl.SingerId == singerId && sls.SongId == songId
            orderby sl.Kind
            select sl.Kind).ToListAsync();
    }

    public async Task RemoveFromListByKindAsync(int singerId, SingerListKind kind, int songId)
    {
        var list = await db.SingerLists
            .FirstOrDefaultAsync(l => l.SingerId == singerId && l.Kind == kind);
        if (list is null)
        {
            return;
        }

        await RemoveSongAsync(singerId, list.Id, songId);
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

    private async Task<string?> GetSongMatchKeyAsync(int songId)
    {
        var info = await (
            from s in db.Songs
            join sa in db.SongArtists on s.Id equals sa.SongId
            join a in db.Artists on sa.ArtistId equals a.Id
            where s.Id == songId && sa.DisplayOrder == 0
            select new { s.Title, ArtistName = a.Name })
            .FirstOrDefaultAsync();

        return info is null ? null : SongMatchKey.Make(info.Title, info.ArtistName);
    }

    private static string FormatTitleArtistCollisionMessage(TitleArtistCollisionDto collision) =>
        $"This list already has \"{collision.Title}\" by {collision.ArtistName}.";
}
