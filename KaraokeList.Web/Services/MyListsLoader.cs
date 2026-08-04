using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public interface IMyListsLoader
{
    Task<MyListsBundle> LoadAsync(Action<string>? onProgress = null, bool forceRefresh = false);
    Task<MyListsBundle?> TryGetCachedAsync();
    Task<bool> NeedsRefreshAsync();
}

public sealed record MyListsBundle(
    IReadOnlyList<SingerListDto> Lists,
    IReadOnlyDictionary<SingerListKind, IReadOnlyList<RepertoireSongDto>> SongsByKind,
    IReadOnlyList<GenreGroupDto> GenreGroups,
    bool Succeeded,
    bool FromCache,
    DateTime? CachedAtUtc,
    string? ErrorMessage = null,
    bool NeedsSingerLink = false);

/// <summary>
/// Single fetch for singer lists + list songs + genre groups. Writes both the My Songs cache
/// and list-related fields on the log catalog cache so Log and My Songs share one API pass.
/// </summary>
public sealed class MyListsLoader(
    IKaraokeApiClient api,
    IMySongsLocalStore mySongsStore,
    ILogPerformanceLocalStore logStore,
    ICatalogVersionService versionService,
    ITicklerSettingsLocalStore ticklerSettingsStore) : IMyListsLoader
{
    // Bump when cached list shape changes. Old JSON deserializes SchemaVersion to 0.
    internal const int CurrentCacheSchemaVersion = 2;

    private readonly SemaphoreSlim loadGate = new(1, 1);
    private Task<MyListsBundle>? inFlightLoad;

    public async Task<MyListsBundle> LoadAsync(Action<string>? onProgress = null, bool forceRefresh = false)
    {
        var existing = inFlightLoad;
        if (existing is not null)
        {
            return await existing;
        }

        Task<MyListsBundle>? taskToAwait = null;
        MyListsBundle? cachedResult = null;

        await loadGate.WaitAsync();
        try
        {
            existing = inFlightLoad;
            if (existing is not null)
            {
                taskToAwait = existing;
            }
            else if (!forceRefresh && !await NeedsRefreshAsync())
            {
                cachedResult = await TryGetCachedAsync();
            }

            if (taskToAwait is null && cachedResult is null)
            {
                existing = inFlightLoad;
                if (existing is not null)
                {
                    taskToAwait = existing;
                }
                else
                {
                    taskToAwait = LoadCoreAsync(onProgress);
                    inFlightLoad = taskToAwait;
                }
            }
        }
        finally
        {
            loadGate.Release();
        }

        if (cachedResult is not null)
        {
            // Fresh online cache hit — still refresh tickler settings best-effort.
            try
            {
                await RefreshTicklerSettingsAsync();
            }
            catch (Exception ex) when (ApiTransientFailure.IsTransient(ex))
            {
            }

            return cachedResult with { FromCache = false };
        }

        try
        {
            return await taskToAwait!;
        }
        finally
        {
            if (ReferenceEquals(inFlightLoad, taskToAwait))
            {
                inFlightLoad = null;
            }
        }
    }

    public async Task<MyListsBundle?> TryGetCachedAsync()
    {
        var cached = await mySongsStore.GetCachedListsAsync();
        if (cached is null || cached.ListsSongs.Count == 0)
        {
            return null;
        }

        return MapCachedToBundle(cached, fromCache: true);
    }

    public async Task<bool> NeedsRefreshAsync()
    {
        var cached = await mySongsStore.GetCachedListsAsync();
        if (cached is null || cached.ListsSongs.Count == 0)
        {
            return true;
        }

        if (cached.SchemaVersion < CurrentCacheSchemaVersion)
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
                await mySongsStore.SaveCachedListsAsync(cached with { CachedAtUtc = DateTime.UtcNow });
            }

            return false;
        }
        catch
        {
            return isStaleByAge;
        }
    }

    private async Task<MyListsBundle> LoadCoreAsync(Action<string>? onProgress)
    {
        try
        {
            onProgress?.Invoke("Loading your playlists...");
            var listsResult = await api.GetMyListsAsync();
            if (!listsResult.Succeeded)
            {
                return await LoadOfflineOrFailAsync(
                    listsResult.ErrorMessage,
                    listsResult.ErrorMessage?.Contains("not linked", StringComparison.OrdinalIgnoreCase) == true);
            }

            var songsByKind = await LoadAllListSongsAsync(listsResult.Lists, onProgress);
            onProgress?.Invoke("Loading genre groups...");
            var genreGroups = await api.GetGenreGroupsAsync();

            var cachedAt = DateTime.UtcNow;
            var cacheTag = await versionService.GetCacheTagAsync();
            await mySongsStore.SaveCachedListsAsync(new CachedMySongsLists(
                listsResult.Lists,
                songsByKind.Select(kv => new CachedListSongsEntry(kv.Key, kv.Value)).ToList(),
                cachedAt,
                cacheTag,
                CurrentCacheSchemaVersion,
                genreGroups));

            await SyncListFieldsToLogCatalogAsync(songsByKind);
            await RefreshTicklerSettingsAsync();

            return new MyListsBundle(
                listsResult.Lists,
                ToReadOnlyDictionary(songsByKind),
                genreGroups,
                Succeeded: true,
                FromCache: false,
                cachedAt);
        }
        catch (Exception ex) when (ApiTransientFailure.IsTransient(ex))
        {
            return await LoadOfflineOrFailAsync(errorMessage: null, needsSingerLink: false);
        }
    }

    private async Task<MyListsBundle> LoadOfflineOrFailAsync(string? errorMessage, bool needsSingerLink)
    {
        var cached = await TryGetCachedAsync();
        if (cached is not null)
        {
            return cached with { ErrorMessage = errorMessage };
        }

        return new MyListsBundle(
            [],
            new Dictionary<SingerListKind, IReadOnlyList<RepertoireSongDto>>(),
            [],
            Succeeded: false,
            FromCache: !needsSingerLink,
            CachedAtUtc: null,
            errorMessage,
            needsSingerLink);
    }

    private async Task<Dictionary<SingerListKind, List<RepertoireSongDto>>> LoadAllListSongsAsync(
        IReadOnlyList<SingerListDto> lists,
        Action<string>? onProgress)
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

    private async Task RefreshTicklerSettingsAsync()
    {
        var result = await api.GetTicklerSettingsAsync();
        if (result.Succeeded && result.Settings is not null)
        {
            await ticklerSettingsStore.SaveAsync(result.Settings);
        }
    }

    private async Task SyncListFieldsToLogCatalogAsync(
        IReadOnlyDictionary<SingerListKind, List<RepertoireSongDto>> songsByKind)
    {
        var logCached = await logStore.GetCachedCatalogAsync();
        if (logCached is null)
        {
            return;
        }

        var repertoire = songsByKind.GetValueOrDefault(SingerListKind.MyRepertoire) ?? [];
        var workingUp = songsByKind.GetValueOrDefault(SingerListKind.WorkingUp) ?? [];

        // Patch list membership only — preserve CachedAtUtc so catalog TTL still reflects
        // when songs/venues/lookups were last fetched, not when lists were synced.
        await logStore.SaveCachedCatalogAsync(logCached with
        {
            RepertoireSongIds = repertoire.Select(s => s.SongId).ToList(),
            WorkingUpSongIds = workingUp.Select(s => s.SongId).ToList(),
            RepertoireStats = repertoire.Select(MapRepertoireStatsEntry).ToList()
        });
    }

    private static MyListsBundle MapCachedToBundle(CachedMySongsLists cached, bool fromCache) =>
        new(
            cached.Lists,
            cached.ListsSongs.ToDictionary(
                entry => entry.Kind,
                entry => (IReadOnlyList<RepertoireSongDto>)entry.Songs),
            cached.GenreGroups ?? [],
            Succeeded: true,
            FromCache: fromCache,
            cached.CachedAtUtc);

    internal static CachedRepertoireStatsEntry MapRepertoireStatsEntry(RepertoireSongDto song) =>
        new(
            song.SongId,
            song.Title,
            song.ArtistName,
            string.IsNullOrWhiteSpace(song.ArtistDisplay) ? song.ArtistName : song.ArtistDisplay,
            song.LastPerformedOn,
            song.PerformanceCount);

    private static IReadOnlyDictionary<SingerListKind, IReadOnlyList<RepertoireSongDto>> ToReadOnlyDictionary(
        Dictionary<SingerListKind, List<RepertoireSongDto>> songsByKind) =>
        songsByKind.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<RepertoireSongDto>)kv.Value);
}
