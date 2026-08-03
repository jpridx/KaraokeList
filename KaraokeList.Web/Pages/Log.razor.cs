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
        var cached = await CatalogLoader.TryGetCachedAsync();

        if (cached is not null)
        {
            // Fast path: render immediately from cache, then refresh in the background.
            // MarkOnline() suppresses the offline banner — cached data is shown for
            // performance, not because the API is unreachable.
            catalogState.Apply(cached);
            catalogState.MarkOnline();

            recentLogs = await LogStore.GetRecentLogsAsync();
            isLoading = false;

            await EnsureLogFormResourcesAsync();

            if (SongId is int querySongId)
            {
                selectedSongId = querySongId;
                await OnSongChangedAsync(querySongId);
            }

            StateHasChanged();
            _ = RefreshCatalogInBackgroundAsync();
            return;
        }

        // No cache — full foreground load.
        await FullLoadCatalogAsync();

        if (SongId is int querySongId2)
        {
            selectedSongId = querySongId2;
            await OnSongChangedAsync(querySongId2);
        }
    }

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
            catalogState.Apply(new LogCatalogSnapshot([], [], [], FromCache: true, HasCatalog: false, null));
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
                        var listsBundle = await MyListsLoader.TryGetCachedAsync()
                            ?? await MyListsLoader.LoadAsync();
                        if (listsBundle.Succeeded)
                        {
                            singerLists = listsBundle.Lists.ToList();
                        }
                    }

                    recentLogs = await LogStore.GetRecentLogsAsync();
                    await InvokeAsync(StateHasChanged);
                }
            });
            return;
        }

        catalogState.Apply(await loadTask);

        if (!catalogState.UsingOfflineCatalog)
        {
            var listsBundle = await MyListsLoader.TryGetCachedAsync()
                ?? await MyListsLoader.LoadAsync();
            if (listsBundle.Succeeded)
            {
                singerLists = listsBundle.Lists.ToList();
            }
        }

        recentLogs = await LogStore.GetRecentLogsAsync();
        loadingStep = null;
        isLoading = false;

        await EnsureLogFormResourcesAsync();
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

    private async Task OnPerformanceSavedAsync(string? message)
    {
        var savedSong = SelectedSong;
        var savedSongId = selectedSongId;
        var recentEntry = (await LogStore.GetRecentLogsAsync()).FirstOrDefault();

        selectedSongId = null;
        songHint = null;
        selectedSongSummary = null;
        saveMessage = message;
        saveError = null;
        recentLogs = await LogStore.GetRecentLogsAsync();

        if (savedSongId is int songId && savedSong is not null && recentEntry?.SongId == songId)
        {
            await CatalogLoader.PatchRepertoireStatsAfterLogAsync(
                songId,
                savedSong.Title,
                savedSong.ArtistName,
                savedSong.ArtistName,
                recentEntry.PerformedOn);
            await MySongsLoader.PatchSongPerformanceAsync(
                songId,
                savedSong.Title,
                savedSong.ArtistName,
                savedSong.ArtistName,
                recentEntry.PerformedOn);

            catalogState.RepertoireSongIds.Add(songId);
            var cached = await CatalogLoader.TryGetCachedAsync();
            if (cached is not null && selectedSongId is null)
            {
                catalogState.Apply(cached);
                catalogState.MarkOnline();
            }
        }
    }

    private Task OnAddedToWorkingUpAsync()
    {
        if (selectedSongId is int songId)
        {
            catalogState.WorkingUpSongIds.Add(songId);
        }

        return Task.CompletedTask;
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
