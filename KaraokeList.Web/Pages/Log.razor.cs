using KaraokeList.Shared;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using Microsoft.AspNetCore.Components;

namespace KaraokeList.Web.Pages;

public partial class Log
{
    [Inject] private IKaraokeApiClient Api { get; set; } = default!;
    [Inject] private ILogPerformanceLocalStore LogStore { get; set; } = default!;
    [Inject] private ILogCatalogLoader CatalogLoader { get; set; } = default!;
    [Inject] private IMyListsLoader MyListsLoader { get; set; } = default!;
    [Inject] private IMySongsLoader MySongsLoader { get; set; } = default!;

    #region Data loader / orchestration

    private readonly LogCatalogState catalogState = new();
    private bool isLoading = true;
    private string? loadingStep;
    private List<SingerListDto> singerLists = [];
    private IReadOnlyList<RecentLoggedPerformance> recentLogs = [];
    private List<VenueDto> logVenues = [];
    private List<SingerDto> logSingers = [];
    private bool logFormResourcesLoaded;
    private bool logFormUsingOfflineCatalog;
    private SongPerformanceSummaryDto? selectedSongSummary;

    protected override async Task OnParametersSetAsync()
    {
        if (SongId is int id && !isLoading && selectedSongId != id)
        {
            selectedSongId = id;
            await OnSongChangedAsync(id);
        }
    }

    private async Task LoadCatalogAsync(int singerId)
    {
        try
        {
            var cached = await CatalogLoader.TryGetCachedAsync();
            if (cached is not null)
            {
                await ApplyCachedCatalogAsync(cached);
                return;
            }

            await FullLoadCatalogAsync();

            if (SongId is int querySongId)
            {
                selectedSongId = querySongId;
                await OnSongChangedAsync(querySongId);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ApiTransientFailure.IsTransient(ex) || ex is HttpRequestException)
        {
            await ApplyOfflineFallbackAsync();
        }
        catch
        {
            await ApplyOfflineFallbackAsync();
        }
        finally
        {
            isLoading = false;
            loadingStep = null;
        }
    }

    private async Task ApplyCachedCatalogAsync(LogCatalogSnapshot cached)
    {
        catalogState.Apply(cached);
        catalogState.MarkOnline();
        recentLogs = await LogStore.GetRecentLogsAsync();
        await SeedLogVenuesFromCacheAsync();

        if (SongId is int querySongId)
        {
            selectedSongId = querySongId;
            _ = InvokeAsync(async () =>
            {
                await OnSongChangedAsync(querySongId);
                StateHasChanged();
            });
        }

        LoadLogFormResourcesInBackground();
        _ = RefreshCatalogInBackgroundAsync();
    }

    private async Task ApplyOfflineFallbackAsync()
    {
        var cached = await CatalogLoader.TryGetCachedAsync();
        if (cached is not null)
        {
            catalogState.Apply(cached);
        }
        else
        {
            catalogState.Apply(new LogCatalogSnapshot(
                [],
                [],
                [],
                FromCache: true,
                HasCatalog: false,
                CachedAtUtc: null));
        }

        recentLogs = await LogStore.GetRecentLogsAsync();
        await SeedLogVenuesFromCacheAsync();
        LoadLogFormResourcesInBackground();
    }

    private async Task SeedLogVenuesFromCacheAsync()
    {
        var cached = await LogStore.GetCachedCatalogAsync();
        if (cached?.Venues is not { Count: > 0 } cachedVenues)
        {
            return;
        }

        logVenues = cachedVenues
            .Select(v => new VenueDto { Id = v.Id, VenueName = v.VenueName })
            .ToList();
        logFormUsingOfflineCatalog = true;
    }

    private void LoadLogFormResourcesInBackground() =>
        _ = InvokeAsync(async () =>
        {
            try
            {
                await EnsureLogFormResourcesAsync();
                StateHasChanged();
            }
            catch
            {
                // Venues and singers are optional for the first paint.
            }
        });

    private void LoadDeferredCatalogExtrasInBackground() =>
        _ = InvokeAsync(async () =>
        {
            try
            {
                await LoadSingerListsBestEffortAsync();
                await EnsureLogFormResourcesAsync();
                StateHasChanged();
            }
            catch
            {
                // Playlist metadata and form resources are optional after the catalog renders.
            }
        });

    private async Task FullLoadCatalogAsync()
    {
        isLoading = true;
        loadingStep = null;
        saveError = null;

        var loadTask = CatalogLoader.LoadAsync(step => { loadingStep = step; StateHasChanged(); });

        if (await Task.WhenAny(loadTask, Task.Delay(ApiSlowRequestNotifier.PageLoadTimeout)) != loadTask)
        {
            // API is slow (DB waking up) — stop blocking the UI.
            // FullLoadCatalogAsync is only reached when LoadCatalogAsync already confirmed there
            // is no cache, so there is nothing to fall back to here. Show an empty state and
            // let the background task auto-update the UI when the DB eventually wakes.
            catalogState.Apply(new LogCatalogSnapshot(
                [],
                [],
                [],
                FromCache: true,
                HasCatalog: false,
                CachedAtUtc: null));
            recentLogs = await LogStore.GetRecentLogsAsync();
            loadingStep = null;
            isLoading = false;
            _ = InvokeAsync(async () =>
            {
                try
                {
                    await EnsureLogFormResourcesAsync();
                    StateHasChanged();
                }
                catch
                {
                    // Background refresh failures are silent.
                }
            });
            StateHasChanged();

            // Auto-update when the DB eventually wakes and the load completes.
            _ = loadTask.ContinueWith(async t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    catalogState.Apply(t.Result);
                    catalogState.MarkOnline();

                    if (!catalogState.UsingOfflineCatalog)
                    {
                        try
                        {
                            var listsBundle = await MyListsLoader.TryGetCachedAsync()
                                ?? await MyListsLoader.LoadAsync();
                            if (listsBundle.Succeeded)
                            {
                                singerLists = listsBundle.Lists.ToList();
                            }
                        }
                        catch
                        {
                            // Background playlist refresh is optional.
                        }
                    }

                    recentLogs = await LogStore.GetRecentLogsAsync();
                    await InvokeAsync(StateHasChanged);
                }
            });
            return;
        }

