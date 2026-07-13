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

        var existingSongKeys = (await (
                from sa in db.SongArtists
                join s in db.Songs on sa.SongId equals s.Id
                where sa.DisplayOrder == 0
                select new { s.Title, sa.ArtistId })
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
            string? artistCreditDisplay = null;
            List<CanonicalArtistCreditDto> artistCredits = [];
            if (canonicize)
            {
                var canonical = await canonicalCatalogService.CanonicizeRowAsync(title, artistName, cancellationToken);
                if (canonical is not null)
                {
                    if (!string.Equals(title, canonical.Title, StringComparison.OrdinalIgnoreCase)
                        || !string.Equals(artistName, canonical.ArtistCreditDisplay, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Canonicized++;
                    }

                    title = canonical.Title;
                    artistName = canonical.ArtistName;
                    artistCreditDisplay = canonical.ArtistCreditDisplay;
                    artistCredits = canonical.ArtistCredits;
                    if (artistName.Length > 128)
                    {
                        artistName = artistName[..128];
                    }

                    recordingMbid = canonical.RecordingMbid;
                }
            }

            if (artistCredits.Count == 0)
            {
                var artistId = await ResolveArtistIdAsync(artistName, null, artistByName, cancellationToken);
                if (artistId is null)
                {
                    result.Errors.Add(new CatalogImportErrorDto
                    {
                        Row = row.SourceRow,
                        Message = $"Could not create artist '{artistName}'."
                    });
                    continue;
                }

                artistCredits =
                [
                    new CanonicalArtistCreditDto
                    {
                        Name = artistName,
                        DisplayOrder = 0
                    }
                ];
            }

            var primaryArtistId = await ResolveArtistIdAsync(artistCredits[0].Name, artistCredits[0].ArtistMbid, artistByName, cancellationToken);
            if (primaryArtistId is null)
            {
                result.Errors.Add(new CatalogImportErrorDto
                {
                    Row = row.SourceRow,
                    Message = $"Could not create artist '{artistCredits[0].Name}'."
                });
                continue;
            }

            for (var i = 1; i < artistCredits.Count; i++)
            {
                await ResolveArtistIdAsync(artistCredits[i].Name, artistCredits[i].ArtistMbid, artistByName, cancellationToken);
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

            var key = MakeSongKey(title, primaryArtistId.Value);
            if (existingSongKeys.Contains(key))
            {
                result.Skipped++;
                continue;
            }

            var song = new Song
            {
                Title = title,
                Genre = genreId,
                Year = row.Year,
                RecordingMbid = recordingMbid,
                ArtistCreditDisplay = artistCreditDisplay
            };
            db.Songs.Add(song);
            await db.SaveChangesAsync(cancellationToken);

            for (var i = 0; i < artistCredits.Count; i++)
            {
                var credit = artistCredits[i];
                var creditArtistId = await ResolveArtistIdAsync(credit.Name, credit.ArtistMbid, artistByName, cancellationToken);
                if (creditArtistId is null)
                {
                    continue;
                }

                db.SongArtists.Add(new SongArtist
                {
                    SongId = song.Id,
                    ArtistId = creditArtistId.Value,
                    DisplayOrder = i
                });
            }
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
