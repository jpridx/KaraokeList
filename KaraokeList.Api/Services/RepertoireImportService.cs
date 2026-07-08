using KaraokeList.Api.Services.Import;
using KaraokeList.Data;
using KaraokeList.Shared;
using Microsoft.EntityFrameworkCore;

namespace KaraokeList.Api.Services;

public sealed class RepertoireImportService(ApplicationDbContext db, SingerListService singerListService)
{
    public const int MaxImportRows = SingerListService.MaxImportSongCount;

    internal async Task<(bool Succeeded, string? Error, ImportSingerListFromFileResponse? Result)> ImportRowsAsync(
        int singerId,
        SingerListKind listKind,
        IReadOnlyList<CatalogImportRow> rows)
    {
        if (rows.Count == 0)
        {
            return (false, "The file has no song rows to import.", null);
        }

        if (rows.Count > MaxImportRows)
        {
            return (false, $"Too many rows in one import (max {MaxImportRows}).", null);
        }

        var catalog = await db.Songs
            .Where(s => s.Artist != null)
            .Join(db.Artists, s => s.Artist, a => a.Id, (s, a) => new { s.Id, s.Title, ArtistName = a.Name })
            .ToListAsync();

        var songByKey = catalog
            .GroupBy(s => MakeMatchKey(s.Title, s.ArtistName))
            .ToDictionary(g => g.Key, g => g.First().Id);

        var response = new ImportSingerListFromFileResponse { TotalRows = rows.Count };
        var matchedSongIds = new List<int>();
        var seenSongIds = new HashSet<int>();

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Title))
            {
                response.Errors.Add(new CatalogImportErrorDto
                {
                    Row = row.SourceRow,
                    Message = "Title is required."
                });
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.Artist))
            {
                response.Errors.Add(new CatalogImportErrorDto
                {
                    Row = row.SourceRow,
                    Message = "Artist is required."
                });
                continue;
            }

            var key = MakeMatchKey(row.Title, row.Artist);
            if (!songByKey.TryGetValue(key, out var songId))
            {
                response.NotFound++;
                response.Errors.Add(new CatalogImportErrorDto
                {
                    Row = row.SourceRow,
                    Message = $"No catalog match for \"{row.Title.Trim()}\" by {row.Artist.Trim()}."
                });
                continue;
            }

            response.Matched++;
            if (seenSongIds.Add(songId))
            {
                matchedSongIds.Add(songId);
            }
        }

        if (matchedSongIds.Count == 0)
        {
            return (false, "No rows matched songs in the catalog. Check title and artist spelling, or ask an admin to import missing songs first.", response);
        }

        var importResult = await singerListService.ImportSongsAsync(singerId, listKind, matchedSongIds);
        if (!importResult.Succeeded || importResult.Result is null)
        {
            return (false, importResult.Error ?? "Could not import songs.", response);
        }

        response.Added = importResult.Result.Added;
        response.Skipped = importResult.Result.Skipped;
        response.Rejected = importResult.Result.Rejected;
        return (true, null, response);
    }

    private static string MakeMatchKey(string title, string artist) =>
        $"{title.Trim().ToLowerInvariant()}|{artist.Trim().ToLowerInvariant()}";
}