        catalogState.Apply(await loadTask);
        recentLogs = await LogStore.GetRecentLogsAsync();
        isLoading = false;
        loadingStep = null;
        LoadDeferredCatalogExtrasInBackground();
    }

    private async Task LoadSingerListsBestEffortAsync()
    {
        if (catalogState.UsingOfflineCatalog)
        {
            return;
        }

        try
        {
            var listsBundle = await MyListsLoader.TryGetCachedAsync()
                ?? await MyListsLoader.LoadAsync();
            if (listsBundle.Succeeded)
            {
                singerLists = listsBundle.Lists.ToList();
            }
        }
        catch
        {
            // Playlist metadata is optional for logging a performance.
        }
    }

    private async Task EnsureLogFormResourcesAsync()
    {
        if (logFormResourcesLoaded)
        {
            return;
        }

        var venueResult = await CatalogLoader.LoadVenuesAsync();
        logVenues = venueResult.Venues.ToList();
        logFormUsingOfflineCatalog = venueResult.FromCache;

        try
        {
            logSingers = await Api.GetSingersAsync();
        }
        catch (Exception ex) when (ApiTransientFailure.IsTransient(ex) || ex is HttpRequestException)
        {
            logFormUsingOfflineCatalog = true;
        }

        logFormResourcesLoaded = true;
    }

    private Task HandleSharedVenuesChangedAsync(IReadOnlyList<VenueDto> venues)
    {
        logVenues = venues.ToList();
        return Task.CompletedTask;
    }

    private async Task RefreshCatalogInBackgroundAsync()
    {
        try
        {
            if (await CatalogLoader.NeedsRefreshAsync())
            {
                var refreshed = await CatalogLoader.LoadAsync();

                // Don't replace the picker if the user has already selected a song.
                if (selectedSongId is null)
                {
                    catalogState.Apply(refreshed);
                }
            }
            else if (await MyListsLoader.NeedsRefreshAsync())
            {
                await MyListsLoader.LoadAsync();
            }

            // Lists are loaded with the catalog via IMyListsLoader (shared My Songs cache).
            var listsBundle = await MyListsLoader.TryGetCachedAsync();
            if (listsBundle is not null)
            {
                singerLists = listsBundle.Lists.ToList();
            }

            recentLogs = await LogStore.GetRecentLogsAsync();
            await InvokeAsync(StateHasChanged);
        }
        catch
        {
            // Background refresh failures are silent.
        }
    }

    #endregion

    #region Song selection state

    [SupplyParameterFromQuery]
    public int? SongId { get; set; }

    private int? selectedSongId;
    private string? songHint;

    private LogSongPickItem? SelectedSong
    {
        get
        {
            if (selectedSongId is not int id)
            {
                return null;
            }

            var fromPicker = catalogState.SongPickerItems.FirstOrDefault(s => s.Id == id);
            if (fromPicker is not null)
            {
                return fromPicker;
            }

            var fromRecent = recentLogs.FirstOrDefault(r => r.SongId == id);
            return fromRecent is null
                ? null
                : new LogSongPickItem(
                    fromRecent.SongId,
                    fromRecent.Title,
                    fromRecent.ArtistName,
                    catalogState.RepertoireSongIds.Contains(id),
                    catalogState.WorkingUpSongIds.Contains(id));
        }
    }

    private async Task OnSongSelectedAsync() => await OnSongChangedAsync(selectedSongId);

    private async Task OnSongChangedAsync(int? songId)
    {
        songHint = null;
        selectedSongSummary = null;
        if (songId is null)
        {
            return;
        }

        if (catalogState.UsingOfflineCatalog)
        {
            songHint = catalogState.RepertoireSongIds.Contains(songId.Value)
                ? "Ready to log (offline — using cached catalog)."
                : "Ready to log.";
            return;
        }

        try
        {
            var result = await Api.GetMySongSummaryAsync(songId.Value);
            if (result.Succeeded && result.Summary is not null)
            {
                selectedSongSummary = result.Summary;
                songHint = SongSummaryHints.Format(result.Summary);
            }
            else
            {
                songHint = "Ready to log.";
            }
        }
        catch (Exception ex) when (ApiTransientFailure.IsTransient(ex) || ex is HttpRequestException)
        {
            songHint = "Ready to log (offline).";
        }
    }

    private async Task SelectSongAsync(int songId)
    {
        selectedSongId = songId;
        await OnSongChangedAsync(songId);
    }

    #endregion

    #region Rendering helpers

    private string? saveMessage;
    private string? saveError;

    private async Task OnPerformanceSavedAsync(PerformanceSavedEventArgs args)
    {
        var savedSong = SelectedSong;
        var savedSongId = selectedSongId;
        var recentEntry = (await LogStore.GetRecentLogsAsync()).FirstOrDefault();

        selectedSongId = null;
        songHint = null;
        selectedSongSummary = null;
        saveMessage = args.Message;
        saveError = null;
        recentLogs = await LogStore.GetRecentLogsAsync();

        if (savedSongId is int songId && savedSong is not null && recentEntry?.SongId == songId)
        {
            try
            {
                await MySongsLoader.PatchCachesAfterPerformanceAsync(
                    songId,
                    savedSong.Title,
                    savedSong.ArtistName,
                    savedSong.ArtistName,
                    recentEntry.PerformedOn,
                    removeFromWantToSing: args.SavedOnServer);
            }
            catch
            {
                // Best-effort cache patch after logging a performance.
            }

            catalogState.RepertoireSongIds.Add(songId);
            var cached = await CatalogLoader.TryGetCachedAsync();
            if (cached is not null && selectedSongId is null)
            {
                catalogState.Apply(cached);
                catalogState.MarkOnline();
            }
        }
    }

    private async Task OnAddedToWorkingUpAsync()
    {
        if (selectedSongId is not int songId)
        {
            return;
        }

        catalogState.WorkingUpSongIds.Add(songId);
        PatchCatalogPickItemMarkers(songId);

        var song = SelectedSong;
        var songDto = new RepertoireSongDto
        {
            SongId = songId,
            Title = song?.Title ?? string.Empty,
            ArtistName = song?.ArtistName ?? string.Empty,
            ArtistDisplay = song?.ArtistName ?? string.Empty
        };

        try
        {
            await MySongsLoader.AddSongToCachedListAsync(SingerListKind.WorkingUp, songDto);
        }
        catch
        {
            // Incremental patch is best-effort; a successful refresh still updates caches.
        }

        try
        {
            var bundle = await MyListsLoader.LoadAsync(forceRefresh: true);
            if (!bundle.Succeeded || bundle.FromCache)
            {
                return;
            }

            var cached = await CatalogLoader.TryGetCachedAsync();
            if (cached is not null)
            {
                catalogState.Apply(cached);
                catalogState.MarkOnline();
                catalogState.WorkingUpSongIds.Add(songId);
                PatchCatalogPickItemMarkers(songId);
            }
        }
        catch
        {
            // Refresh is best-effort after the incremental cache patch.
        }
    }

    private void PatchCatalogPickItemMarkers(int songId)
    {
        var index = catalogState.SongPickerItems.FindIndex(s => s.Id == songId);
        if (index < 0)
        {
            return;
        }

        var existing = catalogState.SongPickerItems[index];
        catalogState.SongPickerItems[index] = existing with
        {
            InRepertoire = catalogState.RepertoireSongIds.Contains(songId),
            InWorkingUp = catalogState.WorkingUpSongIds.Contains(songId)
        };
    }

    private async Task OnNewSongAddedAsync(SongAddedEventArgs args)
    {
        saveError = null;
        catalogState.Apply(args.CatalogSnapshot);
        catalogState.MarkOnline();

        selectedSongId = args.SongId;
        await OnSongChangedAsync(args.SongId);
        saveMessage = "Song added.";
    }

    #endregion
}
