using KaraokeList.Data;
using KaraokeList.Shared;
using Microsoft.EntityFrameworkCore;

namespace KaraokeList.Api.Services;

public interface ICanonicalCatalogService
{
    Task<CanonicalLookupResponse> LookupAsync(string title, string artist, CancellationToken cancellationToken = default);
    Task<ApplyCanonicalResponse?> ApplyAsync(ApplyCanonicalRequest request, CancellationToken cancellationToken = default);
    Task<CatalogVerifyResultDto> VerifyBatchAsync(CatalogVerifyRequest request, CancellationToken cancellationToken = default);
    Task<CanonicalMatchDto?> CanonicizeRowAsync(string title, string artist, CancellationToken cancellationToken = default);
}

public sealed class CanonicalCatalogService(
    ApplicationDbContext db,
    IMusicBrainzService musicBrainzService,
    IHttpClientFactory httpClientFactory) : ICanonicalCatalogService
{
    public const int MaxVerifyBatchSize = 50;

    public Task<CanonicalLookupResponse> LookupAsync(
        string title,
        string artist,
        CancellationToken cancellationToken = default) =>
        musicBrainzService.LookupAsync(title, artist, cancellationToken);

    public async Task<ApplyCanonicalResponse?> ApplyAsync(
        ApplyCanonicalRequest request,
        CancellationToken cancellationToken = default)
    {
        var song = await db.Songs.FirstOrDefaultAsync(s => s.Id == request.SongId, cancellationToken);
        if (song is null)
        {
            return null;
        }

        var credits = request.ArtistCredits.Count > 0
            ? request.ArtistCredits
            : BuildCreditsFromLegacyRequest(request);

        if (credits.Count == 0)
        {
            return null;
        }

        var canonicalTitle = request.Title.Trim();
        if (string.IsNullOrWhiteSpace(canonicalTitle))
        {
            return null;
        }

        var client = httpClientFactory.CreateClient("MusicBrainz");
        var sortNames = musicBrainzService is MusicBrainzService mbService
            ? await mbService.FetchSortNamesAsync(client, credits.Select(c => c.ArtistMbid), cancellationToken)
            : [];

        var resolvedArtists = new List<SongArtistDto>();
        foreach (var credit in credits.OrderBy(c => c.DisplayOrder))
        {
            var artistId = await ResolveArtistIdAsync(
                credit.Name,
                credit.ArtistMbid,
                sortNames.GetValueOrDefault(credit.ArtistMbid ?? string.Empty),
                cancellationToken);
            resolvedArtists.Add(new SongArtistDto
            {
                ArtistId = artistId,
                DisplayOrder = resolvedArtists.Count,
                Name = credit.Name
            });
        }

        song.Title = canonicalTitle;
        song.RecordingMbid = request.RecordingMbid;
        song.ArtistCreditDisplay = string.IsNullOrWhiteSpace(request.ArtistCreditDisplay)
            ? MusicBrainzService.ComposeArtistCreditDisplay(credits)
            : request.ArtistCreditDisplay.Trim();

        var existingCredits = await db.SongArtists.Where(sa => sa.SongId == song.Id).ToListAsync(cancellationToken);
        db.SongArtists.RemoveRange(existingCredits);
        foreach (var artist in resolvedArtists)
        {
            db.SongArtists.Add(new SongArtist
            {
                SongId = song.Id,
                ArtistId = artist.ArtistId,
                DisplayOrder = artist.DisplayOrder
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        return new ApplyCanonicalResponse
        {
            SongId = song.Id,
            Title = song.Title,
            ArtistName = resolvedArtists.FirstOrDefault()?.Name ?? string.Empty,
            ArtistCreditDisplay = song.ArtistCreditDisplay ?? string.Empty,
            ArtistId = resolvedArtists.FirstOrDefault()?.ArtistId,
            RecordingMbid = song.RecordingMbid,
            ArtistMbid = credits.FirstOrDefault()?.ArtistMbid,
            Artists = resolvedArtists
        };
    }

    public async Task<CatalogVerifyResultDto> VerifyBatchAsync(
        CatalogVerifyRequest request,
        CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(request.Limit, 1, MaxVerifyBatchSize);
        var offset = Math.Max(0, request.Offset);

        var query = db.SongArtists.Where(sa => sa.DisplayOrder == 0).Select(sa => sa.SongId).Distinct();
        var songsQuery = db.Songs.Where(s => query.Contains(s.Id)).AsQueryable();
        if (request.UnverifiedOnly)
        {
            songsQuery = songsQuery.Where(s => s.RecordingMbid == null);
        }

        var totalMatching = await songsQuery.CountAsync(cancellationToken);
        var songs = await songsQuery
            .OrderBy(s => s.Id)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var songIds = songs.Select(s => s.Id).ToList();
        var creditsBySong = await LoadCreditsBySongAsync(songIds, cancellationToken);

        var items = new List<CatalogVerifyItemDto>();
        foreach (var song in songs)
        {
            var credits = creditsBySong.GetValueOrDefault(song.Id, []);
            var primaryName = credits.FirstOrDefault()?.Name ?? string.Empty;
            var currentDisplay = SongArtistFormatting.FormatDisplay(
                song.ArtistCreditDisplay,
                credits.Select(c => c.Name));

            var lookup = await musicBrainzService.LookupAsync(song.Title, primaryName, cancellationToken);
            var suggestion = lookup.Match.Found ? lookup.Match : null;
            var namesMatch = suggestion is not null
                && string.Equals(song.Title, suggestion.Title, StringComparison.OrdinalIgnoreCase)
                && string.Equals(currentDisplay, suggestion.ArtistCreditDisplay, StringComparison.OrdinalIgnoreCase);

            if (namesMatch && suggestion?.RecordingMbid is not null && song.RecordingMbid is null)
            {
                song.RecordingMbid = suggestion.RecordingMbid;
            }

            items.Add(new CatalogVerifyItemDto
            {
                SongId = song.Id,
                CurrentTitle = song.Title,
                CurrentArtistName = primaryName,
                CurrentArtistDisplay = currentDisplay,
                RecordingMbid = song.RecordingMbid,
                Suggestion = suggestion,
                NamesMatch = namesMatch
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        var nextOffset = offset + songs.Count;
        return new CatalogVerifyResultDto
        {
            TotalMatching = totalMatching,
            Scanned = songs.Count,
            Offset = offset,
            HasMore = nextOffset < totalMatching,
            Items = items
        };
    }

    public async Task<CanonicalMatchDto?> CanonicizeRowAsync(
        string title,
        string artist,
        CancellationToken cancellationToken = default)
    {
        var lookup = await musicBrainzService.LookupAsync(title, artist, cancellationToken);
        return lookup.Match.Found ? lookup.Match : null;
    }

    private static List<CanonicalArtistCreditDto> BuildCreditsFromLegacyRequest(ApplyCanonicalRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ArtistName))
        {
            return [];
        }

        return
        [
            new CanonicalArtistCreditDto
            {
                Name = request.ArtistName.Trim(),
                ArtistMbid = request.ArtistMbid,
                DisplayOrder = 0
            }
        ];
    }

    private async Task<Dictionary<int, List<SongArtistDto>>> LoadCreditsBySongAsync(
        IReadOnlyCollection<int> songIds,
        CancellationToken cancellationToken)
    {
        if (songIds.Count == 0)
        {
            return [];
        }

        var rows = await (
            from sa in db.SongArtists
            join a in db.Artists on sa.ArtistId equals a.Id
            where songIds.Contains(sa.SongId)
            orderby sa.SongId, sa.DisplayOrder
            select new
            {
                sa.SongId,
                sa.ArtistId,
                sa.DisplayOrder,
                a.Name
            }).ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.SongId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => new SongArtistDto
                {
                    ArtistId = r.ArtistId,
                    DisplayOrder = r.DisplayOrder,
                    Name = r.Name
                }).ToList());
    }

    private async Task<int> ResolveArtistIdAsync(
        string canonicalArtistName,
        string? artistMbid,
        string? musicBrainzSortName,
        CancellationToken cancellationToken)
    {
        if (canonicalArtistName.Length > 128)
        {
            canonicalArtistName = canonicalArtistName[..128];
        }

        var existing = await db.Artists
            .FirstOrDefaultAsync(a => a.Name == canonicalArtistName, cancellationToken);

        if (existing is not null)
        {
            await BackfillArtistMetadataAsync(existing, artistMbid, musicBrainzSortName, canonicalArtistName);
            return existing.Id;
        }

        var created = new Artist
        {
            Name = canonicalArtistName,
            SortableName = ResolveSortableName(musicBrainzSortName, canonicalArtistName),
            Mbid = artistMbid
        };
        db.Artists.Add(created);
        await db.SaveChangesAsync(cancellationToken);
        return created.Id;
    }

    private async Task BackfillArtistMetadataAsync(
        Artist artist,
        string? artistMbid,
        string? musicBrainzSortName,
        string canonicalArtistName)
    {
        if (string.IsNullOrWhiteSpace(artist.Mbid) && !string.IsNullOrWhiteSpace(artistMbid))
        {
            artist.Mbid = artistMbid;
        }

        if (string.IsNullOrWhiteSpace(artist.SortableName))
        {
            artist.SortableName = ResolveSortableName(musicBrainzSortName, canonicalArtistName);
        }

        await db.SaveChangesAsync();
    }

    private static string? ResolveSortableName(string? musicBrainzSortName, string displayName) =>
        !string.IsNullOrWhiteSpace(musicBrainzSortName)
            ? musicBrainzSortName.Trim()
            : SortableNameFormatting.FromDisplayName(displayName);
}
