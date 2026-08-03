using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public interface ILogCatalogLoader
{
    Task<LogCatalogSnapshot> LoadAsync(Action<string>? onProgress = null);
    Task<LogCatalogSnapshot?> TryGetCachedAsync();
    Task<bool> NeedsRefreshAsync();
    Task<VenueLoadResult> LoadVenuesAsync();
    Task<LookupsLoadResult> LoadLookupsAsync();
    Task<LookupsLoadResult?> TryGetCachedLookupsAsync();
    Task SaveLookupsAsync(IReadOnlyList<ArtistLookupDto> artists, IReadOnlyList<GenreDto> genres);
    Task<LogCatalogSnapshot> PatchCachedSongAsync(int songId, string title, string artistName);
    Task PatchRepertoireStatsAfterLogAsync(
        int songId,
        string title,
        string artistName,
        string artistDisplay,
        DateTime performedOn);
}

public sealed class LogCatalogLoader(
    IKaraokeApiClient api,
    ILogPerformanceLocalStore store,
    ICatalogVersionService versionService,
    ITicklerExclusionsLocalStore exclusionsStore,
    IMyListsLoader myListsLoader) : ILogCatalogLoader
{

    public async Task<LogCatalogSnapshot> LoadAsync(Action<string>? onProgress = null)
    {
        try
        {
            onProgress?.Invoke("Loading songs...");
            var songs = await api.GetSongsAsync();

            onProgress?.Invoke("Loading artists...");
            var artists = await api.GetArtistLookupsAsync();

            onProgress?.Invoke("Loading genres...");
            var genres = await api.GetGenresAsync();

            onProgress?.Invoke("Loading venues...");
            var venues = await api.GetVenuesAsync();

            var listsBundle = await myListsLoader.LoadAsync(onProgress);
            var repertoire = listsBundle.SongsByKind.GetValueOrDefault(SingerListKind.MyRepertoire) ?? [];
            var workingUp = listsBundle.SongsByKind.GetValueOrDefault(SingerListKind.WorkingUp) ?? [];
            var repertoireIds = repertoire.Select(s => s.SongId).ToHashSet();
            var repertoireStats = repertoire.Select(MyListsLoader.MapRepertoireStatsEntry).ToList();
            var workingUpIds = workingUp.Select(s => s.SongId).ToHashSet();

            var artistNames = artists.ToDictionary(a => a.Id, a => a.Name);
            var pickItems = CatalogSongMapper.ToPickItems(songs, artistNames, repertoireIds, workingUpIds);

            onProgress?.Invoke("Loading tickler exclusions...");
            await RefreshTicklerExclusionsAsync();

            onProgress?.Invoke("Saving for offline use...");
            var cachedAt = DateTime.UtcNow;
            var cacheTag = await versionService.GetCacheTagAsync();
            await store.SaveCachedCatalogAsync(new CachedLogCatalog(
                pickItems.Select(s => new CachedSongEntry(s.Id, s.Title, s.ArtistName)).ToList(),
                repertoireIds.ToList(),
                venues.Select(v => new CachedVenueEntry(v.Id, v.VenueName)).ToList(),
                cachedAt,
                workingUpIds.ToList(),
                cacheTag,
                MapArtistEntries(artists),
                MapGenreEntries(genres),
                repertoireStats));

            return new LogCatalogSnapshot(pickItems, repertoireIds, workingUpIds, FromCache: false, HasCatalog: pickItems.Count > 0, cachedAt);
        }
        catch (Exception ex) when (ApiTransientFailure.IsTransient(ex))
        {
            return await LoadFromCacheAsync();
        }
    }

    public async Task<LogCatalogSnapshot?> TryGetCachedAsync()
    {
        var cached = await store.GetCachedCatalogAsync();
        if (cached is null || cached.Songs.Count == 0)
        {
            return null;
        }

        return MapCacheToSnapshot(cached);
    }

    public async Task<bool> NeedsRefreshAsync()
    {
        var cached = await store.GetCachedCatalogAsync();
        if (cached is null || cached.Songs.Count == 0)
        {
            return true;
        }

        var isStaleByAge = DateTime.UtcNow - cached.CachedAtUtc >= CatalogCachePolicy.RefreshThreshold;

        try
        {
            var serverTag = await versionService.GetCacheTagAsync(forceRefresh: true);
            if (serverTag is null)
            {
                return isStaleByAge;
            }

            if (!string.Equals(cached.CacheTag, serverTag, StringComparison.Ordinal))
            {
                return true;
            }

            if (isStaleByAge)
            {
                // Version unchanged — bump the timestamp to reset the TTL clock.
                await store.SaveCachedCatalogAsync(cached with { CachedAtUtc = DateTime.UtcNow });
            }

            return false;
        }
        catch
        {
            return isStaleByAge;
        }
    }

    public async Task<VenueLoadResult> LoadVenuesAsync()
    {
        try
        {
            var venues = await api.GetVenuesAsync();
            await PatchCachedVenuesAsync(venues);
            return new VenueLoadResult(venues, FromCache: false);
        }
        catch (Exception ex) when (ApiTransientFailure.IsTransient(ex))
        {
            var cached = await store.GetCachedCatalogAsync();
            if (cached?.Venues is { Count: > 0 } cachedVenues)
            {
                return new VenueLoadResult(MapVenues(cachedVenues), FromCache: true);
            }

            return new VenueLoadResult([], FromCache: true);
        }
    }

    public async Task<LookupsLoadResult> LoadLookupsAsync()
    {
        try
        {
            var artists = await api.GetArtistLookupsAsync();
            var genres = await api.GetGenresAsync();
            await SaveLookupsAsync(artists, genres);
            return new LookupsLoadResult(artists, genres, FromCache: false);
        }
        catch (Exception ex) when (ApiTransientFailure.IsTransient(ex))
        {
            return await LoadLookupsFromCacheAsync();
        }
    }

    public async Task<LookupsLoadResult?> TryGetCachedLookupsAsync()
    {
        var cached = await store.GetCachedCatalogAsync();
        if (cached?.Artists is not { Count: > 0 } artists || cached.Genres is not { Count: > 0 } genres)
        {
            return null;
        }

        return new LookupsLoadResult(MapArtists(artists), MapGenres(genres), FromCache: true);
    }

    public async Task SaveLookupsAsync(IReadOnlyList<ArtistLookupDto> artists, IReadOnlyList<GenreDto> genres)
    {
        var artistEntries = MapArtistEntries(artists);
        var genreEntries = MapGenreEntries(genres);
        var cached = await store.GetCachedCatalogAsync();
        if (cached is null)
        {
            await store.SaveCachedCatalogAsync(new CachedLogCatalog(
                [],
                [],
                [],
                DateTime.UtcNow,
                [],
                Artists: artistEntries,
                Genres: genreEntries));
            return;
        }

        await store.SaveCachedCatalogAsync(cached with
        {
            Artists = artistEntries,
            Genres = genreEntries,
            CachedAtUtc = DateTime.UtcNow
        });
    }

    public async Task<LogCatalogSnapshot> PatchCachedSongAsync(int songId, string title, string artistName)
    {
        versionService.Invalidate();
        var cacheTag = await versionService.GetCacheTagAsync(forceRefresh: true);
        var cachedAt = DateTime.UtcNow;
        var cached = await store.GetCachedCatalogAsync();
        var entry = new CachedSongEntry(songId, title.Trim(), artistName.Trim());

        if (cached is null)
        {
            cached = new CachedLogCatalog(
                [entry],
                [],
                [],
                cachedAt,
                [],
                cacheTag);
            await store.SaveCachedCatalogAsync(cached);
            return MapCacheToSnapshot(cached);
        }

        var songs = cached.Songs.Any(s => s.Id == songId)
            ? cached.Songs
            : cached.Songs.Append(entry).ToList();

        cached = cached with
        {
            Songs = songs,
            CachedAtUtc = cachedAt,
            CacheTag = cacheTag
        };
        await store.SaveCachedCatalogAsync(cached);
        return MapCacheToSnapshot(cached);
    }

    public async Task PatchRepertoireStatsAfterLogAsync(
        int songId,
        string title,
        string artistName,
        string artistDisplay,
        DateTime performedOn)
    {
        var cached = await store.GetCachedCatalogAsync();
        if (cached is null)
        {
            return;
        }

        var performedDate = performedOn.Date;
        var stats = cached.RepertoireStats?.ToList() ?? [];
        var existingIndex = stats.FindIndex(s => s.SongId == songId);
        if (existingIndex >= 0)
        {
            var existing = stats[existingIndex];
            stats[existingIndex] = existing with
            {
                Title = title,
                ArtistName = artistName,
                ArtistDisplay = artistDisplay,
                LastPerformedOn = performedDate,
                PerformanceCount = existing.PerformanceCount + 1
            };
        }
        else
        {
            stats.Add(new CachedRepertoireStatsEntry(
                songId,
                title,
                artistName,
                artistDisplay,
                performedDate,
                PerformanceCount: 1));
        }

        var repertoireIds = cached.RepertoireSongIds.ToHashSet();
        repertoireIds.Add(songId);

        cached = cached with
        {
            RepertoireStats = stats,
            RepertoireSongIds = repertoireIds.ToList(),
            CachedAtUtc = DateTime.UtcNow
        };
        await store.SaveCachedCatalogAsync(cached);
    }

    private async Task RefreshTicklerExclusionsAsync()
    {
        var result = await api.GetMyTicklerExclusionsAsync();
        if (result.Succeeded && result.SongIds is not null)
        {
            await exclusionsStore.SaveExcludedSongIdsAsync(result.SongIds);
        }
    }

    private async Task<LookupsLoadResult> LoadLookupsFromCacheAsync()
    {
        var cached = await store.GetCachedCatalogAsync();
        if (cached?.Artists is { Count: > 0 } artists && cached.Genres is { Count: > 0 } genres)
        {
            return new LookupsLoadResult(MapArtists(artists), MapGenres(genres), FromCache: true);
        }

        return new LookupsLoadResult([], [], FromCache: true);
    }

    private async Task<LogCatalogSnapshot> LoadFromCacheAsync()
    {
        var cached = await store.GetCachedCatalogAsync();
        if (cached is null || cached.Songs.Count == 0)
        {
            return new LogCatalogSnapshot([], [], [], FromCache: true, HasCatalog: false, cached?.CachedAtUtc);
        }

        return MapCacheToSnapshot(cached);
    }

    private static LogCatalogSnapshot MapCacheToSnapshot(CachedLogCatalog cached)
    {
        var repertoireIds = cached.RepertoireSongIds.ToHashSet();
        var workingUpIds = (cached.WorkingUpSongIds ?? []).ToHashSet();
        var pickItems = cached.Songs
            .Select(s => new LogSongPickItem(
                s.Id,
                s.Title,
                s.ArtistName,
                repertoireIds.Contains(s.Id),
                workingUpIds.Contains(s.Id)))
            .OrderByDescending(s => s.InRepertoire)
            .ThenByDescending(s => s.InWorkingUp)
            .ThenBy(s => s.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new LogCatalogSnapshot(pickItems, repertoireIds, workingUpIds, FromCache: true, HasCatalog: true, cached.CachedAtUtc);
    }

    private async Task PatchCachedVenuesAsync(IReadOnlyList<VenueDto> venues)
    {
        var venueEntries = venues.Select(v => new CachedVenueEntry(v.Id, v.VenueName)).ToList();
        var cached = await store.GetCachedCatalogAsync();
        if (cached is null)
        {
            await store.SaveCachedCatalogAsync(new CachedLogCatalog([], [], venueEntries, DateTime.UtcNow, []));
            return;
        }

        await store.SaveCachedCatalogAsync(cached with
        {
            Venues = venueEntries,
            CachedAtUtc = DateTime.UtcNow
        });
    }

    private static List<VenueDto> MapVenues(IReadOnlyList<CachedVenueEntry> venues) =>
        venues.Select(v => new VenueDto { Id = v.Id, VenueName = v.VenueName }).ToList();

    private static List<ArtistLookupDto> MapArtists(IReadOnlyList<CachedArtistEntry> artists) =>
        artists.Select(a => new ArtistLookupDto { Id = a.Id, Name = a.Name }).ToList();

    private static List<GenreDto> MapGenres(IReadOnlyList<CachedGenreEntry> genres) =>
        genres.Select(g => new GenreDto { Id = g.Id, GenreName = g.GenreName }).ToList();

    private static List<CachedArtistEntry> MapArtistEntries(IReadOnlyList<ArtistLookupDto> artists) =>
        artists.Select(a => new CachedArtistEntry(a.Id, a.Name)).ToList();

    private static List<CachedGenreEntry> MapGenreEntries(IReadOnlyList<GenreDto> genres) =>
        genres.Select(g => new CachedGenreEntry(g.Id, g.GenreName)).ToList();

}
