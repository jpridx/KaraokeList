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
        Action<string>? onProgress = null,
        bool forceRefresh = false);

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

    Task PatchCachesAfterPerformanceAsync(
        int songId,
        string title,
        string artistName,
        string artistDisplay,
        DateTime performedOn,
        bool removeFromWantToSing);

    Task AddSongToCachedListAsync(SingerListKind kind, RepertoireSongDto song);

    Task RemoveSongFromCachedListAsync(SingerListKind kind, int songId);
}

public sealed class MySongsLoader(
    IMyListsLoader myListsLoader,
    IMySongsLocalStore store,
    ILogPerformanceLocalStore logStore) : IMySongsLoader
{
    public async Task<MySongsLoadResult> LoadAsync(
        SingerListKind listKind,
        string sortBy,
        string sortDir,
        int? genreId,
        string? groupName = null,
        Action<string>? onProgress = null,
        bool forceRefresh = false)
    {
        var bundle = await myListsLoader.LoadAsync(onProgress, forceRefresh);
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

    public Task PatchCachedSongGenreAsync(int songId, int? genreId, string genreName) =>
        myListsLoader.RunExclusiveAsync(() => PatchCachedSongGenreCoreAsync(songId, genreId, genreName));

    private async Task PatchCachedSongGenreCoreAsync(int songId, int? genreId, string genreName)
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

    public Task PatchSongPerformanceAsync(
        int songId,
        string title,
        string artistName,
        string artistDisplay,
        DateTime performedOn) =>
        myListsLoader.RunExclusiveAsync(() =>
            PatchSongPerformanceCoreAsync(songId, title, artistName, artistDisplay, performedOn));

    public async Task PatchCachesAfterPerformanceAsync(
        int songId,
        string title,
        string artistName,
        string artistDisplay,
        DateTime performedOn,
        bool removeFromWantToSing)
    {
        await myListsLoader.RunExclusiveAsync(async () =>
        {
            await PatchSongPerformanceCoreAsync(songId, title, artistName, artistDisplay, performedOn);
            if (removeFromWantToSing)
            {
                await PatchMySongsListAsync(
                    SingerListKind.WantToSing,
                    songs => songs.Where(s => s.SongId != songId).ToList());
            }

            await PatchLogCatalogAfterPerformanceAsync(songId, title, artistName, artistDisplay, performedOn);
        });
    }

    private async Task PatchSongPerformanceCoreAsync(
        int songId,
        string title,
        string artistName,
        string artistDisplay,
        DateTime performedOn)
    {
        var cached = await store.GetCachedListsAsync();
        if (cached is null)
        {
            return;
        }

        var performedDate = performedOn.Date;
        var addedSong = new RepertoireSongDto
        {
            SongId = songId,
            Title = title,
            ArtistName = artistName,
            ArtistDisplay = artistDisplay,
            LastPerformedOn = performedDate,
            PerformanceCount = 1
        };

        if (cached.ListsSongs.Count == 0
            || cached.ListsSongs.All(entry => entry.Kind != SingerListKind.MyRepertoire))
        {
            var listsSongs = cached.ListsSongs
                .Append(new CachedListSongsEntry(SingerListKind.MyRepertoire, [addedSong]))
                .ToList();
            await store.SaveCachedListsAsync(cached with
            {
                ListsSongs = listsSongs,
                CachedAtUtc = DateTime.UtcNow
            });
            return;
        }

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
                    songs.Add(addedSong);
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

    public Task SetSongPerformanceStatsAsync(
        int songId,
        string title,
        string artistName,
        string artistDisplay,
        DateTime? lastPerformedOn,
        int performanceCount) =>
        myListsLoader.RunExclusiveAsync(() =>
            SetSongPerformanceStatsCoreAsync(
                songId, title, artistName, artistDisplay, lastPerformedOn, performanceCount));

    private async Task SetSongPerformanceStatsCoreAsync(
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

    public async Task AddSongToCachedListAsync(SingerListKind kind, RepertoireSongDto song)
    {
        await myListsLoader.RunExclusiveAsync(async () =>
        {
            await PatchMySongsListAsync(kind, songs =>
            {
                if (songs.Any(s => s.SongId == song.SongId))
                {
                    return songs;
                }

                songs.Add(song);
                return songs;
            }, createIfMissing: true);

            await PatchLogCatalogListMembershipAsync(kind, song.SongId, added: true, song);
        });
    }

    public async Task RemoveSongFromCachedListAsync(SingerListKind kind, int songId)
    {
        await myListsLoader.RunExclusiveAsync(async () =>
        {
            await PatchMySongsListAsync(
                kind,
                songs => songs.Where(s => s.SongId != songId).ToList());
            await PatchLogCatalogListMembershipAsync(kind, songId, added: false, song: null);
        });
    }

    private async Task PatchMySongsListAsync(
        SingerListKind kind,
        Func<List<RepertoireSongDto>, List<RepertoireSongDto>> update,
        bool createIfMissing = false)
    {
        var cached = await store.GetCachedListsAsync();
        if (cached is null)
        {
            return;
        }

        if (cached.ListsSongs.Count == 0)
        {
            if (!createIfMissing)
            {
                return;
            }

            await store.SaveCachedListsAsync(cached with
            {
                ListsSongs = [new CachedListSongsEntry(kind, update([]))]
            });
            return;
        }

        var hasEntry = cached.ListsSongs.Any(entry => entry.Kind == kind);
        List<CachedListSongsEntry> updatedListsSongs;

        if (!hasEntry && createIfMissing)
        {
            updatedListsSongs = cached.ListsSongs
                .Append(new CachedListSongsEntry(kind, update([])))
                .ToList();
        }
        else
        {
            updatedListsSongs = cached.ListsSongs
                .Select(entry =>
                {
                    if (entry.Kind != kind)
                    {
                        return entry;
                    }

                    return new CachedListSongsEntry(kind, update(entry.Songs.ToList()));
                })
                .ToList();
        }

        await store.SaveCachedListsAsync(cached with { ListsSongs = updatedListsSongs });
    }

    private async Task PatchLogCatalogListMembershipAsync(
        SingerListKind kind,
        int songId,
        bool added,
        RepertoireSongDto? song)
    {
        if (kind is not (SingerListKind.WorkingUp or SingerListKind.MyRepertoire))
        {
            return;
        }

        var logCached = await logStore.GetCachedCatalogAsync();
        if (logCached is null)
        {
            return;
        }

        if (kind == SingerListKind.WorkingUp)
        {
            var workingUpIds = (logCached.WorkingUpSongIds ?? []).ToHashSet();
            if (added)
            {
                workingUpIds.Add(songId);
            }
            else
            {
                workingUpIds.Remove(songId);
            }

            await logStore.SaveCachedCatalogAsync(logCached with
            {
                WorkingUpSongIds = workingUpIds.ToList()
            });
            return;
        }

        var repertoireIds = logCached.RepertoireSongIds.ToHashSet();
        var stats = logCached.RepertoireStats?.ToList();

        if (added && song is not null)
        {
            repertoireIds.Add(songId);
            if (stats is not null && stats.All(s => s.SongId != songId))
            {
                stats.Add(MyListsLoader.MapRepertoireStatsEntry(song));
            }
        }
        else if (!added)
        {
            repertoireIds.Remove(songId);
            stats?.RemoveAll(s => s.SongId == songId);
        }

        await logStore.SaveCachedCatalogAsync(logCached with
        {
            RepertoireSongIds = repertoireIds.ToList(),
            RepertoireStats = stats
        });
    }

    private async Task PatchLogCatalogAfterPerformanceAsync(
        int songId,
        string title,
        string artistName,
        string artistDisplay,
        DateTime performedOn)
    {
        var cached = await logStore.GetCachedCatalogAsync();
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

        await logStore.SaveCachedCatalogAsync(cached with
        {
            RepertoireStats = stats,
            RepertoireSongIds = repertoireIds.ToList(),
            CachedAtUtc = DateTime.UtcNow
        });
    }
}
