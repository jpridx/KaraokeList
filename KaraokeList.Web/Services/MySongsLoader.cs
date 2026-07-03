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
    private const int CurrentCacheSchemaVersion = 1;

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
            onProgress?.Invoke("Saving for offline use...");
            var cachedAt = DateTime.UtcNow;
            var cacheTag = await versionService.GetCacheTagAsync();
            await store.SaveCachedListsAsync(new CachedMySongsLists(
                listsResult.Lists,
                songsByKind.Select(kv => new CachedListSongsEntry(kv.Key, kv.Value)).ToList(),
                cachedAt,
                cacheTag,
                CurrentCacheSchemaVersion));

            return BuildResult(
                listsResult.Lists,
                songsByKind,
                listKind,
                sortBy,
                sortDir,
                genreId,
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

        // Old cache written before GenreId was reliably stored won't have a SchemaVersion field,
        // so it deserializes to 0. Treat it as a cache miss to force a fresh foreground load.
        if (cached.SchemaVersion < CurrentCacheSchemaVersion)
        {
            await store.ClearCatalogCacheAsync();
            return null;
        }

        var songsByKind = cached.ListsSongs.ToDictionary(
            entry => entry.Kind,
            entry => entry.Songs.ToList());

        return BuildResult(cached.Lists, songsByKind, listKind, sortBy, sortDir, genreId, FromCache: true, cached.CachedAtUtc);
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

        if (DateTime.UtcNow - cached.CachedAtUtc < RefreshThreshold)
        {
            return false;
        }

        try
        {
            var serverTag = await versionService.GetCacheTagAsync();
            if (serverTag is null)
            {
                return false;
            }

            if (cached.CacheTag == serverTag)
            {
                await store.SaveCachedListsAsync(cached with { CachedAtUtc = DateTime.UtcNow });
                return false;
            }

            return true;
        }
        catch
        {
            return false;
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
