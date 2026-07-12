using KaraokeList.Api.Services.Import;
using KaraokeList.Data;
using KaraokeList.Shared;
using Microsoft.EntityFrameworkCore;

namespace KaraokeList.Api.Services;

public sealed class CatalogImportService(ApplicationDbContext db, ICanonicalCatalogService canonicalCatalogService)
{
    public const int MaxImportRows = 5000;

    internal async Task<CatalogImportResultDto> ImportAsync(
        IReadOnlyList<CatalogImportRow> rows,
        bool canonicize = false,
        CancellationToken cancellationToken = default)
    {
        var result = new CatalogImportResultDto { TotalRows = rows.Count };

        var artistByName = (await db.Artists.ToListAsync(cancellationToken))
            .ToDictionary(a => a.Name, a => a, StringComparer.OrdinalIgnoreCase);

        var genreByName = (await db.Genres.ToListAsync(cancellationToken))
            .ToDictionary(g => g.GenreName, g => g.Id, StringComparer.OrdinalIgnoreCase);

        var existingSongKeys = (await db.Songs
                .Where(s => s.Artist != null)
                .Select(s => new { s.Title, ArtistId = s.Artist!.Value })
                .ToListAsync(cancellationToken))
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

            var title = row.Title.Trim();
            var artistName = row.Artist.Trim();
            if (artistName.Length > 128)
            {
                artistName = artistName[..128];
            }

            string? recordingMbid = null;
            string? artistMbid = null;
            if (canonicize)
            {
                var canonical = await canonicalCatalogService.CanonicizeRowAsync(title, artistName, cancellationToken);
                if (canonical is not null)
                {
                    if (!string.Equals(title, canonical.Title, StringComparison.OrdinalIgnoreCase)
                        || !string.Equals(artistName, canonical.ArtistName, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Canonicized++;
                    }

                    title = canonical.Title;
                    artistName = canonical.ArtistName;
                    if (artistName.Length > 128)
                    {
                        artistName = artistName[..128];
                    }

                    recordingMbid = canonical.RecordingMbid;
                    artistMbid = canonical.ArtistMbid;
                }
            }

            var artistId = await ResolveArtistIdAsync(artistName, artistMbid, artistByName, cancellationToken);
            if (artistId is null)
            {
                result.Errors.Add(new CatalogImportErrorDto
                {
                    Row = row.SourceRow,
                    Message = $"Could not create artist '{artistName}'."
                });
                continue;
            }

            int? genreId = null;
            if (!string.IsNullOrWhiteSpace(row.Genre))
            {
                var genreName = row.Genre.Trim();
                if (!genreByName.TryGetValue(genreName, out var gid))
                {
                    var genre = new Genre { GenreName = genreName };
                    db.Genres.Add(genre);
                    await db.SaveChangesAsync(cancellationToken);
                    gid = genre.Id;
                    genreByName[genreName] = gid;
                }
                genreId = gid;
            }

            var key = MakeSongKey(title, artistId.Value);
            if (existingSongKeys.Contains(key))
            {
                result.Skipped++;
                continue;
            }

            db.Songs.Add(new Song
            {
                Title = title,
                Artist = artistId,
                Genre = genreId,
                Year = row.Year,
                RecordingMbid = recordingMbid
            });
            existingSongKeys.Add(key);
            result.Added++;
        }

        await db.SaveChangesAsync(cancellationToken);

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

    private async Task<int?> ResolveArtistIdAsync(
        string artistName,
        string? artistMbid,
        Dictionary<string, Artist> artistByName,
        CancellationToken cancellationToken)
    {
        if (artistByName.TryGetValue(artistName, out var existingArtist))
        {
            if (string.IsNullOrWhiteSpace(existingArtist.Mbid) && !string.IsNullOrWhiteSpace(artistMbid))
            {
                existingArtist.Mbid = artistMbid;
            }

            return existingArtist.Id;
        }

        var artist = new Artist
        {
            Name = artistName,
            SortableName = SortableNameFormatting.FromDisplayName(artistName),
            Mbid = artistMbid
        };
        db.Artists.Add(artist);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true
                                || ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true)
        {
            db.Entry(artist).State = EntityState.Detached;
            var existing = await db.Artists.FirstOrDefaultAsync(a => a.Name == artistName, cancellationToken);
            if (existing is null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(existing.Mbid) && !string.IsNullOrWhiteSpace(artistMbid))
            {
                existing.Mbid = artistMbid;
            }

            artistByName[artistName] = existing;
            return existing.Id;
        }

        artistByName[artistName] = artist;
        return artist.Id;
    }

    private static string MakeSongKey(string title, int artistId) =>
        $"{title.Trim().ToLowerInvariant()}|{artistId}";
}
