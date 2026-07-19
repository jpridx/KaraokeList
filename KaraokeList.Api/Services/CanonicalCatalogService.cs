using KaraokeList.Data;
using KaraokeList.Shared;
using Microsoft.EntityFrameworkCore;

namespace KaraokeList.Api.Services;

public interface ICanonicalCatalogService
{
    Task<CanonicalLookupResponse> LookupAsync(string title, string artist, CancellationToken cancellationToken = default);
    Task<ApplyCanonicalResponse?> ApplyAsync(ApplyCanonicalRequest request, CancellationToken cancellationToken = default);
    Task<CatalogVerifyResultDto> VerifyBatchAsync(CatalogVerifyRequest request, CancellationToken cancellationToken = default);
    Task<CatalogClearMatchesResultDto> ClearMatchesAsync(CatalogClearMatchesRequest request, CancellationToken cancellationToken = default);
    Task<CanonicalMatchDto?> CanonicizeRowAsync(string title, string artist, CancellationToken cancellationToken = default);
}

public sealed class CanonicalCatalogService(
    ApplicationDbContext db,
    IMusicBrainzService musicBrainzService,
    IHttpClientFactory httpClientFactory) : ICanonicalCatalogService
{
    /// <summary>Default batch size — each song needs ~2s of MusicBrainz rate-limited calls.</summary>
    public const int RecommendedVerifyBatchSize = CatalogVerifyRequest.RecommendedBatchSize;

    public const int MaxVerifyBatchSize = 25;

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

        if (request.Year is int year)
        {
            song.Year = year;
        }

        var resolvedGenreId = request.GenreId ?? await ResolveGenreIdAsync(request.GenreName, cancellationToken);
        if (resolvedGenreId is int genreId)
        {
            song.Genre = genreId;
        }

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

        var genreName = resolvedGenreId is int gid
            ? await db.Genres.Where(g => g.Id == gid).Select(g => g.GenreName).FirstOrDefaultAsync(cancellationToken)
            : null;

        return new ApplyCanonicalResponse
        {
            SongId = song.Id,
            Title = song.Title,
            ArtistName = resolvedArtists.FirstOrDefault()?.Name ?? string.Empty,
            ArtistCreditDisplay = song.ArtistCreditDisplay ?? string.Empty,
            ArtistId = resolvedArtists.FirstOrDefault()?.ArtistId,
            RecordingMbid = song.RecordingMbid,
            ArtistMbid = credits.FirstOrDefault()?.ArtistMbid,
            Year = song.Year,
            GenreId = song.Genre,
            GenreName = genreName,
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
        var genreNamesById = await db.Genres.ToDictionaryAsync(g => g.Id, g => g.GenreName, cancellationToken);

        var items = new List<CatalogVerifyItemDto>();
        foreach (var song in songs)
        {
            var credits = creditsBySong.GetValueOrDefault(song.Id, []);
            var primaryName = credits.FirstOrDefault()?.Name ?? string.Empty;
            var currentDisplay = SongArtistFormatting.FormatDisplay(
                song.ArtistCreditDisplay,
                credits.Select(c => c.Name));

            var currentGenreName = song.Genre is int genreId
                ? genreNamesById.GetValueOrDefault(genreId)
                : null;

            var lookup = await musicBrainzService.LookupAsync(song.Title, primaryName, cancellationToken);
            var pool = lookup.Match.Found
                ? new[] { lookup.Match }.Concat(lookup.Alternatives)
                : lookup.Alternatives;
            var poolList = pool.ToList();
            var suggestion = MusicBrainzSearchHelper.SelectBestCredibleSuggestion(poolList, song.Title, primaryName);
            var alternatives = suggestion is null
                ? poolList
                : poolList
                    .Where(m => !string.Equals(m.RecordingMbid, suggestion.RecordingMbid, StringComparison.Ordinal))
                    .ToList();
            alternatives = MusicBrainzSearchHelper.RankMatches(alternatives, song.Title, searchArtist: primaryName);

            var metadataNeedsApply = suggestion is not null && MetadataDiffers(song, currentGenreName, suggestion);

            var namesMatch = suggestion is not null
                && MusicBrainzSearchHelper.NamesMatchCatalog(song.Title, currentDisplay, suggestion)
                && MusicBrainzSearchHelper.IsBestCredibleMatch(suggestion, song.Title, poolList, primaryName)
                && !metadataNeedsApply;

            if (namesMatch && suggestion is not null)
            {
                BackfillMetadataFromSuggestion(song, suggestion);
                if (song.Genre is null && !string.IsNullOrWhiteSpace(suggestion.SuggestedGenreName))
                {
                    song.Genre = await ResolveGenreIdAsync(suggestion.SuggestedGenreName, cancellationToken);
                    currentGenreName = suggestion.SuggestedGenreName;
                }
            }

            items.Add(new CatalogVerifyItemDto
            {
                SongId = song.Id,
                CurrentTitle = song.Title,
                CurrentArtistName = primaryName,
                CurrentArtistDisplay = currentDisplay,
                CurrentYear = song.Year,
                CurrentGenreName = currentGenreName,
                RecordingMbid = song.RecordingMbid,
                Suggestion = suggestion,
                Alternatives = alternatives,
                NamesMatch = namesMatch,
                MetadataNeedsApply = metadataNeedsApply
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
        var lookup = await musicBrainzService.LookupForImportAsync(title, artist, cancellationToken);
        return lookup.Match.Found ? lookup.Match : null;
    }

    public async Task<CatalogClearMatchesResultDto> ClearMatchesAsync(
        CatalogClearMatchesRequest request,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Song> query = db.Songs.Where(s => s.RecordingMbid != null);

        if (request.SongIds is { Count: > 0 })
        {
            query = query.Where(s => request.SongIds.Contains(s.Id));
        }
        else if (!request.ClearAll)
        {
            return new CatalogClearMatchesResultDto();
        }

        var songs = await query.ToListAsync(cancellationToken);
        var totalMatched = await db.Songs.CountAsync(s => s.RecordingMbid != null, cancellationToken);

        foreach (var song in songs)
        {
            song.RecordingMbid = null;
            if (request.ClearYears)
            {
                song.Year = null;
            }
        }

        if (songs.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return new CatalogClearMatchesResultDto
        {
            ClearedCount = songs.Count,
            TotalMatched = totalMatched
        };
    }

    private static void BackfillMetadataFromSuggestion(Song song, CanonicalMatchDto suggestion)
    {
        if (suggestion.RecordingMbid is not null && song.RecordingMbid is null)
        {
            song.RecordingMbid = suggestion.RecordingMbid;
        }

        if (suggestion.Year is int year && song.Year is null)
        {
            song.Year = year;
        }
    }

    private static bool MetadataDiffers(Song song, string? currentGenreName, CanonicalMatchDto suggestion)
    {
        if (suggestion.Year is int year && song.Year != year)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(suggestion.SuggestedGenreName)
            && !string.Equals(currentGenreName, suggestion.SuggestedGenreName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private async Task<int?> ResolveGenreIdAsync(string? genreName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(genreName))
        {
            return null;
        }

        var trimmed = genreName.Trim();
        var existing = await db.Genres
            .FirstOrDefaultAsync(g => g.GenreName == trimmed, cancellationToken);
        if (existing is not null)
        {
            return existing.Id;
        }

        var created = new Genre { GenreName = trimmed };
        db.Genres.Add(created);
        await db.SaveChangesAsync(cancellationToken);
        return created.Id;
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
