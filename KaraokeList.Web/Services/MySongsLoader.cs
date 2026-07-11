using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public interface IMySongsLoader
{
    Task<MySongsLoadResult> LoadAsync(
        SingerListKind listKind,
        string sortBy,
        string sortDir,
        int? genreId,
        Action<string>? onProgress = null);

    Task<MySongsLoadResult?> TryGetCachedAsync(
        SingerListKind listKind,
        string sortBy,
        string sortDir,
        int? genreId);

    Task<bool> NeedsRefreshAsync();
}

public sealed class MySongsLoader(
    IKaraokeApiClient api,
    IMySongsLocalStore store,
    ICatalogVersionService versionService) : IMySongsLoader
{
    private static readonly TimeSpan RefreshThreshold = TimeSpan.FromHours(2);

    // Bump this when the shape of cached data changes in a way that requires a fresh load.
    // Old cached JSON deserializes SchemaVersion to 0, so any value >= 1 triggers invalidation.
    private const int CurrentCacheSchemaVersion = 2;

    public async Task<MySongsLoadResult> LoadAsync(
        SingerListKind listKind,
        string sortBy,
        string sortDir,
        int? genreId,
        Action<string>? onProgress = null)
    {
        try
        {
            onProgress?.Invoke("Loading your playlists...");
            var listsResult = await api.GetMyListsAsync();
            if (!listsResult.Succeeded)
            {
                return await LoadOfflineOrFailAsync(
                    listKind,
                    sortBy,
                    sortDir,
                    genreId,
                    listsResult.ErrorMessage,
                    listsResult.ErrorMessage?.Contains("not linked", StringComparison.OrdinalIgnoreCase) == true);
            }

            var songsByKind = await LoadAllListSongsAsync(listsResult.Lists, onProgress);
            onProgress?.Invoke("Loading genre groups...");
            var genreGroups = await api.GetGenreGroupsAsync();
            onProgress?.Invoke("Saving for offline use...");
            var cachedAt = DateTime.UtcNow;
            var cacheTag = await versionService.GetCacheTagAsync();
            await store.SaveCachedListsAsync(new CachedMySongsLists(
                listsResult.Lists,
                songsByKind.Select(kv => new CachedListSongsEntry(kv.Key, kv.Value)).ToList(),
                cachedAt,
                cacheTag,
                CurrentCacheSchemaVersion,
                genreGroups));

            return BuildResult(
                listsResult.Lists,
                songsByKind,
                listKind,
                sortBy,
                sortDir,
                genreId,
                genreGroups,
                FromCache: false,
                cachedAt);
        }
        catch (Exception ex) when (IsOfflineFailure(ex))
        {
            return await LoadOfflineOrFailAsync(listKind, sortBy, sortDir, genreId, null, needsSingerLink: false);
        }
    }

    public async Task<MySongsLoadResult?> TryGetCachedAsync(
        SingerListKind listKind,
        string sortBy,
        string sortDir,
        int? genreId)
    {
        var cached = await store.GetCachedListsAsync();
        if (cached is null || cached.ListsSongs.Count == 0)
        {
            return null;
        }

        // Old cache (SchemaVersion < CurrentCacheSchemaVersion) may have missing or incorrect
        // GenreId values, but the song and list data itself is still usable. Serve it so the
        // fast path can show content immediately rather than blocking on a cold DB.
        // NeedsRefreshAsync() returns true for old schema, so a background refresh will fetch
        // fresh data (with correct GenreId) once the API is reachable.

        var songsByKind = cached.ListsSongs.ToDictionary(
            entry => entry.Kind,
            entry => entry.Songs.ToList());

        return BuildResult(cached.Lists, songsByKind, listKind, sortBy, sortDir, genreId, cached.GenreGroups ?? [], FromCache: true, cached.CachedAtUtc);
    }

    public async Task<bool> NeedsRefreshAsync()
    {
        var cached = await store.GetCachedListsAsync();
        if (cached is null || cached.ListsSongs.Count == 0)
        {
            return true;
        }

        if (cached.SchemaVersion < CurrentCacheSchemaVersion)
        {
            return true;
        }

        var isStaleByAge = DateTime.UtcNow - cached.CachedAtUtc >= RefreshThreshold;

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
                await store.SaveCachedListsAsync(cached with { CachedAtUtc = DateTime.UtcNow });
            }

            return false;
        }
        catch
        {
            return isStaleByAge;
        }
    }

    private async Task<Dictionary<SingerListKind, List<RepertoireSongDto>>> LoadAllListSongsAsync(
        IReadOnlyList<SingerListDto> lists,
        Action<string>? onProgress = null)
    {
        var songsByKind = new Dictionary<SingerListKind, List<RepertoireSongDto>>();
        foreach (var list in lists)
        {
            onProgress?.Invoke($"Loading {list.DisplayName} songs...");
            var songsResult = await api.GetListSongsAsync(list.Id);
            if (songsResult.Succeeded)
            {
                songsByKind[list.Kind] = songsResult.Songs.ToList();
            }
        }

        return songsByKind;
    }

    private async Task<MySongsLoadResult> LoadOfflineOrFailAsync(
        SingerListKind listKind,
        string sortBy,
        string sortDir,
        int? genreId,
        string? errorMessage,
        bool needsSingerLink)
    {
        var cached = await store.GetCachedListsAsync();
        if (cached is null || cached.ListsSongs.Count == 0)
        {
            if (needsSingerLink)
            {
                return new MySongsLoadResult(
                    [],
                    [],
                    [],
                    [],
                    FromCache: false,
                    HasCache: false,
                    null,
                    errorMessage,
                    true);
            }

            return new MySongsLoadResult(
                [],
                [],
                [],
                [],
                FromCache: true,
                HasCache: false,
                null,
                errorMessage ?? "Could not load lists. Open My Songs once while online to cache them.",
                false);
        }

        var songsByKind = cached.ListsSongs.ToDictionary(
            entry => entry.Kind,
            entry => entry.Songs.ToList());

        return BuildResult(
            cached.Lists,
            songsByKind,
            listKind,
            sortBy,
            sortDir,
            genreId,
            cached.GenreGroups ?? [],
            FromCache: true,
            cached.CachedAtUtc);
    }

    private static MySongsLoadResult BuildResult(
        IReadOnlyList<SingerListDto> lists,
        IReadOnlyDictionary<SingerListKind, List<RepertoireSongDto>> songsByKind,
        SingerListKind listKind,
        string sortBy,
        string sortDir,
        int? genreId,
        IReadOnlyList<GenreGroupDto> genreGroups,
        bool FromCache,
        DateTime? cachedAt)
    {
        if (!songsByKind.TryGetValue(listKind, out var allSongs))
        {
            allSongs = [];
        }

        var filtered = ApplyGenreFilter(allSongs, genreId);
        var sorted = RepertoireSongSort.Apply(filtered, sortBy, sortDir);
        var filterGenres = BuildFilterGenres(allSongs);

        return new MySongsLoadResult(
            lists,
            sorted,
            filterGenres,
            genreGroups,
            FromCache,
            HasCache: songsByKind.Count > 0,
            cachedAt,
            null,
            false);
    }

    private static List<RepertoireSongDto> ApplyGenreFilter(
        IReadOnlyList<RepertoireSongDto> songs,
        int? genreId) =>
        genreId is int id
            ? songs.Where(s => s.GenreId == id).ToList()
            : songs.ToList();

    private static List<GenreDto> BuildFilterGenres(IReadOnlyList<RepertoireSongDto> songs) =>
        songs
            .Where(s => s.GenreId is int)
            .GroupBy(s => s.GenreId!.Value)
            .Select(g => new GenreDto { Id = g.Key, GenreName = g.First().GenreName })
            .OrderBy(g => g.GenreName, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static bool IsOfflineFailure(Exception ex) =>
        ApiTransientFailure.IsTransient(ex) || ex is HttpRequestException;
}
