using KaraokeList.Api.Services.Import;
using KaraokeList.Data;
using KaraokeList.Shared;
using Microsoft.EntityFrameworkCore;

namespace KaraokeList.Api.Services;

public sealed class CatalogImportService(ApplicationDbContext db)
{
    public const int MaxImportRows = 5000;

    internal async Task<CatalogImportResultDto> ImportAsync(IReadOnlyList<CatalogImportRow> rows)
    {
        var result = new CatalogImportResultDto { TotalRows = rows.Count };

        // Pre-load catalog into memory for fast lookup
        var artistByName = (await db.Artists.ToListAsync())
            .ToDictionary(a => a.Name, a => a.Id, StringComparer.OrdinalIgnoreCase);

        var genreByName = (await db.Genres.ToListAsync())
            .ToDictionary(g => g.GenreName, g => g.Id, StringComparer.OrdinalIgnoreCase);

        var existingSongKeys = (await db.Songs
                .Where(s => s.Artist != null)
                .Select(s => new { s.Title, ArtistId = s.Artist!.Value })
                .ToListAsync())
            .Select(s => MakeSongKey(s.Title, s.ArtistId))
            .ToHashSet();

        var importRows = rows.Take(MaxImportRows).ToList();

        foreach (var row in importRows)
        {
            if (string.IsNullOrWhiteSpace(row.Title))
            {
                result.Errors.Add(new CatalogImportErrorDto { Row = row.SourceRow, Message = "Title is required." });
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.Artist))
            {
                result.Errors.Add(new CatalogImportErrorDto { Row = row.SourceRow, Message = "Artist is required." });
                continue;
            }

            var artistName = row.Artist.Trim();
            if (artistName.Length > 128)
                artistName = artistName[..128];

            if (!artistByName.TryGetValue(artistName, out var artistId))
            {
                var artist = new Artist
                {
                    Name = artistName,
                    SortableName = SortableNameFormatting.FromDisplayName(artistName)
                };
                db.Artists.Add(artist);
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (Exception ex) when (ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true
                                        || ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true)
                {
                    // Race: another request created this artist; reload
                    db.Entry(artist).State = EntityState.Detached;
                    var existing = await db.Artists.FirstOrDefaultAsync(a => a.Name == artistName);
                    if (existing is null)
                    {
                        result.Errors.Add(new CatalogImportErrorDto { Row = row.SourceRow, Message = $"Could not create artist '{artistName}'." });
                        continue;
                    }
                    artistId = existing.Id;
                }
                artistId = artist.Id;
                artistByName[artistName] = artistId;
            }

            int? genreId = null;
            if (!string.IsNullOrWhiteSpace(row.Genre))
            {
                var genreName = row.Genre.Trim();
                if (!genreByName.TryGetValue(genreName, out var gid))
                {
                    var genre = new Genre { GenreName = genreName };
                    db.Genres.Add(genre);
                    await db.SaveChangesAsync();
                    gid = genre.Id;
                    genreByName[genreName] = gid;
                }
                genreId = gid;
            }

            var key = MakeSongKey(row.Title.Trim(), artistId);
            if (existingSongKeys.Contains(key))
            {
                result.Skipped++;
                continue;
            }

            db.Songs.Add(new Song
            {
                Title = row.Title.Trim(),
                Artist = artistId,
                Genre = genreId,
                Year = row.Year
            });
            existingSongKeys.Add(key);
            result.Added++;
        }

        await db.SaveChangesAsync();

        if (rows.Count > MaxImportRows)
        {
            result.Errors.Add(new CatalogImportErrorDto
            {
                Row = MaxImportRows + 1,
                Message = $"File has {rows.Count} rows; only the first {MaxImportRows} were processed."
            });
        }

        return result;
    }

    private static string MakeSongKey(string title, int artistId) =>
        $"{title.Trim().ToLowerInvariant()}|{artistId}";
}
