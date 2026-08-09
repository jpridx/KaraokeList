using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public interface IMySongsLoader
{
    Task<MySongsLoadResult> LoadAsync(
        SingerListKind listKind,
        string sortBy,
        string sortDir,
        int? genreId,
        string? groupName = null,
        Action<string>? onProgress = null);

    Task<MySongsLoadResult?> TryGetCachedAsync(
        SingerListKind listKind,
        string sortBy,
        string sortDir,
        int? genreId,
        string? groupName = null);

    Task<bool> NeedsRefreshAsync();
    Task PatchCachedSongGenreAsync(int songId, int? genreId, string genreName);
    Task PatchSongPerformanceAsync(
        int songId,
        string title,
        string artistName,
        string artistDisplay,
        DateTime performedOn);

    Task SetSongPerformanceStatsAsync(
        int songId,
        string title,
        string artistName,
        string artistDisplay,
        DateTime? lastPerformedOn,
        int performanceCount);
}

public sealed class MySongsLoader(
    IMyListsLoader myListsLoader,
    IMySongsLocalStore store) : IMySongsLoader
{
    public async Task<MySongsLoadResult> LoadAsync(
        SingerListKind listKind,
        string sortBy,
        string sortDir,
        int? genreId,
        string? groupName = null,
        Action<string>? onProgress = null)
    {
        var bundle = await myListsLoader.LoadAsync(onProgress);
        if (!bundle.Succeeded)
        {
            return ToLoadResult(
                bundle,
                listKind,
                sortBy,
                sortDir,
                genreId,
                groupName,
                bundle.FromCache,
                bundle.NeedsSingerLink
                    ? bundle.ErrorMessage
                    : bundle.ErrorMessage ?? "Could not load lists. Open My Songs once while online to cache them.");
        }

        return BuildResult(
            bundle.Lists,
            bundle.SongsByKind,
            listKind,
            sortBy,
            sortDir,
            genreId,
            groupName,
            bundle.GenreGroups,
            bundle.FromCache,
            bundle.CachedAtUtc);
    }

    public async Task<MySongsLoadResult?> TryGetCachedAsync(
        SingerListKind listKind,
        string sortBy,
        string sortDir,
        int? genreId,
        string? groupName = null)
    {
        var bundle = await myListsLoader.TryGetCachedAsync();
        if (bundle is null)
        {
            return null;
        }

        return BuildResult(
            bundle.Lists,
            bundle.SongsByKind,
            listKind,
            sortBy,
            sortDir,
            genreId,
            groupName,
            bundle.GenreGroups,
            FromCache: true,
            bundle.CachedAtUtc);
    }

    public Task<bool> NeedsRefreshAsync() => myListsLoader.NeedsRefreshAsync();

    private static MySongsLoadResult ToLoadResult(
        MyListsBundle bundle,
        SingerListKind listKind,
        string sortBy,
        string sortDir,
        int? genreId,
        string? groupName,
        bool fromCache,
        string? errorMessage) =>
        new(
            [],
            [],
            [],
            [],
            [],
            fromCache,
            HasCache: false,
            bundle.CachedAtUtc,
            errorMessage,
            bundle.NeedsSingerLink);

    private static MySongsLoadResult BuildResult(
        IReadOnlyList<SingerListDto> lists,
        IReadOnlyDictionary<SingerListKind, IReadOnlyList<RepertoireSongDto>> songsByKind,
        SingerListKind listKind,
        string sortBy,
        string sortDir,
        int? genreId,
        string? groupName,
        IReadOnlyList<GenreGroupDto> genreGroups,
        bool FromCache,
        DateTime? cachedAt)
    {
        if (!songsByKind.TryGetValue(listKind, out var allSongs))
        {
            allSongs = [];
        }

        var filtered = ApplyFilters(allSongs, genreId, groupName, genreGroups);
        var sorted = RepertoireSongSort.Apply(filtered, sortBy, sortDir);
        var filterGroups = MySongsGenreFilter.BuildFilterGroups(allSongs, genreGroups);
        var filterGenres = MySongsGenreFilter.BuildFilterGenres(allSongs, genreGroups, groupName);

        return new MySongsLoadResult(
            lists,
            sorted,
            filterGenres,
            filterGroups,
            genreGroups,
            FromCache,
            HasCache: songsByKind.Count > 0,
            cachedAt,
            null,
            false);
    }

    private static List<RepertoireSongDto> ApplyFilters(
        IReadOnlyList<RepertoireSongDto> songs,
        int? genreId,
        string? groupName,
        IReadOnlyList<GenreGroupDto> genreGroups)
    {
        if (genreId is int id)
        {
            return songs.Where(s => s.GenreId == id).ToList();
        }

        return MySongsGenreFilter.ApplyGroupFilter(songs, groupName, genreGroups);
    }

    public async Task PatchCachedSongGenreAsync(int songId, int? genreId, string genreName)
    {
        var cached = await store.GetCachedListsAsync();
        if (cached is null || cached.ListsSongs.Count == 0)
        {
            return;
        }

        var updatedListsSongs = cached.ListsSongs
            .Select(entry => new CachedListSongsEntry(
                entry.Kind,
                entry.Songs
                    .Select(song =>
                    {
                        if (song.SongId != songId)
                        {
                            return song;
                        }

                        song.GenreId = genreId;
                        song.GenreName = genreName;
                        return song;
                    })
                    .ToList()))
            .ToList();

        await store.SaveCachedListsAsync(cached with { ListsSongs = updatedListsSongs });
    }

    public async Task PatchSongPerformanceAsync(
        int songId,
        string title,
        string artistName,
        string artistDisplay,
        DateTime performedOn)
    {
        var cached = await store.GetCachedListsAsync();
        if (cached is null || cached.ListsSongs.Count == 0)
        {
            return;
        }

        var performedDate = performedOn.Date;
        var updatedListsSongs = cached.ListsSongs
            .Select(entry =>
            {
                if (entry.Kind != SingerListKind.MyRepertoire)
                {
                    return entry;
                }

                var songs = entry.Songs.ToList();
                var index = songs.FindIndex(s => s.SongId == songId);
                if (index >= 0)
                {
                    var existing = songs[index];
                    songs[index] = new RepertoireSongDto
                    {
                        SongId = existing.SongId,
                        Title = title,
                        ArtistName = artistName,
                        ArtistDisplay = artistDisplay,
                        GenreId = existing.GenreId,
                        GenreName = existing.GenreName,
                        LastPerformedOn = performedDate,
                        PerformanceCount = existing.PerformanceCount + 1
                    };
                }
                else
                {
                    songs.Add(new RepertoireSongDto
                    {
                        SongId = songId,
                        Title = title,
                        ArtistName = artistName,
                        ArtistDisplay = artistDisplay,
                        LastPerformedOn = performedDate,
                        PerformanceCount = 1
                    });
                }

                return new CachedListSongsEntry(entry.Kind, songs);
            })
            .ToList();

        await store.SaveCachedListsAsync(cached with
        {
            ListsSongs = updatedListsSongs,
            CachedAtUtc = DateTime.UtcNow
        });
    }

    public async Task SetSongPerformanceStatsAsync(
        int songId,
        string title,
        string artistName,
        string artistDisplay,
        DateTime? lastPerformedOn,
        int performanceCount)
    {
        var cached = await store.GetCachedListsAsync();
        if (cached is null || cached.ListsSongs.Count == 0)
        {
            return;
        }

        var safeCount = Math.Max(0, performanceCount);
        var performedDate = lastPerformedOn?.Date;
        var updatedListsSongs = cached.ListsSongs
            .Select(entry =>
            {
                if (entry.Kind != SingerListKind.MyRepertoire)
                {
                    return entry;
                }

                var songs = entry.Songs.ToList();
                var index = songs.FindIndex(s => s.SongId == songId);
                if (index >= 0)
                {
                    var existing = songs[index];
                    songs[index] = new RepertoireSongDto
                    {
                        SongId = existing.SongId,
                        Title = safeCount > 0 ? title : existing.Title,
                        ArtistName = safeCount > 0 ? artistName : existing.ArtistName,
                        ArtistDisplay = safeCount > 0 ? artistDisplay : existing.ArtistDisplay,
                        GenreId = existing.GenreId,
                        GenreName = existing.GenreName,
                        LastPerformedOn = safeCount > 0 ? performedDate : null,
                        PerformanceCount = safeCount
                    };
                }
                else if (safeCount > 0)
                {
                    songs.Add(new RepertoireSongDto
                    {
                        SongId = songId,
                        Title = title,
                        ArtistName = artistName,
                        ArtistDisplay = artistDisplay,
                        LastPerformedOn = performedDate,
                        PerformanceCount = safeCount
                    });
                }

                return new CachedListSongsEntry(entry.Kind, songs);
            })
            .ToList();

        await store.SaveCachedListsAsync(cached with
        {
            ListsSongs = updatedListsSongs,
            CachedAtUtc = DateTime.UtcNow
        });
    }
}
