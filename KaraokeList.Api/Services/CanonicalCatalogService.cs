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
    IMusicBrainzService musicBrainzService) : ICanonicalCatalogService
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
        if (song is null || song.Artist is not int artistId)
        {
            return null;
        }

        var currentArtist = await db.Artists.FindAsync([artistId], cancellationToken);
        if (currentArtist is null)
        {
            return null;
        }

        var canonicalTitle = request.Title.Trim();
        var canonicalArtistName = request.ArtistName.Trim();
        if (string.IsNullOrWhiteSpace(canonicalTitle) || string.IsNullOrWhiteSpace(canonicalArtistName))
        {
            return null;
        }

        var resolvedArtistId = await ResolveArtistIdAsync(
            canonicalArtistName,
            request.ArtistMbid,
            currentArtist,
            cancellationToken);

        song.Title = canonicalTitle;
        song.Artist = resolvedArtistId;
        song.RecordingMbid = request.RecordingMbid;

        await db.SaveChangesAsync(cancellationToken);

        var artist = await db.Artists.FindAsync([resolvedArtistId], cancellationToken);
        return new ApplyCanonicalResponse
        {
            SongId = song.Id,
            Title = song.Title,
            ArtistName = artist?.Name ?? canonicalArtistName,
            ArtistId = resolvedArtistId,
            RecordingMbid = song.RecordingMbid,
            ArtistMbid = artist?.Mbid
        };
    }

    public async Task<CatalogVerifyResultDto> VerifyBatchAsync(
        CatalogVerifyRequest request,
        CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(request.Limit, 1, MaxVerifyBatchSize);
        var offset = Math.Max(0, request.Offset);

        var query = db.Songs.Where(s => s.Artist != null).AsQueryable();
        if (request.UnverifiedOnly)
        {
            query = query.Where(s => s.RecordingMbid == null);
        }

        var totalMatching = await query.CountAsync(cancellationToken);
        var songs = await query
            .OrderBy(s => s.Id)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var artistIds = songs.Where(s => s.Artist is int id).Select(s => s.Artist!.Value).Distinct().ToList();
        var artists = await db.Artists
            .Where(a => artistIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        var items = new List<CatalogVerifyItemDto>();
        foreach (var song in songs)
        {
            var artistName = song.Artist is int aid && artists.TryGetValue(aid, out var artist)
                ? artist.Name
                : string.Empty;

            var lookup = await musicBrainzService.LookupAsync(song.Title, artistName, cancellationToken);
            var suggestion = lookup.Match.Found ? lookup.Match : null;
            items.Add(new CatalogVerifyItemDto
            {
                SongId = song.Id,
                CurrentTitle = song.Title,
                CurrentArtistName = artistName,
                RecordingMbid = song.RecordingMbid,
                Suggestion = suggestion,
                NamesMatch = suggestion is not null
                    && string.Equals(song.Title, suggestion.Title, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(artistName, suggestion.ArtistName, StringComparison.OrdinalIgnoreCase)
            });
        }

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

    private async Task<int> ResolveArtistIdAsync(
        string canonicalArtistName,
        string? artistMbid,
        Artist currentArtist,
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
            if (string.IsNullOrWhiteSpace(existing.Mbid) && !string.IsNullOrWhiteSpace(artistMbid))
            {
                existing.Mbid = artistMbid;
            }

            return existing.Id;
        }

        if (string.Equals(currentArtist.Name, canonicalArtistName, StringComparison.OrdinalIgnoreCase))
        {
            currentArtist.Name = canonicalArtistName;
            if (string.IsNullOrWhiteSpace(currentArtist.SortableName))
            {
                currentArtist.SortableName = SortableNameFormatting.FromDisplayName(canonicalArtistName);
            }

            if (string.IsNullOrWhiteSpace(currentArtist.Mbid) && !string.IsNullOrWhiteSpace(artistMbid))
            {
                currentArtist.Mbid = artistMbid;
            }

            return currentArtist.Id;
        }

        var created = new Artist
        {
            Name = canonicalArtistName,
            SortableName = SortableNameFormatting.FromDisplayName(canonicalArtistName),
            Mbid = artistMbid
        };
        db.Artists.Add(created);
        await db.SaveChangesAsync(cancellationToken);
        return created.Id;
    }
}
